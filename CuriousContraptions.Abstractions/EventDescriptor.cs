namespace CuriousContraptions.Abstractions;

public enum SimulationEventKind : uint
{
    None = 0,
    Impact = 1,
    Passage = 2,
    CounterReached = 3,
    LatchSet = 4,
    ContactLoad = 5,
    PrismaticLimit = 6,
    ElectricalPowered = 7,
    TriggerActivated = 8
}

public readonly record struct EventDescriptor(
    SimulationEventKind EventKind,
    string PayloadLayout);
