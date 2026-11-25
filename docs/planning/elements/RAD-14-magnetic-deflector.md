# RAD-14 · Magnetic deflector — element readiness spec

**Current delivery (24 October 2026):** [RAD-14 delivery slice](../invest/named-elements.md#radiation-14). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-14](../requirements.md#radiation-14) (P3 potential). Lengths are metres; a beam enters along local +X; the poles lie on ±Z. Units, laws, the Relay, coupling and the rows are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions); all values are game fiction.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-14 · Magnetic deflector |
| Type | Radiation (supplied chamber that turns charged beams) |
| Anchor | [requirements.md#radiation-14](../requirements.md#radiation-14); [named-elements.md#radiation-14](../invest/named-elements.md#radiation-14) |
| Owner | S624 |
| Disposition | To deliver as a single element. It refines no CAT part and duplicates none. It is not a field-force part: it turns radiation beams and pushes no body, so it is not a sibling of the Electromagnet ([EL-060](EL-060-electromagnet.md)) in engine terms. |
| Planned package | id `magnetic_deflector`, directory `elements/MagneticDeflector/`, class `MagneticDeflectorElementDeclaration` |
| Related identities | Charged sources [RAD-03](RAD-03-alpha-source-cartridge.md), [RAD-04](RAD-04-beta-minus-source-cartridge.md); neutral controls [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md), [RAD-16](RAD-16-neutron-source-module.md); display [RAD-15](RAD-15-track-chamber.md); the Mirror ([CAT-041](CAT-041-mirror.md)) as the "cannot redirect" control |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 2 s plus the stall guard |

## 2. Declaration

Shape followed: the Powered gate for a supplied electrical load (`ISupplyElement` at `elements/PoweredGate/PoweredGateElementDeclaration.cs:14`, its supply graph of one consumer bound to `PowerIn` at `:88`–`:90`, its electrical node at `:94`), and the State latch for a lamp on a signal node's committed level (`elements/StateLatch/StateLatchElementDeclaration.cs:14`). The bend is not solved: it is a Relay (radiation convention 6, optical convention 7) whose outlets declare where each charged channel leaves and in which direction, the way the optical family declares refraction as its result. Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the relay interface **(field)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, `elements/Battery/BatteryElementDeclaration.cs:61`–`:64`) |
| Colliders (hidden `col_` proxies) | Base box 0.40 × 0.10 × 1.00 m at (0, −0.35, 0), no radiation row (the scene's default row). Two pole boxes 0.40 × 0.60 × 0.10 m at (0, 0, ±0.45), **Dense** row. Entry pane box 0.02 × 0.60 × 0.80 m at (−0.19, 0, 0) and exit pane box of the same size at (+0.19, 0, 0), **Window** row. One socket sphere radius 0.075 m at the port. Six colliders. The boxes touch and do not overlap; the socket sphere's centre lies on the +Z pole's top face, so its lower half is inside the pole, and that overlapped chord counts once with the larger (Dense) coefficient (radiation convention 4). |
| Ports | `PortId.PowerIn`, Electrical, Input, role `PowerIn`, at (0, 0.30, 0.45), normal +Y (on the top of the +Z pole) |
| Supply graph | One `SupplyConsumerDescriptor` `coil` with no mechanism target (an empty target list, admitted today as the System clock's consumer is, `elements/SystemClock/SystemClockElementDeclaration.cs:66`). It is a standing load: its demand is its electrical node's 12 W, granted and debited by the demand law, and its energy granted over the tick's substeps is committed with the tick **(field, ENGINE-SUPPLY-DEMAND)**. `PowerIn` → (`coil`, `SupplyTerminal.PowerIn`). `ElectricalNodes`: one Consumer row `coil`, 12 V, resistance 12 Ω, capacitance 0, maximum power 12 W. |
| Signal graph | One threshold node `energised` over the coil consumer's committed grant, as power (energy granted over the tick's substeps ÷ 1/120 s): on at ≥ 6 W, off below 0.75 × 6 = 4.5 W **(field, ENGINE-SIGNAL-NODES)**. No signal port. |
| Relay **(field)** | Inlet: a rectangle 0.60 (Y) × 0.60 (Z) m on the entry pane's outer face, centre (−0.20, 0, 0), normal −X. Its enable names the signal level `energised`. Each outlet is declared relative to the inlet hit point and the incoming direction: outlet point = hit point + an offset in the part's frame; outlet direction = incoming direction turned by a declared angle about the part's Y axis **(field, ENGINE-OPTICAL)**. Two rows, one compiled, selected by `polarity` **(field, ENGINE-PARAMETER-BOUND-FRAMES)**. **Forward:** outlet mask {Alpha}, offset (0.40, 0, −0.054), turned 15.5° toward −Z; outlet mask {BetaMinus}, offset (0.40, 0, +0.084), turned 23.6° toward +Z. **Reverse:** the same with the Z signs and turn senses swapped. Each outlet: τ 0.8, half-angle 0 (collimated), internal path 0.41 m deducted from range. Per channel the sum over outlets of τ × mask is 0.8 for Alpha, 0.8 for BetaMinus and 0 for the rest, so it is at most 1 and the bundle admits it (optical convention 7). Channels outside both masks, and every channel while `energised` is off, cross the panes by their rows (radiation convention 6). |
| Parameters | `polarity`: Choice, unit None, options Forward, Reverse, default Forward, build only, player-editable unless the level locks it (D11). No strength setting. |
| Stores | None |
| Presentation bindings | `Visual/CoilLamp` albedo slate (85, 101, 115) → gold (247, 203, 82) from `PresentationDriver.CommittedActivation` naming `energised`, `PresentationCurve.SmoothStep` over 0.125 s (the State latch's lamp, `elements/StateLatch/StateLatchElementDeclaration.cs:14`–`:16`). `Visual/PolarityArrow` shows the one of its two printed arrows that matches `polarity` **(field, ENGINE-PARAMETER-BOUND-FRAMES)**. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Electrical, Signal and the radiation flag **(field)** |
| Catalogue | Id `magnetic_deflector`, "Magnetic deflector", `ElementCategory.Radiation` **(field)**, palette order 715 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "While powered it turns alpha and beta beams, each its own way. Its setting swaps the two directions. Other radiation passes straight through." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base` | box 0.40 × 0.10 × 1.00 m, cream `#fff8e9` | solid (base box) | nothing |
| `Visual/PoleLeft`, `Visual/PoleRight` | boxes 0.40 × 0.60 × 0.10 m, navy `#293954`, coil windings printed flat | solid (pole boxes) | nothing |
| `Visual/EntryPane`, `Visual/ExitPane` | boxes 0.02 × 0.60 × 0.80 m, cyan `#66b8c9` glass | solid (pane boxes) | nothing |
| `Visual/CoilLamp` | disc r 0.03 m flush in the +Z pole's top | decorative: flush, recoloured by a binding | nothing (colour only) |
| `Visual/PolarityArrow` | two curved arrows printed flat on the base between the panes, one shown by option | decorative: printed flat | nothing |
| `Visual/PowerStud` | sphere r 0.075 m, gold `#f7cb52` | solid (socket sphere) | nothing |

The chamber is open at the top; a ball dropped in rests on the base between the panes like in any box. Icon: two blocks with a line bending between them. It differs from the Track chamber by the two heavy poles and the single curved arrow (the Track chamber is a glass box with three numbered segments).

### Compiled counts

Bodies 1 (static). Colliders 6 (5 boxes, 1 sphere); the panes carry the relay's inlet and outlets, so none is cosmetic. Joints 0. Drives 0. Sensors 0. Signal nodes 1 (threshold). Supply nodes 1 (consumer). Relays 1 compiled of 2 declared (2 outlets each). Stores 0. Presentation bindings 3 (lamp R, G, B) and one arrow selector.

## 3. Engine capabilities

Map row families (navigation, D14): EnvironmentState, FieldForce, FiniteLedger, GeometryQuery, IonizingTransport, RigidBodyDynamics. FieldForce and RigidBodyDynamics are not needed: the part applies no force. This probe, made on `8d1d68aa`, is the authority until the slice re-runs it: **6 present, 0 untested, 3 narrower, 6 missing**. ENGINE-SPATIAL-QUERIES, ENGINE-SUPPLY-DEMAND, ENGINE-SIGNAL-NODES and ENGINE-PARAMETER-BOUND-FRAMES are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`) |
| R3 | Typed ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R4 | The proportional allocation law that shares one source's allowance between requests | present for the law only: the coil's request is N3 | `ElectricalSupply.Reserve` (`engine/gpu/ElectricalSupply.cs:11`) | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` (`CuriousContraptions.tests/ElectricalSupplyTests.cs:207`) |
| R5 | A lamp binding on a signal node's committed level | present | `PresentationDriver.CommittedActivation` (`CuriousContraptions.Abstractions/ElementPresentation.cs:22`), declared on a node at `elements/StateLatch/StateLatchElementDeclaration.cs:14`, read at `engine/gpu/WorkshopDeclaredPresentation.cs:431`–`:436` | `DeclaredPresentationTests.CommittedLatchAndLampPreserveBothDirectionsAndDelayPhases` (`CuriousContraptions.tests/DeclaredPresentationTests.cs:415`). `CommittedSupply` is not used: it reads the owner's own electrical source (`engine/gpu/WorkshopCompiledScene.cs:231`–`:239`), which this part does not have |
| R6 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) serves airflow rows only and loops over every collider (`:149`) | — | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| N2 | Values bound to the options of a Choice: the compiled relay row and the shown polarity arrow | narrower: `ChoicePredicate` (`CuriousContraptions.Abstractions/AirflowValues.cs:23`) selects airflow input rows only | — | `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:540`). Owner: **ENGINE-PARAMETER-BOUND-FRAMES** (choice-bound values, coordinator decision 16) |
| N3 | A standing load: a consumer with no mechanism target whose declared demand is requested, granted, debited from its source and committed as a grant | narrower: the target kinds are continuous work, energy store, drive and transmission (`CuriousContraptions.Abstractions/SupplyContractValues.cs:44`–`:54`); a consumer with an empty target list is admitted (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:69`–`:72`) but requests nothing, since the electrical reservation collects only store charging and electrical airflow demand (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Supply.cs:88`–`:105`) | `SupplyConsumerDescriptor` | the same Fan test. Owner: **ENGINE-SUPPLY-DEMAND** (one demand law for every powered part) |
| M1 | The segment solve and the Relay response: inlet aperture, outlets with position, direction, τ, channel mask and internal path, and an enable that names a signal level | **missing** | — | owner **ENGINE-OPTICAL** (optical convention 7) |
| M2 | Relay outlets declared relative to the inlet hit point and the incoming direction (offset, rotation) | **missing** (optical convention 7 admits relative outlets since its amendment of 23 November 2026; no engine code) | — | owner **ENGINE-OPTICAL** (an addition to its Relay row) |
| M3 | Radiation channels, collimated radiation segments and per-collider attenuation rows | **missing** | — | owner **ENGINE-RADIATION** |
| M4 | Radiation channels in a Relay's outlet masks; unmasked channels, and every channel of a disabled relay, cross the inlet's collider by the attenuation law | **missing** | — | owner **ENGINE-RADIATION** (radiation convention 6) |
| M5 | `ElementCategory.Radiation` and the radiation domain flag | **missing** (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) | — | owner **ENGINE-RADIATION** |
| M6 | A threshold node with a release ratio over a committed scalar of another domain (here a supply consumer's committed grant), and the committed level it presents to a relay enable and to `CommittedActivation` | **missing** (no signal node reads a supply grant; the presentation read accepts only an activation phase, `engine/gpu/WorkshopDeclaredPresentation.cs:435`) | — | owner **ENGINE-SIGNAL-NODES** |

M4 has one owner. The Relay and its masks over a declared channel set are ENGINE-OPTICAL's (optical conventions 1 and 7: every optical rule is written over a declared channel set). What a radiation channel does at an inlet that does not relay it is a radiation rule, stated in radiation convention 6, and section 10 of RAD-08 is the family's input to ENGINE-RADIATION. RAD-03 and RAD-04 do not count this row; this package does.

Nothing radiation-specific exists in `CuriousContraptions.Abstractions*`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle`, `engine/` or `elements/`. ENGINE-RADIATION ([Story ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure), sprint key `engine-radiation`) is the one owner of the radiation-specific rows.

**Dependencies.** Battery (delivered); RAD-03, RAD-04 and RAD-08 (undelivered) for every charged row.

## 4. Sources and legacy

- [radiation-14](../requirements.md#radiation-14): "Polarity reversal swaps the charged route; gamma/X-ray controls remain straight. Field-off and opposite-charge cases are separately proven."
- [Named entry](../invest/named-elements.md#radiation-14), owner S624; map composition "declare finite ElectricalPower/field supply and reversible polarity; charged path model differs from Photon and neutron channels"; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) beta-minus row; research "Charged particles" law and slots 93 "Around the Corner", 94 "Opposite Ways".
- **Legacy.** No magnetic deflector, and no magnet or field code at all in `parts/`, `engine/`, tests, levels or Campaign. Hits are coverage snapshots only: the older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2070-L2102`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Where the outlet values come from.** A charged beam in a uniform field follows a circle. Over a 0.40 m region, a radius r gives a sideways offset r − √(r² − 0.40²), an exit angle asin(0.40 ÷ r) and an arc length r × angle. Alpha, r = 1.5 m: offset 1.5 − √2.09 = 1.5 − 1.4457 = 0.054 m, angle asin 0.2667 = 15.5°, arc 1.5 × 0.2699 = 0.405 m. BetaMinus, r = 1.0 m: offset 1 − √0.84 = 1 − 0.9165 = 0.084 m, angle asin 0.4 = 23.6°, arc 0.412 m. The two radii differ, as the row requires, and the opposite charges turn opposite ways. These results are declared; no arc is traced at Run.
- **Where a beam leaves.** The outlet is the inlet hit point plus the declared offset, so a beam entering anywhere on the inlet leaves the exit pane's outer face (x = +0.20) beside its own entry line, never from one fixed point. Entering on the axis, Forward Alpha leaves at z = −0.054 and BetaMinus at z = +0.084. The inlet is 0.60 m wide (|z| ≤ 0.30) so that every outlet lies on the exit pane: 0.30 + 0.084 = 0.384 < 0.40.
- **After the outlet.** One metre beyond the exit pane the BetaMinus beam is 0.084 + tan 23.6° = 0.084 + 0.436 = 0.520 m off its entry line on one side and the Alpha beam 0.054 + tan 15.5° = 0.054 + 0.277 = 0.331 m off it on the other. A BetaMinus beam entering at z = +0.25 leaves at 0.25 + 0.084 = 0.334 and is at 0.334 + 0.436 = 0.770 m one metre beyond.
- **Power.** A 1 W beam leaves a powered deflector at 0.8 W. Unpowered, or for a channel outside the masks, the beam crosses both panes by the Window row (Alpha and BetaMinus 5 /m × 0.02 m = 0.1 each): 1 × e^−0.1 × e^−0.1 = 0.819 W, straight. 0.8 ≤ 0.819, so turning never beats going straight.
- **Range.** The relay's 0.41 m is deducted. Alpha (range 2.0 m) from a cartridge 0.30 m before the inlet reaches a meter 1.0 m along the outlet ray: 0.30 + 0.41 + 1.0 = 1.71 m. The beam ends 2.0 − 0.71 = 1.29 m along the outlet ray, so a meter at 1.5 m (0.21 m beyond the end) reads nothing, and one at 1.0 m is 0.29 m inside it.
- **Neutral radiation.** Photons and neutrons are not in the masks: they cross the panes by the Window row and go straight, powered or not, in both polarities: PhotonMedium e^(−0.5 × 0.02) = 0.990 per pane, 0.980 for both; NeutronFast e^(−1 × 0.02) = 0.980 per pane, 0.961 for both. A path along a pole crosses 0.40 m of Dense: PhotonMedium e^(−20 × 0.4) = e^−8 = 0.0003, NeutronFast e^(−1.6 × 0.4) = 0.53, Alpha and BetaMinus e^(−2000 × 0.4) = 0.
- **Coil.** Demand 12 W, 0.1 J per tick; ten such coils run from one default Battery (120 W). `energised` turns on when the committed grant reaches 6 W and off below 4.5 W. The outlet power does not scale with the grant: the field does no work on the beam, so the granted energy is the coil's loss, debited from the Battery by the demand law, and no energy enters the radiation. At the Battery's 10 W minimum `power_limit` the coil is granted 10 W, 67 % above the 6 W mark, so the relay is enabled and the outlet power is still 0.8 W. Three coils on one Battery at 10 W are granted 10 ÷ 3 = 3.33 W each, 44 % below the mark and 26 % below the 4.5 W release, so none is enabled.
- **Model limit.** The declared offsets and turns are exact for a beam entering along +X; a beam entering obliquely is turned by the same angle from its own direction and leaves at the same offset from its hit point. A beam crossing the entry pane outside the inlet (0.30 < |z| < 0.40) crosses both panes straight by the Window row. A beam entering through the exit pane is not turned.
- **Tick order.** The coil is granted energy in tick n's substeps and the grant commits with tick n. The signal network of tick n + 1 turns `energised` on, and the field solve of tick n + 1, after its last substep, turns the beams; a meter's reading of that solve commits with tick n + 1 and acts from tick n + 2. When supply is lost in tick n, tick n's solve still turns the beams (it reads `energised` as resolved at the start of tick n) and they go straight from tick n + 1. The one-tick latency is the convention's (radiation convention 11).

### Rows

Planned class `MagneticDeflectorTests`. Fixture: Battery `Supply` → deflector `PowerIn`; a cartridge 0.30 m before the inlet, aimed along +X on the entry axis unless a row says otherwise; one supplied standard meter set to the beam's channel and facing the deflector, placed per case at one of three points 1.0 m beyond the exit pane: z = 0 (straight), z = +0.520 (the Forward BetaMinus ray) or z = −0.331 (the Forward Alpha ray). One meter per case, because two meter cases (0.50 m wide in Z) at z = 0 and z = −0.331 would overlap. Powers ±3 %; positions 0.02 m; stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| D1 | Given Forward and a BetaMinus cartridge, then the meter at the +Z point reads 0.8 W and the meter at either other point reads nothing; with an Alpha cartridge the meter at the −Z point reads 0.8 W and the other two nothing (row: opposite charges, separately proven) | `MagneticDeflectorTests.OppositeChargesTurnOppositeWays` (theory over channel and meter point) | D3 | ±3 %; nothing is below 0.001 W |
| D2 | Given Reverse, then each cartridge's beam reaches the meter at the point mirrored in z (BetaMinus at z = −0.520, Alpha at z = +0.331) and nothing reaches the Forward points (row: polarity reversal swaps the charged route) | `MagneticDeflectorTests.ReversingPolaritySwapsTheRoutes` (theory) | D1 | ±3 % |
| D3 | Given no supply, then either cartridge's beam reaches the straight meter at 0.819 W and a meter at either side point reads nothing (row: field off) | `MagneticDeflectorTests.AnUnpoweredDeflectorPassesBeamsStraight` (theory) | D1 | ±3 % |
| D4 | Given a Gamma capsule, an X-ray emitter or a Neutron module on the entry axis, then the straight meter's reading is the same powered and unpowered and in both polarities (row: gamma and X-ray controls remain straight) | `MagneticDeflectorTests.NeutralRadiationIsNeverTurned` (theory; queued per source until it is delivered) | D1 | the four readings equal within ±3 % |
| D5 | Given Forward, then the BetaMinus segment's end point 1.0 m beyond the exit pane is 0.520 m off axis and the Alpha one 0.331 m off axis on the other side (row: distinct curvature) | `MagneticDeflectorTests.EachChannelHasItsOwnTurn` | D1 | 0.02 m |
| D6 | Given the Alpha meter 1.0 m along the outlet ray it reads 0.8 W; at 1.5 m it reads nothing | `MagneticDeflectorTests.TheTurnSpendsRange` | D1 | 0.29 m inside and 0.21 m beyond the 1.29 m end |
| D7 | Given a cartridge aimed along +X at z = 0.45 (into a pole), then no segment leaves the deflector | `MagneticDeflectorTests.ABeamIntoAPoleIsAbsorbed` | D1 | below 0.001 W |
| D8 | Given a Mirror in an Alpha or BetaMinus beam, then no segment leaves the mirror's face in the reflected direction (row context: ordinary mirrors cannot redirect these channels; queued until CAT-041 exists) | `MagneticDeflectorTests.AMirrorNeverRedirectsACharge` | a laser reflects | segment count 0 |
| D9 | Given the default Battery, over 120 powered ticks the Battery is debited 12 J and the outlet carries 0.8 W; given `power_limit` at its 10 W minimum, the relay is still enabled, the outlet carries 0.8 W and the Battery is debited 10 J over 120 ticks | `MagneticDeflectorTests.TheCoilIsPaidAndAddsNothingToTheBeam` | D3 | ±3 % |
| D10 | Given a signal output wired to `PowerIn`, then the edit is refused; an option outside Forward and Reverse is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (R3); `MagneticDeflectorTests.AnUndefinedPolarityIsRefused` | — | typed refusal |
| D11 | Given a powered Run, when Reset, then no segment remains and the lamp is at rest; a saved and reloaded construction keeps `polarity` and behaves as D1 or D2 | `MagneticDeflectorTests.ResetAndReloadKeepPolarity`; authored state by R6 | D1 | segment count 0 |
| D12 | Given three deflectors on one Battery at `power_limit` 10 W, each coil is granted 3.33 W and a BetaMinus beam through one of them reaches the straight meter at 0.819 W; with that deflector alone on the Battery the beam is turned | `MagneticDeflectorTests.AnUnderSuppliedCoilDoesNotEnergise` | the single deflector | 44 % below the 6 W mark; ±3 % |
| D13 | Given a turned BetaMinus beam, when the Battery is disabled by its run control on tick n, then the beam is turned on tick n and straight from tick n + 1; when it is enabled again on tick m, the beam is straight on tick m and turned from tick m + 1 | `MagneticDeflectorTests.TheBeamFollowsTheCoilOneTickLater` | D1 | exact tick |
| D14 | Given Forward and a BetaMinus cartridge aimed along +X at z = +0.25, then the segment leaves the exit pane at z = 0.334 and ends 1.0 m beyond it at z = 0.770, where a meter reads 0.8 W; aimed at z = +0.35, outside the inlet, the beam goes straight and a meter at z = 0.35 reads 0.819 W | `MagneticDeflectorTests.TheOutletFollowsTheEntryPoint` | D5 (on-axis entry) | 0.02 m; ±3 % |

Chrome recipe (pending, post-catalogue): Battery, Beta-minus cartridge, deflector, a meter with a lamp; Run; switch `polarity`; unplug the Battery.

## 6. Open questions

None open.

1. **Polarity in Build or Run — decided** (D19): a build-time setting. No row or lesson switches it by signal.
2. **Does it act on balls — decided now** (D14): no. The row is about charged-particle routes; the map's FieldForce and RigidBodyDynamics families are not used, and ENGINE-FIELD-FORCE is not a prerequisite. Magnetic bodies are the Electromagnet's subject.
3. **Curvature values and sampling — decided** (D1, D16): radii 1.5 m and 1.0 m, declared as outlet results; charged sources are collimated beams (radiation convention 3), so there is no cone to sample.
4. **Strength setting — decided now** (D13): none. The row asks for reversible polarity and distinct curvatures, not an adjustable field; a strength would need outlets computed from a parameter.
5. **Fixed or entry-relative outlets — decided now** (audit M-4): entry-relative. Fixed outlets would make a beam entering anywhere on the inlet jump up to 0.4 m sideways to one point, which no player would read as a magnet bending a beam; shrinking the inlet to a small window instead would leave most of a powered chamber passing beams straight. The inlet is narrowed only enough to keep every outlet on the exit pane.
6. **Coupling — decided** (coordinator decision 16): coil grant → threshold node `energised` → relay enable. The relay does not read the supply directly.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery (delivered); RAD-03, RAD-04, RAD-08, RAD-15, RAD-01, RAD-02, RAD-16, CAT-041 (undelivered). `PowerIn` is Electrical Input against the Battery's `Supply`. With the Track chamber 0.07 m behind the exit pane and the beam entering on the axis, Alpha lands 0.054 + 0.65 × tan 15.5° = 0.234 m and BetaMinus 0.084 + 0.65 × tan 23.6° = 0.368 m off axis on its receivers' face, in its outer segments, and the turned Alpha beam reads 0.8 × e^(−5 × 0.02 ÷ cos 15.5°) = 0.72 W there ([RAD-15](RAD-15-track-chamber.md) row K8).
- **Processes.** [IX-15](../requirements.md#interaction-15) and [IX-06](../requirements.md#interaction-06). The generic interaction register has no field-force row and none is needed.
- **Campaign.** Chapter 12, levels 118 and 119; reuse 121–125 and 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-SUPPLY-DEMAND, ENGINE-SIGNAL-NODES and ENGINE-PARAMETER-BOUND-FRAMES; ENGINE-OPTICAL (the Relay, with entry-relative outlets); ENGINE-RADIATION as amended; RAD-03, RAD-04 and RAD-08; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story).
- **Files.** `elements/MagneticDeflector/MagneticDeflector.csproj`, `MagneticDeflectorElementDeclaration.cs`, `magnetic_deflector.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/MagneticDeflectorTests.cs`; a row in `VisibleSolidColliderTests`. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** ENGINE-OPTICAL delivering no Relay, a relay enable that cannot name a threshold node's level, or no entry-relative outlets (then the slice stops and the story is amended; no second design is kept here); charged sources delivered as cones (then the relay has no single inlet segment and this design is redone); a relay whose disabled or unmasked channels are absorbed instead of crossing by their rows (field-off and neutral rows fail); a threshold node that cannot read a supply consumer's committed grant; a target-less consumer that ENGINE-SUPPLY-DEMAND does not debit.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** One static body, six colliders, one port, one supply consumer, one threshold node, two relay rows, one parameter, three lamp bindings and an arrow selector. No code names the deflector and nothing integrates a trajectory.
2. **Generic laws.** The demand law of a standing load; a threshold node over a committed scalar; the Relay response with channel masks and entry-relative outlets; chord attenuation for everything else. "Polarity swaps the route" is a Choice selecting one of two declared rows.
3. **Three layers.** Colliders: six primitives. Visuals: the art table; windings and arrows are printed. Animation: the coil lamp's colour binding on a committed level.
4. **One-way flow and typing.** Supply grant → threshold node → relay enable → segments → readings; presentation reads committed state only. Channels, options and roles are enums; values are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time; one relay row compiles.
6. **Performance fitness.** Static body; six primitives; a charged beam costs one more segment cast after the relay, and a pass-through collimated ray is one cast returning every crossing (radiation convention 12); no region is scanned.
7. **Tests are scenarios.** Rows D1 to D14 run the production simulation through placement, wiring, Run and committed reads.
