# EL-099 · Pool pocket — named-identity spec

**Current delivery (24 October 2026):** [EL-099 delivery slice](../invest/named-elements.md#element-099). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `path@a6c914e:L…` resolves in git history. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#precision-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-099 · Pool pocket · Specialist |
| Anchors | [requirements.md#element-099](../requirements.md#element-099) (task `sequence-task-652`); [named entry](../invest/named-elements.md#element-099); scope index [todo-227](../requirements.md#todo-227) |
| Proof owner | S669 |
| Disposition | To deliver as a single element. It uses the declaration shape of the delivered Receiver (`basket`) but is not a duplicate: it is a sunk cup whose rim is flush with a table bed, its opening admits only the smallest ball, it has no guiding force region, and its capture volume lies wholly below the rim. |
| Planned package | id `pool_pocket`, directory `elements/PoolPocket/`, class `PoolPocketElementDeclaration` |
| Refines / extends | [CAT-004 Receiver](CAT-004-basket.md) (walls and residence capture); contrasts with [CAT-002 Ball detector](CAT-002-ball_detector.md) (passage only) |
| Status | not started; blocked on the engine programme (owner decision 3a; of it, `ENGINE-SENSOR-VOLUMES` changes this part's sensor declaration), then S7 |

## 2. Declaration

A pocket is a real hole with a real cup under it. A ball is pocketed when it has physically dropped through the opening and come to rest in the cup, which the general residence sensor observes. Because the sensed volume is reachable only through the opening, capture implies passage; no second sensor and no composition rule is needed.

Follow the Receiver (`elements/Receiver/ReceiverElementDeclaration.cs:10`) without its guide: the package implements `IResidenceSensorElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:25`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static`, `SupportedDomains = Mechanical`, `PayloadPolicy.None`, footprint one body, none moving.

The part's origin is the centre of the opening, on the plane of the table bed (y = 0). The cup hangs below it.

### Body and colliders

One static body, five box colliders, each a hidden `col_` node (sizes are full extents in metres):

| Collider | Size | Centre | Note |
| --- | --- | --- | --- |
| Floor | 0.70 × 0.08 × 0.70 | (0, −0.64, 0) | its top is 0.60 m below the rim |
| Walls ±X | 0.12 × 0.60 × 0.70 | (±0.29, −0.30, 0) | tops flush with the bed at y = 0; inner faces at x = ±0.23 |
| Walls ±Z | 0.46 × 0.60 × 0.12 | (0, −0.30, ±0.29) | fit between the X walls; inner faces at z = ±0.23 |

Opening 0.46 × 0.46 m; outer footprint 0.70 × 0.70 m; wall thickness 0.12 m (the Receiver's). **Starting values**, sized against every delivered and planned ball:

| Ball | Width | At the pocket |
| --- | --- | --- |
| Pool ball (EL-098) | 0.40 m | drops in, 0.03 m clear each side |
| Tennis ball | 0.50 m (a plain sphere; its seam is printed, owner decision 24i) | rests on the rim |
| Baseball | 0.52 m (radius 0.26 m, the repair contract's value, coordinator decision 36) | rests on the rim |
| Bowling ball, Soccer ball, Cannonball, Basketball | 0.56–0.68 m | rest on the rim |

The earlier 0.52 m opening equalled the repaired Baseball and let the 0.50 m Tennis ball through with 0.01 m a side; 0.46 m puts every ball of 0.50 m or more 0.04 m or more over the opening. The opening is square so that its outer edge meets Wall slabs without gaps. Material: restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (**starting values**: a dead cup, so a ball that drops in stays in; with a Pool ball the contact restitution is 0.1 × 0.9 = 0.09).

No joints, drives, stores or ports.

### Sensor

One `ResidenceSensorDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:188`), `Target: TargetSelector.AllPayloads` as the Receiver (`ReceiverElementDeclaration.cs:16`):

| Field | Value | Reason |
| --- | --- | --- |
| Volume | minimum (−0.23, −0.60, −0.23), maximum (0.23, −0.20, 0.23) | the cup's interior from its floor to 0.20 m below the rim |
| Occupancy rule | centre in volume | today's rule: the body's position point is tested (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:929`–`:934`). `ENGINE-SENSOR-VOLUMES` makes occupancy shape overlap "as the declaration states"; this sensor is declared centre-in-volume under it |
| Speed limit | parameter `capture_speed_limit` | |
| Dwell | parameter `capture_dwell` | |
| Participation | parameter `capture_participation` | required by the bundle validator (`CuriousContraptions.Bundle/BundleValidator.cs:634`–`:639`) |
| Upper margin | fixed 0 | there is no `capture_margin` parameter: nothing may raise the volume toward the rim |

The volume's top is set so that both occupancy readings agree for every single ball; a second Pool ball stacked on the first (A5) overlaps the volume by up to 5 mm with its centre 0.2 m above it (its centre at −0.40 + √(0.40² − 0.06²) = −0.0045 at the full 0.06 m of sideways play, its bottom at −0.2045), so only the declared centre rule keeps it out. A Pool ball resting on the floor has its centre at y = −0.40 and spans −0.60 to −0.20: inside by its centre and overlapping by its shape. A ball too wide for the opening rests on the four inner edges, 0.23 m from the opening's centre, and dips below the rim by r − √(r² − 0.23²): 0.152 m for r = 0.25 (Tennis ball), 0.139 m for 0.26 (Baseball), 0.090 m for 0.34 (Basketball). The deepest, 0.152 m, is 0.048 m above the volume's top at −0.20, so a ball resting on the rim is outside the volume by its centre and by its shape.

Event: `SimulationEventKind.Captured`, as the Receiver (`ReceiverElementDeclaration.cs:63`).

### Parameters

| Key | Kind | Unit | Options | Default | Minimum | Maximum | Editor policy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `capture_speed_limit` | Real | m/s | — | 1.5 | 1/1024 | 64 | Reject |
| `capture_dwell` | Real | s | — | 0.2 | 1/1024 | 30 | Reject |
| `capture_participation` | Choice | none | `Disabled`, `Enabled` | `Enabled` | — | — | Reject |

Names, units and ranges are the Receiver's (`ReceiverElementDeclaration.cs:44`–`:46`), except that `capture_dwell` defaults to 0.2 s (the dead cup settles a ball sooner than the Receiver's 0.35 s).

### Metadata and bindings

Category `Goals`; display name "Pool pocket"; description "A hole with a cup under it. A ball is pocketed only when it has dropped in and settled."; colour (0.29, 0.67, 0.58) (the Receiver's, `#4aab94`); pick radius 0.6; palette order 201 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Presentation: `Visual/Rim` brightens to white on the `Captured` event with the Receiver's halo binding form (`ReceiverElementDeclaration.cs:26`–`:31`: `PresentationDriver.Event`, `SmoothStep`, 0.5 s).

### Art and solidity

No delivered package uses a texture, so every colour is its own mesh.

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/WallLeft`, `Visual/WallRight`, `Visual/WallFront`, `Visual/WallBack` | warm wood `#c28f52` boxes of the collider sizes | solid, one box collider each | nothing (static) |
| `Visual/Floor` | navy `#293954` box of the collider size | solid, its box collider | nothing |
| `Visual/Rim` | four pale green `#bdf4bd` strips 0.04 m wide set flush in the walls' top faces around the opening (coplanar with y = 0; the Receiver's capture colour) | decorative: flush | nothing; recoloured by the event binding |

Nothing stands above the bed plane. Icon: a square hole seen from above with a ball half-way in. It differs from the Receiver at a glance by being sunk flush with a surface, with no standing walls, lowered front lip or halo ring.

### Behaviour and numbers

Laws: contact and friction for the walls and floor; gravity on the ball; the general residence rule (a payload inside the volume, slower than the limit, for the dwell, commits one capture, latched until Reset).

- **Dropping in.** A ball crossing the 0.46 m opening at speed v is unsupported for about 0.46 ÷ v seconds and falls ½ g t² in that time. At 1 m/s that is 0.46 s, long enough to fall a metre, so it drops straight in. At 4 m/s it falls 0.5 × 9.81 × 0.115² = 0.065 m, meets the far wall's top edge above its own centre line and hops out. A fast ball skipping the pocket is real behaviour and a puzzle lesson; nothing forces it in.
- **Settling.** From bed height the ball's centre falls 0.60 m (from y = +0.20 to y = −0.40), so it meets the floor at √(2 × 9.81 × 0.60) = 3.4 m/s; with restitution 0.09 the first rebound is 0.31 m/s, which rises 5 mm. The ball is under the 1.5 m/s limit immediately after its first contact and is captured about `capture_dwell` later.
- **Beside the opening.** A Pool ball at rest on a wall top or on the bed has its centre at y = +0.20, 0.40 m above the top of the sensed volume, and its lowest point at y = 0. It is never pocketed, whatever its name.
- **Too large.** A Tennis ball set on the opening rests on the inner edges with its centre √(0.25² − 0.23²) = 0.098 m above the rim; a Basketball's centre is √(0.34² − 0.23²) = 0.25 m above it. Neither is in the volume (Sensor, above).
- **Tick order.** Today the residence phase is evaluated after the physics step and its `Captured` event is committed with that tick's state; `ENGINE-SENSOR-VOLUMES` moves the evaluation inside the tick. No signal leaves this part.

Residence sensing is a scene-proportional pass today; `ENGINE-SENSOR-VOLUMES` (with `ENGINE-SPATIAL-QUERIES` for its candidates) owns its cost and its rule. This part declares one bounded volume and designs around nothing.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint (unused), RigidBodyDynamics, SlidingFriction. The map adds: "General pocket geometry/contact plus named-body continuous containment/goal observation; geometry alone does not define capture semantics" (`docs/planning/general-engine-element-map.md:204`). Tests marked † were opened for this revision.

| Capability | Status | Evidence |
| --- | --- | --- |
| Static open container of five box colliders | present | `ReceiverElementDeclaration.cs:54`–`:60`; `PayloadInstanceTests.AReceiverCapturesEitherKindAndAShiftedMissNever` † (`CuriousContraptions.tests/PayloadInstanceTests.cs:236`) |
| Residence sensor over all payloads: capture when inside, never when shifted outside | present | `MechanicalContractValues.cs:188`; the same test † (its shifted rows are the control) |
| Several capture owners, each with its own payload, and Reset replay | present | `CompoundMassAndRollingTests.DistantReceiversCaptureTheirOwnPayloadsWithSparseStateAndResetReplay` † (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:566`) |
| `Captured` event driving a presentation binding | present | `ReceiverElementDeclaration.cs:26`–`:31`, `:63`; exercised only through the Receiver (body of a presentation test not opened) |
| A residence sensor declared without a planar guide | present, untested | `IResidenceSensorElement` and `IPlanarGuideElement` are separate interfaces (`MechanicalContracts.cs:25`, `:30`), but the only delivered residence package declares both |
| An upper margin that is a fixed value rather than a parameter | present, untested | the validator takes a `MechanicalValue` (`BundleValidator.cs:631`); the Receiver binds a parameter |
| Occupancy rule stated by the declaration (centre in volume) | narrower | today the centre test is the only rule and is not declared (`WorkshopPhysicsEngine.cs:929`–`:934`); owner `ENGINE-SENSOR-VOLUMES` |
| A level goal that names this part's capture | narrower | today one goal per level of kind Captured, ActivatedAfter or IntegrityBroken (`engine/gpu/WorkshopPuzzle.cs:9`); owned by the goal engine slices (`engine-goal-evaluation` and its siblings). A search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Core` and `CuriousContraptions.Bundle` for the literal `"basket"` found nothing, so capture is not tied to the Receiver's id in code |
| Coverage admits a named-identity package | narrower | owner: coverage follow-on story S7 (section 7) |

Nothing is missing for the part itself.

## 4. Sources and legacy

- Requirement: "Real opening captures cargo through actual passage"; outcome "A ball beside the opening is not pocketed" ([element-099](../requirements.md#element-099)).
- A search at `a6c914e` for "pocket" and "billiard" found no pocket source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Passage is a forward body-centre crossing of a frame-local aperture | `engine/physics/PhysicsPassageSensor.cs@a6c914e:L5-L26` | do not carry forward into this part: capture by residence below the rim already implies passage; a machine that wants a pulse at the mouth places a Ball detector |
| L2 | A standard pipe passes a 0.34 m ball but an oversized 0.8 m ball hits the mouth | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L56-L80` | carry forward as the "too large is blocked by real geometry" control (A4) |
| L3 | Receiver walls 0.12 m thick; residence sensor with dwell and speed limit feeding a capture latch | `engine/gpu/ReceiverGeometry.cs@a6c914e:L8-L19`; `engine/gpu/CaptureLatch.cs@a6c914e:L5-L20` | carry forward; current form at `ReceiverElementDeclaration.cs:12`–`:17` |

**Files harvested:** `engine/physics/PhysicsPassageSensor.cs`, `CuriousContraptions.tests/PipeTests.cs` (oversize test only).

## 5. Acceptance outline

Requirement row: [element-099](../requirements.md#element-099). Planned test class `PoolPocketTests`. The table is a Wall laid flat with a 0.70 m gap that the pocket fills, its rim level with the bed. "Pocketed" means the part's capture latch is Latched for that ball. Until EL-098 is published the rows use a Tennis ball set to radius 0.2 m in place of the Pool ball.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a Pool ball rolling at 1 m/s toward the opening, then it drops in, rests on the cup floor and is pocketed once | `ABallThatDropsInAndSettlesIsPocketed` | A2 | its centre ends within 0.02 m of y = −0.40; one `Captured` event; 5 s stall guard |
| A2 | Given a Pool ball at rest on the bed 0.05 m beside the opening, and another on a wall top, then neither is pocketed during a 5 s run in which A1's ball is | `ABallBesideTheOpeningIsNotPocketed` | A1's ball in the same run | they move less than 1 cm |
| A3 | Given Pool balls crossing the opening at 1, 2, 4 and 6 m/s, then each is pocketed if and only if its centre ends below y = −0.20 inside the cup | `OnlyABallPhysicallyInTheCupIsEverPocketed` (theory) | the 1 m/s row is pocketed | no speed is asserted to skip; the assertion is the equivalence, so the test holds whichever speeds hop out |
| A4 | Given a default Tennis ball (0.50 m) rolled over the opening or set down on it, then it stays on the rim and is not pocketed; likewise a Basketball | `ABallTooLargeForTheOpeningStaysOut` | a Pool ball in the same place drops in and is pocketed | Tennis ball centre at least 0.05 m above y = 0 at rest (predicted 0.098 m); Basketball at least 0.20 m (predicted 0.25 m) |
| A5 | Given a second Pool ball rolled in after the first is pocketed, then it comes to rest on the first, is not pocketed, and the first stays pocketed exactly once | `ASecondBallRestsOnTheFirstAndIsNotPocketed` | the first ball | one `Captured` event in the run; the second ball's centre ends above y = −0.10 (predicted about 0: 0.40 m above the first, less at most 5 mm for the 0.06 m of sideways play) |
| A6 | Given `capture_participation` Disabled, then a ball that drops in is held by the cup and not pocketed | `ADisabledPocketStillHoldsTheBallButReportsNothing` | Enabled | — |
| A7 | Given each Real parameter one step outside its range, `capture_participation` 2, or an added `capture_margin`, in an edit or a save, then it is refused with the typed reason | `ParametersOutsideTheirRangesAreRefused` | the range ends are admitted | — |
| A8 | Given Run, Reset, save and load, then the latch is clear, the balls are back, parameters survive, and a second Run repeats A1 | `ResetSaveAndLoadClearTheLatchAndTheShotRepeats` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` |

A1 and A3 are "captures cargo through actual passage"; A2 is "a ball beside the opening is not pocketed". No ports, so no connection refusal applies.

One pocket holds one settled Pool ball below the volume's top (the cup is 0.60 m deep and a ball is 0.40 m); a second ball stacks above it. That is stated by A5 rather than hidden: a level wanting several balls pocketed uses several pockets.

### Integration

- Works with EL-098 Pool ball (taught with it at 8), Wall as bed and cushions, Bumper (delivered), and EL-097 Pool cue, which the campaign teaches later, at 44a (coordinator decision 36); this part's rows roll the ball in without the cue (A1 to A5). A Ball detector at the mouth gives a pulse where a machine needs one.
- Goal use: a level whose goal is "pocket this ball" needs the goal evaluator to accept this part's capture; a level with several balls, a count or an order needs the goal slices (`engine-goal-evaluation`, `engine-goal-progress-observation`, `engine-goal-ordered-events`, `engine-goal-count-window-hold`). Those belong to the level, not to this slice.
- Campaign: the coverage ledger has no pool-table row; [todo-227](../requirements.md#todo-227) keeps "the original … campaign reservations" without naming levels (`docs/planning/requirements.md:6391`). Owner decision 24c places it in *Bank the Bounce* at **8** (chapter 1): introduction 8, practice 10, reuse 49 and 141 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)).
- A level that fixes a pocket into a table needs the lesson-fixture story S8.

### Delivery

- **Shape.** Single element, after the Pool ball EL-098 (both taught at 8) and before the Pool cue EL-097 (44a), in campaign first-teaching order (owner decision 24a).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element: of it `ENGINE-SENSOR-VOLUMES` fixes the form of this part's sensor (occupancy rule declared, sensor by local id, in-tick evaluation); then S7; EL-098, delivered just before it.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (`TargetSelector.AllPayloads`, the single `ResidenceSensor` property and the fixed `Disabled`/`Enabled` participation Choice).
- **Files.** `elements/PoolPocket/` (project, declaration and `.uid`, `pool_pocket.tscn`, `icon.svg` and its import file); `CuriousContraptions.tests/PoolPocketTests.cs`; one `CuriousContraptions.slnx` line; until S3, the hand lists a new package still edits; the ledger row; the binding section once S7 admits it.
- **Review tier.** Engine tier, because of the two *present, untested* rows; data-only if the probe finds exercising tests.
- **Disqualifiers the probe re-checks.** A residence sensor that cannot be declared without a planar guide or with a fixed upper margin; a sensor story that offers no centre-in-volume declaration; a capture latch or goal reader that assumes one capture owner kind; a part that cannot be placed with its body below its origin plane.

## 6. Open questions

All resolved.

1. **Square opening.** Kept square although Tube and Frustum colliders exist: a square outer edge meets Wall slabs without gaps, and five boxes is the Receiver's count. Decided under the standing delegation.
2. **Which bodies count:** any payload that physically fits and settles (`TargetSelector.AllPayloads`), as the Receiver. Names never enter; size is decided by the opening, which today admits only the Pool ball.
3. **No `ActivationOut` port** (D19): no requirement or lesson uses it, and a Ball detector already gives a passage pulse.
4. **One capture rule, residence only.** The earlier passage-then-residence composition is dropped: it would have been a second rule for the same outcome.
5. **No `capture_margin`.** Reason: any margin moves the volume toward a ball resting on the rim; with the top fixed 0.048 m below the deepest such ball, the two occupancy readings cannot disagree for a single ball; a second Pool ball stacked on the first (A5) overlaps the volume by up to 5 mm with its centre 0.2 m above it, so only the declared centre rule keeps it out.
6. **Values:** starting values (D1).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One static body, five colliders, one residence sensor, three parameters, one event and one presentation binding group. No capture logic of its own.
2. **Generic laws.** Contact and gravity put the ball in the cup; the general residence rule reports it. "Beside the opening" is geometry: the ball is not inside the declared volume.
3. **Three layers.** Colliders: five static hidden `col_` boxes. Visuals: five box meshes matching them; four rim strips flush in the wall tops. Animation: the rim's colour pulse on the committed `Captured` event; it reads committed state and writes nothing back.
4. **Flow and typing.** No ports; `TargetSelector`, the participation Choice and the event kind are typed; values are f32.
5. **Flat state.** Bodies 1 (static); colliders 5 (boxes); joints 0; drives 0; sensors 1 (residence, one bounded volume); domain nodes 0; stores 0; presentation bindings 3 (the rim's three colour channels, as the Receiver's halo).
6. **Performance.** Five static primitives, the Receiver's count. The sensor is one bounded box; its per-tick cost is the sensor pass that `ENGINE-SENSOR-VOLUMES` and `ENGINE-SPATIAL-QUERIES` own. Nothing is cosmetic-only in physics.
7. **Tests are scenarios.** Every row runs the production simulation; A3 is written as an equivalence between the physical state and the report.

**Verdict: conforms** (the audit's fixes of 22 November 2026 are applied: the 0.46 m opening, the declared occupancy rule, no margin, the parameter table).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `pool_pocket` |
| Named identity | EL-099, `../invest/named-elements.md#element-099` |
| Acceptance | `todo-227`, `sequence-task-652` |
| Decision | S669, `../invest/scope-corrections.md#s669` |
| Mode records | `Configuration=Fixed` |

Its sequence task exists: `sequence-task-652` stands beside the `element-099` anchor (`docs/planning/requirements.md:5187`–`:5189`). What the reader lacks today: `scope-corrections.md` has no `s669` block; and the reader wants the todo and the task adjacent in one row, while `todo-227` is a scope index shared by thirteen identities (`:6389`) apart from the task.
