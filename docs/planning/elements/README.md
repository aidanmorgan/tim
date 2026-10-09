# Element declaration readiness specs

One full declaration readiness spec per puzzle element, written by [Story 7.0](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md) before Epic 7 deletes the legacy that holds element knowledge. Each spec lets an implementer write the element's story spec without opening any file Epic 7 deletes. The [requirements rows](../requirements.md#current-catalogue-closure) stay the acceptance authority; the [named identities](../invest/named-elements.md) stay the identity ledger; the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap) governs order. The [legacy disposition ledger](legacy-disposition.md) accounts for every file Epic 7 deletes.

The index has three groups, laid out by batch letter:

1. [Catalogue specs](#catalogue-specs): the 72 CAT specs (`CAT-NNN-<kind>.md`), batches A–E.
2. [Named-identity specs](#named-identity-specs): 276 specs, one per named puzzle-element identity (`EL-NNN-<slug>.md`, `TH-NN-<slug>.md`, `RAD-NN-<slug>.md`, `GAP-NN-<slug>.md`), batches F–O.
3. [Index-only entries](#index-only-entries): 17 rows (10 umbrella scope-index entries and 7 GAP product features), which get a row but no spec.

## How to read a spec

Every spec has six sections, in this order:

1. **Identity.** CAT ID or named identity, kind, requirement anchor, mapped identities, roadmap story (epic.story) and status: delivered, partial or not started. A named-identity spec also names the CAT spec it refines or extends, where one exists.
2. **Declaration.** Bodies and shapes (metres), mass and material, constraints and joints, typed sockets and ports, sensors and activation, work and energy stores, parameters (type, range, default, unit), cosmetic curves and UI bindings, art (scene, mesh, palette tokens), catalogue and inventory entry. Each item gives a value with its source, or "none" with a reason. Current declaration types are cited where they exist.
3. **Engine capabilities.** The capability families from the [general-engine element map](../general-engine-element-map.md) row, split into "exists now" (with a file reference) and "missing" (with the story that builds it), plus element dependencies.
4. **Legacy harvest.** One row per behaviour fact, value, edge case or boundary found in the legacy, each with a pinned citation and a disposition:
   - **carry forward**: the fact is behaviour or data the element must keep;
   - **do not carry forward**: the fact is a legacy mechanism that conflicts with current contracts (f16 values, a CPU solver path, a per-element update loop, a proof-grade certificate, a string-typed closed set), with the reason.

   The section ends with a **Files harvested** list of exact paths; the ledger is compiled from these lists.
5. **Acceptance outline.** Actual Chrome UI construction recipe, positive behaviour, meaningful negative or control, boundaries, Run/Reset restoration, Save/Load and integrations. It points to the requirement row rather than restating it.
6. **Open questions.** Owner decisions the legacy and the requirements leave unsettled, written as "unspecified — owner decision", or "none".

**Citations.** `path@a6c914e:Lstart-Lend` resolves with `git show a6c914e:<path> | sed -n 'start,endp'` and stays retrievable from git history after the purge. Current (surviving) declarations are cited the same way so the line ranges are stable. Untracked build output is not harvested.

**Values.** Legacy values are recorded as found. The current numeric contract is [canonical IEEE-754 f32](../../gpu-f32-physics.md); a value stored as binary16 in a legacy or current declaration is carried forward as its decimal meaning, never as binary16 bits.

**Story numbers.** Roadmap story numbers in this index and in every spec follow the owner's 9 Oct 2026 roadmap reorder; the old-to-new mapping is in [owner reordering dependencies](../invest/vertical-delivery.md#owner-reordering-dependencies). Story 10.1 is ENGINE-CYLINDER.

**Status.** All 348 specs and the closing index/ledger have fresh independent scoped documentation review in [review-7-0-resumed](../../../_bmad-output/implementation-artifacts/review-7-0-resumed.md), including supporting full named-identity coverage. **done** means declaration readiness is reviewed, not that the element is implemented or runtime-qualified. Original historical batch summaries were not reused as proof. "Refines CAT" names the CAT spec the identity refines or extends; related or sibling parts alone do not count.

## Catalogue specs

72 specs, one per CAT ID.

### Batch A: index, ledger, delivered, Epic 6 and unscheduled elements (16)

| CAT | Kind | Spec | Roadmap story | Spec status |
| --- | --- | --- | --- | --- |
| CAT-001 | ball | [CAT-001-ball](CAT-001-ball.md) | delivered (CAT-001-I; Epics 1–2) | done |
| CAT-002 | ball_detector | [CAT-002-ball_detector](CAT-002-ball_detector.md) | 6.8 | done |
| CAT-004 | basket | [CAT-004-basket](CAT-004-basket.md) | delivered (CAT-004-I; Story 3.1) | done |
| CAT-014 | bowling | [CAT-014-bowling](CAT-014-bowling.md) | 6.1 (delivered) | done |
| CAT-015 | bumper | [CAT-015-bumper](CAT-015-bumper.md) | 6.2, 6.3 (partial) | done |
| CAT-022 | delay | [CAT-022-delay](CAT-022-delay.md) | delivered (Story 4.2 cosmetic) | done |
| CAT-023 | domino | [CAT-023-domino](CAT-023-domino.md) | 5.1, 5.2 (delivered; offset centre of mass open) | done |
| CAT-030 | funnel | [CAT-030-funnel](CAT-030-funnel.md) | 6.11 | done |
| CAT-035 | lamp | [CAT-035-lamp](CAT-035-lamp.md) | delivered (CAT-035-I) | done |
| CAT-048 | pipe | [CAT-048-pipe](CAT-048-pipe.md) | 6.6, 6.7 | done |
| CAT-049 | pipe_bend_45 | [CAT-049-pipe_bend_45](CAT-049-pipe_bend_45.md) | 6.9 | done |
| CAT-050 | pipe_bend_90 | [CAT-050-pipe_bend_90](CAT-050-pipe_bend_90.md) | 6.10 | done |
| CAT-054 | ramp | [CAT-054-ramp](CAT-054-ramp.md) | delivered (CAT-054-I; Story 1.4) | done |
| CAT-062 | spring | [CAT-062-spring](CAT-062-spring.md) | 6.4, 6.5 | done |
| CAT-063 | switch | [CAT-063-switch](CAT-063-switch.md) | delivered (CAT-063-I; electrical pass-through open) | done |
| CAT-066 | wall | [CAT-066-wall](CAT-066-wall.md) | delivered (Stories 1.4, 1.5) | done |

### Batch B: Epics 8 and 9 (11)

| CAT | Kind | Spec | Roadmap story | Spec status |
| --- | --- | --- | --- | --- |
| CAT-005 | battery | [CAT-005-battery](CAT-005-battery.md) | 8.1 | done |
| CAT-013 | both_gate | [CAT-013-both_gate](CAT-013-both_gate.md) | 9.6 | done |
| CAT-017 | clock | [CAT-017-clock](CAT-017-clock.md) | 9.3 | done |
| CAT-020 | counter | [CAT-020-counter](CAT-020-counter.md) | 9.2 | done |
| CAT-024 | electrical_nand | [CAT-024-electrical_nand](CAT-024-electrical_nand.md) | 9.7 | done |
| CAT-025 | electrical_nor | [CAT-025-electrical_nor](CAT-025-electrical_nor.md) | 9.7 | done |
| CAT-026 | electrical_or | [CAT-026-electrical_or](CAT-026-electrical_or.md) | 9.8 | done |
| CAT-027 | electrical_xor | [CAT-027-electrical_xor](CAT-027-electrical_xor.md) | 9.8 | done |
| CAT-033 | hold_timer | [CAT-033-hold_timer](CAT-033-hold_timer.md) | 9.5 | done |
| CAT-037 | latch | [CAT-037-latch](CAT-037-latch.md) | 9.4 | done |
| CAT-052 | pressure_plate | [CAT-052-pressure_plate](CAT-052-pressure_plate.md) | 9.1 | done |

### Batch C: Epics 10 and 11 (14)

| CAT | Kind | Spec | Roadmap story | Spec status |
| --- | --- | --- | --- | --- |
| CAT-007 | beam_shutter | [CAT-007-beam_shutter](CAT-007-beam_shutter.md) | 13.3 | done |
| CAT-016 | cannon | [CAT-016-cannon](CAT-016-cannon.md) | 11.4 | done |
| CAT-018 | clutch | [CAT-018-clutch](CAT-018-clutch.md) | 11.2 | done |
| CAT-019 | conveyor | [CAT-019-conveyor](CAT-019-conveyor.md) | 11.1 | done |
| CAT-034 | impact_lever | [CAT-034-impact_lever](CAT-034-impact_lever.md) | 10.3 | done |
| CAT-039 | linear_pusher | [CAT-039-linear_pusher](CAT-039-linear_pusher.md) | 11.3 | done |
| CAT-042 | motor | [CAT-042-motor](CAT-042-motor.md) | 11.1 | done |
| CAT-051 | powered_gate | [CAT-051-powered_gate](CAT-051-powered_gate.md) | 8.2 | done |
| CAT-053 | pulley | [CAT-053-pulley](CAT-053-pulley.md) | 10.2 | done |
| CAT-057 | reverse_transmission | [CAT-057-reverse_transmission](CAT-057-reverse_transmission.md) | 11.2 | done |
| CAT-058 | rope_anchor | [CAT-058-rope_anchor](CAT-058-rope_anchor.md) | 10.2 | done |
| CAT-065 | trampoline | [CAT-065-trampoline](CAT-065-trampoline.md) | 10.5 | done |
| CAT-067 | weight | [CAT-067-weight](CAT-067-weight.md) | 10.4 | done |
| CAT-071 | wound_spring | [CAT-071-wound_spring](CAT-071-wound_spring.md) | 11.4 | done |

### Batch D: Epics 12 and 14 (9)

| CAT | Kind | Spec | Roadmap story | Spec status |
| --- | --- | --- | --- | --- |
| CAT-003 | balloon | [CAT-003-balloon](CAT-003-balloon.md) | 12.1 | done |
| CAT-009 | bell | [CAT-009-bell](CAT-009-bell.md) | 14.2 | done |
| CAT-010 | bellows | [CAT-010-bellows](CAT-010-bellows.md) | 12.4 | done |
| CAT-028 | fan | [CAT-028-fan](CAT-028-fan.md) | 12.2 | done |
| CAT-060 | sound_meter | [CAT-060-sound_meter](CAT-060-sound_meter.md) | 14.1 | done |
| CAT-061 | speaker | [CAT-061-speaker](CAT-061-speaker.md) | 14.3 | done |
| CAT-064 | tennis | [CAT-064-tennis](CAT-064-tennis.md) | 12.1 | done |
| CAT-069 | wind_chimes | [CAT-069-wind_chimes](CAT-069-wind_chimes.md) | 14.4 | done |
| CAT-070 | windmill | [CAT-070-windmill](CAT-070-windmill.md) | 12.3 | done |

### Batch E: Epic 13 (22)

| CAT | Kind | Spec | Roadmap story | Spec status |
| --- | --- | --- | --- | --- |
| CAT-006 | beam_combiner | [CAT-006-beam_combiner](CAT-006-beam_combiner.md) | 13.5 | done |
| CAT-008 | beam_splitter | [CAT-008-beam_splitter](CAT-008-beam_splitter.md) | 13.5 | done |
| CAT-011 | blue_filter | [CAT-011-blue_filter](CAT-011-blue_filter.md) | 13.4 | done |
| CAT-012 | blue_receiver | [CAT-012-blue_receiver](CAT-012-blue_receiver.md) | 13.2 | done |
| CAT-021 | cyan_receiver | [CAT-021-cyan_receiver](CAT-021-cyan_receiver.md) | 13.6 | done |
| CAT-029 | flashlight | [CAT-029-flashlight](CAT-029-flashlight.md) | 13.1 | done |
| CAT-031 | green_filter | [CAT-031-green_filter](CAT-031-green_filter.md) | 13.4 | done |
| CAT-032 | green_receiver | [CAT-032-green_receiver](CAT-032-green_receiver.md) | 13.2 | done |
| CAT-036 | laser | [CAT-036-laser](CAT-036-laser.md) | 13.1 | done |
| CAT-038 | light_receiver | [CAT-038-light_receiver](CAT-038-light_receiver.md) | 13.2 | done |
| CAT-040 | magenta_receiver | [CAT-040-magenta_receiver](CAT-040-magenta_receiver.md) | 13.6 | done |
| CAT-041 | mirror | [CAT-041-mirror](CAT-041-mirror.md) | 13.5 | done |
| CAT-043 | optical_and | [CAT-043-optical_and](CAT-043-optical_and.md) | 13.8 | done |
| CAT-044 | optical_nand | [CAT-044-optical_nand](CAT-044-optical_nand.md) | 13.8 | done |
| CAT-045 | optical_nor | [CAT-045-optical_nor](CAT-045-optical_nor.md) | 13.8 | done |
| CAT-046 | optical_or | [CAT-046-optical_or](CAT-046-optical_or.md) | 13.8 | done |
| CAT-047 | optical_xor | [CAT-047-optical_xor](CAT-047-optical_xor.md) | 13.8 | done |
| CAT-055 | red_filter | [CAT-055-red_filter](CAT-055-red_filter.md) | 13.4 | done |
| CAT-056 | red_receiver | [CAT-056-red_receiver](CAT-056-red_receiver.md) | 13.2 | done |
| CAT-059 | solar_panel | [CAT-059-solar_panel](CAT-059-solar_panel.md) | 13.7 | done |
| CAT-068 | white_receiver | [CAT-068-white_receiver](CAT-068-white_receiver.md) | 13.6 | done |
| CAT-072 | yellow_receiver | [CAT-072-yellow_receiver](CAT-072-yellow_receiver.md) | 13.6 | done |

## Named-identity specs

276 specs, one per named puzzle-element identity in [named-elements.md](../invest/named-elements.md): 216 EL, 37 TH, 19 RAD (RAD-01..04, 06..10, 12..18 and 20..22) and 4 GAP (GAP-04, 06, 07 and 09). The rows below follow the batch lists in the Story 7.0 spec.

### Batch F: Water 1 (22)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [EL-001](../invest/named-elements.md#element-001) | Finite reservoir | [EL-001-finite-reservoir](EL-001-finite-reservoir.md) | none | done |
| [EL-002](../invest/named-elements.md#element-002) | Header tank | [EL-002-header-tank](EL-002-header-tank.md) | none | done |
| [EL-003](../invest/named-elements.md#element-003) | Tap | [EL-003-tap](EL-003-tap.md) | none | done |
| [EL-004](../invest/named-elements.md#element-004) | Catch basin | [EL-004-catch-basin](EL-004-catch-basin.md) | none | done |
| [EL-005](../invest/named-elements.md#element-005) | Liquid funnel | [EL-005-liquid-funnel](EL-005-liquid-funnel.md) | none | done |
| [EL-006](../invest/named-elements.md#element-006) | Drain | [EL-006-drain](EL-006-drain.md) | none | done |
| [EL-007](../invest/named-elements.md#element-007) | Open gutter | [EL-007-open-gutter](EL-007-open-gutter.md) | none | done |
| [EL-008](../invest/named-elements.md#element-008) | Straight water pipe | [EL-008-straight-water-pipe](EL-008-straight-water-pipe.md) | none | done |
| [EL-009](../invest/named-elements.md#element-009) | 45-degree water elbow | [EL-009-water-elbow-45](EL-009-water-elbow-45.md) | none | done |
| [EL-010](../invest/named-elements.md#element-010) | 90-degree water elbow | [EL-010-water-elbow-90](EL-010-water-elbow-90.md) | none | done |
| [EL-011](../invest/named-elements.md#element-011) | Water T junction | [EL-011-water-t-junction](EL-011-water-t-junction.md) | none | done |
| [EL-012](../invest/named-elements.md#element-012) | Water pipe cap | [EL-012-water-pipe-cap](EL-012-water-pipe-cap.md) | none | done |
| [EL-013](../invest/named-elements.md#element-013) | Water nozzle | [EL-013-water-nozzle](EL-013-water-nozzle.md) | none | done |
| [EL-014](../invest/named-elements.md#element-014) | Water-carrying bucket | [EL-014-water-carrying-bucket](EL-014-water-carrying-bucket.md) | none | done |
| [EL-015](../invest/named-elements.md#element-015) | Leaky bucket | [EL-015-leaky-bucket](EL-015-leaky-bucket.md) | none | done |
| [EL-016](../invest/named-elements.md#element-016) | Float | [EL-016-float](EL-016-float.md) | none | done |
| [EL-017](../invest/named-elements.md#element-017) | Mechanical float valve | [EL-017-mechanical-float-valve](EL-017-mechanical-float-valve.md) | none | done |
| [EL-018](../invest/named-elements.md#element-018) | Electronic level switch | [EL-018-electronic-level-switch](EL-018-electronic-level-switch.md) | none | done |
| [EL-019](../invest/named-elements.md#element-019) | Water check valve | [EL-019-water-check-valve](EL-019-water-check-valve.md) | none | done |
| [EL-020](../invest/named-elements.md#element-020) | Water diverter | [EL-020-water-diverter](EL-020-water-diverter.md) | none | done |
| [EL-021](../invest/named-elements.md#element-021) | Sluice gate | [EL-021-sluice-gate](EL-021-sluice-gate.md) | none | done |
| [EL-022](../invest/named-elements.md#element-022) | Water pump | [EL-022-water-pump](EL-022-water-pump.md) | none | done |

### Batch G: Water 2 (22)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [EL-023](../invest/named-elements.md#element-023) | Archimedes screw | [EL-023-archimedes-screw](EL-023-archimedes-screw.md) | none | done |
| [EL-024](../invest/named-elements.md#element-024) | Primed siphon | [EL-024-primed-siphon](EL-024-primed-siphon.md) | none | done |
| [EL-025](../invest/named-elements.md#element-025) | Tipping-bucket water clock | [EL-025-tipping-bucket-water-clock](EL-025-tipping-bucket-water-clock.md) | none | done |
| [EL-026](../invest/named-elements.md#element-026) | Communicating tank | [EL-026-communicating-tank](EL-026-communicating-tank.md) | none | done |
| [EL-027](../invest/named-elements.md#element-027) | Canal lock chamber | [EL-027-canal-lock-chamber](EL-027-canal-lock-chamber.md) | none | done |
| [EL-028](../invest/named-elements.md#element-028) | Buoyant platform | [EL-028-buoyant-platform](EL-028-buoyant-platform.md) | none | done |
| [EL-029](../invest/named-elements.md#element-029) | Boat | [EL-029-boat](EL-029-boat.md) | none | done |
| [EL-030](../invest/named-elements.md#element-030) | Flow meter | [EL-030-flow-meter](EL-030-flow-meter.md) | none | done |
| [EL-031](../invest/named-elements.md#element-031) | Volume meter | [EL-031-volume-meter](EL-031-volume-meter.md) | none | done |
| [EL-032](../invest/named-elements.md#element-032) | Pressure meter | [EL-032-pressure-meter](EL-032-pressure-meter.md) | none | done |
| [EL-033](../invest/named-elements.md#element-033) | Fluid accumulator | [EL-033-fluid-accumulator](EL-033-fluid-accumulator.md) | none | done |
| [EL-034](../invest/named-elements.md#element-034) | Sponge | [EL-034-sponge](EL-034-sponge.md) | none | done |
| [EL-035](../invest/named-elements.md#element-035) | Wick | [EL-035-wick](EL-035-wick.md) | none | done |
| [EL-036](../invest/named-elements.md#element-036) | Sprinkler | [EL-036-sprinkler](EL-036-sprinkler.md) | none | done |
| [EL-165](../invest/named-elements.md#element-165) | Manual tap | [EL-165-manual-tap](EL-165-manual-tap.md) | none | done |
| [EL-166](../invest/named-elements.md#element-166) | Mechanically actuated tap | [EL-166-mechanically-actuated-tap](EL-166-mechanically-actuated-tap.md) | none | done |
| [EL-167](../invest/named-elements.md#element-167) | Solenoid tap | [EL-167-solenoid-tap](EL-167-solenoid-tap.md) | none | done |
| [EL-168](../invest/named-elements.md#element-168) | Siphon priming bulb | [EL-168-siphon-priming-bulb](EL-168-siphon-priming-bulb.md) | none | done |
| [EL-169](../invest/named-elements.md#element-169) | Cork float | [EL-169-cork-float](EL-169-cork-float.md) | none | done |
| [EL-170](../invest/named-elements.md#element-170) | Raft | [EL-170-raft](EL-170-raft.md) | none | done |
| [EL-171](../invest/named-elements.md#element-171) | Squeeze pad | [EL-171-squeeze-pad](EL-171-squeeze-pad.md) | none | done |
| [EL-172](../invest/named-elements.md#element-172) | Rain collector | [EL-172-rain-collector](EL-172-rain-collector.md) | none | done |

### Batch H: Pneumatic, Sound, Electrical and Control (30)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [EL-037](../invest/named-elements.md#element-037) | Air compressor | [EL-037-air-compressor](EL-037-air-compressor.md) | none | done |
| [EL-038](../invest/named-elements.md#element-038) | Pneumatic hose | [EL-038-pneumatic-hose](EL-038-pneumatic-hose.md) | none | done |
| [EL-039](../invest/named-elements.md#element-039) | Air reservoir | [EL-039-air-reservoir](EL-039-air-reservoir.md) | none | done |
| [EL-040](../invest/named-elements.md#element-040) | Pneumatic release valve | [EL-040-pneumatic-release-valve](EL-040-pneumatic-release-valve.md) | none | done |
| [EL-041](../invest/named-elements.md#element-041) | Pneumatic directional valve | [EL-041-pneumatic-directional-valve](EL-041-pneumatic-directional-valve.md) | none | done |
| [EL-042](../invest/named-elements.md#element-042) | Air nozzle | [EL-042-air-nozzle](EL-042-air-nozzle.md) | none | done |
| [EL-043](../invest/named-elements.md#element-043) | Pneumatic pressure gauge | [EL-043-pneumatic-pressure-gauge](EL-043-pneumatic-pressure-gauge.md) | none | done |
| [EL-044](../invest/named-elements.md#element-044) | Tone-selective sound meter | [EL-044-tone-selective-sound-meter](EL-044-tone-selective-sound-meter.md) | [CAT-060](CAT-060-sound_meter.md) | done |
| [EL-045](../invest/named-elements.md#element-045) | Air whistle | [EL-045-air-whistle](EL-045-air-whistle.md) | none | done |
| [EL-046](../invest/named-elements.md#element-046) | Listening horn | [EL-046-listening-horn](EL-046-listening-horn.md) | none | done |
| [EL-047](../invest/named-elements.md#element-047) | Exit horn | [EL-047-exit-horn](EL-047-exit-horn.md) | none | done |
| [EL-048](../invest/named-elements.md#element-048) | Acoustic duct | [EL-048-acoustic-duct](EL-048-acoustic-duct.md) | none | done |
| [EL-049](../invest/named-elements.md#element-049) | Acoustic resonator | [EL-049-acoustic-resonator](EL-049-acoustic-resonator.md) | none | done |
| [EL-050](../invest/named-elements.md#element-050) | Acoustic screen | [EL-050-acoustic-screen](EL-050-acoustic-screen.md) | none | done |
| [EL-051](../invest/named-elements.md#element-051) | Acoustic dish | [EL-051-acoustic-dish](EL-051-acoustic-dish.md) | none | done |
| [EL-052](../invest/named-elements.md#element-052) | Water-tuned bottle | [EL-052-water-tuned-bottle](EL-052-water-tuned-bottle.md) | none | done |
| [EL-133](../invest/named-elements.md#element-133) | Electrical AND gate | [EL-133-electrical-and-gate](EL-133-electrical-and-gate.md) | [CAT-013](CAT-013-both_gate.md) | done |
| [EL-134](../invest/named-elements.md#element-134) | Electrical OR gate | [EL-134-electrical-or-gate](EL-134-electrical-or-gate.md) | [CAT-026](CAT-026-electrical_or.md) | done |
| [EL-135](../invest/named-elements.md#element-135) | Electrical XOR gate | [EL-135-electrical-xor-gate](EL-135-electrical-xor-gate.md) | [CAT-027](CAT-027-electrical_xor.md) | done |
| [EL-136](../invest/named-elements.md#element-136) | Electrical NOR gate | [EL-136-electrical-nor-gate](EL-136-electrical-nor-gate.md) | [CAT-025](CAT-025-electrical_nor.md) | done |
| [EL-137](../invest/named-elements.md#element-137) | Electrical NAND gate | [EL-137-electrical-nand-gate](EL-137-electrical-nand-gate.md) | [CAT-024](CAT-024-electrical_nand.md) | done |
| [EL-179](../invest/named-elements.md#element-179) | Continuous-tone speaker | [EL-179-continuous-tone-speaker](EL-179-continuous-tone-speaker.md) | [CAT-061](CAT-061-speaker.md) | done |
| [EL-180](../invest/named-elements.md#element-180) | Pulse speaker | [EL-180-pulse-speaker](EL-180-pulse-speaker.md) | [CAT-061](CAT-061-speaker.md) | done |
| [EL-181](../invest/named-elements.md#element-181) | Rising-edge detector | [EL-181-rising-edge-detector](EL-181-rising-edge-detector.md) | none | done |
| [EL-182](../invest/named-elements.md#element-182) | Falling-edge detector | [EL-182-falling-edge-detector](EL-182-falling-edge-detector.md) | none | done |
| [EL-183](../invest/named-elements.md#element-183) | Resettable counter | [EL-183-resettable-counter](EL-183-resettable-counter.md) | [CAT-020](CAT-020-counter.md) | done |
| [EL-196](../invest/named-elements.md#element-196) | Battery | [EL-196-battery](EL-196-battery.md) | [CAT-005](CAT-005-battery.md) | done |
| [EL-197](../invest/named-elements.md#element-197) | Electrical wire | [EL-197-electrical-wire](EL-197-electrical-wire.md) | none | done |
| [EL-198](../invest/named-elements.md#element-198) | Switch | [EL-198-switch](EL-198-switch.md) | [CAT-063](CAT-063-switch.md) | done |
| [EL-211](../invest/named-elements.md#element-211) | Solar panel | [EL-211-solar-panel](EL-211-solar-panel.md) | [CAT-059](CAT-059-solar_panel.md) | done |

### Batch I: Mechanical 1 (22)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [EL-053](../invest/named-elements.md#element-053) | Rotary-to-linear converter | [EL-053-rotary-to-linear-converter](EL-053-rotary-to-linear-converter.md) | none | done |
| [EL-054](../invest/named-elements.md#element-054) | Linear-to-rotary converter | [EL-054-linear-to-rotary-converter](EL-054-linear-to-rotary-converter.md) | none | done |
| [EL-055](../invest/named-elements.md#element-055) | Mechanical brake | [EL-055-mechanical-brake](EL-055-mechanical-brake.md) | none | done |
| [EL-056](../invest/named-elements.md#element-056) | Ratchet | [EL-056-ratchet](EL-056-ratchet.md) | none | done |
| [EL-057](../invest/named-elements.md#element-057) | Escapement | [EL-057-escapement](EL-057-escapement.md) | none | done |
| [EL-058](../invest/named-elements.md#element-058) | Size grate | [EL-058-size-grate](EL-058-size-grate.md) | none | done |
| [EL-059](../invest/named-elements.md#element-059) | Weight tray | [EL-059-weight-tray](EL-059-weight-tray.md) | [CAT-052](CAT-052-pressure_plate.md) | done |
| [EL-060](../invest/named-elements.md#element-060) | Electromagnet | [EL-060-electromagnet](EL-060-electromagnet.md) | none | done |
| [EL-061](../invest/named-elements.md#element-061) | Indexed carousel | [EL-061-indexed-carousel](EL-061-indexed-carousel.md) | none | done |
| [EL-062](../invest/named-elements.md#element-062) | Docking ferry | [EL-062-docking-ferry](EL-062-docking-ferry.md) | none | done |
| [EL-063](../invest/named-elements.md#element-063) | Cable winch | [EL-063-cable-winch](EL-063-cable-winch.md) | none | done |
| [EL-064](../invest/named-elements.md#element-064) | Metal loop anchor | [EL-064-metal-loop-anchor](EL-064-metal-loop-anchor.md) | [CAT-058](CAT-058-rope_anchor.md) | done |
| [EL-065](../invest/named-elements.md#element-065) | Load hook | [EL-065-load-hook](EL-065-load-hook.md) | none | done |
| [EL-066](../invest/named-elements.md#element-066) | Scissors | [EL-066-scissors](EL-066-scissors.md) | none | done |
| [EL-067](../invest/named-elements.md#element-067) | Steel cable | [EL-067-steel-cable](EL-067-steel-cable.md) | [CAT-058](CAT-058-rope_anchor.md) | done |
| [EL-068](../invest/named-elements.md#element-068) | Tin snips | [EL-068-tin-snips](EL-068-tin-snips.md) | none | done |
| [EL-069](../invest/named-elements.md#element-069) | Moving bucket | [EL-069-moving-bucket](EL-069-moving-bucket.md) | [CAT-004](CAT-004-basket.md) | done |
| [EL-070](../invest/named-elements.md#element-070) | Moving cage | [EL-070-moving-cage](EL-070-moving-cage.md) | none | done |
| [EL-071](../invest/named-elements.md#element-071) | Straight metal ball pipe | [EL-071-straight-metal-ball-pipe](EL-071-straight-metal-ball-pipe.md) | [CAT-048](CAT-048-pipe.md) | done |
| [EL-072](../invest/named-elements.md#element-072) | Curved metal ball pipe | [EL-072-curved-metal-ball-pipe](EL-072-curved-metal-ball-pipe.md) | [CAT-049](CAT-049-pipe_bend_45.md), [CAT-050](CAT-050-pipe_bend_90.md) | done |
| [EL-073](../invest/named-elements.md#element-073) | Brick barrier | [EL-073-brick-barrier](EL-073-brick-barrier.md) | [CAT-066](CAT-066-wall.md) | done |
| [EL-074](../invest/named-elements.md#element-074) | Wood barrier | [EL-074-wood-barrier](EL-074-wood-barrier.md) | [CAT-066](CAT-066-wall.md) | done |

### Batch J: Mechanical 2 (28)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [EL-107](../invest/named-elements.md#element-107) | Spiral gravity-delay tube | [EL-107-spiral-delay-tube](EL-107-spiral-delay-tube.md) | none | done |
| [EL-108](../invest/named-elements.md#element-108) | Powered airlift | [EL-108-powered-airlift](EL-108-powered-airlift.md) | none | done |
| [EL-109](../invest/named-elements.md#element-109) | Speed-sensitive trapdoor | [EL-109-speed-trapdoor](EL-109-speed-trapdoor.md) | none | done |
| [EL-110](../invest/named-elements.md#element-110) | Flywheel | [EL-110-flywheel](EL-110-flywheel.md) | none | done |
| [EL-111](../invest/named-elements.md#element-111) | Centrifugal governor | [EL-111-centrifugal-governor](EL-111-centrifugal-governor.md) | none | done |
| [EL-156](../invest/named-elements.md#element-156) | Single-lobe cam | [EL-156-single-lobe-cam](EL-156-single-lobe-cam.md) | none | done |
| [EL-157](../invest/named-elements.md#element-157) | Double-lobe cam | [EL-157-double-lobe-cam](EL-157-double-lobe-cam.md) | none | done |
| [EL-158](../invest/named-elements.md#element-158) | Rise-hold-fall cam | [EL-158-rise-hold-fall-cam](EL-158-rise-hold-fall-cam.md) | none | done |
| [EL-162](../invest/named-elements.md#element-162) | Fixed ball diverter | [EL-162-fixed-ball-diverter](EL-162-fixed-ball-diverter.md) | none | done |
| [EL-163](../invest/named-elements.md#element-163) | Powered ball diverter | [EL-163-powered-ball-diverter](EL-163-powered-ball-diverter.md) | none | done |
| [EL-164](../invest/named-elements.md#element-164) | Alternating ball diverter | [EL-164-alternating-ball-diverter](EL-164-alternating-ball-diverter.md) | none | done |
| [EL-184](../invest/named-elements.md#element-184) | Mechanical ball gate | [EL-184-mechanical-ball-gate](EL-184-mechanical-ball-gate.md) | none | done |
| [EL-185](../invest/named-elements.md#element-185) | Releasable assembly joint | [EL-185-releasable-assembly-joint](EL-185-releasable-assembly-joint.md) | none | done |
| [EL-186](../invest/named-elements.md#element-186) | Temporary bridge | [EL-186-temporary-bridge](EL-186-temporary-bridge.md) | none | done |
| [EL-193](../invest/named-elements.md#element-193) | Domino | [EL-193-domino](EL-193-domino.md) | [CAT-023](CAT-023-domino.md) | done |
| [EL-194](../invest/named-elements.md#element-194) | Springboard | [EL-194-springboard](EL-194-springboard.md) | [CAT-062](CAT-062-spring.md) | done |
| [EL-195](../invest/named-elements.md#element-195) | Pinball bumper | [EL-195-pinball-bumper](EL-195-pinball-bumper.md) | [CAT-015](CAT-015-bumper.md) | done |
| [EL-199](../invest/named-elements.md#element-199) | Electric motor | [EL-199-electric-motor](EL-199-electric-motor.md) | [CAT-042](CAT-042-motor.md) | done |
| [EL-200](../invest/named-elements.md#element-200) | Drive belt | [EL-200-drive-belt](EL-200-drive-belt.md) | none | done |
| [EL-201](../invest/named-elements.md#element-201) | Drive chain | [EL-201-drive-chain](EL-201-drive-chain.md) | none | done |
| [EL-202](../invest/named-elements.md#element-202) | Conveyor | [EL-202-conveyor](EL-202-conveyor.md) | [CAT-019](CAT-019-conveyor.md) | done |
| [EL-203](../invest/named-elements.md#element-203) | Reverse transmission | [EL-203-reverse-transmission](EL-203-reverse-transmission.md) | [CAT-057](CAT-057-reverse_transmission.md) | done |
| [EL-205](../invest/named-elements.md#element-205) | Rope | [EL-205-rope](EL-205-rope.md) | none | done |
| [EL-206](../invest/named-elements.md#element-206) | Fixed pulley | [EL-206-fixed-pulley](EL-206-fixed-pulley.md) | [CAT-053](CAT-053-pulley.md) | done |
| [EL-207](../invest/named-elements.md#element-207) | Moving pulley | [EL-207-moving-pulley](EL-207-moving-pulley.md) | [CAT-053](CAT-053-pulley.md) | done |
| [EL-209](../invest/named-elements.md#element-209) | Fan | [EL-209-fan](EL-209-fan.md) | [CAT-028](CAT-028-fan.md) | done |
| [EL-215](../invest/named-elements.md#element-215) | Damped cushion | [EL-215-damped-cushion](EL-215-damped-cushion.md) | none | done |
| [EL-216](../invest/named-elements.md#element-216) | Capture cradle | [EL-216-capture-cradle](EL-216-capture-cradle.md) | none | done |

### Batch K: Optical (28)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [EL-138](../invest/named-elements.md#element-138) | Optical AND gate | [EL-138-optical-and-gate](EL-138-optical-and-gate.md) | [CAT-043](CAT-043-optical_and.md) | done |
| [EL-139](../invest/named-elements.md#element-139) | Optical OR gate | [EL-139-optical-or-gate](EL-139-optical-or-gate.md) | [CAT-046](CAT-046-optical_or.md) | done |
| [EL-140](../invest/named-elements.md#element-140) | Optical XOR gate | [EL-140-optical-xor-gate](EL-140-optical-xor-gate.md) | [CAT-047](CAT-047-optical_xor.md) | done |
| [EL-141](../invest/named-elements.md#element-141) | Optical NOR gate | [EL-141-optical-nor-gate](EL-141-optical-nor-gate.md) | [CAT-045](CAT-045-optical_nor.md) | done |
| [EL-142](../invest/named-elements.md#element-142) | Optical NAND gate | [EL-142-optical-nand-gate](EL-142-optical-nand-gate.md) | [CAT-044](CAT-044-optical_nand.md) | done |
| [EL-143](../invest/named-elements.md#element-143) | Red optical filter | [EL-143-red-optical-filter](EL-143-red-optical-filter.md) | [CAT-055](CAT-055-red_filter.md) | done |
| [EL-144](../invest/named-elements.md#element-144) | Green optical filter | [EL-144-green-optical-filter](EL-144-green-optical-filter.md) | [CAT-031](CAT-031-green_filter.md) | done |
| [EL-145](../invest/named-elements.md#element-145) | Blue optical filter | [EL-145-blue-optical-filter](EL-145-blue-optical-filter.md) | [CAT-011](CAT-011-blue_filter.md) | done |
| [EL-146](../invest/named-elements.md#element-146) | Red selective receiver | [EL-146-red-selective-receiver](EL-146-red-selective-receiver.md) | [CAT-056](CAT-056-red_receiver.md) | done |
| [EL-147](../invest/named-elements.md#element-147) | Green selective receiver | [EL-147-green-selective-receiver](EL-147-green-selective-receiver.md) | [CAT-032](CAT-032-green_receiver.md) | done |
| [EL-148](../invest/named-elements.md#element-148) | Blue selective receiver | [EL-148-blue-selective-receiver](EL-148-blue-selective-receiver.md) | [CAT-012](CAT-012-blue_receiver.md) | done |
| [EL-149](../invest/named-elements.md#element-149) | Yellow selective receiver | [EL-149-yellow-selective-receiver](EL-149-yellow-selective-receiver.md) | [CAT-072](CAT-072-yellow_receiver.md) | done |
| [EL-150](../invest/named-elements.md#element-150) | Cyan selective receiver | [EL-150-cyan-selective-receiver](EL-150-cyan-selective-receiver.md) | [CAT-021](CAT-021-cyan_receiver.md) | done |
| [EL-151](../invest/named-elements.md#element-151) | Magenta selective receiver | [EL-151-magenta-selective-receiver](EL-151-magenta-selective-receiver.md) | [CAT-040](CAT-040-magenta_receiver.md) | done |
| [EL-152](../invest/named-elements.md#element-152) | White selective receiver | [EL-152-white-selective-receiver](EL-152-white-selective-receiver.md) | [CAT-068](CAT-068-white_receiver.md) | done |
| [EL-153](../invest/named-elements.md#element-153) | General-light receiver | [EL-153-general-light-receiver](EL-153-general-light-receiver.md) | none | done |
| [EL-154](../invest/named-elements.md#element-154) | Light-charge receiver | [EL-154-light-charge-receiver](EL-154-light-charge-receiver.md) | none | done |
| [EL-155](../invest/named-elements.md#element-155) | Rope-operated light | [EL-155-rope-operated-light](EL-155-rope-operated-light.md) | none | done |
| [EL-173](../invest/named-elements.md#element-173) | Diverging lens | [EL-173-diverging-lens](EL-173-diverging-lens.md) | none | done |
| [EL-174](../invest/named-elements.md#element-174) | Prism | [EL-174-prism](EL-174-prism.md) | none | done |
| [EL-175](../invest/named-elements.md#element-175) | Optical fibre | [EL-175-optical-fibre](EL-175-optical-fibre.md) | none | done |
| [EL-176](../invest/named-elements.md#element-176) | Red laser emitter | [EL-176-red-laser-emitter](EL-176-red-laser-emitter.md) | none | done |
| [EL-177](../invest/named-elements.md#element-177) | Green laser emitter | [EL-177-green-laser-emitter](EL-177-green-laser-emitter.md) | none | done |
| [EL-178](../invest/named-elements.md#element-178) | Blue laser emitter | [EL-178-blue-laser-emitter](EL-178-blue-laser-emitter.md) | none | done |
| [EL-210](../invest/named-elements.md#element-210) | Flashlight | [EL-210-flashlight](EL-210-flashlight.md) | [CAT-029](CAT-029-flashlight.md) | done |
| [EL-212](../invest/named-elements.md#element-212) | Flat mirror | [EL-212-flat-mirror](EL-212-flat-mirror.md) | [CAT-041](CAT-041-mirror.md) | done |
| [EL-213](../invest/named-elements.md#element-213) | Optical combiner | [EL-213-optical-combiner](EL-213-optical-combiner.md) | [CAT-006](CAT-006-beam_combiner.md) | done |
| [EL-214](../invest/named-elements.md#element-214) | Broadband beam detector | [EL-214-broadband-beam-detector](EL-214-broadband-beam-detector.md) | [CAT-038](CAT-038-light_receiver.md) | done |

### Batch L: Specialist and Character (32)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [EL-075](../invest/named-elements.md#element-075) | Toy pulse emitter | [EL-075-toy-pulse-emitter](EL-075-toy-pulse-emitter.md) | [CAT-036](CAT-036-laser.md) | done |
| [EL-076](../invest/named-elements.md#element-076) | Programmable ball | [EL-076-programmable-ball](EL-076-programmable-ball.md) | [CAT-001](CAT-001-ball.md), [CAT-014](CAT-014-bowling.md), [CAT-064](CAT-064-tennis.md) | done |
| [EL-077](../invest/named-elements.md#element-077) | Soccer ball | [EL-077-soccer-ball](EL-077-soccer-ball.md) | [CAT-001](CAT-001-ball.md) | done |
| [EL-078](../invest/named-elements.md#element-078) | Can opener | [EL-078-can-opener](EL-078-can-opener.md) | none | done |
| [EL-079](../invest/named-elements.md#element-079) | Electric mixer | [EL-079-electric-mixer](EL-079-electric-mixer.md) | [CAT-042](CAT-042-motor.md) | done |
| [EL-080](../invest/named-elements.md#element-080) | Programmable box | [EL-080-programmable-box](EL-080-programmable-box.md) | [CAT-023](CAT-023-domino.md), [CAT-004](CAT-004-basket.md) | done |
| [EL-081](../invest/named-elements.md#element-081) | Message display | [EL-081-message-display](EL-081-message-display.md) | [CAT-020](CAT-020-counter.md), [CAT-035](CAT-035-lamp.md) | done |
| [EL-082](../invest/named-elements.md#element-082) | Lured character | [EL-082-lured-character](EL-082-lured-character.md) | none | done |
| [EL-083](../invest/named-elements.md#element-083) | Escaping character | [EL-083-escaping-character](EL-083-escaping-character.md) | none | done |
| [EL-084](../invest/named-elements.md#element-084) | Fragile fish bowl | [EL-084-fragile-fish-bowl](EL-084-fragile-fish-bowl.md) | none | done |
| [EL-085](../invest/named-elements.md#element-085) | Rope-driven character wheel | [EL-085-rope-driven-character-wheel](EL-085-rope-driven-character-wheel.md) | [CAT-058](CAT-058-rope_anchor.md), [CAT-053](CAT-053-pulley.md) | done |
| [EL-086](../invest/named-elements.md#element-086) | Obstacle-reversing walker | [EL-086-obstacle-reversing-walker](EL-086-obstacle-reversing-walker.md) | none | done |
| [EL-087](../invest/named-elements.md#element-087) | Predator character | [EL-087-predator-character](EL-087-predator-character.md) | none | done |
| [EL-088](../invest/named-elements.md#element-088) | Fish tank lure | [EL-088-fish-tank-lure](EL-088-fish-tank-lure.md) | none | done |
| [EL-089](../invest/named-elements.md#element-089) | Gravity-effect pad | [EL-089-gravity-effect-pad](EL-089-gravity-effect-pad.md) | none | done |
| [EL-090](../invest/named-elements.md#element-090) | Steerable blimp | [EL-090-steerable-blimp](EL-090-steerable-blimp.md) | [CAT-003](CAT-003-balloon.md) | done |
| [EL-091](../invest/named-elements.md#element-091) | Cannonball | [EL-091-cannonball](EL-091-cannonball.md) | [CAT-014](CAT-014-bowling.md) | done |
| [EL-092](../invest/named-elements.md#element-092) | Toy rocket | [EL-092-toy-rocket](EL-092-toy-rocket.md) | none | done |
| [EL-093](../invest/named-elements.md#element-093) | Toy dynamite charge | [EL-093-toy-dynamite-charge](EL-093-toy-dynamite-charge.md) | none | done |
| [EL-094](../invest/named-elements.md#element-094) | Detonation plunger | [EL-094-detonation-plunger](EL-094-detonation-plunger.md) | [CAT-063](CAT-063-switch.md), [CAT-023](CAT-023-domino.md) | done |
| [EL-095](../invest/named-elements.md#element-095) | Toy revolver | [EL-095-toy-revolver](EL-095-toy-revolver.md) | [CAT-016](CAT-016-cannon.md) | done |
| [EL-096](../invest/named-elements.md#element-096) | Tipsy platform | [EL-096-tipsy-platform](EL-096-tipsy-platform.md) | none | done |
| [EL-097](../invest/named-elements.md#element-097) | Pool cue | [EL-097-pool-cue](EL-097-pool-cue.md) | none | done |
| [EL-098](../invest/named-elements.md#element-098) | Pool ball | [EL-098-pool-ball](EL-098-pool-ball.md) | [CAT-001](CAT-001-ball.md) | done |
| [EL-099](../invest/named-elements.md#element-099) | Pool pocket | [EL-099-pool-pocket](EL-099-pool-pocket.md) | [CAT-004](CAT-004-basket.md), [CAT-002](CAT-002-ball_detector.md) | done |
| [EL-100](../invest/named-elements.md#element-100) | Vacuum nozzle | [EL-100-vacuum-nozzle](EL-100-vacuum-nozzle.md) | [CAT-028](CAT-028-fan.md), [CAT-010](CAT-010-bellows.md) | done |
| [EL-101](../invest/named-elements.md#element-101) | Thumb tack | [EL-101-thumb-tack](EL-101-thumb-tack.md) | none | done |
| [EL-102](../invest/named-elements.md#element-102) | Large-bore ball pipe | [EL-102-large-bore-ball-pipe](EL-102-large-bore-ball-pipe.md) | [CAT-048](CAT-048-pipe.md) | done |
| [EL-103](../invest/named-elements.md#element-103) | Accelerator tube | [EL-103-accelerator-tube](EL-103-accelerator-tube.md) | [CAT-048](CAT-048-pipe.md) | done |
| [EL-104](../invest/named-elements.md#element-104) | Toy firework | [EL-104-toy-firework](EL-104-toy-firework.md) | none | done |
| [EL-105](../invest/named-elements.md#element-105) | Toy missile | [EL-105-toy-missile](EL-105-toy-missile.md) | none | done |
| [EL-106](../invest/named-elements.md#element-106) | Impact-sensitive toy charge | [EL-106-impact-sensitive-toy-charge](EL-106-impact-sensitive-toy-charge.md) | none | done |

### Batch M: Construction, Environment, Goal, Gravity, Material and GAP (29)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [EL-112](../invest/named-elements.md#element-112) | Structural beam | [EL-112-structural-beam](EL-112-structural-beam.md) | none | done |
| [EL-113](../invest/named-elements.md#element-113) | Structural brace | [EL-113-structural-brace](EL-113-structural-brace.md) | none | done |
| [EL-114](../invest/named-elements.md#element-114) | Placeable pivot | [EL-114-placeable-pivot](EL-114-placeable-pivot.md) | none | done |
| [EL-115](../invest/named-elements.md#element-115) | Linkage connector | [EL-115-linkage-connector](EL-115-linkage-connector.md) | none | done |
| [EL-116](../invest/named-elements.md#element-116) | Passive wheel | [EL-116-passive-wheel](EL-116-passive-wheel.md) | none | done |
| [EL-117](../invest/named-elements.md#element-117) | Axle | [EL-117-axle](EL-117-axle.md) | none | done |
| [EL-118](../invest/named-elements.md#element-118) | Coating station | [EL-118-coating-station](EL-118-coating-station.md) | none | done |
| [EL-119](../invest/named-elements.md#element-119) | Dye station | [EL-119-dye-station](EL-119-dye-station.md) | none | done |
| [EL-120](../invest/named-elements.md#element-120) | Quantity goal | [EL-120-quantity-goal](EL-120-quantity-goal.md) | none | done |
| [EL-121](../invest/named-elements.md#element-121) | Rate-window goal | [EL-121-rate-window-goal](EL-121-rate-window-goal.md) | none | done |
| [EL-122](../invest/named-elements.md#element-122) | Ordered-events goal | [EL-122-ordered-events-goal](EL-122-ordered-events-goal.md) | none | done |
| [EL-123](../invest/named-elements.md#element-123) | Protected-end-state goal | [EL-123-protected-end-state-goal](EL-123-protected-end-state-goal.md) | none | done |
| [EL-124](../invest/named-elements.md#element-124) | Authored gravity preset | [EL-124-authored-gravity-preset](EL-124-authored-gravity-preset.md) | none | done |
| [EL-125](../invest/named-elements.md#element-125) | Authored atmosphere preset | [EL-125-authored-atmosphere-preset](EL-125-authored-atmosphere-preset.md) | none | done |
| [EL-159](../invest/named-elements.md#element-159) | Tension-limited connector | [EL-159-tension-limited-connector](EL-159-tension-limited-connector.md) | none | done |
| [EL-160](../invest/named-elements.md#element-160) | Shear-limited connector | [EL-160-shear-limited-connector](EL-160-shear-limited-connector.md) | none | done |
| [EL-161](../invest/named-elements.md#element-161) | Bending-limited connector | [EL-161-bending-limited-connector](EL-161-bending-limited-connector.md) | none | done |
| [EL-187](../invest/named-elements.md#element-187) | Basketball | [EL-187-basketball](EL-187-basketball.md) | [CAT-001](CAT-001-ball.md) | done |
| [EL-188](../invest/named-elements.md#element-188) | Bowling ball | [EL-188-bowling-ball](EL-188-bowling-ball.md) | [CAT-014](CAT-014-bowling.md) | done |
| [EL-189](../invest/named-elements.md#element-189) | Tennis ball | [EL-189-tennis-ball](EL-189-tennis-ball.md) | [CAT-064](CAT-064-tennis.md) | done |
| [EL-190](../invest/named-elements.md#element-190) | Ramp | [EL-190-ramp](EL-190-ramp.md) | [CAT-054](CAT-054-ramp.md) | done |
| [EL-191](../invest/named-elements.md#element-191) | Wall | [EL-191-wall](EL-191-wall.md) | [CAT-066](CAT-066-wall.md) | done |
| [EL-192](../invest/named-elements.md#element-192) | Receiving basket | [EL-192-receiving-basket](EL-192-receiving-basket.md) | [CAT-004](CAT-004-basket.md) | done |
| [EL-204](../invest/named-elements.md#element-204) | Weight | [EL-204-weight](EL-204-weight.md) | [CAT-067](CAT-067-weight.md) | done |
| [EL-208](../invest/named-elements.md#element-208) | Balloon | [EL-208-balloon](EL-208-balloon.md) | [CAT-003](CAT-003-balloon.md) | done |
| [GAP-04](../invest/named-elements.md#gap-04) | P2 potential: Driven wheel | [GAP-04-driven-wheel](GAP-04-driven-wheel.md) | none | done |
| [GAP-06](../invest/named-elements.md#gap-06) | P3 potential: Granular dispenser | [GAP-06-granular-dispenser](GAP-06-granular-dispenser.md) | none | done |
| [GAP-07](../invest/named-elements.md#gap-07) | P3 potential: Granular sieve | [GAP-07-granular-sieve](GAP-07-granular-sieve.md) | none | done |
| [GAP-09](../invest/named-elements.md#gap-09) | P3 potential: Fragmentation station | [GAP-09-fragmentation-station](GAP-09-fragmentation-station.md) | none | done |

### Batch N: Thermal (37)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [TH-01](../invest/named-elements.md#thermal-01) | Candle | [TH-01-candle](TH-01-candle.md) | none | done |
| [TH-02](../invest/named-elements.md#thermal-02) | Fire bowl | [TH-02-fire-bowl](TH-02-fire-bowl.md) | none | done |
| [TH-03](../invest/named-elements.md#thermal-03) | Combustible block | [TH-03-combustible-block](TH-03-combustible-block.md) | none | done |
| [TH-04](../invest/named-elements.md#thermal-04) | Electrical heating plate | [TH-04-electrical-heating-plate](TH-04-electrical-heating-plate.md) | none | done |
| [TH-05](../invest/named-elements.md#thermal-05) | Friction brake | [TH-05-friction-brake](TH-05-friction-brake.md) | none | done |
| [TH-06](../invest/named-elements.md#thermal-06) | Converging lens | [TH-06-converging-lens](TH-06-converging-lens.md) | none | done |
| [TH-07](../invest/named-elements.md#thermal-07) | Solar absorber plate | [TH-07-solar-absorber-plate](TH-07-solar-absorber-plate.md) | none | done |
| [TH-08](../invest/named-elements.md#thermal-08) | Heat-conducting bar | [TH-08-heat-conducting-bar](TH-08-heat-conducting-bar.md) | none | done |
| [TH-09](../invest/named-elements.md#thermal-09) | Insulating panel | [TH-09-insulating-panel](TH-09-insulating-panel.md) | none | done |
| [TH-10](../invest/named-elements.md#thermal-10) | Finned heat sink | [TH-10-finned-heat-sink](TH-10-finned-heat-sink.md) | none | done |
| [TH-11](../invest/named-elements.md#thermal-11) | Heat exchanger | [TH-11-heat-exchanger](TH-11-heat-exchanger.md) | none | done |
| [TH-12](../invest/named-elements.md#thermal-12) | Reversible heat pump | [TH-12-reversible-heat-pump](TH-12-reversible-heat-pump.md) | none | done |
| [TH-13](../invest/named-elements.md#thermal-13) | Freezing mold | [TH-13-freezing-mold](TH-13-freezing-mold.md) | none | done |
| [TH-14](../invest/named-elements.md#thermal-14) | Ice block | [TH-14-ice-block](TH-14-ice-block.md) | none | done |
| [TH-15](../invest/named-elements.md#thermal-15) | Ice plug | [TH-15-ice-plug](TH-15-ice-plug.md) | none | done |
| [TH-16](../invest/named-elements.md#thermal-16) | Fusible link | [TH-16-fusible-link](TH-16-fusible-link.md) | none | done |
| [TH-17](../invest/named-elements.md#thermal-17) | Kettle | [TH-17-kettle](TH-17-kettle.md) | none | done |
| [TH-18](../invest/named-elements.md#thermal-18) | Condenser | [TH-18-condenser](TH-18-condenser.md) | none | done |
| [TH-19](../invest/named-elements.md#thermal-19) | Steam piston | [TH-19-steam-piston](TH-19-steam-piston.md) | none | done |
| [TH-20](../invest/named-elements.md#thermal-20) | Steam turbine | [TH-20-steam-turbine](TH-20-steam-turbine.md) | none | done |
| [TH-21](../invest/named-elements.md#thermal-21) | Temperature sensor | [TH-21-temperature-sensor](TH-21-temperature-sensor.md) | none | done |
| [TH-22](../invest/named-elements.md#thermal-22) | Bimetal thermostat | [TH-22-bimetal-thermostat](TH-22-bimetal-thermostat.md) | none | done |
| [TH-23](../invest/named-elements.md#thermal-23) | Expansion rod | [TH-23-expansion-rod](TH-23-expansion-rod.md) | none | done |
| [TH-24](../invest/named-elements.md#thermal-24) | Gas expansion bladder | [TH-24-gas-expansion-bladder](TH-24-gas-expansion-bladder.md) | none | done |
| [TH-25](../invest/named-elements.md#thermal-25) | Hot-air balloon | [TH-25-hot-air-balloon](TH-25-hot-air-balloon.md) | none | done |
| [TH-26](../invest/named-elements.md#thermal-26) | Thermal storage block | [TH-26-thermal-storage-block](TH-26-thermal-storage-block.md) | none | done |
| [TH-27](../invest/named-elements.md#thermal-27) | Phase-change storage cartridge | [TH-27-phase-change-cartridge](TH-27-phase-change-cartridge.md) | none | done |
| [TH-28](../invest/named-elements.md#thermal-28) | Evaporative cooling pad | [TH-28-evaporative-cooling-pad](TH-28-evaporative-cooling-pad.md) | none | done |
| [TH-29](../invest/named-elements.md#thermal-29) | Cold pack | [TH-29-cold-pack](TH-29-cold-pack.md) | none | done |
| [TH-30](../invest/named-elements.md#thermal-30) | Thermoelectric generator | [TH-30-thermoelectric-generator](TH-30-thermoelectric-generator.md) | none | done |
| [TH-31](../invest/named-elements.md#thermal-31) | Flint striker | [TH-31-flint-striker](TH-31-flint-striker.md) | none | done |
| [TH-32](../invest/named-elements.md#thermal-32) | Tinder pad | [TH-32-tinder-pad](TH-32-tinder-pad.md) | none | done |
| [TH-33](../invest/named-elements.md#thermal-33) | Spring-mounted match | [TH-33-spring-mounted-match](TH-33-spring-mounted-match.md) | none | done |
| [TH-34](../invest/named-elements.md#thermal-34) | Timed toaster ejector | [TH-34-timed-toaster-ejector](TH-34-timed-toaster-ejector.md) | none | done |
| [TH-35](../invest/named-elements.md#thermal-35) | Coffee-pot steam vessel | [TH-35-coffee-pot-steam-vessel](TH-35-coffee-pot-steam-vessel.md) | none | done |
| [TH-36](../invest/named-elements.md#thermal-36) | Lamp-trigger apparatus | [TH-36-lamp-trigger-apparatus](TH-36-lamp-trigger-apparatus.md) | none | done |
| [TH-37](../invest/named-elements.md#thermal-37) | Heat-sensitive target | [TH-37-heat-sensitive-target](TH-37-heat-sensitive-target.md) | none | done |

### Batch O: Radiation (26)

| Identity | Name | Spec | Refines CAT | Spec status |
| --- | --- | --- | --- | --- |
| [EL-126](../invest/named-elements.md#element-126) | Thin-screen shield | [EL-126-thin-screen-shield](EL-126-thin-screen-shield.md) | none | done |
| [EL-127](../invest/named-elements.md#element-127) | Polymer shield | [EL-127-polymer-shield](EL-127-polymer-shield.md) | none | done |
| [EL-128](../invest/named-elements.md#element-128) | Dense shield | [EL-128-dense-shield](EL-128-dense-shield.md) | none | done |
| [EL-129](../invest/named-elements.md#element-129) | Sheet-thickness transmission gauge | [EL-129-sheet-thickness-transmission-gauge](EL-129-sheet-thickness-transmission-gauge.md) | none | done |
| [EL-130](../invest/named-elements.md#element-130) | Tank-level transmission gauge | [EL-130-tank-level-transmission-gauge](EL-130-tank-level-transmission-gauge.md) | none | done |
| [EL-131](../invest/named-elements.md#element-131) | Fast-neutron detector | [EL-131-fast-neutron-detector](EL-131-fast-neutron-detector.md) | none | done |
| [EL-132](../invest/named-elements.md#element-132) | Slow-neutron detector | [EL-132-slow-neutron-detector](EL-132-slow-neutron-detector.md) | none | done |
| [RAD-01](../invest/named-elements.md#radiation-01) | P1 potential: Gamma source capsule | [RAD-01-gamma-source-capsule](RAD-01-gamma-source-capsule.md) | none | done |
| [RAD-02](../invest/named-elements.md#radiation-02) | P1 potential: Powered X-ray emitter | [RAD-02-powered-x-ray-emitter](RAD-02-powered-x-ray-emitter.md) | none | done |
| [RAD-03](../invest/named-elements.md#radiation-03) | P2 potential: Alpha source cartridge | [RAD-03-alpha-source-cartridge](RAD-03-alpha-source-cartridge.md) | none | done |
| [RAD-04](../invest/named-elements.md#radiation-04) | P2 potential: Beta-minus source cartridge | [RAD-04-beta-minus-source-cartridge](RAD-04-beta-minus-source-cartridge.md) | none | done |
| [RAD-06](../invest/named-elements.md#radiation-06) | P1 potential: Powered radiation shutter | [RAD-06-powered-radiation-shutter](RAD-06-powered-radiation-shutter.md) | none | done |
| [RAD-07](../invest/named-elements.md#radiation-07) | P1 potential: Collimator block | [RAD-07-collimator-block](RAD-07-collimator-block.md) | none | done |
| [RAD-08](../invest/named-elements.md#radiation-08) | P1 potential: Radiation rate meter | [RAD-08-radiation-rate-meter](RAD-08-radiation-rate-meter.md) | none | done |
| [RAD-09](../invest/named-elements.md#radiation-09) | P1 potential: Integrating dosimeter | [RAD-09-integrating-dosimeter](RAD-09-integrating-dosimeter.md) | none | done |
| [RAD-10](../invest/named-elements.md#radiation-10) | P1 potential: Exposure-sensitive cargo badge | [RAD-10-exposure-sensitive-cargo-badge](RAD-10-exposure-sensitive-cargo-badge.md) | none | done |
| [RAD-12](../invest/named-elements.md#radiation-12) | P2 potential: Scintillator tile | [RAD-12-scintillator-tile](RAD-12-scintillator-tile.md) | none | done |
| [RAD-13](../invest/named-elements.md#radiation-13) | P2 potential: Decay clock capsule | [RAD-13-decay-clock-capsule](RAD-13-decay-clock-capsule.md) | none | done |
| [RAD-14](../invest/named-elements.md#radiation-14) | P3 potential: Magnetic deflector | [RAD-14-magnetic-deflector](RAD-14-magnetic-deflector.md) | none | done |
| [RAD-15](../invest/named-elements.md#radiation-15) | P3 potential: Track chamber | [RAD-15-track-chamber](RAD-15-track-chamber.md) | none | done |
| [RAD-16](../invest/named-elements.md#radiation-16) | P3 potential: Neutron source module | [RAD-16-neutron-source-module](RAD-16-neutron-source-module.md) | none | done |
| [RAD-17](../invest/named-elements.md#radiation-17) | P3 potential: Water moderator tank | [RAD-17-water-moderator-tank](RAD-17-water-moderator-tank.md) | none | done |
| [RAD-18](../invest/named-elements.md#radiation-18) | P3 potential: Neutron absorber panel | [RAD-18-neutron-absorber-panel](RAD-18-neutron-absorber-panel.md) | none | done |
| [RAD-20](../invest/named-elements.md#radiation-20) | P3 potential: Radioisotope thermoelectric generator | [RAD-20-radioisotope-thermoelectric-generator](RAD-20-radioisotope-thermoelectric-generator.md) | none | done |
| [RAD-21](../invest/named-elements.md#radiation-21) | P3 potential: Radiation-responsive material latch | [RAD-21-radiation-responsive-material-latch](RAD-21-radiation-responsive-material-latch.md) | none | done |
| [RAD-22](../invest/named-elements.md#radiation-22) | P2 potential: Sealed tracer capsule | [RAD-22-sealed-tracer-capsule](RAD-22-sealed-tracer-capsule.md) | none | done |

## Index-only entries

These 17 rows get no spec: 10 umbrella scope-index entries and 7 GAP product features. With the 276 named-identity specs above they account for all 293 named identities in [named-elements.md](../invest/named-elements.md).

### Umbrella scope-index entries (10)

| Entry | Children | Notes |
| --- | --- | --- |
| [gap-01](../invest/named-elements.md#gap-01) | EL-112, EL-113 | Navigation only; each child has its own spec. |
| [gap-02](../invest/named-elements.md#gap-02) | EL-114, EL-115 | Navigation only; each child has its own spec. |
| [gap-03](../invest/named-elements.md#gap-03) | EL-116, EL-117 | Navigation only; each child has its own spec. |
| [gap-05](../invest/named-elements.md#gap-05) | EL-159, EL-160, EL-161 | Navigation only; each child has its own spec. |
| [gap-08](../invest/named-elements.md#gap-08) | EL-118, EL-119 | Navigation only; each child has its own spec. |
| [gap-16](../invest/named-elements.md#gap-16) | EL-120, EL-121, EL-122, EL-123 | Navigation only; each child has its own spec. |
| [gap-18](../invest/named-elements.md#gap-18) | EL-124, EL-125 | Navigation only; each child has its own spec. |
| [radiation-05](../invest/named-elements.md#radiation-05) | EL-126, EL-127, EL-128 | Navigation only; each child has its own spec. |
| [radiation-11](../invest/named-elements.md#radiation-11) | EL-129, EL-130 | Navigation only; each child has its own spec. |
| [radiation-19](../invest/named-elements.md#radiation-19) | EL-131, EL-132 | Navigation only; each child has its own spec. |

### GAP product features (7)

| Identity | Name | Notes |
| --- | --- | --- |
| [GAP-10](../invest/named-elements.md#gap-10) | P2 potential: Multi-selection and reusable assembly blueprints | Not a puzzle element; the requirements row stands. |
| [GAP-11](../invest/named-elements.md#gap-11) | P2 potential: Player puzzle editor | Not a puzzle element; the requirements row stands. |
| [GAP-12](../invest/named-elements.md#gap-12) | P2 potential: Portable puzzle and machine exchange | Not a puzzle element; the requirements row stands. |
| [GAP-13](../invest/named-elements.md#gap-13) | P2 potential: Named save library and local player profiles | Not a puzzle element; the requirements row stands. |
| [GAP-14](../invest/named-elements.md#gap-14) | P2 potential: Player simulation transport controls | Not a puzzle element; the requirements row stands. |
| [GAP-15](../invest/named-elements.md#gap-15) | P2 potential: Optional causal inspection | Not a puzzle element; the requirements row stands. |
| [GAP-17](../invest/named-elements.md#gap-17) | P2 potential: Authored construction zones | Not a puzzle element; the requirements row stands. |
