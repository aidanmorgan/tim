using System.Text.Json.Serialization;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public enum PhysicalRequestTargetKind : byte { None, Retention, FiniteWorkActuator }

/// <summary>Exact local capability selection. Inactive identity lanes must be zero.</summary>
public readonly record struct LocalPhysicalRequestTarget(
    [property: JsonRequired] PhysicalRequestTargetKind Kind,
    [property: JsonRequired] LocalPrismaticRetentionId Retention,
    [property: JsonRequired] LocalFiniteWorkActuatorId Actuator)
{
    public bool IsValid => Kind switch
    {
        PhysicalRequestTargetKind.Retention => Retention.Value != 0 && Actuator.Value == 0,
        PhysicalRequestTargetKind.FiniteWorkActuator => Retention.Value == 0 && Actuator.Value != 0,
        _ => false
    };
    public static LocalPhysicalRequestTarget ForRetention(LocalPrismaticRetentionId id) => new(PhysicalRequestTargetKind.Retention, id, default);
    public static LocalPhysicalRequestTarget ForActuator(LocalFiniteWorkActuatorId id) => new(PhysicalRequestTargetKind.FiniteWorkActuator, default, id);
}

/// <summary>Resolved request target; kind and exact identity remain typed through routing and execution.</summary>
public readonly record struct PhysicalRequestTarget(PhysicalRequestTargetKind Kind, PrismaticRetentionId Retention, FiniteWorkActuatorId Actuator)
{
    public bool IsValid => Kind switch
    {
        PhysicalRequestTargetKind.Retention => Retention.Value != 0 && Actuator.Value == 0,
        PhysicalRequestTargetKind.FiniteWorkActuator => Retention.Value == 0 && Actuator.Value != 0,
        _ => false
    };
    public static PhysicalRequestTarget ForRetention(PrismaticRetentionId id) => new(PhysicalRequestTargetKind.Retention, id, default);
    public static PhysicalRequestTarget ForActuator(FiniteWorkActuatorId id) => new(PhysicalRequestTargetKind.FiniteWorkActuator, default, id);
}
