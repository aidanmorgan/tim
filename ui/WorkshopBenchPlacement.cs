using CuriousContraptions.Gpu;
using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Why a bench placement produced no proposed construction. UnsupportedPart covers every declared refusal to capture
/// the part or to compose it into the current construction (a WorkshopEditRejectedException or a full workbench), not only an
/// unknown part id (for example a lesson that refuses the part's identity). Any other exception is a defect, not a
/// rejection.</summary>
public enum BenchPlacementRejection { None, UnsupportedPart }

/// <summary>One bench placement attempt. A rejection carries the unchanged construction and its player-visible message.</summary>
public readonly record struct BenchPlacementProposal(BenchPlacementRejection Rejection, WorkshopConstruction Construction, string Message);

/// <summary>The bench click's placement decision: capture the chosen part and compose the whole proposed construction, then
/// record undo only for a complete proposal.</summary>
public static class WorkshopBenchPlacement
{
    public static BenchPlacementProposal Propose(MachineWorld world, PartId part, GpuBodyId body, Vector3 position, Quaternion rotation,
        RampDimensions? rampDimensions, WallDimensions? wallDimensions, PipeLength? pipeLength, Action pushUndo)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(pushUndo);
        WorkshopConstruction proposed;
        try { proposed = world.Construction.WithInstance(world.CaptureInstance(part, body, position, rotation, rampDimensions, wallDimensions, pipeLength)); }
        catch (Exception error) when (error is WorkshopEditRejectedException or WorkbenchFullException)
        {
            // A part that a declared rule refuses to capture or compose is refused here, before any undo entry exists, and
            // logged as a UI rejection on the standard stream. Any other exception is a defect and reaches the handler
            // boundary, which logs it as a fault.
            return new(BenchPlacementRejection.UnsupportedPart, world.Construction, $"Placement rejected: {WorkshopUiFailure.Report(error)}");
        }
        pushUndo();
        return new(BenchPlacementRejection.None, proposed, string.Empty);
    }
}
