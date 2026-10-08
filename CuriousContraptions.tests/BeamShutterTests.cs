using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BeamShutterTests(NativeSceneFixture godot)
{
    private enum Fixture { Shutter, Battery, Laser, Receiver, Ball }
    private static string Id(Fixture fixture)=>fixture switch
    {
        Fixture.Shutter=>"shutter",Fixture.Battery=>"battery",Fixture.Laser=>"laser",
        Fixture.Receiver=>"receiver",Fixture.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static string Kind(Fixture fixture)=>fixture switch
    {
        Fixture.Shutter=>"beam_shutter",Fixture.Battery=>"battery",Fixture.Laser=>"laser",
        Fixture.Receiver=>"light_receiver",Fixture.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static MachinePart Add(MachineWorld world,Fixture fixture,Vector3 at)=>
        world.AddPart(new(){Id=Id(fixture),Kind=Kind(fixture),Position=[at.X,at.Y,at.Z]});
    private static MachinePart Find(MachineWorld world,Fixture fixture)=>
        world.FindPart(Id(fixture))??throw new InvalidOperationException("Required shutter fixture is absent.");
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        Add(world,Fixture.Shutter,new(0,8,0));
        Add(world,Fixture.Battery,new(-6,2,4));
        return world;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovingBladeClipsActualRayAndRestoresClosed(bool rotated)
    {
        var world=World();
        try
        {
            var shutter=(BeamShutterPart)Find(world,Fixture.Shutter);
            var laser=Add(world,Fixture.Laser,new(-3,8,0));
            var receiver=Add(world,Fixture.Receiver,new(3,8,0));
            if(rotated)
            {
                var basis=Basis.FromEuler(new(.3f,.7f,-.2f));
                var around=new Transform3D(basis,new Vector3(0,8,0)-basis*new Vector3(0,8,0));
                foreach(var part in new MachinePart[]{shutter,laser,receiver})part.Transform=around*part.Transform;
            }
            OpticalTrace Trace()=>OpticalNetwork.Trace(world,laser,laser.OpticalPreviewSource!.Value);
            Assert.Empty(Trace().Receptions);
            var hit=shutter.Transform.AffineInverse()*Assert.Single(Trace().Segments).To;
            Assert.InRange(hit.X,-.0351f,-.0349f);
            var supply=new SupplyControl(world,Find(world,Fixture.Battery));
            Assert.True(world.Connect(supply.Output,shutter));
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();
            supply.SetAndSettle(SimulationLatchPhase.On);
            var previous=shutter.Opening;
            var sawPartialClear=false;
            for(var i=0;i<180;i++)
            {
                world.Step();
                Assert.InRange(shutter.Opening-previous,0,2.8f*MachineWorld.Tick+.00001f);
                previous=shutter.Opening;
                var blade=shutter.Boxes.Single(b=>b.Half==BeamShutterPart.BladeHalf);
                Assert.Equal(Vector3.Zero,blade.At); // Construction remains body-local.
                var visible=shutter.GetChildren().SelectMany(node=>node.GetChildren())
                    .OfType<MeshInstance3D>().Single(m=>m.Mesh is BoxMesh box&&box.Size==BeamShutterPart.BladeHalf*2);
                var body=world.PhysicsAssembly.Body(new(shutter,BeamShutterPart.BladeBody));
                world.PresentFrame(0,1);
                var rendered=SceneGeometryAdapter.CaptureRigidPose(shutter.Transform*visible.Transform);
                Assert.InRange((rendered.Center-body.Center).Length,0,1e-6);
                Assert.InRange((rendered.Rotation.Inverse()*body.Pose.Rotation).RotationVector().Length,0,1e-6);
                if(shutter.Opening<.59f)Assert.Empty(Trace().Receptions);
                if(shutter.Opening>.61f&&shutter.Opening<1.2f)
                {
                    Assert.Equal(receiver,Assert.Single(Trace().Receptions).Receiver);
                    sawPartialClear=true;
                }
            }
            Assert.True(sawPartialClear);
            Console.WriteLine($"Shutter endpoint observation: tick={world.Ticks}, travel={shutter.Opening:R}, stroke={BeamShutterPart.Stroke:R}, speed={shutter.BladeSpeed:R}");
            Assert.Equal(GateState.Open,shutter.State);
            Assert.Equal(receiver,Assert.Single(Trace().Receptions).Receiver);
            supply.SetAndSettle(SimulationLatchPhase.Off);
            for(var i=0;i<180;i++)world.Step();
            Assert.Equal(GateState.Closed,shutter.State);
            Assert.Empty(Trace().Receptions);
            world.Restore();
            shutter=(BeamShutterPart)Find(world,Fixture.Shutter);
            Assert.Equal(GateState.Closed,shutter.State);
            Assert.Equal(0,shutter.Opening);
            Assert.Equal(0,shutter.BladeSpeed);
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false,false)]
    [InlineData(true,false)]
    [InlineData(false,true)]
    [InlineData(true,true)]
    public void ClosingBladeMeetsAuthoredMovingCargoAndResetReplays(bool close,bool rotated)
    {
        var world=World();
        try
        {
            var shutter=(BeamShutterPart)Find(world,Fixture.Shutter);
            if(rotated)shutter.RotationDegrees=new(20,30,40);
            var supply=new SupplyControl(world,Find(world,Fixture.Battery));
            Assert.True(world.Connect(supply.Output,shutter));
            var ball=Add(world,Fixture.Ball,shutter.Transform*new Vector3(-1.5f,0,0));
            ball.InitialVelocity=shutter.Basis.X*.5f;
            var velocity=ball.InitialVelocity;
            var construction=Saved(world);
            (PhysicsBodySnapshot[] Bodies,bool Impact) Run()
            {
                var gate=(BeamShutterPart)Find(world,Fixture.Shutter);
                var cargo=Find(world,Fixture.Ball);
                world.Start();
                supply.SetAndSettle(SimulationLatchPhase.On);
                for(var i=0;i<298;i++)world.Step();
                Assert.True(gate.Opening>cargo.Radius+BeamShutterPart.BladeHalf.Y,
                    "Blade must clear the authored cargo path before closing.");
                var blade=world.PhysicsAssembly.Body(new(gate,BeamShutterPart.BladeBody));
                var moving=world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody));
                var root=world.PhysicsAssembly.Body(new(gate,MachinePart.RootBody));
                Assert.InRange(root.Pose.InverseTransformPoint(moving.Center).X,-.251,-.249);
                Assert.InRange(root.Pose.Rotation.Inverse().Apply(moving.LinearVelocity).X,.499,.501);
                if(close)supply.SetAndSettle(SimulationLatchPhase.Off);
                var contact=false;
                for(var i=0;i<360;i++)
                {
                    world.Step();
                    foreach(var impact in world.TickImpacts)
                        if((impact.Pair.A==blade.Id&&impact.Pair.B==moving.Id)||(impact.Pair.B==blade.Id&&impact.Pair.A==moving.Id))
                            contact=true;
                }
                Assert.Equal(close,contact);
                if(close)Assert.Equal(GateState.Closed,gate.State);
                else Assert.True(gate.Opening>cargo.Radius+BeamShutterPart.BladeHalf.Y);
                return(world.Physics.Capture().BodyStates.ToArray(),contact);
            }
            var first=Run();
            world.Restore();Assert.Equal(construction,Saved(world));
            Assert.Equal(velocity,Find(world,Fixture.Ball).InitialVelocity);
            var second=Run();
            Assert.Equal(first.Bodies,second.Bodies);Assert.Equal(first.Impact,second.Impact);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BladeIsPhysicalNotJustAnOpticalFlag(bool powered)
    {
        var world=World();
        try
        {
            var shutter=(BeamShutterPart)Find(world,Fixture.Shutter);
            if(powered)Assert.True(world.Connect(Find(world,Fixture.Battery),shutter));
            var ball=Add(world,Fixture.Ball,new(-14,8,0));
            ball.InitialVelocity=Vector3.Right*4;
            world.Start();
            for(var i=0;i<480;i++)world.Step();
            world.PresentFrame(0,1);
            if(powered)Assert.InRange(ball.Position.X,1.99f,2.01f);
            else Assert.True(ball.Position.X<-.35f);
        }
        finally{world.Free();}
    }
}
