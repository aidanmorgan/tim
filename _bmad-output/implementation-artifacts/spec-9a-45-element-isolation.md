---
title: '9A.45 — compile-time element isolation'
type: 'refactor'
created: '2026-10-29'
status: 'done'
baseline_revision: '15cf84969870cddefc7c91b6a539d7144dc47f76'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
warnings: [oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** Story9A.45 requires build-enforced element isolation. Workers still compile authoring/bundle-dependent sources, named PartId members select elements, and straight Pipe retains a special instance/codec path.

**Approach:** Main-owned authoring compiles one typed capability installation consumed by the element-free Core runtime and both workers. Convert Pipe to existing generic declarations, remove named identities and enforce package isolation in the normal bundle build.

## Boundaries & Constraints

**Always:** Preserve every existing element, placement/configuration/snapping, commands, typed refusals, Run/Pause/Step/Reset/save/load, events, presentation and 9A.24 observation metadata. Both workers reference a real element-free Core; neither directly nor transitively references authoring/catalogue/lessons/bundle loading. One production compiler, runtime codec and executor also serve native verification. Counts remain logical; mutable containers use category defaults and geometric growth, without arbitrary limits. Game values remain f32; integer identity and deliberate timing remain exact. Keep source requirement IDs and future qualification.

**Never:** Worker authoring compilation or bundle loading, authored blobs in runtime payloads, a parallel native implementation, compatibility overloads/aliases/migrations, format versions, output hash or byte-identity oracles, solver redesign, new elements, renamed generic capabilities solely to satisfy lexical scanning, permanent known-violation lists, browser gates, mutants, CI monitoring, local Release worker build or web publish.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Build isolation | Generated bundle and actual production source membership | Normal build rejects package-specific literals, dispatch and identifiers with file/line; generic capabilities compile | Nonzero build result; no old exception ledger |
| Compiled installation | Main-authored mixed capabilities, empty scene and changed cadence | Both workers consume the same typed compiled context; no element lookup in Core | Invalid rows/references/enums/nonfinite data refuse atomically |
| Admission | Valid, invalid, cancelled/superseded or failed installation | Only accepted candidate changes committed scene/revision/epoch and presentation | Existing typed refusal and cleanup, no partial publication |
| Playback | Run/Pause/Resume/Step, cadence change, Reset | Existing timing and event semantics; same-world presentation retention; Reset clears runtime state and restores authored state | Existing lifecycle refusals remain |
| Pipe | Default/min/max length, resize, mouth snap, joined bore route | Generic parameter drives art, collars, physics and mouths consistently; save/load and Undo preserve values | Out-of-range/missing/nonfinite parameters reject; no old-format migration |
| Observation | Captures followed by edits, Reset or cadence changes; delayed receipts | Retained capture projects its original authored puzzle/parameters/connections by exact scene association; cursors remain independent | Missing/foreign association marks observation incomplete without faulting gameplay |
| Storage | Scene/diagnostic demand exceeds initial capacities | Growth preserves actual data and ownership without count ceilings | Checked storage exhaustion refuses before commit |

</intent-contract>

## Code Map

- Authority: epics.md Story9A.45 and stabilization row; architecture target invariants1–9 and Current status. Read source acceptance, not old archive prerequisites. Pipe preserves current CAT-048 declaration (length1–8m/default3.6m, real hollow shell/collars/mouths), sequence-task-066/067 placement/admission behavior and EL-071 identity; future water/large-bore/accelerator variants are not implemented by this refactor. Browser qualification remains deferred.
- New CuriousContraptions.Core/CuriousContraptions.Core.csproj; existing CuriousContraptions.csproj stays the main-only authoring assembly. Core owns generic declarations/physics/device, simulation/executor/router, activation/supply runtime, read validation, clocks/schedules/events, runtime codec/diagnostics and compiled presentation evaluation. Main owns construction/instances/parameters/connections, catalogue/puzzles/saves, bundle loading, declaration compilers, browser client and external observation projection. No extra authoring project is needed.
- CuriousContraptions.Simulation/CuriousContraptions.Simulation.csproj and Animation.Worker counterpart currently link engine/gpu wholesale and PartId, reference Bundle/tool and embed bundle (Simulation also puzzles). Remove those links/references/resources; both reference Core. Worker Program files retain platform hosts only. Update solution, web/publish/AOT ownership, explicit source memberships and current tool references.
- engine/gpu/PhysicsDeclarations.cs PhysicsSceneDeclaration is the physics reuse boundary, not the whole installation. Include activation nodes/edges/passage sensors/switched-contact owners/substeps, compiled presentation tracks (target/owner/binding ordinal/PresentationBinding/activation declaration/evaluated source capacity), runtime editable-source permissions and owner/capture admission metadata. Preserve declarative supply edges and switched-contact inputs; do not reverse-engineer adjacency.
- engine/gpu/ActivationNetwork.cs WorkshopActivationCompiler (near846) and SupplyRoute.cs topology compilation mix authoring with runtime. Extract compilers to main; leave generic network/graph runtime in Core. Split mixed files rather than moving their forbidden dependencies wholesale.
- engine/gpu/WorkshopMessages.cs command/response/ValidatePresentationOwners currently carry or consult construction. Runtime commands/installations carry compiled scene data and validate compiled ownership/capture/supply logic. Main APIs may accept authored construction but Core cannot depend on it.
- engine/gpu/BrowserWorkshopClient.cs ExecuteCore and EnsureInstalledScene; BrowserWorkshopClient.DeclaredPresentation.cs: prepare the main compilation before runtime dispatch and reuse compiled target mappings. Retain authored candidate ownership separately until accepted installation/ACK. Preserve command identity and failure semantics.
- CuriousContraptions.Simulation/WorkshopGpuDevice.cs Admit currently invokes physics/activation compilers. Admit compiled data instead; preserve staging, sparse event preparation and joint commit/discard. Move actual generic runtime implementation into Core so native tests reference the same assembly.
- engine/gpu/WorkshopSimulation.cs ConfigureElectrical/BistableNodes/ValidateInitialRead/ValidateAdvancedRead currently consult construction and DeclaredPhysics. Use compiled permissions, activation/physics rows and retained compiled state. Initialize gets a compiled empty scene, cadence receives a newly compiled candidate, Reset re-admits retained compiled state.
- CuriousContraptions.Simulation/Program.Schedule.cs installation prepare and CuriousContraptions.Animation.Worker/Program.cs bundle loading/evaluator setup: send compiled context through the existing typed prepare/receipt/commit protocol; animation never resolves a package.
- engine/gpu/WorkshopDeclaredPresentation.cs constructor/rebind and target enumeration: main compiles tracks, Core evaluates them. Same-world playback retains consumed/in-progress channels; Reset starts fresh. Preserve both clock domains and one-way data flow.
- schemas/workshop_wire.fbs, engine/gpu/WorkshopWireFlatBuffers*.cs and canonical generated bindings: separate runtime capability schema/codec ownership from authored construction/save schema/codec ownership, with pinned flatc and one decoder per boundary. Core generated bindings must not contain authored records by transitive accident. Main-only save/authoring encoding stays complete; no opaque authored runtime blob.
- engine/gpu/WorkshopDiagnostics.cs WorkshopCaptureDiagnostic, Simulation/WorkshopTrace.cs Begin, WorkshopWireFlatBuffers.Observation.cs and WorkshopObservationProjection.cs currently carry an authored WorkshopCommand. Runtime capture identifies the admitted compiled scene/run; main-owned observation associates the retained authored snapshot and preserves the existing external construction DTO. Keep earlier capture metadata after later edits/Reset/cadence; missing association is explicit incompleteness.
- elements/Pipe/PipeElementDeclaration.cs already defines length defaults/ranges, parameterized shell/collars, presentation bindings and mouth positions ±(length/2+.09). Reuse it without new Pipe helpers. Delete engine/gpu/WorkshopPipe.cs and uid, WorkshopInput.Pipe/PipeBend45/PipeBend90/Funnel factories, generic-Pipe refusal in WorkshopInstances, DeclaredPhysics typed parameter arm.
- engine/MachineWorld.Gpu.cs CaptureInstance uses a pipe switch: use existing CaptureParameters. engine/PartDefinition.cs Mouths and TubeMouthSnap.cs TubeMouthMover use generic declared parameters; ui/WorkshopGuidance.cs passes preview/placed parameters without PartId.Pipe checks.
- WorkshopWireFlatBuffers.cs typed length encoder/decoder/RequiredLength/CarriesTypedField, WorkshopSavePrevalidation.cs pipe branch and schema instance length field: delete together and regenerate; required generic parameters reject unsupported old inputs.
- engine/PartId.cs: remove all named element members. Current callers span100 C# files (90 tests,5 tools,4 engine,1 UI); permitted test/tool authored inputs use opaque explicit IDs, production selects declared data. Do not recreate a named constant catalogue elsewhere.
- tools/BundleElements/Program.cs and BundleGenerator.Generate; tool csproj RunElementBundleGenerator target: run isolation after successful generation on every normal tool build, even unchanged bundle bytes. Bundle metadata IDs and package scene paths are the authority. Reuse AST/token and TS lexical mechanisms from ElementIsolationTests, moving the implementation into the tool, not copying a test scanner.
- CuriousContraptions.tests/ElementIsolationTests.cs currently scans C# at101, JS/TS at137–203 and membership at205–239; fixture has94 Known rows/112 occurrences and681 Generic rows/1269 occurrences. Delete Known/Generic per-occurrence ledgers and owner/history ratchet machinery. Package-shaped identifier/dispatch detection must reject Workshop<Package>, <Package>Settings/Dimensions/Lessons, CosmeticCurves.<Package>, named/qualified/aliased PartId access and escaped element literals, while admitting capability Clock/Counter/Spring names in generic algorithms. Comments and unrelated strings are controls.
- Scope: engine/**, ui/**, all CuriousContraptions.*/** production projects including all worker/web strict TS; exclude elements owners, native tests, tools/e2e, authored content, nonshipping tools/Coverage, docs/_bmad-output/archives with the story's reasons. Exclude generated/build output by declared ownership, never a broad source escape. Verify new Core sources are covered.
- Native WorkshopIntegrationHarness.cs and WorkshopIntegrationClient.cs: use the same main compiler → runtime codec → executor; keep authoring on the main side. Tests csproj currently source-links device/solver; replace links with actual Core reference and narrowly scoped friend access if necessary.
- Existing tests: WorkshopSimulationTests, WorkshopWireTests, WorkshopActivationAnimationTests, DeclaredPresentationTests, WorkshopObservationTests, DemandSizingLifecycleTests and editor integration; PipeLength/StraightPipe/JoinedPipe/TubeMouthSnap/ClearPipeLevel/SaveRejection/HollowElement/VisibleSolidCollider/PackageRuntimeParity/PlacementPose/PalettePlacement/PipeBend/Funnel. Migrate inputs, retain meaningful behavior and controls.
- The stabilization row also requires deleting unused TimerObservation transport: engine/gpu/WorkshopHint.cs control/ack fields and codec, WorkshopAnimationTracks.cs branches, engine/presentation/AnimationTimerSegment.cs and Animation.csproj compile item. Remove tests exclusive to that dead path with an explicit coverage mapping; preserve live declared timer presentation through WorkshopDeclaredPresentation and its production integration tests. No removal of timer gameplay or timing semantics.
- CSharpStaticGateTests, GameFloatGuardTests and build/source inventories must follow exact assembly ownership without relaxing their checks. ArchitectureDocsConsistencyTests is the actual9A.0 consistency seam.
- docs/architecture/element-package-isolation.md Current status contains stale delivered rows. Reconcile with actual source and existing published receipts; close only established implementation findings. Future MNA/catalogue capabilities and browser qualification remain distinct pending work.

## Tasks & Acceptance

**Execution:**
1. Core/main/worker projects and mixed runtime files above — establish explicit ownership, compiled scene contracts and the existing generic runtime assembly.
2. Schema/codecs, main compiler/client, runtime admission and animation — implement one compiled installation transaction across Initialize/Construct/cadence/Reset and retained observation association.
3. Pipe declaration consumers, PartId and all callers — remove special paths and obsolete schema fields while preserving gameplay and authored semantics.
4. BundleElements and isolation tests — enforce the generated-bundle-derived gate in normal builds; delete violation-list machinery and use production-scanner controls.
5. Native integration, codec/lifecycle/observation and compiled TypeScript tests — exercise every matrix row and real worker wiring; update exact compiler inventories.
6. Current architecture/spec/status/TODO — reconcile implementation findings with evidence, record9A.24 terminal receipt, retain browser/CI qualification limits.

**Acceptance Criteria:**
- Given the solution and both worker reference closures, when normal Debug builds and source membership checks execute, then a real Core owns runtime capability code and neither worker contains/references authoring/catalogue/lessons/bundle loading or wholesale engine sources.
- Given real authoring operations and mixed-capability constructions, when production main compilation, codec, executor and presentation installation execute, then intended interactions, controls, causal events, rollback and supported Reset/save/load remain correct without an alternate native path.
- Given generated bundle identities and production source inputs, when the production isolation scanner runs as part of the build, then all required forbidden shapes produce actionable file/line errors and clean/generic controls succeed; no known-violation mechanism survives.
- Given existing Pipe declarations, when length boundaries, resizing, joining and hollow routing run through generic instances, then geometry/art/mouths and authored restoration remain coherent; invalid data refuses atomically.
- Given published prior slices and actual source, when the architecture consistency test runs, then no unresolved current implementation isolation finding is mislabeled closed or retained merely because a historical status is stale.
- Given the frozen candidate, when local full verification, normal hooks and five Auto layers finish, then all required tests pass with zero skips and exact-SHA approval precedes publication. Browser/release/deployment qualification remains explicitly pending.

## Spec Change Log

- 29 October 2026, coordinator verification: valid compiled scenes exposed a pre-existing signed-16-bit absolute-vtable offset in `CuriousContraptions.FlatBuffers/FlatBufferVerify.cs`. Repair that verifier arithmetic directly, retaining bounds/alignment and malformed-input refusal; update the existing vendor integrity pin/provenance and add behavioral offset-boundary controls. This is a necessary dependency of the existing unlimited compiled-scene acceptance, not a new compatibility path or library upgrade.
- The initial complete-solution attempt was interrupted after 16 emitted failures: six large-scene codec cases, five XOR refusal-boundary cases, four current-puzzle envelope cases and one package-membership scanner-keyword false positive. Its console output is retained at `.tmp/isolation-coordinator-20261029/full-local.log`; it is incomplete failed evidence. Five compiler captures succeeded before repairs (the file-based tool invocation was corrected after unsupported serial flags); relevant captures must be refreshed after source fixes. No terminal verification or review approval is claimed.

## Review Triage Log

### 2026-10-29 — Review pass 1 (repairs pending)
- Reviewers: `/root/isolation_r1_blind`, `/root/isolation_r1_verification`, `/root/isolation_r1_edge`, `/root/isolation_r1_intent`, `/root/isolation_r1_forward`. All five layers launched before triage; concurrency limits required two waves. Reviews were read-only and overlapped the frozen candidate's full local run; approval remains contingent on complete passing verification.
- Candidate: baseline `15cf84969870cddefc7c91b6a539d7144dc47f76`, uncommitted diff `.tmp/isolation-coordinator-round2/candidate.diff`. Original layer reports are retained in their agent responses.
- verdicts: 7 findings — high 1, medium 4, low 0, false 2, maybe-false 0
- findings:
  - [medium] [patch] Blind Hunter: ordinary edits retain every compiled scene through `WorkshopObservation._authored`, including rejected and never-run candidates — `Associate` adds before dispatch and only `Retire` clears the dictionary. Release unsuccessful/superseded uncaptured associations while preserving active installations and metadata needed by delayed/retained captures; verify ordinary edit/Run/Reset lifecycle. Repair pending.
  - [medium] [patch] Verification Gap: malformed compiled presentation endpoints lack decode-boundary tests — existing malformed cases cover motion/mass/ownership/cadence, not presentation `From = NaN`. Add valid control plus command/response typed refusal and unchanged installed-state assertions. Filed evidence accepted under the layer's verification rules. Repair pending.
  - [medium] [patch] Edge Case Hunter: conditional C# package references bypass the scanner — `CSharpSyntaxTree.ParseText(source)` uses no defined symbols and token traversal ignores disabled text, so DEBUG/PLAYTEST bodies evade the normal build gate. Scan the relevant conditional source without exception ledgers; add direct scanner fixtures and location/control assertions. Repair pending.
  - [medium] [patch] Edge Case Hunter: compiled electrical-socket bindings may omit Socket — `WorkshopCompiledScene.Validate` accepts null while `WorkshopDeclaredPresentation.Query` dereferences it for an installed logic owner. Reject malformed driver/socket combinations at compiled admission before either worker installs them. Repair pending.
  - [false] [reject] Intent Alignment: directory discovery differs from obtaining evaluated compiler membership — the implementation does use directory discovery, but no omitted current production input was established. Independent `dotnet msbuild -getItem:Compile -p:Configuration=Debug` evaluation found all current main/Core/Simulation/Animation-worker/web inputs inside scanned roots (60/90/2/1/1 files respectively); exact actual Csc source/reference ownership remains a required guard. The intent requires coverage of actual production membership, not a new membership-export mechanism.
  - [false] [reject] Intent Alignment: finite identifier shapes are not general semantic dispatch detection — true as a description of a lexical guard, but no concrete uncaught supported shape was identified in this claim. The source contract names structural package identifiers, literals and named PartId access while admitting generic capability vocabulary. Conditional-code omission is separately accepted above; no claim of universal semantic analysis is made.
  - [high] [patch] Forward-Only Auditor: unsupported positive-duration curves fall through to Linear — the new compiled boundary permits a defined but unsupported driver/curve combination (for example CommittedActivation with SineSquaredPulse), then the evaluator's catch-all applies a different curve. Replace the fallback with exhaustive supported mappings and explicit refusal, and validate supported combinations at compiled admission. Repair pending; this confirmed forward-only finding cannot be deferred.
- Grouping: compiled socket and curve findings share missing semantic admission of presentation bindings; the malformed-endpoint verification gap remains its own test finding. Retention and scanner findings have separate causes.
- Forward-only auditor's structural old-layout refusal checks were explicitly nonviolations, not additional findings.
- Full local round 2 completed with 5,042 executed, 4,998 passed, 44 failed and zero skipped (native 4,550/4,506/44/0; four tool projects 492/492/0/0), exit 1. Original console and five TRXs remain in `.tmp/isolation-coordinator-round2/full-local.log` and `results/`. All five refreshed compiler captures succeeded. The same implementation owner received the consolidated five review findings and all 44 failures; affected fixes and complete verification remain pending. No SnapshotApproval or implementation Pass is claimed.

### 2026-10-29 — Affected repair re-review (two further repairs pending)
- Frozen candidate: `.tmp/isolation-coordinator-round3/candidate.diff`. Blind, Edge, Verification Gap and Forward-Only reviewers independently inspected their affected repairs; the unchanged rejected intent findings were not reopened. All five original accepted findings are resolved by inspection. Targeted implementation evidence is retained in the handoff: final native 71/71/0/0, bundle 61/61/0/0, with unchanged passing groups reused.
- verdicts: 2 new findings — high 0, medium 2, low 0, false 0, maybe-false 0
- findings:
  - [medium] [patch] Blind Hunter: delayed successful Run acknowledgement pins whichever scene is active on receipt — a held Run acknowledgement permits Reset and a later installation; current Commit can release the original uncaptured association, and the late receipt retains the newer scene. Preserve exact Run-scene ownership across delayed acknowledgement and verify actual-client Reset/reinstall/late-capture ordering. The coordinator confirmed the two-command pending allowance and held-ack transport path. Repair pending with the same implementer.
  - [medium] [patch] Edge Case Hunter: compiled electrical bindings admit defined non-electrical sockets — enum validation admits ActivationIn, but ElectricalLogicRead.At supports only FirstIn, SecondIn, PowerIn and Supply. Refuse unsupported sockets at compiled admission, with command/response negative and valid electrical controls. Repair pending with the same implementer.
- Verification Gap confirmed both NaN decode boundaries directly assert refusal, unchanged world and continuing gameplay; no remaining gap or unjustified precision was found in its affected scope. Forward-Only confirmed the shared admission contract and exhaustive mapping eliminate the Linear fallback, with no new forward-only finding.
- Three affected compiler captures completed successfully with zero warnings/errors; unchanged file-based tool logs are reused from round 2. The full solution run was not started because re-review identified these concrete defects first. No approval or publication is claimed.

### 2026-10-29 — Fourth candidate repair verification
- Both follow-up defects reproduced before repair: `isolation-followup-before-01.trx` 7/5/2/0. Repaired affected run `isolation-followup-after-01.trx` passed 110/110/0/0. Original TRXs remain in `CuriousContraptions.tests/TestResults`.
- Blind Hunter independently confirmed exact Run-scene retention before dispatch, sequence-keyed acknowledgement promotion and unsuccessful ownership cleanup; the real-client delayed acknowledgement/Reset/cadence/Construct/late-capture test establishes original metadata. Edge Case Hunter independently confirmed the four supported electrical sockets and real both-gate command/response control. Both findings are resolved, with no new affected findings. These are resolutions of the two logged rows, not additional findings.
- Frozen review diff: `.tmp/isolation-coordinator-round4/candidate.diff`, baseline unchanged. Refreshed playtest/solution/web compiler captures passed with zero warnings/errors (28.63s/21.66s/14.05s). Unchanged file-based tool compiler logs are referenced from round 2.
- Complete local verification finished successfully: 5,057 executed/passed, zero failed/skipped, exit 0. Native 4,565; tool projects 17/77/192/206. Console/TRXs: `.tmp/isolation-coordinator-round4/full-local.log` and `results/`. Exact committed-SHA approval and publication remain pending.

## Design Notes

Use the existing main game assembly as the authoring owner and one new Core runtime assembly. Separate schema ownership, not a second decoder for the same record. Runtime installation carries generic declarations and exact identities; authoring and element identity never become a hidden worker payload. Rebuild derived layouts/indexes from admitted declarative rows.

Initialize compiles the empty authored scene on main. Construct compilation failures return the current typed refusal without publishing candidate ownership or advancing committed revision. Cadence changes recompile timer-dependent declarations and validate matching revision/settings. Reset re-admits the retained compiled scene; authored snapshots stay main-owned for restoration, save and capture association. Native harnesses use this same production route.

Observation metadata association must be immutable and keyed to the exact admitted scene, including cadence revisions. Do not attach whichever construction is current when a capture arrives. Keep metadata for retained captures until their diagnostic lifetime ends, with dynamic capacity and ordinary cleanup.

The scanner protects actual package coupling, not arbitrary substring overlap. Derive package names from bundle declarations; use structural naming/dispatch rules with explicit positive/negative source fixtures. Do not carry681 generic-context exemptions into a new ledger. The older planted-engine-file/build criterion is deferred under the owner's no-mutated-tree rule: current proof exercises the real scanner/build entry point with fixture inputs and verifies normal build integration; mutation runs resume only at post-catalogue E2E.

## Verification

Implementer runs matching incremental builds and affected native/compiled-Node checks; coordinator owns one final complete solution round after implementation freezes. Reuse unchanged proof. No full-suite duplication on both sides.

- Build affected Core/main/worker/tool closures in Debug, with zero warnings/errors and matching compiler captures. No local Release worker build or web publish.
- Run changed behavior/control/boundary cases via actual production compiler/codec/runtime, including invalid compiled references, unknown enums, nonfinite fields, rejected/cancelled installation, cadence, Reset, same-world animation retention and old capture metadata after later edits.
- Run production scanner fixture controls and normal build integration, exact dependency/source ownership checks and scoped deletion searches. No mutants or whole-tree copied builds for negative controls.
- Follow docs/delivery-workflow.md#local-test-authority for five matching compiler logs, exact Node, generated browser-only deferral filter, full solution TRXs and TreatNoTestsAsError. Zero failures/skips.
- Normal hooks, five independent Auto layers, affected repair re-review and exact committed-SHA approval; push and verify remote main, without CI monitoring.

## Auto Run Result

Finalized 30 October 2026. Local implementation and independent review pass; normal commit hooks, exact committed-SHA approval and publication are the remaining publication steps. The Auto workflow status is done; this is not CI, deployment or browser qualification.

Implemented one element-free Core runtime boundary. Main compiles authoring into validated physics/activation/presentation/permission tables; workers consume the compiled installation. Straight Pipe uses its existing declared parameters, PartId is opaque, and normal builds enforce bundle-derived package isolation.

Changed file groups:
- CuriousContraptions.Core/{CuriousContraptions.Core.csproj,RuntimeSources.props}, main/worker projects and WorkerAot.targets: explicit runtime ownership and worker dependency closure.
- engine/gpu/WorkshopSceneCompiler.cs, WorkshopCompiledScene.cs, compiler/codec partials and schemas/workshop_{wire,authoring}.fbs with generated bindings: separate authored and compiled boundaries, atomic typed validation.
- engine/PartId.cs, registry/ports, instance/placement/snap/save callers, content/puzzles.json and ui/WorkshopGuidance.cs: opaque identities and declared parameters; delete WorkshopPipe, UndeliveredParts and named helper paths.
- engine/gpu/WorkshopObservation*.cs, BrowserWorkshopClient*.cs, diagnostics/trace and client TypeScript: exact session/scene capture association, bounded pending ownership, retained late-capture metadata and cleanup.
- WorkshopDeclaredPresentation.cs, Abstractions/ElementPresentation.cs, BundleValidator.cs and animation protocol consumers: one supported presentation contract; delete unused TimerObservation/AnimationTimerSegment while preserving live timer behavior.
- tools/BundleElements/ElementIsolation.cs, Program.cs and project: normal-build package guard including conditional C# branches; delete the known-violation ledger.
- CuriousContraptions.FlatBuffers/{FlatBufferVerify.cs,Table.cs}, integrity tests and THIRD-PARTY-NOTICES.md: bounded UInt16-vtable correctness repair with honest vendor provenance.
- CuriousContraptions.tests, affected tool/runtime/deferred-browser schema consumers, architecture/contracts/testing guidance, TODO and implementation handoff: migrate actual callers, preserve behavioral controls and reconcile current ownership/evidence.

Review outcome: seven accepted findings repaired (one high, six medium), zero unresolved, zero deferrals. Original pass patched one high/four medium; follow-up patched zero high/two medium.
1. Medium: unused authored/compiled associations retained across edits — remove compiled graphs from metadata and release inactive uncaptured candidates.
2. Medium: missing malformed presentation endpoint proof — command/response NaN controls assert OutOfRange, unchanged installed world and continuing gameplay.
3. Medium: inactive conditional source escaped isolation scanning — scan all directive branches with nested/alias/location/generic controls.
4. Medium: missing electrical socket admitted — refuse absent required sockets before installation.
5. High: incompatible driver/curve silently used Linear — shared supported-combination admission and exhaustive evaluator refusal.
6. Medium follow-up: delayed Run acknowledgement retained the newer scene — bind exact dispatch scene by pending command sequence, promote only its Applied receipt, release unsuccessful ownership.
7. Medium follow-up: defined non-electrical sockets admitted — restrict compiled electrical bindings to FirstIn/SecondIn/PowerIn/Supply and verify both wire boundaries.

Both rejected intent concerns retain their original reasons: evaluated current production Compile inputs were within scanned ownership, so directory discovery did not establish an omitted source; finite structural identifier rules matched the specified guard, and no additional concrete uncaught supported shape was shown. The real conditional omission was separately repaired. Neither rejection claims universal semantic analysis. All five independent Auto layers reported; affected Blind/Edge/Verification Gap/Forward-Only re-review resolved the original findings, and final Blind/Edge re-review resolved both follow-up findings without new ones.

followup_review_recommended: false. This is a converged follow-up pass (patched high 0, medium 2), and actual affected re-review is complete. No specific unverified review risk remains. deferred: [].

Verification: coordinator complete solution run exited 0 with 5,057/5,057/0/0 (total/passed/failed/skipped), comprising native 4,565, WorkerAotGuard 17, Coverage 77, E2eRunner non-browser 192 and BundleElements 206. All five compiler inputs were supplied; refreshed playtest/solution/web captures passed with zero warnings/errors, and unchanged tool captures were reused. Final affected repair proof was 110/110/0/0; prior failed rounds and their exact repair mappings remain in the handoff. Strict runtime TypeScript/Node proof is recorded there. The full suite was not rerun by the implementer.

Evidence identity: baseline published main `15cf84969870cddefc7c91b6a539d7144dc47f76`; final reviewed runtime candidate `.tmp/isolation-coordinator-round4/candidate.diff`; complete console/TRXs `.tmp/isolation-coordinator-round4/full-local.log` and `results/`; compiler captures in that directory with unchanged tool inputs linked from round 2. Finalization changes only current status documents. The resulting normal-hook commit SHA is the canonical candidate identity for independent approval; it will be reported after commit, without self-hashing or amending evidence to include its own SHA.

Remaining qualification: normal hooks and exact-SHA approval precede any push; CI/artifacts/deployment remain unverified. Browser/E2E remains explicitly deferred until every named catalogue element passes native proof; performance/device/retention qualification retains its named later gates. No review finding was deferred into those gates.
