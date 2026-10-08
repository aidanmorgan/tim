using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsLoadSetTests
{
    public enum LoadFamily { Effort, Elastic, Damping, Drag, Compliant, Guide, Spring, Transfer, TransferField }

    private sealed record Fixture(PhysicsWorld World,PhysicsBody Body,PhysicsBody Frame,
        PhysicsFrameJoint Joint,PhysicsLoadSet Loads);
    private static Fixture Create()
    {
        var frame=new PhysicsBody(new(0),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(new(2,1,0)),new(1,0,0),default,1,new(1,1,1));
        var orientation=RigidRotation.FromRotationVector(new(0,Math.PI/2,0));
        var joint=new PhysicsFrameJoint(new(0),FrameJointKind.Slider,body,new(default,orientation),
            frame,new(new(2,1,0),orientation),ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        PhysicsObject Object(PhysicsBody value)=>new(value,new([new(new ConvexSphere(.01),AffineTransform.Identity)]),new(0,0,0));
        var world=new PhysicsWorld([],[Object(body),Object(frame)],[joint],new(default,maximumStep:.05));
        world.InstallEnergyStores([new(frame.Id,100,100)]);
        var loads=new PhysicsLoadSet
        {
            Efforts=[new ConstantAxialEffortLoad(joint.Id,joint.Kind,2)],
            Elastic=[new(joint.Id,joint.Kind,new(1,0))],
            Damping=[new(joint.Id,joint.Kind,1,1)],
            Drag=[new(body.Id,.2,0)],
            Compliant=[new(body.Id,frame.Id,10,10,-10,.5,100,0,CompliantContactInitialState.Unloaded)],
            Guides=[new(body.Id,frame.Id,10,20,10,10,1,9)],
            Transfers=[Jet(frame.Id,body.Id)]
        };
        world.ReplaceLoads(loads);
        return new(world,body,frame,joint,loads);
    }

    private static MechanicalTransferLoad Jet(PhysicsBodyId source,PhysicsBodyId receiver)=>
        new NozzleCoupledJetReceiver().CreateLoad(new(0),
            new(new StoredFlowSource(new(0),source,12,new(2,24))),
            new(source,receiver,default,new(1,0,0),default,10,4,[]),new(2,2),1e-10);

    private static PhysicsLoadSet Invalid(Fixture fixture,LoadFamily family)
    {
        var candidate=fixture.Loads with
        {
            Efforts=[new ConstantAxialEffortLoad(fixture.Joint.Id,fixture.Joint.Kind,30)],
            Elastic=[],Damping=[],Drag=[],Compliant=[],Guides=[],Transfers=[]
        };
        return family switch
        {
            LoadFamily.Transfer=>candidate with {Transfers=[new(new(0),new(new(0),new AxialPowerPort(new(99),FrameJointKind.Slider,1),new(1000,100000)),new AxialPowerPort(fixture.Joint.Id,fixture.Joint.Kind,1),new(1,1),1e-10)]},
            LoadFamily.Effort=>candidate with {Efforts=[new ConstantAxialEffortLoad(new(99),FrameJointKind.Slider,30)]},
            LoadFamily.Elastic=>candidate with {Elastic=[new(new(99),FrameJointKind.Slider,new(1,0))]},
            LoadFamily.Damping=>candidate with {Damping=[new(new(99),FrameJointKind.Slider,1,1)]},
            LoadFamily.Drag=>candidate with {Drag=[new(new(99),1,1)]},
            LoadFamily.Compliant=>candidate with {Compliant=[new(new(99),fixture.Frame.Id,1,1,0,.5,100,0,CompliantContactInitialState.Unloaded)]},
            LoadFamily.Guide=>candidate with {Guides=[new(new(99),fixture.Frame.Id,0,1,1,1,1,-1)]},
            LoadFamily.Spring=>candidate with {Springs=[new(new(99),new(100),1,0,1)]},
            LoadFamily.TransferField=>candidate with {Transfers=[Jet(fixture.Frame.Id,new(99))]},
            _=>throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    [Theory]
    [InlineData(LoadFamily.Transfer)]
    [InlineData(LoadFamily.Effort)]
    [InlineData(LoadFamily.Elastic)]
    [InlineData(LoadFamily.Damping)]
    [InlineData(LoadFamily.Drag)]
    [InlineData(LoadFamily.Compliant)]
    [InlineData(LoadFamily.Guide)]
    [InlineData(LoadFamily.TransferField)]
    [InlineData(LoadFamily.Spring)]
    public void InvalidFamilyRejectsTheEntireReplacementBeforeMutation(LoadFamily family)
    {
        var fixture=Create();var world=fixture.World;var before=world.Capture();
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(Invalid(fixture,family)));
        Assert.Same(fixture.Loads,world.Loads);
        Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(before.Time,world.Time);Assert.Equal(before.StepIndex,world.StepIndex);
        Assert.Equal(PhysicsWorldPhase.Idle,world.Phase);
        world.Step([],[],.05);
        AssertCombinedResponse(fixture);
    }

    private static void AssertCombinedResponse(Fixture fixture)
    {
        // I=mass=1, constant effort 2 plus finite-source saturated jet 2, stiffness 1,
        // damping 1 plus drag .2. Guide and membrane are declared but disengaged.
        const double h=.05;
        var coefficient=h*.6+h*h*.25;
        var velocity=(1-coefficient+4*h)/(1+coefficient);
        Assert.InRange(Math.Abs(fixture.Body.LinearVelocity.X-velocity),0,1e-9);
        Assert.InRange(Math.Abs(fixture.Body.Center.X-2-h*(1+velocity)*.5),0,1e-9);
    }

    [Fact]
    public void SnapshotRestoresTheWholeSetAndItsCoupledResponseExactly()
    {
        var fixture=Create();var world=fixture.World;var initial=world.Capture();
        var result=world.Step([],[],.05);AssertCombinedResponse(fixture);
        Assert.InRange(Math.Abs(world.EnergyStore(fixture.Frame.Id).ReleasedEnergy-1.2),0,1e-10);
        var final=world.Capture();
        world.ReplaceLoads(new());Assert.True(world.Loads.IsEmpty);
        world.Restore(initial);Assert.Same(fixture.Loads,world.Loads);
        Assert.Equal(result,world.Step([],[],.05));
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(final.EnergyStates.ToArray(),world.Capture().EnergyStates.ToArray());
        Assert.Equal(final.TransferTotals.ToArray(),world.TransferTotals.ToArray());
        world.Restore(initial);world.ReplaceLoads(new());
        world.Step([],[],.05);
        Assert.InRange(Math.Abs(fixture.Body.LinearVelocity.X-1),0,1e-12);
        Assert.Throws<ArgumentNullException>(()=>world.ReplaceLoads(null!));
        Assert.True(world.Loads.IsEmpty);
    }

    [Fact]
    public void AllCollectionsAreCopiedAndWithExpressionsCannotMutatePriorSets()
    {
        var fixture=Create();var source=fixture.Loads;
        var transfers=source.Transfers.ToArray();
        var efforts=source.Efforts.ToArray();var elastic=source.Elastic.ToArray();
        var damping=source.Damping.ToArray();var drag=source.Drag.ToArray();
        var compliant=source.Compliant.ToArray();var guides=source.Guides.ToArray();
        var copy=new PhysicsLoadSet
        {
            Efforts=efforts,Elastic=elastic,Damping=damping,Drag=drag,
            Compliant=compliant,Guides=guides,Transfers=transfers
        };
        transfers[0]=null!;
        efforts[0]=null!;elastic[0]=null!;damping[0]=null!;drag[0]=null!;
        compliant[0]=null!;guides[0]=null!;
        static void Check<T>(IReadOnlyList<T> values,T expected) where T:class
        {
            Assert.Same(expected,Assert.Single(values));
            var mutable=Assert.IsAssignableFrom<IList<T>>(values);
            Assert.True(mutable.IsReadOnly);
            Assert.Throws<NotSupportedException>(()=>mutable[0]=null!);
        }
        Check(copy.Transfers,source.Transfers[0]);
        Check(copy.Efforts,source.Efforts[0]);Check(copy.Elastic,source.Elastic[0]);
        Check(copy.Damping,source.Damping[0]);Check(copy.Drag,source.Drag[0]);
        Check(copy.Compliant,source.Compliant[0]);Check(copy.Guides,source.Guides[0]);
        var changed=copy with {Efforts=[],Transfers=[]};
        Assert.Empty(changed.Transfers);Assert.Single(copy.Transfers);
        Assert.Empty(changed.Efforts);
        Assert.Single(copy.Efforts);Assert.False(changed.IsEmpty);
        fixture.World.ReplaceLoads(copy);fixture.World.Step([],[],.05);AssertCombinedResponse(fixture);
    }

    [Theory]
    [InlineData(LoadFamily.Transfer)]
    [InlineData(LoadFamily.Effort)]
    [InlineData(LoadFamily.Elastic)]
    [InlineData(LoadFamily.Damping)]
    [InlineData(LoadFamily.Drag)]
    [InlineData(LoadFamily.Compliant)]
    [InlineData(LoadFamily.Guide)]
    [InlineData(LoadFamily.Spring)]
    public void NullCollectionsAndMembersRejectAtConstruction(LoadFamily family)
    {
        PhysicsLoadSet InvalidNull(bool collection)=>family switch
        {
            LoadFamily.Transfer=>new(){Transfers=collection?null!:[null!]},
            LoadFamily.Effort=>new(){Efforts=collection?null!:[null!]},
            LoadFamily.Elastic=>new(){Elastic=collection?null!:[null!]},
            LoadFamily.Damping=>new(){Damping=collection?null!:[null!]},
            LoadFamily.Drag=>new(){Drag=collection?null!:[null!]},
            LoadFamily.Compliant=>new(){Compliant=collection?null!:[null!]},
            LoadFamily.Guide=>new(){Guides=collection?null!:[null!]},
            LoadFamily.Spring=>new(){Springs=collection?null!:[null!]},
            _=>throw new ArgumentOutOfRangeException(nameof(family))
        };
        Assert.Throws<ArgumentNullException>(()=>InvalidNull(true));
        Assert.Throws<ArgumentNullException>(()=>InvalidNull(false));
    }
}
