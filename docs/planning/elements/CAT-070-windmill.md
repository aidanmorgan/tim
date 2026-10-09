# CAT-070 · windmill — declaration readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline.

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-070 · `windmill` |
| Requirement anchor | [CAT-070](../requirements.md#current-cat-070) and its [retained behaviour](../requirements.md#sequence-task-311); D/I/V row [CAT-070-I](../invest/current-consumers.md#cat-070-i) |
| Mapped identities | None. No EL, TH, RAD or GAP identity names the windmill. |
| Capability row | [general-engine-element-map, CAT-070](../general-engine-element-map.md) |
| Roadmap story | Epic 12, Story 12.3 "Windmill Rotor Aerodynamic Capture & Drive"; [rotary capture](../../finite-gas-foundation.md) in the gas family |
| Status | Not started. |

## Declaration

- **Bodies and shapes.**
  - Static root body: base box at (0.25, −1.08, 0) size 1.1 × 0.16 × 0.9; tower box at (0.25, −0.55, 0) size 0.22 × 1.1 × 0.25; nacelle box at (0.25, −0.1, 0) size 0.65 × 0.45 × 0.55 (half-extents = size/2); guard tube (annulus) at (−0.12, 0, 0), axis X, inner radius 0.745, outer 0.815, half-length 0.035, opaque (`parts/WindmillPart.cs@a6c914e:L75-L77`, `L82-L82`; `engine/TubeProxy.cs@a6c914e:L6-L6`; `reference/cpu/MachinePart.cs@a6c914e:L352-L356`).
  - Dynamic rotor body at (−0.12, 0, 0): hub sphere radius 0.14 plus four blade boxes, half-extents (0.0375, 0.26, 0.105), centred 0.4 m out on arms 90° apart, each blade yawed 30° (pitch) about its arm (`parts/WindmillPart.cs@a6c914e:L19-L19`, `L83-L92`). Excluded from static queries (`parts/WindmillPart.cs@a6c914e:L28-L28`).
- **Mass and material.** Rotor 1 kg, radius 0.7, thickness 0.075; axial inertia m r²/2 = 0.245 kg·m², transverse m(3r² + t²)/12 (`parts/WindmillPart.cs@a6c914e:L22-L27`). Contact material: the legacy default (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`).
- **Constraints and joints.** One hinge between rotor and root at (−0.12, 0, 0); the joint frame rotates local Z onto the part's X axis (rotation vector (0, π/2, 0)); both directions; connected-body collision disabled (`parts/WindmillPart.cs@a6c914e:L20-L21`, `L30-L35`).
- **Typed sockets and ports.** One `Drive` port, mechanical domain, output, at (0.25, −0.1, 0.52), bound 1:1 to the rotor hinge (`parts/WindmillPart.cs@a6c914e:L53-L57`). It cannot accept a drive (`CuriousContraptions.tests/WindmillTests.cs@a6c914e:L280-L298`).
- **Sensors and activation.** A `Turned` occurrence once the rotor's accumulated angular travel reaches 2π (`parts/WindmillPart.cs@a6c914e:L69-L69`). Active while |shaft speed| > 1e-6 rad/s (`parts/WindmillPart.cs@a6c914e:L68-L68`).
- **Work and energy stores.** None; the rotor's kinetic energy is its only store.
- **Airflow receiver.** Four rotary samples at (−0.12, ±0.42, 0) and (−0.12, 0, ±0.42), weight 0.25 each. They are declared on the static root body, not the rotor, so they stay fixed in the part frame and do not turn with the blades (`parts/WindmillPart.cs@a6c914e:L36-L41`, `L38-L39`). Rotary capture material: maximum pitch (torque arm) 0.4 m, speed per force = `radians_per_force`, maximum unloaded speed 12 rad/s, cut-in 0.05 N, force tolerance 1e-7 (`parts/WindmillPart.cs@a6c914e:L16-L18`, `L42-L44`; `engine/AirflowNetwork.cs@a6c914e:L26-L26`).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit |
  | --- | --- | --- | --- | --- |
  | radians_per_force | f32 | 0.1–4 | 0.6666667 | rad/s per N |

  Range `parts/WindmillPart.cs@a6c914e:L58-L62`; default `parts/catalog/windmill.tres@a6c914e:L12-L12`; enum name `parts/WindmillPart.cs@a6c914e:L9-L9`.
- **Cosmetic curves and UI bindings.** The rotor art follows the committed rotor pose; the output pulley art turns at −(shaft angle) about Z (`parts/WindmillPart.cs@a6c914e:L70-L70`). Gas doc: "Blade/hub pose ← committed hinge angle; no cosmetic spin".
- **Art.** Ring "thickness" values are torus half-widths: inner r − t, outer r + t (`engine/PartArt.cs@a6c914e:L22-L23`). Base `#293954`; tower and nacelle `#fff8e9`; axle cylinder radius 0.11, length 0.55, `#e8b764`, at (0.14, 0, 0) along X; guard torus radius 0.78, half-width 0.035 (inner 0.745, outer 0.815, matching the guard collider), `#fff8e9`; blades 0.075 × 0.52 × 0.21 `#66b8c9` with 0.08 × 0.07 × 0.21 `#e8b764` tips at 0.63; hub sphere of radius 0.14 `#e8b764`; output pulley at (0.25, −0.1, 0.44): wheel radius 0.24, thickness 0.12, `#fff8e9`, with a 0.38 × 0.05 × 0.035 `#e8b764` mark; pick radius 1.2 (`parts/WindmillPart.cs@a6c914e:L72-L98`). Catalogue colour (0.4, 0.72, 0.79) (`parts/catalog/windmill.tres@a6c914e:L11-L11`); icon `ui/WorkshopIcons.cs@a6c914e:L34-L34`.
- **Catalogue and inventory entry.** Id `windmill`, title "Windmill", category Power, description "Aim airflow through the rotor, then belt its side pulley to a conveyor or reverse transmission. Backwards wind reverses the shaft; edge-on wind cannot drive it. Obstructions reduce the exposed rotor area. Coasts briefly when wind stops. Uses the current ideal-speed mechanical model, not torque or load conservation." (`parts/catalog/windmill.tres@a6c914e:L6-L12`).

## Engine capabilities

Families: the [CAT-070 row](../general-engine-element-map.md) and `docs/coverage/catalogue-elements.json` (AerodynamicDrag, ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, SlidingFriction).

- **Exists now.** Static boxes, dynamic bodies with full inertia, contact (`engine/gpu/WorkshopPhysicsCompiler.cs`, `engine/gpu/RigidMassProperties.cs`).
- **Missing.**
  - Hinge joint (JointConstraint): Story 10.3 (CAT-034).
  - Annular tube collider for the guard: the pipe collider work, Stories 6.6–6.7 (CAT-048).
  - Airflow field and receivers: Story 12.2 (CAT-028).
  - Rotary capture law (cut-in, bounded unloaded speed, calm-air coast resistance): Story 12.3, decision owner S470 ([decisions](../invest/decisions.md#s470)).
  - Mechanical shaft output and belt network (ShaftTorque): Story 11.1 (CAT-042, CAT-019); reverse transmission Story 11.2 (CAT-057).
- **Element dependencies.** Fan (CAT-028) or Bellows (CAT-010) as source; Conveyor (CAT-019) and Reverse transmission (CAT-057) as loads; Wall (CAT-066) for occlusion.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Unloaded response: target speed = clamp(F × gain, ±12) where F is the signed rest force of the exposed samples; resistance = \|F × 0.4 / target\| (0.4/gain when target is 0); below 0.05 N the rotor is passive with coast resistance 0.4/gain. "Resistance" here is the effective slope of torque against shaft speed at the operating point: the airflow branch's own drive slope (pitch² × conductance) plus a hinge damper. For the catalogue fan (conductance 0.75 N·s/m, 9 N) and gain 2/3 that is 0.12 drive + 0.48 damper = 0.6 = 9 × 0.4 / 6. The pitch is reduced when needed, never negative damping; no velocity clamp and no independent drive torque. | `engine/physics/RotaryCaptureMaterial.cs@a6c914e:L47-L127`; `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L25-L36` | Carry forward the law. |
| 1a | Calibration: flow 12, gain 2/3 → target 6 rad/s, pitch 0.4, damper 0.48 (and −6 for reversed alignment). | `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs@a6c914e:L15-L16` | Carry forward. |
| 1b | Calibration: flow 12, gain 4 → target 12 rad/s (cap), pitch 0.4, damper 0.18. | `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs@a6c914e:L17-L17` | Carry forward. |
| 1c | Calibration: flow 1, gain 4 → target 3 rad/s, pitch reduced to 1/3, damper 0 (the test admits a positive value below 1e-14), both alignments. In every case the torque is zero at the target, positive at 0.9 × target and negative at 1.1 × target. | `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs@a6c914e:L18-L33` | Carry forward. |
| 1d | Cut-in boundary: rest force 0.049 is passive (target 0, coast damper pitch/gain = 0.4 at gain 1); 0.05 drives. Damping never adds energy at −12, 0 or 12 rad/s. | `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs@a6c914e:L49-L65` | Carry forward. |
| 2 | Accepted air force on a turning rotor: F_rest × (1 − 0.4 × ω / 12). | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L37-L42` | Carry forward. |
| 3 | Air force is read as the mean of committed transfer impulses over the substep; the axial component along the hinge axis is the reading. | `engine/AirflowNetwork.cs@a6c914e:L102-L121`; `parts/WindmillPart.cs@a6c914e:L63-L67` | Carry forward the reading; not the per-part observe loop. |
| 4 | Acceptance: fan at (−4, 6, 0), windmill at (−1, 6, 0): first tick 0.14–0.16 rad/s; after 240 ticks 6 rad/s (9 N × 0.6667); a belted conveyor surfaces at 4 m/s, reversed by each Reverse transmission; Turned recorded; fan off → 5.8–5.9 rad/s one tick later, 0 within 120 ticks; Reset zeroes speed, angle and travel; Save/Load replays 6 rad/s. Back wind (fan at (2, 6, 0) yawed 180°) gives the negative of each value. | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L47-L110` | Carry forward. |
| 5 | Acceptance: the whole assembly works in five orientations and has exactly one mechanical port. | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L111-L133` | Carry forward. |
| 6 | Acceptance: exposure full 9 N, partial 6.75 N (a Wall 3 × 0.4 × 0.25 at (−2.5, 6.5, 0) blocks the top sample), blocked 0 N. | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L134-L156` | Carry forward. |
| 7 | Acceptance: a ball at (1, 4.5, 0) on a conveyor at (2, 3, 0) is transported past X = 2.5 within 180 ticks only when belted, unblocked and the fan is on; the jet never touches the ball. | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L157-L181` | Carry forward. |
| 8 | Acceptance: edge-on (yaw 90°), weak (0.01 N) and opposing (equal rear fan) air give zero speed and zero source energy. | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L182-L210` | Carry forward; the supplied-energy ledger assertion is a certificate and is not carried forward. |
| 9 | Acceptance: reversing the wind crosses zero along the finite-inertia trajectory. | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L211-L279` | Carry forward the behaviour. |
| 10 | Acceptance: with force 40 and gain 4 the shaft caps at 12 rad/s; a Motor cannot also drive the belted conveyor; a conveyor cannot drive the windmill; gains 0, 5 and NaN reject. | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L280-L298` | Carry forward. |
| 11 | Acceptance: invalid sample weights fail before any receiver changes. | `CuriousContraptions.tests/WindmillTests.cs@a6c914e:L299-L322` | Carry forward as declaration validation. |
| 12 | Acceptance: forward and reverse air at 9 N and 40 N drive the rotor between 1 and 12 rad/s; one tick after the fan stops the speed falls but keeps its sign; within 120 ticks kinetic energy drops below 10%. No damping or effort load sits on the hinge besides the rotary capture. | `CuriousContraptions.tests/SourceRotorTests.cs@a6c914e:L54-L66`, `L169-L212` | Carry forward. |
| 13 | Solved poses, not rendered transforms, set the rotor force; disabling the fan body removes it. | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L260-L283` | Carry forward. |
| 14 | One fan funds two rotors, or a rotor and a ball, from one reservoir. | `CuriousContraptions.tests/RotorAirflowSupplyTests.cs@a6c914e:L25-L112` | Carry forward. |
| 15 | Bellows → Windmill → Conveyor chain (default 480-tick and response-4, 960-tick cases). | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L82-L254` | Carry forward (see CAT-010). |
| 16 | Shaft angle, speed and travel are read from the owned hinge, not presentation, at 0° and 37° placements, and restore on Reset; the rotor body keeps its authored box offsets under rotation. | `CuriousContraptions.tests/AxialMotionOwnershipTests.cs@a6c914e:L48-L142`; `CuriousContraptions.tests/BodyLocalBoxTests.cs@a6c914e:L25-L77` | Carry forward. |
| 17 | Declaring a transfer on the undeclared rotor body rejects before Run. | `CuriousContraptions.tests/SceneMechanicalTransferTests.cs@a6c914e:L18-L32` | Carry forward as declaration validation. |
| 18 | No level uses the windmill. | `docs/coverage/catalogue-elements.json` (empty fixture list) | Recorded. |
| 19 | More calibration cases: two opposed branches (flow 12 forward, 2 reversed, gain 1) settle at 7.5 rad/s with damper 0.16 in either order; a saturated branch (conductance 100, cap 1, gain 2) gives target 2 and damper 0.2; opposed equal, edge-on and empty branch sets are passive; source power − receiver power = loss ≥ 0 at every speed; invalid pitch, gain, maximum speed, cut-in, duplicate or null branches and alignment outside [−1, 1] reject. At the catalogue operating point the transfer force is 0.75 × (12 − pitch × ω) and the rotor holds its target speed with exact replay. | `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs@a6c914e:L35-L178` | Carry forward; the work-ledger checks are certificates and are not carried forward. |
| 20 | Requested speed is continuous before the separate cut-in: at gain 4, rest force ±100 → ±12, ±0.01 → ±0.04, 0 → 0; NaN rejects. | `CuriousContraptions.tests/RotaryCaptureForcePathTests.cs@a6c914e:L153-L164` | Carry forward. The rest of the file (captured force paths, threshold sweeps, plateau-departure detection) tests CPU calibration-path certificates: do not carry forward. |
| 21 | Calm air: a hinge with no branches damps relative rotor speed with resistance pitch/gain (0.6 at the catalogue gain), never adds energy and conserves the two bodies' total angular momentum. | `CuriousContraptions.tests/RotaryCaptureMotionTests.cs@a6c914e:L71-L91`; `CuriousContraptions.tests/RotaryCaptureDeclarationTests.cs@a6c914e:L291-L304` | Carry forward. |
| 22 | A rotating source is re-sampled within the step and replays exactly. | `CuriousContraptions.tests/RotaryCaptureMotionTests.cs@a6c914e:L11-L69` | Carry forward only "exact replay"; the predictor-stage sampling is a CPU solver path. |
| 23 | Supply lifecycle: disabling the source's participation body stops supply but the rotor keeps coasting, and re-enabling drives again; partial exposure re-prepares without losing branch identity; mixed linear and rotary branches share one finite allocation; an exhausted store leaves the rotor passive, coasting at resistance pitch/gain, and recharging drives it again; Reset replays exactly; duplicate identities reject atomically. | `CuriousContraptions.tests/RotaryCaptureDeclarationTests.cs@a6c914e:L89-L289` | Carry forward the behaviour. Ledger totals, tolerance bands and the authoritative-prediction recalibration are certificates or CPU paths: do not carry forward. |

**Files harvested:**
- `parts/WindmillPart.cs`
- `parts/catalog/windmill.tres`
- `parts/scenes/windmill.tscn` (no element knowledge: script reference only)
- `engine/AirflowNetwork.cs`
- `engine/PartArt.cs` (ring half-width convention)
- `engine/TubeProxy.cs`
- `engine/physics/RotaryCaptureMaterial.cs`
- `engine/physics/RotaryCaptureDeclaration.cs` (CPU preparation and trajectory certificates over fact 1's law; do not carry forward)
- `engine/physics/RotaryCaptureForcePath.cs` (CPU calibration-path certificates; do not carry forward)
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/WindmillTests.cs`
- `CuriousContraptions.tests/SourceRotorTests.cs`
- `CuriousContraptions.tests/RotorAirflowSupplyTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/BellowsTests.cs`
- `CuriousContraptions.tests/AxialMotionOwnershipTests.cs`
- `CuriousContraptions.tests/BodyLocalBoxTests.cs`
- `CuriousContraptions.tests/SceneMechanicalTransferTests.cs`
- `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs` (facts 1a–1d, 19)
- `CuriousContraptions.tests/RotaryCaptureForcePathTests.cs` (fact 20; remainder do not carry forward: certificates)
- `CuriousContraptions.tests/RotaryCaptureMotionTests.cs` (facts 21–22)
- `CuriousContraptions.tests/RotaryCaptureDeclarationTests.cs` (facts 21, 23; certificates not carried forward)

## Acceptance outline

Point of truth: [CAT-070](../requirements.md#current-cat-070) and [retained behaviour](../requirements.md#sequence-task-311).

- **Chrome construction.** Through the real palette, place a Fan, the Windmill 3 m downstream and a Conveyor; connect the windmill's Drive port to the conveyor with the real socket UI; put a ball on the belt.
- **Positive.** Run: the rotor spins up toward 6 rad/s, the belt moves and the ball is transported.
- **Negative or control.** A Wall in the jet stops the rotor (Story 12.3); edge-on, weak and opposing air give no drive; a disconnected belt leaves the ball; fan off → coast then rest.
- **Boundaries.** Partial occlusion gives 3/4 of the force; unloaded speed never exceeds 12 rad/s; rest force 0.049 N is passive and 0.05 N drives; gains outside 0.1–4 reject.
- **Run/Reset.** Reset zeroes speed, angle and travel exactly.
- **Save/Load.** Round-trips the gain and the connection; replay reaches the same speed.
- **Integrations.** Bellows burst (CAT-010); Reverse transmission (CAT-057).

## Open questions

1. **Catalogue wording.** The legacy description says "ideal-speed mechanical model, not torque or load conservation", while the requirement asks for finite inertia, load and backdrive with per-branch source debit. Confirm the description is rewritten.
2. **Cut-in unit.** The gas doc lists the cut-in as 0.05 m/s; the legacy material treats it as 0.05 N of rest force (`engine/physics/RotaryCaptureMaterial.cs@a6c914e:L97-L99`). Owner decision.
3. **Rotor contact material.** No windmill-specific value. Owner decision.
4. **Guard tube.** The guard is an annular collider; confirm it reuses the CAT-048 pipe collider rather than a windmill-specific shape.
