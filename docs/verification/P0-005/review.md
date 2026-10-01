# P0-005 independent review

**Terminal verdict: Pass — scoped Design/tooling implementation and publication.**
Independent A10 verification matched implementation commit 88f52db15611c6a85217838136db1e560ab14747,
its exact parent, remote main, all 46 approved paths/blobs and unchanged relevant inputs.
See [publication receipt](reviewer-publication.json). Earlier SnapshotApproval authorized R4
with only A10 pending; that publication criterion now passes.
All A01–A09 pre-publication criteria pass for the scoped Design/tooling deliverable.
There are zero unresolved known unintended regressions within the reviewed impact scope.
This is not runtime, worker, browser, physical-device, per-part or campaign qualification.

Implementation /root/p0_005, actual session 01a0f835-2500-73c0-a046-738333d3d7ff.
Independent reviewer /root/p0_005_review, actual session 01a0f848-fb01-7a53-90ce-7ff6d51d65e9.
Parent 01a0f5f1-ab9a-79a3-9181-0dbb87285ec5. The reviewer made no deliverable fixes.

Canonical candidate: [snapshot-r4.json](snapshot-r4.json), SHA256
b8696f3cfb195d40673787107a8f99ca13150c072ce36a1afaa1d997edef4e39.
Base 46eb68fa47eee1be50a1da1c7fd1f6debc854522; scoped diff
ee2477fa05126a4eb514fbfe2da6e56a86ee0e83a4c7586891c3f35c716be7f9.
[Reviewer R4 results](reviewer-r4-results.json) retain exact new commands/exits/output.
Earlier [R1](review-r1.md), [R2](review-r2.md), [R3](review-r3.md) failures and raw proof remain
immutable and are not rewritten to Pass. The canonical snapshot owns the precise input/path manifest.

## Criteria

| Criterion | Independent disposition |
| --- | --- |
| A01 reconciliation/provenance | Pass. All 45 candidate hashes and 132 relevant inputs match; base/scoped diff independently reproduce. Actual owner/reviewer sessions differ. All 916 runtime/build/content inputs remain identical. P0-003 measurement and P0-004 ownership terminal reviews remain applicable prerequisites. |
| A02 exact schema/units | Pass for Design. Finite routes, typed variants, widths, checked length rules, units, original result identities, exact query read stamps and immutable animation instances are specified. Prior missing bindings and lifetime rules were forward-corrected. The two final outcome/phase ambiguities now resolve explicitly. |
| A03 stable identities | Pass for Design. Persisted document/member identities survive reorder and storage reuse; local slots do not cross wire/save boundaries. Animation instance IDs never reuse within generation, including failed attempts; retired values cannot target replacement bindings. |
| A04 application/replay | Pass for Design. Admission differs from commit; schedule, phase/order, revision/dependency, cancellation, barrier and duplicate handling are explicit. Result/read identity survives delayed delivery and generation reset. First terminal query outcome is immutable. |
| A05 bounded storage/transport | Pass for Design arithmetic. Separate 504 KiB data/8 KiB control caps and same-allocation Return avoid reciprocal credit deadlock. Per-family application reservations and byte quotas remain bounded. 2516 full animation outputs fit 65533 bytes; 2517 rejects; required 256 outputs need 6773 bytes. Physical example with 257 velocity subscriptions is 44057 bytes. Actual latency/CPU/GPU/memory qualification is not established. |
| A06 current mode/owner mapping | Pass for Design. Current typed parameter, diagnostic, clock, read/event, four evaluator and renderer descriptor modes have explicit mappings and named implementation/proof children. Axes, colours, extents, pose/reference maps, scalar ranges, feedback and wavefront lifetime remain source-compatible; no second physical authority or simulation-owned cosmetic command producer. |
| A07 positive/control/boundary checks | Pass. Reviewer Release build and 109 design fixtures pass. Independently derived source wavefront goldens distinguish early continuation, exact range boundary, late-held/lost receipt and completion. Prior schema/identity/size/reorder controls remain bound to their reviewed snapshots; changed controls ran afresh. |
| A08 integrity/preservation | Pass. Three required full Coverage audits independently exit 0; completion/qualification remain false. R3 originals plus unchanged paths reconstruct all prior hashes; earlier retention remains exact. Two-line TODO inverse matches 4436305d1bf3dd237ea1cc84e7db179d5adf5afe68c30ba7f365e15b6aaa9dd5. New relative file links resolve. Anvil changed-scope check has zero findings. |
| A09 independent closure | Pass for frozen R4. All findings disposed below; actual direct/transitive scope and evidence applicability recorded. SnapshotApproval only authorizes the exact publication scope. |
| A10 publication | Pass. Independent remote lookup, exact parent/pathset and all 46 blob hashes match commit 88f52db15611c6a85217838136db1e560ab14747; all 132 relevant inputs and 916 runtime hashes remain unchanged, index empty. No deployment behavior is required for this isolated Design/tooling artifact. |

## Final finding dispositions

R1-F01/F04 introduced complete browser descriptor/feedback mappings and semantic lifetime tables;
R1-F02 separated reserved control bytes and returned the original leased buffer; R1-F03 added
original command generations; R1-F05 bound registration to complete output size. R2-F01 preserved
strict increasing finite input ranges; R2-F02 added instance-qualified samples and activation/
retirement/capture ordering; R2-F03 added echoed query read revision and retained tuple validation.

R3-F01 is resolved: an admitted pending query invalidated by Reset/Load receives Cancelled.
Stale instead reports an unavailable exact read revision in the same generation. A completed
terminal result is never rewritten by a later barrier/Cancel; wrong-generation admission owns
no result slot. Original identity/revision and one-time release remain intact.

R3-F02 is resolved: early Presented records visibility and continues at 12 m/s until age 8/12 s.
At 0.1/0.2/0.65 s the distances remain 1.2/2.4/7.8 m. An expired unseen occurrence holds
MinimumDistance until a matching receipt, even at 10 s; only then may it complete. Delayed receipt
uses current age; duplicate/stale instance/occurrence receipt cannot complete a reused ring.
The current SceneAcousticWavefrontRun source (retirement at presented AND range; held first frame
only for expired unseen events) independently determines these expected outcomes.

## Impact and scoped reuse

| Closure | Required invariant and reviewed applicability |
| --- | --- |
| Command compiler/inbox/transaction/result callers | Typed stable generation-qualified identities, exact application records and reserved terminal results; no queued command represented as committed success. R1/R2 analysis remains applicable to unchanged command grammar. |
| Query authority → retained reply → UI/history/ack | Captured exact generation/read revision is independent of latest envelope; current consumers discard stale values but release the correct terminal reservation. R4 only resolves terminal outcome selection, not query layout. Prior R3 identity checks remain applicable. |
| Animation evaluator/registration → complete samples → browser adapter | Cosmetic state remains A-owned; immutable instance mapping, pending activation, retirement, exact descriptor and sequence checks reject stale reuse. All 26-byte record/capacity arithmetic is unchanged from independently checked R3. |
| Acoustic occurrences → bounded ring groups → Presented/removal | Matching instance plus occurrence identity prevents cross-ring completion. R4 changes only explicit early/late completion branches; source-derived new controls cover those branches and unchanged bounded retention. Physical events remain independent of drawing. |
| Construction/save/current authored content | Canonical version 5 and stable member IDs require named P0-008/016/020 forward cutover across callers/content/tests. Old v4 is rejected after cutover. Current 72 variants/150-level scope remains; no partial runtime migration is claimed. |
| Physical publication/affine geometry/feedback | Whole committed samples, full affine basis, typed units and simulation query ownership remain unchanged. No renderer data becomes physics input. Exact velocity subscriptions and fixed capacities preserve declared workload obligations. |
| Transport/transfer/restart | Same-allocation returns, control reservations, typed acknowledgements and finite per-family retention are unchanged. Their independently reviewed R2 arithmetic/ownership cases remain applicable; R3/R4 do not alter wire sizes or quotas except the already reviewed R3 sample/query changes. |
| Build/source/performance | Standalone tool is excluded from main game compilation; unchanged 916-entry source/resource/build closure means no changed game behavior. P0-003 budgets and P0-004 ownership contracts remain exact. No runtime/browser test is reused as new qualification. |

Reuse decision: compared current canonical relevant-input identities and actual changed candidates
against R3. Only six prior candidates changed: bindings/reliability, contract/source-impact summaries,
README and added oracle cases/helpers. Only existing relevant TODO input changed, with exact
authorized two-line preservation. Prior source/configuration/tool-independent ownership, command,
transport, identity and capacity proofs retain their original artifacts and applicable criterion
mapping above. New helper branches and changed prose were inspected and checked independently;
Release/109 cases and required inexpensive full audits ran once for the frozen R4 boundary.
No new framework, duplicate full manifest or recursive receipt identity was introduced.

Anvil local checks report zero findings with daemonStatus not-wired; this is not repository-wide
enum or boundary certification. Closed choices in changed design fixtures are enums; actual
IDs/slots use typed records. Necessary labels/JSON metadata stay at report boundaries.
The source retains known wider string-refactoring work for later owned tasks.

## Historical publication authorization and verified receipts

Only the implementation owner may commit/push the **45 paths in snapshot-r4.json publicationAllowlist
plus snapshot-r4.json itself: 46 files total**, through normal hooks.
Expected parent: 46eb68fa47eee1be50a1da1c7fd1f6debc854522.
Do not stage TODO.md, AGENTS.md, unrelated runtime, reviewer records or other dirty paths.
Do not amend the frozen snapshot. Changed deliverables/relevant dependencies invalidate approval.

After the owner reports publication, the reviewer must independently verify:

1. Reported commit has that exact parent and remote main resolves to it.
2. Changed path set is exactly the 46 approved files.
3. Every approved blob and canonical snapshot SHA256 match reviewed bytes.
4. Relevant inputs remain unchanged or receive explicit applicability review; no unreviewed scope
   enters the commit. Only then may A10 and the task receive terminal Pass.

A10 was the sole pending criterion and now passes in the linked receipt. There is no changed browser deployment and no deferred
pre-publication behavior/performance check hidden behind this receipt. Subsequent implementation,
production-origin, worker, exact Run/Reset/save, physical-device and release gates remain mandatory.
No Chrome action, reload, server start or export occurred during this review.

The independent terminal verdict/review-only evidence may later publish under the finite reviewer
exception; it does not require recursive review. Current scoped task status is terminal Pass.
