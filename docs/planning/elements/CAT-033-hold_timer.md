# CAT-033 · hold_timer — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-033-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-033](../requirements.md#current-cat-033). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-033 · `hold_timer` |
| Requirement anchor | [current-cat-033](../requirements.md#current-cat-033); retained behaviour [todo-261](../requirements.md#todo-261) |
| Mapped identities | none owned. Related: [TH-34 Timed toaster ejector](../invest/named-elements.md#thermal-34) composes a generic timer; its requirement row stands as the source. |
| Roadmap story | 9.5 "Hold Timer Pulse Stretcher" (Epic 9) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root box 1.4 × 1.1 × 0.65 m at the origin; foot box 1.6 × 0.14 × 0.85 m centred at y −0.62 (`parts/HoldTimerPart.cs@a6c914e:L61-L62`; legacy `AddBox` = collision box, [CAT-005 §2](CAT-005-battery.md#2-declaration)). |
| Mass and material | Static. Legacy static default material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`). |
| Constraints and joints | none |
| Typed sockets and ports | `ActivationIn` Activation Input at (0, 0.65, 0.2); `PowerIn` Electrical Input at (−0.78, 0, 0); `Supply` Electrical Output at (0.78, 0, 0) (`parts/HoldTimerPart.cs@a6c914e:L37-L42`). Electrical route `PowerIn`→`Supply` closed while the timer is Counting (`parts/HoldTimerPart.cs@a6c914e:L43-L44`). No activation output. |
| Sensors and activation | Trigger activation starts a window if Ready; a busy trigger is ignored (`parts/HoldTimerPart.cs@a6c914e:L51-L56`). |
| Work and energy stores | none; the timer closes a supplied contact and never creates electricity (`parts/HoldTimerPart.cs@a6c914e:L9-L9`). |
| Parameters | `hold_seconds`: float seconds, range 0.1–12 inclusive, default 2.0 (`parts/HoldTimerPart.cs@a6c914e:L45-L50`, `parts/catalog/hold_timer.tres@a6c914e:L14-L14`). Window ticks = ceil(seconds / tick) (`engine/SceneTimerDeclaration.cs@a6c914e:L12-L18`). Carry forward as f32 seconds; do not copy the binary16 `DelayDuration` encoding (`engine/gpu/WorkshopDelay.cs@a6c914e:L6-L33`; remaining f32 migration per `docs/gpu-f32-physics.md@a6c914e:L96-L96`). |
| Cosmetic curves and UI bindings | (1) Remaining bar: gold bar scales along X from anchor −0.5 with fraction = committed remaining fraction (1 at trigger → 0 at expiry), minimum scale 0.001, visible only while the timer is active (`parts/HoldTimerPart.cs@a6c914e:L25-L32`). (2) Indicator lamp slate `#556573` → gold `#f7cb52`, 0.1 s SmoothStep, follows the window (owner active), independent of supply (`parts/HoldTimerPart.cs@a6c914e:L33-L35`). Current cosmetic `Timer` source carries phase and a 0..1 blend (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L13`), used by Delay. |
| Art | Housing in catalogue colour `#66b8c9` (0.40, 0.72, 0.79); navy `#293954` foot; cream `#fff8e9` face 1.18 × 0.72 × 0.045 at z 0.35; navy track 1 × 0.14 × 0.025 at (0, 0.1, 0.39); gold `#f7cb52` bar 1 × 0.10 × 0.035 at (0, 0.1, 0.415), hidden at rest; three `#e8b764` contact pads r 0.08 at (±0.78, 0, 0) and (0, 0.65, 0.2) — "two contact pads distinguish the hold relay from the round delay clock"; indicator r 0.07 at (0, −0.22, 0.40) (`parts/HoldTimerPart.cs@a6c914e:L58-L72`). Selection ring 0.95 (`parts/HoldTimerPart.cs@a6c914e:L60-L60`). Toolbox icon (current, survives): `ui/WorkshopIcons.cs@a6c914e:L87-L87`. Design: "Hold timer" row of [DESIGN.md · Motion and state feedback](../../../DESIGN.md#motion-and-state-feedback). |
| Catalogue / inventory | Id `hold_timer`, Title "Hold timer", Category Control, colour (0.40, 0.72, 0.79), Parameters `{"hold_seconds": 2.0}`; Description: "An activation closes this electrical contact for two seconds. Connect a battery to its input: the timer does not create power. Busy triggers are ignored; it rearms when time is up." (`parts/catalog/hold_timer.tres@a6c914e:L8-L14`). Inventory 1 in `saved_for_later` (harvest H18). |

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction, TimedCommand (P0-022/023 shared evaluator/feedback registration).

**Exists now**
- Timer node in the activation network: Ready → Counting on the first input (busy input ignored), due = ceiling start tick + duration ticks, then `Finished` (latching) with an elapsed occurrence: `engine/gpu/ActivationTimers.cs@a6c914e:L66-L113`; wired by `engine/gpu/ActivationNetwork.cs@a6c914e:L194-L231`.
- Cadence-to-ticks conversion: `engine/gpu/WorkshopDelay.cs@a6c914e:L16-L33`.
- Timer cosmetic source (phase + blend): `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L13`, used by `parts/DelayPart.cs@a6c914e:L35-L46`.
- Note: these types hold binary16 (`Half`) values — `ActivationTime` phases, `DelayDuration` seconds, the cosmetic blend and durations — listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L96-L98`). Extending them for the hold timer inherits that debt (open question 3).

**Missing**
- A rearming completion policy (Counting → Ready at expiry, so a later trigger starts a new window) and a timer with no elapsed output — Story 9.5. The current timer only latches to `Finished` (`engine/gpu/ActivationTimers.cs@a6c914e:L104-L112`), and the network emits each source at most once per world (`engine/gpu/ActivationNetwork.cs@a6c914e:L206-L208`).
- Window expiry ordered before same-tick delayed occurrences and before the electrical solve (H11) — Story 9.5.
- Supplied contact `PowerIn`→`Supply` closed while Counting — Story 8.1 network (open question 1).
- Remaining-fraction scalar extent binding (bar) — Story 9.5.

**Element dependencies**: an activation source (Switch CAT-063, Delay CAT-022); Battery (CAT-005) and a supplied consumer for any observable output (the legacy tests used Powered gate, CAT-051; [current-consumers](../invest/current-consumers.md#cat-033-i) names Motor, CAT-042); `saved_for_later` also needs Motor (CAT-042) and Wound spring (CAT-071).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| H1 | A self-timed electrical contact, not a power source; busy triggers are ignored. | `parts/HoldTimerPart.cs@a6c914e:L9-L9` | carry forward |
| H2 | Readable state: phase (`Ready`/`Counting`/`Finished`), started tick, due tick, duration, remaining fraction (0 unless Counting). | `parts/HoldTimerPart.cs@a6c914e:L14-L22` | carry forward |
| H3 | Timer declaration: duration seconds, completion `Rearm`, boundary `BeforeNetworks`, elapsed signal `None` (no activation on expiry). | `parts/HoldTimerPart.cs@a6c914e:L23-L24` | carry forward |
| H4 | Bar and lamp bindings as in section 2. | `parts/HoldTimerPart.cs@a6c914e:L25-L35` | carry forward |
| H5 | Ports and contact route as in section 2. | `parts/HoldTimerPart.cs@a6c914e:L36-L44` | carry forward |
| H6 | Duration must be finite and within 0.1–12 s. | `parts/HoldTimerPart.cs@a6c914e:L45-L50` | carry forward |
| H7 | Only `Trigger` is accepted; a started window takes effect immediately, a busy trigger is deferred (no effect). The part is active exactly while Counting, evaluated before the network solve. | `parts/HoldTimerPart.cs@a6c914e:L51-L57` | carry forward the rule; do not carry forward the per-part `HandleActivation`/`BeforeNetworks` callbacks |
| H8 | Geometry and art as in section 2. | `parts/HoldTimerPart.cs@a6c914e:L58-L72` | carry forward |
| H9 | Timer phases, quantities (`ProgressFraction`, `RemainingFraction`), completion policies (`Latch`, `Rearm`) and boundaries (`BeforeNetworks`, `BeforePhysics`) are closed enums. | `engine/SimulationTimers.cs@a6c914e:L16-L19` | carry forward as enums |
| H10 | Trigger when Ready: Counting, started = tick, due = tick + duration ticks; trigger when not Ready returns false and changes nothing. Advance: each due timer on this boundary completes (`Rearm` → Ready, `Latch` → Finished) and emits once with its actual deadline, in identity order; skipped ticks drop nothing. Remaining = 1 − progress while Counting, else 0. | `engine/SimulationTimers.cs@a6c914e:L71-L128` | carry forward |
| H11 | A rearming hold window (duration 3, triggered at 0) expires at the `BeforeNetworks` boundary of tick 3, before a same-tick delay elapses at `BeforePhysics`; re-triggered at 3 it is due at 6. A busy trigger at tick 1 is refused. | `CuriousContraptions.tests/SimulationTimersTests.cs@a6c914e:L50-L71` | carry forward |
| H12 | Parameter wire name `hold_seconds`; remaining is 1 at trigger and 0 after expiry; Ready has due −1. | `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L29-L51` and `engine/MachineData.cs@a6c914e:L88-L88` | carry forward |
| H13 | Fixture: battery → hold timer → Powered gate load (`powered_gate`, CAT-051; `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L19-L19`). For 0.1, 1.13 and 2 s in either entity order: the Powered gate is powered on every tick before the due tick; re-triggering every tick never moves the due tick; one step at the due tick opens the contact and returns to Ready; a new trigger then starts a later window; Reset gives Ready, due −1, started −1, remaining 0, authored duration. | `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L52-L101` | carry forward |
| H14 | Supply loss does not pause the countdown or create supply; restoring power after expiry does not replay a spent trigger. | `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L103-L133` | carry forward |
| H15 | Delay (0.1 s) → hold timer (0.5 s): the hold starts on the tick the delay elapses and the downstream Powered gate (`powered_gate`, CAT-051) starts opening, then closes again after the window; delay→hold resolves to Activation; hold→delay is refused. | `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L135-L160` | carry forward |
| H16 | Switch→hold timer offers two compatible pairs (Activation and Electrical), so no suggestion; the explicit Activation link survives save/load with `ActivationIn`. | `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L162-L183` | carry forward |
| H17 | Durations 0, −1, 13, NaN and +∞ are rejected and the part is not added. | `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L185-L202` | carry forward |
| H18 | `saved_for_later` (60 / Stored energy): inventory hold_timer 1; locked battery (−4, 3, 2), motor (−3, 5, 2), switch trigger (4, 2, −2) with striker ball above, delay (−3, 6, −2), 3 s release delay (3, 6, 2), wound spring launcher (0, 3, 0) and basket; solution hold timer at (−5, 6, 0). All seven solution links: battery→hold timer (Electrical `supply`→`power_in`); hold timer→motor (Electrical `supply`→`power_in`); motor→launcher (Mechanical `drive`→`drive_in`); trigger→delay (Activation); delay→hold timer (Activation); delay→release delay (Activation); release delay→launcher (Activation). Goal: payload captured. Hint: "Put a hold timer between battery and motor … Watch the motor stop before the latch opens." | `content/puzzles.json@a6c914e:L31861-L32564` and `tools/Campaign/WoundSpringLesson.cs@a6c914e:L84-L108` | carry forward (Epic 15 input; also needs Motor and Wound spring) |
| H19 | The bar stays hidden until a committed window; the lamp shows the window whether or not the contact is supplied; a failed tick leaves bar and lamp unchanged; a hidden part catches up when shown. | `CuriousContraptions.tests/HoldTimerPresentationTests.cs@a6c914e:L40-L80` | carry forward behaviour; do not carry forward the CPU transaction mechanism |
| H20 | The contact follows committed timer state (Counting), not the transient active flag, and reads open after Reset. | `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L38-L83` | carry forward |
| H21 | Timer diagnostic wire names (`ready`/`counting`/`finished`) and unknown-policy rejection. | `CuriousContraptions.tests/TimerDiagnosticTests.cs@a6c914e:L60-L91` | carry forward at the diagnostics boundary only |
| H22 | Timer observation, committed publication and allocation tests. | `CuriousContraptions.tests/TimerObservationTests.cs@a6c914e:L65-L173` and `CuriousContraptions.tests/CommittedTimerTests.cs@a6c914e:L26-L164` | do not carry forward (CPU publication mechanism; allocation proof belongs to the Epic 16 gate) |
| H23 | Catalogue entry and scene. | `parts/catalog/hold_timer.tres@a6c914e:L1-L14` and `parts/scenes/hold_timer.tscn@a6c914e:L1-L6` | carry forward the data |

`reference/P0-022-before/docs/coverage/engine/*.json` list `hold_timer` only as a catalogue id: no element knowledge.

### Files harvested

- `parts/HoldTimerPart.cs`
- `parts/catalog/hold_timer.tres`
- `parts/scenes/hold_timer.tscn`
- `engine/SceneTimerDeclaration.cs`
- `engine/SimulationTimers.cs`
- `engine/MachineData.cs`
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/HoldTimerTests.cs`
- `CuriousContraptions.tests/SimulationTimersTests.cs`
- `CuriousContraptions.tests/HoldTimerPresentationTests.cs`
- `CuriousContraptions.tests/ElectricalContactBindingTests.cs`
- `CuriousContraptions.tests/TimerDiagnosticTests.cs`
- `CuriousContraptions.tests/TimerObservationTests.cs`
- `CuriousContraptions.tests/CommittedTimerTests.cs`
- `content/puzzles.json` (`saved_for_later`; the file survives)
- `tools/Campaign/WoundSpringLesson.cs`

## 5. Acceptance outline

Authority: [CAT-033](../requirements.md#current-cat-033), [todo-261](../requirements.md#todo-261); Story 9.5 adds `tools/e2e/cat-033.test.ts`.

- **Construction (Chrome UI).** Place Switch, Hold timer (2.0 s default), Battery and a supplied consumer; Connect switch→timer choosing the trigger icon (H16), battery→timer `PowerIn`, timer `Supply`→consumer.
- **Positive.** A momentary impact closes the contact on the trigger tick and holds it for exactly the declared window (2.0 s), then opens; the bar drains leftward and the lamp returns to slate.
- **Negative / controls.** Re-triggering during the window does not extend it (Story 9.5 AC; H13); no supply → consumer stays off while the lamp still shows the window (H19); restoring supply after expiry does not replay (H14).
- **Boundaries.** 0.1 s and 12 s; out-of-range durations rejected (H17); a trigger exactly at expiry starts a new window (H11).
- **Run/Reset.** Reset gives Ready, hidden bar, slate lamp and no pending trigger.
- **Save/Load.** `hold_seconds` and the explicit Activation link round-trip (H16).
- **Integrations.** Delay→Hold (H15); `saved_for_later` once Motor and Wound spring exist (H18).

## 6. Open questions

1. **Output domain in Story 9.5.** The Story 9.5 AC says "the output activates … for exactly 2.0 simulated seconds", but the legacy and the requirement give the hold timer only a supplied electrical contact (no activation output); the supply network (Story 8.1) now precedes Story 9.5. Whether 9.5 proves the output through the Story 8.1 supplied contact, or adds an output the requirement does not define: unspecified — owner decision.
2. **"Pending event state".** The requirement says "display and pending event state Reset exactly", but the legacy hold timer queues no events (elapsed signal `None`, busy triggers dropped). Which pending state the requirement means: unspecified — owner decision.
3. **binary16 activation and cosmetic lanes.** The timer node, delay duration and cosmetic curve the hold timer extends still hold binary16 values (`docs/gpu-f32-physics.md@a6c914e:L96-L98`, remaining f32 migration). Whether Story 9.5 migrates those lanes to f32 before extending them, or extends them as they are and leaves migration to the scheduled f32 work: unspecified — owner decision.
