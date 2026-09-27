using Godot;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<HeadlessCollection>]
public class BallDetectorTests(HeadlessFixture godot)
{
    private MachineWorld World(Vector3 rotation)
    {
        var world = new MachineWorld { Gravity=0,Pressure=0 };
        godot.Tree.Root.AddChild(world);
        world.AddPart(new() { Id="detector",Kind="ball_detector",Position=[0,8,0],
            Rotation=[rotation.X,rotation.Y,rotation.Z] });
        world.AddPart(new() { Id="timer",Kind="hold_timer",Position=[6,8,0] });
        Assert.True(world.Connect(world.FindPart("detector")!,world.FindPart("timer")!));
        return world;
    }
    [Theory]
    [InlineData(4f,0,0,0)]
    [InlineData(40f,0,0,0)]
    [InlineData(120f,0,0,0)]
    [InlineData(40f,20,30,40)]
    [InlineData(40f,0,0,-90)]
    public void ForwardPassEmitsOnceWithoutChangingMomentumAndResetClears(float speed,float x,float y,float z)
    {
        var world = World(new(x,y,z));
        try
        {
            var detector=(BallDetectorPart)world.FindPart("detector")!;
            var ball=world.AddPart(new() { Id="ball",Kind="ball",Position=[0,12,0] });
            ball.Position=detector.Transform*new Vector3(-2,0,0);
            world.Start();
            ball.Velocity=detector.Basis.X*speed;
            var integratedSpeed=Mathf.Min(speed,40); // Existing world safety cap, not detector drag.
            var steps=(int)Math.Ceiling(4/(integratedSpeed*MachineWorld.Tick));
            for(var i=0;i<steps;i++)world.Step();
            Assert.Equal(1,detector.CrossingCount);
            Assert.Equal(HoldTimerState.Holding,((HoldTimerPart)world.FindPart("timer")!).State);
            Assert.InRange(ball.Velocity.Length(),integratedSpeed-.002f,integratedSpeed+.002f);
            Assert.True((detector.Transform.AffineInverse()*ball.Position).X>1.9f);
            world.Restore();
            detector=(BallDetectorPart)world.FindPart("detector")!;
            Assert.Equal(0,detector.CrossingCount);
            Assert.Equal(0,detector.Pulse);
            Assert.False(detector.Active);
            Assert.Equal(HoldTimerState.Ready,((HoldTimerPart)world.FindPart("timer")!).State);
            Assert.Single(world.Connections);
        }
        finally {world.Free();}
    }
    [Theory]
    [InlineData(2f,0f,-4f)]
    [InlineData(-2f,1.4f,4f)]
    [InlineData(0f,0f,0f)]
    public void ReverseOutsideAndStationaryBodiesDoNotTrigger(float x,float y,float speed)
    {
        var world=World(Vector3.Zero);
        try
        {
            var detector=(BallDetectorPart)world.FindPart("detector")!;
            var ball=world.AddPart(new() { Id="ball",Kind="ball",Position=[x,8+y,0] });
            world.Start();ball.Velocity=Vector3.Right*speed;
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(0,detector.CrossingCount);
            Assert.Equal(HoldTimerState.Ready,((HoldTimerPart)world.FindPart("timer")!).State);
        }
        finally {world.Free();}
    }
    [Fact]
    public void PlaneJitterDoesNotRetriggerUntilWholeBallClearsUpstream()
    {
        var world=World(Vector3.Zero);
        try
        {
            var detector=(BallDetectorPart)world.FindPart("detector")!;
            var ball=world.AddPart(new() { Id="ball",Kind="ball",Position=[-1,8,0] });
            // Isolated swept-aperture fixture: no integration, explicit before/after poses.
            void Sweep(float from,float to)
            {
                ball.Position=new(from,8,0);detector.BeforeStep(world,MachineWorld.Tick);
                ball.Position=new(to,8,0);detector.AfterStep(world,MachineWorld.Tick);
            }
            Sweep(-1,1);Assert.Equal(1,detector.CrossingCount);
            for(var i=0;i<20;i++){Sweep(.01f,-.01f);Sweep(-.01f,.01f);}
            Assert.Equal(1,detector.CrossingCount);
            Sweep(1,-1);Assert.Equal(1,detector.CrossingCount);
            Sweep(-1,1);Assert.Equal(2,detector.CrossingCount);
            for(var i=0;i<60;i++)detector.AfterStep(world,MachineWorld.Tick);
            Assert.Equal(0,detector.Pulse);
            Assert.False(detector.Active);
        }
        finally {world.Free();}
    }
    [Fact]
    public void TwoSeparatedBallsProduceTwoEventsAndInvisibleBodiesDoNotArm()
    {
        var world=World(Vector3.Zero);
        try
        {
            var detector=(BallDetectorPart)world.FindPart("detector")!;
            var first=world.AddPart(new() {Id="a",Kind="ball",Position=[-1,8,0]});
            var second=world.AddPart(new() {Id="b",Kind="ball",Position=[-3,8,0]});
            var hidden=world.AddPart(new() {Id="hidden",Kind="ball",Position=[-2,8,0]});
            hidden.Visible=false;
            world.Start();first.Velocity=second.Velocity=Vector3.Right*4;
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(2,detector.CrossingCount);
        }
        finally {world.Free();}
    }
}
