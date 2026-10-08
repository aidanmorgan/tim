using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PlanarGuideTests
{
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
    private static PlanarGuideLoad Load(PhysicsBody body,PhysicsBody frame,double acceleration)=>
        new(body.Id,frame.Id,.5,1.5,1.1,1.1,acceleration,0);

    private static BodyWrench Evaluate(PlanarGuideLoad load,params PhysicsBody[] participants)
    {
        var bodies=participants.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(entry=>entry.Key,entry=>new PhysicsColliderUpdate(entry.Key,
            Object(entry.Value).Geometry,new(0,0,0),CollisionParticipation.Enabled));
        return load.Evaluate(bodies,colliders);
    }

    [Theory]
    [InlineData(0d,0d,-1d)]
    [InlineData(.7,2d,-1d)]
    [InlineData(.7,2d,1d)]
    public void GuidanceUsesLocalBoundsAndRelativeFrameVelocity(double angle,double frameSpeed,double vertical)
    {
        var rotation=RigidRotation.FromRotationVector(new(0,0,angle));
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,new(default,rotation),
            rotation.Apply(new(0,frameSpeed,0)),new(0,0,.3));
        var center=frame.Pose.TransformPoint(new(.5,1,.25));
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(center),
            frame.PointVelocity(center)+rotation.Apply(new(0,vertical,0)),default,2,new(1,1,1));
        var wrench=Evaluate(Load(body,frame,12),body,frame);
        var local=rotation.Inverse().Apply(wrench.Force)*body.InverseMass;
        var expected=vertical>0?default:new CollisionVector(-6,0,-3);
        Assert.InRange((local-expected).Length,0,1e-12);
        Assert.Equal(default(CollisionVector),wrench.Torque);
        Assert.Equal(default(BodyWrench),Evaluate(Load(body,frame,0),body,frame));
    }

    [Theory]
    [InlineData(0,1)]
    [InlineData(12,1)]
    [InlineData(12,4)]
    public void SharedGuideIsBoundedAndRestoresItsDeclaration(double acceleration,double mass)
    {
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,new(0,2,0),default);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(.8,1,0)),new(0,1,0),default,mass,new(1,1,1));
        var load=Load(body,frame,acceleration);
        var world=new PhysicsWorld([],[Object(body),Object(frame)],[],new(default,maximumStep:.001));
        world.ReplaceLoads(world.Loads with {Guides=[load]});var initial=world.Capture();
        void Run()
        {
            for(var i=0;i<20;i++)
            {
                var prior=body.LinearVelocity;world.Step([],[],.001);
                Assert.InRange((body.LinearVelocity-prior).Length,0,acceleration*.001+1e-10);
                Assert.Equal(1,body.LinearVelocity.Y);Assert.Equal(0,body.LinearVelocity.Z);
            }
            Assert.Equal(acceleration>0,body.LinearVelocity.X<0);
        }
        Run();var final=world.Capture().BodyStates.ToArray();
        world.ReplaceLoads(world.Loads with {Guides=[]});Assert.Empty(world.Loads.Guides.ToArray());
        world.Restore(initial);Assert.Same(load,Assert.Single(world.Loads.Guides.ToArray()));
        Run();Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Guides=[new(new(99),frame.Id,.5,1.5,1.1,1.1,12,0)]}));
        Assert.Same(load,Assert.Single(world.Loads.Guides.ToArray()));
    }

    [Fact]
    public void BoundsAndMagnitudeAreEnforced()
    {
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        foreach(var at in new[]{new CollisionVector(1.2,1,0),new(0,1,1.2),new(.5,.49,0),new(.5,1.51,0)})
        {
            var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(at),default,default,1,new(1,1,1));
            Assert.Equal(default(BodyWrench),Evaluate(Load(body,frame,12),body,frame));
        }
        var corner=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(1,1,1)),default,default,3,new(1,1,1));
        var force=Evaluate(Load(corner,frame,12),corner,frame).Force;
        Assert.InRange(Math.Abs(force.Length*corner.InverseMass-12),0,1e-12);
        Assert.Throws<ArgumentException>(()=>new PlanarGuideLoad(new(0),new(0),0,1,1,1,1,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PlanarGuideLoad(new(0),new(1),1,0,1,1,1,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PlanarGuideLoad(new(0),new(1),0,1,0,1,1,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PlanarGuideLoad(new(0),new(1),0,1,1,1,-1,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PlanarGuideLoad(new(0),new(1),0,1,1,1,double.NaN,0));
    }
}
