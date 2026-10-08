using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CoupledFrictionContactTests
{
    [Theory]
    [InlineData(.11609291412523,.3,false)]
    [InlineData(.11609291412523,.3,true)]
    [InlineData(.02,.3,false)]
    [InlineData(.3,.3,false)]
    [InlineData(.11609291412523,.1,false)]
    [InlineData(.11609291412523,.8,false)]
    public void SymmetricSphereImpactMatchesIndependentSlidingSolution(double sine,double friction,bool reverse)
    {
        const double radius=.34,restitution=.055;
        var cosine=Math.Sqrt(1-sine*sine);
        var inertia=.4*radius*radius;
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            new(-.79,-1,0),default,1,new(inertia,inertia,inertia));
        var wall=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var firstNormal=new CollisionVector(cosine,0,-sine);
        var secondNormal=new CollisionVector(cosine,0,sine);
        var first=new ContactConstraint(ContactKinematics.AtPoint(body,wall,-firstNormal*radius,firstNormal),restitution,0,friction);
        var second=new ContactConstraint(ContactKinematics.AtPoint(body,wall,-secondNormal*radius,secondNormal),restitution,0,friction);
        var initial=body.Snapshot();
        var initialEnergy=body.KineticEnergy;

        // Symmetry fixes final vx by restitution, vz=wx=wy=0. Both contacts
        // slide because their opposite U speeds are nonzero. Reduce the
        // circular friction law to one scalar equation for the common V slip.
        var finalX=.79*restitution;
        var sidewaysSlip=sine*finalX;
        var verticalResponse=2+2*radius*radius*cosine*cosine/inertia;
        (double Normal,double Side,double Vertical) Impulses(double slip)
        {
            var length=Math.Sqrt(sidewaysSlip*sidewaysSlip+slip*slip);
            var normal=(.79+finalX)/(2*(cosine-friction*sine*sidewaysSlip/length));
            return (normal,friction*normal*sidewaysSlip/length,-friction*normal*slip/length);
        }
        double low=0,high=1;
        for(var iteration=0;iteration<100;iteration++)
        {
            var slip=(low+high)*.5;
            var expected=Impulses(slip);
            if(slip-1-verticalResponse*expected.Vertical>0) high=slip;
            else low=slip;
        }
        var impulse=Impulses((low+high)*.5);
        var expectedVelocity=new CollisionVector(finalX,-1-2*impulse.Vertical,0);
        var expectedAngular=new CollisionVector(0,0,2*radius*cosine*impulse.Vertical/inertia);
        void Verify(ContactConstraint a,ContactConstraint b)
        {
            IImpulseConstraint[] rows=reverse?[b,a]:[a,b];
            // Resolve impulses as well as velocities in the shallow-angle case.
            var result=ImpulseSolver.Solve(rows,tolerance:1e-10);
            Console.WriteLine($"sine={sine:R}, friction={friction:R}, reverse={reverse}, iterations={result.Iterations}, residual={result.MaximumResidual:R}");
            Assert.InRange(result.MaximumResidual,0,1e-10);
            Assert.InRange((body.LinearVelocity-expectedVelocity).Length,0,2e-7);
            Assert.InRange((body.AngularVelocity-expectedAngular).Length,0,2e-6);
            Assert.InRange(Math.Abs(a.Impulse.Normal-impulse.Normal),0,2e-6);
            Assert.InRange(Math.Abs(b.Impulse.Normal-impulse.Normal),0,2e-6);
            Assert.InRange(Math.Abs(a.TangentImpulse.Length-friction*a.Impulse.Normal),0,1e-10);
            Assert.InRange(Math.Abs(b.TangentImpulse.Length-friction*b.Impulse.Normal),0,1e-10);
            Assert.True(body.KineticEnergy<initialEnergy);
        }
        Verify(first,second);
        var solved=body.Snapshot();
        body.Restore(initial);
        Verify(new(ContactKinematics.AtPoint(body,wall,-firstNormal*radius,firstNormal),restitution,0,friction),
            new(ContactKinematics.AtPoint(body,wall,-secondNormal*radius,secondNormal),restitution,0,friction));
        Assert.Equal(solved,body.Snapshot());
    }

    [Fact]
    public void AccelerationPreservesImpulseAccountingAndConverges()
    {
        const double radius=.34,sine=.02;
        var cosine=Math.Sqrt(1-sine*sine);var inertia=.4*radius*radius;
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            new(-.79,-1,0),default,1,new(inertia,inertia,inertia));
        var wall=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var normals=new[]{new CollisionVector(cosine,0,-sine),new CollisionVector(cosine,0,sine)};
        var maps=normals.Select(n=>ContactKinematics.AtPoint(body,wall,-n*radius,n)).ToArray();
        var contacts=maps.Select(map=>new ContactConstraint(map,.055,0,.3)).ToArray();
        var constraints=contacts.Cast<IImpulseConstraint>().ToArray();
        var acceleration=new ImpulseSweepAcceleration(constraints,contacts.Select(c=>c.Normal).ToArray());
        var initial=body.Snapshot();
        var accepted=0;var rejected=0;double residual=double.PositiveInfinity;
        for(var iteration=0;iteration<256&&residual>1e-10;iteration++)
        {
            foreach(var contact in contacts) contact.Solve();
            var raw=contacts.Max(c=>c.Residual);
            var rawBody=body.Snapshot();
            var rawImpulses=contacts.Select(c=>c.Impulse).ToArray();
            residual=acceleration.Complete(raw);
            Assert.Equal(residual,contacts.Max(c=>c.Residual));
            if(residual<raw) accepted++;
            else if(iteration>0)
            {
                rejected++;
                Assert.Equal(rawBody,body.Snapshot());
                Assert.Equal(rawImpulses,contacts.Select(c=>c.Impulse).ToArray());
            }
            CollisionVector linear=default,angular=default;
            for(var i=0;i<contacts.Length;i++)
            {
                var impulse=normals[i]*contacts[i].Impulse.Normal+contacts[i].TangentImpulse;
                linear+=impulse;
                angular+=CollisionVector.Cross(-normals[i]*radius,impulse);
                Assert.InRange(contacts[i].TangentImpulse.Length,0,
                    .3*contacts[i].Impulse.Normal+1e-14);
            }
            Assert.InRange((body.LinearVelocity-initial.LinearVelocity-linear).Length,0,1e-12);
            Assert.InRange((body.AngularMomentum-initial.AngularMomentum-angular).Length,0,1e-12);
            Assert.Equal(initial.Pose,body.Pose);
            Assert.Equal(default,wall.LinearVelocity);
        }
        Assert.InRange(residual,0,1e-10);
        Assert.True(accepted>0);
        // Dedicated controls below force rejection and exceptional rollback.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MixedRoughAndSmoothContactsRetainTheirDistinctLaws(bool reverse)
    {
        const double radius=.34,sine=.11609291412523;
        var cosine=Math.Sqrt(1-sine*sine);var inertia=.4*radius*radius;
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,
            new(-.79,-1,0),default,1,new(inertia,inertia,inertia));
        var wall=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var firstNormal=new CollisionVector(cosine,0,-sine);
        var secondNormal=new CollisionVector(cosine,0,sine);
        var rough=new ContactConstraint(ContactKinematics.AtPoint(body,wall,-firstNormal*radius,firstNormal),.055,0,.3);
        var smooth=new ContactConstraint(ContactKinematics.AtPoint(body,wall,-secondNormal*radius,secondNormal),.055,0,0);
        IImpulseConstraint[] rows=reverse?[smooth,rough]:[rough,smooth];
        var result=ImpulseSolver.Solve(rows,tolerance:1e-10);
        Assert.InRange(result.MaximumResidual,0,1e-10);
        Assert.Equal(default,smooth.TangentImpulse);
        Assert.InRange(rough.TangentImpulse.Length,0,.3*rough.Impulse.Normal+1e-14);
        Assert.InRange(rough.Residual,0,1e-10);
        Assert.InRange(smooth.Residual,0,1e-10);
    }

    [Theory]
    [InlineData(.01,2,false)]
    [InlineData(.01,2,true)]
    [InlineData(.001,2,false)]
    [InlineData(.001,2,true)]
    [InlineData(.001,.1,false)]
    [InlineData(.001,0,false)]
    public void SingleFrictionContactAndTransmissionShareTheConservedImpulse(double surfaceMass,double friction,bool reverse)
    {
        var x=new CollisionVector(1,0,0);var y=new CollisionVector(0,1,0);var z=new CollisionVector(0,0,1);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.Identity,new(1,-1,0),default,1,new(1,1,1));
        var surface=new PhysicsBody(new(1),PhysicsMotionType.Dynamic,RigidPose.At(x*2),default,default,surfaceMass,new(1,1,1));
        var load=new PhysicsBody(new(2),PhysicsMotionType.Dynamic,RigidPose.At(x*4),default,default,1,new(1,1,1));
        var bodies=new[]{body,surface,load};var initial=bodies.Select(value=>value.Snapshot()).ToArray();
        var initialEnergy=bodies.Sum(value=>value.KineticEnergy);
        var initialMomentum=bodies.Aggregate(default(CollisionVector),(sum,value)=>sum+value.LinearVelocity/value.InverseMass);
        // Normal impact is supplied by a fixed support. Only the material
        // tangent couples the light surface to the load through a bilateral row.
        // Common sticking speed follows total X momentum; otherwise the
        // Coulomb bound fixes the sliding impulse because normal impulse is1.
        var stickingImpulse=(1+surfaceMass)/(2+surfaceMass);
        var impulse=Math.Min(friction,stickingImpulse);
        void Solve()
        {
            var contact=new ContactConstraint(new(y,new([new(body,y,default)]),
                new([new(body,-z,default)]),
                new([new(body,-x,default),new(surface,x,default)])),0,0,friction);
            var row=new ImpulseConstraint(new ConstraintGradient([new(surface,x,default),new(load,-x,default)]),
                0,double.NegativeInfinity,double.PositiveInfinity);
            var block=new BilateralConstraintBlock([row]);
            IImpulseConstraint[] constraints=reverse?[block,contact]:[contact,block];
            var result=ImpulseSolver.Solve(constraints,tolerance:1e-10);
            Assert.InRange(result.MaximumResidual,0,1e-10);
            Assert.InRange(Math.Abs(body.LinearVelocity.X-(1-impulse)),0,1e-9);
            Assert.InRange(Math.Abs(surface.LinearVelocity.X-impulse/(1+surfaceMass)),0,1e-9);
            Assert.InRange(Math.Abs(load.LinearVelocity.X-surface.LinearVelocity.X),0,1e-10);
            Assert.InRange(Math.Abs(contact.Impulse.Normal-1),0,1e-10);
            Assert.InRange(Math.Abs(contact.TangentImpulse.Length-impulse),0,1e-9);
            var momentum=bodies.Aggregate(default(CollisionVector),(sum,value)=>sum+value.LinearVelocity/value.InverseMass);
            Assert.InRange((momentum-initialMomentum-y).Length,0,1e-9);
            Assert.True(bodies.Sum(value=>value.KineticEnergy)<=initialEnergy);
        }
        Solve();
        var final=bodies.Select(value=>value.Snapshot()).ToArray();
        for(var i=0;i<bodies.Length;i++)bodies[i].Restore(initial[i]);
        Solve();
        Assert.Equal(final,bodies.Select(value=>value.Snapshot()).ToArray());
    }
}
