using System.Collections.Concurrent;
using System.Reflection;
using Chronux.Core.Configuration.Models;
using Chronux.Core.Execution.Contracts;
using Chronux.Core.Execution.Internal.Contracts;
using Chronux.Core.Execution.Internal.Models;
using Chronux.Core.Execution.Models;
using Chronux.Core.Jobs.Models;
using Chronux.Core.Middleware.Internal.Services;
using Chronux.Core.Middleware.Models;
using Chronux.Core.Scheduling.Models;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chronux.Core.Execution.Internal.Services;

internal sealed class JobExecutor(
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory,
    IDistributedLockProvider lockProvider,
    IChronuxStorageProvider storage,
    IEnumerable<Type> middlewareTypes,
    IJobQueueStore jobQueue,
    IServiceProvider rootProvider,
    ChronuxOptions options) : IJobExecutor
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _running = new();
    public bool IsRunning(string jobId) => _running.ContainsKey(jobId);
    public async ValueTask<JobResult> ExecuteAsync(InternalJobExecutionContext ctx)
    {
        if (options.DisableWorker)
        {
            return JobResult.Failed("Worker execution is disabled by configuration.");
        }

        var def = ctx.Definition;
        var logger = loggerFactory.CreateLogger(def.HandlerType);
        var retryPolicy = def.Retry ?? options.RetryPolicy;

        var attempt = 0;
        var delay = retryPolicy?.Delay ?? TimeSpan.Zero;
        _running[def.Id] = DateTimeOffset.UtcNow;
        while (true)
        {
            attempt++;
            IDisposable? lease = null;

            try
            {
                TriggerDiagnostics(descriptor =>
                {
                    descriptor.FireOn = DiagnosticsOrder.OnStart;
                    descriptor.JobId = def.Id;
                    descriptor.Context = ctx;
                });

                var useLock = def.UseDistributedLock || options.EnableDistributedLocking;
                if (useLock)
                {
                    lease = await lockProvider.TryAcquireLockAsync(
                        def.LockKey ?? def.Id,
                        TimeSpan.FromSeconds(5),
                        ctx.CancellationToken);

                    if (lease is null)
                    {
                        logger.LogWarning("Lock not acquired for job '{JobId}'", def.Id);
                        return JobResult.Failed("Distributed lock not acquired");
                    }
                }

                using var scope = scopeFactory.CreateScope();
                var sp = scope.ServiceProvider;

                var terminal = (JobExecutionDelegate)(context => ExecuteHandler(context, def, sp, logger));

                var jobTimeout = def.Timeout ?? options.Timeout;
                using var cts = jobTimeout.HasValue
                    ? CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken)
                    : null;

                if (cts is not null)
                    cts.CancelAfter(jobTimeout!.Value);

                var effectiveToken = cts?.Token ?? ctx.CancellationToken;

                var pipeline = new JobMiddlewarePipeline(middlewareTypes);
                var execution = pipeline.Build(sp, terminal);

                // Execute via middleware
                var result = await execution(ctx.ToPublicContext(cancellationToken: effectiveToken, tags: def.Tags?.ToArray() ?? []));
                var maxAttempts = retryPolicy?.MaxAttempts ?? 1;
                var reachedMax = attempt >= maxAttempts;
                var duration = DateTimeOffset.UtcNow - ctx.ScheduledTime;

                await storage.AppendExecutionLogAsync(new ExecutionLog
                {
                    JobId = def.Id,
                    ExecutedAt = DateTimeOffset.UtcNow,
                    Success = result.Success,
                    Message = result.ErrorMessage,
                    Exception = result.Exception,
                    Duration = duration,
                    TriggerId = def.Trigger?.Id,
                    InstanceId = options.InstanceId,
                    Tags = def.Tags?.ToArray(),
                    RetryAttempt = attempt,
                    RetryCount = maxAttempts,
                    MaxAttemptsReached = reachedMax,
                    RetryDelay = retryPolicy?.Delay,
                    Output = result.Data
                }, ctx.CancellationToken);
                var chained = result.NextJobIds 
                              ?? (result.Success ? def.OnSuccessChain : def.OnFailureChain);

                if (chained?.Any() == true)
                {
                    var time = options.TimeProvider.GetUtcNow();

                    foreach (var nextJobId in chained)
                    {
                        // Fire diagnostics
                        TriggerDiagnostics(descriptor =>
                        {
                            descriptor.FireOn = DiagnosticsOrder.OnJobChained;
                            descriptor.JobId = def.Id;
                            descriptor.Next = nextJobId;
                        });

                        // Enqueue the next job into the persistent queue
                        var queueItem = new JobQueueItem
                        {
                            Id = Guid.NewGuid().ToString(),
                            JobId = nextJobId,
                            Input = result.Data ?? new object(),
                            EnqueuedAt = time
                        };

                        await jobQueue.EnqueueAsync(queueItem, ctx.CancellationToken);
                    }
                }

                TriggerDiagnostics(descriptor =>
                {
                    descriptor.FireOn = DiagnosticsOrder.OnSuccess;
                    descriptor.JobId = def.Id;
                    descriptor.JobResult = result;
                });

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Job '{JobId}' failed on attempt {Attempt}", def.Id, attempt);

                var isFinalAttempt = retryPolicy is null || attempt >= retryPolicy.MaxAttempts;

                var attempt1 = attempt;
                TriggerDiagnostics(descriptor =>
                {
                    descriptor.FireOn = DiagnosticsOrder.OnFailure;
                    descriptor.JobId = def.Id;
                    descriptor.Exception = ex;
                    descriptor.Attempt = attempt1;
                });

                if (isFinalAttempt && options.DeadLetterStore is { } dlq)
                {
                    var dead = new DeadLetterItem
                    {
                        JobId = def.Id,
                        FailedAt = DateTimeOffset.UtcNow,
                        Input = ctx.Input ?? "<null input>",
                        ErrorMessage = ex.Message,
                        Exception = ex,
                        RetryAttempt = attempt,
                        MaxAttempts = retryPolicy?.MaxAttempts ?? 1,
                        TriggerId = def.Trigger?.Id,
                        InstanceId = options.InstanceId,
                        Tags = def.Tags?.ToArray(),
                        Id = Guid.NewGuid(),
                        Metadata = ctx.Metadata
                    };

                    await dlq.AddAsync(dead, ctx.CancellationToken);
                }

                if (isFinalAttempt)
                {
                    return JobResult.Failed(ex.Message, ex);
                }

                delay = retryPolicy?.Strategy switch
                {
                    RetryBackoff.Exponential => TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2),
                    _ => delay
                };

                var attempts = attempt;
                var delays = delay;
                TriggerDiagnostics(descriptor =>
                {
                    descriptor.FireOn = DiagnosticsOrder.OnRetry;
                    descriptor.JobId = def.Id;
                    descriptor.Exception = ex;
                    descriptor.Attempt = attempts;
                    descriptor.Delay = delays;
                });

                await Task.Delay(delay, ctx.CancellationToken);
            }

            finally
            {
                _running.TryRemove(def.Id, out _);
                lease?.Dispose();
            }
        }
    }

    private static async ValueTask<JobResult> ExecuteHandler(
        JobExecutionContext context,
        JobDefinition def,
        IServiceProvider sp,
        ILogger logger)
    {
        var handler = sp.GetRequiredService(def.HandlerType);

        var contextType = typeof(JobContext<>).MakeGenericType(def.ContextType);
        var jobContext = Activator.CreateInstance(contextType)!;

        contextType.GetProperty("Input")!.SetValue(jobContext, context.Input);
        contextType.GetProperty("JobId")!.SetValue(jobContext, def.Id);
        contextType.GetProperty("ScheduledTime")!.SetValue(jobContext, context.ScheduledTime);
        contextType.GetProperty("Services")!.SetValue(jobContext, sp);
        contextType.GetProperty("Logger")?.SetValue(jobContext, logger);

        var executeMethod = def.HandlerType.GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.Public)!;

        var resultTask = (ValueTask<JobResult>)executeMethod.Invoke(handler, [jobContext, context.CancellationToken])!;
        return await resultTask;
    }


    private void TriggerDiagnostics(Action<DiagnosticsDescriptor> diagnosticsDescriptor)
    {
        if (!options.EnableDiagnostics)
        {
            return;
        }

        var descriptor = new DiagnosticsDescriptor();
        diagnosticsDescriptor(descriptor);

        switch (descriptor.FireOn)
        {
            case DiagnosticsOrder.OnStart:
                options.Diagnostics.OnJobStart(descriptor.JobId, descriptor.Context!.ScheduledTime);
                break;
            case DiagnosticsOrder.OnSuccess:
                options.Diagnostics.OnJobSuccess(descriptor.JobId, descriptor.JobResult!);
                break;
            case DiagnosticsOrder.OnFailure:
                options.Diagnostics.OnJobFailure(descriptor.JobId, descriptor.Exception!, descriptor.Attempt);
                break;
            case DiagnosticsOrder.OnRetry:
                options.Diagnostics.OnJobRetry(descriptor.JobId, descriptor.Attempt, descriptor.Delay!.Value);
                break;
        }
    }
}