# S002 benchmark manifest and threshold freeze

1 October 2026. Scope: S002 only; documentation, no runtime or tooling implementation. Coordinator scope review accepted after the source-confirmed corrections below; scoped publication pending. This instantiates thresholds before implementation; it does not close PERF-01/02/21/39, S005's executable recipe/support manifest, S006's consumer inventory, or any device tier.

Authority: [TODO measurable gates](../TODO.md#worker-performance-budgets), [common gates](../TODO.md#design-acceptance), specifications [003](../TODO.md#sequence-task-003), [005](../TODO.md#sequence-task-005), [029](../TODO.md#sequence-task-029), [047](../TODO.md#sequence-task-047), and [performance design](browser-physics-performance.md). Existing numeric gates are retained without relaxation. Additional caps below are initial engineering acceptance targets, not inferred measurements; review them before an accepting run and preserve any later revision and rationale.

## Source and measured baseline

Reviewed HEAD: a470f84d6c77a874858dbdb616f647271b5a61a7, containing the S001 evidence publication. Runtime remains the dirty migration identified by [S001's 916-entry source manifest](s001-source-baseline.json), digest 68a68a6589944ef576f667af22d9ba2a21606e2288ef6364342ba5b23d6ab693. [S001 verdict](s001-baseline.md) retains build success, 76 native cases / 66 passes / 10 failures and partial browser windows. Its publication-pending wording is historical; TODO records publication. No new benchmark was run for this documentation row.

| Existing observation | Measured scope | Verdict under frozen gates |
| --- | --- | --- |
| First-principles physical / combined | RAF p95 83.6 / 83.7 ms; p99 84.3 / 84.1 ms; visible simulation 26.27 / 28.96 seconds | Frame budgets fail; neither reaches required 30 simulated seconds or repeated warmed qualification. |
| Stopped-physics hint active / idle | RAF p95 9.2 / 9.2 ms, p99 9.3 / 9.4 ms | These small windows meet interval limits only; worker CPU, actual presentations, freshness and sustained qualification missing. |
| Compliant positive / fresh depth control | 3,600 ticks each; complete tick p95 17.1 / 10.5 ms; physical stage 16.0 / 9.6 ms | Complete-tick budget fails; no worker separation. |
| Compliant production | 50-second window including Run lead-in; RAF p95 150.1 ms, p99 159.0 ms | Both frame tiers fail. |
| WoundSpring / Bellows | Five failures each in S001 native results | Live correctness failures; unchanged tolerances. |
| Unexpected input | Recurrence outside S001 selected windows; earlier altered attempt invalidated | Attribution unresolved for S007. Do not treat a clean retry as a fix. |

## Devices and support

| Reference | Identity / access | Target and prerequisite |
| --- | --- | --- |
| Available development baseline | MacBook Pro M4 Max, 64 GB, macOS 26.5.2, Chrome 154.0.8037.58, viewport 1440×900, DPR 1 | Chrome via Playwright. Measured RAF cadence near 120 Hz does not establish physical display mode or distinct presentations. Current power, thermal temperature, backing-buffer size and GPU timing must be captured per run; old AC/power observations are not current readings. |
| Proposed integrated-GPU reference | Lenovo ThinkPad T14 Gen 5 AMD, Ryzen 5 PRO 8540U / Radeon 740M; proposed 16 GB RAM, Windows 11, 60 Hz panel. Physical access unavailable | 60 Hz qualification. Actual SKU/RAM/panel, OS build, Chrome/Edge/Firefox versions and power/thermal data required before measurement. This is a target configuration, not an acquired device. |
| Owner-selected mobile reference | Physical Pixel 8 Pro; access, OS/browser versions and power/thermal state unverified | Android Chrome 60 Hz plus >=90 Hz only when actual display mode and distinct presentation evidence permit. Desktop emulation cannot close it. |
| Remaining mandatory classes | Low-range physical Android; physical iOS Safari; applicable Windows/macOS/Linux integrated-GPU Chrome/Edge/Firefox and macOS Safari | Exact additional device/version identities remain unassigned, explicitly open for S005 and later device qualification. Pixel 8 Pro alone does not cover low-range Android; M4 Max alone does not cover typical integrated-GPU laptops. |

Lenovo's [primary platform specification](https://psref.lenovo.com/syspool/Sys/PDF/ThinkPad/ThinkPad_T14_Gen_5_AMD/ThinkPad_T14_Gen_5_AMD_Spec.pdf) confirms the processor and integrated graphics pairing; RAM/OS/panel above are proposed test configuration, not observed hardware. No purchasing action is implied.

Every accepting device record includes CPU/GPU/RAM, OS/browser versions, source/bundle hashes, runtime/SDK/2dog versions, WebGL/Wasm/worker capabilities, HTTP headers, cache state, CSS viewport/DPR and actual canvas backing dimensions, physical display mode, power supply/mode, battery and available thermal readings before/after. Unknown readings stay unknown. Use native device viewport/render scale with exact dimensions retained for device qualification; also repeat 1440×900 DPR1 on desktop for baseline comparability. Do not silently reduce render quality.

## Exact existing workload definitions

Counts distinguish authored objects from instantiated simulation bodies. Idle construction has no running PhysicsWorld. A potential Run assembly includes the workbench: one static body, two convex children. Diagnostic frame `bodies` lists dynamic payloads only; its length is not total physical body count.

| Workload | Reproducible real UI recipe and fixed configuration | Source-derived / captured counts |
| --- | --- | --- |
| Idle control | Reload First principles; retain default ball [-4,6.5,0], receiver [2.5,0.9,0], precision .45; place no ramps; physics stopped, goal panel open, hint not active. Same state as S001 animation idle prefix. | 2 authored parts, 0 explicit connections; no running physics. Potential assembly: 3 bodies (1 dynamic, 2 static), 8 collider children (ball1 + basket5 + workbench2); 0 declared joints; 1 residence sensor; no graph edges/domain emitters. One registered hint target, 0 active hint animations. |
| Smallest physical control | Same construction; actual Run button (688,836), allow timeout, then Reset. Ball falls/settles away from receiver. Do not call this a solved construction. | Same 3 bodies / 8 children, 0 joints/edges, 1 receiver-body residence sensor; active contact rows and solver iterations measured over time, not fixed topology counts. No hint animation requested. |
| Animation-only | Same stopped construction; click Show hint (1186,258) 60 times at 500 ms intervals after a 5-second idle prefix, matching S001. | One registered target, at most one hint opacity animation; 30 reveal starts and 30 hides from the 60 toggle clicks in approximately 30 seconds, each reveal nominally .16 seconds. Active/dirty count varies 0–1. These are cosmetic starts, not simulation events. No simulation tick requirement while stopped. |
| Combined | Same physical fixture; Run, then 30 Show hint clicks spaced 1,000 ms, followed by Reset only after capture. For future longer attempts continue the same 1 Hz click schedule until the active window ends. | Physical counts unchanged; at most one active hint animation; 15 reveal starts and 15 hides from the 30 toggle clicks in the original S001 recipe. Not a many-animation workload. |
| Compliant integration positive | Use recorded palette/3D-handle actions in [receiver evidence](committed-compliant-receiver.json), level63, precision .45; trampoline [0,3,0] rotated -30 degrees Z, ball [0,6.999069,0], receiver [5.4,1.195014,0]. Tension180 N/m, damping ratio .12; ball radius .34 m, mass1 kg, bounce .55, drag .04, buoyancy0. | 3 parts, 4 bodies (1 dynamic,3 static), 13 collider children (trampoline5 + ball1 + basket5 + workbench2), 0 joints/explicit edges, 1 compliant load, 1 residence sensor. No separate physics emitter. Expected: ball reaches receiver near [5.0501,1.160114,0]. |
| Compliant matched control | [Fresh control actions/state](committed-compliant-control.json): same configuration except ball depth1.996268. | Same counts. Expected: misses trampoline/receiver and remains near x=0; no positive transfer claim from floor bounce. |

Source derivation: [puzzle content](../content/puzzles.json), [workbench](../engine/Workbench.cs), [construction capture](../engine/WorldGeometry.cs), [ball](../parts/BallPart.cs), [basket](../parts/BasketPart.cs), [trampoline](../parts/TrampolinePart.cs), [hint lifecycle](../ui/WorkshopAnimation.cs). Basket has five AddBox colliders; decorative boxes/ring are not collider children. Trampoline has five AddBox colliders; rendered membrane and legs do not add collider children. Body/contact/animation counts are source-derived where telemetry does not enumerate them, not newly measured counts. Numerical coordinates in this manifest are read-only assertions; actual construction must still use palette/handles, never numeric placement menus or setters.

Current membrane cosmetic processing remains part-local; hint evaluation remains browser-thread C#. Zero actual worker instances exist in the current pipeline. Neither baseline is evidence of the requested worker architecture.

## Required later workload tiers

These are minimum target populations, not measured capacities or newly executed recipes. S005 must bind exact existing/publicly constructed recipes, supported property modes, positions, rates and derived topology to these targets before their accepting run. Unsupported populations remain a failed/unavailable workload, never silently smaller. Every new part adds its own incremental positive/control cost alongside this shared set.

| Required class | Fixed target population / variation | Additional counts to freeze with executable recipe |
| --- | --- | --- |
| Sparse / doubled-sparse | 16 / 32 independent balls at default radius/mass, nonoverlapping and spatially separated; shared workbench | 17/33 bodies and18/34 children; zero joints/edges. Measure pair visits against exhaustive enumeration; placement extents and contacts recorded. |
| Dense-contact | 32 balls in a contacting stack; matched separated32-ball control | 33 bodies,34 children; contact rows/island size vary and must be measured, not prescribed as achieved. |
| Fast / rotating / hollow | One fast translation pair; one rotating rigid payload; one moving hollow receiver with one payload, each with its own stationary/miss control | Minimum 2 moving participants where applicable; exact speed/angular speed, geometry/child counts, clearance and constraints require model-aware S005 recipes before execution. No speed selected from an already-passing result. |
| Coupled mechanism | Existing seven-part construction: wound spring, two balls (payload and switch striker), battery, switch, delay and motor; connected and battery-disconnected control. No separate shaft part. | Exact accepted 4/3 connections retained in [powered](motor-impulse-rounding-powered.json) and [control](motor-impulse-rounding-control.json) evidence. Bodies/children/joints/finite stores must be enumerated from full assembly, not seven parts. |
| Animation-only stress | 1,64,256 registered instances; each at 0%,50%,100% active, visible and hidden variants; physics stopped and running | Count evaluated/dirty/submitted properties and occurrence starts separately. Existing native allocation sizes are targets for integrated UI scenes, not browser evidence. |
| Visual-heavy | 64 visible dynamic visual consumers then256; matched hidden consumers with identical authority | Mesh/triangle/material/upload counts and visual types frozen by S005; retaining gameplay while hidden is mandatory. |
| Lifecycle | 20 Run/Reset + Save/Load cycles, then20 level transitions; scoped smallest and coupled constructions | Record every live world/worker/listener/lease/resource count before/after; pending commands and effects included. |
| Mixed-domain | Each mandated electricity→motor→contact/friction→heat; light→heating→phase/reaction→flow; pressure→actuator→moving occluder; radiation→sensor→powered gate chain | Begin with1 source/1 receiver per chain; add1 source/64 receivers and16 sources/64 receivers where defined. Graph edges, receiver samples, path branches/material intervals, reactions and finite stores must be enumerated when those not-yet-implemented domains exist. |

No universal capacity claim follows from these populations. Missing precise later recipe fields above are explicit S005 prerequisites; S002 freezes categories/populations and measurement rules, not the later executable workload deliverable. Do not check spec003/047 complete from this table.

## Sampling, clocks and acceptance

For each device/workload/build mode, reload with recorded cache state, wait12 wall seconds after usable UI, run one unscored 10-simulated-second warm-up (10 wall seconds for stopped physics), Reset and verify exact construction. Keep warm-up and startup separately. Then take three matched active production runs and three diagnostic runs from identical source/configuration, alternating production/diagnostic order across pairs. Each active run spans at least30 simulated seconds (3,600 ticks at120Hz); keep wall duration, Run acknowledgement, first/last included tick/frame and result. Chain UI attempts only for early wins/game duration limits, retain each run identity and segment, never join intervals across Reset. Stopped-physics scenes use30 wall seconds minimum. Collect five continuous wallminutes per device/workload for sustained/thermal acceptance, with active and lifecycle segments separately labeled.

Hard observation deadline:180 wall seconds per30-simulated-second attempt. A timeout is a retained failure/partial run; inspect the same live run, do not restart invisibly. The deadline never relaxes the normal simulated/wall ratio. Keep startup, intentional pause, fault injection and lifecycle measurements separate; exclude only these predeclared non-active categories from normal-load distributions. Unrequested input, lost records, visibility change, build/test overlap or source mismatch invalidates an accepting window; preserve it and the reason. Ordinary GC/shader/solver stalls remain in active distributions.

Use nearest-rank p50/p95/p99/max and mean, raw samples, sample counts and elapsed duration per run. Require each of three runs to pass absolute gates; do not pool away a failed run. A claimed gain must exceed the full observed between-repeat range and reproduce in all matched pairs; report the range and production/diagnostic delta without subtracting instrumentation cost from production results.

| Quantity / clock | Frozen acceptance / accounting |
| --- | --- |
| Complete simulation tick |120Hz, four outer substeps; p95<=2.5ms; report p99/max, count and fraction above8.333333ms. Includes networks, checkpoint/rollback preparation and publication. Normal active simulated/wall ratio .99–1.01; no rising debt. Also report debt endpoint/max in ms. |
| Animation evaluation | Initial60Hz, p95<=2ms; p99/max and count/fraction above16.666667ms. Registered/active/evaluated/dirty counts per sample. Stopped/hidden instances must generate zero needless polling/evaluation. |
| Bridge/transport | CPU service p95<=1ms per presented frame including amortized producer publication, pack/unpack, drain/application. Attribute raw disjoint CPU service by context/frame; do not sum inclusive stages or percentile values. |
| Snapshot/sample age | Display clock minus mapped committed production time, including interpolation: physical and animation p95<=33.3ms,p99<=50ms. Record clock origins/mapping uncertainty; target uncertainty<=1ms. If unmapped or unobservable, freshness is incomplete. |
| UI/lifecycle | Actual input-to-visible response p95<=100ms; pending indication<=100ms. Measure completion time separately for Run/Reset/Load; no invented completion-time gate and no synchronous wait. |
|60Hz /90Hz | Frame p95 <=18/12 ms,p99 <=25/16.7 ms; aggregate app CPU p95<=10/7ms per displayed frame; GPU p95<=8/6ms where valid.90Hz needs actual>=90Hz display/distinct presentations. RAF alone is callback pacing. |
| Hitches/misses | Report maximum, intervals>20ms/>33.3ms and missed-refresh counts at the actual refresh mode; zero unexplained application work>50ms after warm-up. Explain and retain all long tasks, GC and shader spikes. |
| Replay/restoration | Same-build equal accepted commands at equal ticks: exact authoritative equality; exact construction Reset/save, zero stale-generation application and zero missing/duplicate reliable events. No approximate tolerance for these identity/state gates. Cross-runtime physical-law tolerances belong to each analytic model and must be frozen before its acceptance run; S002 supplies no blanket epsilon. |

Use monotonic clocks per context; simulation time is committed ticks/120, independent of RAF. Assign each disjoint CPU service interval to the display bin [previous actual presentation, current actual presentation), splitting intervals at bin boundaries and summing attributable service across contexts. Count each operation once; inclusive parent/child intervals must be de-overlapped. Retain service in bins with no presentation and startup separately instead of dropping it. CPU service comes from attributable CPU work, not elapsed worker wall spans. Existing PerformanceRecorder measures inclusive Stopwatch elapsed time and thread allocations, not CPU or GPU. Preserve its enum-typed stages/metrics and PerformanceRunId; use the existing [C# analyzer](../tools/Performance/README.md). Group by originating sample run plus recorder/world lifetime and generation, never outer batch run or tick alone. Missing generation in current telemetry is an open transport-qualification gap. Old samples stay separately labeled, unknown/duplicate/stale records reject; zero overwritten samples/counters required for acceptance.

## Allocation, copies and retention caps

Pre-measurement targets for these bounded workloads: zero routine warmed simulation/animation scratch bytes per tick/evaluation. Retained immutable outputs and transport cannot disappear from accounting. Record bytes per tick, evaluation, displayed frame and second for each producer→consumer edge, including WASM→JS and fan-out copies.

Initial payload cap 64 KiB per physical publication,64KiB per animation result; maximum two full-payload copies per edge (packing and destination materialization), with every additional fan-out edge counted independently. No undocumented copies. Maximum live owned transport payload 8 MiB across contexts; maximum 64 pending envelopes per edge, with oldest reliable item age<=50ms under normal load. Report backpressure/rejection when capacity is reached; reliable items may not silently drop. Replaceable snapshots may coalesce only with explicit accounting. maximum resident application memory 1 GiB, with per-context linear-memory capacity<=512MiB. These are new conservative engineering caps for review, not measured requirements or permission to allocate to the cap. They retain the pinned initial 256 MiB host heap; future worker heaps count toward aggregate. If a required workload cannot fit, retain failure and review a new cap before—not after—acceptance.

Reserved Wasm linear-memory capacity is reported independently of live managed/JS bytes and process-resident memory; do not sum overlapping categories. A reserved heap remaining after Reset is not by itself a retained-object leak. The 1 GiB cap is resident application memory across browser and workers; inability to attribute it keeps that gate open.

After warm-up, perform 20 complete Run/Reset+Save/Load cycles and 20 level transitions. At each quiescent post-GC checkpoint require exactly baseline live worlds/workers/listeners/leased buffers and zero outstanding old-generation resources. Maximum post-GC retained managed-byte increase: 1 MiB aggregate over baseline; no monotonically accumulating owned resources. Report before/after counts, every checkpoint, peak managed heap, JS heap, Wasm capacity and GPU allocations separately. Missing observable GC/heap/GPU accounting means that memory gate remains incomplete; zero scratch allocation does not imply zero retained memory.

## Reproduction and S002 disposition

Existing build commands, run serially and never during timing:

```sh
dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Release --no-restore
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=true --no-restore --nologo -m:1 -nr:false -p:UseSharedCompilation=false
dotnet build tools/Performance
dotnet tools/Performance/bin/Debug/net10.0/Performance.dll sparse_catalogue current-first-principles-browser.log
```

The analyzer scenario argument is a declared enum label, not verified construction. The final command requires a freshly retained First-principles diagnostic log; it is not a command executed for S002. No new scenario alias or string-based protocol is introduced here. Current enum lacks animation-only/combined/mixed-domain identities; extend it and affected boundary tests together in the assigned tooling row.

S002 source queries: inspect content first_principles.parts; WorldGeometry.CaptureConstructionBodies and WorkbenchGeometry; BallPart.CollisionEnvelope; BasketPart.Build/PhysicsResidenceSensors; TrampolinePart.Build/PhysicsCompliantSurfaces; WorkshopAnimation hint target and definition; tools/Performance/PerformanceAudit.cs. Inspect each artifact's run/configuration rather than infer counts from screenshots. Documentation links and source-count assertions are checked without rebuilding unchanged runtime.

| Criterion | Expected | Observed / verdict |
| --- | --- | --- |
| Device names/access | Distinguish available development reference, proposed unavailable physical references and unassigned classes | Explicit above; no device tier qualified. Scope pass. |
| Baseline workload counts | Exact source/captured configurations, positive/control and idle/physical/animation/combined counts |3/8 and4/13 body/child totals include workbench; variable solver/animation counts distinguished. Scope pass. |
| Budgets/units/sampling | Preserve TODO numbers; freeze windows, repetitions, clocks, missing caps before implementation | Explicit above; new memory/copy caps accepted by coordinator as provisional pre-measurement engineering targets. No budget relaxed. |
| Current evidence/failures | Compare retained measurements without claiming speedup or passing failed gates | Frame/tick gates fail;10native failures and unexpected input retained. Scope pass. |
| Typing/scope | No new untyped runtime contract; forward existing tooling when extended | Prose-only manifest; enum tooling identified, runtime untouched. Repository-wide audit open. |
| Review/publication | Independent consistency/falsification review, then scoped commit/push | Coordinator scope review accepted after corrections; publication pending. S003 cannot start before publication. |

Coordinator adversarial review corrected two factual errors before acceptance. Workshop.ShowHint toggles visibility: 60 clicks produce 30 reveals and 30 hides; 30 clicks produce 15 reveals and 15 hides. The retained coupled construction contains wound spring, two balls, battery, switch, delay and motor; it has no separate shaft part. Source and retained run payloads confirm both corrections. The coordinator accepted the unchanged existing budgets and additional provisional caps for bounded workloads, transport and retention; these caps do not establish actual capacity. Exact later recipe dependencies remain S005, runtime/device qualification stays open, and scoped publication is pending.
