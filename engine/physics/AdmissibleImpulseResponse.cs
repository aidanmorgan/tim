using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum ImpulseResponseRelation { Equal, Nonnegative, Nonpositive }

/// <summary>A homogeneous constraint on an incremental velocity response.
/// The caller selects active geometric limits; no physical drift is repaired here.</summary>
public sealed record ImpulseResponseConstraint
{
    public ConstraintGradient Gradient { get; }
    public ImpulseResponseRelation Relation { get; }
    public ImpulseResponseConstraint(ConstraintGradient gradient,ImpulseResponseRelation relation)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if(!Enum.IsDefined(relation)) throw new ArgumentOutOfRangeException(nameof(relation));
        Gradient=gradient; Relation=relation;
    }
}

/// <summary>Mass-metric projection of a directed unit impulse onto a velocity
/// cone. Bilateral spaces use a factored response map; inequality cones use
/// the shared coupled solver on scratch states. Live body
/// velocities and constraint accumulators are never mutated by this calculation.</summary>
public sealed class AdmissibleImpulseResponse
{
    private readonly ConstraintGradient _gradient;
    private readonly double[] _reactions;
    private readonly (PhysicsBody Body,ulong Revision)[] _poses;
    public AdmissibleImpulseResponse(ConstraintGradient applied,
        IReadOnlyList<ImpulseResponseConstraint> constraints,double tolerance)
    {
        ArgumentNullException.ThrowIfNull(applied);
        ArgumentNullException.ThrowIfNull(constraints);
        if(!double.IsFinite(tolerance)||tolerance<=0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var declarations=constraints.ToArray();
        foreach(var row in declarations) ArgumentNullException.ThrowIfNull(row);
        var bodies=new Dictionary<PhysicsBodyId,PhysicsBody>();
        foreach(var body in applied.Bodies.ToArray().Concat(declarations.SelectMany(row=>row.Gradient.Bodies.ToArray())))
        {
            if(bodies.TryGetValue(body.Id,out var previous)&&previous!=body)
                throw new ArgumentException("Body identity refers to multiple states.");
            bodies[body.Id]=body;
        }
        _poses=bodies.Values.Select(body=>(body,body.PoseRevision)).ToArray();
        if(declarations.All(row=>row.Relation==ImpulseResponseRelation.Equal))
        {
            var map=new BilateralResponseMap(declarations.Select(row=>row.Gradient).ToArray());
            var projected=map.Project(applied,tolerance);
            _gradient=projected.Gradient;_reactions=projected.Reactions;
            return;
        }
        var scratch=bodies.Values.ToDictionary(body=>body.Id,body=>new PhysicsBody(
            body.Id,body.MotionType,body.Pose,default,default,
            body.MotionType==PhysicsMotionType.Dynamic?1/body.InverseMass:0,body.LocalInertia));
        ConstraintGradient Rebind(ConstraintGradient source)=>new(source.Terms.ToArray().Select(term=>
            new ConstraintTerm(scratch[term.Body.Id],term.Linear,term.Angular)).ToArray());
        var rows=declarations.Select(row=>new ImpulseConstraint(Rebind(row.Gradient),0,
            row.Relation==ImpulseResponseRelation.Nonnegative?0:double.NegativeInfinity,
            row.Relation==ImpulseResponseRelation.Nonpositive?0:double.PositiveInfinity)).ToArray();
        Rebind(applied).Apply(1);
        var equalities=rows.Where((_,i)=>declarations[i].Relation==ImpulseResponseRelation.Equal).ToArray();
        var solve=rows.Where((_,i)=>declarations[i].Relation!=ImpulseResponseRelation.Equal)
            .Cast<IImpulseConstraint>().ToList();
        if(equalities.Length>0) solve.Add(new BilateralConstraintBlock(equalities));
        ImpulseSolver.Solve(solve,tolerance:tolerance);
        _reactions=rows.Select(row=>row.AccumulatedImpulse).ToArray();
        var terms=new List<ConstraintTerm>(applied.Terms.ToArray());
        for(var i=0;i<declarations.Length;i++)
            foreach(var term in declarations[i].Gradient.Terms)
                terms.Add(new(term.Body,term.Linear*_reactions[i],term.Angular*_reactions[i]));
        _gradient=new(terms.ToArray());
    }
    private void ValidatePose()
    {
        foreach(var (body,revision) in _poses)
            if(body.PoseRevision!=revision)
                throw new InvalidOperationException("Impulse response is stale after a participant pose change.");
    }
    public ConstraintGradient Gradient
    {
        get { ValidatePose(); return _gradient; }
    }
    public ReadOnlySpan<double> ReactionsPerUnitImpulse
    {
        get { ValidatePose(); return _reactions; }
    }
}
