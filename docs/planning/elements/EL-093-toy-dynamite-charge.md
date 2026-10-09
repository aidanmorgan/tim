# EL-093 · Toy dynamite charge — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

**Safety boundary.** This is an abstract toy store of energy in joules that releases one declared impulse field. No real composition, formulation, quantity, ignition chemistry or construction recipe is modelled or described (requirement row).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-093 · Toy dynamite charge · Specialist |
| Requirement anchor | [element-093](../requirements.md#element-093); scope index [todo-197](../requirements.md#todo-197) |
| Named entry | [element-093](../invest/named-elements.md#element-093); source owner **S663** |
| CAT spec refined or extended | None. Its finite paid-work release follows the [CAT-015 Bumper](CAT-015-bumper.md) contact-work store. |
| Related identities | EL-094 Detonation plunger (its trigger), EL-106 Impact-sensitive toy charge (the same release law, different trigger), EL-084 Fish bowl (a fragile target) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

- **Bodies and shapes.** One dynamic body: a box 0.18 × 0.6 × 0.18 m, half extents (0.09, 0.3, 0.09) (**proposed**: a recognisable stick at bench scale, smaller than a Domino).
- **Mass and material.** 0.3 kg, restitution 0.1, friction 0.5 (**proposed**: a light paper-wrapped stick that does not bounce).
- **Constraints.** None.
- **Typed ports.** `ActivationIn` (Activation, Input) at the top (0, 0.3, 0) (**proposed**: the trigger arrives as a typed command, normally from EL-094).
- **Sensors and activation.** One trigger command releases the charge at the next tick boundary. A trigger on a spent charge does nothing. The charge does not react to impacts (EL-106 is the impact-sensitive variant).
- **Work and energy stores.** One finite store of `energy` joules, full at Run. **Release law (proposed):** a single radial impulse field centred on the charge, applied once, within `blast_radius`, to each dynamic body with an unobstructed sight line from the centre (opaque bodies shield). Each body's requested impulse is `peak_impulse` × (1 − d ÷ `blast_radius`) along the outward direction. If the summed kinetic energy delivered would exceed `energy`, every impulse is scaled down by the same factor α, the envelope's proportional rule ([envelope](../../gpu-f32-physics.md#game-grade-envelope)). The store then drops to zero and the body becomes a **spent husk**: the same body remains, with a spent state and no energy. Nothing is destroyed or spawned.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `energy` | f32 | 5–100 | 40 | J | **proposed**: about the cannon's 45 J default (`parts/catalog/cannon.tres@a6c914e:L12-L12`), so it is a comparable toy |
  | `blast_radius` | f32 | 0.5–3 | 1.5 | m | **proposed**: reaches neighbouring parts, not the whole bench |
  | `peak_impulse` | f32 | 0.5–6 | 3 | N·s | **proposed**: sends a 1 kg Basketball off at about 3 m/s at point blank |

- **Cosmetic curves and UI bindings.** A cartoon puff ring expands over 0.4 s from the committed release occurrence. The stick shows a charred spent state. A gentle camera-independent flash on the part only, with no screen-wide effect (`DESIGN.md@a6c914e:L264-L264`).
- **Art (DESIGN.md).** A coral `#f06e54` stick (the Switch colour, `DESIGN.md@a6c914e:L173-L173`) with cream `#fff8e9` bands and a navy `#293954` cap. A playful toy, not a realistic explosive (**proposed**).
- **Catalogue and inventory entry.** Id `toy_dynamite`, title "Toy charge", category Motion, colour `#f06e54`, counted allowance (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ChemicalReaction, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, SensibleHeat, StateTransaction, StructuralFracture, TopologyTransaction).

- **Exists now.** Finite paid contact work with a typed effect and per-target occurrences (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L36`; `engine/gpu/WorkshopBumper.cs@a6c914e:L6-L28`). Activation input and coalesced occurrences (`engine/gpu/ActivationTimers.cs@a6c914e:L53-L66`). Dynamic boxes (`engine/gpu/WorkshopDomino.cs@a6c914e:L7-L35`).
- **Missing.**
  - A one-shot radial impulse field with occlusion and a shared energy cap (FiniteWorkActuation, GeometryQuery): owner S663.
  - ChemicalReaction as the abstract charge: S543 reaction, next S554 ([decisions](../invest/decisions.md#s543)).
  - Fracture of nearby fragile targets: S543 fracture, next S563 (consumer side).
  - A spent-state StateTransaction on the body: owner S663.
- **Dependencies.** EL-094 Detonation plunger or CAT-063 Switch (trigger); CAT-066 Wall (shield control).

## 4. Sources and legacy

- **Requirement row.** [element-093](../requirements.md#element-093): a finite toy energy store releases one declared pressure impulse when triggered; a spent charge cannot repeat; no real formulation or construction recipe.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** S543; the [ENGINE-LEDGER](../invest/scope-corrections.md#engine-ledger) rule (a paid element never fires unpaid).
- **Legacy search.** `git grep -i` at a6c914e for "dynamite", "explos", "blast" and "detonat" over every Epic 7 deletion path found nothing. Finite-store facts apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | An energy store is declared with finite capacity and an initial energy between 0 and capacity | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L7-L19` | Carry forward |
| L2 | A debit must be finite, non-negative and no larger than the stored energy; released energy is accounted | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52` | Carry forward the rule. Do not carry forward the CPU double ledger. |
| L3 | Release is atomic: failure restores the world and the store debit equals supplied work | `engine/physics/PhysicsWorld.cs@a6c914e:L1250-L1267` | Carry forward "release and spend commit together" |

**Files harvested:** `engine/physics/PhysicsEnergyStore.cs`, `engine/physics/PhysicsWorld.cs` (energy-store release only).

## 5. Acceptance outline

- **Chrome recipe.** Place the Toy charge on the bench with three Basketballs at 0.5, 1.0 and 2.0 m, and a Wall between the charge and a fourth ball at 1.0 m. Wire EL-094 Plunger (or a Switch) `ActivationOut` → `ActivationIn`. Run and press the plunger with a falling Bowling ball.
- **Positive.** One release: the 0.5 m ball moves fastest and the 1.0 m ball slower; the charge shows spent.
- **Negative or control.** The 2.0 m ball (outside the radius) does not move. The shielded ball does not move. With no trigger, nothing happens. A second trigger does nothing. Total kinetic energy given ≤ 40 J.
- **Boundaries.** A ball at 1.49 m receives a small impulse and one at 1.51 m none; the energy cap scales all impulses equally when many bodies are near.
- **Run/Reset.** Reset restores the full store, the unspent stick and all balls exactly. **Save/Load.** Parameters and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-197](../requirements.md#todo-197) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-28 chemical reaction](../requirements.md#interaction-28), [IX-29 reaction ignition](../requirements.md#interaction-29) and [IX-37 structural fracture](../requirements.md#interaction-37) (fragile targets). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" ("dynamite/plunger abstraction") reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" ("toy rocket/ignition/destruction variants") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: EL-094 Detonation plunger or Switch (CAT-063) trigger, Wall shield (CAT-066), Basketball targets (CAT-001), EL-084 Fish bowl as a fragile target.

## 6. Open questions

1. Release law shape: linear falloff with an energy cap (proposed). Unspecified — owner decision (S663).
2. Whether the release can fracture fragile parts directly, or only through their own impact criterion (proposed).
3. Whether the spent husk stays (proposed) or is removed by a topology transaction.
