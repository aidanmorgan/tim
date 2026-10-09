# EL-192 · Receiving basket — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Every value below is sourced; this identity has no proposed values.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-192 · Receiving basket · Gravity |
| Anchor | [requirements.md#element-192](../requirements.md#element-192); [named-elements entry](../invest/named-elements.md#element-192); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-004 basket](CAT-004-basket.md) ([requirement](../requirements.md#current-cat-004)); catalogue title "Receiver". Goal consumers: `Captured`, and later [EL-120](EL-120-quantity-goal.md)/[EL-121](EL-121-rate-window-goal.md). |
| Roadmap story | Delivered: CAT-004-I, Story 3.1 "Endpoint-Sampled Sensors & Dwell Tick Counter" ([epics](../../../_bmad-output/planning-artifacts/epics.md)). |
| Status | Partial: static capture, guide and halo delivered; a moving-frame (non-static) basket and multi-Receiver-with-balls constructions remain open. |

## 2. Declaration

No enumerated selector; one mode. Capture settings vary by authored precision, not by variant.

- **Bodies and shapes.** One static body with five boxes, given as centre and **half-extents** (`engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18`):

  | Box | Centre (m) | Half-extents (m) | Full size (m) |
  | --- | --- | --- | --- |
  | Floor | (0, −0.45, 0) | (0.75, 0.075, 0.75) | 1.5 × 0.15 × 1.5 |
  | Left / right wall | (∓0.75, 0, 0) | (0.06, 0.5, 0.8) | 0.12 × 1.0 × 1.6 |
  | Back wall | (0, 0, −0.75) | (0.75, 0.5, 0.06) | 1.5 × 1.0 × 0.12 |
  | Front lip | (0, −0.2, 0.75) | (0.75, 0.3, 0.06) | 1.5 × 0.6 × 0.12 (top at y 0.1, lower than the others' 0.5) |

- **Mass and material.** Static. Restitution 0.12, threshold 0.1 m/s, friction 0.3, rolling 0 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L63-L63`) — damped so cargo stays in.
- **Constraints.** None.
- **Typed ports.** None (`engine/gpu/WorkshopConnections.cs@a6c914e:L24-L24`).
- **Sensors and activation.**
  - One residence sensor per (Receiver, ball) in Free play, one per named ball in puzzles; bounds min (−0.66, −0.4, −0.66), max (0.66, 0.45 + margin, 0.66) in the Receiver frame (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L66-L77`).
  - Capture requires the centre inside the bounds, speed ≤ the speed limit, for the dwell time, sampled at substep endpoints and counted in ticks; capture latches once (`engine/gpu/CaptureLatch.cs@a6c914e:L17-L30`). Free defaults: margin 0.02 m, speed 1.5 m/s, dwell 0.35 s (`engine/gpu/WorkshopConstruction.cs@a6c914e:L79-L90`).
  - A ball beside or passing over the basket never satisfies dwell inside the bounds: "A ball beside or passing over it does not satisfy capture" ([element-192](../requirements.md#element-192)).
- **Work and energy stores.** A bounded planar guide (force region) toward the centre, descending-only, for assistance: region (−1.1, 0.5, −1.1)–(1.1, 1.5, 1.1), support height 0.5 m; zero acceleration in Free play (`engine/gpu/WorkshopConstruction.cs@a6c914e:L91-L101`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L143-L161`).
- **Parameters (authored assistance, first_principles).**

  | Knot | Precision | Margin (m) | Speed (m/s) | Dwell (s) | Guide (m/s²) |
  | --- | --- | --- | --- | --- | --- |
  | Forgiving | 0 | 0.3 | 3 | 0.15 | 12 |
  | Balanced | 0.45 | 0.174 | 2.325 | 0.24 | 6.6 |
  | Precise | 1 | 0.02 | 1.5 | 0.35 | 0 |

  Source: `engine/gpu/WorkshopPuzzle.cs@a6c914e:L102-L110`; interpolated between knots (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L34-L50`). Bounds: margin 0–1, speed 1/1024–64, dwell 1/1024–30 (`engine/gpu/WorkshopConstruction.cs@a6c914e:L83-L89`).
- **Cosmetic curves and UI bindings.** Halo ring radius 0.94 m, tube 0.025 m at y 0.55, scaled 1 + margin; albedo eases from `#bdf4bd` to white on capture via `CosmeticCurves.Receiver` (`parts/BasketPart.cs@a6c914e:L16-L22`).
- **Art.** Five boxes in the catalogue colour `#4aab94` (`parts/BasketPart.cs@a6c914e:L13-L15`; `DESIGN.md@a6c914e:L170-L170`).
- **Catalogue and inventory.** Id `basket`, title "Receiver", category Goals, "Catch and hold the named ball. The halo shows the current tolerance." (`parts/catalog/basket.tres@a6c914e:L6-L14`). Free palette row 2 (`engine/gpu/WorkshopInventory.cs@a6c914e:L52-L52`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction; source-specific: "shared moving-frame continuous residence/goal primitive and generic halo binding; contact alone is insufficient" ([element map](../general-engine-element-map.md)).

- **Exists now.** Static compound walls, residence sensor and guide in a static frame (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L161`), latch and goal evaluator (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L27-L35`), halo curve.
- **Missing.** Residence and guide in a moving (dynamic) frame — the document requires a static frame (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L235-L250`). A second Receiver while balls exist is refused (`engine/gpu/WorkbenchCapacity.cs@a6c914e:L67-L72`). Owner S296.
- **Dependencies.** A ball (EL-187/188/189).

## 4. Sources and legacy

- **Requirements.** Row: "Physical open container supports cargo and exposes an actual containment goal"; outcome "A ball beside or passing over it does not satisfy capture" ([element-192](../requirements.md#element-192)). CAT-004: outside/fast/disabled/leave-and-reenter controls break dwell; capture/event/halo latch once; Reset clears all; PickRadius/render meshes are not authority.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Physical dwell owns capture regardless of presentation changes; restores exactly. | `CuriousContraptions.tests/BasketResidenceOwnershipTests.cs@a6c914e:L17-L60` | Carry forward. |
| 2 | Authored margin applies bounded force through shared physics without moving the basket; a fast passage through the whole window still uses shared guidance and resets. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L17-L101` | Carry forward. |
| 3 | Guide clearance uses all owned collider children, not stale presentation. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L104-L132` | Carry forward. |
| 4 | Legacy pick radius 0.85 m. | `parts/BasketPart.cs@a6c914e:L12-L12` | UI only; never physics authority. |

Files consulted: `CuriousContraptions.tests/BasketResidenceOwnershipTests.cs`, `CuriousContraptions.tests/BasketGuideTests.cs`, `parts/BasketPart.cs`, `parts/catalog/basket.tres`.

## 5. Acceptance outline

Point of truth: [element-192](../requirements.md#element-192), [CAT-004](../requirements.md#current-cat-004). Existing Chrome proof: `tools/e2e/engine-core-2c.test.ts@a6c914e:L23-L223` (qualified capture, fast through-pass rejection, Reset and Save/Load).

- **Chrome recipe.** Free Workshop: place a Receiver, then a Basketball above it with the lift gizmo; second lane: Basketball beside it.
- **Positive.** The dropped ball settles inside; after 0.35 s below 1.5 m/s the halo turns white once.
- **Negative or control.** The ball beside the Receiver, and a ball launched over it at more than 1.5 m/s, never capture.
- **Boundaries.** Leave-and-reenter restarts dwell; disabled participation never captures.
- **Run/Reset.** Reset clears latch, halo and ball pose.
- **Save/Load.** Receiver pose and settings survive save and Load.
- **Integrations.** Contact/cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (basket with balls of every type); goal-variant integration [sequence-task-549](../requirements.md#sequence-task-549) (quantity and rate goals count Receiver deliveries); interaction row IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)). Campaign first use: the basket entry of the first row of the [campaign element coverage](../requirements.md#campaign-element-coverage) — first use 1–10; combination and spaced reuse 21–30, 41–50, 91–100, 136–150.

## 6. Open questions

1. Whether a moving (carried) Receiver is required by any scheduled lesson before the moving-frame primitive is built. Unspecified — owner decision.
2. Whether several Receivers with balls must coexist (needed by EL-120/121 multi-target goals).
