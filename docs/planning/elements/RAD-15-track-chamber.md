# RAD-15 · Track chamber — element readiness spec

**Current delivery (24 October 2026):** [RAD-15 delivery slice](../invest/named-elements.md#radiation-15). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-15](../requirements.md#radiation-15) (P3 potential). Lengths are metres; a beam enters along local +X; the three segments lie along Z. Units, laws, receivers, coupling and the Window and Dense rows are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions); all values are game fiction.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-15 · Track chamber |
| Type | Radiation (segmented charged-beam detector with supplied contacts) |
| Anchor | [requirements.md#radiation-15](../requirements.md#radiation-15); [named-elements.md#radiation-15](../invest/named-elements.md#radiation-15) |
| Owner | S625 |
| Disposition | To deliver as a single element. It refines no CAT part and duplicates none: it is the rate meter's receiver, threshold node and contact three times over, on one box that also displays the beam. |
| Planned package | id `track_chamber`, directory `elements/TrackChamber/`, class `TrackChamberElementDeclaration` |
| Related identities | Charged sources [RAD-03](RAD-03-alpha-source-cartridge.md), [RAD-04](RAD-04-beta-minus-source-cartridge.md); steering [RAD-14](RAD-14-magnetic-deflector.md); neutral controls [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-16](RAD-16-neutron-source-module.md); pattern [RAD-08](RAD-08-radiation-rate-meter.md) |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 2 s plus the stall guard |

## 2. Declaration

Shape followed: the rate meter ([RAD-08 section 2](RAD-08-radiation-rate-meter.md#2-declaration)): a receiver commits a strength; a threshold node over that committed strength is its level; a supply contact with `SignalStateContactControl` names the threshold node. The chamber declares three of each. One port binds one supply endpoint (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:144` refuses a second binding of the same port), so `PowerIn` feeds one contact `feed`, whose output fans out to the three segment contacts. The player selects an exit segment by wiring that segment's output; there is no selector setting. Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the receiver interface **(field)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, `elements/Battery/BatteryElementDeclaration.cs:61`–`:64`) |
| Colliders (hidden `col_` proxies) | Base box 0.64 × 0.06 × 0.94 m at (0, −0.33, 0), no radiation row (the scene's default row). Entry pane box 0.02 × 0.60 × 0.90 m at (−0.29, 0, 0); two side pane boxes 0.56 × 0.60 × 0.02 m at (0, 0, ±0.46); top pane box 0.60 × 0.02 × 0.94 m at (0, 0.31, 0); these four with the **Window** row. Back plate box 0.02 × 0.60 × 0.90 m at (+0.29, 0, 0) with the **Dense** row (Alpha and BetaMinus 2000 /m: e^(−2000 × 0.02) = e^−40, so a charged beam ends inside it). Four socket spheres radius 0.075 m at the ports. Ten colliders; the panes and plate touch and do not overlap. Interior 0.56 × 0.60 × 0.90 m. |
| Ports | `PortId.PowerIn`, Electrical, Input, role `PowerIn`, at (−0.32, −0.33, 0), normal −X. Three package-defined ids `SupplyLeft`, `SupplyCentre`, `SupplyRight`, each Electrical, Output, role `Supply`, at (0.32, −0.33, −0.30), (0.32, −0.33, 0) and (0.32, −0.33, +0.30), normal +X, each under its segment. |
| Receivers **(field)** | Three, on the back plate's inner face (x = +0.28, normal −X), each a rectangle 0.30 (Z) × 0.60 m: Left z from −0.45 to −0.15, Centre −0.15 to +0.15, Right +0.15 to +0.45. Channel weights Alpha 1, BetaMinus 1, all others 0; Lambert facing. A collimated beam that crosses a segment's aperture from the front and ends in the back plate behind it is that segment's reading. |
| Signal graph | Three threshold nodes `left`, `centre`, `right`, each over its receiver's committed strength: on at ≥ 0.2 W, off below 0.75 × 0.2 = 0.15 W **(field, ENGINE-SIGNAL-NODES)**. One boolean OR node `any` over the three, resolved in the same signal-network pass as the threshold nodes it reads **(field, ENGINE-SIGNAL-NODES)**. No signal port. |
| Supply graph | Contact `feed` with `SignalStateContactControl` naming `any`; contacts `relayLeft`, `relayCentre`, `relayRight`, each with `SignalStateContactControl` naming its threshold node. `PowerIn` → `feed`; edges `feed` Output → each segment contact's `PowerIn`; each segment contact's Output → its port. A segment's output is live exactly when its threshold node is on and `PowerIn` is supplied, because `any` is on whenever a segment node is. |
| Parameters | None. |
| Stores | None |
| Presentation bindings | Three segment lamps `Visual/LampLeft`, `Visual/LampCentre`, `Visual/LampRight`, albedo slate → gold from each threshold node's committed level. Trails: the shared beam renderer draws committed Alpha and BetaMinus segments inside the declared interior box, fading over 1.5 s **(field, ENGINE-OPTICAL display volume)**. The trail is presentation and feeds nothing. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Electrical, Signal and the radiation flag **(field)** |
| Catalogue | Id `track_chamber`, "Track chamber", `ElementCategory.Radiation` **(field)**, palette order 716 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Shows alpha and beta beams as trails. The output under the segment a beam ends on passes electricity." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base` | box 0.64 × 0.06 × 0.94 m, cream `#fff8e9` | solid (base box) | nothing |
| `Visual/EntryPane`, `Visual/SideLeft`, `Visual/SideRight`, `Visual/Top` | glass boxes equal to their colliders, cyan `#66b8c9` | solid (pane boxes) | nothing |
| `Visual/BackPlate` | box equal to its collider, navy `#293954` | solid (back plate box) | nothing |
| `Visual/Dividers` | two cream `#fff8e9` lines and the numerals 1, 2, 3 printed flat on the back plate's inner face | decorative: printed flat | nothing |
| `Visual/LampLeft`, `Visual/LampCentre`, `Visual/LampRight` | discs r 0.03 m flush in the base's +X edge | decorative: flush, recoloured by a binding | nothing (colour only) |
| trails | gold `#f7cb52` lines drawn by the shared beam renderer | decorative: drawn from committed segments | the renderer |
| four socket studs | spheres r 0.075 m, gold | solid (socket spheres) | nothing |

Icon: a glass box with a dark back plate and a diagonal trail ending on one of three numbered segments. It differs from the Magnetic deflector by having no poles and by the three numbered segments.

### Compiled counts

Bodies 1 (static). Colliders 10 (6 boxes, 4 spheres). The rate meter has 5; the extra five are the box, which must be solid because it is visible, and one more output socket per segment. Joints 0. Drives 0. Sensors 0. Signal nodes 4 (three threshold, one boolean). Supply nodes 4 (contacts). Receivers 3 (one aperture each; a collimated beam needs no sample grid). Stores 0. Presentation bindings 9 (three lamps × R, G, B) and one trail display.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, SignalPropagation. Probe made on `8d1d68aa`: **5 present, 2 untested, 2 narrower, 5 missing**. ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:206`) |
| R3 | Typed ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R5 | Supply contact closed by a signal state, between a source and a consumer | present | `SupplyContactDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:40`), `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`) | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:530`, fixture at `:453`–`:466`) |
| R6 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| U1 | Three output ports with package-defined ids that share the role `Supply` | present, untested: `PortId` is a package-defined value (`CuriousContraptions.Abstractions/PortId.cs:6`–`:11`); `PortRole` is a closed enum with one `Supply` member (`PortDescriptor.cs:20`–`:35`); duplicates are refused by `PortId` only (`engine/gpu/ElementPortCatalogue.cs:102`–`:103`); supply binds by `PortId` (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:137`–`:152`, `engine/gpu/WorkshopSupplyCompiler.cs:54`–`:63`); roles are checked only on Mechanical ports (`CuriousContraptions.Bundle/BundleValidator.cs:377`–`:399`). No delivered package repeats an electrical role, so no test exercises it | — | the slice's probe proves it with this package, or names an abstraction slice |
| U2 | One contact's output fanned out to several contacts inside a package | present, untested: the bundle refuses fan-in only into a consumer (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:165`), and the route powers a contact from any powered edge into its `PowerIn` (`engine/gpu/SupplyRoute.cs:370`–`:373`). No delivered package fans one output out, so no test exercises it | — | the slice's probe proves it with this package, or names an abstraction slice |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) serves airflow rows only and loops over every collider (`:149`) | — | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast: **ENGINE-SPATIAL-QUERIES** |
| N2 | A lamp driven by a threshold node's committed level | narrower: no member for a receiver's level (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`); the delivered socket driver serves only ports bound to a logic node (`SupplyBundleValidation.cs:195`) | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`, socket lamps on logic-node ports). Owner: **ENGINE-SIGNAL-NODES** (the committed level a threshold node presents to `PresentationDriver.CommittedActivation`, `engine/gpu/WorkshopDeclaredPresentation.cs:431`–`:436`; one owner with RAD-08, RAD-14 and EL-129) |
| M1 | Receivers on a collider face with a committed reading and strength; the committed segment table | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels, collimated radiation segments, per-collider rows, weighted-sum strength | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | A declared display volume in which the shared renderer draws committed segments of declared channels with a fade | **missing** | — | owner **ENGINE-OPTICAL** (the shared segment renderer, optical convention 12) |
| M4 | `ElementCategory.Radiation` and the radiation domain flag | **missing** (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) | — | owner **ENGINE-RADIATION** |
| M5 | A threshold node with a release ratio over a committed scalar, a boolean OR node over levels resolved in the same pass, and the contact state each presents to `SignalStateContactControl` | **missing** (a contact today names `SignalContactState.Latched` or `TimerCounting`, `CuriousContraptions.Abstractions/SignalContactControl.cs:5`) | — | owner **ENGINE-SIGNAL-NODES** |

**Dependencies.** Battery and Powered gate (delivered); RAD-03 or RAD-04 for every positive row; RAD-14 for the steering row (undelivered).

## 4. Sources and legacy

- [radiation-15](../requirements.md#radiation-15): "The selected exit segment triggers only for a traversing track; a decorative trail or a miss cannot trigger it. Neutral photons make no direct charged track in the initial abstraction."
- [Named entry](../invest/named-elements.md#radiation-15), owner S625; map row (S605, S257); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) beta-minus row; research row ("only that segment's contact fires"; slots 95 "A Line in the Mist", 96 "Read the Turn").
- **Legacy.** None. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2103-L2133`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **A track is a beam.** Alpha and BetaMinus sources are collimated (radiation convention 3), and every collider passes radiation by its row (convention 4). A beam aimed into the chamber crosses the entry pane (Window row, 5 /m over 0.02 m: e^−0.1 = 0.905), crosses the interior, crosses one segment's aperture on the back plate's inner face from the front, and ends inside the back plate (e^−40). The segment reads its power: 0.905 W from a 1 W cartridge, 353 % above the 0.2 W threshold. "Traversing" is exactly this: the beam entered the box and ended on the inner face. A beam that arrives at the back plate from outside meets its outer face, ends inside the plate and never reaches an aperture; it would also have facing 0.
- **The rule the rows assume.** A collimated segment is one cast returning every crossing (radiation convention 12), so its power at an aperture is its power after every crossing before it, the entry pane included. This is the one own-body rule of optical convention 5 and radiation convention 4 (coordinator decision 26): every crossing before a receiver's aperture face acts, the receiver's own colliders included, and nothing beyond the face is read; a collimated segment then continues past the aperture and is attenuated by every body on its path. A Lambert receiver is credited by a collimated segment that crosses its aperture from the front and ends in the frame body behind it.
- **Selection.** Each segment has its own receiver, threshold node, contact and output. Only the output under the struck segment is live, and only while `PowerIn` is supplied: `feed` closes when any segment node is on, and each segment contact passes only when its own node is on.
- **Neutral radiation.** Photon and neutron weights are 0, so those sources give no reading on any segment, and the renderer draws trails for Alpha and BetaMinus only.
- **Trails.** Drawn from the committed segment table by the renderer. Hiding them, or running with no presentation consumer, changes no reading.
- **Behind the deflector.** With the chamber's entry pane 0.07 m beyond the deflector's exit pane, the receivers' face is 0.07 + 0.02 + 0.56 = 0.65 m beyond the outlets. Alpha lands at 0.054 + 0.65 × tan 15.5° = 0.054 + 0.180 = 0.234 m off axis, BetaMinus at 0.084 + 0.65 × tan 23.6° = 0.084 + 0.284 = 0.368 m on the other side. Both are inside the outer segments (0.15 to 0.45 m): Alpha 0.234 − 0.15 = 0.084 m inside the inner divider, BetaMinus 0.45 − 0.368 = 0.082 m inside the outer edge. Those margins are four times the 0.02 m position tolerance. Row K8's beam enters the deflector's inlet on its axis, so the margins hold whether RAD-14's outlets are fixed or declared relative to the inlet hit point (ENGINE-OPTICAL). With the field off both land at 0, in Centre.
- **Power and range behind the deflector.** Alpha: 0.8 W from the relay × e^(−5 × 0.02 ÷ cos 15.5°) = 0.8 × e^(−5 × 0.0208) = 0.8 × 0.901 = 0.72 W at the segment. Path: 0.30 m to the deflector + 0.41 m inside + 0.65 ÷ cos 15.5° = 0.67 m beyond = 1.38 m, inside the 2.0 m range.
- **Tick order.** The field solve of tick n runs after the last substep on that tick's final poses and commits the three strengths; the signal network of tick n + 1 turns the struck segment's threshold node and `any` on and closes `feed` and that segment's contact (radiation convention 11). The load is supplied one tick after the beam arrives.
- **Energy.** The chamber absorbs the beam in its back plate and adds nothing; its contacts pass only what `PowerIn` receives.

### Rows

Planned class `TrackChamberTests`. Fixture: Battery `Supply` → `PowerIn`; a Powered gate on each output (the Signal lamp is a signal load, not an electrical one); a Beta-minus cartridge 0.5 m before the entry pane, aimed along +X at a chosen z. Powers ±3 % (the nearest decision margin is 260 %); positions 0.02 m; stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| K1 | Given the beam at z = −0.30, 0 and +0.30 in turn, then only `SupplyLeft`, only `SupplyCentre` and only `SupplyRight` is live, with a segment reading of 0.905 W (row: the selected exit segment triggers for a traversing track) | `TrackChamberTests.OnlyTheStruckSegmentIsLive` (theory) | K2 | ±3 %; the other two readings under 0.001 W |
| K2 | Given the beam at z = +0.13 it is Centre; at +0.17 it is Right | `TrackChamberTests.ABeamBesideADividerBelongsToOneSegment` | K1 | 0.02 m either side of the divider |
| K3 | Given the beam passing 0.2 m beside the chamber, or aimed at the back plate from outside, then no output is live for 240 ticks (row: a miss cannot trigger it) | `TrackChamberTests.AMissOrABackwardBeamSwitchesNothing` (theory) | K1 | all readings under 0.001 W |
| K4 | Given a Gamma capsule or a Neutron module 0.5 m before the entry pane, then no segment reads anything and no trail segment exists in the display volume (row: neutral photons make no charged track) | `TrackChamberTests.NeutralRadiationLeavesNoTrack` (theory; the neutron case queued until RAD-16 exists) | K1 | under 0.001 W |
| K5 | Given K1 with and without a presentation consumer, then the committed readings, levels and the tick of first supply are the same (row: a decorative trail cannot trigger it) | `TrackChamberTests.TrailsNeverChangeTheReadout` | K1 | equal decoded levels |
| K6 | Given no wire to `PowerIn`, then the struck segment's threshold node is on and no output is live | `TrackChamberTests.AnUnsuppliedChamberSwitchesNothing` | K1 | 240 ticks |
| K7 | Given an Alpha cartridge 2.2 m before the back plate's inner face, then no segment reads anything | `TrackChamberTests.ABeamBeyondItsRangeLeavesNoTrack` | Alpha at 1.0 m reads 0.905 W | 0.2 m beyond the range |
| K8 | Given an Alpha cartridge, the deflector and the chamber in line as above: Forward gives one outer segment, Reverse the other, an unpowered deflector gives Centre; the BetaMinus cartridge gives the opposite outer segments (queued until RAD-14 exists) | `TrackChamberTests.TheDeflectorMovesTheTrackBetweenSegments` (theory) | K1 | reading 0.72 W ±5 % for Alpha when turned |
| K9 | Given an electrical output wired to `SupplyLeft`, or a segment output wired to a signal input, then the edit is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (R3) | — | typed refusal |
| K10 | Given a lit Run, when Reset, then no reading, level or trail remains; a saved and reloaded construction with its three wires behaves as K1 | `TrackChamberTests.ResetAndReloadRestoreTheDarkChamber`; authored state by R6 | K1 | readings 0 |

No parameter, so no parameter-refusal row. Chrome recipe (pending, post-catalogue): Battery, Beta-minus cartridge, chamber, a Powered gate on `SupplyRight`; Run; then move the cartridge sideways.

## 6. Open questions

None open.

1. **Number of segments — decided** (D13): three, fixed.
2. **Hold time and divider rule — decided now**: no hold. The level follows the reading with the family's release ratio, and a steady beam gives a steady contact. A beam is a zero-width ray, so it crosses one aperture; which aperture owns a shared edge is the engine's aperture rule, and no row tests the edge itself.
3. **One representative track — resolved by the conventions**: charged sources are beams, so there is one track per source by construction.
4. **Selector — decided now** (D19): the segment is chosen by wiring one of three outputs, not by a setting. It needs no parameter-selected contact and lets one chamber drive three loads.
5. **Coupling — decided** (coordinator decision 16): three strengths → three threshold nodes → three signal-state contacts, fed through `feed` because one port binds one endpoint. No memory node is set by a receiver.
6. **Where a beam ends — decided now**: in a Dense back plate, so a traversing beam ends on the segment it crosses and a beam from outside never reaches an aperture.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Powered gate (delivered); RAD-03, RAD-04, RAD-14, RAD-01, RAD-16 (undelivered). `PowerIn` is Electrical Input against the Battery's `Supply`; the three outputs are Electrical Outputs against a load's `PowerIn`.
- **Processes.** [IX-15](../requirements.md#interaction-15), [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07).
- **Campaign.** Chapter 12, levels 119 and 121 (the introduction moved from the chapter 12 finale 120 by coordinator decision 35); reuse 121–125 and 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES; ENGINE-OPTICAL, including the display volume; ENGINE-RADIATION as amended; RAD-08 (the pattern); RAD-03 or RAD-04; S7 for the binding. RAD-14 only for row K8. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story).
- **Files.** `elements/TrackChamber/TrackChamber.csproj`, `TrackChamberElementDeclaration.cs`, `track_chamber.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/TrackChamberTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories, including U1 and U2; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** A repeated `Supply` role, or a contact output fanned out to several contacts, refused by the bundle, wire or connection code; charged sources delivered as cones (the segments would each take a share); no display volume for the renderer (the row's "readable trails" is then unmet); ENGINE-RADIATION skipping a receiver's own colliders before its aperture face, a second own-body rule that radiation convention 4 does not admit (K1, K7's control and K8 then read 1.0 W, 1.0 W and 0.80 W; re-derive section 5). In each case the slice stops and the owning story is amended; no selector is added in element code and no second design is kept here.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story; U1 and U2 are settled by the slice's probe).

1. **Declaration data only.** One static body, ten colliders, four ports, three receivers, four signal nodes (three threshold, one boolean), four contacts, nine bindings and one trail display. No code names the chamber.
2. **Generic laws.** Chord attenuation; a collimated segment crossing an aperture; a threshold node over a committed strength; a boolean node; supply through signal-state contacts. "Reads the track" is three receivers on one face.
3. **Three layers.** Colliders: ten primitives on one static body. Visuals: the glass box, the back plate, printed dividers, flush lamps. Animation: lamp bindings and renderer-drawn trails, both from committed state, writing nothing back.
4. **One-way flow and typing.** Field solve → strengths → threshold nodes → next tick's supply; trails are read-only. Channels and roles are enums; powers are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** Static body; ten primitives, with the reason stated in the counts; the chamber adds no cast of its own (a beam is one segment cast whether or not a chamber is present); no sensor volume in physics.
7. **Tests are scenarios.** Rows K1 to K10 run the production simulation through placement, wiring, Run and committed reads.
