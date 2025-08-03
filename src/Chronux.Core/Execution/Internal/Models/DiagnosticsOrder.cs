namespace Chronux.Core.Execution.Internal.Models;

internal enum DiagnosticsOrder
{
    OnStart,
    OnEnd,
    OnSuccess,
    OnFailure,
    OnException,
    OnCancel,
    OnTimeout,
    OnRetry,
    OnRetryFailure,
    OnRetryException,
    OnJobChained
}