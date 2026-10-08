using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>Analytic derivatives of an active convex feature gap. Rounding is
/// removed before differentiating: a rolling sphere's material surface point is
/// not the point whose centripetal acceleration defines the geometric gap.</summary>
public sealed class ContactGap
{
    private enum FeatureRank { Point, Edge, Face }
    private enum GapGeometry { Points, LineA, LineB, CrossedEdges, PlaneA, PlaneB }
    private readonly record struct Feature(FeatureRank Rank,CollisionVector Edge,CollisionVector Normal,double Width);
    private readonly record struct Scalar(double Value,double First,double Second)
    {
        public static Scalar operator -(Scalar a,Scalar b)=>new(a.Value-b.Value,a.First-b.First,a.Second-b.Second);
        public static Scalar operator *(Scalar a,Scalar b)=>new(a.Value*b.Value,
            a.First*b.Value+a.Value*b.First,a.Second*b.Value+2*a.First*b.First+a.Value*b.Second);
        public static Scalar operator /(Scalar a,Scalar b)
        {
            if(b.Value==0) throw new InvalidOperationException("Contact feature coordinates are singular.");
            var inverse=1/b.Value;
            return a*new Scalar(inverse,-b.First*inverse*inverse,
                2*b.First*b.First*inverse*inverse*inverse-b.Second*inverse*inverse);
        }
    }
    private readonly record struct Vector(CollisionVector Value,CollisionVector First,CollisionVector Second)
    {
        public static Vector operator +(Vector a,Vector b)=>new(a.Value+b.Value,a.First+b.First,a.Second+b.Second);
        public static Vector operator -(Vector a,Vector b)=>new(a.Value-b.Value,a.First-b.First,a.Second-b.Second);
        public static Vector operator *(Vector a,Scalar b)=>new(a.Value*b.Value,
            a.First*b.Value+a.Value*b.First,a.Second*b.Value+a.First*(2*b.First)+a.Value*b.Second);
        public static Scalar Dot(Vector a,Vector b)=>new(CollisionVector.Dot(a.Value,b.Value),
            CollisionVector.Dot(a.First,b.Value)+CollisionVector.Dot(a.Value,b.First),
            CollisionVector.Dot(a.Second,b.Value)+2*CollisionVector.Dot(a.First,b.First)+CollisionVector.Dot(a.Value,b.Second));
        public static Vector Cross(Vector a,Vector b)=>new(CollisionVector.Cross(a.Value,b.Value),
            CollisionVector.Cross(a.First,b.Value)+CollisionVector.Cross(a.Value,b.First),
            CollisionVector.Cross(a.Second,b.Value)+CollisionVector.Cross(a.First,b.First)*2+CollisionVector.Cross(a.Value,b.Second));
        public Scalar Length()
        {
            var length=Value.Length;
            if(!double.IsFinite(length)||length==0)
                throw new InvalidOperationException("The active feature gap has no differentiable distance normal.");
            var first=CollisionVector.Dot(Value,First)/length;
            return new(length,first,(First.LengthSquared+CollisionVector.Dot(Value,Second)-first*first)/length);
        }
        public Vector Unit()
        {
            var length=Length(); var inverse=1/length.Value;
            return this*new Scalar(inverse,-length.First*inverse*inverse,
                2*length.First*length.First*inverse*inverse*inverse-length.Second*inverse*inverse);
        }
    }

    private readonly PhysicsBody _a,_b;
    private readonly GapGeometry _geometry;
    private readonly double _normalOrientation;
    private readonly CollisionVector _anchorA,_anchorB,_normal;
    private readonly Feature _featureA,_featureB;
    private readonly double _radiusA,_radiusB,_tolerance;
    public PhysicsBody A=>_a;
    public PhysicsBody B=>_b;
    public ConstraintJacobian NormalJacobian { get; }
    public CollisionVector Normal { get; }
    public CollisionVector Point { get; }
    public CollisionVector PointVelocity { get; }
    public CollisionVector NormalVelocity { get; }
    public CollisionVector SurfaceSlip { get; }
    public CollisionVector TangentialBias { get; }
    public ConstraintGradient Gradient { get; }
    public double Separation { get; }
    public double Rate=>Gradient.Speed;
    public double ConvectiveAcceleration { get; }
    public ConstraintAcceleration NormalConstraint=>new(Gradient,ConvectiveAcceleration,AccelerationRelation.Nonnegative);

    private ContactGap(PhysicsBody a,PhysicsBody b,CollisionVector anchorA,CollisionVector anchorB,
        Feature fa,Feature fb,CollisionVector normal,double radiusA,double radiusB,double tolerance,GapGeometry geometry)
    {
        _a=a; _b=b; _anchorA=a.Pose.InverseTransformPoint(anchorA); _anchorB=b.Pose.InverseTransformPoint(anchorB);
        static Feature Local(Feature feature,PhysicsBody body)=>new(feature.Rank,
            body.Pose.Rotation.Inverse().Apply(feature.Edge),body.Pose.Rotation.Inverse().Apply(feature.Normal),feature.Width);
        _featureA=Local(fa,a); _featureB=Local(fb,b);
        _normal=a.Pose.Rotation.Inverse().Apply(normal); _radiusA=radiusA; _radiusB=radiusB; _tolerance=tolerance;
        _geometry=geometry;
        var reference=geometry switch
        {
            GapGeometry.PlaneA=>fa.Normal,
            GapGeometry.PlaneB=>fb.Normal,
            GapGeometry.CrossedEdges=>CollisionVector.Cross(fa.Edge,fb.Edge),
            _=>normal
        };
        _normalOrientation=CollisionVector.Dot(reference,normal)<0?-1:1;
        var offsetA=anchorA-a.Center; var offsetB=anchorB-b.Center;
        static Vector Rotate(CollisionVector value,CollisionVector spin)=>
            new(value,CollisionVector.Cross(spin,value),CollisionVector.Cross(spin,CollisionVector.Cross(spin,value)));
        (Scalar Gap,Vector Normal,Vector Point) Evaluate(CollisionVector va,CollisionVector wa,CollisionVector vb,CollisionVector wb)
        {
            var ra=Rotate(offsetA,wa); var rb=Rotate(offsetB,wb);
            var pa=new Vector(anchorA,va+ra.First,ra.Second); var pb=new Vector(anchorB,vb+rb.First,rb.Second);
            var delta=pa-pb; var ca=pa; var cb=pb;
            Vector axis;
            switch(geometry)
            {
                case GapGeometry.PlaneA: axis=Rotate(fa.Normal,wa); break;
                case GapGeometry.PlaneB: axis=Rotate(fb.Normal,wb); break;
                case GapGeometry.CrossedEdges:
                    var u=Rotate(fa.Edge,wa); var v=Rotate(fb.Edge,wb);
                    axis=Vector.Cross(u,v).Unit();
                    var c=Vector.Dot(u,v); var du=Vector.Dot(u,delta); var dv=Vector.Dot(v,delta);
                    var denominator=new Scalar(1,0,0)-c*c;
                    ca=pa+u*((c*dv-du)/denominator);
                    cb=pb+v*((dv-c*du)/denominator);
                    break;
                case GapGeometry.LineA:
                    var ea=Rotate(fa.Edge,wa); axis=(delta-ea*Vector.Dot(delta,ea)).Unit(); break;
                case GapGeometry.LineB:
                    var eb=Rotate(fb.Edge,wb); axis=(delta-eb*Vector.Dot(delta,eb)).Unit(); break;
                case GapGeometry.Points: axis=delta.Unit(); break;
                default: throw new InvalidOperationException("Undefined contact feature geometry.");
            }
            if(geometry is GapGeometry.PlaneA or GapGeometry.PlaneB or GapGeometry.CrossedEdges)
                axis=axis*new Scalar(_normalOrientation,0,0);
            var gap=Vector.Dot(delta,axis);
            if(geometry is GapGeometry.PlaneA or GapGeometry.LineA) ca=pb+axis*gap;
            if(geometry is GapGeometry.PlaneB or GapGeometry.LineB) cb=pa-axis*gap;
            var point=(ca+cb+axis*new Scalar(radiusB-radiusA,0,0))*new Scalar(.5,0,0);
            return (gap,axis,point);
        }
        static CollisionVector Components(Func<CollisionVector,double> derivative)=>
            new(derivative(new(1,0,0)),derivative(new(0,1,0)),derivative(new(0,0,1)));
        var jacobian=new ConstraintJacobian(
            Components(v=>Evaluate(v,default,default,default).Gap.First),
            Components(w=>Evaluate(default,w,default,default).Gap.First),
            Components(v=>Evaluate(default,default,v,default).Gap.First),
            Components(w=>Evaluate(default,default,default,w).Gap.First));
        var sample=Evaluate(a.LinearVelocity,a.AngularVelocity,b.LinearVelocity,b.AngularVelocity);
        Separation=sample.Gap.Value-radiusA-radiusB; ConvectiveAcceleration=sample.Gap.Second;
        NormalJacobian=jacobian; Normal=sample.Normal.Value; Point=sample.Point.Value;
        PointVelocity=sample.Point.First; NormalVelocity=sample.Normal.First;
        var relative=a.PointVelocity(Point)-b.PointVelocity(Point);
        SurfaceSlip=relative-Normal*CollisionVector.Dot(Normal,relative);
        // Differentiate the material velocity field at the moving geometric
        // contact, not at a point rigidly attached to either rotating body.
        var bias=CollisionVector.Cross(a.AngularVelocity-b.AngularVelocity,sample.Point.First)-
            CollisionVector.Cross(a.AngularVelocity,a.LinearVelocity)+CollisionVector.Cross(b.AngularVelocity,b.LinearVelocity);
        TangentialBias=bias-Normal*CollisionVector.Dot(Normal,bias)-
            sample.Normal.First*CollisionVector.Dot(Normal,relative);
        if(!double.IsFinite(Separation)||!double.IsFinite(ConvectiveAcceleration)||!jacobian.IsFinite||!Point.IsFinite||!SurfaceSlip.IsFinite||!TangentialBias.IsFinite)
            throw new InvalidOperationException("Contact gap derivatives exceed numeric range.");
        Gradient=jacobian.Bind(a,b);
    }

    /// <summary>Evaluate the captured active feature branch on a prediction
    /// stage. Do not change its contact/flight state based on an unconverged
    /// midpoint: unilateral force bounds decide whether support is required.</summary>
    public ContactGap Rebind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> states)
    {
        ArgumentNullException.ThrowIfNull(states);
        if(!states.TryGetValue(_a.Id,out var a)||a is null||a.Id!=_a.Id||
            !states.TryGetValue(_b.Id,out var b)||b is null||b.Id!=_b.Id)
            throw new ArgumentException("Contact gap requires both declared prediction bodies.");
        static Feature World(Feature feature,PhysicsBody body)=>new(feature.Rank,
            body.Pose.Rotation.Apply(feature.Edge),body.Pose.Rotation.Apply(feature.Normal),feature.Width);
        var fa=World(_featureA,a); var fb=World(_featureB,b);
        var normal=_geometry switch
        {
            GapGeometry.PlaneA=>fa.Normal*_normalOrientation,
            GapGeometry.PlaneB=>fb.Normal*_normalOrientation,
            GapGeometry.CrossedEdges=>CollisionVector.Cross(fa.Edge,fb.Edge)*_normalOrientation,
            _=>a.Pose.Rotation.Apply(_normal)
        };
        return new(a,b,a.Pose.TransformPoint(_anchorA),b.Pose.TransformPoint(_anchorB),
            fa,fb,normal,_radiusA,_radiusB,_tolerance,_geometry);
    }

    private static GapGeometry SelectGeometry(PhysicsBody a,PhysicsBody b,Feature fa,Feature fb,double tolerance)=>
        fa.Rank==FeatureRank.Face&&(fb.Rank!=FeatureRank.Face||a.Id.Index<b.Id.Index)?GapGeometry.PlaneA:
            fb.Rank==FeatureRank.Face?GapGeometry.PlaneB:
            fa.Rank==FeatureRank.Edge&&fb.Rank==FeatureRank.Edge&&
                CollisionVector.Cross(fa.Edge,fb.Edge).Length*Math.Min(fa.Width,fb.Width)>tolerance?GapGeometry.CrossedEdges:
            fa.Rank==FeatureRank.Edge&&(fb.Rank!=FeatureRank.Edge||a.Id.Index<b.Id.Index)?GapGeometry.LineA:
            fb.Rank==FeatureRank.Edge?GapGeometry.LineB:GapGeometry.Points;

    public static ContactGap[] Query(PhysicsBody a,ConvexInstance shapeA,PhysicsBody b,ConvexInstance shapeB,
        double contactDistance,double tolerance)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(a==b||a.Id==b.Id) throw new ArgumentException("Contact gap requires distinct bodies.");
        var sa=new ConvexPose(shapeA,a.Pose);
        var sb=new ConvexPose(shapeB,b.Pose);
        var manifold=ContactManifold.Query(sa,sb,contactDistance,tolerance);
        if(manifold.Status==ContactManifoldStatus.Clear) return [];
        var normal=manifold.Normal;
        var fa=Describe(sa.SupportingFeature(-normal,tolerance*.5),tolerance);
        var fb=Describe(sb.SupportingFeature(normal,tolerance*.5),tolerance);
        var result=new List<ContactGap>();
        foreach(var point in manifold.Points)
            result.Add(new(a,b,point.PointA+normal*sa.RoundingRadius,point.PointB-normal*sb.RoundingRadius,
                fa,fb,normal,sa.RoundingRadius,sb.RoundingRadius,tolerance,SelectGeometry(a,b,fa,fb,tolerance)));
        return result.ToArray();
    }

    public ContactSlipSample SlipAt(BodyTrajectory a,BodyTrajectory b,double time)
    {
        a.ValidateSource(_a); b.ValidateSource(_b);
        var pa=a.At(time); var pb=b.At(time);
        var va=a.LinearVelocityAt(time); var vb=b.LinearVelocityAt(time);
        var wa=a.PhysicalAngularVelocityAt(time); var wb=b.PhysicalAngularVelocityAt(time);
        var geometry=Rebind(new Dictionary<PhysicsBodyId,PhysicsBody>
        {
            {_a.Id,new(_a.Id,PhysicsMotionType.Kinematic,pa,va,a.AngularVelocityAt(time))},
            {_b.Id,new(_b.Id,PhysicsMotionType.Kinematic,pb,vb,b.AngularVelocityAt(time))}
        });
        var ra=geometry.Point-pa.Center; var rb=geometry.Point-pb.Center;
        var relative=va-vb+CollisionVector.Cross(wa,ra)-CollisionVector.Cross(wb,rb);
        var rate=a.LinearAccelerationAt(time)-b.LinearAccelerationAt(time)+
            CollisionVector.Cross(a.PhysicalAngularAccelerationAt(time),ra)-
            CollisionVector.Cross(b.PhysicalAngularAccelerationAt(time),rb)+
            CollisionVector.Cross(wa,geometry.PointVelocity-va)-CollisionVector.Cross(wb,geometry.PointVelocity-vb);
        var normal=geometry.Normal; var change=geometry.NormalVelocity;
        var paths=new Dictionary<PhysicsBodyId,BodyTrajectory>{{_a.Id,a},{_b.Id,b}};
        var slip=ContactKinematics.AtPoint(geometry.A,geometry.B,geometry.Point,normal).SlipAlong(paths,time);
        return new(slip,
            rate-normal*CollisionVector.Dot(normal,rate)-change*CollisionVector.Dot(normal,relative)-
                normal*CollisionVector.Dot(change,relative));
    }

    /// <summary>Conservative derivative and curvature norms of committed slip
    /// on one smooth rotational segment. Null means feature denominators are
    /// uncertified; subdivide the interval, never assume it clear.</summary>
    public ContactSlipBounds? SlipBounds(BodyTrajectory a,BodyTrajectory b,double start,double end)
    {
        a.ValidateSource(_a); b.ValidateSource(_b);
        if(!double.IsFinite(start)||!double.IsFinite(end)||start<0||end<start||end>a.Duration||end>b.Duration)
            throw new ArgumentOutOfRangeException(nameof(end));
        if(end>Math.Min(a.SegmentEndAfter(start),b.SegmentEndAfter(start)))
            throw new ArgumentException("Slip curvature interval crosses a rotational segment boundary.");
        var h=end-start; var pa=a.At(start); var pb=b.At(start);
        var ra=pa.Rotation.Apply(_anchorA); var rb=pb.Rotation.Apply(_anchorB);
        var delta=pa.Center-pb.Center+ra-rb;
        var sa=a.AngularSpeedBound; var sb=b.AngularSpeedBound;
        var asa=a.AngularAccelerationBound;var asb=b.AngularAccelerationBound;
        var va=Math.Max(a.LinearVelocityAt(start).Length,a.LinearVelocityAt(end).Length)+a.LinearAccelerationBound*h*.5;
        var vb=Math.Max(b.LinearVelocityAt(start).Length,b.LinearVelocityAt(end).Length)+b.LinearAccelerationBound*h*.5;
        var relativeSpeed=Math.Max((a.LinearVelocityAt(start)-b.LinearVelocityAt(start)).Length,
            (a.LinearVelocityAt(end)-b.LinearVelocityAt(end)).Length);
        var relativeAcceleration=a.LinearAccelerationBound+b.LinearAccelerationBound;
        relativeSpeed+=relativeAcceleration*h*.5;
        var deltaSpeed=relativeSpeed+sa*ra.Length+sb*rb.Length;
        var deltaAcceleration=relativeAcceleration+(asa+sa*sa)*ra.Length+(asb+sb*sb)*rb.Length;
        var distance=delta.Length+deltaSpeed*h;
        var pointA=va+sa*ra.Length; var pointB=vb+sb*rb.Length;
        var pointAccelerationA=a.LinearAccelerationBound+(asa+sa*sa)*ra.Length;
        var pointAccelerationB=b.LinearAccelerationBound+(asb+sb*sb)*rb.Length;
        var ea=pa.Rotation.Apply(_featureA.Edge); var eb=pb.Rotation.Apply(_featureB.Edge);
        double normalSpeed,normalAcceleration,pointSpeed,pointAcceleration;
        // For n=x/|x|, |n'| <= |x'|/r and
        // |n''| <= |x''|/r + 3|x'|²/r², with certified r>0.
        switch(_geometry)
        {
            case GapGeometry.PlaneA: normalSpeed=sa; normalAcceleration=asa+sa*sa; break;
            case GapGeometry.PlaneB: normalSpeed=sb; normalAcceleration=asb+sb*sb; break;
            case GapGeometry.Points:
                var minimum=delta.Length-deltaSpeed*h;
                if(minimum<=_tolerance) return null;
                normalSpeed=deltaSpeed/minimum;
                normalAcceleration=deltaAcceleration/minimum+3*normalSpeed*normalSpeed;
                break;
            case GapGeometry.LineA:
            case GapGeometry.LineB:
                var edge=_geometry==GapGeometry.LineA?ea:eb;
                var spin=_geometry==GapGeometry.LineA?sa:sb;
                var spinAcceleration=_geometry==GapGeometry.LineA?asa:asb;
                var radial=delta-edge*CollisionVector.Dot(delta,edge);
                var radialSpeed=deltaSpeed+2*spin*distance;
                var radialMinimum=radial.Length-radialSpeed*h;
                if(radialMinimum<=_tolerance) return null;
                normalSpeed=radialSpeed/radialMinimum;
                normalAcceleration=(deltaAcceleration+4*spin*deltaSpeed+(2*spinAcceleration+4*spin*spin)*distance)/radialMinimum+
                    3*normalSpeed*normalSpeed;
                break;
            case GapGeometry.CrossedEdges:
                var crossMinimum=CollisionVector.Cross(ea,eb).Length-(sa+sb)*h;
                if(crossMinimum*Math.Min(_featureA.Width,_featureB.Width)<=_tolerance) return null;
                normalSpeed=(sa+sb)/crossMinimum;
                normalAcceleration=(asa+asb+(sa+sb)*(sa+sb))/crossMinimum+3*normalSpeed*normalSpeed;
                break;
            default: throw new InvalidOperationException("Undefined contact feature geometry.");
        }
        var gapSpeed=deltaSpeed+normalSpeed*distance;
        var gapAcceleration=deltaAcceleration+2*normalSpeed*deltaSpeed+normalAcceleration*distance;
        var radiusDifference=Math.Abs(_radiusA-_radiusB);
        var offsetSpeed=(normalSpeed*(distance+radiusDifference)+gapSpeed)*.5;
        var offsetAcceleration=(normalAcceleration*(distance+radiusDifference)+2*normalSpeed*gapSpeed+gapAcceleration)*.5;
        switch(_geometry)
        {
            case GapGeometry.PlaneA:
            case GapGeometry.LineA:
                pointSpeed=pointB+offsetSpeed;
                pointAcceleration=pointAccelerationB+offsetAcceleration; break;
            case GapGeometry.PlaneB:
            case GapGeometry.LineB:
                pointSpeed=pointA+offsetSpeed;
                pointAcceleration=pointAccelerationA+offsetAcceleration; break;
            case GapGeometry.Points:
                pointSpeed=(pointA+pointB+normalSpeed*radiusDifference)*.5;
                pointAcceleration=(pointAccelerationA+pointAccelerationB+normalAcceleration*radiusDifference)*.5;
                break;
            case GapGeometry.CrossedEdges:
                var sum=sa+sb;
                var lower=CollisionVector.Cross(ea,eb).Length-sum*h;
                var denominator=lower*lower;
                var numerator=2*distance;
                var numeratorSpeed=2*sum*distance+2*deltaSpeed;
                var numeratorAcceleration=(2*(asa+asb)+4*sum*sum)*distance+4*sum*deltaSpeed+2*deltaAcceleration;
                var denominatorSpeed=2*sum; var denominatorAcceleration=2*(asa+asb)+4*sum*sum;
                var coordinate=numerator/denominator;
                var coordinateSpeed=numeratorSpeed/denominator+numerator*denominatorSpeed/(denominator*denominator);
                var coordinateAcceleration=numeratorAcceleration/denominator+
                    2*numeratorSpeed*denominatorSpeed/(denominator*denominator)+
                    numerator*(2*denominatorSpeed*denominatorSpeed/(denominator*denominator*denominator)+
                        denominatorAcceleration/(denominator*denominator));
                pointSpeed=(pointA+pointB+sum*coordinate+2*coordinateSpeed+normalSpeed*radiusDifference)*.5;
                pointAcceleration=(pointAccelerationA+pointAccelerationB+(asa+asb+sa*sa+sb*sb)*coordinate+
                    2*sum*coordinateSpeed+2*coordinateAcceleration+normalAcceleration*radiusDifference)*.5;
                break;
            default: throw new InvalidOperationException("Undefined contact feature geometry.");
        }
        var geometry=Rebind(new Dictionary<PhysicsBodyId,PhysicsBody>{{_a.Id,a.SampleBody(start)},{_b.Id,b.SampleBody(start)}});
        var reachA=(geometry.Point-pa.Center).Length+(pointSpeed+va)*h;
        var reachB=(geometry.Point-pb.Center).Length+(pointSpeed+vb)*h;
        var wa=a.PhysicalAngularSpeedBound; var wb=b.PhysicalAngularSpeedBound;
        var aa=a.PhysicalAngularAccelerationBound; var ab=b.PhysicalAngularAccelerationBound;
        var velocity=relativeSpeed+wa*reachA+wb*reachB;
        var acceleration=relativeAcceleration+aa*reachA+ab*reachB+
            wa*(pointSpeed+va)+wb*(pointSpeed+vb);
        var curvature=a.LinearJerkBound+b.LinearJerkBound+a.PhysicalAngularCurvatureBound*reachA+b.PhysicalAngularCurvatureBound*reachB+
            2*aa*(pointSpeed+va)+2*ab*(pointSpeed+vb)+
            wa*(pointAcceleration+a.LinearAccelerationBound)+wb*(pointAcceleration+b.LinearAccelerationBound);
        // Slip=(I-n n^T)u. Projection has norm one; its first two
        // derivatives have norms <=2|n'| and <=2|n''|+2|n'|².
        var rateBound=acceleration+2*normalSpeed*velocity;
        var curvatureBound=curvature+4*normalSpeed*acceleration+
            (2*normalAcceleration+2*normalSpeed*normalSpeed)*velocity;
        if(!double.IsFinite(rateBound)||!double.IsFinite(curvatureBound)) return null;
        return new(Math.BitIncrement(rateBound*(1+1e-12)),Math.BitIncrement(curvatureBound*(1+1e-12)),
            Math.BitIncrement(normalSpeed*(1+1e-12)),Math.BitIncrement(normalAcceleration*(1+1e-12)));
    }

    private static Feature Describe(SupportFeature feature,double tolerance)
    {
        var vertices=feature.Vertices; var origin=vertices[0].Point; var edge=default(CollisionVector);
        foreach(var vertex in vertices)
            if((vertex.Point-origin).LengthSquared>edge.LengthSquared) edge=vertex.Point-origin;
        if(edge.Length<=tolerance) return new(FeatureRank.Point,default,default,0);
        var width=edge.Length;
        edge/=width;
        var normal=default(CollisionVector);
        foreach(var vertex in vertices)
        {
            var cross=CollisionVector.Cross(edge,vertex.Point-origin);
            if(cross.LengthSquared>normal.LengthSquared) normal=cross;
        }
        return normal.Length<=tolerance?new(FeatureRank.Edge,edge,default,width):
            new(FeatureRank.Face,edge,normal/normal.Length,width);
    }
}
