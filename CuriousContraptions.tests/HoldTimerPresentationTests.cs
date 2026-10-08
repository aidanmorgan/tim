using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class HoldTimerPresentationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Hold=new("hold_timer"),Battery=new("battery"),Load=new("powered_gate");
    private partial class Probe : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected hold-timer presentation failure.");}
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static Color Colour(SceneColourAnimation lamp)=>((StandardMaterial3D)lamp.Target.MaterialOverride).AlbedoColor;
    private static double Remaining(MachineWorld world,HoldTimerPart meter)
    {
        using var read=world.ReadCommittedPoses();
        return read.ReadScalar(PoseSample.Current,new(world.PhysicsAssembly.QueryOwnerId(new(meter,MachinePart.RootBody)),
            HoldTimerPart.RemainingOutput)).Value;
    }
    public static IEnumerable<object[]> Rows()
    {
        foreach(var duration in new[]{.1f,1.13f})
        foreach(var powered in new[]{false,true})
        foreach(var substep in new[]{1,4})yield return [duration,powered,substep];
    }
    private static void AssertFill(Node3D bar,Transform3D baseline,double fraction)
    {
        var scale=(float)Math.Max(.001,fraction);
        Assert.InRange(Math.Abs(bar.Transform.Basis.Scale.X-scale),0,2e-6);
        Assert.InRange(Math.Abs(bar.Position.X-(baseline.Origin.X-.5f*(1-scale))),0,2e-6);
        Assert.Equal(baseline.Origin.Y,bar.Position.Y);Assert.Equal(baseline.Origin.Z,bar.Position.Z);
    }
    [Theory]
    [MemberData(nameof(Rows))]
    public void CommittedWindowOwnsBarAndLampThroughPauseHideFailuresExpiryResetSave(float duration,bool powered,int substep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var meter=(HoldTimerPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Hold.Value,Position=[0,5,0],
                Properties=new(){[PartParameterName.Of(HoldTimerParameter.HoldSeconds)]=duration}});
            var source=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            source.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-4,5,0],
                Properties=new(){[PartParameterName.Of(BatteryParameter.Enabled)]=powered?1:0}});world.AttachPart(source);
            var load=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Load.Value,Position=[4,5,0]});
            Assert.True(world.Connect(source,SocketId.Supply,meter,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(meter,SocketId.Supply,load,SocketId.PowerIn,ConnectionDomain.Electrical));
            var bar=Assert.Single(meter.ScalarExtents).Target;var lamp=Assert.Single(meter.ColourAnimations);
            var baseline=bar.Transform;var pose=meter.Transform;var saved=Saved(world);
            Assert.False(bar.Visible);
            world.Start();world.Step();
            Assert.Equal(baseline,bar.Transform);Assert.False(bar.Visible);Assert.Equal(lamp.From,Colour(lamp));
            world.PresentFrame(0,1);AssertFill(bar,baseline,0);Assert.False(bar.Visible);
            world.Activate(meter);world.Step();var due=meter.DueTick;
            Assert.False(bar.Visible);Assert.Equal(lamp.From,Colour(lamp));
            Assert.Equal(powered,load.HasElectricalPower(SocketId.PowerIn));
            var physical=world.Physics.Capture().BodyStates.ToArray();var tick=world.Ticks;
            world.Running=false;world.PresentFrame(.05,1);
            Assert.True(bar.Visible);AssertFill(bar,baseline,Remaining(world,meter));
            Assert.Equal(lamp.From.Lerp(lamp.To,.5f),Colour(lamp)); // Window indication is independent of supplied power.
            meter.Active=false;world.PresentFrame(.05,1);
            Assert.True(bar.Visible);Assert.Equal(lamp.To,Colour(lamp));
            Assert.Equal(tick,world.Ticks);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            meter.Active=true;world.Running=true;world.Step();world.PresentFrame(0,1);
            var before=bar.Transform;var remaining=Remaining(world,meter);var revision=world.ControlRevision;
            source.FailAt=source.Visits+substep;Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(revision,world.ControlRevision);Assert.Equal(remaining,Remaining(world,meter));
            world.PresentFrame(.01,1);Assert.Equal(before,bar.Transform);Assert.True(bar.Visible);
            source.FailAt=0;meter.Visible=false;
            for(var i=0;i<3;i++)world.Step();
            world.PresentFrame(.1,1);Assert.Equal(before,bar.Transform);
            meter.Visible=true;world.PresentFrame(0,1);AssertFill(bar,baseline,Remaining(world,meter));Assert.True(bar.Visible);
            while(world.Ticks<=due)world.Step();
            Assert.True(bar.Visible); // No scene mutation without a presentation frame.
            Assert.False(load.HasElectricalPower(SocketId.PowerIn));
            world.PresentFrame(.1,1);Assert.False(bar.Visible);AssertFill(bar,baseline,0);Assert.Equal(lamp.From,Colour(lamp));
            world.Activate(meter);world.Step();world.PresentFrame(.1,1);Assert.True(bar.Visible);Assert.Equal(lamp.To,Colour(lamp));
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=(HoldTimerPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(pose,restored.Transform);Assert.Equal(baseline,Assert.Single(restored.ScalarExtents).Target.Transform);
            Assert.False(Assert.Single(restored.ScalarExtents).Target.Visible);
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));world.Start();world.Step();world.PresentFrame(.1,1);
            Assert.False(Assert.Single(((HoldTimerPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!).ScalarExtents).Target.Visible);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    public enum BindingFault { Unit, Slot, Owner, EmptyRange, Visibility, PhysicalTarget, Duplicate }
    private partial class InvalidMeter : HoldTimerPart
    {
        public BindingFault Fault;
        public override IReadOnlyList<SceneScalarExtent> ScalarExtents
        {
            get
            {
                var extent=base.ScalarExtents[0];
                return Fault switch
                {
                    BindingFault.Unit=>[extent with {Unit=ScalarUnit.GameIrradiance}],
                    BindingFault.Slot=>[extent with {Source=new(this,new(99))}],
                    BindingFault.Owner=>[extent with {Source=default}],
                    BindingFault.EmptyRange=>[extent with {InputTo=0}],
                    BindingFault.Visibility=>[extent with {Visibility=(ScalarExtentVisibility)999}],
                    BindingFault.PhysicalTarget=>[extent with {Target=this}],
                    BindingFault.Duplicate=>[extent,extent],
                    _=>throw new ArgumentOutOfRangeException(nameof(Fault))
                };
            }
        }
    }
    [Theory]
    [InlineData(BindingFault.Unit)]
    [InlineData(BindingFault.Slot)]
    [InlineData(BindingFault.Owner)]
    [InlineData(BindingFault.EmptyRange)]
    [InlineData(BindingFault.Visibility)]
    [InlineData(BindingFault.PhysicalTarget)]
    [InlineData(BindingFault.Duplicate)]
    public void UnsupportedBindingsRejectBeforeRun(BindingFault fault)
    {
        var world=new MachineWorld();godot.Tree.Root.AddChild(world);
        try
        {
            var meter=new InvalidMeter {Definition=world.Registry.Definitions[Hold.Value],Fault=fault};
            meter.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Hold.Value,Position=[0,5,0]});world.AttachPart(meter);
            var saved=Saved(world);Assert.Throws<ArgumentException>(world.Start);Assert.False(world.Running);Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Theory]
    [InlineData(ScalarExtentAxis.X,-.5)]
    [InlineData(ScalarExtentAxis.X,0)]
    [InlineData(ScalarExtentAxis.X,.5)]
    [InlineData(ScalarExtentAxis.Y,-.5)]
    [InlineData(ScalarExtentAxis.Y,0)]
    [InlineData(ScalarExtentAxis.Y,.5)]
    [InlineData(ScalarExtentAxis.Z,-.5)]
    [InlineData(ScalarExtentAxis.Z,0)]
    [InlineData(ScalarExtentAxis.Z,.5)]
    public void ExtentPreservesAnchorAndCoalescesDirtyWrites(ScalarExtentAxis axis,double anchor)
    {
        var node=new Node3D {Position=new(3,4,5),Rotation=new(.2f,.3f,.4f),Scale=new(2,3,4),Visible=false};
        godot.Tree.Root.AddChild(node);
        try
        {
            var baseline=node.Transform;var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
            adapter.ClaimScalarExtent(target,new(axis,anchor,.001,1));
            Assert.Throws<InvalidOperationException>(()=>adapter.ClaimPhysicalPose(target));
            Assert.Throws<InvalidOperationException>(()=>adapter.ClaimScalarExtent(target,new(axis,anchor,.001,1)));
            Assert.Throws<InvalidOperationException>(()=>adapter.BindRotation(target,new(0,1,1,AnimationCurve.Linear,AnimationRepeat.Once,AnimationClock.Presentation),AnimationRotationAxis.X));
            var local=axis switch {ScalarExtentAxis.X=>Vector3.Right,ScalarExtentAxis.Y=>Vector3.Up,ScalarExtentAxis.Z=>Vector3.Back,
                _=>throw new ArgumentOutOfRangeException(nameof(axis))};
            var fixedAnchor=baseline*(local*(float)anchor);
            ulong frame=0;
            foreach(var fraction in new[]{.75,.25,0d,1d})
            {
                adapter.QueueScalarExtent(target,.9,false);adapter.QueueScalarExtent(target,fraction,true);
                var work=adapter.Present(++frame,0,0);Assert.InRange(work.TransformWrites,0,1);Assert.True(node.Visible);
                Assert.InRange((node.Transform*(local*(float)anchor)-fixedAnchor).Length(),0,2e-6);
                adapter.QueueScalarExtent(target,fraction,true);work=adapter.Present(++frame,0,0);
                Assert.Equal(0,work.TransformWrites);Assert.Equal(0,work.VisibilityWrites);
            }
            foreach(var invalid in new[]{double.NaN,double.PositiveInfinity,-.1,1.1})
                Assert.Throws<ArgumentOutOfRangeException>(()=>adapter.QueueScalarExtent(target,invalid,true));
            adapter.QueueScalarExtent(target,.5,false);var hidden=adapter.Present(++frame,0,0);
            Assert.Equal(1,hidden.VisibilityWrites);Assert.False(node.Visible);
            adapter.Reset();Assert.Equal(baseline,node.Transform);Assert.False(node.Visible);
            Assert.Throws<ArgumentException>(()=>adapter.QueueScalarExtent(target,.5,true));
            target=adapter.Register(node);adapter.ClaimPhysicalPose(target);
            Assert.Throws<InvalidOperationException>(()=>adapter.ClaimScalarExtent(target,new(axis,anchor,.001,1)));
            adapter.Reset();
        }
        finally{node.Free();}
    }
    [Fact]
    public void ExtentDefinitionRejectsUnsupportedNumericEnvelope()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ScalarExtentDefinition((ScalarExtentAxis)999,0,.001,1));
        foreach(var value in new[]{double.NaN,double.PositiveInfinity,-1e7,1e7})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ScalarExtentDefinition(ScalarExtentAxis.X,value,.001,1));
        foreach(var value in new[]{double.NaN,double.PositiveInfinity,0,-1,1.01,1e-7})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new ScalarExtentDefinition(ScalarExtentAxis.X,0,value,1));
    }

    [Fact]
    public void UnitMismatchRejectsBeforeControlsAndValidCommitRetries()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var meter=(HoldTimerPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Hold.Value,Position=[0,5,0]});
            var bar=Assert.Single(meter.ScalarExtents).Target;var baseline=bar.Transform;var lamp=Assert.Single(meter.ColourAnimations);
            world.Start();SceneAnimationRun run;PoseReadStamp stamp;ScalarRead[] scalars;ElectricalInputRead[] electrical;SimulationTimerState[] timers;
            using(var seed=world.ReadCommittedPoses())
            {
                run=new(world.Parts,world.PhysicsAssembly,seed,new Dictionary<SceneCounterKey,SimulationCounterId>(),new Dictionary<SceneTimerKey,SimulationTimerId>(),new Dictionary<SceneOscillatorKey,SimulationOscillatorId>());
                stamp=seed.Stamp(PoseSample.Current);scalars=new ScalarRead[seed.ScalarCount];seed.CopyScalars(PoseSample.Current,scalars);
                electrical=new ElectricalInputRead[seed.ElectricalInputCount];seed.CopyElectricalInputs(PoseSample.Current,electrical);
                timers=new SimulationTimerState[seed.TimerCount];seed.CopyTimers(PoseSample.Current,timers);
            }
            scalars[0]=scalars[0] with {Unit=ScalarUnit.GameIrradiance,Value=.5};
            var bodies=world.PhysicsAssembly.CapturePublicationReads(world.Physics).ToArray();
            for(var i=0;i<bodies.Length;i++)bodies[i]=bodies[i] with {Activity=OwnerActivity.Active};
            var foreign=new CommittedPoseBuffer(stamp,bodies,[],electrical,scalars,timers,[]);
            foreign.BeginWrite(0);foreign.Stage(new(stamp.Generation,new(1),MachineWorld.Tick),bodies,[],electrical,scalars,timers,[]);foreign.Publish();
            using(var read=foreign.Acquire())Assert.Throws<ArgumentException>(()=>run.Publish(read));
            run.Present(.1);Assert.False(bar.Visible);Assert.Equal(lamp.From,Colour(lamp));AssertFill(bar,baseline,0);
            world.Activate(meter);world.Step();using(var read=world.ReadCommittedPoses())run.Publish(read);
            run.Present(.1);Assert.True(bar.Visible);AssertFill(bar,baseline,1);Assert.Equal(lamp.To,Colour(lamp));
            run.Remove();Assert.False(bar.Visible);Assert.Equal(baseline,bar.Transform);
        }
        finally{world.Free();}
    }
    [Fact]
    public void WarmedExtentSubmissionAllocatesNoManagedScratchAndRemovalRestoresVisibility()
    {
        var node=new Node3D {Visible=true};godot.Tree.Root.AddChild(node);
        try
        {
            var baseline=node.Transform;var adapter=new SceneAnimationAdapter(1);var target=adapter.Register(node);
            adapter.ClaimScalarExtent(target,new(ScalarExtentAxis.X,-.5,.001,1));
            for(ulong i=1;i<=100;i++){adapter.QueueScalarExtent(target,.5,true);adapter.Present(i,0,0);}
            var before=GC.GetAllocatedBytesForCurrentThread();
            for(ulong i=101;i<=1100;i++){adapter.QueueScalarExtent(target,i%2==0?.25:.75,i%2==0);adapter.Present(i,0,0);}
            var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.Equal(0,allocated);
            adapter.QueueScalarExtent(target,.5,false);adapter.Present(1101,0,0);Assert.False(node.Visible);
            adapter.RemoveTarget(target,AnimationTargetRemoval.RestoreBaseline);
            Assert.True(node.Visible);Assert.Equal(baseline,node.Transform);
            Assert.Throws<ArgumentException>(()=>adapter.QueueScalarExtent(target,.5,true));
        }
        finally{node.Free();}
    }

    private partial class NarrowExtent : HoldTimerPart
    {
        public override IReadOnlyList<SceneScalarExtent> ScalarExtents=>
            base.ScalarExtents.Select(extent=>extent with {InputFrom=.25,InputTo=.75,Visibility=ScalarExtentVisibility.Always}).ToArray();
    }
    [Theory]
    [InlineData(0,0d)]
    [InlineData(1,1d)]
    [InlineData(121,.5d)]
    public void AlwaysVisibleExtentSaturatesOnlyItsDisplay(int ticks,double fraction)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var meter=new NarrowExtent {Definition=world.Registry.Definitions[Hold.Value]};
            meter.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Hold.Value,Position=[0,5,0],
                Properties=new(){[PartParameterName.Of(HoldTimerParameter.HoldSeconds)]=2}});world.AttachPart(meter);
            var bar=Assert.Single(meter.ScalarExtents).Target;var baseline=bar.Transform;world.Start();
            if(ticks>0)world.Activate(meter);
            for(var i=0;i<ticks;i++)world.Step();
            var reading=Remaining(world,meter);var physical=world.Physics.Capture().BodyStates.ToArray();
            world.PresentFrame(.1,1);Assert.True(bar.Visible);AssertFill(bar,baseline,fraction);
            Assert.Equal(reading,Remaining(world,meter));Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(ticks>0,meter.Active);
        }
        finally{world.Free();}
    }
}
