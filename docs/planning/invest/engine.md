# Engine delivery slices

**Current architecture:** [Canonical Half game values and WGSL f16 physics](../../gpu-f16-physics.md) define the numerical model. Current design/acceptance is self-contained; implementation and qualification status are in [TODO](../../../TODO.md).

These are labels within existing canonical work IDs, not new work orders or independently required administrative gates. Their parent IDs, technical prerequisites and full acceptance remain in [the register](../work-register.md). Read [the execution rules](../invest-index.md) before selecting a slice. A bounded candidate is not a runtime Pass or an assertion that its prerequisites are complete.

The target owner is fixed by [P0-004](../../engine-contracts.md#ownership-and-assemblies), [spatial authority](../../engine-contracts.md#ownership-and-assemblies), [P0-005](../../engine-contracts.md#wire-and-admission) and [P0-006](../../engine-contracts.md#lifecycle-and-atomicity). Existing mixed MachineWorld/ScenePhysicsAssembly, SimulationTransaction, SimulationCommandInbox, CommittedPoseBuffer, AnimationBatch, SceneAnimationAdapter and Workshop callers ground these boundaries. Each cutover updates all affected current callers/content and removes the superseded route together. New target-contract adapters are allowed; compatibility bridges are not.

Choices of algorithm, array layout and helper factoring remain negotiable inside those fixed ownership/typing/physics constraints. Estimate by the named affected closure and concrete unknown, not fictional time. Required source-specific positive/control/boundary, rollback, build, actual Chrome, Reset/save, performance and publication gates apply at their original stage. Parent aggregate closure never follows merely from one child passing.

P0-001–006 are bounded baseline/design decisions, already with their own records: use the latest applicable verdict, and reopen only an affected contract question. P0-009/010/012/030/031/033/034/035 are conjunction/audit/qualification/release parents; choose their explicitly named law, consumer, fixture, defect or device/workload child. Do not implement an audit as one feature. P0-035 alone unlocks product expansion.

The planned vertical order starts with **[Basketball drop/contact/Reset](vertical-delivery.md#ball-drop)** then Receiver capture. [TODO](../../../TODO.md) identifies current execution. These engine labels provide required criteria inside playable outcomes, not a serial subsystem queue. Every card below states current physical behavior and admitted f16 controls.

<a id="p007-contact"></a>
## P0-007 · p007-contact

**Outcome / value:** Preserve the captured moving-support physical invariant in the [bounded GPU contact method](gpu-physics.md#p007-constraint); the old CPU diagnostic remains historical evidence.

**Small closure / forward cutover:** Use the new sole GPU numerical owner and approved f16 error contract; change affected callers together, with no CPU correction or second solver path. Follow the live recovery record, not a copied residual.

**Observable acceptance:** Replay captured x15/compound failure and independent stationary/zero-gravity/rotated/support-order controls; finite work, Coulomb admissibility, exact failed-step restoration. Required affected Chrome proof stays in P0-007.

**Dependencies / estimate boundary / stop:** Moving-support/x15/compound contact behavior belongs to the admitted moving-support/compound consumer and full P0-007 audit, not the first ball/workbench scope. Prove that named invariant under current f16 limits; harness success alone is not delivered behavior.

<a id="p007-affine"></a>
## P0-007 · p007-affine

**Outcome / value:** Preserve captured affine support geometry and the corrected support footprint.

**Small closure / forward cutover:** Geometry is sole numeric owner; scene boundary copies full basis, stable identity and material metadata. Update all support/sweep/contact consumers; retire quaternion-only or slot-derived identity paths.

**Observable acceptance:** Independent hollow, rotating, anisotropic and reflected/invalid transform goldens; storage reorder cannot change identity. Distinguish representation-only values from actual SupportFootprint correction.

**Dependencies / estimate boundary / stop:** Ball/workbench geometry is first; Receiver walls and rotated Ramp follow in their named verticals. Retained P0-007 findings constrain physical behavior, while the target GPU implementation replaces the old CPU extraction. Other shape/query consumers remain individually required.

<a id="p008-reject"></a>
## P0-008 · p008-reject

**Outcome / value:** Reject an invalid current construction before any installed state changes.

**Small closure / forward cutover:** ConstructionCompiler validates/copies current declarations at the actual entry point; update current capture/callers and serialized content simultaneously. Reject obsolete fields/unknown enums; no accepted-but-uninstallable DTO stage.

**Observable acceptance:** Valid current construction still runs; malformed topology/duplicate identity/nonfinite geometry fail without changing old state, counters or queues; mutate caller arrays after submission to prove isolation.

**Dependencies / estimate boundary / stop:** P0-004/P0-005/P0-006 fixed contracts; the actual Basketball capture/compile/install proof is internal to ball-drop. Required schema/ownership checks pass now for that closure; complete CHECK-AGGREGATE is a later aggregate, not a first-UI prerequisite.

<a id="p008-compile"></a>
## P0-008 · p008-compile

**Outcome / value:** Compile a valid construction into deterministically owned runnable state.

**Small closure / forward cutover:** Compiler owns candidate-local arrays and bindings; stable typed IDs survive order/compaction. Core retains sole mutable runtime owner. Remove direct scene-object/delegate references through all compiled consumers.

**Observable acceptance:** Same semantic construction in alternate declaration order produces declared canonical IDs/results; unknown references reject; no Godot/BodySlot/Node/closure escapes. Execute actual GPU numerical behavior plus native host/admission controls, not only a DTO roundtrip.

**Dependencies / estimate boundary / stop:** Follows validated entry; source ownership contracts constrain representation choices. This compiler outcome must run its actual compiled candidate end to end. The declared admitted target construction remains runnable through every cut; unsupported current content rejects explicitly and remains required for final release; if compiler replacement cannot be consumed atomically without the install change, join p008-compile and p008-install into one coherent cutover rather than shipping an unused route.

<a id="p008-install"></a>
## P0-008 · p008-install

**Outcome / value:** Install or discard one complete construction atomically.

**Small closure / forward cutover:** Compiler candidate and SimulationCore install transaction transfer ownership at one commit. Update registration, maps, free lists, RNG and pending work together; retire piecemeal scene installation.

**Observable acceptance:** Inject each install failure: old world remains usable and identical; successful replacement retires old handles; duplicate/stale candidate cannot install twice.

**Dependencies / estimate boundary / stop:** Candidate generation/publication capacity decisions are fixed by P0-006; no partial implementation accepted.

<a id="p011-bounds"></a>
## P0-011 · p011-bounds

**Outcome / value:** A shared query cannot miss a declared interaction volume.

**Small closure / forward cutover:** Use current BodyBoundsTree geometry/invalidation evidence as reference; choose the GPU-owned conservative query needed by the named consumer, callers for contact/optical/acoustic/field queries declare conservative extents. Retire remaining authoritative all-pairs query path at its cutover.

**Observable acceptance:** Independent brute-force oracle covers hollow/rotating/grazing, disabled and owner-filtered bodies; no false negatives; topology and movement invalidate.

**Dependencies / estimate boundary / stop:** Review the named consumer's actual bounds first. Ball/workbench then rotated Ramp are the current scope; full all-law audit is not a prerequisite. No CPU index is prescribed and no speculative new abstraction is required.

<a id="p011-intervals"></a>
## P0-011 · p011-intervals

**Outcome / value:** Ordered surface/material intervals preserve pass-through and attenuation.

**Small closure / forward cutover:** Core query returns stable copied geometry/material identity; update optical, acoustic and radiation consumers using the common ordered interval contract. Remove bounding-box hit substitution.

**Observable acceptance:** Hollow bore versus wall, transparent then opaque, inside-origin and equal-distance ordering controls; retained snapshot keeps matching metadata revision.

**Dependencies / estimate boundary / stop:** Separate failure domain from broad-phase bounds; no material model silently selected.

<a id="p012-defect"></a>
## P0-012 · p012-defect

**Outcome / value:** Close one retained numerical defect with an independent oracle.

**Small closure / forward cutover:** Select one current failing invariant from the retained ledger; update its single solver/law owner and complete affected closure. Dead historical prototypes are evidence, never reinstated.

**Observable acceptance:** Original failure, nearby boundary, meaningful passing/control, ordering/scale and rollback checks; retain every unresolved ledger item.

**Dependencies / estimate boundary / stop:** Aggregate P0-012 closes only when every member is reconciled; this pattern names each selected failure before starting.

<a id="p013-scratch"></a>
## P0-013 · p013-scratch

**Outcome / value:** Remove one measured scratch allocation hotspot without changing results.

**Small closure / forward cutover:** Reuse existing specialized storage at the measured owner; update lifetime/reentrancy callers and clear/resize/dispose paths; remove prior allocation route.

**Observable acceptance:** Matched algorithm/topology controls show attributable allocations/time; empty/grow/shrink/reentry and rollback controls preserve results.

**Dependencies / estimate boundary / stop:** OPT-SCRATCH; measure first, no speculative universal store.

<a id="p013-checkpoint"></a>
## P0-013 · p013-checkpoint

**Outcome / value:** Reuse one checkpoint safely across transaction lifetimes.

**Small closure / forward cutover:** Keep SimulationTransaction ownership and participant order; update captured stores and cleanup together, remove duplicated checkpoint storage.

**Observable acceptance:** Failed capture/restore, nested admission rejection, variable topology and stale reuse; exact state equivalence and matched cost.

**Dependencies / estimate boundary / stop:** OPT-CHECKPOINT; cannot narrow rollback coverage to win time.

<a id="p013-predictor"></a>
## P0-013 · p013-predictor

**Outcome / value:** Avoid one repeated predictor computation with correct invalidation.

**Small closure / forward cutover:** Existing coupled solver owns reusable work; key all physically relevant topology/pose/load inputs and retire old repeated calculation in this closure.

**Observable acceptance:** Changed load/geometry/contact invalidates; unchanged input reuses; independent numerical oracle and matched predictor counts.

**Dependencies / estimate boundary / stop:** OPT-PREDICTOR; no cross-evaluation cache without complete input proof.

<a id="p013-sleep"></a>
## P0-013 · p013-sleep

**Outcome / value:** Sleep one quiescent coupled island and wake it correctly.

**Small closure / forward cutover:** SimulationCore owns sleep state; force/command/contact/topology changes wake dependent islands; remove any scene visibility influence on authority.

**Observable acceptance:** Near-threshold wake, connected awake load, queued event, moving contact and Reset; work/error budgets and matched cost.

**Dependencies / estimate boundary / stop:** OPT-SLEEP; genuine dependency closure required.

<a id="p014-state"></a>
## P0-014 · p014-state

**Outcome / value:** Publish only a complete committed physical state.

**Small closure / forward cutover:** Extend CommittedPoseBuffer owned generation/revision snapshots with all state covered by current contract; reserve capacity before commit and delete partial publication routes.

**Observable acceptance:** Failed tick leaks no poses/IDs/topology; old read lease stays coherent; zero/capacity/full/boundary revisions; readers cannot mutate producer storage.

**Dependencies / estimate boundary / stop:** P0-005/P0-006; ball-drop requires actual worker commit/pose proof now, before any global optimization completion. Pure commit unit checks supplement its visible behavior.

<a id="p014-events"></a>
## P0-014 · p014-events

**Outcome / value:** Committed occurrences and results are delivered once with their state.

**Small closure / forward cutover:** Core stages reliable events/results in same commit; host owns delivery/ack storage separately; no transient event escapes before commit.

**Observable acceptance:** Rollback emits nothing; retry cannot duplicate; capacity rejection leaves transaction unchanged; pose/result generation/revision agree.

**Dependencies / estimate boundary / stop:** State publication plus P0-005 event semantics; distinct reliability outcome.

<a id="p015-admit"></a>
## P0-015 · p015-admit

**Outcome / value:** A command is admitted or rejected deterministically without claiming application.

**Small closure / forward cutover:** SimulationCommandInbox/host own bounded admission; update all command producers to typed timed envelope, reject obsolete command routes.

**Observable acceptance:** Stale generation, duplicate ID, invalid enum, late tick and full queue controls; admitted status never reported as completed.

**Dependencies / estimate boundary / stop:** P0-005/P0-006 fixed command/commit policy; ball-drop proves its consumed admission/application boundary inside the vertical.

<a id="p015-apply"></a>
## P0-015 · p015-apply

**Outcome / value:** Replay applies each admitted command at the declared commit point.

**Small closure / forward cutover:** Core executes canonical tick/phase/order with dependent edits before Run; update replay/save callers and retire immediate scene mutation.

**Observable acceptance:** Same timed log across producer ordering/cadences yields identical state; rejection/failed tick cannot consume a result twice.

**Dependencies / estimate boundary / stop:** Admission complete; all coupled mutations participate.

<a id="p016-codec"></a>
## P0-016 · p016-codec

**Outcome / value:** The actual C#/browser boundary transmits one canonical typed protocol losslessly.

**Small closure / forward cutover:** Protocol codecs replace every current caller's ad hoc conversions; selectors stay enums and IDs typed internally; old encodings rejected.

**Observable acceptance:** Cross-language values at 2^53 and signed/unsigned limits, zero/default, overflow, NaN/infinity, truncation and unknown tags; real adapter, not fake-page-only.

**Dependencies / estimate boundary / stop:** P0-005 revised layout; the ball construction/Run/pose/Reset variants include their sender/receiver/application proof inside ball-drop. Extend a later message family only with its actual consumer.

<a id="p017-bootstrap"></a>
## P0-017 · p017-bootstrap

**Outcome / value:** A standalone simulation worker reaches verified readiness or explicit failure.

**Small closure / forward cutover:** SimulationHost owns runtime and imports no Godot payload; current launcher uses new manifest/schema readiness; remove any synchronous fallback.

**Observable acceptance:** Actual worker boot/export check, missing asset, malformed protocol, startup timeout and repeated start/dispose; identity logged.

**Dependencies / estimate boundary / stop:** Ball-drop includes its minimal actual compile/install/codec/device proof; native success is insufficient and complete compiler/codec parents are not predecessors.

<a id="p018-reliable"></a>
## P0-018 · p018-reliable

**Outcome / value:** Real-worker reliable results survive reordering and retransmission.

**Small closure / forward cutover:** Host/adapter own sequence, ack and bounded reliable queues; move all reliable message callers together, reject stale generations.

**Observable acceptance:** At least 10000 actual messages: no unexplained loss or double application; delay, reorder, duplicate and reconnect controls.

**Dependencies / estimate boundary / stop:** The named consumer's committed publication and actual worker readiness must pass together; P0-005/P0-006 fix queue/overflow policy. Full unrelated message variants are not predecessors.

<a id="p018-pressure"></a>
## P0-018 · p018-pressure

**Outcome / value:** Saturation cannot starve lifecycle or fabricate successful commands.

**Small closure / forward cutover:** Admission reserves lifecycle capacity; backpressure returns typed refusal; remove overwrite/drop behavior for reliable work.

**Observable acceptance:** Full data queue still admits required Reset/stop; overflow refusal preserves state; bounded queue/memory under stalled receiver.

**Dependencies / estimate boundary / stop:** Real transport in place; distinguish lossy pose replacement from reliable loss.

<a id="p018-transfer"></a>
## P0-018 · p018-transfer

**Outcome / value:** Each recipient receives independently owned bounded publication storage.

**Small closure / forward cutover:** Producer transfers/copies ownership per actual adapter contract; fan-out never aliases a transferred or mutable buffer.

**Observable acceptance:** Detached/reused buffer, two recipients with different delays, disposal and stale lease controls; bytes/copies/heap measured.

**Dependencies / estimate boundary / stop:** Separate ownership failure domain; not a shared mutable compatibility bridge.

<a id="p019-step"></a>
## P0-019 · p019-step

**Outcome / value:** Simulation worker is the only authoritative stepping owner.

**Small closure / forward cutover:** Move World.Step and full authority closure to SimulationHost; update scene callers to commands/committed samples and remove browser stepping calls and core references.

**Observable acceptance:** 120 Hz/four outer substeps initially; 250 ms browser stall leaves worker progressing; timed replay matches, no double stepping.

**Dependencies / estimate boundary / stop:** Ball-drop's real worker, bounded transport and commit criteria pass inside its one-path cutover. All-consumer extraction and full transport stress remain parent completion gates, not its starting prerequisites.

<a id="p019-debt"></a>
## P0-019 · p019-debt

**Outcome / value:** Overload is bounded and observable without changing physics time silently.

**Small closure / forward cutover:** SimulationHost monotonic scheduler owns debt/catch-up; remove browser cadence/timer ownership of simulation progress.

**Observable acceptance:** Clock jitter, long stall, hidden/resume and sustained overload boundaries; debt and missed-budget counters match fixed limits.

**Dependencies / estimate boundary / stop:** Same worker owner; topology-only comparison or honestly combined measurement.

<a id="p020-run"></a>
## P0-020 · p020-run

**Outcome / value:** Run/Pause/Step complete only after the defined worker acknowledgement.

**Small closure / forward cutover:** Host transition owner coordinates inbox/commit barrier and UI controls; move all current Run/Pause/Step callers together, remove synchronous state flips.

**Observable acceptance:** Delayed/repeated commands, step while paused, busy queue, stale ack and failure before/after commit; exact tick count.

**Dependencies / estimate boundary / stop:** P0-006 plus the actual consuming worker/commit criterion; ball-drop proves Run/Reset, construction-save adds Save/Load. Full stepping across all domains is not a prerequisite.

<a id="p020-reset"></a>
## P0-020 · p020-reset

**Outcome / value:** Reset restores exact construction across worker and presentation state.

**Small closure / forward cutover:** Host owns reset generation barrier; reset every runtime store/queue/map/animation binding via committed snapshot; retire scene-only reset.

**Observable acceptance:** Reset during Run/queued command/pending event/animation, repeated Reset, stale pre-reset ack and old handle; actual Chrome construction equality.

**Dependencies / estimate boundary / stop:** No old-world messages can mutate new generation.

<a id="p020-load"></a>
## P0-020 · p020-load

**Outcome / value:** Load atomically replaces the current construction or leaves it usable.

**Small closure / forward cutover:** Compiler candidate + host Load barrier update current UI/file inputs together; reject unsupported current schema rather than migrate.

**Observable acceptance:** Malformed/partial/unknown input, concurrent Reset/Load, late ack and compile/install failure; old world intact; valid replacement has one owner.

**Dependencies / estimate boundary / stop:** The admitted construction's actual atomic install criterion plus P0-006 transition contract; no accepted half-load or need to complete every compiler domain first.

<a id="p020-save"></a>
## P0-020 · p020-save

**Outcome / value:** Save captures one committed construction snapshot after the required barrier.

**Small closure / forward cutover:** Host serializes the P0-006 construction-only durable-save format; move Workshop save/reopen/input-gating callers together. Reject Save while Run is active and unsupported runtime-resume/old-schema inputs explicitly.

**Observable acceptance:** Save during pending edit, delayed ack, paused/running refusal and snapshot failure; reopen exact construction/IDs with no runtime stores smuggled into durable format and no mutable alias.

**Dependencies / estimate boundary / stop:** Current Workshop Save rejects _inRun. Preserve construction-only support; no new live-runtime resume feature.

<a id="p020-navigation"></a>
## P0-020 · p020-navigation

**Outcome / value:** Navigation cancels or completes pending work without cross-level mutation.

**Small closure / forward cutover:** Host owns retirement/new generation; UI transition waits for declared outcome and disposes old listeners/leases.

**Observable acceptance:** Navigate during Run/Load/save/Reset, double selection, old ack/event after new level; new puzzle remains usable.

**Dependencies / estimate boundary / stop:** All navigation callers move together; no stale generation reuse.

<a id="p021-outcome"></a>
## P0-021 · p021-outcome

**Outcome / value:** A worker crash reports the correct recoverable or uncertain outcome.

**Small closure / forward cutover:** Host records commit/transfer/ack states; recovery never silently replays an uncertain effect or runs browser physics.

**Observable acceptance:** Faults before commit, after commit, before/after transfer and lost ack each follow P0-006 matrix; explicit user recovery.

**Dependencies / estimate boundary / stop:** The affected consumer's Run/Reset/commit criteria and P0-006 recovery contract; device-loss handling is required now for ball-drop. No synchronous fallback or all-lifecycle prerequisite.

<a id="p021-restart"></a>
## P0-021 · p021-restart

**Outcome / value:** Recovery starts exactly one replacement worker from supported state.

**Small closure / forward cutover:** Host owns restart token/generation and timeout; update retry/Reset/UI callers and retire previous worker before accepting new traffic.

**Observable acceptance:** Double retry, late readiness, repeated crash, timeout then Reset, stale old-worker result; bounded recovery attempts.

**Dependencies / estimate boundary / stop:** Outcome classification must be complete before automatic replay choices.

<a id="p021-dispose"></a>
## P0-021 · p021-dispose

**Outcome / value:** Disposal releases workers, buffers, leases and listeners exactly once.

**Small closure / forward cutover:** Each host/presenter resource has one disposal owner; remove orphan subscriptions and live mutable references.

**Observable acceptance:** Repeated dispose, dispose during transfer/startup and subsequent reuse; 20 lifecycle cycles/transition proofs at required integration gate.

**Dependencies / estimate boundary / stop:** No new memory claim without actual retained heap measurements.

<a id="p022-evaluate"></a>
## P0-022 · p022-evaluate

**Outcome / value:** Portable animation samples match existing typed clip mathematics.

**Small closure / forward cutover:** Extract AnimationBatch evaluator/arrays with no scene references; current native callers switch together, old evaluator retired.

**Observable acceptance:** Every curve/repeat/property, endpoints, negative/large time policy, invalid/nonfinite definition and generation checks.

**Dependencies / estimate boundary / stop:** P0-004/P0-005 canonical values/writer ownership; Receiver halo is the first cosmetic consumer. Its simple feedback does not wait for every physical geometry family or all animation workers.

<a id="p022-writer"></a>
## P0-022 · p022-writer

**Outcome / value:** A presentation property has one typed writer for its lifetime.

**Small closure / forward cutover:** Animation owner handles register/start/stop/release; SceneAnimationAdapter becomes target boundary only; no duplicated evaluator.

**Observable acceptance:** Conflicting writer rejection, stale reused handle, stop Hold/RestoreInitial, resize/dispose and inactive allocation controls.

**Dependencies / estimate boundary / stop:** Existing arrays/handles reused, not a second owner.

<a id="p023-feedback"></a>
## P0-023 · p023-feedback

**Outcome / value:** One named feedback consumer responds to committed typed events.

**Small closure / forward cutover:** Use the five concrete consumer scopes below; map committed event to cosmetic clip without functional mutation and remove old callback for that consumer.

**Observable acceptance:** Late/overlap/offscreen/reduced-motion controls and unchanged physical timed replay; generation Reset/save rules per consumer.

**Dependencies / estimate boundary / stop:** Execute one feedback consumer at a time; all five remain mandatory children.

<a id="p023-lifecycle"></a>
## P0-023 · p023-lifecycle

**Outcome / value:** Animation registration follows committed lifecycle boundaries.

**Small closure / forward cutover:** Animation host owns generation/clip state; update Reset/Load/pause/event subscriptions together.

**Observable acceptance:** Old event after reset, dispose/reuse, overlap at generation boundary; no old clip rewrites new part.

**Dependencies / estimate boundary / stop:** Feedback semantics fixed before worker cutover.

<a id="p024-worker"></a>
## P0-024 · p024-worker

**Outcome / value:** A distinct second worker owns all animation evaluation.

**Small closure / forward cutover:** AnimationHost uses portable evaluator; remove main-thread animation evaluation calls, maintain only committed final application.

**Observable acceptance:** Initial60 Hz plus30/60/90/120 sampling; physics stall/pause permits animation; disabling animation leaves physical replay unchanged.

**Dependencies / estimate boundary / stop:** P0-018/P0-023; required architecture, never an optional experiment.

<a id="p025-map"></a>
## P0-025 · p025-map

**Outcome / value:** Monotonic worker clocks map to one bounded display time.

**Small closure / forward cutover:** Host/presenter exchange calibrated timestamps; replace Godot interpolation fraction as authority; retain native clock origins separately.

**Observable acceptance:** Synthetic origin offsets/drift/jitter/delayed calibration and measured real-worker mapping error/age; no backward display time.

**Dependencies / estimate boundary / stop:** P0-005/P0-006 fixed timing bounds plus actual ball committed-pose history now; autonomous animation adds its own clock/history criterion later. Full animation-worker completion was not a prerequisite of the historical first-pose checkpoint. The current [shared-clock outcome](../../shared-clock-cadence.md) requires P0-024/P0-025/P0-026 together: checkpoint 1 runs actual S and A workers with the existing hint, one master and bounded identity/cleanup safety; checkpoint 2 qualifies the active 120 Hz simulation/60 Hz animation/60 Hz presentation defaults, lifecycle, numerical/UI behavior and all affected acceptance criteria. Internal cadence capability remains available for future qualified changes, without player frequency controls. Checkpoint 1 does not grant terminal contract Pass.

<a id="p025-history"></a>
## P0-025 · p025-history

**Outcome / value:** Bounded histories sample coherent physical parent and cosmetic child times.

**Small closure / forward cutover:** Presenter owns read-only histories; remove mixed-revision/direct live-body reads and independent fractional sampling.

**Observable acceptance:** Missing/duplicate/reordered samples,100/250ms stalls, empty/full history and known discontinuity; declared exhaustion policy exactly.

**Dependencies / estimate boundary / stop:** Clock mapping first; no unbounded extrapolation or hidden wait.

<a id="p025-discontinuity"></a>
## P0-025 · p025-discontinuity

**Outcome / value:** Reset/Load/teleport cannot interpolate across incompatible generations.

**Small closure / forward cutover:** Presenter flushes/reseeds only at committed discontinuity stamp; all appearance/hierarchy callers use that boundary.

**Observable acceptance:** Old sample after reset, teleport at interpolation boundary, reparent/remove/reuse; no visual sweep through invalid state.

**Dependencies / estimate boundary / stop:** Distinct lifecycle consequence, same one display-time authority.

<a id="p026-apply"></a>
## P0-026 · p026-apply

**Outcome / value:** Godot applies coherent committed transforms/properties without waiting.

**Small closure / forward cutover:** Presenter owns final property application and physical-parent/cosmetic-child composition; remove live solver/query/evaluator calls.

**Observable acceptance:** Producer stalls keep coherent old view; apply each dirty property once/frame; chrome input remains responsive; no physical feedback.

**Dependencies / estimate boundary / stop:** Ball-drop's bounded committed-pose history/renderer boundary must pass now; full physical/cosmetic history variants remain aggregate P0-025 work. Audit forbidden assemblies/callers for this exact consumer.

<a id="p027-resource"></a>
## P0-027 · p027-resource

**Outcome / value:** Identical artwork shares immutable GPU resources safely.

**Small closure / forward cutover:** Presenter resource owner shares mesh/material/shader inputs; per-instance mutable parameters isolated; update all affected part constructors.

**Observable acceptance:** Change one instance color/scale/state; others unchanged; create/remove/recreate and render call/upload/memory comparison.

**Dependencies / estimate boundary / stop:** P0-026; preserve approved palette/form.

<a id="p027-dirty"></a>
## P0-027 · p027-dirty

**Outcome / value:** Only changed properties produce updates while invalidation remains complete.

**Small closure / forward cutover:** Typed dirty tracking replaces unconditional writers through current caller closure; resize/topology/camera/resource changes invalidate.

**Observable acceptance:** Unchanged frame zero redundant writes; each individual change updates once; remove/reuse/resize boundaries; measured uploads/calls.

**Dependencies / estimate boundary / stop:** Resource identity and lifetime are relevant inputs; no guessed savings.

<a id="p028-idle"></a>
## P0-028 · p028-idle

**Outcome / value:** Idle rendering wakes for every visible change.

**Small closure / forward cutover:** Presenter owns invalidate/request frame; UI/camera/animation/resource callbacks call it, old unconditional loops retired.

**Observable acceptance:** Idle then input/camera/UI/worker sample/animation resize; no stale frame; real display cadence proof separated synthetic tests.

**Dependencies / estimate boundary / stop:** P0-027; frame caps cannot stall workers.

<a id="p028-visible"></a>
## P0-028 · p028-visible

**Outcome / value:** Cosmetic visibility scheduling preserves authority and return appearance.

**Small closure / forward cutover:** Presenter may cull cosmetic work only; no visibility gate in core laws or goals; update shadow/offscreen policies.

**Observable acceptance:** Occlude/offscreen/hidden/reappear while physics runs; physical replay identical, latest coherent pose restored, shadows correct.

**Dependencies / estimate boundary / stop:** Required modes frozen before optimization.

<a id="p029-assets"></a>
## P0-029 · p029-assets

**Outcome / value:** A clean production export serves both exact worker artifacts.

**Small closure / forward cutover:** Build/deploy owns manifest and validated path/MIME/cache/compression; replace old single bundle assumptions and all launcher paths.

**Observable acceptance:** Clean Release export, missing/stale/wrong-schema assets fail visibly at actual origin; exact commit/build/deploy identities.

**Dependencies / estimate boundary / stop:** P0-021/P0-028; publication two-phase review retained.

<a id="p029-startup"></a>
## P0-029 · p029-startup

**Outcome / value:** Startup accounts for and bounds all three contexts and heaps.

**Small closure / forward cutover:** Existing performance record captures main+simulation+animation startup/copies/memory; no hidden omitted worker cost.

**Observable acceptance:** Cold/warm startup and actual device workload; unavailable device remains incomplete; numerical frozen caps.

**Dependencies / estimate boundary / stop:** Production artifact identity must match measurements.

<a id="p032-schedule"></a>
## P0-032 · p032-schedule

**Outcome / value:** Declared concurrency schedules produce the same timed physical replay.

**Small closure / forward cutover:** Attack actual three contexts at supported cadences; no new runtime diagnostic path shipped.

**Observable acceptance:** Delays/saturation/hidden resume, canonical reductions/IDs, reliable event reconciliation; diagnostic fault hooks absent production.

**Dependencies / estimate boundary / stop:** Integration qualification gate; findings produce one defect slice each.

<a id="p032-lifetime"></a>
## P0-032 · p032-lifetime

**Outcome / value:** Topology and resource reuse cannot resurrect stale state.

**Small closure / forward cutover:** Inject failed mutations, compaction and recycled handles across worker/presenter lifecycle.

**Observable acceptance:** Rollback all maps/free lists/queues; stale generation rejected; deterministic reordered storage;20cycles/20transitions when required.

**Dependencies / estimate boundary / stop:** Distinct from scheduling attacks; unchanged exhaustive gate remains.

<a id="p033-remediate"></a>
## P0-033 · p033-remediate

**Outcome / value:** Remove one measured remaining full-system bottleneck.

**Small closure / forward cutover:** Choose one attributable hotspot and exact changed closure; retire superseded route; do not bundle unrelated optimizations.

**Observable acceptance:** Matched before/after with frozen uncertainty, CPU/copy/alloc/frame tails and all affected part proof; no worsened scoped failures.

**Dependencies / estimate boundary / stop:** P0-033 remains aggregate; unknown cost triggers bounded measurement then named fix.

<a id="p034-device"></a>
## P0-034 · p034-device

**Outcome / value:** One declared device/workload meets every applicable frozen budget.

**Small closure / forward cutover:** Use existing device01/02/03 owner record; exact full deployed artifacts and complete actual UI attempts.

**Observable acceptance:** >=3matched attempts,3600ticks/explicit early-goal,>=5minute continuous thermal,20cycles/20transitions; missing hardware incomplete.

**Dependencies / estimate boundary / stop:** Qualification unit is device/workload, all units required; no averaged-away failure.

<a id="wasm-experiment"></a>
## S044 · wasm-experiment

**Outcome / value:** Decide one supported Wasm compilation change from matched evidence.

**Small closure / forward cutover:** Change only a supported compilation option in isolated measured candidate; no runtime compatibility flag or dual shipped format.

**Observable acceptance:** Same source/algorithm/workload outputs, startup/download/memory/CPU; accept only attributable net benefit, otherwise retain current build and recorded rejection.

**Dependencies / estimate boundary / stop:** PERF-12 bounded experiment ends with adopt/reject; required worker implementation is not optional.

<a id="p023-motor"></a>
## P0-023 · motor feedback

**Outcome / value:** Physically functional shaft/contact geometry depicts committed physical state; decorative motor spin may run independently in the animation owner as its source specifies, without inventing a physics body or angle ledger solely for visuals.

**Small closure / cutover:** Replace this consumer's executable scene callback with typed event/feedback, generation/clip and disposal declarations consumed by the shared AnimationKernel/AnimationHost evaluators. Do not copy a per-element callback/evaluator into the worker; extend a reusable typed capability if needed and delete the former scene evaluator/callback writer. Final Godot application remains presenter-only. This is one consumer, not all feedback at once.

**Observable acceptance:** Stall or disable cosmetic animation: torque/work and physical replay stay equal. Where declared, decorative spin continues with physics absent or paused. Reverse/stop/stale generation/reduced motion obey each functional-versus-decorative contract; no cosmetic angle feeds contact physics. Retain this consumer's declared visibility, late-event, overlap, lifecycle/save and real-Chrome proof.

**Dependencies / estimate boundary:** The named consumer's committed-event and evaluator/writer criteria within P0-014/P0-022; inspect this consumer's actual current source and applicable mode list. Choose clip timing/shape within fixed physical separation and source policies.

<a id="p023-indicator"></a>
## P0-023 · indicator feedback

**Outcome / value:** Indicator reads committed sensor/control state; animation owns only glow/pulse appearance and never the signal itself.

**Small closure / cutover:** Replace this consumer's executable scene callback with typed event/feedback, generation/clip and disposal declarations consumed by the shared AnimationKernel/AnimationHost evaluators. Do not copy a per-element callback/evaluator into the worker; extend a reusable typed capability if needed and delete the former scene evaluator/callback writer. Final Godot application remains presenter-only. This is one consumer, not all feedback at once.

**Observable acceptance:** False/no-supply control stays functionally false despite a fading glow; rapid true/false events, late samples, overlap and Reset cannot latch a phantom signal. Retain this consumer's declared visibility, late-event, overlap, lifecycle/save and real-Chrome proof.

**Dependencies / estimate boundary:** The named consumer's committed-event and evaluator/writer criteria within P0-014/P0-022; inspect this consumer's actual current source and applicable mode list. Choose clip timing/shape within fixed physical separation and source policies.

<a id="p023-recoil"></a>
## P0-023 · recoil feedback

**Outcome / value:** One committed launch event causes a cosmetic recoil clip while launch impulse remains solely core-owned.

**Small closure / cutover:** Replace this consumer's executable scene callback with typed event/feedback, generation/clip and disposal declarations consumed by the shared AnimationKernel/AnimationHost evaluators. Do not copy a per-element callback/evaluator into the worker; extend a reusable typed capability if needed and delete the former scene evaluator/callback writer. Final Godot application remains presenter-only. This is one consumer, not all feedback at once.

**Observable acceptance:** One actual shot gives one recoil; no shot/blocked rejected command gives none, duplicates cannot add recoil or impulse, overlapping shots follow fixed policy; Reset cancels old clips. Retain this consumer's declared visibility, late-event, overlap, lifecycle/save and real-Chrome proof.

**Dependencies / estimate boundary:** The named consumer's committed-event and evaluator/writer criteria within P0-014/P0-022; inspect this consumer's actual current source and applicable mode list. Choose clip timing/shape within fixed physical separation and source policies.

<a id="p023-ui"></a>
## P0-023 · ui feedback

**Outcome / value:** UI transition/pending/success clips follow acknowledged state, never complete Run or navigation themselves.

**Small closure / cutover:** Replace this consumer's executable scene callback with typed event/feedback, generation/clip and disposal declarations consumed by the shared AnimationKernel/AnimationHost evaluators. Do not copy a per-element callback/evaluator into the worker; extend a reusable typed capability if needed and delete the former scene evaluator/callback writer. Final Godot application remains presenter-only. This is one consumer, not all feedback at once.

**Observable acceptance:** Delayed/rejected acknowledgement remains pending/error regardless of clip completion; skip/reduced-motion and pause keep controls usable; stale old-level callback cannot close current UI. Retain this consumer's declared visibility, late-event, overlap, lifecycle/save and real-Chrome proof.

**Dependencies / estimate boundary:** The named consumer's committed-event and evaluator/writer criteria within P0-014/P0-022; inspect this consumer's actual current source and applicable mode list. Choose clip timing/shape within fixed physical separation and source policies.

<a id="p023-acoustic"></a>
## P0-023 · acoustic feedback

**Outcome / value:** A committed acoustic occurrence drives the declared sound/visual feedback while finite acoustic propagation remains core-owned.

**Small closure / cutover:** Replace this consumer's executable scene callback with typed event/feedback, generation/clip and disposal declarations consumed by the shared AnimationKernel/AnimationHost evaluators. Do not copy a per-element callback/evaluator into the worker; extend a reusable typed capability if needed and delete the former scene evaluator/callback writer. Final Godot application remains presenter-only. This is one consumer, not all feedback at once.

**Observable acceptance:** Silence/no power/wrong tone does not fabricate occurrence; late/duplicate/overlap events obey audibility policy; offscreen/reduced-motion/disabled audio cannot change receiver or physical replay. Retain this consumer's declared visibility, late-event, overlap, lifecycle/save and real-Chrome proof.

**Dependencies / estimate boundary:** The named consumer's committed-event and evaluator/writer criteria within P0-014/P0-022; inspect this consumer's actual current source and applicable mode list. Choose clip timing/shape within fixed physical separation and source policies.

## Consumer scope for retained engine cards

Read [GPU criteria](gpu-physics.md) for each changed numerical/backend criterion and [the vertical sequence](vertical-delivery.md) for its actual consumer. Existing cards that describe all messages, domains, histories or resources are aggregate acceptance inventories. They do not require implementing unrelated variants before the named consumer; nor does one consumer close the parent. Ball-drop owns minimal Run/Reset/commit/device/pose support, basket-capture adds residence/event/halo, construction-save adds Save/Load, switch-lamp adds a typed activation edge, and first-principles adds rotated Ramp contact and Captured success. Remaining named motor/indicator/recoil/UI/acoustic cards retain their distinct contracts and later consuming scope.
