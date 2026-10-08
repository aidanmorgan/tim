using CuriousContraptions.Bridge;
using CuriousContraptions.Physics;
namespace CuriousContraptions.Tests;

public class CommittedTraceTests
{
    private static readonly CompoundGeometry Sphere=new([new(new ConvexSphere(1),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
    private static PoseReadStamp Stamp(long revision)=>new(new(1),new(revision),revision*.01);
    private static BodyPublicationRead Body(int id,int owner,double x,CollisionParticipation participation,bool opaque,ulong revision=0)=>
        new(new(new(id),PhysicsMotionType.Static,RigidPose.At(new(x,0,0)),RigidPose.Identity),
            new(new(owner),new(revision),participation,Sphere,opaque?Sphere:null),OwnerActivity.Inactive,default);
    private static double Cast(PoseReadLease read,PoseSample sample=PoseSample.Current,TraceMedium medium=TraceMedium.Light,
        PhysicsBodyId? emitter=null,PhysicsBodyId? receiver=null)=>
        read.Trace(read.Stamp(sample).SimulationTime,medium,default,new(1,0,0),10,emitter,receiver);

    [Theory]
    [InlineData(TraceMedium.Light,true,CollisionParticipation.Enabled,2)]
    [InlineData(TraceMedium.Sound,true,CollisionParticipation.Enabled,2)]
    [InlineData(TraceMedium.Air,false,CollisionParticipation.Enabled,2)]
    [InlineData(TraceMedium.Light,false,CollisionParticipation.Enabled,10)]
    [InlineData(TraceMedium.Sound,false,CollisionParticipation.Enabled,10)]
    [InlineData(TraceMedium.Air,true,CollisionParticipation.Disabled,10)]
    [InlineData(TraceMedium.Light,true,CollisionParticipation.Disabled,10)]
    public void MediumAndParticipationSelectCommittedShapes(TraceMedium medium,bool opaque,CollisionParticipation participation,double expected)
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[Body(0,0,3,participation,opaque)],[],[],[],[],[]);
        using var read=buffer.Acquire();
        Assert.InRange(Math.Abs(Cast(read,medium:medium)-expected),0,1e-6);
    }

    [Fact]
    public void RootExclusionsCoverChildrenAndRejectForeignOrChildIdentities()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),
            [Body(0,0,20,CollisionParticipation.Enabled,true),Body(1,0,3,CollisionParticipation.Enabled,true),
             Body(2,2,6,CollisionParticipation.Enabled,true)],[],[],[],[],[]);
        using var read=buffer.Acquire();
        Assert.InRange(Cast(read),1.999999,2.000001);
        Assert.InRange(Cast(read,emitter:new(0)),4.999999,5.000001);
        Assert.Equal(10,Cast(read,emitter:new(0),receiver:new(2)));
        Assert.Equal(10,Cast(read,emitter:new(2),receiver:new(0)));
        Assert.Throws<ArgumentException>(()=>Cast(read,emitter:new(1)));
        Assert.Throws<ArgumentException>(()=>Cast(read,receiver:new(3)));
    }

    [Fact]
    public void DiscardAndPublishKeepTracePoseAndShapeOnOneRevision()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[Body(0,0,3,CollisionParticipation.Enabled,true)],[],[],[],[],[]);
        var next=Body(0,0,6,CollisionParticipation.Enabled,false,1);
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),[next],[],[],[],[],[]);buffer.Discard();
        using(var read=buffer.Acquire())Assert.InRange(Cast(read),1.999999,2.000001);
        buffer.BeginWrite(0);buffer.Stage(Stamp(1),[next],[],[],[],[],[]);buffer.Publish();
        using(var read=buffer.Acquire())
        {
            Assert.InRange(Cast(read,PoseSample.Previous),1.999999,2.000001);
            Assert.Equal(10,Cast(read));
            Assert.InRange(Cast(read,medium:TraceMedium.Air),4.999999,5.000001);
        }
        var retired=buffer.Acquire();buffer.Remove();
        Assert.Throws<InvalidOperationException>(()=>Cast(retired));retired.Dispose();
        Assert.Throws<InvalidOperationException>(()=>Cast(default));
    }

    [Fact]
    public void BoundaryValidationAlsoAppliesToEmptyPublications()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[],[],[],[],[],[]);
        var read=buffer.Acquire();
        Assert.Equal(10,Cast(read));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(read,(PoseSample)999));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Cast(read,medium:(TraceMedium)999));
        Assert.Throws<ArgumentException>(()=>read.Trace(read.Stamp(PoseSample.Current).SimulationTime,TraceMedium.Light,default,default,10));
        Assert.Throws<ArgumentException>(()=>read.Trace(read.Stamp(PoseSample.Current).SimulationTime,TraceMedium.Light,new(double.NaN,0,0),new(1,0,0),10));
        Assert.Throws<ArgumentException>(()=>read.Trace(read.Stamp(PoseSample.Current).SimulationTime,TraceMedium.Light,default,new(double.PositiveInfinity,0,0),10));
        Assert.Throws<ArgumentException>(()=>read.Trace(read.Stamp(PoseSample.Current).SimulationTime,TraceMedium.Light,default,new(1,0,0),-1));
        Assert.Throws<ArgumentException>(()=>read.Trace(read.Stamp(PoseSample.Current).SimulationTime,TraceMedium.Light,default,new(1,0,0),double.NaN));
        read.Dispose();Assert.Throws<InvalidOperationException>(()=>Cast(read));
    }

    [Theory]
    [InlineData(0,2)]
    [InlineData(90,2.75)]
    public void RotatedBodyPoseControlsTheCommittedHit(double degrees,double expected)
    {
        var angle=degrees*Math.PI/360;
        var shape=new CompoundGeometry([new(new ConvexBox(new(1,.25,.25)),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
        var pose=new RigidPose(new(3,0,0),new(0,Math.Sin(angle),0,Math.Cos(angle)));
        var body=new BodyPublicationRead(new(new(0),PhysicsMotionType.Static,pose,RigidPose.Identity),
            new(new(0),new(0),CollisionParticipation.Enabled,shape,shape),OwnerActivity.Inactive,default);
        var buffer=new CommittedPoseBuffer(Stamp(0),[body],[],[],[],[],[]);
        using var read=buffer.Acquire();
        Assert.InRange(Math.Abs(Cast(read)-expected),0,1e-6);
    }

    [Fact]
    public void NonUnitDirectionRetainsRayParameterContractAndZeroRangeIsSupported()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[Body(0,0,3,CollisionParticipation.Enabled,true)],[],[],[],[],[]);
        using var read=buffer.Acquire();
        Assert.InRange(read.Trace(read.Stamp(PoseSample.Current).SimulationTime,TraceMedium.Light,default,new(2,0,0),10),.999999,1.000001);
        Assert.Equal(0,read.Trace(read.Stamp(PoseSample.Current).SimulationTime,TraceMedium.Light,default,new(1,0,0),0));
        Assert.Equal(1,read.Trace(read.Stamp(PoseSample.Current).SimulationTime,TraceMedium.Light,default,new(1,0,0),1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(32)]
    public void WarmTraceMeasurementsRetainExactResults(int bodyCount)
    {
        var bodies=Enumerable.Range(0,bodyCount).Select(i=>Body(i,i,3+i*3,CollisionParticipation.Enabled,true)).ToArray();
        var buffer=new CommittedPoseBuffer(Stamp(0),bodies,[],[],[],[],[]);
        using var read=buffer.Acquire();
        for(var i=0;i<100;i++)Cast(read);
        const int queries=1000;
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        var started=System.Diagnostics.Stopwatch.GetTimestamp();
        double sum=0;
        for(var i=0;i<queries;i++)sum+=Cast(read);
        var elapsed=System.Diagnostics.Stopwatch.GetElapsedTime(started);
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        Assert.InRange(Math.Abs(sum-2*queries),0,1e-3);
        Console.WriteLine($"Committed trace measurement: bodies={bodyCount}, queries={queries}, bytes={allocated}, elapsed_ms={elapsed.TotalMilliseconds:R}");
    }

    [Fact]
    public void ShapeReplacementBurstRetainsPreviousAndOwnedVersions()
    {
        var buffer=new CommittedPoseBuffer(Stamp(0),[Body(0,0,3,CollisionParticipation.Enabled,true)],[],[],[],[],[]);
        BodyQueryRead retained;
        using(var read=buffer.Acquire())retained=read.ReadQuery(PoseSample.Current,0);
        const int replacements=32;
        var allocated=GC.GetAllocatedBytesForCurrentThread();
        for(var revision=1;revision<=replacements;revision++)
        {
            var radius=1+revision*.01;
            var shape=new CompoundGeometry([new(new ConvexSphere(radius),SceneGeometryAdapter.CaptureAffine(Godot.Transform3D.Identity))]);
            var next=new BodyPublicationRead(Body(0,0,3,CollisionParticipation.Enabled,true).Pose,
                new(new(0),new((ulong)revision),CollisionParticipation.Enabled,shape,shape),OwnerActivity.Inactive,default);
            buffer.BeginWrite(0);buffer.Stage(Stamp(revision),[next],[],[],[],[],[]);buffer.Publish();
            using var read=buffer.Acquire();
            Assert.InRange(Math.Abs(Cast(read)-(3-radius)),0,1e-6);
            Assert.InRange(Math.Abs(Cast(read,PoseSample.Previous)-(3-(1+(revision-1)*.01))),0,1e-6);
            Assert.Same(Sphere,retained.Solid);
        }
        allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
        var retainedTrace=new PointTrace(default,new(1,0,0),10);
        retainedTrace.Test(retained.Solid,RigidPose.At(new(3,0,0)));
        Assert.InRange(retainedTrace.Closest,1.999999,2.000001);
        Console.WriteLine($"Shape burst including assertions and casts: replacements={replacements}, bytes={allocated}");
    }
}
