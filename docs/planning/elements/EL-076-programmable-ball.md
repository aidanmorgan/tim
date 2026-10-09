# EL-076 · Programmable ball — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-076 · Programmable ball · Specialist |
| Requirement anchor | [element-076](../requirements.md#element-076); scope index [todo-217](../requirements.md#todo-217) |
| Named entry | [element-076](../invest/named-elements.md#element-076); source owner **S646** ([S646 decision](../invest/scope-corrections.md#s646)) |
| CAT spec refined or extended | Extends the ball family: [CAT-001](CAT-001-ball.md), [CAT-014](CAT-014-bowling.md), [CAT-064](CAT-064-tennis.md) |
| Related identities | EL-077 Soccer ball, EL-080 Programmable box (the same preset-admission pattern), EL-189 Tennis ball |
| Roadmap story | Unscheduled |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The S646 scope correction makes this **preset admission, not a controller**: the author picks typed presets and the ball has no program, port or update loop ([scope corrections](../invest/scope-corrections.md#law-controller-i)).

- **Bodies and shapes.** One dynamic sphere: a `WorkshopBall` whose `BallMaterial` is built from the selected presets (`engine/gpu/WorkshopConstruction.cs@a6c914e:L38-L77`), compiled like every ball (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`).
- **Mass and material.** The material comes from three closed enums, chosen in build mode:

  | Enum | Members and values | Status |
  | --- | --- | --- |
  | `BallSizePreset` | Small r 0.25 m · Medium r 0.34 m · Large r 0.45 m | **proposed**: Small and Medium reuse the Tennis and Basketball radii; Large still clears the 0.65 m pipe bore radius (`engine/gpu/WorkshopPipe.cs@a6c914e:L9-L9`) |
  | `BallMassPreset` | Light 0.35 kg · Standard 1 kg · Heavy 4 kg | **proposed**: the three catalogue masses (Tennis, Basketball, Bowling), so lessons transfer |
  | `BallSurfacePreset` | Lively (bounce 0.78, rolling 0.035) · Standard (0.55, 0.035) · Dead (0.14, 0.03) | **proposed**: Tennis, Basketball and Bowling bounce and rolling values (`parts/catalog/tennis.tres@a6c914e:L14-L14`; `engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L51`) |

  Shared fields: drag 0.04 1/s, buoyancy 0, friction 0.3, bounce threshold 0.1 m/s (the shared ball declaration, `engine/gpu/WorkshopConstruction.cs@a6c914e:L43-L51`). All values fall inside the admission bounds (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`).
- **Unsupported combinations (proposed).** Light with Large is rejected: it reads as a balloon but has no buoyancy. Heavy with Lively is rejected: a 4 kg body at bounce 0.78 would swamp contact work and blur the heavy-ball lesson. Each rejected pair excludes 3 of the 27 combinations (one per value of the third enum), so 21 of the 27 are admitted. Rejection happens at input, atomically, before any state changes.
- **Constraints.** None; a free body.
- **Typed ports.** None. The binding's ProgrammableController, SignalPropagation and TimedCommand families are inherited labels; S646 excludes a controller.
- **Sensors and activation.** None owned. The ball is a payload for Receiver capture, Switch impact and Pressure plate load.
- **Work and energy stores.** None.
- **Parameters.** The three enums above. Defaults Medium, Standard, Standard (**proposed**: the default reproduces the Basketball's behaviour). They are editable only in build mode through contextual icon choices. There is no numeric field and no runtime editor (requirement row).
- **Cosmetic curves and UI bindings.** None physical. The preset choices are shown on the ball: a size-proportional mesh, a band count for mass (1, 2 or 3 cream bands), and a surface pattern (dots, stripe or plain) for the bounce class, so state does not depend on colour alone ([common visual contract](../requirements.md#individual-element-register)).
- **Art.** Sphere with a cream `#fff8e9` band ring over a gold `#f7cb52` base (**proposed**: gold is the operational-cue colour and is not yet a ball identity), navy `#293954` pattern ink (`DESIGN.md@a6c914e:L147-L147`, `DESIGN.md@a6c914e:L189-L189`). Satin material (`DESIGN.md@a6c914e:L193-L193`).
- **Catalogue and inventory entry.** Id `programmable_ball`, title "Programmable ball", category Motion, colour `#f7cb52`, counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`) (**proposed**).

**Variants.** The requirement row names no variant. The 21 admitted preset combinations are parameter values of one element, not variants.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.** Dynamic sphere, contact, friction, rolling resistance and drag (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L722-L754`). Kind-fixed material admission (`engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`). TypedContracts through enums.
- **Missing.** Preset-keyed material admission in place of kind-keyed admission (`BallMaterial.For(kind)` admits only fixed kinds): decision owner S646. Contextual preset-choice UI: S646-I. Wire and save fields for three enums: S646-I. ProgrammableController is not required (scope correction above).
- **Dependencies.** None to place it. Integrations: Receiver (CAT-004), Pressure plate (CAT-052), Pipe (CAT-048) for the Large bore clearance.

## 4. Sources and legacy

- **Requirement row.** [element-076](../requirements.md#element-076): unsupported combinations are rejected; no runtime numeric solution editor.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** [S646](../invest/scope-corrections.md#s646): enumerate the allowed typed material values; one accepted preset behaves as declared, one invalid combination is refused at input. Research reference: TIM2 inventory ([todo-217](../requirements.md#todo-217)).
- **Legacy search.** `git grep -i` at a6c914e for "programmable" over `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` found no programmable-ball source. The ball facts below apply.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Legacy ball parameter set was an enum: Radius, Mass, Bounce, Buoyancy, Drag; solid-sphere dynamics | `reference/cpu/BallPart.cs@a6c914e:L4-L13` | Carry forward the field set. Do not carry forward free numeric values (row forbids a numeric editor). |
| L2 | Every catalogue ball must carry every parameter; a missing one rejects; Run/Restore round-trips the snapshot | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` | Carry forward as "incomplete or unknown preset rejects". Do not carry forward the string-keyed dictionary. |
| L3 | Isolated drop: first impact at √(2h/g) ± 0.02 s and rebound ratio bounce² ± 0.015; replay after Restore identical | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L67` | Carry forward as the per-preset acceptance |
| L4 | A dropped ball bounces, never sinks more than 1 mm below the bench, and settles below 0.05 m/s within 2400 ticks | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L48` | Carry forward |
| L5 | Legacy machines could override one ball's radius per instance (`Properties["radius"]`) | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L65-L70` | Do not carry forward: replaced by the closed size preset |
| L6 | Pressure plate default minimum 0.5 kg; one tennis ball (0.35 kg) does not press, one Basketball does | `parts/catalog/pressure_plate.tres@a6c914e:L11-L14`; `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L38-L62` | Carry forward as the Light versus Standard control |

**Files harvested:** `reference/cpu/BallPart.cs`, `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`, `CuriousContraptions.tests/PipeTests.cs`, `CuriousContraptions.tests/PressurePlateTests.cs`, `parts/catalog/pressure_plate.tres`, `parts/catalog/tennis.tres`.

## 5. Acceptance outline

- **Chrome recipe.** Place two Programmable balls on separate lanes with the move gizmo, as `tools/e2e/cat-014.test.ts` does for the Bowling ball. Set one to Small/Light/Lively and one to Large/Heavy/Dead through the contextual preset icons. Run.
- **Positive.** Each rests at its own radius; the Lively ball rebounds to about 0.78² of its fall height, the Dead ball to about 0.14².
- **Negative or control.** Selecting Light with Large, or Heavy with Lively, is refused at input and the construction is unchanged. No numeric field is offered. A Light ball on a Pressure plate does not press it; a Standard ball does.
- **Boundaries.** Rest on the bench within 1 mm; rebound ratio within ± 0.015 of bounce²; the Large ball passes a Pipe and the rejected sizes never appear.
- **Run/Reset.** Presets are locked during Run; Reset restores the pose exactly. **Save/Load.** The three enums survive reload and the behaviour repeats; an unknown enum value in a save is rejected atomically.
- **Integrations.** Requirement-row integration tasks: the [todo-217](../requirements.md#todo-217) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-02 sliding friction](../requirements.md#interaction-02). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row "Basketball, bowling, tennis, baseball, soccer and programmable physical presets" reserves levels 1–10, with preset-specific lessons allowed in 91–100 (reuse 21–30, 41–50, 91–100, 136–150); chapter 1 "On a Roll" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Receiver capture (CAT-004), Pressure plate load (CAT-052), Pipe bore clearance (CAT-048).

## 6. Open questions

1. Preset membership and values: confirm the 3 × 3 × 3 set and the two rejected combinations. Unspecified — owner decision (S646).
2. Whether the default preset may exactly duplicate the Basketball, or must differ to avoid a cosmetic duplicate (see the S636 Baseball rule, [scope corrections](../invest/scope-corrections.md#s636)).
3. Whether a buoyancy preset is wanted later (it would need Story 12.1 buoyancy).
