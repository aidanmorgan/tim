using System.Numerics;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

/// <summary>
/// Story 10.2a (CAT-067): the Weight is its package's declaration data only, a dynamic solid cylinder whose <c>mass</c> parameter
/// (0.25–8 kg, default 4) is its compiled mass and, through four linear collider bindings, its size; the body origin is the base
/// centre. Every behaviour here runs the real Workshop construction through <see cref="WorkshopPhysicsCompiler"/> and the shipped
/// C# engine (<see cref="WorkshopSimulation"/> on the worker transport). Expectations are derived from the declared values (binary16
/// where the ABI narrows them), never from observed output.
/// </summary>
public sealed class WeightTests
{
    internal static readonly PartId Weight = new("weight");
    private static readonly ParameterId MassKey = new("mass");
    private static readonly float BenchY = (float)WorkshopConstruction.WorkbenchSurface.Value;
    /// <summary>The admitted gravity (the body record's binary16 lane).</summary>
    private static readonly float G = -(float)(Half)(-9.81f);
    private const double DominoUpright = -0.46 + .55;

    /// <summary>The declared size law in f32, as the shared evaluator computes it (multiplier·m + offset).</summary>
    private static float Radius(float mass) => .04808397f * mass + .15932827f;
    private static float HalfHeight(float mass) => .03818433f * mass + .12652539f;
    private static float Scale(float mass) => .136734f * mass + .453070f;
    private static float H(float value) => (float)(Half)value;

    internal static WorkshopGenericInstance Placed(ulong id, double x, double y, double z, float mass,
        double qx = 0, double qy = 0, double qz = 0, double qw = 1) =>
        WorkshopInput.Generic(Weight, new(id), x, y, z, qx, qy, qz, qw, new([new(MassKey, mass)]));

    private static WorkshopConstruction Bench(params IWorkshopInstance[] parts) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(parts));

    // ---------------- declaration ----------------

    [Fact]
    public void TheWeightIsAMassBoundDynamicCylinderWithNoPortAndNoPayloadRole()
    {
        var entry = DeclaredPhysics.Of(Weight);
        var mass = Assert.Single(entry.Metadata.Parameters);
        Assert.Equal(new CuriousContraptions.Abstractions.ParameterDescriptor("mass", CuriousContraptions.Abstractions.ParameterUnit.Kilogram,
            CuriousContraptions.Abstractions.ParameterKind.Real, .25f, 8f, 4f, mass.Options,
            CuriousContraptions.Abstractions.ParameterEditorPolicy.Clamp, CuriousContraptions.Abstractions.ParameterRunEdit.BuildOnly), mass);
        Assert.Empty(mass.Options);
        Assert.Equal(CuriousContraptions.Abstractions.Mechanical.MechanicalValue.FromParameter("mass"), entry.Mechanical!.MassParameter);
        Assert.Equal(CuriousContraptions.Abstractions.Mechanical.KinematicMode.Dynamic, entry.Mechanical.KinematicMode);
        Assert.Equal(4f, entry.Mechanical.MassProperties.Mass);
        Assert.Equal(CuriousContraptions.Abstractions.ColliderShapeKind.Cylinder, Assert.Single(entry.Colliders).ShapeKind);
        Assert.Empty(entry.Ports);
        Assert.Empty(DeclaredPorts.Current.Of(Weight).ToArray());
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
        Assert.Equal(new Kilograms((Half)mass), Assert.Single(WorkshopPhysicsCompiler.Compile(Bench(weight), new(1, 2)).Bodies.ToArray(),
            body => body.Id == weight.Id).Mass);
        var bytes = PayloadInstanceTests.GenericSave("weight", null, [("mass", mass)]);
        var loaded = Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes).Construction.Instances));
        Assert.Equal(BitConverter.SingleToUInt32Bits(mass), BitConverter.SingleToUInt32Bits(loaded.Parameters[MassKey]));
        var command = Assert.IsType<WorkshopGenericInstance>(WorkshopWireFlatBuffers.MapInstance(WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value));
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

        var bytes = PayloadInstanceTests.GenericSave("weight", null, [("mass", mass)]);
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
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
            var bytes = PayloadInstanceTests.GenericSave("weight", null, parameters);
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
            var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
            Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
        }
        // A typed record field is no Weight field either.
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,
            PayloadInstanceTests.GenericSave("weight", TypedField.Length, [("mass", 4f)])));
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
        Assert.Equal(new Kilograms((Half)mass), body.Mass);
        Assert.Equal(new InverseSeconds((Half)0), body.LinearDrag);
        Assert.Equal(new AccelerationVector((Half)0, (Half)(-9.81), (Half)0), body.Gravity);
        var cylinder = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id);
        float r = Radius(mass), h = HalfHeight(mass);
        Assert.Equal(ColliderShapeKind.Cylinder, cylinder.Shape);
        Assert.Equal(new Metres((Half)r), cylinder.Radius);
        Assert.Equal(new MetreVector((Half)0, (Half)h, (Half)0), cylinder.HalfExtents);
        // The base-centre origin: the cylinder's centre, and so the centre of mass, is h above it.
        Assert.Equal(new MetreVector((Half)0, (Half)h, (Half)0), cylinder.Pose.Translation);
        Assert.Equal(CanonicalRotation.Identity, cylinder.Pose.Rotation);
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == cylinder.Material);
        Assert.Equal((new Restitution((Half).08), new FrictionCoefficient((Half).3), new RollingResistance((Half).03), new LinearSpeed((Half).1)),
            (material.Restitution, material.Friction, material.RollingResistance, material.BounceThreshold));

        var properties = RigidMassProperties.Compile(body, cylinder);
        Assert.Equal(new MetreVector((Half)0, (Half)h, (Half)0), properties.LocalCentreOfMass);
        double m = (double)(Half)mass, hr = (double)(Half)r, hh = (double)(Half)h;
        // Six binary16 roundings at most (10.1's bound): 6 x 2^-11 relative.
        const double bound = 6.0 / 2048;
        Near(properties.Y, m * hr * hr / 2);
        Near(properties.X, m * (hr * hr / 4 + hh * hh / 3));
        Near(properties.Z, m * (hr * hr / 4 + hh * hh / 3));
        void Near(PrincipalInertia value, double expected)
        {
            var actual = Math.ScaleB((double)value.Mantissa, value.Exponent);
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
        var scene = Assert.Single(entry.Colliders);
        var bound = Assert.Single(CuriousContraptions.Bundle.BundleGeometry.Colliders(entry, new Dictionary<string, float> { ["mass"] = 4f }));
        Assert.Equal(scene, bound);
        Assert.Equal(new Vector3(Radius(4), HalfHeight(4), Radius(4)), scene.HalfExtents);
        Assert.Equal(new Vector3(0, HalfHeight(4), 0), scene.Offset);
        // The declared fixed mass properties are the 4 kg values (the compiler derives them per instance from the bound collider).
        var declared = entry.Mechanical!.MassProperties;
        Assert.Equal(new Vector3(0, HalfHeight(4), 0), declared.CentreOfMass);
        Assert.InRange(Math.Abs(declared.InertiaTensor.Y - 4 * Radius(4) * Radius(4) / 2), 0, 1e-5);
        Assert.InRange(Math.Abs(declared.InertiaTensor.X - 4 * (Radius(4) * Radius(4) / 4 + HalfHeight(4) * HalfHeight(4) / 3)), 0, 1e-5);
    }

    [Fact]
    public void TheSizeStrictlyIncreasesWithMass()
    {
        var masses = new[] { .25f, 1f, 4f, 8f };
        Half[] Compiled(Func<ColliderDeclaration, Half> lane) => masses.Select(mass =>
            lane(Assert.Single(WorkshopPhysicsCompiler.Compile(Bench(Placed(1, 0, 1, 0, mass)), new(1, 2)).Colliders.ToArray(),
                c => c.Body == new GpuBodyId(1)))).ToArray();
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
        (float)body.LocalCentreOfMass.X, (float)body.LocalCentreOfMass.Y, (float)body.LocalCentreOfMass.Z), Rotation(body));
    private static double Tilt(PhysicsBodyRead body) =>
        Math.Acos(Math.Clamp(1 - 2 * (body.Rotation.X * body.Rotation.X + body.Rotation.Z * body.Rotation.Z), -1, 1)) * 180 / Math.PI;

    private static PhysicsBodyRead Find(WorkshopRead read, GpuBodyId id)
    {
        for (var i = 0; i < read.Bodies.Count; i++) if (read.Bodies[i].Body.Id == id) return read.Bodies[i];
        throw new ArgumentException($"Body {id.Value} is not read.", nameof(id));
    }

    /// <summary>Total mechanical energy of a Weight of this mass, from its compiled (binary16) mass and size: translational and
    /// rotational kinetic energy about the centre of mass (the solid-cylinder moments in its own frame) and gravitational potential of
    /// the centre of mass, whose height is taken through the body's rotation.</summary>
    private static double Energy(PhysicsBodyRead body, float mass)
    {
        double m = H(mass), r = H(Radius(mass)), h = H(HalfHeight(mass));
        double ix = m * (r * r / 4 + h * h / 3), iy = m * r * r / 2;
        var w = Vector3.Transform(Spin(body), Quaternion.Inverse(Rotation(body)));
        var v = Velocity(body);
        return .5 * m * ((double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z) +
            .5 * (ix * w.X * w.X + iy * w.Y * w.Y + ix * w.Z * w.Z) + m * G * (Centre(body).Y - BenchY);
    }

    /// <summary>The energy a tick may add: lifting the body by the solver's contact slop (m g × 0.2 mm), 10.1's bound.</summary>
    private static double SlopEnergy(float mass) => H(mass) * G * WorkshopConstraintSolver.ContactSlop;

    private static double WorstRise(List<WorkshopRead> reads, GpuBodyId id, float mass)
    {
        double worst = double.NegativeInfinity;
        for (var i = 1; i < reads.Count; i++)
            worst = Math.Max(worst, Energy(Find(reads[i], id), mass) - Energy(Find(reads[i - 1], id), mass));
        return worst;
    }

    /// <summary>The production simulation on the worker transport at a cadence: Construct, Run, `ticks` Advances (the reads include
    /// the built state first), then Reset.</summary>
    internal static async Task<(WorkshopRead Built, List<WorkshopRead> Run, WorkshopRead Reset)> RunAndReset(WorkshopConstruction construction,
        int ticks, SimulationCadence cadence = SimulationCadence.Hz120)
    {
        var settings = WorkshopCadenceSettings.Default() with { Simulation = cadence };
        await using var device = new WorkshopGpuDevice(new BrowserGpuTransport(), new(101, 206));
        await using var simulation = new WorkshopSimulation(device, new WorkshopPhysicsEngineTests.StepClock(), settings, new(1),
            new WorkshopPhysicsEngineTests.NoInstallation(), GpuDeviceOwnership.Borrowed);
        static void Applied(WorkshopCommandResult result, string command) =>
            Assert.True(result.Outcome == WorkshopCommandOutcome.Applied, $"{command}: {result.Outcome} ({result.Reason})");
        Applied(await simulation.Initialize(), "Initialize");
        Applied(await simulation.Construct(construction with { Settings = settings }), "Construct");
        var built = simulation.Committed;
        Applied(await simulation.Run(), "Run");
        var reads = new List<WorkshopRead>(ticks + 1) { built };
        for (var tick = 0; tick < ticks; tick++)
        {
            Applied(await simulation.Advance(), $"Advance {tick + 1}");
            reads.Add(simulation.Committed);
        }
        Applied(await simulation.Reset(), "Reset");
        return (built, reads, simulation.Committed);
    }

    private const int Rate = 120;

    [Theory]
    [InlineData(.25f)]
    [InlineData(4f)]
    [InlineData(8f)]
    public async Task DroppedEndDownItRestsOnItsBaseWithoutSinkingDriftOrEnergyGain(float mass)
    {
        var weight = Placed(1, .3, BenchY + .05, -.2, mass);
        var (_, reads, _) = await RunAndReset(Bench(weight), 10 * Rate);
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
        var cylinder = Assert.Single(WorkshopPhysicsCompiler.Compile(edited, new(1, 2)).Colliders.ToArray(), c => c.Body == light.Id);
        // Its lowest point is the base, at the origin: the centre is exactly the half-height above it.
        Assert.Equal(cylinder.HalfExtents.Y, cylinder.Pose.Translation.Y);
        var (_, reads, _) = await RunAndReset(edited, 3 * Rate);
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
    private static WorkshopGenericInstance Ramp(ulong id)
    {
        var half = -RampDegrees * Math.PI / 360;
        return WorkshopInput.Generic(PartId.Ramp, new(id), RampCentre.X, RampCentre.Y, RampCentre.Z, 0, 0, Math.Sin(half), Math.Cos(half));
    }

    /// <summary>A Weight lying on its side (axis along world Z, centred at z = 0) on the Ramp's top face, `along` metres from its
    /// centre (positive downhill), clear of the face by 3 mm.</summary>
    private static WorkshopGenericInstance OnItsSideOnTheRamp(ulong id, float mass, float along)
    {
        var a = (float)(-RampDegrees * Math.PI / 180);
        var normal = new Vector3(-MathF.Sin(a), MathF.Cos(a), 0);
        var downhill = new Vector3(MathF.Cos(a), MathF.Sin(a), 0);
        var centre = RampCentre + .09f * normal + along * downhill + (Radius(mass) + .003f) * normal;
        var origin = centre - new Vector3(0, 0, HalfHeight(mass));
        return Placed(id, origin.X, origin.Y, origin.Z, mass, Math.Sin(Math.PI / 4), 0, 0, Math.Cos(Math.PI / 4));
    }

    [Fact]
    public async Task OnItsSideItRollsDownARampWithoutSlippingAndComesToRestOnTheBenchWithoutAxialCreep()
    {
        var weight = OnItsSideOnTheRamp(1, 4f, .5f);
        var (_, reads, _) = await RunAndReset(Bench(Ramp(2), weight), 14 * Rate);
        var r = H(Radius(4));
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
        Assert.All(reads, read => Assert.InRange(Centre(Find(read, weight.Id)).Z, -.0005f, .0005f));
        var stopped = reads.FindIndex(2 * Rate, read => Velocity(Find(read, weight.Id)).Length() < .001f);
        Assert.True(stopped > 0 && stopped < reads.Count - 2 * Rate, $"stopped at tick {stopped}");
        var creep = Vector3.Distance(Centre(final), Centre(Find(reads[stopped], weight.Id)));
        Assert.True(creep <= .00005f, $"moved {creep * 1000} mm after stopping");
    }

    [Theory]
    [InlineData(4f, 4f)]
    [InlineData(8f, 1f)]
    public async Task AStackEndDownHoldsWithoutBounceOrCreep(float lower, float upper)
    {
        var bottom = Placed(1, 0, BenchY, 0, lower);
        var top = Placed(2, 0, BenchY + 2 * H(HalfHeight(lower)) + .002f, 0, upper);
        var (_, reads, _) = await RunAndReset(Bench(bottom, top), 5 * Rate);
        // Each resting contact keeps the 0.5 mm envelope: the lower base on the bench, and the upper base on the lower Weight's top face
        // (its base plus the compiled height 2h).
        var final = (Lower: Height(Find(reads[^1], bottom.Id)), Upper: Height(Find(reads[^1], top.Id)));
        Assert.InRange(final.Lower, BenchY - .0005f, BenchY + .0005f);
        var face = final.Lower + 2 * H(HalfHeight(lower));
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

    [Fact]
    public async Task AWeightRollingOffARampTopplesADominoAndARestingWeightBesideOneLeavesItStanding()
    {
        var domino = PlacedParts.Domino(new(3), 3.2, DominoUpright, 0, 0, 0, 0, 1);
        var (_, struck, _) = await RunAndReset(Bench(Ramp(2), OnItsSideOnTheRamp(1, 4f, .5f), domino), 6 * Rate);
        Assert.Contains(struck, read => Tilt(Find(read, domino.Id)) > 45);
        // Control: a Weight resting end-down 0.1 m from the tile's face never moves it.
        var beside = Placed(1, 3.2 - .125 - .1 - Radius(4), BenchY, 0, 4f);
        var (_, quiet, _) = await RunAndReset(Bench(beside, domino), 5 * Rate);
        Assert.All(quiet, read => Assert.True(Tilt(Find(read, domino.Id)) < 1, $"tilt {Tilt(Find(read, domino.Id))}"));
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
        var (built, run, reset) = await RunAndReset(construction, 600, cadence);
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
        var (_, replay, _) = await RunAndReset(construction, 600, cadence);
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
            Assert.Equal(("weight", false, 1), (record.PartId, WorkshopWireFlatBuffers.CarriesTypedField(record), record.ParametersLength));
        }
        // The reloaded construction runs as the saved one: the same bodies, poses and motion within the 0.05 mm late-motion bound.
        var (_, run, _) = await RunAndReset(construction, Rate);
        var (_, replay, _) = await RunAndReset(decoded.Construction, Rate);
        AssertSamePoses(run[^1], replay[^1], .00005f, 1e-4f, 1e-3f, "the reloaded run");
    }
}
