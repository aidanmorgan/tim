using System;
using CuriousContraptions.Gpu;

namespace CuriousContraptions;

/// <summary>
/// How a Workshop UI handler reports an exception it caught. A declared refusal is an ordinary outcome: it is logged
/// as a typed UI rejection and its message is shown. Anything else is a defect: it is logged whole
/// as a typed fault and the status line says it was an
/// unexpected fault.
/// </summary>
public static class WorkshopUiFailure
{
    /// <summary>The declared refusals, each a sealed type: a rule refusing the edit (WorkshopEditRejectedException, with
    /// its WorkshopEditRejection reason), an
    /// indeterminate transport, and a stored save its pre-validation refused (WorkshopSaveRejectedException, with its
    /// WorkshopSaveRejection reason). Every other exception is a defect, ArgumentException included: a programmer's
    /// argument guard, a framework failure such as a duplicate dictionary key, ArgumentOutOfRangeException,
    /// InvalidOperationException and the rest.</summary>
    public static bool IsRejection(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error is WorkshopEditRejectedException or WorkshopTransportException or WorkshopSaveRejectedException;
    }

    /// <summary>Records the failure on its channel and returns the status-line text to show the player.</summary>
    public static string Report(Exception error, WorkshopDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(error);
        if (IsRejection(error))
        {
            diagnostics.Rejection(error);
            return error.Message;
        }
        diagnostics.Fault(FaultSite.UiHandler, error);
        return $"Unexpected fault ({error.GetType().Name}): {error.Message} It has been logged.";
    }
}
