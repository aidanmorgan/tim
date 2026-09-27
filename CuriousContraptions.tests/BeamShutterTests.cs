using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BeamShutterTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);
        world.AddPart(new(){Id="shutter",Kind="beam_shutter",Position=[0,8,0]});
        world.AddPart(new(){Id="battery",Kind="battery",Position=[-6,2,4]});
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
            var shutter=(BeamShutterPart)world.FindPart("shutter")!;
            var laser=world.AddPart(new(){Id="laser",Kind="laser",Position=[-3,8,0]});
            var receiver=world.AddPart(new(){Id="receiver",Kind="light_receiver",Position=[3,8,0]});
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
            Assert.True(world.Connect(world.FindPart("battery")!,shutter));
            world.Start();
            var previous=0f;
            var sawPartialClear=false;
            for(var i=0;i<180;i++)
            {
                world.Step();
                Assert.InRange(shutter.Opening-previous,0,2.8f*MachineWorld.Tick+.00001f);
                previous=shutter.Opening;
                var blade=shutter.Boxes.Single(b=>b.Half==BeamShutterPart.BladeHalf);
                Assert.Equal(Vector3.Up*shutter.Opening,blade.At);
                var visible=shutter.GetNode<Node3D>("Visual").GetChildren()
                    .OfType<MeshInstance3D>().Single(m=>m.Mesh is BoxMesh box&&box.Size==BeamShutterPart.BladeHalf*2);
                Assert.Equal(blade.At,visible.Position);
                if(shutter.Opening<.59f)Assert.Empty(Trace().Receptions);
                if(shutter.Opening>.61f&&shutter.Opening<1.2f)
                {
                    Assert.Equal(receiver,Assert.Single(Trace().Receptions).Receiver);
                    sawPartialClear=true;
                }
            }
            Assert.True(sawPartialClear);
            Assert.Equal(GateState.Open,shutter.State);
            Assert.Equal(receiver,Assert.Single(Trace().Receptions).Receiver);
            world.Connections.Clear();
            for(var i=0;i<180;i++)world.Step();
            Assert.Equal(GateState.Closed,shutter.State);
            Assert.Empty(Trace().Receptions);
            world.Restore();
            shutter=(BeamShutterPart)world.FindPart("shutter")!;
            Assert.Equal(GateState.Closed,shutter.State);
            Assert.Equal(0,shutter.Opening);
            Assert.Equal(0,shutter.BladeSpeed);
            Assert.Single(world.Connections);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosingStopsBeforeBallAndResumesWhenClear(bool rotated)
    {
        var world=World();
        try
        {
            var shutter=(BeamShutterPart)world.FindPart("shutter")!;
            if(rotated)shutter.RotationDegrees=new(20,30,40);
            Assert.True(world.Connect(world.FindPart("battery")!,shutter));
            world.Start();
            for(var i=0;i<180;i++)world.Step();
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[0,8,0]});
            var origin=ball.Position;
            world.Connections.Clear();
            for(var i=0;i<180;i++)world.Step();
            Assert.Equal(GateState.Blocked,shutter.State);
            Assert.Equal(0,shutter.BladeSpeed);
            Assert.True(shutter.Opening>0);
            Assert.True(ball.Position.DistanceTo(origin)<.0001f);
            ball.Position=new(-5,8,0);
            for(var i=0;i<180;i++)world.Step();
            Assert.Equal(GateState.Closed,shutter.State);
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
            var shutter=(BeamShutterPart)world.FindPart("shutter")!;
            if(powered)Assert.True(world.Connect(world.FindPart("battery")!,shutter));
            world.Start();
            for(var i=0;i<180;i++)world.Step();
            var ball=world.AddPart(new(){Id="ball",Kind="ball",Position=[-2,8,0]});
            ball.Velocity=Vector3.Right*4;
            for(var i=0;i<120;i++)world.Step();
            if(powered)Assert.InRange(ball.Position.X,1.99f,2.01f);
            else Assert.True(ball.Position.X<-.35f);
        }
        finally{world.Free();}
    }
}
