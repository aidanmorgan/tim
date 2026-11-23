# EL-180 · Pulse speaker — element readiness spec

**Current delivery (24 October 2026):** [EL-180 delivery slice](../invest/named-elements.md#element-180). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against checkout `8d1d68aa`, repaired on 22 November 2026 against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-h.md`) and on 23 November 2026 against the second audit (`docs/planning/elements/readiness/audits/reaudit-group-1.md`). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the current checkout. Shared acoustic facts A1–A13, laws L1–L7, the capability probe (C1–C13, N1–N13) and the shared tolerances are in [EL-044](EL-044-tone-selective-sound-meter.md). This spec holds the **speaker facts K1–K10** that [EL-179](EL-179-continuous-tone-speaker.md) reuses; the speaker declaration is [CAT-061's](CAT-061-speaker.md#2-declaration) (coordinator decision 27, 23 November 2026).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-180 · Pulse speaker |
| Type | Sound |
| Anchor | [requirements.md#element-180](../requirements.md#element-180); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Speaker/pulse/continuous modes"); [named-elements entry](../invest/named-elements.md#element-180); owner S532 |
| Disposition | **Duplicate closure of [CAT-061 Speaker](CAT-061-speaker.md).** Every earlier delta is adopted in CAT-061: the 2 J pulse store charged at 10 W, re-arm in seconds (0.2 s, from the store), the rim drawn flush, category Control, the seated ports and the `Ready` lamp. Closed by [CAT-061 rows 1–14](CAT-061-speaker.md#6-acceptance) in the Speaker slice (coordinator decision 16, 22 November 2026; coordinator decision 27, 23 November 2026). The requirement row ([element-180](../requirements.md#element-180): "Supplied trigger releases a bounded acoustic pulse"; "Held input follows its edge contract and cannot create infinite pulse energy") asks for nothing CAT-061 lacks: it says nothing of the diaphragm's motion, so CAT-061's eased push stands. There is no second package, no second declaration text and no second acceptance text (sections 2 and 5). |
| Planned package | none of its own: it is delivered as `speaker` in `elements/Speaker/` (CAT-061's package). Coverage is through CAT-061's catalogue key; there is no named binding of `speaker` (section 7). Palette order is CAT-061's, 425 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); no order of its own. |
| Related | Sibling [EL-179 Continuous-tone speaker](EL-179-continuous-tone-speaker.md) (its own entry); supply [CAT-005](CAT-005-battery.md)/[EL-196](EL-196-battery.md); triggers from [CAT-017 Clock](CAT-017-clock.md), [CAT-063 Switch](CAT-063-switch.md); heard by [CAT-060](CAT-060-sound_meter.md)/[EL-044](EL-044-tone-selective-sound-meter.md). |
| Roadmap story | Story 14.3 (CAT-061), which closes this identity; [EL-180 ledger entry](../invest/named-elements.md#element-180). |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it) and `ENGINE-PARAMETER-BOUND-FRAMES` (tone bars); then Stories 14.0, 14.2 and 14.1, then Story 14.3, which delivers it. |

## 2. Declaration

The `speaker` declaration, art and bindings are [CAT-061 sections 2–3](CAT-061-speaker.md#2-declaration); this identity adds none (coordinator decision 27, 23 November 2026). The earlier EL-180 declaration text is withdrawn: its `Ready` lamp is adopted in CAT-061; its damped diaphragm oscillation gives way to CAT-061's eased 0.07 m push, because the requirement row asks nothing of the diaphragm's motion; and the wavefront rings are Story 14.0's cues, not package bindings.

### Art and solidity

[CAT-061 section 3](CAT-061-speaker.md#3-art-and-solidity). This heading stays because [EL-179](EL-179-continuous-tone-speaker.md) links to it.

### Speaker facts (shared with EL-179)

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| K1 | Ports PowerIn (−0.8, 0, 0) Electrical input, ActivationIn (0, 0.85, 0) Activation input. | `parts/SpeakerPart.cs@a6c914e:L57-L61` | carry forward the port set; the legacy positions (−0.8, 0, 0) and (0, 0.85, 0) are not carried forward (the sockets are seated in the faces, CAT-061 section 2) |
| K2 | Trigger only; any other command rejects. | `parts/SpeakerPart.cs@a6c914e:L66-L71`, `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L95-L95` | carry forward |
| K3 | Requests from earlier ticks are consumed together (coalesced) at the next tick. | `parts/SpeakerPart.cs@a6c914e:L74-L81` | carry forward the next-tick rule; do not carry forward the per-part request set |
| K4 | Emission needs PowerIn powered and ≥ 24 ticks since the last pulse; an unpowered request is dropped, not deferred. | `parts/SpeakerPart.cs@a6c914e:L82-L87`, `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L69-L84` | carry forward as the 0.2 s store re-arm; do not carry forward the tick counter |
| K5 | Retrigger storm (4 triggers per tick for 240 ticks): never more than 5 pulses in flight; 9–10 pulses total; after supply loss all pulses expire and the speaker goes inactive. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L85-L94` | carry forward the bounds, not the tick script |
| K6 | A clock trigger produces the pulse on the next tick regardless of part order; the pulse origin is the transformed mouth; Reset clears pulses, count and active state. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L41-L68` | carry forward |
| K7 | Muting or stopping audio leaves pulse count and activity unchanged. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L99-L122` | carry forward |
| K8 | Speaker → meter only through an unblocked forward path with meter supply. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L14-L54` | carry forward |
| K9 | The tone is an exported per-instance enum with default Mid. | `parts/SpeakerPart.cs@a6c914e:L38-L38` | carry forward as a typed configuration value |
| K10 | Runtime checkpoint class snapshots pulses, requests and counters per part. | `parts/SpeakerPart.cs@a6c914e:L11-L37` | do not carry forward (CPU transaction participant; the worker's committed state owns Reset) |

### Behaviour and numbers

The numbers and their derivations are [CAT-061 section 5](CAT-061-speaker.md#5-behaviour-and-numbers) (the 2 J pulse store charged at 10 W, the 0.2 s re-arm, the empty start, the ledger and the reach); this identity re-derives none of them (coordinator decision 27). They meet this identity's requirement row as follows:

- **"Supplied trigger releases a bounded acoustic pulse."** Each pulse is paid in full from the store and the store needs `PowerIn` powered (CAT-061 rows 1, 2 and 5).
- **"Held input follows its edge contract."** A latched Impact switch is one trigger occurrence, so it makes one pulse (CAT-061 row 3).
- **"Cannot create infinite pulse energy."** The store's refill bounds a retrigger storm and every fill is debited from a finite source (CAT-061 row 4).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S532): AcousticPropagation, ElectricalPower, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction). The numbered rows are the shared probe in [EL-044 section 3](EL-044-tone-selective-sound-meter.md#shared-capability-probe); the same rows are [CAT-061 section 4](CAT-061-speaker.md#4-engine-capabilities).

| Capability this identity needs | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body and colliders (C1) | present | EL-044 C1 |
| Finite electrical source that the debit is paid from (C3) | present | EL-044 C3 |
| Pulse `ActivationIn` from a clock or switch (C4) | present | EL-044 C4; next-tick delivery of a clock pulse is exercised by `CatalogueIntegrationTests.AClockCountsTwoPulsesThenTheSuppliedCounterOpensItsGate` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:69`) |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| A supplied energy store charged at a declared power and clamped at capacity | present | `EnergyStoreDescriptor` (`CuriousContraptions.Abstractions/EnergyStoreValues.cs:73`), `SupplyEnergyStoreTarget` (`SupplyContractValues.cs:51`); `EnergyStoreContractTests.ReorderedStoresKeepIndependentChargeLimitsAndCapacity` (`CuriousContraptions.tests/EnergyStoreContractTests.cs:192`: a 30 W store reaches its 45 J capacity and stops; Reset restores the authored energy) |
| A stored-fraction presentation driver for CAT-061's `Ready` lamp | present | `PresentationDriver.CommittedStoredFraction` bound to an energy store (`elements/Cannon/CannonElementDeclaration.cs:55`–`:57`); `CannonArtTests.NativeAnnuliAndPrivateCuesUseTheManagedFactoryAndRestoreAuthoredArt` (`CuriousContraptions.tests/CannonArtTests.cs:43`, `:48`) |
| Event cue presentation (C12) | narrower | `PresentationDriver.Event` exists; no acoustic event kind (N12) |
| Art visibility chosen by a `Choice` (tone bars; C13) | narrower | EL-044 C13; owned by `ENGINE-PARAMETER-BOUND-FRAMES` (choice-bound values, coordinator decision 16) |
| Cone pulse emission, propagation, occlusion (N1) | missing | Story 14.0, with reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` |
| Tone band on the emission (N2) | missing | Story 14.0 (its text names the band) |
| An emitter requested by a signal pulse, served next tick or expired, gated by a store at capacity and its supply input powered on the serving tick (the supply route's powered predicate, as the System clock's oscillator reads it, `engine/gpu/ActivationNetwork.cs:445`), which it empties (N5) | missing | Story 14.0, gap 4 |
| Acoustic event kind, wavefront cue, presentation-only audio (N12) | missing | Story 14.0, gap 11 |
| Committed acoustic read model (N13; CAT-061 rows 10 and 14 read it) | missing | Story 14.0, gap 14 |

Counts: 6 present, 0 untested, 2 narrower, 5 missing. Nothing here needs element-specific engine code.

**Dependencies.** The engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`, `ENGINE-PARAMETER-BOUND-FRAMES`); Story 14.0; Battery (delivered); a trigger source (System clock, Impact switch: delivered); a Sound meter (CAT-060, Story 14.1) to observe.

### Engine conformance (22 November 2026)

This identity declares nothing of its own, so its conformance is that of the `speaker` declaration: [CAT-061, Engine conformance](CAT-061-speaker.md#engine-conformance) (coordinator decision 27, 23 November 2026).

**Engine references to the element.** No `elements/Speaker` directory exists in this checkout, and a search of `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle`, `CuriousContraptions.Core` and the `CuriousContraptions.Abstractions*` projects for `speaker` and `Speaker` finds nothing, so there is no element-naming engine code to refactor. Story 14.3's capability probe repeats the search.

## 4. Sources and legacy

- Requirement row [element-180](../requirements.md#element-180): "Supplied trigger releases a bounded acoustic pulse"; outcome "Held input follows its edge contract and cannot create infinite pulse energy". [current-cat-061](../requirements.md#current-cat-061): next-tick coalesced trigger, independent supply, minimum interval 24 ticks bounds five in-flight pulses, Low/Mid/High tones, 12 unit/s, 8 unit reach, 0.15 s, 35° cone; mute cannot change gameplay.
- Named entry [element-180](../invest/named-elements.md#element-180), owner S532; map composition "One supplied trigger debits bounded pulse energy; no implicit indefinitely sustained tone"; component research "Pulse speaker (EL-180, CAT-061) | Electrical node + acoustic source (one pulse per rising trigger)".
- Legacy harvest K1–K10; also harvested by [CAT-061](CAT-061-speaker.md).
- **Files harvested:** `parts/SpeakerPart.cs`, `parts/catalog/speaker.tres`, `CuriousContraptions.tests/AcousticTests.cs`, `CuriousContraptions.tests/SoundMeterTests.cs`, `reference/cpu/MachinePart.cs` (`AddBox` extents); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-180](../requirements.md#element-180), [current-cat-061](../requirements.md#current-cat-061). The rows, fixtures and planned class `SpeakerTests` are [CAT-061's acceptance rows 1–14](CAT-061-speaker.md#6-acceptance), the single authority for both identities (coordinator decision 16, 22 November 2026; coordinator decision 27, 23 November 2026); they are not copied here, so one package has one acceptance text. Tolerances and stall guard: [EL-044 shared tolerances](EL-044-tone-selective-sound-meter.md#shared-tolerances-and-stall-guards). This identity closes on:

| EL-180 criterion | CAT-061 row | Test |
| --- | --- | --- |
| One paid pulse per trigger, on the next tick; the Battery pays 2 J per store fill | 1 | `SpeakerTests.ASuppliedTriggerReleasesOnePaidPulse` |
| An unpowered trigger expires and nothing replays | 2 | `SpeakerTests.AnUnpoweredTriggerExpires` |
| A held input makes one pulse (edge contract, requirement outcome) | 3 | `SpeakerTests.AHeldInputMakesOnePulse` |
| A retrigger storm is bounded and paid (no infinite pulse energy, requirement outcome) | 4 | `SpeakerTests.ARetriggerStormIsBoundedAndPaid` |
| An unaffordable pulse is not emitted | 5 | `SpeakerTests.AnUnaffordablePulseIsNotEmitted` |
| Direction, range and occlusion | 6 | `SpeakerTests.DirectionRangeAndOcclusionSilenceTheMeter` |
| Reset and save/load | 7 | `SpeakerTests.ResetClearsPulsesAndRestoresTheLedger` plus the generic catalogue lifecycle test |
| Typed refusals | 8 | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused`; `SpeakerTests.AnUndefinedToneIsRefused` |
| Triggers of one tick coalesce into one request | 10 | `SpeakerTests.TriggersInOneTickCoalesce` |
| Cues follow only committed emissions; muting and audio suspension change nothing | 12, 14 | `SpeakerTests.MutingAudioChangesNothing`; `SpeakerTests.CuesFollowOnlyCommittedEmissions` |

The rows this spec once proposed (row 3's higher drop, row 4's 0.15 s Clock control, row 5's 3 J Battery) are adopted into CAT-061 rows 3–5.

**Chrome recipe (pending, post-catalogue).** Battery → Speaker `PowerIn`; Clock → Speaker `ActivationIn`; supplied Sound meter 3 m in front; `tone` chosen through the configuration control. Story 14.3's `tools/e2e/cat-061.test.ts` criterion is deferred with the browser phase.

## 6. Open questions

**Resolved.**

1. **Pulse energy.** 2 J per pulse, the same for every tone and strength: [D1](../invest/delivery-batches.md#3-family-level-decisions); strength is fixed at 1, so there is nothing to scale with.
2. **Re-arm unit.** 0.2 s at every cadence: [D10](../invest/delivery-batches.md#3-family-level-decisions); it is the store's 2 J ÷ 10 W.
3. **Duplicate or variant.** Decided: Duplicate closure of CAT-061, since no delta remains (coordinator decision 27, 23 November 2026). The four earlier deltas, the seated ports and the `Ready` lamp are all in CAT-061, and the requirement row asks for nothing beyond CAT-061 rows 1 and 3–5. It needs no package of its own. EL-179 is the separate entry.
4. **Supply semantics** (CAT-061 open question 4). Each pulse is paid from a declared 2 J pulse store charged at 10 W; an emission needs a full store and empties it: coordinator decision, 22 November 2026, under the owner's standing delegation. Reason: a direct 2 J debit exceeds what one supply phase can grant (1 J at 120 W: `power_limit` × 1/120 s in today's code; `ENGINE-SUPPLY-DEMAND` makes it `power_limit` × dt, coordinator decision 30), the store uses a delivered capability, and one rule gives both the payment and the 0.2 s re-arm.
5. **Other coordinator decisions, 22 November 2026.** (a) The store starts empty, so no Run begins with unpaid energy. (b) Tick order is restated against the delivered network: routing is same-tick; the request is served on the next tick. (c) Row 3 uses a bouncing ball and row 4 a single 0.15 s clock, so their counts do not sit on a tick boundary. (d) Coverage is through CAT-061's catalogue key. (e) Palette order is CAT-061's, 425 (coordinator decision 36).

**Needs the owner.** None.

## 7. Integration

- **Works with:** Battery, System clock, Impact switch, Wall (delivered); Sound meter CAT-060, [EL-044](EL-044-tone-selective-sound-meter.md), [EL-049](EL-049-acoustic-resonator.md), [EL-046](EL-046-listening-horn.md), [EL-050](EL-050-acoustic-screen.md), [EL-051](EL-051-acoustic-dish.md) (planned).
- **Campaign:** chapter 8, levels 71–80 (first lesson around 72), reuse 81–100, 117–125, 136–150. CAT-061 retained behaviour [todo-345](../requirements.md#todo-345), CAT-060 [todo-346](../requirements.md#todo-346), [sequence-task-407](../requirements.md#sequence-task-407); processes [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07), [IX-12](../requirements.md#interaction-12).
- **Coverage binding (planned, not yet live).** CAT-061's catalogue key; the `speaker` package is a catalogue package, and this identity is a Duplicate closure of it. There is no named binding: the package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239, binds one named identity to one package, and this spec does not claim `speaker` that way. Whether a named identity closed against a catalogue part needs its own coverage record is for the coverage follow-on story S7; if it does, this identity lacks all three preconditions: its ledger entry has no `todo-NNN` link, there is no co-anchored `sequence-task-NNN`, and its ledger owner S532 has no `<a id="s532">` anchor in `docs/planning/invest/scope-corrections.md`.
- **Lesson fixtures:** as for the Speaker; needed only where a level pre-places it. Such a level waits for [S8, lesson fixtures for any admitted package](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** A Duplicate closure inside Story 14.3's slice, which delivers the `speaker` package. No slice or package of its own; the ledger row cites the slice record and CAT-061 rows 1–14.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it) and `ENGINE-PARAMETER-BOUND-FRAMES` (tone bars); then Story 14.0 amended with gaps 4, 11, 12, 13 and 14; Story 14.2; Story 14.1; then Story 14.3, which delivers the `speaker` package as CAT-061 sections 2–3 declare it.
- **Files.** None beyond the Speaker slice's: CAT-061 rows 1–14 as tests in `CuriousContraptions.tests/SpeakerTests.cs` (no `Compile` line is needed) and the EL-180 ledger row citing them.
- **Review tier.** That of the Speaker slice (data-only if Story 14.0 delivered N5).
- **Disqualified if** the delivered `speaker` package pays a pulse any other way than the declared store, keeps a tick-count interval, or needs a parameter CAT-061 does not list. Each is a second rule for the same behaviour and is fixed in the Speaker slice, not carried beside this one.
