using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsContactLoadSensorTests
{
    public enum Shape { Sphere, Box, Compound }
    public enum Placement { Top, Side, Below, Gap, Outside, StaticTop }
    private static readonly CollisionBounds Region=new(new(-.8,.049,-.8),new(.8,.051,.8));
    private sealed record Fixture(PhysicsWorld World,PhysicsBody Frame,PhysicsBody Load);
    private static Fixture Create(Shape shape=Shape.Sphere,bool rotated=false,double minimumMass=1,
        Placement placement=Placement.Top,bool failLater=false)
    {
        var rotation=rotated?RigidRotation.FromRotationVector(new(.2,.3,.4)):RigidRotation.Identity;
        var local=placement switch
        {
            Placement.Top or Placement.StaticTop=>new CollisionVector(0,.25,0),
            Placement.Side=>new(1.2,0,0),Placement.Below=>new(0,-.25,0),
            Placement.Gap=>new(0,.3,0),Placement.Outside=>new(.9,.25,0),
            _=>throw new ArgumentOutOfRangeException(nameof(placement))
        };
        var geometry=shape switch
        {
            Shape.Sphere=>new CompoundGeometry([new(new ConvexSphere(.2),AffineTransform.Identity)]),
            Shape.Box=>new CompoundGeometry([new(new ConvexBox(new(.3,.2,.3)),AffineTransform.Identity)]),
            Shape.Compound=>new CompoundGeometry([new(new ConvexSphere(.2),new(AffineBasis.Identity,new(-.4f,0,0))),
                new(new ConvexSphere(.2),new(AffineBasis.Identity,new(.4f,0,0)))]),
            _=>throw new ArgumentOutOfRangeException(nameof(shape))
        };
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,new(default,rotation),default,default);
        var dynamic=placement!=Placement.StaticTop;
        var load=new PhysicsBody(new(1),dynamic?PhysicsMotionType.Dynamic:PhysicsMotionType.Static,
            new(rotation.Apply(local),rotation),default,default,dynamic?2:0,dynamic?new(1,1,1):default);
        var objects=new List<PhysicsObject>
        {
            new(frame,new([new(new ConvexBox(new(1,.05,1)),AffineTransform.Identity)]),new(0,0,0)),
            new(load,geometry,new(0,0,0))
        };
        if(failLater)
            foreach(var index in new[]{2,3})
                objects.Add(new(new PhysicsBody(new(index),PhysicsMotionType.Kinematic,RigidPose.At(new(index*3,0,0)),
                    default,new(2,0,0)),new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0)));
        var world=new PhysicsWorld([],objects,[],new(default,maximumStep:.05,maximumEvents:failLater?1:256));
        world.InstallContactLoadSensors([new(frame.Id,Region,new(0,2,0),1-1e-6,minimumMass)]);
        if(failLater)world.InstallTiltSensors([new(new(2),new(0,1,0),.7071067811865476),new(new(3),new(0,1,0),.7071067811865476)]);
        return new(world,frame,load);
    }

    [Theory]
    [InlineData(Shape.Sphere,false,1,PhysicsContactLoadPhase.Loaded)]
    [InlineData(Shape.Sphere,true,3,PhysicsContactLoadPhase.Underweight)]
    [InlineData(Shape.Box,false,1,PhysicsContactLoadPhase.Loaded)]
    [InlineData(Shape.Box,true,3,PhysicsContactLoadPhase.Underweight)]
    [InlineData(Shape.Compound,false,1,PhysicsContactLoadPhase.Loaded)]
    [InlineData(Shape.Compound,true,3,PhysicsContactLoadPhase.Underweight)]
    public void OwnedContactMassCountsEveryBodyOnceWithoutCacheMutation(Shape shape,bool rotated,double threshold,PhysicsContactLoadPhase phase)
    {
        var f=Create(shape,rotated,threshold);var before=f.World.Capture();var pairs=f.World.RetainedContactPairs;
        for(var i=0;i<3;i++)
        {
            var reading=f.World.ContactLoad(f.Frame.Id);
            Assert.Equal(f.Frame.Id,reading.Frame);Assert.Equal(2,reading.Mass);Assert.Equal(phase,reading.Phase);
            Assert.Equal(new[]{f.Load.Id},reading.Bodies.ToArray());
        }
        Assert.Equal(before.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        Assert.Equal(pairs,f.World.RetainedContactPairs);
        var collider=f.World.Collider(f.Load.Id).Declaration;
        f.World.ApplyColliderUpdates([new(f.Load.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
        Assert.Equal(PhysicsContactLoadPhase.Empty,f.World.ContactLoad(f.Frame.Id).Phase);
        f.World.Restore(before);Assert.Equal(2,f.World.ContactLoad(f.Frame.Id).Mass);
        f.World.ApplyImpulse(f.Load.Id,f.Frame.Pose.Rotation.Apply(new(0,2,0)),f.Load.Center);
        var ready=f.World.Capture();
        f.World.Step([],[],.03);
        Assert.Equal(PhysicsContactLoadPhase.Empty,f.World.ContactLoad(f.Frame.Id).Phase);
        var after=f.World.Capture();f.World.Restore(ready);f.World.Step([],[],.03);
        Assert.Equal(after.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        f.World.Restore(before);Assert.Equal(2,f.World.ContactLoad(f.Frame.Id).Mass);
    }

    [Theory]
    [InlineData(Placement.Side)]
    [InlineData(Placement.Below)]
    [InlineData(Placement.Gap)]
    [InlineData(Placement.Outside)]
    [InlineData(Placement.StaticTop)]
    public void OtherFacesGapsOutsideRegionAndStaticObjectsDoNotCount(Placement placement)
    {
        var f=Create(placement:placement);
        var reading=f.World.ContactLoad(f.Frame.Id);
        Assert.Equal(0,reading.Mass);Assert.Equal(PhysicsContactLoadPhase.Empty,reading.Phase);Assert.Empty(reading.Bodies.ToArray());
    }

    [Fact]
    public void DisabledFrameAndOppositeNormalRejectTopContact()
    {
        var f=Create();var before=f.World.Capture();var collider=f.World.Collider(f.Frame.Id).Declaration;
        f.World.ApplyColliderUpdates([new(f.Frame.Id,collider.Geometry,collider.Material,CollisionParticipation.Disabled)]);
        Assert.Equal(0,f.World.ContactLoad(f.Frame.Id).Mass);
        f.World.Restore(before);
        var sensor=new PhysicsContactLoadSensor(f.Frame.Id,Region,new(0,-1,0),.99,1);
        Assert.Equal(0,sensor.Measure(f.Frame,f.World.BodyContacts(f.Frame.Id)).Mass);
    }

    [Fact]
    public void MultipleBodiesAreSummedInStableIdentityOrder()
    {
        var f=Create();
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var a=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(-.4,.25,0)),default,default,2,new(1,1,1));
        var b=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(.4,.25,0)),default,default,3,new(1,1,1));
        var ball=new CompoundGeometry([new(new ConvexSphere(.2),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],[new(frame,f.World.Collider(f.Frame.Id).Declaration.Geometry,new(0,0,0)),
            new(b,ball,new(0,0,0)),new(a,ball,new(0,0,0))],[],new(default));
        world.InstallContactLoadSensors([new(frame.Id,Region,new(0,1,0),.99,5)]);
        var reading=world.ContactLoad(frame.Id);
        Assert.Equal(5,reading.Mass);Assert.Equal(PhysicsContactLoadPhase.Loaded,reading.Phase);
        Assert.Equal(new[]{a.Id,b.Id},reading.Bodies.ToArray());
    }

    [Fact]
    public void InstallationAndSnapshotMembershipAreAtomic()
    {
        var f=Create();var before=f.World.Capture();
        Assert.Throws<ArgumentException>(()=>f.World.InstallContactLoadSensors([new(f.Load.Id,Region,new(0,1,0),.99,1),new(f.Frame.Id,Region,new(0,1,0),.99,1)]));
        Assert.Throws<ArgumentException>(()=>f.World.InstallContactLoadSensors([new(new(99),Region,new(0,1,0),.99,1)]));
        Assert.Throws<ArgumentNullException>(()=>f.World.InstallContactLoadSensors(null!));
        Assert.Throws<ArgumentNullException>(()=>f.World.InstallContactLoadSensors([null!]));
        Assert.Equal(before.ContactLoadSensors.ToArray(),f.World.ContactLoadSensors.ToArray());
        Assert.Throws<ArgumentException>(()=>f.World.ContactLoad(f.Load.Id));
        f.World.InstallContactLoadSensors([new(f.Load.Id,Region,new(0,1,0),.99,1)]);
        Assert.Equal(2,f.World.ContactLoadSensors.Length);
        f.World.Restore(before);Assert.Single(f.World.ContactLoadSensors.ToArray());
    }

    [Fact]
    public void InvalidDeclarationsAndFixtureChoicesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsContactLoadSensor(new(0),default,new(0,1,0),.99,1));
        Assert.Throws<ArgumentException>(()=>new PhysicsContactLoadSensor(new(0),Region,default,.99,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsContactLoadSensor(new(0),Region,new(0,1,0),double.NaN,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsContactLoadSensor(new(0),Region,new(0,1,0),1.1,1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new PhysicsContactLoadSensor(new(0),Region,new(0,1,0),.99,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Create((Shape)int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Create(placement:(Placement)int.MaxValue));
    }

    [Fact]
    public void FailedStepRestoresContactReadingFromOwnedBodies()
    {
        var f=Create(failLater:true);
        f.World.ApplyImpulse(f.Load.Id,new(0,2,0),f.Load.Center);
        var before=f.World.Capture();
        Assert.Throws<InvalidOperationException>(()=>f.World.Step([],[],.5));
        Assert.Equal(before.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        Assert.Equal(before.ContactLoadSensors.ToArray(),f.World.ContactLoadSensors.ToArray());
        Assert.Equal(2,f.World.ContactLoad(f.Frame.Id).Mass);
        Assert.Equal(PhysicsContactLoadPhase.Loaded,f.World.ContactLoad(f.Frame.Id).Phase);
        Assert.Equal(before.Time,f.World.Time);Assert.Equal(before.StepIndex,f.World.StepIndex);
    }
}
