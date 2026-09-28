using System;

namespace CuriousContraptions.Physics;

/// <summary>A nonnegative normal impulse and a tangent vector in the caller's
/// declared coordinate frame. Warm starting projects it onto the current cone.</summary>
public readonly record struct ContactImpulse
{
    public double Normal { get; }
    public CollisionVector Tangent { get; }
    public ContactImpulse(double normal,CollisionVector tangent)
    {
        if(!double.IsFinite(normal)||normal<0||!tangent.IsFinite) throw new ArgumentException("Contact impulse must be finite with a nonnegative normal component.");
        Normal=normal; Tangent=tangent;
    }
}

/// <summary>Normal response plus maximum-dissipation friction over a circular
/// Coulomb disk. The full 2x2 tangent effective mass includes angular coupling.
/// No independent axis clamps (which would create a square friction cone).</summary>
public sealed class ContactConstraint : IImpulseConstraint
{
    public ImpulseConstraint Normal { get; }
    public PhysicsBody A=>Normal.A;
    public PhysicsBody B=>Normal.B;
    public CollisionVector TangentImpulse=>_u*(_j0)+_v*(_j1);
    public double Friction { get; }
    public ContactImpulse Impulse=>new(Normal.AccumulatedImpulse,TangentImpulse);
    private readonly CollisionVector _u,_v;
    private readonly ConstraintJacobian _ju,_jv;
    private readonly double _k00,_k01,_k11,_scale;
    private double _j0,_j1;

    public ContactConstraint(PhysicsBody a,PhysicsBody b,CollisionVector point,CollisionVector normal,
        double restitution,double bounceThreshold,double friction)
    {
        if(!double.IsFinite(friction)||friction<0) throw new ArgumentOutOfRangeException(nameof(friction));
        Normal=ImpulseConstraint.Contact(a,b,point,normal,restitution,bounceThreshold);
        Friction=friction;
        var seed=Math.Abs(normal.X)<.5773502691896258?new CollisionVector(1,0,0):
            Math.Abs(normal.Y)<.5773502691896258?new CollisionVector(0,1,0):new CollisionVector(0,0,1);
        var tangent=CollisionVector.Cross(normal,seed);
        _u=tangent/tangent.Length; _v=CollisionVector.Cross(normal,_u);
        _ju=ConstraintJacobian.AtPoint(a,b,point,_u);
        _jv=ConstraintJacobian.AtPoint(a,b,point,_v);
        _k00=Mass(_ju,_ju); _k01=Mass(_ju,_jv); _k11=Mass(_jv,_jv);
        _scale=_k00+_k11;
        if(!double.IsFinite(_scale)||!double.IsFinite(_k01)||
            (_scale>0&&(_k00<=0||_k11<=0||_k00*_k11-_k01*_k01<=0)))
            throw new ArgumentException("Contact tangent mass must be finite and positive definite.");
    }

    /// <summary>Seed the new row once. Project the cached estimate onto this
    /// contact's current Coulomb disk; the solver can subsequently retract it.</summary>
    public void WarmStart(ContactImpulse impulse)
    {
        Normal.ValidatePose();
        var radius=Friction*impulse.Normal;
        if(!double.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(impulse));
        var tangent=Project(CollisionVector.Dot(impulse.Tangent,_u),CollisionVector.Dot(impulse.Tangent,_v),radius);
        var j=Normal.Jacobian;
        var va=A.AfterImpulse(j.LinearA*impulse.Normal+_ju.LinearA*tangent.X+_jv.LinearA*tangent.Y,
            j.AngularA*impulse.Normal+_ju.AngularA*tangent.X+_jv.AngularA*tangent.Y);
        var vb=B.AfterImpulse(j.LinearB*impulse.Normal+_ju.LinearB*tangent.X+_jv.LinearB*tangent.Y,
            j.AngularB*impulse.Normal+_ju.AngularB*tangent.X+_jv.AngularB*tangent.Y);
        // The row rejects repeated/stale initialization before either body changes.
        Normal.InitializeAccumulatedImpulse(impulse.Normal);
        A.CommitVelocity(va); B.CommitVelocity(vb); _j0=tangent.X; _j1=tangent.Y;
    }

    private double Mass(ConstraintJacobian x,ConstraintJacobian y)=>
        A.InverseMass*CollisionVector.Dot(x.LinearA,y.LinearA)+B.InverseMass*CollisionVector.Dot(x.LinearB,y.LinearB)+
        CollisionVector.Dot(x.AngularA,A.InverseInertia(y.AngularA))+CollisionVector.Dot(x.AngularB,B.InverseInertia(y.AngularB));
    private double Speed(ConstraintJacobian j)=>
        CollisionVector.Dot(j.LinearA,A.LinearVelocity)+CollisionVector.Dot(j.AngularA,A.AngularVelocity)+
        CollisionVector.Dot(j.LinearB,B.LinearVelocity)+CollisionVector.Dot(j.AngularB,B.AngularVelocity);
    private double Radius
    {
        get
        {
            var radius=Friction*Normal.AccumulatedImpulse;
            if(!double.IsFinite(radius)) throw new InvalidOperationException("Friction impulse budget is not finite.");
            return radius;
        }
    }
    private static double Length(double x,double y)
    {
        // Scale before squaring to avoid overflow for otherwise finite inputs.
        var scale=Math.Max(Math.Abs(x),Math.Abs(y));
        if(scale==0) return 0;
        return scale*Math.Sqrt((x/scale)*(x/scale)+(y/scale)*(y/scale));
    }
    private static (double X,double Y) Project(double x,double y,double radius)
    {
        var length=Length(x,y);
        if(!double.IsFinite(length)) throw new InvalidOperationException("Tangent impulse exceeds numeric range.");
        if(length<=radius) return (x,y);
        return (x*(radius/length),y*(radius/length));
    }
    public double Residual
    {
        get
        {
            var residual=Normal.Residual;
            if(_scale==0) return residual;
            var speed0=Speed(_ju); var speed1=Speed(_jv);
            var projected=Project(_j0-speed0/_scale,_j1-speed1/_scale,Radius);
            var tangent=Length(_j0-projected.X,_j1-projected.Y)*_scale;
            if(!double.IsFinite(tangent)) throw new InvalidOperationException("Friction residual is not finite.");
            return Math.Max(residual,tangent);
        }
    }
    public void Solve()
    {
        Normal.Solve();
        if(_scale==0) return;
        var rhs0=_k00*_j0+_k01*_j1-Speed(_ju);
        var rhs1=_k01*_j0+_k11*_j1-Speed(_jv);
        if(!double.IsFinite(rhs0)||!double.IsFinite(rhs1)) throw new InvalidOperationException("Friction solve exceeds numeric range.");
        var radius=Radius;
        var next=radius==0?(X:0.0,Y:0.0):ConstrainedMinimum(rhs0,rhs1,radius);
        var d0=next.X-_j0; var d1=next.Y-_j1;
        var va=A.AfterImpulse(_ju.LinearA*d0+_jv.LinearA*d1,_ju.AngularA*d0+_jv.AngularA*d1);
        var vb=B.AfterImpulse(_ju.LinearB*d0+_jv.LinearB*d1,_ju.AngularB*d0+_jv.AngularB*d1);
        A.CommitVelocity(va); B.CommitVelocity(vb); _j0=next.X; _j1=next.Y;
    }
    private (double X,double Y) ConstrainedMinimum(double rhs0,double rhs1,double radius)
    {
        // Scale K and rhs together. Solve (K + lambda I) j = rhs, with
        // lambda >= 0 selected so |j|=radius when the unconstrained solve slips.
        var a=_k00/_scale; var b=_k01/_scale; var c=_k11/_scale;
        var x=rhs0/_scale; var y=rhs1/_scale;
        (double X,double Y) At(double lambda)
        {
            var aa=a+lambda; var cc=c+lambda; var determinant=aa*cc-b*b;
            if(!double.IsFinite(determinant)||determinant<=0) throw new InvalidOperationException("Friction matrix is not solvable.");
            return ((cc*x-b*y)/determinant,(aa*y-b*x)/determinant);
        }
        var free=At(0);
        if(Length(free.X,free.Y)<=radius) return free;
        double low=0,high=Math.Max(1,Length(x,y)/radius);
        if(!double.IsFinite(high)) throw new InvalidOperationException("Friction multiplier exceeds numeric range.");
        // Positive K ensures ||(K+high I)^-1 rhs|| <= ||rhs||/high.
        var best=At(high);
        for(var i=0;i<96;i++)
        {
            var mid=(low+high)/2;
            var trial=At(mid); var length=Length(trial.X,trial.Y);
            if(length>radius) low=mid;
            else { high=mid; best=trial; }
            if(high-low<=1e-14*Math.Max(1,high)) return best;
        }
        throw new InvalidOperationException("Friction multiplier did not converge.");
    }
}
