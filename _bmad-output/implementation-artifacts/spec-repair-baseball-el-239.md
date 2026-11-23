---
title: 'Repair: Baseball (EL-239) to the owner decisions — the lightest ball, real proportions, one proud band'
type: 'bugfix'
created: '2026-11-22'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/docs/planning/elements/EL-239-baseball.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-named-el-239.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The published Baseball (`origin/main` `25df3cd5`) is 0.65 kg and 0.25 m with the role "medium-light", and draws two seams that stand 2.6 mm proud of its sphere with no collider. The owner decided on 21 and 22 November 2026 that the Baseball has realistic proportions (0.15 kg, radius 0.26 m), is "the lightest ball", and carries one proud band with its own collider (coordinator decision 36, 22 November 2026, records these as the repair's target values); and it is missing from the visible-solid test.

**Approach:** Repair the published package in place: change its declared mass, inertia, radius, description and pick radius; replace the two seams with one band and its torus collider in the Basketball's stripe pattern; re-author the tests that assert the old values or role and add the lightest-ball rows and the Receiver-capture row; add its row to the visible-solid test; update its spec and the records that quote the old values. Forward-only: no old value, node or test is kept.

## Boundaries & Constraints

**Always:**
- Package id `baseball`, directory `elements/Baseball/`, palette order 68, icon, colours, bounce 0.35, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s and drag 0.04 1/s stay as published.
- The `## Package source binding` section of the Baseball spec stays valid in the syntax current on main when the slice starts.
- Every changed or new test runs real production code with the tolerances fixed here: the C# simulation for motion and capture rows (R3 to R9), the production bundle and package scene for geometry rows (R1, R2).
- The declaration stays expression-bodied constant data.

**Never:**
- No engine, abstraction, bundle, wire, tool or TypeScript change; no shared test harness or helper change.
- No second Baseball, alias, parameter, mode or retained old seam node; no migration of saves (a save names the part only).
- No level or fixture that places a Baseball; no change to the Soccer ball or Cannonball plan.
- No hash, byte-identity or fixed-arrival-tick assertion; no skipped test; no mutants; no browser run.

## I/O & Edge-Case Matrix

Drop rows release the ball with its centre 3 m above the bench origin. Plate rows place one ball at the plate centre and two balls 0.5 m either side of it, clear of the plate's centre ring.

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| R1 | Baseball placed from the palette | One dynamic body, a sphere of radius 0.26 m with one raised band 0.01 m proud: mass 0.15 kg, inertia 0.004056 kg·m² on each axis, restitution 0.35, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s, ambient drag 0.04 1/s, no parameters | A save or instance carrying any parameter for `baseball` is refused with the typed reason; nothing is installed |
| R2 | Baseball scene | A 0.26 m sphere and one band torus (inner radius 0.242 m, outer 0.27 m) are drawn; each has its collider; no seam node remains; nothing is drawn or collides beyond 0.27 m | No error expected |
| R3 | S636 comparison: Baseball, Basketball, Bowling ball and default Tennis ball each dropped alone; Balloon as rising control | The Baseball's first rebound peak differs from each of the other three by at least 0.15 m; the Balloon rises | A failure is a defect to diagnose; the slice halts `blocked` |
| R4 | Baseball, default Tennis ball and Basketball, each alone on the axis of a default Fan, 3 m in front of it and in its authored pose, for 12 ticks | The Baseball's speed along the flow is at least 1.5 times the Tennis ball's and at least 3 times the Basketball's | Fan off: the Baseball's speed along the flow stays zero |
| R5 | One, then two Baseballs on a default 0.5 kg Pressure plate; two default Tennis balls as control | One or two Baseballs leave the plate off; two Tennis balls switch it | Plate minimum set to 0.25 kg: two Baseballs switch it, one does not |
| R6 | Baseball at the mouth of a default Toy cannon | It never seats (0.52 to 0.54 m wide against the 0.6 m minimum) | No error expected |
| R7 | Baseball dropped on the bench, and placed resting on it | It rebounds no higher than its release, settles within 1 mm of its 0.26 m resting height and below 0.05 m/s | No error expected |
| R8 | Run, Pause, Step, Resume, Reset, Save and Load | Authored pose and kind restored; the visual follows the committed body | A save with a non-finite pose is refused `NonFinite` |
| R9 | A Baseball inside a default Receiver, centre 0.1 m above the Receiver's origin; the same Baseball 3 m beside it | The aligned Baseball is captured once within 240 ticks, not before the default 0.35 s dwell; the shifted one is never captured | No error expected |

</intent-contract>

## Code Map

Shape: **single element repair** on a published package. Baseline: the published main that contains the planning work (drafted on `25df3cd5`; precondition under Serialisation). The coordinator captures `baseline_revision` at start from that published main (coordinator decision 38, 23 November 2026), and every scope guard and `git diff` here compares against it; `25df3cd5` is the drafting revision only, not a baseline. Every code `path:line` below was re-read at `25df3cd5` on 23 November 2026 with `git show 25df3cd5:<path>`; between `fa86969f` and `25df3cd5` no cited code file changed (only the test project file, `tools/anvil/` and `delivery-batches.md`).

**Status: draft; runs before the engine programme.** It is a repair allowed by owner decision 22. It no longer waits on EL-223: owner decision 24g (23 November 2026) pauses the Passive impact scoop until the programme lands, so this repair starts in main's binding form (audit B4's alternative).

**Planning verdict:** Scoped Pass (ready-for-dev) per the fourth audit (`docs/planning/elements/readiness/audits/audit-baseball-repair-4.md`, Incomplete with one text fix); fixes applied 23 November 2026. The fourth audit found the third audit's B1–B5 and N1–N5 resolved and raised B1 (frontmatter baseline) and N1–N3; B1, N2 and N3 are applied here, and N1 (the Soccer ball spec's description of the Baseball's art) is applied in the planning work, outside this slice's owned scope. No row needs re-audit. The third audit (`docs/planning/elements/readiness/audits/audit-baseball-repair-3.md`, Incomplete) found B1–B5 and N1–N5; all are applied here.

**Binding form.** Main's form stays: the `## Package source binding` section keeps its `**Decision:** [S636](…)` line, and AC4 names `CurrentCatalogueTests.NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias` (`tools/Coverage.Tests/CurrentCatalogueTests.cs:262` at `25df3cd5`). EL-223's planned `**Ownership:**` form and renamed test are that slice's to apply when it resumes.

### What is wrong on main (read at `25df3cd5`)

- Mass 0.65 kg, inertia 0.01625 (`elements/Baseball/BaseballElementDeclaration.cs:22`–`:23`); description "A medium-light seamed ball with moderate bounce." (`:11`); pick radius 0.35 (`:13`).
- Sphere and collider radius 0.25 m (`elements/Baseball/baseball.tscn:13`, `:26`); `Visual/SeamLeft` and `Visual/SeamRight` share a torus of inner radius 0.213 and outer 0.219 at x = ±0.125 (`:18`–`:23`, `:35`–`:43`), which reaches 0.2526 m from the centre, 2.6 mm outside the only collider `col_sphere` (`:45`–`:47`).
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs` has no `baseball` row (its table ends the ball rows at `:109`–`:110`), so the art was never compared with the collider.

### Capability probe

Counts: 13 present, 0 present-untested, 0 narrower, 0 missing.

| # | Capability | Mark | Symbol | Exercising test |
|---|---|---|---|---|
| 1 | Sphere collider read from the package scene | present | `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `BaseballTests.FixedPresetCompilesCanonicalMassAndMaterialAndRejectsForeignParameters` |
| 2 | A proud band drawn as a torus with its own torus collider on a dynamic ball | present | `elements/Ball/ball.tscn` (`Visual/Visual_Stripe`, `col_visual_stripe`); `tools/BundleElements/SceneGeometryExtractor.cs:24` | `BasketballResourceTests.TheBundleAndSceneUseDeclaredNumericMaterials`; `CompoundMassAndRollingTests.EveryAssignedVisibleFeatureStopsAProbeAndLeavesAdjacentSpaceEmpty` (`BasketballSeam`) |
| 3 | Constant mass frame independent of collider children | present | `CuriousContraptions.Abstractions/MassFrameValues.cs:41` | `CompoundMassAndRollingTests.CollisionChildrenDoNotChangeDeclaredFreeFlight` |
| 4 | Collider material shared by a body's colliders | present | `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21` | `PackageRuntimeParityTests.EveryPayloadPackageCompilesItsDeclaredMaterial` |
| 5 | Airflow force on a receiver, falling linearly to zero at the flow speed | present | `CuriousContraptions.Abstractions/ConicalAirflow.cs:19`–`:26`; `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:126` | `ConicalAirflowTests` (`ConicalAirflowTests.cs:23`–`:28`, the linear-slip law); `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` (its Baseball rows) |
| 6 | Contact-load threshold on summed declared mass, inclusive | present | `engine/gpu/ContactLoadSensorDeclaration.cs:27` | `PressurePlateTests.TheInstanceMinimumMassDecidesItsOwnGate`; `TennisInteractionTests.TwoDefaultTennisBallsLoadThePlateWhileOneStaysBelowThreshold` |
| 7 | Chamber seating by declared width window over every collider of the payload | present | `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Chambers.cs:131`; `elements/Cannon/CannonElementDeclaration.cs:31` | `ChamberOccupancyTests.WidthAndRelativeArrivalAdmissionIncludeTheirDeclaredBoundaries`; `ChamberOccupancyTests.CurrentMaterialDeterminesOccupancyAndSeatingThroughTransport` (every collider) |
| 8 | Visible solid equals collider, including a torus | present | `CuriousContraptions.tests/VisibleSolidColliderTests.cs:63` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (its `ball` row) |
| 9 | Zero-parameter instance; forged parameter refused in edit and save | present | `elements/Baseball/BaseballElementDeclaration.cs:12` | `BaseballTests.FixedPresetCompilesCanonicalMassAndMaterialAndRejectsForeignParameters` |
| 10 | Run, Pause, Step, Reset, Save, Load of a payload | present | `engine/gpu/WorkshopSaveCodec.cs:48` | `BaseballTests.RunPauseStepResetSaveLoadRestoresAuthoredBaseball` |
| 11 | Coverage admits the package through its spec's binding section | present | `tools/Coverage/NamedPackageSources.cs:13`; `tools/Coverage/CurrentCatalogue.cs:42` | `CurrentCatalogueTests.NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias` |
| 12 | A receiver whose body has a second collider keeps its samples unless that collider crosses the sample ray | present | `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`–`:162` | `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow(Ball, true)` |
| 13 | Receiver capture of any dynamic payload by its residence sensor, after a declared dwell | present | `elements/Receiver/ReceiverElementDeclaration.cs:12` | `WorkshopPhysicsEngineTests.DeclaredReceiverParticipationAndDwellDriveRealCapture`; `WorkshopPhysicsEngineTests.DeclaredCaptureMarginWidensTheMouthWhileABesideDecoyStaysOutside` (the beside decoy) |

### Review tier

**Engine tier (all six layers on the first pass).** Every capability is present with a symbol and an exercising test, and every product path is on the data-only allow-list (the package's declaration and scene, tests of that package, one table row in `VisibleSolidColliderTests.Specs`, records under `docs/planning/` and `_bmad-output/`), but the candidate commit carries `TODO.md`, which is outside it (`_bmad/custom/bmad-loop-protocol.md`, Review tiers; the published slice ran at engine tier for the same reason, `spec-named-el-239.md:50` at `fa86969f`). No helper, harness, tool or engine file changes. The declaration on main is expression-bodied constants with no branch or method, and stays so. No member is added to `AssignedCompoundFeatureTests.Feature`; none is needed.

### Direct consumers (owned scope list)

Package:
- `elements/Baseball/BaseballElementDeclaration.cs:11` -- description; `:13` -- pick radius 0.36; `:22`–`:23` -- mass 0.15, principal moments 0.004056.
- `elements/Baseball/baseball.tscn` -- sphere and `col_sphere` radius 0.26 (height 0.52); delete `Visual/SeamLeft`, `Visual/SeamRight` and the `Stitch` mesh; add the band mesh, `Visual/Band` and `col_visual_band`; `load_steps` stays 6.

Tests:
- `CuriousContraptions.tests/BaseballTests.cs` -- the edits in the table below.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs`, after the `ball` row (`:109`) -- add `[new PartId("baseball")] = new(["Visual/Sphere"], ["Visual/Band"], [], [], []),`.

Tests read and unchanged: `FanGuidanceTests.cs:9`–`:26` (the Baseball rows assert only that the fan moves it); `WorkshopPhysicsEngineTests.cs:79`–`:97` (the Receiver construction R9 reuses; not edited); `PackageRuntimeParityTests.cs:149`, `:653`, `BasketballResourceTests.cs:34`, `WorkshopIntegrationHarnessTests.cs:694`, `CSharpStaticGateTests.cs:701` and `CuriousContraptions.slnx:20` (membership only); no test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`); `tools/Coverage.Tests/CurrentCatalogueTests.cs:225`–`:355` and `CurrentCapabilityInputsTests.cs:247`–`:269` (they name the spec, scene and declaration paths, which stay, and use their own fixture text).

Documents:
- `docs/planning/elements/EL-239-baseball.md` -- rewrite the body (task 5); the binding section stays as it is on main.
- `docs/planning/invest/scope-corrections.md:25` -- rewrite the S636 result paragraph for the repaired ball.
- `docs/planning/invest/named-elements.md:2107` -- the **Implementation** sentence names the repaired values and role.
- `docs/planning/elements/README.md:406` -- status "implemented; browser qualification pending".
- `docs/planning/invest/delivery-batches.md`, section 5, **Repairs** table, Baseball repair row -- delivered and repaired.
- `docs/planning/general-engine-element-map.md:349` -- "distinct preset: the lightest ball".
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- add key `repair-baseball-el-239`; key `named-el-239` stays `done`.
- `TODO.md`, `_bmad-output/implementation-artifacts/deferred-work.md` (only under the third-pass re-review exception), this contract and `run-logs/spec-repair-baseball-el-239.md`.

Not edited: `docs/planning/requirements.md`; `content/puzzles.json` (no level or fixture places `baseball`: a search of `origin/main` finds the id only in the package, tests, tools' tests and planning documents); `_bmad-output/implementation-artifacts/spec-named-el-239.md` and its evidence (historical record of the published slice); `tools/`; `engine/`.

**Coverage binding.** Unchanged in form. The task source's hash covers the spec text and the S636 decision block (`tools/Coverage/NamedPackageSources.cs:77`–`:79`), so both edits change that hash. `docs/coverage/source-inventory.json:591` stores a hash for `sequence-task-530`, but the published Baseball slice changed the enriched hash without touching `docs/coverage/` and its full round passed, so no committed file expects the enriched value; the task has no proof owner, so no stored proof goes stale.

### What is removed, and what still protects

| Removed | What it protected | Now |
|---|---|---|
| `Visual/SeamLeft`, `Visual/SeamRight`, mesh `Stitch` | Nothing: they were proud art without a collider | The band is drawn and collides; R2 and the new `Specs` row check it |
| `BaseballTests.MatchedSizeImpactChangesPayloadRouteByMass` | The withdrawn role: a 0.65 kg striker sends a target further than a 0.35 kg control | Deleted with the behaviour. Momentum transfer by mass stays proven generically by `TennisBallTests.ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload`; the new role is R4 and R5 |
| Assertions of 0.65 kg, 0.25 m, 0.01625, the seam nodes and one collider | The old declared values | Re-authored to the new values in the same tests |

### Decisions

Owner, 21 and 22 November 2026: realistic proportions (0.15 kg, radius 0.26 m); one proud band with its own collider; role "the lightest ball"; a row in the visible-solid test; the published package is repaired.

Made here under the standing delegation:
1. **Bounce 0.35 stays.** It is the published value and the planned one; the S636 comparison is on bounce and is independent of mass.
2. **Rolling resistance 0.03 stays.** The earlier plan proposed 0.04 without a source; the published value is delivered and nothing in the owner's decisions changes it.
3. **Colours stay as published**: cream sphere, and the band takes the published seam material (coral). The earlier plan's tan sphere is dropped; no decision asked for it.
4. **Band geometry and pose copy the Basketball's stripe**: inner radius r − 0.018, outer r + 0.01, quaternion (0, 0, −0.3007058, 0.95371695). In that pose the band is 55 degrees from the resting point, so a ball placed or dropped without spin lands and rests on its sphere; a rolling ball meets a 1 cm step up to twice a turn, as the Basketball does today.
5. **Pick radius 0.36**, keeping the published 0.10 m margin over the sphere.
6. **Icon unchanged**: the stitched-ball icon still reads as a baseball.
7. **The element spec is rewritten to the six sections** the "How to read a spec" section of `docs/planning/elements/README.md` requires (parts 1–6; Integration and Delivery omitted, as that section allows); the published spec has five paragraphs.
8. **Saves need no migration**: a saved Baseball stores only its part id and pose (`BaseballTests.RunPauseStepResetSaveLoadRestoresAuthoredBaseball` asserts empty parameters), so a published save loads as the repaired ball.
9. **"The lightest ball" holds at default settings** (Tennis ball 0.35 kg, Balloon 0.5 kg, Basketball 1 kg, Bowling ball 4 kg). A player can set a Tennis ball's mass below 0.15 kg (its range starts at 1/1024 kg, `elements/TennisBall/TennisBallElementDeclaration.cs:16`); the role is about the presets as placed. The Fan response also depends on the ball's orientation, because the band can shadow a sample once the ball has turned, as the Basketball's stripe already can.

10. **R9 Receiver capture is asserted here.** The new-identities readiness summary (`docs/planning/elements/readiness/new-identities.md`, "Baseball repair (EL-239)", BB4) finds no capture row in main's `BaseballTests` or this contract, and the EL-239 spec says the Baseball remains a capture payload. The construction is `WorkshopPhysicsEngineTests.DeclaredReceiverParticipationAndDwellDriveRealCapture`'s: the Receiver's interior is 1.38 m wide and its floor top 0.375 m below its origin (`elements/Receiver/basket.tscn`), so a 0.26 m ball 0.1 m above the origin starts clear of every wall.

Needs the owner: none.

### Commit path and concurrency

- Every owned source, test and planning path is already tracked on main. The only new files are this contract and its run log; neither is compiled. No test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`); the solution and the trim list need no edit either, and the pre-commit index snapshot sees the whole change once it is staged.
- Expected total: the current full local round at the baseline (main's at drafting, `TODO.md`: 7,294 passed), re-measured at start, plus 11: −1 (deleted fact), +1 (R4), +6 (R5), +2 (R6), +1 (`Specs` row), +2 (R9). The registration check: the baseline's total + 0 (its filter selects neither `BaseballTests` nor `VisibleSolidColliderTests`).
- **Serialisation (re-planned under owner decisions 24g and 24i).**
  - Prerequisite (coordinator decision 37, 23 November 2026): the planning work now uncommitted in `spec-drafts` (the `docs/planning/elements/readiness/` folder with the owner decisions of 22–23 November 2026, `delivery-batches.md` sections 0 and 5 with the Repairs table, `docs/planning/elements/README.md`, `named-elements.md`, `sprint-status.yaml`, `requirements.md`) is published before this slice starts, and the baseline is the main that contains it. At start the planner re-reads every planning `path:line` in this contract on that baseline (the README Baseball row moves from `:406` to about `:411`, with new status text that task 6 replaces).
  - The repair runs before the programme with visible-solid parts A, B, B2 and C, one publisher at a time; the later publisher merges main once and repeats the full local round on the merged candidate before approval.
  - Part B2 (`spec-elements-visible-solid-remainder-b2.md`, a flush-print repair under owner decision 24i) shares only `VisibleSolidColliderTests.Specs` (the `tennis` row beside this one), `TODO.md` and `delivery-batches.md`: one publisher at a time, in either order, with no content dependency. B2 keeps the Tennis ball's one sphere collider, so the R3 to R5 comparisons with the default Tennis ball are unaffected, and B2 edits no `BaseballTests` code.
  - Parts A, B and C share `VisibleSolidColliderTests.cs` (other rows), `TODO.md`, the sprint status and `deferred-work.md`. Part A changes the Fan of R4 and part C the Cannon of R6 and the visible-solid matcher of R2. Whichever publishes later re-reads those rows' margins on the merged main; the thresholds are not refitted.
  - S3, S5 with S5b, and S6 with S6b–S6g (owner decisions 22 and 24h) share `TODO.md`, `deferred-work.md` and `delivery-batches.md`; one publisher at a time. If S3 publishes first, the membership lines cited under "Tests read and unchanged" no longer exist; nothing here depends on them.
  - EL-223 is paused (owner decision 24g). Its uncommitted draft (`tim-cluster2-after-baseball`, from `fa86969f`) touches `docs/planning/elements/EL-239-baseball.md`'s binding line, `docs/planning/elements/README.md`, `named-elements.md` and the coverage tests. When it resumes after the programme, it replans from the published main that contains this repair. Its edit of the explicit Compile list that S2 replaced is its own to drop (completion ledger DC-85).
  - The Soccer ball and Cannonball family slice waits for the programme and S7. It will later share `VisibleSolidColliderTests.cs`, the sprint status and `TODO.md`; no acceptance row of either depends on the other.

## Tasks & Acceptance

**Execution:**
1. `CuriousContraptions.tests/BaseballTests.cs`, `CuriousContraptions.tests/VisibleSolidColliderTests.cs` -- re-author and add the tests below first; R1, R2, R4, R5 and the `Specs` row fail on the baseline -- red before green.
2. `elements/Baseball/BaseballElementDeclaration.cs` -- description "The lightest ball. A fan carries it easily and it lands with a low bounce."; pick radius 0.36; mass 0.15; principal moments 0.004056 -- declared values.
3. `elements/Baseball/baseball.tscn` -- `Sphere` mesh radius 0.26 and height 0.52; `Contact` shape radius 0.26; replace sub-resource `Stitch` with `Band` (`TorusMesh`, material `Seam`, `inner_radius` 0.242, `outer_radius` 0.27); delete nodes `SeamLeft` and `SeamRight`; add `Visual/Band` (`MeshInstance3D`, mesh `Band`, quaternion (0, 0, −0.3007058, 0.95371695)) and root child `col_visual_band` (`MeshInstance3D`, `visible = false`, the same mesh and quaternion). `col_visual_band` follows `col_sphere`, so collider 0 stays the sphere, which the airflow receiver names; `load_steps` stays 6 -- art and collider.
4. `CuriousContraptions.tests/BaseballTests.cs` -- per test:
   - `IdenticalDropDistinguishesEveryRetainedPreset` -- assertions unchanged; correct the comment (0.15 m is 29 per cent of the 0.52 m diameter). It is row R3.
   - `MatchedSizeImpactChangesPayloadRouteByMass` and its helper `Impact` -- delete.
   - `UnpoweredDropSettlesWithoutFreeRebound` -- resting height `WorkbenchSurface + .26f` in the release list and the rest assertion.
   - `SceneAndIconExposeSeamedSphereMatchingContactGeometry` -- rename `SceneAndIconExposeBandedSphereMatchingContactGeometry`: sphere radius 0.26 and height 0.52; `Visual/Band` is a `TorusMesh` with inner 0.242 and outer 0.27; the `Visual` node has exactly two mesh children, `Sphere` and `Band`; the bundle entry has exactly two colliders, a sphere of 0.26 and a torus whose centreline and tube equal the f32 expressions (0.242f + 0.27f) · 0.5f and (0.27f − 0.242f) · 0.5f, sharing one material.
   - `FixedPresetCompilesCanonicalMassAndMaterialAndRejectsForeignParameters` -- mass 0.15; inertia 0.4 · 0.15 · 0.26² within 0.01 per cent (4.056e-7); the body's sphere collider has radius 0.26 and its torus collider shares the sphere's material; the refusal cases unchanged.
   - `CommittedPresentationFollowsMovingBodyAndReset`, `RunPauseStepResetSaveLoadRestoresAuthoredBaseball` -- unchanged (no old value in them).
   - Add `TheFanMovesTheBaseballMoreThanTheTennisBallAndTheBasketball` (R4), `TwoBaseballsLeaveADefaultPlateOff` (R5, the six-case theory in the table), `BaseballNeverSeatsInTheCannon` (R6, the two-case theory in the table) and `AnAlignedBaseballIsCapturedAndAShiftedOneIsNot` (R9, the two-case theory in the table; it passes on the baseline too, and is coverage the published slice lacked).
5. `docs/planning/elements/EL-239-baseball.md` -- rewrite the body as the six sections of the "How to read a spec" section of `docs/planning/elements/README.md` (parts 1–6; Integration and Delivery omitted, as that section allows): Identity (with the sources `todo-198`, `sequence-task-530`, S636 and the owner decisions of 21 and 22 November 2026); Declaration (the R1 and R2 values, colours, icon, no parameters or ports); Engine capabilities (1–13 as "exists"); Legacy harvest ("none, the Baseball has no legacy implementation; Files harvested: none"); Acceptance outline (rows R1–R9); Open questions ("none"). Line 1 stays `# EL-239 · Baseball`; the file has exactly one `## Package source binding` heading and it is the last `## ` section, unchanged -- spec.
6. `docs/planning/invest/scope-corrections.md`, `named-elements.md`, `docs/planning/elements/README.md`, `delivery-batches.md`, `general-engine-element-map.md`, `sprint-status.yaml`, `TODO.md` -- the record edits listed above, in the candidate commit. The `element-239` ledger entry keeps exactly one `](../requirements.md#todo-N)` link and exactly one `Owner **S636**`; the S636 block gains no `<a id=` anchor and stays non-empty -- status.

**Acceptance Criteria:**
- AC1: Given the repaired package, when it is compiled and its scene is read, then rows R1 and R2 hold, and the package and its tests name no seam node and no Baseball mass of 0.65 kg, inertia of 0.01625 or Baseball radius of 0.25 m.
- AC2: Given the repaired Baseball beside the delivered balls, when the drop, Fan and plate constructions run, then rows R3, R4 and R5 hold: it is the lightest ball, moved most by the Fan, not pressing a default plate in pairs, with a bounce visibly different from every other ball.
- AC3: Given the repaired Baseball, when it is offered to the cannon and to a Receiver, dropped, run, reset, saved and loaded, then rows R6, R7, R8 and R9 hold.
- AC4: Given the candidate, when the registration check and the full local round run, then the Coverage binding still admits `baseball`, `VisibleSolidColliderTests` passes its new row, and there are zero failures and zero skipped tests.

## Design Notes

1. **Behaviour and scope.** One published part changes its declared values, art and role; nothing else changes.
2. **Reusable primitive.** None new; every capability is delivered and tested (probe).
3. **Declared composition.** Metadata, one scene body with one material and two colliders, a constant mass frame, one ambient load, one airflow receiver.
4. **Execution and ownership.** No state, writer or update loop changes; the shared solver owns the body.
5. **Generality.** The role rows compare the Baseball with delivered balls in the same construction; expected ratios come from the declared masses and the airflow law, not from the package under test.
6. **Forward-only.** Old values, seam nodes and the withdrawn role test are deleted; nothing reads them afterwards.
7. **Prerequisites.** No capability prerequisite. Order: before the engine programme (owner decision 22), not after EL-223 (paused, owner decision 24g); publication serialised with visible-solid parts A, B, B2 and C (Commit path and concurrency).

## Verification

Executor: the implementation owner. Environment and the five compiler-log builds are those in `docs/delivery-workflow.md#reproducible-local-verification`, with the pinned toolchain first on `PATH`: `PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH` and `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (on this host the default `PATH` has Homebrew node v26.13.1 and a dotnet without `wasm-tools`, so the capture's node check and the web build fail otherwise); record the session environment check in the run log. A change under `elements/Baseball/` or `CuriousContraptions.tests/` invalidates `solution.binlog` and `playtest.binlog`.

**Commands:**
- The five `dotnet build … -bl:` captures from the workflow -- expected: zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: both canary `PASS` lines, the `Coupling dependencies:` summary, zero violations.
- The registration-check `dotnet test … --filter` command from `docs/delivery-workflow.md#new-package-registration-check`, with `|FullyQualifiedName~CurrentCapabilityInputsTests` appended (the published filter omits it) -- expected: zero failed, `Skipped: 0`.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~BaseballTests|FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~FanGuidanceTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~CurrentCatalogueTests"` -- expected: zero failed, `Skipped: 0`.
- `git add -N` on the contract and the run log, then `git diff --check <baseline_revision>` -- expected: no output.
- `git diff --stat <baseline_revision> -- engine tools schemas CuriousContraptions.slnx CuriousContraptions.Abstractions* CuriousContraptions.Simulation CuriousContraptions.Bundle CuriousContraptions.Core CuriousContraptions.tests/CuriousContraptions.tests.csproj 'elements/*' ':!elements/Baseball'` -- expected: no output.
- `caffeinate -i` the full-round `dotnet test` command from the workflow -- expected: zero failed, `Skipped: 0`; total = the current full local round at the baseline + 11 (re-measured at start).
- The candidate commit runs the unmodified `.githooks/pre-commit` on the staged snapshot.

### Acceptance-to-test table

All tests are in `BaseballTests` unless a class is named. Tolerances reused from the published tests: rest within 1 mm and below 0.05 m/s over the last two seconds of a 1,200-tick window; 0.15 m peak separation. Declared constants are asserted exactly.

| Row | Test | Control | Boundary | Run |
|---|---|---|---|---|
| R1 | `FixedPresetCompilesCanonicalMassAndMaterialAndRejectsForeignParameters` | its foreign-parameter and unknown-element refusals | inertia within 0.01 per cent rejects a wrong mass or radius exponent | planned |
| R2 | `SceneAndIconExposeBandedSphereMatchingContactGeometry`; `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt(baseball)` | `Visual` has exactly the two mesh children | two colliders, one material | planned |
| R3 | `IdenticalDropDistinguishesEveryRetainedPreset` | Basketball, Bowling ball, Tennis ball; Balloon rises | nearest neighbour is the Bowling ball: about 0.29 m apart against 0.15 m | planned |
| R4 | `TheFanMovesTheBaseballMoreThanTheTennisBallAndTheBasketball`. Construction of `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` (`FanGuidanceTests.cs:27`–`:30`): `TennisBallTests.Scene(FanIntegrationTests.Fan(powered: true), cargo)` with each ball alone at (3, 5, 0), velocity X read after `h.Advance(12)` | `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow(Baseball, false)` (speed along the flow exactly zero) | 1.5 and 3 against about 2.0 and 5.4 expected (4.7, 2.3 and 0.87 m/s from 9 N over 0.15, 0.35 and 1 kg with linear slip to 12 m/s); in the authored pose the band clears the nearest sample ray by 2.8 mm against the 0.2 mm query tolerance, so all four samples count | planned |
| R5 | `TwoBaseballsLeaveADefaultPlateOff`, a theory of six cases (minimum, load, expected): (0.5, one Baseball, off), (0.5, two Baseballs, off), (0.5, two default Tennis balls, on), (0.25, two Baseballs, on), (0.25, one Baseball, off), (0.1, one Baseball, on). Construction and observation as `TennisInteractionTests.TwoDefaultTennisBallsLoadThePlateWhileOneStaysBelowThreshold` (main `TennisInteractionTests.cs:32`–`:47`): plate origin at bench + 0.22 m (its top face at origin + 0.08 m: `pressure_plate.tscn` `col_0_1` at y 0.03, height 0.1), balls released with their centres 0.34 m above the plate origin, that is resting on the top face, `Latched` looked for over 2,400 ticks (the 20 s resting guard that test already uses). Each "off" case also asserts at the end that every ball's centre is within 1 mm of bench + 0.56 m (top face + 0.26 m) and below 0.05 m/s, so the ball is resting on the plate | two Tennis balls switch it; one Baseball switches a 0.1 kg plate (the centre placement registers) | minimum 0.25 kg: two Baseballs (0.30 kg) switch it, one (0.15 kg) does not | planned |
| R6 | `BaseballNeverSeatsInTheCannon`: a theory over the payload in the construction of `CannonIntegrationTests.Vertical` (main `CannonIntegrationTests.cs:209`–`:222`, built in the test with the payload's part id; that helper is not edited). Basketball: `ChamberRegions[0].Phase` reaches `Seated` within 600 ticks. Baseball: the phase is `Loading` at tick 600 and was never `Seated`, with `Occupancy` 1 (it is in the chamber) | the Basketball seats in the same construction | 0.54 m across the band against 0.6 m | planned |
| R7 | `UnpoweredDropSettlesWithoutFreeRebound` | the resting release does not move | 1 mm and 0.05 m/s over the last two seconds | planned |
| R8 | `RunPauseStepResetSaveLoadRestoresAuthoredBaseball`; `CommittedPresentationFollowsMovingBodyAndReset` | non-finite pose save refused (in R1's test) | second Run after Load falls as the first | planned |
| R9 | `AnAlignedBaseballIsCapturedAndAShiftedOneIsNot`, a theory of two cases in the construction of `WorkshopPhysicsEngineTests.DeclaredReceiverParticipationAndDwellDriveRealCapture` (`WorkshopPhysicsEngineTests.cs:79`–`:97`: default Receiver at (0, 1), payload at (0, 1.1); built in the test with the part id `baseball`). The construction is `new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(baseball, receiver))`; the admission is made inline from public production calls, `PhysicsGpuAbi.Admission(WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2)), new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)))`, and stepped with `WorkshopPhysicsEngineTests.StepWorld` (internal, `:519`). `Free` and `AdmitMoving` (private, `:226`, `:469`) are not made accessible and not edited: aligned, the residence sensor's occurrence count reaches 1 within 240 ticks and never exceeds 1; shifted to x = 3 m, as that file's beside decoy (`:164`), it stays 0 | the shifted Baseball | first capture no earlier than tick 42 (0.35 s × 120) | planned |
| AC4 | `CurrentCatalogueTests.NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias` (reads the actual repository's Baseball binding; `:262` at `25df3cd5`); `CurrentCapabilityInputsTests.NamedPackageClosureIsRequiredWithoutGrantingCompletion` (`:233`) | their refusal rows | the binding section valid in main's `**Decision:**` form | planned |

**Manual checks (if no CLI):**
- Browser qualification of the Baseball stays pending until the post-catalogue E2E phase.

## Spec Change Log

## Review Triage Log

## Auto Run Result
