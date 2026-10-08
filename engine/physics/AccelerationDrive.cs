using System;

namespace CuriousContraptions.Physics;

public readonly record struct PhysicsDriveId
{
    public int Index { get; }
    public PhysicsDriveId(int index)
    {
        if(index<0)throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}
public enum DriveEffortLimit { None, Lower, Upper, Disabled }

/// <summary>Finite work-conjugate effort acting on an acceleration row.
/// This declaration has no time integration or implicit energy source.</summary>
public sealed record AccelerationDrive
{
    public PhysicsDriveId Id { get; }
    public ConstraintGradient Gradient { get; }
    public double ConvectiveAcceleration { get; }
    public double TargetAcceleration { get; }
    public double MinimumEffort { get; }
    public double MaximumEffort { get; }
    public AccelerationDrive(PhysicsDriveId id,ConstraintGradient gradient,double convectiveAcceleration,
        double targetAcceleration,double minimumEffort,double maximumEffort)
    {
        ArgumentNullException.ThrowIfNull(gradient);
        if(!double.IsFinite(convectiveAcceleration)||!double.IsFinite(targetAcceleration)||
            !double.IsFinite(minimumEffort)||!double.IsFinite(maximumEffort)||minimumEffort>0||maximumEffort<0)
            throw new ArgumentException("Drive acceleration and effort bounds must be finite, with zero in the interval.");
        Id=id;Gradient=gradient;ConvectiveAcceleration=convectiveAcceleration;TargetAcceleration=targetAcceleration;
        MinimumEffort=minimumEffort;MaximumEffort=maximumEffort;
    }
}
public readonly record struct AccelerationDriveResult(PhysicsDriveId Id,double Effort,
    double AchievedAcceleration,DriveEffortLimit Limit);
