using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>The lane rules every run-control consumer shares: the compiled table, the simulation, the tick controls and the committed
/// read. Each lane names its value kind and the committed slot it drives.</summary>
public static class RunControlLanes
{
    /// <summary>The value kind <paramref name="lane"/> takes.</summary>
    public static RunControlValueKind ValueKind(RunControlLane lane) => lane switch
    {
        RunControlLane.SourceEnable => RunControlValueKind.Enable,
        RunControlLane.RevoluteDriveTarget => RunControlValueKind.Scalar,
        _ => throw new ArgumentOutOfRangeException(nameof(lane), lane, "Undefined run-control lane.")
    };

    /// <summary>Whether the row's lane slot exists in the installed physics and belongs to the row's owner.</summary>
    public static bool Resolves(RunControlRow row, IPhysicsSceneDeclaration physics)
    {
        ArgumentNullException.ThrowIfNull(physics);
        return row.Lane switch
        {
            RunControlLane.SourceEnable => row.LaneIndex >= 0 && row.LaneIndex < physics.Electrical.Sources.Length &&
                physics.Electrical.Sources[row.LaneIndex].Owner == row.Owner && physics.Electrical.Sources[row.LaneIndex].Id == row.Source,
            RunControlLane.RevoluteDriveTarget => row.LaneIndex >= 0 && row.LaneIndex < physics.RevoluteDrives.Length &&
                DriveOwner(physics, row.LaneIndex) == row.Owner && row.Source == default,
            _ => false
        };
    }

    /// <summary>Whether a row's declared bounds are its lane's: zero on an Enable lane, finite and ordered within the angular speed
    /// envelope on a RevoluteDriveTarget lane.</summary>
    public static bool BoundsAdmitted(RunControlRow row) => row.Lane switch
    {
        RunControlLane.SourceEnable => row.Minimum == 0f && row.Maximum == 0f,
        RunControlLane.RevoluteDriveTarget => float.IsFinite(row.Minimum) && float.IsFinite(row.Maximum) && row.Minimum <= row.Maximum &&
            row.Minimum >= -AngularVelocity.MaximumSpeed && row.Maximum <= AngularVelocity.MaximumSpeed,
        _ => false
    };

    /// <summary>Whether <paramref name="value"/> lies within the row's declared bounds; an enable has none.</summary>
    public static bool WithinBounds(RunControlRow row, RunControlValue value) =>
        value.Kind != RunControlValueKind.Scalar || (value.Scalar >= row.Minimum && value.Scalar <= row.Maximum);

    /// <summary>The owner of drive <paramref name="index"/> in the compiled drive table: the base body of the hinge it drives.</summary>
    private static GpuBodyId DriveOwner(IPhysicsSceneDeclaration physics, int index)
    {
        var joint = physics.RevoluteDrives[index].Joint;
        foreach (var revolute in physics.Revolutes)
            if (revolute.Id == joint) return revolute.BodyA;
        throw new ArgumentException("A compiled drive names no admitted revolute.");
    }

    /// <summary>The lane slot through which <paramref name="owner"/> is driven on <paramref name="lane"/> in the installed physics.</summary>
    public static int Slot(IPhysicsSceneDeclaration physics, RunControlLane lane, GpuBodyId owner, ElectricalSourceId source = default)
    {
        ArgumentNullException.ThrowIfNull(physics);
        if (lane == RunControlLane.SourceEnable)
        {
            if (source == default) throw new ArgumentException("A source enable control requires an exact source identity.", nameof(source));
            for (var i = 0; i < physics.Electrical.Sources.Length; i++)
                if (physics.Electrical.Sources[i].Owner == owner && physics.Electrical.Sources[i].Id == source) return i;
        }
        if (lane == RunControlLane.RevoluteDriveTarget)
            for (var i = 0; i < physics.RevoluteDrives.Length; i++)
                if (DriveOwner(physics, i) == owner) return i;
        throw new ArgumentException("The owner has no slot on this run-control lane.");
    }

    /// <summary>The committed value of <paramref name="owner"/>'s slot on <paramref name="lane"/>, found among the lane's committed
    /// slots. Every read of an installation carries each slot it compiled, locked owners included; a missing slot is refused.</summary>
    public static RunControlValue RunControl(this WorkshopRead read, GpuBodyId owner, RunControlLane lane, ElectricalSourceId source = default)
    {
        if (lane == RunControlLane.SourceEnable)
        {
            if (source == default) throw new ArgumentException("A source enable control requires an exact source identity.", nameof(source));
            for (var i = 0; i < read.Electrical.Count; i++)
                if (read.Electrical[i].Owner == owner && read.Electrical[i].Id == source) return RunControlValue.Of(read.Electrical[i].Enabled);
        }
        if (lane == RunControlLane.RevoluteDriveTarget)
            for (var i = 0; i < read.DriveTargets.Count; i++)
                if (read.DriveTargets[i].Owner == owner) return RunControlValue.Of(read.DriveTargets[i].TargetSpeed);
        throw new ArgumentException("The committed read carries no slot for this owner on this run-control lane.");
    }

    /// <summary>The row through which <paramref name="owner"/> drives <paramref name="lane"/>, or null when the table has none (a locked
    /// owner, or a part that declares no such run control).</summary>
    public static RunControlRow? Find(IEnumerable<RunControlRow> rows, GpuBodyId owner, RunControlLane lane, ElectricalSourceId source = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (lane == RunControlLane.SourceEnable && source == default)
            throw new ArgumentException("A source enable control requires an exact source identity.", nameof(source));
        foreach (var row in rows)
            if (row.Owner == owner && row.Lane == lane && (lane != RunControlLane.SourceEnable || row.Source == source)) return row;
        return null;
    }
}
