using Godot;
using CuriousContraptions.Physics;

namespace CuriousContraptions.Tests;

public enum HollowFixture { Tube, Frustum, Bend45, Bend90 }

public class HollowGeometryTests
{
    private static HollowGeometryResult Build(HollowFixture kind,double error=.01)=>kind switch
    {
        HollowFixture.Tube=>HollowGeometry.Tube(2,.65,.70,new(error)),
        HollowFixture.Frustum=>HollowGeometry.Frustum(.9,1.3,.65,.05,new(error)),
        HollowFixture.Bend45=>HollowGeometry.Bend(2.4,Math.PI/4,.65,.70,new(error)),
        HollowFixture.Bend90=>HollowGeometry.Bend(2.4,Math.PI/2,.65,.70,new(error)),
        _=>throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static BodyTrajectory Path(RigidPose pose,CollisionVector velocity=default,CollisionVector spin=default,double duration=1)=>
        new PhysicsBody(new(0),PhysicsMotionType.Kinematic,pose,velocity,spin).CreateTrajectory(duration,default);
    private readonly record struct Point(CollisionVector Position):IConvexSupport
    {
        public InteriorBall InteriorBall=>new(Position,0);
        public CollisionVector Support(CollisionVector direction)=>Position;
    }
    private static CollisionVector OnBend(double along,double ring,double radius)=>
        new((2.4+radius*Math.Cos(ring))*Math.Sin(along),(2.4+radius*Math.Cos(ring))*Math.Cos(along),radius*Math.Sin(ring));

    [Theory]
    [InlineData(HollowFixture.Tube)]
    [InlineData(HollowFixture.Frustum)]
    [InlineData(HollowFixture.Bend45)]
    [InlineData(HollowFixture.Bend90)]
    public void DeclaredAnalyticMaterialIsCoveredByItsCorrespondingConvexCell(HollowFixture kind)
    {
        var shape=Build(kind); var random=new Random(316);
        var bend=kind is HollowFixture.Bend45 or HollowFixture.Bend90;
        var sweep=kind==HollowFixture.Bend45?Math.PI/4:Math.PI/2;
        for(var sample=0;sample<240;sample++)
        {
            var ring=sample%shape.RingSegments; var along=sample%shape.PathSegments;
            var phi=Math.Tau*(ring+random.NextDouble())/shape.RingSegments;
            var t=random.NextDouble(); var u=sample%3==0?0:sample%3==1?1:random.NextDouble();
            CollisionVector point;
            if(bend) point=OnBend(sweep*(along+t)/shape.PathSegments,phi,.65+.05*u);
            else
            {
                var half=kind==HollowFixture.Tube?2:.9; var inner=kind==HollowFixture.Tube?.65:1.3-.65*t;
                point=new((2*t-1)*half,(inner+.05*u)*Math.Cos(phi),(inner+.05*u)*Math.Sin(phi));
            }
            var child=shape.Geometry[new(along*shape.RingSegments+ring)];
            var result=ConvexDistance.Query(child,new Point(point));
            Assert.True(result.UpperBound<=1e-7,$"{kind} sample {sample}, distance {result.UpperBound:R}");
        }
        Assert.InRange(shape.MaximumSurfaceError,0,.01);
        Assert.True(shape.MinimumBoreRadius>=.64);
    }

    private static double DistanceToAnalyticShell(HollowFixture kind,CollisionVector point)
    {
        if(kind is HollowFixture.Tube or HollowFixture.Frustum)
        {
            var half=kind==HollowFixture.Tube?2:.9;
            var x=Math.Clamp(point.X,-half,half);
            var inner=kind==HollowFixture.Tube?.65:1.3-.65*((x+half)/(2*half));
            var radius=Math.Sqrt(point.Y*point.Y+point.Z*point.Z);
            var radial=radius-Math.Clamp(radius,inner,inner+.05);
            return Math.Sqrt(Math.Pow(point.X-x,2)+radial*radial);
        }
        var sweep=kind==HollowFixture.Bend45?Math.PI/4:Math.PI/2;
        var angle=Math.Clamp(Math.Atan2(point.X,point.Y),0,sweep);
        var radialDirection=new CollisionVector(Math.Sin(angle),Math.Cos(angle),0);
        var center=radialDirection*2.4;
        var delta=point-center;
        var cross=radialDirection*CollisionVector.Dot(delta,radialDirection)+new CollisionVector(0,0,point.Z);
        var length=cross.Length;
        var direction=length==0?radialDirection:cross/length;
        return (point-(center+direction*Math.Clamp(length,.65,.70))).Length;
    }

    [Theory]
    [InlineData(HollowFixture.Tube)]
    [InlineData(HollowFixture.Frustum)]
    [InlineData(HollowFixture.Bend45)]
    [InlineData(HollowFixture.Bend90)]
    public void ConvexCellSamplesStayWithinTheReportedSurfaceError(HollowFixture kind)
    {
        var shape=Build(kind); var random=new Random(8082);
        for(var sample=0;sample<500;sample++)
        {
            var child=shape.Geometry[new(sample%shape.Geometry.Count)];
            CollisionVector Vector()=>new(random.NextDouble()*2-1,random.NextDouble()*2-1,random.NextDouble()*2-1);
            // Both support-surface points and convex combinations exercise the
            // entire hull, not just the parameterized interpolation surface.
            var p=child.Support(Vector());
            if(sample%2==0) p=(p+child.Support(Vector())+child.Support(Vector()))/3;
            Assert.True(DistanceToAnalyticShell(kind,p)<=shape.MaximumSurfaceError+1e-12,
                $"{kind} sample {sample} exceeded {shape.MaximumSurfaceError:R}");
        }
    }

    [Theory]
    [InlineData(HollowFixture.Tube)]
    [InlineData(HollowFixture.Frustum)]
    [InlineData(HollowFixture.Bend45)]
    [InlineData(HollowFixture.Bend90)]
    public void BoreRetainsItsReportedClearanceAndChildOrderIsDeterministic(HollowFixture kind)
    {
        var shape=Build(kind); var again=Build(kind);
        Assert.Equal(shape.Geometry.Count,again.Geometry.Count);
        var bend=kind is HollowFixture.Bend45 or HollowFixture.Bend90;
        for(var sample=0;sample<=8;sample++)
        {
            var fraction=sample/8.0;
            var center=bend?OnBend((kind==HollowFixture.Bend45?Math.PI/4:Math.PI/2)*fraction,0,0):
                new CollisionVector((2*fraction-1)*(kind==HollowFixture.Tube?2:.9),0,0);
            for(var i=0;i<shape.Geometry.Count;i++)
            {
                var child=shape.Geometry[new(i)];
                Assert.Equal(child.Support(new(1,2,3)),again.Geometry[new(i)].Support(new(1,2,3)));
                var bounds=CollisionBounds.Of(child);
                if(bounds.DistanceLowerBound(new(center,center))>=shape.MinimumBoreRadius) continue;
                var distance=ConvexDistance.Query(child,new Point(center));
                Assert.True(distance.LowerBound>=shape.MinimumBoreRadius-1e-7);
            }
        }
    }

    [Theory]
    [InlineData(HollowFixture.Tube,false)]
    [InlineData(HollowFixture.Tube,true)]
    [InlineData(HollowFixture.Frustum,false)]
    [InlineData(HollowFixture.Frustum,true)]
    public void HighSpeedRotatedAxialPassageIsOpenButItsWallStopsMotion(HollowFixture kind,bool wall)
    {
        var shape=Build(kind,.005); var rotation=RigidRotation.FromRotationVector(new(.2,.7,-.4));
        var center=new CollisionVector(2,3,-1);
        var y=wall?(kind==HollowFixture.Tube?.675:1.325):0;
        var start=center+rotation.Apply(new(-4,y,0));
        var ball=new CompoundMotion(new([new(new ConvexSphere(.05),AffineTransform.Identity)]),
            Path(new(start,rotation),rotation.Apply(new(1000,0,0)),duration:.008));
        var shell=new CompoundMotion(shape.Geometry,Path(new(center,rotation),duration:.008));
        var result=CompoundCollision.Cast(ball,shell,.008,ConvexSweep.ContactDistance);
        Assert.Equal(wall?ConvexSweepStatus.Contact:ConvexSweepStatus.Clear,result.Status);
        if(wall)
        {
            Assert.NotNull(result.ChildB);
            var half=kind==HollowFixture.Tube?2:.9;
            Assert.InRange(Math.Abs(result.Time-(4-half-.05-ConvexSweep.ContactDistance)/1000),0,1e-8);
        }
    }

    [Theory]
    [InlineData(HollowFixture.Bend45)]
    [InlineData(HollowFixture.Bend90)]
    public void ARealRotatingTrajectoryCanFollowTheCurvedOpenBore(HollowFixture kind)
    {
        var shape=Build(kind,.01);
        var sweep=kind==HollowFixture.Bend45?Math.PI/4:Math.PI/2;
        var ball=new CompoundMotion(new([new(new ConvexSphere(.25),new(AffineBasis.Identity,new(0,2.4f,0)))]),
            Path(RigidPose.Identity,spin:new(0,0,-sweep)));
        var shell=new CompoundMotion(shape.Geometry,Path(RigidPose.Identity));
        var result=CompoundCollision.Cast(ball,shell,1,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Clear,result.Status);
    }

    [Fact]
    public void TighterBudgetsIncreaseResolutionRatherThanSilentlyRelaxingGeometry()
    {
        var coarse=HollowGeometry.Bend(2.4,Math.PI/2,.65,.7,new(.02));
        var fine=HollowGeometry.Bend(2.4,Math.PI/2,.65,.7,new(.005));
        Assert.True(fine.Geometry.Count>coarse.Geometry.Count);
        Assert.True(fine.MaximumSurfaceError<coarse.MaximumSurfaceError);
        Assert.True(fine.MinimumBoreRadius>coarse.MinimumBoreRadius);
        Assert.Throws<ArgumentException>(()=>HollowGeometry.Bend(2.4,Math.PI/2,.65,.7,new(.000001,100)));
        Assert.Throws<ArgumentException>(()=>HollowGeometry.Tube(1,.65,.7,new(.000000001,10)));
        Assert.Throws<ArgumentException>(()=>HollowGeometry.Tube(1,.65,.7,new(.65)));
        Assert.Throws<ArgumentException>(()=>HollowGeometry.Tube(1,.7,.65,new(.01)));
        Assert.Throws<ArgumentException>(()=>HollowGeometry.Frustum(1,1,.5,0,new(.01)));
        Assert.Throws<ArgumentException>(()=>HollowGeometry.Bend(.7,Math.PI/2,.65,.7,new(.01)));
        Assert.Throws<ArgumentException>(()=>HollowGeometry.Bend(2.4,Math.Tau,.65,.7,new(.01)));
        Assert.Throws<ArgumentException>(()=>HollowGeometry.Tube(1,.65,.7,new(1e-16)));
    }

    [Fact]
    public void SweptBoundsContainRotatedTranslatedChildSupportAtEverySample()
    {
        var random=new Random(1729);
        CollisionVector Vector()=>new(random.NextDouble()*2-1,random.NextDouble()*2-1,random.NextDouble()*2-1);
        for(var sample=0;sample<100;sample++)
        {
            var child=new ConvexInstance(new ConvexBox(new(.2,.4,.6)),
                new(AffineBasis.Identity,new((float)(3*random.NextDouble()),1,-2)));
            var path=Path(new(Vector(),RigidRotation.FromRotationVector(Vector())),Vector()*10,Vector()*40,.1);
            var motion=new ConvexMotion(child,path); var bounds=CollisionBounds.Swept(motion,.1);
            for(var point=0;point<=100;point++)
            {
                var actual=CollisionBounds.Of(motion.At(.001*point));
                Assert.True(actual.Minimum.X>=bounds.Minimum.X-1e-12&&actual.Minimum.Y>=bounds.Minimum.Y-1e-12&&actual.Minimum.Z>=bounds.Minimum.Z-1e-12);
                Assert.True(actual.Maximum.X<=bounds.Maximum.X+1e-12&&actual.Maximum.Y<=bounds.Maximum.Y+1e-12&&actual.Maximum.Z<=bounds.Maximum.Z+1e-12);
            }
        }
        var stationary=new ConvexMotion(new(new ConvexSphere(.2),new(AffineBasis.Identity,new(10,0,0))),Path(RigidPose.Identity));
        Assert.Equal(CollisionBounds.Of(stationary.At(0)),CollisionBounds.Swept(stationary,1));
    }
    [Theory]
    [InlineData(.09,.78)]
    [InlineData(.5,.70)]
    [InlineData(4,.70)]
    public void AuthoredTubeLengthsAndCollarsRetainTheirOpenBore(double halfLength,double outer)
    {
        var shape=HollowGeometry.Tube(halfLength,.65,outer,new(.0025));
        Assert.InRange(shape.MaximumSurfaceError,0,.0025);
        Assert.True(shape.MinimumBoreRadius>=.6475);
        var ball=new CompoundMotion(new([new(new ConvexSphere(.3),AffineTransform.Identity)]),
            Path(RigidPose.At(new(-halfLength-1,0,0)),new(2*halfLength+2,0,0)));
        Assert.Equal(ConvexSweepStatus.Clear,CompoundCollision.Cast(ball,
            new(shape.Geometry,Path(RigidPose.Identity)),1,ConvexSweep.ContactDistance).Status);
    }

    [Theory]
    [InlineData(HollowFixture.Bend45)]
    [InlineData(HollowFixture.Bend90)]
    public void HighSpeedCrossSectionImpactCannotTunnelThroughABendWall(HollowFixture kind)
    {
        var shape=Build(kind,.005); var sweep=kind==HollowFixture.Bend45?Math.PI/4:Math.PI/2;
        var center=OnBend(sweep*.5,0,0);
        var ball=new CompoundMotion(new([new(new ConvexSphere(.05),AffineTransform.Identity)]),
            Path(RigidPose.At(center),new(0,0,1000),duration:.002));
        var result=CompoundCollision.Cast(ball,new(shape.Geometry,Path(RigidPose.Identity)),.002,ConvexSweep.ContactDistance);
        Assert.Equal(ConvexSweepStatus.Contact,result.Status); Assert.NotNull(result.ChildB);
        Assert.InRange(result.Time,(.6-shape.MaximumSurfaceError-ConvexSweep.ContactDistance)/1000,.0006);
    }

    [Fact]
    public void JoinedTubeWallSupportsSlidingWithoutAnInternalCapOrVelocityKick()
    {
        var tube=HollowGeometry.Tube(1,.65,.7,new(.005));
        var children=new List<ConvexInstance>();
        for(var section=0;section<2;section++)
        for(var i=0;i<tube.Geometry.Count;i++)
        {
            var child=tube.Geometry[new(i)];
            children.Add(new(child.Geometry,new(AffineBasis.Identity,child.Pose.Origin+new CollisionVector(section*2,0,0))));
        }
        var body=new PhysicsBody(new(0),PhysicsMotionType.Dynamic,RigidPose.At(new(-.5,-.44,0)),new(2,0,0),default,1,new(.016,.016,.016));
        var wall=new PhysicsBody(new(1),PhysicsMotionType.Static,RigidPose.Identity,default,default);
        var world=new PhysicsWorld([],[new(body,new([new(new ConvexSphere(.2),AffineTransform.Identity)]),new(0,0,0)),
            new(wall,new(children.ToArray()),new(0,0,0))],[],new(new(0,-9.8,0)));
        var before=world.Capture();
        for(var i=0;i<120;i++) world.Step([],[],1.0/120);
        // Compare drift against the world's configured spatial accuracy.
        Assert.InRange(Math.Abs(body.Center.X-1.5),0,1e-6);
        Assert.InRange(Math.Abs(body.LinearVelocity.X-2),0,1e-6);
        Assert.InRange(Math.Abs(body.LinearVelocity.Y)+body.AngularVelocity.Length,0,1e-6);
        Assert.InRange(body.Center.Y,-.451,-.44);
        var after=world.Capture(); var impacts=world.Impacts.ToArray();
        world.Restore(before); Assert.Equal(before.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        for(var i=0;i<120;i++) world.Step([],[],1.0/120);
        Assert.Equal(after.BodyStates.ToArray(),world.Capture().BodyStates.ToArray());
        Assert.Equal(impacts,world.Impacts.ToArray()); Assert.Equal(after.Time,world.Time);
    }

}
