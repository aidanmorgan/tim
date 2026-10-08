using System.Buffers.Binary;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AffineDeclarationTests
{
    public enum ThresholdCase { PreviouslyAccepted, PreviouslyRejected }

    [Theory]
    [InlineData(ThresholdCase.PreviouslyAccepted)]
    [InlineData(ThresholdCase.PreviouslyRejected)]
    public void SceneThresholdNeighborhoodsPreserveAcceptanceAndExactColumns(ThresholdCase scenario)
    {
        var (x, y) = scenario switch
        {
            ThresholdCase.PreviouslyAccepted => (.9778739213943481f, .20917120575904846f),
            ThresholdCase.PreviouslyRejected => (-.7564643025398254f, .6540426015853882f),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        for (var dx = -8; dx <= 8; dx++)
        for (var dy = -8; dy <= 8; dy++)
        {
            var a = Neighbor(x, dx); var b = Neighbor(y, dy);
            var basis = new Basis(new(a, b, 0), new(-b, a, 0), Vector3.Back);
            CheckScene(basis);
            var captured = SceneGeometryAdapter.CaptureBasis(basis);
            foreach (var changedX in new[] { Math.BitDecrement(captured.X.X), captured.X.X, Math.BitIncrement(captured.X.X) })
            {
                var native = new AffineBasis(new(changedX, captured.X.Y, captured.X.Z), captured.Y, captured.Z);
                Assert.Equal(captured.IsApproximatelyRigid, native.IsApproximatelyRigid);
                Assert.Equal(changedX, native.X.X);
                CheckReconstruction(native);
            }
        }
        var exact = new Basis(new(x, y, 0), new(-y, x, 0), Vector3.Back);
        Assert.Equal(scenario == ThresholdCase.PreviouslyAccepted, AcceptedByScene(exact));
    }

    [Fact]
    public void RotatedScaledShearedAndComposedDeclarationsMatchActualGodotPredicate()
    {
        foreach (var axis in new[] { Vector3.Right, Vector3.Up, Vector3.Back, new Vector3(.3f, .6f, .7f).Normalized() })
        for (var i = -48; i <= 48; i++)
        {
            var rotation = new Basis(axis, i * .073f);
            foreach (var scale in new[] { 0f, -1f, .99999f, .999995f, 1f, 1.000005f, 1.00001f, 2f })
            {
                var scaled = new Basis(rotation.X * scale, rotation.Y, rotation.Z);
                CheckScene(scaled);
                CheckScene(rotation * scaled);
                CheckScene(new(scaled.X, scaled.Y + scaled.X * .00001f, scaled.Z));
            }
        }
    }

    [Fact]
    public void NativeDoubleValuesKeepTheirPrecisionThroughAcceptanceCopyAndAffineWireValues()
    {
        var x = Math.BitIncrement(1d);
        var y = Math.BitDecrement(1d);
        var basis = new AffineBasis(new(x, 0, 0), new(0, y, 0), new(0, 0, 1));
        Assert.True(basis.IsApproximatelyRigid);
        CheckReconstruction(basis);
        var shape = new ConvexInstance(new ConvexBox(new(2, 3, 4)), new(basis, new(Math.ScaleB(1, 40) + .25, 0, 0)));
        Assert.Equal(basis, shape.Pose.Basis);
        Assert.Equal(y * 3, shape.Support(new(0, 1, 0)).Y);
        var composed = new AffineBasis(basis.Apply(basis.X), basis.Apply(basis.Y), basis.Apply(basis.Z));
        Assert.True(composed.IsApproximatelyRigid);
        CheckReconstruction(composed);
        foreach (var bad in new[] { double.MaxValue, (double)float.MaxValue, 2d, 0d, -1d })
            Assert.False(new AffineBasis(new(bad, 0, 0), new(0, 1, 0), new(0, 0, 1)).IsApproximatelyRigid);
        Assert.False(default(AffineBasis).IsApproximatelyRigid);
        Assert.Throws<ArgumentException>(() => new AffineBasis(new(double.PositiveInfinity, 0, 0), default, default));
        Assert.Throws<ArgumentException>(() => new AffineBasis(new(double.NaN, 0, 0), default, default));
    }

    private static float Neighbor(float value, int steps)
    {
        while (steps > 0) { value = MathF.BitIncrement(value); steps--; }
        while (steps < 0) { value = MathF.BitDecrement(value); steps++; }
        return value;
    }

    // Independent oracle: actual pinned Godot operations and the pre-extraction predicate.
    private static bool AcceptedByScene(Basis b) =>
        b.X.IsFinite() && b.Y.IsFinite() && b.Z.IsFinite() &&
        Math.Abs(b.X.LengthSquared() - 1) <= .00001 && Math.Abs(b.Y.LengthSquared() - 1) <= .00001 &&
        Math.Abs(b.Z.LengthSquared() - 1) <= .00001 && Math.Abs(b.X.Dot(b.Y)) <= .00001 &&
        Math.Abs(b.X.Dot(b.Z)) <= .00001 && Math.Abs(b.Y.Dot(b.Z)) <= .00001 &&
        Math.Abs(b.Determinant() - 1) <= .0001;

    private static void CheckScene(Basis scene)
    {
        var affine = SceneGeometryAdapter.CaptureAffine(new(scene, Vector3.Zero));
        var expected = AcceptedByScene(scene);
        Assert.Equal(expected, affine.Basis.IsApproximatelyRigid);
        if (expected)
        {
            var instance = new ConvexInstance(new ConvexSphere(1), affine);
            Assert.Equal(affine, instance.Pose);
        }
        else Assert.Throws<ArgumentException>(() => new ConvexInstance(new ConvexSphere(1), affine));
        Assert.Equal(new CollisionVector(scene.X.X, scene.X.Y, scene.X.Z), affine.Basis.X);
        Assert.Equal(new CollisionVector(scene.Y.X, scene.Y.Y, scene.Y.Z), affine.Basis.Y);
        Assert.Equal(new CollisionVector(scene.Z.X, scene.Z.Y, scene.Z.Z), affine.Basis.Z);
    }

    private static void CheckReconstruction(AffineBasis basis)
    {
        // P0-005's 96-byte Affine value grammar: no unencoded validation tag.
        // This is a grammar-value proof, not a claim that the future bridge codec is implemented.
        double[] values = [basis.X.X, basis.X.Y, basis.X.Z, basis.Y.X, basis.Y.Y, basis.Y.Z,
            basis.Z.X, basis.Z.Y, basis.Z.Z, Math.ScaleB(1, 40) + .25, -2, 3];
        Span<byte> wire = stackalloc byte[96];
        for (var i = 0; i < values.Length; i++) BinaryPrimitives.WriteDoubleLittleEndian(wire[(i * 8)..], values[i]);
        for (var i = 0; i < values.Length; i++) values[i] = BinaryPrimitives.ReadDoubleLittleEndian(wire[(i * 8)..]);
        var restored = new AffineTransform(new(new(values[0], values[1], values[2]),
            new(values[3], values[4], values[5]), new(values[6], values[7], values[8])),
            new(values[9], values[10], values[11]));
        Assert.Equal(basis, restored.Basis);
        Assert.Equal(basis.IsApproximatelyRigid, restored.Basis.IsApproximatelyRigid);
        Assert.Equal(Math.ScaleB(1, 40) + .25, restored.Origin.X);
    }
}
