# EL-113 · Structural brace — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-113 delivery slice](../invest/named-elements.md#element-113). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text (Batch M). Citations of the form `path:line` are to this checkout (published main `8d1d68aa`); `path@a6c914e` citations appear only in section 4. Numbers marked **starting value** are hand calculations shown here and stand under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); nothing was simulated. All game values are f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-113 · Structural brace · Construction |
| Anchor | [requirements.md#element-113](../requirements.md#element-113) with its co-anchored `sequence-task-666`; [named-elements entry](../invest/named-elements.md#element-113); umbrella [gap-01](../requirements.md#gap-01) (index only) |
| Owner key | S327 |
| Related identities | [EL-115 Linkage connector](EL-115-linkage-connector.md) (family sibling); [EL-112 Structural beam](EL-112-structural-beam.md) (the member it mounts on); [EL-114 Placeable pivot](EL-114-placeable-pivot.md) (the pinned frame feet). No CAT refines it |
| Status | Not started; blocked on the engine programme (owner decision 3a), then ENGINE-ASSEMBLY-JOINTS and the 3a story `ENGINE-PARAMETER-BOUND-FRAMES` (section 3) |

### Disposition

**Member of the Family slice "Pin-ended members"** with EL-115 Linkage connector. Both are one resizable dynamic box with a pin mount at each end; they differ only in declared values (section, mass law, length range and default, art). Each is still its own catalogue entry (D8) with its own acceptance rows. Planned package id `structural_brace`, directory `elements/StructuralBrace/`, title "Brace". The slice runs after the engine programme (owner decision 3a: every element is blocked on the engine programme), ENGINE-ASSEMBLY-JOINTS, `ENGINE-PARAMETER-BOUND-FRAMES`, EL-112 and EL-114. The brace has no behaviour a linkage lacks: "no frame-name bonus" is satisfied by there being no brace rule at all.

## 2. Declaration

### Declaration to write

Closest delivered shapes: `elements/Wall/` (parameter-resized box) and `elements/Weight/` (dynamic body following one parameter). The attachment model is the one stated in [EL-112](EL-112-structural-beam.md), section 2: a *mount* declares the joint it makes and joins one *point* of another placed part. The mount descriptor is added by the engine story and this subsection is corrected to its final names.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement`, `IParameterizedGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`, `:29`), and the attachment interface the engine story adds (missing).
- **Body.** One dynamic box, local X along the brace: `length` × 0.12 × 0.20 m, one box collider of the same size, half-extent X bound to `length` × 0.5. Visibly slimmer than the beam's 0.20 × 0.30 m.
- **Mass and inertia (starting values).** Linear density 0.5 kg/m (0.707 kg at the default), lighter than the beam's 0.75 kg/m so it reads as a secondary member. Box moments as polynomials of `length`:
  - about X: m·(0.12² + 0.20²)/12 = 0.002267·L;
  - about Y: m·(L² + 0.20²)/12 = 0.041667·L³ + 0.001667·L;
  - about Z: m·(L² + 0.12²)/12 = 0.041667·L³ + 0.0006·L.
  At L = 1.414: 0.00321, 0.1202 and 0.1187 kg·m². Gravity (0, −9.81, 0) m/s²; drag 0.
- **Material.** As the beam: restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s.
- **Mounts.** `EndA` at (−L/2, 0, 0) and `EndB` at (+L/2, 0, 0), positions following `length`. Each is a **pin** mount: it holds its point of the other part coincident and leaves rotation about the brace's local Z free. Which joined bodies collide is the one rule stated in [EL-112](EL-112-structural-beam.md), section 2. An unconnected mount declares no constraint.
- **Parameter.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `length` | Real (f32) | Metre | 1.414 | 0.5 | 4.0 | Reject | BuildOnly |

  The default is the diagonal of a 1 m bay; the range is the beam's, so any bay built from beams can be braced.
- **Ports, stores, sensors, events.** None.
- **Presentation.** `Visual` scale X ← `length` × (1/1.414); resize along X. Art follows the body pose.
- **Catalogue.** Id `structural_brace`, title "Brace", description "A light pin-ended member. Fit it across a frame to stop the frame leaning.", category `ElementCategory.Structure`, palette order 506 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cream `#fff8e9`.
- **Footprint.** Bodies 1, moving bodies 1, constraints 0 of its own (each made connection adds one pin).

### Art and solidity

| Node | Mesh | Layer that moves it | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Brace` | cream `#fff8e9` box 1.414 × 0.12 × 0.20 m, scaled in X | body pose; parameter scale | solid | the box collider |
| `Visual/Visual_Chevron` | navy `#293954` chevron band on the front face, flush | body pose | decorative | printed flat |
| `Visual/Visual_EyeA`, `Visual_EyeB` | gold `#f7cb52` end faces, flush | body pose | decorative | printed flat; an end face does not distort under the X scale |

Icon: a slim diagonal bar with gold ends and a chevron. It differs from the beam by its gold ends and chevron band (shape and pattern), and from the linkage by its flat 0.12 × 0.20 m section against the linkage's thin square rod.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Dynamic box body, contact and rest | present | `ColliderShapeKind.Box` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `WorkshopPhysicsLawCylinderTests.BoxesDroppedOntoAFaceStayPutAtRest` (`CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs:2829`) |
| Collider half-extent bound to a Real parameter | present | `ColliderParameterBinding` (`SceneGeometry.cs:27`) | `WorkshopWallTests.InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip` (`WorkshopWallTests.cs:55`) |
| Mass and moments as polynomials of one parameter | present | `MassScalar` (`CuriousContraptions.Abstractions/MassFrameValues.cs:8`) | `WeightTests.TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter` (`WeightTests.cs:139`) |
| A revolute constraint law (translation locked, one free axis) | present inside one package | `RevoluteConstraintDeclaration` (`CuriousContraptions.Abstractions/RevoluteConstraintValues.cs:14`) | `RevoluteHingeFrictionTests.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` (`RevoluteHingeFrictionTests.cs:59`) |
| The same constraint between bodies of two separately placed parts, made by the player | missing | connection domains are Activation, Electrical, Rope and Mechanical only (`engine/gpu/WorkshopConnections.cs:11`) | ENGINE-ASSEMBLY-JOINTS ("connections the player makes between two separately placed parts at typed attachment sockets (rigid, hinged or sliding)") |
| A closed loop of pinned members (a triangle) solved without drift | missing as proof: no delivered scene closes a loop of constraints | — | ENGINE-ASSEMBLY-JOINTS ("a closed loop of three pinned members holds within 0.01 m for 10 s") |
| A socket position bound to a parameter | missing | `AnchorPointDescriptor` has no binding (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:63`) | `ENGINE-PARAMETER-BOUND-FRAMES` |

Counts: 3 present, 0 present-untested, 1 narrower (the revolute law exists only inside a package), 3 missing: the player-made connection and the closed loop (ENGINE-ASSEMBLY-JOINTS) and the parameter-bound socket position (`ENGINE-PARAMETER-BOUND-FRAMES`). Every row also waits for the engine programme (owner decision 3a).

## 4. Sources and legacy

- **Requirements.** Row: "Diagonal member constrains frame distortion through actual geometry and load"; outcome "Removing the brace changes stability; no frame-name bonus" ([element-113](../requirements.md#element-113)). Integration: a braced frame supports the load, an otherwise identical unbraced frame deflects or collapses ([sequence-task-534](../requirements.md#sequence-task-534)).
- **Audit.** "Compare a supported triangular frame with an unbraced control under the same load" (`docs/physics-puzzle-gap-audit.md@a6c914e:L80-L80`).
- **Legacy.** No brace part, level or test existed.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A hinge leaves exactly one free angular degree of freedom between its bodies. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L21-L32` | Carry forward: pinned frame corners rack until braced. |
| 2 | An off-centre hinge uses parallel-axis inertia through its anchor rows. | `CuriousContraptions.tests/JointConstraintTests.cs@a6c914e:L33-L40` | Carry forward as behaviour expected of the shared joint row. |
| 3 | Joints with overlapping connected bodies need an explicit collision policy. | `CuriousContraptions.tests/PhysicsJointTests.cs@a6c914e:L210-L219` | Carry forward: joined pairs do not collide. |

Files harvested: `CuriousContraptions.tests/JointConstraintTests.cs`, `CuriousContraptions.tests/PhysicsJointTests.cs`.

## 5. Acceptance outline

### Behaviour and numbers

- **Laws.** Two pin constraints and rigid-body dynamics. A brace across a four-bar frame fixes the distance between two points of two members, which removes the frame's one freedom. Nothing reads the part's name or kind.
- **Test bay (geometry by hand).** Two Pivots stand 1.0 m apart with their undersides 2.65 m above the bench, so both axes are at 3.0 m and the 0.40 m plinths leave 0.60 m between them. Each head mount holds `EndA` of a 1.0 m beam (0.75 kg), which turns in the plane 0.05 m in front of the plinths ([EL-114](EL-114-placeable-pivot.md), section 2). A 1.0 m linkage joins the two `EndB` points. The bay is built leaning 15°: the top-left corner is at (sin 15°, cos 15°) = (0.259, 0.966) m from the left axis. The brace runs from that corner to the `Mid` point of the right-hand beam at (1 + 0.5·0.259, 0.5·0.966) = (1.129, 0.483) m: its length is √(0.871² + 0.483²) = 0.996 m.
- **What overlaps, and why it may.** The brace and the linkage both mount on the left beam's `EndB`, and the brace's other end lies inside the right-hand beam at `Mid`. Every such pair is joined by an attachment or shares a point, so under the rule in [EL-112](EL-112-structural-beam.md), section 2, it does not collide. No other pair of bodies in the bay overlaps: the beams are 1.0 m apart and in front of the plinths. A brace with only one connection may not keep its braced pose, because its free end would lie inside the right-hand beam without a joint and the pair would collide at Run start; row B4 therefore sets it pointing up and to the left from the corner, where every part of it lies above y = 3.966 − 0.06·sin 45° = 3.92 m, overlapping only the left beam and the linkage, which share its one point, and 0.77 m above the left plinth's top (3.15 m).
- **The load.** A 4 kg Weight hangs from the left beam's `TieB` (the corner, 3.0 + 0.966 = 3.97 m above the bench) on a 1.5 m rope. It is 0.58 m from tie to base and 0.35 m in radius (`elements/Weight/WeightElementDeclaration.cs:39`–`:40`, `:59`), so it spans 3.97 − 1.5 − 0.58 = 1.89 m to 2.47 m: 0.18 m below the plinths' undersides, and it meets neither plinth. The 8 kg Weight of row B1's boundary (0.89 m, radius 0.54 m) spans 1.57 to 2.47 m.
- **Why the unbraced bay falls.** A 4 kg Weight roped to the left beam's `TieB` puts 39.2·1.0·sin 15° = 10.2 N·m about the left pivot, and each leaning beam adds 0.75·9.81·0.5·sin 15° = 0.95 N·m about its own pivot. The Pivot's bearing friction is 0.2 N·m, fifty times less than the load, so the bay folds: the legs swing over and down about their Pivots until the Weight reaches the bench. With the brace the same load is carried as an axial force in the brace.
- **Tick order.** Mechanical substeps only (Phase 3 of the [solver model](../../gpu-f32-physics.md#solver-model)).

### Acceptance rows

Family test class `PinEndedMemberTests`; these are the brace's rows. "Holds" means the top-left corner moves less than 0.01 m over 5 s; "folds" means it descends more than 0.3 m within 3 s (the corner of a folded bay ends more than 1 m lower). Stall guards 10 game seconds. Chrome proof is pending the post-catalogue phase.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| B1 | Given the leaning test bay with the 4 kg Weight and the brace fitted, when 5 s pass, then the bay holds | `PinEndedMemberTests.ABracedBayHoldsItsShapeUnderLoad` (a cross-part row owned by the Weight slice, CAT-067: the Weight is taught at 12b, after the brace at 9b; coordinator decisions 36 and 41) | B2 | an 8 kg Weight is also held |
| B2 | Given the same bay with the brace deleted, when Run starts, then it folds | `PinEndedMemberTests.TheSameBayWithoutItsBraceFolds` (a cross-part row owned by the Weight slice, CAT-067: the Weight is taught at 12b, after the brace at 9b; coordinator decisions 36 and 41) | B1 | a 0.25 kg Weight: it still folds (0.64 N·m against 0.2 N·m) |
| B3 | Given the braced bay with a Linkage of the same length in place of the brace, then it also holds: the result follows geometry, not the part's name | `PinEndedMemberTests.AnyPinEndedMemberOfTheSameLengthBracesTheBay` | — | — |
| B4 | Given the 0.996 m brace connected at the left beam's `EndB` only and set pointing up and to the left at 45°, so its free end is at (0.259 − 0.996·cos 45°, 3.966 + 0.996·sin 45°) = (−0.45, 4.67) m (left axis at x = 0, bench at y = 0), clear of every body it is not attached to, when Run starts, then it swings from its one pin and the bay folds as in B2 | `PinEndedMemberTests.AHalfConnectedBraceBracesNothing` | B1 | — |
| B5 | A `length` below 0.5, above 4.0 or non-finite is refused atomically; a wire of any signal or supply kind to a mount is refused | conformance suite rows 2 and 5 (S4) once it lands; until then `PinEndedMemberTests.InvalidLengthAndForeignWiresAreRefused` | 0.5 and 4.0 are admitted | — |
| B6 | Reset restores the bay's poses and every attachment; save and load round-trip `length`, pose and attachments, and the reloaded bay holds | `PinEndedMemberTests.ResetAndSaveLoadRestoreTheBracedBay` | — | Reset while the unbraced bay is folding |
| B7 | Given the braced bay of B1, when in Build the brace is detached, turned 90° about the vertical and laid on the bench 2.0 m to the left of the bay, and Run starts, then the bay folds as in B2, and a Basketball dropped from 1.0 m onto the brace's new place is stopped by it: at its first bounce the ball's centre is no lower than 0.12 + 0.34 − 0.02 = 0.44 m | `PinEndedMemberTests.AMovedMemberCarriesItsContactAndLoadPathWithIt` | with the brace left in the bay the bay holds (B1) and the same ball reaches the bench (centre 0.34 m within 0.02 m) | the brace moved back and re-attached in Build holds the bay again |

Requirement rows preserved: "constrains frame distortion through actual geometry and load" (B1, B3); "removing the brace changes stability" (B2); "no frame-name bonus" (B3). B1 with B2 is [sequence-task-534](../requirements.md#sequence-task-534), including "no implicit world anchor" (the bay stands only on its two Pivots); its last clause, "moving/rotating a member updates real contact and connected mass", is B7: the collider, the pins and the mass they connect go with the member, and nothing stays at its old pose.

### Integration

- **Works with.** Planned: EL-112 (points), EL-114 (feet), EL-115 (sibling). Delivered: `weight` on the delivered rope, for the load.
- **Campaign.** GAP-01 "Brace Yourself": the brace's introduction is the new level **9b**, after the Placeable pivot (9) its frame stands on (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); the beam's objective stays at 4; practice 10, reuse 18, 65, 138 ([campaign allocation](../requirements.md#campaign-gap-allocation)).
- **Lesson fixtures (S8).** Needed: the lesson locks the bay and leaves the brace to the player.
- **Coverage binding.** Section 7.

### Delivery

1. Prerequisites, in order: blocked on the engine programme (owner decision 3a); then ENGINE-ASSEMBLY-JOINTS and the 3a story `ENGINE-PARAMETER-BOUND-FRAMES`; then EL-112; then EL-114.
2. The Family slice "Pin-ended members": `elements/StructuralBrace/` and `elements/Linkage/` (project, declaration, scene, icon each), `CuriousContraptions.tests/PinEndedMemberTests.cs`, two solution lines until S3 lands, the art classifications. Registration is derived. Expected tier: data-only if the story leaves every probe row *present* with a test; otherwise engine.

**Disqualified as a Family slice if** the probe finds that either member needs a capability the other does not, or if the engine story's connect-time rule cannot seat a 0.996 m member between two existing points (then the bay is rebuilt square with a pulley-borne side load and a 1.118 m brace). Disqualified as a part if the story gives every member-to-member joint to connector parts only (then the brace becomes a point-ended member like the beam and frames need pin connectors).

## 6. Open questions

**Resolved under the delegation.**

1. Mid-span attachment: the brace mounts on any beam point, and a beam has three (`EndA`, `Mid`, `EndB`). No free-form or snapped positions along a member. Reason: a fixed socket count compiles to fixed tables.
2. Distinct kind or a beam in a diagonal role: a distinct catalogue entry (D8), because it carries mounts where the beam carries points; it shares a family with the linkage.
3. Closed loops: the engine story must prove one (section 3); this part adds no solver requirement of its own.

**Needs the owner.** None.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, one collider, two pin mounts, one parameter, one scale binding. There is no brace rule.
2. **Generic laws.** A revolute constraint at each made connection, rigid-body dynamics and contact. The missing capabilities are stated element-free, each with one owner: the same constraint between two separately placed parts, proven on a closed loop (ENGINE-ASSEMBLY-JOINTS), and a socket position that follows a parameter (`ENGINE-PARAMETER-BOUND-FRAMES`).
3. **Three layers.** Colliders: one box. Visuals: the bar with flush chevron and end faces; none is a collider. Animation: none; all art follows the body pose.
4. **Flow and typing.** Mounts whose typed id and role enum are the engine story's (a string name today); `length` is an f32 Real in metres.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (box); joints 0 declared, up to 2 made at connection time (revolute); drives 0; sensors 0; domain nodes 0; stores 0; mounts 2; presentation bindings 1 (parameter scale).
6. **Performance.** One primitive collider. A braced bay is one island. Island sleeping does not exist today; it is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a). The Weight in the rows hangs on the delivered rope, whose strand collider is owned by ENGINE-CONNECTION-MATERIALS (owner decision 1, 22 November 2026); this part draws no strand.
7. **Tests are scenarios.** Rows B1 to B7 run the production simulation on a built frame; B3 shows no path depends on the part's identity.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), ENGINE-ASSEMBLY-JOINTS and `ENGINE-PARAMETER-BOUND-FRAMES` for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final package-source-binding section in the identity's spec, read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries its heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `structural_brace` |
| Named identity | EL-113, `../invest/named-elements.md#element-113` |
| Acceptance | none available: the ledger entry has no todo link; the identity's own task is `sequence-task-666`, co-anchored with `element-113` |
| Decision | S327, `../invest/scope-corrections.md#s327` |
| Mode records | `Configuration=Adjustable` (`length`) |

What it lacks today: a todo link in the ledger entry, and an `s327` block in `scope-corrections.md` on published main.
