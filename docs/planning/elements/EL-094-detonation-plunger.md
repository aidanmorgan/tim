# EL-094 · Detonation plunger — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-094 · Detonation plunger · Specialist |
| Requirement anchor | [element-094](../requirements.md#element-094); scope index [todo-197](../requirements.md#todo-197) |
| Named entry | [element-094](../invest/named-elements.md#element-094); source owner **S664** |
| CAT spec refined or extended | Extends the activation-source pattern of [CAT-063 Switch](CAT-063-switch.md) and [CAT-023 Domino](CAT-023-domino.md) (a command after actual motion) |
| Related identities | EL-093 Toy dynamite charge and EL-092 Toy rocket (command consumers), CAT-039 Linear pusher (a slider body) |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The element-map composition rule applies: the trigger is a generic mechanical or electrical intent boundary; ChemicalReaction belongs to the supplied charge, not to an intrinsic plunger explosion ([element map](../general-engine-element-map.md)). The plunger owns no energy store.

- **Bodies and shapes.**
  - Static box: 0.7 × 0.5 × 0.5 m, half extents (0.35, 0.25, 0.25).
  - Dynamic T-handle on a vertical slider: a shaft box 0.08 × 0.6 × 0.08 m and a crossbar 0.6 × 0.08 × 0.08 m, as one compound body. The rest pose is raised 0.3 m above the box top.

  These sizes are **proposed**: a classic plunger box at bench scale; the crossbar is wide enough to catch a falling ball.
- **Mass and material.** Handle 0.2 kg, restitution 0.1, friction 0.4 (**proposed**: light enough for a Basketball to drive down). Box static, with the shared static material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`).
- **Constraints.** A prismatic joint along box local Y with travel limits 0 to −0.3 m. A return spring with a 3 N preload and 20 N/m stiffness holds the handle up: at rest it carries the handle's own weight (0.2 kg × 9.81 ≈ 1.96 N) with about 1 N of margin. No joint damping is declared, so a load placed on the crossbar overshoots to about twice its static travel x = (L + 1.96 − 3) ÷ 20 m before settling. A placed Tennis ball (0.35 kg, 3.43 N) peaks near 0.24 m and settles near 0.12 m, short of the 0.27 m fire point. The dynamic firing threshold for a placed load is about 0.38 kg, and a falling ball fires through its momentum (**proposed**: a real press fires; a light object resting on it does not).
- **Typed ports.** `ActivationOut` (Activation, Output) on the box side (0.35, 0, 0) (**proposed**). The existing socket enum covers it (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).
- **Sensors and activation.** A translation-threshold sensor on the handle: when the committed handle travel reaches `fire_fraction` × 0.3 m, one `Fire` occurrence is emitted. This mirrors the Domino's orientation sensor, which fires once on actual pose change and is sticky until Reset (`engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L26-L36`; `engine/gpu/WorkshopDomino.cs@a6c914e:L25-L26`). It never fires from a timer or command.
- **Work and energy stores.** None. The command carries no energy; a charge receiving it releases only its own store. The return spring's elastic energy is part of the joint, not an output.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `fire_fraction` | f32 | 0.5–1.0 | 0.9 | fraction of stroke | **proposed**: needs an almost full press, so a graze does not fire |

- **Cosmetic curves and UI bindings.** The handle follows its committed pose. A small slate `#556573` to gold `#f7cb52` lamp on the box latches on `Fire` (Activation feedback curve, `engine/gpu/WorkshopCosmetic.cs@a6c914e:L51-L58`). The activation link renders as the shared gold cable (`DESIGN.md@a6c914e:L281-L281`).
- **Art (DESIGN.md).** A navy `#293954` box with cream `#fff8e9` panel lines, a gold `#f7cb52` T-handle and a coral `#f06e54` warning band. Toy-like, not realistic (**proposed**).
- **Catalogue and inventory entry.** Id `plunger`, title "Plunger box", category Control, colour `#293954` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ChemicalReaction, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, PhaseTopology, RigidBodyDynamics, SensibleHeat, StateTransaction, StructuralFracture, TopologyTransaction). ChemicalReaction is inherited from the charge family and is not used by the plunger itself.

- **Exists now.** One-shot pose-threshold sensors feeding activation sources (`engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L9-L36`; `engine/gpu/ActivationNetwork.cs@a6c914e:L8-L8`, `OrientationSource`). Activation edges to consumers (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L62`).
- **Missing.**
  - Prismatic joint with limits and a preloaded return spring (JointConstraint): Epic 10 (Story 10.3/10.2); the spring follows the CAT-062 elastic contract.
  - A translation-threshold sensor (the positional sibling of the orientation sensor): owner S664.
  - Dynamic compound bodies (the T-handle): single-collider mass compile (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L30`).
- **Dependencies.** A consumer: EL-093 or EL-092. A pressing load such as CAT-014 Bowling ball.

## 4. Sources and legacy

- **Requirement row.** [element-094](../requirements.md#element-094): mechanical input emits a typed control command after actual stroke; the command supplies no explosion energy of its own.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Legacy search.** `git grep -i` at a6c914e for "plunger" finds only the wound-spring launcher's plunger head (CAT-071 family, `CuriousContraptions.tests/ScenePlungerBindingTests.cs`), a launcher, not a command plunger. "Detonat" finds nothing. Adjacent facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | The wound-spring plunger is a guided body on a frame joint, pushed by payload contacts | `CuriousContraptions.tests/ScenePlungerBindingTests.cs@a6c914e:L148-L174` | Carry forward only the "guided slider moved by actual contact" pattern; no element knowledge for this part |
| L2 | Domino emits once after turning 45° from its admitted pose; Reset rearms | `engine/gpu/WorkshopDomino.cs@a6c914e:L25-L26` (current) | Carry forward as the fire-after-actual-motion pattern |

**Files harvested:** `CuriousContraptions.tests/ScenePlungerBindingTests.cs` (checked; a launcher plunger, no element knowledge).

## 5. Acceptance outline

- **Chrome recipe.** Place the Plunger box under a Ramp's end so a Bowling ball drops onto the T-handle; wire `ActivationOut` → an EL-093 Toy charge (or a Lamp before EL-093 exists). Run.
- **Positive.** The ball drives the handle down; at 90 % of the stroke one `Fire` occurrence is emitted; the charge releases (or the Lamp lights).
- **Negative or control.** A Tennis ball resting on the crossbar peaks near 0.24 m, settles near 0.12 m and nothing fires. Its margin is only 0.031 m: a Tennis ball released more than about 3.8 cm above the crossbar can reach the fire point, so the control places it resting on the crossbar. No load: the handle stays up and nothing fires. Disconnected: the handle moves and no consumer reacts. The plunger never adds energy to the charge's release.
- **Boundaries.** Fires at travel 0.27 m and not at 0.26 m; a placed Tennis ball (0.35 kg) holds (peak about 0.24 m) while a placed EL-077 Soccer ball (0.45 kg) fires (peak about 0.34 m); exactly one occurrence per Run.
- **Run/Reset.** Reset restores the handle to rest and rearms the sensor. **Save/Load.** Placement, parameter and wiring round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-197](../requirements.md#todo-197) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-03 joint constraint](../requirements.md#interaction-03) (prismatic handle) and [IX-07 signal propagation](../requirements.md#interaction-07) (the `Fire` command carries no energy). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" ("dynamite/plunger abstraction") reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" ("toy rocket/ignition/destruction variants") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: EL-093 Toy dynamite charge and EL-092 Toy rocket (consumers), Signal lamp (CAT-035) before EL-093 exists, Bowling ball press (CAT-014), Tennis ball and EL-077 Soccer ball boundary loads.

## 6. Open questions

1. Undamped return spring with preload (proposed) versus a damped joint or a handle that rests on a stop. Unspecified — owner decision (S664).
2. Whether an electrical (supplied) trigger variant is wanted ("mechanical or electrical intent boundary").
3. Re-arm during a Run: never (proposed) or when the handle returns.
