using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class TimerObservationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Hold=new("hold_timer"),Battery=new("battery"),Load=new("powered_gate");
    private static readonly ScalarObservationSlot ProgressOutput=new(1),CellOutput=new(2);
    public enum DeclarationFault { None, ForeignOwner, MissingSlot, UndefinedQuantity, DuplicateTimer, DuplicateCell }
    private partial class Meter : HoldTimerPart
    {
        public DeclarationFault Fault;
        public MachinePart? Foreign;
        public bool RejectCapture;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(RejectCapture)Cell.Value=double.NaN;}
        public readonly SimulationState<double> Cell=new(.375);
        public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[Cell];
        public override IReadOnlyList<SceneScalarObservation> ScalarObservations=>[new(CellOutput,ScalarUnit.Dimensionless,new(Cell))];
        public override IReadOnlyList<SceneTimerObservation> TimerObservations=>Fault switch
        {
            DeclarationFault.None=>[new(RemainingOutput,new(this,ContactWindow),SimulationTimerQuantity.RemainingFraction),
                new(ProgressOutput,new(this,ContactWindow),SimulationTimerQuantity.ProgressFraction)],
            DeclarationFault.ForeignOwner=>[new(RemainingOutput,new(Foreign!,ContactWindow),SimulationTimerQuantity.RemainingFraction)],
            DeclarationFault.MissingSlot=>[new(RemainingOutput,new(this,new TimerSlot()),SimulationTimerQuantity.RemainingFraction)],
            DeclarationFault.UndefinedQuantity=>[new(RemainingOutput,new(this,ContactWindow),(SimulationTimerQuantity)999)],
            DeclarationFault.DuplicateTimer=>[new(RemainingOutput,new(this,ContactWindow),SimulationTimerQuantity.RemainingFraction),
                new(RemainingOutput,new(this,ContactWindow),SimulationTimerQuantity.ProgressFraction)],
            DeclarationFault.DuplicateCell=>[new(CellOutput,new(this,ContactWindow),SimulationTimerQuantity.RemainingFraction)],
            _=>throw new ArgumentOutOfRangeException(nameof(Fault))
        };
    }
    private partial class Probe : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected timer-observation failure.");}
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static ScalarReadKey Key(MachineWorld world,MachinePart part,ScalarObservationSlot slot)=>
        new(world.PhysicsAssembly.QueryOwnerId(new(part,MachinePart.RootBody)),slot);
    private static double Read(MachineWorld world,ScalarReadKey key)
    {
        using var lease=world.ReadCommittedPoses();
        var value=lease.ReadScalar(PoseSample.Current,key);
        Assert.Equal(ScalarUnit.Dimensionless,value.Unit);return value.Value;
    }
    private static Meter Attach(MachineWorld world,DeclarationFault fault=DeclarationFault.None)
    {
        var meter=new Meter {Definition=world.Registry.Definitions[Hold.Value],Fault=fault};
        meter.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Hold.Value,Position=[0,5,0],
            Properties=new(){[PartParameterName.Of(HoldTimerParameter.HoldSeconds)]=.1f}});
        world.AttachPart(meter);return meter;
    }
    [Theory]
    [InlineData(DeclarationFault.ForeignOwner)]
    [InlineData(DeclarationFault.MissingSlot)]
    [InlineData(DeclarationFault.UndefinedQuantity)]
    [InlineData(DeclarationFault.DuplicateTimer)]
    [InlineData(DeclarationFault.DuplicateCell)]
    public void InvalidBindingsRejectBeforeRunAndValidRetrySucceeds(DeclarationFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var meter=Attach(world,fault);
            meter.Foreign=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Hold.Value,Position=[4,5,0]});
            var saved=Saved(world);
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);Assert.Equal(saved,Saved(world));
            meter.Fault=DeclarationFault.None;world.Start();
            Assert.Equal(0,Read(world,Key(world,meter,HoldTimerPart.RemainingOutput)));
            Assert.Equal(.375,Read(world,Key(world,meter,CellOutput)));
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(false,1)]
    [InlineData(false,4)]
    [InlineData(true,1)]
    [InlineData(true,4)]
    public void TimerPublicationTracksCommittedWindowAndRetainsRollbackResetSave(bool powered,int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var meter=Attach(world);
            var source=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            source.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-4,5,0],
                Properties=new(){[PartParameterName.Of(BatteryParameter.Enabled)]=powered?1:0}});world.AttachPart(source);
            var load=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Load.Value,Position=[4,5,0]});
            Assert.True(world.Connect(source,SocketId.Supply,meter,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(meter,SocketId.Supply,load,SocketId.PowerIn,ConnectionDomain.Electrical));
            var saved=Saved(world);world.Start();
            var remaining=Key(world,meter,HoldTimerPart.RemainingOutput);var progress=Key(world,meter,ProgressOutput);
            Assert.Equal(0,Read(world,remaining));Assert.Equal(0,Read(world,progress));
            world.Activate(meter);var due=meter.DueTick;
            Assert.Equal(0,Read(world,remaining)); // An uncommitted trigger cannot change the publication.
            world.Step();Assert.Equal(1,Read(world,remaining));Assert.Equal(0,Read(world,progress));
            Assert.Equal(powered,load.HasElectricalPower(SocketId.PowerIn));
            world.Step();var committed=Read(world,remaining);Assert.InRange(committed,0,1);
            Assert.Equal(1-Read(world,progress),committed);
            var state=world.Timers.Capture();var ticks=world.Ticks;var revision=world.ControlRevision;
            var bodies=world.Physics.Capture().BodyStates.ToArray();
            source.FailAt=source.Visits+substep;
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(ticks,world.Ticks);Assert.Equal(revision,world.ControlRevision);
            Assert.Equal(state.Tick,world.Timers.Tick);Assert.Equal(due,meter.DueTick);
            Assert.Equal(bodies,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(committed,Read(world,remaining));
            source.FailAt=0;world.Step();Assert.True(Read(world,remaining)<committed);
            var beforeLate=Read(world,remaining);var lateTick=world.Timers.Tick;var lateRevision=world.ControlRevision;
            meter.RejectCapture=true;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(.375,meter.Cell.Value);Assert.Equal(lateTick,world.Timers.Tick);
            Assert.Equal(lateRevision,world.ControlRevision);Assert.Equal(beforeLate,Read(world,remaining));
            meter.RejectCapture=false;world.Step();Assert.True(Read(world,remaining)<beforeLate);
            world.Running=false;var paused=Read(world,remaining);world.PresentFrame(.5,1);
            Assert.Equal(paused,Read(world,remaining));
            world.Running=true;
            while(world.Ticks<=due)world.Step();
            Assert.Equal(SimulationTimerPhase.Ready,meter.State);
            Assert.Equal(0,Read(world,remaining));Assert.Equal(0,Read(world,progress));
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            world.Activate(meter);Assert.True(meter.DueTick>due);
            Assert.Equal(0,Read(world,remaining));world.Step();Assert.Equal(1,Read(world,remaining));
            var timerKey=new SceneTimerKey(meter,HoldTimerPart.ContactWindow);
            var producer=new SceneScalarObservations(world.Parts,world.PhysicsAssembly,world.Physics,world.Timers,
                new Dictionary<SceneTimerKey,SimulationTimerId>{{timerKey,world.ReadTimer(timerKey).Id}},world.Oscillators,new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
            for(var i=0;i<100;i++)producer.Capture();
            var before=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++)producer.Capture();
            Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
            Assert.Equal(3,producer.Capture().Length);
            var old=world.ReadCommittedPoses();world.Restore();
            Assert.Throws<InvalidOperationException>(()=>old.ReadScalar(PoseSample.Current,remaining));
            Assert.Equal(saved,Saved(world));
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();
            var restored=(HoldTimerPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(0,Read(world,Key(world,restored,HoldTimerPart.RemainingOutput)));
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(TimerCompletionPolicy.Latch,TimerBoundary.BeforeNetworks)]
    [InlineData(TimerCompletionPolicy.Latch,TimerBoundary.BeforePhysics)]
    [InlineData(TimerCompletionPolicy.Rearm,TimerBoundary.BeforeNetworks)]
    [InlineData(TimerCompletionPolicy.Rearm,TimerBoundary.BeforePhysics)]
    public void QuantityBoundariesUseTimerClockAndExplicitCompletion(TimerCompletionPolicy completion,TimerBoundary boundary)
    {
        var id=new SimulationTimerId(0);var timers=new SimulationTimers([new(id,4,completion,boundary)]);
        Assert.Equal(0,timers.ReadQuantity(id,SimulationTimerQuantity.RemainingFraction));
        Assert.Equal(0,timers.ReadQuantity(id,SimulationTimerQuantity.ProgressFraction));
        Assert.Throws<ArgumentOutOfRangeException>(()=>timers.ReadQuantity(id,(SimulationTimerQuantity)999));
        Assert.Throws<ArgumentException>(()=>timers.ReadQuantity(new(99),SimulationTimerQuantity.ProgressFraction));
        timers.Trigger(id,2);
        Assert.Equal(1,timers.ReadQuantity(id,SimulationTimerQuantity.RemainingFraction));
        timers.Advance(4,boundary);
        Assert.Equal(.5,timers.ReadQuantity(id,SimulationTimerQuantity.RemainingFraction));
        Assert.Equal(.5,timers.ReadQuantity(id,SimulationTimerQuantity.ProgressFraction));
        timers.BeginTransaction();timers.Advance(6,boundary);
        Assert.Equal(0,timers.ReadQuantity(id,SimulationTimerQuantity.RemainingFraction));
        Assert.Equal(completion==TimerCompletionPolicy.Latch?1:0,timers.ReadQuantity(id,SimulationTimerQuantity.ProgressFraction));
        timers.RollbackTransaction();Assert.Equal(.5,timers.ReadQuantity(id,SimulationTimerQuantity.RemainingFraction));
        timers.Advance(100,boundary);
        Assert.Equal(0,timers.ReadQuantity(id,SimulationTimerQuantity.RemainingFraction));
        Assert.Equal(completion==TimerCompletionPolicy.Rearm,timers.Trigger(id,100));
    }
}
