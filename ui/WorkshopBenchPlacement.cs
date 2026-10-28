using CuriousContraptions.Gpu;
using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Why a bench placement produced no proposed construction. UnsupportedPart covers every declared refusal to capture
/// the part or to compose it into the current construction (a WorkshopEditRejectedException or a full workbench), not only an
/// unknown part id (for example a lesson that refuses the part's identity). UnplaceablePose is a composed pose the shared placement
/// composition refuses (<see cref="PlacementRefusal"/>: a part whose body at its grid, tube-mouth or rested pose would reach outside
/// the bench volume, or a rested part inside a Static part). Any other exception is a defect, not a rejection.</summary>
public enum BenchPlacementRejection { None, UnsupportedPart, UnplaceablePose }

/// <summary>One bench placement attempt. A rejection carries the unchanged construction and its player-visible message.</summary>
public readonly record struct BenchPlacementProposal(BenchPlacementRejection Rejection, WorkshopConstruction Construction, string Message);

/// <summary>The bench click's placement decision: capture the chosen part and compose the whole proposed construction, then
/// record undo only for a complete proposal.</summary>
public static class WorkshopBenchPlacement
{
    public static BenchPlacementProposal Propose(MachineWorld world, PartId part, GpuBodyId body, Vector3 position, Quaternion rotation,
        Vector3? dimensions, Action pushUndo)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(pushUndo);
        WorkshopConstruction proposed;
        try
        {
            proposed = world.Construction.WithInstance(world.CaptureInstance(part, body, position, rotation, dimensions));
            proposed.Validate();
        }
        catch (Exception error) when (error is WorkshopEditRejectedException)
        {
            // A part that a declared rule refuses to capture or compose is refused here, before any undo entry exists, and
            // logged as a UI rejection on the standard stream. Any other exception is a defect and reaches the handler
            // boundary, which logs it as a fault.
            return new(BenchPlacementRejection.UnsupportedPart, world.Construction, $"Placement rejected: {WorkshopUiFailure.Report(error)}");
        }
        pushUndo();
        return new(BenchPlacementRejection.None, proposed, string.Empty);
    }

    /// <summary>The refusal of a composed pose (F.6 F-1, B1, 10.2a F3), with the message of its placement path: the construction is
    /// unchanged and no undo entry is recorded.</summary>
    public static BenchPlacementProposal Refuse(MachineWorld world, PlacementRefusal refusal)
    {
        ArgumentNullException.ThrowIfNull(world);
        return new(BenchPlacementRejection.UnplaceablePose, world.Construction, refusal switch
        {
            PlacementRefusal.OutsideBench => "Placement rejected: the part would reach outside the bench there.",
            PlacementRefusal.SnapOutsideBench => "Placement rejected: joining that tube mouth would take the part outside the bench.",
            PlacementRefusal.RestOutsideBench => "Placement rejected: resting there would lift the part outside the bench.",
            PlacementRefusal.IntoPart => "Placement rejected: resting there would push the part into another part.",
            _ => throw new ArgumentOutOfRangeException(nameof(refusal), refusal, "Only a refused pose has a refusal message.")
        });
    }
}
