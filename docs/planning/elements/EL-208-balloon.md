# EL-208 · Balloon — named-identity readiness spec

**Current delivery (24 October 2026):** [EL-208 delivery slice](../invest/named-elements.md#element-208). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

## Current implementation contract — Story 12.1b

The [native Balloon spec](../../../_bmad-output/implementation-artifacts/spec-12-1b-balloon.md) owns the implementation candidate and verification status. Browser/release qualification remains pending. The historical harvest below preserves source acceptance and citations; its fixed-by-kind, shared-art, moving neck anchor, capped drag and empty 0.5 kg proposals are superseded by these decisions.

- Balloon is its own native Godot art and managed C# declaration package. Five BuildOnly controls retain defaults mass 0.5 kg, reference radius 0.36 m, bounce 0.2, buoyancy 11.5 m/s² and drag 0.4/s; ranges are respectively [0.125,8], [0.18,0.5], [0,1], [0,16] and [0,4]. Friction 0.3, bounce threshold 0.1 m/s and rolling resistance zero are declared material data.
- Construction explicitly selects persisted BuildOnly Standard (101325 Pa, relative pressure 1) or Vacuum (0 Pa, relative pressure 0). These are unbounded ambient boundaries under EL-125, not finite ambient inventories. Runtime never supplies a missing pressure default. Unknown or missing serialized presets are refused. Preparation is atomic against expanded physical geometry.
- A finite sealed ideal-gas store owns mass, volume and internal energy. Material R=2000 J/(kg·K), Cv=3000 J/(kg·K), reference temperature 300 K and positive gauge pressure 1000 Pa define a compliant isobaric envelope. The radius control describes Standard reference geometry; the same reference inventory expands adiabatically in Vacuum, exchanging boundary work and envelope potential. Temperature [1,1000] K, density ≤1 kg/m³ and pressure ≤200000 Pa are package admission limits.
- Declared mass is initial total mass. The one centered-inventory mass source subtracts reference gas to obtain dry carrier mass, then adds current gas exactly once. Dry principal moments are fixed declared coefficients 0.4×(0.0625 m)² times dry mass; gas contributes translational mass only. Isotropic outflow carries removed momentum and kinetic energy without adding thrust.
- Upward load is buoyancy×relative ambient pressure×current body mass×remaining gas fraction. Drag is drag×relative pressure, without angular damping. Shared drag admits finite nonnegative values using bounded exponent handling. A heavier no-lift Bowling control falls; changing only Balloon mass does not contradict the harvested mass-independent acceleration.
- A strict >3 N·s accepted-substep impulse ruptures one exact collider-bound observation. Sum final nonnegative normal impulses only for one unordered collider pair whose pre-force approach exceeds 0.1 m/s; no cross-pair/substep accumulation or speculative-trial events. Restitution and reduced mass apply. The committed occurrence opens the exact outlet once; discharge starts the next substep.
- Pressure-driven conductance referenceMass/(1000 Pa·1 s) vents a finite allocation and accounts enthalpy, boundary work and released envelope potential. It is not a timer. Empty state is canonical zero mass/volume/energy, zero lift and dry carrier mass. The physical sphere and committed presentation share radius cubed = dry radius cubed + 3V/(4π), leaving a 0.0625 m dry core.
- The tensile Tie is fixed at the body origin on the dry carrier; the string is naturally occluded inside the envelope and the neck is cosmetic. Existing tensile rows provide taut restraint and slack freedom. Shrinking never moves a constraint anchor.
- A typed IntegrityBroken goal binds exact owner and local integrity observation. Only its committed occurrence solves; another store, intact contact or a stale session epoch cannot. Reset restores full, intact authored preparation, geometry and tether; save/load preserves authored controls, atmosphere and exact goal selector.
- Required native proof covers rise/ceiling, Vacuum and heavier controls, taut/slack/disconnected tether, rupture/depletion, exact selected goal, art and lifecycle. Fan/no-fan/missed/occluded-jet transfer and fan guidance remain with 12.2; campaign and post-catalogue browser recipes remain required at their named owners.

This slice establishes the bounded finite-store/compliant-boundary/loss prerequisite before Balloon consumption. It does not complete S471 connected-volume/valve qualification or S697 gas-network integration; EL-039 and the later gas-network consumers retain those criteria and IDs. No general gas network or closed-world atmosphere is claimed.

## Historical harvest and original acceptance sources

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-208 · Balloon · Gravity |
| Anchor | [requirements.md#element-208](../requirements.md#element-208); [named-elements entry](../invest/named-elements.md#element-208); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-003 balloon](CAT-003-balloon.md) ([requirement](../requirements.md#current-cat-003)); the CAT spec holds the material and buoyancy harvest. Neighbours, not merged: TH-25 Hot-air balloon, EL-090 Steerable blimp, [EL-125 Atmosphere](EL-125-authored-atmosphere-preset.md). |
| Roadmap story | [EL-208 delivery slice](../invest/named-elements.md#element-208); canonical proof owner and prerequisite decisions are recorded there. |
| Harvest-baseline status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

Configuration: mass, bounce, radius, buoyancy, drag, no enumerated selector ([CAT-003](../requirements.md#current-cat-003)). EL-208 adds three behaviours specified separately below: buoyant rise, tether, puncture with gas loss.

- **Bodies and shapes.** One dynamic sphere, radius 0.36 m (`parts/catalog/balloon.tres@a6c914e:L14-L14`), declared by its own package and compiled by the shared dynamic-body path of `WorkshopPhysicsCompiler.Compile`, as the Basketball and Bowling ball packages are since C-Balls.
- **Mass and material.** Mass 0.5 kg, bounce 0.2, buoyancy 11.5 m/s², drag 0.4 1/s (`parts/catalog/balloon.tres@a6c914e:L14-L14`); friction 0.3, threshold 0.1 m/s (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`).
  - **Proposed** rolling resistance 0.05 — a soft skin rolls poorly; within 0–0.1 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
  - Drag 0.4 exceeds the admitted 0–0.125 1/s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L69-L69`): see Open questions.
- **Behaviour 1 — buoyant rise.** Upward force = buoyancy × pressure factor × mass; drag rate = drag × pressure factor (`reference/cpu/MachineWorld.cs@a6c914e:L877-L883`). At pressure 1, g = 9.81: net 1.69 m/s² upward, terminal rise ≈ 4.2 m/s ([CAT-003 spec](CAT-003-balloon.md)). Pressure comes from [EL-125](EL-125-authored-atmosphere-preset.md).
- **Behaviour 2 — tether.** **Proposed** one `Tie` socket, rope domain, at (0, −r, 0) — where the legacy string hung (`reference/cpu/BallPart.cs@a6c914e:L26-L27`) and the same socket kind as the Weight (`parts/WeightPart.cs@a6c914e:L17-L21`). A taut rope's tension opposes ascent: "Tether force opposes ascent" ([element-208](../requirements.md#element-208)).
- **Behaviour 3 — puncture and gas loss.**
  - **Proposed** puncture when a single contact's normal impulse exceeds 3 N·s. The balloon floats free, so the impulse uses the reduced mass. Restitution mixes as a product (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L642-L642`).
  - Bowling-ball strike: reduced mass μ = (4 × 0.5) / 4.5 = 0.444 kg; e = 0.14 × 0.2 = 0.028; J = μ(1 + e)v = 0.457·v. The threshold needs a closing speed v ≥ 3 / 0.457 = 6.6 m/s.
  - Ceiling arrival at terminal rise: a static Wall gives μ = 0.5 kg; e = 1 × 0.2 = 0.2; J = 0.5 × 1.2 × 4.2 = 2.52 N·s < 3, so the balloon's own ascent never pops it.
  - **Proposed** a punctured balloon vents its gas over 1.0 s; lift scales with remaining gas fraction, reaching zero, after which it falls as a 0.5 kg body — "lost contents alter buoyancy".
  - Gas store (sealed, finite) per the gas family: lift, volume, drag; skin scale ← committed volume ([finite-gas foundation](../../finite-gas-foundation.md)).
- **Typed ports.** `Tie` (above). No electrical or activation ports.
- **Sensors and activation.** Puncture is a contact-impulse event on the balloon body; it emits a typed `Popped` occurrence usable by goals ("popped" predicate, [requirements](../requirements.md#sequence-task-361)).
- **Work and energy stores.** Sealed gas store (finite); no work beyond buoyancy from the boundary atmosphere.
- **Parameters.** Material fixed per kind (`engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`); no player parameter.
- **Cosmetic curves and UI bindings.** Skin scale ← committed gas volume; string art follows the tie.
- **Art.** Shared ball art (`parts/BallPart.cs@a6c914e:L9-L17`) plus string `#e9dfcc`, thickness 0.015, from (0, −r, 0) to (0.06, −r − 0.5, 0) (`reference/cpu/BallPart.cs@a6c914e:L26-L27`). Palette `#ed6378` (`DESIGN.md@a6c914e:L168-L168`).
- **Catalogue and inventory.** Id `balloon`, title "Balloon", category Motion, "Buoyancy lifts it; fans can guide it. Pressure changes its ascent." (`parts/catalog/balloon.tres@a6c914e:L8-L14`); appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-03.json)): AerodynamicDrag, Buoyancy, EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, StructuralFracture, TopologyTransaction.

- **Exists now.** Dynamic sphere, linear drag, per-body gravity vector (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`). No buoyancy is compiled for a dynamic body today: a package's fluid declaration (`FluidPayload`) is not consumed by the dynamic-body compile.
- **Missing.**
  - Buoyancy and pressure: Story 12.1 and EL-125 (LAW-ENVIRONMENT-I, [S635](../invest/decisions.md#s635)).
  - Tether rope row: Story 10.2.
  - GasState sealed store and puncture (StructuralFracture/TopologyTransaction): S470 gas-state S471 and S543 fracture S563 ([decisions](../invest/decisions.md#s470)); required in the linked delivery slice, owner S479.
  - Per-contact impulse read-back for the puncture event: owner S479.
- **Dependencies.** EL-125 atmosphere; Rope (EL-205) or Rope anchor (CAT-058); a Wall as ceiling; Fan (CAT-028) for guidance.

## 4. Sources and legacy

- **Requirements.** Row: "Gas-filled compliant buoyant cargo responds to pressure, mass and puncture properties"; outcome "Tether force opposes ascent; lost contents alter buoyancy" ([element-208](../requirements.md#element-208)). CAT-003: zero-pressure, no-fan, occluded-jet and heavier-body controls.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Material mass 0.5, bounce 0.2, radius 0.36, buoyancy 11.5, drag 0.4. | `parts/catalog/balloon.tres@a6c914e:L14-L14` | Carry forward (f32). |
| 2 | Buoyancy and drag law scaled by world pressure. | `reference/cpu/MachineWorld.cs@a6c914e:L877-L883` | Carry forward law; CPU loop does not carry forward. |
| 3 | Buoyant balls drew a hanging string. | `reference/cpu/BallPart.cs@a6c914e:L26-L27` | Carry forward as art and tie location. |
| 4 | Ball parameters Radius, Mass, Bounce, Buoyancy, Drag were required; missing ones rejected. | `reference/cpu/BallPart.cs@a6c914e:L4-L13`; `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` | Carry forward rejection; not the string-keyed dictionary. |
| 5 | No legacy level, puncture or gas behaviour for the balloon. | `content/puzzles.json@a6c914e:L1-L33007` (no `balloon` kind) | Recorded; puncture and gas loss are new. |

Files consulted: `parts/catalog/balloon.tres`, `reference/cpu/MachineWorld.cs`, `reference/cpu/BallPart.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-208](../requirements.md#element-208), [CAT-003](../requirements.md#current-cat-003).

- **Chrome recipe.** Place a Balloon on the bench, a Wall above it as a ceiling; in a second lane tie a Balloon to a Rope anchor so it hangs stationary at the taut rope length; in a third, tether a Balloon the same way and lift a Bowling ball above it with the gizmo.
- **Positive.** The free balloon rises and rests against the ceiling; the tethered balloon rises until the rope is taut and hangs there. A Bowling ball released 3 m above the tethered balloon would reach 7.67 m/s in free fall and arrives at 7.59 m/s after its 0.04 1/s drag, giving J ≈ 0.457 × 7.59 = 3.47 N·s > 3: the balloon pops, loses lift over about 1 s and falls.
- **Negative or control.** Vacuum preset: no rise. A Bowling ball released 1 m above the tethered balloon arrives at 4.43 m/s: J ≈ 2.0 N·s < 3, the balloon bounces away intact. A heavier body in the same spot does not rise.
- **Boundaries.** Contact with the ceiling rests without penetration; the 2.52 N·s ceiling arrival does not pop it.
- **Run/Reset.** Reset restores an intact, full balloon at its exact pose.
- **Save/Load.** Kind and tether survive save and Load; punctured state is runtime-only.

- **Integrations.** Combine the declared atmosphere, rope anchor and ceiling lanes above; repeat the Bowling-ball puncture control with the same tether, and verify the typed Popped occurrence reaches the selected goal once.

## 6. Open questions

1. Drag 0.4 1/s versus the admitted 0.125: widen the bound or change the value. Unspecified — owner decision (shared with CAT-003).
2. Puncture trigger: impulse threshold (proposed 3 N·s, closing speed ≥ 6.6 m/s for a Bowling ball) or only a declared sharp feature (scissors/pin elements).
3. Whether Story 12.1 delivers buoyancy only, with tether and puncture in a later story.
