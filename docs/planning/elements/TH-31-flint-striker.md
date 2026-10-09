# TH-31 · Flint striker — named-identity readiness spec

Story 7.0 named-identity spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. No legacy holds striker values, so every design value without a cited source is marked "(proposed: …)" with a one-line justification; the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name | TH-31 · Flint striker |
| Type | Thermal · potential element (potential/conditional; source adoption retained) |
| Anchors | [requirements.md#thermal-31](../requirements.md#thermal-31) (sequence-task-465); [named-elements.md#thermal-31](../invest/named-elements.md#thermal-31) |
| Proof owner / research | S593 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | No CAT spec. Ignites [TH-32 Tinder pad](TH-32-tinder-pad.md) and the [TH-01 Candle](TH-01-candle.md) wick; strikes the tip of [TH-33 Spring-mounted match](TH-33-spring-mounted-match.md). Pushed through its swing by a rolling ball ([CAT-001](CAT-001-ball.md)); a single tap from a lever ([CAT-034](CAT-034-impact_lever.md)) transfers too little (Open question 4). |
| Campaign | Intro 58; practice 59; reuse 85, 99 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Roadmap story | Unscheduled. No epic in `_bmad-output/planning-artifacts/epics.md` schedules a thermal element. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`); no legacy part. |

## 2. Declaration

- **Batch N game scale** (proposed, identical in all 37 TH specs; chains derived against it are in [TH-17](TH-17-kettle.md#batch-n-chains); owner question in section 6). Ambient reservoir 288 K and 101.3 kPa. [Research table](../../thermal-component-research.md) rows: specific heat, latent heat and source power read as listed range × scale (64–4096 J/(kg·K), 8192–2,097,152 J/kg, 0.5–2048 W); temperature (128–2048 K), conductance (2⁻¹⁰–2⁸ W/K), fuel/supply (finite), ignition threshold (400–1200 K) and sensor thresholds read as listed, without scale; expansion coefficient reads 2⁻²⁰–2⁻¹⁴ 1/K after scale, which TH-22 and TH-23 exceed on purpose as a declared exaggeration. Condensed-phase specific heat SI × ¼ and latent heat SI × 1/16, so catalogue-scale bodies change temperature or phase within one Run. Gas R and Cv SI × 1, so γ and pressure stay physical; each condensed phase's enthalpy is referenced so vapour made at T_sat carries m·Cv·T_sat and the scaled latent heat is exactly the heat consumed; because water's scaled latent heat (141 kJ/kg) is below R·T_sat (172 kJ/kg), boiling follows a declared saturation curve, not Clausius–Clapeyron. Every fuel's reaction energy and O₂ demand per kilogram SI × 1/1024 (wax 41,016 J/kg and 3.4/1024 kg O₂/kg; wood and cellulose 15,625 J/kg and 1.2/1024 kg O₂/kg), so a 0.3 kg candle holds about 12 kJ, heat per kilogram of O₂ keeps Thornton's ≈ 13 MJ/kg, and fuel mass is consumed 1:1 so burn-down stays visible. Still-air surface exchange h = 2.5 W/(m²·K) (SI × ¼, so condensed-body time constants C/(hA) stay SI-like while gas-node ones run about 4× slower than SI); forced air h = 2.5 + u (u in m/s). Contact pairs conduct G = G_a·G_b/(G_a + G_b) from each part's face conductance: metal 64, ceramic, wood, ice or soft 16, probe tip 0.5 W/K. Every exchange is clamped per 480 Hz substep to the equalising amount ΔT·C_a·C_b/(C_a + C_b), so small nodes never overshoot. Reactive surfaces carry a thermal-only patch node outside rigid mass (fibre 10 mg, wood 0.5 g; area 0.0025 m²; patch-to-bulk 0.01 W/K; thermal nodes 10⁻⁶–1024 kg, not bound by the 1/1024 kg rigid minimum) that takes strikes, flames and light first. Radiant optical power: 1 legacy intensity unit = 1 W, so the flashlight emits 24 W. Canonical f32 ([numeric contract](../../gpu-f32-physics.md#game-grade-envelope)).
- **Variants.** None named in the row.
- **Bodies and shapes.** Static cream hinge mount: post box with half-extents 0.06 × 0.42 × 0.06 m carrying the hinge 0.84 m above the floor (proposed: the hanging arm's tip then sits at a Basketball's 0.34 m centre height). Dynamic striker arm: box half-extents 0.25 × 0.03 × 0.05 m, hanging from the hinge, gold striking face at the tip, 0.5 m from the hinge (proposed: an arm a rolling Basketball can push). Sprung flint plate on the mount beside the arm, pressing on the striking face over the first 20° of swing, so 0.175 m of tip travel (proposed: a short, deliberate scrape). Spark region: box half-extents 0.10 × 0.10 × 0.10 m in front of the striking face (proposed: a small, deliberate aim zone, never a target lookup).
- **Mass and material.** Arm 0.2 kg, I ≈ 0.0167 kg·m² about the hinge (derived, m·L²/3) (proposed: light enough for a ball to push). Striking face friction μ = 0.8 against the flint plate, restitution 0.1 (proposed: rough ferro-flint). Flint plate normal load N = 28 N from its spring (proposed: sets the friction work, below). The striker holds no thermal store of its own; its deposit goes to the reactive patch node of whatever occupies the spark region.
- **Constraints.** Hinge joint, swing range 0–90°, return spring torque 0.5 N·m at 90° (proposed: at 90° the arm is horizontal at hinge height, so a pushing ball passes under it; the arm returns and rearms after each strike).
- **Typed ports.** None as sockets. Input is any contact on the arm; output is the spark deposit into whatever body occupies the spark region.
- **Sensors and activation.** A strike is one arm swing in which the face slides across the flint plate with relative speed ≥ 0.5 m/s (proposed: slow grazes make no spark). Rearms when the arm returns below 10° (proposed).
- **Work and energy stores.** Friction work at the face is W_f = μ·N·s over the engaged arc: 0.8 × 28 N × 0.175 m ≈ 3.9 J when the arm passes the whole arc, drawn from whatever pushes the arm; if the pusher's energy runs out first the arm stalls and W_f is only what was dissipated. Spark deposit = 0.5 × W_f, capped at 4 J per strike (proposed: half the dissipated work as hot particles). Reference strike ([batch chains](TH-17-kettle.md#batch-n-chains)): a 1 kg Basketball (radius 0.34 m, `engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L51`; solid-sphere inertia, `engine/gpu/RigidMassProperties.cs@a6c914e:L34-L37`) released 1.0 m up a Ramp arrives with about 9 J. Its first impact passes at most about 1.1 J (product restitution 0.55 × 0.1, `engine/gpu/WorkshopConstruction.cs@a6c914e:L50-L50`), so the deposit relies on sustained contact: the ball keeps pushing, paying the 3.9 J of friction plus about 0.9 J to lift the arm and wind the spring to 90°, and rolls on with the rest. Deposit about 2.0 J. The deposit enters the patch node of the body in the spark region through the shared ignition interface (IX-29): a 10 mg fibre patch (0.00425 J/K) rises to about 750 K, past tinder's 450 K (0.69 J needed, 2.8× margin) and the candle wick's 500 K (0.90 J needed, 2.2× margin); a 0.5 g wood patch (0.21 J/K) rises only about 9 K. Empty region: the deposit dissipates to ambient. Arm and spring energy accounted mechanically.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | Flint normal load | f32 | fixed | 28 | N | (proposed: above) |
  | Engagement arc | f32 | fixed | 20 | ° | (proposed: above) |
  | Spark fraction | f32 | fixed | 0.5 | — | (proposed: above) |
  | Deposit cap per strike | f32 | fixed | 4 | J | (proposed: above) |
  | Minimum strike speed | f32 | fixed | 0.5 | m/s | (proposed: above) |
  | Return spring torque | f32 | fixed | 0.5 | N·m | (proposed: above) |

- **Cosmetic curves and UI bindings.** Spark ← committed strike occurrence ([research per-element table](../../thermal-component-research.md)): "a restrained brief spark cue" (row); arm pose ← committed hinge angle.
- **Art.** "Cream hinge mount with gold striking face and a restrained brief spark cue" (row). Nearest [DESIGN.md](../../../DESIGN.md) source colours (proposed mapping): cream `#ead39b` (`DESIGN.md@a6c914e:L146-L146`), gold `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`).
- **Catalogue and inventory entry.** New `WorkshopPartKind.FlintStriker`; id `flint_striker`, title "Flint striker", category Heat (proposed: no current category covers thermal parts).

## 3. Engine capabilities

Families (map row TH-31, [general-engine-element-map](../general-engine-element-map.md); binding [thermal-01.json](../../coverage/engine/thermal-01.json)): ChemicalReaction, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, Ignition, RigidBodyDynamics, SensibleHeat, SlidingFriction, TopologyTransaction; JSON adds StateTransaction.

- **Exists now.** Rigid bodies, contacts, friction materials (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`); ball materials and solid-sphere inertia (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L51`, `engine/gpu/RigidMassProperties.cs@a6c914e:L34-L37`); first-qualifying-impact contact triggers (`engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L8-L8`); finite paid contact work in joules (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L38`); Run/Reset and save (`engine/gpu/ActivationNetwork.cs@a6c914e:L67-L67`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Missing.** Hinge and return spring: Stories 6.4 and 10.3. Sprung contact normal load and friction-work debit to a heat deposit (IX-02) and ignition: [S543](../invest/decisions.md#s543) ignition → S555, reaction → S554. Thermal-only patch nodes (batch scale). Unscheduled.
- **Dependencies.** A pushing body (a rolling ball); a reactive target (TH-32, TH-01 wick, TH-33 tip).

## 4. Sources and legacy

- **Requirement row** [thermal-31](../requirements.md#thermal-31): mechanical contact supplies a bounded ignition-energy deposit through the shared interface; no target lookup or guaranteed flame; no variants.
- **Named entry** [thermal-31](../invest/named-elements.md#thermal-31): owner S593; "a miss, insufficient work or a nonreactive target produces no sustained combustion".
- **Research** [TH-S03](../../thermal-component-research.md#th-s03) ("strikers, matches and tinder deposit finite energy into the same ignition state") and per-element table ("impact trigger → finite ignition energy"; energy per strike; spark ← occurrence).
- **Processes.** [IX-02](../requirements.md#interaction-02), [IX-29](../requirements.md#interaction-29), [IX-01](../requirements.md#interaction-01).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for flint, striker, spark and ignition terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance outline

Point of truth: [thermal-31](../requirements.md#thermal-31) and [ELEMENT acceptance](../requirements.md#accept-element). Numbers follow the [batch chains](TH-17-kettle.md#batch-n-chains).

- **Chrome recipe.** Through the real palette and gizmo: a Basketball released 1.0 m up a Ramp aimed at the hanging striker arm; a Tinder pad (TH-32) in the spark region; a Candle (TH-01) wick touching the tinder.
- **Positive.** The ball pushes the arm through the 20° arc against the 28 N flint plate; friction work is about 3.9 J and the deposit about 2.0 J (≤ 4 J, = half the dissipated work); the tinder patch reaches about 750 K, ignites, and its flame lights the candle wick. Ball kinetic energy lost = friction work + arm and spring energy + contact losses.
- **Negative or control.** Ball misses the arm: no spark. Insufficient work: a ball released 0.1 m up (about 0.9 J) stalls the arm inside the arc, deposits at most about 0.45 J and the patch peaks near 395 K, below 450 K. A ball rolled in so slowly that the face slides below 0.5 m/s: no spark. A Thermal storage block (TH-26) in the spark region: it absorbs the deposit and nothing burns. A Combustible block (TH-03) alone in the region: its wood patch rises about 9 K and does not ignite. Empty spark region: the deposit dissipates.
- **Boundaries.** Deposit never exceeds half the strike's dissipated work or 4 J; friction work never exceeds μ·N·s or the pusher's energy; one deposit per strike until the arm rearms.
- **Run/Reset.** Restores arm pose, rearm state and target states exactly.
- **Save/Load.** Pose round-trips; Run after reload reproduces the outcome.
- **Integrations.** Lessons 58–59 (source, fuel and striker as separate objectives); campaign 58, 59, 85, 99.

## 6. Open questions

1. **Batch N game scale.** Adopt or revise the one scale in section 2 (×¼ specific heat, ×1/16 latent heat, gas ×1, reaction energy and O₂ ×1/1024, h 2.5 W/(m²·K), per-pair series contact conductance from per-part face values, 1 intensity unit = 1 W, and which research-table rows the scale multiplies). It carries two consequences: boiling needs a declared saturation curve (S550) because scaled water latent heat is below R·T_sat, and gas-node time constants run about 4× slower than SI (TH-25). Owner decision.
2. **Deposit law.** Fraction of friction work versus a fixed per-strike energy (research "energy per strike"). Owner decision (S555).
3. **Spark region.** A fixed region versus emitted spark particles with trajectories. Owner decision.
4. **Sustained push.** The striker ignites only when a body keeps pushing it through the arc; one impact, such as a lever tap, transfers at most about 1 J and deposits under the 0.69 J tinder threshold. Accept the push-through design, or add a stored-energy trigger (a cocked spring, as in TH-33). Owner decision.
