using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SolarAnimationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Solar=new("solar_panel"),Load=new("powered_gate");
    private partial class ControlledSolar : SolarPanelPart
    {
        public float Requested;
        public int Visits,FailAt;
        public override void ReceiveLight(float irradiance)=>base.ReceiveLight(Requested);
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected solar tick failure.");}
    }
    public static IEnumerable<object[]> Rows()
    {
        for(var lamp=0;lamp<4;lamp++)
        foreach(var substep in new[]{1,4})yield return [lamp,substep];
    }
    private static Color Colour(SceneColourAnimation lamp)=>((StandardMaterial3D)lamp.Target.MaterialOverride).AlbedoColor;
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    [Theory]
    [MemberData(nameof(Rows))]
    public void EachMeterThresholdUsesCommittedReadThroughFailurePauseHideAndReset(int index,int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var panel=new ControlledSolar {Definition=world.Registry.Definitions[Solar.Value]};
            panel.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Solar.Value,Position=[-4,5,0]});world.AttachPart(panel);
            var load=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Load.Value,Position=[4,5,0]});
            Assert.True(world.Connect(panel,SocketId.Supply,load,SocketId.PowerIn,ConnectionDomain.Electrical));
            var saved=Saved(world);var pose=panel.Transform;var lamps=panel.ColourAnimations.ToArray();Assert.Equal(4,lamps.Length);
            var threshold=(float)lamps[index].Feedback.ScalarThreshold;
            panel.Requested=MathF.BitDecrement(threshold);world.Start();world.Step();
            var key=new ScalarReadKey(world.PhysicsAssembly.QueryOwnerId(new(panel,MachinePart.RootBody)),SolarPanelPart.IrradianceOutput);
            using(var read=world.ReadCommittedPoses())
            {
                Assert.Equal(1,read.ScalarCount);Assert.Equal(ScalarUnit.GameIrradiance,read.ReadScalar(PoseSample.Current,key).Unit);
                Assert.Equal((double)panel.Requested,read.ReadScalar(PoseSample.Current,key).Value);
            }
            world.PresentFrame(.1,1);
            for(var i=0;i<4;i++)Assert.Equal(i<index?lamps[i].To:lamps[i].From,Colour(lamps[i]));
            panel.Requested=threshold;world.Step();
            Assert.Equal(threshold>=SolarPanelPart.Threshold,load.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(lamps[index].From,Colour(lamps[index]));
            var physical=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            world.Running=false;world.PresentFrame(.05,1);
            Assert.Equal(lamps[index].From.Lerp(lamps[index].To,.5f),Colour(lamps[index]));
            panel.Requested=0;panel.ReceiveLight(0); // Deliberate unpublished mutation cannot change the meter.
            world.PresentFrame(.05,1);Assert.Equal(lamps[index].To,Colour(lamps[index]));
            Assert.Equal(ticks,world.Ticks);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            panel.Requested=threshold;panel.ReceiveLight(threshold);
            panel.Requested=0;panel.FailAt=panel.Visits+substep;world.Running=true;
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(threshold,panel.Irradiance);
            using(var read=world.ReadCommittedPoses())Assert.Equal((double)threshold,read.ReadScalar(PoseSample.Current,key).Value);
            world.PresentFrame(0,1);Assert.Equal(lamps[index].To,Colour(lamps[index]));
            panel.FailAt=0;panel.Visible=false;world.Step();world.PresentFrame(.1,1);
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(lamps[index].To,Colour(lamps[index]));
            panel.Visible=true;world.PresentFrame(0,1);Assert.All(lamps,lamp=>Assert.Equal(lamp.From,Colour(lamp)));
            var old=world.ReadCommittedPoses();world.Restore();
            Assert.Throws<InvalidOperationException>(()=>old.ReadScalar(PoseSample.Current,key));
            Assert.Equal(saved,Saved(world));
            var restored=(SolarPanelPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(pose,restored.Transform);Assert.Equal(0,restored.Irradiance);
            Assert.All(restored.ColourAnimations,lamp=>Assert.Equal(lamp.From,Colour(lamp)));
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.Step();
            using(var read=world.ReadCommittedPoses())Assert.Equal(0,read.ReadScalar(PoseSample.Current,key).Value);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    public enum TopologyFault { Unit, Slot, Count }
    [Theory]
    [InlineData(TopologyFault.Unit)]
    [InlineData(TopologyFault.Slot)]
    [InlineData(TopologyFault.Count)]
    public void ChangedScalarTopologyRejectsBeforeAnimationAndValidCommitRetries(TopologyFault fault)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var panel=new ControlledSolar {Definition=world.Registry.Definitions[Solar.Value],Requested=1};
            panel.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Solar.Value,Position=[0,5,0]});world.AttachPart(panel);
            world.Start();SceneAnimationRun run;PoseReadStamp stamp;ScalarRead[] scalars;
            using(var seed=world.ReadCommittedPoses())
            {
                run=new(world.Parts,world.PhysicsAssembly,seed,new Dictionary<SceneCounterKey,SimulationCounterId>(),new Dictionary<SceneTimerKey,SimulationTimerId>(),new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
                stamp=seed.Stamp(PoseSample.Current);scalars=new ScalarRead[seed.ScalarCount];seed.CopyScalars(PoseSample.Current,scalars);
            }
            scalars=fault switch
            {
                TopologyFault.Unit=>[scalars[0] with {Unit=ScalarUnit.Dimensionless,Value=1}],
                TopologyFault.Slot=>[scalars[0] with {Key=new(scalars[0].Key.Owner,new(99)),Value=1}],
                TopologyFault.Count=>[],
                _=>throw new ArgumentOutOfRangeException(nameof(fault))
            };
            var bodies=world.PhysicsAssembly.CapturePublicationReads(world.Physics).ToArray();
            var foreign=new CommittedPoseBuffer(stamp,bodies,[],[],scalars,[],[]);
            foreign.BeginWrite(0);foreign.Stage(new(stamp.Generation,new(1),MachineWorld.Tick),bodies,[],[],scalars,[],[]);foreign.Publish();
            using(var read=foreign.Acquire())Assert.Throws<ArgumentException>(()=>run.Publish(read));
            run.Present(.1);Assert.All(panel.ColourAnimations,lamp=>Assert.Equal(lamp.From,Colour(lamp)));
            world.Step();using(var read=world.ReadCommittedPoses())run.Publish(read);run.Present(.1);
            Assert.All(panel.ColourAnimations,lamp=>Assert.Equal(lamp.To,Colour(lamp)));run.Remove();
        }
        finally{world.Free();}
    }

}
