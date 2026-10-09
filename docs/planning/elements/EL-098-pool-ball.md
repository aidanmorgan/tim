# EL-098 · Pool ball — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-098 · Pool ball · Specialist |
| Requirement anchor | [element-098](../requirements.md#element-098); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-098](../invest/named-elements.md#element-098); source owner **S668** |
| CAT spec refined or extended | Extends the ball family ([CAT-001 Basketball](CAT-001-ball.md)) |
| Related identities | EL-097 Pool cue, EL-099 Pool pocket, EL-089 Gravity-effect pad (for the conditional variant B) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

"Planar-table constraints" are realised physically: the ball rolls on a flat table surface (the bench or a Wall slab) under gravity, and cushions are Walls. No joint pins the ball to a plane (**proposed**: a real table contact gives rolling and jumping behaviour without a special constraint; see Open questions).

- **Bodies and shapes.** One dynamic sphere: a `WorkshopBall` with a new `BallMaterial.For` arm (`engine/gpu/WorkshopConstruction.cs@a6c914e:L38-L77`), compiled as every ball (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`).
- **Mass and material.**

  | Field | Value | Status |
  | --- | --- | --- |
  | Radius | 0.2 m | **proposed**: the smallest catalogue ball, as real pool balls are smaller than tennis balls; above the 1/16 m floor |
  | Mass | 0.5 kg | **proposed**: denser than the Tennis ball (0.35 kg at 0.25 m), as a solid resin ball is |
  | Bounce | 0.9 | **proposed**: resin balls are very elastic; the highest catalogue bounce, still inside 0–1 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`) |
  | Friction | 0.2 | **proposed**: polished resin slides more than the shared 0.3 |
  | Rolling resistance | 0.01 | **proposed**: rolls far on a table, a third of the Basketball's 0.035 |
  | Drag | 0.04 1/s | Shared ball declaration (`engine/gpu/WorkshopConstruction.cs@a6c914e:L43-L51`) |
  | Buoyancy | 0 | Same |
  | Bounce threshold | 0.1 m/s | Same |

- **Constraints.** None (variant A).
- **Typed ports.** None.
- **Sensors and activation.** None owned. It is the payload for EL-099 pocket capture and Receiver capture.
- **Work and energy stores.** None.
- **Parameters.** `number`: an integer 1–15 printed on the ball, cosmetic only (**proposed**: distinguishes balls for ordered goals such as EL-122 without changing physics). The material is fixed by kind.
- **Cosmetic curves and UI bindings.** None beyond the static number decal; the decal rotates with the committed orientation, so rolling is visible.
- **Art (DESIGN.md).** A cream `#fff8e9` ball with a coloured band from the approved part palette (ball orange `#f57d38`, tennis green `#b8db59`, bowling blue `#45639c` and so on, by number) and a navy `#293954` number (`DESIGN.md@a6c914e:L165-L187`). Satin (`DESIGN.md@a6c914e:L193-L193`).
- **Catalogue and inventory entry.** Id `pool_ball`, title "Pool ball", category Motion, colour `#fff8e9` (**proposed**).

**Variants.** Each is specified separately.
- **Variant A — table pool ball (adopted).** As above: world gravity, a table contact and Wall cushions.
- **Variant B — no-gravity pool ball (historical, conditional, not adopted).** Requirement row: if adopted, it "requires a separate explicitly fictional field rule". Declaration if adopted: the same body and material as A; the zero gravity comes only from an EL-089 Gravity-effect field with `gravity_scale` 0 that encloses the play area, labelled as fiction. The ball itself carries no gravity override. Acceptance if adopted: inside the field the ball travels in straight lines between cushions with no fall; outside it, the same ball falls normally. Status: not adopted; it needs an owner adoption decision before any work.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction).

- **Exists now.** Every family variant A needs: dynamic sphere, product restitution, geometric-mean friction and rolling resistance at every contact (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L722-L754`; [capability inventory](../../gpu-f32-physics.md#capability-inventory)). Walls as cushions (`engine/gpu/WorkshopWall.cs@a6c914e:L6-L10`).
- **Missing.** The kind, material arm, icon and catalogue entry (S668). Variant B: the EL-089 volume gravity override (S635 environment, next LAW-ENVIRONMENT-I).
- **Dependencies.** EL-097 Pool cue or CAT-015 Bumper as a striker; CAT-066 Wall cushions; EL-099 Pool pocket. Variant B depends on EL-089.

## 4. Sources and legacy

- **Requirement row.** [element-098](../requirements.md#element-098): authored ball material and planar-table constraints define rolling behaviour; the no-gravity historical variant, if adopted, requires a separate explicitly fictional field rule.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "pool", "billiard" and "cue" found nothing. Ball and rolling facts apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Isolated drop: first impact at √(2h/g) ± 0.02 s; rebound ratio bounce² ± 0.015 | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L67` | Carry forward as the bounce acceptance |
| L2 | Legacy test worlds could set gravity 0 for isolation | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L65-L70` | Do not carry forward as a ball setting; variant B uses only the EL-089 fiction field |
| L3 | Legacy ball parameters were an enum set (Radius, Mass, Bounce, Buoyancy, Drag) | `reference/cpu/BallPart.cs@a6c914e:L4-L13` | Carry forward, replaced by `BallMaterial` |

**Files harvested:** `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/PipeTests.cs` (gravity set-up only), `reference/cpu/BallPart.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Build a table: a flat Wall slab as the bed with four Wall cushions. Place two Pool balls and a Bumper or EL-097 cue. Run.
- **Positive.** The struck ball rolls across the bed, strikes the second ball nearly elastically (the first nearly stops, the second carries on), and rebounds from a cushion.
- **Negative or control.** A Basketball in the same shot loses far more speed per cushion (bounce 0.55); a Bowling ball barely rebounds. An unstruck ball stays at rest.
- **Boundaries.** Rolling deceleration follows (5/7)(C_rr g + c v) within the envelope rule ([envelope](../../gpu-f32-physics.md#game-grade-envelope)); rest within 1 mm of the bed; rebound ratio 0.9² ± 0.015.
- **Run/Reset.** Reset restores the poses exactly. **Save/Load.** Kind, material and number round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02). Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim (including the no-gravity variant), and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) names no pool-table row; the nearest is the ball-presets row beginning "Basketball, bowling, tennis, baseball, soccer" (1–10, preset lessons in 91–100), and the ledger task must add a separate pool ball row before campaign use. Element integrations: EL-097 Pool cue or Bumper (CAT-015) striker, Wall cushions (CAT-066), EL-099 Pool pocket; variant B needs EL-089.

## 6. Open questions

1. Planar constraint: physical table contact (proposed) or a planar joint that forbids leaving the table. Unspecified — owner decision (S668).
2. Variant B adoption. Unspecified — owner decision.
3. Ball-to-ball friction lower than ball-to-cloth (needs per-pair materials, not available today).
