# EL-129 · Sheet-thickness transmission gauge — element readiness spec

**Current delivery (24 October 2026):** [EL-129 delivery slice](../invest/named-elements.md#element-129). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`, findings H-1, C-1, C-3, M-11, X-1 to X-4) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [element-129](../requirements.md#element-129). A toy inspection instrument for fictional toy sources; never real-world gauging guidance.

**This spec is the lead of the Family slice "Transmission gauges"** (EL-129 and [EL-130](EL-130-tank-level-transmission-gauge.md)). Both have the [rate meter's](RAD-08-radiation-rate-meter.md) body and aperture, with two threshold levels over one strength in place of one. This spec states what the two share and EL-129's own values and rows. Units, the reading law, receivers and coupling are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions) (cited by item number).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-129 · Sheet-thickness transmission gauge |
| Type | Radiation |
| Anchor | [requirements.md#element-129](../requirements.md#element-129); [named-elements.md#element-129](../invest/named-elements.md#element-129); scope source [radiation-11](../requirements.md#radiation-11) (umbrella, index only) |
| Owner | S618 |
| Disposition | Member and lead of the Family slice **Transmission gauges** with EL-130 (proposed in the [batch summary](readiness/batch-o.md); `delivery-batches.md` does not list it yet). The two differ only in declared values: the parameter's id and default, the dial print, id, title and description. It is not a duplicate of RAD-08: it has two threshold nodes and a boolean node over one strength and no pulse output (D8). |
| Planned package | id `sheet_thickness_gauge`, directory `elements/SheetThicknessGauge/`, class `SheetThicknessGaugeElementDeclaration` |
| Related identities | Sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); samples [EL-128](EL-128-dense-shield.md), [EL-127](EL-127-polymer-shield.md); routing by the Conveyor and a [powered diverter EL-163](../invest/named-elements.md#element-163). No CAT part is refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 5 s |

## 2. Declaration

Shape followed: [RAD-08](RAD-08-radiation-rate-meter.md#2-declaration) for the body, aperture and supplied contact (`SignalStateContactControl`, declared at `elements/StateLatch/StateLatchElementDeclaration.cs:67`). Items marked **(field)** use contracts that do not exist yet.

The idea: the gauge reads how much of a known source gets through. Too little to trust means no source: invalid. Plenty means nothing thick is in the way. In between means a thick sample. `Supply` is live only in between.

| Item | Value (family unless marked EL-129) |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the receiver interface **(field)** |
| Body and colliders | RAD-08's static body: case box 0.30 × 0.60 × 0.50 m, base box 0.40 × 0.06 × 0.60 m at (0, −0.33, 0), and two socket spheres r 0.075 m. Four colliders; none declares a radiation row, so all take the scene's default row (radiation convention 4). Own body by the one rule of optical convention 5 and radiation convention 4 (coordinator decision 26): each shadow cast ends at the receiver's aperture face, so the case behind the face attenuates nothing the receiver reads from the front. |
| Ports | `PortId.PowerIn`, Electrical, Input, at (−0.15, 0, −0.15), normal −X. `PortId.Supply`, Electrical, Output, at (−0.15, 0, 0.15), normal −X. No signal port. |
| Receiver **(field)** | One receiver: aperture square 0.30 × 0.30 m on +X, centre (0.15, 0, 0), normal +X, the standard area 0.09 m², nine samples, Lambert facing (convention 7); WeightedSum strength with PhotonSoft, PhotonMedium, PhotonHard = 1 and every other weight 0. It commits one strength in watts. |
| Signal graph **(field, ENGINE-SIGNAL-NODES)** | Two threshold nodes over the receiver's committed strength (convention 8): `valid`, on at ≥ 0.05 W (fixed), off below 0.75 × 0.05 = 0.0375 W; `clear`, on at ≥ the parameter, off below 0.97 × the parameter. One two-input boolean signal node `window` = `valid` Xor `clear`, an operation of the closed set And, Nand, Nor, Or, Xor (`LogicGateOperation`, `CuriousContraptions.Abstractions/ElectricalContractValues.cs:25`). It equals "`valid` and not `clear`" because `clear` implies `valid`: `clear` turns on only at a strength of at least the parameter, whose minimum 0.06 W is above `valid`'s 0.05 W mark, so `valid` is on by the same pass; and while `clear` holds the strength is at least 0.97 × 0.06 = 0.0582 W, above `valid`'s 0.0375 W release. The equivalence needs the parameter's minimum to stay at or above 0.05 W. `window` is a node of the signal network, not a supply logic node: it needs no electrical input, so the delivered rule that a supply logic node conducts only with its own `PowerIn` powered (`engine/gpu/SupplyRoute.cs:375`–`:376`) does not apply to it. No signal port. |
| Supply graph | One `SupplyContactDescriptor` `relay` with `SignalStateContactControl` naming `window`; `PowerIn` → (`relay`, `SupplyTerminal.PowerIn`); (`relay`, `SupplyTerminal.Output`) → `Supply`. One port binds one endpoint, as the validator requires (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:142`–`:143` refuses a port bound twice). No consumer, no logic node. |
| Electrical nodes | `relay_switch`, the contact's electrical node (its own id, as the Impact switch pairs `contact` with `switched_contact`): `ElectricalRole.Switch`, 12 V, resistance 0.05 Ω, capacitance 0, maximum 100 W, as `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:76`. The bundle refuses an electrical declaration with no node (`CuriousContraptions.Bundle/BundleValidator.cs:867`). |
| Parameters | EL-129: `thin_threshold`: Real, Watt, default 0.2, minimum 0.06, maximum 2.0, editor policy Clamp, build only (D11). The minimum stays above the 0.05 W validity mark so the window is never empty. |
| Stores | None |
| Presentation bindings | `Visual/Needle` rotation from the committed strength on a clamped scale 0 to 1 W **(field)**; `Visual/ValidLamp` slate (85, 101, 115) → gold (247, 203, 82) from the committed `valid` level **(field)**; `Visual/ResultLamp` slate → gold from the committed `window` level **(field)**. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Electrical, Signal and the radiation flag **(field)** |
| Catalogue | EL-129: id `sheet_thickness_gauge`, "Thickness gauge", `ElementCategory.Radiation` **(field)**, palette order 711 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Reads how much of a source's radiation passes a sheet. Its contact passes electricity for a thick sheet, and never when it sees no source." |

### Art and solidity

RAD-08's table without the trigger stud and the channel mark, and with: `Visual/Dial` printed flat with three printed zones (a cross for invalid, a thick bar, a thin bar) and a printed mark at the threshold; `Visual/ValidLamp` and `Visual/ResultLamp`, discs r 0.03 m flush under the dial, recoloured by bindings (decorative: flush). The case and base boxes and the two gold socket spheres are solid with their colliders. EL-129's dial carries a printed sheet-and-arrow mark; EL-130's a wave line. Both differ from the rate meter at a glance by the three-zone dial and two lamps and by having no top stud. Icon: the case with a sheet edge in front of its face.

### Compiled counts

Bodies 1 (static). Colliders 4 (2 boxes, 2 spheres). Signal nodes 3 (two threshold, one boolean). Supply nodes 1 (one contact). Electrical nodes 1. Receivers 1 (nine samples). Stores 0. Presentation bindings 7 (needle; two lamps × R, G, B). Joints, drives, sensors 0.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, SignalPropagation. This probe, made on `8d1d68aa`, is the family's and the authority until the slice re-runs it. **5 present, 0 untested, 2 narrower, 4 missing.** Rows R1 to R3, R5, R6, N1 and N3 are those of [RAD-08 section 3](RAD-08-radiation-rate-meter.md#3-engine-capabilities) with the same symbols and opened tests.
| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | as RAD-08 R1 | as RAD-08 R1 |
| R2 | Every visible solid has a collider | present | as RAD-08 R2 | as RAD-08 R2 |
| R3 | Typed ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R5 | Supply contact closed by a signal state, between a source and a consumer | present | `SignalStateContactControl` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`) | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:530`, fixture `:453`–`:466`) |
| R6 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids | narrower, as RAD-08 N1 | `AirflowLineClear` | owner of the segment cast **ENGINE-SPATIAL-QUERIES** |
| N3 | Presentation driver on a committed strength | narrower, as RAD-08 N3: no strength member in `PresentationDriver` (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | `PresentationDriver` | owner **ENGINE-OPTICAL** |
| M1 | Declared receivers with weighted samples and a committed reading and strength | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels, attenuation rows and chords | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | Threshold nodes with a release ratio over a committed scalar; a boolean signal node over signal levels; the contact state each presents to `SignalStateContactControl`; a presentation driver on their committed levels | **missing**: `SignalNodeKind` (`CuriousContraptions.Abstractions/SignalContractValues.cs:13`) has no threshold or boolean kind, and a contact names only `SignalContactState.Latched` or `TimerCounting` (`SignalContactControl.cs:5`) | — | owner **ENGINE-SIGNAL-NODES** |
| M4 | `ElementCategory.Radiation` and the radiation domain flag | **missing** | — | owner **ENGINE-RADIATION** |

Why one contact and a signal-side boolean node: two contacts fed from one `PowerIn` with a supply Xor cannot compile, because the validator refuses a port bound to two endpoints (`SupplyBundleValidation.cs:142`–`:143`, "Port.Reference"), and a supply logic node conducts only from its own `PowerIn` terminal (`SupplyRoute.cs:375`–`:376`), which that design left unwired. With one contact and no supply logic, no directed supply cycle can arise inside the package, so no loop-refusal row is needed.

**Dependencies.** Battery (delivered); RAD-08 (shape); RAD-01 (source); EL-128 (samples).

## 4. Sources and legacy

- [element-129](../requirements.md#element-129) and the [radiation-11](../requirements.md#radiation-11) integration task: "Known thin/thick samples sort correctly … Missing source is invalid, not a valid heavy/full reading."
- [Named entry](../invest/named-elements.md#element-129), owner S618; map row (S605, S257); binding `docs/coverage/engine/element-02.json` (proof S618, relation radiation-11).
- [S605](../invest/decisions.md#s605) photon row; research "Inspection" law (valid source and reference path, calibrated on a known sample; dead source, misalignment or unsupported material sets Invalid) and slot 88 "Weighing Without Touch".
- **Legacy.** None. Hits are coverage snapshots only: older radiation-11 binding copy listing this identity `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1964-L2008`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

Starting values (D1). Reference arrangement: a Gamma capsule (10 W/sr, PhotonMedium) 1 m from the face on its axis, the sample between them. Strength R is the photon reading; with nothing between them it is 0.882 W (convention 14).

- **Three zones.** R below the `valid` mark: `valid` off, `window` off: invalid, `Supply` dead. `valid` on, `clear` off: thick, `window` on, `Supply` live. Both on: thin or nothing, `window` off, `Supply` dead.
- **Dense samples at the default 0.2 W** (Dense slab 0.05 m, PhotonMedium 20 /m). No sample: 0.882 W: dead. One slab (thin): 0.882 × e^−1 = 0.324 W ≥ 0.2: dead. Two slabs (thick): 0.882 × e^−2 = 0.119 W, below the release 0.194 W and above 0.05 W: live. Margins: 0.324 is 62 % above the mark; 0.119 is 39 % below the release and 2.4 times the `valid` mark.
- **Swapping samples in one Run.** `clear` releases below 0.97 × 0.2 = 0.194 W, so thin (0.324) to thick (0.119) switches, and back switches again at 0.2. The 3 % band is narrow because the thin and thick readings of a weak material are close; the meter's 0.75 would be wrong here. `valid` stays on through both (0.119 W is above its 0.0375 W release).
- **No source.** R = 0: `valid` off, dead. A missing source can never read as thick, because `window` needs `valid`.
- **Too thick to judge.** From Run start, the reading must reach 0.05 W for `valid` to turn on: the gauge is calibrated up to L = ln(0.882 ÷ 0.05) ÷ 20 = 0.143 m of Dense material. Four slabs: 0.882 × e^−4 = 0.0162 W, 68 % under the mark: invalid, dead. Once `valid` is on, it holds down to 0.0375 W, so a third slab added to a live thick reading (0.882 × e^−3 = 0.0439 W) keeps it thick; the level hysteresis is deliberate and stated here.
- **Source too far.** Capsule at 5 m: 0.09 × 10 ÷ 25 = 0.036 W, 28 % under the mark: invalid.
- **An ordinary part is not a sample.** A Wall across the path (0.25 m, default row 0.15 /m) leaves 0.882 × e^−0.0375 = 0.849 W: thin, dead (convention 4).
- **Recalibrating for polymer** (Polymer slab 0.05 m, PhotonMedium 3 /m). One slab: 0.882 × e^−0.15 = 0.882 × 0.861 = 0.759 W. Two: 0.882 × e^−0.30 = 0.882 × 0.741 = 0.653 W. With `thin_threshold` 0.715 W (release 0.97 × 0.715 = 0.694 W): one slab is 6.2 % above the mark (thin, dead) and two are 5.9 % below the release (thick, live). Both clear the ±3 % reading tolerance; lessons still use dense samples, whose margins are wider.
- **Tick order** (convention 11). The field solve of tick n runs after the last substep, on this tick's final poses, and commits the strength with the tick. The signal network of tick n + 1 evaluates `valid`, `clear` and `window` together and closes or opens `relay`, so `window` never sees one threshold updated and the other stale. `Supply` follows the sample one tick late.
- **Energy.** `Supply` passes only what `PowerIn` receives. The gauge's case lies behind its own receiver's aperture face, so it does not attenuate that receiver's casts (they end at the face); for any other receiver it attenuates by the default row like any part.

### Rows

Planned class `TransmissionGaugeTests`, one class for the family; rows marked "family" are theories over both members, each with its own attenuator. Fixture: Battery `Supply` → gauge `PowerIn`; gauge `Supply` → Powered gate `PowerIn`; Gamma capsule 1 m from the face. Readings ±3 % (radiation convention 14; the nearest decision margin of a default row is 28 %). Stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| G1 | Given two dense slabs on the path, the strength is 0.119 W and `Supply` is live; given one slab, 0.324 W and `Supply` is dead (task 419: known thin and thick samples sort) | `TransmissionGaugeTests.ThinAndThickSheetsSort` | G2 | ±3 % |
| G2 | Family: given no source, `Supply` is dead for 240 ticks with any sample or none (row: missing source is invalid, not maximum thickness) | `TransmissionGaugeTests.AMissingSourceIsNeverThickOrFull` | G1 | `valid` level off |
| G3 | Given four dense slabs from Run start (0.0162 W), or the capsule at 5 m (0.036 W), `Supply` is dead and the `valid` level is off for 240 ticks | `TransmissionGaugeTests.BelowTheValidityMarkIsInvalid` (theory) | G1 | below 0.05 W |
| G4 | Given a law-scene attenuator whose transmission steps so the strength goes 0.324 → 0.119 → 0.324 W in one Run, `Supply` goes dead → live → dead, one transition each | `TransmissionGaugeTests.SwappingSamplesSwitchesBothWays` | G1 | exact transition count |
| G5 | Family: given no wire to `PowerIn` and a thick reading, `window` is on and `Supply` is dead | `TransmissionGaugeTests.AnUnsuppliedGaugeSwitchesNothing` | G1 | 240 ticks |
| G6 | Given `thin_threshold` 0.715 W, one polymer slab is thin (0.759 W, dead) and two are thick (0.653 W, live) | `TransmissionGaugeTests.TheThresholdCalibratesForTheSampleMaterial` | G1 | ±3 % |
| G7 | Family: the parameter's minimum 0.06 and maximum 2.0 are admitted; a finite value outside them is clamped to the nearer bound (editor policy Clamp); a non-finite value is refused with nothing changed | `TransmissionGaugeTests.TheThresholdIsBounded` | — | clamped value exact; typed refusal |
| G8 | Family: a signal output wired to `PowerIn`, or `Supply` wired to a signal input, is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (R3) | — | typed refusal |
| G9 | Family: after a Run with a live `Supply`, Reset leaves no strength or level; a saved and reloaded construction behaves as before with its threshold | `TransmissionGaugeTests.ResetAndReloadRestoreTheAuthoredState`; authored state by R6 | G1 | strength 0 |
| G10 | Given a Wall across the path and no sample, the strength is 0.849 W and `Supply` is dead | `TransmissionGaugeTests.AnOrdinaryPartIsNotASample` | G1 | ±3 % |

Chrome recipe (pending, post-catalogue): capsule, gauge, Battery, a Conveyor carrying one-slab and two-slab samples, and a powered diverter on `Supply`.

## 6. Open questions

None open.

1. **Automatic pairing or an explicit calibration link — decided now**: neither. The gauge reads whatever reaches its face; "calibration" is the threshold, set for the source distance and the sample material. No pairing search, no reference reading stored at Run start, no logarithm in the engine.
2. **One gauge with a mode, or two parts — decided** (D8): two packages in one Family slice.
3. **Acceptance cone, minimum valid reading and threshold — decided** (D1, D16): facing is the receiver's Lambert rule; validity mark 0.05 W; default 0.2 W.
4. **A thickness estimate in metres — decided now**: not produced. The dial shows the reading against printed zones; sorting is the gameplay, and the row asks for an estimate of "supported sheet thickness", which the thin and thick zones give.
5. **Coupling — decided** (coordinator decision 16; audit H-1): one strength, two threshold nodes, one boolean signal node, one signal-state contact. No receiver sets a memory node; no supply logic node.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Powered gate, Conveyor (delivered); RAD-01, RAD-02, EL-127, EL-128, EL-163 (undelivered). Port domains agree: `PowerIn` Electrical Input, `Supply` Electrical Output.
- **Cross-element task.** [sequence-task-419](../requirements.md#sequence-task-419): rows G1 and G2 here, L1 to L3 in [EL-130](EL-130-tank-level-transmission-gauge.md). Processes [IX-15](../requirements.md#interaction-15), [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07).
- **Campaign.** Chapter 12, first use 111–120; reuse 121–125, 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader (`tools/Coverage/NamedPackageSources.cs` on `origin/main`) admits an `EL-NNN` identity only when its ledger entry carries exactly one `todo-N` link (`:96`) and its decision has an anchor; the entry for element-129 has no such link and S618 has no anchor in `docs/planning/invest/scope-corrections.md` (checked by search).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Family slice of two packages, one review, one candidate commit, when EL-130 can run its own rows in the slice; otherwise EL-129 is delivered alone as a single element and EL-130 follows with the same shape (see [EL-130 section 8](EL-130-tank-level-transmission-gauge.md#8-delivery)).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01, RAD-08, EL-128; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names none).
- **Files.** Per member `elements/<Dir>/<Dir>.csproj`, `<Dir>ElementDeclaration.cs`, `<id>.tscn`, `icon.svg` (here `SheetThicknessGauge`, `sheet_thickness_gauge`); one guarded line each in `CuriousContraptions.slnx`; `CuriousContraptions.tests/TransmissionGaugeTests.cs`; a row each in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** A threshold or boolean node that `SignalStateContactControl` cannot name (then the contact needs a new control kind, an engine slice); a fixed release ratio (then the polymer calibration is dropped and the dense one stands); threshold and boolean nodes not resolved in one signal-network pass (then `window` could see one level stale); no strength driver for the needle.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story). These points are the family's.

1. **Declaration data only.** A static body, four colliders, two ports, one receiver, two threshold nodes, one boolean node, one contact, one electrical node, one parameter and seven bindings. No code path names a gauge.
2. **Generic laws.** The field law of the conventions; thresholds with release ratios over a committed scalar; a boolean signal node; supply through a signal-state contact. "Invalid", "thin" and "thick" are three combinations of two generic levels.
3. **Three layers.** Colliders: four primitives on one static body. Visuals: flush and printed art only. Animation: needle and lamps read committed state and write nothing back.
4. **One-way flow and typing.** Field solve → strength → signal nodes → next tick's supply; channels, ports and roles are enums; powers are f32.
5. **Flat state.** Counts in section 2, fixed at compile time.
6. **Performance fitness.** Static body; four primitives; nine shadow casts per diverging source in range, each through the spatial query's segment cast; no sensor volume.
7. **Tests are scenarios.** Rows G1 to G10 run the production simulation through placement, wiring, Run and committed reads.
