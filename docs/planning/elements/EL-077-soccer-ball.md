# EL-077 · Soccer ball — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-077 · Soccer ball · Specialist |
| Requirement anchor | [element-077](../requirements.md#element-077); scope index [todo-217](../requirements.md#todo-217) |
| Named entry | [element-077](../invest/named-elements.md#element-077); source owner **S647** |
| CAT spec refined or extended | Extends the ball family: [CAT-001 Basketball](CAT-001-ball.md); its heavy-ball control is [CAT-014 Bowling ball](CAT-014-bowling.md) |
| Related identities | EL-076 Programmable ball, EL-189 Tennis ball, CAT-052 Pressure plate |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

- **Bodies and shapes.** One dynamic sphere: a `WorkshopBall` with a new `BallMaterial.For` arm (`engine/gpu/WorkshopConstruction.cs@a6c914e:L38-L77`), compiled as every ball (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`).
- **Mass and material.**

  | Field | Value | Status |
  | --- | --- | --- |
  | Radius | 0.32 m | **proposed**: a real soccer ball is slightly smaller than a basketball; 0.32 keeps that against the 0.34 m Basketball |
  | Mass | 0.45 kg | **proposed**: "lightweight"; below the 0.5 kg Pressure plate default, so it gives a load control the Basketball does not |
  | Bounce | 0.65 | **proposed**: livelier than the Basketball (0.55), far above the Bowling ball (0.14), so the bounce difference is visible |
  | Rolling resistance | 0.03 | **proposed**: an inflated shell like the Basketball (0.035), slightly freer |
  | Drag | 0.04 1/s | Shared ball declaration (`engine/gpu/WorkshopConstruction.cs@a6c914e:L43-L51`) |
  | Buoyancy | 0 | Same |
  | Friction | 0.3 | Same |
  | Bounce threshold | 0.1 m/s | Same |

  All fit the admission bounds (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`). The material is fixed by kind and admitted bit-for-bit (`engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`).
- **Constraints.** None.
- **Typed ports.** None.
- **Sensors and activation.** None owned. It is a payload for Receiver capture, Switch impact and Pressure plate load.
- **Work and energy stores.** None.
- **Parameters.** None authored; the material is fixed by kind.
- **Cosmetic curves and UI bindings.** None (`WorkshopBall.Cosmetic` is `None`, `engine/gpu/WorkshopConstruction.cs@a6c914e:L67-L77`).
- **Art.** Cream `#fff8e9` sphere with navy `#293954` pentagon patches (**proposed**: a recognisable silhouette in the approved palette, distinct from the Basketball orange and the Bowling blue). Satin material (`DESIGN.md@a6c914e:L193-L193`).
- **Catalogue and inventory entry.** Id `soccer`, title "Soccer ball", category Motion, colour `#fff8e9`, description "A light, lively ball. Bounces higher and presses less than heavy balls." (**proposed**: follows the Tennis entry, `parts/catalog/tennis.tres@a6c914e:L8-L14`).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction).

- **Exists now.** Every needed family: dynamic sphere, contact, friction, rolling resistance and drag (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L722-L754`). JointConstraint is inherited and not used.
- **Missing.** A `WorkshopPartKind.SoccerBall` member, its material arm, the palette icon and the catalogue resource. The source D owner S647 freezes the measured values.
- **Dependencies.** Bowling ball (CAT-014, delivered) for the heavy-ball control. Pressure plate (CAT-052, Story 9.1) for the load control.

## 4. Sources and legacy

- **Requirement row.** [element-077](../requirements.md#element-077): a distinct lightweight ball uses measured authored contact properties; its bounce and load differ reproducibly from a heavy ball.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Research reference.** TIM2 inventory ([todo-217](../requirements.md#todo-217)); intent, not constants ([todo-228](../requirements.md#todo-228)).
- **Legacy search.** `git grep -i "soccer"` at a6c914e over every Epic 7 deletion path returned nothing. The ball facts below apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Isolated drop: first impact at √(2h/g) ± 0.02 s; rebound ratio bounce² ± 0.015; replay identical | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L67` | Carry forward as the bounce acceptance |
| L2 | A dropped ball bounces, stays within 1 mm of the bench and settles below 0.05 m/s within 2400 ticks | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L48` | Carry forward |
| L3 | Pressure plate minimum 0.5 kg; one 0.35 kg Tennis ball does not press, two do; one Basketball does | `parts/catalog/pressure_plate.tres@a6c914e:L11-L14`; `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L38-L62` | Carry forward as the load control |
| L4 | Legacy ball contact material: restitution = bounce, threshold 0.1, friction 0.3 | `reference/cpu/MachinePart.cs@a6c914e:L236-L236` | Carry forward as declared values. Do not carry forward the CPU path. |

**Files harvested:** `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`, `CuriousContraptions.tests/PressurePlateTests.cs`, `parts/catalog/pressure_plate.tres`, `reference/cpu/MachinePart.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Through the real palette, place a Soccer ball and a Bowling ball on separate lanes at the same height with the move gizmo; place a Pressure plate wired to a Battery and a Lamp under a third lane holding a second Soccer ball. Run.
- **Positive.** The Soccer ball rebounds to about 0.65² of its fall height and rests at r 0.32 m.
- **Negative or control.** The Bowling ball released identically rebounds to about 0.14²; one Soccer ball on the plate leaves the Lamp off, while a Bowling ball or two Soccer balls light it.
- **Boundaries.** Rebound within ± 0.015 of bounce²; rest within 1 mm; a save carrying a different material for this kind is rejected.
- **Run/Reset.** Exact pose and identity rotation restored; a second Run replays identically. **Save/Load.** Kind and material survive reload.
- **Integrations.** Requirement-row integration tasks: the [todo-217](../requirements.md#todo-217) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row "Basketball, bowling, tennis, baseball, soccer and programmable physical presets" reserves levels 1–10 (reuse 21–30, 41–50, 91–100, 136–150); chapter 1 "On a Roll" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Pressure plate load control (CAT-052), Bowling ball comparison (CAT-014), Receiver capture (CAT-004).

## 6. Open questions

1. "Measured" properties: the row asks for measured values. Confirm the proposed radius, mass, bounce and rolling resistance, or supply a measured reference. Unspecified — owner decision (S647).
2. Whether the Soccer ball should respond to airflow like the Tennis ball (shared drag 0.04 is proposed).
