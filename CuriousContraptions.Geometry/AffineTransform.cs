using System;

namespace CuriousContraptions.Geometry;

/// <summary>Complete double basis columns. Scale, shear and reflection are retained;
/// a caller requiring a proper rigid basis must validate that narrower contract.</summary>
public readonly record struct AffineBasis
{
    public CollisionVector X { get; }
    public CollisionVector Y { get; }
    public CollisionVector Z { get; }
    public static AffineBasis Identity => new(new(1, 0, 0), new(0, 1, 0), new(0, 0, 1));
    public AffineBasis(CollisionVector x, CollisionVector y, CollisionVector z)
    {
        if (!x.IsFinite || !y.IsFinite || !z.IsFinite)
            throw new ArgumentException("Basis columns must be finite.");
        X = x; Y = y; Z = z;
    }
    public double Determinant => CollisionVector.Dot(X, CollisionVector.Cross(Y, Z));
    public CollisionVector Apply(CollisionVector value) => X * value.X + Y * value.Y + Z * value.Z;
    public CollisionVector TransposeApply(CollisionVector value) =>
        new(CollisionVector.Dot(X, value), CollisionVector.Dot(Y, value), CollisionVector.Dot(Z, value));
    /// <summary>Declaration acceptance is evaluated on binary32-rounded columns using
    /// binary32 norm, dot and determinant operations. This single predicate applies to
    /// every input, including native doubles; it carries no capture/provenance state.
    /// It preserves the scene declaration boundary, not an exact orthogonality test.
    /// Stored columns and all subsequent geometry operations remain double precision.</summary>
    public bool IsApproximatelyRigid
    {
        get
        {
            var xx = (float)X.X; var xy = (float)X.Y; var xz = (float)X.Z;
            var yx = (float)Y.X; var yy = (float)Y.Y; var yz = (float)Y.Z;
            var zx = (float)Z.X; var zy = (float)Z.Y; var zz = (float)Z.Z;
            if (!float.IsFinite(xx) || !float.IsFinite(xy) || !float.IsFinite(xz) ||
                !float.IsFinite(yx) || !float.IsFinite(yy) || !float.IsFinite(yz) ||
                !float.IsFinite(zx) || !float.IsFinite(zy) || !float.IsFinite(zz)) return false;
            // Keep each operation's precision and grouping explicit: the final
            // comparison widens the binary32 residual to the declared double limit.
            var xNorm = xx * xx + xy * xy + xz * xz;
            var yNorm = yx * yx + yy * yy + yz * yz;
            var zNorm = zx * zx + zy * zy + zz * zz;
            var xyDot = xx * yx + xy * yy + xz * yz;
            var xzDot = xx * zx + xy * zy + xz * zz;
            var yzDot = yx * zx + yy * zy + yz * zz;
            var cofactorX = yy * zz - zy * yz;
            var cofactorY = zy * xz - xy * zz;
            var cofactorZ = xy * yz - yy * xz;
            var determinant = xx * cofactorX + yx * cofactorY + zx * cofactorZ;
            return Math.Abs(xNorm - 1) <= .00001 &&
                Math.Abs(yNorm - 1) <= .00001 && Math.Abs(zNorm - 1) <= .00001 &&
                Math.Abs(xyDot) <= .00001 && Math.Abs(xzDot) <= .00001 &&
                Math.Abs(yzDot) <= .00001 && Math.Abs(determinant - 1) <= .0001;
        }
    }
}

/// <summary>Portable affine value. It never decomposes or normalizes the captured basis.</summary>
public readonly record struct AffineTransform
{
    public AffineBasis Basis { get; }
    public CollisionVector Origin { get; }
    public static AffineTransform Identity => new(AffineBasis.Identity, default);
    public AffineTransform(AffineBasis basis, CollisionVector origin)
    {
        if (!origin.IsFinite) throw new ArgumentException("Origin must be finite.");
        Basis = basis; Origin = origin;
    }
    public CollisionVector TransformPoint(CollisionVector point) => Origin + Basis.Apply(point);
}
