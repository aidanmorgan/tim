# EL-083 · Escaping character — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-083 · Escaping character · Character |
| Requirement anchor | [element-083](../requirements.md#element-083); scope index [todo-200](../requirements.md#todo-200) |
| Named entry | [element-083](../invest/named-elements.md#element-083); source owner **S653** ([S653 decision](../invest/scope-corrections.md#s653)) |
| CAT spec refined or extended | None |
| Related identities | EL-082 Lured, EL-086 Walker, EL-087 Predator (a hazard source); [EL-082 spec](EL-082-lured-character.md) for the shared character framework |
| Roadmap story | Unscheduled (aggregate [LAW-CONTROLLER-I](../invest/scope-corrections.md#law-controller-i)) |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

**Declaration-only rule.** A typed perception query, a shared finite-state policy and a locomotion actuator; physics produces the motion; there is no element update loop and no scripted escape path ([engine contracts](../../engine-contracts.md); `docs/general-engine-design.md@a6c914e:L132-L132`).

- **Bodies and shapes.** One dynamic upright box 0.5 × 0.6 × 0.4 m, half extents (0.25, 0.3, 0.2), whose bottom face is the foot; the foot is a face of the single box, not a separate collider (**proposed**: the same body as EL-082, so the controller law is proved once on one body shape and needs no dynamic compound body).
- **Mass and material.** 1.2 kg, friction 0.8, restitution 0.1, threshold 0.1 m/s (**proposed**: lighter than EL-082, so it is a quicker runner at the same drive force).
- **Constraints.** An upright constraint locks tipping about local X and Z; yaw is free (**proposed**, as EL-082).
- **Typed ports.** None.
- **Sensors and activation (perception).** One hazard query, all-round (360°), within `hazard_range`, with sight-line occlusion: opaque blocks and transparent passes. A **hazard** is either:
  - A declared `StimulusKind.Hazard` source, such as the EL-087 Predator.
  - Any dynamic body inside range closing on the character at ≥ 1.5 m/s (**proposed**: a rolling Bowling ball counts; a resting ball does not).
- **Policy (shared ProgrammableController data).** States `Idle`, `Flee` and `Recover`. A perceived hazard turns Idle into Flee: the character drives directly away from the hazard along the ground. Flee ends after `escape_distance` of travel, or when the hazard is no longer perceived, and goes to Recover (stand still for 1 s, **proposed** to avoid oscillation), then Idle. Blocked perception produces no flight. A blocked route is resolved physically: the character pushes against the obstacle and does not path-plan through it.
- **Work and energy stores.** A finite locomotion store of 200 J (**proposed**, as EL-082: about 15 s of full drive at 13.5 W, that is 9 N at 1.5 m/s, inside the 30 s attempt limit).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `hazard_range` | f32 | 1–6 | 2.5 | m | **proposed**: shorter than lure range, so hazards must come close |
  | `escape_distance` | f32 | 0.5–6 | 3 | m | **proposed**: the "bounded avoidance route" |
  | `max_speed` | f32 | 0.2–2.5 | 1.5 | m/s | **proposed**: faster than the walker, slower than a struck ball |
  | `drive_force` | f32 | 1–20 | 9 | N | **proposed**, as EL-082 |

- **Cosmetic curves and UI bindings.** The gait follows the committed speed. A startle cue (a body squash, then upright) eases on the Flee transition. These are animation bindings only.
- **Art (DESIGN.md).** An original, simple silhouette: a pale green `#bff5b0` rounded body (the existing operational pale green, `DESIGN.md@a6c914e:L276-L276`), cream belly `#fff8e9`, navy eyes `#293954` and long ochre `#d69c47` feet that show running. Matte, chamfered and original (`DESIGN.md@a6c914e:L48-L53`).
- **Catalogue and inventory entry.** Id `escaping_character`, title "Skittish critter", category Characters, colour `#bff5b0` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.** Dynamic box contact and friction (`engine/gpu/WorkshopDomino.cs@a6c914e:L7-L35`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L774`); committed body velocities as f32 for closing-speed queries ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)).
- **Missing.** ProgrammableController: S635 controller, next LAW-CONTROLLER-I, source D S653. A visibility query: S484 optical-commit, next S486. Upright constraint and locomotion: Epic 10 joints, owner S653. Finite store: the ENGINE-LEDGER law.
- **Dependencies.** A hazard source: EL-087 Predator or a rolling CAT-014 Bowling ball. CAT-066 Wall as the occluder and route block.

## 4. Sources and legacy

- **Requirement row.** [element-083](../requirements.md#element-083): an original character chooses a bounded avoidance route from perceived hazards; absent or blocked perception does not trigger a hidden scripted escape.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** [S653](../invest/scope-corrections.md#s653): a visible hazard produces the declared move-away; an absent hazard or blocked perception produces none; no scripted escape or new safe-arrival goal. [S635](../invest/decisions.md#s635).
- **Legacy search.** `git grep -i` at a6c914e for "escape", "flee", "hazard" and "character" over every Epic 7 deletion path found no character source. The `Escaped` machine event in legacy tests (`CuriousContraptions.tests/FloorTests.cs@a6c914e:L48-L48`) means a body leaving the world, not this character; it holds no element knowledge.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Visibility is a finite zero-thickness segment, not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | Carry forward the rule. Do not carry forward the CPU sweep. |
| L2 | Clear shells pass light while opaque collars block it | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | Carry forward as the perception occlusion rule |

**Files harvested:** `engine/physics/SegmentOcclusionSweep.cs`, `CuriousContraptions.tests/PipeTests.cs` (occlusion test only), `CuriousContraptions.tests/FloorTests.cs` (checked, `Escaped` is unrelated).

## 5. Acceptance outline

- **Chrome recipe.** Place the Escaping character mid-bench and a Ramp that rolls a Bowling ball toward it. Separately, place an EL-087 Predator 2 m away. Run.
- **Positive.** The approaching ball or the visible predator makes the character run directly away about 3 m, then stop and recover.
- **Negative or control.** With no hazard, it stays idle. A Wall between it and the predator blocks perception, so it does not flee. A resting ball beside it causes no flight. A Wall behind it stops the flight physically, with no pass-through.
- **Boundaries.** A hazard at 2.4 m triggers and one at 2.6 m does not; closing speed 1.4 m/s does not trigger and 1.6 m/s does; flight never exceeds `escape_distance` + one tick of travel.
- **Run/Reset.** Reset restores the pose, state and store. **Save/Load.** Parameters round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-200](../requirements.md#todo-200) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-02 sliding friction](../requirements.md#interaction-02) and [IX-03 joint constraint](../requirements.md#interaction-03) (upright constraint); perception follows the S635 controller decision. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: EL-087 Predator (hazard source), rolling Bowling ball hazard (CAT-014), Wall occluder and route block (CAT-066).

## 6. Open questions

1. Hazard definition: a declared stimulus only, or also generic fast-approaching bodies (proposed). Unspecified — owner decision (S653).
2. Recover duration and whether repeated hazards can re-trigger flight without a limit.
