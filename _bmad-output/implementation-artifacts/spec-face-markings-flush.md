---
title: 'face-markings-flush: print existing control markings on their faces'
type: 'bugfix'
created: '2026-10-29'
status: 'in-review'
baseline_revision: '057df5ae99cc8e620ee88cc161bd1497a5e1b980'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/DESIGN.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Static face markings on five supplied gates, the Hold Timer and State Latch stand visibly proud of their faces without colliders. They are labels, but their raised geometry reads as solid structure.

**Approach:** Replace those raised markings with flush printing on the existing faces. Preserve each original symbol, layout, palette and control meaning, along with working lamps, timer bar, latch rocker and lifecycle behavior.

## Boundaries & Constraints

**Always:** Cover BothGate, ElectricalNand, ElectricalNor, ElectricalOr, ElectricalXor, HoldTimer and StateLatch. Retain CAT-013/024/025/026/027/033/037 and todo-261/267/271/361/362/364; EL-133–137 remain separate later named outcomes. Preserve one/two input marks, Both's converging paths, each operation's distinct pictogram, and gold truth dots in row order00,01,10,11: NAND00/01/10; NOR00; OR01/10/11; XOR01/10; Both has no truth dots. Latch retains filled Set, hollow Reset and downward output chevron. Timer retains its static navy track and animated gold remaining bar. Print remains attached under part rotation and visible through its declared material. Keep typed ports, all solid colliders and committed presentation behavior.

**Never:** No element-specific rendering/solver path, new physics admission domain, compatibility, old raised representation alongside the new one, hidden warmup or fixed population limit. Do not widen sphere admission to accommodate printing. No output hashes, byte-identity saves, historical motion/tick pins, mutants, browser/E2E runs, local Release worker build or web publish. Native artwork checks do not claim rasterized browser readability or full named-element/release qualification.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior | Error handling |
|---|---|---|---|
| Printed artwork | Each listed marking on its actual instantiated package | Flat surface printing; no raised solid volume or buried label | Native assertion identifies missing/protruding/buried marking |
| Symbol meaning | Every gate variant and latch marker | Correct paths, truth rows, input numbering and distinct Set/Reset/output cues | Missing/wrong symbol fails directly |
| Rotation and solids | Representative rotated parts; nearby lamp/socket | Print follows its face; genuine solid features retain their collider and protrusion | Ordinary authored geometry |
| Live presentation | Supplied/unsupplied gate rows, timer active/expired, latch Set/Reset | Correct lamps, anchored draining bar, rocker and indicator | Existing typed command behavior |
| Lifecycle | Run/Reset and supported save/load | Authored state and presentation restored; loaded behavior remains correct | Existing typed refusals |

</intent-contract>

## Code Map

- `elements/BothGate/both_gate.tscn`: Visual/UpperPath, LowerPath, FirstMark, SecondMarkLeft/Right.
- `elements/ElectricalNand/electrical_nand.tscn`, `elements/ElectricalNor/electrical_nor.tscn`, `elements/ElectricalOr/electrical_or.tscn`, `elements/ElectricalXor/electrical_xor.tscn`: every Visual/Pictogram stroke/bubble, Visual/TruthDot* and the three input marks. Preserve XY arrangement and original shapes; replace obsolete raised mesh resources.
- `elements/HoldTimer/hold_timer.tscn`: Visual/Track only. Visual/Remaining/Bar remains animated and anchored; preserve its binding and expired visibility.
- `elements/StateLatch/latch.tscn`: Visual/SetMarker, all12 ResetMarker beads and both OutMarker strokes. Preserve hollow-ring coverage; node/primitive counts may change if the same visual symbol is represented directly. Rocker and IndicatorLamp remain live presentation.
- Host face planes from authored centre+half-depth: gates/latch z0.37; timer z0.3725. Existing mark widths start at25mm. Use native scene/material assets; no renderer subsystem change is needed.
- `CuriousContraptions.tests/VisibleSolidColliderTests.cs`: existing ArtSpec.Uncollided entries list this debt. Decorative membership alone proves no flatness. Add direct geometry checks through actual PartRegistry-created artwork, including transformed vertices/extent rather than only node centres. Existing solid lamp/socket cases supply positive controls.
- `CuriousContraptions.tests/BothGateTests.cs`, `ElectricalNandTests.cs`, `ElectricalNorTests.cs`, `ElectricalOrTests.cs`, `ElectricalXorTests.cs`: retained production truth/supply/lamp behavior. Current presentation assertions pin SphereMesh truth dots at z0.42 and pictogram strokes at z0.4; replace raised-representation assertions with flatness and symbol meaning. Each file has one redundant save re-encode byte comparison beside decoded saved-state equality; remove it and retain decoded equality plus actual loaded behavior.
- `CuriousContraptions.tests/StateLatchTests.cs`: PartRegistry_DiscoversLatchAndPresentationRestoresRestOnReset counts12 reset children. If representation changes, replace this structural count with direct hollow-ring coverage. Preserve Set/Reset/supply/lamp/lifecycle tests.
- `CuriousContraptions.tests/HoldTimerTests.cs`: HoldTimerPresentationTests.ActualCommittedWindowDrivesAnchoredBarAndIndependentLampAndHiddenCatchUp is required existing integration proof.
- `tools/BundleElements/SceneGeometryExtractor.cs` extracts explicit collision proxies. `engine/gpu/PhysicsDeclarations.cs` already admits sphere radius1/32: the claimed0.035 truth-dot minimum blocker is stale; latch0.025 beads need no colliders once printed. These are read-only context, not new engine tasks.
- Current docs: `docs/planning/elements/CAT-013-both_gate.md`, `CAT-024-electrical_nand.md`, `CAT-025-electrical_nor.md`, `CAT-026-electrical_or.md`, `CAT-027-electrical_xor.md`, `CAT-033-hold_timer.md`, `CAT-037-latch.md`; DESIGN.md supplied-logic and motion-feedback descriptions. Replace current raised/debt instructions; historical evidence remains historical.
- Coordinator owns TODO.md, sprint-status.yaml, epics.md and the prior Bumper terminal receipt. Current order is published Bumper → this slice →9A.44B; no harness/platform prerequisite is added.

## Tasks & Acceptance

**Execution:**
- Listed seven scenes and any necessary package-owned art assets — implement one flat representation, remove superseded raised resources, retain original marks and live bindings.
- Listed native tests — prove every matrix row through PartRegistry/presentation and WorkshopSimulation interfaces; preserve existing positive/control/boundary/lifecycle coverage. Record each replaced assertion and its retained behavior.
- Listed CAT/Design docs — describe printing and remove resolved non-colliding debt and obsolete sphere-minimum prerequisite.
- Coordinator current records/spec — record scoped proof, independent Auto review, local full run and exact-SHA publication.

**Acceptance Criteria:**
1. Given each of the seven packages, when native PartRegistry instantiates it, then every named static marking is present as flat printing on its face, without the old raised geometry. No Uncollided exemption remains for these marks.
2. Given actual artwork and representative rotation, when its transformed surface geometry/material is inspected, then printing remains face-attached and recognizable. A thin anti-z-fighting separation may be declared before implementation; keep it within0.5mm of the front face (2% of the smallest25mm stroke), with no solid relief. This distinguishes printing from the old15–85mm protrusions and does not impose physics precision. Verify marking presence and shape, not only bounded depth; missing, buried and wrong symbols must fail.
3. Given each gate truth variant and the latch, when the actual marking content is observed, then the exact meaningful truth rows, input numbering, distinct operation glyphs, hollow Reset/filled Set and downward output cue are preserved. Geometry or texture assertions may follow the chosen native representation; no image/mesh hashes.
4. Given active and control simulation scenarios, when committed presentation advances, then real gate input/output lamps, timer anchored countdown/expiry and latch rocker/indicator continue to follow state. Run/Reset and supported reloaded behavior pass through their production interfaces; the scene changes do not add colliders for printing or alter existing solid geometry.
5. Given the frozen candidate, when local affected checks, final solution run and normal hooks execute, then zero failures/skips and independent Auto review permit publication; exact-SHA production artifact/deployment checks pass before the next slice. Actual browser rendering/legibility remains explicitly pending post-catalogue.

## Spec Change Log

## Review Triage Log

### 29 October 2026 — Review pass1

- verdicts:0 findings — high0, medium0, low0, false0, maybe-false0.
- All five Auto layers completed against review-round1.diff: /root/face_blind, /root/face_edge, /root/face_verification, /root/face_forward and /root/face_intent. Three concurrent worker slots required two launch groups; no layer was skipped. No patches or deferrals.
- Intent auditor described the deliberate Both-path correction and native-versus-rasterized proof boundary, not an unintended defect. The explicit converging-path requirement governs the existing divergent slopes; centers, lengths and palette remain. Selected ink/empty regions protect symbol meaning without contour goldens. Browser appearance remains expressly pending.
- Forward-only review confirmed replacement of raised resources, deletion of Uncollided, no compatibility or alternative representation; mesh/group traversal applies one coverage algorithm to current artwork.

## Verification

Implementer: normal incremental Debug build of affected closure with -noAutoResponse; run the affected native classes with --no-build --no-restore -c Debug -f net10.0 and TreatNoTestsAsError=true. Reuse binaries until relevant inputs change. Keep scenario windows small and meaningful, preserve failed attempts and do not fit bounds to outputs.

Coordinator: matching compiler captures and one final full local solution round per docs/delivery-workflow.md#local-test-authority. All five Auto review layers, same-owner fixes and affected independent re-review, normal hooks, exact committed-SHA approval, then production artifact/deployment verification. No browser or performance qualification claim.

## Implementation evidence — 29 October 2026

The seven packages now use planar QuadMesh strokes and package-owned OBJ discs/annuli, separated from the face by0.25mm within the declared0.5mm visual envelope. The former raised resources and Uncollided exemptions are removed. Both's paths now converge; the latch's original downward chevron transforms remain. Native transformed geometry, not a transposed reading of serialized matrices, establishes their direction. Live lamps, timer remaining bar, latch rocker and solid colliders retain their existing behavior.

Four gate presentation tests no longer pin the incidental printing Z offset. FaceMarkingTests instead checks face attachment, front-facing surfaces, meaningful filled/empty symbol regions and actual solid protrusion controls. Five redundant encoded-save byte comparisons were removed; decoded construction equality and loaded production behavior remain. Existing HasSameBits calls compare decoded actuator/load/logic records through SequenceEqual, not encoded bytes or output hashes.

Implementer verification: Debug build session55519, zero warnings/errors; affected native session80826,260 passed/0 failed/0 skipped. After removing redundant depth pins, incremental Debug session61047 had zero warnings/errors and affected presentation session83451 passed4/0/0. Failed investigations remain recorded in the session evidence: session5076 had247/13/0 with invalid zero-height CylinderMesh normals; session90436 had12/2/0 while correcting direction assumptions. The discarded CylinderMesh approach is absent from the final candidate.

Coordinator final local round1 completed in session77133 (exit0, completion c3f203):4,845 passed/0 failed/0 skipped; main4,354 plus tool projects206/191/77/17. Main TRX spans01:27:53–01:47:59+08 on29 October2026; runner duration20m01s. Matching compiler captures are at /private/var/folders/bg/f_3jxb251pnfck7qpwb9xmxc0000gn/T/tim-face-final-gate.eR0mAq; original per-project TestResults/face-full-round1.trx files retain results. All five TRX counters were independently parsed: total=executed=passed, failed=notExecuted=0. Added relative documentation links19/19 resolve; seven mesh imports have distinct UIDs and existing sources. git diff --check passes. Browser rasterization/readability remains pending post-catalogue.

### Matrix execution audit

| Matrix row | Executed production/native proof in final round1 |
|---|---|
| Printed artwork | EveryPrintedSurfaceIsPresentFrontFacingAndAttachedToTheRotatingFace:7 cases passed |
| Symbol meaning | PrintedSymbolsRetainTheirFilledAndEmptyRegions:7 cases passed |
| Rotation and solids | First artwork theory's rotated cases; EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt:29 cases passed |
| Live presentation | ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset; ActualCommittedWindowDrivesAnchoredBarAndIndependentLampAndHiddenCatchUp; PartRegistry_DiscoversLatchAndPresentationRestoresRestOnReset; CommittedLatchSetResetAndSupplyRestorationDriveBothGate all passed |
| Lifecycle | Gate save/load/Reset scenarios, ClosingBothSwitchesRetractsTheOutputAndSaveLoadAndResetAreExact and DueDelayClosesContactBeforeSameTickConsumerIntegrationAndSaveResetIsExact passed; all cases in BothGate31, NAND26, NOR27, OR28, XOR29, StateLatch20 and HoldTimer22 passed |

The audit matched actual TRX result names and required Passed outcomes, rather than inferring execution from test registration. All five independent Auto layers subsequently completed with no findings; publication remains pending.

## Auto Run Result

Implemented flat printed markings for seven existing packages, preserving meaningful symbols, solid geometry and committed live presentation. Both's paths now converge as required. Removed five redundant encoded-save byte assertions and the obsolete Uncollided exemption.

| Files | Change |
|---|---|
| elements/BothGate/both_gate.tscn | Planar numbered marks and converging paths |
| elements/ElectricalNand/electrical_nand.tscn; elements/ElectricalNor/electrical_nor.tscn | Planar strokes, hollow inversion bubbles and truth discs |
| elements/ElectricalOr/electrical_or.tscn; elements/ElectricalXor/electrical_xor.tscn | Planar distinct operation glyphs and truth discs |
| elements/HoldTimer/hold_timer.tscn | Printed static track; live bar retained |
| elements/StateLatch/latch.tscn | Printed Set, hollow Reset and downward output cues |
| elements/ElectricalNand/printed-disc.obj, printed-ring.obj; elements/ElectricalNor/printed-disc.obj, printed-ring.obj; elements/ElectricalOr/printed-disc.obj; elements/ElectricalXor/printed-disc.obj; elements/StateLatch/printed-disc.obj; each corresponding .obj.import | Package-owned planar assets and native imports |
| CuriousContraptions.tests/FaceMarkingTests.cs; CuriousContraptions.tests.csproj | Registered native surface and symbol proof |
| CuriousContraptions.tests/BothGateTests.cs; ElectricalNandTests.cs; ElectricalNorTests.cs; ElectricalOrTests.cs; ElectricalXorTests.cs | Semantic save assertions retained; printed representation assertions updated |
| CuriousContraptions.tests/VisibleSolidColliderTests.cs | Removed resolved uncollided-art debt |
| DESIGN.md; docs/planning/elements/CAT-013-both_gate.md; CAT-024-electrical_nand.md; CAT-025-electrical_nor.md; CAT-026-electrical_or.md; CAT-027-electrical_xor.md; CAT-033-hold_timer.md; CAT-037-latch.md | Current printing contract and deferred browser qualification |
| TODO.md; _bmad-output/implementation-artifacts/sprint-status.yaml; _bmad-output/planning-artifacts/epics.md; spec-bumper-cap-solid.md; this spec | Current slice/order, prior terminal receipt and this slice's original evidence |

Review: five layers, zero findings, zero patches, zero deferrals, no rejected findings. Patched entry counts high0/medium0/low0; followup_review_recommended:false. Local verification:4,845/0/0, with matrix membership above. Source/asset/link checks pass. No browser, mutation testing, local Release worker build or web publish was run.

Publication is pending normal hooks, exact committed-SHA SnapshotApproval, remote production build and artifact/deployment checks. Status remains in-review until the required terminal receipt is known. Residual risk: camera-dependent rasterized readability and full browser/release qualification remain at the named later gates; native proof does not replace them.
