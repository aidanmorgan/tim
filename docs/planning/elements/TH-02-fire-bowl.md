# TH-02 · Fire bowl — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-02 delivery slice](../invest/named-elements.md#thermal-02). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`); repaired the same day against the Batch N audit (`docs/planning/elements/readiness/audits/elements-batch-n.md`, findings A1, A2, A3 and the Receiver citation), and on 23 November 2026 against `docs/planning/elements/readiness/audits/reaudit-group-3.md` (B2's igniter, the emitter origin, B4's boundary). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here (steady values from the steady-state balance, transients from lumped estimates) and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal rules are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); burning follows the [combustion conventions](TH-01-candle.md#combustion-conventions) (F1 to F6). Neither is restated.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-02 · Fire bowl · Thermal |
| Anchors | [requirements.md#thermal-02](../requirements.md#thermal-02) (task `sequence-task-436`); [named-elements.md#thermal-02](../invest/named-elements.md#thermal-02) |
| Owner key / research | S597 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | Shares the burn with [TH-01 Candle](TH-01-candle.md) (lead); lit by a burning [TH-32 Tinder pad](TH-32-tinder-pad.md) or [TH-33 match](TH-33-spring-mounted-match.md); heats [TH-17 Kettle](TH-17-kettle.md) in TX-01. No CAT refines it. |
| Campaign | Intro 57; practice 58; reuse 85, 140 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), and on ENGINE-THERMAL, ENGINE-IGNITION-BLAST, ENGINE-OPTICAL and ENGINE-PARAMETER-BOUND-FRAMES. |

### Disposition

**To deliver as a single element** after the Candle. It is not a Family member with the Candle: it adds a second body of nodes (the vessel) and a bed that other parts light, so it differs in more than declared values. Planned package id `fire_bowl`, directory `elements/FireBowl/`, display name "Fire bowl".

## 2. Declaration

Follows the delivered Receiver shape for a static open tray of boxes: the Receiver is "a static open box of five walls" (`elements/Receiver/ReceiverElementDeclaration.cs:8`) whose declared collider list is empty (`:56-57`) because its five boxes are baked from its scene (`elements/Receiver/basket.tscn:33-45` shapes, `:77-101` collision nodes). This part declares its six boxes the same way, with the Candle's fuel and flame declaration.

- **Interfaces.** As [TH-01 section 2](TH-01-candle.md#2-declaration). `KinematicMode.Static`; `PayloadPolicy.None`.
- **Body.** One static body; origin at the centre of the base's underside.
- **Colliders (metres, boxes, centre / half extents).**

  | Collider | Centre | Half extents | Purpose |
  | --- | --- | --- | --- |
  | `col_base` | (0, 0.03, 0) | (0.25, 0.03, 0.25) | bowl floor; face of node `bowl` |
  | `col_rim_a`, `col_rim_b` | (0, 0.12, ±0.235) | (0.25, 0.06, 0.015) | rims; faces of `bowl` |
  | `col_rim_c`, `col_rim_d` | (±0.235, 0.12, 0) | (0.015, 0.06, 0.22) | rims; faces of `bowl` |
  | `col_bed` | (0, 0.085, 0) | (0.20, 0.025, 0.20) | the fuel bed, inside the rims (top at 0.11 m); face of node `bed_surface` |

  The rims' inner faces are 0.44 m apart and their tops at 0.18 m, so a 0.36 m Tinder pad lies flat on the bed (its wafer top at 0.17 m) with 0.04 m to spare on each side. Six boxes against the Receiver's five: the bed must be its own collider because it is the only ignitable face and parts rest on it.
- **Material (f32).** Friction 0.6, restitution 0.05, rolling resistance 0 (as the Candle).
- **Thermal nodes** (T2).
  - `bowl`: 210 J/K (1.0 kg of ceramic × 210), ambient 1.0 W/K, faces (base and rims) 16 W/K.
  - `bed`: 637.5 J/K (1.5 kg of wood × 425), internal link to `bowl` 1.0 W/K, no face.
  - `bed_surface`: 0.2125 J/K (0.5 g of wood); initial temperature 288 K, or 1035 K when `lit` is "Lit"; internal link to `bed` 0.01 W/K; face `col_bed` 16 W/K; ignite row at 573 K, extinction 520 K.
  - `flame`: 0.15 J/K (time constant 0.15 ÷ 0.26 = 0.58 s); initial temperature 288 K, or 1182 K when `lit` is "Lit"; ambient 0.25 W/K; internal link to `bed_surface` 0.01 W/K; no face.
- **Region link** (T3). A box 0.40 × 0.50 × 0.40 m centred at (0, 0.425, 0), from 0.175 m to 0.675 m: inside the rims (it reaches ±0.20 m, the rims start at ±0.22 m), above the wafer of a pad lying on the bed (0.17 m) and above the bed itself, so none of the bowl's own faces and no pad on the bed is in it. Axis +Y; fed by `flame`; G_r 4.0 W/K. A face collider's share is the area of overlap of its bounds with the box projected on the XZ plane ÷ 0.16 m², granted nearest-first along +Y, the sum capped at 1.
- **Fuel store.** `bed_fuel` on `bed_surface`: `fuel` × 15 625 J/kg (default 23 438 J); 240 W while lit; oxidiser 1.2/1024 kg of O₂ per kg (240 ÷ 15 625 = 15.4 g/s of wood, 18 mg/s of O₂). Burn credit (F1): 9 W to the emitter, 6 W to `bed_surface`, 225 W to `flame`.
- **Optical emitter** (optical convention 2, paid from a declared store). Origin (0, 0.25, 0): above the bed top (0.11 m) and a resting pad's wafer (0.17 m), below the underside of a body held on Walls (0.30 m) and between those Walls, so it lies inside no collider (an emitter whose origin lies inside another collider emits nothing, `EL-176-red-laser-emitter.md:172`); half-angle 180°; range 3 m; radiant (3, 3, 3) W paid by the burn.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `fuel` | Real | `Kilogram` | 1.5 | 0 | 2.0 | Clamp; build-only. 0 is the empty bowl: the bed is inert ash |
  | `lit` | Choice | none | 0 | 0 | 1 | "Unlit", "Lit"; build-only. "Lit" starts the Run burning with the initial temperatures above |

- **Ports.** None.
- **Presentation bindings.** `Visual/Flame` scale and emission ← committed burn power (0 to 240 W); `Visual/Bed` albedo warm wood → navy-grey ash as the committed fuel fraction falls from 1 to 0.
- **Palette.** Category `ElementCategory.Power`. Order: 321 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base`, `Visual/Rim_A` to `Rim_D` cream `#fff8e9`, with three navy `#293954` notches printed flush on each rim | boxes as the colliders, outer edges chamfered inside the collider | solid, `col_base`, `col_rim_*` | nothing |
| `Visual/Bed` warm wood `#c28f52`, chip pattern printed on top | box 0.40 × 0.05 × 0.40 m | solid, `col_bed` | nothing (recoloured by a binding) |
| `Visual/Flame` gold `#f7cb52` | three teardrops above the bed | decorative: glow, scaled by a binding | presentation binding |

The bowl is drawn as the square shallow tray its colliders are. Icon: a low tray with three flames. At a glance it differs from the Candle by being low and wide with a patterned bed, and from the Receiver by its filled interior.

### Compiled counts

Bodies 1 (static); colliders 6 (boxes); joints 0; sensors 0; thermal nodes 4; internal links 3; region links 1; fuel stores 1; ignite rows 1; optical emitters 1; ports 0; presentation bindings 3.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Owner or test |
| --- | --- | --- | --- |
| TH-04 R1, R6 | Static box colliders from art; parameter bounds | present | tests named in [TH-04](TH-04-electrical-heating-plate.md#3-engine-capabilities) |
| TH-04 R9 to R13 | Thermal nodes, contact conduction, ambient exchange, drivers, units | **missing** | ENGINE-THERMAL |
| TH-01 C1 | Store fraction as a presentation driver | narrower | ENGINE-IGNITION-BLAST |
| TH-01 C2 to C4 | Ignition and burn with a split credit, oxidiser, spent-solid outcome | **missing** | ENGINE-IGNITION-BLAST |
| TH-01 C5 | Region link | **missing** | ENGINE-THERMAL |
| TH-01 C6 | Store-paid optical emitter | **missing** | ENGINE-OPTICAL |
| TH-01 C7 | Initial temperatures selected by the `lit` Choice | **missing** | ENGINE-PARAMETER-BOUND-FRAMES |

Counts: present 2, untested 0, narrower 1, missing 11. It adds no row to the Candle's probe, and each missing row has the one owner named there. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-02](../requirements.md#thermal-02): a vessel that contains combustible material without being an intrinsic heat source; reaction belongs to its contents and surrounding gas; no variants. Visual style: "Shallow cream stone bowl, visible fuel bed and sparse navy rim marks".
- **Named entry** [thermal-02](../invest/named-elements.md#thermal-02): owner S597; "An empty bowl or inadequate oxidizer cannot sustain flame".
- **Research** [TH-S03](../../thermal-component-research.md#th-s03) and per-element table: "larger fuel source + reaction state"; "smothering a lid puts it out"; flame ← power.
- **Element map** row TH-02: ChemicalReaction, Extinction, FiniteLedger, GeometryQuery, Ignition, SensibleHeat, TopologyTransaction (D14).
- **Processes and scenarios.** [IX-28](../requirements.md#interaction-28), [IX-29](../requirements.md#interaction-29), [IX-30](../requirements.md#interaction-30); [TX-01](../requirements.md#thermal-scenario-01), [TX-14](../requirements.md#thermal-scenario-14).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for bowl, fire, fuel, combustion and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787`.

## 5. Acceptance

Point of truth: [thermal-02](../requirements.md#thermal-02) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

Write x = T_flame − 288 K and y = T_bed_surface − 288 K; the 637.5 J/K bed stays within a few kelvin of 288 K over any row here.

- **Burn time.** 23 438 ÷ 240 = 97.7 s at the default; `fuel` 0.25 kg gives 3906 ÷ 240 = 16.3 s; 2.0 kg gives 130 s.
- **Lit steady state, nothing held over it** (audit A3, recomputed). Surface: 6 + 0.01·(x − y) = 0.01·y, so y = 300 + 0.5·x. Flame: 225 = 0.25·x + 0.01·(x − y) = 0.255·x − 3, so x = 228 ÷ 0.255 = 894. Free flame 1182 K, bed surface 1035 K. These are the "Lit" initial temperatures.
- **A lit bowl stays lit** (audit A1). With the flame at ambient the surface's own 6 W holds it at 288 + 6 ÷ 0.02 = 588 K, 68 K above its 520 K extinction, so nothing held in the flame can put the bed out.
- **Kettle in the flame** (the kettle's bounds, 0.2 × 0.2 m, cover 0.04 ÷ 0.16 = 0.25 of the box: G = 1.0 W/K). At 373 K: 225 + 85 = 1.255·x − 3, x = 249.4: flame 537 K, into the kettle 164 W, to ambient from the flame 62 W. At 288 K: x = 228 ÷ 1.255 = 181.7: flame 470 K, into the kettle 182 W, surface 300 + 91 = 391 K of rise (679 K). ([TH-17](TH-17-kettle.md) chain 2 takes 182 W and 164 W.)
- **A default kettle held over the bed for 10 s** (B4's boundary). Its 34.9 J/K warms as it takes the heat: with θ its rise, 1.255·x = 228 + θ gives P = (228 + θ) ÷ 1.255 − θ = 181.7 − 0.203·θ W, and it loses 0.3·θ to the room, so θ = 361 × (1 − e^(−t ÷ 69.4)) (time constant 34.9 ÷ 0.503): 48.5 K at 10 s. It gains 34.9 × 48.5 = 1693 J, 73% of the 2310 J credited, plus part of the roughly 170 J the flame and bed surface release as they cool from their lit values (about 77% in all; it took about 1767 J and lost about 74 J to the room); the flame then reads 288 + (228 + 48.5) ÷ 1.255 = 508 K against 342 K under a covering block.
- **A body covering the box** (G = 4.0 W/K, at 288 K): x = 228 ÷ 4.255 = 53.6: flame 342 K, into the body 214 W of the 231 W of heat, surface 615 K.
- **Held over it.** A kettle or block is held in the box on two Walls of the minimum thickness 0.12 m (`elements/Wall/WallElementDeclaration.cs:30`) laid flat across the rims 0.10 m apart, centred on the bowl's axis (so the emitter origin lies in the gap between them); the body's underside is then at 0.30 m, inside the box. Walls declare no thermal node, so they take no share and conduct nothing.
- **Lighting the bed.** `bed_surface` needs 0.2125 × 285 = 60.6 J. Unlit it loses 0.01 W/K to the bed and 0.01 × 0.25 ÷ 0.26 = 0.0096 W/K through the cold flame node: 0.0196 W/K. A burning Tinder pad lying on the bed holds the bed in its region box (the bed's 0.40 m bounds cover the pad's whole 0.1296 m² box, 0.06 W/K; [TH-32](TH-32-tinder-pad.md)). The fibre sits at (16 + 0.02 × 288 + 0.06·T_s) ÷ 0.08 = 272 + 0.75·T_s (488 K while the surface is at 288 K, above its 420 K extinction), so the surface gains 0.06 × (272 − 0.25·T_s) = 16.32 − 0.015·T_s and keeps 21.97 − 0.0346·T_s: it settles at 635 K with a time constant of 0.2125 ÷ 0.0346 = 6.1 s and passes 573 K at 6.1 × ln(347 ÷ 62) = 10.6 s, well inside the pad's 48.8 s burn.
- **A strike alone.** A Flint strike's whole 3.72 J ([TH-31](TH-31-flint-striker.md)) could raise the surface by at most 3.72 ÷ 0.2125 = 17.5 K: no ignition.
- **Not an intrinsic source.** With `fuel` 0 the store is empty; heating the bowl or the ash to any temperature starts no burn (F2).
- **Why 240 W.** Three candles: "hotter, wider heat" (research table) and payable from the declared bed for about 100 s.
- **Tick order.** As the Candle: the burn runs in the thermal step (T8); the emitter radiates in each tick's optical solve what the burn paid the tick before (optical convention 4).
- **Run length.** Every row fits in the default 30 s.
- **Tolerances** (T12): energy 2% or 5 J; temperature 2 K or 5% of the rise; times 15%; stall guard the smaller of 3 × the hand time and the hand time + 60 s, at least 10 s, never past the Run length.

### Rows

Planned class `FireBowlTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| B1 | Given a default bowl with `lit` "Lit", when 10 s pass, then the store has fallen by 2400 J, the flame reads 1182 K and the bed is lit throughout | `ALitBedBurnsAtItsDeclaredPower` | "Unlit": store whole, flame and surface at 288 K | none | 48 J; ± 45 K (5% of the 894 K rise) |
| B2 | Given `fuel` 0 and "Lit", then nothing burns; a Tinder pad lying on the ash, lit by a Spring match placed as in B5, burns out and no flame follows | `AnEmptyBowlCannotSustainFlame` | B1 | `fuel` 0.25: burns, then goes out between 14 and 19 s | exact zero burn power |
| B3 | Given B1 in the Vacuum atmosphere, then nothing burns and the store is whole | `InadequateOxidiserCannotSustainFlame` | Standard atmosphere (B1) | none | exact store value |
| B4 | Given B1 and a Thermal storage block ([TH-26](TH-26-thermal-storage-block.md), 420 J/K at 288 K) held on Walls across the rims and covering the box, when 10 s pass, then the block has gained at least 85% of the 2310 J of heat the burn credited (hand: 214 W × 10 s plus heat the flame releases as it cools) and the bed is still lit | `ABodyOverTheBedIsHeatedByItsShareOfTheFlame` (a cross-part row owned by the TH-26 slice: the block is taught at 81, after the bowl at 57) | the same block beside the bowl gains under 10 J | a default TH-17 kettle held on the same Walls (bounds covering 0.25 of the box) gains at least 70% (hand about 77%, section 5) and the flame reads at least 100 K hotter (hand 508 K against 342 K) | as stated; the share of the heat is the gameplay quantity |
| B5 | Given an unlit default bowl with a Tinder pad lying on its bed and a Spring match ([TH-33](TH-33-spring-mounted-match.md)) beside the bowl, its block's side against a rim and its base raised 0.10 m so that its region box covers 0.70 of the wafer and none of the bed, when the match is released, then the pad is burning within 1 s, the bed is lit within 30 s, and the pad stays lit until the bed lights | `BurningTinderLightsTheBed` (needs TH-32 and TH-33, in whichever slice is last) | no pulse: the pad and the bed stay unlit | the pad lying on the bench beside the bowl instead: the bed never lights | stall guard 30 s (3 × the 10.7 s hand time, capped at the default Run) |
| B6 | Given B1, then fuel debit = heat credited to nodes and the ambient account + light | `FuelDebitEqualsHeatPlusLight` | "Unlit": all zero | none | 48 J |
| B7 | Given B1 then Reset, then store, temperatures and state are as authored; a second Run repeats B1 | `ResetRestoresBedBowlAndState` | none | none | as B1 |
| B8 | Given `fuel` 0.5 and "Lit", when saved and loaded, both values are kept and no run state is saved | `SaveAndLoadKeepFuelAndLitChoice` | none | none | exact |
| B9 | `fuel` −0.1 or 2.1, or `lit` 2, is not admitted | generic: `PackageRuntimeParityTests.EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds` | 0 and 2.0 admitted | the four bounds | typed refusal |

No port, so no wrong-kind row. The requirement's outcome, "an empty bowl or inadequate oxidizer cannot sustain flame", is B2 and B3. The enclosed-air form of TX-14 is a later row (F3).

## 6. Open questions

Resolved:

1. **Game scale.** D5; stated in TH-04.
2. **Contents.** An authored bed whose mass is the `fuel` parameter; 0 is the empty bowl. A separately placed combustible (TH-03, TH-32) resting in the bowl burns by its own declaration (decided now: one rule for every fuel, no "contents" mode).
3. **Lid.** None (D19): no lid identity exists and the oxidiser control is B3 until an enclosure part is delivered.
4. **Oxygen model.** D6; F3.
5. **Bowl collider.** Six boxes, drawn as the tray they are (decided now: visible = solid; a round bowl would need a hollow frustum for no gameplay gain).
6. **Authored lit state.** Added (D19): TX-01 uses a burning bowl as a source and B1 needs a positive that does not depend on another part. It starts at the lit steady state (audit A1).
7. **Bowl size** (decided now): 0.50 m across with 0.03 m rims, so that a 0.36 m Tinder pad lies on the bed; the earlier 0.32 m opening made a pad rest on the rims, 0.07 m above the bed and with its region box on the rims.
8. **Flame capacity, bed share and link** (audit A1, decided now): `flame` 0.15 J/K; 6 W of the burn on the bed surface, so that a cold kettle cannot put it out (with a flame-only credit and the audit's 0.349 share, a 288 K kettle holds the surface at 404 K, below its 520 K extinction); surface–flame link 0.01 W/K, so that the cold flame node of an unlit bowl drains a pad-heated bed by only 0.0096 W/K.
9. **Flame box above the bed** (decided now): the box starts above a resting pad's wafer, so the flame of a newly lit bed does not chill the pad that lit it.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-32 (lights the bed), TH-33, TH-17 (TX-01), TH-37, TH-03, TH-26; Wall (delivered) as the support of held bodies. Only the Wall is delivered.
- **Campaign.** Introduction 57, practice 58; reuse 85, 140. A fixed bowl needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element, after TH-01. B5 runs when TH-32 and TH-33 are delivered, in whichever slice is last.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the region query in particular); ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL in the order of owner decision 18; ENGINE-IGNITION-BLAST as amended; ENGINE-PARAMETER-BOUND-FRAMES for the `lit` start; ENGINE-OPTICAL for the emitter; TH-01; S7. Row B4 runs in the TH-26 slice.
- **Files.** `elements/FireBowl/FireBowl.csproj`, `FireBowlElementDeclaration.cs`, `fire_bowl.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/FireBowlTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only when the Candle's slice has left every probe row present and tested.
- **Disqualifiers.** A fuel store that cannot take its capacity from a parameter with minimum 0; the Candle's disqualifiers.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Six static boxes, four nodes, three internal links, one region link, one fuel store with a split credit, one emitter, two parameters, three bindings.
2. **Generic laws.** The Candle's: threshold ignition, store-paid burn, Q̇ = G·ΔT by T9's exchange. "The bowl is not a heat source" is the absence of any source on the `bowl` node.
3. **Three layers.** Colliders: six boxes. Visuals: the same six solids with printed notches and chip pattern; flames. Animation: flame scale and bed colour from committed state; nothing written back.
4. **Flow and typing.** No ports; f32; `lit` is a Choice.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static body; six boxes, one more than the nearest delivered tray, for the reason given. One bounded region query per tick (T10).
7. **Tests are scenarios.** B1 to B8 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, ENGINE-IGNITION-BLAST, ENGINE-OPTICAL and ENGINE-PARAMETER-BOUND-FRAMES (general capabilities) and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `fire_bowl` |
| Named identity | TH-02, `../invest/named-elements.md#thermal-02` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-436` and the ledger entry has no todo link |
| Decision | S597; `scope-corrections.md` on published main has no `s597` block |
| Mode records | `Lit=Unlit`, `Lit=Lit` |
