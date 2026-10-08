using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public interface IPositionConstraint
{
    ReadOnlySpan<PhysicsBody> Bodies { get; }
    double Error(double queryTolerance);
    internal void Project(double tolerance,PositionProjector projector);
}

/// <summary>Nonlinear contact projection. Anchors are updated after each pose
/// change, and geometry is queried again on every outer iteration. Physical
/// velocities and world angular momenta are never used as pseudo velocities.</summary>
public sealed class ContactPositionConstraint : IPositionConstraint
{
    public PhysicsBody A { get; }
    public PhysicsBody B { get; }
    private readonly PhysicsBody[] _bodies;
    public ReadOnlySpan<PhysicsBody> Bodies=>_bodies;
    private readonly ConvexInstance _shapeA,_shapeB;
    public ContactPositionConstraint(PhysicsBody a,ConvexInstance shapeA,PhysicsBody b,ConvexInstance shapeB)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(a==b||a.Id==b.Id||shapeA.Geometry is null||shapeB.Geometry is null)
            throw new ArgumentException("Position contact requires distinct bodies and explicit geometry.");
        A=a; B=b; _bodies=[a,b]; _shapeA=shapeA; _shapeB=shapeB;
    }
    internal ConvexMotion MotionA(IRigidTrajectory path)=>new(_shapeA,path);
    internal ConvexMotion MotionB(IRigidTrajectory path)=>new(_shapeB,path);
    private ConvexPose ShapeA()=>new(_shapeA,A.Pose);
    private ConvexPose ShapeB()=>new(_shapeB,B.Pose);
    public double Error(double queryTolerance)=>Math.Max(0,-ConvexSeparation.Query(ShapeA(),ShapeB(),queryTolerance).LowerBound);

    void IPositionConstraint.Project(double tolerance,PositionProjector projector)=>Project(tolerance,projector);
    internal void Project(double tolerance,PositionProjector projector)
    {
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var manifold=ContactManifold.Query(ShapeA(),ShapeB(),0,tolerance*.125);
        var points=manifold.Points;
        var localA=new CollisionVector[points.Length]; var localB=new CollisionVector[points.Length];
        for(var i=0;i<points.Length;i++)
        {
            localA[i]=A.Pose.InverseTransformPoint(points[i].PointA);
            localB[i]=B.Pose.InverseTransformPoint(points[i].PointB);
        }
        var normal=manifold.Normal;
        for(var i=0;i<points.Length;i++)
        {
            var pointA=A.Pose.TransformPoint(localA[i]); var pointB=B.Pose.TransformPoint(localB[i]);
            var error=-CollisionVector.Dot(pointA-pointB,normal);
            if(error<=tolerance*.5) continue;
            var angularA=CollisionVector.Cross(pointA-A.Center,normal);
            var angularB=-CollisionVector.Cross(pointB-B.Center,normal);
            PositionEquations.Project([new(new([new(A,normal,angularA),new(B,-normal,angularB)]),-error)],projector);
        }
    }
}

public readonly record struct PositionSolveResult(int Iterations,double MaximumError);
public static class PositionSolver
{
    internal static PositionSolveResult Solve(Func<IEnumerable<IPositionConstraint>> constraints,PositionProjector projector,double tolerance,int maximumIterations=64)
    {
        ArgumentNullException.ThrowIfNull(constraints); ArgumentNullException.ThrowIfNull(projector);
        if(!double.IsFinite(tolerance)||tolerance<=0||maximumIterations<1) throw new ArgumentOutOfRangeException(nameof(tolerance));
        for(var iteration=0;iteration<=maximumIterations;iteration++)
        {
            var current=new List<IPositionConstraint>(constraints());
            var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();
            foreach(var constraint in current)
            {
                ArgumentNullException.ThrowIfNull(constraint);
                foreach(var body in constraint.Bodies)
                {
                    projector.ValidateBody(body);
                    if(bodies.TryGetValue(body.Id,out var prior)&&prior!=body) throw new ArgumentException("Duplicate body state for one identity.");
                    else bodies[body.Id]=body;
                }
            }
            double error=0;
            foreach(var constraint in current) error=Math.Max(error,constraint.Error(tolerance*.125));
            if(!double.IsFinite(error)) throw new InvalidOperationException("Position error is not finite.");
            if(error<=tolerance) return new(iteration,error);
            if(iteration==maximumIterations) break;
            foreach(var constraint in current) constraint.Project(tolerance,projector);
        }
        throw new InvalidOperationException("Position solve did not converge; penetration was not discarded.");
    }
}
