# P0-006 — lifecycle, clocks and crash recovery

Stage: Design. R1 received [independent Fail](../../verification/P0-006/review-r1.md).
R2 corrects its stream accounting, interval uncertainty and enum findings; independent re-review
and publication remain pending. Implementation owner /root/p0_006, session
01a0f87c-eef8-7f41-bbda-011b5a5733c1. Coordinator /root,
session 01a0f5f1-ab9a-79a3-9181-0dbb87285ec5.
Base: 854bbc71dc528c0e083389ffbabc3d5df71423f6.
Current verdict belongs to the independent review record; this frozen contract is not a runtime pass.

## Scope and prerequisites

[Order 6](../../../TODO.md#work-p0-006) follows independently published
[P0-005](../../verification/P0-005/review.md), including R4 query-terminal and
wavefront Presented corrections. [P0-004](../P0-004/assemblies.md) owns assembly/state
boundaries; its [spatial contract](../P0-004/spatial-authority.md) keeps query metadata
authoritative. [P0-003](../P0-003/devices-budgets.md) owns absolute budgets.
The [primary bridge design](../../simulation-presentation-bridge.md) remains binding.
These decisions refine lifecycle/timing within P0-005's existing fields, not a new message,
wire version, alternate runtime or storage framework.

Allowed changes: this work-order directory; docs/verification/P0-006 implementation
evidence; standalone tools/LifecycleContract design fixtures; exactly TODO's handoff
and P0-006 status. TODO, AGENTS and existing dirty runtime are excluded from publication.
No game source, content or served bundle changes. tools/**/*.cs is excluded from the game.
No browser build or UI action can establish additional Design proof; affected runtime
build, Chrome/Playwright, exact restoration and performance gates remain with their named children.

[Lifecycle](lifecycle.md) fixes exhaustive modes, input/error outcomes and completion.
[Clocks](clocks.md) fixes numerical mappings, history capacity and overload.
[Mutation inventory](mutations.md) names current owners and every mutation family.
[Failure oracles](oracles.md) specifies finite independent before/after expectations.
[Source/impact](source-impact.md) identifies inspected source gaps and successor ownership.
The small executable checks schema rejection, the finite command/mode matrix and numerical
goldens. It is deliberately not a production lifecycle/clock implementation.

## Required-now criteria

| Criterion | Exact acceptance |
| --- | --- |
| P0-006/A01 | Actual source/transitive-consumer reconciliation, distinct implementation/reviewer identities, prerequisite verdicts and one immutable relevant-input snapshot. |
| P0-006/A02 | Every lifecycle mode × all 16 command kinds has an explicit result/transition; transport admission, authority commit, result receipt, renderer completion and durable save are distinct. |
| P0-006/A03 | Failed Load/Run and successful Reset/save/navigation cover authority and browser construction/settings/selection state; construction-only save, no runtime save/recovery replay. |
| P0-006/A04 | Every current mutation family has commit/rollback owner, exact mapping/free-list/command/event/RNG disposition and named implementation/proof children; no renderer authority. |
| P0-006/A05 | Numerical clock mapping/error/expiry, dual physical/wall axes, history depth/byte quotas, interpolation, hidden/stall/overload and process recovery limits are complete and consistent with P0-003/005. |
| P0-006/A06 | Finite independent failure/restoration oracles cover pre/post commit, restore failure, lost result, stale reused IDs/leases and recipient failure; no false exactly-once crash claim. |
| P0-006/A07 | Standalone Release fixture build/checks, negative schema rejection, full current cheap coverage audits, links, enum review and exact TODO/source/bundle preservation pass. |
| P0-006/A08 | Independent criteria/impact/applicability review resolves all required-now findings and approves the exact snapshot. |
| P0-006/A09 | Publication-dependent only: implementation owner publishes approved allowlist; reviewer verifies parent, exact blobs, remote main and terminal Pass. |

A09 alone awaits publication. No deployed artifact is produced by this Design scope;
publication receipts do not waive later production-origin checks.
Unknown coverage is Incomplete; violated criteria Fail. Retained original failures remain immutable.

## Named implementation and proof

P0-008: candidate compiler, typed authored identity/content, full construction replacement.
P0-013/014: command/core boundary, transaction/publication reservation and exact topology state.
P0-015/016: admission/replay and exact codecs/transfer leases.
P0-019: simulation worker scheduler and watchdog integration.
P0-020: asynchronous lifecycle, host/browser coordinator, atomic save/load/navigation.
P0-022/023/024: retained AnimationBatch extraction, feedback and independent worker lifecycle.
P0-025: clock calibration and bounded coherent histories.
P0-026: final browser property/resource ownership and exact baseline restoration.
P0-029: actual built worker deployment.
P0-030/031: every existing consumer/fixture; no representative-only closure.
P0-032: real failure injection and recovery through Chrome/Playwright, including unexpected-input investigation.
P0-033/034/035: optimization, complete physical-device/workload qualification and mandatory engine gate.
P0-014/020/032 execute every failure oracle here against runtime code, not this fixture model.

Reuse SimulationTransaction's unique participant enrollment/reverse restore, SimulationTimers'
existing arrays, AnimationBatch's existing versions/free/active storage and BodyBoundsTree's
specialized hierarchy. No ECS prerequisite, duplicate authoritative store, fallback or migration.
Closed state/mode/reason choices become enums; stable extensible identities retain the typed
P0-005 IDs. No domain enum names are parsed or used as internal string selectors.

Native 66/76 retains ten failures; physical/combined Chrome p95 83.6/83.7 ms and compliant
production p95 150.1 ms fail. Unexpected input remains unattributed. Actual worker contexts,
CPU/GPU/memory/presentation instrumentation and unavailable physical devices remain open.
All 150 campaign levels and all catalogue variants remain required. These choices are
engineering targets and design oracles, not measured browser qualification.
