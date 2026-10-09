# EL-086 · Obstacle-reversing walker — named-identity spec

Story 7.0 named-identity spec ([story spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values with no legacy or requirement source are **proposed**, each with a one-line justification; the owner may revise them. Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#f32-migration-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-086 · Obstacle-reversing walker · Character |
| Requirement anchor | [element-086](../requirements.md#element-086); scope index [todo-221](../requirements.md#todo-221) |
| Named entry | [element-086](../invest/named-elements.md#element-086); source owner **S656** |
| CAT spec refined or extended | None |
| Related identities | EL-082 Lured, EL-083 Escaping, EL-087 Predator (the same controller law; [EL-082 spec](EL-082-lured-character.md) holds the shared framework), GAP-04 Driven wheel |
| Roadmap story | Unscheduled (aggregate [LAW-CONTROLLER-I](../invest/scope-corrections.md#law-controller-i)) |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

**Declaration-only rule.** A contact observation, a shared two-state policy and a locomotion actuator. Reversal is triggered by an actual contact, never by distance lookahead or a timer, and there is no element update loop ([engine contracts](../../engine-contracts.md); `docs/general-engine-design.md@a6c914e:L163-L163`).

- **Bodies and shapes.** One dynamic upright box 0.6 × 0.5 × 0.35 m, half extents (0.3, 0.25, 0.175) (**proposed**: a low wind-up toy shape whose two ends both sense). Its bottom face is the foot and its two end faces (±X) are the bumpers; all are faces of the single box, not separate colliders, so no dynamic compound body is needed.
- **Mass and material.** 1.0 kg, friction 0.8, restitution 0.1, threshold 0.1 m/s (**proposed**: light enough for a Domino or Wall to stop it, grippy feet).
- **Constraints.** An upright constraint locks tipping about local X and Z (**proposed**, as EL-082). Yaw is also locked, so it walks along its authored local X axis (**proposed**: a walker reverses rather than steers).
- **Typed ports.** None.
- **Sensors and activation (obstruction sensing).** An obstruction is a contact on the leading bumper face whose normal opposes the walking direction within 45° (**proposed**: a head-on or angled push counts, a side graze does not), with a normal impulse in one tick ≥ `obstruction_impulse` (**proposed**: a real push, not a graze). A distant obstacle produces no contact and no reversal. Ground contacts under the foot never count.
- **Policy (shared ProgrammableController data).** States `WalkPositive` and `WalkNegative` along local X. A qualifying obstruction toggles the state once; a 0.3 s refractory interval follows (**proposed**: prevents chattering against the same wall). There is no edge detection: it walks off ledges and falls physically (**proposed**: the row asks only for obstruction reversal).
- **Work and energy stores.** A finite locomotion store of 120 J (**proposed**: about 25 s of full drive at 4.8 W, that is 6 N at 0.8 m/s, inside the 30 s attempt limit; the EL-082 figure of 200 J would last about 42 s at this lower power).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Status |
  | --- | --- | --- | --- | --- | --- |
  | `walk_speed` | f32 | 0.2–2 | 0.8 | m/s | **proposed**: slower than the lured character, so its motion is easy to follow |
  | `drive_force` | f32 | 1–20 | 6 | N | **proposed**: cannot push a 2 kg box far, so obstacles reverse it rather than being shoved |
  | `obstruction_impulse` | f32 | 0.05–2 | 0.2 | N·s | **proposed**: a stopping Wall or Domino exceeds it; a rolling Tennis ball brushing past does not |
  | `initial_direction` | enum `WalkDirection` | Positive, Negative | Positive | — | **proposed** |

- **Cosmetic curves and UI bindings.** The leg cycle follows the committed speed. On each reversal, a turn cue rotates the head shell 180° as an animation (the physical yaw stays locked; the body is symmetric).
- **Art (DESIGN.md).** An original wind-up toy: an ochre `#d69c47` rounded body, cream `#fff8e9` face plates on both ends, navy `#293954` eyes on the active end and a gold `#f7cb52` key on top. Matte and chamfered (`DESIGN.md@a6c914e:L48-L53`).
- **Catalogue and inventory entry.** Id `walker`, title "Wind-up walker", category Characters, colour `#d69c47` (**proposed**).

**Variants.** The requirement row names no variant.

## 3. Engine capabilities

Families: the [element-map row](../general-engine-element-map.md) and the binding in `docs/coverage/engine/element-01.json` (ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, StateTransaction, TimedCommand, TypedContracts).

- **Exists now.**
  - Contact manifolds with normal impulses in the solver (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774-L774`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L877-L877`).
  - Contact triggers (approach-speed threshold), but only on static owners (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1155-L1177`; `engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L8-L20`).
- **Missing.**
  - A contact observation on a dynamic owner filtered by face and normal: owner S656.
  - ProgrammableController: S635 controller, next LAW-CONTROLLER-I.
  - Upright and yaw lock, and contact-actuated locomotion: Epic 10 joints.
  - Finite store: the ENGINE-LEDGER law.
- **Dependencies.** CAT-066 Wall and CAT-023 Domino as obstacles.

## 4. Sources and legacy

- **Requirement row.** [element-086](../requirements.md#element-086): locomotion reverses after sensed actual obstruction; a distant obstacle does not reverse it early.
- **Shared contracts.** The row's closing clause applies the shared visual, generic-interaction, campaign and per-element proof contracts of the [individual element register](../requirements.md#individual-element-register).
- **Decisions.** [S635 controller](../invest/decisions.md#s635): a sensed target produces the declared action; missing supply or an unsupported command produces none; nothing teleports. Research reference: TIM2 manual ([todo-221](../requirements.md#todo-221)).
- **Legacy search.** `git grep -i` at a6c914e for "walker", "reverse", "obstacle" and "character" over every Epic 7 deletion path found no walker source. The reverse-transmission hits (CAT-057) concern shaft direction, not locomotion.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Impact-switch threshold default 0.8 m/s approach speed, typed and range-checked 0–64 | `engine/gpu/WorkshopActivationParts.cs@a6c914e:L6-L10` (current) | Carry forward the typed-threshold pattern for `obstruction_impulse` |

**Files harvested:** none deleted (only a surviving file is cited).

## 5. Acceptance outline

- **Chrome recipe.** Place the Walker mid-bench facing +X, a Wall 2 m to its right and a Wall 2 m to its left, using the move gizmo. Run.
- **Positive.** It walks right, touches the right Wall, reverses, walks left, touches the left Wall and reverses again.
- **Negative or control.** A Wall 0.5 m to the side of its path, or a Wall far ahead that it never reaches, never reverses it. A Tennis ball rolling across its path without a frontal push does not reverse it. It reverses only at contact, not before.
- **Boundaries.** Reversal happens within one tick of the first qualifying contact; no second reversal inside the refractory interval; it falls off the bench edge rather than turning.
- **Run/Reset.** Reset restores the pose, direction and store exactly. **Save/Load.** Parameters round-trip.
- **Integrations.** Requirement-row integration tasks: the [todo-221](../requirements.md#todo-221) scope index routes shared interactions to the [generic process register](../requirements.md#generic-interaction-register): [IX-01 contact impulse](../requirements.md#interaction-01), [IX-02 sliding friction](../requirements.md#interaction-02) and [IX-03 joint constraint](../requirements.md#interaction-03) (upright and yaw lock). Candidate evaluation: [todo-322](../requirements.md#todo-322) (distinct decision and teaching example) and [todo-228](../requirements.md#todo-228) (behavioural reference fixture before required use). Campaign first use: the [element coverage ledger](../requirements.md#campaign-element-coverage) row beginning "Original cat/mouse lure/escape roles" (walker) reserves levels 91–100 (reuse 136–150); chapter 10 "Oddly Satisfying" in the [campaign plan](../requirements.md#campaign-plan). Element integrations: Wall (CAT-066) and Domino (CAT-023) as obstacles.

## 6. Open questions

1. Edge behaviour: walk off (proposed) or reverse at drops. Unspecified — owner decision (S656).
2. Whether the walker may push light objects before reversing (needs the threshold confirmed).
3. A locked yaw (proposed) versus a turning walker.
