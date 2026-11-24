# RAD-17 · Water moderator tank — element readiness spec

**Current delivery (24 October 2026):** [RAD-17 delivery slice](../invest/named-elements.md#radiation-17). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`, findings M-5, C-1, C-4, C-6, X-1 to X-4 and the Low note on the detector's lift) and owner decisions 18 and 20 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-17](../requirements.md#radiation-17) (P3 potential). Lengths are metres, volumes cubic metres. Radiation units, laws and the Liquid and Window rows are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions) (cited by item); liquid follows the [liquid conventions](EL-001-finite-reservoir.md#liquid-family-conventions-stated-once-here) L1 to L18. All values are game fiction: no reactor, multiplication or criticality is implied.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-17 · Water moderator tank |
| Type | Radiation + Water (a liquid vessel whose contents moderate neutrons) |
| Anchor | [requirements.md#radiation-17](../requirements.md#radiation-17); [named-elements.md#radiation-17](../invest/named-elements.md#radiation-17) |
| Owner | S627 |
| Disposition | To deliver as a single element. It is a liquid store with glass walls; moderation is a property of the scene's one liquid (D4), not of the tank, so the tank duplicates no liquid part only because of its shape, capacity and Window walls. |
| Planned package | id `moderator_tank`, directory `elements/ModeratorTank/`, class `ModeratorTankElementDeclaration` |
| Related identities | Source [RAD-16](RAD-16-neutron-source-module.md); detectors [EL-131](EL-131-fast-neutron-detector.md), [EL-132](EL-132-slow-neutron-detector.md); absorber [RAD-18](RAD-18-neutron-absorber-panel.md); liquid parts [EL-001](EL-001-finite-reservoir.md), [EL-003](EL-003-tap.md), [EL-006](EL-006-drain.md); gauge [EL-130](EL-130-tank-level-transmission-gauge.md) |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026, including ENGINE-CAPABILITY-INSTANCES), ENGINE-GAS-NETWORK (the fluid port contract), ENGINE-LIQUID, ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); the longest row drains the tank in 4.9 s, and every other row holds the water with the mouth joined to a closed Tap |

## 2. Declaration

Shape followed: the Finite reservoir's planned declaration ([EL-001 section 2](EL-001-finite-reservoir.md#declaration)): a static open-top vessel, one liquid store, one open aperture, one mouth. Items marked **(liquid)**, **(fluid port)** or **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), the one fluid port contract **(fluid port, ENGINE-GAS-NETWORK)**, which reshapes or replaces today's `IFluidElement` (`CuriousContraptions.Abstractions.Fluid/FluidContracts.cs:5`), plus the liquid store interface **(liquid)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, `elements/Battery/BatteryElementDeclaration.cs:61`–`:64`) |
| Colliders (hidden `col_` proxies) | Base box 0.70 × 0.05 × 0.70 m at (0, −0.425, 0) (top at y −0.40), default row. Two wall boxes 0.02 × 0.80 × 0.54 m at (±0.26, 0, 0) and two wall boxes 0.50 × 0.80 × 0.02 m at (0, 0, ±0.26), **Window** row **(field)**. One mouth tube, axis Z, outer radius 0.14 m, bore radius 0.10 m, length 0.10 m, centred (0, −0.26, −0.32) (y −0.40 to −0.12, z −0.37 to −0.27), default row. Six colliders. Checked pairwise: the walls meet the base at y −0.40 and each other at their edges, and the mouth tube touches the base top (y −0.40) and the −Z wall's outer face (z −0.27) without entering either. Should a later edit overlap two of them, radiation convention 4 counts the overlapped chord once. Interior 0.50 × 0.80 × 0.50 m. |
| Liquid store **(liquid)** | Local id 1 on body 0. Capacity 0.20 m³ (3.2 kg at 16 kg/m³), plan area 0.25 m², floor datum (0, −0.40, 0). Open aperture: rectangle 0.50 × 0.50 m at y = +0.40, normal +Y. Overflow point (0, 0.40, 0.27). Like every liquid store it is also a thermal node, authored at the 288 K ambient (owner decision 20; liquid conventions); no row here uses heat. |
| Ports | One liquid mouth `End` **(fluid port)**: a mouth-joined socket of the one fluid port contract (coordinator decision 16), role Bidirectional (the tank both drains and fills through it), pipe bore radius 0.10 m, at (0, −0.26, −0.37), normal −Z, its axis 0.14 m above the floor datum, opening into store 1 (L5). No electrical or signal port. |
| Parameters | `initial_volume`: Real, cubic metres **(liquid: the unit does not exist)**, default 0, minimum 0, maximum 0.20, editor policy Clamp, build only, player-editable unless the level locks it (D11). |
| Radiation | The tank declares only its Window walls. The water is the scene's liquid; its filled region and its Liquid row are crossed by the field solve (radiation convention 4). |
| Presentation bindings | `Visual/Water` height from the committed store depth **(liquid)**. A level scale is printed on one wall. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Mechanical, Fluid and the radiation flag **(field)** |
| Catalogue | Id `moderator_tank`, "Moderator tank", `ElementCategory.Radiation` **(field)**, palette order 721 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A glass tank. Water in it slows fast neutrons; some are lost. It drains from its mouth unless a closed tap is joined." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base` | box 0.70 × 0.05 × 0.70 m, navy `#293954` | solid (base box) | nothing |
| four `Visual/Wall*` | glass boxes equal to their colliders, cyan `#66b8c9`; a gold `#f7cb52` level scale printed flat on the +X wall | solid (wall boxes) | nothing |
| `Visual/Mouth` | tube r 0.14, bore radius 0.10, length 0.10 m at (0, −0.26, −0.32), cream `#fff8e9` | hollow (mouth tube) | nothing |
| `Visual/Water` | box 0.50 × depth × 0.50 m inside the walls | decorative: enclosed by the walls, sized by a binding | presentation binding |

Icon: an open glass tank with a waterline and a small "n" beside a wavy arrow. It differs from the Finite reservoir by its glass walls and the "n" mark, and from the Track chamber by being open-topped with water.

### Compiled counts

Bodies 1 (static). Colliders 6 (5 boxes, 1 tube); EL-001 plans 5 boxes. Joints 0. Drives 0. Sensors 0. Liquid stores 1 (also one thermal node); mouths 1; open apertures 1; overflow points 1. Signal and supply nodes 0. Parameters 1. Presentation bindings 1.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, FluidAdvection, GeometryQuery, IonizingTransport, TopologyTransaction. No topology change is needed. Probe made on `8d1d68aa`: **3 present, 1 untested, 2 narrower, 6 missing.** ENGINE-SPATIAL-QUERIES is a story of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:206`) |
| R3 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| U1 | Tube collider for the mouth | present, untested here: `ColliderShapeKind.Tube` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`); EL-001's probe row W2 cites its test, not opened for this spec | — | — |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) serves airflow rows only and loops over every collider (`:149`) | — | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast: **ENGINE-SPATIAL-QUERIES** |
| N2 | One fluid port contract with a mouth-joined socket (bore, role) | narrower: `IFluidElement` (`CuriousContraptions.Abstractions.Fluid/FluidContracts.cs:5`) is declared and validated, but no solver or element consumes it ([Story ENGINE-GAS-NETWORK](../../../_bmad-output/planning-artifacts/epics.md#story-engine-gas-network-connected-finite-gas-stores-lines-and-valves), *Present today*) | — | owner **ENGINE-GAS-NETWORK** (coordinator decision 16: one contract, link-joined and mouth-joined sockets) |
| M1 | Liquid store with capacity, plan area and level; mouths, free end, open aperture, overflow, accounts | **missing** | — | owner **ENGINE-LIQUID** ([Story ENGINE-LIQUID](../../../_bmad-output/planning-artifacts/epics.md#story-engine-liquid-conserved-liquid-inventory-head-driven-flow-and-immersion); EL-001 probe rows W6 to W9) |
| M2 | A parameter unit for cubic metres | **missing** (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:15`–`:35`) | — | owner **ENGINE-GAS-NETWORK**, which lands first (EL-001 row W13; owner decision 18, 22 November 2026) |
| M3 | Presentation driven by committed liquid depth | **missing** | — | owner **ENGINE-LIQUID** (EL-001 row W12) |
| M4 | The shared emitter, segment and receiver solve | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Radiation channels, per-collider rows, the declared conversion between channels (radiation convention 5), and a liquid store's committed filled region crossed as a volume with the liquid's row | **missing** | — | owner **ENGINE-RADIATION** |
| M6 | `ElementCategory.Radiation` and the radiation domain flag | **missing** (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) | — | owner **ENGINE-RADIATION** |

The store's thermal node is the liquid conventions' general rule (owner decision 20) and is ENGINE-THERMAL's; no row of this spec reads a temperature, so it is not a probe row here.

**Dependencies.** RAD-16, EL-131, EL-132 for every neutron row; EL-003 Tap to hold or fill the tank (all undelivered). The absorber and Dense-shield controls are row W6, a cross-part row owned by the RAD-18 slice (the absorber is taught at 127). This slice owns EL-132's cross-part rows S3 and S5, which need this tank (the detector precedes it in level 126).

## 4. Sources and legacy

- [radiation-17](../requirements.md#radiation-17): "Filling the tank increases the slow-group response in the authored range; a dry tank and a drained tank fail that control. Slowdown is not absorption."
- [Named entry](../invest/named-elements.md#radiation-17), owner S627; map row (S416/S470, S605); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) neutron row; research "Neutrons" law and slots 101 "Slow Water", 103 "A Rising Shield".
- **Legacy.** No moderator; legacy water code is outside the radiation scope. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2165-L2196`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1).

- **The Liquid row** (radiation convention 4): NeutronFast 2.4 /m, NeutronSlow 0.3 /m, with the conversion NeutronFast → NeutronSlow, m = 0.8 (convention 5). One crossing of the full 0.50 m of water with no slow power entering: fast out = e^(−2.4 × 0.5) = e^−1.2 = 0.301 of fast in; slow out = 0 × e^(−0.15) + 0.8 × (1 − 0.301) = 0.8 × 0.699 = 0.559 of fast in (converted power is not attenuated again inside the crossing that produced it); lost 0.2 × 0.699 = 0.140. The 0.140 is the documented loss. Slowdown is not absorption: four fifths of what leaves the fast channel reappears as slow, and fast out + slow out = 0.860 ≤ 1.
- **Reference layout.** Neutron module (10 W/sr NeutronFast) and a standard detector 1 m apart on the tank's X axis, the tank centred between them, the axis 0.40 m above the tank floor, which is 0.45 m above the bench. The detector's aperture centre stands 0.36 m above the bench (RAD-08's case and base) and the module's source point 0.31 m ([RAD-16](RAD-16-neutron-source-module.md) section 2: base bottom at −0.31 m), so the detector is placed 0.09 m and the module 0.14 m above the bench (static parts need no support) to share the axis. Their bases then pass above the tank's base (top 0.05 m above the bench), and the module's box (front face 0.275 m from the tank centre) clears the tank's walls (0.27 m) by 5 mm. The open reading is 0.882 W. Each Window wall keeps e^(−1 × 0.02) = 0.980 of either neutron channel.
- **Dry.** Fast 0.882 × 0.980 × 0.980 = 0.847 W. Slow 0.
- **Full (depth 0.80 m).** Fast 0.882 × 0.980 × 0.301 × 0.980 = 0.255 W. Slow 0.882 × 0.980 × 0.559 × 0.980 = 0.474 W: the slow detector is on (threshold 0.2 W) and the fast reading has fallen from 0.847 W. The second wall attenuates the slow power converted in the water by the same 0.980.
- **Partial fill.** The detector's three sample rows cross the tank at 0.325 to 0.375 m, 0.40 m and 0.425 to 0.475 m above the floor. At depth 0.20 m (0.05 m³) no row is wet: slow 0. At depth 0.412 m (0.103 m³; 12 mm above the middle row and 13 mm below the top row) two rows are wet: slow 0.474 × 6 ÷ 9 = 0.316 W and fast 0.847 × (3 + 6 × 0.301) ÷ 9 = 0.452 W. At depth 0.60 m (0.15 m³) all rows are wet: the full values. The slow reading never falls as the tank fills. An authored `initial_volume` sets the depth exactly at Run start, so the narrow band of the 0.103 m³ case is not exposed to the flow tolerance of L17.
- **Draining.** The `End` mouth's axis is 0.14 m above the floor. Unjoined, it is a free end (L8): a full tank drains to a depth of 0.14 m in 2 × A × √h ÷ K₀ = 2 × 0.25 × √0.66 ÷ 0.08349 = 0.5 × 0.8124 ÷ 0.08349 = 4.87 s, and the slow reading returns to 0 (no sample row is below 0.325 m). To hold water, join a closed Tap. The liquid goes to the first thing its stream meets or to `Spilled`; the ledger of L10 holds on every tick.
- **Absorber and Dense after the tank.** The panel's 0.30 m foot cannot stand in the 0.10 m of bench between the tank's base and the detector's base in this layout, so these rows use [RAD-18](RAD-18-neutron-absorber-panel.md)'s layout A: the detector's aperture 0.80 m from the tank centre, 1.30 m from the module. Open reading 0.526 W; slow through the full tank 0.526 × 0.980 × 0.559 × 0.980 = 0.283 W, on. Absorber panel (0.05 m, NeutronSlow 60 /m) after the tank: 0.283 × e^−3 = 0.283 × 0.0498 = 0.014 W, off. Dense slab instead (NeutronSlow 2.8 /m): 0.283 × e^−0.14 = 0.283 × 0.869 = 0.246 W, still on (23 % above the mark).
- **Photons.** A Gamma capsule in the same layout reads 0.882 × e^(−0.5 × 0.02) × e^(−1.2 × 0.5) × e^(−0.5 × 0.02) = 0.882 × 0.990 × 0.549 × 0.990 = 0.475 W through the full tank; a photon meter never reads neutrons.
- **Tick order** (radiation convention 11). The liquid step runs in each substep (L16). The field solve of tick n runs after the last substep and reads this tick's final liquid level; readings commit with the tick and act from the next tick.
- **Energy.** Fast out + slow out ≤ fast in on every path; the tank creates no neutron power (convention 5).

### Rows

Planned class `ModeratorTankTests`. Fixture: the reference layout. A supplied Fast-neutron detector and a supplied Slow-neutron detector cannot both sit on one axis, so each row names its detector. Unless a row says the mouth is unjoined, the mouth is joined to a closed Tap. Readings ±3 % (radiation convention 14); volumes 0.004 m³ (L17); stall guard three times the hand-calculated time, at least 10 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| W1 | Given `initial_volume` 0, then the slow detector reads 0 and stays off; the fast detector reads 0.847 W (row: a dry tank fails the control) | `ModeratorTankTests.ADryTankModeratesNothing` | W2 | ±3 %; slow under 0.001 W |
| W2 | Given `initial_volume` 0.20, then the slow detector reads 0.474 W and is on, and the fast detector reads 0.255 W (row: filling increases the slow response; task 427: Fast decreases) | `ModeratorTankTests.AFullTankDrivesTheSlowDetector` | W1 | ±3 % |
| W3 | Given volumes 0, 0.05, 0.103, 0.15 and 0.20 m³, then the slow readings are 0, 0, 0.316, 0.474 and 0.474 W and never fall from one to the next (row: water level affects the path) | `ModeratorTankTests.TheSlowReadingRisesWithTheWaterline` (theory) | W1 | ±5 % at 0.103 m³ |
| W4 | Given a full tank with the mouth unjoined, then within 15 s the depth is 0.14 m, the slow detector is off, and stores + `Spilled` + `Drained` equals the authored volume on every tick (row: a drained tank fails the control; conservation) | `ModeratorTankTests.ADrainedTankStopsModerating` | W2 with a closed Tap | volume 0.004 m³; drain time 4.87 s ± 10 % |
| W5 | Given W2 measured with both detectors in turn, then slow ÷ (fast removed) is 0.474 ÷ (0.847 − 0.255) = 0.80 and fast + slow (0.729 W) is below the dry fast reading (0.847 W) (row: slowdown is not absorption; documented loss) | `ModeratorTankTests.FourFifthsOfTheRemovedFastFluxReappearsAsSlow` | W1 | ratio 0.80 ± 0.03 |
| W6 | Given RAD-18's layout A and an Absorber panel between the full tank and the slow detector, the detector is off; given a Dense shield there instead, it stays on (a cross-part row owned by the RAD-18 slice: the absorber is taught at 127, after this tank at 126) | `ModeratorTankTests.OnlyTheAbsorberQuietsTheSlowDetector` (theory) | no panel: 0.283 W | 0.014 W and 0.246 W ±5 % |
| W7 | Given a photon rate meter in place of the detectors, then it reads nothing from the Neutron module with the tank dry or full | `ModeratorTankTests.APhotonMeterIgnoresNeutrons` | W2 | under 0.001 W |
| W8 | Given a Tap pouring into the open top, then the slow detector turns on once the waterline passes the middle sample row and not before (queued until EL-003 exists) | `ModeratorTankTests.FillingFromATapTurnsTheSlowDetectorOn` | W1 | depth 0.40 ± 0.03 m at the transition |
| W9 | Given `initial_volume` −0.01, 0.21 or a non-finite value, then the edit is refused and nothing changes; 0 and 0.20 are admitted | `ModeratorTankTests.TheInitialVolumeIsBounded` | W2 | typed refusal |
| W10 | Given W4 after draining, when Reset, then the store holds its authored 0.20 m³ and the accounts are zero; a saved and reloaded construction keeps `initial_volume` | `ModeratorTankTests.ResetRestoresTheAuthoredWater`; authored state by R3 | W2 | volume 0.004 m³ |

A liquid mouth joins only another mouth of equal bore (L6); a wire cannot attach, so there is no wire-refusal row. Chrome recipe (pending, post-catalogue): Neutron module, tank with a closed Tap on its mouth, Slow-neutron detector with a lamp; pour from a Tap above.

## 6. Open questions

None open.

1. **Fractions and wet-path scaling — decided** (D1, D16; radiation conventions 4 and 5): the Liquid row above, applied by chord through the committed filled region.
2. **How it fills — decided** (D3, D4): through its open top from any stream, and through its one Bidirectional mouth; no second mechanism.
3. **Palette group — decided now**: Radiation. Its lesson is moderation; a second tank in the Water group would duplicate the Finite reservoir there. Order 721 (coordinator decision 36).
4. **Authored initial fill — decided now**: an `initial_volume` setting, default 0, so lessons start dry and filling is the player's act.
5. **Polymer as a moderator — decided in the conventions**: only the liquid converts fast to slow.
6. **Mouth position — decided** (audit M-5): the mouth sits on the base, not in it, so no two colliders overlap; the drain level is 0.14 m.

Needs the owner: none.

## 7. Integration

- **Collaborators.** RAD-16, EL-131, EL-132, RAD-18, EL-128, EL-130, EL-001, EL-003, EL-006 (all undelivered). The `End` mouth is a Bidirectional mouth of pipe bore against any mouth of the same bore (liquid conventions L6).
- **Cross-element task.** [sequence-task-427](../requirements.md#sequence-task-427): rows W2 and W7 with the two detectors and a photon control; processes [IX-15](../requirements.md#interaction-15) and [IX-08](../requirements.md#interaction-08).
- **Campaign.** Chapter 13, levels 126 and 128; reuse 136–150. Both lessons place the tank with its mouth joined to a closed Tap (an unjoined mouth drains it in 4.87 s). No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22); a level that starts with a Tap already joined to the mouth needs a pre-made join.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-CAPABILITY-INSTANCES (the mouth's snap socket); ENGINE-GAS-NETWORK (the one fluid port contract), then ENGINE-LIQUID, then ENGINE-THERMAL (owner decision 18 roadmap order; it also makes the store a thermal node, decision 20, though no row here depends on heat); ENGINE-OPTICAL; ENGINE-RADIATION as amended, its liquid crossing after ENGINE-LIQUID; EL-001 and EL-003 (they prove the store and the tap first); RAD-16, EL-131 and EL-132; S7 for the binding.
- **Files.** `elements/ModeratorTank/ModeratorTank.csproj`, `ModeratorTankElementDeclaration.cs`, `moderator_tank.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/ModeratorTankTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** A liquid representation whose filled region the field solve cannot cross (then moderation has no general home; do not add a tank-specific rule); ENGINE-GAS-NETWORK or ENGINE-LIQUID changing mouth roles or the free-end rule (re-derive draining); a different Liquid row or conversion law (re-derive section 5).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** One static body, six colliders, one liquid store with an aperture, an overflow point and one mouth, one parameter, one binding, one Window row. No code names the tank.
2. **Generic laws.** Head-driven flow and the liquid ledger; chord attenuation with a declared channel conversion through any liquid's filled region. "Water slows neutrons" is the liquid's row; any vessel holding the liquid moderates.
3. **Three layers.** Colliders: five boxes and a tube, none overlapping. Visuals: the glass, the mouth, a printed scale; the water art is enclosed and sized by a binding. Animation: the water height binding only.
4. **One-way flow and typing.** Liquid step → committed level → this tick's field solve after the last substep → readings acting next tick; presentation reads committed state. Channels and mouth roles are enums; values are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** Static body; six primitives; the filled region is one more crossed volume on a path, found through the spatial query's segment cast; the liquid's cost is ENGINE-LIQUID's bounded per-store work.
7. **Tests are scenarios.** Rows W1 to W10 run the production simulation through placement, joining, Run and committed reads.
