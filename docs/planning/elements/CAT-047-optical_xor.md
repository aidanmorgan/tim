# CAT-047 · optical_xor — declaration readiness spec (CAT-047-D)

## Identity

- **CAT ID / kind:** CAT-047 · `optical_xor` (catalogue title "Exactly one light gate", category Optics).
- **Requirement anchor:** [CAT-047](../requirements.md#current-cat-047); retained behaviour [todo-367](../requirements.md#todo-367). Mode record: Logic = Xor.
- **Mapped identities:** EL-140 Optical XOR gate (owner S512).
- **Roadmap story:** 13.8 (Optical Logic Gates AND, NAND, NOR, OR, XOR; shared with CAT-043–046).
- **Status:** not started.

## Declaration

**Optical gate family (shared with CAT-043 AND, CAT-044 NAND, CAT-045 NOR, CAT-046 OR).** Two absorbing control apertures (A = `First`, B = `Second`) set two hysteretic Booleans; at the next tick the gate's operation decides *open*. A separate carrier aperture routes 90% of the carrier's existing RGB power to the outlet only while open; when closed the carrier is absorbed. Controls never become output light; a logically true gate with no carrier emits nothing. Each operation is a separate catalogue entry. Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`).

**XOR variant values:** operation `Xor` (scene `Operation = 2`). Truth table (A, B → open): 00 → closed, 01 → **open**, 10 → **open**, 11 → closed. At Reset both controls are low, so the gate starts **closed**. Truth relief shows gold output dots on rows 01 and 10.

- **Bodies and shapes:** one static rigid body. Opaque cube 1.4 × 1.4 × 1.4 at the origin; opaque base 1.8 × 0.18 × 1.8 at (0, −0.85, 0).
- **Optical apertures (radius 0.43, front face only):**

  | Port | Centre | Normal | Interaction | Marks |
  |---|---|---|---|---|
  | `First` (A) | (−0.76, 0, 0) | (−1, 0, 0) | Absorb | 1 cream bar |
  | `Second` (B) | (0, 0.76, 0) | (0, +1, 0) | Absorb | 2 cream bars |
  | `Carrier` | (0, 0, 0.76) | (0, 0, +1) | Route (open) / Absorb (closed), transmission 0.9 | gold rim |

- **Outlet:** (0.76, 0, 0), direction (+1, 0, 0).
- **Mass and material:** none — static body.
- **Constraints and joints:** none.
- **Typed sockets and ports:** none electrical; the three optical apertures are its ports.
- **Sensors and activation:** each control reads broadband strength (mean of R, G, B). Hysteresis per control: a low control turns high at ≥ 0.25; a high control stays high while > 0.225. Controls are sampled after the optical trace; the open decision advances once before the next optical solve (next-tick decision).
- **Work and energy stores:** none; output power never exceeds 0.9 × carrier.
- **Parameters:** none player-adjustable. Fixed on 0.25, off 0.225 (finite, 0 ≤ off < on). Operation is fixed by the catalogue entry (closed enum `LogicGateKind {And, Or, Xor, Nor, Nand}`).
- **Cosmetic curves and UI bindings:** A and B control lenses follow their Booleans slate `#556573` → gold `#f7cb52` (exponential rate 12 /s). The output lens follows the actual exiting R/G/B power (rate 12 /s, inactive slate); it lights only when a carrier actually exits, so it cannot show a fabricated beam. Selection pick radius 1.3 (`parts/OpticalLogicPart.cs@a6c914e:L74-L74`).
- **Art:** cyan cube `#66b8c9`, navy base `#293954`, cream control rings `#fff8e9` and gold carrier ring `#e8b764` (r 0.48, tube 0.055), slate lenses (r 0.43), outlet lens (r 0.34) with gold rim (r 0.4). Truth relief: four cream plates 0.2 × 0.32 × 0.035 at (−0.42 + 0.28·row, −0.42, 0.72); two bit studs (r 0.025, navy if set, cream if clear); gold output dot (r 0.045) where open. See [DESIGN.md](../../../DESIGN.md#optical-logic-gates).
- **Catalogue and inventory entry:** id `optical_xor`, title "Exactly one light gate", category Optics, colour (0.40, 0.72, 0.79), description "The numbered cream inputs control a separate gold-rimmed carrier. Passes 90% of the carrier when exactly one control is lit. Changes apply next simulation tick; control beams never supply output light." Icon `ui/WorkshopIcons.cs@a6c914e:L77-L77` (current, kept; the same pictogram serves `electrical_xor`). No legacy level uses it.

## Engine capabilities

Families (map row CAT-047): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, OpticalTransport, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Exists now**
- Static box body/colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- Cosmetic blend channel (Activation feedback only): `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport: control absorption, routed carrier with 90% retention, aperture interaction switching by committed state — Story 13.8 (routing from 13.5). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486 (snapshot and next-tick boundary).
- Hysteretic discrete optical control state with next-tick advance — Story 13.8 (SignalPropagation; P0-022/023 shared evaluator/feedback registration).
- Boolean and spectral feedback for lamps — Story 13.8.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser ×3 (A, B, carrier; Story 13.1), CAT-038 light_receiver (output observer; Story 13.2), CAT-005 battery (Story 8.1), CAT-066 wall (occlusion; delivered). Electrical counterpart CAT-027 electrical_xor is a separate element (Story 9.8).

## Legacy harvest

Shared optical transport facts (identical in every narrow-ray Epic 13 spec):
- S1. Ports and interactions are closed enums: `OpticalPortId {Main, First, Second, Third, Carrier}`, `OpticalInteraction {Absorb, Mirror, Split, Filter, Route}` — `engine/OpticalNetwork.cs@a6c914e:L8-L13`. Carry forward as enums.
- S2. Limits: 16 interactions per path, 128 segments per emitter, mirror retention 0.95 — `engine/OpticalNetwork.cs@a6c914e:L31-L33`. Carry forward.
- S3. Aperture validation: unique port ids, finite centre/normal, radius > 0, transmission per channel in [0, 1], Route requires a finite directed outlet; invalid declarations are rejected, never substituted — `engine/OpticalNetwork.cs@a6c914e:L38-L67`; restated by `CuriousContraptions.tests/OpticalPortsTests.cs@a6c914e:L110-L141`. Carry forward.
- S4. A ray stops when remaining range ≤ 1e-4 or power² < 1e-8; split/filter apertures accept either face, all others front face only; hit = nearest finite disc within the opaque-geometry distance — `engine/OpticalNetwork.cs@a6c914e:L79-L104`. Carry forward.
- S5. All emitters are traced from one captured snapshot (ordered by part id) and every receiver reading is committed together after tracing — `engine/OpticalNetwork.cs@a6c914e:L153-L183`; `CuriousContraptions.tests/OpticalPortsTests.cs@a6c914e:L91-L109`. Carry forward the behaviour; do not carry forward the CPU static solve or per-part callbacks.
- S6. Optical readings commit before the electrical solve that reads them. Legacy realised this with the CPU call sequence part pre-network hooks → cone light → optical → acoustic → electrical — `reference/cpu/MachineWorld.cs@a6c914e:L855-L861`. Carry forward the ordering rule (required by [todo-290](../requirements.md#todo-290), assumed by [todo-285](../requirements.md#todo-285)); do not carry forward the CPU call sequence. S486 owns the conflict with the current pipeline, which puts electrical in Phase 1 and optics in Phase 2 ([docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model)).
- S7. Optical path identity is a string (`OpticalPathOwner`) — `engine/OpticalNetwork.cs@a6c914e:L14-L22`. Do not carry forward: string-typed identity; use a typed id.
- S8. Occlusion uses opaque colliders only: Light (and Sound) traces query each body's opaque subset, so transparent panes and hollow bores pass light while frames, walls and balls block it — `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`, `engine/WorldGeometry.cs@a6c914e:L173-L190`. The emitter's own geometry and apertures are skipped only on the first segment (depth 0); every reflected, split, filtered or routed segment can be blocked by, or strike, its own emitter — `engine/OpticalNetwork.cs@a6c914e:L82-L82`, `engine/OpticalNetwork.cs@a6c914e:L89-L89`. This is the mechanism behind returning-ray occlusion. Carry forward.

Gate family facts:
1. `LogicGateKind {And, Or, Xor, Nor, Nand}` and the evaluator (Xor = A ≠ B) — `engine/LogicGate.cs@a6c914e:L6-L19`. Carry forward as a closed enum.
2. Control thresholds on 0.25 / off 0.225, validated finite with 0 ≤ off < on — `engine/LogicGate.cs@a6c914e:L44-L54`. Carry forward.
3. Sampling validates both inputs (finite, ≥ 0) before changing either; hysteresis: high stays high while > off, low turns high at ≥ on — `engine/LogicGate.cs@a6c914e:L62-L69`, `engine/LogicGate.cs@a6c914e:L79-L80`. Carry forward.
4. Advance sets open = evaluate(A, B); Reset clears A, B and sets open = evaluate(false, false), which is closed for Xor — `engine/LogicGate.cs@a6c914e:L71-L77`. Carry forward.
5. Readable quantities `{First, Second, IsOpen}` as a closed enum — `engine/LogicGate.cs@a6c914e:L5-L5`, `engine/LogicGate.cs@a6c914e:L56-L60`; Boolean observation source over the control — `engine/SceneBooleanObservation.cs@a6c914e:L8-L22`. Carry forward the typed observation; not the Godot binding.
6. Retention 0.9, outlet (0.76, 0, 0) — `parts/OpticalLogicPart.cs@a6c914e:L12-L14`; control state, output state, R/G/B output observations and checkpointed runtime state — `parts/OpticalLogicPart.cs@a6c914e:L15-L35`. Carry forward.
7. Control lamps follow A/B at rate 12 — `parts/OpticalLogicPart.cs@a6c914e:L36-L43`; operation validated and control state prepared from it — `parts/OpticalLogicPart.cs@a6c914e:L44-L49`. Carry forward.
8. Decision advances before the networks each tick — `parts/OpticalLogicPart.cs@a6c914e:L50-L50`. Carry forward the next-tick rule; do not carry forward the per-part pre-network hook.
9. Apertures, carrier Route-when-open / Absorb-when-closed — `parts/OpticalLogicPart.cs@a6c914e:L51-L58`; control strength is broadband mean — `parts/OpticalLogicPart.cs@a6c914e:L59-L61`; output power and activity from actual exit — `parts/OpticalLogicPart.cs@a6c914e:L62-L66`; spectral output lamp — `parts/OpticalLogicPart.cs@a6c914e:L67-L71`. Carry forward.
10. Geometry, rings, marks and truth-table relief — `parts/OpticalLogicPart.cs@a6c914e:L72-L112`. Carry forward.
11. Acceptance: every truth row × carrier present/absent — controls are sampled in the first solve without changing open; after the next advance, open matches the table; output = 0.9 × carrier iff open ∧ carrier; only carrier light leaves the gate; removing the carrier clears output; reconfiguration resets A, B and open — `CuriousContraptions.tests/OpticalLogicTests.cs@a6c914e:L16-L69`. Carry forward.
12. Acceptance: the explicit truth table (independent of the evaluator) — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L5-L19`; open does not change during the current trace, changes after advance, Reset restores — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L21-L36`; invalid thresholds and undefined operations rejected — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L92-L107`. Carry forward.
13. Acceptance: controls publish as Booleans for every row with no carrier and zero output; lamps ease 1 − e^(−1.2) per 0.1 s and ignore unpublished state; rollback, hide, Reset and Save/Load restore — `CuriousContraptions.tests/BooleanObservationTests.cs@a6c914e:L60-L111`; per-channel output publishes atomically as carrier × 0.9 — `CuriousContraptions.tests/OpticalObservationTests.cs@a6c914e:L66-L114`. Carry forward.
14. Acceptance: a failed tick restores pending controls, current decision and output — `CuriousContraptions.tests/OpticalControlCheckpointTests.cs@a6c914e:L45-L84`; parameter validation cannot replace the captured control — `CuriousContraptions.tests/ParameterValidationOwnershipTests.cs@a6c914e:L64-L91`. Carry forward at the later (injected-fault) gate.

XOR facts:
15. Catalogue entry — `parts/catalog/optical_xor.tres@a6c914e:L6-L11`; scene `Operation = 2` — `parts/scenes/optical_xor.tscn@a6c914e:L1-L5`. Carry forward.
16. Acceptance (Xor): (A, B) = (1, 0) → open after advance; sampling (1, 1) leaves it open until the next advance, which closes it; returning to (1, 0) reopens it at the following advance — the second input retracts output only at the next snapshot — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L38-L54`. Carry forward.

**Files harvested:** `parts/OpticalLogicPart.cs`, `parts/catalog/optical_xor.tres`, `parts/scenes/optical_xor.tscn`, `engine/LogicGate.cs`, `engine/SceneBooleanObservation.cs`, `engine/OpticalNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/OpticalLogicTests.cs`, `CuriousContraptions.tests/LogicGateTests.cs`, `CuriousContraptions.tests/BooleanObservationTests.cs`, `CuriousContraptions.tests/OpticalObservationTests.cs`, `CuriousContraptions.tests/OpticalControlCheckpointTests.cs`, `CuriousContraptions.tests/ParameterValidationOwnershipTests.cs`, `CuriousContraptions.tests/OpticalPortsTests.cs`.

## Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place three supplied, triggerable lasers aimed at A (left), B (top) and the carrier (front), the XOR gate, and a receiver facing the outlet wired to a supplied load. Enumerate all four rows with and without the carrier.
- **Positive:** rows 01 and 10 with carrier emit 90% of the carrier the tick after the single control goes high.
- **Negative / controls:** rows 00 and 11 stay dark; any row without a carrier emits nothing; the output lamp never shows a fabricated beam.
- **Boundaries:** simultaneous control transitions (01 → 10 in one tick keeps open; 01 → 11 closes at the next tick); on 0.25 / off 0.225 hysteresis; rotation/occlusion; bounded chained routing.
- **Run/Reset:** Reset restores A = B = low and closed. **Save/Load:** round-trip.
- **Integrations:** receivers, other gates. Binding criteria [CAT-047](../requirements.md#current-cat-047). Suite: `tools/e2e/cat-optical-logic.test.ts`.

## Open questions

1. Control strength is the broadband mean in legacy, so a blue-filtered amber control (mean ≈ 0.107) never turns high (derived). The requirement does not state the control channel rule. Owner decision.
2. Story 13.8 says gates emit "output laser beams"; legacy routes only the separate carrier. Confirm carrier routing satisfies the story.
3. Tick order: the requirements commit optical readings before the electrical solve that reads them ([todo-290](../requirements.md#todo-290)); [docs/gpu-f32-physics.md](../../gpu-f32-physics.md#solver-model) puts electrical in Phase 1 and optics in Phase 2. Owner S486.
