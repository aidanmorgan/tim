# EL-202 · Conveyor named-identity spec

**Current delivery (24 October 2026):** [EL-202 delivery slice](../invest/named-elements.md#element-202). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Harvest citations are `path@a6c914e:Lstart-Lend`. **Build-ready revision, 21 November 2026:** sections 1–3, 5 and 6 were verified against checkout `8d1d68aa`; citations of the form `path:line` are in that checkout, and test files are under `CuriousContraptions.tests/`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-202 |
| Name | Conveyor |
| Type | Mechanical |
| Anchor | [requirements.md#element-202](../requirements.md#element-202); [named entry](../invest/named-elements.md#element-202); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S311 |
| Refines / extends | Refines [CAT-019 conveyor](CAT-019-conveyor.md) ([requirement](../requirements.md#current-cat-019)); the full legacy harvest is in that spec |
| Related | EL-199 Electric motor (drive), EL-200 Drive belt (link), EL-203 Reverse transmission |
| Delivered part | Package `conveyor`, directory `elements/Conveyor` (Story 11.1b, record `spec-11-1b-conveyor.md`, status done; commit `a0f6c4c0`) |
| Disposition | **Candidate Duplicate closure of CAT-019.** The identity has no behaviour, parameter, port, mode, material or acceptance of its own. It is closed in the "Drive train" closure-review group ([delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures)) by the check in section 5. No package, palette or catalogue entry of its own |
| Status | Behaviour delivered; closure review not yet run; blocked on the engine programme (owner decision 3a), which comes first and re-qualifies the delivered package (relying most on ENGINE-ISLANDS-SLEEP). Browser qualification pending |

**What EL-202 adds over the delivered part: nothing.** Ledger sentence: "Finite-work driven surface moves cargo through actual contact and friction." Exact source outcome: "Unpowered or noncontact cargo is not carried along a hidden path." Both are executed by the delivered tests in section 5.

## 2. Declaration

Delivered declaration, read from `elements/Conveyor/ConveyorElementDeclaration.cs`. Where a value below differs from the harvest in [CAT-019](CAT-019-conveyor.md), the delivered value governs.

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `IDrivenSurfaceElement` (`:80`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) (`:13`).
- **Bodies.** Static carrier (scene colliders: carrier box, two rails, idle roller, two supports) and one dynamic driven roller: radius 0.16 m, length 1.3 m, 0.5 kg, solid-cylinder moments (transverse m(3r² + w²)/12, axial mr²/2) (`:15`–`:17`, `:94`–`:105`). Footprint 2 bodies, 1 moving, 1 revolute (`:40`).
- **Material.** Carrier and roller: restitution 0.05, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:56`–`:60`).
- **Hinge.** Continuous revolute about local +Z at (−1.35, −0.07, 0); no spring; bearing friction 0.02 N·m; no drive of its own; roller and carrier do not collide (`:80`–`:107`).
- **Driven surface.** The carrier's +Y face moves along local +X at `surface_per_radian` × the roller's rate (`:109`–`:114`). Cargo moves only through contact friction on that face.
- **Parameters** (all build-only reals, editor default policy):

  | Name | Unit | Min | Max | Default |
  | --- | --- | --- | --- | --- |
  | `length` | m | 1.0 | 10.0 | 3.0 |
  | `width` | m | 0.6 | 4.0 | 1.2 |
  | `surface_per_radian` | m/rad | 0.1 | 5.0 | 0.6666667 |

  (`:35`–`:37`). The harvest's "≠ 0, signed" range for `surface_per_radian` is superseded: the value is positive, and travel is reversed by reversing the shaft (Reverse transmission), not by a negative ratio. The harvest's "upper bounds open" note is closed by these ranges.

  **CAT-019 residual, not closed here (22 November 2026, audit M20).** `length` and `width` are admitted, saved and refused out of range, but they bind no collider, hinge frame or port (`elements/Conveyor/ConveyorElementDeclaration.cs:35`–`:36` declare them and nothing reads them; the hinge and both ports are at fixed positions). Every delivered conveyor is therefore 3.0 × 1.2 m whatever the two values say, while CAT-019 requires them to define the surface (`docs/planning/requirements.md:570`). EL-202 adds nothing to CAT-019, so its closure stands; the residual belongs to CAT-019 and needs a general capability whose owner is ENGINE-PARAMETER-BOUND-FRAMES (the size binding of a carrier; owner decision 3a places it before its first consumer, CAT-019).
- **Ports.** `PortId.DriveIn` (role `DriveIn`, Mechanical, input) at (−1.35, −0.07, 0.78) and `PortId.Drive` (role `Drive`, Mechanical, output) at (1.35, −0.07, 0.78), both facing +Z on revolute coordinate 0 with +1 coordinate per radian and a 0.22 m rim (`:44`–`:51`). The harvest's "−1 per radian" is superseded.
- **Stores.** None. Work arrives only through `DriveIn`.
- **Presentation.** The roller follows its body; `Visual/PulleyOut` turns with the committed revolute angle (`:19`–`:22`).
- **Palette.** Category Motion, order 37, id `conveyor`, title "Conveyor belt" (`:25`–`:30`).

### Art and solidity

Classified in `VisibleSolidColliderTests.cs:105`: solid with colliders are `Visual_Carrier`, `Visual_RailA`, `Visual_RailB`, `Visual_IdleRoller`, `Visual_SupportA`, `Visual_SupportB` and `Roller/Visual_Roller`; listed decorative are `Roller/PulleyIn/*` and `PulleyOut/*`.

**Recorded visible-solid defect.** `spec-elements-visible-solid-remainder.md` (part B) measured on `8d1d68aa`: the input and output pulley wheels are cylinders of radius 0.22 m and length 0.1 m at z 0.78, outside every collider; the input pulley rides the roller's hinge body with no binding. Part B gives both wheels cylinder colliders and redraws the input spoke flush. That repair is owned by part B, not by this closure. Colours: conveyor `#d69c47`, carrier `#273744` ([DESIGN colour system](../../../DESIGN.md#colour-system)).

## 3. Engine capabilities

Map families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, SlidingFriction, StateTransaction ([map row](../general-engine-element-map.md); navigation only, decision D14). The harvest's "Missing" list is superseded: every capability is present.

| Capability | Status | Evidence |
| --- | --- | --- |
| Continuous revolute hinge with bearing friction | present | `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`); `RevoluteDriveTests.AContinuousHingeTurnsPastTheFullCircleWithNoStopImpulseAndAccumulatesItsTravel` (`RevoluteDriveTests.cs:195`) |
| Driven surface coupled to a coordinate, with shaft reaction | present | `DrivenSurfaceDescriptor` (`MechanicalContractValues.cs:241`); `DrivenSurfaceBindingTests.CoastingSourcePaysForCargoMotionAndUnloadedControlKeepsItsSpin` (`DrivenSurfaceBindingTests.cs:379`), `ABlockedLoadReactsOnTheSelectedShaft` (`:576`) |
| Mechanical `DriveIn`/`Drive` ports and shaft links | present | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` (`ConveyorTests.cs:417`) |
| Revolute-angle presentation binding | present | `ConveyorTests.TheRollerArtFollowsTheCommittedRollerPoseAndResetsToItsScenePose` (`:630`) |

Nothing here needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Finite-work driven surface moves cargo through actual contact and friction"; outcome "Unpowered or noncontact cargo is not carried along a hidden path" ([element-202](../requirements.md#element-202)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Driven surface speed = travel-per-radian × axis·(ω_shaft − ω_carrier) | `engine/physics/DrivenSurface.cs@a6c914e:L9-L20` | carry forward (law), do not carry forward (CPU class) | Contact transport without copied speed. |
| 2 | Accepted: an unpowered belt holds the ball until a switched motor drives it, then the ball moves past x = 0.5 | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L57-L83` | carry forward | Unpowered control then positive. |
| 3 | Accepted: a body in another depth plane is not transported | `CuriousContraptions.tests/ConveyorTests.cs@a6c914e:L85-L106` | carry forward | Noncontact control. |
| 4 | Accepted: after supply loss the belt coasts with constant energy; Reset zeroes angles | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L113-L136` | carry forward (Reset); superseded (coasting) | The delivered roller has 0.02 N·m bearing friction, so a cut belt slows to rest (`ConveyorTests.cs:141`). |

**Files harvested:**
- `parts/ConveyorPart.cs`
- `parts/catalog/conveyor.tres`
- `engine/physics/DrivenSurface.cs`
- `CuriousContraptions.tests/ConveyorTests.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- The remaining conveyor files are harvested in [CAT-019](CAT-019-conveyor.md#4-legacy-harvest) (Batch C). Searched with no further EL-202 knowledge: `engine/bridge/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-202](../requirements.md#element-202) and the [CAT-019 outline](CAT-019-conveyor.md#5-acceptance-outline). The Chrome recipe (open `conveyor_courier`, place the conveyor under the ball, connect battery → motor `PowerIn` and motor `Drive` → conveyor `DriveIn`, Run) stays queued for the post-catalogue phase.

### Closure check (all executed in `ConveyorTests.cs` unless named)

| Criterion | Test |
| --- | --- |
| Positive: a driven belt carries the ball along +X | `PoweredTransport_AcceleratesBallAlongPositiveX` (`:73`) |
| **Exact source outcome, unpowered:** no transport and no work | `UnpoweredBelt_BallStaysStationaryWithNoWork` (`:105`) |
| **Exact source outcome, noncontact:** cargo beside the belt is ordinary rigid contact, not carried | `OffBeltMiss_SideContactRemainsOrdinaryRigid` (`:311`) |
| Finite work: an unloaded belt transfers none; a load is paid by the shaft | `UnloadedBelt_ConservesEnergy` (`:226`); `DrivenSurfaceBindingTests.cs:379` |
| Boundaries: power cut, reversed drive, two loads, rotated belt | `:141`, `:180`, `:280`, `:339` |
| Run/Reset; Save/Load | `ResetAndReplay_RestoresExactAuthoredState` (`:367`); `SaveLoadRoundtrip_PreservesParametersAndMechanicalConnections` (`:451`) |
| Parameter refusal; wrong-kind connection refusal | `InvalidParameters_RefusedAtomically` (`:496`), `RetiredParameters_RefusedAtomically` (`:516`); `InvalidMechanicalConnections_RefusedAtomically` (`:529`) |
| Collaborators: Motor, Reverse transmission, second Conveyor | `:73`; `ReverseTransmissionTests.ReverseTransportsCargoOppositeTheZeroReverserBypass` (`ReverseTransmissionTests.cs:134`); `:417` |

The closure reviewer reads each test's assertions against the ledger entry and adds a test only if one of these does not assert what its row says. None is planned.

### Integration

- **Collaborators.** Motor, Battery, Reverse transmission, Clutch, Windmill, Ball: all delivered.
- **Campaign.** Row 2: first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); retained behaviour [todo-151](../requirements.md#todo-151), teaching routes [todo-152](../requirements.md#todo-152). In `content/puzzles.json` (numbers are authored positions before decision 24d, each paired with its plan level in [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)): levels 16 `conveyor_courier`, 17 `belt_relay` and 18 `reverse_belt` (planned; provable from delivered parts after the engine programme, owner decision 3a, in the Early level proof "Drive" group), and planned levels 49 `double_cold_start` and 58 `grand_contraption` (several goals; after `ENGINE-GOALS` and their conversion, completion-ledger OS-08).
- **Coverage binding.** Catalogue key `conveyor` (CAT-019). EL-202 has no package of its own and so no package source binding. No lesson-fixture story (S8) is needed by the closure.

### Delivery

- **Slice.** One closure-review slice for the Drive train group. Its owned diff is ledger rows and, only if the review finds a gap, tests. Review tier: data-only (Verification Gap and the two auditors). The closure is against the delivered rigid mechanical connection, which stays the default (owner decision 2); the drive belt and chain (EL-200, EL-201) are new player-chosen materials that leave it unchanged, so the closure has no ordering against ENGINE-MECHANICAL-LINKS.
- **Prerequisites.** Blocked on the engine programme (owner decision 3a), which comes first; the closure review then re-runs its rows on the re-qualified package. It does not wait for visible-solid part B; if part B publishes first, the closure cites tests at that revision.
- **Files.** `docs/planning/invest/named-elements.md` (the element-202 entry cites `conveyor` and the tests above), this spec's status, the closure record.
- **What would disqualify the plan.** A reviewer finding that `:311` does not prove "noncontact cargo is not carried" for a ball in another depth plane (the harvest's fact 3): then one test is added to `ConveyorTests`, placing a ball at the belt's height but beyond its width and asserting it gains no speed along +X over 2 s.

## 6. Open questions

None for EL-202. The [CAT-019 open questions](CAT-019-conveyor.md#6-open-questions) on parameter upper bounds are answered by the delivered ranges in section 2; whether `length` and `width` define the surface is not, and is recorded there as a CAT-019 residual owned by ENGINE-PARAMETER-BOUND-FRAMES.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, with the package opened (`elements/Conveyor/`) and the engine projects searched for the package id and name (no hit).

1. **Declaration data only.** A static carrier, one hinge-carried roller, a declared driven surface, two mechanical ports and three parameters. No engine code names the package. **Finding:** two declared parameters, `length` and `width`, drive nothing. A parameter that the player can set and that changes neither physics nor art is not honest declaration data; it is the CAT-019 residual above, owned by ENGINE-PARAMETER-BOUND-FRAMES.
2. **Generic laws.** Contact friction on a declared driven surface whose speed is a declared ratio of a revolute coordinate, the hinge's friction torque, and the bilateral coupling of a mechanical connection. Cargo moves only by friction on that face.
3. **Three layers.** Colliders: carrier box, two rail boxes, idle-roller cylinder and two support boxes on the static body; the roller cylinder on the hinge body. Visuals: carrier, roller, port pulleys; none is a collider; the two port pulleys are the recorded visible-solid defect owned by part B (section 2). Animation: `Visual/PulleyOut` is turned by a `CommittedRevoluteAngle` binding on coordinate 0 (`elements/Conveyor/ConveyorElementDeclaration.cs:19`–`:22`); decorative, nothing written back. The roller art (`Visual/Roller`) follows its body's pose.
4. **Flow and typing.** Two typed Mechanical ports on revolute coordinate 0; f32 Real parameters with units.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 7 (boxes 5, cylinders 2); joints 1 revolute; drives 0; driven surfaces 1; sensors 0; domain nodes 0; stores 0; presentation bindings 2 (roller body, `PulleyOut` angle).
6. **Performance.** One hinge and one surface relation; nothing scans the scene. The size binding of ENGINE-PARAMETER-BOUND-FRAMES must be a compile-time evaluation of declared bindings (as collider bindings are), not per-tick work. No island sleeping exists today; a resting conveyor costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part; that story keeps a body on a turning conveyor awake.
7. **Tests are scenarios.** The delivered rows run the production simulation.

**Verdict: conforms with one fix (the inert `length` and `width`, a CAT-019 residual owned by ENGINE-PARAMETER-BOUND-FRAMES).**
