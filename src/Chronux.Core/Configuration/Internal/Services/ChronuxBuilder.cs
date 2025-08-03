using Chronux.Core.Configuration.Contracts;
using Chronux.Core.Configuration.Internal.Model;
using Chronux.Core.Jobs.Contracts;
using Chronux.Core.Middleware.Contracts;
using Chronux.Core.Scheduling.Contracts;
using Chronux.Core.Scheduling.Internal.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Chronux.Core.Configuration.Internal.Services;

internal sealed class ChronuxBuilder(IServiceCollection services, ChronuxConfigContext context) : IChronuxBuilder
{
    public IServiceCollection Services => services;

    public IJobBuilder AddJob<THandler, TContext>(string id)
        where THandler : class, IJobHandler<TContext>
    {
        services.AddTransient<THandler>();

        var jobBuilder = new JobBuilder<TContext>(id, typeof(THandler), context);
        return jobBuilder;
    }

    public IChronuxBuilder AddJob<THandler, TContext>(
        string id,
        Action<IJobBuilder> configure)
        where THandler : class, IJobHandler<TContext>
    {
        var builder = AddJob<THandler, TContext>(id);
        configure(builder);
        return this;
    }

    public IChronuxBuilder UseMiddleware<T>() where T : IJobMiddleware
    {
        context.AddMiddleware(typeof(T));
        services.AddSingleton(typeof(T));
        return this;
    }
}