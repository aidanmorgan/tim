# EL-043 · Pneumatic pressure gauge — element readiness spec

**Current delivery (24 October 2026):** [EL-043 delivery slice](../invest/named-elements.md#element-043). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against `8d1d68aa`. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations of the form `path:line` are this checkout. Values marked *proposed* are starting values (decision D1). Box sizes are full extents. Shared sealed-gas facts P1–P20, the family contract F1–F11 and the capability rows G1–G14 are in [EL-039](EL-039-air-reservoir.md). Repaired on 22 November 2026 against the Batch H audit.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-043 · Pneumatic pressure gauge |
| Type | Pneumatic |
| Anchor | [requirements.md#element-043](../requirements.md#element-043); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-043); owner S478 |
| Related | Refines no CAT spec. Pattern sibling of [CAT-060 Sound meter](CAT-060-sound_meter.md) (needle without supply, separately supplied contact with hysteresis); reads [EL-039](EL-039-air-reservoir.md) or any gas store through [EL-038](EL-038-pneumatic-hose.md). |
| Roadmap story | [EL-043 delivery slice](../invest/named-elements.md#element-043); canonical proof owner and prerequisite decisions are recorded there. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SIGNAL-NODES` (its threshold row: a signal node thresholding a committed scalar observation with on and off levels; coordinator decision 16) and `ENGINE-PARAMETER-BOUND-FRAMES` (the threshold mark posed from `threshold`; coordinator decision 16); then `ENGINE-GAS-NETWORK` for the pressure observation kind (owner decision 18). |
| Disposition | To deliver as a single element. Not a duplicate of the Sound meter: it reads a different quantity through a different port. |
| Planned package | id `pressure_gauge`, directory `elements/PressureGauge/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |

## 2. Declaration

Closest delivered shape: the State latch, for a signal state that closes a separately supplied contact between `PowerIn` and `Supply` (`elements/StateLatch/StateLatchElementDeclaration.cs:57`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and the fluid port contract (G5).
- **Bodies and colliders.** One static scene body, mass 0. Dial housing: cylinder r 0.45 m, length 0.35 m, axis Z, centre (0, 0.15, 0) *proposed* (round, so the collider matches the visible bezel; smaller than the 1.6 m Sound meter cabinet). Foot box 1.0 × 0.14 × 0.5 m at (0, −0.37, 0) *proposed*.
- **Material.** Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.
- **Sensor.** The reading is the committed absolute pressure of the line joined to `GasIn`, that is, of the gauge's own hose, read afresh on every committed tick, including the first tick after Run or Reset. That hose starts at the pressure of the store it taps (F9), so the first reading of a 400 000 Pa tank is 400 000 Pa and nothing flows to make it so. The gauge keeps no remembered reading. With nothing joined the reading is ambient (F2). The gauge end of the line is closed: the tap has zero volume and passes no gas.
- **Threshold state.** One signal node `above`: On when the reading is at or above `threshold`, Off when it is at or below 0.9 × `threshold`, otherwise unchanged (the Sound meter's 90% rule, harvest M2). At Run start and after Reset it is Off, and the first reading then applies the rule; nothing is carried over. A first reading inside the band therefore leaves it Off.
- **Supply graph.** One contact node (`SupplyContactDescriptor`, `CuriousContraptions.Abstractions/SupplyContractValues.cs:40`) controlled by `above` being On (`SignalStateContactControl`, `CuriousContraptions.Abstractions/SignalContactControl.cs:12`); `PowerIn` binds its input terminal and `Supply` its output. The contact never creates supply.
- **Stores.** None.

| Parameter | Type | Unit | Default | Minimum | Maximum | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `threshold` | Real f32 | Pa absolute | 300 000 | 120 000 | 600 000 | Reject; build only; player-editable unless the level locks it (D11) |

Fixed: off level 0.9 × `threshold`.

| `PortId` | Role | Domain | Direction | Local position, normal |
| --- | --- | --- | --- | --- |
| `GasIn` | Bidirectional (fluid port, L5) | Fluid | Input (link-joined) | (0, −0.37, −0.25), −Z |
| `PowerIn` | `PortRole.PowerIn` | Electrical | Input | (−0.5, −0.37, 0), −X |
| `Supply` | `PortRole.Supply` | Electrical | Output | (0.5, −0.37, 0), +X |

Each socket centre is seated on a face of the foot box (1.0 × 0.14 × 0.5 m at (0, −0.37, 0), so x ±0.5, y −0.44 to −0.30, z ±0.25), at the foot's mid-height y −0.37. The earlier positions floated: `PowerIn` and `Supply` at x ±0.55 lay 0.05 m beyond the foot's ±0.5 faces, and `GasIn` at (0, −0.3, −0.18) sat on the edge where the foot's top meets the bezel (bezel bottom y = 0.15 − 0.45 = −0.30) and 0.005 m outside the bezel's −0.175 face, with its −Z normal on neither face. `GasIn` is a fluid port of the one record ([EL-039](EL-039-air-reservoir.md) F3; liquid L5): role Bidirectional, and Input is only its link-joined socket's authoring end; `GasIn` is a new `PortId` member (`ENGINE-GAS-NETWORK`), no `PortRole` member is added for gas, and the port declares no aperture area (the hose edge's throat is the connection kind's, EL-039 F10).

- **Presentation bindings.** `Visual/Dial/Needle`, `LocalRotationZ`, driven by the committed reading, linear from 101 325 Pa to 600 000 Pa, clamped (driver missing, G9; the same scale as the tank dial). `Visual/Lamp`, albedo slate `#556573` to gold `#f7cb52`, driver `CommittedSupply`, 0.1 s (harvest M3). `Visual/Dial/Mark` is posed at build from the Real parameter `threshold`, at the needle angle for that pressure on the same scale (a presentation node posed from a parameter, `ENGINE-PARAMETER-BOUND-FRAMES`; missing, section 3).
- **Palette.** `ElementCategory.Control`, palette order 421 (F8; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cyan `#66b8c9`. Title "Pressure gauge". Description: "The needle shows the air pressure where it is connected, with or without electricity. Supply it to pass power while pressure stays above the mark."

### Art and solidity

| Node | Mesh and size | Class | Reason |
| --- | --- | --- | --- |
| `Visual/Bezel` | Cylinder r 0.45 m, length 0.35 m, cyan `#66b8c9` | solid | its own cylinder collider |
| `Visual/Foot` | Box 1.0 × 0.14 × 0.5 m, navy `#293954` | solid | its own box collider |
| `Visual/Dial/Face` | Cream `#fff8e9` disc with navy ticks, flush in the bezel | decorative | printed flat |
| `Visual/Dial/Needle` | Gold `#e8b764` bar | decorative | moved by a presentation binding |
| `Visual/Dial/Mark` | Cream tick | decorative | flush, posed from `threshold` (`ENGINE-PARAMETER-BOUND-FRAMES`) |
| `Visual/Lamp` | Flush disc under the dial | decorative | printed flat, glow |
| `Loc_Port_GasIn`, `Loc_Port_PowerIn`, `Loc_Port_Supply` | Locators | none | not rendered |

Icon: an upright round dial with one needle and a mark. It differs from the Air tank (a lying cylinder with a small dial) by its upright round outline, and from the Sound meter by having no horn or grille.

### Behaviour and numbers

- **Display convention.** Pressures are absolute everywhere, in declarations and on the dial (F1). An empty or unconnected gauge reads 101 325 Pa (shown as about 101 kPa, a display unit only), which is what the outcome sentence calls ambient.
- **Why 300 000 Pa.** It lies between ambient and the 400 000 Pa default charge. All values use the line start state F9, with the gauge's hose and the joining hose both on the supply tank's `GasOut`. Joining a default 400 000 Pa tank to an equal ambient tank settles at 256 400 Pa, (4000 + 320 + 1013.25) J over 0.0208 m³, below the 270 000 Pa off level, so the contact opens. Joining it to a 0.0065 m³ ambient tank settles at 287 800 Pa, (4000 + 320 + 658.61) J over 0.0173 m³, inside the 270 000–300 000 Pa band and at least 12 000 Pa from either edge (F6 allows 5800 Pa), so the contact holds.
- **Tick order (F5, D7).** The reading and `above` commit at the end of tick N; the electrical solve of tick N + 1 reads them.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S478): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction). Under D14 the row is navigation; this probe (21 November 2026, `8d1d68aa`) is the authority. Evidence for G rows is in EL-039 section 3.

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with baked colliders (G1) | present | EL-039 G1 |
| Supplied contact closed by a signal state | present | `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`); `CatalogueIntegrationTests.AnImpactSetsTheSuppliedLatchAndLightsItsLampWhileTheDisconnectedControlStaysOff` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:167`) |
| Lamp from committed supply | present | `PresentationDriver.CommittedSupply` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`); `DeclaredPresentationTests.BatterySupplyAndEachChargeBoundaryUseActualCommittedRead` (`CuriousContraptions.tests/DeclaredPresentationTests.cs:490`) |
| Wrong-kind refusal; authored-state Reset and save/load (G12) | present | EL-039 G12 |
| Fluid port contract (G5) | present, untested | EL-039 G5 |
| Gas connection kind and the gas `PortId` members (G6; role Bidirectional, no new `PortRole` member, EL-039 F3); a zero-flow tap on a store or line (G7) | missing | `ENGINE-GAS-NETWORK` |
| A signal node that thresholds a committed gas pressure with separate on and off levels | missing | `SignalNodeKind` has Contact, Indicator, Timer, Orientation, Passage, ContactLoad, Oscillator, PulseCount, Memory and PhysicalRequest only (`CuriousContraptions.Abstractions/SignalContractValues.cs:13`). Owner: `ENGINE-SIGNAL-NODES`, its threshold row over any committed scalar observation (owner decision 3a, coordinator decision 16); `ENGINE-GAS-NETWORK` adds only the committed pressure observation it reads |
| Needle from committed pressure (G9) | missing | `ENGINE-GAS-NETWORK` |
| Unit for Pa (G11) | missing | `ENGINE-GAS-NETWORK` |
| Several links on one Output port (G13) | present, untested | EL-039 G13; the gauge shares a tank outlet with a consumer through it |
| A presentation node posed at build from a Real parameter (the threshold mark) | missing | `ENGINE-PARAMETER-BOUND-FRAMES` (parameter-bound frames, coordinator decision 16) |

Counts: 4 present, 2 present but untested, 0 narrower, 5 missing. Missing: a presentation node posed at build from a Real parameter (the threshold mark). Owner `ENGINE-PARAMETER-BOUND-FRAMES` (coordinator decision 16). Bellows: no `elements/Bellows` package and no `spec-12-4-pneumatic-bellows.md` exist in this checkout.

**Dependencies.** A gas store to read; a Battery (delivered, `elements/Battery`) and a supplied load (delivered: `elements/PoweredGate`, `elements/Motor`) for the contact.

### Engine conformance (22 November 2026)

Checked against the owner's conformance instruction of 22 November 2026 and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One static body, two colliders, a fluid tap, one threshold signal node, one supply contact, three ports, one parameter and three presentation bindings. No script, callback or loop; the engine may not branch on `pressure_gauge`.
2. **Generic laws.** The reading is a committed observation of the gas network. The switch is a threshold with separate on and off levels over a committed scalar; it must be delivered as that general node, parameterised by the observation it reads, and not as a pressure-gauge rule. The delivered `ContactLoad` node thresholds a contact load in the same shape, and the Sound meter needs the same node over a sound level, so one node kind serves all three: the threshold row of `ENGINE-SIGNAL-NODES` (coordinator decision 16). The contact is the delivered supply-contact rule controlled by a signal state.
3. **Three layers.** *Colliders:* `col_` bezel cylinder and foot box on body 0. *Visuals:* the art table above; `Bezel`, `Foot`, `Dial/Face` and the locators are moved by nothing. *Animation:* `Dial/Needle` from the committed reading, `Lamp` from committed supply, `Dial/Mark` posed at build from the `threshold` parameter (`ENGINE-PARAMETER-BOUND-FRAMES`); all read-only and off the physics thread.
4. **One-way flow and typing.** Gas state → signal node → supply contact inside the physics tick; committed state → animation → renderer. Ports, roles and the parameter key are enums; pressures are f32.
5. **Flat-state counts per instance.** Bodies 1 (static); colliders 2 (1 cylinder, 1 box); joints 0; drives 0; sensors 1 (the gas tap observation); domain nodes 2 (one signal threshold node, one supply contact); ports 3; stores 0; bindings 3; parameters 1.
6. **Performance fitness.** Two primitive colliders, fewer than the Battery's three. Static body, so it joins no island. One scalar comparison per tick; no spatial query. Nothing scales with the scene.
7. **Tests are scenarios.** P-A1 to P-A10 author constructions and assert committed readings, contact states, supply and refusals on the production simulation.

## 4. Sources and legacy

- Requirement row [element-043](../requirements.md#element-043): "Measures local gas pressure independently of electrical controls"; outcome "Empty reservoir reads ambient rather than a stale charged state". No variants listed.
- Named entry [element-043](../invest/named-elements.md#element-043), owner S478. Gas design: "Sensor on a gas node | On/Off thresholds | Needle reads pressure; switches a supplied load at threshold | Needle ← committed pressure"; sensors use "threshold with separate On/Off hysteresis, sampled at substep endpoints".

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| M1 | Meter sockets: `PowerIn` (−0.92, −0.5, 0), `Supply` (0.92, −0.5, 0), contact route PowerIn → Supply closed while the boolean threshold state is true. | `parts/SoundMeterPart.cs@a6c914e:L50-L57` | carry forward the pattern |
| M2 | Hysteresis: once above, the state stays on while level > 0.9 × threshold; from below it needs level ≥ threshold. | `parts/SoundMeterPart.cs@a6c914e:L64-L71` and `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L55-L71` | carry forward |
| M3 | Needle follows the committed level with an eased response; lamp slate → gold 0.1 s SmoothStep on owner-active. | `parts/SoundMeterPart.cs@a6c914e:L40-L47` | carry forward as declared bindings |
| M4 | Reset clears the meter's reading and threshold state, because its source (sound) is silent after Reset. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L49-L51` | carry forward the rule "no state survives Reset"; for the gauge the restored source is the tank's authored charge, so the first reading after Reset is that authored pressure (ambient for an empty or unconnected tank) |

- **Files harvested:** `parts/SoundMeterPart.cs`, `CuriousContraptions.tests/SoundMeterTests.cs` (pattern facts); gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-043](../requirements.md#element-043), [pneumatics profile](../invest/profiles.md#pneumatics). Tolerances and stall guards are EL-039 F6. Planned test class `PressureGaugeTests`. Tanks are Air tanks; the load is a Powered gate.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| P-A1 | Given a 400 000 Pa tank joined to `GasIn` and no electrical link, then the reading is 400 000 Pa (independent of electrical controls) | `PressureGaugeTests.TheReadingNeedsNoElectricalSupply` | the same build with a battery reads the same | 600 000 Pa reads full scale |
| P-A2 | Requirement outcome: given a 400 000 Pa tank whose `GasOut` carries two hoses, one to the gauge and one with a Vent free end, when Run advances 20 s, then the reading falls to ambient and stays there | `PressureGaugeTests.AnEmptiedTankReadsAmbientNotItsEarlierCharge` | P-A1 holds 400 000 Pa | a gauge with nothing joined reads ambient |
| P-A3 | Given battery → `PowerIn`, `Supply` → gate and a 400 000 Pa tank, then the gate is powered; given no battery link, then it is never powered | `PressureGaugeTests.TheContactPassesSupplyOnlyAboveTheMarkAndOnlyWhenSupplied` | a 200 000 Pa tank with the battery: not powered | — |
| P-A4 | Given authored tanks at 306 000 Pa and at 294 000 Pa (2% either side of the mark, the F6 tolerance), then the first is On and the second Off after the first committed reading | `PressureGaugeTests.ATankAboveTheMarkClosesTheContactAndOneBelowDoesNot` | — | 294 000 Pa is inside the band, so it also proves the state starts Off |
| P-A5 | Given a 400 000 Pa tank whose `GasOut` carries the gauge hose and a hose to a 0.0065 m³ ambient tank (settles near 287 800 Pa), then the contact stays closed; given a 0.01 m³ ambient tank instead (near 256 400 Pa), then it opens | `PressureGaugeTests.TheContactHoldsInsideTheBandAndOpensBelowIt` | — | the 0.9 off level |
| P-A6 | Given a tank and a gauge only, when Run advances 20 s, then tank mass is unchanged (the tap draws nothing) | `PressureGaugeTests.TheTapDrawsNoGas` | — | — |
| P-A7 | Given a run that vented the tank, when Reset is pressed, then the next committed reading is the authored charge and the contact state follows it | `PressureGaugeTests.ResetReadsTheRestoredChargeAfresh` | the reading before Reset is ambient | — |
| P-A8 | Given a non-default `threshold` and three links, when saved and loaded, then they are equal; the reading is not stored | `PressureGaugeTests.SaveLoadKeepsThresholdAndLinks` | — | both limits; the mark sits at the needle angle for `threshold` |
| P-A9 | Given `threshold` outside its limits or non-finite, then it is refused with a typed reason | `PressureGaugeTests.InvalidThresholdIsRefusedAtomically` | the limits are admitted | next f32 beyond each limit |
| P-A10 | Given a hose offered to `PowerIn`, or a wire offered to `GasIn`, then each is refused | `PressureGaugeTests.WrongKindLinksAreRefused` | the correct links are admitted | — |

Generic cover today: G12 for authored-state Reset and save/load; P-A7 to P-A10 stay package tests until the S4 conformance suite is delivered.

**Chrome recipe (pending the post-catalogue browser phase).** Charged Air tank; Pressure gauge; Connect tank `GasOut` → gauge `GasIn`; Battery → gauge `PowerIn`; gauge `Supply` → a supplied load.

**Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393) ("Show stroke and reservoir state" in [todo-426](../requirements.md#todo-426)); interaction processes [IX-40 gas state evolution](../requirements.md#interaction-40), [IX-07 signal propagation](../requirements.md#interaction-07) and [IX-06 electrical power transfer](../requirements.md#interaction-06).

## 6. Open questions

1. **Contact scope.** Resolved (D19): the supplied threshold contact is part of this element. The gas design lists it, and chapter 8 asks for "pneumatic stroke → sound → supplied release", which needs a pressure-driven contact.
2. **Absolute or gauge display.** Resolved under the standing delegation: absolute everywhere (F1). One convention means the dial, the threshold and the outcome sentence ("reads ambient") all agree.
3. **How a gauge joins a tank that also feeds something.** Resolved by coordinator decision under the owner's standing delegation (22 November 2026), recorded in EL-039 as F3, F11 and decision 6: a second hose on the same `GasOut`. A tee part and a pass-through gauge port were rejected as larger. P-A2 and P-A5 are built this way.
4. **Line start state.** Resolved (EL-039 F9, decision 5): the gauge's hose starts at the tapped store's pressure, so P-A1 reads 400 000 Pa and P-A6 moves no gas. Under the earlier ambient-line reading both rows failed.

Needs the owner: none.

## 7. Integration

- **Works with.** EL-039 and EL-038 (not delivered); Battery, Motor and Powered gate (delivered).
- **Port agreement.** `GasIn` is a Fluid input (F3). A tank's `GasOut` takes several hoses, so a gauge and a consumer share it (F11); no junction part is needed. The same holds on a valve outlet, so a gauge can read a piston chamber's line.
- **Campaign.** The pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) did not name the gauge, so owner decision 24c places it in the air reservoir's first-use lesson at **72** (chapter 8): introduction 72, practice 73, reuse 87 and 147 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)). A level that pre-places it needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22), so such a level waits for it.
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7**, which also widens the reader for companion packages and non-`EL` identities (owner decision 15): the published form's preconditions are not met. This identity lacks its own `todo-NNN` and `sequence-task-NNN` pair co-anchored in `requirements.md` (the ledger entry links only the umbrella `todo-376`, which has no co-anchored sequence task and is shared by eight identities; the reader refuses two identities on one task source) and an `<a id="s478">` anchor for its ledger owner S478 in `docs/planning/invest/scope-corrections.md`.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SIGNAL-NODES` (threshold row) and `ENGINE-PARAMETER-BOUND-FRAMES` (the threshold mark); then `ENGINE-GAS-NETWORK` including the committed pressure observation; EL-039; EL-038 acceptance; S7.
- **Files.** Adds `elements/PressureGauge/PressureGauge.csproj`, `PressureGaugeElementDeclaration.cs`, `pressure_gauge.tscn`, `icon.svg` and `CuriousContraptions.tests/PressureGaugeTests.cs` (compiled by the test project's one rule; no compile line). Edits one line in `CuriousContraptions.slnx` and this spec; the per-package lists S3 owns until S3 lands.
- **Review tier.** Data-only, if the probe finds every missing row delivered.
- **Disqualifiers.** No threshold row over a committed scalar (returns the work to `ENGINE-SIGNAL-NODES`); no committed pressure observation, a tap that must own volume, or a reading that survives Reset (each returns it to `ENGINE-GAS-NETWORK`).
