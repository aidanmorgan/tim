# TH-06 · Converging lens — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds converging-lens values; the frame and aperture follow the legacy colour filter, as for EL-173. Every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-06 · Converging lens |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-06](../requirements.md#thermal-06) (sequence-task-440); [named-elements.md#thermal-06](../invest/named-elements.md#thermal-06) |
| Proof owner / research | S518 · [TH-S04 optical heating](../../thermal-component-research.md#th-s04) |
| Related identities | **Refines the existing lens identity**: the optical lens family of [EL-173 Diverging lens](EL-173-diverging-lens.md) (Batch K, owner S519); frame, aperture and retention are aligned with it. Historical records [todo-304](../requirements.md#todo-304) (shared with [TH-37](TH-37-heat-sensitive-target.md), plus the integration task [sequence-task-402](../requirements.md#sequence-task-402)) and [todo-196](../requirements.md#todo-196) (shared with EL-155, EL-210, TH-01, TH-03). Frame family [CAT-055 Red filter](CAT-055-red_filter.md). Sources: [CAT-029 Flashlight](CAT-029-flashlight.md), [CAT-036 Laser](CAT-036-laser.md); targets [TH-07](TH-07-solar-absorber-plate.md), [TH-32](TH-32-tinder-pad.md), TH-37. |
| Campaign | Intro 59; practice 60; reuse 85, 142 ([thermal allocation](../requirements.md#thermal-campaign-allocation)); optical lens lesson 51–60 retained. |
| Roadmap story | Unscheduled. Epic 13 builds emitters and mirrors but no lens. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row. One focal length; the diverging lens is the separate identity EL-173.
- **Bodies and shapes.** One static body using the legacy filter frame (aligned with EL-173): cream bars of full size 0.18 × 0.18 × 1.65 m at (0, ±0.74, 0) and 0.18 × 1.3 × 0.18 m at (0, 0, ±0.74), and a navy base 0.9 × 0.2 × 1.65 m at (0, −1, 0) (`parts/ColourFilterPart.cs@a6c914e:L20-L29`; `AddBox` takes full sizes and stores halves, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). Lens body collider, full size 0.12 × 1.3 × 1.3 m at the origin, which collides with bodies but does not occlude light (proposed: EL-173's lens body, so the family shares one frame). The player moves the lens with the gizmo to focus.
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Lens thermal node 0.3 kg glass, c = 210 J/(kg·K) (glass 840 × ¼, batch scale). Transmission retention 0.95 (proposed: the legacy mirror retention, `engine/OpticalNetwork.cs@a6c914e:L33-L33`, shared with EL-173); the 0.05 not transmitted is absorbed into the lens node.
- **Constraints.** None.
- **Typed ports.** Optical aperture `OpticalPortId.Main` (`engine/OpticalNetwork.cs@a6c914e:L8-L8`), centre (0, 0, 0), normal −X like the filter, disc radius 0.65 m (`parts/ColourFilterPart.cs@a6c914e:L11-L12`), two-sided, interaction `Converge`, a proposed new member of the closed `OpticalInteraction` enum (`engine/OpticalNetwork.cs@a6c914e:L9-L9`), beside EL-173's proposed `Diverge`. Not an electrical socket ([optical connections](../requirements.md#sequence-task-281)).
- **Sensors and activation.** None.
- **Work and energy stores.** None of its own beyond the lens node; it redistributes power. Focal length f = 1.0 m (proposed: places the image of a source 1.5 m away at 3.0 m, inside the 8 m flashlight range, `parts/FlashlightPart.cs@a6c914e:L11-L11`). For a cone source treated as a point at its emitter (the legacy emitter model), the image lies at v = u·f/(u − f): u = 1.5 m gives v = 3.0 m. At u = 1.5 m the 15° cone has radius 0.40 m, inside the 0.65 m aperture, so the whole beam is collected. Finite minimum spot diameter 0.13 m (proposed: one tenth of the 1.3 m aperture keeps irradiance finite). Transmitted power never exceeds 0.95 × incident.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Focal length | f32 | fixed | 1.0 | m | (proposed: above) |
  | Aperture radius | f32 | fixed | 0.65 | m | legacy filter aperture |
  | Transmission retention | f32 | fixed | 0.95 | fraction | (proposed: legacy mirror retention) |
  | Minimum spot diameter | f32 | fixed | 0.13 | m | (proposed: above) |

- **Cosmetic curves and UI bindings.** None (static), per the [research per-element table](../../thermal-component-research.md). UI: "faint selected-only finite focus footprint" (row), drawn from the same optical solve, never a separate rule.
- **Art.** "Translucent cyan disc in a cream sculptural frame" (row). Frame cream `#fff8e9` and navy foot `#293954` from the legacy filter (`parts/ColourFilterPart.cs@a6c914e:L25-L28`); cyan `#66b8c9` lens at alpha 0.3 (proposed mapping, as EL-173; `DESIGN.md@a6c914e:L172-L172`), with convex cream rim marks distinguishing it from EL-173's concave marks.
- **Catalogue and inventory entry.** New `WorkshopPartKind.ConvergingLens`; id `converging_lens`, title "Converging lens", category Optics (proposed: the existing category for optical parts).

## 3. Engine capabilities

Families (map row TH-06, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ChemicalReaction, FiniteLedger, GeometryQuery, Ignition, OpticalAbsorption, OpticalTransport, SensibleHeat, TopologyTransaction; JSON adds StateTransaction. The map's composition note: the lens transports and refocuses finite power; ignition and reaction belong to the target, not the lens.

- **Exists now.** Static body and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** OpticalTransport: Stories 13.1 and 13.5 ([S484](../invest/decisions.md#s484) finite-colour → S485, optical-commit → S486, optical-law → S488). Finite-width coverage before any lens ([todo-309](../requirements.md#todo-309), as EL-173). OpticalAbsorption → S489. Refraction and focusing: no S484 row covers them (see Open questions). Light-transparent solid collider: Story 13.4. Thermal: [S543](../invest/decisions.md#s543) for the receiving body. Unscheduled.
- **Dependencies.** A finite optical source (CAT-029, CAT-036); an absorbing target (TH-07, TH-32, TH-37).

## 4. Sources and legacy

- **Requirement row** [thermal-06](../requirements.md#thermal-06): finite-width refraction concentrates irradiance on real surfaces without multiplying incident power; heating is absorption in the receiver; no variants.
- **Named entry** [thermal-06](../invest/named-elements.md#thermal-06): owner S518; "defocus, occlusion or inadequate source power fails to reach ignition conditions".
- **Research** [TH-S04](../../thermal-component-research.md#th-s04) and per-element table: "optical refraction + irradiance concentration"; parameter focal length; no animation.
- **EL-173 spec** ([EL-173](EL-173-diverging-lens.md)): frame, `Main` aperture, 0.95 retention and the area-share coverage law adopted for the family.
- **Processes and scenarios.** [IX-13](../requirements.md#interaction-13), [IX-14](../requirements.md#interaction-14); [TX-03](../requirements.md#thermal-scenario-03); the lens → heat-sensitive latch integration [sequence-task-402](../requirements.md#sequence-task-402).
- **Legacy (no lens; frame and mechanisms only).**

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Filter frame geometry, transparent pane collider and `Main` aperture radius 0.65 with interaction `Filter`. | `parts/ColourFilterPart.cs@a6c914e:L11-L12`, `parts/ColourFilterPart.cs@a6c914e:L20-L29` | Carry forward as the lens frame and aperture. |
  | 2 | Closed enums `OpticalPortId` and `OpticalInteraction` (Absorb, Mirror, Split, Filter, Route; no refraction). | `engine/OpticalNetwork.cs@a6c914e:L8-L9` | Carry forward the closed-enum pattern; add `Converge`. |
  | 3 | Mirror retention 0.95; passive branches divide rather than duplicate power. | `engine/OpticalNetwork.cs@a6c914e:L26-L27`, `engine/OpticalNetwork.cs@a6c914e:L33-L33` | Carry forward; do not carry forward the CPU ray queue. |
  | 4 | Legacy cone light had no reflection or refraction; the flashlight cone is range 8, cosine 0.9659258 (15°), intensity 24. | `engine/LightNetwork.cs@a6c914e:L12-L13`, `parts/FlashlightPart.cs@a6c914e:L10-L13` | Carry forward the cone; refraction is new. |

  Files harvested: `parts/ColourFilterPart.cs`, `engine/OpticalNetwork.cs`, `engine/LightNetwork.cs`, `parts/FlashlightPart.cs`, `reference/cpu/MachinePart.cs`. Searched `engine/physics/`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` for lens terms; none found.

## 5. Acceptance outline

Point of truth: [thermal-06](../requirements.md#thermal-06), [sequence-task-402](../requirements.md#sequence-task-402) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers are derived in the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and gizmo: an activated Flashlight (CAT-029, 24 W), the Converging lens 1.5 m in front of it, and a Tinder pad (TH-32) placed 3.0 m behind the lens at the image.
- **Positive.** The whole cone (0.40 m radius at the lens) passes; 22.8 W lands in the 0.13 m spot (about 1,700 W/m²); the tinder's 0.0025 m² patch absorbs about 3.9 W, heads for about 675 K and ignites past 450 K in under a second. Incident power = transmitted + absorbed in the lens within the envelope.
- **Negative or control.** Tinder moved 1.0 m off the image plane: the spot widens to about 0.27 m (about 400 W/m²), the patch peaks near 378 K and does not ignite, with the same total power. A Wall between lens and tinder: no heating. Weak source (the candle's 1 W light): about 16 K on the patch, no ignition. No lens: at 4.5 m the cone is 2.4 m wide, the 0.3 m wafer intercepts about 2 % of it and its patch about 0.05 %; no ignition.
- **Boundaries.** Transmitted power never exceeds 0.95 × incident; a ray outside the 0.65 m disc hits the frame; the image distance follows v = u·f/(u − f).
- **Run/Reset.** Restores lens pose, lens temperature and the target's state exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the same focus and outcome.
- **Integrations.** TX-03; TH-07 absorber chain; sequence-task-402 latch; campaign 59, 60, 85, 142.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Refraction decision.** No S484 row decides finite-width refraction and focusing. Add a bounded decision before this element. Owner decision.
3. **Map row.** ChemicalReaction, Ignition and TopologyTransaction are inherited by the target composition; remove them from the lens row or keep them. Owner decision.
4. **Focal presets.** One fixed 1.0 m focal length or several presets, each with its own proof. Owner decision.
