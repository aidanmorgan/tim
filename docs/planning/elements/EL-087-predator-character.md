# EL-087 · Predator character — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-087 · Predator character · Character |
| Requirement anchor | [element-087](../requirements.md#element-087); scope index [todo-221](../requirements.md#todo-221) |
| Named entry | [element-087](../invest/named-elements.md#element-087); source owner **S657** |
| CAT spec refined or extended | None |
| Related identities | EL-082 Lured (the same pursuit law; [EL-082 spec](EL-082-lured-character.md) holds the shared framework), EL-083 Escaping (perceives this as a hazard), EL-088 Fish tank lure (prey stimulus behind glass) |
| Roadmap story | Unscheduled (aggregate [LAW-CONTROLLER-I](../invest/scope-corrections.md#law-controller-i)) |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

**Declaration-only rule.** A typed perception query, a shared finite-state policy and locomotion; a target is never addressed by name or identity, only by perceived stimulus; physics resolves reachability ([engine contracts](../../engine-contracts.md); `docs/general-engine-design.md@a6c914e:L132-L132`).

- **Bodies and shapes.** One dynamic upright box 0.8 × 0.7 × 0.45 m, half extents (0.4, 0.35, 0.225) (**proposed**: visibly larger than the lured and escaping characters, so the predator role reads at a glance). Its bottom face is the foot and its front face (+X) is the mouth; both are faces of the single box, not separate colliders, so no dynamic compound body is needed.
- **Mass and material.** 3.0 kg, friction 0.8, restitution 0.1, threshold 0.1 m/s (**proposed**: twice EL-082's mass, so a light character cannot push it aside).
- **Constraints.** An upright constraint locks tipping about local X and Z; yaw is free (**proposed**, as EL-082).
- **Typed ports.** Optional `ActivationOut` (Activation, Output) at the head (0.4, 0.35, 0) (**proposed**: lets a catch drive a machine, using the existing socket, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).
- **Sensors and activation (perception and contact).**
  - **Perception.** A forward cone of half-angle 60° (**proposed**: the EL-082 field of view, so prey behind it is not seen) within `perception_range`, with a sight line where opaque blocks and transparent passes. It perceives `StimulusKind.Prey` sources (**proposed**: lured characters and EL-088 lures declare Prey).
  - **Hazard emission.** It declares a `StimulusKind.Hazard` source that EL-083 perceives.
  - **Contact policy.** A contact between its mouth face and the body that is the Prey source commits one `Caught` occurrence on `ActivationOut` and puts the predator in `Rest`. A body that only encloses a Prey source, such as the EL-088 tank glass around its fish, is not itself Prey. Prey is not destroyed or moved by the rule; any push is ordinary contact (**proposed**: keeps the outcome generic and reversible).
- **Policy (shared ProgrammableController data).** States `Idle`, `Pursue` and `Rest`. A perceived prey stimulus turns Idle into Pursue; losing perception returns to Idle. A catch goes to Rest until Reset. "Reachable routes" are physical: it drives toward the stimulus along the ground; a barrier stops it by contact; it does not path-plan, climb or teleport.
- **Work and energy stores.** A finite locomotion store of 400 J (**proposed**: about 18.5 s of full drive at 21.6 W, that is 18 N at 1.2 m/s, inside the 30 s attempt limit).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `perception_range` | f32 | 1–8 | 5 | m | **proposed**: longer than the lured character, so it notices first |
  | `max_speed` | f32 | 0.2–2.5 | 1.2 | m/s | **proposed**: slower than the escaping character (1.5), so escapes are possible |
  | `drive_force` | f32 | 1–30 | 18 | N | **proposed**: 6 m/s² at 3 kg, the same acceleration as EL-082 |

- **Cosmetic curves and UI bindings.** The gait follows the committed speed; a crouch cue eases in during Pursue; a contented curl shows in Rest. All are animation bindings.
- **Art (DESIGN.md).** An original simple silhouette: a blue `#45639c` rounded body (the Bowling ball and Weight blue, `DESIGN.md@a6c914e:L166-L166`), cream `#fff8e9` muzzle, gold `#f7cb52` eyes and two pointed ear wedges. Friendly toy proportions, not menacing (`DESIGN.md@a6c914e:L135-L135`). Original (`DESIGN.md@a6c914e:L53-L53`).
- **Catalogue and inventory entry.** Id `predator_character`, title "Prowler", category Characters, colour `#45639c` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.** Dynamic box contact (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L774`). Activation output sockets and edges (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L62`; `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`).
- **Missing.**
  - ProgrammableController: S635 controller, next LAW-CONTROLLER-I; source D S657.
  - Visibility query: S484 optical-commit, next S486.
  - A face-filtered contact observation on a dynamic owner (contact triggers exist only on static owners, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1177`): owner S657.
  - Upright constraint and locomotion: Epic 10 joints. Finite store: the ENGINE-LEDGER law.
- **Dependencies.** EL-082 Lured character or EL-088 Fish tank lure as prey; CAT-066 Wall as the occluder; the EL-088 tank glass as the transparent barrier; EL-083 for the hazard integration.

## 4. Sources and legacy

- **Requirement row.** [element-087](../requirements.md#element-087): an original character responds to declared perceivable stimuli and reachable routes; a barrier prevents traversal; target names cannot bypass sensing.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** [S635 controller](../invest/decisions.md#s635); the element-map note "predator perception, pursuit and contact policy" (Source D + LAW-CONTROLLER-D). Research reference: TIM2 manual ([todo-221](../requirements.md#todo-221)).
- **Legacy search.** `git grep -i` at a6c914e for "predator", "cat", "pursu", "prey" and "character" over every Epic 7 deletion path found no predator source. The occlusion facts below apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Visibility is a finite zero-thickness segment, not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | Carry forward the rule. Do not carry forward the CPU sweep. |
| L2 | Clear shells pass light while opaque collars block it | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | Carry forward as the occlusion rule |

**Files harvested:** `engine/physics/SegmentOcclusionSweep.cs`, `CuriousContraptions.tests/PipeTests.cs` (occlusion test only).

## 5. Acceptance outline

- **Chrome recipe.** Place the Predator at the bench left and a Lured character 3 m right; wire the Predator's `ActivationOut` to a Lamp. Keep a Wall ready to slide between them. On a second lane, place an EL-088 Fish tank 3 m in front of a second Predator. Run.
- **Positive.** The Predator sees the prey, walks to it on its feet, touches it with its mouth face, the Lamp lights once, and it rests.
- **Negative or control.** A Wall between them: it does not perceive and stays idle. Transparent barrier: the second Predator perceives the fish through the tank glass (transparent walls, 0.7 m tall, pass sight) and pursues, but the glass stops it by contact; it presses against the tank, never reaches the fish, never routes around it, and no `Caught` occurs, because the glass is not the Prey body. A non-prey body (a Basketball) at the prey's place is ignored. Renaming or re-identifying a part changes nothing; only stimulus declarations matter.
- **Boundaries.** Range edge at 5 m; one `Caught` per Run; the store bounds total travel.
- **Run/Reset.** Reset restores the pose, state, store and Lamp. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-221](../requirements.md#todo-221) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-02 sliding friction](../requirements.md#interaction-02), [IX-03 joint constraint](../requirements.md#interaction-03) and [IX-07 signal propagation](../requirements.md#interaction-07) (`Caught` output). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" (predator) reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: EL-082 Lured character (prey), EL-083 Escaping character (hazard perceiver), EL-088 Fish tank lure (prey behind glass), Signal lamp (CAT-035).

## 6. Open questions

1. Catch outcome: an occurrence only (proposed), or a physical effect on the prey. Unspecified — owner decision (S657).
2. Rest duration: until Reset (proposed) or a timed resume.
