# Batch D readiness: Epics 12 and 14 (9)

> **Current pointers (cross-batch pass, 23 November 2026).** This summary is a dated record. Retired story names used below map to their current stories in the [engine stories index](engine-stories-index.md#3-retired-and-renamed-names) section 3; palette orders are the [palette allocation](palette-allocation.md)'s (coordinator decision 36); `epics.md:NNNN` citations are as of this summary's date (the specs cite stories by heading).

Written 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa` plus uncommitted planning drafts), and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-d.md`) and the owner's decisions of 22 November 2026. All nine catalogue specs are now one current text each: sections 1 to 9, an "Engine conformance" section, and a closing appendix "Historical evidence" that holds the legacy harvest as evidence only. The legacy Declaration, Engine capabilities and Acceptance outline sections and the "questions as asked" lists were removed. No product, test, ledger, requirement, epic, design or contract file was changed.

**Propagation repair, 22 November 2026.** All nine specs and this summary were brought up to [owner-decisions-2026-11-22.md](owner-decisions-2026-11-22.md) (owner decisions 0–20, coordinator decisions 16 and 21) and the engine design audit (`docs/planning/elements/readiness/engine-design-gap.md`). Every spec now says it is **blocked on the engine programme (owner decision 3a)** in its status line and Prerequisites and names the 3a or decision-16 stories it needs; retired or coined names are replaced (section 8). Hand-backs applied are listed in section 8.

**Second-audit repair, 23 November 2026** (`docs/planning/elements/readiness/audits/reaudit-group-1.md`; coordinator decisions 25–27). CAT-010 and CAT-061 were Ready with fixes; every finding is applied and both stay blocked on the engine programme (owner decision 3a). CAT-010: the continuous prismatic-travel presentation driver is owned by `ENGINE-GAS-NETWORK` (engine design audit section 5 row and section 6 item 13); the plate's 8 N preload needs a declared spring rest position 0.4 m beyond the upper stop, owned by `ENGINE-JOINT-KINDS` (counts 4/0/3/2); new row B13 bounds the burst's work by the strike; this spec's rows are the acceptance authority and the 12.4 contract is re-pointed to them. CAT-061 is the single `speaker` declaration text (coordinator decision 27): it adopts EL-180's Ready lamp (7 bindings), seats its foot at y −0.84, says "powered" rather than "granted", and rewrites rows 2, 3 and 10 so each can fail (row 3: a Basketball dropped 1.25 m on an Impact switch, which latches once per Run, gives exactly one pulse) and adds row 14 (cues follow only committed emissions). EL-180 is a Duplicate closure of CAT-061.

## 1. Readiness table

Counts are present / present, untested / narrower / missing, as marked in each spec's section 4. Palette orders of the five planned parts are allocated by the cross-batch pass; this batch picks no number.

| Identity | Name | Disposition | Package id | Counts | Owning engine stories | Prerequisites | Collaborators | Tier | Conformance | Ready to plan a slice |
|---|---|---|---|---|---|---|---|---|---|---|
| CAT-064 | Tennis ball | delivered (`15c8681d`), palette 64; EL-189 is a closure candidate of it | `tennis` | 5/0/0/0 | `ENGINE-ISLANDS-SLEEP`; `ENGINE-CAPABILITY-INSTANCES` (payload mask) | the engine programme (3a); part B2 after part B | Fan, Receiver, Impact switch, Pressure plate, Bellows | repair part B2, the flush seam print | conforms with a fix pending (the printed seam is drawn 3 mm proud; part B2 draws it flush with no collider, owner decision 24i) | repair B2 runs before the programme (owner decision 22); the "Cargo" closure review (one Tennis test with `drag` 0) is blocked on the engine programme (3a) |
| CAT-003 | Balloon | delivered (`4424da13`), palette 65; EL-208 is a closure candidate | `balloon` | 7/0/0/0 | `ENGINE-SPATIAL-QUERIES`; `ENGINE-CONTACT-OBSERVATIONS`; `ENGINE-ISLANDS-SLEEP`; `ENGINE-CAPABILITY-INSTANCES`; `ENGINE-GAS-NETWORK` (sphere-only store, E10); `ENGINE-CONNECTION-MATERIALS` re-runs the tether rows | the engine programme (3a); part C after B and B2 | Fan, Wall, Rope anchor, Pulley, Bowling ball, Bellows | engine tier for repair part C | conforms with a fix pending (part C's table row and its O1, the first-drawn envelope) | blocked on the engine programme (3a); then part C and the "Cargo" closure review (two Balloon rows, now spec rows 10 and 11) |
| CAT-028 | Fan | delivered (`4378410e`), palette 66; EL-209 closes as its Electrical and Shaft modes | `fan` | 8/0/0/0 | `ENGINE-SPATIAL-QUERIES` (airflow line of sight); `ENGINE-ISLANDS-SLEEP` (continuous-work trials); `ENGINE-SUPPLY-DEMAND` (a Motor on the shaft) | the engine programme (3a) | balls, Balloon, Wall, pipes, Impact switch, Battery, Motor, Windmill, Wind chimes | engine tier for repair part A | conforms with a fix pending (post, ring, arrow; part A) | blocked on the engine programme (3a); then repair A and EL-209's closure review |
| CAT-070 | Windmill | delivered and published (`47e01bdb`), palette 67 | `windmill` | 6/0/0/0 | `ENGINE-SPATIAL-QUERIES`; `ENGINE-ISLANDS-SLEEP` | the engine programme (3a); part C after B and B2 | Fan, Conveyor, Reverse transmission, Motor, Wall, Bellows | engine tier for repair part C | conforms with a fix pending (output wheel; part C) | blocked on the engine programme (3a); then repair C |
| CAT-010 | Bellows | single element; Story 12.4 in flight in another session, blocked | `bellows` | 4/0/3/2 | `ENGINE-GAS-NETWORK` (axial chamber and nozzle, directional damping, stroke observation and the continuous prismatic-travel presentation driver: audit section 5 row and section 6 item 13); `ENGINE-JOINT-KINDS` (the plate spring's declared rest position, 8 N preload) | the engine programme (3a), in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-ISLANDS-SLEEP`; `ENGINE-GAS-NETWORK`; the gas draft's two open design answers; `ENGINE-JOINT-KINDS` | Bowling ball, Tennis ball, Balloon, Windmill, Conveyor, Wall, Wind chimes | all six layers (starting rules) | conforms on paper | blocked on the engine programme (3a), then `ENGINE-GAS-NETWORK`; several values are defined only when its contract passes planning |
| CAT-009 | Bell | single element, Story 14.2 | `bell` | 3/0/3/5 | Story 14.0; `ENGINE-SPATIAL-QUERIES`; `ENGINE-CONTACT-OBSERVATIONS` | the engine programme (3a); 14.0 with gaps 11, 12, 13, 14, 15, 17 and the omnidirectional pattern | balls, Weight, Wall, Sound meter | data-only | conforms on paper | blocked on the engine programme (3a), then 14.0 |
| CAT-060 | Sound meter | single element, Story 14.1; family "Sound meters" with EL-044 | `sound_meter` | 4/0/2/4 | Story 14.0; `ENGINE-SIGNAL-NODES` (threshold node and level-following contact); `ENGINE-SPATIAL-QUERIES` | the engine programme (3a); `ENGINE-SIGNAL-NODES`; 14.0 with gaps 3, 11, 12, 13, 14; Story 14.2 | Bell, Battery, Signal lamp, Powered gate, Hold timer, Counter, Wall | data-only | conforms on paper | blocked on the engine programme (3a), then `ENGINE-SIGNAL-NODES`, 14.0 and 14.2 |
| CAT-061 | Speaker | single element, Story 14.3; the single `speaker` declaration (coordinator decision 27); EL-180 is its Duplicate closure, closed by rows 1–14 | `speaker` | 5/0/2/5 | Story 14.0; `ENGINE-SPATIAL-QUERIES` | the engine programme (3a); 14.0 with gaps 4 (plus one clause), 11, 12, 13, 14, 15; Stories 14.2, 14.1 | Battery, System clock, Impact switch, Sound meter, Wall | data-only | conforms on paper | blocked on the engine programme (3a), then 14.0 |
| CAT-069 | Wind chimes | single element, Story 14.4 | `wind_chimes` | 3/1/2/5 | Story 14.0; `ENGINE-JOINT-KINDS` (spherical joint); `ENGINE-SPATIAL-QUERIES`; `ENGINE-CONTACT-OBSERVATIONS`; `ENGINE-ISLANDS-SLEEP`; `ENGINE-RUN-LENGTH` (row 8, 60 s) | the engine programme (3a); `ENGINE-JOINT-KINDS`; Story 12.2; 14.0 with gaps 11, 12, 13, 14, 17 and the omnidirectional pattern; Stories 14.2, 14.1; Story 12.4 for one row | Fan, Bellows, balls, Sound meter, Counter, Wall | data-only only if both engine pieces land first | conforms on paper | blocked on the engine programme (3a), then `ENGINE-JOINT-KINDS` and 14.0 |

By disposition: 4 delivered, 5 to deliver as single elements (1 in flight). By readiness: all 9 are blocked on the engine programme (owner decision 3a); after it, 4 delivered parts have drafted repairs and closure reviews outstanding and 5 wait for further engine stories.

Conformance: 0 conform outright; 4 conform with a fix pending (the delivered parts; each fix is a drafted product repair, parts A, B2 and C); 5 conform on paper and cannot be built conformingly until their engine stories exist; 0 do not conform. A search of `engine/`, `ui/` and every `CuriousContraptions.*` runtime project found no code that names or special-cases any Batch D part. Island sleeping is not implemented; every spec says so and names `ENGINE-ISLANDS-SLEEP` as its owner where a body of the part can rest.

## 2. Decisions recorded

**Owner decisions, 22 November 2026, applied.**

| Spec | Decision |
|---|---|
| CAT-070 | The blades collide, as delivered; DESIGN.md is updated to match |
| CAT-061 | Tone bars are printed, one to three by tone; DESIGN.md is updated to match |
| CAT-009 | The clapper swings and the shell stays still; the clapper is shortened to end inside the shell |
| CAT-069 | The chimes hang on thin dark solid rods (four hangers and the pendulum rod, radius 0.02 m). The tubes-to-cap simplification, which would have removed four colliders, was set aside for that reason |
| CAT-003 | The string keeps its non-solid exception (owner decision 1); no hose, duct, rope, cable, belt or chain inherits it. If part C's test confirms the 0.0625 m first-drawn envelope is momentary, it is a named defect with an owning engine slice and part C proceeds (owner decision 5; no 3a story owns the defect yet, section 8) |
| all | Engine order (owner decision 3a): every spec is blocked on the engine programme (`DOC-ENGINE-STATUS`, `ENGINE-SPATIAL-QUERIES`, `ENGINE-CONTACT-OBSERVATIONS`, `ENGINE-SENSOR-VOLUMES`, `ENGINE-ISLANDS-SLEEP`, `ENGINE-ONE-WAY-INSTALL`, `ENGINE-SUPPLY-DEMAND`, `ENGINE-CAPABILITY-INSTANCES`, `ENGINE-TICK-ALLOCATION`, `ENGINE-SOA-SIMD`) and names the stories that own its gaps; no spec designs around a gap |

**Coordinator decisions, 22 November 2026, under the owner's standing delegation.**

| Spec | Decision | Reason |
|---|---|---|
| CAT-061 | One `speaker` package with a 2 J pulse store, initially empty, charged at 10 W; an emission needs the store full and `PowerIn` powered on the serving tick (the supply route's powered predicate; "granted" was unsatisfiable for a full store, second audit); re-arm is the refill time. Rows 1 and 5 restated. EL-180 closes inside the Speaker slice with that store | One supply phase grants at most power limit × 1/120 s (1 J at 120 W), so a 2 J debit in one tick cannot be paid; a store-gated emitter is the family's general rule (EL-044 N5) |
| CAT-010 | One jet model only: the nozzle law of `ENGINE-GAS-NETWORK` (engine design audit, section 6 item 13). The legacy plate-speed source is deleted. `reach` and `width` are the cone; `force` bounds the push; its mapping to nozzle values is defined when that contract passes planning, and the spec is blocked on it. Held compression is spring plus chamber pressure, with no depth asserted | Two jet models in one declaration are two rules for one behaviour; the requirement row excludes the legacy model |
| CAT-010 | The fold colliders stay two boxes with the scaled accordion; the slice adds a general matcher class to the art test: bound art that stays inside its listed colliders at both ends of its binding's travel | DESIGN.md and the 12.4 contract ask for an accordion that follows travel; the delivered matcher would otherwise leave both boxes unmatched |
| CAT-009, CAT-069 | The contact-excited emitter is N14, Story 14.0 gap 17 | EL-044 already uses N13 and gaps 1 to 16 |
| CAT-009 | Tick order (acoustic, Story 14.0 to confirm): the contact commits in the substeps of tick t; the emission is created in the acoustic field phase of tick t + 1 | `docs/gpu-f32-physics.md` draws that phase before the mechanical substeps. It is not the optical solve, which runs after the last substep (coordinator decision 16; EL-176 section 10) |
| CAT-060, CAT-061 | Socket spheres are seated in the cabinet faces: meter ports at (∓0.80, −0.5, 0) and (0.80, 0.45, 0); speaker ports at (−0.625, 0, 0) and (0, 0.75, 0) | A drawn solid may not hang clear of its part |
| CAT-069 | The requirement's "cap miss" control is restored; only the four tube colliders excite | It had been dropped |
| five planned | Palette order: allocated by the cross-batch pass | Batches claimed overlapping numbers |
| CAT-003 | The consequences of repair part C are recorded in the spec | It changes the first-drawn size |

**Author decisions kept (judged defensible by the audit):** strength from striker momentum; hollow shell and round crown; emitter origin at a declared point; omnidirectional meter; lamp through `ActivationOut`; flush rim, bars, pad and mouth; ambient drag and no angular drag; category Control; durations in seconds; bell tone bars; one chime strength law with two reference momenta chosen by a generic body target set.

## 3. Needs the owner

Nothing. The four DESIGN.md conflicts and the strings-or-rods question were decided by the owner on 22 November 2026.

## 4. Proposed text for documents this batch may not edit

**DESIGN.md (the owner has decided the content; exact text offered for the plan-document update).**

- "Windmill": delete "blades are not individually simulated striking surfaces"; the sentence becomes "The fixed guard, hub and blades are collision geometry."
- "Sound speaker": "Two raised bars identify the default mid tone" becomes "Printed bars identify the tone: one, two or three for Low, Mid, High."
- "Impact bell": "Impacts excite a damped rocking motion without snapping the pose on repeat hits or moving the collision proxy" becomes "On a ring the clapper swings and settles inside the shell; the shell does not move."
- "Wind chimes": "dark suspension strings" becomes "thin dark suspension rods"; "The sail and clapper visibly swing together" is kept.

**Story 14.0 (`epics.md`), added acceptance.** EL-044's gaps 3, 4 and 11 to 15 also apply.

- Pattern: "**And** an emitter declares its pattern as a closed enum, Cone with a declared half-angle or Omnidirectional; an omnidirectional occurrence has no direction."
- Gap 17, contact excitation (N14): "**And** a declared emitter may be excited by a qualified contact: on declared colliders, from a declared body target set, at or above a declared approach speed, reusing the delivered contact qualification; strength = clamp(striker body mass × approach speed ÷ a declared reference momentum, a declared floor, 1); emitters may share one declared minimum interval in seconds; contacts committed in one tick yield one emission per interval group in the next tick's field phase, the strongest, ties to the lower emitter id; a resting or separating contact emits nothing. A package may declare several emitters, each with a fixed tone and its own excitation rows."
- Bounds, consistent with design contract D5 (no arbitrary population ceiling): "**And** occurrences in flight per emitter are bounded by its declared minimum interval and lifetime; emitter, receiver and occurrence tables are sized at Run from the compiled declarations, with no fixed scene ceiling; per tick, reception work is proportional to occurrences whose wavefront covers a receiver within the declared range, found through the broadphase, never to emitters × receivers of the whole scene." This replaces this file's earlier proposal of a stated scene bound, and conflicts with EL-044's "a declared capacity and a typed refusal beyond it", which Batch H should change.
- Gap 4, one added clause: "and requires its declared supply input granted on the serving tick".

**Stories 14.3 and 14.4.** 14.3: add "14.1 (the meter that observes it)" to its "Depends on" line. 14.4: add "*Depends on: the engine programme (owner decision 3a); Stories 12.2 (Fan), 14.0, 14.2, 14.1; the spherical joint of ENGINE-JOINT-KINDS; ENGINE-RUN-LENGTH for the 60 s calm-and-rewind row; row 11 needs Story 12.4.*" Wording: 14.2 "struck at an approach speed of 0.8 m/s or more" in place of "kinetic energy exceeding threshold"; 14.4 "the clapper strikes a tube" in place of "the chime tubes strike each other". Story 14.0 also takes its reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` and its contact excitation from `ENGINE-CONTACT-OBSERVATIONS`' indexed lookup (engine design audit, section 6 item 13).

**ENGINE-JOINT-KINDS (owner decision 3a; coordinator decision 16 gives it the spherical joint), proposed text:** "a spherical joint between two bodies of one package, with declared anchor frames, bearing damping and disabled mutual collision; its moving body is a declared child body everywhere a slider's or hinge's is (ambient load, airflow receiver, excitation rows, body presentation, the art test's moving pool)." Acceptance: "**And** a body on a declared spherical joint swings in any direction about the anchor, holds its anchor within the game-grade envelope, comes to rest under declared damping, and Reset restores the authored pose." The slice's probe first checks whether a short declared tensile span gives the same pendulum from a delivered capability. The engine audit also lists EL-115 Linkage connector as a consumer.

**Story 12.4 and the nozzle's one owner.** Replace Story 12.4's "Depends on" line with: "Depends on: Stories 6.4, 11.1, 12.2 and 12.3 (delivered); the engine programme (owner decision 3a); `ENGINE-GAS-NETWORK` with the axial gas chamber on a prismatic coordinate, the directional nozzle with ambient refill, prismatic directional damping and the prismatic stroke observation and the continuous prismatic-travel presentation driver; and `ENGINE-JOINT-KINDS` with a declared spring rest position." **`ENGINE-GAS-NETWORK` is the one owner of the chamber and nozzle law** (engine design audit, section 6 item 13, which gives it the axial chamber, nozzle, prismatic damping and stroke observation); the separately named slices of the earlier draft are not used. Batch H's specs (EL-042 and the pneumatic family) and this batch cite the same story, so there is one law. The directional damping must not add a defaulted second field: existing packages are edited to declare both coefficients. The travel presentation driver is `ENGINE-GAS-NETWORK`'s too (second audit, 23 November 2026; CAT-010 section 4).

**Other batches' specs.**

- EL-180: done; since the second audit its section 2 points to CAT-061 and it is a Duplicate closure.
- EL-044 must use the meter's seated port positions.
- A later slice, with the first `ENGINE-GAS-NETWORK` consumer, adds a `GasOut` Fluid output on the Bellows's chamber so it can charge a reservoir (sequence-task-393, EL-039); recorded in CAT-010 section 7.

**Chrome criteria.** Stories 12.1, 12.4 and 14.0 to 14.4 each still end with a Chrome e2e file passing; replace with "Browser proof is post-catalogue."

**`requirements.md`.** CAT-064, CAT-003 and CAT-028 are delivered and published but unticked; CAT-070's note still says publication is pending: replace with "Delivered and published (`47e01bdb`); browser qualification pending." CAT-009's checkbox is ticked by Story 14.1 (its meter row) and CAT-060's by Story 14.3 (its wrong-facing row).

**Ledger and `delivery-batches.md`.** EL-189, EL-208 and EL-209 entries still say "Backlog"; each is delivered through its CAT package and should point to its closure review. EL-208 moves from section 2c to a closure candidate, with two rows to add (an occluded jet on a Balloon; a heavier Balloon ruptures on the ceiling that holds the default), and EL-189's closure adds one Tennis row (`bounce` 0.14, `drag` 0). Section 1c should list the acoustic run after the engine programme and Story 14.0: CAT-009, CAT-060 (family with EL-044), CAT-061 (with EL-180), CAT-069.

**`docs/planning/elements/README.md`.** "How to read a spec" describes six sections with a Legacy harvest in the middle; the nine Batch D specs now have sections 1 to 9, Engine conformance and a closing appendix.

**Lesson-fixture story S8** is now defined in [`delivery-batches.md`](../../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) and lands after the engine programme (owner decision 22).

## 5. Contradictions found and their state

- DESIGN.md against the delivered Windmill, and against visible = solid for the Speaker, Bell and Wind chimes: decided by the owner; text in section 4.
- Legacy colliders larger than their art (Bell sphere, Wind chimes cap box): not carried forward.
- EL-044's occurrence capacity with a typed refusal against design contract D5: section 4, bounds.
- Two owners for the nozzle law (Batch H's additions to `ENGINE-GAS-NETWORK` and the 12.4 prerequisite): resolved by the engine design audit, which gives the law to `ENGINE-GAS-NETWORK` (section 4).
- The brief names `elements/Tennis/`; the package directory is `elements/TennisBall/` (id `tennis`).

## 6. Compatibility checks inside the batch

- Package ids and catalogue keys are distinct: `tennis`, `balloon`, `fan`, `windmill`, `bellows`, `bell`, `sound_meter`, `speaker`, `wind_chimes`.
- Port ends agree: meter `Supply` (Electrical out) to Powered gate `PowerIn`; meter `ActivationOut` (Signal pulse) to Signal lamp, Counter or Hold timer `ActivationIn`; Battery `Supply` to meter and Speaker `PowerIn`; System clock or Impact switch `ActivationOut` to Speaker `ActivationIn`; Windmill `Drive` to Conveyor; Motor to Fan `DriveIn`.
- Units agree: kg, m, s, N, J, W, m/s; sound speed 12 m/s, range 8 m, level 0 to 1 for the four sound parts and EL-044; contact threshold 0.8 m/s for Bell, chime payload strikes and the Impact switch default. The Fan's flow is 12 m/s; the Bellows no longer states a flow speed.
- Ordering: Bell before Sound meter before Speaker; each cross-slice row names the later slice that runs it and ticks the checkbox. Wind chimes after Fan, meter and Bell; one chimes row and one Bellows row wait for whichever of those two lands second.

## 7. Not done, and not verified

- No build or test was run. Test bodies were located by name and line, not read in full.
- The Historical evidence appendices restate each harvest row briefly with its original citations; the long legacy fact text of the earlier specs was not kept. It remains in git history at `8d1d68aa`.
- The engine design audit (`docs/planning/elements/readiness/engine-design-gap.md`) is now read; the specs name the 3a stories by key.
- Not verified: the Fan's behaviour above a Battery's power limit; whether a Balloon survives the Cannon; whether a Frustum closed by a sphere with a torus rim is admitted at the Bell's dimensions; whether an airflow receiver and an ambient load are admitted on a third joint kind's body; sail-to-tube clearance at swing; the Bellows drafts, which are uncommitted in another worktree and may have moved.

## 8. Propagation repair record (22 November 2026)

**Owners now named in the nine specs.** Island sleeping: `ENGINE-ISLANDS-SLEEP` (every spec says it is not implemented and names the story). CAT-010's chamber, nozzle, directional damping and stroke observation: `ENGINE-GAS-NETWORK` (the separately named slices of the earlier draft are gone; the in-flight draft is cited only by its file name). CAT-069's spherical joint: `ENGINE-JOINT-KINDS`. CAT-060's receiver threshold node and level-following contact: `ENGINE-SIGNAL-NODES`. CAT-028's Motor drain: `ENGINE-SUPPLY-DEMAND`. Scene-wide airflow, reception and contact scans: `ENGINE-SPATIAL-QUERIES` and `ENGINE-CONTACT-OBSERVATIONS`. CAT-003's string is one of the two non-solid strand exceptions (owner decision 1). CAT-069 row 8 states its Run length (60 s) and `ENGINE-RUN-LENGTH`. The acoustic tick order in CAT-009 and CAT-069 is marked as the acoustic phase, distinct from the optical solve.

**Hand-backs applied.**
- Batch M (`batch-m.md` item 20 and contradiction 5): CAT-003 takes EL-208's rows A6 (occluded jet) and A9 (a 4 kg Balloon ruptures on the ceiling that holds the default) as its rows 10 and 11; CAT-064's closure row states `drag` 0 (EL-189 row A2); CAT-003 and CAT-064 no longer say a resting body sleeps.
- Batch M item 15: CAT-003 records that `ENGINE-CONNECTION-MATERIALS` re-runs its tether rows when the rope gains a collider.
- Batch H (`batch-h.md`): CAT-061's four deltas (2 J pulse store, re-arm in seconds, flush rim, category Control) and CAT-060's category Control were already present; verified, no change.
- Batch K G5 / EL-176 section 10 item 6: CAT-028 now states that its uncollided art does not occlude light and optics adds no exception.

**For the coordinator (no 3a story owns these).**
1. A continuous prismatic-travel presentation driver (CAT-010's folds). Resolved by the second audit (23 November 2026): `ENGINE-GAS-NETWORK`, with the chamber and stroke observation it takes for the same part (engine design audit section 5 row and section 6 item 13).
2. The initial-projection defect of owner decision 5 (CAT-003's O1): Batch M proposes text but names no story.
3. EL-176 section 10 item 6 lists the Fan's blades as a visible-solid defect; CAT-028 keeps them decorative under the owner's decision of 21 November 2026. One of the two texts should change.

**Needs the owner.** Nothing new.

**Checklist counts over the nine specs (before → after).** Missing engine-programme line 9 → 0; retired or coined story names 7 lines → 0 (one remaining hit is the in-flight draft's file name); sleep statements not owned by `ENGINE-ISLANDS-SLEEP` 7 → 0; tether wording for a strand 1 → 0; rows over 30 s without a Run length 1 → 0; kilopascal values, a second ambient temperature, an optical irradiance scale and the package-binding heading 0 → 0.
