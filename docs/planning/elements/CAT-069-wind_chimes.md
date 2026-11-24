# CAT-069 · wind_chimes — element spec

**Current delivery (24 October 2026):** [CAT-069 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

One current text, repaired 22 November 2026 after the Batch D audit, against checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; citations with `@a6c914e` are the legacy harvest and appear only in the closing appendix. Values marked *proposed* are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). The shared acoustic facts, laws (L1–L7), capability rows (C1–C13, N1–N13), tolerances and Story 14.0 gaps 1–16 are in [EL-044](EL-044-tone-selective-sound-meter.md); the contact-excited emitter **N14** (gap 17) is defined in [CAT-009 Bell](CAT-009-bell.md#4-engine-capabilities).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-069 · `wind_chimes` (catalogue title "Wind chimes") |
| Requirement anchor | [CAT-069](../requirements.md#current-cat-069) with [todo-348](../requirements.md#todo-348); owner key [CAT-069-I](../invest/current-consumers.md#cat-069-i) |
| Modes | AcousticTone = Low, Mid, High, fixed per tube |
| Disposition | To deliver as a single element in Story 14.4, the last of Epic 14. No named identity maps to it |
| Planned package | id and catalogue key `wind_chimes`, directory `elements/WindChimes/`, category `ElementCategory.Control`. Palette order: 426 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)) |
| Capability row | [general-engine-element-map, CAT-069](../general-engine-element-map.md) |
| Status | Not started; blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-CONTACT-OBSERVATIONS` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-JOINT-KINDS` (spherical joint), Story 14.0 and `ENGINE-RUN-LENGTH` (row 8) |

## 2. Declaration

Closest delivered shapes: the Impact lever for a dynamic body jointed to a static root (`IHingeElement`, `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), the Tennis ball for an airflow receiver (`elements/TennisBall/TennisBallElementDeclaration.cs:27`–`:30`) and the planned Bell for contact-excited emitters. Interfaces: `IMechanicalElement` (`MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IAirflowReceiverElement` (`MechanicalContracts.cs:45`), `IAmbientLoadElement` (`MechanicalContracts.cs:124`), a spherical-joint interface (none exists) and the acoustic emitter interface Story 14.0 adds.

| Item | Declaration |
| --- | --- |
| Body 0, static | cap: cylinder r 0.6 m, 0.12 m high, at (0, 0.93, 0); knob: sphere r 0.1 m at (0, 1.06, 0); four tubes, cylinders r 0.065 m on vertical axes (table below); four hanger rods, cylinders r 0.02 m, each from its tube's top at y 0.65 to the cap's underside at y 0.87. Material restitution 1, friction 0.2, rolling resistance 0, threshold 0.08 m/s |
| Body 1, dynamic pendulum | hung from the pivot (0, 0.9, 0): rod, cylinder r 0.02 m from 0.05 m to 1.3 m below the pivot; clapper, sphere r 0.12 m at 0.9 m below; sail, box 0.3 × 0.4 × 0.055 m at 1.5 m below, turned 35° about Z. Mass 0.35 kg (clapper 0.2, sail 0.15, rod taken as massless); centre of mass 1.157 m below the pivot; principal moments about (0.034, 0.0023, 0.034) kg·m² (*proposed*; the slice recomputes with the sail's tilt). Material restitution 0.35, friction 0.2, rolling resistance 0, threshold 0.08 m/s |
| Joint | one spherical joint at the pivot between the two bodies, their mutual collision disabled, with a small declared bearing damping (0.02 N·m·s/rad, *proposed*) so spin about the rod dies away |
| Ambient load | linear drag 0.9 /s on the pendulum, scaled by the atmosphere as the balls' drag is (`elements/Ball/BallElementDeclaration.cs:59`) |
| Airflow receiver | one, on the sail's collider, `AirflowSampling.LocalPosition` (`CuriousContraptions.Abstractions/AirflowValues.cs:18`), one sample at the sail's centre, weight 1 |
| Emitters | four, one at each tube's centre: Pulse, Omnidirectional, speed 12 m/s, range 8 m, duration 0.15 s, tone fixed per tube. Ids in the order Right, Left, Front, Back |
| Excitation (N14) | only the four tube colliders excite, each by exact collider reference; the cap, knob, hanger and pendulum colliders never do. Per tube, two rows. By this part's own pendulum (`BodyTargetKind.NamedBody`, `CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:31`): approach at or above 0.08 m/s, strength = clamp(m × v / 0.525, 0.08, 1), which is v / 1.5 for the 0.35 kg pendulum. By any payload (`AllPayloads`): approach at or above 0.8 m/s, strength = clamp(m × v / 1.5, 0.08, 1). The pendulum is not a payload, so the two rows can never both match one strike. One minimum interval of 0.2 s across the four emitters; strikes committed in one tick give one emission, the strongest, ties to the lower emitter id |
| Footprint | 2 bodies, 1 moving, 1 spherical joint. No ports, no parameters, no stores |

| Tube | Centre (m) | Length (m) | Tone |
| --- | --- | --- | --- |
| Right | (0.4, 0.2, 0) | 0.9 | Mid |
| Left | (−0.4, 0.075, 0) | 1.15 | Low |
| Front | (0, 0.275, 0.4) | 0.75 | High |
| Back | (0, 0.15, −0.4) | 1.0 | Mid |

Every tube's top is at y 0.65, so the four hang at one height on equal rods and differ in length below. The tubes hang on thin dark solid rods (owner decision, 22 November 2026). Extending the tubes up to the cap instead would have removed the four rod colliders, and was set aside because the owner chose the hanging-rod form. Presentation: `Visual/Pendulum` is the physical pendulum body; wavefront rings and the bell voice are the engine story's cues. Catalogue: title "Wind chimes", colour gold (0.91, 0.72, 0.39); description: "Aim a fan at the cyan sail. Wind swings the clapper; a tube sounds only when it is struck. A moving ball can strike the tubes too. Sound spreads in every direction. No electrical input."

## 3. Art and solidity

| Node | Mesh and colour | Class | Moved by |
| --- | --- | --- | --- |
| `Cap` | cylinder, cream `#fff8e9` | solid: Cylinder collider | nothing |
| `Knob` | sphere, navy `#293954` | solid: sphere collider | nothing |
| four `Tube`s | cylinders, gold `#e8b764` | solid: Cylinder colliders | nothing |
| tube foot bands | navy bands printed flush on each tube | decorative: printed flat | nothing |
| four `Hanger`s | rods r 0.02 m, 0.22 m long, navy | solid: Cylinder colliders | nothing |
| `Pendulum/Rod` | rod r 0.02 m, navy | solid: Cylinder collider on the pendulum | the pendulum body's pose |
| `Pendulum/Clapper` | sphere, cream | solid: sphere collider on the pendulum | the pendulum body's pose |
| `Pendulum/Sail` | box, cyan `#66b8c9` | solid: box collider on the pendulum | the pendulum body's pose |
| wavefront rings | thin, translucent, shadow-free | decorative: glow | engine cue |

The four hangers and the pendulum line are thin dark solid rods, each with its collider (owner decisions 1 and 4, 22 November 2026: thick strands and rods are solid; only the Balloon's string and thin wires stay non-solid). [DESIGN.md](../../../DESIGN.md) "Wind chimes" is being updated to that form. Icon: hanging tubes and a diamond sail. At a glance it differs from the Bell by four tubes under a round cap with a sail hanging below.

## 4. Engine capabilities

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body (C1) and Cylinder colliders (C10) | present | EL-044 C1, C10 |
| Generic lifecycle (C9) | present | EL-044 C9 |
| Ambient linear drag on a dynamic body | present | `IAmbientLoadElement`; `BalloonTests.ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment` (`CuriousContraptions.tests/BalloonTests.cs:171`) |
| An airflow receiver with a local-position sample on a jointed body | present, untested | `AirflowReceiverDescriptor` (`AirflowValues.cs:50`); local-position sampling is exercised only by law-scene fixtures on free bodies (`CuriousContraptions.tests/LawScene.Airflow.cs:72`) |
| A dynamic body jointed to a static root | narrower | `ENGINE-JOINT-KINDS` (the spherical joint below; coordinator decision 16): one axis only today, `IHingeElement`; `ImpactLeverTests.AsymmetricLoad_TurnsTheBeamToItsStopAboutTheAuthoredPivot` (`CuriousContraptions.tests/ImpactLeverTests.cs:165`) |
| Event cue presentation (C12) | narrower | Story 14.0: no acoustic event kind today (N12) |
| A spherical joint with bearing damping, whose moving body is a declared child body everywhere a slider's or hinge's is | missing | `ENGINE-JOINT-KINDS` (owner decision 3a; coordinator decision 16 assigns the spherical joint to it). Today a child body is only a slider's or a hinge's: the ambient-load admission (`CuriousContraptions.Bundle/GasCouplingDomain.cs:77`–`:78`) and the art test's moving-collider pool (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:218`–`:220`) both enumerate those two; the story's text is proposed in the [batch summary](readiness/batch-d.md). [EL-115 Linkage connector](EL-115-linkage-connector.md) is listed as a second consumer by the engine audit, so it is not a capability for this part alone |
| Emission, propagation, occlusion with an omnidirectional pattern (N1); tone band (N2) | missing | Story 14.0 |
| Contact-excited emitters (N14), here with a named-body target, several emitters in one interval group and a tie-break by emitter id | missing | Story 14.0, gap 17 |
| Acoustic event, wavefront cue, presentation-only audio (N12) | missing | Story 14.0, gap 11 |
| Committed acoustic read model (N13) | missing | Story 14.0, gap 14 |

Counts: 3 present, 1 untested, 2 narrower, 5 missing rows. Angular drag does not exist and is not needed: linear drag damps the swing and the joint's bearing damping damps spin. The joint slice's probe should first check whether a short declared tensile span gives the same pendulum from a delivered capability; that was not verified here.

## 5. Behaviour and numbers

- **Clearance.** The clapper's surface is 0.4 − 0.12 − 0.065 = 0.215 m from each tube, a swing of 13.8°.
- **Wind needed.** Holding that swing statically takes about 0.65 N at the sail. A default Fan gives 9 N (25.7 m/s² on 0.35 kg, inside the 64 m/s² bound), so it rings at once; 0.01 N deflects the pendulum by a fraction of a degree and never rings.
- **A steady jet rings once.** It pins the clapper against a tube; resting contact has no approach speed, so nothing more sounds until the pendulum separates and strikes again. Overlap with a jet never rings by itself.
- **Coast.** The pendulum's period is about 2.2 s; with 0.9 /s drag its swing decays by a factor e every 2.2 s, so it is still within about 15 s of the wind stopping. The stall guard for coming to rest is 30 s.
- **Strength.** A clapper strike at 1.5 m/s or more is full strength, heard by a default Sound meter to 6.1 m; the 0.08 floor is heard only by a sensitive meter nearby.
- **Emission origin** is the struck tube's centre, a declared point.
- **Tick order (acoustic, Story 14.0's to confirm).** The contact commits in the mechanical substeps of tick t; the emission is created in the acoustic field phase of tick t + 1 (`docs/gpu-f32-physics.md:165`–`:171` draws it as Phase 2); a meter's pulse and contact follow by EL-044 L7. The requirement's "next-tick playback" is exactly this: the voice starts from the committed emission. This is the acoustic phase, not the optical solve ([EL-176 section 10](EL-176-red-laser-emitter.md#optical-conventions), item 4).

## 6. Acceptance

Planned classes `WindChimesTests` and `ChimesIntegrationTests`. Emissions and strengths are read through the committed acoustic read model (N13). Tolerances T-level and the 5 s stall guard are EL-044's unless a row states its own.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | A default Fan 3 m upstream aimed at the sail and a supplied Sound meter 3 m away with a Counter: at least one emission with a tube's tone, and the Counter counts it | `ChimesIntegrationTests.AFanRingsTheChimesAndTheMeterCounts` | fan off; 0.01 N; fan 2 m off in depth; Wall in the jet; two equal opposed fans: no emission in 10 s | fan and chimes yawed together by 0°, 90°, 180°, 270° |
| 2 | A jet that covers the sail from the first tick gives no emission until the clapper touches; held against a tube for 10 s it has rung once | `WindChimesTests.ASteadyJetRingsOnceThenRestsSilently` | row 1 | — |
| 3 | A ball driven into each tube in turn sounds that tube's tone | `WindChimesTests.EachTubeSoundsItsOwnTone` | a ball passing 2 m away in depth: silent | all four tubes |
| 4 | A 1 kg ball at 5 m/s rings a tube and rebounds by contact alone | `WindChimesTests.ABallStrikeRingsAboveItsThreshold` | cap miss: the same ball onto the cap, and onto a hanger rod, is silent | 0.75 m/s silent, 0.85 m/s rings |
| 5 | The part is placed tilted (row 10's authored pose) so the pendulum swings into a tube on Run: the emission's strength equals the committed approach speed ÷ 1.5 within T-level | `WindChimesTests.ClapperStrengthFollowsApproachSpeed` | upright and calm: no emission | a smaller tilt giving an approach of about 0.09 m/s rings at the 0.08 floor; one giving less than 0.08 m/s is silent |
| 6 | A 0.1 kg ball on Left and a 1 kg ball on Right at 6 m/s in one tick: one emission, strength 1, tone Mid, in either body order | `WindChimesTests.SimultaneousStrikesKeepTheStrongestAndTieByTube` | each alone | equal strikes on Left and Right: Right sounds |
| 7 | Under a gusting fan for 5 s, successive emissions are at least 0.2 s apart within one tick and at most five are in flight | `WindChimesTests.EmissionsAreRateLimitedAcrossTubes` | — | — |
| 8 | After the fan stops the sail's speed falls below 0.05 m/s within 30 s with no further emission; restarting the fan rings again. Run length 60 s (wind, the 30 s calm guard, wind again), set per level by `ENGINE-RUN-LENGTH` (owner decisions 13 and 17; the default is 30 s) | `WindChimesTests.ThePendulumRestsInCalmAirAndRingsAgainWhenWindReturns` | — | — |
| 9 | Through 10 s of ringing, upright and tilted, the sail's centre stays 1.5 m ± 0.01 m from the pivot | `WindChimesTests.ThePendulumKeepsItsLength` (0.01 m is 5 % of the strike clearance) | — | tilted assembly |
| 10 | Tilting the part in Build keeps the authored hanging pose until Run; Reset restores it and clears emissions; save then load round-trips placement | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) plus `WindChimesTests.ResetRestoresTheHangingPendulum` | — | Reset in mid-swing |
| 11 | A Bellows burst rings the chimes; a Wall in the jet silences them | `ChimesIntegrationTests.BellowsBurstRingsOnlyClearReceiver` (the Bellows row B11) | blocked path | — |
| 12 | Muted and audible runs give identical emissions; the voice starts from the committed emission record | `WindChimesTests.MutingAudioChangesNothing` | — | — |
| 13 | Every drawn solid has its collider, the pendulum's colliders included | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` with a `wind_chimes` row | — | — |

There is no parameter and no port, so no parameter refusal or wrong-kind connection case. Requirement criteria: constrained 3D sail and clapper (9); four identified tubes and their tones (3); separation rearm and no ring from overlap or a resting jet (2); strongest and tie by identity (6); pulse cap (7); playback after the committed strike (12); the direct ball strike path (4); weak, blocked, opposed and no-air controls (1); cap miss (4) and depth miss (3); art following paid physical motion (8, 10). Chrome recipe: post-catalogue.

## 7. Integration

Works with Fan, every ball, Wall, Battery, Counter (delivered); Bellows (Story 12.4, in flight: row 11 stays open until it is delivered); Sound meter and Bell (planned before it). Campaign: first use in levels 71–80, first taught at **75b**, after the Bell and Sound meter (75a) (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); the "Wind in the Tower" recipe ([component research](../../component-research.md#sound)); no level places it today. Coverage binding: catalogue key `wind_chimes`. Lesson-fixture story (S8) only where a level pre-places it.

## 8. Delivery

- **Shape.** One element slice. Data-only review tier only if both engine pieces below are delivered first.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; in particular `ENGINE-SPATIAL-QUERIES` (airflow and reception candidates), `ENGINE-CONTACT-OBSERVATIONS` (N14's qualified contacts found by index) and `ENGINE-ISLANDS-SLEEP` (a calm pendulum); then `ENGINE-JOINT-KINDS` (spherical joint) before its first consumer; Story 12.2 (Fan, delivered); Story 14.0 amended with gaps 11, 12, 13, 14 and 17 and the omnidirectional pattern; `ENGINE-RUN-LENGTH` for row 8; Stories 14.2 and 14.1. Row 11 needs Story 12.4.
- **Files.** Adds `elements/WindChimes/` (`WindChimes.csproj`, `WindChimesElementDeclaration.cs`, `wind_chimes.tscn`, `icon.svg`) and `WindChimesTests.cs`, `ChimesIntegrationTests.cs` under `CuriousContraptions.tests/`; one line in `CuriousContraptions.slnx`; one row in the visible-solid table. Membership tables are derived ([registration derivation](../../../_bmad-output/implementation-artifacts/spec-registration-derivation.md)); until [S3](../../../_bmad-output/implementation-artifacts/spec-s3-remaining-per-package-lists.md) lands it also edits that contract's five test-side lists.
- **Disqualified if** the slice's probe finds no spherical joint, an airflow receiver or ambient load that is not admitted on that joint's body, or emitter grouping that needs this package's id. Each is an engine slice first.

## 9. Open questions

Resolved:

1. Strike model: a clapper strikes four fixed tubes. The requirement row governs over the story's "tubes strike each other" ([D15](../invest/delivery-batches.md#3-family-level-decisions)).
2. Angular drag: none is added; linear drag and bearing damping do its job.
3. Tube collider: a true Cylinder collider.
4. Rearm: 0.2 s of game time ([D10](../invest/delivery-batches.md#3-family-level-decisions)).
5. Wavefront cue: the engine story's (gap 11); the legacy positional tuple is not used.
6. Decided under the standing delegation: the emission origin is the tube's centre; one strength law with two reference momenta chosen by a generic body target set; ambient drag in place of body drag; the cap's collider is the drawn disc; tube foot bands printed flush.
7. Hangers and pendulum line: thin dark solid rods of radius 0.02 m (owner decision, 22 November 2026).

Nothing needs the owner.

## Engine conformance

Against the owner's conformance addendum of 22 November 2026 (element-free engine gate in [AGENTS.md](../../../AGENTS.md); design contracts D1–D7 in `_bmad/custom/element-free-engine-design.md`). **Verdict: conforms on paper; buildable after the engine programme, once Story 14.0 and `ENGINE-JOINT-KINDS`' spherical joint exist.**

1. **Declaration data only.** Two bodies, their colliders, one joint, one ambient load, one airflow receiver, four emitter rows with eight excitation rows, and no code. No assembly helper, per-substep drag call or contact callback.
2. **Generic laws.** Swing: rigid-body dynamics under gravity, a spherical joint and the ambient drag law. Wind: the conical airflow law on a declared receiver sample. Strike: the contact law between the pendulum's colliders and the tubes. Ringing, which tube sounds and the tie-break: the contact-excited emitter law (N14), with exact collider references, target sets, one interval group and ordering by emitter id as declared data.
3. **Layers.** Colliders: thirteen hidden proxies, ten on the static body (cap, knob, four tubes, four hangers) and three on the pendulum (rod, clapper, sail). Visuals: the nodes of section 3; none is a collider. Animation: none of its own; rings and voice are engine cues that read the committed emission.
4. **One-way flow and typing.** Nothing presentational feeds physics (row 12). Tones are an enum; the tube order is emitter ids, not names compared as strings.
5. **Flat state.** 2 bodies (1 static, 1 dynamic); 13 colliders (10 cylinders, 2 spheres, 1 box); 1 spherical joint; 0 drives, stores, ports and signal or supply nodes; 1 ambient load; 1 airflow receiver with 1 sample; 4 emitters in 1 interval group with 8 excitation rows; 1 event kind; 0 presentation bindings and 1 body-pose mapping. All fixed at compile time.
6. **Performance.** Thirteen colliders, four more than the Windmill. Eight are surfaces that are struck or strike (cap, knob, four tubes, clapper, sail); the other five are the four hangers and the pendulum rod, which are solid because drawn rods are solid (owner decision, 22 November 2026), not for looks. Ten of the thirteen are on a static body. The pendulum comes to rest in calm air; island sleeping is not implemented (`docs/gpu-f32-physics.md:273` describes it), so a resting pendulum is still stepped. Owned by `ENGINE-ISLANDS-SLEEP`. Emissions in flight are bounded by the group's interval and lifetime. Reception work is Story 14.0's to bound from compiled counts, with candidates from `ENGINE-SPATIAL-QUERIES` (see the batch summary).
7. **Tests are scenarios.** Rows 1–13 run the production simulation; the joint and emitter stories prove their laws on declarations that are not this package.

## Appendix: Historical evidence

Evidence of the legacy part at `a6c914e`, kept so the harvest stays traceable. Nothing here is a requirement, a tolerance or a design instruction; sections 1–9 are the spec. The legacy part built its pendulum in an assembly helper, applied drag every substep, chose the sounding tube in a contact callback and drew strings without colliders; none of that is the design. Legacy tick counts and error bounds in the cited files are history only.

| # | Fact | Citation |
| --- | --- | --- |
| 1 | Legacy geometry, masses, materials, ball-socket joint, drag, air sample and contact rules (values kept are in section 2) | `engine/ChimeAssembly.cs@a6c914e:L8-L72`; `parts/WindChimesPart.cs@a6c914e:L39-L129` |
| 2 | Fan, chimes, supplied meter and counter: wind rang the chimes; weak or no wind, a depth miss, a Wall and an unpowered fan did not; overlap alone never rang; Reset and load (rows 1, 2, 10) | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L46-L98` |
| 3 | Fan and chimes rang in four yaws (row 1) | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L99-L124` |
| 4 | Tilting the part while editing kept the authored rest pose until Run (row 10) | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L125-L144` |
| 5 | After the fan stopped the pendulum coasted to rest with no further pulses; restarting rang again (row 8) | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L145-L168` |
| 6 | A ball struck into the tubes rang them; one passing in depth did not (rows 3, 4) | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L169-L190` |
| 7 | A tilted assembly kept its pendulum length and still rang (row 9) | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L280-L302` |
| 8 | Two opposed fans cancelled; zero pressure gave zero air force (row 1) | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L303-L319` |
| 9 | Two tubes struck in one tick gave one pulse of the stronger strike, with the tie going by tube identity (row 6) | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L320-L343` |
| 10 | Undefined tube identities were refused | `CuriousContraptions.tests/WindChimeTests.cs@a6c914e:L356-L372` |
| 11 | A bellows burst rang the chimes; a blocking Wall silenced them (row 11) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L455-L490` |
| 12 | The air sample followed the solved pendulum, not the rendered sail | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L285-L320` |
| 13 | Only committed waves rendered | `CuriousContraptions.tests/AcousticWavefrontTests.cs@a6c914e:L27-L87` |
| 14 | A failed emission restored queue and cooldown: a faulting-tick mechanism, not carried | `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs@a6c914e:L26-L92` |
| 15 | Lesson recipe "Wind in the Tower": fan swings the sail, clapper strikes, meter triggers | [component research, sound puzzle recipes](../../component-research.md#sound) |
| 16 | No level used the chimes | `docs/coverage/catalogue-elements.json` (empty fixture list) |
| 17 | Legacy art, voice and catalogue entry | `parts/WindChimesPart.cs@a6c914e:L130-L162`; `engine/AcousticAudio.cs@a6c914e:L11-L31`; `parts/catalog/wind_chimes.tres@a6c914e:L5-L11` |

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
