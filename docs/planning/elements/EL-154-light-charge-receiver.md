# EL-154 · Light-charge receiver — named-identity spec

**Current delivery (24 October 2026):** [EL-154 delivery slice](../invest/named-elements.md#element-154). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`). Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. The absorbing face looks along local −X. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-154 |
| Name | Light-charge receiver |
| Type | Optical |
| Anchor | [requirements.md#element-154](../requirements.md#element-154); named entry [named-elements.md#element-154](../invest/named-elements.md#element-154); scope index [todo-291](../requirements.md#todo-291) |
| Owner | S516 |
| Disposition | To deliver as a **single element**. It refines no catalogue part. It is the storing counterpart of the Solar panel ([EL-211](EL-211-solar-panel.md), a source with no store) and differs from the light sensor ([EL-153](EL-153-general-light-receiver.md), which switches someone else's supply). |
| Planned package | id `light_charge_receiver`, directory `elements/LightChargeReceiver/`, class `LightChargeReceiverElementDeclaration` |
| Roadmap story | [EL-154 delivery slice](../invest/named-elements.md#element-154), `named-el-154`; after ENGINE-OPTICAL and the Flashlight |
| Status | not started; no package; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-THERMAL (the credited store row, R24) and ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query) and the Flashlight |

## 2. Declaration

Shape followed: the Battery, a supply source with a finite store of four parameters and four gauge marks (`elements/Battery/BatteryElementDeclaration.cs:9`–`:79`). The store starts empty and is filled by absorbed light **(optics)** instead of being full at Run.

| Item | Value |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the optical receiver interface **(optics)** |
| Body | One static scene body; material restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s |
| Colliders | Plate box 0.22 × 1.2 × 1.2 m at the origin; foot box 0.9 × 0.16 × 1.2 m at (0, −0.68, 0); one socket sphere r 0.075 m at the port. Three colliders, all Absorb. |
| Receiver **(optics)** | Disc aperture r 0.5 m on the −X face at (−0.11, 0, 0), front face only; area π × 0.5² = 0.785 m². Five samples: the centre and four at radius 0.3 m on the ±Y and ±Z axes, weight 1/5 each. All channels accepted. No threshold and no level: the reading credits the store. |
| Store credit **(optics, M7)** | At the end of each tick's optical and radiation solve ([combined tick order](EL-176-red-laser-emitter.md#combined-tick-order) step 3), 0.5 × the energy booked on the receiver's absorbing collider record in that tick moves from that record to the source's store, up to `capacity`; energy that does not fit stays on the record, where thermal T3 may move it (optical 9; coordinator decision 26). |
| Ports | `PortId.Supply`, Electrical, Output, role `Supply`, at (0, −0.5, −0.6), normal −Z |
| Supply graph | One `SupplySourceDescriptor` `cell` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:39`) with store (`capacity`, `power_limit`, `initial_charge`, `enabled`); `Supply` → (`cell`, `SupplyTerminal.Output`). `ElectricalNodes`: one `Source` row. |
| Parameters | `capacity`: Real, Joule, 60–3600, default 600, clamp. `power_limit`: Real, Watt, 10–120, default 60, clamp. `initial_charge`: Real, fraction, 0–1, default 0, clamp. `enabled`: Choice "Disabled", "Enabled", default 1, run control. The Battery's four, with a smaller range and an empty start. |
| Presentation bindings | `Visual/Mark_0`–`Mark_3` albedo slate (85, 101, 115) → gold (247, 203, 82) at charge fractions 0.25, 0.5, 0.75 and 1, driver `PresentationDriver.CommittedCharge` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`), as the Battery's marks |
| Footprint | `ElementFootprint(1, 0, 0, 0)`; domains Electrical and the optical flag **(optics, M6)** |
| Catalogue | Id `light_charge_receiver`, "Light-charge cell", `ElementCategory.Power`, colour (0.33, 0.40, 0.45), palette order 318 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Light on its dark disc slowly fills its store; the four marks show the charge. It supplies electricity from what it has stored, and nothing in the dark once that is spent." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Plate` | box 0.22 × 1.2 × 1.2 m, cream `#fff8e9` | solid (plate box) | nothing |
| `Visual/Foot` | box 0.9 × 0.16 × 1.2 m at y −0.68, navy `#293954` | solid (foot box) | nothing |
| Absorber disc | disc r 0.5, slate `#556573`, printed flat on the −X face | decorative: printed flat | nothing |
| `Visual/Mark_0`–`Mark_3` | squares 0.08 m in a column at z 0.54, y ∈ {−0.3, −0.1, 0.1, 0.3}, printed flat | decorative: flat, recoloured by bindings | nothing (colour only) |
| Socket stud | sphere r 0.075, gold `#f7cb52` | solid (socket sphere) | nothing |

Icon: an original navy outline of a round disc with a four-step gauge beside it. It differs from the light sensor by one round dark disc instead of nine pale cells, a gauge column and a single socket; and from the Battery by being a flat plate facing the light.

### Compiled counts

Bodies 1 (static). Colliders 3 (2 boxes, 1 sphere). Receivers 1 with 5 samples. Supply nodes 1 (a source with one store). Presentation bindings 12 (four marks × RGB). Joints, drives, signal nodes 0.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): ElectricalPower, FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 5 present, 2 present-untested, 3 narrower, 5 missing.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R3 | Typed electrical ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R13 | Typed parameters with bounds | present | `ParameterDescriptor` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:52`) | pattern `FanAdmissionTests.PackageScalarEndpointsAndClosedModeAreAdmittedAtomically` (`CuriousContraptions.tests/FanAdmissionTests.cs:267`) |
| R15 | Finite supply source store that cannot be overfilled or overspent | present, untested as used: the cited test proves the allocation function on balances, not a compiled source that starts empty; row Q5 is its first executed proof | `SupplySourceDescriptor` (`SupplyContractValues.cs:39`), `SupplySourceStoreDescriptor` (`CuriousContraptions.Abstractions/ElectricalContractValues.cs:31`) | `ElectricalSupplyTests.RepeatedAllocationCannotOverfillOrSpendMoreThanInitialSource` (`CuriousContraptions.tests/ElectricalSupplyTests.cs:269`) |
| R23 | Gauge marks from committed charge | present, untested here: `PresentationDriver.CommittedCharge` (`ElementPresentation.cs:23`) is used by the Battery's marks (`BatteryElementDeclaration.cs:15`–`:26`); no test body was opened for it | `PresentationDriver` | none opened |
| R9 | Line-of-sight occlusion by solid colliders, per sample | narrower: airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | `AirflowLineClear` | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing segment cast: **ENGINE-SPATIAL-QUERIES** |
| R10 | Weighted sample points on a receiver | narrower: airflow receivers only (`AirflowSample`, `CuriousContraptions.Abstractions/AirflowValues.cs:42`) | `AirflowSampleValidation` (`:128`) | `AirflowGeneralityTests.OneOfFourOccludedSamplesRemovesItsOwnQuarterOfDemand` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:138`). Owner: **ENGINE-OPTICAL** |
| R24 | A source store that is credited during a Run | narrower: a source's store only falls during a Run; the per-phase credit law exists for consumer stores (`ElectricalSupply.Reserve`, `engine/gpu/ElectricalSupply.cs:11`) | `ElectricalSupply` | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` (`ElectricalSupplyTests.cs:207`). Owner: **ENGINE-THERMAL**, the credited store row (constant or parameter-bound capacity, power limit, initial charge and enable; filled during a Run only by a credit), which lands before ENGINE-OPTICAL (owner decision 18; coordinator decision 26; Batch O G20) |
| M2 | The optical solve: once per tick after the last substep, with the diverging law and shadow casts from the spatial query | **missing** | — | owner **ENGINE-OPTICAL** |
| M4 | Declared receivers with weighted samples and a continuous committed reading | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Beam and cone read model | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | The optical `SimulationDomain` flag | **missing** | — | owner **ENGINE-OPTICAL** |
| M7 | The receiver credit: at the end of the solve, a declared efficiency × the energy booked on the receiver's absorbing collider record moves to a declared finite source store, bounded by its capacity, the remainder staying on the record | **missing** | — | owner **ENGINE-OPTICAL** (the receiver credit only; the store it fills is R24's, ENGINE-THERMAL's) |

ENGINE-OPTICAL is the story of that name in `_bmad-output/planning-artifacts/epics.md`. The Solar panel uses the same store-credit form (coordinator decision 21; EL-176 section 10, item 9), with a fixed store and a threshold node for availability; the story text is in the [batch summary](readiness/batch-k.md). The credited store (R24) is one row of ENGINE-THERMAL, which lands before ENGINE-OPTICAL; the receiver credit (M7) is ENGINE-OPTICAL's row (coordinator decision 26; Batch O G20).

**Dependencies.** Flashlight ([EL-210](EL-210-flashlight.md), undelivered, first); a consumer that debits its source today, the Fan in Electrical mode (delivered); Battery to switch the torch in bursts; Wall.

## 4. Sources and legacy

**Sources.** Row [element-154](../requirements.md#element-154): integrates absorbed optical energy into a bounded store with declared loss; two bursts accumulate correctly; dark time cannot create charge. [sequence-task-398](../requirements.md#sequence-task-398): a charging variant responds to the actual illumination duration. [todo-311](../requirements.md#todo-311): Reset clears charge. [sequence-task-396](../requirements.md#sequence-task-396): "Charge storage … remain pending". Campaign: first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no part; mechanisms only):
1. Solar panel explicitly has no storage — `parts/SolarPanelPart.cs@a6c914e:L8-L9`. Do not carry forward (the absence); it confirms the store is new.
2. Cone law and nine-sample face — `engine/LightNetwork.cs@a6c914e:L12-L52`, `parts/SolarPanelPart.cs@a6c914e:L26-L34`. Carry forward the law.
3. Torch intensity 24, range 8, 15° — `parts/FlashlightPart.cs@a6c914e:L10-L13`. Carry forward (sizing basis).
4. Order-independent readings and Reset clearing optical state — `CuriousContraptions.tests/LightTests.cs@a6c914e:L226-L258`. Carry forward.

**Files harvested:** `parts/SolarPanelPart.cs`, `engine/LightNetwork.cs`, `parts/FlashlightPart.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/LightTests.cs`.

## 5. Acceptance

### Behaviour and numbers

Starting values (D1). The torch is the Flashlight: 90 W white, I = 420.4 W/sr.

- **Absorbed power.** At 3 m on the axis the centre sample sees 46.7 W/m². A ring sample at radius 0.3 m is √(9 + 0.09) = 3.015 m away and 5.7° off the normal: 420.4 × 0.995 ÷ 9.09 = 46.0 W/m². The five-sample mean is (46.7 + 4 × 46.0) ÷ 5 = 46.1 W/m², so the disc absorbs 0.785 × 46.1 = 36.2 W, less than the 90 W emitted.
- **Stored power.** Efficiency 0.5 is the declared loss: 18.1 W is stored, 18.1 W × dt = 0.151 J per default 1/120 s tick (0.30 J per 1/60 s tick, which even the smallest `capacity`, 60 J, holds two hundred times over, so the store meets coordinator decision 32's one-tick sizing at every setting); the other half stays on the plate's absorbed record and becomes heat only as thermal T3 moves it (K-10; coordinator decision 26).
- **Bursts.** A 2 s burst stores 2 × 18.1 = 36.2 J. A second 2 s burst brings the store to 72.4 J. Between bursts, and in any dark run, the store neither rises nor falls unless a load draws from it. From empty, the default 600 J capacity fills in 600 ÷ 18.1 = 33 s; a 60 J capacity fills in 3.3 s and stays at 60 J.
- **Output.** The store supplies up to `power_limit` (60 W, 60 W × dt = 0.5 J per default 1/120 s tick; energy per tick = power limit × dt, coordinator decision 30) through the ordinary source law and is debited by what consumers take. An empty store supplies nothing. Output energy can never exceed stored energy, which is half the absorbed energy.
- **Tick order.** Light absorbed by the solve at the end of tick n is credited when that tick commits and can be supplied from tick n + 1.
- **Coupling.** The store credit is the conventions' energy transfer (item 9): absorbed energy times a declared efficiency, conserved. It is not a control coupling; this part has no level, no threshold node and no contact.
- **Run length.** Rows Q1 and Q4 need about 10 s of game time; no row needs more than the default 30 s Run (owner decision 13).

### Rows

Planned class `LightChargeReceiverTests`. Fixture: a Flashlight in Electrical mode on its own Battery, latched by a Delay box pulse and switched in bursts with that Battery's run control; the cell 3 m in front facing the lens; cell `Supply` → Fan `PowerIn` (Fan in Electrical mode, a Tennis ball in its cone). Power, charge and energy-ratio tolerance ±2 %, the one optical set (coordinator decision 21).

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| Q1 | **Two bursts accumulate.** Given the Fan unwired and two 2 s bursts 5 s apart, then the committed charge is 36.2 J after the first, unchanged through the dark gap, and 72.4 J after the second | `LightChargeReceiverTests.TwoBurstsAccumulateAndTheGapChangesNothing` | Q2 | ±2 % on each charge; the gap's change below 0.01 J |
| Q2 | **Dark time cannot create charge.** Given no light for 600 ticks, then the charge stays zero, the source is unavailable and a wired Fan never blows | `LightChargeReceiverTests.DarkTimeCreatesNoCharge` | Q1 | charge below 0.01 J |
| Q3 | **Declared loss.** Given 240 lit ticks, then stored energy ÷ absorbed energy (the sum of the committed reading × dt) is 0.5 | `LightChargeReceiverTests.HalfOfAbsorbedEnergyIsStored` | Q1 | ±2 % (the optical set, coordinator decision 21) |
| Q4 | **Bounded store.** Given `capacity` 60 J and 6 s of light, then the charge reaches 60 J and stays there; four marks are lit | `LightChargeReceiverTests.TheStoreStopsAtItsCapacity` | Q1 | charge never above 60 J by more than 0.1 % |
| Q5 | Given a charged cell and the Fan wired, then the Fan blows, the charge falls, the Fan stops when the charge is spent, and the energy the Fan was granted never exceeds the charge lost | `LightChargeReceiverTests.StoredChargeSuppliesALoadUntilItIsSpent` | Q2 | charge lost never below the energy granted and above it by no more than the ledger residual |
| Q6 | Given a Wall between torch and cell, or the cell turned 180°, then the charge stays zero; given a box covering the centre and one ring sample, charging falls to three fifths | `LightChargeReceiverTests.OnlyUnshadowedFrontLightCharges` | Q1 | three fifths ±2 % (the three lit ring samples give 3 × 46.0 ÷ 230.7 = 0.598) |
| Q7 | Given a charged run, when Reset, then the charge is the authored `initial_charge` × `capacity` (zero by default) and the marks are dark; a saved and reloaded construction keeps the four parameters | `LightChargeReceiverTests.ResetReturnsTheAuthoredCharge`; authored state by R12 | Q1 | — |
| Q8 | Given `capacity` 59 or 3601, `power_limit` 9 or 121, or `initial_charge` 1.1, then the editor clamps to the bound; a non-finite value is refused. Wrong-kind links are refused generically (R3) | `LightChargeReceiverTests.ParametersStayInsideTheirRanges` | — | typed result |

Chrome recipe (pending, post-catalogue): flashlight 3 m from the cell, switched twice; a Fan wired to the cell; watch the gauge step up twice and the Fan run from stored charge.

## 6. Open questions

None open.

1. **Output policy — decided** (K-12): the part is a supply source whose finite store is filled by absorbed light. Reason: it uses the delivered finite source store and one new general capability, and refinement S710 says "store/deplete converted optical energy rather than instantaneous free power". The earlier proposal of a leak and a separately supplied contact is not kept: a leak would be a second new capability for no requirement.
2. **Constants — decided** (D1): efficiency 0.5, capacity 60–3600 J (default 600), power limit 10–120 W (default 60).
3. **The lost half — decided** (K-10): it stays on the absorbing collider's record and becomes heat only as thermal T3 moves it (coordinator decision 26); this part declares no thermal node, so here it stays on the record.
4. **Laser light — decided** (owner decision 9): any light charges it by the one receiver law; a 10 W laser on the disc stores 5 W.
5. **Roadmap — resolved**: its own slice after the Flashlight.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Flashlight, pull-cord light [EL-155](EL-155-rope-operated-light.md), lasers (undelivered); Fan, Motor, Powered gate, Signal lamp circuits, Battery, Wall (delivered). Port domain: `Supply` is Electrical Output, as the Battery's.
- **Campaign.** First use 51–60 ("Hold the Light" is a named lesson distinction).
- **Coverage binding (planned, not yet live).** A named package is bound by the binding section that main's `tools/Coverage/NamedPackageSources.cs` reads; **blocked on the coverage follow-on story S7** (source [todo-291](../requirements.md#todo-291) is a scope index; decision S516 has no anchor in `docs/planning/invest/scope-corrections.md`). S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15); the section is not written here.
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-THERMAL's credited store row (R24); ENGINE-OPTICAL as corrected in the batch summary, including the receiver credit M7; the Flashlight; S7 for the binding. Row Q5 with a Motor instead of the Fan also needs ENGINE-SUPPLY-DEMAND (`engine-supply-drive-demand`); with the Fan it does not.
- **Files.** `elements/LightChargeReceiver/LightChargeReceiver.csproj`, `LightChargeReceiverElementDeclaration.cs`, `light_charge_receiver.tscn`, `icon.svg`; one guarded `CuriousContraptions.slnx` line; `CuriousContraptions.tests/LightChargeReceiverTests.cs`; a `VisibleSolidColliderTests` row; this spec's binding section.
- **Review tier.** Data-only if the probe finds every row present after ENGINE-OPTICAL.
- **Disqualifiers.** ENGINE-OPTICAL without a store credit (then the part cannot be declared and the story is amended first); a source store that cannot start empty and rise; a different unit scale.

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Three colliders, one port, one receiver with five samples, one source with a store, four parameters, twelve bindings.
2. **Generic laws.** The receiver law; a store credit of declared efficiency; the delivered source and reservation laws. "Two bursts add up" is the ledger, not a rule.
3. **Three layers.** Colliders: two boxes and a sphere. Visuals: flat disc and marks. Animation: mark colours from committed charge.
4. **One-way flow and typing.** Segments → reading → store → next tick's supply; joules and watts are f32.
5. **Flat state.** Counts in section 2.
6. **Performance fitness.** Static; three primitives; five shadow rays per diverging segment that reaches it.
7. **Tests are scenarios.** Rows Q1 to Q8.
