# CAT-035 · lamp (Signal lamp) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-035 · `lamp` (display name Signal lamp) |
| Requirement | [CAT-035](../requirements.md#current-cat-035) |
| Mapped identities | none: no EL/TH/RAD/GAP identity is the signal lamp. TH-36 Lamp-trigger apparatus (Batch N) concerns a historical light/heat source and is a different element. |
| Roadmap | [CAT-063-I and CAT-035-I](../invest/vertical-delivery.md#switch-lamp); Story 4.1 (lamp glow on the animation worker) |
| Status | delivered; remaining levels in §5 |
| Levels | placed in 18 levels; see [CAT-035-I consumers](../invest/current-consumers.md#cat-035-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One static body with one static box: centre (0, −0.35, 0), half (0.425, 0.1, 0.425) `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L129-L129`. The bulb is art only. |
| Material | restitution 1, bounce threshold 0.1 m/s, friction 0.3 `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`. |
| Constraints and joints | none. |
| Sockets and ports | ActivationIn (Activation, Input) at local origin `engine/gpu/WorkshopConnections.cs@a6c914e:L36-L39`, `engine/gpu/WorkshopConnections.cs@a6c914e:L47-L55`. No electrical PowerIn (the requirement forbids fabricating one). |
| Sensors and activation | Latches active on the first committed activation until Reset (shared activation network `engine/gpu/ActivationNetwork.cs`). |
| Work and energy stores | none. |
| Parameters | none `engine/gpu/WorkshopActivationParts.cs@a6c914e:L24-L34`. |
| Cosmetic curves and UI bindings | Activation source, smoothstep, 0.16 s `engine/gpu/WorkshopCosmetic.cs@a6c914e:L54-L54`; bulb albedo `#556573` → `#fff0a5`, emission black → `#e9b24c` `parts/LampPart.cs@a6c914e:L14-L24`. Design rule (slate to cream-gold with emission) `DESIGN.md@a6c914e:L277-L277`. |
| Art | `parts/LampPart.cs@a6c914e:L9-L25`: navy `#334856` base 0.85 × 0.2 × 0.85 at y −0.35, grey `#c7c7bb` stem r 0.18 h 0.3 at y −0.17, bulb sphere r 0.43 at y 0.22; pick radius 0.65. Scene `parts/scenes/goal_light.tscn` (catalogue scene name differs from the id). Palette Lamp `#fadb82` `DESIGN.md@a6c914e:L175-L175`. Icon `ui/WorkshopIcons.cs@a6c914e:L54-L54`. |
| Catalogue and inventory | `parts/catalog/lamp.tres@a6c914e:L6-L14`: id `lamp`, title Signal lamp, category Goals. Goal kind Activated / ActivatedAfter targets the lamp. |

Current tests: `CuriousContraptions.tests/ContactActivationTests.cs` (disconnected and absent contact cannot light it), `DelayedSignalTests.cs`, `ActivationTimerTests.cs`, `WorkshopActivationAnimationTests.cs`, `DominoEffectTests.cs`; Chrome `tools/e2e/anim-1a.test.ts` (glow via worker), `anim-1b.test.ts`, `cat-023b.test.ts` (domino chain lights the lamp once).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, ObjectiveEvaluation, RigidBodyDynamics, SlidingFriction. The map's decision column: shared supplied state-to-colour bindings, no lamp-specific animation loop.

| Exists now | Reference |
| --- | --- |
| Activation latch and goal | `engine/gpu/ActivationNetwork.cs`, `engine/gpu/WorkshopGoalEvaluator.cs` |
| Declared colour/emission bindings on the animation worker | Story 4.1; `engine/gpu/WorkshopCosmetic.cs` |

| Missing | Story that builds it |
| --- | --- |
| ElectricalPower membership: not used by the current activation contract. Any electrically powered light is a separate element (e.g. EL-155, TH-36). | none for CAT-035 |
| f32 declarations: the base box, pose and the cosmetic duration and colour binding values are still binary16 (`Half`) | Remaining f32 migration ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)) |

Dependencies: an activation source (Impact switch, Delay, Domino, Ball detector, Counter and others).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A switch → lamp activation link lights the lamp; Reset clears it; a duplicate link, a foreign part, or an undefined socket is rejected and never lights it. | `CuriousContraptions.tests/ConnectionPortTests.cs@a6c914e:L139-L185` | carry forward. |
| 2 | Wired after a Delay, the lamp stays dark on every tick before the due tick and lights exactly at it; the Activated event records the due tick; Reset clears it. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L50-L90` | carry forward. |
| 3 | A Delay cannot connect to a motor: commands never replace electrical supply. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L63-L64` | carry forward the domain separation (lamp is activation-only). |
| 4 | A counter → lamp chain lights only when the counter reaches its target (three detector crossings), not at two. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L110-L152` | carry forward as an integration control (counter owned by CAT-020). |
| 5 | Signal modules put the lamp at (4.5, 1, 0) with goal Activated(lamp); counterweight puts it at (5, 1, 0.12). | `tools/Campaign/Program.cs@a6c914e:L53-L63`; `tools/Campaign/Program.cs@a6c914e:L189-L207` | carry forward (content in `content/puzzles.json`). |

Files harvested:
- `CuriousContraptions.tests/ConnectionPortTests.cs`
- `CuriousContraptions.tests/DelayTests.cs`
- `CuriousContraptions.tests/CounterTests.cs`
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-035](../requirements.md#current-cat-035).

- **Construction.** Place Signal lamp and a source (Impact switch); Connect source ActivationOut → lamp ActivationIn; Run.
- **Positive.** Committed activation lights the bulb with emission; latched until Reset.
- **Negative/control.** Missed impact, disconnected wire, wrong domain (no electrical input exists), duplicate triggers produce no extra events.
- **Run/Reset, Save/Load.** Bulb and emission clear after a failed Run and on Reset; connections persist through Save/Load.
- **Integrations.** Activation, signal and timed-logic connection audit [sequence-task-279](../requirements.md#sequence-task-279) (ActivationIn from a switch, detector or delay); [IX-07 signal propagation](../requirements.md#interaction-07); campaign first use 51–60 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (optical sources row: lamp). The lamp is also listed among electrical consumers ([sequence-task-278](../requirements.md#sequence-task-278)) and optical sources ([sequence-task-281](../requirements.md#sequence-task-281)); neither is its current activation contract. Partners: Impact switch (CAT-063), Delay (CAT-022), Domino (CAT-023).
- **Remaining (unmet now).** power_trip, spring_signal and the other lamp levels wait for their companion parts.

## 6. Open questions

none.
