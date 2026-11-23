# CAT-061 · speaker — element spec

**Current delivery (24 October 2026):** [CAT-061 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

One current text, repaired 22 November 2026 after the Batch D audit, against checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; citations with `@a6c914e` are the legacy harvest and appear only in the closing appendix. Values marked *proposed* are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). The shared acoustic facts, laws (L1–L7), capability rows (C1–C13, N1–N13), tolerances and Story 14.0 gaps 1–16 are in [EL-044](EL-044-tone-selective-sound-meter.md) and are cited by number.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-061 · `speaker` |
| Requirement anchor | [CAT-061](../requirements.md#current-cat-061) with [todo-345](../requirements.md#todo-345); owner key [CAT-061-I](../invest/current-consumers.md#cat-061-i) |
| Modes | AcousticTone = Low, Mid, High |
| Disposition | To deliver as a single element in Story 14.3, after 14.0, 14.2 and 14.1. **This spec is the single declaration text of the `speaker` package** (coordinator decision 27, 23 November 2026). [EL-180 Pulse speaker](EL-180-pulse-speaker.md) is a Duplicate closure of it: its earlier deltas (the 2 J pulse store, re-arm in seconds, the flush rim, category Control, the seated ports and the `Ready` lamp) are all adopted here, and it closes on rows 1–14 in this slice. One generic store-gated emitter serves both identities, because a per-pulse debit taken in one supply phase cannot be paid (coordinator decision, 22 November 2026, under the owner's standing delegation). [EL-179 Continuous-tone speaker](EL-179-continuous-tone-speaker.md) is a separate element |
| Planned package | id and catalogue key `speaker`, directory `elements/Speaker/`, category `ElementCategory.Control`. Palette order: 425 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md); one number for the package, which EL-180 shares) |
| Capability row | [general-engine-element-map, CAT-061](../general-engine-element-map.md) |
| Status | Not started; blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`; then Story 14.0 and `ENGINE-PARAMETER-BOUND-FRAMES` (tone bars) |

## 2. Declaration

Closest delivered shapes: the System clock for a supplied signal part (`elements/SystemClock/SystemClockElementDeclaration.cs:40`–`:41`) and the Bumper for a consumer that charges a declared energy store (`elements/Bumper/BumperElementDeclaration.cs:42`–`:43`, `:80`). Interfaces: `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `IEnergyStoreElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:35`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the acoustic emitter interface Story 14.0 adds.

| Item | Declaration |
| --- | --- |
| Body | one static body: cabinet box 1.25 × 1.5 × 1.25 m at the origin; foot box 1.6 × 0.18 × 1.55 m at (0, −0.84, 0), so its top (−0.84 + 0.09 = −0.75) meets the cabinet's bottom (−0.75; at the legacy −0.85 the cabinet floated 0.01 m above the foot); two socket spheres r 0.075 m centred on the cabinet's faces at the port positions. Material restitution 1, friction 0.3, rolling resistance 0, threshold 0.1 m/s (the static default, `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:50`) |
| Emitter | one, at the mouth (0.72, 0, 0), axis local +X: kind Pulse, pattern Cone with half-angle 35°, strength 1, tone = `tone`, speed 12 m/s, range 8 m, duration 0.15 s |
| Pulse store | one energy store: capacity 2 J, initial 0 J, maximum charge power 10 W (*proposed*), charged through a `SupplyEnergyStoreTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:51`) |
| Trigger | `ActivationIn` pulses of tick t are one request, served in tick t + 1. It emits only if the pulse store is at capacity and `PowerIn` is powered on that tick (the supply route's powered predicate, as the System clock's oscillator reads it, `engine/gpu/ActivationNetwork.cs:445`: the source enabled with charge remaining and every contact on the route closed); it then empties the store. Otherwise the request expires and is never replayed. Re-arm is the refill time, 0.2 s at full grant; there is no separate interval rule |
| Supply graph | one consumer on `PowerIn` whose target is the pulse store. Electrical node: role Consumer, rated voltage 12, resistance 0.3, capacitance 0, maximum power 10 W |
| Footprint | 1 body, 0 moving, no constraints |

| Parameter | Kind | Unit | Min | Max | Default | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `tone` | Choice: Low, Mid, High | none | 0 | 2 | 1 (Mid) | reject; build-only |

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | PowerIn | Electrical | Input | — | (−0.625, 0, 0) |
| `ActivationIn` | ActivationIn | Signal | Input | Pulse | (0, 0.75, 0) |

The port positions sit on the cabinet's left and top faces, so each socket sphere is seated in its face (the legacy positions left the spheres hanging clear of the cabinet). Presentation: `Visual/Diaphragm` local X from the emission event cue (`PresentationDriver.Event`, `CuriousContraptions.Abstractions/ElementPresentation.cs:21`), one eased push of 0.07 m over 0.15 s (`PresentationCurve.SmoothRiseFall`, `:87`); `Visual/Ready` albedo R, G and B from `PresentationDriver.CommittedStoredFraction` on the pulse store (`ElementPresentation.cs:33`), mapping `ClampToEndpoints` from slate (85, 101, 115)/255 at fraction 0 to gold (247, 203, 82)/255 at fraction 1, as the Cannon binds its charge bar to its store (`elements/Cannon/CannonElementDeclaration.cs:55`–`:57`), so the lamp is fully gold only when the store is full and shows when the speaker can fire; one of three `ToneBars` nodes shown by `tone` (C13); forward wavefront rings and the 0.15 s voice are the engine story's cues. Catalogue: title "Speaker", colour cream (1, 0.97, 0.91); description: "Supply electricity, then trigger a short forward sound pulse. It needs a moment of supply to recharge between pulses. Aim it at a sound meter; solid obstacles block it."

## 3. Art and solidity

| Node | Mesh and colour | Class | Moved by |
| --- | --- | --- | --- |
| `Cabinet` | box, cream `#fff8e9` | solid: box collider | nothing |
| `Foot` | box, navy `#293954` | solid: box collider | nothing |
| `Rim` | gold `#e8b764` ring r 0.58 m printed flush on the front face | decorative: printed flat | nothing |
| `Diaphragm` | navy disc r 0.49 m on the front face, with `Centre`, a cyan `#66b8c9` dome r 0.17 m, as its child. No recess: the cabinet's art and collider are the full box | decorative: moved by a binding | the emission binding |
| `Ready` | disc r 0.05 m printed flush in the front face below the rim, centre (0.625, −0.665, 0): below the rim needs y ≤ −(0.58 + 0.05) = −0.63 and inside the cabinet needs y ≥ −(0.75 − 0.05) = −0.70; slate, gold when the pulse store is full | decorative: printed flat | colour by `PresentationDriver.CommittedStoredFraction` on the pulse store |
| `ToneBars` | three authored nodes of one, two and three navy bars 0.13 × 0.06 m, printed flush on the top face | decorative: printed flat | visibility by `tone` |
| wavefront rings | thin, translucent, shadow-free | decorative: glow | engine cue |
| port spheres ×2 | r 0.075 m, gold `#f7cb52`, seated in the left and top faces | solid: sphere colliders | nothing |

No static proud visible solid lacks a collider. Icon: a cabinet with one ring leaving it. At a glance it differs from the Sound meter by the cone in place of a dial and by its top socket, and from the Tone speaker by that top activation socket.

## 4. Engine capabilities

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body (C1) | present | EL-044 C1 |
| Finite electrical source (C3) | present | EL-044 C3 |
| Pulse `ActivationIn` from a clock or switch (C4) | present | EL-044 C4; `CatalogueIntegrationTests.AClockCountsTwoPulsesThenTheSuppliedCounterOpensItsGate` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:69`) |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| A declared energy store charged from supply at a limited power | present | `EnergyStoreDescriptor` (`CuriousContraptions.Abstractions/EnergyStoreValues.cs:73`), `SupplyEnergyStoreTarget`; `EnergyStoreContractTests.ReorderedStoresKeepIndependentChargeLimitsAndCapacity` (`CuriousContraptions.tests/EnergyStoreContractTests.cs:192`) |
| A stored-fraction presentation driver bound to an energy store (the `Ready` lamp) | present | `PresentationDriver.CommittedStoredFraction` with `Store:` (`elements/Cannon/CannonElementDeclaration.cs:55`–`:57`; admitted only for an energy store the owner holds, `engine/gpu/WorkshopCompiledScene.cs:271`–`:273`); `CannonArtTests.NativeAnnuliAndPrivateCuesUseTheManagedFactoryAndRestoreAuthoredArt` (`CuriousContraptions.tests/CannonArtTests.cs:43`, `:48`) asserts the bar at a full and an empty store |
| Event cue presentation (C12) | narrower | no acoustic event kind (N12) |
| Art shown by a Choice parameter (C13) | narrower | `ENGINE-PARAMETER-BOUND-FRAMES` (choice-bound values, coordinator decision 16) |
| Cone emission, propagation, occlusion (N1) | missing | Story 14.0 |
| Tone band on the emission (N2) | missing | Story 14.0 |
| A store-gated, signal-requested emitter (N5) | missing | Story 14.0, gap 4, whose text requires the declared supply input powered on the serving tick (EL-044 gap 4) |
| Acoustic event, wavefront cue, presentation-only audio (N12) | missing | Story 14.0, gap 11 |
| Committed acoustic read model (N13) | missing | Story 14.0, gap 14 |

Counts: 6 present, 0 untested, 2 narrower, 5 missing. Nothing here needs element-specific engine code.

## 5. Behaviour and numbers

- **Why a store.** One supply phase grants at most the source's power limit × 1/120 s (`engine/gpu/ElectricalSupply.cs:9`; today's code; `ENGINE-SUPPLY-DEMAND` makes it power limit × dt, coordinator decision 30, the same at the default 120 Hz): 1 J from a default Battery (120 W) and 0.083 J at its 10 W minimum (`elements/Battery/BatteryElementDeclaration.cs:44`). A 2 J debit in one tick cannot be paid, so the pulse is paid by filling a store.
- **Re-arm 0.2 s.** 2 J at 10 W, which is 24 ticks of 1/12 J at 120 Hz. With a lifetime of 8/12 + 0.15 = 0.82 s at most five pulses are in flight, the requirement's bound. A System clock at its 0.1 s minimum (`SystemClockElementDeclaration.cs:32`; 12 ticks) yields a pulse on every second or third request, depending on the order of charging and emission within a tick, which Story 14.0 states: a request 12 ticks after an emission finds 11 or 12 ticks of charge, and one 24 ticks after finds 23 or 24. The store starts empty, so a speaker needs 0.2 s of supply before its first pulse; a System clock's first pulse comes after one full interval, so only a clock faster than 0.2 s loses its first pulse. The `Ready` lamp shows a full store.
- **Unpowered expiry.** A full store does not emit unless `PowerIn` is powered on the serving tick; the request expires. A starved source (several speakers on one 10 W Battery) refills more slowly; nothing is created.
- **Ledger.** A default Battery (3 600 J, `BatteryElementDeclaration.cs:42`) pays 1 800 fills; the smallest (60 J) pays 30, so depletion is a usable puzzle.
- **Cone.** Half-angle 35° about +X: at 3 m the cone is 2.1 m in radius; outside it the level is 0. On axis the level is 0.581 at 3 m; a default Sound meter (0.25) hears it to 6.1 m; nothing beyond 8 m (L3).
- **Tick order:** EL-044 L7.
- **Edge contract.** One request per trigger occurrence: a ball resting on an Impact switch makes one pulse, not a stream.

## 6. Acceptance

Planned class `SpeakerTests`, real production simulation, parts placed through the production edit interface. Rows 1–14 are also [EL-180](EL-180-pulse-speaker.md)'s Duplicate-closure check. Tolerances T-level, T-energy and the 5 s stall guard are EL-044's.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | Supplied speaker, a Clock at 1 s on `ActivationIn`, a supplied meter 3 m in front: one emission per clock pulse, each on the tick after its clock pulse; the meter reads 0.581 within T-level and triggers once per pulse; the Battery falls by 2 J per store fill, so after N pulses and the refill that follows it is down 2(N + 1) J within T-energy | `ASuppliedTriggerReleasesOnePaidPulse` | no activation link: no emission; the Battery is down one fill (2 J) and no more | each tone |
| 2 | Unsupplied (Battery `enabled` off) and triggered: nothing; the Battery enabled later (its `enabled` run control) replays nothing. A full store with the Battery disabled does not emit on a trigger | `AnUnpoweredTriggerExpires` | row 1 | store full, the Battery disabled only for the tick the request is served (t + 1) and re-enabled on the next: no emission for that request, and none later without a new trigger |
| 3 | A default Ball (the Basketball, r 0.34 m, restitution 0.55, `elements/Ball/BallElementDeclaration.cs:38`; pair restitution 0.55 × 1 = 0.55 on the switch) released with its lowest point 1.25 m above the Impact switch's button top strikes it at √(2 × 9.81 × 1.25) = 4.95 m/s about 0.50 s after Run, after the store has filled (0.2 s), and keeps bouncing on it: exactly one pulse within a 3 s window. The bounces after the strike sum to 2 × 4.95 / 9.81 × 0.55 / 0.45 = 1.23 s and fall below the 0.1 m/s bounce threshold after about seven (4.95 × 0.55⁷ = 0.08 m/s), so the ball is at rest by about 1.7 s; the switch latches once per Run (CAT-063), so the later contacts publish nothing | `AHeldInputMakesOnePulse` | the ball released with its centre 1.5 m to the −Z side of the switch's centre, 1.5 − 0.5 − 0.34 = 0.66 m clear of the base's −Z edge, missing it: no pulse | released from 2.5 m (strike at 7.0 m/s about 0.71 s after Run; bounces end by about 2.5 s), so it bounces more: still exactly one pulse within the 3 s window |
| 4 | Four Clocks at 0.1 s for 2 s: successive emissions are at least 0.2 s apart within one tick, at most five are in flight, and each follows a full store | `ARetriggerStormIsBoundedAndPaid` | one clock at 0.5 s for 2.25 s: 4 emissions; one Clock at 0.15 s for 3.1 s with supply from the start: exactly 10 emissions, on every second clock pulse, because the empty store refills in 0.2 s (adopted from EL-180 section 5) | two triggers 0.1 s apart give one emission; 0.3 s apart give two |
| 5 | A Battery holding 1.8 J (`capacity` 60, `initial_charge` 0.03): no emission; Battery plus store energy is unchanged within T-energy | `AnUnaffordablePulseIsNotEmitted` | row 1 | a Battery holding 2.2 J gives one pulse; a Battery holding 3 J gives exactly one pulse and never a second (adopted from EL-180 section 5) |
| 6 | Facing away, a Wall between, or the meter 8.5 m away: the meter reads 0 | `DirectionRangeAndOcclusionSilenceTheMeter` | row 1 | 7.9 m (0.167 within T-level) and 8.1 m (0) |
| 7 | Reset clears pulses in flight and pending requests, empties the store and restores the Battery; save then load round-trips `tone` and both links | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`:273`) plus `ResetClearsPulsesAndRestoresTheLedger` | — | Reset with five pulses in flight |
| 8 | A level output or an Electrical output linked to `ActivationIn`, or `tone` outside 0–2, is refused with a typed reason | generic `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`); `AnUndefinedToneIsRefused` | — | — |
| 9 | A meter 3 m away and 30° off axis hears the pulse; at 40° it reads 0 | `TheConeEdgeLiesAtThirtyFiveDegrees` | row 1 | the two angles |
| 10 | Two Clocks at 1 s in phase on `ActivationIn`, store full: one emission on the tick after the pulses, and the committed acoustic read model (N13) reports one request for that tick, not two. The store gate alone gives one emission either way, so the request count is what distinguishes coalescing; the read model exposes each emitter's requests of a tick, served or expired (Story 14.0 gap 14 and probe row N13 of [EL-044](EL-044-tone-selective-sound-meter.md)) | `TriggersInOneTickCoalesce` | one Clock: one emission, one request | — |
| 11 | A speaker yawed 90° is heard along its new axis only | `EmissionFollowsThePlacedPose` | row 1 | — |
| 12 | Muted, audio-suspended and audible runs give identical emissions and ledgers | `MutingAudioChangesNothing` | — | — |
| 13 | Every drawn solid has its collider; the bars shown match `tone` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` with a `speaker` row; `ToneBarsFollowTheChoice` | — | — |
| 14 | A served request produces one diaphragm cue, one wavefront and one voice; an expired request (store not full) produces none | `CuesFollowOnlyCommittedEmissions` | row 1 | — |

Requirement criteria: next-tick coalesced trigger (1, 10); independent supply and unpowered expiry (2); the minimum interval bounding five in flight (4), delivered as the refill time rather than a tick count ([D10](../invest/delivery-batches.md#3-family-level-decisions)); each tone (1, 13); speed, reach, duration and cone with attenuation (1, 6, 9); direction, range, occlusion, retrigger and no-power tests (2, 4, 6); cone, wave and audio observing the committed occurrence, with mute and audio suspension changing nothing (12, 14). EL-180's requirement row (edge contract for a held input, no infinite pulse energy) is rows 3, 4, 5 and 1. Chrome recipe: post-catalogue.

## 7. Integration

Works with Battery, System clock, Impact switch, Wall (delivered); Sound meter and Bell (planned before it); EL-044, EL-046, EL-049, EL-050, EL-051 and EL-179 (planned after it). Campaign: first use in levels 71–80 ([campaign reservations](../invest/campaign-reservations.md)), first taught at **75b**, after the Bell and Sound meter (75a) (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); no level places it today. Coverage binding: catalogue key `speaker` in `docs/coverage/catalogue-elements.json`. Lesson fixtures only where a level pre-places it; such a level waits for [S8, lesson fixtures for any admitted package](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).

## 8. Delivery

- **Shape.** One element slice that also closes EL-180. Data-only review tier if Story 14.0 delivers N1, N2, N5, N12 and N13.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); then Story 14.0 amended with gaps 4, 11, 12, 13 and 14; `ENGINE-PARAMETER-BOUND-FRAMES` (the tone bars shown by the `tone` Choice; coordinator decision 16); Story 14.2; Story 14.1 (the meter that observes it).
- **Files.** Adds `elements/Speaker/` (`Speaker.csproj`, `SpeakerElementDeclaration.cs`, `speaker.tscn`, `icon.svg`) and `CuriousContraptions.tests/SpeakerTests.cs`; one line in `CuriousContraptions.slnx`; one row in the visible-solid table; EL-180's ledger row cites the package and tests. Membership tables are derived ([registration derivation](../../../_bmad-output/implementation-artifacts/spec-registration-derivation.md)); until [S3](../../../_bmad-output/implementation-artifacts/spec-s3-remaining-per-package-lists.md) lands it also edits that contract's five test-side lists.
- **Disqualified if** the slice's probe finds no store-gated emitter (N5). Then that is an engine slice first.

## 9. Open questions

None open. Resolved:

1. Cone angle: a 35° half-angle (EL-044 A2).
2. Order: propagation and reception are Story 14.0's; the meter (14.1) precedes this part.
3. Re-arm: the store's refill time, 0.2 s of game time, not a tick count ([D10](../invest/delivery-batches.md#3-family-level-decisions)).
4. Supply: a 2 J pulse store, initially empty, charged at 10 W; emission needs the store full and `PowerIn` powered on the serving tick (coordinator decision, 22 November 2026, under the owner's standing delegation; reason in section 5, "Why a store").
5. Wavefront cue: the engine story's (gap 11) and DESIGN.md's; the legacy positional tuple is not used.
6. Sockets seated in the cabinet faces; no recess; tone bars and rim printed flush (coordinator decision, same date: visible = solid and the delivered art test admit only box solids). The bars are printed, one to three by tone: [DESIGN.md](../../../DESIGN.md) "Sound speaker" is being updated to that form (owner decision, 22 November 2026).

## Engine conformance

Against the owner's conformance addendum of 22 November 2026 (element-free engine gate in [AGENTS.md](../../../AGENTS.md); design contracts D1–D7 in `_bmad/custom/element-free-engine-design.md`). **Verdict: conforms on paper; buildable after the engine programme, once Story 14.0 delivers the emitter.**

1. **Declaration data only.** A static body, four colliders, one emitter row with a signal request, one energy store, one supply consumer, two ports, one parameter and its presentation bindings. No queue, cooldown or update in the part.
2. **Generic laws.** Payment: the delivered store-charging rule of the supply network. Emission: the store-gated emitter law (N5), stated without reference to any part. Next-tick service and same-tick coalescing: the signal network's pulse rule. Hearing: the propagation law (L1–L7).
3. **Layers.** Colliders: four hidden proxies (two boxes, two spheres). Visuals: the nodes of section 3; none is a collider. Animation: the diaphragm binding, the `Ready` lamp colour and the tone-bar visibility read committed state and write nothing back.
4. **One-way flow and typing.** Cues read the committed emission; muting changes nothing (row 12). Tone and pattern are enums; ports are typed.
5. **Flat state.** 1 static body; 4 colliders (2 boxes, 2 spheres); 0 joints and drives; 1 energy store; 1 emitter with 1 request row; 1 supply node and 1 electrical node; 2 ports; 1 event kind; 7 presentation bindings (diaphragm, three `Ready` colour channels and three bar visibilities) plus the engine's cues. All fixed at compile time.
6. **Performance.** Static body, four primitive colliders. At most five emissions in flight. Reception work is Story 14.0's to bound from compiled counts, with candidates from `ENGINE-SPATIAL-QUERIES` (see the batch summary).
7. **Tests are scenarios.** Rows 1–14 run the production simulation; Story 14.0's fixtures prove the law without this package.

## Appendix: Historical evidence

Evidence of the legacy part at `a6c914e`, kept so the harvest stays traceable. Nothing here is a requirement, a tolerance or a design instruction; sections 1–9 are the spec. Legacy tick counts, sample rates and byte counts in the cited files are history only.

| # | Fact | Citation |
| --- | --- | --- |
| 1 | Legacy ports, trigger handling, power gating, re-arm, mouth and emission lived in a per-part update. Behaviour is restated in section 2; the per-part code is not the design | `parts/SpeakerPart.cs@a6c914e:L38-L89` |
| 2 | A cone pulse is heard on axis, silent behind, beside, beyond range and after its window; all three tones behave alike (behaviour in rows 6 and 9) | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L10-L27` |
| 3 | Zero direction, undefined tone, negative tick and non-finite sample points were refused | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L28-L36` |
| 4 | A supplied speaker wired to a supplied clock emitted on the tick after the clock pulse, in either part order; origin = part transform × mouth; Reset cleared pulses | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L41-L68` |
| 5 | An unpowered trigger expired; a retrigger storm stayed bounded; removing supply ended emission (behaviour in rows 2 and 4) | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L69-L98` |
| 6 | Muting or stopping playback changed no pulse count (row 12) | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L99-L122` |
| 7 | Speaker to supplied meter: one meter trigger per pulse; a Wall, a reversed speaker or an unsupplied meter gave none | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L14-L54` |
| 8 | Emission used the solved pose, not the rendered transform (row 11) | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L322-L354` |
| 9 | Only committed waves rendered | `CuriousContraptions.tests/AcousticWavefrontTests.cs@a6c914e:L27-L87` |
| 10 | Only committed diaphragm kicks rendered | `CuriousContraptions.tests/AcousticMotionTests.cs@a6c914e:L22-L109` |
| 11 | A failed emission restored the request queue: a faulting-tick mechanism, not carried | `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs@a6c914e:L26-L92` |
| 12 | No level used the speaker | `docs/coverage/catalogue-elements.json` (empty fixture list) |
| 13 | Legacy geometry, art and catalogue entry | `parts/SpeakerPart.cs@a6c914e:L90-L117`; `parts/catalog/speaker.tres@a6c914e:L5-L11`; `engine/AcousticAudio.cs@a6c914e:L11-L31`; `engine/Acoustics.cs@a6c914e:L11-L47` |

**Files harvested:**
- `parts/SpeakerPart.cs`
- `parts/catalog/speaker.tres`
- `parts/scenes/speaker.tscn` (no element knowledge: script reference only)
- `engine/Acoustics.cs`
- `engine/AcousticAudio.cs`
- `engine/PartArt.cs` (ring half-width convention)
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/AcousticTests.cs`
- `CuriousContraptions.tests/SoundMeterTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/AcousticWavefrontTests.cs`
- `CuriousContraptions.tests/AcousticMotionTests.cs`
- `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs`
