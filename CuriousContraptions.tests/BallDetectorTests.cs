using Godot;
using System.Text.Json;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class BallDetectorTests(NativeSceneFixture godot)
{
    private enum Role { Detector, Timer, Ball, First, Second, Hidden }
    private static string Id(Role role)=>role switch
    {
        Role.Detector=>"detector",Role.Timer=>"timer",Role.Ball=>"ball",
        Role.First=>"a",Role.Second=>"b",Role.Hidden=>"hidden",_=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Kind(Role role)=>role switch
    {
        Role.Detector=>"ball_detector",Role.Timer=>"hold_timer",
        Role.Ball or Role.First or Role.Second or Role.Hidden=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private MachineWorld World(Vector3 rotation)
    {
        var world = new MachineWorld { Gravity=0,Pressure=0 };
        godot.Tree.Root.AddChild(world);
        world.AddPart(new() { Id=Id(Role.Detector),Kind=Kind(Role.Detector),Position=[0,8,0],
            Orientation = PartOrientation.FromEulerDegrees(rotation.X,rotation.Y,rotation.Z) });
        world.AddPart(new() { Id=Id(Role.Timer),Kind=Kind(Role.Timer),Position=[6,8,0] });
        Assert.True(world.Connect(world.FindPart(Id(Role.Detector))!,world.FindPart(Id(Role.Timer))!));
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
            var detector=(BallDetectorPart)world.FindPart(Id(Role.Detector))!;
            var ball=world.AddPart(new() { Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[0,12,0] });
            ball.Position=detector.Transform*new Vector3(-2,0,0);
            ball.InitialVelocity=detector.Basis.X*speed;
            world.Start();
            var integratedSpeed=speed; // Shared physics retains the authored speed; no velocity cap.
            var steps=(int)Math.Ceiling(4/(integratedSpeed*MachineWorld.Tick));
            for(var i=0;i<steps;i++)world.Step();
            Assert.Equal(1,detector.CrossingCount);
            Assert.Equal(SimulationTimerPhase.Counting,((HoldTimerPart)world.FindPart(Id(Role.Timer))!).State);
            Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length,integratedSpeed-.002f,integratedSpeed+.002f);
            Assert.True((detector.Transform.AffineInverse()*ball.Position).X>1.9f);
            world.Restore();
            detector=(BallDetectorPart)world.FindPart(Id(Role.Detector))!;
            Assert.Equal(0,detector.CrossingCount);
            Assert.Equal(0,detector.Pulse);
            Assert.False(detector.Active);
            Assert.Equal(SimulationTimerPhase.Ready,((HoldTimerPart)world.FindPart(Id(Role.Timer))!).State);
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
            var detector=(BallDetectorPart)world.FindPart(Id(Role.Detector))!;
            var ball=world.AddPart(new() { Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[x,8+y,0] });
            ball.InitialVelocity=Vector3.Right*speed;world.Start();
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(0,detector.CrossingCount);
            Assert.Equal(SimulationTimerPhase.Ready,((HoldTimerPart)world.FindPart(Id(Role.Timer))!).State);
        }
        finally {world.Free();}
    }
    [Fact]
    public void PlaneJitterDoesNotRetriggerUntilWholeBallClearsUpstream()
    {
        var world=World(Vector3.Zero);
        try
        {
            var detector=(BallDetectorPart)world.FindPart(Id(Role.Detector))!;
            var ball=world.AddPart(new() { Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[-1,8,0] });
            world.Start();
            var solved=world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody));
            void Speed(double speed)=>world.Physics.ApplyImpulse(solved.Id,
                new CollisionVector(speed-solved.LinearVelocity.X,0,0)/solved.InverseMass,solved.Center);
            void Move(double target,double speed)
            {
                Speed(speed);
                var steps=0;
                while(speed>0?solved.Center.X<target:solved.Center.X>target)
                {
                    Assert.True(++steps<500);
                    world.Step();
                }
            }
            Move(.02,4);Assert.Equal(1,detector.CrossingCount);
            for(var i=0;i<20;i++){Move(-.02,-1);Move(.02,1);}
            Assert.Equal(1,detector.CrossingCount);
            Move(-1,-4);Assert.Equal(1,detector.CrossingCount);
            Move(1,4);Assert.Equal(2,detector.CrossingCount);
            Speed(0);
            for(var i=0;i<60;i++)world.Step();
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
            var detector=(BallDetectorPart)world.FindPart(Id(Role.Detector))!;
            var first=world.AddPart(new() {Id=Id(Role.First),Kind=Kind(Role.Ball),Position=[-1,8,0]});
            var second=world.AddPart(new() {Id=Id(Role.Second),Kind=Kind(Role.Ball),Position=[-3,8,0]});
            var hidden=world.AddPart(new() {Id=Id(Role.Hidden),Kind=Kind(Role.Ball),Position=[-2,8,0]});
            hidden.Visible=false;
            first.InitialVelocity=second.InitialVelocity=Vector3.Right*4;world.Start();
            for(var i=0;i<120;i++)world.Step();
            Assert.Equal(2,detector.CrossingCount);
        }
        finally {world.Free();}
    }

    [Fact]
    public void SharedWorldDetectsWithoutSceneCallbacksAndSnapshotRestoresPhysicalHistory()
    {
        var world=World(Vector3.Zero);
        try
        {
            var detector=(BallDetectorPart)world.FindPart(Id(Role.Detector))!;
            var timer=(HoldTimerPart)world.FindPart(Id(Role.Timer))!;
            var ball=world.AddPart(new(){Id=Id(Role.Ball),Kind=Kind(Role.Ball),Position=[-1,8,0]});
            ball.InitialVelocity=new(4,0,0);
            var construction=JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
            world.Start();var initial=world.Physics.Capture();
            detector.Position=new(20,20,20);detector.Visible=false;detector.Tubes.Clear();
            ball.Position=new(-20,20,20);ball.Visible=false;
            world.Physics.Step([],[],.5);
            Assert.Equal(1,detector.CrossingCount);
            Assert.Equal(0,detector.Pulse);Assert.Equal(SimulationTimerPhase.Ready,timer.State);
            Assert.Single(world.Physics.PassageEvents.ToArray());
            var after=world.Physics.Capture();
            world.Physics.Restore(initial);
            Assert.Equal(0,detector.CrossingCount);Assert.Empty(world.Physics.PassageEvents.ToArray());
            world.Physics.Step([],[],.5);
            Assert.Equal(after.BodyStates.ToArray(),world.Physics.Capture().BodyStates.ToArray());
            Assert.Equal(after.PassageStates.ToArray(),world.Physics.PassageStates.ToArray());
            Assert.Equal(after.PassageEvents.ToArray(),world.Physics.PassageEvents.ToArray());
            detector.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(1,detector.Pulse);Assert.Equal(SimulationTimerPhase.Counting,timer.State);
            var events=world.Events.ToArray();
            detector.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(events,world.Events.ToArray());Assert.Equal(1,detector.Pulse);
            world.Restore();
            Assert.Equal(construction,JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData));
            detector=(BallDetectorPart)world.FindPart(Id(Role.Detector))!;
            Assert.Equal(0,detector.CrossingCount);Assert.Equal(0,detector.Pulse);
        }
        finally {world.Free();}
    }
}
