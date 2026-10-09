# EL-079 · Electric mixer — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-079 · Electric mixer · Specialist |
| Requirement anchor | [element-079](../requirements.md#element-079); scope index [todo-218](../requirements.md#todo-218) |
| Named entry | [element-079](../invest/named-elements.md#element-079); source owner **S649** |
| CAT spec refined or extended | Extends [CAT-042 Electric motor](CAT-042-motor.md) (supplied shaft torque) with an attached contact tool |
| Related identities | EL-078 Can opener, EL-069 Moving bucket (open container), GAP-06 Granular dispenser (future contents) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The element-map composition rule applies: freeze a physical supplied shaft and tool and the affected material interaction; there is no mixer-name outcome command ([element map](../general-engine-element-map.md)).

- **Bodies and shapes.**
  - Static head: box 0.9 × 0.5 × 0.6 m, half extents (0.45, 0.25, 0.3), held above the bowl on a static column box 0.2 × 1.6 × 0.2 m (**proposed**: the motor-sized head reads as a stand mixer at bench scale).
  - Beater: a dynamic paddle on a vertical revolute joint under the head, built from two crossed boxes, each 0.6 × 0.5 × 0.04 m (**proposed**: each paddle sweeps a 0.3 m radius about the axis, leaving 0.1 m clearance to the bowl walls, which stand 0.4 m from the axis).
  - Bowl: a static open container of five boxes, 0.8 × 0.5 × 0.8 m inside (**proposed**: the same open-box construction as the Receiver, `engine/gpu/ReceiverGeometry.cs@a6c914e:L8-L19`).
- **Mass and material.** Beater 0.3 kg, friction 0.5, restitution 0.2 (**proposed**: a light metal paddle that grips contents). Head, column and bowl are static with the shared static material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`).
- **Constraints.** Beater revolute joint about local Y, driven by a supplied motor row with bounded torque and regulated speed.
- **Typed ports.** `PowerIn` (Electrical, Input) on the head's rear face (−0.45, 0, 0) (**proposed**). No output port; the result is material motion.
- **Sensors and activation.** None owned. The processed result is the contents' actual motion, observed by generic sensors (Receiver residence, a pressure plate or capture). A stalled beater moves nothing, and no "mixed" event exists without that motion.
- **Work and energy stores.** None. Work comes from supply through the motor row; a stalled beater draws no contact work.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `speed` | f32 | 1–12 | 6 | rad/s | Legacy motor default 6 (`parts/catalog/motor.tres@a6c914e:L14-L14`); range **proposed** |
  | `torque` | f32 | 1–20 | 8 | N·m | **proposed**: below the 20 N·m motor default (same line), so a wedged bowl visibly stalls it |
  | `direction` | enum `RotationDirection` | Clockwise, Counterclockwise | Clockwise | — | **proposed** |

- **Cosmetic curves and UI bindings.** The beater follows its committed joint angle. The supply lamp eases slate `#556573` to gold `#f7cb52`. A stall cue (the lamp pulses ochre `#d69c47`) is driven by the committed speed falling below 10 % of `speed` while supplied.
- **Art.** Cyan head `#66b8c9` (the motor colour, `DESIGN.md@a6c914e:L180-L180`), cream column and bowl `#fff8e9`, navy foot `#293954`, slate beater with gold tips. Original pictogram of a whisk over a bowl.
- **Catalogue and inventory entry.** Id `electric_mixer`, title "Electric mixer", category Power, colour `#66b8c9` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.** Static compound box geometry (the Receiver precedent, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L85`); dynamic spheres as contents; contact and friction (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L774`).
- **Missing.**
  - Revolute joint with a supplied motor row (JointConstraint, FiniteWorkActuation): Epic 10 joints and Story 11.1 motor (CAT-042); S257 mechanical-port, next S690.
  - ElectricalPower: Story 8.1; S257 electrical-port, next S270.
  - Dynamic compound bodies (the crossed-paddle beater): `RigidMassProperties.Compile` takes one collider per dynamic body (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L30`). Owner: the S649 source D.
- **Dependencies.** CAT-005 Battery, CAT-042 Motor; contents such as CAT-064 Tennis balls.

## 4. Sources and legacy

- **Requirement row.** [element-079](../requirements.md#element-079): a supplied rotating tool moves material through actual torque and contact; stall or missing contents cannot create a processed result.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Research reference.** TIM2 inventory ([todo-218](../requirements.md#todo-218)).
- **Legacy search.** `git grep -i "mixer"` at a6c914e over every Epic 7 deletion path returned nothing. Motor facts apply; [CAT-042](CAT-042-motor.md) holds the full motor harvest.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Motor catalogue defaults: speed 6 (rad/s), torque 20 | `parts/catalog/motor.tres@a6c914e:L14-L14` | Carry forward the speed default; torque lowered (proposed) |
| L2 | Motor: electricity turns its shaft; the drive socket is mechanical | `parts/catalog/motor.tres@a6c914e:L11-L11` | Carry forward the supply rule |
| L3 | Actuator: positive work is bounded by available work; dissipated energy is never available for reverse acceleration | `engine/physics/PoweredImpulse.cs@a6c914e:L7-L26` | Carry forward the rule. Do not carry forward the CPU double implementation. |

**Files harvested:** `parts/catalog/motor.tres`, `engine/physics/PoweredImpulse.cs` (generic rule only).

## 5. Acceptance outline

- **Chrome recipe.** Place a Battery and the Electric mixer; drop four Tennis balls into the bowl through the real palette; wire Battery `Supply` → `PowerIn`. Run.
- **Positive.** The beater turns and the balls circulate; their committed positions change continuously around the bowl.
- **Negative or control.** Without supply, nothing moves. An empty bowl gives a turning beater and no processed result. A Wall box wedged against the beater stalls it: the balls stop and the stall cue shows.
- **Boundaries.** Speed 1 and 12 rad/s; torque 1 N·m cannot move four balls while 20 N·m can (the threshold is measured at S649); no ball leaves the bowl at the default speed.
- **Run/Reset.** Reset restores the beater angle and the content positions exactly. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-218](../requirements.md#todo-218) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-02 sliding friction](../requirements.md#interaction-02), [IX-03 joint constraint](../requirements.md#interaction-03), [IX-05 shaft torque transmission](../requirements.md#interaction-05) and [IX-06 electrical power transfer](../requirements.md#interaction-06). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row "Size grate, weight tray/material sorter, timed ejector/toaster, egg timer, phazer-like toy pulse source, opener, mixer, box presets and contact display" reserves levels 91–100 (reuse 111–120, 136–150); chapter 10 "Oddly Satisfying" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Battery supply (CAT-005), Motor law (CAT-042), Tennis balls as contents (CAT-064).

## 6. Open questions

1. What counts as "processed": physical motion only (proposed), or a declared material-state change for granular contents (needs GAP-06). Unspecified — owner decision (S649).
2. Bowl ownership: part of the mixer (proposed) or a separate container element (EL-069).
