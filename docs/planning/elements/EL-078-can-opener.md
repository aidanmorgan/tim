# EL-078 · Can opener — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-078 · Can opener · Specialist |
| Requirement anchor | [element-078](../requirements.md#element-078); scope index [todo-218](../requirements.md#todo-218) |
| Named entry | [element-078](../invest/named-elements.md#element-078); source owner **S648** |
| CAT spec refined or extended | None. It consumes supply from [CAT-005 Battery](CAT-005-battery.md) and rotary drive like [CAT-042 Motor](CAT-042-motor.md). |
| Related identities | EL-066 Scissors and EL-068 Tin snips (the same generic cutting work), EL-079 Electric mixer (supplied rotating tool) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The element-map composition rule applies: freeze a supplied cutting, contact and material-failure capability; ProgrammableController is not a can-opening script ([element map](../general-engine-element-map.md)).

- **Bodies and shapes.**
  - Static housing: box 0.8 × 1.0 × 0.6 m, half extents (0.4, 0.5, 0.3) (**proposed**: about one Receiver width, so a can fits beside it).
  - Cutter wheel: a dynamic disc r 0.12 m, thickness 0.04 m, on a revolute joint at the housing's front face, local (0.4, 0.3, 0) (**proposed**: small against a 0.3 m can rim, so the contact patch is readable).
  - Seat: a static cradle box 0.7 × 0.3 × 0.7 m, half extents (0.35, 0.15, 0.35), centred at local (0.82, −0.35, 0), so its top sits at y −0.2 (**proposed**: a seated 0.5 m-tall can then has its upper rim at y 0.3, the wheel height, with its side against the wheel's outer edge at x 0.52).
- **Compatible container (declared with this element).** A sealed can: a dynamic cylinder r 0.3 m, height 0.5 m, 0.5 kg, with a lid body held by a **seam** declaration (**proposed**: the row needs a "compatible container seam", and no other identity declares one). The seam carries a cutting-work resistance in joules; the lid is released by the shared fracture transaction only when the cumulative cut reaches the full rim.
- **Mass and material.** Housing static. Cutter wheel 0.1 kg, friction 0.6 (**proposed**: a toothed steel wheel grips the rim). Can material restitution 0.2, friction 0.4 (**proposed**: a thin metal shell, dull bounce).
- **Constraints.** Cutter revolute joint about local Z, driven by supplied torque. The seam is a breakable constraint between can and lid.
- **Typed ports.** `PowerIn` (Electrical, Input) at the rear (−0.4, 0, 0) (**proposed**). There is no activation input: "supplied tool" means it runs while supplied.
- **Sensors and activation.** Alignment is physical: cutting work transfers only through actual cutter–rim contact. **Alignment tolerance (proposed):** rim contact within 0.05 m of the wheel's cutting edge and can axis within 10° of the housing's vertical; outside it the wheel slips and delivers no cutting work.
- **Work and energy stores.** No store. Cutting work is drawn from supply at `cutting_power` and debited from the seam's remaining resistance.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `cutting_power` | f32 | 1–20 | 5 | W | **proposed**: with the default seam it opens in about 2 s, readable progress |
  | `seam_work` (on the can) | f32 | 1–50 | 10 | J | **proposed**: two seconds of default supply |

- **Cosmetic curves and UI bindings.** A cut-progress arc on the can lid follows the committed seam progress fraction (0–1). The wheel spins from its committed joint angle. A slate/gold lamp on the housing shows supply.
- **Art.** Cream housing `#fff8e9`, navy foot `#293954`, slate wheel `#556573` with gold teeth `#f7cb52`; can in pale grey metal with a cream label band (`DESIGN.md@a6c914e:L189-L193`). Original pictogram of a wheel on a rim.
- **Catalogue and inventory entry.** Id `can_opener`, title "Can opener", category Power; the can is a separate counted payload entry `sealed_can` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.** Static and dynamic boxes and spheres, contact and friction (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`). The finite paid-work pattern (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L36`). The `PowerIn` socket (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).
- **Missing.**
  - Cylinder or disc colliders: only Sphere, Box and Plane exist (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`). Owner: the S648 source D.
  - Revolute joints and supplied shaft torque: Epic 10 (Stories 10.3/10.2) and Story 11.1 (CAT-042); S257 mechanical-port, next S690.
  - ElectricalPower: Story 8.1; S257 electrical-port, next S270.
  - Cutting work and seam failure (StructuralFracture, TopologyTransaction): S543 fracture, next S563 ([decisions](../invest/decisions.md#s543)), shared with EL-066/068.
- **Dependencies.** CAT-005 Battery, CAT-042 Motor law, EL-066 Scissors cutting law (whichever lands first owns it).

## 4. Sources and legacy

- **Requirement row.** [element-078](../requirements.md#element-078): a supplied tool progressively cuts a compatible container seam using generic cutting work; misalignment and missing supply leave the seal intact.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Research reference.** TIM2 inventory ([todo-218](../requirements.md#todo-218)).
- **Decisions.** S257, S543 ([decisions](../invest/decisions.md)); the design note on cutting work: [general-engine design](../../general-engine-design.md) ("Scissors declare contact geometry, cutting work and material failure").
- **Legacy search.** `git grep -i` at a6c914e for "can opener", "cut", "sever", "seam" and "blade" over every Epic 7 deletion path found no cutting source. Blade hits are the powered gate and shutter sliders (CAT-051, CAT-007), which hold no cutting knowledge.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Finite store charge accepts `min(capacity − energy, power × dt)`; a debit never exceeds stored energy | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L52` | Carry forward the rule as "cut work never exceeds supplied work". Do not carry forward the CPU double ledger. |

**Files harvested:** `engine/physics/PhysicsEnergyStore.cs` (generic finite-work rule only).

## 5. Acceptance outline

- **Chrome recipe.** Place a Battery, the Can opener and a Sealed can on the seat with the move gizmo; wire Battery `Supply` → `PowerIn`. Run.
- **Positive.** The wheel turns, the progress arc grows, and after about 2 s the lid separates as a real body and the can is open.
- **Negative or control.** Without supply, the wheel is still and the seal stays intact. A can moved 0.2 m off the seat, or tilted 30°, is not cut. Removing supply mid-cut stops the progress, which resumes from the same fraction when supply returns.
- **Boundaries.** Progress never exceeds the delivered work divided by `seam_work`; the lid is not released at 99 % progress; out-of-range parameters are rejected at input.
- **Run/Reset.** Reset restores the sealed can, lid and zero progress. **Save/Load.** Placement, wiring and parameters round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-218](../requirements.md#todo-218) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-03 joint constraint](../requirements.md#interaction-03), [IX-05 shaft torque transmission](../requirements.md#interaction-05), [IX-06 electrical power transfer](../requirements.md#interaction-06) and [IX-37 structural fracture](../requirements.md#interaction-37). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row "Size grate, weight tray/material sorter, timed ejector/toaster, egg timer, phazer-like toy pulse source, opener, mixer, box presets and contact display" reserves levels 91–100 (reuse 111–120, 136–150); chapter 10 "Oddly Satisfying" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Battery supply (CAT-005), Motor law (CAT-042), shared cutting law with EL-066 Scissors and EL-068 Tin snips.

## 6. Open questions

1. Container identity: should the sealed can be its own named element or a fixture? Unspecified — owner decision (S648).
2. Whether partial cut progress persists across supply loss (proposed) or decays.
3. Cutting-work law ownership between EL-066, EL-068 and EL-078.
