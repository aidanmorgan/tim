using CuriousContraptions.Abstractions;
using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 10.2a (CAT-067): the Weight is its package's declaration data only, a dynamic solid cylinder whose <c>mass</c> parameter
/// (0.25–8 kg, default 4) is its compiled mass and, through four linear collider bindings, its size; the body origin is the base
/// centre. Every behaviour here runs the real Workshop construction through <see cref="WorkshopPhysicsCompiler"/> and the shipped
/// C# engine (<see cref="WorkshopSimulation"/> on the worker transport). Expectations are derived from the declared f32 values, which
/// the ABI carries unchanged, never from observed output.
/// </summary>
public sealed class WeightTests(ITestOutputHelper output)
{
    internal static readonly PartId Weight = new("weight");
    private static readonly ParameterId MassKey = new("mass");
    private static readonly float BenchY = WorkshopConstruction.WorkbenchSurface.Value;
    /// <summary>The magnitude of the declared f32 gravity (−9.81 m/s² on Y), which the body record's f32 lane carries unchanged.</summary>
    private const float G = 9.81f;
    private const double DominoUpright = -0.46 + .55;

    /// <summary>The declared size law in f32, as the shared evaluator computes it (multiplier·m + offset).</summary>
    private static float Radius(float mass) => .04808397f * mass + .15932827f;
    private static float HalfHeight(float mass) => .03818433f * mass + .12652539f;
    private static float Scale(float mass) => .136734f * mass + .453070f;

    internal static WorkshopGenericInstance Placed(ulong id, double x, double y, double z, float mass,
        double qx = 0, double qy = 0, double qz = 0, double qw = 1) =>
        WorkshopInput.Generic(Weight, new(id), (float)(x), (float)(y), (float)(z), (float)(qx), (float)(qy), (float)(qz), (float)(qw), new([new(MassKey, mass)]));

    private static WorkshopConstruction Bench(params IWorkshopInstance[] parts) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(parts));

    // ---------------- declaration ----------------

    [Fact]
    public void TheWeightIsAMassBoundDynamicCylinderWithALoadTieAndNoPayloadRole()
    {
        var entry = DeclaredPhysics.Of(Weight);
        var mass = Assert.Single(entry.Metadata.Parameters);
        Assert.Equal(new CuriousContraptions.Abstractions.ParameterDescriptor("mass", CuriousContraptions.Abstractions.ParameterUnit.Kilogram,
            CuriousContraptions.Abstractions.ParameterKind.Real, .25f, 8f, 4f, mass.Options,
            CuriousContraptions.Abstractions.ParameterEditorPolicy.Clamp, CuriousContraptions.Abstractions.ParameterRunEdit.BuildOnly), mass);
        Assert.Empty(mass.Options);
        Assert.Equal(new ParameterId("mass"), entry.Mechanical!.MassFrame.Mass.Parameter);
        Assert.Equal(CuriousContraptions.Abstractions.Mechanical.KinematicMode.Dynamic, entry.Mechanical.KinematicMode);
        Assert.Equal(4f, MassFixtures.Evaluate(entry.Mechanical.MassFrame, entry.Metadata.Parameters).Mass);
        Assert.Equal(CuriousContraptions.Abstractions.ColliderShapeKind.Cylinder, entry.Colliders[0].ShapeKind);
        Assert.Equal(RopeAttachmentRole.Load, Assert.Single(entry.Ports).AttachmentRole);
        Assert.Equal(WorkshopSocket.Tie, Assert.Single(DeclaredPorts.Current.Of(Weight).ToArray()).Socket);
        Assert.Empty(entry.Events);
        // A cylinder is never the payload (only a lone dynamic sphere is), so no capture, trigger or work store targets it.
        Assert.False(DeclaredPhysics.IsPayload(Weight));
        Assert.Equal((CuriousContraptions.Abstractions.ElementCategory.Motion, "Weight", 31, .7f),
            (entry.Metadata.Category, entry.Metadata.DisplayName, entry.Metadata.PaletteOrder, entry.Presentation.PickRadius));
        // The pick radius covers the largest Weight: √(r² + h²) at 8 kg is 0.695 m.
        Assert.True(MathF.Sqrt(Radius(8) * Radius(8) + HalfHeight(8) * HalfHeight(8)) < entry.Presentation.PickRadius);
        Assert.Equal(PartAllowance.Unlimited, WorkbenchCapacityTests.FreePalette[Weight]);
        // The defaults placement is the declared 4 kg.
        Assert.Equal(4f, WorkshopInput.Generic(Weight, new(1), 0, 1, 0, 0, 0, 0, 1).Parameters[MassKey]);
    }

    // ---------------- boundaries ----------------

    [Theory]
    [InlineData(.25f)]
    [InlineData(8f)]
    public void TheBoundaryMassesAreAdmittedAtEverySeam(float mass)
    {
        var weight = Placed(1, 0, 1, 0, mass);
        new InstanceParameterCollection([new(MassKey, mass)]).Validate(Weight);
        Bench(weight).Validate();
        Assert.Equal(new Kilograms(mass), Assert.Single(WorkshopPhysicsCompiler.Compile(Bench(weight), new(1, 2)).Bodies.ToArray(),
            body => body.Id == weight.Id).Mass);
        var bytes = PayloadInstanceTests.GenericSave("weight", [("mass", mass)]);
        var loaded = Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes).Construction.Instances));
        Assert.Equal(BitConverter.SingleToUInt32Bits(mass), BitConverter.SingleToUInt32Bits(loaded.Parameters[MassKey]));
        var command = Assert.IsType<WorkshopGenericInstance>(WorkshopAuthoringFlatBuffers.MapInstance(WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value));
        Assert.Equal(BitConverter.SingleToUInt32Bits(mass), BitConverter.SingleToUInt32Bits(command.Parameters[MassKey]));
    }

    public static TheoryData<float, WorkshopSaveRejection> RefusedMasses() => new()
    {
        { .249f, WorkshopSaveRejection.OutOfRange }, { 8.01f, WorkshopSaveRejection.OutOfRange }, { 0f, WorkshopSaveRejection.OutOfRange },
        { -1f, WorkshopSaveRejection.OutOfRange }, { float.NaN, WorkshopSaveRejection.NonFinite },
        { float.PositiveInfinity, WorkshopSaveRejection.NonFinite }, { float.NegativeInfinity, WorkshopSaveRejection.NonFinite },
    };

    // The finite values the text field would clamp (D4) are still refused by every seam behind it: the declared-parameter handler's
    // collection check, construction admission, the save's pre-validation and the command's wire decode. Nothing is clamped, defaulted
    // or migrated there, and the admitted construction and its save bytes are unchanged.
    [Theory]
    [MemberData(nameof(RefusedMasses))]
    public void AnOutOfRangeOrNonFiniteMassIsRefusedAtEverySeamAndChangesNothing(float mass, WorkshopSaveRejection save)
    {
        var admitted = Bench(Placed(1, 0, 1, 0, 4f));
        var refused = new InstanceParameterCollection([new(MassKey, mass)]);
        Assert.Throws<ArgumentException>(() => refused.Validate(Weight));
        Assert.Throws<ArgumentException>(() => Placed(1, 0, 1, 0, mass));
        var current = Assert.IsType<WorkshopGenericInstance>(Assert.Single(admitted.Instances));
        Assert.ThrowsAny<ArgumentException>(() => admitted.WithInstance(current with { Parameters = refused }).Validate());
        // The admitted construction, and the save written from it, still hold the one 4 kg Weight.
        Assert.Equal(4f, Assert.IsType<WorkshopGenericInstance>(Assert.Single(admitted.Instances)).Parameters[MassKey]);
        Assert.Equal(4f, Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(admitted, new(2)))).Construction.Instances)).Parameters[MassKey]);

        var bytes = PayloadInstanceTests.GenericSave("weight", [("mass", mass)]);
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopAuthoringFlatBuffers.MapInstance(record)).Reason);
    }

    // The count check: a Weight record without its mass is not filled from the default and not migrated, and an undeclared key is refused,
    // alone or beside the declared mass.
    [Fact]
    public void AMissingMassOrAnUnknownKeyIsRefusedWithoutDefaultFill()
    {
        Assert.Throws<ArgumentException>(() => new InstanceParameterCollection([]).Validate(Weight));
        Assert.Throws<ArgumentException>(() => new InstanceParameterCollection([new(new("radius"), 4f)]).Validate(Weight));
        Assert.Throws<ArgumentException>(() => new InstanceParameterCollection([new(MassKey, 4f), new(new("radius"), .3f)]).Validate(Weight));
        foreach (var parameters in new (string, float)[]?[] { null, [], [("radius", 4f)], [("mass", 4f), ("radius", .3f)] })
        {
            var bytes = PayloadInstanceTests.GenericSave("weight", parameters);
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
            var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
            Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopAuthoringFlatBuffers.MapInstance(record)).Reason);
        }
        // A typed record field is no Weight field either.
    }

    // ---------------- compile ----------------

    [Theory]
    [InlineData(.25f)]
    [InlineData(4f)]
    [InlineData(8f)]
    public void TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter(float mass)
    {
        var weight = Placed(1, 0, 1, 0, mass);
        var scene = WorkshopPhysicsCompiler.Compile(Bench(weight), new(1, 2));
        var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == weight.Id);
        Assert.Equal(RigidMotionKind.Dynamic, body.Motion);
        Assert.Equal(new Kilograms(mass), body.Mass);
        Assert.Equal(new InverseSeconds(0f), body.LinearDrag);
        Assert.Equal(new AccelerationVector(0f, -G, 0f), body.Gravity);
        var cylinder = Assert.Single(scene.Colliders.ToArray(), c => c.Id == Bench(weight).Instances.Identities[weight.Id].Colliders[0]);
        float r = Radius(mass), h = HalfHeight(mass);
        Assert.Equal(CuriousContraptions.Gpu.ColliderShapeKind.Cylinder, cylinder.Shape);
        Assert.Equal(new Metres(r), cylinder.Radius);
        Assert.Equal(new MetreVector(0f, h, 0f), cylinder.HalfExtents);
        // The base-centre origin: the cylinder's centre, and so the centre of mass, is h above it.
        Assert.Equal(new MetreVector(0f, h, 0f), cylinder.Pose.Translation);
        Assert.Equal(CanonicalRotation.Identity, cylinder.Pose.Rotation);
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == cylinder.Material);
        Assert.Equal((new Restitution(.08f), new FrictionCoefficient(.3f), new RollingResistance(.03f), new LinearSpeed(.1f)),
            (material.Restitution, material.Friction, material.RollingResistance, material.BounceThreshold));

        var properties = body.MassProperties;
        Assert.Equal(new MetreVector(0f, h, 0f), properties.LocalCentreOfMass);
        double m = mass, hr = r, hh = h;
        // Six f32 roundings at most (r², h², the division by 3, the sum, the two products; RigidMassProperties' bound): 6 x 2^-24
        // relative to the exact moments of the declared f32 inputs.
        const double bound = 6.0 / (1 << 24);
        Near(properties.Y, m * hr * hr / 2);
        Near(properties.X, m * (hr * hr / 4 + hh * hh / 3));
        Near(properties.Z, m * (hr * hr / 4 + hh * hh / 3));
        void Near(PrincipalInertia value, double expected)
        {
            var actual = (double)value.Moment;
            Assert.True(Math.Abs(actual - expected) / expected <= bound, $"moment {actual} vs analytic {expected}");
        }
        // The art scales uniformly with the radius: scale ÷ r is the 4 kg ratio within 1e-5 relative (f32 coefficients).
        Assert.InRange(Math.Abs(Scale(mass) / r / (Scale(4) / Radius(4)) - 1), 0, 1e-5);
        var art = Assert.Single(DeclaredPhysics.Of(Weight).Presentation.Parameters);
        Assert.Equal(("Visual", CuriousContraptions.Abstractions.PresentationProperty.UniformScale, "mass"), (art.Node, art.Property, art.Parameter));
        Assert.Equal(Scale(mass), art.Multiplier * mass + art.Offset);
    }

    [Fact]
    public void TheSizeLawIsTheLegacyCubeRootAtItsEndsAndTheSceneColliderAtTheDefault()
    {
        // Exact (f32) at 0.25 and 8 kg against r = 0.85·0.32∛m and h = 0.675·0.32∛m.
        foreach (var mass in new[] { .25f, 8f })
        {
            Assert.InRange(Math.Abs(Radius(mass) - .85 * .32 * Math.Cbrt(mass)), 0, 1e-6);
            Assert.InRange(Math.Abs(HalfHeight(mass) - .675 * .32 * Math.Cbrt(mass)), 0, 1e-6);
        }
        // The binding at the 4 kg default is the authored scene collider, bit for bit (one truth with the extracted geometry).
        var entry = TestBundle.Entry(Weight);
        var scene = entry.Colliders[0];
        var bound = CuriousContraptions.Bundle.BundleGeometry.Colliders(entry, new Dictionary<string, float> { ["mass"] = 4f })[0];
        Assert.Equal(scene, bound);
        Assert.Equal(new Vector3(Radius(4), HalfHeight(4), Radius(4)), scene.HalfExtents);
        Assert.Equal(new Vector3(0, HalfHeight(4), 0), scene.Offset);
        // The declared fixed mass properties are the 4 kg values (the package evaluates them per instance independently of collision geometry).
        var declared = MassFixtures.Evaluate(entry.Mechanical!.MassFrame, entry.Metadata.Parameters);
        Assert.Equal(new Vector3(0, HalfHeight(4), 0), declared.CentreOfMass);
        Assert.InRange(Math.Abs(declared.PrincipalMoments.Y - 4 * Radius(4) * Radius(4) / 2), 0, 1e-5);
        Assert.InRange(Math.Abs(declared.PrincipalMoments.X - 4 * (Radius(4) * Radius(4) / 4 + HalfHeight(4) * HalfHeight(4) / 3)), 0, 1e-5);
    }

    [Fact]
    public void TheSizeStrictlyIncreasesWithMass()
    {
        var masses = new[] { .25f, 1f, 4f, 8f };
        float[] Compiled(Func<ColliderDeclaration, float> lane) => masses.Select(mass =>
            lane(Assert.Single(WorkshopPhysicsCompiler.Compile(Bench(Placed(1, 0, 1, 0, mass)), new(1, 2)).Colliders.ToArray(),
                c => c.Id == Bench(Placed(1, 0, 1, 0, mass)).Instances.Identities[new(1)].Colliders[0]))).ToArray();
        foreach (var lanes in new[] { Compiled(c => c.Radius.Value), Compiled(c => c.HalfExtents.Y), Compiled(c => c.Pose.Translation.Y) })
            for (var i = 1; i < lanes.Length; i++) Assert.True(lanes[i] > lanes[i - 1], $"{lanes[i]} after {lanes[i - 1]}");
    }

    // ---------------- engine behaviour ----------------

    private static float Height(PhysicsBodyRead body) => (body.Body.Cell.Y + body.Body.Local.Y) / 16f;
    private static Vector3 Origin(PhysicsBodyRead body) => new((body.Body.Cell.X + body.Body.Local.X) / 16f,
        (body.Body.Cell.Y + body.Body.Local.Y) / 16f, (body.Body.Cell.Z + body.Body.Local.Z) / 16f);
    private static Quaternion Rotation(PhysicsBodyRead body) => new(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W);
    private static Vector3 Velocity(PhysicsBodyRead body) => new(body.Body.Velocity.X, body.Body.Velocity.Y, body.Body.Velocity.Z);
    private static Vector3 Spin(PhysicsBodyRead body) => new(body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);
    private static Vector3 Centre(PhysicsBodyRead body) => Origin(body) + Vector3.Transform(new Vector3(
        body.LocalCentreOfMass.X, body.LocalCentreOfMass.Y, body.LocalCentreOfMass.Z), Rotation(body));
    private static double Tilt(PhysicsBodyRead body) =>
        Math.Acos(Math.Clamp(1 - 2 * (body.Rotation.X * body.Rotation.X + body.Rotation.Z * body.Rotation.Z), -1, 1)) * 180 / Math.PI;

    private static PhysicsBodyRead Find(WorkshopRead read, GpuBodyId id)
    {
        for (var i = 0; i < read.Bodies.Count; i++) if (read.Bodies[i].Body.Id == id) return read.Bodies[i];
        throw new ArgumentException($"Body {id.Value} is not read.", nameof(id));
    }

    /// <summary>Total mechanical energy of a Weight of this mass, from its declared f32 mass and size: translational and
    /// rotational kinetic energy about the centre of mass (the solid-cylinder moments in its own frame) and gravitational potential of
    /// the centre of mass, whose height is taken through the body's rotation.</summary>
    private static double Energy(PhysicsBodyRead body, float mass)
    {
        double m = mass, r = Radius(mass), h = HalfHeight(mass);
        double ix = m * (r * r / 4 + h * h / 3), iy = m * r * r / 2;
        var w = Vector3.Transform(Spin(body), Quaternion.Inverse(Rotation(body)));
        var v = Velocity(body);
        return .5 * m * ((double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z) +
            .5 * (ix * w.X * w.X + iy * w.Y * w.Y + ix * w.Z * w.Z) + m * G * (Centre(body).Y - BenchY);
    }

    /// <summary>The energy a tick may add: lifting the body by the solver's contact slop (m g × 0.2 mm), 10.1's bound.</summary>
    private static double SlopEnergy(float mass) => mass * G * WorkshopConstraintSolver.ContactSlop;

    private static double WorstRise(List<WorkshopRead> reads, GpuBodyId id, float mass)
    {
        double worst = double.NegativeInfinity;
        for (var i = 1; i < reads.Count; i++)
            worst = Math.Max(worst, Energy(Find(reads[i], id), mass) - Energy(Find(reads[i - 1], id), mass));
        return worst;
    }

    /// <summary>The production simulation on the worker transport at a cadence: Construct, Run, `ticks` Advances (the reads include
    /// the built state first), then Reset.</summary>


    private const int Rate = 120;

    [Theory]
    [InlineData(.25f)]
    [InlineData(4f)]
    [InlineData(8f)]
    public async Task DroppedEndDownItRestsOnItsBaseWithoutSinkingDriftOrEnergyGain(float mass)
    {
        var weight = Placed(1, .3, BenchY + .05, -.2, mass);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(weight), 10 * Rate, includeBuilt: true);
        var settled = Find(reads[2 * Rate], weight.Id);
        var final = Find(reads[^1], weight.Id);
        Assert.InRange(Height(final), BenchY - .0005f, BenchY + .0005f);
        Assert.True(Velocity(final).Length() < .01f && Spin(final).Length() < .05f, $"rest v {Velocity(final).Length()} ω {Spin(final).Length()}");
        Assert.True(Tilt(final) < .5, $"tilt {Tilt(final)}");
        Assert.True(Vector3.Distance(Origin(final), Origin(settled)) < .0005f, $"drift {Origin(final) - Origin(settled)}");
        Assert.All(reads.Skip(2 * Rate), read => Assert.InRange(Height(Find(read, weight.Id)), BenchY - .0005f, BenchY + .0005f));
        var rise = WorstRise(reads, weight.Id, mass);
        Assert.True(rise <= SlopEnergy(mass), $"a tick gained {rise * 1000} mJ (slop {SlopEnergy(mass) * 1000} mJ)");
    }

    // The base-centre origin: editing a bench-resting 0.25 kg Weight to 8 kg grows it upward, so its base stays on the bench and no
    // tick is pushed out of an overlap.
    [Fact]
    public async Task AMassEditGrowsABenchRestingWeightUpwardAndItsBaseStaysOnTheBench()
    {
        var light = Placed(1, 0, BenchY, 0, .25f);
        var bench = Bench(light);
        bench.Validate();
        var edited = bench.WithInstance(light with { Parameters = new([new(MassKey, 8f)]) });
        edited.Validate();
        var cylinder = Assert.Single(WorkshopPhysicsCompiler.Compile(edited, new(1, 2)).Colliders.ToArray(), c => c.Id == edited.Instances.Identities[light.Id].Colliders[0]);
        // Its lowest point is the base, at the origin: the centre is exactly the half-height above it.
        Assert.Equal(cylinder.HalfExtents.Y, cylinder.Pose.Translation.Y);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(edited, 3 * Rate, includeBuilt: true);
        Assert.All(reads, read => Assert.InRange(Height(Find(read, light.Id)), BenchY - .0005f, BenchY + .0005f));
        var rise = WorstRise(reads, light.Id, 8f);
        Assert.True(rise <= SlopEnergy(8f), $"a tick gained {rise * 1000} mJ (slop {SlopEnergy(8f) * 1000} mJ)");
        Assert.True(Velocity(Find(reads[^1], light.Id)).Length() < .01f);
    }

    private static readonly double RampDegrees = 15;
    /// <summary>The default Ramp's centre (3 × 0.18 × 1.3 m slab), set so its top face's lower edge meets the bench: a roll runs off it
    /// onto the bench without a drop.</summary>
    private static readonly Vector3 RampCentre = new(0, (float)(BenchY + 1.5 * Math.Sin(RampDegrees * Math.PI / 180) -
        .09 * Math.Cos(RampDegrees * Math.PI / 180)), 0);
    /// <summary>The default Ramp tilted so its +X end runs down to the bench.</summary>
    private static WorkshopGenericInstance Ramp(ulong id, float lift = 0f)
    {
        var half = -RampDegrees * Math.PI / 360;
        return WorkshopInput.Generic(new PartId("ramp"), new(id), (float)(RampCentre.X), (float)(RampCentre.Y + lift), (float)(RampCentre.Z), 0, 0, (float)(Math.Sin(half)), (float)(Math.Cos(half)));
    }

    /// <summary>A Weight lying on its side (axis along world Z, centred at z = 0) on the Ramp's top face, `along` metres from its
    /// centre (positive downhill), clear of the face by 3 mm.</summary>
    private static WorkshopGenericInstance OnItsSideOnTheRamp(ulong id, float mass, float along, float lift = 0f,
        CylinderAxisDirection direction = CylinderAxisDirection.Authored)
    {
        var a = (float)(-RampDegrees * Math.PI / 180);
        var normal = new Vector3(-MathF.Sin(a), MathF.Cos(a), 0);
        var downhill = new Vector3(MathF.Cos(a), MathF.Sin(a), 0);
        var centre = RampCentre + new Vector3(0, lift, 0) + .09f * normal + along * downhill + (RollingRadius(mass) + .003f) * normal;
        if (direction != CylinderAxisDirection.Authored)
        {
            var sign = direction == CylinderAxisDirection.Positive ? 1f : -1f;
            var directedOrigin = centre - new Vector3(0, 0, sign * HalfHeight(mass));
            return Placed(id, directedOrigin.X, directedOrigin.Y, directedOrigin.Z, mass, .5, sign * .5, .5, sign * .5);
        }
        var origin = centre - new Vector3(0, 0, HalfHeight(mass));
        return Placed(id, origin.X, origin.Y, origin.Z, mass, Math.Sin(Math.PI / 4), 0, 0, Math.Cos(Math.PI / 4));
    }

    private static double ArtScale(float mass) => .136734 * mass + .453070;
    private static float RollingRadius(float mass) => (float)(.3633 * ArtScale(mass));
    private static double EyeCrest(float mass) => (.5771 + .1575) * ArtScale(mass);

    // Independent support of the literal art cylinders and thickened circles at the observed pose.
    private static double LowestArtPoint(PhysicsBodyRead body, float mass) => Origin(body).Y + LowestArtOffset(Rotation(body), mass);

    private static double LowestArtOffset(Quaternion rotation, float mass)
    {
        var normal = Vector3.Transform(Vector3.UnitY, Quaternion.Conjugate(rotation));
        double x = normal.X, y = normal.Y, z = normal.Z, scale = ArtScale(mass);
        var radial = Math.Sqrt(x * x + z * z);
        var primary = HalfHeight(mass) * y - HalfHeight(mass) * Math.Abs(y) - Radius(mass) * radial;
        var foot = scale * (.0517 * y - .025 * Math.Abs(y) - .36 * radial);
        var lower = scale * (.1862 * y - .3558 * radial - .0075);
        var upper = scale * (.3724 * y - .3558 * radial - .0075);
        var eye = scale * (.5771 * y - .14 * Math.Sqrt(x * x + y * y) - .0175);
        return Math.Min(primary, Math.Min(foot, Math.Min(lower, Math.Min(upper, eye))));
    }

    public enum CylinderAxisDirection { Authored, Positive, Negative }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [InlineData(true, CylinderAxisDirection.Positive)]
    [InlineData(true, CylinderAxisDirection.Negative)]
    public async Task OnItsSideItRollsDownARampWithoutSlippingAndComesToRestOnTheBenchWithoutAxialCreep(bool drop, CylinderAxisDirection direction = CylinderAxisDirection.Authored)
    {
        // Moving from the top lower edge to the bottom lower corner raises the slab by its vertical thickness.
        var lift = drop ? .18f * MathF.Cos((float)(RampDegrees * Math.PI / 180)) : 0f;
        var weight = OnItsSideOnTheRamp(1, 4f, .5f, lift, direction);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(Ramp(2, lift), weight), 14 * Rate, includeBuilt: true);
        var r = RollingRadius(4);
        // On the ramp, rolling: ω = v / r within 2% (no slip needs μ ≥ tanθ/3 = 0.09; the material is 0.3).
        foreach (var tick in new[] { Rate / 2, 3 * Rate / 4 })
        {
            var body = Find(reads[tick], weight.Id);
            var speed = Velocity(body).Length();
            Assert.True(speed > .3f, $"rolling at tick {tick} ({speed} m/s)");
            Assert.True(Math.Abs(Spin(body).Length() - speed / r) <= .02 * speed / r, $"tick {tick}: ω {Spin(body).Length()} vs v/r {speed / r}");
        }
        var final = Find(reads[^1], weight.Id);
        Assert.True(Velocity(final).Length() < .01f && Spin(final).Length() < .05f, $"stopped under rolling resistance: v {Velocity(final).Length()} ω {Spin(final).Length()}");
        Assert.True(Centre(final).X > RampCentre.X + 2.5f, $"rolled off the ramp onto the bench ({Centre(final).X})");
        Assert.InRange(Centre(final).Y, BenchY + r - .0005f, BenchY + r + .0005f);
        // No axial creep: the centre never moves 0.5 mm along the cylinder's axis (world Z) while rolling or at rest, and once stopped it
        // stays put: within 0.05 mm (a tenth of the 0.5 mm resting envelope, restitution D10) from the tick it is at rest (below 1 mm/s,
        // which leaves under 3 µm of Crr deceleration) to the end.
        var axialMaximum = reads.Max(read => MathF.Abs(Centre(Find(read, weight.Id)).Z));
        Assert.True(axialMaximum <= .0005f, $"axial max {axialMaximum}; " +
            string.Join("; ", Enumerable.Range(6, 12).Select(i => i * 20).Append(1680).Select(t =>
            {
                var observed = Find(reads[t], weight.Id);
                return $"tick {t}: centre {Centre(observed)}, v {Velocity(observed)}, w {Spin(observed)}";
            })));
        var stopped = reads.FindIndex(2 * Rate, read => Velocity(Find(read, weight.Id)).Length() < .001f);
        Assert.True(stopped > 0 && stopped < reads.Count - 2 * Rate, $"stopped at tick {stopped}");
        var creep = Vector3.Distance(Centre(final), Centre(Find(reads[stopped], weight.Id)));
        Assert.True(creep <= .00005f, $"moved {creep * 1000} mm after stopping");
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(.37f, 0f)]
    [InlineData(2.1f, 0f)]
    [InlineData(0f, .03f)]
    [InlineData(.37f, .03f)]
    [InlineData(2.1f, .03f)]
    public void AFullTorusRollOnAPlaneIsInvariantToItsInitialAxialPhase(float phase, float resistance)
    {
        const float major = .3558f, tube = .0075f, radius = major + tube, speed = 1.5f;
        var scene = new LawScene();
        var rotation = Quaternion.Multiply(new Quaternion(.5f, .5f, .5f, .5f),
            Quaternion.CreateFromAxisAngle(Vector3.UnitY, phase));
        var r = Radius(4); var h = HalfHeight(4);
        var moments = new Vector3(4 * (r * r / 4 + h * h / 3), 2 * r * r, 4 * (r * r / 4 + h * h / 3));
        var body = scene.DynamicHollow(Gpu.ColliderShapeKind.Torus, new(0, BenchY + radius, 0), rotation,
            major, new(0, tube, ColliderDeclaration.FullSweep), new(0, .1f, .3f, resistance), 4, moments,
            new(speed, 0, 0), LawScene.Gravity, new(0, 0, -speed / radius));
        var axial = 0f; var height = 0f;
        var final = scene.Admit().Step(14 * Rate, (_, state) =>
        {
            axial = MathF.Max(axial, MathF.Abs(state[body].Position.Z));
            height = MathF.Max(height, MathF.Abs(state[body].Position.Y - (BenchY + radius)));
        })[body];
        output.WriteLine($"phase={phase}, Crr={resistance}: axial={axial}, height={height}, speed={final.Speed}, spin={final.Spin}");
        Assert.InRange(axial, 0, .0005f);
        Assert.InRange(height, 0, .0005f);
        if (resistance > 0) Assert.True(final.Speed < .01 && final.Spin < .05);
        else Assert.True(final.Speed > 1.4);
    }

    public enum RollingGeometrySet { BareCylinder, SymmetricBands, FullWeight }
    public enum RollingTerrain { Ramp, Plane, Box }

    [Theory]
    [InlineData(RollingGeometrySet.BareCylinder, false, 0)]
    [InlineData(RollingGeometrySet.BareCylinder, true, 90)]
    [InlineData(RollingGeometrySet.BareCylinder, false, 180)]
    [InlineData(RollingGeometrySet.SymmetricBands, false, 0)]
    [InlineData(RollingGeometrySet.SymmetricBands, true, 0)]
    [InlineData(RollingGeometrySet.SymmetricBands, false, 37)]
    [InlineData(RollingGeometrySet.SymmetricBands, false, 90)]
    [InlineData(RollingGeometrySet.SymmetricBands, true, 180)]
    [InlineData(RollingGeometrySet.FullWeight, false, 0)]
    [InlineData(RollingGeometrySet.FullWeight, true, 0)]
    [InlineData(RollingGeometrySet.FullWeight, false, 37)]
    [InlineData(RollingGeometrySet.FullWeight, false, 90)]
    [InlineData(RollingGeometrySet.FullWeight, true, 180)]
    [InlineData(RollingGeometrySet.SymmetricBands, false, 0, RollingTerrain.Plane)]
    [InlineData(RollingGeometrySet.SymmetricBands, true, 0, RollingTerrain.Plane)]
    [InlineData(RollingGeometrySet.SymmetricBands, false, 0, RollingTerrain.Box)]
    [InlineData(RollingGeometrySet.SymmetricBands, true, 0, RollingTerrain.Box)]
    public void CompoundRollingRetainsItsHeadingAcrossTheRampEdge(RollingGeometrySet geometry, bool reverseChildren, int yawDegrees,
        RollingTerrain terrain = RollingTerrain.Ramp)
    {
        var lift = .18f * MathF.Cos((float)(RampDegrees * Math.PI / 180));
        var weight = OnItsSideOnTheRamp(1, 4f, .5f, lift);
        var compiled = WorkshopPhysicsCompiler.Compile(Bench(Ramp(2, lift), weight), new(1, 2));
        var children = compiled.Colliders.ToArray().Where(c => c.Body == weight.Id).ToArray();
        var colliders = compiled.Colliders.ToArray().Where(c => c.Body != weight.Id).ToList();
        var selected = geometry switch
        {
            RollingGeometrySet.BareCylinder => new[] { children[0] },
            RollingGeometrySet.SymmetricBands => new[] { children[0], children[2] with
                { Pose = children[2].Pose with { Translation = new(0, HalfHeight(4) - .0931f * (float)ArtScale(4), 0) } },
                children[3] with { Pose = children[3].Pose with { Translation = new(0, HalfHeight(4) + .0931f * (float)ArtScale(4), 0) } } },
            _ => children
        };
        if (reverseChildren)
        {
            var ids = selected.Select(c => c.Id).Reverse().ToArray();
            for (var i = 0; i < selected.Length; i++) selected[i] = selected[i] with { Id = ids[i] };
        }
        if (terrain == RollingTerrain.Plane) colliders.RemoveAll(c => c.Shape != Gpu.ColliderShapeKind.Plane);
        if (terrain == RollingTerrain.Box)
        {
            var primary = Bench(Ramp(2, lift), weight).Instances.Identities[new(2)].Colliders[0];
            var box = colliders.Single(c => c.Id == primary) with { HalfExtents = new(16, .09f, 16), Pose = RigidLocalPose.Identity };
            colliders.Clear();
            colliders.Add(box);
        }
        colliders.AddRange(selected);
        var yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yawDegrees * MathF.PI / 180);
        var bodies = compiled.Bodies.ToArray().Select(body =>
        {
            if (terrain != RollingTerrain.Ramp)
            {
                var initial = body.Id == weight.Id ? new Vector3(0, BenchY + RollingRadius(4) + .003f, -HalfHeight(4)) :
                    new Vector3(0, body.Id.Value == 2 ? BenchY - .09f : BenchY, 0);
                var px = WorkshopInput.Position((float)(initial.X)); var py = WorkshopInput.Position((float)(initial.Y)); var pz = WorkshopInput.Position((float)(initial.Z));
                body = body with { Cell = new(px.Cell, py.Cell, pz.Cell), Local = new(px.Local, py.Local, pz.Local) };
                body = body.Id == weight.Id ? body with { Velocity = new(1.5f, 0, 0), AngularVelocity = new(0, 0, -1.5f / RollingRadius(4)) } :
                    body with { Rotation = CanonicalRotation.Identity };
            }
            var position = Vector3.Transform(new Vector3((body.Cell.X + body.Local.X) / 16f,
                (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f), yaw);
            var x = WorkshopInput.Position((float)(position.X)); var y = WorkshopInput.Position((float)(position.Y)); var z = WorkshopInput.Position((float)(position.Z));
            var rotation = Quaternion.Multiply(yaw, new Quaternion(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W));
            return body with { Cell = new(x.Cell, y.Cell, z.Cell), Local = new(x.Local, y.Local, z.Local),
                Rotation = CanonicalRotation.Normalize(rotation.X, rotation.Y, rotation.Z, rotation.W) };
        }).ToArray();
        var scene = new PhysicsSceneDeclaration(new(1, 2), compiled.NextIdentity, bodies, [.. colliders], compiled.Materials.ToArray(), [], []);
        var world = new LawWorld(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);
        var before = 0f; var after = 0f; var heading = 0f;
        var final = world.Step(14 * Rate, (tick, state) =>
        {
            var body = state[weight.Id];
            var centre = Vector3.Transform(body.Position + Vector3.Transform(new Vector3(0, HalfHeight(4), 0), body.Rotation), Quaternion.Conjugate(yaw));
            var axis = Vector3.Transform(Vector3.Transform(Vector3.UnitY, body.Rotation), Quaternion.Conjugate(yaw));
            heading = MathF.Max(heading, MathF.Abs(axis.X));
            if (tick <= Rate) before = MathF.Max(before, MathF.Abs(centre.Z));
            else after = MathF.Max(after, MathF.Abs(centre.Z));
        })[weight.Id];
        output.WriteLine($"{geometry}, terrain={terrain}, reversedIDs={reverseChildren}, yaw={yawDegrees}: before={before:R}, after={after:R}, axisX={heading:R}, final v={final.Velocity}, spin={final.Angular}");
        Assert.True(MathF.Max(before, after) <= .0005f, $"before={before:R}, after={after:R}, axisX={heading:R}");
    }

    [Fact]
    public async Task SavingTheCompoundDropPreservesRollingAndSettlement()
    {
        var lift = .18f * MathF.Cos((float)(RampDegrees * Math.PI / 180));
        var weight = OnItsSideOnTheRamp(1, 4f, .5f, lift, CylinderAxisDirection.Positive);
        var construction = Bench(Ramp(2, lift), weight);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(construction, new(4)))).Construction;
        Assert.Equal(construction, decoded);
        var (_, first, _) = await WorkshopIntegrationHarness.ObserveReset(construction, 14 * Rate, includeBuilt: true);
        var (_, saved, _) = await WorkshopIntegrationHarness.ObserveReset(decoded, 14 * Rate, includeBuilt: true);
        foreach (var run in new[] { first, saved })
        {
            var initial = Find(run[0], weight.Id);
            var final = Find(run[^1], weight.Id);
            Assert.True(Origin(final).X > Origin(initial).X + .5f, "The Weight rolls downhill off the ramp.");
            Assert.True(run.Any(read => Spin(Find(read, weight.Id)).Length() > .1f), "Rolling includes rotation.");
            Assert.InRange(LowestArtPoint(final, 4), BenchY - .001, BenchY + .001);
            Assert.True(Velocity(final).Length() < .02f, "The Weight settles on the bench.");
            var lastSecond = run.TakeLast(Rate).Select(read => Origin(Find(read, weight.Id))).ToArray();
            Assert.All(lastSecond, position => Assert.InRange(Vector3.Distance(position, Origin(final)), 0, .001f));
        }
    }

    [Fact]
    public void PrimitiveCylinderAxisReversalPreservesTheCompleteHeadingHistory()
    {
        var lift = .18f * MathF.Cos((float)(RampDegrees * Math.PI / 180));
        var angle = (float)(-RampDegrees * Math.PI / 180);
        var normal = new Vector3(-MathF.Sin(angle), MathF.Cos(angle), 0);
        var downhill = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0);
        var centre = RampCentre + new Vector3(0, lift, 0) + .09f * normal + .5f * downhill + (Radius(4) + .003f) * normal;
        (LawWorld World, GpuBodyId Body) Build(float sign)
        {
            var scene = new LawScene();
            scene.StaticBox(RampCentre + new Vector3(0, lift, 0), new(1.5f, .09f, .65f),
                Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle), new(.15f, .1f, .3f, 0));
            var body = scene.Cylinder(centre, Radius(4), HalfHeight(4), 4, new(.08f, .1f, .3f, .03f),
                new Quaternion(.5f, sign * .5f, .5f, sign * .5f));
            return (scene.Admit(), body);
        }
        Quaternion[] History(float sign)
        {
            var run = Build(sign);
            var rotations = new Quaternion[14 * Rate + 1];
            rotations[0] = run.World.Initial(run.Body).Rotation;
            run.World.Step(14 * Rate, (tick, state) => rotations[tick] = state[run.Body].Rotation);
            return rotations;
        }
        var forward = History(1); var reverse = History(-1);
        // With |q_i| <= 1.0001 and component uncertainty u=2^-24, the polynomial
        // UnitY transform differs by <27.72u across two reads; 32u includes double evaluation.
        const double u = 1.0 / 16777216;
        static (double X, double Y, double Z) Axis(Quaternion q)
        {
            foreach (var component in new[] { q.X, q.Y, q.Z, q.W }) Assert.InRange(Math.Abs(component), 0, 1.0001);
            double x = q.X, y = q.Y, z = q.Z, w = q.W;
            return (2 * (x * y - w * z), 1 - 2 * (x * x + z * z), 2 * (y * z + w * x));
        }
        for (var tick = 0; tick <= 14 * Rate; tick++)
        {
            var a = Axis(forward[tick]);
            var b = Axis(reverse[tick]);
            var error = Math.Sqrt((a.X + b.X) * (a.X + b.X) + (a.Y + b.Y) * (a.Y + b.Y) + (a.Z + b.Z) * (a.Z + b.Z));
            Assert.True(error <= 32 * u, $"axis reversal error {error} at tick {tick}, limit {32 * u}");
        }
    }

    [Theory]
    [InlineData(4f, 4f)]
    [InlineData(8f, 1f)]
    public async Task AStackEndDownHoldsWithoutBounceOrCreep(float lower, float upper)
    {
        var bottom = Placed(1, 0, BenchY, 0, lower);
        var top = Placed(2, 0, BenchY + EyeCrest(lower) + .002f, 0, upper);
        var (_, reads, _) = await WorkshopIntegrationHarness.ObserveReset(Bench(bottom, top), 5 * Rate, includeBuilt: true);
        // Each resting contact keeps the 0.5 mm envelope: the lower base on the bench, and the upper base on the lower Weight's top face
        // (its base plus the compiled height 2h).
        var final = (Lower: Height(Find(reads[^1], bottom.Id)), Upper: Height(Find(reads[^1], top.Id)));
        Assert.InRange(final.Lower, BenchY - .0005f, BenchY + .0005f);
        var face = final.Lower + EyeCrest(lower);
        Assert.InRange(final.Upper, face - .0005f, face + .0005f);
        // D8 (restitution D10): after settling no bounce, and the last second moves at most 0.05 mm per tick and at most 0.5 mm in all
        // (its path length, the resting envelope), so no steady drift hides under the per-tick bound.
        Assert.All(reads.Skip(2 * Rate), read => Assert.True(Find(read, top.Id).Body.Velocity.Y < .01f, "no bounce after settling"));
        foreach (var id in new[] { bottom.Id, top.Id })
        {
            var path = 0f;
            for (var i = reads.Count - Rate; i < reads.Count; i++)
            {
                var step = Vector3.Distance(Origin(Find(reads[i], id)), Origin(Find(reads[i - 1], id)));
                Assert.True(step <= .00005f, $"tick {i}: body {id.Value} moved {step * 1000} mm");
                path += step;
            }
            Assert.True(path <= .0005f, $"body {id.Value} moved {path * 1000} mm in the last second");
        }
        Assert.True(Tilt(Find(reads[^1], top.Id)) < .5);
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task AWeightRollingOffARampTopplesADominoAndARestingWeightBesideOneLeavesItStanding(SimulationCadence cadence)
    {
        var settings = WorkshopCadenceSettings.Default() with { Simulation = cadence };
        var rate = (int)settings.SimulationRate.Numerator;
        var domino = PlacedParts.Domino(new(3), 3.2f, (float)(DominoUpright), 0, 0, 0, 0, 1);
        var positive = Bench(Ramp(2), OnItsSideOnTheRamp(1, 4f, .5f), domino) with { Settings = settings };
        var (built, struck, reset) = await WorkshopIntegrationHarness.ObserveReset(positive, 6 * rate, includeBuilt: true);
        Assert.Contains(struck, read => Tilt(Find(read, domino.Id)) > 45);
        NoFixedCapacityTests.AssertSameState(built, reset, "rolling Weight Reset");
        // Change only the Weight's pose: the same ramp and tile remain, but no rolling impact reaches the tile.
        var beside = Placed(1, 3.2 - .125 - .1 - Radius(4), BenchY, 0, 4f);
        var control = positive.WithInstance(beside);
        var (_, quiet, _) = await WorkshopIntegrationHarness.ObserveReset(control, 6 * rate, includeBuilt: true);
        // A tile remains recognisably upright; sub-degree resting variations are not toppling.
        Assert.All(quiet, read => Assert.True(Tilt(Find(read, domino.Id)) < 15));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACompoundWeightLevelsItsSupportAxisWithAndWithoutRollingResistance(bool resistance)
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 1e-4f) * new Quaternion(.5f, .5f, .5f, .5f);
        var axis = Vector3.Transform(Vector3.UnitY, rotation);
        var origin = -axis * HalfHeight(4);
        origin.Y = BenchY - (float)LowestArtOffset(rotation, 4);
        var authored = Bench(Placed(1, origin.X, origin.Y, origin.Z, 4, rotation.X, rotation.Y, rotation.Z, rotation.W));
        var compiled = WorkshopPhysicsCompiler.Compile(authored, new(1, 2));
        var materials = compiled.Materials.ToArray().Select(material => resistance ? material : material with { RollingResistance = new(0f) }).ToArray();
        var scene = new PhysicsSceneDeclaration(new(1, 2), compiled.NextIdentity, compiled.Bodies.ToArray(), compiled.Colliders.ToArray(), materials, [], []);
        var world = new LawWorld(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);
        var lateTilt = 0f;
        var final = world.Step(1200, (tick, state) =>
        {
            if (tick >= 480)
                lateTilt = MathF.Max(lateTilt, MathF.Asin(MathF.Min(1f, MathF.Abs(Vector3.Transform(Vector3.UnitY, state[new(1)].Rotation).Y))));
        })[new(1)];
        output.WriteLine($"resistance={resistance}: lateTilt={lateTilt:R}, v={final.Velocity}, w={final.Angular}");
        Assert.InRange(lateTilt, 0f, 1e-5f);
    }

    [Fact]
    public async Task TiltedWeightResetAndSavedConstructionReplayEveryCommittedTickExactly()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, 1e-4f) * new Quaternion(.5f, .5f, .5f, .5f);
        var axis = Vector3.Transform(Vector3.UnitY, rotation);
        // Place the actual union tangent to the bench; a bare-cylinder radius
        // would bury the two drawn bands by about11.6mm before the first tick.
        var origin = -axis * HalfHeight(4);
        origin.Y = BenchY - (float)LowestArtOffset(rotation, 4);
        var construction = Bench(Placed(1, origin.X, origin.Y, origin.Z, 4, rotation.X, rotation.Y, rotation.Z, rotation.W));
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication()), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(),
            CompiledSceneFixture.Compile(new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty)), new(1), new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Initialize()).Outcome);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Construct(CompiledSceneFixture.Compile(construction))).Outcome);
        var built = simulation.Committed;
        async Task<List<WorkshopRead>> Run()
        {
            Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Run()).Outcome);
            var reads = new List<WorkshopRead>();
            for (var tick = 0; tick < 1200; tick++)
            {
                Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Advance()).Outcome);
                reads.Add(simulation.Committed);
            }
            return reads;
        }
        static void SameState(WorkshopRead expected, WorkshopRead actual)
        {
            Assert.Equal(expected.Tick, actual.Tick);
            Assert.Equal(expected.Bodies.Count, actual.Bodies.Count);
            for (var i = 0; i < expected.Bodies.Count; i++)
            {
                var e = expected.Bodies[i];
                var a = actual.Bodies[i];
                Assert.Equal(e.Body.Id, a.Body.Id);
                Assert.Equal(e.Body.Tick, a.Body.Tick);
                Assert.Equal(e.Body.Cell, a.Body.Cell);
                Assert.Equal(e.Body.Local.X, a.Body.Local.X);
                Assert.Equal(e.Body.Local.Y, a.Body.Local.Y);
                Assert.Equal(e.Body.Local.Z, a.Body.Local.Z);
                Assert.Equal(Rotation(e), Rotation(a));
                Assert.Equal(Velocity(e), Velocity(a));
                Assert.Equal(Spin(e), Spin(a));
                Assert.Equal(e.LocalCentreOfMass.X, a.LocalCentreOfMass.X);
                Assert.Equal(e.LocalCentreOfMass.Y, a.LocalCentreOfMass.Y);
                Assert.Equal(e.LocalCentreOfMass.Z, a.LocalCentreOfMass.Z);
            }
            // A lone passive Weight has no sensors, activations, timers, work stores or circuits.
            foreach (var read in new[] { expected, actual })
            {
                Assert.Equal(0, read.Captures.Count);
                Assert.Equal(0, read.Activations.Count);
                Assert.Equal(0, read.Timers.Count);
                Assert.Equal(0, read.ContactWorks.Count);
                Assert.Equal(0, read.Memory.Count);
                Assert.Equal(0, read.Electrical.Count);
            }
        }
        var first = await Run();
        var rest = new RestingObservation(MathF.Min(2 * Radius(4), 2 * HalfHeight(4)),
            Centre(Find(first[479], new(1))));
        for (var tick = 479; tick < first.Count; tick++)
        {
            var body = Find(first[tick], new(1));
            var centre = Centre(body);
            rest.Observe(centre);
            var verticalAxis = MathF.Abs(Vector3.Transform(Vector3.UnitY, Rotation(body)).Y);
            Assert.InRange(MathF.Asin(MathF.Min(1f, verticalAxis)), 0f, 1e-5f);
            var lowest = LowestArtPoint(body, 4);
            Assert.InRange(BenchY - lowest, -WorkshopConstraintSolver.ContactSlop, .0005f);
            Assert.InRange(Velocity(body).Length(), 0f, .002f);
        }
        Assert.True(rest.IsResting,
            $"tilted package excursion {rest.PeakMetres:R}/{rest.AllowanceMetres:R} m; resolved {rest.Resolved}");
        Assert.NotEqual(Rotation(Find(built, new(1))), Rotation(Find(first[^1], new(1))));
        Assert.Equal(WorkshopCommandOutcome.Applied, (await simulation.Reset()).Outcome);
        SameState(built, simulation.Committed);
        Assert.Equal(WorkshopCommandOutcome.Applied, simulation.Save().Outcome);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(construction, new(2))));
        var replay = await Run();
        var (_, fresh, _) = await WorkshopIntegrationHarness.ObserveReset(loaded.Construction, 1200, includeBuilt: true);
        for (var tick = 0; tick < first.Count; tick++)
        {
            SameState(first[tick], replay[tick]);
            SameState(first[tick], fresh[tick + 1]);
        }
    }

    // ---------------- Reset and save ----------------

    /// <summary>A tipped 4 kg Weight dropping 1.3 m onto the bench beside a bench-resting one edited from 0.25 to 8 kg.</summary>
    private static WorkshopConstruction EditedPair()
    {
        var light = Placed(1, 0, BenchY, 0, .25f);
        var tipped = Placed(2, 1.2, BenchY + 1.3, .1, 4f, 0, 0, Math.Sin(Math.PI / 18), Math.Cos(Math.PI / 18));
        return Bench(light, tipped).WithInstance(light with { Parameters = new([new(MassKey, 8f)]) });
    }

    /// <summary>The same bodies at the same tick, each within <paramref name="metres"/> of its pose, <paramref name="radians"/> of its
    /// rotation and <paramref name="speed"/> of its linear and angular velocity.</summary>
    private static void AssertSamePoses(WorkshopRead expected, WorkshopRead actual, float metres, float radians, float speed, string label)
    {
        Assert.Equal(expected.Tick, actual.Tick);
        Assert.Equal(expected.Bodies.Count, actual.Bodies.Count);
        for (var i = 0; i < expected.Bodies.Count; i++)
        {
            var e = expected.Bodies[i];
            var a = Find(actual, e.Body.Id);
            // The relative rotation's angle from its vector part (accurate near zero, unlike acos of the dot product).
            var relative = Quaternion.Inverse(Rotation(e)) * Rotation(a);
            var turn = 2 * MathF.Asin(MathF.Min(1f, new Vector3(relative.X, relative.Y, relative.Z).Length()));
            Assert.True(Vector3.Distance(Origin(e), Origin(a)) <= metres && turn <= radians &&
                Vector3.Distance(Velocity(e), Velocity(a)) <= speed && Vector3.Distance(Spin(e), Spin(a)) <= speed,
                $"{label}: body {e.Body.Id.Value} at {Origin(a)} turned {turn} rad, expected {Origin(e)}");
        }
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task ResetRestoresTheBuiltPosesAndAReplayRepeatsTheRunIncludingAfterAMassEdit(SimulationCadence cadence)
    {
        var construction = EditedPair();
        construction.Validate();
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(construction, 600, cadence, includeBuilt: true);
        // Reset restores every authored pose at rest: within a micrometre and a microradian, with no motion.
        AssertSamePoses(built, reset, 1e-6f, 1e-6f, 1e-6f, "Reset");
        Assert.All(Enumerable.Range(0, reset.Bodies.Count), i =>
            Assert.True(Velocity(reset.Bodies[i]).Length() == 0 && Spin(reset.Bodies[i]).Length() == 0, "at rest after Reset"));
        Assert.True(Vector3.Distance(Origin(Find(run[^1], new(2))), Origin(Find(built, new(2)))) > .5f, "the tipped Weight fell");
        // The cadence is the run's own: a quarter second is rate / 4 ticks, in which the tipped Weight falls ½ g t² (within 2%, the
        // 480 Hz step's semi-implicit lag) whatever the cadence.
        var rate = cadence switch { SimulationCadence.Hz60 => 60, SimulationCadence.Hz120 => 120, SimulationCadence.Hz240 => 240,
            _ => throw new ArgumentOutOfRangeException(nameof(cadence)) };
        var fallen = Centre(Find(built, new(2))).Y - Centre(Find(run[rate / 4], new(2))).Y;
        var free = .5 * G * .25 * .25;
        Assert.True(Math.Abs(fallen - free) <= .02 * free, $"{cadence}: fell {fallen} m in {rate / 4} ticks, free fall {free} m");
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(construction, 600, cadence, includeBuilt: true);
        // The replay repeats the run's motion on every tick, within the 0.05 mm late-motion bound (restitution D10).
        for (var i = 0; i < run.Count; i++) AssertSamePoses(run[i], replay[i], .00005f, 1e-4f, 1e-3f, $"replay tick {i}");
    }

    [Fact]
    public async Task SaveRoundTripsPoseRotationAndMassExactlyWithNoWireChange()
    {
        var construction = EditedPair().WithInstance(Placed(3, -1.7, BenchY + .4, .9, 7.3f, .2, .3, .1, .927));
        var save = new WorkshopSavedConstruction(construction, new(4));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(save, decoded);
        for (var i = 0; i < construction.Instances.Count; i++)
        {
            var (expected, actual) = (Assert.IsType<WorkshopGenericInstance>(construction.Instances[i]),
                Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[i]));
            Assert.Equal((expected.Cell, expected.Rotation), (actual.Cell, actual.Rotation));
            Assert.True(F32Bits.Equal(expected.Local, actual.Local));
            Assert.Equal(BitConverter.SingleToUInt32Bits(expected.Parameters[MassKey]), BitConverter.SingleToUInt32Bits(actual.Parameters[MassKey]));
            // The stored record is the generic one: its part id and one declared parameter, no typed field.
            var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(i)!.Value;
            Assert.Equal(("weight", 1), (record.PartId, record.ParametersLength));
        }
        // The reloaded construction runs as the saved one: the same bodies, poses and motion within the 0.05 mm late-motion bound.
        var (_, run, _) = await WorkshopIntegrationHarness.ObserveReset(construction, Rate, includeBuilt: true);
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(decoded.Construction, Rate, includeBuilt: true);
        AssertSamePoses(run[^1], replay[^1], .00005f, 1e-4f, 1e-3f, "the reloaded run");
    }

    // ---------------- pendulum qualification (Story 10.4, CAT-067) ----------------

    private static float TieOffsetY(float mass) => 0.5771f * (0.136734f * mass + 0.453070f);

    private static WorkshopConstruction SuspendedPendulum(float mass, float ropeLength, float angleRad,
        Vector3 anchorPos, float angleZRad, float initialRadialDistance)
    {
        var anchorInstance = WorkshopInput.Generic(new("rope_anchor"), new(1),
            anchorPos.X, anchorPos.Y, anchorPos.Z - 0.18f, 0f, 0f, 0f, 1f);

        var tieOffset = TieOffsetY(mass);
        var sinX = MathF.Sin(angleRad);
        var cosX = MathF.Cos(angleRad);
        var sinZ = MathF.Sin(angleZRad);
        var cosZ = MathF.Cos(angleZRad);

        var tieX = anchorPos.X + initialRadialDistance * sinX * cosZ;
        var tieZ = anchorPos.Z + initialRadialDistance * sinZ;
        var tieY = anchorPos.Y - initialRadialDistance * cosX * cosZ;

        var weightInstance = Placed(2, tieX, tieY - tieOffset, tieZ, mass);

        var connection = new WorkshopConnection(new(1), WorkshopSocket.Tie, new(2), WorkshopSocket.Tie,
            WorkshopConnectionDomain.Rope, ropeLength);

        return new(new(2), WorkshopCadenceSettings.Default(),
            new(anchorInstance, weightInstance),
            Connections: new(connection));
    }

    private static WorkshopConstruction SuspendedPendulum(float mass, float ropeLength, float angleRad, Vector3 anchorPos) =>
        SuspendedPendulum(mass, ropeLength, angleRad, anchorPos, 0f, ropeLength);

    /// <summary>Measures the oscillation period by tracking zero-crossings of the X displacement with linear interpolation.</summary>
    private static float MeasurePeriod(List<WorkshopRead> reads, GpuBodyId id)
    {
        if (reads.Count == 0) throw new ArgumentException("Empty reads list.", nameof(reads));
        var zeroCrossings = new List<float>();
        var prevX = Origin(Find(reads[0], id)).X;

        for (var i = 1; i < reads.Count; i++)
        {
            var body = Find(reads[i], id);
            var x = Origin(body).X;

            if ((prevX > 0f && x <= 0f) || (prevX < 0f && x >= 0f))
            {
                var dt = 1f / 120f;
                var frac = MathF.Abs(prevX) / (MathF.Abs(prevX) + MathF.Abs(x));
                var tZero = (i - 1) / 120f + frac * dt;
                zeroCrossings.Add(tZero);
            }
            prevX = x;
        }

        // Canonical period: require at least 3 zero-crossings (1 full cycle).
        if (zeroCrossings.Count >= 3)
        {
            return zeroCrossings[2] - zeroCrossings[0];
        }
        throw new InvalidOperationException($"Insufficient zero crossings detected: {zeroCrossings.Count}");
    }

    private static (WorkshopConstruction Construction, GpuBodyId WeightA, GpuBodyId Pulley1, GpuBodyId Pulley2, GpuBodyId WeightB)
        TwoPulleyCounterweight(float massA, float massB, float heightA, float heightB, bool reverseWiring = false)
    {
        var a = Placed(1, -2.4f, heightA, 0f, massA);
        var p1 = WorkshopInput.Generic(new("pulley"), new(2), -2f, 6f, -.12f, 0, 0, 0, 1);
        var p2 = WorkshopInput.Generic(new("pulley"), new(3), 2f, 6f, -.12f, 0, 0, 0, 1);
        var b = Placed(4, 2.4f, heightB, 0f, massB);
        var tieA = DeclaredPorts.Current.LocalPosition(a, WorkshopSocket.Tie);
        var tieB = DeclaredPorts.Current.LocalPosition(b, WorkshopSocket.Tie);
        var posA = new Vector3(-2.4f, heightA, 0f) + tieA;
        var posB = new Vector3(2.4f, heightB, 0f) + tieB;
        var guides = new (Vector3 Position, Quaternion Rotation)[]
        {
            (new(-2f, 6f, -.12f), Quaternion.Identity),
            (new(2f, 6f, -.12f), Quaternion.Identity)
        };
        var evaluated = RopeGeometry.EvaluateRoute(posA, posB, guides);
        var len = evaluated.TotalLength;
        var span1 = 4.0f;
        var remaining = MathF.Max(0.1f, len - span1);
        var distA = Vector3.Distance(posA, guides[0].Position);
        var distB = Vector3.Distance(posB, guides[1].Position);
        var span0 = remaining * (distA / (distA + distB));
        var span2 = remaining - span0;

        WorkshopConnection link0, link1, link2;
        if (!reverseWiring)
        {
            link0 = new WorkshopConnection(a.Id, WorkshopSocket.Tie, p1.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span0);
            link1 = new WorkshopConnection(p1.Id, WorkshopSocket.Tie, p2.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span1);
            link2 = new WorkshopConnection(p2.Id, WorkshopSocket.Tie, b.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span2);
        }
        else
        {
            link0 = new WorkshopConnection(b.Id, WorkshopSocket.Tie, p2.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span2);
            link1 = new WorkshopConnection(p2.Id, WorkshopSocket.Tie, p1.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span1);
            link2 = new WorkshopConnection(p1.Id, WorkshopSocket.Tie, a.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span0);
        }
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(a, p1, p2, b),
            Connections: new(link0, link1, link2));
        return (construction, a.Id, p1.Id, p2.Id, b.Id);
    }

    [Fact]
    public async Task SuspendedPendulum_OscillatesWithTheoreticalNaturalPeriod()
    {
        const float mass = 4.0f;
        const float length = 2.0f;
        var angle = 15f * MathF.PI / 180f; // 15 deg
        var construction = SuspendedPendulum(mass, length, angle, new(0, 6, 0));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var reads = await harness.Advance(600); // 5 seconds at 120 Hz

        var measuredPeriod = MeasurePeriod(reads, new(2));
        var effectiveLength = length + (TieOffsetY(mass) - HalfHeight(mass));
        var theoreticalPeriod = 2f * MathF.PI * MathF.Sqrt(effectiveLength / G);

        // Measured period is within 5% of theoretical compound pendulum period T = 2π√(L/g).
        Assert.InRange(measuredPeriod, theoreticalPeriod * 0.95f, theoreticalPeriod * 1.05f);

        // Swings through the release plane symmetrically to negative X.
        var minX = float.MaxValue;
        foreach (var read in reads)
        {
            var x = Origin(Find(read, new(2))).X;
            if (x < minX) minX = x;
        }
        Assert.True(minX < -0.45f, $"The swinging weight must reach negative excursion: {minX}");
    }

    [Fact]
    public async Task SuspendedPendulum_PeriodScalesWithSquareRootOfLength()
    {
        const float mass = 4.0f;
        var angle = 15f * MathF.PI / 180f;

        // Compare L1 = 1.0m vs L2 = 4.0m
        var c1 = SuspendedPendulum(mass, 1.0f, angle, new(0, 7, 0));
        var c2 = SuspendedPendulum(mass, 4.0f, angle, new(0, 7, 0));

        await using var h1 = await WorkshopIntegrationHarness.Running(c1);
        var reads1 = await h1.Advance(600);
        var period1 = MeasurePeriod(reads1, new(2));

        await using var h2 = await WorkshopIntegrationHarness.Running(c2);
        var reads2 = await h2.Advance(1000); // 8.3 seconds for longer pendulum
        var period2 = MeasurePeriod(reads2, new(2));

        var effL1 = 1.0f + (TieOffsetY(mass) - HalfHeight(mass));
        var effL2 = 4.0f + (TieOffsetY(mass) - HalfHeight(mass));
        var expectedRatio = MathF.Sqrt(effL2 / effL1);
        var measuredRatio = period2 / period1;

        // Period ratio scales with √(L2 / L1) within 5%.
        Assert.InRange(measuredRatio, expectedRatio * 0.95f, expectedRatio * 1.05f);
        Assert.True(period2 > period1 * 1.6f, $"Longer pendulum period {period2}s must exceed shorter period {period1}s.");
    }

    [Fact]
    public async Task SuspendedPendulum_PeriodIsInvariantWithMass()
    {
        var angle = 15f * MathF.PI / 180f;
        const float targetEffLength = 2.5f;

        // Keep effective length L_eff = ropeLength + (TieOffsetY(mass) - HalfHeight(mass)) identical for both:
        var ropeLight = targetEffLength - (TieOffsetY(0.25f) - HalfHeight(0.25f));
        var ropeHeavy = targetEffLength - (TieOffsetY(8.0f) - HalfHeight(8.0f));

        var cLight = SuspendedPendulum(0.25f, ropeLight, angle, new(0, 6, 0));
        var cHeavy = SuspendedPendulum(8.0f, ropeHeavy, angle, new(0, 6, 0));

        await using var hLight = await WorkshopIntegrationHarness.Running(cLight);
        var readsLight = await hLight.Advance(600);
        var periodLight = MeasurePeriod(readsLight, new(2));

        await using var hHeavy = await WorkshopIntegrationHarness.Running(cHeavy);
        var readsHeavy = await hHeavy.Advance(600);
        var periodHeavy = MeasurePeriod(readsHeavy, new(2));

        var theoreticalPeriod = 2f * MathF.PI * MathF.Sqrt(targetEffLength / G);

        // Both periods conform to T = 2π√(L/g) within 5% of theoretical and within 3% of each other.
        Assert.InRange(periodLight, theoreticalPeriod * 0.95f, theoreticalPeriod * 1.05f);
        Assert.InRange(periodHeavy, theoreticalPeriod * 0.95f, theoreticalPeriod * 1.05f);
        Assert.InRange(MathF.Abs(periodHeavy - periodLight) / periodLight, 0f, 0.03f);
    }

    [Fact]
    public async Task SuspendedPendulum_DampsGraduallyWithoutEnergyGrowth()
    {
        const float mass = 4.0f;
        const float length = 2.0f;
        var angle = 15f * MathF.PI / 180f;
        var construction = SuspendedPendulum(mass, length, angle, new(0, 6, 0));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var reads = await harness.Advance(1200); // 10 seconds at 120 Hz (~3.2 full cycles)

        var peakExcursions = new List<float>();
        var prevVx = Velocity(Find(reads[0], new(2))).X;

        // Initial mechanical energy: at t = 0 with v = 0, E0 = m * g * (y_com - y_lowest)
        var lowestY = 6f - length - TieOffsetY(mass) + HalfHeight(mass);
        var initialComY = Centre(Find(reads[0], new(2))).Y;
        var initialEnergy = mass * G * (initialComY - lowestY);

        for (var i = 1; i < reads.Count; i++)
        {
            var body = Find(reads[i], new(2));
            var com = Centre(body);
            var v = Velocity(body);
            var w = Spin(body);

            // Total energy: potential + linear kinetic + rotational kinetic in local principal frame
            var potential = mass * G * (com.Y - lowestY);
            var kineticLin = 0.5f * mass * v.LengthSquared();
            // Solid cylinder transverse moment: m * (r²/4 + h²/3), axial: 0.5 * m * r²
            var r = Radius(mass);
            var h = HalfHeight(mass);
            var iTransverse = mass * (r * r / 4f + h * h / 3f);
            var iAxial = 0.5f * mass * r * r;
            var wLocal = Vector3.Transform(w, Quaternion.Inverse(Rotation(body)));
            var kineticRot = 0.5f * (iTransverse * (wLocal.X * wLocal.X + wLocal.Z * wLocal.Z) + iAxial * wLocal.Y * wLocal.Y);
            var totalEnergy = potential + kineticLin + kineticRot;

            // Total mechanical energy must not grow beyond initial energy (tolerance for numerical integration)
            Assert.True(totalEnergy <= initialEnergy * 1.005f,
                $"Tick {i}: energy {totalEnergy:F3}J exceeded initial energy {initialEnergy:F3}J.");

            // Track peaks (where Vx changes sign)
            var vx = v.X;
            if (MathF.Sign(prevVx) != MathF.Sign(vx) && MathF.Abs(prevVx) > 0.005f)
            {
                peakExcursions.Add(MathF.Abs(Origin(body).X));
            }
            prevVx = vx;
        }

        // Over 10 seconds, multiple turning points are recorded and amplitude damps gradually.
        Assert.True(peakExcursions.Count >= 4, $"Expected at least 4 turning points, found {peakExcursions.Count}");
        var firstPeak = peakExcursions[0];
        var lastPeak = peakExcursions[^1];
        Assert.True(lastPeak < firstPeak, $"Oscillation must damp: initial peak {firstPeak:F3}m, final peak {lastPeak:F3}m");
    }

    [Fact]
    public async Task SuspendedPendulum_ThreeDimensionalNonPlanarPrecession()
    {
        const float mass = 4.0f;
        const float length = 2.0f;
        var angleX = 15f * MathF.PI / 180f;
        var angleZ = 10f * MathF.PI / 180f;
        var anchorPos = new Vector3(0, 6, 0);

        var construction = SuspendedPendulum(mass, length, angleX, anchorPos, angleZ, length);
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var reads = await harness.Advance(600); // 5 seconds

        var visitedQuadPlusPlus = false;
        var visitedQuadMinusMinus = false;

        foreach (var read in reads)
        {
            var body = Find(read, new(2));
            var pos = Origin(body);
            var x = pos.X;
            var z = pos.Z;

            if (x > 0.05f && z > 0.05f) visitedQuadPlusPlus = true;
            if (x < -0.05f && z < -0.05f) visitedQuadMinusMinus = true;

            // Tether length from anchor to tie socket remains bounded
            var tiePos = pos + Vector3.Transform(new Vector3(0f, TieOffsetY(mass), 0f), Rotation(body));
            var dist = Vector3.Distance(anchorPos, tiePos);
            Assert.InRange(dist, length - 0.05f, length + 0.05f);

            // Tilt remains bounded (no tumbling)
            Assert.True(Tilt(body) < 45.0, $"Tilt exceeded 45 degrees: {Tilt(body)}");
        }

        // Swings in 3D through the plumb line across release plane
        Assert.True(visitedQuadPlusPlus && visitedQuadMinusMinus, "3D pendulum must swing across release plane.");
    }

    [Fact]
    public async Task SuspendedPendulum_SteepAngleSlackReleaseTransitionsSmoothly()
    {
        const float mass = 4.0f;
        const float ropeLength = 2.0f;
        const float initialDistance = 1.2f; // 0.8m slack initially
        var angle = 75f * MathF.PI / 180f; // 75 degrees steep release
        var anchorPos = new Vector3(0, 6, 0);

        var construction = SuspendedPendulum(mass, ropeLength, angle, anchorPos, 0f, initialDistance);
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var reads = await harness.Advance(480); // 4 seconds

        // Verify initial free fall: at tick 12 (0.1s), body fell ~0.5*g*t² under gravity while rope is slack
        var initial = Origin(Find(reads[0], new(2)));
        var early = Origin(Find(reads[12], new(2)));
        Assert.True(early.Y < initial.Y - 0.03f, "Weight must fall freely under gravity while rope is slack.");

        var swungAcross = false;
        foreach (var read in reads)
        {
            var body = Find(read, new(2));
            var pos = Origin(body);
            var vel = Velocity(body);

            // Kinematic states remain strictly finite (no solver blowup on taut impact)
            Assert.True(float.IsFinite(pos.X) && float.IsFinite(pos.Y) && float.IsFinite(pos.Z));
            Assert.True(float.IsFinite(vel.X) && float.IsFinite(vel.Y) && float.IsFinite(vel.Z));

            if (pos.X < -0.5f) swungAcross = true;
        }

        Assert.True(swungAcross, "Slack-released pendulum must transition through taut shock into swing across release plane.");
    }

    [Fact]
    public async Task Counterweight_LiftsLighterLoadOverPulley()
    {
        // 4 kg counterweight at height 3.5m lifts 1 kg load at height 2.0m
        var (construction, idA, _, _, idB) = TwoPulleyCounterweight(massA: 4.0f, massB: 1.0f, heightA: 3.5f, heightB: 2.0f);
        await using var harness = await WorkshopIntegrationHarness.Running(construction);

        var initialA = Find(harness.Committed, idA);
        var initialB = Find(harness.Committed, idB);

        var reads = await harness.Advance(60); // 0.5 seconds at 120 Hz

        var finalA = Find(reads[^1], idA);
        var finalB = Find(reads[^1], idB);

        var deltaA = Origin(finalA).Y - Origin(initialA).Y;
        var deltaB = Origin(finalB).Y - Origin(initialB).Y;

        // Heavy counterweight descends, light weight is lifted upward
        Assert.True(deltaA < -0.2f, $"4kg counterweight must descend: {deltaA}m");
        Assert.True(deltaB > +0.2f, $"1kg load must be lifted: {deltaB}m");

        // Displacements are opposite and equal magnitude
        Assert.InRange(MathF.Abs(deltaA + deltaB), 0f, 0.05f);
    }

    [Fact]
    public async Task Counterweight_EqualMassesOverPulleyRemainBalanced()
    {
        // Equal 4 kg weights connected over pulleys
        var (construction, idA, _, _, idB) = TwoPulleyCounterweight(massA: 4.0f, massB: 4.0f, heightA: 3.0f, heightB: 3.0f);
        await using var harness = await WorkshopIntegrationHarness.Running(construction);

        var initialA = Find(harness.Committed, idA);
        var initialB = Find(harness.Committed, idB);

        var reads = await harness.Advance(600); // 5 seconds at 120 Hz

        var finalA = Find(reads[^1], idA);
        var finalB = Find(reads[^1], idB);

        var deltaA = Origin(finalA).Y - Origin(initialA).Y;
        var deltaB = Origin(finalB).Y - Origin(initialB).Y;

        // Equal masses remain in static equilibrium over 600 ticks
        Assert.InRange(MathF.Abs(deltaA), 0f, 0.005f);
        Assert.InRange(MathF.Abs(deltaB), 0f, 0.005f);
        Assert.InRange(MathF.Abs(Velocity(finalA).Y), 0f, 0.005f);
        Assert.InRange(MathF.Abs(Velocity(finalB).Y), 0f, 0.005f);
    }

    [Fact]
    public async Task Counterweight_FloorContactRelaxesTensionAndRestsOnBench()
    {
        // Solid shelf placed under the descending 4 kg counterweight at height 2.0m (top surface at y = 2.25m).
        var shelf = WorkshopInput.Generic(new("wall"), new(5), -2.4f, 2.0f, 0f, 0, 0, 0, 1,
            new([new(new("width"), 1.5f), new(new("height"), .5f), new(new("thickness"), 1.5f)]));
        var (construction, idA, _, _, _) = TwoPulleyCounterweight(massA: 4.0f, massB: 1.0f, heightA: 2.8f, heightB: 2.0f);
        construction = construction with { Instances = construction.Instances.With(shelf) };

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var reads = await harness.Advance(180); // 1.5 seconds at 120 Hz

        // Counterweight lands on the shelf surface at y ~ 2.25m and rests stably
        var finalA = Find(reads[^1], idA);
        var contactY = Origin(finalA).Y;
        Assert.InRange(contactY, 2.24f, 2.30f);
        Assert.InRange(MathF.Abs(Velocity(finalA).Y), 0f, 0.05f);

        // Rope presentation relaxes into Slack state upon floor/shelf contact
        var hadSlackState = false;
        foreach (var read in reads)
        {
            if (read.Presentation.Ropes[0].State == RopeState.Slack)
                hadSlackState = true;
        }
        Assert.True(hadSlackState, "Rope must transition to slack upon shelf contact.");
    }

    [Fact]
    public async Task Counterweight_OpenRouteFallsFreelyToBench()
    {
        // Solid shelf placed under the falling load at height 1.0m (top surface at y = 1.25m).
        var shelf = WorkshopInput.Generic(new("wall"), new(3), -2.0f, 1.0f, 0f, 0, 0, 0, 1,
            new([new(new("width"), 1.5f), new(new("height"), .5f), new(new("thickness"), 1.5f)]));
        var a = Placed(1, -2.0f, 3.0f, 0f, 4.0f);
        var p1 = WorkshopInput.Generic(new("pulley"), new(2), -2f, 6f, -.12f, 0, 0, 0, 1);
        var tieA = DeclaredPorts.Current.LocalPosition(a, WorkshopSocket.Tie);
        var span = Vector3.Distance(new Vector3(-2f, 3f, 0f) + tieA, new Vector3(-2f, 6f, -.12f));
        var openConnection = new WorkshopConnection(a.Id, WorkshopSocket.Tie, p1.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, span);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(a, p1, shelf),
            Connections: new(openConnection));

        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var reads = await harness.Advance(180); // 1.5 seconds

        // The weight on an open incomplete route falls freely under gravity to the shelf surface at y ~ 1.25m
        var finalA = Find(reads[^1], a.Id);
        Assert.InRange(Origin(finalA).Y, 1.24f, 1.30f);
    }

    [Fact]
    public async Task Counterweight_PulleyWiringDirectionInvariance()
    {
        // Forward wiring: A -> P1 -> P2 -> B
        var (cForward, idA_F, _, _, _) = TwoPulleyCounterweight(4.0f, 1.0f, 3.5f, 2.0f, reverseWiring: false);
        // Reverse wiring: B -> P2 -> P1 -> A
        var (cReverse, idA_R, _, _, _) = TwoPulleyCounterweight(4.0f, 1.0f, 3.5f, 2.0f, reverseWiring: true);

        await using var hF = await WorkshopIntegrationHarness.Running(cForward);
        await using var hR = await WorkshopIntegrationHarness.Running(cReverse);

        var readsF = await hF.Advance(60);
        var readsR = await hR.Advance(60);

        var deltaA_F = Origin(Find(readsF[^1], idA_F)).Y - Origin(Find(readsF[0], idA_F)).Y;
        var deltaA_R = Origin(Find(readsR[^1], idA_R)).Y - Origin(Find(readsR[0], idA_R)).Y;

        // Displacements match regardless of wiring direction
        Assert.InRange(MathF.Abs(deltaA_F - deltaA_R), 0f, 0.01f);
    }

    [Fact]
    public async Task SuspendedPendulum_ResetRestoresAuthoredPoseAndZeroVelocity()
    {
        const float mass = 4.0f;
        const float length = 2.0f;
        var angle = 15f * MathF.PI / 180f;
        var construction = SuspendedPendulum(mass, length, angle, new(0, 6, 0));

        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(construction, 600, SimulationCadence.Hz120, includeBuilt: true);

        // Reset restores authored pose within 1 um and 1 urad
        AssertSamePoses(built, reset, 1e-6f, 1e-6f, 1e-6f, "Reset");
        var resetBody = Find(reset, new(2));
        Assert.True(Velocity(resetBody).Length() == 0f && Spin(resetBody).Length() == 0f, "at rest after Reset");

        // During the run, the pendulum swung away from its authored pose
        var maxDisplacement = 0f;
        foreach (var read in run)
        {
            var d = Vector3.Distance(Origin(Find(read, new(2))), Origin(Find(built, new(2))));
            if (d > maxDisplacement) maxDisplacement = d;
        }
        Assert.True(maxDisplacement > 0.8f, $"Swinging pendulum moved significantly: {maxDisplacement}m");

        // Replay repeats terminal and milestone ticks
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(construction, 600, SimulationCadence.Hz120, includeBuilt: true);
        Assert.Equal(run.Count, replay.Count);
        AssertSamePoses(run[^1], replay[^1], 0.00005f, 1e-4f, 1e-3f, "replay terminal tick");
        foreach (var t in new[] { 0, 150, 300, 450 })
        {
            AssertSamePoses(run[t], replay[t], 0.00005f, 1e-4f, 1e-3f, $"replay milestone tick {t}");
        }
    }

    [Fact]
    public async Task SuspendedPendulum_SaveLoadRoundtripPreservesSwingingState()
    {
        const float mass = 4.0f;
        const float length = 2.0f;
        var angle = 15f * MathF.PI / 180f;
        var construction = SuspendedPendulum(mass, length, angle, new(0, 6, 0));

        // Save roundtrip of construction
        var save = new WorkshopSavedConstruction(construction, new(4));
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(save, decoded);

        // Both original and decoded constructions run identically
        var (_, run, _) = await WorkshopIntegrationHarness.ObserveReset(construction, 600, SimulationCadence.Hz120, includeBuilt: true);
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(decoded.Construction, 600, SimulationCadence.Hz120, includeBuilt: true);
        Assert.Equal(run.Count, replay.Count);
        AssertSamePoses(run[^1], replay[^1], 0.00005f, 1e-4f, 1e-3f, "saved vs reloaded terminal tick");
        foreach (var t in new[] { 0, 150, 300, 450 })
        {
            AssertSamePoses(run[t], replay[t], 0.00005f, 1e-4f, 1e-3f, $"saved vs reloaded milestone tick {t}");
        }
    }
}
