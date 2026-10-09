# EL-189 · Tennis ball — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-189 · Tennis ball · Gravity |
| Anchor | [requirements.md#element-189](../requirements.md#element-189); [named-elements entry](../invest/named-elements.md#element-189); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-064 tennis](CAT-064-tennis.md) ([requirement](../requirements.md#current-cat-064)); the CAT spec holds the full legacy harvest (lessons, pressure-plate control). Contrasts: [EL-187](EL-187-basketball.md), [EL-188](EL-188-bowling-ball.md). |
| Roadmap story | Story 12.1 "Lightweight Tennis Ball & Floating Balloon Buoyancy" ([epics](../../../_bmad-output/planning-artifacts/epics.md)); airflow use in Story 12.2. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

No enumerated selector; one mode. The identity's point is that its bounce differs from the Bowling ball's "because of declared material, not IDs" ([element-189](../requirements.md#element-189)): it is the same `WorkshopBall` record with a third `BallMaterial` arm.

- **Bodies and shapes.** One dynamic sphere (`engine/gpu/WorkshopConstruction.cs@a6c914e:L67-L77`; `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`). Radius 0.25 m (`parts/catalog/tennis.tres@a6c914e:L14-L14`).
- **Mass and material.** New arm in `BallMaterial.For(kind)` (`engine/gpu/WorkshopConstruction.cs@a6c914e:L48-L53`):

  | Field | Value | Source |
  | --- | --- | --- |
  | Radius | 0.25 m | `parts/catalog/tennis.tres@a6c914e:L14-L14` |
  | Mass | 0.35 kg | same |
  | Bounce | 0.78 legacy; Story 12.1 says 0.85 | same; Open question 1 |
  | Drag | 0.04 1/s | same |
  | Buoyancy | 0 | same |
  | Friction | 0.3 | legacy dynamic default (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`) |
  | Bounce threshold | 0.1 m/s | same |
  | Rolling resistance | **proposed** 0.04 | felt nap rolls a little less freely than the inflated Basketball (0.035) and the hard Bowling ball (0.03), within 0–0.1 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`); [CAT-064](CAT-064-tennis.md) leaves it as an owner decision |

  All values fit the admission bounds (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`).
- **Constraints.** None.
- **Typed ports.** None.
- **Sensors and activation.** None owned; payload for Receiver, Switch and (Story 9.1) Pressure plate.
- **Work and energy stores.** None.
- **Parameters.** None exposed; fixed per kind (`engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`).
- **Cosmetic curves and UI bindings.** None.
- **Art.** Shared ball art (`parts/BallPart.cs@a6c914e:L9-L17`); palette `#b8db59` (`DESIGN.md@a6c914e:L167-L167`).
- **Catalogue and inventory.** Id `tennis`, title "Tennis ball", category Motion, "Light, small and bouncy. Responds strongly to moving air." (`parts/catalog/tennis.tres@a6c914e:L8-L14`); `WorkshopPartKind.TennisBall` appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction.

- **Exists now.** The whole dry-mode path the Basketball and Bowling ball use (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L622-L765`). Adding the kind is declaration data only: enum member, material arm, catalogue entry, palette row, workbench footprint (`engine/gpu/WorkbenchCapacity.cs@a6c914e:L34-L36`), ports (`engine/gpu/WorkshopConnections.cs@a6c914e:L24-L24`).
- **Missing.** Airflow receiver (Story 12.2); pressure scaling (EL-125). Neither blocks the dry mode.
- **Dependencies.** None for the dry mode; Fan (CAT-028) for airflow lessons.

## 4. Sources and legacy

- **Requirements.** Row: "Light compliant spherical cargo retains distinct calibrated contact properties"; outcome "Its bounce differs from the bowling ball because of declared material, not IDs" ([element-189](../requirements.md#element-189)).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Catalogue parameters mass 0.35, bounce 0.78, radius 0.25, buoyancy 0, drag 0.04 as a string-keyed dictionary. | `parts/catalog/tennis.tres@a6c914e:L14-L14` | Carry forward values; the string-keyed dictionary does not carry forward. |
| 2 | Isolated drop: rebound ratio bounce² ± 0.015 (0.61 for 0.78), timing √(2h/g) ± 0.02 s. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81` | Carry forward. |
| 3 | Dropped from y = 3 it bounces and settles within 2400 ticks without sinking. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L51` | Carry forward. |
| 4 | Further facts (pressure plate, 22 airflow lessons). | [CAT-064 spec](CAT-064-tennis.md) | Held in the CAT spec. |

Files consulted: `parts/catalog/tennis.tres`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`.

## 5. Acceptance outline

Point of truth: [element-189](../requirements.md#element-189), [CAT-064](../requirements.md#current-cat-064).

- **Chrome recipe.** Free Workshop: place a Tennis ball and a Bowling ball in two lanes at equal height with the palette and lift gizmo, as `tools/e2e/cat-014.test.ts@a6c914e:L105-L125` builds its lanes.
- **Positive.** The Tennis ball rebounds to about bounce² of its fall height, far above the Bowling ball's.
- **Negative or control.** Swapping only the catalogue kind (same pose) swaps the rebound; no identity check exists in the solver — the bounce follows the material record.
- **Boundaries.** Rests within 1 mm of the bench; only its declared material admitted.
- **Run/Reset.** Exact pose and zero velocity restored.
- **Save/Load.** Kind survives save, reload and Load.
- **Integrations.** Contact/cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (balls of every type); interaction rows IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)), IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)) and, for the Fan lessons of Story 12.2, IX-11 aerodynamic drag ([interaction-11](../requirements.md#interaction-11)). Campaign first use: the "Basketball, bowling, tennis …" row of the [campaign element coverage](../requirements.md#campaign-element-coverage) — first use 1–10; combination and spaced reuse 21–30, 41–50, 91–100, 136–150.

## 6. Open questions

1. Bounce 0.78 (legacy) versus 0.85 (Story 12.1). Unspecified — owner decision (shared with CAT-064).
2. Rolling resistance: proposed 0.04 here; the CAT-064 spec records it as an owner decision.
