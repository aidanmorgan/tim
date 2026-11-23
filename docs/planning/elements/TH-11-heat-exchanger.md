# TH-11 · Heat exchanger — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-11 delivery slice](../invest/named-elements.md#thermal-11). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (C3, section 5, story names) and owner decisions 13, 17 and 20 and coordinator decision 16 of 22 November 2026, and on 23 November 2026 against `docs/planning/elements/readiness/audits/reaudit-group-3.md` (half-full passages, A1, A2, A4, EL-001's liquid temperature). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal units, laws, scale, tick phase and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); shared probe rows are cited as "TH-04 R*n*". Liquid units, mouths, joins and flow are the liquid conventions L1 to L18 owned by [EL-001](EL-001-finite-reservoir.md) (repaired set; Batch G's additions per [Batch F summary](readiness/batch-f.md) section 3).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-11 · Heat exchanger · Thermal |
| Anchors | [requirements.md#thermal-11](../requirements.md#thermal-11) (task `sequence-task-445`); [named-elements.md#thermal-11](../invest/named-elements.md#thermal-11) |
| Owner key / research | S575 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | The Water family's pipes, taps and reservoirs (Batches F and G) feed and drain it; a [Finite reservoir](EL-001-finite-reservoir.md) authored warm is the warm feed (owner decision 20); [TH-17 Kettle](TH-17-kettle.md) and [TH-18 Condenser](TH-18-condenser.md) are the other liquid-carrying thermal parts. No CAT refines it. |
| Campaign | Intro 69; practice 70; reuse 87, 137 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-CAPABILITY-INSTANCES (package-local mouth ids); then ENGINE-GAS-NETWORK (the one fluid port contract, coordinator decision 16), ENGINE-LIQUID, ENGINE-THERMAL and ENGINE-RUN-LENGTH. |

### Disposition

**To deliver as a single element.** Planned package id `heat_exchanger`, directory `elements/HeatExchanger/`, display name "Heat exchanger". Not a duplicate of any pipe: it holds two stores that share no liquid edge and one thermal path.

## 2. Declaration

Follows the shape ENGINE-LIQUID gives a two-mouth straight pipe (L3, L5); no delivered package holds liquid, so the nearest delivered shape is the static block of the Wall for the body.

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`); `IFluidElement` (`CuriousContraptions.Abstractions.Fluid/FluidContracts.cs:5`), the one fluid port contract that ENGINE-GAS-NETWORK owns and ENGINE-LIQUID consumes (coordinator decision 16; L5); ENGINE-THERMAL's node contract. `KinematicMode.Static`.
- **Body and collider.** One static body, one box `col_block`: centre (0, 0.20, 0), half extents (0.40, 0.20, 0.30) m. Friction 0.6, restitution 0.05, rolling resistance 0. Passage A runs along X at z = −0.15, passage B at z = +0.15.
- **Liquid stores (L3, L4).** `passage_a` and `passage_b`: capacity 0.0251 m³ each (0.8 m of the pipe bore, area 0.031416 m², L5), interior 0.80 × 0.20 × 0.1575 m (plan area 0.126 m², height 0.20 m), floor datum y 0.10 m; no open aperture, so each is a closed store by EL-001 L4's **Closed store** rule (at V = C its mouths pass flow as a lossless fitting and it never overflows), not L4's sealed compliant variant; authored empty. The mouths' datum (y 0.20) is mid-height, so a passage with a free outlet stands half full (0.01255 m³, 210.0 J/K; section 5). There is no liquid edge between them. Each store is also a thermal node (T2, owner decision 20): capacity = its liquid mass × 1046 J/(kg·K); inflow mixes by mass, outflow leaves at the store's temperature; an empty store has zero capacity and is inactive (T9).
- **Mouths (L5).** Pipe bore, role `FluidPortRole.Bidirectional` (`CuriousContraptions.Abstractions/FluidContractValues.cs:5`), mouth-joined sockets with package-local ids:

  | Id | Opens into | Position | Facing |
  | --- | --- | --- | --- |
  | `Start` | `passage_a` | (−0.40, 0.20, −0.15) | −X |
  | `End` | `passage_a` | (0.40, 0.20, −0.15) | +X |
  | `StartB` | `passage_b` | (−0.40, 0.20, 0.15) | −X |
  | `EndB` | `passage_b` | (0.40, 0.20, 0.15) | +X |

  `StartB` and `EndB` are package-local ids (L5: "a part declares one or more" mouths with package-local ids, ENGINE-CAPABILITY-INSTANCES; X5). Either stream may run in either direction, so parallel and counter flow are a matter of plumbing.
- **Thermal nodes** (T2). The two stores (above), and `partition`: 28.8 J/K (0.3 kg of copper at 96 J/(kg·K)); no ambient conductance and no face (the block insulates it). Two wetted links, `partition`–`passage_a` and `partition`–`passage_b`, 64 W/K each × the passage's committed fill fraction (T3, T9): 16 W/K between two flowing (half-full) passages.
- **Ports (wires), parameters, stores of energy.** None (D13).
- **Presentation bindings.** `Visual/Fill_A`, `Visual/Fill_B` scale ← committed fill fraction of each store; `Visual/Arrow_A`, `Visual/Arrow_B` emission ← committed heat flow of each wetted link, 0 to 400 W.
- **Palette.** Category `ElementCategory.Structure`. Order: 532 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Block` cream `#fff8e9` | box 0.80 × 0.40 × 0.60 m | solid, `col_block` | nothing |
| `Visual/Window_A`, `Visual/Window_B` cyan `#66b8c9` | two flat panes printed flush on the top face, one over each passage | decorative: printed flat | nothing |
| `Visual/Fill_A`, `Visual/Fill_B` | a flat bar inside each pane | decorative: printed flat, scaled by a binding | presentation binding |
| `Visual/Arrow_A` (one notch), `Visual/Arrow_B` (two notches) navy `#293954` | arrows embossed flush beside each pane | decorative: printed flat, lit by a binding | presentation binding |
| four mouth rings gold `#f7cb52` | flush rings on the end faces | decorative: flush | nothing |

Icon: a block with two parallel channels and a zigzag between them. It differs from a pipe by the two panes side by side; the two routes are told apart by one notch and two notches, not by colour.

### Compiled counts

Bodies 1 (static); colliders 1 (box); joints 0; sensors 0; liquid stores 2 (each also a thermal node); mouths 4; liquid edges inside the part 0; thermal nodes 3 (partition and two stores); wetted links 2; wire ports 0; presentation bindings 4.

## 3. Engine capabilities

### Capability probe

Uses TH-04 R1, R9, R12. Its own rows:

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| X1 | The one fluid port contract with mouth-joined sockets | narrower: declared, serialised and validated by the bundle, with no solver or element consuming it ([Story ENGINE-GAS-NETWORK](../../../_bmad-output/planning-artifacts/epics.md#story-engine-gas-network-connected-finite-gas-stores-lines-and-valves), *Present today*) | `IFluidElement`, `CuriousContraptions.Abstractions.Fluid/FluidContracts.cs:5`; `FluidPortDescriptor`, `CuriousContraptions.Abstractions/FluidContractValues.cs:12` | not opened |
| X2 | Liquid stores, mouths, mouth joins and head-driven flow with a closed volume ledger | **missing** | none | none; owner ENGINE-LIQUID |
| X3 | A liquid store carrying enthalpy: inflow mixes by mass, outflow leaves at the store's temperature, and the store is a thermal node with an authored starting temperature | **missing** | none | none; owner ENGINE-THERMAL |
| X4 | A wetted link whose conductance scales with the store's fill fraction | **missing** | none | none; owner ENGINE-THERMAL |
| X5 | More than the three conventional mouth ids on one part | **missing**: mouths are snap sockets with package-local ids (L5); today the snap socket is the two-ended `TubeMouthDescriptor` (`CuriousContraptions.Abstractions/SceneGeometry.cs:39`) | none | none; owner ENGINE-CAPABILITY-INSTANCES |

Counts: present 1 (TH-04 R1), untested 0, narrower 1 (X1), missing 6 (TH-04 R9, R12; X2 to X5). Owners, one each: X1 **ENGINE-GAS-NETWORK** (coordinator decision 16: one fluid port contract with two socket kinds); X2 **ENGINE-LIQUID**; X5 **ENGINE-CAPABILITY-INSTANCES** (package-local ids, the replacement of the two-ended `TubeEnd` that L5 names); X3, X4 and the TH-04 rows **ENGINE-THERMAL** (T2, T3, T9), which owner decision 18 places after ENGINE-LIQUID. ENGINE-THERMAL's text in `epics.md` has "heat capacity and temperature per body or vessel" but not the advection of X3; the complete proposed text is in the [Batch N summary](readiness/batch-n.md). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-11](../requirements.md#thermal-11): two separated fluid passages exchange energy through a conductive partition; material remains in its own passage; no variants.
- **Named entry** [thermal-11](../invest/named-elements.md#thermal-11): owner S575; "Equal-temperature streams give no net exchange; blocked flow limits delivery and cannot cross-contaminate channels".
- **Research** per-element table: "two-stream link"; parameter conductance; flow arrows ← q.
- **Element map** row TH-11: FiniteLedger, FluidAdvection, GeometryQuery, SensibleHeat, ThermalConduction, ThermalConvection, TopologyTransaction. Navigation only (D14).
- **Processes.** [IX-08](../requirements.md#interaction-08), [IX-18](../requirements.md#interaction-18), [IX-19](../requirements.md#interaction-19).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for exchanger, passage and heat terms. The legacy gas files hold single sealed inventories with no heat exchange (`engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6`).

## 5. Acceptance

Point of truth: [thermal-11](../requirements.md#thermal-11) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Liquid: L7 flow, L10 ledger, per passage. Heat: Q̇ = G·ΔT on the two wetted links (T3) by the exact two-node step (T9); a stream carries ṁ·c·T with it (X3). No liquid edge joins the passages, so nothing can cross.
- **Warm feed (owner decision 20).** Every liquid store is a thermal node with an authored starting temperature, so a Finite reservoir authored at 348 K feeds warm water and one at 288 K feeds cold water. EL-001 declares that temperature as its parameter `liquid_temperature` (273 to 368 K, `EL-001-finite-reservoir.md:61`).
- **A passage stands half full.** The mouths' datum (y 0.20) is the interior's mid-height (floor 0.10 m, height 0.0251 ÷ 0.126 = 0.20 m). By L7 and L8 a passage with a free outlet passes liquid only while its surface is above that datum: flowing at 0.001 m³/s it stands 0.00014 m above it (below), and when its tap closes it drains to it. Either way it holds 0.126 × 0.10 = 0.01255 m³ (0.0251 ÷ 2), 0.2008 kg, 0.2008 × 1046 = 210.0 J/K, and each wetted link is 64 × 0.5 = 32 W/K, 16 W/K between the passages.
- **Standing water.** Passage A drained to its datum at 348 K, passage B at 288 K, taps closed. Two 210.0 J/K stores through 16 W/K: time constant (210.0 ÷ 2) ÷ 16 = 6.6 s (capacity and conductance both halve from a full passage, so the time is unchanged). After 6.6 s the difference is 60 ÷ e = 22 K. Final temperature (210.0 × 348 + 210.0 × 288 + 28.8 × 288) ÷ 448.8 = 316.1 K. Heat moved 210.0 × (348 − 316.1) = 6.71 kJ. When the passages are filled through the mouths, the partition already exchanges during the fill (its links scale with fill), so the rows take the temperatures at the moment both passages have drained to their datum after the feeds close as their start, not the reservoirs' authored values.
- **Flowing water.** Both streams at 0.001 m³/s (0.016 kg/s, a capacity rate of 0.016 × 1046 = 16.74 W/K; with the pipe bore's K₀ = Cd·A₀·√(2g) = 0.6 × 0.031416 × 4.429 = 0.08349 m^2.5/s (EL-001) that is a head of only (0.001 ÷ 0.08349)² = 0.00014 m, so a lesson throttles the feeds with a tap). The flow is twice L17's 0.0005 m³/s "no flow" threshold. Each passage is one mixed store: 16.74·(348 − T_A) = q = 16.74·(T_B − 288) and q = 16·(T_A − T_B) give q = 16 × 60 ÷ (1 + 32 ÷ 16.74) = 960 ÷ 2.912 = 329.7 W, T_A = 348 − 19.7 = 328.3 K, T_B = 288 + 19.7 = 307.7 K. (Full passages would give q = 60 ÷ (1/32 + 2/16.74) = 398 W and T_B = 311.8 K; the mid-height mouths never let a flowing passage fill.) Stream B leaves 19.7 K warmer; stream A leaves 19.7 K cooler. An empty passage fills to its datum in 0.01255 ÷ 0.001 = 12.6 s and is flushed every 12.6 s after that; the outlet is within 2 K of 307.7 K well before 75 s.
- **Blocked flow.** Stream B's tap closed with its passage at its datum (half full, 210.0 J/K): after the tap closes only the 0.00014 m × 0.126 m² = 1.8 × 10⁻⁵ m³ above the datum drains, then nothing leaves `EndB`. B warms toward A's 348 K inlet through 16 W/K into the flowing passage A (210.0 J/K, flushed at 16.74 W/K). With a = 16 ÷ 210.0 = 0.0762 and b = 16.74 ÷ 210.0 = 0.0797 s⁻¹ the slower rate is (2a + b − √((2a + b)² − 4ab)) ÷ 2 = (0.2321 − √(0.05387 − 0.02429)) ÷ 2 = (0.2321 − 0.1720) ÷ 2 = 0.0300 s⁻¹, a time constant of about 33 s (the partition's 28.8 J/K adds a little). From 288 K, with A filling at 348 K, the slow mode carries 44 K of B's 60 K deficit, so after 115 s B is within 44 × e^(−115 ÷ 33) = 1.4 K of 348 K.
- **Equal temperatures.** No exchange at any flow.
- **Tick order.** Liquid moves in the substeps of tick N (L16) carrying its enthalpy; the thermal step of tick N, after the last substep and the optical solve (T8), exchanges heat between the stores as they then stand, using the fill fractions committed by the last substep.
- **Run length these rows need: 135 s** (A4 observes at 75 s; its boundary fills passage B (12.6 s), closes its tap and lets it drain to its datum (a few seconds) before A's feed opens, then observes 115 s later, near 130 s; stall guard the smaller of 3 × 75 and 75 + 60 s, T12; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances.** Temperature 2 K or 5% of the change (T12); volume 0.004 m³ and flow 10% (L17); times 15%.

### Rows

Planned class `HeatExchangerTests` in `CuriousContraptions.tests`. The feeds are Finite reservoirs authored at 348 K and 288 K through taps (EL-003); a tap closed is a closed mouth.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given `End` and `EndB` unjoined free ends over a catch basin, passage A filled from the 348 K reservoir and passage B from the 288 K reservoir, both taps then closed, and t₀ the moment both passages have drained to their mouth datum, when 6.6 s pass, then the difference between the passages is the difference at t₀ divided by e; at t₀ + 40 s both are at the capacity-weighted mean of the three nodes at t₀ (316.1 K for ideal fills) | `StandingStreamsExchangeThroughThePartition` | both reservoirs at 288 K: no passage changes by more than 0.01 K after t₀ | none | 15% of the difference at 6.6 s; 2 K at 40 s |
| A2 | Given A1 at t₀ + 40 s, then the heat A lost since t₀ equals the heat B and the partition gained (6.71 kJ for ideal fills) | `HeatLeavingOnePassageEntersTheOther` | none | none | 2% of the transfer |
| A3 | Given passage A fed and its tap closed three times (each time it drains to its mouth datum through the free `End`) while passage B is empty, then passage B's volume is zero throughout and each passage's volume ledger closes on its own | `LiquidNeverCrossesBetweenPassages` | none | none | exact zero for B; 0.004 m³ for A's ledger |
| A4 | Given `End` and `EndB` unjoined free ends over a catch basin and both streams throttled to 0.001 m³/s, A fed at 348 K and B at 288 K, when 75 s pass, then stream B leaves at 307.7 K | `AFlowingStreamIsWarmedByTheOther` | B fed at 348 K too: outlet within 0.5 K of inlet | stream B stopped: passage B filled from the 288 K reservoir, its tap closed and the passage drained to its mouth datum before A's feed opens; then no volume leaves `EndB`, B warms toward 348 K with a time constant of about 33 s and is within 5 K of 348 K after 115 s | 3 K; stall guard 135 s |
| A5 | Given A1 then Reset, then both passages are empty and the partition is at 288 K | `ResetRestoresEmptyPassages` | none | none | exact volume; 0.01 K |
| A6 | Given an exchanger joined to four pipes, when saved and loaded, then its pose and the four joins are kept and no liquid or temperature is saved | `SaveAndLoadKeepTheJoins` | none | none | exact |

No wire ports and no parameters, so no wrong-kind wire row and no parameter row; a mouth of another bore is refused by ENGINE-LIQUID's own join test (L6). Requirement clauses: equal temperatures (A1's control, A4's control); blocked flow limits delivery (A4's boundary); no cross-contamination (A3).

## 6. Open questions

Resolved:

1. **Game scale.** D5 as restated in the thermal conventions.
2. **Fluid ports.** Liquid mouths by the delivered snap join (D3, L6) on the one fluid port contract (coordinator decision 16); no wire domain.
3. **Liquid density.** 16 kg/m³ (D2). The passage's capacity is its geometry (0.8 m of pipe bore).
4. **Mixed fluids.** Liquid in both passages (D19); a gas passage would wait for ENGINE-GAS-NETWORK and no lesson uses it.
5. **Partition conductance.** 64 W/K per wetted side at full fill; a flowing or drained passage stands half full, so every row runs at 32 W/K per side and 16 W/K overall, decided now: 8 W/K per side gave a rise under 3 K even at throttled flow.
6. **Outer loss.** None: the block declares no ambient conductance, decided now, so that every joule A loses is one B or the partition gains and row A2 closes.
7. **Warm water.** Settled by owner decision 20 (audit owner item 3): a reservoir authored warm. No second kind of store.

Needs the owner: none.

## 7. Integration

- **Works with.** Pipes, taps and reservoirs of the Water family (Batches F and G; none delivered).
- **A warm feed.** A Finite reservoir authored at up to 368 K (owner decision 20) through EL-001's `liquid_temperature` (273 to 368 K, `EL-001-finite-reservoir.md:61`).
- **Campaign.** Introduction 69, practice 70; reuse 87, 137. The levels declare a Run length of at least 135 s for the flowing lesson. A fixed placement needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-CAPABILITY-INSTANCES for package-local mouth ids, X5); ENGINE-GAS-NETWORK (X1), ENGINE-LIQUID (X2), then ENGINE-THERMAL as amended (X3, X4; owner decision 18); ENGINE-RUN-LENGTH; EL-001 with its authored liquid temperature, a tap (EL-003) and a pipe; TH-04; S7; S3 preferred.
- **Files.** `elements/HeatExchanger/HeatExchanger.csproj`, `HeatExchangerElementDeclaration.cs`, `heat_exchanger.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/HeatExchangerTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if X1 to X5 are present and tested when the slice starts; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** A liquid store with no temperature (the part cannot exist); liquid temperature kept beside, not in, the store's ledger (two copies, T2); fewer than four mouths per part; a wetted link not scaled by fill; a different bore or density (re-derive section 5).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** One collider, two liquid stores with four mouths, one node, two wetted links, four bindings.
2. **Generic laws.** Head-driven flow and a volume ledger per store; enthalpy carried with liquid; Q̇ = G·ΔT by the exact two-node step. "Material stays in its own passage" is the absence of any declared liquid edge, not a rule.
3. **Three layers.** Colliders: the block. Visuals: block, printed panes, bars, arrows and rings. Animation: bar scale and arrow glow from committed state. Nothing is written back.
4. **Flow and typing.** Four typed mouths; no wire ports; f32 values.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static; one primitive collider; per tick three thermal nodes and two links, with liquid work per substep bounded by its two stores and four mouths (L18). No scan.
7. **Tests are scenarios.** A1 to A6 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `heat_exchanger` |
| Named identity | TH-11, `../invest/named-elements.md#thermal-11` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-445` and the ledger entry has no todo link |
| Decision | S575; `scope-corrections.md` on published main has no `s575` block |
| Mode records | `Configuration=Fixed` |
