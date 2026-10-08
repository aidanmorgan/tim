using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class DrivenSurfaceWorldTests
{
    private enum Participant { Payload, Carrier, Shaft }
    public enum Face { Top, Side, Underside }
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static readonly ConvexInstance Sphere=new(new ConvexSphere(.5),AffineTransform.Identity);
    private static readonly ConvexInstance Floor=new(new ConvexBox(new(10,.5,10)),AffineTransform.Identity);
    private static PhysicsBody Payload(CollisionVector center,CollisionVector velocity=default)=>
        new(new((int)Participant.Payload),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,default,1,new(.1,.1,.1));
    private static (PhysicsBody Carrier,PhysicsBody Shaft,PhysicsFrameJoint Joint,DrivenSurface Surface) Drive(double speed=4)
    {
        var carrier=new PhysicsBody(new((int)Participant.Carrier),PhysicsMotionType.Static,RigidPose.At(-Y*.5),default,default);
        var shaft=new PhysicsBody(new((int)Participant.Shaft),PhysicsMotionType.Dynamic,carrier.Pose,default,Z*speed,1,new(1,1,1));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,shaft,Origin,carrier,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        return(carrier,shaft,joint,new(joint,Y,X,1));
    }
    private static PhysicsWorld World(PhysicsBody payload,PhysicsBody carrier,PhysicsBody shaft,PhysicsFrameJoint joint)=>
        new([],[new(payload,new([Sphere]),new(0,0,.5)),new(carrier,new([Floor]),new(0,0,.5)),
            new(shaft,new([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,.5))],[joint],new(-Y*9.8,maximumStep:.01));
    private static void Near(double expected,double actual,double tolerance=1e-7)=>Assert.InRange(Math.Abs(expected-actual),0,tolerance);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DrivenSurfaceTransfersStoredShaftEnergyThroughActualWorldContactAndReplays(bool driven)
    {
        var rig=Drive(); var load=Payload(Y*.5); var world=World(load,rig.Carrier,rig.Shaft,rig.Joint);
        world.ReplaceSurfaces(driven?[rig.Surface]:[]);
        var initial=world.Capture();
        void Run()
        {
            for(var i=0;i<10;i++) world.Step([],[],.01);
            Near(.5,load.Center.Y); Near(0,load.LinearVelocity.Y);
            Near(driven?.49:0,load.LinearVelocity.X,2e-6);
            Near(driven?2.45:0,load.AngularVelocity.Z,2e-6);
            Near(driven?3.51:4,rig.Shaft.AngularVelocity.Z,2e-6);
            Assert.InRange(load.KineticEnergy+rig.Shaft.KineticEnergy,0,8+1e-7);
            Near(.1,world.Time);
        }
        Run(); var after=world.Capture();
        world.ReplaceSurfaces([]);
        world.Restore(initial);
        Assert.Equal(driven?1:0,world.Surfaces.Length);
        Run(); Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(1d)]
    public void SustainedSlipEndsWithoutReversalAndDoesNotCreateEnergy(double direction)
    {
        var rig=Drive(direction*4); var load=Payload(Y*.5); var world=World(load,rig.Carrier,rig.Shaft,rig.Joint);
        world.ReplaceSurfaces([rig.Surface]); var energy=8d;
        var events=0;
        for(var i=0;i<30;i++)
        {
            events+=world.Step([],[],.01).Events;
            var slip=load.LinearVelocity.X+.5*load.AngularVelocity.Z-rig.Shaft.AngularVelocity.Z;
            Assert.InRange(direction*slip,-4-1e-7,1e-7);
            var next=load.KineticEnergy+rig.Shaft.KineticEnergy;
            Assert.InRange(next,0,energy+1e-7); energy=next;
        }
        Near(direction*8d/9,load.LinearVelocity.X,2e-6);
        Near(direction*28d/9,rig.Shaft.AngularVelocity.Z,2e-6);
        Assert.True(events>0);
    }

    [Theory]
    [InlineData(Face.Top)]
    [InlineData(Face.Side)]
    [InlineData(Face.Underside)]
    public void OnlyTheAuthoredMaterialFaceTransmitsShaftMotion(Face face)
    {
        var rig=Drive();
        var (center,normal)=face switch
        {
            Face.Top=>(Y*.5,Y),
            Face.Side=>(new CollisionVector(10.5,-.5,0),X),
            Face.Underside=>(-Y*1.5,-Y),
            _=>throw new ArgumentOutOfRangeException(nameof(face))
        };
        var load=Payload(center,-normal);
        var gap=Assert.Single(ContactGap.Query(load,Sphere,rig.Carrier,Floor,.001,1e-9));
        var material=new MaterialContact(gap,[rig.Surface]);
        Assert.Equal(face==Face.Top?1:0,material.Surfaces.Length);
        var contact=new ContactConstraint(material.Kinematics,0,0,.5);
        ImpulseSolver.Solve([contact]);
        if(face==Face.Top) Assert.True(rig.Shaft.AngularVelocity.Z<4);
        else Near(4,rig.Shaft.AngularVelocity.Z);
    }

    [Fact]
    public void ShaftOnlySlipReversalIsCertifiedByTheContinuousSweep()
    {
        var rig=Drive(1); var load=Payload(Y*.5);
        var gap=Assert.Single(ContactGap.Query(load,Sphere,rig.Carrier,Floor,.001,1e-9));
        var material=new MaterialContact(gap,[rig.Surface]);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>
        {
            [load.Id]=load.CreateTrajectory(1,default),
            [rig.Carrier.Id]=rig.Carrier.CreateTrajectory(1,default),
            [rig.Shaft.Id]=rig.Shaft.CreateTrajectory(1,new(default,-Z*2))
        };
        var path=new ContactSlipPath(material,paths);
        Near(-1,path.At(0).Slip.X); Near(1,path.At(1).Slip.X);
        var hit=ContactSlipSweep.Cast(path,1,1e-8);
        Assert.Equal(ContactSlipStatus.Boundary,hit.Status); Near(.5,hit.Time,1e-7);
        var sample=path.At(.1);
        const double h=1e-5;
        var finite=(path.At(.1+h).Slip-path.At(.1-h).Slip)/(2*h);
        Near(0,(sample.Derivative-finite).Length,1e-7);
        var end=path.SegmentEndAfter(.1);
        var bounds=path.Bounds(.1,end)!.Value;
        Assert.InRange(sample.Derivative.Length,0,bounds.Rate);
    }

    [Fact]
    public void MaterialTrajectoryRequiresTheOwnedShaftAndRejectsStaleShaftState()
    {
        var rig=Drive(1); var load=Payload(Y*.5);
        var gap=Assert.Single(ContactGap.Query(load,Sphere,rig.Carrier,Floor,.001,1e-9));
        var material=new MaterialContact(gap,[rig.Surface]);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>
        {
            [load.Id]=load.CreateTrajectory(.1,default),[rig.Carrier.Id]=rig.Carrier.CreateTrajectory(.1,default)
        };
        Assert.Throws<ArgumentException>(()=>new ContactSlipPath(material,paths));
        paths.Add(rig.Shaft.Id,rig.Shaft.CreateTrajectory(.1,default));
        var path=new ContactSlipPath(material,paths);
        rig.Shaft.ApplyImpulse(X,rig.Shaft.Center+Y);
        Assert.Throws<InvalidOperationException>(()=>path.At(0));
        Assert.Throws<InvalidOperationException>(()=>path.Bounds(0,.01));
    }

    [Fact]
    public void SurfaceDeclarationsRestoreAndRejectForeignDuplicateOrRemovedHinges()
    {
        var rig=Drive(); var load=Payload(Y*.5); var world=World(load,rig.Carrier,rig.Shaft,rig.Joint);
        var empty=world.Capture();
        world.ReplaceSurfaces([rig.Surface]);
        var declared=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.ReplaceSurfaces([rig.Surface,rig.Surface]));
        Assert.Throws<ArgumentException>(()=>world.ReplaceSurfaces([Drive().Surface]));
        Assert.Throws<ArgumentException>(()=>world.ReplaceJoints([]));
        Assert.Same(rig.Surface,Assert.Single(world.Surfaces.ToArray()));
        world.Restore(empty); Assert.Empty(world.Surfaces.ToArray());
        world.Restore(declared); Assert.Same(rig.Surface,Assert.Single(world.Surfaces.ToArray()));
        world.ReplaceSurfaces([]);
        Assert.Throws<InvalidOperationException>(()=>world.ReplaceJoints([]));
        var collider=world.Collider(rig.Shaft.Id).Declaration;
        world.ApplyColliderUpdates([new(rig.Shaft.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
        world.ReplaceJoints([]);
        Assert.Empty(world.Surfaces.ToArray()); Assert.Empty(world.Joints.ToArray());
    }

    [Fact]
    public void MovingCarrierMaterialBiasAndTrajectoryDerivativeMatchFiniteDifferences()
    {
        var carrier=new PhysicsBody(new((int)Participant.Carrier),PhysicsMotionType.Kinematic,
            RigidPose.At(-Y*.5),new(.1,.2,0),new(.2,.5,.4));
        var shaft=new PhysicsBody(new((int)Participant.Shaft),PhysicsMotionType.Dynamic,carrier.Pose,
            carrier.LinearVelocity,carrier.AngularVelocity+Z*4,1,new(1,1,1));
        var load=Payload(Y*.5,new(.3,-.1,.2));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,shaft,Origin,carrier,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var surface=new DrivenSurface(joint,Y,X,.7);
        var gap=Assert.Single(ContactGap.Query(load,Sphere,carrier,Floor,.001,1e-9));
        var material=new MaterialContact(gap,[surface]);
        PhysicsBody At(PhysicsBody body,double time)=>new(body.Id,body.MotionType,
            new(body.Center+body.LinearVelocity*time,RigidRotation.FromRotationVector(body.AngularVelocity*time)*body.Pose.Rotation),
            body.LinearVelocity,body.AngularVelocity,body.MotionType==PhysicsMotionType.Dynamic?1/body.InverseMass:0,
            body.MotionType==PhysicsMotionType.Dynamic?body.LocalInertia:default);
        MaterialContact Moved(double time)=>material.Rebind(new Dictionary<PhysicsBodyId,PhysicsBody>
            {{load.Id,At(load,time)},{carrier.Id,At(carrier,time)},{shaft.Id,At(shaft,time)}});
        const double h=1e-5;
        var rate=(Moved(h).Slip-Moved(-h).Slip)/(2*h);
        rate-=gap.Normal*CollisionVector.Dot(gap.Normal,rate);
        Near(0,(rate-material.TangentBias).Length,1e-7);
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>
        {
            [load.Id]=load.CreateTrajectory(.01,default),[carrier.Id]=carrier.CreateTrajectory(.01,default),
            [shaft.Id]=shaft.CreateTrajectory(.01,new(default,new(.2,-.3,.7)))
        };
        var path=new ContactSlipPath(material,paths);
        var sample=path.At(.005);
        var numerical=(path.At(.005+h).Slip-path.At(.005-h).Slip)/(2*h);
        Near(0,(sample.Derivative-numerical).Length,1e-6);
        var bounds=path.Bounds(0,.01)!.Value;
        for(var i=0;i<=10;i++) Assert.InRange(path.At(i*.001).Derivative.Length,0,bounds.Rate);
    }

    private sealed record FailureState(int Calls):PhysicsImpactEffectState;
    private sealed class RejectImpact(PhysicsBodyId owner):PhysicsImpactEffect(owner)
    {
        public int Calls { get; private set; }
        public override PhysicsImpactEffectState Capture()=>new FailureState(Calls);
        public override void Restore(PhysicsImpactEffectState state)=>Calls=((FailureState)state).Calls;
        public override PhysicsImpactCommands OnImpact(PhysicsImpactContext context)
        {
            Calls++;
            throw new InvalidOperationException("Intentional transaction failure.");
        }
    }

    [Fact]
    public void FailedImpactRestoresShaftContactMapsEffectsAndClock()
    {
        var rig=Drive(); var load=Payload(Y*.6,-Y*3); var failure=new RejectImpact(rig.Carrier.Id);
        var world=new PhysicsWorld([failure],[new(load,new([Sphere]),new(0,0,.5)),new(rig.Carrier,new([Floor]),new(0,0,.5)),
            new(rig.Shaft,new([new(new ConvexSphere(.1),AffineTransform.Identity)]),new(0,0,.5))],[rig.Joint],new(-Y*9.8,maximumStep:.1));
        world.ReplaceSurfaces([rig.Surface]); var before=world.Capture();
        Assert.Throws<InvalidOperationException>(()=>world.Step([],[],.1));
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Same(rig.Surface,Assert.Single(world.Surfaces.ToArray()));
        Assert.Equal(0,failure.Calls); Assert.Equal(0,world.Time); Assert.Equal(0ul,world.StepIndex);
        Assert.Equal(0,world.RetainedContactPairs);
    }

    [Fact]
    public void DrivenContactReportsAreReadOnlyAndIdentifyActualReceiver()
    {
        var rig=Drive(); var load=Payload(Y*.5); var world=World(load,rig.Carrier,rig.Shaft,rig.Joint);
        world.ReplaceSurfaces([rig.Surface]); world.Step([],[],.01);
        var before=world.Capture(); var pairs=world.RetainedContactPairs;
        var report=Assert.Single(world.SurfaceContacts());
        Assert.Equal(rig.Joint.Id,report.Drive); Assert.Equal(load.Id,report.Receiver);
        Assert.Equal(X,report.Direction); Assert.True(report.SurfaceSpeed>0);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.Time,world.Time); Assert.Equal(pairs,world.RetainedContactPairs);
    }

    [Fact]
    public void InvalidSurfaceGeometryAndRatioReject()
    {
        var rig=Drive();
        foreach(var invalid in new[]{0d,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentException>(()=>new DrivenSurface(rig.Joint,Y,X,invalid));
        Assert.Throws<ArgumentException>(()=>new DrivenSurface(rig.Joint,Y,Y,1));
        Assert.Throws<ArgumentException>(()=>new DrivenSurface(rig.Joint,default,X,1));
        var slider=new PhysicsFrameJoint(rig.Joint.Id,FrameJointKind.Slider,rig.Shaft,Origin,rig.Carrier,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        Assert.Throws<ArgumentException>(()=>new DrivenSurface(slider,Y,X,1));
    }
}
