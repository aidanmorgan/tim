using System;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>Player controls the discrete owner hands to one physics step: the enable state of every admitted source, in source order,
/// and the target speed of every compiled revolute drive, in drive-table order. This is where a held run control reaches the engine.</summary>
public sealed record WorkshopTickControls(ElectricalEnable[] SourceEnable)
{
    public RopeSpanId[] Cuts { get; init; } = [];
    public RopeGuideDeclaration[] Guides { get; init; } = [];
    /// <summary>Every compiled drive's target speed (rad/s) for this step, in drive-table order: exactly one per drive.</summary>
    public float[] DriveTarget { get; init; } = [];
    /// <summary>The committed enables and drive targets, with each held run control applied to the lane slot its row drives.</summary>

    public void Validate(int sources, int drives)
    {
        if (SourceEnable is null || SourceEnable.Length != sources || SourceEnable.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Tick controls must name one defined enable state per admitted source.");
        PhysicsTickInputs.ValidateDriveTargets(DriveTarget, drives);
    }
}

/// <summary>Everything one physics step reads besides the committed world: per-source enables, per drive slot (every prismatic
/// record, then every revolute record) whether its compiled supply route is closed this tick, and per compiled revolute drive (in
/// drive-table order) its target speed, which the step commits before its substeps. Data flows one way: committed discrete state →
/// next physics step.</summary>
public sealed record PhysicsTickInputs(ElectricalEnable[] SourceEnable, bool[] SupplyClosed, ElectricalLogicRead[]? Logic = null)
{
    public RopeSpanId[] Cuts { get; init; } = [];
    public RopeGuideDeclaration[] Guides { get; init; } = [];
    /// <summary>Every revolute drive's target speed (rad/s) for this step, in drive-table order: exactly one per driven revolute.</summary>
    public float[] DriveTarget { get; init; } = [];
    public DrivenSurfaceDeclaration[] AdmissionDrivenSurfaces { get; init; } = [];
    public MechanicalTransmissionDeclaration[] AdmissionTransmissions { get; init; } = [];
    public static PhysicsTickInputs Admission { get; } = new([], []);

    /// <summary>Refuses inputs that differ from the admitted tables: one defined enable per source, one supply flag per drive slot
    /// (<paramref name="driven"/>: whether each slot declares a drive), closed only on a slot that declares one, and one admitted target
    /// per revolute drive (<paramref name="drives"/>).</summary>
    public void Validate(int sources, ReadOnlySpan<bool> driven, int drives)
    {
        if (SourceEnable is null || SupplyClosed is null || SourceEnable.Length != sources ||
            SupplyClosed.Length != driven.Length || SourceEnable.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Physics tick inputs differ from the admitted source and drive-slot tables.");
        for (var slot = 0; slot < driven.Length; slot++)
            if (SupplyClosed[slot] && !driven[slot])
                throw new ArgumentException("Physics tick inputs close the supply of a slot that declares no drive.");
        ValidateDriveTargets(DriveTarget, drives);
    }
    /// <summary>Exactly one finite target per drive, each within the angular speed envelope.</summary>
    public static void ValidateDriveTargets(float[]? targets, int drives)
    {
        if (targets is null || targets.Length != drives)
            throw new ArgumentException("Tick inputs must name one admitted target speed per revolute drive.");
        foreach (var target in targets)
            if (!float.IsFinite(target) || MathF.Abs(target) > AngularVelocity.MaximumSpeed)
                throw new ArgumentException("Tick inputs must name one admitted target speed per revolute drive.");
    }
}
