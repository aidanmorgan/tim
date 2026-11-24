# EL-112 · Structural beam — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-112 delivery slice](../invest/named-elements.md#element-112). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text (Batch M). Citations of the form `path:line` are to this checkout (published main `8d1d68aa`); `path@a6c914e` citations appear only in section 4 and describe the harvest baseline. Numbers marked **starting value** are hand calculations shown here and stand under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); nothing was simulated. All game values are f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-112 · Structural beam · Construction |
| Anchor | [requirements.md#element-112](../requirements.md#element-112) with its co-anchored `sequence-task-665`; [named-elements entry](../invest/named-elements.md#element-112); umbrella [gap-01](../requirements.md#gap-01) (index only) |
| Owner key | S326 |
| Related identities | [EL-113 Structural brace](EL-113-structural-brace.md) and [EL-115 Linkage connector](EL-115-linkage-connector.md) (pin-ended members that mount on a beam); [EL-114 Placeable pivot](EL-114-placeable-pivot.md) (the first part with a mount, and the static support); [EL-117 Axle](EL-117-axle.md) (a beam is the chassis); [EL-159](EL-159-tension-limited-connector.md), [EL-160](EL-160-shear-limited-connector.md), [EL-161](EL-161-bending-limited-connector.md) and [EL-185](EL-185-releasable-assembly-joint.md) (connectors that hold a beam end). No CAT refines it |
| Status | Not started; blocked on the engine programme (owner decision 3a), then ENGINE-ASSEMBLY-JOINTS and the 3a story `ENGINE-PARAMETER-BOUND-FRAMES` (section 3) |

### Disposition

**To deliver as a single element** after the engine programme (owner decision 3a: every element is blocked on the engine programme), ENGINE-ASSEMBLY-JOINTS and ENGINE-PARAMETER-BOUND-FRAMES. Planned package id `structural_beam`, directory `elements/StructuralBeam/`, title "Beam". It is the first attachable part, so it goes before EL-114 and EL-185. It is not a Family member: the brace and the linkage carry mounts and the beam carries only points (section 2).

## 2. Declaration

### Declaration to write

Closest delivered shapes: `elements/Wall/` (a box resized by a parameter, `WallElementDeclaration.cs:40`–`:45`) and `elements/Weight/` (a dynamic body whose mass, size and rope tie follow one parameter, `WeightElementDeclaration.cs:39`–`:60`). The attachment descriptors do not exist yet; their names and fields are fixed by the engine story's Code Map and this subsection is then corrected to them.

**Attachment model (starting; fixed by ENGINE-ASSEMBLY-JOINTS).** Sockets are built on `AnchorPointDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:63`), whose `Name` is a string today; the story gives every socket a typed id and a role enum. A *point* is a socket that declares nothing. A *mount* is a socket that declares the joint it makes (fixed or pin; the fixed joint law itself is owned by `ENGINE-JOINT-KINDS`) and, where stated, a commanded release or a load limit. A player-made connection joins one mount to one point of another placed part; several mounts may share one point (a frame corner). This is the model [EL-185](EL-185-releasable-assembly-joint.md) and [EL-186](EL-186-temporary-bridge.md) use.

**Which assembled bodies collide (stated once, here; the other Construction specs cite this paragraph).** The body that carries a mount and the body that carries the point it is joined to do not collide with each other, and neither do two bodies whose mounts share one point; every other pair collides. The rule belongs to ENGINE-ASSEMBLY-JOINTS, which compiles the jointed-pair set, and is not a property of any part. Its consequence for a rig: a body may overlap only the bodies it is attached to or shares a point with. Any other body of the same parts (an Axle's shaft beside the beam its collar holds) still collides and must be built clear, so each rig in these specs states its clearances.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` and `IParameterizedGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`, `:29`), and the attachment interface the engine story adds (missing).
- **Body.** One dynamic box, local X along the beam: `length` × 0.20 × 0.30 m, with one box collider of the same size. Half-extent X is bound to `length` × 0.5 (`ColliderParameterBinding`, `SceneGeometry.cs:27`). The 0.20 × 0.30 m section is slimmer than the Impact lever's beam and wide enough to read as structure beside a 0.68 m Basketball.
- **Mass and inertia (starting values).** Linear density 0.75 kg/m, so mass = 0.75·L (1.5 kg at 2 m): between the 1 kg Basketball and the Impact lever's 2 kg beam, so a 4 kg load visibly matters. Moments of a homogeneous box, as polynomials of `length` (degree three, which `MassScalar` admits, `CuriousContraptions.Abstractions/MassFrameValues.cs:8`):
  - about X: m·(0.20² + 0.30²)/12 = 0.008125·L;
  - about Y: m·(L² + 0.30²)/12 = 0.0625·L³ + 0.005625·L;
  - about Z: m·(L² + 0.20²)/12 = 0.0625·L³ + 0.0025·L.
  At L = 2: 0.01625, 0.51125 and 0.505 kg·m². Gravity (0, −9.81, 0) m/s²; linear drag 0.
- **Material.** Restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s: a matte member that does not bounce cargo.
- **Attachment points.** `EndA` at (−L/2, 0, 0), `EndB` at (+L/2, 0, 0) and `Mid` at (0, 0, 0). All three are points; the beam declares no mount. The two end positions follow `length`.
- **Rope ports.** `TieA`, `TieB` and `Tie` at the same three positions: `SimulationDomain.Rope`, bidirectional, attachment role Load, the end positions bound to `length` by `PortPositionBinding` (`CuriousContraptions.Abstractions/PortDescriptor.cs:77`). They let a Weight hang from a beam on a straight load path, which the EL-159 to EL-161 rigs use.
- **Parameter.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `length` | Real (f32) | Metre | 2.0 | 0.5 | 4.0 | Reject | BuildOnly |

  The 4 m maximum is the Ramp's (`elements/Ramp/RampElementDeclaration.cs:27`). Section and density are fixed (D13).
- **Stores, sensors, events, signal ports.** None.
- **Presentation.** `Visual` scale X ← `length` × 0.5; resize along X in Build, as the Ramp declares (`RampElementDeclaration.cs:10`–`:14`). No other binding: the art follows the body's pose.
- **Catalogue.** Id `structural_beam`, title "Beam", description "A rigid member. It stays up only where something holds it.", category `ElementCategory.Structure`, palette order 501 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cream `#fff8e9`.
- **Footprint.** Bodies 1, moving bodies 1, constraints 0.

### Art and solidity

| Node | Mesh | Layer that moves it | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Beam` | cream `#fff8e9` box 2.0 × 0.20 × 0.30 m, scaled in X | body pose; parameter scale | solid | the box collider |
| `Visual/Visual_Inset` | warm-wood `#c28f52` strip on the top and front faces, flush | body pose | decorative | printed flat |
| `Visual/Visual_EndA`, `Visual_EndB` | navy `#293954` end faces, flush | body pose | decorative | printed flat; a flat face does not distort when the beam is scaled |

The umbrella's gold connection collars are drawn by the parts that make a connection (connectors, axle), not by the beam: a collar on a scaled mesh would stretch. Icon: a long cream bar with two navy ends. It differs from the brace and the linkage by its plain square ends (they end in round gold eyes) and from the Wall by its length-to-section ratio.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Dynamic box body, contact and stable rest | present | `ColliderShapeKind.Box` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `WorkshopPhysicsLawCylinderTests.BoxesDroppedOntoAFaceStayPutAtRest` (`CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs:2829`) |
| A collider half-extent bound to a Real parameter | present (a static box and a dynamic sphere; a dynamic box is the same binding and is not separately tested) | `ColliderParameterBinding` (`SceneGeometry.cs:27`) | `WorkshopWallTests.InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip` (`WorkshopWallTests.cs:55`); `TennisBallTests.ConfiguredRadiusChangesContactSpinAtFixedMass` (`TennisBallTests.cs:137`) |
| Mass and principal moments as polynomials of one parameter | present | `MassScalar` (`MassFrameValues.cs:8`); `PrincipalMomentDeclaration.FromPolynomial` (`CuriousContraptions.Abstractions/PrincipalMomentDeclaration.cs:30`) | `WeightTests.TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter` (`WeightTests.cs:139`) |
| Rope ties on a moving body, position bound to a parameter | present | `PortPositionBinding` (`PortDescriptor.cs:77`) | `ImpactLeverTests.RopeLiftingIntegration_TieBLiftsArm` (`ImpactLeverTests.cs:516`); `RopeTests.SocketBindingRefusesUnknownParametersNonfiniteCoefficientsAndDefaultDisagreement` (`RopeTests.cs:526`) |
| Resize along one axis with art scaled from the parameter | present | `ElementPresentation` parameter rows (`RampElementDeclaration.cs:10`–`:14`) | `WorkshopWallTests.ExistingRampSelectionRingTracksResizeAndCanonicalRestore` (`WorkshopWallTests.cs:233`; name and line only, body not opened) |
| Attachment sockets declared on a part | present, untested | `AnchorPointDescriptor` (`MechanicalContractValues.cs:63`), `IMechanicalElement.AnchorPoints` (`MechanicalContracts.cs:12`): validated and bundled, never compiled; its `Name` is a string | ENGINE-ASSEMBLY-JOINTS builds on it or deletes it, and adds the typed id and role enum |
| A player-made connection between a mount and a point of two placed parts | missing | connection domains are Activation, Electrical, Rope and Mechanical only (`engine/gpu/WorkshopConnections.cs:11`) | ENGINE-ASSEMBLY-JOINTS |
| A socket position bound to a parameter | missing | `AnchorPointDescriptor` has no binding; ports have one (`PortDescriptor.cs:65`) | `ENGINE-PARAMETER-BOUND-FRAMES` ("a socket position and a revolute limit may each follow one declared Real parameter") |

Counts: 5 present, 1 present-untested, 0 narrower, 2 missing: the player-made connection (ENGINE-ASSEMBLY-JOINTS) and the parameter-bound socket position (`ENGINE-PARAMETER-BOUND-FRAMES`). Every row also waits for the engine programme (owner decision 3a).

## 4. Sources and legacy

- **Requirements.** Row: "Bounded rigid member transfers load through declared attachment sockets"; outcome "Unsupported member falls rather than floating at authored endpoints" ([element-112](../requirements.md#element-112)). Umbrella integration: a braced frame supports the load while an unbraced one deflects; no implicit world anchor; moving a member updates real contact and connected mass ([sequence-task-534](../requirements.md#sequence-task-534)).
- **Audit.** "Typed endpoints/materials and shared rigid assemblies. Compare a supported triangular frame with an unbraced control" (`docs/physics-puzzle-gap-audit.md@a6c914e:L80-L80`).
- **Legacy.** No beam part, level or test existed. Joint-framework facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy joint kinds were BallSocket, Hinge and Slider; there was no fixed (weld) kind. | `engine/physics/JointEquations.cs@a6c914e:L5-L5` | Carry forward as the reason a rigid attachment is new work. |
| 2 | A joint requires two distinct bodies, at least one dynamic, and an explicit connected-body collision policy. | `engine/physics/PhysicsJoint.cs@a6c914e:L16-L16`, `engine/physics/PhysicsJoint.cs@a6c914e:L33-L48` | Carry forward. |
| 3 | A collision-disabled body still receives gravity and joint reactions. | `CuriousContraptions.tests/PhysicsColliderUpdateTests.cs@a6c914e:L134-L148` | Carry forward as an acceptance fact. |
| 4 | Joint solve on a CPU `PhysicsWorld` with 1e-7 projection tolerances. | `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L48-L72` | Do not carry forward: CPU solver path and proof-grade tolerance. |

Files harvested: `engine/physics/JointEquations.cs`, `engine/physics/PhysicsJoint.cs`, `CuriousContraptions.tests/PhysicsColliderUpdateTests.cs`, `CuriousContraptions.tests/JointRangeTests.cs`.

## 5. Acceptance outline

### Behaviour and numbers

- **Laws.** Gravity, contact and friction on one rigid body; rope tension at its ties; the rigid or pin constraint of whatever mount holds one of its points. The beam has no behaviour of its own, so an unheld beam falls.
- **Fall.** Released 1.0 m above the bench (centre 0.9 m above its rest height) it lands after √(2·0.9/9.81) = 0.43 s at 4.2 m/s and rests with its centre 0.10 m above the bench.
- **Carried load (statics, g = 9.81 m/s²).** A 2 m beam (14.7 N) held at both ends with a 4 kg Weight (39.2 N) hung from `Tie` at mid-span: 27.0 N on each end. The same Weight hung from `TieB`: 46.6 N on end B and 7.4 N on end A.
- **Tick order.** No signal, optical or acoustic read is involved; everything happens in the mechanical substeps (Phase 3 of the [solver model](../../gpu-f32-physics.md#solver-model)).

### Acceptance rows

Test class `StructuralBeamTests`. "Holds" means the beam's centre moves less than 0.01 m over 5 s (a dropped end moves more than 0.5 m). Hung rigs are built clear of the bench: every rope is 1.5 m and the beam is at least 3.0 m above the bench (a Pivot's underside at 2.65 m puts its axis at 3.0 m), so the largest Weight, 8 kg and 0.5771 × (0.136734 × 8 + 0.453070) = 0.89 m from tie to base (`elements/Weight/WeightElementDeclaration.cs:39`–`:40`), ends 3.0 − 1.5 − 0.89 = 0.61 m above the bench. Stall guards 10 game seconds. Chrome proof is pending the post-catalogue phase.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given a beam placed 1.0 m above the bench with nothing holding it, when Run starts, then it falls and comes to rest on the bench (centre 0.10 m above it within 0.005 m, speed under 0.01 m/s) | `StructuralBeamTests.AnUnheldBeamFallsAndRests` | a Wall placed at the same height stays where it was authored | lengths 0.5 and 4.0 m |
| A2 | Given a level beam hung from two Rope anchors by `TieA` and `TieB` with a 4 kg Weight roped to `Tie`, when 5 s pass, then it holds | `StructuralBeamTests.ARopeHungBeamCarriesAHungWeight` (a cross-part row owned by the Rope anchor slice: the Weight and Rope are taught at 12b and the anchor at 12c, after the beam at 4; coordinator decisions 36 and 41) | cutting one rope drops that end more than 0.5 m within 2 s | an 8 kg Weight is also held |
| A3 | Given a beam held at `EndA` and `EndB` by the head mounts of two Pivots with a 4 kg Weight roped to `Tie`, when 5 s pass, then it holds; with one attachment removed in Build it swings down about the other | `PlaceablePivotTests.ABeamOnTwoPivotsCarriesItsLoadAndDropsWhenOneAttachmentIsRemoved` (a cross-part row run as EL-114 P6 and owned by the Weight slice, CAT-067: the Weight and Rope are taught at 12b, after the pivot at 9; coordinator decisions 36 and 41) | the removed attachment | the Weight on `TieB` instead of `Tie` |
| A4 | The compiled mass and moments follow `length`: 0.375, 1.5 and 3.0 kg and Y moments 0.010625, 0.51125 and 4.0225 kg·m² at 0.5, 2.0 and 4.0 m, within 1 part in 10⁵ (the polynomial is a mathematical contract) | `StructuralBeamTests.MassAndInertiaFollowLength` | — | both limits |
| A5 | A `length` below 0.5, above 4.0 or non-finite is refused atomically; an electrical or activation wire to a tie is refused | conformance suite rows 2 and 5 (S4) once it lands; until then `StructuralBeamTests.InvalidLengthAndForeignWiresAreRefused` | 0.5 and 4.0 are admitted | — |
| A6 | Reset restores pose, ropes and attachments; save and load round-trip `length`, pose and connections | `StructuralBeamTests.ResetAndSaveLoadRestoreTheBeam` | — | Reset while the beam is falling |

Requirement rows preserved: "unsupported member falls rather than floating at authored endpoints" (A1); "transfers load through declared attachment sockets" (A3); "no implicit world anchor" (A1 and the A3 control). The braced and unbraced comparison is [sequence-task-534](../requirements.md#sequence-task-534), run as rows B1 and B2 of [EL-113](EL-113-structural-brace.md), which the Weight slice ([CAT-067](CAT-067-weight.md)) owns.

### Integration

- **Works with.** Delivered: `rope_anchor`, `weight`, `wall`, `ramp`, every ball. Planned: EL-113, EL-114, EL-115, EL-117, EL-159 to EL-161, EL-185, EL-186.
- **Campaign.** GAP-01 "Brace Yourself": introduction 4 (the beam alone; the brace's objective is at 9b, after the pivot, coordinator decision 36), practice 5, reuse 18, 65, 138 ([campaign allocation](../requirements.md#campaign-gap-allocation)).
- **Lesson fixtures (S8).** Needed: the lessons lock a partly built structure.
- **Coverage binding.** Section 7.

### Delivery

1. Prerequisites, in order: blocked on the engine programme (owner decision 3a), then ENGINE-ASSEMBLY-JOINTS and the 3a story `ENGINE-PARAMETER-BOUND-FRAMES` (engine tier).
2. This slice: `elements/StructuralBeam/` (project, declaration, scene, icon), `CuriousContraptions.tests/StructuralBeamTests.cs`, the solution line until S3 lands, the art classification. Registration is derived. Expected tier: data-only if the story leaves every probe row *present* with a test; otherwise engine.
3. Rows A2 and A3 close in later slices: A2 in the Rope anchor slice ([CAT-058](CAT-058-rope_anchor.md)), A3 as EL-114 P6 in the Weight slice ([CAT-067](CAT-067-weight.md)); the ledger row stays open until then.

**Disqualified if** the engine story makes sockets symmetric (no mount and point roles): then the beam's three sockets take whatever role the story defines and the specs of this batch that mount on a beam are corrected together; or if `ENGINE-PARAMETER-BOUND-FRAMES` does not let a socket position follow a parameter (then `length` cannot be a parameter and the beam ships in fixed lengths as separate catalogue entries, which is an owner question).

## 6. Open questions

**Resolved under the delegation.**

1. What a beam end attaches to: any mount of another part. A beam never joins a beam directly; a part with a mount joins them. Reason: one rule for every connection, and no implicit world anchor.
2. Weld or pins only: both exist as mount kinds declared by the connecting part (EL-161 rigid; EL-113, EL-115, EL-159 and EL-160 pins). The beam declares neither.
3. Section and density: fixed (D13). Only `length` is a parameter.
4. Rope ties on a beam: yes, three, because the load-limited connector rigs and the hung-load rows use them (D19).
5. The legacy 16-body limit does not apply: tables are scene-sized at Run ([isolation authority](../../architecture/element-package-isolation.md)).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, one collider, three attachment points, three rope ports, one parameter and one scale binding. No beam rule exists or is asked for in engine code.
2. **Generic laws.** Rigid-body gravity and contact; rope tension; the constraint a connecting part's mount declares. The missing capabilities are stated element-free, each with one owner: a player-made mount-to-point connection (ENGINE-ASSEMBLY-JOINTS) and a socket position that follows a parameter (`ENGINE-PARAMETER-BOUND-FRAMES`).
3. **Three layers.** Colliders: one box. Visuals: the beam box with flush inset and end faces; none is a collider. Animation: none; every art node follows the body pose, and the X scale is an authored parameter, not motion.
4. **Flow and typing.** Rope-domain ports; attachment points, whose typed id and role enum are the engine story's (a string name today); `length` is an f32 Real in metres.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (box); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; attachment points 3; rope ports 3; presentation bindings 1 (parameter scale).
6. **Performance.** One primitive collider, as the Wall. A held frame is one island. Island sleeping does not exist today; it is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a), and nothing in this part works around it. The rope used in rows A2 and A3 is the delivered rope, whose strand collider is owned by ENGINE-CONNECTION-MATERIALS (owner decision 1, 22 November 2026); this part draws no strand. A 4 m beam falling at 4 m/s is a large slow body; tunnelling is excluded by the engine's speculative contacts, not by this part.
7. **Tests are scenarios.** Rows A1 to A6 run the production simulation; A4 is a local mathematical check of the declared polynomial.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), ENGINE-ASSEMBLY-JOINTS and `ENGINE-PARAMETER-BOUND-FRAMES` for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final package-source-binding section in the identity's spec, read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries its heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `structural_beam` |
| Named identity | EL-112, `../invest/named-elements.md#element-112` |
| Acceptance | none available: the ledger entry has no todo link; the identity's own task is `sequence-task-665`, co-anchored with `element-112` |
| Decision | S326, `../invest/scope-corrections.md#s326` |
| Mode records | `Configuration=Adjustable` (`length`) |

What it lacks today: a todo link in the ledger entry (the reader requires a todo with a co-anchored sequence task), and an `s326` block in `scope-corrections.md` on published main (that file has blocks for other keys, none for S326).
