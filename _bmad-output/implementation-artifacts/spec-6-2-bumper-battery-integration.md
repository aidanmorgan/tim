---
title: 'Story 6.2: integrate finite Battery recharge and paid Bumper contacts'
type: feature
status: in-review
route: dispatch
baseline_commit: e24fea3644cb572858669bf8db7617482760eab2
context:
  - AGENTS.md
  - docs/planning/elements/CAT-015-bumper.md
  - docs/planning/elements/CAT-005-battery.md
---

## Intent

Deliver the existing Bumper's paid radial contact and working finite recharge in the current purged Workshop. This resumes the independently reviewed sibling preparations; it does not admit Motor, Pipe or general electrical networks. Dedicated implementer bumper_implementation and independent reviewer reviewer retain ownership. Required publication follows standing AGENTS/REQ-11/REQ-14: independent SnapshotApproval, individual commit and normal non-force push, then verified production deployment and applicable production-origin controls before completion.

## Resolved model

Owner approved recharge now and affordable partial boosts. Default Bumper strength8 m/s uses a shared32 J preload (reference mass1 kg, capacity=half mass times strength squared); zero energy leaves ordinary collision response and no paid ring. Normal work is evaluated after ordinary response using current contact arms/inertia, preserving tangential response and angular impulse. Cooldown remains per target72 physical steps.

Owner approved Battery3600 J,120 W, initially full and enabled; energy/power model, no voltage semantics. Typed Supply→PowerIn wiring admits one supplier per consumer and source fan-out. The generic f32 allocator runs120 Hz before every fourth480 Hz contact substep. Source debit funds bounded proportional store credits, including the final positive depletion grant. Ordinary representational residuals clamp/continue; zero realized debit gives zero credits. Battery has no recharge input. Runtime enable applies at the next electrical phase; Reset restores authored source/store state.

The existing bounded64-slot reliable observation queue retains paid events, source debit/store credit and enable changes. Duplicate same-tick observations require exact unchanged bits; new ticks retain full accounting. Unchanged geometry/pose/cosmetic precision follows the declared Epic16 migration boundary; no global f32 qualification is claimed.

## Prerequisites and existing proof

Story7.4 terminal local commit is the baseline above. Frozen preloaded Bumper snapshot in ../../../tim-bumper/_bmad-output/implementation-artifacts/snapshot-6-2-preloaded-contact.md has SHA2566701eea20594ecc62474e1781eaf24a137af2814a4f199f1d0c84f601784ff06. Frozen combined Battery snapshot in ../../../tim-battery/_bmad-output/implementation-artifacts/snapshot-8-1a-battery-finite-recharge.json has SHA25663fd6eaa0c36e28b0de9d453809375e36edec02271dccc1341c14cd20b7666b2. Their original specs/reviews remain immutable historical preparation evidence.

The combined candidate has86 source/deletion entries:35 inherited Bumper entries,22 modified by Battery,51 additional paths. Independent5/5 Chrome, native arithmetic/admission/retention and separate Production/Playtest outputs establish isolated scoped Pass. Reviewer must establish relevance before reusing unchanged proof; integration and new sidekick behavior require current evidence.

## Code and impact map

- Apply combined canonical source to engine/gpu contact/electrical declarations, compiler, wire/save/read formats, worker export and simulation phase; preserve the original prerequisite layer distinction.
- Semantically merge CuriousContraptions.csproj and CuriousContraptions.tests/CuriousContraptions.tests.csproj, preserving root explicit current inputs and adding only necessary source/tests.
- Merge WorkshopActivationAnimationTests.cs without reverting five assertion-warning fixes; merge WorkshopWireTests.cs without removing clock-fault regression cases. Preserve export_presets.cfg archive exclusions while adding Battery to all current presets.
- Admit new canonical parts/BatteryPart.cs and parts/catalog/battery.tres. Explicitly add their tiny declarative parts/scenes/battery.tscn and BatteryPart.cs.uid dependency removed by purge; never restore imperative SceneSource/BinaryInput code.
- Retire engine/BumperWorkResource.cs and its UID; retain generic ContactWorkResource authoring and atomic validation. No compatibility or migration path.
- Reconcile PartRegistry, MachinePart, MachineWorld, UI controls, generic animation Transition and typed source indicators. Versioned wire/save rejects old shapes.
- Add a thin BumperSidekick declaration in engine/gpu using the existing captured-goal/receiver engine: locked Basketball at(-3,5,0), locked Receiver at(2,0.9,0), inventory one Bumper. Preserve authored assistance data and manual placement. Append WorkshopPuzzleId and picker item, validation/precision/save/hint callers; no new solver or imported solution. UI proof selects the named mode, places/moves the Bumper just left of the falling ball and samples its captured goal/paid event. Miss control moves it clear using the gizmo; unpaid control uses existing Strength configuration0, then verifies Passive/no paid ring and unsolved route. Restore strength8 for paid route and exact Reset/SaveLoad checks.
- Shared runtime impact includes source-free Bumper, activation-edge filtering, clock/read transport, generic animation, catalogue/export and ordinary lifecycle. Select affected existing checks accordingly; broaden only for changed dependencies or findings.

## Tasks & Acceptance

- [x] Reviewer agrees current model, bounded scope and retained-proof applicability.
- [x] Integrate source/resources with all root fixes preserved; no unrelated dirty files altered.
- [x] Given actual UI Bumper/Battery construction and Supply→PowerIn connection, when Run advances, recharge permits paid work beyond preload; disabled/depleted/disconnected controls add no energy. Partial budget gives only affordable boost.
- [x] Given two consumer stores, when both demand work, source fan-out remains finite and both receive usable credit; final positive source exhaustion credit is not discarded.
- [x] Given bumper_sidekick selected through UI and Bumper placed using normal controls, when Run executes, a paid radial route solves; meaningful missed and unpaid controls do not solve by powered kick. Observe motion and paid/cosmetic events.
- [x] Given Save/Load and Run/Reset, authored pose/strength/source settings/links restore exactly; runtime stores, occurrences/cooldowns/rings reset. Disable/re-enable preserves stored consumer work and resumes finite transfer.
- [x] Current affected native/wire/Node and actual Chrome controls pass; Production and Playtest builds succeed. Retain failed attempts, exact source/artifact identities and ordinary resource/queue lifecycle evidence.
- [x] Independent integrated-candidate pre-publication review has zero unresolved unintended regressions; final status/publication-policy delta still requires SnapshotApproval.
- [ ] After exact SnapshotApproval, commit and normal non-force push the approved candidate and explicitly approved ancestor range; verify remote/deployed identities and required-now production-origin behavior before terminal Pass.

## Explicit remaining scope

Story6.3 owns advanced multi-angle/depth and wall_and_bumper puzzles. Full Battery Motor/switchedMotor/Latch/generalnetwork acceptance remains separate. Physical placement assistance scheduling is retained as an unresolved full-catalogue obligation, not silently claimed by this bounded route. Broad performance/device/fault qualification stays at named later gates. Do not claim full CAT005/EL196 or global Bumper completion from this slice.

## Progress and review entry

Integration contract prepared from existing exact proof and current root source acceptance. Root dirty documentation/review receipts, .scratch and diagnostic outputs predate this work and are preserved. Reviewer entry Pass: exact Bumper assistance retains correction .25/.1/0 m and zero rotation correction; source-free strength0 at identical pose is an admitted unpaid control. Canonical source/resource integration is complete. Full current native704/704 passed (64cfb7), Node87/87 (147225), Coverage68/68 (af4a22), Anvil affected scan zero warnings. First native run702/704 exposed stale ten-kind catalogue/last-row expectations; corrected to eleven kinds with Bowling retained at row9 and Battery appended (a7288c,921214). Sidekick source/profile/atomic inventory/save test passed (1ddc58). Actual Chrome paid and same-pose strength0 controls passed (6992/f82bc4/f64c43); moved-clear miss passed94497/00120c. Paid route(-3.3,1,0), strength8, debit23.8731346 J, remaining8.1268654 J solved; unpaid zero work retained ordinary bounce and no ring/goal; miss retained32 J and no contact/ring/goal. All authored Save/Load/Reset bytes compared except existing authority revision envelope. Earlier(-3.2,1.5) route undershot; accidental depth-handle movement and two overstrict assumed-snap miss checks remain failed attempts. Actual miss gizmo position was(-5.29333496,1,0), correctly clear. Reviewer-requested pre-Reset solved receipt and console-error capture are in final test. Playtest8245/4d616f passed; retained artifact artifacts/6-2-integration-playtest/AppBundle served8071. Port8070 startup failed occupied (f2ea4a), then8071 started45268. Final Production22336/6c043b passed; retained artifact artifacts/6-2-integration-production/AppBundle. Independent original full Chrome53/53 passed; final corrections passed the17 affected checks with40 unchanged cases reused. No owner model choice remains pending for this bounded integration.

## Review Triage Log

All review layers completed before grouping these findings. Forward corrections retain the current model and public surface.

| ID | Severity | Decision | Evidence and closure |
|---|---|---|---|
| B1 | medium | patch | Paused toggle rebuild uses unchanged committed state; show queued state until its electrical phase. |
| B2 | medium | patch | Add disconnected-source actual-UI control. |
| B3 | medium | patch | Existing toggle proof only exercises Paused; add Running control with continued ticks. |
| B4 | medium | patch | Existing settings proof changes capacity only; cover nondefault output/fraction and authored Disabled. |
| B5 | medium | patch | Supply telemetry alone does not prove rendered charge indicators; add threshold/material observations. |
| B6 | medium | patch | Compare per-commit source debit with aggregate recipient credit within declared f32 residual tolerance. |
| B7 | low | patch, grouped B6 | Exact duplicate observations inflate reported credit sums; deduplicate epoch/tick without weakening positive assertions. |
| B8 | medium | patch | Fan-out has Save/Reset proof but no Load; verify exact round trip. |
| B9 | low | reject, maybe false | Fixed waits have no reproduced bad outcome; broad generic wait rewrite is unnecessary. New controls may use bounded state waits. |
| B10 | low | patch | Current range wording is stale; distinguish implemented bounds from owner-approved defaults. |
| G1 | medium | patch, grouped B3 | Verified Running-toggle coverage gap. |
| G2 | medium | patch, grouped B5 | Verified rendered-charge coverage gap. |
| E1 | medium | patch | Electrical animation ordinal misses cadence revision reset; add boundary and Chrome downshift regression. |

Original review layer results remain in the coordinator's retained review evidence. These fixes reopen only affected criteria; the prior full 53-case Chrome result remains evidence for unchanged behavior.

### Forward-fix verification, 10 October

Cadence revision now resets electrical sample ordinals while retaining target/generation ownership. Queued Battery intent is separate from committed charge, survives deselection, reverses on repeated paused interaction, reconciles at the later electrical phase, and clears at the existing Reset/Load/puzzle retirement boundary. A validated Applied ACK remains accepted when a newer same-world read has already superseded its presentation; retired-world ACKs cannot restore intent.

Affected native animation tests passed27/27 (63151/cc8c2d). Final focused Playtest publish passed73879/510bdb; no subsequent runtime changes. Actual Chrome focused controls passed: fan-out Load/accounting and paused-repeat/Reset/Running stored-work preservation (95665/87157a); connected Running stop/resume and same-world cadence downshift (68924/edd069); depleted/all-off and Reset materials, disconnected demanding store, authored120J/30W fractions0/.25/.5/.75/1 and Disabled, individual rendered charge marks, terminal changes, connected30W transfer bound, and exact restoration (96105/856ceb/9e1bc2). Pure accounting duplicate/mutation/unfunded-credit controls also passed in96105. Per-commit source debit is compared with aggregate recipient credit using affected f32 ULP bounds; exact epoch/tick duplicates are counted once. Evidence images remain under .anvil/battery-*.png.

Running timeline: disabled tick187 retains3552J source/16J store with zero debit/credit; enabled tick219 transfers1J into the store while ticks continue. Cadence has no current product UI selector: its real-Chrome test explicitly calls the existing ConfigureCadence protocol in Building, retaining the same world and observing the lower new animation ordinal. Battery construction/configuration/control proofs use actual UI only.

Failed attempts retained: babd4d (dropdown selection recipe), f49643/dd314c (stale ACK-only Running oracle and malformed protocol revision/sequence), c51388/ae6bc5 (attempted cadence in unsupported Paused phase and no new demand after the prolonged unpaid ball came to rest). The corrected Running transfer control starts a fresh actual-UI Reset/Run and requires observed off-state demand before re-enabling. No physics or goal oracle was changed. Final named protocol constants and evidence-path edits preserve the tested values. Prior immutable snapshot and53-case proof remain historical candidate evidence; corrected Production26126/3480d2 passed, and reviewer32888/2bec3b independently passed17/17 with zero console errors and zero identity drift (a67a6b). Publication remains required before completion.

## Publication and exact commit membership

The earlier preparation-only “no push” wording was a generated workflow restriction, not an owner restriction. Standing AGENTS and delivery-workflow REQ-11/REQ-14 take precedence. This correction changes publication policy/status only; behavior, source and artifact proof remain bound to the immutable integration/correction snapshots.

Commit membership is exactly the103 source paths in snapshot-6-2-bumper-battery-corrections.json, including the two owned deletions, plus snapshot-6-2-bumper-battery-integration.json, snapshot-6-2-bumper-battery-corrections.json and review-6-2-bumper-battery-integration.md in this directory (106 paths). The current spec, TODO and sprint review status are explicit post-snapshot nonruntime deltas; reviewer must bind their final bytes in SnapshotApproval. Exclude pre-existing review-7-0-resumed.md, review-7-1-reference-historical-archive-purge.md, review-7-2-diagnostics-legacy-probe-tools-purge.md, review-7-3-uncompiled-legacy-test-purge.md, review-7-4-legacy-physics-unshipped-engine-purge.md and story-7-0-owner-questions.md; also exclude .scratch/, anvil-diag-* and time-output.txt. Ignored local .anvil evidence, retained artifacts and browser logs are referenced proof, not source payload.

origin/main6416b4c9ffbc9686b64526da638a9f2744bb2a19 is an ancestor of baseline HEAD. A normal push would also publish15 already distinct local commits from a165e88 through e24fea3; their exact range and original approval applicability require explicit reviewer/coordinator reconciliation before push. Do not rewrite, squash, force-push or silently omit this range.

Publication-dependent checks: verify exact approved commit at origin/main; successful .github/workflows/pages.yml Production build and Pages deployment for that commit; bind the actually loaded production assets at https://aidanmorgan.github.io/tim/ to that deployment; perform actual Chrome production-origin Sidekick paid/miss/unpaid and Battery construction/wiring, finite recharge/partial/depleted/disconnected controls, enable/charge rendering and ordinary Save/Load/Run/Reset as applicable to the Production observation surface. Playtest-only numeric telemetry remains local supplemental evidence; do not enable it in Production or claim local receipts prove deployed gameplay. Reviewer enumerates the final bounded production checks in SnapshotApproval. Until those pass, story remains in review and dependent delivery remains blocked.
