using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>Immutable authored Workshop composition; parts compile declarations for the shared engine.</summary>
public readonly record struct WorkshopConstruction(ConstructionRevision Revision, WorkshopCadenceSettings Settings, WorkshopInstances Instances, WorkshopPuzzle Puzzle = default, WorkshopConnections Connections = null!)
{
    public WorkshopConstruction WithInstance(IWorkshopInstance instance) =>
        PuzzleCatalogue.WithInstance(this, instance);
    public WorkshopConnections Connections { get; init; } = Connections ?? WorkshopConnections.Empty;
    public WorkshopConstruction WithoutInstance(GpuBodyId id) => this with { Instances = Instances.Without(id), Connections = Connections.Without(id) };
    public static Acceleration Gravity => new(9.81f);
    public static Metres WorkbenchSurface => new(-0.46f);
    public void Validate()
    {
        if (Revision.Value == 0) throw new ArgumentException("Construction revision must be nonzero.");
        Settings.Validate();
        ArgumentNullException.ThrowIfNull(Instances);
        Instances.Validate(); ArgumentNullException.ThrowIfNull(Connections); Connections.Validate(Instances);
        Puzzle.Validate(this);
    }
}

/// <summary>Canonical f32 authoring helpers; external adapters validate and narrow before calling.</summary>
public static class WorkshopInput
{
    public static WorkshopGenericInstance Generic(PartId partId, GpuBodyId id, float x, float y, float z,
        float qx, float qy, float qz, float qw, InstanceParameterCollection? parameters = null)
    {
        var px = Position(x); var py = Position(y); var pz = Position(z);
        var result = new WorkshopGenericInstance(partId, id, new(px.Cell, py.Cell, pz.Cell), new(px.Local, py.Local, pz.Local),
            CanonicalRotation.Normalize(Rotation(qx), Rotation(qy), Rotation(qz), Rotation(qw)))
            { Parameters = parameters ?? InstanceParameterCollection.Defaults(partId) };
        result.Validate(); return result;
    }
    internal static (int Cell, float Local) Position(float metres)
    {
        if (!float.IsFinite(metres) || metres < -64 || metres > 64)
            throw new ArgumentOutOfRangeException(nameof(metres));
        var cells = metres * 16;
        var cell = checked((int)MathF.Floor(cells + 0.5f));
        var local = (float)(cells - cell);
        if (local == .5f) { cell++; local = -.5f; }
        return (cell, local);
    }

    private static float Rotation(float value)
    {
        if (!float.IsFinite(value) || value < -1 || value > 1)
            throw new ArgumentOutOfRangeException(nameof(value));
        return (float)value;
    }
}
