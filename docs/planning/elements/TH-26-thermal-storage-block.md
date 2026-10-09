# TH-26 · Thermal storage block — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds storage-block values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-26 · Thermal storage block |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-26](../requirements.md#thermal-26) (sequence-task-460); [named-elements.md#thermal-26](../invest/named-elements.md#thermal-26) |
| Proof owner / research | S569 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | No CAT spec. Dynamic box like [CAT-023 Domino](CAT-023-domino.md). The "stored-hot block" heat source of TX-01 (with [TH-17 Kettle](TH-17-kettle.md)); the noncombustible control for [TH-03](TH-03-combustible-block.md); the hot store of TX-11 with [TH-30](TH-30-thermoelectric-generator.md). Latent sibling [TH-27](TH-27-phase-change-cartridge.md); cold sibling [TH-29](TH-29-cold-pack.md). |
| Campaign | Intro 81; practice 82; reuse 90, 140 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. Initial temperature is an authored parameter (proposed: TX-01 needs a "stored-hot block", and charging in-Run takes about 350 s).
- **Bodies and shapes.** One dynamic body, box collider, half-extents 0.30 × 0.20 × 0.20 m (proposed: a 0.6 m prism that sits on a heating plate and can be carried by a conveyor).
- **Mass and material.** 2.0 kg (proposed: "heavy", between the 1 kg Basketball and the 4 kg Bowling ball). Ceramic c = 210 J/(kg·K) (ceramic 840 × ¼, batch scale; 420 J/K total). Ceramic face 16 W/K (batch scale): it pairs at 12.8 W/K with a metal base and 8 W/K with another ceramic body. Surface exchange 2.5 × 1.28 m² = 3.2 W/K (batch scale; cooling time constant about 130 s, so it "stays warm"). Contact friction 0.6, restitution 0.05 (proposed: matte ceramic like the Domino, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`).
- **Constraints.** None.
- **Typed ports.** None as sockets; heat enters and leaves through contacts.
- **Sensors and activation.** None.
- **Work and energy stores.** Sensible enthalpy H = m·c·(T − 288 K) (IX-43). A block authored at 400 K holds 420 × 112 ≈ 47 kJ above ambient (derived), enough for several deliveries that each reduce the stored difference. Delivered heat q = G·(T_block − T_receiver), zero when equal; the block also loses 3.2 W/K to the room throughout. Charging on a 512 W plate (12.8 W/K pair): the block heads for about 406 K, where its own 3.2 W/K loss and the plate's 0.9 W/K balance the 512 W, so 288 K to 400 K takes about 350 s (derived).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Initial temperature | f32 | 288–500 | 288 | K | (proposed: above; 500 K stays below wood ignition) |
  | Mass | f32 | fixed | 2.0 | kg | (proposed: above) |
  | Specific heat | f32 | fixed | 210 | J/(kg·K) | (batch scale: ceramic) |
  | Face conductance | f32 | fixed | 16 | W/K | (batch scale: ceramic face) |

- **Cosmetic curves and UI bindings.** Tint ← temperature ([research per-element table](../../thermal-component-research.md)), always paired with the gold graduated energy marks (row).
- **Art.** "Heavy cream ceramic prism with gold graduated energy marks" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.ThermalStorageBlock`; id `thermal_block`, title "Thermal storage block", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-26, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): FiniteLedger, GeometryQuery, SensibleHeat, ThermalConduction; JSON adds StateTransaction.

- **Exists now.** Dynamic box body with derived inertia (Story 5.1; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`, `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L17`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** [S543](../invest/decisions.md#s543) conduction → S544; convection loss → S545 (not in the map row). SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A heat source to charge it (TH-04) or an authored hot start; a receiver (TH-17, TH-37, TH-30).

## 4. Sources and legacy

- **Requirement row** [thermal-26](../requirements.md#thermal-26): high-heat-capacity body stores sensible enthalpy and is transported to release it by ordinary exchange; no variants.
- **Named entry** [thermal-26](../invest/named-elements.md#thermal-26): owner S569; "an equal-temperature block supplies no net heat; repeated deliveries deplete its stored difference".
- **Research** per-element table: "high-capacity node"; parameters mass, c; "stays warm after the source stops"; tint ← T.
- **Processes and scenarios.** [IX-43](../requirements.md#interaction-43), [IX-18](../requirements.md#interaction-18); [TX-01](../requirements.md#thermal-scenario-01), [TX-11](../requirements.md#thermal-scenario-11).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for storage, enthalpy and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-26](../requirements.md#thermal-26) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers are derived in the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and gizmo: a block authored at 400 K on a Conveyor (CAT-019) that carries it against a Heat-sensitive target (TH-37, threshold 330 K).
- **Positive.** The block cools by its own 3.2 W/K loss during about 5 s of transport, then the 8 W/K ceramic pair brings the target past 330 K about 3 s after contact; the target peaks near 367 K about 15 s after contact and stays above 330 K for over a minute while block and target cool together; heat delivered = block enthalpy drop − block surface loss. In TX-01 the same block brings the TH-17 kettle to its first 2 g of vapour in about 7 s.
- **Negative or control.** A block at 288 K delivers nothing to a 288 K target. Three successive targets: each receives less as the stored difference depletes.
- **Boundaries.** The block never cools below its coldest contact or ambient; total energy is conserved across transport.
- **Run/Reset.** Restores authored initial temperature and pose exactly.
- **Save/Load.** Pose and initial temperature round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-01 source swap; TX-11 with TH-30; campaign 81, 82, 90, 140.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Authored hot start.** May a level author the initial temperature, or must every block be charged in-Run (about 350 s on the plate, approaching 406 K)? Owner decision.
3. **Surface loss.** The map row omits ThermalConvection; confirm ambient loss is modelled. Owner decision.
