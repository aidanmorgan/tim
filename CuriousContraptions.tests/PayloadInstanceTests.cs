using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>The payload of a construction, as tests name it: the generic instance of a payload package.</summary>
internal static class Payloads
{
    /// <summary>The first payload in instance order: an authored lesson's named body.</summary>
    public static WorkshopGenericInstance First(WorkshopConstruction construction) =>
        Find(construction) ?? throw new ArgumentException("The construction holds no payload.");

    public static WorkshopGenericInstance? Find(WorkshopConstruction construction) =>
        construction.Instances.FirstOrDefault(instance => DeclaredPhysics.IsPayload(instance.PartId)) is WorkshopGenericInstance payload
            ? payload : null;
}

/// <summary>C-Balls (Stories 9A.30, 9A.31 and the native part of 9R.11): the Basketball and the Bowling ball are the payload, the
/// rolling thing the player delivers. Written first against the record path and kept green through the cutover: only
/// <see cref="Place"/> changes. Every behaviour runs the production compiler and the worker's physics engine.</summary>
public sealed class PayloadInstanceTests
{
    private const int TicksPerSecond = 120;
    private static readonly float BenchY = (float)WorkshopConstruction.WorkbenchSurface.Value;

    /// <summary>The one placement seam of this suite.</summary>
    internal static IWorkshopInstance Place(PartId kind, ulong id, double x, double y, double z) =>
        WorkshopInput.Generic(kind, new(id), (float)(x), (float)(y), (float)(z), 0, 0, 0, 1);

    private static WorkshopConstruction Free(params IWorkshopInstance[] instances) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances));

    private static float Height(PhysicsBodyRead body) => (body.Body.Cell.Y + body.Body.Local.Y) / 16f;
    private static float Horizontal(PhysicsBodyRead body) => (body.Body.Cell.X + body.Body.Local.X) / 16f;
    private static PhysicsBodyRead Find(PhysicsBodyReadSet bodies, GpuBodyId id)
    {
        for (var i = 0; i < bodies.Count; i++) if (bodies[i].Body.Id == id) return bodies[i];
        throw new ArgumentException($"Body {id.Value} is not read.", nameof(id));
    }

    // Owner decision 9 Oct 2026 (CAT-001, CAT-014): the literal declared materials, pinned here once so a package edit cannot pass as
    // "package equals package". Both kinds share one bench and each keeps its own material.
    [Fact]
    public void BothKindsCompileTheOwnerDeclaredMaterialsOnOneBench()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Free(Place(PartId.Basketball, 3, -1, 2, 0), Place(PartId.BowlingBall, 4, 1, 2, 0)), new(1, 2));
        foreach (var (id, kind, radius, mass, bounce, rolling) in new[]
        {
            (3UL, PartId.Basketball, .34f, 1f, .55f, .035f),
            (4UL, PartId.BowlingBall, .28f, 4f, .14f, .03f)
        })
        {
            var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(id));
            Assert.Equal(RigidMotionKind.Dynamic, body.Motion);
            Assert.Equal(new Kilograms((float)mass), body.Mass);
            Assert.Equal(new InverseSeconds(.04f), body.LinearDrag);
            Assert.Equal(new AccelerationVector(0f, -9.81f, 0f), body.Gravity);
            var collider = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id && c.Shape == ColliderShapeKind.Sphere);
            Assert.Equal(ColliderShapeKind.Sphere, collider.Shape);
            Assert.Equal(new Metres((float)radius), collider.Radius);
            var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == collider.Material);
            Assert.Equal(new Restitution((float)bounce), material.Restitution);
            Assert.Equal(new FrictionCoefficient(.3f), material.Friction);
            Assert.Equal(new LinearSpeed(.1f), material.BounceThreshold);
            Assert.Equal(new RollingResistance((float)rolling), material.RollingResistance);
            // The package is the f32 authority the compiler narrows from.
            var entry = DeclaredPhysics.Of(kind);
            var sphere = Assert.Single(entry.Colliders, collider => collider.ShapeKind == CuriousContraptions.Abstractions.ColliderShapeKind.Sphere);
            Assert.Equal((radius, mass, .04f, bounce, .3f, .1f, rolling), (sphere.HalfExtents.X, MassFixtures.Evaluate(entry.Mechanical!.MassFrame, entry.Metadata.Parameters).Mass,
                entry.Mechanical.MassFrame.LinearDrag, sphere.Material.Restitution, sphere.Material.Friction,
                sphere.Material.BounceThreshold, sphere.Material.RollingResistance));
        }
    }

    // One owner policy selects every explicitly declared payload, without allocating potential pairs.
    [Fact]
    public void AReceiverDeclaresOnePayloadPolicyWhateverTheInstanceOrder()
    {
        var receiver = WorkshopInput.Generic(PartId.Receiver, new(5), 0, 1, 0, 0, 0, 0, 1);
        var scene = WorkshopPhysicsCompiler.Compile(Free(Place(PartId.BowlingBall, 7, -2, 3, 0), Place(PartId.Basketball, 3, 2, 3, 0), receiver), new(1, 2));
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllPayloads, default), Assert.Single(scene.Sensors.ToArray()).Targets);
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllPayloads, default), Assert.Single(scene.Guides.ToArray()).Targets);
        Assert.All(scene.Bodies.ToArray().Where(body => body.Id.Value is 3 or 7), body => Assert.True(scene.Sensors[0].Targets.Contains(body.Id, body.Payload)));
        Assert.All(scene.Sensors.ToArray(), s => Assert.Equal(receiver.Id, s.Frame));
    }

    // Presentation telemetry follows the lowest-identity payload whatever the instance order, and no body (zero) without one.
    [Fact]
    public void TelemetryFollowsTheLowestIdentityPayloadOrNone()
    {
        var domino = PlacedParts.Domino(new(2), 3, .1f, 0, 0, 0, 0, 1);
        Assert.Equal(new GpuBodyId(3), MachineWorld.TelemetryBody(new(Place(PartId.BowlingBall, 7, -2, 3, 0), domino, Place(PartId.Basketball, 3, 2, 3, 0))));
        Assert.Equal(new GpuBodyId(7), MachineWorld.TelemetryBody(new(domino, Place(PartId.BowlingBall, 7, -2, 3, 0))));
        Assert.Equal(default, MachineWorld.TelemetryBody(new(domino)));
        Assert.Equal(default, MachineWorld.TelemetryBody(WorkshopInstances.Empty));
    }

    // Control: explicit payload membership excludes a Domino even though it is dynamic.
    [Fact]
    public void AReceiverPolicyExcludesNonPayloadBodiesWithoutAllocatingPairState()
    {
        var receiver = WorkshopInput.Generic(PartId.Receiver, new(5), 0, 1, 0, 0, 0, 0, 1);
        var domino = PlacedParts.Domino(new(2), 3, .1f, 0, 0, 0, 0, 1);
        var alone = WorkshopPhysicsCompiler.Compile(Free(domino, receiver), new(1, 2));
        Assert.Equal(BodyTargetKind.AllPayloads, Assert.Single(alone.Sensors.ToArray()).Targets.Kind);
        Assert.Equal(BodyTargetKind.AllPayloads, Assert.Single(alone.Guides.ToArray()).Targets.Kind);
        var nonPayload = alone.Bodies.ToArray().Single(body => body.Id == domino.Id);
        Assert.False(alone.Sensors[0].Targets.Contains(nonPayload.Id, nonPayload.Payload));
        var mixed = WorkshopPhysicsCompiler.Compile(Free(domino, receiver, Place(PartId.Basketball, 9, -3, 2, 0)), new(1, 2));
        var payload = mixed.Bodies.ToArray().Single(body => body.Id == new GpuBodyId(9));
        Assert.True(Assert.Single(mixed.Sensors.ToArray()).Targets.Contains(payload.Id, payload.Payload));
        Assert.True(Assert.Single(mixed.Guides.ToArray()).Targets.Contains(payload.Id, payload.Payload));
    }

    // Multiple owner policies coexist with payloads; actual pair state begins only on interaction.
    [Fact]
    public void TwoCaptureOwnersAdmitWithEitherPayloadKindAndNonPayloadBodies()
    {
        var first = WorkshopInput.Generic(PartId.Receiver, new(5), 0, 1, 0, 0, 0, 0, 1);
        var second = WorkshopInput.Generic(PartId.Receiver, new(6), 4, 1, 0, 0, 0, 0, 1);
        var domino = PlacedParts.Domino(new(2), 3, .1f, 0, 0, 0, 0, 1);
        new WorkshopInstances(first, second, domino).Validate();
        foreach (var kind in new[] { PartId.Basketball, PartId.BowlingBall })
        {
            var construction = Free(first, second, domino, Place(kind, 9, -3, 2, 0));
            construction.Validate();
            var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
            Assert.Equal(2, scene.Sensors.Length);
            Assert.Equal(2, scene.Guides.Length);
            Assert.Empty(PhysicsInteractionSnapshot.Empty.Captures);
        }
        // One capture owner guides any number of payloads.
        new WorkshopInstances(first, domino, Place(PartId.Basketball, 9, -3, 2, 0), Place(PartId.BowlingBall, 10, -5, 2, 0)).Validate();
    }

    // In an authored lesson every contact trigger and finite-work Bumper names the lesson's payload; in free play they act on every
    // dynamic body.
    [Fact]
    public void LessonTriggersAndBumpersNameTheLessonPayload()
    {
        var named = 0;
        foreach (var lesson in new[] { "first_principles", "spring_forward", "ready_to_rebound", "bumper_sidekick", "bumper_depth",
                     "wall_and_bumper", "domino_effect", "delayed_signal", "clear_pipe", "gentle_bend", "quarter_bend", "joined_pipe" })
        {
            var level = PuzzleCatalogue.Create(new(lesson), new(1), WorkshopCadenceSettings.Default(), new(11), new(.45f));
            // A lesson that offers a Bumper gets one placed from its inventory, so the player's Bumper is named too.
            if (WorkshopInventoryPolicy.Authored(level.Puzzle).ContainsKey(PartId.PinballBumper))
                level = level.WithInstance(WorkshopInput.Generic(PartId.PinballBumper, new(900), 3, 1, 0, 0, 0, 0, 1));
            var payload = Assert.Single(level.Instances, instance => instance.PartId == PartId.Basketball).Id;
            var scene = WorkshopPhysicsCompiler.Compile(level, new(1, 2));
            foreach (var targets in scene.Triggers.ToArray().Select(t => t.Targets).Concat(scene.ContactWorks.ToArray().Select(w => w.Targets)))
            {
                Assert.Equal(new BodyTargetSet(BodyTargetKind.NamedBody, payload), targets);
                named++;
            }
            var free = WorkshopPhysicsCompiler.Compile(level with { Puzzle = default }, new(1, 2));
            Assert.All(free.Triggers.ToArray().Select(t => t.Targets).Concat(free.ContactWorks.ToArray().Select(w => w.Targets)),
                targets => Assert.Equal(BodyTargetKind.AllDynamic, targets.Kind));
        }
        Assert.True(named >= 2, $"lessons name their payload in {named} trigger/work rows");
    }

    // Placement boundaries: the highest and lowest admitted heights build and compile; one step beyond is refused.
    [Theory]
    [InlineData("ball")]
    [InlineData("bowling")]
    public void PlacementBoundariesBuildAndCompileAndBeyondIsRefused(string part)
    {
        var kind = new PartId(part);
        foreach (var height in new[] { 64d, -64d })
        {
            var placed = Place(kind, 1, 0, height, 0);
            var body = Assert.Single(WorkshopPhysicsCompiler.Compile(Free(placed), new(1, 2)).Bodies.ToArray(), b => b.Id == placed.Id);
            Assert.Equal(placed.Cell, body.Cell);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => Place(kind, 1, 0, 64.01, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Place(kind, 1, 0, -64.01, 0));
    }

    // Resting control: a payload placed resting on the bench (the lowest useful placement) stays put with no drift.
    [Theory]
    [InlineData("ball")]
    [InlineData("bowling")]
    public async Task APayloadRestingOnTheBenchStaysWithoutDrift(string part)
    {
        var kind = new PartId(part);
        var radius = (float)Assert.Single(WorkshopPhysicsCompiler.Compile(Free(Place(kind, 1, 0, 2, 0)), new(1, 2)).Colliders.ToArray(),
            c => c.Body == new GpuBodyId(1) && c.Shape == ColliderShapeKind.Sphere).Radius.Value;
        var reads = await WorkshopIntegrationHarness.Observe(Free(Place(kind, 1, 0, BenchY + radius, 0)), 3 * TicksPerSecond);
        var start = Find(reads[0].Bodies, new(1));
        foreach (var read in reads)
        {
            var body = Find(read.Bodies, new(1));
            Assert.InRange(Height(body), BenchY + radius - .001f, BenchY + radius + .001f);
            Assert.InRange(MathF.Abs(Horizontal(body) - Horizontal(start)), 0f, .001f);
        }
    }

    // Boundary: a payload driven into the bench at 40 m/s (inside the 64 m/s envelope) never tunnels through it.
    [Theory]
    [InlineData("ball")]
    [InlineData("bowling")]
    public void APayloadDrivenIntoTheBenchAtFortyMetresPerSecondNeverTunnels(string part)
    {
        var kind = new PartId(part);
        var scene = WorkshopPhysicsCompiler.Compile(Free(Place(kind, 1, 0, 1, 0)), new(1, 2));
        var radius = (float)Assert.Single(scene.Colliders.ToArray(), c => c.Body == new GpuBodyId(1) && c.Shape == ColliderShapeKind.Sphere).Radius.Value;
        var admitted = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var count = BinaryPrimitives.ReadUInt32LittleEndian(admitted.AsSpan(PhysicsGpuAbi.HeaderBodyCountOffset));
        var record = Enumerable.Range(0, (int)count).Select(slot => PhysicsWorldLayout.BodiesOffset + slot * PhysicsGpuAbi.BodyBytes)
            .Single(offset => BinaryPrimitives.ReadUInt64LittleEndian(admitted.AsSpan(offset)) == 1);
        BinaryPrimitives.WriteSingleLittleEndian(admitted.AsSpan(record + PhysicsGpuAbi.BodyVelocityOffset + 4), -40f);
        var lowest = float.MaxValue; var rebounded = false;
        WorkshopPhysicsEngineTests.StepWorld(admitted, TicksPerSecond, (_, world, interactions) =>
        {
            var body = PhysicsGpuAbi.ReadDynamicBodies(world)[0];
            lowest = MathF.Min(lowest, Height(body));
            rebounded |= body.Body.Velocity.Y > 0;
        });
        Assert.True(rebounded, "the payload struck the bench and rebounded");
        Assert.True(lowest > BenchY + radius - .1f, $"lowest centre {lowest} m stays above the bench (surface {BenchY} m, radius {radius} m)");
    }

    // A Receiver captures either kind settling into its mouth; the same payload shifted 2 m beside the mouth is never captured.
    [Theory]
    [InlineData("ball", 0, true)]
    [InlineData("bowling", 0, true)]
    [InlineData("ball", 2, false)]
    [InlineData("bowling", 2, false)]
    public async Task AReceiverCapturesEitherKindAndAShiftedMissNever(string part, double shift, bool captures)
    {
        var construction = Free(Place(new PartId(part), 1, shift, 1.1, 0), WorkshopInput.Generic(PartId.Receiver, new(2), 0, 1, 0, 0, 0, 0, 1));
        var reads = await WorkshopIntegrationHarness.Observe(construction, 2 * TicksPerSecond);
        static bool Latched(WorkshopRead read) =>
            Enumerable.Range(0, read.Captures.Count).Any(i => read.Captures[i].Phase == CaptureLatchPhase.Latched);
        Assert.False(Latched(reads[0]));
        Assert.Equal(captures, reads.Any(Latched));
    }

    // Exact Run/Reset and save/load for both kinds on one bench: Reset restores every built body state and the reloaded construction
    // preserves each payload's fall.
    [Fact]
    public async Task BothKindsFallAgainAfterExactResetAndReload()
    {
        var construction = Free(Place(PartId.Basketball, 1, -1.5, 3, 0), Place(PartId.BowlingBall, 2, 1.5, 3, 0));
        var saved = new WorkshopSavedConstruction(construction, new(3));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);
        Assert.Equal(saved, loaded);
        Assert.Equal([PartId.Basketball, PartId.BowlingBall], loaded.Construction.Instances.Select(i => i.PartId).ToArray());
        var (built, run, reset) = await WorkshopIntegrationHarness.ObserveReset(construction, 2 * TicksPerSecond);
        Assert.True(Height(Find(run[^1].Bodies, new(1))) < 2.5f && Height(Find(run[^1].Bodies, new(2))) < 2.5f, "both payloads fell");
        NoFixedCapacityTests.AssertSameState(built, reset, "Reset");
        var (_, replay, _) = await WorkshopIntegrationHarness.ObserveReset(loaded.Construction, 2 * TicksPerSecond);
        foreach (var id in new GpuBodyId[] { new(1), new(2) })
        {
            Assert.True(Height(Find(replay[^1].Bodies, id)) < 2.5f, "the reloaded payload falls");
            Assert.Contains(replay, read => Find(read.Bodies, id).Body.Velocity.Y < -.1f);
        }
    }

    // ---------- pre-conversion records (InstanceRecord at 59ecb70b: 27 slots) ----------

    /// <summary>A stored save holding one instance in the pre-conversion InstanceRecord layout: 0 id, 1 part_id, 2 locked, 3 cell,
    /// 4 local, 5 rotation, 6 radius, 7 mass, 8 bounce, 9 drag, 10 buoyancy, 11 length, 23 half_extents, 24 friction, 26 parameters
    /// (the other slots are absent in every record built here). Encoded exactly as the pre-conversion encoder wrote it.</summary>
    internal static byte[] PreConversionSave(string part, IReadOnlyDictionary<int, float> scalars, System.Numerics.Vector3? halfExtents = null,
        IReadOnlyList<(string Key, float Value)>? parameters = null)
    {
        var builder = new FlatBufferBuilder(512);
        var name = builder.CreateString(part);
        VectorOffset? parameterVector = null;
        if (parameters is not null)
            parameterVector = InstanceRecord.CreateParametersVector(builder, parameters.Select(p =>
                ParameterRecord.CreateParameterRecord(builder, builder.CreateString(p.Key), p.Value)).ToArray());
        builder.StartTable(27);
        if (parameterVector is { } vector) builder.AddOffset(26, vector.Value, 0);
        builder.AddUlong(0, 1, 0);
        builder.AddOffset(1, name.Value, 0);
        builder.AddBool(2, false, false);
        builder.AddStruct(3, Vec3i.CreateVec3i(builder, 0, 32, 0).Value, 0);
        builder.AddStruct(4, Vec3f.CreateVec3f(builder, 0, 0, 0).Value, 0);
        builder.AddStruct(5, Quat4f.CreateQuat4f(builder, 0, 0, 0, 1).Value, 0);
        foreach (var (slot, value) in scalars.OrderBy(pair => pair.Key)) builder.AddFloat(slot, value, 0);
        if (halfExtents is { } h) builder.AddStruct(23, Vec3f.CreateVec3f(builder, h.X, h.Y, h.Z).Value, 0);
        var instance = new Offset<InstanceRecord>(builder.EndTable());
        var instances = ConstructionRecord.CreateInstancesVector(builder, [instance]);
        var settings = WorkshopCadenceSettings.Default();
        ConstructionRecord.StartConstructionRecord(builder);
        ConstructionRecord.AddRevision(builder, 1);
        ConstructionRecord.AddSettings(builder, CadenceSettingsRecord.CreateCadenceSettingsRecord(builder, (uint)settings.Simulation,
            (uint)settings.Physical, (uint)settings.Animation, (uint)settings.Presentation, settings.PresentationRate.Numerator,
            settings.PresentationRate.Denominator));
        ConstructionRecord.AddInstances(builder, instances);
        return SaveFixture.Finish(builder, ConstructionRecord.EndConstructionRecord(builder), 2);
    }

    internal static byte[] PreConversionBall(string part, float radius, float mass, float bounce) =>
        PreConversionSave(part, new Dictionary<int, float> { [6] = radius, [7] = mass, [8] = bounce, [9] = .040008544921875f });

    /// <summary>The pre-conversion Domino record: the former record's half-precision material (0.4 kg, bounce 0.05, friction 0.6, half
    /// extents 0.125 × 0.55 × 0.325 m) widened to the f32 slots it was written in.</summary>
    internal static byte[] PreConversionDomino() =>
        PreConversionSave("domino", new Dictionary<int, float> { [7] = OldLayoutSaves.OldDominoMass, [8] = OldLayoutSaves.OldDominoBounce,
            [24] = OldLayoutSaves.OldDominoFriction }, OldLayoutSaves.OldDominoHalfExtents);

    internal static byte[] PreConversionPipe() => PreConversionSave("pipe", new Dictionary<int, float> { [11] = 3.6f });

    internal static byte[] PreConversionResizedRamp() => PreConversionSave("ramp", new Dictionary<int, float>(),
        parameters: [("length", 3.123457f), ("width", 1.234567f)]);

    // The pre-conversion layout is unsupported: deleting radius, drag and buoyancy renumbered every later slot, so each of these saves
    // (each admitted by the record path at 59ecb70b, receipt evidence-c-balls/baseline/baseline-payload-suite.txt) is refused with the
    // typed save refusal, never misread. Re-derived on the 9-field table of C-Clock (threshold removed; slot 7 is target_count and
    // slot 8 parameters). Old ball: its bounce (slot 8) now sits in the parameters slot, so the verifier refuses the buffer (at 9A.32 its
    // drag in slot 9 did; at 2065d3c5 radius and mass landed in typed fields: OutOfRange). Old Domino: likewise its bounce (slot 8) is
    // read as the parameters vector and the verifier refuses it (at 9A.32 its mass and bounce landed in threshold and target_count,
    // typed fields: OutOfRange; at 2065d3c5 MissingField). Old pipe: its length lies beyond the table, so it reads the schema's 0. Old
    // resized Ramp: its parameters are beyond the table, so its declared parameters are missing.
    [Fact]
    public void PreConversionRecordsAreRefusedWithTheTypedSaveRefusal()
    {
        foreach (var (name, bytes, reason) in new[]
        {
            ("ball", PreConversionBall("ball", .340087890625f, 1, .5498046875f), WorkshopSaveRejection.VerifierRejected),
            ("bowling", PreConversionBall("bowling", .280029296875f, 4, .1400146484375f), WorkshopSaveRejection.VerifierRejected),
            ("domino", PreConversionDomino(), WorkshopSaveRejection.VerifierRejected),
            ("pipe", PreConversionPipe(), WorkshopSaveRejection.OutOfRange),
            ("ramp", PreConversionResizedRamp(), WorkshopSaveRejection.OutOfRange)
        })
        {
            var refusal = Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
            Assert.True(reason == refusal.Reason, $"{name}: {refusal.Reason} ({refusal.Message})");
        }
    }

    // ---------- the widened generic-record guard (save pre-validation and command decode alike) ----------

    /// <summary>A stored save holding one current-layout generic record of <paramref name="part"/>, optionally carrying one typed record
    /// field set to a nonzero value and optional declared parameters.</summary>
    internal static byte[] GenericSave(string part, TypedField? field, IReadOnlyList<(string Key, float Value)>? parameters = null, float value = 4f)
    {
        var builder = new FlatBufferBuilder(512);
        var name = builder.CreateString(part);
        VectorOffset? parameterVector = null;
        if (parameters is not null)
            parameterVector = InstanceRecord.CreateParametersVector(builder, parameters.Select(p =>
                ParameterRecord.CreateParameterRecord(builder, builder.CreateString(p.Key), p.Value)).ToArray());
        InstanceRecord.StartInstanceRecord(builder);
        if (parameterVector is { } vector) InstanceRecord.AddParameters(builder, vector);
        InstanceRecord.AddId(builder, 1);
        InstanceRecord.AddPartId(builder, name);
        InstanceRecord.AddCell(builder, Vec3i.CreateVec3i(builder, 0, 32, 0));
        InstanceRecord.AddLocal(builder, Vec3f.CreateVec3f(builder, 0, 0, 0));
        InstanceRecord.AddRotation(builder, Quat4f.CreateQuat4f(builder, 0, 0, 0, 1));
        switch (field)
        {
            case null: break;
            case TypedField.Length: InstanceRecord.AddLength(builder, value); break;
            default: throw new ArgumentOutOfRangeException(nameof(field));
        }
        var instance = InstanceRecord.EndInstanceRecord(builder);
        var instances = ConstructionRecord.CreateInstancesVector(builder, [instance]);
        var settings = WorkshopCadenceSettings.Default();
        ConstructionRecord.StartConstructionRecord(builder);
        ConstructionRecord.AddRevision(builder, 1);
        ConstructionRecord.AddSettings(builder, CadenceSettingsRecord.CreateCadenceSettingsRecord(builder, (uint)settings.Simulation,
            (uint)settings.Physical, (uint)settings.Animation, (uint)settings.Presentation, settings.PresentationRate.Numerator,
            settings.PresentationRate.Denominator));
        ConstructionRecord.AddInstances(builder, instances);
        return SaveFixture.Finish(builder, ConstructionRecord.EndConstructionRecord(builder), 2);
    }

    // Every typed record field, on a generic payload and on another generic element (the clock with its declared interval and the
    // counter with its declared target included, and the latch), is refused atomically by the save pre-validation and by the command
    // decoder alike, a NaN included. (A signed zero cannot be stored: the builder elides any value equal to the field default.) Control:
    // the bare record loads.
    [Theory]
    [MemberData(nameof(EveryTypedFieldOnGenericRecords))]
    public void AGenericRecordCarryingAnyTypedFieldIsRefused(string part, TypedField field, float value)
    {
        (string, float)[]? parameters = part switch { "ramp" => [("length", 3f), ("width", 1.3f)], "clock" => [("interval_seconds", 1f)],
            "counter" => [("target_count", 3f)], _ => null };
        var bytes = GenericSave(part, field, parameters, value);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
        // Control: the same record without the typed field loads.
        Assert.Equal(new PartId(part), Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, GenericSave(part, null, parameters)).Construction.Instances).PartId);
    }

    public static TheoryData<string, TypedField, float> EveryTypedFieldOnGenericRecords()
    {
        var data = new TheoryData<string, TypedField, float>();
        foreach (var part in new[] { "ball", "bowling", "ramp", "domino", "clock", "counter", "latch" })
            foreach (var field in Enum.GetValues<TypedField>())
                data.Add(part, field, 4f);
        data.Add("bowling", TypedField.Length, float.NaN);
        data.Add("domino", TypedField.Length, float.NaN);
        data.Add("clock", TypedField.Length, float.NaN);
        data.Add("counter", TypedField.Length, float.NaN);
        data.Add("latch", TypedField.Length, float.NaN);
        return data;
    }

    [Theory]
    [InlineData("ball")]
    [InlineData("bowling")]
    public void ABareGenericPayloadRecordLoadsAndAnyParameterIsRefused(string part)
    {
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, GenericSave(part, null));
        var payload = Assert.IsType<WorkshopGenericInstance>(Assert.Single(loaded.Construction.Instances));
        Assert.Equal((new PartId(part), 0), (payload.PartId, payload.Parameters.Count));
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,
            GenericSave(part, null, [("radius", .5f)])));
    }
}

/// <summary>Every typed field of the current InstanceRecord (C-Clock deleted the threshold and C-Memory the target count): a generic
/// record carries none of them.</summary>
public enum TypedField
{
    Length
}
