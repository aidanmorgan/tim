# TH-16 · Fusible link — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds fusible-link values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-16 · Fusible link |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-16](../requirements.md#thermal-16) (sequence-task-450); [named-elements.md#thermal-16](../invest/named-elements.md#thermal-16) |
| Proof owner / research | S580 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | No CAT spec. Load paths: [CAT-058 Rope anchor](CAT-058-rope_anchor.md) and [CAT-067 Weight](CAT-067-weight.md). Heat sources [TH-01](TH-01-candle.md), [TH-04](TH-04-electrical-heating-plate.md). Neighbour: [RAD-21](../invest/named-elements.md#radiation-21) is a radiation-weakened latch, a different identity. |
| Campaign | Intro 85; practice 86; reuse 96, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** Two dynamic gold load-end bodies, each a box of half-extents 0.06 × 0.06 × 0.06 m, joined end to end by the collar; overall length 0.30 m (proposed: a short, readable connector between a rope and a hung weight).
- **Mass and material.** Load ends 0.1 kg each (proposed: light relative to the 1 kg-scale loads they carry). Collar: 0.02 kg wax-like solid, c = 625 J/(kg·K), fusion 12,500 J/kg at 330 K (paraffin 2500 × ¼ and 200,000 × 1/16 under the batch scale; matches the row's "wax-like collar"). The collar is a thermal-only node outside the ends' rigid mass. Load ends: metal faces 64 W/K; collar ambient exchange 2.5 × 0.03 m² ≈ 0.08 W/K.
- **Constraints.** One fixed joint between the two ends, carrying the load through the collar. Its breaking capacity is 98 N × (1 − melted fraction) and is never higher than at 288 K (proposed: holds a 10 kg game load cold, far above catalogue masses). When the transmitted load exceeds capacity the joint severs through the shared joint lifecycle; a severed joint never reattaches.
- **Typed ports.** Two mechanical attachment points (one per end) for rope or hook connections (proposed: the rope-port roles from [S257](../invest/decisions.md#s257) rope-port → S688).
- **Sensors and activation.** None. No temperature switch: release follows only strength versus load.
- **Work and energy stores.** Collar node: mass, enthalpy, melted fraction. Below 330 K strength stays full. Releasing the link needs 12.5 × 42 = 525 J to reach 330 K plus up to 250 J of latent heat (derived). Melted wax stays as residue mass on the ends (proposed: conserved, not deleted). Severing releases stored elastic load into motion, accounted by the joint.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Melt temperature | f32 | fixed | 330 | K | (proposed: above) |
  | Cold breaking capacity | f32 | fixed | 98 | N | (proposed: above) |
  | Collar mass | f32 | fixed | 0.02 | kg | (proposed: above) |

- **Cosmetic curves and UI bindings.** Link snap ← committed joint state ([research per-element table](../../thermal-component-research.md)); the navy witness gap widens with melted fraction (row).
- **Art.** "Small cream wax-like collar between gold load ends with a navy witness gap" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), navy `#293954` (`DESIGN.md@a6c914e:L147-L147`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.FusibleLink`; id `fusible_link`, title "Fusible link", category Ropes (proposed: it connects rope loads; Heat is the alternative).

## 3. Engine capabilities

Families (map row TH-16, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, SensibleHeat, SolidMelting, StructuralFracture, TemperatureStrength, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Dynamic box bodies (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Joints and rope: Story 6.4 constraints, Story 10.2 rope. Breakable joint lifecycle: [S543](../invest/decisions.md#s543) fracture → S563, temperature-strength → S566, melting → S547, phase-topology → S565. All thermal rows unscheduled.
- **Dependencies.** Rope anchor (CAT-058) and Weight (CAT-067), Stories 10.2 and 10.4; a heat source.

## 4. Sources and legacy

- **Requirement row** [thermal-16](../requirements.md#thermal-16): finite connector loses strength through its phase/constitutive state; the shared joint/contact lifecycle releases load; no variants.
- **Named entry** [thermal-16](../invest/named-elements.md#thermal-16): owner S580; "subthreshold heating retains support; cooling broken material does not reattach a severed graph".
- **Research** per-element table: "thermal sensor + constraint breaking"; parameter melt T; "breaks a held load above its melt point"; link snap ← state.
- **Processes.** [IX-42](../requirements.md#interaction-42), [IX-21](../requirements.md#interaction-21), [IX-03](../requirements.md#interaction-03), [IX-37](../requirements.md#interaction-37).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for fusible, link, melt and break terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-16](../requirements.md#thermal-16) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and rope controls: Rope anchor → rope → Fusible link → Weight (CAT-067) hanging above a Receiver; a lit Candle (TH-01) under the collar.
- **Positive.** The 0.12 m link covers about 36 % of the candle's 0.2 m plume and takes about 27 W; the collar reaches 330 K after about 20 s and starts melting; capacity falls below the weight's load and the joint severs (about 30 s in all), dropping the weight to the Receiver.
- **Negative or control.** Candle under the weight instead: subthreshold collar keeps support. After severing, removing the candle and cooling never reattaches the link.
- **Boundaries.** Collar at 329 K keeps full capacity; a weight lighter than the residual capacity stays hung until capacity falls below it.
- **Run/Reset.** Restores intact joint, cold collar, residue-free ends and weight pose exactly.
- **Save/Load.** Pose and rope connections round-trip; Run after reload reproduces the outcome.
- **Integrations.** Rope family; campaign 85, 86, 96, 149.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Category.** Ropes or Heat drawer. Owner decision.
3. **Strength curve.** Linear in melted fraction versus a softening curve below the melt point. Owner decision (S566).
4. **Residue.** Does melted wax drip as a separate material or stay on the ends? Owner decision.
