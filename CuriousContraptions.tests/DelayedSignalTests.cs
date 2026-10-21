using System.Buffers.Binary;
using System.Text.Json;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class DelayedSignalTests
{

    [Fact]
    public void AuthoredSourceIdentityRejectsMalformedUnknownMissingAndDuplicateFieldsAtomically()
    {
        var before = Lesson((Half).45);
        var fixture = AuthoredParameterFixture.Load("content.fixtures.delayed_signal_source.json");
        Assert.Equal(PartId.ImpactSwitch, fixture.Part);
        Assert.Equal(new ParameterId("trigger_threshold"), fixture.Parameter);
        foreach (var json in new[]
        {
            "{}", "[]", "{\"part\":\"switch\"}",
            "{\"part\":\"unknown\",\"parameter\":\"trigger_threshold\"}",
            "{\"part\":\"switch\",\"parameter\":\"unknown\"}",
            "{\"part\":\"switch\",\"part\":\"switch\",\"parameter\":\"trigger_threshold\"}",
            "{\"part\":\"switch\",\"parameter\":\"trigger_threshold\",\"extra\":\"x\"}",
            "{\"part\":1,\"parameter\":\"trigger_threshold\"}"
        }) Assert.Throws<ArgumentException>(() => AuthoredParameterFixture.Parse(json));
        Assert.ThrowsAny<JsonException>(() => AuthoredParameterFixture.Parse("{"));
        Assert.Throws<System.IO.InvalidDataException>(() => AuthoredParameterFixture.Load("missing.fixture"));
        Assert.Equal(before, Lesson((Half).45));
        var reversed = before with { Instances = new(before.Instances.Reverse().ToArray()) };
        reversed.Validate(); // source lookup uses identity, not first generic-instance position.
    }

    [Fact]
    public void F32PhaseAtFarOriginKeepsTinyOffsetsAndOrdinalBorrow()
    {
        var start = new ActivationTime(uint.MaxValue - 480, 0f);
        var at = new ActivationTime(uint.MaxValue, 0f);
        Assert.False(WorkshopGoalEvaluator.ElapsedAtLeast(start, at with { Phase = -float.Epsilon }, new((Half)1)));
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(start, at, new((Half)1)));
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(start, at with { Phase = float.Epsilon }, new((Half)1)));
        var borrowed = new ActivationTime(uint.MaxValue - 481, 2047f);
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(borrowed, at with { Phase = -2047f }, new((Half)1)));
        Assert.False(WorkshopGoalEvaluator.ElapsedAtLeast(borrowed,
            new(uint.MaxValue - 1, -2047f), new((Half)1)));
    }

    private static WorkshopConstruction Lesson(Half precision) =>
        DelayedSignal.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new(3), new(precision));

    [Theory]
    [InlineData(0, .2)]
    [InlineData(.45, .47)]
    [InlineData(1, .8)]
    public void SourceFixturesInventoryThresholdAndCanonicalRoundtrip(double precision, double threshold)
    {
        var construction = Lesson((Half)precision);
        Assert.Equal(PartId.Delay, construction.Puzzle.InventoryPart);
        Assert.Equal(1u, construction.Puzzle.InventoryCount);
        Assert.All(construction.Instances, value => Assert.True(value.Locked));
        Assert.Equal(new CellOrigin(-48,64,0), construction.Ball!.Value.Cell);
        Assert.Equal(new CellOrigin(-48,16,0), construction.Instances.Find<WorkshopGenericInstance>()!.Value.Cell);
        Assert.Equal(new CellOrigin(48,16,0), construction.Instances.Find<WorkshopLamp>()!.Value.Cell);
        Assert.Equal((float)(Half)threshold, construction.Instances.Find<WorkshopGenericInstance>()!.Value.Parameters[new ParameterId("trigger_threshold")]);
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1,2));
        Assert.Equal(1,scene.Triggers.Length);
        Assert.Equal((float)(Half)threshold, scene.Triggers[0].Threshold.Value);
        var save = new WorkshopSavedConstruction(construction, new(4));
        Assert.Equal(save, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(save)));
        var changed = construction.WithInstance(WorkshopInput.Generic(PartId.Delay, new(4), 0, 1, 0, 0, 0, 0, 1));
        changed = changed with { Connections = new(new(new(2),WorkshopSocket.ActivationOut,new(4),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation),
            new(new(4),WorkshopSocket.ActivationOut,new(3),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation)) };
        Assert.Equal(changed, WorkshopSaveCodec.Decode(SaveFixture.Elements,WorkshopSaveCodec.Encode(new(changed,new(5)))).Construction);
        EditRefusal.Is(WorkshopEditRejection.InventoryUnsupported, () => changed.WithInstance(WorkshopInput.Receiver(new(5),0,1,0,0,0,0,1)).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => construction.WithInstance(construction.Ball!.Value with { Locked=false }).Validate());
        EditRefusal.Is(WorkshopEditRejection.FixedPart, () => (construction with { Puzzle=construction.Puzzle with { Goal=construction.Puzzle.Goal with { MinimumDelay=new((Half).5) } } }).Validate());
    }

    [Theory]
    [InlineData(-2048)]
    [InlineData(-.00006103515625)]
    [InlineData(0)]
    [InlineData(.00006103515625)]
    [InlineData(2047)]
    public void SignedPhaseJustBeforeAtAfterAndFarOrigin(double phase)
    {
        var start = new ActivationTime(1000,(float)phase);
        var at = new ActivationTime(1480,(float)phase);
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(start,at,new((Half)1)));
        var earlier = MathF.BitDecrement((float)phase);
        if (earlier >= -2048f)
            Assert.False(WorkshopGoalEvaluator.ElapsedAtLeast(start,at with { Phase=earlier },new((Half)1)));
        Assert.False(WorkshopGoalEvaluator.ElapsedAtLeast(at,start,new((Half)0)));
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(new(uint.MaxValue-480,(float)phase),new(uint.MaxValue,(float)phase),new((Half)1)));
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(new(1,0f),new(uint.MaxValue,0f),new((Half)120)));
    }

    [Fact]
    public void DyadicMinimumDoesNotRoundElapsedBeforeComparison()
    {
        Assert.False(WorkshopGoalEvaluator.ElapsedAtLeast(new(0,0f),new(4290,-1f),new((Half)8.9375)));
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(new(0,0f),new(4290,0f),new((Half)8.9375)));
        Assert.Throws<ArgumentException>(() => WorkshopGoalEvaluator.ElapsedAtLeast(default,default,new(Half.NaN)));
    }

    [Fact]
    public void GoalUsesTargetOccurrenceNotRootCauseOrTimerAndRejectsEarlyLatch()
    {
        var goal = Lesson((Half)1).Puzzle.Goal;
        var source = new ActivationTime(100,(float)12);
        var cause = new ActivationCause(new(7),new(1),new(9),source,new((float)2));
        var sourceNode = new ActivationNodeDeclaration(new(2),new(2),ActivationNodeKind.ContactSource,new(7));
        var targetNode = new ActivationNodeDeclaration(new(3),new(3),ActivationNodeKind.StickyFirstInput,default);
        var trigger = ActivationLatch.From(sourceNode,new(ActivationOccurrenceKind.Contact,new(2),source,cause));
        var delayed = ActivationLatch.From(targetNode,new(ActivationOccurrenceKind.TimerElapsed,new(4),new(584,0f),cause));
        var read = new WorkshopRead(new(2),new(146),default,Activations:new([trigger,delayed]));
        Assert.Equal(WorkshopGoalPhase.Solved,WorkshopGoalEvaluator.Evaluate(goal,read,new(2)));
        Assert.Equal(new ActivationTime(584,(float)0),WorkshopGoalEvaluator.Occurrence(goal,read,new(2)));
        Assert.Equal(WorkshopGoalPhase.Waiting,WorkshopGoalEvaluator.Evaluate(goal,read,new(3)));
        var early = ActivationLatch.From(targetNode,trigger.Occurrence);
        Assert.Equal(WorkshopGoalPhase.Waiting,WorkshopGoalEvaluator.Evaluate(goal,read with { Activations=new([trigger,early]) },new(2)));
        Assert.Equal(WorkshopGoalPhase.Waiting,WorkshopGoalEvaluator.Evaluate(goal,read with { Activations=new([ActivationLatch.Clear(sourceNode),delayed]) },new(2)));
        Assert.Equal(WorkshopGoalPhase.Waiting,WorkshopGoalEvaluator.Evaluate(goal,read with { Activations=new([trigger,delayed with { Owner=new(99) }]) },new(2)));
        Assert.Equal(WorkshopGoalPhase.Waiting,WorkshopGoalEvaluator.Evaluate(goal,new(new(3),new(0),default),new(3)));
    }

    [Fact]
    public void ExactAuthoredSourceAndEveryDifficultyFieldArePreserved()
    {
        using var source=JsonDocument.Parse(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(),"content/puzzles.json")));
        var level=source.RootElement.EnumerateArray().Single(p=>p.GetProperty("id").GetString()=="delayed_signal");
        Assert.Equal("Wait for it",level.GetProperty("title").GetString());
        Assert.Equal(1,level.GetProperty("inventory").GetProperty("delay").GetInt32());
        var goal=level.GetProperty("goals")[0];
        Assert.Equal("activated_after",goal.GetProperty("type").GetString());
        Assert.Equal("switch",goal.GetProperty("body").GetString());
        Assert.Equal("lamp",goal.GetProperty("target").GetString());
        Assert.Equal(1,goal.GetProperty("minimum_delay_seconds").GetInt32());
        foreach(var part in level.GetProperty("parts").EnumerateArray())
        {
            Assert.True(part.GetProperty("locked").GetBoolean());
            Check(part.GetProperty("difficulty"),DelayedSignal.FixedAssistance);
        }
        Check(level.GetProperty("solution")[0].GetProperty("difficulty"),DelayedSignal.DelayAssistance);
        static void Check(JsonElement data,AssistanceProfile profile)
        {
            var knots=new[]{profile.Forgiving,profile.Balanced,profile.Precise};
            string[] names=["precision","position_window","rotation_window","max_position_correction","max_rotation_correction","blend_seconds","capture_margin","capture_speed","capture_dwell","guide_acceleration","trigger_threshold"];
            for(var i=0;i<3;i++)
            {
                var k=knots[i];
                Half[] values=[k.Precision,k.PositionWindow.Value,k.RotationWindowDegrees,k.MaximumPositionCorrection.Value,k.MaximumRotationCorrectionDegrees,k.Blend.Value,k.CaptureMargin.Value,k.CaptureSpeed.Value,k.CaptureDwell.Value,k.GuideAcceleration.Value,k.TriggerThreshold];
                for(var j=0;j<values.Length;j++) Assert.Equal((Half)data[i].GetProperty(names[j]).GetDouble(),values[j]);
            }
        }
    }

    [Fact]
    public void PreviousSchemaAndForeignGoalDataReject()
    {
        var bytes=WorkshopSaveCodec.Encode(new(Lesson((Half)1),new(4)));
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4),5);
        SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes));
        var goal=Lesson((Half)1).Puzzle.Goal;
        Assert.Throws<ArgumentException>(() => (goal with { Body=new(1) }).Validate());
        Assert.Throws<ArgumentException>(() => (goal with { SourceNode=goal.TargetNode }).Validate());
        var command=WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1),WorkshopCommandKind.Construct,new(1),new(1),Lesson((Half)1),Session:new(1,2),Cadence:new(1),Projection:new(1)));
        Assert.Equal(WireFaultReason.TrailingBytes, Assert.Throws<WireFaultException>(()=>WorkshopWireFlatBuffers.DecodeCommand([.. command, 12])).Reason);
    }
}
