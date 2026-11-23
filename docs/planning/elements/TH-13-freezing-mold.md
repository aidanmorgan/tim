# TH-13 · Freezing mold — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-13 delivery slice](../invest/named-elements.md#thermal-13). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (section 3 cold-pack chain; TH-13 and ENGINE-FRACTURE; poured water) and owner decisions 13, 17 and 20 and coordinator decision 21 of 22 November 2026, and on 23 November 2026 against the second audit `docs/planning/elements/readiness/audits/reaudit-group-3.md` (the charge's ambient link, A5's ball). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation (the pack chain a tick-by-tick evaluation of the exact two-node step, T9) and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); ice values and probe rows I1 to I6 by [TH-14](TH-14-ice-block.md); liquid conventions L1 to L18 by [EL-001](EL-001-finite-reservoir.md).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-13 · Freezing mold · Thermal |
| Anchors | [requirements.md#thermal-13](../requirements.md#thermal-13) (task `sequence-task-447`); [named-elements.md#thermal-13](../invest/named-elements.md#thermal-13) |
| Owner key / research | S577 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | Cold sources [TH-12 Reversible heat pump](TH-12-reversible-heat-pump.md) and [TH-29 Cold pack](TH-29-cold-pack.md) (TX-02); the same water material as [TH-14 Ice block](TH-14-ice-block.md); water from the water family (Batches F and G). No CAT refines it. |
| Campaign | Intro 79; practice 80; reuse 89, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS (the touch link to a pack beneath it); then ENGINE-LIQUID, ENGINE-THERMAL and ENGINE-RUN-LENGTH. |

### Disposition

**To deliver as a single element, after TH-14 and TH-29.** Planned package id `freezing_mold`, directory `elements/FreezingMold/`, display name "Freezing mold". Not a duplicate of anything delivered. It contains no cold source.

## 2. Declaration

Follows the delivered Receiver, a static open box of five walls (`elements/Receiver/ReceiverElementDeclaration.cs:8`, `:68`), whose walls are five box collision shapes of its scene (`elements/Receiver/basket.tscn:77` onward; its `Colliders` list is empty, `ReceiverElementDeclaration.cs:57`). This part declares its colliders in its package instead, as the Heating plate does (TH-04).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the store, node and phase-row contracts of ENGINE-LIQUID and ENGINE-THERMAL. `KinematicMode.Static`.
- **Body.** One static body; origin at the centre of the base's underside. It is placed on the bench, on a heat pump face or on top of a Cold pack (each a touch pair, T3).
- **Colliders (metres).**

  | Collider | Centre | Half extents | Purpose |
  | --- | --- | --- | --- |
  | `col_base` | (0, 0.02, 0) | (0.22, 0.02, 0.22) | tray floor; the thermal face |
  | `col_wall_px`, `col_wall_nx` | (±0.20, 0.08, 0) | (0.02, 0.04, 0.22) | side walls |
  | `col_wall_pz`, `col_wall_nz` | (0, 0.08, ±0.20) | (0.18, 0.04, 0.02) | end walls |
  | `col_ice_0` to `col_ice_9` | (0, 0.044 + 0.008·k, 0) | (0.18, 0.004, 0.18) | ice layer k (0.008 m thick, 0.0010368 m³); active while the solid amount is at least (k + 1) × 0.0010368 m³ |

  Cavity 0.36 × 0.08 × 0.36 m: plan area 0.1296 m², capacity 0.010368 m³, ten layers of 0.008 m.
- **Material (f32).** Friction 0.4, restitution 0.05, rolling resistance 0; ice layers friction 0.05.
- **Liquid store** `charge` (L4): capacity 0.010368 m³, plan area 0.1296 m², an open top that captures a falling stream (L8), no mouth; authored volume from `water`; authored temperature 288 K. It is a thermal node (T2, owner decision 20), with no ambient conductance of its own (the room reaches it through the tray).
- **Thermal node** `tray`: 67.5 J/K (0.3 kg of aluminium at 225 J/(kg·K)); ambient conductance 0.4 W/K; face `col_base`, 64 W/K. Wetted link `tray`↔`charge`: declared 53 W/K × the store's fill fraction (T3, T9); at the default fill 0.003125 ÷ 0.010368 = 0.301 it is 16.0 W/K.
- **Phase row** (T5) on `charge`: freeze and melt at 273 K; latent heat 20 875 J/kg × the store's mass; committed solid fraction f and solid amount f × volume. Liquid specific heat 1046, solid 525 J/(kg·K) (T11). The row has no heat source or sink of its own.
- **Ports.** None.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `water` | Real | m³ | 0.003125 | 0 | 0.010368 | Clamp; build-only |

- **Presentation bindings.** `Visual/Front` (the cyan phase window) scale Y ← f; `Visual/Ice_0` to `Visual/Ice_9` shown while their colliders are active (ten visibility bindings, one per layer).
- **Palette.** `ElementCategory.Structure`. Order: 538 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base`, four `Visual/Wall_*` cream `#fff8e9` | boxes as the colliders | solid, `col_base`, `col_wall_*` | nothing |
| `Visual/Ice_0` to `Visual/Ice_9` frosted cyan `#66b8c9` | boxes 0.36 × 0.008 × 0.36 m stacked on the tray floor | solid, `col_ice_k`; each layer's art and collider appear together | shown by a binding |
| `Visual/Front` cyan | flat bar printed flush on the front wall, rising with f | decorative: printed flat, scaled by a binding | presentation binding |
| liquid surface | the liquid story's shared store presentation; a part-frozen remainder thinner than one layer (under 0.008 m) is drawn as that flat surface | decorative: not this package's art, flush within one layer | — |

Icon: an open square tray with a rising bar on its front. It differs from the Receiver (a goal box with a halo) by the low square shape and the front bar.

### Compiled counts

Bodies 1 (static); colliders 15 (boxes: base, four walls, ten ice layers, of which at most the layers the charge fills are ever active); joints 0; sensors 0; thermal nodes 2 (tray, and the store); wetted links 1; phase rows 1; liquid stores 1; ports 0; parameters 1; presentation bindings 11.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol or owner | Test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R6 | Static box colliders from art; parameter bounds | present | as TH-04 | as TH-04 (bodies read) |
| TH-04 R10 | A touch link between the static base and a pack or pump face | **missing** | none | ENGINE-THERMAL with ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS (T3) |
| M2 | A liquid store with an authored volume and an open top that captures a stream | **missing** | none | ENGINE-LIQUID (L4, L8) |
| TH-14 I2 | Thermal node, face, ambient, and a wetted link to a liquid store scaled by fill | **missing** | none | ENGINE-THERMAL |
| M3 | A freeze and melt row on a liquid store, with a committed solid fraction and solid amount | **missing** | none | ENGINE-THERMAL ("phase thresholds (melt, freeze …) as conserved transfers") |
| TH-14 I5 | A collider and its art active over a declared interval of a committed phase scalar | **missing** | none | ENGINE-THERMAL (T5) |
| TH-04 R12 | Phase-fraction presentation driver | **missing** | none | ENGINE-THERMAL |

Counts: present 2, untested 0, narrower 0, missing 6. Each has one owner; ENGINE-FRACTURE is not one (the audit found no row-activation rule in that story's text, [Story ENGINE-FRACTURE](../../../_bmad-output/planning-artifacts/epics.md#story-engine-fracture-load-limited-connections-and-irreversible-break), and T5 gives activation intervals to ENGINE-THERMAL alone). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-13](../requirements.md#thermal-13): container defines the solid's geometry and provides heat-transfer surfaces; no hidden cold source; no variants.
- **Named entry** [thermal-13](../invest/named-elements.md#thermal-13): owner S577; "insufficient energy removal leaves a measured liquid/solid mixture; equal-temperature surroundings cannot freeze it".
- **Research** per-element table: "node + phase transition"; parameter volume; "water freezes into a solid cargo when cooled"; ice fraction ← f.
- **Element map** row TH-13: EnvironmentState, FiniteLedger, GasState, GeometryQuery, LiquidFreezing, PhaseTopology, SensibleHeat, ThermalConduction, TopologyTransaction (D14; freezing needs no gas).
- **Processes and scenarios.** [IX-22](../requirements.md#interaction-22), [IX-41](../requirements.md#interaction-41), [IX-18](../requirements.md#interaction-18); [TX-02](../requirements.md#thermal-scenario-02).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for freez, mold, ice and phase terms. The legacy gas material explicitly excluded phase transitions (`engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6`); recorded only.

## 5. Acceptance

Point of truth: [thermal-13](../requirements.md#thermal-13), [TX-02](../requirements.md#thermal-scenario-02) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Q̇ = G·ΔT on the base contact, the wetted link and ambient (T3), each by the exact two-node step (T9); the freeze row (T5).
- **Heat to remove** (default charge, 0.003125 m³ = 0.05 kg, 52.3 J/K as liquid). Water 288 → 273 K: 52.3 × 15 = 785 J. Latent: 0.05 × 20 875 = 1044 J. The tray is not held at 273 K: it is pulled below it by the sink, so the heat a sink receives is 785 J + 1044 J × f + 67.5 J/K × (288 K − the tray's temperature) + the room's inflow into the tray (0.4 W/K; the charge has no ambient link of its own).
- **On a default Cold pack** (TH-29: 437.5 J/K authored at 255 K, 1.2 W/K to the room, face 16 W/K; pair with the base 64 × 16 ÷ 80 = 12.8 W/K). Evaluated tick by tick with T9's exact step: the water reaches 273 K and starts to freeze at about 9 s; **f = 1 at about 25 s** (24.6 s), with the tray at 269.2 K and the pack at about 264 K. The pack has then taken about 3.26 kJ from the mold (785 + 1044 + 67.5 × 18.8 = 3098 J, plus 165 J the tray took from the room) and 0.80 kJ from the room. Left in place, the pack goes on warming (267.6 K at 60 s, 271 K at 120 s) and the ice starts to thaw again near 120 s (f = 0.91 at 180 s).
- **Warmer packs** (same evaluation): authored at 260 K, f reaches 1 at about 43 s; at 262 K f peaks at 0.83 near 60 s and falls again; at 263 K f is 0.59 at 60 s and peaks at 0.60; at 265 K f peaks at 0.20. **A pack authored at 262 K or warmer cannot complete the freeze.**
- **Insufficient removal.** A sink that stops when f = 0.5 leaves a measured mixture at 273 K.
- **Equal temperature.** With no colder body in contact every link has ΔT = 0: f stays 0.
- **Thaw.** Left alone at f = 1 the room melts the charge again: 1044 J at about 0.4 × 15 = 6 W, about 174 s. Freezing is reversible; nothing is latched.
- **The solid.** Ice forms in layers from the cold floor up: layer k is active while f × volume ≥ (k + 1) × 0.0010368 m³. The default charge (0.003125 m³, 0.0241 m deep) fills three layers (0.024 m, active at f ≥ 0.332, 0.664 and 0.995), on which a ball rests instead of reaching the floor. Water poured in during a Run freezes in the same layers, because activation reads the store's solid amount, not the authored parameter. The frozen charge stays in the tray; it is not a TH-14 body.
- **Mass.** The store's volume does not change when it freezes (one density, D2).
- **Tick order.** T8: f commits with the tick; a layer switches at the following step boundary (T5), and a layer that switches off wakes the islands it supported.
- **Run length these rows need: 120 s** (A2 and A4 observe at 120 s; A1's stall guard is the smaller of 3 × 25 and 25 + 60 s, 75 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances.** T12; f ± 0.08; energy 2% or 5 J; liquid as mass on the store's ledger, in grams.

### Rows

Planned class `FreezingMoldTests`. "Sink" is a TH-29 Cold pack under the base unless stated.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a default mold placed on a default Cold pack (255 K), then f reaches 1 and the water is at or below 273 K | `AColdSinkFreezesTheCharge` | A2 | an empty mold (`water` 0) forms nothing and no ice layer activates | stall guard 75 s |
| A2 | Given a mold on the bench with no colder body, then f is 0 and the water is at 288 K after 120 s | `EqualTemperatureSurroundingsFreezeNothing` | A1 | none | f exactly 0; ± 0.05 K |
| A3 | Given A1 at the tick f reaches 1, then heat received by the sink from the mold = 785 J + 1044 J × f + 67.5 J/K × (288 K − tray temperature) + the room's inflow into the tray | `HeatRemovedEqualsSensiblePlusLatent` | none | none | 2% or 5 J |
| A4 | Given a Cold pack with `initial_temperature` 263 K, then at 60 s f is between 0.15 and 0.9, and f never reaches 1 in 120 s | `InsufficientRemovalLeavesAMixture` | A1 | none | the stated band; water within 2 K of 273 K while 0 < f < 1 |
| A5 | Given A1 and a Tennis ball with `radius` 0.10 m (admitted from 1/16 m, `elements/TennisBall/TennisBallElementDeclaration.cs:18`; the default 0.25 m ball cannot enter the 0.36 m cavity) dropped into the tray, then it rests on the ice 0.024 m above the floor; in A2 it reaches the floor | `TheFrozenChargeIsSolid` | A2 | none | 0.01 m |
| A6 | Given a tap pouring 0.003125 m³ into an empty mold on a default Cold pack, then the store holds 50 g and three ice layers activate as in A1 | `PouredWaterFreezesInLayers` (after EL-003) | none | none | 2 g; layer count exact |
| A7 | Reset restores liquid at 288 K, f = 0, no active layer and the authored volume; a second Run repeats A1 | `ResetRestoresLiquidWater` | none | none | as A1 |
| A8 | Save and load keep pose and `water`; no run state | `SaveAndLoadKeepTheFill` | none | none | exact |
| A9 | `water` of −1 or 0.02 is clamped | generic: TH-04 R6 | — | 0 and 0.010368 admitted | exact |

There is no port, so no wrong-kind connection row. The TX-02 rows with the heat pump run in TH-12's slice.

## 6. Open questions

Resolved:

1. **Release.** The frozen charge is not a separate body (decided now, D19: the row asks for a container that shapes the solid; turning a store into a rigid body is a topology change no lesson uses).
2. **Fill source.** Both: authored `water` and water poured in through the open top (D3, D4).
3. **Ice colliders.** Ten fixed 0.008 m layers activated by the solid amount (decided now, following TH-14's layers and the audit): a collider sized from the authored parameter left poured water with no solid. The cavity is 0.08 m deep so ten layers cover it.
4. **Wetted conductance.** Declared 53 W/K, scaled by fill (coordinator decision 21), so a default charge sees 16 W/K.
5. **GasState in the map row.** Not needed (D14).
6. **Game scale.** D5, TH-04 section 11.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-29, TH-12 (sinks); TH-10 on the pump's hot side; the water family's taps (EL-003) for a poured fill; Tennis ball (delivered).
- **Campaign.** "Freeze a Jolly Good Fellow": introduction 79, practice 80; reuse 89, 149. A lesson with a Cold pack uses the default 255 K pack; a fixed mold needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS); ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (M3, I5; owner decision 18); ENGINE-RUN-LENGTH; TH-29 (the test sink); TH-14; S7.
- **Files.** `elements/FreezingMold/FreezingMold.csproj`, `FreezingMoldElementDeclaration.cs`, `freezing_mold.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/FreezingMoldTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it.
- **Review tier.** Engine if it is the first user of a phase row on a liquid store; otherwise data-only.
- **Disqualifiers.** A liquid store that cannot carry a solid amount; activation that reads only a fraction (the layers would then follow f of whatever fill is present, and a part-filled mold would show ice above the water); a TH-29 pack too small to freeze one charge (then A1 uses TH-12).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Fifteen static colliders, one liquid store, one node, one wetted link, one phase row, one parameter, eleven bindings. No cold source, no update loop.
2. **Generic laws.** Conduction and the freeze row. "Freezes when cooled" is heat leaving a store through declared links until its latent amount is gone.
3. **Three layers.** Colliders: base, four walls, ten ice layers. Visuals: their art, each ice layer shown exactly while its collider is active; a printed front bar. Animation: the bar and the layers' visibility from committed phase scalars.
4. **Flow and typing.** No ports; f32; no string in domain logic.
5. **Flat state.** The counts in section 2; the ice rows are compiled and inactive until reached.
6. **Performance.** Static; fifteen boxes, at most the filled layers active; one node, one store, one link, one row per tick.
7. **Tests are scenarios.** A1 to A8.

**Verdict: Conforms; blocked on ENGINE-LIQUID and ENGINE-THERMAL, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Not written as main's binding section. **Blocked on the coverage follow-on story S7:** main's reader (`tools/Coverage/NamedPackageSources.cs`) lists only `EL-*.md` files and admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `freezing_mold` |
| Named identity | TH-13, `../invest/named-elements.md#thermal-13` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-447` and the ledger entry has no todo link |
| Decision | S577; `scope-corrections.md` on published main has no `s577` block |
| Mode records | `Configuration=Fixed` |
