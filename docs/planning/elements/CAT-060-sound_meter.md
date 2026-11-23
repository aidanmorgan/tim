# CAT-060 · sound_meter — element spec

**Current delivery (24 October 2026):** [CAT-060 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

One current text, repaired 22 November 2026 after the Batch D audit, against checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; citations with `@a6c914e` are the legacy harvest and appear only in the closing appendix. Values marked *proposed* are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). The shared acoustic facts, laws (L1–L7), capability rows (C1–C13, N1–N13), tolerances and Story 14.0 gaps 1–16 are in [EL-044](EL-044-tone-selective-sound-meter.md) and are cited by number.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-060 · `sound_meter` (catalogue title "Sound meter") |
| Requirement anchor | [CAT-060](../requirements.md#current-cat-060) with [todo-346](../requirements.md#todo-346); owner key [CAT-060-I](../invest/current-consumers.md#cat-060-i) |
| Disposition | To deliver as a single element in Story 14.1, after the engine story 14.0 and the Bell (14.2), its acceptance source (owner, 18 October 2026). It forms the **Sound meters** family with [EL-044](EL-044-tone-selective-sound-meter.md) (`tuned_sound_meter`): the two declarations differ only in the receiver's band mask, the `band` parameter and the fork and band-bar art. EL-044 may share this slice as a Family slice if the slice's probe finds a receiver band mask delivered; otherwise it follows as its own slice |
| Planned package | id and catalogue key `sound_meter`, directory `elements/SoundMeter/`, category `ElementCategory.Control`. Palette order: 424 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)) |
| Capability row | [general-engine-element-map, CAT-060](../general-engine-element-map.md) |
| Status | Not started; blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, and on `ENGINE-SIGNAL-NODES` (the threshold node); then Story 14.0 and Story 14.2 |

## 2. Declaration

Closest delivered shape: the Impact switch (static scene body, a signal node that closes a supplied contact and emits a pulse; `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:12`, `:40`–`:42`, `:71`–`:83`). Interfaces: `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the acoustic receiver interface Story 14.0 adds.

| Item | Declaration |
| --- | --- |
| Body | one static body: cabinet box 1.6 × 1.5 × 0.65 m at the origin; foot box 1.8 × 0.18 × 0.85 m at (0, −0.84, 0), so its top (−0.84 + 0.09 = −0.75) meets the cabinet's bottom (−0.75; at the legacy −0.85 the cabinet floated 0.01 m above the foot); three socket spheres r 0.075 m centred on the cabinet's side faces at the port positions. Material restitution 1, friction 0.3, rolling resistance 0, threshold 0.1 m/s (the static default, `ImpactSwitchElementDeclaration.cs:50`) |
| Receiver | one point at the local origin, omnidirectional, accepting all three tone bands |
| Threshold node | on when level ≥ `threshold`; off when level ≤ 0.9 × `threshold`. A rising crossing emits one `ActivationOut` pulse only if `PowerIn` is supplied on that tick; an unsupplied crossing is discarded |
| Supply graph | one contact, `PowerIn` → `Supply`, closed while the node is on. Electrical node: role Switch, rated voltage 12, resistance 0.05, capacitance 0, maximum power 100 W (the Impact switch's values, `ImpactSwitchElementDeclaration.cs:77`). No store; the contact adds no limit and draws nothing |
| Footprint | 1 body, 0 moving, no constraints |

| Parameter | Kind | Unit | Min | Max | Default | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `threshold` | Real | none (level 0–1) | 0.05 | 1 | 0.25 | clamp; build-only |

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | PowerIn | Electrical | Input | — | (−0.80, −0.5, 0) |
| `Supply` | Supply | Electrical | Output | — | (0.80, −0.5, 0) |
| `ActivationOut` | ActivationOut | Signal | Output | Pulse | (0.80, 0.45, 0) |

The port positions sit on the cabinet's left and right faces, so each socket sphere is seated in its face (the legacy positions at x ±0.92 left the spheres hanging clear of the cabinet). [EL-044](EL-044-tone-selective-sound-meter.md) shares these ports and must use the same positions. Presentation: `Visual/Needle` local rotation Z from the committed level, 1 rad at level 0 to −1 rad at level 1, `ExponentialFollow`; `Visual/Lamp` albedo slate `#556573` to gold `#f7cb52`, `SmoothStep` 0.1 s, from `PresentationDriver.CommittedSupply` on `Supply` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`). Catalogue: title "Sound meter", colour cyan `#66b8c9`; description: "The needle shows the strongest sound that reaches it. Supply electricity and it sends one trigger when sound rises past the threshold, and passes power while it stays loud. Quiet rearms it. Solid obstacles block sound."

## 3. Art and solidity

| Node | Mesh and colour | Class | Moved by |
| --- | --- | --- | --- |
| `Cabinet` | box, cyan `#66b8c9` | solid: box collider | nothing |
| `Foot` | box, navy `#293954` | solid: box collider | nothing |
| `Dial`, seven ticks, hub, `Lamp` | cream face 1.35 × 1.15 m with navy marks, printed flush on the front face | decorative: printed flat | nothing; the lamp's colour by a binding |
| `Needle` | gold bar 0.035 × 0.44 m on the dial | decorative: moved by a binding | the level binding |
| port spheres ×3 | r 0.075 m, gold `#f7cb52`, seated in the side faces | solid: sphere colliders | nothing |

No static proud visible solid lacks a collider. Icon: the dial and needle. At a glance it differs from the Tuned sound meter by having no fork on top and no band bars, and from the Speaker by a dial where the Speaker has a cone.

## 4. Engine capabilities

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body (C1) | present | EL-044 C1 |
| Finite electrical source feeding the contact (C3) | present | EL-044 C3 |
| Pulse signal port (C4) | present | EL-044 C4 |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| A supplied contact that follows a level and re-opens (C2) | narrower | a contact observes only `Latched` or `TimerCounting` (`CuriousContraptions.Abstractions/SignalContactControl.cs:5`): the level-following contact state is part of N4, owned by `ENGINE-SIGNAL-NODES` |
| Committed-value presentation (C12) | narrower | no acoustic level driver: Story 14.0 (the committed level observation of N4) |
| Emission, propagation, occlusion, receiver level (N1) | missing | Story 14.0 |
| Receiver threshold node with hysteresis, level-following contact state and a committed level (N4) | missing | the threshold node over a committed scalar and the level-following contact state: `ENGINE-SIGNAL-NODES` (coordinator decision 16; the same node the optical receivers and the gauge use); the committed acoustic level it reads: Story 14.0, gap 3 |
| Acoustic event and presentation-only audio (N12) | missing | Story 14.0, gap 11 |
| Committed acoustic read model (N13) | missing | Story 14.0, gap 14; the acceptance rows read levels through it |

Counts: 4 present, 0 untested, 2 narrower, 4 missing. Nothing here needs element-specific engine code.

## 5. Behaviour and numbers

- **Level** is the strongest admissible arrival, never a sum (L4), of any tone. Level = s / (1 + 0.08 d²) within 8 m (L3).
- **Hysteresis** of 10 % leaves a band 0.025 wide at the default threshold, so a pulse whose level steps on and off cannot chatter.
- **Reach at the default 0.25:** a strength-1 source (Speaker, a hard Bell strike) to 6.1 m; a Basketball's 0.67 Bell ring to 4.6 m. At the most sensitive 0.05 a strength-1 source is heard to the 8 m range (0.163 there).
- **Facing.** The meter hears from every side; direction belongs to the source (the Speaker's cone). The requirement's "wrong-facing" control is a Speaker turned away.
- **Tick order:** EL-044 L7. This is the requirement's "samples … coherently before electrical solve".
- **Loads.** The Signal lamp has only an activation input (`elements/SignalLamp/SignalLampElementDeclaration.cs:37`), so "lights a lamp" is `ActivationOut` → Signal lamp; the electrical `Supply` output is proven with a Powered gate (`elements/PoweredGate/PoweredGateElementDeclaration.cs:38`).

## 6. Acceptance

Planned class `SoundMeterTests`, real production simulation, parts placed through the production edit interface. Tolerances T-level and the 5 s stall guard are EL-044's.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | A Bell 4 m away is struck at full strength and the meter is supplied: level 1 / (1 + 0.08 × 16) = 0.439 within T-level, one `ActivationOut` pulse lights a Signal lamp, `Supply` is live while the level is above 0.225 | `ABellRingClosesTheContactAndPulsesOnce` | no striker: level 0, no pulse | thresholds 0.40 and 0.48 (triggers, does not); each Bell tone; meter turned 90° reads the same |
| 2 | A Wall on the straight path gives level 0 and no pulse | `AWallBlocksReception` | row 1 | Wall moved until the path just clears |
| 3 | Unsupplied, the needle still reads the level but no pulse leaves and `Supply` stays dead; connecting supply afterwards replays nothing | `AnUnpoweredCrossingIsDiscarded` | row 1 | supply arriving one tick after the crossing |
| 4 | At threshold 0.25 the node turns on at 0.25, stays on at 0.23, turns off at 0.225 and stays off at 0.24 | `HysteresisTurnsOnAtThresholdAndOffAtNinetyPercent` (exact: a contract of the node, fed declared levels) | — | the four levels |
| 5 | Two equal sources at equal distance read one source's level within T-level, in either placement order | `TwoSourcesReadTheStrongestNotTheSum` | one source | — |
| 6 | Two rings a second apart give two pulses | `QuietRearmsForTheNextRing` | one ring: one pulse | — |
| 7 | Bell → meter → Hold timer → Powered gate: the gate opens | `ABellStrikeOpensTheGateThroughTheMeter` | Wall between; meter unsupplied | — |
| 8 | `ActivationOut` → Counter counts one per ring | `EachRingCountsOnce` | — | — |
| 9 | An editor entry outside 0.05–1 is clamped; a saved or wire value outside it, or non-finite, is refused atomically with a typed reason | `ThresholdIsClampedInTheEditorAndRefusedAtLoad` | 0.05 and 1 accepted | first f32 beyond each limit |
| 10 | Reset clears level, node state, needle and lamp; save then load round-trips `threshold` and all three connections | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) plus `ResetClearsLevelStateAndNeedle` | — | Reset with a pulse in flight |
| 11 | `ActivationOut` linked to an Electrical input, or `Supply` to a Signal input, is refused | generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) | — | — |
| 12 | Muted and audible runs give identical levels and pulses | `MutingAudioChangesNothing` | — | — |
| 13 | Every drawn solid has its collider | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` with a `sound_meter` row | — | — |
| 14 | Wrong-facing source: a Speaker turned away gives level 0 | `SpeakerTests.DirectionRangeAndOcclusionSilenceTheMeter`, run in Story 14.3 | Speaker facing the meter | — |

Requirement criteria: threshold range and default (9); strongest direct pulse (5); coherent sampling before the electrical solve (3, tick order); opaque blocking (2); wrong facing (14); 90 % hysteresis (4); unpowered crossings discarded (3); quiet rearms (6); source order and repeats (5, 6); every admitted tone (1); the needle as committed feedback rather than audio (12). The Wind chimes chain runs in Story 14.4. Chrome recipe: post-catalogue.

## 7. Integration

Works with Battery, Signal lamp, Powered gate, Hold timer, Counter, Wall (delivered); Bell (planned before it); Speaker, Wind chimes, EL-044 and the other sound parts (planned after it). Campaign: first use in levels 71–80 ([campaign reservations](../invest/campaign-reservations.md)); no level places it today. Coverage binding: catalogue key `sound_meter`. Lesson-fixture story (S8) only where a level pre-places it.

## 8. Delivery

- **Shape.** One element slice, or a Family slice with EL-044. Data-only review tier if Story 14.0 delivers N1, N4, N12 and N13.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); `ENGINE-SIGNAL-NODES` before its first consumer (the threshold node and level-following contact of N4); then Story 14.0 amended with gaps 3, 11, 12, 13 and 14; Story 14.2 (Bell).
- **Requirement checkboxes.** This slice runs the Bell's row 12 and so ticks CAT-009. CAT-060's own "wrong-facing" criterion is row 14, which runs in Story 14.3; the CAT-060 checkbox is ticked by that later slice.
- **Files.** Adds `elements/SoundMeter/` (`SoundMeter.csproj`, `SoundMeterElementDeclaration.cs`, `sound_meter.tscn`, `icon.svg`) and `CuriousContraptions.tests/SoundMeterTests.cs`; one line in `CuriousContraptions.slnx`; one row in the visible-solid table. Membership tables are derived ([registration derivation](../../../_bmad-output/implementation-artifacts/spec-registration-derivation.md)); until [S3](../../../_bmad-output/implementation-artifacts/spec-s3-remaining-per-package-lists.md) lands it also edits that contract's five test-side lists.
- **Disqualified if** the slice's probe finds no level-following contact or no committed level driver (N4). Then that is an engine slice first.

## 9. Open questions

None open. Resolved:

1. Facing: the meter is omnidirectional; decided under the standing delegation, because a listening cone would make the Bell lesson fiddly and the requirement's control is a source turned away.
2. Tone filtering: none. [EL-044](EL-044-tone-selective-sound-meter.md) is the band-selective part ([D8](../invest/delivery-batches.md#3-family-level-decisions)).
3. Lamp wiring: `ActivationOut` → Signal lamp; `Supply` → Powered gate.
4. Arrival combination: the strongest arrival (L4).
5. Sockets seated in the cabinet's side faces, and the foot seated under the cabinet at y −0.84 (group repair, 23 November 2026, as [CAT-061](CAT-061-speaker.md) and [EL-044](EL-044-tone-selective-sound-meter.md)); dial, hub and lamp printed flush (coordinator decision, 22 November 2026, under the owner's standing delegation: a drawn solid may not hang clear of the part it belongs to).

## Engine conformance

Against the owner's conformance addendum of 22 November 2026 (element-free engine gate in [AGENTS.md](../../../AGENTS.md); design contracts D1–D7 in `_bmad/custom/element-free-engine-design.md`). **Verdict: conforms on paper; buildable after the engine programme, once `ENGINE-SIGNAL-NODES` delivers the threshold node and Story 14.0 the receiver.**

1. **Declaration data only.** A static body, five colliders, one receiver row, one threshold node, one supply contact, three ports, one parameter and its presentation bindings. No sampling loop or hysteresis code in the part.
2. **Generic laws.** Level: the reception law (strongest admissible arrival, occlusion, falloff). On and off: a receiver threshold node with declared on and off levels (N4), which EL-044 also declares. Passing power: the supply network's contact rule. The pulse: the signal network's pulse rule on the rising crossing.
3. **Layers.** Colliders: five hidden proxies (two boxes, three spheres). Visuals: the nodes of section 3; none is a collider. Animation: the needle and the lamp colour read committed state and write nothing back.
4. **One-way flow and typing.** The needle is feedback of the committed level, never an input (row 12). Ports are typed by domain, direction, role and signal kind.
5. **Flat state.** 1 static body; 5 colliders (2 boxes, 3 spheres); 0 joints, drives and stores; 1 acoustic receiver; 1 signal node; 1 supply node and 1 electrical node; 3 ports; 1 event kind; 4 presentation bindings (needle angle and three colour channels of the lamp). All fixed at compile time.
6. **Performance.** Static body; five primitive colliders, the three socket spheres following the Ball detector's precedent of a solid drawn socket. A receiver is a point, not a volume. Reception work is Story 14.0's to bound from compiled counts, with candidates from `ENGINE-SPATIAL-QUERIES` (see the batch summary).
7. **Tests are scenarios.** Rows 1–14 run the production simulation; row 4 tests the node's contract through its declared interface.

## Appendix: Historical evidence

Evidence of the legacy part at `a6c914e`, kept so the harvest stays traceable. Nothing here is a requirement, a tolerance or a design instruction; sections 1–9 are the spec. Legacy tick counts and bit-exact boundaries in the cited files are history only.

| # | Fact | Citation |
| --- | --- | --- |
| 1 | Legacy ports, threshold, hysteresis, contact route, powered rising-edge trigger and scalar output lived in a per-part update (behaviour restated in section 2) | `parts/SoundMeterPart.cs@a6c914e:L11-L83` |
| 2 | Reception: the strongest single direct arrival; opaque colliders block; no self-hearing (EL-044 L4, L6) | `engine/Acoustics.cs@a6c914e:L49-L80`; `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`; `reference/cpu/MachineWorld.cs@a6c914e:L855-L861` |
| 3 | Speaker to supplied meter with a gate and a counter: one trigger per pulse; Wall, reversed speaker and unsupplied controls; Reset | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L14-L54` |
| 4 | Hysteresis levels at threshold 0.25; invalid levels and thresholds refused (row 4) | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L55-L71` |
| 5 | Two equal sources read as one; a hidden meter read 0 (row 5) | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L72-L95` |
| 6 | The contact closed at the threshold and opened at 90 % of it | `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L113-L130` |
| 7 | The needle followed the committed level and the lamp lit only when supplied and above; pause, hide, Reset and load restored them | `CuriousContraptions.tests/SoundMeterAnimationTests.cs@a6c914e:L33-L97` |
| 8 | Invalid gauge bindings were refused before Run | `CuriousContraptions.tests/SoundMeterAnimationTests.cs@a6c914e:L98-L166` |
| 9 | Reception used the solved pose, not the rendered transform | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L322-L354` |
| 10 | Chains: bell, meter, hold timer, powered gate; wind chimes, meter, counter (rows 7, 8) | `CuriousContraptions.tests/BellTests.cs@a6c914e:L60-L102`; `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L46-L98` |
| 11 | A failed arrival restored hysteresis: a faulting-tick mechanism, not carried | `CuriousContraptions.tests/SoundMeterCheckpointTests.cs@a6c914e:L25-L76` |
| 12 | No level used the sound meter | `docs/coverage/catalogue-elements.json` (empty fixture list) |
| 13 | Legacy geometry, art and catalogue entry | `parts/SoundMeterPart.cs@a6c914e:L84-L100`; `parts/catalog/sound_meter.tres@a6c914e:L5-L12` |

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
