# EL-096 · Tipsy platform — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-096 · Tipsy platform · Specialist |
| Requirement anchor | [element-096](../requirements.md#element-096); scope index [todo-219](../requirements.md#todo-219) (shared with TH-33, TH-35, TH-36) |
| Named entry | [element-096](../invest/named-elements.md#element-096); source owner **S666** |
| CAT spec refined or extended | Related to [CAT-034 Impact lever](CAT-034-impact_lever.md) (a hinged beam) and [CAT-023 Domino](CAT-023-domino.md) (contact tipping of a dynamic box); it extends neither |
| Related identities | EL-112 Structural beam, CAT-054 Ramp |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

Tipping is pure rigid-body contact: the platform rests on a narrow flat ridge and tips only when the combined centre of mass of platform and load leaves the ridge's support width. There is no hinge, timer or tipping script (**proposed**: the Domino already proves box tipping on contact in the shared solver, `engine/gpu/WorkshopDomino.cs@a6c914e:L20-L21`).

- **Bodies and shapes.**
  - Platform: one dynamic box 2.4 × 0.12 × 0.8 m, half extents (1.2, 0.06, 0.4) (**proposed**: long enough for a visible lever arm; about a Ramp's width deep, `engine/gpu/WorkshopInstances.cs@a6c914e:L22-L25`).
  - Ridge (fulcrum): one static box 0.2 × 0.6 × 0.8 m, half extents (0.1, 0.3, 0.4), under the platform's centre (**proposed**: a support width of ±0.1 m sets the tipping threshold).
- **Mass and material.** Platform 1.5 kg, restitution 0.05, friction 0.6, threshold 0.1 m/s (**proposed**: the Domino's wooden contact values, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L10`). Ridge: the shared static material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`).
- **Constraints.** None. A "constrained" platform is made by the player with real props (a Wall block under one end), never by a hidden lock.
- **Typed ports.** None.
- **Sensors and activation.** None owned. Tipping can be observed through generic sensors (for example a Domino-style orientation threshold would be a separate, later choice).
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `ridge_offset` | f32 | −0.8–0.8 | 0 | m along the platform | **proposed**: lets the author pre-bias the balance, so one side needs less load |

  **Tipping threshold (derived).** A load of mass *m* at offset *x* tips the platform when *m x* ÷ (1.5 + *m*) > 0.1 m. A 1 kg Basketball tips it beyond 0.25 m; a 0.35 kg Tennis ball beyond 0.53 m.
- **Cosmetic curves and UI bindings.** None. The platform follows its committed pose.
- **Art (DESIGN.md).** A warm wood `#c28f52` plank (the Ramp colour, `DESIGN.md@a6c914e:L169-L169`) with cream `#fff8e9` end caps and a navy `#293954` ridge; a gold `#f7cb52` centre mark shows the balance point. Original pictogram of a plank on a ridge.
- **Catalogue and inventory entry.** Id `tipsy_platform`, title "Tipsy platform", category Motion, colour `#c28f52` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction).

- **Exists now.** Every needed physical family: dynamic box on static box, with box–box manifolds, friction and four-point area reduction (`engine/gpu/WorkshopDomino.cs@a6c914e:L7-L35`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L774`). Derived inertia from extents and mass (`engine/gpu/RigidMassProperties.cs@a6c914e:L17-L30`). JointConstraint is inherited and not used.
- **Missing.** A two-body part (one static ridge and one dynamic platform as a single placed part), its `WorkshopPartKind`, the `ridge_offset` parameter, icon and catalogue entry: owner S666.
- **Dependencies.** None to place it. Loads: CAT-001 Basketball, CAT-014 Bowling ball; a CAT-066 Wall as the prop control.

## 4. Sources and legacy

- **Requirement row.** [element-096](../requirements.md#element-096): an offset supported load changes equilibrium and causes physical tipping; a balanced or constrained platform does not tip on a timer.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Research reference.** The Sierra Chest walkthrough cited by [todo-219](../requirements.md#todo-219); intent, not constants.
- **Legacy search.** `git grep -i` at a6c914e for "tipsy", "teeter", "seesaw", "balance" and "tip" found no tipping-platform source. Adjacent facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Impact lever: a passive hinged beam (beam mass 2, initial angle 0); a falling load turns one end and can lift a lighter load; fixed stops limit its swing | `parts/catalog/impact_lever.tres@a6c914e:L9-L12` | Do not carry forward as this part's mechanism (a hinge is not "tipsy"); it is the related lever, owned by [CAT-034](CAT-034-impact_lever.md) |
| L2 | The historical scripted Domino tilt is not a physical authority; show the committed pose | `DESIGN.md@a6c914e:L275-L275` (current) | Carry forward: no scripted tipping |

**Files harvested:** `parts/catalog/impact_lever.tres`.

## 5. Acceptance outline

- **Chrome recipe.** Place the Tipsy platform on the bench. Drop a Basketball onto its centre, and on a second copy drop one 0.6 m off-centre. Run.
- **Positive.** The off-centre ball tips the platform, which rotates down on that side and slides or rolls the ball off.
- **Negative or control.** The centred ball rests and the platform stays level for the whole Run (no timed tip). A Wall block under the loaded end props it: no tipping.
- **Boundaries.** A Basketball at 0.2 m holds and at 0.3 m tips; with `ridge_offset` 0.5 m the unloaded platform already rests tipped toward its long side; Run/Reset repeat identically.
- **Run/Reset.** Reset restores the level platform exactly. **Save/Load.** Placement and `ridge_offset` round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-219](../requirements.md#todo-219) scope index (shared with [TH-33](../requirements.md#thermal-33), [TH-35](../requirements.md#thermal-35) and [TH-36](../requirements.md#thermal-36), whose interactions are proved separately) routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" ("tipsy/unstable platform") reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" ("unstable platform") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Basketball and Bowling ball loads (CAT-001, CAT-014), Wall prop (CAT-066).

## 6. Open questions

1. Contact-only tipping (proposed) versus a hinge with a weak centring spring. Unspecified — owner decision (S666).
2. Whether the ridge is part of the element (proposed) or any support may be used.
3. The interactions with TH-33, TH-35 and TH-36 named in the shared scope index.
