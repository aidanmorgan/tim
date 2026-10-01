# P0-006 independent review

**Terminal verdict: Pass — scoped Design/tooling implementation and publication.**
Independent A09 verification matched commit 90437a5d80aa0570dfe11d5b9e3d8b3e9ea95a4b,
its expected parent, remote main, all 22 approved blobs and unchanged relevant inputs.
See [publication receipt](reviewer-publication.json). Earlier SnapshotApproval authorized R2
with only A09 pending; that publication criterion now passes.
All A01–A08 pre-publication Design/tooling criteria pass for this exact snapshot.
There are zero unresolved unintended regressions within the impact scope below.
This is not runtime lifecycle, worker, browser, physical-device or campaign qualification.

Implementation /root/p0_006, actual session 01a0f87c-eef8-7f41-bbda-011b5a5733c1.
Independent reviewer /root/p0_006_review, actual session 01a0f88e-63e6-7fc1-8879-5f1bb7f8fc96.
Parent /root, session 01a0f5f1-ab9a-79a3-9181-0dbb87285ec5.
The reviewer authored no deliverable fix.

Canonical candidate [snapshot-r2.json](snapshot-r2.json), SHA256
6d979c318fd06ad68961bfbd420d282124a6df46aa2e3a0dd91ba5d6e9798051.
Base 854bbc71dc528c0e083389ffbabc3d5df71423f6; scoped diff
223babd8864a27421bbee64d299a01cd6ba7127395c20678c4b710e3a4ed1707.
The snapshot owns the exact allowed paths and relevant-input hashes.
[Raw R2 reviewer commands/results](reviewer-r2-results.json) retain checks and incidents.
[R1 Fail](review-r1.md) and its original evidence remain historical failures.

## Criteria

| Criterion | Independent result |
| --- | --- |
| A01 reconciliation/provenance | Pass. All 21 candidate and 136 relevant hashes match; scoped diff independently reproduces. All 916 runtime/build/content inputs unchanged. Actual owner/reviewer identities differ. All 11 R1 candidates reconstruct from eight exact archives plus unchanged paths. |
| A02 lifecycle matrix | Pass for Design. Five host modes ×16 commands, IntegrityLost distinction, pending admission/commit/application and durable Save are explicit. Revised animation retirement keeps one aggregate pool and at most two retained result generations. Session startup/recovery/disposal and numerical limits remain explicit. |
| A03 exact construction/save | Pass for Design. Source Start/LoadSave/ReplaceMachine gaps map to candidate preparation and exact browser construction/settings/selection restoration. Failed precommit Load differs from committed authority followed by recipient failure. Save is construction-only and transactional durable storage is a separate result. No crash replay or fake rollback. |
| A04 mutation inventory | Pass for Design. 28 named families cover source-backed solver/host maps, topology/query registrations, 20 RuntimeState overrides, controllers, commands/results/events, real animation free/version arrays, browser bindings/resources, save and diagnostic/bootstrap ownership. Fixed current body storage and empty authoritative RNG set are explicit. P0-004 member closure supplies detailed declarations; actual enrollment/injection remains P0-014/020/032. |
| A05 clocks/history/overload | Pass for Design. Three streams and peak replacement are fully charged; required stronger 256 world +256 UI example fits 173833/196608 bytes, 842-UI counterexample rejects at 219541. Calibration and sample intervals include separate quantization plus drift; freshness/uncertainty/overload/liveness remain bounded. No P0-003 budget is relaxed. |
| A06 finite failure oracle | Pass at required Design stage. Pre/postcommit, capture/restore failures, lost results, stale reused identities, recipient/storage/process failures have explicit expected outcomes and future runtime owners. 336 family/point classification rows are not represented as injected runtime proof. Saturated UI/world retirement and delayed acknowledgement have finite progress/recovery cases. |
| A07 verification/integrity/typing | Pass. Clean Release build; 490 official enumerated checks and 23 independent probe controls pass; all three required full Coverage audits exit 0. Completion/qualification remain false. MutationFamily is enum-typed through APIs, reports and iteration; undefined values reject. Relative links, exact TODO hash, baseline/bundle continuity and main Compile exclusion pass. Anvil local changed/probe check has zero findings. |
| A08 independent closure | Pass for this scoped snapshot. R1-F01/F02/F03 resolved; impacts and applicability below reviewed. Retained procedural errors are not retroactively excused. |
| A09 publication | Pass. Independent remote lookup, exact parent/pathset and all 22 blobs match commit 90437a5d80aa0570dfe11d5b9e3d8b3e9ea95a4b. All 136 relevant inputs, 916 baseline inputs and bundles remain unchanged; index empty. No deployed artifact is produced by this Design scope. |

## Findings and independent controls

R1-F01: separate physical, world-animation and standalone-UI histories now use
3*(P+W+U)+1024. All nine samples, endpoint records and referenced S stamps are included.
Single AnimationSamples envelopes still have one generation; no wire amendment combines streams.
World-dependent cosmetics use coherent physical T/presentation D; autonomous UI has a separate
D history. Ordinary world replacement retains UI history and retires affected old histories before
new seed copies. Its peak uses max(old world sum,new world sum), not two simultaneous world rings.
Navigation serializes subsequent UI replacement. Transport leases and sealed candidates keep their
existing separate quotas, while descriptors are owned runtime state rather than hidden wire copies.
The required velocity-rich P0-005 example with both 256-output streams passes; optional 842 UI
outputs exceed the joint quota and reject. Required workloads cannot be trimmed to fit.

One aggregate 128-entry animation result pool remains. Active evaluators and retained result
generations are distinct. With UI7/world8 results live, Reset is sent against 8; new evaluator9 is
empty, with no generation9 request admitted until every 8 result including Reset is acknowledged
and the original acknowledgement allocation has returned. The existing Credit return confirms
processing without inventing a new semantic acknowledgement. Once 8 retires, 7/9 may retain results.
UI generation IDs use the same global allocator, so replacement cannot collide with UI7.

Saturation controls independently verify both 127+1 arrangements; no new result is admitted at128.
Returns/acks consume no result slot and can drain the pool. If an old result or its acknowledgement
is delayed, the barrier remains pending; it cannot create a third result generation or free a
newer reservation. Existing result retransmission/monotonic acknowledgement semantics apply.
Lost original lease return cannot be invented; the 500 ms retirement-stall and 1000 ms liveness/
disposal policies require explicit recovery when progress is not established. Ongoing UI requests
have no implicit extra pool. These are finite protocol progress/recovery decisions, not claims
that actual worker latency or application CPU passes.

R1-F02: the single offset interval now expands each timestamp difference by 2q and drift;
sample mapping adds its own qs once. Independent probes verify containment of 125.41 ms,
quantization-only overlap, exactly touching intervals, strictly disjoint intervals, expiry and
mapped halfwidth exceeding 1 ms by 1 ns. The actual-clock <=1 ms qualification remains required.
Raw timestamps and invalid mapping histories are never silently relabelled or used to rescale physics.

R1-F03: all 28 mutation families are named enum values. Expected(), result records, Enum.GetValues
iteration and invalid-boundary tests preserve that type. Stable extensible identities remain typed
records. The independent probe also rejects undefined MutationFamily and FailurePoint members.
No repository-wide magic-string compliance is claimed.

The [reviewer probe](reviewer-probe.cs.txt) is run with:
dotnet run --project tools/LifecycleContractReview/LifecycleContractReview.csproj -c Release --nologo.
It contains 23 independently derived controls, not production runtime code.

## Impact and evidence applicability

| Relevant closure | Invariant, evidence and disposition |
| --- | --- |
| Command compiler/inbox → transaction → lifecycle/results/query | Original generation, dependency, first terminal query result, one whole-tick revision and barrier invalidation remain P0-005-compatible. Source Start, LoadSave and post-Commit publication demonstrate the current gaps; no existing synchronous path is certified. |
| Solver/topology/query metadata/controllers → participant restoration | P0-004 ownership and member inventory remain unchanged. Current body storage versus animation free lists, all 20 part RuntimeState overrides and host producer metadata remain distinct. M01–M28/F01–F12 name required exact authority/resource sets and implementation/proof children. |
| Animation registrations/controls → world/UI generations → browser property ownership | R2 changes the affected contract, so prior F01 proof is not reused. New aggregate-window, history, peak and delayed-ack controls run. Existing P0-005 instance/descriptor identity, output sizes, Presented and first-terminal rules remain unchanged. |
| Physical/A clocks → histories → coherent display → age qualification | R2 changes intervals and histories; new arithmetic controls ran. No renderer authority, physics extrapolation, clock rescaling or mixed parent/child time is introduced. Missing brackets produce explicit coherent hold and still fail required age/performance if too old. |
| Browser construction/storage/navigation/resources | Candidate state, commit versus application, exact restoration, serialized generation retirement and disposal/recovery gates reviewed together. No extra hidden history or old-world rollback after commit. Runtime UI and storage proof remain named prerequisites. |
| Source/build/content/export/performance budgets | No runtime source change. Actual current main Compile inventory contains zero reviewer-probe/generated paths after cleanup; all 916 baseline inputs and served artifacts remain unchanged. Standalone tools use existing tools exclusion. No game export, Chrome input or timing run is applicable to this Design-only diff. |
| Documentation/manifests/history | Required three full audits ran afresh. Current 134 original inputs remain unchanged; added relevant review records identify prior findings. Eight retained originals exactly reconstruct R1. TODO remains the same authorized two-line state, with original inverse proof still applicable. |

Scoped reuse: R1 is an overall Fail, not blanket positive evidence. Its independently verified exact
candidate/input identities, source observations, file links and TODO inverse are reused only for
unchanged criterion portions enumerated above. R2 compares all original relevant inputs and the
916-entry source/build/content closure, reconstructs changed originals, and freshly checks affected
source/tool/contract logic. New tool build/490 cases, independent 23 controls and required inexpensive
full audits ran once at this frozen boundary. P0-003 budgets, P0-004 ownership and P0-005 wire/replay/
binding/reliability terminal records remain applicable source/design prerequisites; no historical
runtime proof is upgraded.

## Retained procedural incidents

The initial implementation TODO write after a blocked gate remains a real violation in R1 records.
Gated reversal restored the exact predecessor before separately gated reapplication; current TODO
hash/inverse remain verified. That correction is not retroactive compliance.

R2's first archive preservation check exited 1 because each new archive acquired one extra newline.
The owner retained that failure, corrected the bytes through the gate, and this reviewer independently
reconstructed every R1 hash. The failed attempt is not rewritten.

The reviewer initially placed its independent probe project under docs. The probe passed, but actual
main Compile evaluation showed three generated obj C# files entering the game compile list.
Before any game build, gated correction moved only the reviewer project under tools and removed only
its generated docs obj/bin. A broad rm command was rejected; explicit pathlib unlink/rmdir restricted
to the two previously gated generated directories completed cleanup. Probe rerun passed, actual main
Compile then contained zero such paths, and all baseline/bundle identities still matched.
Raw details are retained in reviewer-r2-results.json. The reviewer deliverable contains no game change.

Anvil validation and application were separate explicit steps. Local antipattern results have
daemonStatus not-wired and do not establish repository-wide enforcement or compliance.

## Historical publication authorization and verified receipts

Only the implementation owner may commit/push the 21 publicationAllowlist paths in snapshot-r2.json
plus snapshot-r2.json itself: **22 files total**, through normal hooks.
Expected parent: 854bbc71dc528c0e083389ffbabc3d5df71423f6.
Exclude TODO.md, AGENTS.md, unrelated runtime and all reviewer records/probe paths.
Do not mutate the immutable snapshot. Changed deliverables or relevant dependencies invalidate approval.

A09 requires independent verification that:
1. Reported commit has that exact parent and remote main resolves to it.
2. Its changed paths are exactly the 22 approved files.
3. Every approved blob and canonical snapshot match the reviewed SHA256.
4. Relevant inputs remain applicable/current, with no unreviewed scope entering publication.

A09 now passes in the linked independent receipt, closing P0-006 at Design/tooling scope. No production-origin check is waived: this publication
creates no deployed runtime artifact, while later implementation/worker/UI/device gates remain open.
Review-only evidence may publish separately under the finite reviewer exception, without recursion.
