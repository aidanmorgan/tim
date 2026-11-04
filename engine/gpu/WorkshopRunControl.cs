using System;
using System.Collections.Generic;
using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Gpu;

/// <summary>A compiled run-control row's identity within one compiled scene.</summary>
public readonly record struct RunControlId(ulong Value);

/// <summary>Which field of a run-control value carries it (closed set; 0 is undefined).</summary>
public enum RunControlValueKind : byte { Enable = 1, Scalar = 2 }

/// <summary>A typed run-control value: an enable is an <see cref="ElectricalEnable"/> and a scalar an f32, never one encoded as the
/// other. The field its kind does not name is zero.</summary>
public readonly record struct RunControlValue
{
    private RunControlValue(RunControlValueKind kind, ElectricalEnable enable, float scalar)
    {
        Kind = kind; Enable = enable; Scalar = scalar;
    }

    public RunControlValueKind Kind { get; }
    public ElectricalEnable Enable { get; }
    public float Scalar { get; }

    public static RunControlValue Of(ElectricalEnable enable) => new(RunControlValueKind.Enable, enable, 0f);
    public static RunControlValue Of(float scalar) => new(RunControlValueKind.Scalar, ElectricalEnable.Disabled, scalar);

    /// <summary>The value decoded from its three wire fields; false when the kind or enable is undefined, the scalar is not finite, or
    /// the field the kind does not name is not zero.</summary>
    public static bool TryCreate(RunControlValueKind kind, ElectricalEnable enable, float scalar, out RunControlValue value)
    {
        value = new(kind, enable, scalar);
        return value.IsCanonical;
    }

    public bool IsCanonical => Kind switch
    {
        RunControlValueKind.Enable => Enum.IsDefined(Enable) && BitConverter.SingleToUInt32Bits(Scalar) == 0,
        RunControlValueKind.Scalar => float.IsFinite(Scalar) && Enable == ElectricalEnable.Disabled,
        _ => false
    };
}

/// <summary>One compiled run-control row: a declared RunControl parameter of one unlocked instance, the lane slot it drives and the
/// parameter's declared bounds. A SourceEnable row's lane index is its owner's source in compiled source order, and its bounds are zero;
/// a RevoluteDriveTarget row's lane index is its owner's drive in the compiled drive table, and a scalar outside its bounds is refused.</summary>
public readonly record struct RunControlRow(RunControlId Id, GpuBodyId Owner, RunControlLane Lane, int LaneIndex, float Minimum,
    float Maximum);

/// <summary>Explicit player input: one declared parameter edited during Run. The next committed step applies the value to the row's
/// lane; the control never edits the authored construction, so Reset and a save keep the authored value.</summary>
public readonly record struct WorkshopRunControl(GpuBodyId Owner, RunControlId Control, RunControlValue Value)
{
    public bool IsCanonical => Owner.Value != 0 && Control.Value != 0 && Value.IsCanonical;
}

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
    public static bool Resolves(RunControlRow row, PhysicsSceneDeclaration physics)
    {
        ArgumentNullException.ThrowIfNull(physics);
        return row.Lane switch
        {
            RunControlLane.SourceEnable => row.LaneIndex >= 0 && row.LaneIndex < physics.Electrical.Sources.Length &&
                physics.Electrical.Sources[row.LaneIndex].Owner == row.Owner,
            RunControlLane.RevoluteDriveTarget => row.LaneIndex >= 0 && row.LaneIndex < physics.RevoluteDrives.Length &&
                DriveOwner(physics, row.LaneIndex) == row.Owner,
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
    private static GpuBodyId DriveOwner(PhysicsSceneDeclaration physics, int index)
    {
        var joint = physics.RevoluteDrives[index].Joint;
        foreach (var revolute in physics.Revolutes)
            if (revolute.Id == joint) return revolute.BodyA;
        throw new ArgumentException("A compiled drive names no admitted revolute.");
    }

    /// <summary>The lane slot through which <paramref name="owner"/> is driven on <paramref name="lane"/> in the installed physics.</summary>
    public static int Slot(PhysicsSceneDeclaration physics, RunControlLane lane, GpuBodyId owner)
    {
        ArgumentNullException.ThrowIfNull(physics);
        if (lane == RunControlLane.SourceEnable)
            for (var i = 0; i < physics.Electrical.Sources.Length; i++)
                if (physics.Electrical.Sources[i].Owner == owner) return i;
        if (lane == RunControlLane.RevoluteDriveTarget)
            for (var i = 0; i < physics.RevoluteDrives.Length; i++)
                if (DriveOwner(physics, i) == owner) return i;
        throw new ArgumentException("The owner has no slot on this run-control lane.");
    }

    /// <summary>The committed value of <paramref name="owner"/>'s slot on <paramref name="lane"/>, found among the lane's committed
    /// slots. Every read of an installation carries each slot it compiled, locked owners included; a missing slot is refused.</summary>
    public static RunControlValue RunControl(this WorkshopRead read, GpuBodyId owner, RunControlLane lane)
    {
        if (lane == RunControlLane.SourceEnable)
            for (var i = 0; i < read.Electrical.Count; i++)
                if (read.Electrical[i].Owner == owner) return RunControlValue.Of(read.Electrical[i].Enabled);
        if (lane == RunControlLane.RevoluteDriveTarget)
            for (var i = 0; i < read.DriveTargets.Count; i++)
                if (read.DriveTargets[i].Owner == owner) return RunControlValue.Of(read.DriveTargets[i].TargetSpeed);
        throw new ArgumentException("The committed read carries no slot for this owner on this run-control lane.");
    }

    /// <summary>The row through which <paramref name="owner"/> drives <paramref name="lane"/>, or null when the table has none (a locked
    /// owner, or a part that declares no such run control).</summary>
    public static RunControlRow? Find(IEnumerable<RunControlRow> rows, GpuBodyId owner, RunControlLane lane)
    {
        ArgumentNullException.ThrowIfNull(rows);
        foreach (var row in rows)
            if (row.Owner == owner && row.Lane == lane) return row;
        return null;
    }
}
