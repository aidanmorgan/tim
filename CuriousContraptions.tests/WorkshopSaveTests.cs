using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WorkshopSaveTests
{
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
        Assert.Equal(WorkshopSaveCodec.ByteLength, bytes.Length);
        Assert.Equal(saved, WorkshopSaveCodec.Decode(bytes));
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(bytes)));
        var empty = new WorkshopSavedConstruction(new(new(1), WorkshopCadenceSettings.Default(), WorkshopInstances.Empty), new(1));
        Assert.Equal(empty, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(empty)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    public void UnknownHeaderCannotBeLoaded(int offset)
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        bytes[offset] ^= 128;
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
    }

    [Fact]
    public void TruncatedTrailingAndInvalidAllocatorReject()
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes.AsSpan(1)));
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode([..bytes, 0]));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16), 3);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Encode(Saved() with { NextBodyId = new(3) }));
    }

    [Theory]
    [InlineData(32)]
    [InlineData(152)]
    public void UnsupportedPartPopulationRejectsBeforeAdmission(int offset)
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), uint.MaxValue);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
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
        Assert.Throws<ArgumentException>(excess.Validate);
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
        Assert.Equal(2u, construction.Puzzle.RampInventory);
        var updated = FirstPrinciples.WithPrecision(construction, new((Half).5));
        Assert.Equal(a, updated.Instances[2]); Assert.Equal(b, updated.Instances[3]);
        Assert.Equal(construction.Puzzle.Goal, updated.Puzzle.Goal);
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
            PuzzleDamage.Inventory => construction with { Puzzle = puzzle with { RampInventory = 3 } },
            PuzzleDamage.Body => construction with { Puzzle = puzzle with { Goal = puzzle.Goal with { Body = new(9) } } },
            PuzzleDamage.Target => construction with { Puzzle = puzzle with { Goal = puzzle.Goal with { Target = new(9) } } },
            PuzzleDamage.EventSource => construction with { Puzzle = puzzle with { Goal = puzzle.Goal with { EventSource = new(9) } } },
            PuzzleDamage.Profile => construction with { Puzzle = puzzle with { ReceiverAssistance = default } },
            PuzzleDamage.Lock => construction.WithInstance(construction.Ball!.Value with { Locked = false }),
            _ => throw new ArgumentOutOfRangeException(nameof(damage))
        };
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Encode(new(changed, new(10))));
        construction.Validate();
    }

    [Fact]
    public void PuzzlePaddingAndPreviousSaveSchemaReject()
    {
        var construction = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new((Half)1));
        var bytes = WorkshopSaveCodec.Encode(new(construction, new(3)));
        bytes[24 + 48 + 246] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
        bytes[24 + 48 + 246] = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 1);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
    }

    [Fact]
    public void NamedGoalUsesCommittedBodySensorAndEpochWithoutAnimationFeedback()
    {
        var construction = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new((Half)1));
        var goal = construction.Puzzle.Goal; var ball = construction.Ball!.Value;
        var body = new CanonicalBody(ball.Id, 2, 1, ball.Cell, ball.Local, default);
        var read = new WorkshopRead(new(2), new(1), body, Captures: new([new(goal.EventSource, CaptureLatchPhase.Latched, 4, (Half)0)]));
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, read, new(2)));
        Assert.Equal(WorkshopGoalPhase.Solved, WorkshopGoalEvaluator.Evaluate(goal, read, new(2)));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read, new(3)));
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, read with { Ball = body with { Id = new(9) } }, new(2)));
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
            Assert.Throws<ArgumentException>(() => (construction with { Connections = new(invalid) }).Validate());
        Assert.Throws<ArgumentException>(() => (construction with { Connections = new(link, link) }).Validate());
        var source = new[] { link }; var immutable = new WorkshopConnections(source); source[0] = default;
        Assert.Equal(link, immutable[0]);
        Assert.Single(construction.Connections);
        Assert.Empty(construction.WithoutInstance(link.Source).Connections);
        Assert.Empty(construction.WithoutInstance(link.Target).Connections);
    }

    [Fact]
    public void ConnectionPaddingCountsAndPriorSchemasRejectBeforeAdmission()
    {
        var save = ActivationSaved();
        foreach (var offset in new[] { 24 + 12, 24 + WorkshopWire.ConnectionsOffset + 28,
            24 + WorkshopWire.ConnectionsOffset + WorkshopWire.ConnectionBytes })
        {
            var bytes = WorkshopSaveCodec.Encode(save); bytes[offset] = 255;
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
        }
        var previous = WorkshopSaveCodec.Encode(save);
        BinaryPrimitives.WriteUInt32LittleEndian(previous.AsSpan(4), 2);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(previous));
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
            Assert.Throws<ArgumentException>(() => puzzle.WithInstance(instance).Validate());
        Assert.Throws<ArgumentException>(() => (puzzle with { Connections = activation.Connections }).Validate());
    }

    [Theory]
    [InlineData(0, 99u)]
    [InlineData(8, 99u)]
    [InlineData(16, (uint)WorkshopConnectionDomain.Electrical)]
    [InlineData(20, (uint)WorkshopSocket.ActivationIn)]
    [InlineData(24, (uint)WorkshopSocket.ActivationOut)]
    public void SerializedInvalidConnectionsRejectBeforeAdmission(int lane, uint value)
    {
        var bytes = WorkshopSaveCodec.Encode(ActivationSaved());
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(24 + WorkshopWire.ConnectionsOffset + lane), value);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(bytes));
    }
}
