using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class OscillatorObservationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Clock=new("clock"),Battery=new("battery");
    private static readonly ScalarObservationSlot CellOutput=new(1);
    public enum DeclarationFault { None, ForeignOwner, MissingSlot, UndefinedQuantity, DuplicateOscillator, DuplicateCell }
    private partial class Meter : ClockPart
    {
        public DeclarationFault Fault;
        public MachinePart? Foreign;
        public bool RejectCapture;
        public readonly SimulationState<double> Cell=new(.375);
        public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[Cell];
        public override IReadOnlyList<SceneScalarObservation> ScalarObservations=>[new(CellOutput,ScalarUnit.Dimensionless,new(Cell))];
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(RejectCapture)Cell.Value=double.NaN;}
        public override IReadOnlyList<SceneOscillatorObservation> OscillatorObservations=>Fault switch
        {
            DeclarationFault.None=>base.OscillatorObservations,
            DeclarationFault.ForeignOwner=>[new(ProgressOutput,new(Foreign!,PulseSchedule),SimulationOscillatorQuantity.ProgressFraction)],
            DeclarationFault.MissingSlot=>[new(ProgressOutput,new(this,new OscillatorSlot()),SimulationOscillatorQuantity.ProgressFraction)],
            DeclarationFault.UndefinedQuantity=>[new(ProgressOutput,new(this,PulseSchedule),(SimulationOscillatorQuantity)999)],
            DeclarationFault.DuplicateOscillator=>[new(ProgressOutput,new(this,PulseSchedule),SimulationOscillatorQuantity.ProgressFraction),
                new(ProgressOutput,new(this,PulseSchedule),SimulationOscillatorQuantity.ProgressFraction)],
            DeclarationFault.DuplicateCell=>[new(CellOutput,new(this,PulseSchedule),SimulationOscillatorQuantity.ProgressFraction)],
            _=>throw new ArgumentOutOfRangeException(nameof(Fault))
        };
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static ScalarReadKey Key(MachineWorld world,MachinePart part)=>
        new(world.PhysicsAssembly.QueryOwnerId(new(part,MachinePart.RootBody)),ClockPart.ProgressOutput);
    private static double Read(MachineWorld world,ScalarReadKey key)
    {
        using var lease=world.ReadCommittedPoses();
        var read=lease.ReadScalar(PoseSample.Current,key);
        Assert.Equal(ScalarUnit.Dimensionless,read.Unit);return read.Value;
    }
    private static Meter Attach(MachineWorld world,DeclarationFault fault=DeclarationFault.None)
    {
        var meter=new Meter {Definition=world.Registry.Definitions[Clock.Value],Fault=fault};
        meter.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Clock.Value,Position=[0,5,0],
            Properties=new(){[PartParameterName.Of(ClockParameter.IntervalSeconds)]=.1f}});
        world.AttachPart(meter);return meter;
    }
    [Theory]
    [InlineData(DeclarationFault.ForeignOwner)]
    [InlineData(DeclarationFault.MissingSlot)]
    [InlineData(DeclarationFault.UndefinedQuantity)]
    [InlineData(DeclarationFault.DuplicateOscillator)]
    [InlineData(DeclarationFault.DuplicateCell)]
    public void InvalidDeclarationRejectsBeforeRunAndSupportsValidRetry(DeclarationFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var meter=Attach(world,fault);
            meter.Foreign=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Clock.Value,Position=[4,5,0]});
            var saved=Saved(world);
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);
            Assert.Equal(saved,Saved(world));
            meter.Fault=DeclarationFault.None;world.Start();Assert.Equal(0,Read(world,Key(world,meter)));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PublicationIsAtomicAcrossOrdinaryAndPulseTicksAndRestoresSave(bool pulseBoundary)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var meter=Attach(world);
            var battery=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-4,5,0]});
            var supply=new SupplyControl(world,battery);
            Assert.True(world.Connect(supply.Output,SocketId.Supply,meter,SocketId.PowerIn,ConnectionDomain.Electrical));
            var saved=Saved(world);world.Start();var key=Key(world,meter);
            Assert.Equal(0,Read(world,key));world.Step();Assert.Equal(0,Read(world,key));Assert.Equal(0,meter.PulseCount);
            supply.SetAndSettle(SimulationLatchPhase.On);world.Step();
            if(pulseBoundary)while(world.Ticks<meter.DueTick)world.Step();
            var before=Read(world,key);var tick=world.Ticks;var revision=world.ControlRevision;
            var state=world.ReadOscillator(new(meter,ClockPart.PulseSchedule));
            meter.RejectCapture=true;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(tick,world.Ticks);Assert.Equal(revision,world.ControlRevision);
            Assert.Equal(state,world.ReadOscillator(new(meter,ClockPart.PulseSchedule)));
            Assert.Equal(before,Read(world,key));Assert.Equal(.375,meter.Cell.Value);
            meter.RejectCapture=false;world.Step();
            Assert.Equal(world.Oscillators.Progress(state.Id),Read(world,key));
            Assert.Equal(state.PulseCount+(pulseBoundary?1:0),meter.PulseCount);
            if(pulseBoundary)Assert.Equal(0,Read(world,key));else Assert.True(Read(world,key)>before);
            world.Running=false;var paused=Read(world,key);world.PresentFrame(.5,1);Assert.Equal(paused,Read(world,key));
            world.Running=true;supply.SetAndSettle(SimulationLatchPhase.Off);
            Assert.Equal(0,Read(world,key));
            supply.SetAndSettle(SimulationLatchPhase.On);Assert.Equal(0,Read(world,key));
            var oscillatorKey=new SceneOscillatorKey(meter,ClockPart.PulseSchedule);
            var producer=new SceneScalarObservations(world.Parts,world.PhysicsAssembly,world.Physics,world.Timers,
                new Dictionary<SceneTimerKey,SimulationTimerId>(),world.Oscillators,
                new Dictionary<SceneOscillatorKey,SimulationOscillatorId>{{oscillatorKey,state.Id}});
            for(var i=0;i<100;i++)producer.Capture();
            var allocated=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++)producer.Capture();
            Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-allocated);
            var old=world.ReadCommittedPoses();world.Restore();
            Assert.Throws<InvalidOperationException>(()=>old.ReadScalar(PoseSample.Current,key));
            Assert.Equal(saved,Saved(world));world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));
            world.Start();var restored=world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(0,Read(world,Key(world,restored)));world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Fact]
    public void QuantityUsesOwnedScheduleAndRejectsUnsupportedInputs()
    {
        var id=new SimulationOscillatorId(0);var oscillators=new SimulationOscillators([new(id,4)]);
        Assert.Equal(0,oscillators.ReadQuantity(id,SimulationOscillatorQuantity.ProgressFraction));
        Assert.Throws<ArgumentOutOfRangeException>(()=>oscillators.ReadQuantity(id,(SimulationOscillatorQuantity)999));
        Assert.Throws<ArgumentException>(()=>oscillators.ReadQuantity(new(99),SimulationOscillatorQuantity.ProgressFraction));
        oscillators.Advance(0,[new(id,true)]);oscillators.Advance(1,[new(id,true)]);
        Assert.Equal(.25,oscillators.ReadQuantity(id,SimulationOscillatorQuantity.ProgressFraction));
        oscillators.BeginTransaction();oscillators.Advance(2,[new(id,false)]);
        Assert.Equal(0,oscillators.ReadQuantity(id,SimulationOscillatorQuantity.ProgressFraction));
        oscillators.RollbackTransaction();Assert.Equal(.25,oscillators.ReadQuantity(id,SimulationOscillatorQuantity.ProgressFraction));
    }
}
