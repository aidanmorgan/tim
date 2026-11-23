# EL-203 · Reverse transmission named-identity spec

**Native implementation (17 November 2026):** [Story 11.2b implementation and native evidence](../../../_bmad-output/implementation-artifacts/spec-11-2b-reverse-transmission.md) owns the active contract. The package uses two finite-inertia solid-cylinder wheels and the existing untimed −1 ratio law. Generic rigid shaft links use optional explicit rim metadata, animation-worker geometry and committed-travel witnesses; these links are not EL-200's friction/tension Belt or EL-201 Chain. Current typed ports allow one input and one output role per coordinate, with pass-through and supported bounded cycles; historical fan-out observations below are not a new acceptance prerequisite. The shared row retains its committed reaction impulse so loaded stops do not accumulate false travel/work. The slice is published (commit `c34f6390`, record status done); post-catalogue browser qualification remains pending.

**Current delivery (24 October 2026):** [EL-203 delivery slice](../invest/named-elements.md#element-203). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Harvest citations are `path@a6c914e:Lstart-Lend`. **Build-ready revision, 21 November 2026:** sections 1–3, 5 and 6 were verified against checkout `8d1d68aa`; citations of the form `path:line` are in that checkout, and test files are under `CuriousContraptions.tests/`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-203 |
| Name | Reverse transmission |
| Type | Mechanical |
| Anchor | [requirements.md#element-203](../requirements.md#element-203); [named entry](../invest/named-elements.md#element-203); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S312 |
| Refines / extends | Refines [CAT-057 reverse transmission](CAT-057-reverse_transmission.md) ([requirement](../requirements.md#current-cat-057)); the full legacy harvest is in that spec |
| Related | EL-199 Electric motor, EL-202 Conveyor, EL-200 Drive belt, CAT-018 Clutch (same transmission row, ratio +1) |
| Delivered part | Package `reverse_transmission`, directory `elements/ReverseTransmission` (Story 11.2b, commit `c34f6390`) |
| Disposition | **Candidate Duplicate closure of CAT-057.** The one delta its earlier spec raised, a separate crossed-belt reverser, is decided against (section 6). With that settled the identity has no behaviour, parameter, port, mode or material of its own. It joins the "Drive train" closure-review group with EL-202 ([delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures)). No package, palette or catalogue entry of its own |
| Status | Behaviour delivered; closure review not yet run; blocked on the engine programme (owner decision 3a), which comes first and re-qualifies the delivered package (relying most on ENGINE-ISLANDS-SLEEP). Browser qualification pending |

**What EL-203 adds over the delivered part: nothing.** Ledger sentence: "Declared gear/belt arrangement reverses signed shaft motion under load." Exact source outcome: "It cannot reverse by duplicating input work." Both are executed by delivered tests (section 5), with one assertion to confirm at closure.

## 2. Declaration

Delivered declaration, read from `elements/ReverseTransmission/ReverseTransmissionElementDeclaration.cs`.

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `IMechanicalTransmissionElement` (`:90`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) (`:10`).
- **Bodies.** Static housing (base box 1.5 × 0.16 × 0.9 m and housing box 1.4 × 0.65 × 0.5 m, both colliding) and two dynamic wheels: solid cylinders, radius 0.32 m, width 0.14 m, 0.25 kg each, axial moment mr²/2 = 0.0128 kg·m², transverse m(3r² + w²)/12 (`:49`–`:59`). Footprint 3 bodies, 2 moving, 2 revolute (`:22`).
- **Material.** All three bodies: restitution 0, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:35`–`:40`).
- **Hinges.** Two continuous revolutes about local +Z at (∓0.36, 0, 0.32); no spring, no bearing friction, no drive; wheels do not collide with the housing (`:54`–`:58`).
- **Transmission.** One `MechanicalTransmissionDescriptor`, bilateral, ratio −1 between revolute coordinates 0 and 1 (`:60`–`:61`). It couples the two finite inertias; neither end receives a copied speed or a work allowance.
- **Parameters.** None (`:21`). The ratio is fixed declaration data.
- **Ports.** `PortId.DriveIn` (role `DriveIn`, Mechanical, input) at (−0.36, 0, 0.4) on coordinate 0 and `PortId.Drive` (role `Drive`, Mechanical, output) at (0.36, 0, 0.4) on coordinate 1; both face +Z with −1 coordinate per radian and a 0.32 m rim (`:27`–`:32`).
- **Stores.** None.
- **Presentation.** The two wheel nodes follow their bodies (`:23`–`:24`); no cosmetic binding.
- **Palette.** Category Motion, order 39, id `reverse_transmission`, title "Reverse gear" (`:13`–`:18`).

### Art and solidity

Classified in `VisibleSolidColliderTests.cs:102`: solid with colliders are `Visual/Base`, `Visual/Housing`, `Visual/InputWheel/Disc` and `Visual/OutputWheel/Disc`; listed decorative are each wheel's `Index` and `Mark`.

**Possible visible-solid defect, not in either contract.** Each wheel's `Index` is a box 0.48 × 0.065 × 0.04 m centred 0.09 m in front of the wheel centre, and each `Mark` is a sphere of radius 0.07 m centred at z 0.10 (`elements/ReverseTransmission/reverse_transmission.tscn:31`–`:37`, `:87`–`:93`). The disc's front face is at z 0.07, so the bar stands 0.04 m and the sphere 0.10 m proud of it. Both ride the wheel's hinge body; no presentation binding turns them. Under the owner's rule that is a proud visible solid on a body, the same case as the Conveyor's input pulley. `spec-engine-art-solidity.md` lists findings only for the eleven packages outside the hand table, so it does not record this. It is raised in the batch summary for the visible-solid owner (flush redraw, as the Motor's index was). It does not block the closure, whose subject is shaft behaviour.

Colours: reverser `#d69c47`, navy `#293954` base and index bars, cream `#fff8e9` housing, gold `#f7cb52` marks ([DESIGN colour system](../../../DESIGN.md#colour-system)). It is told from the Clutch by its two exposed side-by-side wheels turning in opposite senses.

## 3. Engine capabilities

Map families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, SlidingFriction, StateTransaction ([map row](../general-engine-element-map.md); navigation only, decision D14). The harvest's "Missing" list is superseded: every capability is present.

| Capability | Status | Evidence |
| --- | --- | --- |
| Continuous revolute hinges with declared cylinder inertia | present | `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`); `ReverseTransmissionTests.AbsentDriveLeavesBothWheelsAtRest` (`ReverseTransmissionTests.cs:154`) |
| Bilateral ratio coupling of two coordinates | present | `MechanicalTransmissionDescriptor` (`MechanicalContractValues.cs:258`); `ReverseTransmissionTests.ADeclaredOutputLoadReactsThroughReverseToTheSource` (`ReverseTransmissionLoadTests.cs:19`) |
| Mechanical ports, shaft links and link presentation | present | `ReverseTransmissionTests.CatalogueMotorDrivesEveryCurrentPairing` (`:81`); `ReverseTransmissionArtTests.cs:17` |
| Committed transmission reaction across Run, Reset and candidate execution | present | `ReverseTransmissionStateTests.cs:14` |

Nothing here needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Declared gear/belt arrangement reverses signed shaft motion under load"; outcome "It cannot reverse by duplicating input work" ([element-203](../requirements.md#element-203)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Two 0.25 kg wheels, r 0.32, w 0.14; hinges at (∓0.36, 0, 0.32); ratio −1 always engaged | `parts/ReverseTransmissionPart.cs@a6c914e:L11-L18` | carry forward | Declaration. |
| 2 | Ideal phase-free transmission; shared inertia sets speeds and reactions; no copied speed or work allowance | `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L9-L13` | carry forward (law), do not carry forward (CPU joint) | "Cannot duplicate input work." |
| 3 | Accepted: through reversers the last conveyor's speed is the motor speed times (−1)^reversers on the same substep; each reverser's output speed is −input speed | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L100-L112` | carry forward | Signed reversal. |
| 4 | Level `reverse_belt` ("The other way round"): reverse the drive so the conveyor carries the ball back to the basket | `content/puzzles.json@a6c914e:L4648-L5070` | carry forward | Lesson. |

**Files harvested:**
- `parts/ReverseTransmissionPart.cs`
- `parts/catalog/reverse_transmission.tres`
- `engine/SceneRotaryShaft.cs`
- `engine/physics/PhysicsTransmissionJoint.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `content/puzzles.json` (level `reverse_belt`)
- The remaining reverser files are harvested in [CAT-057](CAT-057-reverse_transmission.md#4-legacy-harvest) (Batch C). Searched with no hit for a crossed-belt reverser: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-203](../requirements.md#element-203) and the [CAT-057 spec](CAT-057-reverse_transmission.md). The Chrome recipe (open `reverse_belt`, place the reverser, connect motor `Drive` → reverser `DriveIn` and reverser `Drive` → conveyor `DriveIn`, Run) stays queued for the post-catalogue phase.

### Closure check

| Criterion | Test |
| --- | --- |
| Positive, under load: cargo travels the opposite way to the no-reverser bypass | `ReverseTransmissionTests.ReverseTransportsCargoOppositeTheZeroReverserBypass` (`ReverseTransmissionTests.cs:134`) |
| Parity: two reversers restore the direction, for every current pairing | `EveryCurrentPairingCarriesCargoWithReversalParity` (`:73`); `CatalogueMotorDrivesEveryCurrentPairing` (`:81`) |
| **Exact source outcome:** an output load reacts through the reverser on the source, and an output drive back-drives both shafts and the unpowered Motor | `ReverseTransmissionLoadTests.cs:19`, `:114` |
| Controls: no drive, no motion; supply loss adds no motor work | `AbsentDriveLeavesBothWheelsAtRest` (`:154`); `SupplyLossAddsNoMotorWorkAndResetRestoresAuthoredState` (`:166`) |
| Boundaries: stopped load; passive two-reverser loop; open downstream clutch | `ReverseTransmissionLoadTests.cs:19` (stopped case); `ReverseTransmissionLifecycleTests.cs:93`, `:121` |
| Run/Reset and Save/Load, rotated | `ReverseTransmissionTests.SaveLoadAndResetPreserveRotatedTwoReverserParity` (`ReverseTransmissionLifecycleTests.cs:68`) |
| Wrong-kind connection refusal; conflicting sources and wrong direction | `ReverseTransmissionLifecycleTests.cs:104` (unsupported mechanical connections; it does not offer an Electrical wire to `DriveIn`); the Electrical-into-`DriveIn` refusal is `ConveyorTests.InvalidMechanicalConnections_RefusedAtomically` (`CuriousContraptions.tests/ConveyorTests.cs:529`); `ReverseTransmissionTests.cs:192` |
| Parameter refusal | Not applicable: the package declares no parameter; an unknown field is refused by the generic save check (`DeclaredOrientationTests.cs:135`) |
| Solid housing and wheel | `ReverseTransmissionLifecycleTests.cs:18`, `:42` |

**One assertion to confirm at closure.** "Cannot reverse by duplicating input work" is shown today through reaction and back-drive. No delivered test compares work across the reverser; the closure adds `ReverseTransmissionTests.TheLoadGainsNoMoreWorkThanTheSourcePaid` (file `ReverseTransmissionLoadTests.cs`, a part of the partial class `ReverseTransmissionTests`): a supplied Motor drives a Conveyor carrying a ball through one reverser; after the ball has moved 1 m (stall guard 10 s of game time) over the same shaft travel the Motor's committed drive work with one reverser equals that of the no-reverser bypass within 5 %, and the ball's speed has the same magnitude within 5 % and the opposite sign; as a second assertion the Motor's work is at least the kinetic energy of the ball and the turning shafts. A doubled output would pass that inequality alone, which is why the bypass comparison comes first. Control: the no-reverser bypass. Boundary: the Motor at its 0.1 N·m torque floor.

### Integration

- **Collaborators.** Motor, Conveyor, Clutch, Battery: all delivered.
- **Campaign.** Row 2: first use 11–20, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); [todo-151](../requirements.md#todo-151), [todo-152](../requirements.md#todo-152). In `content/puzzles.json` it appears only in level 18 `reverse_belt` (planned; provable now in the Early level proof "Drive" group).
- **Coverage binding.** Catalogue key `reverse_transmission` (CAT-057). EL-203 has no package of its own and so no package source binding. The closure needs no lesson-fixture story (S8).

### Delivery

- **Slice.** The Drive train closure-review slice, with EL-202. Owned diff: ledger rows, at most the one test above. Review tier: data-only. The closure is against the delivered rigid mechanical connection, which stays the default (owner decision 2); the drive belt and chain (EL-200, EL-201) are new player-chosen materials that leave it unchanged, so the closure has no ordering against ENGINE-MECHANICAL-LINKS.
- **Prerequisites.** Blocked on the engine programme (owner decision 3a), which comes first; the closure review then re-runs its rows on the re-qualified package.
- **Files.** `docs/planning/invest/named-elements.md` (the element-203 entry cites `reverse_transmission` and the tests above; its "normal commit/publication checks follow" wording is replaced by the published commit), this spec's status, the closure record, and `ReverseTransmissionLoadTests.cs` if the work test is added.
- **What would disqualify the plan.** The work test failing: that would be a real engine defect (work created across the ratio row) and goes to an engine repair slice, not into the closure.

## 6. Open questions

None open.

1. Does "gear/belt arrangement" require a separate crossed-belt reverser? **Decided no** (standing delegation, decision D19). Reason: the row names one outcome; the delivered part reverses signed shaft motion and is joined to its neighbours by the generic shaft links, which is the arrangement the row describes. A crossed-belt part would be a second part with the same behaviour and nothing new to teach. Belts as friction-and-tension connections belong to [EL-200](EL-200-drive-belt.md). No campaign lesson uses a second reverser kind.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, with the package opened (`elements/ReverseTransmission/`) and the engine projects searched for the package id and name (no hit).

1. **Declaration data only.** A static housing, two hinge-carried wheels, one declared internal ratio coupling of −1 and two mechanical ports. No engine code names the package.
2. **Generic laws.** The bilateral ratio coupling between two revolute coordinates (`MechanicalTransmissionDescriptor`), hinge friction and the coupling of mechanical connections. Reversal is the declared sign of a ratio; work is neither created nor doubled because the coupling is a constraint.
3. **Three layers.** Colliders: housing and wheel proxies. Visuals: housing, wheels, index bars and mark spheres; none is a collider. Animation: none; each wheel's art follows its body's pose. Recorded visible-solid defect: the wheels' `Index` bars (0.04 m proud) and `Mark` spheres (0.10 m proud) ride the hinge bodies with no collider and no binding (`elements/ReverseTransmission/reverse_transmission.tscn:31`–`:37`, `:87`–`:93`); the batch summary proposes a flush redraw in the visible-solid remainder.
4. **Flow and typing.** Typed Mechanical `DriveIn` and `Drive` ports on revolute coordinates 0 and 1; no parameters.
5. **Flat state.** Bodies 3 (1 static, 2 dynamic); colliders 4 (boxes 2: base and housing; cylinders 2: the wheels); joints 2 revolute; couplings 1; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings: two body bindings.
6. **Performance.** Two hinges and one coupling row. Nothing is cosmetic in physics. No island sleeping exists today; a resting reverser costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** The delivered rows and the one added work comparison run the production simulation.

**Verdict: conforms with one art fix (the proud index bars and mark spheres).**
