using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class SampledTraceTests
{
    private static readonly CompoundGeometry Sphere=new([new(new ConvexSphere(.25),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static readonly CompoundGeometry Box=new([new(new ConvexBox(new(1,.25,.25)),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static CommittedPoseBuffer Buffer(CompoundGeometry shape,CollisionVector velocity,CollisionVector spin,
        CollisionParticipation nextParticipation=CollisionParticipation.Enabled,CompoundGeometry? nextShape=null)
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(new(3,0,0)),velocity,spin);
        var world=new PhysicsWorld([],[new(body,shape,new(1,0,0))],[],new(default,maximumStep:1));
        BodyPublicationRead Read(ulong revision,CollisionParticipation participation,CompoundGeometry geometry)=>new(
            new(body.Id,body.MotionType,body.Pose,RigidPose.Identity),
            new(body.Id,new(revision),participation,geometry,geometry),OwnerActivity.Inactive,new(body.LinearVelocity,body.AngularVelocity));
        var buffer=new CommittedPoseBuffer(new(new(1),new(0),0),[Read(0,CollisionParticipation.Enabled,shape)],[],[],[],[],[]);
        world.Step([],[],1);
        buffer.BeginWrite(1);buffer.AppendMotion(world.LastMotion!);
        buffer.Stage(new(new(1),new(1),1),[Read(nextShape is null&&nextParticipation==CollisionParticipation.Enabled?0UL:1UL,
            nextParticipation,nextShape??shape)],[],[],[],[],[]);buffer.Publish();
        return buffer;
    }
    private static double Cast(PoseReadLease read,double time,TraceMedium medium=TraceMedium.Light)=>
        read.Trace(time,medium,default,new(1,0,0),10);

    [Theory]
    [InlineData(TraceMedium.Light)]
    [InlineData(TraceMedium.Sound)]
    [InlineData(TraceMedium.Air)]
    public void MovingObstructionUsesAcceptedInteriorPosition(TraceMedium medium)
    {
        var buffer=Buffer(Sphere,new(1,0,0),default);using var read=buffer.Acquire();
        foreach(var time in new[]{0,.125,.5,1})
            Assert.InRange(Math.Abs(Cast(read,time,medium)-(2.75+time)),0,1e-6);
        Assert.Equal(10,read.Trace(.5,medium,default,new(1,0,0),10,new(0)));
        Assert.InRange(read.Trace(.5,medium,default,new(2,0,0),10),1.624999,1.625001);
    }

    [Fact]
    public void CompleteRevolutionsDoNotCollapseToEndpointOrientation()
    {
        var buffer=Buffer(Box,default,new(0,0,Math.Tau*2));using var read=buffer.Acquire();
        Assert.InRange(Cast(read,0),1.999999,2.000001);
        Assert.InRange(Cast(read,.125),2.749999,2.750001);
        Assert.InRange(Cast(read,.25),1.999999,2.000001);
        Assert.InRange(Cast(read,1),1.999999,2.000001);
    }

    [Theory]
    [InlineData(TraceMedium.Light)]
    [InlineData(TraceMedium.Sound)]
    [InlineData(TraceMedium.Air)]
    public void ParticipationChangesAtTheCommitBoundary(TraceMedium medium)
    {
        var buffer=Buffer(Sphere,new(1,0,0),default,CollisionParticipation.Disabled);
        using var read=buffer.Acquire();
        Assert.InRange(Cast(read,.5,medium),3.249999,3.250001);
        Assert.InRange(Cast(read,Math.BitDecrement(1),medium),3.749999,3.750001);
        Assert.Equal(10,Cast(read,1,medium));
    }

    [Fact]
    public void GeometryVersionChangesAtTheSameBoundaryAsItsPose()
    {
        var larger=new CompoundGeometry([new(new ConvexSphere(.5),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
        var buffer=Buffer(Sphere,new(1,0,0),default,nextShape:larger);using var read=buffer.Acquire();
        Assert.InRange(Cast(read,.5),3.249999,3.250001);
        Assert.InRange(Cast(read,Math.BitDecrement(1)),3.749999,3.750001);
        Assert.InRange(Cast(read,1),3.499999,3.500001);
        Assert.Same(Sphere,read.ReadQuery(PoseSample.Previous,0).Solid);
        Assert.Same(larger,read.ReadQuery(PoseSample.Current,0).Solid);
    }

    [Fact]
    public void InvalidTimesRejectEvenWithoutBodiesAndExpiredLeasesReject()
    {
        var buffer=new CommittedPoseBuffer(new(new(1),new(0),0),[],[],[],[],[],[]);
        var read=buffer.Acquire();
        foreach(var time in new[]{-1,.1,double.NaN,double.PositiveInfinity})
            Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(read,time));
        Assert.Equal(10,Cast(read,0));
        read.Dispose();Assert.Throws<InvalidOperationException>(()=>Cast(read,0));
        Assert.Throws<InvalidOperationException>(()=>Cast(default,0));
    }

    [Fact]
    public void MissingAcceptedHistoryRejectsInteriorInsteadOfInventingMotion()
    {
        var body=new BodyPublicationRead(new(new(0),PhysicsMotionType.Static,RigidPose.At(new(3,0,0)),RigidPose.Identity),
            new(new(0),new(0),CollisionParticipation.Enabled,Sphere,Sphere),OwnerActivity.Inactive,default);
        var buffer=new CommittedPoseBuffer(new(new(1),new(0),0),[body],[],[],[],[],[]);
        buffer.BeginWrite(0);buffer.Stage(new(new(1),new(1),1),[body],[],[],[],[],[]);buffer.Publish();
        using var read=buffer.Acquire();
        Assert.InRange(Cast(read,0),2.749999,2.750001);
        Assert.InRange(Cast(read,1),2.749999,2.750001);
        Assert.Throws<InvalidOperationException>(()=>Cast(read,.5));
    }
}
