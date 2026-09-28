using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public interface IPositionConstraint
{
    PhysicsBody A { get; }
    PhysicsBody B { get; }
    double Error(double queryTolerance);
    void Project(double tolerance);
}

/// <summary>Nonlinear contact projection. Anchors are updated after each pose
/// change, and geometry is queried again on every outer iteration. Physical
/// velocities and world angular momenta are never used as pseudo velocities.</summary>
public sealed class ContactPositionConstraint : IPositionConstraint
{
    public PhysicsBody A { get; }
    public PhysicsBody B { get; }
    private readonly ConvexInstance _shapeA,_shapeB;
    public ContactPositionConstraint(PhysicsBody a,ConvexInstance shapeA,PhysicsBody b,ConvexInstance shapeB)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(a==b||a.Id==b.Id||shapeA.Geometry is null||shapeB.Geometry is null)
            throw new ArgumentException("Position contact requires distinct bodies and explicit geometry.");
        A=a; B=b; _shapeA=shapeA; _shapeB=shapeB;
    }
    private ConvexMotion.AtTime ShapeA()=>new ConvexMotion(_shapeA,A.CreateTrajectory(0)).At(0);
    private ConvexMotion.AtTime ShapeB()=>new ConvexMotion(_shapeB,B.CreateTrajectory(0)).At(0);
    public double Error(double queryTolerance)=>Math.Max(0,-ConvexSeparation.Query(ShapeA(),ShapeB(),queryTolerance).LowerBound);

    public void Project(double tolerance)
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
            var turnA=A.InverseInertia(angularA); var turnB=B.InverseInertia(angularB);
            var mass=A.InverseMass+B.InverseMass+CollisionVector.Dot(angularA,turnA)+CollisionVector.Dot(angularB,turnB);
            if(!double.IsFinite(mass)||mass<=0) throw new InvalidOperationException("Immovable contact cannot resolve penetration.");
            var impulse=error/mass;
            if(!double.IsFinite(impulse)) throw new InvalidOperationException("Position impulse exceeds numeric range.");
            if(A.MotionType==PhysicsMotionType.Dynamic) A.CorrectPose(normal*(A.InverseMass*impulse),turnA*impulse);
            if(B.MotionType==PhysicsMotionType.Dynamic) B.CorrectPose(-normal*(B.InverseMass*impulse),turnB*impulse);
        }
    }
}

public readonly record struct PositionSolveResult(int Iterations,double MaximumError);
public static class PositionSolver
{
    public static PositionSolveResult Solve(IReadOnlyList<IPositionConstraint> constraints,double tolerance,int maximumIterations=64)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        if(!double.IsFinite(tolerance)||tolerance<=0||maximumIterations<1) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();
        foreach(var constraint in constraints)
        {
            ArgumentNullException.ThrowIfNull(constraint);
            foreach(var body in new[]{constraint.A,constraint.B})
                if(bodies.TryGetValue(body.Id,out var prior)&&prior!=body) throw new ArgumentException("Duplicate body state for one identity.");
                else bodies[body.Id]=body;
        }
        for(var iteration=0;iteration<=maximumIterations;iteration++)
        {
            double error=0;
            foreach(var constraint in constraints) error=Math.Max(error,constraint.Error(tolerance*.125));
            if(!double.IsFinite(error)) throw new InvalidOperationException("Position error is not finite.");
            if(error<=tolerance) return new(iteration,error);
            if(iteration==maximumIterations) break;
            foreach(var constraint in constraints) constraint.Project(tolerance);
        }
        throw new InvalidOperationException("Position solve did not converge; penetration was not discarded.");
    }
}
