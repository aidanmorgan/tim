using System;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>An authored switched supply, compiled once at Run: the prismatic actuator it drives, the battery source that feeds the
/// switch, and the switch (State Latch or Counter) whose committed contact closes the route.</summary>
public readonly record struct SwitchedSupplyRouteDeclaration(PrismaticConstraintId Prismatic, ElectricalSourceId Source, GpuBodyId Switch);

/// <summary>The flat compiled route the physics tick reads: indices into the admitted prismatic and electrical-source tables.</summary>
public readonly record struct SwitchedSupplyRoute(int Prismatic, int Source, GpuBodyId Switch);

/// <summary>Player controls the discrete owner hands to one physics step: the enable state of every admitted source, in source order.</summary>
public sealed record WorkshopTickControls(ElectricalEnable[] SourceEnable)
{
    public static WorkshopTickControls For(PhysicsElectricalRead committed, Func<GpuBodyId, ElectricalEnable, ElectricalEnable> enable)
    {
        ArgumentNullException.ThrowIfNull(enable);
        var values = new ElectricalEnable[committed.Count];
        for (var i = 0; i < values.Length; i++) values[i] = enable(committed[i].Owner, committed[i].Enabled);
        return new(values);
    }
    public void Validate(int sources)
    {
        if (SourceEnable is null || SourceEnable.Length != sources || SourceEnable.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Tick controls must name one defined enable state per admitted source.");
    }
}

/// <summary>Everything one physics step reads besides the committed world: per-source enables and, per prismatic record,
/// whether its compiled switched supply is closed this tick. Data flows one way: committed discrete state → next physics step.</summary>
public sealed record PhysicsTickInputs(ElectricalEnable[] SourceEnable, bool[] SwitchedSupplyClosed)
{
    public static PhysicsTickInputs Admission { get; } = new([], []);
    /// <summary>Composes one step's inputs from the compiled routes, the player's source enables and committed switch contacts:
    /// a route is closed only while its switch contact is closed and its battery is enabled.</summary>
    public static PhysicsTickInputs Compose(ReadOnlySpan<SwitchedSupplyRoute> routes, int prismatics, WorkshopTickControls controls,
        Func<GpuBodyId, bool> contactClosed)
    {
        ArgumentNullException.ThrowIfNull(controls); ArgumentNullException.ThrowIfNull(contactClosed);
        var closed = new bool[prismatics];
        foreach (var route in routes)
            closed[route.Prismatic] = controls.SourceEnable[route.Source] == ElectricalEnable.Enabled && contactClosed(route.Switch);
        return new(controls.SourceEnable, closed);
    }
    public void Validate(int sources, int prismatics)
    {
        if (SourceEnable is null || SwitchedSupplyClosed is null || SourceEnable.Length != sources ||
            SwitchedSupplyClosed.Length != prismatics || SourceEnable.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Physics tick inputs differ from the admitted source and prismatic tables.");
    }
}
