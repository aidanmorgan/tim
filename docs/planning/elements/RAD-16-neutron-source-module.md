# RAD-16 · Neutron source module — element readiness spec

**Current delivery (24 October 2026):** [RAD-16 delivery slice](../invest/named-elements.md#radiation-16). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-16](../requirements.md#radiation-16) (P3 potential). Lengths are metres. A **distinct contained toy source**: no implied reactor, multiplication or gamma-shielding equivalence. Units, sources, the propagation law and tick order are the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-16 · Neutron source module |
| Type | Radiation (neutron source, specialist transport domain) |
| Anchor | [requirements.md#radiation-16](../requirements.md#radiation-16); [named-elements.md#radiation-16](../invest/named-elements.md#radiation-16) |
| Owner | S626 |
| Disposition | To deliver as a single element after [RAD-01](RAD-01-gamma-source-capsule.md), whose declaration shape it follows. It is not a duplicate: its channel, body and acceptance differ. |
| Planned package | id `neutron_source_module`, directory `elements/NeutronSourceModule/`, class `NeutronSourceModuleElementDeclaration` |
| Related identities | Detectors [EL-131 Fast](EL-131-fast-neutron-detector.md), [EL-132 Slow](EL-132-slow-neutron-detector.md); moderator [RAD-17](RAD-17-water-moderator-tank.md); absorber [RAD-18](RAD-18-neutron-absorber-panel.md); photon-only control [RAD-08](RAD-08-radiation-rate-meter.md); field control [RAD-14](RAD-14-magnetic-deflector.md). No CAT spec. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 2 s |

## 2. Declaration

Shape followed: [RAD-01](RAD-01-gamma-source-capsule.md#2-declaration). Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the emitter interface **(field)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`elements/Battery/BatteryElementDeclaration.cs:62`) |
| Colliders (hidden `col_` proxies) | Module box 0.45 × 0.50 × 0.45 m at the origin. Base box 0.55 × 0.06 × 0.55 m at (0, −0.28, 0), touching the module. Two colliders; neither declares a radiation row, so both take the scene's default row. |
| Ports | None: no electrical switch. |
| Source **(field)** | Frame body 0; origin (0, 0, 0); isotropic; range 16 m; radiant power 125.66 W in NeutronFast (4π × 10 W/sr); paid by a steady finite inventory (half-life none) of 125.66 W × 300 s = 37,699 J; internal occluders = both colliders. |
| Parameters | None. |
| Stores | The steady inventory above (radiation convention 2): each tick debits 125.66 ÷ 120 = 1.047 J; it holds the 300 s maximum Run of ENGINE-RUN-LENGTH (the story of owner decisions 13 and 17; it only sizes this declared inventory and is not a prerequisite of this slice), so the strength is constant throughout any Run; Reset restores 37,699 J (owner decision of 21 November 2026; coordinator decision 21, 22 November 2026). |
| Presentation bindings | None during Run. The selected-part Build preview is ENGINE-OPTICAL's shared one (radiation convention 13). |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; the radiation domain flag **(field)** |
| Catalogue | Id `neutron_source_module`, "Neutron source", `ElementCategory.Radiation` **(field)**, palette order 719 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A sealed source of fast neutrons. Magnets do not bend them and heavy shields barely stop them." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Module` | box 0.45 × 0.50 × 0.45 m, cream `#fff8e9` | solid (module box) | nothing |
| `Visual/Base` | box 0.55 × 0.06 × 0.55 m at y −0.28, navy `#293954` | solid (base box) | nothing |
| `Visual/Bands` | two navy bands printed flat round the box | decorative: printed flat | nothing |
| `Visual/Mark` | "n" with two chevrons printed flat on each side face, gold `#f7cb52` | decorative: printed flat | nothing |

Icon: a banded box with an "n". It differs from the Gamma capsule by its square section and two bands, and it shares the two-chevron "fast" mark with the Fast-neutron detector.

### Compiled counts

Bodies 1 (static). Colliders 2 (2 boxes), as RAD-01. Joints 0. Drives 0. Sensors 0. Signal nodes 0. Supply nodes 0. Sources 1. Inventories 1 (steady, 37,699 J). Presentation bindings 0.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, RadioactiveDecay. 3 present, 0 untested, 1 narrower, 4 missing. ENGINE-SPATIAL-QUERIES is a story of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row of the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R3 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) is for airflow rows only and loops over every collider (`:149`) | — | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| M1 | A declared emitter and the shared segment and receiver solve | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels with two neutron groups, a steady finite inventory that pays an emitter, per-collider rows with the scene's default row and a declared conversion between channels | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | `ElementCategory.Radiation` and the radiation `SimulationDomain` flag | **missing** | — | owner **ENGINE-RADIATION** |
| M4 | The selected-part Build preview that runs the same solve and commits nothing | **missing** | — | owner **ENGINE-OPTICAL** (optical convention 12; radiation convention 13) |

**Dependencies.** RAD-01 (undelivered, first). For later rows: EL-131, RAD-08, RAD-14, RAD-17, RAD-18 (all undelivered).

## 4. Sources and legacy

- [radiation-16](../requirements.md#radiation-16): "A fast-sensitive neutron detector responds; a photon-only detector does not. Ordinary magnetic deflection leaves the neutron route unchanged."
- [Named entry](../invest/named-elements.md#radiation-16), owner S626; map row (S605); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) neutron row; research "Neutrons" law and slots 100 "The Neutral Visitor", 105 "Two Kinds of Shelter".
- **Legacy.** None. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2134-L2164`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Strength.** 125.66 W over 4π sr is 10 W/sr, the Gamma capsule's intensity, so the standard detector reads the same 0.882 W at 1 m (0.900 W anywhere inside 1 m) and the lesson is the channel, not the strength.
- **Channel.** Only NeutronFast is emitted. A receiver whose weights are photon channels reads 0; so does one weighted for NeutronSlow, because nothing has converted the flux.
- **Dense is a poor neutron shield.** One Dense slab (0.05 m, NeutronFast 1.6 /m): 0.882 × e^−0.08 = 0.882 × 0.923 = 0.814 W. Two: 0.882 × e^−0.16 = 0.882 × 0.852 = 0.752 W. Both stay far above the 0.2 W mark, where the same two slabs cut the Gamma capsule to 0.119 W. This is "no gamma-shielding equivalence".
- **Fields.** The deflector's relay lists only Alpha and BetaMinus. NeutronFast crosses its two Window panes (1 /m × 0.02 m each) by the attenuation law, e^−0.04 = 0.961, whether the coil is supplied or not: 0.882 × 0.961 = 0.848 W both ways.
- **Own housing.** Both colliders are internal occluders of the module's own emission. They take the default row and attenuate other sources' rays: through the 0.45 m module box NeutronFast keeps e^(−0.2 × 0.45) = e^−0.09 = 0.914.
- **Model limits.** No multiplication, no activation of what it strikes, no secondary photons, no buildup or scatter. Conversion to the slow group happens only in a collider or liquid whose row declares it (radiation convention 5; [RAD-17](RAD-17-water-moderator-tank.md)).
- **Constant strength, paid.** 125.66 W from a 37,699 J inventory, debited 1.047 J each tick; after 2 s the debit is 125.66 × 2 = 251 J; Reset restores 37,699 J. The inventory holds the 300 s maximum Run, so the strength never falls within a Run (owner decision of 21 November 2026; coordinator decision 21, 22 November 2026). [IX-16](../requirements.md#interaction-16) holds as written.
- **Energy and tick order.** As [RAD-01](RAD-01-gamma-source-capsule.md#5-acceptance): a 0.09 m² face at 1 m receives at most 0.9 W of the 125.66 W; the field solve of each tick runs after its last substep on that tick's final poses and commits the readings with the tick (radiation convention 11).

### Rows

Planned class `NeutronSourceModuleTests`. Fixture: the module in a production law scene with standard receiver rows 1 m away, one weighted NeutronFast, one weighted for the three photon channels, one weighted NeutronSlow. Readings and energies ±3 %; stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given the fixture, when Run starts, then the fast receiver reads 0.882 W (row: a fast-sensitive detector responds) | `NeutronSourceModuleTests.AFastReceiverReadsTheModule` | no module reads 0 | ±3 % |
| A2 | Given the fixture, then the photon receiver and the slow receiver read 0 for 240 ticks (row: a photon-only detector does not respond) | `NeutronSourceModuleTests.PhotonAndSlowReceiversReadNothing` | A1 | below 0.001 W |
| A3 | Given one and two law-scene boxes 0.05 m thick with the Dense row on the path, then the fast receiver reads 0.814 W and 0.752 W | `NeutronSourceModuleTests.DenseMaterialBarelyStopsFastNeutrons` | the Gamma capsule behind the same two boxes reads 0.119 W | ±3 % |
| A4 | Given a Magnetic deflector on the path, supplied and unsupplied, in both polarities, then the fast receiver reads 0.848 W in all four cases and a receiver on the deflector's bent outlets reads 0 (row: magnetic deflection leaves the route unchanged) | `NeutronSourceModuleTests.AMagneticFieldDoesNotBendNeutrons` (queued until RAD-14 is delivered) | a Beta-minus beam is bent (RAD-04's row) | ±3 % |
| A5 | Given the module at 1 m, 2 m and 3 m, then the readings are 0.882, 0.224 and 0.100 W | `NeutronSourceModuleTests.TheReadingFallsWithDistance` | A1 | ±3 % |
| A6 | Given a supplied Fast-neutron detector 1 m away with a load, then the load is supplied; a supplied Rate meter in Photon sensitivity leaves its load unsupplied | `NeutronSourceModuleTests.TheRealDetectorsAgree` (queued until EL-131 and RAD-08 are delivered) | A2 | load state |
| A7 | Given a Run of 240 ticks, then the inventory has been debited 251 J; when Reset, no reading remains, the inventory is 37,699 J and the next Run reads as A1; the declaration has no port; a saved and reloaded construction behaves as A1 | `NeutronSourceModuleTests.ResetAndReloadRestoreTheAuthoredSource`; authored state by R3 | A1 | pose within 0.01 m; energies ±3 % |

There is no parameter, so no parameter-refusal row. The moderated case of [sequence-task-427](../requirements.md#sequence-task-427) is a row of RAD-17 and the two detectors. Chrome recipe (pending, post-catalogue): Battery, module 1 m from a Fast-neutron detector with a Powered gate; beside it a Rate meter that stays dark.

## 6. Open questions

None open.

1. **Activity and group fractions — decided** (D1, D16): 125.66 W NeutronFast; fractions are the rows of the conventions.
2. **A weak photon component — decided** (D19): none. The requirement treats the module as a distinct domain, and a photon component would make the photon-only control fail.
3. **Payment — decided by the owner** (21 November 2026, and coordinator decision 21, 22 November 2026): constant strength from a finite inventory of 37,699 J, restored on Reset.

Needs the owner: none.

## 7. Integration

- **Collaborators.** EL-131, EL-132, RAD-17, RAD-18, RAD-08, RAD-14, EL-128 (all undelivered). No port, so no port pair needs to agree.
- **Cross-element tasks.** Source in [sequence-task-427](../requirements.md#sequence-task-427); processes [IX-15](../requirements.md#interaction-15) and [IX-16](../requirements.md#interaction-16) (steady inventory; holds as written).
- **Campaign.** Chapter 13, first use 125, then 130; reuse 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity needs S7 to widen it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story). Rows A4 and A6 run when their parts exist.
- **Files.** `elements/NeutronSourceModule/NeutronSourceModule.csproj`, `NeutronSourceModuleElementDeclaration.cs`, `neutron_source_module.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/NeutronSourceModuleTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** ENGINE-RADIATION delivered with one neutron channel (then EL-131, EL-132 and RAD-17 have no model and the family is re-planned); different Dense neutron coefficients (re-derive A3); no steady finite inventory that pays an emitter (an engine slice first).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** A static body, two colliders, one source row and one steady inventory.
2. **Generic laws.** The field law of the conventions and the inventory debit. "Magnets do not bend it" is the relay's channel mask, not a rule about this part.
3. **Three layers.** Colliders: two boxes. Visuals: the art table; bands and marks are printed flat. Animation: none.
4. **One-way flow and typing.** Inventory → source → segments → readings. The channel is an enum; power and energy are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** Static; two primitives; one shadow cast per receiver sample point in the scene (the D × K term of radiation convention 12), each through the spatial query's segment cast.
7. **Tests are scenarios.** Rows A1 to A7 run the production simulation through placement, Run and the committed read.
