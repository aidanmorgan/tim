# P0-002 independent adversarial review

Current verdict: **Pass — scoped Design/tooling publication independently verified**. A01–A10 pass for commit61e35b3fda99e8e758575c98c94c923cd3675927. Earlier failures and approvals remain historical evidence. Runtime qualification and clean-checkout integrated reproduction remain incomplete.

## Identity and reviewed snapshot

Reviewer provider/session: openai / `01a0f733-7712-79f2-bb99-db91244a86c6`; actual agent `/root/p0_002_review`; spawned 2026-10-01T11:22:20.333Z. Independently read the first session_meta record of the reviewer's own rollout; parent_thread_id `01a0f5f1-ab9a-79a3-9181-0dbb87285ec5`. Implementer session `01a0f6f8-aa85-71c3-8b91-1225e841682d`, agent `/root/p0_002`, is different. The reviewer has not authored deliverable fixes.

Reviewed HEAD: `d99b5dc88dda943513e8813b03307428f3b7bcfa`.
Contract SHA256: `c663d7cd9668a3856457df3896bb473bf924832cebcf3e4b40650a684c8691d4`.
Snapshot SHA256 supplied at dispatch: `8a1efbffaaecef4b562746395f193eb37b85d51ac60c14a0d49285ed6f9b3c2a`.
Independently recomputed tracked scoped diff SHA256: `5087ee1670d81cc6dea8d8391c23caae2dfc0328a8aa447196c1f13008bb02ef`.
Every enumerated non-self snapshot file hash matched; no mismatch.
Working TODO SHA256 at inspection: `31a963fbb0091ae4e5d08bcbc0705259ab47373d15c4cb72ee42abe5c1f6a469`.
The reviewed publication list has 52 files. Runtime migration, TODO and AGENTS are excluded. Changed dependencies invalidate affected approval.

## Criterion matrix

| Criterion | Initial observed result | Verdict |
| --- | --- | --- |
| A01 source identity coverage | Current CLI reports 1471 sources/consumers, zero missing/orphan/changed. Semantic law association is incomplete (F06). | Fail |
| A02 capability units/model/oracles/owners | Inspected all 71 model/oracle/unit/dependency declarations and owner map; focused unrelated-valid-owner tests pass. Full owner/source semantic reconciliation remains under review. | Incomplete |
| A03 required children/dependencies | Independent deletion probes F01/F02 accepted missing requirements as current. | Fail |
| A04 current parts/fixtures | BallPart variants and contact-friction consumers omit applicable laws (F04/F05); fixtures inherit omissions. | Fail |
| A05 current modes | Existing mode/default malformed-boundary tests pass; 104 records. No per-mode UI qualification claimed. Deeper resource/mode audit remains pending revised snapshot. | Incomplete |
| A06 typed strict boundary | Inspected enum/ID serialization and callers; supplied 33 tests pass, including canonical/unknown/null cases. Independent additional boundary controls remain pending. | Incomplete |
| A07 source/artifact currency | All original hashes pass, but deleting every CurrentPart artifact still passes (F03). | Fail |
| A08 build/tooling compatibility | Independent Release build 0 warnings/errors; actual xUnit executable 33/33; three current audits return 0 and preserve incomplete runtime/review state. | Pass, bounded |
| A09 runtime/history separation | Independently rehashed all 916 baseline source files: zero mismatches. Root project excludes tools/**/*.cs. No game/browser build or behavior changed by this tooling row. Historical preservation comparison remains pending. | Incomplete |
| A10 independent review/publication | Distinct reviewer is active; no snapshot approval. Remote clean-checkout prerequisite provenance needs correction (F07). | Incomplete |

## Findings returned to implementation

- **F01 / A03 — dependency removal is accepted.** Replaced all 71 capability Dependencies arrays with empty arrays in memory. CapabilityAudit returns InventoryCurrent=true. Existing MissingCapabilityDependencyAndCycleReject tests add an edge then omit closure; they never delete a required edge. Independent required edges must not be defined solely by the data under test.
- **F02 / A03 — research child removal is accepted.** Replaced every Research binding RequiredChildren array with empty arrays. InventoryCurrent=true. NamedChildren only derives selected Markdown anchor links, leaving research obligations unprotected.
- **F03 / A07 — artifact removal defeats currency checks.** Replaced every CurrentPart consumer Artifacts array with empty arrays. InventoryCurrent=true. Source-derived required catalogue/scene artifact membership must survive deletions.
- **F04 / A04 — BallPart variants lose supported buoyancy.** ball, tennis and bowling all use parts/scenes/ball.tscn and BallPart, all have buoyancy=0 parameters. BallPart supports the same Buoyancy binding for each. Only ball's inventory includes Buoyancy. The contract expressly includes supported buoyancy despite the default; variants and fixtures must follow actual supported source.
- **F05 / A04 — contact-friction relationships omitted.** MachinePart.InitialContactMaterial has nonzero friction coefficients; BallDetector, Basket, PressurePlate, PoweredGate, Trampoline, ImpactLever and other overrides also do. Their inventory lacks SlidingFriction while selected balls/ramps/pipes have it. Inspect every declared body's material, including inherited defaults, rather than patching only named examples.
- **F06 / A01 — generic mappings hide named cross-domain requirements.** Spec194 names motor/windmill source rotors, torque, reverse/clutch shafts and shared work but lacks ShaftTorque/ElectricalPower; spec165 motor/transmission work has the same omission. Spec028 explicitly names electricity→mechanics→heat, light→phase/reaction→flow, pressure→moving occlusion, radiation→powered control chains, but maps only seven generic compiler/state/ledger services and zero children. Reconcile all 799 source obligations; count coverage alone does not establish no orphan laws.
- **F07 / A10 context — published design inputs are not a reproducible clean runtime checkout.** HEAD TODO lacks the working register/specification anchors; many referenced current source inputs are unpublished. Design-stage publication need not newly qualify or publish that runtime. It must retain exact required-input identities, demonstrate clean-checkout limitations honestly, and clearly distinguish scoped design/tool publication from a remote integrated audit pass. No fallback, mock canonical input or wholesale unreviewed runtime publication is authorized.

Implementation acknowledged F01–F04 and is addressing the additional findings. Original failures remain retained when fixes arrive.

## Direct/transitive impact matrix

| Surface | Expected invariant / intended change | Independent evidence and limit |
| --- | --- | --- |
| Coverage CLI → discovery → existing manifests | Discover additional task anchors and unlocked fixtures without silently certifying evidence | Inspected actual diff, ran current inventory/element audits; 1471 unreviewed,72 mode/process pending,1097 unresolved sources remain explicit |
| Capability CLI → inputs/schema → inventory relations | Strict typed boundary; missing required relations cannot appear current | Read all new code, ran 33 tests and independent in-memory mutations; F01–F03 are concrete violations |
| Source bindings → capability owners → future engine readiness | Every named law has an appropriate owner and all required domain relationships | Inspected 71 capability contracts and selected source obligations; F04–F06 show incomplete mapping, broad re-review required |
| Catalogue resources → scene scripts → fixture inheritance | Actual type/configuration controls supported capabilities and currency | Checked BallPart presets, common/overridden ContactMaterial and animation declarations; failures propagate to fixtures |
| JSON boundary → typed API/dictionaries/control flow | Closed sets remain enums, extensible identities stay typed; no compatibility aliases | Inspected converters, model fields and consumers; build compiles callers; further independent hostile boundary cases pending |
| Root runtime/build/presentation/clocks/resources | No runtime execution or browser behavior changes from tooling | Root compile excludes tools/**/*.cs; 916 unchanged baseline hashes. Chrome proof and production export are not applicable to this isolated tooling/design delta; their existing runtime owners remain incomplete |
| Active docs/publication | Commands and provenance accurately distinguish working source from remote checkout | F07 provenance correction required. No claim that matching published blobs proves remote integrated input availability |
| Register/campaign scope | Preserve all source identities and 150-level scope | 799 sequence anchors and847 old execution anchors independently counted; implementer makes no register edits in reviewed snapshot |

Anvil graph returned ready with 46 affected symbols but no dependent-file/test edges; this is not evidence of no consumers. Direct project references, code reads, compilation, CLI execution and mutations establish the bounded impacts above. Prewrite checks use explicit workspaceRoot. The local Anvil backend reports not-wired; an allow result is not repository-wide certification.

## Reproduced commands and raw outcomes

`dotnet build tools/Coverage.Tests -c Release --no-restore --nologo`:
Build succeeded; 0 Warning(s), 0 Error(s), elapsed00:00:00.78.

`dotnet tools/Coverage.Tests/bin/Release/net10.0/Coverage.Tests.dll -noColor`:
xUnit.net v3 In-Process Runner v3.2.2; Total33,Errors0,Failed0,Skipped0,NotRun0,Time0.251s.
An earlier `dotnet test ...` returned0 with no tests/output; it was not counted as test proof.

`dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll audit-capabilities . docs/coverage/engine-capabilities.json`:
exit0, Sources1471,Capabilities71,Consumers1471,Modes104,Missing0,Orphaned0,Changed0,InventoryCurrent=true,RuntimeQualified=false.

`dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll audit-inventory . docs/coverage/source-inventory.json`:
exit0,Sources1471,Unreviewed1471,Changed0,Missing0,Orphaned0,SourceInventoryCurrent=true,CompletionProven=false.

`dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll audit-elements . docs/coverage/catalogue-elements.json`:
exit0,Elements72,Fixtures302,UnresolvedSources1097,PendingModeReviews72,PendingProcessReviews72,LinksCurrent=true,CompletionProven=false.

Reviewer-only probe source is tools/p0-002-review/probe.cs (excluded from game compilation). It reads canonical files, deep-copies inventory and mutates only memory:
`dotnet run --file tools/p0-002-review/probe.cs -p:PublishAot=false -p:JsonSerializerIsReflectionEnabledByDefault=true`.
Control and all three destructive cases returned accepted=true and the identical1471/71/1471/104/0/0/0,true,false summary above.
ReviewCase values:0=Control,1=DeleteDeclaredDependencies,2=DeleteResearchChildren,3=DeleteCurrentPartArtifacts.
First invocation without the two properties failed exit134 because file-based app reflection serialization was disabled; that harness configuration failure is retained here and is not a product defect. The corrected invocation completed exit0.

No terminal Pass, SnapshotApproval or publication receipt exists for this snapshot.

## In-progress boundary probe attempt

After implementation resumed, coordinator requested independent probes of valid-but-wrong consumer/obligation kinds and Partial availability without current symbols. Reviewer extended only its own probe harness with cases4–6: catalogue consumer becomes FutureDeclaration and loses Symbols; Engine binding becomes ProductWorkflow; Partial capability loses CurrentSymbols.

The same corrected file-based run command stopped before the control case: eight CS8601 warnings at CapabilityTaskRequirements.cs:8, followed by TypeInitializationException whose inner NullReferenceException arose in SelectMany/ToDictionary at CapabilityTaskRequirements.cs:7. Exit134. The implementation was actively being revised, so this is retained as an in-progress startup result, not a verdict against a submitted revised snapshot. No mutation result can be inferred before a passing control. Reported to implementer and coordinator; awaiting a coherent revised snapshot.

Source hashes at this attempt: CapabilityInventory.cs `9607274c83d0fdf3af446f9d462bd5a95cd73d1ad9247c16f13c0162ee81f8c7`; CapabilityInputs.cs `111bd0be076956a10ad99c90c67fd95ef286caf412a5fba5c4da1a6f49e6f050`; index `e4aadd99b5818099398a3e8dc63545bc94e7ce128aca6a508085c0346dc53acb`.

The external browser-performance requirement document also changed to SHA256 `225c95dc77536d677a49064de8e744191dad9b6b3ce0c830b69f4cd5c6c850c3`. Its new distinct-frame throughput/missed-slot requirements and owners require semantic re-review; refreshing a stale hash alone cannot validate those relationships. This further invalidates original snapshot currency.

## Local status reconciliation

Implementation owner subsequently changed only the current handoff and P0-002 status cell in TODO.md. Reviewer independently inverted the three declared replacement fragments across those two lines: this recovered the prior reviewed TODO SHA256 exactly, proving all other bytes were preserved. Current TODO SHA256 is `a16db75917d3fbc6c98c1c844971678f8c8ec82f557711d70c291b424a5f921c`; recovered prior is `31a963fbb0091ae4e5d08bcbc0705259ab47373d15c4cb72ee42abe5c1f6a469`. Independent review Fail/corrections in progress is accurate. This narrowly approves status accuracy only, not the P0-002 deliverable or its publication. TODO remains excluded from the publication scope.

## Coherent revised probe round

Implementation declared code/data runnable and froze it during this round; documentation/provenance packaging was still underway, so no snapshot approval was requested. [Per-file code/data identities and original probe outcomes](reviewer-revised-probes.json) retain the reviewed intermediate revision. Manifest digest (sorted path:SHA256 lines, no trailing newline): `8f4c6335c197a615eaec26197d2e7c9702fa6d8b27d387f97caa3d772e8df159`.

Independent control again returned InventoryCurrent=true, RuntimeQualified=false, Sources1471,Capabilities71,Consumers1471,Modes104,Missing0,Orphaned0,Changed0. The same seven-case probe command now rejected:
1. Deleted dependency lists: Capability dependencies differ from required law contract.
2. Deleted research scope: Missing named source child.
3. Deleted catalogue artifacts: Missing required consumer artifact.
4. Wrong-valid catalogue consumer kind plus deleted symbols: Consumer kind differs from authoritative source classification.
5. Engine obligation changed to ProductWorkflow: Obligation kind differs from authoritative source classification.
6. Partial capability with no symbols: Capability declaration set differs from current implementation contract.

An expanded probe then independently replaced catalogue script symbols with an unrelated but valid PhysicsWorld declaration; this correctly rejected with Missing actual consumer declaration symbol. Two further mutations were incorrectly accepted:
- Added a second logical consumer for the same catalogue source with a fresh valid typed ID, and linked both: Sources1471,Consumers1472,InventoryCurrent=true. **F08 / A01:** the one-consumer-per-source contract is unenforced.
- Added a fabricated ball→tennis source child: Sources1471,Consumers1471,InventoryCurrent=true. **F09 / A03:** required-subset validation permits extra unrelated edges despite the exact-set claim.

Source relation inspection found 24 existing cycles, including element112↔gap01. The implementer clarified these are traceability references, not work-order prerequisites. The reviewer does not impose a DAG on legitimate crossreferences. The implementation owner will make coverage/reference semantics explicit with typed relations and reject fabricated extras. Capability-dependency DAG and work-order execution order remain separate.

F04/F05 examples now include BallPart buoyancy across all four presets and friction across contact-bearing catalogue/fixture maps. Revised F06 examples028,080,165,194,334 now retain appropriate law/consumer relationships. Wider source inspection still found spec017 island sleep omitting named ropes/transmissions/compliant/airflow/wake interactions and spec251 cross-domain coupling linking only current catalogue rather than future pressure/phase/reaction/buoyancy obligations. These are returned under F06, not accepted based on improved counts.

**F10 / A05/A07 — canonical mode declaration dependency:** numeric Operation/Colour interpretation does not read engine/LogicGate.cs or engine/OpticalColour.cs; neither source path appears in the current inventory shards. Changes to LogicGateKind/OpticalColour ordinal meanings would evade source currency and retain stale mode labels. The canonical external mapping must validate its actual enum definitions (or share the compiler-checked enums), with changed/reordered-declaration negative proof. ToneBand/TubeBendAngle incidental source hashes do not replace semantic boundary validation.

Code/data freeze was released after this round; implementer owns all fixes. The row remains **Fail**, SnapshotApproval **NotApproved**, pending a complete revised hash-bound package and independent re-review. Original failures above remain retained.

## Full revised package review (before owner-role correction)

Reviewed snapshot SHA256 `24060fbd587e6396992973a6427a5d73bcd3f0acaa8e052f2c7478d0571bf71e`: all95 enumerated files matched. Contract remained `e1a518a4a649e4c8db9d8e282a4ad12dee43c0ad9564f204bc3b7cd9ae8cd498`. No publication approval.

**F11 / A05–06, fixed:** canonical enum int.MaxValue and oversized decimal originally threw OverflowException outside the CLI's controlled rejection filter. Independently reproduced, returned to implementer, and then re-ran exact cases. They now throw InvalidDataException respectively with “Canonical mode enum ordinal cannot advance within the supported integer range.” and “Canonical mode enum ordinal is outside the supported integer range.” Canonical control accepts; reordered/changed values reject. All nine inventory mutations reject after the typed relation/uniqueness corrections. The old probe is preserved as noncompiled history/reviewer-pre-relations-probe.cs.txt; the active reviewer harness uses only the current schema.

**F12 / A08–09, fixed:** archived snapshots initially differed in byte hashes despite identical parsed payloads. Implementer restored original bytes; reviewer independently verified initial archive `8a1efbffaaecef4b562746395f193eb37b85d51ac60c14a0d49285ed6f9b3c2a` and relation archive `0e208bff367125cce3c490406229a409ed4341d0303a85ce11e44b3a07ef7cbf`. Hashes, not a conjectured newline direction, establish preservation.

**F13 / A02, open:** five capability implementation owners are actually aggregate Audit rows in the current register. RigidBodyDynamics, DependencyScheduling, ElasticStorage and FiniteWorkActuation point to P0-009, whose scope requires already-completed child extraction. InputAdmission points to retained-scope Audit S007. Existing ID validity does not make them the actual implementation owners. Implementer confirmed the defect and is forward-refactoring explicit typed implementation-owner sets to the actual existing child work orders, keeping closure/proof roles separate. No new runtime work or extra planning layer was requested.

Independent repeated checks:
- Release build:0warnings/errors,0.47s. Actual xUnit DLL:39tests,0failed/skipped/not-run,0.232s.
- Three CLI audits: capability1471sources/71capabilities/1471consumers/104modes, missing/orphan/changed0; inventory1471unreviewed; element72/302fixtures,1097unresolved,72mode/process reviews pending. RuntimeQualified/CompletionProven remain false.
- Recomputed source-origin ledger:216EL,37TH,22RAD,18GAP,72catalogue,302fixtures,5research,799tasks; no missing/orphan/duplicate bindings. Every one of302 fixture capability sets independently matches its actual authored catalogue kind.
- Read capability models/oracles/units and validated all71 owner IDs against actual current register rows. This stage-aware check discovered F13.
- All514 input-provenance entries independently rehashed and compared against exact `git show d99b5dc...:path` bytes:174same,82different,258absent; zero discrepancies. This is read-only prerequisite comparison, not an executed clean-checkout integrated audit. Design publication cannot fabricate absent runtime inputs or claim release qualification.
- All916 baseline runtime source hashes unchanged. Initial historical coverage artifacts retain their recorded hashes. All active scoped Markdown file links resolve. Relative links inside the archived README retain their original historical context; no historical text was rewritten to make them active input.
- Reversed exactly the two recorded TODO status-line edits: current `a343c1ec01ca1d93359db7b01534135168a76f993380b5e4468cff3c15dc475e` returns external baseline `3882418ab15bcf90a7e1e263d6cda482f9acef404e6a0203680dfea47ba9b426`. All other bytes, added reuse requirements and measurement contract are preserved. Orders4558 remain consecutive,799specifications and847 retained anchors unique.
- Independently inspected SimulationTimers arrays/identity sort/borrowed views/checkpoint ownership, AnimationBatch owner/generation/version and active/free/sample lifecycle, BodyBoundsTree query refit/canonical pairs, and SimulationTransaction preflight/reverse unwind/fault semantics plus actual consumers/tests. reuse-state.md accurately separates existing implementation/test presence from missing stable external identity, generation, complete rollback and worker/browser proof. Baseline XML has9WorkshopAnimation and13CommittedCompliantContact cases, no named four-owner test-class proof. ECS experiments remain explicitly deferred.
- Source relations now distinguish CoverageScope and SourceReference, with exact expected sets. Existing cyclic references are traceability only, not readiness dependencies. Canonical external enum definition/value checks bind mode mappings to their actual sources. The corrected source-family relationships include017/028/080/165/194/251 and334, preserving future law scope rather than only current catalogue bookkeeping.
- Active timing requirements retain distinct59.4/89.1FPS and≤1% missed-slot limits; P0-003 owns measurement contract and P0-034/035 qualification. Nothing in this Design review qualifies those runtime metrics.

Updated criterion disposition at this reviewed package: A01,A03–A09 pass within the documented Design/tooling scope; A02 fails until F13 is fixed; A10 remains Incomplete with no SnapshotApproval/publication. Prior failures and limited intermediate probes remain historical evidence, not current passes.

## Exact snapshot approval after owner correction

**SnapshotApproval = Approved. Terminal task verdict = Incomplete, solely pending A10 publication receipts below.** All pre-publication criteria now pass within the documented Design/tooling impact scope; there are zero known unresolved unintended regressions in that scope. This does not qualify the runtime or convert unreviewed per-part proof to Pass.

Approved implementation snapshot:
- Base HEAD/required commit parent: `d99b5dc88dda943513e8813b03307428f3b7bcfa`.
- Exact96-file allowlist and per-file hashes: snapshot.json, SHA256 `27c5a0fba199fb93df30a4f468b90abd37846c500d67486ac31dd8b8f7d72098`.
- Contract SHA256 `8fd1c0b0cff9089f25bfb43a8dba0cdc35e1122076a5986d7ca2e96f0201424d`.
- Tracked scoped diff SHA256 `fc90fdf7842927f694c711b9e019de1646d5325f4edf4e6789fab94d71152a27`; untracked additions are covered by the exact file hash list.
- Required-input provenance SHA256 `eb1c4572638bd8f3be911d40ddc94abeab716b8e707f97ea9c69812063e3b72f`; all514 current input hashes independently matched.
- Local-only TODO SHA256 `7fa335af62b49227baff66f6266a7db0745c2ff1545252c67489d90856b5f759`. Reversing the two recorded status lines exactly recovers external baseline3882418ab15bcf90a7e1e263d6cda482f9acef404e6a0203680dfea47ba9b426.
- Original archived snapshots independently match8a1efbff...,0e208bff... and24060fbd... in full as recorded above. All96 current snapshot hashes matched immediately before this approval.

**F13 resolved.** ImplementationOwners is a nonempty typed set, retaining exact WorkOrderId values through declarations, expected contracts, validation, serialization and tests. Actual work-stage values cross the Markdown boundary through canonical validation into WorkStage enums; only Native/Integration/Worker/Optimize are implementation-eligible. The singular schema is rejected, not migrated. Independently inspected every one of71 capability implementation-owner sets against the current row titles/scopes/stages. The five corrected capabilities point to the separate concrete implementation/optimization children listed in derivation.md; aggregate Audit P0-009/S007 no longer substitutes for them. No new scheduler row or runtime implementation was invented.

Final reviewer checks:
- Release build0warnings/errors,0.52s.
- Actual focused xUnit DLL40tests,0errors/failed/skipped/not-run,0.222s.
- Current-schema control remains1471sources/71capabilities/1471consumers/104modes, zero missing/orphan/changed, InventoryCurrent=true and RuntimeQualified=false.
- All15 independent inventory/owner negative probes reject. New cases: empty owner set; duplicate owner; Audit-stage P0-009; unrelated executable P0-008; correct owner rendered ineligible; old singular JSON field. Canonical enum positive accepts; reordered, changed ordinal, int.MaxValue and oversized-decimal negatives all reject cleanly.
- [Exact final raw probe/test output](reviewer-final-probes.json) and current reviewer-only harness retain commands, cases and results. Earlier failing probes are retained separately. No repeated runtime/browser suite was needed: exact runtime/source identities and the root tools exclusion remain unchanged, with the previously documented impact proof reused by identity.
- Source tables/data, mode boundaries, consumers and manifests were reviewed in the prior rounds; the final owner correction changes only owner representation/role validation, eight capability shards and documentation/evidence. It does not alter model laws, source capability membership, modes or runtime code. Prior passing checks remain applicable by the corresponding hashes and bounded change inspection.

| Criterion | Final pre-publication verdict |
| --- | --- |
| A01 | Pass: exact membership, one logical consumer per source and real fixture inheritance |
| A02 | Pass:71 bounded contracts, actual typed implementation-owner sets and stage/semantic rejection |
| A03 | Pass: exact typed scope/reference relations, required dependency graph and reverse bound-source ledger |
| A04 | Pass: current catalogue/fixture relationships and the four existing-owner reuse/state contracts with truthful deferred proof |
| A05 | Pass: distinct configured modes, actual canonical enum declaration/value checks, unsupported/overflow rejection |
| A06 | Pass: typed closed sets/IDs through callers and strict canonical schema; removed singular input rejects |
| A07 | Pass: current hashed sources/artifacts and lexical declaration associations; stale identity cannot establish currency |
| A08 | Pass: focused Release build/tests/audits and immutable historical failure evidence |
| A09 | Pass:916 unchanged runtime sources, preserved4558/799/847 scope, unchanged historical runtime evidence and justified tooling-only impact |
| A10 | Snapshot approval granted; terminal Incomplete pending publication verification |

This approval authorizes only implementation agent `/root/p0_002` / session `01a0f6f8-aa85-71c3-8b91-1225e841682d` to stage, commit and push the exact96-file snapshot. Reviewer-owned files, TODO.md, AGENTS.md, game/runtime changes and other dirty files are not in that publication. Any content or affected dependency change requires re-review.

Remaining A10 receipts are genuinely publication-dependent:
1. Commit with the reviewed base parent and exactly the96-file scope, without unrelated staged content.
2. Matching remote branch commit identity.
3. Independent remote-commit blob hashes for all96 approved files, including the snapshot file itself, plus unchanged required working inputs.
4. Retain explicit clean-checkout integrated reproduction Incomplete because required TODO/runtime/authored inputs remain unpublished. Do not report remote integrated audit success or runtime/browser qualification.

No new deployment/UI/performance gate belongs to this isolated Design/tooling publication; those required runtime gates remain with their existing owners. A matching receipt is sufficient only for this scoped A10 requirement. No successor is authorized until the reviewer verifies these receipts and records terminal Pass.

## Publication hook rejection and scope clarification pending

The implementation agent's commit attempt failed before creating a commit. The repository's unchanged staged-test hook scans original C# bytes using numbered TypeScript snapshot paths. Independent reviewer read .githooks/pre-commit, tools/anvil/CheckStagedTests.cs and tools/anvil/no-skipped-tests.json: TEST-001 includes a regex alternative matching the word Explicit followed by equals, which also matches the RequiredTaskScope.Explicit=>false switch arm. That branch controls source-scope expansion and is not a test-skip instruction.

The proposed forward-only enum rename to NamedSources accurately identifies the finite authored-source scope. It is not an alias, suppression, test change, conditional skip or hook bypass. This is still a deliverable change and invalidates the prior exact-snapshot approval. Independent inverse-rename identity checks, actual zero-skip test execution, unchanged hook/policy identity and a refreshed exact snapshot are required before another publication attempt. No commit was created; the implementation owner reports its96 paths unstaged. The previous approval and blocked attempt remain historical evidence.

## Fresh snapshot approval after domain-scope rename

**SnapshotApproval = Approved** for the new98-file allowlist in snapshot.json, SHA256 `faff5ee3b870989d97ed98bb8188e1a428ed0c67a6fbb5bc9d811cc4debd78b2`. Terminal verdict remains **Incomplete pending A10 publication verification**. This replaces the invalidated96-file publication authorization.

Independent re-review confirms:
- Every current98-file hash matches the snapshot. Base HEAD/required commit parent remains `d99b5dc88dda943513e8813b03307428f3b7bcfa`; index is empty and the failed attempt created no commit.
- For each of the nine changed TaskRequirements files, reversing only RequiredTaskScope.NamedSources→RequiredTaskScope.Explicit (plus the enum declaration) and applying the recorded trailing-newline count exactly reproduces its previously approved SHA256. No other token, branch, scope membership or behavior changed; all799 task callers compile.
- All other prior96-file content hashes remain identical except revision-results.json, which retains the attempt/reverification. Two new evidence files are publication-failure.json and the exact previous approved snapshot archive. The latter independently hashes to27c5a0fba199fb93df30a4f468b90abd37846c500d67486ac31dd8b8f7d72098.
- Contract,514 required-input hashes, local TODO and all inventory data remain unchanged. Earlier criterion/impact checks apply by these exact identities and the independently verified rename-only code delta.
- Actual Release build:0warnings/errors,0.84s. Actual xUnit executable:40tests,0errors/failed/skipped/not-run,0.384s. Integrated capability audit again returns1471/71/1471/104, missing/orphan/changed0, InventoryCurrent=true, RuntimeQualified=false.
- Retained raw failed-hook output maps8.ts to CapabilityTaskRequirements.cs and reports TEST-001 at the enum switch arm. Independent inspection of the hook/registry explains the false positive; it does not justify skipping a real test or suppressing the rule.
- Hook/policy files are unchanged: .githooks/pre-commit SHA25606b596457ca28409633252379bb8f3d78584ded6dc4341354df1f564ce9f84a1; CheckStagedTests.cs b818ccebcd39be919361b9afeefdf6b6d54c14cc6dd9738f191aa2c1b3864fd9; no-skipped-tests.json1264775c9781fbadc20eae640f4f85721db8c339e6c1bf35f72e1cd8f468e964. No skip, focus, suppression, alternate schema or hook bypass was introduced.

All A01–A09 pre-publication results remain Pass. This approval authorizes only the implementation agent to publish the unchanged98-file snapshot using normal repository checks. The same A10 requirements apply to this new allowlist: reviewed parent, exact changed-file set, matching remote commit/blob identities, unchanged required inputs and explicit clean-checkout integrated reproduction Incomplete. Reviewer files, TODO, AGENTS and runtime changes remain excluded. No successor before terminal independent Pass.


## Terminal independent verdict — Pass

**P0-002 Design/tooling verdict: Pass.** A01–A10 all pass in the scoped contract. This is not a runtime, browser, device or clean-checkout integrated qualification.

Independent A10 inspection verified [publication receipt](reviewer-publication.json):
- Published commit and local HEAD: `61e35b3fda99e8e758575c98c94c923cd3675927`.
- Parent: `d99b5dc88dda943513e8813b03307428f3b7bcfa`, exactly the approved base.
- Independent `git ls-remote origin refs/heads/main` returns61e35b3fda99e8e758575c98c94c923cd3675927.
- Exactly98 changed paths, equal to the approved allowlist; zero unexpected or missing paths.
- Every committed blob matches its approved SHA256, including snapshot.json `faff5ee3b870989d97ed98bb8188e1a428ed0c67a6fbb5bc9d811cc4debd78b2`. Current scoped working files also match.
- All514 required working input hashes and all916 baseline runtime hashes remain unchanged. TODO remains7fa335af62b49227baff66f6266a7db0745c2ff1545252c67489d90856b5f759.
- Index empty. Hook and policy hashes remain exactly those independently recorded above. Normal-hook execution was reported by the implementer; the independent guarantees here are unchanged hook/policy bytes, exact published content and the separately reproduced zero-skip tests.
- Commit content excludes reviewer evidence, TODO, AGENTS and unrelated/runtime migration files.

The bounded design/tooling publication is complete and permits the next dependency-ready work order. Clean-checkout integrated reproduction stays explicitly Incomplete because unpublished input prerequisites remain; the required-input manifest names them precisely. The unchanged native/browser failures, per-part UI/lifecycle/save proof, independent workers, actual60/90FPS device budgets and150-level campaign remain open under their existing owners.

This terminal verdict and attached review-only receipts form the finite independent gate. Publishing those review-only records does not modify the approved implementation contract, criteria, code or authored content.


The executed reviewer probe source is retained verbatim in [reviewer-final-probe.cs.txt](reviewer-final-probe.cs.txt). Its former local execution path was `tools/p0-002-review/probe.cs`; the archived file is review evidence, not a compiled repository tool. The earlier pre-relations probe remains separately archived with its historical failures.
