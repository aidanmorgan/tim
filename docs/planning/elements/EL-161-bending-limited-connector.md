# EL-161 · Bending-limited connector — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-161 delivery slice](../invest/named-elements.md#element-161). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text (Batch M). Citations of the form `path:line` are to this checkout (published main `8d1d68aa`); `path@a6c914e` citations appear only in section 4. Numbers marked **starting value** are hand calculations shown here and stand under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions); nothing was simulated. All game values are f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-161 · Bending-limited connector · Construction |
| Anchor | [requirements.md#element-161](../requirements.md#element-161) with its co-anchored `sequence-task-714`; [named-elements entry](../invest/named-elements.md#element-161); umbrella [gap-05](../requirements.md#gap-05) (index only) |
| Owner key | S682 |
| Related identities | Family siblings [EL-159 Tension-limited](EL-159-tension-limited-connector.md) and [EL-160 Shear-limited](EL-160-shear-limited-connector.md); [EL-112 Structural beam](EL-112-structural-beam.md); [EL-114 Placeable pivot](EL-114-placeable-pivot.md). No CAT refines it |
| Status | Not started; blocked on the engine programme (owner decision 3a), then `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and ENGINE-FRACTURE (section 3) |

### Disposition

**Member of the Family slice "Load-limited connectors"** with EL-159 and EL-160 (the family is described in [EL-159](EL-159-tension-limited-connector.md), section 1). Its declared differences: the limited mount is rigid, the load measure is bending moment, the parameter is in newton metres and the witness marks form an arc. It is also the batch's plain rigid joint: at its maximum capacity it holds a beam as a cantilever under every load in the rows. Its own catalogue entry (D8) and acceptance rows. Planned package id `bending_connector`, directory `elements/BendingConnector/`, title "Bending connector". Blocked on the engine programme (owner decision 3a: every element is blocked on the engine programme), then `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and ENGINE-FRACTURE.

## 2. Declaration

### Declaration to write

The attachment model is the one stated in [EL-112](EL-112-structural-beam.md), section 2. Attachment and load-limit descriptors are fixed by the two engine stories' Code Maps.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the attachment and load-limit interfaces the engine stories add (missing).
- **Body.** The family's collar: one dynamic box 0.30 × 0.30 × 0.40 m with a box collider of the same size, local X the connector axis; mass 0.3 kg; moments m·(0.30² + 0.40²)/12 = 0.00625 kg·m² about X and Y and m·(0.30² + 0.30²)/12 = 0.0045 kg·m² about Z; gravity (0, −9.81, 0) m/s².
- **Material.** Restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s.
- **Mounts.** `MountA` at (−0.15, 0, 0): rigid (a fixed joint), permanent for the Run. `MountB` at (+0.15, 0, 0): a **rigid** mount (a fixed joint) carrying the load limit. A fixed joint transmits moment, which a pin cannot.
- **Load limit on `MountB`.** Measure: **bending moment**, the magnitude of the mount's reaction torque about the two axes perpendicular to the connector axis. Torsion about the axis and every force component are not counted. Limit: the parameter `moment_capacity`. Hold time: 0.10 s, the family's declared constant ([EL-159](EL-159-tension-limited-connector.md), section 2). The break is one-way for the rest of the Run.
- **Parameter.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `moment_capacity` | Real (f32) | NewtonMetre (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:29`) | 45 | 2 | 100 | Reject | BuildOnly |

- **Ports, stores, sensors.** None. **Events.** The broken occurrence, if the read model exposes one.
- **Presentation.** Three witness marks scale with the committed ratio of moment to capacity; the band goes gold `#f7cb52` to slate `#556573` when broken. Both drivers are missing (section 3).
- **Catalogue.** Id `bending_connector`, title "Bending connector", description "Holds a part rigidly until the bending at the joint is too great.", category `ElementCategory.Structure`, palette order 515 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cream `#fff8e9`.
- **Footprint.** Bodies 1, moving bodies 1.

### Art and solidity

| Node | Mesh | Layer that moves it | Class | Collider or reason |
| --- | --- | --- | --- | --- |
| `Visual/Visual_Collar` | cream `#fff8e9` box 0.30 × 0.30 × 0.40 m | body pose | solid | the collar box |
| `Visual/Visual_Band` | gold band around the collar at the `MountB` end, flush | body pose; colour binding | decorative | printed flat |
| `Visual/Visual_Mark1` to `Visual_Mark3` | navy `#293954` bars set in an **arc** on the front face, flush | body pose; scale binding | decorative | printed flat |

Icon: a collar with three marks in an arc and a curved arrow. It differs from its siblings by the arc. The stress indication is shown at all times (owner decision 24, 23 November 2026), as for the family in [EL-159](EL-159-tension-limited-connector.md).

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Small dynamic box body | present | `ColliderShapeKind.Box` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `WorkshopPhysicsLawCylinderTests.BoxesDroppedOntoAFaceStayPutAtRest` (`CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs:2829`) |
| A hung load on a rope, for the rig | present | the Rope connection domain (`engine/gpu/WorkshopConnections.cs:11`) | `ImpactLeverTests.RopeLiftingIntegration_TieBLiftsArm` (`ImpactLeverTests.cs:516`) |
| A one-way topology change restored by Reset | narrower: a tensile span only | `CutRope` (`CuriousContraptions.Abstractions/RuntimeServices.cs:77`) | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`RopeTests.cs:151`); owner `ENGINE-FRACTURE` (one-way release of attachments; engine-stories-index section 2) |
| Attachment sockets declared on a part | present, untested | `AnchorPointDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:63`), never compiled | ENGINE-ASSEMBLY-JOINTS |
| A fixed joint between two bodies that holds a loaded 2 m cantilever without visible sag | missing | constraint kinds are revolute, prismatic and tensile (`CoordinateKind`, `CuriousContraptions.Abstractions/PortDescriptor.cs:43`) | `ENGINE-JOINT-KINDS` ("a fixed joint holds a 2 m, 1.5 kg member carrying 4 kg at its free end within 0.02 m for 5 s") |
| Mounts joined by the player to points of two placed parts | missing | — | ENGINE-ASSEMBLY-JOINTS |
| A declared load limit with a declared measure (bending moment here: the reaction torque resolved against the socket's declared axis) and hold time, and a one-way break | missing | only a contact-integrity observer exists (`IContactIntegrityElement`, `MechanicalContracts.cs:119`) | ENGINE-FRACTURE ("every load limit declares a measure from one closed set and a hold time of zero or more") |
| Committed load ratio and broken state as presentation drivers | missing | no such member of `PresentationDriver` (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | ENGINE-FRACTURE |

Counts: 2 present, 1 present-untested, 1 narrower, 4 missing (1 owned by `ENGINE-JOINT-KINDS`, 1 by ENGINE-ASSEMBLY-JOINTS, 2 by ENGINE-FRACTURE). Every row also waits for the engine programme (owner decision 3a).

## 4. Sources and legacy

- **Requirements.** Row: "Declared moment capacity governs failure under bending"; outcome "Subthreshold moment retains the joint regardless of display animation" ([element-161](../requirements.md#element-161)). Integration as [sequence-task-538](../requirements.md#sequence-task-538).
- **Audit.** "explicit finite breaking threshold and readable stress cue" (`docs/physics-puzzle-gap-audit.md@a6c914e:L84-L84`).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | No weld kind among legacy joints. | `engine/physics/JointEquations.cs@a6c914e:L5-L5` | Carry forward as the reason a rigid attachment is new work. |
| 2 | A zero-width travel range locks the remaining freedom. | `CuriousContraptions.tests/JointRangeTests.cs@a6c914e:L89-L102` | Do not carry forward as the mechanism: the engine story delivers one rigid attachment, not a locked hinge pair. |
| 3 | Balanced loads cancel and an off-centre load produces moment about a hinge. | `CuriousContraptions.tests/SharedHingeMechanicsTests.cs@a6c914e:L47-L59` | Carry forward: the moment arm decides the load, so load position matters in acceptance. |

Files harvested: `engine/physics/JointEquations.cs`, `CuriousContraptions.tests/JointRangeTests.cs`, `CuriousContraptions.tests/SharedHingeMechanicsTests.cs`.

## 5. Acceptance outline

### Behaviour and numbers

- **Law.** While attached, `MountB` holds the two parts' frames together. The engine measures the bending moment in that attachment; when it has exceeded `moment_capacity` on every substep for 0.10 s the attachment is removed at a step boundary with no impulse.
- **Cantilever rig (geometry by hand).** A Pivot is placed with its underside 2.75 m above the bench. The connector's `MountA` is fixed to the Pivot's `Side` point with the axis horizontal, so the collar stands out from the plinth's side face, from x = 0.20 to 0.50 m at a height of 3.0 m. `MountB` holds `EndA` of a level 2 m beam (1.5 kg, x = 0.50 to 2.50 m) that is supported nowhere else. A Weight is roped to `Tie` (1.0 m out) or `TieB` (2.0 m out) on a 1.5 m rope, taut at Run start, so its tie is at 1.50 m. The 4 kg Weight (0.58 m from tie to base, radius 0.35 m; `elements/Weight/WeightElementDeclaration.cs:39`–`:40`, `:59`) spans 0.92 to 1.50 m: 1.25 m below the plinth's underside and clear of the bench.
- **Loads (statics, g = 9.81 m/s²).** The beam alone: 1.5·9.81·1.0 = 14.7 N·m. A Weight of mass m at arm d adds 9.81·m·d.

  | Load | Moment at `MountB` | Against the 45 N·m default |
  | --- | --- | --- |
  | 1 kg on `Tie` | 14.7 + 9.8 = 24.5 N·m | 46 % under |
  | 2 kg on `Tie` | 14.7 + 19.6 = 34.3 N·m | 24 % under |
  | 2 kg on `TieB` | 14.7 + 39.2 = 53.9 N·m | 20 % over |
  | 4 kg on `TieB` | 14.7 + 78.5 = 93.2 N·m | 107 % over |

  The last line is the figure the fixed joint itself must carry: 1.5 × 9.81 × 1 + 4 × 9.81 × 2 = 14.7 + 78.5 = 93.2 N·m at the root.
- **Margins and the hold time.** The loads are static. Row M3 is the closest on both sides: 34.3 N·m is 24 % under, so a false break would need the moment to ring above 45/34.3 = 1.31 times its static value on all 48 substeps of the 0.10 s hold; 53.9 N·m is 20 % over, and a ring that dips below 45/53.9 = 0.83 of static only delays the break until the ring dies.

- **Not bending.** In the tension rig of [EL-159](EL-159-tension-limited-connector.md) the beam hangs straight below the mount: 93.2 N of pull with an 8 kg Weight and no moment. This connector must hold.
- **Tick order.** Mechanical substeps (Phase 3 of the [solver model](../../gpu-f32-physics.md#solver-model)); the break takes effect at a step boundary. Presentation reads the committed ratio afterwards and writes nothing back.

### Acceptance rows

Family test class `LoadLimitedConnectorTests`; these are the bending rows. "Holds" means the beam's free end stays within 0.02 m of its built height for 5 s (a rigid attachment under 93 N·m may settle by millimetres; a break drops the end by more than 0.5 m). "Breaks" means the free end is more than 0.5 m lower within 3 s. Stall guards 10 game seconds. Chrome proof is pending the post-catalogue phase.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| M1 | Given the cantilever rig at the default capacity with a 1 kg Weight on `Tie` (24.5 N·m), when 5 s pass, then it holds | `LoadLimitedConnectorTests.BendingBelowTheCapacityIsHeld` | M2 | `moment_capacity` 100 holds 2 kg on `TieB` (53.9 N·m) |
| M2 | Given a 4 kg Weight on `TieB` (93.2 N·m), then it breaks: the beam and Weight fall, and the collar and Pivot stay | `LoadLimitedConnectorTests.BendingAboveTheCapacityBreaksTheConnector` | M1 | `moment_capacity` 2 breaks under the bare beam (14.7 N·m) |
| M3 | Given the same 2 kg Weight, on `Tie` it holds (34.3 N·m) and on `TieB` it breaks (53.9 N·m): the arm decides, not the weight | `LoadLimitedConnectorTests.TheSameWeightBreaksTheJointOnlyAtTheLongerArm` | each case is the other's control | — |
| M4 | Given the tension rig with this connector and an 8 kg Weight (no moment), then it holds | `LoadLimitedConnectorTests.AxialLoadDoesNotBreakABendingConnector` | M2 | — |
| M5 | **Architecture guard (one-way flow), not a gameplay scenario.** Given row M1 run twice, once reading the committed load ratio every tick and once never reading it, then the committed poses after 5 s agree within 0.001 m: the display drives nothing | `LoadLimitedConnectorTests.TheStressCueDoesNotChangeTheOutcome` | — | — |
| M6 | A break is permanent until Reset and creates no body; Reset restores attachment, poses and marks; save and load round-trip `moment_capacity`, pose and attachments | `LoadLimitedConnectorTests.ABreakIsPermanentUntilReset`, `.SaveLoadKeepsTheAuthoredConnector` (theories over the family) | — | capacities 2 and 100 |
| M7 | A `moment_capacity` below 2, above 100 or non-finite is refused atomically; a wire to a mount is refused | conformance suite rows 2 and 5 (S4) once it lands; until then `LoadLimitedConnectorTests.InvalidLimitsAndForeignWiresAreRefused` | — | — |

Requirement rows preserved: "declared moment capacity governs failure under bending" (M1 to M3); "subthreshold moment retains the joint regardless of display animation" (M1, M5). Its share of sequence-task-538: M1, M2 and M6.

### Integration

- **Works with.** Planned: EL-112, EL-114. Delivered: `weight` on the delivered rope.
- **Campaign.** GAP-05 "The Last Straw": introduction 43, practice 44, reuse 65, 96, 139 ([campaign allocation](../requirements.md#campaign-gap-allocation)).
- **Lesson fixtures (S8).** Needed.
- **Coverage binding.** Section 7.

### Delivery

As [EL-159](EL-159-tension-limited-connector.md). Prerequisites, in order: blocked on the engine programme (owner decision 3a), `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS, ENGINE-FRACTURE, EL-112, EL-114, then the Family slice "Load-limited connectors" (`elements/BendingConnector/` with its two siblings, one test class). Expected tier: data-only if every probe row is *present* with a test by then; otherwise engine.

**Disqualified if** the fixed joint of `ENGINE-JOINT-KINDS` lets a 2 m, 1.5 kg member carrying 4 kg at its free end (93.2 N·m at the root) sag by more than 0.02 m over 5 s (then the joint's stiffness is the finding, not this part), or if ENGINE-FRACTURE offers no torque measure. If row M3's 20 % and 24 % margins prove too tight under the 0.10 s hold, the row moves to 1 kg on `Tie` against 4 kg on `TieB` and the lesson is taught with those.

## 6. Open questions

**Resolved under the delegation.**

1. Weld or a locked hinge pair: one fixed joint, the kind `ENGINE-JOINT-KINDS` delivers. A second way to make the same joint would be a second rule.
2. Torsion: never a failure mode of this part (D19: no row or lesson uses it).
3. Presets or a range: a continuous Real (D11).

4. Stress cue: shown at all times, for the whole family (owner decision 24, 23 November 2026).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, one collider, two fixed-joint mounts, one declared limit with its measure and hold, one parameter, bindings.
2. **Generic laws.** A fixed joint and a one-way break on a declared measure, limit and hold time; all missing, stated element-free, one owner each (`ENGINE-JOINT-KINDS` for the joint; ENGINE-ASSEMBLY-JOINTS for the player-made connection; ENGINE-FRACTURE for the limit).
3. **Three layers.** Colliders: one box. Visuals: collar, flush band and marks. Animation: mark scale and band colour from committed state; row M5 proves nothing is written back.
4. **Flow and typing.** No ports; the capacity is an f32 Real in newton metres; the measure is a closed enum value of the engine's.
5. **Flat state.** Bodies 1 (dynamic); colliders 1 (box); attachments up to 2 when connected (both fixed, one with a limit); drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 4.
6. **Performance.** One primitive collider; a break changes the constraint set at a step boundary without allocation (the stories' duty). Island sleeping does not exist today; it is owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a). The rig's rope is the delivered rope, whose strand collider is owned by ENGINE-CONNECTION-MATERIALS (owner decision 1, 22 November 2026).
7. **Tests are scenarios.** Rows M1 to M4, M6 and M7 run the production simulation on hand-computed static loads; M5 is a labelled architecture guard.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), `ENGINE-JOINT-KINDS`, ENGINE-ASSEMBLY-JOINTS and ENGINE-FRACTURE for capabilities stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final package-source-binding section in the identity's spec, read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries its heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7**, which widens the reader (owner decision 15, 22 November 2026) and must also admit an entry whose own task is a co-anchored sequence task with no todo link (batch summary, section 4 item 16).

| Field | Planned value |
| --- | --- |
| Package | `bending_connector` |
| Named identity | EL-161, `../invest/named-elements.md#element-161` |
| Acceptance | none available: the ledger entry has no todo link; the identity's own task is `sequence-task-714`, co-anchored with `element-161` |
| Decision | S682, `../invest/scope-corrections.md#s682` |
| Mode records | `Configuration=Adjustable` (`moment_capacity`) |

What it lacks today: a todo link in the ledger entry, and an `s682` block in `scope-corrections.md` on published main.
