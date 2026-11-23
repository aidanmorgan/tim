# EL-114 · Placeable pivot — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-114 delivery slice](../invest/named-elements.md#element-114). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text (Batch M). Citations of the form `path:line` are to this checkout (published main `8d1d68aa`); `path@a6c914e` citations appear only in section 4. Numbers marked **starting value** are hand calculations shown here and stand under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); nothing was simulated. All game values are f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-114 · Placeable pivot · Construction |
| Anchor | [requirements.md#element-114](../requirements.md#element-114) with its co-anchored `sequence-task-667`; [named-elements entry](../invest/named-elements.md#element-114); umbrella [gap-02](../requirements.md#gap-02) (index only) |
| Owner key | S328 |
| Related identities | [EL-112 Structural beam](EL-112-structural-beam.md) (the member it carries); [EL-113](EL-113-structural-brace.md) and [EL-115](EL-115-linkage-connector.md) (their rigs stand on Pivots); [EL-117 Axle](EL-117-axle.md) (the continuous counterpart); [EL-159](EL-159-tension-limited-connector.md) to [EL-161](EL-161-bending-limited-connector.md) (their rigs hang from its `Under` and `Side` points). Same hinge law as the delivered Impact lever. No CAT refines it |
| Status | Not started; blocked on the engine programme (owner decision 3a), then `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and `ENGINE-PARAMETER-BOUND-FRAMES` (section 3) |

### Disposition

**To deliver as a single element** after the engine programme (owner decision 3a: every element is blocked on the engine programme), `ENGINE-JOINT-KINDS` (the fixed joint), ENGINE-ASSEMBLY-JOINTS, `ENGINE-PARAMETER-BOUND-FRAMES` (parameter-bound revolute limits) and EL-112. Planned package id `pivot`, directory `elements/Pivot/`, title "Pivot". It is the first part with a mount and the batch's only static anchor, so its slice also runs row A3 of EL-112.

## 2. Declaration

### Declaration to write

Closest delivered shape: `elements/ImpactLever/` (a static base carrying one body on a limited revolute hinge with bearing friction, `ImpactLeverElementDeclaration.cs:91`–`:122`). The attachment model is the one stated in [EL-112](EL-112-structural-beam.md), section 2.

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISceneGeometryElement`, and the attachment interface the engine story adds (missing).
- **Base body (static).** A plinth box 0.40 × 0.50 × 0.40 m, origin at the centre of its underside, with one box collider. Static, so it can be placed on the bench or anywhere above it, like a Wall.
- **Head body (dynamic, on the hinge).** A shaft: one cylinder collider of radius 0.0625 m and length 0.35 m along the package Z axis, centred at (0, 0.35, 0.375), so it stands out of the plinth's front face (z = 0.20) and ends at z = 0.55. The radius is 1/16 m, the smallest cylinder radius the engine admits (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:240`; the bound is inclusive and 1/16 is exact in f32). Mass 0.2 kg; moments 0.5·m·r² = 0.5·0.2·0.00390625 = 0.00039 kg·m² about its axis and m·(3r² + l²)/12 = 0.2·(0.01172 + 0.1225)/12 = 0.00224 kg·m² across it; gravity (0, −9.81, 0) m/s².
- **Hinge.** One `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): support body 0, frames at (0, 0.35, 0.20), free axis the frame's local Z (the package Z axis), travel `Limited`, lower and upper limits from the two parameters (missing capability, section 3), stiffness 0, damping 0, friction torque 0.2 N·m, initial angle 0, bodies do not collide, no drive.
  - **Why 0.2 N·m.** It holds a balanced lever against a 2 cm error in where two 1 kg loads hang (1·9.81·0.02 = 0.2 N·m) and is twelve times less than the lightest Weight on a 1 m arm (0.25·9.81·1.0 = 2.45 N·m), so every real load still turns it.
- **Sockets.** `Head`: a **rigid mount** (a fixed joint) on the head body at (0, 0.35, 0.40). Three **points** on the plinth, for parts that must be fixed to the world: `Base` at (0, 0.50, 0), the centre of its top face, for a part that stands on it; `Under` at (0, 0, 0), the centre of its underside, for a hung assembly; `Side` at (0.20, 0.25, 0), the centre of its +X side face, for a member held out level. A part fixed to `Under` or `Side` lies wholly outside the plinth, so nothing that it carries passes through the plinth.
  - **Clearance (by hand).** A beam is 0.30 m deep. Mounted with its point on `Head` it occupies z = 0.25 to 0.55, 0.05 m clear of the plinth face and flush with the shaft end. A beam in the plinth's own plane could never turn fully: its 0.20 m-high body sweeps a band 0.10 m either side of the axis and the plinth's top corners lie √(0.2² + 0.15²) = 0.25 m from the axis, inside the sweep.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `lower_angle` | Real (f32) | Degree | −180 | −180 | −1 | Reject | BuildOnly |
  | `upper_angle` | Real (f32) | Degree | 180 | 1 | 180 | Reject | BuildOnly |

  At the defaults the head turns a full half-turn either way, which is the requirement's free pivot for a lever. Travel is `Limited`, so a "free" pivot still stops at ±180°: it cannot make a whole turn, and no row asks for one. Narrowing either value gives the limited pivot. Continuous spinning is the Axle's job (EL-117), not this part's.
- **Ports, stores, sensors, events.** None.
- **Presentation.** Head art follows the head body. Two limit ticks on the plinth face take local rotation Z from `lower_angle` and `upper_angle` (parameter rows, authoring only).
- **Catalogue.** Id `pivot`, title "Pivot", description "A bearing on a plinth. Whatever is fixed to its shaft turns about it, between the two stops you set.", category `ElementCategory.Structure`, palette order 504 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cream `#fff8e9`.
- **Footprint.** Bodies 2, moving bodies 1, revolute constraints 1.

### Art and solidity

| Node | Mesh | Layer that moves it | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Plinth` | cream `#fff8e9` box 0.40 × 0.50 × 0.40 m | nothing (static) | solid | the plinth box |
| `Visual/Head/Visual_Shaft` | gold `#f7cb52` cylinder r 0.0625 m, 0.35 m long | head body pose | solid | the shaft cylinder |
| `Visual/Head/Visual_Notch` | navy `#293954` bar across the shaft's end face, flush | head body pose | decorative | printed flat; it shows the turn |
| `Visual/Visual_StopLower`, `Visual_StopUpper` | navy ticks on the plinth's front face at 0.12 to 0.15 m from the axis, flush | parameter rotation (authoring) | decorative | printed flat |
| `Visual/Visual_PointMarks` | gold rings on the top, underside and +X side faces around `Base`, `Under` and `Side`, flush | nothing | decorative | printed flat |

The requirement's limit arc is drawn as the two ticks, shown at all times (owner decision 24, 23 November 2026); no selection-gated binding is needed. Icon: a plinth with a gold shaft end and two ticks. It differs from the Axle (a collar with a through-shaft and no plinth) by its plinth and ticks.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| A static base carrying a body on a limited revolute hinge whose stops arrest travel | present | `IHingeElement` (`MechanicalContracts.cs:20`), `RevoluteHingeDescriptor` (`MechanicalContractValues.cs:149`) | `ImpactLeverTests.AngularLimitsBoundary_StopsArrestTravelWithoutPenetration` (`CuriousContraptions.tests/ImpactLeverTests.cs:380`) |
| Hinge friction that holds below its torque and yields above | present | `FrictionTorque` (`MechanicalContractValues.cs:158`) | `RevoluteHingeFrictionTests.AHingedBarIsHeldWhileItsGravityTorqueIsBelowTheFrictionTorque` (`RevoluteHingeFrictionTests.cs:77`) and `.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` (`:59`) |
| A cylinder collider on a hinge-carried body | present | the Motor's rotor (`elements/Motor/motor.tscn:126`) | `RevoluteDriveTests.AContinuousHingeTurnsPastTheFullCircleWithNoStopImpulseAndAccumulatesItsTravel` (`RevoluteDriveTests.cs:195`) |
| Limits spanning the full ±π | present, untested | the validator admits limits in [−π, π] (`CuriousContraptions.Bundle/BundleValidator.cs:796`–`:797`); no package declares them | this slice's row P1 |
| Revolute limits bound to declared parameters | narrower: the limits are constants (`MechanicalContractValues.cs:153`–`:154`), while a slider's limits are parameter values (`:107`–`:108`) | — | `ENGINE-PARAMETER-BOUND-FRAMES` ("a socket position and a revolute limit may each follow one declared Real parameter") |
| A fixed joint between two bodies | missing | constraint kinds are revolute, prismatic and tensile (`CoordinateKind`, `CuriousContraptions.Abstractions/PortDescriptor.cs:43`) | `ENGINE-JOINT-KINDS` |
| A mount on a hinge-carried body and points on a static body, joined by the player to other placed parts | missing | `AnchorPointDescriptor` is declared and never compiled, and its `Name` is a string (`MechanicalContractValues.cs:63`) | ENGINE-ASSEMBLY-JOINTS (typed socket id, role enum and the connection) |
| An art node rotated by a parameter | present, untested | `PresentationProperty.LocalRotationZ` (`CuriousContraptions.Abstractions/ElementPresentation.cs:14`); delivered parameter rows use scale only | this slice |

Counts: 3 present, 2 present-untested, 1 narrower, 2 missing. Owners: `ENGINE-PARAMETER-BOUND-FRAMES` (limits), `ENGINE-JOINT-KINDS` (fixed joint), ENGINE-ASSEMBLY-JOINTS (connections). Every row also waits for the engine programme (owner decision 3a).

## 4. Sources and legacy

- **Requirements.** Row: "Axis joint constrains translation while allowing declared rotation"; outcome "Off-axis load is solved by the shared joint; no animation path" ([element-114](../requirements.md#element-114)). Integration: a connected lever transfers motion, a detached control does not; a free pivot rotates and a limited pivot stops at its actual bound; Reset restores attachments, limits and transforms ([sequence-task-535](../requirements.md#sequence-task-535)).
- **Audit.** "Build on shared joint constraints; bounded attachments and visible motion limits ... Kernel hinges alone do not close this task" (`docs/physics-puzzle-gap-audit.md@a6c914e:L81-L81`).
- **Legacy** (joint framework; no placeable pivot existed):

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A hinge pendulum keeps its pivot, gains no energy, and replays after Restore. | `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L66-L91` | Carry forward as acceptance. |
| 2 | Radial, axial and pivot-through impulses cannot rotate a hinge. | `CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L61-L70` | Carry forward (row P3). |
| 3 | Balanced loads cancel; an off-centre load rotates the hinge. | `CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L47-L59` | Carry forward (rows P1, P5). |
| 4 | Stops arrest high-speed travel and release inward motion. | `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L48-L72` | Carry forward; its 1e-7 tolerance is proof-grade and is not carried. |
| 5 | A hinge range must lie inside (−π, π); invalid ranges reject. | `engine/physics/PhysicsJoint.cs@a6c914e:L130-L141`; `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L175-L187` | Carry forward as refusal; the current validator admits the closed range. |
| 6 | Initial twist is measured against the authored frame. | `CuriousContraptions.tests/SceneJointDeclarationTests.cs@a6c914e:L186-L202` | Carry forward. |
| 7 | Direction policy (ratchet). | `engine/physics/PhysicsJoint.cs@a6c914e:L117-L117` | Do not carry forward: a ratchet is a separate element. |

Files harvested: `CuriousContraptions.tests/PhysicsJointTests.cs`, `CuriousContraptions.tests/SharedHingeMechanicsTests.cs`, `CuriousContraptions.tests/JointRangeTests.cs`, `CuriousContraptions.tests/JointConstraintTests.cs`, `CuriousContraptions.tests/SceneJointDeclarationTests.cs`, `engine/physics/PhysicsJoint.cs`.

## 5. Acceptance outline

### Behaviour and numbers

- **Laws.** The revolute constraint (three translations and two rotations locked), its stop rows at the declared limits, its Coulomb friction row, and the rigid attachment of whatever is fixed to `Head`.
- **Lever rig.** A 2 m beam (1.5 kg) fixed at `Mid` to `Head`. The Pivot's underside is 2.65 m above the bench, so its axis is at 3.0 m and the beam turns freely. A Weight roped to `TieB` on a 1.5 m rope hangs 1.0 m from the axis.
- **Clearances (by hand, from `elements/Weight/WeightElementDeclaration.cs:39`–`:40` and `:59`).** A Weight of mass m is 0.5771 × (0.136734·m + 0.453070) m from tie to base and 0.04808·m + 0.15933 m in radius: 0.34 m and 0.21 m at 1 kg, 0.58 m and 0.35 m at 4 kg. With the beam turned 90°, `TieB` is at 2.0 m and the 1 kg Weight spans 0.16 to 0.50 m above the bench. The 4 kg Weight of row P3 hangs from the axis and spans 0.92 to 1.50 m: its top is 1.15 m below the plinth's underside, so its 0.35 m radius never meets the plinth.
- **Free.** A 1 kg Weight gives 9.81 N·m against 0.2 N·m of friction; the beam turns until `TieB` is below the axis, about 90°.
- **Limited.** With `lower_angle` −30 and `upper_angle` 30 the same load stops the beam at 30° (0.524 rad), where the stop carries the remaining 9.81·cos 30° = 8.5 N·m.
- **Through the axis.** A 4 kg Weight roped to the beam's `Tie` hangs from the axis itself: no torque, 39.2 N on the pin.
- **Tick order.** Mechanical substeps only (Phase 3 of the [solver model](../../gpu-f32-physics.md#solver-model)).

### Acceptance rows

Test class `PlaceablePivotTests`. Angles are read from the committed head pose. The stop tolerance is 0.05 rad, the delivered Impact lever test's; "the pin holds" means the head's axis point moves less than 0.005 m. Stall guards 10 game seconds. Chrome proof is pending the post-catalogue phase.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| P1 | Given the lever rig at the default limits with a 1 kg Weight on `TieB`, when Run starts, then the beam turns past 60° within 5 s and the pin holds | `PlaceablePivotTests.AFreePivotTurnsUnderAnOffAxisLoad` (a cross-part row owned by the Weight slice, CAT-067: the Weight is taught at 12b, after the pivot at 9; coordinator decisions 36 and 41) | P3 | the 0.25 kg Weight also turns it |
| P2 | Given the same rig with limits −30 and 30, then the beam stops within 0.05 rad of 30° and rests there | `PlaceablePivotTests.ALimitedPivotStopsAtItsDeclaredBound` | P1 (it passes 60°) | limits −1 and 1: the beam moves less than 0.07 rad |
| P3 | Given a 4 kg Weight roped to the beam's `Tie`, then the beam turns less than 0.02 rad over 5 s and the pin holds | `PlaceablePivotTests.ALoadThroughTheAxisTurnsNothing` (a cross-part row owned by the Weight slice, CAT-067: the Weight and Rope are taught at 12b, after the pivot at 9; coordinator decisions 36 and 41) | P1 | — |
| P4 | Given the beam placed in the same pose but not attached, then it falls and the head does not turn | `PlaceablePivotTests.ADetachedBeamFallsAndThePivotStaysStill` | P1 | — |
| P5 | Given 1 kg Weights on `TieA` and `TieB`, then the beam stays within 0.05 rad for 5 s | `PlaceablePivotTests.EqualLoadsBalance` (a cross-part row owned by the Weight slice, CAT-067: the Weight is taught at 12b, after the pivot at 9; coordinator decisions 36 and 41) | adding a 0.25 kg Weight's worth to one side (2.45 N·m) tips it | — |
| P6 | Row A3 of [EL-112](EL-112-structural-beam.md): a beam on two Pivots carries its load and drops when one attachment is removed | `PlaceablePivotTests.ABeamOnTwoPivotsCarriesItsLoadAndDropsWhenOneAttachmentIsRemoved` (a cross-part row owned by the Weight slice, CAT-067, which also closes EL-112's row A3: the Weight and Rope are taught at 12b, after the pivot at 9; coordinator decisions 36 and 41) | the removed attachment | — |
| P7 | A limit outside its range or non-finite is refused atomically; a wire to a socket, and a mount offered to the `Head` mount, are refused with a typed reason | conformance suite rows 2 and 5 (S4) once it lands; until then `PlaceablePivotTests.InvalidLimitsAndForeignConnectionsAreRefused` | −180 and 180 are admitted | — |
| P8 | Reset restores the head angle, the attachment and the limits; save and load round-trip both limits, the pose and the attachment | `PlaceablePivotTests.ResetAndSaveLoadRestoreThePivot` | — | Reset while the beam is turning |

Requirement rows preserved: "constrains translation while allowing declared rotation" (P1, P2, pin holds); "off-axis load is solved by the shared joint; no animation path" (P1 against P3: the turn comes from the load's torque, and no binding moves a body); the free and limited halves of [sequence-task-535](../requirements.md#sequence-task-535) (P1, P2) and its Reset clause (P8).

### Integration

- **Works with.** Planned: EL-112 (first), then EL-113, EL-115, EL-159 to EL-161, EL-185. Delivered: `weight` and the rope for loads.
- **Campaign.** GAP-02 "Joint Effort": introduction 9, practice 10, reuse 38, 46, 138 ([campaign allocation](../requirements.md#campaign-gap-allocation)).
- **Lesson fixtures (S8).** Needed: lessons lock the Pivot and its limits.
- **Coverage binding.** Section 7.

### Delivery

1. Prerequisites, in order: blocked on the engine programme (owner decision 3a); then `ENGINE-JOINT-KINDS` (fixed joint), ENGINE-ASSEMBLY-JOINTS and `ENGINE-PARAMETER-BOUND-FRAMES` (parameter-bound revolute limits).
2. EL-112.
3. This slice: `elements/Pivot/` (project, declaration, scene, icon), `CuriousContraptions.tests/PlaceablePivotTests.cs`, the solution line until S3 lands, the art classification. Expected tier: data-only if every probe row is *present* with a test by then; otherwise engine. No `elements/Pivot/` package exists in this checkout, so no delivered shaft is resized.

**Disqualified if** `ENGINE-PARAMETER-BOUND-FRAMES` does not bind revolute limits to parameters: the Pivot then ships with fixed limits as two catalogue entries (free and ±45°), which changes the lesson and goes to the owner; or if f32 conversion of ±180° falls outside the validator's [−π, π], in which case the range becomes ±179°.

## 6. Open questions

**Resolved under the delegation.**

1. Mounting on a moving support: not in this part. The plinth is static; a pin between two moving members is a pin mount of a brace, linkage or connector.
2. Joint softness: the engine's shared constraint tune; the part declares none.
3. Restitution at the stops: none declared; the stop is the hinge's stop row, as on the Impact lever.
4. Free and limited as a mode: no mode. Two limit parameters whose defaults are the free case. Reason: a mode switch would be a second rule for the same hinge.
5. Limit editing by dragging an arc: a Workbench UI matter; the parameters are ordinary editable values (D11).
6. Three points on the plinth (`Base`, `Under`, `Side`), not one: a hung rig on the top point would pass its beam through the plinth, and a level member needs a side face. Each is one line of declaration data and a fixed socket count.
7. The limit arc is shown at all times, as the two ticks (owner decision 24, 23 November 2026).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, a hinge-carried body, one hinge, one mount, three points, two parameters, two parameter-rotated ticks.
2. **Generic laws.** The revolute constraint with stops and friction; a fixed joint. The missing capabilities are stated element-free, each with one owner: parameter-bound revolute limits (`ENGINE-PARAMETER-BOUND-FRAMES`), the fixed joint (`ENGINE-JOINT-KINDS`) and player-made connections (ENGINE-ASSEMBLY-JOINTS).
3. **Three layers.** Colliders: one box (static), one cylinder (head). Visuals: plinth, shaft, flush notch, ticks and ring. Animation: none; the shaft and notch follow the head body's pose, and the ticks are placed by authored parameters. Nothing is moved by a binding that should push or block.
4. **Flow and typing.** No ports; limits are f32 Reals in degrees; a socket's typed id and role enum are the engine story's (a string name today).
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 2 (1 box, 1 cylinder); joints 1 (revolute) plus one fixed joint when connected; drives 0; sensors 0; domain nodes 0; stores 0; sockets 4 (1 mount, 3 points); presentation: 1 body binding, 2 parameter rows.
6. **Performance.** Two primitive colliders, fewer than the Impact lever. The plinth is static. Island sleeping does not exist today; it is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a). The rows' ropes are the delivered rope, whose strand collider is owned by ENGINE-CONNECTION-MATERIALS (owner decision 1, 22 November 2026).
7. **Tests are scenarios.** Rows P1 to P8 run the production simulation on a built lever.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and `ENGINE-PARAMETER-BOUND-FRAMES` for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final package-source-binding section in the identity's spec, read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries its heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `pivot` |
| Named identity | EL-114, `../invest/named-elements.md#element-114` |
| Acceptance | none available: the ledger entry has no todo link; the identity's own task is `sequence-task-667`, co-anchored with `element-114` |
| Decision | S328, `../invest/scope-corrections.md#s328` |
| Mode records | `Configuration=Adjustable` (`lower_angle`, `upper_angle`) |

What it lacks today: a todo link in the ledger entry, and an `s328` block in `scope-corrections.md` on published main.
