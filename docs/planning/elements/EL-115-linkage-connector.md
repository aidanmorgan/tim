# EL-115 · Linkage connector — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-115 delivery slice](../invest/named-elements.md#element-115). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text (Batch M). Citations of the form `path:line` are to this checkout (published main `8d1d68aa`); `path@a6c914e` citations appear only in section 4. Numbers marked **starting value** are hand calculations shown here and stand under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); nothing was simulated. All game values are f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-115 · Linkage connector · Construction |
| Anchor | [requirements.md#element-115](../requirements.md#element-115) with its co-anchored `sequence-task-668`; [named-elements entry](../invest/named-elements.md#element-115); umbrella [gap-02](../requirements.md#gap-02) (index only) |
| Owner key | S329 |
| Related identities | [EL-113 Structural brace](EL-113-structural-brace.md) (family sibling); [EL-112 Structural beam](EL-112-structural-beam.md) and [EL-114 Placeable pivot](EL-114-placeable-pivot.md) (the levers it couples). Contrast: the delivered rope, which pulls and never pushes. No CAT refines it |
| Status | Not started; blocked on the engine programme (owner decision 3a), then ENGINE-ASSEMBLY-JOINTS and the 3a story `ENGINE-PARAMETER-BOUND-FRAMES` (section 3) |

### Disposition

**Member of the Family slice "Pin-ended members"** with EL-113 Structural brace: one resizable dynamic box with a pin mount at each end, differing from the brace only in declared values (a thin square section, a fixed mass, a shorter minimum length, art). Its own catalogue entry (D8) and its own acceptance rows. Planned package id `linkage`, directory `elements/Linkage/`, title "Linkage". Not a Duplicate closure of the brace: its section, mass law, length range and acceptance differ. Blocked on the engine programme (owner decision 3a: every element is blocked on the engine programme), then ENGINE-ASSEMBLY-JOINTS and `ENGINE-PARAMETER-BOUND-FRAMES`.

## 2. Declaration

### Declaration to write

The attachment model is the one stated in [EL-112](EL-112-structural-beam.md), section 2. The declaration has the shape of [EL-113](EL-113-structural-brace.md) with these values.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement`, `IParameterizedGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`, `:29`), and the attachment interface the engine story adds (missing).
- **Body.** One dynamic box rod, local X along it: `length` × 0.08 × 0.08 m, one box collider of the same size, half-extent X bound to `length` × 0.5.
- **Mass and inertia (starting values).** 0.2 kg at every length: a light link that does not dominate the 1.5 kg beams it couples. Moments: about X, m·(0.08² + 0.08²)/12 = 0.000213 kg·m²; about Y and Z, m·(L² + 0.08²)/12 = 0.016667·L² + 0.000107 (0.0376 kg·m² at 1.5 m). Gravity (0, −9.81, 0) m/s²; drag 0.
- **Material.** Restitution 0.1, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s.
- **Mounts.** `EndA` at (−L/2, 0, 0) and `EndB` at (+L/2, 0, 0), positions following `length`; each a **pin** mount with its free axis along local Z. Which joined bodies collide is the one rule stated in [EL-112](EL-112-structural-beam.md), section 2. An unconnected mount declares no constraint, so the rod hangs from its other pin.
- **Parameter.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `length` | Real (f32) | Metre | 1.5 | 0.3 | 4.0 | Reject | BuildOnly |

- **Ports, stores, sensors, events.** None.
- **Presentation.** `Visual` scale X ← `length` × (1/1.5); resize along X. Art follows the body pose.
- **Catalogue.** Id `linkage`, title "Linkage", description "A thin rod with a pin at each end. It pushes as well as pulls.", category `ElementCategory.Structure`, palette order 505 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cream `#fff8e9`.
- **Footprint.** Bodies 1, moving bodies 1, constraints 0 of its own.

### Art and solidity

| Node | Mesh | Layer that moves it | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Rod` | cream `#fff8e9` box 1.5 × 0.08 × 0.08 m, scaled in X | body pose; parameter scale | solid | the box collider |
| `Visual/Visual_EyeA`, `Visual_EyeB` | gold `#f7cb52` end faces with a navy `#293954` centre dot, flush | body pose | decorative | printed flat |

Icon: a thin rod with a gold dot at each end. It differs from the brace by its thin square section and plain rod (no chevron band), and from the rope by being straight and rigid.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Dynamic box body, contact and rest | present | `ColliderShapeKind.Box` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `WorkshopPhysicsLawCylinderTests.BoxesDroppedOntoAFaceStayPutAtRest` (`CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs:2829`) |
| Collider half-extent bound to a Real parameter | present | `ColliderParameterBinding` (`SceneGeometry.cs:27`) | `WorkshopWallTests.InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip` (`WorkshopWallTests.cs:55`) |
| Moments as a polynomial of one parameter with a constant mass | present | `MassScalar` (`CuriousContraptions.Abstractions/MassFrameValues.cs:8`) | `WeightTests.TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter` (`WeightTests.cs:139`) |
| A revolute constraint law | present inside one package | `RevoluteConstraintDeclaration` (`CuriousContraptions.Abstractions/RevoluteConstraintValues.cs:14`) | `RevoluteHingeFrictionTests.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` (`RevoluteHingeFrictionTests.cs:59`) |
| The same constraint between two dynamic bodies of two separately placed parts, made by the player | missing | connection domains are Activation, Electrical, Rope and Mechanical only (`engine/gpu/WorkshopConnections.cs:11`) | ENGINE-ASSEMBLY-JOINTS |
| A socket position bound to a parameter | missing | `AnchorPointDescriptor` has no binding (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:63`) | `ENGINE-PARAMETER-BOUND-FRAMES` |
| The connect-time geometry rule: what happens when a mount and its point are apart | missing | — | ENGINE-ASSEMBLY-JOINTS (fixed once in that story for every mount) |
| A rope as the pull-only contrast | present | the Rope connection domain (`WorkshopConnections.cs:11`) | `ImpactLeverTests.RopeLiftingIntegration_TieBLiftsArm` (`ImpactLeverTests.cs:516`) |

Counts: 4 present, 0 present-untested, 1 narrower, 3 missing: the player-made connection and the connect-time rule (ENGINE-ASSEMBLY-JOINTS) and the parameter-bound socket position (`ENGINE-PARAMETER-BOUND-FRAMES`). Every row also waits for the engine programme (owner decision 3a).

## 4. Sources and legacy

- **Requirements.** Row: "Finite-length member couples declared body attachment points"; outcome "Disconnected endpoint cannot transmit motion" ([element-115](../requirements.md#element-115)). Integration: "A connected lever transfers motion; a detached control does not" ([sequence-task-535](../requirements.md#sequence-task-535)).
- **Audit.** "Demonstrate a driven linkage and disconnected control" (`docs/physics-puzzle-gap-audit.md@a6c914e:L81-L81`).
- **Legacy** (joint framework; no linkage part existed):

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A ball-socket removes anchor velocity but leaves spin. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L14-L20`; `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L174-L182` | Carry forward as the anchor behaviour of each link end (the planar pin is its hinge restriction). |
| 2 | A rope only tensions and never pushes; slack is free. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L86-L97`; `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L184-L195` | Carry forward as the contrast control. |
| 3 | Removing a constraint at runtime releases the load without replacing world or body. | `CuriousContraptions.tests/PhysicsJointUpdateTests.cs@a6c914e:L52-L69` | Carry forward: a detached end transmits nothing. |
| 4 | A runtime constraint cannot snap an off-axis body into place. | `CuriousContraptions.tests/PhysicsJointUpdateTests.cs@a6c914e:L71-L80` | Carry forward: a connection never moves a part by an impulse at Run start. |
| 5 | Joint edits were Attach/Replace/Detach with no upsert. | `engine/physics/PhysicsJointChange.cs@a6c914e:L5-L28` | Carry forward the explicit edit kinds; the CPU world mutation path does not carry forward. |

Files harvested: `CuriousContraptions.tests/JointConstraintTests.cs`, `CuriousContraptions.tests/PhysicsJointTests.cs`, `CuriousContraptions.tests/PhysicsJointUpdateTests.cs`, `engine/physics/PhysicsJointChange.cs`.

## 5. Acceptance outline

### Behaviour and numbers

- **Laws.** Two pin constraints on a rigid rod: the distance between its two connected points is fixed in both directions, so it carries compression as well as tension. A rope fixes only a maximum distance.
- **Test rig (geometry by hand).** Lever 1 is a 2 m beam held at `Mid` by the head mount of a Pivot whose axis is at (0, 3.0) m (underside 2.65 m above the bench) and whose limits are set to ±30°. Lever 2 is a 2 m beam held at `Mid` by a Pivot with its axis at (2.0, 4.2) m. Lever 1's `EndB` is at (1.0, 3.0) and lever 2's `EndA` at (1.0, 4.2): the linkage stands vertically between them, `length` 1.2 m. A horizontal link in line with both pivots would sit at a dead point and move lever 2 only to second order, which is why the levers are at different heights. The link overlaps only the two beam ends it is pinned to, which the rule in [EL-112](EL-112-structural-beam.md), section 2, allows; lever 2's plinth (x 1.8 to 2.2 m) is 0.8 m from the link and behind the beams' plane.
- **Loads clear the bench.** Weights hang on 1.5 m ropes. A 2 kg Weight is 0.5771 × (0.136734 × 2 + 0.453070) = 0.42 m from tie to base (`elements/Weight/WeightElementDeclaration.cs:39`–`:40`). With lever 1 on its 30° stop the loaded tie is at 3.0 − 0.5 = 2.5 m, so the Weight spans 0.58 to 1.0 m above the bench.
- **Push.** A 2 kg Weight roped to lever 1's `TieA` puts 2·9.81·1.0 = 19.6 N·m on it. Against it stand at most the link's own weight on `EndB`, 0.2·9.81·1.0 = 1.96 N·m, and the two bearings' friction, 0.2 N·m each: 2.36 N·m, an eighth of the load. Lever 1 turns to its 30° stop, raising `EndB` by 1.0·sin 30° = 0.5 m. The link is in compression and raises lever 2's `EndA` by about the same amount, turning lever 2 by about 30° (0.52 rad).
- **Pull.** With the Weight on lever 1's `TieB`, `EndB` descends 0.5 m, the link is in tension and lever 2 turns the other way by about 30°.
- **Rope contrast.** A 1.2 m rope between lever 1's `TieB` and lever 2's `TieA` in place of the link: in the push case the rope goes slack and lever 2 does not move.
- **Controls with an end left unconnected (by hand).** An unconnected end may not stay where its point is, because it would lie inside a beam it is not joined to and the pair would collide at Run start ([EL-112](EL-112-structural-beam.md), section 2). In L3 the link hangs straight down from lever 1's `EndB`, from 3.0 to 1.8 m; with lever 1 on its 30° stop its top is at 3.5 m, 0.6 m below lever 2's underside (4.2 − 0.10 = 4.1 m), and it stays 0.8 m clear of lever 1's plinth (x 1.0 against 0.2) and 1.8 m above the bench. In L5's control the free end stays at the lower beam's original `Mid` height; that beam is 0.20 m deep, so moved 0.15 m down its top lies 0.15 − 0.10 = 0.05 m below the free end. The lower Pivots move with it, leaving 1.2 + 0.15 − 0.50 = 0.85 m between the upper plinths' undersides and the lower plinths' tops.
- **The rig is never at rest unloaded.** With no Weight the link's 1.96 N·m exceeds the bearings' 0.4 N·m, so lever 1 sinks to its lower stop. No row asserts a still rig; the no-impulse row uses a rig that cannot move (row L5).
- **Tick order.** Mechanical substeps only (Phase 3 of the [solver model](../../gpu-f32-physics.md#solver-model)).

### Acceptance rows

Family test class `PinEndedMemberTests`; these are the linkage's rows. "Turns" means lever 2 turns more than 0.35 rad (20°) within 5 s; "stays" means it turns less than 0.02 rad over 5 s. The two are separated by a factor of seventeen. Stall guards 10 game seconds. Chrome proof is pending the post-catalogue phase.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| L1 | Given the rig in the push case, when Run starts, then lever 2 turns | `PinEndedMemberTests.AConnectedLinkPushesTheSecondLever` (a cross-part row owned by the Weight slice, CAT-067: the rig is loaded by a Weight, taught at 12b, after the linkage at 9; coordinator decisions 36 and 41) | L3 | the 1 kg Weight (9.81 N·m against at most 2.36 N·m, a factor of four) also turns it |
| L2 | Given the rig in the pull case, then lever 2 turns the opposite way | `PinEndedMemberTests.AConnectedLinkPullsTheSecondLever` (a cross-part row owned by the Weight slice, CAT-067, like L1: the rig is loaded by a Weight) | L3 | — |
| L3 | Given the push rig with the link connected at lever 1's `EndB` only and set hanging straight down from it (from (1.0, 3.0) to (1.0, 1.8) m, clear of lever 2), then lever 1 still reaches its stop and lever 2 stays | `PinEndedMemberTests.ADisconnectedEndTransmitsNothing` (a cross-part row owned by the Weight slice, CAT-067, like L1: the rig is loaded by a Weight) | L1 | — |
| L4 | Given the push rig with a rope of the same length in place of the link, then lever 2 stays | `PinEndedMemberTests.ARopeOfTheSameLengthCannotPush` (a cross-part row owned by the Rope slice: the Rope and Weight are taught at 12b, after the linkage at 9; coordinator decisions 36 and 41) | L1 | — |
| L5 | Given two 2 m beams, each held level at `EndA` and `EndB` by the head mounts of two Pivots (so neither can move), the upper 1.2 m above the lower, and a 1.2 m link hanging from the upper beam's `Mid` with its free mount at the lower beam's `Mid`, 0.0 m apart; when that second connection is made in Build and Run starts, then no part moves more than 0.005 m in the first second: making a connection adds no impulse | `PinEndedMemberTests.AConnectionDoesNotKickTheAssembly` | with the second connection not made and the lower beam and its Pivots moved 0.15 m down, the link also hangs still (no part moves more than 0.005 m in the first second) | link lengths 0.3 and 4.0 m compile and hold a hanging rod |
| L6 | A `length` below 0.3, above 4.0 or non-finite is refused atomically; a wire to a mount is refused | conformance suite rows 2 and 5 (S4) once it lands; until then `PinEndedMemberTests.InvalidLengthAndForeignWiresAreRefused` | — | — |
| L7 | Reset restores both levers, the link and every attachment; save and load round-trip them and the reloaded rig turns lever 2 again | `PinEndedMemberTests.ResetAndSaveLoadRestoreTheLinkage` | — | Reset while lever 1 is turning |

Requirement rows preserved: "couples declared body attachment points" (L1, L2); "disconnected endpoint cannot transmit motion" (L3). L1 with L3 is the lever half of [sequence-task-535](../requirements.md#sequence-task-535); the free and limited pivot half is in [EL-114](EL-114-placeable-pivot.md).

### Integration

- **Works with.** Planned: EL-112, EL-114, EL-113. Delivered: `weight`, the rope, and later a `motor` for the campaign's driven linkage (level 38).
- **Campaign.** GAP-02 "Joint Effort": introduction 9, practice 10, reuse 38 (driven linkage), 46, 138 ([campaign allocation](../requirements.md#campaign-gap-allocation)).
- **Lesson fixtures (S8).** Needed: the levers are locked and the link is the player's part.
- **Coverage binding.** Section 7.

### Delivery

As [EL-113](EL-113-structural-brace.md). Prerequisites, in order: blocked on the engine programme (owner decision 3a), ENGINE-ASSEMBLY-JOINTS and the 3a story `ENGINE-PARAMETER-BOUND-FRAMES`, EL-112, EL-114, then the Family slice "Pin-ended members" (`elements/Linkage/` with `elements/StructuralBrace/`, one test class). Expected tier: data-only if every probe row is *present* with a test by then; otherwise engine.

**Disqualified if** the engine story's pin leaves all three rotations free (a ball joint): the rig then needs its levers held in plane by their Pivots, which they are, and row L5 gains an out-of-plane check; or if the story's connect-time rule moves parts to close a gap (then row L5 fails by design and the rule is the finding, not this part).

## 6. Open questions

**Resolved under the delegation.**

1. Massed rod or massless distance constraint: a massed solid rod. Reason: visible = solid; a drawn rod has a collider and a body.
2. A length that does not match the socket distance: decided once by the engine story's connect-time rule for every mount. This part only requires that a connection adds no impulse (L5).
3. Ball-socket ends: no. Planar pins, as every other pin in the batch (D19). The engine design audit lists this part as a consumer of the spherical joint of `ENGINE-JOINT-KINDS`; it is not one, and that row should drop it.
4. The no-impulse row on a rig that can move: not asserted. The two-lever rig sinks under the link's own weight (1.96 N·m against 0.4 N·m of bearing friction), so row L5 uses two beams that are each held at both ends.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, one collider, two pin mounts, one parameter, one scale binding.
2. **Generic laws.** Revolute constraints at made connections, rigid-body dynamics, contact. Push and pull follow from a rigid body between two pins; nothing is scripted. Missing capabilities are stated element-free, each with one owner: the connection and its connect-time rule (ENGINE-ASSEMBLY-JOINTS) and the parameter-bound socket position (`ENGINE-PARAMETER-BOUND-FRAMES`).
3. **Three layers.** Colliders: one box. Visuals: the rod and its flush end faces; none is a collider. Animation: none.
4. **Flow and typing.** Mounts whose typed id and role enum are the engine story's (a string name today); `length` is an f32 Real in metres.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (box); joints 0 declared, up to 2 made at connection time; drives 0; sensors 0; domain nodes 0; stores 0; mounts 2; presentation bindings 1 (parameter scale).
6. **Performance.** One primitive collider. The 0.08 m rod is thin but slow (it moves with levers at under 2 m/s, 4 mm per substep at 480 Hz), and tunnelling of anything faster is excluded by the engine's speculative contacts. Island sleeping does not exist today; it is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a). The ropes in the rows are the delivered rope, whose strand collider is owned by ENGINE-CONNECTION-MATERIALS (owner decision 1, 22 November 2026).
7. **Tests are scenarios.** Rows L1 to L7 run the production simulation on a built rig.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), ENGINE-ASSEMBLY-JOINTS and `ENGINE-PARAMETER-BOUND-FRAMES` for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final package-source-binding section in the identity's spec, read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries its heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `linkage` |
| Named identity | EL-115, `../invest/named-elements.md#element-115` |
| Acceptance | none available: the ledger entry has no todo link; the identity's own task is `sequence-task-668`, co-anchored with `element-115` |
| Decision | S329, `../invest/scope-corrections.md#s329` |
| Mode records | `Configuration=Adjustable` (`length`) |

What it lacks today: a todo link in the ledger entry, and an `s329` block in `scope-corrections.md` on published main.
