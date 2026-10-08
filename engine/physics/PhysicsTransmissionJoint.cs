using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum TransmissionEngagement { Open, Engaged }

/// <summary>Ideal bidirectional, phase-free transmission between two axial
/// coordinates: output speed = ratio * input speed. This is a velocity-level
/// rolling/coupling law, not a tooth-phase lock. Shared inertia determines both
/// speeds and reactions; no endpoint receives a copied speed or work allowance.
/// The frame guides are independent world declarations.</summary>
public sealed class PhysicsTransmissionJoint : PhysicsJoint
{
    private readonly PhysicsJoint[] _dependencies;
    public override ReadOnlySpan<PhysicsJoint> Dependencies=>_dependencies;
    public PhysicsFrameJoint Input { get; }
    public PhysicsFrameJoint Output { get; }
    public double Ratio { get; }
    public TransmissionEngagement Engagement { get; }
    public PhysicsTransmissionJoint(PhysicsJointId id,PhysicsFrameJoint input,PhysicsFrameJoint output,double ratio,TransmissionEngagement engagement):
        base(id,Participants(input,output),ConnectedBodyCollision.Enabled)
    {
        if(input.Kind==FrameJointKind.BallSocket||output.Kind==FrameJointKind.BallSocket)
            throw new ArgumentException("Transmission endpoints require axial coordinates.");
        if(!double.IsFinite(ratio)||ratio==0)throw new ArgumentOutOfRangeException(nameof(ratio));
        if(ReferenceEquals(input,output))throw new ArgumentException("Transmission requires distinct coordinates.");
        if(!Enum.IsDefined(engagement)) throw new ArgumentOutOfRangeException(nameof(engagement));
        Engagement=engagement;
        Input=input;Output=output;Ratio=ratio;_dependencies=[input,output];
    }
    private static IEnumerable<PhysicsBody> Participants(PhysicsFrameJoint input,PhysicsFrameJoint output)
    {
        ArgumentNullException.ThrowIfNull(input);ArgumentNullException.ThrowIfNull(output);
        return input.Bodies.ToArray().Concat(output.Bodies.ToArray());
    }
    private (ConstraintGradient Gradient,double Bias) Equation()
    {
        var first=Input.Travel;var second=Output.Travel;
        var input=first.Jacobian.Bind(Input.A,Input.B);var output=second.Jacobian.Bind(Output.A,Output.B);
        var terms=new List<ConstraintTerm>();
        foreach(var term in output.Terms)terms.Add(term);
        foreach(var term in input.Terms)terms.Add(new(term.Body,term.Linear*(-Ratio),term.Angular*(-Ratio)));
        var bias=second.ConvectiveAcceleration-Ratio*first.ConvectiveAcceleration;
        if(!double.IsFinite(bias))throw new InvalidOperationException("Transmission acceleration exceeds numeric range.");
        return (new(terms.ToArray()),bias);
    }
    public double SpeedError=>Equation().Gradient.Speed;
    public override IReadOnlyList<ConstraintGradient> BilateralVelocityGradients()=>
        Engagement==TransmissionEngagement.Engaged?[Equation().Gradient]:[];
    public override IReadOnlyList<IImpulseConstraint> UnilateralVelocityConstraints(double activationTolerance)
    {
        ValidateTolerance(activationTolerance);
        return [];
    }
    public override IReadOnlyList<ConstraintAcceleration> AccelerationConstraints(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> sample,double positionTolerance,double velocityTolerance)
    {
        ValidateTolerance(positionTolerance);ValidateTolerance(velocityTolerance);
        if(Engagement==TransmissionEngagement.Open) return [];
        var equation=((PhysicsTransmissionJoint)Rebind(sample)).Equation();
        return [new(equation.Gradient,equation.Bias,AccelerationRelation.Equal)];
    }
    public override PhysicsJoint Rebind(IReadOnlyDictionary<PhysicsBodyId,PhysicsBody> bodies)=>
        new PhysicsTransmissionJoint(Id,(PhysicsFrameJoint)Input.Rebind(bodies),(PhysicsFrameJoint)Output.Rebind(bodies),Ratio,Engagement);
    public override JointSweepResult Sweep(ReadOnlySpan<BodyTrajectory> paths,double duration,double tolerance,double velocityTolerance)
    {
        ValidatePaths(paths,duration,tolerance,velocityTolerance);
        // A phase-free coupling has no positional stops. Axial guide limits are
        // swept by their own declarations; this law contributes coupled velocity
        // and acceleration equations at every shared solve.
        return new(JointSweepStatus.Clear,duration,null,0);
    }
    public override double Error(double queryTolerance)
    {
        ValidateTolerance(queryTolerance);return 0;
    }
    internal override void Project(double tolerance,PositionProjector projector)
    {
        ValidateTolerance(tolerance);ArgumentNullException.ThrowIfNull(projector);
    }
    private static void ValidateTolerance(double value)
    {
        if(!double.IsFinite(value)||value<=0)throw new ArgumentOutOfRangeException(nameof(value));
    }
}
