using System.Buffers.Binary;
using System.Text.Json;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class DelayedSignalTests
{
    private static WorkshopConstruction Lesson(Half precision) =>
        DelayedSignal.Create(new(1), WorkshopCadenceSettings.Default(), new(1), new(2), new(3), new(precision));

    [Theory]
    [InlineData(0, .2)]
    [InlineData(.45, .47)]
    [InlineData(1, .8)]
    public void SourceFixturesInventoryThresholdAndCanonicalRoundtrip(double precision, double threshold)
    {
        var construction = Lesson((Half)precision);
        Assert.Equal(WorkshopPartKind.Delay, construction.Puzzle.InventoryKind);
        Assert.Equal(1u, construction.Puzzle.InventoryCount);
        Assert.All(construction.Instances, value => Assert.True(value.Locked));
        Assert.Equal(new CellOrigin(-48,64,0), construction.Ball!.Value.Cell);
        Assert.Equal(new CellOrigin(-48,16,0), construction.Instances.Find<WorkshopSwitch>()!.Value.Cell);
        Assert.Equal(new CellOrigin(48,16,0), construction.Instances.Find<WorkshopLamp>()!.Value.Cell);
        Assert.Equal((Half)threshold, construction.Instances.Find<WorkshopSwitch>()!.Value.Trigger.Threshold.Value);
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1,2));
        Assert.Equal(1,scene.Triggers.Length);
        Assert.Equal((Half)threshold, scene.Triggers[0].Threshold.Value);
        var save = new WorkshopSavedConstruction(construction, new(4));
        Assert.Equal(save, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(save)));
        var changed = construction.WithInstance(WorkshopInput.Delay(new(4),0,1,0,0,0,0,1,DelayDuration.Default));
        changed = changed with { Connections = new(new(new(2),WorkshopSocket.ActivationOut,new(4),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation),
            new(new(4),WorkshopSocket.ActivationOut,new(3),WorkshopSocket.ActivationIn,WorkshopConnectionDomain.Activation)) };
        Assert.Equal(changed, WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(new(changed,new(5)))).Construction);
        Assert.Throws<ArgumentException>(() => changed.WithInstance(WorkshopInput.Receiver(new(5),0,1,0,0,0,0,1)).Validate());
        Assert.Throws<ArgumentException>(() => construction.WithInstance(construction.Ball!.Value with { Locked=false }).Validate());
        Assert.Throws<ArgumentException>(() => (construction with { Puzzle=construction.Puzzle with { Goal=construction.Puzzle.Goal with { MinimumDelay=new((Half).5) } } }).Validate());
    }

    [Theory]
    [InlineData(-2048)]
    [InlineData(-.00006103515625)]
    [InlineData(0)]
    [InlineData(.00006103515625)]
    [InlineData(2047)]
    public void SignedPhaseJustBeforeAtAfterAndFarOrigin(double phase)
    {
        var start = new ActivationTime(1000,(Half)phase);
        var at = new ActivationTime(1480,(Half)phase);
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(start,at,new((Half)1)));
        var earlier = Half.BitDecrement((Half)phase);
        if (earlier >= (Half)(-2048))
            Assert.False(WorkshopGoalEvaluator.ElapsedAtLeast(start,at with { Phase=earlier },new((Half)1)));
        Assert.False(WorkshopGoalEvaluator.ElapsedAtLeast(at,start,new((Half)0)));
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(new(uint.MaxValue-480,(Half)phase),new(uint.MaxValue,(Half)phase),new((Half)1)));
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(new(1,(Half)0),new(uint.MaxValue,(Half)0),new((Half)120)));
    }

    [Fact]
    public void DyadicMinimumDoesNotRoundElapsedBeforeComparison()
    {
        Assert.False(WorkshopGoalEvaluator.ElapsedAtLeast(new(0,(Half)0),new(4290,(Half)(-1)),new((Half)8.9375)));
        Assert.True(WorkshopGoalEvaluator.ElapsedAtLeast(new(0,(Half)0),new(4290,(Half)0),new((Half)8.9375)));
        Assert.Throws<ArgumentException>(() => WorkshopGoalEvaluator.ElapsedAtLeast(default,default,new(Half.NaN)));
    }

    [Fact]
    public void GoalUsesTargetOccurrenceNotRootCauseOrTimerAndRejectsEarlyLatch()
    {
        var goal = Lesson((Half)1).Puzzle.Goal;
        var source = new ActivationTime(100,(Half)12);
        var cause = new ActivationCause(new(7),new(1),new(9),source,new((Half)2));
        var sourceNode = new ActivationNodeDeclaration(new(2),new(2),ActivationNodeKind.ContactSource,new(7));
        var targetNode = new ActivationNodeDeclaration(new(3),new(3),ActivationNodeKind.Latch,default);
        var trigger = ActivationLatch.From(sourceNode,new(ActivationOccurrenceKind.Contact,new(2),source,cause));
        var delayed = ActivationLatch.From(targetNode,new(ActivationOccurrenceKind.TimerElapsed,new(4),new(584,(Half)0),cause));
        var read = new WorkshopRead(new(2),new(146),default,Activations:new([trigger,delayed]));
        Assert.Equal(WorkshopGoalPhase.Solved,WorkshopGoalEvaluator.Evaluate(goal,read,new(2)));
        Assert.Equal(new ActivationTime(584,(Half)0),WorkshopGoalEvaluator.Occurrence(goal,read,new(2)));
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
        Assert.Throws<ArgumentException>(()=>WorkshopSaveCodec.Decode(bytes));
        var goal=Lesson((Half)1).Puzzle.Goal;
        Assert.Throws<ArgumentException>(()=>(goal with { Body=new(1) }).Validate());
        Assert.Throws<ArgumentException>(()=>(goal with { SourceNode=goal.TargetNode }).Validate());
        var command=WorkshopWire.Encode(new WorkshopCommand(new(1),WorkshopCommandKind.Construct,new(1),new(1),Lesson((Half)1),Session:new(1,2),Cadence:new(1),Projection:new(1)));
        BinaryPrimitives.WriteUInt32LittleEndian(command.AsSpan(12),12);
        Assert.Throws<ArgumentException>(()=>WorkshopWire.DecodeCommand(command));
    }
}
