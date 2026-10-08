using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class SoundMeterAnimationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Meter=new("sound_meter"),Battery=new("battery"),Load=new("powered_gate");
    public enum LevelCase { Silent, BelowThreshold, Threshold, Full }
    private partial class ControlledMeter : SoundMeterPart
    {
        public float Requested;
        public override void ReceiveAcousticLevel(float level)=>base.ReceiveAcousticLevel(Requested);
    }
    private partial class Probe : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected meter tick failure.");}
    }
    public static IEnumerable<object[]> Rows()
    {
        foreach(var level in Enum.GetValues<LevelCase>())
        foreach(var powered in new[]{false,true})
        foreach(var substep in new[]{1,4})yield return [level,powered,substep];
    }
    private static Color Colour(SceneColourAnimation lamp)=>((StandardMaterial3D)lamp.Target.MaterialOverride).AlbedoColor;
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    [Theory]
    [MemberData(nameof(Rows))]
    public void LevelAndSuppliedLampUseCommittedStateThroughPauseFailureHideResetAndSave(LevelCase kind,bool powered,int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var meter=new ControlledMeter {Definition=world.Registry.Definitions[Meter.Value]};
            meter.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Meter.Value,Position=[0,5,0]});world.AttachPart(meter);
            var source=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            source.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-4,5,0],
                Properties=new(){[PartParameterName.Of(BatteryParameter.Enabled)]=powered?1:0}});world.AttachPart(source);
            var load=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Load.Value,Position=[4,5,0]});
            Assert.True(world.Connect(source,SocketId.Supply,meter,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(meter,SocketId.Supply,load,SocketId.PowerIn,ConnectionDomain.Electrical));
            var level=kind switch
            {
                LevelCase.Silent=>0,LevelCase.BelowThreshold=>MathF.BitDecrement(meter.Threshold),
                LevelCase.Threshold=>meter.Threshold,LevelCase.Full=>1,
                _=>throw new ArgumentOutOfRangeException(nameof(kind))
            };
            meter.Requested=level;var saved=Saved(world);var pose=meter.Transform;
            var gauge=Assert.Single(meter.ScalarRotationAnimations);var lamp=Assert.Single(meter.ColourAnimations);
            var constructionNeedle=gauge.Target.Transform;
            world.Start();world.Step();
            var key=new ScalarReadKey(world.PhysicsAssembly.QueryOwnerId(new(meter,MachinePart.RootBody)),SoundMeterPart.LevelOutput);
            var active=powered&&level>=meter.Threshold;
            Assert.Equal(active,meter.Active);Assert.Equal(active,load.HasElectricalPower(SocketId.PowerIn));
            using(var read=world.ReadCommittedPoses())Assert.Equal((double)level,read.ReadScalar(PoseSample.Current,key).Value);
            Assert.Equal(constructionNeedle,gauge.Target.Transform);Assert.Equal(lamp.From,Colour(lamp));
            var physical=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            world.Running=false;world.PresentFrame(.05,1);
            Assert.InRange(Math.Abs(gauge.Target.Rotation.Z-(1-2*level*(1-Math.Exp(-.9)))),0,2e-6);
            Assert.Equal(active?lamp.From.Lerp(lamp.To,.5f):lamp.From,Colour(lamp));
            meter.Requested=0;meter.ReceiveAcousticLevel(0);meter.Active=!active;
            world.PresentFrame(.05,1);
            var beforeAngle=1-2*level*(1-Math.Exp(-1.8));
            Assert.InRange(Math.Abs(gauge.Target.Rotation.Z-beforeAngle),0,2e-6);
            Assert.Equal(active?lamp.To:lamp.From,Colour(lamp));
            Assert.Equal(ticks,world.Ticks);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            meter.Requested=level;meter.ReceiveAcousticLevel(level);meter.Active=active;
            var next=level==1?0:1;meter.Requested=next;
            source.FailAt=source.Visits+substep;world.Running=true;
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(level,meter.Level);Assert.Equal(active,meter.Active);
            using(var read=world.ReadCommittedPoses())Assert.Equal((double)level,read.ReadScalar(PoseSample.Current,key).Value);
            source.FailAt=0;meter.Visible=false;world.Step();world.PresentFrame(.1,1);
            Assert.InRange(Math.Abs(gauge.Target.Rotation.Z-beforeAngle),0,2e-6);
            Assert.Equal(powered&&next>=meter.Threshold,load.HasElectricalPower(SocketId.PowerIn));
            meter.Visible=true;world.PresentFrame(0,1);
            var target=1-2*next;var expected=target+(beforeAngle-target)*Math.Exp(-1.8);
            Assert.InRange(Math.Abs(gauge.Target.Rotation.Z-expected),0,2e-6);
            Assert.Equal(powered&&next>=meter.Threshold?lamp.To:lamp.From,Colour(lamp));
            var old=world.ReadCommittedPoses();world.Restore();
            Assert.Throws<InvalidOperationException>(()=>old.ReadScalar(PoseSample.Current,key));
            Assert.Equal(saved,Saved(world));
            var restored=(SoundMeterPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(pose,restored.Transform);Assert.Equal(0,restored.Level);
            Assert.Equal(constructionNeedle,Assert.Single(restored.ScalarRotationAnimations).Target.Transform);
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.Step();
            using(var read=world.ReadCommittedPoses())Assert.Equal(0,read.ReadScalar(PoseSample.Current,key).Value);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    public enum GaugeFault { Unit, Slot, EmptyRange, ReversedRange, InfiniteRange, Axis, Mapping, PhysicalTarget }
    private partial class InvalidGauge : SoundMeterPart
    {
        public GaugeFault Fault;
        public override IReadOnlyList<SceneScalarRotationAnimation> ScalarRotationAnimations
        {
            get
            {
                var gauge=base.ScalarRotationAnimations[0];
                return [Fault switch
                {
                    GaugeFault.Unit=>gauge with {Unit=ScalarUnit.GameIrradiance},
                    GaugeFault.Slot=>gauge with {Source=new(this,new(99))},
                    GaugeFault.EmptyRange=>gauge with {InputTo=0},
                    GaugeFault.ReversedRange=>gauge with {InputTo=-1},
                    GaugeFault.InfiniteRange=>gauge with {InputFrom=-double.MaxValue,InputTo=double.MaxValue},
                    GaugeFault.Axis=>gauge with {Axis=(AnimationRotationAxis)999},
                    GaugeFault.Mapping=>gauge with {Mapping=(ScalarAnimationMapping)999},
                    GaugeFault.PhysicalTarget=>gauge with {Target=this},
                    _=>throw new ArgumentOutOfRangeException(nameof(Fault))
                }];
            }
        }
    }
    [Theory]
    [InlineData(GaugeFault.Unit)]
    [InlineData(GaugeFault.Slot)]
    [InlineData(GaugeFault.EmptyRange)]
    [InlineData(GaugeFault.ReversedRange)]
    [InlineData(GaugeFault.InfiniteRange)]
    [InlineData(GaugeFault.Axis)]
    [InlineData(GaugeFault.Mapping)]
    [InlineData(GaugeFault.PhysicalTarget)]
    public void InvalidGaugeBindingsRejectBeforeRun(GaugeFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var meter=new InvalidGauge {Definition=world.Registry.Definitions[Meter.Value],Fault=fault};
            meter.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Meter.Value,Position=[0,5,0]});world.AttachPart(meter);
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);
        }
        finally{world.Free();}
    }
    private partial class NarrowGauge : ControlledMeter
    {
        public override IReadOnlyList<SceneScalarRotationAnimation> ScalarRotationAnimations=>
            base.ScalarRotationAnimations.Select(gauge=>gauge with {InputFrom=.25,InputTo=.75}).ToArray();
    }
    [Theory]
    [InlineData(0f,0d)]
    [InlineData(.5f,.5d)]
    [InlineData(1f,1d)]
    public void GaugeSaturationChangesOnlyPresentation(float level,double amount)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var meter=new NarrowGauge {Definition=world.Registry.Definitions[Meter.Value],Requested=level};
            meter.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Meter.Value,Position=[0,5,0]});world.AttachPart(meter);
            world.Start();world.Step();
            var key=new ScalarReadKey(world.PhysicsAssembly.QueryOwnerId(new(meter,MachinePart.RootBody)),SoundMeterPart.LevelOutput);
            using(var read=world.ReadCommittedPoses())Assert.Equal((double)level,read.ReadScalar(PoseSample.Current,key).Value);
            var before=world.Physics.Capture().BodyStates.ToArray();world.PresentFrame(.1,1);
            Assert.InRange(Math.Abs(Assert.Single(meter.ScalarRotationAnimations).Target.Rotation.Z-(1-2*amount*(1-Math.Exp(-1.8)))),0,2e-6);
            Assert.Equal(level,meter.Level);Assert.Equal(before,world.Physics.Capture().BodyStates.ToArray());
        }
        finally{world.Free();}
    }
    [Fact]
    public void ChangedGaugeUnitRejectsBeforeAnyControlsAndValidPublicationRetries()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var meter=new ControlledMeter {Definition=world.Registry.Definitions[Meter.Value],Requested=1};
            meter.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Meter.Value,Position=[0,5,0]});world.AttachPart(meter);
            world.Start();SceneAnimationRun run;PoseReadStamp stamp;ScalarRead[] scalars;ElectricalInputRead[] electrical;
            using(var seed=world.ReadCommittedPoses())
            {
                run=new(world.Parts,world.PhysicsAssembly,seed,new Dictionary<SceneCounterKey,SimulationCounterId>(),new Dictionary<SceneTimerKey,SimulationTimerId>(),new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
                stamp=seed.Stamp(PoseSample.Current);scalars=new ScalarRead[seed.ScalarCount];seed.CopyScalars(PoseSample.Current,scalars);
                electrical=new ElectricalInputRead[seed.ElectricalInputCount];seed.CopyElectricalInputs(PoseSample.Current,electrical);
            }
            scalars[0]=scalars[0] with {Unit=ScalarUnit.GameIrradiance,Value=1};
            var bodies=world.PhysicsAssembly.CapturePublicationReads(world.Physics).ToArray();
            for(var i=0;i<bodies.Length;i++)bodies[i]=bodies[i] with {Activity=OwnerActivity.Active};
            var foreign=new CommittedPoseBuffer(stamp,bodies,[],electrical,scalars,[],[]);
            foreign.BeginWrite(0);foreign.Stage(new(stamp.Generation,new(1),MachineWorld.Tick),bodies,[],electrical,scalars,[],[]);foreign.Publish();
            using(var read=foreign.Acquire())Assert.Throws<ArgumentException>(()=>run.Publish(read));
            run.Present(.1);Assert.Equal(Assert.Single(meter.ColourAnimations).From,Colour(Assert.Single(meter.ColourAnimations)));
            Assert.InRange(Math.Abs(Assert.Single(meter.ScalarRotationAnimations).Target.Rotation.Z-1),0,2e-6);
            world.Step();using(var read=world.ReadCommittedPoses())run.Publish(read);run.Present(.1);
            Assert.True(Assert.Single(meter.ScalarRotationAnimations).Target.Rotation.Z<0);run.Remove();
        }
        finally{world.Free();}
    }
    [Fact]
    public void FollowRotationCannotSharePhysicalOrClipWriterAndResetRestoresBaseline()
    {
        var node=new Node3D {Rotation=new(.1f,.2f,.3f)};godot.Tree.Root.AddChild(node);
        try
        {
            var baseline=node.Transform;
            var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);adapter.ClaimPhysicalPose(target);
            Assert.Throws<InvalidOperationException>(()=>adapter.BindFollowingRotation(target,new(0,1,0,18,AnimationClock.Presentation),AnimationRotationAxis.Z));
            adapter.Reset();target=adapter.Register(node);
            var follow=adapter.BindFollowingRotation(target,new(0,1,0,18,AnimationClock.Presentation),AnimationRotationAxis.Z);
            Assert.Throws<InvalidOperationException>(()=>adapter.ClaimPhysicalPose(target));
            Assert.Throws<InvalidOperationException>(()=>adapter.BindRotation(target,new(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation),AnimationRotationAxis.Z));
            adapter.FollowValue(follow,1,0);adapter.Present(1,.1,0);Assert.NotEqual(baseline,node.Transform);
            adapter.Reset();Assert.Equal(baseline,node.Transform);
            Assert.Throws<ArgumentException>(()=>adapter.FollowValue(follow,1,0));
        }
        finally{node.Free();}
    }

}
