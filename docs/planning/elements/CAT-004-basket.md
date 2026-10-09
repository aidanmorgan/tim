# CAT-004 · basket (Receiver) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-004 · `basket` (display name Receiver; one part identity) |
| Requirement | [CAT-004](../requirements.md#current-cat-004) |
| Mapped identities | EL-192 Receiving basket. Goal identities EL-120–EL-123 (quantity, rate-window, ordered-events, protected-end-state goals) extend the Captured goal and have their own specs. |
| Roadmap | [CAT-004-I](../invest/vertical-delivery.md#basket-capture); [first_principles](../invest/vertical-delivery.md#first-principles); Story 2.4 (force regions), Story 3.1 (endpoint-sampled sensors), Stories 4.1, 4.3 (capture halo) |
| Status | delivered; remaining items in §5 |
| Levels | see [CAT-004-I consumers](../invest/current-consumers.md#cat-004-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Bodies and shapes | One static body with five static boxes (centre; half extents, m): floor (0, −0.45, 0; 0.75, 0.075, 0.75); left wall (−0.75, 0, 0; 0.06, 0.5, 0.8); right wall (0.75, 0, 0; 0.06, 0.5, 0.8); back wall (0, 0, −0.75; 0.75, 0.5, 0.06); low front wall (0, −0.2, 0.75; 0.75, 0.3, 0.06) `engine/gpu/ReceiverGeometry.cs@a6c914e:L8-L19`, compiled `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L65`. |
| Material | restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L63-L63`. |
| Constraints and joints | none (static). |
| Sockets and ports | none `engine/gpu/WorkshopConnections.cs@a6c914e:L18-L26`; the goal is a typed objective, not a wire. |
| Sensors and activation | One residence sensor per dynamic target ball: open box min (−0.66, −0.4, −0.66), max (0.66, 0.45 + margin, 0.66) in the receiver frame, speed limit and dwell from the capture settings `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L66-L73`; declaration bounds `engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L141`. Capture latches once (`engine/gpu/CaptureLatch.cs`). |
| Work and energy stores | One planar guide (assistance force region) per target: region min (−1.1, 0.5, −1.1), max (1.1, 1.5, 1.1), support height 0.5, support margin = authored margin, maximum acceleration ≤ 12 m/s² `engine/gpu/WorkshopConstruction.cs@a6c914e:L91-L101`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L143-L161`, compiled `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L74-L76`. |
| Parameters | `ReceiverCaptureSettings` margin 0–1 m, speed limit 1/1024–64 m/s, dwell 1/1024–30 s, participation Enabled/Disabled; Free default margin 0.02, speed 1.5, dwell 0.35, guide 0 `engine/gpu/WorkshopConstruction.cs@a6c914e:L79-L90`. first_principles knots at precision 0 / 0.45 / 1: margin 0.3 / 0.174 / 0.02, speed 3 / 2.325 / 1.5, dwell 0.15 / 0.24 / 0.35, guide 12 / 6.6 / 0 `engine/gpu/WorkshopPuzzle.cs@a6c914e:L100-L118`. |
| Cosmetic curves and UI bindings | Capture source, smoothstep, 0.5 s `engine/gpu/WorkshopCosmetic.cs@a6c914e:L58-L58`. Halo ring radius 0.94, half thickness 0.025 (torus radius ± 0.025 `engine/PartArt.cs@a6c914e:L22-L23`) at y 0.55, scaled by 1 + margin, neutral `#bdf4bd` → white `parts/BasketPart.cs@a6c914e:L10-L23`; design rule `DESIGN.md@a6c914e:L278-L278`. |
| Art | Five boxes from the shared geometry, pick radius 0.85 `parts/BasketPart.cs@a6c914e:L12-L15`; scene `parts/scenes/basket.tscn`; palette Basket `#4aab94` `DESIGN.md@a6c914e:L170-L170`; icon `ui/WorkshopIcons.cs@a6c914e:L46-L46`. |
| Catalogue and inventory | `parts/catalog/basket.tres@a6c914e:L6-L14`: id `basket`, title Receiver, category Goals. Free workshop: every Receiver compiles; a second Receiver is rejected while a ball is present (one guide per dynamic body per Receiver; [ANIM-1c note](../delivered-slices.md)). |

Current tests: `CuriousContraptions.tests/WorkshopSimulationTests.cs`, `WorkshopSaveTests.cs`, `WorkshopActivationAnimationTests.cs` (capture feedback), `BasketballResourceTests.cs` (halo), `WorkshopHintTests.cs`; Chrome `tools/e2e/engine-core-2c.test.ts`, `engine-core-2b4.test.ts`, `engine-core-2a3.test.ts`, `anim-1a.test.ts`, `anim-1c.test.ts`, `cat-014.test.ts`.

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, DifficultyAssistance, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ObjectiveEvaluation, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| Static box contact | `CuriousContraptions.Simulation/wwwroot/worker.js`; Story 1.4 |
| Endpoint-sampled residence with tick dwell | Story 3.1; `engine/gpu/CaptureLatch.cs`, `engine/gpu/PhysicsCaptureRead.cs` |
| DifficultyAssistance as a declared force region | Story 2.4; `engine/gpu/WorkshopPuzzle.cs` |
| ObjectiveEvaluation (Captured goal, named body) | `engine/gpu/WorkshopGoalEvaluator.cs` |

| Missing | Story that builds it |
| --- | --- |
| Moving receiver frame (residence in a kinematic/moving frame) | not scheduled; needed by any level that moves a Receiver (see §6) |
| FiniteWorkActuation, JointConstraint memberships (inventory only; no current mode needs them) | none required now |
| Further goal kinds (quantity, ordered events, protected end state) | EL-120–EL-123 (Batch M) and Epic 15 |
| f32 declarations: `ReceiverGeometry` boxes, `ReceiverCaptureSettings`, `ReceiverForceRegion`, residence and guide declarations and the assistance knots are still binary16 (`Half`) | Remaining f32 migration ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)) |

Dependencies: CAT-001 or another dynamic body as the named target.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Guide eligibility: at margin 0.3 and acceleration 12, a descending ball (vy −1) at local height 0.9 or 0.84 inside the rim is guided, also with the basket rotated 47°; it is not guided at margin 0 and height 0.835, below the guide region (0.49), above it (1.6), outside in depth (z 1.2) or ascending (vy +1). | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L17-L58` | carry forward as the eligibility control matrix. |
| 2 | Guide load: maximum acceleration as authored, minimum height 0.5, minimum support height 0.5 − margin; the vertical component of the change is zero and its magnitude ≤ acceleration × tick; basket pose and boxes never change; precision 1 installs no guide. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L49-L65` | carry forward. |
| 3 | A 240 m/s pass through the whole guide window receives only the bounded lateral change its brief residence earns; the walls do not move; Reset restores the construction. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L69-L101` | carry forward the behaviour; do not carry forward the analytic in-window time integral (replaced by substep-endpoint force evaluation, Story 2.4). |
| 4 | Guide clearance uses every collider child of the target body (an extra child protruding 0.6 m below suppresses guidance), not the presentation pose. | `CuriousContraptions.tests/BasketGuideTests.cs@a6c914e:L104-L132` | carry forward (matches the requirement's "all collider-child support heights"). |
| 5 | Residence is evaluated in the receiver's frame: a body co-moving with a translating or rotating frame dwells then captures; a non-co-moving body stays Outside; snapshot replay is exact. | `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs@a6c914e:L28-L52` | carry forward (moving-frame requirement). |
| 6 | Disabled participation (frame or body) resets residence to Outside with zero elapsed; capture then needs a full new dwell; once Captured, leaving does not un-capture. | `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs@a6c914e:L53-L69` | carry forward. |
| 7 | High speed or leaving the region resets partial residence to zero; re-entry starts again. | `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs@a6c914e:L70-L80` | carry forward. |
| 8 | Invalid residence declarations (self target, empty region, zero speed, NaN dwell, duplicates, unknown bodies) reject atomically. | `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs@a6c914e:L81-L96` | carry forward (current `ResidenceSensorDeclaration.Validate`). |
| 9 | Physical dwell owns capture regardless of presentation edits (moving or hiding the basket artwork); the Captured event is published once and is not duplicated by further observation; Reset clears it. | `CuriousContraptions.tests/BasketResidenceOwnershipTests.cs@a6c914e:L17-L60` | carry forward; do not carry forward the per-part `ObservePhysics` loop (per-element update loop). |
| 10 | A decoy ball captured by the Receiver does not satisfy a goal naming another ball. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L236-L258` | carry forward. |
| 11 | In spring_forward, forgiving precision accepts a strictly larger set of receiver x-offsets (−1.6 to 1.6 in 0.1 steps) than precise, and contains every precise success. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L44-L70` | carry forward as an assistance acceptance fact. |
| 12 | Authoring knots: every instance owns its difficulty curve; capture margin 0.30 / 0.174, speed 3 / 2.325, dwell 0.15 / 0.24 at precision 0 / 0.45; guide acceleration 12 / 6.6 for `basket` only; precision 1 uses defaults. | `tools/Campaign/Program.cs@a6c914e:L540-L563` | carry forward the values; do not carry forward the string-keyed `part.Kind` switch (string-typed closed set). |
| 13 | first_principles guide probe fixtures: interior (0.5, 1.1), entry (0.5, 1.6), exit (0.5, 0.55), rotated π/6, ascending, and a re-entry case; puzzle cases at precision 0 / 0.45 / 1 with ramps at (−3.4, 4.3) and (−0.7, 2.7), both −20°. | `reference/p054-guide/Program.cs@a6c914e:L24-L52` | carry forward the fixture matrix; WGSL root diagnostics and the startup worker in `reference/p054-guide/` are probe artefacts, no element knowledge. |
| 14 | With each authored solution at precision 0, the Receiver captures the ball within 1,200 ticks in clear_pipe and quarter_bend at their authored positions, and in spring_forward with the Receiver moved 0.3 m in +x; moved 0.5 m in −x in spring_forward it does not capture. All three Receivers carry the precision-0 knots capture margin 0.3 and guide acceleration 12. | `CuriousContraptions.tests/FlightCampaignTests.cs@a6c914e:L15-L40`; `content/puzzles.json@a6c914e:L781-L796`; `content/puzzles.json@a6c914e:L7368-L7383`; `content/puzzles.json@a6c914e:L7792-L7807` | carry forward as level-regression boundaries of the Receiver's authored assistance region (content in `content/puzzles.json`, which is kept). |

Files harvested:
- `CuriousContraptions.tests/BasketGuideTests.cs`
- `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs`
- `CuriousContraptions.tests/BasketResidenceOwnershipTests.cs`
- `CuriousContraptions.tests/BasicTests.cs`
- `CuriousContraptions.tests/FlightCampaignTests.cs`
- `tools/Campaign/Program.cs`
- `reference/p054-guide/` (Program.cs; puzzles-candidate.json, root-diagnostic.wgsl, startup-worker.js, inputs.json and csproj hold no further element knowledge)

## 5. Acceptance outline

Acceptance: [CAT-004](../requirements.md#current-cat-004) and [simulation and element acceptance](../requirements.md#accept-simulation).

- **Construction.** Free workshop: place Basketball and Receiver; aim the ball into it. first_principles: fixed ball/receiver, place two ramps.
- **Positive.** Capture after declared dwell at ≤ speed limit; halo animates once; Solved once in first_principles.
- **Negative/control.** Fast through-pass, outside/edge placement, disabled participant, leave-and-re-enter, decoy ball, missing ramp.
- **Boundaries.** Guide eligibility matrix (fact 1); knots 0 / 0.45 / 1 without moving walls; dwell counted in ticks at substep endpoints.
- **Run/Reset, Save/Load.** Reset clears capture, timers, halo and guide state; Save/Load restores pose and settings.
- **Integrations.** Every delivery level; Bowling capture (`cat-014`); pipe and bend deliveries (CAT-048/049/050).
- **Remaining (unmet now).** Moving-frame capture is not exercised by any admitted level; first_principles is the only admitted assisted level; the other basket levels need their parts admitted.

## 6. Open questions

- Whether any planned level moves a Receiver (requiring a kinematic frame), or the moving-frame requirement is satisfied by the static-frame implementation: unspecified — owner decision.
