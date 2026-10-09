# CAT-069 · wind_chimes — declaration readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline. Legacy tick counts are at 120 Hz (`reference/cpu/MachineWorld.cs@a6c914e:L20-L21`): 24 ticks = 0.2 s.

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-069 · `wind_chimes` (catalogue title "Wind chimes") |
| Requirement anchor | [CAT-069](../requirements.md#current-cat-069) and its [retained behaviour](../requirements.md#todo-348); D/I/V row [CAT-069-I](../invest/current-consumers.md#cat-069-i) |
| Modes | AcousticTone = Low, Mid, High (fixed per tube) |
| Mapped identities | None. No EL, TH, RAD or GAP identity names the wind chimes; the [acoustic family](../../component-research.md#sound) and the [gas family](../../finite-gas-foundation.md) both list them. |
| Capability row | [general-engine-element-map, CAT-069](../general-engine-element-map.md) |
| Roadmap story | Epic 14, Story 14.4 "Resonant Wind Chimes Airflow Percussion" |
| Status | Not started. |

## Declaration

- **Bodies and shapes.**
  - Static root: top box at (0, 0.93, 0), size 1.2 × 0.12 × 1.2 (collider, drawn as a disc) (`parts/WindChimesPart.cs@a6c914e:L133-L134`).
  - Four static tube bodies, each a 16-sided convex cylinder of radius 0.065 (radial error < 0.0013 m) (`engine/ChimeAssembly.cs@a6c914e:L18-L18`, `L57-L72`; `parts/WindChimesPart.cs@a6c914e:L49-L55`, `L141-L141`):

    | Tube | Centre (m) | Length (m) | Tone |
    | --- | --- | --- | --- |
    | Right | (0.4, 0.2, 0) | 0.9 | Mid |
    | Left | (−0.4, 0.075, 0) | 1.15 | Low |
    | Front | (0, 0.275, 0.4) | 0.75 | High |
    | Back | (0, 0.15, −0.4) | 1.0 | Mid |

    `engine/ChimeAssembly.cs@a6c914e:L8-L9`, `L29-L35`.
  - One dynamic pendulum body hung from the pivot (0, 0.9, 0): clapper sphere radius 0.12 at 0.9 m below the pivot (0.6 × 1.5) and a sail box with half-extents (0.15, 0.2, 0.0275) at 1.5 m below, rotated 35° about Z (`engine/ChimeAssembly.cs@a6c914e:L15-L28`; `parts/WindChimesPart.cs@a6c914e:L148-L150`).
- **Mass and material.** Clapper 0.2 kg, sail 0.15 kg, total 0.35 kg; centre of mass (0.2 × 0.9 + 0.15 × 1.5)/0.35 ≈ 1.157 m below the pivot; inertia = sail box + solid-sphere clapper (2/5 m r²) + parallel-axis shifts (`engine/ChimeAssembly.cs@a6c914e:L19-L23`, `L41-L56`). Pendulum material restitution 0.35, bounce threshold 0.08, friction 0.2; tube material restitution 1, threshold 0.08, friction 0.2 (`parts/WindChimesPart.cs@a6c914e:L43-L53`; argument order `engine/physics/PersistentContactPair.cs@a6c914e:L23-L23`). Pendulum linear and angular drag 0.9 /s (`engine/ChimeAssembly.cs@a6c914e:L22-L22`; `parts/WindChimesPart.cs@a6c914e:L88-L91`).
- **Constraints and joints.** One ball-socket joint between pendulum and root at the pivot; connected-body collision disabled; both directions (`parts/WindChimesPart.cs@a6c914e:L42-L42`, `L58-L70`). The pendulum starts hanging straight down in part space (`CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L125-L144`).
- **Typed sockets and ports.** None; no electrical input (`parts/catalog/wind_chimes.tres@a6c914e:L9-L9`).
- **Sensors and activation.**
  - Tube contact trigger: an impact on a tube counts when the approach speed is ≥ 0.08 m/s for the clapper or ≥ 0.8 m/s for any other body (`parts/WindChimesPart.cs@a6c914e:L96-L108`).
  - Strength = clamp(s / 1.5, 0.08, 1), where s = approach speed for the clapper, approach speed × other mass otherwise (`parts/WindChimesPart.cs@a6c914e:L103-L111`).
  - Rearm 24 ticks (0.2 s); same-tick strikes keep the strongest, ties going to the lower tube identity (Right < Left < Front < Back) (`parts/WindChimesPart.cs@a6c914e:L39-L39`, `L112-L123`).
  - Emits one omnidirectional pulse at the contact point, direction up, tone = the struck tube's band (`parts/WindChimesPart.cs@a6c914e:L124-L127`). Pulse law as CAT-009: 12 m/s, 8 m, 0.15 s, level strength / (1 + 0.08 d²) (`engine/Acoustics.cs@a6c914e:L11-L47`).
- **Airflow receiver.** One body-force sample at the sail centre on the pendulum body, weight 1 (`parts/WindChimesPart.cs@a6c914e:L74-L74`).
- **Work and energy stores.** None.
- **Parameters.** None authored; tube tones are fixed by tube identity.
- **Cosmetic curves and UI bindings.**
  - Pendulum art follows the committed pendulum pose.
  - Wavefront rings, omnidirectional (`parts/WindChimesPart.cs@a6c914e:L81-L81`). The surviving caller passes the positional tuple (0.6, 0, 1, 0.008, 0.3, Strength, 64) after its ring/pattern arguments. Its record definition is absent at baseline a6c914e, so the full field mapping is a baseline gap (Open question 5); no earlier-revision definition is treated as baseline evidence. A visible ring's outer radius is initial radius + distance × radius per distance + thickness (`CuriousContraptions.tests/AcousticWavefrontTests.cs@a6c914e:L63-L68`). The art supplies 15 torus meshes (cycling three axes) of radius 1 and half-width 0.008, each set separately to 64 rings (`parts/WindChimesPart.cs@a6c914e:L154-L162`, `L158-L158`).
  - Audio: Bell voice for each band (220/440/880 Hz), −15 dB, max distance 20, polyphony 2 (`parts/WindChimesPart.cs@a6c914e:L151-L153`; `engine/AcousticAudio.cs@a6c914e:L11-L31`). Playback happens on the tick after the strike and uses the final strongest tone of the strike tick (`parts/WindChimesPart.cs@a6c914e:L128-L128`; `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L333-L338`).
- **Art.** Ring "thickness" values are torus half-widths (inner r − t, outer r + t) and line "width" values are cylinder radii (`engine/PartArt.cs@a6c914e:L22-L27`). Top disc radius 0.6, height 0.12, `#fff8e9`; knob sphere of radius 0.1 `#293954` at (0, 1.06, 0); each tube hung by a `#293954` line of radius 0.008 from y 0.9 to 0.65; tube cylinders radius 0.065 `#e8b764`, each with a `#293954` foot torus of radius 0.065 and half-width 0.008; pendulum line of radius 0.01 `#293954`; clapper `#fff8e9`; sail 0.3 × 0.4 × 0.055 `#66b8c9`; pick radius 1.3 (`parts/WindChimesPart.cs@a6c914e:L130-L150`). Catalogue colour (0.91, 0.72, 0.39) (`parts/catalog/wind_chimes.tres@a6c914e:L11-L11`); icon `ui/WorkshopIcons.cs@a6c914e:L35-L35`.
- **Catalogue and inventory entry.** Id `wind_chimes`, title "Wind chimes", category Sound; description "Aim a fan at the cyan sail. Wind swings the clapper; hitting a tube makes sound, not simply being near a fan. A moving ball can strike the tubes too. Sound radiates in every direction and can trigger a powered sound meter. No electrical input." (`parts/catalog/wind_chimes.tres@a6c914e:L5-L11`).

## Engine capabilities

Families: the [CAT-069 row](../general-engine-element-map.md) and `docs/coverage/catalogue-elements.json` (AcousticPropagation, AerodynamicDrag, AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, SlidingFriction).

- **Exists now.** Dynamic bodies with full inertia and declared linear drag (`engine/gpu/RigidMassProperties.cs`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L82`); sphere and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`); `ContactTriggerDeclaration` with an approach-speed threshold per declared target set (`engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L7-L18`).
- **Missing.**
  - Ball-socket (3D pendulum) joint: Story 10.4 (CAT-067).
  - Angular drag: the current solver has none ([f32 capability inventory](../../gpu-f32-physics.md)); needed for the 0.9 /s angular rate. Story 14.4 or an owner decision.
  - Cylinder or convex-hull collider for the tubes: Story 6.6 (CAT-048a) compound cylindrical collider, or an owner decision.
  - Airflow body-force receiver: Story 12.2 (CAT-028); decision owner S470 ([decisions](../invest/decisions.md#s470)).
  - Acoustic occurrence (14.2) and propagation/reception (14.1), decision owner S528 ([decisions](../invest/decisions.md#s528)); per-tube tone selection, strongest-with-tie-break coalescing and next-tick playback: Story 14.4.
- **Element dependencies.** Fan (CAT-028) or Bellows (CAT-010) as air source; a striking ball for the direct-strike path; Sound meter (CAT-060), Battery (CAT-005) and Counter (CAT-020) for reception; Wall (CAT-066) for occlusion.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Geometry, masses, materials, joint, drag, sample and contact rules as in Declaration. | `engine/ChimeAssembly.cs@a6c914e:L8-L72`; `parts/WindChimesPart.cs@a6c914e:L39-L129` | Carry forward as declaration data. The per-substep drag call and per-part contact callback are not carried forward. |
| 2 | Acceptance: fan at (−4, 5.4, z), chimes at (−1, 6, 0), supplied meter at (2, 6, 0) with a Counter. Force 9 rings and triggers the meter (in either part order); force 0.01 or 0, the fan 2 m off in depth, a Wall at (−2.5, 5.4, 0) yawed 90° and an unpowered fan do not. The first tick never rings (overlap is not a trigger); the pendulum length stays 1.5 ± 0.0001 m; ≤ 5 pulses in flight; tip speed ≤ 20 m/s; a ringing run moves the sail > 0.35 m. Reset restores the hanging pose; Save/Load replays the same count. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L46-L98` | Carry forward. |
| 3 | Acceptance: fan and chimes yawed 0°, 90°, 180° and 270° ring within 120 ticks; presentation calls leave physics unchanged. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L99-L124` | Carry forward. |
| 4 | Acceptance: tilting the part while editing keeps the authored rest pose until Run; Reset restores it. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L125-L144` | Carry forward. |
| 5 | Acceptance: after the fan stops the pendulum coasts and rests (tip speed < 0.001 m/s within 2400 ticks) with no further pulses; restarting the fan rings again. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L145-L168` | Carry forward. |
| 6 | Acceptance: a ball struck at 5 m/s into the tubes rings them and rebounds by ordinary collision; a ball passing 2 m away in depth does not. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L169-L190` | Carry forward as the direct-strike control path. |
| 7 | Acceptance: a tilted assembly keeps the pendulum at 1.5 m with joint error ≤ 1.01e-7 for 1200 ticks and still rings after an impulse. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L280-L302` | Carry forward; the error bound becomes the game-grade envelope. |
| 8 | Acceptance: two opposed fans cancel (no ring, force ≈ 0); zero pressure gives zero air force. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L303-L319` | Carry forward. |
| 9 | Acceptance: 0.1 kg and 1 kg balls at 6 m/s strike the Left and Right tubes in the same tick: one pulse, strength 1, tone Mid, in either order; on the next tick playback uses the Mid waveform and no second pulse appears. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L320-L343` | Carry forward. |
| 10 | Undefined tube identities reject. | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L356-L372` | Carry forward; enums end to end. |
| 11 | The bellows burst rings the chimes at (−1, 6.48, 0); a blocking Wall silences them. | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L455-L490` | Carry forward (see CAT-010). |
| 12 | The air sample is anchored to the solved pendulum, not the rendered sail; disabling the pendulum removes the force; Reset and replay reproduce it. | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L285-L320` | Carry forward. |
| 13 | Only committed waves render: 3 rings visible after 12 ticks; Reset restores ring transforms and radii. | `CuriousContraptions.tests/AcousticWavefrontTests.cs@a6c914e:L27-L87` | Carry forward. |
| 14 | Failed emission restores queue and cooldown. | `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs@a6c914e:L26-L92` | Do not carry forward: no faulting ticks. |
| 15 | Lesson recipe "Wind in the Tower": fan swings the sail → clapper impact → meter → ball into pipe; one reliable strike, not rhythm. | [component-research, sound puzzle recipes](../../component-research.md#sound) (current document) | Carry forward to Epic 15. |
| 16 | No level in `content/puzzles.json` uses the chimes. | `docs/coverage/catalogue-elements.json` (empty fixture list) | Recorded. |

**Files harvested:**
- `parts/WindChimesPart.cs`
- `parts/catalog/wind_chimes.tres`
- `parts/scenes/wind_chimes.tscn` (no element knowledge: script reference only)
- `engine/ChimeAssembly.cs`
- `engine/Acoustics.cs`
- `engine/AcousticAudio.cs`
- `engine/PartArt.cs` (ring half-width and line radius conventions)
- `engine/physics/PersistentContactPair.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/WindChimeTests.cs`
- `CuriousContraptions.tests/BellowsTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/AcousticWavefrontTests.cs`
- `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs`

Baseline gap: the wavefront record definition was deleted before a6c914e; the surviving caller tuple and test assertions above are the harvested facts.

## Acceptance outline

Point of truth: [CAT-069](../requirements.md#current-cat-069) and [retained behaviour](../requirements.md#todo-348).

- **Chrome construction.** Through the real palette, place the Wind chimes, a Fan aimed at the sail and a supplied Sound meter with a Counter; wire with the real socket UI.
- **Positive.** Run: the fan swings the sail, the clapper strikes a tube, one omnidirectional pulse is emitted with that tube's band, playback of that tone starts on the next tick, and the meter triggers.
- **Negative or control.** Calm air (fan off or weak), a fan aimed off in depth, a Wall in the jet and opposed fans each give zero ringing; fan overlap alone never rings.
- **Boundaries.** Clapper threshold 0.08 m/s, other bodies 0.8 m/s; 0.2 s rearm; ≤ 5 pulses in flight; strongest same-tick strike with tie by tube identity; playback exactly one tick after the strike with the chosen tone; Low, Mid and High tubes each qualified.
- **Run/Reset.** Reset restores the hanging pendulum and clears pulses exactly.
- **Save/Load.** Placement survives; replay gives the same pulse count.
- **Integrations.** Fan and Bellows sources; ball strike path; Sound meter.

## Open questions

1. **Strike model.** Story 14.4 says "the chime tubes strike each other"; legacy and the requirement have static tubes struck by the clapper or a ball. Owner decision.
2. **Angular drag.** The legacy pendulum uses 0.9 /s angular drag; the current solver declares no angular drag. Decide whether to add it or retune.
3. **Tube collider.** 16-sided hull versus a true cylinder collider (Story 10.1, ENGINE-CYLINDER). Owner decision.
4. **Rearm in seconds or ticks.** 24 ticks at 120 Hz versus the selectable cadence (`engine/gpu/WorkshopCadence.cs@a6c914e:L6-L6`). Owner decision.

5. **Wavefront field mapping.** Unspecified — owner decision: confirm the intended declaration mapping for the preserved caller tuple because the baseline has no defining record. Preserve the independently sourced radius formula and art values; do not infer missing field names from positional values alone.
