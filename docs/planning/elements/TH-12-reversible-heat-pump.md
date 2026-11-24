# TH-12 · Reversible heat pump — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-12 delivery slice](../invest/named-elements.md#thermal-12). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-n.md` and the owner decisions of 22 November 2026, and on 23 November 2026 against the second audit `docs/planning/elements/readiness/audits/reaudit-group-3.md` (A6 stall guard, Run length). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal units, laws, scale, tick phase and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); shared probe rows are cited as "TH-04 R*n*".

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-12 · Reversible heat pump · Thermal |
| Anchors | [requirements.md#thermal-12](../requirements.md#thermal-12) (task `sequence-task-446`); [named-elements.md#thermal-12](../invest/named-elements.md#thermal-12) |
| Owner key / research | S576 · [TH-S05 active cooling](../../thermal-component-research.md#th-s05) |
| Related identities | Supplied by [CAT-005 Battery](CAT-005-battery.md) (delivered); cools [TH-13 Freezing mold](TH-13-freezing-mold.md) (TX-02); rejects to [TH-10 Finned heat sink](TH-10-finned-heat-sink.md); loads such as [TH-26](TH-26-thermal-storage-block.md); [TH-30](TH-30-thermoelectric-generator.md) (the other converter). Contrast [TH-29 Cold pack](TH-29-cold-pack.md): a store, not a pump. No CAT refines it. |
| Campaign | Intro 78; practice 79; reuse 89, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)); forward and reverse are taught separately before dependent use. |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES (static pairs) and ENGINE-SUPPLY-DEMAND (the compressor's debit); then ENGINE-THERMAL and ENGINE-RUN-LENGTH. |

### Disposition

**To deliver as a single element; lead of the "Converters" specs** (TH-30 cites its row K1). Planned package id `heat_pump`, directory `elements/HeatPump/`, display name "Heat pump". Forward and reverse are two options of one `mode` parameter (D8), each with its own acceptance rows.

## 2. Declaration

Follows the Heating plate ([TH-04](TH-04-electrical-heating-plate.md#2-declaration)) for the supplied static part and the delivered Fan for a Choice that selects among declared rows (`elements/Fan/FanElementDeclaration.cs:13-14`, `:55-58`).

- **Interfaces.** `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement`, and ENGINE-THERMAL's node and converter contracts. `KinematicMode.Static`.
- **Body.** One static body; origin at the centre of its underside. Face A is the top plate, face B the bottom plate.
- **Colliders (metres).**

  | Collider | Shape | Centre | Half extents | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_face_b` | box | (0, 0.04, 0) | (0.20, 0.04, 0.20) | bottom plate; thermal face of node `face_b` |
  | `col_core` | box | (0, 0.20, 0) | (0.16, 0.12, 0.16) | supply core; no thermal face |
  | `col_face_a` | box | (0, 0.36, 0) | (0.20, 0.04, 0.20) | top plate; thermal face of node `face_a` |

- **How parts meet it** (contact links by touch, T3; every pair below is named in T3's static list or is a cargo pair).
  - *Load on face A*: cargo (a TH-26 block, a TH-29 pack) rests on the top plate; a static TH-13 tray is placed with its base at y = 0.40 m, touching the plate.
  - *Sink at face B*: a static TH-10 sink stands on the bench beside the pump with one side of its foot (0.5 × 0.06 × 0.5 m) against a side of the bottom plate (0.08 m high). The shapes touch along a 0.06 × 0.40 m strip; a link depends on touch only, never on area or force, so the pair conducts at the series value 64 × 64 ÷ 128 = 32 W/K.
  - *Pump on a Heating plate*: the bottom plate placed at y = 0.21 m on the disc.
- **Material.** Friction 0.6, restitution 0.05 on all three.
- **Thermal nodes** (T2). `face_a` and `face_b`: 45 J/K each (0.2 kg of aluminium at 225 J/(kg·K)); ambient conductance 0.1 W/K each; face conductance 64 W/K (metal). One internal link between them, 0.2 W/K (the leak back through the core).
- **Converter** (T4). One heat-pump row per `mode` option, selected by the option. Each names a cold node, a hot node and the consumer `compressor`. Per tick, with W the energy granted and D = T_h − T_c: **COP = 4 while D ≤ T_c ÷ 8, otherwise 0.5·T_c ÷ D**; Q_c = COP·W while D ≤ 60 K and T_c ≥ 233 K, otherwise Q_c = 0; the cold node is debited Q_c and the hot node credited Q_c + W. `Forward`: cold `face_a`, hot `face_b`. `Reverse`: the opposite.
- **Supply.** Consumer `compressor`, demand 24 W (fixed), whose target is the selected converter row. Electrical node `ElectricalRole.Consumer`, 12 V, maximum 24 W.
- **Port.** `PortId.PowerIn` (Electrical, Input, `PortRole.PowerIn`) at (0.16, 0.20, 0) facing +X.
- **Parameters.**

  | Name | Kind | Unit | Default | Options | Editor policy |
  | --- | --- | --- | --- | --- | --- |
  | `mode` | `ParameterKind.Choice` | none | 0 | `Forward` (0), `Reverse` (1) | player-editable unless the level locks it (D11); build-only |

- **Stores.** None of its own.
- **Presentation bindings.** On each plate, a printed frost glyph shown while that node's committed temperature is below 286 K and a printed flame glyph while it is above 290 K (four bindings); `Visual/Rotor` spin ← committed grant of `compressor`.
- **Palette.** Category `ElementCategory.Power`. Order: 335 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Face_A` cream `#fff8e9`, `Visual/Face_B` gold `#f7cb52` | boxes 0.40 × 0.08 × 0.40 m | solid, `col_face_a`, `col_face_b` | nothing |
| `Visual/Core` cyan `#66b8c9` | box 0.32 × 0.24 × 0.32 m | solid, `col_core` | nothing |
| frost and flame glyphs (two per plate) navy `#293954` | flat shapes printed flush on each plate's rim | decorative: printed flat, shown by a binding | presentation binding |
| `Visual/Rotor` | a disc printed flush in a window of the core | decorative: printed flat, turned by a binding | presentation binding |
| `PowerIn` socket | shared socket gizmo on the core's +X face | decorative: shared socket art | nothing |

The glyphs follow the committed temperatures, so they show what the faces are doing, not what the mode intends. Icon: two plates round a core with one frost and one flame shape. It differs from the Heating plate by having two plates and from the generator (TH-30) by having no ribbed wing.

### Compiled counts

Bodies 1 (static); colliders 3 (box); joints 0; drives 0; sensors 0; thermal nodes 2; internal links 1; converter rows 2 (one active); supply nodes 1; electrical nodes 1; ports 1; parameters 1; presentation bindings 5.

## 3. Engine capabilities

### Capability probe

Uses TH-04 R1 to R3, R5, R6, R9 to R13. R10 covers this part's static pairs (pump on the disc, sink foot against face B, tray on face A). Rows K1 and K2 are the converter family's own.

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| K1 | A converter row between two thermal nodes and one supply node, with a declared performance law and operating range, whose three flows sum to zero each tick; a declaration whose law can exceed the Carnot fraction is refused | **missing** | none | none; owner ENGINE-THERMAL |
| K2 | A Choice option selecting which of several declared rows is active | present, test not opened | `ChoicePredicate` as the Fan's `Select` (`elements/Fan/FanElementDeclaration.cs:14`) | `FanIntegrationTests.SelectedModeCompilesOneWorkInputAndOneBladeWriterWithoutHiddenStores` (`CuriousContraptions.tests/FanIntegrationTests.cs:485`, not opened) |

Counts: present 5 (TH-04 R1 to R3, R5, R6), untested 1 (K2), narrower 0, missing 6 (TH-04 R9 to R13, K1). All missing rows have one owner, **ENGINE-THERMAL**; its text in `epics.md` has store-paid sources but no two-node converter (complete proposed text in the [Batch N summary](readiness/batch-n.md)). The debit of the consumer's grant follows ENGINE-SUPPLY-DEMAND. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-12](../requirements.md#thermal-12): supplied work moves heat between two thermal ports with an explicit constitutive model and operating range; hot-side rejection equals removed heat plus input work; reverse operation is taught separately.
- **Named entry** [thermal-12](../invest/named-elements.md#thermal-12): owner S576; "Power loss stops pumping; an insulated hot side warms and limits cooling rather than swallowing heat".
- **Research** [TH-S05](../../thermal-component-research.md#th-s05) and per-element table: "electrical node + two-port pump"; parameters coefficient, range; compressor spin ← power.
- **Element map** row TH-12: ElectricalPower, FiniteLedger, GeometryQuery, HeatPumping, SensibleHeat, ThermalConduction. Navigation only (D14).
- **Processes and scenarios.** [IX-33](../requirements.md#interaction-33), [IX-06](../requirements.md#interaction-06), [IX-18](../requirements.md#interaction-18); [TX-02](../requirements.md#thermal-scenario-02).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for pump, refrigerat, Peltier and heat terms.

## 5. Acceptance

Point of truth: [thermal-12](../requirements.md#thermal-12), [TX-02](../requirements.md#thermal-scenario-02) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Law and bound.** Q_h = Q_c + W in every tick, so nothing is created. The law is defined for every pair of temperatures: at start-up D = 0 and the first branch gives COP 4. It is continuous at the switch (at D = T_c ÷ 8 the second branch gives 0.5·T_c ÷ (T_c ÷ 8) = 4). It is always under Carnot (T_c ÷ D): in the first branch Carnot is at least 8 for any D > 0, and for D ≤ 0 heat would flow that way unaided; in the second branch the law is half of Carnot.
- **Why 24 W.** A fifth of a default Battery's 120 W, so a pump and a Heating plate can share one source; 3600 J runs it for 150 s.
- **Unloaded, Forward.** Start: COP 4, Q_c = 96 W, Q_h = 120 W. `face_a` falls at 96 ÷ 45 = 2.1 K/s and `face_b` rises at 120 ÷ 45 = 2.7 K/s, 4.8 K/s between them. D reaches T_c ÷ 8 (about 34 K) after 34 ÷ 4.8 = 7 s, then COP falls: at D = 60 K it is 0.5 × 262 ÷ 60 = 2.2 (Q_c = 52 W) against a leak of 0.2 × 60 = 12 W and about 3 W from the room, so D still grows and reaches the 60 K limit near 15 s, with `face_a` about 262 K and `face_b` about 322 K. Beyond the limit Q_c = 0 and the 24 W goes to the hot face until the leak brings D back under 60 K.
- **Power loss.** With the source disabled the two faces relax through the 0.2 W/K leak: two 45 J/K nodes in series give 22.5 ÷ 0.2 = 112 s.
- **Insulated hot side.** A TH-26 block (420 J/K, ceramic face: 64 × 16 ÷ 80 = 12.8 W/K to the plate) on face A; the pump on the bare bench, nothing touching face B (0.1 W/K to the room only). Face B takes about 104 W into 45 J/K, 2.3 K/s; face A first drops about 6 K below the block (80 W ÷ 12.8 W/K), so D reaches 60 K after about (60 − 6) ÷ 2.3 = 23 s, by when the block has lost about 80 W × 23 s ÷ 465 J/K = 4 K. Pumping then stops: the hot face keeps 24 W of work, loses 12 W through the leak and 0.1 × 50 = 5 W to the room, so it stays above the limit, near 338 K. **With a static sink against face B** (5.5 W/K to the room behind the 32 W/K pair: 5.5 × 32 ÷ 37.5 = 4.69 W/K, plus the face's own 0.1): T_h = 288 + (96 + 24 − 6) ÷ 4.79 = 312 K and T_c is about 7 K under the block, about 280 K, so D is about 32 K, under T_c ÷ 8 = 35 K: COP stays 4. The block gives up 96 − 6 (leak) − 1 (room into face A) = 89 W and takes back about 3.2 W/K × 5 K = 16 W from the room through its own ambient conductance, 73 W net: 73 × 60 ÷ 465 = 9.4 K in a minute.
- **Freezing chain (TX-02), owned here.** Fixture: a static TH-10 sink against face B (4.69 W/K overall, above); a static TH-13 tray on face A. Values read from TH-13: water 0.05 kg (52.3 J/K), tray 67.5 J/K with 0.4 W/K to the room, base pair 32 W/K, wetted link 16 W/K at a full default charge, latent 0.05 × 20 875 = 1044 J.
  - *Freezing plateau.* Let F be the heat leaving the water. Tray temperature 273 − F ÷ 16; the tray also takes 0.4 × (288 − T_tray) from the room; face A takes N = F + that gain and sits at T_tray − N ÷ 32; the pump must lift Q_c = N + leak 0.2·D + 0.1 × (288 − T_c). With F = 61 W: T_tray = 273 − 3.8 = 269.2 K; room gain 0.4 × 18.8 = 7.5 W; N = 68.5 W; T_c = 269.2 − 2.1 = 267.1 K; taking D = 40.7 K, Q_c = 68.5 + 8.1 + 2.1 = 78.7 W; T_h = 288 + (78.7 + 24 − 8.1) ÷ 4.79 = 307.7 K, so D = 40.6 K; D exceeds T_c ÷ 8 = 33.4 K, so COP = 0.5 × 267.1 ÷ 40.6 = 3.29 and Q_c = 3.29 × 24 = 79.0 W. Consistent: **Q_c 79 W, T_c 267 K, T_h 308 K, COP 3.3, 61 W out of the water.** Freezing takes 1044 ÷ 61 = 17 s.
  - *Cooling to the plateau.* To remove: water 52.3 × 15 = 785 J; tray 67.5 × (288 − 269.2) = 1269 J; face A 45 × (288 − 267.1) = 941 J; 2995 J in all. Q_c falls from 96 W to 79 W (mean 87.5 W) while the leak grows from 0 to 8 W (mean 4 W) and the room returns about 5 W to tray and plate: net 78.5 W. 2995 ÷ 78.5 = 38 s.
  - **Frozen after about 55 s**, for 24 × 55 = 1.32 kJ of electrical work; the sink passes about 55 × (80 + 24) = 5.7 kJ to the room. The audit's 47 s (Q_c 81 W, T_c 270.5 K) is this chain without the 0.2 W/K leak, the tray's 3.8 K undershoot and the wetted link; with them the plateau is colder and the time longer.
- **Tick order.** The supply solve grants W inside tick N's substeps; the thermal step of tick N applies the converter with the temperatures committed by tick N − 1 and commits new ones (T8).
- **Run length these rows need: 140 s** (A4 observes at 20 + 112 = 132 s; A6's stall guard is the smaller of 3 × 55 = 165 s and 55 + 60 = 115 s, so 115 s (T12); ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances** (T12): temperature 2 K or 5% of the change; energy 2% or 5 J; times 15%.

### Rows

Planned class `HeatPumpTests` in `CuriousContraptions.tests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a wired pump in `Forward`, when 20 s pass, then face A is below 270 K and face B above 310 K | `ForwardCoolsFaceAAndWarmsFaceB` | unwired: both stay at 288 K | the difference never exceeds 62 K | 0.01 K on the control |
| A2 | Given A1, then at 5 s, 20 s and 40 s the Battery's debit equals the enthalpy change of both faces plus the ambient account | `RejectedHeatEqualsRemovedHeatPlusWork` | none | at the first tick (both faces at 288 K) the cold face is debited four times the grant | 2% or 5 J |
| A3 | Given `mode` = `Reverse`, when 20 s pass, then face B is below 270 K and face A above 310 K | `ReverseSwapsTheColdAndHotFaces` | A1 | none | as A1 |
| A4 | Given A1, when the Battery is disabled at 20 s, then pumping stops and the faces' difference has more than halved 112 s later | `PowerLossStopsPumping` | A1 continued: the difference stays above 55 K | none | 15% |
| A5 | Given a TH-26 block on face A and nothing touching face B, when 60 s pass, then the block has cooled by less than 6 K and face B is above 330 K | `AnInsulatedHotSideLimitsCooling` (a cross-part row owned by the TH-26 slice: the block is taught at 81, after the pump at 78) | with a TH-10 sink placed against face B the block cools by more than 7 K (9.4 K expected) | a sink 0.02 m clear of the plate behaves as no sink | 2 K |
| A6 | Given a TH-13 tray on face A and a TH-10 sink against face B, when Run, then the charge is fully frozen (55 s expected) | `APoweredPumpFreezesTheMold` (TX-02; run in the TH-13 slice) | unwired: never below 288 K | Battery `capacity` 600 J (25 s): the faces are cold but the solid fraction is 0 | stall guard 115 s |
| A7 | Given A1 then Reset, then both faces are at 288 K, the mode is as authored and the Battery is full | `ResetRestoresBothFacesAndTheMode` | none | none | exact mode; 0.01 K |
| A8 | Given a placed, wired pump in `Reverse`, when saved and loaded, then the mode and the connection are kept | `SaveAndLoadKeepTheModeAndConnection` | none | none | exact |
| A9 | An activation output wired to `PowerIn` is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (TH-04 R5) | — | — | typed refusal |
| A10 | `mode` = 2 or −1 is refused | generic: `PackageRuntimeParityTests.EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds` (TH-04 R6) | 0, 1 admitted | −1, 0, 1, 2 | typed refusal |

A6's boundary: 600 J runs the pump for 25 s, less than the 38 s the cooling phase needs, so no ice forms. Requirement clauses: Q_h = Q_c + W (A2); power loss (A4); insulated hot side (A5); reverse taught separately (A3 with its own row); operating range (A1's boundary).

## 6. Open questions

Resolved:

1. **Game scale.** D5 as restated in the thermal conventions.
2. **Mode switching.** Build-only Choice (D19): no lesson reverses the pump during a Run.
3. **COP model.** 4 up to a difference of T_c ÷ 8, half of Carnot beyond it, inside a 60 K and 233 K range (audit finding B3: the earlier single expression divided by zero at start-up). One continuous law with declared numbers, always under Carnot.
4. **Face layout.** Top and bottom plates; a load goes on top and a static sink stands beside the bottom plate with its foot touching it (coordinator decision 21: links come from touch, so the sink is a static fixture and need not sit under anything).
5. **Leak.** A declared 0.2 W/K between the faces: without it an unpowered pump would hold a temperature difference for ever.
6. **Signal input.** None (D19); the circuit switches it.

Needs the owner: none.

## 7. Integration

- **Works with.** Battery (delivered); TH-10, TH-13, TH-26, TH-09 (planned). The chain uses TH-13's charge, tray and wetted link and TH-10's 5.5 W/K (T14); if either changes, the 55 s changes with it.
- **Campaign.** Introduction 78 (Forward), practice 79; reuse 89, 145; Reverse is introduced in its own level before a dependent use. The freezing lesson declares a Run length of at least 90 s. A fixed placement needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element; first consumer of the converter row.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the static touch pairs; ENGINE-SUPPLY-DEMAND for the debit); ENGINE-GAS-NETWORK, ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18; K1 and TH-04 R10); ENGINE-RUN-LENGTH (A4, A6); TH-04; S7; S3 preferred. Row A5 runs in the TH-26 slice and row A6 in the TH-13 slice (both taught later, at 81 and 79).
- **Files.** `elements/HeatPump/HeatPump.csproj`, `HeatPumpElementDeclaration.cs`, `heat_pump.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/HeatPumpTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Engine tier on the first use of K1; data-only if ENGINE-THERMAL ships it tested.
- **Disqualifiers (the slice's probe re-checks).** A converter that takes its law as code instead of declared numbers; a touch rule that needs overlapping area or one face above the other (the side-by-side sink would then not conduct, and face B must be given a shelf); a static part that cannot be placed off the bench (the tray on face A); a consumer that cannot target a converter row.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Three colliders, two nodes, one internal link, two converter rows, one consumer, one port, one parameter, five bindings.
2. **Generic laws.** Proportional supply grant; a two-node converter whose flows sum to zero under a declared bounded law; Q̇ = G·ΔT on touch links. "Cools what stands on it" is conduction to a face that the converter keeps cold.
3. **Three layers.** Colliders: two plates and the core. Visuals: their art and printed glyphs. Animation: glyph visibility and rotor spin from committed state. Nothing is written back.
4. **Flow and typing.** One Electrical input; one Choice; f32 values.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static; three primitive colliders; per tick two nodes, one link, one converter row and the live pairs at the two plates in the link table (T10). It adds no loose solver body. Island sleeping is owned by ENGINE-ISLANDS-SLEEP (owner decision 3a); a resting load is stepped every tick until then, and its link persists and its node keeps exchanging while asleep (T3).
7. **Tests are scenarios.** A1 to A8 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `heat_pump` |
| Named identity | TH-12, `../invest/named-elements.md#thermal-12` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-446` and the ledger entry has no todo link |
| Decision | S576; `scope-corrections.md` on published main has no `s576` block |
| Mode records | `Mode=Forward`, `Mode=Reverse` |
