# Story 7.0 owner decisions (collected)

This file collects the roadmap-ordering problems and requirement conflicts found while writing element specs. Each item is marked either **reported**, meaning the implementer raised it, or **confirmed**, meaning an independent reviewer agreed. Items go to the owner in batches.

Story numbers below retain the labels used when each question was raised; they are historical references, not the current schedule. Use the [old-to-new mapping](../../docs/planning/invest/vertical-delivery.md#owner-reordering-dependencies) and [current epics](../planning-artifacts/epics.md) for implementation order (Battery 8.1, Powered gate 8.2, activation in Epic 9).

## Owner decisions recorded (9 Oct 2026)

- **Bumper recharge and depletion:** Story 6.2 adds a recharge connection now. Insufficient positive stored energy gives an affordable partial boost; zero energy gives ordinary bounce. CAT-015 and EL-195 carry this decision. Source, port mapping and recharge rate are still pending; no Battery default is approved.

- **Baseline-only harvest citations (resumed review):** owner chose baseline-only facts. CAT-009, CAT-061 and CAT-069 retain a6c914e wavefront caller tuples and tests, with missing record-field mapping explicit; CAT-029 retains baseline cone geometry and records the missing opacity definition. Earlier-revision definitions are not prerequisites or approved exceptions.

- **Electrical first:** Story 9.1 (battery and electrical network) moves before Epic 8. This resolves O1.
- **First electrical consumer:** the Powered gate (11.4) moves up to right after 9.1. This resolves O2 for the consumer. The switch electrical mode for 9.3 still needs scheduling.
- **Reorder to dependencies:** each story moves after what it needs. This resolves O3–O7 and the newer ordering items:
  - the shutter goes into Epic 13 after the lasers;
  - the trampoline goes after ropes;
  - a new cylinder-collider story goes before the first shaft wheel;
  - Bell and Speaker go after propagation;
  - Bellows goes after Windmill and Conveyor;
  - the optics receivers go before the mirror and filter stories.
- **Conflicts:** epic-story-versus-requirement conflicts are decided case by case. Present each one to the owner individually once all batches finish.

## Ordering

| Item | Batch | Status | Issue |
|---|---|---|---|
| O1 | B | reported | Epic 8 parts are electrical contacts in the legacy (plate, counter contact, hold timer, latch), and the clock has no unpowered mode, but the supplied network arrives in Story 9.1. |
| O2 | B | reported | Story 9.1 uses the Motor (Story 11.1) as its consumer. Story 9.3 needs the switch electrical mode, which no story schedules. |
| O3 | C | reported | Story 11.6, the beam shutter, needs a laser, which comes in Epic 13. |
| O4 | C | reported | The trampoline's tether criterion needs ropes (Story 10.3), but the trampoline is Story 10.1. |
| O5 | C | reported | No story builds the cylinder collider and inertia that the shaft wheels need. |
| O6 | C | reported | The wound spring needs a mechanical belt connection type, which no current domain provides. |

## Requirement and legacy conflicts

| Item | Batch | Status | Issue |
|---|---|---|---|
| R1 | B | reported | Story 8.1 sets the threshold at "> 0.2 kg". The requirement and the legacy set a 0.5 kg default, inclusive. |
| R2 | B | reported | Stories 8.1 and 9.2 wire their outputs to the Signal lamp, which takes activation only. |
| R3 | B | reported | Story 9.1 says "12V", but nothing models voltage. |
| R4 | B | reported | EL-196 Battery needs a finite energy store, but the legacy battery is plain on/off. EL-183 needs a reset input that the legacy counter lacks. |
| R5 | B | reported | The current network emits once per world per source, which rules out the counter and a re-arming hold timer. |
| R6 | C | reported | Story 11.5 asks for a 12 m/s muzzle speed, but the legacy default gives about 9.5 m/s. The Bowling ball, 0.56 m across, can't seat in the cannon, whose minimum is 0.6 m. |
| R7 | C | reported | The clutch's `close_seconds` is a range in the requirement row but an enum in `docs/rotary-transmission-parts.md`. The legacy tests disagree on whether a sourceless mechanical loop is allowed. |
| R8 | C | reported | The impact lever has no rope sockets and its fulcrum has no collider, though the requirements need both. The beam shutter has no rope socket either. |
| R10 | C | confirmed | Several epic stories conflict with the CAT requirement rows:<br>• Story 11.1: the belt "decelerates to a halt", but the requirement says it keeps momentum. It also cites a "12V battery".<br>• Story 11.2: "disengaged via signal", but the clutch coil is electrical.<br>• Story 11.4: "activation pulse", but the gate is electrical only.<br>• Story 10.3: rope "cutting", which was legacy future work.<br>• Story 11.3: a 1.0 m stroke, against a 2.4 m default.<br>• Story 10.4: "damps gradually", with no damping source. |
| R11 | C | confirmed | 16 tracked `docs/*-recipes.json` files and the `docs/*-sources.json` files lose their Playtest consumer in Epic 7. Keep them as evidence or delete them? |
| R12 | D | reported | Tennis bounce is 0.78 in the legacy but 0.85 in Story 12.1. |
| R13 | D | reported | The fan is self-contained with no battery in the requirement, but battery-powered in Story 12.2. Its field is a cylinder in the legacy and a cone in Story 12.2. The default fan accelerates a tennis ball at about 25.7 m/s², above the 16 m/s² cap. |
| R14 | D | reported | Bell threshold: the requirement uses approach speed, Story 14.1 uses kinetic energy. Story 14.4 has the chime tubes striking each other. The windmill cut-in is 0.05 m/s in the docs but 0.05 N in the legacy. |
| R15 | D | reported | The balloon's legacy drag of 0.4 exceeds the 0.125 /s body drag limit. The chimes need angular drag of 0.9 /s, and the solver has no angular drag. |
| O7 | D | reported | Bell and Speaker (Stories 14.1–14.2) need sound propagation, which arrives with the Sound meter in Story 14.3. No story schedules the gas state needed by the balloon and bellows. |
| R16 | A | reported | The Domino's offset centre of mass has no value anywhere at a6c914e. R-N3, which CAT-001 holds as Fail, is not defined anywhere. The legacy oversize-mouth control used a 0.8 m ball, but both current ball kinds fit the 0.65 m bore, so a control body must be chosen. |
| R17 | A | reported | Story 6.5 plans to delete `engine/LatchedSpringStore.cs`, which does not exist. The springboard contract also says preload is not a latch. |
| B-confirmed | B | confirmed | The Batch B reviewer confirmed O1, O2, R1–R5 and the following:<br>• Every observable electrical consumer (Motor, Powered gate) is in Epic 11, after Epics 8 and 9.<br>• `ActivationNodeKind.Latch` (the Signal lamp's first-input lock) must be renamed before the Set/Reset latch.<br>• The activation and cosmetic code still holds `Half` values (f32 migration). |
| P1 | F, G | coordinator | **Game-scale water density** is one family constant for all liquid elements. Batch F proposed 16 kg/m³, so one 2⁻⁴ m³ step weighs 1 kg; Batch G proposed 20. The coordinator aligned both on 16 (proposed). Catalogue bodies range from 2 to 45 kg/m³, so real water density would make everything float. |
| P2 | K, H | reported | Each EL identity and its matching CAT element (for example EL-146 and CAT-056) describe one part. Confirm that one part satisfies both rows. The EL rows add declared loss, efficiency, meter and finite energy that the legacy parts never had. |
| P3 | N | reported | Thermal scaling is proposed as specific heat at SI × ¼ and latent heat at SI × 1/16. TH-25 (hot-air balloon) needs about 16 min of heating under SI air heat capacity, so it is unplayable as specified. |
| P5 | O | confirmed | Radiation rate law: should it use one path, or average over the emitter and receiver areas (S606-D)? Does a thin screen pass alpha (the S605 row) or stop it (RAD-03)? Should alpha appear in the deflector and track-chamber lessons? Should the element-map anomalies on RAD-13, 21 and 22 be removed? |
| P6 | N | confirmed | One game scale linking reaction energy, specific heat, latent heat and gas Cv. Should the hot-air balloon get more power, a different scale or a hot start? Which optical power unit: game intensity or watts? Brake art and model: TH-05 or EL-055? Should contact conductance be set per part or per pair? |
| P7 | I, M | confirmed | Material density scale for structural parts. Real brick would weigh 1710 kg, against catalogue densities of 2–44 kg/m³. Should metal pipes use a 0.62 m gauge or the shared 1.3 m bore? Are EL-071/072 the same parts as CAT-048–050? Weld joint kind (EL-112, 161). Granular capacity beyond 16 dynamic bodies (GAP-06/07). |
| P8 | A | confirmed | Two items with no owning story or decision. First, physical placement nudging: it blocks Stories 6.9 and 6.10's assistance criteria and CAT-015 and CAT-048, and no story builds it. Second, DESIGN.md says the switch and lamp change state immediately with "no tween", but the delivered curves are a 0.16 s smoothstep. Which is intended? |
| S1 | reorder | reported | **Open scheduling items left by the reorder:**<br>(a) The RGB lasers: should they come before Story 13.4 (filters)?<br>(b) The switch electrical mode needed by Story 8.4 (NAND/NOR).<br>(c) The sound meter (14.1) now comes first, but its acceptance uses the Bell (14.2) as its source.<br>(d) Battery 8.1's acceptance names the Motor (11.1). This is a case-by-case conflict.<br>(e) Story 10.1 (cylinder) sits before the Lever only if the fulcrum is a cylinder; otherwise it moves to just before 11.1. |
| S2 | reorder review | confirmed | The coordinator applied the owner's literal electrical order: Battery, then Powered gate, then the activation stories, then the electrical gates. The whole-epic swap is replaced because it put the Both gate before the Latch it uses. Weight now comes before Trampoline. Under the dependency rule, the Lever moves after ropes. Still open for the owner: the sound meter's source (a propagation story, merging with the Bell, or deferral); the receivers' colour source (RGB lasers); the switch electrical mode. |
| S3 | reorder | reported | **Open item (f), a Pulley/Lever loop.** Pulley (10.2) uses a Lever as its rope load in acceptance, and the Lever (10.3) needs rope sockets from 10.2. One of these integrations must be deferred. |
| C1 | H | reported | Campaign-table gaps:<br>• The electrical gates (EL-133–137) have no integration-verification task in requirements.md.<br>• The compressor, air nozzle and pressure gauge (EL-037, 042, 043) are not named in the campaign table's pneumatic row, so they have no lesson slot. |
| C2 | L | reported | The campaign coverage ledger has no row for 8 elements: EL-091 cannonball, EL-097–099 pool cue/ball/pocket, EL-100 vacuum nozzle, EL-104 firework, EL-105 missile and EL-106 impact charge. Should rows be added now (sequence-task-319)? |
| D1 | A | reported | Should a Domino spinning about one axis without tipping fire the orientation sensor? The legacy tilt sensor rejected spin about the sensed axis; the current sensor counts total rotation (CAT-023 §6). |
| P4 | O | reported | The radiation "cell" unit is read as 1/16 m (`CellScale.Metres = -4`). |
| R9 | C | reported | Legacy wound-spring self-recharge and lumped work accounting conflict with the requirement for per-source accounting. Proposed: do not carry forward. |
