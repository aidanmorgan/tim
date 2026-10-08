using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AirJetBoundaryTests
{
    private const double Tolerance=1e-7;
    private static readonly CompoundGeometry Geometry=new([new(new ConvexSphere(.01),AffineTransform.Identity)]);
    private static PhysicsColliderUpdate Collider(PhysicsBody body)=>new(body.Id,Geometry,new(0,0,0),CollisionParticipation.Enabled);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Kinematic,RigidPose.At(center),velocity,spin);
    private static AirJetGeometry Jet(PhysicsBody source,PhysicsBody receiver,CollisionVector localOrigin=default,CollisionVector localPoint=default)=>
        new(source.Id,receiver.Id,localOrigin,new(1,0,0),localPoint,3,1,[]);

    [Theory]
    [InlineData(AirJetBoundary.Inlet,false)]
    [InlineData(AirJetBoundary.Inlet,true)]
    [InlineData(AirJetBoundary.Outlet,false)]
    [InlineData(AirJetBoundary.Outlet,true)]
    [InlineData(AirJetBoundary.Rim,false)]
    [InlineData(AirJetBoundary.Rim,true)]
    public void CapturedBoundaryFindsBothCrossingDirections(AirJetBoundary surface,bool entering)
    {
        var (position,velocity)=surface switch
        {
            AirJetBoundary.Inlet=>(new CollisionVector(entering?-.5:.5,0,0),new CollisionVector(entering?1:-1,0,0)),
            AirJetBoundary.Outlet=>(new CollisionVector(entering?3.5:2.5,0,0),new CollisionVector(entering?-1:1,0,0)),
            AirJetBoundary.Rim=>(new CollisionVector(1.5,entering?1.5:.5,0),new CollisionVector(0,entering?-1:1,0)),
            _=>throw new ArgumentOutOfRangeException(nameof(surface))
        };
        var source=Body(0,default,default);var receiver=Body(1,position,velocity);
        var bodies=new[]{source,receiver}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(entry=>entry.Key,entry=>Collider(entry.Value));
        var paths=bodies.ToDictionary(entry=>entry.Key,entry=>entry.Value.CreateTrajectory(1,default));
        var jet=Jet(source,receiver);
        var result=jet.Sweep(bodies,colliders,paths,1,Tolerance);
        Assert.Equal(ScalarSweepStatus.Boundary,result.Status);Assert.Equal(surface,result.Boundary);
        Assert.InRange(result.Time,.5,.5+Tolerance);
        var before=bodies.ToDictionary(entry=>entry.Key,entry=>paths[entry.Key].SampleBody(.49));
        var after=bodies.ToDictionary(entry=>entry.Key,entry=>paths[entry.Key].SampleBody(result.Time));
        Assert.Equal(entering,!jet.IsExposed(before,colliders));
        Assert.Equal(entering,jet.IsExposed(after,colliders));
        Assert.Equal(result,jet.Sweep(bodies,colliders,paths,1,Tolerance));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovingSourceAndOffsetReceiverUseRelativeCapturedMotion(bool rotated)
    {
        var rotation=rotated?RigidRotation.FromRotationVector(new(.3,-.5,.2)):RigidRotation.Identity;
        var origin=rotation.Apply(new CollisionVector(4,3,2));
        var source=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,new(origin,rotation),rotation.Apply(new(2,0,0)),default);
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,new(origin+rotation.Apply(new(2,0,0)),rotation),rotation.Apply(new(1,0,0)),default);
        // Source origin x=.2; receiver point x=-.3: inlet reached at 1.5 seconds.
        var jet=Jet(source,receiver,new(.2,0,0),new(-.3,0,0));
        var bodies=new[]{source,receiver}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(entry=>entry.Key,entry=>Collider(entry.Value));
        var paths=bodies.ToDictionary(entry=>entry.Key,entry=>entry.Value.CreateTrajectory(2,default));
        var hit=jet.Sweep(bodies,colliders,paths,2,Tolerance);
        Assert.Equal(AirJetBoundary.Inlet,hit.Boundary);
        Assert.InRange(hit.Time,1.5,1.5+Tolerance);
        var shortHit=jet.Sweep(bodies,colliders,paths,1,Tolerance);
        Assert.Equal(ScalarSweepStatus.Clear,shortHit.Status);Assert.Null(shortHit.Boundary);
    }

    [Fact]
    public void RotatingJetFindsRadialExitAndClearControl()
    {
        var source=Body(0,default,default,new(0,0,1));var receiver=Body(1,new(2,0,0),default);
        var bodies=new[]{source,receiver}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(entry=>entry.Key,entry=>Collider(entry.Value));
        var paths=bodies.ToDictionary(entry=>entry.Key,entry=>entry.Value.CreateTrajectory(1,default));
        var jet=Jet(source,receiver);
        var result=jet.Sweep(bodies,colliders,paths,1,Tolerance);
        Assert.Equal(AirJetBoundary.Rim,result.Boundary);
        Assert.InRange(result.Time,Math.PI/6,Math.PI/6+Tolerance);
        Assert.Equal(ScalarSweepStatus.Clear,jet.Sweep(bodies,colliders,paths,.4,Tolerance).Status);
    }

    [Fact]
    public void ReceiverRotationSweepsItsLocalSamplePoint()
    {
        var source=Body(0,default,default);var receiver=Body(1,new(1.5,0,0),default,new(1,0,0));
        var jet=Jet(source,receiver,default,new(0,1.5,0));
        // A circle around the jet axis never enters its unit radius.
        var bodies=new[]{source,receiver}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(entry=>entry.Key,entry=>Collider(entry.Value));
        var paths=bodies.ToDictionary(entry=>entry.Key,entry=>entry.Value.CreateTrajectory(1,default));
        Assert.Equal(ScalarSweepStatus.Clear,jet.Sweep(bodies,colliders,paths,1,Tolerance).Status);
        // Rotating around Z carries an off-centre point across the radial surface.
        var crossing=Body(2,new(1.5,0,0),default,new(0,0,1));
        bodies=new[]{source,crossing}.ToDictionary(body=>body.Id);
        colliders=bodies.ToDictionary(entry=>entry.Key,entry=>Collider(entry.Value));
        paths=bodies.ToDictionary(entry=>entry.Key,entry=>entry.Value.CreateTrajectory(1,default));
        var hit=Jet(source,crossing,default,new(1.2,0,0)).Sweep(bodies,colliders,paths,1,Tolerance);
        Assert.Equal(AirJetBoundary.Rim,hit.Boundary);
        Assert.InRange(hit.Time,Math.Asin(1/1.2),Math.Asin(1/1.2)+Tolerance);
    }

    [Fact]
    public void InitiallyOnSurfaceMakesPositiveProgressWithoutRepeatedEvents()
    {
        var source=Body(0,default,default);var receiver=Body(1,new(3,0,0),new(1,0,0));
        var bodies=new[]{source,receiver}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(entry=>entry.Key,entry=>Collider(entry.Value));
        var paths=bodies.ToDictionary(entry=>entry.Key,entry=>entry.Value.CreateTrajectory(1,default));
        var jet=Jet(source,receiver);var hit=jet.Sweep(bodies,colliders,paths,1,Tolerance);
        Assert.Equal(AirJetBoundary.Outlet,hit.Boundary);Assert.InRange(hit.Time,double.Epsilon,Tolerance);
        var advanced=bodies.ToDictionary(entry=>entry.Key,entry=>paths[entry.Key].SampleBody(hit.Time));
        var next=advanced.ToDictionary(entry=>entry.Key,entry=>entry.Value.CreateTrajectory(1,default));
        Assert.Equal(ScalarSweepStatus.Clear,jet.Sweep(advanced,colliders,next,1,Tolerance).Status);
    }

    [Fact]
    public void TangencyDoesNotReportAChangeOfSide()
    {
        var source=Body(0,default,default);var receiver=Body(1,new(1.5,1,-1),new(0,0,2));
        var bodies=new[]{source,receiver}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(entry=>entry.Key,entry=>Collider(entry.Value));
        var paths=bodies.ToDictionary(entry=>entry.Key,entry=>entry.Value.CreateTrajectory(1,default));
        Assert.Equal(ScalarSweepStatus.Clear,Jet(source,receiver).Sweep(bodies,colliders,paths,1,Tolerance).Status);
    }

    [Fact]
    public void DisabledAndInvalidDeclarationsAreExplicit()
    {
        var source=Body(0,default,default);var receiver=Body(1,new(2.5,0,0),new(1,0,0));
        var bodies=new[]{source,receiver}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(entry=>entry.Key,entry=>Collider(entry.Value));
        var paths=bodies.ToDictionary(entry=>entry.Key,entry=>entry.Value.CreateTrajectory(1,default));
        var jet=Jet(source,receiver);
        colliders[source.Id]=new(source.Id,Geometry,new(0,0,0),CollisionParticipation.Disabled);
        Assert.Equal(ScalarSweepStatus.Clear,jet.Sweep(bodies,colliders,paths,1,Tolerance).Status);
        Assert.Throws<ArgumentOutOfRangeException>(()=>jet.Sweep(bodies,colliders,paths,2,Tolerance));
        Assert.Throws<ArgumentOutOfRangeException>(()=>jet.Sweep(bodies,colliders,paths,1,.5));
        Assert.Throws<ArgumentOutOfRangeException>(()=>jet.Sweep(bodies,colliders,paths,1,double.NaN));
        paths[receiver.Id]=source.CreateTrajectory(1,default);
        Assert.Throws<InvalidOperationException>(()=>jet.Sweep(bodies,colliders,paths,1,Tolerance));
        paths.Remove(source.Id);
        Assert.Throws<ArgumentException>(()=>jet.Sweep(bodies,colliders,paths,1,Tolerance));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MovingWindmillUsesFieldEventsWithoutDirectBodyJetLoads(bool powered)
    {
        var source=Body(0,default,default);
        var carrier=Body(1,new(2,-2,0),new(0,20,0));
        var rotor=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,carrier.Pose,new(0,20,0),default,1,new(1,1,1));
        var frame=new JointFrame(default,RigidRotation.FromRotationVector(new(0,Math.PI/2,0)));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,rotor,frame,carrier,frame,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var bodies=new[]{source,carrier,rotor};
        var world=new PhysicsWorld([],bodies.Select(body=>new PhysicsObject(body,Geometry,new(0,0,0))),[joint],new(default,maximumStep:.2));
        world.InstallEnergyStores([new(source.Id,100,100)]);
        var supply=new MechanicalTransferSource(new StoredFlowSource(new(0),source.Id,powered?12:0,new(6,72)));
        var field=new AirJetGeometry(source.Id,carrier.Id,default,new(1,0,0),default,3,1,[rotor.Id]);
        var capture=new RotaryCaptureDeclaration(joint.Id,new(.3,2,100,.05),
            [new(new(0),supply,field,new(.5,6),1e-8)],1e-7);
        world.ReplaceLoads(world.Loads with {Rotary=[capture]});
        Assert.Empty(world.Loads.Efforts.ToArray());
        var initial=world.Capture();var initialKinetic=rotor.KineticEnergy;var result=world.Step([],[],.2);
        // Zero speed before entry; midpoint-driven .1 s in the field; passive
        // midpoint damping during the .05 s coast after exit. I=1, damping=.15.
        var expected=powered?.18/(1+.0075)*(1-.00375)/(1+.00375):0;
        var speed=joint.Travel.Jacobian.Bind(rotor,carrier).Speed;
        Assert.InRange(Math.Abs(speed-expected),0,1e-7);
        Assert.True(result.Events>=2);
        Assert.Equal(powered,world.EnergyStore(source.Id).ReleasedEnergy>0);
        Assert.True(rotor.KineticEnergy-initialKinetic
            <=world.EnergyStore(source.Id).ReleasedEnergy+1e-8);
        var final=world.Capture();
        world.Restore(initial);Assert.Equal(result,world.Step([],[],.2));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }

    [Fact]
    public void PredictorStopsAtFieldEventAndWorldIntegratesTheRemainingCoast()
    {
        var source=Body(0,default,default);
        var receiver=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2.8,0,0)),new(2,0,0),default,1,new(1,1,1));
        var bodies=new[]{source,receiver}.ToDictionary(body=>body.Id);
        var colliders=bodies.ToDictionary(entry=>entry.Key,entry=>Collider(entry.Value));
        var loads=bodies.ToDictionary(entry=>entry.Key,_=>default(BodyWrench));
        var jet=new NozzleCoupledJetReceiver().CreateLoad(new(0),
            new(new StoredFlowSource(new(0),source.Id,12,new(6,72))),
            new(source.Id,receiver.Id,default,new(1,0,0),default,3,1,[]),new(6,6),1e-8);
        var stores=new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>{{source.Id,new(source.Id,100,100,100,0,0)}};
        var prediction=AccelerationSolver.Predict(new PhysicsLoadSet {Transfers=[jet]},colliders,bodies.Values,[],[],loads,.2,.2,Tolerance,1e-8,1e-9,[],stores, new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        Assert.Equal(100,stores[source.Id].Energy);
        Assert.Equal(ForcePredictionBoundary.Field,prediction.Boundary);
        var speed=Math.Sqrt(4+2*6*.2);var exit=2*.2/(2+speed);
        Assert.InRange(prediction.Duration,exit,exit+Tolerance);
        Assert.InRange(prediction.Trajectories[receiver.Id].At(prediction.Duration).Center.X,3,3+Tolerance);
        var world=new PhysicsWorld([],bodies.Values.Select(body=>new PhysicsObject(body,Geometry,new(0,0,0))),[],new(default,maximumStep:.2));
        world.InstallEnergyStores([new(source.Id,100,100)]);
        world.ReplaceLoads(world.Loads with {Transfers=[jet]});var initial=world.Capture();
        var result=world.Step([],[],.2);
        Assert.True(result.Events>0);
        Assert.InRange(Math.Abs(receiver.LinearVelocity.X-speed),0,1e-6);
        Assert.True(world.EnergyStore(source.Id).ReleasedEnergy>0);
        var final=world.Capture();
        world.Restore(initial);Assert.Equal(result,world.Step([],[],.2));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
    }
}
