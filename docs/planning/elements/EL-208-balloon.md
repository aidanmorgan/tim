# EL-208 · Balloon — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-208 · Balloon · Gravity |
| Anchor | [requirements.md#element-208](../requirements.md#element-208); [named-elements entry](../invest/named-elements.md#element-208); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-003 balloon](CAT-003-balloon.md) ([requirement](../requirements.md#current-cat-003)); the CAT spec holds the material and buoyancy harvest. Neighbours, not merged: TH-25 Hot-air balloon, EL-090 Steerable blimp, [EL-125 Atmosphere](EL-125-authored-atmosphere-preset.md). |
| Roadmap story | Story 12.1 "Lightweight Tennis Ball & Floating Balloon Buoyancy" for buoyancy; gas store and puncture unscheduled ([epics](../../../_bmad-output/planning-artifacts/epics.md)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

Configuration: mass, bounce, radius, buoyancy, drag, no enumerated selector ([CAT-003](../requirements.md#current-cat-003)). EL-208 adds three behaviours specified separately below: buoyant rise, tether, puncture with gas loss.

- **Bodies and shapes.** One dynamic sphere, radius 0.36 m (`parts/catalog/balloon.tres@a6c914e:L14-L14`), on the shared `WorkshopBall` path (`engine/gpu/WorkshopConstruction.cs@a6c914e:L38-L77`).
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

- **Exists now.** Dynamic sphere, linear drag, per-body gravity vector (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`); `BallMaterial.Buoyancy` field, unconsumed (`engine/gpu/WorkshopConstruction.cs@a6c914e:L41-L44`).
- **Missing.**
  - Buoyancy and pressure: Story 12.1 and EL-125 (LAW-ENVIRONMENT-I, [S635](../invest/decisions.md#s635)).
  - Tether rope row: Story 10.2.
  - GasState sealed store and puncture (StructuralFracture/TopologyTransaction): S470 gas-state S471 and S543 fracture S563 ([decisions](../invest/decisions.md#s470)); unscheduled, owner S479.
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
