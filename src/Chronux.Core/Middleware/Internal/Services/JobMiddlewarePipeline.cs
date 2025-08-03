using Chronux.Core.Middleware.Contracts;
using Chronux.Core.Middleware.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Chronux.Core.Middleware.Internal.Services;

internal sealed class JobMiddlewarePipeline(IEnumerable<Type> middlewareTypes)
{
    private readonly List<Type> _pipeline = middlewareTypes.ToList();

    public JobExecutionDelegate Build(IServiceProvider sp, JobExecutionDelegate terminal)
    {
        JobExecutionDelegate next = terminal;

        foreach (var middlewareType in _pipeline.AsEnumerable().Reverse())
        {
            var current = middlewareType;
            next = Wrap(sp, current, next);
        }

        return next;
    }

    private static JobExecutionDelegate Wrap(IServiceProvider sp, Type middlewareType, JobExecutionDelegate next)
    {
        var instance = (IJobMiddleware)sp.GetRequiredService(middlewareType);
        return ctx => instance.InvokeAsync(ctx, next);
    }
}