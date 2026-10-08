using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class AirflowConservationTests
{
    public enum ReceiverKind { TranslatingBody, AxialRotor }
    private static readonly CollisionVector Y=new(0,1,0),Z=new(0,0,1);
    private static PhysicsObject Object(PhysicsBody body)=>new(body,
        new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));

    [Theory]
    [InlineData(ReceiverKind.TranslatingBody,1,.4,false,10)]
    [InlineData(ReceiverKind.TranslatingBody,2,.4,false,10)]
    [InlineData(ReceiverKind.TranslatingBody,2,.4,true,10)]
    [InlineData(ReceiverKind.AxialRotor,1,.4,false,6)]
    [InlineData(ReceiverKind.AxialRotor,2,.4,false,6)]
    [InlineData(ReceiverKind.AxialRotor,2,.4,true,6)]
    [InlineData(ReceiverKind.TranslatingBody,1,0,false,10)]
    [InlineData(ReceiverKind.AxialRotor,1,0,false,6)]
    [InlineData(ReceiverKind.TranslatingBody,2,.4,false,0)]
    [InlineData(ReceiverKind.TranslatingBody,2,.4,true,0)]
    public void CompressionCannotCreateNetMechanicalEnergyAcrossItsReceivers(
        ReceiverKind kind,int receiverCount,double compressionSpeed,bool reverse,double receiverSpeed)
    {
        var source=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.At(-Z*2),default,default);
        var plate=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(source.Center+Y),
            -Y*compressionSpeed,default,1,new(1,1,1));
        var axis=RigidRotation.FromRotationVector(new(-Math.PI/2,0,0));
        var guide=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,plate,new(default,axis),
            source,new(Y,axis),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var objects=new List<PhysicsObject>{Object(source),Object(plate)};
        var joints=new List<PhysicsJoint>{guide};
        var receivers=new List<PhysicsBody>();
        var transfers=new List<MechanicalTransferLoad>();
        var rotary=new List<RotaryCaptureDeclaration>();
        var supply=new MechanicalTransferSource(new(0),new AxialPowerPort(guide.Id,FrameJointKind.Slider,-12/.8),
            new(18,18*12));
        var impedance=new JetTransferImpedance(18.0/12,18);
        for(var i=0;i<receiverCount;i++)
        {
            var center=new CollisionVector((i-(receiverCount-1)*.5)*.4,0,0);
            var receiver=new PhysicsBody(new(2+2*i),PhysicsMotionType.Dynamic,RigidPose.At(center),
                kind==ReceiverKind.TranslatingBody?Z*receiverSpeed:default,
                kind==ReceiverKind.AxialRotor?Z*receiverSpeed:default,1,new(1,1,1));
            objects.Add(Object(receiver));receivers.Add(receiver);
            switch(kind)
            {
                case ReceiverKind.TranslatingBody:
                    transfers.Add(new NozzleCoupledJetReceiver().CreateLoad(new(i),supply,
                        new(source.Id,receiver.Id,default,Z,default,5,2,[plate.Id]),impedance,1e-8));
                    break;
                case ReceiverKind.AxialRotor:
                    var carrier=new PhysicsBody(new(3+2*i),PhysicsMotionType.Static,RigidPose.At(center),default,default);
                    objects.Add(Object(carrier));
                    var frame=new JointFrame(default,RigidRotation.Identity);
                    var hinge=new PhysicsFrameJoint(new(i+1),FrameJointKind.Hinge,receiver,frame,carrier,frame,
                        ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
                    joints.Add(hinge);
                    var field=new AirJetGeometry(source.Id,carrier.Id,default,Z,default,5,2,[plate.Id,receiver.Id]);
                    rotary.Add(new(hinge.Id,new(.4,4,12,.05),
                        [new(new(i),supply,field,impedance,1e-8)],1e-7));
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
        if(reverse) { objects.Reverse();joints.Reverse();transfers.Reverse();rotary.Reverse(); }
        var world=new PhysicsWorld([],objects,joints,new(default,maximumStep:.01));
        world.ReplaceLoads(world.Loads with
        {
            Transfers=transfers,
            Rotary=rotary
        });
        var initial=world.Capture();
        var before=objects.Sum(o=>o.Body.KineticEnergy);
        var plateBefore=plate.KineticEnergy;
        var receiverBefore=receivers.Sum(b=>b.KineticEnergy);
        world.Step([],[],.01);
        var after=objects.Sum(o=>o.Body.KineticEnergy);
        var receiverGain=receivers.Sum(b=>b.KineticEnergy)-receiverBefore;
        var extracted=plateBefore-plate.KineticEnergy;
        Console.WriteLine($"Airflow work: kind={kind}, count={receiverCount}, compression={compressionSpeed:R}, reverse={reverse}, sourceLoss={extracted:R}, receiverGain={receiverGain:R}, netGain={after-before:R}");
        Assert.InRange(after-before,double.NegativeInfinity,1e-8);
        Assert.InRange(world.TransferError.UpperBound,0,1e-8);
        var totals=world.TransferTotals.ToArray();
        Assert.Equal(receiverCount,totals.Length);
        if(compressionSpeed==0||kind==ReceiverKind.TranslatingBody&&receiverSpeed>12/.8*compressionSpeed)
        {
            Assert.Equal(0,extracted);
            Assert.True(receiverGain<=1e-8);
            Assert.All(totals,total=>Assert.Equal(0,total.SourceExtraction.Supplied));
        }
        else
        {
            Assert.True(extracted>0);
            Assert.True(receiverGain>0);
            Assert.True(receiverGain<=extracted+1e-8);
            Assert.All(totals,total=>Assert.True(total.SourceExtraction.Supplied>0));
        }
        var final=world.Capture();
        world.Restore(initial);world.Step([],[],.01);
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        Assert.Equal(final.SourceTotals.ToArray(),world.SourceTotals.ToArray());
    }
}
