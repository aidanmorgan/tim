using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class SharedHingeMechanicsTests
{
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsObject Object(PhysicsBody body,ConvexGeometry? shape=null)=>
        new(body,new CompoundGeometry([new(shape??new ConvexSphere(.01),AffineTransform.Identity)]),new(.5,0,0));
    private static (PhysicsBody Body,PhysicsBody Anchor,PhysicsFrameJoint Joint,PhysicsWorld World)
        Rig(double initial=0,double speed=0,double inertia=2,RigidRotation? orientation=null)
    {
        var basis=orientation??RigidRotation.Identity;
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,
            new(default,basis*RigidRotation.FromRotationVector(new(0,0,initial))),default,
            basis.Apply(new(0,0,speed)),1,new(1,1,inertia));
        var anchor=new PhysicsBody(new(1),PhysicsMotionType.Static,new(default,basis),default,default);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,body,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,new(-.6,.6),JointTravelDirection.Both);
        return (body,anchor,joint,new PhysicsWorld([],[Object(body),Object(anchor)],[joint],new(default,maximumStep:1)));
    }
    private static PhysicsBody Payload(CollisionVector center,CollisionVector velocity,double mass)=>
        new(new(2),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,default,mass,new(.1,.1,.1));
    private static ImpulseConstraint Contact(PhysicsBody load,PhysicsBody beam,PhysicsFrameJoint joint,
        CollisionVector point,CollisionVector normal,double restitution)
    {
        var contact=ImpulseConstraint.Contact(load,beam,point,normal,restitution,0);
        ImpulseSolver.Solve([..joint.VelocityConstraints(1e-7),contact]);
        return contact;
    }

    [Theory]
    [InlineData(1,1)]
    [InlineData(2,1)]
    [InlineData(-2,4)]
    public void SignedLeverArmActsOnTheOwnedBody(double arm,double impulse)
    {
        var rig=Rig();
        rig.World.ApplyImpulse(rig.Body.Id,new(0,impulse,0),new(arm,0,0));
        ImpulseSolver.Solve(rig.Joint.VelocityConstraints(1e-7));
        Assert.Equal(arm*impulse/2,rig.Body.AngularVelocity.Z,10);
        Assert.InRange(rig.Body.LinearVelocity.Length,0,1e-10);
        Assert.Equal(.5*2*rig.Body.AngularVelocity.LengthSquared,rig.Body.KineticEnergy,10);
    }

    [Fact]
    public void BalancedLoadsCancelAndOffCentreLoadDoesNot()
    {
        var rig=Rig();
        rig.World.ApplyImpulse(rig.Body.Id,new(0,-9.81/120,0),new(-2,0,0));
        rig.World.ApplyImpulse(rig.Body.Id,new(0,-2*9.81/120,0),new(1,0,0));
        ImpulseSolver.Solve(rig.Joint.VelocityConstraints(1e-7));
        Assert.InRange(rig.Body.AngularVelocity.Length,0,1e-12);
        rig.World.ApplyImpulse(rig.Body.Id,new(0,-9.81/120,0),new(-2,0,0));
        rig.World.ApplyImpulse(rig.Body.Id,new(0,-2*9.81/120,0),new(1.2,0,0));
        ImpulseSolver.Solve(rig.Joint.VelocityConstraints(1e-7));
        Assert.True(rig.Body.AngularVelocity.Z<0);
    }

    [Fact]
    public void RadialAxialAndPivotImpulsesCannotRotateTheHinge()
    {
        var rig=Rig();
        rig.World.ApplyImpulse(rig.Body.Id,new(10,0,0),new(1,0,0));
        rig.World.ApplyImpulse(rig.Body.Id,new(0,10,0),new(0,0,1));
        rig.World.ApplyImpulse(rig.Body.Id,new(0,10,0),default);
        ImpulseSolver.Solve(rig.Joint.VelocityConstraints(1e-7));
        Assert.InRange(rig.Body.KineticEnergy,0,1e-12);
    }

    [Theory]
    [InlineData(1,1,0)]
    [InlineData(1,1,1)]
    [InlineData(4,2,.4)]
    [InlineData(.5,-1.5,.8)]
    public void CoupledContactConservesAngularMomentumAndBoundsEnergy(double mass,double arm,double restitution)
    {
        var rig=Rig();var point=new CollisionVector(arm,0,0);
        var load=Payload(point,new(1,-5,.4),mass);
        var before=load.KineticEnergy;
        var momentum=CollisionVector.Cross(point,load.LinearVelocity*mass).Z;
        var contact=Contact(load,rig.Body,rig.Joint,point,new(0,1,0),restitution);
        var after=load.KineticEnergy+rig.Body.KineticEnergy;
        Assert.InRange(after,0,before+.00002);
        if(restitution==1)Assert.InRange(Math.Abs(after-before),0,.00002);
        Assert.InRange(Math.Abs(CollisionVector.Cross(point,load.LinearVelocity*mass).Z+
            rig.Body.AngularMomentum.Z-momentum),0,.00002);
        Assert.InRange(Math.Abs(contact.Speed-restitution*5),0,.00002);
        Assert.Equal(1,load.LinearVelocity.X);Assert.Equal(.4,load.LinearVelocity.Z);
    }

    [Fact]
    public void WholeSetupRotationPreservesImpulseResponse()
    {
        var rotation=RigidRotation.FromRotationVector(new(.2,.4,.6));
        var original=Rig();var rotated=Rig(orientation:rotation);
        var offset=new CollisionVector(1.2,.1,-.2);var impulse=new CollisionVector(.3,-2,.5);
        original.World.ApplyImpulse(original.Body.Id,impulse,offset);
        rotated.World.ApplyImpulse(rotated.Body.Id,rotation.Apply(impulse),rotation.Apply(offset));
        ImpulseSolver.Solve(original.Joint.VelocityConstraints(1e-7));
        ImpulseSolver.Solve(rotated.Joint.VelocityConstraints(1e-7));
        Assert.InRange((rotation.Apply(original.Body.AngularVelocity)-rotated.Body.AngularVelocity).Length,0,1e-8);
        Assert.InRange((rotation.Apply(original.Body.PointVelocity(offset))-
            rotated.Body.PointVelocity(rotation.Apply(offset))).Length,0,1e-8);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void StopsDissipateOutwardMomentumAndAllowPhysicalRelease(int sign)
    {
        var rig=Rig(speed:sign*2);var initial=rig.World.Capture();
        rig.World.Step([],[],.1);
        Assert.InRange(Math.Abs(rig.Joint.Travel.Error-sign*.2),0,1e-7);
        rig.World.Step([],[],.5);
        Assert.InRange(Math.Abs(rig.Joint.Travel.Error-sign*.6),0,1e-7);
        Assert.InRange(rig.Body.KineticEnergy,0,1e-10);
        rig.World.ApplyImpulse(rig.Body.Id,new(0,sign*10,0),new(1,0,0));
        rig.World.Step([],[],.01);
        Assert.InRange(rig.Body.KineticEnergy,0,1e-10);
        rig.World.ApplyImpulse(rig.Body.Id,new(0,-sign*2,0),new(1,0,0));
        rig.World.Step([],[],.1);
        Assert.InRange(Math.Abs(rig.Joint.Travel.Error-sign*.5),0,1e-7);
        rig.World.Restore(initial);
        Assert.Equal(initial.BodyStates.ToArray(),rig.World.Capture().BodyStates.ToArray());
    }

    [Fact]
    public void FreeFlightPartitionsAndSnapshotRestorePreserveTheSameMotion()
    {
        var whole=Rig(.1,.1);var split=Rig(.1,.1);var initial=split.World.Capture();
        whole.World.Step([],[],.5);
        for(var i=0;i<10;i++)split.World.Step([],[],.05);
        Assert.Equal(whole.Joint.Travel.Error,split.Joint.Travel.Error,10);
        Assert.Equal(whole.Body.KineticEnergy,split.Body.KineticEnergy,10);
        var after=split.World.Capture();
        split.World.Restore(initial);
        for(var i=0;i<10;i++)split.World.Step([],[],.05);
        Assert.Equal(after.BodyStates.ToArray(),split.World.Capture().BodyStates.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void RoundedStopArrivalCannotLeakOutwardVelocity(int sign)
    {
        var rig=Rig(sign*Math.BitDecrement(.6),sign);
        rig.World.Step([],[],.001);
        Assert.InRange(Math.Abs(rig.Joint.Travel.Error-sign*.6),0,1e-7);
        Assert.InRange(rig.Body.KineticEnergy,0,1e-10);
    }

    [Theory]
    [InlineData(-1,1)]
    [InlineData(1,-1)]
    public void RestingStopActsAsAnImmovableSurface(int sign,double arm)
    {
        var rig=Rig(sign*.6);
        var load=Payload(new(arm,0,0),new(0,-5,0),1);
        Contact(load,rig.Body,rig.Joint,load.Center,new(0,1,0),.5);
        Assert.InRange(Math.Abs(load.LinearVelocity.Y-2.5),0,1e-7);
        Assert.InRange(rig.Body.KineticEnergy,0,1e-10);
    }

    [Theory]
    [InlineData(-1,0)]
    [InlineData(-1,1)]
    [InlineData(1,0)]
    [InlineData(1,1)]
    public void ReversingAnInwardMovingHingeAtAStopCannotCreateEnergy(int sign,double restitution)
    {
        var rig=Rig(sign*.6,-sign,inertia:1);
        var load=Payload(new(-sign,0,0),new(0,-2,0),1);
        var before=load.KineticEnergy+rig.Body.KineticEnergy;
        Contact(load,rig.Body,rig.Joint,load.Center,new(0,1,0),restitution);
        Assert.InRange(load.KineticEnergy+rig.Body.KineticEnergy,0,before+.00001);
        Assert.InRange(rig.Body.AngularVelocity.Length,0,1e-7);
        Assert.InRange(load.LinearVelocity.Y,-1e-8,double.MaxValue);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void InwardContactReleasesTheStoppedBeam(int sign)
    {
        var rig=Rig(sign*.6,inertia:1);
        var load=Payload(new(sign,0,0),new(0,-2,0),1);
        Contact(load,rig.Body,rig.Joint,load.Center,new(0,1,0),0);
        Assert.True(sign*rig.Body.AngularVelocity.Z<0);
        rig.World.Step([],[],.01);
        Assert.True(Math.Abs(rig.Joint.Travel.Error)<.6);
    }

    [Fact]
    public void RotationDrivenSweptContactTransfersEnergyToAStationaryBall()
    {
        var rig=Rig(speed:1);
        var load=Payload(new(1,1,0),default,1);
        var world=new PhysicsWorld([],[Object(rig.Body,new ConvexBox(new(2,.1,.4))),
            Object(rig.Anchor),Object(load,new ConvexSphere(.2))],[rig.Joint],new(default,maximumStep:1));
        var before=rig.Body.KineticEnergy;
        world.Step([],[],.8);
        Assert.Contains(world.Impacts.ToArray(),hit=>hit.Pair.A==load.Id||hit.Pair.B==load.Id);
        Assert.True(load.LinearVelocity.Length>.1);
        Assert.True(rig.Body.KineticEnergy<before);
        Assert.InRange(load.KineticEnergy+rig.Body.KineticEnergy,0,before+.00001);
    }

    [Fact]
    public void PivotContactCannotRotateAndSeparatingContactDoesNothing()
    {
        var rig=Rig();var load=Payload(default,new(0,-1,0),1);
        Contact(load,rig.Body,rig.Joint,default,new(0,1,0),.5);
        Assert.InRange(Math.Abs(load.LinearVelocity.Y-.5),0,1e-7);
        Assert.InRange(rig.Body.KineticEnergy,0,1e-10);
        var separating=Payload(new(1,0,0),new(0,1,0),1);
        var row=Contact(separating,rig.Body,rig.Joint,separating.Center,new(0,1,0),1);
        Assert.Equal(0,row.AccumulatedImpulse);
        Assert.Equal(new CollisionVector(0,1,0),separating.LinearVelocity);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void VariedFreeAndStoppedContactsRemainPassive(int sign)
    {
        var random=new Random(1729);
        for(var trial=0;trial<1000;trial++)
        {
            var rig=Rig(sign*.6,(random.NextDouble()*2-1)*10,.2+random.NextDouble()*10);
            // Project the initial declared velocity through the actual stop,
            // before measuring the available kinetic energy.
            ImpulseSolver.Solve(rig.Joint.VelocityConstraints(1e-7));
            var point=new CollisionVector(random.NextDouble()*4-2,.1,.3);
            var load=Payload(point,new(.4,random.NextDouble()*20-10,-.3),.2+random.NextDouble()*8);
            var before=load.KineticEnergy+rig.Body.KineticEnergy;
            var row=Contact(load,rig.Body,rig.Joint,point,new(0,1,0),random.NextDouble());
            Assert.InRange(load.KineticEnergy+rig.Body.KineticEnergy,0,before+.0001);
            Assert.True(row.AccumulatedImpulse>=0);Assert.True(row.Speed>=-1e-7);
        }
    }

    [Fact]
    public void StaticNormalResponseDoesNotMoveTheOtherBody()
    {
        var rig=Rig(speed:1);
        var fixedBody=new PhysicsBody(new(2),PhysicsMotionType.Static,RigidPose.At(new(1,0,0)),default,default);
        Contact(fixedBody,rig.Body,rig.Joint,fixedBody.Center,new(0,1,0),.5);
        Assert.Equal(default,fixedBody.LinearVelocity);
        Assert.InRange(Math.Abs(rig.Body.AngularVelocity.Z+.5),0,1e-7);
    }

    [Fact]
    public void InvalidDeclarationsAndContactInputsRejectWithoutMutatingState()
    {
        Assert.Throws<ArgumentException>(()=>Rig(inertia:0));
        Assert.Throws<ArgumentException>(()=>new JointTravelRange(1,-1));
        var rig=Rig();var before=rig.World.Capture();var load=Payload(new(1,0,0),new(0,-1,0),1);
        Assert.Throws<ArgumentOutOfRangeException>(()=>rig.World.Step([],[],double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>rig.World.Step([],[],-1));
        Assert.Throws<ArgumentException>(()=>rig.World.ApplyImpulse(rig.Body.Id,new(double.NaN,0,0),default));
        Assert.Throws<ArgumentException>(()=>ImpulseConstraint.Contact(load,rig.Body,load.Center,new(0,2,0),1,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ImpulseConstraint.Contact(load,rig.Body,load.Center,new(0,1,0),2,0));
        Assert.Equal(before.BodyStates.ToArray(),rig.World.Capture().BodyStates.ToArray());
    }
}
