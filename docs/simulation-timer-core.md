# Simulation timer core

A renderer-independent digital timer capability. Declarations carry strongly typed SimulationTimerId values, positive integer tick durations, TimerCompletionPolicy and TimerBoundary. Ready timers accept a trigger; counting timers reject retriggers. Latch completion remains Finished; Rearm completion returns to Ready and can accept a new trigger. No catalogue identity selects behavior.

BeforeNetworks precedes BeforePhysics at each tick. Advance delivers every elapsed deadline for its declared boundary once in identity order, including deadlines crossed by a larger time advance. Events name their actual due tick. The returned span is borrowed only until the next Advance; retain a copy if needed. Unknown policies and backwards boundaries reject explicitly.

Instances are single-owner, synchronous state. No scene nodes, graphics, physical force equations or resource keys enter the capability. No electricity or energy is supplied by an event. This is a digital controller, not a substitute for physical thermal/fluid/mechanical delay.

Capture owns a retained copy; Restore rejects another world's snapshot and restores the exact clock, boundary and timer states. Capture and Restore require an idle transaction. BeginTransaction copies committed state into reusable owned checkpoint storage; CommitTransaction accepts the changes; RollbackTransaction restores the checkpoint. Nested transactions and unmatched commit/rollback reject. These operations allocate no routine scratch after construction. They cover timer state only, not other gameplay state or published events.

Reset creates a new declared timer world. Unknown identities, invalid durations, backwards time, clock advancement before an admitted future trigger, and deadline overflow reject explicitly. Integer deadlines support the nonnegative Int32 tick range; no wraparound or silent truncation is supported. Advance reuses bounded event storage.

The eight SimulationTimersTests cover deadline/retrigger behavior, retained snapshot replay, all crossed deadlines and ordering, identity/clock/overflow/ownership rejection, both scheduling/completion policies, transaction guards and exact retry after rollback, and warm-up allocation. Build and run:

```sh
dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -v:q
dotnet CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll -noColor -class '*SimulationTimersTests'
```

This publication is the independent capability and its tests. Scene Delay/Hold Timer integration is being verified in the working migration and is not part of this isolated increment. It does not claim a transactional gameplay world, committed bridge/event queue, complete timer family, per-part UI qualification, performance tier or engine replacement completion.
