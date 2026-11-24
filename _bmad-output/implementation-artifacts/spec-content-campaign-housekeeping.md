---
title: 'Campaign content housekeeping: remove the 29 repeat levels, place the 34 kept levels (content slice, lane 3)'
type: 'refactor'
created: '2026-11-24'
status: 'draft'
baseline_revision: 'captured at dispatch (the first published main after 2ce73eed that contains owner decision 24o and ledger rows OS-02, OS-03, OS-06 to OS-10)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/planning/elements/readiness/campaign-coverage.md'
  - '{project-root}/_bmad/custom/bmad-loop-protocol.md'
warnings: []
deferred: ['DF-1 domino_effect re-authoring (ledger OS-06)', 'DF-2 Playable picker order (OS-07)', 'DF-3 conversions of double_cold_start and grand_contraption (OS-08)', 'DF-4 Rope anchor in pulley_depth, PR-24 (OS-09)', 'DF-5 Batch M levels 17, 25, 29, 49, 79, 89 (OS-03)']
---

<intent-contract>

## Intent

**Problem:** Owner decision 24d removes 29 Planned levels of `content/puzzles.json` as repeats of an already introduced mechanic, and 24e/24f place the 34 kept levels at plan levels, 21 of them lettered (`11a`, `3a`, `51c`, …) ([campaign coverage, section 5](../../docs/planning/elements/readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)). The content still holds all 63 levels at their authored positions, the fixture owner table in `current-consumers.md` still declares the 182 fixtures of the removed levels, two levels share the title "Around the corner", `grand_contraption` keeps the title reserved for level 150, and `campaign.md` has no `LEVEL-` entry for any lettered level that holds authored content.

**Approach:** Housekeeping only, nothing proven solvable: delete the 29 entries and their 182 `FIX-` rows together; give each kept level its plan level in its `subtitle`; retitle `grand_contraption` and rename `pulley_depth`; add the 21 lettered `LEVEL-` entries and raise the heading census to 566; restate the current documents and every test that counts or names levels. Re-authoring, conversions, picker order and new levels are deferred (DF-1 to DF-5, Code Map): each must be authored where it can be proven, needs a browser, or needs goal evaluation or undelivered parts, all after the engine programme (owner decision 22).

## Boundaries & Constraints

**Always:** Every kept entry keeps every field except the plan-level prefix of `subtitle` (33 change; `first_principles` is already `01`) and `title` (`pulley_depth`, `grand_contraption`). Content and the fixture table change together, so `CurrentCatalogue.ValidateFixtures` (`tools/Coverage/CurrentCatalogue.cs:187–200`) keeps exact set equality. Surviving `FIX-` ids keep their spelling (no renumbering). A lettered entry uses the canonical form `LEVEL-011a`, anchor `level-011a`, groups `MATRIX-011a-BALANCED/FORGIVING/PRECISE` (`campaign.md:9`). New tests read the real files through the production readers. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A change to any field of `domino_effect`, `double_cold_start`, `grand_contraption` or `pulley_depth` other than those named above; a change to any `presentation.order`, to `DominoEffectTests.cs`, to the `domino_effect/fixture_3` lamp row, or to anything under `tools/e2e/`; a new level, goal, placement or Playable promotion; a renumbered `FIX-` or `LEVEL-` id; an alias, redirect or "removed level" placeholder entry; an edit to `docs/coverage/**` or to evidence rows pinned to a revision (`@a6c914e`), except the description cell of `CAT-053-pulley.md:84` (D5); any shell-command write to a tracked repository file other than the one 24o(1) filter; a local Release build; output hashes, mutants, browser runs or skipped tests.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| H1 | `PuzzleContentReader.ReadIndex` over the candidate file | Exactly the 34 kept ids, each with its baseline availability (15 Playable, 19 Planned); none of the 29 removed ids | No error expected |
| H2 | Each kept entry's `subtitle` | Its prefix before ` / ` is its plan level from campaign-coverage section 5 (`:159–192`), two digits plus an optional letter (`03a`, `11`, `51c`); the topic after ` / ` is the baseline's | No error expected |
| H3 | All titles of the index | Pairwise distinct; `grand_contraption` is "One thing leads to another", `pulley_depth` is "The long way round", `quarter_bend` keeps "Around the corner" | No error expected |
| H4 | `PuzzleCatalogue.Playable` | The same 15 ids in the same order as the baseline (no `presentation.order` changes) | No error expected |
| H5 | `CurrentCatalogue.Read` and `RequirementDiscovery.Discover` on the real tree | Read succeeds with 122 fixtures (78 Planned parts + 44 Playable placements); discovery reports 1293 sources, 122 of origin Fixture | A `FIX-` row left for a removed level throws `InvalidDataException` "Authored fixture membership differs." (`:198`); a removed level left in content with its row deleted throws "Fixture content differs from current declared identity, kind or locked state." (`:194–195`) |
| H6 | `CurrentOwnerMembership.Read` on the real tree | Its `LEVEL-` members with a letter are exactly the 21 placed lettered levels; members lose the 182 removed `FIX-` ids and `CAT-015-V` (its only fixture row is `wall_return/bumper`) | No error expected. The census literal 566 runs only in the root oracle run, which no required run executes; it is verified by recount (AC2) |

</intent-contract>

## Code Map

Delivery shape: single content slice (no element, family or closure). Review tier: **engine**: the owned diff edits a tool source literal (`tools/Ownership/CurrentOwnerMembershipOracles.cs`), a tool test project (`tools/Coverage.Tests`), `TODO.md` and planning documents beyond the slice record, none on the data-only allow-list (`_bmad/custom/bmad-loop-protocol.md`, Review tiers); probe below. Baseline: captured at dispatch (coordinator decision 38) from the first published main after `2ce73eed` that contains owner decision 24o and ledger rows OS-02, OS-03 and OS-06 to OS-10 (if task 0 does not find them, stop); every scope guard and `git diff` compares against it. Drafted and line-checked on `2ce73eed`, the drafting revision only. Lane 3 of owner decision 24m, after lettered level ids.

**Planning verdict: Scoped Pass with non-blocking findings** (round 5, `docs/planning/elements/readiness/audits/audit-content-housekeeping-5.md`, 24 November 2026). Rounds 1 (B1–B3, N1–N10), 2 (B1, N1–N7), 3 (B1–B3, N1–N5) and 4 (B1, N1–N6) were Incomplete; every finding of rounds 1–5 is applied, round 5's N1–N3 after its report (labels, ledger cells, two domino owner pointers; not re-audited). Owner decision 24o settles the open decisions below.

**Settled decisions (owner decision 24o, `owner-decisions-2026-11-22.md:70`, 24 November 2026):**

- **24o(1), the content removal method.** The deletion is four contiguous line ranges of 21,068 lines (about 554 KB), which Write and Edit cannot carry, so the slice removes the 29 levels with one recorded `python3` filter over `content/puzzles.json`. The filter deletes the 29 ids and rewrites 35 string fields (33 subtitles, 2 titles), serialising with `json.dumps(indent=2, ensure_ascii=False) + "\n"`. Before applying it, a no-op run (same filter, no changes) must reproduce the file byte for byte (`cmp`, as checked on `2ce73eed`); after it, `git diff --numstat --diff-algorithm=histogram -- content/puzzles.json` reports 35 added and 21,103 deleted lines (the four ranges plus 35 changed lines; git's default Myers algorithm reports a larger pairing and is not used). Anvil validates the write with `preview` plus `contentSha256`; the exact command goes in the run log. The exception covers that one step only: every other write uses Write or Edit after Anvil. (`jq` is not used: it rewrites 158 unchanged number literals.)
- **24o(2), the fifteen springboard instances.** `requirements.md:186` ("all fifteen composite springboard instances") and `:5998` (EL-194, "all fifteen composite lane instances") are reworded by this slice to the surviving instance and the campaign levels that teach the springboard (exact text in the owned scope); completion-ledger OS-10 records that decision 24d closed the other 14. `EL-194-springboard.md:19`, `:54`, `:113` and `CAT-062-spring.md:166`, which quote or restate the criterion, follow the new wording.
- **24o(3), the deferred parts.** DF-1, DF-3 and DF-4 wait for the engine programme as Early level proof slices (owner decision 22); DF-2 waits for the post-catalogue E2E phase; DF-5 waits for its goal kinds and parts.
- **Coordinator ruling under 24o.** Element documents edited by two lanes are shared documents under 24m, resolved by keeping both sides' edits at the later publisher's merge: here `CAT-019-conveyor.md` `:67`, `:147`, `:160`, `:208` (lane 1 part B) and `CAT-064-tennis.md:94` (lane 1 B2).
- **Ledger rows.** OS-02 (this slice), OS-03 (re-owned), OS-06 to OS-09 (DF-1 to DF-4), OS-10 (the springboard closure), PR-08, PR-24 and OS-01 were written by the coordinator at planning (`completion-ledger.md:365`, `:381`, `:392–401` in the planning worktree); task 0 checks that they are on the baseline.
- **Carried receipts for task 0b** (coordinator, 24 November 2026): merged commit `2ce73eedcc151dc1b513013c74284ac798e14e00` (merge of `3a80c6a8` into `f60849bd`); full local round 7332 passed, 0 failed, `Skipped: 0` (Coverage.Tests 130); registration check 775/0/0; SnapshotApproval Pass by a fresh independent reviewer on `2ce73eed`, no findings; fast-forward push from `3a80c6a8`. Remote build, artifact and deployment unverified.

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
| `content/puzzles.json` | removed entries `2659–2941` (`wind_signal`), `4239–4517` (`wall_return`), `11647–24072` (`bounce_and_roll` … `relay_workshop`), `25093–33172` (`triple_chain` … `cold_bridges`); the 33 kept `subtitle` lines that change (`547`, `767`, … `36944`; line `6` is already `01`; run log lists all); `title` `7199` (`pulley_depth`) and `33176` (`grand_contraption`) | 24o(1) filter: delete 29 entries; subtitle prefix = plan level; two titles |
| `docs/planning/invest/current-consumers.md` | the 182 `FIX-` rows of removed levels in 23 runs (`718`, `732–754`, `757–767`, `785`, `796–822`, `826–831`, `839–841`, `843–844`, `851–861`, `864–875`, `877`, `884–899`, `902–905`, `907–908`, `914`, `918–936`, `948–950`, `952–953`, `963`, `968–985`, `992`, `995–1008`, `1010–1012`); "Level(s) unlocked" lines `10`, `41`, `52`, `135`, `146`, `184`, `269`, `338`, `403`, `521`, `599`, `610`, `621`, `643` | delete rows; drop removed ids from the lists (`146` becomes inventory only) |
| `docs/planning/invest/campaign.md` | title `:1` (the 150 slots and 21 lettered entries; 513 groups, 2,052 retained placement cells); header `:9` (34 authored, 29 removed, groups 450 + 3 × 21 = 513); the 62 "Placement under owner decisions 24d to 24f" paragraphs (`18`, `31`, … `811`) and 62 "Currently authored at position" paragraphs (`20`, `33`, … `813`); 21 new entries | each slot entry gets one present-tense placement paragraph (authored level at this slot, or none; a removed level named once with its section 5 link; links to its lettered entries); the "Currently authored" paragraph is deleted; entries for 3a, 9a, 11a–11d, 12a–12c, 14a–14c, 19a, 19b, 21a, 22a, 23a, 41a, 51a–51c inserted after their slot's entry in letter order, each shaped like `LEVEL-011` (`:143–154`): anchor, heading, decision boundary, authored level, criteria and profile-group line, chapter reservation link |
| `tools/Ownership/CurrentOwnerMembershipOracles.cs` | `:67–70` | literal 545 → 566; comment names this slice's 21 lettered headings |
| `tools/Coverage.Tests/CurrentCatalogueTests.cs` | `:61–62` | 304 → 122; comment "78 Planned parts plus 44 Playable placements" |
| `tools/Coverage.Tests/CurrentCapabilityInputsTests.cs` | `:34`, `:36`, `:69` | 1475 → 1293; 304 → 122; 1475 → 1293 |
| `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs` | one new fact | H6 |
| `CuriousContraptions.tests/CampaignContentTests.cs` | new | H1–H3 (four tests, 5 cases) |
| `docs/planning/elements/readiness/campaign-coverage.md` | `:33`, `:38` (census 566 after this slice), `:40`, `:149`, `:151`, `:155` (placement is the subtitle and the `LEVEL-` entry; picker order is DF-2), `:164`, `:178–179`, `:288`, `:303–304`, `:559`, `:564` | removals, renames and 21 headings done; DF-1 to DF-5 named with owner and gate; "Not done" restated |
| `docs/planning/invest/delivery-batches.md` (shared, §4 and the Counts line `:352`) | `:207`, `:209–213`, `:225–234`, `:352` | 34 levels (15 Playable, 19 Planned) and the counts over them; level numbers become plan levels |
| `docs/planning/requirements.md` | `:186`, `:5998` | 24o(2): "all fifteen composite springboard instances" (`:186`) and "all fifteen composite lane instances" (`:5998`) each become "the springboard lessons `spring_forward` (plan level 2), `ready_to_rebound` (3a) and `spring_signal` (11c), and the surviving composite springboard instance (`double_cold_start` lane 2, plan level 19b, until its conversion, completion-ledger OS-08; decision 24d closed the other 14, OS-10)"; nothing else in either line changes |
| `docs/springboard-elastic-contract.md` | `:29` | "all fifteen composite springboard lane instances" becomes the same 24o(2) set as `requirements.md:5998`, which links this contract |
| Element documents (23), current-state lines only | `CAT-005-battery.md:238` (9 levels; drop 31, 37, 46, 53, 55); `CAT-015-bumper.md:14,166`; `CAT-019-conveyor.md:67` (inventory in 4), `:147`, `:160`, `:208` (49 and 58) (shared with part B, 24o ruling); `CAT-028-fan.md:109` (5 levels, 2 as a fixed cold fan); `CAT-035-lamp.md:14` (5 Planned levels) and `:117` (the other 5 levels: `power_trip`, `spring_signal`, `counterweight`, `pulley_depth`, `grand_contraption`; owner pointer to DF-1); `CAT-042-motor.md:81` (12 levels), `:225` (drop 31, 37, 46, 53, 55); `CAT-053-pulley.md:84` (description cell's `pulley_depth` title only; its evidence cell stays); `CAT-062-spring.md:14` (and 2 others), `:13`, `:66`, `:153`, `:174`, `:180` (one composite lane, `double_cold_start`, until DF-3; the other 14 removed under 24d, OS-10), `:166` (24o(2)); `CAT-063-switch.md:14,214` (placed in 8, inventory in 4); `CAT-064-tennis.md:94` (5 levels; shared with B2, 24o ruling); `CAT-066-wall.md:14,149`; `EL-186-temporary-bridge.md:103` and `EL-209-fan.md:87` (marked a check at `a6c914e`, level since removed); `EL-189-tennis-ball.md:114` (5 levels); `EL-191-wall.md:101`; `EL-194-springboard.md:19` (24o(2) wording; its citation `requirements.md:5955` corrected to `:5998`), `:20`, `:22`, `:126` (the 24o(2) set: the springboard lessons and `double_cold_start` lane 2 until OS-08), `:54` (24o(2) wording), `:112` (the batch's levels by id: `air_mail`, `third_dimension`, `a_gentle_rebound`), `:113` (one instance, 49 `double_cold_start`, until DF-3; 24o(2)); `EL-195-pinball-bumper.md:103`; `EL-196-battery.md:99` (eight levels); `EL-199-electric-motor.md:126` (21, 22, 24, 49, 58); `EL-202-conveyor.md:109` (49 and 58); `EL-209-fan.md:120` (5 levels; `wind_signal` dropped from the provable list); owner pointer only, content change stays with DF-1/DF-3/DF-4: `CAT-023-domino.md:121`, `:141` (DF-1, OS-06), `EL-193-domino.md:116` (DF-1, OS-06, and level 58's conversion DF-3, OS-08; the removed levels now "were removed"), `CAT-058-rope_anchor.md:157` | values re-counted from the candidate content with `current-consumers.md`; lists keep their authored-position labels, marked once as "authored positions" in each restated line (D5), and drop removed levels |
| `_bmad-output/implementation-artifacts/deferred-work.md` (shared) | `:357–363` | the census entry records 21 headings added (566) and keeps the duty for the other 73 |
| `docs/planning/elements/readiness/completion-ledger.md` (shared, by row) | OS-02 | state set to done at close, citing this contract |
| `_bmad-output/implementation-artifacts/run-logs/spec-tool-lettered-level-ids.md` | appended entry | task 0b: the carried receipts |
| `TODO.md` (shared) | `:8` | lane 3 line: lettered level ids published as `2ce73eed`; this slice |
| this contract and `run-logs/spec-content-campaign-housekeeping.md` | new | slice record |

Read, not changed (cite in review): `engine/gpu/PuzzleContent.cs:26–52` (`ReadIndex`: array order unused, ids unique); `engine/gpu/PuzzleCatalogue.cs:10,15,161–177` (Planned indexed, never constructed; Playable order is `presentation.order`, unique); `engine/gpu/PuzzleDefinition.cs:107,113` (`subtitle` required, not consumed); `tools/E2eRunner/LessonFixtures.cs:20–52`; `tools/Coverage/RequirementDiscovery.cs:73–90`; `tools/Coverage/CurrentCatalogue.cs:101–124,187–200`; `tools/Ownership/CurrentOwnerMembership.cs:80–103` (no contiguity or count rule on `FIX-` rows beyond non-empty); `tools/Ownership/Program.cs:17` (plain `--oracles` passes no root); `tools/E2eRunner.Tests/TimingDeclarationTests.cs:124–131` (suites declare Playable ids only); `CuriousContraptions.tests/NativeSceneFixture.cs:169–173`; `CuriousContraptions.tests/PipeBend90Tests.cs:190` (`quarter_bend` title on the real file); `ui/Workshop.cs:262–268`.

### Capability probe

Measured on `2ce73eed`. No solver, ABI, wire or presentation capability is involved; every capability is present:

- Content index: `PuzzleContentReader.ReadIndex` (`PuzzleContent.cs:28`); exercised by `WorkshopSaveTests.ContentLoadRejectsEmptyDuplicateAndNoPlayableCatalogue`.
- Playable catalogue and its order: `PuzzleCatalogue.ReadDefinitions` (`PuzzleCatalogue.cs:161`); exercised by `LaunchLessonSolveTests.EveryPlayableLessonHasAnExplicitExecutedScenario`.
- Fixture binding: `CurrentCatalogue.ValidateFixtures`; exercised by `CurrentCatalogueTests.ActualCurrentContractRetainsAllIdentitiesModesAndFixtures`.
- Lettered owner headings: `CurrentOwnerMembership.Identity` (`:111`); exercised by `CurrentOwnerMembershipTests.ALetteredLevelHeadingInTheCampaignDocumentIsADeclaredOwner`.
- Heading census: `CurrentOwnerMembershipOracles.cs:53–71`, root-only; recount 545 on `2ce73eed` (engine 61, gpu-physics 29, refinements 197, decisions 8, scope-corrections 28, current-consumers 72, campaign 150).

Consumer facts: no code, test or tool names a removed or converted level id (`rg -w` over `*.cs`, `*.ts`); `docs/coverage/**` (which names them) has no reader in tests, `.github/workflows/pages.yml` or `.githooks/pre-commit`; `CAT-015-V` is referenced elsewhere only in prose (`general-engine-element-map.md:43`) and `docs/work-orders/P0-004/catalogue-owners.json`, which no code reads. After removal every part keeps a fixture row except CAT-015 (bumper, 1 → 0).

### Decisions

- **D1. Placement is the subtitle and the `LEVEL-` entry.** Nothing reads array order (`ReadIndex` enumerates; `PuzzleCatalogue.cs:15` sorts Playable by `presentation.order`), so entries are not moved; the subtitle is the content's statement of its campaign level (no consumer, `PuzzleDefinition.cs:107,113`).
- **D2. Picker order unchanged (DF-2).** Order-bound browser anchors in a file another lane owns.
- **D3. Delete, do not renumber, `FIX-` rows.** No rule needs contiguity (`CurrentOwnerMembership.cs:85–101`, `CurrentCatalogue.cs:101–124`); renumbering would churn surviving identities.
- **D4. Titles in sentence case** like every authored title. `pulley_depth` is renamed because it is Planned and never in the picker; `quarter_bend` is Playable, keeps its title and is pinned by `PipeBend90Tests.cs:190`. Coordinator content choice. Decision records keep their own casing (*One Thing Leads to Another* at `campaign-coverage.md:139` stays); lines this slice restates use the content's sentence case.
- **D5. Element documents: current-state lines only.** Rows pinned to `@a6c914e` stay as evidence; "authored position N" labels in Campaign lines stay defined by campaign-coverage section 5 (PR-23 pairs each with its plan level); counts are restated, and level lists keep those labels and drop removed levels. One exception to the pinned-row rule: `CAT-053:84`'s description cell carries `pulley_depth`'s title, which is restated; its evidence cell is not. Dated readiness records (`readiness/batch-a.md:53`, `:84`, `readiness/batch-j.md:34`, the audits) are left unchanged.
- **D6. Census by recount, membership by test.** The census runs only with a root, which needs Release builds this workflow does not run (`deferred-work.md:357–363`); the recount uses the oracle's pattern (`CurrentOwnerMembershipOracles.cs:60`), and H6's fact executes the membership.

### What this removes

The 29 removed levels' content and their 182 `FIX-` identities (git history is the archive); the duplicate title; `grand_contraption`'s claim on *The Grand Contraption*; the 62 "Currently authored at position" paragraphs and the "no `LEVEL-` entry yet" notes for the 21 placed lettered levels. Nothing is kept for old ids: no alias, placeholder or redirect.

## Tasks & Acceptance

**Execution:**

0. Before the first edit, on the baseline: check that owner decision 24o and the ledger rows OS-02, OS-03, OS-06 to OS-10 are on it; run the focused selection and `dotnet run --project tools/Ownership -- --oracles`; record results, the baseline full round (7332 on `2ce73eed`) and the census recount (545) in the run log. Re-read every cited line; record moves.
0b. Append to `run-logs/spec-tool-lettered-level-ids.md` the carried receipts of its Entry 10 as listed under Settled decisions, and restate `TODO.md:8`.
1. `content/puzzles.json` -- the 24o(1) filter -- H1–H3.
2. `current-consumers.md` -- delete the 182 rows, restate the 14 lists -- H5.
3. Test literals, `CampaignContentTests.cs`, the H6 fact -- H1–H3, H5, H6.
4. `campaign.md` title, header, placement paragraphs and 21 entries; census literal -- H6.
5. Documents: `requirements.md:186`, `:5998` (24o(2)), `campaign-coverage.md`, `delivery-batches.md` §4, the element lines, `deferred-work.md`, OS-02, `TODO.md` -- AC7.

**Acceptance Criteria:**

- AC1: Given the candidate, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the baseline round + 6 (5 in `CuriousContraptions.tests`, 1 in `Coverage.Tests`): 7338 on a `2ce73eed` baseline (7332 + 6; Coverage.Tests 130 + 1), re-measured at the baseline; on a merged candidate, the merged main's round + 6.
- AC2: Given the candidate, when `dotnet run --project tools/Ownership -- --oracles` runs, then it exits 0 with `CurrentOwners` `Positive` true and `NegativeCount` 15, as at task 0 (this run passes no root, so it does not execute the census); and the census recount prints 566, equal to the literal at `CurrentOwnerMembershipOracles.cs:70`.
- AC3: Given the candidate diff, when its paths are listed, then each is in the Final diff scope; `git diff --quiet <baseline> -- tools/e2e CuriousContraptions.tests/DominoEffectTests.cs docs/coverage` exits 0; and `git grep -h -c 'domino_effect/fixture_3' -- docs/planning/invest/current-consumers.md` prints 1.
- AC4: Given `F='map(select(.id as $i | [<29 removed ids as JSON strings>] | index($i) | not)) | map(.subtitle |= sub("^[0-9]{2}[a-z]? / "; "")) | map(if .id == "pulley_depth" or .id == "grand_contraption" then del(.title) else . end)'`, when `diff <(git show <baseline>:content/puzzles.json | jq -S "$F") <(jq -S "$F" content/puzzles.json)` runs, then it prints nothing and exits 0 (every kept field, the subtitle topic and `presentation.order` included, unchanged).
- AC5: Given the candidate, when `rg -n -w '<29 removed ids joined by |>' docs/planning/invest/current-consumers.md` and `rg -n 'Currently authored at position|hold authored content \(no .LEVEL-. entry yet\)' docs/planning/invest/campaign.md` run, then each exits 1 with no output; and `rg -U --count-matches '<a id="level-[0-9]{3}[a-z]"></a>\n### LEVEL-[0-9]{3}[a-z]\n' docs/planning/invest/campaign.md` prints 21.
- AC6: Given the candidate, when `rg -n -w '<29 removed ids>' --glob '!**/readiness/**' docs/planning/elements` runs, then every hit is on a row pinned to `@a6c914e`, a "was removed" statement, or the marked checks of `EL-186:103` and `EL-209:87`; and the reviewer confirms each "N levels" or "N others" statement and each authored-position list naming 8, 12, 30–48 or 50–57 in the element documents is either restated or pinned evidence.
- AC7: Given the candidate, when `rg -n -i 'fifteen (composite|springboard|lanes)' docs/planning/requirements.md docs/springboard-elastic-contract.md` runs, then it exits 1 with no output; `rg -c 'surviving composite springboard instance' docs/planning/requirements.md` prints 2; and `rg -n -i 'fifteen (composite|springboard|lanes)' --glob '!**/readiness/**' docs/planning/elements` lists only rows pinned to `@a6c914e` (`EL-194:63`, `:64`).

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

1. **Behaviour and scope.** The authored campaign holds the 34 kept levels, each labelled with its plan level, and the planning ledgers match it. Source: owner decisions 24d–24f; campaign-coverage section 5; completion-ledger OS-02; `deferred-work.md:357–363`.
2. **Reusable primitive.** None new; the production content reader and Coverage/Ownership readers.
3. **Generality.** No engine, package, wire or presentation code changes; element-free rules D1–D7 untouched; engineering rules E2, E7, E8, E9 apply.
4. **Forward-only.** Deleted content and identities leave no alias, placeholder or fallback.
5. **Prerequisites.** Lettered level ids published (`2ce73eed`); owner decision 24o and the ledger rows it names published on the baseline.

## Verification

Executor for every command: the implementation owner. Raw output under `.tools/content-housekeeping-evidence/`; one line per result here, citing its run-log entry. Environment, checkout discipline and log locations as in the [lettered level ids contract](spec-tool-lettered-level-ids.md) Verification, with checkout `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-content-housekeeping` and logs under `~/.local/share/tim-toolchain/logs/slice-content-housekeeping/`. The implementation owner does not commit, merge or push.

### Acceptance-to-test table

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| H1 | `CampaignContentTests.TheAuthoredCampaignHoldsExactlyTheKeptLevels` (fact): the set of (id, availability) from `ReadIndex` on the real file equals the 34 pairs transcribed from campaign-coverage section 5 (`:159–192`) | Set equality refuses a removed level left in (baseline: 63) and a kept level dropped or re-admitted as Playable | planned |
| H2 | `CampaignContentTests.EachKeptLevelIsSubtitledWithItsPlanLevel` (fact): the map id → subtitle prefix equals the transcribed map; AC4 checks the topic | Numbered (`01`, `11`, `20`) and lettered (`03a`, `09a`, `51c`) boundaries; baseline fails (authored positions) | planned |
| H3 | `CampaignContentTests.AuthoredTitlesAreUnique` (fact); `CampaignContentTests.ARenamedLevelCarriesItsNewTitle` (theory, 2 cases) | Baseline fails the first ("Around the corner" twice); control: `PipeBend90Tests.cs:190` keeps `quarter_bend`'s title | planned |
| H4 | AC4 (`presentation.order` field-equal); existing `NativeSceneFixture` first-item check (`:169–173`) | Existing `LaunchLessonSolveTests` and `WorkshopSaveTests` over Playable lessons | planned |
| H5 | `CurrentCatalogueTests.ActualCurrentContractRetainsAllIdentitiesModesAndFixtures` (122); `CurrentCapabilityInputsTests.CurrentPreparationPreservesObligationsAndReportsAllUnresolvedRoles` (1293, 122) and `PlannedAbsentResourcesDoNotBlockCurrentInputsButAdmittedRemovalDoes` (1293) | Refusal paths covered by existing synthetic `CurrentCatalogueTests` cases (`:118–124`) | planned |
| H6 | `CurrentOwnerMembershipTests.TheCampaignDocumentDeclaresExactlyThePlacedLetteredLevels` (fact): on the real tree, the lettered `LEVEL-` members of `CurrentOwnerMembership.Read` equal the 21 expected ids, its `FIX-` members number 122, and neither `FIX-015-001` nor `CAT-015-V` is a member | Baseline fails (no lettered member, 304 `FIX-`); a 22nd or missing heading, or a left-over removed row, fails; AC2 recount | planned |

No float is compared; every assertion is a set, a map, a string or a count.

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, with `CSHARP_STATIC_GATE_BINLOGS` exported -- expected: five logs, zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS, zero violations.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check` (a test file is added) -- expected: its baseline count, 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~CampaignContentTests|FullyQualifiedName~CurrentCatalogueTests|FullyQualifiedName~CurrentCapabilityInputsTests|FullyQualifiedName~CurrentOwnerMembershipTests|FullyQualifiedName~TimingDeclarationTests|FullyQualifiedName~LaunchLessonSolveTests|FullyQualifiedName~WorkshopSaveTests|FullyQualifiedName~PipeBend90Tests"` -- expected: 0 failed, 0 skipped.
- `dotnet run --project tools/Ownership -- --oracles` and the census recount (read-only: the oracle's pattern and digit-or-hyphen filter over the seven invest documents) -- AC2.
- The 24o(1) no-op `cmp` and `git diff --numstat --diff-algorithm=histogram` checks; the AC3–AC7 commands; `git add -N` the new files, then `git diff --check <baseline>` -- expected: no output.
- One full local round under `caffeinate -i` -- AC1. At the candidate commit, the unmodified `.githooks/pre-commit`.

### Running beside other work

Final diff scope: `content/puzzles.json`, `docs/planning/invest/current-consumers.md`, `docs/planning/invest/campaign.md`, `docs/planning/requirements.md` (`:186`, `:5998`), `docs/springboard-elastic-contract.md` (`:29`), `tools/Ownership/CurrentOwnerMembershipOracles.cs`, `tools/Coverage.Tests/CurrentCatalogueTests.cs`, `tools/Coverage.Tests/CurrentCapabilityInputsTests.cs`, `tools/Coverage.Tests/CurrentOwnerMembershipTests.cs`, `CuriousContraptions.tests/CampaignContentTests.cs` (new), `docs/planning/elements/readiness/campaign-coverage.md`, `docs/planning/invest/delivery-batches.md`, the 23 element documents of the owned-scope table, `_bmad-output/implementation-artifacts/deferred-work.md`, `docs/planning/elements/readiness/completion-ledger.md` (row OS-02), `_bmad-output/implementation-artifacts/run-logs/spec-tool-lettered-level-ids.md`, `TODO.md`, this contract and its run log. Nothing else.

Shared under 24m: `TODO.md`, `deferred-work.md`, `delivery-batches.md` (§4 and the Counts line `:352` here), `completion-ledger.md` (by row); `CAT-019-conveyor.md` and `CAT-064-tennis.md` under the 24o coordinator ruling (keep both sides at the later merge). No running lane edits `requirements.md` (the Baseball repair lists it as not edited; S5 names it only as published planning). Disjointness, checked on `2ce73eed` against each lane's owned scope:

- **Lane 1, part B** (`spec-elements-visible-solid-remainder-b.md`, in progress): owns `CAT-019-conveyor.md`; this slice edits `:67`, `:147`, `:160`, `:208` only (24o ruling), both sides kept at the later publisher's merge. B's contract cites array positions "solution 58, placement 59" and conveyor puzzles this slice removes (`:78`); stale citations, re-read and merged at the later publisher's merge. B runs `LaunchLessonSolveTests`, `WoundSpring*Tests` and `F32BehaviourIdentityTests`, which load `content/puzzles.json` by id; their entries change only in `subtitle`. Shared `delivery-batches.md` (B §5), `deferred-work.md`, `TODO.md`, the full-round total.
- **Lane 1, B2, C, Baseball repair**: code disjoint. B2 owns `CAT-064-tennis.md`; this slice edits `:94` only (24o ruling). C cites "one Balloon (placement 62)" (`:73`), a zero-based array index that becomes 33: stale, re-read at dispatch. The Baseball repair edits `scope-corrections.md:25`, one of the seven census documents, with no heading change, and only reads the Coverage tests. `VisibleSolidColliderTests.cs` does not read content. Shared `delivery-batches.md` §5, `deferred-work.md`, `TODO.md`, the total.
- **Lane 2, S5, S5b, S6–S6g**: code disjoint. S5 edits `CSharpStaticGateTests.cs`, which names `content/puzzles.json` only as an embedded resource (`:670–671`); S5b edits `tools/e2e/workshop-driver.ts:9` and `TimingDetector.cs`, untouched here (D2). S6–S6g read goal kinds, `DominoEffectTests` and `GoalSignalSelectorTests`, unchanged here. Shared `deferred-work.md`, `TODO.md`, `delivery-batches.md` (their sections), the total.
- **Lane 4, policy-tool fixes** (`spec-policy-tool-fixes.md`): code disjoint (`tools/anvil/`). It adds `CurrentCapabilityInputsTests` to the registration filter; this slice edits that class's literals without changing its test count; whichever publishes second re-measures the registration count on the merged candidate. Shared `completion-ledger.md` (OS-04 there), `deferred-work.md`, `TODO.md`.
- **Blocked drafts** (not running): `spec-early-level-proof-batch-1.md` cites authored positions and `FIX-` ids (`:77`, `:109`) that this slice moves or deletes; both are re-derived at its planning.

No other running lane edits `content/puzzles.json`, `current-consumers.md`, `campaign.md`, `CurrentOwnerMembershipOracles.cs` or the three Coverage test files. One publisher at a time under the 24m merge rule: the later publisher merges `origin/main` once, keeps both sides' entries in shared documents, re-reads moved citations and repeats the full local round on the merged candidate.

## Auto Run Result
