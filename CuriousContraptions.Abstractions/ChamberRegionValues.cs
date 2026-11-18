using System;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public readonly record struct ChamberRegionId(ulong Value);
public readonly record struct LocalChamberRegionId(uint Value);
public enum ChamberPhase : byte { Empty, Loading, Seated, Ambiguous, Departing, GeometryUnresolved }

/// <summary>A finite cylindrical query in an exact body frame. Local Y is its axis; both transverse support widths must qualify.</summary>
public readonly record struct ChamberRegionDescriptor(LocalChamberRegionId Id, PackageBodyIndex Frame,
    BodyColliderIndex ParticipationCollider, Vector3 Position, Quaternion Rotation, float Radius, float HalfLength,
    float MinimumWidth, float MaximumWidth, float ContainmentAllowance, float MaximumRelativeSpeed,
    SensorParticipation Participation = SensorParticipation.Enabled, TargetSelector Target = default);

public readonly record struct ChamberRegionDeclaration(ChamberRegionId Id, GpuBodyId Frame,
    GpuColliderId ParticipationCollider, BodyTargetSet Targets, RigidLocalPose Pose, float Radius, float HalfLength,
    float MinimumWidth, float MaximumWidth, float ContainmentAllowance, float MaximumRelativeSpeed, SensorParticipation Participation)
{
    public void Validate()
    {
        if (Id.Value == 0 || Frame.Value == 0 || ParticipationCollider.Value == 0 || Targets.Body == Frame || !Enum.IsDefined(Participation))
            throw new ArgumentException("Invalid chamber region identity or participation.");
        Targets.Validate(); Pose.Validate();
        PhysicsDeclarationBounds.Range(Radius, 1f / 1024f, 16f);
        PhysicsDeclarationBounds.Range(HalfLength, 1f / 1024f, 16f);
        if (!float.IsFinite(MinimumWidth) || !float.IsFinite(MaximumWidth) || MinimumWidth < 0f || MaximumWidth < MinimumWidth ||
            !float.IsFinite(ContainmentAllowance) || ContainmentAllowance < 0f)
            throw new ArgumentException("Invalid chamber size admission.");
        new ContactSpeed(MaximumRelativeSpeed).Validate(LinearVelocity.MaximumSpeed);
    }
}

/// <summary>One region owns departure until that actual body fully exits; every actuator sharing the region observes Busy.</summary>
public readonly record struct ChamberRegionRead(ChamberRegionId Id, GpuBodyId Frame, ChamberPhase Phase,
    uint Occupancy, GpuBodyId Candidate, GpuBodyId Departing)
{
    public void Validate()
    {
        if (Id.Value == 0 || Frame.Value == 0 || !Enum.IsDefined(Phase) || Candidate == Frame || Departing == Frame ||
            (Occupancy == 1) != (Candidate.Value != 0) ||
            (Phase == ChamberPhase.Empty && Occupancy != 0) ||
            (Phase is ChamberPhase.Loading or ChamberPhase.Seated && Occupancy != 1) ||
            (Phase == ChamberPhase.Ambiguous && Occupancy < 2) ||
            (Phase == ChamberPhase.Departing && Departing.Value == 0) ||
            (Phase is not (ChamberPhase.Departing or ChamberPhase.GeometryUnresolved) && Departing.Value != 0) ||
            (Phase == ChamberPhase.GeometryUnresolved && (Occupancy != 0 || Candidate.Value != 0)) ||
            (Phase == ChamberPhase.Departing && Occupancy == 0))
            throw new ArgumentException("Invalid committed chamber region state.");
    }
}
