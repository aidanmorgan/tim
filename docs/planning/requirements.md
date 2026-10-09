# Detailed requirements and retained history

**Requirement ledger — navigation and preservation only.** This file keeps every requirement ID, anchor, level name, element behaviour and acceptance fact; it is not a design document. The design authorities are [canonical IEEE-754 f32 game values and WASM SIMD physics](../gpu-f32-physics.md), [engine contracts](../engine-contracts.md), the [delivery workflow](../delivery-workflow.md) and the [ordered roadmap](invest/vertical-delivery.md#rolling-playable-roadmap). Where a row below names a mechanism, the authorities decide; where a numeric budget appears, it is a release-checklist item (P0-034 stage gate), not a per-slice gate.

Identity/hash requirements throughout this document follow [evidence identity and hashing](../delivery-workflow.md#evidence-identity-and-hashing): retain exact scoped proof binding and reuse established identities; they do not require repeated whole-bundle or historical-record hashing.

**Current architecture:** [Canonical IEEE-754 f32 game values and WASM SIMD physics](../gpu-f32-physics.md) define the numerical model. Current design/acceptance is self-contained; implementation and qualification status are in [TODO](../../TODO.md).

The [named verticals](invest/vertical-delivery.md) govern genuine technical dependencies and order; the former register/source-scope ledgers were deleted on 5 October 2026 and remain in git history as evidence only. Whole-subsystem completion, whole-catalogue-before-UI and all150-before-one-level ordering are not prerequisites. Every source behavior, mode, stage proof and final coverage requirement remains binding.

Stable IDs and current acceptance remain binding. [TODO](../../TODO.md) is the short live brief and [delivery workflow](../delivery-workflow.md) controls scheduling/review. Requirement rows are criteria within functional slices; archives create no additional implementation prerequisites.

### Retained migration checkpoint — evidence only

**Current delivery status:** [TODO](../../TODO.md) is the sole current brief. Source requirements describe intended behavior and acceptance, not a second implementation chronology.


<a id="worker-loop-target"></a>

### Required end state — independent physics, animation and rendering loops

This required architecture is the three-system design of the [compilation model](../gpu-f32-physics.md#compilation-model), the [general data-driven engines](../engine-contracts.md#general-data-driven-engines) and the [standing requirements](../delivery-workflow.md#standing-requirements): one generic WASM SIMD f32 physics solver over the compiled initial state, a separate compiled animation model fed one-way by committed physics results, and a main-thread renderer that only reads both. Current in-process behavior is an unfinished migration state, not evidence of worker qualification. There is one target architecture, not selectable old/new engines or an automatic single-thread fallback.

| Execution owner | Exclusive responsibilities | Clock and boundary |
| --- | --- | --- |
| Simulation Web Worker, dedicated WASM SIMD worker | One generic WASM SIMD f32 solver advancing the typed scene record compiled at Run (flat SoA tables: bodies, shapes, materials, constraints, force regions, stores, sources, sensors, network nodes across every domain); scene-independent C# host/compiler, discrete timers/controllers/commands/objectives, transaction orchestration and authoritative save handling. No element-keyed kernel, branch, equation table or update loop. | Fixed 120 Hz gameplay ticks with 480 Hz substeps. Any later change needs separate numerical/timing qualification. Publish only whole committed revisions via zero-copy triple-buffered SharedArrayBuffer pose ring. |
| Animation Web Worker, separate .NET WASM runtime | Its own compiled animation model from the same element data; C# clips, cosmetic phase/easing, UI/autonomous animation, simulation-driven visual responses and effect lifetimes | Own clock, currently 60 Hz evaluation. Consumes committed physics results one-way through timestamped observations/events. No scene nodes, physics mutation, shared mutable state or combined kernel/loop with physics. |
| Browser thread, current 2dog/Godot host | Input, construction previews, scene/resource ownership, final transform/material/UI application and WebGL rendering | Renderer at 30–60 FPS on supported devices: reads the latest committed physics pose/state and the latest animation sample, interpolates and presents; never influences physics or animation, never synchronously waits for workers. Interpolate from worker timestamps, not Godot's physics interpolation fraction. |

Rates are ordered physics (fastest, fixed tick) > animation ≥ renderer; physics always updates faster than rendering. The three domains must execute concurrently in distinct execution contexts, not merely expose three methods or async APIs on one thread. Rendering remains on the browser thread with input; an additional rendering worker for Godot/OffscreenCanvas is not required by this selected design. Revisit only with a measured remaining bottleneck and a separately reviewed host-porting task. Simulation poses are transferred via a lock-free zero-copy triple-buffered pose ring in SharedArrayBuffer, while discrete events/commands use structured messages. Worker startup, memory and interop costs still require proof.

**Ownership and timing invariants:** Compile authored typed declarations into simulation-owned state; keep Godot scene conversion at the boundary. Functional motion, contact, energy and outcomes never depend on animation/render cadence, visibility, LOD or delayed presentation. Cosmetic child transforms compose with authoritative physical parents at one selected display time. Animation evaluation and Godot property application are separate stages. Every mutable property has one writer; every transferred buffer has one current owner. Keep a bounded history with explicit acquisition/release/recycling rather than assuming the existing single-thread buffers or inbox are safe.

**Clock and overload policy:** Use monotonic elapsed time and integer simulation ticks, explicit clock-origin mapping and generation-stamped timestamps. Worker timers wake the scheduler; they do not define elapsed simulated time. Preserve unsolved debt without enlarging delta or skipping events, bound catch-up batches, yield for commands, and enter an explicit observable overload/pause state when the declared capacity is exceeded. Visibility pause/resume rebases time at acknowledged boundaries. Retain older coherent presentation when a producer is late; never render partial state or wait indefinitely. End-to-end snapshot age includes interpolation delay and transport, not just solver completion time.

**Protocol and lifecycle:** Typed command kind/result, worker role/state, message kind, clock domain and diagnostic metrics remain enums through C#, the boundary adapter and tests (compiler-checked equivalents in unavoidable JS/TypeScript). Extensible instances/assets use typed IDs. Validate schema, enum values, finite numbers, lengths, generation, sequence, base revision and target identity before mutation. Separate replaceable state from reliable occurrences/results; bounded overflow produces explicit backpressure/rejection, never silent loss. Run/pause/step/Reset/Load/save/level change/disposal require asynchronous acknowledgements. Old-generation messages cannot affect a replacement world; failed transitions preserve a coherent prior state. No implicit migration, alias or synchronous fallback.

**Research basis, not implementation evidence:** [Microsoft's independent .NET worker approach](https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-on-webworkers?view=aspnetcore-10.0), [transferable ownership](https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API/Transferable_objects), [Godot scene-thread constraints](https://docs.godotengine.org/en/stable/tutorials/performance/thread_safe_apis.html), and [the threaded .NET deputy design](https://github.com/dotnet/designs/blob/main/accepted/2023/wasm-browser-threads.md) explain the choice. The 1 October research observed the deployed Chrome page with workers/OffscreenCanvas available but crossOriginIsolated=false and SharedArrayBuffer unavailable. That capability observation is not a worker integration/performance pass.

<a id="design-acceptance"></a>

### Acceptance contract for every task and adversarial review

**Current enforcing stages:** The owner's [4 October playable-first policy](../delivery-workflow.md#playable-first) takes precedence over earlier required-now clauses in this document, additive profiles, source cards and historical dependency rows. In every slice of the [ordered roadmap](invest/vertical-delivery.md#rolling-playable-roadmap), retain local gameplay/build/schema/ordinary-lifecycle correctness while deferring detailed performance, matched baselines, exhaustive fault/cadence/device/retention tests and global CHECK-* qualification to the release checklist (P0-034 stage gate, enforced at LEGACY-0 and CAMPAIGN), not per slice. No requirement, budget or known failure is deleted; a playable checkpoint does not close full part/fixture/engine qualification. Numerical design resolves the next interaction's necessary model and oracle, not every future domain before implementation.

**Applicability:** Every open checkbox, table row, named child/mode and scope-index requirement belongs to a [functional slice with a dedicated implementation owner and distinct adversarial reviewer](../delivery-workflow.md#paired-subagent-workflow) and inherits this contract, its section's linked acceptance profile, and its own existing description/successful outcome. Profiles add requirements; they never replace the individual physical, UX, teaching or historical-research contract. Nested items inherit their parent. Completed records remain unchanged historical evidence; PERF-23/47 must give each affected previously shipped part/mode a current open requalification record. A checked historical row is not worker-architecture approval. Navigation/teaching lists cannot close an implementation item.

The [general-engine execution design](../general-engine-design.md) and [complete source composition map](general-engine-element-map.md) define how the required general systems execute and where exact source models remain unresolved; every source criterion and enforcing stage below remains binding.

**Required general engines:** Create a general data-driven physics system and a general data-driven animation system under the [engine contract](../engine-contracts.md#general-data-driven-engines). All executable physical laws and numerical physics solvers must be compiled WASM SIMD f32; C# hosts/compiles/orchestrates discrete state and events, with no bespoke per-element physical solver. The separate C# Animation worker evaluates reusable typed animation definitions. Individual per-puzzle-element solvers, catalogue-keyed equations and bespoke per-element animation evaluators/update loops are expressly forbidden. Elements supply typed data/configuration/bindings to shared capabilities; a missing capability extends the general engine in its consuming slice. Verify the affected declaration-to-execution boundary, parameter/instance variation, negative controls and unsupported-input rejection alongside existing source-specific acceptance. Domain-specialised capability kernels selected by declared data and individual artwork remain allowed; this requirement does not introduce a universal-framework prerequisite or claim current runtime compliance.

**Compile-to-state:** the player's construction is compiled at Run into one typed initial state across every physical domain, one generic WASM SIMD f32 solver advances it, and a separate animation model compiled from the same element data is fed one-way by committed physics results ([compilation model](../gpu-f32-physics.md#compilation-model)). A slice's element ships as declarations only; the reviewer confirms no element/puzzle identifier appears in solver kernel dispatches, in the solver dispatch of WorkshopPhysicsCompiler.cs, or in the animation engine's evaluator selection.

**Focused design:** Apply the [focused engineering rules](../delivery-workflow.md#focused-agentic-engineering). State material assumptions and independent expected outcomes, choose the simplest design satisfying the complete current contract, and limit changes to the selected behavior and its necessary dependency closure. Resolve a consequential unknown with a bounded discriminating experiment before dependent implementation; do not invent generic infrastructure or future features. Required error handling, typed boundaries, forward cutovers, configurable cadences and all acceptance gates remain mandatory. Record these decisions in the existing slice evidence record, not a parallel design process.

**Before implementation, instantiate a task evidence record** keyed by the existing PERF/EL/TH/RAD/IX/TX/todo anchor or a newly assigned stable typed task identity for an unanchored item. It must enumerate every named child/mode and exact expected outcome, prerequisites and removed path, producers/consumers and execution owners, affected catalogue/fixtures/content, parameters/units and numeric tolerances, positive and adversarial controls, recipes/commands and evidence paths; reference workloads and measurement thresholds are required when that slice performs optimization/qualification, not before each playable behavior. The item's own successful outcome is the oracle to operationalize: for example, an empty reservoir emits zero and finite transferred volume balances its source/sinks; a missing supply cannot produce work; an incompatible port rejects without mutation. A profile link alone is not execution evidence. An underspecified numerical model or historical candidate remains open until its model/source decision and independent oracle are recorded; do not invent a passing test around observed implementation output.

**Apply these gates at each row's declared [stage](../delivery-workflow.md#stage-gates).** Required-now assertions pass; every deferred gate has a named owner. Plans and intermediate measurements cannot claim later runtime qualification. The release-checklist gates (P0-034/P0-035 at LEGACY-0, S847 at CAMPAIGN) require the full applicable conjunction:

1. **Coverage and ownership:** zero unassigned required children/modes/callers, zero scene-object references or live scene reads in worker contracts, zero direct UI mutation of simulation, zero per-part replacement solvers/cosmetic schedulers. Compile every affected caller. List allowed construction-only scene writes and prove they cannot run against simulation-owned state.
2. **Correctness and negative controls:** assert the item's player-observable output/trajectory/state and typed connections inside the [game-grade envelope](../gpu-f32-physics.md#game-grade-envelope); residuals clamp or continue and never fault a tick. Cover unsupported inputs, ordering and failure atomicity. Compare to an analytic/independent oracle, not a second call through the same algorithm. Same construction plus same inputs on the same machine gives the same outcome regardless of presentation cadence; cross-device comparison is outcome-based inside the envelope, never bitwise.
3. **Independent scheduling:** preserve paused-physics/active-animation and animation-independent physical results in affected ordinary-use checks. Release checklist (P0-034 stage gate), not a per-slice gate: full cadence/stall qualification covers animation and render cadences that differ from the defaults, physics paused/running, skipped frames and delayed consumers. Use synthetic cadence tests for unavailable display rates and label them; they do not qualify actual browser FPS. Cosmetic/UI animation runs while physics is paused; disabling animation leaves physical results unchanged.
4. **Lifecycle and transport:** assert exact current-schema construction restoration, coherent authoritative save/reload, cancellation/reseeding and zero stale-generation application under pending commands/events, delayed buffers and Reset/Load/level changes. Test invalid enum/message values and resource disposal; retain all failed attempts.
5. **Behaviour in the real UI:** actual Chrome via Playwright construction, mode/port choices and connections for each affected element/mode, positive behaviour plus a meaningful matched control and integration; exact Run/Reset/save assertions, continuous motion/audio evidence where relevant. Read-only diagnostics may observe state. No setters, imported solutions or numeric placement menus. Diagnostic fault injection may delay transport/scheduling only; it must not fabricate gameplay success and must be absent from production.
6. **Measured performance:** Release checklist (P0-034 stage gate), not a per-slice gate: at P0-034 and applicable optimization/qualified-release stages, pass the applicable predeclared workload/budget gates below with production Release measurements and paired diagnostic attribution; no improvement claim from native timings alone, screenshots, idle windows or renamed instrumentation.
7. **Review and publication:** the distinct adversarial review subagent independently verifies every required-now criterion and the justified direct/transitive impact scope. Retain both agent/session identities, source revision plus dirty-diff/artifact hashes, environment, exact reviewer-run commands/recipes, raw outputs, terminal failures, distributions/captures, impact matrix and reviewer findings. Every criterion maps to an assertion and evidence result; absent/stale evidence is incomplete. Resolve reproduced regressions, remove superseded callers/content/tooling without compatibility paths, and record commit/push verification. Publish each independently verified new element before starting the next.

**Adversarial verdict record:** For each criterion record expected result/threshold, observed value, evidence location, tested revision, attack/control and pass/fail/incomplete verdict. Enumerate applicability explicitly; a justified nonapplicable check must cite the absent dependency and cannot waive behaviour, enum correctness or delivery. Documentation/research-only items need source/coverage/link/consistency verification and must not claim runtime proof; runtime-affecting items need their required-now runtime gates, with deferred qualification explicitly owned. Review must attempt affected falsification now: wrong port/supply, repeated/stale input, ordinary Reset and invalid Load rather than only the happy path. Full producer/consumer stall, saturation, hidden/muted and injected-delay campaigns retain their P0-032/034/035 qualification owners; a reproduced ordinary-use defect needs its local resolving check now. A separate independent adversarial review subagent is mandatory under REQ-14; the owner's requirement explicitly authorizes this delegation. Its verdict follows the paired-subagent workflow, including second-order impact investigation, current snapshot identity and independent re-review after fixes.

<a id="worker-performance-budgets"></a>

### Measurable worker-loop and release gates

**Release checklist (P0-034 stage gate), not a per-slice gate.** These are acceptance targets, **not achieved measurements**, enforced at the roadmap's LEGACY-0 and CAMPAIGN gates. The current severe stalls remain failures. PERF-39 instantiates exact scenes, counts, rates and device tiers before comparison. A standing-target relaxation requires an explicit owner instruction; freeze workload-specific measurement details before the accepting run. Never lower a budget, drop a supported tier or omit a stall to pass. Missing device access or an unresolved threshold keeps that tier open.

| Measurement | Required threshold / method |
| --- | --- |
| Complete simulation tick, including networks/rollback preparation/publication | p95 <=2.5 ms at fixed 120 Hz; publish p99/max, deadline misses above 8.333 ms, substep/iteration counts and simulated/wall ratio. In normal sustained active windows ratio 0.99–1.01 with no growing debt; exclude and separately report startup, intentional pause and injected faults. |
| Animation worker evaluation | Initial 60 Hz, p95 <=2 ms per evaluation at the declared active-instance workload; record p99/max and missed 16.667 ms deadlines. Compare registered versus active/dirty instances and prove stopped/hidden work does not poll needlessly. |
| Transport and bridge | Initial p95 <=1 ms of CPU service attributable per presented frame, including amortized publication, packing/unpacking, drain and application; measure each context separately and aggregate raw attributable service, not a sum of percentiles. Report transferred/copied bytes, queue high-water/age and allocations; WASM-to-JS copies are included. |
| Freshness and response | Normal-load physical snapshot age at display p95 <=33.3 ms, p99 <=50 ms, including intentional interpolation delay; record animation sample age too with the same initial bounds. Actual pointer/key-to-visible feedback p95 <=100 ms. Lifecycle completion time is separately measured; pending indication <=100 ms and no synchronous UI wait. |
| 60 Hz presentation | Across each full normal active capture >=59.4 distinct frames/s and <=1% missed scheduled cadence slots (1% tolerance); p95 frame interval <=18 ms, p99 <=25 ms; aggregate application CPU service <=10 ms per displayed frame at p95, browser-thread CPU separately reported, GPU p95 <=8 ms where valid timing is available. Report missed refreshes, maxima and long tasks. |
| Sustained frame pacing | Renderer target 30–60 FPS on supported devices: no window longer than 1 s during a full normal active 30 s Run presents fewer than 30 distinct frames/s. Count actual distinct presentations; synthetic callbacks do not prove FPS. |
| Allocation and memory | Zero routine warmed simulation/animation scratch allocations at stable topology; enumerate unavoidable managed/transport copies with explicit byte-per-tick/frame and heap caps in the manifest. No growth in live worlds/workers/listeners/leased buffers after 20 Run/Reset + Save/Load cycles and 20 level transitions; post-GC retained-byte tolerance and peak memory cap declared before measurement. Missing memory/GC observability is an open limitation, not zero. |
| Sampling and noise | Use the [runnable measurement contract](#measurement-contract): >=3 matched complete actual-UI attempts per workload, 3600-tick timeout cases and explicitly identified early-goal cases, plus >=5 wall-clock minutes of uninterrupted recorded thermal-session timeline per device. Report each active interval and every lifecycle/gap interval separately; never splice runs into sustained FPS. Matched comparisons freeze warm-up, topology, algorithms, controls and noise before runs. Missing minimum observations remain Incomplete. |
| Workload coverage | Exact idle, sparse, doubled-sparse, dense-contact, fast/rotating/hollow, coupled-mechanism, animation-only, visual-heavy, lifecycle and mixed-domain recipes. Record bodies/children/constraints/edges/sources/receivers/events and active animations. Per-element additions carry incremental cost and relevant stress/control evidence; no universal capacity claim from one scene. |
| Device/environment identity | SDK/runtime/2dog versions, source and bundle hashes, browser/OS/device, power/thermals, viewport/DPR/backing buffer, display refresh, server headers and worker capabilities. M4 Max evidence is not integrated-GPU or physical Pixel 8 Pro/mobile qualification. Chrome/Playwright is the mandatory project browser; additional supported-device testing supplements it. |

Concurrent CPU service and elapsed critical-path time are different measurements; do not add overlapping wall spans or p95 values to claim FPS. GPU-unavailable, physical-device-unavailable and synthetic cadence results remain explicitly qualified limitations.

<a id="measurement-contract"></a>

**Runnable measurement contract — release checklist (P0-034 stage gate), not a per-slice gate. P0-003 defines; CHECK-METRICS/CHECK-AGGREGATE enforce; every measurement and P0-034 consume.** The current [Workshop Run](../../ui/Workshop.cs) stops at 3600 ticks / 30 simulated seconds, and goals may stop it earlier. Wall time is not simulated time. Do not change the timeout, disable goals, inject state or add a hidden benchmark mode to obtain a longer application run.

1. Freeze exact UI recipes, declared workload counts, expected terminal outcome, minimum usable active ticks/wall duration and sample count before measurement. Use at least three matched complete actual-UI attempts per workload. Timeout qualification recipes must reach the existing 3600-tick endpoint; expected early-goal cases retain their own complete shorter windows and minimum observations, and cannot satisfy the 30-simulated-second timeout case. Warm for at least 10 wall-clock seconds through preceding complete attempts; retain warm-up, then start measurement on a fresh Run. A warm-up within a 30-second attempt cannot also be counted as a fresh full-length capture. Unexpected early exit, insufficient observations or missing identity is Incomplete or Fail according to the declared oracle, never a pass from pooled partial attempts.
2. Capture each Run from acknowledged start to its actual stop, retaining startup and stop boundaries. Assign typed attempt/phase identities and record monotonic wall timestamps, applied start/end ticks, clock origin/mapping and terminal reason; never infer a Run boundary from FPS alone. Apply active-window FPS, frame tails, tick cost and simulated/wall ratio to each qualifying full active interval. Separately report build/Reset/save/startup/idle transitions and their costs without deleting them from raw evidence. Keep failed, slow and early-ended attempts. A short success proves only its observed window; it does not prove 30 seconds of sustained active workload.
3. Thermal qualification records at least five continuous wall-clock minutes of repeatable real-UI Run/Reset cycles per device with the same workload and unchanged game rules. Preserve the complete timeline, every gap/reset/startup/active boundary, temperatures/power where observable, per-attempt metrics and first-versus-last active performance. Record active/idle/lifecycle durations and active duty fraction; P0-003 freezes workload-specific minimum active engagement/duty, repetition recipe and maximum allowed gaps before capture, so early wins or frequent resets cannot turn a heavy-load qualification into an idle pass; a cooling break, hidden tab or missing span cannot be silently removed. Evaluate active budgets per attempt and lifecycle budgets on their own intervals. The session can establish repeated-use thermal behavior; never concatenate active frames, average per-run FPS or divide frames by summed active seconds and call that uninterrupted sustained FPS.
4. A standalone kernel/worker harness may run longer only with a separately labelled duration, workload and build identity; it is not Workshop, actual UI evidence, display FPS or production-origin qualification. Match semantics and replay at equal applied tick/phase/order. No harness result replaces mandatory real-UI, lifecycle, device or complete application budgets.
5. For a claimed optimization, compare matched current before/after snapshots on the same hardware/browser/power/viewport/refresh, workload/actions, worker topology, solver algorithms, serialization and build/diagnostic settings except the declared changed variable. Retain at least three paired complete attempts, predeclared randomized pair order, repeat controls and a noise/uncertainty rule calculated across attempts, not correlated ticks treated as independent trials. Record whole-tick and changed-stage p50/p95/p99/max, allocation/GC, solver work, copied bytes/freshness, actual distinct presentations, startup and peak/retained memory as applicable. If extraction and storage/algorithm changes cannot be isolated, report combined change only; no attribution of worker gains to storage. A gain must exceed the frozen uncertainty threshold while all required-now correctness/non-regression gates pass. Preserve full-system costs and diagnostic overhead; a faster local stage with worse required end-to-end behavior is not a pass. Bind all comparisons to the [stage budget policy](../delivery-workflow.md#stage-gates).

<a id="accept-performance"></a>

**PERF profile — shared infrastructure and optimization.** Playable infrastructure changes require current correctness/ownership/build proof and retain available failures; the following measurement requirements enforce optimization/qualification work, not every playable slice. Apply the [runnable measurement contract](#measurement-contract) and stage budget policy, all common gates plus the task's named counter/algorithm invariant, before/after matched workload and worker-ownership audit. Measure the complete affected stage including bridge costs; enforce absolute budgets at P0-034 and later enforcing workload/device gates; intermediate increments pass their scoped invariant and show claimed gains exceed predeclared noise. Require zero unaccounted allocations/copies/callers in the scoped audit. Conditional AOT/SIMD/art experiments may close with a recorded measured rejection; mandatory worker separation, correctness and qualification cannot. Shared changes enumerate every affected part/mode and invalidate stale proof.

<a id="accept-simulation"></a>

**SIM profile — laws, geometry, processes and solver integration.** Physics exclusively owns functional state, queries, clocks and finite stores. Specify inputs/units, valid ranges, event ordering, conservation/error bounds and independently calculated outputs for the task's law/geometry. Test affected zero/disconnected/blocked/threshold cases, declaration reorder and failed-tick atomicity now; scene/animation independence remains required. Full same-tick replay across all presentation cadences and saturation campaigns enforce P0-032/035. Each IX process is independently proved, then integrated with its consuming parts. Preserve no unused-domain execution; benchmark sparse/doubled/dense cost at P0-034; changes affecting UI parts also inherit ELEMENT.

<a id="accept-element"></a>

**ELEMENT profile — every catalogue part, fixture, variant and mode.** Instantiate this item's named behaviour and successful outcome separately for every mode/material/size that changes its contract. Map each functional property to simulation, each cosmetic to animation, each scene write to the renderer; give typed ports/IDs, source/sink limits, parameter boundaries and observable positive/control assertions. Real-UI construct/connect/run/control/Reset/save evidence must identify the exact part/configuration, not just a family. Check affected ordinary activation/Reset and physical/cosmetic alignment now. Full cadence/hidden-animation, sample-age and incremental/stress measurement enforce P0-032/034/035. Preserve DESIGN.md and individual commit/push. Unknown historical semantics need a recorded model/source decision before implementation.

<a id="accept-interaction"></a>

**INTERACTION profile — connections and cross-domain scenarios.** Inherit SIM and ELEMENT for every participant. Enumerate the complete required endpoint/domain/role/direction/mode/capacity matrix, including invalid combinations; no representative-only closure. Assert accepted graph identities and observable flow/control, and zero mutation/energy on rejected links. Declare coupled transfer/event timing and numerical residual bounds; compare supply to total delivery/loss and prove no duplicate/missing events across consumer delays, reordered/repeated messages and Reset. Each TX/chain has its own positive and disconnected/blocked/wrong-channel control with current UI evidence; detailed queue/cost metrics enforce qualification.

<a id="accept-ui"></a>

**UI profile — placement, editing, lifecycle and feedback.** Input issues typed ordered commands; previews never mutate authority, and admission never masquerades as application. Test mouse/touch/keyboard where supported, exact target/port selection, rapid/repeated input and rejected/stale acknowledgements, with ordinary transport now; the 0/100/250 ms injected-delay and worker-failure matrix enforces P0-032. Require zero unintended Run/Reset/duplicate edits, exact construction/Undo/save restoration and acknowledged Run/Reset/Load/navigation boundaries. Pending/feedback p95 <=100 ms is the unchanged supported-load qualification target; ordinary visibly responsive controls remain required now; stalled simulation must not block input processing. UI animation continues with physics paused; show no stale-generation frame after transition acknowledgement. Retained intermittent defects need a reproducible cause/fix or an explicitly unresolved verdict, not a passing retry.

<a id="accept-content"></a>

**CONTENT profile — campaign, teaching, help and progression.** Enumerate every named level/objective/mode and map it to implemented worker-compatible parts and current individual evidence. Verify unlock/navigation/save and goal outcomes from authoritative results, independent of render/animation rate; labels/help cannot select behaviour. Each changed lesson needs a real-UI reference, a meaningful near/wrong-route control, exact Reset/save; workload-budget qualification remains at its named phase. Teaching/comfort claims require predefined questions, participant counts and pass thresholds with retained feedback, not a solver win. Preserve 150 levels, explicit introductions/practice/reuse and no untaught prerequisites; the full 1,800-cell matrix and exhaustive difficulty sweeps remain deferred until component coverage, never the changed lesson's focused proof.

<a id="accept-review"></a>

**REVIEW profile — research, audit, tooling, delivery and qualification.** Record a finite expected inventory and compare it with actual discovered records: zero orphan tasks, modes, sources, callers, protocol mappings or missing evidence cells. Tooling must reject undefined enums, malformed/stale records and false passes; test the auditor against a deliberately failing/missing/stale fixture, and retain the rejection. Research distinguishes primary observation, proposed game rule and unresolved claim with exact source/version. Documentation-only edits verify links, scope preservation and active-design consistency; no browser benchmark is implied. Qualification tasks require all applicable profiles and raw evidence; a report or approved plan alone cannot complete runtime work.

**Profile routing is exhaustive and additive:** Section markers below apply to every item until the next section marker. PERF tasks use PERF; other physics/process tasks use SIM; controls/readability use UI; connection matrices use INTERACTION; existing/new parts including RAD/TH/EL use ELEMENT; integration rows and TX use INTERACTION; IX uses SIM; teaching/progression uses CONTENT; preparation/delivery/device qualification uses REVIEW. An item's explicit physics, UI, integration, research or content obligations also invoke those profiles even if its primary section is different. No task is exempt because it lacks a PERF or element ID.

**Cross-cutting acceptance:** The following requirements refine current worker/component slices; they are not separate chronological handoff work.

| Existing obligation | Remaining information or action | Completion rule |
| --- | --- | --- |
| PERF-23/24/34/37 — migration coverage | Maintain one current consumer ledger for physical poses/queries, commands, telemetry, discrete events, autonomous/UI animation and physical feedback. Record concrete producer/consumer symbols, old path to remove, affected parts/modes and evidence revision. Reconcile overlapping “current” checkpoints against source; keep their original logs as historical evidence. | Every current caller accounted for; no residual direct physical mutation, live solver read for presentation, or part-local cosmetic update path hidden by a representative pass. Construction-only writes remain explicitly classified. |
| PERF-32/33/38 — acoustic event integration | Bind each committed occurrence through typed source/target identities to the existing shared motion/wavefront capabilities. Specify fan-out admission, acknowledgement, overlapping effects, audio failure semantics, bounded capacity/backpressure, hidden targets, reduced motion and Reset/Load cancellation before removing callbacks. | Positive, control, failed-tick, saturation and lifecycle cases pass; no lost occurrence, double admission or partial fan-out on retry. Cosmetic animation changes leave authoritative results unchanged. Preserve the documented uncertain-audio-delivery policy; do not silently replay an external effect. |
| PERF-02/35/38 — integrated performance | Turn isolated warmed 1/64/256-instance allocation checks into reproducible end-to-end workloads covering source capture, publication, consumer drain, animation evaluation and scene submission. Record active versus registered instances, changed versus submitted properties, copy/allocation bytes, event rate/backlog and stage timings. Define scene size, capture duration and pass thresholds before comparison. | Native cadence equality proves independence, not FPS. Browser frame pacing and integrated costs must satisfy the declared renderer 30–60 FPS workload budgets (release checklist, P0-034); isolated zero-allocation results cannot close whole-pipeline allocation or performance gates. |
| PERF-15/16/27 — evidence and delivery | Run each affected part's focused UI proof during the migration and track implementation, native proof, current-bundle UI proof, performance and commit/push separately. Give each blocked result a concrete external prerequisite and next retry condition. | No part/milestone closes from native tests alone; aggregate qualification never postpones focused per-part proof. Do not begin another new element before publishing the individually verified one. |

**Current bridge and controller acceptance:** Apply the complete [engine ownership/lifecycle/protocol](../engine-contracts.md), [presentation binding](../presentation-bindings.md) and [control/sensor](../simulation-controls.md) contracts. Typed Boolean, scalar, enum, RGB, timer/counter and electrical input observations publish one coherent generation/revision; queries, poses, rope endpoints, reference frames, acoustic motion/wavefronts and optical artwork use committed state at the required common time. Failed ticks publish neither partial state nor events. Whole-fan-out capacity must be admitted before acknowledging an occurrence. Clock pulse overlap, hidden first-visible retention, source identities, reliable age and exact generation cancellation remain explicit controls.

Prepare every configuration/Load candidate, internal body, assistance value and generation before committing; reject malformed/missing/unknown typed parameters, reentrancy and partial capture without changing the prior construction. Binary and scalar runtime controls pass through the ordered transactional inbox; supported motor-speed controls cover 0–20 rad/s under current canonical f32 admission. Direct parameter dictionaries and displayed poses are not authority. Qualify full-capacity Reset, runtime-input ordering, changed topology, failed Load, resource leaks, acoustic recipient failure, pressure/trace allocation and exact construction save. Current status, defects and measured failures live in TODO and the owning slice record, not an obsolete checkpoint queue.

**Animation implementation detail required by PERF-36–38:** Use a central active-instance registry and reusable batched storage, typed clip/procedural definitions and target bindings, explicit start/stop/removal and generation invalidation, and declared continuous-session versus paused-world projections of the [single master clock](../shared-clock-cadence.md). Resolve property ownership when binding; reject conflicting writers. Evaluate eligible animation in its independent worker at its configured master-derived pulse cadence; its native clock is only a calibrated adapter. At render cadence, sample timestamped results, compose with physical parents at the common display time and submit dirty properties before drawing; rendering never schedules animation evaluation. No task/timer/node callback per cosmetic instance solely to advance it; no physics tick or pose stream solely for motor artwork. Keep essential physical feedback while suspending eligible invisible cosmetic evaluation; resume by the declared clock policy, without replaying missed frames.

**Performance acceptance detail:** In A, pin workload sizes, warm-up/capture durations and stable-topology conditions before comparing changes. Include physics-only headless checks, UI/cosmetic-only animation with physics stopped, combined simulation/animation, steady idle, active offscreen-to-visible transitions, tick bursts and Reset/Load. Compare animation on/off physical results for equality, while measuring actual browser CPU/GPU/frame pacing for performance. Account for animation explicitly inside total CPU/frame budgets, not outside the bridge budget or as an additional free allowance. Require no routine steady-state allocation in the bridge/animation hot path after warm-up; report topology/event bursts separately. Proposed budgets remain unqualified until measured; missing hardware blocks only its evidence gate.

**Autonomous readiness:** Each stable work order needs a current record of prerequisites, affected code/contracts, exact verification commands/recipes, quantitative acceptance criteria, evidence revision and next action. Populate these records just before the slice starts using the execution-preparation tasks below; do not spend the entire implementation phase expanding the backlog. The architecture is coherent, but open model choices, absent workload manifests and missing evidence cannot be treated as already resolved.

<a id="autonomous-execution"></a>

### Milestone definitions and retained preparation specifications

**Design acceptance for every item below:** [common gates](#design-acceptance) + [REVIEW](#accept-review); additional profiles apply to cross-cutting requirements.

**Review conclusion:** The architecture and feature inventory define the intended product, but the functional-slice workflow and technical dependencies now control readiness. Complete its preparation steps before dependent implementation; task-specific evidence records still need to be instantiated. Do not infer that a long task description, historical checkpoint or unchecked implementation note is current proof. The owner confirmed **150 levels** on 29 September 2026; active project rules use that target, while historical evidence remains unchanged.

P0 is the highest-priority workstream, not a requirement to qualify unimplemented future parts before allowing their implementation. These milestone definitions describe completion states; the slice workflow schedules their work without waiving prerequisites:

| Milestone | Prerequisites and exit evidence | Completion meaning |
| --- | --- | --- |
| Shared replacement foundations | One authoritative production physics path for the scoped existing behaviour; forward-refactored callers/content; conservative geometry and queries; owned rollback/Reset state; scene-independent core and typed worker-ready contracts, physical-pose presenter and independently owned animation evaluator; browser instrumentation, baseline and measured costs. This intermediate milestone enables dependent slices but does not satisfy the required PERF-39–48 concurrent-worker cutover. Complete affected current-part correctness, real-UI and restoration proof. Record remaining P0 tasks explicitly. | Engine capability delivered through the roadmap's ENGINE-CORE-1/2 and element slices; P0-035 remains the release-checklist gate at LEGACY-0, not a prerequisite for the next roadmap row. |
| Component and domain integration | For each element, add its declaration data and at most the one generic capability it first needs; no solver or animation-engine change. Then its connections, artwork, animation bindings and focused teaching. Apply relevant PERF tasks and current per-part proof, then individual commit/push before the next new element. | The next element in [roadmap order](invest/vertical-delivery.md#rolling-playable-roadmap) (ELEMENT-n); exhaustive campaign sweeps remain later. |
| Generalized-engine qualification | All required elements/modes and fundamental processes implemented and mapped; affected evidence current; mandatory PERF-01–48 requirements and declared device/workload gates satisfied; obsolete implementations removed. | PERF-27 and the full physics replacement may be declared complete. |
| Complete-product qualification | Component coverage complete; 150-level teaching allocation/content ready; full campaign/difficulty and device evidence complete. | Product completion, distinct from engine completion. |

A control, connection or save/load defect that blocks mandatory current-step proof requires an explicit prerequisite inserted immediately before that step. External blockers require a recorded dependency-safe amendment in this document or TODO to proceed elsewhere; their qualification gates remain open. Never bypass failed proof with direct game-state injection, imported solutions or a compatibility path.

<a id="sequence-task-001"></a>

- [ ] **Execution preparation — canonical task and coverage ledger.** Instantiate PERF-23 and the existing per-element/connection ledgers with every required named element, fixture and supported mode from TODO and linked research. Give each record one stable identity, source links, prerequisites, affected shared processes, implementation location, acceptance evidence and next action; map duplicate historical/expanded rows to that record without deleting scope. Distinguish implementation, current proof, measured performance and commit/push status. Keep an explicit current handoff near the top of this file linking the first ready slice and its blockers; old process/session IDs and historical pass counts are not a live handoff.
<a id="sequence-task-002"></a>

- [ ] **Execution preparation — domain contracts before each domain implementation.** For every required process, record the chosen gameplay model and intentional fidelity limits, units/sign conventions, valid parameter ranges, state ownership, finite sources/sinks, coupling order, timestep/error/convergence limits, failure behaviour, and independent analytical or reference acceptance cases with numerical tolerances. Resolve speculative research alternatives into one recorded implementation decision before dependent parts are built. Future parts may be declaration-only within supported laws; a genuinely new fundamental process requires a new shared capability, not a part-specific solver. Do not promise arbitrary future physics without engine changes.
<a id="sequence-task-003"></a>

- [ ] **Execution preparation — reproducible workload and support manifest.** Complete PERF-01/02/21 with exact scene/recipe identities, object/constraint/domain counts, sparse/doubled/dense load sizes, simulation duration, viewport/DPR, build/export commands and artifact hashes. Pin tested browser/OS/device versions and support expectations, including input, memory and sustained-load limits. Distinguish the available M4 Max baseline from the proposed Pixel 8 Pro and still-required integrated-GPU laptop; device access and unmeasured tiers remain explicit external dependencies. Accept or revise proposed budgets from recorded evidence before qualification; neither “wide range of browsers” nor 30–60 FPS is an unbounded guarantee.
<a id="sequence-task-004"></a>

- [ ] **Execution preparation — current verification entry points and evidence validity.** Reconcile active build/test/export/playtest instructions with the current source and serialization schema; the Playtest README contains historical schema/count observations that require this audit. Record exact commands, real-UI recipes, independent assertions, positive/control/integration and Run/Reset/save cases, tested revision plus dirty-diff/artifact hashes, logs and retained failures for each slice. Enumerate the supported connection/mode permutations requested by the coverage plan; do not silently replace exhaustive required coverage with representative sampling. Invalidate and re-run affected proofs when a shared-law, geometry, schema or bridge change alters their behaviour. Missing/stale evidence remains incomplete.

**Choosing the next action:** [TODO](../../TODO.md) identifies the current outcome, blocker and next check. Select the next ready row of the [ordered roadmap](invest/vertical-delivery.md#rolling-playable-roadmap) under genuine technical dependencies; source numbering is not an execution dependency.

**Domain acceptance:** Implement the complete [conserved transfer](../finite-gas-foundation.md#airflow-transfer), [rotor capture](../finite-gas-foundation.md#rotary-capture), [finite store](../world-owned-energy-stores.md), [finite gas/chamber/nozzle](../finite-gas-foundation.md) and [controller/sensor](../simulation-controls.md) contracts. Current part cases include loaded/closing gate and shutter, motor/pusher/conveyor, bellows/chime and every source/receiver configuration. The default bellows transport distance cannot be waived by a stronger configured fixture. Preserve the conveyor-courier reference, composite Hold Timer lesson, all fifteen composite springboard instances, parameter editing, source/mode/process coverage, exact Reset/save, physical-device budgets and production cache/update qualification. Status and past measurements are separate records; no historical export failure or test count is an implementation prerequisite.

<a id="physics"></a>

## Priority 0 — independent worker loops and the new physics engine

**Design acceptance for every item below:** [common gates](#design-acceptance) + [SIM](#accept-simulation); additional profiles apply to cross-cutting requirements.

<a id="physics-performance"></a>

### 0.0 Browser performance — required during the physics migration

**Design acceptance for every item below:** [common gates](#design-acceptance) + [PERF](#accept-performance); additional profiles apply to cross-cutting requirements.

**Priority:** The [ordered roadmap](invest/vertical-delivery.md#rolling-playable-roadmap) delivers physics capabilities, rendering, both workers, current-consumer/generic-fixture proof and cleanup through ENGINE-CORE-1/2 and one element per slice; engine budgets are release-checklist gates (P0-034 stage gate) at LEGACY-0 and CAMPAIGN. Later catalogue/campaign rows are integration/regression audits, not deferred engine implementation.

**Plan and status:** Follow the [browser physics and rendering performance plan](../browser-physics-performance.md) and [general physics capability audit](../general-physics-capability-audit.md) for inspected code, primary research, budgets and extension requirements. Track each task's implementation, measured improvement and qualification separately; research or a partial checkpoint does not complete the migration. All identified requirements below remain part of P0. The [command/read bridge contract](../simulation-presentation-bridge.md) defines the graphics/physics separation and is also required target design, not an implemented feature.

**Execution order:** The [ordered roadmap](invest/vertical-delivery.md#rolling-playable-roadmap) controls implementation, proof and cleanup; P0-035 is the release-checklist gate at LEGACY-0. PERF identifiers are stable obligations rather than an ordering.

<a id="sequence-task-005"></a>

- [ ] **PERF-01 — establish browser baselines and release budgets.** Name an integrated-GPU laptop and physical mid-range mobile reference device; record hardware, OS/browser, thermals, power mode, viewport, DPR and backing-buffer size. Measure production Release before optimization. Start from the plan's proposed 60 Hz targets: p95 frame interval ≤18 ms, p99 ≤25 ms, p95 CPU ≤10 ms/frame, physics/gameplay ≤2.5 ms/tick at 120 Hz, GPU ≤8 ms where measurable, and interaction response ≤100 ms. Confirm or explicitly revise targets from evidence; proposals are not achieved results. The renderer targets 30–60 FPS on supported devices; PERF-21 defines the sustained-device gates. Release checklist (P0-034 stage gate), not a per-slice gate; no tier is qualified yet.

The named desktop reference is MacBook Pro M4 Max with Chrome; the proposed physical mobile reference is Pixel 8 Pro. Record exact environment and measurements before claiming a support tier.

<a id="sequence-task-006"></a>

- [ ] **PERF-02 — add typed instrumentation and reproducible scenarios.** Use enum-typed stages, metrics and scenarios, typed part/body/revision IDs, and validated serialization boundaries in C# diagnostics/Playtest tooling. Capture substeps/events, simulated versus wall time, candidate/tree/leaf counts, solver iterations, largest coupled group, snapshot/allocation bytes, memory growth, draw calls and mesh uploads. Exercise idle, sparse/doubled, dense, fast/rotating/hollow, coupled-mechanism, visual-heavy and lifecycle scenarios using real UI controls. Measure instrumentation overhead; repeat end-to-end timing without diagnostics.

<a id="sequence-task-007"></a>

- [ ] **PERF-03 — replace world all-pairs traversal.** Add a conservative persistent body-level spatial index above the compound hierarchy in PhysicsWorld; benchmark candidates and ship one implementation, deleting superseded traversal. Preserve deterministic typed pairs, filtering, prescribed feasibility and contact release. Bounds must include acceleration, rotation and offset children; invalidate after impulses, projection, geometry/participation changes and Restore. Complete PERF-03a–03d below as part of this migration foundation. Prove candidate completeness against exhaustive enumeration in tests only, plus sparse scaling and dense worst-case measurements.

<a id="sequence-task-008"></a>

- [ ] **PERF-03a — explicit render/collision geometry contract for every part.** Audit every catalogue part, fixture, supported mode and authored size; enumerate individual child records with render geometry, physics body slots, collider children/vertices, bounds, geometric error and verification status. Preserve high-fidelity artwork while declaring a separate authored/procedural collision representation derived from the same typed dimensions and local frames. Use stable typed body/child/resource identities and enum-typed representation/bound kinds through exporters, loaders, builders, runtime and tests; validate external mappings and reject unsupported assets. Generate/import collision assets during asset preparation, not by decomposing the rendered mesh each frame. Procedural parts may directly generate typed convex geometry. Mass, centre of mass and inertia remain explicit physical properties; do not infer them from decorative tessellation or culling bounds. Reuse current separate-geometry declarations rather than creating another physics authority.

<a id="sequence-task-009"></a>

- [ ] **PERF-03b — conservative sphere rejection within the spatial hierarchy.** Provide cached enclosing spheres for bodies/compound groups as cheap early rejection bounds, distinct from their actual collision geometry. Integrate them with the world AABB tree and existing compound-child hierarchy; do not replace spatial indexing with an all-pairs sphere loop. For an instantaneous query, reject only when centre distance exceeds summed enclosing radii plus query margin/numerical reserve; use overflow-safe squared comparisons where appropriate. Overlap means only a candidate, never a contact, collision normal, impulse, occupancy result or filled hollow passage. For continuous queries, reject only when the conservative separation bound over the complete requested time horizon (speculative margin |v|·dt + slop under the [game-grade envelope](../gpu-f32-physics.md#game-grade-envelope)) excludes contact, including both trajectories, acceleration and moving/rotating offset centres. A body-origin enclosing radius covers rotation only if it encloses every child at all relevant configurations. Endpoint-only checks are insufficient. Recompute/invalidate on geometry/scale/topology changes and restore coherently with rollback. Invalid bounds must fail validation; uncertain separation must continue to finer checks.

<a id="sequence-task-010"></a>

- [ ] **PERF-03c — simplify collision geometry without changing functional shape.** For each audit record, reduce unnecessary hull vertices and compound pieces with explicit surface/clearance error limits. Preserve support faces, thin walls, stops, moving blades, seams and open interiors of pipes, bends, funnels and baskets. Do not routinely replace parts with spheres or a single filled convex hull: a sphere is final collision geometry only where it faithfully describes the actual physical surface. Retain appropriate convex pieces for non-spherical parts, using the shared general narrow phase and continuous sweeps rather than per-part collision solvers. Keep physics geometry independent of visual LOD/camera distance; construction resizing and physical deformation must update geometry and bounds through owned typed commands. Provide a read-only diagnostic overlay of artwork, actual colliders and rejection bounds to expose mismatch.

<a id="sequence-task-011"></a>

- [ ] **PERF-03d — qualify hierarchy correctness and measured benefit.** Compare the candidate hierarchy against exhaustive narrow-phase/sweep checks in tests only. Cover separated spheres; overlapping spheres with separated real shapes; sphere-enclosed hollow openings that must stay passable; long thin parts; offset children; two moving bodies; acceleration; pure rotation; clear endpoints with intermediate impact; contact release; resize; disable/re-enable; failed-step rollback; and Reset/Load. Record rejection counts and CPU/allocation costs at each hierarchy stage plus surviving narrow-phase calls. Benchmark sphere rejection against AABB-only pruning on sparse, dense, elongated and hollow scenes; place/use the sphere stage only where measured savings justify its cost, without shipping selectable old/new engines. For each changed part/mode, prove actual geometry/configuration, positive contact, meaningful miss/passage controls, integration and exact restoration through real UI actions. Compile affected callers, test boundary validation, publish production and retain per-part evidence/commit/push status; no representative-only completion.

**Collision hierarchy contract:** World spatial index → conservative body/group bounds (sphere rejection where useful, tighter swept AABBs for elongated geometry) → compound-child bounds → actual convex geometry/continuous narrow phase → shared contact/constraint response. Bounds may admit extra candidates but must never reject a real contact within the query horizon. Detailed render meshes do not enter the solver. This is the project's application of [hierarchical spatial pruning](https://box2d.org/documentation/md_collision.html) and [separate collision-shape tradeoffs](https://docs.godotengine.org/en/stable/tutorials/physics/collision_shapes_3d.html); those sources do not qualify our custom 3D CCD implementation.

<a id="sequence-task-012"></a>

- [ ] **PERF-04 — remove routine hot-path allocations.** Profile CandidatePairs, CurrentPairs, AccelerationSolver, load preparation and compound-bound queries. Replace repeated arrays, dictionary construction and identical sorting with reusable world-owned buffers and dense typed indices. Define capacity, ownership and span lifetimes; preserve retained immutable results and clear retained references. Target zero routine scratch allocation after stable-topology warm-up; document remaining allocation and measured GC/frame-tail changes.

<a id="sequence-task-013"></a>

- [ ] **PERF-05 — optimize snapshots and qualify cache invalidation.** Separate reusable step rollback storage from retained save/Reset and presentation snapshots. Benchmark storage reuse/journaling while preserving full restoration of bodies, contacts, loads, ledgers, effects, clocks and events. Cache keys must include relevant pose, velocity, geometry, topology, load and horizon revisions. Prove invalidation, reentrancy rejection, exact replay and forced-failure rollback; record snapshot bytes/time.

<a id="sequence-task-014"></a>

- [ ] **PERF-06 — buffer committed presentation state and separate render updates.** Audit scene consumers before removing ScenePhysicsAssembly.Present from every outer substep. Physical consumers must read authoritative world state. Maintain bounded reusable presentation history containing committed poses and visual telemetry, with enough owned buffers for previous/current interpolation and declared consumer lag; publish/swap only after a complete successful gameplay tick, with explicit buffer ownership and no overwrite while a reader uses it. Do not copy the entire physics world per frame. Interpolate from those snapshots and apply one coherent set of visible changes before drawing, at most once per rendered frame; batch presentation writes. Qualify orientation, latency, multiple ticks per frame and no double interpolation. Reset, Load, spawn, teleport and rollback must reseed both snapshots; interpolated state must never drive physics, queries, sensors or saves. Prove that failed/intermediate steps never appear, exact Reset is preserved and buffers stop allocating after warm-up. Leave framebuffer presentation to universal WebGL 2 / WebGPU instanced draws; no extra full-frame image buffer is required for this task. Follow PERF-31–35's command/read, event, ownership and lifecycle contracts; double buffering alone does not establish a transactional or asynchronous bridge. These snapshots contain physical state only; cosmetic phase/clips/easing belong to the independent animation system in PERF-36–38 and need no physics-driven updates.

<a id="sequence-task-015"></a>

- [ ] **PERF-07 — update only dirty objects and reuse dynamic artwork.** Introduce typed revision tracking and enum-typed invalidation reasons for poses, meshes, materials, labels, ropes, belts, optical paths and light cones, including moving blockers/source changes. Recompute/upload geometry and write properties only when their dependencies change; retain independent marker/animation updates while active. Unchanged geometry may still need drawing: this is object/data invalidation, not screen-region repainting. Replace repeated CylinderMesh.Height edits with shared unit-mesh transforms where equivalent; retain deforming topology and qualify supported browser buffer updates. Prove unchanged/moving controls, clipping, signed motion, material/UI changes and Reset/Load invalidation. Record avoided rebuilds, uploads and interop writes alongside frame-time and allocation changes.

<a id="sequence-task-016"></a>

- [ ] **PERF-08 — batch artwork, control pixel cost and qualify idle-frame suppression.** Implement instanced draw batches (gl.drawElementsInstanced in WebGL 2, renderPass.drawIndexed in WebGPU) with typed part-to-instance mapping, selection and spatial culling. Share immutable geometry/materials without coupling mutable part states. Measure translucent overdraw, shadows, uploads, MSAA and high-DPI resolution independently. Any presentation settings must be explicit enums with visual proof; preserve the palette, open passages, silhouettes, sockets and interaction targets. Experiment with render-on-demand only when the workshop is genuinely visually idle, using supported browser rendering/canvas controls. Use enum-typed render activity/invalidation states; resume drawing for input/hover/selection, camera movement, simulation/interpolation, cosmetic animation, UI changes, resize/DPR changes, resource readiness and visibility/context restoration. Paused physics alone is insufficient. Preserve input/event processing and request the final settled frame. Verify idle-to-active transitions, continuous motion, Reset/Load, responsiveness, reduced draw submissions and CPU/GPU cost; record acceptance or rejection from measurements. General 3D dirty-rectangle rendering remains conditional on a demonstrated bottleneck and a separately qualified design covering old/new object bounds, revealed background, depth, shadows, transparency and camera invalidation; do not assume preserved framebuffer pixels.

<a id="sequence-task-017"></a>

- [ ] **PERF-09 — reuse coupled work and qualify island sleep.** Extend existing grouping rather than creating a duplicate graph. Account for contacts, joints, ropes, transmissions, compliant loads, airflow and shared energy; static scenery must not merge independent islands. Require island-wide equilibrium and immediate wake on contact, force/supply, motor/servo, prescribed motion, support/topology changes and energy release. Timers, sensor residence and scheduled events continue. Prove resting/disturbed stacks, taut/slack ropes, powered stalls/releases, springs and cargo. Preserve the shared event clock until independent advancement is separately qualified.

<a id="sequence-task-018"></a>

- [ ] **PERF-10 — reduce repeated solver/predictor work.** Profile grazing/slip cases, stiff springs and large coupled groups. Reuse valid contact identities, warm starts, symbolic structure and scratch storage; invalidate numeric factorizations whenever effective coefficients change. Improve conservative interval bounds before raising budgets. Preserve the approved IEEE-754 f32 numeric envelope, qualified replay, penetration limits and energy/work accounting; skipped events or looser current limits do not count as optimization.

<a id="sequence-task-019"></a>

- [ ] **PERF-11 — qualify stepping and overload handling.** Measure the current 120 Hz × four outer steps before experimenting with consolidation under the continuous solver. Preserve tick-timed signals, force/work integration and earliest events; document accepted or rejected policy changes. Implement bounded catch-up that yields to input/rendering without advancing unsolved time, explicit sustained-overload handling and committed-boundary hidden-tab pause/resume without a burst. Test 30/60/90/120 Hz presentation and visibility transitions with exact Reset.

<a id="sequence-task-020"></a>

- [ ] **PERF-12 — evaluate supported Wasm compilation improvements.** Verify the pinned plain 2dog toolchain and published output before claiming managed AOT. Compare CPU throughput against compressed payload, cold startup, memory and reflection/serialization correctness. SIMD is already enabled: inspect/vectorize measured hot loops only where useful, preserving precision and deterministic reductions. Record accepted/rejected experiments; do not infer shared-memory Task.Run, SharedArrayBuffer, WebGPU or threaded native-module support from the stock single-threaded 2dog host. Independent .NET worker runtimes are mandatory under PERF-39–48 and must be qualified directly; they are not deferred behind this conditional compilation experiment.

**Current engine acceptance:** The conditional Wasm experiment concerns host/tooling only; required WASM SIMD f32 physics and universal instanced rendering are not optional or deferred behind it.

<a id="sequence-task-021"></a>

- [ ] **PERF-13 — qualify startup delivery and memory lifetime.** Verify negotiated Brotli/gzip, Wasm MIME type, versioned caching and engine/runtime/content transfer costs in the deployed production bundle. Inspect exported content while preserving reflection roots. Measure first usable construction frame under the plan's network profile. Prove no increasing live state across at least 20 identical Run/Reset and Save/Load cycles plus level changes; report managed, Wasm and GPU resources separately. Qualify any initial-heap change on physical low-memory devices.

<a id="sequence-task-022"></a>

- [ ] **PERF-14 — retain reproducible browser/device measurements.** Capture warm-up and at least three matched active windows per scenario, distributions/maxima, simulated/wall durations and raw traces. Account for the 30-simulation-second attempt limit and early wins; stopped simulation is not active throughput. Compare gains against run-to-run noise on applicable Chrome, Firefox and physical Safari/iOS devices; emulation/WebKit is supplemental. Use asynchronous GPU timing or mark unavailable. Separate capture runs from timing; retain failures.

<a id="sequence-task-023"></a>

- [ ] **PERF-15 — refresh every affected part's behavioural and visual proof.** Use actual construction/connection controls to verify placed configuration, typed links, intended behaviour, meaningful negative/control cases and integration for each affected part/mode, including existing catalogue parts and fixtures. Prove exact Run/Reset and Save/Load restoration, boundaries and continuous motion with DESIGN.md appearance. Native tests and aggregate stress scenes supplement but never replace these proofs; missing/stale evidence remains incomplete.

<a id="sequence-task-024"></a>

- [ ] **PERF-16 — close and publish the migration performance gate.** Compile affected callers, run focused correctness checks, and publish/exercise production Release. Record revision plus dirty-diff/artifact hashes, commands, UI recipes, metrics, captures and failures per task. Review enums/typed identifiers through APIs, collections, comparisons, tests and automation; test changed canonical boundary mappings and unsupported-value rejection. Remove superseded paths without compatibility switches, shims or silent downgrades. Commit/push verified increments and each individually verified new element before the next. Completion requires measured budgets and current mandatory/affected-part evidence.

<a id="sequence-task-025"></a>

- [ ] **PERF-17 — complete domain and element capability coverage.** Extend section 0.5 and the individual element registers with per-element dependencies, interaction surfaces/volumes, stores, laws, query contracts and workload measures. Cover mechanical/elastic/granular, electrical/logic, water/hydraulic, pneumatic/wind, thermal/phase/chemical, optical, acoustic, ionizing-radiation and magnetic/electric-field requirements. Preserve each element's specified behaviour; use generic typed capabilities rather than part-pair selectors. Follow the [multi-domain performance architecture](../browser-physics-performance.md#architecture-under-measurement). Complete this contract review before finalizing PERF-03's index.

<a id="sequence-task-026"></a>

- [ ] **PERF-18 — shared spatial queries with domain-specific geometry/materials.** Extend PERF-03a–03d to separate render, contact and interaction representations under authoritative poses. Index influence regions, non-solid receivers and domain participation independently of contact flags; bounds must enclose relevant interaction geometry. Add typed nearest-hit, ordered-surface and material-interval contracts, with union handling to avoid double-counting overlapping pieces of the same solid. Preserve openings, thickness and distinct layers. Test transparent solid panes, optical filters, moving wind blockers, photon shielding thickness, inside origins, grazing boundaries and non-contact receivers. Reuse geometry only where accuracy agrees; nearest obstruction is not universal transmission.

<a id="sequence-task-027"></a>

- [ ] **PERF-19 — sparse dependency-driven execution and qualified scheduling.** Implement the [existing subsystem dependency-closure requirement](#subsystem-dependency-closure), including reachable phase/reaction/spawn products and edits, with full-enabled/pruned equivalence tests. Cache network topology and schedule changed signals/timers; do not allocate/step absent domains. Permit different domain intervals only with qualified error bounds, integrated stores and threshold/pulse detection. Moving blockers invalidate cached paths even with stationary endpoints; mechanical sleep does not suspend heat, emission, exposure or timers. No FPS-dependent reduction of physical accuracy, implicit field cutoffs or dropped work.

<a id="sequence-task-028"></a>

- [ ] **PERF-20 — conservative cross-domain coupling and atomic lifecycle.** Integrate the existing shared ledger/coupling work with performance scheduling: read consistent state, propose bounded mass/species/energy/force transfers, resolve shared supply and commit deterministically. Qualify coupled iteration/substeps for feedback, and predictor-stage continuous force evaluation where needed. Prove many consumers cannot duplicate source output and thresholds do not acquire arbitrary frame delays. Include accumulated quantities, pending events and topology changes in save/Reset/failed-step rollback. Benchmark electricity-to-mechanics-to-heat, light-to-phase/reaction-to-flow, pressure-to-moving-occlusion and radiation-to-powered-control chains.

<a id="sequence-task-029"></a>

- [ ] **PERF-21 — 30–60 FPS capacity and sustained browser matrix.** Release checklist (P0-034 stage gate), not a per-slice gate. Define measured supported workloads per device/browser class using bodies, collider children, constraints, graph edges, sources, receiver samples, path branches, material intervals and reactions. Cover low/mid-range physical Android Chrome, iOS Safari and integrated-GPU desktop Chrome/Edge, Firefox and Safari on applicable Windows/macOS/Linux systems; record versions and WebGL/Wasm capabilities. Target 30–60 FPS on supported devices using the 60 Hz budgets above. Measure independently paced 120 Hz simulation, catch-up/publication bursts, transport freshness, missed refreshes and simulated/wall time, not average FPS alone; browser rendering must not execute or await those physics ticks. Run at least five-minute repeated UI sessions for thermal effects, separating active windows and lifecycle costs; retain out-of-budget results.

<a id="sequence-task-030"></a>

- [ ] **PERF-22 — qualify combined workload and completion coverage.** Extend PERF-14–16 to every required domain, per-element mode and applicable mixed-domain chain, including many-source/many-receiver stress, long optical paths, dense constraints and connected fluid networks. Track domain implementation, focused behavioural proof, performance evidence and published revision separately; one aggregate pass cannot close unimplemented elements. Maintain explicit supported presentation quality and workload envelopes; never hide cost by disabling required interactions or reducing simulation accuracy. Keep broad campaign/difficulty sweeps deferred until component coverage while focused per-element/mixed-domain correctness, production build and browser performance proof remain mandatory.

<a id="sequence-task-031"></a>

- [ ] **PERF-23 — exhaustive element-to-process coverage manifest.** Complete the [general physics capability audit](../general-physics-capability-audit.md): reconcile current catalogue/fixtures, all 216 element, 37 thermal and 22 radiation specification anchors, variants, GAP records and linked research without treating those counts as implemented parts. Use typed identities and enum-typed capabilities/statuses; record each element/mode's laws, state/stores, geometry/query needs, controller primitives, units, validity/error limits, workload envelope, implementation symbols and native/UI/browser evidence. Track missing processes separately from missing parts and missing proof. Generate a report that rejects orphan requirements and cannot mark a family complete from one representative. These are observed counts, not a fixed limit; discovery must cover future entries.

<a id="sequence-task-032"></a>

- [ ] **PERF-24 — enforce declaration-only parts and generic law ownership.** Define and enforce solver/adapter/part dependency boundaries: no catalogue names, concrete part types, level/goal IDs or renderer state selecting physical outcomes. Audit indirect callbacks as well as engine switches; SpringPart.PhysicsImpact currently computes an impulse and cannon/controller policy needs classification. Move physical equations, stores and state transitions into generic typed laws/controllers with shared ownership, updating callers/tests and deleting superseded paths. Part files declare existing capabilities and present results; hiding bespoke physics in callbacks, tags or arbitrary serialized expressions is not compliance. Physical constants, documented numerical tolerances and validated material parameters remain legitimate. Add compiler/static architecture checks and retain an explicit unfinished repository-wide audit until verified.

<a id="sequence-task-033"></a>

- [ ] **PERF-25 — compile declarations into efficient execution plans.** Resolve typed capabilities/handles, immutable geometry, graph topology, sparse patterns and dependency closure at load or relevant revision changes. Profile compact/contiguous storage, reusable bounded scratch arenas, local sparse/dense numerical kernels and independent SIMD batches in the actual Wasm build. Forbid per-part physics/evaluator callback dispatch on every execution path; cold authoring interfaces may emit typed declarations only. Avoid repeated reflection, topology discovery and world-wide dense solves in hot paths. Use canonical IEEE-754 f32 declarations and WASM SIMD f32 numerical kernels, canonical ordering, bounded solver budgets that take the best iterate under the [game-grade envelope](../gpu-f32-physics.md#game-grade-envelope), cache invalidation and atomic rollback. Use the audit's research/applicability register to accept or reject each technique from measured browser gains; no hidden CPU numerical fallback or unapproved changes to the f32 limits.

<a id="sequence-task-034"></a>

- [ ] **PERF-26 — prove new parts need no new physics code for supported processes.** For every generic process, compose multiple differently named/shaped/materialized elements and an unfamiliar assembly using only typed declarations, presentation assets and tests; retain the file diff proving no law/solver edits and no physical equations added to part scripts. Verify substitution, reordered declarations, unsupported-model rejection, analytic/conservation expectations and applicable cross-domain interactions. Re-run individual real-UI positive/control/integration, typed-link and exact Reset/save proofs. A genuinely new process may add a reusable law with its own qualification; never promise arbitrary future physics without extension or use that distinction to omit any currently required process.

<a id="sequence-task-035"></a>

- [ ] **PERF-27 — generalized-engine completion gate and regression enforcement.** Close PERF-23–26 together with all mandatory PERF-01–48 work before claiming complete generalized coverage: every required process implemented, every required element/mode mapped, no residual bespoke part physics, all affected proofs current, and 30–60 FPS evidence within explicit browser/device/workload envelopes (release checklist, P0-034 stage gate). Distinguish same-build exact replay from qualified cross-browser numerical tolerance. Add maintained architecture/coverage/performance regression checks with noise-aware thresholds and retained failures. Track implementation, proof, performance and published revision independently; preserve focused per-element commit/push and broad component-first integration. Include bridge ordering, event retention, generation barriers and renderer-independent ownership in the completion gate.

<a id="sequence-task-036"></a>

- [ ] **PERF-28 — render-detail budgets and rigid artwork merging.** Audit procedural/art assets by projected size, triangle/surface count and actual draw cost. Qualify simpler decorative cylinders/spheres/rings and visual LOD with stable transitions; physics/contact/interaction geometry must not change with the camera. Merge same-material decorative pieces that always move together where measured savings justify it, preserving separate moving mechanisms, typed selection ownership, culling and bounds. Prove silhouettes, sockets, open passages, camera extremes and the approved palette remain readable; record triangle/draw-call and browser frame-tail improvements.

<a id="sequence-task-037"></a>

- [ ] **PERF-29 — shared art resources and first-use shader qualification.** Cache immutable unit meshes and materials by typed resource/parameter identities instead of allocating an identical material for every PartArt mesh; keep per-instance mutable appearance separate. Measure creation/upload costs, lifecycle resource retention and the combined result with explicit instancing. Reduce unnecessary shader/material variants and warm up representative required materials/effects during loading using actual rendered coverage qualified in Compatibility/WebGL 2; merely loading assets or enabling Forward+/Mobile pipeline baking is not proof. Compare cold first-use hitches, loading time and memory in production browsers; do not trade unbounded startup for warm-up.

<a id="sequence-task-038"></a>

- [ ] **PERF-30 — skip invisible cosmetic work safely.** Suspend offscreen or fully hidden decorative animation/geometry preparation when its output has no visible, shadow, reflection or interaction dependency. Retain authoritative physics, sources, receivers, stored quantities and scheduled events regardless of visibility. Restore current appearance immediately on re-entry without replaying missed cosmetic frames or changing simulation. Verify camera pans, shadow-producing offscreen parts, selected overlays, hidden/visible transitions and Reset/Load; measure avoided CPU/mesh work and prove no stale frame or missed gameplay response.

<a id="sequence-task-039"></a>

- [ ] **PERF-31 — implement typed command admission and deterministic application.** Adopt the [simulation–presentation bridge design](../simulation-presentation-bridge.md). Use enum-typed commands/results and typed entity/command/generation/revision IDs; validate at admission and again against authoritative state at application. Define construction/runtime/lifecycle boundaries, deterministic sequence, duplicate/stale rejection, pending cancellation and explicit command acknowledgements. Preserve dependent command order; coalesce only explicitly replaceable pending intent, never pulses or lifecycle operations. Ghost previews remain nonauthoritative. Queue admission is not successful application.

<a id="sequence-task-040"></a>

- [ ] **PERF-32 — publish coherent read state and reliable committed events.** Extend PERF-06 with renderer-independent compact snapshots, topology revisions and unit-bearing domain telemetry, using reusable previous/current storage and explicit reader ownership. Publish only whole successful gameplay transactions. Separate replaceable pose state from ordered impacts, pulses, removals and command results; retain required occurrences across skipped render frames. Validate delta base revisions or rebuild complete state; never expose mutable solver objects or expired pooled spans. Bound queue/storage capacity and preserve required output with explicit backpressure before losing committed events.

<a id="sequence-task-041"></a>

- [ ] **PERF-33 — qualify bridge transaction and lifecycle barriers.** Complete full gameplay rollback before claiming atomic publication; Idle/Stepping guards alone are insufficient. Define Run/pause/step/Reset/Load/save barriers, generation changes, invalidation outcomes for old pending commands and safe buffer reseeding. Failed Load/step preserves the previous authoritative state and emits no partial success. Save/query services read revision-stamped authoritative state, not interpolated poses. Prove reader lag, handle reuse, pending commands/events, saturation, repeated delivery and lifecycle changes between or across render frames; preserve input and Reset access during backpressure.

<a id="sequence-task-042"></a>

- [ ] **PERF-34 — migrate the generic presentation adapter and remove direct coupling.** Map typed observable capabilities/identities to presentation assets in an adapter outside the solver. Move interpolation, float conversion, dirty writes, animation, visual LOD and visibility policy into presentation; physical queries/controller laws stay authoritative. Replace ScenePhysicsAssembly.Present/body-slot callbacks and direct UI physical mutation through forward migration, with no old/new bridge modes. Enforce no scene node, concrete part, catalogue/level ID or renderer dependency in solver/law contracts. The in-process single-thread implementation is migration input only. Deliver the independent simulation/animation workers and browser-thread presenter under PERF-39–48; asynchronous APIs alone are not parallel execution. Remove the superseded production scheduler after cutover and prove distinct execution contexts with current Chrome traces.

<a id="sequence-task-043"></a>

- [ ] **PERF-35 — measure and prove bridge correctness/performance.** Release checklist (P0-034 stage gate), not a per-slice gate. Include bridge costs within the 30–60 FPS budgets; trial p95 ≤1 ms per rendered frame for command drain, publication and presentation application at the reference workload, explicitly unmeasured until qualified. Record allocations, bytes copied/published, queue high-water marks, snapshot age, event backlog, dirty writes and input-to-visible latency, including two-tick bursts and heavy domain telemetry. Test differing presentation cadences, frames without ticks, skipped snapshots, short events, hidden-tab return and saturation on production browsers. Require architecture/native/boundary tests plus every affected part's real-UI behaviour, typed connections, exact Reset/save and motion evidence; retain failures and commit/push verified increments.

<a id="sequence-task-044"></a>

- [ ] **PERF-36 — independent animation system with typed ownership.** Add a presentation-owned animation service for clips/procedural loops, easing, transitions, clocks and completion, independent of physics stepping. Use enum-typed drive/time-projection/lifecycle modes and typed animation/target IDs; autonomous and world time derive from one continuous session master while evaluation cadence remains independently configurable. Distinguish autonomous cosmetics, UI-driven animation, optional simulation-event/state feedback and authoritative-pose presentation. Cosmetic motor spinning, indicator pulses and decorative recoil need no rigid body, solver joint, force, angular-travel ledger or physics tick just to animate. Animation may consume optional coarse state such as active/direction, or signed physical speed when meaningful, without making its visual phase authoritative. Physics must run headlessly without animation; UI/cosmetic animation must run while physics is absent or stopped.

<a id="sequence-task-045"></a>

- [ ] **PERF-37 — migrate cosmetic motion out of physics and bridge state.** Audit every part animation and document whether it depicts a functional physical pose or is cosmetic; record motor spin explicitly as an animation-system use case. Move visual phase, spin-up/coast easing, recoil envelopes, loop/clip progress and cosmetic timers into animation. Remove physics state introduced solely for visuals after dependency verification; retain actual shafts, inertia, force/work accounting and contact motion where they affect gameplay. Simulation exports only necessary physical observables/events, not cosmetic animation state. Only one presenter writes each visual property; compose cosmetic child transforms with physical parent poses without feeding them back into contact/interaction geometry. Update older visual requirements that imply every rotating mesh must be physically simulated.

<a id="sequence-task-046"></a>

- [ ] **PERF-38 — qualify animation lifecycle, independence and performance.** Define explicit policies for Run/pause/slow-step/success/Reset/Load, hidden tabs, offscreen re-entry and reduced-motion preferences; UI/autonomous animation continues on the shared master session projection when the physical world projection pauses. Cosmetic phase is not authoritative save/replay state: Reset restores the intended visual baseline and cancels stale-generation effects, while any optional presentation persistence remains separately declared. Batch active animation updates, reuse state, remove inactive work and avoid per-animation Tasks or solver events. Prove identical physical trajectories, stores, events and exact construction Reset with animation enabled, disabled and at different render rates. Verify motor/indicator motion through real UI, optional state-driven feedback, stale-event cancellation, actual contact alignment, allocation/CPU cost and continuous 30–60 FPS presentation (release checklist, P0-034) without claiming animation completion proves physics correctness.

<a id="worker-migration-tasks"></a>

#### Required worker migration tasks — release-checklist scope at LEGACY-0

These are mandatory P0 additions enforced as release-checklist gates (P0-034 stage gate), not per-slice gates and not an optional optimization experiment. The roadmap's ENGINE-CORE-1/2, ANIM-1 and element slices deliver the behaviour; these rows audit it. Each inherits [all-task acceptance](#design-acceptance) and [PERF](#accept-performance); dependencies point into the same queue. Implementation, current proof, measured performance and publication remain separately open.

<a id="sequence-task-047"></a>

- [ ] **PERF-39 — freeze the execution map, support manifest and acceptance baseline.** Queue A; integrate PERF-01/02/23. Enumerate every current simulation/animation/scene consumer and required part/mode with its target owner and removed synchronous path. Reconcile active single-thread/future-worker statements in the bridge/performance/audit docs to this decision without rewriting historical artifacts. Instantiate exact workload sizes, stage definitions, device tiers, numeric caps/tolerances and task evidence records from the gates above. **Accept:** zero unassigned consumers or orphan requirements; documented current production baseline and retained failures; each threshold has a unit, sampling rule and oracle before acceptance runs. Planning does not claim speedup.


**Current architecture acceptance:** Current authority is the canonical IEEE-754 f32 game values and WASM SIMD physics design; preserve every required device tier and mark missing WASM SIMD or SharedArrayBuffer support blocking.<a id="sequence-task-048"></a>

- [ ] **PERF-40 — extract a scene-independent simulation assembly and owned construction compiler.** Queue B; PERF-24/25/31. Move MachineWorld stepping policy, networks/timers/objectives/state and all functional part callbacks to generic WebAssembly SIMD f32 numerical capabilities and C# typed discrete runtime capabilities; keep scene capture/geometry conversions in construction adapters. Refactor Parts/Visible/Solved/UI accesses and retained external scene references out of worker execution. **Accept:** compile the core and headless tests without external scene references; architecture checks reject injected scene dependencies and per-part law selectors; exact replay/rollback and analytic controls pass for each migrated consumer. Read-only presentation never enters solver state. Prove an existing part's full UI path and publish the slice.


**Current physics acceptance:** Editor controls, property/configuration values, current resources, difficulty/nudge/goal thresholds, animation tracks, read models and fixture values use canonical IEEE-754 f32; serialized canonical f32 bits use an explicit resource adapter. Numerical laws execute on the dedicated WASM SIMD physics worker; native tests validate host/compiler and offline references only.<a id="sequence-task-049"></a>

- [ ] **PERF-41 — implement and qualify bounded typed worker transport.** Queue B; PERF-31/32/39/40. Package a standalone WebAssembly SIMD worker and minimal compiler-checked JS/TypeScript adapter; no UI engine in worker payloads. Define version/generation/sequence/revision/timestamp envelopes, owned buffer pools, command acknowledgements, reliable occurrences/results and replaceable snapshots. Worker-to-worker fan-out uses explicit copies or independently owned buffers; never transfer a detached buffer twice. **Accept:** Release checklist (P0-034 stage gate), not a per-slice gate: 10,000-message stress tests with zero missing/double-applied reliable results, duplicates/reorder/stale/malformed/unknown-enum rejection, saturated-capacity/backpressure controls and allocation/copy accounting. Integration uses real Workers and actual UI commands; fake transport tests supplement it. No shared-memory/header requirement or silent fallback.


**Current wire acceptance:** The sole forward ABI carries f32 bits and integer scale/cell metadata; reject old F64 messages. Minimal first-body worker/device bootstrap is an internal P0-007 criterion, not a dependency on completion of this whole transport task.<a id="sequence-task-050"></a>

- [ ] **PERF-42 — move complete fixed-step simulation into its dedicated worker.** Queue C; PERF-03–05/09–11/20/40/41. Remove browser-thread World.Step as an authoritative path; implement monotonic fixed-step pacing, bounded work batches, explicit overload and timestamped whole-tick publication. Preserve the 120 Hz/four-substep default and its recorded evidence, with the [admitted configurable 60/120/240 Hz commit profiles](../shared-clock-cadence.md) executing 8/4/2 canonical 480 Hz physical substeps. Preserve integer tick identity, deterministic admission/application boundaries and numerical laws; different commit rates compare at equal physical times. **Accept:** the same construction and accepted command log on the same machine gives the same outcome; independent reference trajectories land inside the [game-grade envelope](../gpu-f32-physics.md#game-grade-envelope); consumer cadences that differ from the defaults do not change events/stores. Chrome traces identify a distinct simulation worker with progress during a 250 ms renderer stall; normal workloads satisfy tick/throughput gates. Debt/overload attacks cannot skip time or falsely advance goals. Solver stalls remain failures requiring profiling, not hidden by moving threads.


**Current worker authority:** The simulation worker exclusively owns the WASM SIMD physics engine and f32 numerical state; no CPU fallback numerical domain remains at production cutover. Match exact discrete events and outcome-based envelope comparison, not obsolete C# double equality.<a id="sequence-task-051"></a>

- [ ] **PERF-43 — move batched animation evaluation into a second C# worker.** Queue D; PERF-36–38/40/41 and committed feedback from PERF-42. Extract AnimationBatch and evaluation from SceneAnimationAdapter scene application. Migrate autonomous/UI/cosmetic feedback consumers, clock mapping, reliable occurrence admission and reduced-motion policies; retain only actual property/resource application on the browser thread. **Accept:** separate animation worker visible in Chrome traces, independently progressing from the shared master timeline with physics paused/stalled; evaluation samples at the animation rate and at differing cadences match declared clip values/tolerances. Physical replay unchanged with animation disabled; no node access, per-animation Task loop, stale effects or missed reliable occurrence. Apply the animation CPU/freshness gates and each affected motor/indicator/recoil/acoustic/UI mode's continuous-motion proof.


**Current animation values:** Authored clips, curves and cosmetic game-value state use canonical IEEE-754 f32.<a id="sequence-task-052"></a>

- [ ] **PERF-44 — make the presentation render consumer independently paced and nonblocking.** Queue E; PERF-06–08/28–30/34/35/42/43. Replace physics-fraction sampling with timestamped worker histories from the SharedArrayBuffer pose ring and a bounded explicitly delayed display clock. Compose physical parents/cosmetic children coherently, batch dirty writes once per displayed frame and retain the last valid state under delay. Never join workers or wait on their fresh result to draw; maintain input/idle invalidation and universal instanced rendering (WebGL 2 / WebGPU). **Accept:** 100/250 ms producer delays leave UI/render dispatch live without torn transforms, stale generations or physics feedback; positive motion/contact controls meet declared interpolation-error and freshness bounds. Pass 30–60 FPS pacing and measured GPU/CPU/write budgets (release checklist, P0-034 stage gate).
<a id="sequence-task-053"></a>

- [ ] **PERF-45 — implement asynchronous lifecycle, save and failure barriers across all owners.** Queue C onward; PERF-05/13/31–33/41. Specify owned transition state machines and acknowledgement ordering for Run/pause/step/success/Reset/Load/save/level change/disposal; allocate a fresh generation before accepting replacement output and retire buffers safely. Reject invalid/failed Load atomically; preserve exact construction and coherent authoritative saves. Surface worker startup/crash/unresponsive failures with usable pending/error UI, bounded timeout and explicit recovery at a valid boundary; no implicit single-thread execution or duplicate restart. **Accept:** Release checklist (P0-034 stage gate), not a per-slice gate: 20 Run/Reset + Save/Load cycles and 20 level changes with no resource growth; zero old-generation application after acknowledged replacement. Reset during charge/contact/queued effects, repeated clicks, failed worker startup, in-flight save and worker crash all have asserted outcomes and current UI proof. Check known state preservation limits explicitly after a crash.


**Current lifecycle acceptance:** Construction saves store canonical f32 bits only; worker crash or context termination before/after commit has explicit fault/checkpoint/Reset behavior.<a id="sequence-task-054"></a>

- [ ] **PERF-46 — publish and measure production worker startup, transport and memory.** Queue E; PERF-12–14/21/39/41–45. Integrate independently versioned worker assets into the production export/deployment, correct subpath/MIME/compression/cache behavior and matching protocol hashes. Measure cold/warm startup, time until all required workers are ready, three runtime heaps, peak/post-GC memory, serialization/copies and worker/listener lifetime. Validate ordinary transferable-worker operation on the actual deployed origin without SharedArrayBuffer and document support limits. **Accept:** no missing/mismatched assets, stale cache pairing or false-ready screen; first interaction/startup and memory caps fixed in PERF-39 pass, 20 lifecycle cycles leak no live workers/buffers. Include worker costs in total 30–60 FPS budgets (release checklist, P0-034 stage gate) and retain production versus diagnostic comparisons.


**Current asset acceptance:** Include WASM SIMD binaries, SharedArrayBuffer pose ring memory and runtime feature identity; missing wasm-simd128 or SharedArrayBuffer is explicit and cannot silently remove a required support tier.<a id="sequence-task-055"></a>

- [ ] **PERF-47 — adversarially prove concurrency, deterministic authority and every migrated consumer.** Queue F continuously; PERF-15/23/26/35/38/42–46. Maintain exact caller-to-part/mode coverage and replay assertions; exercise producer/consumer delays (0/100/250 ms), duplicate/reordered/stale delivery, queue overflow, hidden-tab pause/resume, disposal and Reset at activation boundaries. Delay fixtures manipulate scheduling/transport only and ship in diagnostic builds only. **Accept:** real Chrome thread traces show simultaneous execution in distinct contexts; same-tick authoritative state/events/stores remain equal across the declared cadence matrix; reliable-event reconciliation has zero unexplained loss/duplication. Every affected catalogue part and fixture has current UI positive/control/integration/typed-link/Reset/save/motion proof and retained failures. Passing one mechanism or a fake adapter cannot close the ledger.


**Current deterministic replay:** Bind exact same-environment replay to runtime/features/WASM module and deterministic reductions. Cross-device physical outcomes compare inside the [game-grade envelope](../gpu-f32-physics.md#game-grade-envelope); integer event order remains exact.<a id="sequence-task-056"></a>

- [ ] **PERF-48 — enforce migration regression gates and publish the independent-loop cutover.** Queue F/G; PERF-16/21/22/27/39–47. Build maintained coverage/architecture/protocol/performance checks from the evidence manifest; inject missing/stale/wrong-owner/out-of-budget cases to prove the checker rejects them. Remove superseded in-process scheduling, scene callbacks and obsolete active docs/content/tooling with no compatibility mode. **Accept:** mandatory PERF-01–48 gates and all affected evidence pass within named support tiers; 30–60 FPS and thermal limits are reported honestly, missing devices remain incomplete, and task-specific adversarial findings are resolved. Record successful production build/deployment checks, exact committed/pushed revision and each element's publication. Foundations can ship incrementally, but native passes or an unchecked external gate cannot close full migration.

**Current engine acceptance:** Delete old CPU physical/query authority, F64 protocol readers and obsolete wide game-value content at the reviewed cutover; no mixed production backend. Every current part/mode and fixture is requalified.

**Numeric representation policy:** The owner-approved [canonical IEEE-754 f32 game values and WASM SIMD physics authority](../gpu-f32-physics.md) replace CPU/double authority across authoring, content, saves, worker messages, animation/read models and numerical kernels. Quantize once at validated input boundaries; exact integer identity/ticks and declared external API conversions remain. Apply the admission bounds, [game-grade envelope](../gpu-f32-physics.md#game-grade-envelope) and worker lifecycle contracts; no bespoke CPU physics path, wide hidden game model or automatic format migration. Actual browser qualification and whole-system measurement remain pending.

**Coverage cross-check:** PERF-01/02/14/21 cover profiling, the 30–60 FPS renderer target and real devices; PERF-03a–03d/17/18/23 cover geometry and all element/process requirements; PERF-04/05/09–12/19/20/24–26 cover allocation, snapshots, generic execution, numerical solves and coupling; PERF-06–08/28–30 cover presentation buffers, dirty updates, idle frames and render assets; PERF-13 covers startup/memory; PERF-15/16/22/27 enforce behavioural, lifecycle, architecture and publication evidence. Full 3D dirty-region repainting, AOT, SIMD batching and rate changes remain measured decisions rather than assumed wins. No closed task may hide an unimplemented process or missing per-element proof. PERF-31–35 cover typed command/read separation, bounded event delivery, generation/lifecycle barriers, forward adapter migration and bridge latency/allocation evidence. PERF-36–38 separate animation from physical simulation and prove independent execution/cadence, shared-master time projections, ownership and lifecycle. PERF-39–48 make separate C# worker runtimes, bounded transport, independently paced rendering, asynchronous lifecycle and adversarial measured cutover mandatory; logical single-thread independence is insufficient.

**Continuous constraints:** Preserve atomic failure, exact Reset, continuous translation/rotation collision, open compound geometry and physical energy/work contracts. Never drop simulation time, contacts or modes to meet a budget. Shared engine work and element coverage follow the [ordered roadmap](invest/vertical-delivery.md#rolling-playable-roadmap); component coverage (ELEMENT-n) precedes exhaustive campaign/difficulty sweeps (CAMPAIGN); focused browser correctness and Release proof remain mandatory per slice, with detailed performance at the release checklist.

<a id="physics-target"></a>

<a id="current-catalogue-closure"></a>
### Existing catalogue — explicit current-design closure

**When:** In the [ordered roadmap](invest/vertical-delivery.md#rolling-playable-roadmap), one element by declaration per slice, using the [capability and collaborator order](invest/vertical-delivery.md#existing-element-order) for genuine prerequisites; these are acceptance criteria under the existing CAT-001–CAT-072 owners, not 72 new elements or duplicate work orders. **Status: full source qualification remains Incomplete; scoped delivered interactions are recorded in TODO.** A historical completion report, implemented scene or earlier first interaction is not current qualification. All 302 current authored fixtures and every required configuration/mode remain separately identifiable in [current consumers](invest/current-consumers.md). The 104 inventory mode records include 72 Fixed placeholders and do not prove mode completeness.

**Design acceptance for every entry:** [common gates](#design-acceptance), [ELEMENT](#accept-element), [canonical IEEE-754 f32 game values and WASM SIMD physics](../gpu-f32-physics.md), [independent worker ownership](../engine-contracts.md) and [DESIGN](../../DESIGN.md). Typed C# f32 authoring, catalogue defaults, properties, saves and read models feed the sole WebAssembly SIMD f32 physics authority; integers, enums and explicit external adapters retain their correct types. No scene/CPU fallback, wider hidden game model, copied-speed mechanism or old schema alias qualifies. Decimal quantities below identify intended game parameters, not a waiver of canonical admission or the [game-grade envelope](../gpu-f32-physics.md#game-grade-envelope); acceptance is player-observable in Chrome, never an internal residual, substep history or bitwise replay. If a historical limit collapses under admission, its CAT D owner must freeze a representable model and discriminating positive/control before I; it remains Incomplete in the meantime.

For each entry, enumerate actual source/resource settings and closed enum modes, defaults, finite admissible ranges, endpoint/interior classes, invalid/unknown rejection and exact typed socket mappings before admitting that behavior. The configuration names below are existing external resource keys to forward-migrate, not permission for string domain logic. A missing control requires a normally selectable authored qualification fixture, never a setter/imported solution. Keep all variants, integrations and source fixtures; a complete first interaction is only scoped progress.

Each behavior requires actual Chrome/Playwright construction, verified placed configuration and typed connections, intended outcome plus meaningful negative and boundary controls, applicable integration, continuous motion/screenshots, exact Run/Reset and construction save/load where supported. Preserve build/revision or loaded-bundle identity, raw recipes/assertions/logs and failed attempts. Production build, failed-admission/tick atomicity, worker stop/restart/fault/generation and resource/performance checks accompany affected runtime work; full workload/device/publication gates remain at their named stages. Unchanged applicable proof may be reused under the hashing policy, not repeatedly regenerated.

Functional colliders, physical deformation, contact-bearing shafts and other gameplay geometry render the same committed simulation timestamp. Cosmetic easing/feedback runs in the independent animation worker with explicit clock/stop policy and cannot change physics. Include hidden/visible, pause/stop, duplicate/stale publication and saturation controls; hidden rendering alone does not disable participation. Failed ticks publish no state/effect; retry emits accepted occurrences exactly once, and Reset clears pending as well as accepted transient state. Original palette, icons, sockets and clear state cues remain required. These shared criteria apply even when an individual entry names only its characteristic feedback.

Acoustic entries additionally preserve immutable emission identity, origin/direction/tone/time despite later source motion; simulation-time wave propagation freezes on pause, while specified cosmetic settling follows its own clock. Late unseen essential occurrences retain first-visible feedback without replaying already shown age. Bounded oldest-first cosmetic retention/backpressure may not silently drop gameplay events. Muted/suspended/failing audio cannot erase committed state or change a solution.

Optical preview entries (Mirror, Splitter, Combiner and each RGB filter) are read-only, selected-visible idle-construction previews; exclude ghosts and hide/reject while Running, paused, solved or otherwise nonidle. Combiner merges collinear output preview paths; other elements retain separate paths. Missing/foreign/invalid ownership rejects before mutation. Preview geometry never substitutes for the physical optical snapshot.

<a id="current-cat-001"></a>

- [ ] **CAT-001 · ball.** Canonical Basketball radius/mass/restitution/drag/buoyancy must yield gravity, rolling, bounce and momentum transfer, including rest, boundary and two-height controls. Preserve workbench contact, ramp/pipe/Receiver integrations and visible stripes from committed body pose; retain R-N3 Fail until independently resolved.

  **Configuration / modes:** radius, mass, bounce, drag, buoyancy as typed Basketball material fields. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [simulation and element acceptance](#accept-simulation) and this complete entry. **Owner / collaborators:** [CAT-001 D/I/V and fixtures](invest/current-consumers.md#cat-001-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-002"></a>

- [ ] **CAT-002 · ball_detector.** A forward centre crossing emits once without changing payload momentum; rearm only after whole-ball upstream clearance. Test rotated/fast/multiple-body passes, reverse/outside/stationary/jitter and disabled participants. Arrow and fading pulse must reflect the committed crossing and Reset must clear the observation cursor.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-264) and this complete entry. **Owner / collaborators:** [CAT-002 D/I/V and fixtures](invest/current-consumers.md#cat-002-i), signal capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-003"></a>

- [ ] **CAT-003 · balloon.** Distinct low-mass, buoyant spherical body rises under the declared world pressure/gravity profile, responds to conserved airflow and contacts, and supports applicable tether interactions. Test zero-pressure/no-fan/occluded jet and heavier-body controls. Canonicalize mass, bounce, radius, buoyancy and drag; sharing BallPart must not substitute Basketball material or silently reject this required variant forever.

  **Configuration / modes:** mass, bounce, radius, buoyancy, drag. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [simulation and element acceptance](#accept-simulation) and this complete entry. **Owner / collaborators:** [CAT-003 D/I/V and fixtures](invest/current-consumers.md#cat-003-i), air capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-004"></a>

- [ ] **CAT-004 · basket.** Physical walls remain fixed while authored capture margin/speed/dwell and guide acceleration vary. Default Free workshop is margin 0.02, speed 1.5, dwell 0.35, guide zero; first_principles retains assistance knots 0/0.45/1. Named-body residence is sampled at substep endpoints in the basket's moving frame with dwell counted in ticks; a through-pass shorter than the dwell never captures. Outside/fast/disabled/leave-and-reenter controls break dwell; capture/event/halo latch once and Reset clears all. Guide eligibility uses centre-above-rim and all collider-child support heights; PickRadius/render meshes are not authority.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [simulation and element acceptance](#accept-simulation) and this complete entry. **Owner / collaborators:** [CAT-004 D/I/V and fixtures](invest/current-consumers.md#cat-004-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-005"></a>

- [ ] **CAT-005 · battery.** Enabled/disabled Supply must seed the typed electrical network, with actual contacts and cables. Disconnected, wrong-domain and source-free cycles supply nothing. Binary enable is not finite-energy qualification: before admitting powered work, the owning generic source/store design must name capacity/power/work/depletion and affected consumers explicitly; no invented constant supply or duplicate downstream energy. Show committed supply state.

  **Configuration / modes:** enabled. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-146) and this complete entry. **Owner / collaborators:** [CAT-005 D/I/V and fixtures](invest/current-consumers.md#cat-005-i), electrical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-006"></a>

- [ ] **CAT-006 · beam_combiner.** Three separately identified cream inputs route existing RGB energy to the gold output at 90% transmission per ray, counting internal travel against remaining range/interactions. Test one/two/three inputs, absent/wrong-face/blocked inputs and reordered sources; no energy duplication. Preview input/output power and path from one committed optical snapshot.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-299) and this complete entry. **Owner / collaborators:** [CAT-006 D/I/V and fixtures](invest/current-consumers.md#cat-006-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-007"></a>

- [ ] **CAT-007 · beam_shutter.** Separate electrical control opens an acceleration-limited physical gold blade; loss closes it. Blade collider and visible pose agree. Test partial travel, blocked closing, clearing/retry, power interruption, rotated mounting and a ball obstructing the Laser path. Optical blocking follows actual blade pose, not commanded open/closed state.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-292) and this complete entry. **Owner / collaborators:** [CAT-007 D/I/V and fixtures](invest/current-consumers.md#cat-007-i), actuator capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-008"></a>

- [ ] **CAT-008 · beam_splitter.** Finite two-sided splitter sends half each existing RGB component into transmitted/reflected paths. Preserve shared remaining range, 16 interactions per path and 128 total segments per emitter. Test blocked branch, repeat split below threshold, reverse incidence and opaque frame. Transparent solid pane remains physical; both preview beams match committed routing.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-294) and this complete entry. **Owner / collaborators:** [CAT-008 D/I/V and fixtures](invest/current-consumers.md#cat-008-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-009"></a>

- [ ] **CAT-009 · bell.** Real collision above 0.8 approach speed emits mass/impact-dependent omnidirectional sound, with separation rearm and bounded strongest same-tick/coalesced retriggering. Qualify Low/Mid/High tones separately; rest/gentle/depth miss/cap controls and meter integration. Damped bell artwork and procedural voice add no physical impulse.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. AcousticTone=Low, AcousticTone=Mid, AcousticTone=High. **Current acceptance:** [retained behavior](#todo-347) and this complete entry. **Owner / collaborators:** [CAT-009 D/I/V and fixtures](invest/current-consumers.md#cat-009-i), acoustic capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-010"></a>

- [ ] **CAT-010 · bellows.** Finite-mass local plate, elastic return and conserved gas/nozzle transfer convert actual inward compression into a finite jet. Force/reach/width are explicit; plate/collider/folds agree. Test side/miss, held compression without continuing emission, obstructed refill, silent return, repeated cycles, blocked jet, downstream default-distance rotor/chime and disconnected belt. The obsolete scripted stroke/ideal-force implementation is not the model.

  **Configuration / modes:** force, reach, width. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#sequence-task-312) and this complete entry. **Owner / collaborators:** [CAT-010 D/I/V and fixtures](invest/current-consumers.md#cat-010-i), air capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-011"></a>

- [ ] **CAT-011 · blue_filter.** Blue finite two-sided aperture transmits only existing blue energy through a physically solid transparent pane; opaque frame blocks. Red/green-only input and unlike stacked filters extinguish it. Qualify rotated front/back passage and committed path/blue artwork; no recolouring.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. OpticalChannel=Blue. **Current acceptance:** [retained behavior](#todo-297) and this complete entry. **Owner / collaborators:** [CAT-011 D/I/V and fixtures](invest/current-consumers.md#cat-011-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-012"></a>

- [ ] **CAT-012 · blue_receiver.** Blue front-facing absorbing receiver requires blue at threshold and at least 90% of total RGB; it switches separately supplied electricity. Test wrong colour, below threshold, backside, occlusion and missing electrical supply. Meter/indicator reflect committed readings.

  **Configuration / modes:** threshold. OpticalChannel=Blue. **Current acceptance:** [retained behavior](#todo-300) and this complete entry. **Owner / collaborators:** [CAT-012 D/I/V and fixtures](invest/current-consumers.md#cat-012-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-013"></a>

- [ ] **CAT-013 · both_gate.** And/Both has A/B control inputs, independent bottom supply and switched output. All four truth rows, supply absence, output retraction, order and reconvergence must pass; And cycles settle from unpowered least fixed point. Numbered sockets and three lamps reflect actual committed inputs/output, not logical truth without supply.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=And. **Current acceptance:** [retained behavior](#todo-362) and this complete entry. **Owner / collaborators:** [CAT-013 D/I/V and fixtures](invest/current-consumers.md#cat-013-i), electrical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-014"></a>

- [ ] **CAT-014 · bowling.** Bowling is a distinct heavy low-bounce sphere with canonical mass/bounce/radius/drag/buoyancy. Prove workbench rest/bounce, ball/box momentum transfer and Domino/lever loading against a matched Basketball control. Shared BallPart cannot inherit Basketball defaults; visible size and committed pose agree with collision. *Delivery note (CAT-014, Story 6.1; terminal scoped Pass by the independent reviewer, Murdoch, 9 Oct 2026, two passes, for the original 0.38 m Bowling ball; the 0.28 m re-tune below is covered by the Story 6.1b review): the Bowling ball is the generic `WorkshopBall` with `Kind = BowlingBall` and `BallMaterial.For(kind)` (game-scale 4 kg / 0.28 m — smaller than the Basketball by the owner decision of 9 Oct 2026 — / bounce 0.14 / drag 0.04 / buoyancy 0; friction 0.3 and bounce threshold 0.1 m/s are declared per kind, no compiler constants); `parts/catalog/bowling.tres` carries the same typed bits and `parts/BallPart.cs` reads the declared radius. Chrome proof (`tools/e2e/cat-014.test.ts`) against a matched Basketball control on identical stock Dominoes (two lanes in one Run, where mass and radius differ together; mass-only controls are in the Node harness), on bench rest/bounce and Receiver capture; lever loading waits for the CAT-034 Impact lever slice; the Domino's offset centre of mass (CAT-023c) remains its own item. ENGINE-DRAG (Story 6.1b, pending independent review) applies the declared drag and a declared rolling-resistance coefficient at every sphere contact (Bowling 0.03, Basketball 0.035, kind data like friction, not persisted in the save slot) so both struck balls come to rest in the two-lane Run.*

  **Configuration / modes:** mass, bounce, radius, buoyancy, drag. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [simulation and element acceptance](#accept-simulation) and this complete entry. **Owner / collaborators:** [CAT-014 D/I/V and fixtures](invest/current-consumers.md#cat-014-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-015"></a>

- [ ] **CAT-015 · bumper.** Radial contact redirects the actual body at the contact point using declared finite spring/work storage, not a scene-owned free impulse. Preserve strength configuration, per-body cooldown, missed/grazing/resting and repeated-return controls, isolated authored assistance and overlapping expanding gold impact rings. Model/energy decision precedes admission; a visual ring is not physical proof. Glancing spin/depth misses, stationary/separating contacts and rejected-hit absence of cosmetic pulses remain explicit controls.

  **Configuration / modes:** strength. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-157) and this complete entry. **Owner / collaborators:** [CAT-015 D/I/V and fixtures](invest/current-consumers.md#cat-015-i), elastic capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-016"></a>

- [ ] **CAT-016 · cannon.** Separate capacity/charge-power and Trigger govern a finite store starting empty; full charge and exactly one seated slow payload in the admitted size range are required. Next-tick coalesced trigger expires if rejected. Empty/partial/ambiguous/moving/blocked muzzle controls preserve identities and charge appropriately; blocked-to-clear needs a fresh trigger. Same payload launches along rotated bore preserving transverse motion, no teleport; departing guard, same/distinct-ball reload and timed feeder remain required. Recoil is committed cosmetic feedback. Eligibility uses full compound containment and both transverse support widths 0.6–0.84, not a sphere/radius selector; gaps remain empty and partial child occupancy is detected. Freeze representable seating-clearance/speed controls under f32 before implementation; old 0.002 CPU allowance is not current numeric qualification. Muzzle sweep excludes only cannon and payload. Pending/departing identity, store and shot events roll back together; rejected shots cause no recoil.

  **Configuration / modes:** capacity, charge_power. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-392) and this complete entry. **Owner / collaborators:** [CAT-016 D/I/V and fixtures](invest/current-consumers.md#cat-016-i), actuator capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-017"></a>

- [ ] **CAT-017 · clock.** Interval 0.1–12/default 1: first/restored tick follows a full powered interval; loss cancels coincident deadline with no catch-up. Preserve count/last-event and every pulse identity, max-combined overlapping feedback, hidden essential feedback on first visible frame, atomic saturation handling and queued/accepted-pulse Reset. Pendulum phase and gold flash use their declared simulation/cosmetic clocks.

  **Configuration / modes:** interval_seconds. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-273) and this complete entry. **Owner / collaborators:** [CAT-017 D/I/V and fixtures](invest/current-consumers.md#cat-017-i), signal capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-018"></a>

- [ ] **CAT-018 · clutch.** Separate coil and upstream shaft produce Open/Closing/Engaged/Opening phases; close_seconds 0.05–2/default 0.2 engages only fully closed. Finite-inertia +1 coupling cannot create energy; supply loss disconnects physical work immediately while plates finish opening. Preserve independent signed shaft motion/momentum, missing either supply, reengagement/load and rotated controls. Open routes cannot conceal invalid topology; slip/brake variants remain separate.

  **Configuration / modes:** close_seconds. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#sequence-task-310) and this complete entry. **Owner / collaborators:** [CAT-018 D/I/V and fixtures](invest/current-consumers.md#cat-018-i), rotary capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-019"></a>

- [ ] **CAT-019 · conveyor.** Length/width/surface-per-radian define actual finite-inertia driven surface, signed input/output shafts and load-aware frictional cargo transport. Test unloaded/loaded/stalled/coasting, disconnected drive, reversed ratio and downstream chained conveyor; no copied-speed work source. Physical surface speed, rollers, tread/arrows and belt witnesses follow committed travel and reverse together. Side/underside contacts remain ordinary contacts, and signed port/hinge conventions must agree.

  **Configuration / modes:** length, width, surface_per_radian. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-151) and this complete entry. **Owner / collaborators:** [CAT-019 D/I/V and fixtures](invest/current-consumers.md#cat-019-i), rotary capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-020"></a>

- [ ] **CAT-020 · counter.** Integer target 1–9/default 3 counts distinct activation events, saturates, emits threshold once and latches a separately supplied contact until Reset. Held/busy/extra triggers cannot count every tick or reemit; no supply creates no electricity. Slate/gold dots retain count and threshold history through coherent commit; failed tick restores pending inputs and count.

  **Configuration / modes:** target_count. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-269) and this complete entry. **Owner / collaborators:** [CAT-020 D/I/V and fixtures](invest/current-consumers.md#cat-020-i), signal capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-021"></a>

- [ ] **CAT-021 · cyan_receiver.** Cyan requires green and blue each at threshold and at least 90% total RGB in that channel set; red alone or one missing required channel fails. Test per-channel boundaries, backside/occlusion and absent independent supply; committed meter/indicator and actual switched output agree.

  **Configuration / modes:** threshold. OpticalChannel=Cyan. **Current acceptance:** [retained behavior](#todo-301) and this complete entry. **Owner / collaborators:** [CAT-021 D/I/V and fixtures](invest/current-consumers.md#cat-021-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-022"></a>

- [ ] **CAT-022 · delay.** Ready/Counting/Finished activation-only delay spans 0.1–12/default 1 seconds. Busy/finished inputs are ignored until Reset; no electrical supply is generated. Test before/at deadline, repeated same-tick input and direct-early-bypass control. Countdown hand and finish feedback derive from committed timer state and cannot advance it.

  **Configuration / modes:** delay_seconds. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-130) and this complete entry. **Owner / collaborators:** [CAT-022 D/I/V and fixtures](invest/current-consumers.md#cat-022-i), signal capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-023"></a>

- [ ] **CAT-023 · domino.** Finite 0.4-mass box with offset centre of mass tips and propagates by real contacts, not neighbour scan/prescribed flip. ActivationOut emits once beyond 45 degrees from start orientation; activation input explicitly rejects. Test grounded close-gap chain versus separated gap, prevent the striker directly hitting the second tile, rest and Reset; pips follow committed mass-frame pose. *Delivery note (CAT-023a, Story 5.1): the dynamic box ships with its body origin at the box centre; the offset centre of mass is deferred to CAT-023b/CAT-014.* *Delivery note (CAT-023b, Story 5.2): ActivationOut is a declarative orientation-threshold sensor (45° from the admitted pose, once per world, rearmed by admission) compiled only for a wired Domino; activation input is rejected at the port check; `domino_effect` is authored as `DominoEffect` data; the offset centre of mass remains deferred to CAT-014.* *Delivery note (CAT-014, Story 6.1): the offset centre of mass is explicitly re-deferred as a Domino (CAT-023) follow-up item; the Bowling ball slice ships homogeneous spheres only and does not take it on.*

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [simulation and element acceptance](#accept-simulation) and this complete entry. **Owner / collaborators:** [CAT-023 D/I/V and fixtures](invest/current-consumers.md#cat-023-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-024"></a>

- [ ] **CAT-024 · electrical_nand.** Nand separately supplied A/B gate must pass all four truth rows and retract outputs coherently. A true no-input condition cannot create power. Reject zero-delay nonmonotone feedback atomically even through open switched routes; test reordering/reconvergence and actual input/output lamps.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=Nand. **Current acceptance:** [retained behavior](#todo-362) and this complete entry. **Owner / collaborators:** [CAT-024 D/I/V and fixtures](invest/current-consumers.md#cat-024-i), electrical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-025"></a>

- [ ] **CAT-025 · electrical_nor.** Nor separately supplied A/B gate must pass all four truth rows and retract outputs coherently. True no-input state requires independent supply. Reject zero-delay nonmonotone feedback atomically including open switched routes; test reordering/reconvergence and its own icon/lamps.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=Nor. **Current acceptance:** [retained behavior](#todo-362) and this complete entry. **Owner / collaborators:** [CAT-025 D/I/V and fixtures](invest/current-consumers.md#cat-025-i), electrical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-026"></a>

- [ ] **CAT-026 · electrical_or.** Or separately supplied A/B gate must pass all four truth rows and output retraction; And/Or strongly connected components restart from unpowered least fixed point each solve. Test source-free loop, missing supply, reorder/reconvergence and actual committed lamps.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=Or. **Current acceptance:** [retained behavior](#todo-362) and this complete entry. **Owner / collaborators:** [CAT-026 D/I/V and fixtures](invest/current-consumers.md#cat-026-i), electrical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-027"></a>

- [ ] **CAT-027 · electrical_xor.** Xor separately supplied A/B gate is true only for exactly one control; enumerate all four rows with/without supply and output retraction. Reject zero-delay nonmonotone feedback atomically; test reordered/chained controls and its own committed indicator.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=Xor. **Current acceptance:** [retained behavior](#todo-362) and this complete entry. **Owner / collaborators:** [CAT-027 D/I/V and fixtures](invest/current-consumers.md#cat-027-i), electrical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-028"></a>

- [ ] **CAT-028 · fan.** Self-contained activation-controlled fan exposes powered, force, reach and width; admitted emission spends a finite source budget shared by all linear/rotary receivers. Actual pose, pressure, opposing jets and transparent solid occlusion matter. Test off/miss/full/partial block, heavy/light load and mixed rotor/body paid branches; no battery/shaft input is invented. Rotor artwork may coast cosmetically but cannot emit force.

  **Configuration / modes:** powered, force, reach, width. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [simulation and element acceptance](#accept-simulation) and this complete entry. **Owner / collaborators:** [CAT-028 D/I/V and fixtures](invest/current-consumers.md#cat-028-i), air capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-029"></a>

- [ ] **CAT-029 · flashlight.** Self-contained torch latches on impact/activation and emits a finite 8-unit, 15-degree-half-angle cone. Nine solar front-face samples, facing/range and partial/full occlusion determine output; ambient rendered light contributes none. Four nested 48-direction translucent shells clip consistently yet never supply optical authority. Test off, rotated, shadow and missing-wire controls.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-134) and this complete entry. **Owner / collaborators:** [CAT-029 D/I/V and fixtures](invest/current-consumers.md#cat-029-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-030"></a>

- [ ] **CAT-030 · funnel.** Physically hollow transparent frustum narrows 2.6-unit mouth to 1.3-unit bore with compatible tube mouths. Qualify inlet/wall/outlet/rim, oversize/high-speed/rotated payload and straight/bend joins; no attraction or scripted transport. Solid shell blocks bodies/air, clear wall transmits light except opaque collar; mesh and contact geometry match.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-242) and this complete entry. **Owner / collaborators:** [CAT-030 D/I/V and fixtures](invest/current-consumers.md#cat-030-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-031"></a>

- [ ] **CAT-031 · green_filter.** Green finite two-sided aperture preserves only preexisting green energy, without recolouring or gain. Qualify red/blue-only and unlike stacked-filter extinction, reverse/rotated incidence, solid transparent pane and opaque frame, with committed path/artwork.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. OpticalChannel=Green. **Current acceptance:** [retained behavior](#todo-297) and this complete entry. **Owner / collaborators:** [CAT-031 D/I/V and fixtures](invest/current-consumers.md#cat-031-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-032"></a>

- [ ] **CAT-032 · green_receiver.** Green requires threshold green and 90% channel purity with separate supplied electrical contact. Red/blue-only, below threshold, backface/blocked and no-supply controls remain individual proof; indicator follows coherent committed readings.

  **Configuration / modes:** threshold. OpticalChannel=Green. **Current acceptance:** [retained behavior](#todo-300) and this complete entry. **Owner / collaborators:** [CAT-032 D/I/V and fixtures](invest/current-consumers.md#cat-032-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-033"></a>

- [ ] **CAT-033 · hold_timer.** Activation closes independently supplied contact for 0.1–12/default 2 seconds; ignores busy triggers and rearms after expiry. Source loss does not pause countdown or create electricity. Test trigger/expiry boundary, loss/restoration and early retrigger; display and pending event state Reset exactly.

  **Configuration / modes:** hold_seconds. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-261) and this complete entry. **Owner / collaborators:** [CAT-033 D/I/V and fixtures](invest/current-consumers.md#cat-033-i), signal capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-034"></a>

- [ ] **CAT-034 · impact_lever.** Beam mass and authored initial angle define finite hinge inertia, end stops and rope sockets. Retain mass×arm/equal-load ten-second balance, pivot/miss/insufficient-energy controls, four orientations, all fixed/moving/box/sphere/tube/bend/frustum/beam obstacles and open bores/rims. No canned flip. Gold fulcrum/stops, beam collider and artwork follow committed constrained motion; coupled ropes/friction remain closure criteria. End-socket ropes use angular moment-arm/velocity coupling; prove slack/taut shutter loads, beam/beam and solid fulcrum, resting/sliding distributed loads.

  **Configuration / modes:** beam_mass, initial_angle. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#sequence-task-307) and this complete entry. **Owner / collaborators:** [CAT-034 D/I/V and fixtures](invest/current-consumers.md#cat-034-i), constraint capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-035"></a>

- [ ] **CAT-035 · lamp.** ActivationIn latches the existing Signal lamp until Reset; its contract is activation, not a fabricated electrical PowerIn. Missed/disconnected/wrong-domain input cannot light it. Gold bulb/emission is driven only by committed activation and must clear after failed Run/Reset; duplicate triggers cannot fabricate additional events.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [simulation and element acceptance](#accept-simulation) and this complete entry. **Owner / collaborators:** [CAT-035 D/I/V and fixtures](invest/current-consumers.md#cat-035-i), signal capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-036"></a>

- [ ] **CAT-036 · laser.** Separate PowerIn and latched trigger enable produce authored amber linear-RGB power over 16 units; supply loss clears beam at next optical snapshot while enable survives. Restored supply needs no fake retrigger. Test disabled/no-power/opaque/rotated/range cases; owned beam-path snapshots and colour come from committed optical state, with failed-trace rollback.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-285) and this complete entry. **Owner / collaborators:** [CAT-036 D/I/V and fixtures](invest/current-consumers.md#cat-036-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-037"></a>

- [ ] **CAT-037 · latch.** Set/Reset inputs settle on the next boundary with Reset dominant for same-tick conflict. Memory survives supply loss; independent supplied contact never generates power. Qualify each input, duplicate/reordered events, missing supply and failed-tick pending state; arm/indicator follows committed memory and Reset clears queue/state.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-267) and this complete entry. **Owner / collaborators:** [CAT-037 D/I/V and fixtures](invest/current-consumers.md#cat-037-i), signal capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-038"></a>

- [ ] **CAT-038 · light_receiver.** Broadband finite front-facing absorbing disc switches independent supply when received power meets threshold 0.05–2/default 0.25. Trace all emitters before committing all readings and electrical solve. Half-power 0.35 positive and quarter-power 0.175 negative must survive canonical f32 admission; backside/occlusion/no-supply controls and concentric marker/eased indication remain required.

  **Configuration / modes:** threshold. OpticalChannel=Broadband. **Current acceptance:** [retained behavior](#todo-290) and this complete entry. **Owner / collaborators:** [CAT-038 D/I/V and fixtures](invest/current-consumers.md#cat-038-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-039"></a>

- [ ] **CAT-039 · linear_pusher.** Independent supply/ExtendIn/RetractIn drive bounded stroke 0.25–3, speed 0.25–4, acceleration 1–30 and force 1–100. Conflict/no command holds; power loss engages declared self-locking brake. Supplied endpoint outputs become visible next electrical tick. Test both directions/reversal, low-force gravity support, stall/obstruction/miss and interruption; finite head/rod collider, travel marks and phase indicator follow actual motion/work.

  **Configuration / modes:** stroke, speed, acceleration, force. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#sequence-task-309) and this complete entry. **Owner / collaborators:** [CAT-039 D/I/V and fixtures](invest/current-consumers.md#cat-039-i), actuator capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-040"></a>

- [ ] **CAT-040 · magenta_receiver.** Magenta requires red and blue each at or above threshold and 90% total RGB in that set; one missing channel or green-only fails. Prove its own channel boundaries, backside/blocked/no-supply controls, typed contact and committed indication.

  **Configuration / modes:** threshold. OpticalChannel=Magenta. **Current acceptance:** [retained behavior](#todo-301) and this complete entry. **Owner / collaborators:** [CAT-040 D/I/V and fixtures](invest/current-consumers.md#cat-040-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-041"></a>

- [ ] **CAT-041 · mirror.** Finite front-silvered mirror reflects actual transformed normal retaining 95% RGB per bounce and one remaining range budget up to 16 reflections. Back/frame/mount/returning-ray occlusion are physical controls. Rotate via UI; preview and committed reflection match the same declared aperture.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-286) and this complete entry. **Owner / collaborators:** [CAT-041 D/I/V and fixtures](invest/current-consumers.md#cat-041-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-042"></a>

- [ ] **CAT-042 · motor.** Speed/torque declare finite rotor inertia and bounded effort/power/work, not assigned velocity. Test load/stall/low-torque acceleration, missing/lost supply, actual shaft travel and rotated connected conveyor. Supply loss adds no work and does not erase momentum; any damping is an explicit physical law. Rotor/indicator/belt output observe committed state.

  **Configuration / modes:** speed, torque. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-146) and this complete entry. **Owner / collaborators:** [CAT-042 D/I/V and fixtures](invest/current-consumers.md#cat-042-i), rotary capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-043"></a>

- [ ] **CAT-043 · optical_and.** And independently hysteretic A/B apertures use 0.25 on/0.225 off and next-tick decision; separate carrier passes at 90% only when both controls are high. Every truth row with/without carrier, transition/retraction/chaining, occlusion/rotation and preserved range/interaction budget must pass; indicator shows actual output power.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=And. **Current acceptance:** [retained behavior](#todo-367) and this complete entry. **Owner / collaborators:** [CAT-043 D/I/V and fixtures](invest/current-consumers.md#cat-043-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-044"></a>

- [ ] **CAT-044 · optical_nand.** Nand applies independent 0.25-on/0.225-off control hysteresis and next-tick decision to a separate 90%-retained carrier. Enumerate all truth rows with/without carrier, timing/retraction/occlusion/rotation and remaining range/interactions; true logic without carrier emits nothing and output artwork agrees.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=Nand. **Current acceptance:** [retained behavior](#todo-367) and this complete entry. **Owner / collaborators:** [CAT-044 D/I/V and fixtures](invest/current-consumers.md#cat-044-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-045"></a>

- [ ] **CAT-045 · optical_nor.** Nor passes the independent carrier at 90% only with both controls low under separate 0.25-on/0.225-off hysteresis and next-tick decisions. Prove every truth row and carrier absence, output retraction, chaining/occlusion/rotation and retained range budget; no self-created light.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=Nor. **Current acceptance:** [retained behavior](#todo-367) and this complete entry. **Owner / collaborators:** [CAT-045 D/I/V and fixtures](invest/current-consumers.md#cat-045-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-046"></a>

- [ ] **CAT-046 · optical_or.** Or uses independent 0.25-on/0.225-off hysteretic A/B controls and next-tick decisions. Carrier retains 90% when either input is high; all truth rows with/without carrier, output retraction, coherent ordering, rotation/occlusion and remaining interaction/range budget remain individual proof.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=Or. **Current acceptance:** [retained behavior](#todo-367) and this complete entry. **Owner / collaborators:** [CAT-046 D/I/V and fixtures](invest/current-consumers.md#cat-046-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-047"></a>

- [ ] **CAT-047 · optical_xor.** Xor routes 90% of independent carrier only with exactly one high hysteretic control, at next-tick decision. Prove all four rows with/without carrier, simultaneous transitions, output retraction, rotation/occlusion and bounded chained routing. Actual output-power lamp cannot show a fabricated beam.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. Logic=Xor. **Current acceptance:** [retained behavior](#todo-367) and this complete entry. **Owner / collaborators:** [CAT-047 D/I/V and fixtures](invest/current-consumers.md#cat-047-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-048"></a>

- [ ] **CAT-048 · pipe.** Clear straight annular bore stays 1.3 units while length resizes 1–8 through one local-axis handle with Cancel/single Undo. Qualify tilted/rotated/high-speed/oversize/sidewall travel, collar optical blocking and clear-wall transmission. Typed 0.45-unit/20-degree mouth snapping rejects occupied/wrong-bore/nonopposed ends; physical continuous seams, separation/rejoin and all difficulty controls remain required. Snapping is idle-construction-only, including rejection while paused; foreign same-ID parts reject. Preview is nonmutating, preserves roll through shortest alignment and creates no transport-network edge.

  **Configuration / modes:** length. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-125) and this complete entry. **Owner / collaborators:** [CAT-048 D/I/V and fixtures](invest/current-consumers.md#cat-048-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-049"></a>

- [ ] **CAT-049 · pipe_bend_45.** Distinct 45-degree fixed-radius hollow bend shares 1.3-unit bore and typed mouth snapping; preserve its own icon/rails/collars. Test straight-to-bend/bend-to-bend rotated passage, rim/oversize/sidewall and missed connection, plus actual valid Precise alternative route and authored assistance controls. No 90-degree proof substitutes.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. TubeAngle=Degrees45. **Current acceptance:** [retained behavior](#todo-250) and this complete entry. **Owner / collaborators:** [CAT-049 D/I/V and fixtures](invest/current-consumers.md#cat-049-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-050"></a>

- [ ] **CAT-050 · pipe_bend_90.** Distinct 90-degree hollow bend uses common bore/typed mouth geometry, matching collars/rails and icon. Qualify continuous turned passage, straight/bend joins, rotated/oversize/rim/missing controls and joined_pipe fixture. Declared depth-error Forgiving/Balanced success versus Precise failure remains required; no 45-degree representative closure.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. TubeAngle=Degrees90. **Current acceptance:** [retained behavior](#todo-250) and this complete entry. **Owner / collaborators:** [CAT-050 D/I/V and fixtures](invest/current-consumers.md#cat-050-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-051"></a>

- [ ] **CAT-051 · powered_gate.** Electrical-only supply retracts a physical acceleration-limited gold blade between matching tube mouths; no activation input inferred. Unpowered/loaded/blocked closing, clearing/retry, supply interruption and rotated passage must match actual collider state. Rails/housing/blade and beam occlusion share committed geometry; obstruction cannot be teleported through.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-244) and this complete entry. **Owner / collaborators:** [CAT-051 D/I/V and fixtures](invest/current-consumers.md#cat-051-i), actuator capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-052"></a>

- [ ] **CAT-052 · pressure_plate.** Threshold 0.1–16/default 0.5 kg counts each directly top-contacting body once. Sufficient resting mass closes separate supplied contact only while present; compound contacts and stacked transmitted force do not multiply mass. Side/underside/impact-only/below-threshold/no-supply controls, coherent indicator and removal/Reset must pass.

  **Configuration / modes:** minimum_mass. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-265) and this complete entry. **Owner / collaborators:** [CAT-052 D/I/V and fixtures](invest/current-consumers.md#cat-052-i), signal capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-053"></a>

- [ ] **CAT-053 · pulley.** Fixed frictionless routed pulley participates in typed unbranched endpoint routes; physical length includes the true finite-radius groove tangents and arcs, not the superseded point-guide approximation. Slack/open route transmits no tension; test equal/unequal masses, declaration order, 3D direction, pendulum and invalid graphs. Rim tangent/arc rope artwork and wheel travel agree with physical length; finite-inertia/frictional pulley variants are separate future models. Connected-body collision stays enabled; impossible fully static fixed-length routes reject, prescribed routes require a dynamic participant, and missing registered bodies never become invented fixed points.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-140) and this complete entry. **Owner / collaborators:** [CAT-053 D/I/V and fixtures](invest/current-consumers.md#cat-053-i), constraint capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-054"></a>

- [ ] **CAT-054 · ramp.** Authored length/width, arbitrary pose and bounded assistance define a real contact ramp. Prove first_principles two-ramp delivery with missing/misaligned and precise alternative-solution controls, then source fixtures and supported dimensions. Physical mesh/contact/projections agree and guided placement never creates a separate numerical trajectory.

  **Configuration / modes:** length, width. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [simulation and element acceptance](#accept-simulation) and this complete entry. **Owner / collaborators:** [CAT-054 D/I/V and fixtures](invest/current-consumers.md#cat-054-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-055"></a>

- [ ] **CAT-055 · red_filter.** Red finite two-sided aperture retains only existing red energy; green/blue-only and unlike stacked filters extinguish. Prove front/back/rotated paths, transparent solid pane and opaque frame controls; red artwork does not recolour numerical power.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. OpticalChannel=Red. **Current acceptance:** [retained behavior](#todo-297) and this complete entry. **Owner / collaborators:** [CAT-055 D/I/V and fixtures](invest/current-consumers.md#cat-055-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-056"></a>

- [ ] **CAT-056 · red_receiver.** Red requires red at threshold and 90% total RGB purity, with independent supply. Wrong colour, below threshold, backside/blocked and no-supply controls accompany actual typed contact and coherent threshold indication.

  **Configuration / modes:** threshold. OpticalChannel=Red. **Current acceptance:** [retained behavior](#todo-300) and this complete entry. **Owner / collaborators:** [CAT-056 D/I/V and fixtures](invest/current-consumers.md#cat-056-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-057"></a>

- [ ] **CAT-057 · reverse_transmission.** Two finite-inertia shafts and a −1 phase-free transmission reverse signed motion/work through actual guides. Qualify each input direction, two reversers, load/backdrive, disconnected graph, order/rotation and supply loss; wheels/belt witnesses follow independently committed physical travel, not copied speed.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-151) and this complete entry. **Owner / collaborators:** [CAT-057 D/I/V and fixtures](invest/current-consumers.md#cat-057-i), rotary capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-058"></a>

- [ ] **CAT-058 · rope_anchor.** Fixed typed endpoint supports a hanging load through a complete measured rope route. Test slack/incomplete/wrong-endpoint and invalid branching/cycle admission, angled/pulley routes and force reaction. Knot/rope artwork follows actual attachment, open routes dashed; Reset/save preserve exact endpoint identities and lengths.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-140) and this complete entry. **Owner / collaborators:** [CAT-058 D/I/V and fixtures](invest/current-consumers.md#cat-058-i), constraint capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-059"></a>

- [ ] **CAT-059 · solar_panel.** Nine front-face samples integrate the self-contained torch's finite cone/range/facing and partial/full physical occlusion; ambient rendered light never supplies power. Preserve blue cells/four-mark meter and typed output. No source/shadow/missing-wire/backside controls and actual solar_motor/solar_shadow/delayed_solar fixtures are required; paid downstream work must obey admitted source law.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-134) and this complete entry. **Owner / collaborators:** [CAT-059 D/I/V and fixtures](invest/current-consumers.md#cat-059-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-060"></a>

- [ ] **CAT-060 · sound_meter.** Threshold 0.05–1/default 0.25 samples strongest direct pulse coherently before electrical solve, with facing/opaque-blocking and 90% off hysteresis. Independently supplied contact and rising activation discard unpowered crossings; quiet rearms. Test source order/repeats/wrong-facing/block/no-supply and all admitted tone filtering; needle is committed feedback, not audio playback state.

  **Configuration / modes:** threshold. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-346) and this complete entry. **Owner / collaborators:** [CAT-060 D/I/V and fixtures](invest/current-consumers.md#cat-060-i), acoustic capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-061"></a>

- [ ] **CAT-061 · speaker.** Next-tick coalesced trigger requires independent supply and expires unpowered; minimum interval 24 ticks bounds five in-flight pulses. Typed Low/Mid/High tones each use finite 12-unit/s propagation, 8-unit reach, 0.15-second local duration and 35-degree cone with attenuation. Test direction/range/occlusion/retrigger/no-power; cone/wave/audio observe committed occurrence and mute/suspension cannot change gameplay.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. AcousticTone=Low, AcousticTone=Mid, AcousticTone=High. **Current acceptance:** [retained behavior](#todo-345) and this complete entry. **Owner / collaborators:** [CAT-061 D/I/V and fixtures](invest/current-consumers.md#cat-061-i), acoustic capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-062"></a>

- [ ] **CAT-062 · spring.** Finite 0.25-mass plate on local-Y slider uses stiffness 120–1200/default 400, damping 0–8/default 0.2 and initial compression 0–0.20/default zero, within 0.25 travel. Explicit preload is finite initial energy, not latch/free launch. Test zero-load stationary/missed load, real contact/rebound/tilt/stops and energy bounds; actual plate collider and helix follow compression, obsolete strength field rejects.

  **Configuration / modes:** stiffness, damping, initial_compression. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-461) and this complete entry. **Owner / collaborators:** [CAT-062 D/I/V and fixtures](invest/current-consumers.md#cat-062-i), elastic capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-063"></a>

- [ ] **CAT-063 · switch.** Physical contact above authored trigger threshold latches activation once; ActivationOut and separately supplied PowerIn→Supply remain distinct contracts. Qualify missed/gentle/duplicate impacts, lost/missing supply, coherent network cycles and direct-wire-bypass lesson. Button/collider/colour and event publication derive from committed state; Reset restores unpressed state and both domains.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-147) and this complete entry. **Owner / collaborators:** [CAT-063 D/I/V and fixtures](invest/current-consumers.md#cat-063-i), signal capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-064"></a>

- [ ] **CAT-064 · tennis.** Distinct lightweight high-bounce sphere retains canonical mass/bounce/radius/drag/buoyancy and committed appearance. Test gravity/rest/bounce and conserved Fan trajectory against heavier Bowling/Basketball controls, missed/blocked airflow and source fixtures; sharing scene/source cannot erase its configuration.

  **Configuration / modes:** mass, bounce, radius, buoyancy, drag. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [simulation and element acceptance](#accept-simulation) and this complete entry. **Owner / collaborators:** [CAT-064 D/I/V and fixtures](invest/current-consumers.md#cat-064-i), air capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-065"></a>

- [ ] **CAT-065 · trampoline.** Finite footprint compliant membrane with tension/damping, separate contact patches and rigid rim/back stores/returns energy; no prescribed launch. Prove off-centre/simultaneous/stacked/tilted loads, rim/back/miss/overload and tether integration. Apply physical force once per solver step, not per projection iteration. Direction-aware bounded sag follows committed deformation; cosmetic smoothing cannot change collision/work.

  **Configuration / modes:** tension, damping_ratio. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#sequence-task-306) and this complete entry. **Owner / collaborators:** [CAT-065 D/I/V and fixtures](invest/current-consumers.md#cat-065-i), elastic capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-066"></a>

- [ ] **CAT-066 · wall.** Width/height/thickness, full 3D pose and three local-axis resize handles update real collision, artwork and dashed projections together. Prove limits/invalid dimensions, Cancel/one-gesture Undo and saved properties. Body/air/sound/light occlusion, rotated impacts and solar_shadow/shade fixture remain distinct controls; placement reference walls are not puzzle walls.

  **Configuration / modes:** width, height, thickness. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-162) and this complete entry. **Owner / collaborators:** [CAT-066 D/I/V and fixtures](invest/current-consumers.md#cat-066-i), contact capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-067"></a>

- [ ] **CAT-067 · weight.** Canonical authored mass and spherical body/socket support lifting, counterbalance and 3D pendulum motion. Test equal/unequal ratios, slack release/open route, floor contact, direction/order and energy/length bounds with pulley/anchor; solid pose and rope attachment agree. Default mass 4 is not the fixed fixture mass 1.

  **Configuration / modes:** mass. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-141) and this complete entry. **Owner / collaborators:** [CAT-067 D/I/V and fixtures](invest/current-consumers.md#cat-067-i), constraint capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-068"></a>

- [ ] **CAT-068 · white_receiver.** White requires each red/green/blue channel at threshold and declared 90% set rule, not exact spectral matching. Any absent required channel/below-threshold/backside/occluded/no-supply control fails; independently supplied output and committed white indication remain individual evidence.

  **Configuration / modes:** threshold. OpticalChannel=White. **Current acceptance:** [retained behavior](#todo-301) and this complete entry. **Owner / collaborators:** [CAT-068 D/I/V and fixtures](invest/current-consumers.md#cat-068-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-069"></a>

- [ ] **CAT-069 · wind_chimes.** Constrained 3D sail/clapper strikes four individually identified tube envelopes with separation rearm; overlap or resting jet cannot ring. Tube Low/Mid/High bands, strongest same-tick/tie-by-identity selection, 24-tick pulse cap and next-tick playback remain exact. Direct ball strike is a distinct control path; weak/blocked/opposed/no-air and cap miss fail. Sail/rope/tubes/waves observe paid physical motion and immutable emission.

  **Configuration / modes:** declared geometry, typed ports and any source-owned settings; no parameter absence inferred. AcousticTone=Low, AcousticTone=Mid, AcousticTone=High. **Current acceptance:** [retained behavior](#todo-348) and this complete entry. **Owner / collaborators:** [CAT-069 D/I/V and fixtures](invest/current-consumers.md#cat-069-i), acoustic capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-070"></a>

- [ ] **CAT-070 · windmill.** Four weighted actual-pose samples preserve partial occlusion, signed rear drive, edge-on/opposing controls, response 0.1–4, cut-in 0.05 and unloaded target cap 12 rad/s. Finite inertia/load/backdrive and passive calm-air loss use the current conserved rotor law with per-branch source debit. No velocity clamp or independent drive torque; shaft/blades/output follow committed state and connected/disconnected cargo controls.

  **Configuration / modes:** radians_per_force. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#sequence-task-311) and this complete entry. **Owner / collaborators:** [CAT-070 D/I/V and fixtures](invest/current-consumers.md#cat-070-i), air capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-071"></a>

- [ ] **CAT-071 · wound_spring.** Stiffness/stroke/winding-lead declare finite ratcheted store and guided plunger. Accepted positive shaft travel is capped by torque/work/clearance; reverse freewheels, stopped/reduced drive retains charge. Separate trigger adds no energy, empty request expires, release disconnects winding until physical end/rearm. Blocked/partial/oversize/off-centre, missing supply/release, repeated trigger and charged rollback controls remain required; coil/latch/index follow committed motion. Account separately for legitimate contact/gravitational recharge and paid motor work; do not silently label all accepted stored energy motor input.

  **Configuration / modes:** stiffness, stroke, winding_lead. No enumerated selector in the inventory; audit configuration rather than infer none. **Current acceptance:** [retained behavior](#todo-394) and this complete entry. **Owner / collaborators:** [CAT-071 D/I/V and fixtures](invest/current-consumers.md#cat-071-i), actuator capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

<a id="current-cat-072"></a>

- [ ] **CAT-072 · yellow_receiver.** Yellow requires red and green each at threshold and 90% total power in their set; blue-only or either missing required channel fails. Prove channel boundaries, backside/occlusion/no independent supply and exact typed contact/committed indication.

  **Configuration / modes:** threshold. OpticalChannel=Yellow. **Current acceptance:** [retained behavior](#todo-301) and this complete entry. **Owner / collaborators:** [CAT-072 D/I/V and fixtures](invest/current-consumers.md#cat-072-i), optical capability in the [logical order](invest/vertical-delivery.md#existing-element-order).

**Successful outcome:** Every named existing catalogue element, variant and fixture has a complete current-design contract and independent current proof of its physical behavior, animation/rendering, controls and restoration. Missing model choices or proof remain open under that exact CAT owner; no family representative, archived Pass or generic gate completes a piece.

### 0.1 Current physics completion target

**Design acceptance for every item below:** [common gates](#design-acceptance) + [SIM](#accept-simulation); additional profiles apply to cross-cutting requirements.

**When:** First; blocks the post-physics queue.

**Gameplay outcome:** Players can Run, inspect, Reset and rerun the same constructed machine without solver errors, lost time, invented energy, changed links or drifting initial state. One shared world owns motion and contacts.

These are current behavior and control criteria under WASM SIMD f32 physics authority. [TODO](../../TODO.md) owns current implementation status. Task numbers are stable identities, not a chronological execution order. Rows marked superseded have no current implementation action; their IDs remain for traceability.

<a id="sequence-task-057"></a>

**Superseded — no current action.** **Superseded CPU prototype; no current action.** The unused tangent-quadratic experiment is not part of the WASM SIMD physics design and does not require integration or porting. Current coupled-contact behavior and work/error qualification are owned by the contact criteria below and P0-007/P0-012.

<a id="sequence-task-058"></a>

- [ ] **Coupled constrained-contact response.** Solve contact, friction and equality constraints together within the current f32 puzzle envelope, including rank-deficient tangential response and moving-frame convective targets. Preserve powered/unpowered motor outcomes, bellows loading, chime motion, atomic rejection and exact Reset; a particular CPU factorization is not required.

<a id="sequence-task-059"></a>

- [ ] **Bellows and chime contact stability.** The powered bellows and both chime identity orders must complete their intended physical response without solver exhaustion or order-dependent outcomes. Qualify coupled friction, rank-deficient response and complete-Run work/error bounds at the admitted f32 scales; microscopic CPU residual targets are not acceptance.

<a id="sequence-task-060"></a>

- [ ] **Bounded evaluation of elastic and compliant laws.** Evaluate each elastic/compliant law from a coherent current candidate pose, with no stale cross-evaluation state. Measure complete tick/transfer/allocation costs and preserve motion, no-fan controls, exact Reset and failed-candidate atomicity; optimize only measured redundant work.

<a id="sequence-task-061"></a>

- [ ] **Dependency-bounded coupled-shaft work.** Instrument and bound numerical work for fan-driven coupled shafts against the same construction without fan drive. Typed counters distinguish physical workload from implementation overhead; preserve complete coupled behavior, current error limits and Reset while qualifying whole-system improvement.

<a id="sequence-task-062"></a>

- [ ] **Bounded solver scratch and ownership.** Reuse bounded scratch without routine warmed allocation, cross-query aliasing or retained references. Prove failure atomicity, reuse, zero-rank and concurrent-owner controls; preserve rotor/conveyor outcomes, no-fan controls and exact Reset while measuring the actual GPU/host tick.

<a id="sequence-task-063"></a>

- [ ] **Conveyor and airflow workload qualification.** Construct conveyor contact transport and its unpowered-support control, a fan-driven windmill and mechanical conveyor output through actual UI controls. Verify typed connections, exact Run/Reset and supported save/load; qualify complete-system time, memory and resource behavior without hiding stalls or screenshot failures.


<a id="sequence-task-064"></a>

- [ ] **World-owned angular travel.** Committed hinge winding and traveled distance belong to the simulation and restore with failed-tick rollback and snapshots. Motor, windmill and conveyor events/read models observe that history; prove unpowered, full-turn, reversal and exact Reset/save controls without callback-owned angular accumulators.


<a id="sequence-task-065"></a>

- [ ] **Angular path boundaries.** Measure signed winding and total angular variation along accepted motion, including reversals between matching endpoints. Bound uncertainty under the admitted f32 contract and reject singular/unsupported ranges or exhausted budgets before commit; publication and restoration must reflect the same accepted path.

<a id="sequence-task-066"></a>

- [ ] **Construction gesture isolation.** Fast pipe placement/movement gestures must not accidentally activate Run or Reset. Verify pointer ownership, drag cancellation, stopped-state admission and one Undo per gesture through actual Chrome controls; any unresolved unintended activation remains a failed interaction criterion.

<a id="todo-003"></a>

<a id="sequence-task-067"></a>

- [ ] **Authoritative placement admission.** Run admission uses the same quantized geometry, participation and joint filters as physical execution. Reject shell overlap while accepting valid hollow-bore placement; preserve exact construction/links after failed admission, Run/Reset and supported Save/Load, including fast-drag control isolation.

<a id="sequence-task-068"></a>

- [ ] **Compound-query resource bounds.** Compound traversal must preserve candidate membership and hollow passages while using bounded owned scratch. Qualify sparse, hollow and dense scenes, shell contact versus open-bore passage, exact Reset/save and repeated allocation/memory/performance without omitting hard candidate pairs.

<a id="sequence-task-069"></a>

- [ ] **Typed physics work instrumentation.** Record bounded enum-typed per-tick physical work counters with explicit overflow, missing-data and failed-tick semantics. Reject unknown metric identities and malformed boundaries; measure instrumentation overhead and all required scenarios. Recording must not change outcomes, allocate routine warmed scratch or imply runtime qualification.

<a id="sequence-task-070"></a>

- [ ] **Conservative spatial indexing.** Use conservative world/body bounds for construction, dynamic and prescribed-motion queries. Sparse separated scenes must avoid unnecessary pair work; dense controls retain every required pair. Prove no-ramp, horizontal/tilted ramp, hollow/rotation and exact Reset/save cases with actual browser performance.

<a id="sequence-task-071"></a>

- [ ] **Whole-pipeline performance observation.** Measure complete committed/failed ticks, domains, numerical physics, transport, animation and presentation with bounded typed samples. Predeclare stage definitions, overflow and inclusive/exclusive costs; qualify observer overhead and production controls on required devices. Missing data or old timings cannot satisfy current budgets.

<a id="sequence-task-072"></a>

- [ ] **Canonical body-local pose composition.** Every body/child pose composes in its declared local frame using the current integer-cell/f32 representation and validated external adapters. Rotated pusher installation, bellows/chime contact and exact Reset must remain correct; no render transform or wider hidden model may become physical authority.

<a id="sequence-task-073"></a>

- [ ] **Ramp construction and browser controls.** Through actual UI, place and rotate a ramp and compare no-ramp, horizontal and correctly tilted constructions against their declared outcomes. Verify exact repeated Run/Reset and supported Save/remove/Load, plus rotated-pusher admission. A visible run that misses its goal is not successful qualification.

<a id="sequence-task-074"></a>

- [ ] **Owned axial-motion observations.** Motor, windmill, conveyor, pusher, gate and shutter reads resolve current owned joints/surfaces and reflect replacement/Restore without scene callbacks. Preserve angular travel, actuator state, endpoint/contact and full rollback semantics; no duplicated motion cache may supply physical truth.

<a id="sequence-task-075"></a>

- [ ] **Committed motor work accounting.** Motor and pusher work/impulse totals are typed simulation-owned state, committed atomically and restored on failure/Reset. Prove powered/unpowered supply, low-force saturation, endpoint obstruction, signed work/braking and exact construction/save behavior; scene accumulators are not authoritative.

<a id="sequence-task-076"></a>

- [ ] **Contact-load sensing.** A pressure plate derives direct-contact load from owned geometry, participation and mass, with immutable typed readings and rollback. Distinguish no contact, insufficient load and accepted load; preserve downstream gate controls, named-body identity and exact Run/Reset/save without callback-owned mass totals.

<a id="sequence-task-077"></a>

- [ ] **Continuous tilt sensing.** Tilt sensors own their construction reference, latched state and event time. Detect qualifying tip-and-return motion within one step; disabled sensing suppresses new events without erasing a completed latch. Prove identity/order, grounded-chain contact, event/rollback and Reset controls.

<a id="sequence-task-078"></a>

- [ ] **Latched spring state and work.** Simulation owns the latched spring's finite charge, work, latch/trigger policy and events. Preserve rotated rest-stop binding, winding/release, obstruction/resumption, retained charge, failed-tick rollback and exact Reset/save; neither a scene store nor a free launch may supply energy.

<a id="sequence-task-079"></a>

- [ ] **Shape-aware receiver guidance.** Guide clearance uses all owned compound support geometry and current relative motion. It must not pull geometry through a receiver wall or replace solid contact; prove compound and disabled/re-enabled controls, entry/exit/descent boundaries, capture residence and exact Reset.

<a id="sequence-task-080"></a>

- [ ] **Moving-frame basket residence.** Basket capture measures containment and speed relative to the moving receiver frame. Exit, excessive relative speed or disabled capture breaks dwell; sufficient continuous residence latches capture once for the named body. Preserve canonical margin/speed/dwell, zero-guide controls, event order, failed-tick rollback and exact Reset/save.

<a id="sequence-task-081"></a>

- [ ] **Continuous passage sensing.** Passage sensors use the full owned compound/aperture geometry and detect directional crossings hidden between endpoints. Preserve within-trajectory arming/rearming and split-step behavior, emit exactly once per qualifying passage and reject reverse/miss/blocked/disabled controls. State and events roll back and Reset exactly.

<a id="sequence-task-082"></a>

- [ ] **Compound chamber eligibility.** Cannon chamber occupancy/seating tests every collider child against finite cylindrical caps and radial containment. Accept valid seated payloads, reject protruding or obstructed compounds and use the moving launcher's actual frame; preserve named-body identity, atomic reload/fire and Reset.

<a id="sequence-task-083"></a>

- [ ] **Finite owned energy stores.** Charging/release uses typed simulation-owned reservoirs and atomic commands. Account for supplied, retained, released and dissipated work without inventing energy; prove powered/unpowered, full/empty/capacity, failed-command/failed-tick and exact canonical construction Reset/save controls.

<a id="sequence-task-084"></a>

- [ ] **Compound swept queries.** All current query consumers use full declared compound geometry and current poses, including cannon muzzle clearance and rear support. Preserve material/participation, hollow openings and contact ordering under translation/rotation; reject unsupported geometry/range atomically and restore exactly.

<a id="sequence-task-085"></a>

- [ ] **Compliant-contact state.** Simulation owns trampoline/contact history, active potential selection, declared initial energy and entry events. Continuous membrane entry, off-centre contact, missed payload, corrupted-presentation isolation and failed-tick rollback must preserve the same physical result and exact Reset/save.

<a id="sequence-task-086"></a>

- [ ] **Continuous guide eligibility.** Locate guide entry/exit and relative-descent transitions along actual candidate motion, including fast passage between endpoints. No current/midpoint-only decision may miss a transition. Preserve shape-aware clearance, bounded assistance, capture semantics and exact Reset.

<a id="sequence-task-087"></a>

- [ ] **Single physical velocity authority.** All laws, queries, events and read models consume the committed/candidate WASM SIMD physical state through typed ownership. Presentation caches cannot override velocity. Prove authored-motion, pipe-bend, contact and restoration controls under current f32 admission, without retaining a CPU fallback solver.

<a id="sequence-task-088"></a>

- [ ] **Body-local collider offsets.** Colliders retain their declared body-local offsets and compose with the owned physical pose; no silent origin-forcing path is allowed. Offset boxes, actuator/rope interactions, collision, selection and exact Reset must agree while presentation remains read-only.

<a id="sequence-task-089"></a>

- [ ] **Live joint identity.** Commands and observations resolve typed current joint identities, kinds and ownership after atomic replacement; detached or foreign joints reject. Construction declarations and runtime bindings are distinct. Prove joint/rope/plunger/mechanical controls, restoration and stale-reference rejection.

<a id="sequence-task-090"></a>

- [ ] **Sliding-blade ownership.** Powered gates and optical shutters present their committed physical blade pose without writing it into construction geometry. Preserve supplied opening, supply-loss closing, obstruction-stop/non-crushing controls, cargo interaction, endpoint/speed bounds and exact replay/Reset.

<a id="sequence-task-091"></a>

- [ ] **Explicit runtime geometry transactions.** Runtime body-local geometry updates are immutable, explicit and atomic, preserving material/participation and owned identity. Live/paused construction recapture rejects. Prove pusher extension/contact, valid deformation, invalid updates, query invalidation, rollback and exact restoration.

<a id="sequence-task-092"></a>

- [ ] **Canonical construction orientation.** One canonical typed orientation representation is shared by editor preview, content, current tools, saves and Reset; shortest-arc assistance uses the authoritative geometry. Old schemas reject atomically, and rotated construction round-trips exactly in the current canonical format. The campaign target remains 150 levels.

<a id="sequence-task-093"></a>

- [ ] **Guarded physical mutation.** Only typed owned simulation transactions may mutate physical state. Internal numerical helpers cannot provide an alternate public mutation route. Prove invalid owner/lifecycle rejection, whole-tick rollback, exact Reset and read-only presentation isolation; API visibility alone is not ownership qualification.

<a id="sequence-task-094"></a>

- [ ] **Atomic physical load installation.** Install one immutable coherent set of physical loads with all typed references and ranges validated before mutation. Candidate evaluation, snapshots and publication use that set. Reject partial/foreign/duplicate installation, restore failed work exactly and preserve every affected domain/caller.

<a id="sequence-task-095"></a>

- [ ] **Airflow field and force boundaries.** Locate jet inlet, outlet and rim transitions for body and axial receivers along candidate motion, including moving occlusion and changing source supply. Integrate only accepted source/receiver work, preserve reaction accounting and rollback, and prove hidden-crossing/no-exposure controls.

<a id="sequence-task-096"></a>

- [ ] **Shared continuous boundary queries.** Frame, rope, one-way joint and field consumers require conservative continuous boundary detection with declared finite ranges and budgets. Preserve orientation, crossing/clear and unsupported-range controls; use one current GPU query authority rather than porting an obsolete CPU search helper.

<a id="sequence-task-097"></a>

- [ ] **Bellows emission from current motion.** Bellows compression and refill use actual relative plate motion at the evaluated physical state, not a previous substep's averaged source. Account for finite transported mass/energy, unloaded emission and receiver/reaction work; locate source/field transitions and restore the entire transaction.

<a id="sequence-task-098"></a>

- [ ] **Rotary airflow stage evaluation.** Evaluate rotary exposure, axes, relative speed, passive resistance and shared source allocation from the coherent current candidate state. Preserve finite inertia, signed response, continuous source/field/ratio boundaries and [conserved rotary airflow](../finite-gas-foundation.md#rotary-capture); no independently held drive torque is permitted.

<a id="sequence-task-099"></a>

- [ ] **One airflow geometry/observation path.** Body and rotary loads plus read-only observations use the same typed current-pose airflow fields and owned compound occlusion. Prove rotated/moving frames, partial/blocked exposure, source changes and continuous receiver response with atomic rollback; no duplicated scene sampler decides physical behavior.

<a id="sequence-task-100"></a>

- [ ] **Physical cannon reload.** Author payloads before Run and admit reload only through physical arrival/owned chamber geometry and atomic commands. Prove arrival and no-arrival controls, correct named payload, finite energy, obstruction, repeated firing and exact replay/Reset/save; runtime teleport/insertion cannot counterfeit loading.

<a id="sequence-task-101"></a>

- [ ] **Typed axial effort and physical commands.** Apply axial effort and off-centre force through generic typed simulation declarations evaluated at the current physical state. No scene AddForce/AddTorque/impulse wrapper supplies independent authority. Preserve rotor/bellows/bell/chime timing, reactions, source work and exact rollback/Reset.

<a id="sequence-task-102"></a>

- [ ] **Coupled finite-work cannon actuation.** A cannon release uses the coupled world response and finite owned reservoir, not payload mass alone. Preserve fixed/constrained payload, moving carrier and friction controls, exact enum-typed parameters/diagnostics, charging/release edge cases and canonical replay/Reset/save.

<a id="sequence-task-103"></a>

- [ ] **Finite-work impulse response.** Compute accepted powered impulses against actual constraints, contacts and carrier motion with one source debit and owned reaction/work result. Reject unsupported or insufficient-work requests atomically; prove coupled/fixed/free payload controls and exact restoration. No scalar release fallback exists.

<a id="sequence-task-104"></a>

- [ ] **World-owned release and joint limits.** Joint-range diagnostics, release and gameplay impulses enter through guarded typed world transactions. Preserve legal stop/release directions, linear/angular response, airflow boundary/work controls and complete ownership/rollback; direct body mutation is not a supported integration route.

<a id="sequence-task-105"></a>

- [ ] **Authored moving-sensor fixtures.** Construct moving basket/detector controls from authored initial conditions and actual world advancement, without mutating live snapshots. Verify translation/rotation, valid/missed capture or crossing, serialization, continuous boundaries and exact whole-machine replay/Reset.

<a id="sequence-task-106"></a>

- [ ] **Current-state airflow force integration.** Airflow body loads use current collider poses, owned state and resampling after shortened accepted intervals. Preserve continuous field boundaries, receiver timing, off-centre force/torque and source/receiver work. Fan parameters remain enums and all changes roll back atomically.

<a id="sequence-task-107"></a>

- [ ] **Shared airflow geometry and torque.** Typed airflow fields query current compound poses, occlusion and off-centre application points through the GPU geometry owner. Positive, wall-blocked, partial and moving-frame controls must agree with actual body force and torque; no held scene-force path remains.

<a id="sequence-task-108"></a>

- [ ] **Gate and shutter elastic return.** Gate/shutter return springs and dampers act from current joint state through generic physical laws. Preserve supplied opening, supply-loss closing, obstruction-stop/non-crushing behavior, endpoint/velocity bounds, authored cargo interaction and exact construction/rollback restoration.

<a id="sequence-task-109"></a>

- [ ] **Exclusive body ownership.** A physical body belongs to one simulation world/generation. Reject double ownership and unauthorized standalone motion mutation; candidate integration and rollback remain inside the owning transaction. Prove all command, geometry, lifecycle and presentation boundaries.

<a id="sequence-task-110"></a>

- [ ] **Current collider ownership for compliant laws.** Compliant laws resolve current owned collider geometry after valid replacement and cannot retain stale shapes. Prove replacement/no-change/invalid-update controls, impact-time updates, exact failed-tick replay and trampoline contact/restoration.

<a id="sequence-task-111"></a>

- [ ] **Moving-frame basket guidance.** Guidance is a typed frame-relative physical law evaluated from current receiver and payload state. Preserve strict/no-guide controls, declared force/clearance limits, continuous eligibility, capture semantics and exact restoration without held scene forces.

<a id="sequence-task-112"></a>

- [ ] **Passive windmill resistance.** Windmill damping acts on current relative hinge speed through the same physical transaction as conserved airflow transfer. Prove both wind signs, normal/capped/low-flow response, calm-air coast-down, external loading and rotated canonical Reset/save.

<a id="sequence-task-113"></a>

- [ ] **Geometry-aware trampoline forces.** Trampoline compliance follows declared contact geometry, spring potential, damping and accepted interval work. Detect continuous engagement and collider changes, preserve energy without injection, and prove off-centre/loading/miss, moving-bed and exact restoration controls.

<a id="sequence-task-114"></a>

- [ ] **Shared body drag.** Environmental and wind-chime drag use current body/frame motion in the physical evaluation, with correct force/torque and passive energy loss. Qualify every affected dynamic part, including powered-airflow/blocked controls, without sampled scene-force authority.

<a id="sequence-task-115"></a>

- [ ] **Shared axial damping.** Axial damping uses current relative joint velocity with signed force opposing motion, owned state and coherent rollback. Bellows declares spring plus damping; prove rest, compression/refill, loaded/connected-device and passive-work controls.

<a id="sequence-task-116"></a>

- [ ] **Bellows elastic return.** The bellows return spring is joint-bound, participates in the coupled physical solve and can refill against its declared load/environment. Prove loaded-plate and clear controls, coupled receiver/constraint behavior, signed damping, finite work and exact Reset.

<a id="sequence-task-117"></a>

- [ ] **Shared elastic world evaluation.** Joint-bound elastic potentials participate in candidate motion, accepted work and atomic commit for every elastic consumer. Preserve gravity/initial-charge/passive-energy controls, winding and release, coupled contact and exact whole-world restoration.

<a id="sequence-task-118"></a>

- [ ] **Elastic potential and interval work.** A shared elastic law supplies potential, instantaneous effort and consistent accepted interval work. Potential release, mechanical work and damping must balance within the current f32 contract; no held-force or hidden correction path may add energy.

<a id="sequence-task-119"></a>

- [ ] **Passive spring energy.** For all bodies/stores, account for initial elastic/kinetic/gravitational energy and external work over the whole Run. Zero-total-initial-energy/no-external-work controls cannot create energy or motion; stored energy increase must be paid by external work or reduction of other accounted mechanical energy. Test spring, bellows, gate and shutter consumers without post-hoc energy clamps.

<a id="sequence-task-120"></a>

- [ ] **Joint event limits during integration.** Locate stop/release and other joint boundaries in both motion directions before advancing past them. Events split or limit candidate intervals coherently and preserve signed spring/contact work, ordered event state and rollback; endpoint-only activation cannot miss an intermediate event.

<a id="sequence-task-121"></a>

- [ ] **Constraint-aware force response.** Physical response starts from current joint/contact support and includes coupled reactions. Prove signed and rotated stop-and-release, spring loading and no-drive controls under the f32 puzzle envelope; an unconstrained seed cannot become a committed unsupported motion.

<a id="sequence-task-122"></a>

- [ ] **Moving-carrier impacts.** Spring/bumper contact and finite-work responses use the actual translating/rotating carrier frame. Compare hit/miss and authored-motion controls, including glancing contact; preserve full body/path state, work accounting and exact construction Reset/save.

<a id="sequence-task-123"></a>

- [ ] **Relative impact velocities.** Impact laws consume resolved angular and point velocities from owned physical state. Carrier motion must enter relative approach, reaction and work; prove stationary/translated/rotated controls and identical outcomes under different render cadences.

<a id="sequence-task-124"></a>

- [ ] **Moving receiver capture and guidance.** Basket containment, guidance and residence use receiver point velocity and typed body identities. Prove translated and rotated frames, relative-speed/dwell and miss controls, moving-guide clearance, exact events and canonical construction Reset/save.

<a id="sequence-task-125"></a>

- [ ] **Moving trampoline contact frame.** Damping and approach use payload velocity relative to the moving bed's contact point. Prove authored assisted-bed versus stationary controls, contact/passivity, full body/path replay and exact Reset without using cosmetic mesh velocity as physical state.

<a id="sequence-task-126"></a>

- [ ] **Current blade-guide binding.** Gate/shutter control resolves the live owned guide after replacement, removal and Restore. Stale construction-joint objects cannot drive a blade; missing/foreign bindings reject, and obstruction/endpoints/supply-loss controls retain exact restoration.

<a id="sequence-task-127"></a>

- [ ] **Dynamic contact work sharing.** Analytical controls must establish dynamic contact momentum/work sharing, finite energy caps, reverse separation and exact replay. Frictional motor work, coupled loads and simultaneous supplies share the same physical ledger; no body can receive duplicated source work.

<a id="sequence-task-128"></a>

- [ ] **Guarded hinge impulses.** Hinge fixture and gameplay impulses use the typed simulation transaction and actual constraints. Preserve restitution/passivity, stopped-hinge and release controls, invalid ownership rejection and whole-world replay; do not bypass the world to force a test pose.

<a id="sequence-task-129"></a>

- [ ] **Complete affected integration checks.** Qualify the actual candidate's affected positive/control/boundary and lifecycle matrix, including stopped-hinge/passivity, pendulum energy, coaxial sweeps and rotated canonical saves. Focused subsets do not close engine release; old process handles and runs are not current tasks.

<a id="sequence-task-130"></a>

- [ ] **Impact-triggered motor budgets.** Impact-triggered actuation must respect current locked/released joint state, finite source work, miss controls and full body/effect/event replay. Friction and simultaneous supplies participate in the same accepted transaction; no post-lock impulse expectation may bypass a physical stop.

<a id="sequence-task-131"></a>

- [ ] **Contact-aware motor work.** Contact normals and friction constrain finite-work actuation together with joints. Prove signed blocked/release/braking, thin-obstacle pusher and impact-joint controls, simultaneous supplies, exact work reporting and rollback.

<a id="sequence-task-132"></a>

- [ ] **Owned scene-joint commands.** Every scene-admitted joint impulse becomes a typed owned simulation command. Prove transmission and lever outcomes, no-mutation rejection and contact-aware work; no direct scene/body impulse route remains in production.

<a id="sequence-task-133"></a>

**Superseded — no current action.** **Superseded bilateral-only helper; no current action.** The retired CPU bilateral-only response implementation is not a required GPU port. Current coupled contact/joint response, finite motor work and one-authority cleanup are specified by the active contact/drive contracts and standing requirements.

<a id="sequence-task-134"></a>

- [ ] **Direction-aware constrained motors.** Finite-work motors honor joint constraints, unilateral directions and boundary crossings. Prove stationary stops, hinge/slider braking and release, contact reactions, simultaneous supplies and exact replay; source work cannot be charged for forbidden motion or invented at release.

<a id="sequence-task-135"></a>

- [ ] **Admissible impulse directions.** One-way/release/transmission response must distinguish admitted directions and reject unsupported state without mutation. Integrate the response with finite source work and velocity-boundary/contact transitions; a standalone algebraic result cannot authorize unbudgeted time advancement.

<a id="sequence-task-136"></a>

- [ ] **Joint/contact coupling.** Contact and bilateral joint constraints share one coherent physical solution and accepted state. Prove constrained mass, changed identity order and exact replay, including loaded contacts and motor work, without a second post-processing solver that invalidates earlier constraints.

<a id="sequence-task-137"></a>

- [ ] **Physical integration fixtures.** Lever, rope, plunger, pusher and diagnostics fixtures use owned commands and shared physical advancement. Preserve loaded-contact, passivity, negative/control and exact restoration cases; no fixture may obtain success through direct live-body mutation.

<a id="sequence-task-138"></a>

- [ ] **Internal-body construction state.** Canonical construction saves include declared internal-body initial motion using typed roles and one orientation/state representation. Reject malformed roles/ranges and obsolete schemas atomically; prove plunger/lifecycle restoration without a separate Reset velocity cache.

<a id="sequence-task-139"></a>

- [ ] **Authored initial motion persistence.** Declared initial linear/angular motion survives canonical save/load and Run/Reset for ball, weight and internal bodies. Validate vector dimensions/ranges/finite values and exact canonical orientation, keeping saved construction distinct from running physical state.

<a id="sequence-task-140"></a>

- [ ] **Scene command boundary closure.** Every lever, diagnostic, plunger, geometry and rope caller uses typed owned physical commands. Verify actual outcomes and rejection/rollback through the current transaction; migrating a call signature alone does not qualify behavior.

<a id="sequence-task-141"></a>

- [ ] **Independent coupling groups.** Exactly independent physical coupling groups must not contaminate one another's scale, state or results. Prove disparate admitted scales, identity/order changes and shared-body controls, plus bellows-to-chime positive and wall-blocked cases, within the current f32 representation.

<a id="sequence-task-142"></a>

- [ ] **Finite Coulomb response.** Coulomb contact evaluation must guard operands/intermediates before overflow or invalid operations and preserve passivity at admitted scales. Prove trampoline and mixed-scale bellows controls, finite saturation, coupled actuator behavior and atomic failure; no speculative fallback solver.

<a id="sequence-task-143"></a>

- [ ] **Grazing and small-constraint admission.** Admit only representable f32 constraint/contact ranges, including grazing and low-force cases. Unsupported tiny inputs reject explicitly rather than depending on subnormal persistence. Prove stable gate/trampoline contact, signed blocked/release behavior and no artificial energy.

<a id="sequence-task-144"></a>

- [ ] **Physical gate collision fixtures.** Gate/shutter collision controls use authored moving cargo, real typed supply and owned impacts. Prove opening/closing, endpoint/speed and obstruction-stop/non-crushing outcomes with exact Reset/replay; runtime insertion or teleport cannot counterfeit contact.

<a id="sequence-task-145"></a>

- [ ] **Gameplay tick lifecycle.** Typed lifecycle guards reject recursive Step, Reset and Load without corrupting state. Completion notification occurs only after a whole committed tick; all domain state, events and loads participate in rollback. Prove reentrancy, failure/cancellation and actual UI restoration.

<a id="sequence-task-146"></a>

- [ ] **Coupled acceleration constraints.** Equality blocks and zero-width locks must produce coupled acceleration consistent with all participating bodies. Qualify spring convergence, unilateral motor work, pendulum energy and rotated save/load within the current f32 budget; do not require a particular CPU factorization.

<a id="sequence-task-147"></a>

- [ ] **Constraint classification and response.** Classify equality and inequality declarations explicitly; jointly solve their momentum/reaction and acceleration response with contact. Preserve unilateral actuation and atomic rejection of inadmissible systems.

<a id="sequence-task-148"></a>

- [ ] **Arbitrary coupled pre-actuation response.** Qualify coupled equalities on two-, eight- and thirty-two-body rings, including more than six equations, zero-work stationary stops, springs and unilateral/simultaneous supplies. A hidden pair-only or fixed small-system solver is not an acceptable current path.

<a id="sequence-task-149"></a>

- [ ] **Live driven loops.** Single-link, redundant and locked world loops must respect physical work and exact replay. Stationary stops add essentially zero mechanical work; unilateral and simultaneous supplies share the connected response.

<a id="sequence-task-150"></a>

- [ ] **Redundant constraint admission.** Dependent constraint equations must preserve the consistent coupled solution; inconsistent targets must reject before outputs or world state change. Qualify driven-loop stress, declaration-order controls and the current precision envelope without mandating an obsolete CPU rank algorithm.

<a id="sequence-task-151"></a>

- [ ] **Reentrant world transaction safety.** Reject unsupported reentrant external impulses, nested stepping, snapshot capture/restore, joint replacement, surface replacement and collider updates. Exercise both caught and propagated rejection for each category. Restore the complete world, permit identical replay and safe reuse after recoverable rejection, and fault unrecoverable restore failures.

<a id="sequence-task-152"></a>

- [ ] **Independent mechanical islands.** An unrelated belt or other disconnected mechanism must not alter another island's effective mass, work or response. Qualify driven redundant loops, unilateral motors, stationary stops, springs and rotated save/load.

<a id="sequence-task-153"></a>

- [ ] **RGB filter physical impact.** Filter impact, rebound and exact Reset/replay must follow shared physics. Filter choices remain enums through configuration and genuine serialization boundaries; changing construction settings during a Run rejects without mutating the runtime payload.

<a id="sequence-task-154"></a>

- [ ] **Owned gameplay impulses.** Cannon and other gameplay impulses target registered dynamic bodies through the owned world at an admitted transaction boundary, including supported Idle application. Reject wrong-phase, invalid/foreign/non-dynamic targets and reentrant mutation atomically; no part writes physical velocity directly.

<a id="sequence-task-155"></a>

- [ ] **Signed guide and transmission work.** Qualify signed hinge/slider actuation with open, engaged and ratio-coupled transmissions, stationary stops, unilateral/simultaneous supplies and redundant guides. Work follows actual connected motion and paired reactions.

<a id="sequence-task-156"></a>

- [ ] **Arbitrary-body effective mass.** Compute coupled response, work and rejection for two-, eight- and sixteen-body systems, including more than six equations. The WASM SIMD implementation must preserve physical coupling without a pair-only shortcut.

<a id="sequence-task-157"></a>

- [ ] **Multi-body finite-work impulses.** Finite-work impulses share actual coupled effective mass and reaction across all participants. Qualify a three-body work/momentum case, braking, atomic rejection and exact replay.

<a id="sequence-task-158"></a>

- [ ] **Connected actuation energy.** Uncoupled and two-mass kicks must reach the physically admissible response under their finite work budget. A stationary stop must not consume fictitious mechanical work; solve connected motion and accounting together, without report-only refunds or component-specific bypasses.

<a id="sequence-task-159"></a>

- [ ] **Wound-spring winding and release.** Qualify inward winding with finite shaft inertia and outward release through open and engaged transmissions. Preserve the finite elastic store, physical load response and teaching behavior without a prescribed launch velocity.

<a id="sequence-task-160"></a>

- [ ] **Construction preparation lifecycle.** Preparing construction while running or paused must reject for root and internal bodies without recapturing runtime poses. Preserve canonical orientation and the whole transaction.

<a id="sequence-task-161"></a>

- [ ] **Construction-owned rope capture.** Rope sockets and routes derive from captured owned construction geometry. Preparing while running or paused rejects; no unused historical helper is required as a second authority.

<a id="sequence-task-162"></a>

- [ ] **Typed generic point effects.** Point force and torque declarations and fixture choices remain enum-typed through the domain. Chimes use the generic physical equations; qualify force, sensing, replay, invalid choices and exact Reset.

<a id="sequence-task-163"></a>

- [ ] **Gate motion and obstruction.** Powered gate opening is monotonic between declared endpoints. Qualify obstruction from all relevant approaches, cannon clearance and passage through the moved gate, plus exact Reset; cannon success alone does not qualify the gate.

<a id="sequence-task-164"></a>

- [ ] **Cannon loading boundaries.** Qualify charged and unpowered loading, trigger-time admission, late-arrival rejection and reload obstruction. Save/load and Reset restore the exact construction without preserving transient chamber state.

<a id="sequence-task-165"></a>

- [ ] **Stationary stop work.** A stationary slider stop adds essentially zero mechanical work within the declared f32 accounting budget. This holds with signed generic contacts and transmissions without component bypasses, energy clamps or report-only refunds.

<a id="sequence-task-166"></a>

- [ ] **Captured configuration immutability.** Configuration changes to captured root or internal bodies reject before mutation while running or paused. Reset restores the authored settings and physical declarations.

<a id="sequence-task-167"></a>

- [ ] **Owned rope presentation.** Rope artwork reads the required world route, owned knots, endpoints and pulley frames. Qualify finite-rim routing and immunity to displayed-pose corruption during Run, pause and Reset.

<a id="sequence-task-168"></a>

- [ ] **Owned runtime signatures.** Runtime observations/signatures use owned position, velocity, angular motion, motion type, participation and mass-centre state, not scene visibility. Include joints, components and current collider geometry in replay qualification.

<a id="sequence-task-169"></a>

- [ ] **Required typed physics parameters.** Require every declared physics parameter and typed selector; reject missing, unknown and undefined values without string fallbacks or inferred defaults. Author drag and buoyancy explicitly for each supported resource/body type.

<a id="sequence-task-170"></a>

- [ ] **Construction-only resizing.** Wall and pipe resizing must atomically update construction geometry and proxies. Reject resizing during Run or pause; exact Reset preserves the accepted construction.

<a id="sequence-task-171"></a>

- [ ] **Owned blade orientation.** Blade forces derive from owned orientation, never the displayed transform. Straight and rotated controls must remain physically identical under presentation corruption.

<a id="sequence-task-172"></a>

- [ ] **Owned pusher geometry.** Pusher root/head colliders and the finite-mass telescoping shaft follow owned physical state and atomic collider updates. Presentation cannot write physics. Qualify thin obstacles, low-force support, deformation and exact Reset.

<a id="sequence-task-173"></a>

- [ ] **Typed construction body identity.** Capture construction poses and declared initial motion for every root and internal body under typed construction identities. Run and Reset must restore the exact corresponding body, settings and links.

<a id="sequence-task-174"></a>

- [ ] **Construction-only tube snapping.** Tube snapping validates ownership and construction state. Reject running, paused and foreign-part operations atomically; Reset restores exact eligibility and placement.

<a id="sequence-task-175"></a>

- [ ] **Immutable Reset snapshots.** The owned Reset snapshot remains immutable. Exported observations are detached copies and cannot mutate live or paused state.

<a id="sequence-task-176"></a>

- [ ] **Authored mechanical controls.** Qualify fixed relay/latch controls, wall initial velocity authored before Run, three rotations and construction disconnection. Supply loss permits physical coasting without adding work or erasing momentum.

<a id="sequence-task-177"></a>

- [ ] **Counter and gate crossing controls.** Counters use preauthored motion and qualify two-event, negative and entity-order controls with exact Reset. Gates qualify four approaches and high-speed obstruction, including the required stop/non-crush behavior.

<a id="sequence-task-178"></a>

- [ ] **Read-only rope membership.** Route membership and constraint identities come from one physical capture and remain read-only. Running/paused observations and Reset must preserve the correct route identities.

<a id="sequence-task-179"></a>

- [ ] **Owned connection collections.** Expose read-only connection views and detached snapshots; edits pass through guarded construction operations. Reset/load preserve stable views and topology without relying on live part ordering.

<a id="sequence-task-180"></a>

- [ ] **Fixed optical/electrical controls.** Gate and shutter supply changes use authored wiring/latch roles, typed through the domain. Missing contacts reject; removing a live delay input rejects atomically and Reset restores the fixed construction.

<a id="sequence-task-181"></a>

- [ ] **Construction electrical wiring.** Connect/disconnect are validated construction operations. Relay and motor supply-loss tests use fixed latch controls and preserve physical momentum/coasting.

<a id="sequence-task-182"></a>

- [ ] **Owned light, sound and solar inputs.** Laser, speaker and solar power follow fixed contacts and owned poses/participation, including aperture, occlusion and range. Presentation corruption is a meaningful negative/control.

<a id="sequence-task-183"></a>

- [ ] **Clock and latch topology.** Qualify clock hold and both latch/switch states with fixed authored wiring. Running/paused disconnect rejects and Reset restores the exact topology.

<a id="sequence-task-184"></a>

- [ ] **Immutable typed connection specifications.** Connection specifications are immutable and enum-typed; replacements validate atomically and reject undefined domains. No caller mutates a live connection collection.

<a id="sequence-task-185"></a>

- [ ] **Read-only placed-part membership.** Expose read-only placed-part membership and validate catalogue/custom attachment. Live reordering cannot change physical outcomes or bypass ownership.

<a id="sequence-task-186"></a>

- [ ] **World membership lifecycle.** Guard add, remove, paused Start and connection operations; expose read-only bodies. Reject foreign-world removal and preserve the complete transaction on failure.

<a id="sequence-task-187"></a>

**Superseded — no current action.** **Superseded process polling; no current action.** The instruction to poll a particular September native process is obsolete and creates no current implementation task. Current sweep, passivity, pendulum and rotated-save behavior remains in the corresponding current criteria.

<a id="sequence-task-188"></a>

- [ ] **Declared initial motion restoration.** Reset restores placed and internal bodies' declared construction initial motion, never captured runtime motion. Save/load, topology and failed operations remain transactional.

<a id="sequence-task-189"></a>

- [ ] **Owned body telemetry.** Expose owned physical position, velocity, mass-centre offset and typed sub-body participation during Run and pause. Displayed scene state is not physical authority.

<a id="sequence-task-190"></a>

- [ ] **Construction versus presented velocity.** Initial velocity is an authored construction value; runtime presentation is read-only. Running/paused initial-motion mutation rejects; current callers, persistence and Reset preserve this boundary.

<a id="sequence-task-191"></a>

- [ ] **One shared friction authority.** All callers use the generic contact/friction equations; no per-part friction helper or second numerical authority remains. Qualify topology, initial/runtime motion boundaries and all supported part outcomes.

<a id="sequence-task-192"></a>

- [ ] **One mechanical energy network.** Authored sockets bind owned shared guides, so source, route and load participate in one motion/work calculation. Qualify spring winding/release, clutch power loss/coasting/re-engagement and loops without copied speed/torque or independent downstream motor commands.

<a id="sequence-task-193"></a>

- [ ] **Finite-inertia rotary transmission.** Implement the complete [rotary transmission contract](../rotary-transmission-parts.md): reverse gearbox and clutch shafts, bidirectional coupling, typed engagement, external socket binding, winding linkage, exact lifecycle and save/load.

<a id="sequence-task-194"></a>

- [ ] **Physical source rotors.** Motor and windmill sources use finite-inertia owned rotors, guides and accepted torque/work. Source and load share one calculation; supply loss removes effort without erasing momentum. Qualify rotated save/load, actual committed shaft events/poses and source/load controls.

<a id="sequence-task-195"></a>

- [ ] **Typed guide dependencies.** Mechanical parts declare typed guide dependencies and bind every socket to the owned guide. Missing/cyclic dependencies and dangling runtime replacements reject atomically; no scalar speed/work propagation fallback remains.

<a id="sequence-task-196"></a>

- [ ] **Prescribed-motion obstruction.** Reject conflicting fixed/kinematic and independent prescribed motion atomically, including collider enabling/replacement. Handle shared-frame children and disabled colliders explicitly. Assistance must plan feasible motion, provide player feedback and preserve whole-world rollback.

<a id="sequence-task-197"></a>

- [ ] **Owned assistance execution.** Advance assistance through owned continuous trajectories with body/COM offsets, snapshot cursors, derivative bounds and contact/joint acceleration reactions. Qualify pusher support, pendulum energy, obstruction, exact Reset/replay and whole-tick rollback.

<a id="sequence-task-198"></a>

- [ ] **Bounded varying trajectories.** Prescribed motion supports actual varying acceleration, interior extrema, angular bounds and multiple full turns. Capture immutable profiles and owned cursors, map compound/COM motion correctly, advance exactly within declared f32 budgets and reject unsupported inputs explicitly.

<a id="sequence-task-199"></a>

- [ ] **Physics-owned rope and impact effects.** Rope constraints and impact-local effects execute through the shared world. Assistance corrections must actually advance as owned continuous motion; direct scene pose/velocity writes cannot substitute for execution.

<a id="sequence-task-200"></a>

- [ ] **Contact participation.** Bell and trampoline forces/events depend on declared physical participation, not rendered visibility. Disabling the trampoline clears its active contacts; qualify ownership, re-enable, exact Reset and replay.

<a id="sequence-task-201"></a>

- [ ] **Owned optical output accounting.** Routed optical outputs use captured shared-body outlet poses and physical energy accounting. Combiner and logic presentation transforms cannot become a second optical authority.

<a id="sequence-task-202"></a>

- [ ] **Compound pressure-plate contacts.** Pressure plates use generic shared compound contacts. Qualify gate timing, supported shape/query boundaries, negative/control, exact Reset and real UI behavior without rendered-position/radius proximity logic.

<a id="sequence-task-203"></a>

- [ ] **Initially disabled bodies.** Every authored body retains a shared identity even when initially hidden. Collision/participation is an explicit validated typed declaration; qualify initially disabled detectors, enable transitions and lifecycle restoration.

<a id="sequence-task-204"></a>

- [ ] **Detector and escape ownership.** Detectors and escape checks read solved positions and participation. Qualify initially hidden bodies, positive presentation-distortion controls and exact lifecycle restoration.

<a id="sequence-task-205"></a>

- [ ] **Physical cannon energy.** Cannon chamber admission, loading and launch use owned physical state and finite energy release under current f32 authority. Fixtures author mutations before Start; qualify obstruction, replay, trigger controls and save/load.

<a id="sequence-task-206"></a>

- [ ] **Owned network sampling and capture.** Networks, anchored airflow samples, emitters and basket guidance/capture read shared body state. Qualify all caller migration, numerical/work limits and exact Reset without rendered-pose authority.

<a id="sequence-task-207"></a>

- [ ] **Owned runtime queries.** Sweeps, traces and snapshots read owned poses, participation and current registered collider metadata. Collider replacements commit query metadata atomically and snapshots restore it exactly; migrate every emitter/receiver/aperture caller.

<a id="sequence-task-208"></a>

- [ ] **Shared hollow separation queries.** Tube, frustum and bend geometry use shared compound separation/query declarations and independent analytic witnesses. Qualify exterior-wall and current collider controls without retired per-shape surface algorithms.

<a id="sequence-task-209"></a>

- [ ] **One static shell collision path.** All static tube/frustum shell callers use shared compounds. Qualify long coaxial convergence and independent analytic controls; no retired static intersection fallback remains.

<a id="sequence-task-210"></a>

- [ ] **One hollow continuous collision path.** Tube and funnel motion use bounded shared compounds and captured trajectories. Qualify long coaxial sweeps, oversized spring loads and release energy inside the [game-grade envelope](../gpu-f32-physics.md#game-grade-envelope); the hollow SDF/compound collider lives in the shared pair table, with no shell-specific sweep path.

<a id="sequence-task-211"></a>

- [ ] **One shared sweep path.** Moving/rotating sphere and box callers use generic captured trajectories and continuous collision. Qualify long axial tangency and initial overlap; no old shape-specific sweep/result API remains.

<a id="sequence-task-212"></a>

- [ ] **Shared impact dispatch.** All spring and bumper effects come from actual shared impacts; impulses and collider changes are transactional world commands. Qualify glancing spin and every supported bumper difficulty, without a scene-owned contact hook.


<a id="sequence-task-213"></a>

- [ ] **Physical conveyor transport.** A finite-inertia shaft/carrier drives declared material contacts with equal/opposite reactions; multiple loads share that shaft and unpowered transport coasts. Side/underside contacts remain ordinary. Qualify slip, finite work, solved presentation, exact lifecycle and mechanical coupling.

<a id="sequence-task-214"></a>

- [ ] **Multi-participant driven contacts.** Generic impulse/friction and acceleration contacts couple sparse gradients across every participant, including finite-energy shaft reaction torque. Predict material-slip boundaries and accepted mechanical work continuously; no pair-only or direct conveyor callback authority remains.

<a id="sequence-task-215"></a>

- [ ] **Shared contact and motion authority.** Ratchet and contact behavior use shared bodies, constraints and replay. Remove active direct scene-velocity resolution and obsolete motion hooks; preserve conveyor, sweep, work, query and transaction behavior through the current world.

<a id="sequence-task-216"></a>

- [ ] **Physical lever integration.** Lever beam geometry, finite-mass bodies and frame joints have one shared authority. Qualify support convergence, stop-contact work, seeded energy, exact Reset and generic powered contacts without a separate hinged-body solver.

<a id="sequence-task-217"></a>

- [ ] **Physical wind chimes.** A finite-mass sail/clapper, ball-socket pivot and individually identified convex tube bodies own chime motion/contact. Qualify positive airflow/coast, every tube/load permutation, analytic drag, physical contact identity, exact Reset and rollback without a local pendulum/contact solver.

<a id="sequence-task-218"></a>

- [ ] **Physical bellows.** A finite-mass slider plate with return spring and pumping resistance produces airflow from actual compression. Qualify loading/unloading/refill cycles, continuous force/airflow work, rollback and intended campaign loads; no impact-energy target or scripted plate authority remains.

<a id="sequence-task-219"></a>

- [ ] **Physical wound spring.** Winding and release use finite-work actuation, solved head motion and shared latch/ratchet/stops. Qualify 0.5 kg and 2 kg payload masses, no-trigger controls, winding obstruction, release energy, continuous forces and complete rollback without direct head-velocity or kinematic launch APIs.

<a id="sequence-task-220"></a>

- [ ] **Physical trampoline membrane.** The compliant membrane derives force/torque from actual support geometry and rotational effective mass. Spheres, rotated boxes and compounds share one query path. Qualify rigid-rim contacts, continuous compliant entry, general-load visuals and complete rollback.

<a id="sequence-task-221"></a>

- [ ] **Physical domino propagation.** A finite-mass box with declared offset centre of mass tips through shared contact. Output observes actual tipping and signal input rejects. Qualify close and separated gaps, grounded chains, all authored/reference placements, connections, assistance and exact Reset without timed/proximity propagation.

<a id="sequence-task-222"></a>

- [ ] **Physical linear pusher.** The finite-mass head, slider, bounded motor, locking constraints and finite-mass telescoping shaft share continuous physics and observed presentation. Qualify thin obstacles, low-force vertical support, stationary-stop work, deformation, controllers/energy transactions and exact Reset without direct cargo velocity writes.

<a id="sequence-task-223"></a>

- [ ] **Persistent shared-world integration.** All current components, queries, effects, forces, stores and controls use the persistent WASM SIMD physics world, delivered through the roadmap's ENGINE-CORE-2 and element slices and audited at LEGACY-0. Gates/shutters have finite-mass sliders, bounded motors and return springs; remove remaining active legacy helpers/hooks. Native checks supplement, and cannot replace, current real-UI/worker/lifecycle/performance qualification.

<a id="sequence-task-224"></a>

- [ ] **Constrained integration accuracy.** An authored-friction lever, pendulum and rope-loaded machine must complete intended motion without nonconvergence or unbounded energy error. Exact Run/Reset repeats the outcome. Integrate all state/effects/forces/stores/constraints through the current world; no damping/clamps may disguise accounting defects.

<a id="todo-017"></a>

<a id="sequence-task-225"></a>

- [ ] **Complete shared-world behavior.** All existing part families use one persistent shared world for collisions, motors, constraints and effects. Qualify body-local ball sockets/hinges/sliders, declared travel, endpoint and ordered routed ropes including round-sheave tangents/arcs, multi-body/repeated attachments, continuous correction paths that discover new obstacles, bounded hollow geometry, sparse caches and exact rollback/replay. Stops, rope extension and finite-work commands share the world event clock without early braking or duplicate substep work. A cross-family machine must repeat Run/Reset; catalogue-scale dense islands, body broadphase, supported-device performance and every affected part's UI proof remain required.

<a id="physics-accuracy"></a>

### 0.2 Contact, force and constrained-motion accuracy

**Design acceptance for every item below:** [common gates](#design-acceptance) + [SIM](#accept-simulation); additional profiles apply to cross-cutting requirements.

**When:** Before production authority and new physics-dependent parts.

**Gameplay outcome:** Pendulums, sliding/rolling balls, loaded levers and constrained mechanisms respond consistently to real loads and conserve/dissipate energy according to their authored materials.

Retain the two pendulum-energy failures and rope startup/Reset failure until fresh evidence resolves them. Do not use damping, relaxed tolerances, skipped tests or larger budgets to conceal a solver defect.

<a id="todo-005"></a>

<a id="sequence-task-226"></a>

- [ ] **Supported sliding and near-cancelling motion.** Players can combine near-cancelling spins and supported sliding loads without a stalled Run. Qualify continuous slip bounds, nonlinear support and work limits inside the [game-grade envelope](../gpu-f32-physics.md#game-grade-envelope); the generic WASM SIMD solver's friction is the only path, with no CPU fallback or per-element solver.

<a id="todo-007"></a>

<a id="sequence-task-227"></a>

- [ ] **Authored contact materials.** Every body slot declares typed friction/restitution material, captured consistently for production and tests. Balls roll, slide and rebound according to the visible authored material, including lever/rope interactions; qualify slip boundaries and current work/error limits.

<a id="todo-009"></a>

<a id="sequence-task-228"></a>

- [ ] **Friction transitions and integration energy.** Qualify sliding-to-rolling reversals, stops before a step midpoint, multiple transitions, high-spin feature boundaries and exact replay. Pendulum energy stays within the approved complete-Run budget across supported simulation rates without clamps or tuned-away failures.

<a id="todo-011"></a>

<a id="sequence-task-229"></a>

- [ ] **Continuous force trajectories.** Continuous translation under force and angular momentum under torque participate in captured curved collision and joint sweeps. Contact/joint support and external loads share the accepted trajectory and work accounting; a one-off force kick is not a substitute.

<a id="todo-013"></a>

<a id="sequence-task-230"></a>

- [ ] **Joint direction boundaries.** Active ratchets and unilateral joint directions enforce continuous allowed motion under contact, external force and finite-work motors. Qualify direction changes, energy, exact lifecycle and shared-world execution.

<a id="physics-cutover"></a>

### 0.3 Make the shared world authoritative

**Design acceptance for every item below:** [common gates](#design-acceptance) + [SIM](#accept-simulation); additional profiles apply to cross-cutting requirements.

**When:** After the numerical contracts are sound; migrate dependencies together.

**Gameplay outcome:** Balls remain inside hollow routes, moving geometry blocks them at its visible pose, ropes carry real loads, and a charged mechanism spends only its available energy. Every part shares the same body and event state.

Complete body/pose/mass ownership before effects and constraints depend on it. The following criteria describe the sole shared WASM SIMD physics world and its current callers.

<a id="todo-031"></a>

<a id="sequence-task-231"></a>

- [ ] **Shared body and shape ownership.** Reusable body-local shapes, captured poses, authoritative mass/inertia and dynamic state have one owner. Dynamic envelopes/proxies share their body; beams and moving actuators declare independent bodies where their physical motion requires it.

<a id="todo-029"></a>

<a id="sequence-task-232"></a>

- [ ] **Extensible typed body slots.** Parts declare typed extensible body identities and enum query/coordinate policies. Qualify independent gate/shutter geometry, rotated travel, controls and exact Reset with one authoritative dynamics/shape owner.

<a id="todo-027"></a>

<a id="sequence-task-233"></a>

- [ ] **Explicit initial body dynamics.** Every slot requires explicit motion type, mass, inertia and initial velocity. Balls, weights, plungers, beams and actuators bind persistent shared bodies, materials, constraints, effects, forces and stores; qualify catalogue contact and replay.

<a id="todo-025"></a>

<a id="sequence-task-234"></a>

- [ ] **Owned joint binding.** Typed owner/slot references bind persistent bodies to frame joints. Qualify lever pivot/limits, actual ball/beam hit and miss, rotated stops, exact replay and every declared constraint without a second stepping path.

<a id="todo-019"></a>

<a id="sequence-task-235"></a>

- [ ] **Transactional joint lifecycle.** Validated joint replacement and collision policy operate atomically at admitted boundaries. Scheduled impact-time detach/reattach must preserve motor budgets, reject conflicting changes and restore exactly. Latches can release/reconnect at the intended instant without duplicate impulses.

<a id="todo-039"></a>

<a id="sequence-task-236"></a>

- [ ] **Transactional collider revisions.** Typed collider declarations and revisions update atomically, invalidate only affected contacts and restore exact snapshots. Scheduled impact-time updates preserve shared state/constraints and reject incompatible changes before mutation.

<a id="todo-037"></a>

<a id="sequence-task-237"></a>

- [ ] **Typed continuous force inputs.** Components submit typed force/torque declarations to shared aggregation. Preserve finite source-work accounting and authoritative body state; no independent integration path remains.

<a id="todo-041"></a>

<a id="sequence-task-238"></a>

- [ ] **Impact-time effects.** Typed impulses and simultaneous contact delivery share world event time and effect-state rollback. Current component state/events use this transaction; no direct scene callback owns physical response.

<a id="todo-021"></a>

<a id="sequence-task-239"></a>

- [ ] **Guided plunger latch and ratchet.** A finite-mass plunger binds shared slider/latch/stop frames. The ratchet continuously prevents reverse motion; winding visibly retracts the loaded head, release spends stored charge once and a blocked stroke remains blocked until geometry permits it. Qualify rotated payloads, replay and finite energy.

<a id="todo-023"></a>

<a id="sequence-task-240"></a>

- [ ] **Authored routed ropes.** Canonical socket routes bind shared coupled constraints with collision. Qualify tension/mass ratios, slack/open controls, connected-load collision, exact replay and finite-radius sheaves. A constructed route visibly carries its load and Reset restores the exact construction.

<a id="todo-043"></a>

<a id="sequence-task-241"></a>

- [ ] **Finite-radius conduit contact.** Generic sweeps and shared bodies preserve finite-radius pipe/bend traversal and obstruction. Qualify affected gameplay and numerical boundary cases without a separate sphere response path.

<a id="todo-249"></a>

<a id="sequence-task-242"></a>

- [ ] **Physical hollow conduits.** A ball traverses a genuinely hollow tube/frustum/bend under shared-world gravity, momentum and contact, can jam against obstructions and remains visibly on its physical path. No filled convex substitute, teleportation or scripted constant-speed route is allowed.

<a id="todo-308"></a>

<a id="sequence-task-243"></a>

- [ ] **Shared optical geometry.** Optical tracing uses the generic owned collision/query geometry under WASM SIMD physics authority: finite mirrors/apertures, nearest-hit occlusion, moving balls, wall oriented boxes and explicit opaque frames/mounts. No conflicting second collision world is allowed.

<a id="todo-434"></a>

<a id="sequence-task-244"></a>

- [ ] **Finite load-aware mechanical work.** Loaded pushers, lifts and launchers consume only available work and can stall honestly. Budget stores, actuation, passive loss, return/rearming and simultaneous branches without duplicating source energy; signed belt speed alone does not prove force limits, stalls or loaded lifting. Reject unsupported combinations explicitly.

<a id="todo-470"></a>

<a id="sequence-task-245"></a>

- [ ] **Finite-radius rope consistency.** Physical rope length, slack, wheel travel and artwork must agree with true groove tangents/arcs, including close-up Run/Reset, heavily rotated/multiple pulleys and near-axis approaches. Endpoint motion cannot create unexplained tension jumps.

<a id="physics-exit"></a>

### 0.4 Remove obsolete paths and prove the playable build

**Design acceptance for every item below:** [common gates](#design-acceptance) + [SIM](#accept-simulation); additional profiles apply to cross-cutting requirements.

**When:** Exit gate for priority 0.

**Gameplay outcome:** An ordinary player can complete affected puzzles in a freshly published browser build, stop at any supported point and recover the exact construction with Reset.

Require focused native and real-UI positive/control proofs for every affected part, current save/load restoration and measured catalogue/mobile performance risks. A green reference solution or older web bundle does not close the cutover.

<a id="todo-035"></a>

<a id="sequence-task-246"></a>

- [ ] **Construction validation.** Reject overlapping/invalid construction with a visible explanation before Run, using shared compound geometry including bends and hinges. Nearby valid placements start normally and always Reset; no old solver is used for validation.

<a id="todo-045"></a>

<a id="sequence-task-247"></a>

- [ ] **Shared occlusion queries.** Generic owned rays consistently govern optical, solar, sound and airflow occlusion. Every affected part requires positive/blocked controls and current UI proof; no separate per-shape trace authority remains.

<a id="todo-033"></a>

<a id="sequence-task-248"></a>

- [ ] **Remove obsolete numerical paths.** All current parts use the sole shared WASM SIMD motion/response authority. Remove obsolete body-flight, hinge, rope, guided-motion, shape-specific sweep and isolated impulse paths and their current callers together, without compatibility adapters; prove affected playable behavior and lifecycle.

<a id="generic-interaction-contract"></a>

### 0.5 Generic physical interactions and optional subsystem execution

**Design acceptance for every item below:** [common gates](#design-acceptance) + [SIM](#accept-simulation); additional profiles apply to cross-cutting requirements.

**Scope — owner requirement, 28 September 2026:** Every puzzle element has an individual specification. An interaction between elements is a separate tracked process/integration requirement, never a combined part definition. Campaign tables and family indexes are navigation summaries only. Existing grouped element obligations are superseded by the [individual element register](#individual-element-register) and [thermal element register](#thermal-elements); historical completed evidence retains its original wording/status.

<a id="sequence-task-249"></a>

- [ ] **Generic capability-based interaction architecture.** Declare geometry, material state, conserved stores, ports and constitutive models through typed data/capabilities. Shared solvers discover eligible participants using those capabilities. No part-name/type-pair switches, catalogue-ID comparisons, scenario/level IDs, hidden tags or special-case callbacks may implement physics. A physical law may depend on measured material properties or a validated constitutive model; it must work for any body declaring that model. Part assemblies configure generic primitives and observe results; they do not set another part's velocity, temperature, phase or activation directly.

**Successful outcome:** Substitute a different compliant body/source/container without changing solver code and obtain the predicted response. Audit existing interactions and forward-refactor violations with their callers/tests; do not claim current engine compliance. Reject missing/unsupported material or port contracts explicitly, with no fallback or compatibility path. Closed choices remain enum-typed through definitions, dispatch, collections, UI and tests; extensible identities remain strongly typed at boundaries.

<a id="sequence-task-250"></a>

- [ ] **Generic thermodynamic state and energy ledger.** Add mass/species inventory, enthalpy, temperature derived from state, phase fractions, heat capacity, latent heat, conductivity, emissivity/absorptivity and supported pressure-volume constitutive relations. Interfaces expose finite energy/matter transactions, not part-specific heating methods. Combustion consumes finite reactants and accounts for products/released chemical energy; electricity, light and mechanical dissipation contribute through the same ledger.

**Successful outcome:** Closed fixtures conserve mass and energy within a stated solver tolerance. Open fixtures balance named source/sink boundary fluxes. Heat flows down the modeled temperature gradient; extracting latent heat freezes material and adding latent heat melts/boils it without free energy. Supported pressure changes alter phase conditions; unsupported states fail explicitly. Do not silently apply room-pressure boiling constants to pressurized vessels.

<a id="sequence-task-251"></a>

- [ ] **Deterministic generic subsystem coupling.** Read a stable state, propose bounded transfers/forces, resolve shared supply limits and commit in a defined order. Use coupled iteration/substeps or event resolution where needed for pressure, phase, contact or reaction feedback. Move transported mass with its enthalpy/species; update density, buoyancy, mass/inertia, collision topology and port connectivity through shared lifecycle APIs. Preserve exact initial construction on Reset; save supported dynamic state only through explicit validated contracts.

**Successful outcome:** Two consumers cannot each spend the same heat/fuel/charge; simultaneous events and part enumeration do not create energy. Test thermal expansion against constraints, freezing in a container, depletion/extinction and phase transitions during transport. Bound reaction/fragment/phase topology growth and preserve failures; no per-part fix hides a shared solver defect.

<a id="subsystem-dependency-closure"></a>

<a id="sequence-task-252"></a>

- [ ] **Disable whole unused subsystems only from a proven dependency closure.** Model subsystem IDs/dependencies as enums/typed capability sets, including mechanical, electrical, fluid, gas, thermal, chemical, optical, acoustic and ionizing-radiation services. At authoring/load/Run, compute conservative transitive closure from placed/fixed elements, permitted inventory, material models, environment/boundaries, goals/sensors, supported difficulty profile and every reachable phase/reaction/spawn/assembly product. Keep thermal radiation distinct from ionizing radiation while accounting for energy consistently. Cache a content/profile-versioned capability manifest; rebuild when any input changes.

**Successful outcome:** A genuinely mechanical-only level omits unrelated subsystem stepping/storage. A mechanical level with frictional heating, temperature-dependent friction or thermal goals retains the relevant heat services; a lens that can ignite fuel retains optical, thermal and chemical dependencies even before a flame exists. A frozen block that can melt retains fluid capabilities. Unknown reachability keeps the potentially needed subsystem enabled or rejects the unsupported authoring input; uncertainty never means safe-to-disable. Free Play recomputes closure as inventory/construction changes, and Run cannot mutate its capability set silently.

<a id="sequence-task-253"></a>

- [ ] **Prove pruning and abstraction independently of individual parts.** Compare full-enabled and pruned execution at equal simulated time across every supported profile, including phase/reaction products and rare threshold crossings. Test that editing, loading, copying an assembly or changing goals invalidates the manifest. Separately prove polymorphic substitution with multiple existing and new participants per interaction process.

**Successful outcome:** Supported observable states/events/goals and Reset results match within the documented deterministic/numerical contract; retained subsystems have no hidden dependencies on disabled ones. Record memory/CPU savings on representative levels without suppressing physically reachable effects for performance. Difficulty may simplify an explicitly authored generic model but cannot introduce a named-part exception or remove the lesson's necessary mechanism.

<a id="controls"></a>

## Priority 1 — make the core play loop reliable

**Design acceptance for every item below:** [common gates](#design-acceptance) + [UI](#accept-ui); additional profiles apply to cross-cutting requirements.

<a id="controls-input"></a>

### 1.1 Reliable placement, wiring, selection and Reset

**Design acceptance for every item below:** [common gates](#design-acceptance) + [UI](#accept-ui); additional profiles apply to cross-cutting requirements.

**When:** First post-physics work; reproduce retained failures before claiming fixes.

**Gameplay outcome:** A click or drag does exactly the visible action: place a part, select a link, move one object, navigate a level or Reset. Building never unexpectedly starts simulation or moves a nearby battery.

Treat each recorded failure separately even if one shared fix addresses several. Test cancellation, focus changes, clipped/scrolling inventory and touch; successful retries alone do not close a bug.

<a id="placement-snap-to-grid"></a>

<a id="sequence-task-254"></a>

- [ ] **Automatic snap-to-grid placement** — add a clearly discoverable grid-placement mode, enabled by default, so selecting a position for a new element or moving an existing element snaps its placement anchor to the nearest valid grid point instead of leaving it free-form. Apply the same rule to palette placement, dragging, axis/plane gizmos, duplication and touch placement across X/Y/Z, including height; no keyboard modifier should be required. Show a restrained grid/alignment preview and the exact snapped destination before committing. Define a consistent world-grid origin, spacing, per-part placement anchor and deterministic rounding at cell boundaries. Offer an explicit toggle for deliberate free placement and, if needed, a small enum-typed set of grid-spacing presets. Coordinate with tube/port snapping: a committed join must satisfy both the enabled grid rule and compatible-mouth geometry, or visibly report the conflict rather than silently moving off-grid or recording a false connection. Do not quantize runtime physics or move existing constructions when merely toggling the grid.

**Successful outcome:** Players align new and moved elements automatically on desktop and touch, including elevated placements, without free-form drift. Real-UI Playwright checks cover cell boundaries/negative coordinates, different part sizes/rotations and anchors, duplicate placement, invalid overlap/out-of-bounds placement, compatible and conflicting port joins, toggle behavior and cancellation/Undo/Redo. Verify the actual snapped transforms and typed connections, preserve settings and construction through save/load and Run/Reset, and keep grid assistance separate from difficulty nudging. Implement closed-set placement modes/presets as enums end-to-end with validated boundaries.

<a id="todo-118"></a>

<a id="sequence-task-255"></a>

- [ ] Investigate the retained `L28-balanced-reference-joined-v1` unexpected Run event during construction. Its isolated retry and subsequent reference/error runs complete correctly, but the original cause is unknown; do not count the failed record as a successful attempt.

**Successful outcome:** Replaying the retained level-28 construction never starts Run before the player's Run action; the original unexpected-event record remains a failed attempt.

<a id="todo-136"></a>

<a id="sequence-task-256"></a>

- [ ] Investigate an intermittent missed Reset click after the Precise larger-error solar timeout. Preserve the failed v2 capture (Run/result without Reset); a separate click restored the unchanged scene. The full v3 replay recorded the expected timeout and a successful Reset; its audit passes. The v2 failure remains; root cause is not established.

**Successful outcome:** After a timed-out puzzle, one deliberate Reset action restores the scene and its controls; reproduce and explain the missed click instead of relying on a second click.

<a id="todo-499"></a>

<a id="sequence-task-257"></a>

- [ ] Diagnose and fix intermittent palette placement interruptions observed in levels 6 and 7 (`Timed out: placed part`). Capture failed construction state/screenshots/actions automatically, preserve failed attempts, and add a UI regression; successful fresh-navigation retries do not establish a fix.

**Successful outcome:** Selecting and placing parts in levels 6 and 7 works without a fresh-page retry, including near palette scroll boundaries; failed gestures leave an inspectable construction state.

<a id="todo-149"></a>

<a id="sequence-task-258"></a>

- [ ] Investigate intermittent UI-only level-selector navigation: the first final-goal level-16 attempt timed out before Run; its fresh retry won. Retain the failed capture and add a focused selection regression.

**Successful outcome:** Selecting the intended level always opens that level and leaves it ready to build; the original level-16 pre-Run timeout has a focused regression.

<a id="todo-303"></a>

<a id="sequence-task-259"></a>

- [ ] Investigate selector timing in retained `colour-red-match-v1/v2`: both timed out before construction. Deliberate startup/popup/key timing succeeds in v3, but the underlying missed/delayed input cause is not proven fixed; no automatic fallback was added.

**Successful outcome:** Colour-matching lessons can be opened and constructed using ordinary input timing; the retained v1/v2 startup failures have a reproduced cause or remain explicitly unresolved.

<a id="todo-349"></a>

<a id="sequence-task-260"></a>

- [ ] Investigate retained wind-chimes-relay-v1 fan drag: fan ended at Y=0.5274403 instead of 5.4, leaving the machine idle. The instrumented retry and wall-control setup place it correctly at Y=5.384113, but the original cause is unknown. A later tilt replay (v3) also missed an activation wire and displaced its battery during wiring; an explicit UI repair (v4) actuates correctly. Do not count v1/v3 as successful chains; strengthen construction/connection checks without rejecting legitimate tube snapping.

**Successful outcome:** Dragging the fan preserves the intended axis/height and wiring connects the chosen ports without moving the battery. Both v1 and v3 failures are covered independently.

<a id="todo-441"></a>

<a id="sequence-task-261"></a>

- [ ] Continue investigating earlier missed drags/wires and selector timing; the selection-threshold fix does not prove all causes resolved. Add broader construction assertions that distinguish intentional tube snapping, plus touch/cancel/focus-loss coverage. Keep failed attempts; no automatic retries or substitutions.

**Successful outcome:** A selection tap does not become an unintended drag, a drag does not become wiring, and cancelling a gesture restores the prior construction on mouse and touch.

<a id="controls-loop"></a>

### 1.2 Finish a puzzle, continue, save and return

**Design acceptance for every item below:** [common gates](#design-acceptance) + [UI](#accept-ui); additional profiles apply to cross-cutting requirements.

**When:** After priority 0; independent of new component families.

**Gameplay outcome:** Players can choose a level, experiment, save their machine, reload it and move straight to the next puzzle after winning. Deeper parts and inventory rows remain reachable.

Use actual current-schema browser storage and campaign data. The workshop does not need a puzzle-win result, but its editing, persistence and Run/Reset lifecycle must work.

<a id="todo-502"></a>

<a id="sequence-task-262"></a>

- [ ] Check next-puzzle navigation immediately after success; avoid requiring Reset or showing misleading instructions.

**Successful outcome:** After success, a clear Next action opens the next puzzle immediately without Reset; the last puzzle has an explicit finished state instead of a dead or misleading Next instruction.

<a id="todo-112"></a>

<a id="sequence-task-263"></a>

- [ ] Audit campaign loading, selection/navigation, current-schema save handling, hints, tests and Playwright tooling for hard-coded 40-level assumptions; derive limits from campaign data where possible.

**Successful outcome:** Campaign selection, loading, hints and navigation reach every authored level and stop at the actual end of the data, including the future 150th level.

<a id="todo-155"></a>

<a id="sequence-task-264"></a>

- [ ] Verify current-schema browser Save/Load and motor motion over time; the free-workshop record reaches the diagnostic 30-second timeout, not a puzzle win. Supply/load models currently abstract a complete cable including return, with no voltage, charge depletion or torque/load simulation.

**Successful outcome:** Save, reload the page and Load reconstruct a motor-driven machine with the same links, transforms and settings; subsequent power loss, Run and Reset behave consistently.

<a id="todo-500"></a>

<a id="sequence-task-265"></a>

- [ ] Improve dense 3D scene readability: occluded deeper parts, clipped scrolled palette rows, boundary placement rounding, and abrupt mid-animation success freeze; see playtest observations.

**Successful outcome:** Players can select deeper/overlapping parts, reach every scrolled inventory row and place at the bench edge without unexplained rounding. The completion-animation portion is also tracked explicitly below.

<a id="construction"></a>

## Priority 2 — make machines easy to build, connect and read

**Design acceptance for every item below:** [common gates](#design-acceptance) + [UI](#accept-ui); additional profiles apply to cross-cutting requirements.

<a id="construction-contracts"></a>

### 2.1 Connection rules players can understand

**Design acceptance for every item below:** [common gates](#design-acceptance) + [INTERACTION](#accept-interaction); additional profiles apply to cross-cutting requirements.

**When:** Foundation for the connection menus immediately below.

**Gameplay outcome:** Players can tell which socket supplies energy, which sends a command and which carries motion or rope tension. Invalid links explain why they cannot be made.

Use existing typed contracts and finish missing behaviour; do not recreate already implemented systems or silently accept old schemas. Carry all sources, targets and modes through enum-typed APIs and validated boundaries.

<a id="todo-096"></a>

<a id="sequence-task-266"></a>

- [ ] Build typed electrical/mechanical/rope attachment points, connection rules, simulation state and Reset/save restoration needed by the new systems.

<a id="todo-463"></a>

<a id="sequence-task-267"></a>

- [ ] Add distinct electrical and mechanical power systems instead of treating every connection as a generic activation link.
  - Batteries and solar panels supply electrical power through wires; switches control circuits rather than generating power.
  - Electric motors consume electrical power and provide mechanical drive.
  - Chains/belts transmit mechanical drive from motors to compatible mechanisms.
  - Give parts typed connection points; reject incompatible links and expose clearly different wire versus chain/belt visuals.
  - Model source availability, switching, disconnected links, and mechanical operation consistently; update level inventories, authored puzzles, animation, and tests to teach and exercise these systems.

<a id="todo-275"></a>

<a id="sequence-task-268"></a>

- [ ] **Selectable power and control port roles.** For every relevant element, declare power supply/input/output separately from control/activation inputs/outputs and feedback. Devices that require both must expose independent supply and command connections; a control signal cannot provide actuator energy. Optical carrier/supply remains distinct from condition inputs. Derive compatible connections from generic enum-typed port roles, domains, direction, signal semantics and capacity, never hard-coded source/target part pairs.

<a id="sequence-task-269"></a>

- [ ] **Choose connection purpose and endpoints while wiring.** After selecting source and target, show the compatible power/control choices and specific ports in the existing contextual/radial connection UI. When more than one connection is valid, require an explicit player choice and preview both endpoints and the role before committing; never silently choose power instead of control or reuse one wire for both. A sole compatible choice remains visibly identified. Reject unsupported combinations with a short explanation and no partial mutation. Use distinct socket/cable shapes, original icons and concise labels as well as colour, preserving DESIGN.md's palette and Monument Valley-inspired minimal style. Support touch, cancellation, reconnection and one-step Undo.

**Successful outcome:** For each affected element, real-UI Playwright proof selects and verifies the actual typed source port, target port and connection role. Demonstrate supply-only, command-only, both-connected and neither-connected states according to that element's declared contract: devices requiring both cannot perform their commanded work with either missing. Test ambiguous choices, incompatible roles/directions, independently removing supply or control, and Cancel/Undo without disturbing existing links. Run/Reset and current-schema save/reload restore the exact selected roles, ports and configuration. Keep valid power-only/control-only devices governed by their own capability declarations. Teach supply first, then control and their combination within the existing 150-level campaign; integrate these cases into the all-element connection matrix.

<a id="todo-435"></a>

<a id="sequence-task-270"></a>

- [ ] Keep power, command and position/occupancy feedback as distinct typed contracts. Use C# enums for mechanism states, profiles and supported modes; update all current callers/content together without legacy aliases or fallbacks.


<a id="connection-permutations"></a>

#### Required connection coverage for every element

**Design acceptance for every item below:** [common gates](#design-acceptance) + [INTERACTION](#accept-interaction); additional profiles apply to cross-cutting requirements.

These are new, open requirements from the connection review. They apply to **every existing catalogue part, fixture, distinct mode and planned element in this file and its linked research**, including previously completed parts. The lever is one example, not the coverage boundary. Complete shared-physics prerequisites in priority 0 first; deliver each part's connection implementation and focused Playwright proof together. This focused matrix is not the deferred campaign difficulty sweep.

<a id="sequence-task-271"></a>

- [ ] **Connection capability audit — every element, every connection type.** Create a reviewable, enumerated matrix from the catalogue, fixtures, authored puzzles and planned component inventory. For each individual element and mode, evaluate rope, belt/chain/shaft, electrical power, activation commands, signals, optical paths, acoustic coupling, water and pneumatic connections. Record each as supported, required-but-missing, deliberately unsupported with a gameplay reason, or unresolved research; absent implementation must never be classified as unsupported merely to make tests pass. Separate logical ports from physical attachments and free-space interactions. Record port identity, body-local attachment, direction, multiplicity, compatible counterparts, route/material/channel, supply-versus-command role, operating states and expected player-visible effect. Use enum-typed domains/modes/roles and strongly typed extensible identities through runtime, fixtures and automation; reject unknown boundary values. **Success:** no part or intended interaction disappears from the checklist; every missing capability has its own implementation item and every unsupported pairing has a rejection case.

<a id="sequence-task-272"></a>

- [ ] **Generate and execute the complete finite connection-permutation Playwright matrix.** Give every case a stable identity comprising source element/variant/mode/port, destination element/variant/mode/port, connection kind/material/channel, direction, route class and relevant state combination. Enumerate the full Cartesian product of those declared finite choices, classify valid and invalid combinations, and keep an explicit expected-result ledger. For multiport parts, enumerate every supported simultaneous port-occupancy and input-state combination, including multiple connection types on one element, all supported fan-in/fan-out configurations up to declared capacity, and conflicts. Test each actual source/destination element pairing, not just a representative of a domain or family. Reversed construction order and reversed physical motion are separate where semantics differ. Pairwise sampling, native-only tests, and one generic cable test do not satisfy this requirement. Continuous positions, loads and timings need documented boundary classes (below/at/above thresholds, orientations, contact clearances); arbitrary unbounded networks are not a finite permutation set, so publish topology/size bounds and retain additional chain/cycle/load stress cases. **Success:** expected case count reconciles exactly with individually reported passing, failing and missing cases; unsupported cases prove rejection and required-but-unimplemented cases remain open, never silently filtered or marked skipped.

<a id="sequence-task-273"></a>

- [ ] **Real-UI construction and lifecycle proof for every matrix case.** Use Playwright to choose the actual part and mode, place it with ordinary UI gestures, choose the connection category and radial port, and connect to the intended counterpart. Verify the placed configuration and typed endpoint identities using read-only observation, then Run and assert an observable physical or logical consequence with a meaningful negative/control run. Through the selected connection's radial menu, change source, change destination, cancel an edit, delete and undo; verify the old endpoint no longer drives the target. Move/rotate/resize applicable parts and check attachments follow their actual bodies. Assert exact Run/Reset construction restoration, save/load restoration and applicable Undo/Redo, including route order, rope length, belt wrap, channels and port selections. Do not use state setters, imported solutions or numeric placement menus. **Success:** each case retains reproducible actions, revision/build, assertions, logs, screenshots/motion evidence and failed attempts; stale or missing evidence stays incomplete.

<a id="sequence-task-274"></a>

- [ ] **Connection rejection and capacity proof across all elements.** Playwright must attempt every incompatible domain/port-role pairing and each prohibited direction, self-link, duplicate, occupied port, excessive fan-in/fan-out, illegal route, ambiguous socket, unsupported material/channel and deleted endpoint. Include parts with no connection ports: they must neither expose false sockets nor accept accidental links. For conflicts involving several otherwise valid links, verify the specified deterministic rejection or operating state without hidden energy, ghost commands or partial mutation. **Success:** the UI explains why a link is invalid and leaves the construction, inventory and existing links intact; native serialized-input validation supplements the browser proof.

The following rows are **separate open implementation-and-Playwright work packages**. Expand each named element into its own matrix records and child tasks before implementation; checking one row requires evidence for every named element and permutation in it. Proposed capabilities below must be reconciled with research and the intended gameplay contract, rather than treating the current socket list as the final design.

| Status | Element coverage and connection reasoning | Required Playwright outcomes and controls |
| --- | --- | --- |
| [ ] <a id="sequence-task-275"></a> | **Lever, pulley, anchor, weight and moving rope loads:** impact lever/see-saw needs independently selectable end tie points on the rotating beam; pulleys are ordered guides, anchors fix endpoints, weights exchange tension. Add attachment contracts for bucket, bird cage, leaky bucket, balloon/hot-air balloon and other intended suspended loads as their elements arrive. A fixed receiving basket is not a moving bucket. | Exercise each left/right/end attachment and each compatible load-to-load, load-to-anchor and load-to-actuator pairing, directly and through each supported guide topology. Prove lever motion pulls a load and a falling load rotates a lever, including opposing ropes on both ends. Test slack/taut transition, load too heavy, blocked travel, stops, different lever arms, upward buoyant pull and changing contained mass; rope cannot push or create energy. Reset restores beam angle, load pose, contents and exact route/length. |
| [ ] <a id="sequence-task-276"></a> | **Rope-operated controls, cutters and linear/rotary converters:** drawstring light, rope-triggered toy launcher, character-drive release, moving shutter/trapdoor where intended, hooks/cleats, scissors/tin snips, rope/steel cable, and each rotational-to-linear and linear-to-rotational adapter. Establish pulling direction, stroke, holding versus one-shot trigger, release/rearm and material compatibility separately for each element. | Test every supported endpoint/material/cutter combination and every supported pull direction; a slack, disconnected or wrong-direction pull must not trigger. Cutting the chosen segment releases the correct load; a missed cut or incompatible cutter does not. For each converter, a rope load affects shaft motion and shaft work moves the rope load according to its contract; test end stops and blocked loads, then exact cut/uncut restoration. |
| [ ] <a id="sequence-task-277"></a> | **Mechanical drive sources, transmission and consumers:** motor, windmill, mouse/character motor, generator, gears of each size, reverse transmission, clutch, conveyor input and pass-through, wound spring/jack-in-the-box, flywheel, winch and every planned shaft-driven mechanism. Distinguish belt wheel rims, gear meshing, chain sprockets and shafts; specify which connection forms each actually supports. | Test every compatible source/output-to-consumer/input pairing and supported through-route, both signed directions, each ratio/mode, load and stall boundary, source loss, conflicting sources and bounded cycles. Verify belt tangent/wrap geometry follows wheel rims through placement and movement. Open/crossed belts or chain variants require separate direction/material tests if supported; otherwise prove rejection. Shared drive must not duplicate available work. |
| [ ] <a id="sequence-task-278"></a> | **Electrical sources, routing and consumers:** battery/outlet, switched outlet, solar panel, generator, pressure plate, light receiver, sound meter, electrical logic, latch, timer, counter and every electrically powered part including motor, fan, lamp, laser, speaker, gate, pusher, clutch, cannon and planned appliances. Distinguish a source from a gated pass-through and a powered load. | Test every compatible supply-output to power/input-port pairing, each gating mode, all supported source combinations and fan-out limits. Exercise source present/absent, enabled/disabled, threshold boundaries, blocked/missing external stimulus and source removal. A command or sensed light/sound alone must not supply a pass-through load; ensure each intended generator/solar source obeys its specified energy model. No feedback loop creates free power. |
| [ ] <a id="sequence-task-279"></a> | **Activation, signal and timed logic:** switch, detector, delay, clock, counter, hold timer, latch set/reset, all electrical and optical logic operations, and every command-receiving actuator. Keep command/event ports distinct from power and from sustained state; audit the existing Signal domain rather than silently treating it as Activation. | For every emitter/receiver/port pairing, prove the actual command, edge or sustained-state semantics, repeated triggers, reset/rearm, simultaneous inputs and ordered inputs. Exercise every truth-table row for every operation and every timer/counter mode, with supply on/off where required. Explicitly test swapped latch set/reset and unrelated ports; deleting or reconnecting a command wire must not leave queued ghost events. |
| [ ] <a id="sequence-task-280"></a> | **Parts with several connection types at once:** motor (electrical + mechanical), clutch (electrical + mechanical input/output), generator (mechanical + electrical), wound spring (mechanical + activation), cannon/laser/speaker (power + activation + physical output), pusher (power + extend/retract + limit outputs), latch/counter/hold timer, optical receiver and sound meter. Extend to every later multi-domain element. | Exhaustively exercise each declared finite combination of connected/disconnected ports, energised/inactive inputs, modes and relevant conflicts, including three or more domains when present. Supply-only, command-only, drive-only and fully connected runs must have distinct expected outcomes. Check each output independently and together; removing one input must affect only the behaviour specified by the part's contract. |
| [ ] <a id="sequence-task-281"></a> | **Optical connections and free-space paths:** flashlight, lamp/drawstring light, candle, laser of every channel, mirrors, splitter, combiner, colour filter, magnifier, optical logic, solar panel, receiver and beam shutter. Optical apertures are not electrical sockets; a beam can be a spatial interaction without becoming an invented wire. | Test each compatible emitter/channel to aperture/receiver/mode pairing, every splitter branch and combiner input combination, reflection directions and blocked/misaligned paths. Combine electrical or rope source control with actual light arrival and downstream output. Test threshold, colour mismatch, moving occluder and bounded reflection-loop cases; Reset restores channel, aim and links. |
| [ ] <a id="sequence-task-282"></a> | **Acoustic connections and spatial coupling:** bell, wind chimes, speaker, sound meter and all planned sound-operated components. Keep physical impact/air excitation, powered sound emission and detector output as distinct stages. | Test each source/listener/channel or frequency mode supported by the contract, distance/direction/occlusion boundaries where modelled, simultaneous sources, power/activation permutations and downstream electrical/command outputs. Silent, out-of-range or wrong-channel controls cannot activate the target; test repeated pulses and exact Reset. |
| [ ] <a id="sequence-task-283"></a> | **Water connections and conversions:** each reservoir, pipe/hose, outlet/nozzle, valve, splitter, pump, leaky container, float sensor, water wheel/turbine and other named water element in the research. Record fluid inlet/outlet roles independently of mechanical, power and control ports; specify open flow versus sealed conduit. | Test every compatible producer/conduit/consumer pairing, branch and valve mode, gravity-fed versus powered flow, empty/full/overflow, reverse/blocked flow and pump source loss. Verify transferred volume and mass, changing rope loads, mechanical output and sensor commands; incompatible air/electrical ports must reject a water link. |
| [ ] <a id="sequence-task-284"></a> | **Pneumatic connections and conversions:** bellows, fan, pump/compressor, hose, tank, valve, nozzle, piston and every planned pressure or airflow actuator. Distinguish an open air jet that strikes a windmill from a sealed hose carrying pressure. | Test every supported pressure-source/port/actuator pairing and valve state, fill/discharge, leak/blockage, absent hose, source loss and pressure thresholds. Exercise electrical/rope/mechanical controls with actual airflow or piston travel. A free-space fan connection must not silently become a sealed pressure supply, and a pressure signal must not generate unlimited work. |
| [ ] <a id="sequence-task-285"></a> | **Contact, cargo and elements without ordinary links:** balls of every type, domino, ramps/walls, pipe/bend/funnel, basket, trampoline, spring, bumper, bellows, impact lever, pressure plate, detector, gate, launcher, characters, fragile targets, heat/ignition parts and remaining specialist variants. Audit every catalogue/research entry not already named above. | Explicitly record which interactions are contact, containment, airflow, heat or proximity instead of attachable connections. Prove their intended interaction with connected machinery, including transported cargo and sensor/actuator paths, and reject unsupported rope/belt/wire attachments. If a useful intended attachment is missing, add a separate implementation task rather than accepting “no ports” as completion. |

<a id="sequence-task-286"></a>

- [ ] **End-to-end connection teaching and integration permutations.** Add short playable lessons and Playwright constructions for lever → rope → load; weight → pulley route → drawstring control; windmill → belt → generator → wire → motor → belt → conveyor; drive → clutch → wound spring with separate release command; powered sensor → logic/timer → pusher/gate; and optical/acoustic/fluid/pneumatic conversion chains as those parts arrive. Instantiate every supported source/adapter/consumer substitution required by the matrix, not only these example recipes. Include simultaneous mechanisms that share a part or power source. **Success:** the player can see the causal chain, diagnose a missing/slack/blocked/unpowered connection and repair it via the radial menu; controls isolate each stage and reset the exact original construction.

<a id="sequence-task-287"></a>

- [ ] **Keep connection coverage complete as the catalogue evolves.** Compare the enumerated expected matrix with the catalogue, connection contracts and individual Playwright result ledger in verification tooling. New parts, ports, modes, materials or supported pairings must create visible outstanding cases; removed/renamed contracts invalidate old evidence rather than inheriting a pass. Report expected/passed/failed/missing/stale counts by element and connection type, retain historical results, and prohibit skipped tests from satisfying completion. **Success:** no component is marked complete or published as individually verified until its required cases pass against the tested revision; missing capabilities stay tracked while priority-0 physics work continues. Apply [case ownership](../delivery-workflow.md#case-ownership): current-frontier publication and final all-planned-case closure are distinct; future cases retain their later owner.

**Historical reference and limits.** The [original 1993 manual](https://dos.remotecpu.com/all-files?catid=87&id=53&m=0&task=download.send), “Joining objects” and the sample solution (printed pages 13 and 16), describes deliberate rope/belt endpoint placement, proximity-based electrical plugs and a motor-to-conveyor belt chain. These are different connection semantics; our explicit wire/radial-menu interaction is an intentional adaptation. The [TIM2 manual](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf), printed page 14, gives a lever → pulley → motor rope-building example and distinguishes rope from steel-cable cutting. TIM2 is a separate edition, not proof of every original-game capability. The load/torque, conservation and exhaustive UI-proof requirements above are our proposed game contracts, not claims about the original solver.

<a id="sequence-task-288"></a>

- [ ] **Resolve historical attachment semantics per element before implementing uncertain contracts.** Supplement the manuals with recorded original-game observations for both lever ends, routed ropes, buckets/balloons, rope-operated controls, belt-driven wheels and electrical appliances. Record edition, puzzle/free-form construction, port choices, before/after motion, valid/invalid attachment and reset behaviour; separately investigate later-edition adapters and cable. **Success:** each uncertain matrix cell gains a cited observation or an explicit original-design decision; unmeasured direction, load, capacity and timing behaviour stays labelled uncertain. Research must inform each element, not justify copying one rope example to all parts.


<a id="construction-menus"></a>

### 2.2 Discoverable modes, part help and radial editing

**Design acceptance for every item below:** [common gates](#design-acceptance) + [UI](#accept-ui); additional profiles apply to cross-cutting requirements.

**When:** After compatible-port rules; high value across every puzzle.

**Gameplay outcome:** Players find Campaign/Free Play, learn a part's purpose, choose the right port and repair a machine through compact radial actions without deleting and rebuilding unrelated parts.

Deliver part radial selection before dependent port/link actions. Keep the new three connection requests separate from the older manual-routing umbrella; their obligations overlap but none is removed.

<a id="palette-type-groups"></a>

<a id="sequence-task-289"></a>

- [ ] **Group the left-menu puzzle elements by fundamental type** — replace the endless flat catalogue with clearly labeled, collapsible groups including **Gravity, Mechanical, Radiation, Light and Fluid**. Give other distinct domains, such as Electrical, Sound and Thermal, explicit groups where needed to cover the full catalogue. Keep elements **alphabetically ordered by their displayed names within each group** and use a stable, deliberate group order. Assign every existing and future element one explicit primary group based on its fundamental puzzle behavior; multi-domain elements must remain easy to find without duplicate inventory entries. Preserve campaign inventory limits, availability, part information and placement actions, and coordinate with the separately planned Connections menu. Preserve the approved palette, compact layout and usable mouse/touch targets. Represent categories with an enum end-to-end through catalogue metadata, grouping, UI adapters and tests; display labels are presentation, never category selectors, and unsupported categories must be rejected explicitly.

**Successful outcome:** A player opens a relevant group and finds its elements alphabetically without scrolling through the entire catalogue. Real-UI Playwright checks prove group expand/collapse, alphabetical order, complete and nonduplicated catalogue coverage, correct Campaign/Free Play availability, and selecting/placing parts from different groups on desktop and touch layouts. Changing groups must not lose the construction or alter Run/Reset behavior; new elements require an explicit category before catalogue admission.


<a id="todo-455"></a>

<a id="sequence-task-290"></a>

- [ ] **Prominent top-level game mode selection** — make Campaign and Free Play a prominent, persistent, compact selector in the top bar, not hidden in a secondary menu, so players can readily discover and switch between them. Make the distinction intuitive: Campaign follows authored level progression; Free Play opens the creative sandbox. Preserve the approved palette and minimal visual style without adding floating panels; clearly distinguish the active mode at a glance and retain enum-typed mode selection end-to-end. Verify top-bar visibility, discovery, selection, the appropriate progression/sandbox experience and active-mode feedback for both modes through real-UI Playwright interaction.

**Successful outcome:** A new player can identify and enter Campaign or Free Play from the top-level UI and can always tell which mode is active.

<a id="todo-451"></a>

<a id="sequence-task-291"></a>

- [ ] **Puzzle-element information icons** — give every puzzle element an (i) information icon that opens an on-demand explanation of what the element does and how it works, including applicable inputs, outputs, activation requirements and connections. Keep the information easy to dismiss and consistent with the minimal icon-driven UI and approved visual style; do not add persistent tooltips or floating menus. Verify each element's information through real-UI Playwright interaction.

**Successful outcome:** From any part, a player can open a concise explanation of its inputs, outputs, energy needs and visible states, dismiss it and continue building without losing selection or progress.

<a id="todo-453"></a>

<a id="sequence-task-292"></a>

- [ ] **Contextual radial menu for selected elements** — where applicable, selecting a placed puzzle element should reveal a compact, icon-driven radial menu with rotate and translate/move actions, creation of supported typed links (electrical wiring, mechanical belts/chains and other applicable connections), and element-specific configuration (for example, a timer's delay before firing). Show only actions and settings supported by the selected element; keep configuration on demand, dismiss the menu on deselection, and preserve the approved palette and minimal visual style without a permanent CAD-style inspector. Use enum-typed actions, connection types and configuration choices end-to-end. Verify applicable actions through real-UI Playwright tests, including selection/dismissal, rotation/movement, compatible and rejected link targets, timer-delay changes with observed firing behaviour, and exact configuration/connection restoration through Run/Reset and current-schema save/load.

**Successful outcome:** Selecting a placed part exposes only supported move, rotate, connect and configuration actions; changing a timer setting visibly changes its later firing time and is undoable/restorable.

<a id="todo-447"></a>

<a id="sequence-task-293"></a>

- [ ] **Dedicated connections menu and radial port selection** — give electrical wires, mechanical belts/chains, ropes and other supported connection types their own menu category, separate from puzzle parts. Give each type a distinct icon and connection appearance while preserving the approved palette. When a puzzle element supports multiple connection types or ports, expose its compatible ports in the element's radial menu so players can explicitly choose the intended connection. Keep connection types, ports and actions enum-typed end-to-end; verify each supported type, multi-port selection and rejection of incompatible targets through real-UI Playwright interaction.

**Successful outcome:** Players enter Connections, distinguish wire/belt/rope choices by icon and appearance, then choose a compatible port from a multi-port part's radial menu without guessing from overlapping socket positions.

<a id="todo-449"></a>

<a id="sequence-task-294"></a>

- [ ] **Radial menu for selected connections** — allow players to select an existing electrical, belt/chain, rope or other supported connection and open its own radial menu with Change source, Change destination and Delete actions. Highlight the selected link and its endpoints, restrict reconnection to compatible typed ports, and support cancellation and Undo/Redo. Coordinate with the manual-routing item below; verify each connection type, overlapping-link selection, invalid reconnections, deletion, and exact Run/Reset and current-schema save/load restoration through real-UI Playwright interaction.

**Successful outcome:** Selecting a link highlights both endpoints; Change source or Change destination moves only that endpoint, and Delete removes only that link. Invalid targets and cancellation leave the previous valid connection intact.

<a id="todo-457"></a>

<a id="sequence-task-295"></a>

- [ ] **Select, edit and manually route existing connections** — electrical wires, mechanical belts/chains, activation/signal links and ropes. Give links generous mouse/touch hit targets and a clear selected highlight; allow endpoint reconnection to compatible typed sockets, deletion, and adding/moving/removing route handles. Keep controls contextual and icon-driven, with Undo/Redo and current-schema save/load support. Electrical/signal waypoints change cable presentation, not connectivity or power. Belt/chain routing must use explicit compatible guides/pulleys and preserve drive direction, wrap and tension rules; rope routing must preserve physical length, slack, tension and outside-pulley geometry rather than act as a cosmetic spline. Do not let manual routing bypass walls or connection constraints. Keep editing in build mode and preserve authored routes through Run/Reset. Test overlapping-link selection, occluded/depth handles, invalid endpoints, crossing versus joining, disconnection, route edits/Undo, serialization and desktop/mobile real-UI interaction. Preserve the approved palette and minimal UI; avoid a permanent CAD-style inspector.

**Successful outcome:** A player can select one crossing link, edit its endpoints or route and undo the edit without accidentally joining the crossing. Physical rope/belt paths obey guides and obstacles rather than merely drawing a different curve.

<a id="construction-feedback"></a>

### 2.3 Read the machine while building and running

**Design acceptance for every item below:** [common gates](#design-acceptance) + [UI](#accept-ui); additional profiles apply to cross-cutting requirements.

**When:** Alongside construction menus and each affected part.

**Gameplay outcome:** Belt travel follows wheel rims, visible states explain why a mechanism is waiting or blocked, and success allows the final action to finish before the player continues.

Preserve the palette and minimal UI. Feedback must follow real simulation state; cosmetic motion must not create forces or change timing. Basic readability is immediate work; broad art restyling is later.

<a id="todo-445"></a>

<a id="sequence-task-296"></a>

- [ ] **Mechanical belts wrap around wheels** — route belts around the outside rims/grooves of connected wheels using tangent spans and curved contact arcs, following the pulley presentation rather than drawing axle-to-axle links. Preserve mechanical drive direction and applicable wrap/tension constraints; verify unequal wheel sizes, rotated wheels, compatible guides, invalid geometry, visible motion and exact Run/Reset and save/load restoration through focused native and real-UI Playwright checks.

**Successful outcome:** A running belt visibly follows the outer wheel grooves through tangent spans and wrap arcs; rotation/size differences remain readable and Reset reconstructs the same belt route.

<a id="todo-508"></a>

<a id="sequence-task-297"></a>

- [ ] Finish activation/success animation behaviour: let visible mechanisms complete their motion after success instead of freezing mid-topple, while preserving goal state, Reset restoration, and DESIGN.md's approved visual style.

**Successful outcome:** The goal remains won while the visible final topple, spring return or actuator stroke completes; Next and Reset stay usable and cannot trigger a second reward or duplicate result.

<a id="todo-462"></a>

<a id="sequence-task-298"></a>

- [ ] Continue activation animation across other mechanical pieces and audit continuous rendered fluidity/performance, slow playback and multi-part activation. Spring feedback is cosmetic and begins with the unchanged instantaneous launch, not a simulated deformable spring.

**Successful outcome:** A player can tell when a mechanism accepts an activation, moves, stops or rearms at normal and slow playback without visual motion disagreeing with contact or signal timing.

<a id="todo-436"></a>

<a id="sequence-task-299"></a>

- [ ] Give each element an original pictogram and on-object ready/loaded/charged/moving/blocked cues. Keep cream/navy/cyan/gold styling and fluid contact-synchronised motion. Show aim/stroke/sweep previews only for selection; do not add persistent CAD inspectors or reflex controls.

**Successful outcome:** For every new part, the player can distinguish ready, loaded, charged, moving and blocked states from its original icon and object cues without opening a permanent inspector.

<a id="todo-276"></a>

<a id="sequence-task-300"></a>

- [ ] Give each module one understandable job, with a small dial, countdown ring, status lamps or on-body count. Use contextual controls and icon-first UI; avoid a breadboard editor, arithmetic combinators or mandatory truth-table configuration.

<a id="todo-312"></a>

<a id="sequence-task-301"></a>

- [ ] Keep bright toy housings, chunky stands, clear glass and large target discs. Add original pictograms for each distinct function. Show aim paths/focal envelopes only when relevant; supplement colour with channel symbols/patterns. Visible side-on beams are a readability convention, not a claim of full optical simulation.

<a id="todo-354"></a>

<a id="sequence-task-302"></a>

- [ ] Provide visible source motion, tone symbols, restrained traveling arcs and receiver needles. Separate continuous condition from pulses; electrical outputs require real supply.

<a id="todo-253"></a>

<a id="sequence-task-303"></a>

- [ ] Use chunky collars, transparent strips/cutaways and tap-accessible inspection. Snap compatible endpoints; retain the existing move/rotate interaction, three unfilled dashed projections and transparent reference walls. Consider tap-to-route after basic pieces work, with one Undo for the whole route; do not add a CAD spline editor or permanent camera toolbar.

<a id="existing"></a>

## Priority 3 — finish and teach the parts already in players’ hands

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

<a id="existing-parts"></a>

### 3.1 Close remaining behaviour and evidence gaps in existing parts

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** Existing-part physics, animation, rendering and controls are delivered one element per slice in [roadmap order](invest/vertical-delivery.md#rolling-playable-roadmap) under the [72 current-element requirements](#current-catalogue-closure); all existing modes/fixtures are audited at the LEGACY-0 release-checklist gate. Remaining elements (ELEMENT-n) and campaign expansion (CAMPAIGN) follow in that order; no unfinished part may be assumed ready for a required lesson.

**Gameplay outcome:** Already visible catalogue parts become dependable tools: levers transfer load, pushers move cargo, clutches engage drive, windmills power a belt and bellows produce a bounded useful burst.

Unchecked rows with extensive passing evidence still have explicit remaining work. Reuse the implementation, close those gaps and refresh invalidated proofs. The historical lever row and detailed impact-lever row describe overlapping obligations, not two automatically completed parts.

<a id="todo-476"></a>

<a id="sequence-task-304"></a>

- [ ] Add wall pieces, pinball bumpers, and more interactive items for creating and solving puzzles.
  - Wall pieces must be movable, rotatable in all three dimensions, and resizable through intuitive controls; update collision geometry and dashed placement projections to match their dimensions.
  - Keep these physical puzzle walls distinct from the transparent reference walls used for placement aids.
  - Add pinball bumpers with clear impact feedback and predictable rebound/impulse behaviour, including smooth activation animation and Reset support.
  - Expand the item catalog with complementary puzzle-solving mechanisms; include authoring support, player inventories, teaching puzzles, alternative solutions, and physics/UI tests while retaining DESIGN.md's visual style.

**Successful outcome:** Players can use walls to shape a route and bumpers to redirect a ball, adjust the intended wall dimension and recover it with Undo/Reset. Preserve this umbrella until the remaining interaction and verification requirements are satisfied.

<a id="todo-471"></a>

<a id="sequence-task-305"></a>

- [ ] Add weights, pulleys, and ropes as core mechanics for both creating puzzles and solving them.
  - Support attachable weights, rope endpoints, and routing over pulleys to lift, pull, counterbalance, and redirect forces between mechanisms.
  - Make rope length, slack/tension, pulley routing, and weight effects visually understandable in 3D; animate motion smoothly and restore the complete setup on Reset.
  - Include these parts in authoring tools and player inventories, with introductory puzzles and later combinations that allow alternative solutions.
  - Add deterministic physics, connection/constraint, difficulty-tolerance, and UI playtests for these systems.

**Successful outcome:** A player can build a counterweight lift and a redirected rope trigger in Free Play, see why an open/slack route fails, and solve an introductory level with the same mechanics.

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="sequence-task-306"></a><a id="todo-408"></a> **Passive trampoline / elastic membrane** | Finite compliant membrane with rigid rim/back and visible physical indentation; tension changes compression/contact duration without imposed launch velocity. Qualify off-centre/stacked/tilted loads, fixed-rim and miss controls, direction-aware sag, rotated compounds, slack/short tethers, exact lifecycle and sustained desktop/mobile motion. | Teach drop→trampoline→pipe, varying height/mass; qualify inlet/centre/outlet, energy, missed alignment and final teaching order. |
| <a id="sequence-task-307"></a><a id="todo-409"></a> **Teeter-totter / impact lever** | Finite-mass beam on a real hinge with gold fulcrum/stops and end rope sockets. Qualify aligned/missed/pivot hits, mass×arm balance, equal-load ten-second balance, four 3D orientations, fixed/workbench/sphere/tube/frustum/bend and moving/beam obstacles, open bores/end rims, overlap rejection, friction, rope coupling, insufficient energy and Reset. | Heavy falling ball lifts a lighter payload; a rope raises a shutter. Continuous physical motion, no canned flip; mobile/performance and campaign gates apply. |

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-186"></a> [ ] <a id="sequence-task-308"></a> P1 | Teeter-totter: pivoting lever with rope attachments | Add a hinge constraint, end sockets and visible angular limits. Teach counterweights, then ball-to-rope triggering. Test off-centre loads, stalls and Reset. |

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="sequence-task-309"></a><a id="todo-422"></a> **Electric linear pusher** | Independent supply and extend/retract commands; bounded stroke, speed/acceleration, force/work and endpoint outputs. Conflicting commands stop; explicit brake holds on power loss. Cream barrel, gold rod/travel marks and physical head/shaft match collision. Qualify cargo push/miss, missing supply, conflict, endpoints, timed reversal, interrupted holding, stalls/low-force support and lifecycle. | Sensor→delay→pusher moves conveyor cargo into a tube. Qualify tube/conveyor/rope/multiple-actuator integration, campaign and sustained mobile motion. |

<a id="todo-374"></a>

<a id="sequence-task-310"></a>

- [ ] **Electrically controlled clutch.** Apply the complete [rotary transmission contract](../rotary-transmission-parts.md): powered drive, missing coil/motor supply, timed release, reverse drive, finite inertia/load, truthful plates, typed links and exact Reset/save. Teach the mechanism and qualify sustained mobile animation; brakes and slip/torque-limiting variants remain separate required elements.

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="sequence-task-311"></a><a id="todo-190"></a> **Windmill: air to rotation** | Apply the complete rotor-airflow and finite-inertia transmission contracts: signed response, actual pose/occlusion, connected/disconnected/blocked controls, paid torque/load and exact Reset/save. | Teach fan→rotor→belt; qualify all source/receiver settings and sustained desktop/mobile motion. |
| <a id="sequence-task-312"></a><a id="todo-191"></a> **Bellows: impact-operated air burst** | Finite-mass plate and actual compression drive conserved airflow with physical work/reaction, held-load suppression and silent refill. Preserve visible folds, missed/blocked/disconnected controls and exact Reset/save; true unloaded emission requires finite gas/nozzle transport. | Teach impact→air→rotor/chime and qualify refill/repeated cycles, downstream default-distance outcome, all source configurations and sustained desktop/mobile behavior. |

<a id="todo-369"></a>

<a id="sequence-task-313"></a>

- [ ] Test all truth rows, absent/present supply/carrier, output-retracting transitions, reordered/reconvergent networks, independent optical occlusion, feedback bounds and Reset.

**Successful outcome:** Each of the five electrical and five optical operations has identifiable real-UI evidence for true/false outcomes and missing supply/carrier, including output retraction and Reset. A NOR demonstration cannot complete the other optical gates.

<a id="todo-351"></a>

<a id="sequence-task-314"></a>

- [ ] Speaker/bell/chime follow-up: auditory listening/quality check, browser audio-suspension testing, mobile wave contrast and continuous animation review. The faint wave is supplementary feedback; the sound meter now supplies visible reception feedback.

**Successful outcome:** The player hears readable, non-disruptive source feedback when audio is available and can still solve the identical puzzle when it is muted or suspended; mobile wave/needle cues remain visible.

<a id="teaching"></a>

### 3.2 Teach each working mechanism through focused levels

**Design acceptance for every item below:** [common gates](#design-acceptance) + [CONTENT](#accept-content); additional profiles apply to cross-cutting requirements.

**When:** Deliver a small lesson with each completed part; final balancing waits for component coverage.

**Gameplay outcome:** A first-time player meets one new idea at a time, sees why a failed arrangement fails and combines previously taught ideas to solve later puzzles without copying a hidden reference layout.

The campaign target is **150 distinct levels**, and the end state is 150 progressively taught levels plus an unlimited free-play workshop that is always available ([roadmap CAMPAIGN](invest/vertical-delivery.md#rolling-playable-roadmap), 10 levels per slice after LEGACY-0 and required component coverage). The allocation below is the current authoring plan, not a claim that those levels or parts are implemented. It supersedes the earlier 75-level allocation and the radiation research's preliminary campaign numbering. Deliver each implemented element's focused lesson and proof as it becomes available; unfinished dependencies keep their planned levels visibly blocked.

<a id="todo-098"></a>
<a id="campaign-plan"></a>

<a id="sequence-task-315"></a>

- [ ] Author and rebalance the **150-level teaching progression** below. Maintain 15 chapters of 10 levels, with explicit introductions, practice, combinations and chapter finales. Preserve current draft/evidence identities when reworking content; old level numbers are historical until the current authoring/tooling is updated.

#### Campaign structure: 15 chapters, 150 levels

**Design acceptance for every item below:** [common gates](#design-acceptance) + [CONTENT](#accept-content); additional profiles apply to cross-cutting requirements.

The “new elements” column reserves teaching space for **each named element**, including variants with distinct behavior. It is not permission to introduce an entire family in one unexplained puzzle. A source/receiver or container/flow pair can teach one causal relationship together; otherwise stage new distinctions in separate levels or clearly separated introductory objectives before combining them. The detailed coverage ledger below is required before a chapter is considered fully allocated.

| Chapter / levels | New elements and teaching focus | Increasingly complex use of familiar elements |
| --- | --- | --- |
| 1 · **1–10: On a Roll** | Ball/material variants, ramps, resizable walls, baskets, dominoes, springboards, bumpers, trampoline, impact lever and damped capture. Teach placement, rotation, depth, contact, Run/Reset and observable goal events. | Begin with one gravity path; progress to a two-stage rebound/catch and a contact chain. End with an alternative-route delivery using 2–3 distinct functional roles. |
| 2 · **11–20: A Helping Weight** | Batteries, wires, switches, motors, belts/chains, conveyors, reverse transmission, weights, anchors/hooks, rope/cable, pulleys, moving containers, scissors/tin snips, fan and balloon/tether interactions. | Teach source → connection → load before powered transport. Combine counterbalance, drive direction and deliberate release; use 3–4 roles and one clearly visible dependency. |
| 3 · **21–30: Pipe Dreams** | Straight ball pipes, 45°/90° bends, funnels, powered/mechanical gates, Y-diverter modes, hopper/escapement, ball detector, pressure plate, delay, hold timer, latch, counter and repeating clock. | Route one ball before a queue; then wait for a catcher, retain a command and distribute counted arrivals. End with a feeder/timed-diverter machine using 4–5 roles. Teach each state/mode before requiring its combination. |
| 4 · **31–40: Gears of an Idea** | Electrical AND, OR, XOR, NOR and NAND separately; gears, generator, impact-started mouse-motor role, windmill, clutch, brake, crank-slider, rotation/linear converters, cam/follower profiles and rack-and-pinion. | Start each operation with familiar independent inputs and explicit supply. Combine logic, shaft direction and a real motion constraint; end with an interlocked drive using 4–6 roles across mechanical/electrical systems. |
| 5 · **41–50: Ready, Set, Throw!** | Linear pusher/boxing glove, pinball flipper, wound launcher, jack-in-the-box, passive impact scoop, counterweighted scoop, slingshot, hinged elastic catapult, cannon/reload shuttle, magnetic impulse relay, electromagnet, gripper, docking lift/ferry, zipline carriage and releasable assemblies. | Separate capture, charging, aiming, triggering and rearming. Provide familiar fixed support while teaching each actuator. Combine loading and dock interlocks into 5–7-role deliveries; no free recharge, teleport or unexplained capture. |
| 6 · **51–60: A Bright Idea** | Flashlight/lamp/drawstring sources, solar panel, lasers/receivers, mirrors, physical beam shutter, splitter, color filters, combiner/mixed receivers, converging/diverging lenses, prism, light pipe and optical AND/OR/XOR/NOR/NAND. | Teach illumination, interruption, redirection and finite-power splitting before spectral and logical combinations. Finish with a light-controlled physical transfer using 5–7 roles, including separate control beams/carrier/supply where required. |
| 7 · **61–70: Go with the Flow** | Reservoir/header tank, taps, catch basin/funnel/drain, gutters, water-pipe kit, waterwheel, movable/leaky buckets, float/float valve/level switch, check valve/diverter/sluice, pump/screw, siphon/primer, tipping bucket, communicating tanks/locks, buoyant craft, hydraulic piston and flow/volume/pressure meters. | Teach finite supply and downhill flow before powered lifting or pressure. Reuse ropes, belts and logic to make changing mass and water level control a 6–8-role machine. A source cannot silently refill. |
| 8 · **71–80: Sounds Like a Plan** | Bellows, hoses, finite air reservoir, release/directional valves, spring-return and double-acting pistons; speaker/meter, struck bell, wind chimes, tuned meters, whistle, listening/exit horns, acoustic ducts, resonator, screen/dish and water-tuned bottle. | Distinguish fan airflow from stored pressure and sound strength from tone. Reuse water tuning and mechanical strikers, then join pneumatic stroke → sound → supplied release in a 6–8-role sequence. |
| 9 · **81–90: Stored Potential** | Retain separate ratchet/escapement/carousel, flywheel/governor, accumulator and porous-water lessons; add individual thermal storage, expansion, thermostat, fusible release and conversion objectives from the [thermal ledger](#thermal-campaign-allocation). Heat/phase/ignition prerequisites now begin in earlier chapters. | Teach stored quantity, depletion and recovery with a visible gauge/state. Reuse previously taught water, steam, cooling and heat through7–9 roles; verify startup and energy before feedback. |
| 10 · **91–100: Oddly Satisfying** | Distinct size/material/weight sorters, speed-sensitive/selective trapdoor, spiral delay tube, powered airlift; powered ejector/toaster, egg timer, toy pulse emitter, opener/mixer, configurable container/display; walker/lure/predator/fragile-target roles, rope-driven character motor, gravity pad/blimp, toy rocket/ignition/destruction variants and unstable platform. | Resolve historical behavior before authoring. Give each distinct role a focused introduction with familiar supports, then combine safe arrival, timed ejection, sorting or break/cut/ignite goals. End with 7–9 roles and an ordered multi-object task. |
| 11 · **101–110: A Ray of Possibility** | Gamma source, material/thickness shields, powered radiation shutter, collimator, powered X-ray source, rate meter and integrating dosimeter. Separate source control, field control, distance, current rate and accumulated exposure. | Reuse gates, clocks and logic. Begin with source → meter → gate; end with controlled exposure and actual shutter-travel feedback, using 7–9 familiar/new roles together. |
| 12 · **111–120: Particle Manners** | Exposure-sensitive cargo badge, sheet/tank transmission gauge modes, alpha and beta-minus cartridges, scintillator, magnetic deflector and segmented track chamber. | Reuse physical transport, water, optics and electricity: protect a moving parcel, sort by measured transmission, then route a charged trajectory. Advance to 8–10 roles with two simultaneous conditions. |
| 13 · **121–130: Half the Battle** | Track-segment interpretation, decay-clock capsule; neutron source, Fast/Slow detector modes, water moderator and neutron absorber. | Reinforce earlier scintillation before adding decay; combine a rate window with retained exposure. Reuse conserved water and shielding to satisfy multiple sensor conditions, using 8–11 roles and ordered dependencies. |
| 14 · **131–140: Many Happy Returns** | 131–132: RTG heat/cooling/load and startup; 133–134: radiation-responsive material latch and irreversible state; 135: sealed tracer capsule. **136–140 introduce no new elements or modes.** | Apply chapter 9 thermal/work prerequisites. Progress into production cells that inspect, route, charge, release and protect cargo using 9–12 roles across 3–5 domains. Require explicit shutdown/rearm where taught. |
| 15 · **141–150: All Together Now** | **No new required elements, modes, controls or physical rules.** Revisit all adopted families through combinations; do not force every catalogue item into one puzzle. | Progress from coordinated subsystems to 10–14 meaningful roles across 4–6 domains, parallel delivery, constrained supply, feedback and ordered goals. Final puzzles allow different valid machines and require explaining failures from visible state. |
| **Total** | **15 chapters × 10 levels = 150 planned levels.** | Each new family reuses earlier skills; each finale increases integration rather than merely decorating a larger scene. |

The dense chapter allocations are **capacity reservations, not a verified fit**. The breadth of the backlog requires a per-element lesson ledger, staged objectives and rebalancing across chapter boundaries. Do not compress an unfamiliar set into one level just to preserve this table. If hands-on teaching needs more space, redistribute the 150 slots and move introductions earlier; no required element may disappear into workshop-only content to make the numbers work. Potential radiation candidates retain their proposal status until adopted, but all 22 have reserved lessons here.

<a id="campaign-delight"></a>

#### Playful names and a rewarding campaign

**Design acceptance for every item below:** [common gates](#design-acceptance) + [CONTENT](#accept-content); additional profiles apply to cross-cutting requirements.

**Experience goal:** Players feel curious, capable and free to experiment. The reward is seeing an idea work, learning something useful and making a machine their own. Names, progress and celebrations support that experience.

Retain the current naming voice: **A little lightbulb moment**, **Air mail**, **Spring forward**, **A helping weight**, **Wait for it** and **Through the looking tube**. Preserve a good existing title when its lesson survives reordering. Every new level gets a short playful title, a plain-language goal and a useful contextual hint; the pun must never be the only explanation of the mechanic or a prerequisite for solving it.

| Chapter | Playful chapter name | Example level titles / naming direction |
| --- | --- | --- |
| 1 | **On a Roll** | First principles; Spring forward; A little bounce of faith |
| 2 | **A Helping Weight** | Current affairs; Pulley your weight; Air mail |
| 3 | **Pipe Dreams** | Through the looking tube; Wait for it; Queue the applause |
| 4 | **Gears of an Idea** | A turn for the better; Either way works; Not both, surely |
| 5 | **Ready, Set, Throw!** | Load and behold; Catch my drift; A gripping tale |
| 6 | **A Bright Idea** | A little lightbulb moment; Split decision; Prism break |
| 7 | **Go with the Flow** | Water way to go; A moving experience; Siphon of relief |
| 8 | **Sounds Like a Plan** | Bellows and behold; A sound investment; Whistle while it works |
| 9 | **Stored Potential** | Watt a warm welcome; Steam work; A ratchet above |
| 10 | **Oddly Satisfying** | The plot thickens; A good egg timer; A matter of gravity |
| 11 | **A Ray of Possibility** | Ray of sunshine; Mind the gap; Dose of patience |
| 12 | **Particle Manners** | Alpha better; Beta late than never; Light work |
| 13 | **Half the Battle** | Half-life of the party; Slow and behold; Neutron dance |
| 14 | **Many Happy Returns** | Cool under pressure; Trace expectations; Let there be flight |
| 15 | **All Together Now** | Order of operations; Timing is everything; The grand contraption |

These are working titles to test for clarity and tone, not behavior selectors or final localized strings. Prefer an understandable warm name to a forced pun; localize the wordplay independently of typed level identity. Keep radiation names about the toy mechanism and discovery, rather than jokes about real illness or accidents.

<a id="campaign-rewards"></a>

<a id="sequence-task-316"></a>

- [ ] Add a **competence-supporting progression and reward plan** to each chapter. The following are design requirements to implement and playtest, not existing features.

| Principle | Proposed campaign behavior | Acceptance / control |
| --- | --- | --- |
| Clear purpose and immediate feedback | State one concrete goal before Run; show actual subgoal progress and local causes such as “The carrier has not docked yet.” A partial success highlights what worked and what remains. | Feedback follows real simulated state; a failed run is not falsely marked complete. Reset clears transient subgoals, preserves construction and keeps previously earned campaign progress. |
| Earned competence | First-use lessons provide a small editable inventory and a quick achievable experiment. Follow with a small variation, then a satisfying combination. A discovered part gains a notebook card explaining its observed behavior. | Merely opening the palette does not earn a “used successfully” mark. Completed assisted play is still a legitimate completion; only claim demonstrated mastery where the task actually measures it. |
| Meaningful choice | Offer two or three available puzzles when their prerequisites are met, plus Replay and Workshop. Unlock taught parts for creative workshop use without a currency grind. | A branch cannot expose a required untaught mode. All 150 campaign levels remain tracked even when their local order is flexible; optional challenges cannot replace missing introductions. |
| Visible progress | Show chapters and completed levels, with a small chapter-completion keepsake or original workshop ornament in the approved palette. Progress represents solved puzzles and learned connections. | Completion and rewards are persisted once, survive reload and are not duplicated by replay. No points for repeatedly failing, resetting or farming the same easy action. |
| Friendly celebration | Let the successful machine finish its satisfying motion, then give a short skippable celebration and specific feedback such as “Both deliveries arrived in order.” Offer Continue, Replay and Take a break. | Muted and reduced-motion presentation conveys the same success; no mandatory animation delay, blocked controls or exaggerated praise for a failed condition. |
| Optional mastery | After the first completion, reveal one contextual remix such as use less water, design another route or run on a finite charge. Give an optional distinct stamp, without downgrading the original completion. | First-time players see a full success rather than an empty three-star judgment. Remixes are outside the 150-level count, use taught elements and never gate later lessons. |
| Helpful failure and hints | Unlimited Run/Reset/Undo; keep the player's construction. On request, offer goal reminder → relevant causal clue → suggested experiment → explicit solution help. A gentle hint offer may appear after repeated failed attempts and can be dismissed. | No lost lives, currency fee, reward deduction or shaming for hints/assistance. Test wrong connections, near misses and restart; a hint never silently changes game state or imports a solution. |
| Rhythm and curiosity | Introductions are short and focused; combinations build tension; chapter finales provide a payoff and a natural stopping point. Preview a future toy visually without requiring its untaught rule. | Do not pad play with long waits or tedious rebuilds. A late introduction still gets breathing room, and previews do not count as teaching/evidence. |
| Warm purpose and ownership | Frame machines as small helpful jobs: deliver a parcel, return a character home, fill a garden or ring a welcome bell. Keep saved favorite constructions and optional replay captures as personal achievements. | Narrative stays brief and skippable; self-expression does not require social sharing, competitive ranking or copying the author's machine. |
| Comfortable participation | Keep touch controls, readable icons, captions and muted play; offer existing difficulty choices and no default speed scoring. Save/resume makes stopping safe. | Same completion access regardless of hints, audio or difficulty; no streak loss, time-limited rewards, daily obligations, random reward rolls or pressure to keep playing. |

Proposed unlock rule: finish the prerequisite introduction(s) to access dependent puzzles; independent practice branches remain available. A player stuck on a prerequisite may use graduated help, including a guided completion with the same completion reward, then try a transfer problem with help available. Optional optimization never blocks progress. A chapter celebrates completion of its ten core levels; supported side routes change order, not the 150-level denominator.

**Example reward loop:** In **Queue the applause**, release one ball and see its arrival acknowledged; a held-trigger failure reveals why an escapement matters. Finish the queue objective, receive the normal completion and a readable hopper notebook entry, then choose the next available lesson or stop. A later optional “two destinations, one feeder” remix rewards a new solution rather than repeated clicking. In **Half-life of the party**, clearly separate rate and accumulated exposure before offering an optional lower-exposure route.

**Research basis, not a guarantee of fun:** Self-determination research motivates supporting competence, autonomy and connection through usable controls, meaningful choices and feedback. Our proposed notebook, ornaments, hints and unlocks are design hypotheses, not outcomes established for this game. See [Ryan, Rigby and Przybylski's original game-motivation study](https://selfdeterminationtheory.org/wp-content/uploads/2020/10/2006_RyanRigbyPrzybylski_MandE.pdf) and [their later motivational model](https://selfdeterminationtheory.org/SDT/documents/2010_PrzybylskiRigbyRyan_ROGP.pdf). [Tyack and Mekler's review](https://arxiv.org/abs/2405.12639) cautions against superficial use of the theory; verify the experience with players rather than treating badges as proof of motivation.

<a id="campaign-positive-playtests"></a>

<a id="sequence-task-317"></a>

- [ ] Qualify **clarity, enjoyment, agency and reward comfort** alongside behavioral correctness. Sample new and returning puzzle players at the first lesson, a combination and the finale of each chapter. Ask whether they understood the cause, knew a useful next experiment, felt free to try another solution and felt satisfied rather than pressured by rewards. Observe confusing titles, unwanted hint interruptions, forced waiting, repeated identical mistakes, abandoned runs and whether retries produce learning; do not optimize session length as the success metric.

**Successful outcome:** Retain actual observations and revise chapters with confusing introductions, unrewarding loops or difficulty spikes. A solved level or a high completion rate alone is not evidence of fun. Browser tests prove reward persistence, prerequisite unlocks, hint behavior, muted/reduced-motion feedback, retry preservation and no duplicate rewards; human playtests assess the experience. Keep all new gamification work unchecked until both applicable kinds of evidence exist.

<a id="sequence-task-318"></a>

- [ ] Extend the authoring ledger with presentation title/localization key, intended emotional beat, goal wording, hint ladder, prerequisite/unlock rule, reward/optional-remix rule and player-feedback record. Closed sets such as hint stages, reward kinds, completion states and chapter progression states must be enums end-to-end; extensible level/reward IDs remain strongly typed. Titles and puns never select behavior. Test canonical boundary mappings, unsupported values, saved progression and affected callers when implemented.

**Successful outcome:** Each of the 150 planned levels has both a learning purpose and a positive experience brief. Rewards celebrate verified accomplishments without bypassing gradually taught mechanics or hiding any unimplemented element.


#### Complexity and pacing rules

**Design acceptance for every item below:** [common gates](#design-acceptance) + [CONTENT](#accept-content); additional profiles apply to cross-cutting requirements.

- **Introduce → practice → combine → revisit.** Each element/mode gets an identifiable first-use objective, a positive/control contrast, a later player-built combination and a further reuse after another chapter where space permits. Fixed background fixtures do not count as teaching a part the player has never used.
- **One new distinction at a time.** Introductory levels temporarily reduce inventory and layout complexity, even late in the campaign. Chapter finales rise in complexity; the curve is a staircase with short rests, not an obligation for every single level to exceed the previous one.
- **Count meaningful roles, not copies.** The ranges above count decisions such as feed, detect, store, route, convert, compare and release. Ten repeated pipes or decorative sources do not constitute ten interacting puzzle elements.
- **Increase dependency depth deliberately.** Levels 1–20 favor 1–3 causal steps and one goal; 21–50 add 3–5 steps, memory and rearming; 51–80 add 4–6 steps and cross-domain conversion; 81–110 add resource accounting and visible feedback; 111–140 add parallel conditions and ordered deliveries; 141–150 coordinate several taught subsystems. These are design targets to validate through play, not measured difficulty claims.
- **Change the problem, not only precision.** Add a second destination, finite supply, restricted inventory, conflicting timing windows, transport of the source/receiver, a shared machine resource or a need to recover/reuse material. Do not substitute tighter placement tolerances, opaque timing or longer waiting for reasoning.
- **Teach prerequisite order.** Supply precedes powered devices; physical containment precedes moving cargo; delay/hold precede clocked metering; independent inputs precede logic; flow/conservation precede pressure; heat/finite work precede RTG; rate precedes accumulated exposure. Every supported mode/logic operation must be taught before its use.
- **Keep solutions open.** Judge actual outcomes and resource/ordering constraints, not similarity to a reference layout. Allow at least two meaningfully different constructions in designated open synthesis levels. A shorter valid solution is useful evidence for balancing, not automatically an exploit.
- **Keep difficulty evidence separate.** Forgiving/Balanced/Precise control both authored tolerance/nudging and the planned [physics-realism profile](#difficulty-physics-realism). Easy simplifies secondary physics, medium uses an intermediate model and hard uses the most realistic supported effects; all retain the mechanism being taught and require actual causal success. Record and qualify each profile separately.

<a id="campaign-element-coverage"></a>

<a id="sequence-task-319"></a>

- [ ] Build the **element × supported mode × introduction × practice × combination × later reuse** ledger from every catalogue part, fixture, TODO child record and linked research candidate. Give each distinct element a separate row with typed part/mode identity, prerequisite lesson IDs, proposed/implemented/authored/verified state and evidence links. Grouped entries below are enumeration obligations, not completed coverage.

| Coverage obligation | First-use reservation | Combination / spaced reuse reservation |
| --- | --- | --- |
| Basketball, bowling, tennis, baseball, soccer and programmable physical presets; ramp/wall/material variants, basket, domino, springboard, bumper, trampoline, impact lever, cushion/cradle | 1–10; later preset-specific lessons may occupy 91–100 after material rules | 21–30, 41–50, 91–100, 136–150 |
| Battery, wire, switch, motor, belt, chain, conveyor, reverse transmission; weight, rope, cable, fixed/moving pulley roles, hook/anchor, scissors and tin snips; bucket/cage, balloon tether/pop, fan | 11–20; moving/load variants extend into 41–50 | 31–50, 61–80, 136–150 |
| Ball straight pipe, both bend angles, funnel, powered and mechanical gates, fixed/powered/alternating diverter, hopper/escapement, reload shuttle; spiral delay, airlift and trapdoor variants | 21–30; shuttle 41–50; specialist routes 91–100 | 41–50, 111–120, 136–150 |
| Ball detector, pressure plate, delay, hold timer, Set/Reset latch, counter/reset input, clock, edge detection and separately supplied electrical AND/OR/XOR/NOR/NAND | 21–40; a new rearm/reset mode needs its own objective | 41–80, 101–150 |
| Gears, generator, mouse motor, windmill, clutch, brake; both conversion directions, crank-slider, each cam profile, rack-and-pinion | 31–40 | 41–50, 61–90, 136–150 |
| Pusher, boxing glove, flipper, wound launcher, jack-in-the-box, passive scoop, counterweighted scoop, slingshot, hinged catapult, cannon, magnetic impulse relay, zipline, electromagnet, gripper, lift, ferry and releasable bridge/joint | 41–50; each distinct contact/charge/carry contract remains separate | 61–100, 111–120, 136–150 |
| Flashlight, lamp, drawstring source, solar panel, each laser/receiver channel, mirror, shutter, splitter, filters, combiner/mixed receivers, converging/diverging lens, prism, fiber/light pipe; each optical logic operation | 51–60; heat target returns in 81–90 | 71–90, 111–125, 136–150 |
| Reservoir, header tank, manual/mechanical/solenoid tap variants, basin, water funnel, drain, gutter; straight pipe, 45°/90° elbows, T, cap and nozzle | 61–70, with individually exercised construction objectives | 71–90, 114, 126–130, 136–150 |
| Waterwheel; loaded/leaky bucket; float, mechanical float valve, supplied level switch; check valve, water diverter, sluice, pump, Archimedes screw, siphon/priming bulb, tipping bucket/water clock | 61–70 | 71–90, 114, 126–130, 136–150 |
| Communicating tanks, canal lock, cork/raft/boat/floating platform, hydraulic piston, distinct flow/volume/pressure meters | 61–70; distinct hull/load modes retain separate rows | 81–100, 126–140, 146–150 |
| Bellows, pneumatic hose, air reservoir, release valve, directional valve, spring-return and double-acting piston | 71–80 | 91–100, 136–150 |
| Speaker/pulse/continuous modes, sound meter, bell, wind chimes, each tuned band, whistle, listening horn, exit horn, each duct route, resonator, acoustic screen, reflecting dish, water-tuned bottle | 71–80; water-tuned bottle follows chapter 7 water | 81–100, 117–125, 136–150 |
| Ratchet, escapement, indexed carousel, flywheel, governor, accumulator; sponge, wick, squeeze pad, sprinkler and rain collector | 81–90 | 91–100, 131–140, 146–150 |
| TH-01–37: every thermal element has an individual specification and distinct lesson/control | [Exact thermal allocation](#thermal-campaign-allocation): prerequisites14–20,39–41,55–61; phases66–80; storage/control81–88; ejector91 | Per-record practice/reuse through150; earlier broad81–90 thermal reservation is superseded without removing its storage/mechanical lessons |
| Size grate, weight tray/material sorter, timed ejector/toaster, egg timer, phazer-like toy pulse source, opener, mixer, box presets and contact display | 91–100; familiar control contracts from 21–40 are prerequisites | 111–120, 136–150 |
| Original cat/mouse lure/escape roles, walker, predator, fragile fish-bowl/tank target, rope-to-drive character motor; gravity-effect pad, drifting blimp, rocket, toy revolver, dynamite/plunger abstraction and tipsy/unstable platform | 91–100; distinct powered, fragile, fired, ignited and safe-arrival outcomes get separate objectives | 136–150, using alternatives where a level's role permits |
| RAD-01/02/05/06/07/08/09: gamma/X-ray, each shield material/thickness, shutter, collimator, rate meter and dosimeter | 101–110 | 111–120, 124–130, 136–150 |
| RAD-03/04/10/11/12/14/15: alpha/beta-minus, badge, both gauge modes, scintillator, both field polarities, each chamber segment | 111–120 | 121–125, 136–150 |
| RAD-13/16/17/18/19: decay presets, neutron source, moderator, absorber, Fast/Slow detection | 121–130 | 136–150 |
| RAD-20/21/22: RTG, material latch and sealed tracer | 131–135 | 136–150 |
| GAP-01–18: structures, vehicles, granular/processed material and supporting tools | Exact per-record/mode objectives at [campaign gap allocation](#campaign-gap-allocation), first use 2–100 | Individual practice/reuse through 150; no required introductions in 136–150 |
| Cosmetic-only edition/holiday variants | No new mechanic slot required; record which already taught behavior they share | Verify appearance/accessibility without inventing duplicate completion credit |

**Successful outcome:** Every required element and every adopted candidate has named lesson objectives and later combinations. Every grouped row is expanded before chapter closure; missing/overcrowded/unverified assignments stay open. Nothing is declared taught just because its family appears in this table.

<a id="campaign-gap-allocation"></a>

#### Cross-game additions: exact objectives within the 150 levels

**Design acceptance for every item below:** [common gates](#design-acceptance) + [CONTENT](#accept-content); additional profiles apply to cross-cutting requirements.

All 18 [GAP specifications](#cross-game-gaps) are now planned within the same **15 × 10 = 150** campaign. The following allocation supplements the chapter and original element tables; it does not replace their lessons. Numbers identify existing campaign slots; pun titles identify distinct lesson objectives within them. Keep stable level IDs and original content/evidence identities when rewriting titles. No extra campaign level or workshop-only substitute is counted.

**Staging rule:** In a shared slot, teach its original new distinction first with familiar supports, celebrate that small success, then reveal a separate short GAP objective with a positive/control contrast. Do not require both unfamiliar ideas in the first construction. Preserve the original lesson as an independently recorded objective. Stage transitions must retain the player's saved construction/Undo context or explicitly save it before a fresh mini-setup; no surprise loss of work. Later levels combine the learned pieces with progressively less scaffolding.

**Tool integration:** Save, inspection, playback, library and editor lessons use the player's actual machine and contextual UI, not inventory parts. Offer guided practice at the listed checkpoints; hints/slow playback and declining publication never cost campaign progress. Editor/export interactions can sit after the base puzzle's success as a local creative invitation. A later puzzle cannot require a skipped tool interaction without offering its guidance again. Creating an account, sharing publicly or switching profiles is never a win condition.

| Record / lesson objective | First introduction / prerequisites | Guided practice | Later combinations and spaced reuse |
| --- | --- | --- | --- |
| [GAP-01 beam/brace](#gap-01) · **Brace Yourself** | **4**, after basic placement/gravity; support a familiar basket with one member, then contrast braced/unbraced geometry without teaching break thresholds yet. | **5**, support a different load/route. | **18** rolling chassis; **65** changing water load; **138** structural docking platform. |
| [GAP-02 pivot/linkage](#gap-02) · **Joint Effort** | **9**, after the lever objective; fixed/free axis then limits in separate guided steps. | **10**, link a familiar lever to a catcher. | **38** driven linkage; **46** loading arm; **138** docking joint. |
| [GAP-03 passive wheel](#gap-03) · **Wheel Meet Again** | **18**, after ropes/moving cargo and environment practice; roll a preloaded beam chassis, then build its axle. | **19**, rolling versus slipping with familiar cargo. | **37** powered conversion; **68** cargo alongside a lock; **138** two-carrier transfer. |
| [GAP-04 driven wheel](#gap-04) · **Driven to Deliver** | **37**, after motor, supply, belt and gear lessons; drive an already understood chassis. | **38**, add a familiar linkage under load. | **48** powered loading carrier; **68** haul water; **138** shared docking. |
| [GAP-05 load-limited connector](#gap-05) · **The Last Straw** | **43**, after stable beams/pivots and chapter 4 load/drive; compare one supported breaking mode below/above threshold. | **44**, use a deliberate physical release. | **65** changing water weight; **96** catch processed cargo within load limits; **139** protect a shielding carrier. |
| [GAP-06 grains](#gap-06) · **Against the Grain** | **91**, after conservation, containers and existing size/material distinctions; finite feed and jamming are separate trials. | **92**, meter into a taught weighed container. | **96** feed a processing cell; **136** sort/count material; **141** supply two orders. |
| [GAP-07 sieve](#gap-07) · **Sift Happens** | **93**, after grains; reuse the existing size-sorter lesson but explicitly contrast individual balls with granular feed. | **94**, sort a new mixture before the coating introduction. | **97** sorting under taught gravity; **136** mixed parcel/feed handling; **141** two fractions to two destinations. |
| [GAP-08 coating/dye](#gap-08) · **Coat of Many Colours** | **94**, after sieve practice and finite fluid supply; coating and any adopted dye mode have separate objectives and patterned state cues. | **95**, verify finite coverage before fragmenting anything. | **98** create a surface-state puzzle; **136** inspect treated output; **150** prepare cargo for delivery. |
| [GAP-09 fragmentation](#gap-09) · **A Smashing Success** | **95**, after coating practice and taught press/impact work; compare intact and sufficient-work outcomes. | **96**, size and catch fragments with familiar sieve/limits. | **99** timed processing; **141** distribute bounded fragments; **150** integrated preparation. |
| [GAP-10 assemblies](#gap-10) · **Some Assembly Required** | **20**, once individual placement, connections and Undo are familiar. Copy a small known conveyor/catcher group. | **30**, reuse a feeder with explicit exposed ports. | **50** loader assembly; **100** personal module library; **146** coordinated delivery hub. |
| [GAP-11 editor](#gap-11) · **Mind Your Own Business** | **98**, local post-win guided remix: fixed/movable setup and goal authoring in separate steps. Uses already taught goals and materials. | **100**, test a puzzle using its restricted inventory. | **140** author a known end-state constraint; **150** save a personal finale remix. |
| [GAP-12 exchange](#gap-12) · **Handle with Care** | **30**, machine save/export preview; no new puzzle-authoring concepts yet. Puzzle-file export is introduced separately at **100**, after GAP-11. | **50** machine round-trip; **101** local puzzle-file round-trip checkpoint after the base lesson. | **100** saved machine reuse; **140** authored-puzzle exchange preview; **150** portable creation. Public sharing remains optional. |
| [GAP-13 save library/profiles](#gap-13) · **A Place for Everything** | **2**, save/reopen the first construction; introduce an optional local profile selector at the chapter **10** return screen. | **10**, keep two named attempts; exercise profile isolation in feature proof, not a compulsory puzzle. | **30** return to a saved machine; **100** library curation; **150** favourite invention. |
| [GAP-14 transport](#gap-14) · **Wait a Second** | **3**, pause/resume, then single-step/slow playback as separate contextual demonstrations. | **8**, inspect a familiar contact chain. | **29** rate/timing; **80** pneumatic/acoustic sequence; **148** several time windows. Replay/seek is separately specified before use. |
| [GAP-15 inspection](#gap-15) · **Cause for Celebration** | **7**, inspect a blocked gravity path; at **15**, introduce electrical supply feedback with the first familiar powered machine. | **15** disconnected versus supplied; **39** load/shaft feedback. | **66** inspect fluid load; **145** trace startup/cooling/load dependencies. New measured channels receive contextual guidance before use. |
| [GAP-16 goals](#gap-16) · **A Steady Job / Going Steady / First Things First / Leave It Lovely** | **25** quantity; **29** rate window after delay/clock; **49** ordered events after loading; **89** protected end state after thermal control. Each variant is a separate first-use objective. | **26 / 30 / 50 / 90**, respectively. | Quantity **96, 136**; rate **97, 140**; order **100, 146**; protected end state **100, 150**. |
| [GAP-17 build zones](#gap-17) · **Room for Improvement** | **6**, after basic placement; distinguish the allowed footprint from collision walls and show a failed overhang. | **8**, rotate a known piece inside a zone. | **19** chassis placement; **42** launcher footprint; **147** constrained supply/assembly layout. |
| [GAP-18 environment](#gap-18) · **Down to Earth / Air on the Side** | **17** authored gravity after familiar gravity paths; **79** supported atmosphere after pressure/air lessons. Difficulty remains a separate choice. | **18 / 80**, respectively. | Gravity **97, 139**; atmosphere **99, 143**. Unsupported presets stay blocked until implemented and taught. |

**Chapter integration:** Chapters 1–2 gain construction basics and experiment tools; chapters 3–5 add assemblies, goal variants, driven vehicles and load limits; chapters 7–9 reuse construction with fluids, air and thermal end states; chapter 10 introduces granular/material processing and authoring. Chapters 11–14 retain all 22 radiation reservations and reuse the new skills. Levels **136–150 still introduce no new required mechanics or modes**.

<a id="sequence-task-320"></a>

- [ ] Expand the above objectives into the typed per-mode authoring ledger and playtest each staged level for time, clarity, fatigue and reward pacing. Track original and GAP objectives separately, with actual player construction and meaningful controls.

**Successful outcome:** All 18 GAP records have an introduction, guided practice and at least two later applications, with explicit mode exceptions/checkpoints recorded above; all original element reservations remain present. A chapter closes only after its individual objectives fit a positive play session. This is a complete proposed allocation, not proof that the currently dense chapters are well paced. Rebalance objectives within the 150 slots if playtests reveal crowding, preserving prerequisites and coverage rather than silently deleting lessons.

<a id="campaign-synthesis"></a>

<a id="sequence-task-321"></a>

- [ ] Author **136–150 as increasingly integrated machines**, using only previously taught elements/modes. These are proposed puzzle briefs, not finished levels.

| Level | Problem to solve | Required combination and escalation |
| --- | --- | --- |
| 136 | **Sort of a Big Deal** | Meter finite granular/cargo feed, sieve sizes, inspect coated output and count correct deliveries; combine taught material processing with feeder, gauge, detector, logic and physical sorting. |
| 137 | **Every Drop Twice** | Use a finite water supply to drive transport, catch the discharge and lift a float-controlled exit; integrate work, storage and a second use of the same material. |
| 138 | **Dock and Roll** | Build a braced docking platform and pivot transfer, then coordinate passive/driven wheeled carriers sharing it; use sensing, latch/logic, brake and taught pickup, with no premature unload. |
| 139 | **The Sheltered Crossing** | Move exposure-sensitive cargo while a load-limited carrier repositions shielding in a previously taught gravity preset; synchronized paths must satisfy exposure and structural limits. |
| 140 | **Many Happy Returns** | Start a finite-power thermal/mechanical cell, deliver a batch, then stop feeding and close the relevant source paths without undoing success. |
| 141 | **Two Orders, One Machine** | Sieve finite feed and divide bounded processed fragments between two destinations with a shared loader; retain order/count, conserve material and prevent route conflicts. |
| 142 | **The Lantern Exchange** | Convert radiation to visible light, optical conditions to a supplied acoustic signal, and the acoustic response into a physical release. Every conversion and supply must work. |
| 143 | **A Voice Across Water** | Water-tune a resonator/bottle while a pneumatic pulse drives its sound path; that signal permits a charged mechanical delivery through a timed gate. |
| 144 | **The Water Observatory** | Fill a moderator, verify the required neutron group, adjust absorber/shutter state and route a protected parcel; feedback and transport compete for finite resources. |
| 145 | **The Quiet Engine** | Cold-start cooling/RTG power, operate a modest conveyor/lift and release a dose-controlled material latch; manage startup energy, load and exposure together. |
| 146 | **Order of Operations** | Reuse player-built loader assemblies with explicit exposed ports to dispatch three payloads through a shared hub in a required order; combine sensing, counting, memory, docking and distinct delivery methods. |
| 147 | **Watt Goes Around** | Within taught construction zones, share limited electrical, stored mechanical and fluid supplies between dependent tasks; recover useful discharge/work without a perpetual-power loop. |
| 148 | **Timing Is Everything** | Deliver while a receiver/dock alignment, decay-rate window and stored exposure condition overlap; provide broad readable windows before final tuning. |
| 149 | **The Last Delivery** | Protect fragile cargo or a taught character along a multi-stage route, use a tracer checkpoint to change a branch and complete a separate destination condition. |
| 150 | **The Grand Contraption** | Coat and/or fragment cargo through taught operations, inspect it, coordinate parallel routes and ordered deliveries, then leave the machine in its protected final state. Combine 10–14 roles across 4–6 taught domains with limited resources and two verified valid constructions. Offer a post-win local remix, named save and portable export; publication is not a win condition. |

No finale requires every part simultaneously. Across these levels, distribute representative uses from every adopted family while the per-element ledger tracks the full catalogue's earlier practice/reuse. Later complexity comes from dependencies among subsystems, not prescribed wiring or an unintroduced trick.

<a id="campaign-authoring-gate"></a>

<a id="sequence-task-322"></a>

- [ ] For each of the 150 levels, record typed level identity, chapter, first-use element/mode IDs, prerequisite lessons, available/fixed inventory, ordered goals, causal depth, distinct functional roles/domains, resource constraints, intended failure/control and allowed alternative solutions. Keep proposed, authored, native-verified, real-UI-verified and difficulty-qualified states distinct; retain boundary validation and reject unsupported IDs/modes rather than parsing display names.

**Successful outcome:** A prerequisite audit finds no required first-use gap, the dependency graph has no forward reference, all 150 slots are accounted for exactly once and hands-on players can explain the causal solution. Native/reference success alone does not close teaching or difficulty qualification. All new per-element delivery, Reset/save, build and UI proof obligations remain in force.

<a id="todo-081"></a>

<a id="sequence-task-323"></a>

- [ ] Integrate the trampoline introduction into the final gradual 150-level ordering and add the rebound-to-pipe lesson; sustained/mobile motion and broader combinations remain open.

**Successful outcome:** The rebound introduction precedes a new trampoline-to-pipe puzzle; the player learns aim and entry alignment without needing an unexplained optical, rope or timing mechanism.

<a id="todo-090"></a>

<a id="sequence-task-324"></a>

- [ ] Integrate the two launcher lessons into the final gradual 150-level ordering and add later obstruction combinations as campaign balancing requires. Focused introductory and retained-charge teaching is verified; final campaign progression and exhaustive difficulty testing remain open.

**Successful outcome:** Players first learn wind-and-release, then retained charge after power stops; a later obstruction combination clearly requires those learned behaviours.

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="todo-393"></a> [ ] <a id="sequence-task-325"></a> P1 **Cannon campaign lessons** | Add progressively taught loading, separate charging/triggering, aiming, obstruction and feeder lessons within the 150-level campaign. Prove each through the real UI; keep author-defined forgiveness and gradual difficulty. | Begin with one supplied shot into a receiver, then introduce gates, timed reload, gaps and funnels. |

<a id="todo-245"></a>

<a id="sequence-task-326"></a>

- [ ] Add an introductory powered-gate lesson and later mechanical actuation. Neither is implemented in this batch.

**Successful outcome:** A simple gate lesson contrasts powered release with an unpowered retained ball. A later mechanical gate uses actual mechanical input and receives its own controls/proof.

<a id="todo-263"></a>

<a id="sequence-task-327"></a>

- [ ] Add short hold-timer/gate campaign lessons. Full difficulty testing remains deferred.

**Successful outcome:** The player uses a timed open window to pass a ball and can observe why too short a hold leaves it waiting.

<a id="todo-266"></a>

<a id="sequence-task-328"></a>

- [ ] Add a short detector/pressure-plate/timer/gate teaching sequence when expanding the campaign. Pressure sensing currently sums masses directly touching the top; transmitted stack/rope forces are not simulated by this plate.

**Successful outcome:** Separate lessons teach directional detection, sufficient contact mass, a timed signal and controlled passage before a combined machine requires all four.

<a id="todo-268"></a>

<a id="sequence-task-329"></a>

- [ ] Add a latch teaching level: first detector starts a conveyor; arrival detector stops it. Broader logic combinations and the final difficulty matrix remain pending.

**Successful outcome:** A first detector starts a supplied conveyor and a later arrival detector stops it; the player sees retained memory rather than mistaking the latch for a timer.

<a id="todo-270"></a>

<a id="sequence-task-330"></a>

- [ ] Add a counter teaching level and later explicit reset-signal support alongside resettable logic. The current counter resets through workshop Reset, not a dedicated control input.

**Successful outcome:** The player must deliver the target number of distinct arrivals; fewer arrivals leave the exit closed. Later reset-signal support visibly clears the count for another cycle.

<a id="todo-272"></a>

<a id="sequence-task-331"></a>

- [ ] Teach Both gates with two independent physical conditions (e.g. ball waiting and lift docked). Implement AND/OR/XOR/NOR/NAND in electrical and optical domains under the expanded research below; broader campaign trials remain pending.

**Successful outcome:** An interlock releases cargo only when both real physical conditions hold and the actuator is supplied; a logically true but unpowered circuit stays inactive.

<a id="todo-287"></a>

<a id="sequence-task-332"></a>

- [ ] Add introductory mirror routing lessons and broader moving-obstacle/multiple-source performance checks. Beam splitting is implemented below; finite-width lens coverage remains pending.

**Successful outcome:** A first mirror puzzle can be solved by orienting one reflective face, then an obstacle puzzle requires reflection rather than a direct hidden route.

<a id="todo-293"></a>

<a id="sequence-task-333"></a>

- [ ] Teach beam shutters in the campaign; rope/weight actuation and charged receivers remain pending. Sampled browser captures do not prove continuous animation fluidity or mobile performance.

**Successful outcome:** A player times a physical shutter to interrupt or expose a beam and sees the downstream receiver/gate respond; rope actuation and charging remain separate unfinished behaviours.

<a id="todo-296"></a>

<a id="sequence-task-334"></a>

- [ ] Add splitter teaching levels, adjustable ratios only if useful, and larger-scene browser performance checks. Centre-ray tracing does not yet model finite-width coverage or lenses.

**Successful outcome:** The player routes two weaker branches to separate receivers; blocking one branch or splitting below threshold visibly prevents the intended interlock.

<a id="todo-302"></a>

<a id="sequence-task-335"></a>

- [ ] Teach filtering, mixing and colour matching in campaign levels. Verify mixed-colour receiver variants beyond the yellow UI smoke, selected combiner previews, broader contrast/mobile/performance and sustained illumination; do not equate native rules or sampled captures with all of those checks.

**Successful outcome:** Separate lessons teach removing a colour, combining existing colours and matching the resulting receiver. Every mixed-colour variant receives its own positive and wrong/missing-channel UI proof.

<a id="todo-370"></a>

<a id="sequence-task-336"></a>

- [ ] Teach **One Passenger Only** (XOR; both loaded fails), **Quiet Lock** (NOR; two sound detectors silent), **Two Light Windows** (optical AND), **Alternating Shadows** (optical XOR) and **Duet Veto** (NAND). Include true-condition/missing-supply failures.

**Successful outcome:** Each electrical and optical operation has a small puzzle where changing one input changes the outcome for that operation's reason; missing supply/carrier still fails even when the condition is true.

<a id="todo-355"></a>

<a id="sequence-task-337"></a>

- [ ] Teach **Across the Gap** (speaker/meter), **Wind in the Tower** (fan/chimes), **Three O'Clock at the Mill** (waterwheel/bell/counter) and **Fill to Sing** (water-tuned bottle closes its tap).

<a id="todo-437"></a>

<a id="sequence-task-338"></a>

- [ ] Teach one distinction at a time: **Bank the Bounce**, **Catch, Then Lift**, **Load Before Fire**, **Save the Spring**, **One Ball at a Time**, **Hold the Light**, **Dock Before Drop** and **Carry Without Iron**. First use nearly planar layouts; later combine depth, optics, sound, water and logic. Reallocate within the 150-level target, allowing additional workshop challenges without pretending these levels already exist.

<a id="todo-279"></a>

<a id="sequence-task-339"></a>

- [ ] Teach wait-before-release → hold-open duration → remembered activation → count arrivals → interlock two conditions before requiring clocks or compound timing.

<a id="todo-313"></a>

<a id="sequence-task-340"></a>

- [ ] Teach hit a receiver → interrupt with a ball → reflect around an obstacle → gate a beam → split limited power → filter/mix colours → command a powered machine → focus/spread light → separate colours with a prism.

<a id="todo-254"></a>

<a id="sequence-task-341"></a>

- [ ] Teach almost-planar routing first, then meaningful depth: around a corner → catch and bank → descending delivery → hold/release → powered transfer → stagger two deliveries through a shared crossing.

<a id="todo-274"></a>

<a id="sequence-task-342"></a>

- [ ] Teach the repeating clock with a dispenser/alternating pipe junction once those parts exist. Adjustable player-facing timing controls, broader logic and campaign difficulty testing remain pending.

**Successful outcome:** An already taught dispenser/diverter repeats at the chosen interval; loss and restoration of supply follow the documented phase rule without a burst of missed events.

<a id="todo-230"></a>

<a id="sequence-task-343"></a>

- [ ] Allocate a first-use objective and later combination for every required P1/P2/P3 element in the [150-level coverage ledger](#campaign-element-coverage), including unimplemented variants. Adjust chapter boundaries after focused prototypes; potential candidates keep an explicit adoption status, and no required element is silently excluded for lack of space.

<a id="todo-379"></a>

<a id="sequence-task-344"></a>

- [ ] Rebalance the [150-level plan](#campaign-plan) after prototypes and player feedback: one new distinction at a time, spaced practice, then increasingly integrated machines. Optional workshop remixes supplement the campaign; they cannot replace a required element's introductory lesson. Retain exactly 150 campaign slots.

**Successful outcome:** Prototype and player-feedback results inform the first lesson, later combination and reward beat for each distinct mechanic. All 150 required levels remain teachable, playful and progressively more complex; every tracked element retains its explicit scope and evidence status.

<a id="core-parts"></a>

## Priority 4 — add reusable routing, timing and mechanical choices

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

<a id="transport"></a>

### 4.1 Predictable ball routing, queues and controlled release

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** First new-part family: clear, reusable routing and sequencing decisions.

**Gameplay outcome:** Players join a visible route, hold a queue, release one physical ball and choose its destination. Wrong joins, blocked outlets and insufficient power fail visibly without teleporting or deleting cargo.

Finish the shared-engine seams and mouth rules first, then diverter and hopper, then interlocked feeders and powered/advanced routes. Introduce each as a separate part and teaching step.

<a id="todo-252"></a>

<a id="sequence-task-345"></a>

- [ ] Add explicit compatible inlet/outlet port IDs and orientation to connection definitions. Overlapping silhouettes do not establish connectivity. Test seams, fast-ball tunnelling, blocked outlets, queues, simultaneous arrivals and disconnected mouths.

**Successful outcome:** Every join records the actual oriented mouths; overlapping artwork alone cannot make a route pass balls or satisfy a connection goal.

<a id="todo-251"></a>

<a id="sequence-task-346"></a>

- [ ] Complete **automatic snapping between nearby tube openings** (straight tubes and bends implemented; broader verification pending) during placement/movement: detect compatible mouths within a bounded proximity, align their position and facing direction, show the proposed join subtly, and commit it as one undoable edit. Support straight-to-bend and bend-to-bend joins in 3D. Never snap incompatible bores or move a running machine; keep editor snapping separate from hidden difficulty nudging. Verify physical seams and separation/Undo/Reset.

**Successful outcome:** Compatible pipe mouths show a subtle proposed join and commit as one undoable movement. Incompatible bores, occupied mouths and excessive reach cannot snap or silently connect.

<a id="todo-122"></a>

<a id="sequence-task-347"></a>

- [ ] Expand bend coverage: blocked/queued/high-speed flow, incompatible-bore fixtures, multi-piece gravity-driven joins, larger placement-error sweeps and continuous motion/performance remain unfinished. The first generic nudge profile made some successful errors worse; preserve that finding and test monotonicity more broadly.

**Successful outcome:** Queued, fast and blocked balls behave physically at bends, including joined rotated pieces. Focused cases close before delivery; the broad placement-error sweep remains in final qualification.

<a id="todo-128"></a>

<a id="sequence-task-348"></a>

- [ ] Extend pipes with funnels, blocked-outlet/queue/seam tests and controlled gates. There are no transport-network links or hidden path-following. Broader motion/performance and repeated placement-error coverage remain unfinished.

**Successful outcome:** A player can collect through a funnel, wait behind a gate and travel across real tube seams; blocked queues remain visible and no cargo is teleported along a network.

<a id="todo-243"></a>

<a id="sequence-task-349"></a>

- [ ] Expand funnel puzzles and edge cases: bumper-launched arrivals, oversize jams, multi-ball queues and rope-loaded contact. No resize controls on the funnel; arbitrary routing and the difficulty matrix remain pending.

**Successful outcome:** A launched ball can be caught by a suitable funnel; oversize cargo jams visibly and a loaded/queued inlet does not delete balls or bypass rope contact.

<a id="todo-246"></a>

**Scope index — individual element specifications:** [EL-162](#element-162), [EL-163](#element-163), [EL-164](#element-164). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-350"></a>

- [ ] **Separate integration verification:** A visible Y-junction selection sends the next ball down the chosen physical branch. Teach fixed selection first, then add and separately prove powered/alternating modes. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="todo-247"></a>

<a id="sequence-task-351"></a>

- [ ] Next: hopper with a one-ball escapement, visible queue and one release per trigger. Example: feed two destinations alternately without releasing the whole supply.

**Successful outcome:** A hopper stores a visible queue and releases exactly one ball per accepted trigger; held input, an empty hopper or a blocked outlet cannot dump or duplicate the queue.

<a id="todo-319"></a>

<a id="sequence-task-352"></a>

- [ ] Prototype a dispenser + alternating junction + counter combination: distribute six balls into two groups of three to unlock the goal. Each piece must visibly explain its current state.

**Successful outcome:** Six physical balls reach two destinations in groups of three because the feeder, alternating junction and counter work together; changing a route or withholding an arrival prevents the goal.

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="todo-398"></a> [ ] <a id="sequence-task-353"></a> P2 **Reload shuttle with chamber interlock** — expand feeder | Supplied sliding pocket advances one physical queued ball only when chamber is empty and launcher has returned. Show queue, occupied pocket and jam; never delete overflow. | Conveyor → hopper → shuttle → cannon; shot counter stops after three deliveries. Test two-ball pressure, held trigger, early feeding, supply loss mid-stroke and blocked outlet. |

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-220"></a> [ ] <a id="sequence-task-354"></a> P2 | Trap door separates objects in a TIM2 puzzle. [Easy walkthrough](https://sierrachest.com/index.php?a=games&fld=walkthrough&id=229&pid=101) | Add a hinged/selective passage. Verify its selection rule rather than guessing “size filter”; test thresholds, heavy/light bodies and moving collision geometry. |

<a id="todo-248"></a>

**Scope index — individual element specifications:** [EL-107](#element-107), [EL-108](#element-108), [EL-109](#element-109). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-199"></a> Scope index | Individual element specifications: [EL-071](#element-071), [EL-072](#element-072), [EL-073](#element-073), [EL-074](#element-074). | Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

<a id="timing"></a>

### 4.2 Adjustable timing, memory and dependable logic

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** After typed power/control and reliable Run/Reset.

**Gameplay outcome:** Players deliberately wait, hold, remember, count and repeat. A visible timer explains when a gate will open; loss of power and Reset have predictable effects.

Existing delay, hold, latch, clock and logic implementations are the starting point. Complete adjustable/rearming/reset-input work and mode-specific proof; preserve the latch's documented retained-memory behaviour instead of applying a blanket volatile-state rule.

<a id="todo-277"></a>

<a id="sequence-task-355"></a>

- [ ] Use simulation-time ticks and deterministic event ordering. Pause freezes timers and physics together; slow motion scales both. Explicit Reset cancels pending events and clears timer/counter/latch state; run Reset restores authored initial state. Prevent zero-delay feedback loops and retain brief events across slow rendered frames.

**Successful outcome:** Pause stops the countdown and machine together, slow motion changes their pace together, and Reset removes pending pulses. A short signal still reaches its intended consumer at low frame rates.

<a id="todo-278"></a>

<a id="sequence-task-356"></a>

- [ ] Specify loss-of-power rules: actuator power loss stops action without replaying missed one-shot events when restored. If logic requires external power, losing it clears volatile state/countdowns; restoration samples input without fabricating a rising edge. Test simultaneous Reset/trigger and blocked gates without clipping through balls.

**Successful outcome:** A stopped powered actuator never replays an unserved trigger when electricity returns. Document and test each module's memory rule; the existing Set/Reset latch retains memory across supply loss and must not be silently made volatile.

<a id="todo-368"></a>

<a id="sequence-task-357"></a>

- [ ] Define hysteresis, Reset and edge-event contracts; show A/B/output/supply state with domain-specific socket/lens icons. Update Both content/callers/tests together.

**Successful outcome:** A/B, output and supply/carrier cues explain the live state; thresholds do not flicker and Reset restores the defined state without phantom edges.

<a id="todo-365"></a>

<a id="sequence-task-358"></a>

- [ ] Extend electrical evidence with deliberate memory-boundary circuits, more complex reconvergence, large-network performance, mobile icon/message readability and campaign lessons.

<a id="todo-132"></a>

<a id="sequence-task-359"></a>

- [ ] Expand delay verification with repeated placement-error trials and broader timing puzzles. The initial module is one-shot per Run; player duration adjustment, rearming delay pulses, clocks and resettable memory/logic remain unfinished. Hold timers, counters and switched electrical contacts are separate implemented modules. Do not count it as the whole electronics family.

<a id="todo-260"></a>

<a id="sequence-task-360"></a>

- [ ] First: **delay box — wait, then trigger**. A rising-edge event starts an adjustable countdown (prototype default 1 s), then emits one event even if the input has returned off. Ignore retriggers while busy and show that state. Example: release a ball after its catcher arrives.

**Successful outcome:** Complete the adjustable wait-then-trigger contract using the existing delay implementation: a brief input still yields one delayed output, busy retriggers do not restart it and the countdown is readable. Do not re-add the already completed one-shot prototype as a separate part.

<a id="todo-234"></a>

<a id="sequence-task-361"></a>

- [ ] Use new outcome predicates where required: popped, cut, ignited, broken, ejected, powered, beam-interrupted and arrived-safely. Acceptance must follow actual events, not proximity to the author's reference arrangement.

**Successful outcome:** The goal changes only when the actual popped/cut/ignited/broken/ejected/powered/interrupted/arrived event happens, never merely because a player placed a part near the reference solution.

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-213"></a> [ ] <a id="sequence-task-362"></a> P2 | Egg timer participates in delayed triggering. [TIM2 walkthrough](https://sierrachest.com/index.php?a=games&fld=walkthrough&id=229&pid=102) | Add a readable countdown actuator. Test zero delay, pause/Reset and ordered goals; keep author configuration separate from difficulty assistance. |
| <a id="todo-214"></a> Scope index | Individual element specifications: [EL-075](#element-075), [TH-34](#thermal-34). | Retained research reference: [Contemporary Sierra article](https://www.sierragamers.com/wp-content/uploads/2019/12/022_InterAction_Volume_7_Number_2_Holiday_1994.pdf). Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

<a id="mechanical"></a>

### 4.3 Power conversion, transmission and moving cargo

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** After priority 0 load/energy accounting and typed ports.

**Gameplay outcome:** Players convert a wind/impact/shaft input into useful drive, select or reverse it, and move or carry cargo through visibly constrained motion. Stalls and power loss have understandable causes.

Finish fan supply and current mechanical gaps first. Build gearing/conversion and adapters before lifts, grippers and releasable assemblies. A shaft animation alone does not establish force, work or payload support.

<a id="todo-350"></a>

<a id="sequence-task-363"></a>

- [ ] Forward-refactor the self-contained fan to explicit external electrical/mechanical supply, updating scenes, authored levels, generator, callers and tests together. No hidden battery, compatibility mode or legacy fan alias. Current chime work must not be described as completing external fan power.

**Successful outcome:** An unconnected fan remains inactive; connecting a real electrical/mechanical source makes it blow and removing that source stops its driven effect. All current authored fan puzzles teach/provide the necessary supply.

<a id="todo-154"></a>

<a id="sequence-task-364"></a>

- [ ] Continue solar sources, chain-specific mechanisms, power lessons, repeated difficulty attempts, browser missing-wire/bypass controls and current-schema Save/Load verification.

**Successful outcome:** A player can distinguish a solar-fed or switched drive from an unpowered route, save/reload it and see any chain-specific mechanism obey its own implemented rule; historical belt proof does not complete chain mechanics.

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-187"></a> [ ] <a id="sequence-task-365"></a> P1 | Gears: coupled rotation; adjacent gears reverse direction | Add rotational ports and visible meshing. Begin with a direction-reversal puzzle; prevent contradictory drive loops from creating energy. Ratios need original-game verification. |
| <a id="todo-189"></a> [ ] <a id="sequence-task-366"></a> P1 | Generator: rotation to electricity | Couple a driven shaft to electrical output. Verify source loss stops dependent devices; teach motor/generator conversion without perpetual-power loops. |
| <a id="todo-188"></a> [ ] <a id="sequence-task-367"></a> P1 | Mouse motor: impact-started rotational drive | Add a distinct mechanical source, not a disguised electrical switch. Animate the wheel; author run duration and retrigger policy. |

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-211"></a> Scope index | Individual element specifications: [EL-053](#element-053), [EL-054](#element-054). | Retained research reference: [Inventory](https://the-incredible-machine.fandom.com/wiki/The_Incredible_Machine_2/Parts). Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="todo-423"></a> [ ] <a id="sequence-task-368"></a> P1 **Crank-slider** — expand crank adapter | Signed shaft input produces periodic linear strokes through visible fixed-length rod and offset crank pin. Authored radius, rod length and phase; preserve phase when stopped. | Motor → belt → crank feeds balls while clock meters arrivals. Test full revolution, reverse rotation, invalid geometry, restart without phase jump, collisions and cycle counts. |
| <a id="todo-424"></a> Scope index | Individual element specifications: [EL-156](#element-156), [EL-157](#element-157), [EL-158](#element-158). | Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |
| <a id="todo-425"></a> [ ] <a id="sequence-task-369"></a> P1 **Rack-and-pinion slide** | Signed shaft drives finite toothed rail with attachment carriage and end stops. Teeth/witness marks explain translation. Explicitly distinguish prescribed-speed prototype from load-aware final behaviour. | Solar → motor → rack positions mirror → receiver controls supplied gate. Test travel per turn, reversal, 3D rotation, missing connection, end stops and carried-object collisions. |

<a id="todo-375"></a>

**Scope index — individual element specifications:** [EL-055](#element-055), [EL-056](#element-056), [EL-057](#element-057), [EL-156](#element-156), [EL-157](#element-157), [EL-158](#element-158). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-370"></a>

- [ ] **Separate integration verification:** A cam profile controls a visible stroke, a crank converts rotation to reciprocation, a clutch/brake chooses drive or restraint, and a ratchet/escapement meters motion. Each named mechanism is separately implemented and taught. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-193"></a> Scope index | Individual element specifications: [EL-064](#element-064), [EL-065](#element-065), [EL-066](#element-066). | Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-210"></a> Scope index | Individual element specifications: [EL-067](#element-067), [EL-068](#element-068). | Retained research reference: [Manual](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf). Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-192"></a> Scope index | Individual element specifications: [EL-069](#element-069), [EL-070](#element-070). | Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

<a id="todo-320"></a>

**Scope index — individual element specifications:** [EL-058](#element-058), [EL-059](#element-059), [EL-060](#element-060), [EL-061](#element-061), [EL-062](#element-062), [EL-063](#element-063). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-371"></a>

- [ ] **Separate integration verification:** Sorting depends on observable size, mass or material; pickup/drop requires the declared power/material, and cargo can transfer only when the carrier is actually docked. Preserve separate obligations for every named sorter, magnet, brake, ratchet, carousel and carrier. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="todo-429"></a> [ ] <a id="sequence-task-372"></a> P2 **Docking lift** — expand lift entry | Guided supplied tray, upper/lower dock outputs and loading/unloading interlocks; explicit holding brake. Cargo stays physically supported through travel. | Ball waiting AND lower dock → inlet; upper dock → exit gate → gravity tube. Test edge cargo, acceleration, blocked travel, power loss and unloading between docks. |
| <a id="todo-430"></a> [ ] <a id="sequence-task-373"></a> P2 **Parallel gripper** | Supplied opposing jaws, bounded aperture, close command and object-held output. Capture requires actual two-sided contact; define supported shapes. This variant releases on power loss. | Lift docks → grasp non-ferrous ball → rack carries → limit/hold timer → drop into funnel. Test empty/oversize/off-centre closure, two-object ambiguity, obstruction, carrying collisions and unique ownership. |

<a id="todo-321"></a>

<a id="sequence-task-374"></a>

- [ ] Defer releasable assembly joints/temporary bridges until compound-body and connection lifecycle behaviour is reliable. Do not add unrestricted teleporters merely to expand the palette.

**Successful outcome:** After shared joint lifecycle is reliable, a player can release a supported temporary assembly or bridge and see the resulting physical motion; Reset restores its authored attachments.

<a id="launch"></a>

### 4.4 Rebound, capture and charged delivery

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** After shared contacts, finite stored energy and physical cargo ownership.

**Gameplay outcome:** Players choose a timed punch, a rebound, a catch or a charged throw because each solves a different delivery problem. Payloads remain physical and cannot receive a free recharge.

Reuse charge/hinge/rope infrastructure, but prove each distinct launcher separately. The completed cannon and wound launcher do not complete the other historical/projectile variants.

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="todo-410"></a> [ ] <a id="sequence-task-375"></a> P2 **Supplied pinball flipper** — promote existing candidate | Battery supplies bounded pivoted paddle; independent signal commands strike/hold, spring returns it. Distinct from radial bumper and linear boxing glove. | Beam receiver → timer → paddle releases or strikes a waiting ball into funnel. Test missing supply, power loss during hold, blocked paddle, hinge/tip hits, early/late timing and sustained commands. |

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-194"></a> [ ] <a id="sequence-task-376"></a> P2 | Boxing glove: triggered punch | Add a directional actuator with explicit contact face and cooldown. Teach timing a lateral impulse rather than continuous force. |

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="todo-411"></a> Scope index · P2 | [Damped cushion](#element-215) and [capture cradle](#element-216) are individually specified. | Separate integration proof: deliver cargo onto a moving supported catcher, then release through a separately taught gate; verify grazing, bottoming, overflow, load mass and exact Reset through generic contact/containment. |
| <a id="todo-412"></a> [ ] <a id="sequence-task-377"></a> P2 **Passive impact scoop** | Arriving ball's momentum rotates a shallow hinged cup and carries the same ball upward before release. No raised counterweight or automatic boost; distinct from the counterweighted launcher above. | Downhill run-up → scoop → elevated pipe → cushion. Test minimum approach energy, wrong-side/oversize/off-centre entry, stalled rotation, release geometry and simultaneous arrivals; no teleport between mouths. |
| <a id="todo-395"></a> [ ] <a id="sequence-task-378"></a> P2 **Counterweighted scoop catapult** | Raised physical weight drives a loaded hinged cup; same ball leaves on a free trajectory. Rope/winch must raise the weight again. Show falling weight, swept arm and spent state. | Water-filled bucket → armed cup → upper pipe. Test inadequate counterweight, missed cup, premature release, blocked arm, payload retention/release and work spent each cycle. |
| <a id="todo-396"></a> [ ] <a id="sequence-task-379"></a> P2 **Latched slingshot cradle** | Rope/winch draws an elastic cradle; separate latch releases its seated ball. No reflex aiming during Run. Visible band extension and return; bounded authored charge. | Motor/winch charges; sound meter releases through a timed gate. Test zero charge, rope cut, draw limit, oversized load, empty release and no free recharge. |
| <a id="todo-397"></a> [ ] <a id="sequence-task-380"></a> P2 **Hinged elastic catapult** | Distinct rotary delivery from the linear wound launcher: charged elastic arm swings a cup before releasing. Share finite-charge infrastructure, not an identical impulse with another mesh. | Belt winding → delayed latch → throw into moving basket. Test unwound release, blocked sweep, maximum charge, release angle and one-cycle depletion. |

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-195"></a> [ ] <a id="sequence-task-381"></a> P2 | Jack-in-the-box: belt-wound launcher | Accumulate mechanical input before release. Show winding, lid opening and launch; verify no free launch without a drive connection. |

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="todo-399"></a> [ ] <a id="sequence-task-382"></a> P2 **Magnetic impulse relay** | Compatible arriving ball is retained while a different staged ball departs. Explicit finite stored energy and physical rearming; distinct from electromagnet pickup. Visible input/output occupancy and spent state. | Outgoing ball climbs pipe to switch; retained ball weights a tray. Test wrong material/side, missing staged ball, rapid second arrival, payload identity and no energy-gaining relay loop. |
| <a id="todo-400"></a> [ ] <a id="sequence-task-383"></a> P2 **Zipline signal carriage** — expand docking transport | Incoming ball releases a gravity-driven cable carriage that strikes a separate destination ball; it does not teleport or carry the initiating ball. Winch returns it. | Ball → carriage across ravine → destination ball → bell. Test flat/uphill stall, obstructions, empty/blocked destination, premature retrigger and return work. |

<a id="new-domains"></a>

## Priority 5 — expand water, air, light and sound puzzles

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

<a id="water"></a>

### 5.1 Conserved water, changing loads and useful flow

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** After finite-volume, load and energy foundations; extend within the 150-level teaching budget.

**Gameplay outcome:** Players pour a finite supply, route it downhill, turn a wheel and change a counterweight. Spilled or discharged water remains accounted for, and reusing it creates an intentional puzzle decision.

Implement reservoir/tap/catch/drain/gutter and pipe variants separately, then wheel/load/float controls, then pumps, siphons, locks and hydraulics. Each named variant in an umbrella remains a separate delivery obligation; the linked research supplies their individual contracts.

<a id="todo-334"></a>

**Scope index — individual element specifications:** [EL-001](#element-001), [EL-002](#element-002), [EL-003](#element-003), [EL-004](#element-004), [EL-005](#element-005), [EL-006](#element-006), [EL-007](#element-007). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-384"></a>

- [ ] **Separate integration verification:** A tank empties through the chosen tap/gutter into a catch basin, with visible remaining capacity, overflow and drain loss. Each reservoir, tap, collector, drain and gutter has its own catalogue contract and proof. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="todo-335"></a>

**Scope index — individual element specifications:** [EL-008](#element-008), [EL-009](#element-009), [EL-010](#element-010), [EL-011](#element-011), [EL-012](#element-012), [EL-013](#element-013). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-385"></a>

- [ ] **Separate integration verification:** Straight sections, both elbow angles, T junctions, caps and nozzles join by compatible water ports; splitting divides the available flow and a capped route cannot leak through an invisible connection. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="todo-336"></a>

<a id="sequence-task-386"></a>

- [ ] **P1:** waterwheel with signed mechanical output to belts/conveyors and reusable discharge. Refactor load/energy budgeting before claiming wheel/pump loops conserve energy.

**Successful outcome:** A stream turns the wheel in the visible direction and powers a connected mechanism; stopping the flow stops new energy input and its discharge can be collected below.

<a id="todo-337"></a>

**Scope index — individual element specifications:** [EL-014](#element-014), [EL-015](#element-015), [EL-016](#element-016), [EL-017](#element-017), [EL-018](#element-018). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-387"></a>

- [ ] **Separate integration verification:** A filled rope bucket changes a lift's balance, tipping spills real contents, and a leaking bucket supplies another receiver. Float, mechanical valve and supplied level-switch variants are each selectable and separately verified. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-212"></a> [ ] <a id="sequence-task-388"></a> P1 | Leaky bucket loses mass over time. [Manual](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf) | Extend moving containers with conserved water outflow, changing load and visible fill level; catch leaks in other containers. Teach delayed counterbalance. See expanded water research; countdown-only mass loss is no longer the proposed contract. |

<a id="todo-338"></a>

**Scope index — individual element specifications:** [EL-019](#element-019), [EL-020](#element-020), [EL-021](#element-021), [EL-022](#element-022), [EL-023](#element-023), [EL-024](#element-024), [EL-025](#element-025). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-389"></a>

- [ ] **Separate integration verification:** Each new flow component creates its stated decision: prevent backflow, choose/gate a route, lift water with power, maintain/break a primed siphon, or count real tipping batches. Do not substitute one generic timed valve for all variants. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="todo-339"></a>

**Scope index — individual element specifications:** [EL-026](#element-026), [EL-027](#element-027), [EL-028](#element-028), [EL-029](#element-029), [EL-030](#element-030), [EL-031](#element-031), [EL-032](#element-032). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-390"></a>

- [ ] **Separate integration verification:** Connected tank levels, sluice-controlled floating transport, a loaded hydraulic stroke and flow/volume/pressure measurements react to the actual fluid state; each device/measurement remains a distinct item. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="todo-428"></a> [ ] <a id="sequence-task-391"></a> P2 **Hydraulic piston** — expand existing water entry | Finite liquid displacement, return path, pressure/head and resisting load; visible rod and tank levels. Not a duplicate pneumatic cylinder with different colour. | Elevated tank lifts loaded tray; return water drives wheel. Test conservation, insufficient head, sealed return, overflow, reverse loading and no perpetual wheel/pump loop. |

<a id="todo-341"></a>

<a id="sequence-task-392"></a>

- [ ] Teach **First Pour** (tank/tap/gutter/bucket), **Run the Mill** (wheel/belt/conveyor), **Borrowed Weight** (water/rope counterweight) and **Every Drop Counts** (reuse discharge to lift a float).

<a id="pneumatic"></a>

### 5.2 Stored air and powered strokes

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** After the bellows, sealed-supply model and loaded actuators.

**Gameplay outcome:** Players compress and store air, release it through a valve and see a piston push or return according to its chamber connections. Empty storage cannot power another stroke.

Distinguish open fan airflow, a finite compressed-air network and water hydraulics. Keep the spring-return and double-acting pistons separately testable; the electromagnet/material-sorting obligation in the umbrella also remains tracked under mechanical handling.

<a id="todo-376"></a>

**Scope index — individual element specifications:** [EL-037](#element-037), [EL-038](#element-038), [EL-039](#element-039), [EL-040](#element-040), [EL-041](#element-041), [EL-042](#element-042), [EL-043](#element-043), [EL-060](#element-060). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-393"></a>

- [ ] **Separate integration verification:** The player stores a finite bellows charge in a reservoir, chooses a valve/air-hose route and drives the appropriate piston; a magnet sorts only declared compatible material. These are distinct components, not one family-level completion. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

| Task | Distinct behaviour and visible state | Puzzle combination and focused acceptance |
| --- | --- | --- |
| <a id="todo-426"></a> [ ] <a id="sequence-task-394"></a> P2 **Spring-return pneumatic piston** — expand pneumatic entry | Finite compressed-air supply and valve extend rod; visible spring returns it on exhaust. Show stroke and reservoir state. Do not equate pressure with an electrical on/off flag. | Bellows/reservoir → valve → piston diverts a ball; exhaust → whistle. Test inadequate pressure, blocked stroke, repeated depletion, venting and hose disconnection. |
| <a id="todo-427"></a> [ ] <a id="sequence-task-395"></a> P2 **Double-acting pneumatic piston** | Two typed chamber ports and directional valve drive extension and retraction; distinct from spring return. Account for supply and exhaust in both directions. | Clock/logic alternates a physical sorting pusher between two chutes. Test both chambers pressurised, valve transitions, leaks/depletion, blocked load and mid-stroke reversal. |

<a id="optics"></a>

### 5.3 Broader light sensing and finite-width optics

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** Refresh existing optical behaviour during priority 0; new finite-width parts follow the core routing/control families.

**Gameplay outcome:** Players predict which receiver will light from real beam paths, channel content and available power, then use focus, spread or separation to solve distinct puzzles.

Teach already working mirrors/splitters/filters in priority 3. Implement finite-width coverage before lenses or heat targets, and preserve energy/range bounds. Existing historical light/ignition umbrellas retain all unimplemented sources and targets.

<a id="todo-138"></a>

<a id="sequence-task-396"></a>

- [ ] Extend cone motion/partial-shadow review, multiple-source performance checks and browser Save/Load. Flashlight cones do not reflect or refract; laser mirror reflection is implemented separately. Charge storage and TIM-calibrated radiometry remain pending.

<a id="todo-289"></a>

<a id="sequence-task-397"></a>

- [ ] Extend cone visual checks to moving partial occluders from multiple camera angles and several simultaneous sources. Sampled silhouette clipping is approximate, not full volumetric scattering; measure frame performance and refine visible stepping if needed.

<a id="todo-291"></a>

**Scope index — individual element specifications:** [EL-153](#element-153), [EL-154](#element-154). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-398"></a>

- [ ] **Separate integration verification:** The intended receiver responds to the documented source/channel and, for a charging variant, the actual illumination duration. Existing laser-only reception is not silently treated as flashlight reception. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="todo-309"></a>

<a id="sequence-task-399"></a>

- [ ] Track linear RGB power, beam width, finite range and bounded branch/bounce counts. Passive optics cannot amplify power; stop cycles deterministically and never count circulating light repeatedly as fresh energy. Define finite-width coverage before claiming useful lens physics.

**Successful outcome:** Splitting, focusing and repeated reflections cannot create extra power. A player can see where light spreads or loses useful strength and why a receiver remains below threshold.

<a id="todo-310"></a>

<a id="sequence-task-400"></a>

- [ ] Snapshot geometry/control once per fixed tick; trace emitters, aggregate detector contributions, then commit outputs together. Changes caused by those outputs affect the next optical tick, preventing entity-order dependence and recursive same-tick feedback.

**Successful outcome:** Two simultaneous beam changes produce the same receiver results regardless of part insertion order, with control consequences applied on the documented optical tick.

<a id="todo-311"></a>

<a id="sequence-task-401"></a>

- [ ] Reset restores transforms, shutter pose, emitter settings and electrical/charge/heat state; clear traced beams and recompute previews. Test occluded receivers, wrong colours, insufficient split power, loops, moving blockers and power interruption.

**Successful outcome:** Reset clears heat/charge and restores every optical pose, shutter and emitter setting; the next Run reproduces the same beam routing and interlock behaviour.

<a id="todo-304"></a>

**Scope index — individual element specifications:** [TH-06](#thermal-06), [TH-37](#thermal-37). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-402"></a>

- [ ] **Separate integration verification:** Moving a converging lens places a finite bright region onto the target; a correct focus can actuate the heat-sensitive latch while an unfocused/occluded beam cannot gain free energy. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="todo-305"></a>

<a id="sequence-task-403"></a>

- [ ] Later: **diverging lens**, spreading limited power over a wider footprint. Example: cover two low-threshold detectors, neither receiving the full original power. Implement with finite-width beam coverage and partial occlusion.

**Successful outcome:** A diverging lens trades intensity for coverage: two low-threshold targets may respond, but neither receives the original full beam power.

<a id="todo-306"></a>

<a id="sequence-task-404"></a>

- [ ] Later: **prism**, separating components present in white light into RGB paths using an explicit simplified spectral rule. A pure red beam must not create green or blue energy.

**Successful outcome:** A prism creates only the RGB paths present in its incoming beam; pure red produces no green/blue output and each path can be deliberately blocked.

<a id="todo-307"></a>

<a id="sequence-task-405"></a>

- [ ] Optional: **fibre/light pipe**, carrying light between explicit optical endpoints with coupling limits and loss. Keep separate from ball tubes and restrict its inventory where unrestricted routing would bypass the mirror puzzle.

**Successful outcome:** An optical pipe connects explicit compatible endpoints with bounded coupling/loss, enabling a constrained route without erasing the intended mirror-placement challenge.

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-196"></a> Scope index | Individual element specifications: [EL-155](#element-155), [EL-210](#element-210), [TH-01](#thermal-01), [TH-06](#thermal-06), [TH-03](#thermal-03). | Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-215"></a> Scope index · P2 | Individual optical contracts: [red emitter](#element-176), [green emitter](#element-177), [blue emitter](#element-178), [combiner](#element-213), [mirror](#element-212), [broadband detector](#element-214), and separate [selective receiver records](#element-146). | [Historical manual](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf); prove channel transport/absorption through the generic process register, not a combined part. |

<a id="sound"></a>

### 5.4 Reliable acoustic signals and cross-system machines

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** Existing-part audio checks accompany priority 3; new acoustic routing follows its dependencies.

**Gameplay outcome:** Players can read an acoustic trigger from the object and meter even with audio muted, then deliberately route or discriminate pulses and tones.

Core outcomes must be identical across audio suspension, mute and camera changes. Water-tuned sound and cross-system recipes wait for the corresponding water/mechanical parts; each horn, duct, resonator and tone-selective meter needs its own proof.

<a id="todo-353"></a>

<a id="sequence-task-406"></a>

- [ ] Fixed-tick typed acoustic events own strength, tone, direction, travel, occlusion and bounded propagation. Audio playback, mute, camera and browser audio suspension cannot change outcomes; no microphone requirement.

**Successful outcome:** Audio permission, muting and camera movement never change a win/fail outcome; real simulated pulses, propagation and occlusion own acoustic logic.

<a id="todo-352"></a>

**Scope index — individual element specifications:** [EL-044](#element-044), [EL-045](#element-045), [EL-046](#element-046), [EL-047](#element-047), [EL-048](#element-048), [EL-049](#element-049), [EL-050](#element-050), [EL-051](#element-051), [EL-052](#element-052). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-407"></a>

- [ ] **Separate integration verification:** A tuned meter distinguishes the intended tone; a whistle needs airflow; connected horn/ducts carry bounded sound; a resonator visibly builds/loses excitation. Screens/dishes and water-tuned bottles each retain their later individual contract. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="todo-378"></a>

<a id="sequence-task-408"></a>

- [ ] Combine **Clockwork Rain** (wheel/cam/metred drops), **One Breath** (stored air/piston/whistle), **Sorting Office** (magnet/chute) and **The Mill's Song** (wheel/cam/chimes/sound-controlled sluice).

<a id="radiation"></a>

### 5.5 Potential radiation elements and 150-level campaign research

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** Research proposal added 28 September 2026; implementation follows shared physics and the relevant source, transport, control, water or thermal dependencies. P1/P2/P3 below rank candidates within this family, not above the current project priorities.

**Gameplay outcome:** Players control penetration, instantaneous radiation rate and accumulated exposure, inspect materials, convert radiation to visible signals and combine these with transport, water, timing and mechanical work.

**Status and scope:** These are **22 potential elements**, not implemented or verified features. The user's requested planning horizon is **150 puzzles**; existing 75-level content and evidence remain intact. Inclusion in this candidate section does not itself select every option for implementation. Adopted candidates must pass the existing per-element completion gate and be committed/pushed individually. Existing required component scope remains unchanged. Detailed physical sources, deliberate simulation limits, typed contracts, per-mode proof requirements and teaching plans are in [radiation component research](../radiation-component-research.md).

<a id="radiation-01"></a>

<a id="sequence-task-409"></a>

- [ ] **RAD-01 · P1 potential: Gamma source capsule.** A fixed-strength sealed toy source emits a spatial field continuously during Run; players control exposure by moving it or placing shielding. No electrical off switch for radioactive decay. Proposed campaign teaching: 101, 103, 110.

**Successful outcome:** Moving the capsule farther away lowers the meter reading; disconnecting a nearby battery does not stop emission. Source pose and initial state restore exactly.

<a id="radiation-02"></a>

<a id="sequence-task-410"></a>

- [ ] **RAD-02 · P1 potential: Powered X-ray emitter.** Supply and enable input produce a bounded directional photon field. This is the controllable source alternative; X-ray and gamma penetration depends on the authored energy band, not a universal type ranking. Proposed campaign teaching: 107, 114.

**Successful outcome:** Supply removal stops emission; a disconnected enable input cannot fire. Test every supported energy preset, direction and restart.

<a id="radiation-03"></a>

<a id="sequence-task-411"></a>

- [ ] **RAD-03 · P2 potential: Alpha source cartridge.** A short-range positively charged particle source has an explicitly modeled emission window. Teach near-field detection and stopping before field steering; its enclosing model must not block its own outlet. Proposed campaign teaching: 115, 118.

**Successful outcome:** A close compatible detector responds; extra separation or a thin screen suppresses it. Gamma-only reception rejects it.

<a id="radiation-04"></a>

<a id="sequence-task-412"></a>

- [ ] **RAD-04 · P2 potential: Beta-minus source cartridge.** A negatively charged particle source provides a different range/material response and, later, opposite magnetic curvature. Keep it a distinct inventory item from alpha. Proposed campaign teaching: 116, 119.

**Successful outcome:** A taught thin screen passes enough beta to detect while a polymer screen suppresses it; opposite bending is proven with the deflector.

<a id="radiation-05"></a>

**Scope index — individual element specifications:** [EL-126](#element-126), [EL-127](#element-127), [EL-128](#element-128). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-413"></a>

- [ ] **Separate integration verification:** Equal geometry with different materials produces the declared readings; stacked thickness attenuates monotonically and gaps transmit. Repositioned panels restore. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="radiation-06"></a>

<a id="sequence-task-414"></a>

- [ ] **RAD-06 · P1 potential: Powered radiation shutter.** A supplied actuator moves a dense shielding blade through the field. Radiation follows actual blade position and thickness, including partial travel and jams. Proposed campaign teaching: 105, 110.

**Successful outcome:** Closed blade reduces exposure below the taught target threshold; an obstructed blade leaks according to geometry. Power loss cannot teleport the blade closed.

<a id="radiation-07"></a>

<a id="sequence-task-415"></a>

- [ ] **RAD-07 · P1 potential: Collimator block.** A dense block with a real aperture selects field directions by absorbing off-axis radiation. Aperture presets trade coverage for throughput; this is not a lens or amplifier. Proposed campaign teaching: 106, 114.

**Successful outcome:** An aligned receiver responds while an off-axis control does not; a narrower opening never increases total transmitted power.

<a id="radiation-08"></a>

<a id="sequence-task-416"></a>

- [ ] **RAD-08 · P1 potential: Radiation rate meter.** A supplied, labeled detector reports instantaneous response with typed radiation/energy sensitivity and separate on/off thresholds. Contact output controls existing electrical devices. Proposed campaign teaching: 101, 108, 115, 116.

**Successful outcome:** Correct exposure switches the output; wrong channel, blocked path and missing supply do not. Muting clicks leaves results identical.

<a id="radiation-09"></a>

<a id="sequence-task-417"></a>

- [ ] **RAD-09 · P1 potential: Integrating dosimeter.** A supplied instrument accumulates weighted exposure across separate bursts; power loss disables its contact but retains accumulated measurement within a Run. Keep this distinct from a continuous hold timer. Proposed campaign teaching: 109, 110.

**Successful outcome:** Two separated exposures add to the same total as one equivalent exposure; darkness does not erase the total. Run/Reset clears it to the authored initial value.

<a id="radiation-10"></a>

<a id="sequence-task-418"></a>

- [ ] **RAD-10 · P1 potential: Exposure-sensitive cargo badge.** A passive badge attached to a dedicated movable cargo piece accumulates exposure along its actual trajectory. A delivery goal requires arrival inside a taught exposure window. Proposed campaign teaching: 111, 112.

**Successful outcome:** A shielded route delivers within budget; the same endpoint reached by an exposed route fails. Cargo retains its dose through stops and loses it only on Reset.

<a id="radiation-11"></a>

**Scope index — individual element specifications:** [EL-129](#element-129), [EL-130](#element-130). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-419"></a>

- [ ] **Separate integration verification:** Known thin/thick samples sort correctly; a rising conserved waterline changes the level contact. Missing source is invalid, not a valid heavy/full reading. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="radiation-12"></a>

<a id="sequence-task-420"></a>

- [ ] **RAD-12 · P2 potential: Scintillator tile.** Absorbed ionizing radiation becomes a bounded amount of visible light at a declared optical aperture. A separate compatible supplied optical receiver can control machinery. Proposed campaign teaching: 117, 122.

**Successful outcome:** Incident radiation produces observable light and a receiver response; shielding prevents both. Conversion loss is explicit and output cannot exceed absorbed energy.

<a id="radiation-13"></a>

<a id="sequence-task-421"></a>

- [ ] **RAD-13 · P2 potential: Decay clock capsule.** A visibly marked fictional short-half-life source weakens according to a deterministic exponential envelope. Use authored half-life presets, not random waits or a renamed linear timer. Proposed campaign teaching: 123, 124.

**Successful outcome:** Successive equal half-life intervals halve source strength; a rate contact releases below its threshold, and Reset restores the initial activity and age.

<a id="radiation-14"></a>

<a id="sequence-task-422"></a>

- [ ] **RAD-14 · P3 potential: Magnetic deflector.** A supplied field chamber with reversible polarity bends charged particle trajectories; alpha and beta-minus require distinct curvature parameters. Ordinary mirrors cannot redirect these channels. Proposed campaign teaching: 118, 119.

**Successful outcome:** Polarity reversal swaps the charged route; gamma/X-ray controls remain straight. Field-off and opposite-charge cases are separately proven.

<a id="radiation-15"></a>

<a id="sequence-task-423"></a>

- [ ] **RAD-15 · P3 potential: Track chamber.** A readable detector volume displays persistent short trails from the simulated charged trajectories; an explicitly supplied segmented readout can select an exit region. Proposed campaign teaching: 120, 121.

**Successful outcome:** The selected exit segment triggers only for a traversing track; a decorative trail or a miss cannot trigger it. Neutral photons make no direct charged track in the initial abstraction.

<a id="radiation-16"></a>

<a id="sequence-task-424"></a>

- [ ] **RAD-16 · P3 potential: Neutron source module.** A distinct contained toy source emits a declared fast-neutron group. Treat it as a specialist transport domain with no implied reactor, multiplication or gamma shielding equivalence. Proposed campaign teaching: 125, 130.

**Successful outcome:** A fast-sensitive neutron detector responds; a photon-only detector does not. Ordinary magnetic deflection leaves the neutron route unchanged.

<a id="radiation-17"></a>

<a id="sequence-task-425"></a>

- [ ] **RAD-17 · P3 potential: Water moderator tank.** A conserved-water vessel changes a bounded fraction of traversing fast-neutron flux into a slower group with documented losses. Water level affects the interaction path. Proposed campaign teaching: 126, 128.

**Successful outcome:** Filling the tank increases the slow-group response in the authored range; a dry tank and a drained tank fail that control. Slowdown is not absorption.

<a id="radiation-18"></a>

<a id="sequence-task-426"></a>

- [ ] **RAD-18 · P3 potential: Neutron absorber panel.** A distinct boron-inspired material removes a declared fraction of slow-neutron flux. Teach absorption separately from moderation; any omitted capture products are an explicit model limit. Proposed campaign teaching: 127, 130.

**Successful outcome:** Adding it after the moderator suppresses the slow detector; removing it restores response. Dense photon shielding does not substitute for its neutron contract.

<a id="radiation-19"></a>

**Scope index — individual element specifications:** [EL-131](#element-131), [EL-132](#element-132). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-427"></a>

- [ ] **Separate integration verification:** Moderated flow drives Slow while the documented Fast reading decreases; a photon source and missing supply are negative controls. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="radiation-20"></a>

<a id="sequence-task-428"></a>

- [ ] **RAD-20 · P3 potential: Radioisotope thermoelectric generator.** A contained decay-heat module uses a real hot/cold temperature difference to supply bounded electrical power. Cooling and load create the puzzle; it must not be a battery with decorative fins. Proposed campaign teaching: 131, 132.

**Successful outcome:** Cooling permits useful output; an equal-temperature control produces no thermoelectric output. Heat, electrical work, load and Reset state are accounted for.

<a id="radiation-21"></a>

<a id="sequence-task-429"></a>

- [ ] **RAD-21 · P3 potential: Radiation-responsive material latch.** A fictional polymer insert weakens after accumulated exposure and releases a real loaded latch. The particular threshold-to-release behavior is authored material fiction inspired by radiation processing. Proposed campaign teaching: 133, 134.

**Successful outcome:** Enough exposure releases the load; shielding or insufficient accumulated exposure retains it. Reset restores material state, latch pose and stored load.

<a id="radiation-22"></a>

<a id="sequence-task-430"></a>

- [ ] **RAD-22 · P2 potential: Sealed tracer capsule.** A marked mobile radioactive capsule travels through existing ball-compatible transport and is sensed through its enclosure. This is discrete particle tracking, not dissolved radioactive fluid. Proposed campaign teaching: 135, 136.

**Successful outcome:** Two detectors observe the capsule in causal order and drive a gate/counter; a stationary capsule cannot repeatedly count as new arrivals. Branches move one capsule without copying it.

<a id="radiation-foundations"></a>

<a id="sequence-task-431"></a>

- [ ] Define the shared radiation transport and typed boundary contracts before adopting dependent candidates: distinct radiation identity/energy groups/materials/instrument states, geometry-based transmission, finite aperture coverage, deterministic multi-source aggregation and no unexplained free energy. Reject unsupported modes/connections explicitly; keep enums through callers, collections, authored content, UI adapters and tests.

**Successful outcome:** One documented model explains source → material → receiver for every supported channel; unsupported channels do not silently fall back to light tracing, strings or a default material. Canonical boundary and rejection tests compile with all affected callers.

<a id="radiation-proof"></a>

<a id="sequence-task-432"></a>

- [ ] For every adopted RAD element and each supported preset/mode, retain its own current real-UI Playwright construction, positive/control behavior, integration/typed connections, exact Run/Reset and current-schema save/load proof, production build, logs, revision/content identity, screenshots/motion captures and failed attempts. Track implementation, native checks, UI evidence, commit and push separately.

**Successful outcome:** No element is marked complete from a family demonstration, a meter reading without configuration proof or a reference solution. The repository-wide enum audit remains open until independently verified.

<a id="radiation-campaign"></a>

<a id="sequence-task-433"></a>

- [ ] Integrate all adopted radiation candidates into the [current 150-level campaign plan](#campaign-plan): 101–110 measurement/shielding, 111–120 inspection/charged routing, 121–130 decay/neutrons, 131–135 thermal/material/tracer lessons, then 136–150 cross-family synthesis. Use the [research recipes](../radiation-component-research.md#concrete-radiation-lessons-slots-76110) as design inputs; their preliminary slots 76–110 move to 101–135. Map every supported mode to an introduction, control and later combination; do not give radiation the other families' unfilled teaching slots.

**Successful outcome:** All 150 positions have an explicit teaching purpose, every required mechanism is introduced before use, and unselected/unfinished candidates remain visible. Neither old evidence nor passing reference solutions are misreported as completion of the enlarged campaign.

<a id="radiation-campaign-evidence"></a>

<a id="sequence-task-434"></a>

- [ ] When the 150-level content is authored, forward-update current campaign tooling, typed level identities, navigation and save boundaries together, without aliases or automatic migration. Preserve historical 75-level/900-cell artifacts unchanged. Plan **150 × 3 difficulties × 4 placement variants = 1,800 distinct baseline cases**, plus repeats and mechanism probes, after component coverage.

**Successful outcome:** The enlarged campaign has content-versioned evidence and no silent credit from obsolete levels; focused per-element browser, Reset/save and build proof proceeds during implementation rather than waiting for the exhaustive matrix.


<a id="thermal-elements"></a>

### 5.6 Heating, cooling and phase-change puzzle elements

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** After the relevant generic energy/material/transport capabilities; deliver small lessons as each part is proven. Integrate electricity, mechanics, optics, fluids, gases and radiation through the [generic interaction contract](#generic-interaction-contract).

**Gameplay outcome:** Players route, store, concentrate, supply or remove energy to change material state and power useful machines. Heating, cooling and phase transitions remain observable, conserved and reusable across arbitrary compatible assemblies.

**Research:** [Thermal component research](../thermal-component-research.md) records the source keys below and physical limits. These are original design proposals, not copied game implementations. Existing candle/lens/brake/ice/kettle/thermostat/steam umbrellas map to canonical entries here rather than duplicate catalogue identities. RTG assemblies reuse the same generic thermal converter with a decay-energy source.

**Format and completion:** Each TH record is one independently described placeable element. Separate IX processes and TX interaction scenarios follow; they are independent TODO requirements, not catalogue pieces or hard-coded pair handlers. Each supported mode/material preset receives its own proof before closure. All entries are planned, unchecked and unverified. Apply individual native/build and actual-UI positive/control/typed-connection/Run–Reset/save evidence, retained failures and individual verified commit/push requirements.

**Visual contract:** Follow [DESIGN.md](../../DESIGN.md#art-direction--monument-valley-inspired): original sculptural primitive forms, matte surfaces, soft light, gentle shadows, quiet purposeful motion and contextual icons. Preserve approved palette values. Show temperature and phase through shaped gauges, markings and visible contents, never colour alone. Flame/mist stays restrained and cannot stand in for proof: water vapor is not the white condensed-droplet plume. Keep collision, deformation, phase fraction and artwork synchronized; review desktop/mobile build/run and motion.

<a id="thermal-01"></a>

<a id="sequence-task-435"></a>

- [ ] **TH-01 · Potential element: Candle.** Finite wax inventory feeds a shared combustion model and emits accounted heat/light; heat and oxygen conditions determine burning. Research basis: [TH-S03](../thermal-component-research.md#th-s03).

**Visual style:** Cream wax cylinder with a recessed wick and restrained, state-driven flame.

**Successful outcome:** An unlit or exhausted candle produces no sustained output. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **56**; practice **57**; combination/reuse **83, 137**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-02"></a>

<a id="sequence-task-436"></a>

- [ ] **TH-02 · Potential element: Fire bowl.** A vessel contains combustible material without being an intrinsic heat source; reaction belongs to its contents and surrounding gas. Research basis: [TH-S03](../thermal-component-research.md#th-s03).

**Visual style:** Shallow cream stone bowl, visible fuel bed and sparse navy rim marks.

**Successful outcome:** An empty bowl or inadequate oxidizer cannot sustain flame. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **57**; practice **58**; combination/reuse **85, 140**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-03"></a>

<a id="sequence-task-437"></a>

- [ ] **TH-03 · Potential element: Combustible block.** Transportable structural material has finite reactants, chemical energy and temperature-dependent strength; consumption updates mass and geometry generically. Research basis: [TH-S03](../thermal-component-research.md#th-s03).

**Visual style:** Warm-wood chamfered block, engraved fuel symbol and readable consumed volume.

**Successful outcome:** A noncombustible control absorbs heat but does not burn; spent fuel cannot reignite indefinitely. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **58**; practice **59**; combination/reuse **85, 149**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-04"></a>

<a id="sequence-task-438"></a>

- [ ] **TH-04 · Potential element: Electrical heating plate.** A finite electrical load converts supplied work to enthalpy in its contacting surface; any thermal body may receive that heat. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Cream plinth, gold inset disc and navy heat witness marks.

**Successful outcome:** Disconnected or exhausted supply cannot create new heat. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **14**; practice **15**; combination/reuse **55, 70, 147**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-05"></a>

<a id="sequence-task-439"></a>

- [ ] **TH-05 · Potential element: Friction brake.** Extend the existing brake's generic resistive-contact/shaft model: removed mechanical work enters heat stores, with a visible mechanical cost. This is one brake identity with an added thermal proof. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Gold rotor, cream shoe and a small shaped thermal indicator.

**Successful outcome:** A stationary brake makes no frictional heat; shaft energy loss balances thermal gain and declared losses. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **39**; practice **40**; combination/reuse **81, 147**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-06"></a>

<a id="sequence-task-440"></a>

- [ ] **TH-06 · Potential element: Converging lens.** Refine the existing lens identity: finite-width refraction concentrates irradiance on real surfaces without multiplying incident power. Heating remains absorption in the receiving material. Research basis: [TH-S04](../thermal-component-research.md#th-s04).

**Visual style:** Translucent cyan disc in a cream sculptural frame; faint selected-only finite focus footprint.

**Successful outcome:** Defocus, occlusion or inadequate source power fails to reach ignition conditions. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **59**; practice **60**; combination/reuse **85, 142**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-07"></a>

<a id="sequence-task-441"></a>

- [ ] **TH-07 · Potential element: Solar absorber plate.** Absorbed optical flux becomes internal energy according to surface absorptance and illuminated area; a thermal port can carry it onward. Research basis: [TH-S04](../thermal-component-research.md#th-s04).

**Visual style:** Matte navy inset on a cream slab, gold rim and raised temperature marks.

**Successful outcome:** A reflective comparison absorbs less; shading removes the input without immediately deleting stored heat. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **55**; practice **56**; combination/reuse **69, 131**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-08"></a>

<a id="sequence-task-442"></a>

- [ ] **TH-08 · Potential element: Heat-conducting bar.** Route heat using material conductivity, length, cross-section and actual thermal contacts; neither endpoint names a permitted counterpart. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Broad gold bridge with cream insulated grips and engraved contact ends.

**Successful outcome:** An air gap removes solid contact conduction; a low-conductivity comparison transfers less under the same conditions. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **16**; practice **17**; combination/reuse **55, 82, 145**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-09"></a>

<a id="sequence-task-443"></a>

- [ ] **TH-09 · Potential element: Insulating panel.** Thickness and material properties resist heat transfer while retaining explicitly supported radiative/convective paths. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Layered cream ceramic slab with a visible navy section pattern.

**Successful outcome:** A thinner/lower-resistance comparison leaks heat faster; insulation is not a universal heat deletion field. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **19**; practice **20**; combination/reuse **67, 84, 139**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-10"></a>

<a id="sequence-task-444"></a>

- [ ] **TH-10 · Potential element: Finned heat sink.** Generic exposed area and surface/environment exchange reject heat toward an explicitly modeled ambient reservoir. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Ordered cream architectural fins and a gold contact foot; no industrial clutter.

**Successful outcome:** An equal-temperature environment gives no net cooling; passive convection cannot cool below ambient. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **40**; practice **41**; combination/reuse **78, 87, 145**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-11"></a>

<a id="sequence-task-445"></a>

- [ ] **TH-11 · Potential element: Heat exchanger.** Two separated fluid passages exchange energy through a conductive partition; material remains in its own passage. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Twin cyan windowed passages in a cream block with distinct embossed route symbols.

**Successful outcome:** Equal-temperature streams give no net exchange; blocked flow limits delivery and cannot cross-contaminate channels. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **69**; practice **70**; combination/reuse **87, 137**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-12"></a>

<a id="sequence-task-446"></a>

- [ ] **TH-12 · Potential element: Reversible heat pump.** Supplied work moves heat between two thermal ports with an explicit constitutive model and operating range; hot-side rejection equals removed heat plus input work. Teach reverse operation separately. Research basis: [TH-S05](../thermal-component-research.md#th-s05).

**Visual style:** Opposed cream/gold faces with hot/cold shape glyphs and a cyan supply core.

**Successful outcome:** Power loss stops pumping; an insulated hot side warms and limits cooling rather than swallowing heat. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **78**; practice **79**; combination/reuse **89, 145**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-13"></a>

<a id="sequence-task-447"></a>

- [ ] **TH-13 · Potential element: Freezing mold.** A container defines the geometry of solidifying material and provides heat-transfer surfaces; it has no hidden cold source. Research basis: [TH-S02](../thermal-component-research.md#th-s02).

**Visual style:** Open cream geometric tray with a cyan phase window and readable solidification front.

**Successful outcome:** Insufficient energy removal leaves a measured liquid/solid mixture; equal-temperature surroundings cannot freeze it. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **79**; practice **80**; combination/reuse **89, 149**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-14"></a>

<a id="sequence-task-448"></a>

- [ ] **TH-14 · Potential element: Ice block.** Solid-water cargo carries finite mass/enthalpy; shared phase evolution alters support/contact geometry and releases the same material as liquid. Research basis: [TH-S02](../thermal-component-research.md#th-s02).

**Visual style:** Frosted cyan faceted cuboid with a clear melt edge and bounded droplets.

**Successful outcome:** A cold control retains support; partially melted material cannot disappear or keep impossible full-block support. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **67**; practice **68**; combination/reuse **89, 139**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-15"></a>

<a id="sequence-task-449"></a>

- [ ] **TH-15 · Potential element: Ice plug.** A solid-water insert physically blocks an aperture until generic melting changes the occupied geometry. Separate part placement/shape from the ice-block cargo role. Research basis: [TH-S02](../thermal-component-research.md#th-s02).

**Visual style:** Cyan translucent insert seated visibly inside a cream aperture.

**Successful outcome:** Heating an unrelated body does not open it; residual solid continues to obstruct as geometry dictates. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **68**; practice **69**; combination/reuse **86, 137**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-16"></a>

<a id="sequence-task-450"></a>

- [ ] **TH-16 · Potential element: Fusible link.** A finite material connector loses supporting strength through its phase/constitutive state; the shared joint/contact lifecycle releases load. Research basis: [TH-S02](../thermal-component-research.md#th-s02).

**Visual style:** Small cream wax-like collar between gold load ends with a navy witness gap.

**Successful outcome:** Subthreshold heating retains support; cooling broken material does not reattach a severed graph. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **85**; practice **86**; combination/reuse **96, 149**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-17"></a>

<a id="sequence-task-451"></a>

- [ ] **TH-17 · Potential element: Kettle.** A finite fluid vessel accepts generic heat and has explicit vapor outlet/pressure boundaries; vapor comes from material enthalpy and phase state. Research basis: [TH-S02](../thermal-component-research.md#th-s02).

**Visual style:** Rounded cream vessel, cyan water gauge and clearly visible gold outlet.

**Successful outcome:** An empty vessel emits no water vapor; warm water requires further sensible/latent input before the specified vapor output. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **70**; practice **71**; combination/reuse **86, 140**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-18"></a>

<a id="sequence-task-452"></a>

- [ ] **TH-18 · Potential element: Condenser.** A cooled passage removes vapor enthalpy and collects conserved liquid through generic phase/flow models. Research basis: [TH-S02](../thermal-component-research.md#th-s02).

**Visual style:** Descending cream architectural channel with cyan state window and separate drip outlet.

**Successful outcome:** A warm or exhausted sink cannot condense an unlimited vapor stream. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **74**; practice **75**; combination/reuse **87, 137**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-19"></a>

<a id="sequence-task-453"></a>

- [ ] **TH-19 · Potential element: Steam piston.** A catalogue assembly configures the same generic pressure actuator used for supported fluids, with vapor-capable seals, inlet and exhaust; no steam-specific force callback. Research basis: [TH-S07](../thermal-component-research.md#th-s07).

**Visual style:** Cream cylinder, gold rod and separate embossed inlet/exhaust ports.

**Successful outcome:** No pressure difference, blocked exhaust or excessive load prevents the predicted stroke. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **75**; practice **76**; combination/reuse **86, 143**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-20"></a>

<a id="sequence-task-454"></a>

- [ ] **TH-20 · Potential element: Steam turbine.** A fluid-work converter exchanges pressure/enthalpy drop for load-dependent shaft torque and exhaust; a supported constitutive model governs its range. Research basis: [TH-S07](../thermal-component-research.md#th-s07).

**Visual style:** Gold radial wheel in a cream partial cutaway shell with cyan flow window.

**Successful outcome:** No available pressure/enthalpy drop gives no useful work; stall cannot generate free power. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **76**; practice **77**; combination/reuse **87, 147**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-21"></a>

<a id="sequence-task-455"></a>

- [ ] **TH-21 · Potential element: Temperature sensor.** Read local thermodynamic state through a probe/contact and emit a supplied typed threshold signal with defined hysteresis/response. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Cream engraved thermometer column with navy ticks and gold indicator.

**Successful outcome:** An unattached probe or unmet threshold does not assert the goal; sensing supplies no heat or electrical work. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **15**; practice **16**; combination/reuse **70, 83, 145**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-22"></a>

<a id="sequence-task-456"></a>

- [ ] **TH-22 · Potential element: Bimetal thermostat.** A bonded two-material strip bends by differential expansion and operates a real contact; temperature is not a magic switch instruction. Research basis: [TH-S06](../thermal-component-research.md#th-s06).

**Visual style:** Paired gold/cream curved strip with a visible navy contact gap.

**Successful outcome:** A uniform-material control lacks the same curvature; verify contact opening/closing and hysteresis from declared geometry/material mechanics. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **83**; practice **84**; combination/reuse **90, 145**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-23"></a>

<a id="sequence-task-457"></a>

- [ ] **TH-23 · Potential element: Expansion rod.** Thermal strain changes a solid rod's natural length; stiffness, constraints and applied load determine the actual displacement/work. Research basis: [TH-S06](../thermal-component-research.md#th-s06).

**Visual style:** Slender gold rod with cream guides and navy reference marks.

**Successful outcome:** Insufficient temperature change cannot bridge the target gap; constrained heating produces accounted stress. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **82**; practice **83**; combination/reuse **90, 146**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-24"></a>

<a id="sequence-task-458"></a>

- [ ] **TH-24 · Potential element: Gas expansion bladder.** A compliant sealed boundary contains finite gas whose equation of state couples heat, pressure, volume and mechanical work. Research basis: [TH-S06](../thermal-component-research.md#th-s06).

**Visual style:** Cyan geometric bellows dome, cream neck and gold volume marks.

**Successful outcome:** A vented control cannot retain the same pressure; blocked expansion changes pressure rather than prescribing motion. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **77**; practice **78**; combination/reuse **90, 143**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-25"></a>

<a id="sequence-task-459"></a>

- [ ] **TH-25 · Potential element: Hot-air balloon.** Envelope geometry, contained gas state and surrounding density produce generic buoyancy against envelope/cargo mass; model exchange through the opening. Research basis: [TH-S06](../thermal-component-research.md#th-s06).

**Visual style:** Faceted envelope in established balloon colour with a small cream cargo platform.

**Successful outcome:** Excess load or cooling prevents ascent; no fixed rising velocity or balloon-specific levitation rule. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **80**; practice **81**; combination/reuse **99, 149**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-26"></a>

<a id="sequence-task-460"></a>

- [ ] **TH-26 · Potential element: Thermal storage block.** A high-heat-capacity body stores sensible enthalpy and can be transported to release it through ordinary heat exchange. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Heavy cream ceramic prism with gold graduated energy marks.

**Successful outcome:** An equal-temperature block supplies no net heat; repeated deliveries deplete its stored difference. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **81**; practice **82**; combination/reuse **90, 140**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-27"></a>

<a id="sequence-task-461"></a>

- [ ] **TH-27 · Potential element: Phase-change storage cartridge.** An encapsulated material stores latent enthalpy while transitioning; the container retains mass and exposes a thermal interface. Research basis: [TH-S02](../thermal-component-research.md#th-s02).

**Visual style:** Cream capsule with cyan phase-fraction window and navy endpoint symbols.

**Successful outcome:** A fully transitioned cartridge cannot repeat a charge/discharge without reversing the energy transfer. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **84**; practice **85**; combination/reuse **97, 147**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-28"></a>

<a id="sequence-task-462"></a>

- [ ] **TH-28 · Potential element: Evaporative cooling pad.** A wet porous body loses finite liquid by modeled surface mass transfer, carrying latent energy; airflow and humidity affect the rate. Research basis: [TH-S08](../thermal-component-research.md#th-s08).

**Visual style:** Cream ribbed wick above a cyan reservoir with a visible wet/dry pattern.

**Successful outcome:** A dry pad or saturated surrounding gas prevents equivalent cooling; liquid consumption balances vapor output. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **73**; practice **74**; combination/reuse **88, 145**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-29"></a>

<a id="sequence-task-463"></a>

- [ ] **TH-29 · Potential element: Cold pack.** An authored initially cold finite thermal mass absorbs heat and warms; it is a store, not an active refrigerator. Research basis: [TH-S02](../thermal-component-research.md#th-s02).

**Visual style:** Frosted cyan tiled block with cream grip and a gold capacity gauge.

**Successful outcome:** A warmed pack has depleted cooling capacity; no endless subambient boundary is attached secretly. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **66**; practice **67**; combination/reuse **79, 139**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-30"></a>

<a id="sequence-task-464"></a>

- [ ] **TH-30 · Potential element: Thermoelectric generator.** A generic hot/cold converter produces bounded supplied electrical output from heat flow and temperature difference. RTG assemblies use this same converter with decay-generated heat. Research basis: [TH-S09](../thermal-component-research.md#th-s09).

**Visual style:** Layered cream/gold faces, a small finned cold face and shaped electrical terminals.

**Successful outcome:** Equal temperatures produce no thermoelectric work; load and finite cold-side rejection affect output. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **87**; practice **88**; combination/reuse **131, 145**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-31"></a>

<a id="sequence-task-465"></a>

- [ ] **TH-31 · Potential element: Flint striker.** Mechanical contact supplies a bounded ignition-energy deposit through the same thermal/reaction interface as other sources; no target lookup or guaranteed flame. Research basis: [TH-S03](../thermal-component-research.md#th-s03).

**Visual style:** Cream hinge mount with gold striking face and a restrained brief spark cue.

**Successful outcome:** A miss, insufficient work or a nonreactive target produces no sustained combustion. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **58**; practice **59**; combination/reuse **85, 99**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-32"></a>

<a id="sequence-task-466"></a>

- [ ] **TH-32 · Potential element: Tinder pad.** A finite porous combustible material configures surface area, thermal mass and reaction properties; it does not grant a special lens interaction. Research basis: [TH-S03](../thermal-component-research.md#th-s03).

**Visual style:** Warm-wood patterned wafer on a cream tray with a visibly shrinking fuel area.

**Successful outcome:** Defocused/insufficient heating fails; reacted material cannot be reused as fresh fuel. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **58**; practice **59**; combination/reuse **85, 149**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-33"></a>

<a id="sequence-task-467"></a>

- [ ] **TH-33 · Potential element: Spring-mounted match.** A finite reactive tip on a generic spring/contact assembly receives ignition energy from modeled striking work; its position and stored spring energy remain physical. Research basis: [TH-S03](../thermal-component-research.md#th-s03).

**Visual style:** Cream guide, gold visible spring and warm-wood shaft with a small original tip mark.

**Successful outcome:** No contact or spent reactive tip cannot ignite; winding the spring alone supplies no flame. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **85**; practice **86**; combination/reuse **99, 149**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-34"></a>

<a id="sequence-task-468"></a>

- [ ] **TH-34 · Potential element: Timed toaster ejector.** A supplied heater, finite thermal cargo and generic timer/latch/spring assembly perform warming and delayed ejection; each coupling is typed and energy-accounted. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Rounded cream housing, gold warming tray and cyan timed-state window.

**Successful outcome:** No supply cannot warm; an obstructed carriage cannot teleport the payload; time alone is not proof of temperature. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **91**; practice **92**; combination/reuse **99, 146**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-35"></a>

<a id="sequence-task-469"></a>

- [ ] **TH-35 · Potential element: Coffee-pot steam vessel.** A distinct authored vessel geometry combines finite liquid capacity, thermal contacts and a constrained spout; shared phase/flow laws produce its steam output, not a coffee-pot event. Research basis: [TH-S02](../thermal-component-research.md#th-s02).

**Visual style:** Cream rounded pot with a gold handle and small cyan fill window.

**Successful outcome:** An empty or insufficiently heated pot does not emit vapor; a blocked spout uses actual pressure boundaries. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **86**; practice **87**; combination/reuse **100, 140**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-36"></a>

<a id="sequence-task-470"></a>

- [ ] **TH-36 · Potential element: Lamp-trigger apparatus.** Resolve the historical lamp's trigger before implementation; represent the selected original toy design as a finite supplied light/heat source and a generic contact or displacement actuator. No historical-name selector may choose its effect. Research basis: [TH-S04](../thermal-component-research.md#th-s04).

**Visual style:** Cream sculptural lamp, gold lever and restrained luminous aperture with original icon.

**Successful outcome:** An unactuated or unsupplied source produces no new output; demonstrate trigger displacement and optical/thermal energy independently. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **86**; practice **87**; combination/reuse **100, 142**. Use the staged thermal allocation below; no extra campaign slot.

<a id="thermal-37"></a>

<a id="sequence-task-471"></a>

- [ ] **TH-37 · Potential element: Heat-sensitive target.** A finite thermal body exposes a typed temperature/phase goal observation with explicit threshold and dwell; sensing is separate from the heating source. Research basis: [TH-S01](../thermal-component-research.md#th-s01).

**Visual style:** Cream target disc with navy degree marks and a gold shaped state indicator.

**Successful outcome:** Warm another disconnected target or remain below the target threshold and the goal stays false. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

**Campaign:** Intro **15**; practice **16**; combination/reuse **59, 90, 145**. Use the staged thermal allocation below; no extra campaign slot.

<a id="generic-interaction-register"></a>

#### Individual interaction/process requirements

**Design acceptance for every item below:** [common gates](#design-acceptance) + [SIM](#accept-simulation); additional profiles apply to cross-cutting requirements.

These independently verifiable abstractions work across arbitrary capable participants. Example pairs are fixtures, never dispatch keys. Existing nonthermal interactions also require this audit; target contracts do not certify current implementation. Each IX needs typed capability tests, substitutable-part controls, conservation/boundary assertions, actual-UI integration and restoration proof. Unreachable/unsupported processes stay blocked until a separately specified material/fixture provides their prerequisite, never implemented with a scenario-specific exception.

| Task | Generic interaction | Successful outcome / negative control | First teaching objective |
| --- | --- | --- | --- |
| <a id="interaction-01"></a> [ ] <a id="sequence-task-472"></a> IX-01 | **Contact impulse** | Shared geometry/material collision exchanges momentum; separated bodies receive no contact impulse. | 1; separately staged from other new processes. |
| <a id="interaction-02"></a> [ ] <a id="sequence-task-473"></a> IX-02 | **Sliding friction** | Oppose relative tangential slip and debit mechanical work to declared heat/loss stores; a motionless unloaded contact supplies none. | 5; separately staged from other new processes. |
| <a id="interaction-03"></a> [ ] <a id="sequence-task-474"></a> IX-03 | **Joint constraint** | Constrain declared degrees of freedom through shared force rows; release only through supported lifecycle state, not a named-part instruction. | 9; separately staged from other new processes. |
| <a id="interaction-04"></a> [ ] <a id="sequence-task-475"></a> IX-04 | **Tension transmission** | Transmit admissible pull through routed rope/cable; slack cannot push or create work. | 12; separately staged from other new processes. |
| <a id="interaction-05"></a> [ ] <a id="sequence-task-476"></a> IX-05 | **Shaft torque transmission** | Transfer signed torque with finite input work and load; blocked loads cannot receive free rotation. | 31; separately staged from other new processes. |
| <a id="interaction-06"></a> [ ] <a id="sequence-task-477"></a> IX-06 | **Electrical power transfer** | Allocate finite supplied work among loads/storage; disconnected or exhausted sources deliver none. | 11; separately staged from other new processes. |
| <a id="interaction-07"></a> [ ] <a id="sequence-task-478"></a> IX-07 | **Signal propagation** | Transfer typed control state independently of energy; a true signal alone cannot power a load. | 15 for a simple sensor signal; logic follows21. |
| <a id="interaction-08"></a> [ ] <a id="sequence-task-479"></a> IX-08 | **Fluid advection** | Move finite mass, species and enthalpy through supported geometry/ports; an empty source emits nothing. | 61; separately staged from other new processes. |
| <a id="interaction-09"></a> [ ] <a id="sequence-task-480"></a> IX-09 | **Pressure work** | Exchange pressure-displacement work with a generic actuator; opposed load or sealed return changes actual motion. | 70; separately staged from other new processes. |
| <a id="interaction-10"></a> [ ] <a id="sequence-task-481"></a> IX-10 | **Buoyancy** | Use displacement and surrounding density; without a supporting fluid there is no buoyant force. | 20 for gas buoyancy; liquid displacement follows67. |
| <a id="interaction-11"></a> [ ] <a id="sequence-task-482"></a> IX-11 | **Aerodynamic drag** | Use relative flow, geometry and declared drag model; zero relative motion generates no drag. | 20; separately staged from other new processes. |
| <a id="interaction-12"></a> [ ] <a id="sequence-task-483"></a> IX-12 | **Acoustic propagation** | Account for emitted signal energy, path, attenuation and supported occlusion; an obstructed control receives the predicted diminished field. | 72; separately staged from other new processes. |
| <a id="interaction-13"></a> [ ] <a id="sequence-task-484"></a> IX-13 | **Optical transport** | Trace finite power with geometry-based reflection/refraction and occlusion; focusing redistributes irradiance rather than increasing total energy. | 51; separately staged from other new processes. |
| <a id="interaction-14"></a> [ ] <a id="sequence-task-485"></a> IX-14 | **Optical absorption** | Allocate only the absorbed fraction to material enthalpy; reflected/transmitted power cannot also become heat. | 55; separately staged from other new processes. |
| <a id="interaction-15"></a> [ ] <a id="sequence-task-486"></a> IX-15 | **Ionizing transport** | Apply supported typed particle/channel/material models and energy deposition; blocked transmission cannot activate a downstream detector. | 101; separately staged from other new processes. |
| <a id="interaction-16"></a> [ ] <a id="sequence-task-487"></a> IX-16 | **Radioactive decay** | Advance finite populations with declared emission/energy accounting; spent inventory cannot emit indefinitely. | 123; separately staged from other new processes. |
| <a id="interaction-17"></a> [ ] <a id="sequence-task-488"></a> IX-17 | **Temperature sensing** | Read a valid local thermal state with defined response and supplied outputs; observation creates no heat or actuator energy. | 15; separately staged from other new processes. |
| <a id="interaction-18"></a> [ ] <a id="sequence-task-489"></a> IX-18 | **Thermal conduction** | Transfer equal/opposite energy through geometry/contact conductance along the temperature difference; a missing contact removes that path. | 14 for plate/contact heating; conductor routing follows16. |
| <a id="interaction-19"></a> [ ] <a id="sequence-task-490"></a> IX-19 | **Thermal convection** | Exchange heat between surface and fluid through the declared transfer model; an ambient fan is not an arbitrary cold source. | 40; separately staged from other new processes. |
| <a id="interaction-20"></a> [ ] <a id="sequence-task-491"></a> IX-20 | **Thermal radiation** | Exchange emitted/absorbed non-ionizing radiant heat with view/occlusion and finite reservoirs; distinguish this channel from nuclear dose. | 55; separately staged from other new processes. |
| <a id="interaction-21"></a> [ ] <a id="sequence-task-492"></a> IX-21 | **Solid melting** | Consume latent enthalpy as solid fraction and physical support decrease; inadequate energy leaves residual solid. | 67; separately staged from other new processes. |
| <a id="interaction-22"></a> [ ] <a id="sequence-task-493"></a> IX-22 | **Liquid freezing** | Remove sensible/latent energy to an explicit sink while solid fraction/geometry grow; no colder sink means no spontaneous cooling. | 79; separately staged from other new processes. |
| <a id="interaction-23"></a> [ ] <a id="sequence-task-494"></a> IX-23 | **Liquid evaporation** | Surface mass transfer carries latent energy and responds to vapor conditions; dry/saturated controls cannot evaporate arbitrarily. | 73; separately staged from other new processes. |
| <a id="interaction-24"></a> [ ] <a id="sequence-task-495"></a> IX-24 | **Liquid boiling** | Pressure-dependent phase change consumes liquid mass and latent energy; an empty vessel cannot produce vapor. | 70; separately staged from other new processes. |
| <a id="interaction-25"></a> [ ] <a id="sequence-task-496"></a> IX-25 | **Vapor condensation** | Reject enthalpy to create conserved liquid; a warm or depleted sink limits production. | 74; separately staged from other new processes. |
| <a id="interaction-26"></a> [ ] <a id="sequence-task-497"></a> IX-26 | **Solid sublimation** | Use generic material phase data for direct solid-to-vapor transfer with finite mass/energy; unsupported materials/conditions do not transition. | 88; separately staged from other new processes. |
| <a id="interaction-27"></a> [ ] <a id="sequence-task-498"></a> IX-27 | **Vapor deposition** | Use generic phase data for direct vapor-to-solid growth with removed energy; a too-warm surface does not grow frost. | 88; separately staged from other new processes. |
| <a id="interaction-28"></a> [ ] <a id="sequence-task-499"></a> IX-28 | **Chemical reaction** | Consume declared reactants, form accounted products and transfer reaction energy through a validated material model; missing reactants limit the rate. | 56; separately staged from other new processes. |
| <a id="interaction-29"></a> [ ] <a id="sequence-task-500"></a> IX-29 | **Reaction ignition** | Initiate reaction from local state/rate under that model regardless of heating-source identity; subthreshold energy fails. | 58; separately staged from other new processes. |
| <a id="interaction-30"></a> [ ] <a id="sequence-task-501"></a> IX-30 | **Reaction extinction** | Cooling, reactant depletion and transport alter the reaction rate; no unconditional water-touch extinguish event. | 60; separately staged from other new processes. |
| <a id="interaction-31"></a> [ ] <a id="sequence-task-502"></a> IX-31 | **Thermal expansion** | Map material state to natural strain/volume and solve constraints; heated free and constrained bodies respond differently. | 77; separately staged from other new processes. |
| <a id="interaction-32"></a> [ ] <a id="sequence-task-503"></a> IX-32 | **Thermal stress** | Resolve incompatible constrained strains as mechanical stress; uniform unrestrained heating is not automatic fracture. | 82; separately staged from other new processes. |
| <a id="interaction-33"></a> [ ] <a id="sequence-task-504"></a> IX-33 | **Heat pumping** | Input work moves heat between generic thermal ports; rejected heat equals extracted heat plus work within the stated model. | 78; separately staged from other new processes. |
| <a id="interaction-34"></a> [ ] <a id="sequence-task-505"></a> IX-34 | **Thermoelectric conversion** | Temperature difference, heat flow and load govern electrical work and rejected heat; equal-temperature ports produce none. | 87; separately staged from other new processes. |
| <a id="interaction-35"></a> [ ] <a id="sequence-task-506"></a> IX-35 | **Phase-change storage** | Charge/discharge material enthalpy through ordinary heat transport and phase evolution; a full store cannot absorb unlimited latent energy. | 84; separately staged from other new processes. |
| <a id="interaction-36"></a> [ ] <a id="sequence-task-507"></a> IX-36 | **Material coating** | Conserved deposited substance changes the surface model only where applied; untreated regions retain their properties. | 94; separately staged from other new processes. |
| <a id="interaction-37"></a> [ ] <a id="sequence-task-508"></a> IX-37 | **Structural fracture** | Use stress/work and material state to alter topology into bounded conserved fragments; failure cannot create mass/energy. | 43; separately staged from other new processes. |
| <a id="interaction-38"></a> [ ] <a id="sequence-task-509"></a> IX-38 | **Capillary transport** | Material affinity, pore geometry and pressure move finite liquid with enthalpy; dry supply and saturation limit transport. | 73; separately staged from other new processes. |
| <a id="interaction-39"></a> [ ] <a id="sequence-task-510"></a> IX-39 | **Electrical dissipation** | Debit real electrical work into material internal energy through a generic resistive-load model; missing supply cannot heat. | 14; separately staged from other new processes. |
| <a id="interaction-40"></a> [ ] <a id="sequence-task-511"></a> IX-40 | **Gas state evolution** | Use finite composition, internal energy and pressure-volume constitutive state; heating a vented volume does not mimic a sealed volume. | 70 before the boiling/outlet objective; sealed expansion is later reuse77. |
| <a id="interaction-41"></a> [ ] <a id="sequence-task-512"></a> IX-41 | **Phase topology update** | Transfer ownership/geometry of solid/liquid/gas without duplication and update collision, ports, mass and inertia; partial transitions remain represented. | 67; separately staged from other new processes. |
| <a id="interaction-42"></a> [ ] <a id="sequence-task-513"></a> IX-42 | **Temperature-dependent strength** | Evaluate material strength from thermodynamic state, then use generic stress/fracture/joint models; a warm but unloaded body need not break. | 85; separately staged from other new processes. |
| <a id="interaction-43"></a> [ ] <a id="sequence-task-514"></a> IX-43 | **Sensible heat storage** | Relate enthalpy and temperature through material heat capacity outside phase transitions; energy and temperature are not interchangeable counters. | 14; separately staged from other new processes. |

<a id="thermal-interaction-scenarios"></a>

#### Individual cross-element interaction scenarios

**Design acceptance for every item below:** [common gates](#design-acceptance) + [INTERACTION](#accept-interaction); additional profiles apply to cross-cutting requirements.

Each TX is a separate acceptance/teaching obligation composed from IX abstractions, not a special-purpose part. Test supported source/material/receiver substitutions and boundary classes; restore fuel, phase, topology, energy and controller state exactly. The existing exhaustive typed connection-permutation requirement still applies.

<a id="thermal-scenario-01"></a>

<a id="sequence-task-515"></a>

- [ ] **TX-01: Fire heats water into steam.** A finite reacting fuel heats a vessel; generic heat transport warms/boils contained water and gas advection carries vapor to a receiver. Verify fuel/water/energy balance and empty-vessel/no-fuel controls. Swap candle, heated plate and stored-hot block without changing water logic.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 70 introduction, 71 practice, 86, 140 later combinations.

<a id="thermal-scenario-02"></a>

<a id="sequence-task-516"></a>

- [ ] **TX-02: Powered cooling freezes water.** A heat pump removes enthalpy from a mold and rejects it plus supplied work to a real sink. Compare powered/unsupplied and hot-side-blocked runs; retain partial freezing and exact Reset. A sufficiently cold finite pack uses the same water phase model.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 79 introduction, 80 practice, 89, 149 later combinations.

<a id="thermal-scenario-03"></a>

<a id="sequence-task-517"></a>

- [ ] **TX-03: Focused light ignites combustible material.** Finite optical transport through a lens concentrates absorbed flux, increasing material enthalpy until its reaction model ignites. Defocus, occlusion, weak illumination and noncombustible material are distinct controls; no Lens/Fuel handler.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 59 introduction, 60 practice, 85, 142 later combinations.

<a id="thermal-scenario-04"></a>

<a id="sequence-task-518"></a>

- [ ] **TX-04: Vapor drives a piston.** Pressure state drives one generic piston against a real load and conserves working material through inlet/exhaust. No pressure difference, blocked exhaust and excessive load are separate controls; no steam-specific force callback.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 75 introduction, 76 practice, 86, 143 later combinations.

<a id="thermal-scenario-05"></a>

<a id="sequence-task-519"></a>

- [ ] **TX-05: Condensation recovers working fluid.** Vapor rejects heat through a condenser to a finite/declared sink; liquid can return through taught fluid machinery. A warm sink limits return, and a closed cycle cannot produce more work than its external energy input.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 74 introduction, 75 practice, 87, 137 later combinations.

<a id="thermal-scenario-06"></a>

<a id="sequence-task-520"></a>

- [ ] **TX-06: Differential expansion operates a switch.** Two bonded materials develop different strain; resulting deformation changes physical electrical contact. Compare uniform-material and insufficient-temperature controls. A downstream load still needs supply.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 83 introduction, 84 practice, 90, 145 later combinations.

<a id="thermal-scenario-07"></a>

<a id="sequence-task-521"></a>

- [ ] **TX-07: Melting removes structural support.** An ice body's loss of solid fraction changes ordinary contact support; gravity then moves cargo. Heating another disconnected body does not trigger release; collect meltwater and restore exact initial topology.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 67 introduction, 68 practice, 89, 139 later combinations.

<a id="thermal-scenario-08"></a>

<a id="sequence-task-522"></a>

- [ ] **TX-08: Airflow cools a hot surface.** Generic fluid motion changes surface heat exchange toward ambient. Compare still air, fan flow and equal-temperature states; no under-ambient cooling unless another valid heat/mass-transfer process provides it.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 40 introduction, 41 practice, 78, 145 later combinations.

<a id="thermal-scenario-09"></a>

<a id="sequence-task-523"></a>

- [ ] **TX-09: Evaporation cools a wet surface.** Capillary feed wets a porous pad; generic evaporation exports mass/latent energy with humidity-dependent transfer. Dry pad, exhausted feed and saturated air are controls; droplets and vapor remain distinct.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 73 introduction, 74 practice, 88, 145 later combinations.

<a id="thermal-scenario-10"></a>

<a id="sequence-task-524"></a>

- [ ] **TX-10: Mechanical braking heats a thermal store.** Resistive work decreases shaft/contact energy and increases heat storage through the common ledger. A stalled stationary shaft generates no friction heat; later conduction can operate any thermal receiver.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 39 introduction, 40 practice, 81, 147 later combinations.

<a id="thermal-scenario-11"></a>

<a id="sequence-task-525"></a>

- [ ] **TX-11: Stored heat powers an electrical load.** A thermal store and colder sink drive the generic thermoelectric converter; an electrical consumer spends its actual output. Equal-temperature, depleted-store and disconnected-load controls prove there is no special RTG power rule.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 87 introduction, 88 practice, 131, 145 later combinations.

<a id="thermal-scenario-12"></a>

<a id="sequence-task-526"></a>

- [ ] **TX-12: Cooling extinguishes combustion.** Heat transport lowers reactive material state until its generic reaction rate cannot sustain combustion. Compare sufficient and insufficient applied cooling while holding fuel/oxidizer availability fixed; no water-touch extinguish event.

**Successful outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 60 introduction, 61 practice, 86, 149 later combinations.

<a id="thermal-scenario-13"></a>

<a id="sequence-task-527"></a>

- [ ] **TX-13: Vapor drives a turbine.** A supported pressure/enthalpy drop transfers bounded work to a generic shaft converter. Compare supplied flow against equal-pressure and stalled-load controls; conserve exhaust mass and reject residual heat.

**Successful outcome:** Shaft work and exhaust state balance supplied energy, with no turbine/source pair handler. Record actual-UI positive/control, typed coupling and exact Run/Reset/save restoration. **Campaign:** 76 introduction, 77 practice, 87 and147 reuse.

<a id="thermal-scenario-14"></a>

<a id="sequence-task-528"></a>

- [ ] **TX-14: Oxidizer restriction extinguishes combustion.** Generic gas transport limits reactant delivery to a finite burning material. Change a physical ventilation aperture while keeping the thermal boundary and fuel equivalent; quantify reaction/product/energy changes.

**Successful outcome:** A restricted route limits sustained reaction while an open-route control does not. No named hood/fuel lookup or instant vanish event. Record actual-UI construction, gas-state observations and exact Run/Reset/save restoration. **Campaign:** 61 introduction after cooling suppression60, 62 practice, 86 and149 reuse.

<a id="thermal-campaign-allocation"></a>

#### Thermal integration into the same 150 levels

**Design acceptance for every item below:** [common gates](#design-acceptance) + [CONTENT](#accept-content); additional profiles apply to cross-cutting requirements.

The individual TH/TX entries give exact intro, practice and reuse numbers. This table explains prerequisites and the pun/reward progression; it supplements the existing 15 chapters, GAP entries and radiation lessons, without adding levels or dropping their obligations.

| Existing slots | Lesson names | Individually introduced records | Teaching and combination |
| --- | --- | --- | --- |
| 14–20 | **Some Like It Watt / A Matter of Degrees / Conduct Yourself** | TH-04, 21, 37, 08, 09 | Introduce finite electrical heating, temperature observation, heat routing and insulation separately alongside the existing electrical/mechanical lessons. |
| 39–41 | **Brake for Warmth / A Warm Reception** | TH-05, 10 | Mechanical dissipation then passive heat rejection; separate heat storage from new work and ambient from active cooling. |
| 55–61 | **Focus Pocus / A Burning Question** | TH-07, 01, 02, 03, 31, 32, 06 | Absorption before finite combustion, ignition before focused-light ignition. Lens use retains the earlier optical lesson; source, fuel and striker each have a separate construction objective. |
| 66–71 | **Thaw and Order / Full Steam Ahead** | TH-29, 14, 15, 11, 17 | Finite cold store and melting precede exchanger/boiling; retain chapter 7 fluid conservation and each original valve/flow lesson. |
| 73–81 | **Dew Process / Cool Runnings / Freeze a Jolly Good Fellow** | TH-28, 18, 19, 20, 24, 12, 13, 25 | Evaporation and condensation precede vapor work; gas expansion then supplied heat pumping and freezing; heated buoyancy follows known density/flow. Separate the heat pump's forward/reverse modes before dependent use. |
| 81–88 | **A Warm Memory / Bend It Like Bimetal / Condense and Sensibility** | TH-26, 23, 22, 27, 16, 33, 35, 36, 30 | Sensible/latent storage, expansion, physical thermostat, fusible release, bounded ignition and vessel variants, then thermoelectric conversion. Keep ratchet/flywheel/accumulator lessons as separate objectives. 88 adds separate supported sublimation/deposition demonstrations using generic material state, with their own controls. |
| 89–100 | **Chill of the Chase / Toast of the Town** | TH-34 plus thermal practice | Consolidate freeze/melt/control loops, introduce the timed thermal ejector at91, then reuse thermal state in granular/coating/fragmentation and authoring exercises. Preserve the GAP and character lessons. |
| 101–135 | **Waste Not, Watt Not** | Reuse only; all radiation introductions retained | Earlier thermal principles support absorbed radiation energy, cooling and RTG startup at131–132. Do not consume radiation's reserved first-use objectives. |
| 136–150 | **Every Drop Twice / The Quiet Engine / The Grand Contraption** | Integrated reuse only | Condense/reuse discharge137, manage finite cold/structural support139, batch/shutdown140, vapor/pneumatic work143, heat rejection/RTG145 and resource sharing147. No required new element/mode. |

<a id="sequence-task-529"></a>

- [ ] Author every TH/IX/TX first-use objective and separate control as a typed lesson record with prerequisites, saved stage state, available inventory and later reuse. Each physical element is placed/configured by the player; seeing it as a background fixture does not count. Preserve the original objective in a shared slot, then offer a separately scaffolded objective and a reward/rest beat before combining new ideas. Saved progress survives staged objectives; hints and slower playback carry no penalty.

**Successful outcome:** All 37 thermal element records and 14 thermal scenarios have named placements within 1–150, and IX processes have separate first-use reservations. The existing 150 level IDs and radiation/GAP coverage remain intact. The density is explicitly **unvalidated**: pilot duration, comprehension and fatigue, then redistribute objectives within those 150 slots rather than declare a crowded chapter teachable from a table. Every unsupported mode stays blocked and visible. Finales136–150 only combine taught mechanisms; private creative remixes remain optional rewards.

<a id="specialist"></a>

## Priority 6 — complete specialist mechanisms and variants

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

<a id="variants"></a>

### 6.1 Further puzzle roles and historical candidates

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** After broadly reusable component families; still in scope.

**Gameplay outcome:** Each additional ball, character, tool or field introduces an identifiable decision, such as material response, safe arrival, timed ejection or constrained flight, rather than a cosmetic duplicate.

Resolve uncertain historical behaviours before implementation. Keep original names as research references and use original game artwork/characters. Multi-name rows require an individual implementation/verification record for every distinct element; do not close an umbrella from one representative.

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-198"></a> [ ] <a id="sequence-task-530"></a> P2 | Baseball: additional ball type | Add a recognisable silhouette/material variant only after measured mass/bounce differences justify a distinct puzzle role. |

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-217"></a> Scope index | Individual element specifications: [EL-076](#element-076), [EL-077](#element-077). | Retained research reference: [Inventory](https://the-incredible-machine.fandom.com/wiki/The_Incredible_Machine_2/Parts). Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |
| <a id="todo-218"></a> Scope index | Individual element specifications: [EL-078](#element-078), [EL-079](#element-079). | Retained research reference: [Inventory](https://the-incredible-machine.fandom.com/wiki/The_Incredible_Machine_2/Parts). Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |
| <a id="todo-222"></a> Scope index | Individual element specifications: [EL-080](#element-080), [EL-081](#element-081). | Retained research reference: [Manual](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf). Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-200"></a> Scope index | Individual element specifications: [EL-082](#element-082), [EL-083](#element-083), [EL-084](#element-084), [EL-085](#element-085). | Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-221"></a> Scope index | Individual element specifications: [EL-086](#element-086), [EL-087](#element-087), [EL-088](#element-088). | Retained research reference: [Manual](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf). Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |
| <a id="todo-216"></a> Scope index | Individual element specifications: [EL-089](#element-089), [EL-090](#element-090). | Retained research reference: [Inventory](https://the-incredible-machine.fandom.com/wiki/The_Incredible_Machine_2/Parts). Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

| Task | Historical piece / role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-197"></a> Scope index | Individual element specifications: [EL-091](#element-091), [EL-092](#element-092), [EL-093](#element-093), [EL-094](#element-094), [EL-095](#element-095). | Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-219"></a> Scope index | Individual element specifications: [EL-096](#element-096), [TH-33](#thermal-33), [TH-35](#thermal-35), [TH-36](#thermal-36). | Retained research reference: [Walkthrough](https://sierrachest.com/index.php?a=games&fld=walkthrough&id=229&pid=102). Each linked element has its own implementation/proof obligation; shared interactions use the [generic process register](#generic-interaction-register). |

<a id="advanced"></a>

### 6.2 Advanced storage, thermal systems and cosmetics

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** After their energy, material and thermal dependencies.

**Gameplay outcome:** Stored pressure, absorption, rotational inertia and heating/cooling create explainable delayed or sustained actions. Cosmetic rewards can be chosen without changing puzzle outcomes.

This is a later queue, not a scope reduction. Core readability fixes remain earlier. Holiday art must not consume teaching slots for unchanged mechanics.

<a id="todo-340"></a>

**Scope index — individual element specifications:** [EL-033](#element-033), [EL-034](#element-034), [EL-035](#element-035), [EL-036](#element-036), [EL-171](#element-171), [EL-172](#element-172), [TH-13](#thermal-13), [TH-14](#thermal-14), [TH-15](#thermal-15), [TH-17](#thermal-17), [TH-18](#thermal-18), [TH-19](#thermal-19), [TH-20](#thermal-20), [TH-26](#thermal-26), [TH-27](#thermal-27). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-531"></a>

- [ ] **Separate integration verification:** Finite absorption/storage or thermal state can be observed before it produces an action. Accumulators, sponges, wicks, sprinklers, ice and steam remain individually tracked after their prerequisites. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="todo-377"></a>

**Scope index — individual element specifications:** [EL-110](#element-110), [EL-111](#element-111), [TH-22](#thermal-22), [TH-23](#thermal-23). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-532"></a>

- [ ] **Separate integration verification:** A flywheel bridges a measured supply gap using stored rotational energy, a governor responds to actual speed, and a thermal actuator changes state from an explicit heat source; each has a visible depletion/cooling control. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

| Task | Historical evidence / puzzle role | Proposed implementation and acceptance |
| --- | --- | --- |
| <a id="todo-223"></a> [ ] <a id="sequence-task-533"></a> P3 | Holiday replacements exist in the edition comparison. [Inventory](https://the-incredible-machine.fandom.com/wiki/The_Incredible_Machine_2/Parts) | Treat these as optional unlockable cosmetic variants after core mechanics; do not spend early campaign slots reteaching identical behaviour. |

<a id="cross-game-gaps"></a>

### 6.3 Cross-game puzzle elements and campaign tools

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

**When:** Shared physics and current component proofs remain first. Deliver construction/inspection tools with priorities 1–2 where dependencies permit; physical additions follow their prerequisite families.

**Gameplay outcome:** A comprehensive set of potential pieces and supporting tools lets players build structures, vehicles and material-processing machines, understand their behaviour, save their inventions and author their own puzzles.

**Scope — revised 28 September 2026:** All **GAP-01–18** from the [cross-game audit](../physics-puzzle-gap-audit.md) are now included in the potential-feature backlog and the 150-level campaign plan at the user's request. These are individual specifications, not merely an evaluation shortlist. All remain unimplemented/unverified here; inclusion is not completion. Existing component/radiation requirements remain intact. The audit's additional speculative alternatives are research context, not substitutes for these 18 records.

**Shared visual and technical contract:** Every record follows [DESIGN.md](../../DESIGN.md#art-direction--monument-valley-inspired): original sculptural dioramas, rounded/chamfered simple forms, matte surfaces, soft directional lighting, gentle shadows and calm purposeful motion. Preserve the existing sky/wood/cream/navy/gold and established part colours exactly; Monument Valley informs form and composition, not a replacement palette. Use original pictograms, shaped state/port cues and contextual touch controls. Rendered motion must agree with contact, state and energy; no permanent inspectors, harsh metal, clutter or colour-only information. Each physical element needs build/run, selected/preview and dense-scene desktop/mobile visual and motion evidence.

**Common completion requirements:** Closed modes/materials/actions/goals/categories stay enum-typed end-to-end; extensible part/instance IDs remain strongly typed. Validate serialized/UI boundaries and reject unsupported input. Each distinct element and supported mode needs real-UI construction, positive and meaningful negative/control behaviour, applicable typed connections, exact Run/Reset restoration and save/reload proof, focused native checks and production build. Record tested revision, recipes, assertions, captures and retained failures; commit/push each independently verified element. UI features get equivalent actual-UI workflow/control evidence. No checkbox below claims those proofs exist.

The palette's category enum must cover these roles; a grain dispenser stays a discrete-material element even when displayed next to fluid-flow tools. Alphabetize within the existing fundamental-type groups. Authoring/experiment tools are contextual UI, not fake inventory pieces.

<a id="gap-01"></a>

**Scope index — individual element specifications:** [EL-112](#element-112), [EL-113](#element-113). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

**Visual style:** Chamfered cream ceramic beams with warm-wood inset faces, navy joint marks and gold connection collars; sparse sculptural arches rather than exposed industrial trusses.

<a id="sequence-task-534"></a>

- [ ] **Separate integration verification:** A braced frame supports the specified load while an otherwise identical unbraced frame deflects/collapses under the supported model; no implicit world anchor. Moving/rotating a member updates real contact and connected mass. Apply the common completion requirements above. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

**Campaign:** Introduction **4 · Brace Yourself**; guided practice **5**; later combinations/reuse **18, 65, 138**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-02"></a>

**Scope index — individual element specifications:** [EL-114](#element-114), [EL-115](#element-115). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

**Visual style:** Cream bearing plinth, gold visible pin and navy axis notch; an unobtrusive selected-only arc shows actual limits. Moving artwork follows the solved joint pose.

<a id="sequence-task-535"></a>

- [ ] **Separate integration verification:** A connected lever transfers motion; a detached control does not. A free pivot rotates and a limited pivot stops at its actual bound. Reset restores attachments, limits and construction transforms. Apply the common completion requirements above. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

**Campaign:** Introduction **9 · Joint Effort**; guided practice **10**; later combinations/reuse **38, 46, 138**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-03"></a>

**Scope index — individual element specifications:** [EL-116](#element-116), [EL-117](#element-117). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

**Visual style:** Cream disc wheels, warm-wood tread, navy hub recess and one gold spoke reveal spin and slip; rounded chassis members stay visibly separate.

<a id="sequence-task-536"></a>

- [ ] **Separate integration verification:** A loaded chassis rolls downhill, slips under insufficient traction and stops against a physical block; lifted wheels cannot support it. Compare locked/free axle modes separately if both are supported. Apply the common completion requirements above. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

**Campaign:** Introduction **18 · Wheel Meet Again**; guided practice **19**; later combinations/reuse **37, 68, 138**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-04"></a>

<a id="sequence-task-537"></a>

- [ ] **GAP-04 · P2 potential: Driven wheel.** **Type:** Mechanical. A distinct supplied wheel module turns electrical or shaft work into bounded axle torque. Select and teach each adopted input mode explicitly; initially use the already taught electrical motor and mechanical drive connection. Account for load, traction, coast and supply loss.

**Visual style:** A compact cyan drive pod and gold shaft collar distinguish it from the passive wheel without changing the wheel silhouette. A shaped supply mark supplements the state lamp.

**Successful outcome:** Supply drives a loaded vehicle; missing supply produces no new work, an overloaded drive stalls and a low-friction wheel slips. Coasting comes only from stored motion. Verify connected drivetrain and driven-wheel Reset independently of GAP-03. Apply the common completion requirements above.

**Campaign:** Introduction **37 · Driven to Deliver**; guided practice **38**; later combinations/reuse **48, 68, 138**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-05"></a>

**Scope index — individual element specifications:** [EL-159](#element-159), [EL-160](#element-160), [EL-161](#element-161). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

**Visual style:** Cream collar with a gold load band and navy segmented witness marks; selected-only stress indication includes shape/scale, not colour alone. A break opens a real visible gap.

<a id="sequence-task-538"></a>

- [ ] **Separate integration verification:** The same supported assembly survives below threshold and breaks above it; loss of the connector changes actual topology and load paths. Debris remains bounded and Reset restores the exact pre-break graph. Apply the common completion requirements above. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

**Campaign:** Introduction **43 · The Last Straw**; guided practice **44**; later combinations/reuse **65, 96, 139**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-06"></a>

<a id="sequence-task-539"></a>

- [ ] **GAP-06 · P3 potential: Granular dispenser.** **Type:** Gravity / fluid-flow neighbour. A finite hopper releases simulated grains that pile, flow and jam. Granules remain discrete conserved material, not liquid or infinite repeated balls. Specify grain-size presets, outlet control and performance limits before campaign authoring.

**Visual style:** A cream tapered vessel with a cyan inspection window, gold outlet lip and calm visible fill line. Restrained grains use existing material colours; quantity stays legible at mobile size.

**Successful outcome:** Discharged plus retained material equals initial feed within the explicit representation tolerance. Empty feed stops, a blocked aperture jams, and opening the physical route clears it. Verify interaction with buckets and weighing controls. Apply the common completion requirements above.

**Campaign:** Introduction **91 · Against the Grain**; guided practice **92**; later combinations/reuse **96, 136, 141**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-07"></a>

<a id="sequence-task-540"></a>

- [ ] **GAP-07 · P3 potential: Granular sieve.** **Type:** Mechanical. A physical aperture separates a taught mixture by grain size. Depend on GAP-06 and real collision openings; selected-only aperture choices use typed presets. Do not classify particles by a hidden desired-output label.

**Visual style:** Shallow cream frame and navy slotted insert on gold supports; distinct aperture silhouettes make the selected size clear without a numeric inspector.

**Successful outcome:** Small grains pass and oversize grains remain; an all-oversize control blocks and finite mixed feed conserves both fractions. Verify either gravity-only operation or every separately adopted powered-shake mode before using it. Apply the common completion requirements above.

**Campaign:** Introduction **93 · Sift Happens**; guided practice **94**; later combinations/reuse **97, 136, 141**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-08"></a>

**Scope index — individual element specifications:** [EL-118](#element-118), [EL-119](#element-119). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

**Visual style:** A cream arch over a cyan shallow basin, gold application lip and navy pattern stamp on finished cargo. Patterns distinguish treated/untreated states without introducing a new palette.

<a id="sequence-task-541"></a>

- [ ] **Separate integration verification:** Treated cargo satisfies a downstream material-state goal; untreated cargo fails. Depletion prevents further treatment and application accounts for consumed supply. Verify partial/contact conditions and persistence without silently granting complete coverage. Apply the common completion requirements above. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

**Campaign:** Introduction **94 · Coat of Many Colours**; guided practice **95**; later combinations/reuse **98, 136, 150**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-09"></a>

<a id="sequence-task-542"></a>

- [ ] **GAP-09 · P3 potential: Fragmentation station.** **Type:** Mechanical. A supplied press or impact station breaks one input into a bounded, deterministic set of physical fragments. Specify the fracture threshold and each supported material; fragments conserve mass and inherit explicit material state. Connect to existing broken-goal plans.

**Visual style:** A rounded cream press arch, cyan moving head and gold impact face over a navy catch tray. Show calm, bounded physical separation without sparks or gritty debris.

**Successful outcome:** Insufficient impact leaves cargo intact; adequate supplied work creates finite fragments that can pass a smaller route. Blocked/unsupplied controls fail appropriately; fragments cannot create extra feed or lose inherited state on save/Reset. Apply the common completion requirements above.

**Campaign:** Introduction **95 · A Smashing Success**; guided practice **96**; later combinations/reuse **99, 141, 150**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-10"></a>

<a id="sequence-task-543"></a>

- [ ] **GAP-10 · P2 potential: Multi-selection and reusable assembly blueprints.** **Type:** Construction UI. Select, copy, move, rotate and save a connected group using touch-capable contextual controls. Preserve typed internal links, allocate fresh instance IDs, declare exposed ports and charge inventory per constituent. External links require explicit reconnection; reject dangling references.

**Visual style:** A faint cream/gold group outline and sparse port marks fit the existing selection language. Assembly cards use original navy icons and diorama thumbnails; no permanent CAD toolbar.

**Successful outcome:** One gesture is one Undo. Copies preserve geometry and internal typed configuration without shared identities; reconnect and run both copies independently. Illegal overlap, excess inventory and unsupported boundary links fail visibly. Apply the common completion requirements above.

**Campaign:** Introduction **20 · Some Assembly Required**; guided practice **30**; later combinations/reuse **50, 100, 146**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-11"></a>

<a id="sequence-task-544"></a>

- [ ] **GAP-11 · P2 potential: Player puzzle editor.** **Type:** Authoring UI. Author fixed setup, finite inventory, supported environment, typed goals and hints, then test using player restrictions. Store a demonstrated solution separately from the playable puzzle. Provide a campaign-linked miniature authoring exercise and keep publishing optional.

**Visual style:** Reuse the calm cream drawer, navy pictograms, gold fixed/available markers and generous spacing. Preview authored layouts as the same sculptural diorama; avoid a separate technical editor aesthetic.

**Successful outcome:** Author a small solvable puzzle, run through actual player controls, revise an invalid goal reference and save/reopen it. Creator privileges cannot leak into play mode. A guided local editor exercise never requires an account or public upload. Apply the common completion requirements above.

**Campaign:** Introduction **98 · Mind Your Own Business**; guided practice **100**; later combinations/reuse **140, 150**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-12"></a>

<a id="sequence-task-545"></a>

- [ ] **GAP-12 · P2 potential: Portable puzzle and machine exchange.** **Type:** Persistence UI. Export/import machines and player puzzles as separately validated current-schema documents with typed references and bounded sizes. Reject unknown/unsupported values explicitly; no migrations, old-name aliases or executable scripts. Keep solution data separate from a published puzzle.

**Visual style:** Quiet import/export pictograms in the existing drawer and a small original diorama preview with a readable validation result; no mandatory online storefront.

**Successful outcome:** Round-trip both document types in a fresh session with exact parts, connections and supported settings. Corrupt input cannot damage the current construction. Campaign teaches a local save/export preview; network upload or file-picker availability never gates the base win. Apply the common completion requirements above.

**Campaign:** Introduction **30 · Handle with Care**; guided practice **50**; later combinations/reuse **100, 150**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-13"></a>

<a id="sequence-task-546"></a>

- [ ] **GAP-13 · P2 potential: Named save library and local player profiles.** **Type:** Persistence UI. Keep multiple named constructions and independent progress profiles using typed identities. Explicit overwrite/delete choices, Undo/recovery where supported and thumbnail navigation must work by touch. No cloud service dependency.

**Visual style:** Cream library cards with navy labels, gold selection and uncluttered diorama thumbnails; preserve approved colour hierarchy and quiet transitions.

**Successful outcome:** Two constructions in two profiles survive reload without overwriting or cross-crediting progress. Save the current attempt, reopen it and continue with the exact configuration. Offer profile selection outside the puzzle rather than forcing profile switching to win. Apply the common completion requirements above.

**Campaign:** Introduction **2 · A Place for Everything**; guided practice **10**; later combinations/reuse **30, 100, 150**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-14"></a>

<a id="sequence-task-547"></a>

- [ ] **GAP-14 · P2 potential: Player simulation transport controls.** **Type:** Experiment UI. Expose pause, one simulation step and slow/normal playback through contextual touch controls. Equal simulated time must yield the same supported result at each playback speed. Replay/seek needs its own recording and deterministic-state contract before being offered.

**Visual style:** Small navy transport icons on a cream contextual strip, gold active state and no pulsing overlays. Keep time and paused state readable; render easing must not misrepresent a stopped simulation.

**Successful outcome:** Pause really stops state progression; one step advances exactly the defined simulation quantum. Compare slow/normal outcomes, resume and Reset. Treat recorded replay/seek as a separate child specification, with no claim that a captured movie is a reproducible simulation. Apply the common completion requirements above.

**Campaign:** Introduction **3 · Wait a Second**; guided practice **8**; later combinations/reuse **29, 80, 148**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-15"></a>

<a id="sequence-task-548"></a>

- [ ] **GAP-15 · P2 potential: Optional causal inspection.** **Type:** Experiment UI. Inspect a selected object's actual force/load/energy or signal history and an observable blocked/unpowered reason. Show only supported measured quantities, with units, and distinguish cause from inferred advice. Extend current part-state cues rather than reveal a reference solution.

**Visual style:** Thin selected-only navy/gold traces, sparse shaped markers and small cream readouts; retain the spacious diorama and avoid permanent inspectors or colour-only graphs.

**Successful outcome:** An intentionally detached drive shows missing supply, a constrained assembly shows its actual blockage, and live values follow simulation time. Reset clears prior-run traces. Hiding inspection leaves the puzzle readable and does not change rewards. Apply the common completion requirements above.

**Campaign:** Introduction **7 · Cause for Celebration**; guided practice **15**; later combinations/reuse **39, 66, 145**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-16"></a>

**Scope index — individual element specifications:** [EL-120](#element-120), [EL-121](#element-121), [EL-122](#element-122), [EL-123](#element-123). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

**Visual style:** Use cream goal cards with navy quantity dots/order marks and gold progress; small on-object gauges complement rather than clutter the scene. Completion feedback remains calm and positive.

<a id="sequence-task-549"></a>

- [ ] **Separate integration verification:** Each variant has a positive and near-miss control: insufficient quantity, transient-only throughput, reversed order and an unsafe final state. Reject double counting and premature success. Goals remain outcome-based and allow alternative constructions. Apply the common completion requirements above. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

**Campaign:** Introduction **25 · A Steady Job (quantity); 29 · Going Steady (rate); 49 · First Things First (order); 89 · Leave It Lovely (end state)**; guided practice **26, 30, 50, 90 respectively**; later combinations/reuse **quantity: 96, 136; rate: 97, 140; order: 100, 146; end state: 100, 150**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-17"></a>

<a id="sequence-task-550"></a>

- [ ] **GAP-17 · P2 potential: Authored construction zones.** **Type:** Placement / authoring. Define allowed and forbidden build volumes separately from world collision, camera clipping and fixed fixtures. Validate actual rotated part shapes and connected assemblies, including grid snap and boundary contact. Keep visual zones distinct from force fields.

**Visual style:** Low-contrast cream boundary outlines and navy hatch marks only while building/selecting; valid gold cues share the current preview language. Zones must not resemble solid walls during Run.

**Successful outcome:** Legal construction succeeds and an overhanging rotated piece/assembly fails with a local explanation. Snapping cannot push an accepted pose outside its zone. Save/export preserve author constraints; zones exert no runtime force. Apply the common completion requirements above.

**Campaign:** Introduction **6 · Room for Improvement**; guided practice **8**; later combinations/reuse **19, 42, 147**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-18"></a>

**Scope index — individual element specifications:** [EL-124](#element-124), [EL-125](#element-125). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

**Visual style:** Use a quiet cream environment plaque with navy direction/air symbols and gold selection. Reuse the approved sky/wood palette; avoid per-preset recolouring or misleading weather effects.

<a id="sequence-task-551"></a>

- [ ] **Separate integration verification:** The same construction responds predictably to two supported gravity environments; save/export/replay preserve the chosen world. Unsupported atmosphere choices are unavailable and rejected at boundaries. Each later adopted pressure/air preset needs a distinct lesson before required use. Apply the common completion requirements above. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

**Campaign:** Introduction **17 · Down to Earth (gravity); 79 · Air on the Side (supported atmosphere)**; guided practice **18, 80 respectively**; later combinations/reuse **gravity: 97, 139; atmosphere: 99, 143**. These refer to the mandatory in-level objectives in the [campaign allocation](#campaign-gap-allocation), not additional campaign levels.

<a id="gap-historical-followthrough"></a>

<a id="sequence-task-552"></a>

- [ ] Refine [the historical candidate register](#todo-227) with one source/edition/behaviour record per piece; retain unresolved primary-evidence gaps for original/Even More, Version 3, Return, Even More Contraptions, Toons and ports.

**Successful outcome:** Confirmed manual evidence remains separate from later-series descriptions. Recipe-only winch, slingshot/catapult and zipline references become constituent/dedicated-piece specifications; finite electrical storage and character/thermal/container variants get individual records before campaign use. Existing requirements are not dropped or duplicated.

<a id="gap-campaign-allocation"></a>

<a id="sequence-task-553"></a>

- [ ] Author every GAP-01–18 introductory/practice/reuse objective from the [campaign allocation](#campaign-gap-allocation) within the existing 150 levels, with the shared art direction and [positive-play requirements](#campaign-delight).

**Successful outcome:** All 18 records are represented; physical pieces are assembled by the player, UI tools receive guided use, and individual goal/environment modes are taught before dependent puzzles. Existing lessons and radiation coverage remain accounted for. Keep 150 numbered levels, retain failed teaching attempts, and rebalance staged objectives after playtests without silently removing a required element.

<a id="individual-element-register"></a>
<a id="individual-puzzle-elements"></a>

### 6.4 Individual specifications expanded from existing families

**Design acceptance for every item below:** [common gates](#design-acceptance) + [ELEMENT](#accept-element); additional profiles apply to cross-cutting requirements.

These records split previously grouped scope; they do not duplicate delivered parts or reset historical evidence. An unchecked record means its full current contract/proof remains open after the shared-engine replacement. Original source anchors below retain priority, adoption status and historical evidence; an unresolved historical candidate remains a candidate. Existing individually specified parts elsewhere in TODO remain authoritative. Family lists and campaign combination tables are navigation/teaching summaries, never implementation completion units.

**Common visual contract:** Apply DESIGN.md's original Monument Valley-inspired sculptural forms, soft light, matte surfaces, chamfered silhouettes and calm motion while retaining the approved palette. Mechanical pieces use readable joint/load shapes; water and air pieces expose contained level/pressure through restrained windows; light and radiation instruments use shaped apertures and engraved channel marks; acoustic pieces visibly pulse even when muted; characters use original simple silhouettes. Material, mode and state need shape/pattern cues rather than colour alone. Avoid decorative motion that contradicts simulation.

**Common architecture/proof contract:** Every row declares typed geometry, material, ports, state and capabilities to shared subsystems. A part name, catalogue ID or pair of part classes must never select interaction behaviour. Effects depend on generic contact, transport, field, energy, material and sensing rules. All closed sets remain enums through callers, content, collections and tests. Each record requires its own positive/control real-UI construction, applicable typed connections, exact Run/Reset and current-schema save/reload, native boundary checks, build, revision/captures and retained failures before its own implementation/verification/commit/push completion. Existing evidence stays historical until refreshed when affected.

**Campaign contract:** Each record inherits its original family's reserved first-use window in the 150-level ledger, but receives an independent introduction, practice and spaced-use objective before its chapter closes. Modes are separate objectives; no crowded family-level reservation counts as teaching proof. Rebalance staged objectives within the same 150 levels, preserve prerequisites and reward breaks, and introduce no new required element in 136–150.

<a id="element-001"></a>

<a id="sequence-task-554"></a>

- [ ] **EL-001 · Finite reservoir.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-334). Stores a bounded liquid inventory with actual head and overflow.

**Successful outcome:** Outlet flow depletes inventory; an empty tank supplies none. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-002"></a>

<a id="sequence-task-555"></a>

- [ ] **EL-002 · Header tank.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-334). Elevated storage supplies gravitational head from its real height.

**Successful outcome:** Lowering it reduces available lift; cycling cannot create energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-003"></a>

<a id="sequence-task-556"></a>

- [ ] **EL-003 · Tap.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-334). A bounded aperture regulates flow from a connected supply.

**Successful outcome:** Closing stops routed flow; an unconnected tap emits nothing. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-004"></a>

<a id="sequence-task-557"></a>

- [ ] **EL-004 · Catch basin.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-334). Open vessel collects intersecting liquid flow up to capacity.

**Successful outcome:** A missed stream remains outside; overflow remains conserved. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-005"></a>

<a id="sequence-task-558"></a>

- [ ] **EL-005 · Liquid funnel.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-334). A tapered collector guides liquid toward a real outlet.

**Successful outcome:** Blocked outlet fills and spills; no remote capture. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-006"></a>

<a id="sequence-task-559"></a>

- [ ] **EL-006 · Drain.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-334). An explicit boundary collects and accounts for discharged liquid.

**Successful outcome:** Only liquid crossing its intake enters the tracked sink. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-007"></a>

<a id="sequence-task-560"></a>

- [ ] **EL-007 · Open gutter.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-334). Open channel carries liquid under gravity.

**Successful outcome:** Adverse slope stalls or spills; no hidden uphill transport. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-008"></a>

<a id="sequence-task-561"></a>

- [ ] **EL-008 · Straight water pipe.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-335). Sealed oriented conduit joins compatible fluid mouths.

**Successful outcome:** A disconnected mouth cannot feed its neighbour. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-009"></a>

<a id="sequence-task-562"></a>

- [ ] **EL-009 · 45-degree water elbow.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-335). Routes sealed flow through a 45-degree change.

**Successful outcome:** Wrong-facing or incompatible joins remain disconnected. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-010"></a>

<a id="sequence-task-563"></a>

- [ ] **EL-010 · 90-degree water elbow.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-335). Routes sealed flow through a right angle.

**Successful outcome:** Blocking its outlet prevents through-flow. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-011"></a>

<a id="sequence-task-564"></a>

- [ ] **EL-011 · Water T junction.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-335). Divides available flow among connected branches.

**Successful outcome:** Branch totals cannot exceed supply. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-012"></a>

<a id="sequence-task-565"></a>

- [ ] **EL-012 · Water pipe cap.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-335). Closes a compatible fluid mouth.

**Successful outcome:** Capped flow stops; deleting the cap restores a real opening. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-013"></a>

<a id="sequence-task-566"></a>

- [ ] **EL-013 · Water nozzle.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-335). Trades supplied pressure for a bounded directed jet.

**Successful outcome:** Insufficient head cannot produce the rated reach. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-014"></a>

<a id="sequence-task-567"></a>

- [ ] **EL-014 · Water-carrying bucket.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-337). Movable container couples conserved contents to mass and spill geometry.

**Successful outcome:** Filled load changes rope balance; empty control does not. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-015"></a>

<a id="sequence-task-568"></a>

- [ ] **EL-015 · Leaky bucket.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-337). Finite aperture discharges actual contents and changes load.

**Successful outcome:** Caught leakage equals lost inventory; empty bucket stops leaking. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-016"></a>

<a id="sequence-task-569"></a>

- [ ] **EL-016 · Float.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-337). Buoyant body responds to displaced fluid and load.

**Successful outcome:** Overloaded float sinks; an empty basin provides no buoyant lift. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-017"></a>

<a id="sequence-task-570"></a>

- [ ] **EL-017 · Mechanical float valve.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-337). Float linkage mechanically changes a real flow aperture.

**Successful outcome:** Immobilised linkage prevents closure even at high water. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-018"></a>

<a id="sequence-task-571"></a>

- [ ] **EL-018 · Electronic level switch.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-337). Separately supplied contact observes actual local fluid height.

**Successful outcome:** Dry and unpowered controls cannot switch the output. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-019"></a>

<a id="sequence-task-572"></a>

- [ ] **EL-019 · Water check valve.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-338). Pressure difference opens the permitted direction and closes reverse flow.

**Successful outcome:** Reverse head cannot flow through a closed seat. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-020"></a>

<a id="sequence-task-573"></a>

- [ ] **EL-020 · Water diverter.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-338). Selects one physically connected outlet.

**Successful outcome:** Unselected route receives only explicitly modelled leakage. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-021"></a>

<a id="sequence-task-574"></a>

- [ ] **EL-021 · Sluice gate.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-338). A moving gate changes an open channel aperture.

**Successful outcome:** Closed gate retains upstream volume without deleting inflow. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-022"></a>

<a id="sequence-task-575"></a>

- [ ] **EL-022 · Water pump.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-338). Finite external work raises pressure and transports liquid.

**Successful outcome:** Unpowered pump cannot sustain uphill flow. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-023"></a>

<a id="sequence-task-576"></a>

- [ ] **EL-023 · Archimedes screw.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-338). Rotating bounded screw lifts fluid through its real intake and discharge.

**Successful outcome:** Wrong rotation or a dry intake cannot deliver the target volume. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-024"></a>

<a id="sequence-task-577"></a>

- [ ] **EL-024 · Primed siphon.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-338). Continuous liquid column carries flow while the source and head permit.

**Successful outcome:** Breaking the column stops the siphon; it cannot lift indefinitely above available pressure. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-025"></a>

<a id="sequence-task-578"></a>

- [ ] **EL-025 · Tipping-bucket water clock.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-338). Collected liquid shifts a pivoted bucket until it tips and discharges.

**Successful outcome:** Subthreshold volume does not emit a tip event. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-026"></a>

<a id="sequence-task-579"></a>

- [ ] **EL-026 · Communicating tank.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-339). Connected vessel exchanges volume according to pressure and elevation.

**Successful outcome:** Disconnected tank does not equalise remotely. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-027"></a>

<a id="sequence-task-580"></a>

- [ ] **EL-027 · Canal lock chamber.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-339). Bounded chamber changes water level through separately controlled openings.

**Successful outcome:** Open ends cannot hold a raised level without a balancing supply. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-028"></a>

<a id="sequence-task-581"></a>

- [ ] **EL-028 · Buoyant platform.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-339). Loaded floating deck carries physical cargo with displacement limits.

**Successful outcome:** Overload or lost water removes support. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-029"></a>

<a id="sequence-task-582"></a>

- [ ] **EL-029 · Boat.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-339). Hull displaces liquid and transports its actual cargo.

**Successful outcome:** Leak or overload changes flotation; no authored route animation. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-030"></a>

<a id="sequence-task-583"></a>

- [ ] **EL-030 · Flow meter.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-339). Measures actual volume per simulation time through its ports.

**Successful outcome:** Stopped flow reads zero despite upstream stored volume. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-031"></a>

<a id="sequence-task-584"></a>

- [ ] **EL-031 · Volume meter.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-339). Integrates actual transported liquid volume.

**Successful outcome:** Repeated inspection does not count the same volume twice. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-032"></a>

<a id="sequence-task-585"></a>

- [ ] **EL-032 · Pressure meter.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-339). Measures local supported fluid pressure.

**Successful outcome:** Disconnected or depressurised port cannot report supplied pressure. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-033"></a>

<a id="sequence-task-586"></a>

- [ ] **EL-033 · Fluid accumulator.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-340). Stores a finite fluid charge against a compliant boundary.

**Successful outcome:** Discharging lowers stored energy; no free recharge. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-034"></a>

<a id="sequence-task-587"></a>

- [ ] **EL-034 · Sponge.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-340). Absorbs available liquid up to material capacity.

**Successful outcome:** Saturated sponge cannot remove additional volume. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-035"></a>

<a id="sequence-task-588"></a>

- [ ] **EL-035 · Wick.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-340). Transports liquid through a declared porous medium under bounded capillary rules.

**Successful outcome:** Dry reservoir stops transport; mass remains accounted for. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-036"></a>

<a id="sequence-task-589"></a>

- [ ] **EL-036 · Sprinkler.** **Type:** Water. **Scope/evidence source:** [existing record](#todo-340). Supplied pressure divides liquid into a bounded spray footprint.

**Successful outcome:** Blocked supply prevents spray and off-footprint receivers remain dry. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-037"></a>

<a id="sequence-task-590"></a>

- [ ] **EL-037 · Air compressor.** **Type:** Pneumatic. **Scope/evidence source:** [existing record](#todo-376). External work raises pressure in a connected finite gas volume.

**Successful outcome:** Unpowered compressor cannot increase stored gas energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-038"></a>

<a id="sequence-task-591"></a>

- [ ] **EL-038 · Pneumatic hose.** **Type:** Pneumatic. **Scope/evidence source:** [existing record](#todo-376). Connects compatible gas ports with finite capacity and resistance.

**Successful outcome:** Open or disconnected hose vents or isolates according to its boundary declaration. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-039"></a>

<a id="sequence-task-592"></a>

- [ ] **EL-039 · Air reservoir.** **Type:** Pneumatic. **Scope/evidence source:** [existing record](#todo-376). Stores finite gas mass and internal energy in a declared volume.

**Successful outcome:** Repeated strokes deplete pressure. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-040"></a>

<a id="sequence-task-593"></a>

- [ ] **EL-040 · Pneumatic release valve.** **Type:** Pneumatic. **Scope/evidence source:** [existing record](#todo-376). Opens a controlled gas path to a declared output or exhaust.

**Successful outcome:** Closed valve cannot send a command-shaped free pressure pulse. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-041"></a>

<a id="sequence-task-594"></a>

- [ ] **EL-041 · Pneumatic directional valve.** **Type:** Pneumatic. **Scope/evidence source:** [existing record](#todo-376). Routes supply and exhaust to selected actuator ports.

**Successful outcome:** Blocked exhaust changes motion; opposite routes cannot both receive full independent supply. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-042"></a>

<a id="sequence-task-595"></a>

- [ ] **EL-042 · Air nozzle.** **Type:** Pneumatic. **Scope/evidence source:** [existing record](#todo-376). Expands supplied gas into a bounded directed jet.

**Successful outcome:** Insufficient pressure cannot move a distant load. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-043"></a>

<a id="sequence-task-596"></a>

- [ ] **EL-043 · Pneumatic pressure gauge.** **Type:** Pneumatic. **Scope/evidence source:** [existing record](#todo-376). Measures local gas pressure independently of electrical controls.

**Successful outcome:** Empty reservoir reads ambient rather than a stale charged state. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-044"></a>

<a id="sequence-task-597"></a>

- [ ] **EL-044 · Tone-selective sound meter.** **Type:** Sound. **Scope/evidence source:** [existing record](#todo-352). Separately supplied sensor selects a declared frequency band.

**Successful outcome:** Equal-strength wrong-tone pulse does not close the contact. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-045"></a>

<a id="sequence-task-598"></a>

- [ ] **EL-045 · Air whistle.** **Type:** Sound. **Scope/evidence source:** [existing record](#todo-352). Actual gas flow excites an acoustic source.

**Successful outcome:** Blocked or absent flow produces no sound event. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-046"></a>

<a id="sequence-task-599"></a>

- [ ] **EL-046 · Listening horn.** **Type:** Sound. **Scope/evidence source:** [existing record](#todo-352). Finite aperture couples incident acoustic energy into a duct.

**Successful outcome:** Wrong-facing or occluded arrivals lose coupling. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-047"></a>

<a id="sequence-task-600"></a>

- [ ] **EL-047 · Exit horn.** **Type:** Sound. **Scope/evidence source:** [existing record](#todo-352). Converts incoming duct energy into a directed acoustic field.

**Successful outcome:** Disconnected inlet emits nothing. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-048"></a>

<a id="sequence-task-601"></a>

- [ ] **EL-048 · Acoustic duct.** **Type:** Sound. **Scope/evidence source:** [existing record](#todo-352). Routes bounded acoustic energy between compatible ports with travel and loss.

**Successful outcome:** Disconnected mouths cannot carry a hidden signal. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-049"></a>

<a id="sequence-task-602"></a>

- [ ] **EL-049 · Acoustic resonator.** **Type:** Sound. **Scope/evidence source:** [existing record](#todo-352). Stores and dissipates bounded acoustic excitation around a declared band.

**Successful outcome:** Off-band pulses do not accumulate the same response. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-050"></a>

<a id="sequence-task-603"></a>

- [ ] **EL-050 · Acoustic screen.** **Type:** Sound. **Scope/evidence source:** [existing record](#todo-352). Material surface attenuates or reflects sound according to its declared properties.

**Successful outcome:** A geometric gap permits transmission; no universal mute field. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-051"></a>

<a id="sequence-task-604"></a>

- [ ] **EL-051 · Acoustic dish.** **Type:** Sound. **Scope/evidence source:** [existing record](#todo-352). Curved surface redirects acoustic energy through real coverage.

**Successful outcome:** Misalignment misses the listener and cannot amplify total energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-052"></a>

<a id="sequence-task-605"></a>

- [ ] **EL-052 · Water-tuned bottle.** **Type:** Sound. **Scope/evidence source:** [existing record](#todo-352). Conserved fill volume alters a resonant cavity's tone.

**Successful outcome:** Changing fill changes supported tone; dry and filled cases remain distinct. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-053"></a>

<a id="sequence-task-606"></a>

- [ ] **EL-053 · Rotary-to-linear converter.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-211). Converts shaft work into bounded translation through a declared linkage.

**Successful outcome:** Blocked output loads the input; no command-only displacement. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-054"></a>

<a id="sequence-task-607"></a>

- [ ] **EL-054 · Linear-to-rotary converter.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-211). Converts actual translation and force into shaft work.

**Successful outcome:** An unmoving input supplies no rotation energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-055"></a>

<a id="sequence-task-608"></a>

- [ ] **EL-055 · Mechanical brake.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-375). Dissipative contact restrains a supplied shaft or carrier.

**Successful outcome:** Released brake permits motion; restraint cannot generate shaft work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-056"></a>

<a id="sequence-task-609"></a>

- [ ] **EL-056 · Ratchet.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-375). Directional teeth permit motion one way and constrain reversal.

**Successful outcome:** Reverse load holds within capacity without inventing forward movement. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-057"></a>

<a id="sequence-task-610"></a>

- [ ] **EL-057 · Escapement.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-375). Intermittently releases a loaded mechanism in discrete physical steps.

**Successful outcome:** No stored drive means no step despite a release trigger. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-058"></a>

<a id="sequence-task-611"></a>

- [ ] **EL-058 · Size grate.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-320). Real openings separate physical cargo by supported dimensions.

**Successful outcome:** Oversized object remains blocked; identity labels do not select passage. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-059"></a>

<a id="sequence-task-612"></a>

- [ ] **EL-059 · Weight tray.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-320). Loaded support exposes measured force or threshold state.

**Successful outcome:** An object beside the tray cannot contribute weight. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-060"></a>

<a id="sequence-task-613"></a>

- [ ] **EL-060 · Electromagnet.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-320). Supplied field attracts material through generic magnetic response.

**Successful outcome:** Nonresponsive material and absent supply do not satisfy pickup. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-061"></a>

<a id="sequence-task-614"></a>

- [ ] **EL-061 · Indexed carousel.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-320). A finite-work drive advances a carrier through indexed positions.

**Successful outcome:** Obstruction or missing power prevents the completed index. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-062"></a>

<a id="sequence-task-615"></a>

- [ ] **EL-062 · Docking ferry.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-320). A constrained carrier physically transfers cargo between declared docks.

**Successful outcome:** Undocked loading interlock does not bypass actual support. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-063"></a>

<a id="sequence-task-616"></a>

- [ ] **EL-063 · Cable winch.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-320). Shaft work winds a finite cable path and lifts its attached load.

**Successful outcome:** Stall and reverse load debit work; no instantaneous rope shortening. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-064"></a>

<a id="sequence-task-617"></a>

- [ ] **EL-064 · Metal loop anchor.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-193). Fixed socket anchors a supported rope endpoint.

**Successful outcome:** Disconnected rope carries no anchored tension. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-065"></a>

<a id="sequence-task-618"></a>

- [ ] **EL-065 · Load hook.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-193). Attachable hook transmits load through actual supported engagement.

**Successful outcome:** Unengaged hook cannot carry a remote body. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-066"></a>

<a id="sequence-task-619"></a>

- [ ] **EL-066 · Scissors.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-193). Closing blades sever compatible material through a generic cutting-work/material criterion.

**Successful outcome:** Wrong placement or insufficient cutting work leaves it intact. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-067"></a>

<a id="sequence-task-620"></a>

- [ ] **EL-067 · Steel cable.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-210). Tension-only connector declares stiffness and cutting resistance as material properties.

**Successful outcome:** Compression cannot push a load. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-068"></a>

<a id="sequence-task-621"></a>

- [ ] **EL-068 · Tin snips.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-210). Powered or actuated blades deliver a declared cutting-work range.

**Successful outcome:** Insufficient work fails on resistant material; no cable-name special case. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-069"></a>

<a id="sequence-task-622"></a>

- [ ] **EL-069 · Moving bucket.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-192). Rigid open container carries captured physical cargo with its mass.

**Successful outcome:** Tipping spills through the real opening; a missed object is not captured. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-070"></a>

<a id="sequence-task-623"></a>

- [ ] **EL-070 · Moving cage.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-192). Rigid barred enclosure carries cargo through a declared closable opening.

**Successful outcome:** An open door allows escape through actual geometry. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-071"></a>

<a id="sequence-task-624"></a>

- [ ] **EL-071 · Straight metal ball pipe.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-199). Hollow rigid conduit routes compatible physical balls.

**Successful outcome:** Oversized cargo jams rather than traversing a logical connection. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-072"></a>

<a id="sequence-task-625"></a>

- [ ] **EL-072 · Curved metal ball pipe.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-199). Curved hollow conduit changes direction through real collision surfaces.

**Successful outcome:** Fast and queued cargo remain conserved at seams. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-073"></a>

<a id="sequence-task-626"></a>

- [ ] **EL-073 · Brick barrier.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-199). Rigid material obstacle supplies declared contact and strength properties.

**Successful outcome:** No passage through intact geometry. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-074"></a>

<a id="sequence-task-627"></a>

- [ ] **EL-074 · Wood barrier.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-199). Rigid material obstacle uses independently declared contact and combustible properties.

**Successful outcome:** Mechanical contact cannot silently use brick parameters. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-075"></a>

<a id="sequence-task-628"></a>

- [ ] **EL-075 · Toy pulse emitter.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-214). Separate supply and control emit an authored bounded number of pulses.

**Successful outcome:** Unpowered request emits none; held input follows the explicit retrigger rule. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-076"></a>

<a id="sequence-task-629"></a>

- [ ] **EL-076 · Programmable ball.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-217). Author-selected validated mass, size and material preset defines physical behaviour.

**Successful outcome:** Unsupported combinations are rejected; no runtime numeric solution editor. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-077"></a>

<a id="sequence-task-630"></a>

- [ ] **EL-077 · Soccer ball.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-217). Distinct lightweight ball uses measured authored contact properties.

**Successful outcome:** Its bounce and load differ reproducibly from a heavy ball. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-078"></a>

<a id="sequence-task-631"></a>

- [ ] **EL-078 · Can opener.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-218). Supplied tool progressively cuts a compatible container seam using generic cutting work.

**Successful outcome:** Misalignment and missing supply leave the seal intact. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-079"></a>

<a id="sequence-task-632"></a>

- [ ] **EL-079 · Electric mixer.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-218). Supplied rotating tool moves material through actual torque and contact.

**Successful outcome:** Stall or missing contents cannot create a processed result. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-080"></a>

<a id="sequence-task-633"></a>

- [ ] **EL-080 · Programmable box.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-222). Author-defined validated size, mass and material form a physical container.

**Successful outcome:** Contents and collisions respond to its actual geometry. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-081"></a>

<a id="sequence-task-634"></a>

- [ ] **EL-081 · Message display.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-222). Supplied display renders an explicitly received typed state or count.

**Successful outcome:** Unconnected inputs cannot display a success event. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-082"></a>

<a id="sequence-task-635"></a>

- [ ] **EL-082 · Lured character.** **Type:** Character. **Scope/evidence source:** [existing record](#todo-200). Original character chooses motion from generic perceptible stimulus and locomotion rules.

**Successful outcome:** Occluded or unsupported stimulus does not trigger pursuit. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-083"></a>

<a id="sequence-task-636"></a>

- [ ] **EL-083 · Escaping character.** **Type:** Character. **Scope/evidence source:** [existing record](#todo-200). Original character chooses a bounded avoidance route from perceived hazards.

**Successful outcome:** Absent or blocked perception does not trigger a hidden scripted escape. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-084"></a>

<a id="sequence-task-637"></a>

- [ ] **EL-084 · Fragile fish bowl.** **Type:** Character. **Scope/evidence source:** [existing record](#todo-200). Contained fragile payload uses generic impact failure and containment.

**Successful outcome:** Subthreshold impact retains contents. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-085"></a>

<a id="sequence-task-638"></a>

- [ ] **EL-085 · Rope-driven character wheel.** **Type:** Character. **Scope/evidence source:** [existing record](#todo-200). Rope work drives a visible carrier mechanism under load.

**Successful outcome:** Slack rope supplies no work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-086"></a>

<a id="sequence-task-639"></a>

- [ ] **EL-086 · Obstacle-reversing walker.** **Type:** Character. **Scope/evidence source:** [existing record](#todo-221). Locomotion reverses after sensed actual obstruction.

**Successful outcome:** Distant obstacle does not reverse it early. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-087"></a>

<a id="sequence-task-640"></a>

- [ ] **EL-087 · Predator character.** **Type:** Character. **Scope/evidence source:** [existing record](#todo-221). Original character responds to declared perceivable stimuli and reachable routes.

**Successful outcome:** A barrier prevents traversal; target names cannot bypass sensing. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-088"></a>

<a id="sequence-task-641"></a>

- [ ] **EL-088 · Fish tank lure.** **Type:** Character. **Scope/evidence source:** [existing record](#todo-221). Fragile enclosure presents declared sensory stimulus while containing its payload.

**Successful outcome:** Occlusion affects perception and intact walls retain contents. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-089"></a>

<a id="sequence-task-642"></a>

- [ ] **EL-089 · Gravity-effect pad.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-216). Bounded authored field changes acceleration inside its declared volume.

**Successful outcome:** Outside bodies retain world gravity; label the field as deliberate game fiction. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-090"></a>

<a id="sequence-task-643"></a>

- [ ] **EL-090 · Steerable blimp.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-216). Buoyant body combines gas state with a bounded supplied directional drive.

**Successful outcome:** Unpowered drive cannot steer against a current. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-091"></a>

<a id="sequence-task-644"></a>

- [ ] **EL-091 · Cannonball.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-197). Heavy spherical cargo declares material and mass independently of launcher identity.

**Successful outcome:** Different launchers move the same body through generic contact and work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-092"></a>

<a id="sequence-task-645"></a>

- [ ] **EL-092 · Toy rocket.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-197). Finite stored propellant/energy produces bounded thrust and exhaust.

**Successful outcome:** Spent rocket produces no further thrust; abstract toy parameters only. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-093"></a>

<a id="sequence-task-646"></a>

- [ ] **EL-093 · Toy dynamite charge.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-197). Finite toy energy store releases one declared pressure impulse when triggered.

**Successful outcome:** Spent charge cannot repeat; no real formulation or construction recipe. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-094"></a>

<a id="sequence-task-647"></a>

- [ ] **EL-094 · Detonation plunger.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-197). Mechanical input emits a typed control command after actual stroke.

**Successful outcome:** Command supplies no explosion energy of its own. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-095"></a>

<a id="sequence-task-648"></a>

- [ ] **EL-095 · Toy revolver.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-197). Finite toy launcher supplies bounded work to declared physical payloads.

**Successful outcome:** Empty or uncharged state cannot manufacture a projectile. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-096"></a>

<a id="sequence-task-649"></a>

- [ ] **EL-096 · Tipsy platform.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-219). Offset supported load changes equilibrium and causes physical tipping.

**Successful outcome:** Balanced or constrained platform does not tip on a timer. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-097"></a>

<a id="sequence-task-650"></a>

- [ ] **EL-097 · Pool cue.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Contact actuator delivers bounded work to an actual ball.

**Successful outcome:** A miss cannot move its target. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-098"></a>

<a id="sequence-task-651"></a>

- [ ] **EL-098 · Pool ball.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Authored ball material and planar-table constraints define rolling behaviour.

**Successful outcome:** No-gravity historical variant, if adopted, requires a separate explicitly fictional field rule. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-099"></a>

<a id="sequence-task-652"></a>

- [ ] **EL-099 · Pool pocket.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Real opening captures cargo through actual passage.

**Successful outcome:** A ball beside the opening is not pocketed. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-100"></a>

<a id="sequence-task-653"></a>

- [ ] **EL-100 · Vacuum nozzle.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Supplied pressure/airflow draws responsive material through a finite opening.

**Successful outcome:** Blocked inlet or missing supply prevents suction. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-101"></a>

<a id="sequence-task-654"></a>

- [ ] **EL-101 · Thumb tack.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Sharp-contact geometry punctures material through a generic failure threshold.

**Successful outcome:** Missed contact or puncture-resistant material remains intact. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-102"></a>

<a id="sequence-task-655"></a>

- [ ] **EL-102 · Large-bore ball pipe.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Scaled physical bore supports correspondingly larger cargo.

**Successful outcome:** Too-large cargo remains blocked; size is validated rather than guessed from art. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-103"></a>

<a id="sequence-task-656"></a>

- [ ] **EL-103 · Accelerator tube.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Explicit supply adds bounded work through a declared coupling field.

**Successful outcome:** Unpowered tube cannot increase cargo energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-104"></a>

<a id="sequence-task-657"></a>

- [ ] **EL-104 · Toy firework.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Finite abstract toy charge creates bounded flight and an authored visual/event release.

**Successful outcome:** Spent charge does not retrigger; no real pyrotechnic instructions. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-105"></a>

<a id="sequence-task-658"></a>

- [ ] **EL-105 · Toy missile.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Guided toy projectile follows only supported sensing and finite actuation.

**Successful outcome:** No supply or lost guidance cannot yield unlimited corrective thrust. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-106"></a>

<a id="sequence-task-659"></a>

- [ ] **EL-106 · Impact-sensitive toy charge.** **Type:** Specialist. **Scope/evidence source:** [existing record](#todo-227). Finite abstract stored energy reacts to a generic shock threshold.

**Successful outcome:** Subthreshold contact leaves it unspent; historical nitroglycerine is a reference only. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-107"></a>

<a id="sequence-task-660"></a>

- [ ] **EL-107 · Spiral gravity-delay tube.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-248). Long physical descent delays arrival through travel distance and contact.

**Successful outcome:** Changing speed changes transit time; no precise hidden timer. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-108"></a>

<a id="sequence-task-661"></a>

- [ ] **EL-108 · Powered airlift.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-248). Supplied airflow lifts compatible cargo through a physical passage.

**Successful outcome:** Insufficient airflow cannot lift the load. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-109"></a>

<a id="sequence-task-662"></a>

- [ ] **EL-109 · Speed-sensitive trapdoor.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-248). Physical sensor and supplied latch distinguish taught incoming speed.

**Successful outcome:** Equal-size slow control stays on its declared route. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-110"></a>

<a id="sequence-task-663"></a>

- [ ] **EL-110 · Flywheel.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-377). Rotating inertia stores work and coasts under load.

**Successful outcome:** Stored energy falls as output work is delivered. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-111"></a>

<a id="sequence-task-664"></a>

- [ ] **EL-111 · Centrifugal governor.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-377). Rotating masses displace a linkage according to actual speed.

**Successful outcome:** Stationary shaft does not actuate it. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-112"></a>

<a id="sequence-task-665"></a>

- [ ] **EL-112 · Structural beam.** **Type:** Construction. **Scope/evidence source:** [existing record](#gap-01). Bounded rigid member transfers load through declared attachment sockets.

**Successful outcome:** Unsupported member falls rather than floating at authored endpoints. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-113"></a>

<a id="sequence-task-666"></a>

- [ ] **EL-113 · Structural brace.** **Type:** Construction. **Scope/evidence source:** [existing record](#gap-01). Diagonal member constrains frame distortion through actual geometry and load.

**Successful outcome:** Removing the brace changes stability; no frame-name bonus. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-114"></a>

<a id="sequence-task-667"></a>

- [ ] **EL-114 · Placeable pivot.** **Type:** Construction. **Scope/evidence source:** [existing record](#gap-02). Axis joint constrains translation while allowing declared rotation.

**Successful outcome:** Off-axis load is solved by the shared joint; no animation path. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-115"></a>

<a id="sequence-task-668"></a>

- [ ] **EL-115 · Linkage connector.** **Type:** Construction. **Scope/evidence source:** [existing record](#gap-02). Finite-length member couples declared body attachment points.

**Successful outcome:** Disconnected endpoint cannot transmit motion. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-116"></a>

<a id="sequence-task-669"></a>

- [ ] **EL-116 · Passive wheel.** **Type:** Construction. **Scope/evidence source:** [existing record](#gap-03). Round body rolls according to friction and applied load.

**Successful outcome:** No hidden drive on level ground. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-117"></a>

<a id="sequence-task-670"></a>

- [ ] **EL-117 · Axle.** **Type:** Construction. **Scope/evidence source:** [existing record](#gap-03). Rotating support couples a wheel to the chassis with declared freedom.

**Successful outcome:** Locked axle prevents the formerly permitted rotation. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-118"></a>

<a id="sequence-task-671"></a>

- [ ] **EL-118 · Coating station.** **Type:** Material. **Scope/evidence source:** [existing record](#gap-08). Finite contact-transferred material forms a declared surface layer.

**Successful outcome:** Absent supply or missed contact leaves cargo untreated. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-119"></a>

<a id="sequence-task-672"></a>

- [ ] **EL-119 · Dye station.** **Type:** Material. **Scope/evidence source:** [existing record](#gap-08). Finite pigment transport changes a supported material's colour state.

**Successful outcome:** No pigment means no colour change; optical illumination is not dye. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-120"></a>

<a id="sequence-task-673"></a>

- [ ] **EL-120 · Quantity goal.** **Type:** Goal. **Scope/evidence source:** [existing record](#gap-16). Counts distinct supported deliveries in explicit units.

**Successful outcome:** Repeated sensing of one retained item cannot inflate totals. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-121"></a>

<a id="sequence-task-674"></a>

- [ ] **EL-121 · Rate-window goal.** **Type:** Goal. **Scope/evidence source:** [existing record](#gap-16). Measures throughput over an explicit simulation-time interval.

**Successful outcome:** An instantaneous burst outside the window fails. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-122"></a>

<a id="sequence-task-675"></a>

- [ ] **EL-122 · Ordered-events goal.** **Type:** Goal. **Scope/evidence source:** [existing record](#gap-16). Tracks declared event identities in a required sequence.

**Successful outcome:** Wrong order does not satisfy the sequence. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-123"></a>

<a id="sequence-task-676"></a>

- [ ] **EL-123 · Protected-end-state goal.** **Type:** Goal. **Scope/evidence source:** [existing record](#gap-16). Requires the observed final machine state to remain within declared bounds.

**Successful outcome:** Delivery alone cannot bypass an unsafe or unfinished terminal state. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-124"></a>

<a id="sequence-task-677"></a>

- [ ] **EL-124 · Authored gravity preset.** **Type:** Environment. **Scope/evidence source:** [existing record](#gap-18). Validated world acceleration is visible and fixed for a Run.

**Successful outcome:** Save and replay preserve it independently of difficulty. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-125"></a>

<a id="sequence-task-678"></a>

- [ ] **EL-125 · Authored atmosphere preset.** **Type:** Environment. **Scope/evidence source:** [existing record](#gap-18). Supported gas boundary parameters set actual pressure and density.

**Successful outcome:** Unsupported presets are rejected; a visual sky change alone cannot affect physics. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-126"></a>

<a id="sequence-task-679"></a>

- [ ] **EL-126 · Thin-screen shield.** **Type:** Radiation. **Scope/evidence source:** [existing record](#radiation-05). Declared material attenuation and mass form a thin physical panel.

**Successful outcome:** Gaps transmit; supported alpha/beta/photon responses follow shared transport coefficients. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-127"></a>

<a id="sequence-task-680"></a>

- [ ] **EL-127 · Polymer shield.** **Type:** Radiation. **Scope/evidence source:** [existing record](#radiation-05). Distinct material coefficients and density govern radiation transport.

**Successful outcome:** It cannot inherit dense-shield performance from its category. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-128"></a>

<a id="sequence-task-681"></a>

- [ ] **EL-128 · Dense shield.** **Type:** Radiation. **Scope/evidence source:** [existing record](#radiation-05). Dense material attenuates supported radiation through actual path length.

**Successful outcome:** Increasing thickness cannot increase passive transmitted energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-129"></a>

<a id="sequence-task-682"></a>

- [ ] **EL-129 · Sheet-thickness transmission gauge.** **Type:** Radiation. **Scope/evidence source:** [existing record](#radiation-11). Supplied calibrated detector estimates supported sheet thickness from transmission.

**Successful outcome:** Missing source is invalid rather than maximum thickness. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-130"></a>

<a id="sequence-task-683"></a>

- [ ] **EL-130 · Tank-level transmission gauge.** **Type:** Radiation. **Scope/evidence source:** [existing record](#radiation-11). Supplied calibrated detector observes attenuation across actual fill height.

**Successful outcome:** Empty and full readings follow the material path; disconnected source is invalid. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-131"></a>

<a id="sequence-task-684"></a>

- [ ] **EL-131 · Fast-neutron detector.** **Type:** Radiation. **Scope/evidence source:** [existing record](#radiation-19). Supplied detector applies declared fast-group response.

**Successful outcome:** Slow-group and photon controls cannot masquerade as fast flux. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-132"></a>

<a id="sequence-task-685"></a>

- [ ] **EL-132 · Slow-neutron detector.** **Type:** Radiation. **Scope/evidence source:** [existing record](#radiation-19). Supplied detector applies declared slow-group response.

**Successful outcome:** Unmoderated fast flux does not silently count as slow. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-133"></a>

<a id="sequence-task-686"></a>

- [ ] **EL-133 · Electrical AND gate.** **Type:** Electrical. **Scope/evidence source:** [existing record](#todo-364). Separately supplied electrical output follows both inputs present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-134"></a>

<a id="sequence-task-687"></a>

- [ ] **EL-134 · Electrical OR gate.** **Type:** Electrical. **Scope/evidence source:** [existing record](#todo-364). Separately supplied electrical output follows at least one input present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-135"></a>

<a id="sequence-task-688"></a>

- [ ] **EL-135 · Electrical XOR gate.** **Type:** Electrical. **Scope/evidence source:** [existing record](#todo-364). Separately supplied electrical output follows exactly one input present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-136"></a>

<a id="sequence-task-689"></a>

- [ ] **EL-136 · Electrical NOR gate.** **Type:** Electrical. **Scope/evidence source:** [existing record](#todo-364). Separately supplied electrical output follows neither input present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-137"></a>

<a id="sequence-task-690"></a>

- [ ] **EL-137 · Electrical NAND gate.** **Type:** Electrical. **Scope/evidence source:** [existing record](#todo-364). Separately supplied electrical output follows not both inputs present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-138"></a>

<a id="sequence-task-691"></a>

- [ ] **EL-138 · Optical AND gate.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-367). Separately supplied carrier output follows both inputs present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-139"></a>

<a id="sequence-task-692"></a>

- [ ] **EL-139 · Optical OR gate.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-367). Separately supplied carrier output follows at least one input present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-140"></a>

<a id="sequence-task-693"></a>

- [ ] **EL-140 · Optical XOR gate.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-367). Separately supplied carrier output follows exactly one input present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-141"></a>

<a id="sequence-task-694"></a>

- [ ] **EL-141 · Optical NOR gate.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-367). Separately supplied carrier output follows neither input present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-142"></a>

<a id="sequence-task-695"></a>

- [ ] **EL-142 · Optical NAND gate.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-367). Separately supplied carrier output follows not both inputs present; A/B inputs carry conditions, not output energy.

**Successful outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-143"></a>

<a id="sequence-task-696"></a>

- [ ] **EL-143 · Red optical filter.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-297). Passes only the existing red channel through its finite aperture with declared loss.

**Successful outcome:** Absent red input cannot be recoloured into red; opaque frame blocks rays. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-144"></a>

<a id="sequence-task-697"></a>

- [ ] **EL-144 · Green optical filter.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-297). Passes only the existing green channel through its finite aperture with declared loss.

**Successful outcome:** Absent green input cannot be recoloured into green; opaque frame blocks rays. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-145"></a>

<a id="sequence-task-698"></a>

- [ ] **EL-145 · Blue optical filter.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-297). Passes only the existing blue channel through its finite aperture with declared loss.

**Successful outcome:** Absent blue input cannot be recoloured into blue; opaque frame blocks rays. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-146"></a>

<a id="sequence-task-699"></a>

- [ ] **EL-146 · Red selective receiver.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-300). Separately supplied output requires red under the declared thresholds and channel-purity contract.

**Successful outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-147"></a>

<a id="sequence-task-700"></a>

- [ ] **EL-147 · Green selective receiver.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-300). Separately supplied output requires green under the declared thresholds and channel-purity contract.

**Successful outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-148"></a>

<a id="sequence-task-701"></a>

- [ ] **EL-148 · Blue selective receiver.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-300). Separately supplied output requires blue under the declared thresholds and channel-purity contract.

**Successful outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-149"></a>

<a id="sequence-task-702"></a>

- [ ] **EL-149 · Yellow selective receiver.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-301). Separately supplied output requires red and green under the declared thresholds and channel-purity contract.

**Successful outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-150"></a>

<a id="sequence-task-703"></a>

- [ ] **EL-150 · Cyan selective receiver.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-301). Separately supplied output requires green and blue under the declared thresholds and channel-purity contract.

**Successful outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-151"></a>

<a id="sequence-task-704"></a>

- [ ] **EL-151 · Magenta selective receiver.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-301). Separately supplied output requires red and blue under the declared thresholds and channel-purity contract.

**Successful outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-152"></a>

<a id="sequence-task-705"></a>

- [ ] **EL-152 · White selective receiver.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-301). Separately supplied output requires red, green and blue under the declared thresholds and channel-purity contract.

**Successful outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-153"></a>

<a id="sequence-task-706"></a>

- [ ] **EL-153 · General-light receiver.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-291). Samples actual finite-width illumination over a declared aperture.

**Successful outcome:** Occluded flashlight cone and absent independent supply cannot activate output. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-154"></a>

<a id="sequence-task-707"></a>

- [ ] **EL-154 · Light-charge receiver.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-291). Integrates absorbed optical energy into a bounded store with declared loss.

**Successful outcome:** Two bursts accumulate correctly; dark time cannot create charge. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-155"></a>

<a id="sequence-task-708"></a>

- [ ] **EL-155 · Rope-operated light.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-196). Real rope displacement actuates a separately supplied light source.

**Successful outcome:** Slack rope or missing supply produces no light. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-156"></a>

<a id="sequence-task-709"></a>

- [ ] **EL-156 · Single-lobe cam.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-424). One geometric lobe drives one follower lift per revolution.

**Successful outcome:** Partial rotation yields only its actual profile displacement. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-157"></a>

<a id="sequence-task-710"></a>

- [ ] **EL-157 · Double-lobe cam.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-424). Two geometric lobes drive two separately observable lifts per revolution.

**Successful outcome:** One revolution cannot be recorded as a single-lobe cycle. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-158"></a>

<a id="sequence-task-711"></a>

- [ ] **EL-158 · Rise-hold-fall cam.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-424). A declared dwell sector physically retains follower height.

**Successful outcome:** Changing shaft speed changes dwell time; no hidden timer. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-159"></a>

<a id="sequence-task-712"></a>

- [ ] **EL-159 · Tension-limited connector.** **Type:** Construction. **Scope/evidence source:** [existing record](#gap-05). Declared tensile strength governs failure under axial load.

**Successful outcome:** Subthreshold tension retains the joint. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-160"></a>

<a id="sequence-task-713"></a>

- [ ] **EL-160 · Shear-limited connector.** **Type:** Construction. **Scope/evidence source:** [existing record](#gap-05). Declared transverse strength governs failure under shear load.

**Successful outcome:** Pure supported axial load does not silently count as shear. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-161"></a>

<a id="sequence-task-714"></a>

- [ ] **EL-161 · Bending-limited connector.** **Type:** Construction. **Scope/evidence source:** [existing record](#gap-05). Declared moment capacity governs failure under bending.

**Successful outcome:** Subthreshold moment retains the joint regardless of display animation. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-162"></a>

<a id="sequence-task-715"></a>

- [ ] **EL-162 · Fixed ball diverter.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-246). Player-set blade routes actual balls into one chosen branch.

**Successful outcome:** Wrong branch remains unavailable without physical leakage. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-163"></a>

<a id="sequence-task-716"></a>

- [ ] **EL-163 · Powered ball diverter.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-246). Finite-work actuator changes a routing blade after a command.

**Successful outcome:** Missing supply or a jam prevents instant route switching. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-164"></a>

<a id="sequence-task-717"></a>

- [ ] **EL-164 · Alternating ball diverter.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-246). Observed completed passage requests the next supplied blade position.

**Successful outcome:** Held arrival signal cannot alternate repeatedly without new passages. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-165"></a>

<a id="sequence-task-718"></a>

- [ ] **EL-165 · Manual tap.** **Type:** Water. **Scope/evidence source:** [existing record](#campaign-element-coverage). Player-selected aperture remains fixed during Run.

**Successful outcome:** Closed aperture stops flow; opening cannot supply water without a connected reservoir. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-166"></a>

<a id="sequence-task-719"></a>

- [ ] **EL-166 · Mechanically actuated tap.** **Type:** Water. **Scope/evidence source:** [existing record](#campaign-element-coverage). Physical linkage work changes the flow aperture.

**Successful outcome:** A slack or stalled linkage cannot change aperture. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-167"></a>

<a id="sequence-task-720"></a>

- [ ] **EL-167 · Solenoid tap.** **Type:** Water. **Scope/evidence source:** [existing record](#campaign-element-coverage). Independent supply moves a valve on a control input.

**Successful outcome:** A true command without supply cannot open the valve. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-168"></a>

<a id="sequence-task-721"></a>

- [ ] **EL-168 · Siphon priming bulb.** **Type:** Water. **Scope/evidence source:** [existing record](#campaign-element-coverage). Finite mechanical compression expels gas or transfers liquid to establish a supported liquid column.

**Successful outcome:** Insufficient priming leaves the siphon broken; no remote automatic fill. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-169"></a>

<a id="sequence-task-722"></a>

- [ ] **EL-169 · Cork float.** **Type:** Water. **Scope/evidence source:** [existing record](#campaign-element-coverage). Light material body displaces actual liquid under its measured load.

**Successful outcome:** Dry or overloaded cork cannot provide unlimited lift. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-170"></a>

<a id="sequence-task-723"></a>

- [ ] **EL-170 · Raft.** **Type:** Water. **Scope/evidence source:** [existing record](#campaign-element-coverage). Joined buoyant members support cargo through shared displacement and structure.

**Successful outcome:** Overload or separated members change stability. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-171"></a>

<a id="sequence-task-724"></a>

- [ ] **EL-171 · Squeeze pad.** **Type:** Water. **Scope/evidence source:** [existing record](#campaign-element-coverage). Mechanical compression expels retained liquid from a porous store.

**Successful outcome:** A dry pad cannot produce water and squeezing consumes work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-172"></a>

<a id="sequence-task-725"></a>

- [ ] **EL-172 · Rain collector.** **Type:** Water. **Scope/evidence source:** [existing record](#campaign-element-coverage). Open catch surface gathers only intersecting droplets into finite storage.

**Successful outcome:** An occluded or missed stream does not fill it. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-173"></a>

<a id="sequence-task-726"></a>

- [ ] **EL-173 · Diverging lens.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-305). Finite-aperture refraction broadens illumination while conserving transmitted power.

**Successful outcome:** Two receivers share the beam; each cannot obtain the unsplit full power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-174"></a>

<a id="sequence-task-727"></a>

- [ ] **EL-174 · Prism.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-306). Declared dispersion separates only spectral channels present in incident light.

**Successful outcome:** Red-only input creates no green or blue output. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-175"></a>

<a id="sequence-task-728"></a>

- [ ] **EL-175 · Optical fibre.** **Type:** Optical. **Scope/evidence source:** [existing record](#todo-307). Compatible endpoints carry bounded optical power with coupling loss and finite path length.

**Successful outcome:** Disconnected endpoints cannot bridge a gap. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-176"></a>

<a id="sequence-task-729"></a>

- [ ] **EL-176 · Red laser emitter.** **Type:** Optical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Supplied enabled emitter produces a bounded red optical channel.

**Successful outcome:** Missing supply or enable prevents output. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-177"></a>

<a id="sequence-task-730"></a>

- [ ] **EL-177 · Green laser emitter.** **Type:** Optical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Supplied enabled emitter produces a bounded green optical channel.

**Successful outcome:** Wrong-channel receiver control remains inactive. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-178"></a>

<a id="sequence-task-731"></a>

- [ ] **EL-178 · Blue laser emitter.** **Type:** Optical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Supplied enabled emitter produces a bounded blue optical channel.

**Successful outcome:** Blocked aperture prevents downstream illumination. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-179"></a>

<a id="sequence-task-732"></a>

- [ ] **EL-179 · Continuous-tone speaker.** **Type:** Sound. **Scope/evidence source:** [existing record](#campaign-element-coverage). Finite supplied power sustains a declared acoustic tone while enabled.

**Successful outcome:** Power loss stops excitation; stored resonance may decay only by its own state. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-180"></a>

<a id="sequence-task-733"></a>

- [ ] **EL-180 · Pulse speaker.** **Type:** Sound. **Scope/evidence source:** [existing record](#campaign-element-coverage). Supplied trigger releases a bounded acoustic pulse.

**Successful outcome:** Held input follows its edge contract and cannot create infinite pulse energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-181"></a>

<a id="sequence-task-734"></a>

- [ ] **EL-181 · Rising-edge detector.** **Type:** Control. **Scope/evidence source:** [existing record](#campaign-element-coverage). Observes a false-to-true transition and emits one typed event.

**Successful outcome:** Held true input produces no repeated transitions. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-182"></a>

<a id="sequence-task-735"></a>

- [ ] **EL-182 · Falling-edge detector.** **Type:** Control. **Scope/evidence source:** [existing record](#campaign-element-coverage). Observes a true-to-false transition and emits one typed event.

**Successful outcome:** Held false input produces no repeated transitions. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-183"></a>

<a id="sequence-task-736"></a>

- [ ] **EL-183 · Resettable counter.** **Type:** Control. **Scope/evidence source:** [existing record](#campaign-element-coverage). Counts distinct supported arrivals and accepts a separately addressed reset input.

**Successful outcome:** Reset clears the count without manufacturing a new arrival. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-184"></a>

<a id="sequence-task-737"></a>

- [ ] **EL-184 · Mechanical ball gate.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Actual linkage work moves a collision blade controlling a conduit opening.

**Successful outcome:** Slack linkage or blocked blade prevents instantaneous release. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-185"></a>

<a id="sequence-task-738"></a>

- [ ] **EL-185 · Releasable assembly joint.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-321). Typed release removes a specific physical attachment with conserved momentum.

**Successful outcome:** Unreleased attachment retains load; unsupported command is rejected. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-186"></a>

<a id="sequence-task-739"></a>

- [ ] **EL-186 · Temporary bridge.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#todo-321). Assembled load-bearing crossing loses support only when its declared attachment releases or material fails.

**Successful outcome:** A release signal cannot move unrelated structures. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-187"></a>

<a id="sequence-task-740"></a>

- [ ] **EL-187 · Basketball.** **Type:** Gravity. **Scope/evidence source:** [existing record](#campaign-element-coverage). Spherical cargo uses declared mass, radius and compliant contact properties.

**Successful outcome:** Equal drop height produces reproducible material-dependent rebound. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-188"></a>

<a id="sequence-task-741"></a>

- [ ] **EL-188 · Bowling ball.** **Type:** Gravity. **Scope/evidence source:** [existing record](#campaign-element-coverage). Heavy spherical cargo loads supports according to actual mass.

**Successful outcome:** Its visual size alone cannot supply added work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-189"></a>

<a id="sequence-task-742"></a>

- [ ] **EL-189 · Tennis ball.** **Type:** Gravity. **Scope/evidence source:** [existing record](#campaign-element-coverage). Light compliant spherical cargo retains distinct calibrated contact properties.

**Successful outcome:** Its bounce differs from the bowling ball because of declared material, not IDs. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-190"></a>

<a id="sequence-task-743"></a>

- [ ] **EL-190 · Ramp.** **Type:** Gravity. **Scope/evidence source:** [existing record](#campaign-element-coverage). Inclined rigid support guides motion through contact and gravity.

**Successful outcome:** Wrong-facing slope cannot accelerate cargo uphill. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-191"></a>

<a id="sequence-task-744"></a>

- [ ] **EL-191 · Wall.** **Type:** Gravity. **Scope/evidence source:** [existing record](#campaign-element-coverage). Rigid barrier blocks bodies through actual transformed geometry.

**Successful outcome:** A geometric gap allows passage; invisible silhouette bounds do not block it. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-192"></a>

<a id="sequence-task-745"></a>

- [ ] **EL-192 · Receiving basket.** **Type:** Gravity. **Scope/evidence source:** [existing record](#campaign-element-coverage). Physical open container supports cargo and exposes an actual containment goal.

**Successful outcome:** A ball beside or passing over it does not satisfy capture. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-193"></a>

<a id="sequence-task-746"></a>

- [ ] **EL-193 · Domino.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Finite rigid body tips through real contact, mass and centre of gravity.

**Successful outcome:** Separated neighbour does not fall from a scripted propagation event. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-194"></a>

<a id="sequence-task-747"></a>

- [ ] **EL-194 · Springboard.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Declared finite elastic store transfers work through physical compression and release.

**Successful outcome:** Uncharged or missed contact cannot impart a free launch. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

Use the complete [springboard model and parameter contract](../springboard-elastic-contract.md). Qualify passive/precharged contact, typed signal integration, no-spring/disconnected/missed controls, all fifteen composite lane instances, parameter editing and exact Reset/save, along with workload/device and element publication gates.

<a id="element-195"></a>

<a id="sequence-task-748"></a>

- [ ] **EL-195 · Pinball bumper.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Bounded supplied or explicitly charged contact actuator repels touching cargo.

**Successful outcome:** No available energy or missed contact means no powered kick. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-196"></a>

<a id="sequence-task-749"></a>

- [ ] **EL-196 · Battery.** **Type:** Electrical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Finite declared electrical store supplies connected loads through bounded energy and power.

**Successful outcome:** Disconnected or depleted source cannot operate a motor. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-197"></a>

<a id="sequence-task-750"></a>

- [ ] **EL-197 · Electrical wire.** **Type:** Electrical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Compatible terminals carry power or control according to their distinct typed contracts.

**Successful outcome:** Geometric crossing alone cannot connect wires. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-198"></a>

<a id="sequence-task-751"></a>

- [ ] **EL-198 · Switch.** **Type:** Electrical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Physical or author-set contact state opens or closes an electrical path.

**Successful outcome:** Closed contact cannot create supply energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-199"></a>

<a id="sequence-task-752"></a>

- [ ] **EL-199 · Electric motor.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Supplied electrical work becomes bounded shaft torque with declared losses.

**Successful outcome:** Stalled shaft consumes only supported work and cannot bypass load limits. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-200"></a>

<a id="sequence-task-753"></a>

- [ ] **EL-200 · Drive belt.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Connected pulleys exchange torque through declared friction and tension.

**Successful outcome:** Missing tension or disconnected endpoints prevent full drive transfer. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-201"></a>

<a id="sequence-task-754"></a>

- [ ] **EL-201 · Drive chain.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Compatible sprockets exchange torque through typed toothed engagement.

**Successful outcome:** Incompatible attachment fails rather than inheriting belt behaviour. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-202"></a>

<a id="sequence-task-755"></a>

- [ ] **EL-202 · Conveyor.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Finite-work driven surface moves cargo through actual contact and friction.

**Successful outcome:** Unpowered or noncontact cargo is not carried along a hidden path. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-203"></a>

<a id="sequence-task-756"></a>

- [ ] **EL-203 · Reverse transmission.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Declared gear/belt arrangement reverses signed shaft motion under load.

**Successful outcome:** It cannot reverse by duplicating input work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-204"></a>

<a id="sequence-task-757"></a>

- [ ] **EL-204 · Weight.** **Type:** Gravity. **Scope/evidence source:** [existing record](#campaign-element-coverage). Movable load exchanges gravitational potential and tension through declared sockets.

**Successful outcome:** Raising it requires actual work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-205"></a>

<a id="sequence-task-758"></a>

- [ ] **EL-205 · Rope.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Finite-length tension-only connector couples actual routed endpoints.

**Successful outcome:** Slack rope cannot push or pull until tension develops. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-206"></a>

<a id="sequence-task-759"></a>

- [ ] **EL-206 · Fixed pulley.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Supported rotating sheave redirects a routed rope through actual tangent geometry.

**Successful outcome:** Disconnected or misrouted rope receives no invisible coupling. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-207"></a>

<a id="sequence-task-760"></a>

- [ ] **EL-207 · Moving pulley.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Load-attached sheave moves with the routed rope constraints and shared reaction forces.

**Successful outcome:** Its displacement/work ratio follows routing rather than a named pulley bonus. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-208"></a>

<a id="sequence-task-761"></a>

- [ ] **EL-208 · Balloon.** **Type:** Gravity. **Scope/evidence source:** [existing record](#campaign-element-coverage). Gas-filled compliant buoyant cargo responds to pressure, mass and puncture properties.

**Successful outcome:** Tether force opposes ascent; lost contents alter buoyancy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-209"></a>

<a id="sequence-task-762"></a>

- [ ] **EL-209 · Fan.** **Type:** Mechanical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Explicit electrical or shaft supply drives a bounded air field.

**Successful outcome:** Blocked flow or absent supply prevents remote force. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-210"></a>

<a id="sequence-task-763"></a>

- [ ] **EL-210 · Flashlight.** **Type:** Optical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Supplied finite-aperture source emits a widening optical cone.

**Successful outcome:** Occluding geometry blocks coverage and no supply means no emission. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-211"></a>

<a id="sequence-task-764"></a>

- [ ] **EL-211 · Solar panel.** **Type:** Electrical. **Scope/evidence source:** [existing record](#campaign-element-coverage). Absorbed illumination converts to bounded electrical work through declared efficiency.

**Successful outcome:** Darkness yields no power; output never exceeds absorbed energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

<a id="element-212"></a>

<a id="sequence-task-765"></a>

- [ ] **EL-212 · Flat mirror.** **Type:** Optical. A finite front surface reflects supported optical power according to its normal and material model; opaque frame/back remain physical.

**Successful outcome:** Rear incidence and occluded paths do not reflect through the housing. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 51–60; practice61–70; reuse111–120,142 as separate objectives in existing slots.

<a id="element-213"></a>

<a id="sequence-task-766"></a>

- [ ] **EL-213 · Optical combiner.** **Type:** Optical. Compatible incident optical channels leave an explicit output aperture with conserved combined power and declared coupling loss.

**Successful outcome:** A missing channel is not synthesized, and crossing unrelated beams does not combine by object identity. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 51–60; practice71–80; reuse117,142 as separate objectives in existing slots.

<a id="element-214"></a>

<a id="sequence-task-767"></a>

- [ ] **EL-214 · Broadband beam detector.** **Type:** Optical. A finite sensing surface integrates supported optical channels and drives a separately supplied contact above its threshold.

**Successful outcome:** Back-face, insufficient incident power and missing electrical supply are separate negative controls. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 51–60; practice71–80; reuse110,142 as separate objectives in existing slots.

<a id="element-215"></a>

<a id="sequence-task-768"></a>

- [ ] **EL-215 · Damped cushion.** **Type:** Mechanical. A finite compressible dissipative material surface absorbs impact work through the shared contact/deformation model.

**Successful outcome:** Grazing or bottomed-out impact does not guarantee arrest; deformation/heat loss balances work. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 1–10; practice41–50; reuse96,149 as separate objectives in existing slots.

<a id="element-216"></a>

<a id="sequence-task-769"></a>

- [ ] **EL-216 · Capture cradle.** **Type:** Mechanical. A physical retaining cup supports cargo after contact without remote ownership transfer; a separately placed/taught gate can release it.

**Successful outcome:** Oversize or energetic cargo can escape; no invisible capture radius or forced attachment. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 1–10; practice41–50; reuse96,149 as separate objectives in existing slots.

<a id="qualification"></a>

## Priority 7 — qualify the complete campaign and device experience

**Design acceptance for every item below:** [common gates](#design-acceptance) + [REVIEW](#accept-review); additional profiles apply to cross-cutting requirements.

<a id="campaign-qualification"></a>

### 7.1 Final 150-level progression and difficulty qualification

**Design acceptance for every item below:** [common gates](#design-acceptance) + [CONTENT](#accept-content); additional profiles apply to cross-cutting requirements.

**When:** After all required component coverage; focused proofs remain mandatory earlier.

**Gameplay outcome:** The final 150-level campaign works from start to finish with a deliberate learning curve, alternative valid solutions and understandable differences between Forgiving, Balanced and Precise.

Preserve all historical outcomes and failure records. Establish current content/revision evidence before counting any of the 1,800 baseline cells. Resolve any focused regression immediately rather than waiting for this broad sweep.

<a id="difficulty-physics-realism"></a>

<a id="sequence-task-770"></a>

- [ ] **Make difficulty control physics realism as well as placement tolerance and nudging** — retain the current assistance behavior, and add explicit simulation profiles for **Forgiving (easy)**, **Balanced (medium)** and **Precise (hard)**. Hard mode should use the most realistic supported physics, including detailed contact/friction, rotational inertia, load/torque limits, drag and losses where implemented. Easy mode should deliberately disable or simplify many secondary physical effects so machines are easier to build and predict; medium mode should provide an intermediate profile. Enumerate each supported effect and its behavior in all three profiles before implementation, distinguishing an intentionally disabled effect from an unimplemented one. Preserve the mechanism being taught, physical containment, required connections and meaningful resource/goal rules in every mode; simplification must not fabricate success. Use one authoritative shared engine with enum-typed difficulty/effect policies, not legacy solvers or automatic fallback paths. Explain the player-facing differences, freeze the chosen profile for a Run, and apply changes through an explicit Reset/restart. Update current content, callers, authoring, saves and tests together; identify the physics profile in replay/evidence records.

**Successful outcome:** The same constructed machine exhibits demonstrably simpler physics on easy, intermediate behavior on medium and very realistic supported behavior on hard, in addition to its existing tolerance/nudging differences. Prove selected effect differences with focused native and real-UI positive/control cases, deterministic reruns, exact Run/Reset and current-schema save/load, plus a production build. Audit every affected element and all 150 campaign lessons for solvability and preserved teaching intent; keep unsupported realism and unverified profiles explicit. This new requirement supersedes the preserved historical prohibition on changing physics realism by difficulty; historical evidence remains unchanged.

<a id="todo-503"></a>

<a id="sequence-task-771"></a>

- [ ] Validate difficulty progression and puzzle variety through hands-on play, not just reference solutions.

**Successful outcome:** Hands-on players can explain each new lesson, find more than a copied reference arrangement where intended, and progress without abrupt untaught jumps; final ordering follows observed play rather than reference-solver success alone.

<a id="todo-113"></a>

<a id="sequence-task-772"></a>

- [ ] Verify 150 distinct, playable levels with an intentional difficulty ramp and no unintroduced required mechanism. Expand the baseline matrix to 150 × 3 difficulties × 4 placement variants = 1,800 cases, plus repeatability and mechanism-specific probes; preserve failed attempts and evidence versions.

<a id="todo-493"></a>

<a id="sequence-task-773"></a>

- [ ] Complete the full direct-UI difficulty/placement-error matrix for the final 150-level campaign, including correction bounds/easing and browser Reset audits.
  - Run reference, near-positive, near-negative, and outside-window placements at Forgiving, Balanced, and Precise difficulty (1,800 distinct baseline cases). Use actual palette, gizmo, wiring, Run, and Reset controls; do not substitute game-state setters or numeric placement menus.
  - Review screenshots and sampled movement, document successes and failures, and distinguish repeated/historical attempts from unique matrix coverage. Historical outcomes are documented in docs/browser-playtest.md; rerun changed levels against their new content hash rather than carrying forward old passes.
  - [x] Add run-lifecycle tracking and failure capture to the UI runner. Nine adapter tests pass; a real UI early-Reset negative control is rejected and a level-8 reference completes normally. The original mixed-run record remains failed; its isolated retry passes. The source of the original early Reset/restart is still unknown.
  - Re-run the 52 older Reset records missing connection snapshots under the stronger connection-aware observer/audit; do not infer absent graph evidence.

**Successful outcome:** Every counted cell identifies the current level/content/revision, intended placement variant, actual construction, outcome and Reset result. All 1,800 distinct baseline cells plus required repeats are accounted for without relabelling failures as passes.

<a id="todo-143"></a>

<a id="sequence-task-774"></a>

- [ ] Extend UI-only rope trials with out-of-window errors, repeated boundary-sensitive success/failure comparisons and current-schema Save/Load; review rendered motion beyond sampled transforms. The initial model uses ideal fixed point guides: moving-block ratios, pulley inertia/friction, obstacle wrapping, cutting and rope self-collision are not implemented.

<a id="todo-161"></a>

<a id="sequence-task-775"></a>

- [ ] Extend bumper evidence with repeated matched-error trials, frame-by-frame animation review and browser save persistence; sampled screenshots alone do not prove fluidity.

<a id="todo-164"></a>

<a id="sequence-task-776"></a>

- [ ] Complete repeated wall-lesson browser difficulty trials, save/load and resize checks near screen edges/size limits. Keep the campaign/readme/playtest handoff aligned with the current draft; older content-hash evidence is historical, not current campaign verification.

<a id="todo-170"></a>

<a id="sequence-task-777"></a>

- [ ] Carry the useful legacy UI tests into the revised 150-level campaign; prioritise new mechanics and updated levels before exhaustive legacy coverage. Repeat matched placement errors to establish repeatability and separate nudging from other difficulty assistance.

<a id="todo-168"></a>

<a id="sequence-task-778"></a>

- [ ] Finish the legacy level-10 evidence handoff: all twelve runs are saved; review the four Precise screenshots and complete the audit/report. Do not start another legacy batch before new-mechanics work.

**Successful outcome:** The four remaining Precise captures are reviewed and the twelve legacy outcomes have an accurate final audit/report, preserving failures without starting another exhaustive legacy batch.

<a id="mobile-polish"></a>

### 7.2 Full mobile access, visual cohesion and measured fidelity

**Design acceptance for every item below:** [common gates](#design-acceptance) + [REVIEW](#accept-review); additional profiles apply to cross-cutting requirements.

**When:** Design touch access in every earlier change; complete the full device/content qualification after component coverage.

**Gameplay outcome:** Players can build, wire, run, save and finish puzzles on real iOS and Android browsers without a keyboard, accidental camera gestures or unreadable controls.

Loading, memory, frame pacing and audio suspension are measured on devices. Preserve the palette; original-game calibration is research with recorded uncertainty, not permission to retune production physics or copy an implementation.

<a id="todo-481"></a>

<a id="sequence-task-779"></a>

- [ ] Make the full game playable in mobile browsers.
  - Provide touch-first placement, selection, movement/lift, rotation, wiring, undo, and Run/Reset without requiring keyboard shortcuts, hover, or right-click.
  - Add discoverable camera gestures, generous touch targets, and clear separation between camera gestures and part manipulation.
  - Adapt the UI to small screens, portrait/landscape orientation, and safe areas while keeping the puzzle visible.
  - Check loading size, memory use, frame rate, and WebGL compatibility on real iOS Safari and Android Chrome devices.
  - Playtest all puzzle mechanics on touch devices; desktop browser emulation alone is insufficient.

**Successful outcome:** A player completes the whole build–wire–Run–Reset–save loop on real iOS Safari and Android Chrome without keyboard-only actions. Implement touch-safe controls alongside priority 2; the full device/per-part campaign qualification completes here.

<a id="todo-459"></a>

<a id="sequence-task-780"></a>

- [ ] Apply the owner's Monument Valley-inspired art direction to environment and part forms, composition, gentle lighting and calm readable motion, using original assets; **retain the current colour scheme**, including sky/wood/cream/navy/gold and all established part colours. See DESIGN.md. Preserve minimal icon UI, transparent reference walls, three unfilled placement projections, solid floor collision and clear mechanical behaviour. Review desktop/mobile build and run views, including colour consistency, before claiming completion.

<a id="todo-504"></a>

<a id="sequence-task-781"></a>

- [ ] Quantitatively calibrate classic physics against original-game observations; current physics fidelity is not proven.
  - Obtain reproducible original-game drop/bounce observations with the game edition, object size, pixel scale, timing, and measurement uncertainty recorded; compare dimensionless rebound ratios and timings against the current C# calibration baseline.
  - Resolve remaining primary-source research gaps and distinguish verified measurements from historical descriptions. Do not claim integer-physics equivalence or cross-platform determinism from current quantized-float replay tests.
  - Independently validate the pinned Windows TEMIM/OpenTIM atmosphere fixtures in docs/physics-reference-temim.json, establish original tick/fixed-point scales, and compare material/mass ratios and impact-speed-dependent bounce response. Keep upstream-reported data separate from our own observations; no GPL code integration or runtime retuning is approved by this research note.

**Successful outcome:** Measured edition-specific observations provide an uncertainty-labelled comparison of drops, rebounds and timings. Any later proposal to change game feel is explicit; collecting reference data does not itself authorize runtime retuning.

<a id="delivery"></a>

## Delivery requirements that apply throughout

**Design acceptance for every item below:** [common gates](#design-acceptance) + [REVIEW](#accept-review); additional profiles apply to cross-cutting requirements.

**When:** Applies continuously; not a separate sweep that must finish before the next component.

**Gameplay outcome:** Each delivered change has working current-schema construction, understandable behaviour, a meaningful failure case and reproducible proof on the version players will run.

Run the relevant subset with each item; keep the repository-wide typed-value audit explicitly unfinished until verified. Component coverage takes precedence over exhaustive difficulty sweeps, but never over focused correctness, Reset, production-build or real-UI evidence.

<a id="todo-094"></a>

<a id="sequence-task-782"></a>

- [ ] Audit every catalogue part and fixture against a per-part Playwright evidence matrix: tested revision, recipe, intended behaviour, negative/control, supported modes, connections, Run/Reset, visual review and evidence paths. Mark missing or stale proof incomplete; close gaps for previously shipped parts as well as new ones. Do not infer coverage from native tests, a family representative or historical campaign success.

**Successful outcome:** Every catalogue part/fixture has a current, reproducible real-UI positive/control record with correct configuration, typed links and exact Reset. Missing or stale evidence remains visibly incomplete.

<a id="todo-498"></a>

<a id="sequence-task-783"></a>

- [ ] Close the browser audit's remaining verification gaps: compare the constructed graph with the intended recipe, verify exact target assignment and per-slot bounds (including rotation-only eligibility), cover heterogeneous blend durations and required corrections, and inspect rendered fluidity between samples.

**Successful outcome:** An audit rejects a run with a wrong endpoint, incorrect part setting or cross-run event even if the goal happens to succeed; verified logs correspond to the construction actually seen through the UI.

<a id="todo-097"></a>

<a id="sequence-task-784"></a>

- [ ] Implement each part family as typed declarations and bindings to shared WASM SIMD f32 physical laws, reusable C# discrete controllers and shared C# animation evaluators, with scene/visuals, activation feedback, authoring/inventory support and focused physics/UI tests; avoid placeholder parts or levels that bypass the actual mechanics.

<a id="todo-438"></a>

<a id="sequence-task-785"></a>

- [ ] Apply the per-element completion gate above to every row: native positive/negative and integration checks, repeatability/order tests, complete current-schema construction restoration and Reset during charge/stroke/contact, production build, UI-only positive/control Run–Reset and full-event motion review. Retain failures; screenshots alone do not prove fluid motion. **Commit and push each individually proven element before starting the next.** Full difficulty/nudging sweeps remain deferred, but author-bounded tolerances must never rescue a wrong route, add energy or bypass an interlock.

<a id="todo-231"></a>

<a id="sequence-task-786"></a>

- [ ] Each implemented family needs a C# scene/catalog definition, original pictogram, readable activation animation, typed connections where relevant, authoring/save support, physics/event tests and UI-only browser attempts. Keep toy styling and contextual controls.

<a id="todo-380"></a>

<a id="sequence-task-787"></a>

- [ ] Each selected batch needs authoring/inventory/current-schema support, native behaviour/conservation/Reset checks, production build, real-UI smoke and retained negative attempts, then commit/push. Full difficulty/nudging remains deferred.

<a id="todo-326"></a>

<a id="sequence-task-788"></a>

- [ ] For every selected family, add authoring/inventory/save support, native behavioural/Reset tests, then repeated real-UI Playwright attempts for all difficulties. Include valid references, matched near-errors and outside-window failures; compare normal/slow playback, simultaneous events and Reset at activation boundaries. Do not equate sampled screenshots with proven animation fluidity.

<a id="todo-233"></a>

<a id="sequence-task-789"></a>

- [ ] Add alternative-solution tests and failure controls: disconnected power, missing drive, cut tether, blocked beam, wrong colour, insufficient load and premature timer. Review smooth rendered motion and repeat matched difficulty attempts.

**Successful outcome:** A valid alternative machine can win, while the documented missing-drive, cut-rope, wrong-colour, blocked-path and premature-trigger controls fail for visible mechanical reasons.

<a id="todo-232"></a>

<a id="sequence-task-790"></a>

- [ ] Define author-controlled forgiveness per mechanism: socket reach, contact overlap, aim/timing windows and bounded placement correction where appropriate. Do not implement only weaker gravity/friction; do not let assistance silently connect an incompatible cable or bypass a required mechanism.

**Successful outcome:** Small authored placement/aim/timing errors ease toward a valid physical construction within declared bounds; a wrong route, missing energy or incompatible socket still fails.

<a id="todo-325"></a>

<a id="sequence-task-791"></a>

- [ ] Separate generous editor snapping from hidden author-controlled runtime forgiveness. Define per-part bounded inlet/angle/receiver/contact/timing tolerances in puzzle definitions; smoothly ease eligible errors without warping through walls, selecting the right branch, inventing power or bypassing a required mechanism.

**Successful outcome:** A player-visible construction snap is one undoable edit; hidden authored runtime assistance is separately bounded and cannot choose a branch, bypass a wall or generate power.

<a id="todo-324"></a>

<a id="sequence-task-792"></a>

- [ ] Keep all new interactions touch-compatible, with no required hover/keyboard modifier. Preserve DESIGN.md, existing minimal UI and fluid activation animation; use C# wherever possible.

**Successful outcome:** All new menus and gestures can be operated by touch with visible affordances; hover, right-click and keyboard modifiers remain optional.

<a id="todo-092"></a>

<a id="sequence-task-793"></a>

- [ ] Complete the no-magic-string audit across runtime protocols, UI selectors, tests, tooling and authored content. Closed choices, including sockets, retain enums throughout domain code with validated canonical serialization boundaries; extensible identities use typed IDs. Reject aliases, padded/combined names, unknown and undefined values explicitly.

**Successful outcome:** Current code and content use compiler-checked choices through UI, simulation, serialization and tests. The audit names every remaining violation until none remain; a single converted selector cannot establish repository-wide compliance.

<a id="todo-150"></a>

<a id="sequence-task-794"></a>

- [ ] Continue enum/typed-ID cleanup for remaining closed UI action/mode sets and authoring property contracts as those components are refactored; keep resource/instance IDs extensible rather than hard-coding the catalogue into an enum.

**Successful outcome:** A changed UI action or authoring choice cannot be misspelled internally, while extensible resource/instance identities remain typed identifiers. Close affected callers together without old-name aliases.

<a id="todo-229"></a>

<a id="sequence-task-795"></a>

- [ ] Connect this list to the existing batteries/solar/wires/motors/chains/weights/ropes/pulleys backlog. Batteries and chains are user-requested modern extensions; the researched original evidence here establishes outlets, generators, solar sources and fan belts, not standalone batteries or chain drives.

<a id="todo-322"></a>

<a id="sequence-task-796"></a>

- [ ] Evaluate new pieces for distinct puzzle decisions, recombination with existing pieces, readable behaviour, easy assembly, repeatability and teachability. Research precedents include [Infinifactory's sensors/actuators](https://zachtronics.com/zachademics/) and [Opus Magnum's programmable mechanisms](https://store.steampowered.com/app/558990/Opus_Magnum/); our game should not inherit their programming UI.

**Successful outcome:** Every named candidate retains a distinct decision and a playable teaching example; overlaps are mapped to the same implementation without deleting either requirement or granting duplicate completion credit.

<a id="todo-228"></a>

<a id="sequence-task-797"></a>

- [ ] Record a small behavioural reference fixture for each selected family: edition, source/page or video timestamp, valid inputs, output, timing, repeat behaviour, destructibility and Reset. Manuals/inventories establish intent, not our physics constants.

**Successful outcome:** Before a researched behaviour becomes required in a puzzle, its valid inputs, outcome, timing, repetition and Reset rule can be reproduced from the recorded source and in our focused example.

<a id="todo-227"></a>

**Scope index — individual element specifications:** [EL-097](#element-097), [EL-098](#element-098), [EL-099](#element-099), [EL-100](#element-100), [EL-101](#element-101), [EL-102](#element-102), [EL-103](#element-103), [EL-104](#element-104), [EL-105](#element-105), [EL-106](#element-106), [TH-25](#thermal-25), [TH-31](#thermal-31), [TH-32](#thermal-32). This former umbrella is navigation only; each linked element is independently specified and verified. Retain the original priority/adoption status and campaign reservations; interaction examples are separate proof tasks below.

<a id="sequence-task-798"></a>

- [ ] **Separate integration verification:** Edition-specific claims for every named candidate have a primary executable/manual observation or an explicit unresolved entry; a later-series description cannot silently define its gameplay. This is a cross-element acceptance task using the [generic interaction processes](#generic-interaction-register), not a combined element specification.

<a id="todo-171"></a>

<a id="sequence-task-799"></a>

- [ ] Review and verify the unpublished runner, audit, research, and documentation changes before a follow-up commit/deployment; the live GitHub Pages build does not yet contain these local changes.

**Successful outcome:** The intended current build is reviewed, tested and delivered with a recorded revision; compare it with the actual deployed revision before asserting that earlier unpublished changes are still absent.

<a id="completed-work"></a>

## Existing behavior and control requirements

These stable IDs identify current accepted behavior, not historical completion claims or separate implementation chores. Every affected part still requires current qualification under the common acceptance and its current model. The original completion reports are retained only as provenance.

<a id="todo-078"></a>

Trampoline off-centre deformation uses a frame-bounded, direction-aware visible profile. Preserve physical contact clearance, palette, rigid-rim control, unchanged physical trajectories, exact Reset and continuous desktop/mobile readability.

<a id="todo-080"></a>

Teach “A gentle rebound”: one passive trampoline redirects a falling ball into a fixed basket. Preserve typed authoring, bounded smoothly blended position/rotation assistance, all three difficulty references, flat/missed/missing controls and rebound-through-pipe integration.

<a id="todo-083"></a>

Teach “Saved for later”: a two-second Hold Timer supplies the winding motor, then a three-second delay releases retained charge after shaft motion stops. Missing supply cannot charge; missing release retains charge. Verify typed links and exact Reset at all difficulties.

<a id="todo-085"></a>

Launcher diagnostic enum boundaries reject alternate spellings, padded names, combined values and unknown tags; every canonical value round-trips exactly.

<a id="todo-087"></a>

Qualify launcher off-centre and oversized loading, including the authored 0.061-unit offset and a radius-about-0.508 load outside the 0.41 guide. A physically valid rim tap is allowed; an invalid zero-motion assumption is not an oracle. Inspect latch/helix motion continuously and prove exact Reset.

<a id="todo-089"></a>

Teach “Wind, then release”: place/rotate the launcher under a fixed payload with basket, electrical motor supply, mechanical winding and delayed latch signal. Typed authoring reproduces the construction; omitted supply accepts no work or free release. Qualify all difficulties, controls and Reset.

<a id="todo-117"></a>

Teach “Meet in the middle”: resize and align a straight tube with a fixed 90° bend. Qualify real-UI resizing, rotation, move-release joining and missing-tube control.

<a id="todo-120"></a>

Provide typed 45°/90° clear pipe bends, matching collars/rails and original icons. Straight-to-bend and bend-to-bend snapping use the same typed mouth geometry and continuous hollow contact.

<a id="todo-121"></a>

Preserve bend lessons' bounded assistance and actual alternative solutions: the declared 90° depth error succeeds on Forgiving/Balanced and fails on Precise; an independently valid 45° precise route may succeed. Use authored 0.6/0.5-unit and 5°/2° windows where declared and inspect physical entry/curved passage/exit.

<a id="todo-124"></a>

Teach straight-pipe transport through a real annular bore under shared WASM SIMD gravity/contact. Opaque collars block light while the clear shell transmits it; preserve original pictogram, rotation and palette.

<a id="todo-125"></a>

Qualify straight-pipe references at all difficulties and matched depth-error controls, exact same-environment replay, isolated assistance, missing pipe, rotated passage, side-wall contact, high-speed/oversized balls, optical openings and Reset.

<a id="todo-126"></a>

Straight-pipe length resizing spans 1–8 units with fixed 1.3-unit bore, one contextual local-axis handle, Cancel and one-gesture Undo. Preserve all three wall handles and synchronized geometry/properties, current authoring and Reset.

<a id="todo-127"></a>

Typed editor-only tube snapping uses 0.45-unit/20° proximity, matched bore, opposing faces and occupied-mouth rejection. Preview alignment, placement/move release and single Undo must agree. Qualify three rotated continuous seams, separation/rejoin, Undo and Run/Reset; do not make a rigid joint or change difficulty.

<a id="todo-130"></a>

Delay uses Ready/Counting/Finished, explicit activation input/output, 0.1–12 seconds/default 1 and a readable countdown hand. It supplies no electricity; busy/finished inputs are ignored until Reset. Apply the complete controller contract.

<a id="todo-131"></a>

Both delay lessons use actual placement/wiring/Run/Reset at all difficulties with a direct-early-bypass control. Goals measure elapsed time from the fixed switch, independent of generated timer identity.

<a id="todo-134"></a>

Torch and solar panel use self-contained torch supply, impact/activation latch, nine front-face samples, finite cone/range/facing, partial/full physical occlusion, four-mark power meter and typed supplied output. Ambient rendering light supplies no power.

<a id="todo-135"></a>

Solar lessons expose blue cells and power meter to the default camera. Qualify all difficulty references, shaded-panel and missing-wire controls and exact Reset.

<a id="todo-137"></a>

Torch artwork is a widening layered cone consistent with physical angle/range/occluders, including rotated and partial occlusion. Use reference/shadow/missing-wire controls and never let artwork become optical authority.

<a id="todo-140"></a>

Weights have authored mass, fixed pulleys/anchors, explicit sockets and measured lengths; ropes are unilateral slack/tension constraints with mass-weighted counterbalance and 3D routes. Open routes are dashed and transmit no tension; Reset is exact.

<a id="todo-141"></a>

Qualify rope mass ratios/equal masses, direction/declaration order, slack release, incomplete routes, graph rejection, floor contact, 3D pendulum work/length bounds and exact Reset under current f32 limits.

<a id="todo-142"></a>

Rope teaching cases require every difficulty reference, matched positive/negative pulley errors, a missing-rope control, sampled physical motion and rope-length/Reset checks.

<a id="todo-145"></a>

Connections use named typed compatible sockets for activation, electrical, signal, mechanical and rope domains. All current content declares explicit endpoints; activation and electrical supply remain separate.

<a id="todo-146"></a>

Battery and motor include original scenes/icons, actual electrical contacts, supply-loss clearing, finite-inertia acceleration/coasting and exact Reset. Contextual linking selects the sole compatible pair; navy supply cables terminate at the actual sockets.

<a id="todo-147"></a>

Closed switches conduct supplied power through a coherent network snapshot, source-seeded traversal and simultaneous assignment. Cycles cannot create supply. The switched-supply lesson requires first power after its trigger and rejects the direct-wire bypass.

<a id="todo-148"></a>

Connection/goal/event/socket choices are enum-typed through the domain with canonical validated external mappings. Misspellings, unsupported numeric enum values and aliases reject; typed extensible identities remain distinct.

<a id="todo-151"></a>

Belts carry signed finite-inertia, load-aware mechanical motion through conveyor input/output and a −1 reverse transmission. Preserve opposing wheels, direction-following treads/arrows, navy twin strands and moving gold witnesses. Ideal copied-speed propagation is superseded by the current rotary/transfer contracts.

<a id="todo-152"></a>

Conveyor teaching routes require actual battery/motor drive and switched supply where applicable. Introduce electricity before relay/reverse combinations; preserve required authored links.

<a id="todo-153"></a>

Qualify signed drive, two reversers, declaration order, supply loss/coasting, graph rejection, required links and Reset at each supported difficulty with real mechanical work accounting.

<a id="todo-157"></a>

Bumper lessons teach spherical/radial contact, per-body cooldown, overlapping impact-ring feedback and Reset under the current finite-work model; no scene-owned impulse path.

<a id="todo-158"></a>

Current canonical saves use stable puzzle IDs and explicitly reject earlier/missing formats. No compatibility aliases, implicit migration or format inference.

<a id="todo-159"></a>

Isolated bumper assistance holds unrelated assistance at strict defaults: the specified imperfect placement succeeds on Forgiving/Balanced and fails on Precise, with same physics and exact Reset.

<a id="todo-160"></a>

Both bumper lessons require real-UI difficulty/placement controls, sampled motion and Reset. Inspect the expanding gold impact ring and smooth settling, including overlapping impacts.

<a id="todo-162"></a>

Walls have three local-axis resize handles, declared dimension limits, saved properties and matching collision/projections; Cancel and one-gesture Undo restore exactly. Verify actual UI dimensions and accepted/cancelled edits.

<a id="todo-163"></a>

Teach introductory and combined wall/bumper puzzles. UI tooling uses real resize gestures and verifies actual part dimensions/properties, not injected construction.

<a id="todo-166"></a>

Preserve ground-plane WASD movement and Q/E camera turning alongside mouse orbit and part rotation. Toolbox expands to inventory; icon/name/count form one click target, with actual keyboard/browser controls.

<a id="todo-169"></a>

Superseded report-completion instruction; no current action. Current per-level/difficulty/Reset and visual-review criteria remain in the campaign and UI acceptance.

<a id="todo-172"></a>

DESIGN.md is the approved form, palette, icon, animation, interaction and minimal-UI authority for every current/new feature.

<a id="todo-242"></a>

Pipe kit includes length-resizable straight tube, fixed-radius 45°/90° elbows and transparent funnel with 2.6-unit mouth narrowing to the standard 1.3-unit bore. The funnel is physically hollow, without attraction/scripted transport.

<a id="todo-244"></a>

Powered tube gate uses electrical-only input, compatible mouths, original icon and acceleration-limited gold blade with obstruction-safe closing. Qualify supplied/unpowered/loaded/blocked controls and exact Reset.

<a id="todo-250"></a>

Both 45° and 90° bends join the common bore and preserve continuous physical travel; qualify combinations rather than treating one angle as family coverage.

<a id="todo-261"></a>

Hold Timer closes a supplied electrical contact for 0.1–12 seconds/default 2, ignores busy triggers and rearms on expiry. Source loss does not pause countdown; the timer creates no electricity.

<a id="todo-262"></a>

Contextual connection-choice Cancel and one-step Undo leave original typed links unchanged. Subsequent Run/Reset cannot resurrect an undone trigger wire.

<a id="todo-264"></a>

Ball detector emits once per forward centre crossing of its physical aperture and rearms only after whole-ball upstream clearance. Preserve direction mark/fading indicator and forward/rotated/fast/reverse/outside/stationary/jitter/two-body/disabled controls.

<a id="todo-265"></a>

Pressure plate threshold is 0.1–16 kg direct-contact mass/default 0.5 kg. Contact closes only while enough mass rests on its top face; no supply, latch or impact command is created. Count a body once, not compound contacts or stacked force.

<a id="todo-267"></a>

Set/Reset latch has explicit typed inputs, next-boundary settlement and Reset-dominant same-tick arbitration. Separately supplied contact and memory survive supply loss; Reset clears pending input/state.

<a id="todo-269"></a>

Counter target 1–9/default 3 is shown with slate/gold dots, saturates and emits threshold once. It latches a supplied electrical contact until Reset; held input does not increment each tick and extra triggers cannot re-emit.

<a id="todo-271"></a>

Both gate requires two continuous control inputs and independent supply. One/two-mark sockets and three eased lamps show individual inputs and conjunction.

<a id="todo-273"></a>

Powered clock interval is 0.1–12 seconds/default 1. First/restored event follows a full interval; supply loss cancels coincident deadline and no disabled-time catch-up occurs. Preserve count/last-event history and exact Reset.

<a id="todo-285"></a>

Laser has separate electrical supply and trigger enable latched until Reset, authored amber linear-RGB game power and 16-unit range. Its narrow visible beam clips at shared opaque geometry. Supply loss extinguishes it at the next optical snapshot; restored supply uses retained enable without a fabricated retrigger.

<a id="todo-286"></a>

Finite front-silvered mirror reflects from its actual transformed normal, retains 95% power per bounce, shares one range budget and caps reflections at 16. Frames/backs/mounts and returning rays physically occlude.

<a id="todo-288"></a>

Torch uses a soft cone widening from the lens: four nested translucent shells with 48 sampled directions each, consistent with angle/range and physical clipping. Mesh sampling does not replace the optical law.

<a id="todo-290"></a>

Receiver is a finite front-facing absorbing disc with concentric marker and eased slate/gold threshold indication. It switches separate electrical supply while illuminated and creates no electricity. Trace every emitter, then commit all receiver readings together before the electrical solve.

<a id="todo-292"></a>

Beam shutter has navy housing, cream rails and a gold physical blade. Closing stops before a visible ball; the same committed blade pose blocks light and bodies.

<a id="todo-294"></a>

Two-sided 50:50 splitter halves every existing linear-RGB component into transmitted/reflected paths. Preserve the remaining range across branches, at most 16 interactions per path and 128 total segments per emitter. Transparent solid pane and opaque frame remain physical; selection previews both paths.

<a id="todo-295"></a>

Receiver threshold is 0.05–2/default 0.25: half-power amber 0.35 activates and quarter-power 0.175 does not, subject to canonical f32 admission preserving the control. No extra optical/electrical power.

<a id="todo-297"></a>

RGB filters have finite two-sided apertures, transparent solid panes and opaque frames. Preserve only existing selected-channel energy; unlike stacked filters extinguish it, with no recolouring.

<a id="todo-298"></a>

Typed independent optical apertures are sampled coherently and aggregate/commit per port. Reject duplicate/unsupported IDs, invalid geometry and amplifying passive transmission.

<a id="todo-299"></a>

Passive RGB combiner has three cream input rims, gold output rim and numbered marks. Each ray retains 90% power and remaining range/interaction budget, counting internal travel.

<a id="todo-300"></a>

Red/green/blue powered receivers require selected channel at threshold/default 0.25 and at least 90% of total RGB. They switch independent supplied power.

<a id="todo-301"></a>

Yellow/cyan/magenta/white receivers require every named channel at threshold and at least 90% of total power in that set. These are explicit presence rules, not exact spectral matching.

<a id="todo-323"></a>

Superseded 75-level slot allocation; no current action. The current 150-level campaign reservation and teaching contracts own placement/order of all retained mechanisms.

<a id="todo-345"></a>

Speaker handles next-tick triggers with finite directional pulses, procedural tone, animated cone and fading wavefronts. Independent electricity is required and unpowered requests expire.

<a id="todo-346"></a>

Sound meter uses strongest direct pulse, opaque blocking, actual facing and visible needle; supplied threshold contact emits once until quiet rearms. Qualify source order/repeats/occlusion/backwards/missing-supply/hysteresis/invalid values and Reset.

<a id="todo-347"></a>

Bell sounds from real collision above threshold with mass/impact-dependent omnidirectional response, separation rearm and bounded/coalesced retriggering. Procedural voice and damped artwork do not add a physical kick; qualify positive/depth-miss integration and Reset.

<a id="todo-348"></a>

Wind chimes use constrained 3D sail/clapper, actual individually identified tube contact/tones, separation rearm and direct strikes. Airflow blocks at solid geometry including transparent conduit walls; overlap alone creates no sound.

<a id="todo-359"></a>

Typed And/Or/Xor/Nor/Nand evaluation governs electrical/optical logic. Optical inputs have independent hysteresis, next-advance decisions and explicit Reset.

<a id="todo-360"></a>

Optical decisions use coherent before-network/reception boundaries and conserve independent carrier power. Qualify every truth operation, rotation, occlusion, chained timing and actual UI.

<a id="todo-361"></a>

Both has independent bottom supply and two numbered controls; supplied positive/missing-supply controls and exact Reset are required, with no compatibility alias.

<a id="todo-362"></a>

All five electrical gates have A/B controls, independent supply and switched output. A logically true NOR/NAND without inputs cannot create power.

<a id="todo-363"></a>

Electrical dependency components settle AND/OR cycles from the unpowered least fixed point; reject zero-delay XOR/NOR/NAND feedback atomically before power commit.

<a id="todo-364"></a>

Every electrical OR/XOR/NOR/NAND/Both variant has its own playable catalogue entry, toolbox/face icon and separately supplied output; no representative-only closure.

<a id="todo-366"></a>

Optical parts use separately addressed typed apertures and per-port coherent snapshots through the current shared optical authority.

<a id="todo-367"></a>

All five optical gates have absorbing A/B inputs and an independent carrier routed at 90% power, next-tick decisions, original icons and separate catalogue entries. Qualify every truth row with/without carrier.

<a id="todo-392"></a>

Reloadable cannon has physical open chamber, finite electrical charge, typed trigger, clear-muzzle check and same-payload swept flight with guarded slate recoil. Qualify power/no-supply, empty/blocked, oblique aim, repeated return, distinct-ball feeder, ordering/work and exact Reset/save.

<a id="todo-394"></a>

Latched launcher uses a finite-mass guided plunger, elastic store, continuous ratchet and separate release trigger. Winding is paid mechanical work, charge survives supply loss, all parameters/states/diagnostics are typed, and coil/latch/index follow physical motion. Preserve “Wind, then release” and “Saved for later” teaching controls.

<a id="todo-440"></a>

Planar drag and its Undo entry start only after six pixels. Selection-only release cannot tube-snap or alter construction; qualify click jitter, actual drag, grid snapping and exact Undo.

<a id="todo-461"></a>

Springboard top plate/collider share the current elastic physical pose; helix follows compression, overlapping contacts remain continuous and Reset restores authored precompression. The earlier independent cosmetic recoil/unchanged launch callback is superseded.

<a id="todo-469"></a>

Rope artwork follows actual outside-rim tangents/arcs, removes pulley knots while retaining endpoint knots, prefers local upper threading and preserves that side during motion. Physical length/slack/wheel travel must match.

<a id="todo-487"></a>

Repository/deployment creation is complete historical administration, not new work. Current publication remains governed by independent snapshot and deployed-origin verification gates.

<a id="todo-491"></a>

Canonical construction Save survives fresh-page reload/Load at the actual deployed origin in the same browser profile. Qualify supported devices/storage errors explicitly; cleared/private storage is not assumed persistent.

<a id="todo-492"></a>

Superseded 40-level report-completion instruction; no current action. Every final level remains subject to the current 150-level real-UI/difficulty/campaign acceptance.

<a id="todo-501"></a>

The authored fan-height correction for the named introductory case blends over 0.15 seconds using bounded quintic easing. Qualify positive/negative height perturbations on Forgiving/Balanced without changing global physics.

<a id="todo-510"></a>

Shift-drag aligns the selected movement axis to the tenth-unit world grid while free dragging is unchanged. Qualify the named Precise Y=3.8 path without correction, exact Reset, touch precision and discoverability.

<a id="retained-context"></a>

## Scope and delivery policy

Deliver every named element and variant through the [current acceptance contract](#design-acceptance), [delivery workflow](../delivery-workflow.md), [current architecture](../engine-contracts.md) and the [ordered roadmap](invest/vertical-delivery.md#rolling-playable-roadmap). Preserve real 3D geometry, typed connections, original palette/artwork, author-controlled assistance, alternative solutions, reliable Reset, 150 progressively taught levels and the unlimited free-play workshop. Component coverage (ELEMENT-n after LEGACY-0) precedes exhaustive campaign/difficulty sweeps (CAMPAIGN). Current requirements above contain the complete required outcomes; historical schedules and reports create no additional prerequisites.
