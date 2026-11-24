# CAT-009 · bell — element spec

**Current delivery (24 October 2026):** [CAT-009 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

One current text, repaired 22 November 2026 after the Batch D audit, against checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; citations with `@a6c914e` are the legacy harvest and appear only in the closing appendix. Values marked *proposed* are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). The shared acoustic facts, laws (L1–L7), capability rows (C1–C13, N1–N13), tolerances and Story 14.0 gaps 1–16 are in [EL-044](EL-044-tone-selective-sound-meter.md) and are cited by number. This spec defines one more capability, **N14** (Story 14.0 gap 17), which the Wind chimes also use.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-009 · `bell` |
| Requirement anchor | [CAT-009](../requirements.md#current-cat-009) with [todo-347](../requirements.md#todo-347); owner key [CAT-009-I](../invest/current-consumers.md#cat-009-i) |
| Modes | AcousticTone = Low, Mid, High (each qualified separately) |
| Disposition | To deliver as a single element in Story 14.2, the first acoustic part after the engine story 14.0 (owner, 18 October 2026). No named identity maps to it |
| Planned package | id and catalogue key `bell`, directory `elements/Bell/`, category `ElementCategory.Control` (there is no Sound category, `CuriousContraptions.Abstractions/ElementCategory.cs:5`). Palette order: 423 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)) |
| Capability row | [general-engine-element-map, CAT-009](../general-engine-element-map.md) |
| Status | Not started; blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-CONTACT-OBSERVATIONS`; then Story 14.0 and `ENGINE-PARAMETER-BOUND-FRAMES` (tone bars) |

## 2. Declaration

Closest delivered shapes: the Impact switch for a static part that observes a real hit (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:12`, `:79`) and the Funnel for a hollow tapered collider (`elements/Funnel/FunnelElementDeclaration.cs:10`). Interfaces: `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the acoustic emitter interface Story 14.0 adds.

| Item | Declaration |
| --- | --- |
| Body | one static body; colliders come from the scene (section 3). Material restitution 1, friction 0.3, rolling resistance 0, threshold 0.1 m/s: the delivered static default (`ImpactSwitchElementDeclaration.cs:50`) |
| Emitter | one, at the local origin: kind Pulse, pattern Omnidirectional, tone = `tone`, speed 12 m/s, range 8 m, duration 0.15 s |
| Excitation (N14) | a contact on any of this body's four colliders by any dynamic body, with approach speed at or above 0.8 m/s. Strength = clamp(m × v / 6, 0.05, 1), m the striker's mass in kg and v the approach speed in m/s. Minimum interval 0.2 s. Strikes committed in one tick give one emission with the greatest strength |
| Stores, ports, constraints | none. Footprint 1 body, 0 moving |

| Parameter | Kind | Unit | Min | Max | Default | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `tone` | Choice: Low, Mid, High | none | 0 | 2 | 1 (Mid) | reject; build-only |

The threshold, reference momentum, floor and interval are fixed declared values ([D13](../invest/delivery-batches.md#3-family-level-decisions)). The target set is every dynamic body, not only payloads, so a Weight or a lever arm also rings it. Presentation: `Visual/Clapper` local rotation Z from the emission event cue (`PresentationDriver.Event`, `CuriousContraptions.Abstractions/ElementPresentation.cs:21`), one eased swing of 0.1 rad over 0.4 s (`PresentationCurve.SmoothRiseFall`, overlap `SaturatingSum`, `:87`–`:88`); one of three `ToneBars` nodes shown by `tone` (C13); three wavefront rings per emission and the bell voice are the engine story's cues. Catalogue: title "Bell", colour gold (0.91, 0.72, 0.39); description: "Strike it with a moving ball or weight and it rings in every direction. Harder, heavier hits ring louder; resting on it is silent. No battery needed. A sound meter can turn the ring into a signal."

## 3. Art and solidity

| Node | Mesh and colour | Class | Moved by |
| --- | --- | --- | --- |
| `Shell` | hollow taper, outer radius 0.25 m at y 0.375 to 0.59 m at y −0.325, wall 0.05 m, open below, gold `#e8b764` | hollow: Frustum collider | nothing |
| `Crown` | sphere r 0.25 m at (0, 0.375, 0), gold (*proposed*) | solid: sphere collider | nothing |
| `Rim` | torus, centreline 0.6 m, tube 0.035 m, at y −0.325, cream `#fff8e9` | solid: Torus collider | nothing |
| `Hanger` | cylinder r 0.07 m, 0.2 m long, at (0, 0.70, 0), navy `#293954` | solid: Cylinder collider | nothing |
| `Clapper` | rod r 0.06 m and ball r 0.1 m (navy and cyan `#66b8c9`), hanging inside the shell with the ball's centre at y −0.19 m, so it ends 0.035 m above the mouth | decorative: swung by a binding, and enclosed by the shell | the emission binding |
| `ToneBars` | three authored nodes of one, two and three navy bars printed flush on the shell | decorative: printed flat | visibility by `tone` |
| wavefront rings | thin, translucent, shadow-free | decorative: glow | engine cue |

The shell is drawn hollow because the hollow Frustum is the tapered collider that exists; a ball smaller than the 1.08 m mouth can enter from below, and only such a ball can reach the clapper, which it passes through. The clapper alone moves: a rocking shell would leave its collider behind. The tone bars make the three tones readable without sound or colour. Icon: the bell pictogram. At a glance it differs from every other sound part by the tapered hanging shape with no cabinet and no socket.

## 4. Engine capabilities

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body (C1) | present | EL-044 C1 |
| Frustum, Torus, sphere and Cylinder colliders in contact (C10) | present | `ColliderShapeKind` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`); `VisibleSolidColliderTests.ABallDroppedOntoTheBumpersUpperRingStopsAtItsDrawnTube` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:650`), `CatalogueIntegrationTests.AFunnelGuidesAnOffsetBallThroughTheDetectorAndLightsItsLamp` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:241`) |
| Generic lifecycle (C9) | present | EL-044 C9 |
| A hit qualified by approach speed that can repeat | narrower | `SignalContactDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:51`) latches once per Run (`ContactActivationTests.CommittedContactLatchesSourceAndDeclaredTargetOnceWithoutMutatingPriorCheckpoint`, `CuriousContraptions.tests/ContactActivationTests.cs:105`); a repeating hit with a cooldown exists only on contact work that pays energy (`WorkshopPhysicsLawSensorWorkTests.CooldownEligibilityIsPerBodyAndOpensAtTheExactSeventyTwoStepBoundary`, `CuriousContraptions.tests/WorkshopPhysicsLawSensorWorkTests.cs:253`). Owner: `ENGINE-CONTACT-OBSERVATIONS` (the repeating qualified hit; engine-stories-index C23), with the emission it excites in Story 14.0 (N14) |
| Event cue presentation (C12) | narrower | Story 14.0: no acoustic event kind today (N12) |
| Art shown by a Choice parameter (C13) | narrower | `ENGINE-PARAMETER-BOUND-FRAMES` (choice-bound values, coordinator decision 16) |
| Emission, propagation, occlusion (N1), with an omnidirectional pattern | missing | Story 14.0; its text names only a direction cone (pattern text in the [batch summary](readiness/batch-d.md)) |
| Tone band on the emission (N2) | missing | Story 14.0 |
| Acoustic event, wavefront cue, presentation-only audio (N12) | missing | Story 14.0, gap 11 |
| Committed acoustic read model (N13) | missing | Story 14.0, gap 14; the acceptance rows read emissions and levels through it |
| **N14** an emitter excited by a qualified contact: declared colliders, a declared body target set, an approach threshold, strength from striker momentum with a declared reference, floor and cap, a minimum interval, strongest in a tick | missing | Story 14.0, gap 17 |

Counts: 3 present, 0 untested, 3 narrower, 5 missing. Nothing here needs element-specific engine code: the emitter and its excitation are declaration rows, and the Wind chimes declare the same capability with other values.

## 5. Behaviour and numbers

- **Strength.** Momentum of approach over 6 kg·m/s, so harder and heavier hits are louder (the requirement's "mass/impact-dependent"). It does not depend on bounce. Examples: Basketball (1 kg) at 4 m/s, 0.67; Bowling ball (4 kg) from 1.5 m/s, 1; Tennis ball (0.35 kg) at 4 m/s, 0.23; below 0.3 kg·m/s, the 0.05 floor.
- **Threshold 0.8 m/s** is the Impact switch's default (`ImpactSwitchElementDeclaration.cs:33`): one meaning of "a real hit" across parts. Resting and gentle contact are silent.
- **Reach** (L3). A default Sound meter (threshold 0.25) hears a full ring to 6.1 m and a 0.67 ring to 4.6 m, and never hears a Tennis ball's 0.23; the most sensitive meter (0.05) hears that to 6.8 m.
- **Interval 0.2 s**, in seconds ([D10](../invest/delivery-batches.md#3-family-level-decisions)): with a lifetime of 8/12 + 0.15 = 0.82 s, at most five emissions are in flight.
- **No added impulse.** The bell is an ordinary static collider; the striker rebounds by contact alone.
- **Tick order (acoustic, Story 14.0's to confirm).** The contact commits in the mechanical substeps of tick t; the emission is created in the acoustic field phase of tick t + 1 (`docs/gpu-f32-physics.md:165`–`:171` draws the field phase as Phase 2, before the substeps); a meter's pulse and contact follow by EL-044 L7. This is the acoustic phase, not the optical solve, which runs after the last substep ([EL-176 section 10](EL-176-red-laser-emitter.md#optical-conventions), item 4).

## 6. Acceptance

Planned class `BellTests`, real production simulation. Emissions and levels are read through the committed acoustic read model (N13) and a declared receiver fixture of Story 14.0, because the Sound meter is delivered next. Tolerances T-level and the 5 s stall guard are EL-044's. The 0.05 m/s bands around the threshold exceed contact-speed jitter and stay far from 0.8.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | A 1 kg ball meets the bell at 4 m/s: one emission, strength 0.667 within T-level; a receiver 3 m away reads 0.388 within T-level; the ball leaves no faster than it came | `ARealStrikeRingsOnceWithMomentumStrength` | the same ball lowered onto the bell: silent | six approach directions on a rotated bell; each tone |
| 2 | Approach at 0.75 m/s is silent; at 0.85 m/s it rings | `ApproachBelowThresholdIsSilent` | row 1 | the two speeds |
| 3 | A 0.1 kg ball at 2 m/s (0.033 unclamped) rings at 0.05; a Bowling ball at 2 m/s (1.33 unclamped) rings at 1 | `StrengthHasAFloorAndACap` | row 1 | — |
| 4 | A ball that strikes and stays in contact rings once; lifted and dropped again it rings again | `RestingContactRingsOnceAndSeparationRearms` | — | two balls striking 0.1 s apart give one emission; 0.3 s apart give two |
| 5 | Strikes arriving faster than the interval for 2 s: successive emissions at least 0.2 s apart within one tick, at most five in flight | `RapidStrikesAreRateLimited` | one strike a second: one emission each | — |
| 6 | A 0.1 kg and a 1 kg ball at 6 m/s in one tick: one emission of strength 1, in either body order | `SimultaneousStrikesKeepTheStrongest` | each alone | — |
| 7 | A ball passing 2 m away in depth rings nothing; a Wall between bell and receiver gives level 0 | `ADepthMissOrAWallGivesNoReception` | row 1 | Wall moved until the path just clears |
| 8 | Reset clears emissions in flight, the interval and the clapper; save then load round-trips `tone` | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`:273`) plus `ResetClearsEmissionsAndRearm` | — | Reset with an emission in flight |
| 9 | `tone` outside 0–2 is refused atomically with a typed reason | `AnUndefinedToneIsRefused` | 0, 1 and 2 accepted | — |
| 10 | Muted and audible runs give identical emissions | `MutingAudioChangesNothing` | — | — |
| 11 | Every drawn solid has its collider and no collider is without art; the bars shown match `tone` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` with a `bell` row; `ToneBarsFollowTheChoice` | — | — |
| 12 | Meter integration: a struck bell 4 m from a supplied Sound meter opens a Powered gate through a Hold timer | `SoundMeterTests.ABellStrikeOpensTheGateThroughTheMeter`, run in Story 14.1 | Wall between; meter unsupplied | — |

There is no port, so no wrong-kind connection case. Requirement criteria: threshold (2); mass and impact dependence (1, 3); omnidirectional (1); separation rearm (4); bounded and strongest retriggering (5, 6); each tone (1, 11); rest, gentle, depth miss and cap controls (1, 2, 5, 7); meter integration (12); artwork and voice adding no impulse (1, 10). Chrome recipe: post-catalogue.

## 7. Integration

Works with every ball and the Weight as strikers (delivered); Wall as occluder (delivered); Sound meter [CAT-060](CAT-060-sound_meter.md), [EL-044](EL-044-tone-selective-sound-meter.md) and the other sound receivers (planned after it). Campaign: first use in levels 71–80, first taught at **75a** with the Sound meter, whose acceptance source it is (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)), reuse 81–100, 117–125, 136–150 ([campaign reservations](../invest/campaign-reservations.md)); no level places it today. Coverage binding: catalogue key `bell`. Lesson-fixture story (S8) only where a level pre-places it.

## 8. Delivery

- **Shape.** One element slice. Data-only review tier if Story 14.0 delivers N1, N2, N12, N13 and N14; otherwise the missing one is an engine slice first.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it) and `ENGINE-CONTACT-OBSERVATIONS` (N14 reads qualified contacts found by index, not by scanning rows); then Story 14.0 amended with gaps 11, 12, 13, 14 and 17 and the omnidirectional pattern; `ENGINE-PARAMETER-BOUND-FRAMES` (the tone bars shown by the `tone` Choice; coordinator decision 16). No other element.
- **Requirement checkbox.** "Meter integration" is row 12, which runs in Story 14.1; the CAT-009 checkbox is ticked by that later slice, not by Story 14.2.
- **Files.** Adds `elements/Bell/` (`Bell.csproj`, `BellElementDeclaration.cs`, `bell.tscn`, `icon.svg`) and `CuriousContraptions.tests/BellTests.cs`; one line in `CuriousContraptions.slnx`; one row in the visible-solid table. Membership tables are derived ([registration derivation](../../../_bmad-output/implementation-artifacts/spec-registration-derivation.md)); until [S3](../../../_bmad-output/implementation-artifacts/spec-s3-remaining-per-package-lists.md) lands it also edits that contract's five test-side lists.
- **Disqualified if** the slice's probe finds no contact-excited emitter, a strength law that needs this package's id, or a Frustum collider that cannot stand for the drawn shell.

## 9. Open questions

Resolved:

1. Threshold quantity: approach speed of 0.8 m/s. The requirement row governs over the story's "kinetic energy" ([D15](../invest/delivery-batches.md#3-family-level-decisions)).
2. Story order: 14.0, then this part, then the Sound meter (owner, 18 October 2026).
3. Rearm: 0.2 s of game time ([D10](../invest/delivery-batches.md#3-family-level-decisions)).
4. Material: the static default ([D19](../invest/delivery-batches.md#3-family-level-decisions)).
5. Art: the hanging bell of [DESIGN.md](../../../DESIGN.md) "Impact bell" ([D9](../invest/delivery-batches.md#3-family-level-decisions)).
6. Wavefront cue: the engine story's (gap 11); the legacy positional tuple is not used.
7. Decided under the standing delegation: strength from striker momentum (one generic contact quantity); a hollow shell with a round crown (the collider shapes that exist); the emitter at the body origin; tone bars (the three tones must be readable).

8. Clapper and shell (owner decision, 22 November 2026): the clapper swings and the shell stays still, and the clapper is shortened to end inside the shell. It is decorative, moved by a binding. [DESIGN.md](../../../DESIGN.md) "Impact bell" is being updated to that form.

Nothing needs the owner.

## Engine conformance

Against the owner's conformance addendum of 22 November 2026 (element-free engine gate in [AGENTS.md](../../../AGENTS.md); design contracts D1–D7 in `_bmad/custom/element-free-engine-design.md`). **Verdict: conforms on paper; buildable once Story 14.0 delivers the emitter and its contact excitation.**

1. **Declaration data only.** A static body, four colliders, one emitter row, one excitation row, one parameter and its presentation bindings. No contact callback, pulse list or cooldown in the part.
2. **Generic laws.** Rebound: the contact law with declared material. Ringing: the contact-excited emitter law (N14), stated without reference to any part and also declared by the Wind chimes. Hearing: the propagation, occlusion and reception law (L1–L7).
3. **Layers.** Colliders: four hidden proxies (Frustum, sphere, Torus, Cylinder) on the static body. Visuals: the nodes of section 3; none is a collider. Animation: the clapper binding and the tone-bar visibility read committed state and write nothing back.
4. **One-way flow and typing.** The swing and the voice read the committed emission; muting changes nothing (row 10). Tone and pattern are enums end to end.
5. **Flat state.** 1 static body; 4 colliders (1 Frustum, 1 sphere, 1 Torus, 1 Cylinder); 0 joints, drives, stores, ports and signal or supply nodes; 1 emitter with 1 excitation row; 1 event kind; 4 presentation bindings (clapper and three bar visibilities) plus the engine's cues. All fixed at compile time.
6. **Performance.** Four colliders, each backing a drawn solid of a different primitive; none is cosmetic. The body is static. Emissions in flight are bounded by the interval and lifetime (at most five). Reception work is Story 14.0's to bound from compiled counts, with no fixed scene ceiling, its candidates coming from `ENGINE-SPATIAL-QUERIES` (see the batch summary). The excitation rows use `ENGINE-CONTACT-OBSERVATIONS`' indexed lookup.
7. **Tests are scenarios.** Rows 1–12 run the production simulation; Story 14.0's own fixtures prove the law without this package.

## Appendix: Historical evidence

Evidence of the legacy part at `a6c914e`, kept so the harvest stays traceable. Nothing here is a requirement, a tolerance or a design instruction; sections 1–9 are the spec. The legacy part collided as a sphere of radius 0.75 m around a smaller drawn shell and ran its own contact callback; neither is the design. Legacy tick counts, decibel values and byte counts in the cited files are history only.

| # | Fact | Citation |
| --- | --- | --- |
| 1 | Legacy collider, threshold, strength, rearm, coalescing and emission lived in the part's contact callback (behaviour restated in section 2) | `parts/BellPart.cs@a6c914e:L32-L85` |
| 2 | Acoustic pulse law and expiry (EL-044 A2–A5) | `engine/Acoustics.cs@a6c914e:L11-L47` |
| 3 | Reception: strongest direct arrival, no self-hearing, opaque blocking (EL-044 A6) | `engine/Acoustics.cs@a6c914e:L49-L80`; `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`; `reference/cpu/MachineWorld.cs@a6c914e:L855-L861` |
| 4 | A legacy cap on acoustic events per tick; the current bound is Story 14.0's, from compiled counts | `reference/cpu/MachineWorld.cs@a6c914e:L22-L22` |
| 5 | A 1 kg ball from any of six axes rang a rotated bell once and gained no energy (row 1) | `CuriousContraptions.tests/BellTests.cs@a6c914e:L31-L59` |
| 6 | Chain: bell, meter, hold timer, powered gate, with Wall and unsupplied controls and Reset (row 12) | `CuriousContraptions.tests/BellTests.cs@a6c914e:L60-L102` |
| 7 | Gentle resting contact was silent; separation rearmed (row 4) | `CuriousContraptions.tests/BellTests.cs@a6c914e:L103-L125` |
| 8 | Below-threshold approach was silent; light and heavy strikers hit the floor and the cap (rows 2, 3) | `CuriousContraptions.tests/BellTests.cs@a6c914e:L126-L143` |
| 9 | Rapid strikes stayed bounded; presentation never changed physics (rows 5, 10) | `CuriousContraptions.tests/BellTests.cs@a6c914e:L144-L175` |
| 10 | Two simultaneous impacts gave one pulse of the greater strength in either order (row 6) | `CuriousContraptions.tests/BellTests.cs@a6c914e:L176-L199` |
| 11 | Undefined pattern, tone or voice and out-of-range strengths were refused | `CuriousContraptions.tests/BellTests.cs@a6c914e:L200-L223` |
| 12 | Hidden rendering still rang; a disabled bell or payload did not | `CuriousContraptions.tests/ContactParticipationTests.cs@a6c914e:L20-L75` |
| 13 | Only committed waves rendered; Reset restored the rings | `CuriousContraptions.tests/AcousticWavefrontTests.cs@a6c914e:L27-L87` |
| 14 | Only committed occurrences moved the art | `CuriousContraptions.tests/AcousticMotionTests.cs@a6c914e:L36-L109` |
| 15 | Playback volume followed strength; staging was silent until publish | `CuriousContraptions.tests/SceneAcousticRunTests.cs@a6c914e:L50-L70` |
| 16 | A failed emission restored queue and cooldown: a faulting-tick mechanism, not carried | `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs@a6c914e:L26-L92` |
| 17 | No level used the bell | `docs/coverage/catalogue-elements.json` (empty fixture list) |
| 18 | Legacy art, voice and catalogue entry | `parts/BellPart.cs@a6c914e:L86-L112`; `engine/AcousticAudio.cs@a6c914e:L11-L31`; `parts/catalog/bell.tres@a6c914e:L5-L11` |

**Files harvested:**
- `parts/BellPart.cs`
- `parts/catalog/bell.tres`
- `parts/scenes/bell.tscn` (no element knowledge: script reference only)
- `engine/Acoustics.cs`
- `engine/AcousticAudio.cs`
- `engine/PartArt.cs` (ring half-width convention)
- `engine/physics/BodyQueryGeometry.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/BellTests.cs`
- `CuriousContraptions.tests/ContactParticipationTests.cs`
- `CuriousContraptions.tests/AcousticWavefrontTests.cs`
- `CuriousContraptions.tests/AcousticMotionTests.cs`
- `CuriousContraptions.tests/SceneAcousticRunTests.cs`
- `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs`
- `CuriousContraptions.tests/AcousticWavefrontBindingTests.cs` (presentation-binding validation for any acoustic source; no bell-specific values)
- `CuriousContraptions.tests/AcousticMotionBindingTests.cs` (fan-out preflight and invalid occurrence rejection; no bell-specific values)
