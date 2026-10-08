using Godot;
using CuriousContraptions.Physics;
using System.Text.Json;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class PoweredGateTests(NativeSceneFixture godot)
{

    [Fact]
    public void BladeControllerUsesLiveGuideAndRejectsDetachedGuide()
    {
        var world=World();
        try
        {
            var gate=(PoweredGatePart)Find(world,Fixture.Gate);
            var saved=Saved(world);
            world.Start();
            var key=new SceneJointKey(gate,PoweredGatePart.BladeGuide);
            var original=Assert.IsType<PhysicsFrameJoint>(world.CurrentJoint(key));
            var initial=world.Physics.Capture();
            var shifted=new PhysicsFrameJoint(original.Id,original.Kind,original.A,original.LocalA,
                original.B,new(original.LocalB.Anchor+new CollisionVector(0,-.25,0),original.LocalB.Orientation),
                original.Collision,original.TravelRange,original.Direction);
            world.Physics.ReplaceJoints([shifted]);
            Assert.Same(shifted,world.CurrentJoint(key));
            gate.ObservePhysics(world,MachineWorld.Tick);
            Assert.InRange(Math.Abs(gate.Opening-.25),0,1e-7);
            gate.PreparePhysics(world,MachineWorld.Tick);
            var replacement=world.Physics.Capture();
            // Removing connected-body suppression requires disabling the blade's
            // overlapping closed geometry in the same authored test transition.
            var collider=world.Physics.Collider(original.A.Id).Declaration;
            world.Physics.ApplyColliderUpdates([new(original.A.Id,collider.Geometry,collider.Material,
                CollisionParticipation.Disabled)]);
            world.Physics.ReplaceJoints([]);
            Assert.Throws<InvalidOperationException>(()=>gate.ObservePhysics(world,MachineWorld.Tick));
            Assert.Throws<InvalidOperationException>(()=>gate.PreparePhysics(world,MachineWorld.Tick));
            world.Physics.Restore(replacement);
            gate.ObservePhysics(world,MachineWorld.Tick);
            Assert.InRange(Math.Abs(gate.Opening-.25),0,1e-7);
            world.Physics.Restore(initial);
            gate.ObservePhysics(world,MachineWorld.Tick);
            Assert.Equal(0,gate.Opening);
            world.Restore();
            Assert.Equal(saved,Saved(world));
        }
        finally {world.Free();}
    }
    private enum Fixture { Gate, Battery, Timer, Ball }
    private static string Id(Fixture fixture)=>fixture switch
    {
        Fixture.Gate=>"gate",Fixture.Battery=>"battery",Fixture.Timer=>"timer",Fixture.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static string Kind(Fixture fixture)=>fixture switch
    {
        Fixture.Gate=>"powered_gate",Fixture.Battery=>"battery",Fixture.Timer=>"delay",Fixture.Ball=>"ball",
        _=>throw new ArgumentOutOfRangeException(nameof(fixture))
    };
    private static MachinePart Add(MachineWorld world,Fixture fixture,Vector3 at)=>
        world.AddPart(new(){Id=Id(fixture),Kind=Kind(fixture),Position=[at.X,at.Y,at.Z]});
    private static MachinePart Find(MachineWorld world,Fixture fixture)=>
        world.FindPart(Id(fixture))??throw new InvalidOperationException("Required gate fixture is absent.");
    private static string Saved(MachineWorld world)=>JsonSerializer.Serialize(world.Snapshot(),MachineJson.Default.MachineData);
    private MachineWorld World()
    {
        var world = new MachineWorld { Gravity = 0, Pressure = 0 };
        godot.Tree.Root.AddChild(world);
        Add(world,Fixture.Gate,new(0,8,0));
        Add(world,Fixture.Battery,new(5,8,0));
        return world;
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HorizontalGateSupportsThenReleasesSettledCargo(bool powered)
    {
        var world=new MachineWorld();
        godot.Tree.Root.AddChild(world);
        try
        {
            var ball=Add(world,Fixture.Ball,new(0,5,4));
            var battery=Add(world,Fixture.Battery,new(-4.1f,3,0));
            var gate=(PoweredGatePart)Add(world,Fixture.Gate,new(0,3,4));
            gate.RotationDegrees=new(0,0,90);
            var supply=new SupplyControl(world,battery);
            Assert.True(world.Connect(supply.Output,gate));
            var construction=Saved(world);
            world.Start();
            for(var i=0;i<360;i++)world.Step();
            world.PresentFrame(0,1);
            Assert.InRange(ball.Position.Y,3.3999f,3.4002f);
            if(powered)supply.SetAndSettle(SimulationLatchPhase.On);
            try
            {
                for(var i=0;i<180;i++)world.Step();
            }
            catch(Exception error)
            {
                Console.WriteLine($"Horizontal gate failure: tick={world.Ticks}, travel={gate.Opening:R}, speed={gate.BladeSpeed:R}, ball={ball.Position}; {error}");
                throw;
            }
            world.PresentFrame(0,1);
            if(powered)Assert.True(ball.Position.Y<1);
            else Assert.InRange(ball.Position.Y,3.3999f,3.4002f);
            world.Restore();
            Assert.Equal(construction,Saved(world));
        }
        finally{world.Free();}
    }

    [Fact]
    public void ElectricalSupplyOpensSmoothlyAndResetCloses()
    {
        var world = World();
        try
        {
            var gate = (PoweredGatePart)Find(world,Fixture.Gate);
            var timer = Add(world,Fixture.Timer,new(8,8,0));
            Assert.False(world.Connect(timer, gate));
            Assert.True(world.Connect(Find(world,Fixture.Battery), gate));
            Assert.Equal(ConnectionDomain.Electrical, Assert.Single(world.Connections).Type);
            world.Start();
            var previous = 0f;
            for (var i = 0; i < 180; i++)
            {
                world.Step();
                if(gate.BladeSpeed<0||gate.Opening<previous)
                    Console.WriteLine($"Gate endpoint observation: tick={world.Ticks}, travel={gate.Opening:R}, previous={previous:R}, speed={gate.BladeSpeed:R}");
                Assert.InRange(gate.Opening - previous, 0, 2.8f * MachineWorld.Tick + .00001f);
                Assert.InRange(gate.BladeSpeed, 0, 2.8f);
                previous = gate.Opening;
            }
            Assert.Equal(GateState.Open, gate.State);
            Assert.Equal(PoweredGatePart.Stroke, gate.Opening);
            world.Restore();
            gate = (PoweredGatePart)Find(world,Fixture.Gate);
            Assert.Equal(GateState.Closed, gate.State);
            Assert.Equal(0, gate.Opening);
            Assert.Equal(0, gate.BladeSpeed);
            Assert.Single(world.Connections);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(false, 0, 0, 0)]
    [InlineData(true, 0, 0, 0)]
    [InlineData(false, 20, 30, 40)]
    [InlineData(true, 20, 30, 40)]
    public void OnlyOpenGateAllowsBallThrough(bool powered, float x, float y, float z)
    {
        var world = World();
        try
        {
            var gate = (PoweredGatePart)Find(world,Fixture.Gate);
            gate.RotationDegrees = new(x, y, z);
            if (powered) Assert.True(world.Connect(Find(world,Fixture.Battery), gate));
            var ball = Add(world,Fixture.Ball,new(0,12,0));
            ball.Position = gate.Transform * new Vector3(-2, 0, 0);
            // A pre-authored approach gives the powered blade time to open.
            ball.InitialVelocity = gate.Basis.X;
            world.Start();
            for (var i = 0; i < 480; i++) world.Step();
            world.PresentFrame(0,1);
            var local = gate.Transform.AffineInverse() * ball.Position;
            if (powered) Assert.InRange(local.X, 1.99f, 2.01f);
            else Assert.True(local.X < -.39f);
        }
        finally { world.Free(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClosingBladeMeetsAuthoredMovingCargoAndResetReplays(bool close)
    {
        var world=World();
        try
        {
            var supply=new SupplyControl(world,Find(world,Fixture.Battery));
            Assert.True(world.Connect(supply.Output,Find(world,Fixture.Gate)));
            var ball=Add(world,Fixture.Ball,new(-1.5f,8,0));
            ball.InitialVelocity=Vector3.Right*.5f;
            var construction=Saved(world);
            (PhysicsBodySnapshot[] Bodies,bool Impact) Run()
            {
                var gate=(PoweredGatePart)Find(world,Fixture.Gate);
                var cargo=Find(world,Fixture.Ball);
                world.Start();
                supply.SetAndSettle(SimulationLatchPhase.On);
                for(var i=0;i<298;i++)world.Step();
                Assert.True(gate.Opening>cargo.Radius+.72f,"Blade must clear the authored cargo path before closing.");
                var blade=world.PhysicsAssembly.Body(new(gate,PoweredGatePart.BladeBody));
                var moving=world.PhysicsAssembly.Body(new(cargo,MachinePart.RootBody));
                Assert.InRange(moving.Center.X,-.251,-.249);
                Assert.InRange(moving.LinearVelocity.X,.499,.501);
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
                if(close)
                {
                    Console.WriteLine($"Gate cargo observation: tick={world.Ticks}, travel={gate.Opening:R}, speed={gate.BladeSpeed:R}, cargoX={moving.Center.X:R}, cargoY={moving.Center.Y:R}, cargoSpeedX={moving.LinearVelocity.X:R}");
                    // The cargo is trapped between the returning blade and the fixed lower tube.
                    // Finite spring effort must leave the gate blocked, never push through either surface.
                    Assert.Equal(GateState.Blocked,gate.State);
                    Assert.InRange(gate.Opening,.001f,PoweredGatePart.Stroke-.001f);
                    Assert.InRange(Math.Abs(gate.BladeSpeed),0,1e-7);
                    Assert.InRange(moving.LinearVelocity.Length,0,1e-7);
                    var frame=world.PhysicsAssembly.Body(new(gate,MachinePart.RootBody));
                    var contacts=world.Physics.BodyContacts(moving.Id);
                    Assert.Contains(contacts,gap=>gap.B==blade);
                    Assert.Contains(contacts,gap=>gap.B==frame);
                    Assert.All(contacts,gap=>Assert.True(gap.Separation>=-1e-6));
                }
                else Assert.True(gate.Opening>cargo.Radius+.72f);
                return(world.Physics.Capture().BodyStates.ToArray(),contact);
            }
            var first=Run();
            world.Restore();Assert.Equal(construction,Saved(world));
            Assert.Equal(Vector3.Right*.5f,Find(world,Fixture.Ball).InitialVelocity);
            var second=Run();
            Assert.Equal(first.Bodies,second.Bodies);Assert.Equal(first.Impact,second.Impact);
            world.Restore();Assert.Equal(construction,Saved(world));
        }
        finally {world.Free();}
    }
}
