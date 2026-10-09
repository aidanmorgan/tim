# EL-084 · Fragile fish bowl — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-084 · Fragile fish bowl · Character |
| Requirement anchor | [element-084](../requirements.md#element-084); scope index [todo-200](../requirements.md#todo-200) |
| Named entry | [element-084](../invest/named-elements.md#element-084); source owner **S654** |
| CAT spec refined or extended | None |
| Related identities | EL-088 Fish tank lure (a fragile enclosure with a stimulus), GAP-09 Fragmentation station (the shared fracture law), EL-001..036 water family (contents) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The element-map composition rule applies: the fracture transaction releases the same conserved contents and updates collision and mass; there is no destroy-and-spawn duplication ([element map](../general-engine-element-map.md)). The fish is a contained payload whose swimming is cosmetic only.

- **Bodies and shapes.**
  - Bowl: one dynamic open-top body approximating a 0.35 m-radius glass bowl as a floor disc and a ring of 8 wall boxes, each 0.04 m thick and 0.45 m high (**proposed**: about Basketball size, so it sits on the bench beside other parts).
  - Fragments: the fracture declaration lists 4 fragment bodies that partition the bowl's mass and geometry exactly (**proposed**: bounded pieces, per the S563 rule).
  - Fish: one dynamic sphere r 0.08 m, 0.05 kg, inside the bowl (**proposed**: a payload that falls out visibly when released; above the 1/16 m minimum radius, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L100-L104`).
- **Mass and material.** Bowl shell 0.6 kg, glass restitution 0.3, friction 0.4 (**proposed**: light, hard and slippery). Liquid contents 2.0 kg of water held as a finite liquid inventory (**proposed**: game-scale toy volume; the S416 advection decision owns the volume–mass mapping). The body's total mass includes the contents while contained.
- **Constraints.** None between bowl and fish; the fish is held by geometry and liquid buoyancy.
- **Typed ports.** None.
- **Sensors and activation.** A fracture criterion on the bowl: any single contact whose normal approach speed is ≥ `break_speed` commits one fracture transaction. The bowl is replaced atomically by its fragments with the same total mass and momentum. The liquid inventory is released as conserved free liquid and the fish as a free body. Below the threshold, nothing changes. The transaction is committed after the step's contact response, as the legacy impact-effect hook did (L2–L5 below).
- **Work and energy stores.** None. The liquid is a conserved inventory, not an energy store.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `break_speed` | f32 | 1–8 | 3.0 | m/s | **proposed**: a fall of about 0.46 m (√(2·9.81·0.46) ≈ 3.0) breaks it, while a ball rolling into it at 1 m/s does not |
  | `liquid` | f32 | 0–2 | 2.0 | kg | **proposed**: an empty bowl gives the no-contents control |

- **Cosmetic curves and UI bindings.** The fish swims in a small idle loop inside the water volume, driven by the animation worker from the committed bowl pose; it never moves the fish body. The water surface sways with bowl tilt. The fracture shows crack lines on the committed fracture occurrence.
- **Art (DESIGN.md).** Transparent cyan glass `#66b8c9` at low alpha, as the clear pipe shell does (`DESIGN.md@a6c914e:L181-L181`); a cream `#fff8e9` rim; an original simple fish in gold `#f7cb52` with navy `#293954` eye and fin lines; a pale blue water tint. Original silhouette (`DESIGN.md@a6c914e:L53-L53`).
- **Catalogue and inventory entry.** Id `fish_bowl`, title "Fish bowl", category Characters, colour `#66b8c9` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, StructuralFracture, TopologyTransaction).

- **Exists now.** Dynamic spheres and boxes and their contact (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L56`). The committed contact approach speed is already measured for contact triggers (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1177`), but only on static owners.
- **Missing.**
  - Dynamic compound bodies (the bowl): single-collider mass compile (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L30`).
  - StructuralFracture and TopologyTransaction: S543 fracture, next S563; S543 phase-topology, next S565 ([decisions](../invest/decisions.md#s543)).
  - FluidAdvection (liquid inventory and release): S416 advection, next S418; S416 buoyancy, next S420 ([decisions](../invest/decisions.md#s416)).
  - Impact sensing on a dynamic owner: owner S654.
- **Dependencies.** The water capability family (EL-001 onward) for liquid; a ramp or table edge to make it fall.

## 4. Sources and legacy

- **Requirement row.** [element-084](../requirements.md#element-084): a contained fragile payload uses generic impact failure and containment; a subthreshold impact retains contents.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** S416/S470 liquid and gas boundary; S543 fracture and topology ([decisions](../invest/decisions.md)); [S641](../invest/scope-corrections.md#s641) gives the fracture-threshold shape: just below holds, just above breaks into bounded pieces.
- **Legacy search.** `git grep -i` at a6c914e for "fish", "bowl", "fragile", "fracture", "shatter" and "break" over every Epic 7 deletion path found no fracture or fish source. The legacy impact-effect hook is the nearest generic source for the fracture criterion and its timing.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | A contact trigger records the first approach speed at or above its threshold, once | `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1177` (current) | Carry forward the threshold-on-approach-speed rule for the fracture criterion |
| L2 | An impact effect receives a value context: the impact, both bodies' before and after snapshots, and the contact approach speed | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L9-L19` | Carry forward approach speed as the fracture input. Do not carry forward the f64 records. |
| L3 | Effect impulses apply after the ordinary contact response, never change pose and never bypass the next solve; an effect cannot push through the contact that triggered it | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L21-L33`; `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L208-L216` | Carry forward: fragments inherit the post-response velocities and never pass through the striking surface |
| L4 | Collider and joint changes requested by an effect are committed together after the coupled contact response, never during its solve | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L35-L50` | Carry forward: the fracture topology transaction commits after the solve |
| L5 | An effect's simulation state (counters, cooldowns) is captured and restored with the step; presentation waits until the step succeeds | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L52-L63` | Carry forward the transactional rule (a rejected step leaves the bowl intact). Do not carry forward the abstract callback class: an element-owned executable hook is not declaration data ([engine contracts](../../engine-contracts.md)). |
| L6 | Clear motion without contact activates nothing | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L259-L264` | Carry forward as the missed-contact control |
| L7 | Simultaneous contacts notify every subscribed effect after one coupled response, each with the same approach speed | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L267-L280` | Carry forward: two fragile bodies striking each other both see the same approach speed |
| L8 | A body already touching and closing at the start of a step still triggers, without a zero-time sweep | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L282-L290` | Carry forward |
| L9 | Persistent contact does not retrigger; release and recontact does | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L303-L315` | Carry forward: a bowl resting on the bench is not re-evaluated each tick; a later new impact is |

**Files harvested:** `engine/physics/PhysicsImpactEffects.cs`, `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Place the Fish bowl on a shelf (a Wall box) 0.6 m above the bench, and a Basketball rolling along the shelf into it. Run.
- **Positive.** The bowl is pushed off, falls, strikes the bench above 3 m/s and fractures into 4 pieces; the water spreads and the fish lands as a free body. Total liquid is unchanged.
- **Negative or control.** The same bowl nudged gently along the bench (impact below 3 m/s) stays intact with its contents. An empty bowl fractures with no liquid released. No duplicate bowl or extra fish ever appears.
- **Boundaries.** Impacts of 2.9 m/s hold and 3.1 m/s break; fragment masses sum to the bowl mass; liquid released equals liquid held.
- **Run/Reset.** Reset restores the intact bowl, water and fish exactly. **Save/Load.** Placement and parameters round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-200](../requirements.md#todo-200) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-08 fluid advection](../requirements.md#interaction-08), [IX-10 buoyancy](../requirements.md#interaction-10), [IX-37 structural fracture](../requirements.md#interaction-37) and [IX-41 phase topology update](../requirements.md#interaction-41). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" (fragile fish-bowl target) reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: EL-088 Fish tank lure (shared fracture law), EL-093 and EL-106 toy charges (impact sources), the water family (EL-001 onward) for contents.

## 6. Open questions

1. Is the fish a passive payload with cosmetic swimming (proposed), or a locomoting character? Unspecified — owner decision (S654).
2. Liquid mass and volume at game scale (S416).
3. Fragment count and shapes (S563).
