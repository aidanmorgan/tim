using System.Reflection;
using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class PhysicsMutationVisibilityTests
{
    public enum MutationEntry
    {
        ScalarSolve,BlockSolve,ContactSolve,ImpulseSolve,ContactWarmStart,PairWarmStart,
        PositionSolve,PositionApply,PositionEquations,FrameProject,RopeProject,TransmissionProject,
        PoweredApply,ConstrainedApply
    }
    private delegate double ApplyCorrection(ReadOnlySpan<BodyCorrection> corrections);
    private delegate void ProjectEquations(ReadOnlySpan<PositionEquation> equations,PositionProjector projector);

    public static TheoryData<MutationEntry> Entries()
    {
        var result=new TheoryData<MutationEntry>();
        foreach(var entry in Enum.GetValues<MutationEntry>())result.Add(entry);
        return result;
    }

    [Theory]
    [MemberData(nameof(Entries))]
    public void MutatingKernelsAreAssemblyInternal(MutationEntry entry)
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(2,0,0)),default,default);
        var gradient=ConstraintJacobian.AtPoint(a,b,new(1,0,0),new(1,0,0)).Bind(a,b);
        var row=new ImpulseConstraint(gradient,0,double.NegativeInfinity,double.PositiveInfinity);
        var block=new BilateralConstraintBlock([row]);
        var contact=new ContactConstraint(ContactKinematics.AtPoint(a,b,new(1,0,0),new(1,0,0)),0,0,.3);
        var shape=new ConvexInstance(new ConvexSphere(.1),AffineTransform.Identity);
        var pair=new PersistentContactPair(a,shape,b,shape,new(0,0,.3),.01,.1,[]);
        var projector=new PositionProjector([a,b],(_,_)=>[],1e-6);
        var origin=new JointFrame(default,RigidRotation.Identity);
        var hinge=new PhysicsFrameJoint(new(0),FrameJointKind.Hinge,a,origin,b,origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var output=new PhysicsFrameJoint(new(1),FrameJointKind.Hinge,a,origin,b,origin,
            ConnectedBodyCollision.Disabled,null,JointTravelDirection.Both);
        var rope=new PhysicsRopeJoint(new(2),new([new(a,default),new(b,default)]),3,ConnectedBodyCollision.Disabled);
        var transmission=new PhysicsTransmissionJoint(new(3),hinge,output,2,TransmissionEngagement.Engaged);
        Delegate method=entry switch
        {
            MutationEntry.ScalarSolve=>(Action)row.Solve,
            MutationEntry.BlockSolve=>(Action)block.Solve,
            MutationEntry.ContactSolve=>(Action)contact.Solve,
            MutationEntry.ImpulseSolve=>(Func<IReadOnlyList<IImpulseConstraint>,int,double,ImpulseSolveResult>)ImpulseSolver.Solve,
            MutationEntry.ContactWarmStart=>(Action<ContactImpulse>)contact.WarmStart,
            MutationEntry.PairWarmStart=>(Action)pair.WarmStart,
            MutationEntry.PositionSolve=>(Func<Func<IEnumerable<IPositionConstraint>>,PositionProjector,double,int,PositionSolveResult>)PositionSolver.Solve,
            MutationEntry.PositionApply=>(ApplyCorrection)projector.Apply,
            MutationEntry.PositionEquations=>(ProjectEquations)PositionEquations.Project,
            MutationEntry.FrameProject=>(Action<double,PositionProjector>)hinge.Project,
            MutationEntry.RopeProject=>(Action<double,PositionProjector>)rope.Project,
            MutationEntry.TransmissionProject=>(Action<double,PositionProjector>)transmission.Project,
            MutationEntry.PoweredApply=>(Func<ConstraintGradient,double,double,double,PoweredImpulseResult>)PoweredImpulse.Apply,
            MutationEntry.ConstrainedApply=>(Func<ConstraintGradient,double,double,double,IReadOnlyList<ImpulseResponseConstraint>,double,PoweredImpulseResult>)ConstrainedPoweredImpulse.Apply,
            _=>throw new ArgumentOutOfRangeException(nameof(entry))
        };
        // Method identity comes from compiler-checked delegates, not string
        // lookups that could silently inspect a different overload.
        Assert.True(method.Method.IsAssembly);
        Assert.False(method.Method.IsPublic);
        Assert.True(method.Method.GetBaseDefinition().IsAssembly);
    }

    [Theory]
    [InlineData(typeof(IImpulseConstraint),typeof(ImpulseConstraint))]
    [InlineData(typeof(IImpulseConstraint),typeof(BilateralConstraintBlock))]
    [InlineData(typeof(IImpulseConstraint),typeof(ContactConstraint))]
    [InlineData(typeof(IPositionConstraint),typeof(ContactPositionConstraint))]
    [InlineData(typeof(IPositionConstraint),typeof(PhysicsFrameJoint))]
    [InlineData(typeof(IPositionConstraint),typeof(PhysicsRopeJoint))]
    [InlineData(typeof(IPositionConstraint),typeof(PhysicsTransmissionJoint))]
    public void InterfaceDispatchDoesNotReExposeMutation(Type contract,Type implementation)
    {
        var mutator=Assert.Single(contract.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic),
            method=>method.ReturnType==typeof(void));
        Assert.True(mutator.IsAssembly);Assert.False(mutator.IsPublic);
        var map=implementation.GetInterfaceMap(contract);
        var index=Array.IndexOf(map.InterfaceMethods,mutator);
        Assert.True(index>=0);Assert.True(map.TargetMethods[index].IsPrivate);
    }

    [Fact]
    public void InternalContactProjectionAndExistingInternalHelpersStayNonpublic()
    {
        var a=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
        var b=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(2,0,0)),default,default);
        var shape=new ConvexInstance(new ConvexSphere(.1),AffineTransform.Identity);
        var contact=new ContactPositionConstraint(a,shape,b,shape);
        Action<double,PositionProjector> project=contact.Project;
        Assert.True(project.Method.IsAssembly);
        Assert.False(typeof(CoupledImpulsePair).IsVisible);
        Assert.False(typeof(PhysicsMotorBudget).IsVisible);
        var gradient=new ConstraintGradient([new(a,new(1,0,0),default)]);
        Action<double> apply=gradient.Apply;
        Assert.True(apply.Method.IsAssembly);
    }

    [Fact]
    public void GuardedWorldCommandsStillMutateAndReplayThroughTheSharedEngine()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,default,default,1,new(1,1,1));
        var geometry=new CompoundGeometry([new(new ConvexSphere(.1),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],[new(body,geometry,new(0,0,0))],[],new(default,maximumStep:.1));
        var initial=world.Capture();
        void Run()
        {
            world.ApplyImpulse(body.Id,new(1,0,0),body.Center);
            world.ApplyAngularImpulse(body.Id,new(0,0,1));
            var use=world.ApplyPoweredImpulse(body.Id,new(1,0,0),body.Center,2,1,1.5);
            Assert.InRange(Math.Abs(use.SuppliedWork-1.5),0,1e-12);
            Assert.Equal(new CollisionVector(2,0,0),body.LinearVelocity);
            world.Step([],[],.1);
            Assert.InRange(Math.Abs(body.Center.X-.2),0,1e-12);
        }
        Run();var final=world.Capture().BodyStates.ToArray();
        world.Restore(initial);Run();Assert.Equal(final,world.Capture().BodyStates.ToArray());
    }
}
