# Audit: content campaign housekeeping contract (round 4)

Independent, context-free re-audit of `_bmad-output/implementation-artifacts/spec-content-campaign-housekeeping.md` on `2ce73eed` plus the uncommitted owner decision 24o and ledger edits, 24 November 2026, after [round 1](audit-content-housekeeping-1.md), [round 2](audit-content-housekeeping-2.md) and [round 3](audit-content-housekeeping-3.md). Nothing in the repository was edited, built or run.

## Scope read

**Contract.** I read `_bmad-output/implementation-artifacts/spec-content-campaign-housekeeping.md`, which is untracked at `2ce73eed` and is 4,414 words, inside the 6,000-word budget. I also read the round 1–3 reports.

**Authorities read:**
- `AGENTS.md`
- Owner decision 24o (`owner-decisions-2026-11-22.md:70`, uncommitted) and the uncommitted `completion-ledger.md` diff:
  - rows PR-08, PR-24, OS-01, OS-02 and OS-03 (changed)
  - rows OS-06 to OS-10 (new)
- `campaign-coverage.md`: section 5 (`:95–192`) and lines `:33`, `:38`, `:40`, `:288`, `:303–304`, `:559`, `:564`
- `campaign.md:1`, `:9`, `:143–156` and the lettered-entry phrase
- `deferred-work.md:357–363`
- `delivery-batches.md` §4
- `TODO.md:8`
- the lettered-ids run log (Entries 8–10)
- `bmad-loop-protocol.md` (contract budget)

**Code and tests read:**
- Coverage and Ownership literals
- every `TheoryData` / `MemberData` source in the test projects, checked for any content-driven case count
- `LaunchLessonSolveTests.Lessons` (a static list of Playable ids)
- `GoalSignalSelectorTests`
- `TimingDeclarationTests.DeclaredLevelsExist`

**Lanes read.** I checked every lane contract for edits to `CAT-062`, `EL-194`, `springboard-elastic-contract.md` and `requirements.md`. None edits them; the Baseball repair explicitly does not edit `requirements.md`.

**Candidate built in the scratchpad.** I built the candidate with a filter matching 24o(1) and measured it:
- The no-op serialisation reproduces the file byte for byte (`cmp`).
- 34 levels are kept and 33 subtitles change.
- `--diff-algorithm=histogram` reports `35 21103`; default Myers reports `423 21491`.
- The four cited ranges are entry boundaries and total 21,068 lines.

**Springboard facts:**
- The kept levels that place or stock the `spring` package are:
  - `spring_forward` (plan level 02, inventory)
  - `ready_to_rebound` (03a, placed precompressed)
  - `spring_signal` (11c, inventory)
  - `double_cold_start` (19b, inventory, solution `lane2_spring_1`)
- `wind_then_release` and `saved_for_later` use the separate `wound_spring` package. `a_gentle_rebound` uses no spring.
- There are 15 composite spring lanes; 14 of them are in removed levels. This matches the OS-10 list and `EL-194:64`.

**Ledger rows.** PR-08, PR-24 and OS-01 to OS-10 each have 6 cells, the same as the table header. They are consistent with the contract's DF-1 to DF-5 and with 24o(1)–(3).

## Round-3 findings

| Id | Status | Evidence |
|---|---|---|
| B1 | Resolved | `:52` and `:180` name `--diff-algorithm=histogram`. Measured `35 21103` (Myers would report `423 21491`). The no-op `cmp` is identical. |
| B2 | Resolved | AC6 (`:139`) uses `'!**/readiness/**'`. Every hit is an owned line, a row pinned to `@a6c914e`, or a marked check: `EL-186:97`, `EL-209:71`, `CAT-066:86`, `CAT-019:125`, `CAT-015:126` and `EL-191:64` are all pinned. |
| B3 | Resolved for the lines it named | 24o(2) is applied (`:53`, `:85`). `CAT-062:66`, `:153`, `:166`, `:174` and `EL-194:19`, `:54`, `:113` are owned (`:86`), and OS-10 records the 14 closed. Further lines it missed are new B1. |
| N1 | Resolved | `campaign-coverage.md:155` is owned (`:83`). |
| N2 | Resolved | The P3 fallback is replaced by the 24o coordinator ruling (`:55`), which keeps both sides at the later merge. No lines are handed off. |
| N3 | Resolved | `:189` cites `:208`; `:190` says "becomes 33". |
| N4 | Resolved | `:77` states 513 groups and 2,052 cells. |
| N5 | Resolved | `:86` says restated lines are "marked once as 'authored positions'". |

**24o application, checked and sound:**
- **24o(1):** one recorded filter, a byte-for-byte no-op check, a histogram numstat check, Anvil `preview` plus `contentSha256` (the Anvil API supports this), and Write/Edit for every other write.
- **24o(2):**
  - The replacement text is accurate against the content.
  - AC7 clauses 1 and 2 run as written: after the edit, `rg -c` on the single file prints 2.
  - The surviving lane is `double_cold_start` lane 2, which matches `EL-194:64`.
- **24o(3):** DF-1, DF-3 and DF-4 are gated on the engine programme as Early level proof slices, recorded as OS-06, OS-08 and OS-09.
- **Coordinator ruling:** CAT-019 and CAT-064 are recorded as shared (`:55`, `:187`).

**AC1** is 7332 + 6 = 7338, with Coverage.Tests going from 130 to 131. No existing theory's case count depends on the content entries, the `FIX-` rows or the `LEVEL-` headings, so the total holds.

**Other checks:** testing guidance (sets, maps, strings and counts read through the production readers, expectations transcribed from section 5) and the forward-only rules are both satisfied.

## New findings

**B1 — Blocking: AC7's third clause fails as written, and current statements about "fifteen composite lanes" stay false outside the owned scope.**
- **AC7 fails mechanically.** `rg -n 'fifteen composite' --glob '!**/readiness/**' docs/planning/elements` would still list three lines that are neither pinned to `@a6c914e` nor owned:
  - `CAT-062-spring.md:13`: "the identity stays open until its launch-signal rows, `spring_signal` and the fifteen composite lanes pass"
  - `CAT-062-spring.md:180`: "(5) The fifteen composite lanes after ENGINE-GOALS; only then is EL-194 closed"
  - `EL-194-springboard.md:22`: "fifteen composite lanes", which appears twice on that line
- **A current contract goes stale unchecked.** `docs/springboard-elastic-contract.md:29` is the contract linked from `requirements.md:5998`. It says "including all fifteen composite springboard lane instances", which becomes false after the slice. It is not owned and AC7 does not search it. AGENTS.md requires active docs to be updated together with the change.
- **No lane conflict.** No lane edits any of these files (checked).
- **Fix:**
  - Add `CAT-062:13` and `:180` and `EL-194:22` to the owned element row. Restate each to the 24o(2) set: the springboard lessons plus `double_cold_start` lane 2 until OS-08, the other 14 closed under 24d (OS-10). The element-document count stays 22.
  - Add `docs/springboard-elastic-contract.md:29` to the owned scope and to the Final diff scope.
  - Extend AC7 with: "`rg -n 'fifteen composite' docs/springboard-elastic-contract.md` exits 1."

**N1 — Non-blocking: `readiness/batch-a.md:53` and `:84` still say "the fifteen composite lanes".** AC7's glob excludes them. **Fix:** restate both lines in this slice (owned, under the 24m sharing rule), or state in the contract that `batch-a.md` is a dated readiness record that is left unchanged.

**N2 — Non-blocking: OS-08 does not carry the follow-up wording.** The DF-3 conversion drops `double_cold_start`'s spring lane (`campaign-coverage.md:136`). After that, the reworded criterion's "surviving instance … until its conversion" clause refers to nothing. OS-08 lists only `content/puzzles.json`. **Fix (coordinator, ledger):** add `requirements.md:186`, `:5998`, `EL-194:113` and the CAT-062 lines to OS-08's file(s), so the conversion slice restates the criterion.

**N3 — Non-blocking: wrong requirement line cited in a line this slice edits.** `EL-194:19` cites `requirements.md:5955`; the requirement is at `:5998`. **Fix:** correct the citation in the same edit, and say so in the owned element row.

**N4 — Non-blocking: the baseline wording admits `2ce73eed` itself.** `baseline_revision` (`:6`) and Code Map (`:46`) say "2ce73eed or later". But `2ce73eed` does not contain 24o or the OS-06 to OS-10 rows, which task 0 requires. **Fix:** write "the first published main after `2ce73eed` that contains owner decision 24o and ledger rows OS-02, OS-03 and OS-06 to OS-10; if task 0 does not find them, stop."

**N5 — Non-blocking: the Never clause contradicts Verification.** Never (`:29`) forbids "any shell-command file write other than the one 24o(1) filter". Verification (`:158`, `:175`) writes raw output under `.tools/content-housekeeping-evidence/`, logs and binlogs through the shell. **Fix:** in `:29`, write "any shell-command write to a tracked repository file other than the one 24o(1) filter".

**N6 — Non-blocking: title casing.** `campaign-coverage.md:139` uses *One Thing Leads to Another* and is not owned; D4 and H3 use sentence case ("One thing leads to another"). `:304` and `campaign.md:759` are owned and can be restated. **Fix:** add `:139` to the owned `campaign-coverage.md` lines, restated in sentence case, or state in D4 that section 5's decision text keeps its casing.

Verdict: Incomplete
