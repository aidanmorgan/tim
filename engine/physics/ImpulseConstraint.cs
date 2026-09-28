using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public readonly record struct ConstraintJacobian(CollisionVector LinearA,CollisionVector AngularA,
    CollisionVector LinearB,CollisionVector AngularB)
{
    public bool IsFinite=>LinearA.IsFinite&&AngularA.IsFinite&&LinearB.IsFinite&&AngularB.IsFinite;
    public static ConstraintJacobian AtPoint(PhysicsBody a,PhysicsBody b,CollisionVector point,CollisionVector direction)
    {
        if(!point.IsFinite||!direction.IsFinite||Math.Abs(direction.LengthSquared-1)>1e-10)
            throw new ArgumentException("Contact direction must be unit length and point finite.");
        return new(direction,CollisionVector.Cross(point-a.Center,direction),
            -direction,-CollisionVector.Cross(point-b.Center,direction));
    }
}

/// <summary>One bounded Jacobian row, shared by contacts, joint locks, limits and
/// motors. Clamp the accumulated impulse, not the incremental correction.</summary>
public interface IImpulseConstraint
{
    PhysicsBody A { get; }
    PhysicsBody B { get; }
    double Residual { get; }
    void Solve();
}

public sealed class ImpulseConstraint : IImpulseConstraint
{
    public PhysicsBody A { get; }
    public PhysicsBody B { get; }
    public ConstraintJacobian Jacobian { get; }
    public double TargetSpeed { get; }
    public double MinimumImpulse { get; }
    public double MaximumImpulse { get; }
    public double Softness { get; }
    public double AccumulatedImpulse { get; private set; }
    public double InverseEffectiveMass { get; }
    private readonly ulong _revisionA,_revisionB;
    private bool _started;

    public ImpulseConstraint(PhysicsBody a,PhysicsBody b,ConstraintJacobian jacobian,
        double targetSpeed,double minimumImpulse,double maximumImpulse,double softness=0)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(a==b||a.Id==b.Id) throw new ArgumentException("Constraint requires two distinct body identities.");
        if(!jacobian.IsFinite||!double.IsFinite(targetSpeed)||!double.IsFinite(softness)||softness<0||
            double.IsNaN(minimumImpulse)||double.IsNaN(maximumImpulse)||minimumImpulse>0||maximumImpulse<0)
            throw new ArgumentException("Invalid constraint declaration; impulse interval must include zero.");
        A=a; B=b; _revisionA=a.PoseRevision; _revisionB=b.PoseRevision; Jacobian=jacobian; TargetSpeed=targetSpeed;
        MinimumImpulse=minimumImpulse; MaximumImpulse=maximumImpulse; Softness=softness;
        InverseEffectiveMass=a.InverseMass*jacobian.LinearA.LengthSquared+
            b.InverseMass*jacobian.LinearB.LengthSquared+
            CollisionVector.Dot(jacobian.AngularA,a.InverseInertia(jacobian.AngularA))+
            CollisionVector.Dot(jacobian.AngularB,b.InverseInertia(jacobian.AngularB))+softness;
        if(!double.IsFinite(InverseEffectiveMass)||InverseEffectiveMass<0)
            throw new ArgumentException("Constraint mass is not representable.");
    }
    internal void ValidatePose()
    {
        if(A.PoseRevision!=_revisionA||B.PoseRevision!=_revisionB)
            throw new InvalidOperationException("Constraint geometry is stale after a body pose change.");
    }
    public double Speed
    {
        get
        {
            ValidatePose();
            return CollisionVector.Dot(Jacobian.LinearA,A.LinearVelocity)+
        CollisionVector.Dot(Jacobian.AngularA,A.AngularVelocity)+
        CollisionVector.Dot(Jacobian.LinearB,B.LinearVelocity)+CollisionVector.Dot(Jacobian.AngularB,B.AngularVelocity);
        }
    }
    private double Error=>TargetSpeed-Speed-Softness*AccumulatedImpulse;
    public double Residual
    {
        get
        {
            var error=Error;
            if(!double.IsFinite(error)) throw new InvalidOperationException("Constraint residual is not finite.");
            if(AccumulatedImpulse==MinimumImpulse&&error<0||AccumulatedImpulse==MaximumImpulse&&error>0) return 0;
            return Math.Abs(error);
        }
    }
    internal void InitializeAccumulatedImpulse(double impulse)
    {
        ValidatePose();
        if(_started) throw new InvalidOperationException("A constraint can only be warm started before its first solve.");
        if(!double.IsFinite(impulse)||impulse<MinimumImpulse||impulse>MaximumImpulse)
            throw new ArgumentOutOfRangeException(nameof(impulse));
        AccumulatedImpulse=impulse; _started=true;
    }
    public void Solve()
    {
        ValidatePose(); _started=true;
        if(InverseEffectiveMass==0)
        {
            if(Residual>0) throw new InvalidOperationException("An immovable constraint cannot meet its target.");
            return;
        }
        var error=Error;
        if(!double.IsFinite(error)) throw new InvalidOperationException("Constraint residual is not finite.");
        var next=Math.Clamp(AccumulatedImpulse+error/InverseEffectiveMass,MinimumImpulse,MaximumImpulse);
        var increment=next-AccumulatedImpulse;
        // Validate both results before mutation; a numerical failure is not a partial impulse.
        var va=A.AfterImpulse(Jacobian.LinearA*increment,Jacobian.AngularA*increment);
        var vb=B.AfterImpulse(Jacobian.LinearB*increment,Jacobian.AngularB*increment);
        A.CommitVelocity(va); B.CommitVelocity(vb); AccumulatedImpulse=next;
    }
    public static ImpulseConstraint Contact(PhysicsBody a,PhysicsBody b,CollisionVector point,
        CollisionVector normalFromBToA,double restitution,double bounceThreshold)
    {
        if(!double.IsFinite(restitution)||restitution<0||restitution>1||
            !double.IsFinite(bounceThreshold)||bounceThreshold<0) throw new ArgumentOutOfRangeException(nameof(restitution));
        var jacobian=ConstraintJacobian.AtPoint(a,b,point,normalFromBToA);
        var incoming=CollisionVector.Dot(a.PointVelocity(point)-b.PointVelocity(point),normalFromBToA);
        var target=incoming < -bounceThreshold?-restitution*incoming:0;
        return new(a,b,jacobian,target,0,double.PositiveInfinity);
    }
}

public readonly record struct ImpulseSolveResult(int Iterations,double MaximumResidual);
public static class ImpulseSolver
{
    public static ImpulseSolveResult Solve(IReadOnlyList<IImpulseConstraint> constraints,int maximumIterations=256,double tolerance=1e-8)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        if(maximumIterations<1||!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        // Distinct references may not masquerade as one body under the same identity.
        var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();
        foreach(var row in constraints)
        {
            ArgumentNullException.ThrowIfNull(row);
            foreach(var body in new[]{row.A,row.B})
                if(bodies.TryGetValue(body.Id,out var prior)&&prior!=body)
                    throw new ArgumentException("Body identity refers to multiple velocity states.");
                else bodies[body.Id]=body;
        }
        for(var iteration=1;iteration<=maximumIterations;iteration++)
        {
            foreach(var row in constraints) row.Solve();
            double residual=0;
            foreach(var row in constraints) residual=Math.Max(residual,row.Residual);
            if(residual<=tolerance) return new(iteration,residual);
        }
        double remaining=0;
        foreach(var row in constraints) remaining=Math.Max(remaining,row.Residual);
        throw new InvalidOperationException($"Constraint solve did not converge across {constraints.Count} rows; residual {remaining:R} exceeds {tolerance:R}.");
    }
}
