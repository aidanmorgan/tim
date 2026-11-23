# Cross-batch compatibility and correctness audit of the element specs

**Date:** 23 November 2026.
**Checkout:** `spec-drafts` at published main `25df3cd5`, plus uncommitted planning work. Files were read as they stood; another session was editing campaign documents and campaign lines during the audit.
**Scope:**
- 373 element specs: 72 CAT, 241 EL, 37 TH, 19 RAD and 4 GAP.
- The 15 batch summaries and the new-identities summary.
- `owner-decisions-2026-11-22.md` (binding), `engine-conformance.md`, `engine-stories-index.md`, `campaign-coverage.md`, `docs/planning/elements/README.md` and `named-elements.md`.
- The engine section of `_bmad-output/planning-artifacts/epics.md`, `sprint-status.yaml` and the 45 delivered package declarations.
- The 16 untracked drafted contracts `_bmad-output/implementation-artifacts/spec-*.md`.

**Method:** scripted extraction over every spec, then reading only the hits.
- Extracted: package ids, directories, classes, categories, palette orders, display names, sprint keys, `ENGINE-*` names, capability rows marked missing or narrower, relative links and anchors, and prerequisite lines matched against campaign first use.
- Ports were extracted into a table and checked against delivered declarations.
- Units were checked by grep for density, pressure, temperature, Battery energy per tick, ball sizes, bores and Run length.

**Deliverable alongside this report:** the palette proposal [palette-allocation.md](../../docs/planning/elements/readiness/palette-allocation.md).

Read-only otherwise: nothing was built, tested, committed, stashed or pushed.

Each finding gives its severity, `file:line`, the defect, the evidence, and the exact replacement text or the decision needed. A systemic defect is one finding with a file list.

## Summary

| Check | Checked | High | Medium | Low | Decision needed |
|---|---|---|---|---|---|
| 1 Collisions | 373 specs; 45 delivered packages; 302 `named-*` sprint keys; ids, directories, classes, display names | 0 | 2 | 2 | 1 |
| 2 Palette | 269 placeable undelivered packages allocated; 7 categories | 0 | 1 | 2 | 2 |
| 3 Collaborations | 70 delivered ports; 263 spec port rows (95 specs); 112 typed arrows; 105 delivered-port mentions; gas, acoustic, rope, rim and signal families end to end | 1 | 2 | 3 | 1 |
| 4 Units and scales | ambient, pressure, density, Battery per tick, balls, Weight, Cannon window, plate, bores, hopper range | 1 | 0 | 3 | 1 |
| 5 Story names | every `ENGINE-*` token and backticked key in specs, summaries and plan documents; 1,202 missing/narrower rows | 0 | 2 | 4 | 0 |
| 6 Links | 14,261 relative links in `docs/planning/**`, `epics.md`, `DESIGN.md`, `AGENTS.md` | 0 | 0 | 4 | 0 |
| 7 Decisions | the eleven named decision families plus companions, re-arm and scheduling | 1 | 3 | 3 | 1 |
| 8 Closures and families | 47 named specs against their CAT or satisfying part; 5 families | 0 | 4 | 1 | 1 |
| 9 Ordering | prerequisite and dependency lines of every spec against campaign first use | 0 | 3 | 3 | 1 |
| 10 Drafted contracts | 16 untracked drafts | 2 | 2 | 2 | 0 |
| **Total** | | **5** | **19** | **27** | **8** |

## The ten most important findings

1. **7-1 (High).** Six specs give the release of a coordinate hold to `ENGINE-ASSEMBLY-JOINTS` (order 31). Index C2 gives it to `ENGINE-JOINT-KINDS` (order 14). This delays EL-224, EL-225, EL-226, EL-228, RAD-21 and TH-33.
2. **10-2 (High).** Five drafted contracts are scheduled against owner decision 22. They are a closure review, an engine row under a retired name, a family of new elements, S4, and art-solidity without its dependency.
3. **10-1 (High).** Eight drafted contracts still add a `Compile` line to the test project. Since `25df3cd5`, `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs`.
4. **4-1 (High).** EL-239 Baseball still declares 0.65 kg and 0.25 m with cosmetic seams. Specs downstream disagree: some use 0.15 kg, some 0.65 kg.
5. **3-1 (High).** Four specs wire a Signal lamp to an electrical `Supply` or `PowerIn`. The lamp has only a Signal `ActivationIn`, so those acceptance rigs cannot be built.
6. **8-2 (Medium, systemic).** Coordinator decision 27 is not applied. Seventeen named specs still carry a second declaration of a package that a CAT spec declares, and one case is inverted (CAT-062 repeats EL-194).
7. **9-1 to 9-3 (Medium), 9-4 to 9-6 (Low).** Specs depend on parts that the campaign teaches later. Examples: the Laser receiver needs the Laser, the Pool pocket claims the cue comes first, the brace needs the pivot, and the tank gauge needs the moderator tank. This disproves campaign-coverage §6's "every part's prerequisites … introduced at an earlier level". Under owner decision 24a it also inverts delivery order.
8. **8-1 (Medium).** Nine specs keep a stale harvest-era declaration beside the current one, with a "governs where they differ" clause. Their port contracts contradict: `LinkageOut` is both absent and declared in EL-156, EL-157 and EL-111.
9. **5-1 (Medium).** The re-pointing that index §5 still lists as open is not done. Rows still name the old owner or "unowned" for:
   - supply contacts (EL-061, EL-221);
   - the crank (EL-218);
   - wrap mapping and compliant admission (CAT-019, CAT-065);
   - attachment state (EL-186);
   - visible-solid part D (CAT-018, CAT-057).
10. **7-6 (Medium, decision needed).** EL-223 is being delivered as a new element before the engine programme, against owner decision 22, and the Baseball repair draft is blocked on it.

---

## Check 1 — Collisions

**Verified clean.**
- **Package ids, directories and classes.** Every id, directory and class claimed by two specs is one of the 34 expected CAT/named pairs, each pointing at one package.
  - The pairs: EL-133 to EL-152 and EL-212 to EL-214 with their CAT parts; EL-180/CAT-061; EL-187 to EL-192, EL-194, EL-196, EL-198 and EL-208 to EL-211 with delivered or CAT packages; EL-165/EL-003; TH-05/EL-055; EL-064/CAT-058.
  - No new id, directory or class collides with a delivered one or with another new one.
- **Delivered palette orders** are 1–12, 15–42 and 64–68, all unique. They are read from the declarations, not from the untracked `elements/elements.bundle.json`, which lacks `baseball`. Every delivered spec that states an order or category matches its declaration.
- **Sprint keys.** `sprint-status.yaml` has no duplicate key. It holds 302 `named-*` keys, one per named spec, plus `named-el-242`, which has no spec (see 8-4).

**1-1 (Medium) — duplicate display name "Bucket" in one category.**
- **Where:** `EL-014-water-carrying-bucket.md:16` (title "Bucket", `water_bucket`) and `EL-069-moving-bucket.md:54` (display name "Bucket", `moving_bucket`).
- **Defect:** both are Motion, so the palette would show two identical "Bucket" rows.
- **Replacement:**
  - In EL-014:16, `title "Bucket"` becomes `title "Water bucket"`.
  - In EL-069:54, `display name "Bucket"` becomes `display name "Hanging bucket"`.
- **Decision needed:** the coordinator confirms the names.

**1-2 (Medium) — palette orders taken before the cross-batch pass.**
- **CAT-007:** `CAT-007-beam_shutter.md:48` reads "category Optics, order 47 (proposed)", and `:189` makes "palette order 47 has been taken" a disqualifier. Batch C (`readiness/batch-c.md:137`) chose 47.
  - Replace in `:48` with "order allocated by the cross-batch pass (proposed 604, `readiness/palette-allocation.md`)".
  - In `:189`, delete the clause "palette order 47 has been taken (…);".
- **EL-223:** `EL-223-passive-impact-scoop.md:29` reads "Motion, "Impact scoop", order 323 (**proposed**)". The session delivering EL-223 (worktree `tim-cluster2-after-baseball`, uncommitted on `fa86969f`) declares a different id, directory and order: `passive_impact_scoop`, `elements/PassiveImpactScoop/`, `PaletteOrder: 223`. The spec and the in-flight package disagree on all three.
  - Replace in `:29` with "Motion, "Passive impact scoop", order as published by the delivering slice (in flight: 223); otherwise allocated by the cross-batch pass (proposed 130, `readiness/palette-allocation.md`)". Also set the spec's package id and directory to the published ones. Until EL-223 publishes, no other package may take 223 or 323 (palette proposal section 3).

**1-3 (Low) — EL-185 states two categories.**
- **Where:** `EL-185-releasable-assembly-joint.md:39` says "category `ElementCategory.Structure`"; `:88` says "category Motion — **proposed**".
- **Replacement:** in `:88`, change "category Motion — **proposed**" to "category Structure (section 2)". The allocation uses Structure.

**1-4 (Low) — README counts disagree with each other.**
- **Where:** `docs/planning/elements/README.md:8` says 301 named-identity specs; `:150` and `:519` say 276; `:45` says "All 348 specs". The tree holds 373 specs (72 + 301), and the ledger has 302 named identities because EL-242 has no spec.
- **Replacement:**
  - `:45`: "All 373 specs (72 CAT and 301 named-identity; EL-242 has none yet)".
  - `:150`: "301 specs, one per named identity (276 adopted on 24 October 2026 and EL-217 to EL-241 ledgered on 21 November 2026)".
  - `:519`: "With the 301 named-identity specs …".

## Check 2 — Palette

Every undelivered placeable part is allocated in [palette-allocation.md](../../docs/planning/elements/readiness/palette-allocation.md). The rule:
- The palette is grouped by category, because `ui/Workshop.cs:419` groups by `ElementCategory`.
- Delivered orders are unchanged.
- New parts follow campaign first-use order (owner decision 24a), with ties broken by identity number.
- Each category gets its own block: Motion 101–199, Goals 201–299, Power 301–399, Control 401–499, Structure 501–599, Optics 601–699, Radiation 701–799.
- Companions, connection kinds, level data, closures and TH-05 get no order (owner decision 24b).

That covers 269 packages: 80 Motion, 4 Goals, 44 Power, 37 Control, 50 Structure, 29 Optics and 25 Radiation. Every placeable part that has a spec has a first use in campaign-coverage §6 or §10b, so no part lacks a lesson.

**2-1 (Medium) — the category rule is mixed (palette proposal section 3).**
- Optics and Radiation get domain members of `ElementCategory`, owned by ENGINE-OPTICAL (`epics.md` "the optical palette category and domain flag") and ENGINE-RADIATION ("the palette category and domain flag").
- Liquid, pneumatic, acoustic and thermal parts are spread across the five existing categories by function. For example, the Water tank is Power (`EL-001-finite-reservoir.md:66`) and the Catch basin is Structure (`EL-004`).
- Light sources are split. The lasers are Optics, but the Flashlight (`CAT-029:34`), the Rope-operated light (`EL-155:37`) and the Light-charge receiver (`EL-154:37`) are Power.
- **Decision needed (coordinator):** keep function categories with only those two domain members, or add Water, Air, Sound and Heat members through ENGINE-LIQUID, ENGINE-GAS-NETWORK, Story 14.0 and ENGINE-THERMAL.

**2-2 (Low) — first-use levels missing for seven identities.**
- **Where:** campaign-coverage §6 gives chapters 11–13 only as counts per level (`campaign-coverage.md:472`), and §10b gives EL-126 to EL-132 only as ranges.
- **Inferred fit used by the allocation:** EL-126 and EL-127 at 102; EL-128 at 104; EL-129 at 113; EL-130 at 114; EL-131 at 125; EL-132 at 126.
- **Replacement:** add these seven to the §6 chapter 11–13 sentence, e.g. "chapter 11: 101 RAD-01, RAD-08; 102 EL-126, EL-127; 104 EL-128; …". The campaign editor confirms.

**2-3 (Low) — a structural consequence of the rule.**
- Delivered orders cannot move, so every new part lists after the delivered parts of its category, even one the campaign teaches first. For example, the Pool ball at level 8 lists after the Baseball at 68.
- The proposal states this. **Decision needed** only if the owner wants delivered orders renumbered.

## Check 3 — Collaborations

**Verified clean.**
- Gas port names and directions across EL-037, EL-039 to EL-043, EL-045, EL-230, EL-231, TH-17, TH-18, TH-20 and TH-35, including directional valve `PortA`/`PortB` to the pistons' `GasIn`/`ExtendIn`/`RetractIn`.
- The acoustic route `SoundOut` (EL-046) → duct (EL-048) → `SoundIn` (EL-047).
- EL-200 pitch radii equal the delivered `MechanicalRim` radii: Fan 0.14, Conveyor 0.22, Windmill 0.24, Wound spring 0.26, Clutch 0.28, Motor 0.30, Reverse gear 0.32.
- Every Level output is wired only to inputs that accept a level.
- Every pusher `RetractedOut` and `ExtendIn` reference matches the delivered Linear pusher.

**3-1 (High) — a Signal lamp is wired as an electrical load.**
- **Where:**
  - `EL-018-electronic-level-switch.md:108` ("switch `Supply` → Signal lamp `PowerIn`"), with `:112`–`:114` then asserting "the lamp is supplied";
  - `EL-030-flow-meter.md:68`, `:117`, `:119`;
  - `EL-031-volume-meter.md:100`;
  - `EL-032-pressure-meter.md:100` ("a Signal lamp on `Supply`").
- **Evidence:** the delivered lamp declares one port, `PortId.ActivationIn` with `SignalTerminal.Input` (`elements/SignalLamp/SignalLampElementDeclaration.cs:35`, `:64`). It has no `PowerIn`. `RAD-15:113` states the rule correctly: "the Signal lamp is a signal load, not an electrical one".
- **Effect:** these four parts declare only `PowerIn` and `Supply`, so the rigs cannot be built.
- **Replacement:** use "a Powered gate (CAT-051) on `Supply`" and assert "the gate's blade is raised" or "lowered" in place of the lamp being lit or dark. In EL-018:108, "switch `Supply` → Powered gate `PowerIn`".

**3-2 (Medium) — TH-19's gas ports are one-way.**
- **Where:** `TH-19-steam-piston.md:20` declares "a directed inlet `GasIn` and a directed exhaust `GasOut`".
- **Evidence:** EL-039 F3 (`EL-039-air-reservoir.md:88`) says every gas port is Bidirectional and "flow follows pressure", and coordinator decision 16 sets one fluid port contract.
- **Replacement:** "its ports are `GasIn` (Input end) into the cap chamber and `GasOut` (Output end, declared Vent when unconnected, F4) from the rod chamber, both fluid ports of the one record with role Bidirectional (EL-039 F3)".
- Also delete `TH-19:38` ("needs Batch H's closed vent list …"); F4 at `EL-039:89` already covers it.

**3-3 (Medium) — EL-181 and EL-182 misstate the delivered input roles.**
- **Where:** `EL-181-rising-edge-detector.md:44` says the condition input takes `PowerIn` "as every delivered electrical input does"; `EL-182:39`–`:42` says the same.
- **Evidence:** the delivered gates use `PortRole.FirstIn`/`SecondIn` (`elements/BothGate/BothGateElementDeclaration.cs:25`–`:26`), and so does the Linear pusher for `ExtendIn`/`RetractIn` (`LinearPusherElementDeclaration.cs:45`–`:46`).
- **Replacement:** "so the input takes `PortRole.PowerIn` bound to a consumer with no work target, the System clock's pattern (the gates' condition inputs instead bind a logic node's `FirstIn`/`SecondIn` terminals)".
- **Decision needed:** ENGINE-SUPPLY-DEMAND should state that a consumer with no work target requests nothing.

**3-4 (Low) — EL-207's rope roles.**
- **Where:** `EL-207-moving-pulley.md:75`–`:76`. `Hook` gives "Load", which is a `RopeAttachmentRole`, in the `PortRole` column.
- **Replacement:** `Tie` becomes `PortRole.TieA`, and `Hook` becomes `PortRole.TieB`, `RopeAttachmentRole.Load`, as the Impact lever does (`ImpactLeverElementDeclaration.cs:45`, `:56`).

**3-5 (Low) — EL-069's rope roles.**
- **Where:** `EL-069-moving-bucket.md:48`–`:49` uses `PortRole.Tie` with `PortRole.TieB`.
- **Replacement:** `TieA` and `TieB`.

**3-6 (Low) — TH-05 says "belted".**
- **Where:** `TH-05-friction-brake.md:95` (row H1): "a supplied default Motor belted to a supplied brake". Under owner decision 2 a belt is EL-200's new material.
- **Replacement:** "joined by the rigid mechanical connection (`Drive` → `DriveIn`) to a supplied brake". See also 4-3.

## Check 4 — Units and scales

**Verified clean.**
- Ambient temperature is 288 K.
- Pressure is 101 325 Pa absolute (`AtmosphereBoundary.cs:12`).
- The one liquid density is 16 kg/m³ (EL-001 L2).
- Battery: 120 W, so 1 J per tick at 120 Hz.
- Delivered ball masses and radii.
- The Cannon window.
- The hopper and shuttle range of 0.62–0.78 m against the balls they are fed.
- Weight sizes against mass.
- Pressure plate default 0.5 kg.

**4-1 (High) — the delivered-Baseball spec is stale and its consumers disagree.**
- **Where:** `EL-239-baseball.md:6` reads "mass 0.65 kg, radius 0.25 m, … principal inertia 0.01625", and `:8` reads "two coral-red seams … The seams are cosmetic".
- **Evidence:**
  - The owner values in `_bmad-output/implementation-artifacts/spec-repair-baseball-el-239.md:21` are 0.15 kg and 0.26 m, "the lightest ball", with "one proud band with its own collider". The seams stand 2.6 mm proud without a collider (`:73`), against visible = solid.
  - `CAT-065-trampoline.md:212` and `EL-238-selective-trap-door.md:51` use 0.15 kg.
  - `CAT-052-pressure_plate.md:205` and `CAT-062-spring.md:51` use 0.65 kg, labelled "on published main".
  - Published main does declare 0.65 kg (`elements/Baseball/BaseballElementDeclaration.cs:22`).
- **Replacement for `:6`:** "Baseball is a fixed sphere, the lightest ball: mass 0.15 kg, radius 0.26 m, principal inertia 0.004056 kg·m² on every axis, restitution 0.35, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s, ambient linear drag 0.04 /s, no buoyancy (owner, 21–22 November 2026; repair `spec-repair-baseball-el-239.md`; owner decision 22 runs it before the programme). Published main declares 0.65 kg and 0.25 m until the repair publishes."
- **Replacement for `:8`:** "One raised band, 0.01 m proud, in the Basketball's stripe pose, with its own torus collider `col_visual_band`; no seam nodes."
- **Consumers:** CAT-052:205 and CAT-062:51 add "(0.15 kg after the Baseball repair)".
- **Could not verify:** which owner record holds 0.15 kg and 0.26 m. They appear only in the repair draft and downstream specs, not in `owner-decisions-2026-11-22.md`. **Decision needed:** cite the owner record.

**4-2 (Low) — "bore" means both radius and diameter.**
- **As radius (0.65 m):** `CAT-048-pipe.md:192`, `CAT-049-pipe_bend_45.md:120`, `:157`, `CAT-050-pipe_bend_90.md:161`, `CAT-051-powered_gate.md:5`, `:207`, `EL-008-straight-water-pipe.md:30`, `EL-001-finite-reservoir.md:31`.
- **As diameter (1.3 m):** `CAT-048-pipe.md:179`, `EL-100-vacuum-nozzle.md:42`, `EL-187-basketball.md:81`, `EL-189-tennis-ball.md:96`.
- **Replacement:** write "bore radius 0.65 m" or "bore 1.3 m across" everywhere.

**4-3 (Low) — "belt" names the delivered rigid connection.**
- **Where:** EL-022, EL-055, EL-209, EL-218, EL-219, EL-225, EL-226, EL-233, EL-235 (e.g. `:59` "A belt on `DriveIn`") and EL-240; also the delivered Wound spring description (`elements/WoundSpring/WoundSpringElementDeclaration.cs:12`).
- **Defect:** owner decision 2 makes "Drive belt" (EL-200) a new, solid, slipping material, so these rows read as depending on EL-200.
- **Decision needed:** a name for the rigid connection, e.g. "rigid drive connection". Each row then uses it.

**4-4 (Low) — supply phase written as 1/120 s.**
- **Where:** `EL-180-pulse-speaker.md:49` and `CAT-061-speaker.md:80` write the supply phase as "`power_limit` × 1/120 s".
- **Evidence:** this is the delivered code. Coordinator decision 30 makes a grant "energy per tick = power limit × dt" under ENGINE-SUPPLY-DEMAND.
- **Replacement:** append "(today's code; ENGINE-SUPPLY-DEMAND makes it `power_limit` × dt, coordinator decision 30)".

## Check 5 — Story names

**Verified clean.**
- Every `ENGINE-*` name in a spec's owner or prerequisite text is one of the 37 stories that have a heading in `epics.md`, or the index table, apart from the items below.
- Every index-table story has an `epics.md` heading.
- Every engine sprint key in the index is in `sprint-status.yaml`.
- Withdrawn names that appear with "withdrawn" next to them are not findings: EL-076:178, EL-080:184, EL-100:131 and EL-076:77.

**5-1 (Medium) — re-pointing listed as still open in index §5 is not done.**
- `EL-061-indexed-carousel.md:117` and `EL-221-parallel-gripper.md:65` name ENGINE-JOINT-KINDS as the owner of a supply contact.
  - **Rule:** index C3 says ENGINE-SIGNAL-NODES owns every supply contact.
  - **Replacement:** "ENGINE-SIGNAL-NODES (the contact; index C3), reading the window / Blocked observation that ENGINE-JOINT-KINDS publishes".
- `EL-218-crank-slider.md:67`: "Owner: `ENGINE-MECHANICAL-LINKS`".
  - **Rule:** coordinator decision 29 and index C25 make the crank a rod pinned at both ends by dynamic-support hinges.
  - **Replacement:** "Owner: `ENGINE-JOINT-KINDS` — a rod pinned at both ends by dynamic-support hinges, with its own collider (coordinator decision 29); no new coupling law".
- `CAT-019-conveyor.md:81` and `:187` call the wrap mapping unowned.
  - **Rule:** coordinator decision 29 assigns it.
  - **Replacement:** "owner: ENGINE-CAPABILITY-INSTANCES (wrap mapping; coordinator decision 29)".
- `CAT-065-trampoline.md:119`, `:190`, `:207`, `:225` and `:239` call admission, deflection read and sag unowned.
  - **Rule:** coordinator decision 29 assigns them.
  - **Replacement:** "owner: ENGINE-COMPLIANT-FRICTION (stability-bounded admission, per-sample deflection read and sag binding; coordinator decision 29)". Delete the "Needs the owner and coordinator" paragraph at `:207`.
- `EL-186-temporary-bridge.md:77`: "the same two stories".
  - **Replacement:** "ENGINE-ASSEMBLY-JOINTS (attachment state and read); ENGINE-FRACTURE publishes the broken state into it (index C22)".
- `CAT-018-clutch.md:193` and `CAT-057-reverse_transmission.md:195`: "**Unowned today.** Proposed: part D of ELEMENTS-VISIBLE-SOLID-REMAINDER".
  - **Rule:** coordinator decision 33 makes repair part D real; it runs after the engine programme.
  - **Replacement:** "Owner: repair part D (coordinator decision 33), after the engine programme".
- The hold-release rows are finding 7-1.

**5-2 (Medium, systemic) — capability rows do not each name exactly one story.**
- Of the 1,202 table rows whose status cell begins "missing" or "narrower":
  - 975 name exactly one story;
  - 79 name two to four (examples: `EL-074:78` names GAS-NETWORK, IGNITION-BLAST, LIQUID and THERMAL; `TH-36:73` names ASSEMBLY-JOINTS and SIGNAL-NODES);
  - 148 name none in the row.
- **Rows that name none:**
  - Most rely on a family code (TH-04 R codes, EL-044 N codes, liquid W codes), as index §2's method allows.
  - Others read "owner below" (e.g. `EL-200-drive-belt.md:92`) or nothing at all: all 7 rows at `EL-118:73`–`:79` and `EL-119:54`–`:60`, `GAP-06:83`–`:90`, `GAP-07:71`–`:76`, `GAP-09:87`–`:90`, `EL-159:62`, `EL-160:61`, `EL-161:61`, `EL-124:60`–`:64`, `EL-125:66`–`:68`, `CAT-009:57`–`:58` and `CAT-069:67`–`:68`.
  - Thermal rows that name none: TH-01, TH-04, TH-07 to TH-12, TH-16 to TH-30, TH-34, TH-35 and TH-37, at the lines extracted on 23 November 2026.
- **False positives** (acceptance rows titled "Missing …"): CAT-018:159, CAT-042:186, EL-155:117.
- **Replacement:** each row's owner cell names its one owning story, as index §2 assigned it. For example, EL-118 and EL-119 rows get "ENGINE-SURFACE-STATE"; GAP-06 and GAP-07 rows get "ENGINE-GRANULAR"; GAP-09, EL-159 to EL-161 get "ENGINE-FRACTURE"; EL-124 and EL-125 get "ENGINE-FIELD-FORCE (scene region)"; TH rows get "ENGINE-THERMAL" unless a row is GAS-NETWORK's. A row that needs two capabilities names the governing story first and the other as "with candidates / observations from …", in the index's form.

**5-3 (Low, systemic) — 3a stories still called "proposed" or "no text in epics.md yet".**
- "No text in `epics.md` yet" at `EL-126:59`, `EL-129:54`, `EL-131:117`, `RAD-01:56`, `RAD-02:59`, `RAD-03:55`, `RAD-06:59`, `RAD-07:54`, `RAD-08:64`, `RAD-09:58`, `RAD-10:57`, `RAD-12:59` and six more RAD files (18 in all).
- "(proposed)" after ENGINE-SPATIAL-QUERIES, ENGINE-SIGNAL-NODES, ENGINE-CONNECTION-MATERIALS or ENGINE-RUN-LENGTH at 33 places, including `EL-127:44`, `:107`, `EL-128:44`, `:113`, `EL-130:114`, `EL-132:40`, `:104`, `RAD-17:150`, `RAD-18:44`, `:114`, `RAD-20:18`, `:155`, `RAD-21:18`, `:152`, `EL-155:19`, `:147` and `EL-175:19`, `:143`.
- **Evidence:** every one of these stories has text in `epics.md` (ENGINE-SPATIAL-QUERIES at `epics.md:4455`, ENGINE-SIGNAL-NODES at `:4861`, ENGINE-CONNECTION-MATERIALS at `:4951`, ENGINE-RUN-LENGTH at `:4936`).
- **Replacement:** delete "(proposed)", "both proposed" and "no text in `epics.md` yet".
- The phrase "the status line at `epics.md:4512` is to be amended" (`EL-130:114`, `EL-131:117`) points at a sensor line. Delete it, or cite ENGINE-RADIATION's *Depends on* line by heading.

**5-4 (Low) — retired names used as current names.**
- `EL-086-obstacle-reversing-walker.md:17`, `:111`, `:162` use "`ENGINE-CHARACTERS` (retitled …)". **Replacement:** "the story "Package joints on a dynamic base" (key `engine-characters`, coordinator decision 16)".
- `EL-090-steerable-blimp.md:132` and `EL-092-toy-rocket.md:106` use "`engine-axial-gas-nozzle` (Batch D)". **Replacement:** "the axial gas chamber of `ENGINE-GAS-NETWORK`".
- `EL-133`–`EL-137:17` use "drafted as `engine-supply-self-edge-cycle-rule`". **Replacement:** "drafted in `spec-engine-supply-self-edge-cycle-rule.md` as the self-edge row of `ENGINE-SIGNAL-NODES`".
- Batch summaries still use retired names as live: `batch-h.md:87`, `:167`, `:168`, `:178`, `:180`, `:199`; `batch-l.md:98`, `:103`, `:184`, `:186`; `batch-o.md:117`, `:137`; `batch-a.md:95`; `batch-m.md:123`; `new-identities.md:188`, `:191`. Add "(now …, index §3)" at first use in each summary, or leave them as dated records with a pointer to index §3 at the top of each summary.

**5-5 (Low, systemic) — `epics.md` line citations without a pinned commit have drifted.**
- 112 `epics.md:NNNN` citations in specs and summaries give no commit.
- **Evidence:** at `25df3cd5`, Story 13.1 is `epics.md:4144`. In the edited working copy it is `:4145`, and `:4144` is blank (cited by `EL-210:19`). Likewise `EL-176:20` (`:4158`), `EL-177:18`, `EL-178:18`, `EL-211:71` and the `:4434`/`:4459`/`:4512` group.
- **Replacement:** cite stories by heading anchor, e.g. `[Story 13.1](../../../_bmad-output/planning-artifacts/epics.md#story-131-flashlight-torch--collimated-laser-emitters-cat-029-cat-036)`, or pin `@25df3cd5`.

**5-6 (Low) — CAT-042 calls a delivered slice "next".**
- **Where:** `CAT-042-motor.md:214` calls `engine-tensile-drive-coupling` "the next engine slice".
- **Evidence:** it was resolved on 4 November 2026 (`deferred-work.md:226`).
- **Replacement:** "delivered by the engine slice `engine-tensile-drive-coupling` (4 November 2026)".

## Check 6 — Links

Of the 14,261 relative links checked, 6 anchors and 18 targets do not resolve. Anchors were resolved against heading slugs and explicit `<a id>` tags. Five of the 18 unresolved targets are proposed text quoted for another file (`readiness/batch-i.md:131`–`:137`, written for `invest/named-elements.md`), so they are not defects.

**6-1 (Low) — link to a renamed file.**
- **Where:** `EL-105-toy-missile.md:65` links `EL-176-amber-laser-emitter.md`, which does not exist.
- **Replacement:** `EL-176-red-laser-emitter.md#optical-conventions`.

**6-2 (Low) — CAT-028 anchors.**
- `#legacy-harvest` no longer exists. It is used at `EL-108-powered-airlift.md:125`, `EL-209-fan.md:99` and `:124`. **Replacement:** `CAT-028-fan.md#appendix-historical-evidence`.
- `EL-209-fan.md:34` links `CAT-028-fan.md#current-story-122-contract-owner-decisions-1920-november-2026`, a heading that is gone. **Replacement:** `CAT-028-fan.md#2-declaration` (not verified: confirm that §2 now holds the supply contract the link meant).

**6-3 (Low) — rotary-transmission anchor.**
- **Where:** `CAT-018-clutch.md:141` and `CAT-057-reverse_transmission.md:142` link `../../rotary-transmission-parts.md#chrome-observable-acceptance`.
- **Evidence:** the heading is "Chrome-observable acceptance (post-catalogue)".
- **Replacement:** `#chrome-observable-acceptance-post-catalogue`.

**6-4 (Low) — links to deleted code.**
- `docs/planning/invest/current-consumers.md:12`, `:43`, `:137`, `:148`, `:215`, `:224`, `:340`, `:523`, `:612`, `:645` link `../../../parts/*Part.cs`, deleted by the legacy purge.
- `DESIGN.md:417` links `engine/PartArt.cs` and `engine/PartAssistance.cs`, both deleted.
- **Replacement:** pin each to the commit that last held the file (e.g. `parts/BallPart.cs@a6c914e`, as the specs do), or remove the link.

## Check 7 — Decisions

**Verified clean.**
- No "non-solid tether".
- No delivered connection becomes slipping.
- No "collimated only" receiver.
- No next-tick allowance and no store-less light source.
- No rigid-grain substitute; GAP-06 and GAP-07 wait for ENGINE-GRANULAR.
- No Run length over 300 s.
- One cutting law and one gravity law.
- Every companion is declared not placeable.
- Every undelivered spec except EL-223 states "blocked on the engine programme (owner decision 3a)".

**7-1 (High, systemic) — release of a coordinate hold given to ENGINE-ASSEMBLY-JOINTS.**
- **Where:**
  - `EL-224-counterweighted-scoop-catapult.md:12`, `:15`, `:21`, `:25`, `:69`, `:75`, `:110`
  - `EL-225-latched-slingshot-cradle.md:26`, `:78`, `:120`
  - `EL-226-hinged-elastic-catapult.md:12`, `:15`, `:21`, `:27`, `:68`, `:74`, `:107`
  - `EL-228-zipline-signal-carriage.md:12`, `:15`, `:25`, `:67`, `:74`, `:108`
  - `RAD-21-radiation-responsive-material-latch.md:18`, `:68`, `:155`
  - `TH-33-spring-mounted-match.md:69`, `:164`
- **Evidence:** the rows read "its release commanded by the `ActivationIn` pulse, `ENGINE-ASSEMBLY-JOINTS` (commanded release …)". Index C2 (`engine-stories-index.md:157`) and its §2 table (`:104`–`:105`, naming exactly EL-224:69, EL-225:78, EL-226:68, EL-228:67, RAD-21:68 and TH-33:69) say "Governs: ENGINE-JOINT-KINDS". ASSEMBLY-JOINTS' commanded release removes only a connection or attachment (C33).
- **Effect:** each slice waits for order 31 instead of order 14.
- **Replacement:** "the hold and its release by a physical request bound to `ActivationIn`, `ENGINE-JOINT-KINDS` (holds on a coordinate and their release; engine-stories-index C2)". Delete `ENGINE-ASSEMBLY-JOINTS` from these Status, Delivery and Conformance lines.

**7-2 (Medium) — EL-225 and EL-235 do not declare their re-arm rest speed and settle time.**
- **Where:** `EL-225-latched-slingshot-cradle.md:26`, `:112` and `EL-235-jack-in-the-box.md:12`, `:15`, `:79`, `:115` declare only a re-arm distance.
- **Rule:** coordinator decisions 33 and 34 say "EL-225 and EL-235 declare the rest speed and settle time of their re-arm".
- **Replacement:** after the re-arm distance, "re-arm rest speed v_r and settle time t_s (declared values of `ENGINE-JOINT-KINDS`' re-arm at rest, coordinator decisions 33 and 34)". The rows S9 and J9 assert them.
- **Decision needed:** the coordinator gives starting values (e.g. 0.05 m/s and 0.5 s).

**7-3 (Medium) — EL-078 forbids deleting the sealed can.**
- **Where:** `EL-078-can-opener.md:53`: the sealed can "is never placed or deleted by itself".
- **Rule:** coordinator decision 34 says the player may delete a seated companion alone and restore it from its parent, and a level may place the parent without it.
- **Replacement:** "it is never placed by itself; the player may delete it alone in Build and restore it from its opener, and a level may place an opener without it (coordinator decision 34)".
- EL-084, EL-088, GAP-09 and TH-34 should gain the same sentence. They do not contradict it, but they do not cite decisions 33 and 34.

**7-4 (Low) — a displaced-density law is named.**
- **Where:** `EL-125-authored-atmosphere-preset.md:36`: "the value TH-25's displaced-density law uses".
- **Rule:** coordinator decision 28 allows no second displaced-air law.
- **Replacement:** "the value TH-25's envelope-lift binding uses (coordinator decision 28)".

**7-5 (Low) — retired nozzle name used as a capability.** `EL-090:132` and `EL-092:106` cite `engine-axial-gas-nozzle` as a capability (see 5-4).

**7-6 (Medium, decision needed) — EL-223 delivered as a new element before the engine programme.**
- **Where:** `EL-223-passive-impact-scoop.md:3` ("Another session is delivering EL-223") and `:16` ("Status | Not started"). It is the only undelivered spec without the engine-programme statement. `spec-repair-baseball-el-239.md:60` is blocked on EL-223 publishing. The in-flight work is uncommitted in worktree `tim-cluster2-after-baseball` on `fa86969f`. It edits `CuriousContraptions.tests.csproj`, which `25df3cd5` changed (see 10-1), and its package id, directory and order differ from the spec (see 1-2).
- **Rule:** owner decision 22 says new elements wait for the programme.
- **Decision needed (coordinator, or the owner if a gate is waived):**
  - either record that EL-223 continues under AGENTS.md "Stable rules" ("a slice already in review finishes under the rules it started with"), with its start date;
  - or stop it, and unblock the Baseball repair from it.

**7-7 (Low, systemic) — EL-217 to EL-241 treat S2 as pending.**
- **Where:**
  - 19 files say "S2 and S3 if published" (e.g. `EL-225:120`, `EL-241:111`);
  - `EL-227:117`, `EL-229:102`, `EL-230:103`, `EL-231:106`, `EL-232:105` and `EL-240:97` say "hand-edited lists until S2 and S3".
- **Evidence:** S2 is on main (`25df3cd5`).
- **Replacement:** "S3 (S2 is published at `25df3cd5`; no test-project list edit)".

## Check 8 — Duplicate closures and families

**Verified clean.**
- These have no delta against their satisfying part: EL-064 against CAT-058; EL-165 against EL-003; EL-187 to EL-192, EL-195, EL-202, EL-203, EL-204 and EL-208 against their CAT parts.
- EL-198 and CAT-063 follow coordinator decision 31.
- TH-05 and EL-055 agree on the thermal node values.
- EL-183 is a separate `reset_counter` package with a stated delta.
- EL-153 states its delta against CAT-038 (aperture, meter, threshold) after owner decision 9.
- Families differ only in declared values: filters, receivers, optical gates, lasers EL-176 to EL-178 against CAT-036, and metal against clear pipes (material and opacity stated).

**8-1 (Medium, systemic) — two declaration texts in one spec, one overriding the other.**
- **Where:**
  - "Current declaration … governs wherever the harvest-era subsections below differ" at `EL-110-flywheel.md:26`, `EL-111-centrifugal-governor.md:26`, `EL-156-single-lobe-cam.md:26`, `EL-157-double-lobe-cam.md:27`, `EL-207-moving-pulley.md:29` and `EL-215-damped-cushion.md:29`;
  - the same pattern at `EL-185-releasable-assembly-joint.md:29`, `EL-205-rope.md:47` and `EL-206-fixed-pulley.md:45`.
- **Evidence:**
  - `EL-156:30` says "No `LinkageOut` socket", but `:95` declares one. The same split is at `EL-157:36` against `:68`, and `EL-111:30` against `:98`.
  - `EL-111:181`'s Chrome recipe connects `LinkageOut`.
  - `EL-185:64`–`:66` gives domains "Structural" and "Activation", neither a `SimulationDomain` member, while `:36` says `ReleaseIn` is Signal.
- **Rule:** README "one current text: no section overrides another" and the conformance standard ("rewritten, not kept beside it").
- **Replacement:**
  - Delete the harvest-era subsections, from "### Bodies and shapes" through "### Variants".
  - Put in their place "### Harvest-era subsections — removed; the text is in git history." (as `EL-158:60` and `EL-216:63` already do).
  - Delete each "governs wherever …" sentence and the `EL-111:181` `LinkageOut` step.

**8-2 (Medium, systemic) — coordinator decision 27 not applied.**
- **Where:**
  - "section 2 restates it and must agree" at `EL-138:20`, `EL-143:18`, `EL-146:18`, `EL-210:18`, `EL-212:18`, `EL-213:18`, `EL-214:18`;
  - full restated declarations (line counts) in EL-133 (34), EL-138 (29), EL-143 (15), EL-146 (21), EL-180 (12; it re-derives the store and re-arm at `:48`–`:52` despite `:93`), EL-196 (23), EL-206 (35), EL-209 (34), EL-210 (23), EL-212 (15) and EL-213 (19);
  - EL-139 to EL-142 and EL-144 to EL-152 carry 5–12-row summaries.
- **Evidence:** every number compared agrees, but art node names have drifted. `EL-143:40`–`:43` uses `Visual/Frame`, `Visual/Base`, `Visual/Pane`; `CAT-055:39`–`:43` uses `RailTop`, `RailBottom`, `PostA`, `PostB`, `Base`, `Pane`, `Bar`. The probe-row label also differs: `EL-143:31`, `:33` say "M6"; `CAT-055:29`, `:32` say "M9".
- **Replacement section 2 (EL-198:22 is the model):** "The `<id>` declaration, art and bindings are [CAT-NNN section 2](CAT-NNN-….md#2-declaration); this identity adds none (coordinator decision 27, 23 November 2026)."
- EL-210's stated delta (the `supply` choice and `PowerIn`) is already declared in `CAT-029:34`, `:41`, so EL-210 takes the same pointer.

**8-3 (Medium) — decision 27 inverted for the Springboard.**
- **Where:** `CAT-062-spring.md:45` says the launch output is "specified in EL-194 section 2, whose values this repeats"; `EL-194-springboard.md:41`–`:42` holds the authoritative values.
- **Replacement:**
  - In CAT-062:45, delete "specified in [EL-194 section 2], whose values this repeats" and keep the values as the declaration.
  - EL-194:41–42 becomes "Launch sensor and `ActivationOut`: [CAT-062 section 2](CAT-062-spring.md), "Launch output" (coordinator decision 27)."

**8-4 (Medium, decision needed) — EL-242 has no spec.**
- **Where:** `invest/named-elements.md:2119` ledgers EL-242, and campaign coverage gives it level 52. But `EL-072-curved-metal-ball-pipe.md:16`–`:17` still calls it "a proposed new ledger identity … once the ledger admits it" and declares `metal_pipe_bend_45` inside EL-072. `sprint-status.yaml:720` has `named-el-242`.
- **Replacement for EL-072:16, last sentence:** "The 45° metal bend is [EL-242](../invest/named-elements.md#element-242), its own ledger identity (coordinator decision 27), delivered with this one as the "Metal bends" Family slice."
- **Decision needed:** when EL-242's spec is written; `named-elements.md:7` says at slice planning. Until then the 45° declaration stays in EL-072, as the ledger says.

**8-5 (Low) — a possible duplicate not recorded as a candidate.**
- **Where:** `EL-053-rotary-to-linear-converter.md:177` says EL-219 "is a variant of this package (toothed-rail art) or a Duplicate closure candidate" unless its probe finds a delta beyond the attachment carriage. `EL-219-rack-and-pinion-slide.md` disposition is "single element".
- **Replacement:** EL-219's disposition records "Duplicate-closure candidate against EL-053 unless the attachment carriage (ENGINE-ASSEMBLY-JOINTS) is its only delta; decided at EL-219's capability probe".

## Check 9 — Ordering (decisions 3a, 18, 22, 24a)

Under owner decision 24a, element slices are delivered in campaign first-use order. So a spec whose prerequisites include a part the campaign teaches later is out of order unless it says so. The levels below come from campaign-coverage §6.

**9-1 (Medium) — the Laser receiver depends on the Laser.**
- **Where:** `CAT-038-light_receiver.md:202`: "Prerequisites, in order … (2) Laser CAT-036 (Story 13.1)".
- **Evidence:** CAT-038 is first used at 51 with the Flashlight; CAT-036 is first used at 51d.
- **Decision needed:**
  - either the receiver's rows that need a collimated source run in the Laser's slice, and CAT-038's prerequisites name only the Flashlight (CAT-029, owner decision 9: one receiver law);
  - or the campaign introduces the Laser at 51 with the receiver.
- **Proposed:** the first, as "(2) Flashlight CAT-029 (owner decision 9); rows needing a collimated beam run in the CAT-036 slice".

**9-2 (Medium) — the Pool pocket claims the cue comes first.**
- **Where:** `EL-099-pool-pocket.md:157`: "EL-097 Pool cue (planned before it)".
- **Evidence:** EL-099 is first used at 8 and EL-097 at 44a.
- **Replacement:** "EL-097 Pool cue (taught later, at 44a; the pocket's own rows use a dropped or ramped ball)".

**9-3 (Medium) — the brace needs the pivot.**
- **Where:** `EL-113-structural-brace.md:124`: prerequisites "… then EL-112; then EL-114".
- **Evidence:** EL-113 is first used at 4; EL-114 Placeable pivot at 9.
- **Decision needed:** move the brace's introduction to 9 or later, or make its rows independent of the pivot.
- **Note:** campaign-coverage §6's "every part's prerequisites named in its spec … are introduced at an earlier level" (`campaign-coverage.md:476`) is false for 9-1 to 9-6.

**9-4 (Low) — radiation, liquid and character dependencies taught later.**
- `EL-130-tank-level-transmission-gauge.md:114` lists RAD-17 among its prerequisites. EL-130 is at about 114; RAD-17 is at 126.
- `EL-088-fish-tank-lure.md:153` lists EL-082 (97) and EL-087 (98); EL-088 is at 97b.
- `EL-083-escaping-character.md:143`: row B9 needs EL-087 (98) "in the same slice"; EL-083 is at 97.
- `EL-027-canal-lock-chamber.md:111`, `:131`: rows K5 and K6 need EL-028 (67a); EL-027 is at 66a.
- **Replacement:** each row names its later slice. For example: "Rows K5 and K6 run in the EL-028 slice (taught at 67a)"; "RAD-17 is needed only by row(s) … which run in the RAD-17 slice". Alternatively, the campaign reorders these pairs.

**9-5 (Low) — the sound order conflicts with the campaign.**
- **Where:** `EL-046-listening-horn.md:164`: "In the sound order it follows EL-044, EL-179, EL-050, EL-051 and EL-049".
- **Evidence:** EL-046 is at 78a; EL-049, EL-050 and EL-051 are at 79a.
- **Replacement:** "In campaign order (owner decision 24a) it follows EL-044 and EL-179 (75b); EL-049 to EL-051 follow it (79a)", provided EL-046 needs none of them. Otherwise the campaign reorders.

**9-6 (Low) — row-level dependencies within one chapter.**
- `EL-005-liquid-funnel.md:126`: F2 needs EL-012 (62a); EL-005 is at 61b.
- `EL-010-water-elbow-90.md:116`: E2 needs EL-012 (62a); EL-010 is at 62.
- **Replacement:** "Row F2 / E2 runs in the EL-012 slice".

## Check 10 — Drafted contracts (untracked `_bmad-output/implementation-artifacts/spec-*.md`)

On main `25df3cd5`, `CuriousContraptions.tests/CuriousContraptions.tests.csproj:77` holds `<Compile Include="*.cs" />`, so every top-level test source compiles; `EnableDefaultCompileItems` stays false at `:7`. Main's full round is 7,294 passed.

**10-1 (High, systemic) — obsolete "add a `Compile` line" steps.**
- **Where:**
  - `spec-closure-review-electrical-gates.md:71`, `:111`, `:150` (cites `csproj:156`, which no longer exists)
  - `spec-early-level-proof-batch-1.md:76`, `:152`
  - `spec-engine-goal-evaluation.md:103`, `:175`
  - `spec-engine-supply-self-edge-cycle-rule.md:95`, `:140`
  - `spec-family-ball-presets.md:137`, `:205`
  - `spec-repair-baseball-el-239.md:152`
  - `spec-s4-catalogue-conformance-suite.md:56`, `:103`, `:131`
  - `spec-s5-generate-wire-accessors-in-build.md:71`, `:125`, `:133`
- **Special case:** S4's condition "only while `EnableDefaultCompileItems` is false (line 7)" is still true on main, so it would wrongly trigger the edit.
- **Replacement:** delete every Compile-line task and file entry, and write: "No test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`)." In `spec-family-ball-presets.md:205`, also drop the `CSharpStaticGateTests` list lines that S3 removes.

**10-2 (High) — scheduled against owner decision 22.**
- `spec-closure-review-electrical-gates.md:60`, `:66`: closure reviews wait for the programme. **Replacement status:** "Blocked on the engine programme (owner decision 22) and on the self-edge row of `ENGINE-SIGNAL-NODES`."
- `spec-engine-supply-self-edge-cycle-rule.md:59`, `:96` is a "single engine slice" under a retired name, with a task to add key `engine-supply-self-edge-cycle-rule`. **Replacement:** retitle it as the self-edge row slice of `ENGINE-SIGNAL-NODES` (key `engine-signal-nodes`, already in `sprint-status.yaml` with "self-edge row drafted"), "waits for the engine programme (owner decision 3a)", and delete the new-key task.
- `spec-family-ball-presets.md:66` is blocked only on S7. **Replacement:** "Waits for the engine programme (owner decision 22), then S7; delivered at its campaign first-use position (owner decision 24a; EL-076 and EL-077 at level 2b)."
- `spec-s4-catalogue-conformance-suite.md:47`–`:49`: S4 is not on decision 22's list. **Add:** "waits for the engine programme (owner decision 22)".
- `spec-engine-art-solidity.md:48`–`:52` is "blocked only on `ELEMENTS-VISIBLE-SOLID-REMAINDER`". **Replacement:** "blocked on `ELEMENTS-VISIBLE-SOLID-REMAINDER` A–C and on the engine programme, including `ENGINE-CAPABILITY-INSTANCES` (index order 37 depends on 8)."

**10-3 (Medium) — an owner question that is already decided.**
- **Where:** `spec-elements-visible-solid-remainder-c.md:147`–`:152` still puts "Decision O1 … to the owner", with an unnamed engine slice.
- **Replacement:** "Decided by owner decision 5: if C8 shows the 0.0625 m picture is momentary, it is recorded as a named defect owned by `ENGINE-ONE-WAY-INSTALL` (first committed sample on construction and Reset, index §2) and part C proceeds."

**10-4 (Medium) — stale baselines and totals.**
- `spec-s3-remaining-per-package-lists.md:183`, `:187`, `:188` ("738 from 730", "7,254 from 7,246", "140 from 134 on `8d1d68aa`").
- `spec-repair-baseball-el-239.md:58`, `:68`, `:153`, `:201` (`fa86969f`; the total is unchanged, but the cited lines moved).
- `spec-early-level-proof-batch-1.md:53`, `:64`, `:77` ("63 levels: 15 Playable, 48 Planned"; campaign-coverage §2 now keeps 34 and removes 29, so the batch must exclude removed levels).
- `8d1d68aa` measurements in `spec-s5:63`, `spec-s6:50`, `spec-s4:47`, `:77`, `spec-engine-goal-evaluation.md:62`, `spec-engine-art-solidity.md:48`, `:85` and `spec-closure-review-electrical-gates.md:43`, `:91`.
- **Replacement:** "Baseline: published main at start (at drafting `25df3cd5`: full round 7,294); every expected total is baseline + delta, re-measured at replanning."

**10-5 (Low) — `spec-elements-visible-solid-remainder-c.md:129`: "a belt is not a collider".**
- **Replacement:** "the delivered rigid drive connection is drawn as a schematic and has no collider (owner decision 2, coordinator decision 25)".

**10-6 (Low) — `spec-s7-coverage-named-identity-admission.md:119` adds S7 and S8 entries that already exist.**
- **Evidence:** they are at `docs/planning/invest/delivery-batches.md:308` and `:314`.
- **Replacement:** delete the task.

## What could not be verified

- **Campaign lines.** Campaign coverage was read at 18:03 on 23 November 2026 while another session edited it. The first-use levels, the palette sort and check 9 must be re-run if §6 changes.
- **Prose-only ports.** Ports written only in prose (many RAD, TH and GAP specs, and EL-053 to EL-070) were checked only where another spec names them. Optical and radiation couplings are spatial and have no ports. Port positions were not checked against art.
- **Derived numbers.** Not every derived number was recomputed (optical power chains, thermal time constants, acoustic levels). Delivered Bowling ball and Basketball restitution, cited by the ball-preset specs, were not read from their scenes.
- **Run length.** Rows longer than 30 s were checked by pattern, not row by row, for a stated Run length.
- **Baseball values.** The owner record for 0.15 kg and 0.26 m (4-1), and the start date of the EL-223 session (7-6).
- **Capability row ownership.** Whether each of the 148 capability rows that name no story maps to the story index §2 assigned it. The index's counts were taken from an extraction this audit could not reproduce row for row; this audit counted 1,202 rows against the index's 1,192.
- **EL-242** has no spec, so none of checks 3, 4 or 8 could be applied to it.
