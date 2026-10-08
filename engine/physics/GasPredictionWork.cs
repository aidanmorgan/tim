using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

/// <summary>Pure finite-interval checks for sealed gas work and numerical bilateral
/// endpoint velocity projection. Physical contact/stop impulses are not part of this check.</summary>
internal static class GasPredictionWork
{
    internal static bool Accept(IReadOnlyList<AxialGasLoad> chambers,
        IReadOnlyDictionary<PhysicsGasNodeId,AxialGasPotential> potentials,
        IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> original,
        IReadOnlyList<PhysicsJoint> joints,IReadOnlyDictionary<PhysicsBodyId,BodyTrajectory> paths,
        double duration,double completeDuration,double velocityTolerance)
    {
        if(chambers.Count==0)return true;
        var limit=chambers.Min(chamber=>chamber.WorkTolerance)*(duration/completeDuration);
        var quadrature=limit/(16*chambers.Count);
        if(!double.IsFinite(limit)||limit<=0||quadrature<=0)
            throw new InvalidOperationException("Gas interval work allowance is outside numerical range.");
        var midpoint=paths.ToDictionary(pair=>pair.Key,pair=>pair.Value.SampleBody(duration*.5));
        var endpoint=paths.ToDictionary(pair=>pair.Key,pair=>pair.Value.SampleBody(duration));
        double error=0;
        foreach(var chamber in chambers.OrderBy(chamber=>chamber.Node.Index))
        {
            var joint=chamber.Resolve(joints);
            var middle=(PhysicsFrameJoint)joint.Rebind(midpoint);
            var end=(PhysicsFrameJoint)joint.Rebind(endpoint);
            var effort=potentials[chamber.Node].IntervalEffort(joint.Travel.Error,end.Travel.Error);
            var terms=middle.Travel.Jacobian.Bind(middle.A,middle.B).Terms.ToArray();
            var work=WrenchPathWork.Measure(terms.Select(term=>new WrenchPathTerm(
                original[term.Body.Id],paths[term.Body.Id],new(term.Linear*effort,term.Angular*effort))).ToArray(),
                duration,quadrature,0);
            var released=potentials[chamber.Node].Energy(joint.Travel.Error)-potentials[chamber.Node].Energy(end.Travel.Error);
            error+=Math.Abs(work.Supplied-work.Dissipated-released)+work.SuppliedErrorBound+work.DissipatedErrorBound;
        }
        var before=endpoint.ToDictionary(pair=>pair.Key,pair=>pair.Value.Snapshot());
        var rows=joints.Select(joint=>joint.Rebind(endpoint))
            .SelectMany(joint=>joint.BilateralVelocityGradients())
            .Select(gradient=>new ImpulseConstraint(gradient,0,double.NegativeInfinity,double.PositiveInfinity)).ToArray();
        if(rows.Length>0)ImpulseSolver.Solve([new BilateralConstraintBlock(rows)],tolerance:velocityTolerance);
        foreach(var body in endpoint.Values)
        {
            if(body.MotionType!=PhysicsMotionType.Dynamic)continue;
            var linear=body.LinearVelocity-before[body.Id].LinearVelocity;
            var angular=body.AngularMomentum-before[body.Id].AngularMomentum;
            // Squared correction norm isolates numerical relative-velocity loss
            // from legitimate work supplied through prescribed moving boundaries.
            error+=.5*(linear.LengthSquared/body.InverseMass+CollisionVector.Dot(angular,body.InverseInertia(angular)));
        }
        if(!double.IsFinite(error)||error<0)throw new InvalidOperationException("Gas work error exceeds numerical range.");
        return error<=limit;
    }
}
