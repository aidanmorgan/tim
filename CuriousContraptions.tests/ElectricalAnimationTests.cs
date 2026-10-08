using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ElectricalAnimationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Battery=new("battery");
    private static CatalogueId Gate(LogicGateKind operation)=>operation switch
    {
        LogicGateKind.And=>new("both_gate"),LogicGateKind.Or=>new("electrical_or"),
        LogicGateKind.Xor=>new("electrical_xor"),LogicGateKind.Nor=>new("electrical_nor"),
        LogicGateKind.Nand=>new("electrical_nand"),_=>throw new ArgumentOutOfRangeException(nameof(operation))
    };
    private partial class Supply : BatteryPart
    {
        private readonly SimulationState<bool> _available=new(false);
        public bool Available {get=>_available.Value;set=>_available.Value=value;}
        public override IReadOnlyList<SimulationTransactionParticipant> RuntimeState=>[_available];
        public int Visits,FailAt;
        public override IEnumerable<ElectricalSourceDeclaration> ElectricalSources=>
            [new(SocketId.Supply,ElectricalSourceSignal.When(ElectricalContactSignal.BooleanState(_available)))];
        public override void ObservePhysics(MachineWorld world,float delta)
        { if(++Visits==FailAt)throw new InvalidOperationException("Injected electrical feedback failure."); }
    }
    private static Color Colour(SceneColourAnimation lamp)=>((StandardMaterial3D)lamp.Target.MaterialOverride).AlbedoColor;
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static Supply AddSupply(MachineWorld world,FixturePartId id,ElectricalLogicPart gate,SocketId input,bool available,float z)
    {
        var source=new Supply {Definition=world.Registry.Definitions[Battery.Value],Available=available};
        source.Configure(new(){Id=FixtureParts.Id(id),Kind=Battery.Value,Position=[-4,5,z]});world.AttachPart(source);
        Assert.True(world.Connect(source,SocketId.Supply,gate,input,ConnectionDomain.Electrical));return source;
    }
    public static IEnumerable<object[]> Rows()
    {
        foreach(var row in LogicGateTests.TruthRows())
            foreach(var supply in new[]{false,true})yield return [..row,supply];
    }
    [Theory]
    [MemberData(nameof(Rows))]
    public void EveryOperationUsesCommittedInputsAndOutputAcrossFailurePauseHideResetAndSave(
        LogicGateKind operation,bool first,bool second,bool truth,bool supplied)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var gate=(ElectricalLogicPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),
                Kind=Gate(operation).Value,Position=[0,5,0]});
            var source=AddSupply(world,FixturePartId.Second,gate,SocketId.FirstIn,first,-4);
            AddSupply(world,FixturePartId.Third,gate,SocketId.SecondIn,second,0);
            AddSupply(world,FixturePartId.Fourth,gate,SocketId.PowerIn,supplied,4);
            var saved=Saved(world);var pose=gate.Transform;
            var lamps=gate.ColourAnimations.ToArray();Assert.Equal(3,lamps.Length);
            var firstLamp=Assert.Single(lamps,lamp=>lamp.Feedback.Input==SocketId.FirstIn);
            bool Expected(SceneColourAnimation lamp)=>lamp.Feedback.Kind switch
            {
                SceneAnimationFeedback.OwnerActive=>truth&&supplied,
                SceneAnimationFeedback.ElectricalInput=>lamp.Feedback.Input switch
                {
                    SocketId.FirstIn=>first,SocketId.SecondIn=>second,
                    _=>throw new InvalidOperationException("Unexpected logic input lamp.")
                },
                _=>throw new InvalidOperationException("Unexpected logic feedback.")
            };
            world.Start();
            var owner=world.PhysicsAssembly.QueryOwnerId(new(gate,MachinePart.RootBody));
            var key=new ElectricalInputKey(owner,SocketId.FirstIn);
            using(var seed=world.ReadCommittedPoses())
            {
                Assert.Equal(3,seed.ElectricalInputCount);
                var copy=new ElectricalInputRead[3];seed.CopyElectricalInputs(PoseSample.Current,copy);
                Assert.All(copy,value=>Assert.Equal(ElectricalAvailability.Unavailable,value.Availability));
            }
            world.Step();
            Assert.Equal(truth&&supplied,gate.Active);
            using(var read=world.ReadCommittedPoses())
            {
                Assert.Equal(first?ElectricalAvailability.Available:ElectricalAvailability.Unavailable,read.ReadElectricalInput(PoseSample.Current,key).Availability);
                Assert.Equal(second?ElectricalAvailability.Available:ElectricalAvailability.Unavailable,read.ReadElectricalInput(PoseSample.Current,new(owner,SocketId.SecondIn)).Availability);
                Assert.Equal(supplied?ElectricalAvailability.Available:ElectricalAvailability.Unavailable,read.ReadElectricalInput(PoseSample.Current,new(owner,SocketId.PowerIn)).Availability);
            }
            var physics=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            gate.ClearElectricalPower();gate.Active=!(truth&&supplied);world.Running=false;
            world.PresentFrame(.05,1);
            foreach(var lamp in lamps)Assert.Equal(Expected(lamp)?lamp.From.Lerp(lamp.To,.5f):lamp.From,Colour(lamp));
            Assert.Equal(physics,world.Physics.Capture().BodyStates.ToArray());Assert.Equal(ticks,world.Ticks);
            world.PresentFrame(.05,1);
            foreach(var lamp in lamps)Assert.Equal(Expected(lamp)?lamp.To:lamp.From,Colour(lamp));
            world.Running=true;world.Step();
            ticks=world.Ticks;var revision=world.ControlRevision;
            source.Available=!first;source.FailAt=source.Visits+(second?4:1);
            Assert.Throws<InvalidOperationException>(()=>world.Step());
            Assert.Equal(ticks,world.Ticks);Assert.Equal(revision,world.ControlRevision);
            using(var read=world.ReadCommittedPoses())
                Assert.Equal(first?ElectricalAvailability.Available:ElectricalAvailability.Unavailable,read.ReadElectricalInput(PoseSample.Current,key).Availability);
            world.PresentFrame(.1,1);
            foreach(var lamp in lamps)Assert.Equal(Expected(lamp)?lamp.To:lamp.From,Colour(lamp));
            source.FailAt=0;gate.Visible=false;world.Step();world.PresentFrame(.1,1);
            Assert.Equal(first?firstLamp.To:firstLamp.From,Colour(firstLamp));
            gate.Visible=true;world.PresentFrame(0,1);
            Assert.Equal(first?firstLamp.From:firstLamp.To,Colour(firstLamp));
            var old=world.ReadCommittedPoses();world.Restore();
            Assert.Throws<InvalidOperationException>(()=>old.ReadElectricalInput(PoseSample.Current,key));
            Assert.Equal(saved,Saved(world));
            gate=(ElectricalLogicPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(pose,gate.Transform);Assert.Equal(operation,gate.Operation);
            foreach(var lamp in gate.ColourAnimations)Assert.Equal(lamp.From,Colour(lamp));
            var construction=world.Snapshot();world.LoadMachine(construction);Assert.Equal(saved,Saved(world));
            world.Start();using(var restored=world.ReadCommittedPoses())
            {
                var copy=new ElectricalInputRead[restored.ElectricalInputCount];restored.CopyElectricalInputs(PoseSample.Current,copy);
                Assert.All(copy,value=>Assert.Equal(ElectricalAvailability.Unavailable,value.Availability));
            }
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }

    [Fact]
    public void ForeignElectricalTopologyRejectsBeforeAnyAnimationControlChanges()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var gate=(ElectricalLogicPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Gate(LogicGateKind.And).Value});
            world.Start();SceneAnimationRun run;ElectricalInputRead[] electrical;PoseReadStamp stamp;
            using(var seed=world.ReadCommittedPoses())
            {
                run=new(world.Parts,world.PhysicsAssembly,seed,new Dictionary<SceneCounterKey,SimulationCounterId>(),new Dictionary<SceneTimerKey,SimulationTimerId>(),new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
                electrical=new ElectricalInputRead[seed.ElectricalInputCount];seed.CopyElectricalInputs(PoseSample.Current,electrical);
                stamp=seed.Stamp(PoseSample.Current);
            }
            var owner=world.PhysicsAssembly.QueryOwnerId(new(gate,MachinePart.RootBody));
            for(var i=0;i<electrical.Length;i++)
                electrical[i]=electrical[i].Key.Port==SocketId.SecondIn
                    ?new(new(owner,SocketId.ExtendIn),ElectricalAvailability.Available)
                    :electrical[i] with {Availability=ElectricalAvailability.Available};
            var bodies=world.PhysicsAssembly.CapturePublicationReads(world.Physics).ToArray();
            var foreign=new CommittedPoseBuffer(stamp,bodies,[],electrical,[],[],[]);
            foreign.BeginWrite(0);foreign.Stage(new(stamp.Generation,new(1),MachineWorld.Tick),bodies,[],electrical,[],[],[]);foreign.Publish();
            using(var read=foreign.Acquire())Assert.Throws<ArgumentException>(()=>run.Publish(read));
            run.Present(.2);foreach(var lamp in gate.ColourAnimations)Assert.Equal(lamp.From,Colour(lamp));
            world.Step();using(var read=world.ReadCommittedPoses())run.Publish(read);run.Remove();
        }
        finally{world.Free();}
    }

    private partial class InvalidInputGate : ElectricalLogicPart
    {
        public override IReadOnlyList<SceneColourAnimation> ColourAnimations=>
            base.ColourAnimations.Select(lamp=>lamp with {Feedback=SceneAnimationSignal.InputAvailable(SocketId.Supply)}).ToArray();
    }
    [Fact]
    public void UnknownEnumAndUndeclaredInputAreRejectedAtBinding()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>SceneAnimationSignal.InputAvailable((SocketId)999));
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var part=new InvalidInputGate {Definition=world.Registry.Definitions[Gate(LogicGateKind.And).Value]};
            part.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Gate(LogicGateKind.And).Value});world.AttachPart(part);
            Assert.Throws<ArgumentException>(()=>world.Start());Assert.False(world.Running);
        }
        finally{world.Free();}
    }
}
