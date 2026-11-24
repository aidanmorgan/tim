# RAD-13 · Decay clock capsule — element readiness spec

**Current delivery (24 October 2026):** [RAD-13 delivery slice](../invest/named-elements.md#radiation-13). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-13](../requirements.md#radiation-13) (P2 potential). Lengths are metres. A **visibly marked fictional short-half-life toy source**; half-lives are game presets, not real isotopes. Units, the decaying-inventory law and tick order are the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-13 · Decay clock capsule |
| Type | Radiation (decaying photon source) |
| Anchor | [requirements.md#radiation-13](../requirements.md#radiation-13); [named-elements.md#radiation-13](../invest/named-elements.md#radiation-13) |
| Owner | S623 |
| Disposition | To deliver as a single element after [RAD-01](RAD-01-gamma-source-capsule.md), whose body it reuses. It is not a duplicate of RAD-01: it has a decaying inventory, a `half_life` setting and its own acceptance. It is not a renamed [CAT-022 delay](CAT-022-delay.md) or [CAT-017 clock](CAT-017-clock.md): it emits no signal and has no port. |
| Planned package | id `decay_clock_capsule`, directory `elements/DecayClockCapsule/`, class `DecayClockCapsuleElementDeclaration` |
| Related identities | Steady control [RAD-01](RAD-01-gamma-source-capsule.md); receiver [RAD-08](RAD-08-radiation-rate-meter.md); memory partner [RAD-09](RAD-09-integrating-dosimeter.md). No CAT spec refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-RUN-LENGTH (owner decisions 13 and 17), ENGINE-OPTICAL, ENGINE-RADIATION and RAD-01 |
| Run length needed | 60 s (ENGINE-RUN-LENGTH): row A1 reaches three half-lives of the 16 s preset at 48 s and row A6 runs 60 s. Levels 123 and 124 declare 45 s for the 16 s preset, whose meter releases at 38.6 s. |

## 2. Declaration

Shape followed: [RAD-01](RAD-01-gamma-source-capsule.md#2-declaration). Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the emitter interface **(field)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`elements/Battery/BatteryElementDeclaration.cs:62`) |
| Colliders (hidden `col_` proxies) | Capsule cylinder, axis Y, radius 0.15 m, height 0.40 m at the origin. Base box 0.40 × 0.06 × 0.40 m at (0, −0.23, 0). Two colliders; neither declares a radiation row, so both take the scene's default row. |
| Ports | None: decay has no electrical control. |
| Source **(field)** | Frame body 0; origin (0, 0, 0); isotropic; range 16 m; initial radiant power P₀ = 251.33 W in PhotonMedium (4π × 20 W/sr); internal occluders = both colliders. Paid by one decaying finite inventory whose half-life T½ and size P₀ · T½ ÷ ln 2 are values bound to the options of `half_life`: 4 s → 251.33 × 4 ÷ 0.6931 = 1,450 J; 8 s → 2,901 J; 16 s → 5,801 J. |
| Parameters | `half_life`: Choice, unit None, options "4 s", "8 s", "16 s", default "8 s", build only; player-editable unless the level locks it (D11). |
| Stores | The decaying inventory (radiation convention 2): each tick emits remaining × (1 − 2^(−dt ÷ T½)), dt the committed tick's duration (1/120 s at the default cadence; optical convention 4), so the remaining inventory is inventory × 2^(−t ÷ T½), the power is P₀ · 2^(−t ÷ T½) with t in game seconds since Run start, and the total emitted never exceeds the inventory. Reset restores the full inventory. |
| Presentation bindings | `Visual/AgeHand` rotation from the committed inventory fraction f (remaining ÷ inventory), 270° at f = 0 to 0° at f = 1: 0° fresh, 135° after one half-life, 202.5° after two, 236.25° after three **(field)**. No glow. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; the radiation domain flag **(field)** |
| Catalogue | Id `decay_clock_capsule`, "Decay capsule", `ElementCategory.Radiation` **(field)**, palette order 717 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A sealed source that halves in strength every half-life. Nothing speeds, slows or stops it." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Capsule` | cylinder r 0.15 m, height 0.40 m, cream `#fff8e9` | solid (capsule cylinder) | nothing |
| `Visual/Base` | box 0.40 × 0.06 × 0.40 m at y −0.23, navy `#293954` | solid (base box) | nothing |
| `Visual/HourglassBand` | navy band printed flat round the middle with an hourglass and one, two or three tick marks for 4, 8, 16 s | decorative: printed flat | nothing |
| `Visual/AgeDial` | disc r 0.08 m printed flat on the top face with three half-life marks at 135°, 202.5° and 236.25° | decorative: printed flat | nothing |
| `Visual/AgeHand` | gold `#f7cb52` line in the dial plane | decorative: moved by a binding | presentation binding |

Icon: the capsule with an hourglass. It differs from the Gamma capsule by the hourglass band and the dial on top, a pattern and not a colour.

### Compiled counts

Bodies 1 (static). Colliders 2 (1 cylinder, 1 box), as RAD-01. Joints 0. Drives 0. Sensors 0. Signal nodes 0. Supply nodes 0. Sources 1. Inventories 1 (decaying; half-life and size bound to `half_life`). Presentation bindings 1.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, RadioactiveDecay, SignalPropagation. The map omits IonizingTransport and GeometryQuery, which an emitting capsule needs; under D14 the probe is the authority and they are listed here. 4 present, 0 untested, 3 narrower, 5 missing. ENGINE-SPATIAL-QUERIES and ENGINE-PARAMETER-BOUND-FRAMES are stories of owner decision 3a, and ENGINE-RUN-LENGTH the story of owner decisions 13 and 17.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | A cylinder collider | present | `ColliderShapeKind.Cylinder` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `WeightTests.TheWeightIsAMassBoundDynamicCylinderWithALoadTieAndNoPayloadRole` (`CuriousContraptions.tests/WeightTests.cs:39`) |
| R3 | Every visible solid has a collider | present | per-package row of the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:206`) |
| R4 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) is for airflow rows only and loops over every collider (`:149`) | — | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| N2 | A declared value bound to the options of a Choice | narrower: `ChoicePredicate` (`CuriousContraptions.Abstractions/AirflowValues.cs:23`) selects airflow input rows, and the `Selection` of `EnergyStoreDescriptor` (`CuriousContraptions.Abstractions/EnergyStoreValues.cs:75`) selects supply energy stores; neither binds a radiation inventory's half-life and size | — | `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:540`). Owner: **ENGINE-PARAMETER-BOUND-FRAMES** (choice-bound values, coordinator decision 16) |
| N3 | Presentation binding on a committed inventory fraction | narrower: `PresentationDriver.CommittedStoredFraction` (`CuriousContraptions.Abstractions/ElementPresentation.cs:33`) reads only a mechanical energy store: the bundle refuses its binding unless the named store is one of the package's `MechanicalPayload.EnergyStores` (`CuriousContraptions.Bundle/BundleValidator.cs:310`–`:312`), the compiled scene checks it against `Physics.EnergyStores` (`engine/gpu/WorkshopCompiledScene.cs:271`–`:273`), and its only package use is the Cannon's charge bar (`elements/Cannon/CannonElementDeclaration.cs:55`). It cannot name a radiation inventory, so the hand needs a new driver, a committed inventory fraction (remaining ÷ declared inventory) of a declared finite inventory, not this one | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner: **ENGINE-RADIATION** (inventory-fraction driver) |
| M1 | A declared emitter and the shared segment and receiver solve | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels and a decaying finite inventory with a half-life in game time, emitting remaining × (1 − 2^(−dt ÷ T½)) each tick | **missing** | — | owner **ENGINE-RADIATION** (its acceptance already says "activity falls with the declared half-life measured in game time") |
| M3 | `ElementCategory.Radiation` and the radiation `SimulationDomain` flag | **missing** | — | owner **ENGINE-RADIATION** |
| M4 | The selected-part Build preview that runs the same solve and commits nothing | **missing** | — | owner **ENGINE-OPTICAL** (optical convention 12; radiation convention 13) |
| M5 | A Run longer than 30 s, its length declared per level up to 300 s | **missing** (the Run is fixed at 30 s × 120 ticks, `CuriousContraptions.Abstractions/CadenceValues.cs:77`) | — | owner **ENGINE-RUN-LENGTH** (owner decisions 13 and 17) |

**Dependencies.** RAD-01 (undelivered, first). For the contact row: RAD-08, two Batteries and the delivered Electrical NOR (`FirstIn`, `SecondIn`, `PowerIn`, `Supply`; `elements/ElectricalNor/ElectricalNorElementDeclaration.cs:25`–`:28`).

## 4. Sources and legacy

- [radiation-13](../requirements.md#radiation-13): "Successive equal half-life intervals halve source strength; a rate contact releases below its threshold, and Reset restores the initial activity and age."
- [Named entry](../invest/named-elements.md#radiation-13), owner S623; map row (S605, S257); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) decay row (S607); research "Decay and material memory" law and slots 98 "Half as Bright", 99 "Catch the Window".
- **Legacy.** No decaying source; the legacy "decay" hits are animation impulse curves (`engine/presentation/AnimationImpulseDefinition.cs`), unrelated. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2040-L2069`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Envelope.** P(t) = 251.33 × 2^(−t ÷ T½) W. Per tick the inventory emits remaining × (1 − 2^(−dt ÷ T½)): for the 8 s preset the first tick emits 2,900.7 × (1 − 2^(−1/960)) = 2,900.7 × 0.000722 = 2.094 J, a mean of 251.24 W over the tick, 0.04 % under P₀. Decay depends only on committed game time: while a Run is paused no tick advances, so it does not age; shielding does not slow it; there is no random draw.
- **Why twice RAD-01.** A fresh capsule 1.5 m from the standard detector reads, by the axis formula, 0.09 × 20 ÷ 1.5² = 0.800 W. With the nine samples: centre 8.889 W/m²; edge d² = 2.26, facing 0.998, 8.830; corner d² = 2.27, facing 0.996, 8.772; mean 8.811; R = 0.09 × 8.811 = 0.793 W. Starting at four times the 0.2 W mark gives more than two half-lives of "on".
- **Halving.** At 1.5 m: 0.793 W fresh, 0.397 W after one half-life, 0.198 W after two, 0.099 W after three.
- **Release.** A default Rate meter turns off below 0.15 W. By the axis formula that is at t = T½ × log2(0.8 ÷ 0.15) = 2.415 T½: 9.66 s, 19.3 s and 38.6 s for the three options. With the nine-sample value it is log2(0.793 ÷ 0.15) = 2.402 T½, a difference of 0.013 T½, inside the ±5 % tolerance.
- **Energy.** The inventory is P₀ × T½ ÷ ln 2: 251.33 × 4 ÷ 0.6931 = 1,450 J, 251.33 × 8 ÷ 0.6931 = 2,901 J and 251.327 × 16 ÷ 0.69315 = 5,801 J (P₀ = 80π W). Half of it leaves in the first half-life. After 60 s the 8 s option has emitted 2,900.7 × (1 − 2^−7.5) = 2,900.7 × 0.99448 = 2,885 J and holds 2,900.7 × 0.00552 = 16.0 J. Emitted plus remaining equals the inventory at every tick, so the total can never exceed it (radiation convention 2) and a spent inventory cannot emit indefinitely: [IX-16](../requirements.md#interaction-16) holds as written.
- **Run length.** The rows need 60 s: three half-lives of the 16 s preset end at 48 s and A6 runs 60 s, both beyond the 30 s default, so the slice needs ENGINE-RUN-LENGTH (owner decisions 13 and 17; at most 300 s). Levels 123 and 124 declare 45 s for the 16 s preset, 6.4 s beyond its 38.6 s release; the 4 s and 8 s presets release at 9.66 s and 19.3 s, inside the 30 s default.
- **Build preview.** ENGINE-OPTICAL's preview (radiation convention 13) shows the fresh activity; it neither advances age nor debits the inventory.
- **Tick order.** The field solve of tick n runs after its last substep, debits that tick's emission from the inventory and commits the reading with the tick (radiation convention 11); a meter's contact follows in the signal network of tick n + 1.

### Rows

Planned class `DecayClockCapsuleTests`. Fixture: the capsule in a production law scene with a standard receiver row 1.5 m away. Readings and energies ±3 %; times ±5 % of the half-life; stall guard 60 s, the Run length.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given each option, when Run reaches one, two and three half-lives (at most 48 s), then the reading is 0.397, 0.198 and 0.099 W: each interval halves it (row: successive equal half-life intervals halve source strength) | `DecayClockCapsuleTests.EachHalfLifeHalvesTheReading` (theory, three options) | RAD-01 stays at its value | ±3 % at each mark |
| A2 | Given Battery 1 `Supply` → a default Rate meter's `PowerIn`, the meter 1.5 m from the capsule, meter `Supply` → Electrical NOR `FirstIn`, Battery 2 `Supply` → NOR `PowerIn`, NOR `Supply` → Powered gate `PowerIn`, then from the first tick after the meter's `detect` is on the NOR's output is off, and when the reading falls the meter's contact opens at 2.415 half-lives and the NOR's output goes live once, supplying the gate (row: a rate contact releases below its threshold; the first tick, before any reading is committed, is outside the observation window) | `DecayClockCapsuleTests.ARateContactReleasesWhenTheSourceHasFaded` (queued until RAD-08 is delivered) | RAD-01 never releases | ±5 % of the half-life |
| A3 | Given a Dense box (0.05 m, 20 /m) on the path, then every reading is e^−1 = 0.368 of A1's at the same times: shielding lowers the reading and does not change the half-life | `DecayClockCapsuleTests.ShieldingDoesNotSlowDecay` | A1 | ratio ±3 % |
| A4 | Given two Runs of the same construction, then the tick at which the reading first falls below 0.15 W is the same | `DecayClockCapsuleTests.DecayIsDeterministic` | A1 | equal tick ordinal |
| A5 | Given a Run paused at tick 600 and resumed, then while paused no tick advances and the committed reading and inventory are unchanged, and after resuming the reading at each tick ordinal equals that of an unpaused Run | `DecayClockCapsuleTests.PauseDoesNotAgeTheSource` | A1 | ±3 % |
| A6 | Given a Run of 60 s with the 8 s option, then the energy emitted, summed over ticks, is 2,885 J, the committed inventory is 16.0 J, and at every tick emitted plus remaining equals the 2,901 J inventory, so the total never exceeds it | `DecayClockCapsuleTests.EmittedEnergyNeverExceedsTheInventory` | A1 | ±3 %; emitted plus remaining within 0.1 % (2.9 J) of the inventory, the ledger invariant, wider than f32 summation error over 7,200 ticks |
| A7 | Given a faded Run, when Reset, then the inventory is full, the next Run reads 0.793 W at its first tick and the age hand is at 0°; a saved and reloaded construction keeps its `half_life` (row: Reset restores the initial activity and age) | `DecayClockCapsuleTests.ResetAndReloadRestoreActivityAndAge`; authored state by R4 | A1 | ±3 % |
| A8 | Given an undefined `half_life` option, then the edit is refused and nothing changes; the declaration has no port | `DecayClockCapsuleTests.OnlyTheThreePresetsAreAdmitted` | each defined option is admitted | typed refusal |
| A9 | Given the Build preview after a faded Run and Reset, then it shows the fresh activity and commits no reading | `DecayClockCapsuleTests.TheBuildPreviewShowsFreshActivity` | A7 | reading 0 |

Chrome recipe (pending, post-catalogue): two Batteries, Decay capsule 1.5 m from a Rate meter, an Electrical NOR and a Powered gate; set the half-life; Run and watch the gate act as the source fades.

## 6. Open questions

None open.

1. **Half-lives and initial activity — decided** (D1, D16): 4, 8, 16 s and 251.33 W, with inventories P₀ · T½ ÷ ln 2 (coordinator decision 21, 22 November 2026).
2. **Map row omits transport and geometry — resolved** (D14): the probe lists them; the map row is corrected with the plan-document updates (see the [batch summary](readiness/batch-o.md)).
3. **Build preview — decided** (D19): fresh activity; the row requires Reset to restore age, and a preview that aged would contradict it.
4. **Run length — decided** (owner decisions 13 and 17): the rows need 60 s; levels 123 and 124 declare 45 s.
5. **Age hand — decided now**: it follows the committed inventory fraction, the presentation driver ENGINE-RADIATION provides for every inventory, with the half-life marks printed at the fractions ½, ¼ and ⅛.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Rate meter RAD-08, dosimeter RAD-09, shields (undelivered); Battery, Electrical NOR, Powered gate (delivered). No port, so no port pair needs to agree.
- **Cross-element tasks.** None names it; processes [IX-16](../requirements.md#interaction-16), [IX-15](../requirements.md#interaction-15) and, through the meter's contact, [IX-07](../requirements.md#interaction-07).
- **Campaign.** Chapter 13, first use 123, then 124 (each declaring a 45 s Run for the 16 s preset); reuse 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity needs S7 to widen it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-PARAMETER-BOUND-FRAMES (the choice-bound inventory); ENGINE-RUN-LENGTH (owner decisions 13 and 17); ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01; RAD-08 for row A2; S7 for the binding. No dependency on ENGINE-THERMAL, as the [ENGINE-RADIATION story](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure) states.
- **Files.** `elements/DecayClockCapsule/DecayClockCapsule.csproj`, `DecayClockCapsuleElementDeclaration.cs`, `decay_clock_capsule.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/DecayClockCapsuleTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** No choice-bound inventory values (the slice stops and ENGINE-PARAMETER-BOUND-FRAMES is amended; no second design is kept here); decay measured in ticks and not game time (D10 forbids it); no inventory-fraction driver for the hand (the slice stops and ENGINE-RADIATION is amended); a maximum Run length under 60 s (re-derive the rows).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** A static body, two colliders, one source row, one decaying inventory whose half-life and size are bound to the `half_life` option, one parameter and one binding. No timer code and no element loop.
2. **Generic laws.** The decaying-inventory law and the field law of the conventions. "The contact releases later" is a receiver level falling below its release mark.
3. **Three layers.** Colliders: a cylinder and a box. Visuals: the art table; band and dial are printed flat. Animation: the age hand, from the committed inventory fraction.
4. **One-way flow and typing.** Game time → inventory → source → readings; nothing flows back. The option is a Choice; power and energy are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** As RAD-01: static, two primitives, one shadow cast per receiver sample point in the scene (the D × K term of radiation convention 12).
7. **Tests are scenarios.** Rows A1 to A9 run the production simulation through placement, Run and the committed read.
