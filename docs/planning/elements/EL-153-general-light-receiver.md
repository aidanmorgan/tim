# EL-153 · General-light receiver — named-identity spec

**Current delivery (24 October 2026):** [EL-153 delivery slice](../invest/named-elements.md#element-153). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`). Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. The sensing face looks along local −X. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-153 |
| Name | General-light receiver |
| Type | Optical |
| Anchor | [requirements.md#element-153](../requirements.md#element-153); named entry [named-elements.md#element-153](../invest/named-elements.md#element-153); scope index [todo-291](../requirements.md#todo-291) |
| Owner | S515 |
| Disposition | To deliver as a **single element**. It refines no catalogue part: it has its own body, a nine-sample aperture, a four-mark meter and its own threshold range. It is not a member of the Colour receivers family, whose members share one single-sample disc. |
| Planned package | id `light_sensor`, directory `elements/LightSensor/`, class `LightSensorElementDeclaration` |
| Roadmap story | [EL-153 delivery slice](../invest/named-elements.md#element-153), `named-el-153`; after ENGINE-OPTICAL and the Flashlight (Story 13.1) |
| Status | not started; no package; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query), ENGINE-SIGNAL-NODES and the Flashlight |

## 2. Declaration

Shape followed: the receiver family ([CAT-056's declaration](CAT-056-red_receiver.md#declaration)) for a level that closes a supplied contact, and the Battery for four gauge marks (`elements/Battery/BatteryElementDeclaration.cs:15`–`:26`).

| Item | Value |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the optical receiver interface **(optics)** |
| Body | One static scene body; material restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s |
| Colliders | Plate box 0.22 × 1.2 × 1.2 m at the origin; foot box 0.9 × 0.16 × 1.2 m at (0, −0.68, 0); two socket spheres r 0.075 m at the ports. Four colliders, all Absorb. |
| Receiver **(optics)** | Rectangle aperture = the whole −X face, 1.2 × 1.2 m, area 1.44 m², front face only. Nine samples at (−0.11, y, z) for y, z ∈ {−0.35, 0, 0.35}, weight 1/9 each. Sensed set all three channels; strength = WeightedSum with weights 1/3 (the mean); no purity. It commits its reading and its strength. |
| Signal graph **(ENGINE-SIGNAL-NODES)** | One threshold node `lit` over the committed strength: on at `threshold`, release ratio 0.9. No signal port. |
| Ports | `PortId.PowerIn`, Electrical, Input, at (0, −0.5, 0.6), normal +Z. `PortId.Supply`, Electrical, Output, at (0, −0.5, −0.6), normal −Z. |
| Supply graph | One `SupplyContactDescriptor` with the delivered signal-state control (`SignalStateContactControl`, `CuriousContraptions.Abstractions/SignalContactControl.cs:12`) on `lit`, bound `PowerIn` and `Supply` |
| Parameters | `threshold`: Real, `ParameterUnit.Watt`, minimum 1, maximum 30, default 8, reject, build-only. The mean channel power received over the face. |
| Stores | None. |
| Presentation bindings | `Visual/Ring` albedo slate (85, 101, 115) → gold (247, 203, 82), SmoothStep 0.125 s, from the committed level of `lit` (`PresentationDriver.CommittedActivation`). `Visual/Mark_0`–`Mark_3` slate → gold, lit while the committed strength is at least 25, 50, 75 and 100 % of `threshold` **(optics, M5)**. |
| Footprint | `ElementFootprint(1, 0, 0, 0)`; domains Electrical and the optical flag **(optics, M6)** |
| Catalogue | Id `light_sensor`, "Light sensor", `ElementCategory.Optics` **(optics, M6)**, colour (1, 0.94, 0.65), palette order 606 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Face it toward a light. Enough light across its nine cells closes a contact; connect a separate supply. A shadow over some cells dims it." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Plate` | box 0.22 × 1.2 × 1.2 m, cream `#fff8e9` | solid (plate box) | nothing |
| `Visual/Foot` | box 0.9 × 0.16 × 1.2 m at y −0.68, navy `#293954` | solid (foot box) | nothing |
| Nine cells | squares 0.3 × 0.3 m at the sample points, warm cream `#fff0a5`, printed flat | decorative: printed flat | nothing |
| `Visual/Ring` | ring around the centre cell, printed flat | decorative: flat, recoloured by a binding | nothing (colour only) |
| `Visual/Mark_0`–`Mark_3` | squares 0.06 m at y −0.55, z ∈ {−0.3, −0.1, 0.1, 0.3}, printed flat | decorative: flat, recoloured by bindings | nothing (colour only) |
| Socket studs | spheres r 0.075, gold `#f7cb52` | solid (socket spheres) | nothing |

The foot sits at y −0.68 so it touches the plate. Icon: an original navy outline of a square of nine cells with rays arriving. It differs from the Solar panel (blue cells in a 1.2 × 1.5 m frame, one socket) by a square face of pale cells with two sockets, and from the laser detectors by cells instead of a target disc.

### Compiled counts

Bodies 1 (static). Colliders 4 (2 boxes, 2 spheres). Receivers 1 with 9 samples. Supply nodes 1. Presentation bindings 15 (ring and four marks × RGB). Joints, drives, stores 0.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 6 present, 0 untested, 3 narrower, 5 missing.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R3 | Typed electrical ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R13 | Typed parameter with bounds refused atomically | present | `ParameterDescriptor` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:52`) | pattern `FanAdmissionTests.PackageScalarEndpointsAndClosedModeAreAdmittedAtomically` (`CuriousContraptions.tests/FanAdmissionTests.cs:267`) |
| R4 | A supply contact closed by a committed signal state, one tick after the state | present | `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`) on a `SupplyContactDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:40`) | `StateLatchTests.Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach` (`StateLatchTests.cs:473`) |
| R9 | Line-of-sight occlusion by solid colliders, per sample | narrower: airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | `AirflowLineClear` | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing segment cast: **ENGINE-SPATIAL-QUERIES** |
| R10 | Weighted sample points on a receiver, each occluded on its own | narrower: airflow receivers only (`AirflowSample`, `CuriousContraptions.Abstractions/AirflowValues.cs:42`; validation `:128`) | `AirflowSampleValidation` | `AirflowGeneralityTests.OneOfFourOccludedSamplesRemovesItsOwnQuarterOfDemand` (`AirflowGeneralityTests.cs:138`). Owner: **ENGINE-OPTICAL** |
| R11 | Presentation binding on a committed driver, with threshold levels | narrower: no optical driver (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`); the Battery's marks use thresholds on `CommittedCharge` | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner: **ENGINE-OPTICAL** |
| MS | A signal node "threshold over a committed scalar, with a release ratio" | **missing**: `SignalNodeKind` has no such node (`CuriousContraptions.Abstractions/SignalContractValues.cs:13`) | — | owner **ENGINE-SIGNAL-NODES** |
| M2 | The optical solve: once per tick after the last substep, with the diverging law and one shadow cast per sample from the spatial query | **missing** | — | owner **ENGINE-OPTICAL** |
| M4 | Declared receivers with weighted samples, a continuous committed reading and strength, and shares of one diverging segment scaled so that received never exceeds emitted | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | A driver for the committed reading (the four marks) | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | `ElementCategory.Optics` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) and the optical domain flag | **missing** | — | owner **ENGINE-OPTICAL** |

ENGINE-OPTICAL is [its story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception). Batch H's summary already proposes "a receiver with weighted sample points and a continuous committed reading exposed to presentation" for the Solar panel; this part needs the same and nothing more.

**Dependencies.** Flashlight ([EL-210](EL-210-flashlight.md), undelivered, first); Battery, a load, Wall (delivered).

## 4. Sources and legacy

**Sources.** Row [element-153](../requirements.md#element-153): samples actual finite-width illumination over a declared aperture; an occluded flashlight cone and absent independent supply cannot activate output. [todo-291](../requirements.md#todo-291) and [sequence-task-398](../requirements.md#sequence-task-398): responds to the documented source; laser-only reception is not silently treated as flashlight reception. [todo-309](../requirements.md#todo-309): finite-width coverage. Campaign: first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no part; mechanisms only):
1. Cone emitter and sample records — `engine/LightNetwork.cs@a6c914e:L8-L10`; cone law: range, cone cosine, facing, shared-proxy occlusion excluding emitter and receiver, intensity × facing × weight ÷ max(1, d²), all readings committed together, no reflection or ambient — `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward the law with its near field replaced by optical convention 1's flat rule (beyond 1 m I × facing ÷ d²; inside 1 m I ÷ 1 m² for a source in front of the aperture, no facing factor; coordinator decisions 21 and 26); do not carry forward the CPU static solve.
2. Nine equal-weight front samples and threshold 1 — `parts/SolarPanelPart.cs@a6c914e:L8-L12`, `parts/SolarPanelPart.cs@a6c914e:L26-L34`; four-step meter — `parts/SolarPanelPart.cs@a6c914e:L47-L53`. Carry forward as the pattern.
3. Torch source: lens (0.66, 0, 0), range 8, cone cosine 0.9659258 (15°), intensity 24 — `parts/FlashlightPart.cs@a6c914e:L10-L13`. Carry forward (source facts owned by [CAT-029](CAT-029-flashlight.md)).
4. Acceptance facts for cone reception: reversed or out-of-range (x = 7) receiver reads 0; restoring the pose restores the same reading — `CuriousContraptions.tests/LightTests.cs@a6c914e:L116-L166`; a small occluder reduces rather than erases — `CuriousContraptions.tests/LightTests.cs@a6c914e:L210-L224`; readings are insertion-order independent and Reset clears them — `CuriousContraptions.tests/LightTests.cs@a6c914e:L226-L258`. Carry forward.

**Files harvested:** `engine/LightNetwork.cs`, `parts/SolarPanelPart.cs`, `parts/FlashlightPart.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/LightTests.cs`. No legacy level or lesson uses a general-light receiver.

## 5. Acceptance

### Behaviour and numbers

Starting values (D1). The torch is the Flashlight: 90 W white, I = 420.4 W/sr.

- **Reading.** Received power = min(P, A × Σ wᵢEᵢ) per diverging segment. At 3 m on the axis the centre sample sees 420.4 ÷ 9 = 46.7 W/m²; a corner sample at (0.35, 0.35) is √(9 + 0.245) = 3.04 m away and 9.3° off the face normal: 420.4 × 0.987 ÷ 9.245 = 44.9 W/m². The nine-sample mean is 45.7 W/m², so the face receives 1.44 × 45.7 = 65.8 W, which is less than the 90 W emitted, and the strength (mean per channel) is 21.9 W.
- **Threshold.** Default 8 W. On the axis the strength is about 1.44 × 420.4 ÷ (3 d²) = 201.8 ÷ d²: 9.97 W at 4.5 m (on), 6.67 W at 5.5 m (off); the edge is near 5.0 m, the legacy sensor's reach. The level releases below 0.9 × 8 = 7.2 W.
- **Partial shadow.** Each sample is occluded on its own. With one of the three rows shadowed, six samples remain: 21.9 × 6 ÷ 9 = 14.6 W (on). With two rows shadowed: 7.3 W, which is off from dark at a `threshold` of 10 W with a 27 % margin (at the default 8 W it is too near the 7.2 W release point to assert).
- **Shares.** This sensor alone takes 65.8 W of the 90 W cone, so no scaling applies; with other receivers in the same cone all shares are scaled together if their sum would exceed 90 W (conventions, item 8).
- **Other light.** One receiver law for all light (owner decision 9): a 10 W red laser on the face reads 10 ÷ 3 = 3.33 W, below the default, and lights the sensor only with `threshold` lowered. Each source kind has its own executed row.
- **Contact and tick order.** The strength commits with the tick the light arrives; the threshold node `lit` takes it at the start of the next tick and `PowerIn` → `Supply` closes in that tick. Light never becomes electricity.

### Rows

Planned class `LightSensorTests`. Fixture: a Flashlight in Self-contained mode lit by a Delay box pulse; the sensor 3 m in front, facing the lens; Battery `Supply` → sensor `PowerIn`; sensor `Supply` → Powered gate `PowerIn`. Power tolerance ±2 %, the one optical set (coordinator decision 21); stall guard 1 s from the pulse.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| L1 | Given the fixture, then the committed strength is 21.9 W, the level is on, four marks are lit and the load is supplied on a later tick than the reading | `LightSensorTests.TorchLightAcrossTheCellsClosesTheSuppliedContact` | L2, L3 | ±2 % |
| L2 | **Occluded cone.** Given a Wall between torch and sensor, then the strength is zero, no mark is lit and the load is never supplied | `LightSensorTests.AnOccludedConeActivatesNothing` | L1 | 240 ticks |
| L3 | **Absent independent supply.** Given L1 with `PowerIn` unwired, then the level and marks are on and the load is never supplied | `LightSensorTests.WithoutItsOwnSupplyALitSensorSuppliesNothing` | L1 | 240 ticks |
| L4 | Given `threshold` 10 W and a box shadowing one row of cells, then the strength is 14.6 W and the level is on; shadowing two rows from the start, 7.3 W and off | `LightSensorTests.APartialShadowReducesTheReadingBySampleCount` | L1 | ±2 % (coordinator decision 21). The per-sample values differ by up to 4 % (46.7 W/m² at the centre, 44.9 at a corner), so the exact remaining fractions are 0.669 with one edge row shadowed and 0.331 to 0.338 with one row lit, within 1.3 % of 6/9 and 3/9; the two cases are a factor of two apart |
| L5 | Given the sensor on the axis at 4.5 m, on; at 5.5 m, off; turned 180°, off at any distance; at 8.5 m, zero reading | `LightSensorTests.DistanceFacingAndRangeDecideTheReading` | L1 | 0.5 m either side of the 5.0 m edge and of the 8 m range |
| L6 | Given a 10 W red laser on the face, then the strength is 3.33 W and the level is off at the default; with `threshold` 2.5 W it is on | `LightSensorTests.CollimatedLightIsReceivedByTheSameLaw` | L1 | ±2 % (coordinator decision 21) |
| L7 | Given `threshold` 30 W and the sensor moved to 3.3 m (strength 201.8 ÷ 3.3² = 18.5 W, 62 %), then exactly two marks are lit and the level is off | `LightSensorTests.TheMeterShowsQuartersOfTheThreshold` | L1 (four marks) | 62 % is 12 points above the 50 % mark and 13 below the 75 % mark, both far outside the ±2 % power tolerance |
| L8 | Given a lit run, when Reset, then reading, level, contact, ring and marks are at rest; a saved and reloaded construction keeps `threshold` | `LightSensorTests.ResetAndReloadRestoreTheDarkOpenState`; authored state by R12 | L1 | — |
| L9 | Given `threshold` 0.9, 30.1 or non-finite, then the edit is refused; 1 and 30 are admitted. Wrong-kind links are refused generically (R3) | `LightSensorTests.ThresholdOutsideItsRangeIsRefused` | — | typed refusal |

Chrome recipe (pending, post-catalogue): flashlight with a ball above its button, the sensor 3 m in front, Battery and a load; Run.

## 6. Open questions

None open.

1. **Laser light and units — decided** (K-1; owner decision 9): one unit, watts, and one receiver law for all light; the sensor reads any light. Coupling is observation → signal node → signal-state contact (coordinator decision 16).
2. **Geometry, threshold, art and catalogue values — decided** (D1, D9): the values in section 2.
3. **Hysteresis — decided** (K-4): release ratio 0.9, the family value.
4. **Roadmap — resolved**: its own slice after the Flashlight.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Flashlight, pull-cord light [EL-155](EL-155-rope-operated-light.md), filters between torch and sensor (undelivered); Battery, Powered gate, Motor, Wall, a moving box or ball as a shadow (delivered). Port domains as the receiver family.
- **Campaign.** First use 51–60.
- **Coverage binding (planned, not yet live).** A named package is bound by the binding section that main's `tools/Coverage/NamedPackageSources.cs` reads. This identity is **blocked on the coverage follow-on story S7**: its source [todo-291](../requirements.md#todo-291) is a scope index, and decision S515 has no anchor in `docs/planning/invest/scope-corrections.md`. S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15); the section is not written here.
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-SIGNAL-NODES (threshold node); ENGINE-OPTICAL as corrected in the batch summary; the Flashlight; S7 for the binding.
- **Files.** `elements/LightSensor/LightSensor.csproj`, `LightSensorElementDeclaration.cs`, `light_sensor.tscn`, `icon.svg`; one guarded `CuriousContraptions.slnx` line; `CuriousContraptions.tests/LightSensorTests.cs`; a `VisibleSolidColliderTests` row; this spec's binding section.
- **Review tier.** Data-only if the probe finds every row present after ENGINE-OPTICAL.
- **Disqualifiers.** A receiver that reports only above or below a threshold (the meter and row L4 cannot be stated); samples not occluded one by one; a different unit scale (re-derive the threshold range from the torch).

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Four colliders, two ports, one receiver with nine samples, one supply contact, one parameter, fifteen bindings.
2. **Generic laws.** The receiver law with declared samples and rule; the supply contact law. Partial shadow is a consequence of per-sample occlusion.
3. **Three layers.** Colliders: two boxes and two spheres. Visuals: flat cells, ring and marks. Animation: colours from the committed level and reading.
4. **One-way flow and typing.** Segments → reading → level → next tick's supply; watts are f32.
5. **Flat state.** Counts in section 2.
6. **Performance fitness.** Static; four primitives; nine shadow casts per diverging segment that reaches it, a fixed bound, each through the spatial query.
7. **Tests are scenarios.** Rows L1 to L9.
