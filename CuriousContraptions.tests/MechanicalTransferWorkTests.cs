using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class MechanicalTransferWorkTests
{
    private static readonly CollisionVector Z=new(0,0,1);
    private static void Near(double expected,double actual)=>Assert.InRange(actual,expected-1e-9,expected+1e-9);

    [Theory]
    [InlineData(1,4,false)]
    [InlineData(2,4,false)]
    [InlineData(2,4,true)]
    [InlineData(1,-4,false)]
    [InlineData(2,-4,true)]
    public void AcceptedPredictionReportsSignedPortWorkAndLossWithoutMutation(int count,double receiverSpeed,bool reverse)
    {
        const double duration=.01;
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),Z*10,default,1,new(1,1,1));
        var receivers=Enumerable.Range(0,count).Select(i=>new PhysicsBody(new(i+2),PhysicsMotionType.Dynamic,
            RigidPose.At(new(i+4,0,0)),Z*receiverSpeed,default,1,new(1,1,1))).ToArray();
        var bodies=new[]{frame,source}.Concat(receivers).ToArray();
        var supply=new MechanicalTransferSource(new(7),new PointPowerPort(source.Id,frame.Id,default,Z),new(1000,100000));
        var transfers=receivers.Select((body,index)=>new MechanicalTransferLoad(new(index+10),supply,
            new PointPowerPort(body.Id,frame.Id,default,Z),new(2,100),1e-10)).ToArray();
        if(reverse){Array.Reverse(transfers);Array.Reverse(bodies);}
        var before=bodies.Select(body=>body.Snapshot()).ToArray();
        var forces=bodies.ToDictionary(body=>body.Id,_=>default(BodyWrench));
        ForcePrediction Predict()=>AccelerationSolver.Predict(new(){Transfers=transfers},
            new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),bodies,[],[],forces,duration,duration,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        var prediction=Predict();
        Assert.Equal(duration,prediction.Duration);Assert.Equal(count,prediction.Transfers.Count);
        var force=2*(10-receiverSpeed)/(1+(count+1)*duration);
        var sourceWork=force*(10-count*force*duration/2)*duration;
        var receiverWork=force*(receiverSpeed+force*duration/2)*duration;
        for(var i=0;i<count;i++)
        {
            var report=prediction.Transfers[i];
            Assert.Equal(new MechanicalSourceId(7),report.Source);
            Assert.Equal(new MechanicalTransferId(i+10),report.Transfer);
            Near(sourceWork,report.SourceExtraction.Supplied);
            Near(0,report.SourceExtraction.Dissipated);
            Near(Math.Max(0,receiverWork),report.ReceiverDelivery.Supplied);
            Near(Math.Max(0,-receiverWork),report.ReceiverDelivery.Dissipated);
            Near(0,report.PairedWork.Supplied);
            Near(sourceWork-receiverWork,report.PairedWork.Dissipated);
            Assert.Equal(0,report.SourceExtraction.SuppliedErrorBound);
            Assert.Equal(0,report.ReceiverDelivery.SuppliedErrorBound);
            Assert.Equal(0,report.PairedWork.DissipatedErrorBound);
        }
        var predictedSource=prediction.Trajectories[source.Id].SampleBody(duration);
        Near(count*sourceWork,source.KineticEnergy-predictedSource.KineticEnergy);
        foreach(var receiver in receivers)
            Near(receiverWork,prediction.Trajectories[receiver.Id].SampleBody(duration).KineticEnergy-receiver.KineticEnergy);
        Assert.Equal(before,bodies.Select(body=>body.Snapshot()).ToArray());
        Assert.Equal(prediction.Transfers,Predict().Transfers);
        Assert.Equal(prediction.TransferBodyImpulses,Predict().TransferBodyImpulses);
        foreach(var body in bodies.Where(body=>body.MotionType==PhysicsMotionType.Dynamic))
        {
            var reports=prediction.TransferBodyImpulses.Where(value=>value.Key.Body==body.Id);
            var linear=reports.Aggregate(default(CollisionVector),(sum,value)=>sum+value.Linear);
            Near(prediction.Wrenches[body.Id].Force.Z*duration,linear.Z);
        }
        var impulses=Assert.IsAssignableFrom<IList<MechanicalTransferBodyImpulse>>(prediction.TransferBodyImpulses);
        Assert.True(impulses.IsReadOnly);
        Assert.Throws<NotSupportedException>(()=>impulses[0]=default);
        var mutable=Assert.IsAssignableFrom<IList<PredictedTransferWork>>(prediction.Transfers);
        Assert.True(mutable.IsReadOnly);
        Assert.Throws<NotSupportedException>(()=>mutable[0]=default);
    }

    [Fact]
    public void UnrepresentablePerMeasurementToleranceRejects()
    {
        var port=new PointPowerPort(new(1),new(0),default,Z);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new MechanicalTransferLoad(new(0),
            new(new(0),port,new(1,1)),port,new(1,1),double.Epsilon));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-4)]
    public void LooseAccelerationToleranceCannotBypassTransferWorkBudget(double receiverSpeed)
    {
        const double duration=.01,workTolerance=1e-10;
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var source=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2,0,0)),Z*10,default,1,new(1,1,1));
        var receiver=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(new(4,0,0)),Z*receiverSpeed,default,1,new(1,1,1));
        var bodies=new[]{frame,source,receiver};
        var transfer=new MechanicalTransferLoad(new(0),
            new(new(0),new PointPowerPort(source.Id,frame.Id,default,Z),new(1000,100000)),
            new PointPowerPort(receiver.Id,frame.Id,default,Z),new(2,100),workTolerance);
        ForcePrediction Predict(PhysicsLoadSet loads)=>AccelerationSolver.Predict(loads,
            new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),bodies,[],[],
            bodies.ToDictionary(body=>body.Id,_=>default(BodyWrench)),duration,duration,1e-7,1e-8,10,[],
            new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>());
        var prediction=Predict(new(){Transfers=[transfer]});
        Assert.True(prediction.MidpointEvaluations>1);
        var residual=Assert.IsType<WrenchPathWorkResult>(prediction.TransferResidualWork);
        Assert.InRange(residual.Supplied+residual.Dissipated+residual.SuppliedErrorBound+residual.DissipatedErrorBound,
            0,workTolerance*.25);
        var force=2*(10-receiverSpeed)/(1+2*duration);
        Near(force,prediction.Wrenches[receiver.Id].Force.Z);
        Near(-force,prediction.Wrenches[source.Id].Force.Z);
        var report=Assert.Single(prediction.Transfers);
        var sourceChange=source.KineticEnergy-prediction.Trajectories[source.Id].SampleBody(duration).KineticEnergy;
        var receiverChange=prediction.Trajectories[receiver.Id].SampleBody(duration).KineticEnergy-receiver.KineticEnergy;
        Assert.InRange(Math.Abs(sourceChange-report.SourceExtraction.Supplied),0,workTolerance);
        Assert.InRange(Math.Abs(receiverChange-(report.ReceiverDelivery.Supplied-report.ReceiverDelivery.Dissipated)),0,workTolerance);
        Assert.Equal(prediction.TransferResidualWork,Predict(new(){Transfers=[transfer]}).TransferResidualWork);
        Assert.Null(Predict(new()).TransferResidualWork);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(.005)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidCompleteStepDurationRejects(double completeStepDuration)
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>AccelerationSolver.Predict(new(),
            new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),[],[],[],
            new Dictionary<PhysicsBodyId,BodyWrench>(),.01,completeStepDuration,1e-7,1e-8,1e-9,[],
            new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>()));
    }
}
