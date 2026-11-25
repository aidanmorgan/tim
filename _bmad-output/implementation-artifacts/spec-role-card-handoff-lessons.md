---
title: 'Role-card handoff lessons from the policy-tool fixes slice (lane 4)'
type: 'chore'
created: '2026-11-25'
status: 'done'
baseline_revision: '66c1c200e34d5cbd91ba6f0ff118a9c91bed5a24'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/_bmad/custom/role-cards.md'
  - '{project-root}/_bmad/custom/bmad-loop-protocol.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The policy-tool fixes slice's close note (run log `run-logs/spec-policy-tool-fixes.md` E14) found two mistakes a role card could have prevented: stale record lines (the contract Status line, `TODO.md`, the acceptance table's run citations) lagged the candidate at review passes 1, 2 and 4 because records were edited before a repair returned; and the first implementation owner ended its turn with its full round running in the background, which stalled the slice for a day (E6). The protocol's Close note rule (`bmad-loop-protocol.md:261`) puts such a lesson on the relevant role card, in one line, in the next policy change.

**Approach:** Add one line to the Coordinator card and one line to the Implementation owner card in `_bmad/custom/role-cards.md`, each linking the protocol rule it applies (owner decision 24m lane 4); restate lane 4's line in `TODO.md`. The policy-tool fixes publication receipt for `66c1c200` is not recorded here: the S5 slice records it with its merge (coordinator message of 25 November 2026, run log handled messages).

## Boundaries & Constraints

**Always:** Each new line is stated once, in `role-cards.md`, and links the protocol section it applies; it is consistent with the workflow's Subagents rule (`workflow.md:55`), the protocol's Capacity and independence (`bmad-loop-protocol.md:132`, `:134`), Routing by what is wrong (`:221`, the owner updates the acceptance-to-test table) and Terminal completion (`:259`). `TODO.md` edits are line-local (shared document under 24m). Anvil validation before each write; engine review tier; one full local round with zero failures and `Skipped: 0`; the maintainability gate passes.

**Never:** A second copy of either rule in `bmad-loop-protocol.md`, `AGENTS.md`, `docs/delivery-workflow.md` or a TOML prompt. Any change to `tools/anvil/`, test code, the customization TOML files or any other card line. A second owner for the acceptance-to-test table. Committed prose describing this candidate's own approval or publication as done or pending (`bmad-loop-protocol.md:259`).

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| M1 | Coordinator card at the candidate | One bullet, directly after the existing "Update `TODO.md` and the sprint status only when…" bullet: the coordinator restates the Code Map's Status line (where the contract has one) and `TODO.md` in the same edit that records a returned repair, and returns a stale acceptance-table run citation to the owner as a patch | Absent at the baseline (control) |
| M2 | Implementation owner card at the candidate | One line: a long command runs in the foreground with a timeout, or in the background with the host's blocking tool call in the same turn; the owner never ends its turn while a command it started is running or while saying work is pending | Absent at the baseline (control) |
| M3 | `AGENTS.md`, `_bmad/custom`, `docs/delivery-workflow.md` searched for each new line's distinctive phrase | Exactly one hit each, in `role-cards.md` | A second hit is a duplicate rule (Forward-Only Fail) |
| M4 | `TODO.md` at the candidate | Only the Lane 4 bullet changes; it states this slice's verified state (AC7) and no publication receipt | Any other changed `TODO.md` line is out of scope |

</intent-contract>

## Code Map

**Status: done: review pass 4 clean and Final diff scope Pass (run log E11), close note E12; final-candidate gate, harness and full round passed (E8), matching `TODO.md`'s figures.** Delivery shape: single policy-document slice (no element, family or closure). Review tier: **engine**, because `_bmad/custom/role-cards.md` is a policy file outside the data-only allow-list (`bmad-loop-protocol.md:54–62`). Lane 4 of owner decision 24m; dispatched by the coordinator on 25 November 2026 (run log E1). Planning: Scoped Pass at audit 3 (run log E2, E3); amended-section re-check Scoped Pass in run log E7.

**Dispatch.** Checkout `.claude/worktrees/slice-role-card-handoff`, branch `slice-role-card-handoff`; baseline `66c1c200e34d5cbd91ba6f0ff118a9c91bed5a24` (`origin/main`). Policy revision: the baseline, no owned uncommitted policy delta at planning. Implementation owner: fresh no-history subagent (Claude Opus 5.5, agent `abb9e4d55a934cf7e`; run log E4).

### Owned scope

- `_bmad/custom/role-cards.md` -- Coordinator card: one bullet inserted after `:83`; Implementation owner card: one line after "Before handoff" item 7 (`:41`), before "Report:" (`:43`). Line numbers are the baseline's; in the candidate the coordinator bullets are at `:84–86`.
- `TODO.md` -- the Lane 4 bullet (`:9`) only: this slice's state. The `66c1c200` receipt (`TODO.md` and the policy-tool fixes run log) is S5's, recorded with its merge; this slice takes main's receipt as it is when it merges (coordinator message, 25 November 2026).
- This contract and `_bmad-output/implementation-artifacts/run-logs/spec-role-card-handoff-lessons.md`.

Read, not changed: `bmad-loop-protocol.md:132`, `:134` (waiting, `TODO.md` timing), `:221` (routing; the owner updates the table), `:261` (Close note rule); `role-cards.md:3` (card header), `:13`, `:19`, `:82–83` (baseline); `bmad-build-auto.toml` (names the cards, does not restate them).

### Capability probe

Not applicable: the probe covers element slices and slices that use an engine capability they do not deliver (`bmad-loop-protocol.md:25`); this slice changes two policy lines and uses no engine, abstraction, wire or presentation capability. No planning scout is dispatched. Open owner decisions: none.

### Carry-forward

Previous slice's close note: `run-logs/spec-policy-tool-fixes.md` E14 (source of both lines). `deferred-work.md`: no entry names `role-cards.md` as open (R7 closed by the policy-tool fixes slice).

### Engineering rules

Applicable: E5 (one rule, no second statement; AC2, AC8), E7 (document consistency, preservation and links; AC1, AC8), E8 (owned scope, `TODO.md`, remote qualification unverified; AC7), E9 (maintainability gate; AC5). Not engaged: E1–E4, E6 and D1–D7, because no engine, abstraction, package, wire, presentation, test or tool code changes.

### Decisions

- **D1. Each line applies a protocol rule to its role and links it, as the card header (`role-cards.md:3`) requires.** The owner line applies `:132`'s role-neutral "do not end the unattended turn merely to await background work"; the coordinator line applies `:134` ("It edits `TODO.md` … only when the slice's … candidate changes") and `:221`. The Close note rule (`:261`) places them on the cards; the protocol is not edited.
- **D2. The owner waits with the host's blocking tool call in the same turn until the command exits, waiting again after a timeout or a move to the background** (the term of `role-cards.md:84` in the candidate and `:134`); this host moves a foreground call to the background after 180 s (run log E4), and a notification after the turn ends is the E6 stall.
- **D3. The coordinator line sits directly after the `TODO.md` bullet (`:83` at baseline, `:85` in the candidate) and leaves the table with its owner:** the coordinator restates the Code Map's Status line and `TODO.md` in the same edit; a stale run citation is returned as a patch (`:221`), not edited by the coordinator.

**What this removes:** nothing; the slice adds two card lines and restates one `TODO.md` bullet.

## Tasks & Acceptance

**Execution:**

1. `_bmad/custom/role-cards.md` -- implementation owner makes the bullet after the `TODO.md` bullet read exactly: ``- Restate the Code Map's Status line, where the contract has one, and `TODO.md` in the same edit that records a returned repair, not before the repair returns ([records](bmad-loop-protocol.md#capacity-and-independence)); check that the returned patch also updated the acceptance-to-test table's run citations, and return a stale one to the owner as a patch ([routing](bmad-loop-protocol.md#triage-re-review-and-completion)).`` -- M1.
2. `_bmad/custom/role-cards.md` -- implementation owner makes the paragraph between "Before handoff" item 7 and "Report:" (separated from item 7 by one blank line) read exactly: `Run a long command (a build, the full round) in the foreground with a timeout, or start it in the background, and wait for it with the host's blocking tool call in the same turn until it exits, waiting again whenever a timeout expires or the host moves it to the background ([waiting](bmad-loop-protocol.md#capacity-and-independence)); never end your turn while a command you started is still running or while saying work is pending.` -- M2.
3. `TODO.md` Lane 4 bullet -- coordinator restates lane 4's state (status reconciliation, `bmad-loop-protocol.md:130`, `:134`) after the handoff results -- M4.

**Acceptance Criteria:**

- AC1 (M1, M2): Given the candidate, when `git diff 66c1c200 -- _bmad/custom/role-cards.md` is read, then it adds exactly the two lines of tasks 1 and 2 at those places, plus the one blank line that separates task 2's paragraph from list item 7, and removes nothing; all three links resolve to existing headings of `bmad-loop-protocol.md`.
- AC2 (M3): Given the candidate, when `git grep -n -e 'in the same edit that records a returned repair' -e 'never end your turn while a command you started' -- AGENTS.md _bmad/custom docs/delivery-workflow.md` runs, then it lists exactly two lines, both in `_bmad/custom/role-cards.md`; control: the same command at `66c1c200` lists none.
- AC3: Given the candidate, when `dotnet run --file tools/anvil/VerifyPolicy.cs -- --bmad-check --self-test` runs, then `Fixtures: Passed: 37, Failed: 0, Skipped: 0.`; and `--bmad-check --project-root "$PWD" --route Auto` reports `structural findings=0`.
- AC4: Given the candidate, when `dotnet run --file tools/anvil/VerifyPolicy.cs` runs with no arguments, then `Policy fixtures: Passed: 78, Failed: 0, Skipped: 0.` (asked by the dispatch; `tools/anvil/` is unchanged).
- AC5: Given the candidate, when the maintainability gate runs, then it passes with the `Coupling dependencies:` summary and both canary `PASS` lines.
- AC6: Given the candidate, when the full local round runs, then 7339 passed, 0 failed, `Skipped: 0` (equal to the baseline; no test project changes).
- AC7 (M4): Given the candidate, when `git diff 66c1c200 -- TODO.md` is read, then only the Lane 4 bullet changes; it names this slice's contract and run log, the two card lines by card (not restating their rules), the AC3–AC6 results and remote build, artifact and deployment unverified; it drops the baseline sentence "This commit's full round, harness run and SnapshotApproval are the publisher's next checks." as a statement about `66c1c200`; and it records no publication receipt and does not describe this candidate's own approval or publication. `git diff --stat 66c1c200` lists only owned-scope paths and `git diff --check 66c1c200` is clean.
- AC8 (rule consistency, read in review): Given the candidate, when both lines are read against `role-cards.md:3`, `:13`, `:19`, `:84–85` (candidate numbering), `bmad-loop-protocol.md:132`, `:134`, `:182`, `:221`, `:259` and `workflow.md:55`, then each is consistent: the owner line forbids what `:132`/`workflow.md:55` forbid and uses `:134`'s waiting term; the coordinator line's restate clause follows `:134` and leaves the table's edits to the owner (`:19`, `:182`, `:221`). Control: the planning-audit-1 wording, "Restate the contract's Status line, `TODO.md` and the acceptance-to-test table's run citations in the same edit that records a returned repair, not before the repair returns.", has the coordinator edit the table itself and conflicts with `:221`.

## Spec Change Log

1. 2026-11-25, review pass 1 (`bad_spec`). Trigger: EFE F1, ECH1 and the first Forward-Only borderline item (the owner line's foreground option leaves a command running when this host moves a foreground call to the background after 180 s, run log E4), BH2 (the coordinator line's restate clause linked only `#triage-…`, while its rule is `:134`), ECH6 (AC1 did not count the separating blank line) and EFE F3 (stale `:82–83` citations). Amended: Tasks 1 and 2, D1–D3, AC1, AC8 and the "Read, not changed" list. Known-bad state avoided: a card line whose first option ends in an unwatched background command, and a coordinator clause linked to a section that does not state it. KEEP: both lines' placement and the blank line; AC2's two phrases; the coordinator check-and-return of stale run citations; the owner's "or while saying work is pending"; every handoff result in E4 for inputs this amendment does not change.

## Review Triage Log

### 2026-11-25 — Review pass
- verdicts: 23 findings — high 0, medium 3, low 7, false 13, maybe-false 0
- findings (layers: Blind Hunter BH, Edge Case Hunter ECH, Verification Gap none, Intent Alignment IA, Forward-Only FO, Element-Free Engine EFE; detail in run log E5):
  - `[false]` `[reject]` BH1 owner line has no authority in the linked section — `#capacity-and-independence` holds `:132` "do not end the unattended turn merely to await background work", role-neutral and agreeing with the line, so the header's conflict clause never applies; D1 now names it.
  - `[low]` `[bad_spec]` BH2 coordinator restate clause linked to a section that does not state it — amended Task 1 links `#capacity-and-independence` (`:134`) for that clause (Spec Change Log 1).
  - `[false]` `[reject]` BH3 "while saying work is pending" forbids a blocked return — the card's explicit exits (`role-cards.md:13` "stop and return it to planning") and the protocol's blocked route hand work back; it is then not this owner's pending work.
  - `[low]` `[patch]` BH4 Lane 4 bullet names no next check — coordinator restates `TODO.md` with what remains for the publisher (records, task 3); the policy-tool receipt stays with S5 by coordinator message.
  - `[medium]` `[bad_spec]` ECH1 a blocking wait or foreground timeout that expires leaves the command unwatched — Task 2 now waits until exit, again after a timeout or a move to the background (Spec Change Log 1).
  - `[false]` `[reject]` ECH2 no blocking wait tool on the host — the coordinator card (`:84`) and `:134` already require that tool on the same host, and `:130` blocks a missing capability; the line adds no new dependency.
  - `[false]` `[reject]` ECH3 contract without an acceptance-to-test table — the protocol's Slice record (`:83`) puts the table in every contract.
  - `[low]` `[patch]` ECH4 results cited against an uncommitted candidate — `TODO.md` restated as "verified before this commit" (records, task 3).
  - `[false]` `[reject]` ECH5 policy-tool publication state removed — the coordinator's message of 25 November assigns that receipt to S5's merge (run log handled messages).
  - `[low]` `[bad_spec]` ECH6 AC1 counts two lines but the diff adds a blank separator — AC1 amended (Spec Change Log 1).
  - `[false]` `[reject]` ECH7 two owners for a stale Status line — record lines are coordinator status reconciliation (`role-cards.md:85` candidate, protocol `:130`, `:134`; predecessor E19 routed record rows to the coordinator); `:221` routes a review finding's repair, and the new line keeps the table, the owner's record, with the owner.
  - `[false]` `[reject]` ECH8 linked rule is coordinator-scoped — as BH1: `:132` is role-neutral; the foreground call with a timeout is a blocking tool call.
  - `[false]` `[reject]` IA1 run citations moved from coordinator restatement to check-and-return — deliberate (D3, AC8; planning audit 1 finding 1).
  - `[false]` `[reject]` IA2 "completion notification" replaced by a same-turn blocking wait — deliberate (D2; planning audit 1 finding 2).
  - `[false]` `[reject]` IA3 owner line applies a coordinator rule — as BH1.
  - `[low]` `[patch]` IA4 the Lane 4 bullet paraphrases both rules outside AC2's search — coordinator names the lines by card only (records, task 3); AC7 now says so.
  - `[false]` `[reject]` IA5 Lane 4 history dropped — the receipt is S5's (coordinator message); AC7 requires dropping the stale sentence.
  - `[false]` `[reject]` IA6 checks cannot observe agent behaviour — a policy-prose change is judged by consistency, preservation and links (review policy §1/§2, AC8); adherence shows in later slices (protocol `:263`).
  - `[medium]` `[bad_spec]` FO1 (borderline) foreground option does not complete on this host — same root cause as ECH1 and EFE F1 (Spec Change Log 1).
  - `[false]` `[reject]` FO2 (borderline) coordinator line beside `:85` as a second `TODO.md` rule — the auditor itself places it on the allowed side: a returned repair is a candidate change, so it narrows `:85`.
  - `[medium]` `[bad_spec]` EFE F1 owner line's first option contradicts run log E4 for the full round — Task 2 amended (Spec Change Log 1).
  - `[low]` `[patch]` EFE F2 AC7 row has no run after the `TODO.md` edit — owner reruns `git diff --check`/`--stat` after the coordinator's records and fills the row.
  - `[low]` `[bad_spec]` EFE F3 AC8, D2 and E4 cite `:82–83` for bullets now at `:84–85` — AC8 and D2 amended; run log correction appended (E5).

### 2026-11-25 — Review pass
- verdicts: 26 findings — high 0, medium 0, low 2, false 24, maybe-false 0 (pass 2, the first after `bad_spec`; all six layers; detail in run log E7)
- findings:
  - `[false]` `[reject]` BH2-1 Lane 4 no longer states the policy-tool fixes outcome — carried: as ECH5 (S5 records that receipt, coordinator message).
  - `[false]` `[reject]` BH2-2 "the Code Map's Status line" is undefined, so optional — the rule restates that line where it exists and the protocol requires none (planning audit 2 finding 1); no stale record follows.
  - `[false]` `[reject]` BH2-3 one edit cannot span two files; sprint status omitted — "edit" is the one record update, as `role-cards.md:85` already pairs two files; sprint status tracks story state, which a repair inside review does not change, and `:85` still governs it.
  - `[false]` `[reject]` BH2-4 `[records]` link does not support restating a Code Map line — the clause narrows `:134`'s timing rule; the Status line is the contract record the Coordinator card keeps to one line.
  - `[false]` `[reject]` BH2-5 `[waiting]` link points at a coordinator rule — carried: as BH1 (`:132` is role-neutral; D1).
  - `[false]` `[reject]` BH2-6 a command killed by its timeout leaves nothing to wait for — it has exited; the handoff round counts only with zero failures, `Skipped: 0` and `HEAD-AFTER` (`bmad-loop-protocol.md:184`), so a cut-off run cannot be reported as the round.
  - `[false]` `[reject]` ECH2-1 a hung command makes the owner wait forever — each blocking call returns at its timeout, so the owner regains control at every wait, and the stagnation trigger (`:124`) applies.
  - `[false]` `[reject]` ECH2-2 sprint status omitted — as BH2-3.
  - `[false]` `[reject]` ECH2-3 linked Waiting paragraph covers only the coordinator — carried: as BH1.
  - `[false]` `[reject]` ECH2-4 the merge will conflict on the Lane 4 line — the coordinator's message says to take main's receipt as it is at the merge; no defect in this candidate.
  - `[false]` `[reject]` ECH2-5 the Lane 4 line describes this candidate's pending approval — it names what remains for the publisher, which `:259` permits.
  - `[false]` `[reject]` IA2-a run citations not edited by the coordinator — carried: as IA1.
  - `[false]` `[reject]` IA2-b additions beyond the intent's wording — each comes from planning audits 1–2 or pass-1 triage (D2, D3, Spec Change Log 1).
  - `[false]` `[reject]` IA2-c completion-notification option removed — carried: as IA2.
  - `[false]` `[reject]` IA2-d owner line sits under "Before handoff" — it follows item 7, the full round where E6 stalled, and its text names any long command including a build; nothing limits it to handoff.
  - `[false]` `[reject]` IA2-e link targets the coordinator paragraph — carried: as BH1.
  - `[false]` `[reject]` IA2-f Lane 4 history replaced — carried: as IA5.
  - `[false]` `[reject]` IA2-g each line is long — one physical line each, as `:261` asks.
  - `[false]` `[reject]` IA2-h checks cannot observe behaviour — carried: as IA6.
  - `[false]` `[reject]` FO2-1 two ways to start a command — the auditor places it on the allowed side: both end in the same blocking wait.
  - `[false]` `[reject]` FO2-2 owner and coordinator waiting lines worded differently — allowed side: different roles, no runtime choice.
  - `[false]` `[reject]` FO2-3 `:86` beside `:85` — carried: as FO2.
  - `[false]` `[reject]` FO2-4 who fixes a stale record line — allowed side: `:86` prevents staleness, `:221` routes a confirmed one; the table keeps one owner.
  - `[false]` `[reject]` FO2-5 contract AC8 control and run-log evidence — records only, not rules in force.
  - `[low]` `[patch]` FO2-6 `TODO.md`'s verified figures come from the pre-repair candidate — the designated executor reruns AC4–AC6 on the final candidate before commit; the coordinator restates the line if any figure differs.
  - `[low]` `[patch]` EFE-R2-1 the amended-section planning re-check had no recorded verdict — recorded by the coordinator in run log E7 (auditor `abd405d79a5d0d2d2`, Scoped Pass, no findings); Status line cites it.

### 2026-11-25 — Review pass
- verdicts: 9 findings — high 0, medium 0, low 2, false 7, maybe-false 0 (pass 3: Verification Gap none, Forward-Only Pass, Element-Free Engine Incomplete; Re-review scope applies; detail in run log E9)
- findings:
  - `[false]` `[reject]` FO3-1 two ways to start a command — carried: as FO2-1.
  - `[false]` `[reject]` FO3-2 "waiting again" depends on the host — allowed side: it repeats the same wait, no second rule.
  - `[false]` `[reject]` FO3-3 owner line beside `:84`, `:132`, `:134`; its extra detail versus the header (`role-cards.md:3`) — carried: as FO2-2 and BH1 (D1).
  - `[false]` `[reject]` FO3-4 `:86` beside `:85` — carried: as FO2.
  - `[false]` `[reject]` FO3-5 `:86` against `:221`; EFE-R2-1 repaired by the coordinator — carried: as FO2-4; record lines are coordinator status reconciliation (ECH7).
  - `[false]` `[reject]` FO3-6 contract history quotes old wording — carried: as FO2-5.
  - `[false]` `[reject]` FO3-7 `TODO.md` Lane 4 replaces the stale sentence — allowed side: one current state.
  - `[low]` `[patch]` EFE-R3-1 the Status line's planning clause still pointed to E5 for the amended-section re-check — coordinator restated it to cite E7 in the same edit as this triage.
  - `[low]` `[patch]` EFE-R3-2 the AC8 Run cell does not cite an executed independent read — returned to the owner `abb9e4d55a934cf7e` as a patch (the table is the owner's record, `:221`), citing pass-2 EFE (E7) and pass-3 EFE (E9).

### 2026-11-25 — Review pass
- verdicts: 0 findings — high 0, medium 0, low 0, false 0, maybe-false 0 (pass 4, re-review of the pass-3 record repairs: Verification Gap none, Forward-Only Pass, Element-Free Engine Scoped Pass; run log E11)

## Implementation Notes

- Owner: the coordinator's implementation subagent (Claude Opus 5.5); edits only `_bmad/custom/role-cards.md` and this contract's records; command lines and raw evidence in run log E4.
- Task 1 is `role-cards.md:86`, task 2 is `:43`, both verbatim to the tasks as amended by Spec Change Log 1 (repair 1, E6: task 1 gains the `records` link, task 2 the wait-until-exit clause); task 2 keeps one blank line before it, so the paragraph does not continue list item 7 (3 insertions, 0 deletions).
- Executor: the implementation owner for every Verification command, including the handoff full round. Build: pinned SDK 10.0.403 and Node v26.12.1, the five non-incremental capture builds of `docs/delivery-workflow.md:38–53` (binlogs `~/.local/share/tim-toolchain/logs/slice-role-card-handoff/binlogs/`); raw logs in `.../handoff/` (E4).
- Invalidating inputs: any change to `_bmad/custom`, `tools/anvil`, source, project files or toolchain. Edits to this contract, the run log or `TODO.md` do not: no test or tool reads them (`handoff/records-readers.log`, E4).
- Registration check: not applicable; no package, port, parameter, wire field, test file or tool is added or renamed. Capability probe: not applicable, as the Code Map records; confirmed, no engine capability is used.
- Self-check of the full owned diff: two lines and one blank line added, nothing removed, no other product path changed; no needless branch, duplicate rule or churn; each phrase occurs once (AC2). The `TODO.md` Lane 4 bullet is the coordinator's (task 3); the owner read it against AC7 in E6; remote build, artifact and deployment unverified.

## Verification

**Commands** (executor: implementation owner at handoff; raw output under `~/.local/share/tim-toolchain/logs/slice-role-card-handoff/`; pinned toolchain on `PATH`; reuse is invalidated by any change to `_bmad/custom`, `tools/anvil` or source):
- Compiler-proof capture builds and the full round per `docs/delivery-workflow.md:38–53`, under `caffeinate -i`, `HEAD` at the top of the round log and `HEAD-AFTER` at the bottom -- expected: AC6.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: AC5.
- `dotnet run --file tools/anvil/VerifyPolicy.cs`; `-- --bmad-check --self-test`; `-- --bmad-check --project-root "$PWD" --route Auto` -- expected: AC3, AC4.
- AC2's `git grep` at the candidate and at `66c1c200`; `git add -N` on owned new files, then `git diff --check 66c1c200` -- expected: AC2, AC7.

**Acceptance-to-test table:**

| Row | Check | Control / boundary | Run |
|-----|-------|--------------------|-----|
| M1, M2 / AC1 | role-cards diff read, links resolved | baseline lacks both lines | Pass (repair 1): both lines match amended tasks 1 and 2 at `:86`, `:43`, plus the one blank line, nothing removed; all three links resolve (protocol `:126` twice, `:213`) (`repair-1/ac2-grep-anchors.log`, E6) |
| M3 / AC2 | `git grep` two phrases | zero hits at `66c1c200` | Pass (repair 1): 2 hits, both `role-cards.md`; `66c1c200`: 0 hits, exit 1 (`repair-1/ac2-grep-anchors.log`, E6) |
| AC3 | `--bmad-check --self-test`, `--route Auto` | — | Pass (final candidate): 37/0/0; `structural findings=0` (`final/bmad-self-test.log`, `final/bmad-check-auto.log`, E8) |
| AC4 | policy-tool harness | — | Pass (final candidate): 78/0/0 (`final/policy-harness.log`, E8) |
| AC5 | maintainability gate | — | Pass (final candidate): violations=0 (metrics 0, coupling 0), `Coupling dependencies:` summary and both canary PASS lines (`final/maintainability.log`, E8) |
| AC6 | full local round | — | Pass (final candidate): 7339 passed, 0 failed, `Skipped: 0` in all five projects; HEAD and HEAD-AFTER `66c1c200` (`final/full-round.log`, E8) |
| M4 / AC7 | `TODO.md` diff read, `git diff --stat`/`--check` | stale baseline sentence gone | Pass: `TODO.md` read in E6 (only the Lane 4 bullet, each AC7 clause met); final candidate `--stat` lists only the four owned paths, `--check` clean (`final/diff-check.log`, E8) |
| AC8 | consistency read in review | audit-1 wording conflicts with `:221` | Pass: independent reads by the pass-2 Element-Free Engine auditor, AC8 consistent (E7), and the pass-3 Element-Free Engine auditor, AC8 consistent (E9); owner read consistent (E4) |

## Auto Run Result

- Status: done (native). Summary: two one-line role-card additions from the policy-tool fixes close note (E14): the Implementation owner card's long-command waiting line (`role-cards.md:43`) and the Coordinator card's record-restatement line (`:86`), each linking the protocol rule it applies; `TODO.md` Lane 4 restated.
- Files: `_bmad/custom/role-cards.md` (+3 lines: two lines and a separating blank); `TODO.md` (Lane 4 bullet); this contract and its run log.
- Review: four passes (E5, E7, E9, E11). Pass 1 `bad_spec` (Spec Change Log 1: owner-line wait until exit, coordinator `[records]` link, AC1, citations); passes 2–3 two low record patches each, all repaired and re-reviewed; pass 4 clean. Rejected findings and their reasons are in the Review Triage Log. Nothing deferred.
- Final diff scope: **Pass** (E11; reviewer `a398e6d9121d31115`): candidate HEAD `66c1c200` plus the owned uncommitted changes, baseline `66c1c200e34d5cbd91ba6f0ff118a9c91bed5a24`; all four owned paths kept, nothing removed, no justified simplification.
- Acceptance verdict (engineering, Element-Free Engine auditor pass 4): Scoped Pass, engine tier confirmed, D1–D7 not engaged; E5, E7, E8, E9 met.
- Verification on the final candidate (E8, executor the implementation owner, logs `~/.local/share/tim-toolchain/logs/slice-role-card-handoff/final/`): maintainability gate PASS (violations 0, coupling summary and both canaries); policy-tool harness 78/0/0; `--bmad-check --self-test` 37/0/0; `--route Auto` structural findings 0; full local round 7339 passed, 0 failed, `Skipped: 0`, HEAD and HEAD-AFTER `66c1c200`; `git diff --check 66c1c200` clean. Records edited afterwards are read by no test or tool.
- Follow-up review recommended: false (pass 4 patched nothing).
- Residual risks: the lines' effect on agent behaviour shows only in later slices; remote build, artifact and deployment unverified; browser, performance and release qualification not claimed. Remaining for the publisher: the merge with `origin/main` (taking main's Lane 4 receipt as it is), the full round on the merged commit and SnapshotApproval.
