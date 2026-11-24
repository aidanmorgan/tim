# Planning audit 2: `spec-tool-lettered-level-ids.md` (lettered campaign level ids, lane 3)

Independent planning auditor (context-free), 24 November 2026, on the contract in the worktree against `0959e883` and the uncommitted owner decision 24m. No file edited.

**Verdict: Scoped Pass**

## Round-1 findings

| # | Round-1 finding | Status |
|---|---|---|
| 1 | Real-tree test used a planned level and pinned the count | Resolved: M1 inserts `LEVEL-069z` with whole-set equality; M2 checks `LEVEL-001`…`LEVEL-150` present and `LEVEL-069z` absent; Design Note 5 updated |
| 2 | AC4 matched CA-01's item text | Resolved: AC4 limited to `campaign-coverage.md` and `campaign.md`; CA-01 changes only its state column |
| 3 | AC2 command not shown to run | Resolved: task 0 runs the baseline first; AC2 runs `--oracles` without a root. Run on `0959e883`: exit 0, `CurrentOwners` `Positive` true, `NegativeCount` 15, Debug, about 2 s, no Release build, `ActualCurrentPolicy` null |
| 4 | Grouped-role regex cannot name a suffixed owner | Resolved: widened (D7, task 2), tested by M5 |
| 5 | M3 only checked "contains" | Resolved: `SetEquals`, no numbered `LEVEL-` heading in its fixture, "representative ids" |
| 6 | Wrong probe citation | Resolved: cites `CurrentPreparationPreservesObligationsAndReportsAllUnresolvedRoles` with `ENGINE-CORE-2d` |
| 7 | Census probe classification | Resolved: supplemental CLI check via `OwnershipOracles.cs:47` / `Program.cs:17` |
| 8 | Handoff to OS-02 | Resolved with a note: the entry says "records the root census run, which needs Release outputs"; its feasibility is new finding 3 |
| 9 | Disjointness list incomplete | Resolved: part D, Baseball, S5b, S6 and S6b–S6g added |
| 10 | Traceability | Resolved: `requirements.md:2527,3431` cited |
| 11 | Ordering wording | Resolved: D2 says no code consumes the order; non-canonical spellings are refused |

## New findings

1. **Non-blocking (test).** AC1, AC3 and "Running beside other work": on a merged candidate (24m merge rule), AC1's "baseline + 11" and AC3's diff against `<baseline_revision>` would include other lanes' published files. The contract already says the later publisher re-measures, and both AC3 commands still hold because no running lane edits `content/puzzles.json` or `campaign.md`. Fix: AC1 append "; when this slice publishes second, the merged main's full round + 11"; AC3 append "; on a merged candidate, the path list and both commands compare against the merge's `origin/main` parent instead of `<baseline_revision>`".

2. **Non-blocking (test).** AC4 detects stale text only at `:38`; the "Not done" sentence at `:564` ("Adding them needs two tool changes in one slice: …") would pass unchanged. Fix: add "and `git grep -n 'Adding them needs two tool changes' -- docs/planning/elements/readiness/campaign-coverage.md` lists nothing".

3. **Non-blocking (handoff).** The deferred entry tells OS-02 to record the root census run, which needs Release outputs of Abstractions, FlatBuffers, Core and Bundle (`SourceInventory.cs:39–41,125–129`) that this workflow never builds. Fix: "…raises the census literal by the headings it adds and verifies it by recounting the seven documents with the oracle's census pattern (`CurrentOwnerMembershipOracles.cs:60`), as was done for 545 at `0959e883`, unless the coordinator rules that the Release library builds a root `--oracles .` needs are allowed (owner: that slice)".

4. **Non-blocking (test clarity).** Task 3: an engine document with only the disposition table has no owner heading, so `Parse` throws "Current authority document has no declared owner headings." before reaching M3/M4 (loud, never a false pass). Fix: "minimal valid engine document (one owner heading such as `## ENGINE-CORE-1` and one P0 disposition row)".

5. **Non-blocking (anchors).** `CopyCurrentInputs` spans `CurrentCapabilityInputsTests.cs:188–230`; `WorkOrderId` spans `CapabilityInventory.cs:34–45`.

## What was verified

- Every Code Map and probe anchor on `0959e883` apart from finding 5, including `CurrentOwnerMembership.cs:109` (`LEVEL-[0-9]{3}`), the census literal at `CurrentOwnerMembershipOracles.cs:69`, `CurrentOwnerRelations.cs:12–21,32,43,76`, the `level-069`/`level-070` anchors at `campaign.md:885`/`:896`, `completion-ledger.md:296,393`, `requirements.md:2527,3431`.
- Baseline behaviour per test: M1 and M3 fail at baseline and pass after; M4 passes on both (a boundary against over-widening); M5's positive reports `UnresolvedProofRole` at baseline and returns `LEVEL-069c` after; M5's control throws under both regexes; M6 holds in task order.
- `Coverage.Tests` has 115 cases at `0959e883`; the new cases are 1 + 3 + 5 + 2 = 11.
- The 190 real grant lines (all `S8xx` in `refinements.md`) still match the new role pattern.
- `git grep LEVEL-` over `*.cs`, `*.ts`, `*.json` finds only the two Ownership files; `docs/coverage/` and `docs/work-orders/` hold no `LEVEL-` id; play order is `presentation.order`.
- `Coverage.Tests` reaches `CurrentOwnerMembership.Parse` and `WorkId`; no project edit needed; neither the registration filter nor the hook's filter selects the new class; no planned command runs a Release build.
- Ordinal order holds and no code consumes it; ids stay typed; no alias, second grammar, fallback or skip; no element-specific engine code; engine tier correct; every probe capability cited and tested; no owner decision open; 3,096 words.
- Disjointness: no running lane edits `tools/Ownership`, `tools/Coverage` or `tools/Coverage.Tests`; `completion-ledger.md` is shared by row (CA-01 and OS-02 here, ES-37 and OS-05 in part D, OS-04 in the policy-tool fixes).
