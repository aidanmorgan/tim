using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.44: each principal moment is one f32 closed form of the declared f32 inputs, within the f32 inertia bound.</summary>
public sealed class RigidMassPropertiesTests
{
    /// <summary>At most six f32 roundings separate a compiled moment from the exact moment of its declared f32 inputs
    /// (docs/gpu-f32-physics.md, game-grade envelope: 6 · 2^-24 relative).</summary>
    private const double InertiaBound = 6.0 / (1 << 24);

    // The admitted extremes: 1/1024 kg to 1024 kg, sphere radius 1/16 m to 16 m, box half-extents 1/1024 m to 16 m.
    [Theory]
    [InlineData(ColliderShapeKind.Sphere, 1.0 / 1024, 1.0 / 16)]
    [InlineData(ColliderShapeKind.Sphere, 1024, 16)]
    [InlineData(ColliderShapeKind.Box, 1.0 / 1024, 1.0 / 1024)]
    [InlineData(ColliderShapeKind.Box, 1024, 16)]
    public void ExtremeMomentsStayInsideTheCompiledF32Domain(ColliderShapeKind shape, double mass, double size)
    {
        var properties = MassFixtures.AdmitPrimitive(Body(mass), Collider(shape, size, size, size));
        double m = (float)mass, s = (float)size;
        var expected = shape == ColliderShapeKind.Sphere ? .4 * m * s * s : m * 2 * s * s / 3;
        AssertMoment(properties.X, expected);
        Assert.InRange(properties.X.Moment, PrincipalInertia.Minimum, MathF.BitDecrement(PrincipalInertia.Maximum));
        Assert.Equal(properties.X, properties.Y);
        Assert.Equal(properties.X, properties.Z);
    }

    [Fact]
    public void TheCompiledMomentDomainIsExactAtBothF32Bounds()
    {
        new PrincipalInertia(PrincipalInertia.Minimum).Validate();
        new PrincipalInertia(MathF.BitDecrement(PrincipalInertia.Maximum)).Validate();
        Assert.Throws<ArgumentException>(() => new PrincipalInertia(MathF.BitDecrement(PrincipalInertia.Minimum)).Validate());
        Assert.Throws<ArgumentException>(() => new PrincipalInertia(PrincipalInertia.Maximum).Validate());
        Assert.Throws<ArgumentException>(() => new PrincipalInertia(float.NaN).Validate());
        Assert.Throws<ArgumentException>(() => new PrincipalInertia(float.PositiveInfinity).Validate());
    }

    [Fact]
    public void ThinBoxDoesNotLoseItsSmallPrincipalMoment()
    {
        const double small = 1.0 / 1024;
        var properties = MassFixtures.AdmitPrimitive(Body(1), Collider(ColliderShapeKind.Box, small, 16, small));
        AssertMoment(properties.X, (256 + small * small) / 3);
        AssertMoment(properties.Y, 2 * small * small / 3);
        AssertMoment(properties.Z, (256 + small * small) / 3);
        Assert.True(properties.X.Moment / properties.Y.Moment >= 1 << 26);
    }

    [Fact]
    public void CentreAndPrincipalFrameRemainExactDeclaredGeometry()
    {
        var pose = new RigidLocalPose(new(1f, -2f, .5f),
            new(0f, 0f, (float).7071067811865476, (float).7071067811865476));
        var collider = Collider(ColliderShapeKind.Box, 1, 2, 3) with { Pose = pose };
        var properties = MassFixtures.AdmitPrimitive(Body(2), collider);
        Assert.Equal(pose.Translation, properties.LocalCentreOfMass);
        Assert.Equal(pose.Rotation, properties.PrincipalFrame);
        AssertMoment(properties.X, 2.0 * (4 + 9) / 3);
        AssertMoment(properties.Y, 2.0 * (1 + 9) / 3);
        AssertMoment(properties.Z, 2.0 * (1 + 4) / 3);
    }

    // A declared input that needs a full f32 significand compiles from its exact f32 value: 0.34 m and 0.1 m (a half-precision lane
    // would read 0.340088 and 0.099976), against the closed form evaluated in double from those f32 inputs.
    [Fact]
    public void NonBinary16InputsCompileFromTheirExactF32Values()
    {
        var sphere = MassFixtures.AdmitPrimitive(Body(1), Collider(ColliderShapeKind.Sphere, .34, .34, .34));
        AssertMoment(sphere.X, .4 * (double).34f * (double).34f);
        var box = MassFixtures.AdmitPrimitive(Body(.45), Collider(ColliderShapeKind.Box, .1, .34, 9.81));
        AssertMoment(box.Y, (double).45f * ((double).1f * .1f + (double)9.81f * 9.81f) / 3);
    }

    [Theory]
    [InlineData(ColliderShapeKind.Sphere)]
    [InlineData(ColliderShapeKind.Box)]
    public void NewExtentBoundaryRejectsOverflow(ColliderShapeKind shape)
    {
        Collider(shape, 16, 16, 16).Validate();
        Assert.Throws<ArgumentException>(() => Collider(shape, MathF.BitIncrement(16f), MathF.BitIncrement(16f), MathF.BitIncrement(16f)).Validate());
    }

    [Fact]
    public void DynamicPlaneAndForeignBodyHaveNoMassCompilation()
    {
        Assert.Throws<ArgumentException>(() => MassFixtures.AdmitPrimitive(Body(1),
            new(new(2), new(1), new(3), ColliderShapeKind.Plane, RigidLocalPose.Identity, default, default)));
        Assert.Throws<ArgumentException>(() => MassFixtures.AdmitPrimitive(Body(1),
            Collider(ColliderShapeKind.Sphere, 1, 1, 1) with { Body = new(9) }));
    }

    private static RigidBodyDeclaration Body(double mass) => new(new(1), RigidMotionKind.Dynamic,
        default, default, CanonicalRotation.Identity, default, default, new((float)mass), default, default, new(default, CanonicalRotation.Identity, new(1f), new(1f), new(1f)));

    private static ColliderDeclaration Collider(ColliderShapeKind shape, double x, double y, double z) =>
        new(new(2), new(1), new(3), shape, RigidLocalPose.Identity,
            new(shape == ColliderShapeKind.Sphere ? (float)x : 0f),
            shape == ColliderShapeKind.Box ? new((float)x, (float)y, (float)z) : default);

    private static void AssertMoment(PrincipalInertia value, double expected)
    {
        value.Validate();
        Assert.InRange(Math.Abs(value.Moment - expected) / expected, 0, InertiaBound);
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
        // Story 9R.5b: the read set has no fixed capacity; a seventeenth body is retained by name.
        var seventeen = new PhysicsBodyReadSet([.. values, Read(17)]);
        Assert.Equal(17, seventeen.Count);
        Assert.True(seventeen.TryGet(new(17), out _));
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
        var changed = b with { AngularVelocity = new(-0f, 0f, 0f) };
        Assert.False(new PhysicsBodyReadSet([a, b]).HasSameBits(new([a, changed])));
        var slower = b with { Body = b.Body with { Velocity = new(BitConverter.UInt32BitsToSingle(1), 0f, 0f) } };
        Assert.False(new PhysicsBodyReadSet([a, b]).HasSameBits(new([a, slower])));
        // The centre of mass compares by f32 bits: −0 differs from +0.
        Assert.False(new PhysicsBodyReadSet([a, b]).HasSameBits(new([a, b with { LocalCentreOfMass = new(-0f, 0f, 0f) }])));
        Assert.Throws<ArgumentException>(() => new PhysicsBodyReadSet([a,
            b with { LocalCentreOfMass = new(float.NaN, 0f, 0f) }]));
    }

    [Fact]
    public void AngularMagnitudeRejectsComponentValidButVectorInvalidInput()
    {
        var body = new RigidBodyDeclaration(new(1), RigidMotionKind.Dynamic, default, default,
            CanonicalRotation.Identity, default, new(128f, 0f, 0f), new(1f), default, default, new(default, CanonicalRotation.Identity, new(1f), new(1f), new(1f)));
        body.Validate();
        Assert.Throws<ArgumentException>(() => (body with { AngularVelocity = new(128f, 128f, 0f) }).Validate());
        (Read(1) with { AngularVelocity = body.AngularVelocity }).Validate();
        Assert.Throws<ArgumentException>(() => (Read(1) with { AngularVelocity = new(128f, 128f, 0f) }).Validate());
        Assert.Throws<ArgumentException>(() => (Read(1) with { AngularVelocity = new(float.NaN, 0f, 0f) }).Validate());
        Assert.Throws<ArgumentException>(() => (Read(1) with { AngularVelocity = new(0f, float.NegativeInfinity, 0f) }).Validate());
    }

    [Fact]
    public void StaticDeclarationsRequireAllZeroF32VelocityBits()
    {
        var body = new RigidBodyDeclaration(new(1), RigidMotionKind.Static, default, default,
            CanonicalRotation.Identity, default, default, new(0f), default, default, default);
        body.Validate();
        Assert.Throws<ArgumentException>(() => (body with { Velocity = new(0f, -0f, 0f) }).Validate());
        Assert.Throws<ArgumentException>(() => (body with { AngularVelocity = new(0f, 0f, -0f) }).Validate());
    }

    [Fact]
    public void DominoTileMomentsFollowTheDeclaredHalfExtentsAndMass()
    {
        var tile = LawTile.HalfExtents;
        var body = new RigidBodyDeclaration(new(1), RigidMotionKind.Dynamic, default, default, CanonicalRotation.Identity,
            default, default, new(LawTile.Mass), default, default, new(default, CanonicalRotation.Identity, new(1f), new(1f), new(1f)));
        var collider = new ColliderDeclaration(new(2), new(1), new(3), ColliderShapeKind.Box, RigidLocalPose.Identity, new(0f),
            new(tile.X, tile.Y, tile.Z));
        var properties = MassFixtures.AdmitPrimitive(body, collider);
        double mass = .4f, hx = .125f, hy = .55f, hz = .325f;
        Near(properties.X, mass * (hy * hy + hz * hz) / 3);
        Near(properties.Y, mass * (hx * hx + hz * hz) / 3);
        Near(properties.Z, mass * (hx * hx + hy * hy) / 3);
        Assert.True(properties.X.Moment > 2 * properties.Y.Moment, "The tall tile resists tipping far more than spinning about its height");
        static void Near(PrincipalInertia value, double expected)
        {
            value.Validate();
            Assert.InRange(Math.Abs(value.Moment - expected) / expected, 0, 6.0 / (1 << 24));
        }
    }

    private static PhysicsBodyRead Read(ulong id) => new(
        new(new(id), 2, 1, default, default, default), CanonicalRotation.Identity, default, default);
}
