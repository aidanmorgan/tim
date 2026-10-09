# CAT-030 · funnel — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-030 · `funnel` |
| Requirement | [CAT-030](../requirements.md#current-cat-030) |
| Mapped identities | none directly. EL-005 Liquid funnel (Batch F) is a separate liquid element. |
| Roadmap | Story 6.11 (added by Story 7.0; after the bends because its acceptance includes straight/bend joins). Air blocking and light transmission are integration proofs owed at Stories 12.2 and 13.1; CAT-030 stays open until then. |
| Status | not started on the current engine (legacy `parts/FunnelPart.cs` is uncompiled) |
| Levels | none in `content/puzzles.json`; Free workshop only |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One static hollow frustum along local X: half length 0.9 m; inlet inner radius 1.3 m (mouth 2.6, twice the bore); outlet inner radius = the common bore 0.65 m (1.3); radial shell thickness 0.05 m `parts/FunnelPart.cs@a6c914e:L9-L11`, `parts/FunnelPart.cs@a6c914e:L18-L22`, `engine/FrustumProxy.cs@a6c914e:L5-L6`. Two opaque end collars at x = ∓0.9: half width 0.09, from the mouth radius to radius + 0.13 `parts/FunnelPart.cs@a6c914e:L23-L30`. The legacy bore was `PipePart.BoreRadius`; the current value is `PipeDimensions.BoreRadius` 0.65 m `engine/gpu/WorkshopPipe.cs@a6c914e:L9-L10`. |
| Material | restitution 0.15, bounce threshold 0.1 m/s, friction 0.3 `parts/FunnelPart.cs@a6c914e:L12-L12`. |
| Constraints and joints | none (static). |
| Sockets and ports | none. Typed tube mouths: Start (wide inlet) at (−0.99, 0, 0) facing −X with radius 1.3; End (outlet) at (0.99, 0, 0) facing +X with the common bore `parts/FunnelPart.cs@a6c914e:L13-L17`. Only the outlet matches standard tubes. |
| Sensors and activation | none. |
| Work and energy stores | none (no attraction, no scripted transport). |
| Parameters | none; no resize controls `DESIGN.md@a6c914e:L294-L294`. |
| Cosmetic curves and UI bindings | none. |
| Art | Transparent frustum shell (0.40, 0.72, 0.79, alpha 0.16), 48 sides, double-sided, no shadow `parts/PipeArt.cs@a6c914e:L7-L38`; cream `#fff8e9` collars; two navy `#293954` rails of radius 0.018 from (−0.9, 0, ±1.35) to (0.9, 0, ±0.70) (`PartArt.Line` width is the cylinder radius `engine/PartArt.cs@a6c914e:L24-L30`); pick radius 1.75 `parts/FunnelPart.cs@a6c914e:L18-L34`. Scene `parts/scenes/funnel.tscn`. Icon `ui/WorkshopIcons.cs@a6c914e:L85-L85`. Design rule `DESIGN.md@a6c914e:L294-L294`. |
| Catalogue and inventory | `parts/catalog/funnel.tres@a6c914e:L6-L13`: id `funnel`, title Funnel, category Motion, colour (0.40, 0.72, 0.79). |

Current tests: none compiled.

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| Sphere contact with static shapes | Stories 1.1–1.5 |

| Missing | Story that builds it |
| --- | --- |
| Hollow frustum (tapered annular) collider with rim caps | Story 6.11, reusing Stories 6.6/6.7 collider families |
| Typed mouth snapping (outlet only) | Stories 6.6/6.7 (CAT-048) |
| Optical transmission through the clear wall; opaque collar | Epic 13 (Story 13.1); integration proof owed there |
| Air blocking by the solid shell (fact 10) | Epic 12 (Story 12.2); integration proof owed there |

Dependencies: CAT-048 (bore, collars, snapping, hollow collider), CAT-049/050 for the bend-join controls; CAT-028 Fan and CAT-029 Flashlight for the air and light integration proofs.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The funnel is passive: the sloping shell redirects actual sphere contacts. | `parts/FunnelPart.cs@a6c914e:L6-L6` | carry forward. |
| 2 | Inlet pointing up (funnel at (0, 5, 0) rotated −90°), balls dropped from (0, 8, 0) and with ±0.75 m offset all exit through the narrow mouth: crossing local x 1.34 within 0.36 m of the axis; no step jumps 0.34 m or more; Reset restores the drop position. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L29-L61` | carry forward (inlet/wall/outlet passage). |
| 3 | With zero gravity, a centred ball at 4 m/s (unrotated) or 40 m/s (rotated 20/30/40°) travels from local x −2 to 1.99–2.34 m with speed unchanged ± 0.003 m/s. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L63-L82` | carry forward (high-speed and rotated payload; no energy added). |
| 4 | A funnel at (0, 7, 0) feeding a straight pipe at (0, 4.12, 0), both rotated −90°: a ball dropped 0.75 m off axis exits the pipe below y 1.8 within 0.4 m of the axis. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L84-L110` | carry forward (straight join). |
| 5 | A pipe at (2.95, 5, 0) snaps to the outlet; the same pipe at (−2.95, 5, 0) does not snap to the wide inlet; the inlet mouth radius equals 1.3. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L112-L126` | carry forward (compatible tube mouths; wrong-bore rejection). |
| 6 | Light along z through the glass is unobstructed for 6 m; a ball dropped onto the outside at 4 m/s never overlaps the shell and gains no speed. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L128-L151` | carry forward (clear wall transmits light; solid shell blocks bodies). |
| 7 | Occupancy of a frustum: a box in the bore, inlet hole, outlet hole or beyond the end is clear; boxes in the shell, on the inlet or outlet rim, or surrounding it overlap; invariant under rigid transforms. | `CuriousContraptions.tests/FrustumBoxIntersectionTests.cs@a6c914e:L8-L39` | carry forward (rim and opening geometry). |
| 8 | The frustum wall was a ring of convex segments within 0.005 m surface error; frustums are never optically opaque (collars are opaque tubes). | `engine/physics/HollowGeometry.cs@a6c914e:L83-L108`; `engine/SceneCollisionGeometry.cs@a6c914e:L75-L77` | carry forward the opacity facts; do not carry forward the CPU segmentation. |
| 9 | Test fixtures use an enum role → catalogue string boundary that rejects undefined roles. | `CuriousContraptions.tests/FunnelTests.cs@a6c914e:L11-L21` | carry forward the typed boundary rule only. |
| 10 | Solid shell blocks air: a funnel rotated (20, 35, 10) blocks an air trace from 2 m outside its wall at 0.7–1.4 m while light passes the full 4 m. The same fixture is captured for the fan in CAT-028 (`WindChimeTests.cs` L212-L237). | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L220-L237` | carry forward as the "solid shell blocks air" control (owed at Story 12.2) and the light control (owed at Story 13.1). |

Files harvested:
- `parts/FunnelPart.cs`
- `parts/PipeArt.cs` (frustum mesh)
- `parts/scenes/funnel.tscn` (script binding only)
- `parts/catalog/funnel.tres`
- `engine/FrustumProxy.cs`
- `engine/TubeMouth.cs`
- `engine/physics/HollowGeometry.cs`
- `engine/SceneCollisionGeometry.cs`
- `CuriousContraptions.tests/FunnelTests.cs`
- `CuriousContraptions.tests/FrustumBoxIntersectionTests.cs`
- `CuriousContraptions.tests/WindChimeTests.cs` (L220-L237; cross-reference to CAT-028)

## 5. Acceptance outline

Acceptance: [CAT-030](../requirements.md#current-cat-030) and [retained behaviour](../requirements.md#todo-242); story [6.11](../../../_bmad-output/planning-artifacts/epics.md).

- **Construction.** Free workshop: place Funnel inlet-up, snap a straight pipe (and separately a bend) to its outlet, drop a ball.
- **Positive.** Centred and offset drops leave through the outlet into the joined tube under gravity and contact only.
- **Negative/control.** Outside drop is blocked by the shell; a pipe offered to the wide inlet does not snap; oversize payload stops at the outlet.
- **Boundaries.** Rim strikes at inlet and outlet; 40 m/s rotated passage.
- **Run/Reset, Save/Load.** Pose restored exactly.
- **Integrations.** Straight pipe (CAT-048), bends (CAT-049/050); mesh and contact geometry match.
- **Owed integration proofs (CAT-030 stays open).** Solid shell blocks air (fact 10) at Story 12.2; clear wall transmits light except the opaque collar (facts 6, 10) at Story 13.1.

## 6. Open questions

- No campaign level uses the funnel at `a6c914e`; whether Story 6.11 needs an authored level or proves the element in the Free workshop only: unspecified — owner decision.
- Oversize payload: see CAT-048 §6: unspecified — owner decision.
