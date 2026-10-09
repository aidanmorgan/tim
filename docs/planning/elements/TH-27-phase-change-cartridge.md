# TH-27 · Phase-change storage cartridge — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds phase-change-store values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-27 · Phase-change storage cartridge |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-27](../requirements.md#thermal-27) (sequence-task-461); [named-elements.md#thermal-27](../invest/named-elements.md#thermal-27) |
| Proof owner / research | S589 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | No CAT spec. Sensible sibling [TH-26 Thermal storage block](TH-26-thermal-storage-block.md); cold store [TH-29](TH-29-cold-pack.md). Charged by [TH-04](TH-04-electrical-heating-plate.md); discharges to [TH-37](TH-37-heat-sensitive-target.md) or any receiver. |
| Campaign | Intro 84; practice 85; reuse 97, 147 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. Initial phase is an authored enum `Discharged` (solid at 288 K, default) or `Charged` (liquid at 303 K) (proposed: lessons may start either way; each is proved).
- **Bodies and shapes.** One dynamic body, box collider, half-extents 0.25 × 0.12 × 0.12 m (proposed: a 0.5 m capsule that can be carried and placed on a plate). The container geometry never changes with phase: "the container retains mass".
- **Mass and material.** Total 0.6 kg: shell 0.1 kg aluminium, c = 225 J/(kg·K) (aluminium × ¼, batch scale; 22.5 J/K); fill 0.5 kg paraffin-type phase-change material, c = 625 J/(kg·K) both phases (paraffin 2500 × ¼; 312.5 J/K), latent heat 12,500 J/kg (paraffin 200,000 × 1/16), melting at 303 K (proposed: under the batch scale paraffin's latent-to-sensible ratio is L/c = 20 K, so the transition must sit within about 18 K of ambient for latent storage to exceed the sensible heat needed to reach it). Shell: metal face 64 W/K (batch scale), pairing at 32 W/K with a heating plate and 12.8 W/K with a ceramic target. Ambient exchange 2.5 × 0.6 m² = 1.5 W/K.
- **Constraints.** None.
- **Typed ports.** None as sockets; the thermal interface is the shell contact faces.
- **Sensors and activation.** None.
- **Work and energy stores.** One node with enthalpy and liquid fraction f ∈ [0, 1]. Sensible heat from 288 K to 303 K: 335 J/K × 15 K ≈ 5.0 kJ; latent capacity 0.5 × 12,500 = 6.25 kJ, so latent exceeds sensible (derived). While 0 < f < 1 the fill holds 303 K (IX-35). A fully liquid cartridge stores no more latent heat; a fully solid one releases no more. Charge on a 512 W plate: during melting the plate settles near 318 K and delivers about 480 W through the 32 W/K pair; the 11.3 kJ charge plus about 1.8 kJ of plate warm-up completes in about 28 s (within 40 s).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Initial phase | enum `Discharged`/`Charged` | closed set | `Discharged` | — | (proposed: above) |
  | Transition temperature | f32 | fixed | 303 | K | (proposed: above) |
  | Fill mass | f32 | fixed | 0.5 | kg | (proposed: above) |
  | Latent heat | f32 | fixed | 12,500 | J/kg | (batch scale: paraffin) |

- **Cosmetic curves and UI bindings.** Tint ← temperature ([research per-element table](../../thermal-component-research.md)); the cyan phase-fraction window ← committed f (row), not colour alone.
- **Art.** "Cream capsule with cyan phase-fraction window and navy endpoint symbols" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`), navy `#293954` (`DESIGN.md@a6c914e:L147-L147`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.PhaseChangeCartridge`; id `phase_cartridge`, title "Phase-change cartridge", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-27, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GasState, GeometryQuery, LiquidFreezing, PhaseStorage, PhaseTopology, SensibleHeat, SolidMelting, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Dynamic box body (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** [S543](../invest/decisions.md#s543): phase-storage → S561, melting → S547, freezing → S548, conduction → S544. SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A charging source (TH-04) and a receiver.

## 4. Sources and legacy

- **Requirement row** [thermal-27](../requirements.md#thermal-27): encapsulated material stores latent enthalpy while transitioning; container retains mass and exposes a thermal interface; no variants.
- **Named entry** [thermal-27](../invest/named-elements.md#thermal-27): owner S589; "a fully transitioned cartridge cannot repeat a charge/discharge without reversing the energy transfer".
- **Research** per-element table: "node + latent store"; parameter latent heat; "holds temperature during a transition"; tint ← T.
- **Processes.** [IX-35](../requirements.md#interaction-35), [IX-21](../requirements.md#interaction-21), [IX-22](../requirements.md#interaction-22), [IX-18](../requirements.md#interaction-18).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for phase, latent, paraffin and storage terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-27](../requirements.md#thermal-27) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and gizmo: a `Discharged` cartridge on a supplied Heating plate (TH-04), with a Temperature sensor (TH-21) probe on it; later the cartridge moved onto a Heat-sensitive target (TH-37, threshold 300 K).
- **Positive.** The cartridge warms to 303 K, plateaus while f rises to 1, and is fully charged after about 28 s; on the target it holds near 303 K as f falls, keeping the target above 300 K for about 150 s against its 1.35 W/K loss and the cartridge's own 1.5 W/K loss; latent in = latent out within the envelope.
- **Negative or control.** A `Charged` cartridge left on a running plate takes no more latent heat (only sensible). A fully discharged cartridge cannot discharge again without recharging.
- **Boundaries.** f stays in [0, 1]; plateau temperature equals 303 K while 0 < f < 1; mass never changes.
- **Run/Reset.** Restores the authored initial phase with its temperature (`Discharged`: solid at 288 K; `Charged`: liquid at 303 K) and pose exactly.
- **Save/Load.** Pose and initial phase round-trip; Run after reload reproduces the outcome.
- **Integrations.** Lesson 84 with ratchet/flywheel objectives kept separate; campaign 84, 85, 97, 147.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Transition temperature.** The scale's L/c ratio forces a transition near ambient (303 K); a hotter set point would store less latent than sensible heat. Accept, or choose a material with a larger latent heat. Owner decision.
3. **GasState in the row.** The map row lists GasState; the sealed cartridge needs none. Owner decision.
4. **Hot-start authoring.** Is `Charged` at Run start acceptable, or must it be charged in-Run? Owner decision.
