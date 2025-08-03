using System.Threading.Channels;
using Chronux.Core.Configuration.Contracts;
using Chronux.Core.Configuration.Internal.Model;
using Chronux.Core.Configuration.Internal.Services;
using Chronux.Core.Configuration.Models;
using Chronux.Core.Enqueuing.Contracts;
using Chronux.Core.Enqueuing.Internal;
using Chronux.Core.Execution.Contracts;
using Chronux.Core.Execution.Internal.Contracts;
using Chronux.Core.Execution.Internal.Services;
using Chronux.Core.Metrics.Contracts;
using Chronux.Core.Metrics.Internal.Services;
using Chronux.Core.Middleware.Contracts;
using Chronux.Core.Runtime.Execution.Contracts;
using Chronux.Core.Runtime.Execution.Internal.Services;
using Chronux.Core.Runtime.Status.Contracts;
using Chronux.Core.Runtime.Status.Internal.Services;
using Chronux.Core.Scheduling.Internal.Contracts;
using Chronux.Core.Scheduling.Internal.Services;
using Chronux.Core.Serialization.Contracts;
using Chronux.Core.Serialization.Internal.Factories;
using Chronux.Core.Serialization.Internal.Services;
using Chronux.Core.Storage.Contracts;
using Chronux.Core.Storage.Internal;
using Chronux.Core.Storage.Models;
using Chronux.Core.Storage.providers;
using Chronux.Core.Storage.Services;
using Chronux.Core.Validation.Contracts;
using Chronux.Core.Validation.Internal.Services;
using Chronux.Core.Workers.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Chronux.Core.Configuration.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChronux(
        this IServiceCollection services,
        Action<IChronuxBuilder> configure,
        Action<ChronuxOptions>? configureOptions,
        bool includeScheduler = true)
    {
        var config = new ChronuxConfigContext();
        configureOptions?.Invoke(config.Options);

        var builder = new ChronuxBuilder(services, config);
        configure(builder);

        RegisterCoreServices(services, config, includeScheduler);
        return services;
    }

    public static IServiceCollection AddChronux(
        this IServiceCollection services,
        Action<IChronuxBuilder> configure,
        bool includeScheduler = true)
        => services.AddChronux(configure, null, includeScheduler);

    private static void RegisterCoreServices(
        IServiceCollection services,
        ChronuxConfigContext config,
        bool includeScheduler)
    {
        services.AddSingleton<IJobRegistry>(sp =>
        {
            var reg = new JobRegistry();
            foreach (var job in config.Jobs)
                reg.Register(job);
            return reg;
        });

        services.AddSingleton<IJobExecutor>(sp =>
        {
            var types = config.MiddlewareTypes;
            return new JobExecutor(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILoggerFactory>(),
                config.Options.DistributedLockProvider
                ?? new InMemoryLockProvider(),
                sp.GetRequiredService<IChronuxStorageProvider>(),
                types,
                sp.GetRequiredService<IJobQueueStore>(),
                sp,
                config.Options
            );
        });

        services.AddSingleton<IJobDispatcher, JobDispatcher>();

        if (includeScheduler && config.Options.AutoStartScheduler)
        {
            services.AddSingleton<ITriggerScheduler>(sp =>
                new TriggerScheduler(
                    sp.GetRequiredService<IJobRegistry>(),
                    sp.GetRequiredService<IJobDispatcher>(),
                    sp.GetRequiredService<IChronuxStorageProvider>(),
                    sp.GetRequiredService<ILogger<TriggerScheduler>>(),
                    config.Options.TimeProvider,
                    config.Options.TriggerPollInterval,
                    config.Options.TimeZone
                )
            );
            services.AddHostedService<ChronuxHostedService>();
        }

        services.AddSingleton<IChronuxValidator>(sp =>
            new ChronuxValidator(config)
        );

        services.AddSingleton<IChronuxStorageProvider>(sp =>
            config.Options.StorageProvider ?? new InMemoryStorageProvider()
        );
        services.TryAddSingleton<IJobQueueStore, InMemoryJobQueueStore>();
        services.TryAddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();

        var jobQueue = Channel.CreateUnbounded<EnqueuedJob>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        services.AddSingleton(jobQueue);
        services.AddSingleton<IJobEnqueuer, InMemoryJobEnqueuer>();
        services.AddHostedService<JobQueueWorker>();

        services.AddHostedService<RetentionWorker>();
        services.TryAddSingleton<IChronuxSerializer>(sp =>
        {
            var options = sp.GetRequiredService<ChronuxOptions>();

            return options.SerializerInstance ?? ChronuxSerializerFactory.Create(options.Serializer);
        });
        services.TryAddSingleton<IJobRequeuer, JobRequeuer>();
        services.TryAddSingleton<IJobStatusProvider, JobStatusProvider>();
        services.TryAddSingleton<IExecutionMetricsProvider, ExecutionMetricsProvider>();
    }

    /*public static IServiceCollection AddChronux(this IServiceCollection services, Action<IChronuxBuilder> configure, bool includeScheduler = true)
    {
        var config = new ChronuxConfigContext();
        var builder = new ChronuxBuilder(services, config);
        configure(builder);

        // Register core
        services.AddSingleton<IJobRegistry>(sp =>
        {
            var reg = new JobRegistry();
            foreach (var job in config.Jobs)
                reg.Register(job);
            return reg;
        });

        services.AddSingleton<IJobExecutor>(sp =>
        {
            var types = config.MiddlewareTypes;
            return new JobExecutor(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<IDistributedLockProvider>(),
                types,
                sp
            );
        });

        services.AddSingleton<IJobDispatcher, JobDispatcher>();
        services.AddSingleton<IDistributedLockProvider, InMemoryLockProvider>();

        if (includeScheduler)
        {
            services.AddSingleton<ITriggerScheduler, TriggerScheduler>();
            services.AddHostedService<ChronuxHostedService>();
        }

        return services;
    }*/
}