# TH-02 · Fire bowl — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds fire-bowl values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-02 · Fire bowl |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-02](../requirements.md#thermal-02) (sequence-task-436); [named-elements.md#thermal-02](../invest/named-elements.md#thermal-02) |
| Proof owner / research | S597 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | No CAT spec. Contents may be [TH-03 Combustible block](TH-03-combustible-block.md) or [TH-32 Tinder pad](TH-32-tinder-pad.md); shares the combustion model with [TH-01 Candle](TH-01-candle.md); lit through tinder by [TH-31](TH-31-flint-striker.md) or [TH-33](TH-33-spring-mounted-match.md), or by another flame. |
| Campaign | Intro 57; practice 58; reuse 85, 140 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. The bowl's contents are an authored fuel-bed charge, either `Empty` or `Charged` (proposed: the row's control is an empty bowl, so both states need placement-time proof); a charged bowl also accepts any placed combustible body resting in it.
- **Bodies and shapes.** One static body. Base box, half-extents 0.20 × 0.03 × 0.20 m, and four rim boxes, each half-extents 0.20 × 0.06 × 0.02 m, around it (proposed: no hollow-bowl collider exists, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`; a 0.4 m shallow tray reads as a bowl beside the 0.68 m Basketball). Fuel bed region, full size 0.30 × 0.05 × 0.30 m inside the rim. Plume region, full size 0.30 × 0.80 × 0.30 m above the bed (proposed: wider and taller than the candle's 0.20 × 0.60 m, matching "hotter, wider heat" in the research table).
- **Mass and material.** Static rigid mass is zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Bowl thermal node 1.0 kg stone, c = 210 J/(kg·K) (ceramic 840 × ¼, batch scale). Fuel bed 1.5 kg wood chips, c = 425 J/(kg·K) (wood 1700 × ¼). Bed surface: the batch wood patch node (0.5 g, 0.21 J/K), which needs about 60 J to reach ignition, so a flame or burning tinder lights it and a single ≈ 2 J strike does not. Stone face 16 W/K. Contact friction 0.6, restitution 0.05 (proposed: matte solid like the Domino, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`).
- **Constraints.** None.
- **Typed ports.** None. Heat leaves by plume, radiation and contact; contents sit by ordinary contact, not by a socket.
- **Sensors and activation.** None. The bowl is not an intrinsic heat source: reaction belongs to the contents. Bed ignition at 573 K (proposed: piloted wood ignition is about 570–620 K); extinction below 520 K, at zero fuel, or below 0.15 local O₂ fraction (proposed: hysteresis and the diffusion-flame oxygen limit).
- **Work and energy stores.** Fuel bed 1.5 kg × 15,625 J/kg = 23,438 J (batch scale). Reaction power 256 W (proposed: about three candles, "hotter, wider heat", well below a real wood fire's kilowatts) → 92 s burn. Split 230 W plume, 18 W thermal radiation, 8 W radiant light (proposed: about 90 % convective, as small wood fires). Oxidiser 1.2/1024 kg O₂ per kg wood: 19 mg/s. Residue: 10 % of bed mass remains as inert ash (proposed: S556 expects visible residue).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Contents | enum `Empty`/`Charged` | closed set | `Charged` | — | (proposed: see Variants) |
  | Fuel-bed mass | f32 | 0.25–2.0 | 1.5 | kg | (proposed: 15–122 s burn at 256 W) |
  | Reaction power | f32 | fixed | 256 | W | (proposed: above) |

- **Cosmetic curves and UI bindings.** Flame ← committed reaction power ([research per-element table](../../thermal-component-research.md)); fuel-bed height ← remaining fuel (proposed: the row's "visible fuel bed"). Navy rim marks show fill level without colour alone.
- **Art.** "Shallow cream stone bowl, visible fuel bed and sparse navy rim marks" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), navy `#293954` (`DESIGN.md@a6c914e:L147-L147`), warm wood fuel `#b77c42` (`DESIGN.md@a6c914e:L146-L146`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.FireBowl`; catalogue id `fire_bowl`, title "Fire bowl", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-02 in [general-engine-element-map](../general-engine-element-map.md), binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ChemicalReaction, Extinction, FiniteLedger, GeometryQuery, Ignition, SensibleHeat, TopologyTransaction; the JSON also lists StateTransaction.

- **Exists now.** Static bodies and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); finite paid work in joules, partial FiniteLedger (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L38`); Run/Reset checkpoint and save codec (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Thermal families under decision owner [S543](../invest/decisions.md#s543): reaction → S554, ignition → S555, extinction → S556, phase-topology for bed consumption → S565, conduction from contents to bowl → S544. SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Composition gaps.** As for TH-01, the row omits ThermalConvection (S545), ThermalRadiation (S546) and GasState/EnvironmentState for oxidiser ([S470](../invest/decisions.md#s470) → S471; [S635](../invest/decisions.md#s635)).
- **Dependencies.** Tinder (TH-32) and an igniter (TH-31, TH-33), or a flame; a receiver such as [TH-17 Kettle](TH-17-kettle.md) for TX-01.

## 4. Sources and legacy

- **Requirement row** [thermal-02](../requirements.md#thermal-02): vessel without intrinsic heat; reaction belongs to contents and surrounding gas; no variants listed.
- **Named entry** [thermal-02](../invest/named-elements.md#thermal-02): owner S597; exact outcome "an empty bowl or inadequate oxidizer cannot sustain flame".
- **Research** [TH-S03](../../thermal-component-research.md#th-s03) and per-element table: "larger fuel source + reaction state"; parameters power, fuel, oxidiser; "smothering a lid puts it out"; flame ← power.
- **Processes and scenarios.** [IX-28](../requirements.md#interaction-28), [IX-29](../requirements.md#interaction-29), [IX-30](../requirements.md#interaction-30); [TX-01](../requirements.md#thermal-scenario-01), [TX-14](../requirements.md#thermal-scenario-14).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for bowl, fire, fuel, combustion and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows for thermal identities).

## 5. Acceptance outline

Point of truth: [thermal-02](../requirements.md#thermal-02) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers are derived in the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and gizmo, place a charged Fire bowl, a Kettle (TH-17) in its plume, a Tinder pad (TH-32) on the bed, and a Flint striker (TH-31) aimed at the tinder and pushed through its swing by a Basketball released 1.0 m up a Ramp.
- **Positive.** The strike lights the tinder; its 16 W flame heats the 0.21 J/K bed patch past 573 K in about 4 s; the bed burns at 256 W. The 0.2 m kettle covers 44 % of the 0.09 m² plume and receives 102 W: it reaches 373 K after about 38 s and emits the first 2 g of vapour about 5 s later, inside the 92 s burn. Fuel debit equals heat plus light; the bed height falls and ash remains.
- **Negative or control.** `Empty` bowl with the same tinder and strike: the tinder burns out and no sustained flame follows. Inadequate oxidiser (restricted vent, after TX-14 support): the flame cannot sustain. A hot bowl with no contents never re-ignites.
- **Boundaries.** Bed patch at 572 K does not ignite; a 2 J strike on the bed alone does not ignite it; fuel 0 cannot burn; ash cannot re-ignite; ledger never negative.
- **Run/Reset.** Restores fuel mass, bed and bowl temperatures, flame state and contents exactly.
- **Save/Load.** Construction (pose, contents) round-trips; Run after reload reproduces the outcome.
- **Integrations.** TX-01 with TH-17; TX-14; campaign 57, 58, 85, 140.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Contents.** Is the fuel bed an authored charge, a separately placed combustible part, or both? Owner decision.
3. **Lid.** The research table mentions smothering with a lid; no lid identity exists. Owner decision.
4. **Oxygen model.** Boolean oxidiser flag (research) versus finite gas transport (TX-14). Owner decision (S554/S555).
5. **Bowl collider.** Five boxes approximate the bowl until a hollow collider exists. Owner decision.
