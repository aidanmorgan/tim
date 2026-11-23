# EL-159 · Tension-limited connector — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-159 delivery slice](../invest/named-elements.md#element-159). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text (Batch M). Citations of the form `path:line` are to this checkout (published main `8d1d68aa`); `path@a6c914e` citations appear only in section 4. Numbers marked **starting value** are hand calculations shown here and stand under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); nothing was simulated. All game values are f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-159 · Tension-limited connector · Construction |
| Anchor | [requirements.md#element-159](../requirements.md#element-159) with its co-anchored `sequence-task-712`; [named-elements entry](../invest/named-elements.md#element-159); umbrella [gap-05](../requirements.md#gap-05) (index only) |
| Owner key | S680 |
| Related identities | Family siblings [EL-160 Shear-limited](EL-160-shear-limited-connector.md) and [EL-161 Bending-limited](EL-161-bending-limited-connector.md); [EL-112 Structural beam](EL-112-structural-beam.md) (what it holds); [EL-114 Placeable pivot](EL-114-placeable-pivot.md) (the static `Under` point it hangs from); [EL-185 Releasable assembly joint](EL-185-releasable-assembly-joint.md) (same body and mounts, released by a signal instead of a load); [EL-186 Temporary bridge](EL-186-temporary-bridge.md) (the same load-limit law inside one package). No CAT refines it |
| Status | Not started; blocked on the engine programme (owner decision 3a), then `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and ENGINE-FRACTURE (section 3) |

### Disposition

**Member of the Family slice "Load-limited connectors"** with EL-160 and EL-161. The three are one collar body with one permanent mount and one load-limited mount; they differ only in declared values: which load measure the limit applies to, the limit's unit, range and default, whether the limited mount is a pin or rigid, and the witness-mark art. Each keeps its own catalogue entry (D8) and acceptance rows. Planned package id `tension_connector`, directory `elements/TensionConnector/`, title "Tension connector". [Delivery batches](../invest/delivery-batches.md), section 1c, lists the three as a run; the batch summary proposes moving them to a family. The family is blocked on the engine programme (owner decision 3a: every element is blocked on the engine programme), then `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and ENGINE-FRACTURE.

## 2. Declaration

### Declaration to write

Closest delivered shape: `elements/Weight/` for a small dynamic body; the planned [EL-185](EL-185-releasable-assembly-joint.md) for the two mounts. The attachment model is the one stated in [EL-112](EL-112-structural-beam.md), section 2. Attachment and load-limit descriptors are fixed by the two engine stories' Code Maps and this subsection is then corrected to them.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the attachment and load-limit interfaces the engine stories add (missing).
- **Body.** One dynamic collar: a box 0.30 × 0.30 × 0.40 m with a box collider of the same size, local X the connector axis. It is the Axle's collar kit: 0.05 m proud of a beam's 0.20 × 0.30 m section, so the collar is visible around the member it holds. Mass 0.3 kg; moments m·(0.30² + 0.40²)/12 = 0.00625 kg·m² about X and Y, m·(0.30² + 0.30²)/12 = 0.0045 kg·m² about Z; gravity (0, −9.81, 0) m/s².
- **Material.** Restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s.
- **Mounts.** `MountA` at (−0.15, 0, 0): a rigid mount (a fixed joint), permanent for the Run. `MountB` at (+0.15, 0, 0): a **pin** mount (free axis local Z) carrying the load limit. The collar therefore stays on whatever `MountA` holds after a break, and nothing disappears. Which joined bodies collide is the one rule stated in [EL-112](EL-112-structural-beam.md), section 2.
- **Load limit on `MountB`.** Measure: **axial tension**, the component of the mount's reaction along the connector axis that pulls the two parts apart; compression and the transverse component are not counted. Limit: the parameter `tensile_strength`. Hold time: 0.10 s, a declared constant of the family. The mount's constraint is removed, once, for the rest of the Run, when the measure has exceeded the limit on every substep for the hold time; the settling transient at Run start does not count. This is ENGINE-FRACTURE's one rule for every load limit: each declares a measure from one closed set (axial tension, shear, bending moment, resultant, contact normal, contact impulse) and a hold time of zero or more.
  - **Why 0.10 s.** It is 48 substeps at 480 Hz: long enough that one solver spike cannot break a joint, short enough that an overloaded joint is seen to let go at once.
- **Parameter.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `tensile_strength` | Real (f32) | Newton (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:30`) | 60 | 5 | 200 | Reject | BuildOnly |

- **Ports, stores, sensors.** None. **Events.** The broken occurrence, if the engine story's read model exposes one.
- **Presentation.** Three witness marks take their X scale from the committed ratio of load to limit; the load band goes gold `#f7cb52` to slate `#556573` when broken. Both drivers are reads the fracture story adds (missing). The "real visible gap" after a break is the two parts actually separating.
- **Catalogue.** Id `tension_connector`, title "Tension connector", description "Holds two parts together until the pull between them is too great.", category `ElementCategory.Structure`, palette order 513 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cream `#fff8e9`.
- **Footprint.** Bodies 1, moving bodies 1.

### Art and solidity

| Node | Mesh | Layer that moves it | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Collar` | cream `#fff8e9` box 0.30 × 0.30 × 0.40 m | body pose | solid | the collar box |
| `Visual/Visual_Band` | gold band around the collar at the `MountB` end, flush | body pose; colour binding | decorative | printed flat |
| `Visual/Visual_Mark1` to `Visual_Mark3` | navy `#293954` bars lying **along** the axis on the top face, flush | body pose; scale binding | decorative | printed flat |

Icon: a collar with three marks along its length and an outward arrow at each end. The three connectors differ by the direction of their marks (along the axis, across it, in an arc), a shape difference; from the Release clamp by having no pin disc and no signal socket. The requirement's stress indication is shown at all times, at the marks' own scale (owner decision 24, 23 November 2026), for all three connectors; no selection-gated binding is needed.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Small dynamic box body | present | `ColliderShapeKind.Box` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `WorkshopPhysicsLawCylinderTests.BoxesDroppedOntoAFaceStayPutAtRest` (`CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs:2829`) |
| A hung load on a rope, for the rig | present | the Rope connection domain (`engine/gpu/WorkshopConnections.cs:11`) | `ImpactLeverTests.RopeLiftingIntegration_TieBLiftsArm` (`ImpactLeverTests.cs:516`) |
| A one-way topology change committed at a step and restored by Reset | narrower: a tensile span only, by a UI command; owner ENGINE-FRACTURE (one-way break committed at a step, restored by Reset) | `CutRope` (`CuriousContraptions.Abstractions/RuntimeServices.cs:77`) | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`RopeTests.cs:151`) |
| Attachment sockets declared on a part | present, untested | `AnchorPointDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:63`), never compiled | ENGINE-ASSEMBLY-JOINTS |
| A fixed joint between two bodies (`MountA`) | missing | constraint kinds are revolute, prismatic and tensile (`CoordinateKind`, `CuriousContraptions.Abstractions/PortDescriptor.cs:43`) | `ENGINE-JOINT-KINDS` |
| Mounts joined by the player to points of two placed parts | missing | — | ENGINE-ASSEMBLY-JOINTS |
| A declared load limit on an attachment, with a declared measure (axial tension here) and hold time, and a one-way break | missing | only a contact-integrity observer on one body exists (`IContactIntegrityElement`, `MechanicalContracts.cs:119`) | ENGINE-FRACTURE ("every load limit declares a measure from one closed set and a hold time of zero or more") |
| Committed load ratio and broken state as presentation drivers | missing | no such member of `PresentationDriver` (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | ENGINE-FRACTURE (shared acceptance: the read model is part of the story) |

Counts: 2 present, 1 present-untested, 1 narrower, 4 missing (1 owned by `ENGINE-JOINT-KINDS`, 1 by ENGINE-ASSEMBLY-JOINTS, 2 by ENGINE-FRACTURE). Every row also waits for the engine programme (owner decision 3a).

## 4. Sources and legacy

- **Requirements.** Row: "Declared tensile strength governs failure under axial load"; outcome "Subthreshold tension retains the joint" ([element-159](../requirements.md#element-159)). Integration: the same assembly survives below threshold and breaks above it; loss changes topology and load paths; debris bounded; Reset restores the exact pre-break graph ([sequence-task-538](../requirements.md#sequence-task-538)).
- **Audit.** "explicit finite breaking threshold and readable stress cue ... restore topology on Reset. Extends existing releasable bridges rather than replacing them" (`docs/physics-puzzle-gap-audit.md@a6c914e:L84-L84`).
- **Legacy.** No strength-limited joint existed. Topology facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Joint edits were explicit Attach/Replace/Detach; no upsert. | `engine/physics/PhysicsJointChange.cs@a6c914e:L5-L28` | Carry forward Detach as the break. |
| 2 | An impact-scheduled change released a rope while a missed impact left the load held. | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L84-L104` | Carry forward the positive and control shape. |
| 3 | Removing a joint released the load without replacing world or body. | `CuriousContraptions.tests/PhysicsJointUpdateTests.cs@a6c914e:L52-L69` | Carry forward. |
| 4 | A failed step restored already-committed joint and collider changes. | `CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L35-L54` | Carry forward atomicity; the CPU rollback mechanism does not carry forward. |

Files harvested: `engine/physics/PhysicsJointChange.cs`, `CuriousContraptions.tests/PhysicsImpactJointTests.cs`, `CuriousContraptions.tests/PhysicsJointUpdateTests.cs`.

## 5. Acceptance outline

### Behaviour and numbers

- **Law.** While attached, `MountB` is a pin between the collar and the held part. The engine measures the axial tension in that pin; when it has exceeded `tensile_strength` on every substep for 0.10 s the pin is removed at a step boundary with no impulse. Both bodies keep their velocities. No new body is created, so debris is bounded by construction.
- **Tension rig (geometry by hand).** A Pivot is placed with its underside 5.0 m above the bench. The connector's `MountA` is fixed to the Pivot's `Under` point with the axis vertical, so the collar hangs below the plinth from 5.0 m down to 4.70 m. `MountB` holds `EndA` of a 2 m beam (1.5 kg) that hangs straight down from 4.70 m to 2.70 m. A Weight is roped to the beam's `TieB` on a 1.5 m rope, taut at Run start, so the load is present from the first step and nothing is snatched; its tie is at 1.20 m. The 8 kg Weight is 0.5771 × (0.136734 × 8 + 0.453070) = 0.89 m from tie to base (`elements/Weight/WeightElementDeclaration.cs:39`–`:40`) and ends 0.31 m above the bench. Nothing passes through the plinth: the collar touches only the underside it is fixed to, and the beam and Weight hang below the collar.
- **Loads (statics, g = 9.81 m/s²).** Tension at `MountB` = (1.5 + m)·9.81 N: 14.7 N unloaded, 34.3 N with a 2 kg Weight, 93.2 N with an 8 kg Weight. Against the 60 N default these are 43 % under and 55 % over.
- **Not tension.** In the shear rig of [EL-160](EL-160-shear-limited-connector.md) an 8 kg Weight puts 85.8 N across this connector's axis and almost nothing along it: it must hold.
- **Margins and the hold time.** The loads are static, so the hold time decides only how long an excess must last. In T2 the 93.2 N load exceeds 60 N on every substep once settled and the pin goes about a tenth of a second later. A false break in T1 would need the 34.3 N load to ring above 60 N, 1.75 times its static value, on all 48 substeps of a tenth of a second.
- **Dropped loads.** A Weight that falls through slack before the rope bites loads the pin far above its weight for a moment, usually for less than the hold time; no row asserts either outcome.
- **Tick order.** The reaction is produced in the mechanical substeps (Phase 3 of the [solver model](../../gpu-f32-physics.md#solver-model)); the break takes effect at a step boundary. No signal is involved.

### Acceptance rows

Family test class `LoadLimitedConnectorTests`; these are the tension rows. "Holds" means the beam's `EndA` stays within 0.01 m of the collar for 5 s; "breaks" means it is more than 0.5 m away within 3 s. Loads are chosen well apart from the limit, never at it. Stall guards 10 game seconds. Chrome proof is pending the post-catalogue phase.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| T1 | Given the tension rig at the default limit with a 2 kg Weight (34.3 N), when 5 s pass, then it holds | `LoadLimitedConnectorTests.TensionBelowTheLimitIsHeld` | T2 | `tensile_strength` 200 holds the 8 kg Weight |
| T2 | Given the same rig with an 8 kg Weight (93.2 N), then it breaks: the beam and Weight fall, and the collar and Pivot stay where they were | `LoadLimitedConnectorTests.TensionAboveTheLimitBreaksTheConnector` | T1 | `tensile_strength` 5 breaks under the bare beam (14.7 N) |
| T3 | Given the shear rig with this connector and an 8 kg Weight at the connector end (85.8 N across the axis), then it holds | `LoadLimitedConnectorTests.ShearDoesNotBreakATensionConnector` | T2 | — |
| T4 | A broken connector stays broken for the rest of the Run and creates no body; Reset restores the attachment, the poses and the marks | `LoadLimitedConnectorTests.ABreakIsPermanentUntilReset` | — | Reset while the beam is falling |
| T5 | Save and load round-trip `tensile_strength`, pose and both attachments; a construction saved after a break loads intact, and breaks again when run | `LoadLimitedConnectorTests.SaveLoadKeepsTheAuthoredConnector` | — | limits 5 and 200 |
| T6 | A `tensile_strength` below 5, above 200 or non-finite is refused atomically; a wire to a mount is refused | conformance suite rows 2 and 5 (S4) once it lands; until then `LoadLimitedConnectorTests.InvalidLimitsAndForeignWiresAreRefused` | — | — |

Requirement rows preserved: "declared tensile strength governs failure under axial load" (T1, T2); "subthreshold tension retains the joint" (T1). T1 with T2, T4 and T5 are this connector's share of [sequence-task-538](../requirements.md#sequence-task-538): survives below, breaks above, topology and load path change (the beam falls, the collar stays), debris bounded (T4), Reset restores the pre-break graph (T4).

### Integration

- **Works with.** Planned: EL-112, EL-114, EL-185, EL-186. Delivered: `weight` on the delivered rope.
- **Campaign.** GAP-05 "The Last Straw": introduction 43, practice 44, reuse 65, 96, 139 ([campaign allocation](../requirements.md#campaign-gap-allocation)).
- **Lesson fixtures (S8).** Needed: the lesson locks the structure and the load.
- **Coverage binding.** Section 7.

### Delivery

1. Prerequisites, in order: blocked on the engine programme (owner decision 3a); then `ENGINE-JOINT-KINDS` (the fixed joint), ENGINE-ASSEMBLY-JOINTS, and ENGINE-FRACTURE after them (its stated order).
2. EL-112 and EL-114.
3. The Family slice "Load-limited connectors": `elements/TensionConnector/`, `elements/ShearConnector/`, `elements/BendingConnector/` (project, declaration, scene, icon each), `CuriousContraptions.tests/LoadLimitedConnectorTests.cs`, three solution lines until S3 lands, the art classifications. Expected tier: data-only if every probe row is *present* with a test by then; otherwise engine.

**Disqualified as a Family slice if** ENGINE-FRACTURE offers one measure only (then the measure, not the part, is missing and the story is amended first). Disqualified as a part if the story puts load limits on player-made connections with no body (then each connector becomes a connection kind and its collar is the connection's artwork, as EL-185 notes for itself).

## 6. Open questions

**Resolved under the delegation.**

1. Load measure and "sustained": one rule in ENGINE-FRACTURE for every load limit, a measure from a closed set and a declared hold time. This family declares 0.10 s. The rows need only that a static load 43 % under holds and one 55 % over breaks.
2. Pin or rigid: a pin. Reason: tension needs no moment capacity; the bending connector is the rigid one.
3. Presets or a continuous range: a continuous Real, like every delivered parameter (D11). The earlier stepped presets are dropped.
4. Body or bodiless joint: a small solid collar (visible = solid), as EL-185.
5. Stress cue: the three witness marks are shown at all times, for all three connectors (owner decision 24, 23 November 2026).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, one collider, two mounts, one declared limit with its measure, one parameter, two kinds of binding.
2. **Generic laws.** A fixed joint and a pin attachment; a one-way break when a declared measure of the reaction has exceeded a declared limit for a declared hold time. All are missing and stated element-free, each with one owner: the fixed joint (`ENGINE-JOINT-KINDS`), player-made connections (ENGINE-ASSEMBLY-JOINTS), the limit, its measure, its hold and the read model (ENGINE-FRACTURE). Nothing breaks because of the part's name.
3. **Three layers.** Colliders: one box. Visuals: collar, flush band and marks. Animation: mark scale and band colour, bound to committed load ratio and broken state; they read and never write, which is the requirement's "regardless of display animation".
4. **Flow and typing.** No ports; the limit is an f32 Real in newtons; the measure is a closed enum value of the engine's.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (box); attachments up to 2 when connected (1 fixed, 1 pin with a limit); drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 4 (three scales, one colour).
6. **Performance.** One primitive collider. A break changes the constraint set at a step boundary, which the stories must do inside the compiled tables with no allocation during a tick (batch J's addition). Island sleeping does not exist today; it is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a). The rig's rope is the delivered rope, whose strand collider is owned by ENGINE-CONNECTION-MATERIALS (owner decision 1, 22 November 2026).
7. **Tests are scenarios.** Rows T1 to T6 run the production simulation on hand-computed static loads.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and ENGINE-FRACTURE for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final package-source-binding section in the identity's spec, read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries its heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `tension_connector` |
| Named identity | EL-159, `../invest/named-elements.md#element-159` |
| Acceptance | none available: the ledger entry has no todo link; the identity's own task is `sequence-task-712`, co-anchored with `element-159` |
| Decision | S680, `../invest/scope-corrections.md#s680` |
| Mode records | `Configuration=Adjustable` (`tensile_strength`) |

What it lacks today: a todo link in the ledger entry, and an `s680` block in `scope-corrections.md` on published main.
