---
title: 'Family slice: ball presets (EL-077 Soccer ball, EL-091 Cannonball)'
type: 'feature'
created: '2026-11-21'
status: 'blocked'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/docs/planning/invest/delivery-batches.md'
warnings: ['blocked-on-prerequisite', 'multiple-identities']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Two named ball identities have no package: EL-077 Soccer ball and EL-091 Cannonball. Each is a fixed preset of the delivered ball shape, so a player cannot yet choose a light lively ball that fits the cannon or a heavy ball that fits the cannon. The Baseball (EL-239) was published separately and is not part of this slice.

**Approach:** Deliver the two as one Family slice: two zero-parameter element packages that reuse the delivered ball declaration shape (declaration data, art and tests only), each with its own acceptance rows, tests, ledger row and coverage binding. No engine, abstraction, wire or shared-presentation code changes.

## Boundaries & Constraints

**Always:**
- Each identity is its own package, catalogue entry and palette entry with the fixed values in the matrix; f32 game values; generic interfaces only.
- Each identity keeps its own acceptance rows, named tests, ledger row, sprint key and `## Package source binding` section. One candidate commit and one approval cover both.
- Each ball carries one raised band with its own collider, as the Basketball has (owner, 21 November 2026).
- Tests run the real production C# simulation through defined interfaces, with the tolerances fixed in this contract.
- A declaration file contains only expression-bodied members that return constant data.

**Never:**
- No parameter on either preset (configurable balls are EL-076 Programmable ball and CAT-064 Tennis ball).
- No engine, abstraction, bundle, wire, tool or TypeScript change; no new shared test harness or helper.
- No change to the Baseball package, its tests or its spec.
- No fallback, alias, second id or migration; no hash, byte-identity or fixed-arrival-tick assertion; no skipped test; no mutants; no browser run.
- No level authoring in `content/puzzles.json`; campaign first use stays with the level slices.
- EL-098 Pool ball and EL-076 Programmable ball do not join this slice.

## I/O & Edge-Case Matrix

Fall fraction `f` = (peak centre height after the first bounce − resting centre height) ÷ (release centre height − resting centre height).

Drop rows release every ball with its centre 3 m above the bench origin, as `TennisBallTests.cs:86` does. CB3 releases each ball with its centre 2 m above the Bumper's top surface. Each ball carries one raised band, 0.01 m proud of its sphere, with its own collider; SB5 and CB5 are run with the band in place. Plate rows place one ball at the plate centre, and two balls 0.5 m either side of it, clear of the plate's centre ring (centreline 0.3 m).

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| SB1 | Soccer ball `soccer` placed from the palette | One dynamic body, a sphere of radius 0.32 m with one raised band: mass 0.45 kg, restitution 0.65, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s, ambient drag 0.04 1/s, solid-sphere inertia, no parameters | Any authored parameter on the instance or in a save is refused with the typed reason; nothing is installed |
| SB2 | Soccer ball, Basketball and Bowling ball released from one height | Soccer `f` is 0.36–0.45 and exceeds the Basketball's by at least 0.06; all three come to rest on the bench | No error expected |
| SB3 | One Soccer ball on a default 0.5 kg Pressure plate; then two; then one Bowling ball | One leaves the plate off; two, or one Bowling ball, switch it | Plate minimum set to 0.45 kg: one Soccer ball switches it (inclusive) |
| SB4 | Soccer ball after Run, Reset, Save and Load; Receiver below it | Authored pose and kind restored; an aligned ball is captured, a shifted one is not | A save naming `soccer` with a parameter is refused atomically |
| SB5 | Soccer ball resting on a default-size Ramp rotated 10 degrees about Z, its centre 1.5 m up the slope from the lower end; the same ball placed on the level bench | It rolls down, passes the ramp's lower end and travels at least one ball diameter (0.64 m) beyond it; 480 ticks after passing the lower end it is slower than when it passed it. | On the level bench it moves less than 1 cm in 600 ticks |
| CB1 | Cannonball `cannonball` placed from the palette | One dynamic body, a sphere of radius 0.32 m with one raised band: mass 6 kg, restitution 0.08, friction 0.3, rolling resistance 0.02, bounce threshold 0.1 m/s, ambient drag 0.04 1/s, solid-sphere inertia, no parameters; dropped, its `f` is at most 0.05 and it rests | As SB1 |
| CB2 | Cannonball loaded into a charged default Toy cannon; a Basketball in an identical cannon | Both seat and are launched; Cannonball release speed is 0.30–0.55 of the Basketball's; its kinetic energy does not exceed the work released plus 0.5 J | Uncharged cannon: request refused `Uncharged`, the Cannonball stays seated and still. Tennis ball (0.5 m wide) and Bowling ball (0.56 m) never seat |
| CB3 | Cannonball and Basketball each dropped onto a default Bumper | The Bumper pays both; the Cannonball leaves at 2 m/s or more and at most 0.6 of the Basketball's speed | Bumper with strength 0: the Cannonball leaves at 1 m/s or less |
| CB4 | Cannonball after Run, Reset, Save and Load | Authored pose and kind restored; second Run launches again | As SB4 |
| CB5 | Cannonball in the SB5 construction | As SB5 | As SB5 |
| F1 | The two packages present | Palette shows each at its allocated order (Soccer ball 102, Cannonball 134; `docs/planning/elements/readiness/palette-allocation.md`, coordinator decision 36); registry, save and worker admission accept both ids; each drawn sphere and band has its collider and no collider reaches beyond the art; the Coverage tool admits both through their binding sections | A duplicate id or palette order fails the bundle build |

</intent-contract>

## Code Map

**Status: blocked.** Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): `baseline_revision` is the published main that contains the planning work, and every scope guard and `git diff` here compares against it; `25df3cd5` is the drafting revision only, not a baseline. Waits for the engine programme (owner decisions 3a and 22: new elements wait), then S7 (`spec-s7-coverage-named-identity-admission.md`, the coverage follow-on, itself after the programme); delivered at its campaign first-use position (owner decision 24a: the Soccer ball first appears at level 2b, the Cannonball at level 45, `docs/planning/elements/readiness/campaign-coverage.md` §6). Owner: coordinator. Retry condition: S7 published. Whether the two stay one Family slice although their first uses are far apart, or split so each lands at its own position, is decided at replanning (open, completion ledger DC-22). Shape: **Family slice** (two presets of one part). Replanned 22 November 2026 against `origin/main` `fa86969f`, read with `git show`; every `path:line` below is a line in that revision, and the cited code files are unchanged at `25df3cd5`. `baseline_revision` is the drafting revision `25df3cd5`; the coordinator re-captures it from the revision that publishes S7, before first implementation, and the intent contract is not frozen before then.

**Planning verdict:** returned to planning on 22 November 2026 (Baseball published separately; coverage binding standard changed by the owner). The third audit's Scoped Pass (planning auditor `a97bcc8c85da4c3f9`, 21 November 2026) covered the three-member contract and does not carry over; awaiting re-audit.

### Coverage binding on main, and why this slice is blocked

Main admits a non-CAT package through a `## Package source binding` section in its element spec (`tools/Coverage/NamedPackageSources.cs:13`; owner decision 22 November 2026: this is the standard, and the ledger `**Package:**` line design is dropped). For `NamedPackageSources` to admit a ball, its spec must contain, after that heading and one blank line, exactly these five lines (regex at `:55`–`:61`):

```
**Package:** soccer.
**Named identity:** [EL-077](../invest/named-elements.md#element-077).
**Acceptance:** [todo-N](../requirements.md#todo-N), [sequence-task-M](../requirements.md#sequence-task-M).
**Decision:** [S647](../invest/scope-corrections.md#s647).
**Mode records:** Configuration=Fixed.
```

and for the Cannonball `cannonball`, `EL-091`, `element-091` and `S661`. The file name must start `EL-077-` and the title `# EL-077 · ` (`:64`–`:66`); both specs meet that today. The reader then checks the ledger entry and the linked sources. Against main today:

| Check (`NamedPackageSources.cs`) | Soccer ball | Cannonball |
|---|---|---|
| Ledger entry has the identity heading, exactly one `requirements.md#todo-N` link, and it is the bound todo (`:93`–`:99`) | met: `todo-217` (`named-elements.md:719`) | met: `todo-197` (`:845`) |
| Ledger entry has exactly one `Owner **SNNN**`, equal to the bound decision (`:101`–`:108`) | met: S647 (`:721`) | met: S661 (`:847`) |
| The decision anchor exists once in `scope-corrections.md` with a body (`:121`–`:143`) | **not met**: no `s647` block; the file's S6nn anchors are `s636`, `s638`, `s639`, `s640`, `s641` | **not met**: no `s661` block |
| The todo anchor and a sequence-task anchor are adjacent in one requirements row (`:110`–`:119`) | **not met**: `todo-217` is a "Scope index" row (`requirements.md:4085`); the Soccer ball's task `sequence-task-630` is anchored at its `element-077` row | **not met**: `todo-197` is a "Scope index" row (`:4100`); its task `sequence-task-644` is at `element-091` |
| One task source per binding (`:47`–`:48`) | met once the task is `sequence-task-630` | met once the task is `sequence-task-644` |

Neither identity meets the preconditions, so neither package can be admitted on main as it stands, and `CurrentCatalogue.ValidateResources` would refuse both with "has no current catalogue declaration" (`tools/Coverage/CurrentCatalogue.cs:252`). The prerequisite is the coverage follow-on, which must admit an identity whose owner has no decision block and whose task is anchored at its `element-NNN` row. Building that here is not allowed without a planning amendment. Task 8 gives the binding text to be confirmed against the published follow-on.

### Capability probe

Performed on `8d1d68aa` and rechecked against `fa86969f`. Counts: 16 present, 0 present-untested, 1 narrower, 0 missing.

| # | Capability | Mark | Symbol | Exercising test |
|---|---|---|---|---|
| 1 | Package discovered by glob into the bundle | present | `tools/BundleElements/BundleElements.csproj:26` | `SolutionMembershipTests.EveryPackageIsDiscoveredByGlobWithNoHandList` |
| 2 | Sphere collider read from the package scene | present | `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `WorkshopBowlingTests.BothBallsCompileAsDynamicSpheresWithTheirDeclaredMaterialsAndSolidInertia` |
| 3 | Constant mass frame (mass, inertia, gravity) | present | `CuriousContraptions.Abstractions/MassFrameValues.cs:41` | same test |
| 4 | Collider material: restitution, friction, rolling resistance, bounce threshold | present | `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21` | `PackageRuntimeParityTests.EveryPayloadPackageCompilesItsDeclaredMaterial` |
| 5 | Ambient linear drag load | present | `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:124` | `WorkshopBowlingTests.DeclaredDragAndRollingResistanceCompileIntoTheBodyAndMaterialRecordsAndOnlyBallsCarryACoefficient` |
| 6 | Airflow receiver on a ball | present | `MechanicalContracts.cs:45` | `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` |
| 7 | Payload capture policy | present | `engine/gpu/DeclaredPhysics.cs:144` | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` |
| 8 | Zero-parameter instance; forged parameter refused in edit and save | present | `elements/BowlingBall/BowlingBallElementDeclaration.cs:26` | `PayloadInstanceTests.ABareGenericPayloadRecordLoadsAndAnyParameterIsRefused` |
| 9 | Run, Reset, Save, Load of a payload | present | `engine/gpu/WorkshopSaveCodec.cs:19`, `:48` | `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload` |
| 10 | Chamber seating by declared width window (cannon 0.6–0.84 m) | present | `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Chambers.cs:131`; `elements/Cannon/CannonElementDeclaration.cs:30` | `ChamberOccupancyTests.WidthAndRelativeArrivalAdmissionIncludeTheirDeclaredBoundaries` |
| 11 | Finite-work launch of a seated payload | present | `CuriousContraptions.Abstractions/FiniteWorkActuatorValues.cs:26` | `CannonIntegrationTests.PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply` |
| 12 | Store-limited contact work (Bumper) | present | `elements/Bumper/BumperElementDeclaration.cs:39` | `WorkshopBumperTests.FiniteBudgetCapsTheNormalBoostAndNeverPaysPastTarget` |
| 13 | Contact-load threshold (Pressure plate) | present | `elements/PressurePlate/PressurePlateElementDeclaration.cs:92` | `PressurePlateTests.TheInstanceMinimumMassDecidesItsOwnGate` |
| 14 | Visible solid equals collider | present | `VisibleSolidColliderTests.cs:63` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` |
| 15 | Registry, palette, save and worker admission derived from declarations | present | `tools/BundleElements/PackageDiscovery.cs:16` | `PartRegistryTests.AValidNewPackageIdentityNeedsNoEngineAdmissionChange` |
| 16 | Coverage admits a named-identity package through its spec's binding section | **narrower**: only for an identity with a decision block and a todo co-anchored with its task | `tools/Coverage/NamedPackageSources.cs:55`–`:61`, `:110`–`:143` | `CurrentCatalogueTests.NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias` (the admitted case); `NamedPackageRejectsMissingDuplicateAndUnknownSourceBindings` (the refusals these two identities would hit) |
| 17 | A proud band drawn as a torus with its own torus collider on a dynamic ball | present | `elements/Ball/ball.tscn` (`Visual/Visual_Stripe`, `col_visual_stripe`); `tools/BundleElements/SceneGeometryExtractor.cs:24` | `BasketballResourceTests.TheBundleAndSceneUseDeclaredNumericMaterials` (`:48`–`:54`); `CompoundMassAndRollingTests.EveryAssignedVisibleFeatureStopsAProbeAndLeavesAdjacentSpaceEmpty` (`:403`, `BasketballSeam`) |

**Capability 16 is the blocker, and the binding reason is the Family slice definition:** a Family slice needs every capability present or present-untested, so one narrower capability disqualifies the shape until it is delivered.

Cannon admission (the disqualifier in `delivery-batches.md` section 1a) passes: Cannonball and Soccer ball are 0.66 m wide across their bands (0.64 m across the sphere), inside the declared 0.6–0.84 m window.

### Review tier

**Engine tier (all six layers).** Reasons: capability 16 is narrower today; and `PackageRuntimeParityTests.Placed` (`:641`), which this slice must extend at `:653`, is a helper shared by other tests, so the allow-list's "no changed shared helper" test is in doubt. The declaration files themselves meet the allow-list: they are held to expression-bodied members returning constants, as the Bowling ball and published Baseball declarations are. The tier is determined again at replanning from the revision that publishes the prerequisite.

### Direct consumers (owned scope list)

New files, per package `<Dir>/<id>`: `SoccerBall/soccer`, `Cannonball/cannonball`:
- `elements/<Dir>/<Dir>.csproj` -- copy of `elements/BowlingBall/BowlingBall.csproj`.
- `elements/<Dir>/<Dir>ElementDeclaration.cs` and its `.cs.uid` -- declaration; the `.uid` holds one new `uid://` value that `rg` shows is unused in the repository.
- `elements/<Dir>/<id>.tscn` -- the scene of task 1: one sphere mesh with its `SphereShape3D` named `col_0_0`, and one band mesh with its collider `col_visual_band`; nothing else.
- `elements/<Dir>/icon.svg`, `icon.svg.import` -- the icon of task 1; the import file is the three-line `importer="keep"` form of `elements/BowlingBall/icon.svg.import`.
- `CuriousContraptions.tests/SoccerBallTests.cs`, `CannonballTests.cs` -- the tests in the table below.

Lists a new package still edits by hand on main (each is one added line or list member; the published Baseball slice made the same edits):
- `CuriousContraptions.slnx` -- add two `<Project Path>` lines in the elements block (Baseball is at `:20`, Windmill at `:55`).
- No test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`).
- No `CSharpStaticGateTests` list edit: S3, which runs before the programme, derives `TrimAnalyzedProjects` and the other lists it owns; any list S3 leaves hand-maintained (the solution file, S3 decision D1) is re-checked at replanning.
- `CuriousContraptions.tests/PackageRuntimeParityTests.cs:149` -- expected payload ids become `ball, balloon, baseball, bowling, cannonball, soccer, tennis`.
- `CuriousContraptions.tests/PackageRuntimeParityTests.cs:656` -- the two ids join the existing generic-placement `or` list.
- `CuriousContraptions.tests/PackageRuntimeParityTests.cs:599` needs no edit: its parameter table falls through to `_ => []` for a package with no parameters.
- `CuriousContraptions.tests/BasketballResourceTests.cs:34` -- the two ids join the expected registry keys.
- `CuriousContraptions.tests/WorkshopIntegrationHarnessTests.cs:694` -- `"soccer", "cannonball"` follow `"baseball"` in the Motion rows.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs:110` -- one `Specs` row per package.

Derived, no edit: registry, palette order, save and worker admission, ports and footprint, capacity, generic lifecycle, E2eRunner impact mapping, the bundle and `tools/BundleElements` references.

Documents: `docs/planning/invest/named-elements.md:719`–`:723`, `:845`–`:849`; `docs/planning/elements/EL-077-soccer-ball.md`, `EL-091-cannonball.md` (delivered values and the binding section); `docs/planning/invest/delivery-batches.md` (its ball-presets row, section 1a); `_bmad-output/implementation-artifacts/sprint-status.yaml` keys `named-el-077` and `named-el-091`; `TODO.md`; `_bmad-output/implementation-artifacts/deferred-work.md`; this contract and `run-logs/spec-family-ball-presets.md`.

Not edited: `docs/planning/requirements.md` (each requirement block is a hashed coverage source, `tools/Coverage/RequirementDiscovery.cs:72`), `docs/planning/invest/scope-corrections.md` unless the published follow-on requires a decision block there, `content/puzzles.json`, `elements/Baseball/` and `BaseballTests.cs`, anything under `engine/`, `CuriousContraptions.*` production projects, `tools/`, `schemas/`.

**Deleted by this slice:** none — new declarations. A search of main for `soccer` and `cannonball` outside `docs/planning`, `docs/coverage` and `_bmad-output` returns nothing. No guard or list is removed, so no protection is lost.

**Per identity, what differs from its sibling:** directory, id, display text, colour, band colour, palette order, mass, restitution, rolling resistance, inertia, icon. Radius, pick radius and everything else are identical.

### Decisions

Owner, 21 November 2026: **"One proud band each"** — each ball has one raised band with its own torus collider, as the Basketball has. Owner, 22 November 2026: main's `## Package source binding` section is the coverage binding standard.

Made under the standing delegation:
1. **Fixed presets, no parameters.** The EL-077 and EL-091 specs say "None authored; the material is fixed by kind"; D13 fixes size per kind. The Bowling ball and the published Baseball are the delivered fixed form.
2. **Values are the spec proposals** (D1): Soccer 0.32 m, 0.45 kg, 0.65, 0.03; Cannonball 0.32 m, 6 kg, 0.08, 0.02.
3. **Both respond to airflow** with the receiver every delivered ball declares (EL-077 open question 2).
4. **The Cannonball gets no Pressure plate row** (EL-091 open question 2, D19): it always presses, which teaches nothing.
5. **Solid-sphere inertia (0.4 m r²) for both**, as the Basketball declares.
6. **Cannonball against Bowling ball.** Their bounce difference (0.08 against 0.14) is about 3 cm on a 2.5 m fall and is not player-visible, so the Cannonball's distinctness is proven by launch speed and by seating in the cannon (CB2, CB3). The EL-091 spec's control "the Bowling ball rebounds higher" is replaced for that reason.
7. **Band.** It copies the Basketball's proportions and pose: `TorusMesh` with `inner_radius` r − 0.018 and `outer_radius` r + 0.01, rotated 35 degrees about Z (`elements/Ball/ball.tscn`, quaternion (0, 0, −0.3007058, 0.95371695)). Sphere colours: Soccer cream `#fff8e9`, Cannonball slate `#556573`. Band colours: navy `#293954` on the Soccer ball, gold `#f7cb52` on the Cannonball.
   - *Rest and bounce.* In the authored pose the band is 55 degrees from the resting point, so a ball placed or dropped without spin lands and rests on its sphere; the fall-fraction, rest and plate rows are unaffected by it.
   - *Rolling.* A rolling ball meets its band as a 1 cm step up to twice a turn, as the Basketball does in delivered rolling tests (`HollowElementTests.ABallRollsThroughAStraightPipeOfEveryLengthOnItsWallWithoutCatching`; `CompoundMassAndRollingTests.AnUnloadedLargerChildCannotEnlargeTheRollingTorqueBudget`). This is accepted for gameplay. SB5 and CB5 assert distance and slowing, not a smooth path or a rest height; on the 10 degree ramp the ball drops about 5 cm before first meeting its band.
8. **Palette orders 102 and 134**, as allocated by `docs/planning/elements/readiness/palette-allocation.md` (coordinator decision 36: grouped by category, numbered in campaign first-teaching order).
9. **Retained CAT-001 and CAT-014 acceptance** (gravity, rolling, bounce, momentum transfer, rest): gravity, bounce and rest are rows SB2 and CB1; rolling is rows SB5 and CB5; momentum transfer by mass is the delivered generic proof `TennisBallTests.ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload`.
10. **No row compares against the Baseball.** The Baseball's own distinctness (S636) is proven in its package's tests. Next to the repaired Baseball (0.15 kg, 0.26 m, bounce 0.35) the Soccer ball is three times as heavy, larger, livelier (fall fraction about 0.42 against 0.12) and fits the cannon, which the Baseball (0.54 m across its band) does not; next to the published 0.65 kg Baseball the bounce and cannon differences are the same. So this slice does not depend on the Baseball repair, in either order.

Needs the owner: none open.

### Carry-forward and concurrency

`deferred-work.md` on main records that the Baseball's task has no proof-owner role; the same will be true of these two tasks and is the follow-on's to settle. S2 is published and S3 runs before the programme, so this slice edits neither the test project file nor the lists S3 derives. A lesson fixture that places a named-package part is still refused by the Coverage fixture rule on main (`tools/Coverage/CurrentCatalogue.cs:116`–`:120`); this slice authors no level, and that rule blocks the first level that places either ball. This slice cannot run beside another element slice, the Baseball repair or the EL-223 session without serialised publication: it shares the solution file, `PackageRuntimeParityTests.cs`, `VisibleSolidColliderTests.cs`, the ledger, the sprint status and `TODO.md`.

## Tasks & Acceptance

**Execution:**
1. `elements/SoccerBall/`, `elements/Cannonball/` -- create the six files of each package, with these values -- the declaration data.

   | Field | soccer | cannonball |
   |---|---|---|
   | DisplayName | Soccer ball | Cannonball |
   | Description | A light, lively ball. Bounces higher and presses less than heavy balls. | A heavy iron ball. Launchers give it far less speed than a light ball. |
   | Colour (r, g, b of 255) | 255, 248, 233 | 85, 101, 115 |
   | PaletteOrder | 102 | 134 |
   | Pick radius | 0.44 | 0.44 |
   | Sphere radius (scene mesh and `col_0_0`) | 0.32 | 0.32 |
   | Band `inner_radius`, `outer_radius` | 0.302, 0.33 | 0.302, 0.33 |
   | Mass | 0.45 | 6 |
   | Principal moments (each axis) | 0.018432 | 0.24576 |
   | Material (restitution, friction, rolling, threshold) | 0.65, 0.3, 0.03, 0.1 | 0.08, 0.3, 0.02, 0.1 |

   Category Motion; `Parameters: []`; no ports, events or presentation bindings; footprint one body, one moving; `PayloadPolicy.CaptureTarget`; gravity (0, −9.81, 0); airflow receiver and ambient load exactly as `BowlingBallElementDeclaration.cs:32`–`:35` and `:61`–`:62`.

   **Scene, per package** (`<id>.tscn`, copied from `elements/Ball/ball.tscn`): root `Node3D` named `<id>`; `Visual` (`Node3D`); `Visual/Visual_Sphere` (`MeshInstance3D`, `SphereMesh` with `radius` r and `height` 2r, one `StandardMaterial3D` in the sphere colour); `Visual/Visual_Band` (`MeshInstance3D`, `TorusMesh` with the radii above, one `StandardMaterial3D` in the band colour, quaternion (0, 0, −0.3007058, 0.95371695)); `col_0_0` (`CollisionShape3D`, `SphereShape3D` radius r, `visible = false`); `col_visual_band` (`MeshInstance3D`, `visible = false`, the same torus mesh and quaternion). No other node. Nothing is drawn beyond r + 0.01.

   **`VisibleSolidColliderTests.Specs` rows:** `[new PartId("soccer")] = new(["Visual/Visual_Sphere"], ["Visual/Visual_Band"], [], [], []),` and the same row for `"cannonball"`.

   **Icons** (24 × 24, the stroke attributes of `elements/BowlingBall/icon.svg`): both start with `<circle cx='12' cy='12' r='9'/>`; Soccer adds the pentagon `<path d='M12 8.5l3.3 2.4-1.3 3.9h-4l-1.3-3.9z'/>`; Cannonball adds the fuse `<path d='M12 3V1'/>`.
2. `CuriousContraptions.slnx` -- add the two project lines (kept by hand under S3 decision D1) -- so the packages compile; the trim list and test project need no edit after S2 and S3.
3. `CuriousContraptions.tests/PackageRuntimeParityTests.cs`, `BasketballResourceTests.cs`, `WorkshopIntegrationHarnessTests.cs`, `VisibleSolidColliderTests.cs` -- add the list members named above -- registration expectations.
4. `CuriousContraptions.tests/SoccerBallTests.cs` -- write the SB tests -- acceptance for EL-077.
5. `CuriousContraptions.tests/CannonballTests.cs` -- write the CB tests -- acceptance for EL-091.
6. `docs/planning/elements/EL-077-soccer-ball.md`, `EL-091-cannonball.md` -- record the delivered values, the band and the fixed-preset decision in the Declaration section -- spec.
7. `docs/planning/invest/named-elements.md` -- in each of the two entries replace the **Backlog** sentence with: "**Delivered** by package `elements/<Dir>` (id `<id>`), tests `<Class>` in `spec-family-ball-presets.md`; browser qualification pending." -- ledger rows.
8. `docs/planning/elements/EL-077-soccer-ball.md`, `EL-091-cannonball.md` -- **binding step, open until the coverage follow-on publishes:** append a `## Package source binding` section in the syntax then current on main. As main stands it would be the five lines shown under "Coverage binding on main" with acceptance `[todo-217](../requirements.md#todo-217), [sequence-task-630](../requirements.md#sequence-task-630)` for the Soccer ball and `[todo-197](../requirements.md#todo-197), [sequence-task-644](../requirements.md#sequence-task-644)` for the Cannonball, and it would be refused on the two unmet checks. The exact lines, and any decision block or ownership line the follow-on requires, are written here once it publishes -- coverage admission.
9. `docs/planning/invest/delivery-batches.md:17`, `sprint-status.yaml`, `TODO.md` -- record the delivered packages and the two keys as `done` in the candidate commit -- status. `_bmad-output/implementation-artifacts/deferred-work.md` -- add the owned exclusion: "Campaign coverage ledger has no Cannonball row (EL-091 spec section 5; `docs/planning/requirements.md:2396`). Source: spec-family-ball-presets. Owner: the level slice that first places a Cannonball (first use 41–50 with the Toy cannon), which adds the row under `sequence-task-319`. Not changed here because requirement text is a hashed coverage source."

**Acceptance Criteria:**
- AC-077: Given the Soccer ball package, when the SB tests run in the real simulation, then rows SB1–SB5 hold and its bounce and plate load differ from the Bowling ball's on a first Run and again after Reset.
- AC-091: Given the Cannonball package, when a Toy cannon and a Bumper each act on it, then rows CB1–CB5 hold: two different launchers move the same body through contact and paid work, and neither gives it more energy than was paid.
- AC-F: Given the two packages, when the registration check and full local round run, then row F1 holds with zero failures and zero skipped tests, no test is deleted, and no Baseball test changes.

## Design Notes

1. **Behaviour and scope.** Two fixed ball presets; outcomes are bounce, load and launch differences a player can see (ledger rows `element-077`, `element-091`).
2. **Reusable primitive.** None new. Dynamic sphere, band collider, material, ambient load and capture policy are delivered (probe rows 2–7, 17).
3. **Declared composition.** Each package declares metadata, one scene body with its material and two colliders (sphere and band), a constant mass frame, one ambient load and one airflow receiver; nothing else.
4. **Execution and ownership.** No state, writer or update loop is added; the shared solver owns every body (`docs/architecture/element-package-isolation.md`).
5. **Generality.** Tests compare each preset with delivered balls under the same launcher, plate and drop, so no expectation is copied from the code under test.
6. **Forward-only.** Nothing is deleted or kept for compatibility; see "Deleted by this slice".
7. **Prerequisites.** The coverage follow-on first. Nothing else; the Baseball repair is independent.

## Verification

Executor: the implementation owner runs every command; the coordinator reads the results. Environment and the five compiler-log builds are exactly those in `docs/delivery-workflow.md#reproducible-local-verification`. A source change under `elements/` or `CuriousContraptions.tests/` invalidates `solution.binlog` and `playtest.binlog`.

**Commands:**
- The five `dotnet build … -bl:` captures from the workflow -- expected: zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: both canary `PASS` lines, the `Coupling dependencies:` summary, zero violations.
- The registration-check `dotnet test … --filter` command from `docs/delivery-workflow.md#new-package-registration-check`, with `|FullyQualifiedName~CurrentCapabilityInputsTests` appended (the published filter omits it) -- expected: zero failed, `Skipped: 0`.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~SoccerBallTests|FullyQualifiedName~CannonballTests|FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~PackageRuntimeParityTests|FullyQualifiedName~CurrentCatalogueTests"` -- expected: zero failed, `Skipped: 0`; the owner records the case count.
- `git add -N` on every new file, then `git diff --check <baseline_revision>` -- expected: no output.
- `git diff --stat <baseline_revision> -- elements/Baseball CuriousContraptions.tests/BaseballTests.cs engine tools schemas` -- expected: no output.
- `caffeinate -i` the full-round `dotnet test` command from the workflow -- expected: zero failed, `Skipped: 0`; the total rises by the new cases and nothing is removed.
- The candidate commit runs the unmodified `.githooks/pre-commit`. It tests the index snapshot, so every new file, including `.uid`, `.import`, `.tscn` and `.svg`, is staged before committing.

### Acceptance-to-test table

Tolerances reused from delivered tests: rest within 1 mm of the bench and below 0.05 m/s over the last two seconds of a 2,400-tick window (`TennisBallTests.cs:101`–`:103`); every other window is a stall guard. Declared constants are asserted exactly, because they are exact by contract. In SB1 and CB1 both colliders (sphere, and torus with centreline r − 0.004 and tube 0.014) share the one material, as `BasketballResourceTests.cs:48`–`:54` asserts for the Basketball.

| Row | Test | Control | Boundary | Run |
|---|---|---|---|---|
| SB1 | `SoccerBallTests.SoccerCompilesItsDeclaredSphereMassAndMaterial` | `AnyAuthoredParameterIsRefusedInEditAndSave` (theory: mass, radius, bounce, drag) | zero parameters in the saved record | planned |
| SB2 | `SoccerReboundsAboveTheBasketballAndTheBowlingBallAndRests` | Basketball and Bowling ball in the same construction | band 0.36–0.45 excludes the Basketball's 0.30 and the Tennis ball's 0.72; gap 0.06 is 15 cm on a 2.5 m fall; repeated after Reset | planned |
| SB3 | `OneSoccerBallLeavesADefaultPlateOffAndTwoSwitchIt` (theory) | one Bowling ball switches it | minimum 0.45 kg: one Soccer ball switches it | planned |
| SB4 | `ResetSaveAndLoadRestoreTheAuthoredSoccerBall`; `ReceiverCapturesOnlyTheAlignedSoccerBall` (theory) | forged-parameter save refused `OutOfRange`; shifted ball not captured | second Run after Load reaches the SB2 band | planned |
| SB5 | `SoccerRollsOffATiltedRampAndSlows` | `SoccerOnTheLevelBenchDoesNotDrift` | 0.64 m past the lower end; slower after 480 ticks; 1,200-tick stall guard | planned |
| CB1 | `CannonballTests.CannonballCompilesItsDeclaredSphereMassAndMaterial`; `CannonballLandsDeadAndRests` | `AnyAuthoredParameterIsRefusedInEditAndSave` | `f` ≤ 0.05 (ideal 0.006); rest tolerance | planned |
| CB2 | `ChargedCannonLaunchesTheCannonballSlowerThanABasketballWithinTheWorkPaid` | `AnUnchargedCannonLeavesTheCannonballSeatedAndStill` (speed ≤ 0.05 m/s for 360 ticks) | `OnlyBallsInsideTheSeatWindowSeat` (theory: Cannonball seats; the delivered Tennis ball, 0.5 m, and Bowling ball, 0.56 m, stay `Loading` for 600 ticks); ratio band 0.30–0.55 around the ideal √(1/6) = 0.41 | planned |
| CB3 | `BumperPaysTheCannonballLessSpeedThanABasketball` | `AnEmptyBumperDoesNotLaunchTheCannonball` (strength 0, ≤ 1 m/s) | ≥ 2 m/s separates a paid rebound (about 3.3 m/s from 32 J) from a passive one (below 0.6 m/s) | planned |
| CB4 | `ResetSaveAndLoadRestoreTheAuthoredCannonballAndItLaunchesAgain` | forged-parameter save refused | second launch inside the CB2 band | planned |
| CB5 | `CannonballRollsOffATiltedRampAndSlows` | `CannonballOnTheLevelBenchDoesNotDrift` | 0.64 m past the lower end; slower after 480 ticks; 1,200-tick stall guard | planned |
| F1 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (two new rows); `PackageRuntimeParityTests.EveryPayloadPackageCompilesItsDeclaredMaterial`; `WorkshopIntegrationHarnessTests` palette rows; `BasketballResourceTests.TheBundleAndSceneUseDeclaredNumericMaterials`; `CurrentCatalogueTests.NamedPackageUsesItsExistingTaskAcceptanceWithoutCatalogueAlias` (real repository, now three bindings) | `BundleValidatorTests.ADuplicatePaletteOrderNamesBothPackages` (existing) | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` covers the two by derivation | planned |

Numeric working for the bands (fall fractions from e², the finite-work speed ratio, the Bumper's 32 J store) goes to run-log entry 1 when the slice starts.

**Manual checks (if no CLI):**
- Browser qualification of the two palette entries is pending until the post-catalogue E2E phase; nothing is run now.

## Spec Change Log

## Review Triage Log

## Auto Run Result
