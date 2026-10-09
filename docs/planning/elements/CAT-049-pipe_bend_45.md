# CAT-049 · pipe_bend_45 (45° pipe bend) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-049 · `pipe_bend_45` (display name 45° pipe bend) |
| Requirement | [CAT-049](../requirements.md#current-cat-049) |
| Mapped identities | EL-072 Curved metal ball pipe (shared with CAT-050). Related, own specs: EL-009 45-degree water elbow (liquid), EL-107 Spiral gravity-delay tube. |
| Roadmap | Story 6.9 (added by Story 7.0) |
| Status | not started on the current engine (legacy `parts/PipeBendPart.cs` is uncompiled) |
| Levels | inventory in gentle_bend; see [CAT-049-I consumers](../invest/current-consumers.md#cat-049-i) |
| Mode | TubeAngle = Degrees45 (`TubeBendAngle.Degrees45 = 45`) `engine/MachineData.cs@a6c914e:L8-L8` |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One static hollow circular bend: centreline radius 2.4 m, sweep 45° (π/4), bore radius = the common bore (0.65 m, `engine/gpu/WorkshopPipe.cs@a6c914e:L9-L10`), shell outer radius 0.70 m; centreline R·(sin a, cos a, 0) for a in [0, sweep], offset by −2.4·(sin(sweep/2), cos(sweep/2), 0) so the arc is centred on the part origin `parts/PipeBendPart.cs@a6c914e:L8-L11`, `parts/PipeBendPart.cs@a6c914e:L27-L30`, `engine/BendProxy.cs@a6c914e:L5-L12`. Two opaque end collars: tube half width 0.09, from bore to 0.78 m, oriented on the end tangents `parts/PipeBendPart.cs@a6c914e:L31-L36`. Bend validity: sweep in (0, π], centreline radius > shell radius `engine/physics/HollowGeometry.cs@a6c914e:L109-L113`. |
| Material | restitution 0.15, bounce threshold 0.1 m/s, friction 0.3 `parts/PipeBendPart.cs@a6c914e:L12-L12`. |
| Constraints and joints | none (static). |
| Sockets and ports | none. Typed tube mouths: Start at angle 0, End at angle = sweep; each mouth position = centreline point + outward tangent × 0.09 (outside collar face), outward normal = tangent × (−1 for Start, +1 for End), bore = common bore `parts/PipeBendPart.cs@a6c914e:L13-L26`. |
| Sensors and activation | none. |
| Work and energy stores | none (no scripted travel, no added energy). |
| Parameters | none (fixed geometry; no resize controls) `DESIGN.md@a6c914e:L298-L298`. |
| Cosmetic curves and UI bindings | none. |
| Art | `parts/PipeBendPart.cs@a6c914e:L37-L71`: shell mesh 32 steps along the sweep × 48 sides, inner surface at bore and outer at 0.70, colour (0.40, 0.72, 0.79, alpha 0.16), double-sided, no shadow; navy `#293954` rails of radius 0.018 at z ±0.70 along the centreline (`PartArt.Line` width is the cylinder radius `engine/PartArt.cs@a6c914e:L24-L30`); cream `#fff8e9` collars; selection radius 2.2. Scene `parts/scenes/pipe_bend_45.tscn@a6c914e:L5-L7` (BendAngle = 45). Icon (own, angle-specific) `ui/WorkshopIcons.cs@a6c914e:L89-L89`. Palette as Clear pipe `DESIGN.md@a6c914e:L181-L181`. |
| Catalogue and inventory | `parts/catalog/pipe_bend_45.tres@a6c914e:L6-L13`: id `pipe_bend_45`, title 45° pipe bend, category Motion. |

Current tests: none compiled.

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| Sphere contact with static shapes | Stories 1.1–1.5 |

| Missing | Story that builds it |
| --- | --- |
| Hollow torus-segment collider with rim caps in the shared pair table | Story 6.7 (signed-distance torus caps), applied by Story 6.9 |
| Straight tube collider and typed mouth snapping | Stories 6.6, 6.7 (CAT-048) |
| `WorkshopPartKind` member and declaration for the 45° bend | Story 6.9 |
| Physical placement nudging (the requirement's "authored assistance controls"; fact 4's depth correction at precision 0 and 0.45) | not scheduled, owner decision: the roadmap reports physical nudging unsupported in the current playable mode ([first_principles](../invest/vertical-delivery.md#first-principles)) |
| Air blocking across the bend shell; optical clarity (fact 9) | Epic 12 (Story 12.2) and Epic 13 (Story 13.1) |

Dependencies: CAT-048 (bore, collars, mouth snapping, tube collider). CAT-050 shares the bend family but is a separate element.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | With zero gravity, a ball entering 0.8 m before the inlet at 6 m/s turns through the 45° bend in orientations (0,0,0), (30,40,50) and (0,90,90): steps < 0.051 m, speed ≤ 6.001 m/s, exit velocity within cos > 0.8 of the outlet normal; the bend has no boxes, one bend proxy and two collar tubes; Reset restores the ball's initial pose and velocity. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L145-L193` | carry forward (rotated passage, no added energy). |
| 2 | The 45° bend's End mouth snaps a straight pipe, another 45° bend and a 90° bend placed 0.1 m off: mouths coincide within 1e−4 m. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L195-L221` | carry forward (straight-to-bend and bend-to-bend). |
| 3 | Gravity alone carries a ball dropped 1.6 m above the inlet of a bend at (0, 4, 0) rotated −90° down to y ≤ 1.2 at x < −0.5. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L223-L243` | carry forward. |
| 4 | gentle_bend with the bend moved 0.48 m in depth is won at precision 0, 0.45 and also 1 (an actual valid Precise route); at precision < 1 the assistance corrects the depth to 0, at 1 it stays 0.48. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L245-L271` | carry forward (Precise alternative route; authored assistance controls); the depth correction needs physical placement nudging (§3, §6). |
| 5 | Level layout: inlet x = 2.4·(1 − cos(angle/2)), inlet y = 4 + 2.4·sin(angle/2) + 0.09; ball at the inlet 1.6 m higher; receiver at (−2, 0.6, 0) for 45°; bend_1 at (0, 4, 0) rotated −90° about Z. | `tools/Campaign/Program.cs@a6c914e:L290-L306` | carry forward (content in `content/puzzles.json` gentle_bend: ball (0.1827, 6.6084, 0)). |
| 6 | Bend assistance window: position 0.6 / 0.5 m and rotation 5° / 2° with equal correction caps at precision 0 / 0.45; curved outlets amplify residual lateral error, so a more distant bend is never moved. | `tools/Campaign/Program.cs@a6c914e:L564-L573` | carry forward the values and rule; do not carry forward the string `part.Kind` test. |
| 7 | Test fixtures map an enum (Straight, Bend45, Bend90) to catalogue strings and reject undefined values. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L11-L39` | carry forward the typed boundary rule only. |
| 8 | Bend walls were built as rounded convex segments within 0.005 m surface error; bends are never optically opaque. | `engine/physics/HollowGeometry.cs@a6c914e:L109-L146`; `engine/SceneCollisionGeometry.cs@a6c914e:L76-L76` | carry forward the transparency fact; do not carry forward the CPU segmentation. |
| 9 | A 45° bend rotated (20, 35, 10): a ray from 2 m outside the mid-sweep shell blocks air at 0.7–1.4 m but passes light for the full 4 m. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L220-L237` | carry forward as the air/light shell controls (owed at Stories 12.2 and 13.1). |

Files harvested:
- `parts/PipeBendPart.cs`
- `parts/PipeArt.cs` (shared tube mesh)
- `parts/scenes/pipe_bend_45.tscn`
- `parts/catalog/pipe_bend_45.tres`
- `engine/BendProxy.cs`
- `engine/MachineData.cs` (TubeBendAngle)
- `engine/TubeMouth.cs`
- `engine/physics/HollowGeometry.cs`
- `engine/SceneCollisionGeometry.cs`
- `CuriousContraptions.tests/PipeBendTests.cs`
- `CuriousContraptions.tests/WindChimeTests.cs` (L220-L237)
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-049](../requirements.md#current-cat-049) and [retained behaviour](../requirements.md#todo-250); story [6.9](../../../_bmad-output/planning-artifacts/epics.md).

- **Construction.** gentle_bend: place the 45° bend from inventory, turn the first mouth up under the ball, Run.
- **Positive.** Ball turns through the curve under gravity and reaches the receiver.
- **Negative/control.** Rim strike, oversize payload, sidewall drop, missed connection.
- **Boundaries.** Rotated straight-to-bend and bend-to-bend passage; Precise alternative route (fact 4).
- **Run/Reset, Save/Load.** Pose restored exactly.
- **Distinctness.** Own icon, rails and collars; no 90° proof substitutes.
- **Integrations.** Contact and cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (pipe/bend/funnel); bend combinations [todo-250](../requirements.md#todo-250); [IX-01 contact impulse](../requirements.md#interaction-01); campaign first use 21–30 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (ball routes row: both bend angles) and chapter 3 "Pipe Dreams" of the [150-level progression](../requirements.md#sequence-task-315). Partners: straight pipe (CAT-048), 90° bend (CAT-050), funnel (CAT-030), Receiver (CAT-004).
- **Remaining (unmet now).** The authored assistance controls (fact 4 depth correction) depend on physical placement nudging, which is not scheduled.

## 6. Open questions

- Oversize payload for the rim/oversize control: see CAT-048 §6 (no current oversize ball kind): unspecified — owner decision.
- Physical placement nudging: not scheduled, owner decision. Story 6.9's "authored assistance controls" need it; which story builds it, or whether Story 6.9 closes with those controls deferred, is unspecified.
