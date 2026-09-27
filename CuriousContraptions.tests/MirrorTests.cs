using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class MirrorTests(HeadlessFixture godot)
{
    private MachineWorld World()
    {
        var world=new MachineWorld {Gravity=0,Pressure=0};
        godot.Tree.Root.AddChild(world);return world;
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SurfaceNormalReflectsInThreeDimensionsAndPreviewDoesNotPowerAnything(bool rotated)
    {
        var world=World();
        try
        {
            var mirror=world.AddPart(new(){Id="mirror",Kind="mirror",Position=[0,6,0],Rotation=[0,0,45]});
            var point=mirror.Transform*mirror.OpticalReflector!.Value.Surface.At;
            var laser=(LaserPart)world.AddPart(new(){Id="laser",Kind="laser"});
            laser.Position=point-Vector3.Right*3;
            var receiver=(LightReceiverPart)world.AddPart(new(){Id="receiver",Kind="light_receiver",Rotation=[0,0,-90]});
            receiver.Position=point+Vector3.Down*3;
            if(rotated)
            {
                var rotation=Basis.FromEuler(new(.3f,.7f,-.2f));
                var around=new Transform3D(rotation,new Vector3(0,6,0)-rotation*new Vector3(0,6,0));
                foreach(var p in world.Parts)p.Transform=around*p.Transform;
            }
            var preview=OpticalNetwork.Trace(world,laser,laser.OpticalPreviewSource!.Value);
            Assert.Equal(receiver,preview.Receiver);
            Assert.Equal(2,preview.Segments.Count);
            Assert.False(receiver.Active);Assert.False(laser.Enabled);Assert.Empty(laser.BeamPath);
            Assert.True(preview.Power.DistanceTo(LaserPart.BeamPower*MirrorPart.Reflectivity)<.00001f);
            var incoming=(preview.Segments[0].To-preview.Segments[0].From).Normalized();
            var outgoing=(preview.Segments[1].To-preview.Segments[1].From).Normalized();
            var normal=mirror.Basis*Vector3.Left;
            Assert.True((outgoing-(incoming-2*incoming.Dot(normal)*normal)).Length()<.00001f);
            var battery=world.AddPart(new(){Id="battery",Kind="battery",Position=[-8,2,0]});
            Assert.True(world.Connect(battery,laser));
            world.Start();world.Activate(laser);world.Step();world.Step();
            Assert.True(receiver.Active);
            Assert.Equal(2,laser.BeamPath.Count);
            world.Restore();
            Assert.Empty(((LaserPart)world.FindPart("laser")!).BeamPath);
            Assert.False(world.FindPart("mirror")!.IsSelected);
        }
        finally{world.Free();}
    }
    [Fact]
    public void TwoMirrorsUseOneRangeBudgetAndMultiplyLosses()
    {
        var world=World();
        try
        {
            var first=world.AddPart(new(){Id="first",Kind="mirror",Position=[0,4,0],Rotation=[0,0,-45]});
            var p=first.Transform*first.OpticalReflector!.Value.Surface.At;
            var second=world.AddPart(new(){Id="second",Kind="mirror",Rotation=[0,0,135]});
            second.Position=p+Vector3.Up*3-second.Basis*second.OpticalReflector!.Value.Surface.At;
            var laser=world.AddPart(new(){Id="laser",Kind="laser"});
            laser.Position=p-Vector3.Right*3;
            var receiver=world.AddPart(new(){Id="receiver",Kind="light_receiver"});
            receiver.Position=p+Vector3.Up*3+Vector3.Right*3;
            var source=laser.OpticalPreviewSource!.Value;
            var trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Equal(receiver,trace.Receiver);
            Assert.Equal(3,trace.Segments.Count);
            Assert.True(trace.Power.DistanceTo(source.Power*MirrorPart.Reflectivity*MirrorPart.Reflectivity)<.00001f);
            var limited=OpticalNetwork.Trace(world,laser,source with {Range=4});
            Assert.Null(limited.Receiver);
            Assert.InRange(limited.Segments.Sum(s=>s.From.DistanceTo(s.To)),3.999f,4.001f);
        }
        finally{world.Free();}
    }
    [Fact]
    public void BackFrameAndDownstreamWallAbsorbRatherThanReflect()
    {
        var world=World();
        try
        {
            var mirror=world.AddPart(new(){Id="mirror",Kind="mirror",Position=[0,6,0],Rotation=[0,0,45]});
            var point=mirror.Transform*mirror.OpticalReflector!.Value.Surface.At;
            var laser=world.AddPart(new(){Id="laser",Kind="laser"});
            laser.Position=point-Vector3.Right*3;
            var source=laser.OpticalPreviewSource!.Value;
            var wall=world.AddPart(new(){Id="wall",Kind="wall"});
            wall.Position=point+Vector3.Down*1.5f;
            var trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Null(trace.Receiver);
            Assert.Equal(2,trace.Segments.Count);
            Assert.True(trace.Segments[1].From.DistanceTo(trace.Segments[1].To)<1);
            wall.Visible=false;
            mirror.RotateObjectLocal(Vector3.Up,Mathf.Pi);
            trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Single(trace.Segments);
            Assert.Null(trace.Receiver);
            mirror.RotateObjectLocal(Vector3.Up,Mathf.Pi);
            laser.Position+=Vector3.Back*.73f; // Outside the silvered radius, inside square backing.
            trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Single(trace.Segments);
            Assert.True(trace.Segments[0].From.DistanceTo(trace.Segments[0].To)<4);
        }
        finally{world.Free();}
    }
    [Fact]
    public void PlacementGhostDoesNotRequireAWorldParent()
    {
        var world=World();
        var ghostContainer=new Node3D();
        godot.Tree.Root.AddChild(ghostContainer);
        try
        {
            var ghost=(MirrorPart)world.Registry.Create(new(){Id="ghost",Kind="mirror"});
            ghostContainer.AddChild(ghost);
            ghost.SetSelected(true);
            ghost._Process(0);
            Assert.False(ghost.GetNode<Node3D>("Visual/OutgoingAimPreview").Visible);
        }
        finally{ghostContainer.Free();world.Free();}
    }
    [Fact]
    public void FacingMirrorLoopTerminatesWithoutCreatingPower()
    {
        var world=World();
        try
        {
            world.AddPart(new(){Id="right",Kind="mirror",Position=[1,5,0]});
            world.AddPart(new(){Id="left",Kind="mirror",Position=[-1,5,0],Rotation=[0,180,0]});
            var laser=world.AddPart(new(){Id="laser",Kind="laser",Position=[-4,5,0]});
            var source=new OpticalEmitter(new(4,0,0),Vector3.Right,100,Vector3.One);
            var trace=OpticalNetwork.Trace(world,laser,source);
            Assert.Equal(OpticalNetwork.MaximumReflections+1,trace.Segments.Count);
            Assert.Null(trace.Receiver);Assert.Equal(Vector3.Zero,trace.Power);
            for(var i=1;i<trace.Segments.Count;i++)
                Assert.True(trace.Segments[i].Power.X<trace.Segments[i-1].Power.X);
        }
        finally{world.Free();}
    }
}
