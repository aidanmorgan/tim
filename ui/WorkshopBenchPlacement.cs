using CuriousContraptions.Gpu;
using Godot;
using System;

namespace CuriousContraptions;

/// <summary>Why a bench placement produced no proposed construction. UnsupportedPart covers any ArgumentException from
/// MachineWorld.CaptureInstance, not only an unknown part id (for example a ball resource whose material bits are invalid).</summary>
public enum BenchPlacementRejection { None, UnsupportedPart }

/// <summary>One bench placement attempt. A rejection carries the unchanged construction and its player-visible message.</summary>
public readonly record struct BenchPlacementProposal(BenchPlacementRejection Rejection, WorkshopConstruction Construction, string Message);

/// <summary>The bench click's placement decision: capture the chosen part, then record undo only for a captured part.</summary>
public static class WorkshopBenchPlacement
{
    public static BenchPlacementProposal Propose(MachineWorld world, PartId part, GpuBodyId body, Vector3 position, Quaternion rotation,
        RampDimensions? rampDimensions, WallDimensions? wallDimensions, Action pushUndo)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(pushUndo);
        IWorkshopInstance instance;
        try { instance = world.CaptureInstance(part, body, position, rotation, rampDimensions, wallDimensions); }
        catch (ArgumentException error)
        {
            // A part without a capture path is refused here, before any undo entry exists.
            return new(BenchPlacementRejection.UnsupportedPart, world.Construction, $"Placement rejected: {error.Message}");
        }
        pushUndo();
        return new(BenchPlacementRejection.None, world.Construction.WithInstance(instance), string.Empty);
    }
}
