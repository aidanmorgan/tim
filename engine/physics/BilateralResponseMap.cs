using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Immutable mass-metric map for homogeneous bilateral responses.
/// Factor topology once, then project multiple applied directions. No body or
/// impulse accumulator is changed. Rebuild after any participant pose changes.</summary>
internal sealed class BilateralResponseMap
{
    private readonly ConstraintGradient[] _rows;
    private readonly ConstraintMassMatrix? _mass;
    private readonly Dictionary<PhysicsBodyId,PhysicsBody> _bodies=new();
    private readonly (PhysicsBody Body,ulong Revision)[] _poses;

    public BilateralResponseMap(IReadOnlyList<ConstraintGradient> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _rows=rows.ToArray();
        foreach(var row in _rows)
        {
            ArgumentNullException.ThrowIfNull(row);
            foreach(var body in row.Bodies)
            {
                if(_bodies.TryGetValue(body.Id,out var prior)&&prior!=body)
                    throw new ArgumentException("Body identity refers to multiple response states.");
                _bodies[body.Id]=body;
            }
        }
        _poses=_bodies.Values.Select(body=>(body,body.PoseRevision)).ToArray();
        if(_rows.Length>0)_mass=new(_rows,new double[_rows.Length]);
    }

    /// <summary>Whether the applied coordinate adds a resolved direction to the
    /// existing bilateral mass block, using that solver's scaled rank policy.
    /// A dependent direction has no separately defined conjugate response.</summary>
    public bool Constrains(ConstraintGradient applied)
    {
        ArgumentNullException.ThrowIfNull(applied);
        foreach(var (body,revision) in _poses)
            if(body.PoseRevision!=revision)
                throw new InvalidOperationException("Bilateral response map is stale after a participant pose change.");
        foreach(var body in applied.Bodies)
            if(_bodies.TryGetValue(body.Id,out var prior)&&prior!=body)
                throw new ArgumentException("Applied response identity refers to a different body.");
        var augmented=new ConstraintMassMatrix([.._rows,applied],new double[_rows.Length+1]);
        return augmented.Rank==(_mass?.Rank??0);
    }

    public (ConstraintGradient Gradient,double[] Reactions) Project(ConstraintGradient applied,double tolerance)
    {
        ArgumentNullException.ThrowIfNull(applied);
        if(!double.IsFinite(tolerance)||tolerance<=0)throw new ArgumentOutOfRangeException(nameof(tolerance));
        foreach(var (body,revision) in _poses)
            if(body.PoseRevision!=revision)
                throw new InvalidOperationException("Bilateral response map is stale after a participant pose change.");
        foreach(var body in applied.Bodies)
            if(_bodies.TryGetValue(body.Id,out var prior)&&prior!=body)
                throw new ArgumentException("Applied response identity refers to a different body.");
        if(_mass is null)return(applied,[]);
        var rhs=new double[_rows.Length];
        var reactions=new double[_rows.Length];
        for(var i=0;i<_rows.Length;i++)rhs[i]=-_rows[i].Coupling(applied);
        _mass.Solve(rhs,reactions);
        var terms=new List<ConstraintTerm>(applied.Terms.ToArray());
        for(var i=0;i<_rows.Length;i++)
            foreach(var term in _rows[i].Terms)
                terms.Add(new(term.Body,term.Linear*reactions[i],term.Angular*reactions[i]));
        var projected=new ConstraintGradient(terms.ToArray());
        foreach(var row in _rows)
        {
            var residual=row.Coupling(projected);
            if(!double.IsFinite(residual)||Math.Abs(residual)>tolerance)
                throw new InvalidOperationException("Bilateral response projection exceeds its residual budget.");
        }
        return(projected,reactions);
    }
}

