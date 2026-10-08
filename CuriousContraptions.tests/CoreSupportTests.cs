using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CoreSupportTests
{
    [Fact]
    public void SphereCoreIsExactlyItsCentreUnderRigidMotion()
    {
        var pose=new RigidPose(new(10000000.25,4.896184427805594,-.10381557219440532),
            RigidRotation.FromRotationVector(new(.3,.7,-.2)));
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,pose,default,default);
        var shape=new ConvexPose(new(new ConvexSphere(.34),AffineTransform.Identity),body.Pose);
        var random=new Random(163);
        for(var i=0;i<100;i++)
        {
            var direction=new CollisionVector(random.NextDouble()-.5,random.NextDouble()-.5,random.NextDouble()-.5);
            Assert.Equal(pose.Center,shape.CoreSupport(direction));
        }
    }

    [Fact]
    public void TransformedRoundedCoreRetainsTheDeclaredBallAndBasisDistortion()
    {
        var basis=Basis.FromEuler(new(.3f,.7f,-.2f));
        var shape=new ConvexInstance(new ConvexRounded(new ConvexBox(new(.2,.4,.7)),.3),
            new(SceneGeometryAdapter.CaptureBasis(basis),new(3,5,-2)));
        var random=new Random(291);
        for(var i=0;i<100;i++)
        {
            var direction=new CollisionVector(random.NextDouble()-.5,random.NextDouble()-.5,random.NextDouble()-.5);
            direction/=direction.Length;
            var rebuilt=shape.CoreSupport(direction)+direction*shape.RoundingRadius;
            Assert.InRange((rebuilt-shape.Support(direction)).Length,0,1e-13);
        }
    }

    [Fact]
    public void NestedRoundingDoesNotAlterTheUnderlyingCore()
    {
        var core=new ConvexBox(new(.2,.4,.7));
        var rounded=new ConvexRounded(new ConvexRounded(core,.3),.2);
        foreach(var direction in new[]{new CollisionVector(1,2,3),new(-2,3,-4),new(1,0,0)})
            Assert.Equal(core.Support(direction),rounded.CoreSupport(direction));
    }
}
