# TH-18 · Condenser — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-18 delivery slice](../invest/named-elements.md#thermal-18). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (E1, C2, C3, N6, the cold-pack chain of section 3) and owner decisions 17 to 20 and coordinator decisions 16 and 21, and on 23 November 2026 against the second audit `docs/planning/elements/readiness/audits/reaudit-group-3.md` (gas paths, F4 vent, condense row, A1 boundary). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); probe rows W1 to W11, the saturation table and the boil and condense row by [TH-17](TH-17-kettle.md); liquid conventions L1 to L18 by [EL-001](EL-001-finite-reservoir.md).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-18 · Condenser · Thermal |
| Anchors | [requirements.md#thermal-18](../requirements.md#thermal-18) (task `sequence-task-452`); [named-elements.md#thermal-18](../invest/named-elements.md#thermal-18) |
| Owner key / research | S582 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | Vapour from [TH-17](TH-17-kettle.md), [TH-35](TH-35-coffee-pot-steam-vessel.md), or the exhaust of [TH-19](TH-19-steam-piston.md) and [TH-20](TH-20-steam-turbine.md); sink [TH-29 Cold pack](TH-29-cold-pack.md) (and [TH-10](TH-10-finned-heat-sink.md), [TH-12](TH-12-reversible-heat-pump.md) once T3 lists their pair, section 7); liquid to the water family's stores (Batches F and G). No CAT refines it. |
| Campaign | Intro 74; practice 75; reuse 87, 137 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS (a Cold pack on the shelf is a face pair with a moving member) and ENGINE-SUPPLY-DEMAND (the rig's plate); then ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL (owner decision 18 order); ENGINE-CONNECTION-MATERIALS (solid strand: every vapour connection is a hose, owner decision 1); and ENGINE-RUN-LENGTH (rows need 140 s). |

### Disposition

**To deliver as a single element** after the Steam vessels family. Planned package id `condenser`, directory `elements/Condenser/`, display name "Condenser". Not a duplicate of anything delivered.

## 2. Declaration

Follows the delivered Ramp for a static tilted solid (`elements/Ramp/RampElementDeclaration.cs`) and TH-17 for stores.

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the store, port, node and phase-row contracts of the three engine stories. `KinematicMode.Static`.
- **Body.** One static body; origin on the bench under the foot.
- **Colliders (metres).**

  | Collider | Shape | Centre | Size | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_channel` | box, turned 15° about Z (low end at +X) | (0, 0.45, 0) | half extents (0.60, 0.10, 0.10) | the passage shell |
  | `col_foot` | box | (0, 0.17, 0) | half extents (0.15, 0.17, 0.10) | the gold foot; a thermal face |
  | `col_shelf` | box | (0, 0.05, 0.30) | half extents (0.25, 0.05, 0.20) | the cold shelf in front of the foot; a thermal face, the one top face a sink can rest on |
  | `col_drip` | tube, axis Y | (0.50, 0.17, 0) | outer radius 0.14, bore radius 0.10, length 0.08 | the drip mouth (the liquid family's pipe mouth, L5) |

  Geometry check (audit C3, confirmed): the channel's underside at x = 0 is 0.45 − 0.10 ÷ cos 15° = 0.346 m and at x = +0.15 it is 0.346 − 0.15 × tan 15° = 0.306 m, so the foot's top (0.34 m) lies under or inside the channel and no part can rest on it. The shelf spans x −0.25 to 0.25, y 0 to 0.10, z 0.10 to 0.50: it abuts the foot's front face (z = 0.10), stays clear of the channel (z within ±0.10, lowest point y 0.198 at its far end) and of the drip tube (x from 0.36). A Cold pack (0.4 × 0.16 × 0.3 m) lies on it at y 0.10 to 0.26 with room to spare.
- **Material (f32).** Friction 0.5, restitution 0.05, rolling resistance 0.
- **Gas store** `passage`: rigid, 0.004 m³, carrier air at ambient plus a steam component (W5).
- **Gas paths.** `GasIn` ↔ `passage` and `passage` ↔ `GasOut`, each through a throat of 1.0 × 10⁻⁴ m², flow following pressure in either direction (gas convention F3); `GasOut` is declared Vent when unconnected (gas convention F4, `EL-039-air-reservoir.md:89`). The throats are TH-17's spout throat: at the kettle's 1.17 g/s they need about 116 Pa each ((ṁ ÷ A)² ÷ (2ρ) = 11.7² ÷ 1.178), so they do not change the passage pressure that section 5's figures use.
- **Liquid store** `sump`: capacity 0.0025 m³, plan area 0.02 m²; one mouth `End` (`FluidPortRole` Discharge, pipe bore) at (0.50, 0.13, 0) facing −Y. Unjoined, it is a free end: the condensate falls as a stream and is captured by an open store whose rim is below 0.13 m, or goes to `Spilled` (L8). The store is a thermal node (T2, owner decision 20): mass × 1046 J/(kg·K), authored start 288 K, no ambient link; condensate enters it at the dew point and leaves through `End` carrying its enthalpy.
- **Thermal node** `wall`: 7.5 J/K (a thin aluminium shell, about 0.033 kg at 225 J/(kg·K)); ambient conductance 0.1 W/K (a ceramic-clad shell); faces `col_foot` and `col_shelf`, 64 W/K each. Wetted link `wall`↔`passage`: 6 W/K (a gas store is always full, fraction 1), carrying sensible heat 6 × (T_gas − T_wall).
- **Condense row** (T5's one condense form, the wall-limited row, in T5's words as TH-17 also declares it): while the `wall` node is below the dew point T_dew of the `passage` store's vapour partial pressure (T_sat of that pressure from the game-steam saturation table, T11, clamped at its ends), vapour moves from `passage` to `sump` at ṁ = G × (T_dew − T_wall) ÷ L_v(T_dew), never more than the vapour present, where G is the declared conductance of the `wall`↔`passage` wetted link (here 6 W/K); the row takes m·Cp_v·T_dew out of `passage`, of which m·c·T_dew enters `sump` with the liquid and the latent m·L_v(T_dew) = m × 107.2 × T_dew enters `wall` (T5, T11). The table's clamp gives T_dew = 333.2 K below 20 000 Pa. This row is the only place gas meets liquid in the part; no flow edge joins `passage` and `sump` (coordinator decision 16).
- **Ports.** `GasIn` (Fluid, Input) at (−0.60, 0.61, 0) facing −X; `GasOut` (Fluid, Output) at (0.60, 0.40, 0) facing +X, declared Vent when unconnected (gas conventions F3, F4).
- **Parameters.** None (D13).
- **Presentation bindings.** `Visual/Window` albedo ← committed heat flow of the wetted link, sensible plus latent (0 to 120 W mapped cream to cyan); the falling stream is the liquid story's shared stream presentation.
- **Palette.** `ElementCategory.Structure` (a passive exchanger; TH-04 section 6 item 5). Order: 534 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Channel` cream `#fff8e9` | box 1.2 × 0.2 × 0.2, turned 15° | solid, `col_channel` | nothing |
| `Visual/Foot` gold `#f7cb52` | box 0.30 × 0.34 × 0.20 | solid, `col_foot` | nothing |
| `Visual/Shelf` gold `#f7cb52` | box 0.50 × 0.10 × 0.40 | solid, `col_shelf` | nothing |
| `Visual/Drip` gold | open tube, outer radius 0.14, length 0.08 | hollow, `col_drip` | nothing |
| `Visual/Window` cyan `#66b8c9` | flat strip printed flush along the channel's front | decorative: printed flat, recoloured by a binding | presentation binding |
| port marks | rings printed flush at both ends; ports are scene locators | decorative: printed flat | nothing |

Icon: a sloping bar on a foot, a low shelf in front, a drop under its low end. It differs from the Ramp by the foot, the shelf and the drip tube.

### Compiled counts

Bodies 1 (static); colliders 4 (3 boxes, 1 tube); joints 0; sensors 0; thermal nodes 2 (`wall` and the liquid store `sump`; the gas store carries its own energy); faces 2; wetted links 1; phase rows 1 (condense); gas stores 1; gas paths 2; liquid stores 1; liquid mouths 1; ports 2; presentation bindings 1. Four colliders against the Ramp's one: foot, shelf and drip mouth are proud visible solids, and the shelf is the only face a sink can rest on.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Owner or evidence |
| --- | --- | --- | --- |
| TH-04 R1, R5 | Static colliders from art (box and tube are admitted shapes, `ColliderShapeKind`, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`); typed ports | present | as TH-04 (bodies read) |
| TH-17 W2 | One fluid port contract | narrower | as TH-17; owner ENGINE-GAS-NETWORK |
| TH-17 W4 | Rigid gas store, input and venting output, and the hose connection kind | **missing** | ENGINE-GAS-NETWORK |
| TH-17 W5 | Carrier plus vapour in one store | **missing** | ENGINE-GAS-NETWORK (the mixture of pneumatic P1) |
| C1 | A liquid store that is a thermal node, with a discharge mouth and a free-end stream, including a stream fed by a phase row below L17's 0.0005 m³/s zero-flow threshold (the condensate is at most 1.27 g/s ÷ 16 kg/m³ = 7.9 × 10⁻⁵ m³/s) | **missing** | ENGINE-LIQUID (L5, L8; the threshold exemption is a text gap, Batch N summary) |
| TH-17 W8 | Node, faces, touch-based contact link (T3, TH-04 R10), wetted link, ambient | **missing** | ENGINE-THERMAL (link table on ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS) |
| TH-17 W9 | The condense direction of the phase row, at a wetted link, reading the vapour's partial pressure, with the wall-limited rate | **missing** | ENGINE-THERMAL (the one wall-limited condense row of T5, in the story's phase-row text) |
| TH-04 R12 | Heat-flow presentation driver | **missing** | ENGINE-THERMAL |
| C2 | A colliding hose | **missing** | ENGINE-CONNECTION-MATERIALS (owner decision 1) |

Counts: present 2, untested 0, narrower 1, missing 7. Each missing row has one owner. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-18](../requirements.md#thermal-18): a cooled passage removes vapour enthalpy and collects conserved liquid; no variants.
- **Named entry** [thermal-18](../invest/named-elements.md#thermal-18): owner S582; "a warm or exhausted sink cannot condense an unlimited vapor stream".
- **Research** per-element table: "cold link + phase transition"; parameter conductance; "turns steam back into accounted water"; drip rate ← q.
- **Element map** row TH-18: EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, PhaseTopology, SensibleHeat, ThermalConduction, TopologyTransaction, VaporCondensation (D14).
- **Processes and scenarios.** [IX-25](../requirements.md#interaction-25), [IX-08](../requirements.md#interaction-08), [IX-18](../requirements.md#interaction-18); [TX-05](../requirements.md#thermal-scenario-05).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for condens, vapour and steam terms. The legacy gas material excluded phase transitions (`engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6`); recorded only.

## 5. Acceptance

Point of truth: [thermal-18](../requirements.md#thermal-18), [TX-05](../requirements.md#thermal-scenario-05) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Supply** ([chain 1](TH-17-kettle.md#batch-n-chains)). A default TH-17 kettle on the plate (Battery `capacity` 14 400 J; a default Battery's 3600 J never boils it) starts boiling at 53 s at 1.17 g/s, slowing as it empties; it has 1 g left at about 75 s. Its head keeps 1.18 g, so about 17.8 g has entered `GasIn` by then. The vapour arrives at about 373 K through the hose (which has no node).
- **Heat per gram condensed.** The vapour brings Cp_v × 373 = 430.1 J/g and the condensate keeps c × T_dew, so the condenser must take 430.1 − 1.046 × T_dew J/g: 81.6 J/g at the table floor (T_dew 333.2 K), 61.0 J/g at 353 K, 40.0 J/g at 373 K. With game-scale latent heat, much of this is the vapour's sensible heat; the earlier "0.36 g/s carrying 50.8 W of latent heat" (L = 141 062 J/kg) is withdrawn.
- **Capacity of a cold wall.** At 288 K the row can take 6 × (333.2 − 288) ÷ (107.2 × 333.2) = 271 ÷ 35 720 = 7.6 g/s, six times the 1.27 g/s bound, so a cold wall condenses everything that arrives and the dew point stays at the table floor. It keeps doing so while 6 × (333.2 − T_wall) ≥ 1.17 × 10⁻³ × 35 720 = 41.8 W, that is up to T_wall = 326.2 K.
- **Wall alone (finite sink).** Stage 1, wall 288 → 326.2 K: 7.5 × 38.2 = 287 J at 81.6 J/g, 3.5 g condensed in 3 s. Stage 2, wall 326.2 → 365.7 K (above 365.7 K the dew point would have to exceed 373 K, the most a passage at ambient pressure allows): 7.5 × 39.5 = 296 J at a mean of about 61 J/g plus about 30 J to the room, 5.4 g; meanwhile the passage fills with vapour to about 94 000 Pa, holding 0.004 × 94 000 ÷ (461.5 × 369) = 2.2 g. Stage 3, the wall saturated near 366 to 369 K for the 10 s until the kettle has 1 g left: condensation only replaces the room loss, 0.1 × 80 = 8 W at about 45 J/g, plus the last 3 K of wall rise: 2.3 g. Total about 11.2 g condensed, 2.2 g held, and 17.8 − 11.2 − 2.2 = 4.4 g out of `GasOut`. The held vapour condenses later as the wall cools.
- **Why 7.5 J/K and 0.1 W/K.** The wall alone must be exhausted inside one kettle charge so that the sink is the lesson. Under the 40 000 J/kg latent heat a 22.5 J/K wall would need 22.5 × 78 = 1.76 kJ to saturate, more than one charge brings (17.8 g at 61 to 82 J/g, about 1.2 kJ): it would peak near 345 K and pass under 0.5 g. The earlier 22.5 J/K is withdrawn.
- **With a sink** (the audit's cold-pack chain, redone under owner decision 19). A TH-29 Cold pack (437.5 J/K, authored 255 K, 1.2 W/K to the room, soft face 16 W/K) lies on the shelf from Run start: pair 64·16 ÷ 80 = 12.8 W/K. In under a second pack and wall settle at (437.5 × 255 + 7.5 × 288) ÷ 445 = 255.6 K; the room then warms both through 1.3 W/K with time constant 445 ÷ 1.3 = 342 s: 288 − 32.4 × e^(−53/342) = 260.3 K when the kettle boils. During the boil the condenser takes 17.8 g × 81.6 J/g = 1.45 kJ (the wall stays far below 326 K, so the dew point stays at the floor) and the room 1.3 × 26 × 22 = 0.74 kJ: the pack reaches about 265 K when the kettle has 1 g left, the wall about 5 K above it (66 W ÷ 12.8). Everything that arrives condenses. The pack has then taken about 2.1 kJ from the condenser and 2.6 kJ from the room, 4.7 kJ of its 437.5 × 18 = 7.9 kJ below 273 K. The audit's "259.5 K at the boil, 269 K at dry, 2.9 kJ from the condenser" was derived for the withdrawn latent heat.
- **Mass.** Vapour in = condensate (into `sump`, then captured below or `Spilled`) + vapour still in `passage` + vapour out. The rows read the condensate as the row's transfer into `sump`; where the stream lands does not change them.
- **Tick order.** T8: the row runs in the thermal step; condensate is in `sump` at the tick boundary and drains from the next tick's substeps (L16).
- **Tolerances.** T12. Liquid moved by the phase row is checked as mass on the stores' ledgers, in grams (0.5 g), not against L17's 0.004 m³ volume tolerance (audit C2).
- **Run length these rows need: 140 s** (A1 and A2 observe when the kettle has 1 g left, hand 75 s, stall guard 135 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `CondenserTests`. Rig: a TH-17 kettle on a TH-04 plate wired to a Battery with `capacity` 14 400 J, a hose from its `GasOut` to `GasIn`, an open store under the drip mouth.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the rig with a default Cold pack on the shelf from Run start, when the kettle has at most 1 g of water left (hand 75 s), then all vapour that entered `GasIn` has condensed but for the passage's content, under 0.5 g has left `GasOut`, and the pack reads below 273 K (hand 265 K) | `ASinkOnTheShelfKeepsItCondensing` (with TH-29; in TH-29's slice if that part is later) | the rig with a default Battery (`capacity` 3600 J): the kettle never boils and no condensate is made | the same pack lying on the bench beside the shelf, its near face 0.05 m from the shelf's front face and touching nothing of the condenser, gives no contact link and behaves as A2 | 0.5 g; ± 5 K; stall guard 135 s |
| A2 | Given the rig with nothing touching the condenser, when the kettle has at most 1 g left (hand 75 s), then between 8 and 15 g has condensed (hand 11.2 g), at least 3 g has left `GasOut` (hand 4.4 g), and the wall has passed 355 K (hand 366 K) | `AnExhaustedWallPassesTheRestAsVapour` | A1 | none | the stated band; stall guard 135 s |
| A3 | Given A2, then vapour in = condensate + vapour out + vapour held, and the wall's heat gain plus the ambient account equals the enthalpy brought in less the condensate's and the vented gas's enthalpy and the passage's change (T13) | `CondensateAndHeatBalance` | none | none | 0.5 g; 2% of the heat moved or 5 J |
| A4 | Reset restores a 288 K wall, an empty sump and ambient air; a second Run repeats A2 | `ResetRestoresWallSumpAndAir` | none | none | as A2 |
| A5 | Save and load keep pose and the hose; no run state | `SaveAndLoadKeepConnections` | none | none | canonical field values |
| A6 | An electrical or activation link to `GasIn`, or output to output, is refused | generic: TH-04 R5 once Fluid links are validated | — | — | typed refusal |

The band in A2 is the hand estimate's stage-by-stage uncertainty (about 30%); its lower edge of vapour out, 3 g, is what shows the requirement's control: an exhausted sink passes vapour. There are no parameters, so no parameter-refusal row. The requirement's control ("a warm or exhausted sink cannot condense an unlimited vapor stream") is A2, with A1's boundary.

## 6. Open questions

Resolved:

1. **Internal sink.** The wall is a small finite sink (7.5 J/K), decided now so that it is exhausted within one kettle charge under the 40 000 J/kg latent heat (section 5).
2. **Saturation.** The game-steam table of T11, clamped below 20 000 Pa; condensing reads the vapour's partial pressure.
3. **Fluid ports.** `GasIn` and `GasOut` of the gas family (F3); the liquid mouth is the liquid family's `End`; one fluid port contract for both (coordinator decision 16, ENGINE-GAS-NETWORK).
4. **Drip destination.** The liquid family's free-end rule (D3, D4): one liquid inventory.
5. **Game scale.** D5 and owner decision 19, TH-04 section 11.
6. **Where a sink goes.** On the shelf (audit C3), decided now: the foot's top is covered by the channel and a dynamic pack leaning on a side face would not stay touching.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-17, TH-35 (sources); TH-19, TH-20 (exhaust return, TX-05); TH-29 (sink, a dynamic pack on the shelf); any open liquid store below. A Finned sink (TH-10) or a heat pump face (TH-12) on the shelf is a static-on-static pair that T3's list now names (TH-04 section 11), so either may serve as the condenser's sink; the rows here use the Cold pack. Every vapour connection is a hose: a solid strand (owner decision 1).
- **Campaign.** "Dew Process": introduction 74, practice 75; reuse 87, 137; each such level declares a Run length of at least 140 s and a 14 400 J Battery. A fixed condenser needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-SUPPLY-DEMAND among it); ENGINE-GAS-NETWORK; ENGINE-LIQUID (with the zero-flow exemption of C1); ENGINE-THERMAL as amended; ENGINE-CONNECTION-MATERIALS (solid hose); ENGINE-RUN-LENGTH; TH-04 and TH-17; S7.
- **Files.** `elements/Condenser/Condenser.csproj`, `CondenserElementDeclaration.cs`, `condenser.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/CondenserTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it.
- **Review tier.** Engine, unless an earlier slice has proved the condense row with its wall-limited rate (TH-17's own rows do not exercise it, TH-17 section 5; TH-19's return row does); then data-only.
- **Disqualifiers.** A phase row that cannot be limited by a wetted link's heat flow or that moves other than m·Cp_v·T_dew; a gas store without a vapour component; a liquid store that cannot be written by a phase row; a condensate stream dropped by L17's zero-flow threshold.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Four static colliders, one gas store with two gas paths, one liquid store (also a node) with a mouth, one node with two faces, one wetted link with the condense row, two ports, one binding.
2. **Generic laws.** Q̇ = G·ΔT; the condense row; the liquid free-end rule. "Turns steam into water" is heat leaving vapour through a declared link into a finite node.
3. **Three layers.** Colliders: channel, foot, shelf, drip tube. Visuals: their art; a printed window and port marks. Animation: the window colour from committed heat flow.
4. **Flow and typing.** Two Fluid ports and one liquid mouth; f32; enum roles.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static; four primitive colliders; two nodes, one link, one row and one link-table entry per touching face per tick (T10). No island sleeping is assumed; none exists in the engine today.
7. **Tests are scenarios.** A1 to A5.

**Verdict: Conforms; blocked on ENGINE-GAS-NETWORK, ENGINE-LIQUID, ENGINE-THERMAL, ENGINE-CONNECTION-MATERIALS and ENGINE-RUN-LENGTH and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Not written as main's binding section. **Blocked on the coverage follow-on story S7:** main's reader (`tools/Coverage/NamedPackageSources.cs`) lists only `EL-*.md` files and admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `condenser` |
| Named identity | TH-18, `../invest/named-elements.md#thermal-18` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-452` and the ledger entry has no todo link |
| Decision | S582; `scope-corrections.md` on published main has no `s582` block |
| Mode records | `Configuration=Fixed` |
