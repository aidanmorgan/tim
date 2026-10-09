# TH-15 · Ice plug — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds ice-plug values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-15 · Ice plug |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-15](../requirements.md#thermal-15) (sequence-task-449); [named-elements.md#thermal-15](../invest/named-elements.md#thermal-15) |
| Proof owner / research | S579 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | No CAT spec. Same water material as [TH-14 Ice block](TH-14-ice-block.md), but a separate placement role and shape (row). Host apertures: [CAT-048 Pipe](CAT-048-pipe.md) bore, water-family outlets such as [EL-001 Finite reservoir](../invest/named-elements.md#element-001), or a gate opening. |
| Campaign | Intro 68; practice 69; reuse 86, 137 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** One body seated in a host aperture: box collider, half-extents 0.12 × 0.12 × 0.06 m at full solid (proposed: fills a 0.24 m square aperture, about a pipe bore at catalogue scale). It snaps into a host aperture at placement and is held by a fixed joint to the host while solid (proposed: placement is a seat, not free cargo). Melting shrinks the cross-section by f^½ while the depth stays (proposed: the plug thins radially, so it keeps obstructing until it is smaller than the aperture).
- **Mass and material.** 0.05 kg water (proposed: small enough to melt in seconds); initial temperature 253 K (proposed: as TH-14). Water under the batch scale: ice c = 525 J/(kg·K), fusion 20,875 J/kg at 273 K. Ice face 16 W/K (batch scale). Contact friction 0.05, restitution 0.1 (proposed: wet ice).
- **Constraints.** Fixed joint to the host while the cross-section exceeds the aperture; the joint releases when the shrunk plug no longer spans it (shared joint lifecycle, not a melt event).
- **Typed ports.** None as sockets; the seat is a geometric relation with a host aperture.
- **Sensors and activation.** None.
- **Work and energy stores.** Water node: mass, enthalpy, solid fraction f. Warming 253 → 273 K takes 525 J and melting 1,044 J: about 1.6 kJ in all (derived). Melted mass leaves as liquid into the host channel (IX-41). Pressure on the upstream face is carried as a contact/joint load (PressureWork); the plug never deletes held water.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Water mass | f32 | fixed | 0.05 | kg | (proposed: above) |
  | Seat aperture | f32 | fixed | 0.24 × 0.24 | m | (proposed: above) |
  | Initial temperature | f32 | fixed | 253 | K | (proposed: above) |

- **Cosmetic curves and UI bindings.** Scale ← committed solid fraction ([research per-element table](../../thermal-component-research.md)); the visible seat inside the cream aperture shows the remaining obstruction.
- **Art.** "Cyan translucent insert seated visibly inside a cream aperture" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.IcePlug`; id `ice_plug`, title "Ice plug", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-15, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, PhaseTopology, PressureWork, RigidBodyDynamics, SensibleHeat, SolidMelting, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Box bodies and contact (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Fixed joint and joint lifecycle: Story 6.4 constraints ([S257](../invest/decisions.md#s257) is ports only). PressureWork: [S416](../invest/decisions.md#s416) pressure-work → S419. [S543](../invest/decisions.md#s543): melting → S547, phase-topology → S565. Hollow host apertures: Stories 6.6–6.11 (pipe, bends, funnel). All thermal rows unscheduled.
- **Dependencies.** A host aperture (CAT-048 Pipe, a water-family outlet); a heat source aimed at the plug.

## 4. Sources and legacy

- **Requirement row** [thermal-15](../requirements.md#thermal-15): a solid-water insert blocks an aperture until melting changes the occupied geometry; separate placement and shape from the ice-block cargo role; no variants.
- **Named entry** [thermal-15](../invest/named-elements.md#thermal-15): owner S579; "heating an unrelated body does not open it; residual solid continues to obstruct as geometry dictates".
- **Research** per-element table: "ice block as a gate"; parameter mass; "releases a path when melted"; scale ← f.
- **Processes and scenarios.** [IX-21](../requirements.md#interaction-21), [IX-41](../requirements.md#interaction-41), [IX-09](../requirements.md#interaction-09); [TX-07](../requirements.md#thermal-scenario-07).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for plug, ice, melt and aperture terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-15](../requirements.md#thermal-15) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette and gizmo: a Pipe (CAT-048) with a Basketball resting against an Ice plug seated in its bore; a lit Candle (TH-01) under the plug.
- **Positive.** The 0.24 m plug covers the candle's 0.2 m plume and receives its 75 W; it warms, melts and thins over about 21 s; once smaller than the bore the joint releases, the ball rolls through and meltwater follows; mass is conserved.
- **Negative or control.** The candle under a different body: the plug stays seated and the ball stays put. Heat removed early (candle out at f ≈ 0.6): the residual plug still blocks.
- **Boundaries.** Release occurs exactly when the cross-section no longer spans the aperture; f never below 0.
- **Run/Reset.** Restores the seated full plug at 253 K, the joint and the ball exactly.
- **Save/Load.** Pose and seat round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-07; water-family outlets; campaign 68, 69, 86, 137.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Seat relation.** How a plug declares and validates its host aperture (snap seat versus free placement). Owner decision.
3. **Shrink model.** Radial f^½ thinning versus uniform shrink like TH-14. Owner decision (S547/S565).
4. **Held pressure.** Does a pressurised upstream side push a partly melted plug out before full release? Owner decision (S419).
