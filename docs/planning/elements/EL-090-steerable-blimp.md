# EL-090 · Steerable blimp — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-090 · Steerable blimp · Specialist |
| Requirement anchor | [element-090](../requirements.md#element-090); scope index [todo-216](../requirements.md#todo-216) |
| Named entry | [element-090](../invest/named-elements.md#element-090); source owner **S660** |
| CAT spec refined or extended | Extends [CAT-003 Balloon](CAT-003-balloon.md) (buoyant gas body) with a supplied drive; its current is [CAT-028 Fan](CAT-028-fan.md) |
| Related identities | EL-208 Balloon, EL-125 Authored atmosphere preset, CAT-005 Battery |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

- **Bodies and shapes.** One dynamic body:
  - Envelope: three spheres r 0.4 m in a row along local X at x = −0.5, 0, 0.5, overall 1.8 m long (**proposed**: an elongated silhouette from existing sphere shapes, about 2.5 Balloons long).
  - Gondola: a box 0.5 × 0.2 × 0.25 m beneath, at (0, −0.5, 0), carrying the propeller at its rear (−0.25, −0.5, 0).
- **Mass and material.** 0.8 kg total; envelope restitution 0.2 and drag 0.4 1/s (the legacy Balloon's bounce and drag, `parts/catalog/balloon.tres@a6c914e:L14-L14`); friction 0.3 (the shared ball default). Gas state: a sealed lifting-gas volume whose buoyant acceleration at standard pressure is 9.81 m/s², so the blimp is neutrally buoyant and hovers (**proposed**: the legacy Balloon uses 11.5 to rise, `parts/catalog/balloon.tres@a6c914e:L14-L14`; a steerable craft should hold altitude so steering is the lesson).
- **Constraints.** None; a free body. Yaw follows the authored placement; the drive acts along the body's local +X.
- **Typed ports.**

  | Socket | Domain | Direction | Local position (m) | Status |
  | --- | --- | --- | --- | --- |
  | `PowerIn` | Electrical | Input | gondola (0.25, −0.5, 0) | **proposed**: "bounded supplied directional drive" |
  | `ActivationIn` | Activation | Input | gondola top (0, −0.4, 0) | **proposed**: a Turn command reverses the thrust direction |

  A wire to a free-flying body is a declared tether: the electrical connection carries supply but no force (**proposed**: see Open questions).
- **Sensors and activation.** `ActivationIn` Turn toggles the thrust between `Forward` and `Reverse`. It is coalesced to the tick boundary.
- **Work and energy stores.** None on board. The drive draws `thrust` × speed from supply; without supply, thrust is zero.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `thrust` | f32 | 0.5–6 | 3 | N | **proposed**: a third of the 9 N Fan default ([CAT-028](CAT-028-fan.md)), so a powered blimp can hold against a weak current but not a full jet |
  | `initial_direction` | enum `ThrustDirection` | Forward, Reverse | Forward | — | **proposed** |

- **Cosmetic curves and UI bindings.** The propeller spins in proportion to the committed supplied thrust and stops when unpowered. A rudder fin swings to the active direction. The envelope sways from committed angular velocity only.
- **Art (DESIGN.md).** A balloon-pink `#ed6378` envelope (the Balloon colour, `DESIGN.md@a6c914e:L168-L168`) with cream `#fff8e9` bands, a navy `#293954` gondola and a gold `#f7cb52` propeller. Soft, rounded forms (`DESIGN.md@a6c914e:L48-L48`).
- **Catalogue and inventory entry.** Id `blimp`, title "Steerable blimp", category Motion, colour `#ed6378` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (AerodynamicDrag, Buoyancy, EnvironmentState, FiniteLedger, FiniteWorkActuation, GasState, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.** Dynamic spheres with declared linear drag (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`). `BallMaterial` carries a buoyancy field that the compiler does not consume yet (`engine/gpu/WorkshopConstruction.cs@a6c914e:L38-L62`). Activation sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).
- **Missing.**
  - Buoyancy and GasState: Story 12.1 (CAT-003); S416 buoyancy, next S420; S470 gas-state, next S471 ([decisions](../invest/decisions.md#s470)).
  - Drag above 0.125 1/s: body declarations admit linear drag only up to 0.125 1/s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L69-L69`), so the 0.4 1/s envelope drag needs admission widening, Story 12.1 (CAT-003 carries the same legacy 0.4).
  - Airflow current from the Fan: Story 12.2 (CAT-028); S470 open-versus-sealed, next S697.
  - A body-fixed bounded thrust actuator (FiniteWorkActuation): owner S660.
  - Dynamic compound bodies (envelope and gondola): single-collider mass compile (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L30`).
  - ElectricalPower: Story 8.1; S257 electrical-port, next S270.
- **Dependencies.** CAT-003 Balloon (buoyancy law), CAT-028 Fan (current), CAT-005 Battery.

## 4. Sources and legacy

- **Requirement row.** [element-090](../requirements.md#element-090): a buoyant body combines gas state with a bounded supplied directional drive; unpowered drive cannot steer against a current.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** S416/S470 ([decisions](../invest/decisions.md)); S257 typed power. Research reference: TIM2 inventory ([todo-216](../requirements.md#todo-216)).
- **Legacy search.** `git grep -i "blimp"` and "airship" at a6c914e returned nothing. Balloon and airflow facts apply; [CAT-003](CAT-003-balloon.md) and [CAT-028](CAT-028-fan.md) hold the full harvests.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Balloon: mass 0.5, bounce 0.2, radius 0.36, buoyancy 11.5, drag 0.4; "Buoyancy lifts it; fans can guide it. Pressure changes its ascent." | `parts/catalog/balloon.tres@a6c914e:L11-L14` | Carry forward bounce and drag; buoyancy re-proposed for hover |
| L2 | Legacy law: buoyant force = buoyancy × pressure × mass (upward); drag load = drag × pressure | `reference/cpu/MachineWorld.cs@a6c914e:L881-L882` | Carry forward the law as declaration input. Do not carry forward the per-substep CPU loop. |
| L3 | Fan default force 9, reach 5, width 0.85 | `parts/catalog/fan.tres@a6c914e:L14-L14` | Carry forward as the current used in the control |

**Files harvested:** `parts/catalog/balloon.tres`, `parts/catalog/fan.tres`, `reference/cpu/MachineWorld.cs` (buoyancy and drag lines only).

## 5. Acceptance outline

- **Chrome recipe.** Place the Steerable blimp mid-air above the bench, a Battery wired to `PowerIn`, a Switch under a dropped ball wired to `ActivationIn`, and a Fan 3 m ahead blowing back at the blimp. Run.
- **Positive.** The powered blimp hovers and drives forward along its heading; the Switch hit reverses its direction; it never gains altitude from the drive.
- **Negative or control.** Unpowered, the blimp is pushed back by the Fan's current and cannot advance. With the Fan off and no power, it hovers in place. A Wall ahead stops it by contact.
- **Boundaries.** Thrust never exceeds `thrust`; drive work never exceeds supply; neutral hover drifts no more than the envelope tolerance over 10 s.
- **Run/Reset.** Reset restores the pose, direction and gas state exactly. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-216](../requirements.md#todo-216) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-06 electrical power transfer](../requirements.md#interaction-06), [IX-07 signal propagation](../requirements.md#interaction-07), [IX-10 buoyancy](../requirements.md#interaction-10), [IX-11 aerodynamic drag](../requirements.md#interaction-11) and [IX-40 gas state evolution](../requirements.md#interaction-40). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" ("drifting blimp") reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" ("gravity pad/blimp") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Fan current (CAT-028), Battery supply (CAT-005), Switch turn command (CAT-063), Balloon buoyancy law (CAT-003).

## 6. Open questions

1. How a free-flying body receives electrical supply: a wired tether (proposed) or an on-board battery store. Unspecified — owner decision (S660).
2. Neutral hover (proposed) versus slow rise like the Balloon.
3. Steering model: reversible thrust (proposed) or an authored rudder angle.
