using Chronux.Core.Jobs.Contracts;
using Chronux.Core.Middleware.Contracts;
using Chronux.Core.Scheduling.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Chronux.Core.Configuration.Contracts;

public interface IChronuxBuilder
{
    IServiceCollection Services { get; }
    IJobBuilder AddJob<THandler, TContext>(string id)
        where THandler : class, IJobHandler<TContext>;

    IChronuxBuilder AddJob<THandler, TContext>(
        string id,
        Action<IJobBuilder> configure)
        where THandler : class, IJobHandler<TContext>;
    IChronuxBuilder UseMiddleware<T>() where T : IJobMiddleware;
}