using Godot;
using CuriousContraptions.Physics;
using twodog.Testing;
using twodog.Testing.Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollection>]
public class WorldFlightTests(NativeSceneFixture godot)
{
    private enum Role { First, Second, Wall }
    private enum Kind { Ball, Wall }
    private static string Id(Role role)=>role switch
    {
        Role.First=>"first",Role.Second=>"second",Role.Wall=>"wall",
        _=>throw new ArgumentOutOfRangeException(nameof(role))
    };
    private static string Catalog(Kind kind)=>kind switch
    {
        Kind.Ball=>"ball",Kind.Wall=>"wall",
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static Vector3 Position(MachineWorld world,MachinePart part)=>
        WorldGeometry.CaptureSpatialState(world,new(part,MachinePart.RootBody)).Pose.ToScene().Origin;
    private enum BodyParameter { Radius, Bounce }
    private MachineWorld World()
    {
        var world=new MachineWorld { Gravity=0, Pressure=0 };
        godot.Tree.Root.AddChild(world);
        return world;
    }
    private static MachinePart Ball(MachineWorld world,Role role,Vector3 position,Vector3 velocity)
    {
        var body=world.AddPart(new() {Id=Id(role),Kind=Catalog(Kind.Ball),
            Position=[position.X,position.Y,position.Z],
            Properties=new() {[PartParameterName.Of(BodyParameter.Radius)]=.005f,[PartParameterName.Of(BodyParameter.Bounce)]=1}});
        body.InitialVelocity=velocity;
        return body;
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(47f)]
    [InlineData(90f)]
    public void ThinWallImpactUsesRemainingTimeInWorldStep(float degrees)
    {
        var world=World();
        try
        {
            var pose=new Transform3D(new Basis(Vector3.Up,Mathf.DegToRad(degrees)),new(0,5,0));
            var wall=world.AddPart(new(){Id=Id(Role.Wall),Kind=Catalog(Kind.Wall),Position=[0,5,0],Orientation = PartOrientation.FromEulerDegrees(0,degrees,0)});
            wall.Boxes.Clear();
            wall.Boxes.Add(new(Vector3.Zero,new(.001f,1,1), MachinePart.RootBody));
            var ball=Ball(world,Role.First,pose*new Vector3(-.04f,0,0),pose.Basis*Vector3.Right*40);
            world.Start();world.Step();
            var local=pose.AffineInverse()*Position(world,ball);
            Assert.True(local.X<-.05f,"The ball must rebound and consume remaining flight time.");
            Assert.True(CollisionVector.Dot(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity,SceneGeometryAdapter.CaptureVector(pose.Basis.X))<0);
            Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length,0,40.001f);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CrossingMovingBodiesBounceWithoutPassingThroughEachOther(bool reverse)
    {
        var world=World();
        try
        {
            var a=Ball(world,reverse?Role.Second:Role.First,new(-.04f,5,0),Vector3.Right*40);
            var b=Ball(world,reverse?Role.First:Role.Second,new(.04f,5,0),Vector3.Left*40);
            world.Start();
            world.Step();
            Assert.InRange(Position(world,a).X,-.304f,-.302f);
            Assert.InRange(Position(world,b).X,.302f,.304f);
            Assert.InRange(world.PhysicsAssembly.Body(new(a,MachinePart.RootBody)).LinearVelocity.X,-40.001f,-39.999f);
            Assert.InRange(world.PhysicsAssembly.Body(new(b,MachinePart.RootBody)).LinearVelocity.X,39.999f,40.001f);
            Assert.InRange((world.PhysicsAssembly.Body(new(a,MachinePart.RootBody)).LinearVelocity+world.PhysicsAssembly.Body(new(b,MachinePart.RootBody)).LinearVelocity).Length,0,.0001f);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepeatedRicochetsRemainInsideThinWallsWithoutEnergyGain(bool reverse)
    {
        var world=World();
        try
        {
            const float wallCentre=.02f, halfThickness=.001f;
            foreach(var side in new[] {-1,1})
            {
                var wall=world.AddPart(new(){Id=(side<0)^reverse?Id(Role.First):Id(Role.Second),Kind=Catalog(Kind.Wall),
                    Position=[side*wallCentre,5,0]});
                wall.Boxes.Clear();
                wall.Boxes.Add(new(Vector3.Zero,new(halfThickness,1,1), MachinePart.RootBody));
            }
            var ball=Ball(world,Role.Wall,new(0,5,0),Vector3.Right*40);
            world.Start();
            var limit=wallCentre-halfThickness-ball.Radius;
            var observedImpacts=0;
            for(var tick=0;tick<120;tick++)
            {
                world.Step();
                observedImpacts+=world.TickImpacts.Length;
                Assert.InRange(world.TickImpacts.Length,11,12);
                if(tick==0)
                {
                    // Analytic triangular flight between the shared contact
                    // margins; the retired resolver collided at zero separation.
                    var contactLimit=(double)wallCentre-halfThickness-ball.Radius-ConvexSweep.ContactDistance;
                    var phase=(40d*MachineWorld.Tick+contactLimit)%(4*contactLimit);
                    var expected=phase<=2*contactLimit?phase-contactLimit:3*contactLimit-phase;
                    Assert.Equal(12,world.TickImpacts.Length);
                    Assert.InRange(Math.Abs(Position(world,ball).X-expected),0,48*ConvexDistance.DefaultTolerance);
                }
                Assert.InRange(Position(world,ball).X,-limit-.0001f,limit+.0001f);
                Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length,39.999f,40.001f);
                Assert.Equal(5f,Position(world,ball).Y);
                Assert.Equal(0f,Position(world,ball).Z);
            }
            // Sampling only end positions aliases near-integer bounce periods;
            // count actual continuous impacts and verify all requested time ran.
            Assert.True(observedImpacts>1400);
            Assert.InRange(Math.Abs(world.Physics.Time-120d*MachineWorld.Tick),0,1e-10);
        }
        finally{world.Free();}
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SimultaneousPerpendicularWallsResolveBothNormals(bool reverse)
    {
        var world=World();
        try
        {
            var xWall=world.AddPart(new(){Id=Id(reverse?Role.Second:Role.First),Kind=Catalog(Kind.Wall),Position=[0,5,0]});
            var zWall=world.AddPart(new(){Id=Id(reverse?Role.First:Role.Second),Kind=Catalog(Kind.Wall),Position=[0,5,0]});
            xWall.Boxes.Clear();zWall.Boxes.Clear();
            xWall.Boxes.Add(new(Vector3.Zero,new(.001f,1,1), MachinePart.RootBody));
            zWall.Boxes.Add(new(Vector3.Zero,new(1,1,.001f), MachinePart.RootBody));
            var ball=Ball(world,Role.Wall,new(-.04f,5,-.04f),new Vector3(1,0,1).Normalized()*40);
            world.Start();
            world.Step();
            Assert.True(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.X<0 && world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Z<0);
            Assert.True(Position(world,ball).X<-.05f && Position(world,ball).Z<-.05f);
            Assert.InRange(world.PhysicsAssembly.Body(new(ball,MachinePart.RootBody)).LinearVelocity.Length,39.8f,40.001f);
            Assert.InRange(Mathf.Abs(Position(world,ball).X-Position(world,ball).Z),0,.002f);
        }
        finally{world.Free();}
    }

    public enum ContactEffect { Redirect, DisablePayload, OpenPath }
    private sealed record EffectState(int Calls,double IncomingSpeed):PhysicsImpactEffectState;
    private sealed class ContactProbe(PhysicsObject probe,PhysicsObject payload,PhysicsObject gate,ContactEffect effect)
        :PhysicsImpactEffect(probe.Body.Id)
    {
        public EffectState State {get;private set;}=new(0,0);
        public override PhysicsImpactEffectState Capture()=>State;
        public override void Restore(PhysicsImpactEffectState state)=>State=state is EffectState saved
            ?saved:throw new ArgumentException("Foreign effect state.");
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            State=new(State.Calls+1,context.ApproachSpeed);
            var body=context.A.After.Id==payload.Body.Id?context.A:context.B;
            var velocity=effect switch
            {
                ContactEffect.Redirect=>new CollisionVector(0,0,10),
                ContactEffect.DisablePayload or ContactEffect.OpenPath=>new CollisionVector(40,0,0),
                _=>throw new ArgumentOutOfRangeException(nameof(effect))
            };
            PhysicsColliderUpdate Disable(PhysicsObject item)=>
                new(item.Body.Id,item.Geometry,item.Material,CollisionParticipation.Disabled);
            PhysicsColliderUpdate[] changes=effect switch
            {
                ContactEffect.Redirect=>[],
                ContactEffect.DisablePayload=>[Disable(payload)],
                ContactEffect.OpenPath=>[Disable(probe),Disable(gate)],
                _=>throw new ArgumentOutOfRangeException(nameof(effect))
            };
            return new([new(body.After.Id,(velocity-body.After.LinearVelocity)/body.InverseMass,
                body.After.Pose.Center)],changes,[]);
        }
    }
    private static PhysicsObject FlightBall(int id,CollisionVector position)=>new(
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(position),new(40,0,0),default,1,new(.00001,.00001,.00001)),
        new([new(new ConvexSphere(.005),AffineTransform.Identity)]),new(1,0,0));
    private static PhysicsObject FlightWall(int id,CollisionVector position,CollisionVector half)=>new(
        new(new(id),PhysicsMotionType.Static,RigidPose.At(position),default,default),
        new([new(new ConvexBox(half),AffineTransform.Identity)]),new(1,0,0));

    [Theory]
    [InlineData(ContactEffect.Redirect)]
    [InlineData(ContactEffect.DisablePayload)]
    public void SharedImpactCommandsControlRemainingFlightAndReplayExactly(ContactEffect effect)
    {
        var ball=FlightBall(0,new(-.04,5,0));
        var probe=FlightWall(1,new(0,5,0),new(.001,1,1));
        var gate=FlightWall(2,new(.05,5,0),new(.001,1,1));
        var response=new ContactProbe(probe,ball,gate,effect);
        var world=new PhysicsWorld([response],[ball,probe,gate],[],new(default));
        var before=world.Capture();
        void Verify()
        {
            world.Step([],[],MachineWorld.Tick);
            Assert.Equal(1,response.State.Calls);
            Assert.InRange(response.State.IncomingSpeed,39.999,40.001);
            if(effect==ContactEffect.Redirect)
            {
                Assert.InRange(ball.Body.Center.X,-.0062,-.0058);
                Assert.InRange(ball.Body.Center.Z,.07,.08);
                Assert.Equal(new CollisionVector(0,0,10),ball.Body.LinearVelocity);
                Assert.Equal(CollisionParticipation.Enabled,world.Collider(ball.Body.Id).Declaration.Participation);
            }
            else
            {
                // Collision removal is explicit; an invisible scene node is no
                // longer a second simulation authority or a way to discard time.
                Assert.InRange(ball.Body.Center.X,.292,.294);
                Assert.Equal(new CollisionVector(40,0,0),ball.Body.LinearVelocity);
                Assert.Equal(CollisionParticipation.Disabled,world.Collider(ball.Body.Id).Declaration.Participation);
            }
        }
        Verify();
        var after=world.Capture(); var state=response.State;
        world.Restore(before);
        Assert.Equal(0,response.State.Calls);
        Assert.Equal(CollisionParticipation.Enabled,world.Collider(ball.Body.Id).Declaration.Participation);
        Verify();
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(state,response.State);
    }

    [Fact]
    public void ZeroTimeSharedCommandInvalidatesGeometryForEveryBody()
    {
        var first=FlightBall(0,new(-.006,5,0));
        var second=FlightBall(1,new(-.04,5,.5));
        var probe=FlightWall(2,new(0,5,0),new(.001,.1,.1));
        var gate=FlightWall(3,new(.05,5,0),new(.001,1,1));
        var response=new ContactProbe(probe,first,gate,ContactEffect.OpenPath);
        var world=new PhysicsWorld([response],[first,second,probe,gate],[],new(default));
        var before=world.Capture();
        world.Step([],[],MachineWorld.Tick);
        Assert.Equal(1,response.State.Calls);
        Assert.InRange(first.Body.Center.X,.326,.328);
        Assert.InRange(second.Body.Center.X,.292,.294);
        Assert.Equal(new CollisionVector(40,0,0),first.Body.LinearVelocity);
        Assert.Equal(new CollisionVector(40,0,0),second.Body.LinearVelocity);
        Assert.Equal(CollisionParticipation.Disabled,world.Collider(probe.Body.Id).Declaration.Participation);
        Assert.Equal(CollisionParticipation.Disabled,world.Collider(gate.Body.Id).Declaration.Participation);
        var after=world.Capture();
        world.Restore(before); world.Step([],[],MachineWorld.Tick);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(1,response.State.Calls);
    }

    [Fact]
    public void ImpossibleNarrowGapRejectsBeforeRunning()
    {
        var world=World();
        try
        {
            foreach(var side in new[]{-1,1})
            {
                var wall=world.AddPart(new(){Id=side<0?Id(Role.First):Id(Role.Second),Kind=Catalog(Kind.Wall),
                    Position=[side*.0045f,5,0]});
                wall.Boxes.Clear();wall.Boxes.Add(new(Vector3.Zero,new(.001f,1,1), MachinePart.RootBody));
            }
            Ball(world,Role.Wall,new(0,5,0),Vector3.Zero);
            Assert.Throws<ScenePhysicsOverlapException>(world.Start);
            Assert.False(world.Running);
            Assert.Equal(0,world.Ticks);
        }
        finally{world.Free();}
    }

    [Fact]
    public void InitialOverlapRejectsWithoutMutatingConstruction()
    {
        var world=World();
        try
        {
            var a=Ball(world,Role.First,new(0,5,0),Vector3.Zero);
            var b=Ball(world,Role.Second,new(.004f,5,0),Vector3.Zero);
            Assert.Throws<ScenePhysicsOverlapException>(world.Start);
            Assert.False(world.Running);
            Assert.Equal(Vector3.Zero,a.InitialVelocity);Assert.Equal(Vector3.Zero,b.InitialVelocity);
            Assert.Throws<InvalidOperationException>(()=>world.Physics);
            Assert.Equal(new Vector3(0,5,0),world.FindPart(Id(Role.First))!.Position);
            Assert.Equal(new Vector3(.004f,5,0),world.FindPart(Id(Role.Second))!.Position);
        }
        finally{world.Free();}
    }
    [Fact]
    public void FixtureBoundariesRejectUndefinedChoices()
    {
        Assert.Equal("first",Id(Role.First)); Assert.Equal("ball",Catalog(Kind.Ball));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Id((Role)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Catalog((Kind)999));
    }
}
