# EL-160 · Shear-limited connector — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-160 delivery slice](../invest/named-elements.md#element-160). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text (Batch M). Citations of the form `path:line` are to this checkout (published main `8d1d68aa`); `path@a6c914e` citations appear only in section 4. Numbers marked **starting value** are hand calculations shown here and stand under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); nothing was simulated. All game values are f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-160 · Shear-limited connector · Construction |
| Anchor | [requirements.md#element-160](../requirements.md#element-160) with its co-anchored `sequence-task-713`; [named-elements entry](../invest/named-elements.md#element-160); umbrella [gap-05](../requirements.md#gap-05) (index only) |
| Owner key | S681 |
| Related identities | Family siblings [EL-159 Tension-limited](EL-159-tension-limited-connector.md) and [EL-161 Bending-limited](EL-161-bending-limited-connector.md); [EL-112 Structural beam](EL-112-structural-beam.md); [EL-114 Placeable pivot](EL-114-placeable-pivot.md); [EL-186 Temporary bridge](EL-186-temporary-bridge.md). No CAT refines it |
| Status | Not started; blocked on the engine programme (owner decision 3a), then `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and ENGINE-FRACTURE (section 3) |

### Disposition

**Member of the Family slice "Load-limited connectors"** with EL-159 and EL-161 (the family is described in [EL-159](EL-159-tension-limited-connector.md), section 1). Its declared differences from the tension connector are the load measure (shear), the parameter's name and the direction of its witness marks. Its own catalogue entry (D8) and acceptance rows. Planned package id `shear_connector`, directory `elements/ShearConnector/`, title "Shear connector". Blocked on the engine programme (owner decision 3a: every element is blocked on the engine programme), then `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and ENGINE-FRACTURE.

## 2. Declaration

### Declaration to write

The attachment model is the one stated in [EL-112](EL-112-structural-beam.md), section 2. Attachment and load-limit descriptors are fixed by the two engine stories' Code Maps.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the attachment and load-limit interfaces the engine stories add (missing).
- **Body.** The family's collar: one dynamic box 0.30 × 0.30 × 0.40 m with a box collider of the same size, local X the connector axis; mass 0.3 kg; moments m·(0.30² + 0.40²)/12 = 0.00625 kg·m² about X and Y and m·(0.30² + 0.30²)/12 = 0.0045 kg·m² about Z; gravity (0, −9.81, 0) m/s².
- **Material.** Restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s.
- **Mounts.** `MountA` at (−0.15, 0, 0): rigid (a fixed joint), permanent for the Run. `MountB` at (+0.15, 0, 0): a **pin** mount (free axis local Z) carrying the load limit.
- **Load limit on `MountB`.** Measure: **shear**, the magnitude of the component of the mount's reaction perpendicular to the connector axis. The axial component, in tension or compression, is not counted. Limit: the parameter `shear_strength`. Hold time: 0.10 s, the family's declared constant ([EL-159](EL-159-tension-limited-connector.md), section 2). The break is one-way for the rest of the Run.
- **Parameter.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `shear_strength` | Real (f32) | Newton (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:30`) | 60 | 5 | 200 | Reject | BuildOnly |

- **Ports, stores, sensors.** None. **Events.** The broken occurrence, if the read model exposes one.
- **Presentation.** Three witness marks scale with the committed ratio of load to limit; the band goes gold `#f7cb52` to slate `#556573` when broken. Both drivers are missing (section 3).
- **Catalogue.** Id `shear_connector`, title "Shear connector", description "Holds two parts together until the load across it is too great.", category `ElementCategory.Structure`, palette order 514 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cream `#fff8e9`.
- **Footprint.** Bodies 1, moving bodies 1.

### Art and solidity

| Node | Mesh | Layer that moves it | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Collar` | cream `#fff8e9` box 0.30 × 0.30 × 0.40 m | body pose | solid | the collar box |
| `Visual/Visual_Band` | gold band around the collar at the `MountB` end, flush | body pose; colour binding | decorative | printed flat |
| `Visual/Visual_Mark1` to `Visual_Mark3` | navy `#293954` bars lying **across** the axis on the top face, flush | body pose; scale binding | decorative | printed flat |

Icon: a collar with three marks across it and two opposed sideways arrows. It differs from its siblings by the direction of the marks. The stress indication is shown at all times (owner decision 24, 23 November 2026), as for the family in [EL-159](EL-159-tension-limited-connector.md).

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Small dynamic box body | present | `ColliderShapeKind.Box` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `WorkshopPhysicsLawCylinderTests.BoxesDroppedOntoAFaceStayPutAtRest` (`CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs:2829`) |
| A hung load on a rope, for the rig | present | the Rope connection domain (`engine/gpu/WorkshopConnections.cs:11`) | `ImpactLeverTests.RopeLiftingIntegration_TieBLiftsArm` (`ImpactLeverTests.cs:516`) |
| A one-way topology change restored by Reset | narrower: a tensile span only; owner ENGINE-FRACTURE (one-way break committed at a step, restored by Reset) | `CutRope` (`CuriousContraptions.Abstractions/RuntimeServices.cs:77`) | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`RopeTests.cs:151`) |
| Attachment sockets declared on a part | present, untested | `AnchorPointDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:63`), never compiled | ENGINE-ASSEMBLY-JOINTS |
| A fixed joint between two bodies (`MountA`) | missing | constraint kinds are revolute, prismatic and tensile (`CoordinateKind`, `CuriousContraptions.Abstractions/PortDescriptor.cs:43`) | `ENGINE-JOINT-KINDS` |
| Mounts joined by the player to points of two placed parts | missing | — | ENGINE-ASSEMBLY-JOINTS |
| A declared load limit with a declared measure (shear here: the reaction resolved against the socket's declared axis) and hold time, and a one-way break | missing | only a contact-integrity observer exists (`IContactIntegrityElement`, `MechanicalContracts.cs:119`) | ENGINE-FRACTURE ("every load limit declares a measure from one closed set and a hold time of zero or more") |
| Committed load ratio and broken state as presentation drivers | missing | no such member of `PresentationDriver` (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | ENGINE-FRACTURE |

Counts: 2 present, 1 present-untested, 1 narrower, 4 missing (1 owned by `ENGINE-JOINT-KINDS`, 1 by ENGINE-ASSEMBLY-JOINTS, 2 by ENGINE-FRACTURE). Every row also waits for the engine programme (owner decision 3a).

## 4. Sources and legacy

- **Requirements.** Row: "Declared transverse strength governs failure under shear load"; outcome "Pure supported axial load does not silently count as shear" ([element-160](../requirements.md#element-160)). Integration as [sequence-task-538](../requirements.md#sequence-task-538).
- **Audit.** "measurable load, explicit finite breaking threshold" (`docs/physics-puzzle-gap-audit.md@a6c914e:L84-L84`).
- **Legacy.** No strength-limited joint existed.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Radial and axial impulses through a hinge are carried by its bilateral rows, so the reaction has separable axial and transverse parts. | `CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L61-L70` | Carry forward: the reaction is the measured quantity. |
| 2 | Explicit Detach as a topology edit. | `engine/physics/PhysicsJointChange.cs@a6c914e:L5-L28` | Carry forward as the break. |

Files harvested: `CuriousContraptions.tests/SharedHingeMechanicsTests.cs`, `engine/physics/PhysicsJointChange.cs`.

## 5. Acceptance outline

### Behaviour and numbers

- **Law.** As EL-159, with the measure taken across the axis instead of along it.
- **Shear rig (geometry by hand).** A Pivot is placed with its underside 2.75 m above the bench. The connector's `MountA` is fixed to that Pivot's `Side` point with the axis horizontal, so the collar stands out from the plinth's side face, from x = 0.20 to 0.50 m at a height of 3.0 m. `MountB` holds `EndA` of a level 2 m beam (1.5 kg, x = 0.50 to 2.50 m). The beam's `EndB` is held by the head mount of a second Pivot, whose axis is at (2.50, 3.0) m and whose plinth stands 0.40 m behind the beam's plane, so the far end is a true pin support and each end carries exactly its statical share. A Weight is roped to one of the beam's ties on a 1.5 m rope, taut at Run start, so its tie is at 1.50 m. The 8 kg Weight (0.89 m from tie to base, radius 0.54 m; `elements/Weight/WeightElementDeclaration.cs:39`–`:40`, `:59`) spans 0.61 to 1.50 m: 1.25 m below the first plinth's underside, 1.15 m below the second's, and clear of the bench.
- **Loads (statics, g = 9.81 m/s²).** The pin carries half the beam, 7.4 N, plus the Weight's share. Weight on `TieA`, directly under the pin: 7.4 + 9.81·m, which is 27.0 N for 2 kg and 85.8 N for 8 kg. The same 8 kg Weight on `Tie` at mid-span: 7.4 + 39.2 = 46.6 N. Against the 60 N default: 55 % under, 43 % over, and 22 % under. All of it is vertical, across the horizontal axis.
- **Margins and the hold time.** The loads are static. Row S3 is the closest: 46.6 N is 22 % under, so a false break would need the load to ring above 60/46.6 = 1.29 times its static value on all 48 substeps of the 0.10 s hold.
- **Not shear.** In the tension rig of [EL-159](EL-159-tension-limited-connector.md) an 8 kg Weight pulls 93.2 N along the axis and nothing across it: this connector must hold.
- **Tick order.** Mechanical substeps (Phase 3 of the [solver model](../../gpu-f32-physics.md#solver-model)); the break takes effect at a step boundary.

### Acceptance rows

Family test class `LoadLimitedConnectorTests`; these are the shear rows. "Holds" and "breaks" as in EL-159: within 0.01 m for 5 s, or more than 0.5 m apart within 3 s. Stall guards 10 game seconds. Chrome proof is pending the post-catalogue phase.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| S1 | Given the shear rig at the default limit with a 2 kg Weight on `TieA` (27.0 N), when 5 s pass, then it holds | `LoadLimitedConnectorTests.ShearBelowTheLimitIsHeld` | S2 | `shear_strength` 200 holds the 8 kg Weight |
| S2 | Given an 8 kg Weight on `TieA` (85.8 N), then it breaks: the beam's `EndA` drops as the beam swings down about the far Pivot, and the collar and the first Pivot stay | `LoadLimitedConnectorTests.ShearAboveTheLimitBreaksTheConnector` | S1 | `shear_strength` 5 breaks under the bare beam (7.4 N) |
| S3 | Given the same 8 kg Weight moved to `Tie` at mid-span (46.6 N), then it holds: where the load sits decides | `LoadLimitedConnectorTests.TheSameLoadFurtherFromTheConnectorIsHeld` | S2 | — |
| S4 | Given the tension rig with this connector and an 8 kg Weight (93.2 N along the axis), then it holds | `LoadLimitedConnectorTests.AxialLoadDoesNotBreakAShearConnector` | S2 | — |
| S5 | A break is permanent until Reset and creates no body; Reset restores attachment, poses and marks; save and load round-trip `shear_strength`, pose and attachments | `LoadLimitedConnectorTests.ABreakIsPermanentUntilReset`, `.SaveLoadKeepsTheAuthoredConnector` (theories over the family) | — | limits 5 and 200 |
| S6 | A `shear_strength` below 5, above 200 or non-finite is refused atomically; a wire to a mount is refused | conformance suite rows 2 and 5 (S4) once it lands; until then `LoadLimitedConnectorTests.InvalidLimitsAndForeignWiresAreRefused` | — | — |

Requirement rows preserved: "declared transverse strength governs failure under shear load" (S1, S2); "pure supported axial load does not silently count as shear" (S4). Its share of sequence-task-538: S1, S2 and S5.

### Integration

- **Works with.** Planned: EL-112, EL-114, EL-186. Delivered: `weight` on the delivered rope.
- **Campaign.** GAP-05 "The Last Straw": introduction 43, practice 44, reuse 65, 96, 139 ([campaign allocation](../requirements.md#campaign-gap-allocation)).
- **Lesson fixtures (S8).** Needed.
- **Coverage binding.** Section 7.

### Delivery

As [EL-159](EL-159-tension-limited-connector.md). Prerequisites, in order: blocked on the engine programme (owner decision 3a), `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS, ENGINE-FRACTURE, EL-112, EL-114, then the Family slice "Load-limited connectors" (`elements/ShearConnector/` with its two siblings, one test class). Expected tier: data-only if every probe row is *present* with a test by then; otherwise engine.

**Disqualified if** ENGINE-FRACTURE measures only the resultant reaction: then tension and shear cannot be told apart, row S4 fails by design and the story is amended before this family is planned.

## 6. Open questions

**Resolved under the delegation.**

1. One limit per connector or combined limits: one. Reason: the requirement names three separate identities and the lesson is telling the load kinds apart (D19).
2. Presets or a range: a continuous Real (D11).
3. Dropped loads: not asserted, for the reason given in EL-159, section 5.
4. The far support: a second Pivot, not a Wall. A beam resting on a Wall is carried somewhere along the Wall's top, so the pin's share would not be the 7.4 N the rows state; a pin at `EndB` makes it exact.

5. Stress cue: shown at all times, for the whole family (owner decision 24, 23 November 2026).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, one collider, two mounts, one declared limit with its measure, one parameter, bindings.
2. **Generic laws.** A fixed joint, a pin attachment and a one-way break on a declared measure, limit and hold time; all missing, stated element-free, one owner each (`ENGINE-JOINT-KINDS`; ENGINE-ASSEMBLY-JOINTS; ENGINE-FRACTURE).
3. **Three layers.** Colliders: one box. Visuals: collar, flush band and marks. Animation: mark scale and band colour from committed state; nothing is written back.
4. **Flow and typing.** No ports; the limit is an f32 Real in newtons; the measure is a closed enum value of the engine's.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (box); attachments up to 2 when connected; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 4.
6. **Performance.** One primitive collider; a break changes the constraint set at a step boundary without allocation (the stories' duty). Island sleeping does not exist today; it is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a). The rig's rope is the delivered rope, whose strand collider is owned by ENGINE-CONNECTION-MATERIALS (owner decision 1, 22 November 2026).
7. **Tests are scenarios.** Rows S1 to S6 run the production simulation on hand-computed static loads.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and ENGINE-FRACTURE for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final package-source-binding section in the identity's spec, read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries its heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `shear_connector` |
| Named identity | EL-160, `../invest/named-elements.md#element-160` |
| Acceptance | none available: the ledger entry has no todo link; the identity's own task is `sequence-task-713`, co-anchored with `element-160` |
| Decision | S681, `../invest/scope-corrections.md#s681` |
| Mode records | `Configuration=Adjustable` (`shear_strength`) |

What it lacks today: a todo link in the ledger entry, and an `s681` block in `scope-corrections.md` on published main.
