using Godot;
using System.Text.Json;
using CuriousContraptions.Bridge;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class ClockPendulumTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Clock=new("clock"),Battery=new("battery");
    private partial class Probe : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {if(++Visits==FailAt)throw new InvalidOperationException("Injected pendulum tick failure.");}
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static double Target(MachineWorld world,ClockPart clock)
    {
        using var read=world.ReadCommittedPoses();
        var key=new ScalarReadKey(world.PhysicsAssembly.QueryOwnerId(new(clock,MachinePart.RootBody)),ClockPart.ProgressOutput);
        return .42*double.SinPi(2*read.ReadScalar(PoseSample.Current,key).Value);
    }
    private static void Angle(Node3D node,double expected)=>Assert.InRange(Math.Abs(node.Rotation.Z-expected),0,2e-6);
    [Theory]
    [InlineData(.1f,1)]
    [InlineData(.1f,4)]
    [InlineData(.4f,1)]
    [InlineData(.4f,4)]
    public void PendulumUsesCommittedPhaseAcrossFailurePauseHiddenPowerAndReset(float interval,int failedSubstep)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var clock=(ClockPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Clock.Value,Position=[0,5,0],
                Properties=new(){[PartParameterName.Of(ClockParameter.IntervalSeconds)]=interval}});
            var battery=new Probe {Definition=world.Registry.Definitions[Battery.Value]};
            battery.Configure(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Battery.Value,Position=[-4,5,0]});world.AttachPart(battery);
            var supply=new SupplyControl(world,battery);
            Assert.True(world.Connect(supply.Output,SocketId.Supply,clock,SocketId.PowerIn,ConnectionDomain.Electrical));
            var saved=Saved(world);var gauge=Assert.Single(clock.ScalarRotationAnimations);var baseline=gauge.Target.Transform;
            Assert.Equal(ScalarAnimationMapping.SineCycle,gauge.Mapping);
            Assert.Equal(AnimationClock.Presentation,gauge.Definition.Clock);Assert.Equal(0,gauge.Definition.Initial);
            world.Start();var stopped=world.Oscillators.Capture();
            world.Step();world.PresentFrame(.1,1);Assert.Equal(baseline,gauge.Target.Transform);
            supply.SetAndSettle(SimulationLatchPhase.On);
            for(var i=0;i<clock.IntervalTicks/4;i++)world.Step();
            var target=Target(world,clock);Assert.True(target>.3);
            Assert.Equal(baseline,gauge.Target.Transform);
            world.Running=false;var physical=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            world.PresentFrame(.1,1);var angle=target*(1-Math.Exp(-3));Angle(gauge.Target,angle);
            var live=world.Oscillators.Capture();world.Oscillators.Restore(stopped);
            world.PresentFrame(.1,1);angle=target*(1-Math.Exp(-6));Angle(gauge.Target,angle);
            Assert.Equal(ticks,world.Ticks);Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());
            world.Oscillators.Restore(live);world.Running=true;
            battery.FailAt=battery.Visits+failedSubstep;
            Assert.Throws<InvalidOperationException>(world.Step);Assert.Equal(target,Target(world,clock));
            world.PresentFrame(.1,1);angle=target*(1-Math.Exp(-9));Angle(gauge.Target,angle);
            battery.FailAt=0;clock.Visible=false;
            for(var i=0;i<clock.IntervalTicks/2;i++)world.Step();
            var negative=Target(world,clock);Assert.True(negative<-.3);
            world.PresentFrame(.1,1);Angle(gauge.Target,angle);
            clock.Visible=true;world.PresentFrame(0,1);
            angle=negative+(angle-negative)*Math.Exp(-3);Angle(gauge.Target,angle);
            supply.SetAndSettle(SimulationLatchPhase.Off);Assert.Equal(0,Target(world,clock));
            world.Running=false;world.PresentFrame(.1,1);Angle(gauge.Target,angle*Math.Exp(-3));
            world.PresentFrame(2,1);Angle(gauge.Target,0);
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=(ClockPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(baseline,Assert.Single(restored.ScalarRotationAnimations).Target.Transform);
            world.LoadMachine(world.Snapshot());Assert.Equal(saved,Saved(world));
            world.Start();world.PresentFrame(.5,1);
            restored=(ClockPart)world.FindPart(FixtureParts.Id(FixturePartId.First))!;
            Assert.Equal(baseline,Assert.Single(restored.ScalarRotationAnimations).Target.Transform);
            world.Restore();Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
    [Fact]
    public void InteriorInitialValueSurvivesStopResetAndInvalidDefinitionsReject()
    {
        foreach(var invalid in new[]{-2d,2,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>new AnimationFollowDefinition(-1,1,invalid,30,AnimationClock.Presentation));
        var batch=new AnimationBatch(1);
        var handle=batch.RegisterFollow(new(new(0),AnimationProperty.LocalRotationAngle),new(-1,1,0,30,AnimationClock.Presentation));
        Assert.Equal(0,batch.Read(handle).Value);
        batch.FollowValue(handle,1,0);batch.Advance(1,.1,0);
        Assert.InRange(batch.Read(handle).Value,.9,1);
        batch.Stop(handle,AnimationStop.RestoreInitial,.1);Assert.Equal(0,batch.Read(handle).Value);
        batch.FollowValue(handle,-1,.1);batch.Advance(2,.2,0);Assert.InRange(batch.Read(handle).Value,-1,-.9);
        batch.Reset();Assert.Equal(0,Assert.Single(batch.Samples.ToArray()).Value);
    }
}
