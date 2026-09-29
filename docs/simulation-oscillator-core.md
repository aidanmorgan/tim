# Simulation oscillator core

A renderer-independent powered digital pulse source. Typed identities and positive integer intervals declare the available oscillators. Every consecutive simulation tick supplies one enabled input per declaration in ascending identity order. A rising enabled state schedules the first pulse after one full interval; continuous enablement produces one pulse each interval. A disabled input cancels the current interval before a coincident deadline can fire. Disabled time produces no pulses or backlog. Re-enabling starts a full new interval. Pulse counters and last-pulse timestamps survive disabled periods.

This is digital control timing, with no stored mechanical/electrical energy and no energy supplied by a pulse. It is not a physical pendulum model. Network sampling and delivery are host responsibilities; the scene adapter samples electrical inputs after the network solve and delivers pulses before physical preparation.

The declared numerical envelope is consecutive nonnegative Int32 ticks, positive Int32 intervals, Int32 pulse counts, and representable future deadlines. Duplicate/skipped/backwards ticks, missing/reordered/unknown identities, invalid intervals and overflow reject explicitly. Preflight into reusable staging storage makes each Advance atomic across all declarations. It never silently drops elapsed pulses: a skipped tick is unsupported and rejects before mutation. A disabled oscillator may process a boundary whose enabled future deadline would overflow.

Returned events are ordered by oscillator identity and carry actual tick plus per-oscillator sequence. The borrowed event span lasts until the next Advance attempt. Retained Capture owns its state; Restore rejects foreign owners. Begin/Commit/RollbackTransaction support a larger host transaction using reusable checkpoint storage, preserving counters and schedule on retry. Capture/Restore require idle transactions; nested and unmatched lifecycle operations reject. Retained snapshots allocate by explicit request; steady Advance and transaction rollback allocate no routine storage after construction.

Five SimulationOscillatorsTests prove power-loss precedence/full restart, periodic simultaneous ordering, retained exact replay, invalid boundary and multi-oscillator overflow atomicity, transaction rollback/retry/guards and zero routine allocation.

```sh
dotnet build CuriousContraptions.tests --no-restore --nologo -m:1 -nr:false -v:q
dotnet CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll -noColor -class '*SimulationOscillatorsTests'
```

Initial focused result: 5/5, 0.070s; build 0 warnings/errors, 10.62s. Scene integration and per-clock browser/production evidence are separate pending work. This core does not claim whole-gameplay atomicity, committed event publication, independent animation integration or physics replacement completion.
