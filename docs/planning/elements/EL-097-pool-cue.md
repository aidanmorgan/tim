# EL-097 · Pool cue — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-097 · Pool cue · Specialist |
| Requirement anchor | [element-097](../requirements.md#element-097); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-097](../invest/named-elements.md#element-097); source owner **S667** |
| CAT spec refined or extended | Related to [CAT-039 Linear pusher](CAT-039-linear_pusher.md) (a slider actuator with bounded force) and [CAT-015 Bumper](CAT-015-bumper.md) (finite contact work) |
| Related identities | EL-098 Pool ball (target), EL-099 Pool pocket, EL-091 Cannonball |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The cue is a contact actuator: work reaches the ball only through the tip's actual contact. There is no "hit this ball" command and no velocity written onto a target.

- **Bodies and shapes.**
  - Static rest: box 0.6 × 0.3 × 0.3 m that holds the slider.
  - Cue stick: a dynamic box 1.6 × 0.06 × 0.06 m on a prismatic joint along its own axis, with the tip at local +X.

  These sizes are **proposed**: a long thin stick reads as a cue, and 0.06 m is well above the 1/1024 m half-extent floor (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L105-L109`).
- **Mass and material.** Stick 0.5 kg; tip restitution 0.7, friction 0.5 (**proposed**: a leather tip grips and returns most energy).
- **Constraints.** A prismatic joint with travel limits: rest at 0 and full stroke at +0.4 m. A drive row moves the stick forward during a stroke and back to rest afterwards (**proposed**: a short, visible stroke).
- **Typed ports.** `PowerIn` (Electrical, Input) and `ActivationIn` (Activation, Input) on the rest (**proposed**: separate supply and trigger, as the cannon).
- **Sensors and activation.** Each trigger, if the store holds at least `stroke_energy` and the stick is at rest, starts one stroke. Otherwise it is refused and not queued. The stroke ends at the travel limit or when the stick stops against a load.
- **Work and energy stores.** One store, `capacity`, starts empty and charges from `PowerIn` at `charge_power` (`engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L46`). A stroke drives the stick with bounded force; positive work is debited from the store and capped at `stroke_energy` (`engine/physics/PoweredImpulse.cs@a6c914e:L7-L26`). Work reaches a ball only through tip contact; a miss spends the stroke's work on the stick alone, which the travel stop then dissipates.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `stroke_energy` | f32 | 0.5–10 | 3 | J | **proposed**: a 0.5 kg pool ball can leave at up to about 3.5 m/s, a brisk break at bench scale |
  | `capacity` | f32 | 1–20 | 6 | J | **proposed**: two strokes |
  | `charge_power` | f32 | 1–20 | 6 | W | **proposed**: one stroke recharges in 0.5 s |
  | `max_force` | f32 | 5–60 | 40 | N | Legacy linear-pusher force default 40 (`parts/catalog/linear_pusher.tres@a6c914e:L12-L12`); range **proposed** |

- **Cosmetic curves and UI bindings.** The stick follows its committed pose. A charge bar shows the store fraction. A chalk puff appears at the committed tip-contact occurrence.
- **Art (DESIGN.md).** A warm wood `#c28f52` stick with a cream `#fff8e9` ferrule, a navy `#293954` tip and butt wrap, and a navy rest with a gold `#f7cb52` charge bar (`DESIGN.md@a6c914e:L169-L169`).
- **Catalogue and inventory entry.** Id `pool_cue`, title "Pool cue", category Power, colour `#c28f52` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction).

- **Exists now.** Dynamic boxes and spheres in contact (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L774`). The finite contact-work pattern (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L36`). Typed sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).
- **Missing.**
  - Prismatic joint with limits and a bounded drive row (JointConstraint): Epic 10 and Story 11.3 (CAT-039 Linear pusher).
  - A supply-charged store released per stroke: Story 11.4 (CAT-016) builds the store.
  - ElectricalPower: Story 8.1.
  - Source D owner: S667.
- **Dependencies.** CAT-039 Linear pusher (slider law), CAT-016 (store), CAT-005 Battery, EL-098 Pool ball.

## 4. Sources and legacy

- **Requirement row.** [element-097](../requirements.md#element-097): a contact actuator delivers bounded work to an actual ball; a miss cannot move its target.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Research reference.** The historical candidate register ([todo-227](../requirements.md#todo-227)); edition claims need a [todo-228](../requirements.md#todo-228) fixture.
- **Legacy search.** `git grep -i` at a6c914e for "cue", "pool" and "billiard" found nothing. Linear pusher and actuator facts apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Linear pusher: stroke 2.4, speed 1.5, acceleration 12, force 40; "pushes with limited force and stops at obstacles" | `parts/catalog/linear_pusher.tres@a6c914e:L9-L12` | Carry forward the bounded-force and stop-at-obstacle rule; the stroke is shortened for a cue |
| L2 | Actuator positive work is bounded by available work; dissipated energy is never available for reverse acceleration | `engine/physics/PoweredImpulse.cs@a6c914e:L7-L26` | Carry forward the rule. Do not carry forward the CPU double code. |
| L3 | Charging accepts `min(capacity − energy, power × dt)` only while supplied | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L46` | Carry forward |

**Files harvested:** `parts/catalog/linear_pusher.tres`, `engine/physics/PoweredImpulse.cs`, `engine/physics/PhysicsEnergyStore.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Place the Pool cue aimed at an EL-098 Pool ball 0.2 m ahead of its tip; wire a Battery to `PowerIn` and a Switch to `ActivationIn`. Place a second cue aimed 0.5 m to the side of a second ball. Run.
- **Positive.** The aimed cue strokes, the tip contacts the ball, and the ball rolls away; its kinetic energy is ≤ 3 J.
- **Negative or control.** The misaimed cue strokes and its ball does not move. Without supply, no stroke. A trigger during a stroke is refused.
- **Boundaries.** Ball kinetic energy ≤ `stroke_energy`; total energy delivered over the Run ≤ energy supplied; the stick never passes its travel limit.
- **Run/Reset.** Reset restores the stick at rest, the empty store and the ball. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-03 joint constraint](../requirements.md#interaction-03) and [IX-06 electrical power transfer](../requirements.md#interaction-06). Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim, and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) names no pool-table row; todo-227 retains its original campaign reservations, and the ledger task must add separate rows before campaign use. Element integrations: EL-098 Pool ball (target), EL-099 Pool pocket, Battery supply (CAT-005), Switch trigger (CAT-063).

## 6. Open questions

1. Energy source: an electrical store (proposed), a wound spring, or a player-authored pull-back. Unspecified — owner decision (S667).
2. Whether the cue aims along its placement only (proposed) or exposes an aim control.
