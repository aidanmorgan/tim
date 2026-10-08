using System;
using System.Collections.Generic;

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
    private enum FrictionResponse { MaximumDissipation, Sliding }
    public ImpulseConstraint Normal { get; }
    public IEnumerable<ImpulseConstraint> ScalarRows { get { yield return Normal; } }
    private readonly ContactKinematics _kinematics;
    public ReadOnlySpan<PhysicsBody> Bodies=>_kinematics.Bodies;
    public CollisionVector TangentImpulse=>_u*(_j0)+_v*(_j1);
    public double Friction { get; }
    public ContactImpulse Impulse=>new(Normal.AccumulatedImpulse,TangentImpulse);
    private readonly CollisionVector _u,_v;
    private readonly ConstraintGradient _jn,_ju,_jv;
    private readonly int[] _normalIndices,_uIndices,_vIndices;
    private readonly CollisionVector[] _linear,_angular;
    private readonly bool[] _assigned;
    private readonly BodyVelocityUpdate[] _updates;
    private readonly double _k00,_k01,_k11,_scale;
    private double _j0,_j1;
    private readonly FrictionResponse _response;
    private readonly double _bias0,_bias1,_sliding0,_sliding1;

    public ContactConstraint(ContactKinematics kinematics,double restitution,double bounceThreshold,double friction):
        this(kinematics,ImpactNormal(kinematics,restitution,bounceThreshold),friction,
            default,default,FrictionResponse.MaximumDissipation) { }

    private static ImpulseConstraint ImpactNormal(ContactKinematics kinematics,double restitution,double bounceThreshold)
    {
        ArgumentNullException.ThrowIfNull(kinematics);
        kinematics.ValidatePose();
        if(!double.IsFinite(restitution)||restitution<0||restitution>1||
            !double.IsFinite(bounceThreshold)||bounceThreshold<0) throw new ArgumentOutOfRangeException(nameof(restitution));
        var speed=kinematics.NormalGradient.Speed;
        return new(kinematics.NormalGradient,speed < -bounceThreshold ? -restitution*speed : 0,0,double.PositiveInfinity);
    }

    /// <summary>Acceleration-space contact on scratch bodies. Sliding uses
    /// physical material slip supplied by the caller, never scratch acceleration.</summary>
    public static ContactConstraint ForAcceleration(ContactKinematics kinematics,
        double normalBias,CollisionVector tangentBias,CollisionVector physicalSlip,double friction,FrictionRegime regime)
    {
        ArgumentNullException.ThrowIfNull(kinematics);
        if(!double.IsFinite(normalBias)||!tangentBias.IsFinite||!physicalSlip.IsFinite||!Enum.IsDefined(regime))
            throw new ArgumentException("Acceleration contact requires finite bias, slip and a defined regime.");
        var response=regime==FrictionRegime.Sliding?FrictionResponse.Sliding:FrictionResponse.MaximumDissipation;
        return new(kinematics,new(kinematics.NormalGradient,-normalBias,0,double.PositiveInfinity),
            friction,tangentBias,physicalSlip,response);
    }

    private ContactConstraint(ContactKinematics kinematics,ImpulseConstraint normalRow,double friction,
        CollisionVector tangentBias,CollisionVector physicalSlip,FrictionResponse response)
    {
        if(!double.IsFinite(friction)||friction<0) throw new ArgumentOutOfRangeException(nameof(friction));
        kinematics.ValidatePose();
        Normal=normalRow; _kinematics=kinematics; _response=response; Friction=friction;
        _jn=kinematics.NormalGradient; _ju=kinematics.TangentU; _jv=kinematics.TangentV;
        _u=kinematics.U; _v=kinematics.V;
        // The immutable maps can have different participants (for example a
        // driven surface shaft). Bind each map into their validated union once.
        int[] Indices(ConstraintGradient gradient)
        {
            var indices=new int[gradient.Terms.Length];
            var bodyIndex=0;
            for(var i=0;i<indices.Length;i++)
            {
                while(Bodies[bodyIndex]!=gradient.Terms[i].Body)bodyIndex++;
                indices[i]=bodyIndex;
            }
            return indices;
        }
        _normalIndices=Indices(_jn);_uIndices=Indices(_ju);_vIndices=Indices(_jv);
        _linear=new CollisionVector[Bodies.Length];_angular=new CollisionVector[Bodies.Length];
        _assigned=new bool[Bodies.Length];_updates=new BodyVelocityUpdate[Bodies.Length];
        _bias0=CollisionVector.Dot(tangentBias,_u); _bias1=CollisionVector.Dot(tangentBias,_v);
        if(response==FrictionResponse.Sliding)
        {
            var s0=CollisionVector.Dot(physicalSlip,_u); var s1=CollisionVector.Dot(physicalSlip,_v);
            var length=Length(s0,s1);
            if(!double.IsFinite(length)||length==0) throw new ArgumentException("Sliding requires finite nonzero tangent slip.");
            _sliding0=-s0/length; _sliding1=-s1/length;
        }
        _k00=_ju.Coupling(_ju); _k01=_ju.Coupling(_jv); _k11=_jv.Coupling(_jv);
        _scale=_k00+_k11;
        if(!double.IsFinite(_scale)||!double.IsFinite(_k01)||
            (_scale>0&&(_k00<=0||_k11<=0||_k00*_k11-_k01*_k01<=0)))
            throw new ArgumentException("Contact tangent mass must be finite and positive definite.");
    }

    /// <summary>Seed the new row once. Project the cached estimate onto this
    /// contact's current Coulomb disk; the solver can subsequently retract it.</summary>
    internal void WarmStart(ContactImpulse impulse)
    {
        ValidatePose();
        var radius=Friction*impulse.Normal;
        if(!double.IsFinite(radius)) throw new ArgumentOutOfRangeException(nameof(impulse));
        var tangent=Project(CollisionVector.Dot(impulse.Tangent,_u),CollisionVector.Dot(impulse.Tangent,_v),radius);
        var updates=Updates(impulse.Normal,tangent.X,tangent.Y);
        // Reject repeated/stale initialization before any participant changes.
        Normal.InitializeAccumulatedImpulse(impulse.Normal);
        Commit(updates); _j0=tangent.X; _j1=tangent.Y;
    }

    internal (ConstraintGradient U,ConstraintGradient V) TangentGradients=>(_ju,_jv);
    internal double TangentResponseScale=>_scale;
    internal (double U,double V) TangentCoordinates=>(_j0,_j1);
    internal (double U,double V) ProjectTangentCoordinates(double normal,double u,double v)
    {
        var radius=Friction*normal;
        if(!double.IsFinite(radius)) throw new InvalidOperationException("Friction impulse budget is not finite.");
        return Project(u,v,radius);
    }
    internal void CommitTangentCoordinates(double u,double v) { _j0=u;_j1=v; }

    private void ValidatePose()=>_kinematics.ValidatePose();

    private BodyVelocityUpdate[] Updates(double normal,double u,double v)
    {
        Array.Clear(_assigned);
        void Add(ConstraintGradient gradient,int[] indices,double scale)
        {
            for(var i=0;i<indices.Length;i++)
            {
                var term=gradient.Terms[i];var index=indices[i];
                var linear=term.Linear*scale;var angular=term.Angular*scale;
                if(!linear.IsFinite||!angular.IsFinite)
                    throw new ArgumentException("Constraint derivatives must be finite.");
                if(_assigned[index])
                {
                    linear=_linear[index]+linear;angular=_angular[index]+angular;
                    if(!linear.IsFinite||!angular.IsFinite)
                        throw new ArgumentException("Summed derivative exceeds numeric range.");
                }
                _linear[index]=linear;_angular[index]=angular;_assigned[index]=true;
            }
        }
        // Preserve the original first-assignment and normal/U/V addition order,
        // including signed zero, while removing per-correction map allocation.
        Add(_jn,_normalIndices,normal);Add(_ju,_uIndices,u);Add(_jv,_vIndices,v);
        for(var i=0;i<_updates.Length;i++)
            _updates[i]=Bodies[i].AfterImpulse(_linear[i],_angular[i]);
        return _updates;
    }
    private void Commit(BodyVelocityUpdate[] updates)
    {
        for(var i=0;i<updates.Length;i++) Bodies[i].CommitVelocity(updates[i]);
    }
    internal string Diagnostic => $"direction={_kinematics.Normal}, target={Normal.TargetSpeed:R}, normal={Normal.AccumulatedImpulse:R}, tangent=({_j0:R},{_j1:R}), friction={Friction:R}, normal mass={_jn.Coupling(_jn):R}, normal-tangent=({_jn.Coupling(_ju):R},{_jn.Coupling(_jv):R}), tangent mass=({_k00:R},{_k01:R},{_k11:R}), speeds=({_jn.Speed:R},{_ju.Speed:R},{_jv.Speed:R})";
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
            ValidatePose();
            var residual=Normal.Residual;
            if(_scale==0) return residual;
            var speed0=_ju.Speed+_bias0; var speed1=_jv.Speed+_bias1;
            var projected=_response==FrictionResponse.Sliding?
                (X:_sliding0*Radius,Y:_sliding1*Radius):Project(_j0-speed0/_scale,_j1-speed1/_scale,Radius);
            var tangent=Length(_j0-projected.X,_j1-projected.Y)*_scale;
            if(!double.IsFinite(tangent)) throw new InvalidOperationException("Friction residual is not finite.");
            return Math.Max(residual,tangent);
        }
    }
    /// <summary>One coherent normal/tangent natural map. Its projected normal
    /// owns the tangent disk used by both signed equations and force balance.</summary>
    internal (ContactImpulse Reaction,double ProjectedU,double ProjectedV,double Normal,double U,double V) EvaluateReaction(double normal,double u,double v)
    {
        ValidatePose();
        if(!double.IsFinite(normal)||!double.IsFinite(u)||!double.IsFinite(v))
            throw new ArgumentException("Trial contact reactions must be finite.");
        var projectedNormal=Normal.EvaluateReaction(normal);
        // An immovable tangent has no reaction in the existing law.
        if(_scale==0)return(new(projectedNormal.Reaction,default),0,0,projectedNormal.Residual,u,v);
        var radius=Friction*projectedNormal.Reaction;
        if(!double.IsFinite(radius))throw new InvalidOperationException("Trial friction disk exceeds numeric range.");
        var projected=_response==FrictionResponse.Sliding?
            (X:_sliding0*radius,Y:_sliding1*radius):
            Project(u-(_ju.Speed+_bias0)/_scale,v-(_jv.Speed+_bias1)/_scale,radius);
        var ru=(u-projected.X)*_scale;var rv=(v-projected.Y)*_scale;
        var reaction=new ContactImpulse(projectedNormal.Reaction,_u*projected.X+_v*projected.Y);
        if(!double.IsFinite(ru)||!double.IsFinite(rv)||!reaction.Tangent.IsFinite)
            throw new InvalidOperationException("Contact natural map exceeds numeric range.");
        return(reaction,projected.X,projected.Y,projectedNormal.Residual,ru,rv);
    }

    /// <summary>Linearizes the same raw-normal disk map used by Residual.
    /// Geometry, physical Sliding direction and response scale are held fixed.
    /// Disk ties select the interior derivative; zero friction is a constant disk.</summary>
    internal void LinearizeTangents(double[,] mass,double[,] jacobian,double[] residual,int normalIndex,int tangentIndex)
    {
        ValidatePose();
        var uIndex=tangentIndex;var vIndex=tangentIndex+1;
        if(_scale==0)return; // Original immovable tangent has no residual.
        var radius=Radius;
        var speedU=_ju.Speed+_bias0;var speedV=_jv.Speed+_bias1;
        if(_response==FrictionResponse.Sliding||Friction==0)
        {
            var directionU=_response==FrictionResponse.Sliding?_sliding0:0;
            var directionV=_response==FrictionResponse.Sliding?_sliding1:0;
            residual[uIndex]=(_j0-directionU*radius)*_scale;
            residual[vIndex]=(_j1-directionV*radius)*_scale;
            jacobian[uIndex,uIndex]=_scale;jacobian[vIndex,vIndex]=_scale;
            jacobian[uIndex,normalIndex]=-_scale*directionU*Friction;
            jacobian[vIndex,normalIndex]=-_scale*directionV*Friction;
            return;
        }
        var zU=_j0-speedU/_scale;var zV=_j1-speedV/_scale;
        var length=Length(zU,zV);
        if(!double.IsFinite(length))throw new InvalidOperationException("Tangent projection exceeds numeric range.");
        if(length<=radius)
        {
            // The interior projection cancels algebraically; retain the direct
            // physical speed instead of subtracting nearly equal reactions.
            residual[uIndex]=speedU;residual[vIndex]=speedV;
            for(var column=0;column<mass.GetLength(1);column++)
            {
                jacobian[uIndex,column]=mass[uIndex,column];
                jacobian[vIndex,column]=mass[vIndex,column];
            }
            return;
        }
        var nU=zU/length;var nV=zV/length;var factor=radius/length;
        residual[uIndex]=(_j0-radius*nU)*_scale;
        residual[vIndex]=(_j1-radius*nV)*_scale;
        for(var column=0;column<mass.GetLength(1);column++)
        {
            var identityU=column==uIndex?_scale:0;
            var identityV=column==vIndex?_scale:0;
            var dzU=identityU-mass[uIndex,column];
            var dzV=identityV-mass[vIndex,column];
            var radial=column==normalIndex?_scale*Friction:0;
            jacobian[uIndex,column]=identityU-factor*((1-nU*nU)*dzU-nU*nV*dzV)-nU*radial;
            jacobian[vIndex,column]=identityV-factor*((1-nV*nV)*dzV-nU*nV*dzU)-nV*radial;
        }
    }

    void IImpulseConstraint.Solve()=>Solve();
    internal void Solve()
    {
        ValidatePose();
        Normal.Solve();
        if(_scale==0) return;
        var rhs0=_k00*_j0+_k01*_j1-_ju.Speed-_bias0;
        var rhs1=_k01*_j0+_k11*_j1-_jv.Speed-_bias1;
        if(!double.IsFinite(rhs0)||!double.IsFinite(rhs1)) throw new InvalidOperationException("Friction solve exceeds numeric range.");
        var radius=Radius;
        var next=_response==FrictionResponse.Sliding?(X:_sliding0*radius,Y:_sliding1*radius):
            radius==0?(X:0.0,Y:0.0):ConstrainedMinimum(rhs0,rhs1,radius);
        var d0=next.X-_j0; var d1=next.Y-_j1;
        Commit(Updates(0,d0,d1)); _j0=next.X; _j1=next.Y;
    }
    private (double X,double Y) ConstrainedMinimum(double rhs0,double rhs1,double radius)
    {
        // Scale K and rhs together. Solve (K + lambda I) j = rhs, with
        // lambda >= 0 selected so |j|=radius when the unconstrained solve slips.
        var a=_k00/_scale; var b=_k01/_scale; var c=_k11/_scale;
        var x=rhs0/_scale; var y=rhs1/_scale;
        (double X,double Y) At(double lambda)
        {
            // Divide the shifted system and RHS by the same factor before
            // forming its determinant. Tiny Coulomb budgets can require finite
            // multipliers whose square exceeds binary64's range.
            var divisor=Math.Max(1,lambda);
            var shift=lambda/divisor;
            var aa=a/divisor+shift;var bb=b/divisor;var cc=c/divisor+shift;
            var sx=x/divisor;var sy=y/divisor;
            var determinant=aa*cc-bb*bb;
            if(!double.IsFinite(determinant)||determinant<=0) throw new InvalidOperationException("Friction matrix is not solvable.");
            return ((cc*sx-bb*sy)/determinant,(aa*sy-bb*sx)/determinant);
        }
        var free=At(0);
        if(Length(free.X,free.Y)<=radius) return free;
        double low=0,high=Math.Max(1,Length(x,y)/radius);
        if(!double.IsFinite(high)) throw new InvalidOperationException("Friction multiplier exceeds numeric range.");
        // Positive K ensures ||(K+high I)^-1 rhs|| <= ||rhs||/high.
        var best=At(high);
        for(var i=0;i<96;i++)
        {
            var mid=low+(high-low)/2;
            var trial=At(mid); var length=Length(trial.X,trial.Y);
            if(length>radius) low=mid;
            else { high=mid; best=trial; }
            if(high-low<=1e-14*Math.Max(1,high)) return best;
        }
        throw new InvalidOperationException("Friction multiplier did not converge.");
    }
}
