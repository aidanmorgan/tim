using System;
using System.Linq;

namespace CuriousContraptions.Gpu;

/// <summary>How a compiled supply route closes: straight from its battery's enable, or also through a switch's committed contact.</summary>
public enum SupplyRouteKind : uint { Direct = 1, Switched = 2 }

/// <summary>An authored supply, compiled once at Run: the prismatic actuator it drives, the battery source that feeds it and, for a
/// <see cref="SupplyRouteKind.Switched"/> route only, the switch (State Latch or Counter) whose committed contact closes it.
/// A <see cref="SupplyRouteKind.Direct"/> route's <paramref name="Switch"/> is <c>default</c>.</summary>
public readonly record struct SupplyRouteDeclaration(PrismaticConstraintId Prismatic, ElectricalSourceId Source, SupplyRouteKind Kind, GpuBodyId Switch);

/// <summary>The flat compiled route the physics tick reads: indices into the admitted prismatic and electrical-source tables.</summary>
public readonly record struct SupplyRoute(int Prismatic, int Source, SupplyRouteKind Kind, GpuBodyId Switch);

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
/// whether its compiled supply route is closed this tick. Data flows one way: committed discrete state → next physics step.</summary>
public sealed record PhysicsTickInputs(ElectricalEnable[] SourceEnable, bool[] SupplyClosed)
{
    public static PhysicsTickInputs Admission { get; } = new([], []);
    /// <summary>Composes one step's inputs from the compiled routes, the player's source enables and committed switch contacts:
    /// a route is closed only while its own battery is enabled and, for a switched route, its switch contact is closed. A direct
    /// route never consults a contact (only switches have one).</summary>
    public static PhysicsTickInputs Compose(ReadOnlySpan<SupplyRoute> routes, int prismatics, WorkshopTickControls controls,
        Func<GpuBodyId, bool> contactClosed)
    {
        ArgumentNullException.ThrowIfNull(controls); ArgumentNullException.ThrowIfNull(contactClosed);
        var closed = new bool[prismatics];
        foreach (var route in routes)
            closed[route.Prismatic] = controls.SourceEnable[route.Source] == ElectricalEnable.Enabled &&
                (route.Kind == SupplyRouteKind.Direct || contactClosed(route.Switch));
        return new(controls.SourceEnable, closed);
    }
    public void Validate(int sources, int prismatics)
    {
        if (SourceEnable is null || SupplyClosed is null || SourceEnable.Length != sources ||
            SupplyClosed.Length != prismatics || SourceEnable.Any(value => !Enum.IsDefined(value)))
            throw new ArgumentException("Physics tick inputs differ from the admitted source and prismatic tables.");
    }
}
