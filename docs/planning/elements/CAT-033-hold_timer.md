# CAT-033 · hold_timer — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-033 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-033-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-033](../requirements.md#current-cat-033). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

**Colliders (owner, 25 October 2026, "visible = solid"):** every part a player sees as solid collides with its true shape. Since [`elements-visible-solid-sweep`](../../../TODO.md) the parts that stand proud of the housing collide with their drawn primitives, after the housing and foot boxes: the cream face panel as a 1.18 × 0.72 × 0.045 box at z 0.35, the indicator as a sphere r 0.07 at (0, −0.22, 0.4), and the three ports as spheres r 0.08 at (0, 0.65, 0.2) and (∓0.78, 0, 0) (`elements/HoldTimer/hold_timer.tscn`; `VisibleSolidColliderTests` checks them against the art and drops a ball onto the face; `HoldTimerTests`). The remaining-time bar is scaled by the timer and does not collide. Placement bounds every collider (`PlacementPose`), so the ports now count toward the bench edge: the timer reaches x ±0.86 against the foot's ±0.8, and y +0.73 (the trigger port) against the body's +0.55. The static navy track is flat printing at z 0.37275, 0.25 mm above the face, without a collider; the animated gold remaining bar and its anchoring are retained. The one lesson that uses the hold timer, `saved_for_later` (level 60), is Playable, and its authored solution runs natively (`WoundSpringIntegrationTests`, `LaunchLessonSolveTests`).

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-033 · `hold_timer` |
| Requirement anchor | [current-cat-033](../requirements.md#current-cat-033); retained behaviour [todo-261](../requirements.md#todo-261) |
| Mapped identities | none owned. Related: [TH-34 Timed toaster ejector](../invest/named-elements.md#thermal-34) composes a generic timer; its requirement row stands as the source. |
| Roadmap story | 9.5 "Hold Timer Pulse Stretcher" (Epic 9) Current [CAT-033 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | **Delivered** (22 November 2026 check): package `elements/HoldTimer`, id `hold_timer`; Story 9.5 done on the declared timer of Story 9.5a (`sprint-status.yaml:245–251`); browser qualification pending post-catalogue. Residuals: [section 7](#7-delivered-state-integration-and-residuals-22-november-2026). Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the part needs no specific engine story of its own. |

## 2. Declaration

The delivered declaration is `elements/HoldTimer/HoldTimerElementDeclaration.cs` with the scene `elements/HoldTimer/hold_timer.tscn` (read on `8d1d68aa`). It implements `ISignalElement`, `ISupplyElement` and `ISceneGeometryElement` (`:10`). The legacy sources of the carried values are in section 4.

**Body and material.** One static body with no mass and no joint (`:22`). Material: restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:31`), all f32.

**Colliders (physics graph).** Seven, baked in this order from the hidden scene proxies `col_Body` to `col_PortOutput`. Physics reads no art.

| # | Shape | Size (m) | Centre (m) |
|---|---|---|---|
| 0 | box | 1.4 × 1.1 × 0.65 | origin |
| 1 | box | 1.6 × 0.14 × 0.85 | (0, −0.62, 0) |
| 2 | box | 1.18 × 0.72 × 0.045 | (0, 0, 0.35) |
| 3 | sphere | r 0.07 | (0, −0.22, 0.4) |
| 4–6 | sphere | r 0.08 | (0, 0.65, 0.2), (−0.78, 0, 0), (0.78, 0, 0) |

Placement bounds every collider, so the timer reaches x ±0.86 and y +0.73 at the bench edge.

**Visuals (rendering graph).** No art mesh is used as a collider.

| Art node | Mesh and colour | Solidity | Moved by |
|---|---|---|---|
| `Visual/Body` | box 1.4 × 1.1 × 0.65 m, teal `#66b8c9` | solid, collider 0 | nothing |
| `Visual/Foot` | box 1.6 × 0.14 × 0.85 m, navy `#293954` | solid, collider 1 | nothing |
| `Visual/Face` | box 1.18 × 0.72 × 0.045 m, cream `#fff8e9` | solid, collider 2 | nothing |
| `Visual/Indicator` | sphere r 0.07 m, slate to gold | solid, collider 3 | nothing (colour binding only) |
| `Visual/PortTrigger`, `PortInput`, `PortOutput` | spheres r 0.08 m, ochre `#e8b764` | solid, colliders 4–6 | nothing |
| `Visual/Track` | `QuadMesh` 1 × 0.14 m at (0, 0.1, 0.37275), navy (`hold_timer.tscn:34–36`) | decorative: printed flat, 0.25 mm in front of the face | nothing |
| `Visual/Remaining/Bar` | box 1 × 0.10 × 0.035 m, gold, anchored at x −0.5, z 0.415 | decorative: moved by a binding. It reaches z 0.4325 while shown and takes no collider | the remaining-time bindings (animation graph) |

Selection radius 0.95 (`:46`). Icon `elements/HoldTimer/icon.svg`. At a glance it is the teal case with a horizontal track and three contact pads; the Delay box, its nearest sibling, has a round clock face and a hand.

**Animation (animation graph).** Five presentation bindings (`:48–52`). They read the committed timer row of node `signal` and write nothing back.

| Target | Property | Driver | Values |
|---|---|---|---|
| `Visual/Remaining` | scale X | `CommittedTimerRemaining` | 0.001 to 1, Linear |
| `Visual/Remaining` | visible | `CommittedTimerActive` | hidden at rest, shown while counting |
| `Visual/Indicator` | albedo red, green, blue | `CommittedTimerActive` | slate to gold, SmoothStep 0.1 s; independent of supply |

**Ports** (`:26–28`). There is no activation output.

| `PortId` | Domain | Direction | Role | Signal kind | Position (m) | Facing |
|---|---|---|---|---|---|---|
| `ActivationIn` | Signal | Input | ActivationIn | Pulse | (0, 0.65, 0.2) | +Y |
| `PowerIn` | Electrical | Input | PowerIn | — | (−0.78, 0, 0) | −X |
| `Supply` | Electrical | Output | Supply | — | (0.78, 0, 0) | +X |

**Parameter** (`:21`).

| Name | Kind | Unit | Minimum | Maximum | Default | Editor policy | Run edit |
|---|---|---|---|---|---|---|---|
| `hold_seconds` | Real | second | 0.1 | 12 | 2 | Reject (default) | Build-only |

**Signal node, supply node and event.** One signal node `signal`, a `SignalTimerDescriptor` on `hold_seconds` with completion `Rearm`, boundary `BeforeNetworks` and elapsed signal `None` (`:33`); `ActivationIn` binds to its input (`:35–38`). One supply node `contact`, a `SupplyContactDescriptor` closed while `signal` is `TimerCounting`, between `PowerIn` and `Supply` (`:39–44`). No event (`:32`). `ElectricalNodes` holds one `Switch` row (`:45`); its ratings are unread (section 7).

**Compiled counts per instance.** 1 static body; 7 colliders (3 boxes, 4 spheres); 0 joints; 0 drives; 0 sensors; 1 signal node with one committed timer row; 1 supply node; 0 stores; 3 ports; 5 bindings. All are fixed at compile.

**Catalogue.** Id `hold_timer`, title "Hold timer", category Control, palette order 25, colour (0.40, 0.72, 0.79), description "An activation closes the supplied contact for the configured window. Busy triggers are ignored; expiry rearms it." (`:13–20`). Coverage key `hold_timer`. Authored level 60 `saved_for_later` (plan level 41a) grants one.

## 3. Engine capabilities and conformance

Map families, for navigation only (D14): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction, TimedCommand.

**Generic laws.** The package holds no script, callback, update loop or state machine. The Delay box declares the same timer node with other policy values (`Latch`, `BeforePhysics`, `Activation`), which shows the law is general.

| Behaviour | General law | Declared values |
|---|---|---|
| A body rests on it or rebounds from it | rigid contact with the product pair law for restitution, and friction | seven colliders; the material |
| A trigger opens a window of the declared length; a busy trigger is ignored; expiry rearms | signal network timer rule: a ready node that receives a pulse counts for its duration; at its due tick it completes by its declared completion policy on its declared boundary, and emits its declared elapsed signal | `hold_seconds`; `Rearm`, `BeforeNetworks`, `None` |
| The contact is closed during the window; supply loss never pauses the window | supply network contact rule: a contact is closed while its controlling signal node is in the declared state | `SignalContactState.TimerCounting` |
| A due Delay opens the window before the same tick's consumer step | the engine's declared tick phases: due deliveries reach `BeforeNetworks` timers before supply integration | the boundary value |
| Bar and lamp | presentation bindings on the committed timer row | the five bindings |

**Capability probe.** 6 present, 0 untested, 0 narrower, 0 missing.

| Capability | Status | Symbol | Test |
|---|---|---|---|
| Rearming timer on the before-networks boundary | present | `SignalTimerDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:49`) | `HoldTimerTests.PhysicalTriggerHoldsExactWindowWithoutGeneratingSupply`; `HoldTimerTests.BusyInputsDoNotExtendAndExpiryRearmsWithoutAnyOutput` |
| Supplied contact while counting; supply loss never pauses | present | `SupplyContactDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:40`) | `HoldTimerTests.SupplyLossNeverPausesAndRestorationAfterExpiryNeverReplays` |
| A due Delay opens the window before the same tick's consumer step; save and Reset | present | one chronological router | `HoldTimerTests.DueDelayClosesContactBeforeSameTickConsumerIntegrationAndSaveResetIsExact`; `HoldTimerTests.ScheduledExpiryRetriggerAndPhysicalBypassShareOneChronologicalRouter` |
| Seconds compiled per cadence | present | the same descriptor | `HoldTimerTests.ResetThenCadenceChangeUsesTheNewCompiledTimerDuration` |
| Parameter refusal | present | `ParameterDescriptor` range | `HoldTimerTests.InvalidDurationDoesNotAlterCanonicalPart` |
| Remaining bar and lamp | present | `PresentationDriver.CommittedTimerRemaining`, `CommittedTimerActive` | `HoldTimerPresentationTests.ActualCommittedWindowDrivesAnchoredBarAndIndependentLampAndHiddenCatchUp` |

The window length is the feature, so these tests may assert the due tick.

**Conformance (owner addendum, 22 November 2026): conforms with one fix.**
- Declaration data only: holds. A search of `engine`, `ui` and every non-test `CuriousContraptions.*` project for the package id, class and namespace found no reference (`docs/planning/elements/readiness/audits/elements-batch-b.md` §5).
- Three layers: hold. Every static proud solid has its own collider; the track is a flat print; the bar is moved by bindings and is decorative; the indicator's binding changes colour only.
- One-way flow, typing and flat state: hold, with the counts in section 2. The policies are closed enums.
- Fix: the unread `ElectricalNodes` ratings are a required forward-only refactor (section 7).

**Performance fitness.** The body is static and is never integrated. Seven colliders against the two of the original housing has one reason: visible = solid. The face, the indicator and the three contact pads are each drawn standing clear of the housing, so each has its own primitive. No collider is cosmetic, and the bar, which is cosmetic, has none. The cost for many instances is the recorded, unmeasured engine gap "Per-instance collider growth from visible = solid" (`_bmad-output/implementation-artifacts/deferred-work.md:177–179`), owned by the performance qualification gate P0-034. The timer costs one node row per tick. The authorities state no per-part budget, so none is claimed here.

**Dependencies.** A pulse source (Impact switch, Delay box); Battery and a supplied consumer (Powered gate, Motor). All are delivered.

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

## 6. Resolved delivery decisions

1. Output means the supplied PowerIn→Supply contact only; no ActivationOut is added.
2. Reset clears timers, pending scheduled delivery and committed presentation together; it never invents an expiry occurrence. Busy triggers are dropped.
3. Story 9.5 uses the f32 timer/phase delivered by 9.5a and f32 presentation delivered by 9A.25a. Shared physical ABI migration remains 9A.44. Historical harvest above remains evidence, not a second current implementation.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/HoldTimer`, its scene and its tests on published main `8d1d68aa`, and against `origin/main` `25df3cd5`, which changes none of them. Nothing was built or run.

**Disposition.** Delivered CAT part: package `elements/HoldTimer`, id `hold_timer`, category Control, palette order 25 (`elements/HoldTimer/HoldTimerElementDeclaration.cs:13–18`). No slice is planned for this identity. It owns no named identity. Every residual below is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.

**Integration.**
- Delivered collaborators: Impact switch, Delay box, Battery, Powered gate, Both gate (a Hold contact opens a mutual loop, `BothGateTests.OpeningPoweredContactRetractsMutualLoopWhileSourcesRemainEnabled`), and the Motor and Wound spring of its lesson.
- Campaign: authored level 60 `saved_for_later` (plan level 41a) grants one in the inventory and is Playable; the part itself is first taught at plan level 24 ([campaign coverage section 6](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)).
- Coverage binding: catalogue key `hold_timer` in `docs/coverage/catalogue-elements.json`.

**Residuals.**
- **Required forward-only refactor: unread ratings.** `ElectricalNodes` declares 12 V, 0.05 Ω and 100 W (`HoldTimerElementDeclaration.cs:45`). The four rating values are only checked finite (`CuriousContraptions.Bundle/BundleValidator.cs:873–876`) and nothing reads them. Dead declaration data is removed, not kept; the deferred-work text is in batch B readiness.
- Requirement row CAT-033 stays unchecked until the post-catalogue browser phase.
