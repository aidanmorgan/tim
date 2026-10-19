using System;

namespace CuriousContraptions.Gpu;

/// <summary>A fault boundary that caught an exception it cannot handle more specifically.</summary>
public enum FaultSite
{
    Initialize = 1, Construct, Advance, Reset, Startup, LateDispose, RunLoop, StaleRunLoop, Warmup, Presentation,
    Disposal, TraceFlush, Installation, UiHandler
}

/// <summary>
/// One diagnostic channel for exceptions caught at a boundary. A <see cref="Fault"/> is an unexpected failure that
/// faulted or retired something: it is written whole (type, message and stack) to the error stream, which browser
/// suites treat as a failure. A <see cref="Rejection"/> is an exception the Workshop UI reported to the player (an
/// edit or command it refused): it is written whole to the standard stream, because an ordinary refused edit is
/// not a fault. Both writes are synchronous, so a boundary records its state change before any continuation runs.
/// </summary>
public static class WorkshopDiagnostics
{
    public static void Fault(FaultSite site, Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Console.Error.WriteLine($"CCGPU_{Name(site)}_EXCEPTION {error}");
    }

    public static void Rejection(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Console.WriteLine($"CCGPU_UI_REJECTION {error}");
    }

    // Closed discriminant to its console record name (external text boundary).
    private static string Name(FaultSite site) => site switch
    {
        FaultSite.Initialize => "INITIALIZE",
        FaultSite.Construct => "CONSTRUCT",
        FaultSite.Advance => "ADVANCE",
        FaultSite.Reset => "RESET",
        FaultSite.Startup => "STARTUP",
        FaultSite.LateDispose => "LATE_DISPOSE",
        FaultSite.RunLoop => "RUN_LOOP",
        FaultSite.StaleRunLoop => "STALE_RUN_LOOP",
        FaultSite.Warmup => "WARMUP",
        FaultSite.Presentation => "PRESENTATION",
        FaultSite.Disposal => "DISPOSAL",
        FaultSite.TraceFlush => "TRACE_FLUSH",
        FaultSite.Installation => "INSTALLATION",
        FaultSite.UiHandler => "UI_HANDLER",
        _ => throw new ArgumentOutOfRangeException(nameof(site), site, "Undefined fault site.")
    };
}
