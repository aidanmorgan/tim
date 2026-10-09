# CAT-050 · pipe_bend_90 (90° pipe bend) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-050 · `pipe_bend_90` (display name 90° pipe bend) |
| Requirement | [CAT-050](../requirements.md#current-cat-050) |
| Mapped identities | EL-072 Curved metal ball pipe (shared with CAT-049). Related, own spec: EL-010 90-degree water elbow (liquid). |
| Roadmap | Story 6.10 (added by Story 7.0) |
| Status | not started on the current engine (legacy `parts/PipeBendPart.cs` is uncompiled) |
| Levels | placed in joined_pipe; inventory in quarter_bend; see [CAT-050-I consumers](../invest/current-consumers.md#cat-050-i) |
| Mode | TubeAngle = Degrees90 (`TubeBendAngle.Degrees90 = 90`, the script default) `engine/MachineData.cs@a6c914e:L8-L8`, `parts/PipeBendPart.cs@a6c914e:L8-L8` |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One static hollow circular bend: centreline radius 2.4 m, sweep 90° (π/2), bore radius = the common bore (0.65 m, `engine/gpu/WorkshopPipe.cs@a6c914e:L9-L10`), shell outer radius 0.70 m; centreline R·(sin a, cos a, 0), offset by −2.4·(sin(sweep/2), cos(sweep/2), 0) `parts/PipeBendPart.cs@a6c914e:L8-L11`, `parts/PipeBendPart.cs@a6c914e:L27-L30`, `engine/BendProxy.cs@a6c914e:L5-L12`. Two opaque end collars, half width 0.09, bore to 0.78 m `parts/PipeBendPart.cs@a6c914e:L31-L36`. Bend validity: sweep in (0, π], centreline radius > shell radius `engine/physics/HollowGeometry.cs@a6c914e:L109-L113`. |
| Material | restitution 0.15, bounce threshold 0.1 m/s, friction 0.3 `parts/PipeBendPart.cs@a6c914e:L12-L12`. |
| Constraints and joints | none (static). |
| Sockets and ports | none. Typed tube mouths Start (angle 0) and End (angle 90°), at the outside collar faces (tangent × 0.09), outward along ∓ tangent, common bore `parts/PipeBendPart.cs@a6c914e:L13-L26`. |
| Sensors and activation | none. |
| Work and energy stores | none. |
| Parameters | none (fixed geometry, no resize) `DESIGN.md@a6c914e:L298-L298`. |
| Cosmetic curves and UI bindings | none. |
| Art | Same builder as CAT-049 with sweep 90°: 32 × 48 shell, navy rails of radius 0.018 at z ±0.70 (`PartArt.Line` width is the cylinder radius `engine/PartArt.cs@a6c914e:L24-L30`), cream collars, selection radius 2.2 `parts/PipeBendPart.cs@a6c914e:L37-L71`. Scene `parts/scenes/pipe_bend_90.tscn@a6c914e:L5-L7` (BendAngle = 90). Own icon `ui/WorkshopIcons.cs@a6c914e:L90-L90`. |
| Catalogue and inventory | `parts/catalog/pipe_bend_90.tres@a6c914e:L6-L13`: id `pipe_bend_90`, title 90° pipe bend, category Motion. |

Current tests: none compiled.

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| Sphere contact with static shapes | Stories 1.1–1.5 |

| Missing | Story that builds it |
| --- | --- |
| Hollow torus-segment collider with rim caps | Story 6.7, applied by Stories 6.9/6.10 |
| Straight tube collider, length resize and mouth snapping (joined_pipe) | Stories 6.6, 6.7 (CAT-048) |
| `WorkshopPartKind` member and declaration for the 90° bend | Story 6.10 |
| Physical placement nudging (the declared depth-error result "Forgiving/Balanced succeed where Precise fails", facts 2 and 7) | not scheduled, owner decision: the roadmap reports physical nudging unsupported in the current playable mode ([first_principles](../invest/vertical-delivery.md#first-principles)) |
| Air blocking across the bend shell; optical clarity (fact 11) | Epic 12 (Story 12.2) and Epic 13 (Story 13.1) |

Dependencies: CAT-048. Builds after CAT-049 (Story 6.9), which introduces the bend declaration.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A ball at 6 or 40 m/s crosses a joined seam from a straight pipe or from another 90° bend into a 90° bend rotated (20, 30, 40): each step ≤ speed × tick + 0.002 m, speed ≤ speed + 0.002, exit velocity within cos > 0.8 of the outlet normal. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L41-L85` | carry forward (continuous turned passage, straight/bend joins, high speed). |
| 2 | joined_pipe with the straight pipe moved in depth: error 0.48 is won at precision 0 and 0.45, lost at 1; error 0.58 is won at 0, lost at 0.45 and 1; when won the assistance corrected depth to 0, otherwise the error remains (no editor snap during Run). | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L87-L114` | carry forward (declared depth-error Forgiving/Balanced success versus Precise failure); the corrections need physical placement nudging (§3, §6). |
| 3 | A length-2 straight pipe at −45° joined to the fixed bend at (1, 3.5, 0), −45°: a ball dropped 1.6 m above the pipe entry reaches y ≤ 1.2 more than 0.3 m past the bend outlet, moving within cos > 0.9 of the outlet normal. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L116-L143` | carry forward (joined_pipe fixture). |
| 4 | Zero-gravity 6 m/s turn through the 90° bend in three orientations, no added energy, one bend proxy and two collars, exact Reset. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L145-L193` | carry forward. |
| 5 | The 90° bend's End mouth snaps a straight pipe, a 45° bend and another 90° bend within 1e−4 m. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L195-L221` | carry forward. |
| 6 | Gravity alone carries a ball dropped 1.6 m above the inlet of a bend at (0, 4, 0), −90°, to y ≤ 1.2 at x < −0.5. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L223-L243` | carry forward. |
| 7 | quarter_bend with the bend 0.48 m off in depth is won at precision 0 and 0.45 (depth corrected to 0) and lost at 1 (no Precise alternative, unlike the 45° bend). | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L245-L271` | carry forward; the corrections need physical placement nudging (§3, §6). |
| 8 | Layouts: quarter_bend receiver at (−5.5, 0.6, 0); bend_1 at (0, 4, 0), −90°; ball 1.6 m above the computed inlet. joined_pipe: ball (−1.308, 8.402, 0), fixed bend (1, 3.5, 0) at −45°, receiver (−0.2, 0.6, 0), solution pipe length 2 at (−0.537, 6.031, 0), −45°; "a vertical fall misses the receiver". | `tools/Campaign/Program.cs@a6c914e:L290-L320` | carry forward (content in `content/puzzles.json`). |
| 9 | Bend and joined_pipe straight-pipe assistance windows: 0.6 / 0.5 m and 5° / 2° with equal caps. | `tools/Campaign/Program.cs@a6c914e:L564-L573` | carry forward the values. |
| 10 | Bends are never optically opaque; walls were CPU convex segments within 0.005 m. | `engine/SceneCollisionGeometry.cs@a6c914e:L76-L76`; `engine/physics/HollowGeometry.cs@a6c914e:L109-L146` | carry forward the transparency fact; do not carry forward the CPU segmentation. |
| 11 | A 90° bend rotated (20, 35, 10): a ray from 2 m outside the mid-sweep shell blocks air at 0.7–1.4 m but passes light for the full 4 m. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L220-L237` | carry forward as the air/light shell controls (owed at Stories 12.2 and 13.1). |
| 12 | quarter_bend with its authored solution at precision 0 is won: the ball is captured by the Receiver within 1,200 ticks. | `CuriousContraptions.tests/FlightCampaignTests.cs@a6c914e:L15-L40` | carry forward as a level-regression check for quarter_bend (shared with CAT-004). |

Files harvested:
- `parts/PipeBendPart.cs`
- `parts/PipeArt.cs`
- `parts/scenes/pipe_bend_90.tscn`
- `parts/catalog/pipe_bend_90.tres`
- `engine/BendProxy.cs`
- `engine/MachineData.cs` (TubeBendAngle)
- `engine/TubeMouth.cs`
- `engine/physics/HollowGeometry.cs`
- `engine/SceneCollisionGeometry.cs`
- `CuriousContraptions.tests/PipeBendTests.cs`
- `CuriousContraptions.tests/FlightCampaignTests.cs`
- `CuriousContraptions.tests/WindChimeTests.cs` (L220-L237)
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-050](../requirements.md#current-cat-050) and [retained behaviour](../requirements.md#todo-250); story [6.10](../../../_bmad-output/planning-artifacts/epics.md).

- **Construction.** quarter_bend: place the 90° bend, one mouth up, the other toward the left basket. joined_pipe: resize the straight pipe to 2, turn it downhill, bring its lower mouth near the fixed bend's upper mouth (snap).
- **Positive.** Continuous turned passage into the receiver in both levels.
- **Negative/control.** Rotated, oversize, rim and missing-connection controls; vertical fall without the pipe misses.
- **Boundaries.** Depth error 0.48 / 0.58 across Forgiving, Balanced and Precise (fact 2).
- **Run/Reset, Save/Load.** Poses and pipe length restored.
- **Distinctness.** Matching collars, rails and own icon; no 45° representative closure.
- **Integrations.** Contact and cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (pipe/bend/funnel); bend combinations [todo-250](../requirements.md#todo-250); [IX-01 contact impulse](../requirements.md#interaction-01); campaign first use 21–30 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (ball routes row: both bend angles) and chapter 3 "Pipe Dreams" of the [150-level progression](../requirements.md#sequence-task-315). Partners: straight pipe (CAT-048) in joined_pipe, 45° bend (CAT-049), Receiver (CAT-004).
- **Remaining (unmet now).** The Forgiving/Balanced depth-error successes (facts 2, 7) depend on physical placement nudging, which is not scheduled.

## 6. Open questions

- Oversize payload for the oversize control: see CAT-048 §6: unspecified — owner decision.
- Physical placement nudging: not scheduled, owner decision. Story 6.10's "Forgiving/Balanced succeed where Precise fails" needs it; which story builds it, or whether Story 6.10 closes with that criterion deferred, is unspecified.
