# CAT-002 · ball_detector (Ball detector) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-002 · `ball_detector` (display name Ball detector) |
| Requirement | [CAT-002](../requirements.md#current-cat-002) |
| Mapped identities | none directly. Related identities with their own specs: EL-181 Rising-edge detector, EL-183 Resettable counter (signal consumers). |
| Roadmap | Story 6.8 (generic aperture sensor with directional rearm) |
| Status | not started on the current engine (legacy `parts/BallDetectorPart.cs` is uncompiled) |
| Levels | none in `content/puzzles.json`; Free workshop and counter/timer integrations only |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Bodies and shapes | One static body: an opaque hollow collar along local X, half length 0.28 m, inner radius = the common bore radius, outer radius 0.82 m `parts/BallDetectorPart.cs@a6c914e:L48-L52`; a sensor housing box centre (0, 0.93, 0), size 0.5 × 0.24 × 0.5 `parts/BallDetectorPart.cs@a6c914e:L53-L53` (`AddBox` stores half the size `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). The legacy bore radius was `PipePart.BoreRadius`; the current value is `PipeDimensions.BoreRadius` 0.65 m (bore 1.3) `engine/gpu/WorkshopPipe.cs@a6c914e:L9-L10`. |
| Material | restitution 0.15, bounce threshold 0.1 m/s, friction 0.3 `parts/BallDetectorPart.cs@a6c914e:L40-L40`. |
| Constraints and joints | none (static). |
| Sockets and ports | ActivationOut (Activation, Output) at local (0, 1.12, 0) `parts/BallDetectorPart.cs@a6c914e:L39-L42`. Tube mouths Start at (−0.28, 0, 0) facing −X and End at (0.28, 0, 0) facing +X, bore = common bore `parts/BallDetectorPart.cs@a6c914e:L43-L47`. No electrical output. |
| Sensors and activation | Passage sensor on the owning frame: forward body-centre crossing of the local YZ plane within aperture radius = bore radius; rearm requires the whole collider to clear the upstream side by 0.02 m `parts/BallDetectorPart.cs@a6c914e:L23-L24`, `engine/physics/PhysicsPassageSensor.cs@a6c914e:L13-L26`, `engine/ScenePassageSensorDeclaration.cs@a6c914e:L3-L4`. Each Passed event emits one activation `parts/BallDetectorPart.cs@a6c914e:L59-L78`. |
| Work and energy stores | none. |
| Parameters | none in the catalogue `parts/catalog/ball_detector.tres@a6c914e:L6-L13`. |
| Cosmetic curves and UI bindings | Indicator pulse: on a crossing the pulse jumps to 1 and fades linearly over 0.35 s; colour slate `#556573` → gold `#f7cb52` through smoothstep `parts/BallDetectorPart.cs@a6c914e:L37-L38`, `parts/BallDetectorPart.cs@a6c914e:L64-L77`. Design rule `DESIGN.md@a6c914e:L295-L295`. |
| Art | `parts/BallDetectorPart.cs@a6c914e:L48-L58`: cream `#fff8e9` opaque collar; cyan `#66b8c9` housing; navy chevron (two lines) on top; gold socket sphere r 0.07 at (0, 1.12, 0); indicator sphere r 0.075 at (0, 0, 0.86); pick radius 1.15. Scene `parts/scenes/ball_detector.tscn`. Icon `ui/WorkshopIcons.cs@a6c914e:L86-L86` (original ring-and-arrow pictogram). |
| Catalogue and inventory | `parts/catalog/ball_detector.tres@a6c914e:L6-L13`: id `ball_detector`, title Ball detector, category Control, colour (0.40, 0.72, 0.79). |

Current tests: none compiled.

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction. The map's decision column requires actual sensor geometry, passage and identity predicates and a discrete event policy; contact alone is not detector semantics.

| Exists now | Reference |
| --- | --- |
| SignalPropagation (activation network) | `engine/gpu/ActivationNetwork.cs` |
| Declared activation cosmetics on the animation worker | Story 4.1, 4.2 |

| Missing | Story that builds it |
| --- | --- |
| Generic aperture (passage) sensor with directional rearm, endpoint sampled | Story 6.8 |
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
- **Boundaries.** Rotated detector, fast (120 m/s) and multiple-body passes; second crossing after whole-ball clearance.
- **Run/Reset, Save/Load.** Reset clears the observation cursor and indicator; connection persists through Save/Load.
- **Integrations.** Counter (CAT-020), Hold timer (CAT-033), pipe mouths (CAT-048).

## 6. Open questions

none.
