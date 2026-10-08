using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum SpringLatchState { Latched, Releasing }
public enum SpringTriggerResult { Released, Empty, AlreadyReleased }

/// <summary>Elastic slider with a one-way winding latch and a releasable transmission.
/// The authored rest coordinate and stroke define its physical stops.</summary>
public sealed record LatchedSpringLoad
{
    public PhysicsJointId Guide { get; }
    public PhysicsJointId Transmission { get; }
    public double Stiffness { get; }
    public double RestCoordinate { get; }
    public double Stroke { get; }
    public AxialElasticLoad Elastic { get; }
    public LatchedSpringLoad(PhysicsJointId guide,PhysicsJointId transmission,double stiffness,double restCoordinate,double stroke)
    {
        if(guide==transmission)throw new ArgumentException("Spring guide and transmission must be distinct.");
        if(!double.IsFinite(stiffness)||stiffness<=0||!double.IsFinite(restCoordinate)||
            !double.IsFinite(stroke)||stroke<=0||!double.IsFinite(restCoordinate-stroke)||
            !double.IsFinite(.5*stiffness*stroke*stroke))
            throw new ArgumentOutOfRangeException(nameof(stiffness));
        Guide=guide;Transmission=transmission;Stiffness=stiffness;RestCoordinate=restCoordinate;Stroke=stroke;
        Elastic=new(guide,FrameJointKind.Slider,new(stiffness,restCoordinate));
    }
    public (PhysicsFrameJoint Guide,PhysicsTransmissionJoint Transmission) Resolve(IEnumerable<PhysicsJoint> joints)
    {
        var all=joints.ToArray();
        if(all.SingleOrDefault(j=>j.Id==Guide) is not PhysicsFrameJoint {Kind:FrameJointKind.Slider} guide||
            all.SingleOrDefault(j=>j.Id==Transmission) is not PhysicsTransmissionJoint transmission||
            transmission.Output!=guide)
            throw new ArgumentException("Spring requires its current slider and output transmission.");
        return (guide,transmission);
    }
    internal void ValidateBinding(IEnumerable<PhysicsJoint> joints,SpringLatchState state)
    {
        if(!Enum.IsDefined(state))throw new ArgumentOutOfRangeException(nameof(state));
        var (guide,transmission)=Resolve(joints);
        if(guide.TravelRange!=new JointTravelRange(RestCoordinate-Stroke,RestCoordinate)||
            guide.Direction!=(state==SpringLatchState.Latched?JointTravelDirection.Negative:JointTravelDirection.Positive)||
            transmission.Engagement!=(state==SpringLatchState.Latched?TransmissionEngagement.Engaged:TransmissionEngagement.Open))
            throw new ArgumentException("Spring joint policy does not match its owned latch state.");
    }
    internal bool SameLaw(LatchedSpringLoad other)=>Guide==other.Guide&&Transmission==other.Transmission&&
        Stiffness==other.Stiffness&&RestCoordinate==other.RestCoordinate&&Stroke==other.Stroke;
}
/// <summary>Signed generalized forces from the final accepted support interval.
/// Negative transmission effort attempts winding; positive local contact effort
/// opposes it. These are solved reactions, not commanded or cumulative motor work.</summary>
public enum SpringResponseDomain { Unobserved, Admissible, BilaterallyLocked }

public readonly record struct SpringConstraintObservation
{
    public SpringResponseDomain Response { get; }
    public double TransmissionEffort { get; }
    public double ContactEffort { get; }
    public double OtherConstraintEffort { get; }
    public double WindingDriveEffort { get; }
    /// <summary>Guide-conjugate observation resolution derived from the solver's
    /// acceleration resolution and arithmetic spacing. This is not a bound on
    /// individual multipliers in a redundant or ill-conditioned system.</summary>
    public double EffortResolution { get; }
    public SpringConstraintObservation(SpringResponseDomain response,double transmissionEffort,double contactEffort,double otherConstraintEffort,double windingDriveEffort,double effortResolution)
    {
        if(!Enum.IsDefined(response)||!double.IsFinite(transmissionEffort)||!double.IsFinite(contactEffort)||!double.IsFinite(otherConstraintEffort)||!double.IsFinite(windingDriveEffort)||
            !double.IsFinite(effortResolution)||effortResolution<0)
            throw new ArgumentException("Spring reaction observations require finite forces and nonnegative resolution.");
        if(response!=SpringResponseDomain.Admissible&&
            (transmissionEffort!=0||contactEffort!=0||otherConstraintEffort!=0||windingDriveEffort!=0||effortResolution!=0))
            throw new ArgumentException("A non-admissible coordinate has no conjugate force observation.");
        Response=response;TransmissionEffort=transmissionEffort;ContactEffort=contactEffort;OtherConstraintEffort=otherConstraintEffort;WindingDriveEffort=windingDriveEffort;EffortResolution=effortResolution;
    }
    public bool OpposesWinding=>Response==SpringResponseDomain.Admissible&&TransmissionEffort < -EffortResolution&&ContactEffort>EffortResolution&&
        ContactEffort+OtherConstraintEffort>EffortResolution&&WindingDriveEffort < -EffortResolution;
}
public readonly record struct PredictedSpringConstraint(PhysicsJointId Guide,SpringConstraintObservation Observation);

public readonly record struct PhysicsLatchedSpringState(LatchedSpringLoad Declaration,SpringLatchState State,
    double Compression,double BeforeCompression,double AcceptedWork,double ReleasedWork,int ReleaseCount,double StopTolerance,SpringTriggerResult? LastTrigger)
{
    public SpringConstraintObservation ConstraintObservation { get; init; }
    public bool IsCharged=>Compression>StopTolerance;
    public double Energy=>.5*Declaration.Stiffness*Compression*Compression;
    internal PhysicsLatchedSpringState Measure(double coordinate)
    {
        var compression=Declaration.RestCoordinate-coordinate;
        if(!double.IsFinite(compression)||compression < -StopTolerance||compression>Declaration.Stroke+StopTolerance)
            throw new InvalidOperationException("Spring coordinate is outside its physical stroke.");
        var next=this with {Compression=compression};
        var change=next.Energy-Energy;
        next=next with {AcceptedWork=AcceptedWork+(State==SpringLatchState.Latched?Math.Max(0,change):0),
            ReleasedWork=ReleasedWork+(State==SpringLatchState.Releasing?-change:0)};
        if(!double.IsFinite(next.Energy)||!double.IsFinite(next.AcceptedWork)||!double.IsFinite(next.ReleasedWork))
            throw new InvalidOperationException("Spring work exceeds numeric range.");
        return next;
    }
}
