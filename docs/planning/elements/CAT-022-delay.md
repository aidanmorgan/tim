# CAT-022 · delay — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-022 · `delay` |
| Requirement | [CAT-022](../requirements.md#current-cat-022) |
| Mapped identities | none directly. Related timing identities with their own specs: EL-107 Spiral gravity-delay tube (physical delay), EL-181/EL-182 edge detectors, EL-183 Resettable counter. |
| Roadmap | delayed_signal admitted as a current puzzle (`WorkshopPuzzleId.DelayedSignal`); Story 4.2 (countdown hand and indicator cosmetics) |
| Status | delivered; remaining levels in §5 |
| Levels | placed in wind_then_release, saved_for_later; inventory in delayed_signal, delayed_solar; see [CAT-022-I consumers](../invest/current-consumers.md#cat-022-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One static body with one static box: centre (0, −0.7, 0), half (0.675, 0.075, 0.4) `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L127-L128`. |
| Material | restitution 1, bounce threshold 0.1 m/s, friction 0.3 `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`. |
| Constraints and joints | none. |
| Sockets and ports | ActivationIn at local (−0.72, 0, 0); ActivationOut at (0.72, 0, 0) `engine/gpu/WorkshopConnections.cs@a6c914e:L41-L52`. No electrical supply generated. |
| Sensors and activation | Ready → Counting → Finished activation-only timer in the shared discrete network (`engine/gpu/ActivationTimers.cs`, `engine/gpu/DelayedSignal.cs`); busy or finished inputs are ignored until Reset. |
| Work and energy stores | none. |
| Parameters | `delay_seconds`: 0.1–12 s, default 1 s; tick count is the exact ceiling of seconds × cadence (60, 120 or 240 Hz) `engine/gpu/WorkshopDelay.cs@a6c914e:L6-L34`; resource bits `engine/DelayDurationResource.cs@a6c914e:L7-L15`. |
| Cosmetic curves and UI bindings | Timer source, linear, duration from the committed interval `engine/gpu/WorkshopCosmetic.cs@a6c914e:L55-L55`; hand turns one full clockwise revolution (0 → −2π about Z); indicator Ready `#556573`, Counting `#e8b764`, Finished `#f7cb52` `parts/DelayPart.cs@a6c914e:L35-L46`. Design rule `DESIGN.md@a6c914e:L282-L282`. |
| Art | `parts/DelayPart.cs@a6c914e:L13-L34`: ochre box 1.25 × 1.25 × 0.6 and cylinder housing r 0.65; cream `#fff8e9` face r 0.55; navy base 1.35 × 0.15 × 0.8 at y −0.7; twelve navy marks at r 0.46; navy hand 0.055 × 0.38; gold hub; indicator at (0, −0.35, 0.37); gold sockets at x ±0.72; pick radius 0.9. Scene `parts/scenes/delay.tscn`. Palette Delay box `#d69c47` `DESIGN.md@a6c914e:L182-L182`. Icon `ui/WorkshopIcons.cs@a6c914e:L92-L92`. |
| Catalogue and inventory | `parts/catalog/delay.tres@a6c914e:L7-L19`: id `delay`, title Delay, category Control, default 1 s. |
| Goal coupling | ActivatedAfter goal with a minimum delay 0–120 s `engine/gpu/WorkshopPuzzle.cs@a6c914e:L15-L23`. |

Current tests: `CuriousContraptions.tests/WorkshopDelayTests.cs` (exact ceiling, round trip, invalid durations, ports), `DelayedSignalTests.cs`, `ActivationTimerTests.cs`, `WorkshopActivationAnimationTests.cs` (timer segments), `BasketballResourceTests.cs` (delay artwork); Chrome `tools/e2e/anim-1b.test.ts` (fill rises monotonically, lamp lights after, Reset neutralises), `anim-1c.test.ts` (hint while counting).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction, TimedCommand.

| Exists now | Reference |
| --- | --- |
| TimedCommand, SignalPropagation | `engine/gpu/ActivationTimers.cs`, `engine/gpu/ActivationNetwork.cs`, `engine/gpu/WorkshopTimerWire.cs` |
| Timer cosmetics on the animation worker | Story 4.2 |

| Missing | Story that builds it |
| --- | --- |
| none for the delay itself; delayed_solar needs Flashlight/Solar panel/Motor | Stories 13.1, 13.7, 11.1 |
| f32 declarations: `DelayDuration` is stored as binary16 seconds and its tick count is computed from the binary16 exponent and significand bits `engine/gpu/WorkshopDelay.cs@a6c914e:L16-L33`; the minimum goal delay, timer phases and the `DelayDurationResource` `SecondsBits` field are also `Half` | Remaining f32 migration ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)) |

Dependencies: an activation source and target.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The parameter name is `delay_seconds`; an undefined or extra parameter name rejects. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L16-L23` | carry forward the external name at the resource boundary; do not carry forward string-keyed dictionaries in domain logic. |
| 2 | Timer snapshots restore phase, due tick and progress: Finished → Counting (progress 0) → Ready (start and due ticks −1). | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L24-L49` | carry forward. |
| 3 | For 0.1, 1 and 1.13 s: activation starts Counting; repeated activations before the due tick keep the same due tick and the lamp stays dark; at the due tick it Finishes, lights the lamp and records the event at that tick; activation after Finish is ignored; Reset returns Ready, due −1, progress 0. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L50-L91` | carry forward. |
| 4 | Chained 0.1 s and 0.2 s delays light the lamp at tick 36 (120 Hz) regardless of entity order; removing a live input during Run is rejected without changing the world. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L92-L127` | carry forward. |
| 5 | Durations 0, −1, 13 and NaN reject at construction. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L128-L143` | carry forward. |
| 6 | ActivatedAfter minimum delay −1, 121, NaN or +∞ rejects. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L144-L161` | carry forward (current bound 0–120). |
| 7 | delayed_signal and delayed_solar win at precision 0, 0.45 and 1, replay with identical tick counts, fail when any wire is removed, and fail with a direct switch → lamp (or → torch) bypass while the delay still Finishes; the placed delay id differs from the authored solution slot id. | `CuriousContraptions.tests/DelayTests.cs@a6c914e:L162-L224` | carry forward (direct-early-bypass control). |
| 8 | Hand and indicator follow committed progress and phase through failed substeps, pause, hiding and completion; physics never writes the material; Reset and Load restore the Ready palette and baseline hand. | `CuriousContraptions.tests/DelayPresentationTests.cs@a6c914e:L34-L95` | carry forward; do not carry forward the per-part scalar-rotation evaluator (replaced by the declared timer binding, Story 4.2). |
| 9 | Unsupported presentation bindings (unit, slot, owner, range, angle, axis, duplicate, timer owner/slot, duplicate colour) reject at Start without changing the construction. | `CuriousContraptions.tests/DelayPresentationTests.cs@a6c914e:L142-L165` | carry forward the reject-before-mutation rule. |
| 10 | Reference delayed_signal: switch (−3, 1, 0), trigger ball (−3, 4, 0), lamp (3, 1, 0), delay_1 (0, 1, 0); links switch → delay → lamp; goal ActivatedAfter(lamp, switch, 1 s). delayed_solar: delay at (0, 1, 2), yaw 180°, switch → delay → torch, goal PoweredAfter(motor, switch, 1 s). | `tools/Campaign/Program.cs@a6c914e:L248-L277` | carry forward (content in `content/puzzles.json`). |

Files harvested:
- `CuriousContraptions.tests/DelayTests.cs`
- `CuriousContraptions.tests/DelayPresentationTests.cs`
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-022](../requirements.md#current-cat-022) and [retained behaviour](../requirements.md#todo-130).

- **Construction.** delayed_signal: place the Delay, Connect switch → delay and delay → lamp through the connection controls; set the duration through the existing input.
- **Positive.** Lamp lights at the deadline, not before; hand completes one revolution; indicator Ready → Counting → Finished.
- **Negative/control.** Before-deadline check, repeated same-tick input, direct-early-bypass, missing link.
- **Boundaries.** 0.1 s and 12 s; invalid durations reject.
- **Run/Reset, Save/Load.** Reset rearms Ready; duration and links persist through Save/Load.
- **Integrations.** Activation, signal and timed-logic connection audit [sequence-task-279](../requirements.md#sequence-task-279) (delay); [IX-07 signal propagation](../requirements.md#interaction-07); campaign first use 21–40 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (detector, plate, delay and logic row). Partners: Impact switch (CAT-063) → Delay → Signal lamp (CAT-035) in delayed_signal; delayed_solar, wind_then_release and saved_for_later once their companion parts land.
- **Remaining (unmet now).** delayed_solar, wind_then_release and saved_for_later wait for their companion parts.

## 6. Open questions

none.
