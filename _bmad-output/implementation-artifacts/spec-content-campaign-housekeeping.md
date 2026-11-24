---
title: 'Campaign content housekeeping: remove the 29 repeat levels, place the 34 kept levels (content slice, lane 3)'
type: 'refactor'
created: '2026-11-24'
status: 'done'
baseline_revision: 'b64e145323f6e15249781ff56946553d04fdfd8b'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/planning/elements/readiness/campaign-coverage.md'
  - '{project-root}/_bmad/custom/bmad-loop-protocol.md'
warnings: ['oversized']
deferred: ['DF-1 domino_effect re-authoring (ledger OS-06)', 'DF-2 Playable picker order (OS-07)', 'DF-3 conversions of double_cold_start and grand_contraption (OS-08)', 'DF-4 Rope anchor in pulley_depth, PR-24 (OS-09)', 'DF-5 Batch M levels 17, 25, 29, 49, 79, 89 (OS-03)', 'Review pass 3: CurrentOwnerRelations.cs document list (deferred-work.md)']
---

<intent-contract>

## Intent

**Problem:** Owner decision 24d removes 29 Planned levels of `content/puzzles.json` as repeats of an already introduced mechanic, and 24e/24f place the 34 kept levels at plan levels, 21 of them lettered (`11a`, `3a`, `51c`, …) ([campaign coverage, section 5](../../docs/planning/elements/readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)). The content still holds all 63 levels at their authored positions, the fixture owner table in `current-consumers.md` still declares the 182 fixtures of the removed levels, two levels share the title "Around the corner", `grand_contraption` keeps the title reserved for level 150, and `campaign.md` has no `LEVEL-` entry for any lettered level that holds authored content.

**Approach:** Housekeeping only, nothing proven solvable: delete the 29 entries and their 182 `FIX-` rows together; give each kept level its plan level in its `subtitle`; retitle `grand_contraption` and rename `pulley_depth`; add the 21 lettered `LEVEL-` entries and raise the heading census to 566; restate the current documents and every test that counts or names levels. Re-authoring, conversions, picker order and new levels are deferred (DF-1 to DF-5, Code Map): each must be authored where it can be proven, needs a browser, or needs goal evaluation or undelivered parts, all after the engine programme (owner decision 22).

## Boundaries & Constraints

**Always:** Every kept entry keeps every field except the plan-level prefix of `subtitle` (33 change; `first_principles` is already `01`) and `title` (`pulley_depth`, `grand_contraption`). Content and the fixture table change together, so `CurrentCatalogue.ValidateFixtures` (`tools/Coverage/CurrentCatalogue.cs:187–200`) keeps exact set equality. Surviving `FIX-` ids keep their spelling (no renumbering). A lettered entry uses the canonical form `LEVEL-011a`, anchor `level-011a`, groups `MATRIX-011a-BALANCED/FORGIVING/PRECISE` (`campaign.md:9`). New tests read the real files through the production readers. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A change to any field of `domino_effect`, `double_cold_start`, `grand_contraption` or `pulley_depth` other than those named above; a change to any `presentation.order`, to `DominoEffectTests.cs`, to the `domino_effect/fixture_3` lamp row, or to anything under `tools/e2e/`; a new level, goal, placement or Playable promotion; a renumbered `FIX-` or `LEVEL-` id; an alias, redirect or "removed level" placeholder entry; an edit to `docs/coverage/**` or to evidence rows pinned to a revision (`@a6c914e`), except the description cell of `CAT-053-pulley.md:84` (D5); any shell-command write to a tracked repository file other than the one 24o(1) filter, which only deletes the 29 entries; a local Release build; output hashes, mutants, browser runs or skipped tests.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| H1 | `PuzzleContentReader.ReadIndex` over the candidate file | Exactly the 34 kept ids, each with its baseline availability (15 Playable, 19 Planned); none of the 29 removed ids | No error expected |
| H2 | Each kept entry's `subtitle` | Its prefix before ` / ` is its plan level from campaign-coverage section 5 (`:159–192`), two digits plus an optional letter (`03a`, `11`, `51c`); the topic after ` / ` is the baseline's | No error expected |
| H3 | All titles of the index | Pairwise distinct; `grand_contraption` is "One thing leads to another", `pulley_depth` is "The long way round", `quarter_bend` keeps "Around the corner" | No error expected |
| H4 | `PuzzleCatalogue.Playable` | The same 15 ids in the same order as the baseline (no `presentation.order` changes) | No error expected |
| H5 | `CurrentCatalogue.Read` and `RequirementDiscovery.Discover` on the real tree | Read succeeds with 122 fixtures (78 Planned parts + 44 Playable placements); discovery reports 1293 sources, 122 of origin Fixture | A `FIX-` row left for a removed level throws `InvalidDataException` "Authored fixture membership differs." (`:198`); a removed level left in content with its row deleted throws "Fixture content differs from current declared identity, kind or locked state." (`:194–195`) |
| H6 | `CurrentOwnerMembership.Read` on the real tree | Its `LEVEL-` members with a letter are exactly the 21 placed lettered levels; members lose the 182 removed `FIX-` ids and `CAT-015-V` (its only fixture row is `wall_return/bumper`) | No error expected. The heading census of 566 is executable (coordinator decision, 25 Nov 2026): one public census entry point in `tools/Ownership` counts headings by the oracle's rule against one shared constant, and both the root oracle run and a Coverage test on the real tree call it; the AC2 recount agrees |

</intent-contract>

## Code Map

Delivery shape: single content slice (no element, family or closure). Review tier: **engine**: the owned diff edits a tool source literal (`tools/Ownership/CurrentOwnerMembershipOracles.cs`), a tool test project (`tools/Coverage.Tests`), `TODO.md` and planning documents beyond the slice record, none on the data-only allow-list (`_bmad/custom/bmad-loop-protocol.md`, Review tiers); probe below. Baseline: captured at dispatch (coordinator decision 38) from the first published main after `2ce73eed` that contains owner decision 24o and ledger rows OS-02, OS-03 and OS-06 to OS-10 (if task 0 does not find them, stop); every scope guard and `git diff` compares against it. Drafted and line-checked on `2ce73eed`, the drafting revision only. Lane 3 of owner decision 24m, after lettered level ids.

**Planning verdict: Scoped Pass with non-blocking findings** (round 5, `docs/planning/elements/readiness/audits/audit-content-housekeeping-5.md`, 24 November 2026); earlier rounds and the `515e1b98` SnapshotApproval repairs: run log, entry 15. Owner decision 24o settles the open decisions below.

**Settled decisions (owner decision 24o, `owner-decisions-2026-11-22.md:70`, 24 November 2026):**

- **24o(1), the content removal method.** One recorded `python3` filter deletes the 29 entries (Write and Edit cannot carry 21,068 lines), after a no-op run proves `cmp`-equal serialisation; histogram numstat `0 21068` after it and `35 21103` after the 35 field Edits; every other write uses Write or Edit after Anvil (full text: run log, entry 15).
- **24o(2), the fifteen springboard instances.** `requirements.md:186` ("all fifteen composite springboard instances") and `:5998` (EL-194, "all fifteen composite lane instances") are reworded by this slice to the surviving instance and the campaign levels that teach the springboard (exact text in the owned scope); completion-ledger OS-10 records that decision 24d closed the other 14. `EL-194-springboard.md:19`, `:54`, `:113` and `CAT-062-spring.md:166`, which quote or restate the criterion, follow the new wording.
- **24o(3), the deferred parts.** DF-1, DF-3 and DF-4 wait for the engine programme as Early level proof slices (owner decision 22); DF-2 waits for the post-catalogue E2E phase; DF-5 waits for its goal kinds and parts.
- **Coordinator ruling under 24o.** Element documents edited by two lanes are shared documents under 24m, resolved by keeping both sides' edits at the later publisher's merge: here `CAT-019-conveyor.md` `:67`, `:147`, `:160`, `:208` (lane 1 part B) and `CAT-064-tennis.md:94` (lane 1 B2).
- **Ledger rows.** OS-02 (this slice), OS-03 (re-owned), OS-06 to OS-09 (DF-1 to DF-4), OS-10 (the springboard closure), PR-08, PR-24 and OS-01 were written by the coordinator at planning (`completion-ledger.md:365`, `:381`, `:392–401` in the planning worktree); task 0 checks that they are on the baseline.
- **Carried receipts for task 0b**: `2ce73eed` full round 7332/0/0, registration 775/0/0, SnapshotApproval Pass, fast-forward push (full text: run log, entry 15).
- **Coordinator decisions, 25 Nov 2026** (review pass 1 intent conflicts; run log, entry 5): (1) census, option (b): H6 amended so the 566 census is executable through one minimal public entry point used by the oracle and a Coverage test (D6); AC1 becomes 7339. (2) Pinned `@a6c914e` rows (`CAT-066:86`, `CAT-005:167`, `EL-194:63–64`), option (a): kept as commit-pinned historical evidence (AGENTS.md; AC6, AC7); the Never clause is unchanged. (3) `docs/coverage/**` and `general-engine-element-map.md:17`, option (a): not regenerated here; the consumer fact names their readers, and a `deferred-work.md` entry owned by the engine programme's coverage reconciliation records them stale until regeneration; the Never clause is unchanged.

### Readiness split

| Id | Deferred part | Why it cannot run now | Owner | Gate |
|---|---|---|---|---|
| DF-1 | Re-author `domino_effect` (lamp `fixture_3` and its wire removed; goal Captured: the last domino knocks a ball into a Receiving basket), with `DominoEffectTests`, the `domino_effect/fixture_3` row and CAT-035-I/CAT-023-I lines, the content of `CAT-023:121`, `CAT-035:117`, `EL-193` | A new layout of a Playable level that must be proven solvable (Early-level-proof class, `delivery-batches.md:215–223`) | the `domino_effect` Early level proof slice (OS-06; 24o(3)) | engine programme published (`ENGINE-SOA-SIMD`) |
| DF-2 | Playable picker order (`presentation.order`) set to campaign order, with the browser driver's picker anchors | The anchors (`tools/e2e/workshop-driver.ts:225–237`) are pixel positions by picker order that only a browser can re-measure, and lane 2's S5b owns that file (`:9`) | the post-catalogue E2E phase (order and anchors together; OS-07) | post-catalogue E2E phase (no browser test runs or gates before it, AGENTS.md) |
| DF-3 | Convert `double_cold_start` (19b, one chain keeping `powered_after`) and `grand_contraption` (20, one chain ending in a lit lamp) | Planned content is indexed but never constructed (`PuzzleCatalogue.cs:10`; `delivery-batches.md:217`), so a conversion now proves nothing; it is authored once, in the Playable shape, where it is proven, which needs ENGINE-GOALS (`delivery-batches.md:234`) | their Early level proof slice ("after the goal slices"; OS-08; 24o(3)) | engine programme and ENGINE-GOALS published |
| DF-4 | Author the Rope anchor into `pulley_depth` (12c; PR-24, CAT-058:157) | As DF-3: authored once where proven; the solid-Pulley weight start (`deferred-work.md`, `pulley-lesson-weight-start`) is owned there | the Early level proof "Ropes" group slice (OS-09; 24o(3)) | engine programme published |
| DF-5 | Author levels 17, 25, 29, 49, 79 and 89 (OS-03) | GAP-16 and GAP-18 goal and environment kinds; parts EL-207, EL-067, EL-065 (17), EL-059 (25), TH-13 (79) undelivered | each level's Epic 15 chapter slice (OS-03) | ENGINE-GOALS, the GAP-18 environment story and each level's parts published |

The remaining 73 lettered `LEVEL-` entries stay with the slices that author those levels (`deferred-work.md:357–363`).

### Owned scope

| Path | Lines on `2ce73eed` | Change |
|---|---|---|
| `content/puzzles.json` | the four removed entry ranges, the 33 changed `subtitle` lines and the two `title` lines (line numbers on `2ce73eed`: run log, entry 18) | 24o(1) filter: delete the 29 entries; then Edit after Anvil: subtitle prefix = plan level, two titles |
| `docs/planning/invest/current-consumers.md` | the 182 `FIX-` rows of removed levels and the 14 "Level(s) unlocked" lines (line numbers on `2ce73eed`: run log, entry 18) | delete rows; drop removed ids from the lists (`146` becomes inventory only) |
| `docs/planning/invest/campaign.md` | title `:1` (the 150 slots and 21 lettered entries; 513 groups, 2,052 retained placement cells); header `:9` (34 authored, 29 removed, groups 450 + 3 × 21 = 513); the 62 "Placement under owner decisions 24d to 24f" paragraphs (`18`, `31`, … `811`) and 62 "Currently authored at position" paragraphs (`20`, `33`, … `813`); 21 new entries | each slot entry gets one present-tense placement paragraph (authored level at this slot, or none; a removed level named once with its section 5 link; links to its lettered entries); the "Currently authored" paragraph is deleted; entries for 3a, 9a, 11a–11d, 12a–12c, 14a–14c, 19a, 19b, 21a, 22a, 23a, 41a, 51a–51c inserted after their slot's entry in letter order, each shaped like `LEVEL-011` (`:143–154`): anchor, heading, decision boundary, authored level, criteria and profile-group line, chapter reservation link |
| `tools/Ownership/CurrentOwnerMembershipOracles.cs` | `:53–71` | census 545 → 566; the oracle calls the public census entry point and compares with the shared constant; comment names this slice's 21 lettered headings |
| `tools/Ownership/CurrentOwnerMembership.cs` | new member | the public census entry point and the shared census constant (coordinator decision 1, D6) |
| `tools/Coverage.Tests/CurrentCatalogueTests.cs` | `:61–62` | 304 → 122; comment "78 Planned parts plus 44 Playable placements" |
| `tools/Coverage.Tests/CurrentCapabilityInputsTests.cs` | `:34`, `:36`, `:69` | 1475 → 1293; 304 → 122; 1475 → 1293 |
| `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs` | two new facts | H6 (membership; census, coordinator decision 1) |
| `CuriousContraptions.tests/CampaignContentTests.cs` | new | H1–H3 (four tests, 5 cases) |
| `docs/planning/elements/readiness/campaign-coverage.md` | `:33`, `:38` (census 566 after this slice), `:40`, `:149`, `:151`, `:155` (placement is the subtitle and the `LEVEL-` entry; picker order is DF-2), `:164`, `:178–179`, `:303–304`, `:559`, `:564` | removals, renames and 21 headings done; DF-1 to DF-5 named with owner and gate; "Not done" restated. The owner pointers to OS-06, OS-08 and OS-09 at `:40`, `:288` and `:564` were written at planning (24o(3)); the slice keeps them and restates only what it completes |
| `docs/planning/invest/delivery-batches.md` (shared, §4 and the Counts line `:352`) | `:207`, `:209–213`, `:225–234`, `:352` | 34 levels (15 Playable, 19 Planned) and the counts over them; level numbers become plan levels |
| `docs/planning/requirements.md` | `:186`, `:5998` | 24o(2): "all fifteen composite springboard instances" (`:186`) and "all fifteen composite lane instances" (`:5998`) each become "the springboard lessons `spring_forward` (plan level 2), `ready_to_rebound` (3a) and `spring_signal` (11c), and the surviving composite springboard instance (`double_cold_start` lane 2, plan level 19b, until its conversion, completion-ledger OS-08; decision 24d closed the other 14, OS-10)"; nothing else in either line changes |
| `docs/springboard-elastic-contract.md` | `:29` | "all fifteen composite springboard lane instances" becomes the same 24o(2) set as `requirements.md:5998`, which links this contract |
| Element documents (22), current-state lines only | `CAT-005`, `CAT-015`, `CAT-019` (shared with part B, 24o ruling), `CAT-028`, `CAT-035`, `CAT-042`, `CAT-053` (`:84` description cell only), `CAT-062`, `CAT-063`, `CAT-064` (shared with B2), `CAT-066`, `EL-186`, `EL-189`, `EL-191`, `EL-194`, `EL-195`, `EL-196`, `EL-199`, `EL-202`, `EL-209`; owner pointer only: `CAT-023:141`, `EL-193:116` (DF-1, DF-3); line-by-line detail at planning: run log, entry 15 | values re-counted from the candidate content with `current-consumers.md`; lists keep their authored-position labels, marked once as "authored positions" in each restated line (D5), and drop removed levels |
| `_bmad-output/implementation-artifacts/deferred-work.md` (shared) | `:357–363` | the census entry records 21 headings added (566) and keeps the duty for the other 73 |
| `docs/planning/elements/readiness/completion-ledger.md` (shared, by row) | OS-02; OS-10 (review pass 1 repair) | OS-02 state set to done at close, citing this contract; OS-10 state records the 24o(2) wording this slice applied |
| Review pass 1 repairs (Spec Change Log, 25 November 2026) | `docs/planning/elements/CAT-014-bowling.md:14`, `:140`; `current-consumers.md` CAT-003-I and CAT-023-I "Level(s) unlocked" lines | bowling's placed and inventory levels, and the balloon and domino lines, recounted from the candidate content |
| Review pass 3 repair (Spec Change Log, 25 November 2026) | `docs/planning/elements/EL-211-solar-panel.md:115` | its "positions 21, 22 and 24 … in the 63-level content file" restated as authored positions before decision 24d, with ids and the section 5 link, like `CAT-042:225` |
| Review pass 4 repairs (Spec Change Log, 25 November 2026) | `docs/planning/elements/EL-203-reverse-transmission.md:105`; `ui/Workshop.cs:18`, `:136` (was `:134`), `ui/WorkshopPuzzle.cs:50` | EL-203's "provable now" restated as `EL-202:109`; the picker's built-in "Free workshop" label named once as a production constant used at both sites and by the H3 picker-title check |
| `_bmad-output/implementation-artifacts/run-logs/spec-tool-lettered-level-ids.md` | appended entry | task 0b: the carried receipts |
| `TODO.md` (shared) | `:8` | lane 3 line: lettered level ids published as `2ce73eed`; this slice |
| this contract and `run-logs/spec-content-campaign-housekeeping.md` | new | slice record |

Read, not changed (cite in review): the content, catalogue, fixture and ownership readers and the tests that pin them; list with lines: run log, entry 15.

### Capability probe

Measured on `2ce73eed`. No solver, ABI, wire or presentation capability is involved; every capability is present:

- Content index: `PuzzleContentReader.ReadIndex` (`PuzzleContent.cs:28`); exercised by `WorkshopSaveTests.ContentLoadRejectsEmptyDuplicateAndNoPlayableCatalogue`.
- Playable catalogue and its order: `PuzzleCatalogue.ReadDefinitions` (`PuzzleCatalogue.cs:161`); exercised by `LaunchLessonSolveTests.EveryPlayableLessonHasAnExplicitExecutedScenario`.
- Fixture binding: `CurrentCatalogue.ValidateFixtures`; exercised by `CurrentCatalogueTests.ActualCurrentContractRetainsAllIdentitiesModesAndFixtures`.
- Lettered owner headings: `CurrentOwnerMembership.Identity` (`:111`); exercised by `CurrentOwnerMembershipTests.ALetteredLevelHeadingInTheCampaignDocumentIsADeclaredOwner`.
- Heading census: `CurrentOwnerMembershipOracles.cs:53–71`, root-only; recount 545 on `2ce73eed` (engine 61, gpu-physics 29, refinements 197, decisions 8, scope-corrections 28, current-consumers 72, campaign 150). Present, untested (review pass 1, VG1 and EFE F1): this slice adds the exercising test (D6).

Consumer facts: no code, test or tool names a removed or converted level id (`rg -w` over `*.cs`, `*.ts`); `docs/coverage/**` (which names them) has no reader in tests, `.github/workflows/pages.yml` or `.githooks/pre-commit`; its readers are the manual `Coverage audit-*` commands (`tools/Coverage/Program.cs`), already failing on main (`docs/coverage/README.md`), and its removed-level bindings and the 302 count at `general-engine-element-map.md:17` stay stale until the engine programme's coverage reconciliation regenerates them (coordinator decision, 25 Nov 2026; `deferred-work.md`); `CAT-015-V` is referenced elsewhere only in prose (`general-engine-element-map.md:43`) and `docs/work-orders/P0-004/catalogue-owners.json`, which no code reads. After removal every part keeps a fixture row except CAT-015 (bumper, 1 → 0).

### Decisions

- **D1. Placement is the subtitle and the `LEVEL-` entry.** Nothing reads array order (`ReadIndex` enumerates; `PuzzleCatalogue.cs:15` sorts Playable by `presentation.order`), so entries are not moved; the subtitle is the content's statement of its campaign level (no consumer, `PuzzleDefinition.cs:107,113`).
- **D2. Picker order unchanged (DF-2).** Order-bound browser anchors in a file another lane owns.
- **D3. Delete, do not renumber, `FIX-` rows.** No rule needs contiguity (`CurrentOwnerMembership.cs:85–101`, `CurrentCatalogue.cs:101–124`); renumbering would churn surviving identities.
- **D4. Titles in sentence case** like every authored title. `pulley_depth` is renamed because it is Planned and never in the picker; `quarter_bend` is Playable, keeps its title and is pinned by `PipeBend90Tests.cs:190`. Coordinator content choice. Decision records keep their own casing (*One Thing Leads to Another* at `campaign-coverage.md:139` stays); lines this slice restates use the content's sentence case.
- **D5. Element documents: current-state lines only.** Rows pinned to `@a6c914e` stay as evidence; "authored position N" labels in Campaign lines stay defined by campaign-coverage section 5 (PR-23 pairs each with its plan level); counts are restated, and level lists keep those labels and drop removed levels. One exception to the pinned-row rule: `CAT-053:84`'s description cell carries `pulley_depth`'s title, which is restated; its evidence cell is not. Dated readiness records (`readiness/batch-a.md:53`, `:84`, `readiness/batch-j.md:34`, the audits) are left unchanged.
- **D6. Census and membership by test** (coordinator decision, 25 Nov 2026). One minimal public census entry point in `tools/Ownership` counts the seven invest documents' headings by the oracle's rule (`CurrentOwnerMembershipOracles.cs:60`) against one shared constant; the root oracle and a Coverage test both call it (`Run(root)` reads only markdown; the Release rebuild comes from `OwnershipOracles.CheckCurrentPolicy`). H6's facts execute the membership and the census.

### What this removes

The 29 removed levels' content and their 182 `FIX-` identities (git history is the archive); the duplicate title; `grand_contraption`'s claim on *The Grand Contraption*; the 62 "Currently authored at position" paragraphs and the "no `LEVEL-` entry yet" notes for the 21 placed lettered levels. Nothing is kept for old ids: no alias, placeholder or redirect.

## Tasks & Acceptance

**Execution:**

0. Before the first edit, on the baseline: check that owner decision 24o and the ledger rows OS-02, OS-03, OS-06 to OS-10 are on it; run the focused selection and `dotnet run --project tools/Ownership -- --oracles`; record results, the baseline full round (7332 on `2ce73eed`) and the census recount (545) in the run log. Re-read every cited line; record moves.
0b. Append to `run-logs/spec-tool-lettered-level-ids.md` the carried receipts of its Entry 10 as listed under Settled decisions, and restate `TODO.md:8`.
1. `content/puzzles.json` -- the 24o(1) filter deletes the 29 entries; then the 33 subtitle and 2 title edits with Edit after Anvil -- H1–H3.
2. `current-consumers.md` -- delete the 182 rows, restate the 14 lists -- H5.
3. Test literals, `CampaignContentTests.cs`, the H6 fact -- H1–H3, H5, H6.
4. `campaign.md` title, header, placement paragraphs and 21 entries; census literal -- H6.
5. Documents: `requirements.md:186`, `:5998` (24o(2)), `campaign-coverage.md`, `delivery-batches.md` §4, the element lines, `deferred-work.md`, OS-02, `TODO.md` -- AC7.

**Acceptance Criteria:**

- AC1: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the baseline round + 7 (5 in `CuriousContraptions.tests`, 2 in `Coverage.Tests`; coordinator decision, 25 Nov 2026): 7339 on the `b64e1453` baseline (7332 + 7; Coverage.Tests 130 + 2); on a merged candidate, the merged main's round + 7.
- AC2: Given the candidate, when `dotnet run --project tools/Ownership -- --oracles` runs, then it exits 0 with `CurrentOwners` `Positive` true and `NegativeCount` 15, as at task 0 (this run passes no root, so it does not execute the census); and the census recount prints 566, equal to the shared census constant that the census test executes (D6).
- AC3: Given the candidate diff, when its paths are listed, then each is in the Final diff scope; `git diff --quiet <baseline> -- tools/e2e CuriousContraptions.tests/DominoEffectTests.cs docs/coverage` exits 0; and `git grep -h -c 'domino_effect/fixture_3' -- docs/planning/invest/current-consumers.md` prints 1.
- AC4: Given `F='map(select(.id as $i | [<29 removed ids as JSON strings>] | index($i) | not)) | map(.subtitle |= sub("^[0-9]{2}[a-z]? / "; "")) | map(if .id == "pulley_depth" or .id == "grand_contraption" then del(.title) else . end)'`, when `diff <(git show <baseline>:content/puzzles.json | jq -S "$F") <(jq -S "$F" content/puzzles.json)` runs, then it prints nothing and exits 0 (every kept field, the subtitle topic and `presentation.order` included, unchanged).
- AC5: Given the candidate, when `rg -n -w '<29 removed ids joined by |>' docs/planning/invest/current-consumers.md` and `rg -n 'Currently authored at position|hold authored content \(no .LEVEL-. entry yet\)' docs/planning/invest/campaign.md` run, then each exits 1 with no output; and `rg -U --count-matches '<a id="level-[0-9]{3}[a-z]"></a>\n### LEVEL-[0-9]{3}[a-z]\n' docs/planning/invest/campaign.md` prints 21.
- AC6: Given the candidate, when `rg -n -w '<29 removed ids>' --glob '!**/readiness/**' docs/planning/elements` runs, then every hit is on a row pinned to `@a6c914e`, a "was removed" statement, or the marked checks of `EL-186:103` and `EL-209:87`; and the reviewer confirms each "N levels" or "N others" statement and each authored-position list naming 8, 12, 30–48 or 50–57 in the element documents is either restated or pinned evidence.
- AC7: Given the candidate, when `rg -n -i 'fifteen (composite|springboard|lanes)' docs/planning/requirements.md docs/springboard-elastic-contract.md` runs, then it exits 1 with no output; `rg -c 'surviving composite springboard instance' docs/planning/requirements.md` prints 2; and `rg -n -i 'fifteen (composite|springboard|lanes)' --glob '!**/readiness/**' docs/planning/elements` lists only rows pinned to `@a6c914e` (`EL-194:63`, `:64`).

## Implementation Notes

- Checkout `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-content-housekeeping`, branch `slice-content-housekeeping`, baseline `b64e145323f6e15249781ff56946553d04fdfd8b` (origin/main; contains 24o and OS-02, OS-03, OS-06 to OS-10). Policy: this checkout's `_bmad/custom/` at the baseline, no owned policy delta (run log, entry 1).
- Planning commit `b64e1453` passed SnapshotApproval with full local round 7332/0/0 and was pushed fast-forward from `2ce73eed`; it is the AC1 baseline measurement (7339 expected on the unmerged candidate since coordinator decision 1; 7338 before it). Only planning documents changed from `2ce73eed` to the baseline (`content/puzzles.json`, `tools/`, `CuriousContraptions.tests/`, `current-consumers.md`, `campaign.md` unchanged), so the Capability probe stands; anchors in `TODO.md`, `CAT-023`, `CAT-035`, `CAT-058`, `campaign-coverage.md`, `completion-ledger.md`, `owner-decisions-2026-11-22.md` and `delivery-batches.md` may have moved (run log, entry 1).
- Effective route Auto: `VerifyPolicy --bmad-check --route Auto` structural findings 0; engine tier, all six layers on pass 1. Ready-for-development gate passed 24 November 2026 (run log, entry 1).
- 24o(1) sequence: two `python3` runs (no-op, deletion) and one tracked write, its sha256 matched to a read-only `sed` prediction that Anvil validated first (full text: run log, entry 15).
- Implementation owner: recorded at launch (run log, entry 1).
- 24o(1) applied as specified: no-op `cmp` exit 0; predicted sha256 `1ee222e1…` matched after the one deletion run; histogram numstat `0	21068`, then `35	21103` after the 33 field Edits (run log, entry 2).
- Owned-diff self-check: every changed path is in the Final diff scope; no branch, helper or shared fixture added; no simplification found. Beyond the line list, `current-consumers.md`'s title (122 fixture scopes) and line 3 (34 authored levels) were restated, being made wrong by the deletion and read by no code; its bowling line also gains `burst_of_motion`'s inventory, which the content holds (run log, entry 2).
- Census rule and literal: the shared constant `CurrentOwnerMembership.DeclaredHeadingCount` (566, with its history comment) is at `tools/Ownership/CurrentOwnerMembership.cs:27` and the one census rule, `CountDeclaredHeadings(root)`, at `:31`, both reading the same named engine and consumers documents and scope-document list that `Read` uses; the root oracle calls both (`CurrentOwnerMembershipOracles.cs:57–58`). The Code Map's `:53–71` and D6's `:60` cite the baseline oracle (run log, entry 6).
- Review repairs applied: run log, entries 3, 6, 7, 9, 11 and 13 (pass 1 summary moved to entry 15).

## Spec Change Log

- 25 November 2026, review pass 1 (coordinator; outside the intent contract): owned scope gains `CAT-014-bowling.md:14,140`, the CAT-003-I and CAT-023-I level lines and ledger row OS-10 (findings EFE F2, F3, ECH6); AC2's literal citation moves from `:70` to `:71`. No behaviour, design answer or acceptance row changes; KEEP everything already implemented.
- 25 November 2026, coordinator decisions on intent conflicts I1–I3 (run log, entry 5): intent H6's error-handling cell amended as decision 1 allows (census executable); D6, the probe's census line, the consumer fact, the owned scope (`CurrentOwnerMembership.cs`, a second H6 fact), AC1 (+7, 7339), AC2's comparison and the acceptance-to-test table follow it. The Never clause is unchanged. Known-bad state avoided: a census literal no required run executes. KEEP every pass 1 repair and all earlier work.
- 25 November 2026, review pass 3 (coordinator; outside the intent contract): owned scope gains `EL-211-solar-panel.md:115` (findings B3-2, ECH3-3: a statement this slice's removal made false, under AC6). No behaviour, design answer or acceptance row changes; KEEP all earlier work.
- 25 November 2026, review pass 4 (coordinator; outside the intent contract): the pass-3 deferrals of ECH3-1 and B3-1 did not meet Re-review scope (both are in, or directly affected by, the pass-2 repair; EFE4 D1, D2), so they become repairs: owned scope gains `EL-203-reverse-transmission.md:105` and `ui/Workshop.cs:134`, `ui/WorkshopPuzzle.cs:50` (one production constant for the picker's built-in label). The label constant changes no displayed text. KEEP all earlier work.

## Review Triage Log

### 2026-11-25 — Review pass 1

- Engine tier, all six layers (fresh no-history `general-purpose` children); inputs, agent identities and raw reports at run log, entry 4. Intent Alignment is descriptive: each divergence it named is one row. Forward-Only reported no violation; its two classified borderlines are rows.
- verdicts: 32 findings — high 0, medium 9, low 14, false 9, maybe-false 0; duplicates keep their own rows and share their group's route
- intent conflicts (blocking; owner decision requested, run log entry 4): I1 census executable proof; I2 pinned `@a6c914e` rows with present-tense dispositions; I3 `docs/coverage/**` bindings and their count
- resolved by coordinator decisions, 25 Nov 2026 (run log, entry 5): I1 → patch, option (b), census executable (VG1, EFE F1, IA3); I2 → reject, option (a): commit-pinned historical evidence stays unchanged under AGENTS.md and AC6/AC7 accept these rows by name (ECH3, ECH4, B3, EFE F6); I3 → defer, option (a): owned `deferred-work.md` entry (engine programme's coverage reconciliation) and the consumer fact corrected (FO-B1, B5)
- findings:
  - `[medium]` `[intent_gap I1]` VG1: the 566 census literal (`CurrentOwnerMembershipOracles.cs:71`) runs only in the root oracle run, which no required run executes — real gap (pre-existing for 545); executing it needs a public census surface in `tools/Ownership`, which contradicts intent H6's error-handling cell; value reproduced as 566 by the implementer and two reviewers.
  - `[medium]` `[intent_gap I1]` EFE F1: same root cause as VG1 (probe should read *present, untested*); also corrects D6's premise: `CurrentOwnerMembershipOracles.Run(root)` reads only markdown, the Release rebuild comes from `OwnershipOracles.CheckCurrentPolicy`.
  - `[false]` `[reject]` IA1: kept fields, H2 topic, `quarter_bend` title and H4 order are asserted by no test — AC4 (`jq` field-equality over every kept entry, `presentation.order` included) is the specified check and passed (`ac4.log`); the overclaim was in the coordinator's review header, not the candidate; header corrected for the next staging.
  - `[false]` `[reject]` IA2: H5 error paths only on the passing path — the contract cites the existing synthetic refusal cases `CurrentCatalogueTests.cs:118–124`, which ran in the full round.
  - `[medium]` `[intent_gap I1]` IA3: census untested (duplicate of VG1).
  - `[false]` `[reject]` IA4: subtitles are not displayed and the picker keeps `presentation.order` — the intent defers picker order (DF-2) and D1 records that nothing consumes `subtitle`.
  - `[low]` `[patch]` IA5: two numbering schemes in current documents (duplicate of B1) — fixed with B1.
  - `[false]` `[reject]` IA6: springboard wording adds named items — the text is owner decision 24o(2) verbatim (`owner-decisions-2026-11-22.md:70`; contract Code Map).
  - `[medium]` `[patch]` IA7: OS-10 stale (duplicate of B2) — fixed with B2.
  - `[false]` `[reject]` IA8: `EL-186:100`/`EL-209:84` notes — the owned scope names these exact marked checks and AC6 accepts them.
  - `[false]` `[reject]` IA9: `current-consumers.md` corrections beyond the line list — they restate statements the deletion made false (header 302→122, 62→34 levels) and a missing inventory; recorded in Implementation Notes; no code reads them.
  - `[medium]` `[intent_gap I3]` FO-B1: `docs/coverage/**` manifests still bind removed fixtures and the README counts 302 — the manual `Coverage audit-*` commands read them (the Code Map's "no reader" fact holds only for tests, workflows and hooks); the intent's Never clause forbids editing `docs/coverage/**`.
  - `[false]` `[reject]` FO-B2: "until its conversion" wording leaves the post-OS-08 text open — owner-dictated 24o(2) text; OS-08 owns the later restatement.
  - `[low]` `[patch]` ECH1: authored-position numbers no longer match file positions — fixed with B1.
  - `[medium]` `[patch]` ECH2: OS-10 stale — fixed with B2.
  - `[low]` `[intent_gap I2]` ECH3: `CAT-066-wall.md:86` (`@a6c914e`-pinned row) says `wall_return` "is Planned and has no content yet" — the Never clause forbids editing pinned rows.
  - `[low]` `[intent_gap I2]` ECH4: `CAT-005-battery.md:167` (pinned row L4) marks removed levels' fixtures "carry forward" — same constraint.
  - `[false]` `[reject]` ECH5: `readiness/batch-a.md:53` "fifteen composite lanes" — D5 names it a dated readiness record left unchanged; closure is governed by `requirements.md:5998`, restated here.
  - `[low]` `[patch]` ECH6: CAT-023-I had no "Level(s) unlocked" line (pre-existing; balloon part refuted: `burst_of_motion` places it as locked `fixture_1`) — added from the content (run log, entry 3).
  - `[low]` `[patch]` ECH7: title uniqueness compared ordinally — now `OrdinalIgnoreCase`; `CampaignContentTests` 5/0/0.
  - `[low]` `[patch]` ECH8: AC2 cited `:70` for the literal now at `:71` — AC2 corrected (Spec Change Log).
  - `[low]` `[patch]` B1: authored-position numbers unresolvable now that the file has 34 entries; EL-194 used them unmarked — every restated line now marks "authored positions before decision 24d" linking campaign-coverage section 5; EL-194 `:19`, `:20`, `:112`, `:126` marked (run log, entry 3).
  - `[medium]` `[patch]` B2: OS-10 read "wording pending OS-02" — restated as done by OS-02 with the three reworded lines.
  - `[low]` `[intent_gap I2]` B3: `EL-194-springboard.md:63–64` (pinned facts 6, 7) carry forward the fifteen lanes — Never clause; AC7 lists these rows as accepted pinned hits.
  - `[low]` `[patch]` B4: `CAT-063-switch.md:214` cited `:65` for a fact at `:69` — corrected, with EFE F4.
  - `[medium]` `[intent_gap I3]` B5: `general-engine-element-map.md:17` "302 Fixture source IDs" and `docs/coverage/catalogue-elements.json` bindings to removed levels — the 302 describes the `docs/coverage` shards it links, which the Never clause freezes; same decision as FO-B1.
  - `[false]` `[reject]` B6: subtitle topics ("Final workshop" on 20) out of step — intent H2 requires the topic to stay the baseline's, and AC4 enforces it.
  - `[medium]` `[patch]` EFE F2: OS-10 stale — fixed with B2.
  - `[low]` `[patch]` EFE F3: `CAT-014-bowling.md:14,140` listed two of five placing levels and called `burst_of_motion` a placement (pre-existing) — owned scope amended, both lines recounted from the content.
  - `[low]` `[patch]` EFE F4: `CAT-063:214` put `spring_signal` in Early level proof batch 1, contradicting `spec-early-level-proof-batch-1.md:24` and EL-194:112 — corrected, and the same claim at `CAT-062:180` (owned line).
  - `[low]` `[patch]` EFE F5: EL-194 authored positions unmarked — fixed with B1.
  - `[low]` `[intent_gap I2]` EFE F6: `CAT-066:86` pinned disposition false after removal — same as ECH3.

### 2026-11-25 — Review pass 2

- Re-review of the pass-1 repairs and the coordinator-decision repair (tree `903d0a36` → `0b79fc88`); engine tier: Verification Gap, Forward-Only, Element-Free Engine, Blind Hunter and Edge Case Hunter (the repair touches their subject); no Intent Alignment. Fresh no-history children; identities and inputs at run log, entry 8. Tier re-confirmed engine; Forward-Only: no violation.
- verdicts: 18 findings — high 0, medium 6, low 8, false 4, maybe-false 0
- findings:
  - `[false]` `[reject]` FO borderline 1: the census pattern beside `Parse`'s prefix pattern — a cross-check that only counts and throws; it admits no owner; moved unchanged.
  - `[low]` `[patch]` FO borderline 2: the seven invest documents listed twice in `CurrentOwnerMembership.cs` — one private `InvestDocuments` list now feeds `Read` and `CountDeclaredHeadings` (run log, entry 7).
  - `[low]` `[patch]` FO borderline 3: bare authored-position numbers (`EL-199:126`, `CAT-005:238`) read as plan slots 49, 58 — level ids added beside each bare number in the restated lines.
  - `[false]` `[reject]` FO borderline 4: stale `docs/coverage/**` — coordinator decision 3, owned deferred-work entry.
  - `[medium]` `[patch]` VG2-1: nothing reaches `CountDeclaredHeadings`' refusal — the census fact now inserts `### GAP-18` in a copy and asserts the named `InvalidOperationException`.
  - `[medium]` `[patch]` VG2-2: title uniqueness ignored the picker's `presentation.title` — `AuthoredTitlesAreUnique` now also checks the Playable picker titles through `PuzzleCatalogue.ReadDefinitions`.
  - `[medium]` `[patch]` EFE2 F1: census refusal untested — same fix as VG2-1.
  - `[low]` `[patch]` EFE2 F2: Auto Run Result stale (blocked, 41 paths) — restated by the coordinator at finalize.
  - `[medium]` `[patch]` ECH2-1: picker titles — same fix as VG2-2.
  - `[low]` `[patch]` ECH2-2: whitespace-only title variants counted distinct — grouping key now normalises whitespace.
  - `[low]` `[patch]` ECH2-3: `CAT-005:238` counted `battery_motor`'s inventory as a placement — now 8 placed, 1 inventory; `:240`'s S8 statement checked, unchanged.
  - `[low]` `[patch]` B2-1: `CAT-005:238` — same as ECH2-3.
  - `[medium]` `[patch]` B2-2: `EL-199:126`, `EL-202:109`, `EL-209:120` said "provable now", contradicting owner decision 3a and AGENTS.md — now "after the engine programme", as CAT-019/CAT-042.
  - `[low]` `[patch]` B2-3: `EL-199:126` omitted the Motor's Playable levels 59 and 60 — added.
  - `[false]` `[reject]` B2-4: deferring the coverage manifests on pass 2 breaks the re-review rule — it is the owner's coordinator decision 3 (25 Nov 2026), not a triage deferral.
  - `[medium]` `[patch]` B2-5: census refusal untested — same fix as VG2-1.
  - `[low]` `[patch]` B2-6: the census handoff named only `DeclaredHeadingCount` — `deferred-work.md` and `campaign-coverage.md:38`, `:564` now also name the lettered-id set in `TheCampaignDocumentDeclaresExactlyThePlacedLetteredLevels`.
  - `[false]` `[reject]` B2-7: `EL-191:64` names `wall_return` as in the Early level proof list — the row is pinned `@a6c914e` (coordinator decision 2).

### 2026-11-25 — Review pass 3

- Re-review of the pass-2 repairs (tree `0b79fc88` → `4520d5ba`); Verification Gap, Forward-Only, Element-Free Engine, Blind Hunter, Edge Case Hunter; fresh no-history children (run log, entry 10). Re-review scope applies. Tier re-confirmed engine; Forward-Only: no violation; Verification Gap: no gap.
- verdicts: 15 findings — high 0, medium 2, low 10, false 3, maybe-false 0
- findings:
  - `[false]` `[reject]` FO3 borderline 1: census refusal beside `Parse` — a visible refusal; admits nothing.
  - `[false]` `[reject]` FO3 borderline 2: one document list inside `CurrentOwnerMembership` — one path.
  - `[low]` `[defer]` FO3 borderline 3: `CurrentOwnerRelations.cs:12–13` lists the same seven documents — outside the repair diff, no rule broken; `deferred-work.md` (pass 3 entry).
  - `[false]` `[reject]` FO3 borderline 4: authored-position numbers kept in prose — D5 specifies them; each now carries its level id.
  - `[low]` `[defer]` B3-1: `EL-203:105` "provable now" — outside the owned scope and the repair diff; no hard rule, required-now criterion or gameplay behaviour; `deferred-work.md` with owner.
  - `[low]` `[patch]` B3-2: `EL-211:115` "positions 21, 22 and 24 … in the 63-level content file", false since the removal (AC6) — owned scope amended; restated with ids and the section 5 link (run log, entry 9).
  - `[medium]` `[patch]` B3-3: title normalisation and picker check never executed against a duplicate — synthetic near-duplicate and distinct pairs added inside `AuthoredTitlesAreUnique`.
  - `[low]` `[patch]` B3-4: the handoff omitted the pinned 122 `FIX-` count — `deferred-work.md` census entry and `campaign-coverage.md:38`, `:564` now name the 122 and 1293 pins.
  - `[low]` `[defer]` ECH3-1: the picker's built-in "Free workshop" label is not in the picker-title check — no production symbol exists (`ui/Workshop.cs:134`, `ui/WorkshopPuzzle.cs:50`) and copying the literal would pin UI text in a content test; the fix needs a UI change outside this content slice; H3's authored-title uniqueness is fully tested; `deferred-work.md` (owner OS-07/DF-2).
  - `[low]` `[patch]` ECH3-2: handoff fixture pin — same fix as B3-4.
  - `[low]` `[patch]` ECH3-3: `EL-211:115` — same fix as B3-2.
  - `[low]` `[patch]` EFE3 F1: `Read` selected document roles by string comparison — two named constants and one scope list passed explicitly; no string selects a role; gate PASS (`maintainability-4.log`).
  - `[medium]` `[patch]` EFE3 F2: H3 control claims not executed — same fix as B3-3.
  - `[low]` `[defer]` EFE3 F3: `EL-203:105` — same as B3-1.
  - `[low]` `[patch]` EFE3 F4: Auto Run Result stale — carried from EFE2 F2; restated at finalize.

### 2026-11-25 — Review pass 4

- Re-review of the pass-3 repairs and the coordinator's pass-3 records (tree `4520d5ba` → `30d4b180`); Verification Gap, Forward-Only, Element-Free Engine, Blind Hunter, Edge Case Hunter; fresh no-history children (run log, entry 12). Tier engine; Forward-Only: no violation; Verification Gap and Edge Case Hunter: no gap.
- verdicts: 12 findings — high 0, medium 1, low 9, false 2, maybe-false 0
- findings:
  - `[low]` `[patch]` VG4 other: missing space after the comma at `CurrentOwnerMembership.cs:37` — restored; gate rerun (run log, entry 11).
  - `[false]` `[reject]` FO4 borderline 1: census pattern beside `Parse` — carried from FO3 borderline 1.
  - `[low]` `[defer]` FO4 borderline 2: `CurrentOwnerRelations.cs:12–13` list — carried from FO3 borderline 3; deferral confirmed by EFE4 D3.
  - `[false]` `[reject]` FO4 borderline 3: authored positions 21, 22, 24 in `EL-211:115` — labelled, paired with plan levels 51a–51c and ids; not read as identifiers.
  - `[low]` `[patch]` FO4 note: formatting at `:37` — same fix as VG4 other.
  - `[low]` `[patch]` B4-1: the fixture-pin duty said only "adds locked fixtures" — restated in `deferred-work.md` and `campaign-coverage.md:38`, `:564`: any added, removed or changed placement or planned part, locked or movable, updates its `FIX-` row, the 122/1293 pins and the `CurrentCatalogueTests.cs:61` comment.
  - `[medium]` `[patch]` EFE4 D1: the pass-3 deferral of ECH3-1 did not meet Re-review scope (the picker-title check was in the pass-2 repair) — withdrawn; `Workshop.FreeWorkshopTitle` names the label once (`ui/Workshop.cs:18`, used at `:136` and `ui/WorkshopPuzzle.cs:50`, displayed text unchanged); the H3 fact includes it and a synthetic collision case.
  - `[low]` `[patch]` EFE4 D2: the pass-3 deferral of B3-1 did not meet Re-review scope (EL-202:109's repair made EL-203:105 contradict it) — withdrawn; `EL-203:105` restated; `rg 'provable now'` over current element documents exits 1.
  - `[low]` `[defer]` EFE4 D3: the `CurrentOwnerRelations.cs` deferral meets the test — kept in `deferred-work.md` and `deferred:`.
  - `[low]` `[patch]` EFE4 F1: formatting at `:37` — same fix as VG4 other.
  - `[low]` `[patch]` EFE4 F2: Implementation Notes said 7338 — now 7339 since coordinator decision 1.
  - `[low]` `[patch]` EFE4 F3: H6 rows cited runs before the pass-3 `Read` change — now cite the pass-4 repair run.

### 2026-11-25 — Review pass 5

- Re-review of the pass-4 repairs (tree `30d4b180` → `1f605539`); Verification Gap, Forward-Only, Element-Free Engine, Blind Hunter, Edge Case Hunter; fresh no-history children (run log, entry 14). Tier engine; Forward-Only and Verification Gap: no finding; EFE4 D1–D3, F1–F3 and B4-1 confirmed closed.
- verdicts: 7 findings — high 0, medium 0, low 5, false 2, maybe-false 0
- findings:
  - `[false]` `[reject]` FO5 borderline 1: `presentation?.Title ?? FreeWorkshopTitle` (`WorkshopPuzzle.cs:50`) — `presentation` is null only for the free workshop (`:46–47`); an authored id without a definition is a typed refusal; two behaviours, not a fallback.
  - `[false]` `[reject]` FO5 borderline 2: `EL-203:105` keeps the labelled authored position 18 — paired with plan level 14c and the id; not read as an identifier.
  - `[low]` `[patch]` ECH5-1: `deferred-work.md` lost its final newline in the pass-4 repair — restored, last byte `0a` (run log, entry 13).
  - `[low]` `[patch]` ECH5-2: the pin guidance omitted the comment at `CurrentOwnerMembershipTests.cs:66` — named in `deferred-work.md` and `campaign-coverage.md:38`, `:564`.
  - `[low]` `[patch]` B5-1: final newline — same fix as ECH5-1.
  - `[low]` `[patch]` EFE5 F5-1: final newline — same fix as ECH5-1.
  - `[low]` `[patch]` EFE5 note: the owned-scope row cited `ui/Workshop.cs:134` — now `:18`, `:136`.

### 2026-11-25 — Review pass 6

- Re-review of the pass-5 repair (tree `1f605539` → `5bee499f`, documents only); Verification Gap, Forward-Only, Element-Free Engine (run log, entry 14). Tier engine confirmed; capability probe re-checked.
- verdicts: 0 findings. Forward-Only noted the "(was `:134`)" history in the owned-scope row as not a violation; no action. Budget moves: run log, entries 15 and 18.

### 2026-11-25 — Final diff scope

- Candidate tree `50888c9c` on baseline `b64e1453`, 46 owned paths, checked by the informed pass-6 Element-Free auditor `ae763a4c783757af2` (re-engaged, read-only): every path is in the Final diff scope sentence; AC3–AC7 re-run read-only as expected; no incidental refactor, churn, speculative abstraction or duplicated ledger. Simplification: none justified. One records-only fix applied (a stale lead-in clause in Verification). Verdict: **Pass** (run log, entry 16).

## Design Notes

1. **Behaviour and scope.** The authored campaign holds the 34 kept levels, each labelled with its plan level, and the planning ledgers match it. Source: owner decisions 24d–24f; campaign-coverage section 5; completion-ledger OS-02; `deferred-work.md:357–363`.
2. **Reusable primitive.** None new; the production content reader and Coverage/Ownership readers.
3. **Generality.** No engine, package, wire or presentation code changes; element-free rules D1–D7 untouched; engineering rules E2, E7, E8, E9 apply.
4. **Forward-only.** Deleted content and identities leave no alias, placeholder or fallback.
5. **Prerequisites.** Lettered level ids published (`2ce73eed`); owner decision 24o and the ledger rows it names published on the baseline.

## Verification

Executor for every command: the implementation owner. Raw output under `.tools/content-housekeeping-evidence/`; one line per result here, citing its run-log entry. Environment, checkout discipline and log locations as in the [lettered level ids contract](spec-tool-lettered-level-ids.md) Verification, with checkout `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-content-housekeeping` and logs under `~/.local/share/tim-toolchain/logs/slice-content-housekeeping/`; each full-round log has `git rev-parse HEAD` as its first line and `HEAD-AFTER <git rev-parse HEAD>` as its last. The implementation owner does not commit, merge or push.

### Acceptance-to-test table

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| H1 | `CampaignContentTests.TheAuthoredCampaignHoldsExactlyTheKeptLevels` (fact): the set of (id, availability) from `ReadIndex` on the real file equals the 34 pairs transcribed from campaign-coverage section 5 (`:159–192`) | Set equality refuses a removed level left in (baseline: 63) and a kept level dropped or re-admitted as Playable | `L/full-round-final.log`, passed (run log, entry 17; earlier runs: entry 18) |
| H2 | `CampaignContentTests.EachKeptLevelIsSubtitledWithItsPlanLevel` (fact): the map id → subtitle prefix equals the transcribed map; AC4 checks the topic | Numbered (`01`, `11`, `20`) and lettered (`03a`, `09a`, `51c`) boundaries; baseline fails (authored positions) | `L/full-round-final.log`, passed (run log, entry 17; earlier runs: entry 18) |
| H3 | `CampaignContentTests.AuthoredTitlesAreUnique` (fact); `CampaignContentTests.ARenamedLevelCarriesItsNewTitle` (theory, 2 cases) | Baseline fails the first ("Around the corner" twice); case and repeated, leading or trailing whitespace are ignored; the same fact also checks the Playable levels' picker titles (`presentation.title`, through `PuzzleCatalogue.ReadDefinitions`) and executes the grouping on synthetic pairs: `" Around  The Corner"` with `"Around the corner"` forms one group, a distinct pair (control) none; the picker-title set includes the free workshop's label (`Workshop.FreeWorkshopTitle`), and a title differing from it only by case and spacing collides with it; control: `PipeBend90Tests.cs:190` keeps `quarter_bend`'s title | `L/full-round-final.log`, passed (run log, entry 17; earlier runs: entry 18) |
| H4 | AC4 (`presentation.order` field-equal); existing `NativeSceneFixture` first-item check (`:169–173`) | Existing `LaunchLessonSolveTests` and `WorkshopSaveTests` over Playable lessons | `L/full-round-final.log`, passed (run log, entry 17; earlier runs: entry 18) |
| H5 | `CurrentCatalogueTests.ActualCurrentContractRetainsAllIdentitiesModesAndFixtures` (122); `CurrentCapabilityInputsTests.CurrentPreparationPreservesObligationsAndReportsAllUnresolvedRoles` (1293, 122) and `PlannedAbsentResourcesDoNotBlockCurrentInputsButAdmittedRemovalDoes` (1293) | Refusal paths covered by existing synthetic `CurrentCatalogueTests` cases (`:118–124`) | `L/full-round-final.log`, passed (run log, entry 17; earlier runs: entry 18) |
| H6 | `CurrentOwnerMembershipTests.TheCampaignDocumentDeclaresExactlyThePlacedLetteredLevels` (fact): on the real tree, the lettered `LEVEL-` members of `CurrentOwnerMembership.Read` equal the 21 expected ids, its `FIX-` members number 122, and neither `FIX-015-001` nor `CAT-015-V` is a member | Baseline fails (no lettered member, 304 `FIX-`); a 22nd or missing heading, or a left-over removed row, fails; AC2 recount | `L/full-round-final.log`, passed (run log, entry 17; earlier runs: entry 18) |
| H6 (census) | `CurrentOwnerMembershipTests.TheInvestHeadingCensusEqualsTheDeclaredCount` (fact): on the real tree, `CurrentOwnerMembership.CountDeclaredHeadings` returns the shared `DeclaredHeadingCount` (566) | Controls in the same fact: a copy of the seven invest documents with one added declared heading (`LEVEL-069z`) counts exactly one more; a fresh copy with an undeclared heading the pattern counts (`### GAP-18`) is refused with `InvalidOperationException` naming it; AC2 recount | `L/full-round-final.log`, passed (run log, entry 17; earlier runs: entry 18) |

No float is compared; every assertion is a set, a map, a string or a count.

Handoff results on the pre-repair candidate (7338/0/0, AC2–AC7): run log, entries 2 and 18. `E` = `.tools/content-housekeeping-evidence/`, `L` = `~/.local/share/tim-toolchain/logs/slice-content-housekeeping/`.

Final results on the final candidate: run log, entry 17, summarised under Auto Run Result, Verification (`L/full-round-final.log`, 7339 passed, 0 failed, `Skipped: 0`).

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, with `CSHARP_STATIC_GATE_BINLOGS` exported -- expected: five logs, zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS, zero violations.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` (a test file is added) -- expected: its baseline count, 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~CampaignContentTests|FullyQualifiedName~CurrentCatalogueTests|FullyQualifiedName~CurrentCapabilityInputsTests|FullyQualifiedName~CurrentOwnerMembershipTests|FullyQualifiedName~TimingDeclarationTests|FullyQualifiedName~LaunchLessonSolveTests|FullyQualifiedName~WorkshopSaveTests|FullyQualifiedName~PipeBend90Tests"` -- expected: 0 failed, 0 skipped.
- `dotnet run --project tools/Ownership -- --oracles` and the census recount (read-only: the oracle's pattern and digit-or-hyphen filter over the seven invest documents) -- AC2.
- The 24o(1) no-op `cmp`; `git diff --numstat --diff-algorithm=histogram -- content/puzzles.json` after the filter (expected `0	21068`) and after the field edits (expected `35	21103`); the AC3–AC7 commands; `git add -N` the new files, then `git diff --check <baseline>` -- expected: no output.
- One full local round under `caffeinate -i` -- AC1. At the candidate commit, the unmodified `.githooks/pre-commit`.

### Running beside other work

Final diff scope: `content/puzzles.json`, `docs/planning/invest/current-consumers.md`, `docs/planning/invest/campaign.md`, `docs/planning/requirements.md` (`:186`, `:5998`), `docs/springboard-elastic-contract.md` (`:29`), `tools/Ownership/CurrentOwnerMembershipOracles.cs`, `tools/Ownership/CurrentOwnerMembership.cs` (coordinator decision 1), `tools/Coverage.Tests/CurrentCatalogueTests.cs`, `tools/Coverage.Tests/CurrentCapabilityInputsTests.cs`, `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs`, `CuriousContraptions.tests/CampaignContentTests.cs` (new), `docs/planning/elements/readiness/campaign-coverage.md`, `docs/planning/invest/delivery-batches.md`, the 22 element documents of the owned-scope table, `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/planning/elements/readiness/completion-ledger.md` (rows OS-02 and OS-10), `docs/planning/elements/CAT-014-bowling.md` (review pass 1 repair), `docs/planning/elements/EL-211-solar-panel.md` (review pass 3 repair), `docs/planning/elements/EL-203-reverse-transmission.md`, `ui/Workshop.cs` and `ui/WorkshopPuzzle.cs` (review pass 4 repairs), `_bmad-output/implementation-artifacts/run-logs/spec-tool-lettered-level-ids.md`, `TODO.md`, this contract and its run log. Nothing else.

Shared under 24m: `TODO.md`, `deferred-work.md`, `delivery-batches.md` (§4 and the Counts line `:352` here), `completion-ledger.md` (by row); `CAT-019-conveyor.md` and `CAT-064-tennis.md` under the 24o coordinator ruling (keep both sides at the later merge). No running lane edits `requirements.md` (the Baseball repair lists it as not edited; S5 names it only as published planning).

Disjointness from lanes 1, 2 and 4 and the blocked drafts, checked on `2ce73eed` at planning, with each lane's stale citations to re-read at the later publisher's merge: run log, entry 15.

No other running lane edits `content/puzzles.json`, `current-consumers.md`, `campaign.md`, `CurrentOwnerMembershipOracles.cs` or the three Coverage test files. One publisher at a time under the 24m merge rule: the later publisher merges `origin/main` once, keeps both sides' entries in shared documents, re-reads moved citations and repeats the full local round on the merged candidate.

## Auto Run Result

- **Summary.** The 29 cut levels and their 182 `FIX-` rows are removed (one recorded `python3` deletion, numstat `0 21068`, then 35 field Edits, `35 21103`); the 34 kept levels carry their plan levels in `subtitle`; `pulley_depth` and `grand_contraption` are retitled; `campaign.md` gains the 21 lettered `LEVEL-` entries; the 566 heading census is executable through `CurrentOwnerMembership.CountDeclaredHeadings` and `DeclaredHeadingCount` (coordinator decision 1); the springboard criterion follows 24o(2); the current planning documents are restated.
- **Files.** 46 owned paths, all in the Final diff scope (Verification; grouped list: run log, entry 18).
- **Review.** Engine tier, six passes: 32, 18, 15, 12, 7 and 0 findings. Every confirmed finding was patched by the retained owner and independently re-reviewed; intent conflicts I1–I3 were settled by the coordinator decisions of 25 Nov 2026; one deferral (the `CurrentOwnerRelations.cs` document list, `deferred-work.md`). Final diff scope: Pass. Element-Free Engine: Scoped Pass, no D1–D7 violation. Forward-Only: no violation on any pass.
- **Follow-up review recommended:** false (pass 6 patched nothing).
- **Verification (final candidate, run log, entry 17).** Capture 5/5 logs, 0 warnings, 0 errors; maintainability gate PASS (metrics 0, coupling 0, both canaries PASS); registration 775/0/0; `git diff --check` clean; full local round 17 + 287 + 132 + 256 + 6647 = 7339 passed, 0 failed, `Skipped: 0` (AC1), HEAD and HEAD-AFTER `b64e1453`. The normal pre-commit hook runs at the candidate commit.
- **For the approver.** The 24m merge of `origin/main`, its full round and SnapshotApproval of the merged commit. Remote build, artifact and deployment unverified; browser qualification pending until the post-catalogue E2E phase; DF-1 to DF-5 stay with their owners.
