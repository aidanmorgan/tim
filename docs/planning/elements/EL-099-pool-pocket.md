# EL-099 · Pool pocket — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-099 · Pool pocket · Specialist |
| Requirement anchor | [element-099](../requirements.md#element-099); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-099](../invest/named-elements.md#element-099); source owner **S669** |
| CAT spec refined or extended | Extends [CAT-004 Receiver](CAT-004-basket.md) (capture by residence) and [CAT-002 Ball detector](CAT-002-ball_detector.md) (aperture passage) |
| Related identities | EL-098 Pool ball (cargo), EL-097 Pool cue, EL-192 Receiving basket, EL-120 Quantity goal |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The element-map composition rule applies: general pocket geometry and contact, plus named-body continuous containment and goal observation; geometry alone does not define capture semantics ([element map](../general-engine-element-map.md)).

- **Bodies and shapes.** One static compound body:
  - Table frame: four box slabs, each 0.12 m thick, framing a square opening 0.5 × 0.5 m whose top is flush with a table bed at local y = 0.
  - Catch net: an open five-box container below the opening, 0.6 × 0.5 × 0.6 m inside, using the Receiver construction (`engine/gpu/ReceiverGeometry.cs@a6c914e:L8-L19`).

  These sizes are **proposed**: a 0.5 m opening admits a 0.4 m Pool ball with 0.05 m clearance per side and rejects a 0.68 m Basketball; square, because only box colliders exist.
- **Mass and material.** Static; frame restitution 0.6, friction 0.3 (**proposed**: a lively rail edge); net restitution 0.1 (**proposed**: deadens the ball so it stays caught).
- **Constraints.** None.
- **Typed ports.** Optional `ActivationOut` (Activation, Output) on the frame side (**proposed**: one occurrence per pocketed ball, so a pocket can drive a machine).
- **Sensors and activation.** A capture is two generic observations on a named body:
  1. **Passage.** The body's centre crosses the opening plane downward inside the aperture. A crossing outside the aperture is not a passage.
  2. **Residence.** The body then resides in the net region for the dwell, below the speed limit.

  Only both together commit one `Pocketed` occurrence, latched per body until Reset. A ball resting on the rail beside the opening never passes and is never pocketed.
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `dwell` | f32 | 1/1024–30 | 0.2 | s | Range from `ResidenceSensorDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`); default **proposed**: shorter than the Receiver's 0.35 s, because the net deadens the ball |
  | `speed_limit` | f32 | 1/1024–64 | 1.5 | m/s | The Receiver default (`engine/gpu/WorkshopConstruction.cs@a6c914e:L82-L82`) |

- **Cosmetic curves and UI bindings.** The net sways when a ball lands (contact-driven animation). A gold `#f7cb52` ring flashes on `Pocketed` using the capture feedback source (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L58-L58`).
- **Art (DESIGN.md).** A warm wood `#c28f52` frame with a cream `#fff8e9` lip and a navy `#293954` net; the capture ring follows the Receiver's pale green `#bdf4bd` convention (`DESIGN.md@a6c914e:L278-L278`).
- **Catalogue and inventory entry.** Id `pool_pocket`, title "Pool pocket", category Goals, colour `#4aab94` (the Basket colour, `DESIGN.md@a6c914e:L170-L170`) (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction).

- **Exists now.**
  - Static five-box containers, and named-body residence sensors with dwell and speed limit feeding a capture latch (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L85`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`; `engine/gpu/CaptureLatch.cs@a6c914e:L5-L20`).
  - Goal evaluation over capture (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L5-L8`).
- **Missing.**
  - An aperture passage sensor: Story 6.8 (CAT-002 Ball detector).
  - Composition of passage then residence into one occurrence: owner S669.
  - Multi-target capture (any of several Pool balls): the current residence sensor watches one named target (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`); S635 goal-occurrences, next LAW-GOALS-I.
- **Dependencies.** CAT-002 Ball detector (passage law), CAT-004 Receiver (residence law), EL-098 Pool ball.

## 4. Sources and legacy

- **Requirement row.** [element-099](../requirements.md#element-099): a real opening captures cargo through actual passage; a ball beside the opening is not pocketed.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "pocket" and "billiard" found no pocket source. Passage-sensor facts apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Passage is a forward body-centre crossing of a frame-local aperture; arming needs the whole collider upstream; events are `Passed` or `OutsideAperture` | `engine/physics/PhysicsPassageSensor.cs@a6c914e:L5-L26` | Carry forward the semantics (closed sets as enums). Do not carry forward the CPU double sweep. |
| L2 | Standard pipe passes a 0.34 m ball at 40 m/s but an oversized 0.8 m ball hits the mouth | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L56-L80` | Carry forward the "oversized cargo is blocked by real geometry" control |

**Files harvested:** `engine/physics/PhysicsPassageSensor.cs`, `CuriousContraptions.tests/PipeTests.cs` (oversize test only).

## 5. Acceptance outline

- **Chrome recipe.** Build an EL-098 table with a Pool pocket at one corner, flush with the bed. Strike one Pool ball toward the opening with a cue or bumper. Rest a second Pool ball on the rail 0.05 m beside the opening. Run.
- **Positive.** The struck ball drops through the opening into the net and one `Pocketed` occurrence is committed.
- **Negative or control.** The rail ball is never pocketed. A ball that rolls over the opening too fast and bounces out of the net is not pocketed. A Basketball cannot fit through the opening.
- **Boundaries.** A crossing at the aperture edge with clearance counts; one occurrence per ball per Run.
- **Run/Reset.** Reset clears latches and restores the balls. **Save/Load.** Placement, parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-07 signal propagation](../requirements.md#interaction-07) (`Pocketed` output). Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim, and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) names no pool-table row; todo-227 retains its original campaign reservations, and the ledger task must add a separate pocket row before campaign use. Element integrations: Ball detector passage law (CAT-002), Receiver residence law (CAT-004), EL-098 Pool ball, EL-120 Quantity goal.

## 6. Open questions

1. Square opening (proposed, box colliders) versus a round opening once annular or hollow shapes exist. Unspecified — owner decision (S669).
2. Which bodies count: Pool balls only, or any ball that fits.
