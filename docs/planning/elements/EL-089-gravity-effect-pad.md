# EL-089 · Gravity-effect pad — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-089 · Gravity-effect pad · Specialist |
| Requirement anchor | [element-089](../requirements.md#element-089); scope index [todo-216](../requirements.md#todo-216) |
| Named entry | [element-089](../invest/named-elements.md#element-089); source owner **S659** |
| CAT spec refined or extended | None |
| Related identities | EL-124 Authored gravity preset (world gravity), EL-098 Pool ball (its conditional no-gravity variant would reuse this field rule), EL-090 Steerable blimp |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The coverage binding fixes the model: an authored bounded gravity volume uses EnvironmentState and geometry-qualified acceleration on rigid bodies, **not** magnetic or electric FieldForce (`docs/coverage/engine/element-01.json`, binding element-089). The field is labelled in the UI as deliberate game fiction (requirement row).

- **Bodies and shapes.**
  - Static pad: one box 1.6 × 0.12 × 1.6 m, half extents (0.8, 0.06, 0.8) (**proposed**: about one Receiver footprint, a placeable floor tile).
  - Field volume: an axis-aligned box in the pad frame, from (−0.8, 0.06, −0.8) to (0.8, 3.06, 0.8) (**proposed**: 3 m tall, enough for a visible slow fall or rise). It is a query region, not a collider.
- **Mass and material.** Static, zero mass; the shared static material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`).
- **Constraints.** None.
- **Typed ports.** None. It is always on during Run; the requirement names no supply.
- **Sensors and activation.** Membership test per substep: a dynamic body whose centre of mass lies inside the volume integrates with the field gravity; any other body keeps world gravity. Membership is binary, with no blending at the boundary (**proposed**: the simplest rule that keeps outside bodies exactly on world gravity).
- **Work and energy stores.** None. The field is fiction, but bodies still obey the envelope; no energy ledger is claimed (see Open questions).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `gravity_scale` | f32 | −1.0–1.5 | 0.25 | × world gravity | **proposed**: −1 reverses gravity, 0.25 gives a readable "moon" fall; 1.5 × 9.81 = 14.7 m/s² stays inside the per-body admission bound of 16 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L83`) |
  | `field_height` | f32 | 0.5–6 | 3 | m | **proposed**: the volume's height above the pad |

  The field direction is the pad's local −Y scaled by world gravity magnitude, so rotating the pad rotates the field.
- **Cosmetic curves and UI bindings.** A translucent column shows the volume edges. Slow-drifting motes rise or fall at a speed proportional to `gravity_scale` (an animation loop only). A "fiction" glyph (a stylised star) sits on the pad, and its tooltip states that the field is game fiction.
- **Art (DESIGN.md).** A cream `#fff8e9` pad with a navy `#293954` rim and a gold `#f7cb52` star glyph; a column of pale green `#efffbd` (the selection-ring tone, `DESIGN.md@a6c914e:L156-L156`) at low alpha, kept distinct from real lighting and solid surfaces like the transparent reference walls (`DESIGN.md@a6c914e:L50-L50`).
- **Catalogue and inventory entry.** Id `gravity_pad`, title "Gravity pad (fiction)", category Motion, colour `#f7cb52` (**proposed**).

**Variants.** The requirement row names no variant. Reversed and reduced fields are values of `gravity_scale`.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (EnvironmentState, GeometryQuery, RigidBodyDynamics).

- **Exists now.**
  - Per-body declared gravity vector, admitted to |g| ≤ 16 m/s² (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L83`), compiled per body (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L36-L38`) and integrated each substep (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`).
  - Bounded force regions as a pattern: the planar guide, with acceleration ≤ 12 m/s² (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L144-L161`).
- **Missing.** A volume-qualified gravity override evaluated per substep (the per-body gravity is fixed at compile today): decision owner S635 environment, next LAW-ENVIRONMENT-I ([decisions](../invest/decisions.md#s635)); this element's source D is S659.
- **Dependencies.** None to place it. Integrations: any ball (CAT-001), CAT-003 Balloon, CAT-054 Ramp.

## 4. Sources and legacy

- **Requirement row.** [element-089](../requirements.md#element-089): a bounded authored field changes acceleration inside its declared volume; outside bodies retain world gravity; the field is labelled as deliberate game fiction.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** [S635 environment](../invest/decisions.md#s635): gravity ranges are immutable for a Run and persisted; out-of-range values are refused. Research reference: TIM2 inventory ([todo-216](../requirements.md#todo-216)).
- **Legacy search.** `git grep -i` at a6c914e for "gravity pad", "antigrav" and "gravity scale" found no pad source. Legacy world-gravity facts apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Machine data carried world gravity 9.81 and pressure 1 as authored fields | `engine/MachineData.cs@a6c914e:L170-L171` | Carry forward the world default (also `engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`) |
| L2 | World gravity and pressure could change only while idle (`RequireIdle`) | `reference/cpu/MachineWorld.cs@a6c914e:L185-L189` | Carry forward "fixed for a Run" for the field parameters |
| L3 | Legacy test worlds set gravity 0 and pressure 0 to isolate a behaviour | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L65-L70` | Do not carry forward as a player feature: zero gravity exists only inside this fiction field |

**Files harvested:** `engine/MachineData.cs`, `reference/cpu/MachineWorld.cs` (gravity property only), `CuriousContraptions.tests/PipeTests.cs` (test set-up only).

## 5. Acceptance outline

- **Chrome recipe.** Place the Gravity pad (scale 0.25) and drop one Basketball inside its column and one 2 m beside it from the same height. Run.
- **Positive.** The inside ball falls visibly slower; its first-impact time is about √(2h/(0.25 g)), twice the outside ball's.
- **Negative or control.** The outside ball falls at world gravity exactly as without the pad. Scale −1: the inside ball rises to the volume top, leaves it and falls back under world gravity at the boundary. The fiction label is visible.
- **Boundaries.** A ball whose centre crosses the boundary switches gravity on that substep; scale 1.6 and −1.1 are refused at input; parameters are locked during Run.
- **Run/Reset.** Reset restores all bodies exactly. **Save/Load.** Scale and height round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-216](../requirements.md#todo-216) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) for bodies landing inside the field; the world-gravity environment integration is [sequence-task-551](../requirements.md#sequence-task-551) (two supported gravity environments, owned by EL-124). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" (gravity-effect pad) reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" ("gravity pad/blimp") in the [campaign plan](../requirements.md#campaign-plan). Element integrations: any ball (CAT-001), Balloon (CAT-003), Ramp (CAT-054), EL-098 Pool ball variant B (conditional).

## 6. Open questions

1. Binary membership (proposed) versus blending across the boundary. Unspecified — owner decision (S659).
2. Whether to allow lateral field directions independent of the pad orientation.
3. Energy: a reversed field adds energy to bodies; confirm that the fiction label is sufficient against the "no free energy" ledger ([ENGINE-LEDGER](../invest/scope-corrections.md#engine-ledger)).
