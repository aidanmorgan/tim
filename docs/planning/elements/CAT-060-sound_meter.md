# CAT-060 · sound_meter — declaration readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline. Legacy tick counts are at 120 Hz (`reference/cpu/MachineWorld.cs@a6c914e:L20-L21`).

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-060 · `sound_meter` (catalogue title "Sound meter") |
| Requirement anchor | [CAT-060](../requirements.md#current-cat-060) and its [retained behaviour](../requirements.md#todo-346); D/I/V row [CAT-060-I](../invest/current-consumers.md#cat-060-i) |
| Mapped identities | None directly. [EL-044 Tone-selective sound meter](../invest/named-elements.md#element-044) is a distinct variant with its own band filter, not merged. No TH, RAD or GAP identity. |
| Capability row | [general-engine-element-map, CAT-060](../general-engine-element-map.md) |
| Roadmap story | Epic 14, Story 14.1 "Sound Level Meter Threshold Sensor" |
| Status | Not started. |

## Declaration

- **Bodies and shapes.** One static body: housing box size 1.6 × 1.5 × 0.65 at the origin and base box at (0, −0.85, 0) size 1.8 × 0.18 × 0.85 (half-extents = size/2) (`parts/SoundMeterPart.cs@a6c914e:L87-L88`; `reference/cpu/MachinePart.cs@a6c914e:L352-L356`).
- **Mass and material.** Static; legacy static default material (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`).
- **Constraints and joints.** None.
- **Typed sockets and ports.** `PowerIn` (electrical, input) at (−0.92, −0.5, 0); `Supply` (electrical, output) at (0.92, −0.5, 0); `ActivationOut` (activation, output) at (0.92, 0.45, 0) (`parts/SoundMeterPart.cs@a6c914e:L49-L55`).
- **Sensors and activation.**
  - Acoustic receiver point at the local origin; the level is the strongest direct arrival (0–1) from the shared acoustic network (`parts/SoundMeterPart.cs@a6c914e:L48-L48`; `engine/Acoustics.cs@a6c914e:L49-L80`). Levels outside [0, 1] or non-finite reject (`parts/SoundMeterPart.cs@a6c914e:L64-L67`).
  - Threshold with hysteresis: turns on when level ≥ T, stays on while level > 0.9 T (`parts/SoundMeterPart.cs@a6c914e:L68-L70`).
  - Electrical contact `PowerIn` → `Supply` is closed while above threshold (`parts/SoundMeterPart.cs@a6c914e:L56-L57`).
  - On a rising crossing, if `PowerIn` is supplied in that tick, emit one activation on `ActivationOut` and count a trigger; an unpowered crossing is discarded with no replay when supply returns (`parts/SoundMeterPart.cs@a6c914e:L72-L83`).
  - Active (lamp) = above threshold and supplied (`parts/SoundMeterPart.cs@a6c914e:L76-L76`).
  - Committed level published as scalar observation slot 0, dimensionless (`parts/SoundMeterPart.cs@a6c914e:L30-L34`).
- **Work and energy stores.** None.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit |
  | --- | --- | --- | --- | --- |
  | threshold | f32 | 0.05–1 | 0.25 | dimensionless level |

  `parts/SoundMeterPart.cs@a6c914e:L58-L63`; `parts/catalog/sound_meter.tres@a6c914e:L12-L12`; parameter enum `engine/MachineData.cs@a6c914e:L80-L80`.
- **Cosmetic curves and UI bindings.**
  - Needle: rotation about Z from a construction angle of 1 rad, following the committed level linearly over input 0–1 to an offset of 0 → −2 rad with exponential response 18 /s on the presentation clock; angle = 1 − 2 × level × (1 − e^(−18 t)) (`parts/SoundMeterPart.cs@a6c914e:L42-L45`, `L95-L95`; `CuriousContraptions.tests/SoundMeterAnimationTests.cs@a6c914e:L64-L66`).
  - Lamp: colour `#556573` → `#f7cb52`, SmoothStep over 0.1 s, once, on owner active, endpoint drive (`parts/SoundMeterPart.cs@a6c914e:L43-L43`, `L46-L47`).
- **Art.** Housing `#66b8c9`, base `#293954`; dial face 1.35 × 1.15 × 0.04 `#fff8e9` at (0, 0.04, 0.35); seven tick dots radius 0.025 `#293954` on an arc of radius 0.5 about (0, −0.2) over −1…1 rad at z 0.4; needle 0.035 × 0.44 × 0.035 `#e8b764` pivoting at (0, −0.2, 0.42); hub sphere of radius 0.065 `#293954` at (0, −0.2, 0.46); lamp sphere of radius 0.065 at (0, −0.43, 0.41) (`PartArt.Sphere` radius argument, `parts/SoundMeterPart.cs@a6c914e:L97-L98`); port spheres radius 0.075 `#f7cb52`; pick radius 1.2 (`parts/SoundMeterPart.cs@a6c914e:L84-L100`). Catalogue colour (0.4, 0.72, 0.79) (`parts/catalog/sound_meter.tres@a6c914e:L11-L11`); icon `ui/WorkshopIcons.cs@a6c914e:L37-L37`.
- **Catalogue and inventory entry.** Id `sound_meter`, title "Sound meter", category Sound; description "The needle shows the strongest direct sound pulse. Supply electricity to send one trigger when sound rises past the threshold, and pass supply while it remains loud. Quiet rearms it. Solid opaque obstacles block reception." (`parts/catalog/sound_meter.tres@a6c914e:L5-L12`).

## Engine capabilities

Families: the [CAT-060 row](../general-engine-element-map.md) and `docs/coverage/catalogue-elements.json` (AcousticPropagation, AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction).

- **Exists now.** Static boxes; activation output and wiring (`engine/gpu/ActivationNetwork.cs`; `WorkshopSocket.ActivationOut`, `Supply`, `PowerIn` in `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`); animation worker with activation-driven colour blends (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L50-L59`).
- **Missing.**
  - Acoustic propagation, opaque occlusion and strongest-arrival reception: Story 14.1, decision owner S528 ([decisions](../invest/decisions.md#s528)): S530 arrival combination (strongest versus additive), S529 event routing.
  - ElectricalPower and an electrical contact route through a sensor: Story 8.1 (CAT-005); S257 typed power versus signal.
  - Scalar follow cosmetic (needle) and supplied-and-above lamp: Story 14.1.
- **Element dependencies.** An acoustic source (Bell CAT-009, Speaker CAT-061 or Wind chimes CAT-069); Battery (CAT-005); a load such as Lamp (CAT-035) or Powered gate (CAT-051); Counter (CAT-020) on `ActivationOut`; Wall (CAT-066) as occluder.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Ports, threshold, hysteresis, contact route, powered rising-edge trigger and scalar output as in Declaration. | `parts/SoundMeterPart.cs@a6c914e:L11-L83` | Carry forward as declaration data. The per-part `PreparePhysics` loop is not carried forward. |
| 2 | Reception law: the level is the strongest single direct arrival, never a sum; opaque colliders on the straight line block; the receiver's own pulses are ignored; a disabled receiver reads 0; acoustic solve precedes the electrical solve in a tick. | `engine/Acoustics.cs@a6c914e:L49-L80`; `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`; `reference/cpu/MachineWorld.cs@a6c914e:L855-L861` | Carry forward pending S530. |
| 3 | Acceptance: speaker (−3, 6, 0) → supplied meter (1, 6, 0) with a Powered gate on `Supply` and a Counter on `ActivationOut`: exactly one trigger per speaker pulse, counted by the Counter; the gate is supplied during the pulse and released after it; a Wall at (−1, 6, 0) or a backward speaker gives zero level; an unsupplied meter hears but never triggers or supplies; a second pulse gives a second trigger; part order does not matter; Reset zeroes level, count and threshold state. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L14-L54` | Carry forward. |
| 4 | Acceptance: with T = 0.25, level 0.25 turns on, 0.23 stays on, 0.225 turns off, 0.24 stays off; NaN, 1.1 and threshold 0 reject. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L55-L71` | Carry forward. |
| 5 | Acceptance: two equal strength-1 sources give level 1 (not 2), in either order; a hidden meter reads 0. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L72-L95` | Carry forward pending S530. |
| 6 | Contact boundary: one ULP below T is open, T is closed; 0.9 T opens, one ULP above 0.9 T stays closed. | `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L113-L130` | Carry forward; the transaction-rollback step in the same test is not carried forward. |
| 7 | Acceptance: for silent, just-below, threshold and full levels, powered or not, the committed scalar equals the level; the needle angle follows 1 − 2 × level × (1 − e^(−18 t)); the lamp is half-blended at 0.05 s only when supplied and above; pause, hide, Reset and Save/Load restore the construction needle and colour. | `CuriousContraptions.tests/SoundMeterAnimationTests.cs@a6c914e:L33-L97` | Carry forward; failed-tick cases are not carried forward. |
| 8 | Invalid gauge bindings (unit, slot, empty, reversed or infinite range, axis, mapping, physical target) reject before Run; narrowing the gauge range changes only presentation. | `CuriousContraptions.tests/SoundMeterAnimationTests.cs@a6c914e:L98-L166` | Carry forward as binding validation. |
| 9 | Reception uses the meter's solved pose, not its rendered transform. | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L322-L354` | Carry forward. |
| 10 | Chains: bell → meter → hold timer → powered gate; wind chimes → meter → counter. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L60-L102`; `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L46-L98` | Carry forward (see CAT-009, CAT-069). |
| 11 | A failed arrival restores hysteresis and the retry delivers one powered edge. | `CuriousContraptions.tests/SoundMeterCheckpointTests.cs@a6c914e:L25-L76` | Do not carry forward: no faulting ticks. Keep "one powered edge per crossing". |
| 12 | No level uses the sound meter. | `docs/coverage/catalogue-elements.json` (empty fixture list) | Recorded. |

**Files harvested:**
- `parts/SoundMeterPart.cs`
- `parts/catalog/sound_meter.tres`
- `parts/scenes/sound_meter.tscn` (no element knowledge: script reference only)
- `engine/Acoustics.cs`
- `engine/MachineData.cs`
- `engine/physics/BodyQueryGeometry.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/SoundMeterTests.cs`
- `CuriousContraptions.tests/ElectricalContactBindingTests.cs`
- `CuriousContraptions.tests/SoundMeterAnimationTests.cs`
- `CuriousContraptions.tests/SoundMeterCheckpointTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/BellTests.cs`
- `CuriousContraptions.tests/WindChimeTests.cs`

## Acceptance outline

Point of truth: [CAT-060](../requirements.md#current-cat-060) and [retained behaviour](../requirements.md#todo-346).

- **Chrome construction.** Through the real palette, place a Bell with a ball above it, the Sound meter facing it, a Battery and a Lamp; wire Battery → meter `PowerIn` and meter `Supply` → Lamp with the real socket UI; set the threshold with the real control.
- **Positive.** Run: the bell rings, the needle rises, the meter closes its contact and the lamp lights while the sound is loud.
- **Negative or control.** A Wall between bell and meter, no battery, a source facing away and a quiet strike each leave the lamp dark; an unpowered crossing is not replayed when power returns.
- **Boundaries.** Threshold 0.05–1; on at ≥ T, off at ≤ 0.9 T; strongest arrival, not sum; one trigger per crossing.
- **Run/Reset.** Reset zeroes level, trigger count, needle and lamp exactly.
- **Save/Load.** Threshold and wiring survive; the outcome repeats.
- **Integrations.** Bell, Speaker and Wind chimes sources; Counter, Hold timer, Powered gate and Lamp loads; muted audio gives the same outcome.

## Open questions

1. **Facing.** The requirement mentions "facing/opaque-blocking"; legacy reception is a point receiver with no facing test (only cone sources filter direction). Decide whether the meter itself has a reception cone. Owner decision.
2. **Tone filtering.** The requirement says "all admitted tone filtering"; legacy CAT-060 accepts every band (EL-044 is the band-selective variant). Confirm CAT-060 stays unfiltered.
3. **Lamp wiring.** Story 14.1 lights "a connected lamp"; the current Lamp takes an activation input (`engine/gpu/WorkshopConnections.cs@a6c914e:L36-L39`), while the meter's sustained output is electrical. Decide which route the lesson uses.
4. **Arrival combination.** S530 must confirm strongest-arrival over additive intensity.
