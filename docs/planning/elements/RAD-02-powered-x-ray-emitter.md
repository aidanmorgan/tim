# RAD-02 · Powered X-ray emitter — element readiness spec

**Current delivery (24 October 2026):** [RAD-02 delivery slice](../invest/named-elements.md#radiation-02). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-02](../requirements.md#radiation-02) (P1 potential). Lengths are metres; local +X is the emission axis. A toy emitter; not real-world X-ray equipment or shielding guidance. Units, the propagation law, the rows and the tick order are the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-02 · Powered X-ray emitter |
| Type | Radiation (supplied photon source) |
| Anchor | [requirements.md#radiation-02](../requirements.md#radiation-02); [named-elements.md#radiation-02](../invest/named-elements.md#radiation-02) |
| Owner | S609 |
| Disposition | To deliver as a single element. It is the controllable source: supplied, enabled, directional and with a band setting, none of which the [Gamma capsule](RAD-01-gamma-source-capsule.md) has, so it is not a duplicate. |
| Planned package | id `xray_emitter`, directory `elements/XrayEmitter/`, class `XrayEmitterElementDeclaration` |
| Related identities | Steady alternative [RAD-01](RAD-01-gamma-source-capsule.md); receivers [RAD-08](RAD-08-radiation-rate-meter.md), [RAD-09](RAD-09-integrating-dosimeter.md) (burst source); [RAD-07 Collimator](RAD-07-collimator-block.md); [EL-128 Dense shield](EL-128-dense-shield.md). Supply from the Battery; set and reset pulses from an Impact switch, a Delay box or a Clock. No CAT spec refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 2 s plus the stall guard |

## 2. Declaration

Shape followed: the State latch for a memory node with set and reset inputs (`CuriousContraptions.tests/StateLatchTests.cs:237`) and the planned amber laser for a supplied, enabled emitter ([EL-176](EL-176-red-laser-emitter.md#2-declaration)). The memory node is a legitimate memory node: its inputs are the Set and Reset pulses it is built for, and its committed level is the signal level the emitter's enable names (optical convention 2, radiation convention 8: "a relay or emitter enable names a signal level"). No receiver sets it. Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the emitter interface **(field)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, `elements/Battery/BatteryElementDeclaration.cs:61`–`:64`) |
| Colliders (hidden `col_` proxies) | Housing box 0.70 × 0.45 × 0.45 m at the origin. Base box 0.80 × 0.06 × 0.55 m at (0, −0.255, 0), touching the housing. Window collar cylinder, axis X, radius 0.12 m, length 0.06 m at (0.38, 0, 0), touching the +X face. Three socket spheres radius 0.075 m at the ports. Six colliders; none declares a radiation row, so all take the scene's default row. Each socket sphere's centre lies on a housing face, so half of it is inside the housing; those overlapped chords count once (radiation convention 4). |
| Ports | `PortId.PowerIn`, Electrical, Input, role `PowerIn`, at (−0.35, 0, 0), normal −X. `PortId.SetIn`, Signal, Input, role `SetIn`, `PortSignalKind.Pulse`, at (−0.15, 0.225, 0), normal +Y. `PortId.ResetIn`, Signal, Input, role `ResetIn`, `PortSignalKind.Pulse`, at (0.15, 0.225, 0), normal +Y. |
| Signal graph | One `SignalMemoryDescriptor` `enable`; `SetIn` → (`enable`, `SignalTerminal.Set`); `ResetIn` → (`enable`, `SignalTerminal.Reset`). Off at Run start. |
| Supply graph | One `SupplyConsumerDescriptor` `tube` bound `PowerIn` → (`tube`, `SupplyTerminal.PowerIn`); its target is the source **(field, ENGINE-OPTICAL)**. `ElectricalNodes`: one Consumer row, 12 V, maximum power 12 W. |
| Source **(field)** | Frame body 0; origin (0.41, 0, 0), the collar's front face; axis +X; half-angle 15° (Ω = 2π(1 − cos 15°) = 2π × 0.03407 = 0.2141 sr), uniform inside and zero outside; range 16 m; radiant power 2.141 W (10 W/sr); electrical demand 12 W; enable = the level of `enable`; internal occluders = housing and collar. Three rows, one compiled, each bound to an option of `band` **(field, ENGINE-PARAMETER-BOUND-FRAMES)**: the power is in PhotonSoft, PhotonMedium or PhotonHard. The granted energy not emitted is the declared loss, ledgered to the housing collider. |
| Parameters | `band`: Choice, unit None, options Soft, Medium, Hard, default Medium, build only; player-editable unless the level locks it (D11). Direction is the part's orientation. |
| Stores | None. |
| Presentation bindings | `Visual/Window` albedo slate (85, 101, 115) → gold (247, 203, 82), `PresentationCurve.ExponentialFollow` at 12 /s, driver = committed emission of the source **(field, ENGINE-OPTICAL)**. `Visual/BandMark` shows the chevrons of the chosen `band` **(field, ENGINE-PARAMETER-BOUND-FRAMES)**. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Electrical, Signal and the radiation flag **(field)** |
| Catalogue | Id `xray_emitter`, "X-ray emitter", `ElementCategory.Radiation` **(field)**, palette order 708 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Shines a cone of X-rays while it has electricity and has been set. A reset pulse stops it. Choose how hard the rays are." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Housing` | box 0.70 × 0.45 × 0.45 m, cream `#fff8e9` | solid (housing box) | nothing |
| `Visual/Base` | box 0.80 × 0.06 × 0.55 m at y −0.255, navy `#293954` | solid (base box) | nothing |
| `Visual/Collar` | cylinder r 0.12 m, length 0.06 m at x 0.38, navy | solid (collar cylinder) | nothing |
| `Visual/Window` | disc r 0.08 m flush in the collar's front face | decorative: flush, recoloured by a binding | nothing (colour only) |
| `Visual/BandMark` | one, two or three chevrons printed flat on the side, chosen by `band` | decorative: printed flat | nothing |
| `Visual/PowerStud`, `Visual/SetStud`, `Visual/ResetStud` | spheres r 0.075 m, gold `#f7cb52` | solid (socket spheres) | nothing |

Icon: a long box with a round window and a cone. It differs from the Gamma capsule by its box-and-collar form and its sockets, and from the planned lasers by the cream housing and chevron band marks.

### Compiled counts

Bodies 1 (static). Colliders 6 (2 boxes, 1 cylinder, 3 spheres); one more than the planned laser's five, for the second signal socket. Joints 0. Drives 0. Sensors 0. Signal nodes 1 (memory). Supply nodes 1. Sources 1 (one of three rows). Stores 0. Presentation bindings 3 (window R, G, B) and one band-mark selector.

## 3. Engine capabilities

Map row families (navigation, D14): ElectricalPower, FiniteLedger, GeometryQuery, IonizingTransport. This probe, made on `8d1d68aa`, is the authority until the slice re-runs it: **6 present, 0 untested, 4 narrower, 4 missing**. ENGINE-SPATIAL-QUERIES, ENGINE-SUPPLY-DEMAND and ENGINE-PARAMETER-BOUND-FRAMES are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row of the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R3 | Typed ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R4 | Memory node set by one pulse, cleared by another, off when both arrive together | present | `SignalMemoryDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:64`) | `StateLatchTests.Network_SetHoldsResetClearsAndSimultaneousRequestsResolveOff` (`CuriousContraptions.tests/StateLatchTests.cs:237`) |
| R5 | Proportional sharing of one per-phase power allowance | present | `ElectricalSupply.Reserve` (`engine/gpu/ElectricalSupply.cs:11`) | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` (`CuriousContraptions.tests/ElectricalSupplyTests.cs:207`) |
| R6 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) is for airflow rows only and loops over every collider (`:149`) | — | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| N2 | Values bound to the options of a Choice: the compiled source row and the shown band mark | narrower: `ChoicePredicate` (`CuriousContraptions.Abstractions/AirflowValues.cs:23`) selects airflow input rows only | — | `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:507`). Owner: **ENGINE-PARAMETER-BOUND-FRAMES** (choice-bound values, coordinator decision 16) |
| N3 | Supply consumer paying a continuous target | narrower: targets are a store, a drive, a transmission or continuous airflow work only (`CuriousContraptions.Abstractions/SupplyContractValues.cs:44`–`:54`) | `SupplyConsumerDescriptor` | the same Fan test (`FanIntegrationTests.cs:507`). Owner of the emitter target: **ENGINE-OPTICAL** |
| N4 | Presentation binding on a committed driver | narrower: no emission member (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner: **ENGINE-OPTICAL** |
| M1 | A declared emitter with a uniform cone, an enable naming a signal level and grant scaling, its loss ledgered to a declared collider, and the shared segment and receiver solve | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels and per-collider attenuation rows | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | `ElementCategory.Radiation` and the radiation `SimulationDomain` flag | **missing** (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) | — | owner **ENGINE-RADIATION** |
| M4 | The selected-part Build preview (here a cone) | **missing** | — | owner **ENGINE-OPTICAL** (optical convention 12; radiation convention 13) |

Nothing radiation-specific exists in `CuriousContraptions.Abstractions*`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle`, `engine/` or `elements/`. ENGINE-RADIATION ([Story ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure), sprint key `engine-radiation`) is the one owner of the radiation-specific rows.

**Dependencies.** Battery; a pulse source (Impact switch, Delay box, Clock), all delivered. RAD-01 first, for the proven source shape. Rows read a law-scene receiver until RAD-08 exists.

## 4. Sources and legacy

- [radiation-02](../requirements.md#radiation-02): "Supply removal stops emission; a disconnected enable input cannot fire. Test every supported energy preset, direction and restart."
- [Named entry](../invest/named-elements.md#radiation-02), owner S609; map row (S605, S257); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row (tube glow ← committed emission; slots 82 "Only While Powered", 89).
- **Legacy.** No X-ray part, law, test, level or lesson. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1659-L1689`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Emission law.** While the level of `enable` is on and the supply grants energy, the source emits 2.141 W × (energy granted over the tick's substeps ÷ energy demanded) in the chosen band (radiation convention 2). 2.141 W over 0.2141 sr is 10 W/sr, the Gamma capsule's intensity, so the band and not the strength is the variable.
- **Cone.** At 1 m the cone's radius is tan 15° = 0.268 m. The standard detector's farthest sample is √(0.10² + 0.10²) = 0.141 m off axis, so the whole face is inside and it reads the capsule's 0.882 W. Centred 0.10 m off axis its farthest sample is √(0.20² + 0.10²) = 0.224 m off axis (12.6°), still inside; its nine sample terms 10 ÷ d³ (d² = 1 + y² + z², y in {−0.1, 0, 0.1}, z in {0, 0.1, 0.2}) are 10, 9.852 twice; 9.852, 9.707 twice; 9.429, 9.294 twice; their mean 86.99 ÷ 9 = 9.665 W/m² gives 0.09 × 9.665 = 0.870 W. A detector whose centre is 0.5 m off axis at 1 m has its nearest sample 0.4 m off axis, at atan 0.4 = 21.8°, and reads 0. Distances are measured from the collar's front face.
- **Supply.** Demand 12 W is 12 W × dt per tick, 0.1 J at the default dt of 1/120 s. The delivered Battery grants up to `power_limit` × dt per tick (coordinator decision 30), 1 J at its 120 W default and the default dt (`elements/Battery/BatteryElementDeclaration.cs:44`), so one Battery runs ten emitters, and its 3,600 J last 300 s for one. Radiant 2.141 W ≤ demand 12 W: the other 12 − 2.141 = 9.86 W is the declared loss, ledgered to the housing collider, and never becomes radiation; it is heat only once ENGINE-THERMAL consumes it (radiation convention 10). At the Battery's 10 W minimum the energy granted over the tick's substeps ÷ energy demanded is 10 ÷ 12, the source emits 2.141 × 10 ÷ 12 = 1.784 W and the detector reads 0.882 × 10 ÷ 12 = 0.735 W.
- **Bands and one Dense slab (0.05 m) at 1 m.** Soft, 64 /m: 0.882 × e^−3.2 = 0.882 × 0.0408 = 0.036 W. Medium, 20 /m: 0.882 × e^−1 = 0.882 × 0.368 = 0.324 W. Hard, 10 /m: 0.882 × e^−0.5 = 0.882 × 0.607 = 0.535 W. The Hard X-ray beam passes more than the Medium Gamma capsule's 0.324 W, and the Soft one less: penetration follows the band, with no ranking of source types.
- **Ordinary parts are not shields.** A Wall (0.25 m) across the beam takes the default row: Soft 0.882 × e^(−0.4 × 0.25) = 0.882 × 0.905 = 0.798 W, Medium 0.882 × e^(−0.15 × 0.25) = 0.882 × 0.963 = 0.849 W, Hard 0.882 × e^(−0.075 × 0.25) = 0.882 × 0.981 = 0.866 W.
- **Bursts.** A set pulse then a reset pulse 120 ticks later gives 120 emitting ticks, a 1 s burst: 0.882 W × 1 s = 0.882 J at a detector 1 m away; this is the dosimeter's burst source. There is no warm-up: the source emits in the tick whose signal network sets `enable`.
- **Tick order.** The signal network of tick n resolves `enable`; energy is granted inside tick n's substeps; the field solve after the last substep of tick n emits on that grant, on this tick's final poses and on `enable` as resolved at the start of tick n; the reading commits with tick n and acts from tick n + 1 (radiation convention 11). A tick in which supply is open grants nothing, so that tick has no emission; `enable` is untouched.

### Rows

Planned class `XrayEmitterTests`. Fixture: Battery `Supply` → `PowerIn`; a Delay box `ActivationOut` → `SetIn`; a second pulse source → `ResetIn`; a law-scene receiver of the standard aperture weighted for the three photon channels, 1 m along +X. Readings ±3 %; stall guard 2 s after the set pulse.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given the fixture, when the set pulse arrives, then the receiver reads 0.882 W and the reading is in the chosen band's channel only | `XrayEmitterTests.ASuppliedSetEmitterEmitsInItsBand` (theory, three bands) | A2 | ±3 %; other channels below 0.001 W |
| A2 | Given a lit emitter, when the Battery is disabled by its run control, then the reading is 0 from that tick; enabled again it returns without a second set pulse (row: supply removal stops emission) | `XrayEmitterTests.SupplyRemovalStopsEmissionAndRestoredSupplyResumes` | A1 | `enable` reads on throughout |
| A3 | Given supply and nothing wired to `SetIn`, then no emission for 240 ticks (row: a disconnected enable input cannot fire) | `XrayEmitterTests.AnUnconnectedSetInputNeverFires` | A1 | reading 0 |
| A4 | Given set, reset after 1 s, and set again, then emission stops at the reset and restarts at the second set; set and reset in one tick leave it off (row: restart) | `XrayEmitterTests.ResetStopsAndASecondSetRestarts` | A1 | emission intervals counted: 2 |
| A5 | Given the part turned to face +X, −X, +Z and −Z, then only the receiver 1 m along the facing reads 0.882 W (row: every direction) | `XrayEmitterTests.TheConeFollowsThePartsFacing` (theory, four facings) | the other three read 0 | ±3 % |
| A6 | Given a receiver centred 0.5 m off axis at 1 m it reads 0; centred 0.10 m off axis it reads 0.870 W | `XrayEmitterTests.NothingLeavesTheCone` | A1 | below 0.001 W; ±3 % |
| A7 | Given one law-scene Dense box on the path, then Soft reads 0.036 W, Medium 0.324 W and Hard 0.535 W | `XrayEmitterTests.PenetrationFollowsTheBand` | no box reads 0.882 W in every band | ±3 % |
| A8 | Given the Battery's `power_limit` at 10 W, then the reading is 0.735 W; over 120 lit ticks at full supply the Battery is debited 12 J, the emitted energy is 2.141 J, never more than the debit, and 9.86 J is ledgered to the housing collider | `XrayEmitterTests.EmissionNeverExceedsGrantedEnergy` | A1 | ±3 % |
| A9 | Given an electrical output wired to `SetIn`, or a signal output wired to `PowerIn`, then the edit is refused; an undefined `band` option is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (R3); `XrayEmitterTests.OnlyTheThreeBandsAreAdmitted` | — | typed refusal |
| A10 | Given a lit Run, when Reset, then no emission or reading remains, `enable` is off and the window binding is at rest; a saved and reloaded construction keeps its `band` and behaves as A1 | `XrayEmitterTests.ResetAndReloadRestoreTheAuthoredDarkState`; authored state by R6 | A1 | reading 0 |
| A11 | Given a lit emitter, then the window binding moves toward gold and returns after supply loss | `XrayEmitterTests.TheWindowFollowsCommittedEmission` | unlit | endpoint colours within 1/255 |

Chrome recipe (pending, post-catalogue): Battery, Impact switch with a falling ball on `SetIn`, emitter aimed at a Rate meter 1 m away with a load; wire, Run, then pull the Battery wire.

## 6. Open questions

None open.

1. **Enable domain — decided** (D16, D19): signal pulses on `SetIn` and `ResetIn`, the delivered memory-node shape. A level-following electrical enable would need a new input kind; set and reset give bursts with what exists, and a Clock or Delay box drives them directly.
2. **Cone, strength and draw — decided** (D1): 15°, 2.141 W, 12 W. The Battery is a finite store today, so the draw is real.
3. **Warm-up — decided** (D19): none; no requirement row or lesson uses one.
4. **Coupling — decided** (coordinator decision 16): the emitter's enable names the committed level of the memory node `enable`, which only the Set and Reset pulses change.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Impact switch, Delay box, Clock (delivered). RAD-08, RAD-09, RAD-07, EL-128, EL-129, EL-130 (undelivered). Port domains agree: `PowerIn` Electrical Input against the Battery's `Supply`; `SetIn` and `ResetIn` Signal Pulse inputs against a pulse `ActivationOut`.
- **Cross-element tasks.** Gauge source in [sequence-task-419](../requirements.md#sequence-task-419); processes [IX-15](../requirements.md#interaction-15), [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07).
- **Campaign.** Chapter 11, first use 107, then 114; reuse 111–120, 124–130, 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-SUPPLY-DEMAND and ENGINE-PARAMETER-BOUND-FRAMES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story).
- **Files.** `elements/XrayEmitter/XrayEmitter.csproj`, `XrayEmitterElementDeclaration.cs`, `xray_emitter.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/XrayEmitterTests.cs`; a row in `VisibleSolidColliderTests`. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** An emitter that cannot be a supply consumer target or does not scale with the grant; no choice-bound source rows (the slice stops and ENGINE-PARAMETER-BOUND-FRAMES is amended; no second design is kept here); an emitter with no non-zero, non-isotropic half-angle for radiation channels.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** A static body, six colliders, three ports, one memory node, one supply consumer, three source rows of which one compiles, one parameter, three bindings and a band-mark selector.
2. **Generic laws.** Supply reservation and grant; the memory node's set and reset; the field law of the conventions; the loss ledgered to a collider. "Only while powered" is a supplied emitter whose enable names a signal level.
3. **Three layers.** Colliders: six primitives on one static body. Visuals: the art table; window and marks are flush or printed. Animation: three colour bindings from committed emission.
4. **One-way flow and typing.** Supply grant and the memory node's level → source → segments → readings → next tick's signal network. Bands, roles and ports are enums; powers are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** Static; six primitives; one shadow cast per receiver sample point inside the cone and range, each through the spatial query's segment cast (D × K shadow casts, radiation convention 12).
7. **Tests are scenarios.** Rows A1 to A11 run the production simulation through placement, wiring, Run and the committed read.
