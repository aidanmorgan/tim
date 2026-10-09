# EL-040 · Pneumatic release valve — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared sealed-gas facts P1–P20, the `Gas` domain and the "Air" material are in [EL-039](EL-039-air-reservoir.md#shared-sealed-gas-family-facts).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-040 · Pneumatic release valve |
| Type | Pneumatic |
| Anchor | [requirements.md#element-040](../requirements.md#element-040); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-040); owner S475 |
| Related | Refines no CAT spec. Fed by [EL-039](EL-039-air-reservoir.md) through [EL-038](EL-038-pneumatic-hose.md); feeds a spring-return piston ([todo-426](../requirements.md#todo-426)), [EL-042 Air nozzle](EL-042-air-nozzle.md) or [EL-045 Air whistle](EL-045-air-whistle.md); commanded by any activation source ([CAT-063 Switch](CAT-063-switch.md), [CAT-022 Delay](CAT-022-delay.md), [CAT-017 Clock](CAT-017-clock.md)). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body, collider box 0.9 × 0.6 × 0.5 m at the origin **proposed** (a compact valve body, smaller than the 1.1 m Switch footprint so it fits between tank and piston); navy foot 1.0 × 0.12 × 0.6 m at (0, −0.36, 0) **proposed**. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none; the flap is cosmetic. |
| Typed ports | `GasIn` (Gas, Input) at (−0.5, 0, 0) **proposed**; `ActivationIn` (Activation, Input) at (0, 0.36, 0) **proposed** (top socket, as the Speaker's activation input at (0, 0.85, 0), `parts/SpeakerPart.cs@a6c914e:L57-L61`); variant-specific outlet (below). |
| Sensors and activation | Control: one `ActivationIn` Trigger latches the valve Open from the next committed tick until Reset **proposed** (same sticky target semantics the current network gives a lamp node, `engine/gpu/ActivationNetwork.cs@a6c914e:L213-L216`; a release valve releases once per run). Phase is the closed enum `ValvePhase { Closed, Open }`. The command carries no gas and no pressure: a closed valve passes nothing, and an open valve passes only what the upstream node holds. |
| Work and energy stores | none of its own; transfers inventory through the nozzle law (EL-039 P6) with throat area `opening` while Open; zero area while Closed (P6 NoFlow). |
| Parameters | `opening`: f32, 2 × 10⁻⁵ – 2 × 10⁻⁴ m², default 1 × 10⁻⁴ m² **proposed** (empties the default 0.01 m³, 400 kPa tank to ambient in well under a second, a visible "burst"; the smallest still drives a piston slowly). `outlet`: closed enum `ReleaseOutlet { Output, Exhaust }`, fixed per catalogue variant. |
| Cosmetic curves and UI bindings | Flap angle ← committed phase, 0° Closed → 75° Open, 0.08 s eased presentation transition **proposed** (gas-family binding "Flap angle ← committed opening", [finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas)); exhaust variant adds a short puff sprite while mass flow > 0. Cosmetics never alter flow. |
| Art | Cyan `#66b8c9` valve body, cream `#fff8e9` flap disc, navy `#293954` foot and outlet mouth, gold `#e8b764` activation socket ring, gold `#f7cb52` gas port spheres r 0.075 ([DESIGN.md colour system](../../../DESIGN.md#colour-system)). Geometry **proposed**. |
| Catalogue / inventory | Output variant: Id `release_valve`, Title "Release valve"; Exhaust variant: Id `exhaust_valve`, Title "Dump valve" **proposed**. Category `Air`. Description **proposed**: "A trigger opens the valve and lets stored air through. It never makes air: closed, it holds; open, it passes only what the tank has." |

**Variants** (row: "to a declared output or exhaust"):

| Variant | Outlet | Behaviour when Open |
| --- | --- | --- |
| Output | `GasOut` (Gas, Output) at (0.5, 0, 0) **proposed** | Flow from `GasIn` node to the `GasOut` node, higher to lower pressure. An unconnected `GasOut` holds like a capped end. |
| Exhaust | navy exhaust mouth at (0.5, 0, 0), no socket | Flow from `GasIn` node to ambient (EL-039 P15) until ambient is reached. |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S475): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction).

**Exists now**
- Activation input sockets and the discrete network with sticky latch targets, next-tick ordering and Reset clearing: `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`, `engine/gpu/ActivationNetwork.cs@a6c914e:L199-L229`; `WorkshopSocket.ActivationIn` (`engine/gpu/WorkshopConnections.cs@a6c914e:L9-L9`).
- Static box body/collider: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.

**Missing**
- Gas domain, GasState, FluidAdvection with a controlled orifice — [EL-039 §3](EL-039-air-reservoir.md#3-engine-capabilities); owners [S470](../invest/decisions.md#s470), [S416](../invest/decisions.md#s416), [S257](../invest/decisions.md#s257).
- A latch node whose committed phase drives a solver parameter (orifice area) on the physics worker — new node binding (no current node kind writes a physical parameter).
- Committed-phase flap binding — the activation feedback source exists (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).

**Dependencies.** A charged EL-039 (or EL-037 charging), EL-038 hose, an activation source, and a consumer to show the release.

## 4. Sources and legacy

- Requirement row [element-040](../requirements.md#element-040): "Opens a controlled gas path to a declared output or exhaust"; outcome "Closed valve cannot send a command-shaped free pressure pulse". Integration task [todo-376](../requirements.md#todo-376): "chooses a valve/air-hose route and drives the appropriate piston"; [todo-426](../requirements.md#todo-426): "exhaust → whistle", venting and inadequate pressure.
- Named entry [element-040](../invest/named-elements.md#element-040), owner S475. Gas design: "Gas network node + controller input | opening | Releases a burst after the source stops | Flap angle ← committed opening".
- Legacy: no valve part, level or lesson. Applicable facts: nozzle NoFlow at zero area and forward-only flow (EL-039 P6); a deferred activation command applies on the next tick (`parts/SpeakerPart.cs@a6c914e:L66-L71`) — carry forward the next-tick rule, not the per-part request queue.
- **Files harvested:** `parts/SpeakerPart.cs` (activation-socket and next-tick facts only); gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-040](../requirements.md#element-040), [pneumatics profile](../invest/profiles.md#pneumatics).

- **Construction (actual Chrome UI).** Charged Air tank → hose → Release valve (`GasOut → GasIn`) → hose → spring-return piston; Switch under a falling ball; select the switch, Connect `ActivationOut`, click the valve, choose `ActivationOut → ActivationIn`.
- **Positive.** The ball presses the switch; on the next tick the flap opens, the piston extends and diverts a ball; the tank dial drops.
- **Negative / control.** No activation link: the valve stays closed, the piston never moves and the tank holds (outcome: a closed valve sends no pulse). Empty tank with the valve triggered: the flap opens but nothing moves (a command cannot create pressure). Exhaust variant: the tank vents to ambient and the piston never moves.
- **Boundaries.** `opening` at both limits (slow versus fast extension); a second trigger after Open has no extra effect.
- **Run/Reset.** Reset closes the valve, restores the tank and piston exactly.
- **Save/Load.** Variant id, `opening` and all three connections round-trip.
- **Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393) (reservoir → valve/air-hose route → piston); [todo-426](../requirements.md#todo-426) spring-return piston ("Bellows/reservoir → valve → piston diverts a ball; exhaust → whistle", with venting and inadequate pressure); combination [todo-378](../requirements.md#todo-378) "One Breath" (stored air/piston/whistle); interaction processes [IX-08 fluid advection](../requirements.md#interaction-08), [IX-09 pressure work](../requirements.md#interaction-09) and [IX-07 signal propagation](../requirements.md#interaction-07) (the trigger carries no gas). Campaign: "release valve" is named in the pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 91–100, 136–150).

## 6. Open questions

1. **Control kind.** Activation-triggered latch (proposed) versus a supplied electrical solenoid that opens only while powered: owner decision.
2. **Re-closing.** Whether a second input (e.g. `ResetIn`) can close the valve during Run, making it reusable within a run: owner decision.
3. **Opening rate.** Instant at the tick boundary (proposed) versus a finite opening time that throttles the first burst: owner decision.
