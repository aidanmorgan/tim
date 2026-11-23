# EL-116 · Passive wheel — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-116 delivery slice](../invest/named-elements.md#element-116). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text (Batch M). Citations of the form `path:line` are to this checkout (published main `8d1d68aa`); `path@a6c914e` citations appear only in section 4. Numbers marked **starting value** are hand calculations shown here and stand under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); nothing was simulated. All game values are f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-116 · Passive wheel · Construction |
| Anchor | [requirements.md#element-116](../requirements.md#element-116) with its co-anchored `sequence-task-669`; [named-elements entry](../invest/named-elements.md#element-116); umbrella [gap-03](../requirements.md#gap-03) (index only) |
| Owner key | S330 |
| Related identities | [EL-117 Axle](EL-117-axle.md) (couples it to a chassis); [GAP-04 Driven wheel](GAP-04-driven-wheel.md) (same silhouette with a supplied drive); [EL-112 Structural beam](EL-112-structural-beam.md) (the chassis). Same rolling law as the delivered Weight on its side. No CAT refines it |
| Status | Not started; blocked on the engine programme (owner decision 3a), which it needs for its order only. Every capability its own requirement row needs is delivered, so it waits for no element-specific engine story (section 3) |

### Disposition

**To deliver as a single element after the engine programme (owner decision 3a); no element-specific engine story.** Planned package id `wheel`, directory `elements/Wheel/`, title "Wheel". The rolling cylinder that [delivery batches](../invest/delivery-batches.md), section 1b, asked the probe to settle exists and is tested (section 3). The wheel's attachment point `Hub` is one line of declaration data added by the EL-117 slice once ENGINE-ASSEMBLY-JOINTS exists; the chassis integration ([sequence-task-536](../requirements.md#sequence-task-536)) is proven there.

## 2. Declaration

### Declaration to write

Closest delivered shape: `elements/Weight/` (one dynamic cylinder with a declared mass frame and rolling resistance, `WeightElementDeclaration.cs:49`–`:60`), with constants in place of its mass-bound values, as `elements/BowlingBall/` declares a ball (`BowlingBallElementDeclaration.cs:38`–`:59`).

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`).
- **Body.** One dynamic body with one cylinder collider: radius 0.30 m, width 0.20 m. The collider's own axis is its local +Y (`CuriousContraptions.Abstractions/ColliderDescriptor.cs:30`); the scene's collider node is turned so the wheel's axle lies along the package Z axis and the wheel stands on its curved side when placed.
  - **Why 0.30 × 0.20 m.** The radius is the Motor rotor's scale, so wheels and drives match. The width makes the wheel stand: on its curved side it rights itself up to a lean of atan(0.10/0.30) = 18.4°, against 14.0° for a 0.15 m width.
- **Mass and inertia (starting values).** 0.5 kg. Solid cylinder: 0.5·m·r² = 0.0225 kg·m² about the axle; m·(3r² + w²)/12 = 0.5·(0.27 + 0.04)/12 = 0.01292 kg·m² across it. Gravity (0, −9.81, 0) m/s²; linear drag 0. Light beside a 4 kg load, so traction and load, not wheel mass, decide what a chassis does.
- **Material (starting values).** Restitution 0.1 (a wheel should not bounce a chassis), friction 0.8 (a grippy tread), rolling resistance 0.03 (the delivered Weight's, `WeightElementDeclaration.cs:52`), bounce threshold 0.1 m/s.
- **Payload.** `PayloadPolicy.None`: a wheel is not cargo for a Receiver.
- **Parameters.** None. The requirement row names no size parameter (D13).
- **Ports, stores, sensors, events, joints.** None.
- **Sockets.** None in this slice. `Hub`, a point at the wheel's centre, is added by the EL-117 slice.
- **Presentation.** The art follows the body pose. No binding: the spin a player sees is the body's committed rotation.
- **Catalogue.** Id `wheel`, title "Wheel", description "A free-rolling wheel. It moves only when something pushes it or the ground slopes.", category `ElementCategory.Motion`, palette order 111 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cream `#fff8e9`.
- **Footprint.** Bodies 1, moving bodies 1, constraints 0.

### Art and solidity

| Node | Mesh | Layer that moves it | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Disc` | cream `#fff8e9` cylinder r 0.30 m, 0.20 m wide | body pose | solid | the cylinder collider |
| `Visual/Visual_Tread` | warm-wood `#c28f52` band on the curved face, flush | body pose | decorative | printed flat on the rolling surface, so the wheel rolls on its true radius |
| `Visual/Visual_Hub` | navy `#293954` disc on each flat face, flush | body pose | decorative | printed flat (the requirement's "hub recess" is drawn, not cut) |
| `Visual/Visual_Spoke` | one gold `#f7cb52` radial bar on each flat face, flush | body pose | decorative | printed flat; it makes spin and slip readable |

Icon: a cream disc with a navy hub and one gold spoke. It differs from the Weight (a banded upright cylinder with an eye) and from the balls by its flat faces and single spoke; from the Driven wheel by having no drive housing or socket.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- |
| Dynamic cylinder collider with solid-cylinder mass properties | present | `ColliderShapeKind.Cylinder` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `WeightTests.TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter` (`CuriousContraptions.tests/WeightTests.cs:139`) |
| Rolling without slipping on a slope when friction allows | present | the contact law's friction rows (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119` combines the pair's friction as the geometric mean) | `WorkshopPhysicsLawCylinderTests.ACylinderRollsDownARampAtTwoThirdsGSinThetaWithoutSlipping` (`WorkshopPhysicsLawCylinderTests.cs:180`) |
| Slipping when friction is below tan θ / 3 | present | the same | `WorkshopPhysicsLawCylinderTests.BelowTheNoSlipFrictionTheCylinderSlipsFasterThanItRolls` (`:196`) |
| Rolling resistance on the curved side brings a level roll to rest | present | `RollingResistance` (`PhysicsDeclarationValues.cs:108`) | `WorkshopPhysicsLawCylinderTests.RollingResistanceOnTheCurvedSideBringsABenchRollToRestAndWithoutItTheRollContinues` (`:211`) |
| A packaged cylinder rolling off a Ramp onto the bench and stopping without creep | present | the Weight package | `WeightTests.OnItsSideItRollsDownARampWithoutSlippingAndComesToRestOnTheBenchWithoutAxialCreep` (`WeightTests.cs:361`) |
| A fast roll stays on its curved side | present | — | `WorkshopPhysicsLawCylinderTests.AFastRollStaysOnItsCurvedSideAtConstantSpeed` (`:240`) |
| A package whose cylinder is authored lying down (collider node turned in the scene) | present, untested as a package default: the Weight is authored upright and tests lay it down by pose | scene collider extraction | this slice's row W1 |

Counts: 6 present, 1 present-untested, 0 narrower, 0 missing. No element-specific engine story is needed; like every element, the wheel is blocked on the engine programme (owner decision 3a).

## 4. Sources and legacy

- **Requirements.** Row: "Round body rolls according to friction and applied load"; outcome "No hidden drive on level ground" ([element-116](../requirements.md#element-116)). Integration: a loaded chassis rolls downhill, slips under insufficient traction, stops against a block; lifted wheels cannot support it ([sequence-task-536](../requirements.md#sequence-task-536)).
- **Audit.** "Moving assemblies, collision and mass accounting. Compare roll, slip and blocked wheel; no scripted travel path" (`docs/physics-puzzle-gap-audit.md@a6c914e:L82-L82`).
- **Legacy.** No wheel part, level or test existed. Only decorative wheels (Motor, Clutch, Conveyor art, `parts/MotorPart.cs@a6c914e:L83-L85`), with no rolling physics: nothing to carry.

Files harvested: `parts/MotorPart.cs`, `parts/ClutchPart.cs`, `parts/ConveyorPart.cs` (art only).

## 5. Acceptance outline

### Behaviour and numbers

- **Laws.** Contact, Coulomb friction and rolling resistance on a rigid cylinder. No drive of any kind is declared, which is the whole of "no hidden drive".
- **Grip on a Ramp.** The Ramp's friction is 0.3 (`elements/Ramp/RampElementDeclaration.cs:54`–`:57`), so the pair's is √(0.8·0.3) = 0.49. A solid cylinder rolls without slipping while tan θ ≤ 3μ: up to atan(1.47) = 55.8°. On a 60° Ramp (tan θ / 3 = 0.577 > 0.49) it slips.
- **Rolling resistance of a pair.** The engine takes the larger of the two colliders' coefficients (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:231`), so on a Ramp or bench that declares less than 0.03 the wheel's own value applies.
- **Down a 10° Ramp, 2 m.** Without rolling resistance a = (2/3)·g·sin 10° = 1.14 m/s². Resistance removes (2/3)·0.03·g·cos 10° = 0.19 m/s², leaving about 0.94 m/s² and 1.9 m/s at the foot.
- **On the level.** Deceleration (2/3)·0.03·g = 0.196 m/s² (the law the delivered test states): from 1.9 m/s the wheel stops in about 10 s after about 9.5 m.
- **Tick order.** Mechanical substeps only (Phase 3 of the [solver model](../../gpu-f32-physics.md#solver-model)).

### Acceptance rows

Test class `WheelTests`. Rolling is judged by spin against speed over radius, as the delivered cylinder tests do. Stall guards: 30 game seconds for the level roll (three times the expected stop), 10 s elsewhere. Chrome proof is pending the post-catalogue phase.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| W1 | Given a wheel placed on the level bench, when 10 s pass, then it has moved less than 0.005 m and turned less than 0.01 rad: no hidden drive | `WheelTests.AWheelOnLevelGroundStaysWhereItIsPut` | a Basketball rolling into it at 2 m/s sets it rolling at more than 0.2 m/s | placed leaning 10° it rights itself: after 5 s its centre is within 0.05 m of where it was placed and its speed is under 0.01 m/s |
| W2 | Given a wheel at the top of a 10° Ramp, when it rolls down, then at two samples on the Ramp its spin is within 5 % of speed / 0.30 m | `WheelTests.AWheelRollsDownARampWithoutSlipping` | W4 | — |
| W3 | Given the wheel leaving that Ramp onto the bench, then at one-second samples its speed never exceeds its speed at the foot by more than 1 %, and it comes to rest | `WheelTests.ALevelRollOnlySlowsAndStops` | W1 | — |
| W4 | Given a wheel on a 60° Ramp, then its spin stays below 90 % of speed / 0.30 m: it slips | `WheelTests.OnASlopeTooSteepForItsGripTheWheelSlips` | W2 | — |
| W5 | Given a Wall across the foot of the 10° Ramp, when the wheel reaches it, then it stops against the Wall (overlap under 0.005 m, speed under 0.05 m/s within 5 s of contact) | `WheelTests.AWheelStopsAgainstAWall` | W3 (no Wall: it rolls on) | — |
| W6 | Reset restores pose and zero spin; save and load round-trip the pose, and the reloaded wheel rolls again | `WheelTests.ResetAndSaveLoadRestoreTheWheel` | — | Reset while rolling |

The wheel has no parameter and no port, so there is nothing to refuse; the conformance suite (S4) covers the generic rows when it lands. Requirement rows preserved: "rolls according to friction and applied load" (W2, W4, the W1 control); "no hidden drive on level ground" (W1, W3). The chassis rows of sequence-task-536 are in [EL-117](EL-117-axle.md).

### Integration

- **Works with.** Delivered: `ramp`, `wall`, every ball. Planned: EL-117 (then a beam chassis), GAP-04.
- **Fit.** 0.60 m across: it passes the 1.3 m pipe bore but is not cargo; it is not admitted by the Cannon (payload None).
- **Campaign.** GAP-03 "Wheel Meet Again": introduction 18, practice 19, reuse 37, 68, 138 ([campaign allocation](../requirements.md#campaign-gap-allocation)).
- **Lesson fixtures (S8).** Needed for the preloaded chassis of level 18, not for the wheel alone.
- **Coverage binding.** Section 7.

### Delivery

1. Prerequisites: blocked on the engine programme (owner decision 3a), needed for its order only; no single story of it is a capability this part lacks.
2. This slice, with no element-specific engine prerequisite: `elements/Wheel/` (project, declaration, scene, icon), `CuriousContraptions.tests/WheelTests.cs`, the solution line until S3 lands, the art classification. Registration is derived. Expected tier: data-only.
3. Later, in the EL-117 slice: the `Hub` point.

**Disqualified as data-only if** the slice's probe finds that a cylinder authored lying down does not extract with its axis along Z. If the bench is shorter than the 9.5 m level roll, row W3 uses a 1 m Ramp at 5° instead: a = (2/3)·9.81·(sin 5° − 0.03·cos 5°) = 0.37 m/s², 0.87 m/s at the foot, and a level stop after 0.87²/(2·0.196) = 1.9 m.

## 6. Open questions

**Resolved under the delegation.**

1. Collider: a true cylinder, which the engine has.
2. Width, radius and tread material: fixed (D13).
3. Category: Motion, with the other free-moving bodies.
4. `Hub` now or later: later, with the first part that can use it. Reason: a declared socket nothing compiles would be dead data.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, one collider, one material. No parameter, port, sensor or binding.
2. **Generic laws.** Contact, friction and rolling resistance; all present and tested on cylinders.
3. **Three layers.** Colliders: one cylinder. Visuals: the disc with flush tread, hub and spoke; none is a collider. Animation: none; the spoke turns because the body turns.
4. **Flow and typing.** Nothing flows from the part; values are f32.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (cylinder); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 0.
6. **Performance.** One primitive collider, where the Weight's scene declares five collider nodes (`elements/Weight/weight.tscn:70`–`:91`). A wheel at 12 m/s moves 25 mm per 480 Hz substep against a 600 mm body; tunnelling is excluded by the engine's general means, exercised by `WorkshopPhysicsLawCylinderTests.ACylinderAtSixtyFourMetresPerSecondNeverPassesAOneCentimetreWall` (`:1869`). A resting wheel does not sleep today: island sleeping is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a).
7. **Tests are scenarios.** Rows W1 to W6 run the production simulation through placed packages.

**Verdict: conforms; blocked only on the engine programme (owner decision 3a).**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final package-source-binding section in the identity's spec, read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries its heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `wheel` |
| Named identity | EL-116, `../invest/named-elements.md#element-116` |
| Acceptance | none available: the ledger entry has no todo link; the identity's own task is `sequence-task-669`, co-anchored with `element-116` |
| Decision | S330, `../invest/scope-corrections.md#s330` |
| Mode records | `Configuration=Fixed` |

What it lacks today: a todo link in the ledger entry, and an `s330` block in `scope-corrections.md` on published main. So the wheel is ready to build but cannot be bound for coverage until S7 widens the reader or supplies both.
