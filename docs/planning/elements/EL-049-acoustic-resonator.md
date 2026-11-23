# EL-049 · Acoustic resonator — element readiness spec

**Current delivery (24 October 2026):** [EL-049 delivery slice](../invest/named-elements.md#element-049). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against checkout `8d1d68aa`, and repaired on 22 November 2026 against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-h.md`). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the current checkout. Values marked *proposed* are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions). Box sizes are full extents. Shared acoustic facts A1–A13, laws L1–L7, the capability probe (C1–C13, N1–N13) and tolerances are in [EL-044](EL-044-tone-selective-sound-meter.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-049 · Acoustic resonator |
| Type | Sound |
| Anchor | [requirements.md#element-049](../requirements.md#element-049); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-049); owner S536 |
| Disposition | **To deliver as a single element.** It refines no CAT part. It is not a variant of the tuned meter ([EL-044](EL-044-tone-selective-sound-meter.md)): it has a store (excitation) that builds and decays, which the meter does not. One entry with a `band` setting ([D8](../invest/delivery-batches.md#3-family-level-decisions)). |
| Planned package | id `acoustic_resonator`, directory `elements/AcousticResonator/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |
| Related | Receiver pattern of [CAT-060 Sound meter](CAT-060-sound_meter.md) and EL-044 (band, supplied contact), with a stored excitation. [EL-052 Water-tuned bottle](EL-052-water-tuned-bottle.md) is a different part (a flow-excited emitter whose tone follows its fill); it does not reuse this store. |
| Roadmap story | [EL-049 delivery slice](../invest/named-elements.md#element-049) `named-el-049`. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it), the threshold row of `ENGINE-SIGNAL-NODES` and `ENGINE-PARAMETER-BOUND-FRAMES` (band bars); then Stories 14.0, 14.2, 14.1 and 14.3. |

## 2. Declaration

Closest delivered shape: the Impact switch (static body, a signal node that closes a supplied contact and emits a pulse; `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:12`, `:40`–`:42`, `:71`–`:83`).

**Interfaces.** `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the accumulating acoustic receiver interface Story 14.0 adds (N10).

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and colliders | One static body. Jar: cylinder r 0.4 m, height 1.0 m, axis Y, centred at (0, 0.1, 0) *proposed* (about half a Speaker cabinet); neck ring: Tube r 0.25–0.3 m, height 0.1 m at y = 0.65; foot box 0.9 × 0.14 × 0.9 m at (0, −0.47, 0); three socket spheres r 0.075 m. |
| Mass and material | Static (mass 0). Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. |
| Constraints | none. |
| Receiver | One accumulating receiver at the jar mouth, local (0, 0.7, 0), omnidirectional, band mask = `band`; occlusion applies (L6), its own colliders exempt. |
| Stores | Excitation E ∈ [0, 1], dimensionless, initial 0. It only builds from in-band sound and decays; it never supplies electricity and never re-radiates sound. |
| Threshold node | The threshold row of `ENGINE-SIGNAL-NODES` over the committed E: on at E ≥ `threshold`, off at E ≤ 0.9 × `threshold`. A rising crossing emits one `ActivationOut` pulse only if `PowerIn` is supplied on that tick; no replay. |
| Supply graph | One `SupplyContactDescriptor` closed while the threshold state is on; `PowerIn` → contact → `Supply`. Electrical node role `Switch`. |
| Footprint | 1 body, 0 moving bodies, 0 constraints. |

**Parameters**

| Name | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `band` | Choice | None | 1 (Mid) | 0 | 2 | Reject; `Low`, `Mid`, `High` |
| `gain` | Real f32 | None | 0.4 | 0.1 | 1 | Clamp; BuildOnly |
| `decay_time` | Real f32 | Second | 1.5 | 0.2 | 5 | Clamp; BuildOnly |
| `threshold` | Real f32 | None | 0.6 | 0.05 | 1 | Clamp; BuildOnly |

**Ports**

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | `PortRole.PowerIn` | Electrical | Input | — | (−0.4, −0.3, 0) |
| `Supply` | `PortRole.Supply` | Electrical | Output | — | (0.4, −0.3, 0) |
| `ActivationOut` | `PortRole.ActivationOut` | Signal | Output | Pulse | (0.4, 0.35, 0) |

The Sound meter's port set and left/right layout (`parts/SoundMeterPart.cs@a6c914e:L50-L55`), scaled to this body, with each socket sphere seated in the jar wall (r 0.4 m) as CAT-060 seats its sockets; the earlier x ±0.5 left the spheres 0.1 m clear of the jar.

**Presentation bindings.** Membrane `Visual/Membrane` emission strength and a cyan inner glow ← committed E (new scalar driver, N10), 0 at E = 0 to full at E = 1; lamp `Visual/Lamp` slate → gold, SmoothStep 0.1 s ← `PresentationDriver.CommittedSupply` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`).

**Palette.** Category `ElementCategory.Control`; palette order 431 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); title "Resonator jar"; colour cream `#fff8e9`. Description: "Builds up when sounds of its own marks arrive and slowly dies away. Supply electricity to trigger once it rings strongly enough."

**Variants** (campaign coverage: "each tuned band"): `band` Low, Mid or High builds only from arrivals of that tone.

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Jar` | cylinder r 0.4 × 1.0 m, cream `#fff8e9` | solid, cylinder collider (the cavity is not modelled; nothing can enter) |
| `Neck` | ring r 0.3 m, height 0.1 m, gold `#e8b764` | hollow, Tube collider |
| `Membrane` | disc inside the neck ring, cyan `#66b8c9` glow | decorative: enclosed, glow by a binding |
| `BandBars` | three flush nodes of one, two and three navy bars on the jar; one is visible, chosen by `band` (needs EL-044 C13) | decorative: printed flat |
| `Lamp` | disc flush in the jar wall | decorative: printed flat |
| `Foot` | box 0.9 × 0.14 × 0.9 m, navy `#293954` | solid, box collider |
| Port spheres ×3 | r 0.075 m, gold `#f7cb52` | solid, sphere colliders |

Icon: a jar with a rising bar inside it. **At a glance** it differs from the tuned meter (a flat cabinet with a dial) by being a round jar with a glowing top.

### Behaviour and numbers

**Law.** With L_b(t) the receiver's in-band level (L3–L6) and τ = 0.15 s the pulse duration (A2):

  dE/dt = `gain` · L_b / τ − E / `decay_time`, with E clamped to [0, 1].

One rule covers pulses and sustained tones: a 0.15 s pulse at level L adds about `gain` · L, and a held tone keeps adding at the same rate. Off-band sound has L_b = 0 and adds nothing (requirement outcome). The strongest in-band arrival is used, never a sum (L4). This replaces the earlier per-occurrence increment, which needed an occurrence identity and had no meaning for a sustained tone ([D17](../invest/delivery-batches.md#3-family-level-decisions)).

- **`gain` 0.4, `decay_time` 1.5 s, `threshold` 0.6.** A Speaker 2 m away arrives at 0.758. Integrating the law over one 0.15 s pulse gives E ← 0.905·E + 0.289, and the 0.35 s gap multiplies E by e^(−0.35/1.5) = 0.792. With pulses 0.5 s apart E is 0.289, 0.495 and 0.643 just after the first three: **three pulses cross 0.6, two do not**, which is the "builds visibly" lesson. If the pulses continue, E falls to 0.509 before the fourth, below the 0.54 off level, so the contact opens and the fourth pulse (E = 0.750) closes it again with a second `ActivationOut` pulse; from then on E stays above 0.54 between pulses (0.594 before the fifth) and the contact holds. A lesson that wants exactly one trigger stops the source after the third pulse. Right at the mouth (level 1) two pulses already reach about 0.65.
- **Held tone.** The same source held on crosses the threshold in about 0.33 s, reaches the cap in about 0.6 s, and the contact stays closed; when the tone stops E falls to 0.54 (0.9 × threshold) after 1.5 · ln(1/0.54) = 0.92 s and the contact opens. So a pulse train needs rhythm and a tone does not, a second lesson.
- **`decay_time` 1.5 s** rings down over a second or two, visible but not tedious; 0.2 s makes it nearly a plain meter, 5 s makes slow rhythms accumulate.
- **Tick order.** E is updated in the Phase 2 acoustic pass; its threshold node is sampled and its pulse routed as EL-044 L7 states (same-tick routing; the contact conducts from the next electrical solve).
- **Consistency.** Clock intervals of 0.5 s are within the System clock's range (its minimum is 0.1 s); the contact adds no limit, so `Supply` carries a default Motor or Fan load.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S536): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction). Numbered rows are the shared probe in [EL-044 section 3](EL-044-tone-selective-sound-meter.md#shared-capability-probe).

| Capability this identity needs | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with cylinder and tube colliders (C1, C10) | present | EL-044 C1, C10 |
| Supplied contact closed by a signal node (C2) | narrower | only `Latched` and `TimerCounting` states are observable; a contact that follows a threshold and re-opens is owned by N4 (the threshold row of `ENGINE-SIGNAL-NODES`) |
| Finite source feeding the contact (C3); pulse output (C4) | present | EL-044 C3, C4 |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| Emission, propagation, occlusion, band mask (N1, N2), sustained arrivals (N3) | missing | Story 14.0, with reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` |
| Threshold node with hysteresis driving a pulse and a contact (N4) | missing | the threshold row of `ENGINE-SIGNAL-NODES` (coordinator decision 16), reading the committed excitation of N10 |
| Accumulating receiver store with gain and decay (N10) | missing | Story 14.0, gap 9 |
| Committed scalar driver for the glow (N10) | missing | Story 14.0, gap 9 (the committed excitation as an observation) |
| Art visibility chosen by a `Choice` (band bars; C13) | narrower | EL-044 C13; owned by `ENGINE-PARAMETER-BOUND-FRAMES` (choice-bound values, coordinator decision 16) |

Counts: 4 present, 0 untested, 2 narrower, 4 missing. Nothing here needs element-specific engine code.

**Dependencies.** The engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`, the threshold row of `ENGINE-SIGNAL-NODES`, `ENGINE-PARAMETER-BOUND-FRAMES`); Story 14.0; a toned source (Speaker CAT-061/EL-180, EL-179, Bell CAT-009); System clock and Battery (delivered).

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 and the documents named in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026).

1. **Declaration data only.** One accumulating receiver (band mask, gain, decay time), one threshold signal node, one supply contact, three ports. The excitation law is the engine's rule for every accumulating receiver; the package has no update loop and no engine code may branch on `acoustic_resonator`.
2. **Generic laws.** Reception L2–L6; the accumulating-receiver rate law dE/dt = gain·L/τ − E/decay (N10); the threshold node with declared on and off levels (N4, the threshold row of `ENGINE-SIGNAL-NODES`); the supply contact rule (C2). The four parameters are the only values.
3. **Three layers.** *Colliders:* jar cylinder, neck Tube, foot box and three socket spheres on one static body, static default material. *Visuals:* the art table above; no art mesh is a collider. *Animation:* read-only bindings. Moved by a binding: `Membrane` (glow), `Lamp` (colour), `BandBars` nodes (visibility). Moved by nothing: `Jar`, `Neck`, `Foot`, port spheres.
4. **One-way flow and typing.** Committed band level → excitation → signal node → presentation; nothing returns. `band` is `ToneBand`; values are f32.
5. **Flat state.** 1 static body; 6 colliders (1 cylinder, 1 tube, 1 box, 3 sphere); 0 joints; 0 drives; 0 sensors; domain nodes: 1 accumulating acoustic receiver, 1 threshold signal node, 1 electrical node with 1 contact; 0 stores (the excitation value is receiver state, not an energy store); 8 presentation bindings (membrane emission and glow, three lamp colour channels, three bar visibilities).
6. **Performance fitness.** 6 colliders against the Impact switch's 5 (`elements/ImpactSwitch/switch.tscn`): the extra one is the neck ring, a proud solid. Static body; it joins no island. One receiver point; the excitation update is one multiply-add per receiver per tick. Arrivals follow the D5 bounds in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026) point 6 (tables sized at Run from declarations, no scene ceiling; reception candidates from `ENGINE-SPATIAL-QUERIES`).
7. **Scenarios.** Rows 1–8 run the real simulation through the production edit interface; none depends on a code path that names this package.

## 4. Sources and legacy

- Requirement row [element-049](../requirements.md#element-049): "Stores and dissipates bounded acoustic excitation around a declared band"; outcome "Off-band pulses do not accumulate the same response". Scope index [todo-352](../requirements.md#todo-352): "a resonator visibly builds/loses excitation".
- Named entry [element-049](../invest/named-elements.md#element-049), owner S536; component research: "Resonator | Excitation level, matching band, decay | Builds under a matching tone, decays otherwise; passive resonance supplies no electricity" and "Acoustic resonator (EL-049) | Resonator + contact output | band, threshold" ([component research](../../component-research.md#sound)); [acoustic profile](../invest/profiles.md#acoustic): "a loop does not amplify".
- Legacy: no resonator or accumulating receiver. Facts reused: pulse model and strongest arrival (A1–A13), meter contact and hysteresis (`parts/SoundMeterPart.cs@a6c914e:L50-L83`).
- **Files harvested:** `parts/SoundMeterPart.cs`, `parts/SpeakerPart.cs` (tone marks); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-049](../requirements.md#element-049), [acoustic profile](../invest/profiles.md#acoustic). Planned test class `AcousticResonatorTests`. Excitation tolerance: ±0.02 (the smallest designed margins are 0.043, between the third-pulse value 0.643 and the 0.6 threshold, and 0.031, between 0.509 and the 0.54 off level); otherwise the [EL-044 shared tolerances](EL-044-tone-selective-sound-meter.md#shared-tolerances-and-stall-guards) and stall guard.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | Given Battery → Speaker, a Clock at 0.5 s fed by its own Battery, a supplied Mid resonator 2 m in front and `ActivationOut` → Signal lamp, when the speaker pulses in the Mid tone and the Clock's Battery is disabled with its Run control after the third pulse, then E is 0.289, 0.495 and 0.643 just after the three pulses within tolerance, the lamp is off after the second pulse and on after the third, and exactly one `ActivationOut` pulse is emitted. | `AcousticResonatorTests.MatchingPulsesBuildUntilTheThirdCrossesTheThreshold` | clock stopped after two pulses: never on | each band with its matching tone; clock left running: E dips to 0.509, the contact opens, and the fourth pulse gives a second `ActivationOut` pulse |
| 2 | Given the same pulse train in another tone, then E stays exactly 0 (requirement outcome). | `AcousticResonatorTests.OffBandPulsesDoNotAccumulate` | row 1 | all six off-band pairs |
| 3 | Given the clock stopped after the lamp lights, then E decays and the contact opens when E reaches 0.54; after one further `decay_time` E is e⁻¹ of its earlier value within tolerance. | `AcousticResonatorTests.ExcitationDecaysByItsOwnTimeConstant` | — | `decay_time` 0.2 s and 5 s |
| 4 | Given a held matching tone ([EL-179](EL-179-continuous-tone-speaker.md)), then E reaches 1, never exceeds it, and the contact stays closed until the tone stops. | `AcousticResonatorTests.AHeldToneFillsToTheCapAndNoFurther` | off-band held tone: 0 | `gain` 0.1 and 1 |
| 5 | Given no supply at `PowerIn`, then E builds and the glow follows it but no pulse is emitted and `Supply` stays dead; connecting supply later replays nothing. | `AcousticResonatorTests.WithoutSupplyItRingsButDoesNotTrigger` | row 1 | — |
| 6 | Given a Wall between speaker and jar, then E stays 0. | `AcousticResonatorTests.AWallPreventsAnyBuild` | row 1 | — |
| 7 | Reset sets E to 0 and opens the contact; save then load round-trips all four parameters and the connections; E is not saved (a loaded Run starts at 0). | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` plus `AcousticResonatorTests.ResetEmptiesTheStore` | — | Reset at E = 1 |
| 8 | Each Real parameter below its minimum, above its maximum or non-finite, and `band` outside 0–2, is refused atomically with a typed reason; wrong-domain links are refused. | `AcousticResonatorTests.OutOfRangeParametersAreRefused`; generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` | limits accepted | first f32 beyond each limit |

**Chrome recipe (pending, post-catalogue).** Battery → Speaker; Clock (CAT-017) → Speaker activation at 0.5 s; Resonator 2 m in front with `band` chosen through its configuration control; Battery → resonator `PowerIn`; `ActivationOut` → Signal lamp.

## 6. Open questions

**Resolved.**

1. **Re-radiation.** None; the resonator never emits: [D19](../invest/delivery-batches.md#3-family-level-decisions). It keeps the part a pure store and makes amplifying horn loops impossible by construction.
2. **Off-band response.** Exactly zero: D19; the same discrete rule as EL-044, so the requirement outcome holds at every setting.
3. **Simultaneous arrivals.** Strongest in-band arrival only: EL-044 L4.
4. **Build law.** The rate law above instead of a per-occurrence step: decided under the standing delegation so pulses and sustained tones share one rule (D17).
5. **Coordinator decisions, 22 November 2026 (owner's standing delegation), applied in the audit repair.** (a) Row 1 stops the clock after the third pulse and the arithmetic is corrected (0.289, 0.495, 0.643; a running clock re-triggers at the fourth pulse): the law is unchanged, the earlier row was false for a running clock. (b) Tick order follows EL-044 L7. (c) Band bars need C13. (d) Coverage is bound by the published section form and is blocked on S7 (section 7). (e) Palette order 431 (coordinator decision 36).
6. **Propagation repair, 22 November 2026.** The threshold node is the threshold row of `ENGINE-SIGNAL-NODES` and the band bars are `ENGINE-PARAMETER-BOUND-FRAMES`' (coordinator decision 16); Story 14.0 takes candidates from `ENGINE-SPATIAL-QUERIES` (owner decision 3a). The socket spheres are seated in the jar wall, as CAT-060 seats the meter's. No row needs more than the default 30 s Run (the longest, row 3 at `decay_time` 5 s, takes about 10 s).

**Needs the owner.** None.

## 7. Integration

- **Works with:** Battery, System clock, Signal lamp, Powered gate, Wall (delivered); Speaker, Bell, EL-179 (planned). Row 4 and [EL-179](EL-179-continuous-tone-speaker.md) row 2 need both parts; they run in whichever slice lands second.
- **Campaign:** chapter 8, levels 71–80 ("resonator", "each tuned band"), first taught at **78a**, after EL-179 (76a) and before the horn route (79a) (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); reuse 81–100, 117–125, 136–150. [sequence-task-407](../requirements.md#sequence-task-407) ("a resonator visibly builds/loses excitation"); processes [IX-12](../requirements.md#interaction-12), [IX-07](../requirements.md#interaction-07), [IX-06](../requirements.md#interaction-06).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. This identity lacks: (1) an acceptance pair of its own, since its ledger entry links only the umbrella `todo-352`, which has no co-anchored `sequence-task-NNN` in `requirements.md` and is shared by nine identities (the reader refuses two identities on one task source); (2) a decision anchor, since its ledger owner S536 has no `<a id="s536">` anchor in `docs/planning/invest/scope-corrections.md`. The delivering slice adds the section once S7 supplies both; it is not written earlier, because the reader throws on a malformed block. Mode records: `Band=Low`, `Band=Mid`, `Band=High`.
- **Lesson fixtures (S8):** needed only where a level pre-places it.

## 8. Delivery

- **Slice shape.** Single element, data-only.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it), the threshold row of `ENGINE-SIGNAL-NODES` and `ENGINE-PARAMETER-BOUND-FRAMES` (band bars); then Story 14.0 amended with gaps 1, 3 (the band-level observation) and 9; Stories 14.2, 14.1 and 14.3 (a clocked source); S7. Row 4 needs EL-179, planned before it.
- **Files.** Adds `elements/AcousticResonator/` (`AcousticResonator.csproj`, `AcousticResonatorElementDeclaration.cs`, `acoustic_resonator.tscn`, `icon.svg`) and `CuriousContraptions.tests/AcousticResonatorTests.cs`; registration is derived. The test file needs no `Compile` line (main compiles every top-level `*.cs` of the test project by one rule). Until S3 lands: `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596`, `:653`. This spec gains its binding section when S7 allows (section 7).
- **Review tier.** Data-only.
- **Disqualified if** the probe finds no accumulating receiver, that the store is not reset by the generic Reset path, or that the threshold contact needs a rule naming this package.
