using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public class ContactPatchTests
{
    private static readonly CollisionVector Up=new(0,1,0);
    private static SupportFeature Feature(params CollisionVector[] points)=>
        new(points.Select((point,index)=>new SupportVertex(new(index),point)).ToArray());
    private static SupportFeature Square(double height,double half=1)=>
        Feature(new(-half,height,-half),new(half,height,-half),new(half,height,half),new(-half,height,half));
    private static void Near(CollisionVector a,CollisionVector b,double tolerance=1e-8)=>
        Assert.InRange((a-b).Length,0,tolerance);
    private static ConvexPose At(ConvexGeometry geometry,RigidPose pose)=>
        new(new(geometry,AffineTransform.Identity),pose);
    private static SupportFeature Transform(SupportFeature feature,RigidPose pose)=>
        new(feature.Vertices.ToArray().Select(v=>new SupportVertex(v.Id,pose.TransformPoint(v.Point))).ToArray());

    [Fact]
    public void FeatureExtractionReturnsVerticesEdgesAndFacesWithStableIdentities()
    {
        var box=new ConvexBox(new(1,2,3));
        Assert.Equal(4,box.SupportingFeature(Up,1e-7).Vertices.Length);
        Assert.Equal(2,box.SupportingFeature(new(1,1,0),1e-7).Vertices.Length);
        Assert.Single(box.SupportingFeature(new(1,1,1),1e-7).Vertices.ToArray());
        var first=box.SupportingFeature(Up,1e-7).Vertices.ToArray();
        var again=box.SupportingFeature(Up*3,1e-7).Vertices.ToArray();
        Assert.Equal(first,again);
        Assert.All(first,v=>Assert.Equal(2,v.Point.Y));
        var sphere=new ConvexSphere(2).SupportingFeature(Up,1e-7);
        Near(new(0,2,0),Assert.Single(sphere.Vertices.ToArray()).Point);
    }

    [Fact]
    public void HullInputIsCopiedAndInteriorDuplicatePointsDoNotAddContacts()
    {
        CollisionVector[] points=[new(-1,0,-1),new(1,0,-1),new(1,0,1),new(-1,0,1),default,new(-1,0,-1)];
        var hull=new ConvexHull(points);
        points[0]=new(100,100,100);
        var patch=ContactPatch.Clip(hull.SupportingFeature(Up,1e-7),Square(0),Up);
        Assert.Equal(4,patch.Points.Length);
        foreach(var point in patch.Points) Assert.Equal(0,point.Separation);
    }

    [Fact]
    public void FlatBoxOnFloorProducesFourCornersAndNoArtificialSpin()
    {
        var a=At(new ConvexBox(new(1,1,1)),RigidPose.At(new(0,1,0)));
        var b=At(new ConvexBox(new(4,1,4)),RigidPose.At(new(0,-1,0)));
        var query=ConvexPenetration.Query(a,b);
        Near(Up,query.Normal);
        var patch=ContactPatch.Clip(a.SupportingFeature(-query.Normal,1e-7),b.SupportingFeature(query.Normal,1e-7),query.Normal);
        Assert.Equal(4,patch.Points.Length);
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(0,1,0)),new(0,-3,0),default,1,new InertiaTensor(2.0/3,2.0/3,2.0/3));
        var floor=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.At(new(0,-1,0)),default,default);
        var constraints=patch.Points.ToArray().Select(p=>new ContactConstraint(ContactKinematics.AtPoint(body,floor,(p.PointA+p.PointB)*.5,patch.Normal),0,0,.5)).ToArray();
        ImpulseSolver.Solve(constraints);
        Near(default,body.LinearVelocity); Near(default,body.AngularVelocity);
        Assert.All(constraints,c=>Assert.True(c.Normal.AccumulatedImpulse>0));
    }

    [Theory]
    [InlineData(.01)]
    [InlineData(1)]
    [InlineData(100)]
    public void NearTouchWitnessesRespectTheCombinedFeaturePlaneBudget(double scale)
    {
        var floor=At(new ConvexBox(new CollisionVector(10,.5,10)*scale),RigidPose.At(new(0,-.5*scale,0)));
        var geometry=new ConvexBox(new CollisionVector(.5,.5,.5)*scale);
        var tolerance=ConvexDistance.DefaultTolerance*scale;
        for(var i=1;i<=12;i++)
        for(var j=1;j<=12;j++)
        for(var k=0;k<=10;k++)
        {
            var rotation=RigidRotation.FromRotationVector(new(-i*ConvexDistance.DefaultTolerance/10,0,j*ConvexDistance.DefaultTolerance/10));
            var pose=new RigidPose(new(0,.5*scale-k*.1*tolerance,0),rotation);
            var body=At(geometry,pose);
            var patch=ContactManifold.Query(body,floor,tolerance,tolerance);
            Assert.Equal(ContactManifoldStatus.Contact,patch.Status);
            Assert.NotEmpty(patch.Points.ToArray());
            foreach(var point in patch.Points)
            {
                var local=pose.InverseTransformPoint(point.PointA);
                Assert.InRange(Math.Abs(local.X),0,.5*scale+tolerance);
                Assert.InRange(Math.Abs(local.Y),0,.5*scale+tolerance);
                Assert.InRange(Math.Abs(local.Z),0,.5*scale+tolerance);
                Assert.InRange(Math.Abs(point.PointB.Y),0,tolerance);
                Assert.InRange((point.PointA-point.PointB-patch.Normal*point.Separation).Length,0,2*tolerance);
            }
            var reverse=ContactManifold.Query(floor,body,tolerance,tolerance);
            Assert.Equal(ContactManifoldStatus.Contact,reverse.Status);
            Assert.NotEmpty(reverse.Points.ToArray());
            var clear=At(geometry,new(pose.Center+Up*(10*tolerance),rotation));
            Assert.Equal(ContactManifoldStatus.Clear,ContactManifold.Query(clear,floor,tolerance,tolerance).Status);
        }
    }

    [Fact]
    public void RotatedSquareClipsToEightCornersRatherThanKeepingUncontainedVertices()
    {
        var rotation=RigidRotation.FromRotationVector(Up*(Math.PI/4));
        var patch=ContactPatch.Clip(Square(0),Transform(Square(.2),new(default,rotation)),Up);
        Assert.Equal(8,patch.Points.Length);
        var small=Math.Sqrt(2)-1;
        foreach(var contact in patch.Points)
        {
            Assert.InRange(contact.Separation,-.200000001,-.199999999);
            var x=Math.Abs(contact.PointA.X); var z=Math.Abs(contact.PointA.Z);
            Assert.True(Math.Abs(Math.Max(x,z)-1)<1e-10);
            Assert.True(Math.Abs(Math.Min(x,z)-small)<1e-10);
            Near(contact.PointA+Up*.2,contact.PointB);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(17)]
    public void RandomRectangleClipsMatchAnalyticIntersection(int seed)
    {
        var random=new Random(seed);
        for(var i=0;i<200;i++)
        {
            var x=random.NextDouble()*6-3; var z=random.NextDouble()*6-3;
            var a=Square(0);
            var b=Transform(Square(.25),RigidPose.At(new(x,0,z)));
            var patch=ContactPatch.Clip(a,b,Up);
            var loX=Math.Max(-1,x-1); var hiX=Math.Min(1,x+1);
            var loZ=Math.Max(-1,z-1); var hiZ=Math.Min(1,z+1);
            if(loX>hiX||loZ>hiZ) { Assert.Empty(patch.Points.ToArray()); continue; }
            Assert.Equal(4,patch.Points.Length);
            CollisionVector[] expected=[new(loX,0,loZ),new(hiX,0,loZ),new(hiX,0,hiZ),new(loX,0,hiZ)];
            foreach(var point in expected) Assert.Contains(patch.Points.ToArray(),p=>(p.PointA-point).Length<1e-8);
        }
    }

    [Fact]
    public void PointEdgeAndPolygonCombinationsUseTheSameIntersection()
    {
        var point=Feature(default(CollisionVector));
        var edge=Feature(new(-2,0,0),new(2,0,0));
        var cross=Feature(new(0,0,-2),new(0,0,2));
        var face=Square(0);
        Assert.Single(ContactPatch.Clip(point,point,Up).Points.ToArray());
        Assert.Single(ContactPatch.Clip(point,edge,Up).Points.ToArray());
        Assert.Single(ContactPatch.Clip(edge,cross,Up).Points.ToArray());
        Assert.Single(ContactPatch.Clip(point,face,Up).Points.ToArray());
        var segment=ContactPatch.Clip(edge,face,Up);
        Assert.Equal(2,segment.Points.Length);
        Assert.All(segment.Points.ToArray(),p=>Assert.Equal(1,Math.Abs(p.PointA.X)));
        Assert.Equal(2,ContactPatch.Clip(edge,Feature(new(-1,0,0),new(3,0,0)),Up).Points.Length);
        Assert.Empty(ContactPatch.Clip(point,Feature(new CollisionVector(.001,0,0)),Up).Points.ToArray());
        Assert.Empty(ContactPatch.Clip(edge,Feature(new(-2,0,.001),new(2,0,.001)),Up).Points.ToArray());
    }

    [Fact]
    public void CommonRigidTransformsAndSwappedInputsPreserveWorldAnchors()
    {
        var a=Square(0); var b=Transform(Square(.1),RigidPose.At(new(.3,0,.2)));
        var baseline=ContactPatch.Clip(a,b,Up);
        var pose=new RigidPose(new(300,-200,400),RigidRotation.FromRotationVector(new(.3,.7,-.5)));
        var transformed=ContactPatch.Clip(Transform(a,pose),Transform(b,pose),pose.Rotation.Apply(Up));
        var reversed=ContactPatch.Clip(b,a,-Up);
        Assert.Equal(baseline.Points.Length,transformed.Points.Length);
        Assert.Equal(baseline.Points.Length,reversed.Points.Length);
        foreach(var contact in baseline.Points)
        {
            Assert.Contains(transformed.Points.ToArray(),p=>(p.PointA-pose.TransformPoint(contact.PointA)).Length<1e-8);
            Assert.Contains(reversed.Points.ToArray(),p=>(p.PointB-contact.PointA).Length<1e-8&&(p.PointA-contact.PointB).Length<1e-8);
        }
    }

    [Fact]
    public void FeaturesFollowTheSameCapturedTrajectoryAsSupportQueries()
    {
        var body=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,RigidPose.At(new(2,3,4)),new(1,2,3),new(.3,.4,.5));
        var path=body.CreateTrajectory(.3,default);
        var motion=new ConvexMotion(new(new ConvexBox(new(1,2,3)),new(AffineBasis.Identity,new(.5f,0,0))),path);
        var pose=path.At(.2); var direction=pose.Rotation.Apply(Up);
        var feature=motion.At(.2).SupportingFeature(direction,1e-7);
        Assert.Equal(4,feature.Vertices.Length);
        foreach(var vertex in feature.Vertices)
        {
            var local=pose.InverseTransformPoint(vertex.Point);
            Assert.InRange(local.Y,1.999999999,2.000000001);
            Assert.InRange(Math.Abs(local.X-.5),.999999999,1.000000001);
        }
    }

    [Fact]
    public void SlightlyNoncoplanarFeaturesKeepAnchorsInsideActualGeometry()
    {
        var a=Feature(new(-1,0,-1),new(1,0,-1),new(1,5e-8,1),new(-1,5e-8,1));
        var b=Feature(new CollisionVector(0,1,0));
        var patch=ContactPatch.Clip(a,b,Up);
        var contact=Assert.Single(patch.Points.ToArray());
        Near(new(0,2.5e-8,0),contact.PointA);
        Near(new(0,1,0),contact.PointB);
    }

    [Fact]
    public void SphereAndHullFeaturesClipUsingThePenetrationNormal()
    {
        ConvexGeometry[] geometries=[new ConvexSphere(.8),new ConvexBox(new(1,.9,.7)),
            new ConvexHull([new(-1,-.9,-.7),new(1,-.9,-.7),new(-1,.9,-.7),new(1,.9,-.7),
                new(-1,-.9,.7),new(1,-.9,.7),new(-1,.9,.7),new(1,.9,.7)])];
        foreach(var first in geometries)
        foreach(var second in geometries)
        {
            var a=At(first,RigidPose.Identity);
            var b=At(second,new(new(.4,.2,.1),RigidRotation.FromRotationVector(new(.1,.2,.3))));
            var query=ConvexPenetration.Query(a,b);
            var patch=ContactPatch.Clip(a.SupportingFeature(-query.Normal,1e-7),b.SupportingFeature(query.Normal,1e-7),query.Normal);
            Assert.True(patch.Points.Length>0,$"Missing patch for {first.GetType().Name}, {second.GetType().Name}");
        }
    }

    [Fact]
    public void PenetratingRandomBoxesHaveNonemptyClippedFeaturesWithConsistentDepth()
    {
        var random=new Random(612);
        for(var i=0;i<200;i++)
        {
            var geometry=new ConvexBox(new(1,.8,.6));
            var a=At(geometry,RigidPose.Identity);
            var b=At(geometry,new(new(random.NextDouble()-.5,random.NextDouble()-.5,random.NextDouble()-.5),
                RigidRotation.FromRotationVector(new(random.NextDouble(),random.NextDouble(),random.NextDouble()))));
            var query=ConvexPenetration.Query(a,b);
            Assert.Equal(ConvexPenetrationStatus.Penetrating,query.Status);
            var patch=ContactPatch.Clip(a.SupportingFeature(-query.Normal,1e-7),b.SupportingFeature(query.Normal,1e-7),query.Normal);
            Assert.True(patch.Points.Length>0,$"Missing patch at sample {i}");
            foreach(var point in patch.Points) Assert.InRange(point.Separation,-query.UpperDepth-2e-7,-query.LowerDepth+2e-7);
        }
    }

    [Theory]
    [InlineData(83)]
    [InlineData(142)]
    public void ManifoldsCoverCurvedAndFlatPairsAcrossContactAndClearance(int seed)
    {
        var random=new Random(seed);
        ConvexGeometry[] shapes=[new ConvexSphere(.7),new ConvexBox(new(.8,.9,1)),
            new ConvexHull([new(-1,-1,-1),new(1,-1,-1),new(0,1,-1),new(0,0,1)])];
        for(var i=0;i<150;i++)
        {
            var a=At(shapes[i%shapes.Length],RigidPose.Identity);
            var b=At(shapes[(i/shapes.Length)%shapes.Length],
                new(new(random.NextDouble()*3-1.5,random.NextDouble()*3-1.5,random.NextDouble()*3-1.5),
                RigidRotation.FromRotationVector(new(random.NextDouble(),random.NextDouble(),random.NextDouble()))));
            var separation=ConvexDistance.Query(a,b);
            var manifold=ContactManifold.Query(a,b);
            if(separation.LowerBound>ConvexSweep.ContactDistance) Assert.Equal(ContactManifoldStatus.Clear,manifold.Status);
            else
            {
                Assert.Equal(ContactManifoldStatus.Contact,manifold.Status);
                Assert.NotEmpty(manifold.Points.ToArray());
                foreach(var point in manifold.Points)
                    Near(point.PointA-point.PointB,manifold.Normal*point.Separation,2e-7);
            }
        }
    }

    [Fact]
    public void ManifoldIncludesTheCcdSkinAndCoincidentPointsButRejectsDistantContacts()
    {
        var box=new ConvexBox(new(1,1,1));
        var a=At(box,RigidPose.Identity);
        var b=At(box,RigidPose.At(new(0,2.00005,0)));
        var manifold=ContactManifold.Query(a,b);
        Assert.Equal(ContactManifoldStatus.Contact,manifold.Status);
        Assert.Equal(4,manifold.Points.Length);
        Assert.All(manifold.Points.ToArray(),p=>Assert.InRange(p.Separation,.000049999,.000050001));
        Assert.Equal(ContactManifoldStatus.Clear,ContactManifold.Query(a,At(box,RigidPose.At(new(0,3,0)))).Status);
        var point=At(new ConvexHull([default]),RigidPose.Identity);
        var coincident=ContactManifold.Query(point,point);
        Assert.Equal(ContactManifoldStatus.Contact,coincident.Status);
        Assert.Equal(0,Assert.Single(coincident.Points.ToArray()).Separation);
        Assert.Equal(new CollisionVector(-1,0,0),coincident.Normal);
        Assert.Throws<ArgumentOutOfRangeException>(()=>ContactManifold.Query(a,b,-1));
    }

    [Fact]
    public void InvalidGeometryAndTolerancesAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(()=>new SupportVertexId(-1));
        Assert.Throws<ArgumentException>(()=>new SupportFeature([]));
        Assert.Throws<ArgumentException>(()=>new SupportFeature([new(new(0),default),new(new(0),Up)]));
        Assert.Throws<ArgumentException>(()=>Feature(new CollisionVector(double.NaN,0,0)));
        Assert.Throws<ArgumentOutOfRangeException>(()=>new ConvexSphere(1).SupportingFeature(default,1e-7));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ContactPatch.Clip(Square(0),Square(0),Up,0));
        Assert.Throws<ArgumentException>(()=>ContactPatch.Clip(Feature(default,Up),Square(0),Up));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ContactPatch.Clip(Square(0),Square(0),Up,double.NaN));
    }
}
