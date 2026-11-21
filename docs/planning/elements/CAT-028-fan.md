# CAT-028 · fan — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-028 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline.

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-028 · `fan` |
| Requirement anchor | [CAT-028](../requirements.md#current-cat-028); D/I/V row [CAT-028-I](../invest/current-consumers.md#cat-028-i) |
| Mapped identities | [EL-209 Fan](../invest/named-elements.md#element-209). No TH, RAD or GAP identity. |
| Capability row | [general-engine-element-map, CAT-028](../general-engine-element-map.md) |
| Roadmap story | Epic 12, Story 12.2 "Electric Fan Aerodynamic Airflow Jet"; the first slice of the [gas and airflow family](../../finite-gas-foundation.md) Current [CAT-028 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Harvest-baseline status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## Current Story 12.2 contract (owner decisions, 19–20 November 2026)

One catalogue `fan` exposes Self-contained (default), Electrical and Shaft supply selections. Self-contained retains the activation latch, authored powered state and finite 14,400 J store; Electrical draws exposed accepted work directly from its connected finite supply; Shaft converts actual shaft work ideally, with reaction load and no additional conversion loss. Electrical and Shaft have no hidden construction reservoir. Their active input is required; inactive input connections make a mode edit refuse atomically until disconnected. Supply selection is Build-only. Motor electrical drain remains deferred: a Motor integration proves paid shaft-to-airflow work, not paid battery-to-shaft conversion.

The field is a strict cone: for axial distance x, 0 < x < reach and radial distance r < width × x/reach; width is terminal radius. Force-rated mass-dependent transfer uses global 64 m/s² linear and 1024 rad/s² angular bounds, scaling the complete paid transfer and paired reactions together. No extra 16 m/s² transfer cap applies; that bound belongs to directly authored acceleration fields. Retain force 0–40 N (default 9), reach (0,12] m (5), width (0,4] m (0.85), flow 12 m/s and power force×12 W. Retain sourced static contact material. The store is an energy budget, not a fixed 133-second timer.

Native script-free Godot artwork and explicitly owned C# components/resources remain required. Non-shaft spin binds committed flow through a generic rate evaluator; Shaft artwork follows its exact committed coordinate. This current contract supersedes conflicting cylindrical/no-external-input and incidental-timing prose below; harvest rows and citations remain historical evidence. Rotary receiver integration belongs to 12.3, chimes to 14.4 and campaign sweeps to their later gate. See the [bounded native plan](../../../_bmad-output/implementation-artifacts/spec-12-2-fan.md).

## Declaration

- **Bodies and shapes.** Static housing/base plus one dynamic spherical hub carried by a continuous revolute hinge in all supply modes; Shaft selects that exact coordinate as its work input, while Electrical/Self-contained leave it passive. Collider: root box centred at (0, −0.55, 0), size 0.65 × 0.18 × 1 m (legacy `AddBox` makes a root box with half-extents = size/2, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`; `parts/FanPart.cs@a6c914e:L36-L36`). The additional solid hub proxy is a sphere of radius 0.14 m at the local origin on its own body. Blades, ring, post and arrow are non-contact artwork.
- **Mass and material.** Housing/base is static with no dynamic mass. Hub is 0.15 kg with isotropic inertia 2mr²/5 = 0.001176 kg·m² and its own contact proxy; hub material is restitution 0, threshold 0.1 m/s, friction 0.3, rolling resistance 0. The housing retains the legacy static default restitution 1, threshold 0.1, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`); retain this sourced material under the resolved current contract.
- **Constraints and joints.** Continuous +X hinge with coincident local hub frames, zero initial angle/rate, no drive and zero bearing friction. Shaft art follows the exact physical coordinate and pays accepted load work; other modes leave the hub passive and apply committed-flow spin only to non-contact blade art. The spherical hub remains physically posed/contact-bearing in every mode.
- **Typed sockets and ports.** One `ActivationIn` (activation domain, input) at the local origin; legacy derives it from `CanReceiveActivation` (`parts/FanPart.cs@a6c914e:L18-L18`; `reference/cpu/MachinePart.cs@a6c914e:L247-L254`). Current enum: `WorkshopSocket.ActivationIn` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`). PowerIn and DriveIn are explicit supplied-mode ports; only the selected input is active. Inactive connections refuse atomically.
- **Sensors and activation.** A Trigger latches the fan on and records an Activated occurrence (`reference/cpu/MachineWorld.cs@a6c914e:L734-L737`); nothing switches it off during a Run.
- **Work and energy stores.** Self-contained selects one finite reservoir, 14,400 J, initial = capacity; supplied modes cannot draw it (`parts/FanPart.cs@a6c914e:L12-L16`).
- **Airflow source.** While active: origin at local (0, 0, 0), direction local +X, finite cone of length `reach` and terminal radius `width`; flow speed 12 m/s; force rating `force`, power rating `force` × 12 W; receiver impedance conductance `force`/12 N·s/m capped at `force` N (`parts/FanPart.cs@a6c914e:L19-L23`; `engine/AirflowNetwork.cs@a6c914e:L12-L13`, `L23-L26`).
- **Internal airflow occlusion.** Declare exact LocalColliderReference entries for the base collider and hub collider, resolved to same-instance GpuColliderIds. Only those source-internal solids and the exact sampled receiver collider are excluded from streamlines. Other same-owner solids and all external solids block normally. Reject missing, duplicate, cross-instance or wrong-kind references atomically; no owner-wide exclusion or Fan identity branch. The centered Tennis case at x=2 m must transfer paid work with the real hub present.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit |
  | --- | --- | --- | --- | --- |
  | supply | typed choice | Self-contained, Electrical, Shaft | Self-contained | — |
  | powered | enum Off/On (Self-contained only) | Off or On | On | — |
  | force | f32 | 0–40 | 9 | N |
  | reach | f32 | (0, 12] | 5 | m |
  | width | f32 | (0, 4] | 0.85 | m (terminal cone radius) |

  Ranges `parts/FanPart.cs@a6c914e:L24-L31`; defaults `parts/catalog/fan.tres@a6c914e:L14-L14`. `powered` becomes an enum at the boundary (string-typed closed sets are not carried forward).
- **Cosmetic curves and UI bindings.** Non-shaft rotor phase integrates committed available flow with package scale 18/12 rad per metre (18 rad/s at 12 m/s), linear looping on world time; Shaft uses exact physical coordinate instead. Historical rotor recipe (`parts/FanPart.cs@a6c914e:L50-L53`). The gas doc names the target binding "Blade spin rate ← committed source flow".
- **Art.** Ring "thickness" values are torus half-widths (inner r − t, outer r + t) and line widths are cylinder radii (`engine/PartArt.cs@a6c914e:L22-L27`). Base box `#263d4b`; post 0.13 × 0.5 × 0.15 `#ccd8dc` at (0, −0.3, 0); housing torus radius 0.56, half-width 0.07 (inner 0.49, outer 0.63), axis X, catalogue colour; rotor hub sphere of radius 0.14 `#f4d089`; three blades 0.1 × 0.6 × 0.16 at 0°, 120°, 240° about X, catalogue colour lightened 15%; airflow arrow line (0.6, 0, 0)–(1.15, 0, 0) `#a9e7e0` of radius 0.025; pick radius 0.75 (`parts/FanPart.cs@a6c914e:L32-L49`). Catalogue colour `#66b8c9` (`DESIGN.md@a6c914e:L172-L172`); icon `ui/WorkshopIcons.cs@a6c914e:L51-L51`. The historical scene held a script reference; the current native .tscn is script-free, with explicit collision proxies and owned C# behavior/resources.
- **Catalogue and inventory entry.** Id `fan`, title "Fan", category Power, description "A fan blows along its arrow. Choose Self-contained activation, Electrical input or Shaft input. Solid obstacles block the cone." (`parts/catalog/fan.tres@a6c914e:L8-L14`).

## Engine capabilities

Families: the [CAT-028 row](../general-engine-element-map.md) and `docs/coverage/catalogue-elements.json` (AerodynamicDrag, AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction).

- **Exists now.** Static box colliders, dynamic receiver bodies with declared drag (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`); activation input and activation wiring (`engine/gpu/ActivationNetwork.cs`, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`); 60 Hz animation worker with activation-driven cosmetic curves (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).
- **Missing.**
  - Airflow source, finite jet geometry with occlusion, receiver impedance, shared finite reservoir and paired reactions: Story 12.2. Decision owner S470 ([decisions](../invest/decisions.md#s470)), row "open-versus-sealed" → S697.
  - EnvironmentState pressure scaling of receiver conductance: Story 12.1.
  - A looped spin cosmetic driven by an active state (the current feedback sources are Activation, Timer, ContactWork, Capture): Story 12.2.
  - ElectricalPower: Story 8.1 (CAT-005) supplies routing/storage patterns; Story 12.2 adds generic exposed continuous demand with shared charging arbitration.
  - Rotary receivers: Story 12.3 (CAT-070).
- **Element dependencies.** Tennis ball (CAT-064) as the light payload, Bowling ball (CAT-014) as the heavy control; Switch (CAT-063) for the cold-start lessons; Wall (CAT-066) for occlusion.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Jet exposure: a receiver sample is exposed only when 0 < along-axis distance < reach and radial distance < width, both bodies participate, and no other non-excluded collider intersects the streamline from the nozzle plane to the sample. Source and target owner bodies are excluded. | `engine/physics/AirJetGeometry.cs@a6c914e:L110-L141`; `engine/AirflowNetwork.cs@a6c914e:L64-L74` | Carry forward as the field geometry. Do not carry forward the CPU sweep code. |
| 2 | Jet force law: force = min(cap, conductance × (12 − v)), zero when the receiver's along-axis speed v ≥ 12; source power = force × 12; dissipated = force × slip. | `engine/physics/JetTransferImpedance.cs@a6c914e:L26-L46` | Carry forward. |
| 3 | Conductance and cap are multiplied by world pressure × sample weight; sample weights must be positive, finite and sum to 1; body-force samples require a dynamic body. | `engine/AirflowNetwork.cs@a6c914e:L44-L61`, `L77-L79` | Carry forward. |
| 4 | One finite reservoir funds every receiver of a source jointly; allowances scale down to energy / dt and never overdraw the store. | `engine/physics/StoredFlowSource.cs@a6c914e:L28-L72` | Carry forward as the finite-store law. |
| 5 | Every receiver reaction returns to the nozzle (momentum preserved); no ambient exchange and no unloaded thrust. | `engine/physics/NozzleCoupledJetReceiver.cs@a6c914e:L7-L10` | Carry forward. |
| 6 | Sources are evaluated in deterministic order; a source never acts on its own part. | `engine/AirflowNetwork.cs@a6c914e:L33-L36`, `L64-L64` | Carry forward the determinism; not the string-ordinal ordering. |
| 7 | Acceptance: the default fan delivers 9 N to a ball on its axis 3 m away and 0 N at 2 m off-axis; one tick later the 1 kg Basketball moves at 0.074–0.076 m/s (gravity 0). | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L191-L219` | Carry forward. |
| 8 | Acceptance: a clear pipe shell blocks air (trace 1.1–1.4 m) but not light (5 m); curved bends and the funnel also block air while staying optically clear. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L212-L237`; `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79` | Carry forward: air is blocked by every solid collider, light and sound only by opaque ones. |
| 9 | Acceptance: the same jet accelerates a heavier ball less; with a saturated shared 9 N source each receiver's force is proportional to its remaining slip (12 − v); body drag = drag × pressure. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L238-L279` | Carry forward. |
| 10 | Acceptance: two facing fans cancel on a receiver between them; zero pressure gives zero air force. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L303-L319` | Carry forward. |
| 11 | Acceptance: invalid parameters reject (powered 0.5, force −1, reach 0, width NaN). | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L356-L372` | Carry forward. |
| 12 | Acceptance: one or two balls share the 14,400 J reservoir; an unpowered fan creates no transfer; total kinetic energy never exceeds released energy; replay after Reset is identical and the save restores exactly. | `CuriousContraptions.tests/LinearAirflowSupplyTests.cs@a6c914e:L24-L75` | Carry forward. The supplied/error-bound ledger is a proof-grade certificate: do not carry forward. |
| 13 | Acceptance: one fan (width 3) funds one rotor, two rotors, or a rotor and a ball; unpowered funds none. | `CuriousContraptions.tests/RotorAirflowSupplyTests.cs@a6c914e:L25-L112` | Carry forward (Story 12.3 integration). |
| 14 | The rotor artwork does not move during physics ticks; it spins at 18 rad/s on presentation; paused presentation holds; a fan started by activation begins spinning only from the committed tick after activation. | `CuriousContraptions.tests/SceneAnimationRunTests.cs@a6c914e:L32-L91` | Carry forward as the cosmetic binding contract. |
| 15 | Solved poses, not rendered transforms, set the jet; disabling the fan body removes the force at once. | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L260-L283` | Carry forward. |
| 16 | Lessons: fan in inventory for air_mail, third_dimension, wind_signal and 11 combined levels; fixed and unpowered (`powered` 0) in cold_start, powered_post, start_and_topple, cold_front, relay_workshop, double_cold_start, cold_signals, cold_bridges. Canonical solution at (−4, 3.8, 0) facing +X, 1 m left of and 1.2 m below the tennis ball. | `content/puzzles.json@a6c914e:L495-L706`, `L2093-L2444`; [CAT-028-I](../invest/current-consumers.md#cat-028-i) | Carry forward. |
| 17 | Cold-start pattern: the fixed fan starts off; a Bowling ball at (−5.8, 2.4, 0) falls on a Switch at (−5.8, 1.6, 0) wired to the fan; goals are capture and fan activated. | `tools/Campaign/Program.cs@a6c914e:L64-L75`, `L372-L374` | Carry forward. |
| 18 | Difficulty assistance: fan position step 0.35 m, rotation step 15°, blend 0.15 s, because a fan must settle before a nearby falling ball reaches the jet (about 0.27 s). | `tools/Campaign/Program.cs@a6c914e:L544-L549` | Carry forward to Epic 15 difficulty data. |
| 19 | Transaction-rollback test (a failed observation restores the airflow reading). | `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs@a6c914e:L32-L57` | Do not carry forward: the current engine never faults a tick (clamp-or-continue). Keep only "Reset is exact". |
| 20 | Impedance worked examples (conductance, cap, source, receiver → force, source power, receiver power, loss): 2, 100, 10, 4 → 12, 120, 48, 72; cap 5 → 5, 50, 20, 30; receiver −4 → 28, 280, −112, 392; source 0, receiver −4 → 8, 0, −32, 32; receiver 10 or 14 → 0; conductance 0 or cap 0 → 0. Source power − receiver power = loss ≥ 0. | `CuriousContraptions.tests/JetTransferImpedanceTests.cs@a6c914e:L8-L23` | Carry forward as law acceptance. |
| 21 | Each receiver branch adds its own reaction on the source; supply is never copied between branches. Invalid materials, speeds and unrepresentable slip or power reject. | `CuriousContraptions.tests/JetTransferImpedanceTests.cs@a6c914e:L25-L79` | Carry forward. |
| 22 | Field boundaries are strict: with reach 5 and width 1, samples at the nozzle plane or behind it, at or beyond along-axis 5, or at or beyond radius 1 receive no force. A sample 0.5 off-axis receives 6 N along the rotated axis and a 3 N·m moment (r × F), in any frame. Moving, disabled, replaced and excluded blockers use their current declarations. | `CuriousContraptions.tests/JetReceiverFieldTests.cs@a6c914e:L34-L92` | Carry forward. |
| 23 | Exposure and occlusion queries neither need nor create supply; an unpowered load has zero demand and changes no body. | `CuriousContraptions.tests/AirJetGeometryTests.cs@a6c914e:L8-L33` | Carry forward. |
| 24 | A blocker crossing the jet removes the force for the crossing interval only; only exposed work is committed; a moving receiver or rotating nozzle changes occlusion. | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L14-L90` | Carry forward the behaviour at substep resolution. The exact sub-step crossing-time sweeps are a CPU continuous-sweep mechanism: do not carry forward. |
| 25 | An offset linear or rotary receiver returns force and total moment to the nozzle body; mixed receivers share one supply and balance a moving nozzle; an empty store moves nothing. | `CuriousContraptions.tests/NozzleCoupledJetReceiverTests.cs@a6c914e:L13-L110` | Carry forward. |
| 26 | A stored-flow source never supplies more than its store; an empty store supplies nothing; sources sharing one reservoir split it; recharge never reuses spent work; a collision mid-step debits only accepted work. | `CuriousContraptions.tests/StoredFlowWorldTests.cs@a6c914e:L11-L204` | Carry forward the behaviour. Residual counters, ledger totals and failure rollback are certificates or faulting-tick mechanics: do not carry forward. |
| 27 | Inlet, outlet and rim crossings on moving sources and receivers are found within a step; a moving windmill uses field events rather than direct body loads. | `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L1-L213` | Carry forward only "force starts and stops when a sample crosses the jet boundary". The continuous event sweep is a CPU solver path: do not carry forward. |
| 28 | Certified force-rate bounds over saturation and zero-slip corners. | `CuriousContraptions.tests/JetTransferForcePathTests.cs@a6c914e:L1-L134`; `engine/physics/JetTransferForcePath.cs@a6c914e:L1-L75` | Do not carry forward: proof-grade certificates. |
| 29 | Cylindrical-surface and streamline paths on captured motion for the sweep. | `engine/physics/AirJetBoundaryPath.cs@a6c914e:L1-L80`; `engine/physics/AirJetStreamlinePath.cs@a6c914e:L1-L45` | Do not carry forward: CPU continuous-sweep solver paths; the geometry they sweep is fact 1. |

**Files harvested:**
- `parts/FanPart.cs`
- `parts/catalog/fan.tres`
- `parts/scenes/fan.tscn` (no element knowledge: script reference only)
- `engine/AirflowNetwork.cs`
- `engine/PartArt.cs` (ring half-width and line radius conventions)
- `engine/physics/AirJetGeometry.cs`
- `engine/physics/JetTransferImpedance.cs`
- `engine/physics/StoredFlowSource.cs`
- `engine/physics/NozzleCoupledJetReceiver.cs`
- `engine/physics/BodyQueryGeometry.cs`
- `engine/physics/AirJetBoundaryPath.cs` (do not carry forward: CPU sweep path)
- `engine/physics/AirJetStreamlinePath.cs` (do not carry forward: CPU sweep path)
- `engine/physics/JetTransferForcePath.cs` (do not carry forward: rate certificates)
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/WindChimeTests.cs`
- `CuriousContraptions.tests/LinearAirflowSupplyTests.cs`
- `CuriousContraptions.tests/RotorAirflowSupplyTests.cs`
- `CuriousContraptions.tests/SceneAnimationRunTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs` (do not carry forward: rollback)
- `CuriousContraptions.tests/JetTransferImpedanceTests.cs`
- `CuriousContraptions.tests/JetReceiverFieldTests.cs`
- `CuriousContraptions.tests/AirJetGeometryTests.cs`
- `CuriousContraptions.tests/AirJetOcclusionTests.cs` (behaviour carried; sweep timing not)
- `CuriousContraptions.tests/NozzleCoupledJetReceiverTests.cs`
- `CuriousContraptions.tests/StoredFlowWorldTests.cs` (behaviour carried; ledgers and rollback not)
- `CuriousContraptions.tests/AirJetBoundaryTests.cs` (do not carry forward the sweep; boundary behaviour only)
- `CuriousContraptions.tests/JetTransferForcePathTests.cs` (do not carry forward: certificates)
- `content/puzzles.json` (kept)
- `tools/Campaign/Program.cs`

## Acceptance outline

Point of truth: [CAT-028](../requirements.md#current-cat-028), [simulation and element acceptance](../requirements.md#accept-simulation) and the [first airflow slice](../../finite-gas-foundation.md).

- **Chrome construction.** Rebuild `air_mail` through the real palette: place the Fan with the move gizmo left of and below the locked Tennis ball, facing +X; rotate with the rotation ring for depth lanes.
- **Positive.** Run: the tennis ball is pushed along the fan axis into the Receiver (Solved); the rotor spins while active.
- **Negative or control.** Fan unpowered (and never triggered), fan facing away, a Wall between fan and ball, reach exceeded, and a Bowling ball in place of the tennis ball each leave the ball undelivered. Two facing fans cancel.
- **Occlusion controls.** Full block: a Wall across the jet gives zero force. Partial block: one of four equal body receiver samples blocked leaves 3/4 uncapped demand; the Windmill rotor form remains 12.3 integration (CAT-070 fact 6). Clear shell: a transparent pipe, bend or funnel shell between fan and ball blocks the air although light passes (fact 8).
- **Boundaries.** At axial distance x, radial distance at or beyond `width` × x/`reach`, and x at/beyond either endpoint, give zero force; the reservoir is finite; parameter limits reject.
- **Run/Reset.** Reset restores construction, reservoir and rotor art exactly.
- **Save/Load.** Parameters and placement survive save, reload and Load.
- **Integrations.** Switch-started cold fan (cold_start); windmill and wind chimes receivers (Stories 12.3, 14.4).

## Resolved planning decisions

All five historical questions are resolved by the current Story 12.2 contract above: selectable supply, cone, force-rated global envelope, retained material and finite energy budget. Historical arithmetic/citations are retained as evidence; no readiness or implementation approval is implied.
