using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsLatchedSpringTests
{
    private static readonly CompoundGeometry Geometry=new([new(new ConvexSphere(.01),AffineTransform.Identity)]);
    private static readonly CompoundGeometry FrameGeometry=new([new(new ConvexSphere(.01),new(AffineBasis.Identity,new(0,5,0)))]);
    private sealed record Fixture(PhysicsWorld World,PhysicsBody Head,PhysicsBody Shaft,PhysicsJointId Guide,PhysicsJointId Hinge,LatchedSpringLoad Load);
    private static Fixture Create(double compression=0,bool failPassage=false)
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var head=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(0,0,-compression)),default,default,1,new(1,1,1));
        var shaft=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(3,0,0)),default,default,1,new(1,1,1));
        var guide=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,head,new(default,RigidRotation.Identity),frame,new(default,RigidRotation.Identity),
            ConnectedBodyCollision.Disabled,new(-1,0),JointTravelDirection.Negative);
        var hinge=new PhysicsFrameJoint(new(1),FrameJointKind.Hinge,shaft,new(default,RigidRotation.Identity),frame,new(new(3,0,0),RigidRotation.Identity),
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var transmission=new PhysicsTransmissionJoint(new(2),hinge,guide,.1,TransmissionEngagement.Engaged);
        var objects=new List<PhysicsObject> {new(frame,FrameGeometry,new(0,0,0)),new(head,Geometry,new(0,0,0)),new(shaft,Geometry,new(0,0,0))};
        if(failPassage)
            foreach(var id in new[]{new PhysicsBodyId(3),new(4)})
                objects.Add(new(new PhysicsBody(id,PhysicsMotionType.Static,
                    new(new(0,0,-.25),RigidRotation.FromRotationVector(new(0,-Math.PI/2,0))),default,default),FrameGeometry,new(0,0,0)));
        var world=new PhysicsWorld([],objects,[guide,hinge,transmission],new(default,maximumStep:.005,maximumEvents:failPassage?1:256));
        if(failPassage)world.InstallPassageSensors([new(new(3),.5,.02),new(new(4),.5,.02)]);
        var load=new LatchedSpringLoad(guide.Id,transmission.Id,100,0,1);
        world.ReplaceLoads(new(){Springs=[load]});
        return new(world,head,shaft,guide.Id,hinge.Id,load);
    }
    [Fact]
    public void OwnedJointPotentialAndReleaseDoNotRequireSceneObservers()
    {
        var f=Create(.5);var initial=f.World.Capture();
        Assert.Equal(12.5,f.World.Spring(f.Guide).Energy);
        Assert.Equal(0,f.World.Spring(f.Guide).AcceptedWork);
        Assert.Equal(SpringTriggerResult.Released,f.World.ReleaseSpring(f.Guide));
        Assert.Equal(SpringTriggerResult.AlreadyReleased,f.World.ReleaseSpring(f.Guide));
        var binding=f.Load.Resolve(f.World.Joints.ToArray());
        Assert.Equal(JointTravelDirection.Positive,binding.Guide.Direction);
        Assert.Equal(TransmissionEngagement.Open,binding.Transmission.Engagement);
        var result=f.World.Step([],[],.25);
        var state=f.World.Spring(f.Guide);
        Assert.Equal(SpringLatchState.Latched,state.State);
        Assert.Equal(1,state.ReleaseCount);
        Assert.InRange(state.Energy,0,1e-10);
        Assert.InRange(Math.Abs(state.ReleasedWork+state.Energy-12.5),0,1e-9);
        Assert.Equal(TransmissionEngagement.Engaged,f.Load.Resolve(f.World.Joints.ToArray()).Transmission.Engagement);
        Assert.Equal(SpringTriggerResult.Empty,f.World.ReleaseSpring(f.Guide));
        var after=f.World.Capture();
        f.World.Restore(initial);
        Assert.Equal(initial.Springs.ToArray(),f.World.Springs.ToArray());
        Assert.Equal(SpringTriggerResult.Released,f.World.ReleaseSpring(f.Guide));
        Assert.Equal(SpringTriggerResult.AlreadyReleased,f.World.ReleaseSpring(f.Guide));
        Assert.Equal(result,f.World.Step([],[],.25));f.World.ReleaseSpring(f.Guide);
        Assert.Equal(after.Springs.ToArray(),f.World.Springs.ToArray());
        Assert.Equal(after.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
    }
    [Fact]
    public void ActualWindingWorkAndEmptyTriggerAreOwnedAndSnapshotRestoresThem()
    {
        var f=Create();Assert.Equal(SpringTriggerResult.Empty,f.World.ReleaseSpring(f.Guide));
        var initial=f.World.Capture();
        for(var i=0;i<20;i++)f.World.Step([],[new(f.Hinge,-2,100,100,1000000)],.01);
        var state=f.World.Spring(f.Guide);
        Assert.Equal(SpringLatchState.Latched,state.State);
        Assert.True(state.Compression>.02);Assert.True(state.Energy>0);
        Assert.InRange(Math.Abs(state.AcceptedWork-state.Energy),0,1e-8);
        Assert.Equal(0,state.ReleaseCount);Assert.Equal(0,state.ReleasedWork);
        var after=f.World.Capture();
        f.World.Restore(initial);
        for(var i=0;i<20;i++)f.World.Step([],[new(f.Hinge,-2,100,100,1000000)],.01);
        Assert.Equal(after.Springs.ToArray(),f.World.Springs.ToArray());
        Assert.Equal(after.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
    }
    [Fact]
    public void InvalidLoadBatchOrExternalPolicyChangeCannotCorruptSpringState()
    {
        var f=Create(.5);var before=f.World.Capture();
        Assert.Throws<ArgumentException>(()=>f.World.ReplaceLoads(f.World.Loads with {Springs=[f.Load,f.Load]}));
        Assert.Throws<ArgumentException>(()=>f.World.ReplaceLoads(f.World.Loads with {Springs=[new(f.Guide,new(99),100,0,1)]}));
        Assert.Throws<ArgumentException>(()=>f.World.ReplaceLoads(f.World.Loads with {Springs=[new(f.Guide,f.Load.Transmission,200,0,1)]}));
        Assert.Throws<ArgumentException>(()=>f.World.ReplaceLoads(f.World.Loads with {Elastic=[f.Load.Elastic]}));
        var (guide,transmission)=f.Load.Resolve(f.World.Joints.ToArray());
        var opened=new PhysicsTransmissionJoint(transmission.Id,transmission.Input,guide,transmission.Ratio,TransmissionEngagement.Open);
        Assert.Throws<ArgumentException>(()=>f.World.ReplaceJoints(f.World.Joints.ToArray().Select(j=>j.Id==opened.Id?opened:j)));
        Assert.Equal(before.Springs.ToArray(),f.World.Springs.ToArray());
        Assert.Equal(before.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        Assert.Throws<ArgumentException>(()=>f.World.ReleaseSpring(new(99)));
    }
    [Fact]
    public void RemovingTheExplicitLoadFamilyAndRestoringSnapshotRestoresMembership()
    {
        var f=Create(.25);var before=f.World.Capture();
        Assert.False(f.World.Loads.IsEmpty);
        f.World.ReplaceLoads(new());Assert.Empty(f.World.Springs.ToArray());
        Assert.Throws<ArgumentException>(()=>f.World.ReleaseSpring(f.Guide));
        f.World.Restore(before);
        Assert.Equal(before.Springs.ToArray(),f.World.Springs.ToArray());
        Assert.Equal(SpringTriggerResult.Released,f.World.ReleaseSpring(f.Guide));
    }
    [Theory]
    [InlineData(0,1)]
    [InlineData(double.NaN,1)]
    [InlineData(100,0)]
    [InlineData(100,double.PositiveInfinity)]
    public void InvalidParametersAreRejected(double stiffness,double stroke)=>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new LatchedSpringLoad(new(0),new(1),stiffness,0,stroke));
    [Fact]
    public void FamilyCopiesInputsAndRejectsNulls()
    {
        var f=Create();
        var supplied=new[]{f.Load};var loads=new PhysicsLoadSet {Springs=supplied};
        supplied[0]=new(new(2),new(3),10,0,.5);
        Assert.Same(f.Load,Assert.Single(loads.Springs));
        var view=Assert.IsAssignableFrom<IList<LatchedSpringLoad>>(loads.Springs);
        Assert.True(view.IsReadOnly);Assert.Throws<NotSupportedException>(()=>view[0]=supplied[0]);
        Assert.Empty((loads with {Springs=[]}).Springs);Assert.Single(loads.Springs);
        Assert.Throws<ArgumentNullException>(()=>new PhysicsLoadSet {Springs=null!});
        Assert.Throws<ArgumentNullException>(()=>new PhysicsLoadSet {Springs=[null!]});
        Assert.Throws<ArgumentException>(()=>new LatchedSpringLoad(new(1),new(1),100,0,1));
    }

    [Fact]
    public void FailedReleaseStepRestoresWorkLatchCouplingBodiesAndClock()
    {
        var f=Create(.5,failPassage:true);
        f.World.ReleaseSpring(f.Guide);
        var before=f.World.Capture();
        Assert.Throws<InvalidOperationException>(()=>f.World.Step([],[],.25));
        Assert.Equal(before.Springs.ToArray(),f.World.Springs.ToArray());
        Assert.Equal(before.BodyStates.ToArray(),f.World.Capture().BodyStates.ToArray());
        Assert.Equal(before.Time,f.World.Time);Assert.Equal(before.StepIndex,f.World.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,f.World.Phase);
        Assert.Equal(TransmissionEngagement.Open,f.Load.Resolve(f.World.Joints.ToArray()).Transmission.Engagement);
        Assert.Equal(0,f.World.Spring(f.Guide).ReleasedWork);
        f.World.Step([],[],.01);Assert.True(f.World.Spring(f.Guide).ReleasedWork>0);
    }
}
