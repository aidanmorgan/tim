using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class MachineStepLifecycleTests(NativeSceneFixture godot)
{
    private const string BallCatalogId="ball";
    public enum CallbackStage { Networks, Preparation, Observation }
    public enum LifecycleOperation { Step, Reset, Load, Gravity, Pressure, Precision, Realistic }
    private partial class CallbackBall : BallPart
    {
        public Action<MachineWorld,CallbackStage>? Callback { get; set; }
        public override void BeforeNetworks(MachineWorld world)=>Callback?.Invoke(world,CallbackStage.Networks);
        public override void PreparePhysics(MachineWorld world,float delta)=>Callback?.Invoke(world,CallbackStage.Preparation);
        public override void ObservePhysics(MachineWorld world,float delta)=>Callback?.Invoke(world,CallbackStage.Observation);
    }
    private partial class ControllerBall : CallbackBall
    {
        public static readonly TimerSlot Timer=new();
        public static readonly OscillatorSlot Oscillator=new();
        public static readonly CounterSlot Counter=new();
        public static readonly LatchSlot Latch=new();
        public override IReadOnlyList<SceneTimerDeclaration> SimulationTimers=>
            [new(new(this,Timer),1,TimerCompletionPolicy.Latch,TimerBoundary.BeforePhysics,TimerElapsedSignal.None)];
        public override IReadOnlyList<SceneOscillatorDeclaration> SimulationOscillators=>
            [new(new(this,Oscillator),1,SocketId.PowerIn)];
        public override IReadOnlyList<SceneCounterDeclaration> SimulationCounters=>[new(new(this,Counter),2)];
        public override IReadOnlyList<SceneLatchDeclaration> SimulationLatches=>[new(new(this,Latch))];
        public override IEnumerable<ConnectionPort> ConnectionPorts=>
            [new(SocketId.PowerIn,ConnectionDomain.Electrical,PortDirection.Input,Vector3.Zero)];
    }

    [Fact]
    public void FailedObservationRollsBackAllRegisteredControllersBeforeRetry()
    {
        var world=World();
        try
        {
            var part=new ControllerBall {Definition=world.Registry.Definitions[BallCatalogId]};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=BallCatalogId,Position=[0,6,0]});
            world.AttachPart(part);
            var construction=Saved(world);
            world.Start();
            var timer=world.ReadTimer(new(part,ControllerBall.Timer));
            var oscillator=world.ReadOscillator(new(part,ControllerBall.Oscillator));
            var counter=world.ReadCounter(new(part,ControllerBall.Counter));
            var latch=world.ReadLatch(new(part,ControllerBall.Latch));
            var timerTick=world.Timers.Tick;
            var oscillatorTick=world.Oscillators.Tick;
            var latchTick=world.Latches.Tick;
            part.Callback=(current,stage)=>
            {
                if(stage==CallbackStage.Networks)
                {
                    current.TriggerTimer(new(part,ControllerBall.Timer));
                    current.IncrementCounter(new(part,ControllerBall.Counter));
                    current.SubmitLatch(new(part,ControllerBall.Latch),SimulationLatchCommand.Set);
                }
                if(stage==CallbackStage.Observation)throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(timer,world.ReadTimer(new(part,ControllerBall.Timer)));
            Assert.Equal(oscillator,world.ReadOscillator(new(part,ControllerBall.Oscillator)));
            Assert.Equal(counter,world.ReadCounter(new(part,ControllerBall.Counter)));
            Assert.Equal(latch,world.ReadLatch(new(part,ControllerBall.Latch)));
            Assert.Equal(timerTick,world.Timers.Tick);
            Assert.Equal(oscillatorTick,world.Oscillators.Tick);
            Assert.Equal(latchTick,world.Latches.Tick);
            Assert.Equal(SimulationTransactionPhase.Idle,world.Timers.TransactionPhase);
            Assert.Equal(SimulationTransactionPhase.Idle,world.Oscillators.TransactionPhase);
            Assert.Equal(SimulationTransactionPhase.Idle,world.Counters.TransactionPhase);
            Assert.Equal(SimulationTransactionPhase.Idle,world.Latches.TransactionPhase);
            part.Callback=null;
            world.Step();
            Assert.Equal(1,world.Ticks);
            world.Restore();
            Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    private static string Saved(MachineWorld world)=>
        System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    public static IEnumerable<object[]> ReentrantCases()
    {
        foreach(var stage in Enum.GetValues<CallbackStage>())
        foreach(var operation in Enum.GetValues<LifecycleOperation>())
        foreach(var handled in new[]{false,true})
            yield return [stage,operation,handled];
    }
    [Theory]
    [MemberData(nameof(ReentrantCases))]
    public void TickCallbacksCannotReenterOrReplaceTheLiveWorld(CallbackStage stage,LifecycleOperation operation,bool handled)
    {
        var world=World();
        try
        {
            var part=new CallbackBall {Definition=world.Registry.Definitions[BallCatalogId]};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=BallCatalogId,Position=[0,6,0]});
            world.AttachPart(part);
            var construction=Saved(world);world.Start();
            var physics=world.Physics;var assembly=world.PhysicsAssembly;
            var configuration=(world.Gravity,world.Pressure,world.Precision,world.Realistic);
            var calls=0;
            part.Callback=(current,at)=>
            {
                Assert.Equal(MachineWorldPhase.Stepping,current.Phase);
                if(at!=stage) return;
                calls++;
                var before=physics.Capture().BodyStates.ToArray();
                void Attempt()
                {
                    switch(operation)
                    {
                        case LifecycleOperation.Step: current.Step();break;
                        case LifecycleOperation.Reset: current.Restore();break;
                        case LifecycleOperation.Load: current.LoadMachine(new());break;
                        case LifecycleOperation.Gravity: current.Gravity=3;break;
                        case LifecycleOperation.Pressure: current.Pressure=2;break;
                        case LifecycleOperation.Precision: current.Precision=.9f;break;
                        case LifecycleOperation.Realistic: current.Realistic=true;break;
                        default: throw new ArgumentOutOfRangeException(nameof(operation));
                    }
                }
                if(!handled) {Attempt();return;}
                Assert.Throws<InvalidOperationException>(Attempt);
                Assert.Same(physics,current.Physics);
                Assert.Same(assembly,current.PhysicsAssembly);
                Assert.Same(part,Assert.Single(current.Parts));
                Assert.Equal(before,physics.Capture().BodyStates.ToArray());
                Assert.Equal(configuration,(current.Gravity,current.Pressure,current.Precision,current.Realistic));
            };
            if(handled) world.Step();
            else Assert.Throws<InvalidOperationException>(world.Step);
            Assert.True(calls>0);
            Assert.Equal(configuration,(world.Gravity,world.Pressure,world.Precision,world.Realistic));
            Assert.Equal(MachineWorldPhase.Idle,world.Phase);
            Assert.Equal(handled?1:0,world.Ticks);
            Assert.Same(physics,world.Physics);
            part.Callback=null;
            world.Step();
            Assert.Equal(handled?2:1,world.Ticks);
            world.Restore();
            Assert.Equal(MachineWorldPhase.Idle,world.Phase);
            Assert.Equal(construction,Saved(world));
            world.Start();world.Step();
            Assert.Equal(1,world.Ticks);
        }
        finally {world.Free();}
    }

    [Theory]
    [InlineData(1)]
    [InlineData(MachineWorld.Substeps)]
    public void FailedObservationRestoresPhysicsAndWorldTickOutputs(int failAtSubstep)
    {
        var world=World();
        try
        {
            var part=new CallbackBall {Definition=world.Registry.Definitions[BallCatalogId]};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=BallCatalogId,
                Position=[0,6,0],InitialVelocity=[1,0,0]});
            world.AttachPart(part);
            var construction=Saved(world);
            world.Start();
            world.Events.Add(new(MachineEventKind.Activated,part.Uid),-1);
            world.SetOpticalPaths([new(Vector3.Zero,Vector3.Right,Vector3.One,part.OpticalIdentity)]);
            var before=world.Physics.Capture();
            var body=world.PhysicsAssembly.Body(new(part,MachinePart.RootBody));
            var bodyBefore=body.Snapshot();
            var transformBefore=part.Transform;
            var events=world.Events.ToArray();
            var paths=world.OpticalPaths.ToArray();
            var impacts=world.TickImpacts.ToArray();
            var result=world.LastPhysicsStep;
            var observed=0;
            part.Callback=(current,stage)=>
            {
                if(stage!=CallbackStage.Observation)return;
                observed++;
                current.Events.TryAdd(new(MachineEventKind.Powered,part.Uid),current.Ticks);
                current.SetOpticalPaths([]);
                if(observed!=failAtSubstep)return;
                Assert.NotEqual(bodyBefore,body.Snapshot());
                current.Running=false;
                throw new InvalidOperationException();
            };
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(failAtSubstep,observed);
            Assert.Equal(before.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(transformBefore,part.Transform);
            Assert.Equal(before.Time,world.Physics.Time);
            Assert.Equal(before.StepIndex,world.Physics.StepIndex);
            Assert.Equal(events,world.Events.ToArray());
            Assert.Equal(paths,world.OpticalPaths.ToArray());
            Assert.Equal(impacts,world.TickImpacts.ToArray());
            Assert.Equal(result,world.LastPhysicsStep);
            Assert.Equal(0,world.Ticks);Assert.True(world.Running);Assert.False(world.Won);
            Assert.Equal(SimulationTransactionPhase.Idle,world.Physics.TransactionPhase);
            part.Callback=null;
            world.Step();
            Assert.Equal(1,world.Ticks);
            Assert.Equal((ulong)MachineWorld.Substeps,world.Physics.StepIndex);
            world.Restore();
            Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }

    [Fact]
    public void CompletionCallbackRunsAfterCommitAndCanLoadAnotherMachine()
    {
        var world=World();
        try
        {
            var id=FixtureParts.Id(FixturePartId.First);
            world.LoadMachine(new()
            {
                Parts=[new(){Id=id,Kind=BallCatalogId,Position=[0,6,0]}],
                Goals=[new(){Type=GoalKind.Activated,Target=id}]
            });
            world.Start();
            world.Events.Add(new(MachineEventKind.Activated,id),0);
            var notifications=0;
            world.Solved+=()=>
            {
                notifications++;
                Assert.Equal(MachineWorldPhase.Idle,world.Phase);
                Assert.Equal(PhysicsWorldPhase.Idle,world.Physics.Phase);
                Assert.True(world.Won);Assert.False(world.Running);Assert.Equal(1,world.Ticks);
                world.LoadMachine(new());
            };
            world.Step();
            Assert.Equal(1,notifications);
            Assert.Empty(world.Parts);Assert.Equal(0,world.Ticks);
            Assert.False(world.Won);Assert.False(world.Running);
            world.Step();Assert.Equal(1,notifications);
        }
        finally {world.Free();}
    }
}
