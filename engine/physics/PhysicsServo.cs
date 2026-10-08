using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

public enum PhysicsServoEndpoint { Lower, Upper }

public enum PhysicsServoMode { Hold, Lower, Upper }

/// <summary>Finite-effort endpoint servo with a self-locking hold mode.
/// It commands the shared motor solver; it never integrates a body pose.</summary>
public sealed record PhysicsServoDeclaration
{
    public PhysicsJointId Joint { get; }
    public double MaximumSpeed { get; }
    public double Acceleration { get; }
    public double MaximumEffort { get; }
    public double MaximumPower { get; }
    public PhysicsServoDeclaration(PhysicsJointId joint,double maximumSpeed,double acceleration,
        double maximumEffort,double maximumPower)
    {
        if(!double.IsFinite(maximumSpeed)||maximumSpeed<=0||!double.IsFinite(acceleration)||acceleration<=0||
            !double.IsFinite(maximumEffort)||maximumEffort<=0||!double.IsFinite(maximumPower)||maximumPower<=0)
            throw new ArgumentOutOfRangeException(nameof(maximumSpeed));
        Joint=joint;MaximumSpeed=maximumSpeed;Acceleration=acceleration;
        MaximumEffort=maximumEffort;MaximumPower=maximumPower;
    }
}

/// <summary>Captured guide law and controller memory. Guide retains the authored
/// full range; the world's current joint carries the active hold/drive policy.</summary>
public readonly record struct PhysicsServoState(PhysicsServoDeclaration Declaration,PhysicsFrameJoint Guide,
    PhysicsServoMode Mode,double CommandSpeed,double HoldCoordinate)
{
    public JointTravelRange Range=>Guide.TravelRange!;
    /// <summary>Closed limit test in the joint coordinate's units, including exact tolerance.</summary>
    public bool AtEndpoint(PhysicsServoEndpoint endpoint,double tolerance)
    {
        if(!Enum.IsDefined(endpoint)||!double.IsFinite(tolerance)||tolerance<0)
            throw new ArgumentOutOfRangeException(nameof(endpoint));
        if(Guide is null)throw new InvalidOperationException("Endpoint observation requires a bound servo guide.");
        var coordinate=Guide.Motion.Coordinate;
        if(!double.IsFinite(coordinate))throw new InvalidOperationException("Servo coordinate must be finite.");
        return endpoint switch
        {
            PhysicsServoEndpoint.Lower=>coordinate-Range.Lower<=tolerance,
            PhysicsServoEndpoint.Upper=>Range.Upper-coordinate<=tolerance,
            _=>throw new ArgumentOutOfRangeException(nameof(endpoint))
        };
    }
    internal JointTravelRange ActiveRange=>Mode==PhysicsServoMode.Hold?new(HoldCoordinate,HoldCoordinate):Range;
    internal void ValidateBinding(IEnumerable<PhysicsJoint> joints)
    {
        var id=Declaration.Joint;
        if(joints.SingleOrDefault(j=>j.Id==id) is not PhysicsFrameJoint current||
            current.Kind!=Guide.Kind||current.A!=Guide.A||current.B!=Guide.B||
            current.LocalA!=Guide.LocalA||current.LocalB!=Guide.LocalB||current.Collision!=Guide.Collision||
            current.Direction!=JointTravelDirection.Both||current.TravelRange!=ActiveRange)
            throw new ArgumentException("Servo joint policy must match its owned controller state.");
    }
    internal PhysicsServoState Plan(double duration)
    {
        if(Mode==PhysicsServoMode.Hold)return this with {CommandSpeed=0};
        var target=Mode switch
        {
            PhysicsServoMode.Lower=>Range.Lower,PhysicsServoMode.Upper=>Range.Upper,
            _=>throw new ArgumentOutOfRangeException(nameof(Mode))
        };
        var distance=target-Guide.Motion.Coordinate;
        var increment=Declaration.Acceleration*duration;
        var stoppingSpeedSquared=2*Declaration.Acceleration*Math.Abs(distance);
        if(!double.IsFinite(increment)||!double.IsFinite(stoppingSpeedSquared))
            throw new InvalidOperationException("Servo ramp exceeds numeric range.");
        var requested=Math.Sign(distance)*Math.Min(Declaration.MaximumSpeed,Math.Sqrt(stoppingSpeedSquared));
        return this with {CommandSpeed=Math.Clamp(requested,CommandSpeed-increment,CommandSpeed+increment)};
    }
    internal PhysicsMotorCommand Motor(double duration)=>
        new(Declaration.Joint,CommandSpeed,Declaration.MaximumEffort,Declaration.MaximumPower*duration,Declaration.MaximumPower);
}
