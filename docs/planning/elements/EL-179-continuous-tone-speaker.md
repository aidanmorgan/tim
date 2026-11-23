# EL-179 · Continuous-tone speaker — element readiness spec

**Current delivery (24 October 2026):** [EL-179 delivery slice](../invest/named-elements.md#element-179). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against checkout `8d1d68aa`, and repaired on 22 November 2026 against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-h.md`). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the current checkout. Values marked *proposed* are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions). Box sizes are full extents. Shared acoustic facts A1–A13, laws L1–L7, the capability probe (C1–C13, N1–N13) and tolerances are in [EL-044](EL-044-tone-selective-sound-meter.md); speaker facts K1–K10 in [EL-180](EL-180-pulse-speaker.md#speaker-facts-shared-with-el-179); the speaker body, art and port positions in [CAT-061](CAT-061-speaker.md#2-declaration), the single Speaker declaration (coordinator decision 27, 23 November 2026). Re-pointed on 23 November 2026 from EL-180's former declaration, which is withdrawn.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-179 · Continuous-tone speaker |
| Type | Sound |
| Anchor | [requirements.md#element-179](../requirements.md#element-179); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Speaker/pulse/continuous modes"); [named-elements entry](../invest/named-elements.md#element-179); owner S531 |
| Disposition | **Variant, not a duplicate; to deliver as its own catalogue entry.** Deltas over [CAT-061 Speaker](CAT-061-speaker.md): no `ActivationIn`; a sustained emission instead of pulses; a continuous power draw instead of a per-pulse debit; parameters `enabled` and `power`. The element map says "pulse and continuous speakers stay distinct". It forms the **Speakers** family with CAT-061 (same cabinet, mouth, tones and cone). It is planned as its own data-only slice after EL-044 (sound order: 14.0, 14.2, 14.1, 14.3 with EL-180, EL-044, then EL-179); it may instead share Story 14.3's slice as a Family slice only if that slice's probe finds N3 and the watt-paid emitter of N5 delivered. |
| Planned package | id `tone_speaker`, directory `elements/ToneSpeaker/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |
| Related | Supply [CAT-005](CAT-005-battery.md)/[EL-196](EL-196-battery.md), switched by [CAT-063](CAT-063-switch.md) (which holds EL-198's declaration); listeners [CAT-060](CAT-060-sound_meter.md), [EL-044](EL-044-tone-selective-sound-meter.md), [EL-049 Resonator](EL-049-acoustic-resonator.md). Note: delivery-batches section 2 does not list EL-179 among the 44 refining identities although the element index says it refines CAT-061; recorded in the batch summary. |
| Roadmap story | [EL-179 delivery slice](../invest/named-elements.md#element-179) `named-el-179`, after Story 14.3. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it) and `ENGINE-PARAMETER-BOUND-FRAMES` (tone bars); then Stories 14.0, 14.2, 14.1 and 14.3 and EL-044. |

## 2. Declaration

Closest delivered shape: the Fan in its Electrical supply mode, a consumer that works for exactly as long as it is supplied (`elements/Fan/FanElementDeclaration.cs:54`, `:72`–`:76`).

**Interfaces.** `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the acoustic emitter interface Story 14.0 adds. It is not an `ISignalElement`: it has no signal port or node.

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and colliders | The speaker body of [CAT-061](CAT-061-speaker.md#2-declaration): cabinet box 1.25 × 1.5 × 1.25 m at the origin, foot box 1.6 × 0.18 × 1.55 m at (0, −0.84, 0), seated so its top (−0.84 + 0.09 = −0.75) meets the cabinet's bottom, one socket sphere r 0.075 m; plus one ridge box 0.9 × 0.06 × 0.1 m on the top face at y = 0.78 *proposed*, with the sine wave printed flat on it. |
| Mass and material | Static (mass 0). Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. |
| Constraints | none. |
| Emitter | One emitter at the mouth (0.72, 0, 0), axis +X: kind **Sustained** (L1), pattern Cone 35°, strength 1, tone = `tone`, speed 12 m/s, range 8 m. It is Sounding in a tick exactly when the consumer was paid in full in the previous tick's commit and `enabled` = Enabled. |
| Stores | none. Each supply phase the consumer requests `power` × dt (`ENGINE-SUPPLY-DEMAND`, coordinator decision 30; today's code requests `power` × 1/120 s). It sounds in a tick when it was powered in full in the previous tick's commit: the acoustic phase runs at tick start and reads tick n − 1, like the optical one-tick latency (coordinator decision 34). A smaller grant is silent and takes nothing (Story 14.0, gap 4), so the ledger never pays for silence. If the source cannot pay, the emission stops in the tick after that commit. |
| Supply graph | One `SupplyConsumerDescriptor` on `PowerIn` whose target is the emitter's continuous draw (N5). Electrical node role `Consumer`. |
| Footprint | 1 body, 0 moving bodies, 0 constraints. |

**Parameters**

| Name | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `tone` | Choice | None | 1 (Mid) | 0 | 2 | Reject; `Low`, `Mid`, `High` |
| `enabled` | Choice | None | 1 (Enabled) | 0 | 1 | Reject; BuildOnly; `Disabled`, `Enabled`. An author-set mute; unlike the Battery's `enabled` it is not a Run control, because a switch on the supply is the in-Run control. |
| `power` | Real f32 | Watt | 10 | 1 | 40 | Clamp; BuildOnly |

**Ports**

| `PortId` | Role | Domain | Direction | Position (m) |
| --- | --- | --- | --- | --- |
| `PowerIn` | `PortRole.PowerIn` | Electrical | Input | (−0.625, 0, 0), the socket sphere seated in the cabinet's left face as on [CAT-061](CAT-061-speaker.md#2-declaration) (the legacy K1 position is not carried forward) |

**Presentation bindings.** Diaphragm `Visual/Diaphragm` local X oscillates continuously while Sounding and rests when Silent ← committed emitter state (new committed driver, N12); rings stream from the mouth while Sounding; the sine glyph `Visual/Glyph` albedo slate → gold ← `PresentationDriver.CommittedSupply` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`). Audio voice is presentation-only.

**Palette.** Category `ElementCategory.Control`; palette order 428 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); title "Tone speaker"; colour cream `#fff8e9`. Description: "Holds a steady tone for as long as it has electricity. Cut the power and it stops at once."

**Variants.** `tone` Low, Mid, High (one, two or three bars; 220, 440, 880 Hz, A11).

### Art and solidity

The nodes and classes of the [CAT-061 table](CAT-061-speaker.md#3-art-and-solidity), without the top activation socket and without the `Ready` lamp (this part has no pulse store for it to show), plus:

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Ridge` | one box 0.9 × 0.06 × 0.1 m on the top face, cream | solid, one box collider |
| `Glyph` | a sine wave printed flat on the ridge, slate, gold while supplied | decorative: printed flat, colour by a binding |

Icon: the cabinet with three parallel rings leaving it. **At a glance** it differs from the Speaker by the raised ridge on top and by having no top socket. Its tone bars are CAT-061's `ToneBars` nodes, one shown by `tone` (needs EL-044 C13).

### Behaviour and numbers

- **Laws.** L1 (Sustained), L2–L4, L6, L7. A receiver at distance d hears the tone from start + d/12 s until stop + d/12 s. A meter triggers once on the rising crossing ([EL-044](EL-044-tone-selective-sound-meter.md) row 5).
- **Power loss.** The emission stops in the tick after the commit in which the consumer is unpaid. Sound already travelling arrives; nothing new is made. A downstream resonator then decays by its own time constant only (requirement outcome): the speaker fabricates no tail.
- **`power` 10 W default** *proposed*. A default Battery (3 600 J, `elements/Battery/BatteryElementDeclaration.cs:42`) sustains it for 360 s, longer than the 300 s maximum Run (owner decision 17), so it never runs out in a level; the smallest Battery (60 J) for 6 s, a usable "it runs out" puzzle. The 10 W default equals the Battery's minimum power limit (`:44`); a `power` above the Battery's `power_limit` setting leaves the speaker Silent (a partial grant takes nothing), which row 5 uses; likewise two tone speakers whose requests together exceed the limit are both granted a share, so both are silent until one is removed or the limit is raised. The 40 W maximum stays below the 120 W default limit, so one default Battery runs a speaker and a Fan together.
- **Tick order.** The acoustic phase runs at tick start and reads the supply committed in tick n − 1 (coordinator decision 34). A contact opened by a signal passes no supply from the next electrical solve ([EL-044](EL-044-tone-selective-sound-meter.md) L7), and the tone stops in the tick after that solve's commit.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S531): AcousticPropagation, ElectricalPower, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction). Numbered rows are the shared probe in [EL-044 section 3](EL-044-tone-selective-sound-meter.md#shared-capability-probe).

| Capability this identity needs | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body and colliders (C1) | present | EL-044 C1 |
| Finite source paying a continuous consumer (C3) | present | EL-044 C3; continuous payment from a finite store is also exercised by `AirflowRuntimeTests.DifferentFlowSpeedsShareOneFiniteStoreInJoules` (`CuriousContraptions.tests/AirflowRuntimeTests.cs:82`) |
| A consumer target that is an acoustic emitter | narrower | `SupplyContinuousWorkTarget` exists but names an airflow source only (`CuriousContraptions.Abstractions/SupplyContractValues.cs:50`); owned by N5 |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| Cone emission, propagation, occlusion, tone (N1, N2) | missing | Story 14.0, with reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` |
| Sustained emission (N3) | missing | Story 14.0, gap 2 |
| Watt-paid emitter with the full-grant rule (N5) | missing | Story 14.0, gap 4 |
| Art visibility chosen by a `Choice` (tone bars; C13) | narrower | EL-044 C13; owned by `ENGINE-PARAMETER-BOUND-FRAMES` (choice-bound values, coordinator decision 16) |
| Committed emitter state for the diaphragm; presentation-only audio (N12) | missing | Story 14.0, gap 11 |

Counts: 3 present, 0 untested, 2 narrower, 4 missing. Nothing here needs element-specific engine code.

**Dependencies.** The engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`, `ENGINE-PARAMETER-BOUND-FRAMES`); Story 14.0; Battery (delivered); a supply switch (Impact switch delivered; the author-set contact of [CAT-063](CAT-063-switch.md)) to show power loss; a Sound meter or resonator.

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 and the documents named in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026).

1. **Declaration data only.** One sustained emitter that is a continuous-work supply target, one electrical consumer node and one port. "Stops when unpowered" is the supply network's grant for that tick, not a handler; no engine code may branch on `tone_speaker`.
2. **Generic laws.** Proportional finite allocation (delivered, C3) with the full-grant rule for a continuous-work emitter (N5); acoustic laws L1 (Sustained), L2–L4 and L6. `tone`, `enabled` and `power` are the only values.
3. **Three layers.** *Colliders:* cabinet box, foot box, ridge box and one socket sphere on one static body, static default material. *Visuals:* CAT-061's art table (less the top socket and `Ready` lamp) plus the two rows above; no art mesh is a collider. *Animation:* read-only bindings. Moved by a binding: `Diaphragm` with its child `Centre` (oscillation), `Glyph` (colour), `ToneBars` nodes (visibility); the rings are the engine's cue. Moved by nothing: `Cabinet`, `Foot`, `Ridge`, `Rim`, port sphere.
4. **One-way flow and typing.** Supply grant → emitter → receivers → presentation; nothing returns. `tone` is `ToneBand`, `enabled` a closed Choice, `power` f32 watts.
5. **Flat state.** 1 static body; 4 colliders (3 box, 1 sphere); 0 joints; 0 drives; 0 sensors; domain nodes: 1 acoustic emitter, 1 electrical node, 0 signal nodes; 0 stores; 8 presentation bindings (diaphragm, rings, three glyph colour channels, three tone-bar visibilities).
6. **Performance fitness.** 4 colliders, the same as the Speaker ([CAT-061](CAT-061-speaker.md#3-art-and-solidity): cabinet, foot and two sockets) and against the Fan's 2 (`elements/Fan/fan.tscn`). The first draft's three-box zigzag glyph is replaced by one ridge box with the wave printed on it: three colliders whose only job was a cosmetic outline are not worth their cost. Static body; it joins no island. A held tone is one occurrence for its whole life. Occurrences follow the D5 bounds in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026) point 6 (tables sized at Run from declarations, no scene ceiling; reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES`).
7. **Scenarios.** Rows 1–9 run the real simulation; none depends on a code path that names this package.

## 4. Sources and legacy

- Requirement row [element-179](../requirements.md#element-179): "Finite supplied power sustains a declared acoustic tone while enabled"; outcome "Power loss stops excitation; stored resonance may decay only by its own state".
- Named entry [element-179](../invest/named-elements.md#element-179), owner S531; map composition "Continuous finite supplied acoustic power and emission lifetime differ from pulse occurrence admission"; component research "Continuous-tone speaker (EL-179) | Electrical node + sustained acoustic source | band, strength | Holds a tone while powered and enabled".
- Legacy: only the pulse speaker exists (K1–K10); no continuous mode. Reused: cabinet, port, tone, art and diaphragm facts.
- **Files harvested:** `parts/SpeakerPart.cs`; acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-179](../requirements.md#element-179), [acoustic profile](../invest/profiles.md#acoustic). Planned test class `ToneSpeakerTests`. Tolerances and stall guard: [EL-044 shared tolerances](EL-044-tone-selective-sound-meter.md#shared-tolerances-and-stall-guards).

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | Given Battery → Tone speaker and a supplied Sound meter 3 m in front, when Run starts, then the meter reads 0.581 within T-level, triggers exactly once and stays closed; the Battery falls by `power` × elapsed time within T-energy over 5 s. | `ToneSpeakerTests.ASuppliedSpeakerHoldsOnePaidTone` | no supply link: level 0, no debit | each tone against a matching [EL-044](EL-044-tone-selective-sound-meter.md) meter |
| 2 | Given the supply opened mid-run by a switch contact, then no new sound is made from that tick, the meter falls to 0 within the stall guard, and a Resonator in front decays with its own `decay_time` (its excitation after one further `decay_time` is e⁻¹ of its value when the sound stopped, within T-level): not faster, not slower (requirement outcome). | `ToneSpeakerTests.PowerLossStopsExcitationAndTheResonatorDecaysByItself` | supply kept: the resonator stays at its cap | — |
| 3 | Given `enabled` = Disabled with supply present, then it is Silent and draws nothing. | `ToneSpeakerTests.ADisabledSpeakerIsSilentAndFree` | row 1 | — |
| 4 | Given a 60 J Battery and `power` 10 W, then it sounds for 6 s within 0.3 s and is Silent afterwards with the balance at 0. | `ToneSpeakerTests.ItSoundsOnlyAsLongAsTheBatteryPays` | 3 600 J Battery: still sounding at 10 s | `power` 1 W and 40 W |
| 5 | Given `power` 40 W and a Battery whose `power_limit` is 10 W, then it is Silent and the balance is unchanged (a partial grant takes nothing). | `ToneSpeakerTests.AnUnaffordableDrawIsSilent` | limit 120 W: sounding | limit exactly 40 W: sounding; two 40 W speakers on a 60 W limit: both silent |
| 6 | Reset silences it and restores the Battery; save then load round-trips `tone`, `enabled`, `power` and the connection; muted and audible runs are identical. | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` plus `ToneSpeakerTests.ResetSilencesAndRestoresTheLedger` | — | Reset while sounding |
| 7 | `power` below 1, above 40 or non-finite, or a Choice outside its range, is refused atomically with a typed reason; a Signal output linked to `PowerIn` is refused. | `ToneSpeakerTests.OutOfRangeParametersAreRefused`; generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` | limits accepted | first f32 beyond each limit |
| 8 | Given the speaker facing away from the meter, a Wall between them, or the meter 8.5 m away, then the meter reads 0 (the sustained emitter obeys the same cone, range and occlusion as a pulse). | `ToneSpeakerTests.DirectionRangeAndOcclusionSilenceTheMeter` | row 1 | meter 33° and 37° off the axis; meter at 7.9 m (0.167 within T-level, threshold 0.05) and 8.1 m (0) |
| 9 | ([EL-044](EL-044-tone-selective-sound-meter.md) row 5, run in this slice.) Given a matching tuned meter, then the held tone gives exactly one pulse and the contact opens when the level falls to 0.9 × threshold. | `TunedSoundMeterTests.ASustainedToneTriggersOnceAndReleasesWithHysteresis` | pulse source: one pulse per pulse | level placed just above and just below threshold by distance |

**Chrome recipe (pending, post-catalogue).** Battery → Switch contact → Tone speaker `PowerIn`; supplied Sound meter and a Resonator in front.

## 6. Open questions

**Resolved.**

1. **Control input.** Supply-only, no `ActivationIn`: [D19](../invest/delivery-batches.md#3-family-level-decisions) (no requirement row or lesson uses an activation enable; a switch on the supply does the job).
2. **Sustained representation.** One sustained occurrence: [D17](../invest/delivery-batches.md#3-family-level-decisions).
3. **Mode or element.** Its own entry `tone_speaker`: [D8](../invest/delivery-batches.md#3-family-level-decisions); it is not a duplicate, and its port set differs from the Speaker's.
4. **Coordinator decisions, 22 November 2026 (owner's standing delegation), applied in the audit repair.** (a) `enabled` is BuildOnly. (b) A grant below the declared power is silent and takes nothing: the alternative, a quieter tone for a partial grant, would need a strength-from-grant law no lesson uses. (c) Row 8 proves cone, range and occlusion for the sustained emitter instead of leaning on the Speaker slice. (d) The glyph is one ridge box with a printed wave (engine conformance, point 6). (e) Slice order: after EL-044, whose row 5 runs here as row 9. (f) Coverage is bound by the published section form and is blocked on S7 (section 7). (g) Palette order 428 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

**Needs the owner.** None.

## 7. Integration

- **Works with:** Battery, Impact switch, Wall (delivered); Sound meter CAT-060, EL-044, EL-049, EL-046, EL-050, EL-051 (planned). The Resonator row (2) runs in whichever of the two slices lands second and is stated in both specs.
- **Campaign:** chapter 8, levels 71–80, first taught at **76a**, after EL-044 in the same level (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); reuse 81–100, 117–125, 136–150. CAT-061 retained behaviour [todo-345](../requirements.md#todo-345); [sequence-task-407](../requirements.md#sequence-task-407); processes [IX-06](../requirements.md#interaction-06), [IX-12](../requirements.md#interaction-12).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. This identity lacks: (1) any `todo-NNN` link in its ledger entry (the reader requires exactly one); (2) a `sequence-task-NNN` co-anchored with that todo in `requirements.md`; (3) a decision anchor, since its ledger owner S531 has no `<a id="s531">` anchor in `docs/planning/invest/scope-corrections.md`. The delivering slice adds the section once S7 supplies all three; it is not written earlier, because the reader throws on a malformed block. Mode records: `Tone=Low`, `Tone=Mid`, `Tone=High`.
- **Lesson fixtures (S8):** needed only where a level pre-places it.

## 8. Delivery

- **Slice shape.** A single data-only slice after EL-044 (or a Family slice "Speakers" with CAT-061 in Story 14.3 if that slice's probe finds N3 and N5 delivered).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it) and `ENGINE-PARAMETER-BOUND-FRAMES` (tone bars); then Story 14.0 amended with gaps 2, 4 and 11; Stories 14.2, 14.1, 14.3 (with EL-180); EL-044; S7. Row 2 additionally needs [EL-049](EL-049-acoustic-resonator.md), planned after it, and runs in that slice.
- **Files.** Adds `elements/ToneSpeaker/` (`ToneSpeaker.csproj`, `ToneSpeakerElementDeclaration.cs`, `tone_speaker.tscn`, `icon.svg`) and `CuriousContraptions.tests/ToneSpeakerTests.cs`; registration is derived. The test file needs no `Compile` line (main compiles every top-level `*.cs` of the test project by one rule). Until S3 lands: `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596`, `:653`. This spec gains its binding section when S7 allows (section 7).
- **Review tier.** Data-only.
- **Disqualified if** the probe finds no sustained emission, no watt-paid emitter target, or that the power-loss stop needs a per-package rule.
