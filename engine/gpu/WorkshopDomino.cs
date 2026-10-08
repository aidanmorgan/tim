using System;

namespace CuriousContraptions.Gpu;

/// <summary>The immutable admitted Domino tile declaration: one homogeneous dynamic box and its contact material.
/// The body origin is the box centre; the shared solver derives inertia from these extents and mass.</summary>
public readonly record struct DominoMaterial(MetreVector HalfExtents, Kilograms Mass, Restitution Bounce, FrictionCoefficient Friction)
{
    public static DominoMaterial Default => new(new((Half).125, (Half).55, (Half).325), new((Half).4), new((Half).05), new((Half).6));
    public static LinearSpeed BounceThreshold => new((Half).1);
    public void Validate()
    {
        var expected = Default;
        if (!HalfBits.Equal(HalfExtents, expected.HalfExtents) || !HalfBits.Equal(Mass.Value, expected.Mass.Value) ||
            !HalfBits.Equal(Bounce.Value, expected.Bounce.Value) || !HalfBits.Equal(Friction.Value, expected.Friction.Value))
            throw new ArgumentException("Only the default Domino material is admitted.");
    }
}

/// <summary>Passive finite-mass tile. It declares geometry, mass, material and its orientation-threshold activation; standing,
/// toppling and the threshold evaluation belong to the shared solver. The sensor and its ActivationOut node compile only while the tile is wired.</summary>
public readonly record struct WorkshopDomino(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, DominoMaterial Material, bool Locked = false) : IWorkshopInstance
{
    /// <summary>ActivationOut emits once the tile has turned 45° from its admitted pose (CAT-023).</summary>
    public static OrientationThreshold ActivationThreshold => OrientationThreshold.Degrees(45);
    public WorkshopPartKind Kind => WorkshopPartKind.Domino;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.None;
    public void Validate()
    {
        Material.Validate();
        Rotation.Validate();
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
    }
}
