# EL-101 · Thumb tack — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-101 · Thumb tack · Specialist |
| Requirement anchor | [element-101](../requirements.md#element-101); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-101](../invest/named-elements.md#element-101); source owner **S671** |
| CAT spec refined or extended | None. Its main target is [CAT-003 Balloon](CAT-003-balloon.md). |
| Related identities | EL-208 Balloon, EL-090 Steerable blimp (puncturable envelope), EL-066 Scissors (cutting failure), GAP-09 Fragmentation station |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

Puncture is a generic material-failure rule triggered by contact with a declared **sharp feature**; the tack is never named by the target. Puncture is a topology transaction on the target: its gas is released and its buoyancy and shape change. The target is not destroyed and respawned.

- **Bodies and shapes.** One static body whose point lies along local +Y; the player aims it with the rotation gizmo (**proposed** sizes: visible at bench scale; the tip uses the 1/16 m minimum sphere radius, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L100-L104`):
  - Head: a box 0.3 × 0.04 × 0.3 m.
  - Pin: a box 0.04 × 0.2 × 0.04 m.
  - Tip: a sphere r 0.0625 m, flagged as the sharp feature.
- **Mass and material.** Static, as a pinned tack; restitution 0.3, friction 0.4 (**proposed**: metal).
- **Constraints.** None.
- **Typed ports.** None.
- **Sensors and activation.** The puncture criterion: a contact between the sharp tip feature and a body whose material declares `PunctureClass.Puncturable` commits one puncture transaction on that body when its normal force reaches `puncture_force`. A body declaring `PunctureClass.Resistant` (balls, boxes, Walls) is unaffected. A contact on the head or pin, not the tip, never punctures. The criterion reads the post-response contact state and the transaction commits after the solve (L3–L6 below).
- **Target-side effect (declared by the target).** For a gas-filled target (the Balloon), the puncture releases its gas inventory to the atmosphere. Buoyancy goes to zero and the collider shrinks to its declared deflated shape; mass and momentum are kept. The release cannot repeat.
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `puncture_force` | f32 | 0.05–5 | 0.3 | N | **proposed**: a Balloon pushed by a Fan exceeds it, while a light graze in passing does not |

- **Cosmetic curves and UI bindings.** None on the tack. The target shows a pop puff and a deflated mesh from the committed puncture occurrence.
- **Art (DESIGN.md).** A gold `#f7cb52` round head with a pale grey pin, the metal-detail convention (`DESIGN.md@a6c914e:L189-L189`). Small but readable, with an original pictogram.
- **Catalogue and inventory entry.** Id `thumb_tack`, title "Thumb tack", category Motion, colour `#f7cb52` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-02.json` (ContactImpulse, EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, StructuralFracture, TopologyTransaction).

- **Exists now.** Static compound boxes and spheres with contact normal impulses (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774-L774`). Contact triggers keyed to a static owner and a named or any dynamic body (`engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L8-L20`; `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L9-L17`).
- **Missing.**
  - A per-feature sharp flag and a `PunctureClass` material field: owner S671.
  - The puncture transaction (StructuralFracture, TopologyTransaction): S543 fracture, next S563; S543 phase-topology, next S565 ([decisions](../invest/decisions.md#s543)).
  - The gas inventory release (GasState): S470 gas-state, next S471. The target's buoyancy: Story 12.1 (CAT-003).
- **Dependencies.** CAT-003 Balloon (target), CAT-028 Fan (to push the balloon).

## 4. Sources and legacy

- **Requirement row.** [element-101](../requirements.md#element-101): sharp-contact geometry punctures material through a generic failure threshold; missed contact or puncture-resistant material remains intact.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "tack", "puncture", "pop", "burst" and "sharp" found no puncture source; "burst" hits are bellows air bursts and clock pulse bursts. The generic impact-effect hook is the nearest source for the puncture criterion and its timing.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Balloon: mass 0.5, bounce 0.2, radius 0.36, buoyancy 11.5, drag 0.4; fans can guide it | `parts/catalog/balloon.tres@a6c914e:L11-L14` | Carry forward as the target's declaration |
| L2 | Contact triggers respond to a named body or to all dynamic bodies (`BodyTargetKind`) | `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L9-L17` (current) | Carry forward the target-filter pattern; puncture filters by material class, not identity |
| L3 | An impact effect receives a value context: the impact, both bodies' before and after snapshots, and the contact approach speed | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L9-L19` | Carry forward the post-response contact context as the puncture input. Do not carry forward the f64 records. |
| L4 | Effect impulses apply after the ordinary contact response, never change pose and never bypass the next solve; an effect cannot push through the contact that triggered it | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L21-L33`; `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L208-L216` | Carry forward: a punctured balloon never passes through the tip |
| L5 | Collider and joint changes requested by an effect are committed together after the coupled contact response, never during its solve | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L35-L50` | Carry forward: the deflated collider and gas release commit after the solve |
| L6 | An effect's simulation state is captured and restored with the step; presentation waits until the step succeeds | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L52-L63` | Carry forward the transactional rule. Do not carry forward the abstract callback class: an element-owned executable hook is not declaration data ([engine contracts](../../engine-contracts.md)). |
| L7 | Clear motion without contact activates nothing | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L259-L264` | Carry forward as the missed-contact control |
| L8 | Simultaneous contacts notify every subscribed effect after one coupled response, each with the same approach speed | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L267-L280` | Carry forward: two tacks touched in one step both evaluate the same contact state |
| L9 | A body already touching and closing at the start of a step still triggers, without a zero-time sweep | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L282-L290` | Carry forward: a balloon placed touching the tip and pressed against it can still puncture |
| L10 | Persistent contact does not retrigger; release and recontact does | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L303-L315` | Carry forward only as the one-puncture latch: a target punctures at most once per Run. The force criterion is not edge-triggered: it is evaluated on every tick of tip contact until puncture, so a balloon resting on the tip punctures as soon as its pressing force rises to `puncture_force` |

**Files harvested:** `parts/catalog/balloon.tres`, `engine/physics/PhysicsImpactEffects.cs`, `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Rotate a Thumb tack to point down and fix it under a ceiling Wall, with a Balloon below that rises into it. On a second lane, push a Balloon sideways into a tack's head with a Fan. On a third, drop a Basketball onto an upward-pointing tip. Run.
- **Positive.** The rising Balloon meets the tip, pops, loses its lift and falls as a deflated body.
- **Negative or control.** The Balloon touching only the head stays intact. The Basketball on the tip is unaffected (resistant). A Balloon that misses the tack stays intact.
- **Boundaries.** Tip force 0.29 N holds and 0.31 N punctures; one puncture per target per Run.
- **Run/Reset.** Reset restores the inflated Balloon and its gas. **Save/Load.** Placement and parameter round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-37 structural fracture](../requirements.md#interaction-37), [IX-40 gas state evolution](../requirements.md#interaction-40) and [IX-41 phase topology update](../requirements.md#interaction-41). Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim, and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Battery, wire, switch, motor" ("balloon tether/pop") reserves levels 11–20 (reuse 31–50, 61–80, 136–150) for the balloon pop this tack causes; the ledger task must give the tack its own row. Element integrations: Balloon target (CAT-003), Fan push (CAT-028), Basketball resistant control (CAT-001).

## 6. Open questions

1. Force threshold (proposed) versus approach speed or pressure. Unspecified — owner decision (S671).
2. Whether a dynamic (loose) tack is wanted, and whether it can stick into soft material.
3. Which current and future materials are Puncturable.
