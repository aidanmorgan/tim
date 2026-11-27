---
status: final
stepsCompleted:
  - validate-prerequisites
  - design-epics
  - create-stories
  - final-validation
inputDocuments:
  - 'docs/gpu-f32-physics.md'
  - 'docs/planning/invest/vertical-delivery.md'
  - 'docs/planning/invest/named-elements.md'
  - 'docs/planning/invest/campaign.md'
  - 'docs/planning/requirements.md'
  - 'docs/delivery-workflow.md'
  - 'TODO.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/prd.md'
---

# Curious Contraptions — Epic Breakdown

## Overview

This document provides the complete, authoritative BMAD epic and story breakdown for Curious Contraptions, decomposing the engineering requirements, INVEST vertical roadmap, element catalogue (CAT-001 through CAT-072 across all 7 families), and the campaign challenge levels (at least 150; 244 planned) into implementable vertical slices.

Every story is an INVEST vertical slice: delivering one player-observable behavior through real UI, adding generic data-driven capability, naming the legacy it deletes, and requiring independent adversarial review with pure TypeScript Playwright verification in Chrome.

---

## Requirements Inventory

### Functional Requirements

- **FR-01 (Machine Graph Authoring):** The player can assemble contraptions on the workbench using catalogue puzzle elements, configure parameters, and connect sockets via typed signal wires.
- **FR-02 (Run/Reset Loop):** The player can press Run to compile the machine into a physical simulation, observe dynamic behavior, and press Reset to restore the exact authoring state.
- **FR-03 (WASM SIMD Physics Solver):** Continuous physics advances at 120 Hz (480 Hz substeps) on a dedicated Web Worker via 128-bit WebAssembly SIMD without external physics engine dependencies.
- **FR-04 (TGS Soft Constraint Mechanics):** Contacts, resting stability, and restitution follow the Box2D v3 Temporal Gauss-Seidel Soft Step formulation with compliance $\gamma$ and softness $\beta$.
- **FR-05 (Speculative Contacts Anti-Tunneling):** Fast-moving bodies allocate speculative contacts in narrowphase with target velocity absorption to prevent discrete tunneling through barriers of arbitrary thickness.
- **FR-06 (Decoupled Animation Pipeline):** Cosmetic animations (e.g. halo pulse, switch depression, spring squash) evaluate at 60 Hz on a dedicated animation worker fed one-way by physics events.
- **FR-07 (Catalogue Puzzle Elements):** 72 distinct catalogue elements implemented purely as declarative capability data without bespoke solver branches.
- **FR-08 (Campaign Levels):** at least 150 progressively taught challenge levels (244 planned: the 150 numbered slots and 94 lettered levels, owner decisions 24c to 24f) in 15 chapters, as [requirements](../../docs/planning/requirements.md#campaign-structure-15-chapters-244-levels) define them, covering every adopted element family.
- **FR-09 (Deterministic Save/Load):** Constructions can be saved to and loaded from JSON preserving exact IEEE-754 `f32` coordinates and connection graphs.

### Non-Functional Requirements

- **NFR-01 (Zero External Physics Dependencies):** 100% in-engine custom C# and TypeScript algorithms incorporating Quad-BVH, TGS Soft, and speculative contacts. No Box2D, Jolt, Rapier, or PhysX libraries.
- **NFR-02 (Universal Browser Graphics):** Presentation executes universally on both WebGL 2.0 and WebGPU via instanced draw batches from a lock-free `SharedArrayBuffer` pose ring, multi-buffered with its depth derived from tick rate and render delay (owner decision 24k; target, delivered by `ENGINE-WIRE-ABI`).
- **NFR-03 (Forward-Only Refactoring):** Zero backwards-compatibility shims, legacy flags, or CPU fallback paths. Superseded code is deleted in the slice that replaces it.
- **NFR-04 (Game-Grade Numerical Envelope):** Clamp-or-continue numerical policy. Residuals clamp; non-finite states preserve prior poses; ticks never fault.
- **NFR-05 (Strict Type Safety):** Closed sets are enums end-to-end; extensible identities use strongly typed IDs; string conversions exist only at external boundaries.
- **NFR-06 (Automated Verification Rigor):** Every slice must pass the C# static gate (`dotnet build CuriousContraptions.slnx` with analyzers and warnings as errors, 0 warnings; anvil does not analyse C#), `anvil check --changed` with 0 warnings for non-C# files, `dotnet test CuriousContraptions.slnx` 100% (557+ tests), and the Playwright E2E suites its verification tiers require in actual Chrome, run by `tools/E2eRunner` (the Tier 2 impact selection, plus the Tier 3 full run by `tools/E2eRunner` when required; [Verification tiers](autonomous-development-guide.md#verification-tiers)).

---

## Global Acceptance Criteria & Story Completion Definition of Done (DoD)

Every single story across all epics must formally satisfy the following mandatory Story Completion Criteria before it can be accepted or marked complete:

1. **C# Unit Tests Pass 100%:** `dotnet test CuriousContraptions.slnx` passes 100% with 0 failures (557+ tests).
2. **C# Static Gate Clean:** `dotnet build CuriousContraptions.slnx` reports **0 warnings and 0 errors** with the .NET analyzers and warnings as errors (`Directory.Build.props`, Story 9R.0), in the default configuration and with `-p:PlaytestDiagnostics=true`, and the web host's own sources compile clean. `CSharpStaticGateTests` passes (no suppression, no widened exemption, no swallowed exception). The repository pre-commit hook checks all of this on a from-scratch snapshot of the staged index and refuses a commit that fails; CI repeats the builds. Until the Story 9.4b rebase, the one expected warning is IL2125. Anvil does not analyse C#: `anvil check --changed` with 0 warnings applies to non-C# files only and is never evidence for C# code.
3. **Browser-Based Playwright E2E Suite Passes 100%:** The story's dedicated browser-based Playwright E2E test suite (`tools/e2e/<suite>.test.ts`) executed in actual Chrome via the Playwright browser connector passes 100%.
4. **Required E2E Tiers Pass 100% in Chrome:** Impact-selected functional suites and controls pass in actual Chrome. Full is required only when demonstrated cross-family or unknown impact cannot be resolved by bounded checks, justified in the independent review matrix; an engine filename or absent T.2 coverage alone does not require it. Timing-sensitive suites run alone, others use measured parallel workers ([Verification tiers](autonomous-development-guide.md#verification-tiers)). Every required check must pass; a known regression blocks completion.
5. **Exact Reset and Save/Load Persistence Roundtrip:** Exact state restoration before Run and after Reset, as well as JSON Save/Load persistence roundtrip, is verified in Chrome.
6. **Zero Legacy Remnants or Compatibility Bridges:** Active tree grep confirms complete absence of backwards-compatibility shims, legacy flags, or deprecated code remnants.
7. **Terminal Scoped Pass from Independent Adversarial Reviewer (Murdoch):** Independent adversarial review subagent verifies all criteria and issues an unambiguous terminal scoped `Pass`. Self-review never qualifies.

---

## Current execution plan

**Future slice workflow:** follow [AGENTS delivery speed rules](../../AGENTS.md#delivery-speed-rules-owner-24-october-2026-these-take-precedence-over-conflicting-wording-below): one bmad-build-auto loop with Blind Hunter, Edge Case Hunter, Verification Gap, Intent Alignment and the Forward-Only Auditor (AGENTS.md "Refactor forward", review gate, owner 25 Oct 2026: any fallback, backwards compatibility, retained deprecated slot or second rule for the same behaviour is a Fail), no duplicate review system; fresh implementer per slice, fixes to that implementer; incremental builds, full native suite once per round, CI-owned Release/publish; no mutation testing until the post-catalogue E2E phase (owner, 25 Oct 2026); review Pass on committed SHA and short exact-SHA terminal receipts. New rules apply from the next slice. These rules refine the proof statements below without weakening native behavior or complete-catalogue coverage.

**Owner direction, 24 October 2026.** Stabilize the existing game, then implement **every named element and variant**, then qualify through browsers. This section supersedes older lane alternation, positional numbers and first-undone dispatch. Source behaviors and named requirement IDs remain intact. The [319-identity ledger](../../docs/planning/invest/named-elements.md#execution-contract--complete-named-catalogue-24-october-2026) supplies 302 actual named-element contracts (276 adopted on 24 October 2026, 25 campaign-taught parts ledgered as EL-217 to EL-241 by owner decision, 21 November 2026, and EL-242 ledgered by coordinator decision 27, 23 November 2026), 10 umbrella child dispositions and 7 product-feature contracts. The72 CAT specifications overlap those identities; all 104 current mode records and source-required variants remain obligations. “Potential” no longer excludes an actual named element.

**Current owner follow-ups (27 October):** comparison adoption, synthetic warm-up removal (`822317c`), support coverage (`a98ef86c`) and orientation levelling (`bb35e8c4`) are published. The [owner-approved cap-block completion](../implementation-artifacts/spec-engine-cap-block-game-grade.md) is in progress: replace only the invalid one-substep rod ratio with per-impact non-increasing mechanical energy including small impacts. Retain the active-set candidate, whole-manifold warm-start correction and all settling/Weight/energy bounds. Historical failed attempts remain evidence. Catalogue order is unchanged.

**Selection:** use phases below, then source dependencies and the first ready slice in that phase. Sprint file order is a status view, not a dependency oracle. A blocked slice records its concrete missing capability/decision owner and retry condition; select another independent slice within the same phase. Split parent stories are conjunctions, never implementation dispatch. A named CAT refinement shares its source owner's package where semantically identical, but distinct variants have separate delivery and proof. Existing completion evidence may be reused only after independent applicability review.

1. Delivered at main d87b6a1e (CI 40181624977: 3,952 tests, 0 failed, 0 skipped; deployment 7311595713): 9A.44 Part A, the axial-creep slice `engine-cylinder-axial-creep`, and the 9A.23+9A.25b and 10.2a terminal receipts ([delivered slices](../../docs/planning/delivered-slices.md)).
2. Stabilize in this order (coordinator, 26 Oct 2026; current cap-settling prerequisite and retained orientation retry): delivered `elements-visible-solid` / `elements-visible-solid-sweep` → delivered `engine-hollow-shapes` → delivered `engine-single-point-penetration` → delivered `engine-two-point-friction` → delivered `engine-cap-settling` (`a98ef86c`) → delivered `engine-orientation-levelling` (`bb35e8c4`) → delivered `engine-cap-block` (`6ed1a3e03e94786be964a429d87a6d4a25d9b90c`, full4606/0/0, hooks126/0/0; terminal receipt in current cap-block spec) → delivered `engine-multi-collider-dynamic` (`28a2d302e6c0e1a0e361bfe62beea9b332d6b9f9`, local4822/0/0, hooks126/0/0; terminal receipt in its spec) → delivered `bumper-cap-solid` (`057df5ae99cc8e620ee88cc161bd1497a5e1b980`, local4831/0/0, hooks126/0/0; terminal receipt in its spec) → delivered `face-markings-flush` (`6125040efa3becf31c6a5957f35571735f04972c`, local4845/0/0, hooks126/0/0; terminal receipt in its spec) → delivered9A.44B (`6e0e0ee1799d31211e1769b8883f6bedb3fae396`, local4884/0/0, hooks126/0/0; terminal receipt in its spec) → delivered harness A (`db90de9b231e218025ef72a1007f953e214baee6`, local4901/0/0, hooks126/0/0 and exact-SHA approval; CI/deployment unverified, owner29October says continue without waiting) → delivered harness B (`318d514fba18eae71fa3f9663edb2d13a3cde016`, local4982/0/0, hooks126/0/0 and exact-SHA approval; remote deployment unverified) → delivered TypeScript conversion (`2dae218024ab260cc1797d967b13f2a5ea70d01e`, local4985/0/0, hooks126/0/0 and exact-SHA approval; remote deployment unverified) → test hygiene → 9A.24 → 9A.45. Detailed contracts and [engine and collider stories](#engine-and-collider-slices) below bound this work.
3. **Engine programme first (owner decisions 3a and 22, 22–23 November 2026).** Before it, only repairs and tooling run: visible-solid repair parts A, B, B2 and C, the Baseball repair, and structural stories S3, S5 and S6, with S5b and S6's follow-on slices S6b–S6g (owner decision 24h) and S5c-1, S5c-2 and S5c-3 after S5b (owner decision 24p; coordinator ruling, 26 November 2026). Under owner decision 24m (24 November 2026) the lane 3 slices (lettered level ids, then the content slice once its contract passes planning) and the lane 4 policy-tool fixes also run before the programme, in up to four lanes. Then the [engine programme](#engine-first-capability-stories-21-november-2026) runs in its stated order (DOC-ENGINE-STATUS to ENGINE-SOA-SIMD), then ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL (owner decision 18). The goal slices, Early level proofs, tests-only patches, closure reviews and new elements wait for the programme; after it and the gas, liquid and thermal engines, element slices follow the order in which the campaign levels first teach them (owner decision 24a; the roadmap's [delivery order](../../docs/planning/invest/vertical-delivery.md#capability-cluster-order)), whichever engine capability they need, each also waiting for the engine stories its *Depends on* lines name. The remaining CAT slices follow their engine stories: Story 12.4 (Bellows) after ENGINE-GAS-NETWORK; the Epic 13 children after ENGINE-OPTICAL; 14.2, 14.1, 14.3, 14.4 after Story 14.0. Resolve CAT-023 offset-COM and its motion-piece origin/tick obligations before its complete claim; attach all F.16 catalogue remainders and required F.4 collider behavior to their actual first consumer rather than leaving them after catalogue qualification.
4. Complete every remaining `named-*` slice in the roadmap's [delivery order](../../docs/planning/invest/vertical-delivery.md#capability-cluster-order): after the repairs and tooling of owner decision 22, the programme (owner decision 3a) and the gas, liquid and thermal engines (owner decision 18), element slices are delivered in the order the campaign levels first teach them, whichever engine capability they need (owner decision 24a; first-use reservations in [requirements, campaign element coverage](../../docs/planning/requirements.md#campaign-element-coverage), chapters in [campaign plan](../../docs/planning/requirements.md#campaign-plan)). Each missing engine capability is delivered by its [engine-first story](#engine-first-capability-stories-21-november-2026) before the first element that needs it. Historical row, key and family order is not a dependency. AGENTS.md's **Family slice** and **Duplicate closure** rules govern which identities may share a slice; the groupings are in [delivery batches](../../docs/planning/invest/delivery-batches.md). Its source spec/requirement and canonical S owner are the detailed contract; its native entry decisions stay within the slice. Carry remaining CAT modes/fixtures with their owners. A feature/umbrella cannot stand in for a physical element.
5. After all named intrinsic element implementations, complete the bounded native stage of [Story 15.0](#story-150-physical-placement-nudging--difficulty-assistance) and all remaining required native mode/collaborator integrations under their existing owners. Story 15.0 owns the CAT-049/CAT-050 assistance clauses retained by Stories 6.9/6.10. This native behavior precedes catalogue closure; exhaustive difficulty/campaign sweeps and browser proof do not.
6. Verify full catalogue closure: every CAT obligation and all 302 named contracts have actual implementation and passing unit AND integration proof, including Story 15.0 native assistance and later collaborator cases. Only then expand the campaign (at least 150 levels; 244 planned), deliver retained product features, re-enable the explicit browser deferral list and run post-catalogue Chrome E2E, exhaustive difficulty/campaign, device/performance and release gates. AGENTS.md's **Early level proof** rule is the one exception: after the engine programme (owner decision 22), a planned level that uses only delivered parts may be authored and proven natively before closure; its browser proof stays in the post-catalogue phase.

**Required-now proof:** production C# unit and shared native integration tests; node harnesses test shipped compiler output at genuine browser/worker boundaries. Given each source construction, when the real production system advances bounded ticks, then independently derived outcomes, controls, boundaries, Reset/save/load and ordinary lifecycle hold. All required native tests run with Skipped: 0. No browser test writing, execution or provisioning is a prerequisite now. Existing story Chrome clauses are retained in the final-phase backlog; they do not govern implementation completion before catalogue closure. One matching build, then `--no-build --no-restore`; no output hashes/private reach-ins. Separate implementer/reviewer and individual publication remain required.

### Stabilization contracts

Each item is one bounded native slice with its current source paths, deletion scope and observable exit. Source acceptance and docs/testing-guidance.md remain mandatory. No additional unrelated profiling, audits or tooling platform may be inserted before element delivery.

| Slice / sprint key | Paths and deletion | Native acceptance and dependencies |
|---|---|---|
|`engine-cap-block` (published; owner-approved game-grade acceptance)|`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs` normal block and restitution block; delete both per-row recovery paths.|[Story engine-cap-block](#story-engine-cap-block).|
|`elements-visible-solid`|The listed element packages' collider declarations and a collider-vs-art test per element; delete each collider approximation that differs from the art.|[Story elements-visible-solid](#story-elements-visible-solid).|
|`engine-hollow-shapes`|Shared shape-pair table, compiler, the Pipe, PipeBend45, PipeBend90 and Funnel packages, and the BallDetector collar and PoweredGate collars and sleeve; delete their box approximations.|[Story engine-hollow-shapes](#story-engine-hollow-shapes).|
|`engine-two-point-friction`|Manifold friction rows in the constraint solver; delete the order-dependent per-point friction sequence.|[Story engine-two-point-friction](#story-engine-two-point-friction).|
|`engine-cap-settling`|`CuriousContraptions.Simulation/WorkshopCollisionKernels.cs` shared manifold reduction; delete depth-only fill and independently chosen diameter selection while retaining deepest coverage and line extent.|[Current cap-settling spec](../implementation-artifacts/spec-engine-cap-settling.md): real production cap support/rest, thin/partial support, energy and lifecycle proof; independent review and publication before retrying the retained orientation candidate.|
|`engine-orientation-levelling`|Orientation integration of resting bodies; delete the dropped sub-ulp levelling path.|[Story engine-orientation-levelling](#story-engine-orientation-levelling).|
|9A.44B / `9a-44b-f32-double-game-values-and-tick-clock`|Construction/compiler/schema/codec/presentation/clock and their actual callers; delete every double game calculation, floating game time and conversion shim.|[Story 9A.44B](#story-9a-44b).|
|`test-workshop-harness-a`|CuriousContraptions.tests shared harness, real bundle/content readers, production command/wire/transport seams; migrate existing integration callers and delete duplicate builders reaching internals.|Given the same authored construction, when native command→wire→solver→read/event/presentation runs at fixed ticks, then positive/control results, Pause/Step/Reset and save/load agree fieldwise. Inject invalid command/enum/connection boundary and prove atomic refusal. No browser, wall-clock sleep or mocked solver.|
|`test-workshop-harness-b`|Existing lesson and cross-element integration tests in CuriousContraptions.tests, shared A harness and content; remove superseded duplicate cases only with coverage mapping.|Given every currently playable lesson and admitted element interaction, when its positive and one-cause control run at all three supported cadence profiles, then goals/events/positions and restoration match independent expectations. Enumerate actual lesson/element coverage and rejected boundaries; this is current-catalogue coverage, future slices extend it. Depends on A; no new coverage platform.|
|`runtime-typescript-conversion`|Both worker wwwroot hosts, web client/rings and game node harnesses; pinned compiler config/build/publish/CI; delete handwritten JS/MJS source after callers move.|Given clean checkout/build, when strict pinned tsc compiles, then shipped modules are its output and all game node harnesses exercise that output. No any escape. Public worker/ring lifecycle positive/control/boundaries remain. Replace remaining harness sleeps with deciding events/quiescence; encapsulate pose/shared memory as required. Browser suites stay deferred.|
|`test-behavioral-hygiene`|Game C# tests and node harnesses, including WorkshopActivationAnimationTests and the save re-encode asserts at `CuriousContraptions.tests/ContactWorkInstanceTests.cs:102` and `CuriousContraptions.tests/SliderInstanceTests.cs:145` (about 57 test files used hash or byte-identity patterns on 25 Oct 2026); delete output-hash/byte pins and private reach-ins after equivalent behavior coverage exists.|Given each replaced assertion, then an interface-level positive/control/boundary test asserts the behaviour directly, so that a plausible defect would fail it (judged by reading the assertions; no mutation runs before the post-catalogue E2E phase). Preserve legacy input fixture files without hash pins. Inventory every removed assertion and replacement; all required tests pass with zero skips.|
|9A.24 / `9a-24-typed-observation-port`|Existing story's typed read-only port and console-channel deletions.|Given typed observation requests, when read against committed state, then values/refusals are correct without mutation; validate through native/node tests. Browser consumers stay deferred.|
|9A.45 / `9a-45-compile-time-isolation-gate`|Existing assembly split/build scan, remaining Pipe generic arms and unused TimerObservation. Delete known-violation list only when empty.|Given normal builds and planted source violations, when gate runs, then clean source passes and forbidden element coupling fails at file:line. Pipe geometry/joins/lifecycle pass native behavior tests; no declaration deletion.|
|`engine-supported-impact-restitution` (published at `a39014f4`, 30 October 2026; receipt `3df93f14`; both on published main)|`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs` restitution pass and `CuriousContraptions.tests/WorkshopPhysicsLawRestitutionTests.cs`; the per-manifold restitution that omitted the supporting contacts was replaced.|Delivered: a bounce is solved together with the active supporting contacts of the dynamic bodies it touches, so a ball dropped on a resting dynamic box rebounds at its supported limit ([spec](../implementation-artifacts/spec-engine-supported-impact-restitution.md); tests `ABallDroppedOntoARestingDynamicBoxReboundsAtItsSupportedLimit`, `AnInelasticBallOnARestingBoxDoesNotRebound`, `ABallDroppedOntoATwoBoxDynamicStackReboundsWithoutCollapsing`). Remaining defect, not a reopening: a restitution group of more than eight points is silently cut to eight (`WorkshopConstraintSolver.cs:685-688`); `ENGINE-ISLANDS-SLEEP` removes the cut. The deferred-work entry that still calls the supported-impact loss open (`deferred-work.md:135-137`) is stale.|

**Owner-requested child (26 October 2026):** [`test-semantic-digests`](../implementation-artifacts/spec-test-semantic-digests.md) replaces replay fingerprints and legacy fixture digest pins first: 12 methods, 57 literal cases. [Replacement inventory and remaining groups](../implementation-artifacts/test-behavioral-hygiene-inventory.md). Terminal publication Pass at `b3905fcda3df89b38140c9610b8ff2034e4078a8`: CI 40249169834 passed 4,212 tests with zero failures/skips, artifact 12620546696 and deployment 7321697410 verified; [terminal receipt](../implementation-artifacts/spec-test-semantic-digests.md). The broader hygiene row now has its remaining defined-interface replacements implemented; final local verification and independent Auto review remain pending in the [current spec](../implementation-artifacts/spec-test-behavioral-hygiene.md). Historical child proof does not qualify the changed tests. This cleanup imports no solver changes.

**28 October gameplay-fidelity cleanup, owned by `test-behavioral-hygiene`:** reassess `WorkshopPhysicsLawTests` rolling checks that require every-tick monotonic speed and use a1e-6m/s reversal threshold (including the +X/+Z controls). Replace precision-only assertions with net slowdown, stopping behavior and cumulative backward travel over a meaningful play window, with declared-resistance/zero-resistance controls and functional tolerances established before outputs. Preserve energy/contact-law coverage. Replace the redundant digest of the known-empty solver-violation list with a direct `Assert.Empty` while preserving forbidden-source detection. The compound slice removes the F32 historical timing/endpoint gates and WorkshopRead motion/empty-response byte comparisons; this is scoped cleanup, not a claim that all game tests comply. No new framework or solver change is authorized by these follow-ups.

**27 October owner sizing amendment:** current `engine-multi-collider-dynamic` includes actual-demand storage with no arbitrary scene ceilings, sparse interaction pairs, all declared child visuals/drives, multiple receivers, and atomic event/animation growth. The [revised spec](../implementation-artifacts/spec-engine-multi-collider-dynamic.md#actual-demand-sizing-and-sparse-interaction-ownership--owner-amendment) is the current handoff. This supersedes the temporary9A.45 presentation deferral and one-guide-per-payload restriction; no new catalogue element or aggregate battery-debit model is added. Browser/performance qualification remains at its named gate.

**Stabilization exit:** every row above has terminal scoped native/publication Pass (or independently applicable existing proof), zero unresolved ordinary correctness regressions, no unsupported Half/double game path, strict compiled TS at every changed boundary, current integration matrix passing and isolation list empty. Supported-impact restitution, the just-in-time dependency of 10.3, was published at `a39014f4` (30 October 2026) before 10.3; its remaining eight-point group cut belongs to `ENGINE-ISLANDS-SLEEP`. Existing stress/performance follow-ups retain their named later gates.

<a id="engine-and-collider-slices"></a>
### Engine and collider slices (owner decisions 24–25 Oct 2026)

**Creep scope (owner, 25 Oct 2026, "Land M1+M2, follow-up").** The residuals measured by the published axial-creep slice are follow-up engine work: sub-f32-step orientation levelling, the drop-ramp landing yaw kick and the rank-3 cap block. They are never committed red and never absorbed by a loosened bound. **One normal-branch rule** (owner: "we shouldn't have per-point rules"): every contact point takes the same branch rule. The draft [`spec-engine-orientation-levelling-draft.md`](../implementation-artifacts/spec-engine-orientation-levelling-draft.md) holds the diagnosis for `engine-cap-block`, `engine-two-point-friction` and `engine-orientation-levelling`; it is split into those three slices.

**Collider fidelity (owner, 25 Oct 2026).** An audit found that only simple solids have exact colliders. (a) "Visible = solid": every part a player sees as solid collides with its true shape; this overrides element docs that called such parts art only. (b) "True hollow shapes": ring, torus-segment and frustum collider kinds replace the box approximations of hollow parts.

<a id="story-engine-cap-block"></a>
### Story engine-cap-block: Well-posed contact block for coplanar manifolds
*Sprint key: `engine-cap-block`. Status: **published** at `6ed1a3e03e94786be964a429d87a6d4a25d9b90c` under the [27 October owner-approved game-grade contract and terminal receipt](../implementation-artifacts/spec-engine-cap-block-game-grade.md#terminal-publication-receipt--27-october-2026). Independent review, full local4,606/0/0, hooks126/0/0 and exact artifact/deployment verification pass. The former ratio failures and signed four-substep diagnosis remain [historical evidence](../implementation-artifacts/spec-engine-cap-block-after-orientation.md); the owner decision is resolved. Friction, penetration, support coverage and orientation prerequisites are published.*

*Deletes:* both per-row recovery paths in `SolveNormalBlock` and `ApplyRestitution`, and the diagonal block regularisation; structural rank must not be hidden.

**Acceptance Criteria:**
- **Given** a cylinder cap or box face resting on a plane (four coplanar points whose normal coupling matrix has rank 3), **When** the normal block solves, **Then** every multi-point normal block and the restitution block use one rule: the linear-complementarity solution found by fixed-order active-set enumeration (all points, then each smaller subset in a documented index order, the empty set last). The first well-conditioned set whose impulses are all ≥ 0 and whose inactive transformed soft/speculative residuals are nonnegative within the documented numerical allowance is accepted; a rank-deficient set is skipped as a candidate. The residual is Kz-b in m/s, preserving NormalBranch rather than requiring raw normal velocity to be nonnegative.
- **And** no per-row recovery, fallback, mode or flag remains. The coherent slop band and the rolling axes per manifold are unchanged.
- **And** the rod (r 0.1 m, h 0.4 m, 1 kg, Dead material, released at 10.5° and 12.5°, azimuths 0/22.5/45°, cadences 60/120/240 Hz): every observed rim impact has non-increasing mechanical energy, including small and terminal impacts, with only independently derived f32 measurement uncertainty. It never passes over its rim and remains upright (tilt < 1°) from the unchanged model settling deadline with no +0.01 margin. Above-rim controls topple. The 0.5 mm Weight path limit and original energy gates are unchanged.
- **And** the independently justified energy gates keep their bounds and seeds. `F32BehaviourIdentityTests` follows the owner’s 28 October gameplay-fidelity direction: goals, typed causal event order, semantic replay and Reset replace migration-only 12-tick/5.1 mm comparisons; timer/delay/cadence feature timing remains. The 60-box face sweep (`BoxesDroppedOntoAFaceStayPutAtRest`) retains its original scenes, seed and 4 s settling + 6 s every-tick observation. Its existing implementation-derived resting envelope is `min(50 µm, max(5 µm, 1e-4 × min(2hx,2hz)))`; the 28 October gameplay-fidelity policy requires functional justification before treating that numerical bound as a blocker. Held-back boxes and slow-motion controls retain their coverage. The superseded nanometre-speed observation remains historical evidence per [physical comparisons](../../docs/testing-guidance.md#physical-comparisons-owner-26-october-2026). The enumeration rule, its order and its tolerance are written into `docs/gpu-f32-physics.md`, and the recovery text is deleted.
- **And** the full solution suite passes with Skipped 0.

<a id="story-elements-visible-solid"></a>
### Story elements-visible-solid: Every visible solid collides with its true shape
*Sprint key: `elements-visible-solid`. Status: in progress (next after the cap-block HALT). Owner decision (a), 25 Oct 2026.*

*Current state (collider audit, 25 Oct 2026):* the switch button collides as a box whose corners reach 0.55 m against the drawn 0.38 m button; the ball detector's outer surface stops at 0.70 m against the drawn 0.82 m; the lamp bulb and stem, the delay box's 1.25 m housing, the ramp rails, the battery terminal and the powered gate's collars and outer surfaces do not collide with their drawn shapes.

*Deletes:* each listed element's collider approximation that differs from its art.

**Acceptance Criteria:**
- **Given** each listed part, **When** its colliders compile, **Then** each visible solid collides with its true shape using the existing collider kinds: the lamp bulb as a sphere and its stem as a cylinder (CAT-035); the switch button as a cylinder of the drawn 0.38 m radius (CAT-063); the delay box's 1.25 m housing (CAT-022); the ramp rails (CAT-054); the battery terminal cylinder (CAT-005).
- **And** the ring-shaped parts, the ball detector collar (CAT-002) and the powered gate collars and sleeve (CAT-051), are solid out to their drawn outer radius (the detector to 0.82 m, the gate collars to 0.78 m and the sleeve to 0.70 m). True ring kinds only arrive with `engine-hollow-shapes`, which owns their exact fit.
- **And** one collider-vs-art test per listed element reads its declared art and colliders and fails when a visible solid has no matching collider or a collider reaches beyond its art by more than a stated, derived tolerance. For the ring-shaped parts this slice checks the outer radius; the beyond-art check runs in `engine-hollow-shapes`.
- **And** a ball dropped onto each newly solid feature contacts it (positive), and a ball passing where an old approximation reached beyond the art (for example outside the switch button's 0.38 m radius, inside the old box corner) passes freely (control).
- **And** static restitution is checked: static colliders declare restitution 1.0 while the CAT-048 doc states 0.15. The slice decides each listed element's value from its element doc, records it in that doc and tests a bounce against it.
- **And** existing lessons, Run/Reset and save/load keep their behaviour; the full suite passes with Skipped 0. The box-frame collars of the pipe, bends and funnel take their true hollow shapes in `engine-hollow-shapes`.

<a id="story-engine-hollow-shapes"></a>
### Story engine-hollow-shapes: True hollow collider shapes
*Sprint key: `engine-hollow-shapes`. Status: done, independently reviewed and published; after `elements-visible-solid`. Owner decision (b), 25 Oct 2026; design [hollow geometry](../../docs/hollow-geometry-design.md).*

*Baseline (collider audit, 25 Oct 2026):* the hollow parts are box approximations. The collars are square frames reaching 0.95–1.24 m against the drawn 0.78 m (the funnel inlet 1.6–2.16 m against 1.43 m); the 45° and 90° bends are octagons with 4–8 cm slots between slices; a snapped pipe joint dips about 1.2 cm. The legacy 5 mm surface-error limit was dropped with the CPU segmentation, and nothing replaced it.

*Published (26 Oct 2026):* static Tube/Frustum/Torus geometry, shared contact and atomic admission, refined art and re-authored lessons pass independent Auto review and terminal verification at `eacf561d6833438c9aeaf71c85e52c2626ea33e5`. CI 40239119353: 4,212 passed, 0 failed/skipped; deployment 7320169548. The failed floating-workload build and verified pin repair are retained in the [spec and terminal receipt](../implementation-artifacts/spec-engine-hollow-shapes.md#terminal-publication-receipt--26-october-2026). Basketball seam, Weight bands/lifting eye and pressure-plate ring identity remain required under `engine-multi-collider-dynamic`; browser qualification remains queued for post-catalogue E2E.

*Deletes:* the box approximations in the CAT-048 pipe, the CAT-049 and CAT-050 bends, the CAT-030 funnel, their collars, the CAT-002 ball detector collar and the CAT-051 powered gate collars and sleeve. The open hollow-collider clauses of Stories 6.6, 6.7, 6.9, 6.10 and 6.11, and Story 9R.23's behaviour proofs, run on these kinds.

**Acceptance Criteria:**
- **Given** the shared shape-pair table, **When** this slice lands, **Then** ring, torus-segment and frustum collider kinds exist as declaration data in the one solver, with no element-specific kernel branch.
- **And** the slice states a surface-error bound (the largest distance between the collider surface and the drawn surface), derives it and records it in `docs/hollow-geometry-design.md` and `docs/gpu-f32-physics.md`; a test per kind holds it.
- **And** the pipe, both bends, the funnel, their collars, the ball detector collar and the powered gate collars and sleeve use those kinds, and the collider-vs-art test from `elements-visible-solid` passes for each, including its beyond-art check.
- **And** snapped joints are continuous: a ball rolling through straight–straight, straight–bend and bend–funnel joins meets no dip or step beyond the stated bound (today about 1.2 cm).
- **And** the pipe, both bends and the funnel declare their documented static restitution 0.15 (CAT-048, CAT-049, CAT-050, CAT-030; they declare 1.0 today, deferred from `elements-visible-solid`), each tested by a drop's rebound ratio (0.55 × 0.15 for the Basketball). The lessons this changes are re-authored in this slice, not retuned elsewhere: with the pipe at 0.15, `clear_pipe` no longer solves at precision 0 and `joined_pipe`'s first event moves from tick 240 to 272 (measured 25 Oct 2026); both solve again and keep the event-identity bound.
- **And** controls hold: an oversize payload jams, a ball aimed beside a mouth misses, and an initially overlapping placement rejects at compile. Run/Reset, save/load and the energy gates hold; the full suite passes with Skipped 0.

<a id="story-engine-single-point-penetration"></a>
### Story engine-single-point-penetration: Bound deep contact recovery
*Sprint key: `engine-single-point-penetration`. Status: terminal Pass at 3a633495da8868cb2536f81edecadba629da6c2d; CI 40252950208 passed 4,276/0/0, artifact 12621889117, deployment 7322234061. Prerequisite integrated once into retained friction. [Bounded spec and terminal receipt](../implementation-artifacts/spec-engine-single-point-penetration.md).*

*Deletes:* the arbitrary 3 m/s push-out cap; replaces it with one generic rate derived from contact slop over the longest committed interval (0.012 m/s).

**Acceptance Criteria:** deep centered/oblique contacts recover inside 0.5 mm, preserve committed energy at 60/120/240 Hz, shallow/resting behavior and semantic Reset/readmission; the exact blocked friction trajectory and unchanged full energy/drop/geometry controls pass with this delta. Full native round, five Auto layers and exact-SHA publication remain required; browser qualification is pending post-catalogue.

<a id="story-engine-two-point-friction"></a>
### Story engine-two-point-friction: Simultaneous two-point friction
*Sprint key: `engine-two-point-friction`. Status: terminal publication Pass at `3462ad836dccffc1e1328b1ed99de490b4336786`; CI 40297421010 passed 4,365/0/0, artifact 12639894754 and deployment 7328603563 verified. [Contract, historical failures, two unverified numerical risks and terminal receipt](../implementation-artifacts/spec-engine-two-point-friction.md). Original gameplay bounds, full3D heading32u and semantic saved replay remain.*

*Deletes:* the order-dependent sequential per-point friction solve on multi-point manifolds.

**Acceptance Criteria:**
- **Given** the 10.2a Weight rolling off the 15° drop ramp and landing on its side, **When** it rolls about 7 m and stops, **Then** it stays within 0.5 mm of straight (today 1.25 mm, from a 1.77e-4 rad landing yaw kick), and the restored drop-ramp test passes at its unchanged bound.
- **And** the heading does not depend on the order of the friction rows: the same landing with the contact points in reverse order ends at the same heading within f32 rounding (control).
- **And** the flush-ramp roll, the energy gates, `F32BehaviourIdentityTests`, `LaunchLessonSolveTests` and the restitution laws pass at unchanged bounds; the full suite passes with Skipped 0.

<a id="story-engine-orientation-levelling"></a>
### Story engine-orientation-levelling: Sub-f32-step orientation levelling
*Sprint key: `engine-orientation-levelling`. Status: done and published at `bb35e8c4`; [terminal receipt](../implementation-artifacts/spec-engine-orientation-levelling.md#terminal-publication-receipt--27-october-2026). Independent Auto review/repairs passed; full local 4,453/0/0 and hooks 126/0/0; exact-SHA artifact/deployment verified. Original energy/lifecycle criteria remain. The subsequent cap-block implementation is published at `6ed1a3e03e94786be964a429d87a6d4a25d9b90c`: full local4,606/0/0, hooks126/0/0, exact artifact/deployment verification; [terminal receipt](../implementation-artifacts/spec-engine-cap-block-game-grade.md#terminal-publication-receipt--27-october-2026).*

*Deletes:* the integration path that drops a sub-ulp levelling rotation while its matching translation is still integrated.

**Acceptance Criteria:**
- **Given** a lying cylinder set down tilted 1e-5 and 1e-4 rad, a box standing on its end tilted 1e-4 rad, and the original 200 random lying drops, **When** they rest from 4 s to 10 s, **Then** every tick's horizontal excursion from the original settled reference is within min(50 µm, max(5 µm, 1e-4 × nominal horizontal size)), with coordinate spacing at most allowance/8. Preserve original seeds, angular, support, penetration and 2 mm/s speed limits. Named fixed scenes pass at 60/120/240 Hz. This adopted game-grade policy supersedes the historical resting speed pins.
- **And** the energy gates keep their bounds and seeds. A design that breaks them fails: the compensated remainder "C" broke the Dead gate (CylinderOntoBox 4.109 mJ against 2.043 mJ).
- **And** the exactly-level control retains its 1–10 s displacement window and the 2e-6 rad microtilt retains its 2–10 s displacement and angular-nongrowth window. Run/Reset, supported construction Save/Load replay and fresh/reused engine trajectories preserve typed physical observations and ticks. No body ABI/save change or persistent remainder is allowed. Genuine slow translation and tiny/free rotation remain observable.
- **And** the single-point curved-side control compares the same tilted rocking cylinder at Crr 0 and 0.1 under the existing geometry-derived rolling axis; pitch is not damped as axial rolling, while the actual rolling control slows. No alternate law or mutation is introduced. Published sustained cap translation/rotation, both orders and 17° controls at all three cadences, thin-wall spanning and replay retain their unchanged bounds.
- **And** the full suite passes with Skipped 0.

<a id="story-9a-44b"></a>
### Story 9A.44B: `double` game values to f32, the integer-tick clock and a `double` guard
*Sprint key: `9a-44b-f32-double-game-values-and-tick-clock`. Status: done, published `6e0e0ee1799d31211e1769b8883f6bedb3fae396`; [terminal receipt](../implementation-artifacts/spec-9a-44b-f32-game-values-and-integer-time.md#terminal-publication-receipt--29-october-2026). Part B of [Story 9A.44](#story-9a44-f32-lane-completion--remove-every-binary16-lane); Part A was published at d87b6a1e.*

*Deletes:* every `double` game calculation, state and presentation value, floating game time, and their conversion shims.

**Acceptance Criteria:**
- **Given** every game field, **When** it is authored, encoded, simulated and read, **Then** f32 is retained end to end with source behaviour and envelope controls.
- **And** integer ticks plus an f32 phase replace floating game time; finite, range, undefined-enum and tick-overflow refusals are explicit and atomic.
- **And** `double` appears only at named external adapters, each with its conversion and validation at the boundary.
- **And** an architecture guard fails on a `double` game value in production source (positive) and accepts a named external adapter (control).
- **And** only the JS modules this closure affects convert to strict TypeScript first, with the pinned `tsc` in build, publish and CI and shipped-output tests; the remaining modules belong to the conversion slice.
- **And** independently derived behavioural tests keep every element inside the game-grade envelope; the full suite passes with Skipped 0.

<a id="catalogue-slice-ownership"></a>
### Individual catalogue slice ownership

The following children replace grouped implementation dispatch. Parent acceptance remains a conjunction and its original clauses are preserved. Each child reads its exact CAT declaration/requirements, owns all that element's modes and the indicated outcome/control, and follows the native Given/When/Then contract above with boundary, integration and restoration cases. Its files are that CAT's package, necessary generic capability callers, content and native tests; delete its superseded path in the same slice. Parent integration using two children is explicitly owned by the later consuming child; retain its original source criterion there. A first consumer uses an independently completed generic-capability qualification fixture for intrinsic proof when the later named collaborator does not yet exist. No named child starts until the previous named child has completed required-now acceptance and individual publication; all later integration criteria still block full catalogue closure.

|Story / sprint key|Source contract|Prerequisites|Outcome and meaningful control|
|---|---|---|---|
|11.1a / `11-1a-motor`|[CAT-042 Motor](../../docs/planning/elements/CAT-042-motor.md)|Battery and declared shaft load|Supplied shaft delivers bounded torque/work; unpowered shaft delivers none; stalled demand never creates work.|
|11.1b / `11-1b-conveyor`|[CAT-019 Conveyor](../../docs/planning/elements/CAT-019-conveyor.md)|11.1a|A driven belt transports a contacting load; stopped/reversed drive stops/reverses transport; unloaded belt creates no transferable work.|
|11.2a / `11-2a-clutch`|[CAT-018 Clutch](../../docs/planning/elements/CAT-018-clutch.md)|11.1a|Engagement transfers paid torque; disengagement isolates the load; slip and capacity boundaries obey the source work ledger.|
|11.2b / `11-2b-reverse-transmission`|[CAT-057 Reverse transmission](../../docs/planning/elements/CAT-057-reverse_transmission.md)|11.1a and 11.2a|Output reverses relative to input with the source ratio; no input supplies no work; reversal preserves bounded transmitted energy.|
|11.4a / `11-4a-wound-spring`|[CAT-071 Wound spring](../../docs/planning/elements/CAT-071-wound_spring.md)|11.1a shaft contracts|Declared preload releases finite shaft work; an empty store does none; winding cannot exceed capacity.|
|11.4b / `11-4b-cannon`|[CAT-016 Cannon](../../docs/planning/elements/CAT-016-cannon.md)|11.4a|An admitted load launches from paid stored work; unpaid/empty firing does not launch; recoil and store bounds retain source acceptance.|
|12.1a / `12-1a-tennis-ball`|[CAT-064 Tennis ball](../../docs/planning/elements/CAT-064-tennis.md)|Existing sphere engine|Declared material produces the source bounce/airflow response; matched bowling control differs; bounds and restoration retain CAT-064.|
|12.1b / `12-1b-balloon`|[CAT-003 Balloon](../../docs/planning/elements/CAT-003-balloon.md)|12.1a and generic buoyancy/drag first-consumer decision|Buoyancy/drag produce the source rise and deflection; loss/absence controls and envelope bounds hold without a balloon solver branch.|
|13.1a / `13-1a-torch`|[CAT-029 Torch](../../docs/planning/elements/CAT-029-flashlight.md)|Battery and generic optical emission|Powered divergent cone attenuates and is occluded; no power gives no light; angular/energy boundaries retain CAT-029.|
|13.1e / `13-1e-amber-laser`|[CAT-036 Amber laser](../../docs/planning/elements/CAT-036-laser.md)|13.1a generic optical path|Powered collimated beam follows declared spectrum and occlusion; no power/blocker gives no downstream light; no infinite energy.|
|13.2a / `13-2a-broadband-receiver`|[CAT-038 Broadband receiver](../../docs/planning/elements/CAT-038-light_receiver.md)|13.1a/e|Admitted incident power activates; sub-threshold/occluded input does not; exact threshold and saturation follow the source.|
|13.2b / `13-2b-red-receiver`|[CAT-056 Red receiver](../../docs/planning/elements/CAT-056-red_receiver.md)|13.1b red laser and 13.2a|Red-only qualifies; green/blue controls do not; power threshold and electrical response retain source criteria.|
|13.2c / `13-2c-green-receiver`|[CAT-032 Green receiver](../../docs/planning/elements/CAT-032-green_receiver.md)|13.1c green laser and 13.2a|Green-only qualifies; red/blue controls do not; source thresholds and electrical response hold.|
|13.2d / `13-2d-blue-receiver`|[CAT-012 Blue receiver](../../docs/planning/elements/CAT-012-blue_receiver.md)|13.1d blue laser and 13.2a|Blue-only qualifies; red/green controls do not; source thresholds and electrical response hold.|
|13.4a / `13-4a-red-filter`|[CAT-055 Red filter](../../docs/planning/elements/CAT-055-red_filter.md)|13.2a-d and 13.1b-d|Red passes and other channels are absorbed; mismatched serial filter extinguishes; no energy gain.|
|13.4b / `13-4b-green-filter`|[CAT-031 Green filter](../../docs/planning/elements/CAT-031-green_filter.md)|13.4a generic filtering|Green passes and red/blue do not; mixed and zero incident bounds retain source criteria.|
|13.4c / `13-4c-blue-filter`|[CAT-011 Blue filter](../../docs/planning/elements/CAT-011-blue_filter.md)|13.4a generic filtering|Blue passes and red/green do not; mixed and zero incident bounds retain source criteria.|
|13.5a / `13-5a-mirror`|[CAT-041 Mirror](../../docs/planning/elements/CAT-041-mirror.md)|13.1e and 13.2a|The source45-degree arrangement reflects90 degrees; a miss produces no reflection; normal/grazing incidence follows the source.|
|13.5b / `13-5b-splitter`|[CAT-008 Splitter](../../docs/planning/elements/CAT-008-beam_splitter.md)|13.5a|Incident power divides into source-required outputs; absence/control routes and sum of outgoing energy remain bounded.|
|13.5c / `13-5c-combiner`|[CAT-006 Combiner](../../docs/planning/elements/CAT-006-beam_combiner.md)|13.4a-c and 13.5b|Orthogonal source beams combine declared channels; missing-input controls preserve only admitted channels; no invented power.|
|13.6a / `13-6a-cyan-receiver`|[CAT-021 Cyan receiver](../../docs/planning/elements/CAT-021-cyan_receiver.md)|13.5c|Green+blue qualifies; either alone and wrong mixture do not; thresholds retain source criteria.|
|13.6b / `13-6b-magenta-receiver`|[CAT-040 Magenta receiver](../../docs/planning/elements/CAT-040-magenta_receiver.md)|13.5c|Red+blue qualifies; either alone and wrong mixture do not; thresholds retain source criteria.|
|13.6c / `13-6c-yellow-receiver`|[CAT-072 Yellow receiver](../../docs/planning/elements/CAT-072-yellow_receiver.md)|13.5c|Red+green qualifies; either alone and wrong mixture do not; thresholds retain source criteria.|
|13.6d / `13-6d-white-receiver`|[CAT-068 White receiver](../../docs/planning/elements/CAT-068-white_receiver.md)|13.5c|All three channels qualify; every missing-channel control does not; thresholds retain source criteria.|
|13.8a / `13-8a-optical-and`|[CAT-043 Optical AND](../../docs/planning/elements/CAT-043-optical_and.md)|13.1e, 13.2a and 13.5a-c|All four binary input combinations match AND; boundary and power conditions follow source criteria.|
|13.8b / `13-8b-optical-nand`|[CAT-044 Optical NAND](../../docs/planning/elements/CAT-044-optical_nand.md)|13.8a shared logic capability|All four combinations match NAND; missing supply/control and thresholds follow source criteria.|
|13.8c / `13-8c-optical-nor`|[CAT-045 Optical NOR](../../docs/planning/elements/CAT-045-optical_nor.md)|13.8a shared logic capability|All four combinations match NOR; missing supply/control and thresholds follow source criteria.|
|13.8d / `13-8d-optical-or`|[CAT-046 Optical OR](../../docs/planning/elements/CAT-046-optical_or.md)|13.8a shared logic capability|All four combinations match OR; missing supply/control and thresholds follow source criteria.|
|13.8e / `13-8e-optical-xor`|[CAT-047 Optical XOR](../../docs/planning/elements/CAT-047-optical_xor.md)|13.8a shared logic capability|All four combinations match XOR; equal-input control and thresholds follow source criteria.|

The existing13.1b/c/d RGB laser stories now depend on13.1e, not the old grouped13.1 implementation. Each grouped parent11.1, 11.2, 11.4, 12.1, 13.1, 13.2, 13.4, 13.5, 13.6, 13.8 is aggregate-only. Specific child prerequisites above refine its broad parent dependency; other stories requiring the whole parent wait for all relevant children. CAT obligations not in this split table keep their existing individual story, with retained named refinements from the identity ledger.


### CAT-023c: Domino offset centre of mass and presentation origin
*Sprint key: `cat-023c-offset-centre-of-mass`; canonical CAT-023 obligation, before the next Domino-dependent level and before complete catalogue closure. Depends on stabilized f32 body/motion ABI and existing Domino package.*

The declared centre of mass is (0, −0.05, 0) m in the body frame (owner decision 8, 22 November 2026). The slice re-authors `CuriousContraptions.tests/WorkshopDominoTests.cs:46`, which today asserts a zero local centre of mass, to that value, and re-runs `DominoGuardTests`. Given the [CAT-023 source declaration](../../docs/planning/elements/CAT-023-domino.md) with its nonzero centre of mass, when the production solver advances and the presentation samples its motion, then inertia, world centre of mass, body origin and pips follow the declared mass frame within independent analytic/game-grade tolerances. Given a zero-offset control, when run identically, then existing Domino behavior remains; invalid/nonfinite/out-of-range offsets reject atomically. Given an end-of-substep motion sample, when observed at its declared ordinal, then it neither leads physics by a substep nor subtracts the COM twice. Retain cascade, threshold, matched-ball loading, Reset and supported Save/Load source criteria. Files: elements/Domino, generic mass-property/compiler/ABI/motion-read callers and native simulation/presentation tests; delete zero-COM assumptions and stale sample-origin/ordinal paths. Resolve the source offset-range and art decisions before runtime implementation; the offset value itself is decided above. No Domino solver branch. This is a behavior slice, not a completion-only audit.

### Catalogue native closure (aggregate)
*Sprint key: `catalogue-native-closure`; dispatches no new implementation.*

Given all 72 CAT source contracts, 104 current mode records and all 302 named element contracts, when their independently reviewed implementation/unit/integration evidence is reconciled against exact relevant inputs, then every identity, variant, required collaborator and ordinary lifecycle criterion has a terminal applicable Pass, or closure fails naming its existing owner. Given a missing child or stale proof, when checking an umbrella/shared CAT refinement, then the aggregate remains incomplete. Preserve the 10 umbrella and 7 product-feature dispositions, without counting features as elements. No hash count, package count or readiness-doc count establishes behavior. Required now includes the completed native stage of Story 15.0 (closing the CAT-049/CAT-050 assistance clauses from Stories 6.9/6.10), CAT-023c, assigned F.4/F.16 native remainders and other actual first-consumer obligations; browser cases remain pending until this conjunction passes. The existing reviewer records the result; no new framework or recursive audit story is introduced.

## Delivery order (coordinator, 21 Oct 2026, owner-authorised re-examination)

**Authority.**
- The owner, 21 Oct 2026: "you can re-examine the order of the stories determine the value of the sequence and look at how we can get to adding more puzzle elements faster".
- The owner's amendment the same day: "the stories need to include the cleanup of old code, its critical we retire old code and delete it as soon as possible after".

The coordinator adopted the re-examination and decided the items below. This order supersedes the following, which stay as history:
- Open scheduling item (g) point 3;
- the item (i) "Delivery order" line;
- the delivery-order paragraphs of Epic 9R, the visual repair stories and Epic 9A;
- every "Depends on" line that only recorded number order.

`sprint-status.yaml` lists its keys in this order.

**Result: 18 stories before Story 9.5, down from 64.** The 64 were 9R.6–9R.22, 9.4c and 9A.0–9A.45. Rationale:
- Only the critical path a new element needs comes before 9.5. That path is: one declaration source, bundle ids in saves, declared ports, colliders and bindings, the generic editor, a declared timer and switched contact, and the isolation ratchet.
- Each deletion lands in the story that introduces its replacement.
- Every remaining conversion alternates one for one with a new element.

**Decisions.**
- **Scheduling item (g) is overridden.** Only the critical path precedes Story 9.5, and the rest is interleaved with new elements.
- **Old code is deleted in the story that supersedes it (owner amendment).** No mixed old and new path survives the story that introduces the new one. Each critical-path and merged story has an explicit "Deletes:" list, and an AC that every deleted symbol returns 0 production hits, checked by `rg` or by the Story 9A.45a ratchet.
- **Conversions are batched by family.** Each family's existing suite story (9R.8–9R.17) merges in as a "suite first" guard: the suite lands green on the old record path in the family's first commit, and it stays green after the conversion.
- **One new element per story.** Stories 9.7 and 9.8 are split one story per element (AGENTS.md): 9.7a NAND, 9.7b NOR, 9.8a OR and 9.8b XOR.
- **Story 9.5a moves the timer lane to f32** ahead of Story 9A.44.
- **Story 9A.45 splits in two.** 9A.45a is an early ratchet: no new bundle id, and no `is` or `PartId` arm, outside `elements/`. Its known-violations list may only shrink, and it fails when an entry outlives the conversion that owns it. The final 9A.45 is the empty-list gate.
- **Story 9R.18 is dropped as a separate story.** Each closing story appends the dated addenda to the records it closes (table below).
- **Story 9R.20 merges into 9A.25a**, the committed-state bindings. The one-shot bindings stay as 9A.25b and are delivered with Story 9A.23.
- **Story 9A.18 merges into 9A.14.**
- **Story 9R.15's threshold editor** is built with the generic editor, inside C-Switch.
- **Story 9A.1, Dead Legacy Purge, runs right after 9A.0 (owner amendment).** Stories 9A.3 (typed transport commands) and 9A.16 (bundle-only colliders) run right after 9A.17.
- **Every family conversion runs right after 9.5, ahead of every polish or art story,** and alternates one for one with an element story.
  - Conversions are ordered by how much old code they delete. The measure is the production lines that name the retired symbols, from `rg` over `engine`, `ui` and the `CuriousContraptions.*` production projects at `e25082c8`:

    | Family | Lines |
    |---|---|
    | C-Ramp+Receiver | 80 |
    | C-Bumper+Spring | 56 |
    | C-Balls | 43 |
    | C-Lamp+Detector | 22 |
    | C-Memory | 21 |
    | C-Switch | 20 |
    | Domino | 20 |
    | Battery | 19, plus the 156-line `ui/WorkshopBattery.cs` |
    | C-Plate | 17 |
    | C-Clock | 16 |
    | C-Gate | 10 |

  - Hard dependencies override size:
    - C-Switch comes first, because Story 9.5b needs it.
    - C-Balls precedes Domino and C-Bumper+Spring, because 9A.30 adds the dynamic-body compile.
    - C-Gate precedes Battery and C-Plate.
    - 9A.22 precedes C-Memory.
    - C-Memory is the last conversion, because 9A.43 removes the whole whitelist.
  - The conversions are followed by 9A.22, then 9A.23 with 9A.25b, then 9A.44, then 9A.24. The final 9A.45 gate closes them.
- **Polish and art come after all deletion work:** 9R.19, 9R.21 and 9R.22.

**Reconciliations found while applying the order (architect, 21 Oct 2026). Item 4 is open for the coordinator's decision.**
1. **9A.22 comes immediately before C-Memory, not after it.** Story 9A.41's AC needs a `ThresholdReached` ring record, and Story 9A.22 creates that kind.
2. **Story 9.4c comes immediately before C-Gate, not with polish.** The ACs of 9A.19, 9A.39 and 9A.43 require its suites, `cat-005-n19` and `cat-051-interrupt`.
3. **9A.26 is the first deletion slot after C-Switch.**
   - It is pure deletion: eight lesson files and the closed `WorkshopPuzzleId` enum.
   - The lesson files build records of most families: `WorkshopBall`, `WorkshopReceiver`, `WorkshopWall`, `WorkshopBumper`, `WorkshopSpringboard`, `WorkshopDomino`, `WorkshopLamp`, `WorkshopSwitch` and `WorkshopDelay`.
   - Until it lands, Stories 9A.27, 9.5a and C-Switch each edit the lesson callers they break, following the one-line `BumperAdvanced` precedent.
   - Moving 9A.26 onto the critical path after 9A.16 would remove those three edits.
4. **Story 9A.27 and the `CaptureInstance` id list.** The coordinator asked that 9A.27 delete the `CaptureInstance` id list and the per-element configuration panels.
   - Every remaining arm and panel builds or edits a record type that lives until its own conversion. Deleting them in 9A.27 would need either a generic-to-record bridge, which AGENTS.md forbids, or every conversion folded into 9A.27.
   - So 9A.27 deletes the Wall arm and the `wallDimensions` parameter.
   - Each other arm (`"ball" or "bowling"`, `"basket"`, `"ramp"`, `"switch"`, `"delay"`, `"spring"`, `"battery"`, `"bumper"`, `"lamp"`, `"domino"`, `"ball_detector"`, `"pressure_plate"`, `"counter"`, `"clock"`, `"latch"`) and each panel (Springboard, Battery, Bumper, Clock, Delay) becomes a Story 9A.45a known-violations entry, tagged with its owning conversion. The ratchet then forces it out on time.
   - **Resolved coordinator decision (21 October 2026):** 9A.27 owns Wall’s arm/panel/record and the necessary BumperAdvanced caller. Other arms/panels remain owned by their named conversions and 9A.45a; no bridge or combined conversion is required. Production C# unit proof comes first; browser cases remain post-catalogue.
5. **Story 9A.44 runs as soon as its preconditions allow:** right after C-Memory (the last record conversion), 9A.25b and 9A.26. This follows its own rule that no lane is migrated and then deleted. Story 9.5a has already moved the timer lane.

**Full order.** Positions are counted from 1.

*Critical path, before Story 9.5:*

| # | Story | Merges or notes |
|---|---|---|
| 1 | 9R.6 Typed mouth snapping | in flight |
| 2 | 9R.7 `joined_pipe` playable | in flight |
| 3 | 9A.0 Architecture authority | |
| 4 | 9A.1 Dead legacy purge | owner amendment |
| 5 | 9A.4 Lossless bundle build gate | |
| 6 | PKG-1 | 9A.5 + 9A.6 + 9A.12 |
| 7 | PKG-2 | 9A.8 + 9A.9 + 9A.10 + 9A.11 |
| 8 | PKG-3 | 9A.7 + 9A.13 |
| 9 | 9A.14 Bundle is the sole catalogue source | + 9A.18 |
| 10 | 9A.2 Single FlatBuffers codec | |
| 11 | 9A.15 Bundle element id in saves and messages | |
| 12 | 9A.17 Bundle ports and footprints | |
| 13 | 9A.3 Typed transport commands | owner amendment |
| 13a | T.1 Fast-test quick wins, AC1–AC2 shipment | owner, 21 Oct: ship type-check and selection; AC3–AC4 deferred |
| 14 | 9A.16 Bundle-only colliders | owner amendment |
| 14a (optional) | T.2 Native fixture replay | acceleration work, not a blocker for gameplay; covered assertions can use verified native proof |
| 15 | 9A.25a Committed-state bindings | 9A.25 committed-state part + 9R.20 |
| 16 | 9A.27 Generic instance and editor (Wall) | + 9R.14, one-line `BumperAdvanced` edit |
| 17 | 9A.45a Early isolation ratchet | |
| 18 | 9.5a Declared timer and switched contact | 9A.38 + 9R.17; timer lane f32 |
| 19 | **9.5 Hold Timer** | first new element |
| 19a | T.3 Trim duplicated Chrome variants | owner, 21 Oct; after T.2 |

*Fast-test lane (owner, 21 Oct 2026: "we need to find high-confidence alternatives to running the e2e suite constantly as it is too slow ... based on the rank plan when it comes back implement the changes, we want to make progress to get more game elements live").* The ranked plan comes from a read-only investigation of the 9A.15 and 9A.17 Full runs.
- **Where the time goes:** a Full run now takes 52–54 minutes. About 78% of it is the serial phase, and about 1,290 s of that is suites that run alone only by policy, because they are uncalibrated. Fixed launch and boot overhead is about 6%. The rest is real-time Run windows, benches built through the UI, and tick-stepping through the menu, which is about 60× slower than native.
- **What can leave the browser:** about 60–65% of assertions are simulation behaviour or save/load, so they can run outside Chrome.
- **What Chrome caught:** two of the recent Chrome-only catches were stale test code that a TypeScript type-check would have caught.
- **Order (owner course correction, 21 Oct):** ship the bounded T.1 AC1–AC2 changes, then proceed directly to 9A.16 and gameplay delivery. T.1 AC3–AC4 calibration/measurement and T.2 replay are optional acceleration work, not gameplay prerequisites. T.3 follows 9.5 and requires demonstrated native coverage before deleting duplicate assertions. T.4–T.6 stay at the back of the queue. Actual impact and the independent reviewer’s coverage matrix govern checks; see [Verification tiers](autonomous-development-guide.md#verification-tiers).

*After Story 9.5, one deletion-lane story alternates with one element-lane story:*

| # | Deletion lane | # | Element lane |
|---|---|---|---|
| 20 | C-Switch (9A.35 + 9R.15) | 21 | 9.5b Switch electrical mode |
| 22 | 9A.26 Lessons as content data | 23 | 9.6 Both Gate |
| 24 | C-Ramp+Receiver (9A.28 + 9A.29 + 9R.12 + 9R.13) | 25 | 9.7a NAND |
| 26 | C-Balls (9A.30 + 9A.31 + 9R.11) | 27 | 9.7b NOR |
| 28 | C-Bumper+Spring (9A.33 + 9A.34 + 9R.8) | 29 | 9.8a OR |
| 30 | 9.4c Latch-fixture integrations (guards C-Gate) | 31 | 9.8b XOR |
| 32 | C-Gate (9A.19 + 9A.21) | 33 | 10.1 Cylinder |
| 34 | 9A.39 Battery | 35 | 10.2 Pulley |
| 36 | C-Lamp+Detector (9A.36 + 9A.37 + 9R.9 + 9R.16) | 37 | 10.3 Lever |
| 38 | 9A.32 Domino | 39 | 10.4 Counterweight |
| 40 | C-Plate (9A.20 + 9A.40 + F.6 item F-1) | 41 | 10.5 Trampoline |
| 42 | C-Clock (9A.42 + 9R.10) | 43 | 11.1 Motor and Conveyor |
| 44 | 9A.22 Event ring producer | 45 | 11.2 Clutch |
| 46 | C-Memory (9A.41 + 9A.43) | 47 | 11.3 Linear Pusher |
| 48 | 9A.23 + 9A.25b Ring consumers and one-shot bindings | 49 | 11.4 Wound Spring and Cannon |
| 50 | 9A.44 f32 lane completion | 51 | 12.1 Tennis Ball and Balloon |
| 52 | 9A.24 Typed observation port | 53 | 12.2 Fan |
| 54 | **9A.45 Final isolation gate (last deletion story)** | 55 | 12.3 Windmill |
| 56 | 9R.19 Tube glass art | 57 | 12.4 Bellows |
| 58 | 9R.21 Lit palette fidelity | 59 | 13.1 Torch and Laser |
| 60 | 9R.22 Element art polish | 61 | 13.1b Red Laser |

*Element-lane insertion (coordinator decision, 24 Oct 2026): **Engine: restitution energy correctness** runs after 10.1 (position 33) and before 10.2a / 10.2 Pulley (position 35).*
- **Why:** restitution on multi-point manifolds gains energy engine-wide. The box control gains 205 mJ in a tick at e = 0.3 ([10.1 spec, round 4](../implementation-artifacts/spec-10-1-solid-cylinder.md); `evidence-10-1/round5/`). The reviewer confirmed the gain on base a598a09e, with bit-identical cylinder-free scenes ([re-review 3](../implementation-artifacts/review-10-1-solid-cylinder.md)).
- **Entry check:** the Lively gate, re-added as a routine `[Fact]` from `evidence-10-1/lively-gate-source.txt`. It is currently red.

**Re-prioritisation (coordinator, 24 October 2026, from owner directives).** This supersedes the strict lane alternation for the following items, which run before the next element story (10.2b) in this order:
1. **9A.44 f32 everywhere.** This is the owner's priority: "we should be using f32 everywhere". Scope:
   - every binary16 lane;
   - every `double` game value;
   - `double` only at named external adapters;
   - game time as integer ticks plus an f32 phase;
   - a guard.

   It may split into A (solver/wire) and B (presentation/clock).
2. **Engine: lying cylinder axial creep.** Found in 10.2a.
3. **Workshop integration harness.** Owner directive: browser-free integration tests through one shared deterministic native harness (`docs/testing-guidance.md`). A is the harness plus migration; B is the cross-element coverage matrix.
4. **TypeScript conversion.** Owner rule: all non-C# code is strict TypeScript compiled by a pinned `tsc`. This includes the encapsulated shared-memory access for the pose ring.
5. **Test hygiene.** Owner rules: no output-hash assertions, and tests go through defined interfaces.

Then 9A.24, 9A.45 and the element lane continue in their existing order. All browser/E2E tests stay deferred until every named puzzle element is implemented and passing its unit and integration tests (AGENTS.md).

*Status, 25 Oct 2026:* 9A.44 Part A and item 2 were published at d87b6a1e. The [Current execution plan](#current-execution-plan) supersedes this list: the engine and collider slices run next, then 9A.44B and items 3–5.

From position 62, only the element lane remains, in its existing order:
- 13.1c, 13.1d, then 13.2–13.8;
- 14.0, 14.2, 14.1, 14.3, 14.4;
- 15.0–15.15;
- 16.1–16.4.

The filed follow-ups F.1–F.11 stay last (F.10 is merged into 9A.1).

**The last deletion story is 9A.45, at position 54 overall: the 35th story after 9.5.**

*Constraints this order satisfies:*
- C-Gate (32) precedes Story 11.3 (47).
- 9A.23 + 9A.25b (48) precede Epic 14. If an element story before position 48 needs a new one-shot cue, 9A.22 and 9A.23 + 9A.25b move ahead of it.
- 9A.26 (22) precedes Epic 15.
- From position 17 on, the 9A.45a ratchet makes every new element declaration-only.

**Reopened-record closure (replaces Story 9R.18).** Each closing story's terminal review appends a dated addendum ("Addendum 21 Oct 2026+, retro-verification") to the original story's review record. Each addendum meets Story 9R.18's evidence ACs for that record:
- the reviewer is neither the implementer nor the original reviewer;
- raw logs are saved and referenced by path, and carry the commit, the bundle identity and the run's own timestamps;
- every quote matches its file, and every cited file exists;
- the original text is unchanged (additions only).

`sprint-status.yaml` marks the original `done` only when its last closing addendum records a fresh terminal Pass.

| Original | Closing stories |
|---|---|
| 6.5b | 9A.14, 9A.15 |
| 6.5c | 9A.16, 9A.17 |
| 6.5d | C-Ramp+Receiver (9R.12, 9R.13), C-Balls (9R.11), 9A.27 (9R.14), 9A.16 |
| 6.5e | C-Bumper+Spring (9R.8), C-Switch (9R.15), C-Lamp+Detector (9R.16), 9.5a (9R.17), 9A.16, 9A.17 |
| 6.5f | 9A.16, 9A.17, 9A.25a, 9A.27 and every conversion; closes with C-Memory |
| 6.6, 6.7 (weak) | PKG-2 |
| 6.8 (weak) | C-Lamp+Detector (9R.9) |
| 6.9 | 9R.6, PKG-2 |
| 6.9b | 9A.1 (`AbiOffsets`) |
| 6.10 | 9R.6, 9R.7, PKG-2 |
| 6.11 (weak) | 9R.6, PKG-2 |
| 8.1 | 9A.39, against its amended acceptance (the Motor clause moved to Story 11.1) |
| 8.2 (weak) | C-Gate |
| 9.1 | C-Plate |
| 9.2 | 9A.2, C-Memory |
| 9.2b | 9A.0; closes with the final 9A.45 |
| 9.3 | C-Clock (9R.10) |

These done stories still have open clauses, each recorded with its owner:
- the 6.9 and 6.10 nudging clauses → Story 15.0;
- the 6.11 integrations → PKG-2;
- the 8.2 CAT-051 Q6 clause → Story 9.4c.

---

## Epic List

1. **Epic 1: Engine Core Foundation** (ENGINE-CORE-1, 2a1, 2a2, 2a3, 2a4) — *[Done]*
2. **Epic 2: Compliant Solver Streamlining** (ENGINE-CORE-2b1, 2b2, 2b3, 2b4) — *[Done]*
3. **Epic 3: Decoupled Sensor Evaluation** (ENGINE-CORE-2c) — *[In-Progress]*
4. **Epic 4: Dedicated WebAssembly Animation Worker** (ANIM-1a, ANIM-1b, ANIM-1c) — *[Backlog]*
5. **Epic 5: Dynamic Polyhedral Rigid Bodies & Domino Mechanics** (CAT-023a, CAT-023b) — *[Backlog]*
6. **Epic 6: Core Interactive Catalogue Elements & glTF Data-Driven Architecture** (CAT-014 Bowling Ball, CAT-015a-b Bumper, CAT-062a-b Springboard, Story 6.5b Dynamic PartId Discovery, Story 6.5c glTF Collider Extraction, Stories 6.5d-e Existing Implementation Conversion, Story 6.5f Zero-Remnant Purge, CAT-048a-b Pipe, CAT-002 Ball Detector, CAT-049 45° Pipe Bend, CAT-050 90° Pipe Bend, CAT-030 Funnel) — *[In-Progress]*
7. **Epic 7: Legacy Code Purge & Zero-Remnant Clean Architecture** (LEGACY-0a Historical Archives, LEGACY-0b Probe Tools, LEGACY-0c Dead Tests, LEGACY-0d Legacy CPU Physics, Procedural PartArt & Unshipped Engine Purge) — *[In-Progress]*
8. **Epic 8: Supplied Electrical Power** (Battery Power Source, Powered Gate) — *[Backlog]*
9. **Epic 9: Activation, Timing & Electrical Logic** (Pressure Plate, Pulse Counter, System Clock, State Latch, Hold Timer, Dual-Supply Both, Electrical Logic Gates Nand/Nor/Or/Xor) — *[Backlog]*
   - **Epic 9R: Retro-Verification Repair** (Stories 9R.0–9R.18: real C# static gate, exact Run/Reset snapshots, collider table sized at Run, own-supply gate routes, per-plate load rows, plate ActivationOut, typed mouth snapping, `joined_pipe` and `spring_forward` playable, cat-002 and cat-017 proofs, seven missing element suites, independent re-verification with dated review addenda). Delivered after Story 9.4b and before Story 9.4c (coordinator decision under owner delegation, 18 Oct 2026). Its section precedes Epic 9A. — *[Backlog]*
   - **Epic 9A: Sole Architecture Conformance (Project-per-Element, Command/Query, Event Disruptor)** (Stories 9A.0–9A.45: authority docs, legacy purge, single codec, typed transport controls, lossless bundle, nine missing element packages, bundle as the sole catalogue/collider/port/icon source, declared drive and load sensor, event ring producer and consumers, typed observation port, package presentation bindings, lessons as data, one generic-instance conversion per element, f32 lane completion, compile-time isolation gate). Delivered after Stories 9.4 and 9.4b and before Story 9.5 (owner decision, 18 Oct 2026); Story 9.4c precedes it by coordinator placement. Its section follows Epic 9. — *[Backlog]*
10. **Epic 10: Constrained Mechanics & Elastic Fixtures** (Solid Cylinder Collider & Inertia, Pulley, Rope Anchor, Impact Lever, Counterweight, Trampoline) — *[Elements delivered natively; resting-stack settling is owned by ENGINE-ISLANDS-SLEEP]*
11. **Epic 11: Rotary Drive & Mechanical Actuators** (Electric Motor, Conveyor Belt, Mechanical Clutch, Reverse Transmission, Linear Pusher, Wound Spring Motor, Toy Cannon) — *[Done natively; browser pending]*
12. **Epic 12: Conserved Airflow, Pneumatics & Buoyancy** (Tennis Ball, Floating Balloon, Electric Fan, Windmill, Pneumatic Bellows) — *[In-Progress: Bellows 12.4 remains]*
13. **Epic 13: Optical Propagation, Routing & Sensing** (Torch, Laser, Red/Green/Blue Lasers, Tuned Optical Receivers, Beam Shutter, Spectral Color Filters, Flat Mirror, Beam Splitter & Combiner, Secondary Receivers, Solar Panel, Optical Logic Gates) — *[Backlog]*
14. **Epic 14: Acoustic Emission & Reception** (Sound Level Meter, Service Bell, Audio Speaker, Wind Chimes) — *[Backlog]*
15. **Epic 15: Progressively Taught Campaign (at least 150 levels)** (Stories 15.1–15.15, one story per chapter: 15 chapters as the [requirements campaign plan](../../docs/planning/requirements.md#campaign-plan) defines them, 244 levels planned, with verified UI solutions and retry loops) — *[Backlog]*
16. **Epic 16: Release Qualification, Performance Audits & Production Packaging** (Cross-platform headless/browser performance audit, WebGL2/WebGPU qualification, 60 FPS display pacing, memory leak audits, and release candidate bundle) — *[Backlog]*

Story order follows the owner re-ordering of 9 Oct 2026 (dependencies): each story comes after what it needs, except the open scheduling items listed in epics.md (below). Every moved story carries a "Depends on" line naming its former number. The old-to-new mapping is in [vertical-delivery.md](../../docs/planning/invest/vertical-delivery.md#owner-reordering-dependencies).

*Superseded for Epics 9R and 9A and Stories 9.4c–9.8 by the [Delivery order of 21 Oct 2026](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination). The placement wording in the Epic 9R and 9A entries above is history.*

### End-to-end scope rule (latest owner decision, 21 Oct 2026)

This changes enforcement cadence for every story AC/spec below, including per-story Chrome and cumulative Full wording; behavior, requirement IDs and named release E2E remain intact:
- Per change, build the affected closure once and run fast unit/native controls with matching outputs using `--no-build --no-restore`. Rebuild only when relevant inputs change; never test stale binaries.
- Routine E2E is deferred until all puzzle elements are implemented, then used to find remaining gaps. Each slice explicitly includes dead-code/caller/fixture cleanup and latest-design alignment tasks. Implementation completion requires production C# UNIT tests as PRIMARY proof, integration supplemental, covering interactions, negative/boundary cases, tick progression and supported Reset/save/load, plus independent review. Browser qualification stays pending. Keep pending element/infrastructure/integration cases in existing records; no counter or new test/fixture framework.
- No Chrome is required during catalogue implementation, including for simulation unit tests. Use the existing native C# execution path. Broaden post-catalogue E2E gap-finding as necessary; preserve earlier evidence and failures.
- Follow the [verification guide](autonomous-development-guide.md#verification-tiers) and [standing cadence](../../docs/delivery-workflow.md#fast-test-cadence); eventual release gates retain all pending browser acceptance behavior, without a pre-catalogue E2E trigger.
- Owner's words: "55 minutes is too long to wait - we want to be making progress adding more puzzle elements once we are sure the core engine changes have landed and the old code has been removed".

### Open scheduling items

All items are decided and applied below. The owner decided items (b), (c), (e), (f) and (g), and the asset-format decision in item (i), on 18 Oct 2026. The coordinator decided items (a), (d), (h) and the rest of (i) on 18 Oct 2026 under the owner's delegation that day: "make the decisions based on the intent of the game, it's a physics puzzle game — if there are missing definitions that make the game more playable then you can feel free to add them". Each delegated decision carries a one-line playability rationale. The Battery acceptance that names the Electric Motor in Story 8.1's acceptance (Motor delivery: Story 11.1) is an acceptance conflict for the owner's case-by-case review, not a scheduling item.

- **(a) RGB lasers (EL-176–178) — decided 18 Oct 2026 by coordinator under owner delegation (playability).** They fold into Epic 13 as three new stories, one element each (AGENTS.md): 13.1b Red Laser Emitter (EL-176), 13.1c Green Laser Emitter (EL-177) and 13.1d Blue Laser Emitter (EL-178). They come after Story 13.1 and before Story 13.2. Rationale: players get legible coloured beams before they need to sort colours, and every colour receiver has a direct, teachable source.
- **(b) Switch electrical mode — decided 18 Oct 2026.** Story 9.7 (Electrical NAND & NOR, formerly 9.3) wires its gates to input power switches, which needs the CAT-063 Switch `PowerIn`→`Supply` mode. The CAT-013, CAT-026, CAT-027 and CAT-039 specs cite the same need. Owner decision: a new Story 9.5b (Switch Electrical Mode) after the Hold Timer (9.5) and before the Both Gate (9.6).
- **(c) Sound meter source — decided 18 Oct 2026.** The Sound Level Meter's acceptance uses the Service Bell, which followed it. Owner decision: a separate acoustic-propagation engine story (Story 14.0) comes first, then the Service Bell (14.2), then the Sound Level Meter (14.1) using the Bell as its source, then the Speaker (14.3) and Wind chimes (14.4). Story numbers are kept; the delivery order is 14.0, 14.2, 14.1, 14.3, 14.4.
- **(d) Receivers' colour source — decided 18 Oct 2026 by coordinator under owner delegation (playability).** Story 13.2's pure-channel receivers needed coloured beams. CAT-056 names the red filter (now Story 13.4) as its pure source, and CAT-032 and CAT-012 name the green and blue filters, while the filters use the receivers as observers. That was a loop. Item (a) breaks it: Story 13.2's pure-channel receivers use the Story 13.1b–13.1d coloured lasers as their pure sources, and the filters (Story 13.4) then use the receivers as observers. Rationale: each colour receiver can be taught with a single source and a single beam.
- **(e) Story 10.1 placement — decided 18 Oct 2026.** Owner decision: both the CAT-053 Pulley wheel and the CAT-034 Lever solid fulcrum are true cylinders, so Story 10.1 stays first in Epic 10.
- **(f) Pulley and Lever loop — decided 18 Oct 2026.** Story 10.2 (Pulley) used a Lever as the rope load, but the Lever (Story 10.3) follows it. Owner decision: the Pulley acceptance uses the Counterweight body (CAT-067, shipped with Story 10.2 per CAT-067 §3) as the rope load; the Lever's rope-load integration is proven in Story 10.3.
- **(g) Sole architecture conformance — owner decisions, 18 Oct 2026.** Evidence: the [architecture conformance audit](architecture-conformance-audit-2026-10-18.md).
  1. Retire the old code. What remains must be the only avenue available: **project per puzzle element**, **command/query** and the **event disruptor** pattern are the only allowable design.
  2. There is one source of element declarations: **the element packages plus the bundle**. Each `elements/<Name>` package is the only declaration of its element. The build produces one complete bundle and the runtime loads only that. The `.tres` catalogue files and the C# `PartId` tables are deleted.
  3. Timing: Epic 9A runs right after Stories 9.4 and 9.4b, before any new element (Story 9.5 and later). The coordinator placed the latch-dependent Story 9.4c (which adds no new element) between 9.4b and 9A, so that its two suites guard Epic 9A's supply and drive refactors. *Superseded on 21 Oct 2026 by the [Delivery order](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination): only the critical path precedes Story 9.5, and Story 9.4c now runs immediately before C-Gate.*
  
  Follow-on owner decisions the same day:
  - Review-9-4 F6 (the compiled switched-supply route, which replaces the per-tick `Program.SwitchedSupplies`/`ElectricalState` walk of the authoring graph) is fixed inside Story 9.4, not in Epic 9A.
  - Story 9A.44 migrates every remaining binary16 (`Half`) lane to f32, so that `Half` appears nowhere in the tree. This resolves CAT-037 open question 2.
  - The State Latch gains an ActivationOut socket in Story 9.4 (CAT-037 open question 1).
  - Story 9.4 may reach done with the six pre-existing failing Chrome suites (cat-005, cat-015a, cat-015a-sidekick, cat-015b, cat-023b, cat-062b) carried by Story 9.4b, which must restore a 100% cumulative serial run before Story 9.4c or Epic 9A start. The independent review confirmed 9.4 did not cause or worsen them (review-9-4, rounds 1 and 2).
- **(h) Definitions decided under owner delegation, 18 Oct 2026 (coordinator, playability).**
  - **CAT-051 Q1.** The Powered Gate's return spring is its fail-closed law (Story 9.4c). Rationale: a gate that falls shut on power loss is predictable and teachable.
  - **CAT-005 Q6.** Wires are not edited during Run; supply loss is a switched contact (Story 9.4c). Rationale: Run is for watching, the bench is for building.
  - **Tennis Ball and Balloon** leave the palette until Story 12.1 ships their packages (Story 9A.14). Rationale: a part that throws when placed is unplayable.
  - **Palette categories stay** as the player sees them today (Story 9A.4). Rationale: a refactor must not reshuffle the toolbox.
  - **Synthetic startup warm-up is removed** (owner, 26 Oct 2026; Story 9A.21 / `engine-remove-core-warmup`), superseding the coordinator retention choice. Keep real preparation, solver warm-starting and later first-use/performance qualification.
  - **The e2e observation port** is typed, read-only and part of the single production build (Story 9A.24). Rationale: Chrome proof must exercise the build players get.
  - **Pressure Plate threshold.** The CAT-052 §2 parameter governs: `minimum_mass` f32 kg, range 0.1–16, default 0.5. Story 9.1's "> 0.2 kg" wording is superseded (Story 9A.20). Rationale: as shipped, one light ball is too light and a heavy ball triggers it, which is a clear lesson.
  - **Collider source (Story 9A.16).** `BundleElements` extracts each package's colliders from that package's own scene or glTF at build time, using the Story 6.5c extraction, and bakes them into the bundle. The runtime loads only the bundle. A package declares no colliders in data: its scene is the one collider source (decision 4; the declaration member was deleted on 25 November 2026). Rationale: colliders always match the art the player sees.
  - **Saves from before a format bump are rejected** with a visible message, never migrated (Stories 9A.2, 9A.15, 9A.44; AGENTS.md).
- **(i) Retro-verification repair, 18 Oct 2026.** Evidence: the [retro-verification report](retro-verification-2026-10-18.md). It found invalid Passes for 6.5b, 6.5c, 6.5d, 6.5e, 6.5f, 6.9, 6.9b, 6.10, 8.1, 9.1, 9.2, 9.2b and 9.3, and weak Passes for 6.6, 6.7, 6.8, 6.11 and 8.2.
  - **Owner decision.** The authored asset format is the element's own Godot `.tscn` scene, not `.glb`. Stories 6.5c, 6.5d and 6.5e and the autonomous-development guide DoD are amended (report #9).
  - **Coordinator decisions under the owner's playability and "be aggressive about correctness" delegation:**
    - **CAT-052 Q2.** The Pressure Plate gains an ActivationOut level output that drives Signal lamps, as the latch's does (Story 9R.5). Rationale: Story 9.1's headline AC becomes achievable, and the player sees the plate state.
    - **CAT-052 Q3.** Superseded by the coordinator override below: activation is immediate and release is held for 0.1 s (Story 9R.5).
    - **CAT-048 wrong-bore control.** The Funnel's wide inlet (Story 9R.6). Rationale: a real, visible part.
    - **Player-visible bugs are fixed before Epic 9A** in Epic 9R, using compiled Run-time tables and no new heuristics (report #1, #2, #3, #4, #6, #8, #10, #14, #15). Stories 9A.19 and 9A.20 only convert those tables to declarations.
    - **Review evidence (report #7)** is re-verified independently, with dated addenda, in Story 9R.18; history is never rewritten. *On 21 Oct 2026 Story 9R.18 was dropped: each closing story appends the addenda ([Delivery order](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination), reopened-record closure).*
    - **A real C# static gate (report #18)** is Story 9R.0, first in Epic 9R.
    - **Story 9.4b startup clock, 19 Oct 2026** (Murdoch-2 round 2):
      - D14 is amended. A derived startup offset above 0.1 ms no longer refuses the session. It becomes a typed, counted `CCGPU_CLOCK_OFFSET_WIDE` record, and the mapping applies the offset. Only a derivation with no admissible exchange is refused. Rationale: each worker's time origin has its own host-clock reference, so a host clock step could lock a player out of a valid session, and the measured offset is corrected anyway.
      - **Superseded the same day:** "derive the Browser mapping only after Godot's first frame". Amelia showed the mapping already qualifies after Godot's first frame, that Initialize, Commit and every presentation frame read it, and that Murdoch's lapse came from a main-thread stall after ready.
      - **Decision: a lapse is renewed and checked, not recalibrated.** When a requester's mapping lapses because its own event loop stalled, with no probe outstanding, consumers hold until a fresh probe renews the mapping.
        - If the fresh exchange agrees with the old mapping within the uncertainty already admitted, the lapse is a typed, counted `CCGPU_CLOCK_RENEWED_LATE` record, not a recalibration, and suites don't fail on it.
        - If the fresh exchange disagrees, it is a recalibration and suites still fail on it.
        - Rationale: browser main-thread hitches are ordinary for players; a recalibration should mean the clocks actually moved; and a randomly failing baseline hides real regressions. The driver's zero-recalibration policy stays.
        - **Amended after Murdoch-2 round 3:** a renewal concludes only on an exchange admissible under the same width bound that ordinary mapping checks use.
          - A wider exchange is inconclusive. Consumers keep holding while the renewal keeps probing, within a bounded budget.
          - An exhausted budget is a recalibration.
          - Exactly one `CCGPU_CLOCK_RENEWED_LATE` record is emitted per lapse, when the renewal concludes.
          - Rationale: a wide exchange cannot see a clock move smaller than its own width, so it must never confirm one.
        - **Amended after Murdoch-2 round 4:** a renewal may also conclude only if the old mapping's interval, widened by drift to now, still fits that width bound. A longer stall is a recalibration; with the current constants that renews stalls up to about 1.2 s. Rationale: long stalls are what players get from hidden-tab throttling or a frozen tab, and after those a recalibration is the honest, graceful outcome that keeps the record of any move.
    - **The remaining defects go to their 9A owners:**
      - #11 → 9A.16;
      - #12 → 9A.14, 9A.15 and 9A.17;
      - #13 → 9A.2, 9A.40 and 9A.41;
      - #17 → 9A.44;
      - #19 (9.2b MNA) → 9A.0.
    - **Delivery order:** 9.4b → Epic 9R → 9.4c → Epic 9A → 9.5. *Superseded on 21 Oct 2026 by the [Delivery order](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination).*
  - **Story 8.1's Motor clause, decided by the owner on 18 Oct 2026:** the clause moved to Story 11.1 as an integration check. Story 8.1 is proven with the Powered Gate and the Bumper store.
  - **9R spec readings, coordinator, 18 Oct 2026:**
    - 9R.2 tests the largest instance count Build admits for each element, excluding Tennis and Balloon (they can't be placed until Story 12.1).
    - 9R.6: "locked part" means the part being moved; snapping onto a locked fixture is allowed, which `joined_pipe` (9R.7) needs.
    - 9R.1 adds the engine work to publish counter count and clock phase in the committed read; 9R.10 reuses it.
    - 9R.4 and 9R.5 apply the 0.1 s release hold to both the plate's contact and its ActivationOut.
    - **Resize at a limit clamps, for every resizable part.** A drag past a declared minimum or maximum stops at the limit and keeps the gesture alive. No out-of-range value is ever saved. 9R.13 changes the Ramp from cancel-the-gesture to clamp, and 9R.14 proves the Wall clamp. Rationale: a handle that stops at its limit is legible; a gesture that silently cancels feels broken.
    - **9R.10 owns the Clock's pendulum swing and per-pulse flash presentation,** driven from the committed clock phase and pulse occurrences through the declared cosmetic bindings. Rationale: visible pulses are how players read a clock.
    - **9R.15 adds a player-facing trigger-threshold editor for the Impact Switch,** with typed range 0–64 m/s and default 0.8, Run/Reset and Save/Load. Rationale: a declared parameter with no editor cannot be taught.
    - **9R.7:** the pipe gains a typed `length` (1–8 m) with a resize handle that clamps per the rule above. There are no format versions (owner, 21 Oct 2026). A save that fails structural validation is rejected with a visible message, and there's no migration (AGENTS.md).
    - **9R.9:** "one more" means the detector's committed passage count goes from 1 to 2 while a first-input lamp stays at 1. The indicator re-pulse belongs to 9A.23.
  - **CAT-052 Q3, coordinator override on 18 Oct 2026:** activation is immediate on the first qualifying contact tick, and release is held for 0.1 s (12 ticks at 120 Hz) after the last qualifying contact. Rationale: a ball bouncing on the plate would otherwise flicker the lamp and chatter downstream logic; a short release hold keeps the signal legible without adding lag at activation. This replaces the "no dwell" choice above.

---

## Epic 1: Engine Core Foundation

**Goal:** Establish the WebAssembly SIMD multi-body execution core, lock-free multi-buffered `SharedArrayBuffer` pose ring (depth derived from tick rate and render delay), Box2D v3 TGS Soft solver, Dynamic AABB BVH with 15-axis SAT narrowphase, and speculative contacts anti-tunneling, delivering reliable real-time physics on the workbench.

### Story 1.1: Game-Grade Contact & Per-Substep Normalization (ENGINE-CORE-1)
*Status: Done*

As a player,  
I want balls to bounce and settle on the workbench predictably,  
So that contraptions run reliably without tick faults or simulation stalls.

**Acceptance Criteria:**
- **Given** an authored puzzle construction (`first_principles`, `delayed_signal`, or Bumper demo),
- **When** the player runs the simulation across 20 consecutive runs,
- **Then** the simulation completes without any tick faults or numerical divergence.
- **And** quaternion normalization is applied every substep via fast inverse square root.
- **And** legacy single-body shaders and puzzle-keyed guide branches are completely deleted.
- **And** exact authoring state is restored on Reset and Save/Load roundtrip.

### Story 1.2: Multi-Body Pose Ring & Dual-Sphere Simulation (ENGINE-CORE-2a1)
*Status: Done*

As a player,  
I want to place and simulate multiple basketballs simultaneously,  
So that multi-body interactions render smoothly with independent trajectories.

**Acceptance Criteria:**
- **Given** the Free Workshop with 16 body slots in the multi-buffered `SharedArrayBuffer` pose ring,
- **When** the player drops two Basketballs from different heights via real UI controls,
- **Then** both balls simulate concurrently in WASM SIMD and render via universal instanced batches.
- **And** empty construction and single-ball control baselines pass cleanly.
- **And** single-body pose slot assumptions are permanently deleted.
- **And** exact Reset and Save/Load restore identical configurations.

### Story 1.3: Box2D v3 TGS Soft Solver & FeatureId Warm-Starting (ENGINE-CORE-2a2)
*Status: Done*

As a player,  
I want resting contacts to settle smoothly without jitter or explosive bouncing,  
So that stacked or resting puzzle elements remain rock-solid under gravity.

**Acceptance Criteria:**
- **Given** balls dropped onto the static workbench or inclined surfaces,
- **When** collisions occur under the TGS Soft solver with compliance $\gamma$ and softness $\beta$,
- **Then** balls settle into quiet resting contact without bounce jitter.
- **And** normal and friction impulses warm-start using persistent 32-bit `FeatureId` caches.
- **And** restitution $e \in [0, 1]$ dissipates kinetic energy (second bounce lower than first).
- **And** obsolete `physics.wgsl` compute shaders, Baumgarte stabilization, and hard velocity clamps are deleted.

### Story 1.4: Dynamic AABB BVH & 15-Axis SAT Narrowphase (ENGINE-CORE-2a3)
*Status: Done*

As a player,  
I want balls to roll realistically down angled Ramps and bounce off resized Walls,  
So that complex contraptions can be constructed and solved.

**Acceptance Criteria:**
- **Given** an authored level containing Ramps and Walls (`first_principles`),
- **When** the simulation executes using Dynamic AABB BVH with velocity fattening and 15-axis SAT narrowphase,
- **Then** the basketball rolls smoothly along the ramp and deflects accurately off walls into the receiver.
- **And** local-axis Wall resizing and arbitrary ramp angles are supported generically.
- **And** ad-hoc box collision branches are permanently deleted.
- *Correction (23 November 2026):* this criterion also claimed that brute-force bounding loops were permanently deleted. That is false. The tree serves contact pairs only and is cleared and refilled every substep and trial replay; guides, compliant surfaces, airflow line of sight, chambers and gas mass still scan every body ([engine design audit](../../docs/planning/elements/readiness/engine-design-gap.md), section 1). `ENGINE-SPATIAL-QUERIES` owns their removal.
- **And** tests across suites 2a1, 2a2, and 2a3 pass 100% serially in Playwright.

### Story 1.5: Speculative Contacts Anti-Tunneling for High-Speed Impacts (ENGINE-CORE-2a4)
*Status: Done*

As a player,  
I want high-speed balls to never pass through thin barriers,  
So that contraption mechanics remain completely reliable regardless of velocity.

**Acceptance Criteria:**
- **Given** a Basketball launched at maximum UI speed against a Wall of any admitted thickness ($\ge 1\text{ cm}$),
- **When** the narrowphase generates speculative contact constraints with margin $d_{\text{spec}} = |\mathbf{v}_{\text{rel}} \cdot \mathbf{n}| \, h + s_{\text{slop}}$ and target velocity absorption $v_{\text{target}} = -g/h$,
- **Then** the ball rebounds cleanly and never tunnels through the wall across 20 runs in Chrome.
- **And** pure TypeScript Playwright suite `tools/e2e/engine-core-2a4.test.ts` passes 100% serially.
- **And** ad-hoc discrete tunneling clamps and legacy ray-cast CCD fallbacks are completely deleted.

---

## Epic 2: Compliant Solver Streamlining

**Goal:** Permanently remove host shadow verification, motion-piece error lanes, clearance/closing certificates, directed rounding math, interval-arithmetic sweeps, and legacy guide predictors, completing the transition to a pure TGS Soft solver.

### Story 2.1: Host Validation & Error Lane Removal (ENGINE-CORE-2b1)
*Status: Done*

As an engine developer,  
I want simulation poses published directly through the lock-free `SharedArrayBuffer` ring without host shadow validation,  
So that execution latency and memory bandwidth overhead are minimized.

**Acceptance Criteria:**
- **Given** the dedicated WASM simulation worker publishing poses to the multi-buffered ring,
- **When** the browser client consumes poses for rendering,
- **Then** poses render directly without host per-tick byte validation or motion-piece error lanes.
- **And** Chrome ball drop and ramp solve pass cleanly with zero shadow verification code in `BrowserWorkshopClient`.
- **And** exact Reset and Save/Load persistence remain 100% verified.

### Story 2.2: Certificate & Directed Rounding Removal (ENGINE-CORE-2b2)
*Status: Done*

As an engine developer,  
I want constraint limits enforced exclusively via pure TGS Soft compliance,  
So that complex mathematical certificate verifications and directed rounding checks are removed.

**Acceptance Criteria:**
- **Given** resting contact and multi-body ramp rolling in Chrome,
- **When** constraints are solved solely through compliant TGS Soft formulations,
- **Then** simulation remains completely stable under gravity.
- **And** `ClearanceBoundarySweep`, `LipschitzBoundarySweep`, `JointBoundarySweep`, and directed rounding routines are deleted.
- **And** a grep across the active physics solver confirms zero certificate references.

### Story 2.3: Analytic CCD & Interval Library Removal (ENGINE-CORE-2b3)
*Status: Done*

As an engine developer,  
I want continuous collision handled entirely by speculative contacts,  
So that analytic polynomial sweep solvers and interval arithmetic libraries are eradicated.

**Acceptance Criteria:**
- **Given** a 64 m/s high-speed ball impact against thin walls,
- **When** collisions resolve via speculative contact margins,
- **Then** zero tunneling occurs across 20 Chrome test runs.
- **And** analytic swept roots, interval-arithmetic range libraries, and remaining `physics.wgsl` references are deleted.
- **And** reviewer grep confirms zero interval root-finding routines in active code.

### Story 2.4: Guide Horizon & Departure Ownership Removal (ENGINE-CORE-2b4)
*Status: Done*

As an engine developer,  
I want player assistance forces modeled as declarative spatial force regions,  
So that complex guide horizon predictors and departure ownership code are eliminated.

**Acceptance Criteria:**
- **Given** the `first_principles` level with receiver assistance knots,
- **When** assistance forces are evaluated as declared spatial acceleration fields,
- **Then** the ball guides smoothly into the receiver without moving physical collider walls.
- **And** `GuideHorizon`, departure ownership routines, and `PlanarGuideBoundaryPath.cs` are deleted.
- **And** reviewer grep confirms zero guide predictor code in the solver.

---

## Epic 3: Decoupled Sensor Evaluation

**Goal:** Refactor all sensors and triggers (aperture crossing, residence, impact switches) to evaluate at substep endpoints with tick-accumulated dwell, replacing continuous root-finding with clean discrete events.

### Story 3.1: Endpoint-Sampled Sensors & Dwell Tick Counter (ENGINE-CORE-2c)
*Status: Done*

As a player,  
I want the Receiver to reliably capture balls meeting the speed and dwell criteria,  
So that goal feedback triggers deterministically without continuous curve root-finding.

**Acceptance Criteria:**
- **Given** the Receiver placed in the Workshop,
- **When** the Basketball enters the capture region and remains for the authored dwell duration at $\le 1.5\text{ m/s}$,
- **Then** the Receiver emits a typed captured event and activates its visual halo.
- **And** a fast through-pass ($> 1.5\text{ m/s}$) or glancing boundary contact does NOT trigger capture.
- **And** sensor continuous root-finding, `CurveCuts`, and sub-phase residence intervals are deleted.
- **And** exact Reset clears capture state and timers cleanly.
- **And** `tools/e2e/engine-core-2c.test.ts` passes 100% serially in Chrome alongside all cumulative regression suites.

---

## Epic 4: Dedicated WebAssembly Animation Worker

**Goal:** Establish an independent 60 Hz WebAssembly animation worker consuming committed physics state over a shared event channel, driving cosmetic secondary animations without main-thread or simulation bottlenecks.

### Story 4.1: Dedicated 60 Hz Animation Worker Pipeline & Core Feedback (ANIM-1a)
*Status: Done*

As a player,  
I want glowing lamps and pulsing receiver halos to animate smoothly at 60 Hz,  
So that presentation feedback feels fluid and independent of physics sub-stepping.

**Acceptance Criteria:**
- **Given** active contraptions with a lit Signal lamp and captured Receiver,
- **When** the simulation executes with physics paused or running,
- **Then** cosmetic halo pulses and lamp glow animate continuously via the animation worker at 60 Hz.
- **And** hardcoded main-thread timer loops for halo and lamp are permanently deleted.
- **And** rendering never synchronously blocks waiting for the animation worker.
- **And** `tools/e2e/anim-1a.test.ts` passes 100% in Chrome serially.

### Story 4.2: Mechanical Cosmetic Bindings & Procedural Curves (ANIM-1b)
*Status: Backlog*

As a player,  
I want Bumpers, switches, and delays to visually compress and fill,  
So that physical activation gives immediate, satisfying visual feedback.

**Acceptance Criteria:**
- **Given** mechanical elements receiving impact or activation,
- **When** cosmetic procedural curves (squash/stretch for Bumper, depression for Switch, progress fill for Delay) evaluate,
- **Then** visual components animate smoothly according to declared curve bindings.
- **And** element-specific cosmetic evaluators on the main thread are deleted.
- **And** `tools/e2e/anim-1b.test.ts` passes 100% in Chrome serially.

### Story 4.3: Legacy Presentation Code Retirement (ANIM-1c)
*Status: Backlog*

As an engine developer,  
I want all legacy presentation loops and ad-hoc update hooks deleted,  
So that rendering consumes only the unified pose ring and animation worker channels.

**Acceptance Criteria:**
- **Given** all currently playable contraption parts,
- **When** the full presentation pipeline runs,
- **Then** all visual elements animate correctly through declared bindings.
- **And** retired presentation classes in `engine/presentation/*.cs` and `ui/*.cs` are permanently removed.
- **And** reviewer grep confirms zero element-keyed presentation update loops in the tree.
- **And** `tools/e2e/anim-1c.test.ts` passes 100% in Chrome serially.

---

## Epic 5: Dynamic Polyhedral Rigid Bodies & Domino Mechanics

**Goal:** Deliver full 3D dynamic polyhedral rigid body physics with 3x3 inertia tensors, 4-point area-maximizing contact manifolds, and declarative orientation sensors, unlocking domino cascade puzzles.

### Story 5.1: Dynamic Box Rigid Body & Upright Stability (CAT-023a)
*Status: Backlog*

As a player,  
I want to place Dominoes standing upright on the workbench,  
So that they stand stably under gravity and topple realistically when struck.

**Acceptance Criteria:**
- **Given** an upright Domino placed on the workbench in the Workshop,
- **When** the simulation runs,
- **Then** the domino stands stably without wobbling, drifting, or falling over under gravity.
- **And** when struck by a dropped Basketball, it topples smoothly and comes to rest flat on the workbench without jitter.
- **And** narrowphase evaluates a constant 4-point area-maximizing contact manifold with Box2D v3 TGS Soft friction.
- **And** ad-hoc box inertia approximations are deleted.
- **And** `tools/e2e/cat-023a.test.ts` passes 100% in Chrome serially.

### Story 5.2: Domino Cascade Mechanics & Orientation-Threshold Sensor (CAT-023b)
*Status: Backlog*

As a player,  
I want dominoes to knock each other down in a cascade and trigger connected devices,  
So that chain-reaction contraptions can solve the `domino_effect` level.

**Acceptance Criteria:**
- **Given** four dominoes arranged in sequence connected to a Signal lamp via an orientation threshold sensor,
- **When** the first domino is struck,
- **Then** all dominoes topple in sequence and the lamp lights once.
- **And** an incomplete chain of three dominoes fails to light the lamp (negative control).
- **And** a small 10° nudge does NOT trigger the orientation sensor.
- **And** `parts/DominoPart.cs` ad-hoc physics logic is deleted in favor of generic rigid body and orientation sensor declarations.
- **And** `tools/e2e/cat-023b.test.ts` passes 100% in Chrome serially.

---

## Epic 6: Core Interactive Catalogue Elements

**Goal:** Implement the foundational mechanical and aperture interactive elements by pure data declaration and integrate them into solvable campaign levels.

### Story 6.1: Bowling Ball Dynamic Sphere by Material Declaration (CAT-014)
*Status: Backlog*

As a player,  
I want to use a heavy Bowling Ball to knock down heavy obstacles,  
So that different mass and density properties enable varied physical puzzles.

**Acceptance Criteria:**
- **Given** a Bowling Ball placed alongside a Basketball,
- **When** both strike identical domino targets,
- **Then** the heavier Bowling Ball topples targets that resist the Basketball.
- **And** Bowling Ball is implemented purely via material declaration data without bespoke solver branches.
- **And** `tools/e2e/cat-014.test.ts` passes 100% in Chrome serially.

### Story 6.2: Bumper Radial Contact Impulse & Finite Work Store (CAT-015a)
*Status: Backlog*

As a player,  
I want Bumpers to impart an energetic bounce only when powered or charged,  
So that kinetic boosting respects thermodynamic energy limits.

**Acceptance Criteria:**
- **Given** a Bumper configured with a finite work store,
- **When** a ball contacts the bumper,
- **Then** a radial impulse is applied and the work store debited accordingly.
- **And** an uncharged/unpaid bumper imparts zero boost (regular bounce only).
- **And** `engine/BumperWorkResource.cs` ad-hoc work logic is deleted in favor of generic finite work store declarations.
- **And** `tools/e2e/cat-015a.test.ts` passes 100% in Chrome serially.

### Story 6.3: Bumper Multi-Angle Contacts & Advanced Bumper Puzzles (CAT-015b)
*Status: Backlog*

As a player,  
I want Bumpers to handle glancing and multi-angle collisions accurately,  
So that levels `bumper_depth` and `wall_and_bumper` can be solved.

**Acceptance Criteria:**
- **Given** levels `bumper_depth` and `wall_and_bumper`,
- **When** played in Chrome through real UI controls,
- **Then** both levels are solvable with valid route trajectories.
- **And** glancing impacts do NOT falsely trigger radial boost.
- **And** `tools/e2e/cat-015b.test.ts` passes 100% in Chrome serially.

### Story 6.4: Prismatic Slider & Unified TGS Soft Spring Constraint (CAT-062a)
*Status: Backlog*

As a player,  
I want an uncharged Springboard to compress and rebound elastically,  
So that spring dynamics follow real physical spring-damper laws.

**Acceptance Criteria:**
- **Given** a Springboard placed under a dropped ball,
- **When** impact occurs,
- **Then** the 1D prismatic slider compresses within limits ($-0.25\text{ m}$ to $0\text{ m}$) and rebounds elastically.
- **And** hardcoded spring launch shims are deleted in favor of unified compliant soft constraints.
- **And** `tools/e2e/cat-062.test.ts` passes 100% in Chrome serially. *(Suite name corrected from `cat-062a` by Story 9A.0, 21 Oct 2026.)*

### Story 6.5: Preload Energy Store & Springboard Launch (CAT-062b)
*Status: Done — published commit 9107148; independent terminal scoped Pass*

As a player,  
I want to preload a Springboard to launch objects across the workbench,  
So that contraptions can solve `spring_forward`.

**Acceptance Criteria:**
- **Given** the `spring_forward` level,
- **When** the preloaded springboard releases on Run start,
- **Then** the ball is launched across the workbench into the target receiver.
- **And** an uncharged springboard fails to launch the ball.
- **And** no legacy latch implementation remains: `engine/LatchedSpringStore.cs` is absent from sole baseline `a6c914e` and the current tree, so there is no such file to delete. Deliver the intended declarative finite preload energy store without restoring or inventing a legacy latch.
- **And** `tools/e2e/cat-062b.test.ts` passes 100% in Chrome serially.

### Story 6.5b: Dynamic Puzzle Part Discovery & Extensible PartId Architecture
*Status: In-Progress*

As an engine architect and player,  
I want puzzle parts to be discovered dynamically from catalogue data and identified by extensible `PartId`s,  
So that new parts can be authored as declarative resources without modifying closed enums or engine switch statements.

**Acceptance Criteria:**
- **Given** `res://parts/catalog/`,
- **When** `PartRegistry.Discover()` executes at startup via `DirAccess.GetFilesAt`,
- **Then** all valid catalogue `.tres` files are discovered and registered dynamically without hardcoded file path lists.
- **And** parts are identified by strongly typed extensible `PartId` record structs wrapping canonical strings.
- **And** closed enum `WorkshopPartKind` is eliminated from active part registration, creation, inventory, and machine parts.
- **And** `PartRegistry.Create(PartId)` instantiates parts from `definition.Scene` and calls `Configure(definition)` without switch statements.
- **And** Free Workshop mode palette populates dynamically from discovered `PartRegistry.Definitions` with unlimited allowances.
- **And** all 796 native solution tests pass with zero warnings (`dotnet test CuriousContraptions.slnx`).
- **And** existing cumulative Chrome Playwright E2E suites pass 100% serially.

### Story 6.5c: glTF Asset-Driven Geometry & Collider Extraction (ENGINE-CORE-2d)
*Status: Ready-for-Dev*

*Depends on: Story 6.5b (extensible PartId discovery).*

*Amended 18 Oct 2026 (owner decision; Open scheduling item i): the authored asset format is the element's own Godot `.tscn` scene, not `.glb`. Each `.glb` / glTF reference in this story's acceptance below reads as "the element's `.tscn` scene" (with `col_*` proxy nodes and `Loc_Port_<Id>` locators). The extraction logic stays. Under Story 9A.16 it runs at build time, and its output is baked into the bundle. Reopened by the retro-verification; closes via Stories 9R.18, 9A.16 and 9A.17.*

As an engine developer,  
I want `WorkshopPhysicsCompiler` to extract collision shapes and socket anchors directly from glTF 2.0 (`.glb`) scene hierarchies,  
So that physical geometry and connection ports are defined purely by 3D artist assets without hardcoded C# tables.

**Acceptance Criteria:**
- **Given** an instantiated `.glb` scene hierarchy with collision proxy nodes of the admitted kinds (`col_box`, `col_sphere`, `col_cylinder` and the hollow tube, frustum and torus proxies of [hollow geometry](../../docs/hollow-geometry-design.md#declaration-data)) or `KHR_physics_rigid_bodies`; capsule and hull proxies (`col_capsule`, `col_hull`) are refused with a typed reason, as `docs/gpu-f32-physics.md:59` states, until a story admits them (a convex extruded profile is `ENGINE-PROFILE-COLLIDER`'s; index C29),
- **When** `WorkshopPhysicsCompiler` compiles the physics graph at Run start,
- **Then** it traverses the scene graph and extracts `ColliderDeclaration` records generically based solely on node transforms and geometry without element-specific branching.
- **And** 3D socket anchor points for electrical, activation, and mechanical connections are resolved dynamically from named empty locator nodes (`Loc_Port_<Id>`) matching declared `PortDefinition.Id` values.
- **And** secondary child branches in the `.glb` share articulated transforms automatically.
- **And** placed elements with asset-driven colliders interact, collide, and rest stably in Chrome without hardcoded C# collider tables; zero element identifiers in compiler dispatch.
- **And** the visual mesh and collider mesh are strictly separate entities: visual meshes are dedicated to presentation rendering, while collision proxies are separate nodes whose visual rendering is suppressed; physics colliders are never extracted directly from visual meshes.
- **And** `anvil check --changed` passes with 0 warnings; exact authoring Reset and Save/Load persistence roundtrip in Chrome.

### Story 6.5d: Existing Implementation Conversion — Core Ball & Obstacle Primitives (CAT-001, CAT-014, CAT-064, CAT-003, CAT-066, CAT-054, CAT-004)
*Status: Backlog*

*Depends on: Stories 6.5b and 6.5c.*

*Amended 18 Oct 2026 (owner decision; Open scheduling item i): the authored asset format is the element's own Godot `.tscn` scene, not `.glb`. The "self-contained `.glb` binary asset in `parts/models/`" clause below reads as "the element's own `.tscn` scene, with PBR materials and co-located `col_*` proxy nodes". The scene moves into its `elements/<Name>` package under Epic 9A. The AC-named suites that never existed are added by Stories 9R.11–9R.14 (`cat-001`, `cat-004`, `cat-054`, `cat-066`). `cat-064` and `cat-003` move to Story 12.1, because Story 9A.14 removes Tennis and Balloon from the palette. Reopened by the retro-verification; closes via Stories 9R.11–9R.14, 9R.18 and 9A.16.*

As a player and developer,  
I want the existing balls, walls, ramps, and receiver converted from procedural C# code to declarative glTF 2.0 (`.glb`) assets,  
So that core simulation primitives follow the data-driven architecture with zero backwards compatibility shims.

**Acceptance Criteria:**
- **Given** existing ball and obstacle elements (Basketball CAT-001, Bowling Ball CAT-014, Tennis Ball CAT-064, Floating Balloon CAT-003, Wall CAT-066, Ramp CAT-054, Receiver CAT-004),
- **When** they are refactored to the new design,
- **Then** each element's visual mesh and collision geometry are authored in a self-contained `.glb` binary asset in `parts/models/` with standard PBR materials and co-located `col_*` proxy nodes.
- **And** each element is defined by a declarative `.tres` resource in `parts/catalog/` and instantiated via generic `MachinePart`.
- **And** bespoke per-part C# classes (`BallPart.cs`, `WallPart.cs`, `RampPart.cs`, `BasketPart.cs`) and hardcoded dimension resources (`WallDimensionsResource.cs`, `RampDimensionsResource.cs`, `ReceiverGeometry.cs`) are deleted.
- **And** strictly zero backwards-compatibility shims, dual loaders, or fallback branches are introduced.
- **And** all existing ball, ramp, wall, and receiver levels (`first_principles`, `two_ball_lead`, etc.) simulate identically and solve in Chrome.
- **And** dedicated E2E suites (`cat-001`, `cat-014`, `cat-064`, `cat-003`, `cat-066`, `cat-054`, `cat-004`) pass 100% in Chrome serially.

### Story 6.5e: Existing Implementation Conversion — Interactive Mechanisms & Dynamic Fixtures (CAT-015, CAT-023, CAT-062, CAT-063, CAT-035, CAT-022, CAT-005)
*Status: Backlog*

*Depends on: Story 6.5d.*

*Amended 18 Oct 2026 (owner decision; Open scheduling item i): the authored asset format is the element's own Godot `.tscn` scene, not `.glb`. The `.glb` clauses below read as "the element's own `.tscn` scene, with `col_*` proxies, `Loc_Port_<Id>` locators named exactly by the typed socket (Story 9A.17), and moving sub-assemblies as child branches". The AC-named suites that never existed are added by Stories 9R.15–9R.17 (`cat-063`, `cat-035`, `cat-022`), and `spring_forward` gains its Chrome proof in Story 9R.8. Reopened by the retro-verification; closes via Stories 9R.8, 9R.15–9R.17, 9R.18, 9A.16 and 9A.17.*

As a player and developer,  
I want all existing interactive mechanisms, sensors, and timers converted to declarative glTF 2.0 (`.glb`) assets,  
So that all gameplay fixtures use data-driven 3D assets, port locators, and generic `MachinePart` entities with zero backwards compatibility.

**Acceptance Criteria:**
- **Given** existing interactive fixtures (Pinball Bumper CAT-015, Domino CAT-023, Springboard CAT-062, Impact Switch CAT-063, Signal Lamp CAT-035, Delay Box CAT-022, Battery CAT-005),
- **When** they are refactored to the new design,
- **Then** each fixture's visual meshes, collision proxies (`col_*`), and socket locators (`Loc_Port_*`) are authored in a self-contained `.glb` asset in `parts/models/`.
- **And** moving sub-assemblies (e.g. Springboard moving plate, Switch button) are authored as child branches in the `.glb` scene.
- **And** each fixture is defined by a `.tres` resource in `parts/catalog/` and instantiated via generic `MachinePart`.
- **And** bespoke per-part C# classes (`BumperPart.cs`, `DominoPart.cs`, `SpringboardPart.cs`, `SwitchPart.cs`, `LampPart.cs`, `DelayPart.cs`, `BatteryPart.cs`) and dimension/resource files (`SpringboardResource.cs`, `ContactWorkResource.cs`, `DelayDurationResource.cs`, `ElectricalSourceResource.cs`) are deleted.
- **And** strictly zero backwards-compatibility shims, adapters, or fallback loaders are introduced.
- **And** all existing puzzle levels (`spring_forward`, `domino_effect`, `delayed_signal`, etc.) simulate and solve identically in Chrome.
- **And** dedicated E2E suites (`cat-015`, `cat-023`, `cat-062`, `cat-063`, `cat-035`, `cat-022`, `cat-005`) pass 100% in Chrome serially.

### Story 6.5f: Zero-Remnant Procedural Geometry, Per-Part Classes, and WorkshopPartKind Purge
*Status: Backlog*

*Depends on: Stories 6.5d and 6.5e.*

As an engine maintainer,  
I want all procedural mesh generation in `PartArt.cs`, closed enums in `WorkshopPartKind`, and dead per-part classes purged,  
So that Curious Contraptions contains strictly zero procedural mesh building routines or legacy part enums in the active tree.

**Acceptance Criteria:**
- **Given** the completed conversion of all 14 existing elements to declarative `.glb` + `PartId` assets,
- **When** `Story 6.5f` is applied,
- **Then** `engine/PartArt.cs` and `engine/gpu/WorkshopPartKind.cs` are permanently deleted from the codebase.
- **And** all remaining switch statements or branches based on part kind in `ui/`, `engine/`, `parts/`, and tests are eradicated.
- **And** codebase grep confirms 0 occurrences of `PartArt` or `WorkshopPartKind` across all active C# source files.
- **And** `anvil check --changed` passes with 0 warnings.
- **And** `dotnet test CuriousContraptions.slnx` passes 100% (all unit tests).
- **And** all cumulative Playwright E2E suites execute serially in Chrome and pass 100%.
- **And** strictly zero backwards compatibility shims, adapters, or fallback wrappers remain.

### Story 6.6: Straight Pipe Compound Cylindrical Collider (CAT-048a)
*Status: Backlog*

*Depends on: Story 6.5f (zero-remnant purge of PartArt and legacy part scripts; CAT-048 is authored purely via declarative .glb asset and .tres catalog definition without per-part C# code or enum modifications).*

As a player,  
I want balls to roll smoothly through straight pipes without snagging,  
So that enclosed ball transport operates reliably.

**Acceptance Criteria:**
- **Given** a straight pipe collider in the Workshop,
- **When** an admitted ball rolls into the pipe mouth,
- **Then** the ball travels through and exits without snagging or falling through walls.
- **And** an oversized ball stops at the pipe entrance without clipping.
- **And** `tools/e2e/cat-048a.test.ts` passes 100% in Chrome serially.

### Story 6.7: Hollow Signed-Distance Torus Rim Cap Pipe Collider (CAT-048b)
*Status: Backlog*

*Depends on: Story 6.6 (straight pipe collider). Moved from Story 6.8 (owner re-ordering, 9 Oct 2026), because the Ball Detector now follows the pipe stories.*

As a player,  
I want curved and complex pipe paths to guide balls reliably,  
So that level `clear_pipe` can be solved.

**Acceptance Criteria:**
- **Given** level `clear_pipe` played in Chrome,
- **When** the ball enters the mouth at high speed and edge angles,
- **Then** the ball negotiates the pipe geometry and reaches the receiver.
- **And** all legacy annular-specific kernel code, `AnnularFeature` enums, and `reference/pipe/` are deleted.
  - Historical reference and annular cleanup clauses discharged by Stories 7.1 and 7.4 (10 Oct 2026); the fresh implementation must preserve the generic shared-engine/no-annular-identifier rule.
- **And** `tools/e2e/cat-048b.test.ts` passes 100% in Chrome serially.

### Story 6.8: Generic Aperture Sensor with Directional Rearm (CAT-002)
*Status: Backlog*

*Depends on: Stories 6.6 and 6.7 (the detector body is a hollow collar that reuses the pipe's annular collider, rim caps and tube mouths; CAT-002 §3). Moved from Story 6.6 (owner re-ordering, 9 Oct 2026).*

As a player,  
I want a Ball Detector to emit a signal when a ball passes through its aperture,  
So that crossing events can trigger downstream contraption components.

**Acceptance Criteria:**
- **Given** a Ball Detector wired to a Signal lamp,
- **When** a ball passes forward through the aperture,
- **Then** the lamp illuminates once.
- **And** reverse or glancing outside crossings do NOT trigger the lamp.
- **And** a second forward crossing after complete clearance triggers again.
- **And** `tools/e2e/cat-002.test.ts` passes 100% in Chrome serially.

### Story 6.9: 45-Degree Pipe Bend Hollow Torus Segment (CAT-049)
*Status: Backlog*

As a player,  
I want a 45-degree hollow pipe bend that joins straight pipes and other bends,  
So that level `gentle_bend` can be solved by turning a falling ball through a gentle curve.

**Acceptance Criteria:**
- **Given** level `gentle_bend` played in Chrome with the 45-degree bend placed through real UI controls,
- **When** the ball enters the upper mouth and travels the 1.3-unit bore around the fixed-radius curve,
- **Then** it exits the lower mouth without added speed and reaches the receiver; straight-to-bend and bend-to-bend rotated passages succeed through typed mouth snapping.
- **And** rim, oversize, sidewall and missed-connection controls deliver nothing, and an actual valid Precise alternative route is proven.
- **And** the authored assistance controls depend on physical placement nudging, owned by Story 15.0's bounded native stage after all named intrinsic implementations and before catalogue-native-closure. They are proven there, and CAT-049 stays open until that native proof passes; actual UI proof remains post-catalogue.
- **And** the bend keeps its own icon, rails and collars; no 90-degree proof substitutes for it.
- **And** the element is built from its declaration spec `docs/planning/elements/CAT-049-pipe_bend_45.md`, with exact Reset and Save/Load.
- **And** `tools/e2e/cat-049.test.ts` passes 100% in Chrome serially.

### Story 6.10: 90-Degree Pipe Bend and Joined Pipe Route (CAT-050)
*Status: Backlog*

As a player,  
I want a 90-degree hollow pipe bend that joins a straight pipe into one continuous route,  
So that levels `quarter_bend` and `joined_pipe` can be solved.

**Acceptance Criteria:**
- **Given** levels `quarter_bend` and `joined_pipe` played in Chrome through real UI controls,
- **When** the ball falls into the bend's upper mouth, or through a resized straight pipe snapped to the fixed bend,
- **Then** it follows the continuous turned passage and reaches the receiver.
- **And** straight/bend joins succeed, and rotated, oversize, rim and missing-connection controls deliver nothing.
- **And** the declared depth-error result (Forgiving and Balanced succeed where Precise fails) depends on physical placement nudging, owned by Story 15.0's bounded native stage after all named intrinsic implementations and before catalogue-native-closure. It is proven there, and CAT-050 stays open until that native proof passes; actual UI proof remains post-catalogue.
- **And** the bend shares the common bore and typed mouth geometry with matching collars, rails and icon; no 45-degree proof stands in for it.
- **And** the element is built from its declaration spec `docs/planning/elements/CAT-050-pipe_bend_90.md`, with exact Reset and Save/Load.
- **And** `tools/e2e/cat-050.test.ts` passes 100% in Chrome serially.

### Story 6.11: Funnel Hollow Frustum Inlet to the Standard Bore (CAT-030)
*Status: Backlog*

As a player,  
I want a wide clear funnel that narrows into the standard tube bore,  
So that loosely aimed balls are gathered into a pipe route by gravity and contact alone.

**Acceptance Criteria:**
- **Given** a funnel placed through real UI controls with its narrow outlet snapped to a straight pipe or bend,
- **When** a ball drops into the 2.6-unit mouth, including off-centre, fast and rotated entries,
- **Then** the physically hollow frustum walls guide it to the 1.3-unit outlet and on into the joined tube, with no attraction or scripted transport.
- **And** inlet, wall, outlet and rim contacts, oversize payloads and straight/bend joins are qualified, an outside drop is blocked by the solid shell, and mesh and contact geometry match.
- **And** two integration proofs are owed, and CAT-030 stays open until they pass: the solid shell blocks air at Story 12.2 (Electric Fan), and the clear wall transmits light except at the opaque collar, proved by [ENGINE-OPTICAL](#story-engine-optical-beam-emission-transport-surface-interaction-and-reception), which lists and tests every delivered package that declares Transmit.
- **And** the element is built from its declaration spec `docs/planning/elements/CAT-030-funnel.md`, with exact Reset and Save/Load.
- **And** `tools/e2e/cat-030.test.ts` passes 100% in Chrome serially.

---

## Epic 7: Legacy Code Purge & Zero-Remnant Clean Architecture

**Goal:** Eradicate all dead reference tarballs, ad-hoc diagnostic probe tools, uncompiled test suites, and superseded CPU physics implementations across four disciplined deletion stages (LEGACY-0a through 0d).

**Entry gate (Stories 7.1–7.4):** no file is deleted until [legacy-disposition.md](../../docs/planning/elements/legacy-disposition.md) has a row for it. The ledger is written by Story 7.0 ([spec](../implementation-artifacts/spec-7-0-element-implementation-readiness.md)).

### Story 7.1: Reference & Historical Archive Purge (LEGACY-0a)
*Status: Done — local commit aaac712; independent committed-tree Pass*

As an engine maintainer,  
I want obsolete tarballs and benchmark dumps in `reference/` purged,  
So that repository bloat is eliminated and active docs are clearly isolated.

**Acceptance Criteria:**
- **Given** the `reference/` directory containing dead tarballs and old candidate archives,
- **When** `LEGACY-0a` is applied,
- **Then** ~4,000 obsolete files are deleted.
- **And** `reference/` retains only active Markdown documentation.
- **And** the active solution compiles and passes all unit and Playwright tests cleanly.

### Story 7.2: Diagnostics & Legacy Probe Tools Purge (LEGACY-0b)
*Status: Done — local commit 90db56b; independent committed-tree Pass*

As an engine maintainer,  
I want obsolete probe tools and ad-hoc harnesses in `tools/` purged,  
So that the codebase contains only active build, lint, and test tools.

**Acceptance Criteria:**
- **Given** `tools/p0-*`, `tools/P0-007-*`, and dead diagnostic folders,
- **When** `LEGACY-0b` is applied,
- **Then** ~2,000 obsolete files are purged.
- **And** remaining tools compile with zero warnings under Anvil.

### Story 7.3: Uncompiled Legacy Test Purge (LEGACY-0c)
*Status: Review — 417 obsolete files removed; all active compile inputs preserved; 643 solution tests passed; scoped commit verification pending*

As an engine maintainer,  
I want uncompiled, commented-out, or obsolete test files in `CuriousContraptions.tests/` purged,  
So that 100% of test files in the project are actively compiled and executed.

**Acceptance Criteria:**
- **Given** dead test files and obsolete native fixtures,
- **When** `LEGACY-0c` is applied,
- **Then** all obsolete test files are deleted.
- **And** `dotnet test CuriousContraptions.slnx` runs with 100% pass rate (0 failures).

### Story 7.4: Legacy CPU Physics, Procedural PartArt & Unshipped Engine Purge (LEGACY-0d)
*Status: Review — all 655 legacy paths removed; current source/resource classification and applicable 45-case Chrome proof complete; procedural PartArt and hardcoded collider tables scheduled for permanent deletion; final scoped commit review pending*

As an engine maintainer,  
I want all superseded CPU physics files in `engine/physics/`, procedural mesh routines in `engine/PartArt.cs`, and dead classes in `parts/` deleted,  
So that Curious Contraptions contains zero legacy physics code or procedural geometry generation in the active tree.

**Acceptance Criteria:**
- **Given** all remaining legacy physics files in `engine/physics/` and `engine/presentation/`,
- **When** `LEGACY-0d` is applied,
- **Then** all superseded physics classes are permanently purged.
- **And** procedural mesh generation in `engine/PartArt.cs`, hardcoded C# collider tables, and dead per-part classes in `parts/` are permanently deleted.
- **And** all elements are defined exclusively via declarative `.glb` assets and `.tres` catalog resources.
- **And** `git ls-files` contains only compiled, shipped, active content, docs, and CI files.
- **And** P0-030 and P0-031 cleanup audits are closed with zero warnings.
- **And** cumulative Playwright E2E suites pass 100% serially in Chrome.
- **And** strictly zero backwards-compatibility shims, legacy flags, or dual-path adapters remain.

---

## Epic 8: Supplied Electrical Power

**Goal:** Implement supplied direct-current electrical networks and battery power sources as declarative network graphs with zero continuous circuit simulation overhead. The Powered Gate is the first supplied consumer (owner decision, 9 Oct 2026).

### Story 8.1: Battery DC Power Source & Network Graph (CAT-005)
*Status: Backlog*

*Depends on: Epic 7 (legacy purge). Moved from Story 9.1 (owner decision, 9 Oct 2026: electrical first, before the activation stories). Its first electrical consumer is the Powered Gate (Story 8.2); the Electric Motor named in its acceptance arrives in Story 11.1 (an acceptance conflict for case-by-case owner review). Its supply-loss control (CAT-005 N19, which uses the Latch supply-switching fixture) is a deferred integration, owed once the Latch (Story 9.4) lands; it is scheduled as Story 9.4c (coordinator, 18 Oct 2026).*

As a player,  
I want Batteries to supply power to connected electrical elements,  
So that powered mechanisms like motors and lamps can operate.

**Acceptance Criteria:**

*Owner decision, 18 Oct 2026: the Battery-drives-Motor clause moved to Story 11.1 as an integration check. Story 8.1 is proven with its delivered consumers, the Powered Gate and the Bumper store.*

- **Given** a Battery connected to a supplied consumer (a Powered Gate, or a Bumper work store) via an electrical cable,
- **When** the simulation runs,
- **Then** the consumer receives continuous 12V supply and actuates: the gate opens, or the store is credited.
- **And** losing supply through a switched contact, or a wire edit between Runs, stops supplying the consumer immediately.
- **And** `tools/e2e/cat-005.test.ts` passes 100% in Chrome serially.

### Story 8.2: Powered Gate Passageway Barrier (CAT-051)
*Status: Backlog*

*Depends on: Stories 6.4 (prismatic slider), 6.6–6.7 (pipe mouths and routes) and 8.1 (supply). Moved from Story 11.4 (owner decision, 9 Oct 2026: first electrical consumer). It now precedes the Linear Pusher (Story 11.3), so it first builds the bounded slider drive that the pusher reuses. Its "supply interruption mid-stroke" control (CAT-051) is a deferred integration, owed once the Latch fixture (Story 9.4) lands; it is scheduled as Story 9.4c (coordinator, 18 Oct 2026).*

As a player,  
I want a Powered Gate to open when triggered,  
So that balls previously blocked can pass through unobstructed.

**Acceptance Criteria:**
- **Given** a closed Powered Gate blocking a ball in a ramp,
- **When** an activation pulse is received,
- **Then** the gate opens fully, allowing the ball to roll past.
- **And** `tools/e2e/cat-051.test.ts` passes 100% in Chrome serially.

---

## Epic 9: Activation, Timing & Electrical Logic

**Goal:** Deliver the full suite of sensor and discrete logic elements (Pressure Plate, Pulse Counter, System Clock, State Latch, Hold Timer) evaluating cleanly at substep endpoints with exact tick accumulation, then the electrical Boolean logic gates (NAND, NOR, OR, XOR, Both) as declarative network graphs on the Epic 8 supply.

### Story 9.1: Pressure Plate Surface Contact Load Sensor (CAT-052)
*Status: Backlog*

*Depends on: Stories 8.1 (supplied contact) and 8.2 (supplied consumer). Moved from Story 8.1 (the Battery and Powered Gate now precede this epic).*

As a player,  
I want to use a Pressure Plate that activates only when a body rests upon it,  
So that weight-sensitive mechanisms and sustained gates can be triggered.

**Acceptance Criteria:**
- **Given** a Pressure Plate placed on the workbench wired to a Signal lamp,
- **When** a ball or domino rests on the plate with mass $> 0.2\text{ kg}$,
- **Then** the plate activates and the lamp lights.
- **And** when the body rolls off or is lifted, the plate immediately deactivates.
- **And** light bouncing impacts without resting weight do NOT produce sustained activation.
- **And** `tools/e2e/cat-052.test.ts` passes 100% in Chrome serially.

### Story 9.2: Pulse Counter Event Accumulator (CAT-020)
*Status: Backlog*

*Depends on: Stories 6.8 (Ball Detector, one event per crossing), 8.1 and 8.2. Moved from Story 8.2.*

As a player,  
I want a Pulse Counter to emit a signal after receiving $N$ pulses,  
So that multi-step contraption sequences can be orchestrated.

**Acceptance Criteria:**
- **Given** a Pulse Counter configured with target count $N = 3$,
- **When** three distinct balls trigger its input detector,
- **Then** the counter emits an output pulse on the third event.
- **And** two pulses do NOT emit an output (negative control).
- **And** clicking Reset clears the accumulated count back to zero.
- **And** `tools/e2e/cat-020.test.ts` passes 100% in Chrome serially.

### Story 9.2b: Modular Element Package Isolation, Pure C# WASM Solver Consolidation & Multi-Consumer Event Ringbuffer (ENGINE-CORE-2e)
*Status: Backlog*

*Depends on: Story 9.2 (Pulse Counter completion).*

As an engine architect and contraption engineer,  
I want strict compile-time project isolation between puzzle elements and the core engine, a pure C# WebAssembly simulation core that eliminates custom JavaScript in the worker, and a multi-consumer broadcast event ringbuffer,  
So that the core engine and solver remain completely data-driven with strictly zero element knowledge, zero duplicated solver code, and scalable inter-domain streaming.

**Acceptance Criteria:**
- **Given** the Abstractions package layer (`CuriousContraptions.Abstractions` general package, plus domain-specific abstractions packages `Mechanical`, `Electrical`, `Fluid`, and `Signal`),
- **When** Story 9.2b is applied,
- **Then** every puzzle element is isolated into its own dedicated project (e.g. `elements/*`), referencing ONLY the Abstractions packages and strictly zero references to the Core Engine or sibling elements.
- **And** the Core Engine (`CuriousContraptions.Engine`, `CuriousContraptions.Simulation`) contains strictly zero compile-time references, zero hardcoded `PartId` constants, and zero assets (icons, i18n localization, scenes, cosmetic scripts) for any puzzle element.
- **And** a build-time packaging pipeline aggregates element metadata, assets, icons, and i18n into a unified distribution bundle dynamically loaded by the engine at runtime.
- **And** 100% of physical simulation, numerical integration, broadphase AABB BVH, analytical collision manifolds, TGS constraint solving, electrical Modified Nodal Analysis (MNA), and discrete activation logic is authored in C# and compiled to WebAssembly (`wasm-simd128`).
- **And** `worker.js` is stripped of all physics, collision, and domain logic, reduced to an un-opinionated ~50–80 line platform shell that only boots dotnet, transfers `postMessage` envelopes, and exposes `SharedArrayBuffer` pointers.
- **And** continuous simulation data updates stream via a zero-copy lock-free pose ringbuffer, and discrete simulation occurrences stream via a lock-free multi-consumer Event Ringbuffer supporting independent read cursors and consumer-side predicate filtering.
- **And** all project boundaries, inter-thread communication, and element integrations satisfy the Balanced Coupling model (coupling.dev) with zero intrusive or functional coupling across project/worker boundaries.
- **And** `anvil check` on all modified/new files reports 0 errors and 0 warnings.
- **And** `dotnet test CuriousContraptions.slnx` passes 100% across all unit test suites.
- **And** all existing Playwright E2E suites (`cat-052`, `cat-051`, `cat-050`, `cat-049`, `cat-048b`, `cat-048a`, `cat-002`, `cat-030`, `cat-020`) pass 100% in Chrome serially.

### Story 9.3: System Clock Periodic Pulse Generator (CAT-017)
*Status: Backlog*

*Depends on: Stories 8.1 (power to run), 8.2 and 9.2 (the Counter observes repeated pulses). Moved from Story 8.3.*

As a player,  
I want a System Clock to emit pulses at regular intervals,  
So that rhythmic and timed contraption mechanisms can operate.

**Acceptance Criteria:**
- **Given** a System Clock configured for a 1.0 s period,
- **When** the simulation runs for 5.0 seconds,
- **Then** exactly 5 discrete activation pulses are emitted at 1-second intervals.
- **And** pausing simulation halts clock emission; resetting restores clock phase to 0.
- **And** `tools/e2e/cat-017.test.ts` passes 100% in Chrome serially.

### Story 9.4: State Latch Bistable Memory (CAT-037)
*Status: Done*

*Depends on: Stories 8.1 and 8.2 (supplied contact and consumer). Moved from Story 8.4.*

*Scope added 18 Oct 2026 (owner decisions; Open scheduling item g):*
- *Review-9-4 F6 is fixed here: the switched-supply route is compiled at Run into the Physics graph, which deletes the per-tick `Program.SwitchedSupplies`/`ElectricalState` walk and the `SwitchedSupplyRoutes` graph walk in `engine/gpu/WorkshopLatch.cs`.*
- *The latch gains an ActivationOut socket (CAT-037 open question 1).*

As a player,  
I want a State Latch to hold memory between Set and Reset pulses,  
So that machine state can toggle and persist across dynamic events.

**Acceptance Criteria:**
- **Given** a State Latch with separate Set and Reset inputs,
- **When** a Set pulse arrives,
- **Then** the latch output transitions to high and remains high indefinitely.
- **And** when a Reset pulse arrives, output transitions to low.
- **And** simultaneous Set and Reset pulses resolve in favor of Reset (Reset dominance).
- **And** `tools/e2e/cat-037.test.ts` passes 100% in Chrome serially.

### Story 9.4b: Cumulative Chrome Regression Repair
*Status: Done (19 Oct 2026). Murdoch-2's round-5 terminal scoped Pass on candidate 6: source `d7536a1c…`, bundle `102e9f12…` ([review](../implementation-artifacts/review-9-4b-cumulative-regression-repair.md)).*

*Depends on: Story 9.4. New story (coordinator, 18 Oct 2026): during Story 9.4 the full serial run of every `tools/e2e/*.test.ts` suite found failures that predate 9.4. Stories 6.9–9.3 ran only a subset of suites. Classification and first failing assertions are in `_bmad-output/implementation-artifacts/spec-9-4-state-latch.md` (item 3 table).*

As a player,  
I want every previously delivered element to keep working as later elements are added,  
So that the catalogue I build with stays trustworthy.

**Acceptance Criteria:**
- **Given** the CAT-015 Bumper energy ledger, **When** a later empty contact or a paid sidekick contact occurs, **Then** no energy is debited that was not earned (f32 residual leakage removed at its source, not by loosening assertions), and `cat-015a`, `cat-015a-sidekick` and `cat-015b` pass 100%.
- **And** `cat-062b`'s Precise Receiver guide expectation matches the declared Receiver support geometry and save codec: either the engine or the stale test expectation is corrected, with the source acceptance cited.
- **And** `cat-062b`'s fifth test, which fails at `tools/e2e/cat-062b.test.ts:257` (`goal_body` 1 ≠ replacement 4), is fixed at its source. For Captured goals the FlatBuffers encoder writes `Goal.Body` as the ball, not the goal body (`engine/gpu/WorkshopWireFlatBuffers.cs:640`, goal encoding). The fix makes the encoder write the declared goal body, with a codec unit test for a Captured goal, and the suite passes (review-9-4 F8).
- **And** the `cat-023b` worker transport / "clock mapping recovery exhausted" failure is root-caused and fixed so the suite passes in three consecutive serial runs. The fix also covers the other two failures review-9-4 F8 recorded: test 1's empty lamp history at `tools/e2e/cat-023b.test.ts:182` (0 samples after the lamp lit and the goal was solved), and "Pose slot must be readable".
- **And** if the `cat-023b` root cause lies in the raw `reliableStalled` `postMessage` flag path (architecture audit C2: `CuriousContraptions.web/wwwroot/workshop-client.js:78-86` → `worker.js:315` → `Program.SetReliableStall` at `CuriousContraptions.Simulation/Program.cs:495`, which stops the run at `:428-431`), it is fixed by making the stall a typed command with a sequence and a receipt. Story 9A.3 then deletes only what remains of that path ([audit](architecture-conformance-audit-2026-10-18.md), C2).
- **And** `cat-005` either runs against the production preview (port 8060) and passes, or its diagnostic-only status is made explicit in the suite and the README with the production checks it duplicates named.
- **And** the IL2125 trim warning from the `Google.FlatBuffers` package (Story 8.1) is resolved without suppression, so `dotnet build CuriousContraptions.slnx` reports 0 warnings. This warning has one home, this story; Story 9A.2 (single FlatBuffers codec) builds on the warning-free state.
- **And** fixes here keep to the current channels. The `CCGPU` console acknowledgement and the `globalThis.Workshop*` hooks that `cat-015a`, `cat-015a-sidekick`, `cat-015b`, `cat-062b`, `cat-005` and `cat-023b` read stay as they are; Story 9A.24 migrates every suite to the typed observation port.
- **And** the retained JS-worker harness `tools/workshop-rigid-body.test.mjs` (50 of 61 tests fail at module load since `worker.js` stopped exporting its imports on 17 Oct 2026; review-9-4 R2-F2, `tools/workshop-rigid-body.test.mjs:42,858`) is either repaired against the current C# WASM engine boundary or deleted together with its references in `tools/e2e/README.md:20` and `docs/simulation-presentation-bridge.md:105`, with the coverage it uniquely provided named and moved to native or Chrome tests.
- **And** every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run, recorded as the new cumulative baseline that every Epic 9A story must keep.
- **And** (coordinator, 18 Oct 2026, aggressive-correctness mandate) every other test in the repository passes:
  - All eight `tools/*.test.mjs` Node harnesses pass. On `8f682f8`, 51 of 107 tests failed, including the clock-ring, client-lifecycle, isolation and save-storage harnesses. Each failure is fixed against the shipped boundary, and harnesses that asserted deliberately deleted paths are rewritten for the new design.
  - `tools/Coverage` passes. On `8f682f8`, 3 of 68 failed on stale catalogue paths.
  - Every test project in the repository is part of `CuriousContraptions.slnx`, so `dotnet test CuriousContraptions.slnx` runs all of them.
- **And** the cat-023b clock-recovery fault is fixed structurally. Clock probes are answered by a context the solver never blocks. A recovery lapse recalibrates as counted telemetry and never terminates the session (game-grade envelope). A permanent injected-load Chrome test fails 100% before the fix and passes after it. cat-023b passes five consecutive serial runs.

### Story 9.4c: Latch-Fixture Deferred Integrations (CAT-005 N19, CAT-051 Supply Interruption Mid-Stroke)
*Status: Backlog*

*Depends on: Story 9.4 (the State Latch and its switched supply contact), Story 9.4b (the cumulative baseline), and Stories 8.1 and 8.2. The former dependency on Epic 9R recorded delivery order only (corrected 21 Oct 2026).*

*Placement (Delivery order, 21 Oct 2026): position 30, immediately before C-Gate, because the ACs of Stories 9A.19, 9A.39 and 9A.43 require its two suites.*

*New story (coordinator, 18 Oct 2026): review-9-4 D1 found both latch-dependent integrations due but unscheduled. Sources: [CAT-005](../../docs/planning/elements/CAT-005-battery.md) §4 N17 and N19 and §5 "Integrations"; [CAT-051](../../docs/planning/elements/CAT-051-powered_gate.md) §2 (return spring and drive law), §5 "Boundaries" (supply interruption mid-stroke; fact 14, closing stops at a ball without crushing it); [CAT-037](../../docs/planning/elements/CAT-037-latch.md) T21 (the supply-switching fixture recipe). The coordinator placed it before Epic 9A (18 Oct 2026). The owner's timing decision names Stories 9.4 and 9.4b; this story adds no new element, and its two suites guard Epic 9A's drive and supply refactors.*

*Definitions decided by the coordinator under owner delegation (18 Oct 2026, playability):*
- *CAT-051 open question 1. The return spring (14 N/m at rest 0, damping 4 N·s/m, acting whether or not the gate is powered) is the gate's fail-closed law. When supply drops, the drive stops and the spring closes the blade. Rationale: a gate that falls shut when power is lost is predictable and easy to teach.*
- *CAT-005 open question 6. Wires are not edited during Run. "Disconnecting the cable stops the motor" in Story 8.1 means supply loss through a switched contact (here the latch), or a wire edit between Runs. Rationale: Run is for watching the machine; building happens at the bench; supply changes are activation commands, never live topology edits (N19).*

As a player,  
I want to cut and restore a gate's power with switches during a run, even while the gate is moving,  
So that I can build timed doors that fail safe.

**Acceptance Criteria:**
- **Given** this machine, built through real UI controls with no setters or numeric placement:
  - Battery `Supply` → State Latch `PowerIn`;
  - Latch `Supply` → Powered Gate `PowerIn`;
  - Impact Switch A → Latch `SetIn`;
  - Impact Switch B → Latch `ResetIn`;
  - a ball upstream of the gate;
  
  **When** a ball strikes Switch A, **Then** the latch sets and the gate becomes supplied and opens. The supply change settles two ticks after the activation, and the committed construction shows no topology change (N19).
- **And** when a ball strikes Switch B with the gate fully open, the supply drops: the drive stops, every downstream input is cleared (N17), and the blade closes under its return spring.
- **And** for supply interruption mid-stroke: **Given** the blade strictly between closed and the 1.55 m open limit (sampled from committed reads), **When** Switch B fires, **Then**:
  - within two ticks the blade stops gaining opening and then returns to closed;
  - a ball still upstream stays upstream;
  - a ball partly through the opening stops the closing blade without being crushed (fact 14).
- **And** controls:
  - without the Switch B strike the same run completes the opening;
  - with the Battery disabled the gate never opens, even after Set;
  - the latch's memory survives a supply off/on (CAT-037 T14).
- **And** the setup uses one battery and one gate. Independence across multiple supplies is proved in Story 9A.19.
- **And** Run/Reset restores latch Off, the blade closed and the authored battery state, and Save/Load preserves all five connections with their exact socket names.
- **And** `tools/e2e/cat-005-n19.test.ts` and `tools/e2e/cat-051-interrupt.test.ts` pass 100% in Chrome serially, and every `tools/e2e/*.test.ts` suite still passes in one serial run.

### Story 9.5a: Declared Timer and Switched-Contact Capabilities; Delay Box Converted (9A.38 + 9R.17)
*Status: Backlog. Delivery position 18, the last story before 9.5 (Delivery order, 21 Oct 2026). It merges Stories 9A.38 and 9R.17, whose ACs stay in place, are marked "merged into 9.5a" and apply here in full.*

*Depends on: Stories 9A.27 (generic instance and editor), PKG-3 (the Delay Box and Impact Switch packages) and 9A.45a (ratchet). Story 9A.38's former dependency on 9A.35 recorded number order only. The Impact Switch in the Delay fixture stays a record instance until C-Switch.*

*Why it is on the critical path.* Story 9.5 is a timer element and must be declaration-only, and no 9A story declared the switched-supply route generically. Today the compiler chooses supply sources and switched contacts by record type, in `SupplyFeed` (`engine/gpu/WorkshopPhysicsCompiler.cs`, around `:295-312`): `IsBattery` is `instance is WorkshopBattery`, and the admitted predicate is `instance is WorkshopBattery or WorkshopLatch or WorkshopCounter or WorkshopPressurePlate`.

The coordinator's brief cites `:224/244/272/306`:
- `:306` is that predicate;
- `:224`, `:244` and `:272` are the Pressure Plate, Counter and Latch emission loops (bodies, colliders and contact loads), and `:258` is the Clock loop.

This story makes only the supply route declaration-driven. The record-type selection of those emission loops is deleted by C-Plate, C-Memory and C-Clock, and their inline colliders by Story 9A.16.

*Deletes:*
- *the whole of Story 9A.38's list: `WorkshopDelay` and `DelayDuration` (`engine/gpu/WorkshopDelay.cs`), `WorkshopInput.Delay`, the Delay panel in `ui/WorkshopConfiguration.cs`, the delay arm in `ActivationNetwork.Compile`, and `WorkshopDelay` in the `Validate` whitelist;*
- *the `"delay"` arm of `MachineWorld.CaptureInstance` and every remaining `is WorkshopDelay`;*
- *the record-type supply-route selection in `SupplyFeed`: `IsBattery` and the `is WorkshopBattery or WorkshopLatch or WorkshopCounter or WorkshopPressurePlate` predicate;*
- *every `Half` in the timer lane: `ActivationTime.Phase` and the timer arithmetic in `engine/gpu/ActivationTimers.cs`, and the Delay duration on the wire and in saves;*
- *the `WorkshopDelay` construction in `engine/gpu/DelayedSignal.cs`, which is edited to build the generic instance. Story 9A.26 deletes the file later;*
- *the Story 9A.45a known-violations entries owned by this story.*

As a player,  
I want the Delay Box and every power switch to work from what each element declares,  
So that the Hold Timer and every later timed or switched element are data only.

**Acceptance Criteria:**
- **Suite first:** Story 9R.17's `tools/e2e/cat-022.test.ts` lands green on the record path in this story's first commit and stays green after the conversion. Every Story 9R.17 AC applies.
- **And** every Story 9A.38 AC applies: the generic timer is compiled from declared data, the lamp lights after exactly the declared duration and not earlier, and Run/Reset and Save/Load are exact.
- **And the switched contact is declared.** The Battery, State Latch, Pulse Counter and Pressure Plate packages declare a supply-source or switched-contact capability: `PowerIn` → `Supply`, closed by the element's own committed state. The compiler builds Direct and Switched supply routes from those declarations only.
  - The golden route rows are identical to those of Stories 9.4, 9R.3, 9R.4 and 9R.5.
  - An instance with the same sockets but no declared capability never supplies (negative control).
  - Story 9.5b's switch electrical mode then only declares this capability on the Impact Switch.
- **And** the timer lane is IEEE-754 f32 end to end: authoring, wire, save, solver and read model. `rg -w Half engine/gpu/ActivationTimers.cs` returns 0 hits, and Delay durations round-trip bit-exactly. Story 9A.44's scope loses this lane.
- **And** these return 0 production hits: `rg -w "WorkshopDelay|DelayDuration"`, and `rg "is WorkshopBattery or WorkshopLatch"`. The 9A.45a ratchet passes with this story's entries removed.
- **And** `cat-022`, `anim-1b`, `anim-1c`, `cat-062`, `cat-005`, `cat-037`, `cat-051`, `cat-052`, `cat-052-lamp` and `cat-020` pass. This is a material compiler change, so the Tier 3 full run applies (End-to-end scope rule).
- **And** the review appends the dated addendum that closes Story 6.5e's 9R.17 part.

### Story 9.5: Hold Timer Pulse Stretcher (CAT-033)
*Status: Backlog*

*Depends on: Stories 8.1 and 8.2 (supplied contact and consumer), 9.5a (declared timer and switched contact) and 9A.45a (isolation ratchet). Moved from Story 8.5. The former "Epic 9A" dependency is replaced by the 21 Oct 2026 Delivery order: only the critical path precedes this story. The Hold Timer is a new package with no record type, it adds no entry to the 9A.45a list, and it deletes nothing.*

As a player,  
I want a Hold Timer to keep an output energized for a configured duration after a momentary trigger,  
So that timed doors and temporary circuits can stay open.

**Acceptance Criteria:**
- **Given** a Hold Timer configured for 2.0 seconds,
- **When** a momentary impact pulse is received,
- **Then** the output activates immediately and remains active for exactly 2.0 simulated seconds before deactivating.
- **And** re-triggering during the active window does NOT extend the timeout.
- **And** `tools/e2e/cat-033.test.ts` passes 100% in Chrome serially.

### Story 9.5b: Impact Switch Electrical Mode (CAT-063 PowerIn → Supply)
*Status: Review — native scoped proof complete, exact publication review pending.*

*Depends on: Stories 8.1 (supply and network graph), 8.2 (a supplied consumer), 9.5a (the declared switched-contact capability, which this story only declares on the switch) and C-Switch (generic switch instance). Position 21 (Delivery order, 21 Oct 2026). New story (owner decision, 18 Oct 2026; Open scheduling item b), placed before the Both Gate (9.6) so every electrical gate story can use real input switches. Source: [CAT-063](../../docs/planning/elements/CAT-063-switch.md) §4 facts 1–5 and §5.*

As a player,  
I want an Impact Switch to pass power from its PowerIn socket to its Supply socket once a qualifying impact latches it,  
So that a physical event can switch an electrical circuit.

**Current implementation closure (22 Oct 2026):** The earlier one-hop supply route could not represent the required seeded cycles.9.5b replaces it with one declared source/contact/consumer graph, preserving direct finite-work accounting and typed refusals. No element-id routing branch or physical ABI change. Native unit proof is primary under current owner cadence; the Chrome criterion below is retained for post-catalogue qualification. Motor/campaign-specific retained outcomes remain with those unavailable integrations.

**Acceptance Criteria:**
- **Given** a Battery wired to the Impact Switch `PowerIn` and the switch `Supply` wired to a Powered Gate, built through real UI controls,
- **When** a ball strikes the button at or above the trigger threshold,
- **Then** the switch latches once and the Powered Gate becomes supplied through the switch.
- **And** a missed or below-threshold ball, a missing supply, a disabled supply, or a missing wire leaves the consumer unpowered.
- **And** the switch's existing ActivationOut behaviour is unchanged, and both domains can be used on one switch.
- **And** a closed contact still needs upstream supply; a loop of switches alone never supplies.
- **And** Run/Reset restores the unpressed button, the open contact and both domains exactly; connections persist through Save/Load.
- **And** `tools/e2e/cat-063-electrical.test.ts` passes 100% in Chrome serially.

### Story 9.6: Dual-Supply Both Gate (CAT-013)
*Status: Backlog*

*Depends on: Stories 8.1 (supply), 8.2 (supplied consumer) and 9.4 (the Latch supply fixture used by its boundary and integration checks; CAT-013 §5). Moved from Story 9.2 (coordinator decision, 9 Oct 2026: the owner's literal order puts the electrical gates after the activation stories).*

As a player,  
I want a Dual-Supply Both Gate to conduct power only when both inputs are energized,  
So that safety interlocks and dual-condition activations are possible.

**Acceptance Criteria:**
- **Given** a Both Gate connected to two separate Battery circuits and an output lamp,
- **When** both batteries are connected,
- **Then** the lamp illuminates.
- **And** disconnecting either battery extinguishes the lamp.
- **And** `tools/e2e/cat-013.test.ts` passes 100% in Chrome serially.

### Story 9.7: Electrical Logic Gates NAND & NOR (CAT-024, CAT-025)
*Status: Split on 21 Oct 2026 into Stories 9.7a (NAND) and 9.7b (NOR), one new element per story (AGENTS.md). The text below is their shared source. Each split story applies it to its own gate, with its own suite in place of `cat-024-025.test.ts`.*

*Depends on: Stories 8.1, 8.2, 9.5b (switch electrical mode for its input power switches; Open scheduling item b, decided 18 Oct 2026) and 9.6 (gate element and sockets). Moved from Story 9.3.*

As a player,  
I want Electrical NAND and NOR gates to evaluate Boolean conditions,  
So that complex electrical logic contraptions can be constructed.

**Acceptance Criteria:**
- **Given** Electrical NAND and NOR gates wired to input power switches,
- **When** all four input permutations (00, 01, 10, 11) are tested,
- **Then** output power states match standard Boolean truth tables with 100% accuracy.
- **And** `tools/e2e/cat-024-025.test.ts` passes 100% in Chrome serially.

### Story 9.7a: Electrical NAND Gate (CAT-024)
*Status: Backlog. Delivery position 25. Split from Story 9.7.*

*Depends on: Stories 8.1, 8.2, 9.5b (input power switches) and 9.6 (gate element and sockets). Source: [CAT-024](../../docs/planning/elements/CAT-024-electrical_nand.md) §5. Deletes: nothing, because this is a new element. It ships as a package with no record type and adds no Story 9A.45a known-violations entry.*

As a player,  
I want an Electrical NAND gate,  
So that I can build a circuit that stays powered unless both conditions hold.

**Acceptance Criteria:**
- **Given** a NAND gate wired through real UI to two input power switches (Story 9.5b) and an output consumer, **When** the four input permutations 00, 01, 10 and 11 are tested, **Then** the output is powered for 00, 01 and 10, and unpowered for 11. This is Story 9.7's truth-table AC, for this gate.
- **And** a gate with no upstream supply powers nothing (negative control).
- **And** Run/Reset is exact against the Story 9R.1 snapshot, and Save/Load preserves the gate and its typed connections.
- **And** the 9A.45a ratchet passes with no new entry.
- **And** `tools/e2e/cat-024.test.ts` passes 100% in Chrome serially.

### Story 9.7b: Electrical NOR Gate (CAT-025)
*Status: Backlog. Delivery position 27. Split from Story 9.7.*

*Depends on: Stories 8.1, 8.2, 9.5b and 9.6. Source: [CAT-025](../../docs/planning/elements/CAT-025-electrical_nor.md) §5. Deletes: nothing, because this is a new element. It ships as a package with no record type and adds no 9A.45a entry.*

As a player,  
I want an Electrical NOR gate,  
So that I can build a circuit that is powered only when neither condition holds.

**Acceptance Criteria:**
- **Given** a NOR gate wired through real UI to two input power switches and an output consumer, **When** the four permutations are tested, **Then** the output is powered only for 00 (Story 9.7's truth-table AC, for this gate).
- **And** a gate with no upstream supply powers nothing (negative control).
- **And** Run/Reset is exact, and Save/Load preserves the gate and its connections.
- **And** the 9A.45a ratchet passes with no new entry.
- **And** `tools/e2e/cat-025.test.ts` passes 100% in Chrome serially.

### Story 9.8: Electrical Logic Gates OR & XOR (CAT-026, CAT-027)
*Status: Split on 21 Oct 2026 into Stories 9.8a (OR) and 9.8b (XOR), one new element per story (AGENTS.md). The text below is their shared source. Each split story applies it to its own gate, with its own suite in place of `cat-026-027.test.ts`.*

*Depends on: Stories 8.1, 8.2 and 9.6 (gate element and sockets). Moved from Story 9.4.*

As a player,  
I want Electrical OR and XOR gates to route and differentiate electrical signals,  
So that branching circuits and toggle mechanisms can be built.

**Acceptance Criteria:**
- **Given** Electrical OR and XOR gates,
- **When** all input combinations are evaluated,
- **Then** XOR outputs high only when inputs differ (01, 10) and low when identical (00, 11).
- **And** OR outputs high whenever at least one input is high.
- **And** `tools/e2e/cat-026-027.test.ts` passes 100% in Chrome serially.

### Story 9.8a: Electrical OR Gate (CAT-026)
*Status: Backlog. Delivery position 29. Split from Story 9.8.*

*Depends on: Stories 8.1, 8.2 and 9.6. Source: [CAT-026](../../docs/planning/elements/CAT-026-electrical_or.md) §5. Deletes: nothing, because this is a new element. It ships as a package with no record type and adds no 9A.45a entry.*

As a player,  
I want an Electrical OR gate,  
So that either of two conditions can power a circuit.

**Acceptance Criteria:**
- **Given** an OR gate wired through real UI to two supplied inputs and an output consumer, **When** every input combination is evaluated, **Then** the output is powered whenever at least one input is high (Story 9.8's OR AC).
- **And** with both inputs low, the output is unpowered (negative control).
- **And** Run/Reset is exact, and Save/Load preserves the gate and its connections.
- **And** the 9A.45a ratchet passes with no new entry.
- **And** `tools/e2e/cat-026.test.ts` passes 100% in Chrome serially.

### Story 9.8b: Electrical XOR Gate (CAT-027)
*Status: Backlog. Delivery position 31. Split from Story 9.8.*

*Depends on: Stories 8.1, 8.2 and 9.6. Source: [CAT-027](../../docs/planning/elements/CAT-027-electrical_xor.md) §5. Deletes: nothing, because this is a new element. It ships as a package with no record type and adds no 9A.45a entry.*

As a player,  
I want an Electrical XOR gate,  
So that I can build toggles that react when exactly one input is high.

**Acceptance Criteria:**
- **Given** an XOR gate wired through real UI to two supplied inputs and an output consumer, **When** every input combination is evaluated, **Then** the output is high only for 01 and 10, and low for 00 and 11 (Story 9.8's XOR AC).
- **And** a gate with no upstream supply powers nothing (negative control).
- **And** Run/Reset is exact, and Save/Load preserves the gate and its connections.
- **And** the 9A.45a ratchet passes with no new entry.
- **And** `tools/e2e/cat-027.test.ts` passes 100% in Chrome serially.

---

## Epic 9R: Retro-Verification Repair

**Goal:** Make every reopened Pass true. Player-visible bugs found by the 18 Oct 2026 retro-verification are fixed here; the missing Chrome proofs are added; and every story whose Pass was invalid or weak is independently re-verified on fresh evidence, with dated addenda to its review record.

**Delivery order (coordinator decision under owner delegation, 18 Oct 2026; Open scheduling item i).** After Story 9.4b and before Story 9.4c and Epic 9A. Stories run in number order, 9R.0 to 9R.18, except that Story 9R.0c runs first: it fixes a player-visible crash (coordinator, 19 Oct 2026). Story 9R.0 (the C# static gate) comes next because it strengthens every later gate.

*Superseded on 21 Oct 2026 by the [Delivery order](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination):*
- *9R.6 and 9R.7 stay on the critical path;*
- *9R.8–9R.17 merge into their family conversions as suite-first guards (9R.14 into 9A.27, 9R.17 into 9.5a);*
- *9R.18 is dropped;*
- *9R.20 merges into 9A.25a;*
- *9R.19, 9R.21 and 9R.22 follow all deletion work.*

**Evidence.** [Retro-verification report, 18 Oct 2026](retro-verification-2026-10-18.md). Defect numbers (#1–#20) are the report's §G numbering. Its line numbers come from HEAD `8f682f8`; the symbol named with each line is authoritative.

**Rules for every 9R story.**
- Repairs use compiled Run-time tables built at Run from the construction (the Story 9.4 `SwitchedSupplyRouteDeclaration` pattern). No new heuristic (size, drag, joint limit or element-id detection) and no shim is added.
- Epic 9A later converts these tables to package declarations without changing behaviour.
- Each story names the reopened original story it repairs. Story 9R.18 closes the reopened records. *From 21 Oct 2026, each closing story appends the dated addenda instead ([reopened-record closure](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination)).*
- Chrome proof uses actual UI controls: no setters, imported solutions or numeric placement.
- Exact Run/Reset is asserted against the pre-Run snapshot helper from Story 9R.1.

### Story 9R.0: Real C# Static Analysis Gate
*Status: Done (19 Oct 2026). Murdoch-3's round-7 terminal verdict was given on `ea4edab4` ([review](../implementation-artifacts/review-9r-0-csharp-static-gate.md)).*
- *The gate: analyzers with warnings as errors, checked against the actual Csc inputs.*
- *Native results: 1692/1692 tests and 0 warnings.*
- *Chrome: one full run (33/34) found a regression from this story, which was fixed. The failed suite (clock-master 8/8), the smoke pair and the checklist then passed.*
- *CI now runs on every push and every PR.*
- *Open Lows: the forced fault prints two stderr lines, identical to main (owned by 9A.24); CA2000 is left at its default; the publish isn't byte-reproducible; a hook edit in the working tree can bypass the gate (CI is the backstop).*

*Depends on: Story 9.4b (the IL2125 warning is removed there). Closes report #18 and X1: `anvil check --changed` scans no `.cs` (review-9-4 matrix row A2, `anvil-csharp-control`: a probe `.cs` file with two empty catch blocks returned 0 warnings). Every recorded "anvil 0 warnings" claim on C# was vacuous; this story makes the C# gate real for every story, reopened or new.*

*Deletes: the DoD claim that `anvil check --changed` covers C#, in `_bmad-output/planning-artifacts/autonomous-development-guide.md` (DoD item 2; pre-commit and reviewer steps) and in this file's Global DoD item 2.*

As the owner,  
I want C# defects such as swallowed exceptions to fail the build,  
So that "0 warnings" means something for the engine code.

**Acceptance Criteria:**
- **Given** every production and test C# project (including `elements/*`, the Abstractions, the Simulation and Animation workers, `tools/BundleElements` and `CuriousContraptions.tests`), **When** it builds, **Then** .NET analyzers are enabled with `AnalysisLevel` latest-recommended plus `TreatWarningsAsErrors` (via `Directory.Build.props`) or an equivalent Roslyn gate. Any rule left disabled is listed with a one-line justification.
- **And** every warning this exposes is fixed at its source, not suppressed (any unavoidable suppression is justified inline and listed).
- **And** the repository pre-commit hook runs `dotnet build CuriousContraptions.slnx` with the gate, and a commit with a C# warning is refused.
- **And** both DoD texts (the guide and this file's Global DoD) state that anvil does not analyse C#, and that the C# gate is `dotnet build` with analyzers and warnings as errors.
- **And** a negative control proves the gate fires: a temporary `.cs` file with an empty `catch {}` and an unused variable fails the build with both diagnostics and is removed; the unchanged tree builds clean (native proof).
- **And** `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview (the Story 9.4b baseline), since warning fixes touch shipped code.

### Story 9R.0c: Undelivered Parts Leave the Palette and Bench Placement Never Throws
*Status: Done (19 Oct 2026). Murdoch-4 gave a scoped Pass on candidate 4, `8de7f259` ([review](../implementation-artifacts/review-9r-0c-undelivered-parts-palette.md), [evidence](../implementation-artifacts/evidence-9r-0c/)). Scope grew from a defect-class audit: all 19 `async void` UI handlers now catch at a boundary that logs, shows a status and never rethrows. Before, an exception that escaped one of them killed the browser .NET runtime (proven in Chrome). Open Lows owned by Epic 9A: `_gpuPending` is set before the try in Run, Reset, Save and Load; the guard test is a regex scan; palette-placement hard-codes its rows.*

*Origin: the owner reported on 19 Oct 2026 that the Balloon looks like a basketball and the game crashes the moment it is added. Investigation notes are in the session scratchpad, `balloon-investigation/notes.md`.*

*Root causes:*
- *Commit `8f682f8` (Story 6.5d) removed the `Parameters` lines from `parts/catalog/balloon.tres` and `tennis.tres`. That defeated the text check in `engine/PartRegistry.cs:30-38` that excluded them, so both entered the Free palette through `WorkshopInventory.GenerateFree`.*
- *Both resources use `parts/scenes/ball.tscn`, the basketball scene.*
- *Clicking the bench calls `World.CaptureInstance` in `private async void Click` (`ui/Workshop.cs:521`). That call sits outside `SubmitConstruction`'s try/catch, so it throws an untyped `ArgumentException` (`engine/MachineWorld.Gpu.cs:138`) from an `async void` method, after `PushUndo` has already run.*

*Coordinator decision under owner delegation, 19 Oct 2026: pull forward the palette-removal part of Story 9A.14, which was the owner's decision of 18 Oct (Open scheduling item h). Deleting the resources and `PartId` members stays in 9A.14, and Story 12.1 returns both parts as real packages.*

*Deletes:*
- *the text-match admission check (`content.Contains("Parameters = {")`) in `PartRegistry`;*
- *the three test assertions that lock Tennis and Balloon into the registry (`PartRegistryTests.cs:17,23-24`, `BasketballResourceTests.cs:34`, `WorkbenchCapacityTests.cs:168`).*

As a player,  
I want every part in the palette to be placeable, and a part that can't be placed to be refused with a message,  
So that the Workshop never crashes when I add a part.

**Acceptance Criteria:**
- **Given** the Free Workshop palette, **When** it is built, **Then** it offers only parts with a constructible instance path. Tennis and Balloon are absent.
  - Admission comes from an explicit, typed, checked-in set of undelivered parts, owned by 9A.14 and 12.1, not a text match on resource contents.
  - A test fails if any palette part has no `CaptureInstance`, `BallMaterial`, `WorkbenchFootprint` or save-decode path.
- **And** placing a part on the bench never throws past the UI.
  - `CaptureInstance` in `Click` is moved inside the same typed rejection path `CommitSelected` uses.
  - `PushUndo` runs only after a successful capture.
  - An unsupported part produces one typed rejection with a visible status message, and leaves no undo entry.
  - A native negative control proves it: force an unsupported id through `Click`'s capture path.
- **And** a Chrome e2e suite `tools/e2e/palette-placement.test.ts`, using actual UI controls, places every Free palette part on the bench once and verifies that each appears with no console or worker error. It asserts Tennis and Balloon are absent from the palette.
- **And** e2e scope: Tier 2, meaning this suite plus the smoke pair, because no engine code changes. Tier 1 passes 100%.

### Story 9R.0a: Tiered and Parallel E2E Runner
*Status: Done (19 Oct 2026). Murdoch-5 gave a scoped Pass on `d8e071f9` ([review](../implementation-artifacts/review-9r-0a-tiered-parallel-e2e-runner.md)).*
- *The runner is `tools/E2eRunner`. Calibration settled on N = 2, and the parallel full run passed 34 files and 166/166 in 1085 s, against about 26 min serially.*
- *A debt fault now fails a suite.*
- *Tier 2 selection includes the parts that a suite's levels pre-place.*
- *Cut-scope items and review findings F1–F14 are filed to Story F.1.*

*Depends on: Story 9R.0. Owner decisions, 19 Oct 2026 ([Verification tiers](autonomous-development-guide.md#verification-tiers)):*
- *a full run only for material engine changes, otherwise impact-selected suites;*
- *a background checkpoint after every 5 newly verified named puzzle elements; infrastructure/documentation stories do not advance that counter (owner amendment, 21 Oct);*
- *parallel Chrome workers for suites that don't depend on timing.*

*Deletes: the hand-rolled scratch gate scripts that serialise all suites, and every remaining "all suites serially" or "cumulative twice" wording in active docs. Tracked evidence scripts stay unchanged as historical evidence (AGENTS.md).*

*Coordinator decisions on the spec, 19 Oct 2026 ([spec](../implementation-artifacts/spec-9r-0a-tiered-parallel-e2e-runner.md)):*
- *the runner is C# (`tools/E2eRunner`), as AGENTS.md says to use C# wherever possible;*
- *`E2E_PREVIEW_URL` is required, and the :8060 defaults are dropped;*
- *Story 9R.1's Node unit test moves to `tools/e2e/unit/`, so the runner doesn't treat it as a Chrome suite;*
- *the background checkpoint is proven against a local, unpushed candidate commit that contains the runner;*
- *the full run is selected for the guide's engine list plus a small set of central UI and world files; other `engine/` and `ui/` files select suites through the typed area table;*
- *Story 9R.0b's "two back-to-back serial runs" becomes one full runner run.*

*Coordinator scope cut, 19 Oct 2026 (during implementation): this story delivers the typed runner (serial by default, Parallel only where the catalogue marks it), the `CCGPU_DEBT_FAULT` close finding with the typed expected-fault assertion, the Tier 2 selection (unknown changes select Full; a changed element with no Primary suite is a typed error), the calibration and one parallel full run, skipped and todo tests failing the run, and the runner publishing the bundle itself. The second proof run, the background checkpoint (the AC below), signal handling and the cross-worktree slot lock (one runner lock instead) move to one follow-up story at the back of the queue.*

As the owner,  
I want the full Chrome run to take minutes, and each story to run only the suites its change affects,  
So that element delivery isn't gated on a 26-minute serial run.

**Acceptance Criteria:**
- **Given** `tools/e2e/`, **When** a full run is requested, **Then** one checked-in runner (C# or TypeScript, the same tooling the repo already uses) runs every `tools/e2e/*.test.ts` file in its own process with per-file TAP, exit code and close findings, and writes one summary.
  - Suites marked timing-sensitive run serially, with no other Chrome running: clock-master, cat-017, cat-023b, and any stall, load or time-measuring suite.
  - The rest run on N parallel Chrome workers.
  - N is chosen from measurement and recorded.
- **And** the timing-sensitive set is a typed, checked-in declaration, not a filename heuristic. A test fails if a suite that measures time or injects stalls or load isn't in it.
- **And** the runner can be given a changed-file set and emits the Tier 2 selection: the changed element's suite, its connected and integration suites from the catalogue or bundle port declarations, suites touching changed shared code, and the smoke pair `engine-core-2b1` and `engine-core-2a1`. Each selected suite carries a one-line reason. An unknown or shared change selects the full run.
- **And** the runner records the served bundle identity before and after, and refuses to start if the identity changes mid-run.
- **And** the parallel full run passes 100% on two consecutive invocations (proof of the runner itself, not a per-story rule). Its wall time and the serial baseline (about 26 min) are recorded.
  - **And** a seeded failure in one parallel suite is reported against that suite, with all other suites unaffected.
- **And** the Tier 3 checkpoint can run against a committed HEAD in a separate worktree and preview port, so it can run in the background while the next story proceeds.
- **And** `dotnet build` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and the Node harnesses pass.

### Story 9R.0b: AOT-Compiled WASM SIMD Workers
*Status: Done (20 Oct 2026). Murdoch-6 gave a scoped Pass on `7a8a48e4` ([review](../implementation-artifacts/review-9r-0b-aot-compiled-wasm-simd-workers.md)).*
- *Both workers now ship as LLVMOnly AOT wasm-simd128 code, behind a publish guard.*
- *Physics tick p50 fell from 3.91 ms to 0.54 ms on cat-023b, and the solver uses 66 f32x4 operations.*
- *The full run passed 166/166.*
- *Findings F1–F12 are filed to Story F.2.*

*Depends on: Story 9.4b. New story (coordinator, 18 Oct 2026). It restores a stated requirement: AGENTS.md and `docs/gpu-f32-physics.md` require WebAssembly 128-bit SIMD physics on a dedicated worker. Neither `CuriousContraptions.Simulation.csproj` nor `CuriousContraptions.Animation.Worker.csproj` sets `RunAOTCompilation`, so both workers run .NET IL in the WebAssembly interpreter, where `WasmEnableSIMD` cannot vectorise managed code. Story 9.4b measured physics ticks at 3–4 ms in Chrome against about 0.08 ms native.*

As a player,  
I want the physics and animation workers to run as compiled WebAssembly SIMD code,  
So that machines run smoothly and stay responsive as I add more parts.

**Acceptance Criteria:**
- **Given** the production publish of `CuriousContraptions.web`, **When** the Simulation and Animation workers are built, **Then** both are AOT-compiled to WebAssembly with SIMD enabled (`RunAOTCompilation`, `WasmEnableSIMD`). The published bundle contains no interpreter-only managed execution path for the engine. This is proven by build output and a bundle inspection test.
- **And** the build fails explicitly if the AOT toolchain (wasm-tools workload) is missing. It never silently falls back to the interpreter, and there is no CPU or interpreter fallback.
- **And** tick time for the cat-023b wired-chain scene and the largest current Free-workshop fixture is recorded in Chrome as telemetry before and after. This is advisory per the game-grade envelope; formal qualification stays at P0-034.
- **And** `dotnet test` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one full run of the Story 9R.0a runner against the AOT production preview. This replaces "two back-to-back serial runs" under the owner's verification tiers of 19 Oct 2026.
- **And** published bundle size and first-load time are recorded, and DESIGN.md or the release notes state any change in first-load behaviour.

### Story 9R.1: Exact Run/Reset Snapshot Assertions in Every Listed Suite
*Status: Done (20 Oct 2026). Murdoch-7 gave a scoped Pass on `b1390cd9` ([review](../implementation-artifacts/review-9r-1-exact-run-reset-snapshot-assertions.md)).*
- *Reset is now checked against an exact pre-Run snapshot (raw f32 bits) in ten suites.*
- *The Counter and Clock activation memory is committed and restored on Reset.*
- *Probes that Reset while Running pass.*
- *The serial full run passed 166/166.*
- *cat-020 is SerialAlone under the AOT workers.*
- *Findings are filed to Story F.3.*

*Depends on: Story 9R.0. Closes report #14 and the §D last row. No suite captures a pre-Run state and compares against it in cat-048a (`:100`), cat-048b (`:88`), cat-049, cat-050, cat-030, cat-051 (`driver.reset()` with no assertion), cat-052, cat-017 (`:278`), cat-002 or cat-020 (`:435`). The pipe suites also reuse state left by the previous test. Re-verifies the lifecycle claims of Stories 6.6, 6.7, 6.8, 6.9, 6.10, 6.11, 8.2, 9.1, 9.2 and 9.3.*

*Deletes: the range-only Reset checks in those suites, and the test-order dependence (each test now builds its own bench).*

As a player,  
I want Reset to put my machine back exactly as I built it,  
So that every retry starts from the same place.

**Acceptance Criteria:**
- **Given** a shared `tools/e2e/workshop-driver.ts` helper that captures the committed construction before Run (every body pose as f32 bits, part parameters, connections and goal state) through the existing committed-read channel, **When** a suite runs, resets and re-reads, **Then** the helper asserts bit-equality with the pre-Run snapshot and reports the first differing field.
- **And** the helper's negative control passes: a unit check over a snapshot with one perturbed f32 bit fails and names that field.
- **And** each listed suite asserts the snapshot after Reset, and after a second Run then Reset.
- **And** each test in cat-048a, cat-048b, cat-049 and cat-050 builds its own bench, and each passes when run alone.
- **And** cat-051 asserts the blade's closed pose after Reset; cat-017 asserts clock phase 0; cat-020 asserts count 0 and an open contact.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings (Story 9R.0 gate), `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.2: Collider Table Sized at Run (Reopened Story 6.9b)
*Status: Done (20 Oct 2026). Murdoch-8 gave a scoped Pass on `50ba7c72` ([review](../implementation-artifacts/review-9r-2-collider-table-sized-at-run.md)).*
- *The collider table is sized from the compiled scene, so the Free maxima now admit.*
- *The dead ABI contact cache is deleted, which removes the 888-pair ceiling.*
- *The broadphase pruning is byte-identical across 9 scenes.*
- *The full run passed 169/169.*
- *Findings are filed to Story F.4.*

*Depends on: Story 9R.0. Closes report #6: `ColliderCapacity = 2048` (`engine/gpu/PhysicsDeclarations.cs:168`) while 32 × `pipe_bend_90` or 32 × `funnel` = 32 × 72 + 1 = 2,305 colliders. Build admits the construction because `WorkbenchCapacity.Validate` (`WorkbenchCapacity.cs:68-106`) no longer counts colliders; the `PhysicsSceneDeclaration` constructor then throws "Invalid physics document or capacity" at Run (`PhysicsDeclarations.cs:211`). Repairs Story 6.9b AC2 ("any construction within the 32-instance budget is admitted").*

*Deletes: the fixed 2,048 `ColliderCapacity` ceiling and the Run-time throw it causes for admitted constructions.*

As a player,  
I want any bench the Workshop lets me build to run,  
So that a full bench of bends never crashes when I press Run.

**Acceptance Criteria:**
- **Given** a construction within the 32-instance budget, **When** Run compiles it, **Then** the collider table is sized from the compiled construction's actual collider count, so every Build-admitted construction compiles.
- **And** a native property test builds 32 instances of each catalogue element (including `pipe_bend_90` and `funnel`, 2,305 colliders) and the 30-instance mixed case, and each compiles and steps.
- **And** a construction over the 32-instance budget is still rejected at Build with the existing message (negative control).
- **And** in actual Chrome, the new `tools/e2e/capacity-9r.test.ts` places 32 `pipe_bend_90` through the real drawer, runs, resets (snapshot exact) and save/loads with no worker fault.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.3: Each Directly Wired Gate Follows Its Own Battery (Story 8.2 Repair)
*Status: Done (20 Oct 2026). Murdoch-9 gave a scoped Pass on `3414f836` ([review](../implementation-artifacts/review-9r-3-direct-gate-own-supply-route.md)).*
- *Gates now open and close with their own battery's in-run Enable or Disable, including on two separate circuits.*
- *Coordinator decision, 20 Oct 2026, on the spec row "Authored Disabled, enabled in run → the ball passes": the gate opens and releases the ball, which meets the epics AC.*
- *A ball already resting against the closed, tilted blade is thrown back upstream when the blade rises. That behaviour exists on base too, and Story 9A.19 now owns it.*
- *Findings are filed to Story F.5.*

*Depends on: Story 9R.1. Closes report #2:*
- *Direct-mode gates are compiled from the battery's **authored** enable (`WorkshopPhysicsCompiler.cs:190-206`, blade drag 0.0625).*
- *The in-run toggle only updates `_electricalControls` and never reaches the gate (`WorkshopSimulation.cs:52-65`).*
- *`batteryPowered` becomes true when **any** source is enabled (`WorkshopPhysicsEngine.cs:767-776`).*
- *`PoweredGateTests.cs:198` pins the drag code.*

*Repairs Story 8.2's functional contradiction (Story 8.2 stays `done`, marked weak).*

*Deletes:*
- *the global `batteryPowered` flag;*
- *the direct-mode authored-enable compile and its drag band (the 0.03–0.08 band in `PrismaticConstraintSolver.cs:247` and the 0.0625 encoding);*
- *the 9R.3 entry in the Story 9.4b element-scan ratchet's known-violations list;*
- *the pinning assertion in `PoweredGateTests.cs:198`.*

As a player,  
I want a gate to open and close with its own battery's switch during a run,  
So that two gates on two batteries behave as two separate circuits.

**Acceptance Criteria:**
- **Given** a Battery wired directly to a Powered Gate through real UI, **When** Run compiles the construction, **Then** a direct supply route row (battery source → gate consumer, the same compiled route table as the Story 9.4 `SwitchedSupplyRouteDeclaration`) is emitted. Its powered state is the committed electrical state that the typed `ConfigureElectrical` command updates.
- **And** a battery authored Disabled opens its gate when the player presses in-run **Enable supply**, and an Enabled battery closes its gate on in-run Disable.
- **And** two batteries each wired to their own gate are independent: disabling one leaves the other gate open (negative control for the global flag).
- **And** an unpowered gate still blocks the ball (control).
- **And** Run/Reset matches the pre-Run snapshot (authored enable restored, blade closed), and Save/Load preserves both wires.
- **And** `cat-051` and the new `tools/e2e/cat-051-own-supply.test.ts` pass 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.4: Each Pressure Plate Honours Its Own Minimum Mass (Reopened Story 9.1)
*Status: Done (20 Oct 2026). Murdoch-10 gave a scoped Pass on `15b7431b` ([review](../implementation-artifacts/review-9r-4-pressure-plate-per-instance-load-rows.md)).*
- *Each plate compiles its own contact-load row, using a normal-and-gap region and f32 mass.*
- *The 8-slot activation limit is removed (owner decision).*
- *The C# gate is re-pinned for SDK 10.0.402.*
- *The reviewer's full run passed 185/185.*
- *Findings are filed to Story F.6.*

*Depends on: Story 9R.3. Closes report #1:*
- *`_plateLoaded = currentPlateLoad >= 0.5f` ignores the instance's `MinimumMass` (`WorkshopPhysicsEngine.cs:856`).*
- *The plate is found by half-extents 0.9/0.05/0.9 (`:834`, `:838`).*
- *One global `_plateLoaded` drives every plate-switched gate (`PrismaticConstraintSolver.cs:248`; compiler drag 0.09375), so plates cross-talk.*
- *`ContactLoadSensorTracker` has no production caller, so the native tests `PressurePlateTests.cs:193-305` prove dead code.*

*Deletes:*
- *the global `_plateLoaded` flag and its 0.5 kg literal;*
- *the half-extent plate detection;*
- *the plate drag band and its 0.09375 encoding;*
- *the 9R.4 entries in the Story 9.4b element-scan ratchet's known-violations list. Removing them must leave the scan green.*

As a player,  
I want each Pressure Plate to respond to the weight on it and only to that,  
So that two plates switch two gates independently.

**Acceptance Criteria:**
- **Owner decision, 20 Oct 2026: there is no limit on activation slots.** In the owner's words, "there should be no restriction on the number of activation slots, this was not in the description and is an unreasonable limitation".
  - The fixed 8-slot activation table is removed. Activation slots and activation memory are sized from the compiled scene at Run.
  - Build never refuses a machine for its count of activation parts (plates, counters, clocks, latches, delays and so on), only for the ordinary Free-palette part maxima.
  - Proven by a bench at the palette maximum of plates combined with counters, clocks and latches. It must compile, step, Reset exactly and survive Save/Load. In it, the last plate opens its own gate.
- **Given** Pressure Plates built through real UI, **When** Run compiles the construction, **Then** each plate emits its own compiled contact-load row (owner body, region, normal, alignment, and `minimum_mass` from that instance; CAT-052 §2). `ContactLoadSensorTracker` evaluates those rows at runtime, so its native tests now prove the live path.
- **And** each plate's supplied contact is a switch in the compiled switched-supply route table (Battery → plate contact → gate).
- **And** a domino (0.4 kg) leaves the plate Underweight and the gate closed; a basketball (1 kg) closes it; mass exactly at the minimum closes it (inclusive ≥, CAT-052 §2); two bodies sum.
- **And** removing the body opens the contact on the next tick.
  - Story 9R.5 adds the 0.1 s (12-tick) release hold for both the contact and the plate's ActivationOut level.
  - Coordinator decision, 20 Oct 2026: the hold is delivered once, in 9R.5, and not split across two stories.
- **And** two plates wired to two gates are independent: loading one opens only its own gate (negative control for cross-talk).
- **And** a static box with the plate's exact half-extents but no plate declaration never switches a gate (negative control for size detection).
- **And** a native test with a non-default `minimum_mass` (2.0 kg, saved and loaded) proves the instance value governs. The UI editor for it arrives with the generic parameter editor (Story 9A.40).
- **And** Run/Reset matches the snapshot (Empty, gate closed), and Save/Load preserves `minimum_mass`.
- **And** `cat-052` and the new `tools/e2e/cat-052-two-plates.test.ts` pass 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.5: Pressure Plate ActivationOut Lights a Signal Lamp (Reopened Story 9.1)
*Status: Done (20 Oct 2026). Murdoch-11 gave a scoped Pass on `d6f5cacd` ([review](../implementation-artifacts/review-9r-5-pressure-plate-activation-out-level.md)).*
- *The level is held for 12 ticks: lit through tick 11, dark at tick 12.*
- *The contact and the lamp share one held level.*
- *Wiring the plate to anything other than a lamp is refused.*
- *Findings are filed to Story F.7.*

### Story F.17: Single Codec Follow-ups
*Status: Backlog. Filed by the Story 9A.2 implementer on 21 Oct 2026 ([spec](../implementation-artifacts/spec-9a-2-single-flatbuffers-codec.md#implementation-notes)).*

**Acceptance Criteria:**
- F17-1: an instance record that carries a field of another element kind (a ramp record with a `radius`) is refused structurally; today the decoder ignores the foreign field.
- F17-2: the workbench guide rule (`WorkbenchFullException`) and an authored lesson's settings rules (`PuzzleSettings`) refuse a stored save as `WorkshopSaveRejectedException` reasons; today they stay the domain's own typed refusals. Story 9A.26 deletes the lessons; fold the remainder there.
- F17-3: the per-publish allocation of the FlatBuffers read (one array per committed read, the motion copy included) is measured and recorded against P0-034's budget. Story 9A.2 reuses one builder but did not measure.
- F17-4: Story 9A.15 removes `PartId.ToNumericId`/`FromNumericId`: no wire or save reads them since Story 9A.2, and `tools/Coverage` must take its declared-identity list from the bundle instead. *Delivered by Story 9A.15 (done, 21 Oct 2026).*
- F17-5: re-add mutants survive for a swallowing catch on the client read decode, a swallowing catch on the Animation endpoint decode, a retry on the client endpoint decode, and a C# `ResponseAbi` export. None of them is at head. Add a guard that kills each one.
- F17-6: an undefined command kind, and a counter target of 10 on the command path, are refused as a `WireFault` that names the reason, not as a plain `ArgumentException`.
- F17-7: the literal AC5 `rg` has 3 path-string hits left. Clear them or justify each one correctly.
- F17-8: `docs/engine-contracts.md:203` names the deleted `UnsupportedSave`, and two element docs name deleted files. Correct all three.
- F17-9: a Chrome test proves that content refusals keep the bench; today Chrome covers only frame damage.
- F17-10: the response fuzz fixture covers the contact-work and electrical sections again.
- F17-11: two publishes of one commit produce identical bundle identities, or the cause of the difference is found and recorded.
- F17-12: save pre-validation takes its list of the 22 decodable elements from the decoder or the bundle, not from a hand-written list.
- F17-13: `anvil check` reports 0 warnings (today GS-001 at `tools/e2e/workshop-driver.ts:985`), and Tier 1 evidence includes the anvil log.

### Story T.1: Fast-Test Quick Wins
*Status: In progress, partial shipment under review. Owner course correction, 21 Oct 2026: ship AC1–AC2, then proceed directly to 9A.16. AC3–AC4 are deferred acceleration/measurement work, preserved below without a completion or speedup claim. They do not block gameplay slices. Position 13a.*

As a developer, I want stale test code caught in Tier 1, accurate suite selection, and calibrated suites running in parallel, so that a story's Chrome time shrinks without losing any proof.

**Acceptance Criteria:**
- **AC1, type-check:** a `tools/e2e/tsconfig.json` and a `tsc --noEmit` step are part of Tier 1 (pre-commit or the documented Tier 1 command). A suite that reads a deleted accessor fails Tier 1, proven red with the 9A.2 `threshold()` case re-introduced.
- **AC2, selection:** `ImpactRules` stops escalating to Full for these classes of path:
  - an element package `.csproj` or the `.slnx` resolves to Element or Suite impact for the packages it touches;
  - catalogue rows resolve to Suite;
  - an element with no Primary suite gets one, or resolves to its declared suites instead of refusing with `NoPrimarySuite` (closes F19-4).

  Engine paths keep their Full rule. A table-driven test pins every rule, each with a mutant.
- **AC3, calibration:** the F.1 calibration runs, so every suite that is serial only by policy, not because it measures wall time, is either qualified Parallel (pinned to its suite digest) or recorded with the reason it stays serial. True timing suites stay serial and alone: clock-master, cat-017, cat-023a/b, cat-037, cat-014 and anim-1a/b/c.
- **AC4, measured result:** one Full run before and one after (the after run may double as the calibration run). Record the wall time of each, and the escalation rate of `run --diff` over the last 10 merged stories, replayed through the new rules.
- **Deletes:** the policy-serial entries that calibration replaces, and the `NoPrimarySuite` refusal path for elements that declare suites.

### Story T.2: Native Fixture Replay
*Status: Backlog, optional acceleration work after the owner’s playable-first course correction, 21 Oct 2026. Position 14a is not a gameplay prerequisite. Verified native coverage can replace covered assertions under [Verification tiers](autonomous-development-guide.md#verification-tiers). All ACs below remain.*

As a developer, I want each element suite's UI-built benches replayed natively through the real save codec and the production simulation, so that behaviour and save/load are proven in seconds on every change.

**Acceptance Criteria:**
- **AC1, fixtures from the actual UI:** each element and integration suite exports its UI-built save slot as a committed test input under `CuriousContraptions.tests/fixtures/`.
  - The fixtures are produced by the Chrome suite itself, through the actual UI controls, never hand-written.
  - They are plain test inputs: no golden-trace store, generator platform, cache or schema (AGENTS.md).
- **AC2, native replay:** a dotnet test decodes each fixture with `WorkshopSaveCodec`, compiles it with the production compiler, and runs it through `RunPhases`/`StepClock` (`WorkshopPhysicsEngineTests.cs:554-622`). It then asserts the suite's own behaviour and save/load outcomes, as meaning-level assertions with game-grade tolerances, not bit digests (JIT and wasm may differ).
- **AC3, no in-app acceleration:** stepping lives in the test host only (`requirements.md:93`), and no product mode is added.
- **AC4, mutant parity:** for each family covered, at least one behaviour mutant that its Chrome suite kills is also killed by the native replay.
- **AC5, refresh:** a stale fixture is detected: a fixture whose bundle element ids or schema no longer decode fails Tier 1 with a message naming the fixture and the suite that regenerates it.
- **AC6, coverage list:** the review records which suites and families are covered. The gate switch applies only to those.

### Story T.3: Trim Duplicated Chrome Variants
*Status: Backlog. Owner, 21 Oct 2026. Position 19a, after Story 9.5 and requiring T.2.*

As a developer, I want Chrome to keep each part's required UI proof and drop variants that T.2's native replay now proves, so that a Chrome Full run takes about 15–20 minutes.

**Acceptance Criteria:**
- **AC1, the per-part proof stays in Chrome:** every part and fixture keeps the full AGENTS.md set:
  - UI construction;
  - verified placed configuration and typed connections;
  - intended behaviour;
  - a meaningful negative/control;
  - exact Run/Reset restoration;
  - Save/Load.

  Rendering, lifecycle and timing proofs also stay in Chrome.
- **AC2, what may be deleted:** only Chrome tests or tick-stepping whose assertions a T.2 native replay demonstrably covers. A per-test mapping records each one, and requirement and test IDs are preserved in the mapping. An example is the tick-stepped cat-052-lamp tests 2 and 4.
- **AC3, measured result:** a Full run before and after, with the wall time recorded.
- **Deletes:** the mapped duplicate Chrome tests and the menu tick-stepping loops they used.

### Story T.4: Headless Real-Wasm Harness (back of queue)
*Status: Backlog. Owner, 21 Oct 2026.*

**Acceptance Criteria:**
- **AC1, spike:** the published `simulation/_framework` boots in Node, with a host-supplied clock through the `Bootstrap`/`Dispatch`/`ResumeTurn` exports and the `now`/`scheduleTurn`/`publish`/`acknowledge` imports. Record the blockers: the `native-clock.js` isolation check, the nested clock-master worker, the clock-derivation qualification and asset loading.
- **AC2, harness:** if the spike succeeds, the T.2 fixtures replay on the same wasm bytes Chrome runs, and a same-build trace digest is compared. There is an explicit re-bless step for the digests.

### Story T.5: Coverage-Based Suite Selection (back of queue)
*Status: Backlog. Owner, 21 Oct 2026.*

**Acceptance Criteria:**
- **AC1:** coverlet coverage over the T.2 native replays maps each `engine/gpu` file to the suites that exercise it.
- **AC2:** `E2eRunner` uses that map to narrow its Full escalation to native code paths.

### Story T.6: Persistent Browser Sessions (back of queue)
*Status: Backlog. Owner, 21 Oct 2026.*

**Acceptance Criteria:**
- **AC1:** suites reset state between tests instead of reloading the page, where the Reset proof allows it. The expected saving is at most about 3 minutes.

### Story F.20: Posted-Read Receipt Identity
*Status: Backlog, back of queue. Story 9A.3 review B2/E2; qualification owner P0-032. [Review](../implementation-artifacts/review-9a-3-typed-transport-control-commands.md).*

**Acceptance Criteria:**
- Given a generated publication still queued behind the actual posted read, when its identity is spoofed as a receipt, then managed transport accounting rejects it without releasing the posted read or retiring undelivered occurrences.
- Given the actual posted read's receipt, when it is accepted, then cumulative retirement still covers coalesced older publications; ordinary delayed receipts and Reset retain correct ownership.
- Keep typed C# identity validation and one transport path. Prove positive, spoofed, duplicate and boundary cases against production accounting; no compatibility channel.

### Story F.21: Bounded Memory-Query Completion
*Status: Backlog, back of queue. Story 9A.3 review B6; qualification owners P0-032/P0-034. [Review](../implementation-artifacts/review-9a-3-typed-transport-control-commands.md).*

**Acceptance Criteria:**
- Define the query's terminal deadline and distinguish it from the UI TimedOut indication and non-cancellable collector execution.
- Given a query whose declared deadline expires, when its acknowledgement is absent or late, then the caller receives an explicit terminal result, late replies capture nothing, and ownership is released safely without admitting conflicting simulation work.
- Prove success, deadline boundary, late acknowledgement and disposal using the current typed query transport; retain stopped-only paired capture and no raw memory channel.

### Story F.22: Stopped Reliable-Read Resource Bound
*Status: Backlog, back of queue. Story 9A.3 review B8; qualification owners P0-032/P0-034. [Review](../implementation-artifacts/review-9a-3-typed-transport-control-commands.md).*

**Acceptance Criteria:**
- Given repeated stopped-world publications without receipts, when the declared accounting capacity is reached, then resource use remains bounded with explicit admission/backpressure behavior, never silent reliable loss.
- Preserve exact published-identity rejection and cumulative retirement, including coalescing, queued occurrences and Reset; keep one authoritative accounting path.
- Prove the capacity boundary and receipt recovery against production accounting, alongside ordinary lifecycle controls and the named resource-stress qualification.

### Story F.19: Bundle Ports Follow-ups
*Status: Backlog. Filed from the Story 9A.17 review on 21 Oct 2026 ([review](../implementation-artifacts/review-9a-17-bundle-ports-and-footprint.md)).*

**Acceptance Criteria:**
- F19-1: the guard against re-adding the port table matches the table's structure as well as its names. Mutant R6 survives it today.
- F19-2: each declared port normal is checked against the facing of its scene locator.
- F19-3: the old snake_case port names in `content/puzzles.json` are removed, or the file is deleted if nothing reads it.
- F19-4: `delay` has a Primary suite in `SuiteCatalogue`, so `E2eRunner select` no longer refuses with `NoPrimarySuite`.
- F19-5: `DeclaredPorts` stops being a global singleton and is injected. A check proves that the worker's embedded bundle and the main thread's bundle have the same identity.
- F19-6: the two socket enums for one set merge into one.
- F19-7: the runtime reads declared footprints. Owned by Story 9A.16, then the family conversions.
- F19-8: the C# static gate's `/resource` exemption is pinned to the resolved bundle path.
- F19-9: anvil per-write validation runs before every write. Story 9A.17's implementer and reviewer both skipped it.

### Story F.18: Bundle Element Id Follow-ups
*Status: Backlog. Filed from the Story 9A.15 review on 21 Oct 2026 ([review](../implementation-artifacts/review-9a-15-bundle-element-id-save-wire.md)).*

**Acceptance Criteria:**
- F18-1: a committed test refuses a save whose puzzle inventory names an unknown element id. Today the mutant that removes this check survives, although head refuses it correctly.
- F18-2: `dotnet test tools/Coverage.Tests` passes on a fresh clone without a prior solution build: generate the bundle first, or declare the dependency.
- F18-3: a committed test proves the worker's decoder knows no element outside the bundle (the reverse direction of the parity check).
- F18-4 (Low): the schema guard matches field types and tables as well as field names.
- F18-5 (Low): the 101 test call sites that lack a space after the comma are formatted.
- F18-6: commit the guard test that kills mutant M9 (re-adding the generic per-id decode list); the implementer reverted it to keep the evidence commit evidence-only.

### Story F.16: Catalogue Follow-ups
*Current stage ownership 24 October 2026: category grouping and switch metadata native criteria travel with test-workshop-harness-b and test-behavioral-hygiene; Workshop startup refusal is required in harness A;9A.15 already owns rejected-id evidence. Cosmetic declaration correctness belongs to each affected element; final visual comparison remains post-catalogue. This aggregate must not postpone required native catalogue behavior until after browser eligibility.*
*Status: Backlog. Filed from the 9A.14+18 review on 21 Oct 2026 ([review](../implementation-artifacts/review-9a-14-bundle-sole-catalogue.md)).*

**Acceptance Criteria:**
- Resolve items F16-1 and F16-3 to F16-7:
  - category grouping versus the old order;
  - native pins for the switch's colour, description, title and icon;
  - naming the rejected Tennis/Balloon id (owned by 9A.15; delivered by Story 9A.15, done 21 Oct 2026);
  - a Workshop-level start-up refusal test;
  - the cosmetic items.

### Story F.15: Package Follow-ups (incl. Reproducible Publish Bundle Identity)
*PKG-3 review findings: F15-10 (titles, descriptions and colours) and F15-11 (Signal declaration parity) were moved into Story 9A.14 on 21 Oct 2026. F15-12, the cat-052-lamp spec wording, stays here.*
*Also covers the PKG-1 review findings F15-1 to F15-5:*
- *a test for the BUNDLE007 Real-parameter check;*
- *a test for the slider upper-limit check;*
- *a ratchet against re-adding moved scenes under `parts/scenes`;*
- *`PackageReferenceTests` covers `Reference` and `Compile` items;*
- *typed slider parameter keys ([review](../implementation-artifacts/review-9a-pkg-1.md)).*
*Status: Backlog. Filed by PKG-1 on 21 Oct 2026 ([spec](../implementation-artifacts/spec-9a-pkg-1-receiver-bowling-springboard-packages.md#e2e-chrome)).*

**Acceptance Criteria:**
- Two `dotnet publish` runs of one source give one runner `bundleIdentity`, or the varying files are named and the identity is defined over the reproducible ones. Today it differs:
  - `c948fec6` published as `30ae989e…` and `c194034b…` (evidence-9a-4);
  - PKG-1's three publishes of unchanged runtime sources gave `ceb64963…`, `589fffef…` and `3839c6e4…`.
- Reviewers can then match a reused Chrome run to a candidate by bundle identity alone.

### Story F.14: E2E Runner Selection and Bundle Gate Follow-ups
*Status: Backlog. Filed by Story 9A.4 on 21 Oct 2026 ([evidence](../implementation-artifacts/evidence-9a-4/runner-select-diff-844981c9.txt)).*
- *It also covers the 9A.4 review findings:*
  - *deletion ratchets: an empty property bag, an ignored bundle file, the runner rule (F14-2);*
  - *mass and inertia parity (F14-3);*
  - *rejecting an empty element id (F14-4);*
  - *a Release generator build (F14-6);*
  - *a randomised-order test run (F14-8).*
- *PKG-1 (21 Oct 2026) added owner-body motion, mass, drag and gravity parity for every mechanical package (`EveryMechanicalPackageDeclaresTheCompiledBodyMotionMassDragAndGravity`). Inertia is still not compared (F14-3).*
- *PKG-2 (21 Oct 2026, [raw](../implementation-artifacts/evidence-9a-pkg-2/runner-select-diff-8333682f.txt)) found two selection gaps that force Full: a `parts/scenes/*.tscn` path deleted by a move into `elements/<Name>/` resolves to no part (`Full(Unknown)`), because only head catalogues are read; and `tools/BundleElements.Tests/**` matches no rule (`Full(Unknown)`), although the tests are browser-free.*

**Acceptance Criteria:**
- `select --diff` and `run --diff` let a path that forces Full win over a `NoPrimarySuite` refusal raised by another path. Today `select --diff 844981c9` refuses with `NoPrimarySuite` for `elements/Ball`, although the same diff touches the Abstractions, `tools/BundleElements` and `tools/E2eRunner`, each of which selects Full.
- `ball` has a Primary suite, or its absence is a declared and tested decision. Every C-Balls story changes `elements/Ball`.
- `tools/BundleElements.Tests/**` has a rule (no browser impact), instead of selecting Full as an unmatched path.

### Story F.12: Architecture Doc Follow-ups
*Status: Backlog. Filed from the 9A.0 review on 21 Oct 2026 ([review](../implementation-artifacts/review-9a-0-architecture-authority-docs.md)).*

**Acceptance Criteria:**
- Items F.12-1 to F.12-7 are resolved:
  - the deleted D4 JS reader is recorded;
  - the `PartRegistry` source-map row is shown as historical;
  - the doc-consistency test checks `#anchors`;
  - the wording fixes.

### Story F.11: Joined Pipe Follow-ups
*Status: Backlog. Filed from the 9R.7 review on 21 Oct 2026 as items F10-1 to F10-10. F10-11, the placed-vs-loaded collider split, is owned by 9A.16.*

**Acceptance Criteria:**
- Every item in the review's F10-1 to F10-10 list is resolved or justified, including:
  - a test for the bend in-cell offset check;
  - a typed part id in `JoinedPipe.Validate`;
  - a Chrome proof of the save-rejection message.

### Story F.13: Legacy Purge Follow-ups
*Status: Backlog. Filed from the 9A.1 review on 21 Oct 2026 ([review](../implementation-artifacts/review-9a-1-dead-legacy-purge.md)).*

**Acceptance Criteria:**
- **Leftover version and format text:**
  - remove the `"Version": "1.0"` in the bundle JSON (F13-2), if 9A.4 hasn't already;
  - document the isolation marker's 4→3 change (F13-3).
- **Tests and checks:**
  - a test that kills a re-added `writeEventRing` (F13-4);
  - wrong-length and wrong-profile controls for the master-peer check (F13-8).
- **Docs:**
  - split the over-long `engine-contracts.md` line (F13-5);
  - document `--experimental-vm-modules` (F13-6).

### Story F.10: Retire the Remaining Format Version Fields
*Status: Done (21 Oct 2026). Merged into Story 9A.1, which passed on `b71d32fd`.*

*Original note: merged into Story 9A.1 (coordinator ruling, 21 Oct 2026, matching Story 9R.7's "the remaining format versions belong to 9A.1"); these ACs stay here and apply there. Filed by Story 9A.0 on 21 Oct 2026, under the owner's rule of the same day: "why are you versioning the wire format and the save formats, this isnt required and goes against the rule that says we dont have any backwards compatability". Story 9R.7 already deleted the wire, save and cadence-settings versions.*

*Deletes: `WorkshopClockWire.Version`, the `WorkshopMemoryWire` version, `WorkshopHint.Version`, `WorkshopTraceVersion`, `TraceTimingVersion`, `WorkshopScheduleVersion`, `PhysicsStateVersion` (and the worker's `STATE_VERSION`) and `BodyRecordVersion`, with every field that carries them and every check that compares them.*

**Acceptance Criteria:**
- **Given** the tree, **When** this story lands, **Then** `rg -w` for each deleted symbol returns 0 production hits, and no wire, ring, trace or record layout has a version field.
- **And** each affected decoder rejects bad input by structural validation only (verifier or checked length, defined enums, finite ranged values), with a negative control per decoder (truncated, unknown-tag and out-of-range input).
- **And** the [isolation authority's Current status](../../docs/architecture/element-package-isolation.md#current-status) row for these enums is removed.
- **And** Tier 1 passes, plus the runner's Tier 2 selection (the clock, schedule and trace suites).

### Story F.9: Tube Snapping Follow-ups
*Status: Backlog. Filed from the 9R.6 review on 21 Oct 2026.*

**Acceptance Criteria:**
- **Snapping behaviour:**
  - a test that the nearest candidate wins (F9-2);
  - the snapped pose is checked against the bench bounds (F9-3);
  - gizmo moves snap on release, per DESIGN.md (F9-4).
- **Tests and docs:**
  - the locked-target join is proven in Chrome, and CAT-048 fact 11 is reworded (F9-6; 9R.7 may cover this);
  - a stronger native outward-mouth check (F9-8);
  - the CAT-048 snapping facts are covered by tests (F9-9).
- **Visuals:** no 0.05 m seam between joined collars (F9-7).
- **Cleanup:**
  - one statement per line (F9-1);
  - no per-frame list allocation, a performance-gate item (F9-5).

### Story F.8: Capacity Follow-ups
*Status: Backlog. Filed from the 9R.5b review on 21 Oct 2026.*

**Acceptance Criteria:**
- **Stale wording:** remove the old cap wording from `EL-112`, `GAP-06` and `browser-physics-performance.md` (F8-1), and the "ball guide table" wording (F8-7).
- **Growth tests:** tests that kill the uncovered growth branches: the activation queue, the pose-ring body-count growth, the device and engine scratch, and the redundant `FitFeedbackSlots` call (F8-2/F8-3).
- **Performance gate:**
  - the per-frame presentation allocations and the per-read decode allocations (F8-4);
  - the unbounded contact pulses and the per-contact scans (F8-5).
- **Determinism:** deterministic bundle hashes across worktrees (F8-6).

### Story F.7: Plate Signal Follow-ups
*Status: Backlog. Filed from the 9R.5 review on 20 Oct 2026.*

**Acceptance Criteria:**
- A test that a stored hold disagreeing with the plate level is rejected (F7-1).
- One statement per line at `ActivationNetwork.cs:473` (F7-2).
- A dedicated test for the "lifted" case (F7-3).

*Depends on: Story 9R.4. Closes report #5: the plate declares only `PowerIn`/`Supply` and the lamp only `ActivationIn` (`WorkshopConnections.cs:62-66`, `:87-90`), so Story 9.1's headline AC ("plate wired to a Signal lamp") was impossible.*

*Coordinator decisions under owner delegation (18 Oct 2026; Open scheduling item i):*
- *CAT-052 Q2: the plate gains an ActivationOut **level** output, high while Loaded and low otherwise. Like the latch's output, it drives Signal lamps only. Rationale: the player sees at a glance whether the plate is pressed, and 9.1's AC becomes achievable.*
- *CAT-052 Q3 (coordinator override, 18 Oct 2026): activation is immediate on the first qualifying contact tick, and release is held for 0.1 s (12 ticks at 120 Hz) after the last qualifying contact. A ball bouncing on the plate therefore keeps the lamp steadily lit through the bounce and goes dark 0.1 s after it leaves. Rationale: without the hold, the lamp flickers and downstream logic chatters on every bounce; the hold adds no lag at activation.*

*Deletes: the element-keyed rule `source.PartId == PartId.Latch && target.PartId != PartId.SignalLamp` and the follow-the-latch check in `WorkshopConnections.Validate` (`engine/gpu/WorkshopConnections.cs:172-178`). They are replaced by a rule on the source port's declared output kind ("level" output), shared by the latch and the plate.*

As a player,  
I want to wire a Pressure Plate to a Signal lamp,  
So that the lamp shows when something heavy enough rests on the plate.

**Acceptance Criteria:**
- **Given** a Pressure Plate whose ActivationOut is wired to a Signal lamp through the real wiring UI (the icon-only socket choice where ambiguous), **When** a basketball comes to rest on it, **Then** the lamp lights.
- **And** when the ball rolls off or is lifted, the lamp goes dark on the first tick after the 0.1 s (12-tick) release hold (coordinator decision, 18 Oct 2026).
- **And** a domino below the minimum leaves the lamp dark. A ball that bounces on the plate keeps the lamp steadily lit between contacts closer together than 0.1 s, and leaves the lamp dark 0.1 s (12 ticks) after its last contact (controls; boundary at 11 / 12 / 13 ticks).
- **And** wiring the plate's ActivationOut to a Counter, Delay or latch input is refused with "A level output can only light a Signal lamp.", and the latch's existing refusal behaves identically (negative control and regression).
- **And** the plate's supplied contact still switches a gate at the same time, so both domains work on one plate.
- **And** Run/Reset matches the snapshot (lamp Slate), and Save/Load preserves the `activation_out` wire.
- **And** the new `tools/e2e/cat-052-lamp.test.ts`, `cat-052` and `cat-037` pass 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.5b: Every Physics Table Sized at Run (No Fixed Capacities)
*Status: Done (21 Oct 2026). Murdoch-12 gave a scoped Pass on `a5a6c494` ([review](../implementation-artifacts/review-9r-5b-physics-tables-sized-at-run.md)).*
- *Every physics, worker and wire table is sized from the scene, and the 32-part Build limit is removed (no requirement states it).*
- *One machine past every old cap runs, and the full run passed 194/194.*
- *Findings are filed to Story F.8.*
- *Origin: coordinator, 20 Oct 2026, under the owner's rule that there are no arbitrary engine caps.*

**Acceptance Criteria:**
- **Given** a machine with any number of trigger-bearing parts up to the ordinary Free-palette maxima (Impact Switches and every other trigger user), **When** Run compiles it, **Then** the physics trigger table is sized from the compiled scene. The 9R.2 collider table and the 9R.4 activation slots are the pattern.
- **And** Build never refuses a machine for its trigger count.
- **And, by owner decision of 20 Oct 2026, the physics body table has no limit.** In the owner's words: "the physics body table needs to be infinite why would you restrict it".
  - The body table, and every other fixed-capacity physics or worker table, is sized from the compiled scene at Run. That covers bodies, joints, prismatics, sensors, contact-work rows, electrical rows, motion and occurrence buffers.
  - Memory is the only bound.
  - The story begins with an audit that lists every fixed capacity constant in `engine/gpu`, the Simulation and Animation workers, and the wire. Each one is either removed or justified by a written requirement. "Historical" doesn't count as a justification.
- **And** the per-part Build limit (32 instances) is kept only if a requirement states it. The goal is an "unlimited Free Workshop" (TODO.md). Otherwise it is removed, and the e2e and native benches prove a machine beyond it compiles, steps, Resets exactly and Saves/Loads.
- **And** the fixed cap constant and its wording are deleted.
- **Proof:**
  - red today when a 9th Impact Switch is placed;
  - a bench at the palette maximum of Impact Switches, where the last switch fires its own lamp, Reset is exact and Save/Load works;
  - a mutant that restores a fixed cap fails.
- **And** Tier 1 passes, plus the runner's Tier 2 selection.

### Story 9R.6: Typed Mouth Snapping for Tubes (Reopened Stories 6.9 and 6.10; Story 6.11 Repair)
*Status: Done (21 Oct 2026). Murdoch-13 gave a scoped Pass on `63ba232c` ([review](../implementation-artifacts/review-9r-6-typed-mouth-snapping.md)). His full run passed 209/209, and findings are filed to Story F.9.*

*Depends on: Story 9R.1. Closes report #3: no snapping implementation exists (`git grep -i 'snap\|mouth' HEAD -- engine ui` returns only description strings), yet Stories 6.9, 6.10 and 6.11 and the CAT-051 recipe require typed mouth snapping. Sources:*
- *[CAT-048](../../docs/planning/elements/CAT-048-pipe.md) §4 facts 9–11 and 21, and §5 "Snapping" (0.45 m and 20° thresholds per DESIGN.md);*
- *[CAT-049](../../docs/planning/elements/CAT-049-pipe_bend_45.md) fact 2 (mouths coincide within 1e−4 m);*
- *[CAT-030](../../docs/planning/elements/CAT-030-funnel.md) fact 5 (the outlet snaps; the wide inlet does not).*

*Coordinator decision under owner delegation (CAT-048 open question on the wrong-bore control): the Funnel's wide inlet (radius 1.3) is the wrong-bore control. Rationale: it is a real part the player sees, so the refusal is teachable.*

*Deletes: the click-only placement path for tube parts with no mouth check (`cat-030.test.ts:218` asserts no snap today).*

As a player,  
I want a pipe, bend or funnel outlet to snap onto a matching open mouth when I place it nearby,  
So that I can join tubes into a sealed route without fiddly alignment.

**Acceptance Criteria:**
- **Given** declared mouth sockets (position, outward axis, bore radius) on the Pipe, both bends, the Funnel outlet and the Powered Gate, **When** the player places a tube part with a mouth within 0.45 m and 20° of a free compatible mouth (equal bore, opposing axes), **Then**:
  - the preview shows the snap without mutating any body;
  - on release the mouths coincide within 1e−4 m and face opposite (dot < −0.9999);
  - one Undo removes only the new part.
- **And** these are refused (negative controls), each proven:
  - a mouth 0.72 m away or 40° off;
  - an occupied mouth;
  - a locked part;
  - the Funnel's wide inlet (wrong bore).
- **And** snapping is unavailable while Running or Paused and returns after Reset.
- **And** a ball crosses a straight→45°, a 45°→45° and a straight→90° joint, and a Funnel outlet→pipe joint, continuously, with no snag (positive, per CAT-048 fact 9).
- **And** the joined placement survives Reset (snapshot) and Save/Load.
- **And** the new `tools/e2e/tube-snapping.test.ts`, `cat-049`, `cat-050` and `cat-030` pass 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.7: `joined_pipe` Level Is Playable (Reopened Story 6.10)
*Status: Done (21 Oct 2026). Murdoch-14 gave a scoped Pass in round 2, on `914bbbf5` ([review](../implementation-artifacts/review-9r-7-joined-pipe-level-playable.md)).*
- *The command/response wire, the save and the cadence settings no longer carry a format version (owner), and they reject foreign bytes by structure.*
- *The full run passed 234/234.*
- *Findings are filed to Story F.11.*
- *The remaining format versions belong to 9A.1.*

*Depends on: Story 9R.6. Closes report #4: `joined_pipe` exists only in `content/puzzles.json:7930`. It has no `WorkshopPuzzleId` member (`engine/gpu/WorkshopPuzzle.cs:5`), no driver level in `tools/e2e/workshop-driver.ts`, and no test.*

*Deletes: nothing beyond the gap. The new puzzle id joins the existing closed `WorkshopPuzzleId` enum until Story 9A.26 converts every lesson to content data.*

As a player,  
I want to play the Joined Pipe lesson,  
So that I learn to build a route from straight pipes and bends.

**Acceptance Criteria:**
- **Given** the `joined_pipe` lesson selected from the real puzzle menu, **When** I place its granted pipe and bend through the drawer, snapping them into one route (Story 9R.6), and press Run, **Then** the ball travels the joined route and the goal is solved.
- **And** leaving one joint unsnapped leaves the goal unsolved (negative control).
- **And** the lesson's locked fixtures and inventory match `content/puzzles.json`, and placing more than the granted parts is refused.
- **And** Run/Reset matches the snapshot, and Save/Load preserves the route.
- **And** `tools/e2e/cat-050-joined.test.ts` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.8: `spring_forward` Lesson Chrome Proof (Reopened Story 6.5e)
*Status: Backlog. Merged into C-Bumper+Spring (21 Oct 2026) as its suite-first guard. These ACs stay here and apply there in full.*

*Depends on: Story 9R.1. Closes the 6.5e W row: `spring_forward` is in no e2e suite and not in the driver's `WorkshopLevel` union (`tools/e2e/workshop-driver.ts:310`), although Story 6.5e's AC says it solves identically in Chrome.*

*Deletes: nothing.*

As a player,  
I want the Spring Forward lesson to work as taught,  
So that the Springboard lesson can be completed.

**Acceptance Criteria:**
- **Given** `spring_forward` selected from the real puzzle menu, **When** I place the granted Springboard through the drawer, set its preload with the existing control and press Run, **Then** the ball is launched and the goal is solved.
- **And** zero preload or a misplaced board leaves the goal unsolved (negative control).
- **And** Run/Reset matches the snapshot, and Save/Load preserves the board and its settings.
- **And** `tools/e2e/spring-forward.test.ts` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.9: Ball Detector Forward Pass Lights the Lamp Exactly Once (Story 6.8 Repair)
*Status: Backlog. Merged into C-Lamp+Detector (21 Oct 2026) as a suite-first guard. These ACs stay here and apply there in full.*

*Depends on: Story 9R.1. Closes report #15: `cat-002.test.ts:81-84` scans **all** animation targets, so the detector's own pulse satisfies "lamp lights", and "once" is not counted (`driver.readAnimationSamplesForTarget` exists but is unused). The reverse and glancing checks (`:93`, `:132`) never prove a crossing occurred. The second forward crossing (`:171`) uses Reset rather than an in-run clearance.*

*Deletes: the any-target predicate in `cat-002.test.ts`.*

As a player,  
I want the lamp wired to a Ball Detector to flash once per forward pass,  
So that I can trust it to count.

**Acceptance Criteria:**
- **Given** a Ball Detector wired to a Signal lamp through real UI, **When** a ball passes forward, **Then** the lamp target (and only it) shows exactly one activation, counted with `readAnimationSamplesForTarget`.
- **And** a reverse and a glancing crossing are each proven to have physically crossed the aperture (body positions on both sides) and produce zero lamp activations (negative controls).
- **And** a second forward crossing in the same run, after full clearance, lights the wired counter(target 2) lamp target exactly once, or publishes exactly one more `Passage` record (Count 2); the first-input lamp stays at one. *(Amended by Story 9A.22, coordinator decision 23 Oct 2026, C-Lamp review F1: a first-input lamp cannot show a second activation in one run.)*
- **And** Run/Reset matches the snapshot.
- **And** `cat-002` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.10: System Clock Pulses, Pause and Reset Proven in Chrome (Reopened Story 9.3)
*Status: Backlog. Merged into C-Clock (21 Oct 2026) as its suite-first guard. These ACs stay here and apply there in full.*

*Depends on: Story 9R.1. Closes report #10: Story 9.3's 5-pulse AC has native proof only (`ClockTests.cs:189`). Pause (`cat-017.test.ts:267`) checks a ball, not the clock. Reset (`:278`) checks the blade only. There are no native pause or reset tests.*

*Deletes: the ball-only pause check and the blade-only reset check in `cat-017.test.ts`.*

As a player,  
I want a 1-second Clock to tick exactly five times in five seconds, stop when paused, and restart from zero on Reset,  
So that timed machines behave predictably.

**Acceptance Criteria:**
- **Given** a supplied System Clock (interval 1.0 s, the default) built through real UI, **When** the run advances 5.0 simulated seconds, **Then** exactly 5 clock pulses are observed from committed activation reads, at 1.0 s spacing within one tick.
- **And** pausing for 2.0 s wall time adds no pulse and no phase advance; resuming continues the spacing.
- **And** after Reset and a new Run, the first pulse arrives at 1.0 s (phase 0), and the snapshot matches.
- **And** native `ClockTests` gain pause and reset facts.
- **And** an unsupplied clock emits nothing (control).
- **And** (coordinator decision, 18 Oct 2026) the Clock's pendulum swings, and its face flashes once per pulse. Both are driven from the committed clock phase and pulse occurrences through declared cosmetic bindings, held still while paused, and restored to rest by Reset. They are proven in Chrome by pixel and binding reads at known pulse ticks.
- **And** `cat-017` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Stories 9R.11–9R.17: the AC-named element suites that never existed

These close report #8. Story 6.5d named `cat-001`, `cat-004`, `cat-054` and `cat-066` (also `cat-064` and `cat-003`, which go to Story 12.1 because Story 9A.14 removes Tennis and Balloon from the palette). Story 6.5e named `cat-063`, `cat-035` and `cat-022`. None exists in any commit. Each story adds one suite for one element, following that element's spec §5. Each suite has:
- actual-UI construction;
- verified placed configuration and typed connections;
- the intended behaviour;
- a negative/control;
- exact Run/Reset against the Story 9R.1 snapshot;
- Save/Load.

A suite that finds a behaviour defect stops the story, and the defect is fixed in the same story, under the same rules.

### Story 9R.11: Basketball Suite `cat-001` (Reopened Story 6.5d)
*Status: Backlog. Merged into C-Balls (21 Oct 2026) as its suite-first guard. These ACs stay here and apply there in full.*

*Depends on: Story 9R.1. Source: [CAT-001](../../docs/planning/elements/CAT-001-ball.md) §5. Deletes: nothing (gap only).*

As a player,  
I want the Basketball proven to roll and bounce as declared,  
So that the first part I learn behaves as the lesson says.

**Acceptance Criteria:**
- **Given** a Basketball placed through the real drawer on a Ramp, **When** run, **Then** it rolls down and reaches the Receiver in `first_principles`.
- **And** a ball resting on the flat bench stays at rest with no drift (control).
- **And** Run/Reset matches the snapshot, and Save/Load preserves pose and material.
- **And** `tools/e2e/cat-001.test.ts` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.12: Receiver Suite `cat-004` (Reopened Story 6.5d)
*Status: Backlog. Merged into C-Ramp+Receiver (21 Oct 2026) as a suite-first guard. These ACs stay here and apply there in full.*

*Depends on: Story 9R.1. Source: [CAT-004](../../docs/planning/elements/CAT-004-basket.md) §5. Deletes: nothing (gap only).*

As a player,  
I want the Receiver proven to capture a ball that enters its mouth,  
So that goals are fair.

**Acceptance Criteria:**
- **Given** a Receiver placed through the real drawer, **When** a ball enters its mouth, **Then** it is captured and the goal is solved.
- **And** a ball passing outside the mouth is not captured (negative control).
- **And** Run/Reset matches the snapshot, and Save/Load preserves pose and settings.
- **And** `tools/e2e/cat-004.test.ts` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.13: Ramp Suite `cat-054` (Reopened Story 6.5d)
*Status: Backlog. Merged into C-Ramp+Receiver (21 Oct 2026) as a suite-first guard. These ACs stay here and apply there in full.*

*Depends on: Story 9R.1. Source: [CAT-054](../../docs/planning/elements/CAT-054-ramp.md) §5. Deletes: nothing (gap only).*

As a player,  
I want the Ramp proven to guide a ball and to resize as declared,  
So that I can shape a ball's path.

**Acceptance Criteria:**
- **Given** a Ramp placed and resized through the real resize control, **When** a ball is released on it, **Then** it rolls down, and a longer ramp gives a longer run-out.
- **And** a ball dropped beside the ramp falls straight (control), and an out-of-range resize is refused (negative).
- **And** Run/Reset matches the snapshot, and Save/Load preserves length and width.
- **And** (coordinator decision, 18 Oct 2026) dragging a Ramp resize handle past a declared limit clamps at that limit and keeps the gesture alive, instead of cancelling it. The declared 0.125 m minimum is reachable by hand, and no out-of-range value is ever saved.
- **And** `tools/e2e/cat-054.test.ts` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.14: Wall Suite `cat-066` (Reopened Story 6.5d)
*Status: Backlog. Merged into Story 9A.27 (21 Oct 2026) as its suite-first guard. These ACs stay here and apply there in full. The out-of-range resize clamps, per the resize rule.*

*Depends on: Story 9R.1. Source: [CAT-066](../../docs/planning/elements/CAT-066-wall.md) §5. Deletes: nothing (gap only).*

As a player,  
I want the Wall proven to stop a rolling ball and to resize as declared,  
So that I can block paths reliably.

**Acceptance Criteria:**
- **Given** a Wall placed and resized through the real UI across a ball's path, **When** run, **Then** the ball is stopped or rebounds and never passes through.
- **And** the same path with no Wall lets the ball continue (control), and an out-of-range resize is refused (negative).
- **And** Run/Reset matches the snapshot, and Save/Load preserves dimensions.
- **And** `tools/e2e/cat-066.test.ts` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.15: Impact Switch Suite `cat-063` (Reopened Story 6.5e)
*Status: Backlog. Merged into C-Switch (21 Oct 2026) as its suite-first guard. These ACs stay here and apply there in full. The threshold editor is the generic parameter editor, not a per-element panel.*

*Depends on: Story 9R.1. Source: [CAT-063](../../docs/planning/elements/CAT-063-switch.md) §5 (activation mode only; the electrical mode is Story 9.5b). Deletes: nothing (gap only).*

As a player,  
I want the Impact Switch proven to latch on a qualifying hit,  
So that I can trigger machines with a ball.

**Acceptance Criteria:**
- **Given** an Impact Switch wired to a Signal lamp through the real wiring UI, **When** a ball strikes the button at or above the threshold, **Then** the switch latches once and the lamp lights.
- **And** a below-threshold or missed ball leaves the switch unlatched and the lamp dark (negative control).
- **And** Run/Reset matches the snapshot (button raised), and Save/Load preserves the wire and the threshold.
- **And** (coordinator decision, 18 Oct 2026) the Impact Switch has a player-facing trigger-threshold editor:
  - typed linear speed, range 0–64 m/s, default 0.8 m/s;
  - out-of-range input clamps;
  - the value is used by the compiled trigger, restored by Run/Reset and persisted by Save/Load;
  - proven through the actual UI with below, equal and above threshold drops.
- **And** `tools/e2e/cat-063.test.ts` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.16: Signal Lamp Suite `cat-035` (Reopened Story 6.5e)
*Status: Backlog. Merged into C-Lamp+Detector (21 Oct 2026) as a suite-first guard. These ACs stay here and apply there in full.*

*Depends on: Story 9R.5 (level outputs). Source: [CAT-035](../../docs/planning/elements/CAT-035-lamp.md) §5. Deletes: nothing (gap only).*

As a player,  
I want the Signal lamp proven to light on its input,  
So that I can see that my machine fired.

**Acceptance Criteria:**
- **Given** a Signal lamp wired from an Impact Switch through real UI, **When** the switch fires, **Then** the lamp lights on that first input and stays lit.
- **And** a lamp wired from a State Latch or a Pressure Plate level output follows that level on and off.
- **And** an unwired lamp stays dark (control), and a lamp that follows a level output refuses a second signal (negative).
- **And** Run/Reset matches the snapshot (Slate), and Save/Load preserves wires.
- **And** `tools/e2e/cat-035.test.ts` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.17: Delay Box Suite `cat-022` (Reopened Story 6.5e)
*Status: Backlog. Merged into Story 9.5a (21 Oct 2026) as its suite-first guard. These ACs stay here and apply there in full.*

*Depends on: Story 9R.1. Source: [CAT-022](../../docs/planning/elements/CAT-022-delay.md) §5. Deletes: nothing (gap only).*

As a player,  
I want the Delay Box proven to hold a signal for its set duration,  
So that I can time my machines.

**Acceptance Criteria:**
- **Given** a Delay Box between an Impact Switch and a Signal lamp, wired through real UI with its duration set by the existing Delay control, **When** the switch fires, **Then** the lamp lights after exactly the declared duration, within one tick.
- **And** the lamp does not light before the duration elapses (negative control), and a Delay link to a Powered Gate is refused (control).
- **And** Run/Reset matches the snapshot, and Save/Load preserves the duration and wires.
- **And** `tools/e2e/cat-022.test.ts` passes 100% in Chrome serially.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9R.18: Independent Re-Verification and Dated Review Addenda
*Status: Dropped as a separate story (coordinator, 21 Oct 2026).*
- *Each story that closes a reopened or weak record appends its dated addendum, under the evidence ACs below, as listed in the [reopened-record closure table](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination).*
- *The full serial re-run in the last AC is replaced by the End-to-end scope rule's tiers.*
- *The text is kept as the evidence contract for those addenda.*

*Depends on: Stories 9R.0–9R.17. Closes report #7 and §D: review evidence is not credible.*
- *The raw cat-062 output in review-6-5e, 6-5f, 6-6, 6-7 and 6-8 is presented as fresh runs, yet its first three test timings are identical to the nanosecond (`14729.544167ms`, `11075.602542ms`, `12073.702125ms`).*
- *review-9-1 links nine source files that don't exist, and review-9-3 links four.*
- *review-9-2b marked contradicted ACs as PASS (G8).*
- *review-6-9 reports 851/851 despite 12 new tests; review-6-5b claims 12 parts where its test asserts 24; review-6-9b quotes a grep that doesn't match the file; review-6-6 and review-6-10 claim a bit-for-bit Reset that no assertion checks.*

*Also records report #20 (done stories with open clauses).*

*Deletes: no historical text. Review records are never rewritten.*

As the owner,  
I want every reopened or weak Pass re-checked on fresh, real evidence by someone who did not produce it,  
So that `done` means proven.

**Acceptance Criteria:**
- **Given** each story whose Pass was invalid (6.5b, 6.5c, 6.5d, 6.5e, 6.5f, 6.9, 6.9b, 6.10, 8.1, 9.1, 9.2, 9.2b, 9.3) or weak (6.6, 6.7, 6.8, 6.11, 8.2), **When** its repair story has landed, **Then** a reviewer who is neither its implementer nor its original reviewer independently re-runs its acceptance against the current tree.
  - Raw logs come from that run and are saved as files referenced by path.
  - Each log carries the commit, the bundle identity and the run's own timestamps.
  - Any output quoted in the record matches its file exactly.
  - Every file cited in the record exists.
- **And** each `review-<story>.md` receives a dated addendum ("Addendum 18 Oct 2026+, retro-verification"). The addendum:
  - lists each false or unsupported claim from the report's §D and the per-story tables;
  - gives the corrected result with fresh evidence;
  - states the new verdict.
  
  The original text is left unchanged (negative control: `git diff` of each record shows additions only).
- **And** where an AC is owned by a later story, the addendum names that story and the original stays `in-progress` until that story's terminal review appends a closing addendum:
  - **6.5b:** palette from discovery → 9A.14; extensible `PartId` → 9A.15.
  - **6.5c:** colliders → 9A.16; locators → 9A.17.
  - **6.5d, 6.5e:** fallbacks → 9A.16.
  - **6.5f:** switch tables → 9A.16, 9A.17, 9A.25, 9A.27–9A.43.
  - **6.9b:** `AbiOffsets` → 9A.1.
  - **9.2:** decode default → 9A.2.
  - **9.2b:** → 9A.0 and Epic 9A.
- **And** Story 8.1 is re-verified against its amended acceptance: Powered Gate and Bumper store consumers, with the Motor clause moved to Story 11.1 by owner decision on 18 Oct 2026.
- **And** each done story with an open clause (6.9 and 6.10 nudging, 6.11 integrations, 8.2 CAT-051 Q6) is recorded with its owning later story.
- **And** the re-runs include every story's AC-named suite plus one full serial Chrome run of every `tools/e2e/*.test.ts` suite against the production preview, and `dotnet build` (Story 9R.0 gate) plus `dotnet test`, all passing 100%.
- **And** `sprint-status.yaml` marks each reopened story `done` only when its addendum records a fresh terminal Pass.

### Visual repair stories 9R.19–9R.22 (owner report and coordinator decision, 19 Oct 2026)

*Origin: the owner reported that the Funnel's glass wall looks inverted and said there are likely other issues. The [visual review of 19 Oct 2026](visual-review-2026-10-19/static-findings.md) covered every Free palette part, in static analysis and in Chrome captures.*

*Delivery order: these run after Story 9R.17 and before Story 9R.18, so 9R.18 re-verifies Stories 6.6, 6.9, 6.10 and 6.11 against art that is now correct. They also precede 9A.8–9A.11 and 9A.25, which move scenes and bindings with exact parity and would otherwise carry these defects into the packages. Coordinator decision under owner delegation: DESIGN.md already requires every item here, so none of it is a new design choice.*

*Superseded on 21 Oct 2026 by the [Delivery order](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination), with the owner's amendment that polish and art follow all deletion work:*
- *Story 9R.19 is at position 56, 9R.21 at 58 and 9R.22 at 60. They now edit the scenes inside the `elements/` packages, which PKG-1, PKG-2 and PKG-3 moved there with exact parity.*
- *Story 9R.20 merges into 9A.25a, so the golden test there captures live cues, not dead ones.*
- *Story 9R.18 is dropped.*

*Review depth: 9R.19, 9R.21 and 9R.22 are art and data, so Murdoch reviews them alone. 9R.20 changes presentation bindings, so it also gets the three lenses. E2e uses Tier 2 with the affected element suites, plus Chrome screenshots that use actual UI controls.*

### Story 9R.23: Tube Behaviour Clause Proofs (Closes Stories 6.6, 6.7, 6.9, 6.10 and 6.11)
*Status: Backlog. Runs immediately before Story 9R.19. Coordinator decision, 21 Oct 2026: the PKG-2 review (F15-8) found that each tube story still has clauses that no repair touched, so the re-verification addenda came out Incomplete.*

**Acceptance Criteria:**
- **Given** each clause the five PKG-2 addenda list as unproven, **Then** it gets a native production-simulation test with a control, or it is fixed; the Chrome proof is queued for the post-catalogue E2E phase. The addenda list these clauses: the oversized-ball stop, the entry speed and angle bounds, the rim and sidewall controls, and the parity at non-default pipe lengths (F15-9).
- **And** the clauses already proven natively are cited, not re-proven: Story 6.7's entry speed and edge angles by `FastAxialLaunchPassesOnlyTheBoreOrClearAir` and `ActualPackageWallsPassAFittingPayloadAndStopAnOversizedPayload`; the 1,200-tick bound of the earlier addendum is not carried. The residual tests this story adds are the planned rows of each CAT spec's section 5:
  - CAT-030 (Story 6.11): `WorkshopPhysicsLawHollowTests.AFastAxialLaunchCrossesTheRotatedFunnelPackage`.
  - CAT-048 (Stories 6.6 and 6.7): `TubeMouthSnapTests.ASnapIsRefusedWhileRunningOrPausedAndReturnsAfterReset` and `WorkshopPipeLengthTests.AResizeIsRefusedWhileRunningOrPausedWithoutChangingTheConstruction`.
  - CAT-049 (Story 6.9): `TubeMouthSnapTests.AnUnsnappedBend45DoesNotReceiveTheBall`.
  - CAT-050 (Story 6.10): a `Bend45ToBend90` case of `TubeMouthSnapTests.ABallCrossesASnappedJointWithoutSnagging` and a `Bend90` case of `FanGuidanceTests.TransparentCurvedAndTaperedSolidWallsOccludeOnlyWhenInTheStream`.
- **And** the story waits for the engine programme (owner decisions 3a and 22: tests-only patches run after it).
- **And** the pipe and funnel colliders match their art (F15-7). Since the owner's "true hollow shapes" decision (25 Oct 2026), [Story engine-hollow-shapes](#story-engine-hollow-shapes) replaces their box approximations with true hollow kinds; this story's behaviour proofs run on those kinds, and no box collider is re-authored.
- **And** each of Stories 6.6, 6.7, 6.9, 6.10 and 6.11 gets a dated Pass addendum, and `sprint-status.yaml` marks it done.
- **Excluded:** the art clauses belong to 9R.19, nudging to 15.0, air to 12.2 and light to ENGINE-OPTICAL.

### Story 9R.19: Tube Glass Art Repair (Funnel, Clear Pipe, Pipe Bends, Gate and Detector Collars)
*Status: Backlog*

*Repairs the art acceptance of Stories 6.6, 6.9, 6.10 and 6.11 (visual review #1–#4 and #10).*

**Acceptance Criteria:**
- **Given** the Funnel, **When** it is placed, **Then** its glass narrows from the wide 1.3 inlet collar to the 0.65 outlet collar (delivered: 64 segments, wide end at the inlet), and a native test checks that the visual and collider radii agree at both ends.
- **And** every open glass mesh (funnel, pipe, `pipe_bend_45`, `pipe_bend_90`, the powered-gate collars, the ball-detector collar) is double-sided (`cull_mode = 2`), so the far wall and inner faces render.
  - The gate and detector collars get real wall thickness (a torus or walled tube) and no longer render as "C" half-arcs.
  - Transparency sorting where a funnel outlet meets a pipe is checked in Chrome from outside and inside, in the post-catalogue E2E phase.
- **And** the pipe, both bends and the funnel carry the navy rails that DESIGN.md:353, :357 and :358 require: navy `#293954` cylinders of radius 0.018 m centred 0.675 m from the bore axis, wholly inside the 0.65 to 0.70 m glass wall, so they are enclosed and decorative (flush within the wall satisfies visible = solid).
- **And** the bends use a swept torus-segment mesh, with no seam bands.
- **And** the Chrome screenshots, taken through actual UI placement, show each part from the front, from 45° and from above, and are kept as evidence. Their suites (cat-030, cat-048a/b, cat-049, cat-050, cat-062b and cat-002) pass.

### Story 9R.20: Live State Cues During Run (Clock Lamp, Counter Dots, Gate Indicator, Pressure Plate Studs)
*Status: Backlog. Merged into Story 9A.25a (21 Oct 2026). These ACs stay here and apply there in full. The cues are delivered as declared committed-state bindings, not as `MachinePart` lamp methods.*

*Visual review #6. Today `UpdateClockLamp` and `UpdateCounterLamps` are called only from tests, and in production only with false or 0 (`MachinePart.cs:220-221`). The gate's `Visual_Indicator` and the plate's `Stud_*` have no binding. Must land before Story 9A.25, whose golden test would otherwise lock in the dead cues.*

**Acceptance Criteria:**
- **Given** a running machine, **When** the System Clock pulses, the Pulse Counter counts, the Powered Gate is driven or the Pressure Plate is loaded, **Then** the clock lamp flashes, the counter dots show the count, the gate indicator shows its drive state and the plate studs show its activation, all driven from the read model on the animation path (no new per-element update loop).
- **And** Reset restores every cue exactly to its pre-Run state, and Save/Load preserves the authored state.
- **And** Chrome screenshots during Run prove each cue changes, with a negative control: an unpowered gate and an unloaded plate show no change. A mutant that removes the binding fails a test.

### Story 9R.21: Lit Palette Fidelity
*Status: Backlog*

*Visual review #5. Lit ramps and walls look pale yellow, the receiver looks mint and the deck looks near-white, but DESIGN.md requires the approved colours to survive lighting. The cause is unverified: light energy, or a double sRGB conversion in `gl_compatibility`.*

**Acceptance Criteria:**
- **Given** the approved palette, **When** the Workshop renders each part under the DESIGN.md lighting in Chrome, **Then** each part's lit base colour, sampled in the shaded mid-tone region, stays within ΔE ≤ 10 of its approved colour. The root cause is identified and fixed once, with no per-part colour compensation.
- **And** a Chrome sampling test that reads pixels from known part regions fails if the colour drifts beyond that bound.

### Story 9R.22: Element Art Polish (Bumper, Counter, Detector, Sockets, Delay, Switch, Spring, Wall)
*Status: Backlog*

*Visual review #7–#10.*

**Acceptance Criteria:**
- **Bumper:** the slate pedestal `Visual_Base` is visible below the head.
- **Counter:** `Mat_Socket` is gold, the `ActivationOut` socket is visible and not buried in the foot, and the side sockets sit on the housing.
- **Ball Detector:** the chevron points in the forward-crossing direction (+X), matching the palette icon.
- **Sockets:** every part's socket uses the one approved socket gold.
- **Delay Box:** it shows its twelve marks.
- **Impact Switch:** the palette icon shows a push-button that matches the model.
- **Springboard:** the coil is a helix attached to the base and plate.
- **Wall:** the stripes are offset enough not to z-fight.
- **And** Chrome screenshots of each part, through actual UI placement, are kept, and each part's suite passes.

---

## Epic 9A: Sole Architecture Conformance (Project-per-Element, Command/Query, Event Disruptor)

**Goal:** Retire every parallel path so that the only design available in the tree is: one project per puzzle element, the bundle built from those projects as the single declaration source, typed commands and queries, and the event ring for discrete occurrences. After this epic, element-specific code outside `elements/` fails the build.

**Delivery order (owner decision, 18 Oct 2026; Open scheduling item g).** Epic 9A runs after Stories 9.4 and 9.4b (and Story 9.4c, which the coordinator placed first) and before Story 9.5 and every later new element. Its stories run in number order, 9A.0 to 9A.45. The section is placed after Epic 9 so that Epic 9's stories stay together; `sprint-status.yaml` and each "Depends on" line carry the delivery order.

*Superseded on 21 Oct 2026 by the [Delivery order](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination).*
- *Only the critical path (9A.0, 9A.1, 9A.4, PKG-1–3, 9A.14, 9A.2, 9A.15, 9A.17, 9A.3, 9A.16, 9A.25a, 9A.27, 9A.45a, 9.5a) precedes Story 9.5.*
- *The remaining conversions and ring stories alternate with new elements, and the final 9A.45 is at position 54.*
- *"Depends on" lines that only recorded number order are corrected in place.*

**Evidence.** [Architecture conformance audit, 18 Oct 2026](architecture-conformance-audit-2026-10-18.md) (finding IDs A1–A19, B1–B8, C1–C5, D1–D4, G1–G10). Its line numbers are a snapshot of a tree that Story 9.4 was still editing, so every story names the symbol as well as the line. Where they disagree, the symbol wins.

**Rules for every 9A story.**
- The story names the legacy it deletes and deletes it in the same slice.
- No compatibility shim, fallback branch, alias, dual loader or automatic migration is permitted. Formats carry no version (owner, 21 Oct 2026); structurally invalid save or wire input is rejected explicitly.
- Every existing element keeps working.
- Any lane the story rewrites lands as IEEE-754 f32; no story adds a `Half`.
- Review-9-4 F6 (audit A8: the per-tick `Program.SwitchedSupplies`/`ElectricalState` walk of the authoring graph, `SwitchedSupplyRoutes` in `engine/gpu/WorkshopLatch.cs`) is fixed inside Story 9.4 (owner decision, 18 Oct 2026), so no 9A story owns it. If any part of it remains when 9A starts, Story 9A.19 deletes that remainder.
- Epic 9R runs first and fixes the player-visible defects with compiled Run-time tables. Each 9A story converts those tables to declarations and does not re-implement the fixes.
- Where a 9A story closes an acceptance clause reopened by the 18 Oct 2026 retro-verification (the mapping is in Story 9R.18), its terminal review appends a dated closing addendum to the original story's review record, and `sprint-status.yaml` then marks that story `done`.
- **Owner amendment, 21 Oct 2026: every story deletes, in the same story, the old code its new path supersedes.** No mixed old and new path survives the story that introduces the new one. Each story's ACs include that an active-tree `rg` for every symbol in its "Deletes:" list returns 0 production hits, and that its Story 9A.45a known-violations entries are gone.

### Merged delivery stories (coordinator, 21 Oct 2026)

These stories implement the [Delivery order](#delivery-order-coordinator-21-oct-2026-owner-authorised-re-examination).
- Each one merges existing stories, whose ACs stay where they are, are marked "merged into X", and apply in full.
- A merged story is one slice: one implementer, one independent reviewer and one frozen candidate. Its impact-selected suites run once on that candidate.
- A family's 9R suite story is its **suite-first guard**: the suite lands green on the old record path in the first commit, and must stay green after the conversion.
- **Status rule.** When a merged story goes `done`, its merged originals go `done` in the same `sprint-status.yaml` update. A split original (9.7, 9.8, 9A.25) goes `done` when all of its splits are done. Story selection skips keys marked "merged into", "split into" or "dropped".
- The positions are those of the Delivery order.

### Story PKG-1: Receiver, Bowling Ball and Springboard Element Packages (9A.5 + 9A.6 + 9A.12)
*Status: Done (21 Oct 2026). Murdoch-18 gave a scoped Pass on `8333682f` ([review](../implementation-artifacts/review-9a-pkg-1.md); [spec](../implementation-artifacts/spec-9a-pkg-1-receiver-bowling-springboard-packages.md)). Position 6. Findings are filed to Story F.15.*

*Depends on: Story 9A.4.*

*Deletes: the union of the merged Deletes lists:*
- *`parts/scenes/basket.tscn`, `parts/scenes/bowling.tscn` and `parts/scenes/springboard.tscn`, moved (not copied) into `elements/Receiver/`, `elements/BowlingBall/` and `elements/Springboard/`;*
- *`basket`, `bowling` and `spring` from the parity guard's unpackaged set.*

**Acceptance Criteria:**
- **Given** the three packages, **When** they are added, **Then** every AC of Stories 9A.5, 9A.6 and 9A.12 holds.
- **And** `git ls-files parts/scenes/basket.tscn parts/scenes/bowling.tscn parts/scenes/springboard.tscn` returns nothing, and the parity guard's unpackaged set no longer contains the three ids.
- **And** the union of the three stories' named suites passes once on the merged candidate.

### Story PKG-2: Funnel, Clear Pipe and Pipe Bend Element Packages (9A.8 + 9A.9 + 9A.10 + 9A.11)
*Status: Review (21 Oct 2026; [spec](../implementation-artifacts/spec-9a-pkg-2-funnel-pipe-bend-packages.md)). Position 7.*

*Depends on: Story 9A.4.*

*Deletes:*
- *`parts/scenes/funnel.tscn`, `pipe.tscn`, `pipe_bend_45.tscn` and `pipe_bend_90.tscn`, moved into `elements/Funnel/`, `elements/Pipe/`, `elements/PipeBend45/` and `elements/PipeBend90/`;*
- *the four ids from the parity guard's unpackaged set.*

**Acceptance Criteria:**
- **Given** the four packages, **When** they are added, **Then** every AC of Stories 9A.8–9A.11 holds, including bit-for-bit collider parity.
- **And** `git ls-files` returns nothing for the four moved scenes, and the four ids are gone from the unpackaged set.
- **And** `cat-030`, `cat-048a`, `cat-048b`, `cat-049`, `cat-050`, `tube-snapping` and `cat-050-joined` pass on the merged candidate.
- **And** the review appends the dated addenda that re-verify Stories 6.6, 6.7, 6.9, 6.10 and 6.11 (reopened-record closure).

### Story PKG-3: Delay Box and Impact Switch Element Packages (9A.7 + 9A.13)
*Status: Review (21 Oct 2026; [spec](../implementation-artifacts/spec-9a-pkg-3-delay-switch-packages.md)). Position 8.*

*Depends on: Story 9A.4.*

*Deletes:*
- *`parts/scenes/delay.tscn` and `parts/scenes/switch.tscn`, moved into `elements/DelayBox/` and `elements/ImpactSwitch/`;*
- *`delay` and `switch` from the parity guard's unpackaged set.*

**Acceptance Criteria:**
- **Given** the two packages, **When** they are added, **Then** every AC of Stories 9A.7 and 9A.13 holds.
- **And** `git ls-files` returns nothing for the two moved scenes, and the parity guard's unpackaged set is then exactly `tennis` and `balloon`.
- **And** the union of both stories' named suites passes once on the merged candidate.

### Story 9A.25a: Presentation Bindings Ship in Packages, with Live State Cues (9A.25 committed-state part + 9R.20)
*Status: Backlog. Position 15.*

*Depends on: Story 9A.17. Story 9A.25's former dependency on 9A.23 applies only to the one-shot drivers, which are Story 9A.25b.*

*Deletes, all of Story 9A.25's list:*
- *every per-id branch of `MachinePart.BuildDeclarative` (the 24 `PartId ==` arms, `engine/MachinePart.cs:237-530`);*
- *`ResizableAxes` by id;*
- *the Clock and Counter lamp methods (`UpdateClockLamp` and `UpdateCounterLamps`, the methods Story 9R.20 found dead);*
- *`CosmeticCurves.ImpactSwitch/SignalLamp/Delay/PinballBumper/Receiver/BallDetector/StateLatch`;*
- *the element-named `AnimationFeedbackSource.Memory`.*

*Every binding, one-shot curves included, is declared in its package here. The one-shot bindings keep their current driver, the main-thread relay, until Story 9A.25b replaces that driver with ring kinds.*

**Acceptance Criteria:**
- **Given** each package's declared bindings, **When** a part is built, **Then** every Story 9A.25 AC holds for all bindings: `MachinePart` applies them with no element branch, the golden sampled properties are equal, and the Chrome motion screenshots match.
- **And** every Story 9R.20 AC holds. The clock lamp, counter dots, gate indicator and plate studs are declared committed-state bindings, live during Run, restored by Reset, and each is proven by a binding-removal mutant.
- **And** the golden test is captured with the 9R.20 cues live, so it never locks in a dead cue.
- **And** `rg "PartId\s*==|UpdateClockLamp|UpdateCounterLamps" engine/MachinePart.cs` returns 0 hits, resizable axes come only from the declaration, and `rg "CosmeticCurves\.(ImpactSwitch|SignalLamp|Delay|PinballBumper|Receiver|BallDetector|StateLatch)|AnimationFeedbackSource\.Memory"` returns 0 production hits.
- **And** `anim-1a`, `anim-1b`, `anim-1c`, `cat-017`, `cat-020`, `cat-051` and `cat-052` pass.

### Story 9A.23 + 9A.25b: Event Ring Consumers and One-Shot Presentation Bindings
*Status: Backlog. Position 48. Delivered as one slice; Story 9A.23's ACs stay in place.*

*Depends on: Stories 9A.22 and 9A.25a. It precedes Epic 14 and any new one-shot cue.*

*Deletes:*
- *the whole of Story 9A.23's list, including the main-thread `Transition` relay and the one-shot pumps in `PumpCosmetics`;*
- *the relay driver of every declared one-shot binding (Bumper impact pulse, Receiver capture, switch and latch one-shots), which becomes a ring-kind driver.*

*Scope notes (24 Oct 2026, [spec](../implementation-artifacts/spec-9a-23-25b-ring-consumers.md)):*
- *"Latch one-shots" is vacuous. The State Latch's memory is a level (invariant 4), and its 0.125 s transition is already the 9A.25a committed binding, so it has no ring-kind driver.*
- *The Ball detector's cue is a ring-kind binding of kind `Passage`, on every crossing (CAT-002 §2).*
- *The main-thread cursor's production consumer is the goal-cue wake (coordinator decision). The 9A.24 port reuses the same reader.*

**Acceptance Criteria:**
- **Given** the event ring, **When** a run starts, **Then** every Story 9A.23 AC holds.
- **And** every declared one-shot binding is driven only by its ring kind. The golden sampled properties at fixed curve times equal the 9A.25a values, and the Chrome motion screenshots match.
- **And** `rg "occurrenceReads|reserveOccurrenceRead|releaseOccurrenceRead|retainOccurrence|createEventConsumer|readEventRing"` returns 0 production hits, and no one-shot binding names a relay source.

### Story 9A.45a: Early Element Isolation Ratchet
*Status: Backlog. Position 17.*

*Depends on: Stories 9A.14, 9A.15, 9A.17 and 9A.27. Split from Story 9A.45, whose final empty-list gate stays at position 54.*

*Deletes: no production code. It is the forcing function that makes every later deletion permanent. Each entry it records names the story that deletes it.*

As the owner,  
I want the build to refuse any new element-specific code outside `elements/` from now on, and to refuse old code that outlives its owner,  
So that every new element from Story 9.5 on is declaration-only and every conversion actually deletes what it replaces.

**Acceptance Criteria:**
- **Given** the solution, **When** `dotnet test` runs, **Then** an isolation scan fails, citing file and line, for any hit not in a checked-in known-violations list.
  - The scan uses Story 9A.45's rules and scope: a bundle element id as a string literal; a `PartId.<Name>` member access; an `is`, `OfType<>` or `switch` arm on a `Workshop<Package>` record or on a `PartId`; and an identifier built from a package name.
  - The id list comes from the generated bundle.
- **And** each known-violations entry names its file, its symbol and its owning story's `sprint-status.yaml` key. The baseline includes, each tagged with its owning conversion:
  - every remaining `MachineWorld.CaptureInstance` id arm;
  - the per-element panels in `ui/WorkshopConfiguration.cs` (Springboard, Battery, Bumper, Clock, Delay);
  - every record-type arm in `WorkshopPhysicsCompiler`, `ActivationNetwork.Compile` and `WorkshopInstances.Validate`.
- **And** the list may only shrink:
  - adding an entry fails;
  - widening an entry's hit count fails;
  - an entry that no longer matches any hit fails until it is removed.
- **And** (owner amendment, 21 Oct 2026) the test fails when any entry remains while its owning story's `sprint-status.yaml` key is `done`.
- **And** the negative controls each fail the test: a planted element-id literal; a planted `is Workshop<Package>` arm; an added entry; and a fixture status file that marks an owning story `done` while its entry remains. The unchanged tree passes (positive control).
- **And** the Story 9.4b element-scan ratchet's known-violations list follows the same may-only-shrink rule until Story 9A.19 empties it.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings and `dotnet test CuriousContraptions.slnx` passes 100% (native proof; no runtime change).

### Story C-Switch: Impact Switch Generic Instance with Threshold Editor (9A.35 + 9R.15)
*Status: Backlog. Position 20, before Story 9.5b.*

*Depends on: Stories 9A.27 (generic editor), PKG-3 (switch package) and 9.5a (declared switched contact).*

*Deletes:*
- *the whole of Story 9A.35's list: `WorkshopSwitch`, the element-specific use of `ContactTriggerSettings`, `WorkshopInput.Switch`, the `is WorkshopSwitch` arms in the compiler and in `ActivationNetwork.Compile`, and `WorkshopSwitch` in the whitelist;*
- *the `"switch"` arm of `CaptureInstance`;*
- *the `WorkshopSwitch` construction in `engine/gpu/DelayedSignal.cs`, which is edited to build the generic instance;*
- *this story's 9A.45a entries.*

**Acceptance Criteria:**
- **Suite first:** Story 9R.15's `cat-063` suite lands green on the record path and stays green after the conversion. Every Story 9R.15 AC applies, and the trigger-threshold editor is the generic parameter editor rendering the declared threshold (0–64 m/s, default 0.8; out-of-range input clamps).
- **And** every Story 9A.35 AC holds: the generic contact-trigger compile, golden rows, and Run/Reset and Save/Load exact.
- **And** `rg -w WorkshopSwitch` returns 0 production hits, and the 9A.45a ratchet passes with this story's entries removed.
- **And** the review appends the addendum for Story 6.5e's 9R.15 part.

### Story C-Ramp+Receiver: Ramp and Receiver Generic Instances (9A.28 + 9A.29 + 9R.12 + 9R.13)
*Status: Backlog. Position 24.*

*Depends on: Stories 9A.27 and 9A.26 (lessons as data, so no lesson file builds these records).*

*Deletes:*
- *the union of the Stories 9A.28 and 9A.29 lists: `WorkshopRamp`, `RampDimensions`, `WorkshopInput.Ramp`, `WorkshopReceiver`, `ReceiverCaptureSettings`, `ReceiverForceRegion`, `CaptureReceiver`, the `WorkshopConstruction.Receiver` accessor, the compiler arms and the whitelist entries;*
- *the `"ramp"` and `"basket"` arms of `CaptureInstance` and its `rampDimensions` parameter;*
- *this family's 9A.45a entries.*

**Acceptance Criteria:**
- **Suite first:** `cat-004` (9R.12) and `cat-054` (9R.13) land green on the record path and stay green after the conversion. Every 9R.12 and 9R.13 AC applies, including the Ramp's clamp-at-limit resize.
- **And** every Story 9A.28 and 9A.29 AC holds.
- **And** `rg -w "WorkshopRamp|RampDimensions|WorkshopReceiver|ReceiverCaptureSettings|ReceiverForceRegion|CaptureReceiver|rampDimensions"` returns 0 production hits, and the ratchet passes.
- **And** the review appends the addenda for Story 6.5d's 9R.12 and 9R.13 parts.

### Story C-Balls: Basketball and Bowling Ball Generic Instances (9A.30 + 9A.31 + 9R.11)
*Status: Backlog. Position 26. It precedes Domino and C-Bumper+Spring, because it adds the generic dynamic-body compile.*

*Depends on: Stories 9A.27 and 9A.26 (no lesson file builds a ball record).*

*Deletes:*
- *the union of the Stories 9A.30 and 9A.31 lists: `WorkshopBall`, `BallMaterial`-by-id, `CaptureBall`, the `WorkshopConstruction.Ball` accessor, `OfType<WorkshopBall>` and the whitelist entry;*
- *the `"ball" or "bowling"` arm of `CaptureInstance`;*
- *this family's 9A.45a entries.*

**Acceptance Criteria:**
- **Suite first:** `cat-001` (9R.11) lands green on the record path and stays green after the conversion. Every 9R.11 AC applies.
- **And** every Story 9A.30 and 9A.31 AC holds: golden rows, and each ball keeps its own material on one bench.
- **And** `rg -w "WorkshopBall|CaptureBall"` returns 0 production hits, and the ratchet passes.
- **And** the review appends the addendum for Story 6.5d's 9R.11 part.

### Story C-Bumper+Spring: Pinball Bumper and Springboard Generic Instances (9A.33 + 9A.34 + 9R.8)
*Status: Backlog. Position 28.*

*Depends on: Stories C-Balls and 9A.26.*

*Deletes:*
- *the union of the Stories 9A.33 and 9A.34 lists: `WorkshopBumper`, `BumperWork`, `WorkshopInput.Bumper`, the Bumper panel, `WorkshopSpringboard`, `SpringboardSettings`, `WorkshopInput.Springboard`, `OfType<WorkshopSpringboard>`, the Springboard panel and the whitelist entries;*
- *the `"bumper"` and `"spring"` arms of `CaptureInstance`;*
- *this family's 9A.45a entries.*

**Acceptance Criteria:**
- **Suite first:** `spring-forward` (9R.8) lands green on the record path and stays green after the conversion. Every 9R.8 AC applies, with the preload set through the generic editor.
- **And** every Story 9A.33 and 9A.34 AC holds.
- **And** `rg -w "WorkshopBumper|BumperWork|WorkshopSpringboard|SpringboardSettings|AddBumperConfiguration|AddSpringboardConfiguration"` returns 0 production hits, and the ratchet passes.
- **And** the review appends the addendum for Story 6.5e's 9R.8 part.

### Story C-Gate: Declared Prismatic Drive and Declaration-Free Warm-Up (9A.19 + 9A.21)
*Status: Backlog. Position 32, after Story 9.4c and before Story 11.3.*

*Depends on: Stories 9A.17, 9.4c (the `cat-005-n19` and `cat-051-interrupt` guards), 9R.3 and 9R.4.*

*Deletes:*
- *the union of the Stories 9A.19 and 9A.21 lists: gate detection by joint limit, the drag band and `ActuatorSliderDrive` heuristics, the record-type and `PartId.PoweredGate` selection of the drive, `SimJoint.SwitchedSupplyClosed` if it is unread, the element warm-up scene in `CuriousContraptions.Simulation/Program.cs` and the dummy joint in `WorkshopPhysicsEngine.cs`;*
- *this family's 9A.45a entries.*

*Coordinator decision (22 Oct 2026, from Story 9.4c review F1):* C-Gate also fixes ActuatorDriveRead.Blocked residual (blade resting on ball reads −1.083e-5 m/s vs 1e-5 threshold, indicator shows unpowered); native proof required; see spec-9-4c D-notes. The threshold itself is CAT-051 open question 3 (owner).

**Acceptance Criteria:**
- **Given** the Powered Gate package, **When** it is compiled, **Then** every Story 9A.19 and 9A.21 AC holds. That includes the empty Story 9.4b element-scan known-violations list and the owner-directed removal of synthetic startup work.
- **And** `rg -w "ActuatorSliderDrive|SwitchedSupplyClosed"` and `rg "PartId\.PoweredGate"` return 0 production hits, and no synthetic startup workload remains.
- **And** the review appends the addendum that re-verifies Story 8.2.

### Story C-Lamp+Detector: Signal Lamp and Ball Detector Generic Instances (9A.36 + 9A.37 + 9R.9 + 9R.16)
*Status: Backlog. Position 36.*

*Depends on: Stories C-Switch and 9A.26.*

*Deletes:*
- *the union of the Stories 9A.36 and 9A.37 lists: `WorkshopLamp`, `WorkshopInput.Lamp`, `WorkshopBallDetector`, `WorkshopInput.BallDetector`, its registry and collider lookup, the `ActivationNetwork.Compile` arms and the whitelist entries;*
- *the `"lamp"` and `"ball_detector"` arms of `CaptureInstance`;*
- *the any-target predicate in `cat-002.test.ts` (from 9R.9);*
- *this family's 9A.45a entries.*

**Acceptance Criteria:**
- **Suite first:** `cat-002` (9R.9) and `cat-035` (9R.16) land green on the record path and stay green after the conversion. Every 9R.9 and 9R.16 AC applies.
- **And** every Story 9A.36 and 9A.37 AC holds.
- **And** `rg -w "WorkshopLamp|WorkshopBallDetector"` returns 0 production hits, and the ratchet passes.
- **And** the review appends the addenda for Story 6.8 and for Story 6.5e's 9R.16 part.

### Story C-Plate: Declared Contact-Load Sensor and Pressure Plate Generic Instance (9A.20 + 9A.40 + F.6 item F-1)
*Status: Backlog. Position 40.*

*Depends on: Stories C-Gate and 9R.4/9R.5.*

*Deletes:*
- *the union of the Stories 9A.20 and 9A.40 lists: plate detection by half-extents, `_plateLoaded`, the 0.5 kg literal, the plate drag band, the `OfType<WorkshopPressurePlate>` emission loop (`WorkshopPhysicsCompiler.cs:224`), `WorkshopPressurePlate`, `WorkshopInput.PressurePlate` and the whitelist entry;*
- *the `"pressure_plate"` arm of `CaptureInstance`;*
- *this family's 9A.45a entries.*

**Acceptance Criteria:**
- **Given** the Pressure Plate package, **When** a plate is compiled and edited, **Then** every Story 9A.20 and 9A.40 AC holds.
- **And** Story F.6 item F-1 holds: a domino placed from the palette onto a plate rests on top, not straddling it, so two dominoes open the gate. It is proven in Chrome through the real drawer. F.6 keeps its other items.
- **And** `rg -w "WorkshopPressurePlate|_plateLoaded"` returns 0 production hits, and the ratchet passes.
- **And** the review appends the addendum that re-verifies Story 9.1.

### Story C-Clock: System Clock Generic Instance with Proven Pulses (9A.42 + 9R.10)
*Status: Backlog. Position 42.*

*Depends on: Stories 9.5a (generic timer) and 9A.25a (declared bindings for the pendulum and flash).*

*Deletes:*
- *the whole of Story 9A.42's list: `WorkshopClock`, `DefaultIntervalSeconds`, `WorkshopInput.Clock`, the Clock panel, the `OfType<WorkshopClock>` loop (`WorkshopPhysicsCompiler.cs:258`), the clock arm in `ActivationNetwork.Compile` and the whitelist entry;*
- *the `"clock"` arm of `CaptureInstance`;*
- *the ball-only pause check and the blade-only reset check in `cat-017.test.ts` (from 9R.10);*
- *this story's 9A.45a entries.*

**Acceptance Criteria:**
- **Suite first:** `cat-017`, with Story 9R.10's assertions, lands green on the record path and stays green after the conversion. Every 9R.10 AC applies, including the pendulum and the per-pulse flash.
- **And** every Story 9A.42 AC holds.
- **And** `rg -w "WorkshopClock|DefaultIntervalSeconds|AddClockConfiguration"` returns 0 production hits, and the ratchet passes.
- **And** the review appends the addendum that re-verifies Story 9.3.

### Story C-Memory: Pulse Counter and State Latch Generic Instances (9A.41 + 9A.43)
*Status: Backlog. Position 46, the last record conversion.*

*Depends on: Story 9A.22 (the `ThresholdReached` and `MemoryChanged` kinds) and every other conversion (9.5a, C-Switch, C-Ramp+Receiver, C-Balls, C-Bumper+Spring, C-Gate, 9A.39, C-Lamp+Detector, 9A.32, C-Plate, C-Clock).*

*Deletes:*
- *the union of the Stories 9A.41 and 9A.43 lists: `WorkshopCounter`, `DefaultTargetCount`, `WorkshopLatch`, their `WorkshopInput` factories, the latch-only Latched→Clear admission, the `OfType<WorkshopCounter>` and `OfType<WorkshopLatch>` loops (`WorkshopPhysicsCompiler.cs:244`, `:272`), the `ActivationNetwork.Compile` arms, and the whole `WorkshopInstances.Validate` type whitelist;*
- *the `"counter"` and `"latch"` arms of `CaptureInstance`, which leaves no id arm except the straight pipe's `"pipe"` (owned by 9A.45; coordinator decision, 23 Oct 2026);*
- *this family's 9A.45a entries.*

**Acceptance Criteria:**
- **Given** the Counter and Latch packages, **When** they are compiled, **Then** every Story 9A.41 and 9A.43 AC holds, including the Story 9A.43 record-name `rg` returning 0 production hits.
- **And** `CaptureInstance` has no id arm except `"pipe"` (9A.45), and the 9A.45a list has no record-type entry left except the Pipe's FinalIsolation rows.
- **And** the review appends the addenda for Stories 9.2 and 6.5f: the Counter and Latch arms of 6.5f close, while 6.5f stays open for the Pipe residue (9A.45) and 9.2 stays open until `cat-020` runs in Chrome (coordinator decision, 23 Oct 2026).

`cat-005-n19` and `cat-051-interrupt` exist from Story 9.4c (position 30), so the C-Gate, 9A.39 and C-Memory ACs that name them can be met.

### Story 9A.0: Architecture Authority Records the Sole Design
*Status: Done (21 Oct 2026). Murdoch-15 gave a scoped documentation Pass on `9e4d6d86` ([review](../implementation-artifacts/review-9a-0-architecture-authority-docs.md)). The merge items M-1 to M-6 are applied in the merge with main `7d6dc38a`; the other findings are filed to Story F.12.*

*Depends on: Story 9R.5b (the current tree). Position 3. The former dependency on Story 9.4c recorded delivery order only (corrected 21 Oct 2026). Current status names each finding's owner under the 21 Oct 2026 Delivery order, including the merged stories. Audit: G1–G10. Documentation only; no runtime change.*

*Deletes:*
- *the stale claims in the untracked `docs/architecture/element-package-isolation.md` (G2): the nonexistent `CuriousContraptions.Engine`, the nested `elements/mechanical/ramp/` layout, the Optical/Pneumatic Abstractions, `IPartDefinition`/`IWorkshopInstance`/`PartId`/`GpuBodyId` "in Abstractions", the "~50–80 line" `worker.js`, and the "compiler guarantees" sentence;*
- *the `.tres` catalogue authority in `architecture.md` AD-11 (lines 145–152) and the source-map rows for `engine/PartId.cs` (line 289) and `CuriousContraptions.Simulation/wwwroot/pose-ring.js` (line 295);*
- *the `.tres` discovery and schema text in `docs/engine-contracts.md` ("Dynamic Part Discovery and Extensible PartId Contract" and "Declarative Part Definition Resource Schema Contract") and in `docs/general-engine-design.md` ("Dynamic Part Discovery and Extensible Part Identities" and "Conversion of existing implementations…");*
- *the `getF16`/`setF16` worker rows in the `docs/gpu-f32-physics.md` "f32 Migration Status" table (G7).*

As an engineer or agent starting any slice,  
I want one tracked document that states the only permitted architecture, and states separately what the tree does not yet meet,  
So that nobody builds on an end state that is described as if it already exists.

**Acceptance Criteria:**
- **Given** the tracked authoritative docs, **When** Story 9A.0 lands, **Then** `docs/architecture/element-package-isolation.md` is tracked and has two separate parts: **Target invariants** and **Current status**. Current status lists each open audit finding with its owning 9A story, and no unmet invariant is described as holding.
- **And** the target invariants state:
  1. **Project per element.** Each `elements/<Name>/` project is the only declaration of its element: metadata, typed parameter schema, ports, scene bodies (whose colliders the build bakes from the scene), body and material, domain payloads, events, presentation bindings, icon and scene asset. It references only `CuriousContraptions.Abstractions` and the domain Abstractions it needs, never the engine or a sibling.
  2. **One declaration source.** `tools/BundleElements` discovers the packages at build time and writes one bundle with one schema and no version field (owner, 21 Oct 2026; superseding "versioned"). The runtime loads only that bundle. There is no `.tres` catalogue, no per-element C# table and no `PartId` named constant.
  3. **Command/query.** Every mutation is a typed command through `WorkshopCommandRouter` with a sequence and a receipt. The query side is the published committed read model (`WorkshopRead`), the pose ring and typed queries (this resolves G5). Raw `postMessage` flags and console channels are forbidden.
  4. **Event disruptor.** One-shot discrete occurrences travel only through the single-producer, multi-consumer event ring, and each consumer has its own cursor. Level states (activation level, latch memory, timer phase, electrical level) are committed-state queries (this resolves G6).
  5. **Kernels.** Capability-named kernels and `ActivationNodeKind` values selected by declared data are valid. Element-named event kinds such as `CounterReached` and `LatchSet` are not (this resolves G9).
  6. **Forcing function.** The Story 9A.45 build gate enforces these invariants.
- **And** `architecture.md`:
  - AD-06's zero-identifier rule covers all non-element code, not only the solver core (G4);
  - AD-11 names the bundle;
  - a new AD-13, "Sole architecture", links the isolation doc;
  - the diagram text at lines 54 and 65 separates the ring from committed state;
  - the source map no longer cites `pose-ring.js`.
- **And** `docs/engine-contracts.md` and `docs/general-engine-design.md` describe bundle loading and the bundle schema contract (G10): one schema with no version field, bad input rejected by structural validation (owner, 21 Oct 2026; superseding "versioned"); f32 vectors as `[x, y, z]` and quaternions as `[x, y, z, w]`; mechanical, electrical and signal payloads; a typed parameter schema with key, unit, enum kind, f32 range and default or enum options; and an enum category.
- **And** the f32 table in `docs/gpu-f32-physics.md` names Story 9A.44 as the owner of every remaining binary16 lane.
- **And** `AGENTS.md` links the isolation doc as the sole design authority from its engineering requirements, in one line.
- **And** the Current status section records that review-9-2b's PASS for AC1, AC3, AC4 and AC5 is not supported by the tree (G8), including:
  - the MNA clause: no Modified Nodal Analysis exists, since `git grep -i 'nodal\|MNA'` returns 0 and the electrical domain is graph reachability with compiled supply routes;
  - the "~50–80 line `worker.js`" clause (347 lines).
  
  The docs state the electrical model that actually exists (compiled supply routes over a reachability graph) as the current design. MNA becomes a named later capability, delivered only when an element first needs it (retro-verification #19). The review file is historical evidence and is not edited by the implementer; Story 9A.0's terminal review appends its dated addendum (reopened-record closure, 21 Oct 2026, which replaced Story 9R.18).
- **And** the docs correct two stale text items found by the retro-verification. The Story 6.4 suite name `cat-062a.test.ts` becomes the actual `cat-062.test.ts` (§E). The spec-9-4 AC2 example "pressure plate wired to `SetIn`" is recorded as superseded: the plate's ActivationOut is a level that drives Signal lamps only (Story 9R.5).
- **And** the new `CuriousContraptions.tests/ArchitectureDocsConsistencyTests.cs` asserts that:
  - `AGENTS.md` links the isolation doc;
  - none of `AGENTS.md`, `architecture.md`, `engine-contracts.md`, `general-engine-design.md` or `gpu-f32-physics.md` presents `res://parts/catalog` / `.tres` discovery, `pose-ring.js`, `CuriousContraptions.Engine` or `getF16` as current.
  
  Its negative control feeds the same check a fixture string that reinstates one phrase, and the check fails.
- **And** every relative link in the changed docs resolves, and `dotnet test CuriousContraptions.slnx --filter ArchitectureDocsConsistencyTests` passes (native proof; this documentation-only story needs no Chrome run).

### Story 9A.1: Dead Legacy Purge
*Status: Done (21 Oct 2026). Murdoch-16 gave a scoped Pass in round 2, on `b71d32fd` ([review](../implementation-artifacts/review-9a-1-dead-legacy-purge.md)). It includes F.10: every remaining format version is retired. Findings are filed to Story F.13.*

*Depends on: Story 9A.0. Position 4, right after 9A.0 (owner amendment, 21 Oct 2026). Audit: B3, B4, B5, B7, B8, D4 (JS producer) and the dead parts of A17.*

*Deletes:*
- *`scratch/` (untracked), `scratch/CounterTestsDraft.cs.uid` and the root `scratch_test.cs.uid`;*
- *`tools/workshop-rigid-body.test.mjs`, its reference in `tools/e2e/README.md:20` and the comment in `tools/e2e/cat-014.test.ts:15`;*
- *the EarlyLoss case and the `navigator.gpu` device-loss mock in `tools/workshop-observation.test.mjs:161-174`;*
- *in `CuriousContraptions.Simulation/wwwroot/worker.js`: the 20 layout constants at `:13-37` (`BODY_*`, `MOTION_*`, `PRISMATIC_BYTES`, `COLLIDERS_OFFSET`, …) and `writeEventRing` (`:84-108`);*
- *in `CuriousContraptions.tests/WorkerAbiTests.cs`: the facts that pin those constants (`:35`, and `:52` `WorkerDynamicOffsetFallbacksDeclareValidTableOffsets`);*
- *the JS-boundary exports in `CuriousContraptions.Simulation/Program.cs:175-264` (`AllocateElectrical`, `PaidContactImpulse`, `PrismaticRowKinds`, `SpringCoefficients`, `PrismaticJacobians`, `ConstraintRows`, `OperationAbi`, `StateBytes`, `AbiOffsets`) and their host stubs in `tools/workshop-pose-ring.test.mjs` and `tools/workshop-observation.test.mjs`;*
- *the `MachinePart.Build()` subclassing hook (`engine/MachinePart.cs:236`, called at `:128`) and the test `ProbePart` override (`CuriousContraptions.tests/BasketballResourceTests.cs:162-165`);*
- *the dead "unmigrated legacy" skips in `engine/PartRegistry.cs:28-29` and `:35-36`;*
- *the remaining format versions, merged in from Story F.10 (coordinator ruling, 21 Oct 2026): `WorkshopClockWire.Version`, the `WorkshopMemoryWire` version, `WorkshopHint.Version`, `WorkshopTraceVersion`, `TraceTimingVersion`, `WorkshopScheduleVersion`, `PhysicsStateVersion` (and `STATE_VERSION`) and `BodyRecordVersion`. Story F.10's ACs apply here.*

As a maintainer,  
I want code that no current path can reach deleted,  
So that agents cannot copy or revive a dead pattern.

**Acceptance Criteria:**
- **Given** the audit's §7 evidence that nothing live references these items, **When** they are deleted, **Then** an active-tree `rg` for each deleted symbol returns 0 hits outside git history.
- **And** the engine laws that the deleted exports exposed stay covered by their existing C# unit tests; the story names those tests and they pass.
- **And** `node --test tools/workshop-observation.test.mjs tools/workshop-pose-ring.test.mjs` passes on the remaining cases.
- **And** `ProbePart` now proves its behaviour through declared bindings.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview (the Story 9.4b baseline).

### Story 9A.2: Single FlatBuffers Codec
*Status: Done (21 Oct 2026). Murdoch-22 gave a scoped Pass in round 1 on `0519cedb`, merged as `379f84ba` ([review](../implementation-artifacts/review-9a-2-single-flatbuffers-codec.md)).*
- *One framed FlatBuffers codec now carries every Workshop message. The legacy binary codec, the fallbacks that swallowed decode errors, and the ABIs are deleted.*
- *Stored saves are pre-validated, and a bad save is refused with a typed reason.*
- *AC6: the one Full run passed 43 of 44. The single failure, CAT-020, was a test bug, and its suite-only fix passed a targeted rerun.*
- *The Story 9.2 closure addendum is appended. Findings F17-5 to F17-13 are filed to Story F.17.*

*Depends on: Story 9A.1. Position 10, after 9A.14, so that Story 9A.15 edits one codec. Audit: B1, plus the legacy decode arms in A3.*

*Deletes:*
- *the legacy binary codec `engine/gpu/WorkshopWire.cs` (command, response, construction and decode-per-id arms from `:400`);*
- *the swallowing `catch { WorkshopWire.Decode* }` fallbacks in `engine/gpu/WorkshopCommandRouter.cs:17-25` and `engine/gpu/BrowserWorkshopClient.cs:268-275`;*
- *in `worker.js:138-175`: the hand-written vtable decode in `extractReadIdentity` and its fixed-offset `responseAbi` fallback;*
- *the Counter decode's silent default and float→int coercion, `inst.Threshold == 0 ? DefaultTargetCount : (int)Math.Round(inst.Threshold)` (`engine/gpu/WorkshopWireFlatBuffers.cs:896` at HEAD `8f682f8`; retro-verification #13). The target count becomes an integer field, and a missing or out-of-range value is rejected.*

As a player,  
I want every message between the game and its workers to use one checked format,  
So that a malformed message fails visibly instead of being silently reinterpreted.

**Acceptance Criteria:**
- **Given** the remaining `WorkshopWire` callers, **When** this story lands, **Then** each of them encodes and decodes through `WorkshopWireFlatBuffers` and the FlatBuffers schemas only:
  - the schedule endpoints (`CuriousContraptions.Simulation/Program.Schedule.cs:46`, `engine/gpu/BrowserWorkshopClient.Schedule.cs:58`, `CuriousContraptions.Animation.Worker/Program.cs:100`);
  - `engine/gpu/WorkshopTrace.cs:41`;
  - `engine/gpu/WorkshopSaveCodec.cs` (`ByteLength = 24 + WorkshopWire.ConstructionBytes`).
- **And** `tools/e2e/workshop-wire.ts` decodes the FlatBuffers format only.
- **And** (coordinator decision, 18 Oct 2026; from the Story 9R.0 review) saved-construction decoding does complete structural pre-validation before any domain construction runs. The checks:
  - buffer verifier and root identifier (no version check: owner, 21 Oct 2026);
  - every enum is defined;
  - every numeric value is finite and within its declared range;
  - instance ids are unique and every reference resolves;
  - every connection is type-valid.
  
  Any violation throws a typed `WorkshopSaveRejectedException` carrying a closed `WorkshopSaveRejection` reason, and the player sees the refusal. Domain argument guards stay as programmer-error faults. An `ArgumentException` that escapes after pre-validation is a stderr fault, never an "Unsupported construction save" refusal. Tests cover:
  - every rejection reason;
  - a fuzz corpus of truncated, bit-flipped and oversized inputs;
  - a duplicate-key defect injected after validation, which must surface as a fault.
- **And** a truncated, unknown-tag or trailing-byte buffer is rejected with a typed fault naming the reason. The input is never retried through another codec (negative control in a unit test for commands, responses and saves).
- **And** there are no format versions (owner, 21 Oct 2026). A save that fails structural validation is rejected explicitly with a player-visible message, and the current bench is kept (no migration).
- **And** `rg "WorkshopWire\."` returns 0 hits outside the FlatBuffers codec.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview, including every suite that saves and loads.

### Story 9A.3: Transport Controls as Typed Commands and Queries
*Status: Review (21 Oct 2026). Amelia-24 implemented it on main `379cc24a` ([spec and evidence](../implementation-artifacts/spec-9a-3-typed-transport-control-commands.md)).*

*Depends on: Story 9A.2. Position 13, right after 9A.17 (owner amendment, 21 Oct 2026). Audit: C2, C3.*

*Deletes:*
- *the raw `reliableStalled` `postMessage` flag path (`CuriousContraptions.web/wwwroot/workshop-client.js:78-86` → `worker.js:315` → `Program.SetReliableStall` at `Program.cs:495`, which stops the run at `:428-431`), or whatever of it Story 9.4b left;*
- *the raw `memoryRequest` channel (`worker.js:321-333`, `Program.ObserveMemory` at `Program.cs:144-158`, `BrowserWorkshopClient.cs:325-348`, `workshop-client.js:508-534`).*

As a player,  
I want transport problems handled through the same checked channel as every other command,  
So that a stalled connection stops the run in a predictable way.

**Acceptance Criteria:**
- **Given** a reliable-read stall, **When** it is detected, **Then** either a typed `TransportStalled` command with a sequence and a receipt passes through `WorkshopCommandRouter`, or the worker detects the stall in its own reliable-read accounting. Either way the run stops with `ReliableStalled` and the player sees the existing stall state.
- **And** memory observation is a typed query with a receipt on the command/query transport.
- **And** an untyped message on the old channel names is rejected, and the rejection is visible in the worker fault log (negative control).
- **And** `tools/workshop-observation.test.mjs` exercises the typed command and query.
- **And** `cat-023b` passes in three consecutive serial runs.
- **And** `rg "reliableStalled|memoryRequest|SetReliableStall|ObserveMemory"` returns 0 production hits outside the typed command and query definitions.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.4: Lossless Bundle Build Gate
*Status: Done (21 Oct 2026). Murdoch-17 gave a scoped Pass in round 2, on `1ba4eb95` ([review](../implementation-artifacts/review-9a-4-lossless-bundle-build-gate.md); [spec and evidence](../implementation-artifacts/spec-9a-4-lossless-bundle-build-gate.md)). Findings F14-1 to F14-8 are filed to Story F.14.*

*Depends on: Story 9R.5b (the current tree). Position 5. The former dependency on Story 9A.0 recorded number order only (corrected 21 Oct 2026). Audit: A13 (build side), A14, G10.*

*Deletes:*
- *the hand-maintained list of 13 declarations in `tools/BundleElements/Program.cs:21-36` and the anonymous-object serialiser at `:38-50`;*
- *the committed lossy `parts/elements.bundle.json`, which becomes a build output and is git-ignored;*
- *the `ElementMetadata.Parameters` property bag (`IReadOnlyDictionary<string, object>`) and the string `ElementMetadata.Category` (`CuriousContraptions.Abstractions/ElementMetadata.cs`);*
- *the bundle's `Version` and `GeneratedAt` fields (owner, 21 Oct 2026: no format versioning).*

As an element author,  
I want my package to be built, validated and serialised without loss on every build,  
So that the bundle can become the only declaration the game loads.

**Acceptance Criteria:**
- **Given** the solution, **When** it builds, **Then** `CuriousContraptions.slnx` contains every `elements/*` project, the four domain Abstractions and `tools/BundleElements`.
- **And** an MSBuild target runs `BundleElements` before the Godot export and before publish. The target discovers packages by globbing `elements/*/*.csproj` and reflecting over `IPuzzleElementDeclaration` implementations; there is no hand list.
- **And** the bundle schema is typed, with one schema and no version field or generation timestamp (owner, 21 Oct 2026; superseding "versioned"):
  - f32 vectors as `[x, y, z]` and quaternions as `[x, y, z, w]`, written bit-exactly;
  - mechanical (mass, inertia, material, anchors), electrical (nodes) and signal (triggers, latch, counter) payloads;
  - a typed parameter schema: key, unit, an enum kind, f32 minimum, maximum and default, or enum options;
  - an enum category.
- **And** generation fails the build, naming the package and field, for a duplicate id, a non-finite value, an unknown enum, or a body without a collider (negative control with fixture declarations).
- **And** a round-trip test proves that every package's declaration equals its deserialised bundle entry, f32 bit for bit.
- **And** the deleted items are gone: `git ls-files parts/elements.bundle.json` returns nothing, `rg "IReadOnlyDictionary<string, object>" CuriousContraptions.Abstractions/ElementMetadata.cs` returns 0 hits, and `tools/BundleElements/Program.cs` contains no hand-listed declaration.
- **And** a temporary cutover guard, `BundleCatalogueParityTests`, proves that every packaged element's metadata, category, ports, colliders, footprint, material and parameter defaults equal the current runtime values: `parts/catalog/*.tres`, `WorkshopPorts.For`, `WorkshopCanonicalColliders.For`, `WorkbenchFootprint.Of`, and the defaults in `MachineWorld.CaptureInstance`.
- **And** the same test asserts that the catalogue ids without a package are exactly: basket, bowling, delay, funnel, pipe, pipe_bend_45, pipe_bend_90, spring, switch, tennis and balloon. Story 9A.14 deletes this guard together with the tables it compares.
- **And** the package categories adopt the catalogue's current player-facing categories (for example `ball` is "Motion"), so palette grouping does not change for the player. *Coordinator decision under owner delegation, 18 Oct 2026 (playability): the palette must not reshuffle during a refactor.*
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview (no runtime change).

### Stories 9A.5–9A.13: one package per element that has none

Each of these nine stories adds one `elements/<Name>/` project. It does not change runtime behaviour; it makes the atomic catalogue cutover in Story 9A.14 possible without dropping an element.

### Story 9A.5: Receiver Element Package (CAT-004, id `basket`)
*Status: Review, with PKG-1. Merged into PKG-1 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/basket.tscn`, which is moved (not copied) into `elements/Receiver/`; `parts/catalog/basket.tres` points at the moved scene until Story 9A.14 deletes it. Also deletes `basket` from the parity guard's unpackaged set.*

As an element author,  
I want the Receiver declared only in its own package,  
So that the bundle can carry it when the `.tres` catalogue is deleted.

**Acceptance Criteria:**
- **Given** the Receiver's current runtime values:
  - `basket.tres` (title "Receiver", category "Goals");
  - the `basket` arms of `WorkshopPorts.For`, `WorkbenchFootprint.Of` and `WorkshopCanonicalColliders.For`;
  - `ReceiverCaptureSettings` and `ReceiverForceRegion` (`engine/gpu/WorkshopConstruction.cs:90-125`);
  - `CosmeticCurves.Receiver` (`engine/gpu/WorkshopCosmetic.cs`);
  
  **When** `elements/Receiver/Receiver.csproj` is added, referencing only `CuriousContraptions.Abstractions` and `CuriousContraptions.Abstractions.Mechanical`, **Then** the generated bundle contains `basket` and `BundleCatalogueParityTests` proves its values equal the runtime values bit for bit.
- **And** the capture settings and force region are declared parameters with the current defaults and ranges.
- **And** a project-reference test proves the package references neither the engine nor a sibling.
- **And** `cat-062b` and every other suite that places a Receiver pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.6: Bowling Ball Element Package (CAT-014, id `bowling`)
*Status: Review, with PKG-1. Merged into PKG-1 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/bowling.tscn`, moved into `elements/BowlingBall/`. Also deletes `bowling` from the parity guard's unpackaged set.*

As an element author,  
I want the Bowling Ball declared only in its own package,  
So that its heavy-ball material lives with the element and not in an engine switch.

**Acceptance Criteria:**
- **Given** the current bowling values:
  - `bowling.tres` (title "Bowling ball", category "Motion", embedded ball material);
  - the bowling `BallMaterial` arm (`engine/gpu/WorkshopConstruction.cs:61-62`), radius 0.28 m and mass 4 kg (owner decision, 9 Oct 2026);
  - the `bowling` arms of the port, footprint and collider tables;
  
  **When** `elements/BowlingBall/BowlingBall.csproj` is added, **Then** the bundle contains `bowling` with that material as declared data, and the parity guard proves equality bit for bit.
- **And** the package references only the Abstractions packages it needs.
- **And** `cat-014`, `cat-015a`, `cat-052` and `cat-005` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.7: Delay Box Element Package (CAT-022, id `delay`)
*Status: Review, with PKG-3. Merged into PKG-3 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/delay.tscn`, moved into `elements/DelayBox/`. Also deletes `delay` from the parity guard's unpackaged set.*

As an element author,  
I want the Delay Box declared only in its own package,  
So that its duration parameter and activation ports come from the bundle.

**Acceptance Criteria:**
- **Given** the current delay values:
  - `delay.tres` (title "Delay", category "Control");
  - the `delay` port, footprint and collider arms;
  - `DelayDuration` (default and range);
  - `CosmeticCurves.Delay`;
  
  **When** `elements/DelayBox/DelayBox.csproj` is added (Abstractions plus `Abstractions.Signal`), **Then** the bundle contains `delay` with a typed duration parameter and its activation in/out ports, and the parity guard proves equality.
- **And** `anim-1b`, `anim-1c`, `cat-015a-sidekick`, `cat-015b`, `cat-037`, `cat-062` and `cat-062b` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.8: Funnel Element Package (CAT-030, id `funnel`)
*Status: Review, with PKG-2. Merged into PKG-2 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/funnel.tscn`, moved into `elements/Funnel/`. Also deletes `funnel` from the parity guard's unpackaged set.*

As an element author,  
I want the Funnel declared only in its own package,  
So that its hollow-frustum colliders come from the bundle.

**Acceptance Criteria:**
- **Given** the current funnel values (`funnel.tres`, the colliders that `WorkshopGenericInstance` receives from the registry or `WorkshopCanonicalColliders.For("funnel")`, footprint and ports), **When** `elements/Funnel/Funnel.csproj` is added, **Then** the bundle contains `funnel` and the parity guard proves its colliders equal the current ones bit for bit.
- **And** `cat-030` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.9: Clear Pipe Element Package (CAT-048, id `pipe`)
*Status: Review, with PKG-2. Merged into PKG-2 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/pipe.tscn`, moved into `elements/Pipe/`. Also deletes `pipe` from the parity guard's unpackaged set.*

As an element author,  
I want the Clear Pipe declared only in its own package,  
So that its compound-cylinder and torus-rim colliders come from the bundle.

**Acceptance Criteria:**
- **Given** the current pipe values (`pipe.tres` title "Clear pipe", the declared colliders, footprint and ports), **When** `elements/Pipe/Pipe.csproj` is added, **Then** the bundle contains `pipe` and the parity guard proves equality bit for bit.
- **And** `cat-048a`, `cat-048b` and `cat-030` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.10: 45° Pipe Bend Element Package (CAT-049, id `pipe_bend_45`)
*Status: Review, with PKG-2. Merged into PKG-2 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/pipe_bend_45.tscn`, moved into `elements/PipeBend45/`. Also deletes `pipe_bend_45` from the parity guard's unpackaged set.*

As an element author,  
I want the 45° bend declared only in its own package, as a distinct element from the 90° bend,  
So that its torus-segment colliders come from the bundle.

**Acceptance Criteria:**
- **Given** the current `pipe_bend_45` values, **When** `elements/PipeBend45/PipeBend45.csproj` is added, **Then** the bundle contains `pipe_bend_45` and the parity guard proves equality bit for bit.
- **And** `cat-049` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.11: 90° Pipe Bend Element Package (CAT-050, id `pipe_bend_90`)
*Status: Review, with PKG-2. Merged into PKG-2 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/pipe_bend_90.tscn`, moved into `elements/PipeBend90/`. Also deletes `pipe_bend_90` from the parity guard's unpackaged set.*

As an element author,  
I want the 90° bend declared only in its own package,  
So that its torus-segment colliders come from the bundle.

**Acceptance Criteria:**
- **Given** the current `pipe_bend_90` values, **When** `elements/PipeBend90/PipeBend90.csproj` is added, **Then** the bundle contains `pipe_bend_90` and the parity guard proves equality bit for bit.
- **And** `cat-050` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.12: Springboard Element Package (CAT-062, id `spring`)
*Status: Review, with PKG-1. Merged into PKG-1 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/springboard.tscn`, moved into `elements/Springboard/`. The id is `spring`; the catalogue file is `parts/catalog/springboard.tres`. Also deletes `spring` from the parity guard's unpackaged set.*

As an element author,  
I want the Springboard declared only in its own package,  
So that its stiffness, damping and precompression are declared parameters.

**Acceptance Criteria:**
- **Given** the current springboard values:
  - `springboard.tres`;
  - the `spring` port, footprint and collider arms;
  - `SpringboardSettings` (defaults and ranges);
  - the prismatic slider declaration that `WorkshopPhysicsCompiler` emits for it;
  
  **When** `elements/Springboard/Springboard.csproj` is added, **Then** the bundle contains `spring` with typed parameters and its slider declaration, and the parity guard proves equality.
- **And** `cat-062`, `cat-062b` and `cat-051` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.13: Impact Switch Element Package (CAT-063, id `switch`)
*Status: Review, with PKG-3. Merged into PKG-3 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.4. Audit: A15.*

*Deletes: `parts/scenes/switch.tscn`, moved into `elements/ImpactSwitch/`. Also deletes `switch` from the parity guard's unpackaged set.*

As an element author,  
I want the Impact Switch declared only in its own package,  
So that its trigger threshold and sockets come from the bundle (Story 9.5b later adds its electrical mode to this package).

**Acceptance Criteria:**
- **Given** the current switch values:
  - `switch.tres` (title "Impact switch", category "Power");
  - the `switch` port, footprint and collider arms;
  - `ContactTriggerSettings` (threshold default and range);
  - `CosmeticCurves.ImpactSwitch`;
  
  **When** `elements/ImpactSwitch/ImpactSwitch.csproj` is added, **Then** the bundle contains `switch` and the parity guard proves equality.
- **And** `cat-037`, `anim-1b`, `anim-1c` and `engine-core-2c` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.14: Bundle Is the Sole Catalogue Source
*Status: Backlog. Position 9. Story 9A.18 (element icons ship in their packages) merges into this story (21 Oct 2026): its Deletes list and ACs apply here in full.*

*Depends on: Stories 9A.5–9A.13. Audit: A13 (runtime side), A17, A18, G3.*

*Deletes:*
- *all 24 `parts/catalog/*.tres` files;*
- *the Godot `Resource` form of `engine/PartDefinition.cs` and `engine/BallMaterialResource.cs`;*
- *in `engine/PartRegistry.cs:13-49`: `PartRegistry.Discover()`'s `.tres` scan, the `clock.tres` / `PartId.Clock` exemption (`:35-36`) and the `Ball.Capture` Basketball/Bowling exemption (`:44-45`);*
- *the remaining 13 `parts/scenes/*.tscn` (moved into their packages) and the emptied `parts/` directory;*
- *the static Free palette list (`engine/gpu/WorkshopInventory.cs` around lines 50–70);*
- *Tennis and Balloon (A18): `tennis.tres`, `balloon.tres`, `PartId.Tennis`/`PartId.Balloon` and their `FromNumericId` arms (`engine/PartId.cs:33-34`, `:90-91`), and their `"tennis"`/`"balloon"` arms in `ui/WorkshopIcons.cs`;*
- *the `BundleCatalogueParityTests` cutover guard.*

As a player,  
I want the toolbox to show exactly the elements the game can build,  
So that every part I pick can be placed and run.

**Acceptance Criteria:**
- **Given** the generated bundle, **When** the game starts, **Then** `PartRegistry` loads only the bundle. Each definition is a plain runtime record built from a bundle entry, and its scene loads from the package path (`res://elements/<Name>/…`).
- **And** a missing or structurally invalid bundle stops start-up with an explicit message naming the problem. Nothing is loaded from elsewhere.
- **And** the Free palette lists every bundle element, grouped by declared category, with the same titles, categories and order the player saw before (screenshot compared against the Story 9.4b baseline).
- **And** Tennis and Balloon no longer appear.
- **And** the new `tools/e2e/arch-9a-catalogue.test.ts` passes. In actual Chrome it:
  - asserts that the palette shows exactly the 22 bundle elements;
  - places each one through the real drawer;
  - runs, resets and save/loads one bench containing all of them.
- **And** (owner amendment, 21 Oct 2026) the old catalogue loader and the static palette list are gone:
  - `git ls-files 'parts/catalog/*.tres' 'parts/scenes/*'` returns nothing;
  - `rg "res://parts/catalog|BundleCatalogueParityTests"` returns 0 hits outside history;
  - `PartRegistry` has no `.tres` scan;
  - `engine/gpu/WorkshopInventory.cs` has no static Free palette list.
- **And** every Story 9A.18 AC holds. Each package's `icon.svg` is rendered from `ElementMetadata.IconPath`, a missing icon fails the bundle build, and `ui/WorkshopIcons.cs` keeps no element arm (`rg` for each deleted element id in that file returns 0 hits).
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

*Tennis and Balloon (coordinator decision under owner delegation, 18 Oct 2026).* They are removed from the palette rather than kept, for three reasons:
- They cannot be constructed today: `MachineWorld.CaptureInstance` throws for them (`engine/MachineWorld.Gpu.cs:124`) and `WorkbenchCapacity.cs:55` has no footprint.
- They have no package and no buoyancy or air-drag capability, so no actual-UI proof is possible.
- A part that throws when placed makes the game less playable.

Story 12.1 creates their packages and returns them to the palette.

### Story 9A.15: Saves and Messages Carry the Bundle Element Id
*Status: Done (21 Oct 2026). Murdoch-23 gave a scoped Pass in round 1 on `d3f17d27`; the evidence is in `ae196aaa` ([review](../implementation-artifacts/review-9a-15-bundle-element-id-save-wire.md), [spec](../implementation-artifacts/spec-9a-15-bundle-element-id-save-wire.md), [evidence](../implementation-artifacts/evidence-9a-15/)).*
- *Saves and messages carry the bundle element id. An unknown id is refused with a message naming it, and the bench is kept.*
- *`PartId.ToNumericId`/`FromNumericId` and the numeric part-id fields are deleted.*
- *The Full run passed 44 of 44 suites but was marked Contended, and the three contended suites passed a clean rerun.*
- *It closes F16-4 and F17-4. Findings are filed to Story F.18.*

*Depends on: Story 9A.14. Audit: A2 (numeric switches).*

*Deletes: `PartId.ToNumericId` (`engine/PartId.cs:46-70`), including its silent `_ => 0` default for unknown ids (`:72`; retro-verification #12), and `PartId.FromNumericId` (`:72-99`), and every numeric part-id field in the FlatBuffers schemas and the save codec.*

As a player,  
I want my saved machines to name each part by the same id the game loads it by,  
So that adding a new element never renumbers existing ones.

**Acceptance Criteria:**
- **Given** a construction, **When** it is saved, sent to a worker or traced, **Then** each instance carries its typed `PartId` string. That string is validated against the loaded bundle only at the serialisation boundary.
- **And** an unknown id is rejected atomically with the id named in the error (negative control).
- **And** `rg -w "ToNumericId|FromNumericId"` returns 0 hits, and no FlatBuffers schema or save field carries a numeric part id.
- **And** there are no format versions (owner, 21 Oct 2026). Structurally invalid saves are rejected with a player-visible message, and there's no migration (AGENTS.md: no automatic migration).
- **And** every suite that saves and loads passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.16: Bundle Is the Sole Collider Source
*Status: Backlog*

*Depends on: Story 9A.14. Position 14, right after 9A.17 and 9A.3 (owner amendment, 21 Oct 2026). Audit: A3 (colliders), B2.*

*Deletes:*
- *`engine/gpu/WorkshopCanonicalColliders.cs` (whole file: the 10-arm `For` switch at `:20-33`, every per-element table including `CreateLatchColliders`, and 295 `Half` tokens);*
- *the fallbacks `def.Colliders.Count > 0 ? def.Colliders : WorkshopCanonicalColliders.For(id)` at `engine/MachineWorld.Gpu.cs:118` and `:120`, and `colliders ?? WorkshopCanonicalColliders.For(...)` at `engine/gpu/WorkshopConstruction.cs:256, 304, 307, 310, 313`;*
- *the collider re-derivation in decode (`WorkshopWireFlatBuffers.cs:892`);*
- *the inline latch collider loops in `WorkshopPhysicsCompiler.cs:245-300`, and the compiler's read of `WorkshopCanonicalColliders.LatchColliders` in place of extraction (`WorkshopPhysicsCompiler.cs:281` at HEAD `8f682f8`; retro-verification #11);*
- *the silent empty return of `PartDefinition.EnsureExtracted` when the scene is missing or not a `Node3D` (`engine/PartDefinition.cs:73-96`; retro-verification X4 and #11). At build time a missing or non-`Node3D` scene now fails the bundle build, naming the package;*
- *the runtime call sites of collider extraction from scene nodes (`engine/gpu/GltfPhysicsBodyExtractor.cs` callers on the main thread and in the compiler). The Story 6.5c extraction logic itself moves to build time in `tools/BundleElements` and is not reversed.*

As a player,  
I want every part to collide exactly as the art I see,  
So that a part never behaves differently because a fallback table was used.

**Acceptance Criteria:**
- **Given** a package, **When** the bundle builds, **Then** `BundleElements` runs the Story 6.5c extraction over that package's own authored scene or glTF (`col_*` proxies or `KHR_physics_rigid_bodies`) and bakes the resulting colliders into the bundle as f32 values.
- **And** a package declares no colliders in data (decision 4; the declaration member was deleted on 25 November 2026): every body's colliders are baked from its scene.
- **And** there is one collider source per package, its scene, with no merging; a body whose scene root holds no collider proxy fails the build, naming the package (negative control).
- **And** at runtime, colliders come only from the bundle. An element with a body but no collider fails the bundle build (Story 9A.4 validation), and a construction referencing an element with no colliders is rejected explicitly.
- **And** a golden compile test captured before the change proves that every element's compiled collider rows are unchanged within f32 representation.
- **And** `rg -w "WorkshopCanonicalColliders|LatchColliders|CreateLatchColliders"` returns 0 hits, and no runtime caller of `GltfPhysicsBodyExtractor` remains.
- **And** the powered gate's moving blade, a second body on a prismatic slider, is declared in its package: its scene body and material (restitution 0.1, friction 0.3, threshold 0.1), its mass and its slider, with its collider ((0.06, 0.72, 0.72) half-extents) baked from the scene. Story 9A.4 declares only the frame and a footprint of 2 bodies, because the Abstractions cannot express a second body. *Filed by Story 9A.4, 21 Oct 2026.*
- **And** the ball detector's scene-extracted ring, which `CaptureInstance` then preferred over the canonical table, was to be reconciled with its package before the canonical table was deleted (the gate's scene colliders were not compared): at filing, at least its second collider's rotation differed, (-0.19509006, 0, 0, 0.9807853) in the scene and (0.19509034, 0, 0, 0.9807853) in the canonical table and the package; its colliders are now baked from its scene. *Filed by Story 9A.4, 21 Oct 2026.*
- **And** the same reconciliation covers the funnel, the clear pipe and both bends. Their packages then declared the canonical tables, which the worker's wire decode and every load compiled; their colliders are now baked from their scenes. Their scene-extracted colliders differ: 64 of 72 funnel, 15 of 24 pipe, 36 of 40 bend-45 and 64 of 72 bend-90 rows differ in rotation bits, and the funnel and pipe ring rotations have the opposite sign about X ([probe](../implementation-artifacts/evidence-9a-pkg-2/scene-vs-canonical-probe.txt)). Main-thread placement of the funnel and bends then preferred the scene set, and the pipe always used its canonical table. *Filed by PKG-2, 21 Oct 2026.*
- **And** `cat-048a`, `cat-048b`, `cat-049`, `cat-050`, `cat-030` and `cat-037` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

*Decision (coordinator under owner delegation, 18 Oct 2026; Open scheduling item h).* Colliders are extracted from the package's own scene at build time and baked into the bundle. Story 6.5c is kept, the package owns its scene, and the bundle stays the single declaration source. Rationale: colliders always match the art the player sees. Story 9A.0's rewrite of AD-11 records this.

### Story 9A.17: Bundle Ports and Footprints
*Status: Done (21 Oct 2026). Murdoch-24 gave a scoped Pass in round 1 on `24179187` ([review](../implementation-artifacts/review-9a-17-bundle-ports-and-footprint.md), [spec](../implementation-artifacts/spec-9a-17-bundle-ports-and-footprint.md)).*
- *The bundle is the sole source of ports for the main thread and the Simulation worker. The `WorkshopPorts` tables, `MachinePart` port transforms with their alias chain and scene fallback, and the `PartDefinition.Ports` alias are deleted.*
- *Every bad declaration is refused with a typed `ElementPortRejection`.*
- *Footprints: no table restates them, and a parity test checks them. The runtime doesn't read them yet; that moves to Story 9A.16 (F19-7).*
- *The Full run passed 284 of 284 tests and was marked Contended; the contended suites passed a clean rerun.*
- *Findings are filed to Story F.19.*

*Depends on: Story 9A.14. Audit: A3 (ports, footprint).*

*Deletes:*
- *`WorkshopPorts.For` (`engine/gpu/WorkshopConnections.cs:19-34`, 12 arms) and the per-(id, socket) `LocalPosition` lookup (`:97`);*
- *`WorkbenchFootprint.Of` (`engine/gpu/WorkbenchCapacity.cs:35-56`, 18 arms);*
- *any other runtime port-position source found, such as scene `Loc_Port_<Id>` lookups in `engine/MachinePart.cs`;*
- *the string alias chain in `MachinePart.TryGetPortTransform` (`"PowerIn"`→`"power_in"`→`"in"`, `"Supply"`→`"out"`; `engine/MachinePart.cs:560-568`; retro-verification #12). Any locator a scene keeps is named by the typed socket exactly, and a mismatched name fails the bundle build.*

As a player,  
I want wires to snap to the sockets each part declares, and parts to occupy the space they declare,  
So that wiring and placement behave the same for every element.

**Acceptance Criteria:**
- **Given** any element, **When** I wire or place it, **Then** its sockets, directions, domains and anchor positions come only from its bundle `PortDescriptor` list, and its bench footprint only from its declaration.
- **And** a golden test proves that every element's ports and footprint are unchanged.
- **And** `rg "WorkshopPorts\.For|WorkbenchFootprint\.Of"` returns 0 hits, and `MachinePart.TryGetPortTransform` has no string alias chain.
- **And** a wrong-domain or output-to-output link is still refused (negative control).
- **And** every wiring suite passes (`cat-005`, `cat-017`, `cat-020`, `cat-037`, `cat-051`, `cat-052`, `anim-1b`, `anim-1c`).
- **And** the existing disagreements between the declaration ports and the scene `Loc_Port_*` locators are resolved, with one source in the bundle (visual review of 19 Oct 2026, #11). For example, the detector's signal port is at (0, 0.4, 0) in its declaration and (0, 1.12, 0) in its scene; the Gate, Counter, Plate, Battery, Bumper, Domino and Lamp also differ. The pipe and bend start-mouth locators face outward, as the Funnel's does. A test fails if a declared port and its visual locator disagree.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.18: Element Icons Ship in Their Packages
*Status: Backlog. Merged into Story 9A.14 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.14. Audit: A3 (icons).*

*Deletes: every element arm in `ui/WorkshopIcons.cs`, both delivered elements (ball, bowling, ramp, wall, basket, battery, bumper, spring, switch, domino, lamp, latch, clock, …) and undelivered ones (motor, fan, bellows, windmill, wind_chimes, bell, sound_meter, speaker, clutch). Each undelivered element's icon is authored again in its own package when that element ships. Icons for UI controls and socket choices (resize, set_input, reset_input, first_input, second_input, extended_output, retracted_output) stay.*

As a player,  
I want each part's toolbox icon to come with that part,  
So that new elements bring their own icon without engine edits.

**Acceptance Criteria:**
- **Given** each package, **When** the bundle is built, **Then** the package's `icon.svg` is referenced by `ElementMetadata.IconPath` and the palette renders it.
- **And** a missing icon fails the bundle build (negative control).
- **And** in actual Chrome, palette icon screenshots match the Story 9.4b baseline for every element.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.19: Declared Prismatic Drive for the Powered Gate
*Status: Backlog. Merged into C-Gate (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Stories 9A.17, 9.4 (review-9-4 F6) and 9R.3/9R.4. Those stories already fixed the behaviour with compiled Run-time supply routes: per-gate own-battery routes, per-plate routes, latch and counter routes. This story only converts the gate drive into a package declaration and does not repeat those fixes. Audit: A7 (gate part).*

*Deletes whatever of the following Stories 9.4, 9R.3 and 9R.4 have not already deleted (the implementer lists which are present at start):*
- *gate detection by joint limit (`joint.Upper ≈ 1.55`, `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:241`);*
- *any drag band left in `:247-249` and the `ActuatorSliderDrive` heuristics;*
- *the record-type and `PartId.PoweredGate` selection of the drive in `engine/gpu/WorkshopPhysicsCompiler.cs` (`:186`, `:288` at HEAD `8f682f8`);*
- *`SimJoint.SwitchedSupplyClosed` (`PrismaticConstraintSolver.cs:35`) if it is no longer read from the compiled route table.*

As an element author,  
I want the Powered Gate's drive declared in its package,  
So that any slider element can declare a drive and the solver never recognises a gate.

**Acceptance Criteria:**
- **Given** the Powered Gate package, **When** it is compiled, **Then** it declares a prismatic drive capability: the target opening 1.55 m, the speed law sign(d)·min(2.8, √(2·14·|d|)), effort ≤ 60 N, power ≤ 120 W, and the return spring 14 N/m with damping 4 N·s/m (CAT-051 §2).
- **And** the drive is enabled only by the compiled supply route rows from Stories 9.4, 9R.3 and 9R.4, unchanged. Golden route and drive rows are identical before and after.
- **And** a box body with the gate's dimensions and joint limit but no declared drive never actuates (negative control for the heuristics).
- **And** the Story 9R.3 own-supply and two-battery checks still pass (regression, not re-implementation).
- **And** when a gate is enabled during Run with a ball resting against its closed, tilted blade, the ball passes through and is not thrown back out of the entry mouth. A Chrome check asserts that the ball ends downstream. This behaviour predates Story 9R.3 (base matches candidate in Murdoch-9's probe) and was filed here by the coordinator on 20 Oct 2026.
- **And** `cat-051`, `cat-052`, `cat-037`, `cat-005-n19` and `cat-051-interrupt` pass.
  - Before catalogue E2E, the native guard is LatchSupplyFixtureTests (Story 9.4c); the browser suites remain pending.
- **And** the element-scan ratchet test introduced in Story 9.4b (`WorkshopPhysicsLawTests.TheShippedEngineSourceNamesNoElementHardCodedInertiaOrBallMaterial`) has an empty known-violations list. 9R.3 and 9R.4 each remove their own entries, and this story removes the last (gate travel-limit recognition and the literal drive constants). The list then stays empty permanently (coordinator decision, 18 Oct 2026).
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.20: Declared Contact-Load Sensor for the Pressure Plate
*Status: Backlog. Merged into C-Plate (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Stories 9A.19 and 9R.4/9R.5. Story 9R.4 already replaced size detection and the global `_plateLoaded` flag with per-plate compiled contact-load rows that honour `minimum_mass`, and 9R.5 added the ActivationOut level. This story only moves the source of those rows into the package declaration. Audit: A7 (plate part).*

*Deletes whatever Stories 9R.4 and 9R.5 have not already deleted: plate detection by half-extents, `_plateLoaded`, the 0.5 kg literal and the plate drag band. It also deletes the compiler's record-type selection of the plate row (`OfType<WorkshopPressurePlate>` emitting the contact-load row), which is replaced by the package's declared contact-load sensor.*

As an element author,  
I want the Pressure Plate's load sensor declared in its package,  
So that any element can declare a load sensor and the compiler never recognises a plate.

**Acceptance Criteria:**
- **Given** the Pressure Plate package, **When** a plate is compiled, **Then** its `ContactLoadSensorDeclaration` row (region, normal, alignment, and the `minimum_mass` parameter: f32 kg, range 0.1–16, default 0.5, inclusive ≥; [CAT-052](../../docs/planning/elements/CAT-052-pressure_plate.md) §2) comes from the package. The golden rows are identical to Story 9R.4's.
- **And** a static box with the plate's exact half-extents and offset but no declared sensor never activates (negative control).
- **And** the Story 9R.4 and 9R.5 checks still pass: threshold, two-plate independence, lamp level, and no lit lamp after a bounce (regression).
- **And** `cat-052`, `cat-052-two-plates` and `cat-052-lamp` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.21: Remove Synthetic Worker Startup Work
*Status: historical declaration-free replacement delivered in C-Gate (21 Oct 2026); owner-directed removal is implemented and under independent review in `engine-remove-core-warmup` (26 Oct 2026). That slice owns the current ACs, verification and publication; Story 9A.21 and audit A9 retain their identities.*

*Depends on: Story 9A.19. Audit: A9.*

*Deletes: the synthetic `WorkshopPhysicsEngine.Warmup` API and transport caller, `WarmupScene`, `Program.WarmupSimulation` / `WarmupInstallation`, and their diagnostic, compile and guard closure. This supersedes the older element-scene replacement under the same story and A9 identities.*

As a player,  
I want real device preparation and the first Run to work without synthetic startup workloads,
So that core startup performs only the work the real session needs.

**Acceptance Criteria:**
- **Given** worker start-up, **When** actual device resources prepare, **Then** no synthetic scene, dummy kernel/solver workload or dummy command lifecycle executes. Explicit and implicit preparation both support the first real construction and tick, exact Reset/replay and ordinary engine reuse. Preserve bootstrap, the real clock, resource ownership and solver warm-starting.
- **And** first-use responsiveness and time to the first committed tick retain their advisory measurement and non-regression contract at the named Epic 16 performance gate. No synthetic runtime workload is introduced to satisfy a measurement warm-up.
- **And** matching Debug compiler proof reports 0 warnings/errors and required local native tests pass with zero skips. Normal hooks and exact-SHA production publication remain required. Serial production-origin Chrome proof remains at post-catalogue E2E.

*Superseding owner decision (26 Oct 2026).* Remove all synthetic core startup work; the older coordinator retention choice is obsolete. [Removal contract](../implementation-artifacts/spec-engine-remove-core-warmup.md) C1–C5 governs. First-use measurements and performance warm-up remain at Epic 16; browser proof remains deferred until post-catalogue E2E.

### Story 9A.22: Event Ring Producer
*Status: Backlog. Position 44, immediately before C-Memory, whose ACs need `ThresholdReached` and `MemoryChanged`.*

*Depends on: Stories 9A.2 and 9.4. Audit: D1, A16.*

*Deletes:*
- *the default-empty `Span<byte> eventBuffer = default` parameter of `ActivationNetwork.Consume` (`engine/gpu/ActivationNetwork.cs:243`) and its publish-only-if-non-empty branches (`:391-402` Clock `TriggerActivated`, `:487-491` Latch);*
- *the element-named kinds `SimulationEventKind.CounterReached` and `LatchSet` (`CuriousContraptions.Abstractions/EventDescriptor.cs:3-14`), replaced by the generic `ThresholdReached` and `MemoryChanged`. The Counter and State Latch packages are updated in the same commit.*

As a player,  
I want every discrete thing that happens in a run (an impact, a crossing, a threshold, a power change) to be announced once, at the moment it is committed,  
So that sound, animation and the interface can all react to the same event.

**Acceptance Criteria:**
- **Given** a run, **When** a tick commits, **Then** the worker publishes exactly one ring record per discrete transition, with the kind's engine-owned layout. The kinds are: Impact, Passage, Captured, ContactLoad, ElectricalPowered, ThresholdReached, MemoryChanged and TriggerActivated. *(Captured added for the 9A.23 Receiver one-shot; PrismaticLimit is deferred because no row produces it: the first story with a gate-limit cue adds the kind and its producer. Coordinator decision, 23 Oct 2026.)*
- **And** a discarded or failed tick publishes nothing.
- **And** Reset and each new run restart the ring sequence.
- **And** overrun follows the spec-9-2b §5 policy.
- **And** `worker.js` copies the committed ring bytes into the SharedArrayBuffer the same way `writePoseRing` copies poses, with no domain logic in JS.
- **And** unit tests cover every kind, including a negative case where a non-qualifying contact publishes nothing.
- **And** in actual Chrome, `cat-015a` and `cat-020` gain a ring read and see exactly one Impact per Bumper hit and one ThresholdReached per Counter target. The read goes through the Story 9A.23 consumer or the Story 9A.24 port, post-catalogue (no `globalThis` reader exists or is added; browser qualification pending).
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.23: Event Ring Consumers
*Status: Backlog. Delivered with Story 9A.25b as one slice, "9A.23 + 9A.25b", at position 48 (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.22. Audit: D2, D3, D4 (JS reader).*

*Deletes:*
- *occurrence-carrying reliable reads: `occurrenceReads`, `reserveOccurrenceRead`, `releaseOccurrenceRead` and `publish(..., retainOccurrence)` (`worker.js:11`, `:217-235`), and `EmitRead(retainOccurrence)` (`CuriousContraptions.Simulation/Program.cs:472-491`);*
- *the occurrence clauses of `WorkshopRead.RequiresReliableRead` (`engine/gpu/WorkshopRead.cs:9-18`) and the occurrence arrays in `PhysicsContactWorkRead`;*
- *the sequence-diff pumps in `engine/gpu/BrowserWorkshopClient.ContactWork.cs:27-60` and `BrowserWorkshopClient.Capture.cs`, and the one-shot pumps in `PumpCosmetics` (`BrowserWorkshopClient.Cosmetic.cs:23`);*
- *the main-thread `Transition` relay of one-shots to the Animation worker;*
- *the divergent JS reader `createEventConsumer`/`readEventRing` (`workshop-client.js:734-788`), replaced by the new main-thread consumer. (Already absent from the tree at Story 9A.22; the main thread only attaches `client.eventRing`.)*

As a player,  
I want impacts, captures and switch clicks to animate from the event itself,  
So that effects line up with the physics and none is lost or doubled.

**Acceptance Criteria:**
- **Given** the event ring, **When** a run starts, **Then** the Animation worker receives the ring and holds its own cursor with domain and kind filters. It drives the Bumper impact pulse, Receiver capture, and switch and latch one-shots from ring records.
- **And** the main-thread UI holds a separate cursor for goal and sensor readouts.
- **And** level states (lamp level, latch memory, Delay phase, electrical level) stay committed-state queries.
- **And** one consumer implementation per worker reproduces `EventRingConsumer`'s skip-uncommitted semantics, and its overrun is explicit.
- **And** the Story 9A.22 ring assertions in `cat-015a` and `cat-020` pass through the main-thread consumer.
- **And** `anim-1a`, `anim-1b`, `anim-1c`, `cat-015a`, `cat-015a-sidekick`, `cat-015b`, `cat-062b` and `cat-037` pass, with cosmetic timing checked.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.24: Typed Read-Only Observation Port Replaces Console Channels
*Status: Implementation done; five Auto layers and affected re-reviews Pass, final local 5,047/0/0. Normal hooks, exact-SHA approval and publication pending. Browser qualification pending; CI/deployment unverified. Position 52.*

*Current enforcement: the stabilization contract defers the browser consumer migration and Chrome assertions below until post-catalogue E2E. This slice replaces all active production publishers and migrates native/node consumers; no old production alias remains for deferred browser tools. [Implementation and evidence](../implementation-artifacts/spec-9a-24-typed-observation-port.md).*

*Depends on: Story 9A.23. Audit: C4, C5.*

*Deletes:*
- *`GD.Print("CCGPU " + base64(response))` (`engine/MachineWorld.Gpu.cs:197`, `:363-366`);*
- *`CCGOAL_SOLVED` (`ui/WorkshopAnimation.cs:96`);*
- *`globalThis.WorkshopPoseRing`, `WorkshopAnimation` and `WorkshopEventRing` (`workshop-client.js:796-809`);*
- *the `CCGPU` acknowledgement parsing and global-hook reads in `tools/e2e/workshop-driver.ts` (`:53`, `:117-120`, `:417-420`, `:465-474`, `:786`) and in `cat-005`, `cat-015a`, `cat-015a-sidekick`, `cat-015b`, `cat-062`, `cat-062b`, `cat-023b` and `engine-core-2a1`.*

As a tester proving the shipped game,  
I want one typed, read-only observation port,  
So that every e2e suite observes the production build without console scraping or ad-hoc globals.

**Acceptance Criteria:**
- **Given** the production build, **When** a page loads, **Then** a typed observation port, with no version field (owner, 21 Oct 2026), exposes three things:
  - command receipts (sequence, kind, result);
  - a committed read-model query;
  - an independent event-ring cursor.
- **And** the port cannot issue a command or mutate any state (negative control: every mutation attempt through it is rejected and the construction is unchanged).
- **And** `workshop-driver.ts` waits on receipts and goal events through the port, and every suite above is migrated.
- **And** `rg "CCGPU|CCGOAL_SOLVED|globalThis.Workshop"` returns 0 hits.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

*Design choice (coordinator, under owner delegation).* The port ships in the single production artifact rather than in a separate `PLAYTEST` build, because AGENTS.md requires Chrome proof of the production build. It is read-only by construction.

### Story 9A.25: Presentation Bindings Ship in Packages
*Status: Backlog. Split on 21 Oct 2026. The committed-state part is Story 9A.25a (position 15, with 9R.20). The one-shot drivers are Story 9A.25b, delivered with 9A.23 (position 48). These ACs stay here and apply to both.*

*Depends on: Story 9A.17 for 9A.25a, and Story 9A.23 for 9A.25b only. The former single dependency on 9A.23 recorded number order for the committed-state bindings (corrected 21 Oct 2026). Audit: A4, A10.*

*Deletes:*
- *the `MachinePart.BuildDeclarative` chain of 24 `PartId ==` arms (`engine/MachinePart.cs:237-530`);*
- *`ResizableAxes` by id (`:40-50`);*
- *the Clock and Counter lamp methods (`:106-113`, `:220-221`, `:533`, `:543`);*
- *`CosmeticCurves.ImpactSwitch/SignalLamp/Delay/PinballBumper/Receiver/BallDetector/StateLatch` (`engine/gpu/WorkshopCosmetic.cs:55-63`);*
- *the element-named `AnimationFeedbackSource.Memory` (`:8`), replaced by generic feedback sources: activation level, state level, timer phase, contact pulse and capture.*

As an element author,  
I want my element's moving parts, curves, pick radius and resizable axes declared in its package,  
So that `MachinePart` applies every element's presentation generically.

**Acceptance Criteria:**
- **Given** each package's declared bindings (target node path, enum channel, f32 from/to, curve, duration, driver as a committed-state query or ring kind, pick radius, resizable axes), **When** a part is built, **Then** `MachinePart` applies them with no element branch.
- **And** sampled node properties at fixed curve times equal the pre-change values for every element (golden test). The shipped values are preserved, including whatever Story 9.4 decides for the State Latch curve (review-9-4 F5).
- **And** in actual Chrome, per-element motion screenshots match the Story 9.4b baseline.
- **And** `anim-1a`, `anim-1b` and `anim-1c` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.26: Lessons and Puzzles as Content Data
*Status: Backlog. Position 22, the first deletion slot after C-Switch (21 Oct 2026). Its eight lesson files build records of most families, so deleting them first spares every later conversion a lesson edit.*

*Depends on: Stories 9A.14 and 9A.17. Audit: A12, A19.*

*Deletes:*
- *`engine/gpu/BumperAdvanced.cs`, `BumperSidekick.cs`, `SpringboardLessons.cs`, `DominoEffect.cs`, `DelayedSignal.cs`, `ClearPipe.cs`, `GentleBend.cs` and `QuarterBend.cs`;*
- *the closed `WorkshopPuzzleId` enum (`engine/gpu/WorkshopPuzzle.cs:5`) and `WorkshopPuzzle.WallAssistance`'s id check (`:57`);*
- *the `ReadyToRebound` branch in `WorkshopConstruction.WithInstance` (`engine/gpu/WorkshopConstruction.cs:131-133`);*
- *the `WallAndBumper` branch in `WorkshopInventory.Authored` (`engine/gpu/WorkshopInventory.cs:93`);*
- *the puzzle-id switch in `ui/WorkshopPuzzle.cs:18-28`.*

As a player,  
I want every lesson to play exactly as before while being loaded as data,  
So that every campaign level (at least 150; 244 planned) can be authored without engine code.

**Acceptance Criteria:**
- **Given** `content/puzzles.json`, extended with a typed schema with no version field (owner, 21 Oct 2026), **When** a lesson loads, **Then** the lesson is built entirely from that data:
  - an extensible typed puzzle id;
  - placements (part id, pose, locked flag, parameter values validated against each element's declared parameter schema);
  - inventory allowances;
  - assistance profiles;
  - goal predicates.
  
  The construction is compiled on the main thread from that data.
- **And** a content file with an unknown part id, an unknown parameter or an out-of-range value is rejected at load, with the puzzle and field named (negative control).
- **And** (owner amendment, 21 Oct 2026) `git ls-files` returns nothing for the eight deleted lesson files, and `rg -w "WorkshopPuzzleId|ReadyToRebound|WallAndBumper"` returns 0 production hits outside `content/`.
- **And** each lesson keeps its current layout, inventory and solution: FirstPrinciples, DelayedSignal, DominoEffect, BumperSidekick, BumperDepth, WallAndBumper, SpringForward, ReadyToRebound, ClearPipe, GentleBend and QuarterBend.
- **And** `cat-015a-sidekick`, `cat-015b`, `cat-062`, `cat-062b`, `cat-023b`, `cat-048b`, `cat-049` and `cat-050` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Stories 9A.27–9A.43: one element per story onto the generic declared instance

Each story converts one element from its engine record type to `WorkshopGenericInstance`, which carries an id, part id, pose, locked flag and typed parameters validated against the element's declared parameter schema. Both bends, Funnel and Powered Gate already use it. Pipe still uses `WorkshopPipe`/`PipeLength`; its remaining generic-instance conversion is explicitly owned by final Story9A.45. The worker never resolves an element: the main thread resolves the bundle declaration, and the instance carries the declared capability payloads.

Each story deletes, for its element:
- the record type and its element-named settings records;
- its `WorkshopInput.<X>` factory (`engine/gpu/WorkshopConstruction.cs:152-313`);
- its `MachineWorld.CaptureInstance` arm (`engine/MachineWorld.Gpu.cs:93-125`);
- its arm in the `WorkshopInstances.Validate` type whitelist (`engine/gpu/WorkshopInstances.cs:95`);
- its `OfType<>`/`is` arms in `WorkshopPhysicsCompiler` and `ActivationNetwork.Compile` (`engine/gpu/ActivationNetwork.cs:688-722`);
- its arm in `MachineWorld.Gpu.cs:318-338`;
- its per-id FlatBuffers encode and decode arms (`engine/gpu/WorkshopWireFlatBuffers.cs:853+`).

If the element is the first to need a capability (dynamic body, contact trigger, orientation sensor, timer, memory, counter, electrical source), that story adds the generic compile of that capability from declared data. Each story's golden compile test proves the compiled capability rows are unchanged. No story bumps a format version (owner, 21 Oct 2026); a save that fails structural validation is rejected explicitly.

*Delivery order, 21 Oct 2026.* These conversions are delivered as family stories (C-Switch, C-Ramp+Receiver, C-Balls, C-Bumper+Spring, C-Gate, C-Lamp+Detector, C-Plate, C-Clock and C-Memory), plus 9A.32 (Domino) and 9A.39 (Battery) alone. Story 9A.38 is delivered in 9.5a, and 9A.27 stays on the critical path.
- They run right after Story 9.5 and alternate one for one with new elements, ordered by how much old code they delete.
- Each deletes its element's `CaptureInstance` arm, and any per-element panel, together with its record.
- Each AC includes: an active-tree `rg` for every deleted symbol returns 0 production hits, and the story's 9A.45a known-violations entries are gone.

### Story 9A.27: Generic Instance and Parameter Editor — Wall (CAT-066)
*Status: Backlog. Position 16. Story 9R.14 (the `cat-066` suite) merges into this story as its suite-first guard (21 Oct 2026).*

*Depends on: Story 9A.25a. The former dependency on Story 9A.26 is replaced by a one-line edit to `engine/gpu/BumperAdvanced.cs` (the WallAndBumper lesson's `instance is WorkshopWall` checks at `:69-74` read the generic Wall instance); Story 9A.26 deletes that file later. Audit: A5, A6, A11 (generic editor).*

*Deletes:*
- *`WorkshopWall` and `WallDimensions` (`engine/gpu/WorkshopWall.cs:30`), `WorkshopInput.Wall`, and the `"wall"` arm and the `wallDimensions` parameter of `CaptureInstance`;*
- *the Wall half of `is WorkshopRamp or WorkshopWall` in `WorkshopPhysicsCompiler`;*
- *`WorkshopWall` in the `Validate` whitelist.*

*The coordinator asked (21 Oct 2026) that this story delete the whole `CaptureInstance` id list and the per-element configuration panels. The other arms and panels build or edit record types that live until their own conversions, so they are recorded as 9A.45a known-violations entries, each owned by its conversion. Reconciliation item 4 is resolved: retain those named owners; 9A.27 converts Wall only. 9A.25a has already replaced dimension-specific CaptureInstance parameters with the generic dimensions argument.*

As a player,  
I want to resize and configure a Wall through an editor built from its declared parameters,  
So that every element after it can use the same editor.

**Acceptance Criteria:**
- **Given** a Wall placed through the real drawer, **When** I edit its width and height, **Then** the generic parameter editor (the generic path in `ui/WorkshopConfiguration.cs`) renders the declared parameters with their units and ranges, and the instance stores typed f32 values.
- **And** an out-of-range or unknown parameter is rejected and the instance is unchanged (negative control).
- **And** Run/Reset and Save/Load restore the wall exactly.
- **And** the golden compile rows are unchanged.
- **And** (suite first) Story 9R.14's `cat-066` suite lands green on the record path and stays green after the conversion. Every Story 9R.14 AC applies, and the out-of-range resize clamps at its limit.
- **And** `rg -w "WorkshopWall|WallDimensions|wallDimensions"` returns 0 production hits.
- **And** the review appends the addendum for Story 6.5d's 9R.14 part.
- **And** `cat-015b` and every suite that places a Wall pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.28: Generic Instance — Ramp (CAT-054)
*Status: Backlog. Merged into C-Ramp+Receiver (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.27. Audit: A5, A6.*

*Deletes: `WorkshopRamp` and `RampDimensions` (`engine/gpu/WorkshopInstances.cs:42-56`), `WorkshopInput.Ramp`, the `"ramp"` arm and `rampDimensions` parameter of `CaptureInstance`, the remaining `is WorkshopRamp` in the compiler, and `WorkshopRamp` in the whitelist.*

As a player,  
I want the Ramp configured through the generic editor,  
So that it behaves as before with no engine record.

**Acceptance Criteria:**
- **Given** a Ramp placed and resized through real UI, **When** run, reset, saved and loaded, **Then** its declared length and width parameters are restored exactly and the golden compile rows are unchanged.
- **And** an out-of-range length is rejected (negative control).
- **And** every suite that places a Ramp passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.29: Generic Instance — Receiver (CAT-004)
*Status: Backlog. Merged into C-Ramp+Receiver (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.27. Audit: A5, A6.*

*Deletes: `WorkshopReceiver`, `ReceiverCaptureSettings` and `ReceiverForceRegion` (`engine/gpu/WorkshopConstruction.cs:90-125`), `CaptureReceiver`, the `WorkshopConstruction.Receiver` accessor, `OfType<WorkshopReceiver>` in the compiler, and `WorkshopReceiver` in the whitelist.*

As a player,  
I want the Receiver to capture balls exactly as before,  
So that every goal keeps working with no engine record.

**Acceptance Criteria:**
- **Given** a Receiver placed through real UI, **When** a ball enters, **Then** capture and guide behaviour match the golden compile rows and the Story 9.4b `cat-062b` expectations.
- **And** a ball that misses is not captured (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-062b`, `cat-062` and `cat-014` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.30: Generic Instance — Basketball (CAT-001)
*Status: Backlog. Merged into C-Balls (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.27. Audit: A5, A6, A3 (ball materials).*

*Deletes: the `"ball"` arm of `CaptureInstance`/`CaptureBall` and the basketball arm of `BallMaterial` by id (`engine/gpu/WorkshopConstruction.cs:61-62`). The shared `WorkshopBall` record stays until Story 9A.31, because Bowling still uses it.*

As a player,  
I want the Basketball to roll and bounce exactly as before,  
So that its material comes only from its package.

**Acceptance Criteria:**
- **Given** a Basketball placed through real UI, **When** run, **Then** its declared dynamic-sphere body and material compile to rows equal to the golden rows. This story adds the generic dynamic-body compile.
- **And** Run/Reset and Save/Load are exact.
- **And** `engine-core-2a1` to `engine-core-2c`, `cat-002` and `cat-023a` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.31: Generic Instance — Bowling Ball (CAT-014)
*Status: Backlog. Merged into C-Balls (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.30. Audit: A5, A6.*

*Deletes: `WorkshopBall` and `BallMaterial`-by-id (`engine/gpu/WorkshopConstruction.cs:49-89`), `CaptureBall`, the `WorkshopConstruction.Ball` accessor, `OfType<WorkshopBall>` in the compiler, and `WorkshopBall` in the whitelist.*

As a player,  
I want the Bowling Ball to keep its heavy feel,  
So that the last ball record leaves the engine.

**Acceptance Criteria:**
- **Given** a Bowling Ball placed through real UI, **When** run, **Then** its 0.28 m, 4 kg declared material gives golden-equal rows, and a Basketball on the same bench keeps its own material (control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-014`, `cat-015a`, `cat-052` and `cat-005` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.32: Generic Instance — Domino (CAT-023)
*Status: Backlog. Position 38 (Delivery order, 21 Oct 2026).*

*Depends on: Story 9A.30 (C-Balls). Audit: A5, A6.*

*Deletes: `WorkshopDomino` and `DominoMaterial` (`engine/gpu/WorkshopDomino.cs:22`), `WorkshopInput.Domino`, the `"domino"` arm of `CaptureInstance`, `OfType<WorkshopDomino>` in the compiler, the `instance is WorkshopDomino` arm in `ActivationNetwork.Compile`, `WorkshopDomino` in the whitelist, and this story's 9A.45a entries. AC: `rg -w "WorkshopDomino|DominoMaterial"` returns 0 production hits.*

As a player,  
I want dominoes to stand, topple and trigger exactly as before,  
So that the orientation sensor is selected by declaration only.

**Acceptance Criteria:**
- **Given** a domino row built through real UI, **When** the first domino is pushed, **Then** the cascade, the 45° orientation-sensor occurrence and the lamp activation match the golden rows. This story adds the generic orientation-sensor compile.
- **And** an upright domino never fires (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-023a` and `cat-023b` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.33: Generic Instance — Pinball Bumper (CAT-015)
*Status: Backlog. Merged into C-Bumper+Spring (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.30. Audit: A5, A6, A11.*

*Deletes: `WorkshopBumper` and `BumperWork` (`engine/gpu/WorkshopBumper.cs:36`), `WorkshopInput.Bumper`, the Bumper panel in `ui/WorkshopConfiguration.cs:12-16`, and `WorkshopBumper` in the whitelist.*

As a player,  
I want the Bumper's strength set through the generic editor,  
So that its finite work store is declared data.

**Acceptance Criteria:**
- **Given** a Bumper configured through the generic editor, **When** balls strike it, **Then** paid and unpaid contacts and the energy ledger match the golden rows and the Story 9.4b repaired behaviour.
- **And** a later empty contact spends nothing (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-015a`, `cat-015a-sidekick` and `cat-015b` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.34: Generic Instance — Springboard (CAT-062)
*Status: Backlog. Merged into C-Bumper+Spring (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.30. Audit: A5, A6, A11.*

*Deletes: `WorkshopSpringboard` and `SpringboardSettings` (`engine/gpu/WorkshopSpringboard.cs:17`), `WorkshopInput.Springboard`, `OfType<WorkshopSpringboard>` in the compiler, the Springboard panel in `ui/WorkshopConfiguration.cs`, and `WorkshopSpringboard` in the whitelist.*

As a player,  
I want to set the Springboard's preload through the generic editor,  
So that launches come from declared slider and spring data.

**Acceptance Criteria:**
- **Given** a Springboard configured through the generic editor, **When** a ball lands on it, **Then** the preload and launch routes match the golden rows.
- **And** zero preload gives no launch (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-062` and `cat-062b` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.35: Generic Instance — Impact Switch (CAT-063)
*Status: Backlog. Merged into C-Switch (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Stories 9A.27 (generic editor), 9A.13 (switch package, in PKG-3) and 9.5a. The former dependency on 9A.30 recorded number order only (corrected 21 Oct 2026). Audit: A5, A6.*

*Deletes: `WorkshopSwitch` (`engine/gpu/WorkshopActivationParts.cs:13`) and the element-specific use of `ContactTriggerSettings`, which becomes a declared threshold parameter. Also deletes `WorkshopInput.Switch`, the `is WorkshopSwitch` arms in the compiler (around lines 96 and 115–150) and in `ActivationNetwork.Compile`, and `WorkshopSwitch` in the whitelist.*

As a player,  
I want the Impact Switch to latch on a qualifying hit exactly as before,  
So that contact triggers are selected by declaration only.

**Acceptance Criteria:**
- **Given** a switch wired to a lamp through real UI, **When** a ball hits at or above the declared threshold, **Then** the switch latches once and the lamp lights. This story adds the generic contact-trigger compile.
- **And** a below-threshold hit does nothing (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-037`, `anim-1b`, `anim-1c` and `engine-core-2c` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.36: Generic Instance — Signal Lamp (CAT-035)
*Status: Backlog. Merged into C-Lamp+Detector (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.35. Audit: A5, A6.*

*Deletes: `WorkshopLamp` (`engine/gpu/WorkshopActivationParts.cs:25`), `WorkshopInput.Lamp`, the lamp arms in the compiler and `ActivationNetwork.Compile`, and `WorkshopLamp` in the whitelist.*

As a player,  
I want the lamp to light on its first input exactly as before,  
So that its sticky first-input node is selected by declaration.

**Acceptance Criteria:**
- **Given** a lamp wired from a switch, **When** the switch fires, **Then** the lamp lights and stays lit (declared activation node kind, renamed in Story 9.4 per review-9-4 F9).
- **And** an unwired lamp stays dark (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-023b`, `anim-1b` and `anim-1c` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.37: Generic Instance — Ball Detector (CAT-002)
*Status: Backlog. Merged into C-Lamp+Detector (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.35. Audit: A5, A6.*

*Deletes: `WorkshopBallDetector` (`engine/gpu/WorkshopActivationParts.cs:37`), `WorkshopInput.BallDetector`, its `CaptureInstance` registry/collider lookup, the detector arm in `ActivationNetwork.Compile`, and `WorkshopBallDetector` in the whitelist.*

As a player,  
I want the Ball Detector to count each crossing once, with directional rearm, exactly as before,  
So that aperture sensors are selected by declaration.

**Acceptance Criteria:**
- **Given** a detector on a pipe route, **When** balls cross, **Then** there is one occurrence per crossing and a reverse crossing does not rearm (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-002` and `cat-020` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.38: Generic Instance — Delay Box (CAT-022)
*Status: Backlog. Merged into Story 9.5a (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Stories 9A.27 and 9A.7 (delay package, in PKG-3). The former dependency on 9A.35 recorded number order only: the Impact Switch in the Delay fixture stays a record instance until C-Switch (corrected 21 Oct 2026). Audit: A5, A6, A11.*

*Deletes: `WorkshopDelay` and `DelayDuration` (`engine/gpu/WorkshopDelay.cs:36`), `WorkshopInput.Delay`, the Delay panel in `ui/WorkshopConfiguration.cs`, the delay arm in `ActivationNetwork.Compile`, and `WorkshopDelay` in the whitelist.*

As a player,  
I want to set the Delay through the generic editor,  
So that timers are selected by declaration.

**Acceptance Criteria:**
- **Given** a Delay between a switch and a lamp, **When** the switch fires, **Then** the lamp lights after exactly the declared duration (this story adds the generic timer compile), and it does not light earlier (negative control).
- **And** Run/Reset and Save/Load are exact.
- **And** `anim-1b`, `anim-1c` and `cat-062` pass.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.39: Generic Instance — Battery (CAT-005)
*Status: Backlog. Position 34 (Delivery order, 21 Oct 2026). It also deletes the `"battery"` arm of `CaptureInstance` and this story's 9A.45a entries. AC: `rg -w "WorkshopBattery|AddBatteryConfiguration"` returns 0 production hits, and `git ls-files ui/WorkshopBattery.cs` returns nothing. The review appends the addendum that re-verifies Story 8.1 against its amended acceptance.*

*Depends on: Stories 9A.19 and 9A.35. Audit: A5, A6, A11.*

*Deletes:*
- *`WorkshopBattery` (`engine/gpu/WorkshopBattery.cs:18`) and `WorkshopInput.Battery`;*
- *`ui/WorkshopBattery.cs` (whole file) and the Battery panel in `ui/WorkshopConfiguration.cs`;*
- *the `PartId.Battery` visibility rule (`ui/WorkshopGuidance.cs:273`) and `BrowserWorkshopClient.Electrical.cs:29`;*
- *`WorkshopBattery` in the whitelist.*

As a player,  
I want to enable or disable a Battery through the generic editor, including during a run,  
So that power sources are selected by declaration.

**Acceptance Criteria:**
- **Given** a Battery wired to a Powered Gate, **When** I toggle its declared boolean `enabled` parameter in the editor, **Then** a typed `ConfigureElectrical` command with a receipt changes the supply and the gate responds.
- **And** a disabled battery powers nothing (negative control).
- **And** the editor offers this one parameter during Run because the declaration marks it run-editable, not because of an element check.
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-005`, `cat-005-n19`, `cat-051`, `cat-051-interrupt` and `cat-037` pass.
  - Before catalogue E2E, the native guard is LatchSupplyFixtureTests (Story 9.4c); the browser suites remain pending.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.40: Generic Instance — Pressure Plate (CAT-052)
*Status: Backlog. Merged into C-Plate (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Stories 9A.20 and 9A.35. Audit: A5, A6.*

*Deletes: `WorkshopPressurePlate` (`engine/gpu/WorkshopPressurePlate.cs:5`), `WorkshopInput.PressurePlate`, `OfType<WorkshopPressurePlate>` in the compiler, and `WorkshopPressurePlate` in the whitelist.*

As a player,  
I want the plate's minimum mass set through the generic editor,  
So that the load sensor is fully declared.

**Acceptance Criteria:**
- **Given** a plate whose `minimum_mass` is set to 2.0 kg through the generic editor in actual Chrome (retro-verification #13: there was no editor), **When** a 4 kg Bowling Ball rests on it, **Then** the plate activates. A 1 kg Basketball does not (negative control), and values outside 0.1–16 kg are refused by the editor.
- **And** Run/Reset and Save/Load are exact.
- **And** `cat-052` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.41: Generic Instance — Pulse Counter (CAT-020)
*Status: Backlog. Merged into C-Memory (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Stories 9A.22 and 9A.35. Audit: A5, A6.*

*Deletes: `WorkshopCounter` and `DefaultTargetCount` (`engine/gpu/WorkshopCounter.cs:5`), `WorkshopInput.Counter`, `OfType<WorkshopCounter>` in the compiler, the counter arm in `ActivationNetwork.Compile`, and `WorkshopCounter` in the whitelist.*

As a player,  
I want the Counter's target set through the generic editor,  
So that counting is selected by declaration.

**Acceptance Criteria:**
- **Given** a Counter with target N = 3, **When** three balls cross its detector, **Then** it emits once on the third (a `ThresholdReached` ring record). Two crossings emit nothing (negative control).
- **And** in actual Chrome, the generic editor sets the target to 1 and to 9 (the boundaries) and refuses 0 and 10 (retro-verification #13: there was no editor). A target of 1 emits on the first crossing.
- **And** Reset clears the count.
- **And** Save/Load is exact.
- **And** `cat-020` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.42: Generic Instance — System Clock (CAT-017)
*Status: Backlog. Merged into C-Clock (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Story 9A.38. Audit: A5, A6, A11.*

*Deletes: `WorkshopClock` and `DefaultIntervalSeconds` (`engine/gpu/WorkshopClock.cs:5`), `WorkshopInput.Clock`, the Clock panel in `ui/WorkshopConfiguration.cs`, `OfType<WorkshopClock>` in the compiler, the clock arm in `ActivationNetwork.Compile`, and `WorkshopClock` in the whitelist.*

As a player,  
I want the Clock's interval set through the generic editor,  
So that periodic pulses are selected by declaration.

**Acceptance Criteria:**
- **Given** a 1.0 s Clock, **When** it runs for 5.0 s, **Then** it emits exactly 5 pulses.
- **And** Pause halts emission and Reset restores phase 0 (control).
- **And** Save/Load is exact.
- **And** `cat-017` passes.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.43: Generic Instance — State Latch (CAT-037)
*Status: Backlog. Merged into C-Memory (21 Oct 2026). These ACs stay here and apply there.*

*Depends on: Stories 9A.22 and 9A.27–9A.42; it is the last record conversion, so it removes the whole whitelist. Audit: A5, A6.*

*Deletes:*
- *`WorkshopLatch` (`engine/gpu/WorkshopLatch.cs:7`) and `WorkshopInput.Latch`;*
- *the latch-only Latched→Clear admission (`is WorkshopLatch` at `engine/gpu/WorkshopSimulation.cs:57` and `:459`), replaced by a declared memory capability;*
- *`OfType<WorkshopLatch>` in the compiler and the latch arm in `ActivationNetwork.Compile`;*
- *with the last record gone, the whole `WorkshopInstances.Validate` type whitelist (`engine/gpu/WorkshopInstances.cs:95`).*

As a player,  
I want the State Latch to hold memory, switch supply and drive its ActivationOut exactly as before,  
So that the last element record leaves the engine.

**Acceptance Criteria:**
- **Given** a latch with Set, Reset, supply and ActivationOut wired through real UI, **When** Set and Reset pulses arrive, **Then** memory, the switched supply and the output follow Story 9.4's acceptance.
- **And** same-tick Set and Reset resolve to Reset (control).
- **And** a committed Latched→Clear flip is admitted only for StateLatch, LevelIndicator and ContactLoadSource nodes (the node kinds a declared memory, a level-wired indicator and a declared contact load compile to), and refused for every other node with `Faulted`/`InvalidRead` (negative control). There is no generic Clear command (coordinator decision, 24 Oct 2026, superseding the 23 Oct wording).
- **And** Run/Reset and Save/Load are exact.
- **And** `rg "Workshop(Ball|Receiver|Ramp|Switch|Lamp|Wall|Delay|Bumper|Domino|Battery|Springboard|PressurePlate|Counter|Clock|Latch|BallDetector)\b"` returns 0 production hits.
- **And** `cat-037`, `cat-005-n19` and `cat-051-interrupt` pass.
  - Before catalogue E2E, the native guard is LatchSupplyFixtureTests (Story 9.4c); the browser suites remain pending.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.44: f32 Lane Completion — Remove Every binary16 Lane
*Status: Part A (every binary16 lane, and the deprecated `raw_assistance` save slot deleted) done, published at main d87b6a1e (CI 40181624977, 3,952 tests, 0 failed, 0 skipped; deployment 7311595713). Part B (`double` game values, the integer-tick clock and a `double` guard) is [Story 9A.44B](#story-9a-44b), which owns those clauses. Originally position 50 (Delivery order, 21 Oct 2026), after C-Memory, 9A.23 + 9A.25b and 9A.26. Story 9.5a had already moved the timer lane (`ActivationTimers.cs`) to f32.*

*Depends on: Story 9A.43. Retro-verification X2 and #17 found 675 `Half` lines added since baseline `9107148` (458 production, 217 test) by Stories 6.6–9.4; all of them are in scope. It is placed after the stories that delete the largest `Half` holdings (`WorkshopCanonicalColliders` in 9A.16, `MachinePart` bindings and `CosmeticCurves` in 9A.25, the puzzle lanes in 9A.26) so that no lane is migrated and then deleted. Owner decision, 18 Oct 2026. Audit: B6. Resolves CAT-037 open question 2.*

*Deletes every binary16 lane, with no conversion shim:*
- *`System.Half` in every production and test `.cs` file (69 production files at audit time). This includes:*
  - *`Metres`, `Kilograms`, `Acceleration`, `InverseSeconds`, `Restitution` and `HalfBits` (`engine/gpu/WorkshopConstruction.cs:11-15`, `:335`);*
  - *`PhysicsDeclarations.cs`, `RigidMassProperties.cs`, `OrientationSensorDeclaration.cs`, `PhysicsGpuAbi.cs`, `PhysicsMotionRead.cs` and `WorkshopPhysicsCompiler.cs`;*
  - *`WorkshopSaveCodec.cs`, `ActivationTimers.cs`, `ActivationNetwork.cs` (`(Half)0` at `:385`), `ContactWorkDeclaration.cs`, `WorkshopPuzzle.cs` and `WorkshopPuzzleWire.cs`;*
  - *`engine/presentation/AnimationValues.cs`, `WorkshopCosmetic.cs`, `WorkshopHint.cs` and `ContactTriggerSettings`;*
  - *the port and pose vectors.*
- *`decodeFloat16` in `CuriousContraptions.web/wwwroot/workshop-client.js` and any float16 decode in `tools/e2e/workshop-wire.ts`;*
- *every half-width field in `schemas/*.fbs`.*

As a player,  
I want every game value stored and computed in one precision,  
So that sizes, masses, timings and animations behave identically everywhere.

**Acceptance Criteria:**
- **Given** executable production source and current schema inputs, **When** the architecture guard runs, **Then** no binary16 type/API/lane remains (the `double` game-value guard is Story 9A.44B's). Exclude documentation, historical evidence, third-party code and committed malformed/legacy input fixtures with named reasons; never erase those to satisfy a token scan. Test actual forbidden type/API use and a permitted external adapter as positive/control guard cases.
- **And** every former lane carries IEEE-754 f32 end to end: authoring, construction, wire, save, solver, read model and animation.
- **And** there are no format versions (owner, 21 Oct 2026). Structurally invalid saves are rejected explicitly, and there's no migration.
- **And** the `docs/gpu-f32-physics.md` "f32 Migration Status" table records no remaining lane, and the envelope statements that depended on binary16 are restated for f32.
- **And** independently derived behavioral unit/integration tests (no output-hash or byte-identity assertions) show every element's outcomes stay inside the [game-grade envelope](../../docs/gpu-f32-physics.md#game-grade-envelope).
- **And** every compiled record field touched by the f32 relayout (bodies, colliders, sensors, prismatics, electrical, motion and cache) is read and written only through named `PhysicsGpuAbi` offset constants. `WorkshopAbiLiteralScanTests` is extended from Story 9.4b's header, contact-work, occurrence and trigger records to every record type, and fails on any literal ABI offset in production. Coordinator decision, 19 Oct 2026, Story 9.4b R2-F4: the offsets change in this story anyway, so naming them here migrates each one exactly once.
- **And** an injected non-finite value is rejected at the boundary (negative control).
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

### Story 9A.45: Compile-Time Element Isolation Gate
*Status: Backlog. Position 54, the last deletion story and the final empty-list gate (21 Oct 2026). Story 9A.45a delivered the early ratchet at position 17. This story moves that scan into the build, requires its known-violations list to be empty, and deletes the list mechanism.*

*Depends on: Stories 9A.1–9A.44. Audit: A1, A2 (constants).*

*Deletes:*
- *every element-named `PartId` constant (`engine/PartId.cs:20-44`);*
- *the wholesale `Compile Include="../engine/gpu/*.cs"` and the `engine/PartId.cs` link in `CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj:28` and `CuriousContraptions.Animation.Worker/CuriousContraptions.Animation.Worker.csproj:28`;*
- *remaining `WorkshopPipe`/`PipeLength`, its factory, codec/CaptureInstance/whitelist arms and obsolete callers/fixtures: convert Pipe to declared generic parameters while preserving geometry, joins, controls and lifecycle; no compatibility path;*
- *any element-specific production code still found by the scan below.*

As the owner,  
I want the build to fail when anyone (person or agent) adds element-specific code outside `elements/`,  
So that project-per-element, command/query and the event ring stay the only possible design.

**Acceptance Criteria:**
- **Given** the solution, **When** it builds, **Then** both workers reference a new element-free `CuriousContraptions.Core` assembly (capability tables, solver contracts, the capability-table codec, the activation network and the event ring) instead of compiling `engine/gpu` wholesale. Authoring, catalogue, lessons and the bundle loader are in a main-thread-only assembly that neither worker references.
- **And** `PartId` is an opaque typed id with no named members, so `PartId.Wall` no longer compiles.
- **And** the `BundleElements` build target also runs an isolation scan that fails the build, citing file and line, for any of the following in production source outside `elements/**`:
  - any bundle element id as a string literal;
  - any `PartId.<Name>` member access;
  - an identifier built from a package name (`Workshop<Package>`, `<Package>Settings`, `<Package>Dimensions`, `<Package>Lessons`, `CosmeticCurves.<Package>`).
  
  The id list comes from the generated bundle; there is no hand list.
- **And** the scan covers `engine/**`, `ui/**`, every `CuriousContraptions.*/**` project except `CuriousContraptions.tests`, `CuriousContraptions.web/wwwroot/**` and `CuriousContraptions.Simulation/wwwroot/**`.
- **And** the scan explicitly excludes, with these reasons:
  - `elements/**`: the owners;
  - `CuriousContraptions.tests/**` and `tools/e2e/**`: consumers that build machines by id or through the UI;
  - `content/**`: level data names elements by design;
  - `tools/Coverage/**`: a planning-coverage tool over all 72 catalogue specs that does not ship;
  - `docs/**`, `_bmad-output/**` and `archives/**`.
- **And** a test plants an element-id literal and, separately, a `Workshop<Package>` type in a temporary engine file, and the build fails with file:line for each (negative control). The unchanged tree builds clean (positive).
- **And** the Story 9A.0 consistency test now asserts that the isolation doc's Current status section lists no open finding.
- **And** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test CuriousContraptions.slnx` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview.

---

## Epic 10: Constrained Mechanics & Elastic Fixtures

**Goal:** Deliver the solid cylinder collider and inertia, compliant elastic fixtures, pivots, pulleys, ropes, and counterweights using unified TGS Soft constraints without ad-hoc velocity projections or Baumgarte stabilization.

### Story 10.1: Solid Cylinder Collider & Cylinder Inertia (ENGINE-CYLINDER)
*Status: Done natively (reconciled 21 November 2026 against sprint key `10-1-solid-cylinder-collider-and-inertia`). Browser qualification pending.*

*Depends on: Stories 1.4 (BVH and SAT narrowphase) and 5.1 (dynamic rigid body and inertia compile). New story (owner decision, 9 Oct 2026), placed before its first consumer, the Story 10.2 Pulley wheel; the owner chose cylinders for both the Pulley wheel and the Story 10.3 Lever fulcrum (Open scheduling item e, decided 18 Oct 2026). ENGINE-CYLINDER is a roadmap-only ID, not yet in `docs/planning/requirements.md`.*

As a player,  
I want shaft wheels, rollers and pivot posts to collide and turn as true solid cylinders,  
So that wheeled and pivoted parts move with their real shape and mass.

**Acceptance Criteria:**
- **Given** a solid cylinder declared by radius, width and mass, built through real UI controls on its first consuming element or on an explicitly selectable authored qualification fixture,
- **When** a ball strikes its curved side and its flat end, and a dynamic cylinder falls onto the workbench,
- **Then** contact follows the true curved side and flat end faces against spheres, boxes and the workbench plane, with no sphere, box or prism-hull stand-in.
- **And** the dynamic cylinder rolls on its side and rests on an end face without jitter.
- **And** cylinder inertia is compiled from the declaration: axial $m r^2 / 2$, transverse $m (3 r^2 + w^2) / 12$.
- **And** the collider shape set gains Cylinder as a closed enum value end-to-end, and a non-positive or non-finite radius, width or mass is rejected atomically before any state changes.
- **And** this is engine capability only, delivered as an engine slice or through one consuming element's slice; no element identifier appears in the physics solver.
- **And** it serves the shaft wheels of CAT-042 Motor (`docs/planning/elements/CAT-042-motor.md`), CAT-019 Conveyor (`docs/planning/elements/CAT-019-conveyor.md`), CAT-018 Clutch (`docs/planning/elements/CAT-018-clutch.md`), CAT-057 Reverse transmission (`docs/planning/elements/CAT-057-reverse_transmission.md`) and CAT-071 Wound spring (`docs/planning/elements/CAT-071-wound_spring.md`), the CAT-053 Pulley wheel (`docs/planning/elements/CAT-053-pulley.md`) and the CAT-034 Impact lever solid fulcrum (`docs/planning/elements/CAT-034-impact_lever.md`) (owner decision, 18 Oct 2026); and, if the owner selects a cylinder for them, the CAT-067 Weight body (`docs/planning/elements/CAT-067-weight.md`) and CAT-069 Wind chimes tubes (`docs/planning/elements/CAT-069-wind_chimes.md`).
- **And** the legacy 24-sided prism-hull shaft (`engine/SceneRotaryShaft.cs`) is deleted, or confirmed already purged by Story 7.4; no hull fallback is introduced.
- **And** exact Reset and Save/Load restore the cylinder poses.
- **And** `tools/e2e/engine-cylinder.test.ts` passes 100% in Chrome serially.

### Story 10.2: Pulley Wheel & Tensile Rope Dynamics (CAT-053, CAT-058)
*Status: Aggregate only; children 10.2a, 10.2b and 10.2c are delivered on main. Browser qualification pending.*

*Depends on: Story 10.1 (cylinder wheel collider; owner decision 18 Oct 2026, CAT-053 open question 1). Moved from Story 10.3, ahead of the Lever whose end rope sockets need the ropes. The Counterweight body (CAT-067) ships with Story 10.2 per CAT-067 §3 and is the rope load in this acceptance (Open scheduling item f, decided 18 Oct 2026); the Lever's rope-load integration is proven in Story 10.3.*

*Split (24 Oct 2026): 10.2a Weight; 10.2b Rope Anchor, rope domain, Weight Tie/Load and required cutting; 10.2c Pulley. Parent acceptance is aggregate-only and closes with all children. The detailed independent contracts below govern dispatch.*

As a player,  
I want to route ropes around Pulleys to transmit tensile forces,  
So that dropping one weight can lift an object in a different part of the workbench.

**Acceptance Criteria:**
- **Given** a rope routed through two Pulleys between two Counterweights of unequal mass,
- **When** the heavier counterweight falls under gravity,
- **Then** tensile forces transmit through the rope, lifting the lighter counterweight; equal masses stay balanced.
- **And** slack ropes apply zero force; cutting or releasing tension uncouples the bodies.
- **And** `tools/e2e/cat-053-058.test.ts` passes 100% in Chrome serially.


### Story 10.2b: Rope Anchor, tensile rope and cutting (CAT-058)
*Sprint key: `10-2b-rope-tie-load`; delivered under native verification; entry decisions resolved in [CAT-058](../../docs/planning/elements/CAT-058-rope_anchor.md). Depends on 10.2a and stabilized generic constraint engine; does not depend on Pulley.*

Given two delivered Weight bodies and a fixed Rope Anchor constructed through native authoring, when a declared rope becomes taut, then tensile-only constraint work couples admitted Tie/Load endpoints; slack applies zero force and equal-load controls remain balanced. Given a rope at rest or under tension, when the typed cut/release command succeeds, then its constraint is removed atomically at the committed boundary and subsequent motion is uncoupled; repeat/unknown cuts return a typed refusal without changing other ropes. Given Reset or supported Save/Load, when restoring the authored machine, then original endpoints, length and uncut authored construction are restored and rerunning reproduces behavior. Cut is required, not an open scope question; determine its allowed user phase and exact command/read schema before implementation with the source owner. The source length-authoring, compliance and slack-boundary questions are finite entry decisions, not guessed constants. Native tests cover zero/maximum admitted length, incompatible/missing endpoints, slack/taut crossing, cut before/after load and multiple independent ropes, with gameplay-derived bounds and assertion review; mutation runs remain deferred. Files: elements/RopeAnchor, Weight declared ports, mechanical Abstractions, generic constraint compile/solver, command/query, native tests/content; delete superseded rope paths, no element-specific solver.

### Story 10.2c: Pulley wheel and routed rope (CAT-053)
*Sprint key: `10-2c-pulley-wheel`; depends on 10.1 cylinder and10.2b.*

Given two pulleys routing a rope between unequal delivered Weights, when the heavy load falls, then the light load rises through the source tensile/work law; equal masses balance, slack transmits no force, cutting uncouples and pulley routing obeys length/contact boundaries. Preserve every CAT-053 variant and exact source geometry. Prove all parent10.2 cross-element criteria, native Reset/save/load and atomic rejection, then update10.2b's pending pulley integration proof. Files: elements/Pulley, necessary generic routing/constraint declarations and tests/content; no bespoke loop or capacity.

### Story 10.3: Balanced Impact Lever & Pivot Fulcrum (CAT-034)
*Status: Done natively (reconciled 21 November 2026 against sprint key `10-3-balanced-impact-lever-pivot-fulcrum`). Browser qualification pending.*

*Depends on: engine-supported-impact-restitution native acceptance, Stories 6.2 (Bumper obstacle), 10.1 (solid cylinder fulcrum; owner decision 18 Oct 2026, CAT-034 open question 2) and 10.2 (ropes for its end rope sockets; CAT-034 §3). Moved from Story 10.2 (owner dependency rule, 9 Oct 2026: after ropes). It carries the Lever rope-load integration moved out of Story 10.2 (Open scheduling item f, decided 18 Oct 2026).*

As a player,  
I want an Impact Lever to pivot smoothly when struck on one arm,  
So that see-saw mechanisms can fling objects or lift connected ropes.

**Acceptance Criteria:**
- **Given** an Impact Lever with a payload resting on one arm,
- **When** a heavy Bowling Ball drops onto the opposing arm,
- **Then** the lever pivots around its fulcrum, strikes its end stop, and launches the payload upward.
- **And** an equal load placed on both arms remains balanced indefinitely.
- **And** the lever pivots on a solid cylinder fulcrum (Story 10.1), not an art-only post.
- **And** with a rope routed through two Pulleys from a falling Counterweight to a lever end rope socket, rope tension lifts that lever arm (the rope-load integration moved from Story 10.2).
- **And** `tools/e2e/cat-034.test.ts` passes 100% in Chrome serially.

*Open item (owner decision, 1 November 2026): the indefinite equal-load balance is reopened. The hinge is frictionless, so a separate engine slice adds a declared pivot holding mechanism; see [deferred work](../implementation-artifacts/deferred-work.md#lever-equal-load-indefinite-balance).*

### Story 10.4: Counterweight & Multi-Body Pendulum Lifting (CAT-067)
*Status: Done natively (reconciled 21 November 2026 against sprint key `10-4-counterweight-multibody-pendulum-lifting`). Browser qualification pending.*

*Depends on: Story 10.2 (rope anchor and tension-only rope). Number unchanged; the Pulley moved up to Story 10.2 and the Trampoline now follows this story.*

*The Weight body, its mass parameter and its rest, roll, stack, Reset and Save/Load proof ship with Story 10.2a; this story is pendulum qualification only.*

As a player,  
I want Counterweights to swing as pendulums and balance moving loads,  
So that gravitational energy can be harnessed smoothly.

**Acceptance Criteria:**
- **Given** a Counterweight suspended as a 3D pendulum,
- **When** released from an angle,
- **Then** it oscillates with natural period $T = 2\pi\sqrt{L/g}$ and damps gradually.
- **And** exact Reset restores initial angular displacement and zero velocity.
- **And** `tools/e2e/cat-067.test.ts` passes 100% in Chrome serially.

### Story 10.5: Trampoline Compliant Membrane Dynamics (CAT-065)
*Status: Done natively (reconciled 21 November 2026): `elements/Trampoline` is on main and [its record](../implementation-artifacts/spec-10-5-trampoline.md) closed after review loop 5. Browser qualification pending.*

*Depends on: Stories 6.4 (elastic spring constraint), 6.6–6.7 (pipe for the drop-to-pipe lesson), 10.2 (ropes) and 10.4 (the Weight for the tether controls; CAT-065 §3). Moved from Story 10.1 (owner decision, 9 Oct 2026: after ropes and the Weight).*

As a player,  
I want dropped objects to bounce high off a Trampoline,  
So that contraptions can launch payloads across vertical obstacles.

**Acceptance Criteria:**
- **Given** a Trampoline placed under a falling Basketball,
- **When** impact occurs,
- **Then** the membrane deflects with compliant stiffness and launches the ball upwards.
- **And** off-center hits deflect at an angle according to surface normal.
- **And** total kinetic energy after rebound does not exceed initial potential energy (no artificial gain).
- **And** `tools/e2e/cat-065.test.ts` passes 100% in Chrome serially.

---

## Epic 11: Rotary Drive & Mechanical Actuators

**Goal:** Implement electric motors, conveyors, clutches, reversing transmissions, linear pushers, wound springs, and toy cannons as declarative actuators.

### Story 11.1: Electric Motor & Continuous Tangential Conveyor Belt (CAT-042, CAT-019)
*Status: Aggregate only; children 11.1a (Motor) and 11.1b (Conveyor) are delivered on main. Browser qualification pending.*

As a player,  
I want an Electric Motor to drive a Conveyor Belt,  
So that resting objects are transported horizontally across the workbench.

**Acceptance Criteria:**
- **Given** an Electric Motor coupled to a Conveyor Belt receiving 12V battery power,
- **When** a Basketball lands on the conveyor surface,
- **Then** tangential friction accelerates the ball along the belt travel direction.
- **And** cutting motor power decelerates the belt to a halt under friction.
- **And** (integration moved from Story 8.1, owner decision 18 Oct 2026) a Battery connected to the Motor by an electrical cable supplies continuous 12V and spins its axle, and losing that supply stops the motor drive immediately.
- **And** `tools/e2e/cat-042-019.test.ts` passes 100% in Chrome serially.

### Story 11.2: Mechanical Clutch & Reverse Transmission (CAT-018, CAT-057)
*Status: Aggregate only; children 11.2a (Clutch) and 11.2b (Reverse transmission) are delivered on main. Browser qualification pending.*

As a player,  
I want to engage clutches to connect axles and reverse transmissions to invert rotation,  
So that complex mechanical drivetrains can be governed.

**Acceptance Criteria:**
- **Given** a motor driving a shaft through a Reverse Transmission and Clutch,
- **When** the clutch is engaged, the output shaft rotates in the reversed direction.
- **When** the clutch is disengaged via signal, the output shaft freewheels to a stop.
- **And** `tools/e2e/cat-018-057.test.ts` passes 100% in Chrome serially.

### Story 11.3: Linear Pusher Telescopic Actuator (CAT-039)
*Status: Done natively (reconciled 21 November 2026 against sprint key `11-3-linear-pusher-telescopic-actuator`). Browser qualification pending.*

As a player,  
I want a Linear Pusher to extend upon signal activation,  
So that objects in front of it are forcibly pushed forward.

**Acceptance Criteria:**
- **Given** a Linear Pusher with configured stroke 1.0 m wired to a switch,
- **When** the switch activates,
- **Then** the pusher head extends at rated speed and pushes a resting Bowling Ball forward.
- **And** upon reaching maximum stroke, extension halts and holds position.
- **And** `tools/e2e/cat-039.test.ts` passes 100% in Chrome serially.

### Story 11.4: Wound Spring Potential Motor & Toy Cannon Launcher (CAT-071, CAT-016)
*Status: Aggregate only; children 11.4a (Wound spring) and 11.4b (Cannon) are delivered on main. Browser qualification pending.*

*Depends on: Stories 6.4–6.7 (slider, preload store and annular colliders), 8.1, 10.1 (shaft cylinder), 11.1 and 11.2 (shaft drive and ratio coupling); the cannon feeder integration also uses Stories 8.2, 9.3 and 9.4. Renumbered from Story 11.5 after the Powered Gate moved to Story 8.2.*

As a player,  
I want to fire Toy Cannons and wind up Spring Motors,  
So that high-energy mechanical impulses can launch balls across wide gaps.

**Acceptance Criteria:**
- **Given** a loaded Toy Cannon triggered by an activation wire,
- **When** triggered,
- **Then** the same loaded ball launches along the physical barrel axis from finite stored work, with a 12 m/s release cap. Owner decision, 18 November 2026: defaults remain 45 J capacity and 30 W charging, so a stationary free 1 kg payload is energy-limited to approximately 9.49 m/s; 12 m/s is not a guaranteed default speed.
- **And** uncharged or unloaded cannons fire nothing.
- **And** the separate 11.4a/11.4b slices pass their production native unit/integration and independent review gates. Actual Chrome construction, configuration, intended/control behavior and lifecycle proof remains pending at the post-catalogue browser gate under current owner policy.

---

## Epic 12: Conserved Airflow, Pneumatics & Buoyancy

**Goal:** Deliver aerodynamic airflow fields, pneumatic pressure pulses, and buoyant gas dynamics conforming strictly to energy conservation laws.

### Story 12.1: Lightweight Tennis Ball & Floating Balloon Buoyancy (CAT-064, CAT-003)
*Status: Aggregate only; children 12.1a (Tennis ball) and 12.1b (Balloon) are delivered on main. Browser qualification pending.*

*Note (18 Oct 2026, Open scheduling item h): Story 9A.14 removed Tennis and Balloon from the palette because neither could be constructed and neither had a package. This story creates their `elements/TennisBall` and `elements/Balloon` packages (a declaration, scene, icon and parameter schema for each, in the bundle) and so returns them to the palette.*

As a player,  
I want lightweight Tennis Balls to bounce briskly and Balloons to float upward,  
So that aerodynamic and buoyant contraptions are possible.

**Acceptance Criteria:**
- **Given** a Floating Balloon placed on the workbench,
- **When** released,
- **Then** buoyant forces overcome gravity and the balloon rises vertically until contacting a ceiling or wall.
- **And** a Tennis Ball rebounds with high restitution ($e = 0.85$) and decelerates under aerodynamic drag.
- **And** each part has its own authored `.tscn` scene. The Balloon is a pink gas envelope with a tied neck and a string, and the Tennis Ball is a felt-yellow ball with a seam curve. Neither uses `parts/scenes/ball.tscn`. The palette icon and the 3D model match (owner report, 19 Oct 2026: the Balloon looked like a basketball).
- *Browser qualification:* pending at the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-064-003.test.ts` is run only then and gates nothing before it.

### Story 12.2: Fan with Selectable Supply and Conical Airflow (CAT-028 / EL-209)
*Status: Done natively and published (4378410e; status b5b9b233); final diff scope PASS, browser qualification pending. [Evidence](../implementation-artifacts/spec-12-2-fan.md).*

As a player,  
I want an Electric Fan to blow light objects across the workbench,  
So that contactless aerial steering can guide balls into goals.

**Acceptance Criteria:**
- **Given** an Electric Fan powered by a battery aimed across the path of a falling Tennis Ball,
- **When** the ball enters the fan's conical airflow cone,
- **Then** aerodynamic drag forces deflect the ball horizontally into the target receiver.
- **And** heavy Bowling Balls experience negligible deflection (mass-scaled drag).
- **And** the same catalogue Fan offers Self-contained (activation-controlled 14,400 J budget), Electrical and Shaft; ideal shaft conversion applies the paid reaction load with no additional conversion loss.
- **And** width is terminal cone radius; force-rated transfer is mass-dependent under global linear/angular bounds, without an extra 16 m/s² transfer cap.
- **And** real native production positive/control/boundary and lifecycle cases pass; actual Chrome construction/configuration/Run/Reset/save-load proof remains pending at the post-catalogue browser gate. No browser test runs in this slice.
- **Scope:** Windmill rotary capture/integration remains Story 12.3; full gas networks and later campaign qualification are not bundled. Motor electrical drain remains deferred and is not claimed by shaft-to-airflow proof.

### Story 12.3: Windmill Rotor Aerodynamic Capture & Drive (CAT-070)
*Status: Done natively and published (47e01bdb, on main 4f193bb2). Full7232/0/0, hooks134/0/0 and E9 PASS; browser/remote/release qualification unverified. See [native completion record](../implementation-artifacts/spec-12-3-windmill.md#auto-run-result).*

*Depends on: Stories 6.6–6.7 (annular guard collider), 10.3 (hinge), 11.1–11.2 (shaft output and loads) and 12.2 (airflow). Moved from Story 12.4, ahead of the Bellows that needs it.*

As a player,  
I want a Windmill to spin when struck by fan airflow,  
So that airflow can be converted into mechanical shaft work.

**Acceptance Criteria:**
- **Given** a Windmill positioned in the airflow of an active Electric Fan,
- **When** airflow strikes the rotor blades,
- **Then** the windmill spins its axle and delivers rotational torque to connected mechanical components.
- **And** blocking the fan airflow removes drive and the finite-inertia rotor passively coasts to rest; connected/disconnected cargo, signed rear drive and loaded shaft controls follow [CAT-070](../../docs/planning/requirements.md#current-cat-070).
- **And** required native production unit/integration and ordinary Reset/save/load checks pass now; `tools/e2e/cat-070.test.ts` Chrome construction/configuration/connection/motion proof remains required at the post-catalogue browser gate, not this slice's native gate.

### Story 12.4: Pneumatic Bellows Compression Pulse (CAT-010)
*Status: Backlog*

*Depends on: Stories 6.4, 11.1, 12.2 and 12.3 (delivered); the engine programme (owner decision 3a); `ENGINE-GAS-NETWORK`, the one owner of the chamber and nozzle law (the axial gas chamber on a prismatic coordinate, the directional nozzle with ambient refill, prismatic directional damping and the prismatic stroke observation); `ENGINE-CAPABILITY-INSTANCES` (the continuous prismatic-travel presentation driver, coordinator decision 29); and `ENGINE-JOINT-KINDS` (the declared spring rest position, owner decision 6). CAT-010 §3 and §4. Moved from Story 12.3 (owner decision, 9 Oct 2026: after Windmill and Conveyor).*

As a player,  
I want a Pneumatic Bellows to emit a focused gust of air when compressed,  
So that impacting bodies can trigger secondary pneumatic launches.

**Acceptance Criteria:**
- **Given** a Bellows struck by a falling Bowling Ball,
- **When** the plates compress,
- **Then** a high-velocity directional air jet discharges from the nozzle, propelling an adjacent balloon or tennis ball.
- **And** uncompressed bellows discharge zero airflow.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-010.test.ts` is written and run only then and gates nothing before it.

---

## Epic 13: Optical Propagation, Routing & Sensing

**Goal:** Implement raymarched optical propagation, planar reflections, spectral color bandpass filtering (RGB), and optical logic gates as a discrete, deterministic optical graph. The mechanical Beam Shutter joins this epic after the lasers and receivers it switches.

### Story 13.1: Flashlight Torch & Collimated Laser Emitters (CAT-029, CAT-036)
*Status: Backlog*

As a player,  
I want Flashlights to emit divergent light cones and Lasers to emit straight collimated beams,  
So that optical puzzles have both wide-area and pinpoint illumination sources.

**Acceptance Criteria:**
- **Given** a Flashlight and Laser placed in the Workshop,
- **When** powered,
- **Then** the laser projects a collimated beam of 16 m range ([CAT-036](../../docs/planning/elements/CAT-036-laser.md)); the flashlight projects a cone of 15° half-angle and 8 m range whose strength falls with distance ([CAT-029](../../docs/planning/elements/CAT-029-flashlight.md)).
- **And** opaque obstacles (walls, bodies) cast crisp geometric shadows blocking downstream transmission.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-029-036.test.ts` is written and run only then and gates nothing before it.

### Story 13.1b: Red Laser Emitter (EL-176)
*Status: Backlog*

*Depends on: Stories 8.1 (supply) and 13.1 (the collimated laser emission capability, CAT-036). New story (coordinator decision under owner delegation, 18 Oct 2026; Open scheduling items a and d). Per [CAT-036](../../docs/planning/elements/CAT-036-laser.md), EL-176 is a separate named identity with its own channel; it does not fold into the amber laser. This story introduces the declared emission-variant mechanism: a spectral channel value declared in the package and consumed by the shared laser emission capability. Stories 13.1c and 13.1d reuse it.*

As a player,  
I want a red laser,  
So that I can send a pure red beam to a matching receiver or filter.

**Acceptance Criteria:**
- **Given** a Red Laser (its own package, `elements/RedLaser`) placed and wired to a Battery through real UI controls (no setters or numeric placement),
- **When** it is powered,
- **Then** it projects a collimated beam carrying only the red channel, drawn in the approved-palette red per DESIGN.md.
- **And** the red channel is a declared emission variant of the shared laser emission capability. No new solver code is added, and `rg` finds no channel- or element-specific branch in the optical solver.
- **And** an unpowered or disabled supply produces no beam, and an opaque Wall blocks the beam (negative controls).
- **And** Run/Reset restores the unpowered emitter exactly, and Save/Load preserves pose, channel and wiring.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/el-176.test.ts` is written and run only then and gates nothing before it.

### Story 13.1c: Green Laser Emitter (EL-177)
*Status: Backlog*

*Depends on: Story 13.1b (the declared emission-variant mechanism). New story (coordinator decision under owner delegation, 18 Oct 2026; Open scheduling items a and d). A separate named identity per CAT-036.*

As a player,  
I want a green laser,  
So that I can send a pure green beam to a matching receiver or filter.

**Acceptance Criteria:**
- **Given** a Green Laser (its own package, `elements/GreenLaser`) placed and wired to a Battery through real UI controls,
- **When** it is powered,
- **Then** it projects a collimated beam carrying only the green channel, drawn in the approved-palette green per DESIGN.md.
- **And** the package reuses Story 13.1b's declared emission-variant mechanism with the green channel value. No optical solver or emission-capability source file changes.
- **And** a Red Laser on the same bench keeps its red channel (control), and an unpowered supply or an opaque Wall leaves no beam downstream (negative controls).
- **And** Run/Reset restores the unpowered emitter exactly, and Save/Load preserves pose, channel and wiring.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/el-177.test.ts` is written and run only then and gates nothing before it.

### Story 13.1d: Blue Laser Emitter (EL-178)
*Status: Backlog*

*Depends on: Story 13.1b (the declared emission-variant mechanism). New story (coordinator decision under owner delegation, 18 Oct 2026; Open scheduling items a and d). A separate named identity per CAT-036.*

As a player,  
I want a blue laser,  
So that I can send a pure blue beam to a matching receiver or filter.

**Acceptance Criteria:**
- **Given** a Blue Laser (its own package, `elements/BlueLaser`) placed and wired to a Battery through real UI controls,
- **When** it is powered,
- **Then** it projects a collimated beam carrying only the blue channel, drawn in the approved-palette blue per DESIGN.md.
- **And** the package reuses Story 13.1b's declared emission-variant mechanism with the blue channel value. No optical solver or emission-capability source file changes.
- **And** Red and Green Lasers on the same bench keep their channels (control), and an unpowered supply or an opaque Wall leaves no beam downstream (negative controls).
- **And** Run/Reset restores the unpowered emitter exactly, and Save/Load preserves pose, channel and wiring.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/el-178.test.ts` is written and run only then and gates nothing before it.

### Story 13.2: Broadband & Pure Channel Optical Receivers (CAT-038, CAT-056, CAT-032, CAT-012)
*Status: Backlog*

*Depends on: Stories 8.1 (switched contact), 8.2 (electrical load), 13.1 (laser source) and 13.1b–13.1d (the red, green and blue lasers are the pure sources for the pure-channel receivers; Open scheduling items a and d, decided 18 Oct 2026). Moved from Story 13.4 (owner decision, 9 Oct 2026: receivers before the mirror and filter stories that need them as observers).*

*Delivery split (coordinator decision 37, 23 November 2026):* CAT-038 is delivered as its own slice at its teaching level, 51; CAT-056, CAT-032 and CAT-012 are delivered with the rest of the Colour receivers family at their own teaching level. Each keeps its own acceptance rows here.

As a player,  
I want Optical Receivers that respond only to specific colors,  
So that color-tuned optical triggers can activate designated circuits.

**Acceptance Criteria:**
- **Given** a Red Receiver illuminated by red, green, and blue beams successively,
- **Then** only the red beam triggers electrical activation; green and blue produce zero response.
- **And** Broadband Receivers activate under any light exceeding power threshold.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-receivers-primary.test.ts` is written and run only then and gates nothing before it.

### Story 13.3: Beam Shutter Mechanical Guillotine (CAT-007)
*Status: Backlog*

*Depends on: Stories 6.4 (slider), 8.1, 8.2 (bounded slider drive), 10.2 (rope opening), 13.1 (laser) and 13.2 (light receiver); CAT-007 §3. Moved from Story 11.6 (owner decision, 9 Oct 2026: into Epic 13 after the lasers).*

As a player,  
I want a Beam Shutter to block or unblock light paths upon mechanical activation,  
So that optical circuits can be switched physically.

**Acceptance Criteria:**
- **Given** a Beam Shutter positioned across a laser path,
- **When** closed, the laser beam is blocked; when opened by signal or rope, the beam passes through cleanly.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-007.test.ts` is written and run only then and gates nothing before it.

### Story 13.4: Spectral Color Bandpass Filters (Red, Green, Blue) (CAT-055, CAT-031, CAT-011)
*Status: Backlog*

*Depends on: Stories 13.1 (laser source), 13.1b–13.1d (coloured beams for the pass and extinguish checks) and 13.2 (pure-channel receivers as observers). Moved from Story 13.3.*

As a player,  
I want Color Filters to filter out non-matching wavelengths,  
So that multi-chromatic light beams can be separated into pure primary colors.

**Acceptance Criteria:**
- **Given** a white light beam passing through a Red Filter,
- **When** the beam exits the filter,
- **Then** only red spectral energy is transmitted; green and blue are absorbed.
- **And** passing the red beam into a Green Filter completely extinguishes the beam (zero transmission).
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-filters.test.ts` is written and run only then and gates nothing before it.

### Story 13.5: Planar Reflection Mirror, Beam Splitter & Beam Combiner (CAT-041, CAT-008, CAT-006)
*Status: Backlog*

*Depends on: Stories 13.1 (laser), 13.2 (receivers as observers) and 13.4 (filters make the distinct channels the Combiner merges; CAT-006 §3). Moved from Story 13.2.*

As a player,  
I want Mirrors to bounce beams and Splitters/Combiners to divide and merge light paths,  
So that optical rays can be routed around obstacles.

**Acceptance Criteria:**
- **Given** a laser directed into a $45^\circ$ Flat Mirror,
- **When** the beam strikes the mirror surface,
- **Then** it reflects about the mirror's actual normal (within 0.5°), so a mirror at 45° turns the beam through 90°.
- **And** Beam Splitters halve each channel of the incident power (within 2 %); Beam Combiners merge orthogonal beams into one.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-041-008-006.test.ts` is written and run only then and gates nothing before it.

### Story 13.6: Secondary Spectral Receivers (Cyan, Magenta, Yellow, White) (CAT-021, CAT-040, CAT-072, CAT-068)
*Status: Backlog*

*Depends on: Stories 13.1, 13.2 (single-channel reception), 13.4 (filters) and 13.5 (Combiner mixing). Renumbered from Story 13.5.*

As a player,  
I want Cyan, Magenta, Yellow, and White receivers that require mixed spectral combinations,  
So that advanced color-combining puzzles can be constructed.

**Acceptance Criteria:**
- **Given** a Yellow Receiver,
- **When** red and green light both reach it, from one source or several,
- **Then** it activates; a Red-only or Green-only beam fails to activate it.
- **And** White Receiver requires all three channels, from one source or several.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-receivers-secondary.test.ts` is written and run only then and gates nothing before it.

### Story 13.7: Photovoltaic Solar Panel Energy Conversion (CAT-059)
*Status: Backlog*

*Depends on: Stories 8.1 (network), 11.1 (Motor load) and 13.1 (Flashlight). Renumbered from Story 13.6.*

As a player,  
I want Solar Panels to generate electrical power from incident light,  
So that optical beams can remotely power electrical motors and circuits.

**Acceptance Criteria:**
- **Given** a Solar Panel connected to an Electric Motor,
- **When** illuminated by a Flashlight or Laser,
- **Then** output electrical power energizes the motor.
- **And** blocking the light cuts the supply once the panel's light falls below its release level, from the next tick.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-059.test.ts` is written and run only then and gates nothing before it.

### Story 13.8: Optical Logic Gates (AND, NAND, NOR, OR, XOR) (CAT-043 through CAT-047)
*Status: Backlog*

*Depends on: Stories 13.1 (lasers), 13.2 (output observer) and 13.5 (optical routing). Renumbered from Story 13.7.*

As a player,  
I want Optical Logic Gates to perform contactless optical computations,  
So that speed-of-light logic contraptions can operate without electrical wires.

**Acceptance Criteria:**
- **Given** Optical logic gates (AND, NAND, NOR, OR, XOR) illuminated by laser inputs,
- **When** input combinations are tested,
- **Then** each gate relays a separate carrier at 90 % of its strength while its operation holds over the two input levels and absorbs it otherwise, matching its truth table for all four input combinations.
- **And** native tests run the production simulation for each criterion above, with a one-cause control and the boundary of each declared threshold, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-optical-logic.test.ts` is written and run only then and gates nothing before it.

---

## Epic 14: Acoustic Emission & Reception

**Goal:** Implement acoustic percussion, directional sound waves, and acoustic resonance sensors as discrete wave occurrences without audio thread blocking.

Delivery order (owner decision, 18 Oct 2026; Open scheduling item c): 14.0, 14.2, 14.1, 14.3, 14.4.

Story 14.0 (ENGINE-ACOUSTIC), the acoustic engine story, is written once in the [engine programme and capability stories](#engine-first-capability-stories-21-november-2026) section, at its order position; it keeps its number and sprint key `14-0-acoustic-propagation-occlusion-reception-engine`.

### Story 14.1: Sound Level Meter Threshold Sensor (CAT-060)
*Status: Backlog*

*Depends on: the engine programme (owner decision 3a); Stories 8.1 (supplied contact), 8.2 (a supplied load), 14.0 (acoustic propagation and reception) and 14.2 (the Service Bell, its acceptance source). Delivered after the Bell (owner decision, 18 Oct 2026; Open scheduling item c). Formerly Story 14.3.*

As a player,  
I want a Sound Level Meter to trigger an electrical contact when sound volume exceeds threshold,  
So that bells and speakers can activate remote circuits without physical wires.

**Acceptance Criteria:**
- **Given** a Sound Level Meter facing a Service Bell,
- **When** the bell rings,
- **Then** the sound meter closes its electrical contact and illuminates a connected lamp.
- **And** placing a solid Wall between bell and meter occludes the acoustic wave, preventing activation.
- **And** native tests run the production simulation through placement, wiring, Run, Reset and the committed read for each criterion above, with a one-cause control and the threshold boundary, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-060.test.ts` is written and run only then and gates nothing before it.

### Story 14.2: Service Bell Percussion & Resonant Ringing (CAT-009)
*Status: Backlog*

*Depends on: the engine programme (owner decision 3a); Story 14.0 (acoustic propagation, occlusion and reception; CAT-009 §3). Delivered before the Sound Level Meter (owner decision, 18 Oct 2026; Open scheduling item c). Formerly Story 14.1.*

As a player,  
I want Service Bells to ring with a crisp chime when struck,  
So that physical impacts can produce audible confirmation and trigger acoustic meters.

**Acceptance Criteria:**
- **Given** a Service Bell placed in the path of a rolling ball,
- **When** struck at an approach speed of 0.8 m/s or more,
- **Then** a crisp metallic ring is emitted as a timestamped acoustic event.
- **And** sub-threshold nudges produce no ring.
- **And** native tests run the production simulation for a strike just below and at 0.8 m/s, a resting contact and Reset, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-009.test.ts` is written and run only then and gates nothing before it.

### Story 14.3: Audio Speaker Directional Pulse Emission (CAT-061)
*Status: Backlog*

*Depends on: the engine programme (owner decision 3a); Stories 8.1 (supply), 9.3 (System Clock trigger), 14.0 (propagation and reception; CAT-061 §3) and 14.1 (the meter that observes it). Moved from Story 14.2 (owner decision, 9 Oct 2026: after propagation; 18 Oct 2026: propagation moved to Story 14.0).*

As a player,  
I want an Audio Speaker to emit directional acoustic pressure waves upon signal activation,  
So that electrical pulses can be broadcast acoustically.

**Acceptance Criteria:**
- **Given** an Audio Speaker wired to a system clock,
- **When** energized,
- **Then** directional acoustic pulses radiate forward in a cone of 35° half-angle ([CAT-061](../../docs/planning/elements/CAT-061-speaker.md)).
- **And** the trigger pulses of a tick are one request, coalesced into one emission served on the next tick; each emission is paid from a declared 2 J pulse store charged at 10 W, which gives a 0.2 s re-arm in seconds at every cadence; the store starts empty and Reset empties it; a request that finds the store below capacity, or the supply input unpowered in the serving tick, expires; the tone is a typed parameter (Low, Mid, High).
- **And** native tests run the production simulation: a meter inside the cone receives each pulse, one outside it or behind the speaker receives none, an unsupplied speaker is silent, and Reset clears pending emissions, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-061.test.ts` is written and run only then and gates nothing before it.

### Story 14.4: Resonant Wind Chimes Airflow Percussion (CAT-069)
*Status: Backlog*

*Depends on: the engine programme (owner decision 3a); Stories 12.2 (Fan), 14.0, 14.2 and 14.1; the spherical joint of `ENGINE-JOINT-KINDS`; `ENGINE-RUN-LENGTH` for the 60 s calm-and-rewind row; row 11 needs Story 12.4.*

As a player,  
I want Wind Chimes to sway and chime in fan airflow,  
So that wind-driven musical contraptions can be solved.

**Acceptance Criteria:**
- **Given** Wind Chimes placed in the airflow of an Electric Fan,
- **When** the airflow strikes the chime clapper,
- **Then** the clapper strikes a tube, producing musical acoustic events.
- **And** calm air produces zero ringing.
- **And** native tests run the production simulation with the Fan on and off and Reset, `Skipped: 0`.
- *Browser qualification:* deferred to the post-catalogue browser phase under AGENTS.md's browser exception; `tools/e2e/cat-069.test.ts` is written and run only then and gates nothing before it.

---

## Engine-first capability stories (21 November 2026)

**What this section is (rewritten 23 November 2026).** One current text per engine story, merged from the [engine design audit](../../docs/planning/elements/readiness/engine-design-gap.md) and every element readiness summary's proposals (`docs/planning/elements/readiness/`), under the [owner decisions of 22 and 23 November 2026](../../docs/planning/elements/readiness/owner-decisions-2026-11-22.md) and the [engine conformance standard](../../docs/planning/elements/readiness/engine-conformance.md). The [engine stories index](../../docs/planning/elements/readiness/engine-stories-index.md) holds the order table, the capability-to-story map, retired and renamed names and every contradiction reconciled here. A missing solver, ABI, wire or shared-presentation capability is its own engine slice before its first consumer (REQ-12; [capability probe](../../_bmad/custom/bmad-loop-protocol.md#capability-probe)); each story's planning scout re-runs that probe and narrows the story to what is still missing, never rebuilding what is present. Coordinator decisions 26 to 30 (23 November 2026) and the conventions repaired under them are applied in the stories they name; every capability an element spec marks missing or narrower has one owning story here, and none awaits an owner.

**What may run, and when (owner decisions 3a and 22).**
- **Before the engine programme, only repairs and tooling run:** visible-solid repair parts A, B, B2 and C, the Baseball repair, and structural stories S3, S5 and S6, with S5b and S6's follow-on slices S6b–S6g (owner decision 24h) and S5c-1, S5c-2 and S5c-3 after S5b (owner decision 24p; coordinator ruling, 26 November 2026). Under owner decision 24m (24 November 2026) the lane 3 slices (lettered level ids, then the content slice once its contract passes planning) and the lane 4 policy-tool fixes also run before the programme, in up to four lanes.
- **The engine programme** is stories 1 to 10 below, including 8a `ENGINE-WIRE-ABI` and 8b `ENGINE-WASM-THREADS` (owner decision 24k, 23 November 2026) and 8c `ENGINE-INSTANCED-RENDERING` (coordinator decision 40, 23 November 2026) between 8 and 9, in that order, before any further element delivery. "Orders 1 to 10" always includes 8a, 8b and 8c.
- **Everything else waits for the programme:** the five goal slices, Early level proofs, tests-only patches, closure reviews and new elements.
- **After the programme:** `ENGINE-GAS-NETWORK` → `ENGINE-LIQUID` → `ENGINE-THERMAL` (owner decision 18). Then element slices follow the order in which the campaign levels first teach them, whichever capability they need (owner decision 24a), and every other story here lands before the first element in that order that needs it.
- **Document order and execution order.** Stories appear in the order of owner decisions 3a and 18. Each story's *Depends on* line governs when it may start. Where a later-listed story is a dependency of an earlier one it runs first; the inversions are:
  - `ENGINE-SIGNAL-NODES` (threshold row), `ENGINE-JOINT-KINDS` (spring rest position), `ENGINE-PARAMETER-BOUND-FRAMES`, `ENGINE-RUN-LENGTH` and `ENGINE-CONNECTION-MATERIALS` (solid strand for hoses) are needed by the gas, liquid or thermal stories or by their first consumers, so they land before those stories.
  - `ENGINE-CONNECTION-MATERIALS` lands before `ENGINE-MECHANICAL-LINKS` and before the cutting clause of `ENGINE-TENSILE-EXTENSIONS`.
  - `ENGINE-THERMAL`'s radiant-heating row reads an optical receiver; it is delivered as an interface whose end-to-end proof lands in `ENGINE-OPTICAL`.

**Shared acceptance for every story in this section.**
- **Given** declaration data only (typed descriptors, parameters and ports), **when** the production engine compiles and advances it, **then** the capability's behaviour holds for at least two differently parameterised declared qualification fixtures. No element identity, kind or name appears in engine code. A story adds no element package; converting a delivered package's declaration to the story's new form in the same change is not adding one.
- **And** non-finite, out-of-range or unsupported declarations are refused atomically with a typed reason before any state changes; numerical residuals clamp and never fault the tick.
- **And** every conserved quantity the capability introduces is a finite ledger: nothing is created, lost or duplicated across a tick, Run/Reset or supported Save/Load, and Reset restores the authored state exactly.
- **And** native tests through defined interfaces cover a positive case, a one-cause control and the boundary of each declared threshold. Tolerances follow the [float rules](../../docs/testing-guidance.md#float-comparison-and-effort-rules) and the [game-grade envelope](../../docs/gpu-f32-physics.md#game-grade-envelope): each strict number names the gameplay failure it excludes; no hash, digest or byte-identity assertion; no exact float equality except authored values, counts and intentional zeros. Browser proof is post-catalogue.
- **And** tables are sized at Run from the compiled declarations, backing storage grows geometrically and interaction state is sparse (design contract D5). No story sets a fixed scene ceiling or reserves owner × target state; a typed refusal is admitted only for a per-declaration limit a package declares (for example a segment limit per emitter), never for a scene population. Per-tick work is bounded by the compiled declarations and the bodies inside declared regions, never by the whole scene. This replaces the "declared capacities, typed refusal at Run" wording of earlier proposals.
- **And** every spatial candidate comes from the query interface of `ENGINE-SPATIAL-QUERIES`; no story adds a scan of every body, collider, row or contact.
- **And** every rate, credit, per-tick sum and accumulator is written per committed tick of duration dt (1/120 s by default, 1/60 or 1/240 s by the Run's simulation cadence), never as a fixed 120 Hz; each domain step sits where the one [combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order) (optical convention 4) puts it, and a story cites that order rather than restating a different one (coordinator decision 26).
- **And** the wire schema, TypeScript callers and presentation read model the capability needs are part of the story, so the first consuming element is declarations, art, bindings and tests only.
- **And** the story names the legacy it deletes, and deletes it in the same change (forward-only).
- **And** open questions of the unblocked family that concern the shared model (units, scales, and the slot of a domain the combined tick order does not yet place) are decided in the story's Code Map by game intent, once, and recorded for every consumer; a new slot is added to the combined tick order, never stated as a second order.

**Performance criterion (owner decision 3a).** Every story marked *performance* states a measured before and after on the fixed reference scenes below and must not regress. The scenes are committed fixture files built from the engine audit's measurements (section 4, items 1 to 6):
- **R1** a grid of resting boxes, with collider count as the variable;
- **R2** a dense mixed-shape pile, measuring candidate pairs against contacts produced;
- **R3** a steady-state scene at stable topology, measuring managed bytes allocated per tick (`GC.GetAllocatedBytesForCurrentThread`);
- **R4** a fixed body count with a varying number of airflow sources, sensors, compliant surfaces and region capabilities, and of unrelated trigger, work, load and integrity rows (the contact-row extension that `ENGINE-CONTACT-OBSERVATIONS` measures, with rows visited per contact and contacts visited per row counted); a story that adds stores, streams, emitters or sources states them as its own extension of R4;
- **R5** one drive at its power limit and one airflow source loading a hinge, measuring substep replays per tick;
- **R6** the publication path per tick.

A performance comparison never uses a flag or setting that disables an engine feature: it measures the reference scene before the story lands and again after (coordinator decision 41, 24 November 2026). The asserted criterion in each slice is a deterministic counter (candidate pairs, collider visits per query, replays per tick, managed bytes per tick): it must not rise above the recorded before value on any scene the story touches. Wall-clock tick time is measured and recorded beside it, before and after, and must not regress on the declared development device; the absolute tick target (REQ-04, 2.5 ms p95) is confirmed at the P0-034 performance gate.

**Path shorthand.** `SIM/` = `CuriousContraptions.Simulation/`, `ABS/` = `CuriousContraptions.Abstractions/`, `GPU/` = `engine/gpu/`, `WEB/` = `CuriousContraptions.web/wwwroot/`, `ANIM/` = `CuriousContraptions.Animation.Worker/`, `FBS` = `schemas/workshop_wire.fbs`, `DOC` = `docs/gpu-f32-physics.md`. *Present today* evidence is the engine audit's, at published main `8d1d68aa`, unless stated otherwise; `ENGINE-WIRE-ABI`, `ENGINE-WASM-THREADS`, `ENGINE-INSTANCED-RENDERING` and the wire, worker-message and shared-memory clauses added to other stories on 23 November 2026 cite the [ABI audit](../../docs/planning/elements/readiness/audits/abi-interfaces.md) at published main `25df3cd5`.

### Engine programme (owner decision 3a): before any further element delivery

### Story DOC-ENGINE-STATUS: The documents describe the engine that exists
*Status: Backlog. Sprint key: `doc-engine-status`. Order 1. Documents only.*

*Outcome:* the authority documents stop stating absent features as present, so every element spec's conformance section cites real behaviour.

*Present today:* `DOC` states flat SoA tables (`DOC:63`, `:353`), 4-wide SIMD batching (`:111`, `:268`), an incremental dual Quad-BVH (`:192-197`, `:283-297`), DSU islands and sleeping (`:65`, `:270-273`, `:332-336`), work-stealing dispatch (`:274`), canonical pair order (`:352`), any-pair contact events (`:151`), optics in a phase before the substeps (`:169`) and a zero-copy pose ring (`:20-21`, `:76`); none is implemented (audit section 1). The delete-declaration-colliders slice (coordinator ruling 5) removed the kinematic body kind and its word at `DOC:145`. `docs/architecture/element-package-isolation.md:147` says no unmet invariant remains. `DOC:259` keeps a superseded supported-impact limitation. `docs/hollow-geometry-design.md:43` and the comments at `ABS/PhysicsDeclarationValues.cs:9-11` and `SIM/WorkshopCollisionKernels.cs:1996` call hollow colliders static only, while the solver has a dynamic-hollow branch (`SIM/WorkshopConstraintSolver.cs:161`).

*Contract:* every unmet claim of audit section 1 moves to the isolation authority's Current status with its owning story from this section; `DOC` states each as target, not fact. The tick-phase diagram states what runs where today: supply grants and airflow inside each substep (`SIM/WorkshopPhysicsEngine.cs:1010-1011`), one optical and one thermal step after the last substep with readings acting from the next tick (coordinator decisions 16 and 21); the target order of every domain is the one [combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order) (coordinator decision 26), which `DOC` cites rather than restates, and the supply phase fixed at 1/120 s at every cadence (`engine/gpu/ElectricalSupply.cs:9`) is recorded as open with `ENGINE-SUPPLY-DEMAND`. Whether a kinematic body is a supported kind is stated (no consumer needs one; the word is removed unless a story adds it). Same-build replay determinism and non-sphere tunnelling are recorded as unestablished with owners (`ENGINE-SPATIAL-QUERIES` for pair order; `ENGINE-TUNNEL-QUALIFICATION`).

*Contract, wire and threading (ABI audit; owner decisions 24j and 24k):* `DOC` and the isolation authority also state, each with its owner: the `SharedArrayBuffer` pose ring is written every tick and read by no production code, and the renderer is fed by the whole per-tick FlatBuffers read, posted and decoded on the main thread and again on the animation worker (`SIM/Program.cs:335-348`; `WEB/workshop-client.ts:152`, `:256-257`; owner `ENGINE-WIRE-ABI`); zero-copy between the C# runtimes is not reachable without WebAssembly threads, so each shared-memory exchange costs at least one JavaScript-mediated copy per side today (`SIM/wwwroot/worker.ts:286-290`; `WEB/pose-ring.ts:87-99`; owner `ENGINE-WASM-THREADS`); instanced draws do not exist, and each body is one node transform (`engine/MachinePart.cs:342-350`; owner `ENGINE-INSTANCED-RENDERING`, coordinator decision 40); the physics worker blocks in `Atomics.wait` for up to 2 ms every 100 ms (`WEB/clock-ring.ts:246`, `:270`; `GPU/WorkshopClockDerivation.cs:39-40`), recorded and changed only if R6 shows it matters; the FlatBuffers wire is the control plane and save, while every shared-memory and worker-message layout is hand-defined until `ENGINE-WIRE-ABI`. The pose ring is described as multi-buffered with its depth derived from tick rate and render delay (owner decision 24k), never as triple-buffered.

*Acceptance:* each `DOC` sentence about SoA, SIMD batching, Quad-BVH, dual tree, islands, sleeping, parallel dispatch, kinematic bodies, canonical pair order, any-pair contact events, tick phases, the pose ring and its reader, copies between runtimes, instanced draws and blocking waits on the physics worker either cites the running code path or is listed as open with its owning story; a reviewer reading `DOC` and the isolation authority finds no claim the code contradicts.

*Deletes:* the stale supported-impact text at `DOC:259`; the "no unresolved finding remains" sentence at `element-package-isolation.md:147`; the static-only hollow-collider statements; every statement that the renderer reads the pose ring or that the ring is zero-copy or triple-buffered.

*Depends on:* nothing. *Unblocks:* every element spec's engine-conformance section (all identities).

### Story ENGINE-SPATIAL-QUERIES: Persistent trees and one query interface for every phase
*Status: Backlog. Sprint key: `engine-spatial-queries`. Order 2. Performance story (R1, R2, R4). Corrects Story 1.4's statement that brute-force bounding loops were deleted.*

*Outcome:* one persistent broadphase answers every spatial question in the tick, so no capability scans the scene.

*Present today:* one scalar binary tree (`SIM/WorkshopDynamicBvh.cs:6-17`), cleared and refilled with every collider on every substep and every trial replay (`SIM/WorkshopPhysicsEngine.cs:808-863`; `SIM/WorkshopPhysicsEngine.Motors.cs:33`; `.ContinuousReplay.cs:22`; `.FiniteWork.cs:136`), inserted without rotations (`SIM/WorkshopDynamicBvh.cs:55-141`), fattened by the maximum speed (64 m/s × h plus spin reach plus 0.05 m, at least 0.18 m per side, `SIM/WorkshopPhysicsEngine.cs:810-822`); plane bounds assume a +Y normal (`:825-829`); pairs come in tree order with no canonical sort (`SIM/WorkshopDynamicBvh.cs:145-189`); every candidate pair scans every prismatic and revolute joint (`SIM/WorkshopPhysicsEngine.cs:877-886`). Only contact pairs use the tree (`:866`). Full scans: guides (`:1032-1068`), compliant surfaces (`:1412`, `:1540`), airflow line of sight (`SIM/WorkshopPhysicsEngine.Airflow.cs:149`), chambers (`SIM/WorkshopPhysicsEngine.Chambers.cs:86-160`), gas mass (`SIM/WorkshopPhysicsEngine.Gas.cs:69`). `AirflowLineClear` (`.Airflow.cs:141`) answers clear or blocked with no crossing.

*Contract:*
- A static tree built once at Run; a dynamic tree updated only when a collider leaves a box fattened by its actual velocity over the substep plus the speculative margin; incremental rotations keep it balanced.
- Bounds are correct for a plane of any normal.
- One query interface used by every phase: box overlap, shape against region, point containment, and a segment cast that returns, in order along the segment, every crossing of every participating collider (entry point, exit point, normal, collider, feature), exact for every admitted shape kind (sphere, box, plane, cylinder, tube, frustum, torus) on static and moving bodies. Results come in a canonical order (body pair, then feature).
- **Batched queries (ABI audit G16).** Every query kind takes N inputs in columns and writes its results into caller-provided buffers (per-input result ranges and counts), with no delegate, closure or allocation per hit or per query; a single query is a batch of one through the same path. Optics, acoustics, radiation and liquid streams cast many segments per tick and use the batch form from their first slice.
- A compiled body-to-collider range and a compiled jointed-pair set replace the per-pair joint scan.
- Users moved onto it in this story: contact pairs, guides, compliant-surface sampling (each surface queries its bounded stroke volume), airflow line of sight and receiver candidates (a source queries its bounded cone), chamber occupancy and exit-clearance sweeps, gas mass gathering. Later stories (optics, acoustics, radiation, liquid streams, thermal links, field regions, strand checks, granular apertures) use it from their first slice.

*Acceptance:*
- For seeded scenes of mixed shapes, each query returns exactly the set (and the crossing order) an all-pairs reference computed in the test returns; this is an exact comparison of collider identities, not of floats.
- A body resting on a tilted plane stays supported within the game-grade envelope.
- On every existing lesson and the delivered law scenes, goals, event kinds, owners and causal order are unchanged, and positions and speeds agree within the game-grade envelope.
- A capability's per-substep work does not change when bodies are added outside its declared volume (R4: collider visits counted).
- A batch of segment casts returns, for each input, exactly the colliders and crossing order the same cast returns alone, and allocates no managed memory after warm-up (a deterministic counter).
- *Performance:* R1 and R2 before and after; candidate pairs on R2 fall and never rise.

*Deletes:* the per-substep rebuild and the trial-replay rebuilds; the maximum-speed fattening; every scan listed above; the joint scan in `AddCandidatePair`; `AirflowLineClear` and its own test.

*Depends on:* DOC-ENGINE-STATUS. *Unblocks:* CAT-016, CAT-028, CAT-065, CAT-066, CAT-070, EL-042, EL-048, EL-090, EL-100, EL-107, EL-109, EL-175, EL-209; the segment cast every identity listed under `ENGINE-OPTICAL` and `ENGINE-RADIATION` needs; and the candidate source of `ENGINE-OPTICAL`, `ENGINE-RADIATION`, Story 14.0, `ENGINE-FIELD-FORCE`, `ENGINE-LIQUID`, `ENGINE-THERMAL`, `ENGINE-GRANULAR`, `ENGINE-IGNITION-BLAST` and `ENGINE-CONNECTION-MATERIALS`.

### Story ENGINE-CONTACT-OBSERVATIONS: Contact rows on any body pair, found by index
*Status: Backlog. Sprint key: `engine-contact-observations`. Order 3. Performance story (R4).*

*Outcome:* contact triggers, contact work, contact loads and integrity observers work on any pair of bodies, static or dynamic, and are found by compiled index.

*Present today:* triggers, contact work and contact loads fire only for a static body against a dynamic body, and the owner must be the static side (`SIM/WorkshopPhysicsEngine.cs:1341-1342`, `:226-229`, `:145`, `:169`); every contact scans every trigger and work row (`:141`, `:165`); every load row scans every contact (`:223-237`); integrity observers scan every contact (`SIM/WorkshopPhysicsEngine.Gas.cs:121`) and an integrity observer must be bound to a gas store (`ABS/GasCouplingValues.cs:15`); per-pair cooldown state is a managed map (`SIM/WorkshopPhysicsEngine.cs:1323`); a signal contact latches once per Run (`ABS/SignalContractValues.cs:51`).

*Contract:*
- Each row names its owning collider or body by compiled handle; a contact reports to the rows of both sides through a compiled index; no row or contact scan.
- Begin-touch and end-touch occurrences per declared pair are published for consumers that need them (thermal links, surface treatment).
- **Integrity observer, one owner (coordinator decision 16).** It names a body, one collider or a set of colliders; several may sit on one static or dynamic body; threshold impulse and minimum approach speed are constants or parameter-bound values; the pair measure may be multiplied by the other collider's declared stress concentration (at least 1, default 1); it publishes a one-way committed occurrence that a declared effect consumes (gas outlet, body break, blast release); it needs no gas store. A second separate strike on the same owner publishes a second occurrence; a persisting contact publishes none.
- A contact-qualified repeating hit (approach speed threshold, declared target set, minimum interval) is the same row kind as contact work, without the energy payment.

*Acceptance:*
- A trigger owned by a dynamic body fires once when struck at its threshold speed and above, and not below; the same declaration with the owner static gives the same occurrence.
- A scene with many unrelated rows gives the same events, owners and order as the same scene without them (R4: rows visited per contact counted).
- The same body meeting two colliders that differ only in stress concentration breaks only on the higher one, just below and just above the scaled threshold.
- An integrity observer with no gas store publishes its occurrence and changes nothing else.
- *Performance:* on R4 extended with many unrelated trigger, work, load and integrity rows, rows visited per contact and contacts visited per row, measured on the scene before this story lands and after; the after count is bounded by the rows indexed to the contacting colliders and never rises above the before value.

*Deletes:* the static-side requirement; the per-contact row scans and per-row contact scans; the integrity observer's mandatory gas-store binding.

*Depends on:* ENGINE-SPATIAL-QUERIES. *Unblocks:* CAT-009, CAT-015, CAT-052, CAT-063, CAT-069, EL-055, EL-057, EL-061, EL-084, EL-088, EL-090, EL-101, EL-106, EL-198, EL-210, GAP-09, TH-14, TH-33; the link table of `ENGINE-THERMAL` and the dwell of `ENGINE-SURFACE-STATE`.

### Story ENGINE-SENSOR-VOLUMES: Shape-based, frame-relative sensors inside the tick
*Status: Backlog. Sprint key: `engine-sensor-volumes`. Order 4. Performance story (R4). Amends Story 3.1.*

*Outcome:* residence, passage and guide evaluation happen inside the physics tick, against collider shapes, relative to the sensor's frame, with several sensors and guides per element.

*Present today:* residence tests the body origin point against a box once per committed tick (`SIM/WorkshopPhysicsEngine.cs:929-938`, `:348`, `:358`), reads world speed so a moving frame mis-measures (`:933`; guides likewise, `:1071`), and each sensor scans every dynamic body (`:913`) with an immutable-dictionary write per pair (`:973-976`). Passage sensing runs on the host after the tick, sensors × participants, in a helper named `RenderPosition` (`GPU/PassageSensorDeclaration.cs:80`, `:89-112`; called from `SIM/WorkshopGpuDevice.cs:243`). One residence sensor and one planar guide per element (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:27`, `:32`).

*Contract:*
- A sensor is a declared region on a frame body with a local id; a package may declare several sensors and guides.
- Occupancy is a declared closed rule per sensor, ShapeOverlap or CentreInVolume, found through the region query; the upper margin may be fixed or parameter-bound.
- Speed and approach are measured relative to the frame body's velocity at the sample point.
- Residence is evaluated on the committed motion inside the tick at each substep endpoint; passage crossing is evaluated inside the tick on the swept segment; a passage sensor may declare a minimum crossing speed, parameter-bound, measured relative to its frame.
- The residence phase (outside, dwelling, qualified) and the occurrence at which it last became true are on the committed read, for goals.

*Acceptance:*
- A body riding a moving frame faster than the speed limit dwells and is captured; a world-stationary body swept by the frame is not; static-frame results are unchanged.
- A body whose origin is outside but whose shape overlaps is reported as its declared rule states; the two rules give different answers on the same scene.
- Two sensors on one element report independently.
- The same body crossing a passage sensor just below and just above its declared minimum speed: only the second publishes an occurrence.
- *Performance:* on R4 with many sensors and bodies, sensor evaluations per tick (and bodies tested per sensor), measured on the scene before this story lands and after; the after count is bounded by the bodies the region query returns for each sensor and never rises above the before value.

*Deletes:* the origin-point test; absolute-speed tests in residence and guides; the host-side passage tracker and `RenderPosition`; the one-sensor and one-guide properties (audit E6, sensor part).

*Depends on:* ENGINE-SPATIAL-QUERIES. *Unblocks:* CAT-002, CAT-004, CAT-062, EL-062, EL-069, EL-070, EL-099, EL-109, EL-120, EL-123, EL-192, EL-194.

### Story ENGINE-ISLANDS-SLEEP: Islands, sleeping, and work confined to an island
*Status: Backlog. Sprint key: `engine-islands-sleep`. Order 5. Performance story (R1, R5). Absorbs `engine-resting-stack-settling`.*

*Outcome:* bodies joined by contacts, joints, transmissions and tensile routes form islands; an island at rest sleeps and costs nothing until woken; trial work touches only the island it affects.

*Present today:* no island, union-find, sleep state, timer or wake path exists in `SIM/`, `engine/`, `ABS/`, Core or Bundle (audit section 1); every body is integrated and every pair retested every substep, so a level's cost follows parts placed, not parts moving. A drive's power budget restores and re-solves the whole world's substep up to about ten more times (`SIM/WorkshopPhysicsEngine.Motors.cs:36-66`); continuous work runs whole-world trial substeps inside nested restart, scale and bisection loops (`SIM/WorkshopPhysicsEngine.ContinuousReplay.cs:33-52`, `.ContinuousSolve.cs:24-91`). A restitution group is silently cut to 8 points (`SIM/WorkshopConstraintSolver.cs:685-689`) and each root scans all manifolds (`:677-683`). Some resting three-box stacks keep a sub-millimetre vertical oscillation (deferred `engine-resting-stack-settling`, `_bmad-output/implementation-artifacts/deferred-work.md:122`).

*Contract:*
- Islands are built each substep by disjoint-set union over contacts, joints, transmissions and tensile routes.
- An island sleeps after the thresholds of `DOC:273` (0.01 m/s and 0.02 rad/s for 0.5 s); it wakes on contact with an awake body, a drive or supply change, a force region or airflow reaching it, a request, a cut, a break, a change of liquid or granular load on it, or an edit.
- A sleeping body's committed pose and velocity do not change; sleep state is on the committed read; Reset and Save/Load restore it exactly.
- Persistent link tables (thermal links, sensor occupancy) survive while an island sleeps.
- Drive-budget and continuous-work trials are confined to the island they affect. The restitution group has no point cap and finds its manifolds by island.

*Acceptance:*
- A settled stack reports asleep within the threshold time plus a stall guard and does not move while asleep.
- A ball rolled into it wakes it; the capture, toppling and event order are asserted as the scene's stated outcomes (whether the ball is captured, whether and which way the stack topples, and the causal order of the events), not compared with a recording (coordinator decision 41: no sleep-off setting ships or is declared in a test), within position tolerances justified by the receiving clearance.
- A body on a moving surface, in airflow or on a powered hinge does not sleep.
- A drive at its power limit in one island leaves a distant island's outcome unchanged, and R5 replays touch only that island's bodies.
- Three-box stacks come to rest (no per-tick motion while asleep).
- A group of more than eight bouncing points keeps every point.
- *Performance:* R1 tick time and bodies integrated per substep before and after; R5 bodies re-solved per replay.

*Deletes:* whole-world trial capture and restore; the 8-point restitution cut; the deferred story `engine-resting-stack-settling` (its stacks are this story's control).

*Depends on:* ENGINE-SPATIAL-QUERIES. *Unblocks:* CAT-001, CAT-007, CAT-014, CAT-023, CAT-039, CAT-051, CAT-062, EL-089, GAP-09, and the "parts that rest can sleep" line of every element spec.

### Story ENGINE-ONE-WAY-INSTALL: Physics commits alone
*Status: Backlog. Sprint key: `engine-one-way-install`. Order 6. Scope confirmed by owner decision 3a ("remove physics' wait for animation/renderer acknowledgement"); the audit's open decision is closed. Its widening beyond install receipts (the 500 ms read stall, latest-only reads on the animation port and removal of the Presented wait; ABI audit G3 and G4) applies owner decision 3a's "physics commits alone" (coordinator decision 40, 23 November 2026).*

*Outcome:* a slow or failed animation worker or renderer cannot delay, abort or retire a physics transition.

*Present today:* an install (Run, Reset, Step, completion) sends Prepare to the animation worker and the renderer and waits up to 250 ms for their receipts; commit proceeds only when both arrive, and a failure after the physical commit retires the session (`SIM/Program.Schedule.cs:101-103`, `:152-157`, `:179-180`, `:219-222`, `:232-236`; `CuriousContraptions.Animation.Worker/Program.cs:215-225`). For Step, Completed and Reset it also waits for a Presented receipt (`SIM/Program.Schedule.cs:231-235`). Beyond installs (ABI audit section 3, at `25df3cd5`): a committed read the main thread has not receipted for 500 ms is a stall (`GPU/WorkshopReliableReadLedger.cs:18`, `:79-80`), and `DebtExceeded` then triggers `StopForDebt`, which faults the run (`SIM/Program.cs:275-297`), so a main-thread hitch over 500 ms (a long garbage collection, an asset load, a frozen page) ends a puzzle run; and every committed read is `slice()`-copied and posted to the animation port with no coalescing or back-pressure (`SIM/wwwroot/worker.ts:291-294`), and the animation worker verifies and decodes all of it (`ANIM/Program.cs:213-215`), so a worker that falls behind grows the port's queue without bound.

*Contract:* the simulation publishes each install with its epoch and commits without waiting; consumers adopt the newest epoch; presentation readiness is a presentation concern. On adopting an epoch, and on construction and Reset, a consumer draws the first committed sample of every binding driven by committed state, never a zero-input value (owner decision 5; coordinator decision 29). The same one-way rule holds between installs:
- A presentation consumer that falls behind never stops physics: an unreceipted read is a presentation lag reported on the read, and the reliable-read stall no longer faults the run.
- The animation port carries latest-only reads: a newer committed read replaces one the worker has not taken, so its queue holds at most one read (until `ENGINE-WIRE-ABI` moves the animation worker onto the shared feed).
- The Presented wait is removed with the other receipts.

*Acceptance:* with the animation consumer withheld in a node harness, Run, Step, Reset and completion commit and the physical read advances; when the consumer returns it adopts the current epoch and its first sample equals the committed state; a consumer that fails leaves the session running; a binding driven by committed state shows its first committed sample, not a zero-input value, on construction and after Reset (a declared body whose binding is driven by committed state). Receipts withheld for 600 ms leave the run advancing and not faulted (the lifecycle failure: a renderer hitch ending a puzzle run). An animation worker whose service is throttled in a node harness for 10 s of Run holds at most one pending read and a heap that does not grow with Run time, and on release it draws the newest committed sample.

*Deletes:* the receipt wait, its 250 ms timeout and the post-commit retirement path in `SIM/Program.Schedule.cs`; the Presented wait; `StopForDebt` on a presentation stall and the read-debt fault; the per-read posting of every committed read to the animation port; any zero-input initial projection of a committed-state binding.

*Depends on:* DOC-ENGINE-STATUS. *Unblocks:* CAT-003 (first committed sample); ordinary lifecycle for all.

### Story ENGINE-SUPPLY-DEMAND: Drive work debits its source
*Status: Backlog. Sprint key: `engine-supply-drive-demand`. Order 7. The names `powered-part-demand-drain` and `engine-powered-part-demand-drain` are retired (owner decision 3a).*

*Outcome:* every supplied consumer's committed work is debited from the sources on its route within their power bounds; an exhausted source opens the route.

*Present today:* a closed `PowerIn` route enables a prismatic or revolute drive, which is on or off and debits nothing (`_bmad-output/implementation-artifacts/deferred-work.md:211`); contact-work stores and continuous airflow work are debited through `ElectricalSupply.Reserve`; electrical node ratings (`RatedVoltage`, `Resistance`, `Capacitance`, `MaxPower`) are checked finite and read by nothing (`CuriousContraptions.Bundle/BundleValidator.cs:877-889`); a consumer with no mechanism target requests nothing (`SIM/WorkshopPhysicsEngine.Supply.cs:88-105`). The finite-energy ledger is `ABS/FiniteEnergyBalance.cs`.

*Contract:*
- Each supplied drive declares demand = its maximum torque or force × the absolute target speed; the grant scales the drive's limit; its committed positive work, never above the grant, is debited as a request lane of the existing source reservation, not a second allocator.
- **A consumer with no work target requests nothing (coordinator decision 37).** A consumer with no work target (no drive, transmission, energy store or continuous-work target) is granted and debited nothing and only reads whether its route is powered, as the System clock's consumer does today (`SIM/WorkshopPhysicsEngine.Supply.cs:88-105`), unless it declares a standing demand: that demand is requested, granted, debited and committed as a grant.
- A work target (a consumer that does work in another domain, such as lifting liquid or compressing gas) is debited the energy it took, never more than its grant.
- **Shaft-to-supply conversion:** a supply source whose available power is at most a declared efficiency times the work absorbed on a declared coordinate delivers no more than that work.
- Several consumers on one source share it by the existing proportional rule; an unlimited source is a declared kind; an exhausted source opens the route on the next solve.
- **Grant per tick at every cadence (coordinator decisions 27 and 30).** A source's grant in a tick is energy = its power limit × dt, dt the committed tick's duration ([combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order)): 1 J per tick for a 120 W source at 120 Hz, 2 J at 60 Hz, 0.5 J at 240 Hz. This replaces the fixed 1/120 s supply phase (`engine/gpu/ElectricalSupply.cs:9`, `PhaseSeconds`), which is 1/120 s at every cadence today.
- **A drive limited to its allowance (coordinator decision 27).** A supplied drive whose request in a tick exceeds its source's per-tick allowance is limited to that allowance in that tick: its torque or force is scaled so the work it does that tick equals the energy granted. Element specs whose rows assume more are re-derived by their owners.
- **A consumer that can accept nothing requests nothing (coordinator decision 30).** A consumer target that reports it can accept nothing in a tick requests nothing in that tick and is debited nothing. `ENGINE-THERMAL`'s electrical heat target reports this while the store it heats is at its declared limit, so no grant is debited for heat the store would refuse.
- The electrical node's rated values are either the standing demand or deleted; no unread value remains.

*Acceptance:*
- A drive whose request exceeds its allowance does, in each tick, work equal to the energy granted (power limit × dt) and never more, at 60, 120 and 240 Hz cadences, and its source falls by that amount; the same drive under its allowance is unaffected.
- A consumer that reports it can accept nothing is granted and debited nothing in that tick, and resumes its request in the tick it can accept again.
- Stored energy falls by the committed work, never below it and above it by no more than the ledger's stated residual (`ABS/FiniteEnergyBalance.cs`); a stalled drive debits nothing.
- The drive stops when the source is empty and the route reads open; two consumers on one source stop together; an unlimited source never drains; Reset restores the authored charge.
- A standing-demand consumer drains a finite source at its declared rate and stops when it is empty; the same consumer with no work target and no standing demand leaves the source's charge unchanged while its route still reads powered.
- A shaft-paid source delivers no more energy than the shaft work absorbed.
- Two differently rated fixtures drain a finite source at their declared rates.

*Deletes:* the binary-supply limitation and its deferred-work entry; the unread electrical node ratings (or their unread status); the fixed 1/120 s supply phase as the grant duration.

*Depends on:* DOC-ENGINE-STATUS. *Unblocks:* CAT-005, CAT-007, CAT-018, CAT-039, CAT-042, CAT-051, CAT-059, EL-022, EL-037, EL-060, EL-068, EL-078, EL-079, EL-082, EL-083, EL-086, EL-087, EL-088, EL-095, EL-097, EL-167, EL-196, EL-199, EL-209, EL-211, EL-217, EL-220, EL-221, EL-222, EL-233, EL-234, EL-237, EL-241, GAP-04, RAD-06, RAD-14, TH-05, TH-34. EL-199 closes inside this slice.

### Story ENGINE-CAPABILITY-INSTANCES: Shared contracts stop encoding one product's shape
*Status: Backlog. Sprint key: `engine-capability-instances`. Order 8. Refactor; may be split by contract family.*

*Outcome:* shared contracts name capabilities and hold lists of capability instances with local ids, so no package needs an engine edit to declare two, three or four of something.

*Present today (audit section 2):* product-named activation kinds `Counter`, `StateLatch`, `LevelIndicator` (`ABS/ActivationValues.cs:18`, `:24`; `ABS/CompiledRuntimeContracts.cs:244-245`), E1; a boolean supply node fixed at two inputs (`ABS/SupplyContractValues.cs:26`; `GPU/SupplyRoute.cs:375-376`, `:391-395`), E2; a socket-glyph enum of product terminals (`ABS/PortDescriptor.cs:38`; `ui/WorkshopConnections.cs:36-44`), E3; player-facing strings naming parts (`ui/Workshop.cs:371`, `:884`; `GPU/WorkshopConnections.cs:150`; `GPU/SupplyRoute.cs:70`), E4; tube mouth snapping with exactly two ends and constants of one family (`engine/TubeMouthSnap.cs:23-25`, `:98-99`; `ABS/SceneGeometry.cs:37`, `:39`), E5; one compliant surface, residence sensor and guide per element, a fixed four-sample recipe and dense engagement rows (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:27`, `:32`, `:77`; `SIM/WorkshopPhysicsEngine.cs:1555-1556`; `GPU/PhysicsWorldLayout.cs:41-42`, `:90-94`), E6; presentation axes hard-wired (`engine/MachinePart.cs:162`, `:172`, `:231`), E7; a driver named after one wheel (`ABS/ElementPresentation.cs:25`), E8; a chamber region always a cylinder (`SIM/WorkshopPhysicsEngine.Chambers.cs:177-183`, `:141-166`), E9; run control assuming one driven hinge and a single inventory entry (`ABS/ParameterDescriptor.cs:48`; `GPU/PuzzleCatalogue.cs:36`, `:90-91`), E11; a product role in the target filter (`ABS/ContactWorkDeclarationValues.cs:31`), E12; hand-mirrored TypeScript enums that have drifted (`CuriousContraptions.web/wwwroot/workshop-observation.ts:12`, `:41`), E13; a hand-maintained package list (`CuriousContraptions.slnx:16-59`), E14. Rope ports admitted by enumerating three role names (`GPU/WorkshopConnections.cs:163-164`; `GPU/ElementPortCatalogue.cs:182`; `CuriousContraptions.Bundle/BundleValidator.cs:543`, `:546`). A second collider-shape enum and unadmitted `Capsule` and `Mesh` members (`ABS/ColliderDescriptor.cs:8-16`; `ABS/PhysicsDeclarationValues.cs:12`). Visible art accepted as a collider proxy (`tools/BundleElements/SceneGeometryExtractor.cs:103`), L2. The CPU engine reached through GPU-named types (`WorkshopGpuDevice`, `BrowserGpuTransport`, `PhysicsGpuAbi`, `GpuBodyId`, `engine/gpu/`).

*Contract:*
- Activation kinds take capability names (`PulseCount`, `Memory`, `Level`) through compiler, wire, read and TypeScript.
- One N-input boolean node whose input list and truth function (the closed `LogicGateOperation` set) are declared.
- Socket glyphs ship as package assets; snap sockets are a declared list (one to four per part, each with bore, capture distance and angle, joinable or a non-joinable outlet, plus immersed-port locators), each with a package-local id.
- Compliant surfaces, residence sensors and guides are list-valued with local ids; sample layout is declaration data; engagement rows are sparse, held only for bodies overlapping a surface volume.
- Presentation derives axes from the declared joint frame; drivers name the capability instance they read; the wheel-named driver becomes the guide angle it is.
- **Coordinate drivers whatever the joint kind (coordinator decision 29).** A presentation driver that names a coordinate reads its committed coordinate continuously whatever the joint kind, so a prismatic coordinate has the continuous travel driver a revolute one has. The closed value-mapping set gains one member: a wrap of a committed coordinate into a declared pitch.
- Region shape and exit axis are declared; run controls and inventory name drives by local id and inventory is a list; participation is a declared mask.
- Rope-port admission reads domain, direction and declared attachment role, never an enumeration of role names.
- TypeScript mirrors are generated from the C# owners in the build; the package list is derived from discovery. **One source per enum (owner decisions 24j and 24k; amended by owner decision 24p, 25 November 2026):** the C# enum is the single source of every closed set that does not cross the wire, and this story's C#-to-TypeScript generator is the only producer of their TypeScript mirrors. It replaces the non-wire hand mirrors among the about 45 in `WEB/workshop-observation.ts:3-49`. Closed sets that cross the wire are declared once, in the schema, and reach C# and TypeScript through `flatc` (`ENGINE-WIRE-ABI`); this generator does not emit them. `ENGINE-WIRE-ABI` adds no second generator.
- The bundle tool refuses a visible mesh used as a collider proxy with a typed reason; one collider-shape enum remains with only admitted members.
- Player-facing text uses capability wording or declared display names. In particular the level-rule refusal at `GPU/WorkshopConnections.cs:150` ("A level output can only light a Signal lamp.") becomes "A level output connects only to an input that admits a level."; this story is the one change that rewords it (the first slice to edit the line), and it updates the tests that pin the old text (`DeclaredIndicatorPassageTests.cs:296`, `LampDetectorGuardTests.cs:304`, `StateLatchTests.cs:164`, `PressurePlateTests.cs:698`) and the constant of the deferred browser suite `tools/e2e/cat-052-lamp.test.ts:36` (edited, not run).
- Engine-facing types drop the GPU names for the CPU engine they reach.

*Acceptance:* a qualification fixture declaring two compliant surfaces, a three-input boolean node, a three-socket snap part with one outlet, two residence sensors, three rope ports and a hinge on a rotated axis compiles and behaves by its declaration; a binding on a prismatic coordinate follows its committed travel continuously, and a wrap mapping repeats at its declared pitch on two fixtures with different pitches; every existing lesson keeps its goals and causal event order; a visible proxy is refused with its typed reason; a generated TypeScript enum matches its C# owner member for member (checked by the build, not by a hash).

*Deletes:* each old name, fixed slot, enum member and hand-mirrored enum in the same slice: `TubeEnd`, the First/Second terminals and the two-input state enum, the glyph table, `CommittedWheelAngle`, `AllPayloads`, the fixed axes, the duplicate shape enum, the hand-maintained package list; and the part names in shared engine comments, reworded element-free (Bumper: `GPU/ContactWorkImpulse.cs:11`, `:26` and `ABS/ContactWorkDeclarationValues.cs:56`; Domino: `ABS/OrientationSensorDeclarationValues.cs:11`).

*Depends on:* ENGINE-SENSOR-VOLUMES (list-valued sensors). *Unblocks:* CAT-001, CAT-002, CAT-003, CAT-004, CAT-010, CAT-013, CAT-014, CAT-019, CAT-020, CAT-024, CAT-025, CAT-026, CAT-027, CAT-030, CAT-035, CAT-037, CAT-039, CAT-048, CAT-049, CAT-050, CAT-053, CAT-058, CAT-062, CAT-064, CAT-065, CAT-067, EL-001, EL-008, EL-011, EL-012, EL-017, EL-020, EL-023, EL-024, EL-026, EL-027, EL-029, EL-030, EL-031, EL-032, EL-033, EL-035, EL-036, EL-133, EL-134, EL-135, EL-136, EL-137, EL-166, EL-167, EL-168, EL-172, EL-183, TH-11, TH-22, TH-23, and every package with more than one instance of a capability. The payload contract (a declared participation mask in place of `AllPayloads`) changes the declarations of `ball`, `bowling`, `tennis`, `balloon` and `basket` (CAT-001, CAT-014, CAT-064, CAT-003, CAT-004).

### Story ENGINE-WIRE-ABI: Every boundary record generated once; the pose ring feeds the renderer
*Status: Backlog. Sprint key: `engine-wire-abi`. Order 8a. Performance story (R6). Added to the programme by owner decision 24k (23 November 2026), from the [ABI audit](../../docs/planning/elements/readiness/audits/abi-interfaces.md) section 6.1. Element-free; may be split by layout family. Instanced drawing is not part of this story: it is `ENGINE-INSTANCED-RENDERING` (order 8c, coordinator decision 40), which draws from the feed this story provides.*

*Outcome:* every record that crosses a worker, thread or persistence boundary is declared once in a schema and read and written through code generated from the pinned `flatc` on both sides (owner decision 24j: wire code is generated, never hand-coded), and the renderer reads committed poses from the shared ring without locks, serialisation or allocation per frame.

*Present today (ABI audit, at `25df3cd5`):*
- After S5 and S5b the FlatBuffers accessors are generated, but about 3,470 lines of hand mapping codecs remain (`GPU/WorkshopWireFlatBuffers*.cs`, `GPU/WorkshopAuthoringFlatBuffers.cs`, `GPU/WorkshopSaveCodec.cs`; audit W1), with vtable arithmetic, field counts and slot indices typed in by hand (`GPU/WorkshopWireFlatBuffers.cs:45-53`, `:59-82`; `GPU/WorkshopAuthoringFlatBuffers.cs:44-100`; W2). Unknown-field refusal is hand-coded for 6 of about 76 tables and absent for the rest; no test ties a literal to the schema.
- `motion: [uint8]` tunnels a hand-laid record (`PhysicsMotionRead`, 80-byte motion pieces) through the schema (`FBS:305-306`; encode `GPU/WorkshopWireFlatBuffers.cs:495`, decode `:694-697`; `GPU/PhysicsMotionRead.cs:47-52`; W3), copied at each end.
- About 51 kind, phase, mode, state and reason fields are raw integers (W4) with per-site refusal, and one sampled decode casts without a local check (`GPU/WorkshopWireFlatBuffers.cs:864`). Seven of the schema's eight enum types duplicate a C# owner: `SimulationPhase` (`FBS:3`; `WorkshopSimulationPhase`, `ABS/RuntimeServices.cs:10`), `CommandKind` (`FBS:17`; `WorkshopCommandKind`, `ABS/WorkshopMessageValues.cs:11`), `CommandOutcome` (`FBS:37`; `WorkshopCommandOutcome`, `ABS/RuntimeServices.cs:11`), `ResponseKind` (`FBS:45`; `WorkshopResponseKind`, `ABS/WorkshopMessageValues.cs:13`), `ElectricalEnable` (`FBS:50`; `ABS/ElectricalSourceValues.cs:36`), `RunControlLane` (`FBS:56`; `ABS/ParameterDescriptor.cs:50`) and `RunControlValueKind` (`FBS:62`; `ABS/RunControlValues.cs:11`); the eighth, `ObservationPayload` (`FBS:385`), has no C# enum owner.
- Every non-FlatBuffers binary layout is hand-offset in C# and mirrored by hand in TypeScript (W7): the pose ring (`GPU/WorkshopPoseRing.cs:11-17`; `WEB/pose-ring.ts:12-15`), the event ring (`ABS/EventRingBuffer.cs`; `WEB/event-ring-cursor.ts:14-20`), the clock ring (`GPU/WorkshopClockWire.cs`; `WEB/clock-ring.ts`), schedule controls with literal offsets (`SIM/Program.Schedule.cs:134-140`, `:168-171`, `:248`; `ANIM/Program.cs:232-234`), animation control and output (`GPU/WorkshopHint.cs:7-47`), the clock peer bootstrap (`WEB/workshop-client.ts:232-235`), and the trace record, canonical body, physics body, mechanical link, cadence and receipt layouts (`GPU/WorkshopTraceRecord.cs`, `GPU/CanonicalBodyCodec.cs`, `GPU/PhysicsBodyWire.cs`, `GPU/MechanicalLinkWire.cs`, `GPU/WorkshopCadence.cs`, `GPU/WorkshopClockWire.cs`). `CuriousContraptions.tests/WorkerAbiTests.cs:67-83` pins `const NAME = n;` in four scripts only; the Animation worker's literal offsets (`ANIM/wwwroot/worker.ts:78`, `:80`) and the inline literals at `WEB/pose-ring.ts:131-145` and `SIM/wwwroot/worker.ts:48` are pinned by nothing. The hand TypeScript schema copies in deferred tooling (`tools/e2e/workshop-wire.ts:12732-13101`; `tools/e2e/run-reset-snapshot.ts:14`, `SNAPSHOT_SCHEMA`; W5) remain after S5b.
- **The pose ring is write-only.** Every tick the simulation composes a new slot array (`SIM/Program.cs:347`; `GPU/WorkshopPoseRing.cs:58-64`), JSImport marshals it to JavaScript as a copy (`SIM/wwwroot/worker.ts:286-290`) and `PoseProducer.publish` copies it into the next of three slots (`WEB/pose-ring.ts:87-99`); no production code reads it (`PoseReader` is constructed and swapped at `WEB/workshop-client.ts:152`, `:256-257` and never read). Its header holds only a sequence and a capture time and its record has no angular velocity or motion pieces (`GPU/WorkshopPoseRing.cs:55-57`), so it could not feed interpolation as laid out. Both sides use the plain-access seqlock the event ring rejected as tear-prone (review finding F1, `WEB/event-ring-cursor.ts:9-11`; `WEB/pose-ring.ts:96`, `:123-150`; `GPU/WorkshopPoseRing.cs:109-138`), and `latest()` and `read()` allocate per call (`WEB/pose-ring.ts:128-147`, `:157-160`).
- **The renderer is fed by the whole read.** The full `WorkshopResponse` is encoded every tick (`SIM/Program.cs:335-348`; `GPU/WorkshopWireFlatBuffers.cs:204`, `:467`, `:568`), posted by transfer (`SIM/wwwroot/worker.ts:219-225`) and decoded into managed objects on the main thread and again on the animation worker (`ANIM/Program.cs:213-215`); `WorkshopPoseHistory` keeps seven decoded responses and allocates `new PresentedBody[]` every frame (`GPU/WorkshopPoseHistory.cs:12-15`, `:276`, `:301`), brackets only adjacent ticks and otherwise reports `AwaitingHistory` (`:241-242`, `:298`), so catch-up turns of up to four ticks (`GPU/WorkshopTickLoop.cs:15`, `:38`) can leave frames that do not advance (unmeasured). How the renderer then draws each body is `ENGINE-INSTANCED-RENDERING`'s.
- **Animation output** is one 104-byte sample per animated target per `postMessage`, with one sample in flight across all targets (`ANIM/wwwroot/worker.ts:51-57`, `:76-82`, `:134-138`; `WEB/workshop-client.ts:191-203`), so a full refresh of T targets needs T round trips.

*Contract:*
- **Fixed records are FlatBuffers structs** in the schema, with C# and TypeScript accessors generated from the pinned `flatc` (S5, S5b), so no new generator is introduced: the pose record and pose-slot header, event record, schedule control and result, animation control and output, clock peer and receipt, motion piece, trace record, canonical body, physics body, mechanical link and cadence. Shared-memory access is a thin hand layer that copies whole words atomically into a private buffer and decodes it with the generated accessor (the event ring's pattern); it holds no byte offset of its own.
- **Unknown-field refusal is derived from the generated code** for every table, replacing the W2 literals. S5c delivers this before the programme (owner decision 24p), as one rule in the vendored verifier; this story keeps it for every record it adds.
- **`motion: [uint8]` becomes schema structs**, read through generated accessors.
- **Enums: one declaration per closed set (owner decision 24p, amending C36 for wire enums; coordinator ruling of 25 November 2026).**
  - **Closed sets that cross the wire** have the schema enum as their one source: `CommandKind`, `ResponseKind`, `SimulationPhase`, `CommandOutcome`, `ElectricalEnable`, `RunControlLane`, `RunControlValueKind`, and `ObservationPayload`, which has no C# owner. C# and TypeScript both take them from `flatc`.
  - This story makes the C# domain use the generated enum, so each closed set has one declaration. Its planning chooses, under the element-free design, between Abstractions referencing the dependency-free `CuriousContraptions.FlatBuffers` assembly and deleting the domain duplicate (`ABS/WorkshopMessageValues.cs:11-13`, `ABS/RuntimeServices.cs:10-11`, `ABS/ElectricalSourceValues.cs:36`, `ABS/ParameterDescriptor.cs:50`, `ABS/RunControlValues.cs:11`).
  - S5c's interim mirror-equality test is deleted with the mirrors.
  - Every remaining raw-integer field that carries a closed set (W4) becomes a schema enum field or is decoded through the one refusal helper (`ObservationEnum<T>`, `GPU/WorkshopWireFlatBuffers.Observation.cs:56-59`), with a test that maps each such field to its declaration.
  - The TypeScript wire enums in production script (`WEB/workshop-observation.ts:21`, `:39-40`, `:47`) are imported from the generated module.
  - `ENGINE-CAPABILITY-INSTANCES`' C#-to-TypeScript generator produces only the non-wire enums.
  - This story adds no generator.
- **One declaration per capability record (ABI audit G14).** A capability's wire, read and shared-layout records are declared once in the schema, and the C# and TypeScript sides follow from generation, so a new capability edits the schema and its domain mapping only; it no longer edits hand codecs, vtable literals, layout constants or TypeScript mirrors.
- **The pose ring is the presentation feed.** Its header carries epoch, tick and body count; its records carry what interpolation needs (pose, linear and angular velocity, and the tick's motion pieces). The producer writes the slot in place from committed state, with no per-tick array. Every word is read and written atomically, so a torn read is impossible. The reader allocates nothing per frame, and a reader that misses slots keeps its last good frame. Presentation stops decoding bodies and motion from the per-tick `WorkshopResponse`; the Code Map decides whether the read still carries bodies for latch and goal validation (`GPU/WorkshopMessages.cs:22-41`, `engine/MachineWorld.Gpu.cs:285`). The animation worker reads the same feed.
- **Multi-buffered, depth derived (owner decision 24k).** Presentation renders `DelayNanoseconds` (16.67 ms, `GPU/WorkshopPoseHistory.cs:13`) behind the newest commit and needs both bracket ticks while the producer writes the next slot, so the ring's depth = the delay in ticks at the Run's cadence + 2 bracket slots + 1 write slot: 1 + 3 = 4 slots at 60 Hz, 2 + 3 = 5 at 120 Hz and 4 + 3 = 7 at 240 Hz, the 7 slots owner decision 24k names. The depth is computed at install from the cadence and the delay, never a constant.
- **Copies.** Until `ENGINE-WASM-THREADS` lands, the exchange costs one bounded copy per side (producer storage into the ring, ring into the consumer's buffer), the floor without threads (ABI audit section 3.4); `ENGINE-WASM-THREADS` removes the producer copy.
- **Animation output is one shared table per pulse**, written in place and published once, not one message per target.

*Acceptance:*
- For every layout, the generated C# and TypeScript accessors read the same committed fixture bytes to equal field values.
- Adding or moving a field in the schema changes both sides with no hand edit (a scratch-tree build test).
- No production source in `GPU/`, `SIM/`, `ANIM/` or `WEB/` holds a numeric byte offset for a boundary record: the literal scan is extended to every TypeScript script, the Animation worker's included, and finds none.
- In a node harness the reader reads N bodies from the ring for 1,000 frames while the producer publishes concurrently at 240 Hz: no managed or JavaScript allocation per frame after warm-up (a deterministic counter), every frame's records belong to one tick (no torn read), and a reader paused for three frames resumes with its last good frame and then the newest.
- Under a forced 3-tick catch-up the renderer brackets every frame and reports no `AwaitingHistory` frame (the failure excluded: frames that do not advance during catch-up).
- An undefined enum value and an unknown appended field are each refused with their typed reasons at both decode boundaries.
- With T animated targets, every target's newest sample reaches the main thread in one publication per pulse, for two values of T.
- A qualification capability record added to the schema in a scratch tree reaches C# and TypeScript with edits to the schema and its domain mapping only (files touched counted; ABI audit G14).
- Every non-FlatBuffers boundary record this story replaces with generated code (pose record and slot header, event record, schedule control and result, animation control and output, clock peer and receipt, motion piece, trace record, canonical body, physics body, mechanical link, cadence) is declared in the same single schema file as every other record, `schemas/workshop_wire.fbs`, with no second schema file or `include` (owner decision 24p). S5c's one-file test covers these records too. The first slice that adds a production TypeScript table decoder also provides TypeScript verification without hand code (S5c inventory row H-R1; the candidate to evaluate is the compiler's binary schema with `reflection.fbs`).
- *Performance (R6):* bytes copied per tick on the publication path, main-thread bytes allocated per frame and decode time per tick against body count, before and after; none rises.

*Deletes:* the hand layouts and their TypeScript constant mirrors; `WorkerAbiTests`' regex pinning; the `motion` blob and `PhysicsMotionRead`'s hand layout; the second declaration of each wire closed set (the C# domain duplicate or its separate declaration, per the Enums bullet) (the W2 vtable literals, the hand unknown-field checks and the hand TypeScript schema copies of W5 are deleted earlier by S5c, owner decision 24p); the presentation decode of bodies and motion from the per-tick read, with `WorkshopPoseHistory`'s copies of whole responses; per-target animation messages; `ComposeSlot`'s array; the fixed `SlotCount = 3`.

*Depends on:* S5 and S5b (pinned generation for C# and TypeScript), S5c-1 to S5c-3, ending with S5c-3 (the one schema file and the C#/TypeScript conformance test), ENGINE-ONE-WAY-INSTALL (epoch adoption), ENGINE-CAPABILITY-INSTANCES (the one enum generator). *Unblocks:* ENGINE-WASM-THREADS; ENGINE-INSTANCED-RENDERING (the renderer's pose feed); ENGINE-TICK-ALLOCATION's host read path (measured after this story shrinks it); ENGINE-SOA-SIMD (poses written from its columns into the ring); no identity directly. Every capability delivered before it adds hand layouts, codec fields and TypeScript mirrors that it would then convert, so it precedes further element delivery.

### Story ENGINE-WASM-THREADS: The simulation runs on WebAssembly threads over shared memory
*Status: Backlog. Sprint key: `engine-wasm-threads`. Order 8b. Performance story (R6, R1). Added to the programme by owner decision 24k (23 November 2026), placed with ENGINE-WIRE-ABI. Element-free.*

*Outcome:* the simulation runtime is built with WebAssembly threads, so the pose ring, the event ring and (from ENGINE-SOA-SIMD) the engine's tables live in shared memory that C# writes in place and the other workers read without a copy on the producer side, and islands can be solved on several threads.

*Present today (ABI audit section 3.4, at `25df3cd5`):* no project sets `WasmEnableThreads` (`SIM/CuriousContraptions.Simulation.csproj:2-31`); the worker is full LLVM AOT (`RunAOTCompilation`, `AOTMode` `LLVMOnly`, `:19-20`) with SIMD (`:22`). The physics worker, the animation worker and the main thread are separate .NET WASM runtimes; none can address another's heap, and C# cannot address a JavaScript `SharedArrayBuffer` or run `Atomics` on it, so every shared exchange goes through `byte[]` JSImport marshalling (`SIM/Program.cs:103-104`; `SIM/Program.Schedule.cs:15-17`, `:24`) and is copied again into the ring by JavaScript (`SIM/wwwroot/worker.ts:63-64`, `:94-100`, `:286-290`); no JSImport passes a `Span<byte>` as `JSType.MemoryView`. Cross-origin isolation, which `SharedArrayBuffer` and threads require, is obtained by the page's isolation service worker adding the headers (`WEB/workshop-isolation-worker.ts:34-36`; checked at `WEB/workshop-isolation.ts:129`, `:143`), because the static host sets none. Process-wide state would race under threads: `[ThreadStatic]` restitution scratch (`SIM/WorkshopConstraintSolver.cs:642-643`), static worker state (`SIM/Program.cs:13-35`; `SIM/Program.Schedule.cs:9-36`) and `lock (Gate)` in `GPU/DeclaredPhysics.cs:32-64` and `GPU/ElementPortCatalogue.cs:312-340`. Nothing in a tick reduces results across threads. Whether the pinned SDK supports threads with `LLVMOnly` AOT is not established (ABI audit section 7).

*Contract:*
- **First slice, the gate.** The pinned SDK builds the simulation worker with `WasmEnableThreads` and the same full LLVM AOT, SIMD, trimming and build gate; the published page is cross-origin isolated through the isolation service worker in every supported browser; a threaded runtime starts, runs a tick and shuts down cleanly. If the pinned SDK cannot combine threads with `LLVMOnly` AOT, the slice stops and returns to the owner with the measured alternatives (another AOT mode, or one bounded copy per side kept), with no partial switch and no second build.
- **Shared layouts in shared memory.** The pose ring and the event ring live in the simulation runtime's shared memory, laid out by `ENGINE-WIRE-ABI`'s generated structs; C# writes them in place and JavaScript and the other workers read views of that memory. The producer copy that `ENGINE-WIRE-ABI` leaves is removed; a consumer reads with at most one copy, into its own instance buffers.
- **A deterministic worker pool for island work.** Work is partitioned by island and each island's result is committed in canonical island order, so committed results do not depend on thread count or scheduling; nothing is reduced across threads in arrival order. The pool has a consumer whether or not the conditional `ENGINE-PARALLEL-ISLANDS` ever runs: the island-partitioned solve of the tick is dispatched through it from this story on (one thread is the same path), and this story's own acceptance runs every lesson and law scene through it at 1, 2 and 4 threads; `ENGINE-PARALLEL-ISLANDS`, if it runs, only tunes that dispatch.
- **Thread-safe engine state.** Process-wide mutable state moves into the engine instance or becomes per-thread by design; locks remain only on host paths outside the tick.
- **One thread-count rule.** The pool size is the device's reported hardware concurrency bounded by a declared maximum; one thread is a valid size of the same path, not a fallback mode.

*Acceptance:*
- The threaded, AOT-compiled worker passes the full native round and the build gate's f32x4 check (`SIM/CuriousContraptions.Simulation.csproj:24-26`).
- A node harness runs the same scenes on 1, 2 and 4 threads: goals, event kinds, owners and causal order are identical, and positions and speeds agree within the game-grade envelope.
- With the ring in shared memory, bytes copied per tick on the producer side are zero (R6).
- A page that is not cross-origin isolated is refused with a typed reason before Run; it never runs by a second, unthreaded path.
- *Performance:* R6 publication bytes and time, and R1 tick time, before and after; must not regress.

*Deletes:* the `byte[]` marshalling of shared layouts and the JavaScript copy into the rings; process-wide mutable engine state; the "one copy per side" floor that ENGINE-WIRE-ABI records.

*Depends on:* ENGINE-WIRE-ABI, ENGINE-ISLANDS-SLEEP (the islands the pool partitions and dispatches). *Unblocks:* ENGINE-SOA-SIMD (tables in shared memory), ENGINE-PARALLEL-ISLANDS; no identity directly.

### Story ENGINE-INSTANCED-RENDERING: Bodies are drawn in instanced batches from the pose ring
*Status: Backlog. Sprint key: `engine-instanced-rendering`. Order 8c. Performance story (R1, R2). Added to the programme by coordinator decision 40 (23 November 2026), split from ENGINE-WIRE-ABI: the [ABI audit](../../docs/planning/elements/readiness/audits/abi-interfaces.md) G1 found instanced batching owned by no story. Element-free.*

*Outcome:* the renderer draws every presented body in GPU instanced draw batches grouped by mesh and material, on both WebGL 2 and WebGPU, filled each frame from the pose ring that ENGINE-WIRE-ABI makes the presentation feed, as `AGENTS.md`'s rendering graph and `DOC`'s Rendering Graph (`DOC:70-71`) state; the batches are derived from compiled package declarations, so a new part adds declarations and art, never renderer code.

*Present today (ABI audit section 3.1, at `25df3cd5`):*
- `WorkshopPhysicalPresentation.Apply` writes one Godot node transform per presented body every frame (`engine/MachinePart.cs:342-350`) and counts the node positions it changes (`writes`, returned at `:352`); before writing, it finds each body's node by scanning every part for every pose (`:326-339`). Strand spans are one node each, placed, turned and scaled to the span's length (`engine/MachinePart.cs:222-233`), and declared presentation bindings set node properties one binding at a time (`:236-241`).
- No `MultiMesh`, `RenderingServer` call or instanced draw exists in `engine/`, `ui/` or `WEB/` (ABI audit section 3.1), so `DOC`'s instanced-batch statement (`DOC:70-71`) and NFR-02 are unimplemented.
- The poses it applies come from `WorkshopPoseHistory`'s decoded responses (`GPU/WorkshopPoseHistory.cs:276`, `:301`), which ENGINE-WIRE-ABI replaces with the ring reader.
- The project selects Godot's `gl_compatibility` renderer for desktop and mobile (`project.godot:17-18`), which is WebGL 2 on the web; nothing configures WebGPU. Whether the pinned Godot web export can drive WebGPU, and how it batches draw calls on the web today, are not established (ABI audit section 7; [engine design audit](../../docs/planning/elements/readiness/engine-design-gap.md) section 7, `:262`).

*Contract:*
- **First slice, the gate.** It establishes, with the pinned Godot version and web export, how an instanced batch is drawn on WebGL 2 and whether WebGPU can be driven at all. If a backend cannot be reached, the slice stops and returns to the owner with the measured alternatives, with no partial switch and no second renderer path.
- **One draw path.** Every presented body is one instance in a batch keyed by its declared (mesh, material) pair. Batch keys, meshes and materials are compiled at Run from the visual meshes the packages declare; no element identity, kind or name appears in renderer code. A batch of one is a valid size of the same path, not a fallback; no body is drawn by a per-node transform.
- **Fed by the ring.** Each frame the renderer reads the newest bracket from ENGINE-WIRE-ABI's allocation-free ring reader, interpolates it, and writes instance transforms straight into per-batch instance buffers. The buffers are sized at Run from the compiled declarations and grow geometrically (design contract D5); nothing is allocated per frame, and after ENGINE-WASM-THREADS the copy into the instance buffers is the consumer's only copy.
- **Strands and bindings stay in their batches.** A strand span is an instance of its declared segment mesh whose transform carries the span's midpoint, direction and length scale (today's rule at `engine/MachinePart.cs:222-233`). Art moved by a declared presentation binding is an instance whose transform composes its body's pose with the binding's committed sample from ENGINE-WIRE-ABI's shared animation table; a binding that changes a declared material value writes it as per-instance data, so no binding splits a part out of its batch.
- **One batch build for both backends.** The same batches feed WebGL 2 (`drawElementsInstanced`) and WebGPU (`drawIndexed` with an instance count); there is no per-backend batching rule.
- **Refusal.** A package whose visible mesh cannot be keyed (no declared mesh, or a material the batch key cannot name) is refused at compile with a typed reason; it is never drawn by another path.

*Acceptance (native, through the batch builder's defined interface fed by the ring reader):*
- On R1 at two collider counts and on R2, the batch count equals the number of distinct declared (mesh, material) pairs in the scene and does not change with body count, and per-body node transform writes are zero (the failure excluded: draw cost growing with body count).
- Two qualification fixtures with different meshes and materials compile to their own batches with no renderer edit; a fixture whose binding changes a material value stays in its batch.
- Fed by the ring at 240 Hz for 1,000 frames, every instance transform equals the interpolated committed pose within the game-grade envelope's position and orientation tolerances, no frame mixes two ticks, and nothing is allocated per frame after warm-up (a deterministic counter).
- A strand span's instance length matches the committed span length within the game-grade envelope's position tolerance on two spans of different rest length (the failure excluded: a drawn strand that does not reach its anchors).
- Reset and a new install rebuild the batches from the compiled declarations; a removed part leaves no instance; on adopting a new epoch the first drawn frame is the first committed sample (ENGINE-ONE-WAY-INSTALL).
- A package whose visible mesh cannot be keyed is refused with its typed reason before Run.
- Actual WebGL 2 and WebGPU frame pacing in Chrome is Story 16.2's, run in the post-catalogue browser phase; this story claims no browser qualification.
- *Performance (R1, R2):* batches per frame, per-body node transform writes per frame and main-thread bytes allocated per frame against body count, before and after (deterministic counters); main-thread frame time recorded beside them; none rises.

*Deletes:* the per-body node transform loop and its per-frame target scan in `WorkshopPhysicalPresentation.Apply` (`engine/MachinePart.cs:326-350`); the per-span node writes of `ApplyPhysicalSpans` (`:222-233`); every visual node kept only to carry a body pose.

*Depends on:* ENGINE-WIRE-ABI (the ring as the presentation feed and the shared animation table), and through it ENGINE-CAPABILITY-INSTANCES (declared presentation drivers) and ENGINE-ONE-WAY-INSTALL (epoch adoption). Listed after ENGINE-WASM-THREADS (order 8c) but not dependent on it. *Unblocks:* Story 16.2 (instanced batches on WebGL 2 and WebGPU, qualified in the browser phase); `DOC`'s Rendering Graph statement and NFR-02; no identity directly.

### Story ENGINE-TICK-ALLOCATION: An allocation-free tick
*Status: Backlog. Sprint key: `engine-tick-allocation`. Order 9. Performance story (R3, R6). Before further elements (owner decision 3a).*

*Outcome:* a steady-state tick at stable topology allocates no managed memory, in the solver and on the host path.

*Present today:* a new object per contact point (`SIM/WorkshopCollisionKernels.cs:305-322`); a new list per box–plane, box–box, cylinder–box and cylinder–cylinder pair (`:474`, `:726`, `:1546`, `:1894`); a new feature-set key with an array and a sort per manifold (`SIM/WorkshopFrictionSolver.cs:13-31`); sort lambdas per manifold (`:117`; `SIM/WorkshopHollowKernels.cs:148`); a delegate per broadphase query (`SIM/WorkshopPhysicsEngine.cs:866`); two immutable-dictionary builders per tick (`:249-250`, `:370`); a hash set per tick (`SIM/WorkshopPhysicsEngine.Motors.cs:17`); host-side dictionaries, LINQ and closures (`SIM/WorkshopGpuDevice.cs:196-209`, `:329-349`), passage collections (`GPU/PassageSensorDeclaration.cs:96-99`), and a full typed read with fresh arrays (`SIM/WorkshopGpuDevice.cs:284-294`). The pose ring's per-tick slot array and copies are `ENGINE-WIRE-ABI`'s (owner decision 24k), not this story's. The host activation network matches events O(V × N), checks duplicates O(V²) and propagates O(occurrences × E) (`GPU/ActivationNetwork.cs:335-342`, `:505`, `:689`).

*Contract:* pooled contact points, manifolds and keys; no per-pair lists; interaction state in sparse reusable tables; no immutable-dictionary rebuilds; a host tick path with no LINQ, closures or per-tick arrays, measured after `ENGINE-WIRE-ABI` has shrunk the per-tick read; activation events matched by compiled index and de-duplicated by a reusable set.

*Acceptance:* on R3, managed bytes allocated per steady-state tick at stable topology are zero after a stated warm-up (a deterministic counter); existing scenes keep goals, events and causal order, and positions within the game-grade envelope. *Performance:* R3 bytes and R6 publication time before and after.

*Deletes:* every allocation site listed.

*Depends on:* ENGINE-ISLANDS-SLEEP, ENGINE-CAPABILITY-INSTANCES, ENGINE-WIRE-ABI. *Unblocks:* no identity directly; the frozen workloads of P0-034.

### Story ENGINE-SOA-SIMD: Flat tables and four-wide contact solving
*Status: Backlog. Sprint key: `engine-soa-simd`. Order 10. Performance story (R1, R2, R3). Before further elements (owner decision 3a).*

*Outcome:* the engine matches its stated design: columnar tables compiled once at Run and kept across ticks, and contacts and narrowphase solved four at a time.

*Present today:* the world is array-of-structures byte records (`DOC:128`); each tick copies the world (`SIM/WorkshopPhysicsEngine.cs:254`) and decodes every body and collider into pooled class instances in lists and a dictionary (`:32-38`, `:396-543`; classes `SIM/WorkshopCollisionKernels.cs:29`, `:93`). `Vector128` appears only in joint rows gathered four at a time (`SIM/PrismaticConstraintSolver.cs:208-218`; `SIM/RevoluteConstraintSolver.cs:213-221`); the tensile row broadcasts one row into four lanes (`SIM/TensileConstraintSolver.cs:80-82`); contacts, friction, narrowphase and broadphase are scalar, one manifold at a time (`SIM/WorkshopConstraintSolver.cs:577-586`); cylinder–box costs 312 distance evaluations per pair (`DOC:210-211`); friction preparation scans driven surfaces per contact point (`SIM/WorkshopFrictionSolver.cs:124-144`). The build gate checks only that f32x4 instructions exist in four named types (`SIM/CuriousContraptions.Simulation.csproj:25-26`).

*Contract:* body, collider, contact and joint state in columnar, 64-byte-aligned tables compiled at Run and kept across ticks; contact rows solved four at a time within an island; narrowphase evaluated four lanes at a time for every solid shape pair (the Sphere, Box, Plane and Cylinder cells of the shape-pair table), while a pair that involves a hollow shape (Tube, Torus or Frustum, which keeps its hollow-side rule ahead of the table) stays scalar and is counted, not hidden; driven surfaces found by compiled index in friction preparation; gas, line and acoustic rows of later stories compile into the same flat tables; the build gate checks the kernels the design names. **Tables in shared memory (owner decision 24k; ABI audit G15).** The tables live in the simulation runtime's shared memory, which `ENGINE-WASM-THREADS` provides, laid out so an island's rows are contiguous: committed poses are written from the columns straight into the pose ring (`ENGINE-WIRE-ABI`), and the worker pool can solve islands on separate threads (`ENGINE-PARALLEL-ISLANDS`) without copying a table.

*Acceptance:* existing behaviour within the game-grade envelope on every lesson and law scene; goals, events and causal order unchanged. On R1 and R2 the story's deterministic counters hold, so it cannot pass with the scalar path unchanged: scalar contact rows solved per tick are zero (every row is in a four-lane group, a ragged last group padded with inactive lanes), and scalar narrowphase pairs per tick equal the number of candidate pairs that involve a hollow shape, counted from the compiled shapes, so every solid pair is in a four-lane group and the scalar remainder is exactly the hollow pairs the contract names; lane groups used per tick equal the sum over islands of the rows in the island divided by four, rounded up; world copies and decoded object instances per tick are zero; and every table a tick reads or writes lies inside the shared-memory region that `ENGINE-WASM-THREADS` provides, the same region the pose ring's producer reads. *Performance:* R1 and R2 tick time and R3 bytes before and after; must not regress.

*Deletes:* the per-tick world copy and decode into objects; the object graph `SimBody` / `SimCollider`; the scalar contact path.

*Depends on:* ENGINE-TICK-ALLOCATION, ENGINE-WASM-THREADS. *Unblocks:* no identity directly; the REQ-04 tick target at P0-034. If the target is missed there, `ENGINE-PARALLEL-ISLANDS` follows.

### Domain stories in owner decision 18's order: gas, then liquid, then thermal

### Story ENGINE-GAS-NETWORK: Connected finite gas stores, lines and valves
*Status: Backlog. Sprint key: `engine-gas-network`. Order 11. First domain story after the programme (owner decision 18).*

*Outcome:* finite gas stores joined by lines through typed ports equalise by conserved transfer, valves and vents are commanded by signals, and pressure does work on declared coordinates.

*Present today:* one finite gas store per body with an isobaric envelope, coupled to a sphere only (`ABS/GasCouplingValues.cs:10`; `ABS/IsobaricGas.cs:133`), which refuses refill; conical airflow sources and receivers; rotary airflow capture; ambient load. A fluid port contract (`IFluidElement`, `FluidPortDescriptor`, `FluidProperties`; `CuriousContraptions.Abstractions.Fluid/FluidContractValues.cs:12`, `:18`) is declared, serialised and validated with no solver or consumer. Slider damping is one coefficient; the travel observation takes a revolute coordinate and fires once (`ABS/CoordinateTravelValues.cs:13-17`).

*Contract:*
- **One fluid port record (coordinator decision 16).** A fluid port is a node, a role, an aperture and a socket (liquid L5). A socket is either a scene mouth joined by adjacency (mouth-joined) or a link terminal joined by a connection of a fluid connection material (link-joined); both compile to the same network edge, and no join rule depends on the medium. A gas port is role Bidirectional (flow follows pressure) with a link-joined socket whose authoring end is Input or Output, and its aperture is the throat the connection kind declares (pneumatic F3, F10); no `PortRole` member is added for gas. This record serves gas and liquid; `IFluidElement` is reshaped into it or deleted. The edge between a gas node and a liquid node is `ENGINE-LIQUID`'s (coordinator decision 26), not this story's.
- **Stores.** A rigid fixed-volume store that loses and gains mass; a sealed store whose volume follows a prismatic coordinate; a fixed-volume store vented to ambient that exchanges counted mass as its temperature changes. The sphere-only coupling is removed. **Mixtures (coordinator decision 26; pneumatic P1).** A store may hold one carrier gas and one vapour component (air and water vapour) at one temperature: mass, internal energy and pressure are sums over the components by Dalton's law, U = Σ mᵢ·Cvᵢ·T and p = Σ mᵢ·Rᵢ·T ÷ V, each component's partial pressure mᵢ·Rᵢ·T ÷ V is a committed read, and flow carries each component in proportion to its mass fraction. Each component is a calorically perfect ideal gas with declared limits (pneumatic P14); heat reaches a store only through `ENGINE-THERMAL`, which moves none into a store at its temperature or pressure limit (thermal T9), so the clamp never destroys heat.
- **Step (coordinator decision 26; pneumatic F5).** The gas step runs once per 1/480 s substep, first in the substep and before the liquid step, at its slot in the [combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order), and is not repeated by a solver trial replay (as the delivered advance runs, `SIM/WorkshopPhysicsEngine.cs:994`). Paths are applied one at a time in compiled order from current state, each moving at most the amount that equalises the two volumes it joins, so no node gives more than it holds, even a node joined to several paths; for a mixture the equalising amount is set by total pressure. Work per substep is one row per store and per path.
- **Lines.** A fluid connection kind whose line volume and throat are bundle data, with a `LineEnd { Sealed, Vent }` free end, a save field, a Connect control and tube presentation; the strand collider is the connection material's (`ENGINE-CONNECTION-MATERIALS`). A line starts at the pressure of the store on its Output end, else the store on its Input end, else ambient. Several lines on one Output; vents as declaration data on any Fluid port, Input or Output end (a vent on an Input end is admitted); flow through a declared throat follows pressure in either direction.
- **Valves and paths.** One-way and routed paths selected by one signal memory node; an exhaust port that vents when unconnected and that a Choice can close; a one-way path with a cracking pressure. **A cracking relief, each gas step:** it is open while its upstream store's absolute pressure is at or above the declared cracking pressure and flow would leave toward its downstream side, and closed otherwise; while open it passes the one throat law's flow for that step (choked or subsonic by the pressure ratio), clamped like every path; it is re-tested every step, so a store held at its crack rises above it by at most one step's inflow and the relief opens and shuts with no hysteresis rule of its own. **Venting a mixed head:** a store holding air and steam vents through any path at the mixture's mass-weighted R and Cv (so the mixture's own γ and critical ratio set choking), each component leaving in proportion to its mass fraction; a path to ambient never draws the store below ambient.
- **Work.** A work target that compresses ambient gas into a line with declared efficiency and rating and, for a shaft input, a power-limited resisting torque with a cap (debited under `ENGINE-SUPPLY-DEMAND`); an axial chamber on a prismatic coordinate, including two chambers on one coordinate; pressure work paid from the store; an expanding work target using the flow's declared Cp, γ and efficiency, shaft work removed from downstream enthalpy.
- **Jets (boundary with `ENGINE-REACTION-THRUST`).** The converging-nozzle law with one-way ambient refill, and an airflow source fed by a gas path whose force and reach are declared functions of committed thrust, belong here. The reaction of any airflow source on its own frame body when nothing is in its cone belongs to `ENGINE-REACTION-THRUST`; a gas-fed source on a dynamic body uses both.
- **Prismatic additions its first consumer needs:** slider damping declared per direction of travel (every existing package declares both coefficients; no defaulted second field) and a repeatable stroke occurrence on a prismatic coordinate.
- **Observations and units.** Committed pressure, partial pressure, temperature and mass-rate observations at a store, line or inlet (thresholds over them are `ENGINE-SIGNAL-NODES`'); presentation drivers for pressure, mass rate and regime; unit members Pa, m³ and m² (this story lands before `ENGINE-LIQUID`, so it owns them); `PortId`s `GasIn` (Input end), `GasOut`, `PortA`, `PortB` and `Exhaust` (Output ends), and named inlets for a part with more than one.
- Pressures are pascals absolute with ambient 101 325 Pa and the thermal conventions' ambient temperature (288 K).

*Acceptance:*
- Two stores joined by a line equalise when a valve opens, total mass unchanged; a closed valve or disconnected line transfers none; with every valve closed no store changes at Run start.
- A store on a Vent end falls to ambient and never below; on a Sealed end mass is unchanged and nothing moves.
- A chamber drives a prismatic coordinate through pressure work paid from the store; an exhausted store does no work; two opposed chambers on one coordinate drive it both ways.
- Mass enters a compressed line only while work is supplied and stops at the rating; gas leaving through an exhaust joined to a line arrives in that line.
- A receiver beyond the thrust-scaled reach receives zero impulse.
- A slider declaring different damping per direction moves faster one way than the other under the same force; a stroke occurrence repeats on every stroke.
- A line tee'd from one Output to two small stores at ambient never leaves the feeding store below the stores it feeds and never lets it give more than it holds; total mass is unchanged.
- A store holding air and vapour reads a pressure equal to the sum of its partial pressures, and gas leaving it carries each component in proportion to its mass fraction.
- The same equalisation run at 60, 120 and 240 Hz cadences reaches the same final pressures within pressure 2000 Pa or 2 %, whichever is larger (the pneumatic family's named pressure tolerance: the 2 % is far inside the gauge's 10 % hysteresis band, so a cadence that shifts a settled pressure by more than a gauge reading would fail).

*Deletes:* the sphere-only gas coupling; the store's refusal of refill as a separate rule; `IFluidElement` in its present form; the single slider damping coefficient; the one-shot revolute-only travel observation as a separate kind.

*Depends on:* the engine programme; ENGINE-SIGNAL-NODES (threshold, memory routing); ENGINE-CONNECTION-MATERIALS (solid hose strand); ENGINE-RUN-LENGTH (rows over 30 s). *Unblocks:* CAT-003 (re-qualified on the new store), CAT-010, EL-037, EL-038, EL-039, EL-040, EL-041, EL-042, EL-043, EL-045, EL-230, EL-231, TH-17, TH-18, TH-19, TH-20, TH-24, TH-25, TH-35; and the fluid port record and units of EL-001, EL-002, EL-014, EL-015, EL-026, EL-027, EL-030, EL-031, EL-032, EL-033, EL-034, EL-168, EL-171, EL-229, RAD-17 and TH-11.

### Story ENGINE-LIQUID: Conserved liquid inventory, head-driven flow and immersion
*Status: Backlog. Sprint key: `engine-liquid`. Order 12. After ENGINE-GAS-NETWORK (owner decision 18). Performance story (R4 with stores and streams). The liquid conventions are owned by the [EL-001 spec](../../docs/planning/elements/EL-001-finite-reservoir.md) (L1 to L18); this story implements them.*

*Outcome:* a liquid network compiled at Run from declared stores, fittings, sinks and sources conserves volume, flows by head through apertures, spills streams by sampled paths, and buoys bodies by displaced volume.

*Present today:* no liquid solver, vessel or flow; buoyancy exists only for a gas-filled body in air. Liquid ports will use the fluid port record of `ENGINE-GAS-NETWORK`.

*Contract:*
- **Network.** Stores, zero-volume fittings, sinks and sources, with four world accounts, `Spilled`, `Drained`, `Supplied` and `Evaporated` (liquid L3; coordinator decision 26); at every committed tick Σ store volumes (pending overflow and prime inventories included) + `Spilled` + `Drained` + `Evaporated` = Σ authored initial volumes + `Supplied` (liquid L10). Each pair of joined snap sockets of equal bore compiles to one edge (socket lists are `ENGINE-CAPABILITY-INSTANCES`'); fittings joined to fittings merge into one node. The liquid is one declared material row (density, immersion damping, discharge coefficient, and the rows other capabilities attach).
- **Stores.** Capacity, plan area, interior height, floor datum points and an interior region, any optionally parameter-bound. Level follows volume, displaced volume and committed tilt by one relation; retained capacity is that relation at the rim. A store may be sealed and compliant (mouth head proportional to volume held, no interior), porous (keeps contents at any orientation, interior is its carrying collider, may take liquid up from a store its body is immersed in at a declared rate), or have capacity bound to a committed coordinate (excess held as pending overflow, leaving through a declared relief mouth next substep). Nested interiors resolve innermost first. A mouth may lie below its store's floor datum. Immersed ports have no socket; their node is the innermost interior containing their point, wet while that surface is above it.
- **Flow.** Head is lost only at declared apertures by one orifice law; fitting flows sum to zero. Liquid passes a mouth datum only while the highest total head among the wet stores feeding that fitting, plus the head added by granted work targets between them, is at or above it, unless the fitting declares a crest (crest datum, prime volume, self-priming rate) and its prime is full; a dry intake breaks it; there is no lift limit. Each substep is one pass: aperture flows from committed heads, then stream credits. Volume above retained capacity is pending overflow leaving next substep. A store never gives more than it holds; in the wedge regime it never receives more than raises its surface to the supplying head; a transfer between two stores never passes levelling; after the declared sweeps a fitting's outflows are scaled to its inflows.
- **Streams.** A free end discharges a stream at the contracted-jet speed along its normal plus the owning body's velocity, as a fixed declared number of sample paths each carrying an equal share; a sample that ends inside a store's or sink's declared interior is credited in the same step; one that meets any other collider is spilled; no collider is exempt by kind; no in-flight inventory. Each sample path is a declared fixed number of segment casts of `ENGINE-SPATIAL-QUERIES` along its ballistic arc, ending at the world bound (liquid L18).
- **Bodies.** A dynamic body's displacement shapes (its colliders, or a declared envelope, counted as their union) inside an interior receive buoyancy and damping from submerged volume and raise the level; the region inside them is excluded from the surrounding store; buoyancy on a body inside a carried store is reacted on the carrying body. A store on a dynamic body adds its contents to that body's mass at the centroid of the liquid region at the committed pose, with no added inertia (a new mass-source kind), and captures at an aperture that follows the body. A declared capture region may do paid work on a declared coordinate, at most a declared fraction of ρ·g × descent per unit volume, and releases the volume from a declared outlet; a store whose capacity is bound to a prismatic coordinate receives force ρ·g·h·A on it from the head at its intake, paid by the volume entering at that head.
- **Control.** A seat's opening follows a parameter (including one value per Choice option), a committed coordinate, a memory node or any committed scalar through a clamped linear map; a seat may be one-way with a cracking head. A work target adds head on a declared edge, limited by a declared maximum head and by efficiency × input power, with a declared sense on its coordinate input; energy taken = hydraulic work ÷ efficiency; an electrical input debits its grant; a shaft input meets a resisting torque of that power ÷ shaft speed, capped. An ungranted work target adds no head and is an open edge.
- **Gas edge (coordinator decision 26; liquid L15).** The edge between a gas node and a liquid node is this story's; it lands after `ENGINE-GAS-NETWORK`. It is a work target whose input is a gas store: the energy taken (hydraulic work ÷ efficiency) is debited from that store's internal energy, the edge adds no head while the store's pressure does not exceed the liquid node's head pressure at the edge, it has its own head cap in metres, and it reads the gas pressure the gas step of the same substep has just committed. There is no pascal equality across the two networks.
- **Exchange.** A declared exchange moves volume between a store and another capability's ledger (phase change), counted in `Drained` (liquid leaving) and `Supplied` (liquid arriving); evaporation to the world is counted in `Evaporated` only, never in `Drained` (liquid L3); a phase-row source or sink is not subject to the zero-flow threshold and is checked as mass; it may deliver into a zero-volume fitting at its committed rate, including on a dynamic body.
- **Observations.** Store volume, depth, fraction and level; aperture flow; node pressure; account totals; seat opening; probe depth (a depth probe reads the store whose interior contains its point); prime fraction; cumulative volume per declared fitting. Readable by presentation drivers, threshold nodes and goals. Unit m³/s (Pa and m³ are `ENGINE-GAS-NETWORK`'s). Stores accumulate with the engine's bulk-and-remainder pair.
- **Bounds.** The step runs once per substep, right after the gas step and before supply grants, at its slot in the [combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order) (liquid L16), and is not repeated by a solver trial replay; it carries `ENGINE-THERMAL`'s enthalpy column, which moves each transfer's heat with its volume (thermal T2). A change of contents or level wakes the islands it loads. Work per substep is bounded by the compiled counts of stores, apertures, fittings, seats, probes, envelopes and work targets and by the declared sweep, sample and cast counts, which the story states (per-declaration limits); tables are sized at Run with no scene ceiling and no scene-capacity refusal (D5).

*Acceptance:*
- An elevated store joined to a lower one flows until levels balance or the source empties; total volume is unchanged; an empty source supplies none.
- A body less dense than the liquid floats at the draught its mass requires and a denser one sinks; a hollow body with a declared envelope floats at its draught and sinks when its mass exceeds the envelope's displacement; a body resting in a floating hull is buoyed only by the liquid in the hull; a carried vessel weighs the same whether a body in it rests on its floor or floats.
- A route's flow is unchanged when a lossless fitting is inserted; two equal apertures in series pass 1/√2 of one.
- A route with no granted work target whose highest mouth datum is above its supply surface passes nothing however low its outlet; a crested fitting with a short prime passes nothing, primed passes the aperture law's flow, stops when its intake uncovers and does not restart by itself.
- A tilted store's reported depth and its overflow point agree; a cascade of three full stores conserves volume at every committed tick; a body lowered into a full store spills its displaced volume.
- A stream sample ending inside an interior adds exactly its share; one meeting another collider first adds to the spilled account; a body floating inside a store does not spill a stream landing on it.
- A seat driven by a held coordinate passes its held opening; a one-way seat passes nothing against its direction at any head.
- A work target with no grant adds no head and passes flow downhill; the energy taken is never less than hydraulic work ÷ efficiency; a dry or stalled work target takes none; granted shaft work lifts liquid above its supply surface.
- A sealed compliant store charges only to the head that feeds it; lowering a bound capacity moves exactly the excess through the relief mouth next substep and raising it draws nothing back; a porous store on an immersed body gains what the surrounding store loses and stops at capacity.
- A twin-chamber rocker whose contents act at their centroids tips when the raised chamber's moment passes its hold and stays on its stop below it.
- A fitting's cumulative volume equals what the stores upstream lost, whatever the sampling interval.
- A gas store driving liquid up through a gas edge loses the energy the lift took (hydraulic work ÷ efficiency) and lifts nothing while its pressure does not exceed the head pressure at the edge; a store at 400 000 Pa lifts no higher than the edge's head cap.
- The four-account ledger closes at every committed tick, including volume a phase-row exchange moves (counted in `Drained` and `Supplied`).
- After two stores joined by one aperture have levelled, their level difference stays within the stated overshoot bound and reported flow stays under the stated no-flow allowance (L17 of the liquid conventions).
- *Performance:* R4 with stores and streams before and after.

*Deletes:* nothing in the engine (no liquid code exists); the per-material stream flag of earlier drafts is withdrawn.

*Depends on:* ENGINE-GAS-NETWORK (port record, units, and the gas store a gas edge debits); the engine programme (spatial queries, islands, capability instances, supply demand); ENGINE-SIGNAL-NODES; ENGINE-PARAMETER-BOUND-FRAMES; ENGINE-JOINT-KINDS (spring rest position of channel gates); ENGINE-RUN-LENGTH. *Unblocks:* EL-001 to EL-036 (the Water identities ledgered before 21 November 2026), EL-052, EL-165 to EL-172, EL-229, EL-240, TH-11, TH-13, TH-14, TH-15, TH-17, TH-18, TH-28, TH-35, RAD-17, the liquid crossing of EL-130, and the liquid buoyancy rows of CAT-001, CAT-014, EL-187 and EL-188.

### Story ENGINE-THERMAL: Heat storage, transfer and phase change
*Status: Backlog. Sprint key: `engine-thermal`. Order 13. After ENGINE-GAS-NETWORK and ENGINE-LIQUID (owner decision 18: the 3a programme → gas network → liquid → thermal → the rest), so campaign levels 14 to 20 and 39 to 41 can teach thermal parts. The thermal conventions are owned by [TH-04 section 11](../../docs/planning/elements/TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14).*

*Outcome:* declared thermal nodes store heat, exchange it by one link law, change phase with conserved latent heat, expand declared coordinates and convert heat to supply only through a finite store.

*Present today:* a gas temperature inside the isobaric gas law only, entering at charge (`ABS/IsobaricGas.cs`); no thermal node, link, unit or driver (`ABS/ParameterDescriptor.cs:15-35` ends at `RadianPerSecondPerNewton`; `ABS/ElementPresentation.cs:18-37` has no thermal driver); a source's store has four parameter-bound values filled only by its initial charge.

*Contract:* (the thermal conventions T1 to T14 as repaired on 23 November 2026 under coordinator decisions 26 and 28; the tick order is the [combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order), which this story cites and does not restate)
- **Nodes.** A lumped node on any body: heat capacity, initial temperature (fixed, parameter-bound or Choice-selected, including below ambient), ambient conductance and faces (a collider or a declared disc aperture, each with a face conductance). Every liquid store is also a node with an authored starting temperature; inflow mixes by mass and outflow leaves at the store's temperature (owner decision 20). A gas store is a node through the energy it carries, Σ mᵢ·Cvᵢ·T over its components (pneumatic P1). Units kelvin, J/K, W/K and W/(m·K).
- **Heat moves with liquid (coordinator decision 26; thermal T2, liquid L2).** This story adds an enthalpy column to `ENGINE-LIQUID`'s step, executed there per substep: every transfer of volume ΔV carries ΔV × the source store's enthalpy per unit volume H ÷ V; a fitting passes the mixed enthalpy per unit volume of its inflows; a stream credits its enthalpy with its volume; spilled and drained volume carries its enthalpy into that account. It reads no temperature and, with the liquid bound ΔV ≤ V, never makes H negative. The thermal step reads H as the last substep committed it and never moves advected heat itself.
- **One link law, one step.** Q̇ = G·ΔT on every link, applied each tick in compiled link order by the exact two-node exchange ΔQ = ΔT·C_r·(1 − e^(−G·dt ÷ C_r)), C_r = C_a·C_b ÷ (C_a + C_b) (C_r = C for a link to ambient or to a held temperature; a node holding at a phase threshold counts as held); a zero-capacity node is inactive. A gas store at its material's temperature or pressure limit (pneumatic P14) accepts no heat: a link or source that would raise it moves nothing in that direction, and the paying node or store keeps its energy (thermal T9; coordinator decision 26). Internal links (declared G, or a slab k·A ÷ L, length optionally parameter-bound). Contact links between two face colliders while their shapes touch within one declared tolerance, G = G_a·G_b ÷ (G_a + G_b): static pairs resolved once at Run through the query interface, pairs with a moving member added and removed on begin- and end-touch (`ENGINE-CONTACT-OBSERVATIONS`), held in a link table that persists while an island sleeps; a link never depends on contact force. Attachment links across a player-made connection whose sockets declare a face conductance. Wetted links scaled by fill fraction. Region links: a box on a frame body with a declared axis, shares by projected overlap, granted nearest-first and capped at a sum of 1. Ambient exchange to the world account `ToAmbient`, multiplied by (2.5 + u) ÷ 2.5 with u the committed airflow speed at one declared forced-air sample point per node (it takes no force); zero in Vacuum.
- **Sources.** Every source credits a node and debits a finite store by the same joules in the same tick (thermal T4):
  - a supply heat target, crediting the energy granted over the tick's substeps; while the store it heats is at its declared limit it accepts nothing and so requests nothing that tick, through `ENGINE-SUPPLY-DEMAND`'s rule for a consumer that can accept nothing (coordinator decision 30);
  - an inventory source paying a constant declared power from a finite inventory, restored on Reset;
  - a frictional source (contact friction, joint friction or a resisting drive, summed over all the tick's substeps from each substep's accepted solve only, credited by declared shares);
  - **absorbed light or radiation, booked once (coordinator decision 26; thermal T3):** where a collider is a face of a node with a declared absorptance a, a × the energy the one optical and radiation solve booked on that collider's absorbed record in the tick, after any store credit has taken its fraction, moves from that record to the node in the thermal step; the remainder stays on the record. A receiver's reading is an observation of flux and credits nothing. This row is delivered here as an interface whose end-to-end proof lands in `ENGINE-OPTICAL`, the first story that books absorbed energy;
  - a converter row between two nodes and one supply node with a declared bounded law whose flows sum to zero, refused at bundle time if its law can exceed the Carnot fraction.
- **Heat or light to supply: one form (coordinator decisions 21 and 26).** A credit of a declared fraction of converted or absorbed energy into a finite store that an ordinary supply source draws from, availability optionally gated by a threshold node. **The credited store** (a supply source whose finite store has a declared capacity, power limit, initial charge and enable, as constants or bound to parameters, and gains energy during a Run only by a credit) is one row of this story, for every filler, in the optical, radiation and thermal conventions alike (coordinator decision 26). The receiver credit, taken from the absorbing collider's record at the end of the optical solve, is `ENGINE-OPTICAL`'s row; a thermal converter's credit is this story's.
- **Phase rows** on a node or a liquid store (thermal T5):
  - melt and freeze with a latent amount;
  - boil at T_sat(p) from the saturation table of the declared "game steam" material row, moving m·Cp_v·T_sat out of the liquid store's node into a gas store's vapour component, the latent heat being the game water's declared value (owner decision 19);
  - **condense in one form, the wall-limited row (coordinator decision 26):** declared on a wetted link between a gas store holding vapour and the node that takes the latent heat, naming the liquid store that receives the condensate; while that node is below the dew point of the vapour's partial pressure, vapour moves at ṁ = G × (T_dew − T_node) ÷ L_v(T_dew), never more than is present, moving m·Cp_v·T_dew out of the gas store (m·c·T_dew into the liquid store, the latent m·L_v(T_dew) into the node); there is no other condensing rule;
  - evaporation at a declared rate × (1 − φ) × the airflow multiplier, φ one validated scene relative humidity (0 to 1, default 0.5), off in Vacuum, its mass counted in the liquid `Evaporated` account (liquid L3);
  - an ignite threshold handed to `ENGINE-IGNITION-BLAST`; any row may be one-way; committed phase fraction and solid amount;
  - a collider and its art active over a declared interval of a committed phase scalar, switched at a tick boundary (an inactive collider wakes the islands it supported); a body's mass equal to its committed solid mass with centre and inertia recomputed from its active colliders at a tick boundary;
  - a vapour expander (an expanding work target) declares an efficiency that is checked at bundle validation from declared values alone: the game-steam row declares the largest efficiency an expander may declare, η_max = 0.25 (thermal T11), and a declaration above it is refused with a typed reason, so no expander's best shaft power can reach the Carnot fraction of its source; nothing about the scene or the source is read.
- **Expansion.** A binding from a node's temperature to the rest position of one coordinate's position drive, with declared coefficient, stiffness and force or torque limit, active without a supply route; positive work debited from the node and negative work credited. The binding publishes its committed load (the force or torque its drive exerts) as a scalar observation and a presentation driver (coordinator decision 29).
- **Lift in air (coordinator decision 28; thermal T6).** Buoyancy in air is one law: a body declares its ambient lift, a declared acceleration (the delivered ambient load). A heated envelope's lift is that declared acceleration with its magnitude bound to the envelope store's committed temperature: a = A × (1 − T_ref ÷ T), A declared (decision 28's lift, envelope volume × reference air density × |g|, over the body's dry mass; the envelope air is not a mass source of the body), clamped to a declared range inside the 16 m/s² domain of a declared acceleration (`CuriousContraptions.Abstractions/WorldAccelerationDeclaration.cs:25`), read from the previous tick's committed temperature and acting from the next tick. `ENGINE-FIELD-FORCE` scales it by |effective gravity| ÷ |declared gravity| like any declared lift, bounded only by the body's one combined acceleration bound, and never twice. The reference air density is one declared value of the scene atmosphere, stated once in this story's Code Map. The binding mechanism is `ENGINE-PARAMETER-BOUND-FRAMES`' committed-state binding. There is no displaced-air law; buoyancy in liquid stays `ENGINE-LIQUID`'s displaced-volume law.
- **Observations.** Committed temperature, heat flow, phase fraction and expansion load as scalar observations for `ENGINE-SIGNAL-NODES`; presentation drivers for each.
- **Tick slot (thermal T8).** One thermal step per committed tick of duration dt, at its slot in the [combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order): after the last substep and the optical and radiation solve, before sensor evaluation and commit. It applies the tick's source credits first, then each face's absorptance share of what remains on its collider's absorbed record, then the link exchange and phase rows on the post-exchange values. It reads this tick's supply grants, the friction and drive work summed over the substeps (accepted solve only), the touching pairs as of the last substep, the absorbed records the solve has just booked and each liquid store's enthalpy as the last substep committed it. Thresholds over its results act from the next tick; mass a phase row moved enters its store at the tick boundary.
- **Ledger and bounds (thermal T13; coordinator decision 26).** Every committed tick: Σ node enthalpy + `ToAmbient` + the enthalpy carried into `Spilled`, into sink nodes (the part of `Drained` a sink received) and into `Evaporated`, and out of vents + the work done on bodies and on liquid (expansion drives, gas chambers, the gas input of a liquid edge) = Σ initial enthalpy + Σ source credits (including absorbed light and radiation moved from collider records) + the enthalpy brought in by source nodes (the part of `Supplied` a source delivered) − Σ converter work out. A phase row's exchange between a liquid store and a gas store stays inside Σ node enthalpy and is counted once. Reset restores authored temperatures, phases and stores and zeroes the account; a save holds authored values only. Work per tick is the compiled rows plus live pairs and region overlaps; tables are sized at Run with no scene ceiling and no scene-capacity refusal (D5).

*Acceptance:*
- A hot and a cold body in declared contact converge, total heat unchanged; an insulating declaration between them stops transfer; two static faces placed touching exchange heat and the same faces apart by more than the declared tolerance do not.
- A node held at its phase threshold changes phase only after the declared latent amount has been supplied; boiling a mass moves exactly the enthalpy its vapour carries and the ledger, with its sink, source and work terms, closes within 2 % over a boil-dry (integrated over hundreds of ticks; 2 % is under the smallest margin any thermal row uses), boiling counted once.
- A condense row moves vapour only while its node is below the dew point of the vapour's partial pressure, never more than is present, and conserves enthalpy as boiling does.
- Cold liquid passing through a small warm store in one tick never makes that store's enthalpy negative, and the heat each transfer carries equals the volume moved × its source's enthalpy per unit volume.
- A small node joined to a large conductance neither overshoots nor oscillates; the same exchange run at 60, 120 and 240 Hz cadences reaches the same temperature within the thermal temperature tolerance (T12).
- A stack that has gone to sleep keeps exchanging heat.
- Friction heat over a tick equals the friction work of the accepted solves of all its substeps; a supplied drive slipping at constant speed heats its nodes by torque × angle and a stalled one by nothing.
- A face with absorptance a gains a × the energy booked on its collider's record after any store credit, and the record keeps the rest; a reading alone credits nothing (interface test here; end-to-end in `ENGINE-OPTICAL`).
- A gas store heated to its pressure limit accepts no more heat; its heating link moves nothing further and its source keeps its energy, and an electrical heater on it is granted and debited nothing while it is at the limit.
- In Vacuum the ambient account stays zero and no evaporation occurs; an evaporating store's volume loss equals the `Evaporated` account's gain.
- Free expansion equals coefficient × rise within the game-grade position envelope; work on an elastic load equals the node's debit; a blocked expansion publishes its committed load.
- A heated envelope's lift is zero at T = T_ref, follows A × (1 − T_ref ÷ T) as its committed temperature rises, stops at its clamp, and changes one tick after the temperature does (its scaling by a field is proven in `ENGINE-FIELD-FORCE`).
- A vapour expander declared at an efficiency whose best shaft power could reach the Carnot fraction is refused at bundle validation; a lower efficiency is admitted.
- A converter's flows sum to zero and its supply output never exceeds the Carnot fraction of the heat passing; a declaration that could is refused; a credited store gains energy during a Run only by its credit and returns to its declared initial charge at Reset.

*Deletes:* the gas store's private temperature entry as a second heat rule (the gas store becomes a node); the parameter-bound-only form of a source's store values as the sole store kind.

*Depends on:* ENGINE-GAS-NETWORK, ENGINE-LIQUID, the engine programme (spatial queries, contact observations, islands, supply demand), ENGINE-SIGNAL-NODES, ENGINE-RUN-LENGTH, ENGINE-PARAMETER-BOUND-FRAMES (committed-state binding). *Unblocks:* TH-01 to TH-37 (including TH-23's committed load and TH-25's heated-envelope lift), the store thermal nodes of EL-001, EL-025, EL-026, EL-027, EL-029, EL-033, EL-034, EL-052, EL-168, EL-171 and EL-172, the thermal row of EL-055, the heat of EL-074, RAD-20, and the credited store of CAT-059, EL-154, EL-211 and RAD-12.

### Before their first consumer (owner decision 3a)

Each story below lands before the first element that needs it; where an earlier-listed story depends on it, it lands first (see the preamble). Each is element-free and carries the shared acceptance above.

### Story ENGINE-JOINT-KINDS: Joint kinds, holds and drives on a coordinate
*Status: Backlog. Sprint key: `engine-joint-kinds`. Order 14. Split out of ENGINE-ASSEMBLY-JOINTS; owns the fixed joint, spherical joint and dynamic-support hinge (coordinator decision 16), the spring rest position (owner decision 6; coordinator decisions 21 and 25) and every hold on a coordinate. May be delivered as separate slices (kinds; spring rest; holds; revolute position drive) before their first consumers.*

*Outcome:* packages can declare every joint and coordinate behaviour their mechanisms need, inside one package, on static or dynamic supports.

*Present today:* prismatic (5 lock rows, spring, two stops, position drive with speed, effort and power limits; `GPU/PrismaticRows.cs:6`) and revolute (5 lock rows, axial spring, stops, friction, velocity drive only; `GPU/RevoluteRows.cs:7-21`; `ABS/RevoluteConstraintValues.cs:25`); `CoordinateKind` has only `Revolute` and `Prismatic` (`ABS/PortDescriptor.cs:43-47`); no spherical, fixed or weld joint. A slider's spring rest must lie inside its travel (`CuriousContraptions.Bundle/MechanicalValueDomain.cs:78-81`); `Compression` is only the starting offset (`ABS/MechanicalContractValues.cs:111`; `GPU/WorkshopPhysicsCompiler.cs:159`). A constant axial spring on a limited hinge exists untested through a package; a continuous hinge must have stiffness 0 (`CuriousContraptions.Bundle/BundleValidator.cs:827-832`). A supplied zero-speed drive holds a shaft (`CuriousContraptions.tests/RevoluteDriveTests.cs:371`). Prismatic retention is a physical-request target (`ABS/PhysicalRequestValues.cs:6`).

*Contract:*
- **Kinds.** A fixed joint between two bodies of one package, including a package whose root body is dynamic and carries further bodies on joints, with a declared bodies-collide flag; a spherical joint with declared anchor frames, bearing damping and disabled mutual collision, whose moving body is a declared child body wherever a slider's or hinge's is; every joint kind, and a supplied drive on a hinge, qualified with a dynamic support body as well as a static one.
- **Spring rest position.** A spring on a prismatic or revolute coordinate declares a rest position that may lie outside the travel limits (a preload against a limit); travel admission accepts it; the starting offset stays a separate value. It applies to every spring-returned blade, hatch or gate that must close unpowered.
- **Holds on a coordinate.** A one-way hold on a revolute coordinate (free one declared way, resisting the other up to a declared torque capacity, direction selectable by a Choice), usable on a coordinate carrying both input and output drive ports; an indexed stop (a rigid hold at a declared angular pitch that a physical Release request frees for exactly one step and that re-locks at the next index, with a minimum carried torque below which a request is answered Empty and a request during a step answered AlreadyReleased); an authored-position retention for prismatic and revolute coordinates (holds the authored coordinate with no winding and no torque limit, released once per Run by a physical request; Reset re-arms it); each a physical-request target kind with its committed-released presentation driver; Reset clears pending requests. Revolute retention is this authored-position retention on a revolute coordinate, owned here like every hold on a coordinate and its release by physical request (C2 of the engine stories index); `ENGINE-ASSEMBLY-JOINTS` owns only the commanded release of a connection or attachment.
- **Re-arm at rest (coordinator decisions 31 and 33).** A released retention or hold on a prismatic or revolute coordinate re-arms when its coordinate comes to rest within a declared re-arm distance of its hold position, not only within the solver slop of travel 0 (`CuriousContraptions.Simulation/SimPrismaticRetention.cs:85`). At rest means the coordinate's speed has stayed below a declared rest speed for a declared settle time. The re-arm distance, rest speed and settle time are declared values of the retention, in its coordinate's units (m or rad, m/s or rad/s, s), with their domains and defaults stated in this story's Code Map; its first consumers declare all three (each a 0.1 m re-arm distance, a rest speed of 0.05 m/s and a settle time of 0.5 s; coordinator decisions 33, 34 and 37) and keep rows asserting the re-arm.
- **Revolute position drive.** The counterpart of the prismatic position drive: a target on the unwrapped angle advanced by a declared pitch per accepted physical request, with declared maximum speed, stopping acceleration, torque and power bounds and a self-lock inactive response; a request during a step or while unsupplied is refused with a typed answer. A signal-commanded strike-and-hold is a velocity drive against a stop on a limited hinge, qualified here.
- **Observations for contacts.** The committed coordinate window state and a drive's committed Blocked phase are published as observations; the supply contacts that read them are `ENGINE-SIGNAL-NODES`' rows.
- **A mechanical input on a driven coordinate.** The story settles whether a mechanical input may couple a coordinate that also carries a supplied drive: its planning probe runs the first mechanical consumer's row that couples a mechanical input to a coordinate whose supplied drive is unsupplied (bound zero) and states the answer in its Code Map; a refusal, or a coupling row and a drive row that fight, is repaired here, not in the consumer's package.
- **Crank coupling (coordinator decision 29).** A crank between a revolute and a prismatic coordinate is a rod body with its own collider, pinned at both ends by dynamic-support hinges (one on the crank's body, one on the slider's carried body); no coupling law is added. It is qualified here.

*Acceptance:*
- Locked freedoms of each kind hold within the game-grade envelope under load, the free ones move, and drive effort never exceeds its declared limit. A fixed joint holds a cantilevered member with an end load within ±0.02 m for 5 s (the end-point tolerance of the shared set, under the smallest socket clearance a cantilevered member must keep).
- A body on a spherical joint swings in any direction about its anchor, holds the anchor within the envelope, comes to rest under declared damping, and Reset restores its pose.
- An unpowered spring-closed slider closes upright, on its side and inverted; a declared rest outside travel is admitted.
- A one-way hold passes one way and holds the other up to its capacity; an indexed stop frees exactly one pitch per request; a retained coordinate holds until its request and releases once.
- A released retention whose coordinate stays below its declared rest speed for its declared settle time within its declared re-arm distance re-arms; one that comes to rest outside the distance, or has not yet stayed below the rest speed for the settle time, does not.
- A revolute position drive steps one pitch per accepted request within its bounds and refuses a request during a step.
- A rod pinned between a driven crank and a slider moves the slider through a stroke of twice the crank radius per turn within the game-grade envelope, keeps both pins within the envelope, and collides with a ball placed in its path.

*Deletes:* the refusal of a rest position outside travel; the reading of `Compression` as a rest position; prismatic-only forms of holds where a general form replaces them; the re-arm only within the solver slop of travel 0 (`ActuatorStopSlopMetres`).

*Depends on:* the engine programme. *Unblocks:* CAT-007, CAT-029, CAT-051, CAT-069, EL-021, EL-056, EL-057, EL-061, EL-078, EL-109, EL-112, EL-113, EL-114, EL-115, EL-117, EL-159, EL-160, EL-161, EL-167, EL-168, EL-170, EL-171, EL-184, EL-185, EL-186, EL-210, EL-218, EL-221, EL-222, EL-224, EL-225, EL-226, EL-228, EL-241, GAP-04, GAP-06, RAD-06, RAD-21, TH-33.

### Story ENGINE-MECHANICAL-LINKS: Force-bounded couplings and toothed meshes
*Status: Backlog. Sprint key: `engine-mechanical-link-kinds`. Order 15. After ENGINE-CONNECTION-MATERIALS, whose material record it uses.*

*Outcome:* a mechanical connection may carry a player-chosen material whose coupling law slips above a declared capacity or meshes teeth; the delivered rigid link is unchanged and stays the default (owner decision 2).

*Present today:* a mechanical connection compiles to a bilateral ratio coupling between two revolute coordinates and carries no values (`GPU/WorkshopConnections.cs:166`, `:182-192`); link routes are drawn from cosmetic rim radii (`ABS/MechanicalLinkPresentationValues.cs:9`); `MechanicalTransmissionDescriptor` couples two coordinates by a ratio inside one package (`SIM/MechanicalTransmissionSolver.cs`).

*Contract:*
- The material record itself is `ENGINE-CONNECTION-MATERIALS`'. This story adds the mechanical law kinds a material may name: a force-bounded ratio coupling whose transmitted force is bounded by a capacity computed from declared tension, friction coefficient and wrap angle, slipping beyond it, with the slip recorded as a ledger loss and readable as the link's own travel; a toothed mesh with reversed sign and ratio from declared tooth counts, adjacency checked at connection.
- Each mechanical port declares a physical pitch radius distinct from the cosmetic rim; capacity uses the smaller pitch radius of a pair. A material declares, as data, where its speed ratio comes from: the ports' coordinate ratio (the default, and the belt's), or the pitch-radius ratio of its two ports, ω_out = ω_in × r_in ÷ r_out, so the strand moves at one speed at both rims (the chain's; coordinator decision 24l). The coupling law is the same bilateral or force-bounded row either way; only its ratio value differs.
- Mechanical ports declare an engagement class; a material whose required class a port does not offer is refused with a typed reason.
- A closed ring of meshes turns when the product of its ratios is +1 and stays at rest otherwise.
- Link presentation draws a material's declared style (link-by-link route, rim teeth).
- Two transmissions composed in series inside one package are qualified here (coordinator decision 29): the composed ratio is the product of the two.

*Acceptance:* the same train just below and just above its declared capacity: only the second shows relative travel, and no sample gains energy; a material declaring the pitch-radius ratio turns two ports of unequal pitch radius at r_in ÷ r_out and two of equal radius at 1, while a coordinate-ratio material between the same ports keeps the coordinate ratio; a material offered to a port of the wrong engagement class is refused and changes nothing; two differently parameterised materials on two differently parameterised rotors; a mesh reverses direction at the tooth ratio; a ring whose ratio product is not +1 stays at rest; two transmissions in series inside one package turn their output at the product of their ratios.

*Deletes:* the use of the cosmetic rim radius as a physical value; the material-free mechanical connection record as the only form.

*Depends on:* ENGINE-CONNECTION-MATERIALS. *Unblocks:* EL-054, EL-200, EL-201, EL-232. (EL-218's crank is `ENGINE-JOINT-KINDS`', coordinator decision 29.)

### Story ENGINE-TENSILE-EXTENSIONS: Dynamic guides, winding and cutting
*Status: Backlog. Sprint key: `engine-tensile-extensions`. Order 16. The cutting clause is the one cutting-work law (coordinator decision 16) and runs after ENGINE-CONNECTION-MATERIALS; the other clauses may land first.*

*Outcome:* tensile routes react on moving guides, wind on drums, and are severed by blade pairs through declared cutting work.

*Present today:* route geometry follows each guide body's pose, but the length row's impulse and effective mass use the two end bodies only (`SIM/TensileConstraintSolver.cs:56`, `:64-69`); a commanded release removes a route's tensile rows at a committed step (`ABS/RuntimeServices.cs:77`); the player's Run-time cut is kept (owner decision of 22 October 2026).

*Contract:*
- **Dynamic guide.** For each guide of a taut route, tension is applied to the guide body at its entry and exit tangent points along the two unit tangents, with every dynamic guide body in the row's effective mass.
- **Winding.** A winch winds a tensile span: the wound end is declared (a winding naming the revolute coordinate and radius), on a driven or undriven hinge of any package; paid-out length follows the coordinate at the radius between a declared minimum and capacity; capacity bounds the route's total length; at Run start the drum holds capacity minus the connect-time length; tension reacts on the coordinate; a route longer than capacity is refused; the wound length is on the read.
- **Cutting.** A blade pair: a fixed straight edge on one body and a straight edge on a body moving on one revolute coordinate, both through the hinge axis, with a blade thickness and a capacity C (J, constant or parameter-bound). A target is a tensile span or an attachment seam (a declared straight segment with thickness d and cutting resistance R on a body held by a fixed joint). A target crossing the blade plane within the thickness, between the edges, engages when the perpendicular gap at the crossing point falls to d; while engaged and closing it opposes the moving edge with force R ÷ d at the crossing point and never pushes the blades apart; both edges load the target equally. If R ≤ C the target is severed when the gap reaches 0, having absorbed R (a seam releases its fixed joint one-way; Reset restores it); if R > C the gap is held at d. Absorbed work leaves the mechanism and is a committed read on the cutter. Blade-edge colliders do not collide with a target's strand or seam piece. Candidates come from a bounded query.
- An elastic span is a material value of `ENGINE-CONNECTION-MATERIALS`, not a clause here.

*Acceptance:*
- Two differently parameterised dynamic guides each hold at half the free-end travel; an isolated two-load-plus-guide system gains no momentum or energy; a static guide behaves as today.
- A winch pays out and winds within its capacity; an over-length route is refused.
- A span crossing a blade pair at distance x: torque just below and just above R·x ÷ d severs only for the second; the cut-work read equals R and the mechanism's energy falls by R; a material whose R exceeds capacity holds the gap at d for any input; a target outside the blade thickness or beyond the edges never engages; a seam and a span of equal R and d cut at the same torque; Reset restores the route.

*Deletes:* the end-bodies-only effective mass of the length row.

*Depends on:* the engine programme; ENGINE-JOINT-KINDS (fixed joint for seams); ENGINE-CONNECTION-MATERIALS (cutting clause). *Unblocks:* EL-063, EL-066, EL-067, EL-068, EL-078, EL-085, EL-207, EL-225 (EL-069 and EL-070 if their probe shows the guide need).

### Story ENGINE-SIGNAL-NODES: Rows of the one compiled signal and supply network
*Status: Backlog. Sprint key: `engine-signal-nodes`. Order 17. One story because every item is a row of the same compiled network (coordinator decisions 16 and 25); the self-edge, saturating-count, edge, count-reset, contact-setting, oscillator-supply and rearming-timer slices of earlier drafts are rows here and keep no names of their own. Its threshold row lands before ENGINE-GAS-NETWORK.*

*Outcome:* the signal network can threshold any committed scalar, detect edges, count, reset, combine levels and drive supply contacts from levels and coordinate states, as declaration data.

*Present today:* only a contact load is thresholded (`ABS/SignalContractValues.cs:58`); a signal node on how far a body has turned fires once (`SIM/WorkshopPhysicsEngine.cs:1349-1360`); only the oscillator carries a supply input (`ABS/SignalContractValues.cs:61`; `GPU/WorkshopActivationCompiler.cs:72`); `SignalContactState` has only `Latched` and `TimerCounting` (`ABS/SignalContactControl.cs:5`); a pulse-count node's committed count keeps rising past its target (`GPU/ActivationMemory.cs:13-14`); every self-edge is refused as `InvalidConnection` before the monotone cycle rule (`GPU/SupplyRoute.cs:50-61`); a rearming timer cannot emit an elapsed signal (`CuriousContraptions.Bundle/SignalBundleValidation.cs:157`); a Level output is exercised only into a level-indicator input; the prismatic endpoint contact uses one fixed slop for close and open and reads only a supplied drive's actuator row (`SIM/WorkshopGpuDevice.cs:338`).

*Rows:*
- **Threshold.** A node over any declared committed scalar observation of any domain (contact load, coordinate angle or travel, gas pressure and mass rate, liquid depth, flow, pressure and cumulative volume, acoustic band level, optical strength, accumulator value, store fraction, a consumer's committed grant, temperature, phase fraction) with on and off levels or a release ratio, optional assert and release holds in seconds and a separate re-arm level; initial state from the authored state at compile; its state drives a Level output, a supply contact and a presentation binding; its crossing may emit a Pulse routed in the tick it is published. Domain stories add only their observation kinds.
- **Edge.** A node over a declared electrical input or a supply contact of its own package, typed polarity (Rising, Falling, both), sampled beside the oscillators; one Pulse per transition, routed in the same tick, repeatable; none at Run start; `previous` is committed memory, false at admission and Reset.
- **Count.** Saturation: `count = min(count + 1, target)`, emitting once on reaching the target; a `Reset` Pulse terminal; count and reset requests of a tick resolve once per node at the boundary (any reset gives count 0, phase Clear and no emission).
- **Boolean over levels.** A node over two or more signal levels with the closed `LogicGateOperation` set; a feed-forward chain resolves within one tick; only a loop costs a tick. The two-input boolean signal node that specs name is this row with two inputs, owned here; the N-input boolean supply node of engine gap E2 is `ENGINE-CAPABILITY-INSTANCES`' reshaping of the delivered two-input supply node, which this row reads like any other signal level.
- **Self-edge and cycles.** A self-edge on a monotone logic node is admitted and never powers itself; on a nonmonotone node it is refused as `NonmonotoneFeedback`, at construction, at load and in the bundle validator; a contact feeding its own `PowerIn` is admitted and inert; a cycle through an oscillator (at least one interval) is admitted; zero-delay nonmonotone feedback stays refused. An oscillator's enable may name a public `PowerIn` or a supply node of its own package.
- **Rearming timer.** A timer whose completion policy is Rearm and whose elapsed signal is Activation is admitted, emits one activation on elapse and is ready again; busy inputs neither restart nor re-emit. A timer's duration may be zero (a zero delay): every timer is due at the later of its start boundary plus its duration in ticks and the first tick boundary strictly after its start pulse, so no timer elapses in the routing pass that started it: a loop through a zero-duration timer costs one tick per pass, like any loop through a node, and a chain or loop of zero-duration timers emits at most once per tick.
- **Contacts.** `SignalStateContactControl`'s state set (today only `Latched` and `TimerCounting`, `CuriousContraptions.Abstractions/SignalContactControl.cs:5`) gains the threshold, boolean and level states; a threshold node presents one state, `Asserted`: the contact is closed while the node's committed level is asserted (after its assert hold) and open once it has released (after its release hold); a supply contact may declare one control per option of a Choice parameter, or a fixed open or closed control ("contact set by a parameter", coordinator decision 25), with a `CommittedContactClosed` driver that reads closure irrespective of supply; a prismatic endpoint contact takes a declared release travel and may read the coordinate of a passive slider; contacts closed while a revolute coordinate rests in a declared angular window (repeating at a declared pitch, slower than a declared speed) or while a drive is Blocked read `ENGINE-JOINT-KINDS`' observations. A Level output drives any typed signal input of matching kind.
- **Wiring.** A package-internal edge from a pulse to a physical-request input.
- A presentation binding may read the committed phase of any node.

*Acceptance:* one fixture per row on two differently parameterised declarations; in particular: a threshold over two different scalar kinds asserts and releases at its levels and holds; a held input emits nothing from an edge node and a one-tick opposite state re-arms it; a count with target N reads N after more than N occurrences and has emitted once, and Reset returns it to 0; a self-edge on a monotone node is inert and on a nonmonotone node refused; an oscillator whose pulses latch a count that opens its own enabling gate emits exactly the target number; a parameter-set contact closes for its option and an uncovered option is refused at bundle validation; a zero-duration rearming timer started inside a tick emits at the next boundary, and one started by another timer's elapse on a boundary emits at the boundary after it, never in the same tick; every existing counter, gate and latch test stays green.

#### Slice: self-edge row (a supply self-edge is a one-node cycle)
*The one current text of this row's slice. It replaces the drafted contract `spec-engine-supply-self-edge-cycle-rule.md`, retired on 23 November 2026 because it named this capability a second time (owner decision 3a; coordinator decision 16; index §3). Blocked: waits for the engine programme (owner decision 3a), retry when ENGINE-SOA-SIMD publishes; owner: coordinator. Planned then as a contract under this story's sprint key `engine-signal-nodes` (no key of its own), from the published main of that day; the planning scout re-verifies every citation below against ENGINE-CAPABILITY-INSTANCES' node renames (each was unchanged from `8d1d68aa` to `25df3cd5`). Engine tier, all six layers. It precedes the electrical-gates closure review (`spec-closure-review-electrical-gates.md`), whose rows G4 and G5 use a direct self-wire.*

*Defect:* a wire from a supply node's output to one of its own inputs is refused as `InvalidConnection` ("Unsupported or duplicate resolved supply edge.") before the cycle rule runs: `from == to` in `SupplyTopology.ValidateEdges` (`GPU/SupplyRoute.cs:50`; edit, save load and main compile) and in `CompiledSupplyGraph.AdmitEdges` (`:267`; worker admission through `GPU/PhysicsDeclarations.cs:223`), and `source.Id == target.Id` for declared edges (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:162`–`:163`). The one-node branch of `SupplyCycles.Cyclic` (`ABS/SupplyCycles.cs:67`–`:68`) is therefore unreachable, while a two-node loop is judged by the cycle rule (`GPU/SupplyRoute.cs:60`–`:74`); the requirement (`requirements.md:6689`) makes no exception. *Approach:* remove the three conditions so the existing rule (`SupplyCycles.Monotone`, `ABS/SupplyCycles.cs:24`, over `LogicGateOperation`) decides at bundle admission, edit, save load, main compile and worker admission alike; no new rule, state, schema, ABI, read model or reachable message text; the activation graph's own rules (`GPU/ActivationNetwork.cs:119`) are untouched.

*Matrix* (terminals `PowerIn`, `FirstIn`, `SecondIn`):
- **S1** A self-edge on a nonmonotone logic node (Xor, Nor, Nand), any terminal: refused before any solve as `NonmonotoneFeedback` with "Break the wire loop through parts N. This gate's output cannot feed its own inputs."; nothing installed.
- **S2** A self-edge on a monotone logic node (And, Or), any terminal: admitted; each tick it settles from unpowered and is never powered by its own output alone.
- **S3** An Or node self-fed on `FirstIn`, supplied on `PowerIn`, with a second source on `SecondIn` enabled then disabled: powered while it is enabled, unpowered on the next committed tick after.
- **S4** A contact node self-fed on `PowerIn`: admitted; never powered through it, open or closed.
- **S5** An S1 wire at each boundary: edit `NonmonotoneFeedback`; save load `InvalidConnection` with inner reason `NonmonotoneFeedback` (`GPU/WorkshopSavePrevalidation.cs:154`–`:156`); main compile refused with the installed scene unchanged; direct graph construction "A nonmonotone electrical gate lies on a directed cycle."
- **S6** An S2 wire saved, loaded, run on the worker and Reset: the load equals the save, the worker admits it, Reset restores the built state.
- **S7** Still `InvalidConnection`, unchanged: unknown node; duplicate edge, a duplicated self-edge included; consumer as source; source as target; `FirstIn` or `SecondIn` on a node that is not a logic node, a contact self-edge included.
- **S8** A package-declared internal self-edge: And or Or on any terminal, or a contact on `PowerIn`, admitted; Xor, Nor or Nand `NonmonotoneCycle`; a source, a consumer, a contact on `FirstIn`/`SecondIn` and a duplicated declared self-edge `Edge.Terminal`, unchanged.

*Decisions (standing delegation, 21 November 2026):* one rule for every directed supply cycle of any length, general over node kinds (S4), the same at bundle admission (S8); reachable message text unchanged, "parts N." included; the unreachable ownerless loop branch and its filter (`GPU/SupplyRoute.cs:63`–`:73`; owner 0 is refused at `:29`) are deleted; the Workbench connect tool's same-part refusal (`ui/WorkshopConnections.cs:106`) is unchanged, so a self-wire reaches the engine only through the edit interface, a save, authored content or a package's declared edges, and a loaded one is drawn and can be disconnected (`:95`, `:181`). No test pinned the old refusal on `8d1d68aa` (the only invalid-edge message assertion is the duplicate case, `PackageLocalPortsTests.cs:186`); the planning scout repeats the search, and a pinned test found later is rewritten to the row it contradicts.

*Owned scope:* `GPU/SupplyRoute.cs` (two condition removals, the ownerless branch, the comment at `:340`, which wrongly says a singleton is evaluated once), `CuriousContraptions.Bundle/SupplyBundleValidation.cs` (one removal), `ABS/SupplyCycles.cs` (one summary line: a self-edge is a one-node cycle), the new `CuriousContraptions.tests/SupplySelfEdgeTests.cs` (name checked at planning; no test-project edit, `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs`), `TODO.md` and the sprint status.

*Tests* (in `SupplySelfEdgeTests`; synthetic node ids through `ISupplyQueries.CreateTopology` and `new CompiledSupplyGraph(...)`; delivered parts only as test data; every assertion a typed refusal, a typed state or an equality of authored records, so no tolerance; 60-tick windows only to prove an absence):
- `ASelfEdgeOnANonmonotoneLogicNodeIsRefusedAsFeedback` (S1; Xor, Nor, Nand × three terminals; control: the graph without the self-edge is created).
- `ASelfEdgeOnAMonotoneLogicNodeIsAdmittedAndNeverPowersItself` (S2; And, Or × three terminals on the worker; control: the terminal fed by an enabled battery powers the consumer; boundary: And with its other condition powered stays off).
- `AnOrNodeFeedingItselfFollowsItsOtherInputAndForgetsIt` (S3; off before enabling, off on the next committed read after disabling).
- `AContactFeedingItsOwnPowerInIsAdmittedAndInert` (S4; open and closed; control: a battery also on `PowerIn` powers the consumer while closed; boundary: the same self-edge on `FirstIn` is S7).
- `ANonmonotoneSelfWireIsRefusedInEditSaveCompileAndGraphConstruction` (S5; installed scene and saved construction equal the originals afterwards).
- `ASelfEdgedNodeSettlesFromUnpoweredInASyntheticGraph` (S2–S4 as law, three declaration orders, three `Resolve` calls per phase: an Or self-fed on `PowerIn` and `FirstIn` alone stays unpowered although powered would also satisfy its equation — the least fixed point; read through `SupplyOutputRead.IsPowered`, `ABS/SupplyValues.cs:61`).
- `AMonotoneSelfWireSurvivesSaveLoadRunAndReset` (S6; the decoded construction still holds the same-part link).
- `InvalidConnectionStillRefusesEveryOtherBadEdge` (S7; eleven cases, each control without the bad edge).
- `ADeclaredInternalSelfEdgeFollowsTheCycleRule` (S8, through `SupplyBundleValidation.Validate`, one admitted and one refused case also through `BundleValidator.Validate`; control: a declared two-node Xor cycle stays `NonmonotoneCycle`).
- Every existing electrical, supply and gate test passes unchanged; the full local round rises by the new cases only, with zero failed and `Skipped: 0`. No browser case: the connect tool cannot make a self-wire.

*Planning evidence carried from the retired contract (`8d1d68aa`, unchanged at `25df3cd5`; re-verified at planning):*
- *Probe* (6 present, 1 present-untested, 1 narrower owned here, 0 missing): monotonicity `SupplyCycles.IsMonotone`/`Monotone` (`ABS/SupplyCycles.cs:13`, `:24`; `ABS/ElectricalContractValues.cs:25`), tested by `ElectricalOrTests.LawMonotonicityAndUndefinedValuesAreClosed`; cycle rule over components (`GPU/SupplyRoute.cs:60`–`:74`), `ElectricalXorTests.ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve`; one-node component cyclic (`ABS/SupplyCycles.cs:67`–`:68`), present-untested, added here; self-edge reaching the rule, narrower (`GPU/SupplyRoute.cs:50`, `:267`; `CuriousContraptions.Bundle/SupplyBundleValidation.cs:162`–`:163`), a validator case with no law, ABI, wire or presentation change; least-fixed-point settle (`GPU/SupplyRoute.cs:343`–`:351`, `_cyclic`), `ElectricalOrTests.OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick`; edit refusal mapping (`GPU/WorkshopSupplyCompiler.cs:41`–`:50`) and save-load inner reason (`GPU/WorkshopSavePrevalidation.cs:154`–`:156`, reached from `WorkshopSaveCodec.Decode` `:48`–`:50`), the Xor test's `saveRefusal` assertions; worker admission from the decoded scene (`GPU/PhysicsDeclarations.cs:223`; edge record `GPU/WorkshopWireFlatBuffers.Compiled.cs:695`–`:704`), `ElectricalOrTests.PureOrLoopIsAdmittedBySaveAndConstruct` and the settle test, both through the worker (`WorkshopIntegrationHarness.cs:239`–`:242`).
- *Read and unchanged:* `GPU/WorkshopSupplyCompiler.cs`, `GPU/WorkshopConnections.cs:134` (a same-part electrical link is permitted), `WorkshopSaveCodec.cs`, `GPU/PhysicsGpuAbi.Airflow.cs:43` and `SupplyTopology.UpstreamSources` (`GPU/SupplyRoute.cs:77`, a visited-set walk that tolerates cycles), `GPU/WorkshopMessages.cs:153` (read admission recomputes the declared law, consistent for a self-fed node), `ui/WorkshopConnections.cs:95`, `:106`, `:181`, `SIM/WorkshopGpuDevice.cs:326` (second caller of `Resolve`), `ABS/SupplyContractValues.cs:66`–`:69` (the terminal rule that still refuses source, consumer and contact-condition self-edges), `schemas/`, generated wire code, TypeScript, every element package, `GPU/ActivationNetwork.cs:119`.
- *Law-test cases* (`ASelfEdgedNodeSettlesFromUnpoweredInASyntheticGraph`, synthetic `ElectricalSourceDeclaration` rows and a `PhysicsElectricalRead` with charge): (a) Or self-fed on `PowerIn` and `FirstIn` alone, unpowered on every call; (b) source → Or (self on `FirstIn`, second source on `SecondIn` enabled then disabled) → consumer, unpowered on the first `Resolve` after the disable; (c) source → contact (self on `PowerIn`) → consumer, closed then open; (d) source → And (self on `FirstIn`, second source on `SecondIn`) → consumer, never powered; (b) enabled and (c) closed power the consumer.
- *Affected run:* `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter` over `SupplySelfEdgeTests`, `BothGateTests`, `ElectricalOrTests`, `ElectricalXorTests`, `ElectricalNorTests`, `ElectricalNandTests`, `ComposableSupplyRoutingTests`, `PackageLocalPortsTests`, `ElectricalSupplyTests`, `SwitchElectricalTests`, `LatchSupplyFixtureTests`, `BundleValidatorTests`; plus the registration check (a test file is added), the maintainability gate and the full round.
- *Rules:* E1, E2, E4, E5, E7, E8, E9, D4 and D5 apply; D6 holds because the law is unchanged; E3, E6, D1–D3 and D7 are unaffected.

*Not in this slice:* the row's oscillator clauses (a cycle through an oscillator is admitted; an oscillator's enable may name a public `PowerIn` or a supply node of its own package) are planned as their own slice of this story, with the oscillator supply node row (coordinator decision 16).

*Deletes:* the blanket self-edge refusal; the unbounded committed count; the two-state `SignalContactState`; the single fixed endpoint slop as the release rule; the refusal of a rearming elapsed timer; the refusal of a zero timer duration.

*Depends on:* the engine programme (ENGINE-CAPABILITY-INSTANCES renames the node kinds and makes the boolean node N-input). *Unblocks:* CAT-012, CAT-013, CAT-020, CAT-021, CAT-024, CAT-025, CAT-026, CAT-027, CAT-032, CAT-038, CAT-040, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, CAT-056, CAT-059, CAT-060, CAT-063, CAT-068, CAT-072, EL-001, EL-018, EL-025, EL-030, EL-031, EL-032, EL-043, EL-044, EL-049, EL-075, EL-081, EL-086, EL-094, EL-129, EL-130, EL-133, EL-134, EL-135, EL-136, EL-137, EL-138, EL-139, EL-140, EL-141, EL-142, EL-146, EL-153, EL-155, EL-181, EL-182, EL-183, EL-198, EL-221, EL-211, EL-236, RAD-08, RAD-09, RAD-12, RAD-14, RAD-15, RAD-20, RAD-21, TH-04, TH-21, TH-22, TH-30, TH-36, TH-37.

### Story ENGINE-COMPLIANT-FRICTION: Friction on a compliant surface
*Status: Backlog. Sprint key: `engine-compliant-friction`. Order 18.*

*Outcome:* a declared compliant surface applies tangential friction, and rolling resistance for spheres, through the general friction law.

*Present today:* the compliant surface applies normal force only (`_bmad-output/implementation-artifacts/deferred-work.md:205`) at four fixed samples (`SIM/WorkshopPhysicsEngine.cs:1555-1556`). Sampling candidates and list-valued surfaces are `ENGINE-SPATIAL-QUERIES`' and `ENGINE-CAPABILITY-INSTANCES`'.

*Contract:* per bearing sample, the general friction law with the pair's combined coefficient, energy-removing only; rolling resistance for spheres as on any contact. **Admission, deflection read and sag (coordinator decision 29).** A compliant surface is admitted by the law's stability condition, not by product ranges; each sample's committed deflection is a read; a height-field presentation binding draws bounded sag from that read.

*Acceptance:* a load sliding along a surface comes to rest and a frictionless declaration does not; no tick gains energy; a resting load stays put on a tilted surface within its friction angle; a declaration outside the stability condition is refused with a typed reason and one just inside it is admitted; each sample's deflection read equals its committed deflection, and the drawn sag never exceeds it.

*Deletes:* the normal-only surface law; product-range admission of compliant surfaces.

*Depends on:* ENGINE-SPATIAL-QUERIES, ENGINE-CAPABILITY-INSTANCES. *Unblocks:* CAT-065, EL-215.

### Story ENGINE-PARAMETER-BOUND-FRAMES: Values, frames and parts bound to parameters
*Status: Backlog. Sprint key: `engine-parameter-bound-frames`. Order 19. Owns choice-bound values and parameter-bound sockets, limits and parts (coordinator decision 16); `ENGINE-PARAMETER-SELECTED-PARTS` and `ENGINE-CHOICE-BOUND-VALUES` are rows here. Lands before ENGINE-LIQUID.*

*Outcome:* a package can scale and configure its geometry, joints, ports and values from its declared parameters, resolved at compile so table counts stay fixed for the Run, and can bind a declared value to a committed observation evaluated each tick (counts still fixed at compile).

*Present today:* collider binding exists for the base body only, by multiplier and offset of a Real (`CuriousContraptions.Bundle/BundleGeometry.cs:28-48`; `ABS/SceneGeometry.cs:24-27`); joint frames, rims and ports are literals; colliders are baked unconditionally; `PresentationBinding.Selection` and `Visible` exist but every binding needs a committed driver (`ABS/ElementPresentation.cs:15`, `:112`); a collider binding refuses a Choice.

*Contract:*
- A declared Real parameter may drive a hinge or slider base frame, a collider component of any body, a mechanical rim centre, a socket position, a revolute limit, mass and mass-frame centre, principal-moment operands, a store or inventory value, an accumulator's initial value and a field magnitude; a port position may follow more than one parameter.
- A whole-number parameter may set a port rim, a hinge body's collider radius, its mass and a drawn repeat count.
- **Choice-bound values.** Any binding site that accepts a Real may instead name a Choice with one declared f32 value per option (collider half-extent, offset or radius; material component; aperture value; emitter band row; receiver channel weights; presentation parameter including albedo channels); save and wire carry only the option index; a value-count mismatch, a non-finite value or a value outside the site's range is refused at bundle validation.
- **Parameter-selected parts.** An art node and its paired collider may be present only when a declared whole-number or Choice parameter meets a declared comparison; a `Visible` binding by parameter is admitted only on a node the package classifies decorative.
- Bundle admission refuses a declared parameter that binds nothing.
- **Committed-state binding (coordinator decision 28).** A declared value (for example a declared lift's magnitude) may be bound to a committed scalar observation through a declared function from a closed set, including the non-linear A × (1 − T_ref ÷ T) read from a committed store temperature, clamped to a declared range inside the bound value's own domain; it is evaluated at each tick from the previous tick's committed value, so it acts from the next tick; counts and table shapes stay fixed at compile, the binding reads committed state only and writes nothing back, and Reset restores the value the authored state gives. `ENGINE-THERMAL`'s declared lift binding is its first consumer (the consuming identity is in that story's Unblocks).

*Acceptance:* two differently sized fixtures compile with joints, rims, ports and colliders at their evaluated positions; out-of-range values refuse atomically; for two values of a selecting parameter the compiled collider count and drawn nodes differ as declared and a body passes where a deselected collider would stand; a Choice-bound collider follows its option; a value bound to a committed scalar follows it from the next tick, a non-linear binding stops at its clamp, and the value returns to its authored value at Reset; Reset, save and load keep the parameters.

*Deletes:* literal frames and ports where a bound form replaces them; the base-body-only collider binding; the Real-only binding rule.

*Depends on:* the engine programme. *Unblocks:* CAT-009, CAT-019, CAT-020, CAT-061, EL-001, EL-029, EL-044, EL-045, EL-046, EL-047, EL-049, EL-050, EL-051, EL-052, EL-076, EL-080, EL-112, EL-113, EL-114, EL-115, EL-162, EL-179, EL-180, EL-227, EL-232, GAP-07, RAD-02, RAD-07, RAD-08, RAD-09, RAD-13, RAD-14, TH-01, TH-02, TH-25, TH-26, TH-27.

### Story ENGINE-PROFILE-COLLIDER: Convex extruded profile on a moving body
*Status: Backlog. Sprint key: `engine-profile-collider`. Order 20. Performance story (contacts per mechanism). Before its first consumer only if that consumer's fixture built from delivered colliders fails.*

*Outcome:* a convex profile or sector extruded along one axis is an admitted collider kind on hinge- or slider-carried bodies, with the same contact law as the other primitives.

*Present today:* sphere, box, plane, cylinder, tube, frustum and torus (`SIM/WorkshopCollisionKernels.cs:1999-2028`; `GPU/HollowContactQuery.cs:50-52`); hull and capsule proxies are refused at bundle time (`tools/BundleElements/SceneGeometryExtractor.cs:103-106`).

*Contract:* a declared convex polygon and extrusion length, compiled to the flat collider tables, with support-function narrowphase against every admitted kind and segment-cast crossings.

*Acceptance:* a follower riding the profile keeps contact through the profile's dwell within the game-grade envelope; the same mechanism built from delivered collider kinds gives the same follower travel within that envelope. *Performance:* contacts and narrowphase evaluations per substep on the consumer's fixture before and after.

*Deletes:* the multi-cylinder approximation of a profile in the consuming package, replaced forward in the same change.

*Depends on:* ENGINE-SPATIAL-QUERIES, ENGINE-SOA-SIMD. *Unblocks:* EL-158.

### Story ENGINE-RUN-LENGTH: Run length declared per level
*Status: Backlog. Sprint key: `engine-run-length`. Order 21. Owner decisions 13 and 17. Lands before the first row longer than 30 s, so before ENGINE-GAS-NETWORK.*

*Outcome:* a level declares its Run length in game seconds, up to 300 s; 30 s is the default.

*Present today:* one fixed limit (`ABS/CadenceValues.cs:77`).

*Contract:* the run limit, the physical ordinal limit and every admission check that compares a duration with the Run read the level's declared value; a steady inventory source may size itself for the 300 s maximum.

*Acceptance:* two levels with different declared lengths each end at their own; a goal window ending after, or a hold not shorter than, the declared length is refused at admission; an out-of-range or non-finite length is refused; Reset, save and load keep it.

*Deletes:* the fixed run limit as a constant read directly by those sites.

*Depends on:* the engine programme. *Unblocks:* CAT-029, CAT-069, EL-001, EL-004, EL-015, EL-017, EL-019, EL-024, EL-027, EL-029, EL-030, EL-031, EL-032, EL-034, EL-035, EL-037, EL-039, EL-075, EL-090, EL-110, EL-121, EL-123, EL-229, EL-236, EL-240, RAD-01, RAD-13, RAD-20, TH-03, TH-21, TH-24, TH-32, TH-37.

### Story ENGINE-CONNECTION-MATERIALS: Declared connection materials and solid strands
*Status: Backlog. Sprint key: `engine-connection-materials`. Order 22. Owns the solid strand (owner decision 1; coordinator decision 16) and the material record every connection domain uses. Lands before ENGINE-GAS-NETWORK (hoses), ENGINE-MECHANICAL-LINKS and the cutting clause of ENGINE-TENSILE-EXTENSIONS.*

*Outcome:* a connection carries a player-chosen declared material; thick strands (ropes, cables, hoses, ducts, visible belt and chain loops) collide as ordinary bodies; thin wires, the delivered rigid mechanical connection's drawing and the thin string stay non-solid drawings (owner decisions 1 and 2; coordinator decision 25). The thin string's exception is data, not a rule naming a part: a declared **thin-string** material is a tensile strand that declares no strand collider and is drawn as a thin string, and any connection may declare it; every rope, cable, hose and duct material is solid, and no other strand inherits the exception.

*Present today:* a Rope-domain connection carries only a measured length (`GPU/WorkshopConnections.cs:166`) and one span law at the solver's fixed stiffness; strands are drawn with no collider. Admission reads domain, direction and port role (`GPU/WorkshopConnections.cs:112-114`, `:161-168`).

*Contract:*
- **Material record.** A non-placeable bundle entry kind (no body, scene, palette entry or ports), admitted by the bundle validator, excluded from the palette and per-package placement suites, present in the generated bundle: a typed id, the domain it serves, its law kind and values with ranges and defaults (span stiffness and damping, strand diameter, segment rule, cutting resistance; a mechanical law kind and its values for `ENGINE-MECHANICAL-LINKS`; line volume and throat for fluid lines), the engagement class it requires and its strand art. Chosen in the connection UI with a declared default; carried on the connection record, wire, save and read; one material per route; an unknown id refused. The engine knows no material by name.
- **Solid strand.** A strand's collider is a property of its material (declared diameter, segment rule), compiled from declarations with cost bounded by them, candidates from the query interface.
- **Straight static strand.** A material may be a straight static strand (one collider per run); a connection of it is refused, with a typed reason, where its straight run would cross any collider other than its two end parts', checked at every edit that makes or changes it or moves a part into it, and at Run compile.
- The delivered rope is converted to the first declared material in this story, with no default fill and no migration; its missing collider (a recorded visible-solid defect) is repaired here, re-running the delivered tests of every package that ties a rope and the duplicate-closure checks that rely on the rope.

*Acceptance:* two declared materials with different stiffness each settle a hanging load by their own values; a route joining two materials, a material on the wrong domain and an unknown id in a save are refused before any state changes; Reset, save and load restore each material; a ball dropped onto a taut strand is stopped or deflected and never passes through, and one dropped beside it falls past; a taut strand run against a wheel collider, a beam collider, a frame collider and a plate collider (declared test fixtures) rests on each collider without passing through it while its span still carries the load; a straight static strand that would cross a collider is refused and a clear run is admitted; a ball dropped onto a taut thin-string connection passes through it, the string still holds its load in tension, and the same ball is stopped by a rope in the same place.

*Deletes:* the material-free rope connection; the drawn-without-collider rope strand.

*Depends on:* ENGINE-SPATIAL-QUERIES, ENGINE-CAPABILITY-INSTANCES (rope-port admission). *Unblocks:* CAT-003 (tether re-check), CAT-058, EL-038, EL-048, EL-063, EL-066, EL-067, EL-068, EL-085, EL-090, EL-175, EL-200, EL-201, EL-204 (closure re-check), EL-205, EL-224, EL-225, EL-228, TH-16, TH-18, TH-19, TH-20.

### Story ENGINE-GOALS: Declared goal kinds and ordered objectives
*Status: Backlog. Sprint keys `engine-goal-evaluation`, `engine-goal-supplied-occurrence`, `engine-goal-progress-observation`, `engine-goal-ordered-events` and `engine-goal-count-window-hold`. Order 23. Five engine slices (owner decision 12); the drafted contract [spec-engine-goal-evaluation.md](../implementation-artifacts/spec-engine-goal-evaluation.md) holds the first and lists all five. ENGINE-GOALS stays open until all five publish. All five wait for the engine programme (owner decision 22).*

*Outcome:* a level declares a list of goal conditions over committed observations; one stateless evaluator decides Solved, Waiting or failed, with progress on the observation port. There is one condition list and no goal-kind enum per product; a turned condition is a travel observer a package declares.

*Present today:* one goal per level, of kind Captured, ActivatedAfter or IntegrityBroken (`WorkshopGoalKind`; `GPU/WorkshopPuzzle.cs:9-31`; `GPU/WorkshopGoalEvaluator.cs:41-54`); the planned `goals` array is indexed but never constructed; the observation projection carries no phase (`GPU/WorkshopObservationProjection.cs:84-93`); `Supplied` is a level with no occurrence time (`ABS/PhysicsPresentationRead.cs:27`). Goals read committed state, not presentation (`GPU/WorkshopGoalEvaluator.cs:7`).

#### Slice engine-goal-evaluation
Several conditions per level (all-of) in the save schema and level content; conditions over committed latches and travel observers; the Solved result. *Acceptance:* a two-condition level solves only when both hold; removing one cause leaves it Waiting.

#### Slice engine-goal-supplied-occurrence
An electrical supplied occurrence with its time on the worker read and wire, so a powered-after condition can be evaluated. *Acceptance:* a consumer supplied after a named occurrence satisfies powered-after; supplied before it does not.

#### Slice engine-goal-progress-observation
Per-condition progress on the observation port and HUD: a group's count, a window's bounds, hold progress and the failed state. *Acceptance:* each progress value is read through the observation port as the level advances.

#### Slice engine-goal-ordered-events
Ordered conditions with a minimum gap (equal occurrence times satisfy a zero minimum); forbidden events fail the attempt until Reset; the any-of form as a count group "at least k of n" whose occurrence is the k-th earliest, one subject counted once per group. *Acceptance:* events in order solve; the same events out of order do not; a forbidden event fails the attempt until Reset; one item sensed repeatedly counts once.

#### Slice engine-goal-count-window-hold
A half-open game-time window from Run start on a condition or group, refused when it ends after the level's declared Run length; a condition may require a named committed scalar of its subject to lie in an authored closed band at its occurrence (a failed qualifier leaves it unmet and reports the failed state); a held level is a committed state with the occurrence at which it last became true (residence phase from `ENGINE-SENSOR-VOLUMES`), and the goal holds when the read's time is the declared hold past the latest became-true occurrence (coordinator decision 16); the evaluator keeps no state and schedules one time wake; a hold not shorter than the Run length is refused. *Acceptance:* deliveries just below and at a count threshold: only the second solves; a burst before the window never solves; delivery alone does not solve a held goal; a level that drops and returns between wakes restarts the hold; a subject that leaves during the hold leaves the goal Waiting; a capture with its scalar outside the band does not solve.

*Deletes:* the single-goal level shape and its three-kind enum as the only form.

*Depends on:* the engine programme (ENGINE-SENSOR-VOLUMES for residence phases), S5 (wire accessors), ENGINE-RUN-LENGTH (window and hold admission). *Unblocks:* CAT-004, CAT-042, CAT-062, CAT-063, EL-069 (capture by goal), EL-099, EL-120, EL-121, EL-122, EL-123, EL-192, RAD-10, and every planned level with more than one goal or a goal outside the three present kinds.

### Story ENGINE-OPTICAL: Beam emission, transport, surface interaction and reception
*Status: Backlog. Sprint key: `engine-optical-propagation`. Order 24. Performance story (R4 with emitters). Precedes every Epic 13 story. The optical conventions are owned by [EL-176 section 10](../../docs/planning/elements/EL-176-red-laser-emitter.md#optical-conventions).*

*Outcome:* one optical solve per tick transports declared light in watts per channel from paid emitters through declared surface responses to receivers whose committed readings drive signal nodes.

*Present today:* nothing optical; the airflow line-of-sight test (replaced by the segment cast in `ENGINE-SPATIAL-QUERIES`); `ElementCategory` has no optics member.

*Contract:*
- **Emitters.** Frame (static or dynamic, including a companion body), origin, axis, half-angle up to 180° (a cone has uniform intensity), range (optionally parameter-bound), radiant power per channel, electrical demand, internal occluders and an optional enable naming a signal level; paid as a supply consumer target or by any finite conversion (a declared store, a fuel burn); it emits radiant power × the energy granted over the tick; granted energy not emitted is ledgered to a declared collider of the emitter. Its committed lit state is on the read.
- **Solve.** One solve per tick of duration dt, at its slot in the [combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order) (optical convention 4): after the last substep and before the thermal step, taking ordered crossings from the segment cast against every participating collider at that tick's final pose, with signal levels as resolved at the start of the tick; readings commit with the tick's poses and act from the next tick; no logic is evaluated inside the solve. A collimated segment is a zero-width ray; diverging segments follow one distance law and are terminal.
- **One near-field rule and one own-body rule, for light and radiation (coordinator decision 26; optical conventions 1 and 5).** Beyond 1 m irradiance is I × facing ÷ d²; inside 1 m it is I ÷ 1 m² for a source on the front side of the aperture (facing 1) and 0 behind it, so a reading is flat inside 1 m. A shadow cast from a source to a receiver's sample ends at the receiver's own aperture face (for a Uniform receiver, at the sample point); every crossing before that end acts by its declared response, the receiver's own colliders included, and nothing beyond it is read. Neither rule has a second form by channel class.
- **Responses** per collider face or disc aperture, applied once per crossing, the default declared per channel (Absorb, τ = 0, for light channels; τ = 1 for radiation channels): reflect, transmit by channel with optional spread, split (a diverging segment is multiplied by τ, the ρ share absorbed and ledgered), and relay from an inlet to one or more outlets (channel mask, direction, half-angle, internal path, optional enable; an outlet may be declared relative to the inlet hit point and incoming direction, or on another placed part through an Optical-domain connection). An outlet is a declared node with a typed id committing the per-channel sum leaving it; a segment leaving an outlet ignores the collider carrying it at its origin; a Relay inlet may be a receiver aperture taking its conserved share of diverging segments. A declaration whose outputs can exceed its input in any channel is refused at bundle time. A port's maximum connection count and a connection kind's maximum length are declared and checked at edit and Run.
- **Receivers.** Static or dynamic frame, weighted sample points, a sensed channel set, a strength rule (minimum over a required set, or a weighted sum) and purity, committing a continuous reading and a strength that a threshold node reads; coupling is observation → signal node → signal-state contact only (one receiver law for all light, owner decision 9).
- **Absorbed energy is booked once (coordinator decision 26; optical conventions 7 and 8).** Power that ends on an absorbing face, a receiver's collimated power and its conserved diverging shares included, is booked once, on that collider's absorbed record; a receiver's reading observes that power and books or credits nothing again. The record becomes heat only as `ENGINE-THERMAL` moves it (thermal T3), and supply only by the credit below.
- **Credit to supply.** At the end of the solve, a declared fraction (at most 1) of the energy booked on a receiver's absorbing collider in the tick moves from that record into one declared finite store, before the thermal step reads what remains; a credit that does not fit the store stays on the record. The store row is `ENGINE-THERMAL`'s (coordinator decision 26); there is no next-tick allowance form (coordinator decisions 21 and 25).
- **Limits.** Per emitter, a declared segment limit L and interaction limit; at the limit segments are kept breadth-first by depth, then emitter and surface order.
- **Presentation and authoring.** Drivers for committed emission, receiver reading and outlet power; the beam and cone read model; a declared display volume in which committed segments of declared channels are drawn; a Build preview running the same solve without committing, stating its collider source; the optical palette category and domain flag.
- Every delivered package that declares a response other than Absorb is listed and tested here, so no element slice edits a delivered package for light.

*Acceptance:*
- An emitter, a reflecting surface and a receiver: with a clear path the receiver reports the emitted channels; an occluding solid or a surface turned off the path gives none.
- Emitted energy never exceeds energy paid; for each segment, power received by all receivers together never exceeds its power (shares scaled by power ÷ sum when the sum exceeds it); no response creates a channel the incoming segment lacks.
- A channel-selective surface passes only its channels; a split conserves channel power; a relay never exceeds its inlet.
- A reading acts from the next tick; nothing reads a receiver directly.
- A segment-limit qualification scene commits exactly the declared number of segments.
- A clear shell declaring Transmit passes a beam to a detector, an absorbing collar does not, and a body inside the shell blocks it.
- Energy is booked once: a laser's paid energy equals its emitted power plus its housing loss over the tick, and across the absorber's record, the store credit taken from it and the share a thermal face moves from it, nothing exceeds the energy absorbed; a reading alone moves nothing.
- A receiver sample inside 1 m of a source reads the same at every distance inside 1 m; a receiver whose own collider stands in front of its aperture is shadowed by it.
- Tolerances are the one optical set (coordinator decision 21): power ±2 %, stall guard 1 s, direction 0.5°, end points ±0.02 m, each excluding the failure the conventions name.
- *Performance:* work per tick is at most N × L segment casts (N emitters, L segment limit) plus one shadow cast per sample point of each candidate receiver that a diverging segment's cone-and-range region query returns (optical convention 11); no other receiver is tested (counted on a scene with receivers outside every cone), none is a scan; R4 with emitters before and after.

*Deletes:* the closed palette category set without optics (`AirflowLineClear` goes with `ENGINE-SPATIAL-QUERIES`).

*Depends on:* ENGINE-SPATIAL-QUERIES, ENGINE-SIGNAL-NODES, ENGINE-SUPPLY-DEMAND, ENGINE-THERMAL (credited store row). *Unblocks:* CAT-006, CAT-007, CAT-008, CAT-011, CAT-012, CAT-016, CAT-021, CAT-029, CAT-030, CAT-031, CAT-032, CAT-036, CAT-038, CAT-040, CAT-041, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, CAT-048, CAT-049, CAT-050, CAT-051, CAT-055, CAT-056, CAT-059, CAT-068, CAT-071, CAT-072, EL-075, EL-082, EL-083, EL-087, EL-088, EL-105, EL-138 to EL-155, EL-173 to EL-178, EL-210 to EL-214, TH-01, TH-02, TH-06, TH-07, TH-32, TH-37; the light-occlusion rows of CAT-066 and EL-191; and, through the solve `ENGINE-RADIATION` shares, EL-126, EL-129, RAD-01 to RAD-03, RAD-06 to RAD-10, RAD-12 to RAD-17, RAD-21 and RAD-22.

### Story ENGINE-RADIATION: Emission, attenuation, decay and exposure
*Status: Backlog. Sprint key: `engine-radiation`. Order 25. After ENGINE-SPATIAL-QUERIES, ENGINE-SIGNAL-NODES and ENGINE-OPTICAL; the liquid crossing after ENGINE-LIQUID; no dependency on ENGINE-THERMAL (decay heat is a thermal inventory source). The radiation conventions are owned by [RAD-08 section 10](../../docs/planning/elements/RAD-08-radiation-rate-meter.md#radiation-conventions).*

*Outcome:* radiation is seven more channels on the one optical solve, attenuated exponentially by declared material rows, paid by supply or finite decaying inventories, and accumulated as dose at declared receivers.

*Present today:* nothing.

*Contract:*
- Channels as a closed set (PhotonSoft, PhotonMedium, PhotonHard, Alpha, BetaMinus, NeutronFast, NeutronSlow) on the emitters, segments and receivers of the optical solve, in its tick slot.
- Every collider transmits radiation channels with transmittance 1 unless declared; a declared attenuation row of per-channel coefficients on any collider and one declared default row for the scene (the photon coefficients ÷ 10, coordinator decision 21), applied as exp(−coefficient × chord) to the ordered crossings of the segment cast at the tick's final poses; overlapping chords of one body counted once with the largest coefficient. The own-body rule is optical convention 5's one rule for light and radiation (coordinator decision 26): a receiver's shadow cast ends at its aperture face, or at the sample point for a Uniform receiver, and every crossing before that end attenuates it, the receiver's own colliders included. A receiver that must read the same whatever way it faces declares one aperture sample per face and reads their mean (coordinator decision 30); the own-body rule has no second form.
- One optional conversion per row (from, to, fraction ≤ 1): out_a = in_a·e^(−μ_a·L), out_b = in_b·e^(−μ_b·L) + m·in_a·(1 − e^(−μ_a·L)). A liquid store's committed filled region is crossed as a volume with the liquid's row. Radiation channels may appear in a Relay's outlet masks; unmasked channels, and every channel of a disabled relay, cross the inlet's collider by the attenuation law.
- Emitters paid by a supply consumer or a declared finite inventory that decays with a half-life (emitting remaining × (1 − 2^(−dt ÷ T½)) each tick) or is steady (constant power, inventory sized for the 300 s maximum Run), restored on Reset.
- Receiver facing as a closed set (Lambert, or Uniform with an effective cross-section and sample points); a collimated segment is credited where it crosses the aperture and continues; readings flat inside 1 m (optical convention 1's one near-field rule); one dose scale on one standard aperture area; a weighted-sum strength. A reading is the power crossing the aperture, a flux and not energy absorbed: it credits nothing, and several apertures on one collimated path each read what crosses them (radiation convention 7; coordinator decision 26). An accumulator integrates committed strength into joules, + strength × dt each tick, with a declared capacity and authored initial value, never reduced within a Run. Power removed along a crossing is booked once, on the absorbing collider's absorbed record (radiation convention 10), from which `ENGINE-THERMAL` moves heat and the optical store credit moves supply; no buildup, scatter, reflection or refraction.
- Presentation drivers for strength, accumulator value or fraction and inventory fraction; the palette category and domain flag.

*Acceptance:*
- A collider with a declared row on the path lowers the reading by exp(−coefficient × chord); a second lowers it further; one beside the path changes nothing; a gap between colliders adds nothing; a collider with no row attenuates by the default row; an unknown or missing channel in a row is refused.
- A decaying inventory's power halves each half-life of game time and its total never exceeds its inventory; a steady inventory emits constant power, is debited each tick and is restored on Reset.
- Two separate exposures credit an accumulator the same total as one of the same length, at 60, 120 and 240 Hz cadences alike; the total survives loss of source and supply; Reset restores the initial value.
- Power leaving a crossing never exceeds power entering it; diverging shares are scaled so received ≤ emitted; two thin receivers on one collimated path both read the flux crossing them, and the energy booked as absorbed along the path never exceeds the power removed from it.
- A Uniform receiver reads a source behind its own body through the chord of that body; a receiver with one sample per face reads the same mean whichever way it faces.
- *Performance:* at most C × L segment casts for C collimated sources and L the optical segment limit, plus, for each diverging source, one shadow cast per sample point of each candidate receiver its cone query returns through the query interface; no other receiver is tested. A cast returns every crossing on its segment: there is no crossing cap and no scene-capacity refusal (D5; radiation convention 12). R4 with sources before and after.

*Deletes:* nothing in the engine; the earlier "after ENGINE-THERMAL (decay heat)" ordering.

*Depends on:* ENGINE-OPTICAL; ENGINE-LIQUID for the liquid crossing; ENGINE-PARAMETER-BOUND-FRAMES; ENGINE-RUN-LENGTH. *Unblocks:* RAD-01 to RAD-04, RAD-06 to RAD-10, RAD-12 to RAD-18, RAD-20 to RAD-22 (the 19 named RAD identities; RAD-05, RAD-11 and RAD-19 are umbrellas forwarding to EL-126 to EL-132), EL-126 to EL-132.

### Story ENGINE-FIELD-FORCE: The one uniform-field law and declared force regions
*Status: Backlog. Sprint key: `engine-field-force`. Order 26. Owns gravity as one uniform-field law (coordinator decision 16) and its scene-region slice (the name `engine-scene-environment` is retired). Performance story (R4 with regions).*

*Outcome:* every non-contact field is one declared law: acceleration fields (gravity, scene-wide or in a region) and force fields with declared selectors and payment.

*Present today:* gravity is a fixed compiled value; the atmosphere is a two-value enum (`ABS/AtmosphereBoundary.cs:6-15`; `SIM/WorkshopPhysicsCompiler.Gas.cs:21`, `:37-38`); an ambient load acts only on the body that declares it (`ABS/GasCouplingValues.cs:19`); a supply consumer that pays only for delivered work exists for airflow only (`ABS/SupplyContractValues.cs:50`); no environment field in level content or on the observation port.

*Contract:*
- A field source declares a region (unbounded: the scene; or a bounded box or cylinder on a frame body), a selector (a responding property declared on a body by one rule, or a target selector), a magnitude whose quantity is a closed enum {Force (N), Acceleration (m/s²)}, a direction, optional falloff from a closed enum, and an optional occupancy capacity (acting on no more responding bodies than that until one leaves; occupancy is a committed read).
- **Direction as data (coordinator decision 29).** A field's direction is declared from a closed enum {Constant vector, Toward a declared point} with its vector or its point in the source's frame; both forms are the one uniform-field law, evaluated per body at its centre of mass, with no branch by element.
- Membership by centre of mass through the region query, waking sleepers; the field at a body is the sum of the fields whose region contains its centre of mass; the story sets the bound on a summed field.
- An Acceleration field is the one gravity law: a body's effective gravity is its declared gravity plus the Acceleration fields it is inside; every declared buoyant lift, including a heated envelope's lift bound to its temperature (`ENGINE-THERMAL`, coordinator decision 28), scales by |effective gravity| ÷ |declared gravity| and is directed against it. Buoyancy in air is declared ambient lift only; there is no displaced-air law.
- **One acceleration bound.** A body's compiled acceleration has one bound, the combined |gravity + ambient load| ≤ 16 m/s² already checked at `engine/gpu/GasSceneValidation.cs:78`; the separate per-load check on the compiled load (|load| ≤ 16 m/s², `CuriousContraptions.Abstractions/GasCouplingValues.cs:65`) is deleted, forward-only, so a field-scaled lift has no second bound. A declared lift itself stays inside the 16 m/s² domain of a declared acceleration (`CuriousContraptions.Abstractions/WorldAccelerationDeclaration.cs:25`).
- Payment kind from a closed enum {None, StandingDraw, PaidWork within a declared power bound, scaled proportionally when several bodies share it}; StandingDraw is a standing-demand consumer of `ENGINE-SUPPLY-DEMAND`; the reaction is applied to a dynamic frame body; one work read per source.
- **Scene-region slice.** The scene field (one validated f32 from 1.0 to 16.0 m/s² along −Y, folded into each body's acceleration at compile, no per-tick cost), the environment record (gravity, and the atmosphere converted from an enum to a declared row of boundary pressure and relative factor), level-content fields, observation port and plaque.

*Acceptance:*
- The same drop under two declared scene values falls ½gt² within 0.05 m (the smallest landing clearance the gravity rows use); a declared buoyant body rises at the same rate in a 16.0 m/s² scene and in a Standard scene inside a region that brings the sum to 16.0; a heated envelope's bound lift is scaled once by the field, not twice; a lift scaled above 16 m/s² by a strong field is admitted while the body's combined |gravity + load| stays within 16 m/s², and refused once that combination exceeds it.
- A responding and a non-responding body at the same distance: only the responding one accelerates; a point-form field accelerates responding bodies on opposite sides of its point toward it, and a constant-vector field accelerates both the same way; switching the source off removes the force from the tick the command commits; the reaction on a dynamic source is equal and opposite, so an isolated pair gains no momentum.
- A body crossing a supplied PaidWork region gains at most force × path length and the supply is debited at least that; unsupplied it gains none; two bodies inside share one bound; a StandingDraw source empties a finite store at its rate and stops applying force when it is empty.
- Out-of-range, non-finite or missing values and a save in the enum layout are refused atomically; difficulty never changes the scene field; Reset, save and load keep it.
- *Performance:* region work bounded by the bodies inside; R4 before and after.

*Deletes:* the fixed gravity constant as a separate rule; the atmosphere enum; the per-body-only ambient load as the only form of a field; the per-load acceleration check at `GasCouplingValues.cs:65`.

*Depends on:* ENGINE-SPATIAL-QUERIES, ENGINE-ISLANDS-SLEEP, ENGINE-SUPPLY-DEMAND, ENGINE-PARAMETER-BOUND-FRAMES. *Unblocks:* EL-060, EL-089, EL-103, EL-124, EL-125, EL-227, TH-25, and the atmosphere and gravity rows of CAT-001, CAT-014, EL-187 and EL-188.

### Story ENGINE-FRACTURE: Load-limited connections and irreversible break
*Status: Backlog. Sprint key: `engine-fracture`. Order 27. After ENGINE-JOINT-KINDS and ENGINE-CONTACT-OBSERVATIONS; limits on player-made connections additionally after ENGINE-ASSEMBLY-JOINTS. Package-declared joints and bodies do not wait for player assembly.*

*Outcome:* a declared load limit breaks a joint, attachment, rope tie or body one way for the rest of the Run, replacing removed colliders with declared fragments in the breaking substep.

*Present today:* an integrity observer on one body opens a gas outlet and leaves the body in place; it qualifies on a per-substep pair impulse strictly above its threshold with approach speed above its minimum (`ABS/ContactIntegrity.cs:16-43`; `ABS/GasCouplingValues.cs:15`); a one-way topology change exists only as a commanded release of a tensile span (`ABS/RuntimeServices.cs:77`).

*Contract:*
- Every load limit declares a measure from one closed set (axial tension; shear; bending moment; resultant; contact normal, the largest normal load one other body exerts, summed over that pair's contact points; contact impulse, the integrity observation of `ENGINE-CONTACT-OBSERVATIONS`) and a hold time of zero or more; the break occurs when the measure has exceeded the limit on every substep for the hold; the settling transient at Run start does not count; a load of another kind or sign never breaks it. A limit may be multiplied by a clamped linear function of committed scalars (a store fraction, a node temperature).
- A limit may sit on a joint or attachment, a rope tie, or a static or dynamic body; a body's break removes one declared collider, a declared set, or all of them.
- Declared fragments are companion content of the package (owner decision 15): body and collider rows fixed in count and compiled at Run, inactive until the break (outside the broadphase, sensors and the committed body set), each within the removed colliders' volume (checked at bundle validation). The breaking substep is re-solved with the fragments active in place of the removed colliders, so the breaking contact acts on them and momentum is conserved; each fragment starts at its declared pose in the parent's frame with the parent's rigid-body velocity at its own centre (rest for a static parent), and takes the parent's material values, payload role and per-body surface state. Fragments of a whole dynamic body sum to its mass; fragments of a removed set sum to the mass that set carried and the remaining body's post-break mass frame is compiled at Run; a static parent's fragments declare their own mass. Fragments break again only if they declare a limit. A companion body may also take part from Run start.
- The break publishes the existing integrity-broken observation; the committed load-to-limit ratio and the committed broken state are presentation drivers.

*Acceptance:* the same connection loaded just below and just above its limit: only the second breaks, stays broken for the Run, and Reset restores it; a load of the other sign never breaks a tension limit; a transient shorter than the hold does not break a held limit; a struck body is replaced by its fragments in the same substep with total momentum conserved within the game-grade envelope and no intact body left for a tick; a declaration placing a fragment outside the removed volume or breaking the mass rules is refused.

*Deletes:* the integrity observer's gas-outlet-only effect as the sole break.

*Depends on:* ENGINE-JOINT-KINDS, ENGINE-CONTACT-OBSERVATIONS. *Unblocks:* EL-073, EL-084, EL-088, EL-159, EL-160, EL-161, EL-170, EL-186, GAP-09, TH-03, TH-16. Cutting tools (EL-066, EL-068) are not consumers; cutting is `ENGINE-TENSILE-EXTENSIONS`'.

### Story ENGINE-IGNITION-BLAST: Ignition, burn and blast impulse
*Status: Backlog. Sprint key: `engine-ignition-blast`. Order 28. The blast half does not wait for ENGINE-THERMAL; the burn half is after it. The combustion conventions are owned by [TH-01 section 11](../../docs/planning/elements/TH-01-candle.md).*

*Outcome:* declared fuel burns from an ignition threshold until fuel or oxidiser runs out, crediting heat and light; a declared charge releases one occluded radial impulse.

*Present today:* nothing.

*Contract:*
- **Burn.** An ignite threshold handed from `ENGINE-THERMAL`; a finite fuel store (capacity from a parameter whose minimum is 0, its fraction a presentation driver) consumed at a declared rate; the burn's payment credited to declared thermal nodes by declared shares, less any radiant power paid to a declared optical emitter; an optional extinction temperature; an authored lit start setting the declared initial temperatures of its nodes; the oxidiser read from the scene atmosphere (absent in Vacuum) or an enclosure's finite gas store; a body's mass and one uniform collider scale bound to the fuel fraction at a tick boundary; a declared outcome for a solid whose fuel is spent (it keeps or loses its collider), restored on Reset.
- **Blast.** A source on a body declares a reference impulse at distance 0 and a falloff from a closed enum (at least Linear) to zero at a maximum radius, applied once to every dynamic body whose centre of mass is inside the radius and joined to the origin by a segment no solid crosses (the source's and target's own colliders excluded), candidates from the query interface; the negative sum is applied to the source body; if Σ J² ÷ 2m over targets and source from rest exceeds the paying store's balance, every impulse scales by one factor; the store then reads zero until Reset. Release by a signal input (a physical-request target kind, typed refusal and no spend when spent) or by consuming an integrity observer's one-way occurrence. It commits at the end of the substep after its contact solve and publishes one event-ring record per release. A blast breaks nothing directly.

*Acceptance:* the same fuel burns in Standard and not in Vacuum; fuel debit equals heat credited plus light emitted within the thermal ledger's tolerance; a charge behind a solid wall and one in the open fire, and only the body with a clear path receives impulse; total momentum is conserved; a spent charge cannot fire again before Reset.

*Deletes:* nothing.

*Depends on:* ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS; burn half ENGINE-THERMAL and ENGINE-OPTICAL (radiant share). *Unblocks:* EL-074, EL-093, EL-106, TH-01, TH-02, TH-03, TH-31, TH-32, TH-33.

### Story ENGINE-GRANULAR: Bounded granular inventory and transport
*Status: Backlog. Sprint key: `engine-granular`. Order 29. After ENGINE-LIQUID, whose store and aperture ledger it reuses. Sand waits for it; no rigid-grain substitute (owner decision 14).*

*Outcome:* granular material is a conserved inventory of declared fractions in stores and piles, passing apertures by grain size against clear width.

*Present today:* nothing.

*Contract:* (a) a store holds declared fractions, each with a grain diameter, conserved separately; a mixed store discharges fractions in proportion to their retained amounts; (b) an aperture declares a clear width, a full-width rate independent of fill height and a passage ratio; a fraction passes only while the clear width is at least the ratio × its grain diameter, otherwise it is retained (a jam); (c) the clear width is reduced by any solid collider across the aperture (candidates from the query interface) and the rate scales with the clear fraction, so a blade, a wall or the pile below gates the flow with no command channel; (d) material leaving an aperture is credited to the store interior beneath it or forms a free pile on the solids below, bounded by a declared repose limit, in the same ledger; (e) a store's and a pile's mass load their supports and are counted by contact-load sensors; (f) a store's contents and a pile present a surface that supports rigid bodies; (g) committed inventory per store and pile by fraction, an amount-reached observation goals can select, and fraction and flow as presentation drivers; (h) an aperture declares an occlusion reach, and only a solid whose top lies within that reach of the outlet plane narrows its clear width (the story's fixture declares a 0.10 m reach); (i) a store admits every declared fraction that enters it, by the grain diameter its source declares; (j) a store's committed surface height is a presentation driver (a `Visual/Fill` node); (k) the story measures its tick cost on its fixed reference scene and sizes its tables from compiled declarations (no scene ceiling).

*Acceptance:* a filled upper store over an open aperture: the lower gains exactly what the upper loses, at a rate that does not change with fill height; one feed passes a wide aperture and is retained by a narrow one; a solid moved across an open aperture stops the flow and moving it away restores it, while the same solid held just beyond the occlusion reach does not narrow it; a store fed two fractions admits both; a mixed feed over a selective aperture delivers one fraction and retains the other, each conserved; a pile presses a contact-load sensor as a rigid body of the same mass would.

*Deletes:* nothing.

*Depends on:* ENGINE-LIQUID, ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS. *Unblocks:* GAP-06, GAP-07.

### Story 14.0: Acoustic Propagation, Occlusion & Reception Engine (ENGINE-ACOUSTIC)
*Status: Backlog. Sprint key: `14-0-acoustic-propagation-occlusion-reception-engine`. Order 30. Before the first acoustic consumer (Story 14.2, then 14.1, 14.3, 14.4; owner decision of 18 October 2026). The acoustic laws (L1 to L7), probe (C1 to C13, N1 to N14) and numbered gaps are owned by the [EL-044 spec](../../docs/planning/elements/EL-044-tone-selective-sound-meter.md); this text implements them. May be delivered as four slices: free field (gaps 1 to 5, 11 to 14, 17), surfaces (7, 8), routes (6) and resonance (9, 10).*

*Outcome:* declared emitters produce acoustic occurrences that travel at a declared speed, fall off with distance, are occluded or attenuated by colliders, and are received by declared receivers as committed band levels.

*Present today:* nothing acoustic; `SimulationDomain.Audio` is an enum member whose ports are refused (`ABS/SimulationDomain.cs:13`); the event-kind set has no acoustic kind and is pinned closed (`ABS/EventDescriptor.cs:7`); the multi-consumer event ring exists.

*Contract:*
- An occurrence has origin, axis, a pattern as a closed enum (Cone with a declared half-angle, or Omnidirectional with no direction), tone band, strength in (0, 1] and kind (Pulse of declared duration, or one Sustained occurrence with start and stop, never repeated pulses).
- It reaches a receiver at distance ÷ declared speed, lasts its declared duration, and is zero beyond its declared range; level follows the declared falloff law (acoustic law L3); a receiver declares a band mask (out-of-band contributes exactly zero); its level is the strongest admissible arrival, never a sum; an instance's own colliders do not occlude it and it does not hear itself.
- A collider may declare an acoustic transmission factor in [0, 1] (default 0, opaque); occlusion candidates from the segment cast.
- Each receiver commits its band level every tick as a committed scalar observation; levels of a tick commit together; thresholds over it are `ENGINE-SIGNAL-NODES`'.
- **Paid emitters.** A signal pulse on a declared input requests one emission; requests of a tick coalesce and are served on the next tick; an unserved request expires. A pulse emitter may be gated by a declared energy store (emission needs the store at capacity and its supply input powered in the tick that serves it, the route's powered state as resolved at that tick's start, and empties it; coordinator decision 32). A sustained emitter may be a continuous-work supply target that requests its declared power × dt each tick and sounds in a tick exactly when it was powered in full in the previous tick's commit (the acoustic phase runs at tick start and reads tick t − 1, like the optical one-tick latency; coordinator decision 34). An unpaid emitter is silent.
- **Contact excitation.** An emitter may be excited by a qualified contact (`ENGINE-CONTACT-OBSERVATIONS`): declared colliders, a declared body target set, an approach speed threshold, strength = clamp(striker mass × approach speed ÷ a declared reference momentum, a declared floor, 1); emitters may share one declared minimum interval; contacts committed in one tick yield one emission per interval group in the next tick's acoustic phase, the strongest, ties to the lower emitter id; a resting or separating contact emits nothing; a package may declare several emitters with fixed tones and their own excitation rows.
- **Observation excitation.** A sustained emitter may follow a threshold node over a committed scalar observation, with strength = observation ÷ a declared full-strength value, capped at one; this story delivers one observation kind, an observation-only airflow probe excited by the committed flow speed of a declared airflow source at declared probe samples (it takes no force, adds no demand and may sit on a static body); an emitter's tone or disabled state may be selected through a declared table by a committed scalar observation.
- **Routes and surfaces.** Acoustic ports join through a typed route connection that delays by length ÷ speed and multiplies by a declared loss; an aperture captures arrivals inside its acceptance cone and an outlet releases routed sound; no stage has gain above one; a routed occurrence traverses each route at most once by lineage; relay depth is bounded; `WorkshopConnectionDomain` gains Audio with a typed per-connection setting through authoring, save and wire. A redirecting surface emits one child occurrence per incident occurrence using the total path from the source. An accumulating receiver integrates in-band level with a declared gain and decays with a declared time constant, capped at one.
- **Presentation.** A typed acoustic event per emission for cues; audio playback is presentation-only; a committed acoustic read model exposes each receiver's level, each emitter's state, each emitter's requests of a tick, served or expired (so coalescing and expiry are observable), and each routed occurrence at an Audio port. Art chosen by a Choice is `ENGINE-PARAMETER-BOUND-FRAMES`'.
- **Tick order.** The acoustic phase runs at the slot the [combined tick order](../../docs/planning/elements/EL-176-red-laser-emitter.md#combined-tick-order) gives it (step 1: at the start of each tick, after the signal network resolves and before the substeps, reading tick t − 1's commits), which this story cites and does not restate; so a contact committed in tick t excites in tick t + 1, distinct from the optical solve after the last substep.
- **Bounds.** Occurrences in flight per emitter are bounded by its declared minimum interval and lifetime; reception work is proportional to occurrences whose wavefront covers a receiver within range, found through the query interface, never emitters × receivers of the whole scene.

*Acceptance:* distance falloff, cone edges, an omnidirectional source, occlusion on and off, a transmission factor, same-tick multiple emissions (strongest wins, never summed), out-of-band arrivals reading zero, a store-gated pulse emitter firing only at capacity with its supply input powered in the serving tick, a sustained supplied emitter sounding in the tick after one in which it was powered in full and silent in the tick after one in which it was not, a contact excitation just below and above its approach threshold, a sustained occurrence starting and stopping with its threshold node, a route delaying by length ÷ speed and never amplifying, and Reset clearing every pending emission and received level exactly; native tests only, browser proof post-catalogue. Tolerances are the acoustic family's named set: level within max(0.02, 5 % of expected), a delay of ±0.05 s asserted only for a duct transit (free-air travel is asserted as causal order), energy within max(0.1 J, 2 %), and a stall guard of 5 s of game time after the cause.

*Deletes:* the refusal of Audio-domain ports as unsupported; the closed event-kind set without acoustics; the Chrome e2e criterion of the earlier text.

*Depends on:* ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-SIGNAL-NODES, ENGINE-SUPPLY-DEMAND, ENGINE-PARAMETER-BOUND-FRAMES. *Unblocks:* CAT-009, CAT-060, CAT-061, CAT-069, EL-044, EL-045, EL-046, EL-047, EL-048, EL-049, EL-050, EL-051, EL-052, EL-179, EL-180, and the sound rows of CAT-066, EL-125 and EL-191.

### Story ENGINE-ASSEMBLY-JOINTS: Player-built rigid and releasable connections
*Status: Backlog. Sprint key: `engine-assembly-joints`. Order 31. Reduced to typed attachment sockets and commanded release (owner decision 3a; coordinator decision 16). The joint kinds, holds, drives, winding, elasticity, couplings and shaft-to-supply conversion it formerly listed are owned by ENGINE-JOINT-KINDS, ENGINE-TENSILE-EXTENSIONS, ENGINE-CONNECTION-MATERIALS, ENGINE-MECHANICAL-LINKS and ENGINE-SUPPLY-DEMAND.*

*Outcome:* the player joins two separately placed parts at typed attachment sockets with a rigid or pinned connection, and a declared signal can release a connection.

*Present today:* `AnchorPointDescriptor` (`ABS/MechanicalContractValues.cs:63`) is declared by one package, validated and bundled, never compiled; its `Name` is a string; there is no player-made joint.

*Contract:*
- Sockets carry a typed id and a role enum (mount, point); a mount is rigid or pin, optionally with a load limit (`ENGINE-FRACTURE`); a connection joins one mount to one point; several mounts may share a point; other pairings are refused with a typed reason. This story builds on `AnchorPointDescriptor` or deletes it; it adds no second socket contract.
- The mount-carrying body and the point-carrying body of a connection do not collide, nor do bodies whose mounts share one point; every other pair collides.
- The connect-time rule for a mount and a point that are apart is fixed once; a made connection adds no impulse at Run start.
- **Commanded release.** A connection (player-made, or an attachment declared inside one package) may be released by a declared signal input of its owning part or by an internal signal node through a package-internal edge; one occurrence releases it at a step boundary, changing the compiled constraint set with no allocation during the tick; a second pulse changes nothing and publishes no second occurrence; Reset restores the authored connection. The committed attachment state is on the read with a presentation driver. Holds on a coordinate and their release by physical request are `ENGINE-JOINT-KINDS`'.

*Acceptance:* two parts joined at compatible sockets move as the declared joint allows; incompatible sockets are refused at connection time; a closed loop of three pinned members holds its shape within the game-grade envelope for 10 s; a commanded release frees the parts in the tick it commits, and Reset restores the connection.

*Deletes:* `AnchorPointDescriptor` in its string-named, uncompiled form.

*Depends on:* ENGINE-JOINT-KINDS, ENGINE-CAPABILITY-INSTANCES, ENGINE-SIGNAL-NODES. *Unblocks:* EL-112, EL-113, EL-114, EL-115, EL-117, EL-185, EL-186, EL-219, EL-221, GAP-04, and the player-made limited connectors of EL-159 to EL-161.

### Story: Package joints on a dynamic base
*Status: Backlog. Sprint key: `engine-characters` (kept; formerly ENGINE-CHARACTERS, retitled and reduced by coordinator decision 16). Order 32. After ENGINE-CONTACT-OBSERVATIONS and ENGINE-SUPPLY-DEMAND. No agent, behaviour table, perception query or route planner.*

*Outcome:* a package whose base body is dynamic and carries driven hinges and sprung sliders rolls, coasts and stops as ordinary physics.

*Present today:* hinges and sliders on any support body are admitted by the validator and compiler; the drive reaction on a free base is tested only without contact (`CuriousContraptions.tests/RevoluteDriveTests.cs:503`, zero gravity, no ground).

*Contract:* qualification only, on two differently sized fixtures with an internal finite source: the base rolls by contact friction under its driven hinges, coasts and stops unsupplied, gains no momentum in isolation, and survives Reset and save/load; a sprung slider on the dynamic base keeps its stops and its endpoint contact while the base rolls; two driven hinges in one package each follow their own consumer.

*Acceptance:* as the contract, with travel and stopping asserted within the game-grade envelope inside a stated stall guard.

*Deletes:* the scripted-agent text of the former ENGINE-CHARACTERS.

*Depends on:* ENGINE-CONTACT-OBSERVATIONS, ENGINE-SUPPLY-DEMAND, ENGINE-JOINT-KINDS (dynamic support). *Unblocks:* EL-082, EL-083, EL-086, EL-087.

### Further stories (name proposed, not in owner decision 3a)

### Story ENGINE-REACTION-THRUST: Airflow source reaction, converging flow and ambient angular drag
*Status: Backlog. Sprint key: `engine-reaction-thrust`. Order 33. Name proposed, not in 3a; it is "the thrust story" to which coordinator decision 16 gives the ambient angular drag. Amends REQ-12. After ENGINE-SPATIAL-QUERIES.*

*Outcome:* an airflow source on a free body pushes it by its jet even with nothing in the cone; a source can draw toward its origin; bodies can declare angular drag.

*Present today:* a conical airflow source on a dynamic frame receives the equal and opposite force and moment of what it delivers to each exposed receiver sample, paid from its store, supply or coordinate (`CuriousContraptions.tests/AirflowGeneralityTests.cs:189`); there is no reaction when nothing is in the cone and no angular drag.

*Contract (one change to the airflow source descriptor, its wire row and read):* (a) **ambient reaction**: a reaction impulse on the frame body at the source origin, opposite the axis, equal to the delivered law for the still ambient medium, (granted limit − impulse granted to exposed receivers) × pressure scale × clamp((flow speed + origin velocity · axis) ÷ flow speed, 0, 1); work paid = that impulse × flow speed from the same finite input, for every input kind; applied at the origin, so an off-centre source turns its body; the read reports the ambient share; (b) **flow sense** as a closed enum (Diverging, Converging): a converging source's force on a sample points to the origin and its occlusion segment runs from sample to origin; a converging source has no ambient share, and declaring both is refused; (c) **ambient angular drag** (1/s) on an ambient load, scaled by the atmosphere's pressure scale, zero by default. Boundary with `ENGINE-GAS-NETWORK`: gas-fed nozzles and their thrust are that story's; the reaction on the source body is this one's.

*Acceptance:* two free bodies with a declared ambient reaction and a finite store accelerate opposite their axis, never exceed their flow speed, and stop accelerating when the store is empty; total debit equals work paid within the ledger residual; the same holds for a supply-consumer and a coordinate input; with a receiver filling the cone the pair gains no net momentum; in Vacuum or disabled nothing is applied or debited; a source declared off the centre of mass, with nothing in its cone, gives its body, from rest, a spin rate after 0.25 s within 10 % of (origin − centre of mass) × reaction ÷ inertia × 0.25 s, about that axis and in that sense (the quantity is the body's angular speed in rad/s; the gameplay consequence is that an off-centre jet turns its body the way and at the rate its lever arm and inertia predict, so a level can aim a turning part; 10 % is wider than the fall of the reaction over the window as the origin gains speed along the axis, which the fixture keeps under 2 % by a flow speed at least fifty times the speed reached, and narrower than each plausible defect: no lever arm gives 0, the opposite sense −100 %, and the fixture's inertia about the source origin differs from its inertia about the centre of mass by more than 25 %); a supply-fed source whose signal-controlled contact is open applies and debits nothing; two converging sources draw a receiver toward their origin, and a solid between receiver and origin stops it while one behind does not; two bodies given the same spin stop within their declared time-constant ratio and zero drag spins on.

*Deletes:* nothing (additive to one descriptor; no second source law).

*Depends on:* ENGINE-SPATIAL-QUERIES, ENGINE-SUPPLY-DEMAND. *Unblocks:* EL-090, EL-092, EL-100, EL-104, EL-105.

### Story ENGINE-SURFACE-STATE: Treatment regions that change a body's surface
*Status: Backlog. Sprint key: `engine-surface-state`. Order 34. Name proposed, not in 3a. After engine-goal-evaluation.*

*Outcome:* a declared treatment region changes one contact-material value or a surface tag of a body that dwells in contact with its applicator, once per unit of a finite store.

*Present today:* collider material lanes are written once at ingest (`SIM/WorkshopPhysicsEngine.cs:532-535`); target selection exists for residence sensors only (`ABS/TargetSelector.cs:12`); the store-fraction driver reads electrical stores only (`ABS/ElementPresentation.cs:23`).

*Contract:* a treatment region names an applicator collider, a target selector, a dwell of cumulative contact (`ENGINE-CONTACT-OBSERVATIONS`), a whole-number store, a local treatment id and one effect: set one declared contact-material value on every collider of the treated body, or set a declared surface tag (a typed local id with an overlay style). Per-body state is a fixed-width lane compiled at Run; the effect applies at a step boundary, lasts until Reset and is never saved; canonical body order decides when more bodies complete in one tick than units remain. Each treatment publishes a committed first-occurrence `Treated` observation the goal list can select; a per-body surface-state read and a renderer overlay from it; any store's committed fraction as a presentation driver. The engine knows no tag or coating by name.

*Acceptance:* contact just under and just over the dwell: only the second is treated and debits one unit; an empty store treats nothing; a treated body's pair friction follows the new value from the next tick; a goal of {Captured, Treated} is Solved for a treated subject and Waiting for an untreated one; a goal naming an undeclared treatment is refused at load; Reset restores values, tags, latches and the store.

*Deletes:* the ingest-once material lane as the only form.

*Depends on:* ENGINE-CONTACT-OBSERVATIONS, ENGINE-GOALS (first slice). *Unblocks:* EL-118, EL-119.

### Story ENGINE-PARAMETER-COMBINATIONS: Excluded option tuples
*Status: Backlog. Sprint key: `engine-parameter-combinations`. Order 35. Name proposed, not in 3a.*

*Outcome:* a package can forbid combinations of its Choice options.

*Present today:* each parameter is validated alone.

*Contract:* a package declares excluded tuples over two or more of its Choice parameters; an edit, placement, paste, save or load that would produce one is refused atomically with a typed reason naming the parameters; the default tuple may not be excluded (bundle validation); the authoring read model reports which options are currently unavailable.

*Acceptance:* on two fixtures with different exclusions, an excluded tuple is refused from either parameter's edit and from a save, the construction is unchanged, and every non-excluded tuple is admitted.

*Deletes:* nothing. *Depends on:* ENGINE-PARAMETER-BOUND-FRAMES. *Unblocks:* EL-076, EL-080.

### Story ENGINE-TUNNEL-QUALIFICATION: Tunnelling envelope for every shape
*Status: Backlog. Sprint key: `engine-tunnel-qualification`. Order 36. Name proposed, not in 3a. Tests first.*

*Outcome:* the `DOC:375` envelope (no tunnelling of a 1 cm wall at 64 m/s) is proven for every admitted dynamic shape, not only spheres.

*Present today:* the speculative margin includes spin reach (`SIM/WorkshopPhysicsEngine.cs:1112-1117`); only spheres get the conservative-advancement normal (`SIM/WorkshopCollisionKernels.cs:1940-1946`); a cylinder at 64 m/s is tested (`WorkshopPhysicsLawCylinderTests.ACylinderAtSixtyFourMetresPerSecondNeverPassesAOneCentimetreWall`).

*Contract:* native scenarios for a box, a cylinder, a hollow shape and a thin plate at 64 m/s and at the angular bound against a 1 cm wall, and two fast bodies meeting. If any fails, the fix is a general swept test in the segment cast, not a per-shape rule.

*Acceptance:* the body's centre never reaches the wall's near face plane in any scenario.

*Deletes:* nothing unless a fix replaces a path. *Depends on:* ENGINE-SPATIAL-QUERIES. *Unblocks:* no identity directly (fast bodies everywhere).

### Story ENGINE-ART-SOLIDITY: Each package declares which of its art is solid
*Status: Backlog; drafted contract [spec-engine-art-solidity.md](../implementation-artifacts/spec-engine-art-solidity.md), blocked. Sprint key: `engine-art-solidity`. Order 37. Name proposed, not in 3a. Not a repair part of owner decision 22, so it waits for the programme.*

*Outcome:* every mesh of every package is declared solid, hollow, arc chord or decorative as typed build-time data; the bundle generator refuses an unclassified or doubly classified mesh; the visible-solid test reads the declarations.

*Present today:* the classification is a hand table inside one test.

*Acceptance and deletes:* as the drafted contract; the hand table is deleted.

*Depends on:* ENGINE-CAPABILITY-INSTANCES (visible-proxy refusal). *Unblocks:* every package's visible-solid row.

### Story ENGINE-PARALLEL-ISLANDS: Islands solved in parallel
*Status: Conditional. Sprint key: `engine-parallel-islands`. Order 38. Name proposed, not in 3a. Only if ENGINE-SOA-SIMD misses the REQ-04 tick target at P0-034.*

*Outcome:* independent islands are solved on worker threads (`DOC:274`), with results committed in canonical island order. *Present today:* single-threaded (`SIM/wwwroot/worker.ts:139`); no project sets `WasmEnableThreads`, so it is impossible as configured (ABI audit G15). It becomes possible once `ENGINE-WASM-THREADS` provides threads, shared memory and the deterministic worker pool, `ENGINE-SOA-SIMD` keeps each island's rows in shared tables and `ENGINE-ISLANDS-SLEEP` builds the islands; this story then schedules island solves on that pool and stays conditional on the tick target (owner decision 24k changes its enabling, not its condition). *Acceptance:* the same outcomes as single-threaded within the game-grade envelope, on 1, 2 and 4 threads; R1 and R2 tick time before and after. *Depends on:* ENGINE-WASM-THREADS, ENGINE-SOA-SIMD, ENGINE-ISLANDS-SLEEP. *Unblocks:* no identity directly.

---

## Epic 15: Progressively Taught Campaign (at least 150 levels)

**Goal:** Deliver the progressively taught campaign: at least 150 levels (244 planned) in 15 chapters, one story per chapter. Every placeable part is used by a lesson, each mechanic is introduced once, no level introduces more than two or three new parts, and later levels combine and build on what earlier levels taught (owner decisions 24c to 24f, 23 November 2026; `AGENTS.md`). Each level is verified natively with its solution, negative controls and exact Reset; the actual-Chrome proof belongs to the post-catalogue phase.

**Chapter authority (reconciled 21 November 2026).** The [requirements campaign plan](../../docs/planning/requirements.md#campaign-plan) defines the 15 chapters; the earlier five-chapter, three-part labels in Stories 15.1–15.15 are replaced below. Each story is one chapter. Its teaching focus, first-use elements and reuse come from that chapter's requirements row, the [chapter reservations](../../docs/planning/invest/campaign-reservations.md) and the [level scopes](../../docs/planning/invest/campaign.md); a story's summary sentence never narrows them. Sprint keys keep their story number and carry the corrected chapter number. **Owner decisions 24c and 24d (23 November 2026):** the campaign now has 244 levels (owner decisions 24c to 24f and coordinator decisions 35 and 36). Each chapter story covers its chapter's levels in the [per-level allocation](../../docs/planning/elements/readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f), lettered levels included, with at most three new parts in any level and every part's prerequisites taught before it (coordinator decision 36); 29 authored levels that repeat an introduced mechanic are removed, 2 are converted, and every kept authored level moves to the chapter its parts allow.

**Early level proof (owner, 21 November 2026).** Under AGENTS.md's **Early level proof** rule, a planned level that uses only delivered parts may be authored and proven natively before catalogue closure. Every Early level proof waits for the engine programme (owner decision 22, 23 November 2026; [engine programme](#engine-first-capability-stories-21-november-2026)). After it, of the 45 planned levels that use only delivered parts, the 11 with a single captured or activated-after goal can be proven (21 November 2026 audit; after the owner decision 24d removals, 9 levels are provable and 7 wait for the goal slices); the other 34 also wait for Story ENGINE-GOALS. That proof belongs to the level's chapter story; the Chrome clauses below stay in the post-catalogue phase, and a chapter story closes only when every level of its chapter passes.

### Story 15.0: Physical Placement Nudging & Difficulty Assistance
*Status: Backlog*

**Native implementation stage (sprint key `15-0-physical-placement-nudging-difficulty-assistance`):** after all named intrinsic element implementations, before `catalogue-native-closure`. This is one bounded authoring-assistance behavior slice, not the exhaustive campaign/difficulty sweep. It owns the remaining CAT-049/CAT-050 native assistance criteria from Stories 6.9/6.10 and preserves their numerical expectations, including the CAT-049 valid Precise alternative route. Through the shared native harness and production placement/Undo/command interfaces, prove the source positive routes, one-cause controls and just-inside/exact/just-outside tolerance boundaries; locked/overlap refusals; no correction during Run; Reset and supported Save/Load. Verify declared presentation feedback through its evaluator. Record matching unit/integration results and individual review/publication before marking this native stage done.

**Post-catalogue qualification stage:** the actual UI/Chrome clauses below, the retained exhaustive difficulty/campaign sweeps and browser-regression suites remain with `post-catalogue-browser-qualification` and campaign owners after native catalogue closure. The same source behaviors are retained; native completion is labelled browser qualification pending. Browser re-enablement depends on native Story 15.0, never the reverse.

*Depends on: Epic 9A (declared element packages and parameters) and Story 9R.6 (typed mouth snapping). New story (owner decision, 18 Oct 2026), replacing the earlier "not scheduled" decision. It closes the open assistance clauses of Stories 6.9 (CAT-049) and 6.10 (CAT-050).*

As a player,  
I want parts I place near a working position to be eased into it at the difficulty I chose,  
So that easier difficulties forgive small placement errors and Precise rewards exact building.

**Acceptance Criteria:**
- **Given** a level with a declared difficulty tier (Forgiving, Balanced or Precise) and per-part authored assistance controls, **When** the player releases a dragged part within that tier's declared position and angle tolerance of a declared working pose, **Then** the part is physically nudged into that pose, and the nudge is visible to the player.
  - The nudge is a bounded authoring-time correction of the placed transform. It is not a solver force, and it never applies during Run.
- **And** the tolerances are declaration data per tier and per part. No element identifier appears in the nudging code.
- **And** CAT-049 and CAT-050 prove their declared depth-error results through the actual UI:
  - Forgiving and Balanced succeed where Precise fails, at the same placement error;
  - the boundary is tested at just inside, exactly at, and just outside each tolerance.
- **And** the same mechanism closes three more native controls, each with its source values ([CAT-048](../../docs/planning/elements/CAT-048-pipe.md) facts 5 and 18, [CAT-015](../../docs/planning/elements/CAT-015-bumper.md) fact 5, [CAT-054](../../docs/planning/elements/CAT-054-ramp.md) facts 1 to 5):
  - CAT-048's depth controls: in `clear_pipe` a reference pipe moved 0.48 m in depth wins at precision 0 and 0.45 but not at 1; in `joined_pipe` the straight pipe uses the narrow bend windows (position 0.6 / 0.5 m, rotation 5° / 2°, equal correction caps at precision 0 / 0.45);
  - CAT-015's isolated authored-assistance control: in `bumper_sidekick` a bumper placed at (−3.110, 1.500, 0.035) is corrected to (−3.2, 1.5, 0) and the level is rescued by nudging alone at precision 0 and 0.45 but not at 1; the bumper does not move at Run start and Reset restores the placed pose;
  - CAT-054's `first_principles` knots: the per-ramp knots at precision 0 / 0.45 / 1 stored in `content/puzzles.json` (position step 0.3 / 0.12 / 0 m, rotation step 5° / 2° / 0°, windows 3 m and 100°, then 1.5 m and 45°, blend 0.4 s) are applied, interpolated linearly between knots; inside the window the placed ramp is corrected by no more than the knot's cap and the level then solves; outside the window, and at precision 1, the transform is unchanged.
- **And** nudging never moves a locked part. It never overlaps another part's collider: an overlap leaves the part where the player put it. Undo restores the pre-nudge pose.
- **And** Run/Reset and Save/Load preserve the nudged pose exactly.
- **And** `tools/e2e/nudging.test.ts` passes 100%, the `cat-049` and `cat-050` assistance cases pass, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run.

### Story 15.1: Campaign Levels 1–10 (Chapter 1: "On a Roll")
*Status: Backlog*

As a player,  
I want introductory levels teaching ball drops, ramps, rebounds, dominoes and basic capture,  
So that I learn the core mechanics of Curious Contraptions.

**Acceptance Criteria:**
- **Given** the 17 levels of Chapter 1 in the per-level allocation (authored: `first_principles`, `spring_forward`, `a_gentle_rebound`, `ready_to_rebound`, `domino_effect` re-authored without its lamp and with a Captured goal (the last domino knocks a ball into a Receiving basket, coordinator decision 41), `bumper_sidekick`, `bumper_depth`, `wall_and_bumper` and `burst_of_motion`; new level 9b introduces the Structural brace after the pivot under coordinator decision 36; `power_trip`, `air_mail`, `third_dimension`, `spring_signal` and `cold_start` move to chapter 2 under owner decision 24f, and `wind_signal` is removed under 24d),
- **When** executed in Chrome via Playwright,
- **Then** each level possesses an admitted inventory, solvable route, and Solved celebration.
- **And** exact Reset restores initial authoring state bit-for-bit.
- **And** `tools/e2e/campaign-01.test.ts` passes 100% in Chrome serially.

### Story 15.2: Campaign Levels 11–20 (Chapter 2: "A Helping Weight")
*Status: Backlog*

As a player,  
I want the levels of "A Helping Weight" (25, with their lettered levels) to teach the parts and combinations the requirements reserve for slots 11–20,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** the 25 levels of Chapter 2 (slots 11 through 20 and their lettered levels), with the teaching focus and reservations its requirements row names,
- **When** solved via real UI placements,
- **Then** all 25 levels transition to Solved and reset cleanly, and completing them unlocks Chapter 3.
- **And** `tools/e2e/campaign-02.test.ts` passes 100% in Chrome serially.

### Story 15.3: Campaign Levels 21–30 (Chapter 3: "Pipe Dreams")
*Status: Backlog*

As a player,  
I want the levels of "Pipe Dreams" (20, with their lettered levels) to teach the parts and combinations the requirements reserve for slots 21–30,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** the 20 levels of Chapter 3 (slots 21 through 30 and their lettered levels), with the teaching focus and reservations its requirements row names,
- **When** played in Chrome,
- **Then** all 20 levels pass with 100% verification and unlock Chapter 4.
- **And** `tools/e2e/campaign-03.test.ts` passes 100% in Chrome serially.

### Story 15.4: Campaign Levels 31–40 (Chapter 4: "Gears of an Idea")
*Status: Backlog*

As a player,  
I want the levels of "Gears of an Idea" (17, with their lettered levels) to teach the parts and combinations the requirements reserve for slots 31–40,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** the 17 levels of Chapter 4 (slots 31 through 40 and their lettered levels), with the teaching focus and reservations its requirements row names,
- **When** solved in Chrome,
- **Then** all 17 levels pass with verified Solved transitions and unlock Chapter 5.
- **And** `tools/e2e/campaign-04.test.ts` passes 100% in Chrome serially.

### Story 15.5: Campaign Levels 41–50 (Chapter 5: "Ready, Set, Throw!")
*Status: Backlog*

As a player,  
I want the levels of "Ready, Set, Throw!" (18, with their lettered levels) to teach the parts and combinations the requirements reserve for slots 41–50,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** the 18 levels of Chapter 5 (slots 41 through 50 and their lettered levels), with the teaching focus and reservations its requirements row names,
- **When** tested in Chrome,
- **Then** all 18 levels achieve 100% pass rate serially and unlock Chapter 6.
- **And** `tools/e2e/campaign-05.test.ts` passes 100% in Chrome serially.

### Story 15.6: Campaign Levels 51–60 (Chapter 6: "A Bright Idea")
*Status: Backlog*

As a player,  
I want the levels of "A Bright Idea" (27, with their lettered levels) to teach the parts and combinations the requirements reserve for slots 51–60,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** the 27 levels of Chapter 6 (slots 51 through 60 and their lettered levels), with the teaching focus and reservations its requirements row names,
- **When** solved in Chrome,
- **Then** all 27 levels solve and unlock Chapter 7.
- **And** `tools/e2e/campaign-06.test.ts` passes 100% in Chrome serially.

### Story 15.7: Campaign Levels 61–70 (Chapter 7: "Go with the Flow")
*Status: Backlog*

As a player,  
I want the levels of "Go with the Flow" (23, with their lettered levels) to teach the parts and combinations the requirements reserve for slots 61–70,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** the 23 levels of Chapter 7 (slots 61 through 70 and their lettered levels), with the teaching focus and reservations its requirements row names,
- **When** executed in Chrome,
- **Then** all 23 levels solve and unlock Chapter 8.
- **And** `tools/e2e/campaign-07.test.ts` passes 100% in Chrome serially.

### Story 15.8: Campaign Levels 71–80 (Chapter 8: "Sounds Like a Plan")
*Status: Backlog*

As a player,  
I want the levels of "Sounds Like a Plan" (20, with their lettered levels) to teach the parts and combinations the requirements reserve for slots 71–80,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** the 20 levels of Chapter 8 (slots 71 through 80 and their lettered levels), with the teaching focus and reservations its requirements row names,
- **When** tested in Chrome,
- **Then** all 20 levels pass 100% serially with verified solutions and unlock Chapter 9.
- **And** `tools/e2e/campaign-08.test.ts` passes 100% in Chrome serially.

### Story 15.9: Campaign Levels 81–90 (Chapter 9: "Stored Potential")
*Status: Backlog*

As a player,  
I want the ten levels of "Stored Potential" to teach the parts and combinations the requirements reserve for slots 81–90,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** Levels 81 through 90 in Chapter 9, with the teaching focus and reservations its requirements row names,
- **When** executed in Chrome,
- **Then** all 10 levels solve and unlock Chapter 10.
- **And** `tools/e2e/campaign-09.test.ts` passes 100% in Chrome serially.

### Story 15.10: Campaign Levels 91–100 (Chapter 10: "Oddly Satisfying")
*Status: Backlog*

As a player,  
I want the levels of "Oddly Satisfying" (17, with their lettered levels) to teach the parts and combinations the requirements reserve for slots 91–100,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** the 17 levels of Chapter 10 (slots 91 through 100 and their lettered levels), with the teaching focus and reservations its requirements row names,
- **When** played in Chrome,
- **Then** all 17 levels solve and unlock Chapter 11.
- **And** `tools/e2e/campaign-10.test.ts` passes 100% in Chrome serially.

### Story 15.11: Campaign Levels 101–110 (Chapter 11: "A Ray of Possibility")
*Status: Backlog*

As a player,  
I want the ten levels of "A Ray of Possibility" to teach the parts and combinations the requirements reserve for slots 101–110,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** Levels 101 through 110 in Chapter 11, with the teaching focus and reservations its requirements row names,
- **When** tested in Chrome,
- **Then** all 10 levels pass 100% serially and unlock Chapter 12.
- **And** `tools/e2e/campaign-11.test.ts` passes 100% in Chrome serially.

### Story 15.12: Campaign Levels 111–120 (Chapter 12: "Particle Manners")
*Status: Backlog*

As a player,  
I want the ten levels of "Particle Manners" to teach the parts and combinations the requirements reserve for slots 111–120,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** Levels 111 through 120 in Chapter 12, with the teaching focus and reservations its requirements row names,
- **When** solved in Chrome,
- **Then** all 10 levels solve and unlock Chapter 13.
- **And** `tools/e2e/campaign-12.test.ts` passes 100% in Chrome serially.

### Story 15.13: Campaign Levels 121–130 (Chapter 13: "Half the Battle")
*Status: Backlog*

As a player,  
I want the ten levels of "Half the Battle" to teach the parts and combinations the requirements reserve for slots 121–130,
So that each new part is introduced, practised and combined before the next chapter.

**Acceptance Criteria:**
- **Given** Levels 121 through 130 in Chapter 13, with the teaching focus and reservations its requirements row names,
- **When** solved in Chrome,
- **Then** all 10 levels achieve verified Solved status and unlock Chapter 14.
- **And** `tools/e2e/campaign-13.test.ts` passes 100% in Chrome serially.

### Story 15.14: Campaign Levels 131–140 (Chapter 14: "Many Happy Returns")
*Status: Backlog*

As a player,  
I want the ten levels of "Many Happy Returns" to teach the parts and combinations the requirements reserve for slots 131–140,
So that each new part is introduced, practised and combined before the final chapter.

**Acceptance Criteria:**
- **Given** Levels 131 through 140 in Chapter 14, with the teaching focus and reservations its requirements row names,
- **When** tested in Chrome,
- **Then** all 10 levels pass 100% serially and unlock Chapter 15.
- **And** `tools/e2e/campaign-14.test.ts` passes 100% in Chrome serially.

### Story 15.15: Campaign Levels 141–150 (Chapter 15: "All Together Now")
*Status: Backlog*

As a player,  
I want the final chapter to combine everything taught, introducing no new required element,
So that solving every campaign level completes the game with celebratory recognition.

**Acceptance Criteria:**
- **Given** Levels 141 through 150 in Chapter 15, with the combinations its requirements row names,
- **When** solved in Chrome,
- **Then** the final levels complete cleanly, culminating in the Campaign Completion achievement (the final level of the campaign, whatever its count).
- **And** `tools/e2e/campaign-15.test.ts` passes 100% in Chrome serially.

---

## Epic 16: Release Qualification, Performance Audits & Production Packaging

**Goal:** Execute formal qualification gates (P0-034, P0-035), cross-platform WebGL2/WebGPU rendering audits, memory leak audits, and production artifact packaging for final distribution.

### Story 16.1: Cross-Platform Simulation Timing & Telemetry Audit (P0-034 Gate)
*Status: Backlog*

As an engine architect,  
I want formal verification that the WASM SIMD physics solver executes within fixed timing budgets,  
So that the game runs smoothly across diverse consumer hardware.

**Acceptance Criteria:**
- **Given** the 3600-tick benchmark scenes under full active workloads,
- **When** profiled across 20 consecutive runs,
- **Then** simulation tick p95 $\le 2.5\text{ ms}$ at 120 Hz, with simulated/wall ratio between 0.99 and 1.01 and zero accumulating clock debt.
- **And** `tools/e2e/perf-timing-audit.test.ts` passes 100% in Chrome.

### Story 16.2: Universal Rendering & WebGL2/WebGPU Hardware Pacing
*Status: Backlog*

As an engine architect,  
I want verified instanced draw batches and sustained 60 FPS display pacing on both WebGL 2.0 and WebGPU,  
So that rendering remains buttery smooth without frame drops or pipeline stalls.

**Acceptance Criteria:**
- **Given** dense contraption scenes with 50+ active elements,
- **When** rendered on both WebGL 2.0 and WebGPU backends,
- **Then** frame rate sustains $\ge 59.4$ FPS with $\le 1\%$ missed cadence slots.
- **And** `tools/e2e/perf-render-pacing.test.ts` passes 100% in Chrome.

### Story 16.3: Long-Session Memory Leak & Lifecycle Audit (20 Cycles)
*Status: Backlog*

As an engine maintainer,  
I want proof that extended gameplay sessions have zero memory leaks or uncollected buffers,  
So that browser memory consumption remains completely flat over hours of play.

**Acceptance Criteria:**
- **Given** 20 continuous Run/Reset cycles and 20 level transitions in Chrome,
- **When** heaps are sampled after garbage collection,
- **Then** zero growth in live simulation worlds, workers, event listeners, or leased buffers is observed.
- **And** `tools/e2e/perf-memory-audit.test.ts` passes 100% in Chrome.

### Story 16.4: Production Build Packaging, Asset Optimizations & Final Release Manifest
*Status: Backlog*

As a release engineer,  
I want optimized, minified production assets and an audited distribution bundle,  
So that Curious Contraptions is ready for public release.

**Acceptance Criteria:**
- **Given** the production build pipeline,
- **When** executed,
- **Then** production WASM modules, scripts, and content packs build cleanly with 0 warnings.
- **And** all 72 catalogue parts and all 302 named element identities of the [ledger](../../docs/planning/invest/named-elements.md) (the same set `catalogue-native-closure` verifies), every campaign level (at least 150; 244 planned), and the Free Workshop sandbox are 100% verified in the final distribution bundle.

---

## Filed Follow-ups (back of the queue)

*Owner decision, 19 Oct 2026, evening: review findings beyond a story's own acceptance criteria are filed here, after every other queued story, unless they are a player-visible crash. See the [verification tiers](autonomous-development-guide.md#verification-tiers).*

### Story F.6: Contact-Load Follow-ups
*Status: Backlog. Filed from the Story 9R.4 review on 20 Oct 2026 ([review](../implementation-artifacts/review-9r-4-pressure-plate-per-instance-load-rows.md)).*

**Acceptance Criteria:**
- **Placement:** a domino placed from the palette onto a plate rests on top, not straddling it, so two dominoes open the gate. This is player-visible (F-1). *Moved into C-Plate on 21 Oct 2026; F.6 keeps its other items.*
- **Plate behaviour:**
  - Underweight is proven directly (F-3);
  - the plate's sensor id no longer shares the +6 offset with the passage sensor (F-4);
  - the box-contact alignment tolerance covers slightly tilted resting boxes (F-6).
- **Performance gate:** no per-tick array allocation for committed reads (F-7).
- **Tests:** the face and alignment filters are killed by engine-path tests (F-8).
- **Docs:** `CAT-052-pressure_plate.md` §2 describes the normal-and-gap region.

### Story F.5: Supply Route Follow-ups
*Status: Backlog. Filed from the Story 9R.3 review on 20 Oct 2026 ([review](../implementation-artifacts/review-9r-3-direct-gate-own-supply-route.md)).*

**Acceptance Criteria:**
- **Plate drag band:** the plate drag-band ratchet owner and the code comments agree (F3), and a native test covers the plate interim's behaviour (F4).
- **Docs and comments:**
  - the CAT-051 doc states the decided binary enable (F5);
  - the D4 comment in Chrome is accurate (F6);
  - the two "SwitchedSupply" test names are renamed (F9).
- **Mutant coverage:** `WorkshopSpringboardTests` kills the "drop `SupplyClosed`" mutant, or the spec is amended (F2).
- **Declaration:** `PoweredGateElementDeclaration` drag 0.05 is reconciled (F7).
- **Supply:** supply comes from remaining battery charge, not enable alone (deferred by the coordinator).
- **Scheduling:** cat-051-own-supply is requalified as Parallel only through calibration (F8).

### Story F.4: Collider Table Follow-ups
*Current stage ownership24October2026: F1 checked cast, F2 never-fired occurrence bounds and F4 extreme-count refusal belong to 9A.44 ABI/field migration; F3 buffer reuse and F8 pair-order behavior belong to harness A/B; moving-body Reset behavior belongs to harness B. Browser snapshot-helper and performance items remain at their stated later gates. The engine boundary cases are required before declaring stabilization complete.*
*Status: Backlog. Filed from the Story 9R.2 review on 20 Oct 2026 ([review](../implementation-artifacts/review-9r-2-collider-table-sized-at-run.md)).*

**Acceptance Criteria:**
- **Guards:**
  - a checked u16 collider-slot cast (F1);
  - a bounded collider field on occurrences that never fired (F2);
  - the correct exception type at extreme collider counts (F4).
- **Tests:**
  - buffer reuse (F3);
  - a permanent broadphase pair-order test (F8);
  - a moving body in `capacity-9r` test 1, so its Reset compare isn't near-vacuous.
- **The 9R.1 snapshot helper accepts a bench with no moving body** (`capacity-9r` test 1).
- **`anvil --changed`** detects unstaged edits in a worktree.
- **Performance gate items:**
  - 2.79 ms per tick on the 32-bend bench (F6);
  - building the static BVH once at admission. This changes pair order, so it needs a trajectory owner.
- **Story F.1 item:** clock-master `wideOffset=1`, first seen on `1f755119` (F9).

### Story F.3: Run/Reset Snapshot Hardening
*Status: Backlog. Filed from the Story 9R.1 review on 20 Oct 2026 ([review](../implementation-artifacts/review-9r-1-exact-run-reset-snapshot-assertions.md)).*

**Acceptance Criteria:**
- **Snapshot checks:**
  - cat-051 gets a real closed-pose check (F1);
  - the listed suites gain one Reset-from-Running cycle (F2);
  - the schema parser covers fields with defaults (F5; the U7 mutant must be killed);
  - the memory parameters are required, not defaulted (F7);
  - TS enums are used instead of string constants (F8);
  - a stronger post-Reset goal check, and the inserted Saves are justified or removed (F11/F12);
  - a body mutant with the guard bypassed (F14).
- **Gating:** the `tools/e2e/unit/*` tests run under a gate, in the hook or CI (F6).
- **Evidence:** mutant diffs are kept as evidence (F10).
- **Cleanup:**
  - the fake hosts quote the current response size;
  - one cosmetic line (F9).

### Story F.2: AOT Publish Guard Hardening
*Status: Backlog. Filed from the Story 9R.0b review on 20 Oct 2026 ([review](../implementation-artifacts/review-9r-0b-aot-compiled-wasm-simd-workers.md)). Amelia-5's partly finished work is on branch `story-f2-aot-guard-hardening`.*

**Acceptance Criteria:**
- **Guard checks:**
  - the guard fails on any skipped engine or FlatBuffers method, or on a missing `aot_instances` symbol;
  - the outer guard fails when a nested report is missing;
  - the guard's tests run in the hook and in CI;
  - a missing wasm-tools workload or AOT cross-compiler produces an explicit error.
- **SIMD proof:** each hot solver function has a minimum f32x4 count, and the Animation worker's `.o` is counted too.
- **Cross-thread audit:** the audit is corrected (`MachineWorld.Gpu.cs:311-318`, which bit-compares the main thread's results with the worker's), and a property test covers the reachable rotations.
- **Coverage and measurements:**
  - a suite throws and catches an exception inside the worker under LLVMOnly;
  - the CI publish time is measured;
  - the size and publish-time evidence is committed.
- **Lows:**
  - stale extra files in the host folders;
  - the namespace shape;
  - the largest-fixture claim.

### Story F.1: E2E Runner Hardening
*Status: Backlog. Cut from Story 9R.0a on 19 Oct 2026.*

**Acceptance Criteria:**
- A second consecutive parallel full-run proof, and the 2N oversubscription trial.
- The background checkpoint runs against a committed HEAD in a separate worktree and preview port.
- A per-run PID ledger, with SIGINT/SIGTERM handling and stale-ledger reaping.
- A cross-worktree global runner lock.
- A seeded failure in one parallel suite is reported against that suite, and the other suites are unaffected.
- Story 9R.0a review findings F1–F9 (Murdoch-5, 19 Oct 2026; [review](../implementation-artifacts/review-9r-0a-tiered-parallel-e2e-runner.md)):
  - a Parallel qualification pinned to the suite digest;
  - the spec's "no lexical timing detector" rule;
  - a runner pass rule that counts debt faults;
  - `closeExpecting` excusing exactly one declared fault;
  - product source identity;
  - committed level and bundle edits under `--changed`;
  - silent partners that have no Primary suite;
  - `tools/e2e/unit/**` selection;
  - live skip and todo fixtures;
  - and from the final round:
    - removal of the kept lexical `TimingDetector` (F2);
    - superseded spec text (F11);
    - a parse guard for the level-parts reader (F12);
    - a check that Node modules resolve (F13);
    - deterministic bundle hashes across publishes of one commit (F14).
- Recalibrate the runner's measured N and per-suite durations under the AOT workers. Both were measured under the interpreter (Story 9R.0b design review item 8, 19 Oct 2026); the 9R.0b full run passed with them unchanged.
