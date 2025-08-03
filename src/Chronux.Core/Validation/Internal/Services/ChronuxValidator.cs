using Chronux.Core.Configuration.Internal.Model;
using Chronux.Core.Validation.Contracts;
using Chronux.Core.Validation.Models;

namespace Chronux.Core.Validation.Internal.Services;

internal sealed class ChronuxValidator(ChronuxConfigContext config) : IChronuxValidator
{
    public IReadOnlyList<JobValidationError> Validate()
    {
        var errors = new List<JobValidationError>();
        var seenIds = new HashSet<string>();

        foreach (var job in config.Jobs)
        {
            if (string.IsNullOrWhiteSpace(job.Id))
            {
                errors.Add(new JobValidationError { JobId = "?", Message = "Job ID is empty" });
                continue;
            }

            if (!seenIds.Add(job.Id))
            {
                errors.Add(new() { JobId = job.Id, Message = "Duplicate Job ID" });
            }

            if (job.OnSuccess is not null)
            {
                errors.AddRange(from id in job.OnSuccess
                    where config.Jobs.All(j => j.Id != id)
                    select new JobValidationError() { JobId = job.Id, Message = $"Chained job '{id}' not registered" });
            }

            if (job.OnFailure is not null)
            {
                errors.AddRange(from id in job.OnFailure
                    where config.Jobs.All(j => j.Id != id)
                    select new JobValidationError
                        { JobId = job.Id, Message = $"Failure-chained job '{id}' not registered" });
            }

            if (job is { Trigger: not null, Input: null } && !HasDefaultCtor(job.ContextType!))
                errors.Add(new JobValidationError
                {
                    JobId = job.Id,
                    Message =
                        $"Job requires input but none provided, and context type '{job.ContextType.Name}' has no default constructor."
                });
        }
        /*if (sp.GetRequiredService<IChronuxStorageProvider>() is InMemoryStorageProvider)
{
    logger.LogWarning("Chronux is using InMemoryStorageProvider. Not recommended for production.");
}
         */

        return errors;
    }

    private static bool HasDefaultCtor(Type t) =>
        t.GetConstructor(Type.EmptyTypes) != null;
}