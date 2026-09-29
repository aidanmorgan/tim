# Simulation latch core

Pure-C# owned reset-dominant digital memory. SimulationLatchId is a nonnegative typed identity; commands, phases, pending requests and transaction lifecycle are enum-typed. The core has no renderer, scene, catalogue, electrical network or physical energy dependency.

A delivery tagged tick t affects the settled phase at boundary t+1. Same-tick Set and Reset produce Off regardless of delivery order; repeated identical inputs are idempotent memory requests. Different ticks remain separate. Without a new request the phase persists. This ideal digital memory conducts no power by itself; the host routes real supply and retains any required command/event occurrences separately.

Advance requires consecutive long integer ticks, beginning with firstTick (default zero). Tick initially equals firstTick minus one. Submit accepts the current boundary's tick or the immediately next tick, enabling admission both before and after that boundary. Negative, late and farther-future deliveries reject without mutation. Two request buckets per latch preserve both adjacent ticks with bounded storage; no dictionary allocation per delivery. This is a supported host scheduling limit, not implicit dropping or rescheduling. The host must advance every tick. At long.MaxValue, further advancement rejects rather than overflowing.

Read returns an immutable value. CurrentRequests refer to Tick; NextRequests refer to Tick+1. Advance settles CurrentRequests, shifts NextRequests and clears the next bucket. Reset dominance is the latch's defined input-resolution rule, not a bridge policy for coalescing arbitrary pulses or command acknowledgements.

Capture retains owned copies of the clock, settled phases and both request buckets. Restore rejects a foreign owner. Begin/Commit/RollbackTransaction reuse checkpoints; Capture/Restore require Idle. Reset constructs a fresh world. No compatibility path or format migration exists.

Seven SimulationLatchesTests cover both same-tick delivery orders across a boundary, adjacent ticks and independent latches, persistent memory, duplicate idempotent requests, both pending buckets, retained snapshot/transaction replay, invalid IDs/commands/ticks/lifecycle, final supported tick and zero routine allocation.

```sh
dotnet build CuriousContraptions.tests --no-restore --nologo -m:1 -nr:false -v:q
dotnet CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll -noColor -class '*SimulationLatchesTests'
```

Initial build: zero warnings/errors, 10.38 seconds. Tests: 7/7 pass, 0.072 seconds. Graph context found no dependency edges; explicit caller review and compilation supplement it. Scene integration, per-part real-browser proof, production export and whole-gameplay rollback are separate unfinished requirements. This core alone does not complete the latch element or bridge.
