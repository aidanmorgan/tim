using System.Buffers.Binary;
using System.Runtime.Intrinsics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopBumperTests
{

    [Theory]
    [InlineData(WorkshopPuzzleId.BumperDepth, "bumper_depth")]
    [InlineData(WorkshopPuzzleId.WallAndBumper, "wall_and_bumper")]
    public void AdvancedLessonsPreserveSourceFixturesProfilesInventoryAndSave(
        WorkshopPuzzleId lesson, string sourceId)
    {
        using var source = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(value => value.GetProperty("id").GetString() == sourceId);
        var construction = BumperAdvanced.Create(lesson, new(1), WorkshopCadenceSettings.Default(),
            new(1), new(2), new((Half)0));
        Assert.Equal(WorkshopGoalKind.Captured, construction.Puzzle.Goal.Kind);
        Assert.Equal(new GpuBodyId(1), construction.Puzzle.Goal.Body);
        Assert.Equal(new GpuBodyId(2), construction.Puzzle.Goal.Target);
        var fixtures = level.GetProperty("parts");
        CheckSourcePose(fixtures[0], construction.Ball!.Value.Cell, construction.Ball!.Value.Local, construction.Ball!.Value.Rotation);
        CheckSourcePose(fixtures[1], construction.Receiver!.Value.Cell, construction.Receiver!.Value.Local, construction.Receiver!.Value.Rotation);
        Assert.True(construction.Ball!.Value.Locked && construction.Receiver!.Value.Locked);
        CheckProfile(fixtures[0].GetProperty("difficulty"), construction.Puzzle.BallAssistance);
        CheckProfile(fixtures[1].GetProperty("difficulty"), construction.Puzzle.ReceiverAssistance);
        var solution = level.GetProperty("solution").EnumerateArray().ToArray();
        CheckProfile(solution.Single(value => value.GetProperty("kind").GetString() == "bumper").GetProperty("difficulty"),
            construction.Puzzle.RampAssistance);
        var inventory = WorkshopInventoryPolicy.Authored(construction.Puzzle);
        Assert.Equal(PartAllowance.Counted(1), inventory[WorkshopPartKind.PinballBumper]);
        var bumper = WorkshopInput.Bumper(new(3), 0, 1.5, 0, 0, 0, 0, 1, BumperWork.Default);
        var placed = construction.WithInstance(bumper);
        var wall = WorkshopInput.Wall(new(4), -1, 4, 0, 0, 0, 0, 1, WallDimensions.FromInput(.4f, 6, 1.5f));
        if (lesson == WorkshopPuzzleId.WallAndBumper)
        {
            Assert.Equal(2, inventory.Count);
            Assert.Equal(PartAllowance.Counted(1), inventory[WorkshopPartKind.Wall]);
            Assert.Equal(1, level.GetProperty("inventory").GetProperty("wall").GetInt32());
            CheckProfile(solution.Single(value => value.GetProperty("kind").GetString() == "wall").GetProperty("difficulty"),
                construction.Puzzle.WallAssistance);
            placed = placed.WithInstance(wall);
            Assert.Throws<ArgumentException>(() => placed.WithInstance(wall with { Id = new(5) }).Validate());
        }
        else
        {
            Assert.Single(inventory);
            Assert.Equal(default, construction.Puzzle.WallAssistance);
            Assert.Throws<ArgumentException>(() => placed.WithInstance(wall).Validate());
        }
        placed.Validate();
        Assert.Throws<ArgumentException>(() => placed.WithInstance(bumper with { Id = new(5) }).Validate());
        Assert.Throws<ArgumentException>(() => placed.WithInstance(bumper with { Locked = true }).Validate());
        Assert.Throws<ArgumentException>(() => placed.WithInstance(construction.Ball!.Value with { Locked = false }).Validate());
        Assert.Throws<ArgumentException>(() => (placed with { Puzzle = placed.Puzzle with { InventoryCount = 2 } }).Validate());
        Assert.Throws<ArgumentException>(() => (placed with { Puzzle = placed.Puzzle with { RampAssistance = FirstPrinciples.RampAssistance } }).Validate());
        foreach (var precision in new Half[] { (Half)0, (Half).45, (Half)1 })
        {
            var next = BumperAdvanced.WithPrecision(placed, new(precision));
            var expected = precision == (Half)0 ? (.3,3,.15,12) : precision == (Half)1 ? (.02,1.5,.35,0) : (.174,2.325,.24,6.6);
            Assert.Equal((Half)expected.Item1,next.Receiver!.Value.Capture.Margin.Value);
            Assert.Equal((Half)expected.Item2,next.Receiver!.Value.Capture.SpeedLimit.Value);
            Assert.Equal((Half)expected.Item3,next.Receiver!.Value.Capture.Dwell.Value);
            Assert.Equal((Half)expected.Item4,next.Receiver!.Value.ForceRegion.MaximumAcceleration.Value);
            var save = new WorkshopSavedConstruction(next, new(6));
            var bytes = WorkshopSaveCodec.Encode(save);
            Assert.Equal(save, WorkshopSaveCodec.Decode(bytes));
            Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(bytes)));
            Assert.Equal(bumper, next.Instances.Single(value => value.Id == bumper.Id));
            Assert.Single(WorkshopPhysicsCompiler.Compile(next, new(1, 2)).ContactWorks.ToArray());
        }
    }


    [Theory]
    [InlineData(WorkshopPuzzleId.BumperDepth)]
    [InlineData(WorkshopPuzzleId.WallAndBumper)]
    public void AdvancedLessonSaveAndCommandRejectMalformedIdentityProfileAndInventory(WorkshopPuzzleId lesson)
    {
        var construction = BumperAdvanced.Create(lesson,new(1),WorkshopCadenceSettings.Default(),new(1),new(2),new((Half).45));
        var saved = new WorkshopSavedConstruction(construction,new(3));
        var original = WorkshopSaveCodec.Encode(saved);
        var command = new WorkshopCommand(new(1),WorkshopCommandKind.Construct,new(1),new(1),construction,
            Session:new(1,2),Cadence:new(1),Projection:new(1));
        var wire = WorkshopWire.Encode(command);
        // Relative to the construction's immutable puzzle record: unknown id, extra inventory,
        // substituted Bumper assistance and invalid precision. Every decode must reject before admission.
        foreach (var mutation in new (int Offset, uint Value, bool Wide)[]
        {
            (48,uint.MaxValue,true), (48+12,2,true),
            (48+180+6,BitConverter.HalfToUInt16Bits((Half).3),false),
            (48+8,BitConverter.HalfToUInt16Bits(Half.NaN),false)
        })
        {
            var badSave=(byte[])original.Clone(); var badWire=(byte[])wire.Clone();
            if(mutation.Wide)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(badSave.AsSpan(24+mutation.Offset),mutation.Value);
                BinaryPrimitives.WriteUInt32LittleEndian(badWire.AsSpan(WorkshopWire.CommandHeaderBytes+mutation.Offset),mutation.Value);
            }
            else
            {
                BinaryPrimitives.WriteUInt16LittleEndian(badSave.AsSpan(24+mutation.Offset),(ushort)mutation.Value);
                BinaryPrimitives.WriteUInt16LittleEndian(badWire.AsSpan(WorkshopWire.CommandHeaderBytes+mutation.Offset),(ushort)mutation.Value);
            }
            var rejectedSave=(byte[])badSave.Clone(); var rejectedWire=(byte[])badWire.Clone();
            Assert.Throws<ArgumentException>(()=>WorkshopSaveCodec.Decode(badSave));
            Assert.Throws<ArgumentException>(()=>WorkshopWire.DecodeCommand(badWire));
            Assert.Equal(rejectedSave,badSave); Assert.Equal(rejectedWire,badWire);
            Assert.Equal(original,WorkshopSaveCodec.Encode(saved));
            Assert.Equal(saved,WorkshopSaveCodec.Decode(original));
            Assert.Equal(command,WorkshopWire.DecodeCommand(wire));
        }
    }

    private static void CheckSourcePose(System.Text.Json.JsonElement source, CellOrigin cell,
        LocalPosition local, CanonicalRotation rotation)
    {
        var position = source.GetProperty("position");
        Assert.InRange(Math.Abs((cell.X + (double)local.X) / 16 - position[0].GetDouble()), 0, .001);
        Assert.InRange(Math.Abs((cell.Y + (double)local.Y) / 16 - position[1].GetDouble()), 0, .001);
        Assert.InRange(Math.Abs((cell.Z + (double)local.Z) / 16 - position[2].GetDouble()), 0, .001);
        var q = new Godot.Quaternion((float)rotation.X, (float)rotation.Y, (float)rotation.Z, (float)rotation.W).Normalized();
        var basis = new Godot.Basis(q);
        var orientation = source.GetProperty("orientation");
        for (var column = 0; column < 3; column++)
            for (var row = 0; row < 3; row++)
                Assert.InRange(Math.Abs(basis[column][row] - orientation[column * 3 + row].GetDouble()), 0, .002);
    }

    private static void CheckProfile(System.Text.Json.JsonElement data, AssistanceProfile profile)
    {
        var values = new[] { profile.Forgiving, profile.Balanced, profile.Precise };
        var names = new[] { "precision", "position_window", "rotation_window", "max_position_correction", "max_rotation_correction",
            "blend_seconds", "capture_margin", "capture_speed", "capture_dwell", "guide_acceleration", "trigger_threshold" };
        for (var i = 0; i < 3; i++)
        {
            var k = values[i];
            Half[] actual = [k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees, k.MaximumPositionCorrection.Value,
                k.MaximumRotationCorrectionDegrees, k.Blend.Value, k.CaptureMargin.Value, k.CaptureSpeed.Value,
                k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold];
            for (var field = 0; field < names.Length; field++)
                Assert.Equal((Half)data[i].GetProperty(names[field]).GetDouble(), actual[field]);
        }
    }

    [Fact]
    public void SidekickKeepsSourceProfilesAndAtomicInventory()
    {
        using var source = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement.EnumerateArray().Single(value => value.GetProperty("id").GetString() == "bumper_sidekick");
        Assert.Equal(1, level.GetProperty("inventory").GetProperty("bumper").GetInt32());
        var construction = BumperSidekick.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new((Half)0));
        Assert.Equal(WorkshopGoalKind.Captured, construction.Puzzle.Goal.Kind);
        Check(level.GetProperty("parts")[0].GetProperty("difficulty"), construction.Puzzle.BallAssistance);
        Check(level.GetProperty("parts")[1].GetProperty("difficulty"), construction.Puzzle.ReceiverAssistance);
        Check(level.GetProperty("solution")[0].GetProperty("difficulty"), construction.Puzzle.RampAssistance);
        Assert.Equal(WorkshopInput.Basketball(new(1), -3, 5, 0, 0, 0, 0, 1) with { Locked = true }, construction.Ball);
        var sourceReceiver = level.GetProperty("parts")[1].GetProperty("position");
        Assert.Equal(2, sourceReceiver[0].GetDouble()); Assert.Equal(.9, sourceReceiver[1].GetDouble());
        var bumper = new WorkshopBumper(new(3), new(-51, 24, 0), default, CanonicalRotation.Identity, BumperWork.Default);
        var placed = construction.WithInstance(bumper); placed.Validate();
        Assert.Throws<ArgumentException>(() => placed.WithInstance(bumper with { Id = new(4) }).Validate());
        Assert.Throws<ArgumentException>(() => (placed with { Puzzle = placed.Puzzle with { RampAssistance = FirstPrinciples.RampAssistance } }).Validate());
        var unpaid = placed.WithInstance(bumper with { Work = BumperWork.FromCanonicalStrength(0) }); unpaid.Validate();
        Assert.Equal(0f, ((WorkshopBumper)unpaid.Instances.Single(value => value.Id == bumper.Id)).Work.Preload.Value);
        foreach (var precision in new Half[] { (Half)0, (Half).45, (Half)1 })
        {
            var next = BumperSidekick.WithPrecision(placed, new(precision)); next.Validate();
            var saved = new WorkshopSavedConstruction(next, new(4));
            Assert.Equal(saved, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(saved)));
        }
        static void Check(System.Text.Json.JsonElement data, AssistanceProfile profile)
        {
            var values = new[] { profile.Forgiving, profile.Balanced, profile.Precise };
            var names = new[] { "precision", "position_window", "rotation_window", "max_position_correction", "max_rotation_correction",
                "blend_seconds", "capture_margin", "capture_speed", "capture_dwell", "guide_acceleration", "trigger_threshold" };
            for (var i = 0; i < 3; i++)
            {
                var k = values[i];
                Half[] actual = [k.Precision, k.PositionWindow.Value, k.RotationWindowDegrees, k.MaximumPositionCorrection.Value,
                    k.MaximumRotationCorrectionDegrees, k.Blend.Value, k.CaptureMargin.Value, k.CaptureSpeed.Value,
                    k.CaptureDwell.Value, k.GuideAcceleration.Value, k.TriggerThreshold];
                for (var field = 0; field < names.Length; field++)
                    Assert.Equal((Half)data[i].GetProperty(names[field]).GetDouble(), actual[field]);
            }
        }
    }

    [Theory]
    [InlineData(32f)]
    [InlineData(2.5f)]
    [InlineData(.000001f)]
    [InlineData(0f)]
    public void StorePaymentUsesOneF32BudgetAndSimdNormalProjection(float budget)
    {
        var normal = Vector128.Create(.6f, .8f, 0f, 0f);
        var result = ContactWorkImpulse.FromStore(4f, 8f, 1f, budget, normal);
        Assert.InRange(result.Remaining, 0f, budget);
        Assert.Equal(budget - result.Remaining, result.Debit);
        var x = result.Impulse.GetElement(0);
        var y = result.Impulse.GetElement(1);
        Assert.Equal(0f, result.Impulse.GetElement(2));
        Assert.Equal(0f, result.Impulse.GetElement(3));
        Assert.InRange(Math.Abs(.8f * x - .6f * y), 0, 1e-6f);
        var impulse = .6 * x + .8 * y;
        var work = 4 * impulse + .5 * impulse * impulse;
        Assert.InRange(work, 0, result.Debit + 1e-5);
        Assert.InRange(4 + impulse, 4, 8.000001);
        if (budget == 0) Assert.Equal(Vector128<float>.Zero, result.Impulse);
        else Assert.True(result.Debit > 0 && impulse > 0);
    }

    [Fact]
    public void PaidGpuRecordReadsF32WorkAndRejectsPadding()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction((Half)8), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var before = PhysicsGpuAbi.ReadContactWorks(bytes);
        var owner = PhysicsGpuAbi.ContactWorksOffset;
        var occurrence = PhysicsGpuAbi.WorkOccurrencesOffset;
        var collider = Array.FindIndex(scene.Colliders.ToArray(), value => value.Body == new GpuBodyId(2));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(88), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(owner + 32), 1);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(owner + 48), 20f);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(occurrence + 2), checked((ushort)collider));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(occurrence + 12), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(occurrence + 16), 1);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(occurrence + 22), 4.000001f);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(occurrence + 26), 12f);
        bytes[occurrence + 30] = (byte)ContactWorkEffect.Paid;
        var paid = PhysicsGpuAbi.ReadContactWorks(bytes);
        paid.ValidateAdvance(before);
        Assert.Equal(20f, paid[0].RemainingEnergy.Value);
        Assert.Equal(12f, paid.Occurrence(0).Debit.Value);
        Assert.Equal(4.000001f, paid.Occurrence(0).ApproachSpeed.Value);
        Assert.Equal(ContactWorkEffect.Paid, paid.Occurrence(0).Effect);
        bytes[occurrence + 31] = 1;
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadContactWorks(bytes));
    }

    [Theory]
    [InlineData(32f, 8f, 24f)]
    [InlineData(2.5f, 4.582576f, 2.5f)]
    [InlineData(0f, 4f, 0f)]
    public void FiniteBudgetCapsTheNormalBoostAndNeverPaysPastTarget(
        float budget, float expectedSpeed, float expectedDebit)
    {
        var payment = ContactWorkImpulse.TowardTarget(4f, 8f, 1f, budget);
        Assert.InRange(4f + payment.NormalImpulse, expectedSpeed - 1e-5f, expectedSpeed + 1e-5f);
        Assert.InRange(payment.Debit, expectedDebit - 1e-5f, expectedDebit);
        Assert.InRange(payment.Debit, 0f, budget);
    }

    [Theory]
    [InlineData(8f)]
    [InlineData(12f)]
    public void ContactAlreadyAtTargetIsNeverSlowedOrCharged(float speed) =>
        Assert.Equal(default, ContactWorkImpulse.TowardTarget(speed, 8f, 1f, 32f));

    [Theory]
    [InlineData(0f, 1f, 32f, 8f)]
    [InlineData(4f, 1f, 24f, 4f)]
    [InlineData(4f, .25f, 96f, 16f)]
    public void AuthorizedNormalWorkPaysTheContactSpaceKineticIncrease(
        float outgoingSpeed, float inverseEffectiveMass, float work, float expectedImpulse)
    {
        var payment = ContactWorkImpulse.FromAuthorizedWork(outgoingSpeed, inverseEffectiveMass, work);
        Assert.InRange(payment.NormalImpulse, expectedImpulse - 1e-5f, expectedImpulse + 1e-5f);
        Assert.InRange(payment.Debit, work - 1e-4f, work);
        var after = outgoingSpeed + inverseEffectiveMass * payment.NormalImpulse;
        var increase = (after * after - outgoingSpeed * outgoingSpeed) / (2f * inverseEffectiveMass);
        Assert.InRange(increase, payment.Debit - 1e-4f, payment.Debit + 1e-4f);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void NoAuthorizedWorkNeverProducesAnImpulse(float work) =>
        Assert.Equal(default, ContactWorkImpulse.FromAuthorizedWork(4f, 1f, work));

    [Fact]
    public void TinyWorkAtHighOutgoingSpeedSurvivesWithoutSubtractingNearlyEqualRoots()
    {
        var payment = ContactWorkImpulse.FromAuthorizedWork(64f, 1f, 1e-6f);
        Assert.True(payment.NormalImpulse > 0f);
        Assert.InRange(payment.Debit, 0.999e-6f, 1e-6f);
    }

    [Theory]
    [InlineData(-1f, 1f)]
    [InlineData(float.NaN, 1f)]
    [InlineData(1f, 0f)]
    [InlineData(1f, -1f)]
    [InlineData(1f, float.PositiveInfinity)]
    public void InvalidContactResidualContinuesWithoutPaidImpulse(float speed, float inverseMass) =>
        Assert.Equal(default, ContactWorkImpulse.FromAuthorizedWork(speed, inverseMass, 32f));

    private static WorkshopConstruction Construction(Half strength) => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), 0, 6, 0, 0, 0, 0, 1),
            WorkshopInput.Bumper(new(2), 0, 4, 0, 0, 0, 0, 1, BumperWork.FromCanonicalStrength((float)strength))));

    [Fact]
    public void SourceSphereAndExplicitPreloadCompileAsGenericDeclarations()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction((Half)8), new(1, 2));
        var sphere = Assert.Single(scene.Colliders.ToArray(), x => x.Body == new GpuBodyId(2));
        Assert.Equal(ColliderShapeKind.Sphere, sphere.Shape);
        Assert.Equal(new Metres((Half).65), sphere.Radius);
        Assert.Equal(RigidLocalPose.Identity, sphere.Pose);
        var material = Assert.Single(scene.Materials.ToArray(), x => x.Id == sphere.Material);
        Assert.Equal(new Restitution((Half)1), material.Restitution);
        Assert.Equal(new LinearSpeed((Half).1), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient((Half).3), material.Friction);
        var work = Assert.Single(scene.ContactWorks.ToArray());
        Assert.Equal(new GpuBodyId(2), work.Owner);
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllDynamic, default), work.Targets);
        Assert.True(work.Targets.Contains(new(1)));
        Assert.Equal(new ContactSpeed((float)8), work.TargetSpeed);
        Assert.Equal(new Joules(32), work.InitialEnergy);
        Assert.Equal(new ContactSpeed((float).05), work.Threshold);
        Assert.Equal(72u, work.CooldownPhysicalSteps);
    }

    [Fact]
    public void ConstructionRoundTripPreservesCalibrationAndRejectsPriorSchemasOrForgedEnergy()
    {
        foreach (var strength in new Half[] { (Half)0, (Half)8, (Half)20 })
        {
            var save = new WorkshopSavedConstruction(Construction(strength), new(3));
            var bytes = WorkshopSaveCodec.Encode(save);
            Assert.Equal(save, WorkshopSaveCodec.Decode(bytes));
            var bumper = Assert.IsType<WorkshopBumper>(save.Construction.Instances[1]);
            Assert.Equal(new WorkMass((float)1), bumper.Work.ReferenceMass);
            Assert.Equal(.5f * (float)strength * (float)strength, bumper.Work.Preload.Value);
            var forged = (byte[])bytes.Clone();
            var bumperOffset = 24 + WorkshopWire.ConstructionHeaderBytes + WorkshopWire.InstanceBytes;
            BinaryPrimitives.WriteUInt16LittleEndian(forged.AsSpan(bumperOffset + 112),
                BitConverter.HalfToUInt16Bits((Half)17));
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(forged));
            var prior = (byte[])bytes.Clone();
            BinaryPrimitives.WriteUInt32LittleEndian(prior.AsSpan(4), 6);
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(prior));
            var padding = (byte[])bytes.Clone(); padding[bumperOffset + 116] = 1;
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(padding));
        }
    }

    [Fact]
    public void WorkRequiresUniquePhysicalStaticOwnerAndDynamicTarget()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction((Half)8), new(1, 2));
        var work = scene.ContactWorks[0];
        foreach (var invalid in new[] {
            work with { Owner = new(1) }, work with { Targets = new(BodyTargetKind.NamedBody, work.Owner) },
            work with { Owner = new(99) }, work with { CooldownPhysicalSteps = 0 },
            work with { InitialEnergy = new(201) } })
            Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(scene.Document, scene.NextIdentity,
                scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, scene.Triggers, [invalid]));
        Assert.Throws<ArgumentException>(() => new PhysicsSceneDeclaration(scene.Document, scene.NextIdentity + 1,
            scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, scene.Triggers,
            [work, work with { Id = new(scene.NextIdentity) }]));
    }

    [Fact]
    public void StrengthBoundariesAndPopulationRejectAtomically()
    {
        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, -.01f, MathF.BitIncrement(20f) })
            Assert.Throws<ArgumentException>(() => BumperWork.FromCanonicalStrength(invalid));
        foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, -.001, 20.001 })
            Assert.Throws<ArgumentException>(() => BumperWork.FromInput(invalid));
        var construction = Construction((Half)8);
        var bumper = Assert.IsType<WorkshopBumper>(construction.Instances[1]);
        construction.WithInstance(bumper with { Id = new(3) }).Validate(); // No per-kind bumper count in free play.
        Assert.Throws<ArgumentException>(() => (bumper.Work with { ReferenceMass = new((float)2) }).Validate());
        Assert.False(WorkshopPorts.Has(WorkshopPartKind.PinballBumper, WorkshopSocket.ActivationOut,
            WorkshopConnectionDomain.Activation, WorkshopPortDirection.Output));
    }
}
