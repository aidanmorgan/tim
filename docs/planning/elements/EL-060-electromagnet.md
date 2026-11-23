# EL-060 · Electromagnet declaration readiness spec

**Current delivery (24 October 2026):** [EL-060 delivery slice](../invest/named-elements.md#element-060). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

Deepened to build-ready detail on 21 November 2026 against checkout `8d1d68aa`; every `path:line` outside section 4 was read in that checkout. Values are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-060 |
| Name | Electromagnet |
| Type | Mechanical |
| Requirement anchor | [element-060](../requirements.md#element-060) (sequence-task-613); scope index [todo-320](../requirements.md#todo-320) and the scope index at `docs/planning/requirements.md:3039`; integration [sequence-task-371](../requirements.md#sequence-task-371) and [sequence-task-393](../requirements.md#sequence-task-393) ("a magnet sorts only declared compatible material") |
| Named entry | [named-elements.md#element-060](../invest/named-elements.md#element-060); proof owner S642. The "material sorter" obligation at `docs/planning/invest/named-elements.md:1986` is this identity's |
| CAT spec refined or extended | none. Related: [CAT-005 battery](CAT-005-battery.md) (supply, delivered), [CAT-067 weight](CAT-067-weight.md) (the ferromagnetic responder, delivered), [CAT-014 bowling](CAT-014-bowling.md) and [CAT-001 ball](CAT-001-ball.md) (non-responsive controls) |
| Related identities | [EL-058 size grate](EL-058-size-grate.md) and [EL-059 weight tray](EL-059-weight-tray.md) (todo-320 sorting); EL-227 Magnetic impulse relay (same engine story); [EL-091 cannonball](EL-091-cannonball.md) (an iron ball, a second responder once delivered); [EL-063 cable winch](EL-063-cable-winch.md) (crane pickup) |
| Roadmap story | [EL-060 delivery slice](../invest/named-elements.md#element-060); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the coil's standing draw), ENGINE-SPATIAL-QUERIES (field candidates) and ENGINE-ISLANDS-SLEEP (a held or dropped Weight at rest); then ENGINE-FIELD-FORCE; then S7. No package exists and the engine has no field force. Planning spec at build-ready detail |
| Disposition | To deliver as a single element after its engine story |
| Planned package | id `electromagnet`, directory `elements/Electromagnet/`, class `ElectromagnetElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | Not listed by number. Section 1c lists only EL-227 under "Field force"; the story's own text named "electromagnet pickup" at `25df3cd5` (`_bmad-output/planning-artifacts/epics.md@25df3cd5:4450`). **Confirmed** from code: no field source, responding property or field supply target exists, so EL-060 is a 1c identity behind `ENGINE-FIELD-FORCE`. Proposed addition to 1c is in the batch summary |

## 2. Declaration

Shape followed: `elements/Battery/BatteryElementDeclaration.cs` for a static electrical box with a supply-driven lamp, and `elements/Fan/FanElementDeclaration.cs:73` for a consumer that pays continuously for a declared force source. The field declaration itself has no delivered pattern.

**Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the field-source declaration interface that `ENGINE-FIELD-FORCE` adds (it does not exist; this spec does not name it). `KinematicMode.Static`, `PayloadPolicy.None`, zero mass frame, domains Mechanical | Electrical, footprint 1 body, 0 moving.

**Body and colliders.** One static body; local −Y is the pole direction.

| Collider | Shape (m) | Centre (m) |
| --- | --- | --- |
| 0 housing | box 0.7 × 0.4 × 0.7 | (0, 0, 0) |
| 1 pole plate (pole face at y −0.26) | box 0.5 × 0.06 × 0.5 | (0, −0.23, 0) |
| 2 socket | sphere r 0.075 | (−0.40, 0.10, 0) |

**Material** (restitution, friction, rolling resistance, bounce threshold): (0, 0.6, 0, 0.1). No bounce, so an attracted load seats on the pole instead of chattering; friction 0.6 so a held load does not slide off sideways.

**Field source (to be declared through the engine story's descriptor).** Frame body 0; origin the pole-face centre (0, −0.26, 0); axis −Y; region a cylinder of radius 0.5 m reaching `range` along the axis; force on a responding body whose centre is in the region, directed from its centre to the origin, of magnitude `max_force` × (1 − d / `range`) newtons, d the centre-to-origin distance in metres; zero outside the region, at d ≥ `range`, and on non-responding bodies; no occlusion by intervening bodies. Enabled while the `PowerIn` route is closed.

This is one instance of ENGINE-FIELD-FORCE's unified source (one text for all batches' additions, `readiness/batch-l.md` section 4 item 6, with the gravity law of `readiness/batch-m.md` section 4 item 7): a bounded region on a frame body, a responding-property selector, a Force quantity with Linear falloff, membership by centre of mass with candidates found through ENGINE-SPATIAL-QUERIES (waking sleepers), and payment kind StandingDraw (the supply-switched draw of `power`, debited under ENGINE-SUPPLY-DEMAND). The direction is declared as data in the point form, toward the declared point at the pole-face centre, as above: one form of the one uniform-field law, whose direction is a constant vector or toward a declared point (coordinator decision 29; [ENGINE-FIELD-FORCE](../../../_bmad-output/planning-artifacts/epics.md#story-engine-field-force-the-one-uniform-field-law-and-declared-force-regions), "Direction as data"). Rows M1 to M8 are derived for this form.

**Responding property.** The Weight carries the responding property: the story's declared property, declared on a body by one rule for every consumer, is set on the Weight (`elements/Weight/WeightElementDeclaration.cs`, an edit inside this slice) and on no other delivered part. Basketball, Bowling ball, Tennis ball, Domino and Balloon stay non-responding.

**Parameters.**

| Key | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `max_force` | Real | Newton | 100 | 5 | 120 | Reject | BuildOnly |
| `range` | Real | Metre | 1.2 | 0.2 | 2 | Reject | BuildOnly |
| `power` | Real | Watt | 10 | 1 | 50 | Reject | BuildOnly |

**Ports.** Body 0.

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) | Normal |
| --- | --- | --- | --- | --- | --- | --- |
| `PowerIn` | PowerIn | Electrical | Input | — | (−0.40, 0.10, 0) | −X |

**Supply.** One `SupplyConsumerDescriptor` `coil` whose target is the field source (a supply target kind the engine story adds); `PowerIn` bound to its `PowerIn` terminal. There is no `Supply` pass-through: no descriptor relays it and no row or lesson uses one (D19). One `ElectricalNodeDescriptor` consumer: 12 V, 50 W maximum, 2.88 Ω (the delivered rule R = V²/MaxPower, as the Clutch coil's 7.2 Ω at 20 W, `elements/Clutch/ClutchElementDeclaration.cs:156`–`:158`). While supplied the coil draws `power` watts from the source store (payment kind StandingDraw; the debit is ENGINE-SUPPLY-DEMAND's). No stores, no events, no signal nodes.

**Presentation.** `PickRadius` 0.6. Albedo of `Visual/Lamp` driven by `PresentationDriver.CommittedSupply` (slate unsupplied, gold supplied), as the Battery terminal (`elements/Battery/BatteryElementDeclaration.cs:12`–`:14`). No field glyphs: the lamp and the moving load show the state.

**Palette.** Category `ElementCategory.Power`, palette order 312 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Electromagnet", colour `#66b8c9` (the electric motor family, `DESIGN.md:239`).

### Art and solidity

| Node | Mesh and dimensions (m) | Class |
| --- | --- | --- |
| `Visual/Housing` | box 0.7 × 0.4 × 0.7 | solid, collider 0 |
| `Visual/PolePlate` | box 0.5 × 0.06 × 0.5 | solid, collider 1 |
| `Visual/Socket_PowerIn` | sphere r 0.075 | solid, collider 2 |
| `Visual/CoilBand` | a band drawn round the housing sides, flush | decorative: printed flat |
| `Visual/Lamp` | disc on the housing top, flush | decorative: printed flat; colour moved by a binding |
| `Visual/PoleMark` | a horseshoe glyph on the pole face, flush | decorative: printed flat |

Colours (`DESIGN.md:80`, `:239`): housing Navy `#1c2836`, coil band `#66b8c9`, pole plate Cream `#fff8e9` (part-body cream, owner decision 4), lamp Gold `#f7cb52` when lit. The box housing replaces the earlier cylinder so every collider is a shape with a delivered static test. Icon: the block seen from the side with the pole plate down, three short field strokes and a weight below. At a glance it differs from the Battery (a tall cell with charge marks) by its squat block, hanging pole plate and horseshoe mark.

### Behaviour and numbers

- **Units.** Force N, range m, power W, mass kg, g 9.81 m/s².
- **The Weight it lifts.** The delivered Weight is a cylinder standing on its base whose half-height is 0.03818433·m + 0.12652539 metres (`elements/Weight/WeightElementDeclaration.cs:60`): 0.279 m at 4 kg, 0.356 m at 6 kg, 0.432 m at 8 kg. But the Weight meets the pole with the top of its collided eye, not its cylinder top: the eye top is 0.7346·s above the base, s = 0.136734·m + 0.45307 (`elements/Weight/weight.tscn:91`; `WeightElementDeclaration.cs:73`–`:75`). At contact the centre of mass is therefore 0.7346·s less the half-height from the origin: 0.455 m at 4 kg, 0.518 m at 5 kg, 0.580 m at 6 kg, 0.704 m at 8 kg. Distance d is always measured to the centre of mass, and "gap" below is pole face to eye top. The harvest-time numbers used a legacy radius (0.51 m at 4 kg) and are replaced.
- **Holding at the defaults.** Field at contact is 100 × (1 − d/1.2): 62.1 N on the 4 kg Weight (weight 39.2 N, held with a factor 1.58), 56.9 N at 5 kg (49.1 N, held, factor 1.16), 51.7 N at 6 kg (58.9 N, not held), 41.3 N at 8 kg (78.5 N, not held). The break-even is about 5.5 kg. A default magnet therefore carries the default Weight and cannot carry the heaviest one: the lesson that force is finite.
- **Lifting from rest.** A 4 kg Weight leaves the bench when 100 × (1 − d/1.2) > 39.2 N, that is d < 0.729 m, so the gap from eye top to pole face must be under 0.729 − 0.455 = 0.274 m. Between 0.274 m and the edge of `range` it is inside the field and stays put: placement matters.
- **Arrival.** From a 0.15 m gap the net force grows from 10.3 N to 22.8 N (about 2.5 J over the lift); the Weight arrives at roughly 1.1 m/s and seats without bounce (restitution 0). Held, the 22.8 N normal force at pair friction √(0.6 × 0.3) = 0.42 resists about 9.7 N sideways.
- **Supply and release.** The field exists only while the coil is supplied. The engine story's acceptance removes the force "within the tick the command commits" (`epics.md@25df3cd5:4453`); electrical state resolves in Phase 1 (D7), so a load starts to fall on the tick the supply opens. A disabled, disconnected or exhausted Battery gives no field.
- **Draw.** At 10 W the default 3600 J Battery (`elements/Battery/BatteryElementDeclaration.cs:42`) outlasts the longest admitted Run (300 s, owner decision 17), so a default magnet never drops its load for lack of charge within a Run; the Battery's minimum 60 J holds for 6 s, a usable "it will drop soon" lesson. 50 W stays under the Battery's 120 W default limit. No acceptance row needs more than 30 s, so every row runs within the default Run length (owner decision 13).
- **Reaction.** The magnet is static, so the reaction is absorbed by the world; the story's equal-and-opposite rule matters only to a dynamic source (EL-227).

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test or owning story |
| --- | --- | --- | --- |
| Static box body and sphere sockets from scene geometry | *present* | `ISceneGeometryElement` `SceneGeometry.cs:10` | `CuriousContraptions.tests/ReverseTransmissionLifecycleTests.cs:18` (a static housing supports cargo) |
| Electrical consumer enabled while its `PowerIn` route is closed | *present* | `SupplyConsumerDescriptor` `CuriousContraptions.Abstractions/SupplyContractValues.cs:42` | `CuriousContraptions.tests/MotorTests.cs:294`, `:330` |
| Supply-driven lamp colour | *present* | `PresentationDriver.CommittedSupply` `CuriousContraptions.Abstractions/ElementPresentation.cs:23` | `CuriousContraptions.tests/DeclaredPresentationTests.cs:490` |
| Finite source store that empties | *present* | `SupplySourceDescriptor` `SupplyContractValues.cs:39`; Battery `BatteryElementDeclaration.cs:71` | `DeclaredPresentationTests.cs:490` |
| Declared non-contact force region with falloff and strength | *missing* | none (gravity, airflow wrench and ambient load only) | `ENGINE-FIELD-FORCE` ([story](../../../_bmad-output/planning-artifacts/epics.md#story-engine-field-force-the-one-uniform-field-law-and-declared-force-regions)), direction toward a declared point (coordinator decision 29) |
| Responding material property on a body | *missing* | `MaterialProperties` has four contact floats only (`CuriousContraptions.Abstractions/ColliderDescriptor.cs:21`) | `ENGINE-FIELD-FORCE` |
| Field source switched by the supply route | *missing* | `SupplyTarget` kinds are EnergyStore, Drive, Transmission, ContinuousWork (`SupplyContractValues.cs:44`–`:53`) | `ENGINE-FIELD-FORCE` ("switched by a signal or supply") |
| Continuous electrical draw while a force source is supplied | *narrower* | `SupplyContinuousWorkTarget` takes an airflow source id only (`SupplyContractValues.cs:50`; Fan `FanElementDeclaration.cs:73`) | `ENGINE-FIELD-FORCE` (payment kind StandingDraw in its unified text, `readiness/batch-l.md` section 4 item 6), debited under ENGINE-SUPPLY-DEMAND |
| Run/Reset and save/load | *present* | generic | `CuriousContraptions.tests/CatalogueIntegrationTests.cs:273` |

Counts: 5 present, 0 untested, 1 narrower, 3 missing; one owning story. Nothing here needs element-specific engine code: the field is a declared region, the response a declared property.

**Element dependencies.** Battery and Weight (delivered). The Weight's declaration gains the responding property in this slice.

### Engine conformance

- **Declaration data only.** One static body, one declared field source, one electrical consumer. The field is a general engine capability stated without naming this part; a body responds because its own declaration carries the responding property, by one rule for every consumer. No callback or engine branch.
- **Generic laws.** Attraction is the declared field law (linear falloff, force referenced to the responding body's centre of mass); holding is field force plus contact and friction; release is the supply route opening.
- **Layers.** Colliders: 2 static boxes (housing, pole plate) and 1 socket sphere. Visuals: the art table, none used as a collider. Animation: the lamp colour is a presentation binding reading committed supply; nothing moves any other art node. Visible = solid: housing, pole plate and socket each have their collider; the coil band, lamp and pole mark are printed flat.
- **One-way flow and typing.** The binding writes nothing back. The falloff law is a closed enum in the engine story; ports and domains are enums; parameters are typed f32.
- **Flat state.** 1 static body, 3 colliders (2 boxes, 1 sphere), 0 joints, 0 drives, 1 field source, 1 supply node, 1 electrical node, 0 sensors, 0 stores, 3 albedo bindings.
- **Performance.** The field region is a bounded cylinder (radius 0.5 m, length `range`), so candidates come from ENGINE-SPATIAL-QUERIES, not a scan of every body, and the field's work is bounded by the bodies inside it. The engine story states that query's cost; no budget is invented here. No island sleeping exists today; a resting Weight costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows M1 to M13 run the production simulation; none needs a magnet-specific path.

## 4. Sources and legacy

- Requirements row: "Nonresponsive material and absent supply do not satisfy pickup." No variants.
- Research row: "Electromagnet / Magnetic pickup — electrical node + force region on compatible material; force, range; attracts steel while supplied, releases on loss; wooden ball unaffected" (`docs/component-research.md@a6c914e:L81-L81`); recipe "Sorting Office: magnet removes steel ball, remaining ball reaches a different chute" (`docs/component-research.md@a6c914e:L86-L86`).
- Decision: S635 field-response — "A supported responder moves in the field, an unsupported one does not; zero source or reversed field reverses/halts motion" ([decisions.md](../invest/decisions.md#s635)).

No legacy implementation. The only legacy mention is a coverage scope note naming a future magnet element (`reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1575-L1575`), with no element knowledge. Searched `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` (terms: magnet, ferro, field).

**Files harvested:** `reference/P0-022-before/docs/coverage/engine/task-017.json` (checked; no element knowledge).

## 5. Acceptance outline

Requirement row: [element-060](../requirements.md#element-060).

- **Chrome UI recipe (pending, post-catalogue).** Place a Battery and an Electromagnet mounted above the bench, a 4 kg Weight and a Bowling ball under the pole with the pole face 0.15 m above the top of the Weight's eye; wire Battery → `PowerIn`. Verify placement, parameters and the wire. Run: the Weight rises and is held, the Bowling ball stays; disable the Battery: the Weight falls.

Planned tests are in `ElectromagnetTests`. "Gap" is pole face to the top of the Weight's eye; the Weight stands on the bench.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| M1 | Supplied magnet, 4 kg Weight, gap 0.15 m (49.6 N against 39.2 N). Then the Weight reaches the pole face and stays | `ASuppliedMagnetLiftsAndHoldsTheWeight` | M2 and M3 | — | "held": top within 0.01 m of the pole face (a visible gap would read as dropped) on every tick from first contact to +5 s; guard 5 s to reach it |
| M2 | Requirement outcome, material: a Bowling ball (4 kg, non-responding) at the same pose | `ANonRespondingBallIsNotAttracted` | M1 | — | rises less than 0.01 m and speed under 0.05 m/s for 5 s |
| M3 | Requirement outcome, supply: no wire, a disabled Battery, or an empty Battery | `AnUnsuppliedMagnetLiftsNothing` (theory over the three causes) | M1 | — | as M2 |
| M4 | Supply cut while holding. Then the Weight falls to the bench | `CuttingTheSupplyDropsTheLoad` | M1 | — | falling faster than 0.05 m/s within 0.1 s of the cut (the tick of release is the engine story's contract, not this row's); on the bench within 3 s |
| M5 | Lift distance: gap 0.22 m lifts (43.7 N), gap 0.33 m does not (34.6 N) | `TheLiftDistanceFollowsTheFalloff` | each is the other's control | 11 % above and 12 % below 39.2 N | lifts: as M1; stays: as M2 |
| M6 | Holding limit: a 4 kg Weight placed with its eye touching the pole is held (62.1 N against 39.2 N), an 8 kg Weight is not (41.3 N against 78.5 N). Optional closer pair: 5 kg held, 6 kg not | `TheHoldLimitFollowsForceAndWeight` | each is the other's control | 58 % above and 47 % below; the optional pair 16 % above and 12 % below | held: as M1; not held: on the bench within 3 s |
| M7 | A Battery of 60 J at `power` 10 W. Then the Weight is held and later drops | `AnExhaustedBatteryReleasesTheLoad` | 3600 J: still held at 20 s | — | drop between 3 s and 12 s (derived 6 s; the window excludes "never draws" and "drains at once") |
| M8 | A magnet rotated 90° pulls along its turned axis; a Weight beside the unrotated magnet outside the 0.5 m region is not moved | `TheFieldFollowsThePoleAxis` | M1 | region radius ± 0.1 m | as M1 and M2 |
| M9 | Wrong-kind connection: an activation wire into `PowerIn` is refused | S4 row R5 once delivered, else `AnActivationWireIntoThePowerInputIsRefused` (pattern `MotorTests.cs:316`) | the recipe's wire is admitted | — | exact refusal |
| M10 | Each parameter outside its range or non-finite is refused at every seam; bounds admitted | S4 row R2 once delivered, else `AnOutOfRangeParameterIsRefused` (pattern `MotorTests.cs:1044`) | bounds admitted | one f32 step outside | exact refusal |
| M11 | Run, Reset, save and load mid-hold restore the Weight's pose, the Battery charge, parameters and wire | `ResetAndSaveLoadRestoreTheHeldLoadAndCharge`; placed alone: `CatalogueIntegrationTests.cs:273` | — | — | authored state as the generic test compares it |
| M12 | Sorting task: a Weight and a Bowling ball roll under a supplied magnet; only the Weight is removed from the stream | `OnlyTheRespondingBodyIsPickedFromAStream` | unsupplied: both continue | — | exact: which body ends on the pole; guard 10 s |
| M13 | The Weight's own tests still pass with the responding property declared, and a Weight with no magnet in the scene behaves as before | existing `WeightTests` unchanged; `AWeightWithoutAFieldIsUnaffected` | — | — | reuses `WeightTests` tolerances |

## 6. Open questions

**Resolved**

1. Which catalogue bodies respond? The Weight only among delivered parts (an iron weight); the Cannonball EL-091 declares the property in its own slice if that follows this one, or here if it is already delivered. Decided now: one clear responder keeps the first lesson unambiguous.
2. Hold by field plus contact, or a joint on touch? Field plus contact: D19 (the spec's proposal). A joint would be a second rule for the same behaviour.
3. Force law shape? Linear falloff as proposed is the starting law (D1). The shared model is fixed once in the `ENGINE-FIELD-FORCE` Code Map for every consumer, as that story's shared acceptance requires; this spec's numbers are recomputed if it chooses another shape.
4. Occlusion by intervening bodies? None: D19.
5. Cylinder or box housing? Decided now: box, so no new static shape needs proof.
6. Keep the `power` draw? Yes: D19 (the proposal), with the draw owned by the engine story.
7. `Supply` pass-through? Dropped: D19 (no descriptor relays it and no row uses it).

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Battery (delivered), Weight (delivered, edited), Bowling ball and Basketball (delivered controls), electrical gates and the Pressure plate as switches (delivered). Port kinds agree: a `Supply` (Electrical, Output) into `PowerIn` (Electrical, Input), 12 V.
- **Sorting task** [sequence-task-371](../requirements.md#sequence-task-371) and [sequence-task-393](../requirements.md#sequence-task-393): "pickup/drop requires the declared power/material" is rows M1–M4 and M12.
- **Campaign.** First use levels 41–50, reuse 61–100, 111–120 and 136–150 (`docs/planning/invest/campaign-reservations.md:129`). The material-sorter lesson is in levels 91–100 (`campaign-reservations.md:75`).
- **S8.** A lesson that places the magnet needs the lesson-fixture story S8 first.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `electromagnet` |
| Named identity | EL-060, `../invest/named-elements.md#element-060` |
| Acceptance | `todo-320` and `sequence-task-613` |
| Decision | S642; `scope-corrections.md` has no `s642` block |
| Mode records | `Configuration=Adjustable` (it declares parameters; confirmed at slice planning) |

What it lacks today: an `s642` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-320 anchor is a scope index, not immediately followed by its sequence-task-613 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element, after its engine story. If that story delivers everything in section 3, this slice is declaration data, art, one edit to the Weight and tests.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-ISLANDS-SLEEP and ENGINE-SUPPLY-DEMAND; then ENGINE-FIELD-FORCE (its unified text, including the supply-switched source with payment kind StandingDraw and the responding property declared on a body by one rule); then S7 (the coverage follow-on, which widens the reader, owner decision 15); S3 optional.
- **Files added.** `elements/Electromagnet/Electromagnet.csproj`, `ElectromagnetElementDeclaration.cs`, `electromagnet.tscn`, `icon.svg`; `CuriousContraptions.tests/ElectromagnetTests.cs`, `ElectromagnetArtTests.cs`.
- **Files edited.** `elements/Weight/WeightElementDeclaration.cs` (responding property) and `CuriousContraptions.tests/WeightTests.cs` (row M13); no test-project edit (published main compiles every top-level `*.cs` by one rule); until S3 `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703` and `CuriousContraptions.tests/PackageRuntimeParityTests.cs:483`, `:641` (generic list `:653`); at delivery the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Data-only if the engine story has published every capability with tests; engine tier if any row of section 3 is still untested, narrower or missing at planning.
- **Disqualifiers the probe must re-check.** The story delivered a supply-switched source and a draw, not only a signal-switched one; the responding property is declarable on a body whose mass frame is parameter-bound (the Weight's is, `WeightElementDeclaration.cs:96`); the chosen falloff still holds 4 kg and refuses 8 kg at the defaults, otherwise the defaults are re-derived, not the tests widened.
