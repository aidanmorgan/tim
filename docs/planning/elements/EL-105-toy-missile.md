# EL-105 · Toy missile — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

**Safety boundary.** An abstract guided toy: a declared impulse budget, a typed perception query and bounded steering. No real guidance hardware, propellant or construction is modelled or described.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-105 · Toy missile · Specialist |
| Requirement anchor | [element-105](../requirements.md#element-105); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-105](../invest/named-elements.md#element-105); source owner **S378** |
| CAT spec refined or extended | None. It combines the [EL-092](EL-092-toy-rocket.md) thrust law with the perception query of [EL-082](EL-082-lured-character.md); its beacon is a lit [CAT-035 Lamp](CAT-035-lamp.md). |
| Related identities | EL-092 Toy rocket, EL-104 Toy firework, EL-082 and EL-087 (the same perception law), CAT-063 Switch |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

Guidance is a shared ProgrammableController policy: perceive, then request a bounded steering intent. The solver applies the steering as a finite force drawn from the same store as the thrust. There is no homing path, no target identity lookup and no velocity override.

- **Bodies and shapes.** One dynamic body: a box 0.8 × 0.16 × 0.16 m, half extents (0.4, 0.08, 0.08), nose at local +X (**proposed**: a dart-like toy about a Domino's length).
- **Mass and material.** 0.3 kg, restitution 0.2, friction 0.4 (**proposed**: a light foam dart).
- **Constraints.** None. It launches from wherever it is placed, for example resting on a Ramp used as a rail.
- **Typed ports.** `ActivationIn` (Activation, Input) at the tail (**proposed**: the launch command).
- **Sensors and activation (seeker).** A forward cone of half-angle 30° (**proposed**: narrower than the characters' 60°, so the dart reacts only to beacons roughly ahead) within `seeker_range`, with sight-line occlusion (opaque blocks, transparent passes). It perceives `StimulusKind.Beacon` sources; a CAT-035 Signal lamp declares Beacon only while lit (**proposed**: reuses a delivered, visibly on/off part).
- **Policy.** While burning: if a beacon is perceived, request a lateral steering force toward it, bounded by `max_turn_accel` × mass. If no beacon is perceived (occluded, unlit, outside the cone or out of range), request none and fly straight. After burnout, nothing: no thrust and no steering.
- **Work and energy stores.** One impulse store, `total_impulse`, shared by axial thrust and steering: each tick debits |F_axial| × dt + |F_lateral| × dt. When it is empty, the missile is spent. Steering can therefore never be unlimited.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `thrust` | f32 | 1–8 | 4 | N | **proposed**: 13 m/s² at 0.3 kg, a brisk launch |
  | `total_impulse` | f32 | 0.5–6 | 3 | N·s | **proposed**: about 0.6–0.75 s of burn, including steering |
  | `max_turn_accel` | f32 | 0–8 | 4 | m/s² | **proposed**: a visible curve of about 1 m radius at 2 m/s, under the 64 m/s² region cap ([capability inventory](../../gpu-f32-physics.md#capability-inventory)) |
  | `seeker_range` | f32 | 1–12 | 8 | m | **proposed**: about half the bench width |

- **Cosmetic curves and UI bindings.** The plume scales with committed thrust. A seeker lamp on the nose is gold `#f7cb52` while a beacon is perceived and slate `#556573` otherwise. The fins deflect toward the committed steering direction.
- **Art (DESIGN.md).** A cream `#fff8e9` foam body, a coral `#de7058` nose cone, cyan `#66b8c9` fins and a navy `#293954` band (`DESIGN.md@a6c914e:L158-L158`, `DESIGN.md@a6c914e:L172-L172`). Clearly a toy dart.
- **Catalogue and inventory entry.** Id `toy_missile`, title "Seeker dart", category Motion, colour `#de7058` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-02.json` (ChemicalReaction, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, SensibleHeat, StateTransaction, StructuralFracture, TopologyTransaction). ProgrammableController is needed for the seeker policy, although the binding does not list it (see Open questions).

- **Exists now.** Dynamic boxes (`engine/gpu/WorkshopDomino.cs@a6c914e:L7-L35`). The Signal lamp's lit state as a committed activation phase (`engine/gpu/WorkshopActivationParts.cs@a6c914e:L24-L34`; `engine/gpu/ActivationNetwork.cs@a6c914e:L9-L9`).
- **Missing.**
  - A body-fixed thrust and steering actuator sharing one impulse store: owners S378, S662.
  - The perception query with occlusion: S635 controller, next LAW-CONTROLLER-I; S484 optical-commit, next S486.
  - Beacon stimulus declaration on the Lamp: owner S378.
  - ChemicalReaction as the abstract charge: S543 reaction, next S554.
- **Dependencies.** CAT-035 Lamp (beacon), CAT-063 Switch (launch), EL-092 (thrust law), EL-082 (perception law).

## 4. Sources and legacy

- **Requirement row.** [element-105](../requirements.md#element-105): a guided toy projectile follows only supported sensing and finite actuation; no supply or lost guidance cannot yield unlimited corrective thrust.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "missile", "seeker", "guid" and "homing" found nothing. Adjacent facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Visibility is a finite zero-thickness segment, not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | Carry forward the rule. Do not carry forward the CPU sweep. |
| L2 | A finite store debit never exceeds stored energy | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52` | Carry forward |

**Files harvested:** `engine/physics/SegmentOcclusionSweep.cs`, `engine/physics/PhysicsEnergyStore.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Place the Seeker dart on the bench facing +X; place a lit Signal lamp (driven by a Switch hit) 5 m ahead and 1 m to the side; wire a launch Switch to `ActivationIn`. Run.
- **Positive.** The dart launches and curves toward the lit Lamp while burning, then coasts and falls.
- **Negative or control.** An unlit Lamp: the dart flies straight. A Wall between dart and Lamp: no steering. A Lamp behind the dart (outside the cone): no steering. A second launch command: nothing. With `total_impulse` at its minimum, the dart curves only briefly before it is spent.
- **Boundaries.** Thrust plus steering impulse delivered ≤ `total_impulse`; lateral acceleration ≤ `max_turn_accel`; a beacon at 8.1 m is ignored.
- **Run/Reset.** Reset restores the pose, full store and unspent state. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-07 signal propagation](../requirements.md#interaction-07) (launch command), [IX-28 chemical reaction](../requirements.md#interaction-28) and [IX-29 reaction ignition](../requirements.md#interaction-29); seeker perception follows the S635 controller decision. Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim, and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) names no missile row; the nearest family is the row beginning "Original cat/mouse lure/escape roles" (rocket; 91–100, reuse 136–150) and chapter 10 "Oddly Satisfying" in the [campaign plan](../requirements.md#campaign-plan); the ledger task must add a separate row. Element integrations: Signal lamp beacon (CAT-035), Switch launch (CAT-063), Wall occluder (CAT-066), EL-092 thrust law, EL-082 perception law.

## 6. Open questions

1. The coverage binding omits ProgrammableController, though the row requires sensing; confirm the family list. Unspecified — owner decision (S378).
2. Beacon source: a lit Lamp (proposed), a light beam, or a dedicated beacon part.
3. Whether steering and thrust share one store (proposed) or have separate budgets.
