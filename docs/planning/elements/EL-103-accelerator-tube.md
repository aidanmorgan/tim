# EL-103 · Accelerator tube — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-103 · Accelerator tube · Specialist |
| Requirement anchor | [element-103](../requirements.md#element-103); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-103](../invest/named-elements.md#element-103); source owner **S363** |
| CAT spec refined or extended | Extends [CAT-048 Clear pipe](CAT-048-pipe.md) (the standard bore) with a supplied axial coupling field |
| Related identities | EL-102 Large-bore ball pipe, CAT-005 Battery, CAT-002 Ball detector, CAT-019 Conveyor (another supplied transport) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The tube adds energy only through a declared, supplied coupling field acting on bodies inside its bore. It never writes an exit velocity, teleports cargo or boosts an unsupplied ball, consistent with the clear-pipe rule "no transport teleport, velocity boost or control overlay" (`DESIGN.md@a6c914e:L299-L299`).

- **Bodies and shapes.** One static annular tube with the standard pipe profile: bore radius 0.65 m, shell 0.70 m, collars 0.78 m with half-width 0.09 m (`engine/gpu/WorkshopPipe.cs@a6c914e:L9-L18`). Fixed length 3.6 m (**proposed**: the standard default, `engine/gpu/WorkshopPipe.cs@a6c914e:L8-L8`; a fixed length keeps the work budget readable). Two driver rings sit on the shell as art.
- **Mass and material.** Static; the static-surface convention (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`).
- **Constraints.** None.
- **Typed ports.** `PowerIn` (Electrical, Input) on the shell midpoint (**proposed**). Two standard `BoreClass.Standard` mouths that snap like the clear pipe.
- **Sensors and activation.** None. The field acts continuously while supplied.
- **Coupling field.** A force region equal to the bore cylinder. Each dynamic body whose centre is inside receives an axial force toward the +X mouth: F = min(`max_force`, k × (`target_speed` − v_axial)), with k = `max_force` ÷ 2 m/s, and zero when v_axial ≥ `target_speed` (**proposed**: the same bounded slip law as the Fan jet, [CAT-028 fact 2](CAT-028-fan.md)). The total power over all bodies is capped at `power_rating`; demand above the cap is scaled proportionally ([envelope](../../gpu-f32-physics.md#game-grade-envelope)). Unsupplied, F = 0: the tube is then a passive clear pipe.
- **Work and energy stores.** None. The work comes from supply: F × v per body, debited from the electrical source.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `target_speed` | f32 | 1–16 | 8 | m/s | **proposed**: a clear boost, below the cannon's legacy 16 m/s cap ([CAT-016](CAT-016-cannon.md)) |
  | `max_force` | f32 | 1–20 | 6 | N | **proposed**: 6 m/s² on a 1 kg Basketball, gentle enough to watch |
  | `power_rating` | f32 | 5–100 | 40 | W | **proposed**: enough for one ball at full force near the target speed |
  | `direction` | enum `TubeDirection` | Positive, Negative | Positive | — | **proposed**: which mouth the field pushes toward |

- **Cosmetic curves and UI bindings.** Driver rings glow in sequence along the travel direction while supplied; the glow follows committed supplied power, not ball position.
- **Art (DESIGN.md).** The clear pipe language: a transparent cyan shell, cream collars and navy rails, plus two gold `#f7cb52` driver rings and a navy direction chevron (`DESIGN.md@a6c914e:L299-L299`).
- **Catalogue and inventory entry.** Id `accelerator_tube`, title "Accelerator tube", category Power, colour `#66b8c9` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-02.json` (ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction).

- **Exists now.** A bounded force-region declaration with a frame, a target and a maximum acceleration (the planar guide, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L144-L161`), evaluated each substep in the worker (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1225-L1227`). The pipe profile, parked (`engine/gpu/WorkshopPipe.cs@a6c914e:L6-L41`).
- **Missing.**
  - Annular collider and mouths: Stories 6.6/6.7 (CAT-048).
  - ElectricalPower: Story 8.1; S257 electrical-port, next S270.
  - A supplied axial force region that applies to any dynamic body and debits supply (FiniteWorkActuation): owner S363.
- **Dependencies.** CAT-048 Pipe, CAT-005 Battery; CAT-002 Ball detector for a speed-gate integration.

## 4. Sources and legacy

- **Requirement row.** [element-103](../requirements.md#element-103): explicit supply adds bounded work through a declared coupling field; an unpowered tube cannot increase cargo energy.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "accelerator" and "booster" found nothing. Adjacent facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Jet force = min(cap, conductance × (12 − v)); zero at or above flow speed; power = force × 12 | `engine/physics/JetTransferImpedance.cs@a6c914e:L26-L46` (via [CAT-028](CAT-028-fan.md)) | Carry forward the bounded slip law shape. Do not carry forward the CPU class. |
| L2 | Actuator work is bounded by available work; dissipated energy is never reused | `engine/physics/PoweredImpulse.cs@a6c914e:L7-L26` | Carry forward |
| L3 | Clear pipe: gravity carries balls through the open bore; tilt it downhill to guide them | `parts/catalog/pipe.tres@a6c914e:L17-L17` | Carry forward: unsupplied, the tube behaves exactly as a clear pipe |

**Files harvested:** `engine/physics/JetTransferImpedance.cs`, `engine/physics/PoweredImpulse.cs`, `parts/catalog/pipe.tres`.

## 5. Acceptance outline

- **Chrome recipe.** Lay an Accelerator tube level on supports, wire a Battery to `PowerIn`, and roll a Basketball into its −X mouth at about 1 m/s. Place an identical unsupplied tube beside it with its own ball. Run.
- **Positive.** The supplied tube accelerates the ball toward 8 m/s; it exits the +X mouth visibly faster.
- **Negative or control.** The unsupplied tube's ball exits no faster than it entered (minus drag and rolling loss). A ball entering already above 8 m/s gains nothing. A ball beside the tube is unaffected.
- **Boundaries.** Kinetic energy gained ≤ supply energy delivered; per-body force ≤ `max_force`; two balls inside share the 40 W cap.
- **Run/Reset.** Reset restores the balls exactly. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-06 electrical power transfer](../requirements.md#interaction-06). Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim, and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Ball straight pipe, both bend angles, funnel" reserves levels 21–30 with "specialist routes 91–100" (reuse 41–50, 111–120, 136–150). Element integrations: Clear pipe mouths (CAT-048), Battery supply (CAT-005), Ball detector speed gate (CAT-002), Basketball cargo (CAT-001).

## 6. Open questions

1. Field law: bounded slip toward a target speed (proposed) versus a constant force. Unspecified — owner decision (S363).
2. Resizable length, as the clear pipe has.
3. Whether reversing is a parameter (proposed) or a separate command input.
