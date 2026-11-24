---
title: 'Lettered campaign level ids are declared owners (tool slice, lane 3)'
type: 'refactor'
created: '2026-11-24'
status: 'done'
baseline_revision: '5f1c4d4a170f5eceedf16b68378b2befa096651d'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/planning/elements/readiness/campaign-coverage.md'
  - '{project-root}/_bmad/custom/bmad-loop-protocol.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** The campaign plan has 244 levels: the 150 numbered slots and 94 lettered levels (`9a`, `28a`–`28c`, `69c`, `55e`, …; owner decisions 24c to 24f, coordinator decisions 35 and 36). A lettered level's owner heading in `docs/planning/invest/campaign.md` would be `LEVEL-069c`. The ownership tool's identity grammar admits only `LEVEL-[0-9]{3}` (`tools/Ownership/CurrentOwnerMembership.cs:109`), while its heading filter (`:26`) and the oracle's census pattern (`CurrentOwnerMembershipOracles.cs:60`) already select such a heading. So the first lettered heading makes `CurrentOwnerMembership.Read` throw `Invalid current owner identity 'LEVEL-069c'`, and with it `CurrentOwnerRelations.Read`, `CapabilityInputs.Prepare` and every Coverage test that reads the current documents. The Coverage tool's grouped-role pattern (`tools/Coverage/CurrentOwnerRelations.cs:43`) also cannot name a suffixed owner. The content slice that authors lettered levels cannot start.

**Approach:** Widen the one grammar arm to `LEVEL-[0-9]{3}[a-z]?` and the grouped-role pattern to the owner grammar, keep the identity a whole typed `WorkId`, refuse every other spelling with the existing typed refusal, prove it in the full local round through the Coverage tool's real document path, and correct the planning documents that say the tool refuses the form. No heading and no content are added.

## Boundaries & Constraints

**Always:** A lettered level id is the number of the slot it follows, zero-padded to three digits, plus one lowercase ASCII letter (`LEVEL-069c`). It is carried as the existing strongly typed identities, `Ownership.WorkId` and `CuriousContraptions.Coverage.WorkOrderId`; level ids are an extensible identity set, so no enum. Campaign order is `LEVEL-069` < `LEVEL-069a` < `LEVEL-069b` < `LEVEL-070`, which is the ordinal order of the canonical spelling; no code consumes it. The refusal stays `InvalidDataException` with the message `Invalid current owner identity '<value>'.` Every other grammar arm is unchanged. Tests run the real `CurrentOwnerMembership` and `CurrentOwnerRelations` code. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A change to `content/puzzles.json` or any `LEVEL-` heading in `campaign.md` (later slices; the first is the content slice, completion-ledger OS-02). A new level type, comparer, sort, base-slot rule or document-order rule. A second grammar for lettered ids, an alias that maps `LEVEL-069c` to `LEVEL-069`, or a fallback that skips an invalid heading. A change to the census count 545. A local Release build. Output hashes, mutants, browser runs or skipped tests.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| M1 | The seven current invest documents with `### LEVEL-069z` (and its anchor) inserted after `LEVEL-069`'s entry; `z` is outside the planned letters `a`–`e` | `CurrentOwnerRelations.Read` succeeds; its members equal the unmodified copy's members plus `LEVEL-069z` and nothing else | No error expected |
| M2 | The same seven documents unmodified | The members contain every `LEVEL-001`–`LEVEL-150` and not `LEVEL-069z` | No error expected |
| M3 | A heading `LEVEL-001a`, `LEVEL-069z` or `LEVEL-150a` in a minimal document set with no numbered `LEVEL-` heading | Admitted as exactly that identity: the result is the fixture's set plus that id | No error expected |
| M4 | A heading `LEVEL-069C`, `LEVEL-069aa`, `LEVEL-069a1`, `LEVEL-69a` or `LEVEL-0069a` | Refused; nothing is returned | `InvalidDataException`, message `Invalid current owner identity '<value>'.` |
| M5 | An all-criteria grant line under `## LEVEL-069c` naming `LEVEL-069c` as its owner | The grouped proof role resolves to `LEVEL-069c` | A grant naming `LEVEL-069` under that heading is refused: `InvalidDataException`, "Grouped role owner differs from its current heading." |
| M6 | The ownership oracles without a root | The positive fixture includes `LEVEL-069c`; every existing attack is still rejected | An accepted attack or a changed positive set fails the oracle |

</intent-contract>

## Code Map

Delivery shape: single structural tool slice (no element, family or closure). Review tier: **engine**, because the owned diff changes tool projects (`tools/Ownership`, `tools/Coverage`) and adds a test to a tool test project, which are not on the data-only allow-list; the capability probe is below. Baseline: `5f1c4d4a170f5eceedf16b68378b2befa096651d`, the dispatch baseline (origin/main, which publishes owner decision 24m at `docs/planning/elements/readiness/owner-decisions-2026-11-22.md:67`); every scope guard and `git diff` compares against it. Drafted and line-checked on `0959e883` (origin/main, 24 November 2026), which is the drafting revision only. Lane 3 of owner decision 24m; the content slice follows it in the same lane.

**Status: review complete** (passes 1–4 and the Final diff scope check, Review Triage Log; final round in Verification). Remaining for the approver: SnapshotApproval of the committed SHA and the lane-3 publication under 24m. Planning verdict: **Scoped Pass** (ready-for-dev) per the second audit (`docs/planning/elements/readiness/audits/audit-tool-lettered-level-ids-2.md`, 24 November 2026), whose five non-blocking findings are applied; the first audit (`audit-tool-lettered-level-ids-1.md`, Incomplete) found 1–11, all applied. The shared-files and disjointness text was aligned with the coordinator ruling after the second audit (records only; no behaviour, task or acceptance changed). At dispatch every cited anchor was re-read on the baseline and matches; no cited code file and none of the seven invest documents changed between `0959e883` and the baseline, so the probe and the planning verdict carry over (run log, entry 1). Checkout: `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-lettered-level-ids`, branch `slice-lettered-level-ids`, lane 3. Implementation owner: agent `a7b84f34f7e0c9863` (general-purpose, launched without conversation history, inherited model; retained for every repair).

Correction to the dispatch brief: `:109` on `0959e883` reads `LEVEL-[0-9]{3}`, with no letter; adding `[a-z]?` is this slice's change.

Owned scope:

- `tools/Ownership/CurrentOwnerMembership.cs` -- `Identity` `:105–112`: the `LEVEL-[0-9]{3}` arm at `:109` becomes `LEVEL-[0-9]{3}[a-z]?`; the comment at `:107–108` names the campaign's lettered level beside the roadmap sub-slices.
- `tools/Ownership/CurrentOwnerMembershipOracles.cs` -- positive fixture `:28` gains `\n## LEVEL-069c` and the expected set `:30` gains `new("LEVEL-069c")`; the census comment `:67–68` adds that each `LEVEL-` heading a later slice adds raises 545 by one.
- `tools/Coverage/CurrentOwnerRelations.cs` -- the grouped-role pattern `:43`, `([A-Z0-9-]+)`, becomes the owner grammar, built from the shared `WorkOrderId.Grammar` (`CapabilityInventory.cs:39`; review repair P1), so a suffixed owner's grant is checked and granted; a clause line whose grant phrases are not exactly its one parsed grant is refused (review repairs P3 and P6).
- `tools/Coverage/CapabilityInventory.cs` -- review repair P1 (pass 1): `WorkOrderId`'s grammar becomes one internal constant that its constructor and the grouped-role pattern both use; P7 (pass 2) names it the owner-identity shape that `CurrentOwnerMembership.Identity` narrows per family.
- `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs` -- new; the four tests of the acceptance-to-test table (15 cases; the twelfth, a malformed grant, is review repair P3; the thirteenth, a malformed grant before a well-formed one, is P6; the fourteenth and fifteenth, a well-formed grant followed by a malformed or a second well-formed one, are P9).
- `docs/planning/elements/readiness/campaign-coverage.md` -- `:38` (the paragraph saying the tool accepts only `LEVEL-[0-9]{3}` and the CAMPAIGN slice extends it) and `:564` ("Not done": the two tool changes) restated as done, with the id form and the ordering rule.
- `docs/planning/invest/campaign.md` -- `:9`, the clause "a lettered level's `LEVEL-` entry is added when it is authored" gains the form (`LEVEL-069c`).
- `docs/planning/elements/readiness/completion-ledger.md` -- row CA-01 (`:296`): state column set to done, citing this slice, item text unchanged; row OS-02 (`:393`): item gains "adds the `LEVEL-` headings of the levels it places at lettered positions and raises the census literal".
- `_bmad-output/implementation-artifacts/deferred-work.md` -- one entry: every slice that adds a `LEVEL-` heading (first expected: the content slice OS-02, for relocated levels such as 11a) raises the census literal (`CurrentOwnerMembershipOracles.cs:70`) by the headings it adds and verifies it by recounting the seven documents with the oracle's census pattern (`CurrentOwnerMembershipOracles.cs:60`), as was done for 545 at `0959e883`, unless the coordinator rules that the Release library builds a root `--oracles .` needs are allowed (owner: that slice).
- `TODO.md`, this contract and `_bmad-output/implementation-artifacts/run-logs/spec-tool-lettered-level-ids.md`.

Read, not changed (cite in review): `tools/Ownership/CurrentOwnerMembership.cs:9–13,26` (`Read`, heading filter); `tools/Ownership/WorkId.cs:3`; `tools/Ownership/OwnershipOracles.cs:47,133–135` (calls the current-owner oracles; the policy check returns null without a root); `tools/Ownership/SourceInventory.cs:39–41,125–129` (the root run's Release rebuilds); `tools/Ownership/Program.cs:12,17`; `tools/Ownership/README.md:4,37`; `tools/Coverage/CurrentOwnerRelations.cs:12–21,32,76` (reads the seven documents through `CurrentOwnerMembership.Read`; sorts owners per key with `StringComparer.Ordinal`); `tools/Coverage/CapabilityInputs.cs:65`; `tools/Coverage.Tests/CurrentCapabilityInputsTests.cs:9–14,188–230` (`Root`, `CopyCurrentInputs`); `tools/Coverage.Tests/Coverage.Tests.csproj` (no Compile list: a new file needs no project edit); `engine/gpu/PuzzleContent.cs:9–18,38`, `engine/gpu/PuzzleDefinition.cs:151–153`, `engine/gpu/PuzzleCatalogue.cs:15,174–175` (content ids are snake_case `PuzzleId`s and play order is the integer `presentation.order`; no `LEVEL-` id is read); `tools/E2eRunner/Selection.cs:72,104` and `tools/E2eRunner/SuiteCatalogue.cs:31` (level ids are `puzzles.json` ids); `docs/planning/elements/readiness/palette-allocation.md:9` (rule 3: a lettered level sorts after its number).

### Capability probe

Measured on `0959e883`. No solver, ABI, wire or presentation capability is involved.

- **Narrower, owned here:** the identity grammar. `CurrentOwnerMembership.Identity` (`:109`) admits `LEVEL-[0-9]{3}` only; exercised by the oracle positive `LEVEL-001` (`CurrentOwnerMembershipOracles.cs:28,30`) and, on the real tree, by every Coverage test that reaches `CurrentOwnerRelations.Read` (for example `CurrentCapabilityInputsTests.NamedPackageClosureIsRequiredWithoutGrantingCompletion`, through `CopyCurrentInputs` and `CapabilityInputs.Prepare`). This slice widens it and adds M1–M4.
- **Narrower, owned here:** the grouped-role pattern (`CurrentOwnerRelations.cs:43`) cannot match a suffixed owner; exercised for unsuffixed owners by `CurrentCapabilityInputsTests.LinkCardinalityNeverGrantsProofAndRepeatedExplicitRolesCoalesce` (`:88–109`). Unreachable on today's documents (no "Exact source clauses" line in `campaign.md` or `engine.md`); this slice widens it and adds M5.
- **Present:** heading selection. `CurrentOwnerMembership.cs:26` selects `## LEVEL-…`/`### LEVEL-…` lines; exercised by the same real-tree tests (150 `LEVEL-` headings in `campaign.md`).
- **Present:** the Coverage typed identity. `WorkOrderId` (`CapabilityInventory.cs:41` at drafting; now `Grammar` `:39`, constructor `:43`) admits a lowercase letter after a digit; exercised by `CurrentCapabilityInputsTests.CurrentPreparationPreservesObligationsAndReportsAllUnresolvedRoles` (`:28–32`), which converts the real `ENGINE-CORE-2d` owner (`engine.md:200`) through `CurrentOwnerRelations.Read`.

Supplemental CLI check, not a capability this behaviour needs: the heading census (`CurrentOwnerMembershipOracles.cs:53–70`, literal 545 at `:69`), reached only through `OwnershipOracles.Run` (`OwnershipOracles.cs:47`) from `Program.cs:17` with a root. Re-counted on `0959e883`: engine 61, gpu-physics 29, refinements 197, decisions 8, scope-corrections 28, current-consumers 72, campaign 150. With a root, `OwnershipOracles.Run` also runs `CheckCurrentPolicy` → `SourceInventory.Capture`, a Release `-target:Rebuild -p:BuildProjectReferences=false` of the game, Animation and test projects (`SourceInventory.cs:39–41,125–129`), which needs Release outputs this workflow does not build (AGENTS.md: no local Release worker build). So this slice runs the oracles without a root (M6) and proves the real tree through M1.

Direct consumers of a lettered id: `CurrentOwnerRelations.Read` and `Parse` (Coverage) and `OwnershipAudit.Validate` (`Program.cs:23,43`), the first and last through `CurrentOwnerMembership.Read`. No consumer reads or counts the number or letter of a level id: `git grep -n "LEVEL-"` over `*.cs`, `*.ts`, `*.json` on `0959e883` finds only the two Ownership files. `docs/coverage/` and `docs/work-orders/` hold no `LEVEL-` id. No test counts `LEVEL-` ids.

### Decisions

- **D1. One grammar arm, single letter.** `LEVEL-[0-9]{3}[a-z]?`, not the roadmap form `[a-z][0-9]*`: the 94 planned lettered levels all use one letter, `a` to `e` (campaign-coverage section 6, counted on `0959e883`).
- **D2. Ordering is the spelling, not code.** No code consumes level order: the only sort is the per-key ordinal sort at `CurrentOwnerRelations.cs:76`, and play order is `presentation.order`. Zero-padded digits and one lowercase letter make ordinal order the campaign order; the rule describes the canonical spelling and adds no tested behaviour. Non-canonical spellings are refused because they are not canonical.
- **D3. Typed ids, no new type.** `WorkId` and `WorkOrderId` are the strongly typed identities (AGENTS.md typing rule); nothing reads a level's number or letter, so splitting them into fields would be unused structure.
- **D4. No base-slot rule.** Every numbered slot `LEVEL-001`–`LEVEL-150` is permanent (`campaign.md:9`: a removed level keeps its slot), and numbered ids are not range-checked today.
- **D5. Census unchanged here; owned by the slices that add headings.** No heading is added, so 545 stays. The root census run needs Release outputs; the slice that first adds a `LEVEL-` heading raises the literal and records how it ran the census (deferred-work entry above; owner decision 24m merge rule 3 names "oracle counts").
- **D6. Discriminating tests in `Coverage.Tests`.** The oracle is not in the full local round, so M1–M5 are xUnit tests there; the oracle's positive fixture gains one lettered heading so it covers every arm it admits (`campaign-coverage.md:564` asks for it). Its attacks are not duplicated.
- **D7. The grouped-role pattern takes the owner grammar.** A lettered owner is first-class in Coverage as well as Ownership; one pattern, the `WorkOrderId` one, replaces the narrower one.

No owner decision is open.

### What this removes

The refusal of a lettered level heading by `CurrentOwnerMembership.Identity`; the narrower grouped-role pattern at `CurrentOwnerRelations.cs:43`; the planning text that defers the grammar change to the CAMPAIGN slice (`campaign-coverage.md:38,564`). Nothing is kept for the old form: a numbered id keeps its one spelling.

## Tasks & Acceptance

**Execution:**

0. Before the first edit, on the baseline: run `dotnet run --project tools/Ownership -- --oracles` and record its exit code and `CurrentOwners` result in the run log. If it fails at the baseline, stop and return to planning.
1. `tools/Ownership/CurrentOwnerMembership.cs:109` -- `LEVEL-[0-9]{3}` → `LEVEL-[0-9]{3}[a-z]?`; comment `:107–108` -- M3, M4.
2. `tools/Coverage/CurrentOwnerRelations.cs:43` -- D7 -- M5.
3. `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs` -- the four tests below. The Parse cases use minimal valid engine and consumers documents in the test (an engine document with one owner heading such as `## ENGINE-CORE-1` and one P0 disposition row; one `CAT-` owner and its fixture row; no numbered `LEVEL-` heading), shaped like the oracle's fixtures at `CurrentOwnerMembershipOracles.cs:10–27`. The real-tree case copies the seven documents `CurrentOwnerRelations` reads to a new temporary directory, deleted at the end -- M1–M5.
4. `tools/Ownership/CurrentOwnerMembershipOracles.cs:28,30,67–68` -- the positive heading, the expected identity, the census comment -- M6.
5. `docs/planning/elements/readiness/campaign-coverage.md:38,564`, `docs/planning/invest/campaign.md:9`, `docs/planning/elements/readiness/completion-ledger.md:296,393`, `deferred-work.md` (D5's entry), `TODO.md` -- current documents.

**Acceptance Criteria:**

- AC1: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the full round at the baseline + 15 (all in `Coverage.Tests`, 115 + 15; 11 at drafting, plus review repairs P3, P6 and P9), re-measured at the baseline before the first edit; when this slice publishes second, the merged main's full round + 15.
- AC2: Given the candidate, when `dotnet run --project tools/Ownership -- --oracles` runs, then it exits 0 and its `CurrentOwners` result reports `Positive` true and `NegativeCount` 15, as at the baseline (task 0).
- AC3: Given the candidate diff, when its paths are listed, then each is in the Final diff scope; `git diff --quiet <baseline_revision> -- content/puzzles.json` exits 0; and `git diff <baseline_revision> -- docs/planning/invest/campaign.md | grep -c '^+#\{2,3\} LEVEL-'` prints 0. On a merged candidate, the path list and both commands compare against the merge's `origin/main` parent instead of `<baseline_revision>`.
- AC4: Given the candidate, when `git grep -n 'accepts only .LEVEL-\[0-9\]{3}.' -- docs/planning/elements/readiness/campaign-coverage.md docs/planning/invest/campaign.md` and `git grep -n 'Adding them needs two tool changes' -- docs/planning/elements/readiness/campaign-coverage.md` run, then neither lists anything.

## Implementation Notes

- Task 0 and probe: baseline oracles exit 0, `CurrentOwners` Positive true, NegativeCount 15; every probe entry matched the code before the first edit (run log, entry 2).
- Consumers edited: exactly the Code Map's owned scope; no other caller, schema, TypeScript, table or fixture changed.
- M2's coverage of `LEVEL-001`–`LEVEL-150` is `Assert.All(… Assert.Contains …)`, because the xUnit analyzer (xUnit2029, an error here) refuses `Assert.Empty(… Where(!Contains))`; M3 compares ordinal-sorted id arrays, equivalent to `SetEquals` for the duplicate-free sets and clearer on failure.
- Self-inspection of the full owned diff: no needless branch, duplicate rule, abstraction or churn; no simplification applied (run log, entry 2).
- Review repair (pass 1, P1–P5; run log, entry 4): `WorkOrderId.Grammar` is the one internal owner grammar, used by its constructor and the grouped-role pattern; a clause line whose grant names an owner outside that grammar is refused ("Malformed grouped role owner."); the M1 copy deletes its temporary root when a copy fails; `campaign.md:9` defines a lettered entry's anchor and profile groups and the 450 + 3-per-lettered-level group count; `campaign-coverage.md:564`'s closing sentence is scoped to its planning pass.
- Repair checks (run log, entry 4): incremental solution build 0 warnings, 0 errors; focused selection 87 passed, 0 failed, 0 skipped, including all 12 new-class cases; AC3, AC4 and `git diff --check` unchanged. The first-candidate capture, gate, registration and full-round results in Verification are superseded for the repaired candidate.
- Review repair (pass 2, P6–P8; run log, entry 6): a clause line is refused ("Malformed grouped role owner.") unless its grant-phrase count equals its parsed grants (at most one), so a malformed grant cannot hide behind a later well-formed one; all 190 real grant clause lines carry exactly one phrase. The `Grammar` comment now calls it the owner-identity shape that `Identity` narrows. The Code Map records are refreshed, and two spaces are restored in `campaign.md:9`.
- Repair checks (run log, entry 6): incremental solution build 0 warnings, 0 errors; focused selection 88 passed, 0 failed, 0 skipped, including all 13 new-class cases; AC3, AC4 and `git diff --check` hold.
- Review repair (pass 3, P9; run log, entry 8): M5 gains a well-formed grant followed by a malformed one and two well-formed grants, both refused ("Malformed grouped role owner."), so the one-grant-per-clause-line rule is pinned whichever grant comes first; the counts are +15 (7324).
- Repair checks (run log, entry 8): incremental solution build 0 warnings, 0 errors; focused selection 90 passed, 0 failed, 0 skipped, including all 15 new-class cases.

## Spec Change Log

## Review Triage Log

### 2026-11-24 — Review pass 1 (engine tier, all six layers; candidate = baseline + uncommitted owned delta; detail in run log entry 3)
- verdicts: 16 findings — high 0, medium 4, low 8, false 4, maybe-false 0
- findings:
  - `low` `patch` BH1 `campaign.md:9` "and450 level×profile groups" contradicts `:7` (real level count ×3×4, 2,928 cells at 244 levels); pre-existing, in an owned line — P2: state 450 for the 150 slots plus three per lettered entry (732 at 244).
  - `low` `patch` BH2 a lettered entry's anchor and profile-group names are undefined while every slot entry has both (`campaign.md:885–892`) — P2: state `level-069c` and `MATRIX-069c-BALANCED/FORGIVING/PRECISE` beside the id form.
  - `medium` `patch` BH3 (G1) the grant pattern `CurrentOwnerRelations.cs:44` is a hand copy of `WorkOrderId`'s grammar (`CapabilityInventory.cs:41`); a drift silently drops grants, the defect class this slice repairs — P1: one internal grammar constant on `WorkOrderId` used by both.
  - `low` `patch` ECH1 (G2) a clause line whose grant phrase carries an unparsable owner (`LEVEL-069aa.`) is skipped, so the document error surfaces only as a generic `UnresolvedProofRole`; pre-existing — P3: refuse it at parse time with `InvalidDataException`, plus a test case.
  - `low` `reject` ECH2 a lettered heading with no base slot (`LEVEL-151a`) is admitted — the intent's Never list forbids a base-slot rule (D4); numbered ids are not range-checked either.
  - `low` `patch` ECH3 `CurrentOwnerMembershipTests.cs:111–115` leaks the temporary directory when a copy throws after it is created — P4: delete it on failure, as `CopyCurrentInputs` does.
  - `low` `patch` ECH4 (G2) claim: the Approach's "refuse every other spelling" is not met for a malformed grant spelling — same root cause and fix as ECH1.
  - `false` `reject` IA1 M6 is not in the full round — by design (D6); executed by the CLI oracle on the candidate: `.tools/lettered-level-evidence/oracles-candidate.log`, exit 0, Positive true, NegativeCount 15.
  - `medium` `patch` IA2 (G1) the role capture is a textual copy of `WorkOrderId`, not a reference — grouped with BH3. Its behavioural reading (R2b) is the contract's D7 and ends in a typed refusal for every spelling `Identity` refuses.
  - `false` `reject` IA3 M5 uses a synthetic member set — the M5 row is defined at the grant line under a heading, which `CurrentOwnerRelations.Parse` is; M1 covers the real `Read` path.
  - `false` `reject` IA4 the M5 refusal case passes on the baseline too — it is the contract's control by design; the positive case discriminates (asserts no issue, which the baseline reports).
  - `false` `reject` IA5 `campaign.md:9` gains the spelling rather than a refusal correction — task 5 owns that clause; no heading is added (AC3 heading grep 0).
  - `medium` `patch` FO1 (G1) borderline duplicate grammar, classified maintainability defect, not a forward-only violation (`Identity` remains the one admission rule) — grouped with BH3.
  - `low` `patch` FO2 (G2) malformed grant skipped, classified not a violation (typed `UnresolvedProofRole`) — grouped with ECH1.
  - `low` `patch` EFE-F1 `campaign-coverage.md:564` ends "No build or test was run." after this slice's built and tested result — P5: scope the sentence to the planning pass.
  - `medium` `patch` EFE-S (G1) simplification candidate: share one pattern constant owned by `WorkOrderId` — grouped with BH3.
- Verification Gap: no gaps. Forward-Only: Pass (no violation). Element-Free Engine: tier engine confirmed, not Mis-tiered; E2, E5, E7, E8, E9 pass; D1–D7, E1, E3–E6 not applicable; no Fail or Incomplete apart from F1.
- Routing: no intent_gap, no bad_spec. Patches P1–P5 returned to the implementation owner; owned scope gains `tools/Coverage/CapabilityInventory.cs` (P1). Re-review: Blind Hunter, Edge Case Hunter, Verification Gap and both auditors on the repair diff.

### 2026-11-24 — Review pass 2 (re-review of P1–P5 on the repair diff from pass-1 tree `1c1a7167`; Blind Hunter, Edge Case Hunter, Verification Gap, both auditors; detail in run log entry 5)
- verdicts: 4 findings — high 0, medium 0, low 3, false 1, maybe-false 0
- findings:
  - `low` `patch` ECH-R2a `CurrentOwnerRelations.cs:46–48`: a clause line holding a malformed grant and then a well-formed one passes, because `Regex.Match` finds the second grant — P6: refuse a clause line unless its grant-phrase count equals its parsed grant count (at most one), plus a test case.
  - `false` `reject` ECH-R2b `CurrentOwnerMembershipTests.cs:119–123` a failing `root.Delete` could hide the copy failure — the directory is one the test just created, `File.Copy` releases its handles before throwing and the round runs only on macOS, where deleting such a file does not fail; the same pattern stands in `CopyCurrentInputs`.
  - `low` `patch` VG-R2 `CapabilityInventory.cs:36–38` calls `Grammar` "The one owner grammar" while `CurrentOwnerMembership.Identity` is the stricter admission rule — P7: comment names it the owner-identity shape that `Identity` narrows per family.
  - `low` `patch` EFE-R2-1 contract records stale after P1: `CapabilityInventory.cs` still listed as read-not-changed, the `CurrentOwnerRelations.cs` owned-scope bullet still describes an inline copy, the probe cites `:41` — P8: refresh the three records.
- Blind Hunter: no finding. Forward-Only: Pass, no finding. Element-Free Engine: tier engine confirmed; P1–P5 resolved; E2, E5, E7 pass on the repair; capture, gate, registration and full round on the repaired candidate pending. Its editorial note (missing spaces at `campaign.md:9`, "reservations.15", "slot.136–150") is not a finding and is fixed with P8 as a list of owned-line typos.
- Routing: patches P6–P8 to the implementation owner; re-review pass 3 under Re-review scope.

### 2026-11-24 — Review pass 3 (re-review of P6–P8 on the repair diff from pass-2 tree `74f6cd42`; Blind Hunter, Edge Case Hunter, Verification Gap, both auditors; Re-review scope applies; detail in run log entry 7)
- verdicts: 1 finding — high 0, medium 0, low 1, false 0, maybe-false 0
- findings:
  - `low` `patch` BH-R3 `CurrentOwnerMembershipTests.cs:88`: the P6 one-grant rule is tested only with the malformed grant first; a regression that anchors the grant match and drops the count would still pass while accepting a well-formed grant followed by a malformed or second grant — inside the repair diff, so repaired: P9 adds the reversed-order and two-well-formed-grants cases with their refusal.
- Edge Case Hunter: no finding (traced both orders and duplicate grants to the refusal). Verification Gap: no gap (noted the untested order, judged covered by the same count check). Forward-Only: Pass. Element-Free Engine: tier engine confirmed; P6–P8 resolved; E2, E5, E7, E8 hold; E9 and the final round pending; record note: the Verification results bullets still show the first candidate's figures until the final runs land.

### 2026-11-24 — Review pass 4 (re-review of P9 on the repair diff from pass-3 tree `7bf6bce3`; Blind Hunter, Verification Gap, both auditors; detail in run log entry 9)
- verdicts: 0 findings — high 0, medium 0, low 0, false 0, maybe-false 0
- Blind Hunter: no finding; Verification Gap: no gap; Forward-Only: Pass; Element-Free Engine: tier engine confirmed, BH-R3 resolved, no finding. Its observation that the M5 theory's name understates its five refusal cases is stated as not a finding (quality only; not on the guidance's reviewer checklist) and is recorded in the close note.

### Final diff scope (informed reviewer: the pass-4 Element-Free Engine auditor, re-engaged; run log entry 9)
- Candidate: baseline `5f1c4d4a` + the uncommitted owned delta after P9; coverage: all 12 owned paths of the full owned diff, against the intent, AC1–AC4, the owned scope and the confirmed repairs P1–P9. Every path and hunk serves a contract task or a confirmed repair; no churn, speculative abstraction or unrelated work. Simplification: no justified opportunity (sharing `Identity` with `WorkOrderId.Grammar` would merge two contracts and reverse the dependency; reusing the oracle fixtures needs intrusive access; merging the two regex calls is not simpler). Two stale records it named (`TODO.md` lane-3 line, the `:69` census citation) were corrected by the coordinator. **Pass.**

### Acceptance verdict
- Scoped Pass for implementation acceptance in the stated tool scope: Forward-Only Pass and Element-Free Engine with no open finding on every pass; all confirmed findings repaired and independently re-reviewed; maintainability gate PASS and the full local round 7324/0/0 with `Skipped: 0` on the final candidate (Verification). Deferred, not waived: browser qualification (post-catalogue E2E), the root `--oracles .` census (deferred-work D5 entry), remote build/artifact/deployment (unverified, owner instruction of 29 October); the normal pre-commit hook runs at the candidate commit.

## Design Notes

1. **Behaviour and scope.** A lettered campaign level id is a declared owner wherever owner ids are validated; numbered ids are unchanged. Source: owner decisions 24c–24f; completion-ledger CA-01; campaign-coverage `:38`. Related rows `requirements.md:2527,3431` (typed level identity for content and navigation, the `PuzzleId` side) stay open; this slice covers only the planning-owner id.
2. **Reusable primitive.** None new: the existing identity grammar and typed `WorkId`.
3. **Declared composition.** Owner headings in the seven invest documents remain the only declarations (`tools/Ownership/README.md:37`).
4. **Execution and ownership.** Tool and test code only; the tests write only their own temporary directory.
5. **Generality.** Representative ids (lowest and highest planned slot numbers, first and last letter), five refused near-miss spellings, and a real-tree heading (`LEVEL-069z`) that no planned level uses.
6. **Forward-only.** Deletes the refusal of the lettered form, the narrower role pattern and the stale planning text; no alias, second grammar or skip.
7. **Prerequisites.** Publication of owner decision 24m; no capability prerequisite. Engineering rules E2 (typed identities), E7, E8, E9 apply; E1, E3–E6 and the element-free design rules D1–D7 are untouched, because no engine, package, wire or presentation code changes.

## Verification

Executor for every command: the implementation owner. Raw output under `.tools/lettered-level-evidence/`; one line per result here, citing its run-log entry. A change to a tool source, test source or project file invalidates the builds and logs.

Environment: the pinned toolchain first on `PATH` (`PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403`); record the session environment check in the run log. Checkout: `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-lettered-level-ids`; start every shell command with `cd` into it and write nothing outside it (never the primary checkout or another worktree). Anvil validation uses `workspaceRoot` set to that checkout and workspace-relative paths. Full-round logs go under `~/.local/share/tim-toolchain/logs/slice-lettered-level-ids/`, each with `git rev-parse HEAD` as its first line; the capture builds export `CSHARP_STATIC_GATE_BINLOGS`. Other lanes may run full rounds on this machine at the same time. The implementation owner does not commit, merge or push; the coordinator commits the reviewed candidate. Baseline full round: 7309/0/0, measured on `37fbd3a4`; no `*.cs`, `*.csproj`, `*.slnx`, `*.ts`, `*.json`, `*.props` or `*.targets` file differs between it and the baseline, so it is the baseline measurement and AC1 expects 7324 on the unmerged candidate (7309 + 15; run log, entries 1, 4, 6 and 8).

### Acceptance-to-test table

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| M1, M2 | `CurrentOwnerMembershipTests.ALetteredLevelHeadingInTheCampaignDocumentIsADeclaredOwner` (fact): copy, insert `<a id="level-069z"></a>` and `### LEVEL-069z` immediately before `<a id="level-070"></a>`, `CurrentOwnerRelations.Read`; its `Members` equal `Read(unmodified copy).Members` ∪ {`LEVEL-069z`} | Control (M2, same fact): the unmodified copy's members contain every generated `LEVEL-001`…`LEVEL-150` and not `LEVEL-069z`. Whole-set equality catches an alias to `LEVEL-069`. On the baseline the inserted copy throws, naming `LEVEL-069z` | focused-1 and full-round-1: passed (run log, entry 2) |
| M3 | `CurrentOwnerMembershipTests.AWellFormedLetteredLevelIdentityIsAdmitted` (theory: `LEVEL-001a`, `LEVEL-069z`, `LEVEL-150a`): `Parse` returns exactly the fixture's expected set ∪ {that `WorkId`} (`SetEquals`) | Representative ids: lowest and highest planned slot, first and last letter | focused-1 and full-round-1: 3 cases passed |
| M4 | `CurrentOwnerMembershipTests.AMalformedLetteredLevelIdentityIsRefusedNamingIt` (theory: `LEVEL-069C`, `LEVEL-069aa`, `LEVEL-069a1`, `LEVEL-69a`, `LEVEL-0069a`): `Parse` throws `InvalidDataException` whose message equals `Invalid current owner identity '<value>'.` | Each differs from an admitted M3 spelling in one cause: case, letter count, an ordinal, digit count (two, four) | focused-1 and full-round-1: 5 cases passed |
| M5 | `CurrentOwnerMembershipTests.AGroupedRoleUnderALetteredLevelResolvesToThatLevel` (theory, 6 cases): members {`LEVEL-069c`}, document `## LEVEL-069c` then one line holding `Original owner, all criteria and technical stage: LEVEL-069c.` and one exact-source-clause link (the form at `CurrentCapabilityInputsTests.cs:91,97`); `ResolveProof` returns `LEVEL-069c` with no issue. On the baseline this case reports `UnresolvedProofRole` | Control case: the same grant naming `LEVEL-069` throws `InvalidDataException` "Grouped role owner differs from its current heading." Malformed-grant case (review repair P3): a grant naming `LEVEL-069aa` throws `InvalidDataException` "Malformed grouped role owner." Second-grant case (review repair P6): a malformed grant followed by a well-formed one (`LEVEL-069aa. Original owner, all criteria and technical stage: LEVEL-069c`) throws `InvalidDataException` "Malformed grouped role owner." Later-grant cases (review repair P9): a well-formed grant followed by a malformed one (`LEVEL-069c. Original owner, all criteria and technical stage: LEVEL-069aa`) or by a second well-formed one (`… LEVEL-069c`) throw `InvalidDataException` "Malformed grouped role owner." | focused-4: 6 cases passed (focused-3 ran the first 4; focused-2 the first 3; focused-1 and full-round-1 the first 2) |
| M6, AC2 | `Ownership --oracles` (Verification command) | Existing 15 current-owner attacks; baseline run of task 0 | oracles-candidate: exit 0, Positive true, NegativeCount 15 (baseline identical) |
| AC1 | Full local round | Totals against the baseline round | full-round-2 on the final candidate: 7324/0/0, `Skipped: 0` (7309 + 15; Coverage.Tests 130 = 115 + 15); full-round-1 (7320, first candidate) superseded |

No float is compared; every assertion is a set, an identity or a refusal message.

Results on the final candidate (executor: the coordinator, the designated final-round executor; candidate: baseline + the uncommitted owned delta after P9; logs under `~/.local/share/tim-toolchain/logs/slice-lettered-level-ids/`; run log, entry 9). The first candidate's results (entry 2) are superseded.

- Compiler-proof capture (`capture-c2.log`, binlogs `binlogs-c2`): five binlogs, `CAPTURE_OK`, 0 warnings, 0 errors. Maintainability gate (`maintainability-2.log`): both canary PASS lines, `Coupling dependencies: contract=4671, model=4, intrusive=0; violations=0`, violations 0 (metrics 0, coupling 0), PASS.
- Registration check (`registration-2.log`): 767 passed, 0 failed, 0 skipped, unchanged. Focused selection (`.tools/lettered-level-evidence/focused-4.log`, implementation owner): 90 passed, including the 15 new cases. AC2: `oracles-candidate.log` applies (no `tools/Ownership` change since it ran). AC3: the scope is as listed; `puzzles.json` is unchanged; 0 added `LEVEL-` headings. AC4: both greps are empty. `git diff --check 5f1c4d4a`: clean.
- Full local round (`full-round-2.log`): WorkerAotGuard 17, BundleElements 287, Coverage 130, E2eRunner 256, CuriousContraptions.tests 6634; 7324 passed, 0 failed, `Skipped: 0`, exit 0 (AC1).
- Invalidated by any change to a tool source, test source, project file or the seven invest documents.

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, with `CSHARP_STATIC_GATE_BINLOGS` exported -- expected: five logs, zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: both canary `PASS` lines, the `Coupling dependencies:` summary, zero violations. Run as soon as the candidate compiles and again at handoff.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` (a test file is added) -- expected: its count at the baseline, unchanged (the new class is not in its filter), 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~CurrentOwnerMembershipTests|FullyQualifiedName~CurrentCapabilityInputsTests|FullyQualifiedName~CurrentCatalogueTests"` -- expected: 0 failed, 0 skipped; the fifteen new cases listed.
- `dotnet run --project tools/Ownership -- --oracles` -- at the baseline (task 0) and on the candidate: AC2.
- The AC3 and AC4 commands -- expected as stated.
- `git add -N` the new test file, this contract and its run log, then `git diff --check <baseline_revision>` -- expected: no output. Whoever stages the candidate replaces the test file's intent-to-add entry with `git add` (`tools/anvil/CheckStagedTests.cs:35`).
- One full local round under `caffeinate -i` -- expected: AC1.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: its selection at the baseline count, 0 failed, 0 skipped.

### Running beside other work

Prerequisite: the coordinator's planning edit that records owner decision 24m, its merge rule and the coordinator ruling after it (uncommitted in `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` at drafting) is published, and the baseline is captured from that main.

Final diff scope: `tools/Ownership/CurrentOwnerMembership.cs`, `tools/Ownership/CurrentOwnerMembershipOracles.cs`, `tools/Coverage/CurrentOwnerRelations.cs`, `tools/Coverage/CapabilityInventory.cs` (review repair P1), `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs` (new), `docs/planning/elements/readiness/campaign-coverage.md`, `docs/planning/invest/campaign.md`, `docs/planning/elements/readiness/completion-ledger.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `TODO.md`, this contract and its run log. Nothing else.

Disjointness, checked against the owned-scope lists on `0959e883` (plus the worktree drafts). Code: `tools/Ownership`, `tools/Coverage` and `tools/Coverage.Tests` are edited by this lane alone. Shared documents, under the coordinator ruling recorded after owner decision 24m (shared policy and workflow documents are shared documents, not lane code): `completion-ledger.md`, `deferred-work.md` and `TODO.md`. A line citation that another slice moves in this contract (for example `tools/anvil/CheckStagedTests.cs:35`, moved by lane 4) is resolved at each later publisher's merge: that publisher re-reads the cited lines on the merged commit and its full round runs the changed gates (24m merge rule 3).

- **S3** (`spec-s3-remaining-per-package-lists.md`): code disjoint (S3 owns `CuriousContraptions.tests/*` and `docs/delivery-workflow.md`). Shared: `deferred-work.md`, `TODO.md`. Shared measured total: the full local round (S3 + 8, this slice + 15); the later publisher re-measures on the merged candidate.
- **S5** (`spec-s5-generate-wire-accessors-in-build.md`): code disjoint (S5 reads `tools/Ownership/SourceInventory.cs:13–16,125–129` and does not change it; this slice does not touch that file). Shared: `deferred-work.md`, `TODO.md`; the full-round total.
- **Visible-solid parts B, B2, C**: code disjoint (packages under `elements/`, `VisibleSolidColliderTests.cs`, CAT documents). Shared: `deferred-work.md` (B, C), `TODO.md`; the full-round total.
- **Part D (after the engine programme; not a running lane); the Baseball repair (lane 1); S5b, S6, S6b–S6g (lane 2)**: code disjoint (checked on the drafts; none edits `tools/Ownership`, `tools/Coverage` or `tools/Coverage.Tests`). Part D shares `completion-ledger.md` (its rows, not CA-01 or OS-02); all share `deferred-work.md`, `TODO.md` and the full-round total.
- **Policy-tool fixes (`spec-policy-tool-fixes.md`, lane 4)**: code disjoint (`tools/anvil/` belongs to lane 4 alone; the `_bmad/custom/*.md` and `docs/delivery-workflow.md` it edits are shared documents this slice does not touch). Shared: `completion-ledger.md` (this slice rows CA-01 and OS-02, that slice row OS-04), `deferred-work.md`, `TODO.md`. It adds `CurrentCapabilityInputsTests` to the registration filter; that changes the registration count, not this slice's new class, and whichever publishes second re-measures it on the merged candidate. It moves `CheckStagedTests.cs:35`, which this contract cites; resolved at the later publisher's merge.
- **The content slice (OS-02)** runs after this one in lane 3 and owns `content/puzzles.json`, the first `LEVEL-` headings and the census literal.

One publisher at a time under the owner decision 24m merge rule: the later publisher merges `origin/main` once, keeps both sides' entries in the shared documents, and repeats the full local round on the merged candidate.

## Auto Run Result

- Summary: a lettered campaign level id (`LEVEL-069c`) is a declared owner: `Identity`'s one level arm admits `LEVEL-[0-9]{3}[a-z]?`, every other spelling keeps the typed refusal, and the Coverage grouped-role pattern is built from the shared `WorkOrderId.Grammar`, refusing a clause line whose grant does not parse or that carries more than one grant. No heading or content was added.
- Files: `CurrentOwnerMembership.cs` (grammar arm), `CurrentOwnerMembershipOracles.cs` (lettered positive case, census comment), `CurrentOwnerRelations.cs` (grant pattern from the shared grammar, one-grant refusal), `CapabilityInventory.cs` (`WorkOrderId.Grammar`), new `CurrentOwnerMembershipTests.cs` (4 tests, 15 cases, M1–M5), `campaign-coverage.md`, `campaign.md`, `completion-ledger.md` (CA-01 done, OS-02 duty), `deferred-work.md` (census entry), `TODO.md`, this contract and its run log.
- Review: four passes, 21 finding rows: 15 confirmed and patched through P1–P9, 1 rejected by the intent's Never clause (ECH2, base-slot rule), 5 refuted (IA1, IA3, IA4, IA5, ECH-R2b), none deferred. Final diff scope Pass.
- Follow-up review recommended: false (the first pass patched no high entry and one medium entry, the duplicated grammar; later passes patched no high).
- Verification: capture 0 warnings and 0 errors; maintainability gate PASS; registration 767/0/0; full local round 7324/0/0, `Skipped: 0`; oracles exit 0, NegativeCount 15; AC3 and AC4 hold.
- Residual risks: the baseline failures of M1 and M5 were reasoned, not executed (no mutants); the root census is not run until a slice adds a `LEVEL-` heading (deferred-work); the pre-commit hook runs at the commit; browser and remote qualification remain unverified.
