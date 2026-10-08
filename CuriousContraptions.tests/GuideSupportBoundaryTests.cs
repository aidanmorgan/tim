using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class GuideSupportBoundaryTests
{
    public enum Shape { Sphere, Box, RoundedHull, Compound }
    private static CompoundGeometry Geometry(Shape shape)=>shape switch
    {
        Shape.Sphere=>new([new(new ConvexSphere(.2),AffineTransform.Identity)]),
        Shape.Box=>new([new(new ConvexBox(new(.2,.4,.1)),AffineTransform.Identity)]),
        Shape.RoundedHull=>new([new(new ConvexRounded(new ConvexHull([new(-.1,-.1,0),new(.1,-.1,0),new(0,.1,.1)]),.1),AffineTransform.Identity)]),
        Shape.Compound=>new([new(new ConvexSphere(.1),AffineTransform.Identity),
            new(new ConvexSphere(.1),new(AffineBasis.Identity,new(0,-.5f,0)))]),
        _=>throw new ArgumentOutOfRangeException(nameof(shape))
    };
    private static readonly CompoundGeometry FrameGeometry=new([new(new ConvexSphere(.01),new(AffineBasis.Identity,new(0,5,0)))]);
    [Theory]
    [InlineData(Shape.Sphere,false)]
    [InlineData(Shape.Sphere,true)]
    [InlineData(Shape.Box,false)]
    [InlineData(Shape.Box,true)]
    [InlineData(Shape.RoundedHull,false)]
    [InlineData(Shape.RoundedHull,true)]
    [InlineData(Shape.Compound,false)]
    [InlineData(Shape.Compound,true)]
    public void OwnedCompoundSupportControlsRimClearance(Shape shape,bool clear)
    {
        var frame=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(.4,clear?1.2:.6,0)),default,default,1,new(1,1,1));
        var bodies=new[]{body,frame}.ToDictionary(item=>item.Id);
        var colliders=new Dictionary<PhysicsBodyId,PhysicsColliderUpdate>
        {
            [body.Id]=new(body.Id,Geometry(shape),new(0,0,0),CollisionParticipation.Enabled),
            [frame.Id]=new(frame.Id,FrameGeometry,new(0,0,0),CollisionParticipation.Enabled)
        };
        var guide=new PlanarGuideLoad(body.Id,frame.Id,.5,2,2,2,12,.5);
        var force=guide.Evaluate(bodies,colliders);
        Assert.Equal(clear,force.Force.Length>0);
        if(clear)Assert.InRange(Math.Abs(force.Force.X+4.8),0,1e-12);
        colliders[body.Id]=new(body.Id,Geometry(shape),new(0,0,0),CollisionParticipation.Disabled);
        Assert.Equal(default,guide.Evaluate(bodies,colliders));
    }

    [Fact]
    public void InvalidSupportHeightIsRejected()
    {
        var body = new PhysicsBody(new(0), PhysicsMotionType.Dynamic, RigidPose.Identity, default, default, 1, new(1, 1, 1));
        var frame = new PhysicsBody(new(1), PhysicsMotionType.Static, RigidPose.Identity, default, default);
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlanarGuideLoad(body.Id, frame.Id, 0, 2, 3, 3, 12, double.NaN));
    }

    [Fact]
    public void UndefinedFixtureChoiceIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>Geometry((Shape)(-1)));
    }
}
