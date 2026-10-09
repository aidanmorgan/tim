# EL-100 · Vacuum nozzle — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-100 · Vacuum nozzle · Specialist |
| Requirement anchor | [element-100](../requirements.md#element-100); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-100](../invest/named-elements.md#element-100); source owner **S670** |
| CAT spec refined or extended | Extends [CAT-028 Fan](CAT-028-fan.md) (open airflow jet, here reversed toward an inlet) and [CAT-010 Bellows](CAT-010-bellows.md) (supplied pressure work) |
| Related identities | CAT-064 Tennis ball and CAT-003 Balloon (responsive material), EL-080 Programmable box (heavy, non-rolling control), CAT-048 Pipe (bore), EL-042 Air nozzle |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

- **Bodies and shapes.** One static compound body (**proposed** sizes: a tennis-ball-sized inlet, so the opening size is the lesson):
  - Inlet: an open tube, bore radius 0.3 m, half-length 0.4 m, along local +X. It needs annular colliders.
  - Canister: an open five-box container behind it, 0.8 × 0.8 × 0.8 m inside, that collects drawn material.
  - Motor housing: a box 0.8 × 0.4 × 0.8 m, half extents (0.4, 0.2, 0.4), on top of the canister (**proposed**: it matches the canister footprint and closes its top, so drawn material stays inside).
- **Mass and material.** Static; shared static material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`).
- **Constraints.** None.
- **Typed ports.** `PowerIn` (Electrical, Input) on the housing (**proposed**: "supplied pressure/airflow").
- **Sensors and activation.** None. Suction is an airflow field, not a command.
- **Airflow (suction) declaration.** While supplied, an airflow region extends `reach` in front of the inlet with radius `width`. The flow is directed toward the inlet at `flow_speed`. Each exposed dynamic body receives the Fan's bounded jet force law: force = min(cap, conductance × (`flow_speed` − v)) along the flow, where v is the body's speed toward the inlet ([CAT-028 fact 2](CAT-028-fan.md)). The same capped force gives a light body a large acceleration and a heavy one a small acceleration.
  - **Blocked inlet:** if a body or collider occludes the inlet aperture (the sight line from the inlet plane to the sample), the flow is zero. A ball larger than the bore seals it physically.
  - **Missing supply:** zero flow.
- **Work and energy stores.** None. Flow power = force × `flow_speed`, drawn from supply; no supply, no work ([CAT-028](CAT-028-fan.md)).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `force` | f32 | 0–20 | 4 | N (cap) | **proposed**: under half the Fan's 9 N (`parts/catalog/fan.tres@a6c914e:L14-L14`). It accelerates a 0.35 kg Tennis ball at up to about 11 m/s², but cannot slide a non-rolling 6 kg EL-080 Programmable box (Heavy, Wood) resting on the bench: the mixed friction √(0.6 × 0.3) ≈ 0.42 gives a limit of about 25 N against 4 N |
  | `reach` | f32 | 0.5–4 | 2 | m | **proposed**: suction is shorter-ranged than blowing (Fan 5 m) |
  | `width` | f32 | 0.1–1 | 0.4 | m (region radius) | **proposed**: a little wider than the inlet |
  | `flow_speed` | f32 | fixed | 12 | m/s | The Fan's flow speed ([CAT-028](CAT-028-fan.md)) |

- **Cosmetic curves and UI bindings.** Streamline dashes move toward the inlet while flowing and stop when blocked or unsupplied. A slate/gold lamp on the housing shows supply.
- **Art (DESIGN.md).** A cyan `#66b8c9` inlet tube (the Fan colour, `DESIGN.md@a6c914e:L172-L172`) with cream `#fff8e9` collars, a transparent canister, and a navy `#293954` housing with a gold `#f7cb52` lamp.
- **Catalogue and inventory entry.** Id `vacuum_nozzle`, title "Vacuum nozzle", category Power, colour `#66b8c9` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (AerodynamicDrag, EnvironmentState, FiniteLedger, FiniteWorkActuation, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction).

- **Exists now.** Declared linear drag on dynamic spheres (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`). Static five-box containers (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L85`). A bounded force-region pattern (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L144-L161`).
- **Missing.**
  - Airflow region with an occlusion-gated jet force: Story 12.2 (CAT-028); S470 open-versus-sealed, next S697 ([decisions](../invest/decisions.md#s470)).
  - GasState and PressureWork for the canister: S470 gas-state, next S471; S416 pressure-work, next S419.
  - Annular inlet collider: Stories 6.6/6.7. ElectricalPower: Story 8.1.
- **Dependencies.** CAT-028 Fan law, CAT-048 Pipe annular geometry, CAT-005 Battery, CAT-064 Tennis ball, EL-080 Programmable box (control).

## 4. Sources and legacy

- **Requirement row.** [element-100](../requirements.md#element-100): supplied pressure or airflow draws responsive material through a finite opening; a blocked inlet or missing supply prevents suction.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i "vacuum"` at a6c914e finds only the nozzle test's zero back-pressure case, which is a gas law, not a vacuum element. Adjacent facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Choked nozzle mass rate is independent of lower back pressure, and a vacuum back pressure gives more thrust than 30 kPa | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L31-L47` | Do not carry forward the f64 compressible model; keep only the qualitative "flow depends on the pressure difference" |
| L2 | A closed area or balanced pressure gives zero flow and zero force | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | Carry forward as "blocked inlet gives no suction" |
| L3 | Fan defaults: force 9, reach 5, width 0.85; solid obstacles block the direct jet | `parts/catalog/fan.tres@a6c914e:L11-L14` | Carry forward the occlusion rule and the scale |

**Files harvested:** `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`, `parts/catalog/fan.tres`.

## 5. Acceptance outline

- **Chrome recipe.** Place the Vacuum nozzle with a Battery wired to `PowerIn`; place a Tennis ball 1.5 m in front of the inlet. Place an EL-080 Programmable box (Medium, Heavy, Wood; 6 kg) resting on the bench 1 m in front of a second supplied nozzle, and a Basketball resting at the mouth of a third. Run.
- **Positive.** The Tennis ball is drawn in through the inlet and lands in the canister.
- **Negative or control.** The 6 kg box does not move: 4 N cannot overcome its friction limit of about 25 N, and as a box it cannot roll. The Basketball seals its inlet: no flow, and a Tennis ball behind it stays put. Without supply, nothing moves. A Wall between the inlet and the ball blocks suction.
- **Boundaries.** A ball at 2.1 m (outside `reach`) is not drawn; force never exceeds `force`, so a body's acceleration never exceeds `force` ÷ mass; flow work never exceeds supply.
- **Run/Reset.** Reset restores all bodies and an empty canister. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-06 electrical power transfer](../requirements.md#interaction-06), [IX-09 pressure work](../requirements.md#interaction-09), [IX-11 aerodynamic drag](../requirements.md#interaction-11) and [IX-40 gas state evolution](../requirements.md#interaction-40). Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim, and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) names no vacuum row; the nearest family is the row beginning "Bellows, pneumatic hose, air reservoir" (71–80, reuse 91–100, 136–150), and the ledger task must add a separate row before campaign use. Element integrations: Fan jet law (CAT-028), Battery supply (CAT-005), Tennis ball (CAT-064), EL-080 Programmable box control.

## 6. Open questions

1. Airflow field (proposed) versus a sealed pressure model through the inlet tube. Unspecified — owner decision (S670, S470).
2. Whether captured material can be released from the canister later.
