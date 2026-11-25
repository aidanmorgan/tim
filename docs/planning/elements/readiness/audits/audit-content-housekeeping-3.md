# Audit: content campaign housekeeping contract (round 3)

Independent, context-free re-audit of `_bmad-output/implementation-artifacts/spec-content-campaign-housekeeping.md` on `2ce73eed`, 24 November 2026, after the repairs from [round 1](audit-content-housekeeping-1.md) and [round 2](audit-content-housekeeping-2.md). Nothing in the repository was edited, built or run.

## Scope read

**Contract.** Untracked, 4,055 words, so within the 6,000-word planning budget. Rounds 1 and 2 were both read.

**Authorities read:**
- `AGENTS.md`
- `owner-decisions-2026-11-22.md` decisions 22, 24d–24f, 24m and 41
- `campaign-coverage.md` sections 2, 5 (`:95–192`), 6 (`:288`, `:303–304`) and 9 (`:559–564`)
- `completion-ledger.md:365–400`
- `deferred-work.md:357–363`
- `delivery-batches.md` §4 (`:203–236`) and `:352`
- `bmad-loop-protocol.md` (Review tiers, Slice record, contract budget)
- `docs/delivery-workflow.md#new-package-registration-check`
- `docs/testing-guidance.md`
- `TODO.md:1–12`

**Code read:**
- `PuzzleContent.cs:26–52`, `PuzzleCatalogue.cs:8–16`
- `CurrentOwnerMembership.cs`, `CurrentOwnerMembershipOracles.cs:50–75`, `Program.cs:15–19`
- `CurrentCatalogue.cs:95–124` and `:185–200`, `CurrentOwnerRelations.cs`
- `CurrentCatalogueTests.cs:50–62` and `:110–126`, `CurrentCapabilityInputsTests.cs:26–75`, `CurrentOwnerMembershipTests.cs:30–60`
- `TimingDeclarationTests.cs:124–131`
- `.githooks/pre-commit`, `tools/anvil/CheckStagedTests.cs` (head)

**Lanes read** (every line that names a path this slice owns):
- `spec-elements-visible-solid-remainder-b`, `-b2` and `-c`
- `spec-repair-baseball-el-239`
- `spec-s5*` and `spec-s6*`
- `spec-policy-tool-fixes`

**Candidate built for measurement.** A Python filter wrote the candidate to the scratchpad. It deletes the 29 ids from section 5 (`:104–132`), writes each kept level's plan-level prefix from `:159–192`, and sets the two titles. All numbers below come from it. Diff counts were measured with `git diff --no-index` on git 2.56.1; no `diff.algorithm` is configured and there is no `.gitattributes`. A throwaway-repository run was refused by the sandbox.

**Recomputed; these match the contract:**
- The no-op serialisation reproduces the file exactly (`cmp`).
- 34 kept levels: 15 Playable and 19 Planned.
- 33 subtitles change (`first_principles` is already `01`).
- Titles are pairwise distinct.
- The Playable order by `presentation.order` is unchanged.
- AC4 as written exits 0 with no output.
- Fixtures: 78 Planned parts + 44 Playable placements = 122, from 304. 182 `FIX-` rows are removed.
- `rg -w` over `current-consumers.md` finds 196 hits: the 182 rows plus the 14 lists.
- Census recount: 545 (61/29/197/8/28/72/150), so 566 after adding 21.
- 21 lettered levels.
- AC3's `git grep -h -c` prints 1. AC5's anchor pattern matches 150 numbered entries now and 0 lettered.
- No `*.cs` or `*.ts` file, including `tools/e2e`, names a removed id.
- Per-part counts on the candidate:

| Part | Count |
|---|---|
| Battery | 9 levels (8 locked placements, 1 inventory) |
| Conveyor | inventory 4 |
| Fan | 5 levels, 2 placed |
| Lamp | 7 levels (2 Playable, 5 Planned) |
| Motor | 12 |
| Spring | inventory 3 (`spring_forward` + 2) |
| Switch | 8 placed, 4 inventory |
| Tennis ball | 5 |
| Bumper | inventory only, 3 |
| Wall | 2 |

## Round-2 findings

| Id | Status | Evidence |
|---|---|---|
| B1 | Resolved | All nine lines are in the owned element row (`:83`) with values that match the candidate: CAT-042 12; CAT-019:208 and EL-202 "49 and 58"; EL-199 "21, 22, 24, 49, 58"; EL-194:113 one instance; CAT-028 5/2; CAT-064 5; EL-196 eight, confirmed as locked placements. EL-209:120 now has its value. The document count is 22, the same at `:83` and `:181`. P3 (`:54`) covers `CAT-019:208` and `CAT-064:94`. CAT-005's "Level 14" is settled by D5 keeping authored-position labels (`:110`), not by restating the line. |
| N1 | Partly resolved | 33 subtitles, 35 fields and "about 554 KB" are now right (`:27`, `:52`, `:73`). The numstat expectation holds only under a non-default diff algorithm: see new B1. |
| N2 | Not resolved | AC6 (`:136`) now uses `--glob '!readiness/**'`. With ripgrep 16.0.0 and the search path `docs/planning/elements`, that glob still lists `readiness/campaign-coverage.md`, `readiness/completion-ledger.md` and the audits. `'!**/readiness/**'` and `'!docs/planning/elements/readiness/**'` do exclude them. See new B2. |
| N3 | Resolved | `:83` names the other 5 levels of `CAT-035:117` and points the domino change to DF-1. |
| N4 | Resolved | Never (`:29`) and D5 (`:110`) make `CAT-053:84`'s description cell the one exception. |
| N5 | Resolved | The H6 fact (`:165`) asserts 122 `FIX-` members and that neither `FIX-015-001` nor `CAT-015-V` is a member. |
| N6 | Resolved | P2 (`:53`) has the coordinator supply the merged total, the SnapshotApproval and the push receipt. |
| N7 | Resolved | `EL-209:87` is marked as a check (`:83`). `:90` now reads "plain `--oracles` passes no root", which matches `Program.cs:17`. |

## New findings

**B1 — Blocking: the numstat expectation depends on the diff algorithm (`:52`, `:176`).**
- On the candidate, git's default Myers algorithm reports `423	21491`.
- `--diff-algorithm=minimal`, `patience` and `histogram` all report `35	21103`.
- A correct filter would therefore fail U1's after-check as written, and the implementer would have to stop.
- **Fix:** in `:52`, write "`git diff --numstat --diff-algorithm=histogram -- content/puzzles.json` reports `35	21103`". Name the same flag in Verification `:176`.

**B2 — Blocking: AC6's exclusion glob does not exclude anything (carried from round-2 N2).**
- As written, AC6 prints these hits, none of which is in AC6's three allowed classes:
  - `campaign-coverage.md:104–132`, `:151`, `:562`
  - `completion-ledger.md:174`, `:297`, `:299`, `:308`
  - every saved audit report, this one included
- AC6 therefore fails mechanically.
- **Fix:** `rg -n -w '<29 removed ids>' --glob '!**/readiness/**' docs/planning/elements`. With this glob the remaining hits are the 13 element documents, whose hits are the owned lines or rows pinned to `@a6c914e` (`CAT-005:167`, `CAT-023:103`, `CAT-064:134`, `EL-190:62`, `EL-194:64`).

**B3 — Blocking: "fifteen composite springboard lanes" becomes false, and AGENTS.md's "Nothing else" scope rule (`:181`) blocks the repair.**
- On the candidate, one composite spring lane survives: `double_cold_start`'s `lane2_spring_1`. The other 14 are in removed levels. DF-3 later drops this lane too (`campaign-coverage.md:136`).
- Current-state statements that become false and are outside the owned scope:
  - `CAT-062-spring.md:66`: "(the fifteen composite spring lanes)"
  - `CAT-062-spring.md:153`: "the fifteen composite spring lanes"
  - `CAT-062-spring.md:174`: "and the fifteen composite lanes in `content/puzzles.json`"
- Requirement acceptance text:
  - `requirements.md:186` says "all fifteen composite springboard instances", and `requirements.md:5998` (EL-194) says "all fifteen composite lane instances".
  - `EL-194:19`, `EL-194:54` and `CAT-062:166` quote this text.
  - Once `EL-194:113` is restated to "one instance" (`:83`), EL-194 contradicts itself, and its criterion names 14 instances that no longer exist.
  - AGENTS.md says "Preserve every requirement ID and acceptance criterion", so the implementer cannot restate this text without a ruling.
- **Fix:**
  - Add `CAT-062-spring.md:66`, `:153` and `:174` to the owned element row and the Final diff scope, each restated as "one composite lane (`double_cold_start`, until DF-3); the other 14 were removed under 24d".
  - Add a precondition (coordinator, or owner if the acceptance wording changes) that rules how EL-194's criterion at `requirements.md:186` and `:5998` reads after 24d. Restate `EL-194:19` and `:54` and `CAT-062:166` to match.
  - Alternatively, state at `EL-194:113` that the requirement text is kept verbatim and that decision 24d closes the 14 removed instances, and record that in a ledger row through P2.
  - `readiness/batch-a.md:28` and `:53` say the same and may be restated in the same step.

**N1 — Non-blocking: `campaign-coverage.md:155` is contradicted and not owned.** It says "A relocated Playable level changes only its presentation order; `domino_effect` is the only Playable level whose content changes." After this slice, 14 Playable subtitles change and no `presentation.order` changes (D1, D2). **Fix:** add `:155` to the owned lines of `campaign-coverage.md`, restated so that placement is the subtitle plus the `LEVEL-` entry and picker order is DF-2.

**N2 — Non-blocking: P3's fallback is not carried through.**
- P3 (`:54`) lets the coordinator hand `CAT-019:67/:147/:160/:208` and `CAT-064:94` to lane 1.
- In that case `:83`, `:181` and AC6 still count them as this slice's. `CAT-019:147` and `:160` (`depth_delivery`) and `CAT-064:94` would then fail AC6's review clause.
- **Fix:** in AC6 and `:181`, write "lines handed to lane 1 under P3 are excluded and listed in the run log".

**N3 — Non-blocking: citations in Running beside other work.**
- `:185` says this slice edits CAT-019 "`:67`, `:147`, `:160` only (P3)". `:54` and `:83` also include `:208`.
- `:186`: part C's "placement 62" is a 0-based array index (`burst_of_motion` is index 62), so it becomes 33, not 34.
- **Fix:** add `:208` at `:185`, and write "becomes 33" at `:186`.

**N4 — Non-blocking: the `campaign.md:1` title value is unspecified.**
- The title says "1800 retained placement cells", which is 450 groups × 4.
- The header (`:75`) moves to 513 groups, so the cells become 2,052. The owned row only says "lettered entries named".
- **Fix:** state the restated title count (2,052 cells, 513 groups), or state that 1800 stays and refers to the numbered slots.

**N5 — Non-blocking: numbering differs between documents after the slice (D5).**
- After the slice, content subtitles and `delivery-batches.md` §4 use plan levels (for example 12 `battery_motor`).
- The element lines restated under D5 keep unlabelled authored positions ("14 `battery_motor`", "Level 14 needs the `turned` goal kind", `CAT-005:238`), and they link to §4.
- **Fix:** in each restated element line, mark the numbers once as authored positions, for example "Planned (authored positions): 14 `battery_motor` …".

**Checked and sound:**
- Preconditions U1, P2, P3 and O1 each state the owner, the content and the stop condition. U1 states that the slice does not start without permission, and O1 leaves the slice independent of the answer.
- The engine tier is justified by the tool-source literal and the tool test edits.
- H1–H6 tests:
  - They read real files through production readers: `PuzzleContentReader` is already used from `CuriousContraptions.tests`, and `Coverage.Tests` references Ownership transitively.
  - Their expectations are transcribed from section 5.
  - They use only set, map, string and count assertions, with no hash or float.
  - The baseline fails each one.
  - AC1's +6 equals 4 tests with 5 cases, plus 1.
- Existing real-tree tests (`ALetteredLevelHeadingInTheCampaignDocumentIsADeclaredOwner`, `DeclaredLevelsExist`, `PipeBend90Tests.cs:190`) are unaffected.
- Forward-only: the removed content and identities leave no alias, placeholder or fallback. The marked checks at `a6c914e` are evidence, not compatibility.
- Disjointness otherwise holds:
  - Lane 1 B, B2 and C, and the Baseball repair, only read content or cite lines that this slice leaves in place (`CurrentCatalogueTests` and `CurrentCapabilityInputsTests` literals are edited in place, with no line shift).
  - S6d reads goal-kind text only.
  - S6e's `CAT-005:93` re-read is a different line from `:238`.
  - Lane 4's re-measurement rule is stated.
- Other sweep results:
  - `requirements.md:2519` and `:4088` (*The Grand Contraption*, level 150) and `epics.md:5485` ("`wind_signal` is removed") stay true.
  - Every other removed-id hit in `docs/planning/invest` is in owned files.

Verdict: Incomplete
