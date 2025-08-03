using Chronux.Core.Jobs.Models;

namespace Chronux.Core.Jobs.Internal.Contracts;

internal interface IChronuxJob
{
    ValueTask<JobResult> ExecuteAsync(object context, IServiceProvider provider, CancellationToken cancellationToken);
}