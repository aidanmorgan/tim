using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Text.Json.Serialization;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Gpu;

public readonly record struct LocalAirflowSourceId(uint Value);
public readonly record struct LocalAirflowReceiverId(uint Value);
public readonly record struct AirflowSourceId(ulong Value);
public readonly record struct AirflowReceiverId(ulong Value);

public enum AirflowSampling : byte { LocalPosition, ColliderNormalized }
public enum ContinuousWorkInputKind : byte { Store, Electrical, Coordinate }
public enum AirflowAvailability : byte { Unavailable, Available }

/// <summary>A bounded selection by one declared choice parameter; values are its canonical option indices.</summary>
public readonly record struct ChoicePredicate(ParameterId Parameter, int Option);

public readonly record struct AirflowEnableDescriptor(ParameterId InitialState, SignalNodeId Latch);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Input")]
[JsonDerivedType(typeof(StoredAirflowInput), "Store")]
[JsonDerivedType(typeof(ElectricalAirflowInput), "Electrical")]
[JsonDerivedType(typeof(CoordinateAirflowInput), "Coordinate")]
public abstract record AirflowInputDescriptor(ChoicePredicate? Selection)
{
    [JsonRequired]
    public AirflowEnableDescriptor? Enable { get; init; }
}
public sealed record StoredAirflowInput(ChoicePredicate? Selection, LocalEnergyStoreId Store) : AirflowInputDescriptor(Selection);
public sealed record ElectricalAirflowInput(ChoicePredicate? Selection, LocalSupplyNodeId Consumer) : AirflowInputDescriptor(Selection);
public sealed record CoordinateAirflowInput(ChoicePredicate? Selection, CoordinateReference Coordinate, PortId Port,
    float Sense) : AirflowInputDescriptor(Selection);

/// <summary>One distributed receiver sample. Weight is dimensionless; position uses the declared sampling frame.</summary>
public readonly record struct AirflowSample(Vector3 Position, float Weight);

/// <summary>Package-owned cone and finite work inputs. Origin/radius/reach are metres, flow m/s, force N and power W.</summary>
public sealed record AirflowSourceDescriptor(LocalAirflowSourceId Id, PackageBodyIndex Frame, Vector3 Origin,
    Vector3 Axis, MechanicalValue Reach, MechanicalValue TerminalRadius, MechanicalValue FlowSpeed,
    MechanicalValue MaximumForce, MechanicalValue MaximumPower, IReadOnlyList<LocalColliderReference> InternalOccluders,
    IReadOnlyList<AirflowInputDescriptor> Inputs);

public sealed record AirflowReceiverDescriptor(LocalAirflowReceiverId Id, LocalColliderReference Collider,
    AirflowSampling Sampling, IReadOnlyList<AirflowSample> Samples);

/// <summary>Resolved work input; precisely one typed identity is populated. Connectedness is compiled from the exact input port.</summary>
public readonly record struct ContinuousWorkInput(ContinuousWorkInputKind Kind, EnergyStoreId Store,
    SupplyNodeId Electrical, RevoluteConstraintId Coordinate, float Sense, bool Connected)
{
    public void Validate()
    {
        var valid = Kind switch
        {
            ContinuousWorkInputKind.Store => Store.Value != 0 && Electrical == default && Coordinate == default && Sense == 0 && Connected,
            ContinuousWorkInputKind.Electrical => Store == default && Electrical.Value != 0 && Coordinate == default && Sense == 0 && Connected,
            ContinuousWorkInputKind.Coordinate => Store == default && Electrical == default && Coordinate.Value != 0 && (Sense == 1 || Sense == -1),
            _ => false
        };
        if (!valid) throw new ArgumentException("Invalid exact continuous-work input.");
    }
}

public sealed record AirflowSourceDeclaration(AirflowSourceId Id, GpuBodyId Owner, GpuBodyId Frame,
    Vector3 Origin, Vector3 Axis, float Reach, float TerminalRadius, float FlowSpeed, float MaximumForce,
    float MaximumPower, float PressureScale, ImmutableArray<GpuColliderId> InternalOccluders,
    ContinuousWorkInput Input, bool InitiallyEnabled = true, ActivationNodeId? Enable = null)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || Frame.Value == 0 || InternalOccluders.IsDefault)
            throw new ArgumentException("Invalid airflow identity or missing exclusions.");
        AirflowValidation.Vector(Origin);
        AirflowValidation.Vector(Axis);
        if (MathF.Abs(Axis.LengthSquared() - 1f) > 1e-5f)
            throw new ArgumentException("Airflow axis must be normalized.");
        AirflowValidation.Positive(Reach);
        AirflowValidation.Positive(TerminalRadius);
        AirflowValidation.Nonnegative(FlowSpeed);
        AirflowValidation.Nonnegative(MaximumForce);
        AirflowValidation.Nonnegative(MaximumPower);
        if (PressureScale != 0 && PressureScale != 1)
            throw new ArgumentException("Unsupported atmosphere pressure scale.");
        var seen = new HashSet<GpuColliderId>();
        foreach (var collider in InternalOccluders)
            if (collider.Value == 0 || !seen.Add(collider))
                throw new ArgumentException("Invalid or duplicate internal occluder.");
        Input.Validate();
        if (Enable is { Value: 0 }) throw new ArgumentException("Invalid airflow enable identity.");
    }
}

public sealed record AirflowReceiverDeclaration(AirflowReceiverId Id, GpuBodyId Owner, GpuColliderId Collider,
    AirflowSampling Sampling, ImmutableArray<AirflowSample> Samples)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || Collider.Value == 0 || !Enum.IsDefined(Sampling) || Samples.IsDefaultOrEmpty)
            throw new ArgumentException("Invalid airflow receiver.");
        AirflowSampleValidation.Validate(Samples);
    }
}

public readonly record struct AirflowSourceRead(AirflowSourceId Id, GpuBodyId Owner, ContinuousWorkInputKind Input,
    AirflowAvailability Availability, float FlowSpeed, float TransferPower, Joules Work, Joules JetLoss, Joules CommitWork, Joules CommitLoss)
{
    public void Validate()
    {
        if (Id.Value == 0 || Owner.Value == 0 || !Enum.IsDefined(Input) || !Enum.IsDefined(Availability))
            throw new ArgumentException("Invalid airflow observation.");
        AirflowValidation.Nonnegative(FlowSpeed);
        AirflowValidation.Nonnegative(TransferPower);
        Work.Validate(float.MaxValue);
        JetLoss.Validate(float.MaxValue);
        CommitWork.Validate(float.MaxValue);
        CommitLoss.Validate(float.MaxValue);
        if (Availability == AirflowAvailability.Unavailable && FlowSpeed != 0)
            throw new ArgumentException("Unavailable airflow cannot report flow.");
    }
}

public static class AirflowSampleValidation
{
    public static void Validate(ImmutableArray<AirflowSample> samples)
    {
        if (samples.IsDefaultOrEmpty) throw new ArgumentException("Missing airflow samples.");
        var sum = 0f;
        foreach (var sample in samples)
        {
            AirflowValidation.Vector(sample.Position);
            AirflowValidation.Positive(sample.Weight);
            sum += sample.Weight;
        }
        // Admission arithmetic for dimensionless normalized weights, not a scene-motion tolerance.
        if (!float.IsFinite(sum) || MathF.Abs(sum - 1f) > 1e-5f)
            throw new ArgumentException("Receiver sample weights must sum to one.");
    }

}

internal static class AirflowValidation
{
    public static void Nonnegative(float value)
    {
        if (!float.IsFinite(value) || value < 0) throw new ArgumentException("Expected finite nonnegative airflow quantity.");
    }

    public static void Positive(float value)
    {
        if (!float.IsFinite(value) || value <= 0) throw new ArgumentException("Expected finite positive airflow quantity.");
    }

    public static void Vector(Vector3 value)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z))
            throw new ArgumentException("Expected finite airflow vector.");
    }
}
