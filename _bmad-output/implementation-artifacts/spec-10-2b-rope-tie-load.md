---
title: '10.2b — Rope Anchor, tensile rope and cutting'
type: 'feature'
created: '2026-10-30'
status: 'done'
baseline_revision: '71135ecd0109b8d5eba0a9f4b944e2ffbefceedd'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-10-context.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Delivered Weights have no usable rope attachment, and players cannot connect a fixed anchor to a load, observe tensile coupling, or cut that connection. Story10.2b delivers CAT-058/EL-064 and the straight-rope portion of EL-205 before Pulley.

**Approach:** Add the isolated RopeAnchor declaration and generic parameter-driven Tie/Load ports, main-owned measured connection/route compilation, one unilateral length capability in the existing TGS Soft runtime, committed connection artwork and typed runtime cutting. Preserve Weight behavior and the current authoring → compiled Core → presentation boundary.

## Boundaries & Constraints

**Always:** Preserve CAT-058, EL-064, EL-205, todo-140/141/469 and later integration owners. Source decisions below settle entry before code. Use real registered attachment bodies, f32 game values, exact typed identities, 480Hz physical substeps and the existing projected soft-constraint equation. Mutable storage starts at category defaults and grows geometrically with actual demand; no arbitrary rope/body/collider cap. All graph/codec admission and cuts are atomic. Endpoints, lengths, pose and parameters survive exact authored Reset/save/load. Native tests run production compiler/codec/runtime with causal gameplay outcomes and meaningful controls. UI uses existing place/connect/transform controls and a contextual Cut action; native proof now, browser qualification later.

**Never:** Package dispatch in engine/Core, an element-specific solver/update loop, a point-guide substitute for future finite-radius pulleys, rope collision/mass/elastic material features, a length editor or separate rope inventory budget, compatibility/version/migration/fallback paths, fake catalogue replacement, a second numerical law, velocity snapping, arbitrary precision or historical trajectory/tick/hash oracles, mutants, browser gates, CI monitoring, local Release-worker builds or web publish.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Authoring | RopeAnchor/Weight Tie sockets, then moves or mass edits | Connect measures world socket distance once; stored length remains; sockets follow declared transforms/parameters | Wrong/missing/self/duplicate/branched/cyclic connection or absent/out-of-range length refuses atomically |
| Hanging / slack | Complete fixed-anchor/load route; moved-closer slack control | Tension supports load; slack initially follows unconnected motion; inward motion remains free | Coincident positive-length endpoints stay slack, finite and usable |
| Reaction | Two dynamic endpoints with an independent separating cause | Equal/opposite tension, mass-weighted response, no pushing; connected contact remains enabled | Missing participant and unsupported motion/geometry refuse before install |
| Runtime cut | Running or Paused, selected intact span | Typed accepted request queues; next committed step severs its route and publishes cut state; Paused shows Cut queued until Step/Resume | Unknown, already-cut or already-pending route refuses without changing other routes |
| Cancelled candidate | Prepared physics step containing pending cut is discarded | No cut or partial read publishes; unapplied request remains pending for a later successful commit | Existing cancellation/refusal ownership applies |
| Restoration | Cut/run then Reset; saved authored machine loaded anew | Original uncut routes, exact endpoints/lengths and authored poses/parameters return; behavior recurs | Unsupported/malformed saves reject without migration |
| Presentation | Committed attachment positions plus route state | Taut straight, slack length-matched curved, incomplete dashed; gold endpoint knots; selected cut span disappears at commit | No NaNs at coincidence/vertical alignment; no optimistic visual physics |
| Demand | More routes/attachments than initial reserve | Preserved data, identities and behavior through geometric growth | Checked representation/resource failure refuses before commit |

</intent-contract>

## Code Map

- Source acceptance: epics.md Story10.2b and aggregate10.2; docs/planning/elements/CAT-058-rope_anchor.md §§2/4/5/6, EL-205-rope.md and CAT-067-weight.md; requirements.md anchors current-cat-058, element-205, todo-140, todo-141, todo-469; named-elements.md EL-064/205. Update current decision wording, not historical evidence.
- Continuity: Weight published b88ca4251d8ff37272981bdd4654bd950987d04d is an ancestor of baseline. Its terminal review, not stale spec status review, establishes delivery. Preserve actual mass-dependent cylinder/COM and palette. Old Half/hash/mutation instructions are superseded. 9A.45 published baseline supplies element-free Core, main compilation, split codecs and sole bundle membership.
- CuriousContraptions.Abstractions/PortDescriptor.cs:22/38 and SimulationDomain.cs:6; engine/gpu/WorkshopSocket.cs and WorkshopConnections.cs:11–23: extend closed domain/socket/direction sets for Rope/Tie/Bidirectional and declared attachment roles. Add a generic linear parameter-driven socket position binding using typed ParameterId and vector coefficients. Fixed LocalPosition remains the declared default; validate agreement at the parameter default and finite evaluations at both bounds. Do not implement Weight arithmetic in consumers.
- CuriousContraptions.Bundle/BundleSchema.cs:18/60/128 and BundleValidator.cs:83/224/263/333: carry/validate port position binding and attachment role in the sole bundle. Validate role/domain/direction, declared Real parameter, finite binding and scene-locator agreement at default.
- engine/gpu/ElementPortCatalogue.cs:59–168: currently rejects Bidirectional and evaluates constant positions. Provide one instance-parameter-aware socket evaluation used by connection measurement, compiler and art. DeclaredPorts.Install is immutable; no replacement test catalogue.
- elements/Weight/WeightElementDeclaration.cs:39/52–101 and weight.tscn:65: add Tie/Load/root Loc_Port_Tie. Existing eye centre is local Y=.5771 under declared uniform scale .136734*mass+.453070; binding follows that same actual eye. Body origin is base; COM differs and remains mass-dependent.
- New elements/RopeAnchor/{RopeAnchor.csproj,RopeAnchorElementDeclaration.cs,rope_anchor.tscn,icon.svg} plus standard import/uid metadata and solution/package test membership. Package references only the Abstractions layer, including CuriousContraptions.Abstractions.Mechanical; no main/Core/Bundle or sibling-element reference. Generated bundle supplies identity. Geometry/art/material below.
- engine/gpu/WorkshopConnections.cs:46–110: extend validation with explicit domains; current electrical else must not catch Rope. Rope edges are undirected for duplicate/topology purposes. Persist finite per-span length only on Rope links. Use a generic topology compiler taking declared endpoint-role data and edges; main resolves actual package declarations before calling it. Canonical endpoint/order traversal cannot depend on authored edge direction.
- ui/WorkshopConnections.cs:20/56/85/113/132: existing connect controls assume output→input. Admit bidirectional Tie, measure evaluated world sockets and validate before Undo. Moves/mass edits retain stored length; reconnect remeasures. Existing connection selection/disconnect becomes contextual runtime Cut when permitted. Save/deletion remain Building-only.
- schemas/workshop_authoring.fbs:13/73, WorkshopAuthoringFlatBuffers.cs:159/257 and WorkshopSaveCodec.cs:47: per-span length in authored connections; non-Rope has no length. Validate missing/nonfinite/range/unknown sockets and atomic restoration. No runtime cut fields in saves.
- engine/gpu/WorkshopSceneCompiler.cs:17 and WorkshopPhysicsCompiler.cs:24/55–75: compile canonical routes with evaluated body-local attachments and actual body IDs. Main retains connection-to-compiled-span mapping. Runtime RopeSpanId/RopeRouteId are nonzero strongly typed capability IDs scoped by installation; saved endpoint/socket pairs already identify authored edges, so no redundant persistent edge-ID system is needed.
- engine/gpu/PhysicsDeclarations.cs:222/256: add generic tensile-length route/attachment declarations and typed integrity/state. Retain ordered participants; admit only currently supported straight complete geometry. Open declared-guide topology has no force row. No usable guide package or finite-radius routing is added.
- engine/gpu/PhysicsWorldLayout.cs:15–45, PhysicsGpuAbi.Constraints.cs:41/100/123 and PhysicsGpuAbi.cs:292–396: demand-sized rows/attachments, logical counts, immutable declaration lanes and candidate runtime impulse/cut state; extend exact codec/layout controls.
- CuriousContraptions.Simulation/BrowserGpuTransport.cs:61–79/100–124 and WorkshopGpuDevice.cs:174/230–287: existing geometric world capacity, stage/read/validate/Commit/Discard. Cut/impulse state belongs to candidate bytes; no scratch state may leak across discarded candidates.
- CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:211/417–458/728–738/793–807: copy source to candidate, decode constraints, warm-start/solve/integrate/refresh/relax in the current 480Hz, four biased plus four relax sweeps. Do not inherit old eight-sweep prose.
- engine/gpu/SoftConstraint.cs:9/37: FromSpring and four-lane Solve4 implement the shared implicit projected law. PrismaticConstraintSolver.cs:82–107 contains reusable mass/velocity/body-impulse helpers; extract them once to a generic helper and update prismatic callers rather than copy or depend on prismatic-specific APIs. WorkshopCollisionKernels.ApplyInvInertia/InverseInertiaCoupling support COM-based angular response. WorkshopPhysicsEngine.cs:525 collision suppression remains restricted to explicitly disabled declared pairs; ropes never disable collision.
- WorkshopSimulation.cs:77/299/347–357, WorkshopMessages.cs, WorkshopCommandRouter.cs, WorkshopCommandExecutor.cs, BrowserWorkshopClient.cs, IWorkshopClient and engine/MachineWorld.Gpu.cs: extend normal runtime control path for CutRope payload and typed refusal. Follow ConfigureElectrical's Running/Paused queued-input convention, existing envelope checks and candidate ownership; never advance time solely to accept a cut.
- PhysicsPresentationRead.cs, WorkshopRead/WorkshopMessages.ValidatePresentationOwners, WorkshopWireFlatBuffers*.cs and schemas/workshop_wire.fbs: committed typed route identity, integrity, state, maximum/current length, cut-span identity and attachment geometry. Validate unique IDs/references/finites and current installation ownership at command/response boundaries.
- ui/WorkshopConnections.cs:132 → ui/WorkshopArtwork.cs:32 is the current cross-part connection art path. Extend it with a generic committed-route geometry evaluator and retained segment/knot nodes, updated after admitted presentation at MachineWorld.Gpu.cs:208–237. MachinePart.ApplyPhysicalSpans:182 is package-local and cannot substitute for cross-body rope art. Preserve activation/electrical wiring in one connection presenter.
- schemas/README.md pins flatc25.2.10. Generate runtime and authoring separately into clean scratch directories, then join sorted generated type files into WorkshopWireGenerated.cs/WorkshopAuthoringGenerated.cs; update strict TypeScript schema consumers. Core owns runtime, main owns authoring; no format version.
- Tests: shared WorkshopIntegrationHarness; WeightTests, BundlePortsTests, PackageRuntimeParityTests; tools/BundleElements.Tests/{SolutionMembershipTests,PortLocatorTests,BundleValidatorTests}; CompiledSceneBoundaryTests, WorkshopFormatTests/WorkshopWireDecodeBoundaryTests and new rope topology/physics/cut/art behavior tests. Conditional package scanner and actual compiler ownership remain required. Generic declaration fixtures exercise incomplete/cycle roles through production topology admission without a fake installed package or solver.
- Current documents: CAT-058/EL-205/CAT-067 decision and gate wording, named/epic/sprint current status and TODO. Record prior9A.45 receipt in this slice without amending its approved SHA. Keep later gates and source IDs.

## Tasks & Acceptance

**Execution:**
1. Source contracts, Abstractions/Bundle, port catalogue and RopeAnchor/Weight package files — record the finite decisions below, add declarations and canonical socket evaluation; preserve Weight geometry and material.
2. WorkshopConnections/editor/authoring schema/save — measured typed ropes, graph admission, Undo/move/reconnect/save semantics; no runtime-derived authoring mutation.
3. Main scene/physics compiler, Core declarations/ABI and shared constraint helper — one generic tensile row and candidate-owned runtime state in the existing solver schedule.
4. Runtime command/read/codec/device/client/UI — queued typed cut with exact identity, atomic step commit, pending-duplicate refusal and restoration.
5. Existing connection presenter plus shared geometry evaluator — real committed rope artwork and contextual interaction, ordinary node/buffer cleanup and retained capacity.
6. Affected native/compiled-TypeScript tests — all matrix rows, valid/refusal/control and lifecycle cases, actual game pipeline and numerical law controls. Record assertions/coverage, not hashes.
7. Current status/source docs — preserve later integration gates, passing local proof and independent review; publication after normal hooks and exact-SHA approval.

**Acceptance Criteria:**
- Given package-built RopeAnchor and mass-bound Weights, when the player uses existing contextual Connect and transform/mass controls, then actual Tie positions agree across art/measurement/compiler, the measured length persists through edits, and unsupported input refuses before construction or Undo changes.
- Given a hanging load and a matched unconnected/slack control, when production simulation advances over a useful game-time window, then the rope supports the load without material drift, slack does not accelerate it, inward movement is free, and a 3D displacement produces plausible constrained swinging without meaningful added energy.
- Given two dynamic loads with different masses and an independently arranged separating cause, when the route tensions, then reactions respect actual mass/inertia and endpoints; reversing declarations/direction preserves behavior. A free-falling two-load pair is not misrepresented as a pulley counterweight.
- Given slack or cut connected bodies, when they contact, then ordinary collision remains active; coincidence never creates a NaN or faults a tick.
- Given Running/Paused intact routes, when Cut is accepted and the next physical step commits, then only the selected route becomes Cut/Open and loses tension; duplicate/unknown requests refuse, discarded candidates publish nothing and preserve unapplied intent, and independent ropes keep their constraints.
- Given a cut machine, when Reset and supported Save/Load execute, then original uncut authored endpoints/lengths and poses/parameters restore exactly and a subsequent Run reproduces meaningful behavior.
- Given committed route reads, when the connection presenter evaluates them, then knots meet actual transformed sockets, taut/slack/open art follows state and length, and cut art changes only on commit, with finite vertical/coincident geometry and ordinary teardown.
- Given actual demand beyond defaults and malformed authored/compiled/runtime inputs, when storage grows or admission refuses, then logical data/ownership is preserved without caps or partial publication.
- Given the final candidate, when affected checks, full local solution, normal hooks and five Auto layers complete, then all required tests pass with zero skips and exact committed-SHA approval precedes push; remote main is independently verified. No browser/release/CI qualification is implied.

## Spec Change Log

## Review Triage Log

### 2026-10-30 — Review pass 1
- verdicts: 11 reported findings/assessments — high 1, medium 6, low 0, false 4, maybe-false 0. Permitted borderline/descriptive assessments are retained explicitly rather than silently dropped.
- findings (report order):
  - `[medium]` `[patch]` Blind Hunter: internal solver tests bypass the supported simulation boundary — RopeTests constructs SimBody/SimTensileSpan and invokes internal stages; integration can diverge while these checks pass. Replace with admitted production runtime motion controls; assigned to the same implementer.
  - `[medium]` `[patch]` Verification Gap: malformed cut shapes lack incoming decoder proof — filed evidence establishes encoder-only negative checks; missing/zero/unexpected cut fields need direct incoming-wire refusal and valid controls. Assigned to the same implementer.
  - `[medium]` `[patch]` Verification Gap other finding: internal rows and accumulated impulse are asserted — confirmed in RopeTests; grouped with the Blind Hunter boundary issue, retaining its distinct claim.
  - `[medium]` `[patch]` Edge Case Hunter: rope attachments are not bound to committed owner poses — translating both endpoints preserves current length/state/ownership checks in RopeSpanRead.Validate and ValidatePresentationOwners. The reported guard snippet is a missing check, not existing code. Add pose/socket validation and decoder refusal controls; assigned to the same implementer.
  - `[high]` `[patch]` Forward-Only: renderer selects authored-node geometry/state when a committed rope read is absent — confirmed at WorkshopConnections.RefreshConnectionArtwork; structural fallback violates the owner rule even though admitted observations normally contain every span. Delete the alternate rule and use the existing committed representation with typed visible refusal. Mandatory high; not deferred.
  - `[false]` `[reject]` Forward-Only permitted borderline: declaration-position and evaluated-instance queries are not an old-caller bridge — one reads declaration metadata and the other evaluates instance parameters; production consumers use the latter. No alleged compatibility outcome occurs.
  - `[false]` `[reject]` Forward-Only permitted borderline: the perpendicular sag-axis recovery is not a second solver — it continues the same cosmetic length algorithm at its degenerate axis, expressly allowed by the owner rule.
  - `[medium]` `[patch]` Intent Alignment: some physical guarantees stop at internals — same verified root cause as Blind Hunter/Verification Gap; retain mass-weighted/no-pushing coverage through admitted runtime observations.
  - `[medium]` `[patch]` Intent Alignment: malformed-input proof often tests producers rather than incoming boundaries — encoder/constructor tests do not protect the new incoming command/response contracts. Group with decoder-proof work, including translated-attachment response refusal.
  - `[false]` `[reject]` Intent Alignment descriptive distinction: incomplete-route capability proof is not playable Guide authoring — the intent expressly delivers straight Rope before Pulley, and generic incomplete declarations plus actual renderer checks satisfy that current boundary without claiming Guide gameplay.
  - `[false]` `[reject]` Intent Alignment descriptive distinction: native handler/node tests are not full browser input qualification — the intent explicitly defers browser proof; actual native authoring/controls/art remain covered, so this distinction is not a current violation.
- grouping/route: four patch groups (single committed renderer; attachment ownership validation; incoming codec proof; supported-interface physics tests). These are direct corrections at existing surfaces under already explicit intent; no new public surface, policy or unproven state is required. No intent gap, bad-spec loopback or deferral was identified. All four patch groups were implemented and independently closed in the affected re-review below; full coordinator round4 and normal publication checks remain pending.
- independent identities: /root/rope_blind_review, /root/rope_verification_review, /root/rope_edge_review, /root/rope_forward_review, /root/rope_intent_review. Each reviewed the frozen `.tmp/rope-coordinator-round3/review.diff`; original reports are retained in their task transcripts. Platform capacity required waves of three; every layer reported before triage.


## Design Notes

Entry decisions (coordinator,30October2026), grounded in source facts and existing controls rather than an unrecorded owner answer:

1. Length is connect-time world Tie distance, inclusive .05–200m per span. Zero, missing, nonfinite and out-of-range values refuse. Moving endpoints or changing mass retains authored length, allowing slack or a stretched initial dynamic route; reconnect measures anew. No separate length editor. This follows EL-205's declared workflow and avoids another authoring mode.
2. Rope is massless and has no collision or elastic energy store. Gameplay is inextensible/tension-only; numerical softness is regularization. Reuse the existing 60Hz, damping-ratio10 contact pair as the shared policy for this new length row, keeping contact behavior unchanged. This explicitly extends an existing numerical policy; it is not a claim that contacts previously defined rope material. Use effective mass m=1/K, omega=2*pi*f, k=m*omega² and d=2*m*zeta*omega with FromSpring, or an algebraically equivalent shared helper. Verify hanging, release, stretch and energy behavior; do not invent a separate rope solver.
3. One scalar row uses the negative route-length gradient and nonnegative accumulated tensile impulse. Evaluate world attachments from body origin/rotation/local position, but torque arms from world COM. Clear cached tension on slack/cut/open/degenerate rows before warm-start. Inward motion is not an equality constraint. Relax removes positional bias according to the shared soft law. No arbitrary breaking-force cap or momentum snap. Aggregate repeated-body contributions before mass evaluation if present; no inverse of zero effective mass.
4. All-static feasible routes may display with no equation; overlength immovable routes reject atomically. Current authoring admits Static/Dynamic; unsupported prescribed motion refuses explicitly. Complete guide geometry is deferred to10.2c and rejects if supplied without its supported geometry; do not silently solve point-guide sums. Generic role fixtures can still prove open/cycle admission.
5. Anchor: static mass0; box offset(0,0,-.12), half-extents(.275,.275,.08); Tie(0,0,.18); pickradius.55; restitution1, threshold.1m/s, friction.3, rolling0. Gold/ochre eye on cream mount follows CAT-058 and DESIGN. Existing full3D transforms rotate collider, Tie and art together; no separate wall-attachment mechanic. Rope is a connection, not inventory stock or a metre budget.
6. State is Open for incomplete/cut, Slack when current geometric length is below authored maximum, otherwise Taut. Taut is a length classification, not a promise of nonzero force. No inherited .001m deadband or new universal epsilon. Cosmetic slack uses length-matched sag, not physics.
7. Cut contract: proposed enum command CutRope with a typed span payload; route/span IDs are compiled capability identities, not body IDs or package names. Running/Paused only; other phases return WrongPhase and active-operation rules retain Busy. ExpectedRevisionKind.Any follows ConfigureElectrical, while session/epoch/cadence/projection still refuse stale installations. Applied receipt means queued input, clearly shown as Cut queued; only the next successful committed step establishes severing. Paused waits for Step/Resume. Thus no artificial tick advance or special zero-time solver path is needed. A pending or already cut route returns RopeAlreadyCut; absent span returns UnknownRopeSpan. An entire tensile route releases at its selected span; repeated cosmetic shredding is not added. Reset clears pending/cut state. Save remains Building-only and stores authored edges only.
8. Committed route integrity (Intact/Cut) is distinct from Open/Slack/Taut; include selected cut-span identity for ownership/art. Presentation consumes committed data; a one-shot cut event is unnecessary unless a real cue needs it, in which case use the existing ring.
9. Extend the existing connection presenter, with persistent reusable segment/knot geometry. A deterministic slack polyline may approximate sag using a bounded visual subdivision rule; it must match length to a justified visual allowance and remain finite for vertical/coincident endpoints. This is a cosmetic tessellation choice, not a scene-population cap. Current Godot mesh rendering is reused; no custom WebGL/WebGPU instancing qualification is claimed.

Game-grade proof: choose bounds from visible load support, clearance and displacement over the play window before observing output. For the hanging fixture, allow deformation small relative to the load and its clearance rather than historical millimetre pins; assert no cumulative fall and supported placement over time. Use analytic mass-weighted impulse and no-created-work controls at the local constraint seam, and broad meaningful swing/energy behavior at scene scale. Run until named outcomes with generous game-second stall guards; only the queued-cut commit boundary is timing-contract exact. No new precision audit is required.

Later owners:10.2c supplies finite-radius pulley tangents/arcs, guide presentation and actual two-pulley equal/unequal counterbalance;10.3 Lever rope interaction;10.4 comprehensive pendulum/mass-ratio qualification;10.5 Trampoline tether/rebound. Basic 3D straight-rope correctness is still required now. The complete named catalogue, all150levels, assistance, browser and release gates remain open.

## Verification

- Matching incremental Debug builds of affected main/Core/worker/tool closure, zero warnings/errors; affected tests use --no-build --no-restore and the real shared native harness.
- Positive/control/boundary checks through authoring, both codecs, actual solver, committed reads, declared/connection art and Run/Pause/Step/Reset/save/load. Test .05/200 admitted, zero and outside values refused; parameter extremes, multiple ropes, growth, delayed/failed candidate ownership and legacy/unknown refusal.
- Strict TypeScript normal build and affected shipped-output node checks when glue/schema consumers change. No browser suite provision/run.
- Coordinator performs one final complete local solution round with the five matching compiler captures per docs/delivery-workflow.md#local-test-authority, exact Node26.12.1, generated browser-only deferral and TreatNoTestsAsError. Zero failures/skips; reuse unchanged captures and proof.
- Scoped source/membership/deletion/link checks and actual package-free Core guards. Retired source patterns: output→input-only connection assumption, implicit electrical catch-all, constant-only socket position consumer path and edit-only connection artwork ownership. Replace those callers together; no retained old-name bridges.
- Same fresh implementer handles fixes; five independent Auto layers, affected re-review, normal hooks and exact-SHA approval precede publication. No CI waiting; preserve failed attempts and qualification limits.

## Implementation handoff — 30 October 2026

Implementation candidate on published baseline `71135ecd0109b8d5eba0a9f4b944e2ffbefceedd`; independent review, complete local round and publication remain pending.

- Added the isolated RopeAnchor package, the Weight's mass-bound Tie, typed bidirectional rope authoring and measured persistent length. Graph admission canonicalizes undirected routes and rejects duplicate/cyclic/branching and unsupported complete guided routes.
- Added generic compiled tensile rows, candidate-owned impulse/cut state, shared constraint impulse mechanics and committed attachment/state reads. The existing soft solver schedule applies tension only; collision remains independent.
- Added typed queued Cut through codec, executor, worker/client and contextual controls. Pending intent is consumed only after owned commit. A failed Step installation now aborts its own installation reservation so retry can proceed; superseded owners cannot abort newer reservations.
- Added retained rope/knot artwork, deterministic cosmetic slack geometry, committed cut presentation and queue reconciliation. Updated authoring/runtime schemas, generated C#/TypeScript and affected consumers together; no compatibility path is retained.
- Updated current element, roadmap and architecture views while preserving later pulley/pendulum/integration owners and browser/release limits.

Affected proof:

| Boundary | Evidence |
| --- | --- |
| Native production authoring, solver, lifecycle and codecs | `CuriousContraptions.tests/TestResults/rope-round-5.trx`: **363 passed, 0 failed, 0 skipped**, 31 seconds. Includes new RopeTests, real native Workshop connection/mass/reconnect/Undo/Run/Reset test, existing Weight/package parity, wire/save boundary, prismatic and harness checks. |
| Package discovery, isolation, locator and bundle validation | `tools/BundleElements.Tests/TestResults/rope-bundle-round-1.trx`: **208 passed, 0 failed, 0 skipped**. |
| Strict shipped TypeScript | `npm run build:runtime` passed; `node --experimental-vm-modules --test .runtime-build/tools/runtime/workshop-observation.test.js`: **15 passed, 0 failed, 0 skipped**. Output retained in implementation tool transcript. |
| Matching build | `dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -v:q`: zero warnings/errors; tools test project incremental Debug build also zero warnings/errors. Build output retained in implementation tool transcript. |
| Scoped consistency | `git diff --check` clean; RopeAnchor references only Abstractions and Abstractions.Mechanical. Generated codecs use pinned flatc25.2.10; Core explicit runtime inventory includes the generic tensile sources. |

Native final command (from this worktree; coordinator owns the unfiltered complete round):

```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~RopeTests|FullyQualifiedName~WeightTests|FullyQualifiedName~BundlePortsTests|FullyQualifiedName~WorkshopFormatTests|FullyQualifiedName~WorkshopSaveTests|FullyQualifiedName~WorkshopSaveRejectionTests|FullyQualifiedName~WorkshopWireDecodeBoundaryTests|FullyQualifiedName~WorkshopPhysicsLawPrismaticTests|FullyQualifiedName~CompiledSceneBoundaryTests|FullyQualifiedName~WorkshopIntegrationHarnessTests|FullyQualifiedName~WorkshopIntegrationStartupTests|FullyQualifiedName~PackageRuntimeParityTests' --logger 'trx;LogFileName=rope-round-5.trx' -v:minimal
dotnet test tools/BundleElements.Tests/BundleElements.Tests.csproj --no-build --no-restore --logger 'trx;LogFileName=rope-bundle-round-1.trx' -v:minimal
```

The test matrix covers supported/slack/free loads, inward/coincident controls, 3D swing, unequal dynamic reactions and reversed endpoints, collision with slack/cut ropes, independent routes and demand growth, static feasibility, graph rejection, .05/200m bounds, malformed commands/reads/bindings, cut discard/retry, exact authored Reset and save/load rerun. Gameplay bounds derive from load size/clearance; isolated momentum and kinetic-energy controls exercise the projected row without scene pins.

Failed attempts are retained in raw TRX: round1 exposed stale schema field indices; round2 passed102/0/0; round3 passed351/354 with three missing anchor package-fixture placements; round4 passed362/363 with missing anchor palette expectation. Those fixtures now cover the new declared package. Earlier focused checks exposed omitted save length prevalidation, command root shape and the failed-Step reservation release; each was repaired and covered by the final run. The first node invocation lacked the required VM modules flag; the documented invocation passed.

No browser suite, mutant, local Release worker build or web publish ran. Pulley wraps/counterbalance, broader pendulum ratios, Lever/Trampoline interactions, detailed performance/device qualification and actual Chrome proof remain at their named later gates. This handoff is affected implementation evidence, not independent approval or release qualification.

### Coordinator round 1 repairs — 30 October 2026

The coordinator's complete local round found **81 failures / 5,086 tests, 0 skipped**; its original log and five TRX files remain unchanged under `.tmp/rope-coordinator-round1/`. This supersedes any implication that the first affected handoff completed whole-solution acceptance.

Repaired the missing RopeAnchor package/catalogue declarations in metadata scanning, impact membership, trim-analyzer project admission, registry/palette/icon/capacity fixtures and executed-interaction mapping. Historical command envelopes now assert structural `VerifierRejected` while retaining their fixture bytes and current save-field controls; current runtime field boundaries remain owned by CompiledSceneBoundaryTests. Cancel's missing-target test now clears the correct target vtable entry and keeps its valid identity/session controls. The solver guard recognizes exact generic tensile identity/state and contact-anchor concepts, scans both new solver/helper files and retains positive package-dispatch controls.

Restored all sixteen accidentally truncated observation snapshot layouts exactly; a read-only comparison confirms every unchanged layout and original field order survives, with only the two new rope types and four intended existing-record additions. Removed the obsolete authored goalEventSource helper/caller; the existing Receiver replacement case still asserts current goal body/target and committed capture sensor identity. Added current connection-struct arguments to its deferred fixture without running that fixture.

The real native Workshop presenter test now asserts gold knots at authored/committed endpoints, a joined rendered cylinder path with a 1cm visual allowance, motion following committed reads, queued-cut artwork unchanged, cut artwork and disabled status after Step, exact Reset knot restoration, retained node reuse and actual teardown. Open-route dashed art is required now at the generic declared presentation boundary; the matrix closure below executes incomplete rows and checks their actual native meshes. Playable Guide/Pulley authoring and finite-radius wraps remain owned by10.2c.

Final affected repair proof (matching incremental builds, zero warnings/errors; all commands use `PATH="$PWD/.tmp/node-v26.12.1-darwin-arm64/bin:$PATH"`):

- `CuriousContraptions.tests/TestResults/rope-repair-round-2.trx`: **277 passed, 0 failed, 0 skipped**,13seconds. Filter covers RopeTests, native startup/presenter, five legacy refusal suites, registry/package/capacity, material catalogue, executed-interaction mapping, Cancel, solver generic-name controls and existing compiler captures via `CSHARP_STATIC_GATE_BINLOGS="$PWD/.tmp/rope-coordinator-round1"`.
- `tools/Coverage.Tests/TestResults/rope-repair-coverage-2.trx`: **77 passed, 0 failed, 0 skipped**.
- `tools/E2eRunner.Tests/TestResults/rope-repair-tools-2.trx`: **192 passed, 0 failed, 0 skipped**, using the central `deferred-filter` output. No deferred browser/toolchain test is part of this required result.
- `npm run build:runtime` passed; `git diff --check` clean. Historical inline fixture-byte preservation checked across all five repaired suites.

Failed repair attempts remain: native round1 **276/1/0** (last icon count), Coverage round1 **76/1/0** (last admitted count), two compile attempts enforcing async test analyzer rules before simplifying the interaction mapping to its established form.

Execution mistake, explicitly non-gate: the first E2eRunner repair invocation omitted the central deferral filter and ran **207 passed / 1 failed / 0 skipped** (`rope-repair-tools-1.trx`). This included deferred TypeScript/toolchain tests, including their existing temporary altered-accessor fixtures, contrary to the current deferral policy. No Chrome ran. It was not repeated; the correctly filtered required run above passed. Its typecheck failure exposed the fixed connection arguments and obsolete goal accessor, plus an unchanged deferred defect: `tools/e2e/cat-005.test.ts:417` supplies `settings` to MockCommandParams, which already lacks that property in published baseline `71135ecd`. That pre-existing cadence-protocol test awaits its post-catalogue browser owner; no compatibility property was added and no browser/typecheck qualification is claimed.

The candidate remains pending the coordinator's matching final full round and five Auto review layers; no implementation Pass or publication is claimed here.

Repair commands, run from the implementation worktree:

```sh
export PATH="$PWD/.tmp/node-v26.12.1-darwin-arm64/bin:$PATH"
dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -v:q
dotnet build tools/Coverage.Tests/Coverage.Tests.csproj --no-restore -v:q
dotnet build tools/E2eRunner.Tests/E2eRunner.Tests.csproj --no-restore -v:q
CSHARP_STATIC_GATE_BINLOGS="$PWD/.tmp/rope-coordinator-round1" dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~RopeTests|FullyQualifiedName~WorkshopIntegrationStartupTests|FullyQualifiedName~PreConversionLaunchSaveTests|FullyQualifiedName~DeclaredOrientationTests|FullyQualifiedName~DeclaredMemoryTests|FullyQualifiedName~DeclaredOscillatorTests|FullyQualifiedName~DeclaredContactLoadTests|FullyQualifiedName~PartRegistryTests|FullyQualifiedName~BundleElementIdTests|FullyQualifiedName~WorkbenchCapacityTests|FullyQualifiedName~BasketballResourceTests.TheBundleAndSceneUseDeclaredNumericMaterials|FullyQualifiedName~CatalogueIntegrationTests.EveryAdmittedPartHasAnExecutedNamedInteraction|FullyQualifiedName~WorkshopWireTests.CancelWireRetains|FullyQualifiedName~WorkshopPhysicsLawTests.TheShippedSolver|FullyQualifiedName~WorkshopPhysicsLawTests.TheGeneralScan|FullyQualifiedName~CSharpStaticGateTests.EveryLoggedCompile' --logger 'trx;LogFileName=rope-repair-round-2.trx' -v:minimal
dotnet test tools/Coverage.Tests/Coverage.Tests.csproj --no-build --no-restore --logger 'trx;LogFileName=rope-repair-coverage-2.trx' -v:minimal
dotnet test tools/E2eRunner.Tests/E2eRunner.Tests.csproj --no-build --no-restore --filter "$(dotnet tools/E2eRunner/bin/Debug/net10.0/E2eRunner.dll deferred-filter)" --logger 'trx;LogFileName=rope-repair-tools-2.trx' -v:minimal
npm run build:runtime
git diff --check
```

### Final matrix closure — 30 October 2026

Coordinator round2 was intentionally cancelled once the remaining matrix gaps were identified. Its partial log/TRX remain unchanged; the four completed tool projects passed17+77+208+192 tests with zero skips, but native execution was incomplete and is not a Pass. The final candidate still requires a complete matching coordinator round.

The single current renderer now takes explicit committed span reads; all three production callers pass `World.WorkshopRead.Presentation.Ropes`. The declared incomplete/complete fixture advances the production engine, validates the candidate ABI and reads presentation before calling that same renderer. No fake installed catalogue, Guide package, alternate renderer or browser is used.

| Matrix row | Actual executed checks |
| --- | --- |
| Authoring | `RopeAuthoringMeasuresSocketsAndRefusesBeforeUndoThenReconnectsAtChangedMass`; `MeasurementPersistsAcrossMovesAndMassEditsButReconnectMeasuresAgain`; `TieTracksTheActualMassScaledEye`; length boundary, malformed binding and `OpenGuideTopologyIsIncompleteAndCyclesOrBranchesRefuse` checks. The topology check also refuses complete guided geometry. |
| Hanging / slack | `ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls`, `SlackRopeDoesNotAccelerateTheFallingLoad`, `InwardMotionAndCoincidentSlackDoNotReceiveConstraintImpulse`, `IncompleteRowsDiscardStaleTensionWhileCompleteRowsReact` (both controls) and `ThreeDimensionalReleaseSwingsWithoutLosingItsTether`. |
| Reaction | `UnequalDynamicLoadsReactToASeparatingFallAndReversingTheEdgePreservesIt`, `TheLengthRowHasMassWeightedReactionAndNeverCreatesKineticEnergy`, `SlackAndCutConnectionsKeepOrdinaryLoadContact`, `TensileAdmissionRejectsMissingParticipantsAndUnsupportedMotion` with valid scene control, plus static-overlength and complete-guide refusal. |
| Runtime cut | `CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` now checks both pending and committed duplicate refusal with unchanged read; unknown IDs and independent routes covered. Native `RopePresenterUsesCommittedKnotsAndCutStateAndRetainsNodesUntilTeardown` checks queued and committed controls/art. |
| Cancelled candidate | The same cut test rejects a prepared Step installation, observes unchanged committed state, then retries successfully with the retained cut intent. Existing installation owner checks remain covered in repair-round2. |
| Restoration | Cut test restores exact authored pose/rotation/uncut links and reruns; `DemandBeyondInitialReservesPreservesIndependentRopesAndSaveLoad` reruns loaded authored routes; outer native authoring test exercises save/Reset. Existing save/wire refusal controls remain in repair-round2. |
| Presentation | `RopePresenterUsesCommittedKnotsAndCutStateAndRetainsNodesUntilTeardown` checks actual knots, connected mesh path, committed movement/cut, Reset, reuse and teardown. `DeclaredIncompleteRopeReadsProduceDashedArtworkAndCompleteReadsRemainSolid` checks actual engine-produced Open/Taut reads through the same native renderer. `SlackArtworkMatchesLengthAndRemainsFiniteForVerticalAndCoincidentEndpoints` checks cosmetic path length and finite endpoints. |
| Demand | `DemandBeyondInitialReservesPreservesIndependentRopesAndSaveLoad` exercises24 independent routes/48 bodies, identities, isolated cutting and loaded rerun. Existing WorkbenchCapacity/ABI/refusal checks passed in repair-round2; no new arbitrary cap is introduced. |

Final affected run: `CuriousContraptions.tests/TestResults/rope-matrix-round-2.trx`, **40 passed, 0 failed, 0 skipped**,2seconds. It includes all RopeTests, native startup/presenter tests and the existing activation/electrical connection-choice control. Unchanged broader repair proof remains in `rope-repair-round-2.trx` (**277/0/0**), Coverage (**77/0/0**) and filtered tool (**192/0/0**) records above. First matrix run is preserved as `rope-matrix-round-1.trx` (**39/1/0**): the new valid admission fixture initially omitted mandatory dynamic collision geometry; retaining the compiled colliders/materials fixed that fixture and made its negative controls meaningful. Matching incremental build completed with zero warnings/errors; `git diff --check` is clean.

```sh
PATH="$PWD/.tmp/node-v26.12.1-darwin-arm64/bin:$PATH" dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -v:q
PATH="$PWD/.tmp/node-v26.12.1-darwin-arm64/bin:$PATH" dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~RopeTests|FullyQualifiedName~WorkshopIntegrationStartupTests|FullyQualifiedName~PalettePlacementTests.ImpactContactConnectionChoicesPreserveDomainCancelAndOneUndo' --logger 'trx;LogFileName=rope-matrix-round-2.trx' -v:minimal
```

### Complete local verification — 30 October 2026

Coordinator round3 completed successfully: **5,101 passed, 0 failed, 0 skipped** (native4,607; Coverage77; WorkerAotGuard17; BundleElements208; required E2eRunner tooling192). Native duration21minutes3seconds. Original console and all five TRX files are retained under `.tmp/rope-coordinator-round3/`. Three affected compiler captures passed with zero warnings/errors; the two unchanged file-tool captures were reused. SDK10.0.403, pinned Node26.12.1, Debug only, central browser deferral only. Coordinator inspected the complete candidate diff and verified the eight-row matrix against actual passing TRX entries. Five independent Auto layers, normal hooks and publication remain pending.

### Review fixes and affected re-review — 30 October 2026

The same implementer corrected all four patch groups in five files: WorkshopConnections, WorkshopMessages, WorkshopWireFlatBuffers, RopeTests and WorkshopIntegrationHarnessTests. Rope geometry/state/length now comes only from matching committed reads; missing observations produce typed visible refusal and hidden artwork. Attachment validation binds static declared and dynamic committed poses to sockets with a1cm visual allowance, including scene-bearing response decode. Actual wire tests exercise missing/zero/unexpected cut payloads and malformed/translated observations. Physical-law controls use admitted declarations, the public ABI and real engine schedule; internal solver-stage/row assertions were removed.

Matching incremental build passed with zero warnings/errors. Scoped proof: `CuriousContraptions.tests/TestResults/rope-review-fixes-round-1.trx`, **167 passed, 0 failed, 0 skipped**,4seconds. Exact commands:

```sh
PATH="$PWD/.tmp/node-v26.12.1-darwin-arm64/bin:$PATH" dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-restore -v:q
PATH="$PWD/.tmp/node-v26.12.1-darwin-arm64/bin:$PATH" dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~RopeTests|FullyQualifiedName~WorkshopIntegrationStartupTests|FullyQualifiedName~CompiledSceneBoundaryTests|FullyQualifiedName~WorkshopWireTests|FullyQualifiedName~WorkshopWireDecodeBoundaryTests|FullyQualifiedName~PalettePlacementTests.ImpactContactConnectionChoicesPreserveDomainCancelAndOneUndo' --logger 'trx;LogFileName=rope-review-fixes-round-1.trx' -v:minimal
```

All five original independent reviewers re-inspected their affected findings against frozen `.tmp/rope-coordinator-round4/candidate.diff` and the actual passing TRX. Forward-Only closed the fallback finding; Edge Case closed attachment ownership; Verification Gap closed incoming codec proof and internal assertions; Blind Hunter and Intent Alignment closed supported-interface and incoming-boundary divergences. No residual affected finding was reported, and reviewers did not rerun builds/tests or create mutants. Original task transcripts retain their closure reports.

### 2026-10-30 — Affected follow-up review
- verdicts: 0 new findings — high 0, medium 0, low 0, false 0, maybe-false 0.
- carried pass1 decisions: all four patch groups independently closed (one high, three medium groups); four permitted/descriptive assessments remain rejected on their recorded evidence. No deferred entry.
- follow-up recommendation: false. This follow-up introduced no patch or unresolved specific review risk; original high-risk fallback was independently closed. Full coordinator round4 finished (5,110 passed, 0 failed, 0 skipped); normal hooks and publication remain pending.

## Auto Run Result

Finalized 30 October 2026. Local implementation and independent review pass; normal commit hooks, exact committed-SHA approval and publication are the remaining publication steps. The Auto workflow status is done; this is not CI, deployment or browser qualification.

Implemented the isolated RopeAnchor package, Weight mass-bound Tie socket, generic tensile soft constraint law, queued runtime cut mechanics, committed presentation artwork, and authoring/runtime FlatBuffer codecs.

Changed file groups:
- `CuriousContraptions.Abstractions/{PortDescriptor.cs,SimulationDomain.cs}`: admit Bidirectional, Rope domain, Tie/Load sockets, and generic parameter-driven socket position bindings.
- `CuriousContraptions.Bundle/{BundleSchema.cs,BundleValidator.cs}`: validate port position bindings and attachment roles in bundle schema.
- `CuriousContraptions.Core/RuntimeSources.props`: include generic tensile constraint sources and declarations in Core runtime.
- `CuriousContraptions.Simulation/{ConstraintBodyImpulse.cs,TensileConstraintSolver.cs,PrismaticConstraintSolver.cs,Program.cs,WorkshopPhysicsEngine.cs}`: shared body impulse calculations, projected soft tensile constraint solver, schedule integration, and warm-start/relax logic.
- `CuriousContraptions.tests/RopeTests.cs`: comprehensive unit and integration tests for rope physics, authoring, cut, restitution, demand growth, and boundaries.
- `CuriousContraptions.tests/{BasketballResourceTests.cs,BundleElementIdTests.cs,BundlePortsTests.cs,CSharpStaticGateTests.cs,CatalogueIntegrationTests.cs,CompiledSceneBoundaryTests.cs,DeclaredContactLoadTests.cs,DeclaredMemoryTests.cs,DeclaredOrientationTests.cs,DeclaredOscillatorTests.cs,PackageRuntimeParityTests.cs,PalettePlacementTests.cs,PartRegistryTests.cs,PreConversionLaunchSaveTests.cs,WeightTests.cs,WorkbenchCapacityTests.cs,WorkshopFormatTests.cs,WorkshopHintTests.cs,WorkshopIntegrationClient.cs,WorkshopIntegrationHarness.cs,WorkshopIntegrationHarnessTests.cs,WorkshopPhysicsLawPrismaticTests.cs,WorkshopPhysicsLawTests.cs,WorkshopSaveRejectionTests.cs,WorkshopSaveTests.cs,WorkshopWireTests.cs}`: update tests for new RopeAnchor package, tie sockets, connection counts, and boundary checks.
- `CuriousContraptions.web/wwwroot/workshop-observation.ts`: strict TypeScript observation decode for rope spans and cut state.
- `TODO.md`: current slice status, next checks, and execution plan updates.
- `_bmad-output/implementation-artifacts/spec-10-2b-rope-tie-load.md`: specification, triage log, verification, and auto run result.
- `_bmad-output/implementation-artifacts/epic-10-context.md`: compiled epic 10 context.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`: sprint tracking for story 10.2b.
- `_bmad-output/planning-artifacts/epics.md`: update story 10.2b status and child ownership.
- `docs/architecture/element-package-isolation.md`: record RopeAnchor package isolation status.
- `docs/planning/elements/{CAT-058-rope_anchor.md,CAT-067-weight.md,EL-205-rope.md}`: update operative decisions, ports, and acceptance for rope anchor, weight tie, and straight rope.
- `docs/planning/invest/named-elements.md`: update EL-064/EL-205 delivery notes.
- `elements/RopeAnchor/{RopeAnchor.csproj,RopeAnchorElementDeclaration.cs,rope_anchor.tscn,icon.svg}`: new RopeAnchor package files and visual scene.
- `elements/Weight/{WeightElementDeclaration.cs,weight.tscn}`: declare Tie port and locator bound to eye position.
- `engine/MachineWorld.Gpu.cs`: wire rope connection presentation and observation reads.
- `engine/gpu/{BrowserWorkshopClient.cs,ElementPortCatalogue.cs,IWorkshopClient.cs,PhysicsDeclarations.cs,PhysicsGpuAbi.Header.cs,PhysicsGpuAbi.Tensile.cs,PhysicsGpuAbi.cs,PhysicsPresentationRead.cs,PhysicsWorldLayout.cs,RopeGeometry.cs,RopeTopology.cs,SupplyRoute.cs,TensileSpanDeclaration.cs,WorkshopAuthoringFlatBuffers.cs,WorkshopAuthoringGenerated.cs,WorkshopCommandExecutor.cs,WorkshopConnections.cs,WorkshopMessages.cs,WorkshopPhysicsCompiler.cs,WorkshopSavePrevalidation.cs,WorkshopSimulation.cs,WorkshopSocket.cs,WorkshopWireFlatBuffers.Compiled.cs,WorkshopWireFlatBuffers.cs,WorkshopWireGenerated.cs}`: engine authoring, compilation, wire protocol, connection topology, and ABI for ropes.
- `schemas/{workshop_authoring.fbs,workshop_wire.fbs}`: authoring connection length and wire protocol schemas for tensile spans and cut command/read.
- `tools/Coverage.Tests/CurrentCatalogueTests.cs`: include RopeAnchor in admitted catalogue.
- `tools/E2eRunner/ImpactRules.cs`: add RopeAnchor impact mappings.
- `tools/e2e/{cat-062b.test.ts,run-reset-snapshot.ts,unit/run-reset-snapshot.test.ts,workshop-wire.ts}`: update e2e tooling for new wire formats.
- `tools/runtime/workshop-observation.test.ts`: test observation decoding of rope records.
- `ui/{WorkshopConnections.cs,WorkshopGpu.cs,WorkshopGuidance.cs}`: editor connection handling, mesh presentation, and guidance updates.

Review outcome: 11 reported findings (high 1, medium 6, low 0, false 4).
1. High [patch]: Forward-Only fallback in WorkshopConnections.RefreshConnectionArtwork (renderer selecting authored nodes when committed read absent) — deleted fallback; single committed renderer with typed visible refusal.
2. Medium [patch]: Blind Hunter internal solver tests bypass supported simulation boundary — replaced internal solver tests with admitted runtime motion controls.
3. Medium [patch]: Verification Gap malformed cut shapes lack incoming decoder proof — added incoming wire decode refusal tests and controls.
4. Medium [patch]: Verification Gap internal rows/accumulated impulse asserted — removed solver-internal assertions, validated via public ABI and engine schedule.
5. Medium [patch]: Edge Case Hunter rope attachments not bound to committed owner poses — added pose/socket validation with 1cm allowance.
6. Medium [patch]: Intent Alignment physical guarantees stop at internals — grouped with supported-interface physics tests.
7. Medium [patch]: Intent Alignment malformed input tests producers rather than incoming boundaries — added decoder tests.
8. 4 false/descriptive findings rejected on recorded evidence.

All four patch groups were independently re-reviewed and closed in affected re-review; follow-up review produced zero new findings.

followup_review_recommended: false. This is a converged follow-up pass (patched high 0, medium 0), and actual affected re-review is complete. No specific unverified review risk remains. deferred: [].

Verification: coordinator complete solution run (round 4) exited 0 with **5,110 passed, 0 failed, 0 skipped** across all 5 test projects (CuriousContraptions.tests 4,616, WorkerAotGuard.Tests 17, Coverage.Tests 77, BundleElements.Tests 208, E2eRunner.Tests non-browser 192). Native suite duration 20m 31s. Full console log and TRXs retained in `.tmp/rope-coordinator-round4/` (`full-local.log`, five `results/*.trx`). Scoped review-fix proof was 167/167/0/0 (`rope-review-fixes-round-1.trx`). Shipped strict TypeScript compiled and verified via node (15/15/0/0). Matching incremental Debug build reported zero warnings, zero errors.

Evidence identity: baseline published main `71135ecd0109b8d5eba0a9f4b944e2ffbefceedd`; final reviewed runtime candidate `.tmp/rope-coordinator-round4/candidate.diff`; complete console/TRXs `.tmp/rope-coordinator-round4/full-local.log` and `results/`; compiler captures in that directory. The resulting normal-hook commit SHA is the canonical candidate identity for independent approval.

Remaining qualification: normal hooks and exact-SHA approval precede push; CI/artifacts/deployment remain unverified. Browser/E2E remains explicitly deferred until every named catalogue element passes native proof; performance/device/retention qualification retains its named later gates.

## Publication receipt — 30 October 2026

Published and independently verified on remote main: `d2862ebea41b4b9036ba6d7367383ec62d93f811`. Full local verification passed 5,110 tests; unchanged normal commit hooks passed 127 tests; both had zero failures/skips. Compiler builds and Anvil reported zero warnings/errors. Exact committed SHA approved, verified the clean tree and reviewed/tested candidate match, then independently confirmed remote main after the successful push. Terminal publication Pass under the owner's local-authority policy; CI/artifact/deployment and browser qualification remain unverified.

Original full-test/compiler/hook evidence remains in the `story-10-2b-rope-tie-load` worktree at `.tmp/rope-coordinator-round4/` (`full-local.log`, five `results/*.trx`, compiler logs and normal-hook commit log). This receipt is appended by the following slice; the approved commit is not amended.

