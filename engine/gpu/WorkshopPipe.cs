using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>CAT-048 §2: the straight pipe's declared f32 length along local X, 1–8 m (default 3.6). The bore (0.65 m) and the
/// collar half width (0.09 m) are fixed; the shell spans ±length/2 and the mouths sit at the collar faces, ±(length/2 + 0.09).</summary>
public readonly record struct PipeLength(float Metres)
{
    public const float Minimum = 1f;
    public const float Maximum = 8f;
    public static PipeLength Default => new(3.6f);
    /// <summary>The collar half width: a mouth sits this far beyond the shell end.</summary>
    public const float CollarHalfWidth = .09f;
    /// <summary>The collar outer radius (CAT-048 §2 art).</summary>
    public const float CollarRadius = .78f;

    public float HalfLength => Metres * .5f;
    /// <summary>The mouth's distance from the centre along local X.</summary>
    public float MouthOffset => HalfLength + CollarHalfWidth;
    /// <summary>CAT-048 §2 art: √((length/2 + 0.09)² + 0.78²).</summary>
    public float PickRadius => MathF.Sqrt(MouthOffset * MouthOffset + CollarRadius * CollarRadius);

    public void Validate()
    {
        if (!float.IsFinite(Metres) || Metres < Minimum || Metres > Maximum)
            throw new ArgumentException($"Pipe length {Metres} m is outside its admitted 1–8 m domain.");
    }

    /// <summary>Named pointer-input boundary: a finite drag clamps to 1–8 m; a non-finite one is rejected.</summary>
    public static PipeLength FromInput(float metres)
    {
        if (!float.IsFinite(metres)) throw new ArgumentException("Nonfinite pipe length.");
        var result = new PipeLength(Math.Clamp(metres, Minimum, Maximum));
        result.Validate(); return result;
    }
}

/// <summary>The straight pipe: a typed instance whose 24 compound colliders are generated from its length at compile time.</summary>
public readonly record struct WorkshopPipe(GpuBodyId Id, CellOrigin Cell, LocalPosition Local,
    CanonicalRotation Rotation, PipeLength Length, bool Locked = false) : IWorkshopDeclaredColliders
{
    public PartId PartId => PartId.Pipe;
    public CosmeticCurveDeclaration Cosmetic => CosmeticCurveDeclaration.For(PartId);
    public IReadOnlyList<ExtractedCollider> DeclaredColliders => DeclaredPhysics.Colliders(PartId, DeclaredPhysics.Parameters(this));
    public void Validate()
    {
        new CanonicalBody(Id, 0, 0, Cell, Local, default).Validate();
        Rotation.Validate(); Length.Validate();
    }
}
