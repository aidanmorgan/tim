# RAD-03 · Alpha source cartridge — element readiness spec

**Current delivery (24 October 2026):** [RAD-03 delivery slice](../invest/named-elements.md#radiation-03). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-03](../requirements.md#radiation-03) (P2 potential). Lengths are metres; local +X is the beam direction. A fictional sealed toy cartridge; never real-world source guidance. Units, sources, the propagation law and tick order are the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions). **This spec is the lead of the Family slice "Charged cartridges"**; [RAD-04](RAD-04-beta-minus-source-cartridge.md) cites its shape, probe and conformance points.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-03 · Alpha source cartridge |
| Type | Radiation (charged-particle source) |
| Anchor | [requirements.md#radiation-03](../requirements.md#radiation-03); [named-elements.md#radiation-03](../invest/named-elements.md#radiation-03) |
| Owner | S620 |
| Disposition | To deliver as a member, and the lead, of the Family slice **Charged cartridges** with [RAD-04](RAD-04-beta-minus-source-cartridge.md). The two differ only in declared values: channel, range, printed dots, id, title, description and icon. Each has its own catalogue entry (D8) and its own rows. Neither is a duplicate of the other: the requirement keeps them distinct inventory items. |
| Planned package | id `alpha_source_cartridge`, directory `elements/AlphaSourceCartridge/`, class `AlphaSourceCartridgeElementDeclaration` |
| Related identities | Sibling [RAD-04](RAD-04-beta-minus-source-cartridge.md); receiver [RAD-08](RAD-08-radiation-rate-meter.md) with `sensitivity` Alpha; stopper [EL-126 Thin screen](EL-126-thin-screen-shield.md); steering [RAD-14](RAD-14-magnetic-deflector.md), [RAD-15](RAD-15-track-chamber.md). No CAT spec. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); the longest row is 25 s |

## 2. Declaration

Shape followed: [RAD-01](RAD-01-gamma-source-capsule.md#2-declaration) for a static source with no port. Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the emitter interface **(field)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`elements/Battery/BatteryElementDeclaration.cs:62`) |
| Colliders (hidden `col_` proxies) | Cartridge box 0.25 × 0.30 × 0.25 m at the origin. Base box 0.35 × 0.06 × 0.35 m at (0, −0.18, 0), touching the cartridge. Two colliders; neither declares a radiation row, so both take the scene's default row. |
| Ports | None: emission is continuous during Run. |
| Source **(field)** | Frame body 0; origin (0.125, 0, 0), on the +X face; axis +X; half-angle 0 (collimated); range 2.0 m; radiant power 1 W in Alpha; paid by a steady finite inventory (half-life none) of 1 W × 300 s = 300 J; internal occluders = both colliders. |
| Parameters | None. Direction is the part's orientation. |
| Stores | The steady inventory above (radiation convention 2): each tick debits 1 ÷ 120 = 0.00833 J; it holds the 300 s maximum Run of ENGINE-RUN-LENGTH (the story of owner decisions 13 and 17; it only sizes this declared inventory and is not a prerequisite of this slice), so the strength is constant throughout any Run; Reset restores 300 J (owner decision of 21 November 2026; coordinator decision 21, 22 November 2026). |
| Presentation bindings | None during Run. The selected-part Build preview (ENGINE-OPTICAL's, radiation convention 13) draws the beam to the end of its range. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; the radiation domain flag **(field)** |
| Catalogue | Id `alpha_source_cartridge`, "Alpha cartridge", `ElementCategory.Radiation` **(field)**, palette order 712 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Sends a thin beam of alpha particles from its ring. The beam ends after two metres and a thin screen stops it." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Cartridge` | box 0.25 × 0.30 × 0.25 m, cream `#fff8e9` | solid (cartridge box) | nothing |
| `Visual/Base` | box 0.35 × 0.06 × 0.35 m at y −0.18, navy `#293954` | solid (base box) | nothing |
| `Visual/OutletRing` | ring r 0.04 m printed flush on the +X face, navy | decorative: printed flat | nothing |
| `Visual/Mark` | "α" and one dot printed flat on the top face, gold `#f7cb52` | decorative: printed flat | nothing |

The outlet is a printed ring, not a recess or a nozzle, so nothing proud needs a collider and the housing has no hole that could block or leak. Icon: a small box with a ring and one dot. It differs from the Beta-minus cartridge by the dot count (one against two), a pattern and not a colour.

### Compiled counts

Bodies 1 (static). Colliders 2 (2 boxes), as RAD-01. Joints 0. Drives 0. Sensors 0. Signal nodes 0. Supply nodes 0. Sources 1 (collimated). Inventories 1 (steady, 300 J). Presentation bindings 0.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, RadioactiveDecay. 3 present, 0 untested, 1 narrower, 4 missing. ENGINE-SPATIAL-QUERIES is a story of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row of the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`) |
| R3 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) is for airflow rows only, gives no crossing lengths and loops over every collider (`:149`) | — | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| M1 | A declared collimated emitter with a range, internal occluders skipped on its first segment, and the shared segment and receiver solve | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels; a collimated segment that crosses every collider with surface transmittance 1 and per-channel attenuation along the chord, the default row for colliders that declare none; a steady finite inventory that pays an emitter | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | `ElementCategory.Radiation` and the radiation `SimulationDomain` flag | **missing** | — | owner **ENGINE-RADIATION** |
| M4 | The selected-part Build preview that runs the same solve and commits nothing | **missing** | — | owner **ENGINE-OPTICAL** (optical convention 12; radiation convention 13) |

**Dependencies.** RAD-01 (undelivered, first). Rows read a law-scene receiver until RAD-08 exists; the screen rows use a law-scene box with the Thin screen row until EL-126 exists.

## 4. Sources and legacy

- [radiation-03](../requirements.md#radiation-03): "A close compatible detector responds; extra separation or a thin screen suppresses it. Gamma-only reception rejects it."
- [Named entry](../invest/named-elements.md#radiation-03), owner S620; map row (S605); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) alpha row ("thin shield passes, thick shield stops" — conflicts with this row; open question 1); research row ("reaches only a close meter; a thin screen stops it"; slots 90 "The Short Journey", 93).
- **Legacy.** None in parts, engine, tests, levels, Campaign or reference. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1690-L1720`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Beam.** A zero-width ray along +X from the outlet, carrying 1 W of Alpha. A collimated beam does not spread, so there is no inverse-square fall: a receiver whose aperture the ray ends on reads 1 W anywhere inside the range and 0 beyond it. Air does not attenuate. Every collider on the ray is crossed with surface transmittance 1 and multiplied by exp(−μ × chord) of its row (radiation convention 4). A receiver is credited with the power the ray carries where it crosses its aperture, after every crossing before it, the receiver's own colliders included (the one own-body rule of optical convention 5 and radiation convention 4; coordinator decision 26); its frame body behind the aperture does not reduce that credit, and the ray continues through it. A receiver with no collider of its own in front of its aperture, as on the meters this cartridge feeds, therefore reads the ray's whole power when nothing else stands between.
- **Why 1 W.** It is five times the standard 0.2 W mark, so the beam still switches a detector after the deflector's relay (0.8) and one Window pane (0.905): 1 × 0.8 × 0.905 = 0.72 W.
- **Why 2 m.** Short enough that "extra separation" is a small move on the bench, long enough for the deflector layout: 0.3 m to the inlet + 0.41 m internal path + 0.65 ÷ cos 15.47° = 0.675 m to a track chamber's exit = 1.385 m.
- **Thin screen.** 0.02 m at 400 /m: 1 × e^−8 = 0.000335 W, below the 0.001 W floor, so the segment ends in the screen. Any collider with the default row (Alpha 400 /m) does the same within ln 1000 ÷ 400 = 0.0173 m.
- **Thickness decides.** A 0.002 m foil of the default row passes 1 × e^(−400 × 0.002) = e^−0.8 = 0.449 W, above the 0.2 W mark. So a thin enough foil passes alpha and the 0.02 m Thin screen stops it: both the requirement row and S605's "thin passes, thick stops" hold, read as statements about thickness.
- **Window pane.** 0.02 m at 5 /m: 1 × e^−0.1 = 0.905 W passes.
- **Channel.** Only Alpha is carried. A receiver weighted for photon channels reads 0.
- **Own housing.** The origin is on the +X face and both colliders are internal occluders, so the housing cannot block the outlet. Another part's solid in front of the outlet does.
- **Aim.** The ray is thin: the standard 0.30 m face must sit on the axis. Its centre may be up to 0.15 m off axis; at 0.30 m off it reads 0.
- **Constant strength, paid.** 1 W from a 300 J inventory: each tick debits 1 ÷ 120 = 0.00833 J; after 25 s the debit is 25 J, leaving 275 J; Reset restores 300 J. The inventory holds the 300 s maximum Run, so the strength never falls within a Run (owner decision of 21 November 2026; coordinator decision 21, 22 November 2026). [IX-16](../requirements.md#interaction-16) holds as written. A receiver never reads more than the 1 W emitted.
- **Tick order.** As [RAD-01](RAD-01-gamma-source-capsule.md#5-acceptance): the field solve of each tick runs after its last substep on that tick's final poses and commits the reading with the tick (radiation convention 11).

### Rows

Planned class `ChargedCartridgeTests` (one class for the family; shared rows are theories over the two packages). Fixture: the cartridge in a production law scene with a receiver row of the standard aperture weighted for its channel, centred on the axis. Readings and energies ±3 %; stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given the receiver 1 m along the axis, when Run starts, then it reads 1 W (row: a close compatible detector responds) | `ChargedCartridgeTests.ABeamOnACompatibleReceiverReadsItsWholePower` | no cartridge reads 0 | ±3 % |
| A2 | Given the receiver at 1.85 m it reads 1 W; at 2.15 m it reads 0 (row: extra separation suppresses it) | `ChargedCartridgeTests.TheBeamEndsAtItsDeclaredRange` | A1 | 0.15 m either side of 2.0 m |
| A3 | Given a law-scene box 0.02 m thick with the Thin screen row on the axis, then the receiver reads 0; the box beside the axis leaves 1 W; a law-scene box 0.002 m thick with the default row on the axis leaves 0.449 W (row: a thin screen suppresses it; thickness decides) | `ChargedCartridgeTests.AThinScreenStopsAlpha` | A1 | below 0.001 W; 0.449 W ±3 % |
| A4 | Given a receiver weighted for the three photon channels at 1 m, then it reads 0 for 240 ticks (row: gamma-only reception rejects it) | `ChargedCartridgeTests.APhotonReceiverRejectsAChargedBeam` | A1 | below 0.001 W |
| A5 | Given the bare fixture, then the reading equals that of a law-scene emitter with no housing; a Wall placed against the outlet gives 0 (row context: its enclosure must not block its own outlet) | `ChargedCartridgeTests.TheHousingNeverBlocksItsOwnOutlet` | A1 | ±3 %; below 0.001 W |
| A6 | Given the receiver's centre 0.10 m off axis it reads 1 W; 0.30 m off axis it reads 0 | `ChargedCartridgeTests.TheBeamHasNoWidth` | A1 | exact side of the 0.15 m half-face |
| A7 | Given a law-scene box 0.02 m thick with the Window row on the axis, then the receiver reads 0.905 W | `ChargedCartridgeTests.AWindowPanePassesMostOfTheBeam` | A3 | ±3 % |
| A8 | Given the part turned to face −X, +Z and −Z, then the beam follows the part | `ChargedCartridgeTests.TheBeamFollowsThePartsFacing` | receivers on the other axes read 0 | ±3 % |
| A9 | Given a Run of 25 s the reading is unchanged and the inventory has been debited 25 J; when Reset, no reading remains, the inventory is 300 J and the next Run reads as A1; the declaration has no port; a saved and reloaded construction behaves as A1 | `ChargedCartridgeTests.ResetAndReloadRestoreTheAuthoredSource`; authored state by R3 | A1 | ±3 % reading and energies |
| A10 | Given a supplied Rate meter with `sensitivity` Alpha 1 m along the axis, then its load is supplied; with `sensitivity` Photon it is not; with an EL-126 screen between, it is not | `ChargedCartridgeTests.TheRealMeterAndScreenAgree` (queued until RAD-08 and EL-126 are delivered) | A3, A4 | load state |

There is no parameter, so no parameter-refusal row. Steering by the deflector and the track chamber's readout are rows of [RAD-14](RAD-14-magnetic-deflector.md) and [RAD-15](RAD-15-track-chamber.md), which name this cartridge as their source. Chrome recipe (pending, post-catalogue): cartridge aimed at an Alpha-set Rate meter 1 m away with a load; move the meter to 2.5 m; put a Thin screen between.

## 6. Open questions

None open.

1. **S605 says a thin shield passes alpha; the row says a thin screen suppresses it — resolved by thickness**: both hold. A 0.002 m default-row foil passes 0.449 W; the 0.02 m Thin screen leaves 0.000335 W. Row A3 proves both, and the S605 line needs no correction.
2. **Range shape, cone and curvature — decided now**: a collimated ray with a hard range, no cone and no tail. A cone would spread over every track-chamber segment and could not enter a relay; a hard end is readable. Curvature is the deflector's declared outlet, not a value of this part.
3. **Range against the inverse-square region — resolved**: a collimated beam has no inverse-square region, so the two no longer coincide.
4. **Payment — decided by the owner** (21 November 2026, and coordinator decision 21, 22 November 2026): constant strength from a finite inventory of 1 W × 300 s = 300 J, restored on Reset.

Needs the owner: none.

## 7. Integration

- **Collaborators.** RAD-08, EL-126, RAD-14, RAD-15 (all undelivered); Wall (delivered). No port, so no port pair needs to agree.
- **Cross-element tasks.** Source in [sequence-task-413](../requirements.md#sequence-task-413) and in the RAD-14 opposite-charge proof; processes [IX-15](../requirements.md#interaction-15) and [IX-16](../requirements.md#interaction-16) (steady inventory; holds as written).
- **Campaign.** Chapter 12, first use 115, then 118; reuse 121–125, 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity needs S7 to widen it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Family slice of two packages (this and RAD-04), one review, one candidate commit.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story). Row A10 runs when RAD-08 and EL-126 exist.
- **Files.** `elements/AlphaSourceCartridge/AlphaSourceCartridge.csproj`, `AlphaSourceCartridgeElementDeclaration.cs`, `alpha_source_cartridge.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/ChargedCartridgeTests.cs` (shared with RAD-04); a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** Collimated segments that stop at the first collider with no attenuated continuation (then the Window and thin-screen rows for RAD-04 fail and ENGINE-RADIATION is amended first); a different floor than 0.001 W (re-derive the screen rows); no steady finite inventory that pays an emitter (an engine slice first); a family member found to need a capability the other does not (then two slices).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** A static body, two colliders, one collimated source row and one steady inventory.
2. **Generic laws.** The field law of the conventions: range, per-channel attenuation along a chord, the 0.001 W floor, the inventory debit. "A thin screen stops it" is e^−8 of a declared row, not a rule about screens.
3. **Three layers.** Colliders: two boxes. Visuals: the art table; ring and mark are printed flat. Animation: none.
4. **One-way flow and typing.** Inventory → source → segment → readings. The channel is an enum; power and energy are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** Static; two primitives; one collimated source costs one segment cast per tick: a pass-through collimated ray is one cast returning every crossing (the C × L term of radiation convention 12), through the spatial query's segment cast. A ray is tested against this tick's final poses, so tunnelling does not arise.
7. **Tests are scenarios.** Rows A1 to A10 run the production simulation through placement, Run and the committed read.
