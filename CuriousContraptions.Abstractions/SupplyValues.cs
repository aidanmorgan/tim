using System;
using System.Linq;
using System.Collections.Immutable;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Gpu;

public enum SupplyNodeKind { Source, Contact, Consumer, LogicGate }

/// <summary>Which kind of declared drive a consumer node's route enables.</summary>
public enum SupplyConsumerKind : uint { PrismaticDrive, RevoluteDrive }

/// <summary>A typed reference from a consumer node to one driven joint: a prismatic drive named by its prismatic identity, or a
/// revolute drive named by its revolute identity. Only the two factories construct one, so a kind always carries its own identity
/// type; the other identity is unset.</summary>
public readonly record struct SupplyConsumer
{
    private SupplyConsumer(SupplyConsumerKind kind, PrismaticConstraintId prismaticJoint, RevoluteConstraintId revoluteJoint)
    {
        Kind = kind; PrismaticJoint = prismaticJoint; RevoluteJoint = revoluteJoint;
    }

    public SupplyConsumerKind Kind { get; }
    /// <summary>The driven prismatic, for <see cref="SupplyConsumerKind.PrismaticDrive"/>; unset otherwise.</summary>
    public PrismaticConstraintId PrismaticJoint { get; }
    /// <summary>The driven revolute, for <see cref="SupplyConsumerKind.RevoluteDrive"/>; unset otherwise.</summary>
    public RevoluteConstraintId RevoluteJoint { get; }

    public static SupplyConsumer Prismatic(PrismaticConstraintId joint) => new(SupplyConsumerKind.PrismaticDrive, joint, default);
    public static SupplyConsumer Revolute(RevoluteConstraintId joint) => new(SupplyConsumerKind.RevoluteDrive, default, joint);
}

public readonly record struct SupplyNodeDeclaration(GpuBodyId Owner, SupplyNodeKind Kind,
    ElectricalSourceId Source = default, ImmutableArray<SupplyConsumer> Consumers = default, LogicGateOperation? Operation = null);
public readonly record struct SupplyEdge(GpuBodyId Source, GpuBodyId Target, PortRole Input);
public readonly record struct SupplyOutput(GpuBodyId Provider, bool Powered);

/// <summary>Effective outputs of the admitted supply graph at one phase boundary.</summary>
public readonly struct SupplyOutputRead
{
    private readonly SupplyOutput[]? _rows;
    public SupplyOutputRead(ReadOnlySpan<SupplyOutput> rows)
    {
        var seen = new System.Collections.Generic.HashSet<GpuBodyId>();
        foreach (var row in rows)
            if (row.Provider.Value == 0 || !seen.Add(row.Provider))
                throw new ArgumentException("Supply outputs require unique declared providers.");
        _rows = rows.ToArray();
    }
    public bool IsPowered(GpuBodyId provider)
    {
        if (provider.Value == 0) return false;
        foreach (var row in _rows ?? [])
            if (row.Provider == provider) return row.Powered;
        throw new ArgumentException("Selected supply provider is missing.");
    }
}
public readonly record struct SupplyResolution(bool[] Consumers, ElectricalLogicRead[] Logic, SupplyOutputRead Outputs);
