using Chronux.AspNetCore.Models;
using Chronux.Core.Enqueuing.Contracts;
using Chronux.Core.Metrics.Contracts;
using Chronux.Core.Runtime.Execution.Contracts;
using Chronux.Core.Runtime.Status.Contracts;
using Chronux.Core.Scheduling.Internal.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Chronux.AspNetCore.Extension;

public static class ChronuxApiExtensions
{
    public static IEndpointRouteBuilder MapChronuxApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/chronux/jobs/enqueue", async (
            EnqueueJobRequest request,
            IJobEnqueuer enqueuer,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("Chronux.API");

            try
            {
                await enqueuer.EnqueueAsync(
                    request.JobId,
                    request.Input,
                    request.Metadata,
                    ct);

                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to enqueue job");
                return Results.Problem(ex.Message);
            }
        });


        endpoints.MapPost("/chronux/scheduler/stop", async (ITriggerScheduler scheduler, CancellationToken ct) =>
        {
            await scheduler.StopAsync(ct);
            return Results.Ok("Stopped");
        });
        
        endpoints.MapGet("/chronux/scheduler/status", (ITriggerScheduler scheduler) => Results.Ok(new { status = scheduler.GetStatus().ToString() }));

        endpoints.MapPost("/chronux/scheduler/resume", async (ITriggerScheduler scheduler) =>
        {
            await scheduler.ResumeAsync();
            return Results.Ok("Resumed");
        });
        
        endpoints.MapPost("/chronux/jobs/retry/{deadLetterId}", async (
            Guid deadLetterId,
            IJobRequeuer requeuer,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("Chronux.API");

            try
            {
                await requeuer.RequeueFromDeadLetterAsync(deadLetterId, ct);
                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to requeue dead letter job");
                return Results.Problem(ex.Message);
            }
        });

        endpoints.MapGet("/chronux/jobs/status", async (
            IJobStatusProvider statusProvider,
            CancellationToken ct) =>
        {
            var result = await statusProvider.GetAllStatusesAsync(ct);
            return Results.Ok(result);
        });

        endpoints.MapGet("/chronux/jobs/status/{jobId}", async (
            string jobId,
            IJobStatusProvider statusProvider,
            CancellationToken ct) =>
        {
            var result = await statusProvider.GetStatusAsync(jobId, ct);
            return result is null
                ? Results.NotFound()
                : Results.Ok(result);
        });
        
        endpoints.MapGet("/chronux/jobs/metrics", async (
            IExecutionMetricsProvider provider,
            CancellationToken ct) =>
        {
            var result = await provider.GetAllMetricsAsync(ct);
            return Results.Ok(result);
        });

        endpoints.MapGet("/chronux/jobs/metrics/{jobId}", async (
            string jobId,
            IExecutionMetricsProvider provider,
            CancellationToken ct) =>
        {
            var result = await provider.GetMetricsAsync(jobId, ct);
            return result is null
                ? Results.NotFound()
                : Results.Ok(result);
        });
 
        return endpoints;
    }
}
/*
var builder = WebApplication.CreateBuilder(args);
   builder.Services.AddChronux(...);
   
   var app = builder.Build();
   
   app.MapChronuxApi();
   app.Run();
   */
   
   /*
   🧠 When should we make it more advanced?
      Here’s when to consider expanding it:
      
      Feature Need	Change Needed
      🔐 Require auth	Add .RequireAuthorization() support
      🧱 Custom route base (e.g. /api/chronux)	Accept route prefix or group
      🪪 Metadata for Swagger/OpenAPI	Add minimal endpoint description attributes
      🧩 More endpoints (list jobs, view logs)	Extend MapChronuxApi() into grouped mapping
      🛡 Rate-limiting, filters, CORS	Let user pass Action<RouteHandlerBuilder> config
      */