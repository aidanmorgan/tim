# Simulation timer core

A renderer-independent, one-shot digital timer capability. Declarations carry strongly typed SimulationTimerId values and positive integer tick durations. Ready timers accept one trigger; counting and finished timers ignore retriggers. Advance delivers every elapsed deadline once in identity order, including deadlines crossed by a larger time advance. Events name their actual due tick. The returned span is borrowed only until the next Advance; retain a copy if needed.

Instances are single-owner, synchronous state. No scene nodes, graphics, catalogue IDs, physical force equations or resource keys enter the capability. No electricity or energy is supplied by an event. This is a digital controller, not a substitute for physical thermal/fluid/mechanical delay.

Capture owns a retained copy; Restore rejects another world's snapshot and restores the exact timer clock and states. Reset creates a new declared timer world. Unknown identities, invalid durations, backwards time, clock advancement before an admitted future trigger, and deadline overflow reject explicitly. Integer deadlines support the nonnegative Int32 tick range; no wraparound or silent truncation is supported. Steady Advance reuses bounded event storage and allocates no routine scratch.

The four SimulationTimersTests cover deadline/retrigger behavior, retained snapshot replay, all crossed deadlines and ordering, identity/clock/overflow/ownership rejection and warm-up allocation. Build and run:

```sh
dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -v:q
dotnet CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll -noColor -class '*SimulationTimersTests'
```

This publication is the independent capability and its tests. Scene delay integration is being verified in the working migration and is not part of this isolated increment. It does not claim a transactional gameplay world, committed bridge/event queue, complete timer family, per-part UI qualification, performance tier or engine replacement completion.
