using Godot;
using System.Text.Json;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class LatchAnimationTests(NativeSceneFixture godot)
{
    private readonly record struct CatalogueId(string Value);
    private static readonly CatalogueId Latch=new("latch"),Switch=new("switch"),Battery=new("battery"),Gate=new("powered_gate");
    private partial class Supply : BatteryPart
    {
        public int Visits,FailAt;
        public override void ObservePhysics(MachineWorld world,float delta)
        {
            base.ObservePhysics(world,delta);
            if(++Visits==FailAt)throw new InvalidOperationException("Injected latch observation failure.");
        }
    }
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private static void AssertPose(Node3D rocker,Transform3D baseline,float angle)
    {
        var expected=baseline.Basis*new Basis(Vector3.Back,angle);
        Assert.InRange((rocker.Basis.X-expected.X).Length(),0,1e-6f);
        Assert.InRange((rocker.Basis.Y-expected.Y).Length(),0,1e-6f);
        Assert.Equal(baseline.Origin,rocker.Position);
    }

    [Theory]
    [InlineData(1,false)]
    [InlineData(1,true)]
    [InlineData(4,false)]
    [InlineData(4,true)]
    public void RockerAndLampFollowOnlyCommittedMemoryAcrossRollbackPauseReverseAndReset(int substep,bool supplied)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var source=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Switch.Value,Position=[-4,6,0]});
            var latch=(LatchPart)world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Second),Kind=Latch.Value,Position=[0,6,0]});
            var supply=new Supply {Definition=world.Registry.Definitions[Battery.Value]};
            supply.Configure(new(){Id=FixtureParts.Id(FixturePartId.Third),Kind=Battery.Value,Position=[0,10,4]});world.AttachPart(supply);
            var gate=world.AddPart(new(){Id=FixtureParts.Id(FixturePartId.Fourth),Kind=Gate.Value,Position=[4,6,0]});
            Assert.True(world.Connect(source,SocketId.ActivationOut,latch,SocketId.SetIn,ConnectionDomain.Activation));
            if(supplied)Assert.True(world.Connect(supply,SocketId.Supply,latch,SocketId.PowerIn,ConnectionDomain.Electrical));
            Assert.True(world.Connect(latch,SocketId.Supply,gate,SocketId.PowerIn,ConnectionDomain.Electrical));
            var rotation=Assert.Single(latch.RotationAnimations);var colour=Assert.Single(latch.ColourAnimations);
            Assert.Equal(SceneAnimationDrive.Endpoint,rotation.Drive);Assert.Equal(SceneAnimationDrive.Endpoint,colour.Drive);
            var rocker=rotation.Target;var baseline=rocker.Transform;
            var material=(StandardMaterial3D)colour.Target.MaterialOverride;var initial=material.AlbedoColor;
            var saved=Saved(world);var pose=latch.Transform;
            world.Start();world.Activate(source);world.Step();world.Step();
            Assert.Equal(SimulationLatchPhase.On,latch.State);Assert.Equal(supplied,gate.HasElectricalPower(SocketId.PowerIn));
            Assert.Equal(baseline,rocker.Transform);Assert.Equal(initial,material.AlbedoColor);
            var physical=world.Physics.Capture().BodyStates.ToArray();var ticks=world.Ticks;
            latch.Active=false;world.Running=false;world.PresentFrame(.0625,1);
            AssertPose(rocker,baseline,.25f);Assert.Equal(colour.From.Lerp(colour.To,.5f),material.AlbedoColor);
            Assert.Equal(physical,world.Physics.Capture().BodyStates.ToArray());Assert.Equal(ticks,world.Ticks);
            latch.Active=true;world.Running=true;
            latch.HandleActivation(world,ActivationCommand.Reset);world.Step();
            supply.FailAt=supply.Visits+substep;
            var beforeFailure=world.Physics.Capture().BodyStates.ToArray();var beforeTicks=world.Ticks;
            Assert.Throws<InvalidOperationException>(world.Step);
            Assert.Equal(SimulationLatchPhase.On,latch.State);Assert.True(latch.Active);
            Assert.Equal(beforeTicks,world.Ticks);Assert.Equal(beforeFailure,world.Physics.Capture().BodyStates.ToArray());
            world.PresentFrame(0,1);AssertPose(rocker,baseline,.25f);
            supply.FailAt=0;world.Step();
            Assert.Equal(SimulationLatchPhase.Off,latch.State);Assert.False(gate.HasElectricalPower(SocketId.PowerIn));
            world.Running=false;world.PresentFrame(.03125,1);
            AssertPose(rocker,baseline,.5f*.103515625f);
            Assert.Equal(colour.From.Lerp(colour.To,.103515625f),material.AlbedoColor);
            world.PresentFrame(.125,1);AssertPose(rocker,baseline,0);Assert.Equal(initial,material.AlbedoColor);
            world.Restore();Assert.Equal(saved,Saved(world));
            var restored=(LatchPart)world.FindPart(FixtureParts.Id(FixturePartId.Second))!;
            Assert.Equal(pose,restored.Transform);Assert.Equal(baseline,Assert.Single(restored.RotationAnimations).Target.Transform);
            Assert.Equal(initial,((StandardMaterial3D)Assert.Single(restored.ColourAnimations).Target.MaterialOverride).AlbedoColor);
            Assert.Equal(SimulationLatchPhase.Off,restored.State);
        }
        finally{world.Free();}
    }

    private partial class InvalidLatch : LatchPart
    {
        public SceneAnimationDrive Drive;
        public AnimationRepeat Repeat;
        public override IReadOnlyList<SceneRotationAnimation> RotationAnimations=>
            [base.RotationAnimations[0] with
            {
                Drive=Drive,
                Definition=new(0,.5,.125,AnimationCurve.SmoothStep,Repeat,AnimationClock.Presentation)
            }];
    }
    [Theory]
    [InlineData((SceneAnimationDrive)999,AnimationRepeat.Once)]
    [InlineData(SceneAnimationDrive.Endpoint,AnimationRepeat.Loop)]
    [InlineData(SceneAnimationDrive.Endpoint,AnimationRepeat.PingPong)]
    public void UnsupportedRotationDriveRejectsBeforeRun(SceneAnimationDrive drive,AnimationRepeat repeat)
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};godot.Tree.Root.AddChild(world);
        try
        {
            var latch=new InvalidLatch {Definition=world.Registry.Definitions[Latch.Value],Drive=drive,Repeat=repeat};
            latch.Configure(new(){Id=FixtureParts.Id(FixturePartId.First),Kind=Latch.Value});world.AttachPart(latch);
            var saved=Saved(world);
            Assert.Throws<ArgumentException>(world.Start);Assert.False(world.HasPhysicsState);Assert.Equal(saved,Saved(world));
        }
        finally{world.Free();}
    }
}
