# CAT-008 · beam_splitter — declaration readiness spec (CAT-008-D)

## Identity

- **CAT ID / kind:** CAT-008 · `beam_splitter` (catalogue title "Beam splitter", category Optics).
- **Requirement anchor:** [CAT-008](../requirements.md#current-cat-008); retained behaviour [todo-294](../requirements.md#todo-294).
- **Mapped identities:** none. No EL/TH/RAD/GAP identity names a beam splitter; the requirements row stands as the source.
- **Roadmap story:** 13.5 (Planar Reflection Mirror, Beam Splitter & Beam Combiner, shared with CAT-041 and CAT-006).
- **Status:** not started.

## Declaration

Lengths are legacy scene units; the current engine treats scene units as metres (`MetreVector`).

- **Bodies and shapes:** one static rigid body. Opaque cream frame: two rails 0.18 × 0.18 × 1.65 at (0, ±0.74, 0) and two posts 0.18 × 1.3 × 0.18 at (0, 0, ±0.74); opaque base 0.9 × 0.2 × 1.65 at (0, −1, 0). A **transparent but collidable** pane, half-extents (0.015, 0.65, 0.65) at the origin: it stops balls and passes light.
- **Optical aperture:** port `Main`, interaction Split, centre (0, 0, 0), normal (−1, 0, 0), finite disc radius 0.65, **both faces** active; transmission (1, 1, 1).
- **Mass and material:** none — static body.
- **Constraints and joints:** none.
- **Typed sockets and ports:** none.
- **Sensors and activation:** none.
- **Work and energy stores:** none. Each incident ray divides every RGB component 50 : 50 into a transmitted ray (same direction) and a reflected ray (mirror reflection about the normal). The two branches share the parent's remaining range and the per-path interaction count; nothing is duplicated.
- **Parameters:** none. Aim with the same rotation gizmo as a mirror.
- **Cosmetic curves and UI bindings:** none animated. Selected in construction, it previews **both** outgoing paths without activating receivers (composition "Separate"). Running branch beams render fainter than the parent (opacity follows power). Selection pick radius 1.2 (`parts/BeamSplitterPart.cs@a6c914e:L14-L14`).
- **Art:** cream frame `#fff8e9`, navy base `#293954`, cyan pane `#66b8c9` at alpha 0.25 (0.03 × 1.3 × 1.3, no shadow, double-sided), ochre coating ring `#e8b764` (r 0.64, tube 0.015), paired gold studs `#f7cb52` (r 0.055) at (0, 0.74, ±0.24). See [DESIGN.md](../../../DESIGN.md) row "Beam splitter".
- **Catalogue and inventory entry:** id `beam_splitter`, title "Beam splitter", category Optics, colour (0.4, 0.72, 0.79), description "Half the beam continues straight; half reflects from the glass. Rotate with the same gizmo as a mirror. Selecting it previews both paths. Further splitting may leave too little power for a receiver." Icon `ui/WorkshopIcons.cs@a6c914e:L81-L81` (current, kept). No legacy level uses it.

## Engine capabilities

Families (map row CAT-008): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, RigidBodyDynamics, SensibleHeat, SlidingFriction.

**Exists now**
- Static box body/colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`. There is no per-collider optical opacity flag yet.
- Binary16 debt: at a6c914e `MetreVector`, `LinearSpeed` (the contact-trigger threshold type) and `AccelerationVector` are `Half` — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L18-L21`; `RigidLocalPose` translation is a `Half` vector bounded to ±16 — `engine/gpu/PhysicsDeclarations.cs@a6c914e:L31-L38`; `WorkshopCosmeticSample.Blend` is `Half` — `engine/gpu/WorkshopCosmetic.cs@a6c914e:L10-L10`. These are remaining lanes in the [f32 migration status](../../gpu-f32-physics.md#f32-migration-status); this element's new values are declared as f32.

**Missing**
- OpticalTransport two-sided split with finite branch allocation, shared range and segment caps — Story 13.5. Decision owner **S484** ([decisions](../invest/decisions.md#s484)); S485 finite-colour allocation is the governing row ("neither receiver reads more than it would alone").
- Collider optical opacity (transparent pane versus opaque frame) — Story 13.1 occlusion; S488 transparent/hollow paths.
- SensibleHeat coupling — S543, mode-specific only.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser (Story 13.1), CAT-038 light_receiver ×2 (Story 13.2), CAT-005 battery (Story 8.1), CAT-066 wall (delivered), CAT-001 ball (delivered; pane collision control), CAT-041 mirror (bounded-loop control).

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

Splitter facts:
1. Aperture `Main`, Split, origin, normal −X, radius 0.65, transmission 1 — `parts/BeamSplitterPart.cs@a6c914e:L9-L9`. Carry forward.
2. Split accepts either face (rejects only grazing incidence) — `engine/OpticalNetwork.cs@a6c914e:L94-L97`. Carry forward.
3. Split emits straight and reflected branches at 0.5 power each, with the parent's remaining range and depth + 1 — `engine/OpticalNetwork.cs@a6c914e:L113-L116`, `engine/OpticalNetwork.cs@a6c914e:L128-L131`. Carry forward.
4. Frame, transparent collidable pane and art — `parts/BeamSplitterPart.cs@a6c914e:L12-L36`; the pane flag is `Opaque = false` on `BoxProxy` — `reference/cpu/MachinePart.cs@a6c914e:L11-L11`; full sizes passed to `AddBox` become half-extents — `reference/cpu/MachinePart.cs@a6c914e:L352-L356`. The pane passes light because light traces query only opaque children (S8). Carry forward geometry; not the Godot proxy type.
5. Preview composition Separate — `parts/BeamSplitterPart.cs@a6c914e:L10-L11`. Carry forward.
6. Catalogue entry — `parts/catalog/beam_splitter.tres@a6c914e:L6-L11`; scene — `parts/scenes/beam_splitter.tscn@a6c914e:L1-L4`. Carry forward the entry.
7. Acceptance: from front or back (45° splitter), straight and reflected receivers each get beam × 0.5 (±1e-5) over 3 segments; a wall on the reflected leg blocks only that receiver; running lights receivers independently; default receiver threshold 0.25; Reset clears paths and readings — `CuriousContraptions.tests/BeamSplitterTests.cs@a6c914e:L16-L57`. Carry forward.
8. Acceptance: two cascaded splitters give 3 receptions whose sum equals the full beam (±1e-5); the half-power receiver lights; the quarter-power receiver has power > 0 but stays dark (amber quarter broadband mean 0.175 < 0.25) — `CuriousContraptions.tests/BeamSplitterTests.cs@a6c914e:L58-L81`. Carry forward.
9. Acceptance: light traverses the glass unobstructed (trace 8 of 8 at the centre); the frame at y + 0.74 blocks (< 2); a ball at 4 /s rebounds from the glass and never crosses x = 0 — `CuriousContraptions.tests/BeamSplitterTests.cs@a6c914e:L82-L102`. Carry forward.
10. Acceptance: a splitter between two facing mirrors with a range-100 unit beam hits the 128-segment cap, never amplifies (every segment power ≤ 1) and repeats deterministically — `CuriousContraptions.tests/BeamSplitterTests.cs@a6c914e:L103-L120`. Carry forward.
11. Acceptance: the selected preview matches the trace, with miss, zero-power, deselect, Run-hidden, Reset and Save/Load controls — `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs@a6c914e:L45-L82`; several selected optics share one source read; removal releases targets — `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs@a6c914e:L83-L100`. Carry forward.

**Files harvested:** `parts/BeamSplitterPart.cs`, `parts/catalog/beam_splitter.tres`, `parts/scenes/beam_splitter.tscn`, `engine/OpticalNetwork.cs`, `engine/WorldGeometry.cs`, `engine/physics/BodyQueryGeometry.cs`, `reference/cpu/MachinePart.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/BeamSplitterTests.cs`, `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs`, `CuriousContraptions.tests/OpticalPortsTests.cs`.

## Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a supplied, triggered laser; a splitter on its ray rotated 45° with the gizmo; one receiver on the straight leg and one on the reflected leg, each wired to its own supplied load. Select the splitter to see both previews; Run.
- **Positive:** both receivers light, each with half power.
- **Negative / controls:** wall blocking one branch (other stays lit); second splitter below threshold (quarter branch dark); reverse incidence still splits; ray hitting the opaque frame stops; a ball cannot pass the pane.
- **Boundaries:** 16 interactions per path, 128 segments per emitter, one shared range.
- **Run/Reset:** Reset clears both branches. **Save/Load:** orientation round-trips.
- **Integrations:** mirrors, filters, receivers. Binding criteria [CAT-008](../requirements.md#current-cat-008). Suite: `tools/e2e/cat-041-008-006.test.ts`.

## Open questions

1. Observer for the split: resolved by the 9 Oct 2026 reorder. The Story 13.2 receivers now precede Story 13.5, so a receiver is available as the observer.
2. The legacy interaction cap counts depth along each path, so a splitter tree can run 16 deep per branch until 128 segments. Confirm "16 interactions per path" means per branch depth (as legacy), not per tree. Owner S484 (S485).
3. Heating of the pane (SensibleHeat) has no legacy behaviour. Owner S543, mode-specific.
