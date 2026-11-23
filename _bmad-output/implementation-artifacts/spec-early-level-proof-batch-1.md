---
title: 'Early level proof batch 1: air and capture levels'
type: 'feature'
created: '2026-11-21'
status: 'blocked'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/planning/invest/delivery-batches.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Status (23 November 2026):** waits for the engine programme (owner decision 22); no Early level proof runs before it. Revised for owner decisions 24d to 24f: `wall_return` is removed from the campaign, so this batch is three levels, and the campaign places `air_mail` and `third_dimension` in chapter 2 (plan levels 11a and 11b) and `a_gentle_rebound` in chapter 1 (plan level 3) ([campaign coverage](../../docs/planning/elements/readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)). The picker `order` values below stay interim. Line references, counts and record texts are re-derived at planning from the published main of that time, under the rule that every count follows the levels actually proven.

**Problem:** Forty-five planned levels used only delivered parts when counted; after owner decision 24d removed 29 of them, 16 remain (9 provable with single-goal evaluation, 7 after the goal slices), yet none is playable. They sit in the planned content shape, which the engine indexes but never constructs, so solutions written for the legacy engine have never met the current solver.

**Approach:** Author three single-capture levels (03 `air_mail`, 05 `third_dimension`, 61 `a_gentle_rebound`) into the playable shape and prove each natively: the real C# simulation solves the intended solution, a control fails, and Reset and save/load restore it. The slice is content, tests and records only.

## Boundaries & Constraints

**Always:**
- Each level keeps its identity, title, subtitle, description, hint, inventory, fixture identities (the slot is the planned part id), part kinds and locked state.
- Proof uses the production content reader, catalogue, placement interface and Simulation worker path through the existing integration harness.
- Each level meets the standing bar of a Playable level: solved at simulation cadences 60, 120 and 240 Hz and at precisions 0, 0.45 and 1.
- Assertions are outcomes: the goal occurrence, the Captured record's owners, a failing one-cause control, the authored state after Reset, decoded save equality and a solving replay after load. Every wait is bounded by a 10 game-second stall guard.
- A level becomes `Playable` only together with its proof. Every other entry of `content/puzzles.json` is unchanged.
- If a planned solution does not solve on the current solver, re-tune the solution part's placement first. Move a fixture only when no placement of the solution part can solve, and record the change and its reason.
- A level whose intended solution cannot be made to solve at every cadence and precision by content re-tuning stays `Planned` and unchanged in this slice, is recorded in `deferred-work.md` with its evidence and owner, and the slice proceeds with the remaining levels; every count in the records follows the levels actually proven.

**Never:**
- No engine, abstraction, schema, wire, tool-source, shared test harness or helper, or element package change. A level that needs one stays `Planned` under the rule above. The three ids added to `LaunchLessonSolveTests.Lessons` and the corrected count comment in `tools/Coverage.Tests/CurrentCatalogueTests.cs` are the only edits to existing test files; neither changes executable code.
- No level with several goals, a `turned` or `powered_after` goal, an activation goal, a connection or a rope.
- Not browser proof, not campaign difficulty or nudging qualification, and not completion of a chapter story.
- No hash or byte-identity assertion, fixed arrival tick, trajectory pin, tolerance fitted to output, skipped test or mutant.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| L03 `air_mail` | Locked Tennis ball above the bench and locked Receiver to its right; one self-contained Fan placed left of and below the ball, blowing toward the Receiver | The ball is captured by the Receiver within the stall guard. With the Fan removed, or placed the same but switched off, it is never captured | No error expected |
| L05 `third_dimension` | Locked Tennis ball at depth −2 m and locked Receiver at depth +2 m; one Fan placed behind the ball, turned to blow along depth | Captured within the stall guard. With the Fan removed, or placed the same but not turned, never captured | No error expected |
| L61 `a_gentle_rebound` | Locked Basketball above the bench and locked Receiver to the right; one Trampoline placed under the ball, tilted toward the Receiver | Captured within the stall guard. With the Trampoline removed, or placed the same but flat (struck, yet missing), never captured | No error expected |

</intent-contract>

## Code Map

Facts measured at `8d1d68aa` (published main); the cited code and content are unchanged at `25df3cd5` except that main now has 45 packages (the Baseball). `baseline_revision` is captured at dispatch (coordinator decision 38, 23 November 2026) from the published main that contains the planning work, before first implementation; scope guards compare against it. `25df3cd5` is the drafting revision only, not a baseline.

**Status: blocked on the engine programme (owner decision 22).** Owner: coordinator. Retry condition: `ENGINE-SOA-SIMD` (the programme's last story) published; the capability probe and every count are then re-checked on that main.

**Planning verdict.** Scoped Pass after this repair (planning auditor `a365db0929fe17531`, second pass, 21 November 2026, stated the repair as its exact fix); engine tier.

**Delivery shape and tier.** An **Early level proof** slice (loop protocol, Slice shapes): one ordinary record, its own worktree from published main. **Engine tier** (all six layers on the first pass). Every capability is present, but the comment edit at `tools/Coverage.Tests/CurrentCatalogueTests.cs:61` and `TODO.md` are not on the data-only allow-list as written and doubt resolves to engine tier. The `oversized` warning is the native template's marker for a contract above 1,600 tokens; the protocol's 6,000-word budget governs and is met.

### Recount of section 4 (`docs/planning/invest/delivery-batches.md`)

Measured with `python3` over `content/puzzles.json` against the `Metadata.Id` of the 44 packages under `elements/` at `8d1d68aa` (45 at `25df3cd5`; no level places `baseball`, so the counts below are unchanged by it):

- 63 levels: 15 `Playable`, 48 `Planned`. 45 Planned levels use only delivered parts; the other three need `flashlight` and `solar_panel`.
- 13 of the 45 have one goal: seven `captured`, five `activated`, one `turned`.
- **Eleven, not twelve, are expressible today.** Level 02 `power_trip` has one `activated` goal on its lamp, but the engine's only activation goal needs a distinct fixed predecessor node (`engine/gpu/WorkshopPuzzle.cs:22-24`, `engine/gpu/PuzzleDefinition.cs:229-235`), and that level's fixed parts are a ball and the lamp. It waits for ENGINE-GOALS with level 14.
- The eleven were 03, 05, 12 and 61; Signals 07 and 08; Drive 16, 17 and 18; Ropes 19 and 20. Owner decision 24d removes 08 `wind_signal` and 12 `wall_return`, leaving nine; this batch is 03, 05 and 61, and six remain for later batches (07, 16, 17, 18, 19, 20). The other seven kept levels (02, 09, 14, 15, 29, 49, 58) wait for the goal slices, so none of the 16 is outside this account.
- Ropes 19 and 20 also need an authored rope length: `AuthoredConnection` carries none (`engine/gpu/PuzzleDefinition.cs:15-16`) and a rope connection requires one (`engine/gpu/WorkshopConnections.cs:166`). That is a content-field addition, so their batch is engine tier.

**Batch size.** One group of three: all single `Captured` goals, no connection, no capability beyond what executed tests already cover. Three matrix rows and five criteria keep the contract inside budget; a level costs about 250 words here. Later batches: Signals (1), Drive (3), Ropes (2).

### Owned scope

- `content/puzzles.json` -- the three entries (today's authored positions 3, 5 and 61; zero-based array indices 2, 4 and 60; the plan places them at 11a, 11b and 3, and the content slice re-orders them).
- `CuriousContraptions.tests/LaunchLessonSolveTests.cs:88-94` -- add the three ids to `Lessons` (a list line).
- `CuriousContraptions.tests/EarlyLevelProofBatch1Tests.cs` -- new test file (level content tests only; no helper).
- No test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`).
- `tools/Coverage.Tests/CurrentCatalogueTests.cs:61` -- the comment "260 Planned parts plus 44 Playable placements" becomes 254 and 50 (six fixtures move; the asserted total 304 is unchanged).
- `docs/planning/invest/delivery-batches.md` section 4 (the level-count line, the provable and 24d reconciliation bullets, the provable-levels heading and group table) and its closing section summary, `docs/planning/elements/readiness/campaign-coverage.md` (the Early level proof paragraph and its batch 1 bullet), `_bmad-output/planning-artifacts/epics.md` (the Early level proof paragraph before Story 15.0), `_bmad-output/implementation-artifacts/sprint-status.yaml` (above the `15-1-campaign-chapter-1-levels-1-10` key), `TODO.md`, `_bmad-output/implementation-artifacts/deferred-work.md` (close-note items and any level left `Planned`), this contract and its run log.

### Capability probe

Performed while drafting; the planning scout re-verifies each citation.

| # | Capability | State | Symbol | Exercising test |
|---|---|---|---|---|
| 1 | Playable entry admission (placements, inventory, solution, goal, profiles, presentation) | present | `engine/gpu/PuzzleDefinition.cs:98` | `WorkshopSaveTests.EveryLessonAdmitsItsAuthoredPlacementsInventoryGoalAndKnotsAsF32` |
| 2 | Single `Captured` goal evaluation | present | `engine/gpu/WorkshopGoalEvaluator.cs:27-36` | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` |
| 3 | Production level creation, canonical solution, player placement, precision change | present | `engine/gpu/PuzzleCatalogue.cs:25,47,55,71` | `LaunchLessonSolveTests.EachLaunchRouteSolvesOrStaysUnsolvedAndResetRestoresTheBench` |
| 4 | Run, Pause, Step, Resume, Reset and save/load replay of a level | present | `CuriousContraptions.tests/WorkshopIntegrationHarness.cs:178,194,420` | `LaunchLessonSolveTests.cs:127-189` (same theory) |
| 5 | Self-contained airflow moving a payload, and none when off | present | `elements/Fan/FanElementDeclaration.cs:20-21,53` | `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` |
| 6 | Airflow carrying a Tennis ball into a Receiver | present | `elements/TennisBall/TennisBallElementDeclaration.cs:12` | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` |
| 7 | Aimed compliant surface delivering a ball into a Receiver; flat control | present | `elements/Trampoline/TrampolineElementDeclaration.cs:27-28` | `TrampolineTests.TheAimedBedDeliversTheBallIntoTheReceiverAndTheControlsDoNot` |
| 8 | Withdrawn: it served `wall_return`, which owner decision 24d removes | — | — | — |
| 9 | Two inventory kinds in one level | present | `engine/gpu/PuzzleDefinition.cs:189-210` | `wall_and_bumper` in the theory of row 2 |
| 10 | Fixture identity, kind and locked checks across both content shapes | present | `tools/E2eRunner/LessonFixtures.cs:17`, `tools/Coverage/CurrentCatalogue.cs:180` | `CurrentCatalogueTests.ActualCurrentContractRetainsAllIdentitiesModesAndFixtures` |
| 11 | Unique presentation order and picker membership | present | `engine/gpu/PuzzleCatalogue.cs:15,174` | `WorkshopSaveTests.ContentLoadRejectsEmptyDuplicateAndNoPlayableCatalogue` |

Counts: 10 present (row 8 withdrawn), none untested, narrower or missing. No prerequisite slice. The combination each level makes (for example a self-contained Fan, a Tennis ball and a Receiver) is the level's own proof, not a new capability.

### Direct consumers (searched)

- `LaunchLessonSolveTests.cs:97-98` and `F32BehaviourIdentityTests.cs:38` fail until `Lessons` equals `PuzzleCatalogue.Playable`. `Lessons` then drives two theories for each new id (`LaunchLessonSolveTests.cs:100-125`, `F32BehaviourIdentityTests.cs:20-27`).
- `WorkshopSaveTests.cs:228-229` reads every Playable entry and checks each placement bit for bit against the authored numbers.
- `LaunchLessonSolveTests.cs:116-120` picks the control: the first connection, or the first unlocked instance. Each of these solutions has exactly one part and no connection, so the control is "the solution part removed", which equals the unsolved start.
- `tools/Coverage/CurrentCatalogue.cs:184-192` binds fixtures by `lesson/slot`, kind and locked state to `docs/planning/invest/current-consumers.md` (rows FIX-001-006, FIX-001-062, FIX-004-002, FIX-004-004, FIX-004-008, FIX-004-057, FIX-015-001, FIX-064-001, FIX-064-002). Keeping each slot equal to the planned part id leaves that document unchanged.
- `ui/Workshop.cs:129` and `CuriousContraptions.tests/NativeSceneFixture.cs:169-172` list Playable levels from the catalogue; no edit.
- `tools/E2eRunner/ImpactRules.cs:123` and `tools/E2eRunner/Selection.cs:72` treat the content file as level impact; no edit. No suite declares these three levels (`tools/E2eRunner/SuiteCatalogue.cs`); browser cases are queued, not written.
- `docs/coverage/` is a frozen snapshot no tool reads; no edit.
- No engine or tool source names any of the three ids (`rg -n "air_mail|third_dimension|a_gentle_rebound" -g '*.cs' -g '*.ts'`).

### Authoring into the playable shape

Pattern entries: `first_principles` (fixed ball and Receiver, single `Captured` goal) and `wall_and_bumper` (two inventory kinds, Wall parameters, Bumper). Each new entry has exactly the keys `PuzzleDefinition.cs:113-114` requires; `parts` and the planned `goals` are removed.

- **placements**: each planned `parts` row, in order, as `{"slot": <planned id>, "instance": {...}}` with `locked` true.
- **cell and local**: per axis, `cell` is the nearest integer to metres × 16 and `local` is metres × 16 − `cell`. In `first_principles` −3.4 m is cell −54, local −0.4.
- **rotation**: the quaternion `[x, y, z, w]` of the planned `orientation`, whose nine numbers list the world images of local X, Y and Z in turn. Identity is `[0, 0, 0, 1]`. Check by reading, not by convention: the Trampoline is turned −30° about Z (normal leaning toward +X), and the Fan of `third_dimension` blows along world +Z.
- **the two rotations here**: the Trampoline is `[0, 0, -0.25881905, 0.9659258]`. The Fan of `third_dimension` is `[0, -0.70710677, 0, 0.70710677]`, the opposite sign to the fixtures of `bumper_depth` (`[0, 0.7071068, 0, 0.7071068]`), which face the other way along depth.
- **parameters**: every declared parameter of the part at its declared default, except a value the planned `properties` set (Wall `width` 0.4, `height` 6, `thickness` 1.5). The Receiver takes the six parameters and five `assistance_bindings` of `first_principles` `fixture_2`; the Bumper takes `{"strength": 8}`; the Fan keeps `supply` 0 and `powered` 1.
- **assistance**: three rows of eleven lanes from the part's planned `difficulty` rows, in the lane order of `AssistanceKnot` (`engine/gpu/WorkshopPuzzle.cs:34-37`).
- **inventory**: one row per planned kind in planned order with its count, `goal_target` false and `configuration` null. Its assistance is the planned solution part's `difficulty`; the unused Ramp of `air_mail` takes the Ramp row of `first_principles`.
- **solution**: each planned solution part as slot and instance with `locked` false. `solution_connections` is empty and `connections_allowed` is false.
- **goal**: `{"kind": "Captured", "source": <payload slot>, "target": "receiver", "minimum_seconds": 0}`. If `spec-engine-goal-evaluation` is on main at baseline, author instead `"goals": [{"kind": "Captured", "owner": "receiver", "subject": <payload slot>}]`. If that slice publishes while this one is in flight, probe rows 1, 2 and 9 are re-verified on merged main and converting the three goals is a patch, not a return to planning.
- **profiles**: `primary` is the payload's rows, `target` the Receiver's, `placement` the solution part's. `secondary` is all zeros for the three one-kind levels, deliberately, as in every one-kind Playable capture level; for `air_mail` it is the Ramp row, as `wall_and_bumper` carries its other kind's row there.
- **presentation**: `title` and `hint` as the entry's; `task` is the description; `precision_status` is "Receiver assistance updated. Placement remains manual."; `order` and `status` below.

| Level | Fixtures (slot: part at metres) | Inventory | Intended solution (starting values) | `order` | `status` |
|---|---|---|---|---|---|
| `air_mail` | `ball`: tennis (−3, 5, 0); `receiver`: basket (1.7, 0.9, 0) | fan 1, ramp 1 | `fan_1`: fan (−4, 3.8, 0), unrotated | 12 | "Place the fan beside the falling ball. Placement remains manual." |
| `third_dimension` | `ball`: tennis (−1, 5, −2); `receiver`: basket (−1, 0.9, 2) | fan 1 | `fan_1`: fan (−1, 3.8, −3), turned so local +X is world +Z | 13 | "Place the fan behind the ball and turn it toward the receiver. Placement remains manual." |
| `a_gentle_rebound` | `payload`: ball (0, 7, 0); `receiver`: basket (5.4, 1.2, 0) | trampoline 1 | `bed`: trampoline (0, 3, 0), −30° about Z | 60 | "Place the trampoline beneath the ball and tilt it toward the basket. Placement remains manual." |

### Decisions made (defensible from the game's intent)

- **Slots keep the planned part ids** (`ball`, `receiver`, `payload`), as `wind_then_release` does, so coverage fixture identities do not change.
- **Presentation orders 12, 13 and 60**: unused values; no existing entry is renumbered, so AC5 holds. Picker order is interim per `docs/planning/invest/campaign.md:40`. Uniqueness is the engine's only rule (`PuzzleCatalogue.cs:174`).
- **The solution may be re-tuned; the lesson may not.** These solutions were written for the legacy solver (the authored wall of `wall_and_bumper` stands at −1.75 m, where this level's planned wall is at −1 m). The table's values are starting values.
- **Both inventory kinds of `air_mail` stay**, though the intended solution uses only the Fan: the planned lesson offers the Ramp.
- **Three cadences and three precisions per level**, because the existing theories apply that bar to every Playable level and an exemption would be a second rule.

No decision cuts scope or waives a gate. None needs the owner.

### What "proven natively" claims

It claims, for each level: the entry loads through `PuzzleCatalogue`; the intended solution, placed through the production interface, reaches the goal in the real C# simulation within the stall guard; one-cause controls do not; Reset restores the authored state; a saved solved construction decodes equal and solves after load.

It does not claim: Chrome or any browser behaviour, real pointer construction, readable art or motion, device performance, campaign difficulty or nudging qualification across the tolerance range, uniqueness of the solution, or completion of chapter stories 15.1 or 15.2. Browser qualification is recorded as pending for the post-catalogue phase.

### Publishing beside an element slice

Run in a dedicated worktree from published main. Shared files: `content/puzzles.json`, `LaunchLessonSolveTests.cs`, `docs/planning/invest/delivery-batches.md`, `docs/planning/invest/vertical-delivery.md`, `TODO.md`, `sprint-status.yaml`, `deferred-work.md`. It does not touch coverage or impact tables. One publisher at a time: whichever slice publishes second merges main once and repeats the full local round on the merged candidate before approval. Against `spec-engine-goal-evaluation` the same rule applies, and the later slice writes the goal in the shape then on main. The commit path needs nothing special: `.githooks/pre-commit` builds the index snapshot, and the new test file is staged with `git add` (S2's staged-source rule in `tools/anvil/CheckStagedTests.cs` refuses an untracked or intent-to-add C# source at commit).

### Carry-forward

No open `deferred-work.md` entry names these three levels, the Fan, the Trampoline or the Wall as blocked. A defect found here that content cannot fix is added there with its evidence and owner.

## Tasks & Acceptance

**Execution:**
1. `content/puzzles.json` -- rewrite the three entries by the authoring rules and set `availability` to `Playable` -- the engine constructs only that shape.
2. `CuriousContraptions.tests/LaunchLessonSolveTests.cs` -- append the ids of the levels proven to `Lessons` -- enrols each level in the solve, control, lifecycle and save/load theories.
3. `CuriousContraptions.tests/EarlyLevelProofBatch1Tests.cs` -- add the two theories of the table below -- the authored-shape proof and the second control. The control is built from `PuzzleCatalogue.CreateSolution(…, new(0f))` at the default 120 Hz cadence. Its single unlocked instance is replaced, through `WithoutInstance` and `PuzzleCatalogue.WithInstance`, by a `with` copy that differs in exactly one field, and `Validate` is called. It is observed for 1,200 ticks with `WorkshopIntegrationHarness.ObserveReset`. The three copies are: `powered` 0; `Rotation` identity; `Rotation` identity. Precision 0 is chosen because the widest capture margin (0.3 m) and the 12 m/s² guide are where a control is most likely to solve by accident. Helpers in the new file are private to it, and the file must pass the existing skip scan (`SkipScan`, `tools/anvil/CheckStagedTests.cs:227`, patterns in `tools/anvil/no-skipped-tests.json`).
4. Run the affected filter. If a level does not solve at every cadence and precision, re-tune the solution part's placement in content, state the gameplay reason in the run log and rerun. After two failed attempts at one level, reassess per the stagnation rule. A level content cannot make solve stays `Planned` under the intent's rule, with its `deferred-work.md` entry, and the slice continues with the others.
5. `tools/Coverage.Tests/CurrentCatalogueTests.cs:61` -- correct the comment's two counts.
6. Records. Every count follows the levels actually proven. With N proven (N = 3 if all are): Playable is 15 + N (18), "provable with single-goal evaluation" is 9 − N (6: 07, 16, 17, 18, 19, 20), the 7 after the goal slices (02, 09, 14, 15, 29, 49, 58), the 3 blocked on parts and the 29 removed do not move (63 in all), tests rise by 8 N (24) and the moved fixtures are 2, 2 and 2 for the three levels in order. The current texts below are cited by heading, not line, as they read at drafting (owner decisions 24d to 24f and coordinator decision 41 applied); they are re-derived at planning for the levels actually proven.
   - `docs/planning/invest/delivery-batches.md` section 4, count line -- "`content/puzzles.json` holds 63 levels: 15 `Playable` and 48 `Planned`." becomes "18 `Playable` and 45 `Planned` (15 and 48 before early level proof batch 1; the counts below are of those 48)."
   - Section 4, the bullet "**11 of the 45 can be proven with single-goal evaluation.**" gains "Batch 1 proved 03, 05 and 61 (08 and 12 are removed by owner decision 24d), leaving six." The owner decision 24d reconciliation bullet gains "After batch 1: 18 Playable + 6 provable + 7 after the goal slices + 3 blocked on parts + 29 removed = 63."
   - Section 4, the heading "**Provable with single-goal evaluation (9), after the engine programme.**" becomes "**Provable with single-goal evaluation (9; 03, 05 and 61 proven natively in batch 1, six remain), after the engine programme.**" and the Air and capture row gains "proven natively in batch 1; browser pending". The closing "Section 4:" summary line gains "After batch 1: 18 playable + 6 provable + 7 after the goal slices + 3 blocked on parts + 29 removed."
   - `docs/planning/elements/readiness/campaign-coverage.md`, the **Early level proof** paragraph, gains "After batch 1: 18 Playable + 6 single-goal provable + 7 after the goal slices + 3 blocked on parts + 29 removed = 63"; its batch 1 bullet drops "(this effort's draft)" and says the batch is proven natively with browser qualification pending.
   - `_bmad-output/planning-artifacts/epics.md`, the **Early level proof** paragraph before Story 15.0 -- "(21 November 2026 audit; after the owner decision 24d removals, 9 levels are provable and 7 wait for the goal slices)" gains "; batch 1 proves 03, 05 and 61 natively with browser qualification pending, leaving six".
   - `sprint-status.yaml` -- add `early-level-proof-batch-1: review # levels 03, 05 and 61 native; browser pending` above `15-1-campaign-chapter-1-levels-1-10`; the chapter keys stay `backlog`.
   - `TODO.md` -- the **Outcome** paragraph takes one line: "Early level proof batch 1: levels 03, 05 and 61 are Playable and proven natively; browser qualification pending."
   - `docs/planning/invest/vertical-delivery.md` carries no Early level proof count today (its text says only that a level may be proven after the engine programme); no edit.

**Acceptance Criteria:**
- AC1: Given the authored entries, when the catalogue is read, then each proven level loads with its fixtures at the positions in the Code Map table (a fixture moved under the re-tune rule is asserted at its recorded new position, with the reason in the Spec Change Log), its inventory kinds and counts, a `Captured` goal from its payload to its Receiver and its presentation, and `PuzzleCatalogue.Playable` lists 15 plus the levels proven (18 with all three).
- AC2: Given any run of the levels proven, solved or not, when Reset is committed, then the committed state equals the state built before Run, the goal cue is neutral and the construction is unchanged.
- AC3: Given each solved construction, when it is saved and loaded, then the decoded construction equals the saved one and a fresh run of it reaches the goal within the stall guard.
- AC4: Given the changed content, when the coverage and runner tools read it, then the 304 fixture identities, kinds and locked states still match `current-consumers.md` with no edit to that document.
- AC5: Given the baseline and candidate content files, when their decoded entries are compared, then only the entries of the levels proven differ.

## Spec Change Log

## Review Triage Log

## Design Notes

Design answers for the rules that apply (E1, E4, E7, E8, E9; no engine behaviour changes, so D1 to D7 are met by the existing contracts).

1. **Behaviour and scope.** Three planned levels become playable with native proof; Early level proof rule, section 4 of the delivery batches.
2. **Reusable primitive.** None new. The levels compose delivered capabilities through the one playable content shape.
3. **Declared composition.** Level data only: placements, inventory, solution, goal and assistance rows, validated by the production reader.
4. **Execution and ownership.** Content → `PuzzleDefinitionReader.Parse` → `PuzzleCatalogue` → compiled scene → worker; Reset and save/load use the existing paths. Nothing new owns state.
5. **Generality.** Not an engine claim. Each level has two one-cause controls so a pass cannot come from the fixtures alone.
6. **Forward-only.** The planned-shape entries of these three levels are replaced, not kept beside the playable ones. Nothing else is deleted, so no protection is removed: the two list guards and the reader's refusals stay.
7. **Prerequisites.** The engine programme (owner decision 22). The slice does not depend on ENGINE-GOALS.

## Verification

Executors: the implementation owner runs every command; the coordinator inspects the results; a designated executor repeats the full round only if the candidate changes after handoff, including by a merge of main. Evidence is invalidated by a change to content, test source, dependencies or build configuration.

**Commands:**
- Session check: `test "$(node --version)" = v26.12.1`, `dotnet --version`, `dotnet workload list` -- expected: v26.12.1, the `global.json` SDK, wasm tools present.
- The five compiler-log builds of `docs/delivery-workflow.md` "Reproducible local verification" -- expected: all succeed. The content file is an embedded resource, so rebuild after every content edit.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: both canary `PASS` lines, the `Coupling dependencies:` summary, zero violations.
- The registration filter of `docs/delivery-workflow.md` "New-package registration check" -- expected: zero failed, `Skipped: 0` (a test file is added; `CSharpStaticGateTests` and `CurrentCatalogueTests` are in the filter).
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~EarlyLevelProofBatch1Tests|FullyQualifiedName~LaunchLessonSolveTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~WorkshopSaveTests|FullyQualifiedName~CurrentCatalogueTests|FullyQualifiedName~TimingDeclarationTests|FullyQualifiedName~SelectionTests"` -- expected: zero failed, `Skipped: 0`, and every table row's test listed as run for each of the three ids.
- AC5: `python3 -I` decoding `git show <baseline_revision>:content/puzzles.json` (after a merge of main, the merged main revision) and the working file, printing the ids whose entries differ -- expected: exactly the ids of the levels proven (with all three: `air_mail`, `third_dimension`, `a_gentle_rebound`).
- `git add -N CuriousContraptions.tests/EarlyLevelProofBatch1Tests.cs`, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i` (the `local_deferred_filter` and `dotnet test CuriousContraptions.slnx ...` lines of the same section) -- expected: zero failed, `Skipped: 0` in every project. Expected test delta: +8 per level proven in `CuriousContraptions.tests` (3 cadence rows, 3 precision rows and 2 new theory rows; +24 with all three) and none elsewhere; record the measured delta and explain any difference.

### Acceptance-to-test table

Tolerances and windows chosen now:
- **Stall guard:** 10 game seconds converted by the selected cadence (`LaunchLessonSolveTests.cs:122`; 1200 ticks at 120 Hz in `F32BehaviourIdentityTests.cs:11`). The longest route, a 7 m drop, a rebound and a 5.4 m flight with the 0.35 s capture dwell, takes under 4 s.
- **Authored position:** 0.1 mm, the existing authoring-quantisation allowance (`DominoEffectTests.cs:35-37`); it is not a motion tolerance.
- **Controls** assert "never solved within the stall guard". Engagement uses a sign or an intentional zero only: airflow work exactly 0 for the switched-off Fan (as `FanGuidanceTests.cs:34` does), and an upward ball velocity after the flat bed is struck.
- No position, speed or arrival tick of a simulated body is asserted.

| Rows | Named discriminating test | Control | Boundary | Run |
|---|---|---|---|---|
| L03, L05, L61 solve | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal(<id>, cadence)`: solved, Captured record from the Receiver about the payload, goal cue lit | Same theory: the solution part removed is never solved and the cue stays neutral | Cadences 60, 120 and 240 Hz; Pause holds the tick and Step advances exactly one | planned |
| L03, L05, L61 precision | `F32BehaviourIdentityTests.ALessonPreservesGoalsAndCausalEventOrderAndReplaysAfterSaveLoad(<id>, precision)`: one capture latch, preceded by a clear state | Covered by the removal control above | Precisions 0, 0.45 and 1 (capture margin 0.3 m down to 0.02 m) | planned |
| L03 second control | `EarlyLevelProofBatch1Tests.TheSolutionPartMisplacedInOneRespectDoesNotSolve("air_mail")`: Fan at the solution pose with `powered` 0 | The solving theory row with `powered` 1 | Airflow work is exactly 0 | planned |
| L05 second control | Same theory, `"third_dimension"`: Fan at the solution position, unrotated | The solving row with the Fan turned | Never captured within the guard at precision 0, the widest capture margin | planned |
| L61 second control | Same theory, `"a_gentle_rebound"`: Trampoline at the solution position, flat | The solving row with the bed tilted | The ball is struck (its vertical velocity turns upward) yet is not captured | planned |
| AC1 | `EarlyLevelProofBatch1Tests.TheAuthoredLevelKeepsItsPlannedFixturesInventoryAndCapturedGoal(<id>)`; `WorkshopSaveTests.EveryLessonAdmitsItsAuthoredPlacementsInventoryGoalAndKnotsAsF32`; `LaunchLessonSolveTests.EveryPlayableLessonHasAnExplicitExecutedScenario`. Expected positions, kinds, counts and slots are literals in the test, taken from the Code Map table, not read back from `content/puzzles.json` | A level id absent from `Lessons` fails the last guard | Fixture positions within 0.1 mm; inventory counts exact; 15 plus the levels proven Playable ids | planned |
| AC2 | The solving theory: `NoFixedCapacityTests.AssertSameState(built, reset)`, neutral cue and equal construction, on the solved and the control run | Before Reset the solved run's cue is lit | Reset after the solved run and after the failed run | planned |
| AC3 | The solving theory's saved replay (`LaunchLessonSolveTests.cs:175-189`) and the precision theory's reloaded run | Save while running is refused with `WorkshopRejection.WrongPhase` (`:167`) | Decoded construction equals the saved one at three cadences and three precisions | planned |
| AC4 | `CurrentCatalogueTests.ActualCurrentContractRetainsAllIdentitiesModesAndFixtures`; `TimingDeclarationTests.DeclaredLevelsExist` | `CurrentCatalogueTests` refuses a renamed slot or changed kind (`:119-120`) | 304 fixtures | planned |
| AC5 | The decoded-entry comparison command | not a test | Exactly the proven ids differ | planned |

Proof limits: native C# simulation through the worker transport only. Browser, performance, remote build, artifact and deployment qualification are unverified and stay at their named gates.
