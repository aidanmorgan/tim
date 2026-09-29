# Simulation counter core

Pure-C# owned saturating event counters. Each declaration carries a strongly typed SimulationCounterId and positive integer target. Each Increment represents one admitted event delivery. It returns Accumulated below the threshold, Reached exactly once when the threshold is crossed, and Saturated thereafter without changing the count. Read returns typed phase, count and target.

This is ideal digital memory, not a material or power source. It neither samples a held input as repeated deliveries nor supplies electrical energy. The host owns event ordering/deduplication and real electrical routing. Targets from 1 through Int32.MaxValue are supported; increments cannot overflow because saturated counters never increment. Scene catalogue limits may be narrower and must validate separately.

Unknown/duplicate identities, nonpositive targets and invalid transaction lifecycle reject. Capture owns a retained copy; Restore rejects another owner. Begin/Commit/RollbackTransaction reuse checkpoint arrays to restore partial counts and threshold-result replay exactly. Capture/Restore require an idle transaction. Reset creates a new declared world with zero counts. No compatibility format or implicit migration exists.

Six SimulationCountersTests cover targets 1/3/9, repeated saturated deliveries, independent counters, retained snapshots, threshold replay after rollback, invalid identity/declaration/ownership/lifecycle, maximum declared target and zero routine allocation.

```sh
dotnet build CuriousContraptions.tests --no-restore --nologo -m:1 -nr:false -v:q
dotnet CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll -noColor -class '*SimulationCountersTests'
```

Initial build passes with zero warnings/errors (10.95s); core tests 6/6 pass (0.069s). Scene counter integration and per-part browser/production proof are separate pending work. This independent capability does not claim whole-gameplay rollback, committed event publication, completed component coverage or engine replacement completion.
