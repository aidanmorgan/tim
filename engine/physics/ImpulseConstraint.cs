using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public readonly record struct ConstraintJacobian(CollisionVector LinearA,CollisionVector AngularA,
    CollisionVector LinearB,CollisionVector AngularB)
{
    public bool IsFinite=>LinearA.IsFinite&&AngularA.IsFinite&&LinearB.IsFinite&&AngularB.IsFinite;
    public ConstraintGradient Bind(PhysicsBody a,PhysicsBody b)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if(a==b||a.Id==b.Id) throw new ArgumentException("Pair geometry requires distinct body identities.");
        return new([new(a,LinearA,AngularA),new(b,LinearB,AngularB)]);
    }
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
    ReadOnlySpan<PhysicsBody> Bodies { get; }
    IEnumerable<ImpulseConstraint> ScalarRows { get; }
    double Residual { get; }
    internal void Solve();
}

public sealed class ImpulseConstraint : IImpulseConstraint
{
    public ReadOnlySpan<PhysicsBody> Bodies=>Gradient.Bodies;
    public IEnumerable<ImpulseConstraint> ScalarRows { get { yield return this; } }
    public ConstraintGradient Gradient { get; }
    public double TargetSpeed { get; }
    public double MinimumImpulse { get; }
    public double MaximumImpulse { get; }
    public double Softness { get; }
    public double AccumulatedImpulse { get; private set; }
    public double InverseEffectiveMass { get; }
    private readonly ulong[] _revisions;
    private bool _started;

    public ImpulseConstraint(ConstraintGradient gradient,
        double targetSpeed,double minimumImpulse,double maximumImpulse,double softness=0)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if(!double.IsFinite(targetSpeed)||!double.IsFinite(softness)||softness<0||
            double.IsNaN(minimumImpulse)||double.IsNaN(maximumImpulse)||minimumImpulse>0||maximumImpulse<0)
            throw new ArgumentException("Invalid constraint declaration; impulse interval must include zero.");
        Gradient=gradient; TargetSpeed=targetSpeed;
        _revisions=new ulong[Bodies.Length];
        for(var i=0;i<Bodies.Length;i++) _revisions[i]=Bodies[i].PoseRevision;
        MinimumImpulse=minimumImpulse; MaximumImpulse=maximumImpulse; Softness=softness;
        InverseEffectiveMass=gradient.Coupling(gradient)+softness;
        if(!double.IsFinite(InverseEffectiveMass)||InverseEffectiveMass<0)
            throw new ArgumentException("Constraint mass is not representable.");
    }
    internal void ValidatePose()
    {
        for(var i=0;i<Bodies.Length;i++)
            if(Bodies[i].PoseRevision!=_revisions[i])
                throw new InvalidOperationException("Constraint geometry is stale after a body pose change.");
    }
    public double Speed
    {
        get
        {
            ValidatePose();
            return Gradient.Speed;
        }
    }
    internal double Error=>TargetSpeed-Speed-Softness*AccumulatedImpulse;
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
    /// <summary>One signed bound natural map and its projected reaction.
    /// Force balance and physical acceptance use this same projection.</summary>
    internal (double Residual,double Reaction) EvaluateReaction(double impulse)
    {
        ValidatePose();
        if(!double.IsFinite(impulse))throw new ArgumentOutOfRangeException(nameof(impulse));
        var scale=InverseEffectiveMass>0?InverseEffectiveMass:1;
        var error=TargetSpeed-Speed-Softness*impulse;
        var projected=Math.Clamp(impulse+error/scale,MinimumImpulse,MaximumImpulse);
        var residual=(impulse-projected)*scale;
        if(!double.IsFinite(residual)||!double.IsFinite(projected))
            throw new InvalidOperationException("Constraint natural map exceeds numeric range.");
        return(residual,projected);
    }

    /// <summary>Signed natural map on the current feasible reaction coordinates.
    /// Interior equations use their physical error directly, avoiding subtraction
    /// of two almost equal impulses. Exact bound ties select the bound branch.</summary>
    internal double LinearizeReaction(double[,] mass,double[,] jacobian,int index)
    {
        ValidatePose();
        var error=Error;
        if(!double.IsFinite(error))throw new InvalidOperationException("Constraint residual is not finite.");
        var scale=InverseEffectiveMass>0?InverseEffectiveMass:1;
        var increment=error/scale;
        var lower=MinimumImpulse-AccumulatedImpulse;
        var upper=MaximumImpulse-AccumulatedImpulse;
        // Comparing the increment with bound distances preserves the branch
        // when adding a small inward increment would round back to the bound.
        if(increment<=lower||increment>=upper)
        {
            var bound=increment<=lower?MinimumImpulse:MaximumImpulse;
            jacobian[index,index]=scale;
            return (AccumulatedImpulse-bound)*scale;
        }
        for(var column=0;column<mass.GetLength(1);column++)
            jacobian[index,column]=mass[index,column]+(column==index?Softness:0);
        return -error;
    }

    internal void InitializeAccumulatedImpulse(double impulse)
    {
        ValidatePose();
        if(_started) throw new InvalidOperationException("A constraint can only be warm started before its first solve.");
        if(!double.IsFinite(impulse)||impulse<MinimumImpulse||impulse>MaximumImpulse)
            throw new ArgumentOutOfRangeException(nameof(impulse));
        AccumulatedImpulse=impulse; _started=true;
    }
    void IImpulseConstraint.Solve()=>Solve();
    internal void Solve()
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
        Gradient.Apply(increment); AccumulatedImpulse=next;
    }
    internal void CommitCoupledImpulse(double impulse)
    {
        AccumulatedImpulse=impulse; _started=true;
    }
    public static ImpulseConstraint Contact(PhysicsBody a,PhysicsBody b,CollisionVector point,
        CollisionVector normalFromBToA,double restitution,double bounceThreshold)
    {
        if(!double.IsFinite(restitution)||restitution<0||restitution>1||
            !double.IsFinite(bounceThreshold)||bounceThreshold<0) throw new ArgumentOutOfRangeException(nameof(restitution));
        var jacobian=ConstraintJacobian.AtPoint(a,b,point,normalFromBToA);
        var incoming=CollisionVector.Dot(a.PointVelocity(point)-b.PointVelocity(point),normalFromBToA);
        var target=incoming < -bounceThreshold?-restitution*incoming:0;
        return new(jacobian.Bind(a,b),target,0,double.PositiveInfinity);
    }
}

public readonly record struct ImpulseSolveResult(int Iterations,double MaximumResidual,int CouplingTests,int CoupledPairs,ImpulseCorrectionWork Corrections);
public static class ImpulseSolver
{
    internal static ImpulseSolveResult Solve(IReadOnlyList<IImpulseConstraint> constraints,int maximumIterations=256,double tolerance=1e-8)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        if(maximumIterations<1||!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        // Distinct references may not masquerade as one body under the same identity.
        var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();
        foreach(var row in constraints)
        {
            ArgumentNullException.ThrowIfNull(row);
            foreach(var body in row.Bodies)
                if(bodies.TryGetValue(body.Id,out var prior)&&prior!=body)
                    throw new ArgumentException("Body identity refers to multiple velocity states.");
                else bodies[body.Id]=body;
        }
        var scalarRows=new List<ImpulseConstraint>();
        var uniqueRows=new HashSet<ImpulseConstraint>();
        foreach(var constraint in constraints)
            foreach(var row in constraint.ScalarRows)
                if(uniqueRows.Add(row)) scalarRows.Add(row);
        // Rows can exchange an impulse only through a shared dynamic body.
        // A common immovable floor does not connect otherwise independent loads.
        var incidence=new Dictionary<PhysicsBodyId,List<int>>();
        for(var i=0;i<scalarRows.Count;i++)
        {
            if(scalarRows[i].InverseEffectiveMass==0) continue;
            foreach(var body in scalarRows[i].Bodies)
            {
                if(body.MotionType!=PhysicsMotionType.Dynamic) continue;
                if(!incidence.TryGetValue(body.Id,out var rows)) incidence.Add(body.Id,rows=new());
                rows.Add(i);
            }
        }
        var candidates=new HashSet<(int A,int B)>();
        foreach(var rows in incidence.Values)
            for(var i=0;i<rows.Count;i++)
            for(var j=i+1;j<rows.Count;j++) candidates.Add((rows[i],rows[j]));
        var frictionNormals=constraints.OfType<ContactConstraint>()
            .Where(contact=>contact.Friction>0).Select(contact=>contact.Normal).ToHashSet();
        var pairs=new List<CoupledImpulsePair>();
        foreach(var (a,b) in candidates.OrderBy(p=>p.A).ThenBy(p=>p.B))
            if(!(frictionNormals.Contains(scalarRows[a])&&frictionNormals.Contains(scalarRows[b]))&&
                scalarRows[a].Gradient.Coupling(scalarRows[b].Gradient)!=0)
                pairs.Add(new(scalarRows[a],scalarRows[b]));
        // One friction contact can also converge slowly through a transmission
        // or guide. Tangent rows are not in ScalarRows, so the scalar-pair pass
        // cannot accelerate that coupling. Detect it from the shared mass map.
        var coupledFriction=frictionNormals.Count>1||constraints.OfType<ContactConstraint>()
            .Where(contact=>contact.Friction>0).Any(contact=>scalarRows.Any(row=>row!=contact.Normal&&
                (contact.TangentGradients.U.Coupling(row.Gradient)!=0||
                 contact.TangentGradients.V.Coupling(row.Gradient)!=0)));
        var acceleration=coupledFriction?new ImpulseSweepAcceleration(constraints,scalarRows):null;
        ImpulseCorrectionWork CorrectionWork()=>acceleration is null?default:new(acceleration.Factorizations,acceleration.TrialEvaluations,acceleration.MaximumCoordinates);
        double previousResidual=double.PositiveInfinity,lastResidual=double.PositiveInfinity;
        for(var iteration=1;iteration<=maximumIterations;iteration++)
        {
            foreach(var pair in pairs) pair.Solve();
            foreach(var row in constraints)
                if(row is not BilateralConstraintBlock) row.Solve();
            // Finish each sweep on the bilateral manifold. Contact residuals
            // are checked afterward, so projection cannot hide an unresolved contact.
            foreach(var row in constraints)
                if(row is BilateralConstraintBlock) row.Solve();
            double residual=0;
            foreach(var row in constraints) residual=Math.Max(residual,row.Residual);
            if(residual<=tolerance) return new(iteration,residual,candidates.Count,pairs.Count,CorrectionWork());
            if(acceleration is not null) residual=acceleration.Complete(residual);
            if(residual<=tolerance) return new(iteration,residual,candidates.Count,pairs.Count,CorrectionWork());
            previousResidual=lastResidual;lastResidual=residual;
        }
        double remaining=0;
        foreach(var row in constraints) remaining=Math.Max(remaining,row.Residual);
        var details=string.Join("; ",constraints.Select((row,index)=>
            $"row {index}: {row.GetType().Name}, residual {row.Residual:R}, scalar residuals [{string.Join(", ",row.ScalarRows.Select(scalar=>$"{scalar.Residual:R}"))}]"));
        details+=string.Join("; ",constraints.OfType<ContactConstraint>().Select(contact=>contact.Diagnostic));
        throw new InvalidOperationException($"Constraint solve did not converge across {constraints.Count} rows; residual {remaining:R} exceeds {tolerance:R}; previous residual {previousResidual:R}; {details}.");
    }
}
