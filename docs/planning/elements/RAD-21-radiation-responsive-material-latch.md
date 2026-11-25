# RAD-21 · Radiation-responsive material latch — element readiness spec

**Current delivery (24 October 2026):** [RAD-21 delivery slice](../invest/named-elements.md#radiation-21). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`, findings H-4, C-1, C-3, C-10, X-1 to X-4 and cross-batch item 7) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-21](../requirements.md#radiation-21) (P3 potential). Lengths are metres; the shelf extends along local +X from the post and the insert faces −X. Units, laws, receivers and accumulators are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions) (cited by item number); the weakening polymer is authored fiction.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-21 · Radiation-responsive material latch |
| Type | Radiation + Mechanical (a held shelf released by accumulated dose) |
| Anchor | [requirements.md#radiation-21](../requirements.md#radiation-21); [named-elements.md#radiation-21](../invest/named-elements.md#radiation-21) |
| Owner | S632 |
| Disposition | To deliver as a single element. It refines no CAT part and duplicates none. It is not the Fusible link ([TH-16](../invest/named-elements.md#thermal-16)), which releases on temperature, and not the dosimeter, which closes a contact. |
| Planned package | id `radiation_material_latch`, directory `elements/RadiationMaterialLatch/`, class `RadiationMaterialLatchElementDeclaration` |
| Related identities | Sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); shield [EL-128](EL-128-dense-shield.md); dose scale shared with [RAD-09](RAD-09-integrating-dosimeter.md) and [RAD-10](RAD-10-exposure-sensitive-cargo-badge.md); loads Bowling ball and Weight |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-SIGNAL-NODES, ENGINE-JOINT-KINDS (the hold and its release by a physical request from the package-internal `release` node; holds on a coordinate and their release, engine-stories-index C2), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); the longest row is 12.3 s |

## 2. Declaration

Shape followed: the Wound spring for a retention released by a signal request (`elements/WoundSpring/WoundSpringElementDeclaration.cs:55`–`:56` and `:67`) and for a hinge declared on a package body (`:38`–`:50`). The delivered retention holds a prismatic coordinate that a winding transmission has charged (`CuriousContraptions.Abstractions/PrismaticRetentionValues.cs:13`–`:15`), and a physical request may target only a prismatic retention or a finite-work actuator (`CuriousContraptions.Abstractions/PhysicalRequestValues.cs:6`–`:21`). The latch needs the revolute counterpart that holds an authored angle, which ENGINE-JOINT-KINDS owns (its authored-position retention for prismatic and revolute coordinates, [Story ENGINE-JOINT-KINDS](../../../_bmad-output/planning-artifacts/epics.md#story-engine-joint-kinds-joint-kinds-holds-and-drives-on-a-coordinate), "Holds on a coordinate"; engine-stories-index C2). Items marked **(joints)**, **(signal)** or **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the revolute retention **(joints)** and the receiver and accumulator **(field)** |
| Body 0, frame | Static; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, `elements/Battery/BatteryElementDeclaration.cs:61`–`:64`). Colliders: post box 0.20 × 1.20 × 0.50 m at (−0.10, 0.60, 0); base box 0.80 × 0.06 × 0.50 m at (0.20, −0.03, 0); insert box 0.10 × 0.30 × 0.30 m at (−0.10, 1.35, 0) on top of the post. None declares a radiation row: all take the scene's default row (radiation convention 4). |
| Body 1, shelf | Dynamic, 0.8 kg, constant mass frame, principal moments (0.0168, 0.0407, 0.0242) kg·m² for the 0.60 × 0.05 × 0.50 m board (m(b² + c²) ÷ 12), gravity −9.81 m/s²; restitution 0.1, friction 0.5, rolling resistance 0. Colliders: board box 0.60 × 0.05 × 0.50 m centred on the body, and a lip box 0.02 × 0.05 × 0.50 m at (0.29, 0.05, 0) so a ball does not roll off the free edge. Held level, the board's centre is at (0.30, 0.90, 0); the clear length between the post face and the lip is 0.58 m, which seats the 0.28 m Bowling ball with 0.02 m to spare. |
| Hinge | Body 0 to body 1 at (0, 0.90, 0), axis Z, limits −90° (hanging) to 0° (level), friction torque 0.05 N·m, connected bodies do not collide |
| Retention **(joints)** | One revolute retention on the hinge: holds the authored angle 0° as a constraint row, with no declared torque limit, as the delivered prismatic retention holds its coordinate; released once by a physical request; never re-engages within a Run |
| Receiver **(field)** | Frame body 0; aperture square 0.30 × 0.30 m on the insert's −X face, centre (−0.15, 1.35, 0), normal −X, the standard area 0.09 m² (radiation convention 9); nine samples at offsets 0 and ±0.10 m; Lambert facing; weights PhotonSoft, PhotonMedium, PhotonHard 1, BetaMinus 0.5, others 0; own body by the one rule of optical convention 5 and radiation convention 4 (coordinator decision 26): each shadow cast ends at this aperture face, so the insert behind the face attenuates nothing read from the front, and a source behind the face has facing 0 |
| Accumulator **(field)** | `dose`: integrates the receiver's committed strength (convention 9); capacity 4 J, initial 0; never reduced within a Run |
| Signal graph **(signal, ENGINE-SIGNAL-NODES)** | `dose` is a committed scalar. `spent` is a threshold node over it, on at ≥ 2.0 J (release ratio 0.75; the dose never falls within a Run, so `spent` never turns off before Reset). `release` is a `SignalPhysicalRequestDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:65`) targeting the retention with `PhysicalRequestOperation.Release`, fed by the pulse on the rise of `spent` through a package-internal edge. A memory node cannot do this: its output is a Level and a physical request's input is a Pulse (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:19`–`:22`), `CanConnect` requires equal kinds (`:29`–`:37`), and the bundle refuses that edge (`CuriousContraptions.Bundle/SignalBundleValidation.cs:65`–`:67`). |
| Ports | None. The latch records without a socket and releases by itself. |
| Parameters | None (D13). The release dose is fixed by identity. |
| Presentation bindings | `Visual/Shelf` follows body 1's pose. Three printed crack marks on the insert become visible (`PresentationProperty.Visible`) at one third, two thirds and all of the release dose (0.667, 1.333 and 2.0 J), from the accumulator's committed value **(field)**. `Visual/Catch` rotation from `PresentationDriver.CommittedRetentionReleased`. |
| Footprint | `ElementFootprint(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 1)`; domains Mechanical, Signal and the radiation flag **(field)** |
| Catalogue | Id `radiation_material_latch`, "Exposure latch", `ElementCategory.Radiation` **(field)**, palette order 724 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Holds its shelf level until its insert has taken enough radiation, then lets go. It never heals during a Run." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Post`, `Visual/Base` | boxes equal to their colliders, cream `#fff8e9` and navy `#293954` | solid (post and base boxes) | nothing |
| `Visual/Insert` | box 0.10 × 0.30 × 0.30 m, gold `#f7cb52`, a square face outline printed flat on −X | solid (insert box) | nothing |
| `Visual/Cracks` | three crack lines printed flat on the insert | decorative: printed flat, shown by bindings | nothing (visibility only) |
| `Visual/Shelf` | board 0.60 × 0.05 × 0.50 m with its lip, ochre `#e8b764` | solid (board and lip boxes) | body 1's pose |
| `Visual/Catch` | small hook printed in the plane of the post's +X face under the shelf | decorative: moved by a binding | presentation binding |

Icon: a post with a level shelf and a cracked square on top. It differs from the Springboard and the Impact lever by the gold cracked insert and the shelf that only drops.

### Compiled counts

Bodies 2 (1 static, 1 dynamic). Colliders 5 boxes; the lip is the fifth and exists to keep a round load on the shelf. Joints 1 revolute. Retentions 1. Drives 0. Sensors 0. Signal nodes 2 (threshold `spent`, physical request `release`) and 1 package-internal signal edge. Receivers 1 (nine samples). Accumulators 1. Presentation bindings 4 and one body-pose node.

## 3. Engine capabilities

Map row families (navigation, D14): EnvironmentState, FiniteLedger, GeometryQuery, IonizingTransport, JointConstraint, RigidBodyDynamics, SensibleHeat, StructuralFracture, TemperatureStrength, TopologyTransaction. SensibleHeat, TemperatureStrength, StructuralFracture and TopologyTransaction are not needed: nothing heats, breaks or changes topology. Probe made on `8d1d68aa`: **5 present, 0 untested, 3 narrower, 4 missing.** ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`) |
| R3 | A dynamic body on a revolute hinge with friction torque, under gravity | present | `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`) | `RevoluteHingeFrictionTests.AHingedBarIsHeldWhileItsGravityTorqueIsBelowTheFrictionTorque` (`CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:77`) |
| R4 | A physical-request node turning a pulse into one Release request on a retention, which then expires | present | `SignalPhysicalRequestDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:65`), `PhysicalRequestOperation.Release` (`PrismaticRetentionValues.cs:10`) | `RetentionRequestTests.SimultaneousSourcesCoalesceByOccurrenceAndExactTargetThenExpire` (`CuriousContraptions.tests/RetentionRequestTests.cs:10`): contact pulses route to Release requests on retentions, which expire on the next step |
| R5 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Retention released by a physical request | narrower: prismatic only, and it holds what a winding transmission charged (`PrismaticRetentionDescriptor`, `PrismaticRetentionValues.cs:13`–`:15`; request targets are a prismatic retention or an actuator, `PhysicalRequestValues.cs:6`–`:21`) | — | `PrismaticRetentionTests.PaidWindingRetainsAndIndependentReleaseReturnsToRest` (`CuriousContraptions.tests/PrismaticRetentionTests.cs:16`). Owner of one authored-position retention row for both coordinate kinds (prismatic and revolute), and of the request target that names it (the hold and its release by a physical request): **ENGINE-JOINT-KINDS** (holds on a coordinate and their release; engine-stories-index C2) |
| N2 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) serves airflow rows only and loops over every collider (`:149`) | — | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast: **ENGINE-SPATIAL-QUERIES** |
| N3 | Presentation binding on an accumulator value | narrower: `CommittedStoredFraction` (`CuriousContraptions.Abstractions/ElementPresentation.cs:33`) reads only a mechanical energy store (`MechanicalPayload.EnergyStores`, `CuriousContraptions.Bundle/BundleValidator.cs:310`–`:312`), not an accumulator | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`) for the binding mechanism. Owner of the accumulator driver: **ENGINE-RADIATION** |
| M1 | Receivers with weighted samples and a committed reading and strength | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels and rows; an accumulator that integrates a strength into a committed scalar with a capacity and an authored initial value | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | `ElementCategory.Radiation` and the radiation domain flag | **missing** (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) | — | owner **ENGINE-RADIATION** |
| M4 | A threshold node over a committed scalar; a pulse on the rise of a level node, routed by a package-internal edge to a physical-request input | **missing**: no threshold kind in `SignalNodeKind` (`SignalContractValues.cs:13`); no delivered package declares an internal signal edge and no test builds one | — | owner **ENGINE-SIGNAL-NODES** (threshold and edge rows) |

**Dependencies.** Bowling ball and Weight (delivered) as loads; RAD-01, RAD-02 and EL-128 (undelivered) for the exposure rows.

## 4. Sources and legacy

- [radiation-21](../requirements.md#radiation-21): "Enough exposure releases the load; shielding or insufficient accumulated exposure retains it. Reset restores material state, latch pose and stored load."
- [Named entry](../invest/named-elements.md#radiation-21), owner S632; map row (S605, S543); binding `radiation-01.json`.
- Research "Decay and material memory" law ("removing exposure never repairs it; Reset restores both") and slots 108 "The Weakening Pin", 109 "Release, Then Shelter".
- **Legacy.** None. Hits are coverage snapshots only, e.g. `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L15442-L15445`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1).

- **Dose rate.** Gamma capsule (10 W/sr) 1 m from the insert face, square on. The insert declares the standard area, so the strength is the standard detector's: 0.882 W (convention 14; nine samples, centre 10 W/m², edges 9.85, corners 9.71, mean 9.80 W/m² × 0.09 m²). The dose rises 0.882 J/s, on the one dose scale shared with the dosimeter and the badge (convention 9).
- **Release dose and time.** 2.0 J, chosen so the release comes after about two seconds of square-on exposure at 1 m, long enough to see the cracks appear and short enough for a lesson: 2.0 ÷ 0.882 = 2.27 s. The pulse on the rise of `spent` is routed in the signal network of the next tick, and the retention lets go in that tick's substeps.
- **Shielded.** Two Dense slabs: 0.882 × e^−2 = 0.119 W, so release would take 2.0 ÷ 0.119 = 16.8 s; in a 10 s Run the dose is 1.19 J, 40 % under the release dose, and the shelf holds.
- **Too little.** 1.2 s at 1 m gives 0.882 × 1.2 = 1.06 J, 47 % under 2.0 J: retained, and the insert shows one crack (1.06 J lies between the 0.667 J and 1.333 J marks, 59 % above one and 20 % below the other).
- **Bursts add.** Two 1.5 s bursts from the X-ray emitter at 1 m (10 W/sr; the aperture's corners are 8.0° off the axis, inside its 15° cone, so it reads 0.882 W): 1.32 J after the first; the remaining 0.68 J is reached 0.68 ÷ 0.882 = 0.77 s into the second. A gap between bursts repairs nothing.
- **Load held.** A Bowling ball (4 kg, 0.28 m) at mid-shelf loads the hinge with 4 × 9.81 × 0.30 = 11.8 N·m; the shelf adds 0.8 × 9.81 × 0.30 = 2.4 N·m: 14.1 N·m. A 2 kg Weight (radius 0.048 × 2 + 0.159 = 0.255 m, `elements/Weight/WeightElementDeclaration.cs:59`, so it fits the 0.58 m clear length) at mid-shelf gives 2 × 9.81 × 0.30 + 2.4 = 8.3 N·m. A 4 kg or 8 kg Weight (radius 0.352 m or 0.544 m) does not fit between post and lip and is not a load for this part.
- **No torque release.** The retention has no torque limit: a law-scene box of 15 kg at mid-shelf (15 × 9.81 × 0.30 + 2.4 = 46.5 N·m) is held level. Decided now: a holding limit would be a second release path that lets a heavy load bypass the exposure puzzle, and no placeable load needs one.
- **After release.** The hinge is free between −90° and 0°. The loaded shelf swings down; the ball leaves the lip and drops. Nothing teleports. The stored load is the ball's height: released, it falls as a real body.
- **Why a threshold.** The row calls the threshold-to-release behaviour authored fiction. One fixed release dose makes the lesson "how much exposure", not "how heavy"; "weakens" is shown by the three cracks, which are presentation.
- **Tick order** (convention 11). The field solve of tick n runs after the last substep, on this tick's final poses, and commits the strength and `dose` with the tick. The signal network of tick n + 1 turns `spent` on and routes the pulse on its rise to `release`; the substeps of tick n + 1 run with the retention released.
- **Energy.** The insert absorbs what it receives (ledgered to it, convention 10); the released motion is paid by the load's potential energy alone.

### Rows

Planned class `RadiationMaterialLatchTests`. Fixture: the latch with a Bowling ball resting at mid-shelf against no support but the shelf; a Gamma capsule 1 m from the insert face on its normal. Dose ±3 % (convention 14); angles 2°; release time ±0.15 s (18 ticks, wider than the one-tick latency and the ±3 % rate tolerance on 2.27 s, which is ±0.07 s); stall guard three times the hand-calculated time, at least 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| L1 | Given the fixture, when Run starts, then the shelf stays within 2° of level until the dose reaches 2.0 J at 2.27 s, then swings below −60° within 2 s and the ball's centre falls below the shelf's original height (row: enough exposure releases the load) | `RadiationMaterialLatchTests.EnoughExposureReleasesTheLoad` | L2 | release time 2.27 s ± 0.15 s |
| L2 | Given two Dense slabs between capsule and insert, then after 10 s the dose is 1.19 J and the shelf is level (row: shielding retains it) | `RadiationMaterialLatchTests.ShieldingRetainsTheLoad` | L1 | dose ±3 %; angle 2° |
| L3 | Given a law-scene source at the capsule's place that stops after 1.2 s, then the dose stays at 1.06 J, one crack shows, and the shelf is level 10 s later (row: insufficient accumulated exposure retains it) | `RadiationMaterialLatchTests.TooLittleExposureRetainsTheLoad` | L1 | dose ±3 % |
| L4 | Given two 1.5 s bursts 3 s apart, then the release comes 0.77 s into the second burst and not during the gap (queued until RAD-02 exists; a law-scene source until then) | `RadiationMaterialLatchTests.SeparateExposuresAddAndNeverHeal` | L3 | ±0.15 s |
| L5 | Given L1 continued for 10 s after release, then exactly one release request was made and the retention never re-engages | `RadiationMaterialLatchTests.TheLatchReleasesOnce` | L1 | request count 1 |
| L6 | Given a 2 kg Weight at mid-shelf and no source, then after 10 s the shelf is within 2° of level | `RadiationMaterialLatchTests.TheUnexposedLatchHoldsItsLoad` | L1 | 8.3 N·m held |
| L7 | Given a law-scene box of 12 kg (37.7 N·m) or 15 kg (46.5 N·m) at mid-shelf and no source, then after 10 s the shelf is within 2° of level and no request was made: load alone never releases it | `RadiationMaterialLatchTests.NoLoadReleasesAnUnexposedLatch` (theory) | L1 | angle 2°; request count 0 |
| L8 | Given an Alpha beam or a Neutron module on the insert, or the Gamma capsule behind the insert (on +X), then the dose stays 0 (weights and facing) | `RadiationMaterialLatchTests.OnlyDeclaredChannelsOnTheFaceCount` (theory; queued per source) | L1 | under 0.001 J after 5 s |
| L9 | Given a released Run, when Reset, then the dose is 0, no crack shows, the shelf is level and retained and the ball is back at its authored pose; a saved and reloaded construction behaves as L1 (row: Reset restores material state, latch pose and stored load) | `RadiationMaterialLatchTests.ResetRestoresInsertShelfAndLoad`; authored state by R5 | L1 | angle 2°; ball position 0.01 m |
| L10 | Given a dose passing 0.667, 1.333 and 2.0 J, then one, two and three crack marks are visible, and the catch binding shows released only after the request | `RadiationMaterialLatchTests.TheInsertShowsItsDose` | L3 | visibility states |

No port and no parameter, so no connection or parameter refusal row. Chrome recipe (pending, post-catalogue): latch with a Bowling ball on its shelf over a Receiver, capsule 1 m behind the insert; Run; then repeat with two Dense shields.

## 6. Open questions

None open.

1. **Strength curve — decided now**: none. One fixed release dose of 2.0 J on the standard dose scale (see "Release dose and time" and "Why a threshold").
2. **Payload — decided now** (D19): the player places the load on the shelf; the part ships no payload. Any body that rests on the shelf works, which is what a general shelf should do.
3. **SensibleHeat and TemperatureStrength — decided** (D14): not used. The row names accumulated exposure only; temperature-driven release is TH-16's subject.
4. **Mechanism — decided now**: a hinge held by a retention and released by a signal request, both general capabilities. A breaking pin (IX-37 structural fracture) is not used: nothing breaks, so ENGINE-FRACTURE is not a prerequisite.
5. **Holding limit — decided now**: none (see "No torque release").
6. **Coupling — decided** (coordinator decision 16; audit H-4): committed scalar → threshold node → pulse on its rise → physical request. No memory node.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Bowling ball, Weight, Receiver (delivered); RAD-01, RAD-02, EL-128 (undelivered). No ports.
- **Processes.** [IX-15](../requirements.md#interaction-15) and [IX-03](../requirements.md#interaction-03). [IX-37](../requirements.md#interaction-37) and [IX-42](../requirements.md#interaction-42) are not exercised (decisions 3 and 4).
- **Campaign.** Chapter 14, levels 133 and 134; reuse 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22); a level that starts with a load already on the shelf needs an authored placement of that load.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES (threshold and edge rows); ENGINE-JOINT-KINDS (one authored-position retention row for prismatic and revolute coordinates, with its request target; holds on a coordinate and their release, engine-stories-index C2); ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01; RAD-09 (it proves the accumulator first); S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story).
- **Files.** `elements/RadiationMaterialLatch/RadiationMaterialLatch.csproj`, `RadiationMaterialLatchElementDeclaration.cs`, `radiation_material_latch.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/RadiationMaterialLatchTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Engine tier: the slice is the first consumer of a package-internal signal edge into a physical request and of a revolute retention, so its review covers how they meet, even if both stories land first.
- **Disqualifiers (the slice's probe re-checks).** ENGINE-JOINT-KINDS delivering a revolute retention that can only hold a wound coordinate, or with no holding at an authored angle (then the latch is redesigned from what it does deliver; no latch-specific constraint); a pulse on a threshold node's rise that cannot reach a physical request through a package-internal edge; a 0.8 kg shelf under the 15 kg law-scene load proving unstable at the delivered solver settings (raise the shelf mass and re-derive the torques).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** Two bodies, five colliders, one hinge, one retention, one receiver, one accumulator, two signal nodes and one internal edge, four bindings. No code names the latch and nothing computes a strength.
2. **Generic laws.** Hinge and contact laws; a retention released by a physical request; the weighted-sum receiver and accumulator; a threshold node and the pulse on its rise. "Radiation weakens the pin" is a committed dose crossing a threshold whose rise requests a release.
3. **Three layers.** Colliders: five boxes on two bodies. Visuals: the art table; face outline and cracks are printed. Animation: the shelf art follows its body; cracks and catch are bindings on committed state.
4. **One-way flow and typing.** Field solve → accumulator → threshold node → release request → joint; presentation reads committed state. Operations and channels are enums; values are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time; the retention is a compiled row that changes state, not topology.
6. **Performance fitness.** One slow moving body; five primitives; nine shadow casts per diverging source in range, each through the spatial query's segment cast; no sensor volume. Island sleeping does not exist today; when ENGINE-ISLANDS-SLEEP is delivered the held shelf may sleep like any resting body.
7. **Tests are scenarios.** Rows L1 to L10 run the production simulation; L3, L4 and L7 use law-scene sources or loads of the same general kinds.
