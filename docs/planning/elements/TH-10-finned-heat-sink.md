# TH-10 · Finned heat sink — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-10 delivery slice](../invest/named-elements.md#thermal-10). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Units, laws, scale and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); probe rows "TH-04 R<n>" and "TH-08 C<n>" are in [TH-04 section 3](TH-04-electrical-heating-plate.md#3-engine-capabilities) and [TH-08 section 3](TH-08-heat-conducting-bar.md#3-engine-capabilities).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-10 · Finned heat sink · Thermal |
| Anchors | [requirements.md#thermal-10](../requirements.md#thermal-10) (task `sequence-task-444`); [named-elements.md#thermal-10](../invest/named-elements.md#thermal-10) |
| Owner key / research | S572 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | Airflow from the delivered Fan ([CAT-028](CAT-028-fan.md), TX-08); heat rejection for [TH-12](TH-12-reversible-heat-pump.md) and [TH-30](TH-30-thermoelectric-generator.md); follows [TH-05](TH-05-friction-brake.md) in the campaign. Conductors lead: [TH-08](TH-08-heat-conducting-bar.md). No CAT refines it. |
| Campaign | Intro 39 (moved from the chapter 4 finale by coordinator decision 35); practice 41; reuse 78, 87, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES (static pairs and the flow sample); then ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (N3: a static sink; the airflow sample inside its own fin; one sample owner; Run length) and coordinator decision 21. |

### Disposition

**To deliver as a single element** after TH-04. Planned package id `heat_sink`, directory `elements/HeatSink/`, display name "Finned heat sink". Not a duplicate and not a Family member of TH-08: one node, six colliders and a forced-air term.

## 2. Declaration

Follows the delivered Wall shape for a static part with no ports (`elements/Wall/WallElementDeclaration.cs:11-13`), with colliders baked from its scene, as the Heating plate (TH-04).

- **Interfaces.** `IMechanicalElement`, `ISceneGeometryElement`, and the thermal-node contract of ENGINE-THERMAL.
- **Body.** One static body (`KinematicMode.Static`, zero static mass frame), origin at the centre of the foot's underside. The player places it with its foot touching the face it cools: on the Heating plate's disc, on a heat pump face, against a generator's cold plate (static-on-static pairs resolved once at Run, T3's list). Static because thermal links come from touch (coordinator decision 21; audit N3); the earlier dynamic sink could not hang under a pump face and added a loose six-collider body to every thermal level.
- **Colliders (metres).**

  | Collider | Centre | Half extents | Purpose |
  | --- | --- | --- | --- |
  | `col_foot` | (0, 0.03, 0) | (0.25, 0.03, 0.25) | the contact foot; the thermal face |
  | `col_fin_0` to `col_fin_4` | (−0.23, −0.115, 0, 0.115, 0.23; 0.28; 0) | (0.02, 0.22, 0.25) | five fins, 0.075 m apart; no thermal face |

  Six boxes against the Receiver's five: fins with visible gaps must each be solid, and one block over the gaps would reach beyond the art (TH-04 R1).
- **Material (f32).** Friction 0.6, restitution 0.05, rolling resistance 0.
- **Thermal node.** `sink`: heat capacity 56.25 J/K (0.25 kg of aluminium at 225 J/(kg·K), declared); face `col_foot`, 64 W/K (metal); ambient conductance 5.5 W/K, which is the declared wetted fin area 5 × 2 × 0.44 × 0.5 = 2.2 m² × 2.5 W/(m²·K); one forced-air sample point at (0.0575, 0.28, 0), in the open channel between `col_fin_2` (x from −0.02 to 0.02) and `col_fin_3` (x from 0.095 to 0.135), for the forced-air multiplier of T3; the sample excludes this body's own colliders (T3), so the fins never shadow their own sample (the earlier point (0, 0.28, 0) lay inside `col_fin_2`). The channels run along Z: a Fan cools the sink when its stream runs along ±Z through the fins (or down −Y onto them); a stream along X meets the outer fin face-on.
- **Parameters.** None (D13). **Ports.** None. **Stores.** None beyond the node.
- **Presentation bindings.** `Visual/Foot` emission ← committed temperature of `sink`, 288 K to 400 K mapped 0 to 1 (driver of TH-04 R12).
- **Palette.** Category `ElementCategory.Structure`. Order: 512 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Foot` gold `#f7cb52` | box 0.5 × 0.06 × 0.5 m | solid, `col_foot` | the body pose |
| `Visual/Fin_0` to `Fin_4` cream `#fff8e9` | five boxes 0.04 × 0.44 × 0.5 m | solid, `col_fin_0` to `col_fin_4` | the body pose |

Icon: five upright bars on a base. At a glance it differs from the Insulating panel (a flat layered slab) and the Thermal storage block (a plain prism with marks) by its comb of fins.

### Compiled counts

Bodies 1 (static); colliders 6 (box); joints 0; drives 0; sensors 0; thermal nodes 1; faces 1; airflow sample points 1; ports 0; stores 0; presentation bindings 1.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test (body read) |
| --- | --- | --- | --- | --- |
| TH-04 R1 | Box colliders from art, visible = solid | present | as TH-04 | as TH-04 |
| F1 | A declared airflow source commits a flow speed, zero when off and in the Vacuum preset | present | `AirflowSourceDescriptor`, `CuriousContraptions.Abstractions/AirflowValues.cs:45`; the Fan declares 12 m/s (`elements/Fan/FanElementDeclaration.cs:48`) | `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`CuriousContraptions.tests/FanIntegrationTests.cs:514`) |
| F2 | A solid in the stream occludes it | present, test not opened | the airflow occlusion sweep | `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream` (`CuriousContraptions.tests/FanGuidanceTests.cs:45`), not opened |
| F3 | The flow speed of the airflow law sampled at a declared point of a body, taking no force, to scale that body's ambient conductance | **missing** | airflow reaches only declared receivers, as force (`AirflowReceiverDescriptor`, `CuriousContraptions.Abstractions/AirflowValues.cs:50`) | none; owner ENGINE-THERMAL |
| TH-04 R9, R10, R11, R12 | Node, contact conduction, ambient exchange with the atmosphere preset, temperature driver | **missing** | as TH-04 | none; owner ENGINE-THERMAL |

Counts: present 2, untested 1, narrower 0, missing 5. All five missing rows have one owner, **ENGINE-THERMAL**; F3 is stated in T3 and is not yet in the story's text (proposed text in the [Batch N summary](readiness/batch-n.md)). ENGINE-THERMAL is the sole owner of the forced-air sample (T3); the earlier pointer to an observation-only probe under Story 14.0 (the acoustic engine) is withdrawn, so there is one sampler and one owner. R10 here covers static pairs only (the foot on a disc, pump face or cold plate). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-10](../requirements.md#thermal-10): exposed area and surface/environment exchange reject heat toward an explicitly modelled ambient reservoir; no variants.
- **Named entry** [thermal-10](../invest/named-elements.md#thermal-10): owner S572; "An equal-temperature environment gives no net cooling; passive convection cannot cool below ambient".
- **Research** per-element table: "high-conductance link to environment + airflow scaling"; "cools faster under a Fan".
- **Element map** row TH-10: FiniteLedger, FluidAdvection, GeometryQuery, SensibleHeat, ThermalConduction, ThermalConvection, TopologyTransaction. Navigation only (D14); nothing here changes topology.
- **Processes and scenarios.** [IX-19](../requirements.md#interaction-19), [IX-18](../requirements.md#interaction-18), [IX-08](../requirements.md#interaction-08); [TX-08](../requirements.md#thermal-scenario-08).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for sink, fin, convect and heat terms.

## 5. Acceptance

Point of truth: [thermal-10](../requirements.md#thermal-10), [TX-08](../requirements.md#thermal-scenario-08) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** A contact link at the foot; Q̇ = G_amb·(T − 288 K) to the ambient account, with G_amb multiplied by (2.5 + u) ÷ 2.5 under a flow of speed u (T3). The flow is toward ambient temperature only, so the law cannot cool below 288 K and gives nothing at 288 K.
- **On a supplied Heating plate** (T14; foot on disc, metal on metal, 32 W/K). θ is the rise above 288 K.
  - Still air: 32·(θ_d − θ_s) = 5.5·θ_s gives θ_d = 1.172·θ_s; 120 = 0.5 × 1.172·θ_s + 5.5·θ_s = 6.086·θ_s, so the sink settles at 307.7 K and the disc at 311.1 K, against 528 K for a bare plate.
  - Under the Fan's 12 m/s: G_amb = 5.5 × 14.5 ÷ 2.5 = 31.9 W/K; θ_d = 1.997·θ_s; 120 = 32.9·θ_s, so the sink settles at 291.6 K and the disc at 295.3 K, 15.8 K cooler than in still air.
  - Time constant in still air (15.25 + 56.25) ÷ 6.0 = 12 s, so 45 s is past three time constants.
- **After the supply ends.** Battery `capacity` 1200 J: heating stops at 10 s; both nodes fall toward 288 K with the 12 s time constant and are within 0.2 K of it 60 s later (23 × e^(−5)); neither goes below it.
- **Vacuum preset.** G_amb is zero for every node, so 45 s of supply (5400 J) only warms the two nodes: 5400 ÷ 71.5 = 75.5 K, to about 364 K, and the ambient account stays at zero.
- **Why 5.5 W/K.** Eleven times the plate's own 0.5 W/K, so the sink visibly holds a 120 W source near ambient, and a Fan visibly improves on it.
- **Stays put.** It is static; it declares no airflow receiver, so a stream exerts no force on it.
- **Tick order.** T8: the flow speed the airflow phase committed in tick N scales the ambient term of tick N's thermal step. **Tolerances.** T12. Rows are fixed-time observations.
- **Run length these rows need: 70 s** (A4 observes for 70 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `HeatSinkTests`. Rows A1, A2 and A6 use a Battery with `capacity` 7200 J.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the sink standing on a supplied Heating plate, when 45 s pass, then the disc reads 311 K | `ASinkHoldsASuppliedPlateNearAmbient` | a bare plate reads above 450 K at 45 s (288 + 240 × (1 − e^(−1.475)) = 473 K) | none | ± 3 K |
| A2 | Given A1 with a running Fan 2 m away on the sink's Z axis, blowing along the fin channels, when 45 s pass, then the disc is at least 8 K cooler than in A1; with a Wall between Fan and fins it is within 3 K of A1 | `AFanCoolsTheSinkFasterAndAWallUndoesIt` | A1 | none | 8 K margin against a calculated 15.8 K |
| A3 | Given an unsupplied plate and sink at 288 K, with and without the Fan, when 30 s pass, then no node has moved and the ambient account is zero | `AnEqualTemperatureEnvironmentGivesNoCooling` | A1 | none | 0.01 K; 0.01 J |
| A4 | Given Battery `capacity` 1200 J and the Fan running, when 70 s pass, then every node has stayed at or above 288 K throughout and ends within 2 K of it | `ConvectionNeverCoolsBelowAmbient` | none | the minimum temperature seen | not below 287.99 K |
| A5 | Given A1 at 45 s, then the Battery's debit equals the heat stored in the two nodes plus the ambient account | `RejectedHeatReachesTheAmbientAccount` | none | none | 2% of the debit |
| A6 | Given A1 in the Vacuum preset, when 45 s pass, then the ambient account is zero and the disc is above 350 K | `InAVacuumTheSinkOnlyStoresHeat` | A1 | the preset boundary | 0.01 J; disc as stated |
| A7 | Given A2 then Reset, then both nodes are at 288 K, the sink is at its authored pose and a second Run repeats A2 | `ResetRestoresTheSinkAndItsNode` | none | none | as A2 |
| A8 | Given a placed sink, when saved and loaded, then it and its pose are present and no run temperature is saved | `SaveAndLoadKeepThePoseAndNoRunState` | none | none | exact |

No port and no parameter, so no wrong-kind or parameter-refusal row. The requirement's clauses are A3 (equal temperature) and A4 (never below ambient).

## 6. Open questions

Resolved:

1. **Game scale.** D5, restated once in TH-04 section 11.
2. **Convection law.** The linear (2.5 + u) rule of T3, a starting value (D1); ENGINE-THERMAL records it once for every consumer.
3. **Ambient reservoir.** One world account at 288 K (T3, T13), decided in the conventions.
4. **TopologyTransaction in the map row.** Not used (D14).
5. **Static or dynamic.** Static (coordinator decision 21; audit N3): two static parts placed touching conduct through the link table, so the sink can stand on, beside or above what it cools.
6. **Fin count.** Five solid fins (visible = solid); the wetted area is a declared value, not derived from the colliders.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04 (planned before it); Fan, Wall and Battery (delivered); TH-05 (lesson 39 precedes it), TH-26, TH-12 and TH-30 (later rows in their slices).
- **Campaign.** "A Warm Reception": introduction 39 (moved from the chapter 4 finale 40 by coordinator decision 35), practice 41; reuse 78, 87, 145. Loose cargo; fixed partners need the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the static pairs and the flow sample); ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); ENGINE-RUN-LENGTH; TH-04; S7; S3 preferred.
- **Files.** `elements/HeatSink/HeatSink.csproj`, `HeatSinkElementDeclaration.cs`, `heat_sink.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/HeatSinkTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if the probe finds every row present and tested; otherwise the engine tier.
- **Disqualifiers.** ENGINE-THERMAL delivered without the forced-air term (rows A2 and the Fan half of A3 and A4 then wait for it); a flow sample that needs a force-taking receiver or reads its owner's colliders; a different convection rule (re-derive section 5).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** One static body, six boxes, one node with one face and one flow sample, one binding. No update loop or engine branch on this part.
2. **Generic laws.** Q̇ = G·ΔT at the foot and to ambient; one multiplier from the delivered airflow law. "Cools faster under a Fan" is that multiplier.
3. **Three layers.** Colliders: foot and five fins. Visuals: the same six boxes as art, each with its collider. Animation: foot glow from committed temperature; nothing is written back.
4. **Flow and typing.** No ports; f32 values; the atmosphere is the `AtmospherePreset` enum.
5. **Flat state.** The counts in section 2.
6. **Performance.** Six primitive colliders, the count justified in section 2; the flow sample is one bounded query per tick (T10); nothing scans the scene. The delivered airflow pass itself tests every source against every sample and collider; that cost is the engine programme's (ENGINE-SPATIAL-QUERIES), not designed around here. Static, it adds no solver body.
7. **Tests are scenarios.** A1 to A8 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `heat_sink` |
| Named identity | TH-10, `../invest/named-elements.md#thermal-10` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-444` and the ledger entry has no todo link |
| Decision | S572; `scope-corrections.md` on published main has no `s572` block |
| Mode records | `Configuration=Fixed` |
