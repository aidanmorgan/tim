using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AccelerationSolverTests
{
    [Fact]
    public void PredictionSeparatesInitialConstraintSolveFromCoupledMidpointEquations()
    {
        var body=Body(0);var anchor=Fixed();
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,new(X*20,default)},{anchor.Id,default}};
        var prediction=AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),
            [body,anchor],[joint],[],loads,.01,.01,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        Assert.Equal(1,prediction.ConstraintWork.Solves);
        Assert.Equal(1,prediction.ConstraintWork.Iterations);
        Assert.True(prediction.MidpointEvaluations>=1);
        Assert.Equal(11,prediction.Coordinates);
        // Initial five bilateral slider rows share the dynamic body: ten pairs.
        Assert.Equal(10,prediction.ConstraintWork.CouplingTests);
        Assert.Equal(0,prediction.ConstraintWork.CoupledPairs);
        Near(default,prediction.Wrenches[body.Id].Force);
        prediction.ConstraintWork.Validate();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void FutureStopIsReachedBySweepRatherThanActivatedDuringPrediction(int sign)
    {
        const double gap=.001,duration=.01,acceleration=100;
        var body=Body(0,-Z*(gap*sign));var anchor=Fixed();
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,Origin,anchor,Origin,
            ConnectedBodyCollision.Disabled,sign>0?new(-1,0):new(0,1),JointTravelDirection.Both);
        var force=Z*(acceleration*2*sign);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,new(force,default)},{anchor.Id,default}};
        var initial=new[]{body.Snapshot(),anchor.Snapshot()};
        var prediction=AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[body,anchor],[joint],[],loads,duration,duration,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        Near(force,prediction.Wrenches[body.Id].Force);
        Assert.Equal(ForcePredictionBoundary.Joint,prediction.Boundary);
        Assert.True(prediction.Duration<duration);
        var paths=joint.Bodies.ToArray().Select(b=>prediction.Trajectories[b.Id]).ToArray();
        var hit=joint.Sweep(paths,prediction.Duration,1e-7,1e-8);
        Assert.Equal(JointSweepStatus.Boundary,hit.Status);
        Assert.Equal(sign>0?JointBoundary.Upper:JointBoundary.Lower,hit.Boundary);
        Assert.InRange(Math.Abs(hit.Time-Math.Sqrt(2*gap/acceleration)),0,1e-5);
        Assert.Equal(initial,new[]{body.Snapshot(),anchor.Snapshot()});
    }
    [Theory]
    [InlineData(1,0)]
    [InlineData(-1,0)]
    [InlineData(1,.4)]
    [InlineData(-1,.4)]
    public void PredictionSupportsAnActiveStopAndPermitsForceReversal(int sign,double angle)
    {
        var body=Body(0);var anchor=Fixed();
        var orientation=RigidRotation.FromRotationVector(Y*angle);
        var frame=new JointFrame(default,orientation);
        var axis=orientation.Apply(Z);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,frame,anchor,frame,
            ConnectedBodyCollision.Disabled,sign>0?new(-1,0):new(0,1),JointTravelDirection.Both);
        var before=new[]{body.Snapshot(),anchor.Snapshot()};
        foreach(var held in new[]{true,false})
        {
            var force=axis*(100*sign*(held?1:-1));
            var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,new(force,default)},{anchor.Id,default}};
            var prediction=AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[body,anchor],[joint],[],loads,.01,.01,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
            Near(held?default:force,prediction.Wrenches[body.Id].Force);
            Assert.Equal(before,new[]{body.Snapshot(),anchor.Snapshot()});
        }
    }
    private static readonly CollisionVector X=new(1,0,0),Y=new(0,1,0),Z=new(0,0,1);
    private static readonly JointFrame Origin=new(default,RigidRotation.Identity);
    private static PhysicsBody Body(int id,CollisionVector center=default,CollisionVector velocity=default,CollisionVector spin=default)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,spin,2,new(2,2,2));
    private static PhysicsBody Fixed()=>new(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
    private static void Near(CollisionVector expected,CollisionVector actual)=>
        Assert.InRange((expected-actual).Length,0,1e-8);
    private static IReadOnlyDictionary<PhysicsBodyId,BodyWrench> Solve(PhysicsJoint joint,BodyWrench first=default)
    {
        var bodies=joint.Bodies.ToArray();
        return AccelerationSolver.Solve(bodies,joint.AccelerationConstraints(joint.Bodies.ToArray().ToDictionary(body=>body.Id),1e-7,1e-8),[],
            bodies.ToDictionary(b=>b.Id,b=>b==bodies[0]?first:default),1e-9,out _,[],out _,out _,out _);
    }

    [Theory]
    [InlineData(2,false)]
    [InlineData(8,false)]
    [InlineData(32,false)]
    [InlineData(2,true)]
    [InlineData(8,true)]
    [InlineData(32,true)]
    public void CoupledClosedLoopDistributesExternalLoadWithoutMutatingBodies(int count,bool reverse)
    {
        var bodies=Enumerable.Range(0,count).Select(i=>Body(i,X*i)).ToArray();
        var before=bodies.Select(b=>b.Snapshot()).ToArray();
        var constraints=Enumerable.Range(0,count).Select(i=>new ConstraintAcceleration(
            new ConstraintGradient([new(bodies[i],X,default),new(bodies[(i+1)%count],-X,default)]),
            0,AccelerationRelation.Equal)).ToArray();
        if(reverse) Array.Reverse(constraints);
        var loads=bodies.ToDictionary(b=>b.Id,b=>b==bodies[0]?new BodyWrench(X*(count*2),default):default);
        var result=AccelerationSolver.Solve(bodies,constraints,[],loads,1e-9,out _,[],out _,out _,out _);
        foreach(var body in bodies)
        {
            Near(X*2,result[body.Id].Force);
            Near(default,result[body.Id].Torque);
        }
        Near(X*(count*2),result.Values.Aggregate(default(CollisionVector),(sum,w)=>sum+w.Force));
        Assert.Equal(before,bodies.Select(b=>b.Snapshot()).ToArray());
        var repeated=AccelerationSolver.Solve(bodies,constraints,[],loads,1e-9,out _,[],out _,out _,out _);
        foreach(var body in bodies) Assert.Equal(result[body.Id],repeated[body.Id]);
    }

    [Fact]
    public void RedundantAccelerationRowsRetainTheirConvectiveBias()
    {
        var a=Body(0);var b=Body(1);
        var row=new ConstraintAcceleration(new([new(a,X,default),new(b,-X,default)]),-2,AccelerationRelation.Equal);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,default},{b.Id,default}};
        var before=new[]{a.Snapshot(),b.Snapshot()};
        var result=AccelerationSolver.Solve([a,b],[row,row],[],loads,1e-10,out _,[],out _,out _,out _);
        Near(X*2,result[a.Id].Force);Near(-X*2,result[b.Id].Force);
        var conflicting=new ConstraintAcceleration(row.Gradient,-3,AccelerationRelation.Equal);
        Assert.Throws<InvalidOperationException>(()=>AccelerationSolver.Solve([a,b],[row,conflicting],[],loads,1e-10,out _,[],out _,out _,out _));
        Assert.Equal(before,new[]{a.Snapshot(),b.Snapshot()});
    }

    [Theory]
    [InlineData(JointTravelDirection.Positive,-1,true)]
    [InlineData(JointTravelDirection.Negative,1,true)]
    [InlineData(JointTravelDirection.Positive,1,false)]
    [InlineData(JointTravelDirection.Negative,-1,false)]
    public void RotatingRailSuppliesOnlyTheRequiredOneWayCentripetalForce(JointTravelDirection direction,int sign,bool held)
    {
        var head=Body(0,X*sign,Y*sign,Z);
        var rail=new PhysicsBody(new(1),PhysicsMotionType.Kinematic,RigidPose.Identity,default,Z);
        var frame=new JointFrame(default,RigidRotation.FromRotationVector(Y*(Math.PI/2)));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,head,frame,rail,frame,
            ConnectedBodyCollision.Disabled,null,direction);
        var before=head.Snapshot(); var railBefore=rail.Snapshot();
        var loads=Solve(joint);
        Near(held?X*(-2*sign):default,loads[head.Id].Force);
        Near(default,loads[head.Id].Torque); Assert.Equal(default,loads[rail.Id]);
        Assert.Equal(before,head.Snapshot()); Assert.Equal(railBefore,rail.Snapshot());
    }

    [Theory]
    [InlineData(FrameJointKind.BallSocket)]
    [InlineData(FrameJointKind.Hinge)]
    public void PivotSupportSuppliesCentripetalForceWithoutDoingInstantaneousWork(FrameJointKind kind)
    {
        var head=Body(0,X,Y*2,Z*2); var anchor=Fixed();
        var joint=new PhysicsFrameJoint(new(0),kind,head,new(-X,RigidRotation.Identity),anchor,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var load=Solve(joint)[head.Id];
        Near(-X*8,load.Force); Near(default,load.Torque);
        Assert.InRange(Math.Abs(CollisionVector.Dot(load.Force,head.LinearVelocity)+
            CollisionVector.Dot(load.Torque,head.AngularVelocity)),0,1e-9);
    }

    [Fact]
    public void DynamicParticipantsReceiveEqualAndOppositeReactions()
    {
        var a=Body(0,X,Y*2,Z*2); var b=Body(1);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.BallSocket,a,new(-X,RigidRotation.Identity),b,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var loads=Solve(joint);
        Near(-X*4,loads[a.Id].Force); Near(X*4,loads[b.Id].Force);
        Near(default,loads[a.Id].Force+loads[b.Id].Force);
        Near(default,loads[a.Id].Torque+CollisionVector.Cross(a.Center,loads[a.Id].Force)+
            loads[b.Id].Torque+CollisionVector.Cross(b.Center,loads[b.Id].Force));
    }

    [Theory]
    [InlineData(0,-8)]
    [InlineData(20,-8)]
    [InlineData(-20,-20)]
    public void RopeSuppliesTensionButNeverCompression(double applied,double expected)
    {
        var a=Body(0,X,Y*2); var b=Fixed();
        var joint=new PhysicsRopeJoint(new(0),new([new(a,default),new(b,default)]),1,ConnectedBodyCollision.Disabled);
        var load=Solve(joint,new(X*applied,default))[a.Id];
        Near(X*expected,load.Force); Near(default,load.Torque);
    }

    [Theory]
    [InlineData(2,0)]
    [InlineData(1,-1)]
    public void SlackOrSeparatingRopeDoesNotSupplySupport(double length,double radialSpeed)
    {
        var a=Body(0,X,X*radialSpeed+Y*2); var b=Fixed();
        var joint=new PhysicsRopeJoint(new(0),new([new(a,default),new(b,default)]),length,ConnectedBodyCollision.Disabled);
        Assert.Empty(joint.AccelerationConstraints(joint.Bodies.ToArray().ToDictionary(body=>body.Id),1e-7,1e-8));
        Assert.Equal(default,Solve(joint)[a.Id]);
    }

    [Theory]
    [InlineData(FrameJointKind.Slider)]
    [InlineData(FrameJointKind.Hinge)]
    public void TravelBiasMatchesSecondDifferenceAlongFixedSpinPaths(FrameJointKind kind)
    {
        var random=new Random(4371);
        double Number()=>2*random.NextDouble()-1;
        CollisionVector Vector()=>new(Number(),Number(),Number());
        for(var sample=0;sample<30;sample++)
        {
            var a=Body(0,Vector(),Vector(),Vector()*3);
            var b=Body(1,Vector(),Vector(),Vector()*3);
            var la=new JointFrame(Vector(),RigidRotation.FromRotationVector(Vector()));
            var lb=new JointFrame(Vector(),RigidRotation.FromRotationVector(Vector()));
            JointFrame At(PhysicsBody body,JointFrame local,double time)
            {
                var rotation=RigidRotation.FromRotationVector(body.AngularVelocity*time)*body.Pose.Rotation;
                return new(body.Center+body.LinearVelocity*time+rotation.Apply(local.Anchor),rotation*local.Orientation);
            }
            var equation=JointEquations.Travel(kind,a,b,At(a,la,0),At(b,lb,0));
            const double h=1e-4;
            var before=JointEquations.Travel(kind,a,b,At(a,la,-h),At(b,lb,-h)).Error-equation.Error;
            var after=JointEquations.Travel(kind,a,b,At(a,la,h),At(b,lb,h)).Error-equation.Error;
            if(kind==FrameJointKind.Hinge)
            {
                before=Math.IEEERemainder(before,Math.Tau); after=Math.IEEERemainder(after,Math.Tau);
            }
            var numeric=(before+after)/(h*h);
            Assert.InRange(Math.Abs(numeric-equation.ConvectiveAcceleration),0,(1+Math.Abs(numeric))*1e-5);
        }
    }

    [Fact]
    public void UnconstrainedLoadsRemainExactIncludingAnisotropicGyroscopicMotion()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,new(1,2,3),2,new(1,2,3));
        var load=new BodyWrench(new(1,2,3),new(.1,.2,.3));
        var before=body.Snapshot();
        var result=AccelerationSolver.Solve([body],[],[],new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,load}},1e-9,out _,[],out _,out _,out _);
        Assert.Equal(load,result[body.Id]); Assert.Equal(before,body.Snapshot());
    }

    [Fact]
    public void PredictionPreservesFreeLoadsAndRejectsForeignDeclarationsWithoutMutation()
    {
        var a=Body(0); var b=Fixed(); var before=a.Snapshot();
        var load=new BodyWrench(X*3,Z*.2);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,load},{b.Id,default}};
        var result=AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[a,b],[],[],loads,.01,.01,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        Assert.Equal(load,result.Wrenches[a.Id]); Assert.Equal(before,a.Snapshot());
        Assert.Equal(6,result.Coordinates);
        Assert.Equal(1,result.MidpointEvaluations);
        Assert.Equal(0,result.NewtonIterations);
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,a,Origin,b,Origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[Body(0),b],[joint],[],loads,.01,.01,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>()));
        Assert.Throws<ArgumentException>(()=>joint.Rebind(new Dictionary<PhysicsBodyId,PhysicsBody>{{a.Id,Body(5)},{b.Id,b}}));
        Assert.Throws<ArgumentNullException>(()=>joint.Rebind(null!));
        Assert.Equal(before,a.Snapshot());
    }

    [Fact]
    public void ReturnedFrictionPredictionSatisfiesItsOwnMidpointResidualWithoutMutation()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,1,0)),
            new(.8,0,.3),new(.2,.4,-.7),1,new(.1,.2,.3));
        var b=Fixed();
        var sphere=new ConvexInstance(new ConvexSphere(.5),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity));
        var floor=new ConvexInstance(new ConvexBox(new(4,.5,4)),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity));
        var pair=new PersistentContactPair(a,sphere,b,floor,new(0,0,.6),.01,.1,[]);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,new(new(0,-9.81,0),default)},{b.Id,default}};
        var beforeA=a.Snapshot(); var beforeB=b.Snapshot();
        PhysicsBody[] bodies=[a,b];
        var initialStates=bodies.ToDictionary(body=>body.Id);
        var initialPaths=new PredictionSamples(bodies).Capture(loads,.008);
        var initialContacts=pair.AccelerationContacts(initialStates,1e-8,initialPaths,0);
        Assert.Single(initialContacts);
        var prediction=AccelerationSolver.Predict(new PhysicsLoadSet {},new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[a,b],[],[pair],loads,.008,.008,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        const int wrenchCoordinates=6; // Three force and three torque components per dynamic body.
        const int contactCoordinates=3; // One normal and two tangent reactions per contact.
        Assert.Equal(wrenchCoordinates*bodies.Count(body=>body.MotionType==PhysicsMotionType.Dynamic)
            +contactCoordinates*initialContacts.Length,prediction.Coordinates);
        Assert.True(prediction.MidpointEvaluations>1);
        Assert.True(prediction.MidpointEvaluations>=1+prediction.NewtonIterations*(2*prediction.Coordinates+2));
        var states=prediction.Trajectories.ToDictionary(entry=>entry.Key,
            entry=>entry.Value.SampleBody(prediction.Duration*.5));
        var evaluated=AccelerationSolver.Solve(states.Values,[],
            pair.AccelerationContacts(states,1e-8,prediction.Trajectories,prediction.Duration*.5),loads,1e-10,out _,[],out _,out _,out _);
        var difference=evaluated[a.Id];
        Assert.InRange((difference.Force-prediction.Wrenches[a.Id].Force).Length*a.InverseMass,0,1e-9);
        Assert.InRange(a.InverseInertia(difference.Torque-prediction.Wrenches[a.Id].Torque).Length,0,1e-9);
        Assert.True(prediction.Wrenches[a.Id].Force.Y>-1); // Actual normal support, not a free-load identity.
        Assert.Equal(beforeA,a.Snapshot()); Assert.Equal(beforeB,b.Snapshot());
    }

    [Fact]
    public void InvalidOwnershipLoadsAndRelationsRejectWithoutPhysicalMutation()
    {
        var a=Body(0); var b=Fixed(); var before=a.Snapshot();
        var gradient=new ConstraintGradient([new(a,X,default),new(b,-X,default)]);
        Assert.Throws<ArgumentException>(()=>new ConstraintAcceleration(gradient,0,(AccelerationRelation)999));
        Assert.Throws<ArgumentException>(()=>new ConstraintAcceleration(gradient,double.NaN,AccelerationRelation.Equal));
        Assert.Throws<ArgumentNullException>(()=>new ConstraintAcceleration(null!,0,AccelerationRelation.Equal));
        var row=new ConstraintAcceleration(gradient,0,AccelerationRelation.Equal);
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,default},{b.Id,default}};
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Solve([a,a],[row],[],loads,1e-9,out _,[],out _,out _,out _));
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Solve([Body(0),b],[row],[],loads,1e-9,out _,[],out _,out _,out _));
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Solve([a],[row],[],loads,1e-9,out _,[],out _,out _,out _));
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Solve([a,b],[row],[],
            new Dictionary<PhysicsBodyId,BodyWrench>{{a.Id,default}},1e-9,out _,[],out _,out _,out _));
        loads[b.Id]=new(X,default);
        Assert.Throws<ArgumentException>(()=>AccelerationSolver.Solve([a,b],[row],[],loads,1e-9,out _,[],out _,out _,out _));
        Assert.Equal(before,a.Snapshot());
    }
}
