---
title: 'Story 7.0: Element implementation readiness before the legacy purge'
type: 'chore'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 2
baseline_commit: 'a6c914e'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/research-epic-7-legacy-inventory.md'
  - '{project-root}/docs/planning/general-engine-element-map.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epic 7 deletes everything that still holds knowledge about the unbuilt puzzle elements:
- `reference/`;
- the probe tools, including `tools/Campaign`;
- 416 uncompiled tests;
- `engine/physics` and `engine/bridge`;
- 71 dead `engine/*.cs` files;
- 43 uncompiled `parts/*.cs` scripts and their scenes.

Owner, 9 Oct 2026: "we need to make sure we have all of the puzzle elements prepared for implementation when we remove legacy things." Today, `named-elements.md` and the requirements rows say *what* each element must do. They do not record the concrete declaration knowledge the legacy holds: dimensions, masses, tunings, port layouts, edge cases, test assertions and lesson set-ups. Two catalogue elements also have no scheduled story: CAT-030 funnel, and the CAT-049 and CAT-050 pipe bends.

**Approach:** Write one declaration readiness spec, the CAT-NNN-D document, for each of the 72 catalogue elements. Harvest every legacy fact first, each with a pinned citation. Add a legacy disposition ledger that accounts for every file Epic 7 deletes. Map all 293 named identities. Schedule the missing stories. Stories 7.1–7.4 may not delete a file until the ledger accounts for it.

## Boundaries & Constraints

**Always:**

- **Files.** Use one file per element: `docs/planning/elements/CAT-NNN-<kind>.md`. Add an index at `docs/planning/elements/README.md` and a ledger at `docs/planning/elements/legacy-disposition.md`. Link the index from the `named-elements.md` header and from the LEGACY-0d roadmap row. That row's wording changes from "in named-elements.md" to "linked from named-elements.md".
- **Citations.** Every legacy citation is `path@a6c914e:Lstart-Lend`, which stays retrievable from git history after the purge. Untracked content (the two p025 app bundles) is cited by its archive path, taken from the Story 7.1 archive step. If that archive does not exist yet, record "not harvested — untracked build output, no element source".
- **Spec sections, in this order:**
  1. **Identity.** Give the CAT ID, kind, requirement anchor, the mapped EL/TH/RAD/GAP identities, the roadmap story (epic.story), and the status: delivered, partial or not started.
  2. **Declaration.** Cover each item, or say "none" with a reason:
     - bodies and shapes, with dimensions in metres;
     - mass and material values;
     - constraints and joints;
     - typed sockets and ports;
     - sensors and activation;
     - work and energy stores;
     - parameters, with type, range, default and unit;
     - cosmetic curves and UI bindings;
     - art: scene, mesh and palette tokens;
     - the catalogue and inventory entry.

     Use the current declaration types where they exist (`engine/gpu/*Declaration*.cs`, `BallMaterial`, `RigidMassProperties` and so on) and cite them.
  3. **Engine capabilities.** List the capability families from `general-engine-element-map.md` and `docs/coverage/catalogue-elements.json`, without duplicating them. Split them into "exists now" (with a file reference) and "missing", where each missing capability names the story that builds it. List element dependencies, for example that Conveyor needs Motor.
  4. **Legacy harvest.**
     - Record each behaviour fact, value, edge case and boundary found in the legacy, with its citation. Sources:
       - `parts/<X>Part.cs`;
       - the `engine/physics/*` and `engine/*.cs` files that implement the element;
       - uncompiled `CuriousContraptions.tests/*` assertions, each restated as an acceptance fact;
       - its levels in `content/puzzles.json`;
       - `tools/Campaign` lessons;
       - `reference/` folders.
     - Mark each fact "carry forward" or "do not carry forward". A fact is not carried forward if it is a legacy mechanism that conflicts with current contracts: f16 values, a CPU solver path, a per-element update loop, proof-grade certificates, string-typed closed sets. Give the reason.
  5. **Acceptance outline.** Give the actual Chrome UI construction recipe, the positive behaviour, a meaningful negative or control, the boundaries, Run/Reset restoration, Save/Load and integrations. Point to the requirement row rather than restating it.
  6. **Open questions.** List owner decisions the legacy and the requirements leave unsettled, or write "none".
- **Readiness test.** An implementer can write the element's story spec without opening any file Epic 7 deletes.
- **Delivered elements.** The specs for delivered elements (CAT-001, 004, 014, 022, 023, 035, 054, 063, 066, and 015 where partial) cite the current declarations. They list the remaining variants and modes and any unmet acceptance criteria.
- **Ledger.** The ledger lists every tracked file Epic 7 deletes.
  - **Per file:** `parts/`, `engine/physics/`, `engine/bridge/`, dead `engine/*.cs`, and the uncompiled tests.
  - **Per folder:** `reference/<folder>`, the `tools/<tool>` folders and `diagnostics/`.
  - **Each row** gives the element spec or specs it was harvested into, or "no element knowledge" with a reason, such as a P0 probe, benchmark output or build artefact.
  - **Gaps:** a deleted path that has no row, or a deleted file whose behaviour is not harvested, counts as a gap.
- **Named-identity specs (owner, 9 Oct 2026: "we should have full specs for every puzzle element").** Each of the 276 named puzzle-element identities gets its own full spec with the same six sections:
  - 216 EL;
  - 37 TH;
  - 19 RAD: RAD-01..04, 06..10, 12..18 and 20..22;
  - 4 GAP: GAP-04, 06, 07 and 09.

  **Files:** `docs/planning/elements/EL-NNN-<slug>.md`, `TH-NN-<slug>.md`, `RAD-NN-<slug>.md` and `GAP-NN-<slug>.md`.

  **Section 1** also names the CAT spec the identity refines or extends, where one exists.

  **Section 4** becomes "Sources and legacy". It covers the full requirements row with every variant (`requirements.md#element-NNN` or the TH/RAD/GAP anchor), the `named-elements.md` entry, the element-map row, the coverage JSON and the bounded decisions in `docs/planning/invest/decisions.md`. Legacy is still searched and cited when it exists.

  **Every variant** in the requirements row is specified separately. None is folded into another.

  **Roadmap story:** each spec names its future epic/story. Where none exists, it says "unscheduled". The index then lists these identities so the roadmap can schedule them later. This story does not add epics for them.
- **Index-only entries.** These get a row but no spec:
  - the 10 umbrella "scope index" entries (gap-01, 02, 03, 05, 08, 16, 18 and radiation-05, 11, 19), which point to their EL children;
  - the 7 GAP product features (GAP-10..15 and 17), which are not puzzle elements. Their requirements rows stand.
- **Missing stories.** Add stories for CAT-030 funnel and the CAT-049/050 pipe bends to `epics.md` (Epic 6, after 6.8) and `sprint-status.yaml` (backlog). Follow the format of the neighbouring stories.

**Never:**

- Delete, move or edit any legacy file. Epic 7 owns deletion.
- Change requirement IDs or acceptance criteria, or drop any variant or mode.
- Present a value as sourced when the legacy and the requirements do not support it.
  - **Catalogue elements:** an unsupported value goes under Open questions as "unspecified — owner decision".
  - **Named identities without legacy:** a full spec needs design values. Give each one as "proposed", with a one-line physical or gameplay justification. Keep proposals consistent with the game-grade envelope (`docs/gpu-f32-physics.md`) and with the scale of the existing catalogue, such as the Basketball, the Ramp and the workbench. The owner may revise them.
- Copy whole legacy source into the specs. Harvest facts and cite them.
- Touch code, tests, `tools/e2e` or the Chrome runtime.

</frozen-after-approval>

## Batches (one implementer and one independent reviewer per batch)

- **A: index, ledger and the delivered, Epic 6 and unscheduled elements.** CAT-001, 002, 004, 014, 015, 022, 023, 030, 035, 048, 049, 050, 054, 062, 063, 066. Batch A also adds the missing stories, and in a closing pass compiles the ledger and the identity mapping from every batch's specs.
- **B: Epics 8 and 9.** CAT-005, 013, 017, 020, 024, 025, 026, 027, 033, 037, 052.
- **C: Epics 10 and 11.** CAT-007, 016, 018, 019, 034, 039, 042, 051, 053, 057, 058, 065, 067, 071.
- **D: Epics 12 and 14.** CAT-003, 009, 010, 028, 060, 061, 064, 069, 070.
- **E: Epic 13.** CAT-006, 008, 011, 012, 021, 029, 031, 032, 036, 038, 040, 041, 043, 044, 045, 046, 047, 055, 056, 059, 068, 072.

Named-identity batches, added by the owner on 9 Oct 2026:

- **F: Water 1.** EL-001 to EL-022.
- **G: Water 2.** EL-023 to EL-036 and EL-165 to EL-172.
- **H: Pneumatic, Sound, Electrical and Control.** EL-037 to 052, EL-133 to 137, EL-179, 180, 181, 182, 183, 196, 197, 198 and 211.
- **I: Mechanical 1.** EL-053 to EL-074.
- **J: Mechanical 2.** EL-107 to 111, 156, 157, 158, 162, 163, 164, 184, 185, 186, 193, 194, 195, 199 to 203, 205, 206, 207, 209, 215 and 216.
- **K: Optical.** EL-138 to 155, 173 to 178, 210, 212, 213 and 214.
- **L: Specialist and Character.** EL-075 to 081, EL-082 to 088 and EL-089 to 106.
- **M: Construction, Environment, Goal, Gravity, Material and GAP.** EL-112 to 125, 159, 160, 161, 187 to 192, 204 and 208, plus GAP-04, 06, 07 and 09.
- **N: Thermal.** TH-01 to TH-37.
- **O: Radiation.** EL-126 to 132, plus RAD-01 to 04, 06 to 10, 12 to 18 and 20 to 22.

Batches B to O write only their own element files. Each spec's legacy harvest lists the files it drew from, and Batch A's closing pass builds the ledger from those lists.

## Tasks & Acceptance

**Execution:**
- [x] A: index and ledger skeleton, 16 specs, missing stories
- [x] B: 11 specs
- [x] C: 14 specs
- [x] D: 9 specs
- [x] E: 22 specs
- [x] F–O: 276 named-identity specs (22, 22, 30, 22, 28, 28, 32, 29, 37, 26)
- [x] A closing pass: ledger complete, index covers 72 CAT + 276 named specs + 17 index-only rows, LEGACY-0d row and `named-elements.md` link, the Stories 7.1–7.4 entry gate in `epics.md`, TODO and sprint status

**Acceptance Criteria:**
- **Specs.** Each of the 72 CAT IDs and each of the 276 named puzzle-element identities has exactly one spec with all six sections. Every requirements variant is specified. Every non-sourced value is marked "proposed" with a justification.
- **Citations.** Every citation resolves when checked with `git show a6c914e:<path>` and the cited line range.
- **Ledger.** Every tracked path matched by the Epic 7 deletion scope at a6c914e has a ledger row.
- **Reviewer sampling.** Each batch reviewer samples at least 3 deleted legacy files per batch, picking the riskiest. A sampled file fails the batch if it holds element knowledge the specs do not capture.
- **Stories.** Funnel and pipe-bend stories are in `epics.md` and `sprint-status.yaml`.
- **Checks.** Link checks pass for the new docs. `anvil check --changed` reports 0 warnings.

## Spec Change Log

## Review Triage Log

- **Completion review (9 Oct 2026):** the [fresh independent review](review-7-0-resumed.md) accepted the frozen documentation candidate; the same reviewer verifies the narrow status/owner-decision delta and F13 RTG overload control before local commit. The immutable [snapshot](story-7-0-snapshot.json) remains the original candidate identity; affected delta hashes are retained in the same review record. All 348 specs, 293 named index identities, deletion-path coverage, baseline citations, links and scoped Anvil checks are complete. This is documentation readiness only; no runtime or global qualification is claimed.
  - Owner now requires Bumper recharge in Story 6.2, affordable partial boost on insufficient energy and ordinary bounce at zero. Source/port/rate remain pending.
  - F13 removes the false two-unloaded-motors overload assumption: the control must observe actual summed demand above available power under resisting loads.

- **Resumed independent review (9 Oct 2026): candidate in review.** Original raw batch proof is unavailable; historical summaries below are retained as history, not reused as approval. Current findings and raw checks are in [review-7-0-resumed](review-7-0-resumed.md), with [named-variant supporting review](review-7-0-named-variant-support.md).
  - F1–F12 and VR-1 forward fixes are implemented for independent re-review: orphan sidecar disposition; thermal cold-pack and converter transients; cylinder scheduling; squeeze-pad preload and accumulator gravity; ballistic sprinkler/collector; lens negative control; clock pulse identity rejection; per-body immersion damping; shared Water ports; shared thermoelectric law; fragmentation head and joints.
  - G/N dependency references are renumbered. The ledger has 852 deletion-scope rows after adding the orphan sidecar; current source associations are synchronized.
  - Owner selected baseline-only facts for the four earlier-revision citation exceptions. Supported baseline tuples/tests remain; unavailable field mapping and opacity are explicit owner decisions.
  - Verification so far: 348 unique six-section specs (72 CAT, 216 EL, 37 TH, 19 RAD, 4 GAP), 293 unique named index anchors, no missing integration sections, no non-baseline legacy citations. Anvil changed-doc and explicit scoped-doc scans report zero warnings. Terminal verdict remains pending.

- **Closing pass, written for 13 batches (2026-10-09). Not yet reviewed.**
  - **Ledger:** 886 rows (851 in deletion scope plus 35 kept recipes).
  - **Index:** all 293 headings and 72 CAT IDs appear once each, and all 348 specs are linked.
  - **Renumber:** 211 files and 998 changes, applied in one pass. The 22 G and N files are deferred until those batches pass.
  - **Links:** the named-elements header, the LEGACY-0d wording and the Story 7.1–7.4 entry gate are done.
  - **Routed to owning implementers:**
    - **Harvest gaps:**
      - ImpactFrameTests → CAT-062 and 015
      - FlightCampaignTests → CAT-004, 048, 050 and 062
      - the tilt-sensor files → CAT-023
      - CompressionTransferSupplyTests → CAT-010
    - **Ledger rulings:** tools/Coverage.Tests is kept; balloon.tres and tennis.tres are kept.
    - **Missing §5 Integrations bullet:** 129 specs across batches A, F, H, L, M and O.
    - **Stale epic and order prose:** flagged by the renumber.
- **Roadmap reorder (2026-10-09): Pass, two review passes.**
  - **Pass 1:** Fail. The whole-epic swap put the Both gate before the Latch, the Trampoline came before the Weight, and the doc overclaimed.
  - **Pass 2:** Pass.
  - **New order:**
    - Epic 8: Battery and Powered gate.
    - Epic 9: activation, then the electrical gates.
    - Epic 10: cylinder, pulley/rope, Lever, Weight, Trampoline.
    - Sound meter first, receivers before the shutter, filters and mirrors.
  - **Records:** 31 Depends lines. Open items (a)–(f) recorded. No acceptance text lost.
  - **Carried to the closing pass:** renumber the element-spec story references using the mapping table, in one pass, plus a README note.
- **Batch A (2026-10-09): Pass, two review passes.**
  - **Pass 1 Fail** on these findings:
    - **F1:** palette citations were off by one line.
    - **F2:** a stale counts sentence.
    - **F3:** Story 6.11 dropped the air criterion.
    - **F4:** the dependency on placement nudging was not stated.
    - **F5:** the resize and snap facts in `WorkshopInteractionTests` were not harvested.
    - **N1–N5:** non-blocking notes.
  - **Pass 2: all fixed.** Citations: 368 unique. Anvil: 0 warnings across 20 files. Links are clean. `epics.md` and `README.md` are intact, and README lists all 293 anchors exactly once.
  - **Open for the owner:**
    - which story builds placement nudging;
    - DESIGN.md says "no tween" for the switch and lamp, but the delivered curve is a 0.16 s smoothstep.
- **Batch B (2026-10-09): Pass, two review passes.**
  - **Pass 1 failed on two counts.**
    - **B1:** CAT-017 missed the clock pulse-saturation rules from `ClockPulseFeedbackTests` and MaximumPendingOscillatorEvents = 4096.
    - **B2:** section 2 had unpinned citations.
    - **Non-blocking:** N1–N8, covering the powered_gate load, two unharvested electrical tests, `Half` notes, the once-per-world emission rule and recipe sources.
  - **Pass 2:** every finding fixed. 365 citations resolve, anvil reports 0 warnings and links are clean.
  - **Ledger rows needed:** `ElectricalPlanTests.cs` and `CommittedElectricalTests.cs`. `ClockPulseFeedbackTests.cs` and `reference/cpu/MachineWorld.cs` already have rows and now cover B1.
  - **Optional tidy-up:** CAT-024..027 could add a pointer to CAT-013 open question 3.
  - **Evidence:** `scratchpad/review-7-0-B/`.
- **Batch C (2026-10-09): pass 1 Fail; pass 2 Fail on two leftover contradictions.**
  - **Pass 1 blocking findings:**
    - F1: wound-spring sleeve half-length read as a length.
    - F2: cannon wall half-thickness read as a thickness.
    - F3: missing trampoline, wound-spring and cannon facts.
  - **Pass 1 non-blocking:** F4–F7.
  - **Pass 2 Fail:** CAT-065:45 contradicted row 33, and CAT-042:20/23 contradicted "no owning story".
  - **Status:** both are fixed. The cylinder owner decision is now in all eight specs.
  - **Pass 3: Pass.** No contradictions remain. Anvil reports 0 warnings. 1,286 citations resolve and the links are clean.
  - **Ledger rows needed:**
    - `reference/cpu/` and the p025 `src/` tracked files;
    - SlidingBlade, SceneCompliantSurface, MechanicalNetwork, ConnectionPort, TubeProxy and TubeMouth;
    - CompliantContactLoad/State, PhysicsWorld and SupportFootprint;
    - the `.uid` sidecars;
    - the orphaned `docs/*-recipes.json` and `*-sources.json` files.
- **Batch F (2026-10-09): Pass after three passes.**
  - **Pass 2 failed:** the bob settling time in EL-016 was half the value the physics gives. Damping c stays at 1.0; the amplitude time constant is about 8.4 s and settling takes about 25 s.
  - **Pass 3:** all numbers recomputed and correct. Anvil is clean.
  - **Water family alignment:** density 16, damping 1.0, a shared vessel material, the EL-003 tap body/step, and WaterInlet/WaterOutlet port names.
- **Batch F (2026-10-09): pass 1 Fail.**
  - **Recipe and geometry errors:**
    - F1: EL-009's negative control can't fail.
    - F2: EL-022's pump recipe stalls, and its source has no outlet.
    - F3: EL-013's jet misses the basin.
    - F4: EL-021's blade is wider than the channel.
  - **F5:** water density differs from Batch G. The coordinator aligned both batches on 16 kg/m³.
  - **Low:** L1–L6.
  - **Status:** routed to the Batch F implementer.
- **Batch O (2026-10-09): Pass after three passes.** Story numbers are out of scope; the global renumber in the closing pass handles them.
  - **Pass 2:** F1–F8 were recomputed against the 3×3 area-averaged rate law, and the deflector was corrected to a 0.40 m region with 10° cones.
  - **Pass 3:** the low fixes landed: the RAD-14 angle range, the RAD-12 reading, the RAD-07 neighbour, and neutron readings that now include the tank walls.
  - **Checks:** Anvil reports 0 warnings, and 50 citations and 469 links are clean.
- **Batch O (2026-10-09): pass 1 Fail.** The cross-spec controls don't work with the proposed numbers.
  - **F1:** the deflector field region is too long, so the Dense pole absorbs beta and the alpha control is impossible.
  - **F2:** the single-path rate law can't express collimator coverage or shutter leak.
  - **F3:** the f32 dose accumulation reaches 31.99986, short of the 32 threshold.
  - **F4:** the S605 alpha row conflicts with RAD-03.
  - **Low:** F5, F7 and F8.
  - **Status:** routed to the Batch O implementer.
- **Batch D (2026-10-09): Pass, two review passes.**
  - **Pass 1:** Fail.
    - B1: wavefront ring fields were mis-mapped.
    - B2: about 15 deleted air-jet and rotary files had no disposition.
    - N1–N5: non-blocking findings.
  - **Pass 2:** Pass. All 438 cited ranges resolve, anvil reports 0 warnings and the links check out.
  - **Closed:** a non-blocking wording fix in CAT-060:40 ("sphere of radius 0.065", citing SoundMeterPart L97-L98).
- **Batch E (2026-10-09): pass 2 Pass.**
  - S8 occlusion is in all 20 narrow-ray specs.
  - S6 now carries forward "optics commit before electrical", with the open question restated against gpu-f32-physics, owner S486.
  - Binary16 notes, PickRadius and the 180° yaw note are added. The pre-baseline LightConeVisual citation at 738cb3e (f7a8ef0^) is verified.
  - Citations: 962 occurrences, 271 unique. Anvil reports 0 warnings and links are clean.
  - Ledger rows to add: WorldGeometry, BodyQueryGeometry, WorldQueryContracts, MachineWorld.Transaction, RenderCadenceTests, MachineStepLifecycleTests and PreparedConfigurationTests.
- **Batch E (2026-10-09): pass 1 Fail.** Two blocking findings, both routed to the implementer:
  - occlusion on later segments (emitter exclusion applies only to the first segment; opaque-only traces) was not harvested;
  - S6 contradicted the binding "optics commit before electrical" requirement.

  It also had five non-blocking findings.
- **Batch G (2026-10-09): pass 1 Fail.** Routed to the implementer, with the coordinator's water-family alignment:
  - **B1:** water density is 20, not the family's 16;
  - **B2:** the EL-025 tip volume doesn't fit the cup;
  - **B3:** siphon priming leaves the outlet open;
  - **Minor:** several smaller fixes.

  The coordinator's alignment, all values proposed: density 16 kg/m³, immersion damping 1.0 1/s, taps matching EL-003, and vessel material (0.12, 0.3).
- **Batch H (2026-10-09): pass 2 Pass.**
  - All of F1–F10 were recomputed and hold.
  - Checks: 258 citations resolve, anvil reports 0 warnings, and all 552 links resolve.
  - Advisory for the story specs:
    - EL-050: the Reflecting positive needs a total path of 4.74 m or less.
    - EL-042: the static-hold assumption is open question 3.
- **Batch H (2026-10-09): pass 1 Fail.** Routed to the implementer:
  - **F1:** AdiabaticGasDischargeTests not restated;
  - **F2:** the dish reflection exceeds the direct reading;
  - **F3:** the air-nozzle negative isn't guaranteed;
  - **F4–F10:** minor.
- **Batch M (2026-10-09): Pass after three passes.**
  - **Pass 2 failed on three items:** EL-161's top preset at the tip, the EL-112 tie identities, and GAP-04's coated surface and roll-back.
  - **Pass 3 passed:** every number was recomputed. 499 citations and 569 links check out, and Anvil reports 0 warnings.
- **Batch M (2026-10-09): pass 1 Fail.** Five specs' proposed values break their own controls: connector beam self-weight, shear placement, drop impulse, balloon puncture and fracture work. GAP-06's feed exceeds capacity. Several minor findings. All routed to the implementer.
- **Batch N (2026-10-09): pass 2 Fail.**
  - **What passed:** the single game scale works, and every pass-1 Major is fixed. Fuel ratio, kettle, plate, lens, brake, balloon and match all check out.
  - **Major 1:** the striker chain relies on an elastic point-mass bound. The flint normal load must be declared.
  - **Minors 2–10:** stated derived values are wrong in the storage block, heat target, toaster, TEG, cold pack and hot block, and the range rule needs a fix.
  - **New owner questions:** the saturation curve, and the 4× slower gas time constants.
- **Batch N (2026-10-09): pass 1 Fail.** The thermal scales don't fit together, so the fuel chain can't boil the kettle. Other failures:
  - the match can't ignite;
  - the heating plate is too weak;
  - the hot-air balloon never lifts;
  - TH-05 and TH-06 disagree with EL-055 and EL-173.

  The implementer must propose one game scale and re-derive every chain.
- **Batch I (2026-10-09): Pass after three passes.**
  - **Pass 2 failed:** EL-060's lift distance was uncovered, the cylinder wording was stale and the EL-069 volume was wrong.
  - **Pass 3:** all three were fixed and every number recomputed. Anvil, citations and links are clean.
- **Batch I (2026-10-09): pass 1 Fail.**
  - **High:** F1, the electromagnet default can't lift its own test Weight.
  - **Medium:** F2–F5, F7 and F8. These cover the tray range, an unsourced value, a missing integrations item, compound bodies, water constants and material density scale.
  - **Low:** F6, F9–F13.
- **Batch K (2026-10-09): pass 2 Pass.** Every value was recomputed: EL-173 0.1325 against 0.10; EL-154 4.06, 2.46, 6.29 and 5.34 s. EL-210's supply is now an owner decision, and the RGB-laser scheduling candidate is recorded. Citations: 212 unique. Anvil: 0 warnings. Links are clean.
- **Batch K (2026-10-09): pass 1 Fail.** Three blocking findings, plus minors 4–6, all routed to the implementer:
  - **EL-173:** the lens receivers can't reach their threshold at the given spacing.
  - **EL-155:** the bead sits inside the foot, and the preload has too little margin.
  - **EL-210:** it invents a charge store that contradicts CAT-029.
- **Batch L (2026-10-09): Pass after three passes.**
  - **Pass 3 fixes (all confirmed):**
    - EL-100: the control is now a 6 kg box, with a friction limit of 25 N against 4 N.
    - EL-087: the barrier is the EL-088 tank glass.
    - EL-094: the boundary is now 0.35 kg holds and 0.45 kg fires.
    - EL-101: the latch is one puncture only.
  - **Low notes:** three wording notes on EL-088 and EL-094 went to the implementer and need no re-review.
- **Batch L (2026-10-09): pass 2 Fail.** Three findings are blocking:
  - **R1:** the EL-100 Weight is a rolling sphere, so its control does not hold.
  - **R2:** the EL-087 sight line passes through the opaque pane base.
  - **R3:** EL-094 has no damping, so a 0.6 kg load overshoots and fires.

  R4 is low. Everything else passes, and the EL-086 120 J deviation is accepted. Pass 3 checks only EL-100, EL-087, EL-094 and EL-101:71.
- **Batch L (2026-10-09): pass 1 Fail.** The blocking findings, routed to the implementer:
  - **F1:** the `PhysicsImpactEffects` legacy and its tests were not harvested.
  - **F2–F4:** three negative controls cannot fail (vacuum nozzle, plunger spring, predator barrier).

  Medium and low findings: F5–F10.
- **Batch J (2026-10-09): pass 2 Pass.** F1–F9 are fixed, and every number was recomputed.
  - Springboard lanes: all 15 checked.
  - AirJet and CompliantContactState rows are present.
  - All 28 files have a Files harvested list and an Integrations section.
  - Cam float values of 16.25, 8.61 and 8.61 rad/s fall within the motor range.
  - The cradle geometry fits.
  - Citations: 308 unique. Anvil reports 0 warnings, and the links are clean.
  - N1, a low EL-216 wording point, went to the implementer and needs no re-review.
- **Batch J (2026-10-09): pass 1 Fail.** All findings are routed to the implementer.
  - **F1:** the 15 composite springboard lanes, found in `tools/Campaign`, were not harvested.
  - **F2:** five deleted test files are uncited: the AirJet occlusion, geometry and boundary tests, and CompliantContactStateTests.
  - **F3:** none of the specs has a "Files harvested" list.
  - **F4, F6, F8 (medium):**
    - the cam float thresholds are wrong;
    - the specs have no integrations item;
    - the cradle geometry doesn't fit.
  - **Low:** F5, F7, F9.
