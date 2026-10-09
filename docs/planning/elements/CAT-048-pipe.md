# CAT-048 · pipe (Clear pipe) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-048 · `pipe` (display name Clear pipe) |
| Requirement | [CAT-048](../requirements.md#current-cat-048) |
| Mapped identities | EL-071 Straight metal ball pipe (refines the straight tube). Related, own specs: EL-102 Large-bore ball pipe, EL-103 Accelerator tube, EL-107 Spiral gravity-delay tube, EL-008 Straight water pipe (liquid, not balls). |
| Roadmap | Stories 6.6 (CAT-048a compound cylindrical collider), 6.7 (CAT-048b hollow torus/rim cap collider, clear_pipe) |
| Status | not started on the current engine; parked GPU declaration work exists but is excluded from the build (`CuriousContraptions.csproj@a6c914e:L29-L29`) |
| Levels | inventory in clear_pipe, joined_pipe; see [CAT-048-I consumers](../invest/current-consumers.md#cat-048-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One static hollow straight tube along local X (parked): `PipeDimensions` length 1–8 m (default 3.6), bore radius 0.65 m (bore 1.3), annular profile half length = length/2, inner radius 0.65, shell outer radius 0.70, collar (end band) outer radius 0.78, collar half width 0.09 `engine/gpu/WorkshopPipe.cs@a6c914e:L5-L29`. Profile topology rules: middle > inner, end ≥ middle, half length > end half width, end bands present exactly when end radius > middle `engine/gpu/AnnularProfile.cs@a6c914e:L18-L37`. Mouths sit at the outer collar faces, ±(length/2 + 0.09): legacy `TubeMouth` is the outside face of the collar `engine/TubeMouth.cs@a6c914e:L6-L14`, and two default pipes 3.78 m apart have touching mouths `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L110-L113`. `parts/PipePart.cs` at `a6c914e` no longer implements `ITubePart`; the mouth data survives only in the tests and the other tube parts. |
| Material | restitution 0.15, bounce threshold 0.1 m/s, friction 0.3 `CuriousContraptions.tests/WorkshopPipeTests.cs@a6c914e:L34-L44`. |
| Constraints and joints | none (static). |
| Sockets and ports | none: no activation or electrical ports; a pipe → lamp activation link rejects `CuriousContraptions.tests/WorkshopPipeTests.cs@a6c914e:L118-L136`. Typed tube mouths Start (−X) and End (+X) with bore radius are placement data, not network edges. |
| Sensors and activation | none. |
| Work and energy stores | none (no transport effect, no added speed). |
| Parameters | `length` 1–8 m, default 3.6; pointer input clamps before conversion and rejects non-finite values or any bore change `engine/gpu/WorkshopPipe.cs@a6c914e:L21-L28`; resource `engine/PipeDimensionsResource.cs@a6c914e:L8-L17`. |
| Cosmetic curves and UI bindings | none (`CosmeticCurveDeclaration.None`) `engine/gpu/WorkshopPipe.cs@a6c914e:L31-L41`. One local-axis (X) resize handle `parts/PipePart.cs@a6c914e:L12-L12`, reached through the contextual "Resize mode" button (§4 fact 20). |
| Art | `parts/PipePart.cs@a6c914e:L15-L47`: transparent shell (0.40, 0.72, 0.79, alpha 0.16) between bore and shell radius, scaled along X by length; two opaque cream `#fff8e9` collars (half width 0.09, outer 0.78) moved to ±length/2; two navy `#293954` rails of radius 0.018 at z ±0.70 (`PartArt.Line` width is the cylinder radius `engine/PartArt.cs@a6c914e:L24-L30`); pick radius √((length/2 + 0.09)² + 0.78²). Mesh builder: 48-sided annular cylinder, double-sided, transparent shells cast no shadow `parts/PipeArt.cs@a6c914e:L40-L71`. Scene `parts/scenes/pipe.tscn`. Palette Clear pipe `#66b8c9`, shell alpha 0.16 `DESIGN.md@a6c914e:L181-L181`. Icon `ui/WorkshopIcons.cs@a6c914e:L91-L91`. Design rules (resize 1–8, Escape cancels, one Undo, snap 0.45 units and 20°, preview on hover, snap on release) `DESIGN.md@a6c914e:L299-L299`. |
| Catalogue and inventory | `parts/catalog/pipe.tres@a6c914e:L8-L20`: id `pipe`, title Clear pipe, category Motion, default length 3.6. |

Parked current-engine test: `CuriousContraptions.tests/WorkshopPipeTests.cs` (uncompiled; references `WorkshopPartKind.Pipe` and `ColliderShapeKind.AnnularProfile`, which do not exist at `a6c914e`).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| Sphere contact against static boxes/planes/spheres | Stories 1.1–1.5 |
| Local-axis resize gizmo and dashed projections | `ui/RotationGizmo.cs`, `ui/PlacementShadows.cs` |

| Missing | Story that builds it |
| --- | --- |
| `WorkshopPartKind.Pipe`, compiled hollow-tube collider (compound cylinder in the shared pair table) | Story 6.6 |
| Rim/collar caps and edge entries (signed-distance torus caps); clear_pipe | Story 6.7 |
| Typed mouth snapping (0.45 m / 20°), idle-construction-only, non-mutating preview | Story 6.6 or 6.7 (UI criterion inside the pipe slice) |
| Physical placement nudging (authored correction that moves the pipe's collider; the depth-error difficulty controls of facts 5 and 18 depend on it) | not scheduled, owner decision: the roadmap reports physical nudging unsupported in the current playable mode ([first_principles](../invest/vertical-delivery.md#first-principles)) |
| Optical transmission through the clear shell, collar occlusion | Epic 13 (Story 13.1) |
| Air blocking by the shell, open bore along the axis (fact 22) | Epic 12 (Story 12.2) |
| f32 declarations: the parked `PipeDimensions` and `AnnularProfile` lanes and the `PipeDimensionsResource` `LengthBits` field are binary16 (`Half`) | Remaining f32 migration ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)); declare in f32 when Story 6.6 admits the pipe |

Dependencies: CAT-001 ball. Consumers: CAT-049/050 bends, CAT-030 funnel, CAT-002 detector and CAT-051 powered gate reuse the tube collider and mouths.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | With zero gravity, a ball started at local (−3, 0.1, 0) moving 4 m/s along the axis of a pipe rotated (0,0,0), (30,40,50) or (0,90,90) travels continuously (< 0.04 m per 1/120 s step) to local x 3 ± 0.01, y 0.1 ± 0.01, speed 4 ± 0.01. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L11-L42` | carry forward (tilted/rotated passage, no energy change). |
| 2 | A ball dropped at 8 m/s onto the outer wall stays outside: local y ≥ 0.70 + radius − 0.001. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L43-L51` | carry forward (sidewall control). |
| 3 | A 0.34 m ball at 40 m/s passes the bore (x 5 ± 0.01 after 24 steps); a 0.8 m ball at 4 m/s is stopped at the mouth (x < −1.89); speed never increases. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L56-L80` | carry forward (high-speed and oversize controls); the per-instance radius property is not current (see §6). |
| 4 | Light: along the axis through the bore is unobstructed (10 m); at y + 0.7 the collar face blocks at 3.10–3.12 m; across the clear shell sideways is unobstructed. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | carry forward (collar optical blocking, clear-wall transmission). |
| 5 | clear_pipe: with the reference pipe moved 0.48 m in depth, the level is won at precision 0 and 0.45 but not 1; with no depth error it is won at all three; Reset replays the same state signature; without the pipe it is never won. | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L98-L138` | carry forward (difficulty controls); the depth-error wins at precision 0 and 0.45 need physical placement nudging (§3, §6). |
| 6 | Resizing to 1, 4.2 or 8 sets tube half length length/2, keeps bore 0.65, moves collars to ±length/2, serializes `length`, artwork bounds length + 0.18, collar optical face at 6 − length/2 − 0.09; Reset keeps length and unit scale; a bore change rejects. | `CuriousContraptions.tests/PipeResizeTests.cs@a6c914e:L10-L40` | carry forward. |
| 7 | Lengths 0, 9 and NaN reject at construction with no part added. | `CuriousContraptions.tests/PipeResizeTests.cs@a6c914e:L42-L57` | carry forward. |
| 8 | Only the local length handle is interactive (Y and Z disabled); a 0.3-axis drag sets 4.2; Cancel restores 3.6 and half length 1.8. | `CuriousContraptions.tests/PipeResizeTests.cs@a6c914e:L59-L89` | carry forward. |
| 9 | Snapping a second pipe 0.1 m off and 0.08 rad rotated: the query is non-mutating; applied, mouths coincide within 1e−4 and face opposite (dot < −0.9999); a ball crosses the seam continuously to local x 7 ± 0.01 at 4 ± 0.01 m/s; a joined mouth is no longer a snap candidate; the placement survives Reset. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L11-L52` | carry forward (continuous seams, occupied mouths). |
| 10 | Snapping is unavailable while Running and while paused (Running false after Start) and never mutates bodies; it returns after Reset; a part from another world never snaps. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L54-L85` | carry forward (idle-construction-only, foreign same-ID parts reject). |
| 11 | Mouths 0.72 m apart (pipes 4.5 m apart) or facing 40° off do not snap; an occupied mouth and a locked part are not candidates. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L87-L121` | carry forward (0.45 m / 20° thresholds per DESIGN). |
| 12 | Resizing is rejected while Running or Paused without changing tubes, saved construction or physics; it applies after Reset. | `CuriousContraptions.tests/ConstructionLifecycleTests.cs@a6c914e:L92-L139` | carry forward. |
| 13 | Occupancy of a hollow tube: a box in the bore, in the end hole or beyond the end is clear; a box in the shell, on the annular rim, surrounding the tube or touching the outside overlaps; the result is invariant under a rigid transform. | `CuriousContraptions.tests/TubeBoxIntersectionTests.cs@a6c914e:L8-L38` | carry forward (geometry facts for any part inside a bore, e.g. powered gate blade). |
| 14 | Parked GPU profile: lengths 1, 3.6, 8 round-trip; one exposed annular profile per pipe; saved lengths 0, 0.5, 9, NaN, +∞ and non-zero padding reject; previous schemas reject; a pipe in the first_principles puzzle rejects; maximum population (8 instances incl. a length-8 pipe) fits capacity. | `CuriousContraptions.tests/WorkshopPipeTests.cs@a6c914e:L14-L136` | carry forward the behaviour; do not carry forward the binary16 lanes, the fixed collider ABI bytes or the `AnnularFeature` enum (deleted by Story 6.7). |
| 15 | Hollow walls were built as convex segments with maximum surface error 0.005 m; tube collars carry an opaque flag, bends and frustums are never opaque. | `engine/SceneCollisionGeometry.cs@a6c914e:L37-L38`; `engine/SceneCollisionGeometry.cs@a6c914e:L75-L77`; `engine/physics/HollowGeometry.cs@a6c914e:L80-L108` | carry forward the opacity facts; do not carry forward the CPU convex segmentation (CPU solver path). |
| 16 | GPU probe fixture matrix for a default pipe and a Basketball: bore-axis fall onto the inner wall; frictionless circumferential motion at constant speed; tilted axial rolling (−0.2 rad); mouth departure to rim/free motion; outer-shell approach hitting the exposed collar shoulder; plane and box rolling controls. | `reference/pipe/Program.cs@a6c914e:L22-L119` | carry forward the fixture matrix; do not carry forward the "shorter trial" subdivision case (CPU-style directed trial) or the WGSL diagnostic shaders in `reference/pipe/` (proof/diagnostic artefacts). |
| 17 | clear_pipe reference: ball (−1.4, 6, 0), receiver (2, 0.6, 0), pipe_1 at (0, 3, 0) rotated −45° about Z, length 3.6. joined_pipe: ball (−1.308, 8.402, 0), fixed bend (1, 3.5, 0) at −45°, receiver (−0.2, 0.6, 0), pipe_1 at (−0.537, 6.031, 0), −45°, length 2. | `tools/Campaign/Program.cs@a6c914e:L279-L320` | carry forward (content in `content/puzzles.json`). |
| 18 | joined_pipe's straight pipe uses the narrow bend assistance windows: position 0.6 / 0.5 m and rotation 5° / 2° with equal correction caps at precision 0 / 0.45. | `tools/Campaign/Program.cs@a6c914e:L564-L573` | carry forward the values; do not carry forward the string `part.Kind` test. |
| 19 | A trampoline rebound traverses the bore only when trampoline and pipe align; a pipe moved 2 m in depth is missed. | `CuriousContraptions.tests/TrampolinePipeTests.cs@a6c914e:L32-L90` | carry forward as an integration (owned with CAT-065). |
| 20 | Workshop UI: the contextual "Resize mode" button is hidden until a pipe (or wall) is placed and selected; pressing it puts the gizmo in resize mode; a Shift-drag of the length handle by half a world unit increases the length by 1 (symmetric about the centre); one "↺ Undo" restores the original length and keeps the part. | `CuriousContraptions.tests/WorkshopInteractionTests.cs@a6c914e:L703-L741` | carry forward (contextual resize, one gesture is one Undo). |
| 21 | Workshop UI: with a locked fixed pipe at (−2, 3, 0), placing a new pipe by clicking near (1.9, 3, 0) snaps it to x 1.78 ± 0.001, y 3 ± 0.001 (mouths touching, 3.78 m apart); one "↺ Undo" removes only the new tube and keeps the fixed one. | `CuriousContraptions.tests/WorkshopInteractionTests.cs@a6c914e:L742-L765` | carry forward (snap on placement, one gesture is one Undo). |
| 22 | A pipe at (0, 4, 0): light crossing the clear shell sideways is unobstructed (5 m); air crossing it sideways is blocked by the shell (1.1–1.4 m); air along the axis through the bore is open (10 m). | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L211-L215` | carry forward as the air/light shell controls (air owed at Story 12.2, light at Story 13.1); the fan/chime fixture itself belongs to CAT-028/CAT-069. |
| 23 | clear_pipe with its authored solution at precision 0 is won: the ball is captured by the Receiver within 1,200 ticks. | `CuriousContraptions.tests/FlightCampaignTests.cs@a6c914e:L15-L40` | carry forward as a level-regression check for clear_pipe (shared with CAT-004). |

Files harvested:
- `engine/gpu/WorkshopPipe.cs` (parked; survives until Story 6.6 decides)
- `engine/gpu/AnnularProfile.cs` (parked; `AnnularFeature` deleted by Story 6.7)
- `parts/PipePart.cs`
- `parts/PipeArt.cs`
- `parts/scenes/pipe.tscn` (script binding only)
- `parts/catalog/pipe.tres`
- `engine/PipeDimensionsResource.cs`
- `engine/TubeMouth.cs`
- `engine/TubeProxy.cs`
- `engine/SceneCollisionGeometry.cs`
- `engine/physics/HollowGeometry.cs`
- `CuriousContraptions.tests/PipeTests.cs`
- `CuriousContraptions.tests/PipeResizeTests.cs`
- `CuriousContraptions.tests/TubePlacementSnapTests.cs`
- `CuriousContraptions.tests/WorkshopPipeTests.cs`
- `CuriousContraptions.tests/ConstructionLifecycleTests.cs`
- `CuriousContraptions.tests/TubeBoxIntersectionTests.cs`
- `CuriousContraptions.tests/TrampolinePipeTests.cs`
- `CuriousContraptions.tests/FlightCampaignTests.cs`
- `CuriousContraptions.tests/WorkshopInteractionTests.cs` (L703-L765)
- `CuriousContraptions.tests/WindChimeTests.cs` (L211-L215)
- `tools/Campaign/Program.cs`
- `reference/pipe/` (Program.cs; 64 WGSL diagnostics, three inputs JSON files and the csproj hold no further element knowledge)

## 5. Acceptance outline

Acceptance: [CAT-048](../requirements.md#current-cat-048) and [retained behaviour](../requirements.md#todo-125).

- **Construction.** clear_pipe: place the Clear pipe from inventory, rotate it downhill under the ball, Run. Free workshop: select the pipe, press "Resize mode", resize through the single X handle; Cancel; Undo.
- **Positive.** Ball travels the bore without snagging, speed, teleport or falling through walls; reaches the receiver.
- **Negative/control.** Outer-wall drop stays outside; oversize payload stops at the mouth; depth error at Precise fails; no pipe, no win.
- **Boundaries.** Length 1 and 8; tilted/rotated/high-speed passage; collar optical blocking versus clear shell.
- **Snapping.** 0.45 m / 20° typed mouth snapping; occupied, wrong-bore and non-opposed ends reject; idle construction only (rejected while running or paused); foreign same-ID parts reject; preview is non-mutating, keeps roll through the shortest alignment and creates no transport-network edge; separation and rejoin; one Undo removes only the new tube.
- **Run/Reset, Save/Load.** Length and pose restored exactly.
- **Integrations.** Bends (CAT-049/050), funnel (CAT-030), detector (CAT-002), trampoline (CAT-065).
- **Remaining (unmet now).** The Forgiving/Balanced depth-error wins (facts 5, 18) depend on physical placement nudging, which is not scheduled; air and light shell controls are integration proofs owed at Stories 12.2 and 13.1.

## 6. Open questions

- Oversize payload for the mouth control: the legacy used a per-instance 0.8 m ball radius; current balls have fixed per-kind radii (0.34 and 0.28 m, both fit the 0.65 m bore). Which declared body is the oversize control: unspecified — owner decision.
- Wrong-bore snapping control: every legacy tube shares one bore; which part provides a different bore (e.g. EL-102 Large-bore ball pipe or the funnel's wide inlet): unspecified — owner decision.
- Whether the parked `engine/gpu/WorkshopPipe.cs` and `AnnularProfile.cs` are kept as Story 6.6's starting point or deleted in Story 7.4: unspecified — owner decision (TODO says preserve Pipe).
- Physical placement nudging: not scheduled, owner decision. The requirement keeps "all difficulty controls" for the pipe, but the depth-error wins at Forgiving/Balanced need nudging that the roadmap reports unsupported; which story builds it is unspecified.
