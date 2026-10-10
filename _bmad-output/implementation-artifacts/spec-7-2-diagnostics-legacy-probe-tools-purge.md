---
title: 'Story 7.2: Diagnostics and legacy probe tools purge'
type: 'chore'
created: '2026-10-10'
status: 'done'
route: 'dispatch'
review_loop_iteration: 1
source_commit: 'aaac712a5b49f6ce8762ef2b6fcb3372d426d0d5'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/planning/elements/legacy-disposition.md'
  - '{project-root}/_bmad-output/implementation-artifacts/research-epic-7-legacy-inventory.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Obsolete probe and diagnostic tools remain after the independently verified Story 7.1 reference purge.

**Approach:** Delete only Story 7.2's ledger-covered tools, diagnostics and unused geometry library; update active command references and prove the remaining tools compile without warnings.

## Boundaries & Constraints

**Always:** Match all 24 ledger rows and 129 tracked paths before deletion. Current inventory is 1,431 regular files, 325,211,436 bytes: 129 tracked and 1,302 ignored bin/obj outputs, with no untracked files or symlinks. Include the empty untracked `tools/p0-002-review/`. Preserve Git history and all baseline-pinned element citations. Owner already authorized Campaign and Playtest deletion. Retain all working tools, including Coverage.Tests and the new console helper. Use Anvil before every write/delete, the same independent reviewer and exact scoped commit approval; local commit only.

**Never:** Delete Stories 7.3/7.4 sources or tests, local archives, unrelated scratch/diagnostics, Battery decisions or review receipts; change gameplay, remove requirement IDs, weaken proof or rebuild a compatibility tool. No push.

## I/O & Edge-Case Matrix

| State | Action |
| --- | --- |
| Exact ledger scope; generated outputs only; no active consumer | Delete the reviewed set |
| New untracked source, symlink, unmatched path or changed bytes | Stop and reconcile before removal |
| Remaining consumer or warning found | Fix the bounded dependency or record a blocker; never claim Pass |

</frozen-after-approval>

## Code Map

- `legacy-disposition.md` §7.2: exact 24 roots and tracked counts; source acceptance is epics Story 7.2.
- Delete `tools/p0-*`, `tools/P0-007-*`, Campaign, Playtest, Performance, Performance.Tests, PortableGeometryProof, LifecycleContractReview, GpuBodyFixture, `diagnostics/` and `CuriousContraptions.Geometry/` only as enumerated by the ledger.
- Keep/build seven projects: Coverage, Coverage.Tests, Ownership, Preview, LifecycleContract, WireContract, TraceAllocations. Keep `tools/anvil`, `tools/e2e` and all current workshop Node harness/helper files.
- Current solution/project/script/config search found no dependency outside the deletion roots. Runtime and publication inputs remain unchanged.
- `docs/README.md` and `docs/delivery-workflow.md:181`: remove obsolete tool command pointers. Requirements PERF-02's old diagnostic-tool naming needs current wording while preserving every metric/scenario and criterion.
- TODO, sprint, epics, roadmap and ledger: record scoped outcome. Coverage/Ownership legacy policy cleanup remains Story 7.4.

## Tasks & Acceptance

**Execution:**
- [x] Reconcile exact files, ignored classification, source drift and current consumers; independent plan review before deletion.
- [x] Apply Anvil-gated bounded removals and active documentation corrections.
- [x] Build kept projects and check remaining tools with Anvil; run affected tests.
- [x] Freeze one scoped snapshot/diff; independent review, local commit and committed-tree verification.

**Acceptance Criteria:**
- Given `tools/p0-*`, `tools/P0-007-*` and dead diagnostic folders, when LEGACY-0b is applied, then approximately 2,000 obsolete files are purged. Preserve this source criterion; actual current count is 1,431, not an invented 2,000.
- Remaining tools compile with zero warnings under Anvil.
- No current runtime/tool dependency or active link breaks; source knowledge and unrelated work remain preserved.

## Implementation Notes

Original 23-root deletion is complete after exact byte recheck and individual Anvil gates; the added GpuBodyFixture scope passed expanded independent approval and fresh byte equality before its Anvil-gated deletion. Inventory raw commands: a181bc/87a039; same reviewer independently counted 1,166 files. Story 7.1 committed `aaac712` with independent terminal Pass. No new owner intent gap found; exact file count corrects the historical estimate. All ignored candidates are regenerable outputs, so no new untracked-source archive is needed.

## Spec Change Log

10 Oct: current-tool compilation exposed GpuBodyFixture's stale f16 producer/consumer. Direct review established it is a retired GPU integration/readback experiment, not a current WASM SIMD fixture; coordinator authorized correcting the inventory classification and adding its five tracked files plus 260 generated outputs. No replacement solver or compatibility path. Preserve current CanonicalBody and shared WGSL at its existing later gate. Expanded deletion requires independent entry approval and fresh byte identity.

## Review Triage Log

F1 resolved by obsolete fixture retirement, not a compatibility repair. Seven retained projects build in Release with warnings as errors and zero warnings; remaining-tool Anvil scan passed. Exact removals: 1,431 files (129 tracked), 325,211,436 bytes; all nontracked files were ignored bin/obj outputs. Inventories: `.anvil/story-7-2-deletions.json` (`acf4ab42377e7ae23063b0f887c79ff308cf82f441429facf4fa430d17e30fbf`) and added fixture inventory (`d52505b27c418c75fe866294672fc7bf7e6b59b9c92191a881b57b0d4ef7fd94`). Raw checks/builds: `.anvil/story-7-2-{checks,builds}.json`; Coverage build raw38e4b9/d22f76, Node summary raw821b5c. Initial Node/contract console captures were truncated; complete contract outputs were recaptured, Node terminal85/85summary retained. Two long JSON result lines exceeded Anvil scanning length. Initial fixture restore's cached NU1900 cleared with force-evaluate restore; subsequent compile errors exposed the obsolete classification. No warnings or checks were suppressed. Independent snapshot and committed-tree approval remain the final gates.

## Verification

- Build each of the seven kept projects: `dotnet build <project> -c Release --nologo -warnaserror`.
- Execute built Coverage.Tests DLL with `-noColor` (40/40), LifecycleContract (510 cases) and WireContract (109 cases); workshop Node suites (85/85). `dotnet test` does not discover this standalone xUnit executable, so direct execution is required.
- Anvil scan remaining tool source files; exact deletion membership, active references and Git-pinned source preservation checks; `git diff --check`.
- Reuse Story 7.1 production/unit/Chrome proof only after reviewer checks unchanged relevant inputs. Do not repeat full Chrome or rebuild the runtime without changed inputs or an unresolved concern.
