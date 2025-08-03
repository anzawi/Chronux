using Chronux.Core.Validation.Models;

namespace Chronux.Core.Validation.Contracts;

public interface IChronuxValidator
{
    IReadOnlyList<JobValidationError> Validate();
    bool IsValid() => Validate().Count == 0;
}