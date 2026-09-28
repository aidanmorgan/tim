using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class CompoundCollisionTests
{
    private static BodyTrajectory Path(RigidPose pose,CollisionVector velocity=default,CollisionVector spin=default,double duration=1)=>
        new PhysicsBody(new(0),PhysicsMotionType.Kinematic,pose,velocity,spin).CreateTrajectory(duration);
    private static CompoundGeometry Passage()
    {
        List<ConvexInstance> children=[];
        foreach(var side in new[]{-1,1})
        {
            children.Add(new(new ConvexBox(new(1,.1,1.2)),new(Basis.Identity,new(3,side*1.1f,0))));
            children.Add(new(new ConvexBox(new(1,1,.1)),new(Basis.Identity,new(3,0,side*1.1f))));
        }
        return new(children.ToArray());
    }

    [Theory]
    [InlineData(0,false)]
    [InlineData(1.1f,true)]
    [InlineData(3,false)]
    public void HollowCompoundHasARealOpenPassageAndSolidWalls(float height,bool expected)
    {
        var moving=new CompoundMotion(new([new(new ConvexSphere(.25),Transform3D.Identity)]),
            Path(RigidPose.At(new(0,height,0)),new(6,0,0)));
        var passage=new CompoundMotion(Passage(),Path(RigidPose.Identity));
        var result=CompoundCollision.Cast(moving,passage,1);
        Assert.Equal(expected?ConvexSweepStatus.Contact:ConvexSweepStatus.Clear,result.Status);
        if(expected)
        {
            Assert.Equal(new ColliderChildId(0),result.ChildA);
            Assert.NotNull(result.ChildB);
            Assert.InRange(result.Time,.29,.30);
        }
        else
        {
            Assert.Null(result.ChildA);
            Assert.Null(result.ChildB);
        }
    }

    [Fact]
    public void NovelSupportMappedShapeRequiresNoPairRegistration()
    {
        // Only this shape's support function is new: sphere/capsule/compound
        // collision uses the exact same production distance and sweep code.
        var capsule=new Capsule(.2,.5);
        var moving=new CompoundMotion(new([new(capsule,Transform3D.Identity)]),
            Path(RigidPose.Identity,new(6,0,0),new(.1,.2,.3)));
        var passage=new CompoundMotion(Passage(),Path(RigidPose.Identity));
        Assert.Equal(ConvexSweepStatus.Clear,CompoundCollision.Cast(moving,passage,1).Status);
    }

    [Fact]
    public void BroadPhaseSkipsDistantPairsAndCannotHideRotation()
    {
        var shape=new CompoundGeometry([new(new ConvexBox(new(2,.05,.05)),Transform3D.Identity)]);
        var moving=new CompoundMotion(shape,Path(RigidPose.Identity,spin:new(0,0,120),duration:.01));
        var far=new CompoundMotion(shape,Path(RigidPose.At(new(100,100,100))));
        var miss=CompoundCollision.Cast(moving,far,.01);
        Assert.Equal(ConvexSweepStatus.Clear,miss.Status);
        Assert.Equal(0,miss.NarrowPhaseCalls);
        var near=new CompoundMotion(new([new(new ConvexBox(new(.05,.05,.05)),Transform3D.Identity)]),
            Path(RigidPose.At(new(1.5f*Mathf.Cos(.4f),1.5f*Mathf.Sin(.4f),0))));
        var hit=CompoundCollision.Cast(moving,near,.01);
        Assert.Equal(ConvexSweepStatus.Contact,hit.Status);
        Assert.Equal(1,hit.NarrowPhaseCalls);
    }

    [Fact]
    public void EmptyOrInvalidCompoundDeclarationsAreRejected()
    {
        Assert.Throws<ArgumentException>(()=>new CompoundGeometry([]));
        Assert.Throws<ArgumentException>(()=>new CompoundGeometry([default]));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ColliderChildId(-1));
        Assert.Throws<InvalidOperationException>(()=>CompoundCollision.Cast(default,default,1));
    }

    private sealed class Capsule(double radius,double halfLength) : ConvexGeometry
    {
        public override SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance)
        {
            var normal=SupportFeature.UnitDirection(direction,planeTolerance);
            var radial=normal*radius;
            return SupportFeature.FromPoints([radial+new CollisionVector(0,-halfLength,0),
                radial+new CollisionVector(0,halfLength,0)],normal,planeTolerance);
        }
        public override double BoundingRadius=>radius+halfLength;
        public override InteriorBall InteriorBall=>new(default,radius);
        public override CollisionVector Support(CollisionVector direction)
        {
            var ball=direction.Length==0?new CollisionVector(radius,0,0):direction*(radius/direction.Length);
            return ball+new CollisionVector(0,direction.Y<0?-halfLength:halfLength,0);
        }
    }
}
