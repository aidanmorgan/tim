using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class RigidMassPropertiesTests
{
    [Theory]
    [InlineData(ColliderShapeKind.Sphere, 1.0 / 1024, 1.0 / 16, -19)]
    [InlineData(ColliderShapeKind.Sphere, 1024, 16, 17)]
    [InlineData(ColliderShapeKind.Box, 1.0 / 1024, 1.0 / 1024, -30)]
    [InlineData(ColliderShapeKind.Box, 1024, 16, 18)]
    public void ExtremeMomentsRetainFiniteHalfMantissas(ColliderShapeKind shape, double mass, double size, int exponent)
    {
        var properties = RigidMassProperties.Compile(Body(mass), Collider(shape, size, size, size));
        var expected = shape == ColliderShapeKind.Sphere ? .4 * mass * size * size : mass * 2 * size * size / 3;
        Assert.Equal(exponent, properties.X.Exponent);
        AssertMoment(properties.X, expected);
        Assert.Equal(properties.X, properties.Y);
        Assert.Equal(properties.X, properties.Z);
    }

    [Fact]
    public void ThinBoxDoesNotLoseItsSmallPrincipalMoment()
    {
        const double small = 1.0 / 1024;
        var properties = RigidMassProperties.Compile(Body(1), Collider(ColliderShapeKind.Box, small, 16, small));
        AssertMoment(properties.X, (256 + small * small) / 3);
        AssertMoment(properties.Y, 2 * small * small / 3);
        AssertMoment(properties.Z, (256 + small * small) / 3);
        Assert.True(properties.X.Exponent - properties.Y.Exponent >= 26);
    }

    [Fact]
    public void CentreAndPrincipalFrameRemainExactDeclaredGeometry()
    {
        var pose = new RigidLocalPose(new((Half)1, (Half)(-2), (Half).5),
            new((Half)0, (Half)0, (Half).7071067811865476, (Half).7071067811865476));
        var collider = Collider(ColliderShapeKind.Box, 1, 2, 3) with { Pose = pose };
        var properties = RigidMassProperties.Compile(Body(2), collider);
        Assert.Equal(pose.Translation, properties.LocalCentreOfMass);
        Assert.Equal(pose.Rotation, properties.PrincipalFrame);
        AssertMoment(properties.X, 2.0 * (4 + 9) / 3);
        AssertMoment(properties.Y, 2.0 * (1 + 9) / 3);
        AssertMoment(properties.Z, 2.0 * (1 + 4) / 3);
    }

    [Theory]
    [InlineData(ColliderShapeKind.Sphere)]
    [InlineData(ColliderShapeKind.Box)]
    public void NewExtentBoundaryRejectsOverflow(ColliderShapeKind shape)
    {
        Collider(shape, 16, 16, 16).Validate();
        Assert.Throws<ArgumentException>(() => Collider(shape, 17, 17, 17).Validate());
    }

    [Fact]
    public void DynamicPlaneAndForeignBodyHaveNoMassCompilation()
    {
        Assert.Throws<ArgumentException>(() => RigidMassProperties.Compile(Body(1),
            new(new(2), new(1), new(3), ColliderShapeKind.Plane, RigidLocalPose.Identity, default, default)));
        Assert.Throws<ArgumentException>(() => RigidMassProperties.Compile(Body(1),
            Collider(ColliderShapeKind.Sphere, 1, 1, 1) with { Body = new(9) }));
    }

    private static RigidBodyDeclaration Body(double mass) => new(new(1), RigidMotionKind.Dynamic,
        default, default, CanonicalRotation.Identity, default, default, new((Half)mass), default, default);

    private static ColliderDeclaration Collider(ColliderShapeKind shape, double x, double y, double z) =>
        new(new(2), new(1), new(3), shape, RigidLocalPose.Identity,
            new(shape == ColliderShapeKind.Sphere ? (Half)x : (Half)0),
            shape == ColliderShapeKind.Box ? new((Half)x, (Half)y, (Half)z) : default);

    private static void AssertMoment(PrincipalInertia value, double expected)
    {
        value.Validate();
        var actual = Math.ScaleB((double)value.Mantissa, value.Exponent);
        Assert.InRange(Math.Abs(actual - expected) / expected, 0, .003);
    }
}

public sealed class PhysicsBodyReadSetTests
{
    [Fact]
    public void SixteenBodiesRetainNamedIdentityAndOneTime()
    {
        var values = Enumerable.Range(1, 16).Select(i => Read((ulong)i)).ToArray();
        var set = new PhysicsBodyReadSet(values);
        Assert.Equal(16, set.Count);
        Assert.True(set.TryGet(new(16), out var last));
        Assert.True(last.HasSameBits(values[15]));
        Assert.False(set.TryGet(new(17), out _));
        Assert.True(set.HasSameBits(new(values)));
        Assert.Throws<ArgumentException>(() => new PhysicsBodyReadSet([.. values, Read(17)]));
    }

    [Fact]
    public void DuplicateReversedAndMixedTimeReadsRejectAtomically()
    {
        var a = Read(1); var b = Read(2);
        Assert.Throws<ArgumentException>(() => new PhysicsBodyReadSet([a, a]));
        Assert.Throws<ArgumentException>(() => new PhysicsBodyReadSet([b, a]));
        Assert.Throws<ArgumentException>(() => new PhysicsBodyReadSet([a, b with { Body = b.Body with { Tick = 2 } }]));
        Assert.Throws<ArgumentException>(() => new PhysicsBodyReadSet([a, b with { Body = b.Body with { Epoch = 3 } }]));
    }

    [Fact]
    public void EqualityRetainsSignedZeroAndNonselectedBodyBits()
    {
        var a = Read(1); var b = Read(2);
        var negativeZero = BitConverter.UInt16BitsToHalf(0x8000);
        var changed = b with { AngularVelocity = new(negativeZero, (Half)0, (Half)0) };
        Assert.False(new PhysicsBodyReadSet([a, b]).HasSameBits(new([a, changed])));
        Assert.Throws<ArgumentException>(() => new PhysicsBodyReadSet([a,
            b with { LocalCentreOfMass = new(Half.NaN, (Half)0, (Half)0) }]));
    }

    [Fact]
    public void AngularMagnitudeRejectsComponentValidButVectorInvalidInput()
    {
        var body = new RigidBodyDeclaration(new(1), RigidMotionKind.Dynamic, default, default,
            CanonicalRotation.Identity, default, new((Half)64, (Half)0, (Half)0), new((Half)1), default, default);
        body.Validate();
        Assert.Throws<ArgumentException>(() => (body with { AngularVelocity = new((Half)64, (Half)64, (Half)0) }).Validate());
        (Read(1) with { AngularVelocity = body.AngularVelocity }).Validate();
        Assert.Throws<ArgumentException>(() => (Read(1) with { AngularVelocity = new((Half)64, (Half)64, (Half)0) }).Validate());
    }

    private static PhysicsBodyRead Read(ulong id) => new(
        new(new(id), 2, 1, default, default, default), CanonicalRotation.Identity, default, default);
}
