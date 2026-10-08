using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CompliantContactTests
{
    private static CompoundGeometry Sphere(double radius)=>new([new(new ConvexSphere(radius),AffineTransform.Identity)]);
    private static PhysicsBody Dynamic(int id,CollisionVector at,CollisionVector velocity,double mass)=>
        new(new(id),PhysicsMotionType.Dynamic,RigidPose.At(at),velocity,default,mass,new(1,1,1));

    [Theory]
    [InlineData(false,0)]
    [InlineData(true,0)]
    [InlineData(false,.2)]
    [InlineData(true,.2)]
    public void SharedComplianceBalancesReactionsEnergyAndExactReplay(bool movingFrame,double damping)
    {
        var body=Dynamic(0,new(0,.3,0),new(0,-.2,0),1);
        var frame=movingFrame?Dynamic(1,default,default,2):
            new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var geometry=Sphere(.1);
        var load=new CompliantContactLoad(body.Id,frame.Id,2,2,.3,.5,100,damping,CompliantContactInitialState.Preloaded);
        var world=new PhysicsWorld([],[new(body,geometry,new(0,0,0)),new(frame,Sphere(.001),new(0,0,0))],[],new(default,maximumStep:.001));
        world.ReplaceLoads(world.Loads with {Compliant=[load]});var initial=world.Capture();
        double Energy()
        {
            var depth=load.RestHeight-SupportFootprint.Sample(geometry,body.Pose,frame.Pose).LowestPoint.Y;
            return body.KineticEnergy+frame.KineticEnergy+.5*load.Stiffness*depth*depth;
        }
        void Run()
        {
            for(var i=0;i<50;i++)
            {
                var before=Energy();world.Step([],[],.001);var after=Energy();
                Assert.InRange(after,0,before+1e-9);
                if(damping==0)Assert.InRange(Math.Abs(after-before),0,1e-9);
                if(movingFrame)Assert.InRange((body.LinearVelocity+frame.LinearVelocity*2-new CollisionVector(0,-.2,0)).Length,0,1e-9);
            }
        }
        Run();Assert.True(body.LinearVelocity.Y>0);
        var final=world.Capture().BodyStates.ToArray();
        world.ReplaceLoads(world.Loads with {Compliant=[]});Assert.Empty(world.Loads.Compliant.ToArray());
        world.Restore(initial);Assert.Same(load,Assert.Single(world.Loads.Compliant.ToArray()));
        Run();Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Throws<ArgumentException>(()=>world.ReplaceLoads(world.Loads with {Compliant=[new(new(99),frame.Id,2,2,.3,.5,100,0,CompliantContactInitialState.Unloaded)]}));
        Assert.Same(load,Assert.Single(world.Loads.Compliant.ToArray()));
    }

    [Fact]
    public void OutsideSeparatedAndFastSeparatingContactsCannotAttract()
    {
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var geometry=Sphere(.1);
        var load=new CompliantContactLoad(new(0),frame.Id,1,1,.3,.5,100,.2,CompliantContactInitialState.Unloaded);
        foreach(var body in new[]{
            Dynamic(0,new(2,.3,0),default,1),
            Dynamic(0,new(0,1,0),default,1),
            Dynamic(0,new(0,.3,0),new(0,100,0),1)})
        {
            var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>{{body.Id,body},{frame.Id,frame}};
            var result=load.Evaluate(geometry,bodies,bodies,bodies);
            Assert.Equal(default(BodyWrench),result.Body);Assert.Equal(default(BodyWrench),result.Frame);
        }
    }

    [Fact]
    public void CompressionPotentialWorkMatchesAcrossUnloadedAndSaturatedBoundaries()
    {
        var law=new CompressionSpringPotential(120,.5);
        foreach(var first in new[]{-.7,0,.1,.5,.8})
        foreach(var last in new[]{-.7,0,.1,.5,.8})
        {
            var work=law.IntervalForce(first,last)*(last-first);
            Assert.InRange(Math.Abs(work-law.Energy(last)+law.Energy(first)),0,1e-12);
            Assert.Equal(law.IntervalForce(first,last),law.IntervalForce(last,first));
        }
        Assert.Equal(0,law.IntervalForce(-1,-.1));
        Assert.Equal(60,law.IntervalForce(.5,1));
        Assert.Equal(12,law.IntervalForce(.1,.1),12);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CompressionSpringPotential(0,.5));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CompressionSpringPotential(120,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>law.Energy(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(()=>law.IntervalForce(double.NaN,0));
    }


    [Theory]
    [InlineData(.05)]
    [InlineData(.1)]
    [InlineData(.2)]
    public void ColliderReplacementResolvesCurrentGeometryAndRestoresExactReplay(double radius)
    {
        var body=Dynamic(0,new(0,.45,0),default,1);
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var original=Sphere(.1);var replacement=Sphere(radius);
        var load=new CompliantContactLoad(body.Id,frame.Id,1,1,.3,.5,100,0,CompliantContactInitialState.Unloaded);
        var world=new PhysicsWorld([],[new(body,original,new(0,0,0)),new(frame,Sphere(.001),new(0,0,0))],
            [],new(default,maximumStep:.001));
        world.ReplaceLoads(world.Loads with {Compliant=[load]});var initial=world.Capture();
        world.Step([],[],.001);
        Assert.Equal(default,body.LinearVelocity);
        world.Restore(initial);
        void ReplaceAndRun()
        {
            world.ApplyColliderUpdates([new(body.Id,replacement,new(0,0,0),CollisionParticipation.Enabled)]);
            Assert.Same(load,Assert.Single(world.Loads.Compliant.ToArray()));
            Assert.Same(replacement,world.Collider(body.Id).Declaration.Geometry);
            world.Step([],[],.001);
            var expected=100*Math.Max(0,.3-(.45-radius))*.001/(1+100*.001*.001/4);
            Assert.InRange(Math.Abs(body.LinearVelocity.Y-expected),0,1e-10);
        }
        ReplaceAndRun();var final=world.Capture().BodyStates.ToArray();
        var revision=world.Collider(body.Id).Revision;
        world.Restore(initial);
        Assert.Same(original,world.Collider(body.Id).Declaration.Geometry);
        Assert.Equal(new PhysicsColliderRevision(0),world.Collider(body.Id).Revision);
        world.Step([],[],.001);Assert.Equal(default,body.LinearVelocity);
        world.Restore(initial);ReplaceAndRun();
        Assert.Equal(final,world.Capture().BodyStates.ToArray());
        Assert.Equal(revision,world.Collider(body.Id).Revision);
    }

    [Fact]
    public void PredictionRejectsMissingOrNullCurrentGeometry()
    {
        var body=Dynamic(0,new(0,.3,0),default,1);
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var load=new CompliantContactLoad(body.Id,frame.Id,1,1,.3,.5,100,0,CompliantContactInitialState.Unloaded);
        var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>{{body.Id,body},{frame.Id,frame}};
        Assert.Throws<ArgumentNullException>(()=>load.Evaluate(null!,bodies,bodies,bodies));
        var loads=new Dictionary<PhysicsBodyId,BodyWrench>{{body.Id,default}};
        foreach(var geometry in new[]{
            new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>(),
            new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>{{body.Id,default}}})
            Assert.Throws<ArgumentException>(()=>AccelerationSolver.Predict(new PhysicsLoadSet {Compliant=[load]},geometry,
                [body,frame],[],[],loads,.001,.001,1e-7,1e-8,1e-9,[],new Dictionary<PhysicsBodyId,PhysicsEnergyStoreState>(), new Dictionary<PhysicsGasNodeId,AxialGasPotential>()));
    }

    [Fact]
    public void InvalidDeclarationsReject()
    {
        Assert.Throws<ArgumentException>(()=>new CompliantContactLoad(new(0),new(0),1,1,0,1,100,0,CompliantContactInitialState.Unloaded));
        foreach(var value in new[]{0d,-1,double.NaN,double.PositiveInfinity})
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CompliantContactLoad(new(0),new(1),value,1,0,1,100,0,CompliantContactInitialState.Unloaded));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CompliantContactLoad(new(0),new(1),1,value,0,1,100,0,CompliantContactInitialState.Unloaded));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CompliantContactLoad(new(0),new(1),1,1,0,value,100,0,CompliantContactInitialState.Unloaded));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new CompliantContactLoad(new(0),new(1),1,1,0,1,value,0,CompliantContactInitialState.Unloaded));
        }
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CompliantContactLoad(new(0),new(1),1,1,double.NaN,1,100,0,CompliantContactInitialState.Unloaded));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CompliantContactLoad(new(0),new(1),1,1,0,1,100,-1,CompliantContactInitialState.Unloaded));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CompliantContactLoad(new(0),new(1),1,1,0,1,100,0,(CompliantContactInitialState)(-1)));
        var load=new CompliantContactLoad(new(0),new(1),1,1,0,1,100,0,CompliantContactInitialState.Unloaded);
        Assert.Throws<ArgumentException>(()=>load.Validate(new Dictionary<PhysicsBodyId,PhysicsBody>()));
    }
}
