---
title: 'T.1 Fast-Test Quick Wins'
type: 'refactor'
created: '2026-10-21'
status: 'in-progress'
baseline_commit: '81c3be3bfcd85e51ac8f213721adcc930f33430e'
route: 'dispatch'
story_key: 't-1-fast-test-quick-wins'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="owner-authorized autonomous backlog intent">

## Intent

Catch stale Chrome test code before browser execution, narrow unjustified selection escalation, and qualify policy-serial suites for parallel execution without losing proof. Exact authority: Story T.1 AC1–AC4 in ../planning-artifacts/epics.md and its verification-tiers link.

## Boundaries & Constraints

Preserve every per-part actual Chrome UI/configuration/connection/intended/control/RunReset/saveLoad assertion. Engine paths retain Full escalation. Unknown impact remains conservative. True timing suites, clock-master, cat-017, cat-023a/b, cat-037, cat-014 and anim-1a/b/c remain serial alone. No in-app acceleration, new cache/generator/schema platform, native replay implementation, repeat-run feature or unrelated backlog work.

Delete only policy-serial entries superseded by measured qualification and NoPrimarySuite refusals where declared suites provide coverage. Keep independent implementation/review identities, Anvil before writes and required publication. No human intent gaps; publication remains gated by independent approval.

## I/O & Edge-Case Matrix

| Scenario | Expected behavior |
|---|---|
| Deleted accessor in suite | Type check fails before Chrome |
| Known package/solution membership edit | Union base/head affected packages, dependents, declared integrations and smoke |
| Unknown/shared build edit or malformed input | Full, with reason |
| Catalogue declaration row changes | Affected suites; helper/unknown logic changes retain Full |
| No Primary but declared coverage | Select declared coverage and connections |
| No actual coverage | Explicit refusal or conservative Full; never silent omission |
| Qualified suite/import changes | Qualification invalidated; serial until measured again |
| True timing suite | Serial irrespective of policy qualification |

</frozen-after-approval>

## Current shipment (owner course correction, 21 Oct)

Ship AC1–AC2 only, then 9A.16. The original approved intent above is retained; this explicit owner correction defers AC3–AC4 without marking T.1 complete. Calibration and T.2 replay are optional acceleration work, not gameplay prerequisites. Known bounded changes use selected functional checks and controls under the reviewer’s impact matrix; Full is reserved for unresolved cross-family/unknown coverage. Named release gates and per-part actual UI/lifecycle proof remain.

Deferred calibration source, including the former candidate Program and two tests, is preserved verbatim under [evidence-t-1/deferred-calibration](evidence-t-1/deferred-calibration/). Production Program is restored to baseline; no new scheduling path or qualification is shipped.

## Code Map

- tools/E2eRunner/{ImpactRules,Selection,ChangeSet}.cs: full-rule precedence, existing base reader and rename paths; fix null historical-head reads falling through to current files.
- tools/E2eRunner/{SuiteCatalogue,Program,Calibration,MeasuredWorkers}.cs: typed declarations, publish-once calibration and worker criteria; existing calibration trials only already-Parallel suites.
- tools/E2eRunner/{TimingDetector,SourceIdentity,Toolchain}.cs: reuse transitive local import closure, sorted content hashes and external-tool discovery.
- tools/E2eRunner.Tests/{SelectionTests,CalibrationTests,TimingDeclarationTests,IdentityTests}.cs: extend current controls/mutants.
- tools/e2e/{tsconfig.json,workshop-driver.ts,cat-052.test.ts}, tools/E2eRunner.Tests/TypeCheckTests.cs: recovered unfinished strict checking; missing TypeCheck/ResolveTypeScript implementation.

## Tasks & Acceptance

**Execution:**
- [ ] Preserve recovered edits; integrate coordinator's frozen post-9A.3 base before implementation.
- [ ] Complete type-check tool boundary and tests; document Tier 1 command. Resolve installed TypeScript deterministically; missing tools fail rather than skip. Retain suite assertions.
- [ ] Implement base/head-aware XML package/solution membership selection. Union removed/added dependencies; reject unsupported shared build changes. Narrow catalogue rows using syntax comparison, not arbitrary file exemptions.
- [ ] Resolve element and connected-part declared coverage without requiring a Primary when real coverage exists. Keep missing coverage explicit.
- [ ] Extend calibration to explicit typed policy candidates after timing inspection; measure them with existing Parallel workload. Persist compact typed qualification/evidence entries with suite import-closure digests. Keep failed or invalidated qualifications serial.
- [ ] Extend affected unit/mutation checks for the matrix, and freeze before/after ten-merge replay inputs.
- [ ] Complete measured baseline/after, independent review, publication and scoped status evidence.

**Acceptance Criteria:**
- AC1: Given current suites, when Tier 1 runs tsc --noEmit, then all type-check; restoring CAT-020's deleted threshold() accessor produces the specific compiler error.
- AC2: Given each named path class, when selection runs, then package .csproj and solution package changes select their affected packages/suites, catalogue rows select suites, and declared coverage replaces NoPrimarySuite. Table-driven checks and mutants pin every changed rule; engine paths retain Full.
- AC3: Given policy-only serial candidates, when calibration completes, then each is qualified Parallel against its exact closure identity or has an evidenced serial reason. All true timing suites remain alone.
- AC4: Given one frozen Full-before and the final after/calibration run, when recording results, then report their wall/publish time and the same ten-merge escalation sample before/after. Preserve all failures and inventory/workload differences; no promised saving replaces measurement.

## Design Notes

Recover in place on branch worktree-agent-a268845b34c2e23da at c4f322bb45ce4b6e7f5fd4fe3f6e1e0f81df0c28; do not discard dirty files. Coordinator owns baseline scheduling and subsequent base integration. No builds, Chrome or source implementation until released.

One clean committed-main Full after 9A.3 serves checkpoint and AC4-before. The latest ten story-merge commits at this post-9A.3 boundary are fixed below for both rule versions; exclude documentation-only acceptance commits. This includes 9A.3 and drops the oldest prior sample entry. The original evidence-t-1/escalation-replay-before.txt remains historical evidence, not the AC4 acceptance baseline; replay this corrected sample with both rule versions. Coordinator confirms the exact Full baseline commit separately. Historical replay must distinguish absent head files from current fallback; generated bundle/closure approximations must be explicit, not presented as exact historical runtime evidence.

1. 28e2bb9c77dd16026d460a4c23f1f915dd59792c
2. 6ca5591e2e75df9370f455d358eb35c95b69e984
3. 379cc24a7fe17e666b234f8c0e7ed0b0c88d4292
4. 379f84ba13951c3258ee2c5c54d379f31213e9b8
5. c9b45da057a41796ed31f2969f468a6c88561af1
6. dea664745c0f62bf28e07ffd171ac1174ea05e9b
7. 6f14de9a39fb673c952841e0a37fdbdd2db56d27
8. f3105a5726b1dde90daedf8155852fd45171a46f
9. 73be4b452acf1539e684ec0e63e5577f629f6aa4
10. 2fa05e15ff18ec3184a1dc2d4fe006395a459f03

## Verification

**Required now for the bounded AC1–AC2 shipment:** reuse the unchanged passing focused/type-check and mutation evidence after independent applicability review; run only the extraction compilation to detect dangling dependencies (passed, 0 warnings/errors). Normal commit protections remain. No new whole Tier 1, Chrome, Full or calibration run is required by this shipment. The original complete-story sequence below is deferred with AC3–AC4 and is not a prerequisite for 9A.16.

**Deferred complete-story sequence:** run affected runner tests first, then documented Tier 1: solution build zero warnings, all solution tests, Node tests, Lifecycle/Wire/Ownership and changed-file Anvil checks. Compile/type-check positive and deleted-accessor negative; mutation checks include removed references, unknown build changes, catalogue helper edits, connected partner without Primary and transitive import changes.

Coordinator reserves Chrome and quiet serial windows. Reuse original baseline evidence; calibration can supply after only when its final serial plus chosen-worker trial covers the final inventory. Reviewer reuses matching Full evidence and independently verifies affected probes/mutants. No duplicate Full for review.

## Implementation Notes

- Recovered edits integrated on frozen `81c3be3bfcd85e51ac8f213721adcc930f33430e`. Inherited binary contents were not scanned by Anvil; their checked-out bytes matched the approved git objects. Text writes were gated.
- Before extraction, restored runner tests passed 193/193 in 23s, including strict TypeScript positive and Counter-specific same-line `threshold()` negative (TS2339). The generic generated accessor remains valid for other parts; this is a Counter typed-view restriction, not a globally discriminated wire model.
- Thirteen targeted mutations were assertion-killed; [mutation logs](evidence-t-1/mutations/) retain failures and diffs. M10/M12 first hit analyzer errors; compiling M10b/M12b then failed assertions. Calibration mutation evidence remains historical deferred work.
- Baseline independently verified 44 Clean suites, 246/246 on the exact base: [original recovery summary](../../.anvil/checkpoint-81c3be3-t1-before-recovery-1/summary.json). Runner 2593.150237s, publish 38.0s separately. Startup includes foreign-Chrome wait; do not subtract it as pure overhead. Earlier interrupted run remains incomplete. No after measurement or speedup claim.
- AC1–AC2 code and tests are unchanged during extraction. Only the smallest compilation check for removed calibration dependencies is authorized; no repeat tests/Chrome/Full/calibration. Independent bounded review and publication approval remain pending.

## Spec Change Log

- 21 Oct, latest owner correction: finish only the existing AC1–AC2 shipment, preserve AC3–AC4 code/evidence as deferred, and move to 9A.16. T.2/calibration do not block gameplay. Replace blanket engine-file Full with independently justified actual-impact coverage. Original AC IDs and named release gates remain. No new verification programme.

- 21 Oct, owner steering: broad timing/checkpoint cadence is every five newly verified named puzzle elements, not infrastructure/documentation stories or automatic epic-end repeats. Targeted timing for affected clocks/workers/transport/pose/timing-test changes, ordinary per-part proof, shared/unknown-impact escalation and named release/calibration gates remain. Update guide, epics crossreferences and TODO; no suite filter or test split. Original AC1–AC4 remain intact.

## Review Triage Log

First-candidate input: .anvil/t1-static-candidate-81c3be3.diff, SHA256 24685e1bfb2fdc6bebb18539f8432d94d1b7ddd961143acbe36ab997403da34a. Original findings are collaboration messages from /root/t1_blind_lens, /root/t1_edge_lens and /root/t1_verification_lens, inspected and triaged by /root. No raw-file links are invented. All 13 rows remain distinct. The restored candidate passed 193/193 native tests and 13 mutation scenarios were detected before extraction; the baseline is now independently verified. Rows concerning calibration are preserved deferred work, not shipped acceptance. The unchanged AC1–AC2 evidence is offered for independent reuse; bounded shipment approval remains pending.

| Finding | Lens | Triage | Forward fix / evidence status |
|---|---|---|---|
| B1 | blind | false as current runtime defect | Current qualifications remain empty and trusted checked-in records require independent measurement review before population. Nonempty evidence is not runtime attestation; no evidence-validation platform added. |
| B2 | blind | medium | Qualification binds a value snapshot of MeasuredWorkers; resolver checks matching calibration, maximum workers and actual Chrome. Machine CPU/memory remain checked by existing resolver. Boundary tests passed before deferral; calibration source is inactive. |
| B3 | blind | high | Capture immutable closure/source/bundle identities at each trial; report chosen trial identities and reject cross-trial drift. Native drift tests and mutant passed before deferral. |
| B4 | blind | medium | Locate exactly one SuiteCatalogue class and direct All field; ambiguity returns unknown instead of throwing. Test source added. |
| B5 | blind | medium | Changed directives or disabled text conservatively return unknown before syntax equivalence. Test source added. |
| B6 | blind | low | Non-asserting one-off replay moved to evidence-t-1/EscalationReplayProbe.cs.txt; not a credited regression test. Corrected sample still requires explicit before/after measurement. |
| B7 | blind | low | History-dependent replay removed from routine test discovery; execute only for AC4. |
| B8 | blind | medium | Added reverse/transitive chain, removed-edge, cycle and unrelated-part controls. Tests and removed-edge/transitive mutants passed. |
| B9 | blind | medium | Actual Program.Files boundary is tested for qualified versus serial scheduling; removing Resolve must be killed. Scheduler mutation detected before deferral; source now inactive. |
| E1 | edge | high | Same captured chosen-trial identity fix as B3, retained as a separate finding; cross-trial drift rejects qualification. |
| E2 | edge | medium | Same structural All-field lookup as B4; unrelated All variable and duplicate-field controls added. |
| V1 | verification | medium | Added actual Selector project/solution/catalogue suite identity and smoke assertions plus Full controls; omitted add/SelectElement mutations detected. |
| V2 | verification | medium | Added base/head reverse/transitive/cycle controls independently requested; tests and mutations detected. |

