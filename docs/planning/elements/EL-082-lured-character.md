# EL-082 · Lured character — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-082 · Lured character · Character |
| Requirement anchor | [element-082](../requirements.md#element-082); scope index [todo-200](../requirements.md#todo-200) |
| Named entry | [element-082](../invest/named-elements.md#element-082); source owner **S652** ([S652 decision](../invest/scope-corrections.md#s652)) |
| CAT spec refined or extended | None |
| Related identities | EL-083 Escaping, EL-086 Walker, EL-087 Predator (the same controller law); EL-088 Fish tank lure (a stimulus source) |
| Roadmap story | Unscheduled (aggregate [LAW-CONTROLLER-I](../invest/scope-corrections.md#law-controller-i)) |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

**Declaration-only rule.** The character is a typed sensor query, a shared finite-state policy and a locomotion actuator. Physics resolves the motion through contact. It has no per-element solver or update loop and never writes a destination pose ([engine contracts](../../engine-contracts.md); `docs/general-engine-design.md@a6c914e:L132-L132`, `docs/general-engine-design.md@a6c914e:L149-L149`).

- **Bodies and shapes.** One dynamic upright body: a box 0.5 × 0.6 × 0.4 m, half extents (0.25, 0.3, 0.2) (**proposed**: smaller than a Receiver and taller than a Basketball, so it reads as a small creature). Its bottom face is the foot: locomotion acts only through that face's support contact. The foot is a face of the single box, not a separate collider, so no dynamic compound body is needed.
- **Mass and material.** 1.5 kg, friction 0.8, restitution 0.1, bounce threshold 0.1 m/s (**proposed**: heavy enough not to be bowled over by a Tennis ball; grippy feet; a dull landing).
- **Constraints.** An upright constraint locks tipping about the body's local X and Z axes while yaw stays free (**proposed**: a walking toy must stay upright; it falls only by leaving its support).
- **Typed ports.** None. Stimuli are perceived, never wired.
- **Sensors and activation (perception).** One perception query against declared stimulus sources. It sees a source only if all three hold:
  - Within `perception_range`.
  - Inside a forward cone of half-angle 60° (**proposed**: a wide but directional field of view, so a lure behind the character is not seen).
  - With a clear sight line: a zero-thickness visibility segment blocked by opaque colliders and passed by transparent ones. This is the same occlusion rule as light.

  **Supported stimulus (proposed):** `StimulusKind.Visual` only in the first slice. Any other kind is unsupported and ignored.
- **Policy (shared ProgrammableController data).** States `Idle` and `Pursue`. Idle becomes Pursue while a supported stimulus is perceived. Pursue turns toward the nearest perceived source and requests forward drive. Losing perception returns to Idle and the drive stops; there is no memory of the last seen position (**proposed**: simplest rule satisfying "occluded stimulus does not trigger pursuit").
- **Work and energy stores.** A finite locomotion store of 200 J (**proposed**: about 22 s of full drive at 9 W, that is 9 N at 1 m/s, inside the 30 s attempt limit, so the character cannot run indefinitely). Drive work debits it, and an empty store stops locomotion.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `perception_range` | f32 | 1–8 | 4 | m | **proposed**: about a quarter of the 16.8 m bench width |
  | `max_speed` | f32 | 0.2–2 | 1.0 | m/s | **proposed**: a readable walking pace, slower than a rolling ball |
  | `drive_force` | f32 | 1–20 | 9 | N | **proposed**: 6 m/s² at 1.5 kg; enough to start on a bench, not to climb a wall |

  Locomotion acts only while the foot has support contact; there is no air control.
- **Cosmetic curves and UI bindings.** The gait cycle is driven by the committed ground speed. Head turn follows the committed yaw. An attention cue (raised ears) eases in while in Pursue. All are animation-worker bindings that never change physics.
- **Art (DESIGN.md).** An original, simple, rounded silhouette: a cream `#fff8e9` bean-shaped ceramic body, two ochre `#d69c47` ear discs, navy `#293954` dot eyes and a thin tail line. Matte satin material (`DESIGN.md@a6c914e:L48-L48`, `DESIGN.md@a6c914e:L193-L193`). Calm, purposeful motion (`DESIGN.md@a6c914e:L51-L51`). Not a copy of any historical character (`DESIGN.md@a6c914e:L53-L53`).
- **Catalogue and inventory entry.** Id `lured_character`, title "Curious critter", category Characters (a new category), colour `#fff8e9` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.** Dynamic boxes with friction and contact (`engine/gpu/WorkshopDomino.cs@a6c914e:L7-L35`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L774`).
- **Missing.**
  - ProgrammableController (finite-state perception to intent): decision owner S635 controller, next LAW-CONTROLLER-I ([decisions](../invest/decisions.md#s635)); this element's source D is S652.
  - Visibility query with transparent and opaque occlusion (GeometryQuery): shared with optics, S484 optical-commit, next S486.
  - Upright angular constraint and contact-actuated locomotion (JointConstraint, FiniteWorkActuation): Epic 10 joints; owner S652.
  - A finite locomotion store (FiniteLedger): the [ENGINE-LEDGER](../invest/scope-corrections.md#engine-ledger) law at its first consumer.
- **Dependencies.** A stimulus source: EL-088 Fish tank lure, or a declared lure fixture. CAT-066 Wall as the occluder.

## 4. Sources and legacy

- **Requirement row.** [element-082](../requirements.md#element-082): an original character chooses motion from generic perceptible stimulus and locomotion rules; occluded or unsupported stimulus does not trigger pursuit.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** [S652](../invest/scope-corrections.md#s652): freeze a finite sensing region and allowed physical intent; in range it moves toward the lure, out of range or occluded it does not, and with a missing lure it idles. [S635 controller](../invest/decisions.md#s635).
- **Legacy search.** `git grep -i` at a6c914e for "character", "lure", "mouse", "cat", "pursu" and "walker" over every Epic 7 deletion path found no character source. The occlusion facts below apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Visibility is a finite zero-thickness segment that is not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | Carry forward the rule. Do not carry forward the f64 CPU sweep. |
| L2 | Opaque pipe collars occlude a light trace; the bore and the clear shell do not | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | Carry forward as "transparent passes sight, opaque blocks" |

**Files harvested:** `engine/physics/SegmentOcclusionSweep.cs`, `CuriousContraptions.tests/PipeTests.cs` (occlusion test only).

## 5. Acceptance outline

- **Chrome recipe.** Place the Lured character at the bench left and a Fish tank lure 3 m to its right, facing it. Add a Wall that can be slid between them with the move gizmo. Run.
- **Positive.** The character turns, walks to the lure on its own feet and stops against it.
- **Negative or control.** With the Wall between them, it idles. With the lure at 6 m (beyond range), it idles. With the lure behind it (outside the cone), it idles. With no lure, it idles. With an unsupported stimulus kind, it idles. It never teleports, and its committed position is continuous every tick.
- **Boundaries.** Range edge at 4 m ± one tick of travel; an empty store stops it; a pushed character can fall over an edge but never tips over on flat ground.
- **Run/Reset.** Reset restores the pose, Idle state and full store exactly. **Save/Load.** Parameters round-trip and the trace repeats.
- **Integrations.** Requirement-row integration tasks: the [todo-200](../requirements.md#todo-200) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-02 sliding friction](../requirements.md#interaction-02) and [IX-03 joint constraint](../requirements.md#interaction-03) (upright constraint); perception follows the S635 controller decision. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" ("walker/lure/predator/fragile-target roles") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: EL-088 Fish tank lure (stimulus), EL-087 Predator (perceives this as prey), Wall occluder (CAT-066).

## 6. Open questions

1. Stimulus kinds beyond Visual (scent, sound). Unspecified — owner decision (S652).
2. Pursuit memory: whether the character continues to the last seen position after occlusion (not proposed).
3. Whether characters need the finite store or can be supplied externally.
4. Original character naming and art sign-off.
