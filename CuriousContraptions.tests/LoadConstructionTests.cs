using Godot;
using CuriousContraptions.Bridge;
using System.Text.Json;
namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class LoadConstructionTests(NativeSceneFixture godot)
{
    public enum CallbackStage { Build, Assistance, EnterTree }
    public enum ReentrantOperation { Load, Reset, Step, Start, QueueControl, PeekResult, AcknowledgeResult, Gravity, Pause, Snapshot }
    public enum FailurePoint { None, Build, Assistance }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId ProbeCatalogue=new("load_probe"),BatteryCatalogue=new("battery");
    private partial class Probe : MachinePart
    {
        public static FailurePoint Failure;
        public static int Builds;
        public static Action<CallbackStage>? Callback;
        protected override void Build()
        {
            Builds++;
            Callback?.Invoke(CallbackStage.Build);
            if(Failure==FailurePoint.Build)throw new InvalidOperationException("Injected construction failure.");
        }
        public override void UpdateAssistance(float precision)
        {
            Callback?.Invoke(CallbackStage.Assistance);
            if(Failure==FailurePoint.Assistance)throw new InvalidOperationException("Injected assistance failure.");
        }
    }
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);

    [Theory]
    [InlineData(FailurePoint.Build)]
    [InlineData(FailurePoint.Assistance)]
    public void FailedReplacementPreservesRunningWorldAndQueuedCommand(FailurePoint failure)
    {
        var world=World();
        var source=new Probe();
        using var scene=new PackedScene();
        using var definition=new PartDefinition {Id=ProbeCatalogue.Value,Scene=scene};
        try
        {
            Assert.Equal(Error.Ok,scene.Pack(source));
            world.Registry.Definitions.Add(ProbeCatalogue.Value,definition);
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=BatteryCatalogue.Value});
            var construction=Saved(world);world.Start();world.Step();
            var generation=world.ControlGeneration;var physics=world.Physics;
            var states=physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            var command=world.QueueBinaryInput(new(battery,BatteryPart.EnableInput),BinaryInputState.Disabled);
            Probe.Failure=failure;Probe.Builds=0;
            var replacement=new MachineData {Parts=[new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=ProbeCatalogue.Value}]};
            Assert.Throws<InvalidOperationException>(()=>world.LoadMachine(replacement));
            Assert.Equal(1,Probe.Builds);
            Assert.Equal(MachineWorldPhase.Idle,world.Phase);
            Assert.Same(physics,world.Physics);Assert.Equal(states,physics.Capture().BodyStates.ToArray());
            Assert.Equal(ticks,world.Ticks);Assert.True(world.Running);
            Assert.Equal(generation,world.ControlGeneration);Assert.False(world.TryPeekControlResult(out _));
            world.Step();
            Assert.True(world.TryPeekControlResult(out var result));Assert.Equal(command,result.Sequence);
            world.AcknowledgeControlResult(result.Id);
            Assert.Equal(CommandOutcome.Applied,result.Outcome);
            Probe.Failure=FailurePoint.None;
            world.Restore();Assert.Equal(construction,Saved(world));
            Probe.Builds=0;
            world.LoadMachine(replacement);
            Assert.Equal(1,Probe.Builds);
            Assert.Single(world.Parts);
            Assert.Equal(generation.Value+2,world.ControlGeneration.Value);
        }
        finally {Probe.Failure=FailurePoint.None;world.Free();source.Free();}
    }

    public static IEnumerable<object[]> ReentrantCases()
    {
        foreach(var stage in Enum.GetValues<CallbackStage>())
        foreach(var operation in Enum.GetValues<ReentrantOperation>())
            yield return [stage,operation];
    }

    [Theory]
    [MemberData(nameof(ReentrantCases))]
    public void ReplacementCallbacksCannotReenterOrPublishPartialConstruction(CallbackStage stage,ReentrantOperation operation)
    {
        var world=World();
        var source=new Probe();
        using var scene=new PackedScene();
        using var definition=new PartDefinition {Id=ProbeCatalogue.Value,Scene=scene};
        try
        {
            Assert.Equal(Error.Ok,scene.Pack(source));
            world.Registry.Definitions.Add(ProbeCatalogue.Value,definition);
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=BatteryCatalogue.Value});
            world.Start();world.Step();
            var generation=world.ControlGeneration;
            var command=world.QueueBinaryInput(new(battery,BatteryPart.EnableInput),BinaryInputState.Disabled);
            var observed=false;
            world.ChildEnteredTree+=child=>
            {
                if(child is Probe)Probe.Callback?.Invoke(CallbackStage.EnterTree);
            };
            Probe.Callback=current=>
            {
                if(current!=stage)return;
                Probe.Callback=null;
                observed=true;
                Assert.Equal(MachineWorldPhase.Replacing,world.Phase);
                Assert.Throws<InvalidOperationException>(()=>
                {
                    switch(operation)
                    {
                        case ReentrantOperation.Load:world.LoadMachine(new());break;
                        case ReentrantOperation.Reset:world.Restore();break;
                        case ReentrantOperation.Step:world.Step();break;
                        case ReentrantOperation.Start:world.Start();break;
                        case ReentrantOperation.QueueControl:world.QueueBinaryInput(new(battery,BatteryPart.EnableInput),BinaryInputState.Enabled);break;
                        case ReentrantOperation.PeekResult:world.TryPeekControlResult(out _);break;
                        case ReentrantOperation.AcknowledgeResult:world.AcknowledgeControlResult(new(generation,command));break;
                        case ReentrantOperation.Gravity:world.Gravity=3;break;
                        case ReentrantOperation.Pause:world.Running=false;break;
                        case ReentrantOperation.Snapshot:world.Snapshot();break;
                        default:throw new ArgumentOutOfRangeException(nameof(operation));
                    }
                });
            };
            world.LoadMachine(new(){Parts=[new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=ProbeCatalogue.Value}]});
            Assert.True(observed);
            Assert.Equal(MachineWorldPhase.Idle,world.Phase);
            Assert.Equal(generation.Value+1,world.ControlGeneration.Value);
            Assert.Single(world.Parts);Assert.False(world.HasPhysicsState);Assert.False(world.Running);
            Assert.True(world.TryPeekControlResult(out var result));
            world.AcknowledgeControlResult(result.Id);
            Assert.Equal(command,result.Sequence);Assert.Equal(CommandOutcome.InvalidatedByBarrier,result.Outcome);
            Assert.Equal(generation,result.Generation);
            Assert.False(world.TryPeekControlResult(out _));
            Assert.Single(world.Snapshot().Parts);
        }
        finally {Probe.Callback=null;world.Free();source.Free();}
    }

    [Fact]
    public void EveryCataloguePartConstructsBeforeAttachmentAndSurvivesLoadRoundTrip()
    {
        var world=World();
        try
        {
            foreach(var definition in world.Registry.Definitions.Values)
            {
                var data=new MachineData {Parts=[new(){Id=definition.Id,Kind=definition.Id,Position=[0,6,0]}]};
                world.LoadMachine(data);
                var saved=Saved(world);
                world.LoadMachine(world.Snapshot());
                Assert.Equal(saved,Saved(world));
                Assert.All(world.Parts,part=>Assert.True(part.IsNodeReady()));
            }
        }
        finally {world.Free();}
    }
}
