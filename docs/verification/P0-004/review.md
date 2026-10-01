# P0-004 independent review

**Terminal verdict: Pass — scoped Design/tooling implementation and publication.**
Independent A10 verification matched commit `275849a102ff91f3f4984e8fd9ab8bf138df48d1`, its expected parent, remote main, all65approved blobs and unchanged relevant inputs. See [publication receipt](reviewer-publication.json).

Historical pre-publication decision: SnapshotApproval was Approved for r3, with only A10 pending.
All A01–A09 pre-publication criteria pass within the documented Design/tooling scope.
There are zero unresolved known unintended regressions in that scope. This is not runtime, worker,
browser, device, per-part or campaign qualification.

Implementation: /root/p0_004, actual thread 01a0f7b9-0ecd-74b2-81ce-361031596a36.
Independent reviewer: /root/p0_004_review, actual thread 01a0f7d2-92c9-7e11-9fbf-3ea247ea179c.
Parent: 01a0f5f1-ab9a-79a3-9181-0dbb87285ec5. Distinct actual threads establish independence.

The sole current canonical candidate is [snapshot-r3.json](snapshot-r3.json), SHA-256
`5e6d99943d7f3b81f065fc5d6485c89c63f9acb8bb2e29ef2d2291048723b2ea`.
Base revision: `eeeb932734ccd9034020592f0da7a87b619cbe3f`.
Scoped diff: `326dab1b7383a53048440aaecc5af7eb7ed398bd3e26ee1262538fb819c848c6`.
The snapshot owns the exact allowed paths/input hashes; this review references rather than duplicates
that manifest. [Reviewer results](reviewer-r3-results.json) retain actual commands/output, probe identity
and preservation checks. Earlier [r1 Fail](review-r1.md), [r2 Fail](review-r2.md) and their raw results
remain unchanged historical evidence.

## Criteria

| Criterion | Verdict and independent evidence |
| --- | --- |
| A01 source/prerequisites | Pass. All64 candidate and28 relevant-input hashes match; scoped diff exactly reproduces. P0-002/P0-003 have applicable terminal independent Design/tooling/publication verdicts. Their unchanged source/measurement contracts are prerequisites, not reused runtime qualification. All916 runtime/build baseline entries remain identical. |
| A02 assembly dependencies | Pass for target Design. Eight acyclic boundaries explicitly separate Geometry, Protocol, Core, compiler, two hosts, animation kernel and Godot presenter. The full Protocol declaration/signature/enum closure is enumerated; topology validators and evaluator methods leave Protocol. Specialized solver/query kernels remain Core. No browser/animation-host reference to simulation-host implementation is authorized. Actual extraction/compile isolation remains with named implementation rows. |
| A03 members/callers | Pass for the finite reviewed source boundary. Independent capture reproduces1820inputs4386members332135uses333066calls18551conservative contexts with zero binding errors. Field/property/event/record/indexer/primary captures, reference-bearing values, readonly backing collections, linked test/generated/package inputs and current web entry are reconciled. Direct uses plus explicitly conservative alias/dispatch/callback closure map impacts; no precise points-to or current worker-isolation theorem is claimed. |
| A04 authority and typed bindings | Pass for Design. Reviewed source chains distinguish core functional quantities, compiler candidate transfer, host producer buffers, animation evaluator state and browser resources/history. Exact parent/nested-reference and spatial corrections resolve earlier contradictions. Typed stable IDs/generations replace scene references/local slots at boundaries; no second mutable authoritative copy or animation feedback into physics is allowed. |
| A05 existing owners/lifecycle | Pass for Design. Exact unchanged timer arrays, AnimationBatch, BodyBoundsTree and SimulationTransaction source identities match the prior independent reuse record. Their specialized algorithms remain; canonical enrollment/order, reverse rollback, metadata/maps/free-list/event restoration, retained query leases and generation rejection have explicit implementing/proof owners. This does not upgrade historical unexecuted/native failure evidence. |
| A06 typed schema/adversarial controls | Pass. Clean tool build; canonical positive plus22 negative cases,22 semantic-flow controls and106 positive/wrong-placement pairs pass. Reviewer-derived18 additional controls pass on current code, including nested casts, as-casts, conditional/coalesced aliases, local functions, tuple aliases, returned views and collection mutation. Unknown recipients reject even where conservative access is Read. Domain selectors are enums; source/member/caller/type/work identities remain typed at internal APIs. JSON/CLI/compiler metadata conversions are boundaries. Repository-wide magic-string cleanup is not claimed. |
| A07 impact/preservation | Pass in scope.72 distinct catalogue variants across51 scripts independently match catalogue→scene→script references and exact CAT-I/V task IDs. Actor ownership, code location and implementation/proof owners remain distinct. Runtime/content/assets/export configuration are unchanged. Exact two-line TODO reversal preserves4558orders799specifications847original execution anchors and every other byte. r1/r2 historical snapshots/originals/shard reversals recover exact prior hashes. |
| A08 verification/integrity | Pass. Reviewer Release tool build has0warnings/errors; actual main/test Release/PLAYTEST rebuild/census passes with0binding errors. All three full Coverage audits independently exit0 with0missing/orphaned/changed, while CompletionProven/RuntimeQualified stayfalse. Probe/rebuild/audit output is retained. Main Compile excludes tools at HEAD and current source; tools are Godot-ignored, and browser export excludes docs. No affected runtime part requires new UI/export evidence from this isolated Design/tooling change. |
| A09 independent frozen review | Pass. Reviewed r3 exact hashes, actual source/target mapping and changed checks; all prior findings disposed below. No reviewer deliverable fix. SnapshotApproval authorizes only the finite publication scope below. |
| A10 publication | Pass. Independent remote lookup, parent/pathset and all65blob hashes match the approved snapshot at commit275849a102ff91f3f4984e8fd9ab8bf138df48d1; current28relevant inputs unchanged and index empty. |

## Impact and applicability matrix

| Input/consumer family | Required invariant / independent review |
| --- | --- |
| Pure numeric/spatial/solver state | Geometry values keep double authority and complete affine basis; specialized sweeps/constraints/manifolds/hierarchy remain Core. Reviewed actual RigidPose/Convex/Compound/BodyBoundsTree dependencies and source type-role groups; Godot conversion methods are explicitly presenter-only after extraction. No algorithm replacement or native-equivalence claim. |
| MachineWorld, networks and functional catalogue participants | Core functional state is distinct from Host admission/lifecycle/publication; current callbacks and scene-key networks become generic typed capabilities. Visibility currently affects physical eligibility and must become authoritative participation, including initial capture, query, hash, fallen-body and rollback paths. Shared-script variants retain separate proofs. |
| Construction and installed topology | Candidate ownership transfers once; failed compilation cannot mutate installed generation. Body/joint maps, declarations and query metadata become core typed identities, not renderer dictionaries. Browser authoring caches retain declaration values only. |
| Collider replacement/query/publication | Traced LinearPusher→CollisionGeometry→WithChild→ReplaceCollider→query metadata→rollback/captured query→publication and sweep/trace callers. R3 places installed metadata/kernels in Core, producer reference/pose scratch in Host and received history/resources in browser. Missing metadata, stale revision and wrong slot/type/source/opacity remain explicit rejection/proof cases. |
| Protocol and live producer references | Reviewed all currently Protocol-assigned type groups, constructors, generic constraints and method dependencies. CommittedEventId/PoseReadStamp/enum reads are Protocol; BodyPublicationRead/live query geometry stays producer-local. Six MachineWorld parent references and PhysicsAssembly getter have explicit producer/recipient replacements. |
| Animation/resources/events | Existing batch storage remains AnimationKernel. Scene target registries/materials/audio stay presenter-owned. Occurrence reservation/physical commit is independent of recipient admission and animation capacity; clocks/IDs/event cursors have named host/consumer owners. No synchronous scene call remains permitted inside physical transaction after cutover. |
| Recorder/diagnostic consumers | Frozen assemblies.md assigns physical/publication collection to SimulationHost, evaluation collection to AnimationHost and application/render collection to Presenter. r2-boundaries excludes recorder instances from Protocol. Combined with graph and forward replacement, current cross-context PerformanceRecorder parameters are deleted; each context emits immutable Protocol samples. Existing physical recorder assignments do not authorize other contexts to reference its assembly. Exact producer class names/layout are implementation choices for P0-014/024/026/027, not a missing ownership decision. |
| Aliases, callbacks, generated state | Conservative whole-context expansion explicitly includes unknown reference recipients, virtual/interface/delegate families, ref/out, collections, primary captures, closures and iterator state. Generated Godot/JSON caches are browser-bound; source index is not claimed to enumerate framework internals. Compiler errors reject rather than imply exact binding. |
| Browser entry/lifecycle | Actual web Compile evaluation and12input hashes bound Program.cs/configuration. Main/test have four semantic contexts; web has separately identified source reconciliation. Browser Engine/async state and external loop/disposal ownership are explicit; success is not falsely claimed to dispose. No web build/export or UI action was performed. |
| Build/content/tool/global views | Source/compiler/package inputs include generated/untracked additions; missing/deleted members/sources reject. Main exclusions and runtime identity justify no affected app behavior. Three required full inventory audits pass their documented scopes; semantic runtime/campaign qualification remains open. |

## Finding dispositions and retained proof

- PF01/PF02/PF06: actual linked/generated/compiler contexts and indexers replace the incomplete directory/syntax census. Independently reproduced zero binding errors and exact current membership.
- PF03/PF04/PF07 and R1-F04: refreshed semantic guards plus operation-aware classification and all-recipient authorization close the known bypass. Conservative graph limits remain explicit. Current reviewer18-case evidence supplements, rather than merely repeats, the bundled cases.
- PF05 and R1-F01/F02: extraction/application/publication roles are separated, Protocol dependency closure corrected, and parent producer references split.
- R1-F03: independently checked separate web source/configuration reconciliation; no fifth compiled-context or new browser proof is asserted.
- R2-F01:69 placements and full spatial-authority contract resolve installed metadata, query kernels, getters, producer scratch and renderer copies. Reviewed related pusher replacement, Core factory, query eligibility, rollback lease/revision and final property application impacts.

Current code/configuration are bound by r3. SourceInventory, OwnershipAudit and model hashes match r2;
classifier/decoder expectations remain applicable. OwnershipPolicy changes are the enumerated spatial
guards; the timer-recipient rule remains unchanged. Nevertheless current18-case probe execution is
retained because its test-source identity is now explicitly hashed, removing ambiguity about reuse.
All new/affected policy/currency checks were independently rerun. Prior raw failures stay bound to
their original snapshots; they are not rewritten to Pass. Runtime source identity was compared,
not used as a substitute for missing runtime proof.

Exploratory checker mistakes are not product findings: the first catalogue script check looked only
in .tres files and found72 absent direct script paths; tracing their actual PackedScene references
confirmed72correct edges. A broad table-row/anchor regex counted unrelated tables before the exact
work/execution/sequence patterns verified4558/799/847. The initial current-oracle output was truncated;
the full rerun/output is retained alongside it. None of those incomplete reads was used for approval.

## Historical SnapshotApproval authorization and required receipts

The implementation owner may commit/push **only the64 paths in snapshot-r3.json publicationAllowlist,
plus snapshot-r3.json itself:65files total**. Expected parent is
`eeeb932734ccd9034020592f0da7a87b619cbe3f`. Keep runtime, AGENTS.md, TODO.md and all review-only
records/probes out of this implementation commit. No amended dependency or deliverable is approved.

After publication the reviewer must independently establish:

1. Remote main points to the reported implementation commit with the expected parent.
2. Its changed path set is exactly the65approved files, with no unrelated paths.
3. Every approved blob matches the canonical SHA-256; snapshot bytes match its approved hash.
4. Current relevant inputs still match or any difference receives actual applicability review.

At SnapshotApproval only A10 remained; it now passes as recorded above. No deployment/UI/performance receipt was deferred from this Design-only change;
there is no changed browser artifact. Therefore no new deployed behavior check is required to close
this scoped publication. A receipt never replaces the later mandatory runtime/browser/device gates.
The independent terminal verdict and review-only evidence may be published under the finite reviewer
exception. A10 is now independently verified and terminal Pass is recorded; P0-005 is dependency-ready after coordinator handoff reconciliation.

Native66/76 failures, browser timing failures, unexplained Chrome input, unpublished integrated
runtime inputs, three-context worker proof, physical devices, every part/mode Run/Reset/save proof
and the150-level campaign remain open. ECS is deferred; no new framework prerequisite was introduced.

Review-only probe source is retained under [reviewer-probes](reviewer-probes/): r1 targets its historical classifier signature; r2 also ran against r3. These text archives preserve exact experimental source and project inputs without adding supported application paths. The canonical implementation snapshots and original failure logs remain immutable.
