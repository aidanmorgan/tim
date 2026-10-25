# CAT-002 · ball_detector (Ball detector) — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-002 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Colliders (owner, 25 October 2026, "visible = solid"):** every part a player sees as solid collides with its true shape. Since [`elements-visible-solid`](../../../_bmad-output/planning-artifacts/epics.md#story-elements-visible-solid) the collar's existing 16 boxes are solid from the 0.65 m bore to the drawn 0.82 m outer surface (radial half 0.085 about radius 0.735, tangential width 2 · 0.82 · tan(π/16) written rounded up to f32 (0.32621631), half-length 0.28), so the outer faces meet and the end faces are closed; no box is added (`elements/BallDetector/ball_detector.tscn`; `VisibleSolidColliderTests` checks the bore, the outer radius and the closure). The ActivationOut socket (sphere r 0.07 at y 1.12) and the indicator (sphere r 0.075 at z 0.86) collide as drawn (coordinator decision, 25 October 2026); the printed chevron does not. The ring boxes' corners reach 0.82 / cos(π/16) = 0.836 m; the round fit belongs to [`engine-hollow-shapes`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-hollow-shapes), which makes the collar a true ring kind. Material: the package now declares this doc's restitution 0.15 (it declared 1). Pair restitution is the product, as it was in the legacy engine, so 1 made a ball leave the collar at its own restitution instead of 0.15 of it; a drop onto the collar tests 0.55 × 0.15.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-002 · `ball_detector` (display name Ball detector) |
| Requirement | [CAT-002](../requirements.md#current-cat-002) |
| Mapped identities | none directly. Related identities with their own specs: EL-181 Rising-edge detector, EL-183 Resettable counter (signal consumers). |
| Roadmap | Story 6.8 (generic aperture sensor with directional rearm) |
| Status | delivered: package `elements/BallDetector`, a generic instance whose passage sensor is selected by its declared aperture (C-Lamp+Detector); browser qualification pending (post-catalogue E2E) |
| Levels | none in `content/puzzles.json`; Free workshop and counter/timer integrations only |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Bodies and shapes | One static body: an opaque hollow collar along local X, half length 0.28 m, inner radius = the common bore radius, outer radius 0.82 m `parts/BallDetectorPart.cs@a6c914e:L48-L52`; a sensor housing box centre (0, 0.93, 0), size 0.5 × 0.24 × 0.5 `parts/BallDetectorPart.cs@a6c914e:L53-L53` (`AddBox` stores half the size `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). The legacy bore radius was `PipePart.BoreRadius`; the current value is `PipeDimensions.BoreRadius` 0.65 m (bore 1.3) `engine/gpu/WorkshopPipe.cs@a6c914e:L9-L10`. Current (`elements/BallDetector/ball_detector.tscn`): the collar as 16 boxes, 0.56 long, from the 0.65 m bore to 0.82 m (centre radius 0.735, tangential width 2 · 0.82 · tan(π/16)); the housing box 0.5 × 0.24 × 0.5 at y 0.93; the ActivationOut socket sphere r 0.07 at y 1.12; and the indicator sphere r 0.075 at z 0.86. |
| Material | restitution 0.15, bounce threshold 0.1 m/s, friction 0.3 `parts/BallDetectorPart.cs@a6c914e:L40-L40`. |
| Constraints and joints | none (static). |
| Sockets and ports | ActivationOut (Activation, Output) at local (0, 1.12, 0) `parts/BallDetectorPart.cs@a6c914e:L39-L42`. Tube mouths Start at (−0.28, 0, 0) facing −X and End at (0.28, 0, 0) facing +X, bore = common bore `parts/BallDetectorPart.cs@a6c914e:L43-L47`. No electrical output. |
| Sensors and activation | Declared passage aperture (`SignalPassageDescriptor(ApertureRadius: 0.65 m, RearmClearance: 0.02 m)` in f32, `elements/BallDetector/BallDetectorElementDeclaration.cs`; validated: radius 0.01–10 m, clearance 0.001–10 m, a pulse ActivationOut and a static frame). The shared compiler builds the passage sensor on the owning frame (`WorkshopActivationCompiler.PassageSensorFor`, f32 sensor lanes since Story 9A.44): a forward body-centre crossing of the local YZ plane along +X within the aperture radius fires once; the body rearms only after its whole collider clears the upstream side by the clearance (`engine/gpu/PassageSensorDeclaration.cs`). Each crossing emits one Passage activation. There is no detector record or arm. |
| Work and energy stores | none. |
| Parameters | none: the placed detector is a generic instance of its package with `Parameters: []` (the aperture is the collar's bore), and the editor shows no panel. |
| Cosmetic curves and UI bindings | Indicator pulse: on a crossing the pulse jumps to 1 and fades linearly over 0.35 s; colour slate `#556573` → gold `#f7cb52` through smoothstep `parts/BallDetectorPart.cs@a6c914e:L37-L38`, `parts/BallDetectorPart.cs@a6c914e:L64-L77`. Design rule `DESIGN.md@a6c914e:L295-L295`. Current (Story 9A.25b, 24 Oct 2026): the three indicator colour bindings are `PresentationDriver.Event` bindings of kind `Passage` (SmoothStep, Replace, 0.35 s, values unchanged). The Animation worker restarts them from slate at each Passage record's own physical time, on every crossing (the deferred §2 rule), and they hold gold until the next crossing. Native proof: `EventRingConsumerTests` (two crossings, each re-pulses; glancing and reverse crossings cue nothing) and `PresentationBindingGoldenTests`. |
| Art | `parts/BallDetectorPart.cs@a6c914e:L48-L58`: cream `#fff8e9` opaque collar; cyan `#66b8c9` housing; navy chevron (two lines) on top; gold socket sphere r 0.07 at (0, 1.12, 0); indicator sphere r 0.075 at (0, 0, 0.86); pick radius 1.15. Scene `parts/scenes/ball_detector.tscn`. Icon `ui/WorkshopIcons.cs@a6c914e:L86-L86` (original ring-and-arrow pictogram). |
| Catalogue and inventory | `parts/catalog/ball_detector.tres@a6c914e:L6-L13`: id `ball_detector`, title Ball detector, category Control, colour (0.40, 0.72, 0.79). |

Current tests: `CuriousContraptions.tests/LampDetectorGuardTests.cs` (9R.9 on the production simulation: one ball fires forward, not in reverse, and fires once more after clearing upstream in the same run; glancing and reverse crossings proven by positions on both sides fire nothing; Reset and Save/Load exact), `EventRingProducerTests.cs` (Story 9A.22: every committed pass publishes one `Passage` record, Count 1 then 2 for the in-run second crossing after clearance, while the first-input lamp stays at one; the in-run second crossing lights the wired counter(target 2) lamp target exactly once, or publishes exactly one more `Passage` record (Count 2); the first-input lamp stays at one; glancing and reverse crossings publish nothing), `DeclaredIndicatorPassageTests.cs` (aperture boundary, a synthetic aperture's own radius and clearance, record-path saves), `BallDetectorTests.cs` (the #1–#4 matrix on the production tracker), `CounterTests.cs` (detector → counter → lamp), `TubeMouthSnapTests.cs`, `PalettePlacementTests.cs` (drawer placement, mouth snap, gestures). Chrome `tools/e2e/cat-002.test.ts` counts the lamp target's own activations (pending re-run in the post-catalogue E2E phase).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction. The map's decision column requires actual sensor geometry, passage and identity predicates and a discrete event policy; contact alone is not detector semantics.

| Exists now | Reference |
| --- | --- |
| SignalPropagation (activation network) | `engine/gpu/ActivationNetwork.cs` |
| Declared activation cosmetics on the animation worker | Story 4.1, 4.2 |
| Generic aperture (passage) sensor with directional rearm, substep-endpoint sampled, selected by the declared `SignalPassageDescriptor` | Story 6.8; C-Lamp+Detector (declaration) |

| Missing | Story that builds it |
| --- | --- |
| Hollow collar contact (annular tube) | Story 6.6 (compound cylinder); the detector collar reuses it |
| Typed tube-mouth snapping | Story 6.6/6.7 (CAT-048) |

Dependencies: CAT-048 hollow tube collider and mouth snapping; an activation target (Signal lamp, Counter, Hold timer).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A forward pass at 4, 40 or 120 m/s (also with the detector rotated 20/30/40° and −90° about Z) records exactly one crossing, starts the connected hold timer, leaves the ball speed unchanged (± 0.002 m/s) and the ball beyond local x 1.9; Reset clears count, pulse, Active and the timer, keeping the connection. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L34-L66` | carry forward. |
| 2 | Reverse motion (from x 2 at −4 m/s), a pass outside the aperture (y + 1.4) and a stationary ball record no crossing. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L67-L84` | carry forward. |
| 3 | Jitter across the plane (twenty ±0.02 m oscillations) after a crossing does not retrigger; moving fully upstream (x −1) and forward again triggers the second crossing; the pulse decays to 0 and Active clears. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L85-L118` | carry forward (whole-ball upstream clearance). |
| 4 | Two separated balls produce two events; a hidden (non-participating) ball never arms. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L119-L135` | carry forward (multiple bodies, disabled participants). |
| 5 | Crossing detection belongs to the physics world, not scene callbacks: moving or hiding artwork does not matter; snapshot restore replays bodies, passage states and events exactly; repeated observation does not duplicate events. | `CuriousContraptions.tests/BallDetectorTests.cs@a6c914e:L137-L173` | carry forward; do not carry forward the per-part `ObservePhysics` loop (per-element update loop). |
| 6 | Sphere, box, rounded hull and compound bodies crossing fixed, translating and rotating frames produce one Passed event at the plane (time 0.25 or 1/3), with the body centre on the plane and velocity unchanged; replay is exact. | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L42-L65` | carry forward (moving-frame and rotated passes). |
| 7 | A curved path that crosses and returns within one step is still detected; outside-aperture crossings publish OutsideAperture and disarm without counting. | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L84-L107` | carry forward the behaviour; do not carry forward the 100-step root-finding sweep (sensor root-finding removed by Story 3.1; use substep-endpoint sampling). |
| 8 | A disabled frame or body cannot arm; after re-enabling, a crossing counts only after physical upstream clearance. A geometry change invalidates arming; a compound wider than the aperture crosses as OutsideAperture. | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L109-L144` | carry forward. |
| 9 | Invalid declarations (non-positive or NaN radius or clearance, unknown bodies) reject atomically; a failed step restores counts, events, bodies and clock. | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L146-L179` | carry forward the atomic rejection. |
| 10 | Detector → counter (target 3) → lamp: two or three forward crossings count exactly; the lamp lights only at three; entity order does not change replay; Reset clears both counts. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L89-L152` | carry forward as an integration (counter owned by CAT-020). |
| 11 | With a rotated detector and a ball at 10 m/s, crossing uses solved positions and collider participation: disabled participation clears pulse and Active and blocks counting. | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L421-L458` | carry forward. |
| 12 | A failed tick restores the observation cursor (count 0, pulse 0, not Active) and the retried tick delivers the crossing and its activation exactly once. | `CuriousContraptions.tests/ImpactRuntimeCheckpointTests.cs@a6c914e:L108-L142` | carry forward the exact-restore rule. |

Files harvested:
- `parts/BallDetectorPart.cs`
- `parts/scenes/ball_detector.tscn` (script binding only)
- `parts/catalog/ball_detector.tres`
- `reference/cpu/MachinePart.cs` (AddBox half-size rule)
- `engine/physics/PhysicsPassageSensor.cs`
- `engine/ScenePassageSensorDeclaration.cs`
- `CuriousContraptions.tests/BallDetectorTests.cs`
- `CuriousContraptions.tests/PhysicsPassageSensorTests.cs`
- `CuriousContraptions.tests/CounterTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/ImpactRuntimeCheckpointTests.cs`

## 5. Acceptance outline

Acceptance: [CAT-002](../requirements.md#current-cat-002) and [retained behaviour](../requirements.md#todo-264).

- **Construction.** Free workshop: place Ball detector and Signal lamp; Connect detector ActivationOut → lamp ActivationIn; aim a ball through the collar along the arrow (or snap a pipe to a mouth).
- **Positive.** One forward centre crossing lights the lamp once; momentum unchanged; arrow and fading pulse from the committed crossing.
- **Negative/control.** Reverse, outside, stationary, jitter without upstream clearance, disabled participant.
- **Boundaries.** Rotated detector, fast (120 m/s) and multiple-body passes; second crossing after whole-ball clearance. The in-run second crossing lights the wired counter(target 2) lamp target exactly once, or publishes exactly one more `Passage` record (Count 2); the first-input lamp stays at one (Story 9A.22, coordinator decision 23 Oct 2026).
- **Run/Reset, Save/Load.** Reset clears the observation cursor and indicator; connection persists through Save/Load.
- **Integrations.** Counter (CAT-020), Hold timer (CAT-033), pipe mouths (CAT-048).

## 6. Open questions

none.
