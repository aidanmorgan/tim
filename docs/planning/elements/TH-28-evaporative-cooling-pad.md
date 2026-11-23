# TH-28 · Evaporative cooling pad — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-28 delivery slice](../invest/named-elements.md#thermal-28). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 and probe rows R1 to R14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); water values and rows W1 to W11 by [TH-17](TH-17-kettle.md); liquid conventions L1 to L18 by [EL-001](EL-001-finite-reservoir.md).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-28 · Evaporative cooling pad · Thermal |
| Anchors | [requirements.md#thermal-28](../requirements.md#thermal-28) (task `sequence-task-462`); [named-elements.md#thermal-28](../invest/named-elements.md#thermal-28) |
| Owner key / research | S590 · [TH-S08 evaporative cooling](../../thermal-component-research.md#th-s08) |
| Related identities | Airflow from the delivered [Fan](CAT-028-fan.md); read by a [TH-21 Temperature sensor](TH-21-temperature-sensor.md); contrast [TH-10](TH-10-finned-heat-sink.md), which cannot go below ambient (TX-08). No CAT refines it. |
| Campaign | Intro 73; practice 74; reuse 88, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), then ENGINE-LIQUID, ENGINE-THERMAL (evaporation row, scene humidity and the forced-air sample, one owner) and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (two owners, coined story names, Vacuum energy) and owner decision 19 (game-scale latent heat, which made the earlier rate four times too weak). |

### Disposition

**To deliver as a single element.** Planned package id `evaporative_pad`, directory `elements/EvaporativePad/`, display name "Cooling pad". Not a duplicate of anything delivered.

## 2. Declaration

Follows the delivered Wall for a static box part (`elements/Wall/`) with a liquid store as TH-17 declares one.

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the store, node, phase-row and forced-air sample contracts of ENGINE-LIQUID and ENGINE-THERMAL. `KinematicMode.Static`.
- **Body.** One static body; origin on the bench under the reservoir.
- **Colliders (metres).** `col_reservoir`: box, centre (0, 0.08, 0), half extents (0.25, 0.08, 0.10). `col_pad`: box, centre (0, 0.41, 0), half extents (0.25, 0.25, 0.05); the thermal face. Friction 0.6, restitution 0.05.
- **Liquid store** `reservoir`: capacity 0.00625 m³ (0.1 kg, 104.6 J/K when full; the trough's interior 0.5 × 0.16 × 0.2 m holds 0.016 m³); no mouth; authored by `water`; authored temperature 288 K. It is a thermal node (T2, owner decision 20) with no ambient conductance of its own.
- **Thermal node** `pad`: 42.5 J/K (0.1 kg of cellulose at 425 J/(kg·K)); ambient conductance 1.25 W/K (2.5 W/(m²·K) × the two 0.25 m² faces), with the airflow multiplier of T3; face `col_pad`, 16 W/K. Wetted link `pad`↔`reservoir`: declared 16 W/K × the store's fill fraction (T3, T9).
- **Forced-air sample** (T3, ENGINE-THERMAL): one sample point for `pad` at the pad's face centre (0, 0.41, 0.05), excluding this body's colliders; it reads the committed flow speed u of the delivered airflow law there, takes no force and adds no demand. It is not a second probe kind: it is the one forced-air sample T3 gives a node.
- **Phase row** (T5) declared on `pad`, an evaporation row: liquid leaves `reservoir` at ṁ = 4.6 × 10⁻⁴ kg/s × (1 − φ) × (2.5 + u) ÷ 2.5, never more than the store holds, where φ is the scene's relative humidity (T5: one validated level value, 0 to 1, default 0.5, owned by ENGINE-THERMAL). Each kilogram carries its own enthalpy out of the store and takes L_v(T_pad) = 107.2 × T_pad J (T11; about 30 300 J at 283 K) from `pad`; both go to the world account `Evaporated`. In the Vacuum preset the row is off (T5).
- **Ports.** None.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `water` | Real | m³ | 0.00625 | 0 | 0.00625 | Clamp; build-only |

  Humidity is not a parameter of this part: it is a value of the scene (section 3).
- **Presentation bindings.** `Visual/Stripes` scale Y ← committed fraction of `reservoir` (the wet pattern climbs the wick as far as the fill); `Visual/Level` scale Y ← the same fraction.
- **Palette.** `ElementCategory.Structure`. Order: 533 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Reservoir` cyan `#66b8c9` | box 0.5 × 0.16 × 0.2 | solid, `col_reservoir` | nothing |
| `Visual/Wick` cream `#fff8e9` | box 0.5 × 0.5 × 0.1; its ribs are printed lines | solid, `col_pad` | nothing |
| `Visual/Stripes` navy `#293954` | diagonal hatch printed flush on both wick faces | decorative: printed flat, scaled by a binding | presentation binding |
| `Visual/Level` cream | flat bar printed flush on the reservoir | decorative: printed flat, scaled by a binding | presentation binding |

Vapour is not drawn. Icon: an upright ribbed panel standing in a trough, with a hatch climbing it. It differs from the Wall by the trough and the hatch, and from the Finned heat sink (TH-10, separate fins on a foot) by being one flat panel.

### Compiled counts

Bodies 1 (static); colliders 2 (boxes); joints 0; sensors 0; forced-air samples 1; thermal nodes 2 (pad, and the store); wetted links 1; phase rows 1; liquid stores 1; ports 0; parameters 1; presentation bindings 2.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol or owner | Test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R6 | Static box colliders from art; parameter bounds | present | as TH-04 | as TH-04 (bodies read) |
| E1 | A declared airflow source with a committed flow speed | present, untested here | `AirflowSourceDescriptor`, `CuriousContraptions.Abstractions/AirflowValues.cs:45`; the Fan's 12 m/s, `elements/Fan/FanElementDeclaration.cs:48` | `FanIntegrationTests.DeclaredFlowBindingSamplesQueuedInteriorsAndStopsWithItsActualSource` (`CuriousContraptions.tests/FanIntegrationTests.cs:353`, not opened) |
| E2 | Reading that flow speed at a node's forced-air sample point on a static body, excluding the owner's colliders and taking no force | **missing** | airflow reaches only declared receivers (`IAirflowReceiverElement`, `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:45`) | ENGINE-THERMAL (T3's forced-air sample; sole owner; the earlier "Story 14.0" owner is withdrawn: Story 14.0 is the acoustic engine, [Story 14.0](../../../_bmad-output/planning-artifacts/epics.md#story-140-acoustic-propagation-occlusion--reception-engine-engine-acoustic)) |
| TH-17 W7, W11 | A liquid store with an authored volume; fraction driver | **missing** | ENGINE-LIQUID | — |
| TH-17 W8 | Node, face, wetted link, ambient exchange with the airflow multiplier | **missing** | ENGINE-THERMAL | — |
| E3 | An evaporation row: a declared rate, scaled by humidity and airflow, that moves liquid to a world account and takes latent heat from a node | **missing** | ENGINE-THERMAL (text gap, Batch N summary) | — |
| E4 | A scene relative-humidity value, validated, in level content | **missing** | ENGINE-THERMAL, with the evaporation row (T5; humidity is not a force field: ENGINE-FIELD-FORCE owns only gravity, the one uniform-field law, coordinator decision 16) | — |

Counts: present 2, untested 1, narrower 0, missing 6 (W7, W11, W8, E2, E3, E4). Each has one owner: W7 and W11 ENGINE-LIQUID; W8, E2, E3 and E4 ENGINE-THERMAL. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-28](../requirements.md#thermal-28): wet porous body loses finite liquid by modelled surface mass transfer carrying latent energy; airflow and humidity affect the rate; no variants.
- **Named entry** [thermal-28](../invest/named-elements.md#thermal-28): owner S590; "a dry pad or saturated surrounding gas prevents equivalent cooling; liquid consumption balances vapor output".
- **Research** [TH-S08](../../thermal-component-research.md#th-s08) and per-element table: "wet node + water store"; parameter wetting; "cools airflow while wet"; damp fraction ← water.
- **Element map** row TH-28: CapillaryTransport, EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, LiquidEvaporation, PhaseTopology, SensibleHeat, ThermalConvection, TopologyTransaction (D14).
- **Processes and scenarios.** [IX-23](../requirements.md#interaction-23), [IX-38](../requirements.md#interaction-38), [IX-19](../requirements.md#interaction-19); [TX-09](../requirements.md#thermal-scenario-09) ("evaporation cools a wet surface").
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for evaporat, wick, humidity and pad terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance

Point of truth: [thermal-28](../requirements.md#thermal-28), [TX-09](../requirements.md#thermal-scenario-09) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** The evaporation row above; Q̇ = G·ΔT to ambient (T3); the wetted link by T9's exact step. Rate and ambient exchange carry the same airflow multiplier, so the steady depth below ambient does not depend on the wind: ΔT = L_v × 4.6 × 10⁻⁴ × (1 − φ) ÷ 1.25, with L_v = 107.2 × 282.4 = 30 270 J/kg near the wet pad: 11.14 × (1 − φ) K. At φ = 0.5: 5.57 K, a steady 282.4 K; at φ = 0: 11.1 K, 276.9 K.
- **Why 4.6 × 10⁻⁴ kg/s and 0.1 kg.** Owner decision 19 makes the latent heat of evaporation about 30 300 J/kg, not the earlier 141 062 J/kg, so each joule of cooling evaporates 4.7 times more water. The rate constant is raised to keep the same playable depth (a drop a sensor can threshold), and the reservoir is doubled so that a pad in a Fan's stream still works for over a minute and visibly empties inside a Run.
- **Figures below are a tick-by-tick evaluation** of pad (42.5 J/K), store (104.6 J/K × fill) and the wetted link scaled by fill, with L_v taken at the pad's temperature.
- **Still air, φ = 0.5.** ṁ = 2.3 × 10⁻⁴ kg/s, about 7.0 W of latent heat. After 40 s the pad reads 286.2 K (the full store is cooled with it, so the approach is slow: about 1.8 K below ambient at 40 s, 3.8 K at 120 s).
- **In a Fan's stream, u = 12 m/s.** Multiplier 14.5 ÷ 2.5 = 5.8: ṁ = 1.33 × 10⁻³ kg/s, about 40 W; exchange 7.25 W/K. After 40 s the pad reads 283.0 K and reaches 282.4 K near 60 s; in 30 s the store loses 40 g; the full 0.1 kg lasts 0.1 ÷ 1.334 × 10⁻³ = 75 s, after which the dry pad returns to 288 K within about 15 s.
- **Dry or saturated.** `water` 0 or φ = 1 makes the rate zero: the pad stays at 288 K whatever the wind (TX-08: airflow alone never cools below ambient).
- **Running dry.** `water` 0.000625 m³ (10 g) lasts 7.5 s under the Fan; the dry pad (42.5 J/K, 7.25 W/K, time constant 5.9 s) is back within 1 K of 288 K by 30 s.
- **Energy.** Latent heat taken = fall in the enthalpy of pad and water + heat drawn from ambient; the latent heat and the vapour's own enthalpy go to `Evaporated` (T5, T13); nothing is created. The vapour is not tracked in the room.
- **Vacuum preset.** The row is off and ambient exchange is zero (T3, T5): the pad stays at its temperature and the ambient account stays zero. Decided now (the earlier "cools until dry, latent to ambient" broke "the ambient account stays zero"); no lesson uses it.
- **Tick order.** T8: the row runs in the thermal step and reads the flow speed committed by this tick's airflow pass.
- **Run length these rows need: 60 s** (A5 observes at 60 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances.** T12; mass 15% of the expected loss; liquid as mass on the store's ledger, in grams (T12; 40 g is below L17's 0.004 m³ = 64 g volume tolerance).

### Rows

Planned class `EvaporativePadTests`. "Fan" is a delivered Fan, self-contained and on, 1 m from the pad and facing it.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a full pad in the Fan's stream at φ = 0.5, when 40 s pass, then the pad reads 283.0 K | `AWetPadInAirflowCoolsBelowAmbient` | the same with `water` 0 stays at 288 K | φ = 1 stays at 288 K | ± 2 K; controls within 0.05 K |
| A2 | Given a full pad in still air, when 40 s pass, then it reads 286.2 K, warmer than A1 | `AirflowSpeedsTheCoolingNotItsDepth` | A1 | none | ± 2 K |
| A3 | Given A1 at 30 s, then `reservoir` has lost 40 g and the liquid world account `Evaporated` has gained the same, read as its volume, 0.04 kg ÷ 16 kg/m³ = 0.0025 m³ (liquid L3; it is a volume account); `Drained` and `Spilled` are unchanged and the liquid ledger closes (L10: Σ store volumes + `Spilled` + `Drained` + `Evaporated` = Σ authored initial volumes + `Supplied`) | `LiquidLostEqualsVapourProduced` | dry control: both zero | none | 6 g (15%); the store's loss and `Evaporated` equal within 0.05 g (3.1 × 10⁻⁶ m³) |
| A4 | Given A1, then the pad never reads below 288 − 5.57 K | `ThePadNeverPassesItsWetLimit` | none | φ = 0: limit 276.9 K | 1 K |
| A5 | Given `water` 0.000625 m³ in the Fan's stream, when 60 s pass, then the reservoir is empty and the pad is within 1 K of 288 K | `ADriedPadReturnsToAmbient` | A1 (still at least 4 K below ambient at 60 s) | none | 1 K |
| A6 | Given A1, then latent heat taken = enthalpy fall of pad and water + ambient account inflow | `LatentHeatIsPaidByThePadAndTheRoom` | none | none | 2% or 5 J |
| A7 | Reset restores `water`, 288 K and a zero `Evaporated` account; a second Run repeats A1 | `ResetRestoresWaterAndTemperature` | none | none | as A1 |
| A8 | Save and load keep pose and `water`; no run state | `SaveAndLoadKeepTheFill` | none | none | exact |
| A9 | `water` of −1 or 0.007 is clamped | generic: TH-04 R6 | — | 0 and 0.00625 admitted | exact |
| A10 | Given A1 in the Vacuum atmosphere preset, then no mass leaves, the pad stays at 288 K and the ambient account stays zero | `NoEvaporationInVacuum` | A1 | none | exact zero mass; 0.05 K |

There is no port, so no wrong-kind connection row. Cooling a body that touches the pad is a row in TH-21's slice (the probe reads the pad).

## 6. Open questions

Resolved:

1. **Humidity model.** One relative-humidity value on the scene, owned by ENGINE-THERMAL with the evaporation row (T5; decided now: the simplest general form; it needs no vapour inventory for the room and gives the saturated control). A sealed gas store's vapour component (TH-17 W5) is the general form for enclosed spaces and is not needed here.
2. **Mass-transfer law.** Linear in (1 − φ) with the convection multiplier (decided now: the depth is then a declared constant, which a lesson can rely on).
3. **Capillary feed.** None: the row draws on the reservoir directly (D19; a separate wick inventory adds a rate no lesson uses).
4. **Cooling another body.** The pad cools itself and what touches it, not the air stream (decided now: under the scale an air stream would cool by about 0.01 K).
5. **Refill.** None; authored fill only (D13).
6. **Game scale.** D5, TH-04 section 11.

Needs the owner: none.

## 7. Integration

- **Works with.** Fan (delivered); TH-21 (reads the pad); TH-10 (TX-08 comparison).
- **Campaign.** "Dew Process": introduction 73, practice 74; reuse 88, 145. A level sets the scene humidity; a fixed pad needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme; ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (E2, E3, E4; owner decision 18); ENGINE-RUN-LENGTH (A5); TH-04; S7.
- **Files.** `elements/EvaporativePad/EvaporativePad.csproj`, `EvaporativePadElementDeclaration.cs`, `evaporative_pad.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/EvaporativePadTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it.
- **Review tier.** Engine if it is the first user of the evaporation row or the forced-air sample; otherwise data-only.
- **Disqualifiers.** An airflow multiplier applied to ambient exchange but not to the row (the depth would then depend on wind: re-derive); no scene humidity; a forced-air sample that needs a dynamic body or reads the owner's own colliders; a latent heat other than T11's L_v(T) (re-derive the rate constant).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Two static colliders, one liquid store, one node, one wetted link, one phase row, one forced-air sample, one parameter, two bindings.
2. **Generic laws.** The evaporation row and ambient exchange. "Cools when blown on" is the row's rate rising with a committed flow speed.
3. **Three layers.** Colliders: reservoir, pad. Visuals: their art; printed hatch and level bar. Animation: two bindings from the committed store fraction.
4. **Flow and typing.** No ports; f32 values; no string in domain logic.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static; two primitive colliders; two nodes, one row and one forced-air sample per tick; the sample is a bounded point query, not a scene scan (the delivered airflow pass's scene-proportional occlusion sweep is owned by the engine programme, owner decision 3).
7. **Tests are scenarios.** A1 to A8.

**Verdict: Conforms; blocked on ENGINE-LIQUID and ENGINE-THERMAL, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Not written as main's binding section. **Blocked on the coverage follow-on story S7:** main's reader (`tools/Coverage/NamedPackageSources.cs`) lists only `EL-*.md` files and admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `evaporative_pad` |
| Named identity | TH-28, `../invest/named-elements.md#thermal-28` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-462` and the ledger entry has no todo link |
| Decision | S590; `scope-corrections.md` on published main has no `s590` block |
| Mode records | `Configuration=Fixed` |
