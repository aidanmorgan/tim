# TH-12 · Reversible heat pump — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds heat-pump values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-12 · Reversible heat pump |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-12](../requirements.md#thermal-12) (sequence-task-446); [named-elements.md#thermal-12](../invest/named-elements.md#thermal-12) |
| Proof owner / research | S576 · [TH-S05 active cooling](../../thermal-component-research.md#th-s05) |
| Related identities | No CAT spec. Supplied by [CAT-005 Battery](CAT-005-battery.md); cools [TH-13 Freezing mold](TH-13-freezing-mold.md) (TX-02); rejects to [TH-10 Finned heat sink](TH-10-finned-heat-sink.md). Contrast: [TH-29 Cold pack](TH-29-cold-pack.md) is a store, not a pump. |
| Campaign | Intro 78; practice 79; reuse 89, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)); forward and reverse modes taught separately before dependent use. |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants (modes), specified separately.**
  - **Forward.** Face A is the cold face, face B the hot face. Removes Q_c from A and rejects Q_h = Q_c + W into B.
  - **Reverse.** Face B is the cold face, face A the hot face; same law with A and B swapped.
  The mode is an authored or player-set configuration enum `Forward`/`Reverse`, fixed for the Run (proposed: "teach reverse operation separately" makes each mode its own proof; runtime switching is an open question).
- **Bodies and shapes.** One static body: box, half-extents 0.20 × 0.20 × 0.20 m (proposed: a 0.4 m cube, with face A on −X and face B on +X, sized to sit between a mold and a heat sink).
- **Mass and material.** Static rigid mass zero (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L70-L76`). Two face nodes of 0.2 kg aluminium each, c = 225 J/(kg·K) (aluminium × ¼, batch scale). Faces: metal, 64 W/K (batch scale), so the mold base and the sink foot each pair at 32 W/K and a ceramic block at 12.8 W/K.
- **Constraints.** None.
- **Typed ports.** One electrical input `WorkshopSocket.PowerIn`, `Electrical` domain (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). Thermal ports: faces A and B by contact. No signal input (proposed: a switch in the supply circuit controls it, S257).
- **Sensors and activation.** None.
- **Work and energy stores.** Input rating W = 24 W (proposed: with the COP below it removes about 70–90 W from a freezing mold while its hot side stays inside the operating range on a still-air TH-10 sink). Cooling Q_c = COP·W with COP = min(4, 0.5 × T_c/(T_h − T_c)) (proposed: practical machines reach about half of Carnot; the cap avoids a singular COP at equal temperatures). Hot-side rejection Q_h = Q_c + W exactly (IX-33). Operating range: T_h − T_c ≤ 60 K and T_c ≥ 233 K (proposed: beyond that the pump only dissipates W into the hot face). No supply → no pumping. Freezing chain (TX-02): Q_h ≈ 100 W into a still-air sink (5 W/K) holds the hot face about 20 K above ambient; at T_c ≈ 268 K and T_h ≈ 312 K COP ≈ 3, so Q_c ≈ 73 W at first. Q_c falls as the cold face drops further below the freezing water and the mold gains about 10 W from the room, so the 2.8 kJ mold charge freezes in about 50 s ([batch chains](TH-17-kettle.md#batch-n-chains)).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Mode | enum `Forward`/`Reverse` | closed set | `Forward` | — | (proposed: above) |
  | Input rating | f32 | fixed | 24 | W | (proposed: above) |
  | COP model | f32 pair | fixed | 0.5 Carnot, cap 4 | — | (proposed: above) |
  | Operating range | f32 pair | fixed | ΔT ≤ 60, T_c ≥ 233 | K | (proposed: above) |

- **Cosmetic curves and UI bindings.** Compressor spin ← committed input power ([research per-element table](../../thermal-component-research.md)); hot and cold shape glyphs on the faces swap with the mode.
- **Art.** "Opposed cream/gold faces with hot/cold shape glyphs and a cyan supply core" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.HeatPump`; id `heat_pump`, title "Reversible heat pump", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-12, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ElectricalPower, FiniteLedger, GeometryQuery, HeatPumping, SensibleHeat, ThermalConduction; JSON adds StateTransaction.

- **Exists now.** Static body and box collider (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); typed `PowerIn` socket enum (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** ElectricalPower: Story 8.1 ([S257](../invest/decisions.md#s257) → S270). [S543](../invest/decisions.md#s543): heat-pump → S559, conduction → S544. SensibleHeat (IX-43) has no S543 row. Unscheduled.
- **Dependencies.** A supply (CAT-005); a cold load (TH-13, TH-26); a hot-side sink (TH-10).

## 4. Sources and legacy

- **Requirement row** [thermal-12](../requirements.md#thermal-12): supplied work moves heat between two thermal ports with an explicit constitutive model and range; Q_h = Q_c + W; reverse operation taught separately.
- **Named entry** [thermal-12](../invest/named-elements.md#thermal-12): owner S576; "power loss stops pumping; an insulated hot side warms and limits cooling rather than swallowing heat".
- **Research** [TH-S05](../../thermal-component-research.md#th-s05) and per-element table: "electrical node + two-port pump"; parameters coefficient, range; compressor spin ← power.
- **Processes and scenarios.** [IX-33](../requirements.md#interaction-33), [IX-06](../requirements.md#interaction-06), [IX-18](../requirements.md#interaction-18); [TX-02](../requirements.md#thermal-scenario-02).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for pump, refrigerat, Peltier and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-12](../requirements.md#thermal-12), [TX-02](../requirements.md#thermal-scenario-02) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe (Forward).** Through the real palette and sockets: Battery → Switch → Heat pump `PowerIn`; a Thermal storage block (TH-26) on face A; a Finned heat sink (TH-10) on face B.
- **Positive (Forward).** Face A's block cools below ambient; face B warms; Q_h = Q_c + W within the envelope; battery debit = W.
- **Chrome recipe and positive (Reverse).** The same construction with the mode set to `Reverse`: the block on face A warms and the sink side cools.
- **Negative or control.** Switch open or battery exhausted: no pumping in either mode. Face B wrapped in an Insulating panel (TH-09): B warms, ΔT reaches 60 K and cooling stops; heat is never lost.
- **Boundaries.** COP never exceeds 4; at ΔT > 60 K all input becomes hot-face heat; T_c never falls below 233 K.
- **Run/Reset.** Restores both face temperatures, the mode and supply state exactly.
- **Save/Load.** Pose, mode and the `PowerIn` connection round-trip; Run after reload reproduces each mode's outcome.
- **Integrations.** TX-02 powered freezing with TH-13 (about 50 s); campaign 78, 79, 89, 145.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Mode switching.** Configuration-only mode, or a typed control signal that may reverse the pump during a Run. Owner decision (S257).
3. **COP model.** Fraction of Carnot versus a fixed COP curve. Owner decision (S559).
