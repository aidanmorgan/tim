# EL-106 · Impact-sensitive toy charge — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

**Safety boundary.** An abstract toy store of energy in joules with a generic shock threshold. The historical nitroglycerine piece is a reference only (requirement row); no real substance, formulation, quantity or handling is modelled or described.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-106 · Impact-sensitive toy charge · Specialist |
| Requirement anchor | [element-106](../requirements.md#element-106); scope index [todo-227](../requirements.md#todo-227) |
| Named entry | [element-106](../invest/named-elements.md#element-106); source owner **S379** |
| CAT spec refined or extended | None. Its threshold follows the [CAT-063 Switch](CAT-063-switch.md) contact-trigger pattern; its release follows [EL-093](EL-093-toy-dynamite-charge.md). |
| Related identities | EL-093 Toy dynamite charge (the same release law, command-triggered), EL-084 Fragile fish bowl (the same impact-threshold idea, different effect) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

- **Bodies and shapes.** One dynamic sphere r 0.15 m (**proposed**: a small round flask silhouette, well above the 1/16 m floor, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L100-L104`).
- **Mass and material.** 0.4 kg, restitution 0.3, friction 0.4, threshold 0.1 m/s (**proposed**: a glass-like toy flask).
- **Constraints.** None.
- **Typed ports.** None. It reacts only to shock, never to a command (that is EL-093).
- **Sensors and activation.** A shock criterion on the charge itself: the first contact whose normal approach speed is ≥ `shock_speed` releases the charge on that substep. A contact below the threshold changes nothing and leaves it unspent. The approach speed is the relative normal speed of the two contacting bodies, so being struck and striking are treated alike. The release impulses apply after the step's contact response and the spent-state change commits after the solve (L3–L6 below).
- **Work and energy stores.** One finite store of `energy` joules. **Release law:** the EL-093 law (proposed there): one radial impulse field within `blast_radius` with linear falloff, line-of-sight shielding, and every impulse scaled down together so total kinetic energy given ≤ `energy`. The body then becomes a spent husk (the same body; no destroy or spawn) and cannot release again.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `shock_speed` | f32 | 0.5–8 | 2.5 | m/s | **proposed**: above the 0.8 m/s Switch threshold (`engine/gpu/WorkshopActivationParts.cs@a6c914e:L6-L8`), so careful rolling is safe; a drop of about 0.32 m (√(2·9.81·0.32) ≈ 2.5) sets it off |
  | `energy` | f32 | 5–60 | 25 | J | **proposed**: smaller than EL-093's 40 J, a lighter toy |
  | `blast_radius` | f32 | 0.5–3 | 1.2 | m | **proposed** |
  | `peak_impulse` | f32 | 0.5–5 | 2.5 | N·s | **proposed** |

- **Cosmetic curves and UI bindings.** A gentle wobble of the liquid inside follows committed angular velocity, and its tint warms as approach speeds near the threshold, from the committed contact read (presentation only). A puff ring plays on release. Spent state: an empty flask.
- **Art (DESIGN.md).** A transparent cyan `#66b8c9` flask (low alpha, `DESIGN.md@a6c914e:L181-L181`) with a cream `#fff8e9` stopper, a gold `#f7cb52` liquid and a coral `#f06e54` warning band. A toy, not a laboratory item (**proposed**).
- **Catalogue and inventory entry.** Id `shock_charge`, title "Jumpy flask", category Motion, colour `#66b8c9` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-02.json` (ChemicalReaction, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, SensibleHeat, StateTransaction, StructuralFracture, TopologyTransaction).

- **Exists now.** Contact triggers with a typed approach-speed threshold, recording the first qualifying approach once (`engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L8-L20`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1177`). These triggers are owned only by static bodies. Dynamic spheres (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`).
- **Missing.**
  - A contact trigger on a dynamic owner (the charge moves): owner S379.
  - The one-shot radial impulse field with an energy cap (shared with EL-093): owners S663 and S379.
  - ChemicalReaction as the abstract charge: S543 reaction, next S554 ([decisions](../invest/decisions.md#s543)).
  - The spent-state StateTransaction: owner S379.
- **Dependencies.** EL-093 (release law), CAT-054 Ramp or a drop to produce the shock, CAT-066 Wall (shield control).

## 4. Sources and legacy

- **Requirement row.** [element-106](../requirements.md#element-106): finite abstract stored energy reacts to a generic shock threshold; subthreshold contact leaves it unspent; historical nitroglycerine is a reference only.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "nitro", "shock", "explos" and "sensitive" over every Epic 7 deletion path found no source. The generic impact-effect hook is the nearest source for the shock criterion and the release timing.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Impact-switch threshold is a typed approach speed, default 0.8 m/s, range-checked 0–64 | `engine/gpu/WorkshopActivationParts.cs@a6c914e:L6-L10` (current) | Carry forward the typed threshold; the value is raised (proposed) |
| L2 | A finite store debit never exceeds stored energy; release and debit commit together | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52`; `engine/physics/PhysicsWorld.cs@a6c914e:L1250-L1267` | Carry forward the rule. Do not carry forward the CPU code. |
| L3 | An impact effect receives a value context: the impact, both bodies' before and after snapshots, and the contact approach speed | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L9-L19` | Carry forward approach speed as the shock input. Do not carry forward the f64 records. |
| L4 | Effect impulses apply after the ordinary contact response, never change pose and never bypass the next solve; an effect cannot push through the contact that triggered it | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L21-L33`; `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L208-L216` | Carry forward: the release impulses are applied after the contact response and cannot push a body through the striking surface |
| L5 | Collider and joint changes requested by an effect are committed together after the coupled contact response, never during its solve | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L35-L50` | Carry forward: the spent-state change commits after the solve |
| L6 | An effect's simulation state is captured and restored with the step; presentation waits until the step succeeds | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L52-L63` | Carry forward the transactional rule (a rejected step leaves the flask unspent). Do not carry forward the abstract callback class: an element-owned executable hook is not declaration data ([engine contracts](../../engine-contracts.md)). |
| L7 | Clear motion without contact activates nothing | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L259-L264` | Carry forward as the missed-contact control |
| L8 | Simultaneous contacts notify every subscribed effect after one coupled response, each with the same approach speed | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L267-L280` | Carry forward: two flasks striking each other both see the same approach speed and both release if it meets their thresholds |
| L9 | A body already touching and closing at the start of a step still triggers, without a zero-time sweep | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L282-L290` | Carry forward |
| L10 | Persistent contact does not retrigger; release and recontact does | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L303-L315` | Carry forward: a flask resting on the bench is not re-evaluated each tick; a later new impact is |

**Files harvested:** `engine/physics/PhysicsEnergyStore.cs`, `engine/physics/PhysicsWorld.cs` (energy-store release only), `engine/physics/PhysicsImpactEffects.cs`, `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Place two Jumpy flasks: one on a 0.5 m shelf that a rolling ball pushes off, one rolled gently down a shallow Ramp onto the bench. Place Basketballs at 0.5 m and 2 m from the drop point, and a Wall shielding a third ball. Run.
- **Positive.** The dropped flask hits the bench at about 3.1 m/s, releases once, pushes the 0.5 m ball outward and shows spent.
- **Negative or control.** The gently rolled flask (impacts below 2.5 m/s) stays unspent all Run. The 2 m ball and the shielded ball do not move. A spent flask dropped again does nothing.
- **Boundaries.** An impact at 2.4 m/s holds and 2.6 m/s releases; total kinetic energy given ≤ 25 J; one release per Run.
- **Run/Reset.** Reset restores the full, unspent flask and all balls exactly. **Save/Load.** Parameters round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-227](../requirements.md#todo-227) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-28 chemical reaction](../requirements.md#interaction-28) and [IX-37 structural fracture](../requirements.md#interaction-37). Its cross-element [separate integration verification](../requirements.md#sequence-task-798) requires a primary observation or an explicit unresolved entry for each edition-specific claim, and [sequence-task-552](../requirements.md#sequence-task-552) refines the historical candidate register. Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) names no shock-charge row; the nearest family is the row beginning "Original cat/mouse lure/escape roles" ("dynamite/plunger abstraction"; 91–100, reuse 136–150) and chapter 10 "Oddly Satisfying" ("toy rocket/ignition/destruction variants") in the [campaign plan](../requirements.md#campaign-plan); the ledger task must add a separate row. Element integrations: EL-093 release law, Ramp or shelf drop (CAT-054, CAT-066), Basketball targets (CAT-001), Wall shield (CAT-066).

## 6. Open questions

1. Shock measure: approach speed (proposed) versus impulse or deceleration. Unspecified — owner decision (S379).
2. Whether the release can set off a neighbouring flask (a chain reaction) through its impulse alone (proposed: yes, only if its own threshold is reached).
