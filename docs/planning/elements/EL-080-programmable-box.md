# EL-080 · Programmable box — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-080 · Programmable box · Specialist |
| Requirement anchor | [element-080](../requirements.md#element-080); scope index [todo-222](../requirements.md#todo-222) |
| Named entry | [element-080](../invest/named-elements.md#element-080); source owner **S650** ([S650 decision](../invest/scope-corrections.md#s650)) |
| CAT spec refined or extended | Extends [CAT-023 Domino](CAT-023-domino.md) (dynamic box) and [CAT-004 Receiver](CAT-004-basket.md) (open container walls) |
| Related identities | EL-076 Programmable ball (the same preset-admission pattern), EL-069 Moving bucket, EL-192 Receiving basket |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

S650 makes this **preset admission, not a controller or a numeric solution editor** ([scope corrections](../invest/scope-corrections.md#law-controller-i)).

- **Bodies and shapes.** One dynamic open-top box: a floor and four walls as box colliders on one body, wall half-thickness 0.06 m (the Receiver wall thickness, `engine/gpu/ReceiverGeometry.cs@a6c914e:L12-L16`). Outer size comes from the size preset.
- **Mass and material.** Three closed enums, chosen in build mode:

  | Enum | Members and values | Status |
  | --- | --- | --- |
  | `BoxSizePreset` | Small 0.8 × 0.6 × 0.8 m · Medium 1.2 × 0.8 × 1.2 m · Large 1.6 × 1.0 × 1.6 m | **proposed**: Medium fits one Basketball (0.68 m diameter) with clearance; Large matches the 1.5 m Receiver footprint |
  | `BoxMassPreset` | Light 0.5 kg · Standard 2 kg · Heavy 6 kg | **proposed**: inside the legacy Weight range 0.25–8 kg (`parts/WeightPart.cs@a6c914e:L24-L26`), from lighter than a Basketball to heavier than a Bowling ball |
  | `BoxMaterialPreset` | Wood (restitution 0.1, friction 0.6) · Metal (0.05, 0.3) · Rubber (0.5, 0.9) | **proposed**: Wood reuses the Domino's friction 0.6 (`engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`); Metal slides, Rubber grips and bounces |

  Shared: bounce threshold 0.1 m/s and rolling resistance 0, the box convention (`engine/gpu/WorkshopDomino.cs@a6c914e:L10-L10`; `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L27-L27`). Inertia comes from the actual wall geometry and mass, not a solid-box approximation.
- **Unsupported combinations (proposed).** Large with Light is rejected: a 1.6 m, 0.5 kg box is shoved by any ball and cannot hold a load readably. This excludes 3 of the 27 combinations (one per material), so 24 of the 27 are admitted; rejection happens at input, atomically.
- **Constraints.** None; a free body.
- **Typed ports.** None. The inherited controller families are not used (S650).
- **Sensors and activation.** None owned. Containment is physical: contents rest on the floor and are held by the walls.
- **Work and energy stores.** None.
- **Parameters.** The three enums. Defaults Medium, Standard, Wood (**proposed**: a mid-size wooden crate that holds one ball). Editable only in build mode through contextual icon choices; no numeric fields.
- **Cosmetic curves and UI bindings.** None physical. Material shows as surface pattern (wood grain lines, rivets, rubber ribs) and mass as cream corner bands (1, 2 or 3), so neither relies on colour alone.
- **Art.** Warm wood `#c28f52` walls (the Physical wall colour, `DESIGN.md@a6c914e:L178-L178`), cream `#fff8e9` rims and navy `#293954` pattern ink; chamfered corners per the art direction (`DESIGN.md@a6c914e:L42-L54`).
- **Catalogue and inventory entry.** Id `programmable_box`, title "Programmable box", category Motion, colour `#c28f52`, counted allowance (**proposed**).

**Variants.** The requirement row names no variant. Preset combinations are parameter values.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.**
  - Dynamic single-box bodies with derived inertia (the Domino, `engine/gpu/WorkshopDomino.cs@a6c914e:L7-L35`; `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L30`).
  - Static five-box open containers (the Receiver, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L85`).
  - Box–sphere and box–box contact with friction (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L774`).
- **Missing.**
  - Dynamic compound bodies: mass properties compile from exactly one collider per dynamic body (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L30`). Decision owner S650; S650-I builds the compound mass and inertia.
  - Preset-keyed material admission, and wire/save fields for three enums: S650-I.
- **Dependencies.** None to place it. Integrations: CAT-001 Basketball as contents, CAT-054 Ramp, CAT-066 Wall.

## 4. Sources and legacy

- **Requirement row.** [element-080](../requirements.md#element-080): author-defined validated size, mass and material form a physical container; contents and collisions respond to its actual geometry.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** [S650](../invest/scope-corrections.md#s650): freeze one validated preset; a contained load stays inside its actual walls, a collision deflects from them, and an unsupported preset is refused. Research reference: TIM2 manual ([todo-222](../requirements.md#todo-222)).
- **Legacy search.** `git grep -i` at a6c914e for "programmable", "crate" and "container" over every Epic 7 deletion path found no programmable-box source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Weight mass must be finite and 0.25–8 kg | `parts/WeightPart.cs@a6c914e:L24-L26` | Carry forward as the mass-preset envelope |
| L2 | Every catalogue part with physics parameters rejects a missing parameter; Run/Restore round-trips | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` | Carry forward as "incomplete or unknown preset rejects". Do not carry forward string keys. |

**Files harvested:** `parts/WeightPart.cs`, `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`.

## 5. Acceptance outline

- **Chrome recipe.** Place a Programmable box (Medium, Standard, Wood) on the bench with the move gizmo. Drop a Basketball into it. Roll a second Basketball down a Ramp into its side. Run.
- **Positive.** The dropped ball settles inside on the floor; the rolling ball deflects off the outer wall; a light box is pushed further than a Heavy one by the same ball.
- **Negative or control.** A ball dropped just outside the rim falls beside the box. Selecting Large with Light is refused at input and the construction is unchanged. No numeric field is offered.
- **Boundaries.** The contained ball stays within the inner walls for the run; a ball rolling over the rim edge is deflected by the actual wall top; box rest penetration ≤ 0.5 mm ([envelope](../../gpu-f32-physics.md#game-grade-envelope)).
- **Run/Reset.** Presets are locked during Run; Reset restores the box and contents exactly. **Save/Load.** The enums round-trip; an unknown value in a save is rejected.
- **Integrations.** Requirement-row integration tasks: the [todo-222](../requirements.md#todo-222) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row "Size grate, weight tray/material sorter, timed ejector/toaster, egg timer, phazer-like toy pulse source, opener, mixer, box presets and contact display" reserves levels 91–100 (reuse 111–120, 136–150); chapter 10 "Oddly Satisfying" ("configurable container") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Basketball contents (CAT-001), Ramp (CAT-054), Wall (CAT-066), EL-102 Large-bore pipe cargo, EL-100 Vacuum nozzle control.

## 6. Open questions

1. Preset membership: confirm the size, mass and material sets and the rejected combination. Unspecified — owner decision (S650).
2. Open-top only (proposed), or also a closed or lidded box.
3. Whether the box may be locked as a static fixture in authored levels.
