using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BooleanObservationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Laser=new("laser"),Battery=new("battery");
    private static CatalogueId Catalogue(LogicGateKind kind)=>kind switch
    {
        LogicGateKind.And=>new("optical_and"),LogicGateKind.Or=>new("optical_or"),LogicGateKind.Xor=>new("optical_xor"),
        LogicGateKind.Nor=>new("optical_nor"),LogicGateKind.Nand=>new("optical_nand"),
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private partial class Source : LaserPart
    {
        public Vector3 Direction,Power;
        public int Visits,FailAt;
        public override OpticalEmitter? OpticalSource=>new(Vector3.Zero,Direction,12,Power);
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected Boolean publication failure.");}
    }
    private static Source Attach(MachineWorld world,FixturePartId id,Vector3 at,Vector3 direction,bool powered)
    {
        var source=new Source {Definition=world.Registry.Definitions[Laser.Value],Direction=direction,Power=powered?Vector3.One:Vector3.Zero};
        source.Configure(new(){Id=FixtureParts.Id(id),Kind=Laser.Value,Position=[at.X,at.Y,at.Z]});world.AttachPart(source);return source;
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static BooleanReadKey Key(MachineWorld world,MachinePart part,BooleanObservationSlot slot)=>
        new(world.PhysicsAssembly.QueryOwnerId(new(part,MachinePart.RootBody)),slot);
    private static void Reads(MachineWorld world,OpticalLogicPart gate,bool first,bool second,bool open)
    {
        using var read=world.ReadCommittedPoses();
        Assert.Equal(first,read.ReadBoolean(PoseSample.Current,Key(world,gate,OpticalLogicPart.FirstOutput)).Value);
        Assert.Equal(second,read.ReadBoolean(PoseSample.Current,Key(world,gate,OpticalLogicPart.SecondOutput)).Value);
        Assert.Equal(open,read.ReadBoolean(PoseSample.Current,Key(world,gate,OpticalLogicPart.OpenOutput)).Value);
    }
    private static void Lamp(OpticalLogicPart gate,BooleanObservationSlot slot,double amount)
    {
        var declaration=Assert.Single(gate.FollowingColours,d=>d.Signal.Observation?.Slot==slot);
        var expected=declaration.From.Lerp(declaration.To,(float)amount);
        var actual=((StandardMaterial3D)declaration.Target.MaterialOverride).AlbedoColor;
        Assert.InRange(Math.Abs(actual.R-expected.R),0,2e-6);Assert.InRange(Math.Abs(actual.G-expected.G),0,2e-6);
        Assert.InRange(Math.Abs(actual.B-expected.B),0,2e-6);Assert.Equal(expected.A,actual.A);
    }
    private static void Controls(OpticalLogicPart gate,bool first,bool second)=>gate.ReceiveOpticalPower(
        new Dictionary<OpticalPortId,Vector3>
        {
            [OpticalPortId.First]=first?Vector3.One:Vector3.Zero,
            [OpticalPortId.Second]=second?Vector3.One:Vector3.Zero
        });
    public static IEnumerable<object[]> Cases()
    {
        foreach(var row in LogicGateTests.TruthRows())yield return row;
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryTruthRowPublishesTypedControlsWithoutInventingCarrierEnergy(LogicGateKind kind,bool first,bool second,bool expected)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var gate=(OpticalLogicPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Catalogue(kind).Value,Position=[0,6,0]});
            var a=Attach(world,FixturePartId.First,new(-4,6,0),Vector3.Right,first);
            var b=Attach(world,FixturePartId.Second,new(0,10,0),Vector3.Down,second);
            var saved=Saved(world);world.Start();Reads(world,gate,false,false,LogicGate.Evaluate(kind,false,false));
            world.Step();world.Step();Reads(world,gate,first,second,expected);
            Assert.Equal(Vector3.Zero,gate.OutputPower);Assert.False(gate.Active);
            Lamp(gate,OpticalLogicPart.FirstOutput,0);Lamp(gate,OpticalLogicPart.SecondOutput,0);
            var initialPhysics=world.Physics.Capture().BodyStates.ToArray();var tick=world.Ticks;
            world.Running=false;world.PresentFrame(.1,1);
            Lamp(gate,OpticalLogicPart.FirstOutput,first?1-Math.Exp(-1.2):0);
            Lamp(gate,OpticalLogicPart.SecondOutput,second?1-Math.Exp(-1.2):0);
            Controls(gate,!first,!second);world.PresentFrame(.1,1);
            Lamp(gate,OpticalLogicPart.FirstOutput,first?1-Math.Exp(-2.4):0);
            Lamp(gate,OpticalLogicPart.SecondOutput,second?1-Math.Exp(-2.4):0);
            Assert.Equal(initialPhysics,world.Physics.Capture().BodyStates.ToArray());Assert.Equal(tick,world.Ticks);
            Controls(gate,first,second);world.Running=true;
            var revision=world.ControlRevision;var physical=world.Physics.Capture().BodyStates.ToArray();
            a.Power=first?Vector3.Zero:Vector3.One;b.Power=second?Vector3.Zero:Vector3.One;
            a.FailAt=a.Visits+4;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(revision,world.ControlRevision);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            Reads(world,gate,first,second,expected);
            world.PresentFrame(.1,1);
            var firstBefore=first?1-Math.Exp(-3.6):0;var secondBefore=second?1-Math.Exp(-3.6):0;
            Lamp(gate,OpticalLogicPart.FirstOutput,firstBefore);Lamp(gate,OpticalLogicPart.SecondOutput,secondBefore);
            a.FailAt=0;world.Step();Reads(world,gate,!first,!second,expected);
            world.Step();Reads(world,gate,!first,!second,LogicGate.Evaluate(kind,!first,!second));
            world.Running=false;gate.Visible=false;world.PresentFrame(.1,1);
            Lamp(gate,OpticalLogicPart.FirstOutput,firstBefore);Lamp(gate,OpticalLogicPart.SecondOutput,secondBefore);
            gate.Visible=true;world.PresentFrame(0,1);
            Lamp(gate,OpticalLogicPart.FirstOutput,(first?0:1)+(firstBefore-(first?0:1))*Math.Exp(-1.2));
            Lamp(gate,OpticalLogicPart.SecondOutput,(second?0:1)+(secondBefore-(second?0:1))*Math.Exp(-1.2));
            Reads(world,gate,!first,!second,gate.IsOpen);
            var producer=new SceneBooleanObservations(world.Parts,world.PhysicsAssembly);
            for(var i=0;i<100;i++)producer.Capture();var before=GC.GetAllocatedBytesForCurrentThread();
            for(var i=0;i<1000;i++)producer.Capture();Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread()-before);
            var old=world.ReadCommittedPoses();var key=Key(world,gate,OpticalLogicPart.FirstOutput);
            world.Restore();Assert.Throws<InvalidOperationException>(()=>old.ReadBoolean(PoseSample.Current,key));Assert.Equal(saved,Saved(world));
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();
            var restored=(OpticalLogicPart)world.FindPart(FixtureParts.Id(FixturePartId.Third))!;
            Reads(world,restored,false,false,LogicGate.Evaluate(kind,false,false));
            Lamp(restored,OpticalLogicPart.FirstOutput,0);Lamp(restored,OpticalLogicPart.SecondOutput,0);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    public enum DeclarationFault { None, Foreign, Missing, Duplicate }
    private partial class Probe : BatteryPart
    {
        public readonly SimulationState<bool> Owned=new(true),Foreign=new(false);
        public DeclarationFault Fault;
        public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[Owned];
        public override IReadOnlyList<SceneBooleanObservation> BooleanObservations=>Fault switch
        {
            DeclarationFault.None=>[new(new(0),new(Owned))],
            DeclarationFault.Foreign=>[new(new(0),new(Foreign))],
            DeclarationFault.Missing=>[new(new(0),default)],
            DeclarationFault.Duplicate=>[new(new(0),new(Owned)),new(new(0),new(Owned))],
            _=>throw new ArgumentOutOfRangeException(nameof(Fault))
        };
    }
    [Theory]
    [InlineData(DeclarationFault.Foreign)]
    [InlineData(DeclarationFault.Missing)]
    [InlineData(DeclarationFault.Duplicate)]
    public void InvalidOwnershipRejectsAndCorrectedDeclarationRetries(DeclarationFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var part=new Probe {Definition=world.Registry.Definitions[Battery.Value],Fault=fault};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Battery.Value});world.AttachPart(part);
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);
            part.Fault=DeclarationFault.None;world.Start();
            using var read=world.ReadCommittedPoses();Assert.True(read.ReadBoolean(PoseSample.Current,Key(world,part,new(0))).Value);
        }
        finally{world.Free();}
    }
    [Fact]
    public void TypedSourceBoundariesRejectUnknownQuantitiesAndMissingOwners()
    {
        var control=new OpticalLogicControl(LogicGateKind.And);
        Assert.Throws<ArgumentOutOfRangeException>(()=>control.Read((OpticalControlQuantity)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new BooleanObservationSource(control,(OpticalControlQuantity)999));
        Assert.Throws<ArgumentNullException>(()=>new BooleanObservationSource((SimulationState<bool>)null!));
        Assert.Throws<ArgumentNullException>(()=>new BooleanObservationSource(null!,OpticalControlQuantity.First));
        var source=new BooleanObservationSource(control,OpticalControlQuantity.First);Assert.False(source.Read());
        control.BeginTransaction();control.Sample(1,0);Assert.True(source.Read());control.RollbackTransaction();Assert.False(source.Read());
    }
}
