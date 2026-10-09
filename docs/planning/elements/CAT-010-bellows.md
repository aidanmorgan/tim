# CAT-010 · bellows — declaration readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline.

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-010 · `bellows` |
| Requirement anchor | [CAT-010](../requirements.md#current-cat-010) and its [retained behaviour](../requirements.md#sequence-task-312); D/I/V row [CAT-010-I](../invest/current-consumers.md#cat-010-i) |
| Mapped identities | None. No EL, TH, RAD or GAP identity names the bellows; [EL-042 Air nozzle](../invest/named-elements.md#element-042) is a distinct element. |
| Capability row | [general-engine-element-map, CAT-010](../general-engine-element-map.md) |
| Roadmap story | Epic 12, Story 12.4 "Pneumatic Bellows Compression Pulse" (after Windmill, Story 12.3, and Conveyor, Story 11.1; see Open question 5) |
| Status | Not started. |

## Declaration

- **Bodies and shapes.**
  - Static root body: base box at (0, −0.4, 0), size 1.5 × 0.16 × 1.2 m; nozzle box at (0.73, −0.12, 0), size 0.45 × 0.19 × 0.24 m (`parts/BellowsPart.cs@a6c914e:L166-L166`, `L177-L177`; legacy `AddBox` half-extents = size/2, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`).
  - Dynamic press plate: box half-extents (0.7, 0.06, 0.55) m, centred at rest height (0, 0.45, 0) (`parts/BellowsPart.cs@a6c914e:L51-L51`, `L60-L60`, `L165-L167`).
- **Mass and material.** Plate mass 0.5 kg; inertia of a solid box, m(b² + c²)/3 on half-extents (`parts/BellowsPart.cs@a6c914e:L53-L53`, `L63-L70`). Plate contact material restitution 0, bounce threshold 0.1, friction 0.3 (`parts/BellowsPart.cs@a6c914e:L96-L96`; argument order `engine/physics/PersistentContactPair.cs@a6c914e:L23-L23`).
- **Constraints and joints.** One slider between plate and root along local +Y at (0, 0.45, 0); travel limits [−0.4, 0] m (compression only); connected-body collision disabled (`parts/BellowsPart.cs@a6c914e:L52-L52`, `L71-L85`).
  - Elastic return on the slider: stiffness 20 N/m with rest coordinate 8/20 = 0.4 m, so the plate is preloaded with 8 N at rest and pushes 8 + 20c N at compression c (`parts/BellowsPart.cs@a6c914e:L54-L55`, `L118-L125`).
  - Refill damping 4 N·s/m on the return (outward) direction only; zero on compression, because receivers extract compression work (`parts/BellowsPart.cs@a6c914e:L56-L56`, `L124-L124`).
- **Typed sockets and ports.** None (`CuriousContraptions.tests/BellowsTests.cs@a6c914e:L367-L367`).
- **Sensors and activation.** A stroke starts when compression leaves zero (tolerance 1e-7 m) and records one Activated occurrence; phases Ready, Compressing, Held, Refilling (speed tolerance 1e-6 m/s) (`parts/BellowsPart.cs@a6c914e:L9-L9`, `L57-L59`, `L126-L155`).
- **Work and energy stores.** No reservoir: the source is the plate's own motion. Airflow source: nozzle at (0.96, −0.12, 0), direction local +X; flow speed = −(slider speed) × 12/0.8 = −15 × slider speed; force rating `force`, power rating `force` × 12; receiver impedance conductance `force`/12 capped at `force` (`parts/BellowsPart.cs@a6c914e:L57-L57`, `L61-L61`, `L97-L106`).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit |
  | --- | --- | --- | --- | --- |
  | force | f32 | (0, 40] | 18 | N |
  | reach | f32 | (0, 12] | 5 | m |
  | width | f32 | (0, 4] | 0.85 | m (jet radius) |

  Ranges `parts/BellowsPart.cs@a6c914e:L108-L117`; defaults `parts/catalog/bellows.tres@a6c914e:L12-L12`. Parameter names are an enum (`parts/BellowsPart.cs@a6c914e:L10-L10`).
- **Cosmetic curves and UI bindings.** The plate art follows the committed plate pose; the accordion scales along Y with plate height (affine map: offset (0, −0.3, 0), scale (1, 0.3/0.75, 1), Y gain 1/0.75) (`parts/BellowsPart.cs@a6c914e:L156-L161`). Gas doc binding: "Bag squash ← committed slider travel" ([gas family](../../finite-gas-foundation.md)).
- **Art.** Ring "thickness" values are torus half-widths: inner r − t, outer r + t (`engine/PartArt.cs@a6c914e:L22-L23`). Base `#293954`; plate `#fff8e9` with a 0.7 × 0.025 × 0.35 `#e8b764` strip 0.07 above; five folds at y = (i + 0.5) × 0.15 from −0.3, each a 1.2 × 0.1 × 0.92 `#66b8c9` box plus a 1.28 × 0.035 × 1 `#fff8e9` rim; nozzle box `#e8b764`; mouth torus radius 0.12, half-width 0.025 (inner 0.095, outer 0.145), `#293954`, axis X; pick radius 0.95 (`parts/BellowsPart.cs@a6c914e:L162-L180`). Catalogue colour (0.4, 0.72, 0.79) (`parts/catalog/bellows.tres@a6c914e:L11-L11`); icon `ui/WorkshopIcons.cs@a6c914e:L33-L33`.
- **Catalogue and inventory entry.** Id `bellows`, title "Bellows", category Power, description "Press the spring-loaded top plate to squeeze air from the side nozzle. Aim the burst at a windmill or chimes. Heavy loads keep the plate compressed; removing them lets its return spring refill it. Air flows only while the plate moves inward. Uses the game's ideal airflow model, not a pressurised-air network." (`parts/catalog/bellows.tres@a6c914e:L6-L12`).

## Engine capabilities

Families: the [CAT-010 row](../general-engine-element-map.md) and `docs/coverage/catalogue-elements.json` (AerodynamicDrag, ContactImpulse, ElasticStorage, EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SlidingFriction).

- **Exists now.** Dynamic boxes with full inertia (`engine/gpu/RigidMassProperties.cs`), static boxes, contact and friction (`engine/gpu/WorkshopPhysicsCompiler.cs`), activation occurrences (`engine/gpu/ActivationNetwork.cs`).
- **Missing.**
  - Slider joint with limits and elastic return (ElasticStorage, JointConstraint): Story 6.4 (CAT-062a).
  - Airflow jet field, receivers and occlusion: Story 12.2 (CAT-028).
  - Plate-driven nozzle source (flow from committed slider speed), PressureWork and GasState: Story 12.4, under decision owner S470 ([decisions](../invest/decisions.md#s470)); S471 gas-state, S697 open versus sealed.
  - Pose-driven accordion scaling binding: Story 12.4.
- **Element dependencies.** Fan (CAT-028) airflow capability; Windmill (CAT-070), Conveyor (CAT-019) and Wind chimes (CAT-069) for the downstream controls; a striker ball (Basketball CAT-001 or Bowling ball CAT-014).

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Geometry, mass, joint, spring, damping and nozzle values as in Declaration. | `parts/BellowsPart.cs@a6c914e:L51-L106` | Carry forward as declaration data. |
| 2 | The plate's elastic and damping loads are re-applied every substep by the part. | `parts/BellowsPart.cs@a6c914e:L118-L125` | Do not carry forward the per-element update loop; declare the spring and damper once. |
| 3 | `EmissionForce`/`EmittedImpulse` are compression-derived diagnostics, not committed transfer. | `parts/BellowsPart.cs@a6c914e:L133-L137` | Do not carry forward as gameplay; observe committed transfer instead. |
| 4 | Acceptance: plate checks each step: slider travel within [−0.4, 0]; joint error ≤ 1.01e-7; one elastic load (k = 20, rest 0.4) and one damper (0 compressing, 4 refilling); plate art matches the solved plate. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L58-L80` | Carry forward the behaviour; the error bound becomes the game-grade envelope. |
| 5 | Acceptance chain: pump at (−4, 6, 0), striker dropped from (−4, 9, 0), windmill at (−1, 5.88, 0), conveyor at (2, 3, 0) with cargo at (1, 3.6, 0). The striker is 1 kg in the default (Original) case and 2 kg in the full-stroke case. Within 480 ticks: one stroke unless missed; compression > 0.05 m; rotor > 1 rad/s unless missed or a Wall at (−2.5, 5.88, 0) yawed 90° blocks; cargo ends at X > 2.5 only when struck, clear and belted, otherwise stays at X = 1. Reset returns Ready, 0 compression, 0 strokes. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L82-L254`; striker masses `L128-L129` | Carry forward. The default 480-tick case cannot be replaced by the response-4, 960-tick variants. |
| 6 | Full-stroke static support needs (m + 0.5) g > 8 + 20 × 0.4 = 16 N; the 2 kg striker satisfies it (24.5 N). | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L132-L136` | Carry forward. A 4 kg Bowling ball (44.1 N, arithmetic) also bottoms the plate at 0.4 m and holds it. |
| 7 | Acceptance: a resting load holds the plate at equilibrium compression ((m + 0.5) g − 8)/20, settling to < 1e-5 m/s and within 1e-4 m; it stays held 120 ticks; pushing the load off lets the plate refill to Ready with zero emission while refilling; a second press counts stroke 2. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L256-L301` | Carry forward. |
| 8 | Acceptance: a real top impact works in five orientations; emission is along the part's local X. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L303-L336` | Carry forward. |
| 9 | Acceptance: spring potential plus kinetic energy never exceeds 1.02 × input energy + 0.001 J. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L338-L370` | Carry forward as "no energy creation" within the envelope. |
| 10 | Acceptance: only a top inward stroke pumps; side, underside and missed strikes do not. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L372-L397` | Carry forward. |
| 11 | Acceptance: two simultaneous loads share the plate and count one stroke. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L399-L420` | Carry forward. |
| 12 | Acceptance: Reset, Save/Load replay and render calls cannot change physics. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L422-L453` | Carry forward. |
| 13 | Acceptance: the burst rings wind chimes at (−1, 6.48, 0); the blocking Wall silences them; Save/Load round-trips. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L455-L490` | Carry forward. |
| 14 | Acceptance: a second load can contact the returning plate without passing through it. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L492-L518` | Carry forward. |
| 15 | Acceptance: force 0 or 41, reach −1, width 5 and NaN reject. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L520-L548` | Carry forward. |
| 16 | Two receivers in front of the nozzle extract work only while the plate is compressing; uncompressed, nothing moves; receiver gain ≤ extracted work. | `CuriousContraptions.tests/LinearAirflowSupplyTests.cs@a6c914e:L77-L120`; `CuriousContraptions.tests/AirflowConservationTests.cs@a6c914e:L13-L106` | Carry forward the behaviour. Do not carry forward the extraction ledger certificates. |
| 17 | Failed-substep rollback restores stroke history. | `CuriousContraptions.tests/MechanicalRuntimeCheckpointTests.cs@a6c914e:L34-L76` | Do not carry forward: no faulting ticks under clamp-or-continue. |
| 18 | No level uses the bellows. | `docs/coverage/catalogue-elements.json` (empty fixture list) | Recorded. |
| 19 | A plate that starts at rest supplies the jet in the same interval in which an inward force moves it, and the plate pays the receiver's reaction in that interval. An outward or zero force supplies nothing. Receiver plus plate kinetic energy never exceeds the external work done on the plate; replay after Reset is exact. Disabling the plate's or the source's collider participation stops the transfer, and the plate then moves freely under the applied force. A pump or participation body that does not exist, or a participation body equal to the source, rejects without replacing the installed load. | `CuriousContraptions.tests/CompressionTransferSupplyTests.cs@a6c914e:L11-L56` | Carry forward the behaviour. Do not carry forward the worked value (0.1875 N, target 0.01875 m/s at L30-L34): it comes from the legacy coupled-midpoint prediction interval, a CPU solver path. |
| 20 | Compression is measured relative to the carrier: with a moving and rotating kinematic base, the compression speed is the plate's speed along the pump axis relative to the base, so carrier motion alone supplies nothing and refill (outward motion) supplies nothing. With conductance 6/0.8 and cap 6 the force is 6 × clamp(−v/0.8, 0, 1), so the full rated force is reached at 0.8 m/s inward compression (the bellows reference compression speed). Evaluating supply does not change any body. A pump port whose body and source are the same rejects. | `CuriousContraptions.tests/CompressionTransferSupplyTests.cs@a6c914e:L58-L90` | Carry forward. |
| 21 | Non-finite or negative ratings, impedances and stored-flow speeds reject; a zero port direction and null source or material reject. | `CuriousContraptions.tests/CompressionTransferSupplyTests.cs@a6c914e:L92-L111` | Carry forward as declaration validation. Do not carry forward the per-load work-tolerance argument (rejected when ≤ 0 or non-finite at L104-L106): it belongs to the legacy work-ledger certificate. |
| 22 | An undefined collider participation value rejects; plate-driven supply rejects a missing source or plate collider; stored-flow supply rejects a missing source collider. | `CuriousContraptions.tests/CompressionTransferSupplyTests.cs@a6c914e:L113-L138` | Carry forward; enums end to end. |

**Files harvested:**
- `parts/BellowsPart.cs`
- `parts/catalog/bellows.tres`
- `parts/scenes/bellows.tscn` (no element knowledge: script reference only)
- `reference/cpu/MachinePart.cs`
- `engine/PartArt.cs` (ring half-width convention)
- `engine/physics/PersistentContactPair.cs`
- `CuriousContraptions.tests/BellowsTests.cs`
- `CuriousContraptions.tests/LinearAirflowSupplyTests.cs`
- `CuriousContraptions.tests/AirflowConservationTests.cs`
- `CuriousContraptions.tests/MechanicalRuntimeCheckpointTests.cs`
- `CuriousContraptions.tests/CompressionTransferSupplyTests.cs` (facts 19–22; the worked prediction-interval value and the work-tolerance argument are not carried forward)

Consulted, no bellows-specific knowledge (unused sealed-gas and nozzle foundation; no part consumes it; the law lives in the current [gas family document](../../finite-gas-foundation.md)): `engine/physics/AdiabaticGasDischarge.cs`, `engine/physics/AxialGasGeometry.cs`, `engine/physics/AxialGasLoad.cs`, `engine/physics/AxialGasPotential.cs`, `engine/physics/ConvergingGasNozzle.cs`, `engine/physics/GasNozzleTransit.cs`, `engine/physics/GasPredictionWork.cs`, `CuriousContraptions.tests/AdiabaticGasDischargeTests.cs`, `CuriousContraptions.tests/AxialGasPotentialTests.cs`, `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`, `CuriousContraptions.tests/GasNozzleTransitTests.cs`, `CuriousContraptions.tests/PhysicsGasNodeTests.cs`.

## Acceptance outline

Point of truth: [CAT-010](../requirements.md#current-cat-010), [retained behaviour](../requirements.md#sequence-task-312) and step 3 of the [first airflow slice](../../finite-gas-foundation.md).

- **Chrome construction.** Through the real palette, place the Bellows, a Windmill in the nozzle line, a Conveyor belted to the windmill and cargo on the belt; place a striker ball above the plate.
- **Positive.** Run: the striker compresses the plate, the burst spins the windmill and the cargo reaches X > 2.5 within the default 480-tick budget.
- **Negative or control.** Missed striker, a Wall in the jet, a disconnected belt, a side or underside strike, and an uncompressed bellows each produce no transport and zero airflow.
- **Boundaries.** Full stroke 0.4 m (2 kg striker or heavier holds it); held load stays compressed without continuing emission; refill emits nothing; parameter limits reject.
- **Run/Reset.** Reset restores plate pose, phase Ready and stroke count 0.
- **Save/Load.** Round-trips the construction; replay matches.
- **Integrations.** Windmill and conveyor; wind chimes; Story 12.4's Bowling-ball strike propelling a balloon or tennis ball.

## Open questions

1. **Model.** Legacy uses the balanced ideal-transfer model ("not a pressurised-air network"); the requirement asks for "conserved gas/nozzle transfer" and the gas doc gives the Bellows a nozzle record with unloaded emission, NoFlow/Subsonic/Choked regimes and refill from a declared environment. Decide which model Story 12.4 adopts. Unspecified — owner decision (S470).
2. **Story 12.4 receivers.** It says the jet propels "an adjacent balloon or tennis ball"; legacy acceptance uses windmill, conveyor and chimes. Confirm both are required.
3. **Striker.** Story 12.4 uses a falling Bowling ball; legacy tests use a 1 kg ball (2 kg for full stroke). Confirm the Bowling ball default and its full-stroke hold.
4. **Obstructed refill.** The requirement lists "obstructed refill"; legacy has no explicit test beyond the returning-plate contact. Expected outcome unspecified — owner decision.
5. **Story order (owner decision recorded).** The legacy acceptance chain needs Windmill (Story 12.3) and Conveyor (Story 11.1). The owner's 9 Oct 2026 reorder placed Bellows at Story 12.4, after both.
