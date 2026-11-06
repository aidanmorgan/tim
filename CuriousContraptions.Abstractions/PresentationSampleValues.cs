using CuriousContraptions.Abstractions;
using System;

namespace CuriousContraptions.Gpu;

public enum PresentationBoundary { Continuous, Seed, Run, Completed }
public enum PresentationQuality { AwaitingHistory, Interpolated, ClockInvalid, Stale, Terminal, Faulted, Retired }
public readonly record struct PresentationSimulationTime(PhysicalTime Time);
public readonly record struct WorkshopPresentationSample(PresentedBodySet Bodies, MonotonicNanoseconds? DisplayWall,
    PresentationSimulationTime SimulationTime, PresentationQuality Quality, WorkshopPresentationEvidence Evidence = default, PhysicsCaptureRead Captures = default, WorkshopClockStamp? FeedbackCapture = null, PhysicsActivationRead Activations = default, PhysicsTimerRead Timers = default);
public readonly record struct PresentationEndpoint(SimulationTick Tick, WorkshopClockStamp Capture, MappedCapture? Mapped);
public readonly record struct WorkshopPresentationEvidence(MonotonicNanoseconds SelectedAt, ulong ClockEpoch, ulong DisplayEpoch,
    SimulationEpoch WorldEpoch, PresentationEndpoint? Latest, PresentationEndpoint? Before, PresentationEndpoint? After);
