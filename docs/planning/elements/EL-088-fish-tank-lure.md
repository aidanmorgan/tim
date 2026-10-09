# EL-088 · Fish tank lure — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-088 · Fish tank lure · Character |
| Requirement anchor | [element-088](../requirements.md#element-088); scope index [todo-221](../requirements.md#todo-221) |
| Named entry | [element-088](../invest/named-elements.md#element-088); source owner **S658** |
| CAT spec refined or extended | None |
| Related identities | EL-084 Fragile fish bowl (the same contents and fracture law; [EL-084 spec](EL-084-fragile-fish-bowl.md)), EL-082 Lured and EL-087 Predator (perceivers) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The element-map composition rule applies: the lure participates in shared perception; tank geometry, finite contents and visibility govern detection; there is no unconditional target notification ([element map](../general-engine-element-map.md)).

- **Bodies and shapes.** One static, rectangular, open-top glass tank (static so it is a fixed lure unless broken): a floor and four transparent wall boxes forming 1.0 × 0.7 × 0.6 m outside, walls 0.04 m thick (**proposed**: wider than the EL-084 bowl, so a fish is visible from the side at bench scale). Fish: one dynamic sphere r 0.08 m, 0.05 kg (**proposed**: the EL-084 fish).
- **Mass and material.** Tank static while intact; glass restitution 0.3, friction 0.4 (**proposed**, as EL-084). Liquid contents 3.0 kg (**proposed**: a larger toy volume than the bowl; S416 owns the mapping).
- **Constraints.** None.
- **Typed ports.** None. The stimulus is perceived, not wired.
- **Sensors and activation (stimulus).** The fish body declares the one `StimulusKind.Prey` visual source, at its committed position (**proposed**: the fish is the lure; the tank glass declares no stimulus and is never Prey). A perceiver detects it only through the shared visibility rule: the transparent tank walls pass sight, opaque bodies (a Wall, a cloth cover) block it, and range and cone are the perceiver's own. **Fracture:** a contact with normal approach speed ≥ `break_speed` commits one fracture transaction, as in EL-084, releasing the liquid and fish as conserved contents; intact walls retain them. The transaction is committed after the step's contact response (L3–L6 below).
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `break_speed` | f32 | 1–12 | 4.0 | m/s | **proposed**: sturdier than the bowl (3.0); a Bowling ball rolled at 4 m/s breaks it, a walking character does not |
  | `stimulus_enabled` | bool | true/false | true | — | **proposed**: gives the "no stimulus" control without removing the tank |

- **Cosmetic curves and UI bindings.** The fish idles cosmetically inside the water volume; its rendered position follows the committed fish body and does not move it. A ripple cue shows while a perceiver is in Pursue of it (read-only from committed controller state). Crack lines show on the committed fracture.
- **Art (DESIGN.md).** Transparent cyan glass `#66b8c9` at low alpha with cream `#fff8e9` corner frames and a navy `#293954` base, after the clear pipe language (`DESIGN.md@a6c914e:L181-L181`, `DESIGN.md@a6c914e:L299-L299`); a gold `#f7cb52` original fish. Original silhouette (`DESIGN.md@a6c914e:L53-L53`).
- **Catalogue and inventory entry.** Id `fish_tank`, title "Fish tank", category Characters, colour `#66b8c9` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.** A static five-box open container (the Receiver, `engine/gpu/ReceiverGeometry.cs@a6c914e:L8-L19`; `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L85`). Static-owner contact approach speed (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1177`).
- **Missing.**
  - A stimulus declaration in the shared perception law (ProgrammableController, GeometryQuery): S635 controller, next LAW-CONTROLLER-I; S484 optical-commit, next S486 for transparent and opaque occlusion.
  - StructuralFracture and TopologyTransaction for the walls: S543 fracture, next S563; phase-topology, next S565.
  - Liquid contents: S416 advection, next S418.
- **Dependencies.** A perceiver: EL-082 or EL-087. EL-084 for the shared fracture and contents law. CAT-066 Wall as the occluder.

## 4. Sources and legacy

- **Requirement row.** [element-088](../requirements.md#element-088): a fragile enclosure presents a declared sensory stimulus while containing its payload; occlusion affects perception and intact walls retain contents.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** S543 and S416 ([decisions](../invest/decisions.md)); [S652](../invest/scope-corrections.md#s652) (a missing lure leaves the character idle). Research reference: TIM2 manual ([todo-221](../requirements.md#todo-221)).
- **Legacy search.** `git grep -i` at a6c914e for "fish", "tank", "aquarium" and "lure" over every Epic 7 deletion path found no source. The occlusion facts and the generic impact-effect hook (the nearest source for the fracture criterion) apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Clear pipe shell passes light; opaque collars block it | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | Carry forward as "transparent tank walls pass sight" |
| L2 | Visibility is a zero-thickness segment that is not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | Carry forward the rule. Do not carry forward the CPU sweep. |
| L3 | An impact effect receives a value context: the impact, both bodies' before and after snapshots, and the contact approach speed | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L9-L19` | Carry forward approach speed as the fracture input. Do not carry forward the f64 records. |
| L4 | Effect impulses apply after the ordinary contact response, never change pose and never bypass the next solve; an effect cannot push through the contact that triggered it | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L21-L33`; `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L208-L216` | Carry forward: wall fragments inherit post-response velocities |
| L5 | Collider and joint changes requested by an effect are committed together after the coupled contact response, never during its solve | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L35-L50` | Carry forward: the wall topology transaction commits after the solve |
| L6 | An effect's simulation state is captured and restored with the step; presentation waits until the step succeeds | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L52-L63` | Carry forward the transactional rule. Do not carry forward the abstract callback class: an element-owned executable hook is not declaration data ([engine contracts](../../engine-contracts.md)). |
| L7 | Clear motion without contact activates nothing | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L259-L264` | Carry forward as the missed-contact control |
| L8 | Simultaneous contacts notify every subscribed effect after one coupled response, each with the same approach speed | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L267-L280` | Carry forward: a fragile striker and the tank both see the same approach speed |
| L9 | A body already touching and closing at the start of a step still triggers, without a zero-time sweep | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L282-L290` | Carry forward |
| L10 | Persistent contact does not retrigger; release and recontact does | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L303-L315` | Carry forward: a character pressing on the glass is evaluated once per new contact, not every tick |

**Files harvested:** `CuriousContraptions.tests/PipeTests.cs` (occlusion test only), `engine/physics/SegmentOcclusionSweep.cs`, `engine/physics/PhysicsImpactEffects.cs`, `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Place the Fish tank at the bench right and a Lured character 3 m left, facing it. Keep a Wall ready to slide between them. Separately, place a Ramp that rolls a Bowling ball into the tank. Run.
- **Positive.** The character perceives the fish through the glass and walks to the tank, stopping at its wall. The tank holds its water and fish.
- **Negative or control.** A Wall between them: no perception, so it idles. `stimulus_enabled` false: no pursuit. A character pressing against the tank does not break it.
- **Boundaries.** The Bowling ball at 4.1 m/s fractures the tank and releases the conserved liquid and fish; at 3.9 m/s the tank holds. Liquid out equals liquid held.
- **Run/Reset.** Reset restores the intact tank, water and fish. **Save/Load.** Placement and parameters round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-221](../requirements.md#todo-221) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-08 fluid advection](../requirements.md#interaction-08), [IX-37 structural fracture](../requirements.md#interaction-37) and [IX-41 phase topology update](../requirements.md#interaction-41). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" (fragile fish tank target) reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: EL-082 Lured character and EL-087 Predator (perceivers), EL-084 Fragile fish bowl (shared fracture and contents law), Wall occluder (CAT-066).

## 6. Open questions

1. Static tank (proposed) or movable. Unspecified — owner decision (S658).
2. Stimulus kinds: visual only (proposed), or scent that glass blocks.
3. Whether a cover accessory is needed for the occlusion lesson, or a Wall suffices.
