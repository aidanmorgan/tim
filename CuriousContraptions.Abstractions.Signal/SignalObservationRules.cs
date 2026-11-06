using CuriousContraptions.Abstractions;

namespace CuriousContraptions.Abstractions.Signal;

/// <summary>Typed committed fields and occurrences supported by each reusable signal operation.</summary>
public static class SignalObservationRules
{
    public static bool Supports(SignalNodeKind kind, TimerElapsedSignal elapsed, PresentationDriver driver, SimulationEventKind occurrence) =>
        driver switch
        {
            PresentationDriver.Event => Emits(kind, elapsed, occurrence),
            PresentationDriver.CommittedActivation => true,
            PresentationDriver.CommittedCount => kind == SignalNodeKind.PulseCount,
            PresentationDriver.CommittedOscillatorPhase or PresentationDriver.CommittedOscillatorPulse => kind == SignalNodeKind.Oscillator,
            PresentationDriver.CommittedTimerProgress or PresentationDriver.CommittedTimerRemaining or
                PresentationDriver.CommittedTimerActive or PresentationDriver.CommittedTimerPhase => kind == SignalNodeKind.Timer,
            PresentationDriver.CommittedContactLoadPhase => kind == SignalNodeKind.ContactLoad,
            _ => false
        };

    public static bool Emits(SignalNodeKind kind, TimerElapsedSignal elapsed, SimulationEventKind occurrence) => occurrence switch
    {
        SimulationEventKind.TriggerActivated => kind is SignalNodeKind.Contact or SignalNodeKind.Orientation or SignalNodeKind.Oscillator ||
            kind == SignalNodeKind.Timer && elapsed == TimerElapsedSignal.Activation,
        SimulationEventKind.Passage => kind == SignalNodeKind.Passage,
        SimulationEventKind.ThresholdReached => kind == SignalNodeKind.PulseCount,
        SimulationEventKind.MemoryChanged => kind == SignalNodeKind.Memory,
        SimulationEventKind.ContactLoad => kind == SignalNodeKind.ContactLoad,
        _ => false
    };
}
