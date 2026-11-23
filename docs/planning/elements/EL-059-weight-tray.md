# EL-059 · Weight tray declaration readiness spec

**Current delivery (24 October 2026):** [EL-059 delivery slice](../invest/named-elements.md#element-059). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

Deepened to build-ready detail on 21 November 2026 against checkout `8d1d68aa`; every `path:line` outside section 4 was read in that checkout. Values are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-059 |
| Name | Weight tray |
| Type | Mechanical |
| Requirement anchor | [element-059](../requirements.md#element-059) (sequence-task-612); scope index [todo-320](../requirements.md#todo-320); integration [sequence-task-371](../requirements.md#sequence-task-371) |
| Named entry | [named-elements.md#element-059](../invest/named-elements.md#element-059); proof owner S368. The entry "also retains [CAT-052 acceptance](CAT-052-pressure_plate.md); a refinement does not waive distinct behavior" |
| CAT spec refined or extended | Refines [CAT-052 pressure plate](CAT-052-pressure_plate.md) (delivered, `elements/PressurePlate/`): the same contact-load sensing on a rimmed pan |
| Related identities | [EL-058 size grate](EL-058-size-grate.md) (sorts by size; this sorts by mass), [EL-060 electromagnet](EL-060-electromagnet.md) (by material), [EL-069 moving bucket](EL-069-moving-bucket.md) |
| Roadmap story | [EL-059 delivery slice](../invest/named-elements.md#element-059); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-CONTACT-OBSERVATIONS (its contact load), ENGINE-SUPPLY-DEMAND (the supply it switches) and ENGINE-ISLANDS-SLEEP (weighed cargo at rest); then S7. No package exists (`elements/` has no `WeightTray`). Planning spec at build-ready detail |
| Disposition | To deliver as a single element: a **variant** of the delivered Pressure plate, **not a Duplicate closure** (deltas below) |
| Planned package | id `weight_tray`, directory `elements/WeightTray/`, class `WeightTrayElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | **Not classified**: EL-059 appears in none of sections 1a, 1b, 1c, 2a, 2b or 2c of `delivery-batches.md` and falls under its "Not classified here … need their domain's engine slice" line. **Corrected**: it needs no engine slice of its own beyond the engine programme (owner decision 3a). It belongs in 2c beside the other refinements, with the residual "a rim that holds rolling cargo on the sensing face; default threshold 2 kg" |

**Duplicate or variant.** The coordinator asked for a judgement by any behaviour, parameter, port, mode or acceptance delta. The sensing rule, the three ports and the parameter key are the plate's. The deltas are:

1. **Geometry and behaviour.** Four rim walls hold a rolling ball on the face. On the flat plate a ball with any sideways speed rolls off and the level drops (`CuriousContraptions.tests/PressurePlateTests.cs:522`); on the tray it stays and is weighed. This is an acceptance delta (row T5).
2. **Declared values.** Sensing face 1.4 m instead of 1.8 m; default `minimum_mass` 2 kg instead of 0.5 kg, so the default tray separates the two default balls.
3. **Art and catalogue entry.** A pan on a pedestal with a gauge strip, its own entry (D8).

One delta is enough, so EL-059 is a variant with its own package. It is the plate's declaration shape with different declared values and scene, which is why it needs no engine work of its own beyond the engine programme.

## 2. Declaration

Shape followed: `elements/PressurePlate/PressurePlateElementDeclaration.cs`, line for line, with a different scene and default.

**Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static`, `PayloadPolicy.None`, zero mass frame, domains Mechanical | Electrical | Signal, footprint 1 body, 0 moving.

**Body and colliders.** One static body, local Y up, baked in this order:

| Collider | Shape (m) | Centre (m) |
| --- | --- | --- |
| 0 pedestal | box 1.0 × 0.2 × 1.0 | (0, −0.12, 0) |
| 1 pan floor (sensing face at y 0.08) | box 1.4 × 0.1 × 1.4 | (0, 0.03, 0) |
| 2, 3 rim ±X | box 0.08 × 0.2 × 1.56 | (±0.74, 0.08, 0) |
| 4, 5 rim ±Z | box 1.4 × 0.2 × 0.08 | (0, 0.08, ±0.74) |
| 6–8 sockets | sphere r 0.075 | (−0.575, −0.12, 0), (0.575, −0.12, 0), (0, −0.12, 0.575): tangent to the pedestal faces, under the pan overhang |

The rim top is 0.10 m above the face. The face height equals the plate's (`elements/PressurePlate/pressure_plate.tscn`, top box at y 0.03, 0.1 thick), so a tray and a plate on one bench are level.

**Material** (restitution, friction, rolling resistance, bounce threshold): (0.1, 0.3, 0, 0.1), the plate's (`PressurePlateElementDeclaration.cs:57`).

**Parameters.**

| Key | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `minimum_mass` | Real | Kilogram | 2 | 0.1 | 16 | Reject | BuildOnly |

The key and range are the plate's (`PressurePlateElementDeclaration.cs:39`); the earlier name `threshold` and the `dwell` parameter are dropped (section 6).

**Ports.** Body 0, positions in metres.

| `PortId` | Role | Domain | Direction | Signal kind | Position | Normal |
| --- | --- | --- | --- | --- | --- | --- |
| `PowerIn` | PowerIn | Electrical | Input | — | (−0.575, −0.12, 0) | −X |
| `ActivationOut` | ActivationOut | Signal | Output | Level | (0, −0.12, 0.575) | +Z |
| `Supply` | Supply | Electrical | Output | — | (0.575, −0.12, 0) | +X |

`ActivationOut` is listed before `Supply`, as on the plate (`PressurePlateElementDeclaration.cs:44`–`:50`).

**Signal and supply.** One `SignalContactLoadDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:58`): node `signal`, frame body 0, `MinimumMassParameter` `minimum_mass`, `RegionCollider` 1, normal +Y, minimum alignment 1 − 10⁻⁶, contact distance 10⁻⁴ m, depth margin 10⁻⁶ m, release hold 0.1 s, `SupportingColliders` [1]. One `SupplyContactDescriptor` `contact` controlled by `SignalStateContactControl(signal, Latched)`, bound `PowerIn` → `contact` → `Supply`; one `ElectricalNodeDescriptor` switch (12 V, 0.05 Ω, 100 W). One event `ContactLoad` on `signal`. All as `PressurePlateElementDeclaration.cs:60`, `:78`–`:98`. No stores.

**Presentation.** `PickRadius` 1.1. Three bindings on `Visual/Gauge` (albedo red, green, blue) driven by `PresentationDriver.CommittedContactLoadPhase` with the plate's three state colours (`PressurePlateElementDeclaration.cs:14`–`:16`): slate empty, ochre underweight, gold loaded.

**Palette.** Category `ElementCategory.Control`, palette order 402 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Weight tray", colour (0.91, 0.72, 0.39), the plate's, so the two load sensors share a family tone.

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Visual/Pedestal` | box 1.0 × 0.2 × 1.0 | solid, collider 0 |
| `Visual/Pan` | box 1.4 × 0.1 × 1.4 | solid, collider 1 |
| `Visual/Rim_0`…`Rim_3` | boxes as colliders 2–5 | solid, colliders 2–5 |
| `Visual/Socket_PowerIn`, `_Supply`, `_ActivationOut` | spheres r 0.075 | solid, colliders 6–8 |
| `Visual/Gauge` | strip 0.6 × 0.08 on the outer face of the +Z rim, flush | decorative: printed flat; its colour is moved by a binding |
| `Visual/ScaleMark` | a balance glyph on the pan floor, flush | decorative: printed flat |

Colours (`DESIGN.md:80`): pan Cream `#fff8e9` (part-body cream, owner decision 4), rim Ochre `#d99e32`, pedestal Navy `#1c2836`, sockets Ochre. There are no studs and no raised ring (the plate's own studs and ring are collided in this checkout, `elements/PressurePlate/pressure_plate.tscn:119`–`:141`). Icon: a shallow pan on a pedestal with a ball in it and a three-step gauge. At a glance it differs from the plate by its raised rim and narrow pedestal; the plate is a flat slab with corner studs.

### Behaviour and numbers

- **Law.** The engine's contact-load rule, unchanged: the measured load is the sum of the masses (kg) of dynamic bodies in resting or pressed contact with the face, each body once, contact normal within 10⁻⁶ of +Y, inside the face footprint. The phase is Loaded when the sum is at least `minimum_mass` (inclusive), Underweight when positive and below it, otherwise Empty. Rim contacts have a sideways normal and never count. An object beside the tray touches no face and contributes nothing: the requirement outcome.
- **"Measured force or threshold state."** The row offers either. The tray exposes the threshold state on its gauge, its level output and its switched supply. No analogue read exists in the engine (the sensor commits a phase), none is required, and none is added (section 6).
- **Tick order.** The level commits with the tick's contacts and is held 0.1 s (12 ticks at 120 Hz) after the last qualifying tick; the supply route reads it on the next tick (`CuriousContraptions.tests/PressurePlateTests.cs:576`).
- **Default 2 kg.** Basketball 1 kg (`elements/Ball/BallElementDeclaration.cs:51`) is underweight; Bowling ball 4 kg (`elements/BowlingBall/BowlingBallElementDeclaration.cs:54`) and the default 4 kg Weight (`elements/Weight/WeightElementDeclaration.cs:33`) are loaded; a Tennis ball (0.35 kg) and a Domino (0.4 kg) are underweight. Range 0.1–16 kg covers two 8 kg Weights.
- **Face 1.4 m.** Holds one Basketball (0.68 m) with room, or two Bowling balls (2 × 0.56 = 1.12 m) for a summed load; smaller than the plate's 1.8 m so the two read as different parts.
- **Rim 0.10 m.** A ball needs about √(10/7 · g · 0.10) = 1.2 m/s of rolling speed to climb it, so cargo delivered at ordinary ramp-exit speeds below 1 m/s stays. A Linear pusher or a tilt still clears the tray, because the rim is below every ball's centre.
- **Stacks.** Only direct contact counts, as on the plate (`PressurePlateTests.cs:466`). The earlier proposal that a stack should count belonged to the sprung tray and is withdrawn with it.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static multi-collider body from scene geometry | *present* | `ISceneGeometryElement` `SceneGeometry.cs:10` | `CuriousContraptions.tests/PressurePlateTests.cs:56` |
| Resting-mass threshold on a declared face of a static frame, any package | *present* | `SignalContactLoadDescriptor` `SignalContractValues.cs:58`; static-frame rule `CuriousContraptions.Bundle/SignalBundleValidation.cs:190` | `CuriousContraptions.tests/DeclaredContactLoadTests.cs:22`, `:68`; `PressurePlateTests.cs:210`; a second, synthetic package: `CuriousContraptions.tests/ComposedPhysicalSignalTests.cs:121` |
| Threshold bound to an instance parameter | *present* | `MinimumMassParameter` | `DeclaredContactLoadTests.cs:85`, `:106`; `PressurePlateTests.cs:407` |
| Supply contact closed by the committed level | *present* | `SignalStateContactControl` `CuriousContraptions.Abstractions/SignalContactControl.cs:12` | `CuriousContraptions.tests/PoweredGateTests.cs:240`; `CuriousContraptions.tests/CatalogueIntegrationTests.cs:139` |
| Level signal output with release hold | *present* | `PortSignalKind.Level` `CuriousContraptions.Abstractions/PortDescriptor.cs:17` | `PressurePlateTests.cs:717`, `:828` |
| Three-state colour from the committed phase | *present* | `PresentationDriver.CommittedContactLoadPhase` `CuriousContraptions.Abstractions/ElementPresentation.cs:24` | `CuriousContraptions.tests/DeclaredPresentationTests.cs:288` |
| Side contacts and neighbours excluded | *present* | same sensor | `PressurePlateTests.cs:210` (side, underside, outside the footprint, another frame) |
| Run/Reset and save/load | *present* | generic | `CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`; `PressurePlateTests.cs:646` |
| Analogue load read on a port | *missing*, **not needed** | none | no owner: not required by the row (section 6) |

Counts for what the tray needs: 8 present, 0 untested, 0 narrower, 0 missing.

**Element dependencies.** Battery, Powered gate, Signal lamp, Basketball, Bowling ball, Weight, Domino: all delivered.

### Engine conformance

- **Declaration data only.** One static body, one declared contact-load sensor, one supply contact, three ports. It is the Pressure plate's declaration shape with other values; no callback or engine branch, and no test or tool may key the sensor on a package id.
- **Generic laws.** Weighing is the engine's contact-load rule on a declared face with the declared `minimum_mass`; the output is a supply contact controlled by the committed level; the rim holds cargo by contact.
- **Layers.** Colliders: 6 static boxes (pedestal, pan floor, 4 rim walls) and 3 socket spheres. Visuals: the art table, none used as a collider. Animation: the gauge colour is a presentation binding reading the committed load phase; nothing moves any other art node. Visible = solid: pedestal, pan, rim walls and sockets each have their collider; the gauge strip and scale mark are printed flat.
- **One-way flow and typing.** The binding writes nothing back. The phase is a closed enum; ports, roles and domains are enums; `minimum_mass` is a typed f32 parameter.
- **Flat state.** 1 static body, 9 colliders (6 boxes, 3 spheres), 0 joints, 0 drives, 1 sensor, 1 signal node, 1 supply node, 1 electrical node, 0 stores, 3 albedo bindings.
- **Performance.** The sensor region is one bounded face; its contact loads are found by index once ENGINE-CONTACT-OBSERVATIONS is delivered. Nine colliders against the plate's larger set; the rim boxes are the behaviour, not decoration. No island sleeping exists today; a resting load costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows T1 to T12 run the production simulation through placed parts and wires.

## 4. Sources and legacy

- Requirements row: "An object beside the tray cannot contribute weight." No variants.
- todo-320 integration: sorting by observable mass.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | The pressure plate sums masses directly resting on its top face — not impacts, not stacked load transmission, not a power supply. | `parts/PressurePlatePart.cs@a6c914e:L8-L9` | carry forward "not impacts" and "not a supply"; do not carry forward "not stacked load" for the tray | The tray measures real transmitted force, so a stacked load counts (Open question 2). |
| 2 | Load region: a box just above the top face, contact normal alignment ≥ 1 − 1e-6 with local +Y, threshold mass. | `parts/PressurePlatePart.cs@a6c914e:L21-L25`; `engine/SceneContactLoadSensorDeclaration.cs@a6c914e:L5-L6` | carry forward (geometry rule for the plate); the tray uses spring force instead | Region test is the plate's mechanism, recorded for comparison. |
| 3 | Threshold 0.1–16 kg, default 0.5 ("one tennis ball is too light"). | `parts/PressurePlatePart.cs@a6c914e:L38-L40`; `parts/catalog/pressure_plate.tres@a6c914e:L8-L14` | carry forward | Sourced threshold scale. |
| 4 | Indicator: slate empty, ochre underweight, gold loaded. | `parts/PressurePlatePart.cs@a6c914e:L59-L59`; `DESIGN.md@a6c914e:L293-L293` | carry forward | Visual language. |
| 5 | Phase enum {Empty, Underweight, Loaded}. | `parts/PressurePlatePart.cs@a6c914e:L20-L20` | carry forward | Closed set for the threshold state read. |

**Files harvested:** `parts/PressurePlatePart.cs`, `parts/catalog/pressure_plate.tres`, `engine/SceneContactLoadSensorDeclaration.cs`. No weight-tray legacy (searched for tray, scale, weigh).

## 5. Acceptance outline

Requirement row: [element-059](../requirements.md#element-059).

- **Chrome UI recipe (pending, post-catalogue).** Place a Weight tray, a Battery and a Powered gate; wire Battery → tray `PowerIn`, tray `Supply` → gate `PowerIn`; set `minimum_mass` 2 kg; place a Bowling ball above the tray and a Basketball on the bench beside it. Verify placement, threshold and wires. Run: the gate opens after the Bowling ball settles; swapping the two balls leaves it closed.

Planned tests are in `WeightTrayTests` unless named otherwise. "Gate" is a Powered gate wired as in the recipe; its supplied state is the observed result.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| T1 | A Bowling ball settles in the tray at 2 kg. Then the phase is Loaded and the gate is supplied | `AHeavyBallLoadsTheTrayAndOpensItsGate` | a Basketball: Underweight, gate never supplied | — | exact phase and supplied flag; guard 5 s |
| T2 | Requirement outcome: a Bowling ball at rest on the bench touching the outside of the rim, tray empty. Then the phase is Empty and the gate is never supplied | `ABallBesideTheTrayContributesNothing` | T1 | a second ball inside and underweight: the outside ball adds nothing to it | exact; observed 5 s |
| T3 | The threshold is inclusive: a 4 kg load at `minimum_mass` 4 is Loaded; at the next f32 above 4 it is Underweight | `TheThresholdIsInclusive` (pattern `PressurePlateTests.cs:443`) | — | one f32 step, an explicit admission threshold | exact |
| T4 | Two Bowling balls at `minimum_mass` 8 are Loaded; one is Underweight | `LoadsOnTheFaceSum` | one ball | — | exact; guard 5 s |
| T5 | The variant's delta: a Bowling ball lands on the face with 0.8 m/s sideways speed. Then it stays in the tray and the level stays high | `ARollingBallIsHeldAndWeighed` | the same ball and pose on a Pressure plate rolls off and that plate's level drops | — | level high on every tick from 2 s to 10 s; the plate's level low by 10 s |
| T6 | CAT-052 rules retained: two flat Dominoes stacked read 0.4 kg; a ball in the corner touching floor and two rim walls counts once | `OnlyDirectFloorContactCounts` | two Dominoes side by side read 0.8 kg | — | exact phase at `minimum_mass` 0.5 |
| T7 | A Signal lamp on `ActivationOut` is lit exactly while the level is high | `TheLevelOutputLightsALamp` (pattern `PressurePlateTests.cs:717`) | unloaded tray: dark | — | exact per tick |
| T8 | A loaded tray with no battery leaves the gate unsupplied | `ALoadedTrayWithoutSupplyPowersNothing` (pattern `PressurePlateTests.cs:679`) | T1 | — | exact |
| T9 | Wrong-kind connections are refused: an activation wire into `PowerIn`, an electrical wire from `ActivationOut` | S4 row R5 once delivered, else `ConnectionsAcceptTheirOwnKindOnly` (pattern `PressurePlateTests.cs:119`) | the recipe's wires are admitted | — | exact refusal |
| T10 | `minimum_mass` below 0.1, above 16 or missing is refused at every seam; 0.1 and 16 admitted | S4 row R2 once delivered, else `AnOutOfRangeMinimumMassIsRefused` (pattern `DeclaredContactLoadTests.cs:106`) | bounds admitted | one f32 step outside | exact refusal |
| T11 | Run, Reset, save and load with `minimum_mass` 3 and the recipe's wires restore pose, phase, gate and parameter | `ResetAndSaveLoadRestoreTheTray`; placed alone: `CatalogueIntegrationTests.cs:273` | — | — | authored state as the generic test compares it |
| T12 | Every solid art node has its collider and the sockets collide | `WeightTrayArtTests.SceneCollidersMatchTheArt` (pattern `PressurePlateTests.cs:56`) | — | — | exact counts and sizes |

## 6. Open questions

**Resolved**

1. Measured and threshold outputs, both or a mode? Threshold only: the row says "measured force **or** threshold state", no consumer of an analogue value exists, and D19 adds an output only when a row or lesson uses it. Not a narrowing: the row's alternative is met.
2. Does a stacked load count? No. Decided now: the ledger entry retains CAT-052 acceptance, whose rule is direct contact (`PressurePlateTests.cs:466`); one rule for both load sensors.
3. Scalar signal domain and `MeasureOut`? Not built (item 1). `SimulationDomain` gains no member.
4. Continuous or latched threshold output? Continuous: `ActivationOut` is a Level that clears 0.1 s after the load leaves, as delivered for the plate.
5. Sprung, sinking tray (the harvest-time proposal)? Withdrawn. Decided now: the contact-load sensor requires a static frame (`SignalBundleValidation.cs:190`), a kilogram threshold cannot set a spring stiffness (`MechanicalProduct` admits only force × speed, `CuriousContraptions.Abstractions/MechanicalProduct.cs:28`), and a sprung tray would trip on impact; the rimmed static pan gives the lesson with delivered capabilities.
6. `dwell` parameter? Dropped: the sensor already ignores non-resting contacts and holds 0.1 s (D13, fewer controls).
7. Duplicate of the Pressure plate? No: variant (section 1).

**Needs the owner**

None. For information only: if the owner wants a numeric weight read-out, that is a new analogue-signal capability with no story today and would be raised as its own engine item.

## 7. Integration

- **Collaborators.** Battery, Powered gate, Linear pusher (`ExtendIn` fed from `Supply`), Signal lamp, every ball, Weight, Domino: all delivered. Port kinds agree at both ends: `Supply` (Electrical, Output) to a `PowerIn` (Electrical, Input); `ActivationOut` (Signal, Level) to the lamp's `ActivationIn`.
- **Sorting task** [sequence-task-371](../requirements.md#sequence-task-371): this spec covers the mass clause (T1, T2); it needs neither EL-058 nor EL-060.
- **Campaign.** First use levels 91–100 as "weight tray/material sorter", reuse 111–120 and 136–150 (`docs/planning/invest/campaign-reservations.md:75`); the plate's control contract from levels 21–40 is its stated prerequisite. Owner decision 24e moves the first use to level **25** in chapter 3, beside the Pressure plate ([per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)).
- **S8.** A lesson that places the tray needs the lesson-fixture story S8 first.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `weight_tray` |
| Named identity | EL-059, `../invest/named-elements.md#element-059` |
| Acceptance | `todo-320` and `sequence-task-612` |
| Decision | S368; `scope-corrections.md` has no `s368` block |
| Mode records | `Configuration=Adjustable` (it declares a parameter; confirmed at slice planning) |

What it lacks today: an `s368` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-320 anchor is a scope index, not immediately followed by its sequence-task-612 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element; declaration data, scene, icon and tests.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-CONTACT-OBSERVATIONS, ENGINE-SUPPLY-DEMAND and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 only shortens the edit list. No engine story of its own.
- **Files added.** `elements/WeightTray/WeightTray.csproj`, `WeightTrayElementDeclaration.cs`, `weight_tray.tscn`, `icon.svg`; `CuriousContraptions.tests/WeightTrayTests.cs`, `WeightTrayArtTests.cs`.
- **Files edited.** No test-project edit (published main compiles every top-level `*.cs` by one rule); until S3, `CuriousContraptions.slnx`, the trim list at `CuriousContraptions.tests/CSharpStaticGateTests.cs:703` and the parameter and placement switches at `CuriousContraptions.tests/PackageRuntimeParityTests.cs:483` and `:641` (generic list at `:653`; the plate has its own arm at `:655`); at delivery the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Data-only (Verification Gap, Forward-Only Auditor, Element-Free Engine Auditor): every capability is present with a cited test.
- **Disqualifiers the probe must re-check.** That the baked collider order puts the pan floor at index 1 (or the declaration names the index the scene bakes); that a second package declaring `SignalContactLoadDescriptor` compiles its own row beside the plate's (`DeclaredContactLoadTests.cs:22` asserts per plate; `ComposedPhysicalSignalTests.cs:121` covers a synthetic package); that no test or tool keys the contact-load path on the id `pressure_plate`. Any of these failing makes the slice engine tier and is repaired in the engine, never by a tray-specific branch.
