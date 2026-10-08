using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public readonly record struct ConstraintTerm(PhysicsBody Body,CollisionVector Linear,CollisionVector Angular);

/// <summary>Immutable sparse Jacobian over any number of rigid bodies.
/// Contributions for multiple attachments on one body are summed before mass
/// evaluation; they are not independent degrees of freedom.</summary>
public sealed class ConstraintGradient
{
    private readonly ConstraintTerm[] _terms;
    private readonly PhysicsBody[] _bodies;
    public ReadOnlySpan<ConstraintTerm> Terms=>_terms;
    public ReadOnlySpan<PhysicsBody> Bodies=>_bodies;
    public ConstraintGradient(ReadOnlySpan<ConstraintTerm> terms)
    {
        if(terms.Length==0) throw new ArgumentException("A constraint needs at least one body.");
        var summed=new Dictionary<PhysicsBodyId,ConstraintTerm>();
        foreach(var term in terms)
        {
            ArgumentNullException.ThrowIfNull(term.Body);
            if(!term.Linear.IsFinite||!term.Angular.IsFinite) throw new ArgumentException("Constraint derivatives must be finite.");
            if(summed.TryGetValue(term.Body.Id,out var prior))
            {
                if(prior.Body!=term.Body) throw new ArgumentException("Body identity refers to multiple states.");
                var total=new ConstraintTerm(term.Body,prior.Linear+term.Linear,prior.Angular+term.Angular);
                if(!total.Linear.IsFinite||!total.Angular.IsFinite) throw new ArgumentException("Summed derivative exceeds numeric range.");
                summed[term.Body.Id]=total;
            }
            else summed.Add(term.Body.Id,term);
        }
        _terms=summed.Values.OrderBy(t=>t.Body.Id.Index).ToArray();
        _bodies=_terms.Select(t=>t.Body).ToArray();
    }
    public double Speed
    {
        get
        {
            double speed=0;
            foreach(var term in _terms)
                speed+=CollisionVector.Dot(term.Linear,term.Body.LinearVelocity)+CollisionVector.Dot(term.Angular,term.Body.AngularVelocity);
            return speed;
        }
    }
    /// <summary>Physical velocity along captured paths, with complete products and
    /// participant cancellation retained until the final scalar read.</summary>
    public double SpeedAlong(IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,double time)
    {
        ArgumentNullException.ThrowIfNull(paths);
        Span<ulong> storage=stackalloc ulong[BinaryProductSum.StorageLength];
        var sum=new BinaryProductSum(storage);
        foreach(var term in _terms)
        {
            if(!paths.TryGetValue(term.Body.Id,out var path)||path is null||path.SourceBody!=term.Body.Id)
                throw new ArgumentException("Constraint path is missing a participant.",nameof(paths));
            path.AccumulatePhysicalVelocity(ref sum,term.Linear,term.Angular,time);
        }
        return sum.Finish();
    }
    public double Coupling(ConstraintGradient other)
    {
        ArgumentNullException.ThrowIfNull(other);
        double mass=0; var i=0; var j=0;
        while(i<_terms.Length&&j<other._terms.Length)
        {
            var a=_terms[i]; var b=other._terms[j];
            if(a.Body.Id.Index<b.Body.Id.Index) { i++; continue; }
            if(a.Body.Id.Index>b.Body.Id.Index) { j++; continue; }
            if(a.Body!=b.Body) throw new ArgumentException("Body identity refers to multiple states.");
            mass+=a.Body.InverseMass*CollisionVector.Dot(a.Linear,b.Linear)+
                CollisionVector.Dot(a.Angular,a.Body.InverseInertia(b.Angular));
            i++; j++;
        }
        return mass;
    }
    internal void Apply(double impulse)
    {
        var updates=new BodyVelocityUpdate[_terms.Length];
        for(var i=0;i<_terms.Length;i++)
            updates[i]=_terms[i].Body.AfterImpulse(_terms[i].Linear*impulse,_terms[i].Angular*impulse);
        // Validate every result before committing any participant.
        for(var i=0;i<_terms.Length;i++) _terms[i].Body.CommitVelocity(updates[i]);
    }
}
