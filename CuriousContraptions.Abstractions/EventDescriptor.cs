namespace CuriousContraptions.Abstractions;

/// <summary>The closed set of one-shot transitions the Simulation publishes on the event ring (Story 9A.22). Each kind names a
/// capability, never an element, and exactly one compiled engine row produces it. Its domain and the meaning of each record field
/// are engine-owned (<see cref="SimulationEvents"/>); a package only declares which kinds its compiled rows produce. None is never a
/// record and is refused in a declaration.</summary>
public enum SimulationEventKind : uint
{
    None = 0,
    /// <summary>A contact-work owner's qualified hit (one per occurrence). Magnitude: the energy supplied to the target (J);
    /// Count: the owner's occurrence ordinal this run; Other: the struck body.</summary>
    Impact = 1,
    /// <summary>A passage aperture's forward pass (every pass). Magnitude: the crossing speed (m/s); Count: the n-th pass of that
    /// aperture this run; Other: the passing body.</summary>
    Passage = 2,
    /// <summary>A tally reaching its declared target. Count: the target; Other: the body that caused the final input.</summary>
    ThresholdReached = 3,
    /// <summary>A bistable memory's boundary resolution changing its state. Level: the new state (1 on, 0 off).</summary>
    MemoryChanged = 4,
    /// <summary>A contact-load level rising or releasing. Magnitude: the row's measured mass that tick (kg); Level: 1 or 0.</summary>
    ContactLoad = 5,
    /// <summary>A residence capture latching. Other: the captured body.</summary>
    Captured = 6,
    /// <summary>A supply source's availability edge. Magnitude: the remaining energy (J); Level: 1 available, 0 unavailable.</summary>
    ElectricalPowered = 7,
    /// <summary>A signal source's emitted pulse (contact, orientation, oscillator or elapsed latching timer). Magnitude: the
    /// approach speed or 0 (m/s); Count: the oscillator's pulse count or 0; Other: the causing body.</summary>
    TriggerActivated = 8
}

/// <summary>One kind of event ring record an element's compiled rows produce. The layout is the kind's engine-owned layout.</summary>
public readonly record struct EventDescriptor(SimulationEventKind Kind);

/// <summary>The engine-owned facts of each event kind: one exhaustive table.</summary>
public static class SimulationEvents
{
    /// <summary>The domain a kind's records carry, for consumer filtering. An undefined kind or None is refused.</summary>
    public static SimulationDomain Domain(SimulationEventKind kind) => kind switch
    {
        SimulationEventKind.Impact or SimulationEventKind.Passage or SimulationEventKind.ContactLoad or SimulationEventKind.Captured =>
            SimulationDomain.Mechanical,
        SimulationEventKind.ThresholdReached or SimulationEventKind.MemoryChanged or SimulationEventKind.TriggerActivated =>
            SimulationDomain.Signal,
        SimulationEventKind.ElectricalPowered => SimulationDomain.Electrical,
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "No event record has this kind.")
    };
}
