using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

/// <summary>Minimum of a positive-semidefinite tangent quadratic on a circular
/// impulse disk. Eigenvalues are mass norms of rotated gradients, avoiding
/// subtraction of nearly equal determinant products for constrained directions.</summary>
internal sealed class TangentQuadratic
{
    private readonly double _cos,_sin,_large,_small;
    public TangentQuadratic(ConstraintGradient u,ConstraintGradient v)
    {
        ArgumentNullException.ThrowIfNull(u);ArgumentNullException.ThrowIfNull(v);
        var a=u.Coupling(u);var b=u.Coupling(v);var c=v.Coupling(v);
        if(!double.IsFinite(a)||!double.IsFinite(b)||!double.IsFinite(c)||a<0||c<0)
            throw new ArgumentException("Tangent mass must be finite and nonnegative.");
        var scale=Math.Max(a,c);
        if(b==0){_cos=a>=c?1:0;_sin=a>=c?0:1;}
        else
        {
            var angle=.5*Math.Atan2(2*(b/scale),a/scale-c/scale);
            _cos=Math.Cos(angle);_sin=Math.Sin(angle);
        }
        ConstraintGradient Rotate(double x,double y)
        {
            var terms=new List<ConstraintTerm>();
            foreach(var t in u.Terms)terms.Add(new(t.Body,t.Linear*x,t.Angular*x));
            foreach(var t in v.Terms)terms.Add(new(t.Body,t.Linear*y,t.Angular*y));
            return new(terms.ToArray());
        }
        var rank=new ConstraintMassMatrix([u,v],[0,0]).Rank;
        var large=Rotate(_cos,_sin);var small=Rotate(-_sin,_cos);
        _large=rank>0?large.Coupling(large):0;
        _small=rank>1?small.Coupling(small):0;
        if(!double.IsFinite(_large)||!double.IsFinite(_small)||_large<0||_small<0)
            throw new ArgumentException("Tangent eigenvalues exceed numeric range.");
    }
    private static double Length(double x,double y)
    {
        var scale=Math.Max(Math.Abs(x),Math.Abs(y));
        if(scale==0)return 0;
        if(double.IsInfinity(scale))return double.PositiveInfinity;
        return scale*Math.Sqrt((x/scale)*(x/scale)+(y/scale)*(y/scale));
    }
    public (double X,double Y) Minimum(double rhs0,double rhs1,double radius)
    {
        if(!double.IsFinite(rhs0)||!double.IsFinite(rhs1)||!double.IsFinite(radius)||radius<0)
            throw new ArgumentException("Tangent load and disk radius must be finite; radius must be nonnegative.");
        if(radius==0)return(0,0);
        (double X,double Y) Finish(double x,double y)
        {
            var result=(X:_cos*x-_sin*y,Y:_sin*x+_cos*y);
            var length=Length(result.X,result.Y);
            if(!double.IsFinite(length))throw new InvalidOperationException("Tangent minimum exceeds numeric range.");
            // Preserve the disk after the final floating-point basis rotation.
            return length>radius?(result.X*(radius/length),result.Y*(radius/length)):result;
        }
        var g0=_cos*rhs0+_sin*rhs1;var g1=-_sin*rhs0+_cos*rhs1;
        if(!double.IsFinite(g0)||!double.IsFinite(g1))
            throw new InvalidOperationException("Rotated tangent load exceeds numeric range.");
        if(_large==0&&_small==0)
        {
            var scale=Math.Max(Math.Abs(g0),Math.Abs(g1));
            if(scale==0)return(0,0);
            var x=g0/scale;var y=g1/scale;var length=Length(x,y);
            return Finish(radius*(x/length),radius*(y/length));
        }
        var stationary=(_large>0||g0==0)&&(_small>0||g1==0);
        if(stationary)
        {
            var x=_large==0?0:g0/_large;var y=_small==0?0:g1/_small;
            if(Length(x,y)<=radius)return Finish(x,y);
        }
        var massScale=Math.Max(_large,_small);
        var a=_large/massScale;var c=_small/massScale;
        if((_large!=0&&a==0)||(_small!=0&&c==0))
            throw new InvalidOperationException("Tangent mass dynamic range is not representable.");
        var f0=g0/massScale;var f1=g1/massScale;
        if(!double.IsFinite(f0)||!double.IsFinite(f1)||(g0!=0&&f0==0)||(g1!=0&&f1==0))
            throw new InvalidOperationException("Scaled tangent load is not representable.");
        (double X,double Y) At(double lambda)=>(f0/(a+lambda),f1/(c+lambda));
        double low=0,high=Math.Max(1,Length(f0,f1)/radius);
        if(!double.IsFinite(high))throw new InvalidOperationException("Tangent multiplier exceeds numeric range.");
        var best=At(high);
        // 2100 halvings cover the finite binary64 exponent span, including subnormals.
        for(var i=0;i<2100;i++)
        {
            var mid=low+(high-low)/2;
            if(mid==low||mid==high)return Finish(best.X,best.Y);
            var trial=At(mid);
            if(Length(trial.X,trial.Y)>radius)low=mid;
            else{high=mid;best=trial;}
        }
        throw new InvalidOperationException("Tangent multiplier did not converge.");
    }
}

