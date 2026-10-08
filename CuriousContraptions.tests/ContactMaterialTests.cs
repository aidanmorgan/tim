using CuriousContraptions.Physics;
using Godot;

namespace CuriousContraptions.Tests;

public class ContactMaterialTests
{
    [Theory]
    [InlineData(0,1,0)]
    [InlineData(1,0,0)]
    [InlineData(.5,1,.5)]
    [InlineData(.5,.2,.1)]
    [InlineData(1,1,1)]
    public void EitherSurfaceCanAbsorbNormalEnergy(double a,double b,double expected)
    {
        var first=new ContactMaterial(a,.1,.25);
        var second=new ContactMaterial(b,.2,1);
        var combined=ContactMaterial.Combine(first,second);
        Assert.Equal(expected,combined.Restitution,12);
        Assert.Equal(.2,combined.BounceThreshold);
        Assert.Equal(.5,combined.Friction);
        Assert.Equal(combined,ContactMaterial.Combine(second,first));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(.2)]
    [InlineData(1)]
    public void WorldUsesBothDeclaredSurfacesAndReplays(double surface)
    {
        var ball=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,1,0)),
            new(0,-2,0),default,1,new(.1,.1,.1));
        var floor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var sphere=new CompoundGeometry([new(new ConvexSphere(.2),AffineTransform.Identity)]);
        var deck=new CompoundGeometry([new(new ConvexBox(new(4,.1,4)),AffineTransform.Identity)]);
        var world=new PhysicsWorld([],[new(ball,sphere,new(.5,0,0)),new(floor,deck,new(surface,0,0))],[],new(default));
        var initial=world.Capture();
        world.Step([],[],.4);
        Assert.InRange(Math.Abs(ball.LinearVelocity.Y-surface),0,1e-7);
        Assert.True(ball.LinearVelocity.LengthSquared<=4+1e-12);
        var final=world.Capture();
        world.Restore(initial);
        world.Step([],[],.4);
        Assert.Equal(final.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
    }
}
