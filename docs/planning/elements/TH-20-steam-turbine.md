# TH-20 · Steam turbine — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy turbine exists; the legacy converging nozzle supplies the jet law and test oracles (section 4). Every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-20 · Steam turbine |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-20](../requirements.md#thermal-20) (sequence-task-454); [named-elements.md#thermal-20](../invest/named-elements.md#thermal-20) |
| Proof owner / research | S584 · [TH-S07 heat to mechanical work](../../thermal-component-research.md#th-s07) |
| Related identities | No CAT spec. Rotary capture shares the gas family law with [CAT-070 Windmill](CAT-070-windmill.md) ([gas family](../../finite-gas-foundation.md)); shaft output drives [CAT-019 Conveyor](CAT-019-conveyor.md) or other shaft loads (Story 11.1). Vapour from [TH-17](TH-17-kettle.md); exhaust to [TH-18](TH-18-condenser.md). |
| Campaign | Intro 76; practice 77; reuse 87, 147 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element or a gas nozzle source. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** Static cream shell: box, half-extents 0.30 × 0.30 × 0.10 m (proposed: a 0.6 m partial-cutaway housing). Dynamic gold rotor: sphere-bounded wheel of radius 0.25 m modelled as a box with half-extents 0.25 × 0.25 × 0.04 m (proposed: colliders are sphere, box or plane only, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** Rotor 0.4 kg (proposed: the Domino's mass; inertia about 0.0125 kg·m² for a 0.25 m disc, derived). Rotor contact friction 0.3 (proposed: smooth metal). Steam R = 461.5, Cv = 1410 J/(kg·K) SI (batch scale).
- **Constraints.** One hinge joint, rotor on the shell axis.
- **Typed ports.** Gas inlet and gas exhaust ports, enum-typed (proposed: gas family port types). Mechanical shaft output port (proposed: the mechanical-port roles of [S257](../invest/decisions.md#s257) mechanical-port → S690).
- **Sensors and activation.** None.
- **Work and energy stores.** Inlet feeds an internal converging nozzle of throat area 1 × 10⁻⁴ m² (proposed: matches the kettle spout) aimed tangentially at the blades. Capture follows the rotary-capture law: torque = p_pitch·c·(u − p_pitch·ω), with c from the jet and exposure (gas family), pitch selected at compile so the unloaded speed is ≤ 30 rad/s (proposed: fast enough to read as a turbine, within the 128 rad/s envelope). Shaft work ≤ jet kinetic power; the remainder leaves as exhaust enthalpy; calm-air resistance b = 0.002 N·m·s (proposed: coasts down within a few seconds). With the plate-driven TH-17 kettle supplying 2.7 g/s near ambient pressure, the jet leaves at about 46 m/s and carries about 2.9 W of kinetic power (derived): enough to spin the rotor and drive a light shaft load.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Nozzle throat area | f32 | fixed | 1 × 10⁻⁴ | m² | (proposed: above) |
  | Unloaded speed cap | f32 | fixed | 30 | rad/s | (proposed: above) |
  | Passive resistance b | f32 | fixed | 0.002 | N·m·s | (proposed: above) |
  | Rotor mass | f32 | fixed | 0.4 | kg | (proposed: above) |

- **Cosmetic curves and UI bindings.** Rotor ← committed hinge angle ([research per-element table](../../thermal-component-research.md)); the cyan flow window shows the jet's presence; no cosmetic spin.
- **Art.** "Gold radial wheel in a cream partial cutaway shell with cyan flow window" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`), cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), cyan `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.SteamTurbine`; id `steam_turbine`, title "Steam turbine", category Heat (proposed: no current category covers thermal parts; Motion is the alternative).

## 3. Engine capabilities

Families (map row TH-20, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): EnvironmentState, FiniteLedger, GasState, JointConstraint, PressureWork, RigidBodyDynamics, ShaftTorque; JSON adds StateTransaction. Composition note: freeze the pressure/enthalpy-to-shaft constitutive model and exhaust flow; no prescribed speed.

- **Exists now.** Rigid bodies and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Hinge: Story 10.3. Rotary capture: Story 12.3 (Windmill). ShaftTorque output: Story 11.1. Gas nozzle source and sealed store: [S470](../invest/decisions.md#s470) → S471 (unscheduled). PressureWork: [S416](../invest/decisions.md#s416) → S419.
- **Dependencies.** A vapour source (TH-17); a shaft load (Story 11.1 shaft consumers).

## 4. Sources and legacy

- **Requirement row** [thermal-20](../requirements.md#thermal-20): fluid-work converter exchanges pressure/enthalpy drop for load-dependent shaft torque and exhaust; a supported constitutive model governs its range; no variants.
- **Named entry** [thermal-20](../invest/named-elements.md#thermal-20): owner S584; "no available pressure/enthalpy drop gives no useful work; stall cannot generate free power".
- **Research** per-element table: "nozzle + rotary capture"; parameters pitch, inertia; rotor ← hinge angle.
- **Processes and scenarios.** [IX-09](../requirements.md#interaction-09), [IX-05](../requirements.md#interaction-05), [IX-08](../requirements.md#interaction-08); [TX-13](../requirements.md#thermal-scenario-13).
- **Legacy harvest** (converging nozzle; no part consumed it).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Nozzle regimes NoFlow, Subsonic, Choked; thrust = momentum rate + pressure force; enthalpy power = ṁ·Cp·T₀. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L5-L37` | Carry forward the jet flux record. |
  | 2 | Fluxes conserve continuity, stagnation enthalpy (600·T + v²/2 = 180,000 in the oracle) and momentum. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L9-L29` | Carry forward as oracle. |
  | 3 | Closed area or balanced pressure: no mass, enthalpy or thrust. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | Carry forward: no pressure drop, no work. |
  | 4 | Reverse flow and downstream plume expansion are not inferred. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L41` | Carry forward: exhaust needs its own declaration. |
  | 5 | Nozzle transit quadrature with evaluation budgets. | `engine/physics/GasNozzleTransit.cs@a6c914e:L5-L6` | Do not carry forward: proof-grade CPU integration. |

  Files harvested: `engine/physics/ConvergingGasNozzle.cs`, `engine/physics/GasNozzleTransit.cs`, `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`.

## 5. Acceptance outline

Point of truth: [thermal-20](../requirements.md#thermal-20), [TX-13](../requirements.md#thermal-scenario-13) and [ELEMENT acceptance](../requirements.md#accept-element).

- **Chrome recipe.** Through the real palette, fluid and shaft connections: a Kettle (TH-17) boiling on a supplied Heating plate, hosed to the turbine inlet, exhaust hosed to a Condenser (TH-18), shaft output to a lightly loaded Conveyor (CAT-019) carrying a Tennis ball to a Receiver.
- **Positive.** The rotor spins up under the jet; the conveyor moves the ball; shaft work + exhaust enthalpy + losses = enthalpy supplied; exhaust mass = inlet mass; shaft work never exceeds the jet's ≈ 2.9 W.
- **Negative or control.** Equal pressure (cold kettle): no rotation. Stalled load (conveyor blocked): torque but zero shaft work, no free power. Exhaust blocked: flow stops when pressures equalise.
- **Boundaries.** Unloaded speed never exceeds 30 rad/s; nozzle flow is forward only; still air coasts the rotor down.
- **Run/Reset.** Restores rotor angle, zero speed and all gas inventories exactly.
- **Save/Load.** Pose and connections round-trip; Run after reload reproduces the outcome.
- **Integrations.** TX-13; TX-05; campaign 76, 77, 87, 147.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Shaft power.** About 2.9 W of jet power from the toy kettle may be too little for heavier shaft loads; raise the boil rate, add a pressure head, or keep loads light. Owner decision.
3. **Constitutive model.** Impulse rotary-capture law versus a reaction-turbine efficiency curve. Owner decision (S584 composition).
4. **Gas source schedule.** S471 and a nozzle source story are unscheduled. Owner decision.
