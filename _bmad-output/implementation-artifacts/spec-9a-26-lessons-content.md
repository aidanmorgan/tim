---
title: "9A.26 — Lessons and puzzles as content data"
status: in-review
baseline: 484ec38f726474d61f59a68730604f5bd600a5fc
context:
  - TODO.md
  - _bmad-output/planning-artifacts/epics.md
  - docs/architecture/element-package-isolation.md
  - docs/gpu-f32-physics.md
  - content/puzzles.json
---

# Outcome and scope

Preserve all twelve currently playable lessons while constructing them from typed content, with an extensible puzzle identity, placements, declared parameters, ordered inventory, assistance and goals. This includes JoinedPipe and FirstPrinciples in addition to the eight explicitly named lesson files. Preserve all 62 authored content rows and their difficulty/solution evidence; unavailable future content stays explicitly unavailable, never silently playable. No version, old loader, identity switch or migration bridge.

The exact source acceptance is Story 9A.26 in epics.md. Existing working C# lesson behavior is the preservation baseline where unused JSON disagrees (notably DominoEffect); reconcile those current rows explicitly, without claiming both baselines were identical. Preserve existing numeric admission/assistance behavior; shared Half assistance/goal/collider work remains owned by9A.44.

# Required behavior

- Typed content load rejects duplicate/missing/unknown fields and identities, unknown parts/parameters, nonfinite/out-of-range values and foreign goal references before construction mutation, naming the puzzle and field.
- Current placements, locks, inventory ordering/counts, compiled geometry/materials, goals and solutions survive the conversion. Free Workshop remains unrestricted by authored lessons.
- Precision updates retain capture/guide changes. A player-placed goal receiver receives its declared assistance and updates goal target/sensor without a lesson-id branch. Locked fixture tampering, unsupported/excess inventory and forged save goals still reject.
- UI selection, hints and precision controls read content. Save/wire carry the extensible identity and reject unsupported old input; no numeric-id alias.
- Existing real simulation behavior, meaningful negative controls, Reset and save/load remain verified. Browser suites cat-015a-sidekick,cat-015b,cat-062,cat-062b,cat-023b,cat-048b,cat-049,cat-050 remain pending the post-catalogue gap-finding phase.

# Tasks

- [x] Capture independent prechange evidence for all12 current lessons at authored precision knots: placements/settings, ordered inventory, assistance/goals and compiled rows; retain existing native solution tests.
- [x] Extend existing content with typed playable admission and one current generic lesson reader/constructor/validator; retain future rows and reject their runtime loading until supported.
- [x] Replace closed WorkshopPuzzleId and all lesson-specific dispatch in construction, inventory, UI, hints, precision and save/wire.
- [x] Delete BumperAdvanced.cs, BumperSidekick.cs, SpringboardLessons.cs, DominoEffect.cs, DelayedSignal.cs, ClearPipe.cs, GentleBend.cs, QuarterBend.cs and obsolete uid/includes/callers/fixtures; remove equivalent FirstPrinciples/JoinedPipe executable authored helpers and superseded single-fixture resource loader.
- [x] Align current design: data owns lesson identities and declarations; shared engines own behavior; no new element recognition, ratchet debt, compatibility path or unrelated numeric migration.
- [x] Prove production C# UNIT behavior as PRIMARY evidence: lesson solutions and controls, bounds/atomic rejection, difficulty changes, placement/rebinding, ticks, Reset/save-load. Integration is supplemental; no Chrome until all catalogue elements are complete.
- [ ] Build affected closure once per changed inputs/config, run matching tests --no-build --no-restore; include schema/generated callers, worker and actual compiler-input resource guard when changed. Freeze and obtain retained independent SnapshotApproval before protected publication.

# Current state and evidence

9.5b prerequisite independently passed9155c8: exact484ec38, CI39668936780 attempt2,2086/2086, artifact12379562697, deployment7236728459. Original attempt1 unexplained cross-project reference failure remains in its review receipt, not claimed fixed. Carry that terminal receipt into this publication.

The current path loads twelve Playable definitions and preserves fifty Planned rows without exposing unavailable parts. Typed PuzzleId replaces the closed enum; one strict reader validates complete declarations before exposure, one current codec constructs existing instance types, and generic catalogue operations own validation, precision, goal rebinding and presentation. Nine executable lesson files plus uid companions and the superseded single-fixture reader/resource are deleted. The isolation known list shrinks by452 stale entries; no new debt or element recognition.

Prechange capture8cc030 retained36 exact cases (12 lessons × precision0/.45/1) in [the immutable fixture](../../CuriousContraptions.tests/Fixtures/lessons-484ec38.json). Current geometry/materials, ordered inventory, profiles and goals match (f5b23d and finalbf244e). Existing native lesson solutions, Reset/save/load and placement controls remain covered by the affected355-case run8a99bf:352 passed; three failures were subsequently corrected. Delay inventory assistance lost authored difficulty metadata and was restored from the retained solution knots; quarter-bend still read the retired JSON shape; unsupported Wall inventory now correctly uses shared InventoryUnsupported (count overflow remains InventoryExceeded). Earlier342-case rundb99a4 had325 passes and17 failures from retired JSON assertions, authored-settings classification and missing empty-catalogue rejection; none is hidden. Shared authored settings now refuse PuzzleSettings, while altered fixed placements retain FixedPart.

Final changed-case run358aca passed39/40 including all21 isolation controls, malformed load and forged placed-receiver configuration. Its remaining Delay failure exposed an unapplied metadata correction; after the actual saved correction, bf244e passed10/10 (Delay metadata,36-case golden and schema-format controls). New controls exercise unknown/missing/duplicate fields, unknown parts/parameters, ranges, invalid enums, foreign goals, empty/all-Planned catalogue, and forged receiver capture/force bounds/support/participation through actual construction/save admission. Valid adjusted placement preserves pose and goal rebinding. No blanket355/355 claim is made.

Matching Debug build9a29c1:0 warnings/errors,8.32s; Release worker e734a7:0 warnings/errors,14.29s after the new worktree's missing-assets restore. Actual original Debug/Release binlogs /tmp/9a26-final-native.binlog and /tmp/9a26-worker-release.binlog passed existing compiler-input/resource guardc71735 (1/1,386ms). Strict TypeScript6.0.3 b36786 passed with existing cached Node types; earlier compiler discovery attempts lacked the proper cached version/types, not source fixes. Generated schema uses pinned flatc25.2.10; current string identity has no numeric alias. Deletion/whitespace scan1f61fe is clean. C# unit tests are primary; integration supplemental and browser qualification remains deferred as above.

Domino's current placements and goal deliberately follow the independently captured production baseline rather than the discrepant old unused JSON layout. Future authored rows and historical solution evidence remain intact; this is not a claim that the two old baselines matched. Common Half assistance/goal/collider lanes remain9A.44.

Final reviewer finding: picker identity allocation could overflow after an accepted submission. The picker now preflights and reserves the next ID before constructing/submitting, reports typed IdentityExhausted, and assigns the allocator only after acceptance. Actual native picker boundary1c5b28 passes1/1 (46ms), preserving construction/allocator/Undo and sending no command on exhaustion. Matching Debug3111f8 has0 warnings/errors (12.24s); unchanged worker/schema/TypeScript proof remains applicable.

Next: retained independent exact SnapshotApproval, normal protected publication, then exact remote/CI/artifact/deployment verification. No scoped runtime failure remains known; reviewer applicability and final verdict are pending.
