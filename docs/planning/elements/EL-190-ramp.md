# EL-190 · Ramp — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Every value below is sourced; this identity has no proposed values.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-190 · Ramp · Gravity |
| Anchor | [requirements.md#element-190](../requirements.md#element-190); [named-elements entry](../invest/named-elements.md#element-190); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-054 ramp](CAT-054-ramp.md) ([requirement](../requirements.md#current-cat-054)). Neighbours: [EL-191 Wall](EL-191-wall.md) (same static box path), [EL-192 Receiving basket](EL-192-receiving-basket.md) (first_principles target). |
| Roadmap story | Delivered: CAT-054-I, Story 1.4 ([epics](../../../_bmad-output/planning-artifacts/epics.md)). |
| Status | Partial: the Workbench mode and first_principles are delivered; remaining CAT-054 fixtures and supported-dimension controls are open. |

## 2. Declaration

Configuration: length and width, no enumerated selector ([CAT-054](../requirements.md#current-cat-054)); one mode.

- **Bodies and shapes.** One static box (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L83-L101`). `RampDimensions` (`engine/gpu/WorkshopInstances.cs@a6c914e:L22-L31`) stores full lengths; the compiler halves them into box half-extents (length × 0.5, thickness × 0.5, width × 0.5; `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L93-L94`).
  - Full length 3 m (range 0.125–4 m), full width 1.3 m (range 0.125–4 m), fixed full thickness 0.18 m.
  - The slope comes only from the authored rotation (`engine/gpu/WorkshopInstances.cs@a6c914e:L33-L43`); there is no separate incline parameter.
- **Mass and material.** Static (zero mass). Contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L90-L90`). With product restitution the ball's own bounce governs.
- **Constraints.** None; a static body.
- **Typed ports.** None (`engine/gpu/WorkshopConnections.cs@a6c914e:L24-L24`).
- **Sensors and activation.** None.
- **Work and energy stores.** None: "Wrong-facing slope cannot accelerate cargo uphill" ([element-190](../requirements.md#element-190)) — a static surface does no work.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `length` | `Metres` | 0.125–4 | 3 | m | `engine/gpu/WorkshopInstances.cs@a6c914e:L22-L31`; `parts/catalog/ramp.tres@a6c914e:L8-L11` (LengthBits 16896 = 3) |
  | `width` | `Metres` | 0.125–4 | 1.3 | m | same (WidthBits 15667 = 1.3) |

  Authored assistance for first_principles: ramp position/rotation correction knots (Forgiving 0.3 m / 5°, Balanced 0.12 m / 2°, Precise 0) (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L111-L115`).
- **Cosmetic curves and UI bindings.** None. Resize along X and Z only (`parts/RampPart.cs@a6c914e:L13-L13`); rotate ring tilts it.
- **Art.** Wood box plus two cream rails (0.055 m) at ±0.46 × width and two slate studs (`parts/RampPart.cs@a6c914e:L20-L25`); palette `#c28f52` (`DESIGN.md@a6c914e:L169-L169`), rail cream `#f9e8c9`.
- **Catalogue and inventory.** Id `ramp`, title "Ramp", category Structure, "Rotate a ramp to guide a falling ball. Its width extends into depth." (`parts/catalog/ramp.tres@a6c914e:L13-L21`). Free palette row 8 (`engine/gpu/WorkshopInventory.cs@a6c914e:L58-L58`); first_principles inventory 2 (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L78-L78`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** Static box, box–sphere/box–box manifolds and friction (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L345-L620`), rolling resistance on the ball side (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L721-L765`).
- **Missing.** Nothing for the delivered mode. JointConstraint applies only if a ramp is ever hinged (not required). Remaining CAT-054 fixtures need their own levels (Epic 15).
- **Dependencies.** A ball (EL-187) and a Receiver (EL-192) for the lesson.

## 4. Sources and legacy

- **Requirements.** Row: "Inclined rigid support guides motion through contact and gravity"; outcome "Wrong-facing slope cannot accelerate cargo uphill" ([element-190](../requirements.md#element-190)). CAT-054: two-ramp delivery with missing/misaligned and alternative-solution controls; mesh, contact and projections agree.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | first_principles: Basketball locked at (−4, 6.5, 0), Receiver at (2.5, 0.9, 0), inventory 2 ramps, captured goal. | `content/puzzles.json@a6c914e:L2-L275`; `engine/gpu/WorkshopPuzzle.cs@a6c914e:L119-L134` | Carry forward (current). |
| 2 | Ramp used in 36 legacy placements across delivery lessons. | `content/puzzles.json@a6c914e:L8205-L8673` (`two_deliveries`), `content/puzzles.json@a6c914e:L13930-L14460` (`deep_routes`) | Carry forward as lesson references. |
| 3 | Legacy resize took a free float vector and converted to binary16. | `parts/RampPart.cs@a6c914e:L27-L31` | Do not carry forward binary16 conversion (f32 contract). |

Files consulted: `content/puzzles.json`, `parts/RampPart.cs`, `parts/catalog/ramp.tres`.

## 5. Acceptance outline

Point of truth: [element-190](../requirements.md#element-190), [CAT-054](../requirements.md#current-cat-054). Existing Chrome proof: `tools/e2e/engine-core-2a3.test.ts@a6c914e:L21-L61` (ramp roll) and `tools/e2e/engine-core-2a3.test.ts@a6c914e:L183-L226` (first_principles two-ramp solve).

- **Chrome recipe.** first_principles: place two Ramps from the inventory and tilt them with the rotate ring to chain the ball into the Receiver.
- **Positive.** The ball rolls down both ramps into the Receiver; Captured, Solved once.
- **Negative or control.** A ramp tilted the wrong way: a ball released at rest at its low end does not climb; a missing second ramp: the ball misses.
- **Boundaries.** Length and width 0.125 and 4 m admitted; outside rejected without mutation.
- **Run/Reset.** Reset restores ball and ramps exactly.
- **Save/Load.** Dimensions and poses survive save, reload and Load (`tools/e2e/engine-core-2a3.test.ts@a6c914e:L227-L289`).
- **Integrations.** Contact/cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (ramps/walls with balls of every type, pipes and the basket); interaction rows IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)) and IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)). Campaign first use: the "ramp/wall/material variants" entry of the first row of the [campaign element coverage](../requirements.md#campaign-element-coverage) — first use 1–10; combination and spaced reuse 21–30, 41–50, 91–100, 136–150.

## 6. Open questions

1. Whether ramp friction (0.3) stays a fixed declaration or becomes a material choice (coated ramps, EL-118). Unspecified — owner decision.
