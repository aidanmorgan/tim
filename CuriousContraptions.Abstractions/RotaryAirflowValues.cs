using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public readonly record struct LocalRotaryAirflowCaptureId(uint Value);
public readonly record struct RotaryAirflowCaptureId(ulong Value);

/// <summary>Samples are metres in the explicit frame; pitch m/rad, response rad/s/N,
/// cut-in N and maximum requested unloaded rate rad/s. The coordinate owns the axis.</summary>
public sealed record RotaryAirflowCaptureDescriptor(LocalRotaryAirflowCaptureId Id, PackageBodyIndex Frame,
    CoordinateReference Coordinate, IReadOnlyList<AirflowSample> Samples,
    IReadOnlyList<LocalColliderReference> InternalOccluders, MechanicalValue MaximumPitch,
    MechanicalValue Response, MechanicalValue CutInForce, MechanicalValue MaximumRate);

public sealed record RotaryAirflowCaptureDeclaration(RotaryAirflowCaptureId Id, GpuBodyId Owner, GpuBodyId Frame,
    RevoluteConstraintId Coordinate, ImmutableArray<AirflowSample> Samples,
    ImmutableArray<GpuColliderId> InternalOccluders, float MaximumPitch, float Response,
    float CutInForce, float MaximumRate)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || Frame.Value == 0 || Coordinate.Value == 0 ||
            InternalOccluders.IsDefault)
            throw new ArgumentException("Invalid rotary airflow identity or exclusions.");
        AirflowSampleValidation.Validate(Samples);
        var seen = new HashSet<GpuColliderId>();
        foreach (var collider in InternalOccluders)
            if (collider.Value == 0 || !seen.Add(collider))
                throw new ArgumentException("Invalid or duplicate rotary internal occluder.");
        RotaryAirflowLaw.Prepare([], MaximumPitch, Response, CutInForce, MaximumRate);
    }
}
