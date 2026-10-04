# Source scopes and aggregate gates

**Current architecture:** [Canonical Half game values and WGSL f16 physics](../../gpu-f16-physics.md) define the numerical model. Current design/acceptance is self-contained; implementation and qualification status are in [TODO](../../../TODO.md).

All substantive source criteria and current reviewed technical/stage prerequisites remain mandatory. [Vertical delivery](vertical-delivery.md) and [scope corrections](scope-corrections.md) supersede old horizontal scheduling and generic ready labels. These are scoped labels under existing canonical IDs, not new work orders. A bounded label is not readiness or a Pass. Read the complete linked current acceptance. Historical reports are optional provenance and cannot supply a missing design requirement or an execution prerequisite. At cutover name the sole target owner, update affected callers/content/tests together, delete/reject the obsolete route and prove it unreachable. Choices within the fixed architecture remain negotiable; unknown model/parameter questions end in one bounded decision with the same named implementation next. Positive/control/boundary and all required build/Chrome/Reset/save/performance/publication proof remain at the original stage.

[Execution index](../invest-index.md) · [Family constraints](profiles.md) · [Explicit broad splits](refinements.md).

<a id="p0-001"></a>
### P0-001 · Capture the untouched engine baseline

Existing contract/baseline gate; use applicable verdict, reopen only affected criteria. **Source outcome:** Record source, dirty-diff and served-bundle hashes; exact current build/test/export commands; current numerical, UI and performance failures.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-001).


 **Named criteria:** source: Exact untouched source/dirty context and served artifact identity; commands: Actual current build/test/export entry points; failures: Retained numerical, actual-UI and performance failures.

<a id="p0-002"></a>
### P0-002 · Freeze the complete required engine capability inventory

Existing contract/baseline gate; use applicable verdict, reopen only affected criteria. **Source outcome:** Map all 799 retained specifications, catalogue entries, fixtures and linked research to named laws, consumers and proof owners; zero orphan required behavior. Future product declarations are not engine prerequisites. Inventory the current timer/animation arrays, BodyBoundsTree and transaction participants with source hashes, existing proof and remaining gaps under the reuse/state contract; ECS adoption and experiments are deferred, not prerequisites.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-002).


 **Named criteria:** source-members: Every current source binding, named law/consumer and proof owner; existing-owners: Timer/animation/spatial/transaction reuse and missing capabilities.

<a id="p0-003"></a>
### P0-003 · Freeze workload sizes, device tiers and numerical budgets

Existing contract/baseline gate; use applicable verdict, reopen only affected criteria. **Source outcome:** Publish exact sparse/doubled/dense/mixed fixtures and counts, device tiers, precision/conservation tolerances, startup/memory caps and required-now versus retained-failure gates. Freeze the [runnable measurement contract](../requirements.md#measurement-contract): prior-attempt warm-up, complete Run boundaries, 3600-tick/early-goal cases, minimum observations, three matched pairs, continuous five-minute thermal recipe/gap cap and noise/uncertainty oracles. Absent hardware stays incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-003).


 **Named criteria:** workloads: Exact sparse/doubled/dense/mixed fixture counts; devices: Every required actual device tier; budgets: Numerical/performance/startup/memory limits and full measurement recipe.

<a id="p0-004"></a>
### P0-004 · Freeze assemblies and exclusive state/property ownership

Existing contract/baseline gate; use applicable verdict, reopen only affected criteria. **Source outcome:** Name portable geometry/core, construction compiler, protocol, simulation host, animation kernel/host and Godot presenter boundaries; every mutable field/property has one owner and all callers are mapped. Preserve one solver owner per authoritative quantity, typed stable identity/generation and value-only bindings; storage slots/order are internal. Map reuse/extraction of existing timer/animation/spatial/transaction owners before replacing any path.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-004).


 **Named criteria:** assemblies: Allowed target C# assembly dependencies; state-ownership: Exactly one writer for every mutable authority; stable-identity: Typed IDs/generation independent of slots/order; reuse: Current specialized owner extraction rather than duplicate infrastructure.

<a id="p0-005"></a>
### P0-005 · Freeze wire schema and timed-command replay

Existing contract/baseline gate; use applicable verdict, reopen only affected criteria. **Source outcome:** Define fixed-width lossless IDs, message variants/lengths, units, sequence/generation, application tick/phase/order, capacities, ownership, acknowledgement windows and late/duplicate handling; no unresolved mandatory field. Worker/save identities must survive local storage compaction/reordering; slots never cross these boundaries. Canonical ordering is explicit, not incidental traversal order.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-005).


 **Named criteria:** wire: Lossless fields, variants/lengths/units/capacities; replay: Tick/phase/order and late/duplicate semantics; identity: Stable lifetime/generation and compaction independence; reliability: Ownership, ack and delivery windows.

<a id="p0-006"></a>
### P0-006 · Freeze lifecycle, clocks and crash recovery

Existing contract/baseline gate; use applicable verdict, reopen only affected criteria. **Source outcome:** State/input/error transition matrix fixes commit/ack points, failed Load atomicity, construction/runtime-save support, timeout and recovery limits; clock mapping, interpolation delay/history and overload limits are numerical. Enumerate every existing topology/registration mutation and affected mapping/free-list/queued-event or command state in commit/rollback; retain RNG state where used. A queued command is not a completed transaction.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-006).


 **Named criteria:** lifecycle: Every state/input/error commit and ack transition; clocks: Origin mapping/history/delay/overload bounds; mutation: Every topology/registration/queue/free-list/RNG transaction; recovery: Fault/timeout/restart/uncertainty and construction-only save rules.

<a id="check-schema"></a>
### CHECK-SCHEMA · Freeze criterion/evidence schema and examples

Supporting enforcement or closure gate; not a product story. **Source outcome:** Separate Pass/Fail/Incomplete/justified NotApplicable; criterion IDs, expected/observed/tolerance, provenance, frontier cases and deferred-gate owners. Freeze typed implementation/reviewer agent and session IDs/roles, distinct-identity rule, reviewed revision/diff/contract/artifact hashes, reviewer-run checks, criterion and direct/transitive impact matrices, review verdict, findings/disposition, re-review links and publication receipt identity. Define a separate enum-typed SnapshotApproval decision, criterion publication-dependency/phase, approved snapshot hashes and required deployed checks; publication approval is nonterminal and task completion remains Incomplete until receipts and all deployed criteria pass. Review statuses/roles are enums; identities are strongly typed. Define negative oracles: same agent in both roles or a reviewer-authored fix accepted by that reviewer is Fail; missing review/impact coverage or stale reviewed hashes is Incomplete; unresolved regressions or fabricated verdicts is Fail; only a genuine distinct matching-snapshot review with all required-now criteria passes. Freeze two-phase oracles: valid pre-publication approval with named receipt/deployed criteria pending authorizes publication but returns Incomplete for task completion; complete matching post-publication evidence permits final Pass; premature final Pass, bypassed pre-publication checks or publication of an unapproved/changed snapshot is Fail.

[Complete criteria/status/technical predecessors](../work-register.md#work-check-schema).


 **Exact closure members:** [P0-003](../work-register.md#work-p0-003). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="check-graph"></a>
### CHECK-GRAPH · Freeze intended-configuration validator oracles

Supporting enforcement or closure gate; not a product story. **Source outcome:** Specify exact fixtures: wrong endpoint/mode with matching Run/Reset must produce Fail when executed by CHECK-AGGREGATE; missing lifecycle/revision is Incomplete; actual graph equals recipe expectation, not merely itself after Reset.

[Complete criteria/status/technical predecessors](../work-register.md#work-check-graph).


 **Exact closure members:** [CHECK-SCHEMA](../work-register.md#work-check-schema). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="check-metrics"></a>
### CHECK-METRICS · Freeze metric-separation validator oracles

Supporting enforcement or closure gate; not a product story. **Source outcome:** Specify exact fixtures for CHECK-AGGREGATE: 3600 covered ticks over budget: coverage Pass and performance Fail. Screenshots/synthetic callbacks cannot prove actual FPS/motion; stale artifacts stay Incomplete. Add independently derived verdict fixtures for short early wins, timeout/wall-time mismatch, partial warm-up captures, stitched active windows, missing thermal gaps, incomparable topology, false attribution, new/worsened failure and an unchanged retained baseline failure. Valid baseline observation passes only Baseline; engine budget failure remains Fail and missing observations Incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-check-metrics).


 **Exact closure members:** [CHECK-SCHEMA](../work-register.md#work-check-schema). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="check-aggregate"></a>
### CHECK-AGGREGATE · Implement adversarial dependency/completion checking

Supporting enforcement or closure gate; not a product story. **Source outcome:** Implement schema validation and configuration/metric verdicts from all three frozen CHECK records; execute every original/mutated oracle and retain observed versus expected verdicts. Implement the criterion/requirement checker: Exit0 only if every applicable criterion and standing REQ passes; exit1 violation; exit2 missing/stale. Reject cycles, parent-before-child, absent REQ ownership, fabricated exemption/inventory flags and contaminated runs; mutate each requirement to prove rejection. Enforce REQ-14 from frozen CHECK-SCHEMA: distinct actual implementation/reviewer identities, current reviewed snapshot and artifact hashes, complete required-now criterion/impact coverage, independently attributable checks, zero unresolved findings and matching publication receipts. Execute positive and rejection fixtures for same-agent review, reviewer-authored unreviewed fix, missing reviewer/verdict/impact matrix, stale revision/diff/artifact hashes, unexplored transitive impacts, unresolved regressions, fabricated review flags and a deliverable changed after approval. Same-agent/known violation is exit1; missing/stale coverage is exit2; only genuine independent current proof is exit0. Prove valid stage deferral is not mistaken for missing later U proof. Implement the distinct SnapshotApproval and terminal-completion gates: a valid frozen pre-publication approval permits publication only; the task completion query remains exit2 until every named receipt/deployed criterion passes. A premature final Pass or bypass of required pre-publication checks is exit1. Exercise positive publication-then-verification and negative missing/wrong receipts, deployed regression and changed-approved-snapshot oracles; only final matching complete evidence yields task-completion exit0. Until its own implementation passes independent review, validate its closure using retained manual proof; it cannot self-certify its checker implementation.

[Complete criteria/status/technical predecessors](../work-register.md#work-check-aggregate).


 **Exact closure members:** [CHECK-GRAPH](../work-register.md#work-check-graph), [CHECK-METRICS](../work-register.md#work-check-metrics). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s001"></a>
### S001 · Audit retained scope: Record the untouched source, bundle and failure baseline

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** zero unassigned consumers or orphan requirements; documented current production baseline and retained failures; each threshold has a unit, sampling rule and oracle before acceptance runs. Planning does not claim speedup. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-001).


 **Exact closure members:** [P0-001](../work-register.md#work-p0-001), [P0-029](../work-register.md#work-p0-029). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s002"></a>
### S002 · Audit retained scope: Fix the benchmark manifest and task-specific thresholds before implementation

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** zero unassigned consumers or orphan requirements; documented current production baseline and retained failures; each threshold has a unit, sampling rule and oracle before acceptance runs. Planning does not claim speedup. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-002).


 **Exact closure members:** [P0-003](../work-register.md#work-p0-003), [P0-034](../work-register.md#work-p0-034). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s003"></a>
### S003 · Audit retained scope: Execution preparation — canonical task and coverage ledger

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** Execution preparation — canonical task and coverage ledger. Instantiate PERF-23 and the existing per-element/connection ledgers with every required named element, fixture and supported mode from TODO and linked research. Give each record one stable identity, source links, prerequisites, affected shared processes, implementation… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-003).


 **Exact closure members:** [CAT-001-V](../work-register.md#work-cat-001-v), [CAT-002-V](../work-register.md#work-cat-002-v), [CAT-003-V](../work-register.md#work-cat-003-v), [CAT-004-V](../work-register.md#work-cat-004-v), [CAT-005-V](../work-register.md#work-cat-005-v), [CAT-006-V](../work-register.md#work-cat-006-v), [CAT-007-V](../work-register.md#work-cat-007-v), [CAT-008-V](../work-register.md#work-cat-008-v), [CAT-009-V](../work-register.md#work-cat-009-v), [CAT-010-V](../work-register.md#work-cat-010-v), [CAT-011-V](../work-register.md#work-cat-011-v), [CAT-012-V](../work-register.md#work-cat-012-v), [CAT-013-V](../work-register.md#work-cat-013-v), [CAT-014-V](../work-register.md#work-cat-014-v), [CAT-015-V](../work-register.md#work-cat-015-v), [CAT-016-V](../work-register.md#work-cat-016-v), [CAT-017-V](../work-register.md#work-cat-017-v), [CAT-018-V](../work-register.md#work-cat-018-v), [CAT-019-V](../work-register.md#work-cat-019-v), [CAT-020-V](../work-register.md#work-cat-020-v), [CAT-021-V](../work-register.md#work-cat-021-v), [CAT-022-V](../work-register.md#work-cat-022-v), [CAT-023-V](../work-register.md#work-cat-023-v), [CAT-024-V](../work-register.md#work-cat-024-v), [CAT-025-V](../work-register.md#work-cat-025-v), [CAT-026-V](../work-register.md#work-cat-026-v), [CAT-027-V](../work-register.md#work-cat-027-v), [CAT-028-V](../work-register.md#work-cat-028-v), [CAT-029-V](../work-register.md#work-cat-029-v), [CAT-030-V](../work-register.md#work-cat-030-v), [CAT-031-V](../work-register.md#work-cat-031-v), [CAT-032-V](../work-register.md#work-cat-032-v), [CAT-033-V](../work-register.md#work-cat-033-v), [CAT-034-V](../work-register.md#work-cat-034-v), [CAT-035-V](../work-register.md#work-cat-035-v), [CAT-036-V](../work-register.md#work-cat-036-v), [CAT-037-V](../work-register.md#work-cat-037-v), [CAT-038-V](../work-register.md#work-cat-038-v), [CAT-039-V](../work-register.md#work-cat-039-v), [CAT-040-V](../work-register.md#work-cat-040-v), [CAT-041-V](../work-register.md#work-cat-041-v), [CAT-042-V](../work-register.md#work-cat-042-v), [CAT-043-V](../work-register.md#work-cat-043-v), [CAT-044-V](../work-register.md#work-cat-044-v), [CAT-045-V](../work-register.md#work-cat-045-v), [CAT-046-V](../work-register.md#work-cat-046-v), [CAT-047-V](../work-register.md#work-cat-047-v), [CAT-048-V](../work-register.md#work-cat-048-v), [CAT-049-V](../work-register.md#work-cat-049-v), [CAT-050-V](../work-register.md#work-cat-050-v), [CAT-051-V](../work-register.md#work-cat-051-v), [CAT-052-V](../work-register.md#work-cat-052-v), [CAT-053-V](../work-register.md#work-cat-053-v), [CAT-054-V](../work-register.md#work-cat-054-v), [CAT-055-V](../work-register.md#work-cat-055-v), [CAT-056-V](../work-register.md#work-cat-056-v), [CAT-057-V](../work-register.md#work-cat-057-v), [CAT-058-V](../work-register.md#work-cat-058-v), [CAT-059-V](../work-register.md#work-cat-059-v), [CAT-060-V](../work-register.md#work-cat-060-v), [CAT-061-V](../work-register.md#work-cat-061-v), [CAT-062-V](../work-register.md#work-cat-062-v), [CAT-063-V](../work-register.md#work-cat-063-v), [CAT-064-V](../work-register.md#work-cat-064-v), [CAT-065-V](../work-register.md#work-cat-065-v), [CAT-066-V](../work-register.md#work-cat-066-v), [CAT-067-V](../work-register.md#work-cat-067-v), [CAT-068-V](../work-register.md#work-cat-068-v), [CAT-069-V](../work-register.md#work-cat-069-v), [CAT-070-V](../work-register.md#work-cat-070-v), [CAT-071-V](../work-register.md#work-cat-071-v), [CAT-072-V](../work-register.md#work-cat-072-v), [FIX-001-001](../work-register.md#work-fix-001-001), [FIX-001-002](../work-register.md#work-fix-001-002), [FIX-001-003](../work-register.md#work-fix-001-003), [FIX-001-004](../work-register.md#work-fix-001-004), [FIX-001-005](../work-register.md#work-fix-001-005), [FIX-001-006](../work-register.md#work-fix-001-006), [FIX-001-007](../work-register.md#work-fix-001-007), [FIX-001-008](../work-register.md#work-fix-001-008), [FIX-001-009](../work-register.md#work-fix-001-009), [FIX-001-010](../work-register.md#work-fix-001-010), [FIX-001-011](../work-register.md#work-fix-001-011), [FIX-001-012](../work-register.md#work-fix-001-012), [FIX-001-013](../work-register.md#work-fix-001-013), [FIX-001-014](../work-register.md#work-fix-001-014), [FIX-001-015](../work-register.md#work-fix-001-015), [FIX-001-016](../work-register.md#work-fix-001-016), [FIX-001-017](../work-register.md#work-fix-001-017), [FIX-001-018](../work-register.md#work-fix-001-018), [FIX-001-019](../work-register.md#work-fix-001-019), [FIX-001-020](../work-register.md#work-fix-001-020), [FIX-001-021](../work-register.md#work-fix-001-021), [FIX-001-022](../work-register.md#work-fix-001-022), [FIX-001-023](../work-register.md#work-fix-001-023), [FIX-001-024](../work-register.md#work-fix-001-024), [FIX-001-025](../work-register.md#work-fix-001-025), [FIX-001-026](../work-register.md#work-fix-001-026), [FIX-001-027](../work-register.md#work-fix-001-027), [FIX-001-028](../work-register.md#work-fix-001-028), [FIX-001-029](../work-register.md#work-fix-001-029), [FIX-001-030](../work-register.md#work-fix-001-030), [FIX-001-031](../work-register.md#work-fix-001-031), [FIX-001-032](../work-register.md#work-fix-001-032), [FIX-001-033](../work-register.md#work-fix-001-033), [FIX-001-034](../work-register.md#work-fix-001-034), [FIX-001-035](../work-register.md#work-fix-001-035), [FIX-001-036](../work-register.md#work-fix-001-036), [FIX-001-037](../work-register.md#work-fix-001-037), [FIX-001-038](../work-register.md#work-fix-001-038), [FIX-001-039](../work-register.md#work-fix-001-039), [FIX-001-040](../work-register.md#work-fix-001-040), [FIX-001-041](../work-register.md#work-fix-001-041), [FIX-001-042](../work-register.md#work-fix-001-042), [FIX-001-043](../work-register.md#work-fix-001-043), [FIX-001-044](../work-register.md#work-fix-001-044), [FIX-001-045](../work-register.md#work-fix-001-045), [FIX-001-046](../work-register.md#work-fix-001-046), [FIX-001-047](../work-register.md#work-fix-001-047), [FIX-001-048](../work-register.md#work-fix-001-048), [FIX-001-049](../work-register.md#work-fix-001-049), [FIX-001-050](../work-register.md#work-fix-001-050), [FIX-001-051](../work-register.md#work-fix-001-051), [FIX-001-052](../work-register.md#work-fix-001-052), [FIX-001-053](../work-register.md#work-fix-001-053), [FIX-001-054](../work-register.md#work-fix-001-054), [FIX-001-055](../work-register.md#work-fix-001-055), [FIX-001-056](../work-register.md#work-fix-001-056), [FIX-001-057](../work-register.md#work-fix-001-057), [FIX-001-058](../work-register.md#work-fix-001-058), [FIX-001-059](../work-register.md#work-fix-001-059), [FIX-001-060](../work-register.md#work-fix-001-060), [FIX-001-061](../work-register.md#work-fix-001-061), [FIX-001-062](../work-register.md#work-fix-001-062), [FIX-001-063](../work-register.md#work-fix-001-063), [FIX-004-001](../work-register.md#work-fix-004-001), [FIX-004-002](../work-register.md#work-fix-004-002), [FIX-004-003](../work-register.md#work-fix-004-003), [FIX-004-004](../work-register.md#work-fix-004-004), [FIX-004-005](../work-register.md#work-fix-004-005), [FIX-004-006](../work-register.md#work-fix-004-006), [FIX-004-007](../work-register.md#work-fix-004-007), [FIX-004-008](../work-register.md#work-fix-004-008), [FIX-004-009](../work-register.md#work-fix-004-009), [FIX-004-010](../work-register.md#work-fix-004-010), [FIX-004-011](../work-register.md#work-fix-004-011), [FIX-004-012](../work-register.md#work-fix-004-012), [FIX-004-013](../work-register.md#work-fix-004-013), [FIX-004-014](../work-register.md#work-fix-004-014), [FIX-004-015](../work-register.md#work-fix-004-015), [FIX-004-016](../work-register.md#work-fix-004-016), [FIX-004-017](../work-register.md#work-fix-004-017), [FIX-004-018](../work-register.md#work-fix-004-018), [FIX-004-019](../work-register.md#work-fix-004-019), [FIX-004-020](../work-register.md#work-fix-004-020), [FIX-004-021](../work-register.md#work-fix-004-021), [FIX-004-022](../work-register.md#work-fix-004-022), [FIX-004-023](../work-register.md#work-fix-004-023), [FIX-004-024](../work-register.md#work-fix-004-024), [FIX-004-025](../work-register.md#work-fix-004-025), [FIX-004-026](../work-register.md#work-fix-004-026), [FIX-004-027](../work-register.md#work-fix-004-027), [FIX-004-028](../work-register.md#work-fix-004-028), [FIX-004-029](../work-register.md#work-fix-004-029), [FIX-004-030](../work-register.md#work-fix-004-030), [FIX-004-031](../work-register.md#work-fix-004-031), [FIX-004-032](../work-register.md#work-fix-004-032), [FIX-004-033](../work-register.md#work-fix-004-033), [FIX-004-034](../work-register.md#work-fix-004-034), [FIX-004-035](../work-register.md#work-fix-004-035), [FIX-004-036](../work-register.md#work-fix-004-036), [FIX-004-037](../work-register.md#work-fix-004-037), [FIX-004-038](../work-register.md#work-fix-004-038), [FIX-004-039](../work-register.md#work-fix-004-039), [FIX-004-040](../work-register.md#work-fix-004-040), [FIX-004-041](../work-register.md#work-fix-004-041), [FIX-004-042](../work-register.md#work-fix-004-042), [FIX-004-043](../work-register.md#work-fix-004-043), [FIX-004-044](../work-register.md#work-fix-004-044), [FIX-004-045](../work-register.md#work-fix-004-045), [FIX-004-046](../work-register.md#work-fix-004-046), [FIX-004-047](../work-register.md#work-fix-004-047), [FIX-004-048](../work-register.md#work-fix-004-048), [FIX-004-049](../work-register.md#work-fix-004-049), [FIX-004-050](../work-register.md#work-fix-004-050), [FIX-004-051](../work-register.md#work-fix-004-051), [FIX-004-052](../work-register.md#work-fix-004-052), [FIX-004-053](../work-register.md#work-fix-004-053), [FIX-004-054](../work-register.md#work-fix-004-054), [FIX-004-055](../work-register.md#work-fix-004-055), [FIX-004-056](../work-register.md#work-fix-004-056), [FIX-004-057](../work-register.md#work-fix-004-057), [FIX-005-001](../work-register.md#work-fix-005-001), [FIX-005-002](../work-register.md#work-fix-005-002), [FIX-005-003](../work-register.md#work-fix-005-003), [FIX-005-004](../work-register.md#work-fix-005-004), [FIX-005-005](../work-register.md#work-fix-005-005), [FIX-005-006](../work-register.md#work-fix-005-006), [FIX-005-007](../work-register.md#work-fix-005-007), [FIX-005-008](../work-register.md#work-fix-005-008), [FIX-005-009](../work-register.md#work-fix-005-009), [FIX-005-010](../work-register.md#work-fix-005-010), [FIX-005-011](../work-register.md#work-fix-005-011), [FIX-005-012](../work-register.md#work-fix-005-012), [FIX-005-013](../work-register.md#work-fix-005-013), [FIX-014-001](../work-register.md#work-fix-014-001), [FIX-014-002](../work-register.md#work-fix-014-002), [FIX-014-003](../work-register.md#work-fix-014-003), [FIX-014-004](../work-register.md#work-fix-014-004), [FIX-014-005](../work-register.md#work-fix-014-005), [FIX-014-006](../work-register.md#work-fix-014-006), [FIX-014-007](../work-register.md#work-fix-014-007), [FIX-014-008](../work-register.md#work-fix-014-008), [FIX-014-009](../work-register.md#work-fix-014-009), [FIX-014-010](../work-register.md#work-fix-014-010), [FIX-014-011](../work-register.md#work-fix-014-011), [FIX-014-012](../work-register.md#work-fix-014-012), [FIX-014-013](../work-register.md#work-fix-014-013), [FIX-014-014](../work-register.md#work-fix-014-014), [FIX-014-015](../work-register.md#work-fix-014-015), [FIX-014-016](../work-register.md#work-fix-014-016), [FIX-014-017](../work-register.md#work-fix-014-017), [FIX-014-018](../work-register.md#work-fix-014-018), [FIX-014-019](../work-register.md#work-fix-014-019), [FIX-014-020](../work-register.md#work-fix-014-020), [FIX-014-021](../work-register.md#work-fix-014-021), [FIX-014-022](../work-register.md#work-fix-014-022), [FIX-014-023](../work-register.md#work-fix-014-023), [FIX-014-024](../work-register.md#work-fix-014-024), [FIX-014-025](../work-register.md#work-fix-014-025), [FIX-014-026](../work-register.md#work-fix-014-026), [FIX-014-027](../work-register.md#work-fix-014-027), [FIX-014-028](../work-register.md#work-fix-014-028), [FIX-014-029](../work-register.md#work-fix-014-029), [FIX-014-030](../work-register.md#work-fix-014-030), [FIX-015-001](../work-register.md#work-fix-015-001), [FIX-019-001](../work-register.md#work-fix-019-001), [FIX-019-002](../work-register.md#work-fix-019-002), [FIX-022-001](../work-register.md#work-fix-022-001), [FIX-022-002](../work-register.md#work-fix-022-002), [FIX-022-003](../work-register.md#work-fix-022-003), [FIX-023-001](../work-register.md#work-fix-023-001), [FIX-023-002](../work-register.md#work-fix-023-002), [FIX-023-003](../work-register.md#work-fix-023-003), [FIX-023-004](../work-register.md#work-fix-023-004), [FIX-023-005](../work-register.md#work-fix-023-005), [FIX-023-006](../work-register.md#work-fix-023-006), [FIX-023-007](../work-register.md#work-fix-023-007), [FIX-023-008](../work-register.md#work-fix-023-008), [FIX-023-009](../work-register.md#work-fix-023-009), [FIX-023-010](../work-register.md#work-fix-023-010), [FIX-023-011](../work-register.md#work-fix-023-011), [FIX-023-012](../work-register.md#work-fix-023-012), [FIX-023-013](../work-register.md#work-fix-023-013), [FIX-023-014](../work-register.md#work-fix-023-014), [FIX-023-015](../work-register.md#work-fix-023-015), [FIX-023-016](../work-register.md#work-fix-023-016), [FIX-023-017](../work-register.md#work-fix-023-017), [FIX-023-018](../work-register.md#work-fix-023-018), [FIX-028-001](../work-register.md#work-fix-028-001), [FIX-028-002](../work-register.md#work-fix-028-002), [FIX-028-003](../work-register.md#work-fix-028-003), [FIX-028-004](../work-register.md#work-fix-028-004), [FIX-028-005](../work-register.md#work-fix-028-005), [FIX-028-006](../work-register.md#work-fix-028-006), [FIX-028-007](../work-register.md#work-fix-028-007), [FIX-028-008](../work-register.md#work-fix-028-008), [FIX-029-001](../work-register.md#work-fix-029-001), [FIX-029-002](../work-register.md#work-fix-029-002), [FIX-029-003](../work-register.md#work-fix-029-003), [FIX-035-001](../work-register.md#work-fix-035-001), [FIX-035-002](../work-register.md#work-fix-035-002), [FIX-035-003](../work-register.md#work-fix-035-003), [FIX-035-004](../work-register.md#work-fix-035-004), [FIX-035-005](../work-register.md#work-fix-035-005), [FIX-035-006](../work-register.md#work-fix-035-006), [FIX-035-007](../work-register.md#work-fix-035-007), [FIX-035-008](../work-register.md#work-fix-035-008), [FIX-035-009](../work-register.md#work-fix-035-009), [FIX-035-010](../work-register.md#work-fix-035-010), [FIX-035-011](../work-register.md#work-fix-035-011), [FIX-035-012](../work-register.md#work-fix-035-012), [FIX-035-013](../work-register.md#work-fix-035-013), [FIX-035-014](../work-register.md#work-fix-035-014), [FIX-035-015](../work-register.md#work-fix-035-015), [FIX-035-016](../work-register.md#work-fix-035-016), [FIX-035-017](../work-register.md#work-fix-035-017), [FIX-035-018](../work-register.md#work-fix-035-018), [FIX-035-019](../work-register.md#work-fix-035-019), [FIX-035-020](../work-register.md#work-fix-035-020), [FIX-035-021](../work-register.md#work-fix-035-021), [FIX-035-022](../work-register.md#work-fix-035-022), [FIX-035-023](../work-register.md#work-fix-035-023), [FIX-035-024](../work-register.md#work-fix-035-024), [FIX-035-025](../work-register.md#work-fix-035-025), [FIX-035-026](../work-register.md#work-fix-035-026), [FIX-035-027](../work-register.md#work-fix-035-027), [FIX-042-001](../work-register.md#work-fix-042-001), [FIX-042-002](../work-register.md#work-fix-042-002), [FIX-042-003](../work-register.md#work-fix-042-003), [FIX-042-004](../work-register.md#work-fix-042-004), [FIX-042-005](../work-register.md#work-fix-042-005), [FIX-042-006](../work-register.md#work-fix-042-006), [FIX-042-007](../work-register.md#work-fix-042-007), [FIX-042-008](../work-register.md#work-fix-042-008), [FIX-042-009](../work-register.md#work-fix-042-009), [FIX-042-010](../work-register.md#work-fix-042-010), [FIX-042-011](../work-register.md#work-fix-042-011), [FIX-042-012](../work-register.md#work-fix-042-012), [FIX-042-013](../work-register.md#work-fix-042-013), [FIX-042-014](../work-register.md#work-fix-042-014), [FIX-042-015](../work-register.md#work-fix-042-015), [FIX-042-016](../work-register.md#work-fix-042-016), [FIX-042-017](../work-register.md#work-fix-042-017), [FIX-050-001](../work-register.md#work-fix-050-001), [FIX-053-001](../work-register.md#work-fix-053-001), [FIX-053-002](../work-register.md#work-fix-053-002), [FIX-053-003](../work-register.md#work-fix-053-003), [FIX-062-001](../work-register.md#work-fix-062-001), [FIX-063-001](../work-register.md#work-fix-063-001), [FIX-063-002](../work-register.md#work-fix-063-002), [FIX-063-003](../work-register.md#work-fix-063-003), [FIX-063-004](../work-register.md#work-fix-063-004), [FIX-063-005](../work-register.md#work-fix-063-005), [FIX-063-006](../work-register.md#work-fix-063-006), [FIX-063-007](../work-register.md#work-fix-063-007), [FIX-063-008](../work-register.md#work-fix-063-008), [FIX-063-009](../work-register.md#work-fix-063-009), [FIX-063-010](../work-register.md#work-fix-063-010), [FIX-063-011](../work-register.md#work-fix-063-011), [FIX-063-012](../work-register.md#work-fix-063-012), [FIX-063-013](../work-register.md#work-fix-063-013), [FIX-063-014](../work-register.md#work-fix-063-014), [FIX-063-015](../work-register.md#work-fix-063-015), [FIX-063-016](../work-register.md#work-fix-063-016), [FIX-063-017](../work-register.md#work-fix-063-017), [FIX-063-018](../work-register.md#work-fix-063-018), [FIX-063-019](../work-register.md#work-fix-063-019), [FIX-063-020](../work-register.md#work-fix-063-020), [FIX-063-021](../work-register.md#work-fix-063-021), [FIX-063-022](../work-register.md#work-fix-063-022), [FIX-063-023](../work-register.md#work-fix-063-023), [FIX-063-024](../work-register.md#work-fix-063-024), [FIX-063-025](../work-register.md#work-fix-063-025), [FIX-063-026](../work-register.md#work-fix-063-026), [FIX-063-027](../work-register.md#work-fix-063-027), [FIX-063-028](../work-register.md#work-fix-063-028), [FIX-064-001](../work-register.md#work-fix-064-001), [FIX-064-002](../work-register.md#work-fix-064-002), [FIX-064-003](../work-register.md#work-fix-064-003), [FIX-064-004](../work-register.md#work-fix-064-004), [FIX-064-005](../work-register.md#work-fix-064-005), [FIX-064-006](../work-register.md#work-fix-064-006), [FIX-064-007](../work-register.md#work-fix-064-007), [FIX-064-008](../work-register.md#work-fix-064-008), [FIX-064-009](../work-register.md#work-fix-064-009), [FIX-064-010](../work-register.md#work-fix-064-010), [FIX-064-011](../work-register.md#work-fix-064-011), [FIX-064-012](../work-register.md#work-fix-064-012), [FIX-064-013](../work-register.md#work-fix-064-013), [FIX-064-014](../work-register.md#work-fix-064-014), [FIX-064-015](../work-register.md#work-fix-064-015), [FIX-064-016](../work-register.md#work-fix-064-016), [FIX-064-017](../work-register.md#work-fix-064-017), [FIX-064-018](../work-register.md#work-fix-064-018), [FIX-064-019](../work-register.md#work-fix-064-019), [FIX-064-020](../work-register.md#work-fix-064-020), [FIX-064-021](../work-register.md#work-fix-064-021), [FIX-064-022](../work-register.md#work-fix-064-022), [FIX-064-023](../work-register.md#work-fix-064-023), [FIX-066-001](../work-register.md#work-fix-066-001), [FIX-067-001](../work-register.md#work-fix-067-001), [FIX-067-002](../work-register.md#work-fix-067-002), [FIX-071-001](../work-register.md#work-fix-071-001), [P0-002](../work-register.md#work-p0-002), [S003](../work-register.md#execution-step-003), [S004](../work-register.md#execution-step-004), [S005](../work-register.md#execution-step-005), [S006](../work-register.md#execution-step-006), [S013](../work-register.md#execution-step-013), [S014](../work-register.md#execution-step-014), [S015](../work-register.md#execution-step-015), [S016](../work-register.md#execution-step-016), [S017](../work-register.md#execution-step-017), [S018](../work-register.md#execution-step-018), [S019](../work-register.md#execution-step-019), [S020](../work-register.md#execution-step-020), [S021](../work-register.md#execution-step-021), [S022](../work-register.md#execution-step-022), [S023](../work-register.md#execution-step-023), [S038](../work-register.md#execution-step-038), [S039](../work-register.md#execution-step-039), [S040](../work-register.md#execution-step-040), [S041](../work-register.md#execution-step-041), [S042](../work-register.md#execution-step-042), [S043](../work-register.md#execution-step-043), [S044](../work-register.md#execution-step-044), [S048](../work-register.md#execution-step-048), [S049](../work-register.md#execution-step-049), [S050](../work-register.md#execution-step-050), [S051](../work-register.md#execution-step-051), [S052](../work-register.md#execution-step-052), [S053](../work-register.md#execution-step-053), [S054](../work-register.md#execution-step-054), [S055](../work-register.md#execution-step-055), [S056](../work-register.md#execution-step-056), [S057](../work-register.md#execution-step-057), [S058](../work-register.md#execution-step-058), [S059](../work-register.md#execution-step-059), [S060](../work-register.md#execution-step-060), [S061](../work-register.md#execution-step-061), [S062](../work-register.md#execution-step-062), [S063](../work-register.md#execution-step-063), [S064](../work-register.md#execution-step-064), [S065](../work-register.md#execution-step-065), [S066](../work-register.md#execution-step-066), [S067](../work-register.md#execution-step-067), [S068](../work-register.md#execution-step-068), [S069](../work-register.md#execution-step-069), [S070](../work-register.md#execution-step-070), [S071](../work-register.md#execution-step-071), [S072](../work-register.md#execution-step-072), [S073](../work-register.md#execution-step-073), [S074](../work-register.md#execution-step-074), [S075](../work-register.md#execution-step-075), [S076](../work-register.md#execution-step-076), [S077](../work-register.md#execution-step-077), [S078](../work-register.md#execution-step-078), [S079](../work-register.md#execution-step-079), [S080](../work-register.md#execution-step-080), [S081](../work-register.md#execution-step-081), [S082](../work-register.md#execution-step-082), [S083](../work-register.md#execution-step-083), [S084](../work-register.md#execution-step-084), [S085](../work-register.md#execution-step-085), [S086](../work-register.md#execution-step-086), [S087](../work-register.md#execution-step-087), [S088](../work-register.md#execution-step-088), [S089](../work-register.md#execution-step-089), [S090](../work-register.md#execution-step-090), [S091](../work-register.md#execution-step-091), [S092](../work-register.md#execution-step-092), [S093](../work-register.md#execution-step-093), [S094](../work-register.md#execution-step-094), [S095](../work-register.md#execution-step-095), [S096](../work-register.md#execution-step-096), [S097](../work-register.md#execution-step-097), [S098](../work-register.md#execution-step-098), [S099](../work-register.md#execution-step-099), [S100](../work-register.md#execution-step-100), [S101](../work-register.md#execution-step-101), [S102](../work-register.md#execution-step-102), [S103](../work-register.md#execution-step-103), [S104](../work-register.md#execution-step-104), [S105](../work-register.md#execution-step-105), [S106](../work-register.md#execution-step-106), [S107](../work-register.md#execution-step-107), [S108](../work-register.md#execution-step-108), [S109](../work-register.md#execution-step-109), [S110](../work-register.md#execution-step-110), [S111](../work-register.md#execution-step-111), [S112](../work-register.md#execution-step-112), [S113](../work-register.md#execution-step-113), [S114](../work-register.md#execution-step-114), [S115](../work-register.md#execution-step-115), [S116](../work-register.md#execution-step-116), [S117](../work-register.md#execution-step-117), [S118](../work-register.md#execution-step-118), [S119](../work-register.md#execution-step-119), [S120](../work-register.md#execution-step-120), [S121](../work-register.md#execution-step-121), [S122](../work-register.md#execution-step-122), [S123](../work-register.md#execution-step-123), [S124](../work-register.md#execution-step-124), [S125](../work-register.md#execution-step-125), [S126](../work-register.md#execution-step-126), [S127](../work-register.md#execution-step-127), [S128](../work-register.md#execution-step-128), [S129](../work-register.md#execution-step-129), [S130](../work-register.md#execution-step-130), [S131](../work-register.md#execution-step-131), [S132](../work-register.md#execution-step-132), [S133](../work-register.md#execution-step-133), [S134](../work-register.md#execution-step-134), [S135](../work-register.md#execution-step-135), [S136](../work-register.md#execution-step-136), [S137](../work-register.md#execution-step-137), [S138](../work-register.md#execution-step-138), [S139](../work-register.md#execution-step-139), [S140](../work-register.md#execution-step-140), [S141](../work-register.md#execution-step-141), [S142](../work-register.md#execution-step-142), [S143](../work-register.md#execution-step-143), [S144](../work-register.md#execution-step-144), [S145](../work-register.md#execution-step-145), [S146](../work-register.md#execution-step-146), [S147](../work-register.md#execution-step-147), [S148](../work-register.md#execution-step-148), [S149](../work-register.md#execution-step-149), [S150](../work-register.md#execution-step-150), [S151](../work-register.md#execution-step-151), [S152](../work-register.md#execution-step-152), [S153](../work-register.md#execution-step-153), [S154](../work-register.md#execution-step-154), [S155](../work-register.md#execution-step-155), [S156](../work-register.md#execution-step-156), [S157](../work-register.md#execution-step-157), [S158](../work-register.md#execution-step-158), [S159](../work-register.md#execution-step-159), [S160](../work-register.md#execution-step-160), [S161](../work-register.md#execution-step-161), [S162](../work-register.md#execution-step-162), [S163](../work-register.md#execution-step-163), [S164](../work-register.md#execution-step-164), [S165](../work-register.md#execution-step-165), [S166](../work-register.md#execution-step-166), [S167](../work-register.md#execution-step-167), [S168](../work-register.md#execution-step-168), [S169](../work-register.md#execution-step-169), [S170](../work-register.md#execution-step-170), [S171](../work-register.md#execution-step-171), [S172](../work-register.md#execution-step-172), [S173](../work-register.md#execution-step-173), [S174](../work-register.md#execution-step-174), [S175](../work-register.md#execution-step-175), [S176](../work-register.md#execution-step-176), [S177](../work-register.md#execution-step-177), [S178](../work-register.md#execution-step-178), [S179](../work-register.md#execution-step-179), [S180](../work-register.md#execution-step-180), [S181](../work-register.md#execution-step-181), [S182](../work-register.md#execution-step-182), [S183](../work-register.md#execution-step-183), [S184](../work-register.md#execution-step-184), [S185](../work-register.md#execution-step-185), [S186](../work-register.md#execution-step-186), [S187](../work-register.md#execution-step-187), [S188](../work-register.md#execution-step-188), [S189](../work-register.md#execution-step-189), [S190](../work-register.md#execution-step-190), [S191](../work-register.md#execution-step-191), [S192](../work-register.md#execution-step-192), [S193](../work-register.md#execution-step-193), [S194](../work-register.md#execution-step-194), [S195](../work-register.md#execution-step-195), [S196](../work-register.md#execution-step-196), [S197](../work-register.md#execution-step-197), [S198](../work-register.md#execution-step-198), [S199](../work-register.md#execution-step-199), [S200](../work-register.md#execution-step-200), [S201](../work-register.md#execution-step-201), [S202](../work-register.md#execution-step-202), [S203](../work-register.md#execution-step-203), [S204](../work-register.md#execution-step-204), [S205](../work-register.md#execution-step-205), [S206](../work-register.md#execution-step-206), [S207](../work-register.md#execution-step-207), [S208](../work-register.md#execution-step-208), [S209](../work-register.md#execution-step-209), [S210](../work-register.md#execution-step-210), [S211](../work-register.md#execution-step-211), [S212](../work-register.md#execution-step-212), [S213](../work-register.md#execution-step-213), [S214](../work-register.md#execution-step-214), [S215](../work-register.md#execution-step-215), [S216](../work-register.md#execution-step-216), [S217](../work-register.md#execution-step-217), [S218](../work-register.md#execution-step-218), [S219](../work-register.md#execution-step-219), [S220](../work-register.md#execution-step-220), [S221](../work-register.md#execution-step-221), [S222](../work-register.md#execution-step-222), [S223](../work-register.md#execution-step-223), [S224](../work-register.md#execution-step-224), [S225](../work-register.md#execution-step-225), [S226](../work-register.md#execution-step-226), [S227](../work-register.md#execution-step-227), [S228](../work-register.md#execution-step-228), [S229](../work-register.md#execution-step-229), [S230](../work-register.md#execution-step-230), [S231](../work-register.md#execution-step-231), [S232](../work-register.md#execution-step-232), [S233](../work-register.md#execution-step-233), [S234](../work-register.md#execution-step-234), [S235](../work-register.md#execution-step-235), [S236](../work-register.md#execution-step-236), [S237](../work-register.md#execution-step-237), [S238](../work-register.md#execution-step-238), [S239](../work-register.md#execution-step-239), [S240](../work-register.md#execution-step-240), [S241](../work-register.md#execution-step-241), [S242](../work-register.md#execution-step-242), [S243](../work-register.md#execution-step-243), [S244](../work-register.md#execution-step-244), [S245](../work-register.md#execution-step-245), [S246](../work-register.md#execution-step-246), [S247](../work-register.md#execution-step-247), [S248](../work-register.md#execution-step-248), [S249](../work-register.md#execution-step-249), [S250](../work-register.md#execution-step-250), [S251](../work-register.md#execution-step-251), [S252](../work-register.md#execution-step-252), [S253](../work-register.md#execution-step-253), [S254](../work-register.md#execution-step-254), [S255](../work-register.md#execution-step-255), [S256](../work-register.md#execution-step-256), [S258](../work-register.md#execution-step-258), [S259](../work-register.md#execution-step-259), [S260](../work-register.md#execution-step-260), [S261](../work-register.md#execution-step-261), [S262](../work-register.md#execution-step-262), [S263](../work-register.md#execution-step-263), [S264](../work-register.md#execution-step-264), [S265](../work-register.md#execution-step-265), [S266](../work-register.md#execution-step-266), [S267](../work-register.md#execution-step-267), [S268](../work-register.md#execution-step-268), [S269](../work-register.md#execution-step-269), [S270](../work-register.md#execution-step-270), [S271](../work-register.md#execution-step-271), [S272](../work-register.md#execution-step-272), [S273](../work-register.md#execution-step-273), [S274](../work-register.md#execution-step-274), [S275](../work-register.md#execution-step-275), [S276](../work-register.md#execution-step-276), [S277](../work-register.md#execution-step-277), [S278](../work-register.md#execution-step-278), [S279](../work-register.md#execution-step-279), [S280](../work-register.md#execution-step-280), [S281](../work-register.md#execution-step-281), [S282](../work-register.md#execution-step-282), [S283](../work-register.md#execution-step-283), [S284](../work-register.md#execution-step-284), [S285](../work-register.md#execution-step-285), [S286](../work-register.md#execution-step-286), [S287](../work-register.md#execution-step-287), [S288](../work-register.md#execution-step-288), [S289](../work-register.md#execution-step-289), [S290](../work-register.md#execution-step-290), [S291](../work-register.md#execution-step-291), [S293](../work-register.md#execution-step-293), [S294](../work-register.md#execution-step-294), [S295](../work-register.md#execution-step-295), [S296](../work-register.md#execution-step-296), [S297](../work-register.md#execution-step-297), [S298](../work-register.md#execution-step-298), [S299](../work-register.md#execution-step-299), [S300](../work-register.md#execution-step-300), [S301](../work-register.md#execution-step-301), [S302](../work-register.md#execution-step-302), [S303](../work-register.md#execution-step-303), [S304](../work-register.md#execution-step-304), [S305](../work-register.md#execution-step-305), [S306](../work-register.md#execution-step-306), [S307](../work-register.md#execution-step-307), [S308](../work-register.md#execution-step-308), [S309](../work-register.md#execution-step-309), [S310](../work-register.md#execution-step-310), [S311](../work-register.md#execution-step-311), [S312](../work-register.md#execution-step-312), [S313](../work-register.md#execution-step-313), [S314](../work-register.md#execution-step-314), [S315](../work-register.md#execution-step-315), [S316](../work-register.md#execution-step-316), [S317](../work-register.md#execution-step-317), [S318](../work-register.md#execution-step-318), [S319](../work-register.md#execution-step-319), [S320](../work-register.md#execution-step-320), [S321](../work-register.md#execution-step-321), [S322](../work-register.md#execution-step-322), [S323](../work-register.md#execution-step-323), [S324](../work-register.md#execution-step-324), [S326](../work-register.md#execution-step-326), [S327](../work-register.md#execution-step-327), [S328](../work-register.md#execution-step-328), [S329](../work-register.md#execution-step-329), [S330](../work-register.md#execution-step-330), [S331](../work-register.md#execution-step-331), [S332](../work-register.md#execution-step-332), [S333](../work-register.md#execution-step-333), [S334](../work-register.md#execution-step-334), [S335](../work-register.md#execution-step-335), [S336](../work-register.md#execution-step-336), [S337](../work-register.md#execution-step-337), [S338](../work-register.md#execution-step-338), [S339](../work-register.md#execution-step-339), [S340](../work-register.md#execution-step-340), [S341](../work-register.md#execution-step-341), [S342](../work-register.md#execution-step-342), [S343](../work-register.md#execution-step-343), [S344](../work-register.md#execution-step-344), [S345](../work-register.md#execution-step-345), [S346](../work-register.md#execution-step-346), [S347](../work-register.md#execution-step-347), [S348](../work-register.md#execution-step-348), [S349](../work-register.md#execution-step-349), [S350](../work-register.md#execution-step-350), [S351](../work-register.md#execution-step-351), [S352](../work-register.md#execution-step-352), [S353](../work-register.md#execution-step-353), [S354](../work-register.md#execution-step-354), [S355](../work-register.md#execution-step-355), [S356](../work-register.md#execution-step-356), [S357](../work-register.md#execution-step-357), [S358](../work-register.md#execution-step-358), [S360](../work-register.md#execution-step-360), [S361](../work-register.md#execution-step-361), [S362](../work-register.md#execution-step-362), [S363](../work-register.md#execution-step-363), [S364](../work-register.md#execution-step-364), [S365](../work-register.md#execution-step-365), [S366](../work-register.md#execution-step-366), [S367](../work-register.md#execution-step-367), [S368](../work-register.md#execution-step-368), [S369](../work-register.md#execution-step-369), [S370](../work-register.md#execution-step-370), [S371](../work-register.md#execution-step-371), [S372](../work-register.md#execution-step-372), [S373](../work-register.md#execution-step-373), [S374](../work-register.md#execution-step-374), [S375](../work-register.md#execution-step-375), [S376](../work-register.md#execution-step-376), [S377](../work-register.md#execution-step-377), [S378](../work-register.md#execution-step-378), [S379](../work-register.md#execution-step-379), [S380](../work-register.md#execution-step-380), [S381](../work-register.md#execution-step-381), [S382](../work-register.md#execution-step-382), [S383](../work-register.md#execution-step-383), [S384](../work-register.md#execution-step-384), [S385](../work-register.md#execution-step-385), [S386](../work-register.md#execution-step-386), [S387](../work-register.md#execution-step-387), [S388](../work-register.md#execution-step-388), [S389](../work-register.md#execution-step-389), [S390](../work-register.md#execution-step-390), [S391](../work-register.md#execution-step-391), [S392](../work-register.md#execution-step-392), [S393](../work-register.md#execution-step-393), [S394](../work-register.md#execution-step-394), [S395](../work-register.md#execution-step-395), [S396](../work-register.md#execution-step-396), [S397](../work-register.md#execution-step-397), [S398](../work-register.md#execution-step-398), [S399](../work-register.md#execution-step-399), [S400](../work-register.md#execution-step-400), [S402](../work-register.md#execution-step-402), [S403](../work-register.md#execution-step-403), [S404](../work-register.md#execution-step-404), [S405](../work-register.md#execution-step-405), [S406](../work-register.md#execution-step-406), [S407](../work-register.md#execution-step-407), [S408](../work-register.md#execution-step-408), [S409](../work-register.md#execution-step-409), [S410](../work-register.md#execution-step-410), [S411](../work-register.md#execution-step-411), [S412](../work-register.md#execution-step-412), [S413](../work-register.md#execution-step-413), [S414](../work-register.md#execution-step-414), [S417](../work-register.md#execution-step-417), [S418](../work-register.md#execution-step-418), [S419](../work-register.md#execution-step-419), [S420](../work-register.md#execution-step-420), [S421](../work-register.md#execution-step-421), [S422](../work-register.md#execution-step-422), [S423](../work-register.md#execution-step-423), [S424](../work-register.md#execution-step-424), [S425](../work-register.md#execution-step-425), [S426](../work-register.md#execution-step-426), [S427](../work-register.md#execution-step-427), [S428](../work-register.md#execution-step-428), [S429](../work-register.md#execution-step-429), [S430](../work-register.md#execution-step-430), [S431](../work-register.md#execution-step-431), [S432](../work-register.md#execution-step-432), [S433](../work-register.md#execution-step-433), [S434](../work-register.md#execution-step-434), [S435](../work-register.md#execution-step-435), [S436](../work-register.md#execution-step-436), [S437](../work-register.md#execution-step-437), [S438](../work-register.md#execution-step-438), [S439](../work-register.md#execution-step-439), [S440](../work-register.md#execution-step-440), [S441](../work-register.md#execution-step-441), [S442](../work-register.md#execution-step-442), [S443](../work-register.md#execution-step-443), [S444](../work-register.md#execution-step-444), [S445](../work-register.md#execution-step-445), [S446](../work-register.md#execution-step-446), [S447](../work-register.md#execution-step-447), [S448](../work-register.md#execution-step-448), [S449](../work-register.md#execution-step-449), [S450](../work-register.md#execution-step-450), [S451](../work-register.md#execution-step-451), [S452](../work-register.md#execution-step-452), [S453](../work-register.md#execution-step-453), [S454](../work-register.md#execution-step-454), [S455](../work-register.md#execution-step-455), [S456](../work-register.md#execution-step-456), [S457](../work-register.md#execution-step-457), [S458](../work-register.md#execution-step-458), [S459](../work-register.md#execution-step-459), [S460](../work-register.md#execution-step-460), [S461](../work-register.md#execution-step-461), [S462](../work-register.md#execution-step-462), [S463](../work-register.md#execution-step-463), [S464](../work-register.md#execution-step-464), [S465](../work-register.md#execution-step-465), [S466](../work-register.md#execution-step-466), [S467](../work-register.md#execution-step-467), [S468](../work-register.md#execution-step-468), [S471](../work-register.md#execution-step-471), [S472](../work-register.md#execution-step-472), [S473](../work-register.md#execution-step-473), [S474](../work-register.md#execution-step-474), [S475](../work-register.md#execution-step-475), [S476](../work-register.md#execution-step-476), [S477](../work-register.md#execution-step-477), [S478](../work-register.md#execution-step-478), [S479](../work-register.md#execution-step-479), [S480](../work-register.md#execution-step-480), [S481](../work-register.md#execution-step-481), [S482](../work-register.md#execution-step-482), [S485](../work-register.md#execution-step-485), [S486](../work-register.md#execution-step-486), [S487](../work-register.md#execution-step-487), [S488](../work-register.md#execution-step-488), [S489](../work-register.md#execution-step-489), [S490](../work-register.md#execution-step-490), [S491](../work-register.md#execution-step-491), [S492](../work-register.md#execution-step-492), [S493](../work-register.md#execution-step-493), [S494](../work-register.md#execution-step-494), [S495](../work-register.md#execution-step-495), [S496](../work-register.md#execution-step-496), [S497](../work-register.md#execution-step-497), [S498](../work-register.md#execution-step-498), [S499](../work-register.md#execution-step-499), [S500](../work-register.md#execution-step-500), [S501](../work-register.md#execution-step-501), [S502](../work-register.md#execution-step-502), [S503](../work-register.md#execution-step-503), [S504](../work-register.md#execution-step-504), [S505](../work-register.md#execution-step-505), [S506](../work-register.md#execution-step-506), [S507](../work-register.md#execution-step-507), [S508](../work-register.md#execution-step-508), [S509](../work-register.md#execution-step-509), [S510](../work-register.md#execution-step-510), [S511](../work-register.md#execution-step-511), [S512](../work-register.md#execution-step-512), [S513](../work-register.md#execution-step-513), [S514](../work-register.md#execution-step-514), [S515](../work-register.md#execution-step-515), [S516](../work-register.md#execution-step-516), [S517](../work-register.md#execution-step-517), [S518](../work-register.md#execution-step-518), [S519](../work-register.md#execution-step-519), [S520](../work-register.md#execution-step-520), [S521](../work-register.md#execution-step-521), [S522](../work-register.md#execution-step-522), [S523](../work-register.md#execution-step-523), [S524](../work-register.md#execution-step-524), [S525](../work-register.md#execution-step-525), [S526](../work-register.md#execution-step-526), [S529](../work-register.md#execution-step-529), [S530](../work-register.md#execution-step-530), [S531](../work-register.md#execution-step-531), [S532](../work-register.md#execution-step-532), [S533](../work-register.md#execution-step-533), [S534](../work-register.md#execution-step-534), [S535](../work-register.md#execution-step-535), [S536](../work-register.md#execution-step-536), [S537](../work-register.md#execution-step-537), [S538](../work-register.md#execution-step-538), [S539](../work-register.md#execution-step-539), [S540](../work-register.md#execution-step-540), [S541](../work-register.md#execution-step-541), [S544](../work-register.md#execution-step-544), [S545](../work-register.md#execution-step-545), [S546](../work-register.md#execution-step-546), [S547](../work-register.md#execution-step-547), [S548](../work-register.md#execution-step-548), [S549](../work-register.md#execution-step-549), [S550](../work-register.md#execution-step-550), [S551](../work-register.md#execution-step-551), [S552](../work-register.md#execution-step-552), [S553](../work-register.md#execution-step-553), [S554](../work-register.md#execution-step-554), [S555](../work-register.md#execution-step-555), [S556](../work-register.md#execution-step-556), [S557](../work-register.md#execution-step-557), [S558](../work-register.md#execution-step-558), [S559](../work-register.md#execution-step-559), [S560](../work-register.md#execution-step-560), [S561](../work-register.md#execution-step-561), [S562](../work-register.md#execution-step-562), [S563](../work-register.md#execution-step-563), [S564](../work-register.md#execution-step-564), [S565](../work-register.md#execution-step-565), [S566](../work-register.md#execution-step-566), [S567](../work-register.md#execution-step-567), [S568](../work-register.md#execution-step-568), [S569](../work-register.md#execution-step-569), [S570](../work-register.md#execution-step-570), [S571](../work-register.md#execution-step-571), [S572](../work-register.md#execution-step-572), [S573](../work-register.md#execution-step-573), [S574](../work-register.md#execution-step-574), [S575](../work-register.md#execution-step-575), [S576](../work-register.md#execution-step-576), [S577](../work-register.md#execution-step-577), [S578](../work-register.md#execution-step-578), [S579](../work-register.md#execution-step-579), [S580](../work-register.md#execution-step-580), [S581](../work-register.md#execution-step-581), [S582](../work-register.md#execution-step-582), [S583](../work-register.md#execution-step-583), [S584](../work-register.md#execution-step-584), [S585](../work-register.md#execution-step-585), [S586](../work-register.md#execution-step-586), [S587](../work-register.md#execution-step-587), [S588](../work-register.md#execution-step-588), [S589](../work-register.md#execution-step-589), [S590](../work-register.md#execution-step-590), [S591](../work-register.md#execution-step-591), [S592](../work-register.md#execution-step-592), [S593](../work-register.md#execution-step-593), [S594](../work-register.md#execution-step-594), [S595](../work-register.md#execution-step-595), [S596](../work-register.md#execution-step-596), [S597](../work-register.md#execution-step-597), [S598](../work-register.md#execution-step-598), [S599](../work-register.md#execution-step-599), [S600](../work-register.md#execution-step-600), [S601](../work-register.md#execution-step-601), [S602](../work-register.md#execution-step-602), [S605](../work-register.md#execution-step-605), [S606](../work-register.md#execution-step-606), [S607](../work-register.md#execution-step-607), [S608](../work-register.md#execution-step-608), [S609](../work-register.md#execution-step-609), [S610](../work-register.md#execution-step-610), [S611](../work-register.md#execution-step-611), [S612](../work-register.md#execution-step-612), [S613](../work-register.md#execution-step-613), [S614](../work-register.md#execution-step-614), [S615](../work-register.md#execution-step-615), [S616](../work-register.md#execution-step-616), [S617](../work-register.md#execution-step-617), [S618](../work-register.md#execution-step-618), [S619](../work-register.md#execution-step-619), [S620](../work-register.md#execution-step-620), [S621](../work-register.md#execution-step-621), [S622](../work-register.md#execution-step-622), [S623](../work-register.md#execution-step-623), [S624](../work-register.md#execution-step-624), [S625](../work-register.md#execution-step-625), [S626](../work-register.md#execution-step-626), [S627](../work-register.md#execution-step-627), [S628](../work-register.md#execution-step-628), [S629](../work-register.md#execution-step-629), [S630](../work-register.md#execution-step-630), [S631](../work-register.md#execution-step-631), [S632](../work-register.md#execution-step-632), [S633](../work-register.md#execution-step-633), [S636](../work-register.md#execution-step-636), [S637](../work-register.md#execution-step-637), [S638](../work-register.md#execution-step-638), [S639](../work-register.md#execution-step-639), [S640](../work-register.md#execution-step-640), [S641](../work-register.md#execution-step-641), [S642](../work-register.md#execution-step-642), [S643](../work-register.md#execution-step-643), [S644](../work-register.md#execution-step-644), [S645](../work-register.md#execution-step-645), [S646](../work-register.md#execution-step-646), [S647](../work-register.md#execution-step-647), [S648](../work-register.md#execution-step-648), [S649](../work-register.md#execution-step-649), [S650](../work-register.md#execution-step-650), [S651](../work-register.md#execution-step-651), [S652](../work-register.md#execution-step-652), [S653](../work-register.md#execution-step-653), [S654](../work-register.md#execution-step-654), [S655](../work-register.md#execution-step-655), [S656](../work-register.md#execution-step-656), [S657](../work-register.md#execution-step-657), [S658](../work-register.md#execution-step-658), [S659](../work-register.md#execution-step-659), [S660](../work-register.md#execution-step-660), [S661](../work-register.md#execution-step-661), [S662](../work-register.md#execution-step-662), [S663](../work-register.md#execution-step-663), [S664](../work-register.md#execution-step-664), [S665](../work-register.md#execution-step-665), [S666](../work-register.md#execution-step-666), [S667](../work-register.md#execution-step-667), [S668](../work-register.md#execution-step-668), [S669](../work-register.md#execution-step-669), [S670](../work-register.md#execution-step-670), [S671](../work-register.md#execution-step-671), [S672](../work-register.md#execution-step-672), [S673](../work-register.md#execution-step-673), [S674](../work-register.md#execution-step-674), [S675](../work-register.md#execution-step-675), [S676](../work-register.md#execution-step-676), [S677](../work-register.md#execution-step-677), [S678](../work-register.md#execution-step-678), [S679](../work-register.md#execution-step-679), [S680](../work-register.md#execution-step-680), [S681](../work-register.md#execution-step-681), [S682](../work-register.md#execution-step-682), [S684](../work-register.md#execution-step-684), [S685](../work-register.md#execution-step-685), [S686](../work-register.md#execution-step-686), [S687](../work-register.md#execution-step-687), [S688](../work-register.md#execution-step-688), [S689](../work-register.md#execution-step-689), [S690](../work-register.md#execution-step-690), [S691](../work-register.md#execution-step-691), [S692](../work-register.md#execution-step-692), [S693](../work-register.md#execution-step-693), [S694](../work-register.md#execution-step-694), [S695](../work-register.md#execution-step-695), [S696](../work-register.md#execution-step-696), [S697](../work-register.md#execution-step-697), [S698](../work-register.md#execution-step-698), [S699](../work-register.md#execution-step-699), [S700](../work-register.md#execution-step-700), [S701](../work-register.md#execution-step-701), [S702](../work-register.md#execution-step-702), [S703](../work-register.md#execution-step-703), [S704](../work-register.md#execution-step-704), [S705](../work-register.md#execution-step-705), [S706](../work-register.md#execution-step-706), [S707](../work-register.md#execution-step-707), [S708](../work-register.md#execution-step-708), [S709](../work-register.md#execution-step-709), [S710](../work-register.md#execution-step-710), [S711](../work-register.md#execution-step-711), [S712](../work-register.md#execution-step-712), [S713](../work-register.md#execution-step-713), [S714](../work-register.md#execution-step-714), [S715](../work-register.md#execution-step-715), [S716](../work-register.md#execution-step-716), [S717](../work-register.md#execution-step-717), [S718](../work-register.md#execution-step-718), [S719](../work-register.md#execution-step-719), [S720](../work-register.md#execution-step-720), [S721](../work-register.md#execution-step-721), [S722](../work-register.md#execution-step-722), [S723](../work-register.md#execution-step-723), [S724](../work-register.md#execution-step-724), [S725](../work-register.md#execution-step-725), [S726](../work-register.md#execution-step-726), [S727](../work-register.md#execution-step-727), [S728](../work-register.md#execution-step-728), [S729](../work-register.md#execution-step-729), [S730](../work-register.md#execution-step-730), [S731](../work-register.md#execution-step-731), [S732](../work-register.md#execution-step-732), [S733](../work-register.md#execution-step-733), [S734](../work-register.md#execution-step-734), [S735](../work-register.md#execution-step-735), [S736](../work-register.md#execution-step-736), [S737](../work-register.md#execution-step-737), [S738](../work-register.md#execution-step-738), [S740](../work-register.md#execution-step-740), [S741](../work-register.md#execution-step-741), [S742](../work-register.md#execution-step-742), [S743](../work-register.md#execution-step-743), [S744](../work-register.md#execution-step-744), [S745](../work-register.md#execution-step-745), [S746](../work-register.md#execution-step-746), [S747](../work-register.md#execution-step-747), [S748](../work-register.md#execution-step-748), [S749](../work-register.md#execution-step-749), [S750](../work-register.md#execution-step-750), [S751](../work-register.md#execution-step-751), [S752](../work-register.md#execution-step-752), [S753](../work-register.md#execution-step-753), [S754](../work-register.md#execution-step-754), [S755](../work-register.md#execution-step-755), [S756](../work-register.md#execution-step-756), [S757](../work-register.md#execution-step-757), [S758](../work-register.md#execution-step-758), [S759](../work-register.md#execution-step-759), [S760](../work-register.md#execution-step-760), [S761](../work-register.md#execution-step-761), [S762](../work-register.md#execution-step-762), [S763](../work-register.md#execution-step-763), [S764](../work-register.md#execution-step-764), [S765](../work-register.md#execution-step-765), [S766](../work-register.md#execution-step-766), [S767](../work-register.md#execution-step-767), [S768](../work-register.md#execution-step-768), [S769](../work-register.md#execution-step-769), [S770](../work-register.md#execution-step-770), [S771](../work-register.md#execution-step-771), [S772](../work-register.md#execution-step-772), [S773](../work-register.md#execution-step-773), [S774](../work-register.md#execution-step-774), [S775](../work-register.md#execution-step-775), [S776](../work-register.md#execution-step-776), [S777](../work-register.md#execution-step-777), [S778](../work-register.md#execution-step-778), [S779](../work-register.md#execution-step-779), [S780](../work-register.md#execution-step-780), [S781](../work-register.md#execution-step-781), [S782](../work-register.md#execution-step-782), [S783](../work-register.md#execution-step-783), [S784](../work-register.md#execution-step-784), [S785](../work-register.md#execution-step-785), [S786](../work-register.md#execution-step-786), [S787](../work-register.md#execution-step-787), [S788](../work-register.md#execution-step-788), [S790](../work-register.md#execution-step-790), [S791](../work-register.md#execution-step-791), [S792](../work-register.md#execution-step-792), [S793](../work-register.md#execution-step-793), [S794](../work-register.md#execution-step-794), [S795](../work-register.md#execution-step-795), [S796](../work-register.md#execution-step-796), [S797](../work-register.md#execution-step-797), [S798](../work-register.md#execution-step-798), [S799](../work-register.md#execution-step-799), [S800](../work-register.md#execution-step-800), [S801](../work-register.md#execution-step-801), [S802](../work-register.md#execution-step-802), [S803](../work-register.md#execution-step-803), [S804](../work-register.md#execution-step-804), [S805](../work-register.md#execution-step-805), [S806](../work-register.md#execution-step-806), [S807](../work-register.md#execution-step-807), [S808](../work-register.md#execution-step-808), [S809](../work-register.md#execution-step-809), [S810](../work-register.md#execution-step-810), [S811](../work-register.md#execution-step-811), [S812](../work-register.md#execution-step-812), [S813](../work-register.md#execution-step-813), [S814](../work-register.md#execution-step-814), [S815](../work-register.md#execution-step-815), [S816](../work-register.md#execution-step-816), [S817](../work-register.md#execution-step-817), [S818](../work-register.md#execution-step-818), [S819](../work-register.md#execution-step-819), [S820](../work-register.md#execution-step-820), [S821](../work-register.md#execution-step-821), [S822](../work-register.md#execution-step-822), [S823](../work-register.md#execution-step-823), [S824](../work-register.md#execution-step-824), [S825](../work-register.md#execution-step-825), [S826](../work-register.md#execution-step-826), [S827](../work-register.md#execution-step-827), [S828](../work-register.md#execution-step-828), [S829](../work-register.md#execution-step-829), [S830](../work-register.md#execution-step-830), [S831](../work-register.md#execution-step-831), [S832](../work-register.md#execution-step-832), [S834](../work-register.md#execution-step-834), [S835](../work-register.md#execution-step-835), [S836](../work-register.md#execution-step-836), [S837](../work-register.md#execution-step-837), [S838](../work-register.md#execution-step-838), [S839](../work-register.md#execution-step-839), [S840](../work-register.md#execution-step-840), [S841](../work-register.md#execution-step-841), [S842](../work-register.md#execution-step-842), [S843](../work-register.md#execution-step-843), [S844](../work-register.md#execution-step-844), [S845](../work-register.md#execution-step-845), [S846](../work-register.md#execution-step-846), [S847](../work-register.md#execution-step-847). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s004"></a>
### S004 · Audit retained scope: Execution preparation — current verification entry points and evidence validity

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** Execution preparation — current verification entry points and evidence validity. Reconcile active build/test/export/playtest instructions with the current source and serialization schema; the Playtest README contains historical schema/count observations that require this audit. Record exact commands, real-UI recipes,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-004).


 **Exact closure members:** [P0-001](../work-register.md#work-p0-001), [P0-012](../work-register.md#work-p0-012), [P0-029](../work-register.md#work-p0-029), [P0-030](../work-register.md#work-p0-030), [P0-031](../work-register.md#work-p0-031), [P0-034](../work-register.md#work-p0-034). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s005"></a>
### S005 · Audit retained scope: Execution preparation — reproducible workload and support manifest

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** Execution preparation — reproducible workload and support manifest. Complete PERF-01/02/21 with exact scene/recipe identities, object/constraint/domain counts, sparse/doubled/dense load sizes, simulation duration, viewport/DPR, build/export commands and artifact hashes. Pin tested browser/OS/device versions and support… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-005).


 **Exact closure members:** [P0-003](../work-register.md#work-p0-003), [P0-034](../work-register.md#work-p0-034). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s006"></a>
### S006 · Audit retained scope: PERF-39 — freeze the execution map, support manifest and acceptance baseline

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** zero unassigned consumers or orphan requirements; documented current production baseline and retained failures; each threshold has a unit, sampling rule and oracle before acceptance runs. Planning does not claim speedup. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-006).


 **Exact closure members:** [P0-003](../work-register.md#work-p0-003), [P0-004](../work-register.md#work-p0-004), [P0-032](../work-register.md#work-p0-032), [P0-034](../work-register.md#work-p0-034). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s007"></a>
### S007 · Audit retained scope: Resolve proof-blocking input and lifecycle defects for the first worker construction

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** Replaying the retained level-28 construction never starts Run before the player's Run action; the original unexpected-event record remains a failed attempt. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-007).


 **Exact closure members:** [S258](../work-register.md#execution-step-258), [S259](../work-register.md#execution-step-259), [S260](../work-register.md#execution-step-260), [S261](../work-register.md#execution-step-261), [S262](../work-register.md#execution-step-262), [S263](../work-register.md#execution-step-263), [S264](../work-register.md#execution-step-264), [S265](../work-register.md#execution-step-265), [S266](../work-register.md#execution-step-266), [S267](../work-register.md#execution-step-267), [S268](../work-register.md#execution-step-268). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s008"></a>
### S008 · Audit retained scope: Establish per-element model/source decisions and typed identity rules

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** Execution preparation — domain contracts before each domain implementation. For every required process, record the chosen gameplay model and intentional fidelity limits, units/sign conventions, valid parameter ranges, state ownership, finite sources/sinks, coupling order, timestep/error/convergence limits, failure behaviour,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-008).


 **Exact closure members:** [S257](../work-register.md#execution-step-257), [S772](../work-register.md#execution-step-772), [S782](../work-register.md#execution-step-782), [S783](../work-register.md#execution-step-783), [S784](../work-register.md#execution-step-784), [S785](../work-register.md#execution-step-785), [S786](../work-register.md#execution-step-786), [S787](../work-register.md#execution-step-787). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s009"></a>
### S009 · Audit retained scope: Compile scene-independent core storage and immutable construction declarations

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** compile the core and headless tests without Godot/2dog references; architecture checks reject injected scene dependencies and per-part law selectors; exact replay/rollback and analytic controls pass for each migrated consumer. Read-only presentation never enters solver state. Prove an existing part's full UI path and publish… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-009).


 **Exact closure members:** [P0-007](../work-register.md#work-p0-007), [P0-008](../work-register.md#work-p0-008), [P0-009](../work-register.md#work-p0-009). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s010"></a>
### S010 · Audit retained scope: Implement finite-store/coupling interfaces for the current physical slice

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-17 — complete domain and element capability coverage. Extend section 0.5 and the individual element registers with per-element dependencies, interaction surfaces/volumes, stores, laws, query contracts and workload measures. Cover mechanical/elastic/granular, electrical/logic, water/hydraulic, pneumatic/wind,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-010).


 **Exact closure members:** [ENGINE-LEDGER](../work-register.md#work-engine-ledger), [ENGINE-TOPOLOGY](../work-register.md#work-engine-topology), [P0-014](../work-register.md#work-p0-014). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s011"></a>
### S011 · Audit retained scope: Implement ordered commands, reliable events and owned snapshot contracts

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-31 — implement typed command admission and deterministic application. Adopt the simulation–presentation bridge design. Use enum-typed commands/results and typed entity/command/generation/revision IDs; validate at admission and again against authoritative state at application. Define construction/runtime/lifecycle… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-011).


 **Exact closure members:** [P0-014](../work-register.md#work-p0-014), [P0-015](../work-register.md#work-p0-015), [P0-016](../work-register.md#work-p0-016). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s012"></a>
### S012 · Audit retained scope: Prepare direct-UI recipes and per-part proof tooling for this architecture

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** no part or intended interaction disappears from the checklist; every missing capability has its own implementation item and every unsupported pairing has a rejection case. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-012).


 **Exact closure members:** [S775](../work-register.md#execution-step-775), [S776](../work-register.md#execution-step-776), [S777](../work-register.md#execution-step-777), [S778](../work-register.md#execution-step-778), [S781](../work-register.md#execution-step-781). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s013"></a>
### S013 · Audit retained scope: Use simulation-time ticks and deterministic event ordering

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** Pause stops the countdown and machine together, slow motion changes their pace together, and Reset removes pending pulses. A short signal still reaches its intended consumer at low frame rates.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-013).


 **Exact closure members:** [P0-015](../work-register.md#work-p0-015), [P0-019](../work-register.md#work-p0-019). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s014"></a>
### S014 · Audit retained scope: Specify loss-of-power rules: actuator power loss stops action without replaying missed one-shot events when restored

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** A stopped powered actuator never replays an unserved trigger when electricity returns. Document and test each module's memory rule; the existing Set/Reset latch retains memory across supply loss and must not be silently made volatile.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-014).


 **Exact closure members:** [P0-023](../work-register.md#work-p0-023), [S323](../work-register.md#execution-step-323). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s015"></a>
### S015 · Audit retained scope: Define hysteresis, Reset and edge-event contracts; show A/B/output/supply state with domain-specific socket/lens icons

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** A/B, output and supply/carrier cues explain the live state; thresholds do not flicker and Reset restores the defined state without phantom edges.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-015).


 **Exact closure members:** [P0-020](../work-register.md#work-p0-020), [P0-023](../work-register.md#work-p0-023), [S323](../work-register.md#execution-step-323). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s035"></a>
### S035 · Audit retained scope: Define current-part render/collision geometry contracts

Supporting spatial acceptance; see exact scope correction. **Source outcome:** PERF-03 — replace world all-pairs traversal. Add a conservative persistent body-level spatial index above the compound hierarchy in PhysicsWorld; benchmark candidates and ship one implementation, deleting superseded traversal. Preserve deterministic typed pairs, filtering, prescribed feasibility and contact release. Bounds must… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-035).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s035).

<a id="s036"></a>
### S036 · Audit retained scope: Implement the conservative spatial hierarchy

Supporting spatial acceptance; see exact scope correction. **Source outcome:** PERF-03 — replace world all-pairs traversal. Add a conservative persistent body-level spatial index above the compound hierarchy in PhysicsWorld; benchmark candidates and ship one implementation, deleting superseded traversal. Preserve deterministic typed pairs, filtering, prescribed feasibility and contact release. Bounds must… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-036).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s036).

<a id="s037"></a>
### S037 · Audit retained scope: Qualify spatial cache invalidation

Supporting spatial acceptance; see exact scope correction. **Source outcome:** PERF-03 — replace world all-pairs traversal. Add a conservative persistent body-level spatial index above the compound hierarchy in PhysicsWorld; benchmark candidates and ship one implementation, deleting superseded traversal. Preserve deterministic typed pairs, filtering, prescribed feasibility and contact release. Bounds must… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication. Engine/current-consumer/generic-fixture scope; future catalogue cases keep separate owners.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-037).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s037).

<a id="s048"></a>
### S048 · Superseded CPU prototype; no current action

**Superseded; no current action.** The unused tangent-quadratic experiment is not part of the WGSL physics design and does not require integration or porting. Current coupled-contact behavior and work/error qualification are owned by the contact criteria below and P0-007/P0-012.

[Complete current acceptance](../requirements.md#sequence-task-057) · [Status and technical predecessors](../work-register.md#execution-step-048).

This retained identity is not an implementation prerequisite or a runtime Pass.

<a id="s049"></a>
### S049 · Coupled constrained-contact response

**Current outcome:** Solve contact, friction and equality constraints together within the current f16 puzzle envelope, including rank-deficient tangential response and moving-frame convective targets. Preserve powered/unpowered motor outcomes, bellows loading, chime motion, atomic rejection and exact Reset; a particular CPU factorization is not required.

[Complete current acceptance](../requirements.md#sequence-task-058) · [Status and technical predecessors](../work-register.md#execution-step-049).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s050"></a>
### S050 · Bellows and chime contact stability

**Current outcome:** The powered bellows and both chime identity orders must complete their intended physical response without solver exhaustion or order-dependent outcomes. Qualify coupled friction, rank-deficient response and complete-Run work/error bounds at the admitted f16 scales; microscopic CPU residual targets are not acceptance.

[Complete current acceptance](../requirements.md#sequence-task-059) · [Status and technical predecessors](../work-register.md#execution-step-050).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s051"></a>
### S051 · Bounded evaluation of elastic and compliant laws

**Current outcome:** Evaluate each elastic/compliant law from a coherent current candidate pose, with no stale cross-evaluation state. Measure complete tick/transfer/allocation costs and preserve motion, no-fan controls, exact Reset and failed-candidate atomicity; optimize only measured redundant work.

[Complete current acceptance](../requirements.md#sequence-task-060) · [Status and technical predecessors](../work-register.md#execution-step-051).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s052"></a>
### S052 · Dependency-bounded coupled-shaft work

**Current outcome:** Instrument and bound numerical work for fan-driven coupled shafts against the same construction without fan drive. Typed counters distinguish physical workload from implementation overhead; preserve complete coupled behavior, current error limits and Reset while qualifying whole-system improvement.

[Complete current acceptance](../requirements.md#sequence-task-061) · [Status and technical predecessors](../work-register.md#execution-step-052).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s053"></a>
### S053 · Bounded solver scratch and ownership

**Current outcome:** Reuse bounded scratch without routine warmed allocation, cross-query aliasing or retained references. Prove failure atomicity, reuse, zero-rank and concurrent-owner controls; preserve rotor/conveyor outcomes, no-fan controls and exact Reset while measuring the actual GPU/host tick.

[Complete current acceptance](../requirements.md#sequence-task-062) · [Status and technical predecessors](../work-register.md#execution-step-053).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s054"></a>
### S054 · Conveyor and airflow workload qualification

**Current outcome:** Construct conveyor contact transport and its unpowered-support control, a fan-driven windmill and mechanical conveyor output through actual UI controls. Verify typed connections, exact Run/Reset and supported save/load; qualify complete-system time, memory and resource behavior without hiding stalls or screenshot failures.

[Complete current acceptance](../requirements.md#sequence-task-063) · [Status and technical predecessors](../work-register.md#execution-step-054).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s055"></a>
### S055 · World-owned angular travel

**Current outcome:** Committed hinge winding and traveled distance belong to the simulation and restore with failed-tick rollback and snapshots. Motor, windmill and conveyor events/read models observe that history; prove unpowered, full-turn, reversal and exact Reset/save controls without callback-owned angular accumulators.

[Complete current acceptance](../requirements.md#sequence-task-064) · [Status and technical predecessors](../work-register.md#execution-step-055).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s056"></a>
### S056 · Angular path boundaries

**Current outcome:** Measure signed winding and total angular variation along accepted motion, including reversals between matching endpoints. Bound uncertainty under the admitted f16 contract and reject singular/unsupported ranges or exhausted budgets before commit; publication and restoration must reflect the same accepted path.

[Complete current acceptance](../requirements.md#sequence-task-065) · [Status and technical predecessors](../work-register.md#execution-step-056).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s057"></a>
### S057 · Construction gesture isolation

**Current outcome:** Fast pipe placement/movement gestures must not accidentally activate Run or Reset. Verify pointer ownership, drag cancellation, stopped-state admission and one Undo per gesture through actual Chrome controls; any unresolved unintended activation remains a failed interaction criterion.

[Complete current acceptance](../requirements.md#sequence-task-066) · [Status and technical predecessors](../work-register.md#execution-step-057).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s058"></a>
### S058 · Authoritative placement admission

**Current outcome:** Run admission uses the same quantized geometry, participation and joint filters as physical execution. Reject shell overlap while accepting valid hollow-bore placement; preserve exact construction/links after failed admission, Run/Reset and supported Save/Load, including fast-drag control isolation.

[Complete current acceptance](../requirements.md#sequence-task-067) · [Status and technical predecessors](../work-register.md#execution-step-058).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s059"></a>
### S059 · Compound-query resource bounds

**Current outcome:** Compound traversal must preserve candidate membership and hollow passages while using bounded owned scratch. Qualify sparse, hollow and dense scenes, shell contact versus open-bore passage, exact Reset/save and repeated allocation/memory/performance without omitting hard candidate pairs.

[Complete current acceptance](../requirements.md#sequence-task-068) · [Status and technical predecessors](../work-register.md#execution-step-059).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s060"></a>
### S060 · Typed physics work instrumentation

**Current outcome:** Record bounded enum-typed per-tick physical work counters with explicit overflow, missing-data and failed-tick semantics. Reject unknown metric identities and malformed boundaries; measure instrumentation overhead and all required scenarios. Recording must not change outcomes, allocate routine warmed scratch or imply runtime qualification.

[Complete current acceptance](../requirements.md#sequence-task-069) · [Status and technical predecessors](../work-register.md#execution-step-060).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s061"></a>
### S061 · Conservative spatial indexing

**Current outcome:** Use conservative world/body bounds for construction, dynamic and prescribed-motion queries. Sparse separated scenes must avoid unnecessary pair work; dense controls retain every required pair. Prove no-ramp, horizontal/tilted ramp, hollow/rotation and exact Reset/save cases with actual browser performance.

[Complete current acceptance](../requirements.md#sequence-task-070) · [Status and technical predecessors](../work-register.md#execution-step-061).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s062"></a>
### S062 · Whole-pipeline performance observation

**Current outcome:** Measure complete committed/failed ticks, domains, numerical physics, transport, animation and presentation with bounded typed samples. Predeclare stage definitions, overflow and inclusive/exclusive costs; qualify observer overhead and production controls on required devices. Missing data or old timings cannot satisfy current budgets.

[Complete current acceptance](../requirements.md#sequence-task-071) · [Status and technical predecessors](../work-register.md#execution-step-062).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s063"></a>
### S063 · Canonical body-local pose composition

**Current outcome:** Every body/child pose composes in its declared local frame using the current integer-cell/f16 representation and validated external adapters. Rotated pusher installation, bellows/chime contact and exact Reset must remain correct; no render transform or wider hidden model may become physical authority.

[Complete current acceptance](../requirements.md#sequence-task-072) · [Status and technical predecessors](../work-register.md#execution-step-063).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s064"></a>
### S064 · Ramp construction and browser controls

**Current outcome:** Through actual UI, place and rotate a ramp and compare no-ramp, horizontal and correctly tilted constructions against their declared outcomes. Verify exact repeated Run/Reset and supported Save/remove/Load, plus rotated-pusher admission. A visible run that misses its goal is not successful qualification.

[Complete current acceptance](../requirements.md#sequence-task-073) · [Status and technical predecessors](../work-register.md#execution-step-064).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s065"></a>
### S065 · Owned axial-motion observations

**Current outcome:** Motor, windmill, conveyor, pusher, gate and shutter reads resolve current owned joints/surfaces and reflect replacement/Restore without scene callbacks. Preserve angular travel, actuator state, endpoint/contact and full rollback semantics; no duplicated motion cache may supply physical truth.

[Complete current acceptance](../requirements.md#sequence-task-074) · [Status and technical predecessors](../work-register.md#execution-step-065).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s066"></a>
### S066 · Committed motor work accounting

**Current outcome:** Motor and pusher work/impulse totals are typed simulation-owned state, committed atomically and restored on failure/Reset. Prove powered/unpowered supply, low-force saturation, endpoint obstruction, signed work/braking and exact construction/save behavior; scene accumulators are not authoritative.

[Complete current acceptance](../requirements.md#sequence-task-075) · [Status and technical predecessors](../work-register.md#execution-step-066).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s067"></a>
### S067 · Contact-load sensing

**Current outcome:** A pressure plate derives direct-contact load from owned geometry, participation and mass, with immutable typed readings and rollback. Distinguish no contact, insufficient load and accepted load; preserve downstream gate controls, named-body identity and exact Run/Reset/save without callback-owned mass totals.

[Complete current acceptance](../requirements.md#sequence-task-076) · [Status and technical predecessors](../work-register.md#execution-step-067).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s068"></a>
### S068 · Continuous tilt sensing

**Current outcome:** Tilt sensors own their construction reference, latched state and event time. Detect qualifying tip-and-return motion within one step; disabled sensing suppresses new events without erasing a completed latch. Prove identity/order, grounded-chain contact, event/rollback and Reset controls.

[Complete current acceptance](../requirements.md#sequence-task-077) · [Status and technical predecessors](../work-register.md#execution-step-068).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s069"></a>
### S069 · Latched spring state and work

**Current outcome:** Simulation owns the latched spring's finite charge, work, latch/trigger policy and events. Preserve rotated rest-stop binding, winding/release, obstruction/resumption, retained charge, failed-tick rollback and exact Reset/save; neither a scene store nor a free launch may supply energy.

[Complete current acceptance](../requirements.md#sequence-task-078) · [Status and technical predecessors](../work-register.md#execution-step-069).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s070"></a>
### S070 · Shape-aware receiver guidance

**Current outcome:** Guide clearance uses all owned compound support geometry and current relative motion. It must not pull geometry through a receiver wall or replace solid contact; prove compound and disabled/re-enabled controls, entry/exit/descent boundaries, capture residence and exact Reset.

[Complete current acceptance](../requirements.md#sequence-task-079) · [Status and technical predecessors](../work-register.md#execution-step-070).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s071"></a>
### S071 · Moving-frame basket residence

**Current outcome:** Basket capture measures containment and speed relative to the moving receiver frame. Exit, excessive relative speed or disabled capture breaks dwell; sufficient continuous residence latches capture once for the named body. Preserve canonical margin/speed/dwell, zero-guide controls, event order, failed-tick rollback and exact Reset/save.

[Complete current acceptance](../requirements.md#sequence-task-080) · [Status and technical predecessors](../work-register.md#execution-step-071).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s072"></a>
### S072 · Continuous passage sensing

**Current outcome:** Passage sensors use the full owned compound/aperture geometry and detect directional crossings hidden between endpoints. Preserve within-trajectory arming/rearming and split-step behavior, emit exactly once per qualifying passage and reject reverse/miss/blocked/disabled controls. State and events roll back and Reset exactly.

[Complete current acceptance](../requirements.md#sequence-task-081) · [Status and technical predecessors](../work-register.md#execution-step-072).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s073"></a>
### S073 · Compound chamber eligibility

**Current outcome:** Cannon chamber occupancy/seating tests every collider child against finite cylindrical caps and radial containment. Accept valid seated payloads, reject protruding or obstructed compounds and use the moving launcher's actual frame; preserve named-body identity, atomic reload/fire and Reset.

[Complete current acceptance](../requirements.md#sequence-task-082) · [Status and technical predecessors](../work-register.md#execution-step-073).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s074"></a>
### S074 · Finite owned energy stores

**Current outcome:** Charging/release uses typed simulation-owned reservoirs and atomic commands. Account for supplied, retained, released and dissipated work without inventing energy; prove powered/unpowered, full/empty/capacity, failed-command/failed-tick and exact canonical construction Reset/save controls.

[Complete current acceptance](../requirements.md#sequence-task-083) · [Status and technical predecessors](../work-register.md#execution-step-074).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s075"></a>
### S075 · Compound swept queries

**Current outcome:** All current query consumers use full declared compound geometry and current poses, including cannon muzzle clearance and rear support. Preserve material/participation, hollow openings and contact ordering under translation/rotation; reject unsupported geometry/range atomically and restore exactly.

[Complete current acceptance](../requirements.md#sequence-task-084) · [Status and technical predecessors](../work-register.md#execution-step-075).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s076"></a>
### S076 · Compliant-contact state

**Current outcome:** Simulation owns trampoline/contact history, active potential selection, declared initial energy and entry events. Continuous membrane entry, off-centre contact, missed payload, corrupted-presentation isolation and failed-tick rollback must preserve the same physical result and exact Reset/save.

[Complete current acceptance](../requirements.md#sequence-task-085) · [Status and technical predecessors](../work-register.md#execution-step-076).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s077"></a>
### S077 · Continuous guide eligibility

**Current outcome:** Locate guide entry/exit and relative-descent transitions along actual candidate motion, including fast passage between endpoints. No current/midpoint-only decision may miss a transition. Preserve shape-aware clearance, bounded assistance, capture semantics and exact Reset.

[Complete current acceptance](../requirements.md#sequence-task-086) · [Status and technical predecessors](../work-register.md#execution-step-077).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s078"></a>
### S078 · Single physical velocity authority

**Current outcome:** All laws, queries, events and read models consume the committed/candidate WGSL physical state through typed ownership. Presentation caches cannot override velocity. Prove authored-motion, pipe-bend, contact and restoration controls under current f16 admission, without retaining a wider CPU solver.

[Complete current acceptance](../requirements.md#sequence-task-087) · [Status and technical predecessors](../work-register.md#execution-step-078).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s079"></a>
### S079 · Body-local collider offsets

**Current outcome:** Colliders retain their declared body-local offsets and compose with the owned physical pose; no silent origin-forcing path is allowed. Offset boxes, actuator/rope interactions, collision, selection and exact Reset must agree while presentation remains read-only.

[Complete current acceptance](../requirements.md#sequence-task-088) · [Status and technical predecessors](../work-register.md#execution-step-079).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s080"></a>
### S080 · Live joint identity

**Current outcome:** Commands and observations resolve typed current joint identities, kinds and ownership after atomic replacement; detached or foreign joints reject. Construction declarations and runtime bindings are distinct. Prove joint/rope/plunger/mechanical controls, restoration and stale-reference rejection.

[Complete current acceptance](../requirements.md#sequence-task-089) · [Status and technical predecessors](../work-register.md#execution-step-080).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s081"></a>
### S081 · Sliding-blade ownership

**Current outcome:** Powered gates and optical shutters present their committed physical blade pose without writing it into construction geometry. Preserve supplied opening, supply-loss closing, obstruction-stop/non-crushing controls, cargo interaction, endpoint/speed bounds and exact replay/Reset.

[Complete current acceptance](../requirements.md#sequence-task-090) · [Status and technical predecessors](../work-register.md#execution-step-081).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s082"></a>
### S082 · Explicit runtime geometry transactions

**Current outcome:** Runtime body-local geometry updates are immutable, explicit and atomic, preserving material/participation and owned identity. Live/paused construction recapture rejects. Prove pusher extension/contact, valid deformation, invalid updates, query invalidation, rollback and exact restoration.

[Complete current acceptance](../requirements.md#sequence-task-091) · [Status and technical predecessors](../work-register.md#execution-step-082).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s083"></a>
### S083 · Canonical construction orientation

**Current outcome:** One canonical typed orientation representation is shared by editor preview, content, current tools, saves and Reset; shortest-arc assistance uses the authoritative geometry. Old schemas reject atomically, and rotated construction round-trips exactly in the current canonical format. The campaign target remains 150 levels.

[Complete current acceptance](../requirements.md#sequence-task-092) · [Status and technical predecessors](../work-register.md#execution-step-083).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s084"></a>
### S084 · Guarded physical mutation

**Current outcome:** Only typed owned simulation transactions may mutate physical state. Internal numerical helpers cannot provide an alternate public mutation route. Prove invalid owner/lifecycle rejection, whole-tick rollback, exact Reset and read-only presentation isolation; API visibility alone is not ownership qualification.

[Complete current acceptance](../requirements.md#sequence-task-093) · [Status and technical predecessors](../work-register.md#execution-step-084).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s085"></a>
### S085 · Atomic physical load installation

**Current outcome:** Install one immutable coherent set of physical loads with all typed references and ranges validated before mutation. Candidate evaluation, snapshots and publication use that set. Reject partial/foreign/duplicate installation, restore failed work exactly and preserve every affected domain/caller.

[Complete current acceptance](../requirements.md#sequence-task-094) · [Status and technical predecessors](../work-register.md#execution-step-085).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s086"></a>
### S086 · Airflow field and force boundaries

**Current outcome:** Locate jet inlet, outlet and rim transitions for body and axial receivers along candidate motion, including moving occlusion and changing source supply. Integrate only accepted source/receiver work, preserve reaction accounting and rollback, and prove hidden-crossing/no-exposure controls.

[Complete current acceptance](../requirements.md#sequence-task-095) · [Status and technical predecessors](../work-register.md#execution-step-086).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s087"></a>
### S087 · Shared continuous boundary queries

**Current outcome:** Frame, rope, one-way joint and field consumers require conservative continuous boundary detection with declared finite ranges and budgets. Preserve orientation, crossing/clear and unsupported-range controls; use one current GPU query authority rather than porting an obsolete CPU search helper.

[Complete current acceptance](../requirements.md#sequence-task-096) · [Status and technical predecessors](../work-register.md#execution-step-087).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s088"></a>
### S088 · Bellows emission from current motion

**Current outcome:** Bellows compression and refill use actual relative plate motion at the evaluated physical state, not a previous substep's averaged source. Account for finite transported mass/energy, unloaded emission and receiver/reaction work; locate source/field transitions and restore the entire transaction.

[Complete current acceptance](../requirements.md#sequence-task-097) · [Status and technical predecessors](../work-register.md#execution-step-088).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s089"></a>
### S089 · Rotary airflow stage evaluation

**Current outcome:** Evaluate rotary exposure, axes, relative speed, passive resistance and shared source allocation from the coherent current candidate state. Preserve finite inertia, signed response, continuous source/field/ratio boundaries and [conserved rotary airflow](../../rotor-airflow-contract.md); no independently held drive torque is permitted.

[Complete current acceptance](../requirements.md#sequence-task-098) · [Status and technical predecessors](../work-register.md#execution-step-089).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s090"></a>
### S090 · One airflow geometry/observation path

**Current outcome:** Body and rotary loads plus read-only observations use the same typed current-pose airflow fields and owned compound occlusion. Prove rotated/moving frames, partial/blocked exposure, source changes and continuous receiver response with atomic rollback; no duplicated scene sampler decides physical behavior.

[Complete current acceptance](../requirements.md#sequence-task-099) · [Status and technical predecessors](../work-register.md#execution-step-090).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s091"></a>
### S091 · Physical cannon reload

**Current outcome:** Author payloads before Run and admit reload only through physical arrival/owned chamber geometry and atomic commands. Prove arrival and no-arrival controls, correct named payload, finite energy, obstruction, repeated firing and exact replay/Reset/save; runtime teleport/insertion cannot counterfeit loading.

[Complete current acceptance](../requirements.md#sequence-task-100) · [Status and technical predecessors](../work-register.md#execution-step-091).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s092"></a>
### S092 · Typed axial effort and physical commands

**Current outcome:** Apply axial effort and off-centre force through generic typed simulation declarations evaluated at the current physical state. No scene AddForce/AddTorque/impulse wrapper supplies independent authority. Preserve rotor/bellows/bell/chime timing, reactions, source work and exact rollback/Reset.

[Complete current acceptance](../requirements.md#sequence-task-101) · [Status and technical predecessors](../work-register.md#execution-step-092).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s093"></a>
### S093 · Coupled finite-work cannon actuation

**Current outcome:** A cannon release uses the coupled world response and finite owned reservoir, not payload mass alone. Preserve fixed/constrained payload, moving carrier and friction controls, exact enum-typed parameters/diagnostics, charging/release edge cases and canonical replay/Reset/save.

[Complete current acceptance](../requirements.md#sequence-task-102) · [Status and technical predecessors](../work-register.md#execution-step-093).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s094"></a>
### S094 · Finite-work impulse response

**Current outcome:** Compute accepted powered impulses against actual constraints, contacts and carrier motion with one source debit and owned reaction/work result. Reject unsupported or insufficient-work requests atomically; prove coupled/fixed/free payload controls and exact restoration. No scalar release fallback exists.

[Complete current acceptance](../requirements.md#sequence-task-103) · [Status and technical predecessors](../work-register.md#execution-step-094).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s095"></a>
### S095 · World-owned release and joint limits

**Current outcome:** Joint-range diagnostics, release and gameplay impulses enter through guarded typed world transactions. Preserve legal stop/release directions, linear/angular response, airflow boundary/work controls and complete ownership/rollback; direct body mutation is not a supported integration route.

[Complete current acceptance](../requirements.md#sequence-task-104) · [Status and technical predecessors](../work-register.md#execution-step-095).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s096"></a>
### S096 · Authored moving-sensor fixtures

**Current outcome:** Construct moving basket/detector controls from authored initial conditions and actual world advancement, without mutating live snapshots. Verify translation/rotation, valid/missed capture or crossing, serialization, continuous boundaries and exact whole-machine replay/Reset.

[Complete current acceptance](../requirements.md#sequence-task-105) · [Status and technical predecessors](../work-register.md#execution-step-096).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s097"></a>
### S097 · Current-state airflow force integration

**Current outcome:** Airflow body loads use current collider poses, owned state and resampling after shortened accepted intervals. Preserve continuous field boundaries, receiver timing, off-centre force/torque and source/receiver work. Fan parameters remain enums and all changes roll back atomically.

[Complete current acceptance](../requirements.md#sequence-task-106) · [Status and technical predecessors](../work-register.md#execution-step-097).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s098"></a>
### S098 · Shared airflow geometry and torque

**Current outcome:** Typed airflow fields query current compound poses, occlusion and off-centre application points through the GPU geometry owner. Positive, wall-blocked, partial and moving-frame controls must agree with actual body force and torque; no held scene-force path remains.

[Complete current acceptance](../requirements.md#sequence-task-107) · [Status and technical predecessors](../work-register.md#execution-step-098).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s099"></a>
### S099 · Gate and shutter elastic return

**Current outcome:** Gate/shutter return springs and dampers act from current joint state through generic physical laws. Preserve supplied opening, supply-loss closing, obstruction-stop/non-crushing behavior, endpoint/velocity bounds, authored cargo interaction and exact construction/rollback restoration.

[Complete current acceptance](../requirements.md#sequence-task-108) · [Status and technical predecessors](../work-register.md#execution-step-099).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s100"></a>
### S100 · Exclusive body ownership

**Current outcome:** A physical body belongs to one simulation world/generation. Reject double ownership and unauthorized standalone motion mutation; candidate integration and rollback remain inside the owning transaction. Prove all command, geometry, lifecycle and presentation boundaries.

[Complete current acceptance](../requirements.md#sequence-task-109) · [Status and technical predecessors](../work-register.md#execution-step-100).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s101"></a>
### S101 · Current collider ownership for compliant laws

**Current outcome:** Compliant laws resolve current owned collider geometry after valid replacement and cannot retain stale shapes. Prove replacement/no-change/invalid-update controls, impact-time updates, exact failed-tick replay and trampoline contact/restoration.

[Complete current acceptance](../requirements.md#sequence-task-110) · [Status and technical predecessors](../work-register.md#execution-step-101).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s102"></a>
### S102 · Moving-frame basket guidance

**Current outcome:** Guidance is a typed frame-relative physical law evaluated from current receiver and payload state. Preserve strict/no-guide controls, declared force/clearance limits, continuous eligibility, capture semantics and exact restoration without held scene forces.

[Complete current acceptance](../requirements.md#sequence-task-111) · [Status and technical predecessors](../work-register.md#execution-step-102).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s103"></a>
### S103 · Passive windmill resistance

**Current outcome:** Windmill damping acts on current relative hinge speed through the same physical transaction as conserved airflow transfer. Prove both wind signs, normal/capped/low-flow response, calm-air coast-down, external loading and rotated canonical Reset/save.

[Complete current acceptance](../requirements.md#sequence-task-112) · [Status and technical predecessors](../work-register.md#execution-step-103).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s104"></a>
### S104 · Geometry-aware trampoline forces

**Current outcome:** Trampoline compliance follows declared contact geometry, spring potential, damping and accepted interval work. Detect continuous engagement and collider changes, preserve energy without injection, and prove off-centre/loading/miss, moving-bed and exact restoration controls.

[Complete current acceptance](../requirements.md#sequence-task-113) · [Status and technical predecessors](../work-register.md#execution-step-104).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s105"></a>
### S105 · Shared body drag

**Current outcome:** Environmental and wind-chime drag use current body/frame motion in the physical evaluation, with correct force/torque and passive energy loss. Qualify every affected dynamic part, including powered-airflow/blocked controls, without sampled scene-force authority.

[Complete current acceptance](../requirements.md#sequence-task-114) · [Status and technical predecessors](../work-register.md#execution-step-105).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s106"></a>
### S106 · Shared axial damping

**Current outcome:** Axial damping uses current relative joint velocity with signed force opposing motion, owned state and coherent rollback. Bellows declares spring plus damping; prove rest, compression/refill, loaded/connected-device and passive-work controls.

[Complete current acceptance](../requirements.md#sequence-task-115) · [Status and technical predecessors](../work-register.md#execution-step-106).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s107"></a>
### S107 · Bellows elastic return

**Current outcome:** The bellows return spring is joint-bound, participates in the coupled physical solve and can refill against its declared load/environment. Prove loaded-plate and clear controls, coupled receiver/constraint behavior, signed damping, finite work and exact Reset.

[Complete current acceptance](../requirements.md#sequence-task-116) · [Status and technical predecessors](../work-register.md#execution-step-107).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s108"></a>
### S108 · Shared elastic world evaluation

**Current outcome:** Joint-bound elastic potentials participate in candidate motion, accepted work and atomic commit for every elastic consumer. Preserve gravity/initial-charge/passive-energy controls, winding and release, coupled contact and exact whole-world restoration.

[Complete current acceptance](../requirements.md#sequence-task-117) · [Status and technical predecessors](../work-register.md#execution-step-108).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s109"></a>
### S109 · Elastic potential and interval work

**Current outcome:** A shared elastic law supplies potential, instantaneous effort and consistent accepted interval work. Potential release, mechanical work and damping must balance within the current f16 contract; no held-force or hidden correction path may add energy.

[Complete current acceptance](../requirements.md#sequence-task-118) · [Status and technical predecessors](../work-register.md#execution-step-109).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s110"></a>
### S110 · Passive spring energy

**Current outcome:** For all bodies/stores, account for initial elastic/kinetic/gravitational energy and external work over the whole Run. Zero-total-initial-energy/no-external-work controls cannot create energy or motion; stored energy increase must be paid by external work or reduction of other accounted mechanical energy. Test spring, bellows, gate and shutter consumers without post-hoc energy clamps.

[Complete current acceptance](../requirements.md#sequence-task-119) · [Status and technical predecessors](../work-register.md#execution-step-110).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s111"></a>
### S111 · Joint event limits during integration

**Current outcome:** Locate stop/release and other joint boundaries in both motion directions before advancing past them. Events split or limit candidate intervals coherently and preserve signed spring/contact work, ordered event state and rollback; endpoint-only activation cannot miss an intermediate event.

[Complete current acceptance](../requirements.md#sequence-task-120) · [Status and technical predecessors](../work-register.md#execution-step-111).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s112"></a>
### S112 · Constraint-aware force response

**Current outcome:** Physical response starts from current joint/contact support and includes coupled reactions. Prove signed and rotated stop-and-release, spring loading and no-drive controls under the f16 puzzle envelope; an unconstrained seed cannot become a committed unsupported motion.

[Complete current acceptance](../requirements.md#sequence-task-121) · [Status and technical predecessors](../work-register.md#execution-step-112).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s113"></a>
### S113 · Moving-carrier impacts

**Current outcome:** Spring/bumper contact and finite-work responses use the actual translating/rotating carrier frame. Compare hit/miss and authored-motion controls, including glancing contact; preserve full body/path state, work accounting and exact construction Reset/save.

[Complete current acceptance](../requirements.md#sequence-task-122) · [Status and technical predecessors](../work-register.md#execution-step-113).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s114"></a>
### S114 · Relative impact velocities

**Current outcome:** Impact laws consume resolved angular and point velocities from owned physical state. Carrier motion must enter relative approach, reaction and work; prove stationary/translated/rotated controls and identical outcomes under different render cadences.

[Complete current acceptance](../requirements.md#sequence-task-123) · [Status and technical predecessors](../work-register.md#execution-step-114).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s115"></a>
### S115 · Moving receiver capture and guidance

**Current outcome:** Basket containment, guidance and residence use receiver point velocity and typed body identities. Prove translated and rotated frames, relative-speed/dwell and miss controls, moving-guide clearance, exact events and canonical construction Reset/save.

[Complete current acceptance](../requirements.md#sequence-task-124) · [Status and technical predecessors](../work-register.md#execution-step-115).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s116"></a>
### S116 · Moving trampoline contact frame

**Current outcome:** Damping and approach use payload velocity relative to the moving bed's contact point. Prove authored assisted-bed versus stationary controls, contact/passivity, full body/path replay and exact Reset without using cosmetic mesh velocity as physical state.

[Complete current acceptance](../requirements.md#sequence-task-125) · [Status and technical predecessors](../work-register.md#execution-step-116).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s117"></a>
### S117 · Current blade-guide binding

**Current outcome:** Gate/shutter control resolves the live owned guide after replacement, removal and Restore. Stale construction-joint objects cannot drive a blade; missing/foreign bindings reject, and obstruction/endpoints/supply-loss controls retain exact restoration.

[Complete current acceptance](../requirements.md#sequence-task-126) · [Status and technical predecessors](../work-register.md#execution-step-117).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s118"></a>
### S118 · Dynamic contact work sharing

**Current outcome:** Analytical controls must establish dynamic contact momentum/work sharing, finite energy caps, reverse separation and exact replay. Frictional motor work, coupled loads and simultaneous supplies share the same physical ledger; no body can receive duplicated source work.

[Complete current acceptance](../requirements.md#sequence-task-127) · [Status and technical predecessors](../work-register.md#execution-step-118).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s119"></a>
### S119 · Guarded hinge impulses

**Current outcome:** Hinge fixture and gameplay impulses use the typed simulation transaction and actual constraints. Preserve restitution/passivity, stopped-hinge and release controls, invalid ownership rejection and whole-world replay; do not bypass the world to force a test pose.

[Complete current acceptance](../requirements.md#sequence-task-128) · [Status and technical predecessors](../work-register.md#execution-step-119).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s120"></a>
### S120 · Complete affected integration checks

**Current outcome:** Qualify the actual candidate's affected positive/control/boundary and lifecycle matrix, including stopped-hinge/passivity, pendulum energy, coaxial sweeps and rotated canonical saves. Focused subsets do not close engine release; old process handles and runs are not current tasks.

[Complete current acceptance](../requirements.md#sequence-task-129) · [Status and technical predecessors](../work-register.md#execution-step-120).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s121"></a>
### S121 · Impact-triggered motor budgets

**Current outcome:** Impact-triggered actuation must respect current locked/released joint state, finite source work, miss controls and full body/effect/event replay. Friction and simultaneous supplies participate in the same accepted transaction; no post-lock impulse expectation may bypass a physical stop.

[Complete current acceptance](../requirements.md#sequence-task-130) · [Status and technical predecessors](../work-register.md#execution-step-121).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s122"></a>
### S122 · Contact-aware motor work

**Current outcome:** Contact normals and friction constrain finite-work actuation together with joints. Prove signed blocked/release/braking, thin-obstacle pusher and impact-joint controls, simultaneous supplies, exact work reporting and rollback.

[Complete current acceptance](../requirements.md#sequence-task-131) · [Status and technical predecessors](../work-register.md#execution-step-122).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s123"></a>
### S123 · Owned scene-joint commands

**Current outcome:** Every scene-admitted joint impulse becomes a typed owned simulation command. Prove transmission and lever outcomes, no-mutation rejection and contact-aware work; no direct scene/body impulse route remains in production.

[Complete current acceptance](../requirements.md#sequence-task-132) · [Status and technical predecessors](../work-register.md#execution-step-123).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s124"></a>
### S124 · Superseded bilateral-only helper; no current action

**Superseded; no current action.** The retired CPU bilateral-only response implementation is not a required GPU port. Current coupled contact/joint response, finite motor work and one-authority cleanup are specified by the active contact/drive contracts and standing requirements.

[Complete current acceptance](../requirements.md#sequence-task-133) · [Status and technical predecessors](../work-register.md#execution-step-124).

This retained identity is not an implementation prerequisite or a runtime Pass.

<a id="s125"></a>
### S125 · Direction-aware constrained motors

**Current outcome:** Finite-work motors honor joint constraints, unilateral directions and boundary crossings. Prove stationary stops, hinge/slider braking and release, contact reactions, simultaneous supplies and exact replay; source work cannot be charged for forbidden motion or invented at release.

[Complete current acceptance](../requirements.md#sequence-task-134) · [Status and technical predecessors](../work-register.md#execution-step-125).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s126"></a>
### S126 · Admissible impulse directions

**Current outcome:** One-way/release/transmission response must distinguish admitted directions and reject unsupported state without mutation. Integrate the response with finite source work and velocity-boundary/contact transitions; a standalone algebraic result cannot authorize unbudgeted time advancement.

[Complete current acceptance](../requirements.md#sequence-task-135) · [Status and technical predecessors](../work-register.md#execution-step-126).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s127"></a>
### S127 · Joint/contact coupling

**Current outcome:** Contact and bilateral joint constraints share one coherent physical solution and accepted state. Prove constrained mass, changed identity order and exact replay, including loaded contacts and motor work, without a second post-processing solver that invalidates earlier constraints.

[Complete current acceptance](../requirements.md#sequence-task-136) · [Status and technical predecessors](../work-register.md#execution-step-127).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s128"></a>
### S128 · Physical integration fixtures

**Current outcome:** Lever, rope, plunger, pusher and diagnostics fixtures use owned commands and shared physical advancement. Preserve loaded-contact, passivity, negative/control and exact restoration cases; no fixture may obtain success through direct live-body mutation.

[Complete current acceptance](../requirements.md#sequence-task-137) · [Status and technical predecessors](../work-register.md#execution-step-128).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s129"></a>
### S129 · Internal-body construction state

**Current outcome:** Canonical construction saves include declared internal-body initial motion using typed roles and one orientation/state representation. Reject malformed roles/ranges and obsolete schemas atomically; prove plunger/lifecycle restoration without a separate Reset velocity cache.

[Complete current acceptance](../requirements.md#sequence-task-138) · [Status and technical predecessors](../work-register.md#execution-step-129).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s130"></a>
### S130 · Authored initial motion persistence

**Current outcome:** Declared initial linear/angular motion survives canonical save/load and Run/Reset for ball, weight and internal bodies. Validate vector dimensions/ranges/finite values and exact canonical orientation, keeping saved construction distinct from running physical state.

[Complete current acceptance](../requirements.md#sequence-task-139) · [Status and technical predecessors](../work-register.md#execution-step-130).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s131"></a>
### S131 · Scene command boundary closure

**Current outcome:** Every lever, diagnostic, plunger, geometry and rope caller uses typed owned physical commands. Verify actual outcomes and rejection/rollback through the current transaction; migrating a call signature alone does not qualify behavior.

[Complete current acceptance](../requirements.md#sequence-task-140) · [Status and technical predecessors](../work-register.md#execution-step-131).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s132"></a>
### S132 · Independent coupling groups

**Current outcome:** Exactly independent physical coupling groups must not contaminate one another's scale, state or results. Prove disparate admitted scales, identity/order changes and shared-body controls, plus bellows-to-chime positive and wall-blocked cases, within the current f16 representation.

[Complete current acceptance](../requirements.md#sequence-task-141) · [Status and technical predecessors](../work-register.md#execution-step-132).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s133"></a>
### S133 · Finite Coulomb response

**Current outcome:** Coulomb contact evaluation must guard operands/intermediates before overflow or invalid operations and preserve passivity at admitted scales. Prove trampoline and mixed-scale bellows controls, finite saturation, coupled actuator behavior and atomic failure; no speculative fallback solver.

[Complete current acceptance](../requirements.md#sequence-task-142) · [Status and technical predecessors](../work-register.md#execution-step-133).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s134"></a>
### S134 · Grazing and small-constraint admission

**Current outcome:** Admit only representable f16 constraint/contact ranges, including grazing and low-force cases. Unsupported tiny inputs reject explicitly rather than depending on subnormal persistence. Prove stable gate/trampoline contact, signed blocked/release behavior and no artificial energy.

[Complete current acceptance](../requirements.md#sequence-task-143) · [Status and technical predecessors](../work-register.md#execution-step-134).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s135"></a>
### S135 · Physical gate collision fixtures

**Current outcome:** Gate/shutter collision controls use authored moving cargo, real typed supply and owned impacts. Prove opening/closing, endpoint/speed and obstruction-stop/non-crushing outcomes with exact Reset/replay; runtime insertion or teleport cannot counterfeit contact.

[Complete current acceptance](../requirements.md#sequence-task-144) · [Status and technical predecessors](../work-register.md#execution-step-135).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s136"></a>
### S136 · Gameplay tick lifecycle

**Current outcome:** Typed lifecycle guards reject recursive Step, Reset and Load without corrupting state. Completion notification occurs only after a whole committed tick; all domain state, events and loads participate in rollback. Prove reentrancy, failure/cancellation and actual UI restoration.

[Complete current acceptance](../requirements.md#sequence-task-145) · [Status and technical predecessors](../work-register.md#execution-step-136).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s137"></a>
### S137 · Coupled acceleration constraints

**Current outcome:** Equality blocks and zero-width locks must produce coupled acceleration consistent with all participating bodies. Qualify spring convergence, unilateral motor work, pendulum energy and rotated save/load within the current f16 budget; do not require a particular CPU factorization.

[Complete current acceptance](../requirements.md#sequence-task-146) · [Status and technical predecessors](../work-register.md#execution-step-137).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s138"></a>
### S138 · Constraint classification and response

**Current outcome:** Classify equality and inequality declarations explicitly; jointly solve their momentum/reaction and acceleration response with contact. Preserve unilateral actuation and atomic rejection of inadmissible systems.

[Complete current acceptance](../requirements.md#sequence-task-147) · [Status and technical predecessors](../work-register.md#execution-step-138).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s139"></a>
### S139 · Arbitrary coupled pre-actuation response

**Current outcome:** Qualify coupled equalities on two-, eight- and thirty-two-body rings, including more than six equations, zero-work stationary stops, springs and unilateral/simultaneous supplies. A hidden pair-only or fixed small-system solver is not an acceptable current path.

[Complete current acceptance](../requirements.md#sequence-task-148) · [Status and technical predecessors](../work-register.md#execution-step-139).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s140"></a>
### S140 · Live driven loops

**Current outcome:** Single-link, redundant and locked world loops must respect physical work and exact replay. Stationary stops add essentially zero mechanical work; unilateral and simultaneous supplies share the connected response.

[Complete current acceptance](../requirements.md#sequence-task-149) · [Status and technical predecessors](../work-register.md#execution-step-140).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s141"></a>
### S141 · Redundant constraint admission

**Current outcome:** Dependent constraint equations must preserve the consistent coupled solution; inconsistent targets must reject before outputs or world state change. Qualify driven-loop stress, declaration-order controls and the current precision envelope without mandating an obsolete CPU rank algorithm.

[Complete current acceptance](../requirements.md#sequence-task-150) · [Status and technical predecessors](../work-register.md#execution-step-141).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s142"></a>
### S142 · Reentrant world transaction safety

**Current outcome:** Reject unsupported reentrant external impulses, nested stepping, snapshot capture/restore, joint replacement, surface replacement and collider updates. Exercise both caught and propagated rejection for each category. Restore the complete world, permit identical replay and safe reuse after recoverable rejection, and fault unrecoverable restore failures.

[Complete current acceptance](../requirements.md#sequence-task-151) · [Status and technical predecessors](../work-register.md#execution-step-142).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s143"></a>
### S143 · Independent mechanical islands

**Current outcome:** An unrelated belt or other disconnected mechanism must not alter another island's effective mass, work or response. Qualify driven redundant loops, unilateral motors, stationary stops, springs and rotated save/load.

[Complete current acceptance](../requirements.md#sequence-task-152) · [Status and technical predecessors](../work-register.md#execution-step-143).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s144"></a>
### S144 · RGB filter physical impact

**Current outcome:** Filter impact, rebound and exact Reset/replay must follow shared physics. Filter choices remain enums through configuration and genuine serialization boundaries; changing construction settings during a Run rejects without mutating the runtime payload.

[Complete current acceptance](../requirements.md#sequence-task-153) · [Status and technical predecessors](../work-register.md#execution-step-144).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s145"></a>
### S145 · Owned gameplay impulses

**Current outcome:** Cannon and other gameplay impulses target registered dynamic bodies through the owned world at an admitted transaction boundary, including supported Idle application. Reject wrong-phase, invalid/foreign/non-dynamic targets and reentrant mutation atomically; no part writes physical velocity directly.

[Complete current acceptance](../requirements.md#sequence-task-154) · [Status and technical predecessors](../work-register.md#execution-step-145).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s146"></a>
### S146 · Signed guide and transmission work

**Current outcome:** Qualify signed hinge/slider actuation with open, engaged and ratio-coupled transmissions, stationary stops, unilateral/simultaneous supplies and redundant guides. Work follows actual connected motion and paired reactions.

[Complete current acceptance](../requirements.md#sequence-task-155) · [Status and technical predecessors](../work-register.md#execution-step-146).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s147"></a>
### S147 · Arbitrary-body effective mass

**Current outcome:** Compute coupled response, work and rejection for two-, eight- and sixteen-body systems, including more than six equations. The WGSL implementation must preserve physical coupling without a pair-only shortcut.

[Complete current acceptance](../requirements.md#sequence-task-156) · [Status and technical predecessors](../work-register.md#execution-step-147).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s148"></a>
### S148 · Multi-body finite-work impulses

**Current outcome:** Finite-work impulses share actual coupled effective mass and reaction across all participants. Qualify a three-body work/momentum case, braking, atomic rejection and exact replay.

[Complete current acceptance](../requirements.md#sequence-task-157) · [Status and technical predecessors](../work-register.md#execution-step-148).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s149"></a>
### S149 · Connected actuation energy

**Current outcome:** Uncoupled and two-mass kicks must reach the physically admissible response under their finite work budget. A stationary stop must not consume fictitious mechanical work; solve connected motion and accounting together, without report-only refunds or component-specific bypasses.

[Complete current acceptance](../requirements.md#sequence-task-158) · [Status and technical predecessors](../work-register.md#execution-step-149).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s150"></a>
### S150 · Wound-spring winding and release

**Current outcome:** Qualify inward winding with finite shaft inertia and outward release through open and engaged transmissions. Preserve the finite elastic store, physical load response and teaching behavior without a prescribed launch velocity.

[Complete current acceptance](../requirements.md#sequence-task-159) · [Status and technical predecessors](../work-register.md#execution-step-150).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s151"></a>
### S151 · Construction preparation lifecycle

**Current outcome:** Preparing construction while running or paused must reject for root and internal bodies without recapturing runtime poses. Preserve canonical orientation and the whole transaction.

[Complete current acceptance](../requirements.md#sequence-task-160) · [Status and technical predecessors](../work-register.md#execution-step-151).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s152"></a>
### S152 · Construction-owned rope capture

**Current outcome:** Rope sockets and routes derive from captured owned construction geometry. Preparing while running or paused rejects; no unused historical helper is required as a second authority.

[Complete current acceptance](../requirements.md#sequence-task-161) · [Status and technical predecessors](../work-register.md#execution-step-152).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s153"></a>
### S153 · Typed generic point effects

**Current outcome:** Point force and torque declarations and fixture choices remain enum-typed through the domain. Chimes use the generic physical equations; qualify force, sensing, replay, invalid choices and exact Reset.

[Complete current acceptance](../requirements.md#sequence-task-162) · [Status and technical predecessors](../work-register.md#execution-step-153).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s154"></a>
### S154 · Gate motion and obstruction

**Current outcome:** Powered gate opening is monotonic between declared endpoints. Qualify obstruction from all relevant approaches, cannon clearance and passage through the moved gate, plus exact Reset; cannon success alone does not qualify the gate.

[Complete current acceptance](../requirements.md#sequence-task-163) · [Status and technical predecessors](../work-register.md#execution-step-154).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s155"></a>
### S155 · Cannon loading boundaries

**Current outcome:** Qualify charged and unpowered loading, trigger-time admission, late-arrival rejection and reload obstruction. Save/load and Reset restore the exact construction without preserving transient chamber state.

[Complete current acceptance](../requirements.md#sequence-task-164) · [Status and technical predecessors](../work-register.md#execution-step-155).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s156"></a>
### S156 · Stationary stop work

**Current outcome:** A stationary slider stop adds essentially zero mechanical work within the declared f16 accounting budget. This holds with signed generic contacts and transmissions without component bypasses, energy clamps or report-only refunds.

[Complete current acceptance](../requirements.md#sequence-task-165) · [Status and technical predecessors](../work-register.md#execution-step-156).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s157"></a>
### S157 · Captured configuration immutability

**Current outcome:** Configuration changes to captured root or internal bodies reject before mutation while running or paused. Reset restores the authored settings and physical declarations.

[Complete current acceptance](../requirements.md#sequence-task-166) · [Status and technical predecessors](../work-register.md#execution-step-157).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s158"></a>
### S158 · Owned rope presentation

**Current outcome:** Rope artwork reads the required world route, owned knots, endpoints and pulley frames. Qualify finite-rim routing and immunity to displayed-pose corruption during Run, pause and Reset.

[Complete current acceptance](../requirements.md#sequence-task-167) · [Status and technical predecessors](../work-register.md#execution-step-158).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s159"></a>
### S159 · Owned runtime signatures

**Current outcome:** Runtime observations/signatures use owned position, velocity, angular motion, motion type, participation and mass-centre state, not scene visibility. Include joints, components and current collider geometry in replay qualification.

[Complete current acceptance](../requirements.md#sequence-task-168) · [Status and technical predecessors](../work-register.md#execution-step-159).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s160"></a>
### S160 · Required typed physics parameters

**Current outcome:** Require every declared physics parameter and typed selector; reject missing, unknown and undefined values without string fallbacks or inferred defaults. Author drag and buoyancy explicitly for each supported resource/body type.

[Complete current acceptance](../requirements.md#sequence-task-169) · [Status and technical predecessors](../work-register.md#execution-step-160).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s161"></a>
### S161 · Construction-only resizing

**Current outcome:** Wall and pipe resizing must atomically update construction geometry and proxies. Reject resizing during Run or pause; exact Reset preserves the accepted construction.

[Complete current acceptance](../requirements.md#sequence-task-170) · [Status and technical predecessors](../work-register.md#execution-step-161).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s162"></a>
### S162 · Owned blade orientation

**Current outcome:** Blade forces derive from owned orientation, never the displayed transform. Straight and rotated controls must remain physically identical under presentation corruption.

[Complete current acceptance](../requirements.md#sequence-task-171) · [Status and technical predecessors](../work-register.md#execution-step-162).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s163"></a>
### S163 · Owned pusher geometry

**Current outcome:** Pusher root/head colliders and the finite-mass telescoping shaft follow owned physical state and atomic collider updates. Presentation cannot write physics. Qualify thin obstacles, low-force support, deformation and exact Reset.

[Complete current acceptance](../requirements.md#sequence-task-172) · [Status and technical predecessors](../work-register.md#execution-step-163).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s164"></a>
### S164 · Typed construction body identity

**Current outcome:** Capture construction poses and declared initial motion for every root and internal body under typed construction identities. Run and Reset must restore the exact corresponding body, settings and links.

[Complete current acceptance](../requirements.md#sequence-task-173) · [Status and technical predecessors](../work-register.md#execution-step-164).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s165"></a>
### S165 · Construction-only tube snapping

**Current outcome:** Tube snapping validates ownership and construction state. Reject running, paused and foreign-part operations atomically; Reset restores exact eligibility and placement.

[Complete current acceptance](../requirements.md#sequence-task-174) · [Status and technical predecessors](../work-register.md#execution-step-165).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s166"></a>
### S166 · Immutable Reset snapshots

**Current outcome:** The owned Reset snapshot remains immutable. Exported observations are detached copies and cannot mutate live or paused state.

[Complete current acceptance](../requirements.md#sequence-task-175) · [Status and technical predecessors](../work-register.md#execution-step-166).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s167"></a>
### S167 · Authored mechanical controls

**Current outcome:** Qualify fixed relay/latch controls, wall initial velocity authored before Run, three rotations and construction disconnection. Supply loss permits physical coasting without adding work or erasing momentum.

[Complete current acceptance](../requirements.md#sequence-task-176) · [Status and technical predecessors](../work-register.md#execution-step-167).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s168"></a>
### S168 · Counter and gate crossing controls

**Current outcome:** Counters use preauthored motion and qualify two-event, negative and entity-order controls with exact Reset. Gates qualify four approaches and high-speed obstruction, including the required stop/non-crush behavior.

[Complete current acceptance](../requirements.md#sequence-task-177) · [Status and technical predecessors](../work-register.md#execution-step-168).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s169"></a>
### S169 · Read-only rope membership

**Current outcome:** Route membership and constraint identities come from one physical capture and remain read-only. Running/paused observations and Reset must preserve the correct route identities.

[Complete current acceptance](../requirements.md#sequence-task-178) · [Status and technical predecessors](../work-register.md#execution-step-169).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s170"></a>
### S170 · Owned connection collections

**Current outcome:** Expose read-only connection views and detached snapshots; edits pass through guarded construction operations. Reset/load preserve stable views and topology without relying on live part ordering.

[Complete current acceptance](../requirements.md#sequence-task-179) · [Status and technical predecessors](../work-register.md#execution-step-170).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s171"></a>
### S171 · Fixed optical/electrical controls

**Current outcome:** Gate and shutter supply changes use authored wiring/latch roles, typed through the domain. Missing contacts reject; removing a live delay input rejects atomically and Reset restores the fixed construction.

[Complete current acceptance](../requirements.md#sequence-task-180) · [Status and technical predecessors](../work-register.md#execution-step-171).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s172"></a>
### S172 · Construction electrical wiring

**Current outcome:** Connect/disconnect are validated construction operations. Relay and motor supply-loss tests use fixed latch controls and preserve physical momentum/coasting.

[Complete current acceptance](../requirements.md#sequence-task-181) · [Status and technical predecessors](../work-register.md#execution-step-172).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s173"></a>
### S173 · Owned light, sound and solar inputs

**Current outcome:** Laser, speaker and solar power follow fixed contacts and owned poses/participation, including aperture, occlusion and range. Presentation corruption is a meaningful negative/control.

[Complete current acceptance](../requirements.md#sequence-task-182) · [Status and technical predecessors](../work-register.md#execution-step-173).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s174"></a>
### S174 · Clock and latch topology

**Current outcome:** Qualify clock hold and both latch/switch states with fixed authored wiring. Running/paused disconnect rejects and Reset restores the exact topology.

[Complete current acceptance](../requirements.md#sequence-task-183) · [Status and technical predecessors](../work-register.md#execution-step-174).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s175"></a>
### S175 · Immutable typed connection specifications

**Current outcome:** Connection specifications are immutable and enum-typed; replacements validate atomically and reject undefined domains. No caller mutates a live connection collection.

[Complete current acceptance](../requirements.md#sequence-task-184) · [Status and technical predecessors](../work-register.md#execution-step-175).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s176"></a>
### S176 · Read-only placed-part membership

**Current outcome:** Expose read-only placed-part membership and validate catalogue/custom attachment. Live reordering cannot change physical outcomes or bypass ownership.

[Complete current acceptance](../requirements.md#sequence-task-185) · [Status and technical predecessors](../work-register.md#execution-step-176).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s177"></a>
### S177 · World membership lifecycle

**Current outcome:** Guard add, remove, paused Start and connection operations; expose read-only bodies. Reject foreign-world removal and preserve the complete transaction on failure.

[Complete current acceptance](../requirements.md#sequence-task-186) · [Status and technical predecessors](../work-register.md#execution-step-177).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s178"></a>
### S178 · Superseded process polling; no current action

**Superseded; no current action.** The instruction to poll a particular September native process is obsolete and creates no current implementation task. Current sweep, passivity, pendulum and rotated-save behavior remains in the corresponding current criteria.

[Complete current acceptance](../requirements.md#sequence-task-187) · [Status and technical predecessors](../work-register.md#execution-step-178).

This retained identity is not an implementation prerequisite or a runtime Pass.

<a id="s179"></a>
### S179 · Declared initial motion restoration

**Current outcome:** Reset restores placed and internal bodies' declared construction initial motion, never captured runtime motion. Save/load, topology and failed operations remain transactional.

[Complete current acceptance](../requirements.md#sequence-task-188) · [Status and technical predecessors](../work-register.md#execution-step-179).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s180"></a>
### S180 · Owned body telemetry

**Current outcome:** Expose owned physical position, velocity, mass-centre offset and typed sub-body participation during Run and pause. Displayed scene state is not physical authority.

[Complete current acceptance](../requirements.md#sequence-task-189) · [Status and technical predecessors](../work-register.md#execution-step-180).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s181"></a>
### S181 · Construction versus presented velocity

**Current outcome:** Initial velocity is an authored construction value; runtime presentation is read-only. Running/paused initial-motion mutation rejects; current callers, persistence and Reset preserve this boundary.

[Complete current acceptance](../requirements.md#sequence-task-190) · [Status and technical predecessors](../work-register.md#execution-step-181).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s182"></a>
### S182 · One shared friction authority

**Current outcome:** All callers use the generic contact/friction equations; no per-part friction helper or second numerical authority remains. Qualify topology, initial/runtime motion boundaries and all supported part outcomes.

[Complete current acceptance](../requirements.md#sequence-task-191) · [Status and technical predecessors](../work-register.md#execution-step-182).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s183"></a>
### S183 · One mechanical energy network

**Current outcome:** Authored sockets bind owned shared guides, so source, route and load participate in one motion/work calculation. Qualify spring winding/release, clutch power loss/coasting/re-engagement and loops without copied speed/torque or independent downstream motor commands.

[Complete current acceptance](../requirements.md#sequence-task-192) · [Status and technical predecessors](../work-register.md#execution-step-183).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s184"></a>
### S184 · Finite-inertia rotary transmission

**Current outcome:** Implement the complete [rotary transmission contract](../../rotary-transmission-parts.md): reverse gearbox and clutch shafts, bidirectional coupling, typed engagement, external socket binding, winding linkage, exact lifecycle and save/load.

[Complete current acceptance](../requirements.md#sequence-task-193) · [Status and technical predecessors](../work-register.md#execution-step-184).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s185"></a>
### S185 · Physical source rotors

**Current outcome:** Motor and windmill sources use finite-inertia owned rotors, guides and accepted torque/work. Source and load share one calculation; supply loss removes effort without erasing momentum. Qualify rotated save/load, actual committed shaft events/poses and source/load controls.

[Complete current acceptance](../requirements.md#sequence-task-194) · [Status and technical predecessors](../work-register.md#execution-step-185).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s186"></a>
### S186 · Typed guide dependencies

**Current outcome:** Mechanical parts declare typed guide dependencies and bind every socket to the owned guide. Missing/cyclic dependencies and dangling runtime replacements reject atomically; no scalar speed/work propagation fallback remains.

[Complete current acceptance](../requirements.md#sequence-task-195) · [Status and technical predecessors](../work-register.md#execution-step-186).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s187"></a>
### S187 · Prescribed-motion obstruction

**Current outcome:** Reject conflicting fixed/kinematic and independent prescribed motion atomically, including collider enabling/replacement. Handle shared-frame children and disabled colliders explicitly. Assistance must plan feasible motion, provide player feedback and preserve whole-world rollback.

[Complete current acceptance](../requirements.md#sequence-task-196) · [Status and technical predecessors](../work-register.md#execution-step-187).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s188"></a>
### S188 · Owned assistance execution

**Current outcome:** Advance assistance through owned continuous trajectories with body/COM offsets, snapshot cursors, derivative bounds and contact/joint acceleration reactions. Qualify pusher support, pendulum energy, obstruction, exact Reset/replay and whole-tick rollback.

[Complete current acceptance](../requirements.md#sequence-task-197) · [Status and technical predecessors](../work-register.md#execution-step-188).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s189"></a>
### S189 · Bounded varying trajectories

**Current outcome:** Prescribed motion supports actual varying acceleration, interior extrema, angular bounds and multiple full turns. Capture immutable profiles and owned cursors, map compound/COM motion correctly, advance exactly within declared f16 budgets and reject unsupported inputs explicitly.

[Complete current acceptance](../requirements.md#sequence-task-198) · [Status and technical predecessors](../work-register.md#execution-step-189).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s190"></a>
### S190 · Physics-owned rope and impact effects

**Current outcome:** Rope constraints and impact-local effects execute through the shared world. Assistance corrections must actually advance as owned continuous motion; direct scene pose/velocity writes cannot substitute for execution.

[Complete current acceptance](../requirements.md#sequence-task-199) · [Status and technical predecessors](../work-register.md#execution-step-190).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s191"></a>
### S191 · Contact participation

**Current outcome:** Bell and trampoline forces/events depend on declared physical participation, not rendered visibility. Disabling the trampoline clears its active contacts; qualify ownership, re-enable, exact Reset and replay.

[Complete current acceptance](../requirements.md#sequence-task-200) · [Status and technical predecessors](../work-register.md#execution-step-191).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s192"></a>
### S192 · Owned optical output accounting

**Current outcome:** Routed optical outputs use captured shared-body outlet poses and physical energy accounting. Combiner and logic presentation transforms cannot become a second optical authority.

[Complete current acceptance](../requirements.md#sequence-task-201) · [Status and technical predecessors](../work-register.md#execution-step-192).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s193"></a>
### S193 · Compound pressure-plate contacts

**Current outcome:** Pressure plates use generic shared compound contacts. Qualify gate timing, supported shape/query boundaries, negative/control, exact Reset and real UI behavior without rendered-position/radius proximity logic.

[Complete current acceptance](../requirements.md#sequence-task-202) · [Status and technical predecessors](../work-register.md#execution-step-193).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s194"></a>
### S194 · Initially disabled bodies

**Current outcome:** Every authored body retains a shared identity even when initially hidden. Collision/participation is an explicit validated typed declaration; qualify initially disabled detectors, enable transitions and lifecycle restoration.

[Complete current acceptance](../requirements.md#sequence-task-203) · [Status and technical predecessors](../work-register.md#execution-step-194).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s195"></a>
### S195 · Detector and escape ownership

**Current outcome:** Detectors and escape checks read solved positions and participation. Qualify initially hidden bodies, positive presentation-distortion controls and exact lifecycle restoration.

[Complete current acceptance](../requirements.md#sequence-task-204) · [Status and technical predecessors](../work-register.md#execution-step-195).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s196"></a>
### S196 · Physical cannon energy

**Current outcome:** Cannon chamber admission, loading and launch use owned physical state and finite energy release under current f16 authority. Fixtures author mutations before Start; qualify obstruction, replay, trigger controls and save/load.

[Complete current acceptance](../requirements.md#sequence-task-205) · [Status and technical predecessors](../work-register.md#execution-step-196).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s197"></a>
### S197 · Owned network sampling and capture

**Current outcome:** Networks, anchored airflow samples, emitters and basket guidance/capture read shared body state. Qualify all caller migration, numerical/work limits and exact Reset without rendered-pose authority.

[Complete current acceptance](../requirements.md#sequence-task-206) · [Status and technical predecessors](../work-register.md#execution-step-197).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s198"></a>
### S198 · Owned runtime queries

**Current outcome:** Sweeps, traces and snapshots read owned poses, participation and current registered collider metadata. Collider replacements commit query metadata atomically and snapshots restore it exactly; migrate every emitter/receiver/aperture caller.

[Complete current acceptance](../requirements.md#sequence-task-207) · [Status and technical predecessors](../work-register.md#execution-step-198).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s199"></a>
### S199 · Shared hollow separation queries

**Current outcome:** Tube, frustum and bend geometry use shared compound separation/query declarations and independent analytic witnesses. Qualify exterior-wall and current collider controls without retired per-shape surface algorithms.

[Complete current acceptance](../requirements.md#sequence-task-208) · [Status and technical predecessors](../work-register.md#execution-step-199).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s200"></a>
### S200 · One static shell collision path

**Current outcome:** All static tube/frustum shell callers use shared compounds. Qualify long coaxial convergence and independent analytic controls; no retired static intersection fallback remains.

[Complete current acceptance](../requirements.md#sequence-task-209) · [Status and technical predecessors](../work-register.md#execution-step-200).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s201"></a>
### S201 · One hollow continuous collision path

**Current outcome:** Tube and funnel motion use bounded shared compounds and captured trajectories. Qualify long coaxial sweeps, oversized spring loads and release energy within current f16 budgets, without specialized shell-sweep fallback.

[Complete current acceptance](../requirements.md#sequence-task-210) · [Status and technical predecessors](../work-register.md#execution-step-201).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s202"></a>
### S202 · One shared sweep path

**Current outcome:** Moving/rotating sphere and box callers use generic captured trajectories and continuous collision. Qualify long axial tangency and initial overlap; no old specialized sweep/result API remains as a fallback.

[Complete current acceptance](../requirements.md#sequence-task-211) · [Status and technical predecessors](../work-register.md#execution-step-202).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s203"></a>
### S203 · Shared impact dispatch

**Current outcome:** All spring and bumper effects come from actual shared impacts; impulses and collider changes are transactional world commands. Qualify glancing spin and every supported bumper difficulty, without a scene-owned contact hook.

[Complete current acceptance](../requirements.md#sequence-task-212) · [Status and technical predecessors](../work-register.md#execution-step-203).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s204"></a>
### S204 · Physical conveyor transport

**Current outcome:** A finite-inertia shaft/carrier drives declared material contacts with equal/opposite reactions; multiple loads share that shaft and unpowered transport coasts. Side/underside contacts remain ordinary. Qualify slip, finite work, solved presentation, exact lifecycle and mechanical coupling.

[Complete current acceptance](../requirements.md#sequence-task-213) · [Status and technical predecessors](../work-register.md#execution-step-204).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s205"></a>
### S205 · Multi-participant driven contacts

**Current outcome:** Generic impulse/friction and acceleration contacts couple sparse gradients across every participant, including finite-energy shaft reaction torque. Predict material-slip boundaries and accepted mechanical work continuously; no pair-only or direct conveyor callback authority remains.

[Complete current acceptance](../requirements.md#sequence-task-214) · [Status and technical predecessors](../work-register.md#execution-step-205).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s206"></a>
### S206 · Shared contact and motion authority

**Current outcome:** Ratchet and contact behavior use shared bodies, constraints and replay. Remove active direct scene-velocity resolution and obsolete motion hooks; preserve conveyor, sweep, work, query and transaction behavior through the current world.

[Complete current acceptance](../requirements.md#sequence-task-215) · [Status and technical predecessors](../work-register.md#execution-step-206).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s207"></a>
### S207 · Physical lever integration

**Current outcome:** Lever beam geometry, finite-mass bodies and frame joints have one shared authority. Qualify support convergence, stop-contact work, seeded energy, exact Reset and generic powered contacts without a separate hinged-body solver.

[Complete current acceptance](../requirements.md#sequence-task-216) · [Status and technical predecessors](../work-register.md#execution-step-207).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s208"></a>
### S208 · Physical wind chimes

**Current outcome:** A finite-mass sail/clapper, ball-socket pivot and individually identified convex tube bodies own chime motion/contact. Qualify positive airflow/coast, every tube/load permutation, analytic drag, physical contact identity, exact Reset and rollback without a local pendulum/contact solver.

[Complete current acceptance](../requirements.md#sequence-task-217) · [Status and technical predecessors](../work-register.md#execution-step-208).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s209"></a>
### S209 · Physical bellows

**Current outcome:** A finite-mass slider plate with return spring and pumping resistance produces airflow from actual compression. Qualify loading/unloading/refill cycles, continuous force/airflow work, rollback and intended campaign loads; no impact-energy target or scripted plate authority remains.

[Complete current acceptance](../requirements.md#sequence-task-218) · [Status and technical predecessors](../work-register.md#execution-step-209).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s210"></a>
### S210 · Physical wound spring

**Current outcome:** Winding and release use finite-work actuation, solved head motion and shared latch/ratchet/stops. Qualify 0.5 kg and 2 kg payload masses, no-trigger controls, winding obstruction, release energy, continuous forces and complete rollback without direct head-velocity or kinematic launch APIs.

[Complete current acceptance](../requirements.md#sequence-task-219) · [Status and technical predecessors](../work-register.md#execution-step-210).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s211"></a>
### S211 · Physical trampoline membrane

**Current outcome:** The compliant membrane derives force/torque from actual support geometry and rotational effective mass. Spheres, rotated boxes and compounds share one query path. Qualify rigid-rim contacts, continuous compliant entry, general-load visuals and complete rollback.

[Complete current acceptance](../requirements.md#sequence-task-220) · [Status and technical predecessors](../work-register.md#execution-step-211).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s212"></a>
### S212 · Physical domino propagation

**Current outcome:** A finite-mass box with declared offset centre of mass tips through shared contact. Output observes actual tipping and signal input rejects. Qualify close and separated gaps, grounded chains, all authored/reference placements, connections, assistance and exact Reset without timed/proximity propagation.

[Complete current acceptance](../requirements.md#sequence-task-221) · [Status and technical predecessors](../work-register.md#execution-step-212).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s213"></a>
### S213 · Physical linear pusher

**Current outcome:** The finite-mass head, slider, bounded motor, locking constraints and finite-mass telescoping shaft share continuous physics and observed presentation. Qualify thin obstacles, low-force vertical support, stationary-stop work, deformation, controllers/energy transactions and exact Reset without direct cargo velocity writes.

[Complete current acceptance](../requirements.md#sequence-task-222) · [Status and technical predecessors](../work-register.md#execution-step-213).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s214"></a>
### S214 · Persistent shared-world integration

**Current outcome:** All current components, queries, effects, forces, stores and controls use the persistent WGSL world before P0-035. Gates/shutters have finite-mass sliders, bounded motors and return springs; remove remaining active legacy helpers/hooks. Native checks supplement, and cannot replace, current real-UI/worker/lifecycle/performance qualification.

[Complete current acceptance](../requirements.md#sequence-task-223) · [Status and technical predecessors](../work-register.md#execution-step-214).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s215"></a>
### S215 · Constrained integration accuracy

**Current outcome:** An authored-friction lever, pendulum and rope-loaded machine must complete intended motion without nonconvergence or unbounded energy error. Exact Run/Reset repeats the outcome. Integrate all state/effects/forces/stores/constraints through the current world; no damping/clamps may disguise accounting defects.

[Complete current acceptance](../requirements.md#sequence-task-224) · [Status and technical predecessors](../work-register.md#execution-step-215).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s216"></a>
### S216 · Complete shared-world behavior

**Current outcome:** All existing part families use one persistent shared world for collisions, motors, constraints and effects. Qualify body-local ball sockets/hinges/sliders, declared travel, endpoint and ordered routed ropes including round-sheave tangents/arcs, multi-body/repeated attachments, continuous correction paths that discover new obstacles, bounded hollow geometry, sparse caches and exact rollback/replay. Stops, rope extension and finite-work commands share the world event clock without early braking or duplicate substep work. A cross-family machine must repeat Run/Reset; catalogue-scale dense islands, body broadphase, supported-device performance and every affected part's UI proof remain required.

[Complete current acceptance](../requirements.md#sequence-task-225) · [Status and technical predecessors](../work-register.md#execution-step-216).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s217"></a>
### S217 · Supported sliding and near-cancelling motion

**Current outcome:** Players can combine near-cancelling spins and supported sliding loads without a stalled Run. Qualify continuous slip bounds, nonlinear support and work/error limits within current f16 budgets; no obsolete CPU fixed-point or Newton algorithm is mandated.

[Complete current acceptance](../requirements.md#sequence-task-226) · [Status and technical predecessors](../work-register.md#execution-step-217).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s218"></a>
### S218 · Authored contact materials

**Current outcome:** Every body slot declares typed friction/restitution material, captured consistently for production and tests. Balls roll, slide and rebound according to the visible authored material, including lever/rope interactions; qualify slip boundaries and current work/error limits.

[Complete current acceptance](../requirements.md#sequence-task-227) · [Status and technical predecessors](../work-register.md#execution-step-218).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s219"></a>
### S219 · Friction transitions and integration energy

**Current outcome:** Qualify sliding-to-rolling reversals, stops before a step midpoint, multiple transitions, high-spin feature boundaries and exact replay. Pendulum energy stays within the approved complete-Run budget across supported simulation rates without clamps or tuned-away failures.

[Complete current acceptance](../requirements.md#sequence-task-228) · [Status and technical predecessors](../work-register.md#execution-step-219).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s220"></a>
### S220 · Continuous force trajectories

**Current outcome:** Continuous translation under force and angular momentum under torque participate in captured curved collision and joint sweeps. Contact/joint support and external loads share the accepted trajectory and work accounting; a one-off force kick is not a substitute.

[Complete current acceptance](../requirements.md#sequence-task-229) · [Status and technical predecessors](../work-register.md#execution-step-220).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s221"></a>
### S221 · Joint direction boundaries

**Current outcome:** Active ratchets and unilateral joint directions enforce continuous allowed motion under contact, external force and finite-work motors. Qualify direction changes, energy, exact lifecycle and shared-world execution.

[Complete current acceptance](../requirements.md#sequence-task-230) · [Status and technical predecessors](../work-register.md#execution-step-221).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s222"></a>
### S222 · Shared body and shape ownership

**Current outcome:** Reusable body-local shapes, captured poses, authoritative mass/inertia and dynamic state have one owner. Dynamic envelopes/proxies share their body; beams and moving actuators declare independent bodies where their physical motion requires it.

[Complete current acceptance](../requirements.md#sequence-task-231) · [Status and technical predecessors](../work-register.md#execution-step-222).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s223"></a>
### S223 · Extensible typed body slots

**Current outcome:** Parts declare typed extensible body identities and enum query/coordinate policies. Qualify independent gate/shutter geometry, rotated travel, controls and exact Reset with one authoritative dynamics/shape owner.

[Complete current acceptance](../requirements.md#sequence-task-232) · [Status and technical predecessors](../work-register.md#execution-step-223).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s224"></a>
### S224 · Explicit initial body dynamics

**Current outcome:** Every slot requires explicit motion type, mass, inertia and initial velocity. Balls, weights, plungers, beams and actuators bind persistent shared bodies, materials, constraints, effects, forces and stores; qualify catalogue contact and replay.

[Complete current acceptance](../requirements.md#sequence-task-233) · [Status and technical predecessors](../work-register.md#execution-step-224).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s225"></a>
### S225 · Owned joint binding

**Current outcome:** Typed owner/slot references bind persistent bodies to frame joints. Qualify lever pivot/limits, actual ball/beam hit and miss, rotated stops, exact replay and every declared constraint without a second stepping path.

[Complete current acceptance](../requirements.md#sequence-task-234) · [Status and technical predecessors](../work-register.md#execution-step-225).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s226"></a>
### S226 · Transactional joint lifecycle

**Current outcome:** Validated joint replacement and collision policy operate atomically at admitted boundaries. Scheduled impact-time detach/reattach must preserve motor budgets, reject conflicting changes and restore exactly. Latches can release/reconnect at the intended instant without duplicate impulses.

[Complete current acceptance](../requirements.md#sequence-task-235) · [Status and technical predecessors](../work-register.md#execution-step-226).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s227"></a>
### S227 · Transactional collider revisions

**Current outcome:** Typed collider declarations and revisions update atomically, invalidate only affected contacts and restore exact snapshots. Scheduled impact-time updates preserve shared state/constraints and reject incompatible changes before mutation.

[Complete current acceptance](../requirements.md#sequence-task-236) · [Status and technical predecessors](../work-register.md#execution-step-227).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s228"></a>
### S228 · Typed continuous force inputs

**Current outcome:** Components submit typed force/torque declarations to shared aggregation. Preserve finite source-work accounting and authoritative body state; no independent integration path remains.

[Complete current acceptance](../requirements.md#sequence-task-237) · [Status and technical predecessors](../work-register.md#execution-step-228).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s229"></a>
### S229 · Impact-time effects

**Current outcome:** Typed impulses and simultaneous contact delivery share world event time and effect-state rollback. Current component state/events use this transaction; no direct scene callback owns physical response.

[Complete current acceptance](../requirements.md#sequence-task-238) · [Status and technical predecessors](../work-register.md#execution-step-229).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s230"></a>
### S230 · Guided plunger latch and ratchet

**Current outcome:** A finite-mass plunger binds shared slider/latch/stop frames. The ratchet continuously prevents reverse motion; winding visibly retracts the loaded head, release spends stored charge once and a blocked stroke remains blocked until geometry permits it. Qualify rotated payloads, replay and finite energy.

[Complete current acceptance](../requirements.md#sequence-task-239) · [Status and technical predecessors](../work-register.md#execution-step-230).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s231"></a>
### S231 · Authored routed ropes

**Current outcome:** Canonical socket routes bind shared coupled constraints with collision. Qualify tension/mass ratios, slack/open controls, connected-load collision, exact replay and finite-radius sheaves. A constructed route visibly carries its load and Reset restores the exact construction.

[Complete current acceptance](../requirements.md#sequence-task-240) · [Status and technical predecessors](../work-register.md#execution-step-231).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s232"></a>
### S232 · Finite-radius conduit contact

**Current outcome:** Generic sweeps and shared bodies preserve finite-radius pipe/bend traversal and obstruction. Qualify affected gameplay and numerical boundary cases without a separate sphere response path.

[Complete current acceptance](../requirements.md#sequence-task-241) · [Status and technical predecessors](../work-register.md#execution-step-232).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s233"></a>
### S233 · Physical hollow conduits

**Current outcome:** A ball traverses a genuinely hollow tube/frustum/bend under shared-world gravity, momentum and contact, can jam against obstructions and remains visibly on its physical path. No filled convex substitute, teleportation or scripted constant-speed route is allowed.

[Complete current acceptance](../requirements.md#sequence-task-242) · [Status and technical predecessors](../work-register.md#execution-step-233).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s234"></a>
### S234 · Shared optical geometry

**Current outcome:** Optical tracing uses the generic owned collision/query geometry under WGSL authority: finite mirrors/apertures, nearest-hit occlusion, moving balls, wall oriented boxes and explicit opaque frames/mounts. No conflicting second collision world is allowed.

[Complete current acceptance](../requirements.md#sequence-task-243) · [Status and technical predecessors](../work-register.md#execution-step-234).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s235"></a>
### S235 · Finite load-aware mechanical work

**Current outcome:** Loaded pushers, lifts and launchers consume only available work and can stall honestly. Budget stores, actuation, passive loss, return/rearming and simultaneous branches without duplicating source energy; signed belt speed alone does not prove force limits, stalls or loaded lifting. Reject unsupported combinations explicitly.

[Complete current acceptance](../requirements.md#sequence-task-244) · [Status and technical predecessors](../work-register.md#execution-step-235).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s236"></a>
### S236 · Finite-radius rope consistency

**Current outcome:** Physical rope length, slack, wheel travel and artwork must agree with true groove tangents/arcs, including close-up Run/Reset, heavily rotated/multiple pulleys and near-axis approaches. Endpoint motion cannot create unexplained tension jumps.

[Complete current acceptance](../requirements.md#sequence-task-245) · [Status and technical predecessors](../work-register.md#execution-step-236).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s237"></a>
### S237 · Construction validation

**Current outcome:** Reject overlapping/invalid construction with a visible explanation before Run, using shared compound geometry including bends and hinges. Nearby valid placements start normally and always Reset; no old solver is used for validation.

[Complete current acceptance](../requirements.md#sequence-task-246) · [Status and technical predecessors](../work-register.md#execution-step-237).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s238"></a>
### S238 · Shared occlusion queries

**Current outcome:** Generic owned rays consistently govern optical, solar, sound and airflow occlusion. Every affected part requires positive/blocked controls and current UI proof; no separate per-shape trace authority remains.

[Complete current acceptance](../requirements.md#sequence-task-247) · [Status and technical predecessors](../work-register.md#execution-step-238).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s239"></a>
### S239 · Remove obsolete numerical paths

**Current outcome:** All current parts use the sole shared WGSL motion/response authority. Remove obsolete body-flight, hinge, rope, guided-motion, specialized sweep and isolated impulse paths and their current callers together, without compatibility adapters; prove affected playable behavior and lifecycle.

[Complete current acceptance](../requirements.md#sequence-task-248) · [Status and technical predecessors](../work-register.md#execution-step-239).

**Delivery owners:** [P0-007](../work-register.md#work-p0-007), [P0-009](../work-register.md#work-p0-009), [P0-012](../work-register.md#work-p0-012), [P0-030](../work-register.md#work-p0-030).

<a id="s485"></a>
### S485 · Audit retained scope: Track linear RGB power, beam width, finite range and bounded branch/bounce counts

Aggregate optical acceptance; see exact scope correction. **Source outcome:** Splitting, focusing and repeated reflections cannot create extra power. A player can see where light spreads or loses useful strength and why a receiver remains below threshold.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-485).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s485).

<a id="s487"></a>
### S487 · Audit retained scope: Reset restores transforms, shutter pose, emitter settings and electrical/charge/heat state; clear traced beams and recompute previews

Aggregate optical lifecycle acceptance; see exact scope correction. **Source outcome:** Reset clears heat/charge and restores every optical pose, shutter and emitter setting; the next Run reproduces the same beam routing and interlock behaviour.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-487).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s487).

<a id="s016"></a>
### S016 · Close IX-01 — Contact impulse

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-01 &#124; Contact impulse &#124; Shared geometry/material collision exchanges momentum; separated bodies receive no contact impulse. &#124; 1; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-016).

<a id="s017"></a>
### S017 · Close IX-02 — Sliding friction

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-02 &#124; Sliding friction &#124; Oppose relative tangential slip and debit mechanical work to declared heat/loss stores; a motionless unloaded contact supplies none. &#124; 5; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-017).

<a id="s018"></a>
### S018 · Close IX-03 — Joint constraint

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-03 &#124; Joint constraint &#124; Constrain declared degrees of freedom through shared force rows; release only through supported lifecycle state, not a named-part instruction. &#124; 9; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-018).

<a id="s019"></a>
### S019 · Close IX-04 — Tension transmission

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-04 &#124; Tension transmission &#124; Transmit admissible pull through routed rope/cable; slack cannot push or create work. &#124; 12; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-019).

<a id="s020"></a>
### S020 · Close IX-05 — Shaft torque transmission

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-05 &#124; Shaft torque transmission &#124; Transfer signed torque with finite input work and load; blocked loads cannot receive free rotation. &#124; 31; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-020).

<a id="s021"></a>
### S021 · Close IX-06 — Electrical power transfer

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-06 &#124; Electrical power transfer &#124; Allocate finite supplied work among loads/storage; disconnected or exhausted sources deliver none. &#124; 11; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-021).

<a id="s022"></a>
### S022 · Close IX-07 — Signal propagation

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-07 &#124; Signal propagation &#124; Transfer typed control state independently of energy; a true signal alone cannot power a load. &#124; 15 for a simple sensor signal; logic follows21. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-022).

<a id="s418"></a>
### S418 · Close IX-08 — Fluid advection

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-08 &#124; Fluid advection &#124; Move finite mass, species and enthalpy through supported geometry/ports; an empty source emits nothing. &#124; 61; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-418).

<a id="s419"></a>
### S419 · Close IX-09 — Pressure work

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-09 &#124; Pressure work &#124; Exchange pressure-displacement work with a generic actuator; opposed load or sealed return changes actual motion. &#124; 70; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-419).

<a id="s420"></a>
### S420 · Close IX-10 — Buoyancy

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-10 &#124; Buoyancy &#124; Use displacement and surrounding density; without a supporting fluid there is no buoyant force. &#124; 20 for gas buoyancy; liquid displacement follows67. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-420).

<a id="s023"></a>
### S023 · Close IX-11 — Aerodynamic drag

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-11 &#124; Aerodynamic drag &#124; Use relative flow, geometry and declared drag model; zero relative motion generates no drag. &#124; 20; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-023).

<a id="s530"></a>
### S530 · Close IX-12 — Acoustic propagation

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-12 &#124; Acoustic propagation &#124; Account for emitted signal energy, path, attenuation and supported occlusion; an obstructed control receives the predicted diminished field. &#124; 72; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-530).

<a id="s488"></a>
### S488 · Close IX-13 — Optical transport

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-13 &#124; Optical transport &#124; Trace finite power with geometry-based reflection/refraction and occlusion; focusing redistributes irradiance rather than increasing total energy. &#124; 51; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-488).

<a id="s489"></a>
### S489 · Close IX-14 — Optical absorption

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-14 &#124; Optical absorption &#124; Allocate only the absorbed fraction to material enthalpy; reflected/transmitted power cannot also become heat. &#124; 55; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-489).

<a id="s606"></a>
### S606 · Close IX-15 — Ionizing transport

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-15 &#124; Ionizing transport &#124; Apply supported typed particle/channel/material models and energy deposition; blocked transmission cannot activate a downstream detector. &#124; 101; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-606).

<a id="s607"></a>
### S607 · Close IX-16 — Radioactive decay

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-16 &#124; Radioactive decay &#124; Advance finite populations with declared emission/energy accounting; spent inventory cannot emit indefinitely. &#124; 123; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-607).

<a id="s490"></a>
### S490 · Close IX-17 — Temperature sensing

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-17 &#124; Temperature sensing &#124; Read a valid local thermal state with defined response and supplied outputs; observation creates no heat or actuator energy. &#124; 15; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-490).

<a id="s544"></a>
### S544 · Close IX-18 — Thermal conduction

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-18 &#124; Thermal conduction &#124; Transfer equal/opposite energy through geometry/contact conductance along the temperature difference; a missing contact removes that path. &#124; 14 for plate/contact heating; conductor routing follows16. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-544).

<a id="s545"></a>
### S545 · Close IX-19 — Thermal convection

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-19 &#124; Thermal convection &#124; Exchange heat between surface and fluid through the declared transfer model; an ambient fan is not an arbitrary cold source. &#124; 40; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-545).

<a id="s546"></a>
### S546 · Close IX-20 — Thermal radiation

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-20 &#124; Thermal radiation &#124; Exchange emitted/absorbed non-ionizing radiant heat with view/occlusion and finite reservoirs; distinguish this channel from nuclear dose. &#124; 55; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-546).

<a id="s547"></a>
### S547 · Close IX-21 — Solid melting

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-21 &#124; Solid melting &#124; Consume latent enthalpy as solid fraction and physical support decrease; inadequate energy leaves residual solid. &#124; 67; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-547).

<a id="s548"></a>
### S548 · Close IX-22 — Liquid freezing

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-22 &#124; Liquid freezing &#124; Remove sensible/latent energy to an explicit sink while solid fraction/geometry grow; no colder sink means no spontaneous cooling. &#124; 79; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-548).

<a id="s549"></a>
### S549 · Close IX-23 — Liquid evaporation

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-23 &#124; Liquid evaporation &#124; Surface mass transfer carries latent energy and responds to vapor conditions; dry/saturated controls cannot evaporate arbitrarily. &#124; 73; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-549).

<a id="s550"></a>
### S550 · Close IX-24 — Liquid boiling

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-24 &#124; Liquid boiling &#124; Pressure-dependent phase change consumes liquid mass and latent energy; an empty vessel cannot produce vapor. &#124; 70; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-550).

<a id="s551"></a>
### S551 · Close IX-25 — Vapor condensation

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-25 &#124; Vapor condensation &#124; Reject enthalpy to create conserved liquid; a warm or depleted sink limits production. &#124; 74; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-551).

<a id="s552"></a>
### S552 · Close IX-26 — Solid sublimation

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-26 &#124; Solid sublimation &#124; Use generic material phase data for direct solid-to-vapor transfer with finite mass/energy; unsupported materials/conditions do not transition. &#124; 88; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-552).

<a id="s553"></a>
### S553 · Close IX-27 — Vapor deposition

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-27 &#124; Vapor deposition &#124; Use generic phase data for direct vapor-to-solid growth with removed energy; a too-warm surface does not grow frost. &#124; 88; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-553).

<a id="s554"></a>
### S554 · Close IX-28 — Chemical reaction

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-28 &#124; Chemical reaction &#124; Consume declared reactants, form accounted products and transfer reaction energy through a validated material model; missing reactants limit the rate. &#124; 56; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-554).

<a id="s555"></a>
### S555 · Close IX-29 — Reaction ignition

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-29 &#124; Reaction ignition &#124; Initiate reaction from local state/rate under that model regardless of heating-source identity; subthreshold energy fails. &#124; 58; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-555).

<a id="s556"></a>
### S556 · Close IX-30 — Reaction extinction

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-30 &#124; Reaction extinction &#124; Cooling, reactant depletion and transport alter the reaction rate; no unconditional water-touch extinguish event. &#124; 60; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-556).

<a id="s557"></a>
### S557 · Close IX-31 — Thermal expansion

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-31 &#124; Thermal expansion &#124; Map material state to natural strain/volume and solve constraints; heated free and constrained bodies respond differently. &#124; 77; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-557).

<a id="s558"></a>
### S558 · Close IX-32 — Thermal stress

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-32 &#124; Thermal stress &#124; Resolve incompatible constrained strains as mechanical stress; uniform unrestrained heating is not automatic fracture. &#124; 82; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-558).

<a id="s559"></a>
### S559 · Close IX-33 — Heat pumping

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-33 &#124; Heat pumping &#124; Input work moves heat between generic thermal ports; rejected heat equals extracted heat plus work within the stated model. &#124; 78; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-559).

<a id="s560"></a>
### S560 · Close IX-34 — Thermoelectric conversion

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-34 &#124; Thermoelectric conversion &#124; Temperature difference, heat flow and load govern electrical work and rejected heat; equal-temperature ports produce none. &#124; 87; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-560).

<a id="s561"></a>
### S561 · Close IX-35 — Phase-change storage

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-35 &#124; Phase-change storage &#124; Charge/discharge material enthalpy through ordinary heat transport and phase evolution; a full store cannot absorb unlimited latent energy. &#124; 84; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-561).

<a id="s562"></a>
### S562 · Close IX-36 — Material coating

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-36 &#124; Material coating &#124; Conserved deposited substance changes the surface model only where applied; untreated regions retain their properties. &#124; 94; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-562).

<a id="s563"></a>
### S563 · Close IX-37 — Structural fracture

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-37 &#124; Structural fracture &#124; Use stress/work and material state to alter topology into bounded conserved fragments; failure cannot create mass/energy. &#124; 43; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-563).

<a id="s421"></a>
### S421 · Close IX-38 — Capillary transport

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-38 &#124; Capillary transport &#124; Material affinity, pore geometry and pressure move finite liquid with enthalpy; dry supply and saturation limit transport. &#124; 73; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-421).

<a id="s564"></a>
### S564 · Close IX-39 — Electrical dissipation

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-39 &#124; Electrical dissipation &#124; Debit real electrical work into material internal energy through a generic resistive-load model; missing supply cannot heat. &#124; 14; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-564).

<a id="s471"></a>
### S471 · Close IX-40 — Gas state evolution

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-40 &#124; Gas state evolution &#124; Use finite composition, internal energy and pressure-volume constitutive state; heating a vented volume does not mimic a sealed volume. &#124; 70 before the boiling/outlet objective; sealed expansion is later reuse77. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-471).

<a id="s565"></a>
### S565 · Close IX-41 — Phase topology update

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-41 &#124; Phase topology update &#124; Transfer ownership/geometry of solid/liquid/gas without duplication and update collision, ports, mass and inertia; partial transitions remain represented. &#124; 67; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-565).

<a id="s566"></a>
### S566 · Close IX-42 — Temperature-dependent strength

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-42 &#124; Temperature-dependent strength &#124; Evaluate material strength from thermodynamic state, then use generic stress/fracture/joint models; a warm but unloaded body need not break. &#124; 85; separately staged from other new processes. &#124; &#124; open requirement

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-566).

<a id="s491"></a>
### S491 · Close IX-43 — Sensible heat storage

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Model, native and UI fixture criteria pass, including IX-43 &#124; Sensible heat storage &#124; Relate enthalpy and temperature through material heat capacity outside phase transitions; energy and temperature are not interchangeable counters. &#124; 14; separately staged from other new processes.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-491).

<a id="law-granular-i"></a>
### LAW-GRANULAR-I · Implement Granular transport and sorting

Aggregate supporting capability; see exact scope correction. **Source outcome:** Finite inventory, real openings/contact, material compatibility, jam and bounded spawn; no identity-based passage. Shared capabilities only; native, atomic lifecycle and unsupported boundary cases pass.

[Complete criteria/status/technical predecessors](../work-register.md#work-law-granular-i).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#law-granular-i).

<a id="law-controller-i"></a>
### LAW-CONTROLLER-I · Implement Character and programmable controllers

Aggregate supporting capability; see exact scope correction. **Source outcome:** Typed finite-state controls express physical intent using finite supplies, bounded sensing and deterministic ticks. Shared capabilities only; native, atomic lifecycle and unsupported boundary cases pass.

[Complete criteria/status/technical predecessors](../work-register.md#work-law-controller-i).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#law-controller-i).

<a id="engine-ledger"></a>
### ENGINE-LEDGER · Implement typed finite inventory/energy transactions

Aggregate supporting invariants; see exact scope correction. **Source outcome:** Mass/species/enthalpy/work have units, nonnegative capacities, equal/opposite transfer and exact rollback. Ledger storage only; phase/reaction closure follows its laws.

[Complete criteria/status/technical predecessors](../work-register.md#work-engine-ledger).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#engine-ledger).

<a id="engine-topology"></a>
### ENGINE-TOPOLOGY · Implement atomic topology/material/handle transactions

Aggregate supporting invariants; see exact scope correction. **Source outcome:** Create/remove/rebind and fragment/phase ownership commit atomically; stale handles reject; bounded growth and save/Reset restore graph, material and geometry.

[Complete criteria/status/technical predecessors](../work-register.md#work-engine-topology).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#engine-topology).

<a id="engine-fixture-ui"></a>
### ENGINE-FIXTURE-UI · Provide normally placeable generic engine fixtures

Aggregate real-UI acceptance; see exact scope correction. **Source outcome:** Typed fixture palette exposes real bodies, ports, materials and supplied actuators through ordinary controls; no setters/imported solutions/numeric placement menu; invalid configurations reject.

[Complete criteria/status/technical predecessors](../work-register.md#work-engine-fixture-ui).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#engine-fixture-ui).

<a id="engine-effects"></a>
### ENGINE-EFFECTS · Implement shared difficulty-effect policies

Aggregate effects qualification; see exact scope correction. **Source outcome:** Inventory each effect under Forgiving/Balanced/Precise; separately review each shared effect; profile freezes at Run and is included in save/replay. No hidden force source.

[Complete criteria/status/technical predecessors](../work-register.md#work-engine-effects).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#engine-effects).

<a id="p0-009"></a>
### P0-009 · Audit current functional-consumer extraction

Aggregate engine criteria inside named consumers; parent closure requires all children. **Source outcome:** Every separately listed current consumer has completed portable declaration/behavior extraction; all functional callbacks, networks, timers and objectives have generic authority and headless rollback proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-009).


 **Exact closure members:** [CAT-001-I](../work-register.md#work-cat-001-i), [CAT-002-I](../work-register.md#work-cat-002-i), [CAT-003-I](../work-register.md#work-cat-003-i), [CAT-004-I](../work-register.md#work-cat-004-i), [CAT-005-I](../work-register.md#work-cat-005-i), [CAT-006-I](../work-register.md#work-cat-006-i), [CAT-007-I](../work-register.md#work-cat-007-i), [CAT-008-I](../work-register.md#work-cat-008-i), [CAT-009-I](../work-register.md#work-cat-009-i), [CAT-010-I](../work-register.md#work-cat-010-i), [CAT-011-I](../work-register.md#work-cat-011-i), [CAT-012-I](../work-register.md#work-cat-012-i), [CAT-013-I](../work-register.md#work-cat-013-i), [CAT-014-I](../work-register.md#work-cat-014-i), [CAT-015-I](../work-register.md#work-cat-015-i), [CAT-016-I](../work-register.md#work-cat-016-i), [CAT-017-I](../work-register.md#work-cat-017-i), [CAT-018-I](../work-register.md#work-cat-018-i), [CAT-019-I](../work-register.md#work-cat-019-i), [CAT-020-I](../work-register.md#work-cat-020-i), [CAT-021-I](../work-register.md#work-cat-021-i), [CAT-022-I](../work-register.md#work-cat-022-i), [CAT-023-I](../work-register.md#work-cat-023-i), [CAT-024-I](../work-register.md#work-cat-024-i), [CAT-025-I](../work-register.md#work-cat-025-i), [CAT-026-I](../work-register.md#work-cat-026-i), [CAT-027-I](../work-register.md#work-cat-027-i), [CAT-028-I](../work-register.md#work-cat-028-i), [CAT-029-I](../work-register.md#work-cat-029-i), [CAT-030-I](../work-register.md#work-cat-030-i), [CAT-031-I](../work-register.md#work-cat-031-i), [CAT-032-I](../work-register.md#work-cat-032-i), [CAT-033-I](../work-register.md#work-cat-033-i), [CAT-034-I](../work-register.md#work-cat-034-i), [CAT-035-I](../work-register.md#work-cat-035-i), [CAT-036-I](../work-register.md#work-cat-036-i), [CAT-037-I](../work-register.md#work-cat-037-i), [CAT-038-I](../work-register.md#work-cat-038-i), [CAT-039-I](../work-register.md#work-cat-039-i), [CAT-040-I](../work-register.md#work-cat-040-i), [CAT-041-I](../work-register.md#work-cat-041-i), [CAT-042-I](../work-register.md#work-cat-042-i), [CAT-043-I](../work-register.md#work-cat-043-i), [CAT-044-I](../work-register.md#work-cat-044-i), [CAT-045-I](../work-register.md#work-cat-045-i), [CAT-046-I](../work-register.md#work-cat-046-i), [CAT-047-I](../work-register.md#work-cat-047-i), [CAT-048-I](../work-register.md#work-cat-048-i), [CAT-049-I](../work-register.md#work-cat-049-i), [CAT-050-I](../work-register.md#work-cat-050-i), [CAT-051-I](../work-register.md#work-cat-051-i), [CAT-052-I](../work-register.md#work-cat-052-i), [CAT-053-I](../work-register.md#work-cat-053-i), [CAT-054-I](../work-register.md#work-cat-054-i), [CAT-055-I](../work-register.md#work-cat-055-i), [CAT-056-I](../work-register.md#work-cat-056-i), [CAT-057-I](../work-register.md#work-cat-057-i), [CAT-058-I](../work-register.md#work-cat-058-i), [CAT-059-I](../work-register.md#work-cat-059-i), [CAT-060-I](../work-register.md#work-cat-060-i), [CAT-061-I](../work-register.md#work-cat-061-i), [CAT-062-I](../work-register.md#work-cat-062-i), [CAT-063-I](../work-register.md#work-cat-063-i), [CAT-064-I](../work-register.md#work-cat-064-i), [CAT-065-I](../work-register.md#work-cat-065-i), [CAT-066-I](../work-register.md#work-cat-066-i), [CAT-067-I](../work-register.md#work-cat-067-i), [CAT-068-I](../work-register.md#work-cat-068-i), [CAT-069-I](../work-register.md#work-cat-069-i), [CAT-070-I](../work-register.md#work-cat-070-i), [CAT-071-I](../work-register.md#work-cat-071-i), [CAT-072-I](../work-register.md#work-cat-072-i), [P0-008](../work-register.md#work-p0-008). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="p0-010"></a>
### P0-010 · Audit all generic capability implementations

Aggregate engine criteria inside named consumers; parent closure requires all children. **Source outcome:** All IX and additional capability children have frozen models and passing independent numerical/rollback tests; every unshipped law has a named typed placeable qualification fixture.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-010).


 **Exact closure members:** [LAW-CONTROLLER-I](../work-register.md#work-law-controller-i), [LAW-ENVIRONMENT-I](../work-register.md#work-law-environment-i), [LAW-FIELD-I](../work-register.md#work-law-field-i), [LAW-GOALS-I](../work-register.md#work-law-goals-i), [LAW-GRANULAR-I](../work-register.md#work-law-granular-i), [P0-009](../work-register.md#work-p0-009), [S016-I](../work-register.md#work-s016-i), [S017-I](../work-register.md#work-s017-i), [S018-I](../work-register.md#work-s018-i), [S019-I](../work-register.md#work-s019-i), [S020-I](../work-register.md#work-s020-i), [S021-I](../work-register.md#work-s021-i), [S022-I](../work-register.md#work-s022-i), [S023-I](../work-register.md#work-s023-i), [S418-I](../work-register.md#work-s418-i), [S419-I](../work-register.md#work-s419-i), [S420-I](../work-register.md#work-s420-i), [S421-I](../work-register.md#work-s421-i), [S471-I](../work-register.md#work-s471-i), [S488-I](../work-register.md#work-s488-i), [S489-I](../work-register.md#work-s489-i), [S490-I](../work-register.md#work-s490-i), [S491-I](../work-register.md#work-s491-i), [S530-I](../work-register.md#work-s530-i), [S544-I](../work-register.md#work-s544-i), [S545-I](../work-register.md#work-s545-i), [S546-I](../work-register.md#work-s546-i), [S547-I](../work-register.md#work-s547-i), [S548-I](../work-register.md#work-s548-i), [S549-I](../work-register.md#work-s549-i), [S550-I](../work-register.md#work-s550-i), [S551-I](../work-register.md#work-s551-i), [S552-I](../work-register.md#work-s552-i), [S553-I](../work-register.md#work-s553-i), [S554-I](../work-register.md#work-s554-i), [S555-I](../work-register.md#work-s555-i), [S556-I](../work-register.md#work-s556-i), [S557-I](../work-register.md#work-s557-i), [S558-I](../work-register.md#work-s558-i), [S559-I](../work-register.md#work-s559-i), [S560-I](../work-register.md#work-s560-i), [S561-I](../work-register.md#work-s561-i), [S562-I](../work-register.md#work-s562-i), [S563-I](../work-register.md#work-s563-i), [S564-I](../work-register.md#work-s564-i), [S565-I](../work-register.md#work-s565-i), [S566-I](../work-register.md#work-s566-i), [S606-I](../work-register.md#work-s606-i), [S607-I](../work-register.md#work-s607-i). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="opt-scratch"></a>
### OPT-SCRATCH · Reuse hot-path scratch storage

Measured follow-up; accepted named consumer and attributable profile required. **Source outcome:** Warmed stable-topology scratch allocation is zero for declared buffers; malformed/capacity boundaries reject and exact physical/rollback results stay equal. Reconcile current timer/animation scratch arrays first; optimize remaining measured allocation owners only, without a framework rewrite.

[Complete criteria/status/technical predecessors](../work-register.md#work-opt-scratch).

<a id="opt-checkpoint"></a>
### OPT-CHECKPOINT · Reuse rollback and publication checkpoints

Measured follow-up; accepted named consumer and attributable profile required. **Source outcome:** No live pooled aliases or partial rollback; delayed leases and topology invalidation are safe; measure allocation and bytes per tick. Reuse SimulationTransaction participant/checkpoint ownership; enumerate all affected values, identity maps, free lists and pending structural/event state. Prove failure at each changed phase restores the entire prior committed state, not only body poses.

[Complete criteria/status/technical predecessors](../work-register.md#work-opt-checkpoint).

<a id="opt-predictor"></a>
### OPT-PREDICTOR · Remove repeated predictor/solver work

Measured follow-up; accepted named consumer and attributable profile required. **Source outcome:** Qualify exact reused-state invalidation and analytical/contact controls; measured solve/candidate counts and CPU service improve beyond noise without changing tolerances.

[Complete criteria/status/technical predecessors](../work-register.md#work-opt-predictor).

<a id="opt-sleep"></a>
### OPT-SLEEP · Qualify sparse scheduling and island sleep

Measured follow-up; accepted named consumer and attributable profile required. **Source outcome:** Only proven inactive dependency closure sleeps; wake on commands/contact/store/material changes; full-enabled replay stays equal and unused-domain cost is measured.

[Complete criteria/status/technical predecessors](../work-register.md#work-opt-sleep).

<a id="clean-core"></a>
### CLEAN-CORE · Remove superseded physics authority and declaration paths

Supporting enforcement or closure gate; not a product story. **Source outcome:** Delete old solver dispatch/callback/world ownership and redundant geometry/state representations; refactor every production/test/fixture/content caller to the new contracts. Search proves zero old references or dual authority; unsupported old inputs reject.

[Complete criteria/status/technical predecessors](../work-register.md#work-clean-core).


 **Exact closure members:** [P0-019](../work-register.md#work-p0-019). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="clean-animation"></a>
### CLEAN-ANIMATION · Remove superseded animation scheduling

Supporting enforcement or closure gate; not a product story. **Source outcome:** Delete part-local cosmetic loops, old bridge phase/state and renderer-triggered evaluation; one animation-worker owner remains, one browser property application stage. Disabled animation cannot change authority.

[Complete criteria/status/technical predecessors](../work-register.md#work-clean-animation).


 **Exact closure members:** [P0-024](../work-register.md#work-p0-024), [P0-026](../work-register.md#work-p0-026). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="clean-protocol"></a>
### CLEAN-PROTOCOL · Remove obsolete protocols and serialization

Supporting enforcement or closure gate; not a product story. **Source outcome:** Delete old schema readers/writers, aliases, deprecated discriminants, automatic migrations, default substitution and version-selection branches. Update current authored content, saves/fixtures and tools together; old schema inputs reject atomically.

[Complete criteria/status/technical predecessors](../work-register.md#work-clean-protocol).


 **Exact closure members:** [P0-020](../work-register.md#work-p0-020), [P0-021](../work-register.md#work-p0-021). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="clean-build"></a>
### CLEAN-BUILD · Remove obsolete host/build/deployment artifacts

Supporting enforcement or closure gate; not a product story. **Source outcome:** Delete retired host paths, packages, build targets, exported bundles and cache references from maintained source/deployment. Clean publish proves only new required artifacts are reachable; stale assets fail rather than loading a fallback.

[Complete criteria/status/technical predecessors](../work-register.md#work-clean-build).


 **Exact closure members:** [P0-029](../work-register.md#work-p0-029). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="clean-context"></a>
### CLEAN-CONTEXT · Reconcile active documentation and agent context

Supporting enforcement or closure gate; not a product story. **Source outcome:** Update README/AGENTS/design/build/test/runbook references to one current system. Move dated logs/context to clearly marked evidence references, remove superseded next-action instructions, preserve all task scope and immutable historical artifacts. No archived record is supported current input.

[Complete criteria/status/technical predecessors](../work-register.md#work-clean-context).


 **Exact closure members:** [CLEAN-ANIMATION](../work-register.md#work-clean-animation), [CLEAN-BUILD](../work-register.md#work-clean-build), [CLEAN-CORE](../work-register.md#work-clean-core), [CLEAN-PROTOCOL](../work-register.md#work-clean-protocol). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="law-field-v"></a>
### LAW-FIELD-V · Qualify Magnetic/electric field response

Aggregate supporting invariant/fixture qualification; not a standalone feature. **Source outcome:** Finite supplied field work/material force/torque; zero/disconnected source and substitutable responders. Each variant passes actual-UI positive/control, typed state, Reset/save and timed replay.

[Complete criteria/status/technical predecessors](../work-register.md#work-law-field-v).

<a id="law-granular-v"></a>
### LAW-GRANULAR-V · Qualify Granular transport and sorting

Aggregate supporting invariant/fixture qualification; not a standalone feature. **Source outcome:** Finite inventory, real openings/contact, material compatibility, jam and bounded spawn; no identity-based passage. Each variant passes actual-UI positive/control, typed state, Reset/save and timed replay.

[Complete criteria/status/technical predecessors](../work-register.md#work-law-granular-v).

<a id="law-controller-v"></a>
### LAW-CONTROLLER-V · Qualify Character and programmable controllers

Aggregate supporting invariant/fixture qualification; not a standalone feature. **Source outcome:** Typed finite-state controls express physical intent using finite supplies, bounded sensing and deterministic ticks. Each variant passes actual-UI positive/control, typed state, Reset/save and timed replay.

[Complete criteria/status/technical predecessors](../work-register.md#work-law-controller-v).

<a id="law-goals-v"></a>
### LAW-GOALS-V · Qualify Quantity, ordered, rate-window and protected-state goals

Aggregate supporting invariant/fixture qualification; not a standalone feature. **Source outcome:** Authoritative occurrence ticks/identities, supported variants, duplicate rejection and exact boundary traces. Each variant passes actual-UI positive/control, typed state, Reset/save and timed replay.

[Complete criteria/status/technical predecessors](../work-register.md#work-law-goals-v).

<a id="law-environment-v"></a>
### LAW-ENVIRONMENT-V · Qualify Authored environment and simulation profiles

Aggregate supporting invariant/fixture qualification; not a standalone feature. **Source outcome:** Typed gravity/atmosphere/material/profile ranges; immutable per-Run state and current-schema replay. Each variant passes actual-UI positive/control, typed state, Reset/save and timed replay.

[Complete criteria/status/technical predecessors](../work-register.md#work-law-environment-v).

<a id="clean-enforce"></a>
### CLEAN-ENFORCE · Prove forward-only architecture and publish cleanup evidence

Supporting enforcement or closure gate; not a product story. **Source outcome:** Negative architecture/boundary fixtures inject old API usage, dual authority, schema aliases, fallback and old-name mappings; checker rejects all. Zero obsolete active callers/docs/assets; compile all current callers, clean Release, UI/Reset/save and deployment smoke pass.

[Complete criteria/status/technical predecessors](../work-register.md#work-clean-enforce).


 **Exact closure members:** [CLEAN-CONTEXT](../work-register.md#work-clean-context). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="p0-030"></a>
### P0-030 · Audit every current catalogue consumer and authored fixture

Aggregate engine criteria inside named consumers; parent closure requires all children. **Source outcome:** Each named catalogue/mode and fixture has current real-UI construction, exact typed links, positive/control/integration, motion and Run/Reset/save proof from its own scheduled row.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-030).


 **Exact closure members:** [P0-029](../work-register.md#work-p0-029), [S001](../work-register.md#execution-step-001), [S002](../work-register.md#execution-step-002), [S003](../work-register.md#execution-step-003), [S004](../work-register.md#execution-step-004), [S005](../work-register.md#execution-step-005), [S006](../work-register.md#execution-step-006), [S007](../work-register.md#execution-step-007), [S008](../work-register.md#execution-step-008), [S009](../work-register.md#execution-step-009), [S010](../work-register.md#execution-step-010), [S011](../work-register.md#execution-step-011), [S012](../work-register.md#execution-step-012), [S013](../work-register.md#execution-step-013), [S014](../work-register.md#execution-step-014), [S015](../work-register.md#execution-step-015), [S024](../work-register.md#execution-step-024), [S025](../work-register.md#execution-step-025), [S026](../work-register.md#execution-step-026), [S027](../work-register.md#execution-step-027), [S028](../work-register.md#execution-step-028), [S029](../work-register.md#execution-step-029), [S030](../work-register.md#execution-step-030), [S031](../work-register.md#execution-step-031), [S032](../work-register.md#execution-step-032), [S033](../work-register.md#execution-step-033), [S034](../work-register.md#execution-step-034), [S035](../work-register.md#execution-step-035), [S036](../work-register.md#execution-step-036), [S037](../work-register.md#execution-step-037), [S038](../work-register.md#execution-step-038), [S039](../work-register.md#execution-step-039), [S040](../work-register.md#execution-step-040), [S041](../work-register.md#execution-step-041), [S042](../work-register.md#execution-step-042), [S043](../work-register.md#execution-step-043), [S044](../work-register.md#execution-step-044), [S045](../work-register.md#execution-step-045), [S046](../work-register.md#execution-step-046), [S047](../work-register.md#execution-step-047), [S048](../work-register.md#execution-step-048), [S049](../work-register.md#execution-step-049), [S050](../work-register.md#execution-step-050), [S051](../work-register.md#execution-step-051), [S052](../work-register.md#execution-step-052), [S053](../work-register.md#execution-step-053), [S054](../work-register.md#execution-step-054), [S055](../work-register.md#execution-step-055), [S056](../work-register.md#execution-step-056), [S057](../work-register.md#execution-step-057), [S058](../work-register.md#execution-step-058), [S059](../work-register.md#execution-step-059), [S060](../work-register.md#execution-step-060), [S061](../work-register.md#execution-step-061), [S062](../work-register.md#execution-step-062), [S063](../work-register.md#execution-step-063), [S064](../work-register.md#execution-step-064), [S065](../work-register.md#execution-step-065), [S066](../work-register.md#execution-step-066), [S067](../work-register.md#execution-step-067), [S068](../work-register.md#execution-step-068), [S069](../work-register.md#execution-step-069), [S070](../work-register.md#execution-step-070), [S071](../work-register.md#execution-step-071), [S072](../work-register.md#execution-step-072), [S073](../work-register.md#execution-step-073), [S074](../work-register.md#execution-step-074), [S075](../work-register.md#execution-step-075), [S076](../work-register.md#execution-step-076), [S077](../work-register.md#execution-step-077), [S078](../work-register.md#execution-step-078), [S079](../work-register.md#execution-step-079), [S080](../work-register.md#execution-step-080), [S081](../work-register.md#execution-step-081), [S082](../work-register.md#execution-step-082), [S083](../work-register.md#execution-step-083), [S084](../work-register.md#execution-step-084), [S085](../work-register.md#execution-step-085), [S086](../work-register.md#execution-step-086), [S087](../work-register.md#execution-step-087), [S088](../work-register.md#execution-step-088), [S089](../work-register.md#execution-step-089), [S090](../work-register.md#execution-step-090), [S091](../work-register.md#execution-step-091), [S092](../work-register.md#execution-step-092), [S093](../work-register.md#execution-step-093), [S094](../work-register.md#execution-step-094), [S095](../work-register.md#execution-step-095), [S096](../work-register.md#execution-step-096), [S097](../work-register.md#execution-step-097), [S098](../work-register.md#execution-step-098), [S099](../work-register.md#execution-step-099), [S100](../work-register.md#execution-step-100), [S101](../work-register.md#execution-step-101), [S102](../work-register.md#execution-step-102), [S103](../work-register.md#execution-step-103), [S104](../work-register.md#execution-step-104), [S105](../work-register.md#execution-step-105), [S106](../work-register.md#execution-step-106), [S107](../work-register.md#execution-step-107), [S108](../work-register.md#execution-step-108), [S109](../work-register.md#execution-step-109), [S110](../work-register.md#execution-step-110), [S111](../work-register.md#execution-step-111), [S112](../work-register.md#execution-step-112), [S113](../work-register.md#execution-step-113), [S114](../work-register.md#execution-step-114), [S115](../work-register.md#execution-step-115), [S116](../work-register.md#execution-step-116), [S117](../work-register.md#execution-step-117). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="p0-031"></a>
### P0-031 · Audit every generic engine qualification fixture

Aggregate engine criteria inside named consumers; parent closure requires all children. **Source outcome:** Every required law has real-UI positive/control, substitutable-capability, conservation and lifecycle proof; qualification fixtures exercise actual authority and never fabricate success.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-031).


 **Exact closure members:** [LAW-CONTROLLER-V](../work-register.md#work-law-controller-v), [LAW-ENVIRONMENT-V](../work-register.md#work-law-environment-v), [LAW-FIELD-V](../work-register.md#work-law-field-v), [LAW-GOALS-V](../work-register.md#work-law-goals-v), [LAW-GRANULAR-V](../work-register.md#work-law-granular-v), [P0-010](../work-register.md#work-p0-010), [P0-029](../work-register.md#work-p0-029), [S016](../work-register.md#execution-step-016), [S017](../work-register.md#execution-step-017), [S018](../work-register.md#execution-step-018), [S019](../work-register.md#execution-step-019), [S020](../work-register.md#execution-step-020), [S021](../work-register.md#execution-step-021), [S022](../work-register.md#execution-step-022), [S023](../work-register.md#execution-step-023), [S418](../work-register.md#execution-step-418), [S419](../work-register.md#execution-step-419), [S420](../work-register.md#execution-step-420), [S421](../work-register.md#execution-step-421), [S471](../work-register.md#execution-step-471), [S488](../work-register.md#execution-step-488), [S489](../work-register.md#execution-step-489), [S490](../work-register.md#execution-step-490), [S491](../work-register.md#execution-step-491), [S530](../work-register.md#execution-step-530), [S544](../work-register.md#execution-step-544), [S545](../work-register.md#execution-step-545), [S546](../work-register.md#execution-step-546), [S547](../work-register.md#execution-step-547), [S548](../work-register.md#execution-step-548), [S549](../work-register.md#execution-step-549), [S550](../work-register.md#execution-step-550), [S551](../work-register.md#execution-step-551), [S552](../work-register.md#execution-step-552), [S553](../work-register.md#execution-step-553), [S554](../work-register.md#execution-step-554), [S555](../work-register.md#execution-step-555), [S556](../work-register.md#execution-step-556), [S557](../work-register.md#execution-step-557), [S558](../work-register.md#execution-step-558), [S559](../work-register.md#execution-step-559), [S560](../work-register.md#execution-step-560), [S561](../work-register.md#execution-step-561), [S562](../work-register.md#execution-step-562), [S563](../work-register.md#execution-step-563), [S564](../work-register.md#execution-step-564), [S565](../work-register.md#execution-step-565), [S566](../work-register.md#execution-step-566), [S606](../work-register.md#execution-step-606), [S607](../work-register.md#execution-step-607). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="engine-device-01"></a>
### ENGINE-DEVICE-01 · Qualify worker/render runtime on physical Android Chrome

Aggregate supporting invariant/fixture qualification; not a standalone feature. **Source outcome:** Current catalogue/generic fixtures meet pinned startup, worker, input, audio/visibility, memory and sustained budgets on actual hardware. Emulation cannot pass; Chrome/Playwright stays mandatory project proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-engine-device-01).

<a id="engine-device-02"></a>
### ENGINE-DEVICE-02 · Qualify worker/render runtime on physical iOS Safari

Aggregate supporting invariant/fixture qualification; not a standalone feature. **Source outcome:** Current catalogue/generic fixtures meet pinned startup, worker, input, audio/visibility, memory and sustained budgets on actual hardware. Emulation cannot pass; Chrome/Playwright stays mandatory project proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-engine-device-02).

<a id="engine-device-03"></a>
### ENGINE-DEVICE-03 · Qualify worker/render runtime on integrated-GPU desktop

Aggregate supporting invariant/fixture qualification; not a standalone feature. **Source outcome:** Current catalogue/generic fixtures meet pinned startup, worker, input, audio/visibility, memory and sustained budgets on actual hardware. Emulation cannot pass; Chrome/Playwright stays mandatory project proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-engine-device-03).

<a id="p0-035"></a>
### P0-035 · Publish the mandatory engine-first gate

Aggregate engine criteria inside named consumers; parent closure requires all children. **Source outcome:** All prior P0 work, generic laws, current consumers and fixture proofs pass on current artifacts; old synchronous paths removed; production deploy and committed/pushed revision verified. Only this gate unlocks product work.

[Complete criteria/status/technical predecessors](../work-register.md#work-p0-035).


 **Exact closure members:** [CAT-001-D](../work-register.md#work-cat-001-d), [CAT-002-D](../work-register.md#work-cat-002-d), [CAT-003-D](../work-register.md#work-cat-003-d), [CAT-004-D](../work-register.md#work-cat-004-d), [CAT-005-D](../work-register.md#work-cat-005-d), [CAT-006-D](../work-register.md#work-cat-006-d), [CAT-007-D](../work-register.md#work-cat-007-d), [CAT-008-D](../work-register.md#work-cat-008-d), [CAT-009-D](../work-register.md#work-cat-009-d), [CAT-010-D](../work-register.md#work-cat-010-d), [CAT-011-D](../work-register.md#work-cat-011-d), [CHECK-AGGREGATE](../work-register.md#work-check-aggregate), [CHECK-GRAPH](../work-register.md#work-check-graph), [CHECK-METRICS](../work-register.md#work-check-metrics), [CHECK-SCHEMA](../work-register.md#work-check-schema), [P0-001](../work-register.md#work-p0-001), [P0-002](../work-register.md#work-p0-002), [P0-003](../work-register.md#work-p0-003), [P0-004](../work-register.md#work-p0-004), [P0-005](../work-register.md#work-p0-005), [P0-006](../work-register.md#work-p0-006), [P0-007](../work-register.md#work-p0-007), [P0-008](../work-register.md#work-p0-008), [S001-D](../work-register.md#work-s001-d), [S002-D](../work-register.md#work-s002-d), [S003-D](../work-register.md#work-s003-d), [S004-D](../work-register.md#work-s004-d), [S005-D](../work-register.md#work-s005-d), [S006-D](../work-register.md#work-s006-d), [S007-D](../work-register.md#work-s007-d), [S008-D](../work-register.md#work-s008-d), [S009-D](../work-register.md#work-s009-d), [S010-D](../work-register.md#work-s010-d), [S011-D](../work-register.md#work-s011-d), [S012-D](../work-register.md#work-s012-d), [S013-D](../work-register.md#work-s013-d), [S014-D](../work-register.md#work-s014-d), [S015-D](../work-register.md#work-s015-d), [S024-D](../work-register.md#work-s024-d), [S025-D](../work-register.md#work-s025-d), [S026-D](../work-register.md#work-s026-d), [S027-D](../work-register.md#work-s027-d), [S028-D](../work-register.md#work-s028-d), [S029-D](../work-register.md#work-s029-d), [S030-D](../work-register.md#work-s030-d), [S031-D](../work-register.md#work-s031-d), [S032-D](../work-register.md#work-s032-d), [S033-D](../work-register.md#work-s033-d), [S034-D](../work-register.md#work-s034-d), [S035-D](../work-register.md#work-s035-d), [S036-D](../work-register.md#work-s036-d), [S037-D](../work-register.md#work-s037-d), [S038-D](../work-register.md#work-s038-d), [S039-D](../work-register.md#work-s039-d), [S040-D](../work-register.md#work-s040-d), [S041-D](../work-register.md#work-s041-d), [S042-D](../work-register.md#work-s042-d), [S043-D](../work-register.md#work-s043-d), [S044-D](../work-register.md#work-s044-d), [S045-D](../work-register.md#work-s045-d), [S046-D](../work-register.md#work-s046-d), [S047-D](../work-register.md#work-s047-d), [S048-D](../work-register.md#work-s048-d), [S049-D](../work-register.md#work-s049-d), [S050-D](../work-register.md#work-s050-d), [S051-D](../work-register.md#work-s051-d), [S052-D](../work-register.md#work-s052-d), [S053-D](../work-register.md#work-s053-d), [S054-D](../work-register.md#work-s054-d), [S055-D](../work-register.md#work-s055-d), [S056-D](../work-register.md#work-s056-d), [S057-D](../work-register.md#work-s057-d), [S058-D](../work-register.md#work-s058-d), [S059-D](../work-register.md#work-s059-d), [S060-D](../work-register.md#work-s060-d), [S061-D](../work-register.md#work-s061-d), [S062-D](../work-register.md#work-s062-d), [S063-D](../work-register.md#work-s063-d), [S064-D](../work-register.md#work-s064-d), [S065-D](../work-register.md#work-s065-d), [S066-D](../work-register.md#work-s066-d), [S067-D](../work-register.md#work-s067-d), [S068-D](../work-register.md#work-s068-d), [S069-D](../work-register.md#work-s069-d), [S070-D](../work-register.md#work-s070-d), [S071-D](../work-register.md#work-s071-d), [S072-D](../work-register.md#work-s072-d), [S073-D](../work-register.md#work-s073-d), [S074-D](../work-register.md#work-s074-d), [S075-D](../work-register.md#work-s075-d), [S076-D](../work-register.md#work-s076-d), [S077-D](../work-register.md#work-s077-d), [S078-D](../work-register.md#work-s078-d), [S079-D](../work-register.md#work-s079-d), [S080-D](../work-register.md#work-s080-d), [S081-D](../work-register.md#work-s081-d), [S082-D](../work-register.md#work-s082-d), [S083-D](../work-register.md#work-s083-d), [S084-D](../work-register.md#work-s084-d), [S085-D](../work-register.md#work-s085-d), [S086-D](../work-register.md#work-s086-d), [S087-D](../work-register.md#work-s087-d), [S088-D](../work-register.md#work-s088-d), [S089-D](../work-register.md#work-s089-d), [S090-D](../work-register.md#work-s090-d), [S091-D](../work-register.md#work-s091-d), [S092-D](../work-register.md#work-s092-d), [S093-D](../work-register.md#work-s093-d), [S094-D](../work-register.md#work-s094-d), [S095-D](../work-register.md#work-s095-d), [S096-D](../work-register.md#work-s096-d), [S097-D](../work-register.md#work-s097-d), [S098-D](../work-register.md#work-s098-d), [S099-D](../work-register.md#work-s099-d), [S100-D](../work-register.md#work-s100-d), [S101-D](../work-register.md#work-s101-d), [S102-D](../work-register.md#work-s102-d), [S103-D](../work-register.md#work-s103-d), [S104-D](../work-register.md#work-s104-d), [S105-D](../work-register.md#work-s105-d), [S106-D](../work-register.md#work-s106-d), [S107-D](../work-register.md#work-s107-d), [S108-D](../work-register.md#work-s108-d), [S109-D](../work-register.md#work-s109-d), [S110-D](../work-register.md#work-s110-d), [S111-D](../work-register.md#work-s111-d), [S112-D](../work-register.md#work-s112-d), [S113-D](../work-register.md#work-s113-d). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s258"></a>
### S258 · Review and deliver: Automatic snap-to-grid placement — add a clearly discoverable grid-placement mode, enabled by default, so selecting a position for a new element or moving an existing element snaps its placement anchor to the n

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Players align new and moved elements automatically on desktop and touch, including elevated placements, without free-form drift. Real-UI Playwright checks cover cell boundaries/negative coordinates, different part sizes/rotations and anchors, duplicate placement, invalid overlap/out-of-bounds placement, compatible and conflicting port joins, toggle behavior and cancellation/Undo/Redo. Verify the actual snapped transforms and typed connections, preserve settings and construction through save/load and Run/Reset, and keep grid assistance separate from difficulty nudging. Implement closed-set placement modes/presets as enums end-to-end with validated boundaries.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-258).

<a id="s259"></a>
### S259 · Review and deliver: Investigate the retained `L28-balanced-reference-joined-v1` unexpected Run event during construction

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Replaying the retained level-28 construction never starts Run before the player's Run action; the original unexpected-event record remains a failed attempt.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-259).

<a id="s260"></a>
### S260 · Review and deliver: Investigate an intermittent missed Reset click after the Precise larger-error solar timeout

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** After a timed-out puzzle, one deliberate Reset action restores the scene and its controls; reproduce and explain the missed click instead of relying on a second click.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-260).

<a id="s261"></a>
### S261 · Review and deliver: Diagnose and fix intermittent palette placement interruptions observed in levels 6 and 7 (`Timed out: placed part`)

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Selecting and placing parts in levels 6 and 7 works without a fresh-page retry, including near palette scroll boundaries; failed gestures leave an inspectable construction state.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-261).

<a id="s262"></a>
### S262 · Review and deliver: Investigate intermittent UI-only level-selector navigation: the first final-goal level-16 attempt timed out before Run; its fresh retry won

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Selecting the intended level always opens that level and leaves it ready to build; the original level-16 pre-Run timeout has a focused regression.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-262).

<a id="s263"></a>
### S263 · Review and deliver: Investigate selector timing in retained `colour-red-match-v1/v2`: both timed out before construction

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Colour-matching lessons can be opened and constructed using ordinary input timing; the retained v1/v2 startup failures have a reproduced cause or remain explicitly unresolved.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-263).

<a id="s264"></a>
### S264 · Review and deliver: Investigate retained wind-chimes-relay-v1 fan drag: fan ended at Y=0.5274403 instead of 5.4, leaving the machine idle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Dragging the fan preserves the intended axis/height and wiring connects the chosen ports without moving the battery. Both v1 and v3 failures are covered independently.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-264).

<a id="s265"></a>
### S265 · Review and deliver: Continue investigating earlier missed drags/wires and selector timing; the selection-threshold fix does not prove all causes resolved

Audit/reconciliation aggregate; see exact scope correction. **Source outcome:** A selection tap does not become an unintended drag, a drag does not become wiring, and cancelling a gesture restores the prior construction on mouse and touch.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-265).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s265).

<a id="s266"></a>
### S266 · Review and deliver: Check next-puzzle navigation immediately after success; avoid requiring Reset or showing misleading instructions

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** After success, a clear Next action opens the next puzzle immediately without Reset; the last puzzle has an explicit finished state instead of a dead or misleading Next instruction.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-266).

<a id="s267"></a>
### S267 · Review and deliver: Audit campaign loading, selection/navigation, current-schema save handling, hints, tests and Playwright tooling for hard-coded 40-level assumptions; derive limits from campaign data where possible

Audit/reconciliation aggregate; see exact scope correction. **Source outcome:** Campaign selection, loading, hints and navigation reach every authored level and stop at the actual end of the data, including the future 150th level.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-267).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s267).

<a id="s268"></a>
### S268 · Review and deliver: Verify current-schema browser Save/Load and motor motion over time; the free-workshop record reaches the diagnostic 30-second timeout, not a puzzle win

Aggregate of two distinct visible closures; see exact scope correction. **Source outcome:** Save, reload the page and Load reconstruct a motor-driven machine with the same links, transforms and settings; subsequent power loss, Run and Reset behave consistently.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-268).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s268).

<a id="s292"></a>
### S292 · Review and deliver: Recheck current physical and animated consumers before catalogue expansion

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-15 — refresh every affected part's behavioural and visual proof. Use actual construction/connection controls to verify placed configuration, typed links, intended behaviour, meaningful negative/control cases and integration for each affected part/mode, including existing catalogue parts and fixtures. Prove exact Run/Reset… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-292).


 **Exact closure members:** [P0-009](../work-register.md#work-p0-009), [P0-023](../work-register.md#work-p0-023), [P0-030](../work-register.md#work-p0-030). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s293"></a>
### S293 · Review and deliver: EL-190 · Ramp

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Wrong-facing slope cannot accelerate cargo uphill. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-293). **Distinct child outcomes:** [element-190](named-elements.md#element-190).

<a id="s294"></a>
### S294 · Review and deliver: EL-187 · Basketball

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Equal drop height produces reproducible material-dependent rebound. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-294). **Distinct child outcomes:** [element-187](named-elements.md#element-187).

<a id="s295"></a>
### S295 · Review and deliver: EL-188 · Bowling ball

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Its visual size alone cannot supply added work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-295). **Distinct child outcomes:** [element-188](named-elements.md#element-188).

<a id="s296"></a>
### S296 · Review and deliver: EL-192 · Receiving basket

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A ball beside or passing over it does not satisfy capture. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-296). **Distinct child outcomes:** [element-192](named-elements.md#element-192).

<a id="s297"></a>
### S297 · Review and deliver: EL-189 · Tennis ball

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Its bounce differs from the bowling ball because of declared material, not IDs. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-297). **Distinct child outcomes:** [element-189](named-elements.md#element-189).

<a id="s298"></a>
### S298 · Review and deliver: EL-191 · Wall

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A geometric gap allows passage; invisible silhouette bounds do not block it. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-298). **Distinct child outcomes:** [element-191](named-elements.md#element-191).

<a id="s299"></a>
### S299 · Review and deliver: EL-193 · Domino

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Separated neighbour does not fall from a scripted propagation event. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-299). **Distinct child outcomes:** [element-193](named-elements.md#element-193).

<a id="s300"></a>
### S300 · Review and deliver: EL-194 · Springboard

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Uncharged or missed contact cannot impart a free launch. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-300). **Distinct child outcomes:** [element-194](named-elements.md#element-194).

<a id="s301"></a>
### S301 · Review and deliver: EL-195 · Pinball bumper

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No available energy or missed contact means no powered kick. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-301). **Distinct child outcomes:** [element-195](named-elements.md#element-195).

<a id="s302"></a>
### S302 · Review and deliver: EL-196 · Battery

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected or depleted source cannot operate a motor. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-302). **Distinct child outcomes:** [element-196](named-elements.md#element-196).

<a id="s303"></a>
### S303 · Review and deliver: EL-197 · Electrical wire

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Geometric crossing alone cannot connect wires. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-303). **Distinct child outcomes:** [element-197](named-elements.md#element-197).

<a id="s304"></a>
### S304 · Review and deliver: EL-198 · Switch

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Closed contact cannot create supply energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-304). **Distinct child outcomes:** [element-198](named-elements.md#element-198).

<a id="s305"></a>
### S305 · Review and deliver: EL-199 · Electric motor

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Stalled shaft consumes only supported work and cannot bypass load limits. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-305). **Distinct child outcomes:** [element-199](named-elements.md#element-199).

<a id="s306"></a>
### S306 · Review and deliver: EL-204 · Weight

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Raising it requires actual work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-306). **Distinct child outcomes:** [element-204](named-elements.md#element-204).

<a id="s307"></a>
### S307 · Review and deliver: EL-205 · Rope

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Slack rope cannot push or pull until tension develops. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-307). **Distinct child outcomes:** [element-205](named-elements.md#element-205).

<a id="s308"></a>
### S308 · Review and deliver: EL-206 · Fixed pulley

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected or misrouted rope receives no invisible coupling. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-308). **Distinct child outcomes:** [element-206](named-elements.md#element-206).

<a id="s309"></a>
### S309 · Review and deliver: EL-207 · Moving pulley

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Its displacement/work ratio follows routing rather than a named pulley bonus. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-309). **Distinct child outcomes:** [element-207](named-elements.md#element-207).

<a id="s310"></a>
### S310 · Review and deliver: EL-200 · Drive belt

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing tension or disconnected endpoints prevent full drive transfer. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-310). **Distinct child outcomes:** [element-200](named-elements.md#element-200).

<a id="s311"></a>
### S311 · Review and deliver: EL-202 · Conveyor

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unpowered or noncontact cargo is not carried along a hidden path. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-311). **Distinct child outcomes:** [element-202](named-elements.md#element-202).

<a id="s312"></a>
### S312 · Review and deliver: EL-203 · Reverse transmission

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** It cannot reverse by duplicating input work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-312). **Distinct child outcomes:** [element-203](named-elements.md#element-203).

<a id="s313"></a>
### S313 · Review and deliver: EL-209 · Fan

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Blocked flow or absent supply prevents remote force. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-313). **Distinct child outcomes:** [element-209](named-elements.md#element-209).

<a id="s316"></a>
### S316 · Review and deliver: P1 Passive trampoline / elastic membrane — C# finite spring/damper membrane, rigid rim/back, actual mesh deflection, catalog and original icon implemented. Tension changes physical compression/contact time; no

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 Passive trampoline / elastic membrane &#124; C# finite spring/damper membrane, rigid rim/back, actual mesh deflection, catalog and original icon implemented. Tension changes physical compression/contact time; no imposed launch velocity. 56 focused native cases pass (1,193 full suite), including the new introductory campaign… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-316).

<a id="s317"></a>
### S317 · Review and deliver: P1 Teeter-totter / impact lever — implementation started — Initial C# catalogue scene, original icon and continuous shared-clock sphere/beam contact are implemented. Native aligned/missed/pivot impact controls,

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 Teeter-totter / impact lever — implementation started &#124; Initial C# catalogue scene, original icon and continuous shared-clock sphere/beam contact are implemented. Native aligned/missed/pivot impact controls, energy bounds, ten-second equal-load balance, four 3D orientations and Reset/replay pass. A retained supported-contact… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-317).

<a id="s318"></a>
### S318 · Review and deliver: P1 — Teeter-totter: pivoting lever with rope attachments

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 &#124; Teeter-totter: pivoting lever with rope attachments &#124; Add a hinge constraint, end sockets and visible angular limits. Teach counterweights, then ball-to-rope triggering. Test off-centre loads, stalls and Reset. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-318).

<a id="s319"></a>
### S319 · Review and deliver: P1 Electric linear pusher — C# part/scene/icon, moving head/rod, force-limited cargo contact and typed source/target UI choices implemented and focused proof passed. 28 pusher cases plus 2 presentation cases pa

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 Electric linear pusher &#124; C# part/scene/icon, moving head/rod, force-limited cargo contact and typed source/target UI choices implemented and focused proof passed. 28 pusher cases plus 2 presentation cases pass (883 native total); diagnostic/production publishes and 63 driver tests pass. Five current-build real-UI cases prove… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-319).

<a id="s320"></a>
### S320 · Review and deliver: Electrically controlled clutch: C# implementation, scene/catalog/icon and 18 native cases pass (756 total; 48 UI-driver tests)

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Electrically controlled clutch: C# implementation, scene/catalog/icon and 18 native cases pass (756 total; 48 UI-driver tests). Five real-UI Playwright cases prove powered drive, missing coil power, missing motor power, timed release and reversed drive, with verified construction/typed links and exact Reset; production Release… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-320).

<a id="s321"></a>
### S321 · Review and deliver: P1 — Windmill: air to rotation

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 &#124; Windmill: air to rotation &#124; C# part, catalog, icon and signed shaft implemented and locally verified; 22 new native cases (709 total), UI connected/disconnected/blocked controls and exact Reset. See verification. Fan → rotor → belt campaign lesson, torque/load physics and sustained/mobile visual review remain open. &#124; &#124; open requirement Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-321).

<a id="s322"></a>
### S322 · Review and deliver: P1 — Bellows: impact-operated air burst

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 &#124; Bellows: impact-operated air burst &#124; C# part/catalog/icon implemented: moving plate and folds, finite impact-dependent stroke, held-load suppression and silent refill. 26 new cases (735 native total), UI impact/missed controls and exact Reset pass; see verification. Campaign lessons, extended UI refill/blocked-path checks… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-322).

<a id="s326"></a>
### S326 · Review and deliver: EL-112 · Structural beam

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unsupported member falls rather than floating at authored endpoints. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-326). **Distinct child outcomes:** [element-112](named-elements.md#element-112).

<a id="s327"></a>
### S327 · Review and deliver: EL-113 · Structural brace

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Removing the brace changes stability; no frame-name bonus. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-327). **Distinct child outcomes:** [element-113](named-elements.md#element-113).

<a id="s328"></a>
### S328 · Review and deliver: EL-114 · Placeable pivot

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Off-axis load is solved by the shared joint; no animation path. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-328). **Distinct child outcomes:** [element-114](named-elements.md#element-114).

<a id="s329"></a>
### S329 · Review and deliver: EL-115 · Linkage connector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected endpoint cannot transmit motion. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-329). **Distinct child outcomes:** [element-115](named-elements.md#element-115).

<a id="s330"></a>
### S330 · Review and deliver: EL-116 · Passive wheel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No hidden drive on level ground. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-330). **Distinct child outcomes:** [element-116](named-elements.md#element-116).

<a id="s331"></a>
### S331 · Review and deliver: EL-117 · Axle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Locked axle prevents the formerly permitted rotation. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-331). **Distinct child outcomes:** [element-117](named-elements.md#element-117).

<a id="s332"></a>
### S332 · Review and deliver: EL-064 · Metal loop anchor

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected rope carries no anchored tension. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-332). **Distinct child outcomes:** [element-064](named-elements.md#element-064).

<a id="s333"></a>
### S333 · Review and deliver: EL-065 · Load hook

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unengaged hook cannot carry a remote body. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-333). **Distinct child outcomes:** [element-065](named-elements.md#element-065).

<a id="s334"></a>
### S334 · Review and deliver: EL-063 · Cable winch

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Stall and reverse load debit work; no instantaneous rope shortening. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-334). **Distinct child outcomes:** [element-063](named-elements.md#element-063).

<a id="s335"></a>
### S335 · Review and deliver: EL-066 · Scissors

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Wrong placement or insufficient cutting work leaves it intact. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-335). **Distinct child outcomes:** [element-066](named-elements.md#element-066).

<a id="s336"></a>
### S336 · Review and deliver: EL-067 · Steel cable

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Compression cannot push a load. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-336). **Distinct child outcomes:** [element-067](named-elements.md#element-067).

<a id="s337"></a>
### S337 · Review and deliver: EL-110 · Flywheel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Stored energy falls as output work is delivered. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-337). **Distinct child outcomes:** [element-110](named-elements.md#element-110).

<a id="s338"></a>
### S338 · Review and deliver: EL-111 · Centrifugal governor

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Stationary shaft does not actuate it. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-338). **Distinct child outcomes:** [element-111](named-elements.md#element-111).

<a id="s339"></a>
### S339 · Review and deliver: EL-053 · Rotary-to-linear converter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Blocked output loads the input; no command-only displacement. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-339). **Distinct child outcomes:** [element-053](named-elements.md#element-053).

<a id="s340"></a>
### S340 · Review and deliver: EL-054 · Linear-to-rotary converter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An unmoving input supplies no rotation energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-340). **Distinct child outcomes:** [element-054](named-elements.md#element-054).

<a id="s341"></a>
### S341 · Review and deliver: EL-055 · Mechanical brake

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Released brake permits motion; restraint cannot generate shaft work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-341). **Distinct child outcomes:** [element-055](named-elements.md#element-055).

<a id="s342"></a>
### S342 · Review and deliver: EL-056 · Ratchet

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Reverse load holds within capacity without inventing forward movement. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-342). **Distinct child outcomes:** [element-056](named-elements.md#element-056).

<a id="s343"></a>
### S343 · Review and deliver: EL-057 · Escapement

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No stored drive means no step despite a release trigger. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-343). **Distinct child outcomes:** [element-057](named-elements.md#element-057).

<a id="s344"></a>
### S344 · Review and deliver: EL-156 · Single-lobe cam

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Partial rotation yields only its actual profile displacement. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-344). **Distinct child outcomes:** [element-156](named-elements.md#element-156).

<a id="s345"></a>
### S345 · Review and deliver: EL-157 · Double-lobe cam

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** One revolution cannot be recorded as a single-lobe cycle. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-345). **Distinct child outcomes:** [element-157](named-elements.md#element-157).

<a id="s346"></a>
### S346 · Review and deliver: EL-158 · Rise-hold-fall cam

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Changing shaft speed changes dwell time; no hidden timer. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-346). **Distinct child outcomes:** [element-158](named-elements.md#element-158).

<a id="s347"></a>
### S347 · Review and deliver: P1 — Gears: coupled rotation; adjacent gears reverse direction

Design decision before gear implementation; see exact scope correction. **Source outcome:** P1 &#124; Gears: coupled rotation; adjacent gears reverse direction &#124; Add rotational ports and visible meshing. Begin with a direction-reversal puzzle; prevent contradictory drive loops from creating energy. Ratios need original-game verification. &#124; &#124; open requirement Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-347).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s347).

<a id="s348"></a>
### S348 · Review and deliver: P1 — Generator: rotation to electricity

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 &#124; Generator: rotation to electricity &#124; Couple a driven shaft to electrical output. Verify source loss stops dependent devices; teach motor/generator conversion without perpetual-power loops. &#124; &#124; open requirement Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-348).

<a id="s349"></a>
### S349 · Review and deliver: P1 — Mouse motor: impact-started rotational drive

Design decision before mouse-motor implementation; see exact scope correction. **Source outcome:** P1 &#124; Mouse motor: impact-started rotational drive &#124; Add a distinct mechanical source, not a disguised electrical switch. Animate the wheel; author run duration and retrigger policy. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-349).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s349).

<a id="s350"></a>
### S350 · Review and deliver: P1 Crank-slider — expand crank adapter — Signed shaft input produces periodic linear strokes through visible fixed-length rod and offset crank pin. Authored radius, rod length and phase; preserve phase when sto

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 Crank-slider — expand crank adapter &#124; Signed shaft input produces periodic linear strokes through visible fixed-length rod and offset crank pin. Authored radius, rod length and phase; preserve phase when stopped. &#124; Motor → belt → crank feeds balls while clock meters arrivals. Test full revolution, reverse rotation, invalid… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-350).

<a id="s351"></a>
### S351 · Review and deliver: P1 Rack-and-pinion slide — Signed shaft drives finite toothed rail with attachment carriage and end stops. Teeth/witness marks explain translation. Explicitly distinguish prescribed-speed prototype from load-aw

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 Rack-and-pinion slide &#124; Signed shaft drives finite toothed rail with attachment carriage and end stops. Teeth/witness marks explain translation. Explicitly distinguish prescribed-speed prototype from load-aware final behaviour. &#124; Solar → motor → rack positions mirror → receiver controls supplied gate. Test travel per turn,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-351).

<a id="s352"></a>
### S352 · Review and deliver: P2 Docking lift — expand lift entry — Guided supplied tray, upper/lower dock outputs and loading/unloading interlocks; explicit holding brake. Cargo stays physically supported through travel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Docking lift — expand lift entry &#124; Guided supplied tray, upper/lower dock outputs and loading/unloading interlocks; explicit holding brake. Cargo stays physically supported through travel. &#124; Ball waiting AND lower dock → inlet; upper dock → exit gate → gravity tube. Test edge cargo, acceleration, blocked travel, power loss… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-352).

<a id="s353"></a>
### S353 · Review and deliver: P2 Parallel gripper — Supplied opposing jaws, bounded aperture, close command and object-held output. Capture requires actual two-sided contact; define supported shapes. This variant releases on power loss

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Parallel gripper &#124; Supplied opposing jaws, bounded aperture, close command and object-held output. Capture requires actual two-sided contact; define supported shapes. This variant releases on power loss. &#124; Lift docks → grasp non-ferrous ball → rack carries → limit/hold timer → drop into funnel. Test empty/oversize/off-centre… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-353).

<a id="s355"></a>
### S355 · Review and deliver: Forward-refactor the self-contained fan to explicit external electrical/mechanical supply, updating scenes, authored levels, generator, callers and tests together

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An unconnected fan remains inactive; connecting a real electrical/mechanical source makes it blow and removing that source stops its driven effect. All current authored fan puzzles teach/provide the necessary supply.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-355).

<a id="s357"></a>
### S357 · Review and deliver: EL-068 · Tin snips

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Insufficient work fails on resistant material; no cable-name special case. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-357). **Distinct child outcomes:** [element-068](named-elements.md#element-068).

<a id="s358"></a>
### S358 · Review and deliver: EL-201 · Drive chain

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Incompatible attachment fails rather than inheriting belt behaviour. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-358). **Distinct child outcomes:** [element-201](named-elements.md#element-201).

<a id="s360"></a>
### S360 · Review and deliver: EL-071 · Straight metal ball pipe

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Oversized cargo jams rather than traversing a logical connection. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-360). **Distinct child outcomes:** [element-071](named-elements.md#element-071).

<a id="s361"></a>
### S361 · Review and deliver: EL-072 · Curved metal ball pipe

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Fast and queued cargo remain conserved at seams. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-361). **Distinct child outcomes:** [element-072](named-elements.md#element-072).

<a id="s362"></a>
### S362 · Review and deliver: EL-102 · Large-bore ball pipe

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Too-large cargo remains blocked; size is validated rather than guessed from art. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-362). **Distinct child outcomes:** [element-102](named-elements.md#element-102).

<a id="s363"></a>
### S363 · Review and deliver: EL-103 · Accelerator tube

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unpowered tube cannot increase cargo energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-363). **Distinct child outcomes:** [element-103](named-elements.md#element-103).

<a id="s364"></a>
### S364 · Review and deliver: EL-185 · Releasable assembly joint

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unreleased attachment retains load; unsupported command is rejected. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-364). **Distinct child outcomes:** [element-185](named-elements.md#element-185).

<a id="s365"></a>
### S365 · Review and deliver: EL-186 · Temporary bridge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A release signal cannot move unrelated structures. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-365). **Distinct child outcomes:** [element-186](named-elements.md#element-186).

<a id="s366"></a>
### S366 · Review and deliver: EL-184 · Mechanical ball gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Slack linkage or blocked blade prevents instantaneous release. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-366). **Distinct child outcomes:** [element-184](named-elements.md#element-184).

<a id="s367"></a>
### S367 · Review and deliver: EL-058 · Size grate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Oversized object remains blocked; identity labels do not select passage. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-367). **Distinct child outcomes:** [element-058](named-elements.md#element-058).

<a id="s368"></a>
### S368 · Review and deliver: EL-059 · Weight tray

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An object beside the tray cannot contribute weight. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-368). **Distinct child outcomes:** [element-059](named-elements.md#element-059).

<a id="s369"></a>
### S369 · Review and deliver: EL-069 · Moving bucket

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Tipping spills through the real opening; a missed object is not captured. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-369). **Distinct child outcomes:** [element-069](named-elements.md#element-069).

<a id="s370"></a>
### S370 · Review and deliver: EL-070 · Moving cage

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An open door allows escape through actual geometry. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-370). **Distinct child outcomes:** [element-070](named-elements.md#element-070).

<a id="s371"></a>
### S371 · Review and deliver: EL-061 · Indexed carousel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Obstruction or missing power prevents the completed index. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-371). **Distinct child outcomes:** [element-061](named-elements.md#element-061).

<a id="s372"></a>
### S372 · Review and deliver: EL-062 · Docking ferry

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Undocked loading interlock does not bypass actual support. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-372). **Distinct child outcomes:** [element-062](named-elements.md#element-062).

<a id="s373"></a>
### S373 · Review and deliver: EL-162 · Fixed ball diverter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Wrong branch remains unavailable without physical leakage. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-373). **Distinct child outcomes:** [element-162](named-elements.md#element-162).

<a id="s374"></a>
### S374 · Review and deliver: EL-163 · Powered ball diverter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing supply or a jam prevents instant route switching. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-374). **Distinct child outcomes:** [element-163](named-elements.md#element-163).

<a id="s375"></a>
### S375 · Review and deliver: EL-164 · Alternating ball diverter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Held arrival signal cannot alternate repeatedly without new passages. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-375). **Distinct child outcomes:** [element-164](named-elements.md#element-164).

<a id="s376"></a>
### S376 · Review and deliver: EL-109 · Speed-sensitive trapdoor

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Equal-size slow control stays on its declared route. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-376). **Distinct child outcomes:** [element-109](named-elements.md#element-109).

<a id="s377"></a>
### S377 · Review and deliver: EL-104 · Toy firework

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Spent charge does not retrigger; no real pyrotechnic instructions. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-377). **Distinct child outcomes:** [element-104](named-elements.md#element-104).

<a id="s378"></a>
### S378 · Review and deliver: EL-105 · Toy missile

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No supply or lost guidance cannot yield unlimited corrective thrust. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-378). **Distinct child outcomes:** [element-105](named-elements.md#element-105).

<a id="s379"></a>
### S379 · Review and deliver: EL-106 · Impact-sensitive toy charge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Subthreshold contact leaves it unspent; historical nitroglycerine is a reference only. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-379). **Distinct child outcomes:** [element-106](named-elements.md#element-106).

<a id="s380"></a>
### S380 · Review and deliver: EL-107 · Spiral gravity-delay tube

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Changing speed changes transit time; no precise hidden timer. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-380). **Distinct child outcomes:** [element-107](named-elements.md#element-107).

<a id="s381"></a>
### S381 · Review and deliver: EL-215 · Damped cushion

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Grazing or bottomed-out impact does not guarantee arrest; deformation/heat loss balances work. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 1–10; practice41–50; reuse96,149 as separate objectives in existing slots.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-381). **Distinct child outcomes:** [element-215](named-elements.md#element-215).

<a id="s382"></a>
### S382 · Review and deliver: EL-216 · Capture cradle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Oversize or energetic cargo can escape; no invisible capture radius or forced attachment. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 1–10; practice41–50; reuse96,149 as separate objectives in existing slots.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-382). **Distinct child outcomes:** [element-216](named-elements.md#element-216).

<a id="s388"></a>
### S388 · Review and deliver: Next: hopper with a one-ball escapement, visible queue and one release per trigger

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A hopper stores a visible queue and releases exactly one ball per accepted trigger; held input, an empty hopper or a blocked outlet cannot dump or duplicate the queue.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-388).

<a id="s390"></a>
### S390 · Review and deliver: P2 Reload shuttle with chamber interlock — expand feeder — Supplied sliding pocket advances one physical queued ball only when chamber is empty and launcher has returned. Show queue, occupied pocket and jam; ne

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Reload shuttle with chamber interlock — expand feeder &#124; Supplied sliding pocket advances one physical queued ball only when chamber is empty and launcher has returned. Show queue, occupied pocket and jam; never delete overflow. &#124; Conveyor → hopper → shuttle → cannon; shot counter stops after three deliveries. Test two-ball… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-390).

<a id="s391"></a>
### S391 · Review and deliver: P2 — Trap door separates objects in a TIM2 puzzle. Easy walkthrough(https://sierrachest.com/index.php?a=games&fld=walkthrough&id=229&pid=101)

Design decision before trap-door implementation; see exact scope correction. **Source outcome:** P2 &#124; Trap door separates objects in a TIM2 puzzle. Easy walkthrough &#124; Add a hinged/selective passage. Verify its selection rule rather than guessing “size filter”; test thresholds, heavy/light bodies and moving collision geometry. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-391).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s391).

<a id="s392"></a>
### S392 · Review and deliver: P2 Supplied pinball flipper — promote existing candidate — Battery supplies bounded pivoted paddle; independent signal commands strike/hold, spring returns it. Distinct from radial bumper and linear boxing glov

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Supplied pinball flipper — promote existing candidate &#124; Battery supplies bounded pivoted paddle; independent signal commands strike/hold, spring returns it. Distinct from radial bumper and linear boxing glove. &#124; Beam receiver → timer → paddle releases or strikes a waiting ball into funnel. Test missing supply, power loss… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-392).

<a id="s393"></a>
### S393 · Review and deliver: P2 — Boxing glove: triggered punch

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 &#124; Boxing glove: triggered punch &#124; Add a directional actuator with explicit contact face and cooldown. Teach timing a lateral impulse rather than continuous force. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-393).

<a id="s394"></a>
### S394 · Review and deliver: P2 Passive impact scoop — Arriving ball's momentum rotates a shallow hinged cup and carries the same ball upward before release. No raised counterweight or automatic boost; distinct from the counterweighted lau

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Passive impact scoop &#124; Arriving ball's momentum rotates a shallow hinged cup and carries the same ball upward before release. No raised counterweight or automatic boost; distinct from the counterweighted launcher above. &#124; Downhill run-up → scoop → elevated pipe → cushion. Test minimum approach energy,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-394).

<a id="s395"></a>
### S395 · Review and deliver: P2 Counterweighted scoop catapult — Raised physical weight drives a loaded hinged cup; same ball leaves on a free trajectory. Rope/winch must raise the weight again. Show falling weight, swept arm and spent sta

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Counterweighted scoop catapult &#124; Raised physical weight drives a loaded hinged cup; same ball leaves on a free trajectory. Rope/winch must raise the weight again. Show falling weight, swept arm and spent state. &#124; Water-filled bucket → armed cup → upper pipe. Test inadequate counterweight, missed cup, premature release,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-395).

<a id="s396"></a>
### S396 · Review and deliver: P2 Latched slingshot cradle — Rope/winch draws an elastic cradle; separate latch releases its seated ball. No reflex aiming during Run. Visible band extension and return; bounded authored charge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Latched slingshot cradle &#124; Rope/winch draws an elastic cradle; separate latch releases its seated ball. No reflex aiming during Run. Visible band extension and return; bounded authored charge. &#124; Motor/winch charges; sound meter releases through a timed gate. Test zero charge, rope cut, draw limit, oversized load, empty… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-396).

<a id="s397"></a>
### S397 · Review and deliver: P2 Hinged elastic catapult — Distinct rotary delivery from the linear wound launcher: charged elastic arm swings a cup before releasing. Share finite-charge infrastructure, not an identical impulse with another

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Hinged elastic catapult &#124; Distinct rotary delivery from the linear wound launcher: charged elastic arm swings a cup before releasing. Share finite-charge infrastructure, not an identical impulse with another mesh. &#124; Belt winding → delayed latch → throw into moving basket. Test unwound release, blocked sweep, maximum charge,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-397).

<a id="s398"></a>
### S398 · Review and deliver: P2 — Jack-in-the-box: belt-wound launcher

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 &#124; Jack-in-the-box: belt-wound launcher &#124; Accumulate mechanical input before release. Show winding, lid opening and launch; verify no free launch without a drive connection. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-398).

<a id="s399"></a>
### S399 · Review and deliver: P2 Magnetic impulse relay — Compatible arriving ball is retained while a different staged ball departs. Explicit finite stored energy and physical rearming; distinct from electromagnet pickup. Visible input/out

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Magnetic impulse relay &#124; Compatible arriving ball is retained while a different staged ball departs. Explicit finite stored energy and physical rearming; distinct from electromagnet pickup. Visible input/output occupancy and spent state. &#124; Outgoing ball climbs pipe to switch; retained ball weights a tray. Test wrong… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-399).

<a id="s400"></a>
### S400 · Review and deliver: P2 Zipline signal carriage — expand docking transport — Incoming ball releases a gravity-driven cable carriage that strikes a separate destination ball; it does not teleport or carry the initiating ball. Winch

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Zipline signal carriage — expand docking transport &#124; Incoming ball releases a gravity-driven cable carriage that strikes a separate destination ball; it does not teleport or carry the initiating ball. Winch returns it. &#124; Ball → carriage across ravine → destination ball → bell. Test flat/uphill stall, obstructions,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-400).

<a id="s402"></a>
### S402 · Review and deliver: EL-181 · Rising-edge detector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Held true input produces no repeated transitions. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-402). **Distinct child outcomes:** [element-181](named-elements.md#element-181).

<a id="s403"></a>
### S403 · Review and deliver: EL-182 · Falling-edge detector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Held false input produces no repeated transitions. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-403). **Distinct child outcomes:** [element-182](named-elements.md#element-182).

<a id="s404"></a>
### S404 · Review and deliver: EL-183 · Resettable counter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Reset clears the count without manufacturing a new arrival. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-404). **Distinct child outcomes:** [element-183](named-elements.md#element-183).

<a id="s405"></a>
### S405 · Review and deliver: EL-133 · Electrical AND gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-405). **Distinct child outcomes:** [element-133](named-elements.md#element-133).

<a id="s406"></a>
### S406 · Review and deliver: EL-134 · Electrical OR gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-406). **Distinct child outcomes:** [element-134](named-elements.md#element-134).

<a id="s407"></a>
### S407 · Review and deliver: EL-135 · Electrical XOR gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-407). **Distinct child outcomes:** [element-135](named-elements.md#element-135).

<a id="s408"></a>
### S408 · Review and deliver: EL-136 · Electrical NOR gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-408). **Distinct child outcomes:** [element-136](named-elements.md#element-136).

<a id="s409"></a>
### S409 · Review and deliver: EL-137 · Electrical NAND gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent supply; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-409). **Distinct child outcomes:** [element-137](named-elements.md#element-137).

<a id="s411"></a>
### S411 · Review and deliver: Expand delay verification with repeated placement-error trials and broader timing puzzles

Aggregate retained timer acceptance; see exact scope correction. **Source outcome:** Expand delay verification with repeated placement-error trials and broader timing puzzles. The initial module is one-shot per Run; player duration adjustment, rearming delay pulses, clocks and resettable memory/logic remain unfinished. Hold timers, counters and switched electrical contacts are separate implemented modules. Do… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-411).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s411).

<a id="s412"></a>
### S412 · Review and deliver: First: delay box — wait, then trigger

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Complete the adjustable wait-then-trigger contract using the existing delay implementation: a brief input still yields one delayed output, busy retriggers do not restart it and the countdown is readable. Do not re-add the already completed one-shot prototype as a separate part.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-412).

<a id="s413"></a>
### S413 · Review and deliver: P2 — Egg timer participates in delayed triggering. TIM2 walkthrough(https://sierrachest.com/index.php?a=games&fld=walkthrough&id=229&pid=102)

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 &#124; Egg timer participates in delayed triggering. TIM2 walkthrough &#124; Add a readable countdown actuator. Test zero delay, pause/Reset and ordered goals; keep author configuration separate from difficulty assistance. &#124; &#124; Scope index &#124; Individual element specifications: EL-075, TH-34. &#124; Retained research reference: Contemporary… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-413).

<a id="s415"></a>
### S415 · Review and deliver: Run the mechanical/control performance and concurrency checkpoint

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-02 — add typed instrumentation and reproducible scenarios. Use enum-typed stages, metrics and scenarios, typed part/body/revision IDs, and validated serialization boundaries in C# diagnostics/Playtest tooling. Capture substeps/events, simulated versus wall time, candidate/tree/leaf counts, solver iterations, largest… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-415).


 **Exact closure members:** [P0-032](../work-register.md#work-p0-032), [P0-034](../work-register.md#work-p0-034). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s422"></a>
### S422 · Review and deliver: EL-001 · Finite reservoir

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Outlet flow depletes inventory; an empty tank supplies none. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-422). **Distinct child outcomes:** [element-001](named-elements.md#element-001).

<a id="s423"></a>
### S423 · Review and deliver: EL-002 · Header tank

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Lowering it reduces available lift; cycling cannot create energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-423). **Distinct child outcomes:** [element-002](named-elements.md#element-002).

<a id="s424"></a>
### S424 · Review and deliver: EL-003 · Tap

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Closing stops routed flow; an unconnected tap emits nothing. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-424). **Distinct child outcomes:** [element-003](named-elements.md#element-003).

<a id="s425"></a>
### S425 · Review and deliver: EL-004 · Catch basin

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A missed stream remains outside; overflow remains conserved. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-425). **Distinct child outcomes:** [element-004](named-elements.md#element-004).

<a id="s426"></a>
### S426 · Review and deliver: EL-005 · Liquid funnel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Blocked outlet fills and spills; no remote capture. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-426). **Distinct child outcomes:** [element-005](named-elements.md#element-005).

<a id="s427"></a>
### S427 · Review and deliver: EL-006 · Drain

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Only liquid crossing its intake enters the tracked sink. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-427). **Distinct child outcomes:** [element-006](named-elements.md#element-006).

<a id="s428"></a>
### S428 · Review and deliver: EL-007 · Open gutter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Adverse slope stalls or spills; no hidden uphill transport. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-428). **Distinct child outcomes:** [element-007](named-elements.md#element-007).

<a id="s429"></a>
### S429 · Review and deliver: EL-008 · Straight water pipe

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A disconnected mouth cannot feed its neighbour. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-429). **Distinct child outcomes:** [element-008](named-elements.md#element-008).

<a id="s430"></a>
### S430 · Review and deliver: EL-009 · 45-degree water elbow

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Wrong-facing or incompatible joins remain disconnected. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-430). **Distinct child outcomes:** [element-009](named-elements.md#element-009).

<a id="s431"></a>
### S431 · Review and deliver: EL-010 · 90-degree water elbow

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Blocking its outlet prevents through-flow. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-431). **Distinct child outcomes:** [element-010](named-elements.md#element-010).

<a id="s432"></a>
### S432 · Review and deliver: EL-011 · Water T junction

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Branch totals cannot exceed supply. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-432). **Distinct child outcomes:** [element-011](named-elements.md#element-011).

<a id="s433"></a>
### S433 · Review and deliver: EL-012 · Water pipe cap

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Capped flow stops; deleting the cap restores a real opening. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-433). **Distinct child outcomes:** [element-012](named-elements.md#element-012).

<a id="s434"></a>
### S434 · Review and deliver: EL-013 · Water nozzle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Insufficient head cannot produce the rated reach. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-434). **Distinct child outcomes:** [element-013](named-elements.md#element-013).

<a id="s435"></a>
### S435 · Review and deliver: EL-014 · Water-carrying bucket

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Filled load changes rope balance; empty control does not. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-435). **Distinct child outcomes:** [element-014](named-elements.md#element-014).

<a id="s436"></a>
### S436 · Review and deliver: EL-015 · Leaky bucket

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Caught leakage equals lost inventory; empty bucket stops leaking. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-436). **Distinct child outcomes:** [element-015](named-elements.md#element-015).

<a id="s437"></a>
### S437 · Review and deliver: EL-016 · Float

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Overloaded float sinks; an empty basin provides no buoyant lift. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-437). **Distinct child outcomes:** [element-016](named-elements.md#element-016).

<a id="s438"></a>
### S438 · Review and deliver: EL-017 · Mechanical float valve

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Immobilised linkage prevents closure even at high water. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-438). **Distinct child outcomes:** [element-017](named-elements.md#element-017).

<a id="s439"></a>
### S439 · Review and deliver: EL-018 · Electronic level switch

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Dry and unpowered controls cannot switch the output. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-439). **Distinct child outcomes:** [element-018](named-elements.md#element-018).

<a id="s440"></a>
### S440 · Review and deliver: EL-019 · Water check valve

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Reverse head cannot flow through a closed seat. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-440). **Distinct child outcomes:** [element-019](named-elements.md#element-019).

<a id="s441"></a>
### S441 · Review and deliver: EL-020 · Water diverter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unselected route receives only explicitly modelled leakage. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-441). **Distinct child outcomes:** [element-020](named-elements.md#element-020).

<a id="s442"></a>
### S442 · Review and deliver: EL-021 · Sluice gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Closed gate retains upstream volume without deleting inflow. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-442). **Distinct child outcomes:** [element-021](named-elements.md#element-021).

<a id="s443"></a>
### S443 · Review and deliver: EL-022 · Water pump

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unpowered pump cannot sustain uphill flow. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-443). **Distinct child outcomes:** [element-022](named-elements.md#element-022).

<a id="s444"></a>
### S444 · Review and deliver: EL-023 · Archimedes screw

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Wrong rotation or a dry intake cannot deliver the target volume. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-444). **Distinct child outcomes:** [element-023](named-elements.md#element-023).

<a id="s445"></a>
### S445 · Review and deliver: EL-024 · Primed siphon

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Breaking the column stops the siphon; it cannot lift indefinitely above available pressure. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-445). **Distinct child outcomes:** [element-024](named-elements.md#element-024).

<a id="s446"></a>
### S446 · Review and deliver: EL-025 · Tipping-bucket water clock

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Subthreshold volume does not emit a tip event. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-446). **Distinct child outcomes:** [element-025](named-elements.md#element-025).

<a id="s447"></a>
### S447 · Review and deliver: EL-026 · Communicating tank

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected tank does not equalise remotely. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-447). **Distinct child outcomes:** [element-026](named-elements.md#element-026).

<a id="s448"></a>
### S448 · Review and deliver: EL-027 · Canal lock chamber

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Open ends cannot hold a raised level without a balancing supply. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-448). **Distinct child outcomes:** [element-027](named-elements.md#element-027).

<a id="s449"></a>
### S449 · Review and deliver: EL-028 · Buoyant platform

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Overload or lost water removes support. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-449). **Distinct child outcomes:** [element-028](named-elements.md#element-028).

<a id="s450"></a>
### S450 · Review and deliver: EL-029 · Boat

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Leak or overload changes flotation; no authored route animation. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-450). **Distinct child outcomes:** [element-029](named-elements.md#element-029).

<a id="s451"></a>
### S451 · Review and deliver: EL-030 · Flow meter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Stopped flow reads zero despite upstream stored volume. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-451). **Distinct child outcomes:** [element-030](named-elements.md#element-030).

<a id="s452"></a>
### S452 · Review and deliver: EL-031 · Volume meter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Repeated inspection does not count the same volume twice. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-452). **Distinct child outcomes:** [element-031](named-elements.md#element-031).

<a id="s453"></a>
### S453 · Review and deliver: EL-032 · Pressure meter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected or depressurised port cannot report supplied pressure. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-453). **Distinct child outcomes:** [element-032](named-elements.md#element-032).

<a id="s454"></a>
### S454 · Review and deliver: EL-033 · Fluid accumulator

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Discharging lowers stored energy; no free recharge. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-454). **Distinct child outcomes:** [element-033](named-elements.md#element-033).

<a id="s455"></a>
### S455 · Review and deliver: EL-034 · Sponge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Saturated sponge cannot remove additional volume. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-455). **Distinct child outcomes:** [element-034](named-elements.md#element-034).

<a id="s456"></a>
### S456 · Review and deliver: EL-035 · Wick

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Dry reservoir stops transport; mass remains accounted for. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-456). **Distinct child outcomes:** [element-035](named-elements.md#element-035).

<a id="s457"></a>
### S457 · Review and deliver: EL-036 · Sprinkler

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Blocked supply prevents spray and off-footprint receivers remain dry. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-457). **Distinct child outcomes:** [element-036](named-elements.md#element-036).

<a id="s458"></a>
### S458 · Review and deliver: EL-165 · Manual tap

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Closed aperture stops flow; opening cannot supply water without a connected reservoir. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-458). **Distinct child outcomes:** [element-165](named-elements.md#element-165).

<a id="s459"></a>
### S459 · Review and deliver: EL-166 · Mechanically actuated tap

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A slack or stalled linkage cannot change aperture. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-459). **Distinct child outcomes:** [element-166](named-elements.md#element-166).

<a id="s460"></a>
### S460 · Review and deliver: EL-167 · Solenoid tap

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A true command without supply cannot open the valve. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-460). **Distinct child outcomes:** [element-167](named-elements.md#element-167).

<a id="s461"></a>
### S461 · Review and deliver: EL-168 · Siphon priming bulb

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Insufficient priming leaves the siphon broken; no remote automatic fill. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-461). **Distinct child outcomes:** [element-168](named-elements.md#element-168).

<a id="s462"></a>
### S462 · Review and deliver: EL-169 · Cork float

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Dry or overloaded cork cannot provide unlimited lift. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-462). **Distinct child outcomes:** [element-169](named-elements.md#element-169).

<a id="s463"></a>
### S463 · Review and deliver: EL-170 · Raft

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Overload or separated members change stability. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-463). **Distinct child outcomes:** [element-170](named-elements.md#element-170).

<a id="s464"></a>
### S464 · Review and deliver: EL-171 · Squeeze pad

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A dry pad cannot produce water and squeezing consumes work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-464). **Distinct child outcomes:** [element-171](named-elements.md#element-171).

<a id="s465"></a>
### S465 · Review and deliver: EL-172 · Rain collector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An occluded or missed stream does not fill it. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-465). **Distinct child outcomes:** [element-172](named-elements.md#element-172).

<a id="s466"></a>
### S466 · Review and deliver: P1: waterwheel with signed mechanical output to belts/conveyors and reusable discharge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A stream turns the wheel in the visible direction and powers a connected mechanism; stopping the flow stops new energy input and its discharge can be collected below.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-466).

<a id="s467"></a>
### S467 · Review and deliver: P1 — Leaky bucket loses mass over time. Manual(https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf)

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P1 &#124; Leaky bucket loses mass over time. Manual &#124; Extend moving containers with conserved water outflow, changing load and visible fill level; catch leaks in other containers. Teach delayed counterbalance. See expanded water research; countdown-only mass loss is no longer the proposed contract. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-467).

<a id="s468"></a>
### S468 · Review and deliver: P2 Hydraulic piston — expand existing water entry — Finite liquid displacement, return path, pressure/head and resisting load; visible rod and tank levels. Not a duplicate pneumatic cylinder with different colo

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Hydraulic piston — expand existing water entry &#124; Finite liquid displacement, return path, pressure/head and resisting load; visible rod and tank levels. Not a duplicate pneumatic cylinder with different colour. &#124; Elevated tank lifts loaded tray; return water drives wheel. Test conservation, insufficient head, sealed return,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-468).

<a id="s469"></a>
### S469 · Review and deliver: Audit newly delivered water costs and lifecycle after this block

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-22 — qualify combined workload and completion coverage. Extend PERF-14–16 to every required domain, per-element mode and applicable mixed-domain chain, including many-source/many-receiver stress, long optical paths, dense constraints and connected fluid networks. Track domain implementation, focused behavioural proof,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-469).


 **Exact closure members:** [S418](../work-register.md#execution-step-418), [S419](../work-register.md#execution-step-419), [S420](../work-register.md#execution-step-420), [S421](../work-register.md#execution-step-421), [S422](../work-register.md#execution-step-422), [S423](../work-register.md#execution-step-423), [S424](../work-register.md#execution-step-424), [S425](../work-register.md#execution-step-425), [S426](../work-register.md#execution-step-426), [S427](../work-register.md#execution-step-427), [S428](../work-register.md#execution-step-428), [S429](../work-register.md#execution-step-429), [S430](../work-register.md#execution-step-430), [S431](../work-register.md#execution-step-431), [S432](../work-register.md#execution-step-432), [S433](../work-register.md#execution-step-433), [S434](../work-register.md#execution-step-434), [S435](../work-register.md#execution-step-435), [S436](../work-register.md#execution-step-436), [S437](../work-register.md#execution-step-437), [S438](../work-register.md#execution-step-438), [S439](../work-register.md#execution-step-439), [S440](../work-register.md#execution-step-440), [S441](../work-register.md#execution-step-441), [S442](../work-register.md#execution-step-442), [S443](../work-register.md#execution-step-443), [S444](../work-register.md#execution-step-444), [S445](../work-register.md#execution-step-445), [S446](../work-register.md#execution-step-446), [S447](../work-register.md#execution-step-447), [S448](../work-register.md#execution-step-448), [S449](../work-register.md#execution-step-449), [S450](../work-register.md#execution-step-450), [S451](../work-register.md#execution-step-451), [S452](../work-register.md#execution-step-452), [S453](../work-register.md#execution-step-453), [S454](../work-register.md#execution-step-454), [S455](../work-register.md#execution-step-455), [S456](../work-register.md#execution-step-456), [S457](../work-register.md#execution-step-457), [S458](../work-register.md#execution-step-458), [S459](../work-register.md#execution-step-459), [S460](../work-register.md#execution-step-460), [S461](../work-register.md#execution-step-461), [S462](../work-register.md#execution-step-462), [S463](../work-register.md#execution-step-463), [S464](../work-register.md#execution-step-464), [S465](../work-register.md#execution-step-465), [S466](../work-register.md#execution-step-466), [S467](../work-register.md#execution-step-467), [S468](../work-register.md#execution-step-468). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s472"></a>
### S472 · Review and deliver: EL-037 · Air compressor

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unpowered compressor cannot increase stored gas energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-472). **Distinct child outcomes:** [element-037](named-elements.md#element-037).

<a id="s473"></a>
### S473 · Review and deliver: EL-038 · Pneumatic hose

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Open or disconnected hose vents or isolates according to its boundary declaration. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-473). **Distinct child outcomes:** [element-038](named-elements.md#element-038).

<a id="s474"></a>
### S474 · Review and deliver: EL-039 · Air reservoir

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Repeated strokes deplete pressure. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-474). **Distinct child outcomes:** [element-039](named-elements.md#element-039).

<a id="s475"></a>
### S475 · Review and deliver: EL-040 · Pneumatic release valve

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Closed valve cannot send a command-shaped free pressure pulse. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-475). **Distinct child outcomes:** [element-040](named-elements.md#element-040).

<a id="s476"></a>
### S476 · Review and deliver: EL-041 · Pneumatic directional valve

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Blocked exhaust changes motion; opposite routes cannot both receive full independent supply. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-476). **Distinct child outcomes:** [element-041](named-elements.md#element-041).

<a id="s477"></a>
### S477 · Review and deliver: EL-042 · Air nozzle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Insufficient pressure cannot move a distant load. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-477). **Distinct child outcomes:** [element-042](named-elements.md#element-042).

<a id="s478"></a>
### S478 · Review and deliver: EL-043 · Pneumatic pressure gauge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Empty reservoir reads ambient rather than a stale charged state. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-478). **Distinct child outcomes:** [element-043](named-elements.md#element-043).

<a id="s479"></a>
### S479 · Review and deliver: EL-208 · Balloon

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Tether force opposes ascent; lost contents alter buoyancy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-479). **Distinct child outcomes:** [element-208](named-elements.md#element-208).

<a id="s480"></a>
### S480 · Review and deliver: EL-108 · Powered airlift

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Insufficient airflow cannot lift the load. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-480). **Distinct child outcomes:** [element-108](named-elements.md#element-108).

<a id="s481"></a>
### S481 · Review and deliver: P2 Spring-return pneumatic piston — expand pneumatic entry — Finite compressed-air supply and valve extend rod; visible spring returns it on exhaust. Show stroke and reservoir state. Do not equate pressure with

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Spring-return pneumatic piston — expand pneumatic entry &#124; Finite compressed-air supply and valve extend rod; visible spring returns it on exhaust. Show stroke and reservoir state. Do not equate pressure with an electrical on/off flag. &#124; Bellows/reservoir → valve → piston diverts a ball; exhaust → whistle. Test inadequate… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-481).

<a id="s482"></a>
### S482 · Review and deliver: P2 Double-acting pneumatic piston — Two typed chamber ports and directional valve drive extension and retraction; distinct from spring return. Account for supply and exhaust in both directions

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** P2 Double-acting pneumatic piston &#124; Two typed chamber ports and directional valve drive extension and retraction; distinct from spring return. Account for supply and exhaust in both directions. &#124; Clock/logic alternates a physical sorting pusher between two chutes. Test both chambers pressurised, valve transitions,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-482).

<a id="s483"></a>
### S483 · Review and deliver: Audit newly delivered pneumatic costs and lifecycle after this block

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-22 — qualify combined workload and completion coverage. Extend PERF-14–16 to every required domain, per-element mode and applicable mixed-domain chain, including many-source/many-receiver stress, long optical paths, dense constraints and connected fluid networks. Track domain implementation, focused behavioural proof,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-483).


 **Exact closure members:** [S471](../work-register.md#execution-step-471), [S472](../work-register.md#execution-step-472), [S473](../work-register.md#execution-step-473), [S474](../work-register.md#execution-step-474), [S475](../work-register.md#execution-step-475), [S476](../work-register.md#execution-step-476), [S477](../work-register.md#execution-step-477), [S478](../work-register.md#execution-step-478), [S479](../work-register.md#execution-step-479), [S480](../work-register.md#execution-step-480), [S481](../work-register.md#execution-step-481), [S482](../work-register.md#execution-step-482). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s492"></a>
### S492 · Review and deliver: EL-176 · Red laser emitter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing supply or enable prevents output. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-492). **Distinct child outcomes:** [element-176](named-elements.md#element-176).

<a id="s493"></a>
### S493 · Review and deliver: EL-177 · Green laser emitter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Wrong-channel receiver control remains inactive. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-493). **Distinct child outcomes:** [element-177](named-elements.md#element-177).

<a id="s494"></a>
### S494 · Review and deliver: EL-178 · Blue laser emitter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Blocked aperture prevents downstream illumination. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-494). **Distinct child outcomes:** [element-178](named-elements.md#element-178).

<a id="s495"></a>
### S495 · Review and deliver: EL-210 · Flashlight

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Occluding geometry blocks coverage and no supply means no emission. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-495). **Distinct child outcomes:** [element-210](named-elements.md#element-210).

<a id="s496"></a>
### S496 · Review and deliver: EL-212 · Flat mirror

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Rear incidence and occluded paths do not reflect through the housing. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 51–60; practice61–70; reuse111–120,142 as separate objectives in existing slots.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-496). **Distinct child outcomes:** [element-212](named-elements.md#element-212).

<a id="s497"></a>
### S497 · Review and deliver: EL-214 · Broadband beam detector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Back-face, insufficient incident power and missing electrical supply are separate negative controls. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 51–60; practice71–80; reuse110,142 as separate objectives in existing slots.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-497). **Distinct child outcomes:** [element-214](named-elements.md#element-214).

<a id="s498"></a>
### S498 · Review and deliver: EL-143 · Red optical filter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Absent red input cannot be recoloured into red; opaque frame blocks rays. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-498). **Distinct child outcomes:** [element-143](named-elements.md#element-143).

<a id="s499"></a>
### S499 · Review and deliver: EL-144 · Green optical filter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Absent green input cannot be recoloured into green; opaque frame blocks rays. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-499). **Distinct child outcomes:** [element-144](named-elements.md#element-144).

<a id="s500"></a>
### S500 · Review and deliver: EL-145 · Blue optical filter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Absent blue input cannot be recoloured into blue; opaque frame blocks rays. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-500). **Distinct child outcomes:** [element-145](named-elements.md#element-145).

<a id="s501"></a>
### S501 · Review and deliver: EL-146 · Red selective receiver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-501). **Distinct child outcomes:** [element-146](named-elements.md#element-146).

<a id="s502"></a>
### S502 · Review and deliver: EL-147 · Green selective receiver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-502). **Distinct child outcomes:** [element-147](named-elements.md#element-147).

<a id="s503"></a>
### S503 · Review and deliver: EL-148 · Blue selective receiver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-503). **Distinct child outcomes:** [element-148](named-elements.md#element-148).

<a id="s504"></a>
### S504 · Review and deliver: EL-149 · Yellow selective receiver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-504). **Distinct child outcomes:** [element-149](named-elements.md#element-149).

<a id="s505"></a>
### S505 · Review and deliver: EL-150 · Cyan selective receiver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-505). **Distinct child outcomes:** [element-150](named-elements.md#element-150).

<a id="s506"></a>
### S506 · Review and deliver: EL-151 · Magenta selective receiver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-506). **Distinct child outcomes:** [element-151](named-elements.md#element-151).

<a id="s507"></a>
### S507 · Review and deliver: EL-152 · White selective receiver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing required channel, wrong channel and absent supply remain independent negative controls. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-507). **Distinct child outcomes:** [element-152](named-elements.md#element-152).

<a id="s508"></a>
### S508 · Review and deliver: EL-211 · Solar panel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Darkness yields no power; output never exceeds absorbed energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-508). **Distinct child outcomes:** [element-211](named-elements.md#element-211).

<a id="s509"></a>
### S509 · Review and deliver: EL-213 · Optical combiner

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A missing channel is not synthesized, and crossing unrelated beams does not combine by object identity. Apply the individual register's shared visual, generic-process and per-element proof requirements. **Campaign:** 51–60; practice71–80; reuse117,142 as separate objectives in existing slots.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-509). **Distinct child outcomes:** [element-213](named-elements.md#element-213).

<a id="s510"></a>
### S510 · Review and deliver: EL-138 · Optical AND gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-510). **Distinct child outcomes:** [element-138](named-elements.md#element-138).

<a id="s511"></a>
### S511 · Review and deliver: EL-139 · Optical OR gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-511). **Distinct child outcomes:** [element-139](named-elements.md#element-139).

<a id="s512"></a>
### S512 · Review and deliver: EL-140 · Optical XOR gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-512). **Distinct child outcomes:** [element-140](named-elements.md#element-140).

<a id="s513"></a>
### S513 · Review and deliver: EL-141 · Optical NOR gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-513). **Distinct child outcomes:** [element-141](named-elements.md#element-141).

<a id="s514"></a>
### S514 · Review and deliver: EL-142 · Optical NAND gate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Prove every input combination plus absent carrier; logically true cannot create power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-514). **Distinct child outcomes:** [element-142](named-elements.md#element-142).

<a id="s515"></a>
### S515 · Review and deliver: EL-153 · General-light receiver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Occluded flashlight cone and absent independent supply cannot activate output. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-515). **Distinct child outcomes:** [element-153](named-elements.md#element-153).

<a id="s516"></a>
### S516 · Review and deliver: EL-154 · Light-charge receiver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Two bursts accumulate correctly; dark time cannot create charge. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-516). **Distinct child outcomes:** [element-154](named-elements.md#element-154).

<a id="s517"></a>
### S517 · Review and deliver: EL-155 · Rope-operated light

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Slack rope or missing supply produces no light. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-517). **Distinct child outcomes:** [element-155](named-elements.md#element-155).

<a id="s518"></a>
### S518 · Review and deliver: TH-06 · Potential element: Converging lens

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Defocus, occlusion or inadequate source power fails to reach ignition conditions. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-518). **Distinct child outcomes:** [thermal-06](named-elements.md#thermal-06).

<a id="s519"></a>
### S519 · Review and deliver: EL-173 · Diverging lens

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Two receivers share the beam; each cannot obtain the unsplit full power. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-519). **Distinct child outcomes:** [element-173](named-elements.md#element-173).

<a id="s520"></a>
### S520 · Review and deliver: EL-174 · Prism

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Red-only input creates no green or blue output. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-520). **Distinct child outcomes:** [element-174](named-elements.md#element-174).

<a id="s521"></a>
### S521 · Review and deliver: EL-175 · Optical fibre

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected endpoints cannot bridge a gap. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-521). **Distinct child outcomes:** [element-175](named-elements.md#element-175).

<a id="s527"></a>
### S527 · Review and deliver: Audit newly delivered optical costs and lifecycle after this block

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-22 — qualify combined workload and completion coverage. Extend PERF-14–16 to every required domain, per-element mode and applicable mixed-domain chain, including many-source/many-receiver stress, long optical paths, dense constraints and connected fluid networks. Track domain implementation, focused behavioural proof,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-527).


 **Exact closure members:** [S485](../work-register.md#execution-step-485), [S486](../work-register.md#execution-step-486), [S487](../work-register.md#execution-step-487), [S488](../work-register.md#execution-step-488), [S489](../work-register.md#execution-step-489), [S490](../work-register.md#execution-step-490), [S491](../work-register.md#execution-step-491), [S492](../work-register.md#execution-step-492), [S493](../work-register.md#execution-step-493), [S494](../work-register.md#execution-step-494), [S495](../work-register.md#execution-step-495), [S496](../work-register.md#execution-step-496), [S497](../work-register.md#execution-step-497), [S498](../work-register.md#execution-step-498), [S499](../work-register.md#execution-step-499), [S500](../work-register.md#execution-step-500), [S501](../work-register.md#execution-step-501), [S502](../work-register.md#execution-step-502), [S503](../work-register.md#execution-step-503), [S504](../work-register.md#execution-step-504), [S505](../work-register.md#execution-step-505), [S506](../work-register.md#execution-step-506), [S507](../work-register.md#execution-step-507), [S508](../work-register.md#execution-step-508), [S509](../work-register.md#execution-step-509), [S510](../work-register.md#execution-step-510), [S511](../work-register.md#execution-step-511), [S512](../work-register.md#execution-step-512), [S513](../work-register.md#execution-step-513), [S514](../work-register.md#execution-step-514), [S515](../work-register.md#execution-step-515), [S516](../work-register.md#execution-step-516), [S517](../work-register.md#execution-step-517), [S518](../work-register.md#execution-step-518), [S519](../work-register.md#execution-step-519), [S520](../work-register.md#execution-step-520), [S521](../work-register.md#execution-step-521), [S522](../work-register.md#execution-step-522), [S523](../work-register.md#execution-step-523), [S524](../work-register.md#execution-step-524), [S525](../work-register.md#execution-step-525), [S526](../work-register.md#execution-step-526). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s531"></a>
### S531 · Review and deliver: EL-179 · Continuous-tone speaker

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Power loss stops excitation; stored resonance may decay only by its own state. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-531). **Distinct child outcomes:** [element-179](named-elements.md#element-179).

<a id="s532"></a>
### S532 · Review and deliver: EL-180 · Pulse speaker

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Held input follows its edge contract and cannot create infinite pulse energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-532). **Distinct child outcomes:** [element-180](named-elements.md#element-180).

<a id="s533"></a>
### S533 · Review and deliver: EL-045 · Air whistle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Blocked or absent flow produces no sound event. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-533). **Distinct child outcomes:** [element-045](named-elements.md#element-045).

<a id="s534"></a>
### S534 · Review and deliver: EL-047 · Exit horn

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected inlet emits nothing. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-534). **Distinct child outcomes:** [element-047](named-elements.md#element-047).

<a id="s535"></a>
### S535 · Review and deliver: EL-048 · Acoustic duct

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected mouths cannot carry a hidden signal. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-535). **Distinct child outcomes:** [element-048](named-elements.md#element-048).

<a id="s536"></a>
### S536 · Review and deliver: EL-049 · Acoustic resonator

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Off-band pulses do not accumulate the same response. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-536). **Distinct child outcomes:** [element-049](named-elements.md#element-049).

<a id="s537"></a>
### S537 · Review and deliver: EL-050 · Acoustic screen

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A geometric gap permits transmission; no universal mute field. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-537). **Distinct child outcomes:** [element-050](named-elements.md#element-050).

<a id="s538"></a>
### S538 · Review and deliver: EL-051 · Acoustic dish

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Misalignment misses the listener and cannot amplify total energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-538). **Distinct child outcomes:** [element-051](named-elements.md#element-051).

<a id="s539"></a>
### S539 · Review and deliver: EL-052 · Water-tuned bottle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Changing fill changes supported tone; dry and filled cases remain distinct. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-539). **Distinct child outcomes:** [element-052](named-elements.md#element-052).

<a id="s540"></a>
### S540 · Review and deliver: EL-046 · Listening horn

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Wrong-facing or occluded arrivals lose coupling. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-540). **Distinct child outcomes:** [element-046](named-elements.md#element-046).

<a id="s541"></a>
### S541 · Review and deliver: EL-044 · Tone-selective sound meter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Equal-strength wrong-tone pulse does not close the contact. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-541). **Distinct child outcomes:** [element-044](named-elements.md#element-044).

<a id="s542"></a>
### S542 · Review and deliver: Audit newly delivered acoustic costs and lifecycle after this block

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-22 — qualify combined workload and completion coverage. Extend PERF-14–16 to every required domain, per-element mode and applicable mixed-domain chain, including many-source/many-receiver stress, long optical paths, dense constraints and connected fluid networks. Track domain implementation, focused behavioural proof,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-542).


 **Exact closure members:** [S529](../work-register.md#execution-step-529), [S530](../work-register.md#execution-step-530), [S531](../work-register.md#execution-step-531), [S532](../work-register.md#execution-step-532), [S533](../work-register.md#execution-step-533), [S534](../work-register.md#execution-step-534), [S535](../work-register.md#execution-step-535), [S536](../work-register.md#execution-step-536), [S537](../work-register.md#execution-step-537), [S538](../work-register.md#execution-step-538), [S539](../work-register.md#execution-step-539), [S540](../work-register.md#execution-step-540), [S541](../work-register.md#execution-step-541). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s567"></a>
### S567 · Review and deliver: TH-04 · Potential element: Electrical heating plate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Disconnected or exhausted supply cannot create new heat. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-567). **Distinct child outcomes:** [thermal-04](named-elements.md#thermal-04).

<a id="s568"></a>
### S568 · Review and deliver: TH-21 · Potential element: Temperature sensor

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An unattached probe or unmet threshold does not assert the goal; sensing supplies no heat or electrical work. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-568). **Distinct child outcomes:** [thermal-21](named-elements.md#thermal-21).

<a id="s569"></a>
### S569 · Review and deliver: TH-26 · Potential element: Thermal storage block

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An equal-temperature block supplies no net heat; repeated deliveries deplete its stored difference. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-569). **Distinct child outcomes:** [thermal-26](named-elements.md#thermal-26).

<a id="s570"></a>
### S570 · Review and deliver: TH-08 · Potential element: Heat-conducting bar

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An air gap removes solid contact conduction; a low-conductivity comparison transfers less under the same conditions. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-570). **Distinct child outcomes:** [thermal-08](named-elements.md#thermal-08).

<a id="s571"></a>
### S571 · Review and deliver: TH-09 · Potential element: Insulating panel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A thinner/lower-resistance comparison leaks heat faster; insulation is not a universal heat deletion field. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-571). **Distinct child outcomes:** [thermal-09](named-elements.md#thermal-09).

<a id="s572"></a>
### S572 · Review and deliver: TH-10 · Potential element: Finned heat sink

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An equal-temperature environment gives no net cooling; passive convection cannot cool below ambient. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-572). **Distinct child outcomes:** [thermal-10](named-elements.md#thermal-10).

<a id="s573"></a>
### S573 · Review and deliver: TH-07 · Potential element: Solar absorber plate

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A reflective comparison absorbs less; shading removes the input without immediately deleting stored heat. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-573). **Distinct child outcomes:** [thermal-07](named-elements.md#thermal-07).

<a id="s574"></a>
### S574 · Review and deliver: TH-05 · Potential element: Friction brake

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A stationary brake makes no frictional heat; shaft energy loss balances thermal gain and declared losses. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-574). **Distinct child outcomes:** [thermal-05](named-elements.md#thermal-05).

<a id="s575"></a>
### S575 · Review and deliver: TH-11 · Potential element: Heat exchanger

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Equal-temperature streams give no net exchange; blocked flow limits delivery and cannot cross-contaminate channels. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-575). **Distinct child outcomes:** [thermal-11](named-elements.md#thermal-11).

<a id="s576"></a>
### S576 · Review and deliver: TH-12 · Potential element: Reversible heat pump

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Power loss stops pumping; an insulated hot side warms and limits cooling rather than swallowing heat. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-576). **Distinct child outcomes:** [thermal-12](named-elements.md#thermal-12).

<a id="s577"></a>
### S577 · Review and deliver: TH-13 · Potential element: Freezing mold

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Insufficient energy removal leaves a measured liquid/solid mixture; equal-temperature surroundings cannot freeze it. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-577). **Distinct child outcomes:** [thermal-13](named-elements.md#thermal-13).

<a id="s578"></a>
### S578 · Review and deliver: TH-14 · Potential element: Ice block

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A cold control retains support; partially melted material cannot disappear or keep impossible full-block support. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-578). **Distinct child outcomes:** [thermal-14](named-elements.md#thermal-14).

<a id="s579"></a>
### S579 · Review and deliver: TH-15 · Potential element: Ice plug

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Heating an unrelated body does not open it; residual solid continues to obstruct as geometry dictates. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-579). **Distinct child outcomes:** [thermal-15](named-elements.md#thermal-15).

<a id="s580"></a>
### S580 · Review and deliver: TH-16 · Potential element: Fusible link

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Subthreshold heating retains support; cooling broken material does not reattach a severed graph. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-580). **Distinct child outcomes:** [thermal-16](named-elements.md#thermal-16).

<a id="s581"></a>
### S581 · Review and deliver: TH-17 · Potential element: Kettle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An empty vessel emits no water vapor; warm water requires further sensible/latent input before the specified vapor output. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-581). **Distinct child outcomes:** [thermal-17](named-elements.md#thermal-17).

<a id="s582"></a>
### S582 · Review and deliver: TH-18 · Potential element: Condenser

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A warm or exhausted sink cannot condense an unlimited vapor stream. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-582). **Distinct child outcomes:** [thermal-18](named-elements.md#thermal-18).

<a id="s583"></a>
### S583 · Review and deliver: TH-19 · Potential element: Steam piston

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No pressure difference, blocked exhaust or excessive load prevents the predicted stroke. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-583). **Distinct child outcomes:** [thermal-19](named-elements.md#thermal-19).

<a id="s584"></a>
### S584 · Review and deliver: TH-20 · Potential element: Steam turbine

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No available pressure/enthalpy drop gives no useful work; stall cannot generate free power. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-584). **Distinct child outcomes:** [thermal-20](named-elements.md#thermal-20).

<a id="s585"></a>
### S585 · Review and deliver: TH-22 · Potential element: Bimetal thermostat

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A uniform-material control lacks the same curvature; verify contact opening/closing and hysteresis from declared geometry/material mechanics. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-585). **Distinct child outcomes:** [thermal-22](named-elements.md#thermal-22).

<a id="s586"></a>
### S586 · Review and deliver: TH-23 · Potential element: Expansion rod

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Insufficient temperature change cannot bridge the target gap; constrained heating produces accounted stress. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-586). **Distinct child outcomes:** [thermal-23](named-elements.md#thermal-23).

<a id="s587"></a>
### S587 · Review and deliver: TH-24 · Potential element: Gas expansion bladder

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A vented control cannot retain the same pressure; blocked expansion changes pressure rather than prescribing motion. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-587). **Distinct child outcomes:** [thermal-24](named-elements.md#thermal-24).

<a id="s588"></a>
### S588 · Review and deliver: TH-25 · Potential element: Hot-air balloon

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Excess load or cooling prevents ascent; no fixed rising velocity or balloon-specific levitation rule. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-588). **Distinct child outcomes:** [thermal-25](named-elements.md#thermal-25).

<a id="s589"></a>
### S589 · Review and deliver: TH-27 · Potential element: Phase-change storage cartridge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A fully transitioned cartridge cannot repeat a charge/discharge without reversing the energy transfer. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-589). **Distinct child outcomes:** [thermal-27](named-elements.md#thermal-27).

<a id="s590"></a>
### S590 · Review and deliver: TH-28 · Potential element: Evaporative cooling pad

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A dry pad or saturated surrounding gas prevents equivalent cooling; liquid consumption balances vapor output. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-590). **Distinct child outcomes:** [thermal-28](named-elements.md#thermal-28).

<a id="s591"></a>
### S591 · Review and deliver: TH-29 · Potential element: Cold pack

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A warmed pack has depleted cooling capacity; no endless subambient boundary is attached secretly. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-591). **Distinct child outcomes:** [thermal-29](named-elements.md#thermal-29).

<a id="s592"></a>
### S592 · Review and deliver: TH-30 · Potential element: Thermoelectric generator

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Equal temperatures produce no thermoelectric work; load and finite cold-side rejection affect output. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-592). **Distinct child outcomes:** [thermal-30](named-elements.md#thermal-30).

<a id="s593"></a>
### S593 · Review and deliver: TH-31 · Potential element: Flint striker

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A miss, insufficient work or a nonreactive target produces no sustained combustion. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-593). **Distinct child outcomes:** [thermal-31](named-elements.md#thermal-31).

<a id="s594"></a>
### S594 · Review and deliver: TH-32 · Potential element: Tinder pad

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Defocused/insufficient heating fails; reacted material cannot be reused as fresh fuel. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-594). **Distinct child outcomes:** [thermal-32](named-elements.md#thermal-32).

<a id="s595"></a>
### S595 · Review and deliver: TH-33 · Potential element: Spring-mounted match

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No contact or spent reactive tip cannot ignite; winding the spring alone supplies no flame. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-595). **Distinct child outcomes:** [thermal-33](named-elements.md#thermal-33).

<a id="s596"></a>
### S596 · Review and deliver: TH-03 · Potential element: Combustible block

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A noncombustible control absorbs heat but does not burn; spent fuel cannot reignite indefinitely. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-596). **Distinct child outcomes:** [thermal-03](named-elements.md#thermal-03).

<a id="s597"></a>
### S597 · Review and deliver: TH-02 · Potential element: Fire bowl

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An empty bowl or inadequate oxidizer cannot sustain flame. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-597). **Distinct child outcomes:** [thermal-02](named-elements.md#thermal-02).

<a id="s598"></a>
### S598 · Review and deliver: TH-01 · Potential element: Candle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An unlit or exhausted candle produces no sustained output. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-598). **Distinct child outcomes:** [thermal-01](named-elements.md#thermal-01).

<a id="s599"></a>
### S599 · Review and deliver: TH-34 · Potential element: Timed toaster ejector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No supply cannot warm; an obstructed carriage cannot teleport the payload; time alone is not proof of temperature. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-599). **Distinct child outcomes:** [thermal-34](named-elements.md#thermal-34).

<a id="s600"></a>
### S600 · Review and deliver: TH-35 · Potential element: Coffee-pot steam vessel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An empty or insufficiently heated pot does not emit vapor; a blocked spout uses actual pressure boundaries. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-600). **Distinct child outcomes:** [thermal-35](named-elements.md#thermal-35).

<a id="s601"></a>
### S601 · Review and deliver: TH-36 · Potential element: Lamp-trigger apparatus

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An unactuated or unsupplied source produces no new output; demonstrate trigger displacement and optical/thermal energy independently. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-601). **Distinct child outcomes:** [thermal-36](named-elements.md#thermal-36).

<a id="s602"></a>
### S602 · Review and deliver: TH-37 · Potential element: Heat-sensitive target

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Warm another disconnected target or remain below the target threshold and the goal stays false. Prove the positive use, this control, generic connections, finite stores and exact restoration after Run/Reset and save/reload.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-602). **Distinct child outcomes:** [thermal-37](named-elements.md#thermal-37).

<a id="s603"></a>
### S603 · Review and deliver: Audit newly delivered thermal costs and lifecycle after this block

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-22 — qualify combined workload and completion coverage. Extend PERF-14–16 to every required domain, per-element mode and applicable mixed-domain chain, including many-source/many-receiver stress, long optical paths, dense constraints and connected fluid networks. Track domain implementation, focused behavioural proof,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-603).


 **Exact closure members:** [S544](../work-register.md#execution-step-544), [S545](../work-register.md#execution-step-545), [S546](../work-register.md#execution-step-546), [S547](../work-register.md#execution-step-547), [S548](../work-register.md#execution-step-548), [S549](../work-register.md#execution-step-549), [S550](../work-register.md#execution-step-550), [S551](../work-register.md#execution-step-551), [S552](../work-register.md#execution-step-552), [S553](../work-register.md#execution-step-553), [S554](../work-register.md#execution-step-554), [S555](../work-register.md#execution-step-555), [S556](../work-register.md#execution-step-556), [S557](../work-register.md#execution-step-557), [S558](../work-register.md#execution-step-558), [S559](../work-register.md#execution-step-559), [S560](../work-register.md#execution-step-560), [S561](../work-register.md#execution-step-561), [S562](../work-register.md#execution-step-562), [S563](../work-register.md#execution-step-563), [S564](../work-register.md#execution-step-564), [S565](../work-register.md#execution-step-565), [S566](../work-register.md#execution-step-566), [S567](../work-register.md#execution-step-567), [S568](../work-register.md#execution-step-568), [S569](../work-register.md#execution-step-569), [S570](../work-register.md#execution-step-570), [S571](../work-register.md#execution-step-571), [S572](../work-register.md#execution-step-572), [S573](../work-register.md#execution-step-573), [S574](../work-register.md#execution-step-574), [S575](../work-register.md#execution-step-575), [S576](../work-register.md#execution-step-576), [S577](../work-register.md#execution-step-577), [S578](../work-register.md#execution-step-578), [S579](../work-register.md#execution-step-579), [S580](../work-register.md#execution-step-580), [S581](../work-register.md#execution-step-581), [S582](../work-register.md#execution-step-582), [S583](../work-register.md#execution-step-583), [S584](../work-register.md#execution-step-584), [S585](../work-register.md#execution-step-585), [S586](../work-register.md#execution-step-586), [S587](../work-register.md#execution-step-587), [S588](../work-register.md#execution-step-588), [S589](../work-register.md#execution-step-589), [S590](../work-register.md#execution-step-590), [S591](../work-register.md#execution-step-591), [S592](../work-register.md#execution-step-592), [S593](../work-register.md#execution-step-593), [S594](../work-register.md#execution-step-594), [S595](../work-register.md#execution-step-595), [S596](../work-register.md#execution-step-596), [S597](../work-register.md#execution-step-597), [S598](../work-register.md#execution-step-598), [S599](../work-register.md#execution-step-599), [S600](../work-register.md#execution-step-600), [S601](../work-register.md#execution-step-601), [S602](../work-register.md#execution-step-602). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s608"></a>
### S608 · Review and deliver: RAD-01 · P1 potential: Gamma source capsule

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Moving the capsule farther away lowers the meter reading; disconnecting a nearby battery does not stop emission. Source pose and initial state restore exactly.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-608). **Distinct child outcomes:** [radiation-01](named-elements.md#radiation-01).

<a id="s609"></a>
### S609 · Review and deliver: RAD-02 · P1 potential: Powered X-ray emitter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Supply removal stops emission; a disconnected enable input cannot fire. Test every supported energy preset, direction and restart.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-609). **Distinct child outcomes:** [radiation-02](named-elements.md#radiation-02).

<a id="s610"></a>
### S610 · Review and deliver: EL-126 · Thin-screen shield

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Gaps transmit; supported alpha/beta/photon responses follow shared transport coefficients. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-610). **Distinct child outcomes:** [element-126](named-elements.md#element-126).

<a id="s611"></a>
### S611 · Review and deliver: EL-127 · Polymer shield

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** It cannot inherit dense-shield performance from its category. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-611). **Distinct child outcomes:** [element-127](named-elements.md#element-127).

<a id="s612"></a>
### S612 · Review and deliver: EL-128 · Dense shield

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Increasing thickness cannot increase passive transmitted energy. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-612). **Distinct child outcomes:** [element-128](named-elements.md#element-128).

<a id="s613"></a>
### S613 · Review and deliver: RAD-06 · P1 potential: Powered radiation shutter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Closed blade reduces exposure below the taught target threshold; an obstructed blade leaks according to geometry. Power loss cannot teleport the blade closed.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-613). **Distinct child outcomes:** [radiation-06](named-elements.md#radiation-06).

<a id="s614"></a>
### S614 · Review and deliver: RAD-07 · P1 potential: Collimator block

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** An aligned receiver responds while an off-axis control does not; a narrower opening never increases total transmitted power.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-614). **Distinct child outcomes:** [radiation-07](named-elements.md#radiation-07).

<a id="s615"></a>
### S615 · Review and deliver: RAD-08 · P1 potential: Radiation rate meter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Correct exposure switches the output; wrong channel, blocked path and missing supply do not. Muting clicks leaves results identical.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-615). **Distinct child outcomes:** [radiation-08](named-elements.md#radiation-08).

<a id="s616"></a>
### S616 · Review and deliver: RAD-09 · P1 potential: Integrating dosimeter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Two separated exposures add to the same total as one equivalent exposure; darkness does not erase the total. Run/Reset clears it to the authored initial value.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-616). **Distinct child outcomes:** [radiation-09](named-elements.md#radiation-09).

<a id="s617"></a>
### S617 · Review and deliver: RAD-10 · P1 potential: Exposure-sensitive cargo badge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A shielded route delivers within budget; the same endpoint reached by an exposed route fails. Cargo retains its dose through stops and loses it only on Reset.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-617). **Distinct child outcomes:** [radiation-10](named-elements.md#radiation-10).

<a id="s618"></a>
### S618 · Review and deliver: EL-129 · Sheet-thickness transmission gauge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missing source is invalid rather than maximum thickness. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-618). **Distinct child outcomes:** [element-129](named-elements.md#element-129).

<a id="s619"></a>
### S619 · Review and deliver: EL-130 · Tank-level transmission gauge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Empty and full readings follow the material path; disconnected source is invalid. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-619). **Distinct child outcomes:** [element-130](named-elements.md#element-130).

<a id="s620"></a>
### S620 · Review and deliver: RAD-03 · P2 potential: Alpha source cartridge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A close compatible detector responds; extra separation or a thin screen suppresses it. Gamma-only reception rejects it.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-620). **Distinct child outcomes:** [radiation-03](named-elements.md#radiation-03).

<a id="s621"></a>
### S621 · Review and deliver: RAD-04 · P2 potential: Beta-minus source cartridge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A taught thin screen passes enough beta to detect while a polymer screen suppresses it; opposite bending is proven with the deflector.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-621). **Distinct child outcomes:** [radiation-04](named-elements.md#radiation-04).

<a id="s622"></a>
### S622 · Review and deliver: RAD-12 · P2 potential: Scintillator tile

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Incident radiation produces observable light and a receiver response; shielding prevents both. Conversion loss is explicit and output cannot exceed absorbed energy.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-622). **Distinct child outcomes:** [radiation-12](named-elements.md#radiation-12).

<a id="s623"></a>
### S623 · Review and deliver: RAD-13 · P2 potential: Decay clock capsule

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Successive equal half-life intervals halve source strength; a rate contact releases below its threshold, and Reset restores the initial activity and age.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-623). **Distinct child outcomes:** [radiation-13](named-elements.md#radiation-13).

<a id="s624"></a>
### S624 · Review and deliver: RAD-14 · P3 potential: Magnetic deflector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Polarity reversal swaps the charged route; gamma/X-ray controls remain straight. Field-off and opposite-charge cases are separately proven.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-624). **Distinct child outcomes:** [radiation-14](named-elements.md#radiation-14).

<a id="s625"></a>
### S625 · Review and deliver: RAD-15 · P3 potential: Track chamber

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The selected exit segment triggers only for a traversing track; a decorative trail or a miss cannot trigger it. Neutral photons make no direct charged track in the initial abstraction.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-625). **Distinct child outcomes:** [radiation-15](named-elements.md#radiation-15).

<a id="s626"></a>
### S626 · Review and deliver: RAD-16 · P3 potential: Neutron source module

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A fast-sensitive neutron detector responds; a photon-only detector does not. Ordinary magnetic deflection leaves the neutron route unchanged.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-626). **Distinct child outcomes:** [radiation-16](named-elements.md#radiation-16).

<a id="s627"></a>
### S627 · Review and deliver: RAD-17 · P3 potential: Water moderator tank

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Filling the tank increases the slow-group response in the authored range; a dry tank and a drained tank fail that control. Slowdown is not absorption.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-627). **Distinct child outcomes:** [radiation-17](named-elements.md#radiation-17).

<a id="s628"></a>
### S628 · Review and deliver: RAD-18 · P3 potential: Neutron absorber panel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Adding it after the moderator suppresses the slow detector; removing it restores response. Dense photon shielding does not substitute for its neutron contract.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-628). **Distinct child outcomes:** [radiation-18](named-elements.md#radiation-18).

<a id="s629"></a>
### S629 · Review and deliver: EL-131 · Fast-neutron detector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Slow-group and photon controls cannot masquerade as fast flux. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-629). **Distinct child outcomes:** [element-131](named-elements.md#element-131).

<a id="s630"></a>
### S630 · Review and deliver: EL-132 · Slow-neutron detector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unmoderated fast flux does not silently count as slow. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-630). **Distinct child outcomes:** [element-132](named-elements.md#element-132).

<a id="s631"></a>
### S631 · Review and deliver: RAD-20 · P3 potential: Radioisotope thermoelectric generator

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Cooling permits useful output; an equal-temperature control produces no thermoelectric output. Heat, electrical work, load and Reset state are accounted for.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-631). **Distinct child outcomes:** [radiation-20](named-elements.md#radiation-20).

<a id="s632"></a>
### S632 · Review and deliver: RAD-21 · P3 potential: Radiation-responsive material latch

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Enough exposure releases the load; shielding or insufficient accumulated exposure retains it. Reset restores material state, latch pose and stored load.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-632). **Distinct child outcomes:** [radiation-21](named-elements.md#radiation-21).

<a id="s633"></a>
### S633 · Review and deliver: RAD-22 · P2 potential: Sealed tracer capsule

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Two detectors observe the capsule in causal order and drive a gate/counter; a stationary capsule cannot repeatedly count as new arrivals. Branches move one capsule without copying it.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-633). **Distinct child outcomes:** [radiation-22](named-elements.md#radiation-22).

<a id="s634"></a>
### S634 · Review and deliver: Audit newly delivered radiation costs and lifecycle after this block

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-22 — qualify combined workload and completion coverage. Extend PERF-14–16 to every required domain, per-element mode and applicable mixed-domain chain, including many-source/many-receiver stress, long optical paths, dense constraints and connected fluid networks. Track domain implementation, focused behavioural proof,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-634).


 **Exact closure members:** [S606](../work-register.md#execution-step-606), [S607](../work-register.md#execution-step-607), [S608](../work-register.md#execution-step-608), [S609](../work-register.md#execution-step-609), [S610](../work-register.md#execution-step-610), [S611](../work-register.md#execution-step-611), [S612](../work-register.md#execution-step-612), [S613](../work-register.md#execution-step-613), [S614](../work-register.md#execution-step-614), [S615](../work-register.md#execution-step-615), [S616](../work-register.md#execution-step-616), [S617](../work-register.md#execution-step-617), [S618](../work-register.md#execution-step-618), [S619](../work-register.md#execution-step-619), [S620](../work-register.md#execution-step-620), [S621](../work-register.md#execution-step-621), [S622](../work-register.md#execution-step-622), [S623](../work-register.md#execution-step-623), [S624](../work-register.md#execution-step-624), [S625](../work-register.md#execution-step-625), [S626](../work-register.md#execution-step-626), [S627](../work-register.md#execution-step-627), [S628](../work-register.md#execution-step-628), [S629](../work-register.md#execution-step-629), [S630](../work-register.md#execution-step-630), [S631](../work-register.md#execution-step-631), [S632](../work-register.md#execution-step-632), [S633](../work-register.md#execution-step-633). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s636"></a>
### S636 · Review and deliver: P2 — Baseball: additional ball type

Conditional design decision; adoption status preserved; see exact scope correction. **Source outcome:** P2 &#124; Baseball: additional ball type &#124; Add a recognisable silhouette/material variant only after measured mass/bounce differences justify a distinct puzzle role. Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-636).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s636).

<a id="s638"></a>
### S638 · Review and deliver: GAP-04 · P2 potential: Driven wheel

Design decision before driven-wheel mode; see exact scope correction. **Source outcome:** Supply drives a loaded vehicle; missing supply produces no new work, an overloaded drive stalls and a low-friction wheel slips. Coasting comes only from stored motion. Verify connected drivetrain and driven-wheel Reset independently of GAP-03. Apply the common completion requirements above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-638). **Distinct child outcomes:** [gap-04](named-elements.md#gap-04).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s638).

<a id="s639"></a>
### S639 · Review and deliver: GAP-06 · P3 potential: Granular dispenser

Design decision before finite grain feed; see exact scope correction. **Source outcome:** Discharged plus retained material equals initial feed within the explicit representation tolerance. Empty feed stops, a blocked aperture jams, and opening the physical route clears it. Verify interaction with buckets and weighing controls. Apply the common completion requirements above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-639). **Distinct child outcomes:** [gap-06](named-elements.md#gap-06).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s639).

<a id="s640"></a>
### S640 · Review and deliver: GAP-07 · P3 potential: Granular sieve

Conditional model decision; separate adopted modes; see exact scope correction. **Source outcome:** Small grains pass and oversize grains remain; an all-oversize control blocks and finite mixed feed conserves both fractions. Verify either gravity-only operation or every separately adopted powered-shake mode before using it. Apply the common completion requirements above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-640). **Distinct child outcomes:** [gap-07](named-elements.md#gap-07).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s640).

<a id="s641"></a>
### S641 · Review and deliver: GAP-09 · P3 potential: Fragmentation station

Design decision before breakable-material mode; see exact scope correction. **Source outcome:** Insufficient impact leaves cargo intact; adequate supplied work creates finite fragments that can pass a smaller route. Blocked/unsupplied controls fail appropriately; fragments cannot create extra feed or lose inherited state on save/Reset. Apply the common completion requirements above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-641). **Distinct child outcomes:** [gap-09](named-elements.md#gap-09).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s641).

<a id="s642"></a>
### S642 · Review and deliver: EL-060 · Electromagnet

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Nonresponsive material and absent supply do not satisfy pickup. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-642). **Distinct child outcomes:** [element-060](named-elements.md#element-060).

<a id="s643"></a>
### S643 · Review and deliver: EL-073 · Brick barrier

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No passage through intact geometry. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-643). **Distinct child outcomes:** [element-073](named-elements.md#element-073).

<a id="s644"></a>
### S644 · Review and deliver: EL-074 · Wood barrier

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Mechanical contact cannot silently use brick parameters. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-644). **Distinct child outcomes:** [element-074](named-elements.md#element-074).

<a id="s645"></a>
### S645 · Review and deliver: EL-075 · Toy pulse emitter

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unpowered request emits none; held input follows the explicit retrigger rule. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-645). **Distinct child outcomes:** [element-075](named-elements.md#element-075).

<a id="s646"></a>
### S646 · Review and deliver: EL-076 · Programmable ball

Design decision before programmable preset; see exact scope correction. **Source outcome:** Unsupported combinations are rejected; no runtime numeric solution editor. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-646). **Distinct child outcomes:** [element-076](named-elements.md#element-076).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s646).

<a id="s647"></a>
### S647 · Review and deliver: EL-077 · Soccer ball

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Its bounce and load differ reproducibly from a heavy ball. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-647). **Distinct child outcomes:** [element-077](named-elements.md#element-077).

<a id="s648"></a>
### S648 · Review and deliver: EL-078 · Can opener

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Misalignment and missing supply leave the seal intact. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-648). **Distinct child outcomes:** [element-078](named-elements.md#element-078).

<a id="s649"></a>
### S649 · Review and deliver: EL-079 · Electric mixer

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Stall or missing contents cannot create a processed result. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-649). **Distinct child outcomes:** [element-079](named-elements.md#element-079).

<a id="s650"></a>
### S650 · Review and deliver: EL-080 · Programmable box

Design decision before programmable physical container; see exact scope correction. **Source outcome:** Contents and collisions respond to its actual geometry. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-650). **Distinct child outcomes:** [element-080](named-elements.md#element-080).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s650).

<a id="s651"></a>
### S651 · Review and deliver: EL-081 · Message display

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unconnected inputs cannot display a success event. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-651). **Distinct child outcomes:** [element-081](named-elements.md#element-081).

<a id="s652"></a>
### S652 · Review and deliver: EL-082 · Lured character

Design decision before character perception; see exact scope correction. **Source outcome:** Occluded or unsupported stimulus does not trigger pursuit. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-652). **Distinct child outcomes:** [element-082](named-elements.md#element-082).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s652).

<a id="s653"></a>
### S653 · Review and deliver: EL-083 · Escaping character

Design decision before hazard avoidance; see exact scope correction. **Source outcome:** Absent or blocked perception does not trigger a hidden scripted escape. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-653). **Distinct child outcomes:** [element-083](named-elements.md#element-083).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s653).

<a id="s654"></a>
### S654 · Review and deliver: EL-084 · Fragile fish bowl

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Subthreshold impact retains contents. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-654). **Distinct child outcomes:** [element-084](named-elements.md#element-084).

<a id="s655"></a>
### S655 · Review and deliver: EL-085 · Rope-driven character wheel

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Slack rope supplies no work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-655). **Distinct child outcomes:** [element-085](named-elements.md#element-085).

<a id="s656"></a>
### S656 · Review and deliver: EL-086 · Obstacle-reversing walker

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Distant obstacle does not reverse it early. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-656). **Distinct child outcomes:** [element-086](named-elements.md#element-086).

<a id="s657"></a>
### S657 · Review and deliver: EL-087 · Predator character

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A barrier prevents traversal; target names cannot bypass sensing. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-657). **Distinct child outcomes:** [element-087](named-elements.md#element-087).

<a id="s658"></a>
### S658 · Review and deliver: EL-088 · Fish tank lure

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Occlusion affects perception and intact walls retain contents. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-658). **Distinct child outcomes:** [element-088](named-elements.md#element-088).

<a id="s659"></a>
### S659 · Review and deliver: EL-089 · Gravity-effect pad

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Outside bodies retain world gravity; label the field as deliberate game fiction. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-659). **Distinct child outcomes:** [element-089](named-elements.md#element-089).

<a id="s660"></a>
### S660 · Review and deliver: EL-090 · Steerable blimp

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unpowered drive cannot steer against a current. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-660). **Distinct child outcomes:** [element-090](named-elements.md#element-090).

<a id="s661"></a>
### S661 · Review and deliver: EL-091 · Cannonball

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Different launchers move the same body through generic contact and work. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-661). **Distinct child outcomes:** [element-091](named-elements.md#element-091).

<a id="s662"></a>
### S662 · Review and deliver: EL-092 · Toy rocket

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Spent rocket produces no further thrust; abstract toy parameters only. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-662). **Distinct child outcomes:** [element-092](named-elements.md#element-092).

<a id="s663"></a>
### S663 · Review and deliver: EL-093 · Toy dynamite charge

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Spent charge cannot repeat; no real formulation or construction recipe. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-663). **Distinct child outcomes:** [element-093](named-elements.md#element-093).

<a id="s664"></a>
### S664 · Review and deliver: EL-094 · Detonation plunger

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Command supplies no explosion energy of its own. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-664). **Distinct child outcomes:** [element-094](named-elements.md#element-094).

<a id="s665"></a>
### S665 · Review and deliver: EL-095 · Toy revolver

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Empty or uncharged state cannot manufacture a projectile. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-665). **Distinct child outcomes:** [element-095](named-elements.md#element-095).

<a id="s666"></a>
### S666 · Review and deliver: EL-096 · Tipsy platform

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Balanced or constrained platform does not tip on a timer. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-666). **Distinct child outcomes:** [element-096](named-elements.md#element-096).

<a id="s667"></a>
### S667 · Review and deliver: EL-097 · Pool cue

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A miss cannot move its target. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-667). **Distinct child outcomes:** [element-097](named-elements.md#element-097).

<a id="s668"></a>
### S668 · Review and deliver: EL-098 · Pool ball

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No-gravity historical variant, if adopted, requires a separate explicitly fictional field rule. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-668). **Distinct child outcomes:** [element-098](named-elements.md#element-098).

<a id="s669"></a>
### S669 · Review and deliver: EL-099 · Pool pocket

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A ball beside the opening is not pocketed. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-669). **Distinct child outcomes:** [element-099](named-elements.md#element-099).

<a id="s670"></a>
### S670 · Review and deliver: EL-100 · Vacuum nozzle

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Blocked inlet or missing supply prevents suction. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-670). **Distinct child outcomes:** [element-100](named-elements.md#element-100).

<a id="s671"></a>
### S671 · Review and deliver: EL-101 · Thumb tack

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Missed contact or puncture-resistant material remains intact. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-671). **Distinct child outcomes:** [element-101](named-elements.md#element-101).

<a id="s672"></a>
### S672 · Review and deliver: EL-118 · Coating station

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Absent supply or missed contact leaves cargo untreated. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-672). **Distinct child outcomes:** [element-118](named-elements.md#element-118).

<a id="s673"></a>
### S673 · Review and deliver: EL-119 · Dye station

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** No pigment means no colour change; optical illumination is not dye. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-673). **Distinct child outcomes:** [element-119](named-elements.md#element-119).

<a id="s678"></a>
### S678 · Review and deliver: EL-124 · Authored gravity preset

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Save and replay preserve it independently of difficulty. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-678). **Distinct child outcomes:** [element-124](named-elements.md#element-124).

<a id="s679"></a>
### S679 · Review and deliver: EL-125 · Authored atmosphere preset

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Unsupported presets are rejected; a visual sky change alone cannot affect physics. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-679). **Distinct child outcomes:** [element-125](named-elements.md#element-125).

<a id="s680"></a>
### S680 · Review and deliver: EL-159 · Tension-limited connector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Subthreshold tension retains the joint. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-680). **Distinct child outcomes:** [element-159](named-elements.md#element-159).

<a id="s681"></a>
### S681 · Review and deliver: EL-160 · Shear-limited connector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Pure supported axial load does not silently count as shear. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-681). **Distinct child outcomes:** [element-160](named-elements.md#element-160).

<a id="s682"></a>
### S682 · Review and deliver: EL-161 · Bending-limited connector

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Subthreshold moment retains the joint regardless of display animation. Apply the shared visual, generic-interaction, campaign and per-element proof contracts above.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-682). **Distinct child outcomes:** [element-161](named-elements.md#element-161).

<a id="s683"></a>
### S683 · Review and deliver: Audit specialist scaling and complete the per-part publication ledger

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** PERF-22 — qualify combined workload and completion coverage. Extend PERF-14–16 to every required domain, per-element mode and applicable mixed-domain chain, including many-source/many-receiver stress, long optical paths, dense constraints and connected fluid networks. Track domain implementation, focused behavioural proof,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-683).


 **Exact closure members:** [S636](../work-register.md#execution-step-636), [S637](../work-register.md#execution-step-637), [S638](../work-register.md#execution-step-638), [S639](../work-register.md#execution-step-639), [S640](../work-register.md#execution-step-640), [S641](../work-register.md#execution-step-641), [S642](../work-register.md#execution-step-642), [S643](../work-register.md#execution-step-643), [S644](../work-register.md#execution-step-644), [S645](../work-register.md#execution-step-645), [S646](../work-register.md#execution-step-646), [S647](../work-register.md#execution-step-647), [S648](../work-register.md#execution-step-648), [S649](../work-register.md#execution-step-649), [S650](../work-register.md#execution-step-650), [S651](../work-register.md#execution-step-651), [S652](../work-register.md#execution-step-652), [S653](../work-register.md#execution-step-653), [S654](../work-register.md#execution-step-654), [S655](../work-register.md#execution-step-655), [S656](../work-register.md#execution-step-656), [S657](../work-register.md#execution-step-657), [S658](../work-register.md#execution-step-658), [S659](../work-register.md#execution-step-659), [S660](../work-register.md#execution-step-660), [S661](../work-register.md#execution-step-661), [S662](../work-register.md#execution-step-662), [S663](../work-register.md#execution-step-663), [S664](../work-register.md#execution-step-664), [S665](../work-register.md#execution-step-665), [S666](../work-register.md#execution-step-666), [S667](../work-register.md#execution-step-667), [S668](../work-register.md#execution-step-668), [S669](../work-register.md#execution-step-669), [S670](../work-register.md#execution-step-670), [S671](../work-register.md#execution-step-671), [S672](../work-register.md#execution-step-672), [S673](../work-register.md#execution-step-673), [S674](../work-register.md#execution-step-674), [S675](../work-register.md#execution-step-675), [S676](../work-register.md#execution-step-676), [S677](../work-register.md#execution-step-677), [S678](../work-register.md#execution-step-678), [S679](../work-register.md#execution-step-679), [S680](../work-register.md#execution-step-680), [S681](../work-register.md#execution-step-681), [S682](../work-register.md#execution-step-682). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s716"></a>
### S716 · Review and deliver: TX-01: Fire heats water into steam

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 70 introduction, 71 practice, 86, 140 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-716).

<a id="s717"></a>
### S717 · Review and deliver: TX-02: Powered cooling freezes water

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 79 introduction, 80 practice, 89, 149 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-717).

<a id="s718"></a>
### S718 · Review and deliver: TX-03: Focused light ignites combustible material

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 59 introduction, 60 practice, 85, 142 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-718).

<a id="s719"></a>
### S719 · Review and deliver: TX-04: Vapor drives a piston

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 75 introduction, 76 practice, 86, 143 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-719).

<a id="s720"></a>
### S720 · Review and deliver: TX-05: Condensation recovers working fluid

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 74 introduction, 75 practice, 87, 137 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-720).

<a id="s721"></a>
### S721 · Review and deliver: TX-06: Differential expansion operates a switch

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 83 introduction, 84 practice, 90, 145 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-721).

<a id="s722"></a>
### S722 · Review and deliver: TX-07: Melting removes structural support

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 67 introduction, 68 practice, 89, 139 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-722).

<a id="s723"></a>
### S723 · Review and deliver: TX-08: Airflow cools a hot surface

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 40 introduction, 41 practice, 78, 145 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-723).

<a id="s724"></a>
### S724 · Review and deliver: TX-09: Evaporation cools a wet surface

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 73 introduction, 74 practice, 88, 145 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-724).

<a id="s725"></a>
### S725 · Review and deliver: TX-10: Mechanical braking heats a thermal store

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 39 introduction, 40 practice, 81, 147 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-725).

<a id="s726"></a>
### S726 · Review and deliver: TX-11: Stored heat powers an electrical load

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 87 introduction, 88 practice, 131, 145 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-726).

<a id="s727"></a>
### S727 · Review and deliver: TX-12: Cooling extinguishes combustion

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** The stated chain and controls pass through shared capabilities with no catalogue-pair/level-specific code. Record an actual-UI recipe, typed state/connection observations, before/after quantities and exact Run/Reset/save restoration. **Campaign:** 60 introduction, 61 practice, 86, 149 later combinations.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-727).

<a id="s728"></a>
### S728 · Review and deliver: TX-13: Vapor drives a turbine

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** Shaft work and exhaust state balance supplied energy, with no turbine/source pair handler. Record actual-UI positive/control, typed coupling and exact Run/Reset/save restoration. **Campaign:** 76 introduction, 77 practice, 87 and147 reuse.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-728).

<a id="s729"></a>
### S729 · Review and deliver: TX-14: Oxidizer restriction extinguishes combustion

Source-derived interaction candidate; exact D/model decisions required before implementation. **Source outcome:** A restricted route limits sustained reaction while an open-route control does not. No named hood/fuel lookup or instant vanish event. Record actual-UI construction, gas-state observations and exact Run/Reset/save restoration. **Campaign:** 61 introduction after cooling suppression60, 62 practice, 86 and149 reuse.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-729).

<a id="mobile-01"></a>
### MOBILE-01 · Gesture arbitration and cancellation

Aggregate current interaction improvements; exact first gesture required. **Source outcome:** One interaction across supported touch devices using typed commands; correct targets/modes, cancelled gestures, zero unintended Run/lost edits, exact Reset/save and <=100 ms pending feedback; preserve palette.

[Complete criteria/status/technical predecessors](../work-register.md#work-mobile-01).

<a id="mobile-04"></a>
### MOBILE-04 · Touch typed port selection/wiring

Aggregate current interaction improvements; exact first gesture required. **Source outcome:** One interaction across supported touch devices using typed commands; correct targets/modes, cancelled gestures, zero unintended Run/lost edits, exact Reset/save and <=100 ms pending feedback; preserve palette.

[Complete criteria/status/technical predecessors](../work-register.md#work-mobile-04).

<a id="art-01"></a>
### ART-01 · Environment form/composition

Aggregate named scene/part/track applications; one child per change. **Source outcome:** Apply DESIGN.md and existing palette; freeze before/after views and observable readability/selection criteria; preserve physical geometry and clocks; measure active rendering cost and UI controls.

[Complete criteria/status/technical predecessors](../work-register.md#work-art-01).

<a id="art-02"></a>
### ART-02 · Part silhouettes/port legibility

Aggregate named scene/part/track applications; one child per change. **Source outcome:** Apply DESIGN.md and existing palette; freeze before/after views and observable readability/selection criteria; preserve physical geometry and clocks; measure active rendering cost and UI controls.

[Complete criteria/status/technical predecessors](../work-register.md#work-art-02).

<a id="art-03"></a>
### ART-03 · Lighting/material readability

Aggregate named scene/part/track applications; one child per change. **Source outcome:** Apply DESIGN.md and existing palette; freeze before/after views and observable readability/selection criteria; preserve physical geometry and clocks; measure active rendering cost and UI controls.

[Complete criteria/status/technical predecessors](../work-register.md#work-art-03).

<a id="art-04"></a>
### ART-04 · Calm motion/reduced-motion application

Aggregate named scene/part/track applications; one child per change. **Source outcome:** Apply DESIGN.md and existing palette; freeze before/after views and observable readability/selection criteria; preserve physical geometry and clocks; measure active rendering cost and UI controls.

[Complete criteria/status/technical predecessors](../work-register.md#work-art-04).

<a id="chapter-01-h"></a>
### CHAPTER-01-H · Evaluate chapter 01 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-01-h).


 **Exact closure members:** [LEVEL-001](../work-register.md#work-level-001), [LEVEL-002](../work-register.md#work-level-002), [LEVEL-003](../work-register.md#work-level-003), [LEVEL-004](../work-register.md#work-level-004), [LEVEL-005](../work-register.md#work-level-005), [LEVEL-006](../work-register.md#work-level-006), [LEVEL-007](../work-register.md#work-level-007), [LEVEL-008](../work-register.md#work-level-008), [LEVEL-009](../work-register.md#work-level-009), [LEVEL-010](../work-register.md#work-level-010), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-02-h"></a>
### CHAPTER-02-H · Evaluate chapter 02 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-02-h).


 **Exact closure members:** [LEVEL-011](../work-register.md#work-level-011), [LEVEL-012](../work-register.md#work-level-012), [LEVEL-013](../work-register.md#work-level-013), [LEVEL-014](../work-register.md#work-level-014), [LEVEL-015](../work-register.md#work-level-015), [LEVEL-016](../work-register.md#work-level-016), [LEVEL-017](../work-register.md#work-level-017), [LEVEL-018](../work-register.md#work-level-018), [LEVEL-019](../work-register.md#work-level-019), [LEVEL-020](../work-register.md#work-level-020), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-03-h"></a>
### CHAPTER-03-H · Evaluate chapter 03 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-03-h).


 **Exact closure members:** [LEVEL-021](../work-register.md#work-level-021), [LEVEL-022](../work-register.md#work-level-022), [LEVEL-023](../work-register.md#work-level-023), [LEVEL-024](../work-register.md#work-level-024), [LEVEL-025](../work-register.md#work-level-025), [LEVEL-026](../work-register.md#work-level-026), [LEVEL-027](../work-register.md#work-level-027), [LEVEL-028](../work-register.md#work-level-028), [LEVEL-029](../work-register.md#work-level-029), [LEVEL-030](../work-register.md#work-level-030), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-04-h"></a>
### CHAPTER-04-H · Evaluate chapter 04 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-04-h).


 **Exact closure members:** [LEVEL-031](../work-register.md#work-level-031), [LEVEL-032](../work-register.md#work-level-032), [LEVEL-033](../work-register.md#work-level-033), [LEVEL-034](../work-register.md#work-level-034), [LEVEL-035](../work-register.md#work-level-035), [LEVEL-036](../work-register.md#work-level-036), [LEVEL-037](../work-register.md#work-level-037), [LEVEL-038](../work-register.md#work-level-038), [LEVEL-039](../work-register.md#work-level-039), [LEVEL-040](../work-register.md#work-level-040), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-05-h"></a>
### CHAPTER-05-H · Evaluate chapter 05 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-05-h).


 **Exact closure members:** [LEVEL-041](../work-register.md#work-level-041), [LEVEL-042](../work-register.md#work-level-042), [LEVEL-043](../work-register.md#work-level-043), [LEVEL-044](../work-register.md#work-level-044), [LEVEL-045](../work-register.md#work-level-045), [LEVEL-046](../work-register.md#work-level-046), [LEVEL-047](../work-register.md#work-level-047), [LEVEL-048](../work-register.md#work-level-048), [LEVEL-049](../work-register.md#work-level-049), [LEVEL-050](../work-register.md#work-level-050), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-06-h"></a>
### CHAPTER-06-H · Evaluate chapter 06 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-06-h).


 **Exact closure members:** [LEVEL-051](../work-register.md#work-level-051), [LEVEL-052](../work-register.md#work-level-052), [LEVEL-053](../work-register.md#work-level-053), [LEVEL-054](../work-register.md#work-level-054), [LEVEL-055](../work-register.md#work-level-055), [LEVEL-056](../work-register.md#work-level-056), [LEVEL-057](../work-register.md#work-level-057), [LEVEL-058](../work-register.md#work-level-058), [LEVEL-059](../work-register.md#work-level-059), [LEVEL-060](../work-register.md#work-level-060), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-07-h"></a>
### CHAPTER-07-H · Evaluate chapter 07 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-07-h).


 **Exact closure members:** [LEVEL-061](../work-register.md#work-level-061), [LEVEL-062](../work-register.md#work-level-062), [LEVEL-063](../work-register.md#work-level-063), [LEVEL-064](../work-register.md#work-level-064), [LEVEL-065](../work-register.md#work-level-065), [LEVEL-066](../work-register.md#work-level-066), [LEVEL-067](../work-register.md#work-level-067), [LEVEL-068](../work-register.md#work-level-068), [LEVEL-069](../work-register.md#work-level-069), [LEVEL-070](../work-register.md#work-level-070), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-08-h"></a>
### CHAPTER-08-H · Evaluate chapter 08 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-08-h).


 **Exact closure members:** [LEVEL-071](../work-register.md#work-level-071), [LEVEL-072](../work-register.md#work-level-072), [LEVEL-073](../work-register.md#work-level-073), [LEVEL-074](../work-register.md#work-level-074), [LEVEL-075](../work-register.md#work-level-075), [LEVEL-076](../work-register.md#work-level-076), [LEVEL-077](../work-register.md#work-level-077), [LEVEL-078](../work-register.md#work-level-078), [LEVEL-079](../work-register.md#work-level-079), [LEVEL-080](../work-register.md#work-level-080), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-09-h"></a>
### CHAPTER-09-H · Evaluate chapter 09 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-09-h).


 **Exact closure members:** [LEVEL-081](../work-register.md#work-level-081), [LEVEL-082](../work-register.md#work-level-082), [LEVEL-083](../work-register.md#work-level-083), [LEVEL-084](../work-register.md#work-level-084), [LEVEL-085](../work-register.md#work-level-085), [LEVEL-086](../work-register.md#work-level-086), [LEVEL-087](../work-register.md#work-level-087), [LEVEL-088](../work-register.md#work-level-088), [LEVEL-089](../work-register.md#work-level-089), [LEVEL-090](../work-register.md#work-level-090), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-10-h"></a>
### CHAPTER-10-H · Evaluate chapter 10 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-10-h).


 **Exact closure members:** [LEVEL-091](../work-register.md#work-level-091), [LEVEL-092](../work-register.md#work-level-092), [LEVEL-093](../work-register.md#work-level-093), [LEVEL-094](../work-register.md#work-level-094), [LEVEL-095](../work-register.md#work-level-095), [LEVEL-096](../work-register.md#work-level-096), [LEVEL-097](../work-register.md#work-level-097), [LEVEL-098](../work-register.md#work-level-098), [LEVEL-099](../work-register.md#work-level-099), [LEVEL-100](../work-register.md#work-level-100), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-11-h"></a>
### CHAPTER-11-H · Evaluate chapter 11 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-11-h).


 **Exact closure members:** [LEVEL-101](../work-register.md#work-level-101), [LEVEL-102](../work-register.md#work-level-102), [LEVEL-103](../work-register.md#work-level-103), [LEVEL-104](../work-register.md#work-level-104), [LEVEL-105](../work-register.md#work-level-105), [LEVEL-106](../work-register.md#work-level-106), [LEVEL-107](../work-register.md#work-level-107), [LEVEL-108](../work-register.md#work-level-108), [LEVEL-109](../work-register.md#work-level-109), [LEVEL-110](../work-register.md#work-level-110), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-12-h"></a>
### CHAPTER-12-H · Evaluate chapter 12 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-12-h).


 **Exact closure members:** [LEVEL-111](../work-register.md#work-level-111), [LEVEL-112](../work-register.md#work-level-112), [LEVEL-113](../work-register.md#work-level-113), [LEVEL-114](../work-register.md#work-level-114), [LEVEL-115](../work-register.md#work-level-115), [LEVEL-116](../work-register.md#work-level-116), [LEVEL-117](../work-register.md#work-level-117), [LEVEL-118](../work-register.md#work-level-118), [LEVEL-119](../work-register.md#work-level-119), [LEVEL-120](../work-register.md#work-level-120), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-13-h"></a>
### CHAPTER-13-H · Evaluate chapter 13 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-13-h).


 **Exact closure members:** [LEVEL-121](../work-register.md#work-level-121), [LEVEL-122](../work-register.md#work-level-122), [LEVEL-123](../work-register.md#work-level-123), [LEVEL-124](../work-register.md#work-level-124), [LEVEL-125](../work-register.md#work-level-125), [LEVEL-126](../work-register.md#work-level-126), [LEVEL-127](../work-register.md#work-level-127), [LEVEL-128](../work-register.md#work-level-128), [LEVEL-129](../work-register.md#work-level-129), [LEVEL-130](../work-register.md#work-level-130), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-14-h"></a>
### CHAPTER-14-H · Evaluate chapter 14 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-14-h).


 **Exact closure members:** [LEVEL-131](../work-register.md#work-level-131), [LEVEL-132](../work-register.md#work-level-132), [LEVEL-133](../work-register.md#work-level-133), [LEVEL-134](../work-register.md#work-level-134), [LEVEL-135](../work-register.md#work-level-135), [LEVEL-136](../work-register.md#work-level-136), [LEVEL-137](../work-register.md#work-level-137), [LEVEL-138](../work-register.md#work-level-138), [LEVEL-139](../work-register.md#work-level-139), [LEVEL-140](../work-register.md#work-level-140), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-15-h"></a>
### CHAPTER-15-H · Evaluate chapter 15 with players

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** Freeze first/combination/finale tasks, participant count/questions/pass thresholds before sessions; observe clarity/agency/fatigue on exact revision; retain feedback/failures. Missing participants keep this incomplete.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-15-h).


 **Exact closure members:** [LEVEL-141](../work-register.md#work-level-141), [LEVEL-142](../work-register.md#work-level-142), [LEVEL-143](../work-register.md#work-level-143), [LEVEL-144](../work-register.md#work-level-144), [LEVEL-145](../work-register.md#work-level-145), [LEVEL-146](../work-register.md#work-level-146), [LEVEL-147](../work-register.md#work-level-147), [LEVEL-148](../work-register.md#work-level-148), [LEVEL-149](../work-register.md#work-level-149), [LEVEL-150](../work-register.md#work-level-150), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-01"></a>
### CHAPTER-01 · Reconcile chapter 01

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-01).


 **Exact closure members:** [CHAPTER-01-H](../work-register.md#work-chapter-01-h), [LEVEL-001](../work-register.md#work-level-001), [LEVEL-002](../work-register.md#work-level-002), [LEVEL-003](../work-register.md#work-level-003), [LEVEL-004](../work-register.md#work-level-004), [LEVEL-005](../work-register.md#work-level-005), [LEVEL-006](../work-register.md#work-level-006), [LEVEL-007](../work-register.md#work-level-007), [LEVEL-008](../work-register.md#work-level-008), [LEVEL-009](../work-register.md#work-level-009), [LEVEL-010](../work-register.md#work-level-010), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-02"></a>
### CHAPTER-02 · Reconcile chapter 02

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-02).


 **Exact closure members:** [CHAPTER-02-H](../work-register.md#work-chapter-02-h), [LEVEL-011](../work-register.md#work-level-011), [LEVEL-012](../work-register.md#work-level-012), [LEVEL-013](../work-register.md#work-level-013), [LEVEL-014](../work-register.md#work-level-014), [LEVEL-015](../work-register.md#work-level-015), [LEVEL-016](../work-register.md#work-level-016), [LEVEL-017](../work-register.md#work-level-017), [LEVEL-018](../work-register.md#work-level-018), [LEVEL-019](../work-register.md#work-level-019), [LEVEL-020](../work-register.md#work-level-020), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-03"></a>
### CHAPTER-03 · Reconcile chapter 03

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-03).


 **Exact closure members:** [CHAPTER-03-H](../work-register.md#work-chapter-03-h), [LEVEL-021](../work-register.md#work-level-021), [LEVEL-022](../work-register.md#work-level-022), [LEVEL-023](../work-register.md#work-level-023), [LEVEL-024](../work-register.md#work-level-024), [LEVEL-025](../work-register.md#work-level-025), [LEVEL-026](../work-register.md#work-level-026), [LEVEL-027](../work-register.md#work-level-027), [LEVEL-028](../work-register.md#work-level-028), [LEVEL-029](../work-register.md#work-level-029), [LEVEL-030](../work-register.md#work-level-030), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-04"></a>
### CHAPTER-04 · Reconcile chapter 04

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-04).


 **Exact closure members:** [CHAPTER-04-H](../work-register.md#work-chapter-04-h), [LEVEL-031](../work-register.md#work-level-031), [LEVEL-032](../work-register.md#work-level-032), [LEVEL-033](../work-register.md#work-level-033), [LEVEL-034](../work-register.md#work-level-034), [LEVEL-035](../work-register.md#work-level-035), [LEVEL-036](../work-register.md#work-level-036), [LEVEL-037](../work-register.md#work-level-037), [LEVEL-038](../work-register.md#work-level-038), [LEVEL-039](../work-register.md#work-level-039), [LEVEL-040](../work-register.md#work-level-040), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-05"></a>
### CHAPTER-05 · Reconcile chapter 05

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-05).


 **Exact closure members:** [CHAPTER-05-H](../work-register.md#work-chapter-05-h), [LEVEL-041](../work-register.md#work-level-041), [LEVEL-042](../work-register.md#work-level-042), [LEVEL-043](../work-register.md#work-level-043), [LEVEL-044](../work-register.md#work-level-044), [LEVEL-045](../work-register.md#work-level-045), [LEVEL-046](../work-register.md#work-level-046), [LEVEL-047](../work-register.md#work-level-047), [LEVEL-048](../work-register.md#work-level-048), [LEVEL-049](../work-register.md#work-level-049), [LEVEL-050](../work-register.md#work-level-050), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-06"></a>
### CHAPTER-06 · Reconcile chapter 06

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-06).


 **Exact closure members:** [CHAPTER-06-H](../work-register.md#work-chapter-06-h), [LEVEL-051](../work-register.md#work-level-051), [LEVEL-052](../work-register.md#work-level-052), [LEVEL-053](../work-register.md#work-level-053), [LEVEL-054](../work-register.md#work-level-054), [LEVEL-055](../work-register.md#work-level-055), [LEVEL-056](../work-register.md#work-level-056), [LEVEL-057](../work-register.md#work-level-057), [LEVEL-058](../work-register.md#work-level-058), [LEVEL-059](../work-register.md#work-level-059), [LEVEL-060](../work-register.md#work-level-060), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-07"></a>
### CHAPTER-07 · Reconcile chapter 07

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-07).


 **Exact closure members:** [CHAPTER-07-H](../work-register.md#work-chapter-07-h), [LEVEL-061](../work-register.md#work-level-061), [LEVEL-062](../work-register.md#work-level-062), [LEVEL-063](../work-register.md#work-level-063), [LEVEL-064](../work-register.md#work-level-064), [LEVEL-065](../work-register.md#work-level-065), [LEVEL-066](../work-register.md#work-level-066), [LEVEL-067](../work-register.md#work-level-067), [LEVEL-068](../work-register.md#work-level-068), [LEVEL-069](../work-register.md#work-level-069), [LEVEL-070](../work-register.md#work-level-070), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-08"></a>
### CHAPTER-08 · Reconcile chapter 08

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-08).


 **Exact closure members:** [CHAPTER-08-H](../work-register.md#work-chapter-08-h), [LEVEL-071](../work-register.md#work-level-071), [LEVEL-072](../work-register.md#work-level-072), [LEVEL-073](../work-register.md#work-level-073), [LEVEL-074](../work-register.md#work-level-074), [LEVEL-075](../work-register.md#work-level-075), [LEVEL-076](../work-register.md#work-level-076), [LEVEL-077](../work-register.md#work-level-077), [LEVEL-078](../work-register.md#work-level-078), [LEVEL-079](../work-register.md#work-level-079), [LEVEL-080](../work-register.md#work-level-080), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-09"></a>
### CHAPTER-09 · Reconcile chapter 09

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-09).


 **Exact closure members:** [CHAPTER-09-H](../work-register.md#work-chapter-09-h), [LEVEL-081](../work-register.md#work-level-081), [LEVEL-082](../work-register.md#work-level-082), [LEVEL-083](../work-register.md#work-level-083), [LEVEL-084](../work-register.md#work-level-084), [LEVEL-085](../work-register.md#work-level-085), [LEVEL-086](../work-register.md#work-level-086), [LEVEL-087](../work-register.md#work-level-087), [LEVEL-088](../work-register.md#work-level-088), [LEVEL-089](../work-register.md#work-level-089), [LEVEL-090](../work-register.md#work-level-090), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-10"></a>
### CHAPTER-10 · Reconcile chapter 10

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-10).


 **Exact closure members:** [CHAPTER-10-H](../work-register.md#work-chapter-10-h), [LEVEL-091](../work-register.md#work-level-091), [LEVEL-092](../work-register.md#work-level-092), [LEVEL-093](../work-register.md#work-level-093), [LEVEL-094](../work-register.md#work-level-094), [LEVEL-095](../work-register.md#work-level-095), [LEVEL-096](../work-register.md#work-level-096), [LEVEL-097](../work-register.md#work-level-097), [LEVEL-098](../work-register.md#work-level-098), [LEVEL-099](../work-register.md#work-level-099), [LEVEL-100](../work-register.md#work-level-100), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-11"></a>
### CHAPTER-11 · Reconcile chapter 11

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-11).


 **Exact closure members:** [CHAPTER-11-H](../work-register.md#work-chapter-11-h), [LEVEL-101](../work-register.md#work-level-101), [LEVEL-102](../work-register.md#work-level-102), [LEVEL-103](../work-register.md#work-level-103), [LEVEL-104](../work-register.md#work-level-104), [LEVEL-105](../work-register.md#work-level-105), [LEVEL-106](../work-register.md#work-level-106), [LEVEL-107](../work-register.md#work-level-107), [LEVEL-108](../work-register.md#work-level-108), [LEVEL-109](../work-register.md#work-level-109), [LEVEL-110](../work-register.md#work-level-110), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-12"></a>
### CHAPTER-12 · Reconcile chapter 12

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-12).


 **Exact closure members:** [CHAPTER-12-H](../work-register.md#work-chapter-12-h), [LEVEL-111](../work-register.md#work-level-111), [LEVEL-112](../work-register.md#work-level-112), [LEVEL-113](../work-register.md#work-level-113), [LEVEL-114](../work-register.md#work-level-114), [LEVEL-115](../work-register.md#work-level-115), [LEVEL-116](../work-register.md#work-level-116), [LEVEL-117](../work-register.md#work-level-117), [LEVEL-118](../work-register.md#work-level-118), [LEVEL-119](../work-register.md#work-level-119), [LEVEL-120](../work-register.md#work-level-120), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-13"></a>
### CHAPTER-13 · Reconcile chapter 13

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-13).


 **Exact closure members:** [CHAPTER-13-H](../work-register.md#work-chapter-13-h), [LEVEL-121](../work-register.md#work-level-121), [LEVEL-122](../work-register.md#work-level-122), [LEVEL-123](../work-register.md#work-level-123), [LEVEL-124](../work-register.md#work-level-124), [LEVEL-125](../work-register.md#work-level-125), [LEVEL-126](../work-register.md#work-level-126), [LEVEL-127](../work-register.md#work-level-127), [LEVEL-128](../work-register.md#work-level-128), [LEVEL-129](../work-register.md#work-level-129), [LEVEL-130](../work-register.md#work-level-130), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-14"></a>
### CHAPTER-14 · Reconcile chapter 14

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-14).


 **Exact closure members:** [CHAPTER-14-H](../work-register.md#work-chapter-14-h), [LEVEL-131](../work-register.md#work-level-131), [LEVEL-132](../work-register.md#work-level-132), [LEVEL-133](../work-register.md#work-level-133), [LEVEL-134](../work-register.md#work-level-134), [LEVEL-135](../work-register.md#work-level-135), [LEVEL-136](../work-register.md#work-level-136), [LEVEL-137](../work-register.md#work-level-137), [LEVEL-138](../work-register.md#work-level-138), [LEVEL-139](../work-register.md#work-level-139), [LEVEL-140](../work-register.md#work-level-140), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="chapter-15"></a>
### CHAPTER-15 · Reconcile chapter 15

Aggregate chapter teaching/qualification gate; not a new feature. **Source outcome:** All ten levels, individual introductions/practice/reuse, prerequisites and feedback corrections current; family reservation is not per-mode teaching proof.

[Complete criteria/status/technical predecessors](../work-register.md#work-chapter-15).


 **Exact closure members:** [CHAPTER-15-H](../work-register.md#work-chapter-15-h), [LEVEL-141](../work-register.md#work-level-141), [LEVEL-142](../work-register.md#work-level-142), [LEVEL-143](../work-register.md#work-level-143), [LEVEL-144](../work-register.md#work-level-144), [LEVEL-145](../work-register.md#work-level-145), [LEVEL-146](../work-register.md#work-level-146), [LEVEL-147](../work-register.md#work-level-147), [LEVEL-148](../work-register.md#work-level-148), [LEVEL-149](../work-register.md#work-level-149), [LEVEL-150](../work-register.md#work-level-150), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s747"></a>
### S747 · Audit retained scope: Execution preparation — domain contracts before each domain implementation

Aggregate closure over named current criteria; not a fresh feature. **Source outcome:** Execution preparation — domain contracts before each domain implementation. For every required process, record the chosen gameplay model and intentional fidelity limits, units/sign conventions, valid parameter ranges, state ownership, finite sources/sinks, coupling order, timestep/error/convergence limits, failure behaviour,… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-747).


 **Exact closure members:** [S257](../work-register.md#execution-step-257), [S416](../work-register.md#execution-step-416), [S470](../work-register.md#execution-step-470), [S484](../work-register.md#execution-step-484), [S528](../work-register.md#execution-step-528), [S543](../work-register.md#execution-step-543), [S605](../work-register.md#execution-step-605), [S635](../work-register.md#execution-step-635), [S016](../work-register.md#execution-step-016), [S017](../work-register.md#execution-step-017), [S018](../work-register.md#execution-step-018), [S019](../work-register.md#execution-step-019), [S020](../work-register.md#execution-step-020), [S021](../work-register.md#execution-step-021), [S022](../work-register.md#execution-step-022), [S023](../work-register.md#execution-step-023). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.

<a id="s752"></a>
### S752 · Audit retained scope: PERF-03c — simplify collision geometry without changing functional shape

Aggregate per-geometry audit acceptance; see exact scope correction. **Source outcome:** PERF-03c — simplify collision geometry without changing functional shape. For each audit record, reduce unnecessary hull vertices and compound pieces with explicit surface/clearance error limits. Preserve support faces, thin walls, stops, moving blades, seams and open interiors of pipes, bends, funnels and baskets. Do not… Every source criterion maps to current passing assertions/artifacts; preserve failures, full scope and individual publication.

[Complete criteria/status/technical predecessors](../work-register.md#execution-step-752).


**Current executable disposition:** [exact question/children and finite stop](scope-corrections.md#s752).

<a id="clean-final"></a>
### CLEAN-FINAL · Audit final code and context quality after all product changes

Supporting enforcement or closure gate; not a product story. **Source outcome:** Reconcile the union of every migration's removed paths, current dependency/build graph, typed boundaries and active runbooks. Zero production compatibility paths, aliases, duplicate implementation ownership, obsolete assets or stale next-action instructions; original task/history scope preserved.

[Complete criteria/status/technical predecessors](../work-register.md#work-clean-final).

 **Exact closure members:** [ART-01](../work-register.md#work-art-01), [ART-02](../work-register.md#work-art-02), [ART-03](../work-register.md#work-art-03), [ART-04](../work-register.md#work-art-04), [CLEAN-ENFORCE](../work-register.md#work-clean-enforce), [LEVEL-001](../work-register.md#work-level-001), [LEVEL-002](../work-register.md#work-level-002), [LEVEL-003](../work-register.md#work-level-003), [LEVEL-004](../work-register.md#work-level-004), [LEVEL-005](../work-register.md#work-level-005), [LEVEL-006](../work-register.md#work-level-006), [LEVEL-007](../work-register.md#work-level-007), [LEVEL-008](../work-register.md#work-level-008), [LEVEL-009](../work-register.md#work-level-009), [LEVEL-010](../work-register.md#work-level-010), [LEVEL-011](../work-register.md#work-level-011), [LEVEL-012](../work-register.md#work-level-012), [LEVEL-013](../work-register.md#work-level-013), [LEVEL-014](../work-register.md#work-level-014), [LEVEL-015](../work-register.md#work-level-015), [LEVEL-016](../work-register.md#work-level-016), [LEVEL-017](../work-register.md#work-level-017), [LEVEL-018](../work-register.md#work-level-018), [LEVEL-019](../work-register.md#work-level-019), [LEVEL-020](../work-register.md#work-level-020), [LEVEL-021](../work-register.md#work-level-021), [LEVEL-022](../work-register.md#work-level-022), [LEVEL-023](../work-register.md#work-level-023), [LEVEL-024](../work-register.md#work-level-024), [LEVEL-025](../work-register.md#work-level-025), [LEVEL-026](../work-register.md#work-level-026), [LEVEL-027](../work-register.md#work-level-027), [LEVEL-028](../work-register.md#work-level-028), [LEVEL-029](../work-register.md#work-level-029), [LEVEL-030](../work-register.md#work-level-030), [LEVEL-031](../work-register.md#work-level-031), [LEVEL-032](../work-register.md#work-level-032), [LEVEL-033](../work-register.md#work-level-033), [LEVEL-034](../work-register.md#work-level-034), [LEVEL-035](../work-register.md#work-level-035), [LEVEL-036](../work-register.md#work-level-036), [LEVEL-037](../work-register.md#work-level-037), [LEVEL-038](../work-register.md#work-level-038), [LEVEL-039](../work-register.md#work-level-039), [LEVEL-040](../work-register.md#work-level-040), [LEVEL-041](../work-register.md#work-level-041), [LEVEL-042](../work-register.md#work-level-042), [LEVEL-043](../work-register.md#work-level-043), [LEVEL-044](../work-register.md#work-level-044), [LEVEL-045](../work-register.md#work-level-045), [LEVEL-046](../work-register.md#work-level-046), [LEVEL-047](../work-register.md#work-level-047), [LEVEL-048](../work-register.md#work-level-048), [LEVEL-049](../work-register.md#work-level-049), [LEVEL-050](../work-register.md#work-level-050), [LEVEL-051](../work-register.md#work-level-051), [LEVEL-052](../work-register.md#work-level-052), [LEVEL-053](../work-register.md#work-level-053), [LEVEL-054](../work-register.md#work-level-054), [LEVEL-055](../work-register.md#work-level-055), [LEVEL-056](../work-register.md#work-level-056), [LEVEL-057](../work-register.md#work-level-057), [LEVEL-058](../work-register.md#work-level-058), [LEVEL-059](../work-register.md#work-level-059), [LEVEL-060](../work-register.md#work-level-060), [LEVEL-061](../work-register.md#work-level-061), [LEVEL-062](../work-register.md#work-level-062), [LEVEL-063](../work-register.md#work-level-063), [LEVEL-064](../work-register.md#work-level-064), [LEVEL-065](../work-register.md#work-level-065), [LEVEL-066](../work-register.md#work-level-066), [LEVEL-067](../work-register.md#work-level-067), [LEVEL-068](../work-register.md#work-level-068), [LEVEL-069](../work-register.md#work-level-069), [LEVEL-070](../work-register.md#work-level-070), [LEVEL-071](../work-register.md#work-level-071), [LEVEL-072](../work-register.md#work-level-072), [LEVEL-073](../work-register.md#work-level-073), [LEVEL-074](../work-register.md#work-level-074), [LEVEL-075](../work-register.md#work-level-075), [LEVEL-076](../work-register.md#work-level-076), [LEVEL-077](../work-register.md#work-level-077), [LEVEL-078](../work-register.md#work-level-078), [LEVEL-079](../work-register.md#work-level-079), [LEVEL-080](../work-register.md#work-level-080), [LEVEL-081](../work-register.md#work-level-081), [LEVEL-082](../work-register.md#work-level-082), [LEVEL-083](../work-register.md#work-level-083), [LEVEL-084](../work-register.md#work-level-084), [LEVEL-085](../work-register.md#work-level-085), [LEVEL-086](../work-register.md#work-level-086), [LEVEL-087](../work-register.md#work-level-087), [LEVEL-088](../work-register.md#work-level-088), [LEVEL-089](../work-register.md#work-level-089), [LEVEL-090](../work-register.md#work-level-090), [LEVEL-091](../work-register.md#work-level-091), [LEVEL-092](../work-register.md#work-level-092), [LEVEL-093](../work-register.md#work-level-093), [LEVEL-094](../work-register.md#work-level-094), [LEVEL-095](../work-register.md#work-level-095), [MOBILE-01](../work-register.md#work-mobile-01), [MOBILE-02](../work-register.md#work-mobile-02), [MOBILE-03](../work-register.md#work-mobile-03), [MOBILE-04](../work-register.md#work-mobile-04), [MOBILE-05](../work-register.md#work-mobile-05), [MOBILE-06](../work-register.md#work-mobile-06), [P0-035](../work-register.md#work-p0-035). These are coverage/criterion references, not new scheduling prerequisites; original technical stages control readiness.
