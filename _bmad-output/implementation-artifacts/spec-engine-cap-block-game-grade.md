---
title: 'Complete the contact block with owner-approved game-grade rod acceptance'
type: 'bugfix'
created: '2026-10-27'
status: 'done'
baseline_revision: bb35e8c4dc0a9b8f609dcc7e786223a0216e11af
review_loop_iteration: 0
followup_review_recommended: false
context: [AGENTS.md, docs/testing-guidance.md, docs/delivery-workflow.md]
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** The retained active-set contact-block candidate removes competing recovery paths but its rod test assumes a single-substep impact. Measured soft-contact transitions span four substeps. Its independently diagnosed Weight cache defect is corrected and passes scoped tests; the candidate still needs complete verification and review.

**Approach:** Finish this same candidate under the owner's explicit approval, 27 October 2026: replace the per-impact one-substep energy ratio with non-increasing mechanical energy per impact. Keep the existing no-margin model settling deadline, no-toppling bounds, energy envelopes and 0.5 mm Weight-stack limit. Deliver the complete single-path contact-block slice, not merely a test edit.

## Boundaries & Constraints

**Always:** f32 game arithmetic; one bounded accumulated active-set algorithm for every normal/restitution block; unchanged coherent NormalBranch/slop/push-out policy, central friction/rolling, restitution eligibility/energy limiter. Preserve all existing seeds and physical bounds except the explicitly replaced rod ratio. Normal warm starts require complete unchanged manifold feature membership, reset each substep and cleared at Reset. Numerical correction failures discard that correction and continue. All game tests pass with zero skipped; local proof and normal hooks are authoritative. GitHub builds and deploys only. Retain REQ-06/07/09/14 and deferred browser/performance gates.

**Never:** per-row fallback, second solver, aliases/compatibility, regularization hiding rank, output-fitted margins, velocity snapping, changed ABI/save format, hash/byte-identity tests, mutations, browser gates, local Release worker/web publish. Do not restore the old +0.01 margin or loosen the retained settling/Weight/energy criteria. No arbitrary cutoff excluding small impacts.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Observable behavior | Handling |
|---|---|---|---|
| Flat support | cap/face/narrow-face/line/point, r0.1/0.4, mass0.25/8, rest/2mm drop | finite advancement, support within0.5mm, size-based positional/angular rest envelope | Same active-set rule |
| Rod impacts | r0.1,h0.4,m1, Dead,10.5/12.5 degrees, azimuth0/22.5/45,60/120/240Hz | mechanical energy does not increase across every completed impact, including rebounds below1degree; meaningful impacts observed | Independently derived f32 measurement allowance only, no division by vanishing energy |
| Rod settling/control | same released rods and above-rim15.5degree controls | no over-rim crossing below critical angle; tilt<1degree from unchanged no-margin deadline onward; above-rim topples | Preserve physical deadline/control |
| Unloading/restitution | spinning face, clear control, flat cap/box, box on dynamic box, e0/intermediate/1 | no tensile attraction, real support/rebound, unchanged energy bounds | One enumerator, no row fallback |
| Weight stack | equal4/4 and8/1 package scenes | unchanged support/bounce/tilt and final-second path<=0.5mm | Changed feature set invalidates whole normal warm load |
| Slop/scales | line/single/mixed-band, near-dependence, admitted scales | retained physical speculation/support; finite motion, no persistent support loss | Same algorithm and dimensional tolerances |
| Lifecycle | fresh/reused engines, Reset and supported construction save/load | typed physical replay, correct ticks/state, no stale impulses/membership | Clear caches on Reset |
| Existing regression | orientation, hollow support, friction, F32 identity, original drop/energy matrices | unchanged bounds/seeds/windows and meaningful controls | Broaden by shared impact |

</intent-contract>

## Code Map

- `WorkshopPhysicsEngine.cs`: cache world principal axes on SimBody; remove expanded InvIWorld. `WorkshopConstraintSolver.cs`, `WorkshopFrictionSolver.cs`, and `PrismaticConstraintSolver.cs` evaluate the same inverse inertia by principal-axis application and Gram coupling; delete duplicate ConstraintInverseInertia. No alternative path or ABI change.

- `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs`: retained candidate replaces normal/restitution regularization and sequential recovery with descending-cardinality, lexicographic active subsets, empty last. Preserve its transformed soft equation and finite atomic application.
- `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs`: dead NormalMass removed; ContactManifold holds NormalWarmStartKey. Published support reducer/orientation remain.
- `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs`, `WorkshopFrictionSolver.cs`: retained whole-manifold cache validity and generic ContactFeatureSetKey/ContactColliderPair, no old aliases. Audit Reset/reuse effects.
- `CuriousContraptions.tests/WorkshopPhysicsLawCylinderTests.cs:1324`: replace EveryRodRimImpactObeysTheInelasticOneSubstepRetention, preserving eighteen scene/cadence rows; remove the >=1degree exclusion and ratio division. Existing Energy/LawBody/LawScene are real-production observation helpers. InelasticRockingSettleSeconds keeps its no-margin numerical deadline; correct misleading comments claiming it bounds actual soft impacts. Existing ARodRightsItself control remains.
- `WorkshopContactBlockTests.cs`, `WorkshopPhysicsLawRestitutionTests.cs`, `WorkshopPhysicsLawTests.cs` under the same test project: retained support/unloading/scalar tests. Strengthen coordinate/angular observable gaps if present. Test registration already exists.
- `CuriousContraptions.tests/WeightTests.cs`: unchanged production package stack and lifecycle tests; do not weaken them.
- `docs/gpu-f32-physics.md`, `_bmad-output/planning-artifacts/epics.md#story-engine-cap-block`: update current numeric/source acceptance with the approved decision and exact retained bounds.
- `TODO.md`, sprint-status.yaml and deferred-work.md: current status/next check, no pending-owner blocker remains.
- `spec-engine-cap-block-after-orientation.md` and its evidence directory: preserve historical failed results/old immutable intent as evidence, mark superseded by this current spec. Not additional prerequisites.
- Current candidate spans all changes from baseline SHA above. Preserve the predecessor terminal receipt already present in spec-engine-orientation-levelling.md.

## Tasks & Acceptance

**Execution:**
- Update current acceptance and status documents; retain earlier failure evidence.
- Implement the complete per-impact energy observation, including small/terminal impacts, with an independently derived dimensional measurement tolerance selected before results. Document energy reference, finite sampling, f32 resolution and event detection. Assert actual values so energy injection fails; no vacuous event counting or output-derived threshold.
- Keep/strengthen no-toppling and unchanged deadline checks at the supported cadences; use existing production helpers and meaningful above-rim controls.
- Run the first scoped native check, then affected shared-contact/orientation/hollow/friction/restitution/Weight/energy/lifecycle checks. Investigate actual failures; do not fit bounds.
- Return complete candidate to coordinator for five Auto review layers, repairs, final compiler/full local round, normal hooks and exact-SHA publication. Do not commit/push or run the full round as implementer.

**Acceptance Criteria:**
- Given all matrix inputs, when the real production simulation advances, then every expected behavior has executed passing positive/control/boundary proof.
- Given finite soft impacts, when comparing mechanical energy across each observable impact, then none gains energy beyond documented f32 measurement resolution, including small rebounds, while retained settling/no-toppling criteria pass.
- Given the complete candidate, when independently reviewed and verified locally, then no unresolved required-now regression, fallback, skipped game test or compatibility path remains.
- Given accepted publication, when verifying the exact SHA, then production build/artifact/deployment match; browser/performance qualification remains pending.

## Spec Change Log

- Owner approved the coordinator's game-grade recommendation: “you may continue with your recommendation.” This supersedes the former rod ratio only. Resume the existing authorized dirty candidate; do not discard or republish baseline work.

## Review Triage Log

### 2026-10-27 — Review pass
- verdicts: 15 findings — high 0, medium 1, low 0, false 14, maybe-false 0
- layers: Edge Case Hunter returned []; Verification Gap returned no gaps; Forward-Only returned no violations. Blind Hunter returned ten findings and then supplied read-only clarification; Intent Alignment returned five descriptive divergences. All five layers launched before triage (three then two due to the available slots).
- findings:
  - [false] [reject] Blind1, peak energy can hide instantaneous impact injection — approved design explicitly measures full mechanical energy between turning observations bracketing each observed impact. Each completed interval directly compares its two energies; an increase across that interval fails. Instantaneous monotonicity is a stronger, unrequested property. Reviewer withdrew after reading the approved design.
  - [false] [reject] Blind2, committed samples can miss pairs of crossings — true of discrete sampling, but acceptance is every observed impact at the named cadences, not every unobserved physical contact. Consecutive observed crossings and the terminal interval are closed explicitly without amplitude exclusion. Reviewer withdrew the stronger requirement.
  - [false] [reject] Blind3, projected tilt may misidentify transverse impacts — no missed transition was established in these release scenes. Full three-dimensional tilt independently checks toppling and settling; the declared signed release-axis observation defines the measured rebound sequence. Reviewer confirmed the concern was hypothetical.
  - [false] [reject] Blind4, missing membership-transition controls — the original 4/4 Weight reproducer failed before whole-set invalidation and passes unchanged afterwards; 8/1 and lifecycle controls remain. The complete collider-pair/sorted-feature equality gates all warm loads, and Reset clears both caches. No uncovered reachable failure was identified; reviewer withdrew.
  - [false] [reject] Blind5, missing cutoff-boundary tests — narrow faces and original thin-wall cases exercise near-dependent physical support, while rank-deficient caps exercise subset selection. No admitted boundary causing a missed behavior was demonstrated; pinning an internal pivot decision would contradict behavior-based testing. Reviewer withdrew.
  - [false] [reject] Blind6, discard guarantee lacks forced failure — no reachable admitted input rejecting every candidate was established. Inspection shows private candidates and all finite checks precede body/accumulator application; no partial application occurs on return. Reviewer withdrew.
  - [false] [reject] Blind7, removed per-edge restitution targets — the old fixture manually set approach and impulses and asserted the deleted regularization/fallback and equal sharing. Current flat-drop restitution/spin and reflection tests retain physical rebound coverage; unloading adds actual support, energy and clear controls. Reviewer found no surviving behavioral defect and reclassified the suggestion as optional.
  - [false] [reject] Blind8, relaxed tick can hide biased-pass formula differences — internal pass values are not acceptance. The unchanged NormalBranch is exercised by mixed/all-band physical cases, both spin signs and translation; the assertions distinguish arrested approach from permitted approach and retain geometric/no-attraction bounds. No masked behavioral regression was established; reviewer reclassified as optional.
  - [false] [reject] Blind9, directional inertia response not directly pinned — the removed test injected a tensor rather than exercising frame updates. New orientation/work tests assert the actual energy-debit behavior, unchanged contact-work tests retain target delivery, and original thin-wall cases discriminate the real cancellation defect. No required lost behavior was established; reviewer reclassified the additional oracle as optional.
  - [medium] [patch] Blind10, transient support and cadence gap — move the unchanged lower penetration bound before the settling return and enumerate 60/120/240 Hz; retain the four-second settling and six-second rest window and initial drop allowance. Same implementer owns the patch and affected verification.
  - [false] [reject] Intent1, changed mouth axial upper bound — the literal coordinate inequality changes, but the physical normal penetration limit does not. Exact circular-rim distance now checks the existing 0.51 mm bound every tick at zero friction too; the old axial projection was not equivalent at grazing contact (0.20003788 mm actual normal penetration at the old failure). Progress, no-crossing, transverse and jam bounds remain. This corrects measurement of the existing physical requirement, not its tolerance.
  - [false] [reject] Intent2, observed intervals narrower than instantaneous transitions — the explicit approved Design Notes choose peak-to-peak full energy including kinetic remainder and all observed small/terminal intervals. No instantaneous/substep monotonicity is claimed as proven.
  - [false] [reject] Intent3, inertia also touches friction/prismatic — completing the shared solver slice requires fixing the demonstrated thin-body cancellation without a second path. All callers now use the same factorized law; affected friction, rolling and prismatic proof is included. This necessary dependency closure is not an independent feature.
  - [false] [reject] Intent4, old and new assertions differ — removed solver internals and injected tensors are intentionally replaced with production-interface behavior; complementary unchanged rebound, work, energy and lifecycle tests retain physical criteria. Neither old fallback nor impulse-sharing implementation is a supported invariant.
  - [false] [reject] Intent5, final verification/publication incomplete — accurate current candidate status, not a defect or completion claim. Final compiler/full local round, normal hooks and exact-SHA publication are required before done.

## Design Notes

Pre-result observation choice: evaluate normalized-axis kinetic plus gravitational energy in double from declared f32 reads, referenced to upright COM on the bench. One-ULP position/velocity/angular lane uncertainty is propagated in joules; quaternion lane uncertainty <=u and norm>=1-4u imply axis error <9u; add the existing six-u inertia-compilation bound. Pair allowance is the sum of both observation uncertainties, with no fitted floor. Turning observations retain actual kinetic remainder; exact-zero samples retain last nonzero side, every crossing closes at a subsequent turn (or the last sample before another crossing), and final segment is also checked. No impact is excluded by amplitude. Coverage counts require at least three above-degree impacts and one sub-degree impact. All eighteen release/cadence rows also assert no-toppling and the unchanged no-margin deadline; above-rim controls now cover all three cadences. Support checks use the existing every-tick RestingObservation resolution guard and all three local orientation axes.

For soft normal rows preserve lambdaNew=(1-c)*lambdaOld+m*K^-1*(-vn-bias), using z=lambdaNew/m and b=K*((1-c)/m*lambdaOld)-vn-bias. Active-set residual Kz-b is in m/s. Equilibrated pivot cutoff64*2^-24 is dimensionless; residual allowance64*2^-24*max(ContactSlop/h,maxAbs(b),maxRowSumAbs(Kij*zj)). These are numerical solve tolerances, not scene acceptance. Restitution uses m1,c0 and its own target.

The old impact ratio excluded transverse contact moments across multiple substeps. Replacing it does not require changing the contact law. Peak-to-peak mechanical energy includes kinetic remainder and gravity potential; do not treat changing angular momentum about a pivot as energy creation. A measurement allowance must account for declared f32 position/quaternion/velocity resolution in joules, independently of measured failing output. Preserve the no-margin model strictly as the existing settling deadline.

Existing clean bounded result:51/9/0, all40support+bothWeight pass, nine old ratio rows fail. Final matching build62292 passes0warnings/errors; original TRX /private/tmp/tim-cap-after-orientation-weight/clean-membership-60.trx. Changed tests require a new matching incremental build. Published baseline full local4453/0/0 lives at /private/tmp/tim-orientation-retry-gate.NdBkqB/results; its two FreshSeedDrops outputs have bounds Dead2.191mJ,Rolling2.114mJ,Lively1.991mJ, every class0/200 failures. Reuse baseline evidence, not a duplicate sweep.

## Verification

Incremental Debug build of CuriousContraptions.tests/CuriousContraptions.tests.csproj, -noAutoResponse, then:
`dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore -noAutoResponse --filter 'FullyQualifiedName~WorkshopContactBlockTests|FullyQualifiedName~EveryRodRimImpact|FullyQualifiedName~ARodRightsItself|FullyQualifiedName~AStackEndDownHoldsWithoutBounceOrCreep' --logger 'console;verbosity=normal' -- RunConfiguration.TreatNoTestsAsError=true`

Expect all rows passing, zero skipped. Then affected contact/cylinder/hollow/restitution/friction/Weight/energy/F32/lifecycle tests with the same build; retain original commands/results. Coordinator inspects all diff and matrix coverage, runs five Auto layers, then captures required compiler logs and full solution once after fixes. Normal hooks remain mandatory. No GitHub tests or browser gates.

### Thin-support inertia correction (pre-counterfactual design)

The unchanged hollow checks exposed two real skinny-box horizontal-speed failures. At ordinal257, an independently reconstructed principal-frame effective-mass matrix differs from the expanded-world implementation by up to0.00074305; its observed antisymmetry reaches0.00080872 although coefficients are order1–4. The declared box moments are (0.0000166667,0.333341667,0.333341667)kg·m². Expanded world inverse-inertia entries of order60000 cancel when applying transverse torques.

Preserve the same inertia law uniformly: cache the three world principal axes; evaluate inverse-inertia application as Σ axis_k invI_k dot(axis_k,t), and rotational effective-mass/coupling as Σ invI_k dot(axis_k,a)dot(axis_k,b). Use that factorization for contact, friction, rolling and prismatic callers; delete InvIWorld and the duplicate prismatic multiplication. No cutoff, contact geometry, acceptance bound or alternate solver changes. First counterfactual is the original thin-wall rows plus declared anisotropic paid-contact orientation/control scenes, then retained69 and affected shared-inertia tests.

The mouth failure is a separate oracle-coordinate error: at the failing axial coordinate, exact normal rim penetration is0.00020003788m, within the unchanged0.00051m bound. Apply that same geometric distance bound every tick for zero-friction oversize rows, retain progress/no-crossing/transverse/jam-speed and all fitting controls. Source H2/H7 in the hollow contract requires physical penetration and jam, not an axial projection of normal slop.

Replace the private Pair/RestitutionPass energy fixture with four production-interface ABounceRetainsEnergyTransferredByANonBouncingContactWithoutExceedingIt rows. These explicitly cover relaxed energy greater than pre-solve energy, within-budget bounce and capped excess, each with a no-upper-body control. Existing analytic flat-drop, high-restitution dynamic-box and reviewer-stack cases remain complementary coverage, not substitutes for that unique branch. Replace the direct paid-contact tensor fixture with real admitted anisotropic boxes at three orientations and powered/passive controls, checking recorded debit against observed kinetic energy. Other unchanged private tests are outside this correction.

## Completed scoped verification — 27 October 2026

**Result:** current scoped coverage is 1,425 distinct passing cases after the independently verified support repair; full local verification passes4,606/0/0. Independent review is complete; hooks and publication remain pending. The original pre-review scoped total was1,345/0/0. Runtime diagnostics were removed before the clean matching build; the final supplemental change is test-only.

| Criterion / affected dependency | Actual proof |
|---|---|
| Cap/face/narrow-face/line/point, scales, drop/rest, all-axis resting envelope | 120 WorkshopContactBlockTests at60/120/240Hz, including the lower support bound during settling |
| All completed rod impacts, small/terminal impacts, unchanged no-margin settling/no-toppling | 18 EveryRodRimImpact rows; 9 ARodRightsItself controls at all cadences |
| Whole-manifold warm validity and unchanged 0.5 mm path | Both original AStackEndDownHoldsWithoutBounceOrCreep rows; Weight lifecycle in remaining selection |
| Mixed band, unloading, singleton/scalar behavior | 6 production MixedDepthSupportArrestsApproach rows; remaining law/restitution checks |
| Hollow thin supports and mouth normal-space penetration | 32 original ThinWallSupports/BareSegmentMouth rows, unchanged physical bounds |
| Shared principal-frame inertia, paid/passive orientation controls | 6 AReorientingAnisotropicBox rows; friction, rolling, prismatic/soft constraints and original energy/drop matrices in remaining selection |
| Energy limiter max(pre-solve, relaxed), positive within budget and capped excess | 4 ABounceRetainsEnergyTransferredByANonBouncingContactWithoutExceedingIt rows |
| Reset, fresh/reused engine, supported construction/save/load, typed F32, ordinary contacts/work | Remaining engine, Weight, hollow element, F32, lesson, contact activation/load/work and prismatic selection |

Environment: SDK 10.0.402 / runtime 10.0.13 via the explicit PATH/DOTNET_ROOT below; Node 26.12.1. This restores the published baseline environment. The earlier 69-pass run on SDK 10.0.403/runtime 10.0.14 is diagnostic only; supported first69 also passed (92817).

Clean incremental build42175: zero warnings/errors, 5.65s. First113 run9808: 113/0/0, about8s. Remaining run74995: 1228/0/0, 9.4826min. Supplemental test-only build13968: zero warnings/errors, 8.63s; run17111: 4/0/0, 2.3464s. All builds use:
```sh
PATH=/opt/homebrew/Cellar/dotnet/10.0.402_1/libexec:$PATH DOTNET_ROOT=/opt/homebrew/Cellar/dotnet/10.0.402_1/libexec dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 -noAutoResponse --no-restore -v:q
```

Exact first and remaining commands (remaining excludes only the already executed first113; this is scoped local proof, not the required full round):
```sh
PATH=/opt/homebrew/Cellar/dotnet/10.0.402_1/libexec:$PATH DOTNET_ROOT=/opt/homebrew/Cellar/dotnet/10.0.402_1/libexec dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore -noAutoResponse --filter 'FullyQualifiedName~WorkshopContactBlockTests|FullyQualifiedName~EveryRodRimImpact|FullyQualifiedName~ARodRightsItself|FullyQualifiedName~AStackEndDownHoldsWithoutBounceOrCreep|FullyQualifiedName~MixedDepthSupportArrestsApproach|FullyQualifiedName~ThinWallSupportsAdvancingTiltedPartnersBetweenTheirVertices|FullyQualifiedName~BareSegmentMouthSupportsOversizePayloadsAndPassesFittingPayloads|FullyQualifiedName~AReorientingAnisotropicBoxReceivesNoMoreKineticEnergy' --logger 'trx;LogFileName=game-grade-clean-first.trx' --logger 'console;verbosity=minimal' --results-directory /private/tmp/tim-cap-block-game-grade -- RunConfiguration.TreatNoTestsAsError=true
PATH=/opt/homebrew/Cellar/dotnet/10.0.402_1/libexec:$PATH DOTNET_ROOT=/opt/homebrew/Cellar/dotnet/10.0.402_1/libexec dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore -noAutoResponse --filter '(FullyQualifiedName~WorkshopPhysicsLaw|FullyQualifiedName~WorkshopPhysicsEngineTests|FullyQualifiedName~WorkshopContactBlockTests|FullyQualifiedName~WeightTests|FullyQualifiedName~HollowElementTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~F32SaveBoundaryTests|FullyQualifiedName~F32LaneRoundTripTests|FullyQualifiedName~LaunchLessonSolveTests|FullyQualifiedName~ContactActivationTests|FullyQualifiedName~DeclaredContactLoadTests|FullyQualifiedName~ContactWorkInstanceTests|FullyQualifiedName~ContactWorkLedgerPropertyTests|FullyQualifiedName~DeclaredPrismaticDriveTests|FullyQualifiedName~PrismaticConstraintTests|FullyQualifiedName~SoftConstraintTests)&FullyQualifiedName!~WorkshopContactBlockTests&FullyQualifiedName!~EveryRodRimImpact&FullyQualifiedName!~ARodRightsItself&FullyQualifiedName!~AStackEndDownHoldsWithoutBounceOrCreep&FullyQualifiedName!~MixedDepthSupportArrestsApproach&FullyQualifiedName!~ThinWallSupportsAdvancingTiltedPartnersBetweenTheirVertices&FullyQualifiedName!~BareSegmentMouthSupportsOversizePayloadsAndPassesFittingPayloads&FullyQualifiedName!~AReorientingAnisotropicBoxReceivesNoMoreKineticEnergy' --logger 'trx;LogFileName=game-grade-clean-affected.trx' --logger 'console;verbosity=normal' --results-directory /private/tmp/tim-cap-block-game-grade -- RunConfiguration.TreatNoTestsAsError=true
PATH=/opt/homebrew/Cellar/dotnet/10.0.402_1/libexec:$PATH DOTNET_ROOT=/opt/homebrew/Cellar/dotnet/10.0.402_1/libexec dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug -f net10.0 --no-build --no-restore -noAutoResponse --filter 'FullyQualifiedName~ABounceRetainsEnergyTransferredByANonBouncingContactWithoutExceedingIt' --logger 'trx;LogFileName=game-grade-relaxed-energy.trx' --logger 'console;verbosity=normal' --results-directory /private/tmp/tim-cap-block-game-grade -- RunConfiguration.TreatNoTestsAsError=true
```

Original raw evidence: /private/tmp/tim-cap-block-game-grade/game-grade-clean-first.trx, game-grade-clean-affected.trx and game-grade-relaxed-energy.trx. No duplicate log or manifest. Both original FreshSeedDrops gates retain seeds/windows/formula: all18 classes have 0/200 failures. Current control-derived budgets are Dead1.994mJ, Rolling2.114mJ, Lively1.967mJ; historical baseline2.191/2.114/1.991mJ. These are results of the unchanged formula, not acceptance changes.

### Causal corrections and retained failed evidence

The earlier affected run20116 was 1253/4/0 (game-grade-affected.trx): two zero-friction mouth assertions incorrectly projected normal slop axially, and two real thin-box lateral speeds exceeded unchanged0.001m/s (Tube0.0012450386, Frustum0.0014109083). Mouth normal penetration0.00020003788m satisfies the unchanged0.00051m physical bound; the corrected oracle independently measures rim distance every tick and retains progress, no-crossing, transverse and jam controls.

Bounded onset/growth traces in game-grade-hollow-impulses.trx, game-grade-hollow-onset.trx and game-grade-hollow-growth.trx rejected the hypothesized box-distance cancellation cause and implicated expanded inverse-inertia cancellation. The factorized same-law correction yields both box speeds0.0000160804438m/s and quarter-second XZ paths1.6067/1.9651µm; cylinder control0.000300437m/s also passes. No pivot cutoff or physical limit changed. All diagnostic instrumentation was removed before42175.

Preserve game-grade-band.trx (invalid unchanged-speed expectation corrected to finite-time geometric remaining-slop/continuing-approach), game-grade-hollow-observation.trx, game-grade-factorized-first.trx and game-grade-anisotropic-control.trx (new fixture direction/restitution setup corrected before acceptance). The final anisotropic elastic control passed6/0/0 before clean113. Prior Weight and four-substep rod failures remain in the earlier after-orientation spec and original evidence; none is silently overwritten.

The supplemental energy scene is derived before output: after four biased/four relaxation sweeps, a100kg nonbouncing upper sphere transfers approach to a1kg lower sphere. With q=100/101 and bounded recovery0.012m/s, relaxed approach lies between ((0.01+0.024)q^4-0.024)q^4 and0.01m/s. Restitution0.25 preserves a0.005m/s target; restitution1 caps0.02m/s at the relaxed approach. The first published PhysicsMotionRead segment (StartOrdinal0, VelocityOffsetY), emitted after restitution/settle and before the next substep, exposes actual outgoing velocity without private arrays. No-upper controls remain stationary. Tolerance128u*(0.0002*480+0.02+0.01) derives from f32 speed scales; positive rebound must exceed0.004m/s. All four cases passed the first chosen bounds.


### Review repair verification

Blind10 was repaired by the same implementation owner and independently closed by Blind Hunter. All 120 support cases (five shapes × two radii × two masses × two drops × three cadences) pass, zero failed/skipped, in /private/tmp/tim-cap-block-game-grade/game-grade-review-support.trx. The unchanged 0.5 mm lower bound now runs during every settling tick; existing resting upper/displacement/all-axis angular bounds and ten-second window remain. Root independently read the diff and original TRX. Current scoped coverage is 1,425 cases: previous 1,345 minus the superseded 40 support rows plus 120.

Review result: one medium patch applied and independently verified, fourteen rejected findings with individual reasons above, no deferrals and no unresolved required-now finding. Follow-up whole review recommendation is false (zero high and one medium patched entry); the affected repair already has independent verification. All five compiler logs and the final full local round completed successfully; hooks and publication remain pending.

## Auto Run Result

Step04 code complete: independently reviewed implementation and full local verification Pass. One medium patch applied and independently verified; fourteen findings rejected with recorded reasons; zero deferred findings and no unresolved review risk. followup_review_recommended remains false. Status done records code completion, not publication.

Changed file groups: shared normal/restitution active-set solve and whole-manifold warm validity; factorized contact/friction/rolling/prismatic inertia; production-interface support, rod, hollow, energy and anisotropic controls; current contracts/status and retained historical evidence. The support repair preserves every bound and adds settling-tick checks across all three cadences.

Final local round exited0 with4,606 passed, zero failed and zero skipped: game4,114; Coverage77; E2eRunner nonbrowser/Node192; WorkerAotGuard17; Bundle206. Original five TRXs are in /private/tmp/tim-cap-game-grade-gate.5Bp0P4/results; all five compiler logs in the same parent directory completed successfully. Current scoped proof is1,425/0/0; original run totals above remain unchanged evidence.

Normal hooks, exact-SHA SnapshotApproval, publication and terminal CI/artifact/deployment checks remain pending. Sprint engine-cap-block stays in-progress until publication. Browser and detailed performance qualification remain at their named later gates. This receipt is not publication or browser/performance qualification. No commits, staging or publication performed by implementer.
