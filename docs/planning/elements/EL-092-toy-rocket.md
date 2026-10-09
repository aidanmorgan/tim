# EL-092 · Toy rocket — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

**Safety boundary.** This is an abstract toy. Its charge is a declared impulse and energy budget in SI units only; no real propellant, formulation, quantity or construction is modelled or described (requirement row).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-092 · Toy rocket · Specialist |
| Requirement anchor | [element-092](../requirements.md#element-092); scope index [todo-197](../requirements.md#todo-197) |
| Named entry | [element-092](../invest/named-elements.md#element-092); source owner **S662** |
| CAT spec refined or extended | None. Its exhaust is an airflow like [CAT-028 Fan](CAT-028-fan.md); its finite store follows [CAT-016 Toy cannon](CAT-016-cannon.md). |
| Related identities | EL-104 Toy firework, EL-105 Toy missile (the same thrust law), EL-094 Detonation plunger (an ignition command source) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

- **Bodies and shapes.** One dynamic body: a box 0.24 × 0.9 × 0.24 m, half extents (0.12, 0.45, 0.12), with the nozzle at local −Y (**proposed**: a slim toy rocket about two Basketball diameters tall). Cosmetic fins do not collide.
- **Mass and material.** 0.4 kg, constant (**proposed**: the propellant mass is not tracked; the toy abstraction is an impulse budget). Restitution 0.2, friction 0.4 (**proposed**: a cardboard tube).
- **Constraints.** None; a free body. The launch stand is a separate static fixture (a Wall box) if wanted.
- **Typed ports.** `ActivationIn` (Activation, Input) at the base (0, −0.45, 0.12) (**proposed**: ignition is a typed command, for example from a Switch or EL-094).
- **Sensors and activation.** One ignition command starts the burn. A command while burning or spent is ignored. A spent rocket accepts no further ignition until Reset.
- **Work and energy stores.** A finite thrust store expressed as a total impulse. During the burn, the solver applies `thrust` along body +Y at the centre of mass until the delivered impulse reaches `total_impulse`. Then it is **Spent**: zero thrust for the rest of the Run.
- **Exhaust.** While burning, a bounded exhaust airflow region extends 1.0 m behind the nozzle (radius 0.2 m) and pushes exposed dynamic bodies with at most 1 N. It draws on the same store and never adds energy (**proposed**: makes "exhaust" physical but weak, using the Fan's jet law, [CAT-028](CAT-028-fan.md)).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `thrust` | f32 | 1–10 | 6 | N | **proposed**: 15 m/s² at 0.4 kg, a net 5.2 m/s² climb against gravity, readable at bench scale |
  | `total_impulse` | f32 | 0.5–10 | 6 | N·s | **proposed**: a 1 s burn at default thrust, about 4 m of climb |

  Burn duration = `total_impulse` ÷ `thrust`. Peak speed stays far below the 64 m/s envelope bound ([envelope](../../gpu-f32-physics.md#game-grade-envelope)).
- **Cosmetic curves and UI bindings.** The exhaust plume scales with committed thrust and disappears when spent. A fuel band on the body drains with the remaining impulse fraction. A soot mark appears when spent. These are animation bindings.
- **Art (DESIGN.md).** A cream `#fff8e9` body, coral `#de7058` nose and fins (the gizmo coral, `DESIGN.md@a6c914e:L158-L158`), a navy `#293954` band and a gold `#f7cb52` plume core. Rounded toy forms (`DESIGN.md@a6c914e:L48-L48`).
- **Catalogue and inventory entry.** Id `toy_rocket`, title "Toy rocket", category Motion, colour `#de7058`, counted allowance (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ChemicalReaction, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, SensibleHeat, StateTransaction, StructuralFracture, TopologyTransaction).

- **Exists now.** Dynamic boxes with derived inertia (`engine/gpu/WorkshopDomino.cs@a6c914e:L7-L35`; `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L30`). Activation input sockets and coalesced activation occurrences (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`; `engine/gpu/ActivationTimers.cs@a6c914e:L53-L66`).
- **Missing.**
  - A body-fixed finite thrust actuator (FiniteWorkActuation, FiniteLedger): owner S662.
  - ChemicalReaction (the abstract charge as a reaction with a finite reactant): S543 reaction, next S554; ignition, next S555 ([decisions](../invest/decisions.md#s543)).
  - Exhaust airflow region: Story 12.2 (CAT-028); S470 open-versus-sealed, next S697.
  - SensibleHeat release from the exhaust: thermal family (S543 convection, next S545), not needed for the first slice.
- **Dependencies.** CAT-063 Switch or EL-094 Plunger for ignition; CAT-028 Fan law for the exhaust.

## 4. Sources and legacy

- **Requirement row.** [element-092](../requirements.md#element-092): finite stored propellant or energy produces bounded thrust and exhaust; a spent rocket produces no further thrust; abstract toy parameters only.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** [S543](../invest/decisions.md#s543) reaction and ignition; the [ENGINE-LEDGER](../invest/scope-corrections.md#engine-ledger) no-free-energy rule.
- **Legacy search.** `git grep -i "rocket"` at a6c914e returned nothing. The legacy gas nozzle laws are the nearest thrust source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Thrust = momentum rate + pressure force; thrust impulse = momentum impulse + pressure impulse | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L13-L29`; `engine/physics/GasNozzleTransit.cs@a6c914e:L39-L39` | Carry forward the decomposition as background. Do not carry forward the f64 gas model: the toy uses a declared impulse budget. |
| L2 | A closed or balanced nozzle has zero mass flow and zero thrust | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74`; `engine/physics/GasNozzleTransit.cs@a6c914e:L73-L74` | Carry forward as "no reservoir, no thrust" |
| L3 | Discharge is finite: a reservoir discharges only to a stated remaining inventory, and zero change gives zero transit | `CuriousContraptions.tests/GasNozzleTransitTests.cs@a6c914e:L89-L103` | Carry forward as "spent produces nothing". Do not carry forward the adaptive quadrature (proof-grade). |
| L4 | A finite store debit never exceeds stored energy | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52` | Carry forward the rule |

**Files harvested:** `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`, `engine/physics/GasNozzleTransit.cs`, `CuriousContraptions.tests/GasNozzleTransitTests.cs`, `engine/physics/PhysicsEnergyStore.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Stand the Toy rocket upright on the bench; wire a Switch's `ActivationOut` to its `ActivationIn`; drop a Basketball on the Switch. Place a Tennis ball 0.5 m behind the nozzle. Run.
- **Positive.** The rocket ignites, climbs on a visible plume for about 1 s, coasts, falls and comes to rest. The Tennis ball is nudged by the exhaust.
- **Negative or control.** No trigger: no motion. A second trigger after burnout: nothing. A Wall above it stops the climb by contact, and the thrust still ends at the same impulse. The exhaust never pushes a body harder than 1 N.
- **Boundaries.** Delivered impulse equals `total_impulse` within the envelope; the burn ends at `total_impulse` ÷ `thrust`; out-of-range parameters are rejected at input.
- **Run/Reset.** Reset restores the pose, a full store and the unspent state. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-197](../requirements.md#todo-197) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-11 aerodynamic drag](../requirements.md#interaction-11) (exhaust), [IX-28 chemical reaction](../requirements.md#interaction-28), [IX-29 reaction ignition](../requirements.md#interaction-29) and [IX-43 sensible heat storage](../requirements.md#interaction-43). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" (rocket) reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" ("toy rocket/ignition/destruction variants") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Switch (CAT-063) or EL-094 Plunger ignition, Fan jet law for the exhaust (CAT-028), Tennis ball as an exhaust target (CAT-064).

## 6. Open questions

1. Whether mass should decrease as the charge burns (proposed: constant mass). Unspecified — owner decision (S662).
2. Exhaust heat (SensibleHeat) and ignition by heat rather than by command.
3. Whether the exhaust push is needed in the first slice.
