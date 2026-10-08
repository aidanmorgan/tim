using System;

namespace CuriousContraptions.Physics;

public readonly record struct PoweredImpulseResult(double Impulse,double SuppliedWork,double DissipatedWork);

/// <summary>Finite-energy actuation along an arbitrary-body mass/inertia gradient.
/// Positive work is integrated separately from braking along the actual rounded
/// velocity update. Dissipated energy is never available for reverse acceleration.</summary>
public static class PoweredImpulse
{
    private readonly record struct Candidate(BodyVelocityUpdate[] Updates,
        double Impulse,double Supplied,double Dissipated,double Speed);
    internal static PoweredImpulseResult Apply(ConstraintGradient gradient,
        double targetSpeed,double maximumImpulse,double availableWork)
    {
        if(!double.IsFinite(maximumImpulse)||maximumImpulse<0||!double.IsFinite(availableWork)||availableWork<0)
            throw new ArgumentException("Actuator budgets must be finite and nonnegative.");
        var row=new ImpulseConstraint(gradient,targetSpeed,-maximumImpulse,maximumImpulse);
        var k=row.InverseEffectiveMass; var initialSpeed=row.Speed;
        if(k<=0||!double.IsFinite(initialSpeed)) throw new ArgumentException("Actuator needs a finite dynamic response.");
        var difference=targetSpeed-initialSpeed;
        if(!double.IsFinite(difference)) throw new ArgumentException("Actuator speed change is not representable.");
        if(difference==0||maximumImpulse==0) return default;
        var direction=Math.Sign(difference);
        var requested=Math.Min(maximumImpulse,Math.Abs(difference)/k);
        var terms=gradient.Terms.ToArray();

        Candidate Evaluate(double magnitude)
        {
            var impulse=direction*magnitude;
            var updates=new BodyVelocityUpdate[terms.Length];
            for(var i=0;i<terms.Length;i++)
                updates[i]=terms[i].Body.AfterImpulse(terms[i].Linear*impulse,terms[i].Angular*impulse);
            double linear=0,quadratic=0;
            void Work(PhysicsBody body,BodyVelocityUpdate next,CollisionVector jl,CollisionVector ja)
            {
                if(body.MotionType!=PhysicsMotionType.Dynamic)
                {
                    // Work is relative to the actuator's carrier. A prescribed
                    // kinematic carrier is a separate external energy source/sink.
                    linear+=impulse*(CollisionVector.Dot(jl,body.LinearVelocity)+CollisionVector.Dot(ja,body.AngularVelocity));
                    return;
                }
                var dv=next.Linear-body.LinearVelocity; var dl=next.AngularMomentum-body.AngularMomentum;
                linear+=CollisionVector.Dot(body.LinearVelocity,dv)/body.InverseMass+CollisionVector.Dot(body.AngularVelocity,dl);
                quadratic+=.5*(dv.LengthSquared/body.InverseMass+CollisionVector.Dot(dl,body.InverseInertia(dl)));
            }
            for(var i=0;i<terms.Length;i++)
                Work(terms[i].Body,updates[i],terms[i].Linear,terms[i].Angular);
            if(!double.IsFinite(linear)||!double.IsFinite(quadratic)||quadratic<0)
                throw new InvalidOperationException("Actuator work is not representable.");
            double supplied,dissipated;
            if(linear>=0) { supplied=linear+quadratic; dissipated=0; }
            else if(linear+2*quadratic<=0) { supplied=0; dissipated=-(linear+quadratic); }
            else
            {
                var turn=-linear/(2*quadratic);
                dissipated=quadratic*turn*turn;
                supplied=quadratic*(1-turn)*(1-turn);
            }
            CollisionVector Spin(PhysicsBody body,BodyVelocityUpdate next)=>body.MotionType==PhysicsMotionType.Dynamic?
                body.InverseInertia(next.AngularMomentum):body.AngularVelocity;
            double speed=0;
            for(var i=0;i<terms.Length;i++)
                speed+=CollisionVector.Dot(terms[i].Linear,updates[i].Linear)+
                    CollisionVector.Dot(terms[i].Angular,Spin(terms[i].Body,updates[i]));
            if(!double.IsFinite(supplied)||!double.IsFinite(dissipated)||!double.IsFinite(speed))
                throw new InvalidOperationException("Actuator result is not finite.");
            return new(updates,impulse,supplied,dissipated,speed);
        }
        bool Fits(Candidate value)=>value.Supplied<=availableWork&&direction*(value.Speed-targetSpeed)<=0;
        var candidate=Evaluate(requested);
        if(!Fits(candidate))
        {
            double lower=0,upper=requested;
            candidate=Evaluate(0);
            // Bracket the representable update itself, including velocity and
            // angular-momentum rounding, rather than charging an ideal impulse.
            for(var i=0;i<96;i++)
            {
                var middle=lower+(upper-lower)*.5;
                if(middle==lower||middle==upper) break;
                var proposal=Evaluate(middle);
                if(Fits(proposal)) { lower=middle; candidate=proposal; }
                else upper=middle;
            }
        }
        // A representable impulse may still round to no physical velocity
        // change. Do not report an actuation that never reached any body.
        var changed=false;
        for(var i=0;i<terms.Length;i++)
            changed|=candidate.Updates[i].Linear!=terms[i].Body.LinearVelocity||
                candidate.Updates[i].AngularMomentum!=terms[i].Body.AngularMomentum;
        if(!changed) return default;
        // All participants were validated before any body is committed.
        for(var i=0;i<terms.Length;i++) terms[i].Body.CommitVelocity(candidate.Updates[i]);
        return new(candidate.Impulse,candidate.Supplied,candidate.Dissipated);
    }
}
