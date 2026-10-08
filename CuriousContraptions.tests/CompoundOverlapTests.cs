using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CompoundOverlapTests
{
    private static CompoundMotion Motion(CompoundGeometry geometry,RigidPose pose,int id)=>
        new(geometry,new PhysicsBody(new(id),PhysicsMotionType.Static,pose,default,default).CreateTrajectory(0,default));
    private static CompoundGeometry Box()=>new([new(new ConvexBox(new(1,1,1)),AffineTransform.Identity)]);
    [Theory]
    [InlineData(2.1,false)]
    [InlineData(2,false)]
    [InlineData(1.99995,false)]
    [InlineData(1.5,true)]
    public void SharedInitialQueryHonoursTheDeclaredPenetrationBudget(double x,bool overlaps)
    {
        var result=CompoundCollision.FindOverlap(Motion(Box(),RigidPose.Identity,0),
            Motion(Box(),RigidPose.At(new(x,0,0)),1),.0001,out _);
        Assert.Equal(overlaps,result is not null);
        if(result is { } hit)
        {
            Assert.Equal(0,hit.ChildA.Index); Assert.Equal(0,hit.ChildB.Index);
            Assert.InRange(hit.Separation.LowerBound,-.500001,-.499999);
        }
    }

    public enum HollowKind { Tube, Funnel, Bend45, Bend90 }
    [Theory]
    [InlineData(HollowKind.Tube,false)]
    [InlineData(HollowKind.Tube,true)]
    [InlineData(HollowKind.Funnel,false)]
    [InlineData(HollowKind.Funnel,true)]
    [InlineData(HollowKind.Bend45,false)]
    [InlineData(HollowKind.Bend45,true)]
    [InlineData(HollowKind.Bend90,false)]
    [InlineData(HollowKind.Bend90,true)]
    public void HollowMaterialOverlapsButItsPassageRemainsEmpty(HollowKind kind,bool rotated)
    {
        var settings=new HollowGeometrySettings(.005);
        var angle=kind==HollowKind.Bend45?Math.PI/8:Math.PI/4;
        var geometry=kind switch
        {
            HollowKind.Tube=>HollowGeometry.Tube(2,.65,.75,settings).Geometry,
            HollowKind.Funnel=>HollowGeometry.Frustum(2,1,.65,.1,settings).Geometry,
            HollowKind.Bend45=>HollowGeometry.Bend(2,Math.PI/4,.65,.75,settings).Geometry,
            HollowKind.Bend90=>HollowGeometry.Bend(2,Math.PI/2,.65,.75,settings).Geometry,
            _=>throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var centre=kind is HollowKind.Bend45 or HollowKind.Bend90?new CollisionVector(2*Math.Sin(angle),2*Math.Cos(angle),0):default;
        var wall=kind switch
        {
            HollowKind.Tube=>new CollisionVector(0,.7,0),
            HollowKind.Funnel=>new CollisionVector(0,.875,0),
            HollowKind.Bend45 or HollowKind.Bend90=>centre+new CollisionVector(0,0,.7),
            _=>throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var pose=rotated?new RigidPose(new(3,4,-2),RigidRotation.FromRotationVector(new(.3,.7,-.2))):RigidPose.Identity;
        var probe=new CompoundGeometry([new(new ConvexSphere(.05),AffineTransform.Identity)]);
        var fixture=Motion(geometry,pose,1);
        Assert.Null(CompoundCollision.FindOverlap(Motion(probe,RigidPose.At(pose.TransformPoint(centre)),0),fixture,.0001,out _));
        Assert.NotNull(CompoundCollision.FindOverlap(Motion(probe,RigidPose.At(pose.TransformPoint(wall)),0),fixture,.0001,out _));
    }

    [Fact]
    public void MultipleOverlapsKeepDeclarationOrderInsteadOfHierarchyOrder()
    {
        var children=new CompoundGeometry([
            new(new ConvexBox(new(1,1,1)),SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,new(.1f,0,0)))),
            new(new ConvexBox(new(1,1,1)),SceneGeometryAdapter.CaptureAffine(new Transform3D(Basis.Identity,new(-.1f,0,0))))]);
        var result=CompoundCollision.FindOverlap(Motion(Box(),RigidPose.Identity,0),Motion(children,RigidPose.Identity,1),.0001,out _);
        Assert.Equal(0,result!.Value.ChildB.Index);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidBudgetRejectsEvenWhenBoundsAreDistant(double budget)
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>CompoundCollision.FindOverlap(Motion(Box(),RigidPose.Identity,0),
            Motion(Box(),RigidPose.At(new(100,0,0)),1),budget,out _));
    }
}
