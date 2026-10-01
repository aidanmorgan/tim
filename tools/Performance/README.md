# Performance evidence analysis

This C# tool reads current diagnostic browser logs. It cannot command or modify a game.

Build and analyze:

```sh
dotnet build tools/Performance
dotnet tools/Performance/bin/Debug/net10.0/Performance.dll sparse_catalogue current-browser.log
```

Inputs are one canonical scenario name and one .log or .gz path. The scenario is a **declared label**, not verified UI construction or qualification. Names are enum-checked: idle_construction, sparse_catalogue, doubled_population, dense_contacts, connected_mechanism, fast_translation, pure_rotation, moving_hollow, coupled_mechanism, visual_heavy and lifecycle. The tool analyzes gameplay tick and separate integrated frame records. Full idle frame pacing and actual recipes for every scenario remain separate open work.

The reader uses the same performance contracts and serialization boundary as the game. Every field is required, unknown members/enum values reject, and untagged historical records are unsupported current input. Historical files remain unchanged. Non-frame console messages are outside this reader's scope.

Samples and counters are grouped by typed originating run ID, including undrained prior-run records emitted in a later batch. Duplicate stages/counters, conflicting tick/sequence ownership, mixed outcomes, invalid values and future run IDs reject. Missing records, overwritten data and failed ticks cannot produce complete coverage.

GameplayFrame is the inclusive frame root, sharing the global sequence with tick records. Frames contain one PhysicalPresentation and one Animation call, plus four aggregated SceneSubmission calls (physical adapter, animation adapter, wavefronts, and optical/light-cone scene work); no physics counters or tick stages are allowed. Adapter scene submission measures the dirty-property write loop after evaluation/composition. PhysicalPresentation and Animation include their respective submission costs, so do not sum inclusive stages. These scopes do not measure Godot renderer traversal, GPU execution or browser compositor cost.

Tick stages are GameplayTick, Networks, Physics and Publication (one, one, four and two calls respectively). Publication aggregates staged read/event capture and committed consumer publication; it excludes transaction checkpoint creation and solver work, which remain in GameplayTick. Failures retain reached stages and may have fewer calls. Missing frame stages or calls make coverage partial, mixed tick/frame records reject, and conflicting outcomes reject. The previous Presentation wire value is unsupported; historical artifacts remain unchanged.

Coverage "complete" means **3600 ordered gameplay tick records with all current tick stages/counters and expected calls, without gaps in the combined sequence**, not correct physics, a verified scenario, a full 30-second active frame interval sample, budget compliance or release qualification. Short/partial runs remain separate. Reports retain inclusive elapsed timing, allocations, nearest-rank p50/p95/p99/max and mean. Work counters sum; maximum predictor coordinates uses a maximum. These timings are not CPU/GPU measurements.

No old-format fallback or migration is provided. Keep the source revision, device/bundle metadata, real-UI recipe, Reset/save proof, failures and captures alongside each report. PERF-02 still needs additional counters, typed entity/revision metadata, all required actual scenarios and current per-part/browser evidence.

Tests are compiled through CuriousContraptions.tests, including missing-field removal, duplicates, missing work, invalid identities, failed outcomes, scenario boundaries and aggregation. See [current spatial-counter checkpoint](../../docs/spatial-work.md) for results and remaining limitations.

## Current P0-003 arithmetic contract

The current [P0-003 measurement freeze](../../docs/work-orders/P0-003/contract.md) defines full fresh Runs, prior complete warm-up attempts, distinct presentation slots and continuous thermal timelines. PerformanceAudit.AnalyzeAttempt, WarmupBeforeFreshRun and AnalyzeThermal are arithmetic rehearsal helpers, not a replacement for CHECK-AGGREGATE's future authenticated evidence gate. Coverage, arithmetic pacing, actual browser pacing and simulated/wall rate are separate typed results. Synthetic and RAF evidence never return BrowserPacing Pass. Thermal continuity/duty alone does not prove its referenced attempts passed.

MeasurementEvidence is a strict current JSON boundary: closed enums use canonical validated wire values; attempt and presentation identities retain distinct types. Unknown/missing/undefined/null records reject. Decimal millisecond timestamps make the arithmetic examples reproducible. No Run controller, game setter, hidden long-run mode or compatibility reader is added.

Run the independent tooling test assembly (including the existing diagnostic reader tests):

```sh
dotnet build tools/Performance.Tests/Performance.Tests.csproj -c Release --nologo
dotnet tools/Performance.Tests/bin/Release/net10.0/Performance.Tests.dll -noColor
```

At P0-003 the inherited linked engine/diagnostic contracts are still unpublished runtime prerequisites. A normal clean checkout cannot yet build this tool. Exact immutable copies and a scratch reconstruction recipe are retained under docs/verification/P0-003; this reconstructed rehearsal is not ordinary clean-checkout or integrated-game reproducibility. P0-029/035 must publish the reviewed runtime prerequisites. No source-copy fallback exists in the tool project.
