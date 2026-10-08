using Godot;
using CuriousContraptions.Bridge;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class EnumObservationTests(NativeSceneFixture godot)
{
    public enum Mode { Idle, Ready }
    public enum Fault { None, ForeignCell, Duplicate, UndefinedSeed, UndefinedTick }
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery");
    private partial class Probe:BatteryPart
    {
        public readonly SimulationState<Mode> State=new(Mode.Idle),Foreign=new(Mode.Idle);
        public readonly SimulationState<DayOfWeek> Other=new(DayOfWeek.Monday);
        public Fault Fault;
        public int Visits,FailAt;
        public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[State,Other];
        public override IReadOnlyList<SceneEnumObservation> EnumObservations=>Fault switch
        {
            Fault.ForeignCell=>[new SceneEnumObservation<Mode>(new(0),Foreign)],
            Fault.Duplicate=>[new SceneEnumObservation<Mode>(new(0),State),new SceneEnumObservation<Mode>(new(0),State)],
            _=>[new SceneEnumObservation<Mode>(new(0),State),new SceneEnumObservation<DayOfWeek>(new(0),Other)]
        };
        public override void BeforeNetworks(MachineWorld world)
        {State.Value=Mode.Ready;Other.Value=Fault==Fault.UndefinedTick?(DayOfWeek)999:DayOfWeek.Friday;}
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected enum publication failure.");}
    }
    private static Probe Add(MachineWorld world)
    {
        var p=new Probe{Definition=world.Registry.Definitions[Battery.Value]};
        p.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value,Position=[0,5,0]});world.AttachPart(p);return p;
    }
    private static string Saved(MachineWorld world)=>System.Text.Json.JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void MultipleEnumChannelsRollbackPublishAndInvalidateWithTheWholeWorld(int substep)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=Add(world);var saved=Saved(world);world.Start();
            var owner=world.PhysicsAssembly.QueryOwnerId(new(part,MachinePart.RootBody));
            var key=new EnumReadKey<Mode>(owner,new(0));var other=new EnumReadKey<DayOfWeek>(owner,new(0));
            part.FailAt=substep;var physical=world.Physics.Capture().BodyStates.ToArray();
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(0,world.Ticks);Assert.Equal(Mode.Idle,part.State.Value);Assert.Equal(DayOfWeek.Monday,part.Other.Value);
            Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            using(var seed=world.ReadCommittedPoses())
            {Assert.Equal(Mode.Idle,seed.ReadEnum(PoseSample.Current,key).Value);Assert.Equal(DayOfWeek.Monday,seed.ReadEnum(PoseSample.Current,other).Value);}
            part.FailAt=0;world.Step();
            using(var committed=world.ReadCommittedPoses())
            {
                Assert.Equal(Mode.Ready,committed.ReadEnum(PoseSample.Current,key).Value);
                Assert.Equal(Mode.Idle,committed.ReadEnum(PoseSample.Previous,key).Value);
                Assert.Equal(DayOfWeek.Friday,committed.ReadEnum(PoseSample.Current,other).Value);
            }
            part.State.Value=Mode.Idle;world.PresentFrame(.1,1);
            var old=world.ReadCommittedPoses();Assert.Equal(Mode.Ready,old.ReadEnum(PoseSample.Current,key).Value);
            world.Restore();Assert.Throws<InvalidOperationException>(()=>old.ReadEnum(PoseSample.Current,key));
            Assert.Equal(saved,Saved(world));world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(Fault.ForeignCell)]
    [InlineData(Fault.Duplicate)]
    [InlineData(Fault.UndefinedSeed)]
    public void InvalidEnumSourceRejectsBeforeRunAndCanBeCorrected(Fault fault)
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=Add(world);part.Fault=fault;if(fault==Fault.UndefinedSeed)part.State.Value=(Mode)999;
            var saved=Saved(world);Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);Assert.Equal(saved,Saved(world));
            part.Fault=Fault.None;part.State.Value=Mode.Idle;world.Start();world.Step();Assert.Equal(Mode.Ready,part.State.Value);
        }
        finally{world.Free();}
    }
    [Fact]
    public void UndefinedLateCaptureRollsBackAllChannelsAndAllowsRetry()
    {
        var world=new MachineWorld{Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=Add(world);world.Start();part.Fault=Fault.UndefinedTick;
            Assert.Throws<ArgumentException>(world.Step);Assert.Equal(0,world.Ticks);
            Assert.Equal(Mode.Idle,part.State.Value);Assert.Equal(DayOfWeek.Monday,part.Other.Value);
            using(var read=world.ReadCommittedPoses())
                Assert.Equal(Mode.Idle,read.ReadEnum(PoseSample.Current,new EnumReadKey<Mode>(
                    world.PhysicsAssembly.QueryOwnerId(new(part,MachinePart.RootBody)),new(0))).Value);
            part.Fault=Fault.None;world.Step();Assert.Equal(1,world.Ticks);
        }
        finally{world.Free();}
    }
}
