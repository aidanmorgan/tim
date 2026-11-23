# EL-187 · Basketball — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-187 delivery slice](../invest/named-elements.md#element-187). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text, checked against `elements/Ball/` and its tests in checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4 and are source facts. The delivered declaration is recorded once, in [CAT-001](CAT-001-ball.md); this spec states what the named identity adds to it and what remains open.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-187 · Basketball · Gravity |
| Anchor | [requirements.md#element-187](../requirements.md#element-187) (task `sequence-task-740`); [named-elements entry](../invest/named-elements.md#element-187); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Owner key / slice | S294; `named-el-187` |
| Refines | [CAT-001 ball](CAT-001-ball.md) ([requirement](../requirements.md#current-cat-001)). Contrasts: [EL-188 Bowling ball](EL-188-bowling-ball.md), [EL-189 Tennis ball](EL-189-tennis-ball.md) |
| Row and outcome | "Spherical cargo uses declared mass, radius and compliant contact properties." Outcome: "Equal drop height produces reproducible material-dependent rebound." |
| Status | Delivered through package `ball`. Further work is blocked on the engine programme (owner decision 3a), including `ENGINE-CAPABILITY-INSTANCES` (payload contract). The identity stays open on one tests-only row, A4 (Disposition) |

### Disposition

**Refinement of the delivered package `ball`** (`elements/Ball/`, palette order 1, `BallElementDeclaration.cs:20`). It gets no package, catalogue entry or palette order of its own. Its own outcome is executed today (section 5, row A1).

Nothing is deliverable now: every step is **blocked on the engine programme (owner decision 3a)**. The tests-only row A4 and the closure review run after that programme (owner decision 22, 23 November 2026).

The ledger entry also retains the CAT-001 acceptance, which lists buoyancy as a material field. For this identity buoyancy is the declared ambient lift (owner decision 24, 23 November 2026), and that field is delivered: the ball declares an ambient load whose lift is zero (`BallElementDeclaration.cs:58`–`:59`), under the same ambient-load law the Tennis ball and the Balloon use with non-zero lift. The identity therefore has no delta from `ball` and closes with row A4 by a recorded check in the "Cargo" closure review ([delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures)). Floating in a liquid is acceptance of [ENGINE-LIQUID](../../../_bmad-output/planning-artifacts/epics.md#story-engine-liquid-conserved-liquid-inventory-head-driven-flow-and-immersion), which cites this part (row A5); it does not hold this identity open.

The CAT-001 row's "R-N3 Fail" reference is retired (owner decision, 22 November 2026) and no longer holds the identity open; the clause is struck from the requirement row with the plan-document updates.

## 2. Declaration

Delivered and unchanged by this identity; every value and line is in [CAT-001 §2](CAT-001-ball.md#2-declaration). In short: one dynamic body, mass 1 kg, a 0.34 m sphere collider plus a 0.014 m torus for the seam, solid-sphere moments 0.04624 kg·m² (0.4 × 1 × 0.34²), gravity (0, −9.81, 0) m/s² (`elements/Ball/BallElementDeclaration.cs:50`–`:56`), one ambient load of linear drag 0.04 1/s with zero lift (`:58`–`:59`), one airflow receiver, payload policy `CaptureTarget` (`:48`), no parameter, port, store or event.

**Material, as declared data.** The scene body carries restitution 0.55, friction 0.3, rolling resistance 0.035 and bounce threshold 0.1 m/s (`BallElementDeclaration.cs:38`). These are the four fields the engine's contact material has (`ContactMaterialDeclaration`, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:133`–`:143`: restitution 0 to 1, rolling resistance 0 to 0.1). The material model has no density, strength, thermal or optical property today, and this identity's row asks for none of them. The package declares no `MaterialParameterBinding`: the values are fixed per package (C-Balls decision, 22 October 2026).

### Art and solidity

Two art nodes, both moved only by the body's pose: `Visual/Visual_Sphere` (solid, collider `col_0_0`) and `Visual/Visual_Stripe` (a torus 1 cm proud, collider `col_visual_stripe`). No presentation binding. The hand table agrees (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:109`). `_bmad-output/implementation-artifacts/spec-engine-art-solidity.md` lists no finding for `ball`. At a glance: the only ball with one dark tilted seam on orange (`#f57d38`).

## 3. Engine capabilities

### Capability probe

Probe of 22 November 2026. "Opened" means the test body was read for this spec; the others were confirmed by name and line and are cited from CAT-001's probe.

| Capability the row needs | Status | Symbol | Test |
| --- | --- | --- | --- |
| Sphere contact with declared restitution; pair value is the product | present | `WorkshopCollisionKernels.CollideSpherePlane` (`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:325`) | `WorkshopPhysicsEngineTests.DroppedFromThreeMetresEachBallReboundsToESquaredHOfItsDeclaredBounce` (`CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs:408`, opened) |
| Declared mass and moments decide momentum transfer | present | the shared contact solver (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`) | `WorkshopPhysicsEngineTests.RollingStrikeTopplesTheDominoOnlyWithTheHeavierMass` (`:710`, opened) |
| Friction and rolling resistance at each contact | present | `WorkshopConstraintSolver.cs:231` | `WorkshopPhysicsLawTests.EachBallRollingAt0Point7DeceleratesAtTheRollingLawAndRestsWithoutBackwardTravel` |
| Ambient drag scaled by the scene's atmosphere preset | present for the two presets | `WorkshopPhysicsEngine.ApplyAmbientLoads` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Gas.cs:95`); `AtmospherePreset` (`CuriousContraptions.Abstractions/AtmosphereBoundary.cs:6`) | `BalloonTests.ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment` (`CuriousContraptions.tests/BalloonTests.cs:167`, opened): drag 0.04 in Standard, 0 in Vacuum, for `ball` and `bowling` |
| Second (torus) collider on the dynamic body | present | `WorkshopHollowKernels` (`CuriousContraptions.Simulation/WorkshopHollowKernels.cs:13`) | `CompoundMassAndRollingTests.BasketballSeamMustClearBeforeRearmingAndStaticPlaneReadsAreExcluded` |
| Fast-sphere contact without tunnelling | present | `AdvanceSpeculativeSphereContact` (`WorkshopCollisionKernels.cs:1940`) | `PayloadInstanceTests.APayloadDrivenIntoTheBenchAtFortyMetresPerSecondNeverTunnels` |
| Reset and reload restore kind and pose | present | generic lifecycle | `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload` (`CuriousContraptions.tests/PayloadInstanceTests.cs:249`) |
| Payload policy read by other parts' sensors | present | `DeclaredPhysics.IsPayload` (`engine/gpu/DeclaredPhysics.cs:144`) | `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` (`:236`) |
| Pressures between the two presets, or a pressure that varies in space | narrower | the atmosphere is one of two scene presets | owner `ENGINE-FIELD-FORCE`: the one uniform-field law, owner of gravity (coordinator decision 16), whose scene-region slice carries the scene atmosphere ([EL-125](EL-125-authored-atmosphere-preset.md) is the identity that needs it); not required by this identity's row |
| Buoyancy in a liquid | missing | the declared ambient lift is zero and no liquid exists | ENGINE-LIQUID |

Counts: 8 present, 0 untested, 1 narrower, 1 missing. Nothing here needs code that names the part.

## 4. Sources and legacy

- **Requirements.** Row and outcome as in section 1. CAT-001: rest, boundary and two-height controls; ramp, pipe and Receiver integrations; the stripe follows the committed pose.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The catalogue stored the material as half-precision bit patterns (RadiusBits 13681 = 0.34, …). | `parts/catalog/ball.tres@a6c914e:L8-L17` | The decimal values are the delivered f32 values; half-precision storage is not carried. |
| 2 | Isolated drop at zero pressure: first impact at √(2h/g) ± 0.02 s; rebound height ratio bounce² ± 0.015 (0.3025 for 0.55); identical replay after Restore. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81` | The rebound ratio is the delivered law test (row A1, 15% band). The impact time and replay identity are not acceptance. |
| 3 | Dropped on the bench it bounces, never sinks more than 1 mm, settles below 0.05 m/s and never escapes. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L51` | Delivered (row A1 asserts rest within 1 mm and below 0.02 m/s). |
| 4 | Legacy drag scaled by world pressure. | `reference/cpu/MachineWorld.cs@a6c914e:L877-L883` | Delivered as the ambient load scaled by the atmosphere preset (probe row 4). |

Files consulted: `parts/catalog/ball.tres`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`, `reference/cpu/MachineWorld.cs`.

## 5. Acceptance outline

Point of truth: [element-187](../requirements.md#element-187) and [CAT-001](../requirements.md#current-cat-001). Chrome proof of every row is pending post-catalogue.

### Behaviour and numbers

- **Rebound.** A sphere released at rest from centre height 3 m falls to its rest height, bench surface −0.46 m plus radius 0.34 m = −0.12 m, so h = 3.12 m. The bench is neutral (restitution 1), so the pair restitution is 0.55 and the first apex is e²h = 0.3025 × 3.12 = 0.944 m above rest. The delivered test accepts ±15% (0.802 to 1.085 m). The Bowling ball in the same theory gives 0.14² × 3.18 = 0.062 m. The two bands cannot overlap, so a swapped material fails; that is "material-dependent".
- **Reproducible.** Reset restores the authored pose and zero velocity, and a second Run falls again. No trajectory or tick is pinned.
- **Buoyancy in a liquid (ENGINE-LIQUID's scenario).** Under decision D2 the liquid density is 16 kg/m³. The ball's volume is (4/3)π × 0.34³ = 0.1646 m³, so fully immersed it displaces 0.1646 × 16 = 2.63 kg against its own 1 kg and floats, with 1/2.63 = 38% of its volume under the surface. The Bowling ball displaces (4/3)π × 0.28³ × 16 = 1.47 kg against 4 kg and sinks.
- **Fit with delivered parts.** 0.68 m across (0.70 m at the seam) fits the Cannon's 0.6 to 0.84 m window, the pipe's bore, 1.3 m across and the Receiver's 1.32 m interior; 1 kg presses a default 0.5 kg Pressure plate.

### Acceptance rows

| # | Given / When / Then | Test | Control and boundary | State |
| --- | --- | --- | --- | --- |
| A1 | Dropped from 3 m, the first rebound apex is e²h of the declared bounce and the ball then rests at its radius | `WorkshopPhysicsEngineTests.DroppedFromThreeMetresEachBallReboundsToESquaredHOfItsDeclaredBounce` | the Bowling ball row; the test first asserts the two bands are disjoint; rest within 1 mm, speed below 0.02 m/s; 6 s stall guard | executed |
| A2 | After Reset and after reload the same fall happens again | `PayloadInstanceTests.BothKindsFallAgainAfterExactResetAndReload` | both ball kinds | executed |
| A3 | Declared mass, not size, decides what it can topple | `WorkshopPhysicsEngineTests.RollingStrikeTopplesTheDominoOnlyWithTheHeavierMass` | 1 kg leaves the tile standing at either radius | executed |
| A4 | Two drop heights in one Run: the lower ball lands first and the higher one rebounds higher | planned `PayloadInstanceTests.TwoDropHeightsGiveTheExpectedImpactOrderAndHigherFirstRebound` (defined in [CAT-001 §5](CAT-001-ball.md#5-acceptance-outline): centres at y 2 and 4 m, apex difference at least 0.3 m, control both at 2 m within 0.05 m, 10 s stall guard) | as defined there | planned, tests only; blocked on the engine programme (owner decision 3a) and run after it (owner decision 22) |
| A5 | Released under a liquid surface it rises and floats; a Bowling ball beside it sinks | planned in the first slice after ENGINE-LIQUID that floats a body: `BuoyancyTests.ABasketballFloatsAndABowlingBallSinksInTheSameVessel`. Then the Basketball's centre settles above the surface minus its radius and the Bowling ball rests on the vessel floor. Control: with the vessel empty both rest on the floor. Stall guard 20 s. Tolerance 0.05 m on the floating height (a seventh of the radius; the outcome is float or sink, not a draught) | empty vessel | blocked on ENGINE-LIQUID; it is ENGINE-LIQUID's own acceptance and does not hold this identity open (owner decision 24) |
| A6 | Parameter refusal, wrong-kind connection, Reset and save/load of the package | generic and delivered: `PayloadInstanceTests.ABareGenericPayloadRecordLoadsAndAnyParameterIsRefused`; the part has no port (`WorkshopWallTests.WallPopulationPuzzleAndForeignPortsReject` shows a port-free part refusing a wire) | — | executed |

Every other CAT-001 row (rolling, tunnelling, seam, collaborators) is listed with its test in [CAT-001 §5](CAT-001-ball.md#5-acceptance-outline) and is not repeated.

### Integration

- **Collaborators, all delivered:** Receiver, Ramp, Wall, Domino, Bumper, Springboard, Impact switch, pipe family, Funnel, Ball detector, Pressure plate, Fan, Impact lever, Cannon, Trampoline, Conveyor. Audit [sequence-task-285](../requirements.md#sequence-task-285); interaction rows [IX-01](../requirements.md#interaction-01), [IX-02](../requirements.md#interaction-02), later [IX-10](../requirements.md#interaction-10) buoyancy.
- **Campaign.** 14 of the 15 Playable lessons place it; first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign element coverage](../requirements.md#campaign-element-coverage)).
- **Coverage binding.** The catalogue key `ball` in `docs/coverage/catalogue-elements.json`. A refinement has no named-package binding of its own.
- **Lesson fixture (S8).** Not needed.

### Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-SENSOR-VOLUMES` and `ENGINE-ISLANDS-SLEEP` (they change the contact, capture and resting paths rows A1 to A4 exercise) and `ENGINE-CAPABILITY-INSTANCES` (the payload contract, rows A2 and A6); ENGINE-LIQUID for row A5 only.

Every step runs after the engine programme (owner decision 22, 23 November 2026), against its stories as delivered; the order:

1. Row A4: a tests-only patch owned by CAT-001's residual list; data-only review tier.
2. The closure check in the "Cargo" review: the ledger entry cites `ball` and rows A1 to A4 and A6 (buoyancy is the declared ambient lift, owner decision 24).
3. Row A5: inside the first slice after ENGINE-LIQUID that floats a body; the engine tier belongs to the engine story, the row itself is a scenario. It is ENGINE-LIQUID's acceptance and does not hold the closure.

Disqualifiers: a change to the declared material, radius or seam; ENGINE-LIQUID publishing with a density other than D2's; the ball-presets family changing a shared payload rule; `ENGINE-CAPABILITY-INSTANCES` changing which bodies a Receiver admits (section "Engine conformance", point 1).

## 6. Open questions

Resolved:

- **Material fields as configuration.** Fixed per package; no per-instance material (C-Balls, 22 October 2026). A configurable ball is the Tennis ball or the ball-presets family, not this part.
- **Pressure modes.** The delivered response is drag scaled by the scene's Standard or Vacuum preset; anything further belongs to the one uniform-field law under `ENGINE-FIELD-FORCE`, owner of gravity (coordinator decision 16), in that story's scene-region slice (D19).
- **R-N3.** Retired (owner decision, 22 November 2026). Its definition was deleted before `a6c914e`, so no test could be derived from it.
- **Buoyancy.** The declared ambient lift (owner decision 24, 23 November 2026). This identity closes with row A4; liquid float is `ENGINE-LIQUID`'s own acceptance (row A5). EL-188 and EL-189 close the same way.
- **Tests-only patches and closure reviews before the engine programme.** They wait for it (owner decision 22, 23 November 2026): row A4 and the closure review run after the programme.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Against the owner's addendum of 22 November 2026, `AGENTS.md` (element-free engine gate, Tri-Graph) and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** The package is one declaration class, one scene and one icon. A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions projects for the id `ball`, "Basketball" and the class name found no code or comment that names it. One refactor is owed, by a shared contract the part uses and not by the part: its `PayloadPolicy.CaptureTarget` (`BallElementDeclaration.cs:48`) is the product role that `BodyTargetKind.AllPayloads` builds into the target filter (`CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:31`, `:43`; engine design audit finding E12). `ENGINE-CAPABILITY-INSTANCES` replaces it with a declared participation mask; this package's declaration changes in that slice and rows A2 and A6 are re-run. The part declares no gas store, so the sphere-only gas coupling (finding E10) does not touch it.
2. **Generic laws.** Fall: gravity on a dynamic body. Bounce and rest: contact with restitution and a bounce threshold. Roll and stop: Coulomb friction and rolling resistance. Air drag: the ambient-load law scaled by the atmosphere preset. Float (ENGINE-LIQUID): the liquid immersion law from displaced volume. Each takes only declared values.
3. **Three layers.** Colliders: a sphere and a torus, hidden `col_` nodes. Visuals: sphere and stripe meshes, never colliders. Animation: none; both art nodes follow the body pose.
4. **One-way flow and typing.** Nothing reads art; values are f32; the id is a typed `PartId` at the bundle boundary.
5. **Flat state.** Bodies 1 (dynamic); colliders 2 (1 sphere, 1 torus); joints, drives, sensors, domain nodes, stores, ports and presentation bindings 0; airflow receivers 1 (4 samples); ambient loads 1.
6. **Performance.** Two primitives; the torus exists because the seam is a visible solid (visible = solid). Tunnelling is excluded by the engine's speculative sphere contact and the 64 m/s speed limit. Engine gaps, not element faults, each with its owner in the engine programme (owner decision 3a): island sleeping is specified (`docs/gpu-f32-physics.md:273`) and does not exist in `CuriousContraptions.Simulation` or `engine/gpu`, so a resting ball is stepped every tick, owned by `ENGINE-ISLANDS-SLEEP`; every residence sensor tests every payload each tick, owned by `ENGINE-SENSOR-VOLUMES` with candidates from `ENGINE-SPATIAL-QUERIES`; airflow occlusion walks every collider per sample line, owned by `ENGINE-SPATIAL-QUERIES`. Nothing in the package is designed around them.
7. **Tests are scenarios.** Rows A1 to A6 run the production simulation; none reaches a ball-specific path.

**Verdict: Conforms**, with the E12 contract refactor owed by `ENGINE-CAPABILITY-INSTANCES`; blocked on the engine programme (owner decision 3a).
