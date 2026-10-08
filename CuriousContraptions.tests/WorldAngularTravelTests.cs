using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class WorldAngularTravelTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,0));
    private static (PhysicsWorld World,PhysicsBody Rotor,PhysicsBody Anchor,PhysicsFrameJoint Joint) Setup()
    {
        var rotor=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,new(0,0,8*Math.Tau),1,new(1,1,1));
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var joint=Joint(FrameJointKind.Hinge,rotor,anchor);
        var fixture=new PhysicsObject(anchor,new CompoundGeometry([new(new ConvexSphere(.1),
            SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,new Vector3(0,0,2))))]),new(0,0,0));
        return (new PhysicsWorld([],[Object(rotor),fixture],[joint],new(default,maximumStep:1)),rotor,anchor,joint);
    }
    private static PhysicsFrameJoint Joint(FrameJointKind kind,PhysicsBody a,PhysicsBody b)=>
        new(new(0),kind,a,Origin,b,Origin,ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
    private static void Near(double expected,double actual)=>Assert.InRange(Math.Abs(expected-actual),0,1e-9);

    [Fact]
    public void CommittedMultiTurnTravelRestoresAndReplaysExactly()
    {
        var (world,rotor,anchor,joint)=Setup();
        var initial=world.Capture();
        Assert.Equal(default,world.AngularTravel(joint.Id));
        world.Step([],[],1);
        var moved=world.Capture();var travel=world.AngularTravel(joint.Id);
        Near(8*Math.Tau,travel.Winding);Near(8*Math.Tau,travel.Distance);
        Assert.Equal(0,travel.DistanceError);
        world.Restore(initial);
        Assert.Equal(default,world.AngularTravel(joint.Id));
        world.Step([],[],1);
        Assert.Equal(travel,world.AngularTravel(joint.Id));
        Assert.Equal(moved.AngularTravelStates.ToArray(),world.Capture().AngularTravelStates.ToArray());
        Assert.Equal(moved.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        world.Restore(initial);world.Restore(moved);
        Assert.Equal(travel,world.AngularTravel(joint.Id));
    }

    [Fact]
    public void ReversalAddsDistanceWhileCancellingWinding()
    {
        var (world,rotor,anchor,joint)=Setup();
        world.Step([],[],.25);
        world.ApplyAngularImpulse(rotor.Id,new(0,0,-16*Math.Tau));
        world.Step([],[],.25);
        var travel=world.AngularTravel(joint.Id);
        Near(0,travel.Winding);Near(4*Math.Tau,travel.Distance);
    }

    [Fact]
    public void ReplacementRetainsIdentityRemovalEndsItsHistoryAndRestoreRecoversIt()
    {
        var (world,rotor,anchor,joint)=Setup();
        world.Step([],[],.125);
        var moved=world.Capture();var travel=world.AngularTravel(joint.Id);
        world.ReplaceJoints([Joint(FrameJointKind.Hinge,rotor,anchor)]);
        Assert.Equal(travel,world.AngularTravel(joint.Id));
        world.ReplaceJoints([]);
        Assert.Throws<ArgumentException>(()=>world.AngularTravel(joint.Id));
        world.ReplaceJoints([Joint(FrameJointKind.Hinge,rotor,anchor)]);
        Assert.Equal(default,world.AngularTravel(joint.Id));
        world.Restore(moved);
        Assert.Equal(travel,world.AngularTravel(joint.Id));
        world.ReplaceJoints([Joint(FrameJointKind.Slider,rotor,anchor)]);
        Assert.Throws<ArgumentException>(()=>world.AngularTravel(joint.Id));
    }

    [Fact]
    public void UnknownJointIsRejected()
    {
        var (world,rotor,anchor,joint)=Setup();
        Assert.Throws<ArgumentException>(()=>world.AngularTravel(new(99)));
    }

    private sealed record EffectState(int Count):PhysicsImpactEffectState;
    private sealed class Failure(PhysicsBodyId owner,Action observed):PhysicsImpactEffect(owner)
    {
        private EffectState _state=new(0);
        public override PhysicsImpactEffectState Capture()=>_state;
        public override void Restore(PhysicsImpactEffectState state)=>_state=(EffectState)state;
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            _state=new(_state.Count+1);observed();
            throw new InvalidOperationException("Deliberate post-motion failure.");
        }
    }

    [Fact]
    public void FailureAfterAcceptedMotionRestoresTravelBodiesAndClock()
    {
        var rotor=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,4,0)),default,new(0,0,10),1,new(1,1,1));
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,rotor.Pose,default,default);
        var joint=Joint(FrameJointKind.Hinge,rotor,anchor);
        var ball=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(-2,0,0)),new(10,0,0),default,1,new(1,1,1));
        var obstacle=new PhysicsBody(new(3),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var reached=false;
        var effect=new Failure(obstacle.Id,()=>{reached=true;Assert.True(Math.Abs(joint.Motion.Coordinate)>.1);});
        var world=new PhysicsWorld([effect],[Object(rotor),Object(anchor),Object(ball),Object(obstacle)],[joint],
            new(default,maximumStep:1));
        var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.3));
        Assert.True(reached);
        Assert.Equal(default,world.AngularTravel(joint.Id));
        Assert.Equal(before.AngularTravelStates.ToArray(),world.Capture().AngularTravelStates.ToArray());
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(0,world.Time);Assert.Equal(0ul,world.StepIndex);
    }
}
