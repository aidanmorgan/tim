using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopSaveTests
{
    // Every malformed stored save, in a deterministic corpus (each truncation, each single-bit flip and seeded random
    // buffers of a valid save with parts and connections), decodes or is the typed save refusal; nothing else escapes.
    [Fact]
    public void EveryMalformedSaveIsTheTypedRefusal()
    {
        var valid = WorkshopSaveCodec.Encode(ActivationSaved());
        var random = new Random(9);
        var corpus = new List<byte[]>();
        for (var length = 0; length < valid.Length; length++) corpus.Add(valid[..length]);
        for (var bit = 0; bit < valid.Length * 8; bit++)
        {
            var flipped = (byte[])valid.Clone();
            flipped[bit / 8] ^= (byte)(1 << (bit % 8));
            corpus.Add(flipped);
        }
        for (var count = 0; count < 1000; count++)
        {
            var noise = new byte[random.Next(0, valid.Length * 2)];
            random.NextBytes(noise);
            corpus.Add(noise);
        }
        var escaped = new List<string>();
        foreach (var bytes in corpus)
        {
            var error = Record.Exception(() => WorkshopSaveCodec.Decode(bytes));
            if (error is not (null or WorkshopEditRejectedException)) escaped.Add($"{bytes.Length} bytes: {error.GetType().FullName}: {error.Message}");
        }
        Assert.Empty(escaped);
    }

    // Only the failures malformed bytes produce become the refusal, each only where it can mean that; a defect stays a
    // defect. The reader's bounds failures count only while reading records, never during construction validation.
    // Exact ArgumentException and ArgumentOutOfRangeException are the structural validators' declared input rejection
    // (shared argument guards), the untrusted-input boundary the coordinator ruled on (spec §10).
    [Theory]
    [InlineData(typeof(ArgumentException), true, true)]
    [InlineData(typeof(ArgumentOutOfRangeException), true, true)]
    [InlineData(typeof(IndexOutOfRangeException), false, true)]
    [InlineData(typeof(OverflowException), false, true)]
    [InlineData(typeof(ArgumentNullException), false, false)]
    [InlineData(typeof(System.ComponentModel.InvalidEnumArgumentException), false, false)]
    [InlineData(typeof(InvalidOperationException), false, false)]
    [InlineData(typeof(NullReferenceException), false, false)]
    [InlineData(typeof(KeyNotFoundException), false, false)]
    public void OnlyMalformedInputFailuresBecomeTheSaveRefusal(Type failure, bool invalidStoredValue, bool malformedRecord)
    {
        var error = (Exception)Activator.CreateInstance(failure)!;
        Assert.Equal((invalidStoredValue, malformedRecord), (WorkshopSaveCodec.IsInvalidStoredValue(error), WorkshopSaveCodec.IsMalformedRecord(error)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.45)]
    [InlineData(1)]
    public void ReceiverForceRegionSurvivesSaveAndCompilesWithoutPuzzleSelection(double precision)
    {
        var receiver = FirstPrinciples.Receiver(new(3), new((Half)precision)) with { Locked = false };
        var construction = Saved().Construction.WithInstance(receiver);
        var saved = new WorkshopSavedConstruction(construction, new(7));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var decoded = WorkshopSaveCodec.Decode(bytes);
        Assert.Equal(saved, decoded);
        var scene = WorkshopPhysicsCompiler.Compile(decoded.Construction, new(1, 2));
        {
            var guide = Assert.Single(scene.Guides.ToArray());
            Assert.Equal(receiver.ForceRegion.Minimum, guide.Minimum);
            Assert.Equal(receiver.ForceRegion.Maximum, guide.Maximum);
            Assert.Equal(receiver.ForceRegion.MaximumAcceleration, guide.MaximumAcceleration);
            Assert.Equal(receiver.ForceRegion.SupportHeight, guide.SupportHeight);
            Assert.Equal(receiver.ForceRegion.SupportMargin, guide.SupportMargin);
        }
        Assert.Equal((Half)0, Assert.Single(WorkshopPhysicsCompiler.Compile(Saved().Construction, new(1, 2)).Guides.ToArray()).MaximumAcceleration.Value);
    }

    [Theory]
    [InlineData(0, 0x7e00)]
    [InlineData(1, 0x7c00)]
    [InlineData(2, 0xfc00)]
    [InlineData(3, 0xc000)]
    [InlineData(4, 0x0000)]
    [InlineData(5, 0xc000)]
    [InlineData(6, 0x4c40)]
    [InlineData(7, 0xbc00)]
    [InlineData(8, 0x4000)]
    [InlineData(9, 0xbc00)]
    [InlineData(10, 0x4a80)]
    public void InvalidForceRegionRejectsWithoutChangingExistingConstruction(int field, int bits)
    {
        var saved = Saved();
        var original = WorkshopSaveCodec.Encode(saved);
        var baseReceiver = (WorkshopReceiver)saved.Construction.Instances[1];
        var invalidVal = BitConverter.UInt16BitsToHalf((ushort)bits);
        var invalidRegion = field switch
        {
            0 => baseReceiver.ForceRegion with { Minimum = new(invalidVal, baseReceiver.ForceRegion.Minimum.Y, baseReceiver.ForceRegion.Minimum.Z) },
            1 => baseReceiver.ForceRegion with { Minimum = new(baseReceiver.ForceRegion.Minimum.X, invalidVal, baseReceiver.ForceRegion.Minimum.Z) },
            2 => baseReceiver.ForceRegion with { Minimum = new(baseReceiver.ForceRegion.Minimum.X, baseReceiver.ForceRegion.Minimum.Y, invalidVal) },
            3 => baseReceiver.ForceRegion with { Maximum = new(invalidVal, baseReceiver.ForceRegion.Maximum.Y, baseReceiver.ForceRegion.Maximum.Z) },
            4 => baseReceiver.ForceRegion with { Maximum = new(baseReceiver.ForceRegion.Maximum.X, invalidVal, baseReceiver.ForceRegion.Maximum.Z) },
            5 => baseReceiver.ForceRegion with { Maximum = new(baseReceiver.ForceRegion.Maximum.X, baseReceiver.ForceRegion.Maximum.Y, invalidVal) },
            6 => baseReceiver.ForceRegion with { SupportHeight = new(invalidVal) },
            7 or 8 => baseReceiver.ForceRegion with { SupportMargin = new(invalidVal) },
            9 or 10 => baseReceiver.ForceRegion with { MaximumAcceleration = new(invalidVal) },
            _ => baseReceiver.ForceRegion
        };
        var malformedReceiver = baseReceiver with { ForceRegion = invalidRegion };
        var malformedConstruction = saved.Construction with {
            Instances = new WorkshopInstances(saved.Construction.Instances[0], malformedReceiver)
        };
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, malformedConstruction, validate: false);
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, WorkshopSaveCodec.Magic);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, saved.NextBodyId.Value);
        CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, crOffset);
        var end = CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder);
        builder.Finish(end.Value);
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(builder.SizedByteArray()));
        Assert.Equal(original, WorkshopSaveCodec.Encode(saved));
    }

    private static WorkshopSavedConstruction Saved()
    {
        var ball = WorkshopInput.Basketball(new(2), .125, 3, -.25, 0, 0, 0, 1);
        var receiver = WorkshopInput.Receiver(new(3), .5, 1, -.5, 0, 0, 0, 1);
        return new(new(new(9), WorkshopCadenceSettings.Default(), new(ball, receiver)), new(7));
    }

    [Fact]
    public void CanonicalBitsAndAllocatorRoundTripWithoutLiveState()
    {
        var saved = Saved();
        var bytes = WorkshopSaveCodec.Encode(saved);
        Assert.True(bytes.Length >= 16);
        Assert.Equal(saved, WorkshopSaveCodec.Decode(bytes));
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(bytes)));
        var empty = new WorkshopSavedConstruction(new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty), new(1));
        Assert.Equal(empty, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(empty)));
    }

    [Theory]
    [InlineData(0x12345678u, 7UL, true)]
    [InlineData(WorkshopSaveCodec.Magic, 0UL, true)]
    [InlineData(WorkshopSaveCodec.Magic, 2UL, true)]
    [InlineData(WorkshopSaveCodec.Magic, 7UL, false)]
    public void UnknownHeaderCannotBeLoaded(uint magic, ulong nextBodyId, bool includeConstruction)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        Google.FlatBuffers.Offset<CuriousContraptions.Wire.ConstructionRecord>? crOffset = includeConstruction
            ? WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, Saved().Construction)
            : null;
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, magic);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, nextBodyId);
        if (crOffset.HasValue) CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, crOffset.Value);
        var end = CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder);
        builder.Finish(end.Value);
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(builder.SizedByteArray()));
    }

    [Fact]
    public void TruncatedTrailingAndInvalidAllocatorReject()
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(bytes.AsSpan(1)));
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(bytes.AsSpan(..10)));
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var crOffset = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, Saved().Construction);
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, WorkshopSaveCodec.Magic);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, 3);
        CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, crOffset);
        var end = CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder);
        builder.Finish(end.Value);
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(builder.SizedByteArray()));
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Encode(Saved() with { NextBodyId = new(3) }));
    }

    [Theory]
    [InlineData("unsupported_unknown_part")]
    [InlineData("unknown_device_kind")]
    public void UnsupportedPartPopulationRejectsBeforeAdmission(string unknownPartId)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var partIdOffset = builder.CreateString(unknownPartId);
        CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
        CuriousContraptions.Wire.InstanceRecord.AddId(builder, 2);
        CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, partIdOffset);
        CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, 0, 0, 0));
        CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, 0, 0, 0));
        CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, 0, 0, 0, 1));
        var instOffset = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);
        var instancesOffset = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, [instOffset]);
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, 1);
        var defCadence = WorkshopCadenceSettings.Default();
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder, CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(builder, (uint)defCadence.Simulation, (uint)defCadence.Physical, (uint)defCadence.Animation, (uint)defCadence.Presentation, defCadence.PresentationRate.Numerator, defCadence.PresentationRate.Denominator));
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instancesOffset);
        var crOffset = CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder);
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, WorkshopSaveCodec.Magic);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, 10);
        CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, crOffset);
        var end = CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder);
        builder.Finish(end.Value);
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(builder.SizedByteArray()));
    }

    [Fact]
    public void SaveCommandRequiresExactRevisionAndRoundTrips()
    {
        var command = new WorkshopCommand(new(5), WorkshopCommandKind.Save, new(2), new(8), null,
            Session: new(1, 2), Cadence: new(1), Projection: new(2));
        var current = WorkshopWire.Encode(command);
        Assert.Equal(command, WorkshopWire.DecodeCommand(current));
        BinaryPrimitives.WriteUInt32LittleEndian(current.AsSpan(12), 7);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeCommand(current));
        Assert.Throws<ArgumentException>(() => WorkshopWire.Encode(command with
            { RevisionKind = ExpectedRevisionKind.Any, Revision = default }));
    }

    [Fact]
    public void TwoRampInstancesKeepDistinctCanonicalDimensionsThroughSaveAndCompile()
    {
        var saved = Saved();
        var a = WorkshopInput.Ramp(new(4), -3.4, 4.3, 0, 0, 0, 0, 1, RampDimensions.Default);
        var b = WorkshopInput.Ramp(new(5), -.7, 2.7, 0, 0, 0, 0, 1, new(new((Half)2.5), new((Half).75))) with { Locked = true };
        saved = saved with { Construction = saved.Construction.WithInstance(a).WithInstance(b) };
        var bytes = WorkshopSaveCodec.Encode(saved);
        var decoded = WorkshopSaveCodec.Decode(bytes);
        Assert.Equal(saved, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
        var scene = WorkshopPhysicsCompiler.Compile(decoded.Construction, new(1, 2));
        foreach (var ramp in new[] { a, b })
        {
            var body = Assert.Single(scene.Bodies.ToArray(), body => body.Id == ramp.Id);
            Assert.Equal(RigidMotionKind.Static, body.Motion);
            var box = Assert.Single(scene.Colliders.ToArray(), collider => collider.Body == ramp.Id);
            Assert.Equal(ColliderShapeKind.Box, box.Shape);
            Assert.Equal((Half)(ramp.Dimensions.Length.Value * (Half).5), box.HalfExtents.X);
            Assert.Equal((Half)(ramp.Dimensions.Width.Value * (Half).5), box.HalfExtents.Z);
            Assert.Equal((Half)(RampDimensions.Thickness.Value * (Half).5), box.HalfExtents.Y);
            Assert.Equal((Half)1, Assert.Single(scene.Materials.ToArray(), material => material.Id == box.Material).Restitution.Value);
        }
    }

    [Fact]
    public void InstanceCollectionOwnsItsArrayAndReplacementDoesNotMutatePriorConstruction()
    {
        var first = Saved().Construction;
        var input = first.Instances.ToArray();
        var copy = new WorkshopInstances(input);
        input[0] = input[1];
        Assert.Equal(first.Instances, copy);
        var receiver = first.Receiver!.Value with { Id = first.Ball!.Value.Id };
        var changed = first.WithoutInstance(first.Receiver.Value.Id).WithInstance(receiver);
        changed.Validate();
        Assert.NotNull(first.Ball);
        Assert.Null(changed.Ball);
        Assert.Equal(receiver.Id, changed.Receiver!.Value.Id);
        var saved = new WorkshopSavedConstruction(changed, new(7));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(saved)));
    }

    [Fact]
    public void DuplicatePopulationAndInvalidDimensionsRejectBeforeGpuAdmission()
    {
        var saved = Saved();
        var duplicate = saved.Construction with { Instances = new(saved.Construction.Ball!.Value, saved.Construction.Ball.Value) };
        Assert.Throws<ArgumentException>(duplicate.Validate);
        var ramp = WorkshopInput.Ramp(new(4), 0, 3, 0, 0, 0, 0, 1, RampDimensions.Default);
        foreach (var value in new[] { (Half)0, (Half)(-1), (Half)4.5, Half.NaN, Half.PositiveInfinity })
        {
            var invalid = saved with { Construction = saved.Construction.WithInstance(ramp with { Dimensions = new(new(value), new((Half)1)) }) };
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Encode(invalid));
        }
        var excess = saved.Construction.WithInstance(ramp).WithInstance(ramp with { Id = new(5) }).WithInstance(ramp with { Id = new(6) });
        excess.Validate(); // Free play has no per-kind ramp count; WorkbenchCapacityTests cover the compiled table ceilings.
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.45)]
    [InlineData(1)]
    public void FirstPrinciplesSettingsAndTwoRampsRoundTripAtEveryAuthoredKnot(double value)
    {
        var precision = new PuzzlePrecision((Half)value);
        var construction = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(11), new(12), precision);
        var a = WorkshopInput.Ramp(new(13), -3.4, 4.3, 0, 0, 0, 0, 1, RampDimensions.Default);
        var b = WorkshopInput.Ramp(new(14), -.7, 2.7, 0, 0, 0, 0, 1, new(new((Half)2.5), new((Half)1)));
        construction = construction.WithInstance(a).WithInstance(b);
        var saved = new WorkshopSavedConstruction(construction, new(15));
        var bytes = WorkshopSaveCodec.Encode(saved);
        Assert.Equal(saved, WorkshopSaveCodec.Decode(bytes));
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(bytes)));
        Assert.True(construction.Ball!.Value.Locked); Assert.True(construction.Receiver!.Value.Locked);
        Assert.Equal(new GpuBodyId(11), construction.Puzzle.Goal.Body);
        Assert.Equal(new GpuBodyId(12), construction.Puzzle.Goal.Target);
        Assert.Equal(2u, construction.Puzzle.InventoryCount);
        var updated = FirstPrinciples.WithPrecision(construction, new((Half).5));
        Assert.Equal(a, updated.Instances[2]); Assert.Equal(b, updated.Instances[3]);
        Assert.Equal(construction.Puzzle.Goal, updated.Puzzle.Goal);
        foreach (var knot in new[] { (Half)0, (Half).45, (Half)1 })
        {
            updated = FirstPrinciples.WithPrecision(updated, new(knot));
            var expected = FirstPrinciples.Receiver(construction.Puzzle.Goal.Target, new(knot));
            Assert.Equal(expected, updated.Receiver);
            var guide = Assert.Single(WorkshopPhysicsCompiler.Compile(updated, new(1, 2)).Guides.ToArray());
            Assert.Equal(expected.ForceRegion.MaximumAcceleration, guide.MaximumAcceleration);
            Assert.Equal(expected.ForceRegion.SupportMargin, guide.SupportMargin);
            Assert.Equal(a, updated.Instances[2]); Assert.Equal(b, updated.Instances[3]);
            var changedSave = new WorkshopSavedConstruction(updated, new(15));
            Assert.Equal(changedSave, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(changedSave)));
        }
    }

    public enum PuzzleDamage { Id, Mode, Precision, Inventory, Body, Target, EventSource, Profile, Lock }
    [Theory]
    [InlineData(PuzzleDamage.Id)]
    [InlineData(PuzzleDamage.Mode)]
    [InlineData(PuzzleDamage.Precision)]
    [InlineData(PuzzleDamage.Inventory)]
    [InlineData(PuzzleDamage.Body)]
    [InlineData(PuzzleDamage.Target)]
    [InlineData(PuzzleDamage.EventSource)]
    [InlineData(PuzzleDamage.Profile)]
    [InlineData(PuzzleDamage.Lock)]
    public void InvalidPuzzleContextRejectsBeforeAdmission(PuzzleDamage damage)
    {
        var construction = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new((Half).45));
        var puzzle = construction.Puzzle;
        var changed = damage switch
        {
            PuzzleDamage.Id => construction with { Puzzle = puzzle with { Id = (WorkshopPuzzleId)999 } },
            PuzzleDamage.Mode => construction with { Puzzle = puzzle with { Placement = (WorkshopPlacementMode)999 } },
            PuzzleDamage.Precision => construction with { Puzzle = puzzle with { Precision = new(Half.NaN) } },
            PuzzleDamage.Inventory => construction with { Puzzle = puzzle with { InventoryCount = 3 } },
            PuzzleDamage.Body => construction with { Puzzle = puzzle with { Goal = puzzle.Goal with { Body = new(9) } } },
            PuzzleDamage.Target => construction with { Puzzle = puzzle with { Goal = puzzle.Goal with { Target = new(9) } } },
            PuzzleDamage.EventSource => construction with { Puzzle = puzzle with { Goal = puzzle.Goal with { EventSource = new(9) } } },
            PuzzleDamage.Profile => construction with { Puzzle = puzzle with { ReceiverAssistance = default } },
            PuzzleDamage.Lock => construction.WithInstance(construction.Ball!.Value with { Locked = false }),
            _ => throw new ArgumentOutOfRangeException(nameof(damage))
        };
        // A puzzle rule refuses the change; a non-finite precision is a structural bound (an argument guard).
        var error = Assert.ThrowsAny<ArgumentException>(() => WorkshopSaveCodec.Encode(new(changed, new(10))));
        Assert.Equal(damage == PuzzleDamage.Precision ? typeof(ArgumentException) : typeof(WorkshopEditRejectedException), error.GetType());
        construction.Validate();
    }

    [Fact]
    public void PuzzlePaddingAndPreviousSaveSchemaReject()
    {
        var construction = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new((Half)1));
        var bytes = WorkshopSaveCodec.Encode(new(construction, new(3)));
        bytes[24 + 48 + 246] = 1;
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(bytes));
        bytes[24 + 48 + 246] = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 1);
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(bytes));
    }

    [Fact]
    public void NamedGoalUsesCommittedBodySensorAndEpochWithoutAnimationFeedback()
    {
        var construction = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new((Half)1));
        var goal = construction.Puzzle.Goal; var ball = construction.Ball!.Value;
        var body = new CanonicalBody(ball.Id, 2, 1, ball.Cell, ball.Local, default);
        var bodies = new PhysicsBodyReadSet(new[] { new PhysicsBodyRead(body, ball.Rotation, default, default) });
        var read = new WorkshopRead(new(2), new(1), bodies, Captures: new([new(goal.EventSource, CaptureLatchPhase.Latched, 4, (Half)0)]));
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, read, new(2)));
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, read, new(2)));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read, new(3)));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read with { Bodies = new(new[] { bodies[0] with { Body = body with { Id = new(9) } } }) }, new(2)));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read with { Captures = new([CaptureLatch.Clear(goal.EventSource)]) }, new(2)));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read with { Captures = new([new(new(9), CaptureLatchPhase.Latched, 4, (Half)0)]) }, new(2)));
    }

    [Fact]
    public void CanonicalAssistanceKeepsEveryAuthoredSourceField()
    {
        using var source = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "content/puzzles.json")));
        var level = source.RootElement[0];
        Assert.Equal("first_principles", level.GetProperty("id").GetString());
        Check(level.GetProperty("parts")[0].GetProperty("difficulty"), FirstPrinciples.BallAssistance);
        Check(level.GetProperty("parts")[1].GetProperty("difficulty"), FirstPrinciples.ReceiverAssistance);
        foreach (var ramp in level.GetProperty("solution").EnumerateArray()) Check(ramp.GetProperty("difficulty"), FirstPrinciples.RampAssistance);
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
                    Assert.Equal(BitConverter.HalfToUInt16Bits((Half)data[i].GetProperty(names[field]).GetDouble()), BitConverter.HalfToUInt16Bits(actual[field]));
            }
        }
    }

    private static WorkshopSavedConstruction ActivationSaved()
    {
        var trigger = new WorkshopSwitch(new(2), new(0, 16, 0), default, CanonicalRotation.Identity, ContactTriggerSettings.Default);
        var lamp = new WorkshopLamp(new(3), new(48, 16, 0), default, CanonicalRotation.Identity);
        var link = new WorkshopConnection(trigger.Id, WorkshopSocket.ActivationOut, lamp.Id,
            WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
        return new(new(new(4), WorkshopCadenceSettings.Default(), new(trigger, lamp), Connections: new(link)), new(4));
    }

    [Fact]
    public void ActivationInstancesThresholdAndTypedEdgeRoundTripExactly()
    {
        var save = ActivationSaved();
        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(bytes);
        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
        Assert.Equal((Half).8, ((WorkshopSwitch)decoded.Construction.Instances[0]).Trigger.Threshold.Value);
        // Authored values are canonical data, never replaced by the Free Workshop default.
        var changed = save with { Construction = save.Construction.WithInstance(
            ((WorkshopSwitch)save.Construction.Instances[0]) with { Trigger = new(new((Half).47)) }) };
        Assert.Equal(changed, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(changed)));
        Assert.Single(decoded.Construction.Connections);
    }

    [Fact]
    public void ConnectionsRejectWrongDomainPortDirectionIdentityAndDuplicates()
    {
        var save = ActivationSaved(); var construction = save.Construction; var link = construction.Connections[0];
        foreach (var invalid in new[]
        {
            link with { Domain = WorkshopConnectionDomain.Electrical },
            link with { Domain = (WorkshopConnectionDomain)99 },
            link with { Output = WorkshopSocket.PowerIn },
            link with { Input = WorkshopSocket.Supply },
            link with { Source = link.Target, Target = link.Source },
            link with { Target = new(99) }, link with { Target = link.Source },
            link with { Input = (WorkshopSocket)99 }
        })
            EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (construction with { Connections = new(invalid) }).Validate());
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (construction with { Connections = new(link, link) }).Validate());
        var source = new[] { link }; var immutable = new WorkshopConnections(source); source[0] = default;
        Assert.Equal(link, immutable[0]);
        Assert.Single(construction.Connections);
        Assert.Empty(construction.WithoutInstance(link.Source).Connections);
        Assert.Empty(construction.WithoutInstance(link.Target).Connections);
    }

    [Fact]
    public void ACommandWithNonZeroHeaderPaddingRejectsBeforeAdmission()
    {
        var save = ActivationSaved();
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), save.Construction,
            Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var wire = WorkshopWire.Encode(command);
        Assert.Equal(command, WorkshopWire.DecodeCommand(wire));
        BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(12), 9);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeCommand(wire));
    }

    [Fact]
    public void FirstPrinciplesRejectsForeignInstancesAndConnections()
    {
        var puzzle = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(11), new(12), new((Half)1));
        var activation = ActivationSaved().Construction;
        foreach (var instance in activation.Instances)
            EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => puzzle.WithInstance(instance).Validate());
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => (puzzle with { Connections = activation.Connections }).Validate());
    }

    [Theory]
    [InlineData(99UL, 3UL, (uint)WorkshopConnectionDomain.Activation, (uint)WorkshopSocket.ActivationOut, (uint)WorkshopSocket.ActivationIn)]
    [InlineData(2UL, 99UL, (uint)WorkshopConnectionDomain.Activation, (uint)WorkshopSocket.ActivationOut, (uint)WorkshopSocket.ActivationIn)]
    [InlineData(2UL, 3UL, (uint)WorkshopConnectionDomain.Electrical, (uint)WorkshopSocket.ActivationOut, (uint)WorkshopSocket.ActivationIn)]
    [InlineData(2UL, 3UL, (uint)WorkshopConnectionDomain.Activation, (uint)WorkshopSocket.ActivationIn, (uint)WorkshopSocket.ActivationIn)]
    [InlineData(2UL, 3UL, (uint)WorkshopConnectionDomain.Activation, (uint)WorkshopSocket.ActivationOut, (uint)WorkshopSocket.ActivationOut)]
    public void SerializedInvalidConnectionsRejectBeforeAdmission(ulong source, ulong target, uint domain, uint fromSocket, uint toSocket)
    {
        var save = ActivationSaved();
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var instanceOffsets = new Google.FlatBuffers.Offset<CuriousContraptions.Wire.InstanceRecord>[save.Construction.Instances.Count];
        for (var i = 0; i < save.Construction.Instances.Count; i++)
        {
            var inst = save.Construction.Instances[i];
            var partIdOffset = builder.CreateString(inst.PartId.Value);
            CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
            CuriousContraptions.Wire.InstanceRecord.AddId(builder, inst.Id.Value);
            CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, partIdOffset);
            CuriousContraptions.Wire.InstanceRecord.AddLocked(builder, inst.Locked);
            CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, inst.Cell.X, inst.Cell.Y, inst.Cell.Z));
            CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, inst.Local.X, inst.Local.Y, inst.Local.Z));
            CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, inst.Rotation.X, inst.Rotation.Y, inst.Rotation.Z, inst.Rotation.W));
            instanceOffsets[i] = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);
        }
        var instancesVector = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, instanceOffsets);
        CuriousContraptions.Wire.ConstructionRecord.StartConnectionsVector(builder, 1);
        CuriousContraptions.Wire.ConnectionRecord.CreateConnectionRecord(builder, source, target, domain, fromSocket, toSocket);
        var connectionsVector = builder.EndVector();
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, save.Construction.Revision.Value);
        var saveCadence = save.Construction.Settings;
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder, CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(builder, (uint)saveCadence.Simulation, (uint)saveCadence.Physical, (uint)saveCadence.Animation, (uint)saveCadence.Presentation, saveCadence.PresentationRate.Numerator, saveCadence.PresentationRate.Denominator));
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instancesVector);
        CuriousContraptions.Wire.ConstructionRecord.AddConnections(builder, connectionsVector);
        var crOffset = CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder);
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, WorkshopSaveCodec.Magic);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, save.NextBodyId.Value);
        CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, crOffset);
        var end = CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder);
        builder.Finish(end.Value);
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => WorkshopSaveCodec.Decode(builder.SizedByteArray()));
    }
}
