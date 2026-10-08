using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

/// <summary>Physical requirements migrated from the removed isolated sphere
/// impulse solver. The shared contact rows/world are the only execution path.</summary>
public class SharedImpactRequirementsTests
{
    private static readonly CollisionVector Left=new(-1,0,0);
    private static PhysicsBody Body(int id,CollisionVector center,CollisionVector velocity,double mass=1)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(center),velocity,default,mass,new(.1,.1,.1));
    private static PhysicsObject Object(PhysicsBody body,double restitution)=>new(body,
        new([new(new ConvexSphere(.5),AffineTransform.Identity)]),new(restitution,0,0));
    private static void Near(CollisionVector expected,CollisionVector actual,double tolerance=1e-8)=>
        Assert.InRange((expected-actual).Length,0,tolerance);

    [Theory]
    [InlineData(1,1,1)]
    [InlineData(1,4,1)]
    [InlineData(4,1,0)]
    [InlineData(.5,8,.4)]
    public void NormalContactConservesMomentumCannotAddEnergyAndLeavesTangentialVelocity(double aMass,double bMass,double bounce)
    {
        var va=new CollisionVector(10,2,3); var vb=new CollisionVector(-4,1,-2);
        var a=Body(0,Left*.5,va,aMass); var b=Body(1,-Left*.5,vb,bMass);
        var momentum=va*aMass+vb*bMass; var energy=a.KineticEnergy+b.KineticEnergy;
        var row=ImpulseConstraint.Contact(a,b,default,Left,bounce,0);
        ImpulseSolver.Solve([row]);
        Near(momentum,a.LinearVelocity*aMass+b.LinearVelocity*bMass);
        Assert.InRange(a.KineticEnergy+b.KineticEnergy,0,energy+1e-8);
        Assert.InRange(Math.Abs(b.LinearVelocity.X-a.LinearVelocity.X-14*bounce),0,1e-8);
        Assert.Equal(va.Y,a.LinearVelocity.Y); Assert.Equal(va.Z,a.LinearVelocity.Z);
        Assert.Equal(vb.Y,b.LinearVelocity.Y); Assert.Equal(vb.Z,b.LinearVelocity.Z);
        Near(default,a.AngularMomentum); Near(default,b.AngularMomentum);
        // Independent one-dimensional conservation/restitution oracle.
        var expectedImpulse=(1+bounce)*14/(1/aMass+1/bMass);
        Assert.InRange(Math.Abs(row.AccumulatedImpulse-expectedImpulse),0,1e-8);
    }

    [Theory]
    [InlineData(.5)]
    [InlineData(.01)]
    public void WorldFindsImpactAndConsumesRemainingTimeWithoutExternalSphereSweep(double maximumStep)
    {
        var a=Body(0,new(-5,0,0),new(20,0,0));
        var b=Body(1,new(5,0,0),new(-20,0,0));
        var world=new PhysicsWorld([],[Object(a,1),Object(b,1)],[],new(default,maximumStep:maximumStep));
        var before=world.Capture();
        world.Step([],[],.5);
        Near(new(-20,0,0),a.LinearVelocity); Near(new(20,0,0),b.LinearVelocity);
        Assert.InRange(a.Center.X,-6.0001,-5.9999);
        Assert.InRange(b.Center.X,5.9999,6.0001);
        Assert.InRange((a.Center-b.Center).Length,11.999,12.001);
        Near(default,a.LinearVelocity+b.LinearVelocity);
        Assert.InRange(Math.Abs(a.KineticEnergy+b.KineticEnergy-400),0,1e-8);
        var after=world.Capture();
        world.Restore(before); world.Step([],[],.5);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(after.Time,world.Time);
    }

    [Fact]
    public void SwappingContactParticipantsPreservesThePhysicalResult()
    {
        var a=Body(0,Left*.5,new(10,2,0),2); var b=Body(1,-Left*.5,new(-4,0,0),4);
        var beforeA=a.Snapshot(); var beforeB=b.Snapshot();
        ImpulseSolver.Solve([ImpulseConstraint.Contact(a,b,default,Left,.5,0)]);
        var firstA=a.Snapshot(); var firstB=b.Snapshot();
        a.Restore(beforeA); b.Restore(beforeB);
        ImpulseSolver.Solve([ImpulseConstraint.Contact(b,a,default,-Left,.5,0)]);
        Assert.Equal(firstA,a.Snapshot()); Assert.Equal(firstB,b.Snapshot());
    }

    [Fact]
    public void SeparatingContactAppliesNoAttractiveImpulse()
    {
        var a=Body(0,Left*.5,Left); var b=Body(1,-Left*.5,-Left);
        var row=ImpulseConstraint.Contact(a,b,default,Left,1,0);
        ImpulseSolver.Solve([row]);
        Assert.Equal(0,row.AccumulatedImpulse);
        Near(Left,a.LinearVelocity); Near(-Left,b.LinearVelocity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThreeTouchingBodiesRequireOneCoupledAnswer(bool reverseIds)
    {
        var first=Body(reverseIds?9:2,new(-1,0,0),new(3,0,0));
        var middle=Body(5,default,default);
        var last=Body(reverseIds?2:9,new(1,0,0),default);
        var world=new PhysicsWorld([],[Object(last,0),Object(first,0),Object(middle,0)],[],new(default,maximumStep:.1));
        world.Step([],[],.1);
        foreach(var body in new[]{first,middle,last})
        {
            Near(new(1,0,0),body.LinearVelocity);
            Near(default,body.AngularMomentum);
        }
        Near(new(3,0,0),first.LinearVelocity+middle.LinearVelocity+last.LinearVelocity);
        Assert.InRange(Math.Abs(first.KineticEnergy+middle.KineticEnergy+last.KineticEnergy-1.5),0,1e-8);
        Assert.InRange(middle.Center.X-first.Center.X,1-1e-7,1+1e-7);
        Assert.InRange(last.Center.X-middle.Center.X,1-1e-7,1+1e-7);
    }

    [Fact]
    public void PhysicalDeclarationsRejectInvalidMassVelocityRestitutionAndNormal()
    {
        Assert.Throws<ArgumentException>(()=>Body(0,default,default,0));
        Assert.Throws<ArgumentException>(()=>Body(1,default,default,double.NaN));
        Assert.Throws<ArgumentException>(()=>Body(0,default,new(double.NaN,0,0)));
        var a=Body(0,Left,default); var b=Body(1,-Left,default);
        Assert.Throws<ArgumentOutOfRangeException>(()=>ImpulseConstraint.Contact(a,b,default,Left,1.1,0));
        Assert.Throws<ArgumentException>(()=>ImpulseConstraint.Contact(a,b,default,Left*2,1,0));
    }
}
