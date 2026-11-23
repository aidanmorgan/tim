# EL-188 · Bowling ball — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-188 delivery slice](../invest/named-elements.md#element-188). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text, checked against `elements/BowlingBall/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and are source facts. The delivered declaration is recorded once, in [CAT-014](CAT-014-bowling.md); this spec states what the named identity adds and what remains open.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-188 · Bowling ball · Gravity |
| Anchor | [requirements.md#element-188](../requirements.md#element-188) (task `sequence-task-741`); [named-elements entry](../invest/named-elements.md#element-188); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Owner key / slice | S295; `named-el-188` |
| Refines | [CAT-014 bowling](CAT-014-bowling.md) ([requirement](../requirements.md#current-cat-014)). Contrasts: [EL-187 Basketball](EL-187-basketball.md), [EL-189 Tennis ball](EL-189-tennis-ball.md) |
| Row and outcome | "Heavy spherical cargo loads supports according to actual mass." Outcome: "Its visual size alone cannot supply added work." |
| Status | Delivered through package `bowling`. Further work is blocked on the engine programme (owner decision 3a), including `ENGINE-CAPABILITY-INSTANCES` (payload contract). The identity stays open on one tests-only row, A4 (Disposition) |

### Disposition

**Refinement of the delivered package `bowling`** (`elements/BowlingBall/`, palette order 10, `BowlingBallElementDeclaration.cs:23`). It gets no package, catalogue entry or palette order of its own. Its own outcome is executed today (row A1).

Nothing is deliverable now: every step is **blocked on the engine programme (owner decision 3a)**. The tests-only row A4 and the closure review run after that programme (owner decision 22, 23 November 2026).

The ledger entry also retains the CAT-014 acceptance, of which one item is open and one is decided:

1. **Lever loading against a matched Basketball** (CAT-014: "Domino/lever loading against a matched Basketball control"). The Domino half is executed; the lever half has no Basketball striker. Tests only (row A4); no capability is missing.
2. **Buoyancy** (CAT-014 lists the field). For this identity it is the declared ambient lift (owner decision 24, 23 November 2026), and the field is delivered: the ball declares an ambient load with zero lift (`BowlingBallElementDeclaration.cs:61`–`:62`). Sinking in a liquid is acceptance of [ENGINE-LIQUID](../../../_bmad-output/planning-artifacts/epics.md#story-engine-liquid-conserved-liquid-inventory-head-driven-flow-and-immersion), which cites this part (row A6); it does not hold this identity open.

After row A4 the identity has no delta from `bowling` and closes by a recorded check in the "Cargo" closure review ([delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures)).

## 2. Declaration

Delivered and unchanged by this identity; every value and line is in [CAT-014 §2](CAT-014-bowling.md#2-declaration). In short: one dynamic body, mass 4 kg, one 0.28 m sphere collider, solid-sphere moments 0.12544 kg·m² (0.4 × 4 × 0.28²), gravity (0, −9.81, 0) m/s² (`elements/BowlingBall/BowlingBallElementDeclaration.cs:53`–`:59`), one ambient load of linear drag 0.04 1/s with zero lift (`:61`–`:62`), one airflow receiver, payload policy `CaptureTarget` (`:51`), no parameter, port, store or event. The owner set 0.28 m and 4 kg on 9 October 2026: heavier and smaller than the Basketball.

**Material, as declared data.** Restitution 0.14, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s (`BowlingBallElementDeclaration.cs:40`), the four fields of `ContactMaterialDeclaration` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:133`–`:143`). There is no density field: "heavy" is the declared 4 kg in the mass frame, not a density times the drawn volume, which is exactly what the outcome asks for. No strength, thermal or optical property is needed or present.

### Art and solidity

One art node, `Visual/Visual_Sphere`, a 0.28 m sphere equal to its collider `col_0_0`, moved only by the body's pose. No presentation binding. The hand table agrees (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:110`); `spec-engine-art-solidity.md` lists no finding. At a glance: the only delivered ball with no seam or band, plain blue (`#45639c`), and smaller than the Basketball.

## 3. Engine capabilities

### Capability probe

Probe of 22 November 2026. "Opened" means the test body was read for this spec; the others are cited from CAT-014's probe and confirmed by name and line.

| Capability the row needs | Status | Symbol | Test |
| --- | --- | --- | --- |
| Declared mass and moments decide momentum transfer, independent of radius | present | the shared contact solver (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`) | `WorkshopPhysicsEngineTests.RollingStrikeTopplesTheDominoOnlyWithTheHeavierMass` (`CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs:710`, opened) |
| Sphere contact with declared restitution | present | `WorkshopCollisionKernels.CollideSpherePlane` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:325`) | `WorkshopPhysicsEngineTests.DroppedFromThreeMetresEachBallReboundsToESquaredHOfItsDeclaredBounce` (`:408`, opened) |
| Loading a hinged beam by impact | present | `CuriousContraptions.Simulation/RevoluteConstraintSolver.cs` | `ImpactLeverTests.AlignedBowlingBallImpact_FlingsPayloadUpward` (`CuriousContraptions.tests/ImpactLeverTests.cs:73`) |
| Rolling resistance and ambient drag, the drag scaled by the atmosphere preset | present | `WorkshopConstraintSolver.cs:231`; `ApplyAmbientLoads` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Gas.cs:95`) | `BalloonTests.ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment` (`CuriousContraptions.tests/BalloonTests.cs:167`, opened, row `bowling`) |
| Airflow acting on declared mass | present | `GatherAirflowBranches` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:69`) | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls` (`CuriousContraptions.tests/FanGuidanceTests.cs:124`, opened): row `Control.Heavy` is not delivered |
| Named goal body captured by a Receiver | present | `DeclaredPhysics.IsPayload` (`engine/gpu/DeclaredPhysics.cs:144`) | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` (`CuriousContraptions.tests/PayloadInstanceTests.cs:236`) |
| Pressures between the two presets | narrower | two scene presets only | owner `ENGINE-FIELD-FORCE`: the one uniform-field law, owner of gravity (coordinator decision 16), whose scene-region slice carries the scene atmosphere ([EL-125](EL-125-authored-atmosphere-preset.md) is the identity that needs it); not required by this identity's row |
| Buoyancy in a liquid | missing | the declared ambient lift is zero and no liquid exists | ENGINE-LIQUID |

Counts: 6 present, 0 untested, 1 narrower, 1 missing.

## 4. Sources and legacy

- **Requirements.** Row and outcome as in section 1. CAT-014: workbench rest and bounce, ball and box momentum transfer, Domino and lever loading against a matched Basketball control.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Material bits: RadiusBits 13435 = 0.28, MassBits 17408 = 4, BounceBits 12411 = 0.14, RollingResistanceBits 10158 = 0.03. | `parts/catalog/bowling.tres@a6c914e:L8-L17` | The decimal values are the delivered f32 values; half-precision storage is not carried. |
| 2 | Drop calibration and bench settle as EL-187, run for bowling too. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81`; `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L51` | Delivered as the rebound law test; impact time and replay identity are not acceptance. |
| 3 | Legacy levels used the Bowling ball as the heavy trigger in 30 placements (for example `power_trip`, `domino_effect`). | `content/puzzles.json@a6c914e:L276-L494`, `content/puzzles.json@a6c914e:L1129-L1528` | Lesson references; the content stays. |

Files consulted: `parts/catalog/bowling.tres`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-188](../requirements.md#element-188) and [CAT-014](../requirements.md#current-cat-014). Chrome proof is pending post-catalogue.

### Behaviour and numbers

- **Work comes from mass, not size.** A ball rolling at 0.7 m/s carries momentum m·v and, rolling without slip, kinetic energy 0.7·m·v² (0.5 translation plus 0.2 rotation for a solid sphere). At 4 kg: 2.8 kg·m/s and 1.37 J. At 1 kg: 0.7 kg·m/s and 0.34 J. The delivered theory sets the mass to 1 or 4 kg at each of the two radii (0.34 and 0.28 m): 4 kg topples the Domino at either radius, 1 kg at neither. The radius changes nothing, so drawn size supplies no work.
- **Rebound.** Rest height −0.46 + 0.28 = −0.18 m; a 3 m release falls 3.18 m; first apex 0.14² × 3.18 = 0.062 m (the Basketball's is 0.944 m).
- **Lever.** In the delivered lever scene (`ImpactLeverTests.cs:75`–`:77`, opened) the lever is at y = 3 m, the Basketball payload rests on the arm at centre height 3.46 m, so the arm's top is at 3.46 − 0.34 = 3.12 m, and the striker is released at 6 m. The Bowling ball falls 6 − (3.12 + 0.28) = 2.60 m and arrives with m·g·h = 4 × 9.81 × 2.60 = 102 J. A 1 kg Basketball released from the same point falls 6 − (3.12 + 0.34) = 2.54 m and arrives with 1 × 9.81 × 2.54 = 24.9 J, a quarter of the work. That difference is what row A4 must show.
- **Buoyancy in a liquid (ENGINE-LIQUID's scenario).** At D2's 16 kg/m³ the ball displaces (4/3)π × 0.28³ × 16 = 1.47 kg against its own 4 kg and sinks.
- **Fit with delivered parts.** 0.56 m across is below the Cannon's 0.6 m minimum (Unseated by geometry, decided in CAT-016); it passes the pipe's bore, 1.3 m across; 4 kg presses a default 0.5 kg plate; a default Fan that delivers a Tennis ball does not deliver it.

### Acceptance rows

| # | Given / When / Then | Test | Control and boundary | State |
| --- | --- | --- | --- | --- |
| A1 | A rolling strike topples a Domino only with the heavier mass, at either radius | `WorkshopPhysicsEngineTests.RollingStrikeTopplesTheDominoOnlyWithTheHeavierMass` | 1 kg rows at both radii; maximum tilt above 60° only for 4 kg | executed |
| A2 | Dropped on the bench it barely rebounds and rests at 0.28 m | `WorkshopPhysicsEngineTests.DroppedBowlingBallBarelyReboundsAndRestsAtItsOwnRadius` (`:393`); `DroppedFromThreeMetresEachBallReboundsToESquaredHOfItsDeclaredBounce` | the Basketball row of the same theory | executed |
| A3 | It loads a lever and flings the payload | `ImpactLeverTests.AlignedBowlingBallImpact_FlingsPayloadUpward` | `DepthMissControl_PayloadRemainsUndisturbed`; `PivotHitControl_PayloadRemainsUndisturbed` | executed |
| A4 | A Basketball striker in the same lever scene flings the payload visibly lower | planned `ImpactLeverTests.ABasketballStrikerFlingsThePayloadLowerThanTheBowlingBall` (defined in [CAT-014 §5](CAT-014-bowling.md#5-acceptance-outline): highest point differs by at least 0.3 m; two runs with one striker agree within 0.05 m; 2 s per run) | as defined there | planned, tests only; blocked on the engine programme (owner decision 3a) and run after it (owner decision 22) |
| A5 | A stream that delivers a light ball does not deliver it | `FanGuidanceTests.ElectricalConeGuidesFallingTennisToReceiverWithMatchedControls`, row `Control.Heavy` | `Control.Supplied` delivers the Tennis ball; 10 s stall guard | executed |
| A6 | In a liquid it sinks while a Basketball floats | planned with [EL-187](EL-187-basketball.md) row A5, one test for both identities | empty vessel | blocked on ENGINE-LIQUID; it is ENGINE-LIQUID's own acceptance and does not hold this identity open (owner decision 24) |
| A7 | Reset, save and load, parameter refusal, wrong-kind connection | `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload`; `WorkshopBowlingTests.SaveRoundTripsBothKindsAsMaterialFreeRecordsAndRefusesAForgedMaterialOrParameter`; no port exists | forged material refused | executed |
| A8 | Its lesson solves, and not with one cause missing | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal` (`burst_of_motion`) | 60, 120 and 240 Hz | executed |

### Integration

- **Collaborators, all delivered:** Domino, Receiver, Impact lever, Impact switch, Pressure plate, Basketball (control), Bumper, Fan (heavy control), Balloon (it ruptures one in `burst_of_motion`), pipe family. Audit [sequence-task-285](../requirements.md#sequence-task-285); interaction rows [IX-01](../requirements.md#interaction-01), [IX-02](../requirements.md#interaction-02), [IX-03](../requirements.md#interaction-03).
- **Campaign.** `burst_of_motion` is Playable; `power_trip` and `cold_start` are Planned. First use 1–10; reuse 21–30, 41–50, 91–100, 136–150.
- **Coverage binding.** The catalogue key `bowling` in `docs/coverage/catalogue-elements.json`. A refinement has no named-package binding of its own.
- **Lesson fixture (S8).** Not needed.

### Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-SENSOR-VOLUMES` and `ENGINE-ISLANDS-SLEEP` (they change the contact, capture and resting paths the rows exercise) and `ENGINE-CAPABILITY-INSTANCES` (the payload contract, row A7); ENGINE-LIQUID for row A6 only.

Every step runs after the engine programme (owner decision 22, 23 November 2026), against its stories as delivered; the order:

1. Row A4: a tests-only patch from CAT-014's residual list; data-only review tier.
2. The closure check in the "Cargo" review: the ledger entry cites `bowling` and rows A1 to A5, A7 and A8 (buoyancy is the declared ambient lift, owner decision 24).
3. Row A6: with EL-187's row, after ENGINE-LIQUID. It is ENGINE-LIQUID's acceptance and does not hold the closure.

Disqualifiers: a change to the declared mass, radius or material; a Cannon window change that would seat a 0.56 m ball; `ENGINE-CAPABILITY-INSTANCES` changing which bodies a Receiver admits (section "Engine conformance", point 1).

## 6. Open questions

Resolved:

- **Configuration fields.** Fixed per package; no per-instance material (C-Balls, 22 October 2026).
- **Named goal body.** A goal names a body by identity and a Receiver captures either kind (row A7's sibling test, probe row 6). No decision is needed.
- **No band.** The banded-ball decision of 21 November 2026 applies to the ball-presets family; the plain surface is what tells this ball apart (Batch A, standing delegation).
- **Buoyancy.** The declared ambient lift (owner decision 24, 23 November 2026). This identity closes with row A4; sinking in a liquid is `ENGINE-LIQUID`'s own acceptance (row A6). EL-187 and EL-189 close the same way.
- **Tests-only patches and closure reviews before the engine programme.** They wait for it (owner decision 22, 23 November 2026): row A4 and the closure review run after the programme.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Against the owner's addendum of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One declaration class, one scene, one icon. A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions projects for `bowling`, "Bowling" and the class name found no code or comment that names it. One refactor is owed, by a shared contract the part uses and not by the part: its `PayloadPolicy.CaptureTarget` (`BowlingBallElementDeclaration.cs:51`) is the product role that `BodyTargetKind.AllPayloads` builds into the target filter (`CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:31`, `:43`; engine design audit finding E12). `ENGINE-CAPABILITY-INSTANCES` replaces it with a declared participation mask; this package's declaration changes in that slice and row A7 is re-run. The part declares no gas store, so the sphere-only gas coupling (finding E10) does not touch it.
2. **Generic laws.** Topples and loads: contact impulse between bodies, the difference being the declared mass. Barely rebounds: restitution and bounce threshold. Rolls and stops: friction and rolling resistance. Hardly moved by a stream: the airflow law acting on 4 kg. Sinks (ENGINE-LIQUID): the immersion law.
3. **Three layers.** Colliders: one hidden sphere. Visuals: one sphere mesh. Animation: none.
4. **One-way flow and typing.** Nothing reads art; values are f32; typed `PartId`.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (sphere); joints, drives, sensors, domain nodes, stores, ports and presentation bindings 0; airflow receivers 1 (4 samples); ambient loads 1.
6. **Performance.** One primitive, the minimum. Tunnelling is excluded by the engine's speculative sphere contact and the 64 m/s limit. Engine gaps, not element faults, each owned inside the engine programme (owner decision 3a): island sleeping does not exist, so a resting ball is stepped every tick (`ENGINE-ISLANDS-SLEEP`); every residence sensor tests every payload each tick (`ENGINE-SENSOR-VOLUMES`, candidates from `ENGINE-SPATIAL-QUERIES`); contact triggers and loads that this ball sets off on other parts are found by scanning rows (`ENGINE-CONTACT-OBSERVATIONS`); airflow occlusion walks every collider per sample line (`ENGINE-SPATIAL-QUERIES`).
7. **Tests are scenarios.** Rows A1 to A8 run the production simulation; none reaches a ball-specific path.

**Verdict: Conforms**, with the E12 contract refactor owed by `ENGINE-CAPABILITY-INSTANCES`; blocked on the engine programme (owner decision 3a).
