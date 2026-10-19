using CuriousContraptions.Gpu;
using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Why a bench placement produced no proposed construction. UnsupportedPart covers any failure to capture the part or
/// to compose it into the current construction, not only an unknown part id (for example a lesson that refuses the part's
/// identity).</summary>
public enum BenchPlacementRejection { None, UnsupportedPart }

/// <summary>One bench placement attempt. A rejection carries the unchanged construction and its player-visible message.</summary>
public readonly record struct BenchPlacementProposal(BenchPlacementRejection Rejection, WorkshopConstruction Construction, string Message);

/// <summary>The bench click's placement decision: capture the chosen part and compose the whole proposed construction, then
/// record undo only for a complete proposal.</summary>
public static class WorkshopBenchPlacement
{
    public static BenchPlacementProposal Propose(MachineWorld world, PartId part, GpuBodyId body, Vector3 position, Quaternion rotation,
        RampDimensions? rampDimensions, WallDimensions? wallDimensions, Action pushUndo)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(pushUndo);
        WorkshopConstruction proposed;
        try { proposed = world.Construction.WithInstance(world.CaptureInstance(part, body, position, rotation, rampDimensions, wallDimensions)); }
        catch (Exception error)
        {
            // A part that cannot be captured or composed is refused here, before any undo entry exists.
            return new(BenchPlacementRejection.UnsupportedPart, world.Construction, $"Placement rejected: {error.Message}");
        }
        pushUndo();
        return new(BenchPlacementRejection.None, proposed, string.Empty);
    }
}
