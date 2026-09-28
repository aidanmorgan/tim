using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>Exact two-coordinate minimization of the bounded impulse quadratic.
/// Every finite edge and the free stationary point are considered. This is a
/// regular block-coordinate sweep, not a retry after scalar convergence fails.</summary>
internal sealed class CoupledImpulsePair
{
    private readonly ImpulseConstraint _a,_b;
    private readonly double _aa,_ab,_bb,_ratio,_schur;
    public CoupledImpulsePair(ImpulseConstraint a,ImpulseConstraint b)
    {
        _a=a; _b=b; _aa=a.InverseEffectiveMass; _bb=b.InverseEffectiveMass;
        _ab=a.Gradient.Coupling(b.Gradient); _ratio=_ab/_aa;
        // Compute the Schur complement as a squared mass norm instead of
        // subtracting nearly equal diagonal products for parallel constraints.
        var terms=new List<ConstraintTerm>();
        foreach(var t in b.Gradient.Terms) terms.Add(t);
        foreach(var t in a.Gradient.Terms) terms.Add(new(t.Body,-t.Linear*_ratio,-t.Angular*_ratio));
        var remainder=new ConstraintGradient(terms.ToArray());
        _schur=remainder.Coupling(remainder)+b.Softness+_ratio*_ratio*a.Softness;
        if(!double.IsFinite(_ratio)||!double.IsFinite(_schur)||_schur<0)
            throw new InvalidOperationException("Coupled constraint mass exceeds numeric range.");
    }
    public void Solve()
    {
        _a.ValidatePose(); _b.ValidatePose();
        var ea=_a.Error; var eb=_b.Error;
        if(!double.IsFinite(ea)||!double.IsFinite(eb)) throw new InvalidOperationException("Coupled residual is not finite.");
        var lowA=_a.MinimumImpulse-_a.AccumulatedImpulse; var highA=_a.MaximumImpulse-_a.AccumulatedImpulse;
        var lowB=_b.MinimumImpulse-_b.AccumulatedImpulse; var highB=_b.MaximumImpulse-_b.AccumulatedImpulse;
        double bestX=0,bestY=0,best=0;
        void Consider(double x,double y)
        {
            if(!double.IsFinite(x)||!double.IsFinite(y)||x<lowA||x>highA||y<lowB||y>highB) return;
            // Positive-semidefinite completed square avoids cancellation in
            // the quadratic energy for nearly dependent rows.
            var along=x+_ratio*y;
            var objective=.5*(_aa*along*along+_schur*y*y)-ea*x-eb*y;
            if(!double.IsFinite(objective)) throw new InvalidOperationException("Coupled impulse energy exceeds numeric range.");
            if(objective<best) { best=objective; bestX=x; bestY=y; }
        }
        void EdgeA(double x) { if(double.IsFinite(x)) Consider(x,Math.Clamp((eb-_ab*x)/_bb,lowB,highB)); }
        void EdgeB(double y) { if(double.IsFinite(y)) Consider(Math.Clamp((ea-_ab*y)/_aa,lowA,highA),y); }
        EdgeA(lowA); EdgeA(highA); EdgeB(lowB); EdgeB(highB);
        // Coordinate stationary points also represent rank-deficient optima.
        EdgeA(0); EdgeB(0);
        if(_schur>0)
        {
            var y=(eb-_ratio*ea)/_schur;
            Consider((ea-_ab*y)/_aa,y);
        }
        var nextA=Math.Clamp(_a.AccumulatedImpulse+bestX,_a.MinimumImpulse,_a.MaximumImpulse);
        var nextB=Math.Clamp(_b.AccumulatedImpulse+bestY,_b.MinimumImpulse,_b.MaximumImpulse);
        if(!double.IsFinite(nextA)||!double.IsFinite(nextB)) throw new InvalidOperationException("Coupled impulse exceeds numeric range.");
        var terms=new List<ConstraintTerm>();
        foreach(var t in _a.Gradient.Terms) terms.Add(new(t.Body,t.Linear*(nextA-_a.AccumulatedImpulse),t.Angular*(nextA-_a.AccumulatedImpulse)));
        foreach(var t in _b.Gradient.Terms) terms.Add(new(t.Body,t.Linear*(nextB-_b.AccumulatedImpulse),t.Angular*(nextB-_b.AccumulatedImpulse)));
        // Combine shared participants and validate all updates before commit.
        new ConstraintGradient(terms.ToArray()).Apply(1);
        _a.CommitCoupledImpulse(nextA); _b.CommitCoupledImpulse(nextB);
    }
}
