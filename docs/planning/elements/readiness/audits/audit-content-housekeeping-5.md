# Audit: content campaign housekeeping contract (round 5)

Independent re-audit of `_bmad-output/implementation-artifacts/spec-content-campaign-housekeeping.md`, 4,536 words. I audited it on `2ce73eed` plus the uncommitted owner decision 24o and the ledger edits, on 24 November 2026. Nothing in the repository was edited, built or run. Earlier rounds: [1](audit-content-housekeeping-1.md), [2](audit-content-housekeeping-2.md), [3](audit-content-housekeeping-3.md), [4](audit-content-housekeeping-4.md).

## Scope read

- **Contract and earlier reports:** the contract in full and the round-4 report.
- **Authorities:**
  - `AGENTS.md`
  - owner decisions 22, 24d–24f, 24m and 24o (`owner-decisions-2026-11-22.md:70`), and coordinator decision 41
  - the uncommitted `completion-ledger.md` diff: PR-08, PR-24 and OS-01 to OS-10
  - `campaign-coverage.md` section 5 and the owned lines
  - `campaign.md:1`, `:9`, `:143–156`
  - `delivery-batches.md` §4 and `:346–352`
  - `deferred-work.md:357–363` and `TODO.md:8`
  - `bmad-loop-protocol.md`: Review tiers, Slice record and Contract budget
  - `docs/testing-guidance.md`
  - Entry 10 of the lettered-ids run log
- **Code read:**
  - `PuzzleContent.cs:26–52`, `PuzzleCatalogue.cs:10,15`, `PuzzleDefinition.cs:107,113`
  - `CurrentCatalogue.cs:101–124,187–200`
  - `CurrentOwnerMembership.cs`, `CurrentOwnerRelations.cs`
  - the literal lines in `CurrentOwnerMembershipOracles.cs:67–70`, `CurrentCatalogueTests.cs:61–62` and `CurrentCapabilityInputsTests.cs:34,36,69`
  - `CurrentOwnerMembershipTests.cs`, `PipeBend90Tests.cs:190`, `NativeSceneFixture.cs:169–173`
- **Lane contracts:** visible-solid parts B, B2 and C, the Baseball repair, S5, S5b, S6–S6g and the policy-tool fixes.

**Measured on `2ce73eed`:**
- **Content ranges:**
  - The four content ranges end on entry boundaries and total 21,068 lines.
  - Subtitle lines run `6` to `36944`.
  - The two titles are at `:7199` and `:33176`.
- **Fixture rows:**
  - There are 304 `FIX-` rows. The 182 rows of the 29 removed ids form exactly the contract's 23 runs.
  - The kept rows are 78 Planned and 44 Playable, so 122.
  - The 14 "Level(s) unlocked" lines are the only other removed-id hits in `current-consumers.md`.
- **Campaign document:**
  - `campaign.md` has no `Exact source clauses` line, so the 21 new entries cannot change `CurrentOwnerRelations` scopes.
  - It has 0 lettered headings, 62 placement paragraphs, 62 "Currently authored" paragraphs and 11 "no `LEVEL-` entry yet" phrases.
- **Readers and titles:**
  - No `*.cs`, `*.ts`, workflow or hook reads `docs/coverage` or `docs/work-orders`.
  - No code names a removed id.
  - The title sweep's only hits are owned lines, the D4 decision record (`:139`), `quarter_bend`, level 150, or the "Currently authored" paragraphs that are being deleted.
- **Ledger rows:** PR-08, PR-24 and OS-01 to OS-10 each have 6 cells, the same as the header.

**AC7 hits on the drafting revision.** Every hit of `rg -n -i 'fifteen (composite|springboard|lanes)'` is either owned or pinned:

| File | Lines | Disposition |
|---|---|---|
| `docs/planning/requirements.md` | `:186`, `:5998` | owned (24o(2)) |
| `docs/springboard-elastic-contract.md` | `:29` | owned (contract `:86`) |
| `CAT-062-spring.md` | `:13`, `:66`, `:153`, `:166`, `:174`, `:180` | owned (contract `:87`) |
| `EL-194-springboard.md` | `:19`, `:20`, `:22`, `:54`, `:113`, `:126` | owned (contract `:87`) |
| `EL-194-springboard.md` | `:63`, `:64` | pinned to `@a6c914e` |
| `readiness/batch-a.md` | `:53`, `:84` | outside AC7's scope; kept as dated records by D5 |
| `readiness/batch-j.md` | `:34` | outside AC7's scope; kept as a dated record by D5 |

Once the owned lines are restated, AC7's first clause exits 1 and its third clause lists only `EL-194:63` and `:64`. Its second clause prints 2, because `surviving composite springboard instance` currently has 0 hits and the 24o(2) text adds one to each of the two lines.

## Round-4 findings

| Id | Status | Evidence |
|---|---|---|
| B1 | Resolved | `CAT-062:13`, `:180` and `EL-194:22` (plus `:20` and `:126`) are owned at `:87`. `springboard-elastic-contract.md:29` is owned at `:86` and is in the Final diff scope at `:186`. AC7 at `:141` also searches the springboard contract. The table above covers every hit. |
| N1 | Resolved | D5 at `:114` keeps `readiness/batch-a.md:53`, `:84` and `batch-j.md:34` unchanged as dated readiness records. |
| N2 | Resolved | OS-08 (`completion-ledger.md:399`) lists `requirements.md:186`, `:5998`, `springboard-elastic-contract.md:29`, EL-194 and CAT-062. |
| N3 | Resolved | `:87` corrects `EL-194:19`'s citation from `requirements.md:5955` to `:5998`. The `element-194` text is at `:5998`. |
| N4 | Resolved | `:6` and `:46` say "first published main after `2ce73eed` that contains owner decision 24o and ledger rows …; if task 0 does not find them, stop". |
| N5 | Resolved | `:29` now says "any shell-command write to a tracked repository file". `.tools/` is git-ignored (`.gitignore:6`). |
| N6 | Resolved | D4 at `:113` keeps *One Thing Leads to Another* at `campaign-coverage.md:139` as decision-record casing. |

**Checked and sound:**
- **AC arithmetic:** AC1 is 7332 + 6, with 5 new cases in `CuriousContraptions.tests` and 1 in `Coverage.Tests`. The census goes from 545 to 566 with 21 lettered entries. Discovery goes from 1475 to 1293.
- **Mechanical runs:** AC3, AC5 and AC6 run as written. AC6's unowned hits are pinned rows, the marked checks `EL-186:103` and `EL-209:87`, or plan-level mentions. Examples are `EL-115:117` "level 38" and `TH-05:122` "39, 40".
- **Rules:**
  - Tests read the real files through the production readers. Their expectations are transcribed from section 5, and every assertion is a set, map, string or count.
  - Nothing violates the forward-only rule: no alias, placeholder or redirect is kept.
  - The engine tier is justified.
  - The contract is within the 6,000-word budget.
- **Lanes:** disjointness holds. Lane 1's B and B2 share only the `CAT-019:67`, `:147`, `:160`, `:208` and `CAT-064:94` lines that the 24o ruling covers. Part C, the Baseball repair, S5, S5b, S6–S6g and lane 4 own none of this slice's files.

## New findings

**N1 — Non-blocking: the `delivery-batches.md` scope label disagrees with its owned lines.**
- **Evidence:** `:84` says "(shared, §4 only)" and `:188` says "(§4 here)". But the slice also owns `:352`, which sits under "Counts and how they reconcile" (heading `:346`), not §4 (`:203–236`).
- **Fix:** in `:84` and `:188`, write "§4 and the Counts line `:352`".

**N2 — Non-blocking: OS-02 and OS-10 do not list everything the contract now owns.**
- **Evidence:**
  - OS-08 (`completion-ledger.md:399`) lists `springboard-elastic-contract.md:29`, but OS-02 (`:393`) and OS-10 (`:401`) do not.
  - OS-02's file(s) cell also omits the 22 element documents and `campaign-coverage.md`.
  - OS-02's state cell says "planning audit rounds 1-4", which goes stale once this report lands.
- **Fix (coordinator, ledger):**
  - Add `springboard-elastic-contract.md:29` to the file(s) cells of OS-02 and OS-10.
  - Add "the element documents and `campaign-coverage.md` lines in the contract's owned scope" to OS-02's file(s) cell.
  - Change "rounds 1-4" to "rounds 1-5".

**N3 — Non-blocking, pre-existing: two domino lines describe deferred work as already done in `content/puzzles.json`.**
- **Evidence:**
  - `CAT-023-domino.md:141` and `EL-193-domino.md:116` say `domino_effect` is "(Playable; plan level 4, re-authored without the lamp …) in `content/puzzles.json`".
  - `EL-193:116` also says "the kept level 58 (in its converted form)".
  - Both statements are false now and stay false after this slice, because the work is DF-1 and DF-3. This slice does not make them false. `EL-193:116`'s "were removed" clause becomes true.
  - The contract gives the sibling lines `CAT-023:121` and `CAT-035:117` owner pointers, but not these two. The review repair policy (AGENTS.md) still requires confirmed pre-existing issues to be fixed.
- **Fix, either option:**
  - **Own the lines:** add `CAT-023:141` and `EL-193:116` to the owned element row (`:87`) as owner pointers only, citing DF-1/OS-06 and DF-3/OS-08. Then change "Element documents (22)" at `:87` and "the 22 element documents" at `:186` to 23, since EL-193 is new and CAT-023 is already counted.
  - **Keep the lines:** name both in D5 as planned-state lines left to DF-1 and DF-3, with that reason.

Verdict: Scoped Pass with non-blocking findings
