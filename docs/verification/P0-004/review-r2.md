# P0-004 r2 independent review — Fail

Reviewer /root/p0_004_review, thread 01a0f7d2-92c9-7e11-9fbf-3ea247ea179c; implementation /root/p0_004, thread 01a0f7b9-0ecd-74b2-81ce-361031596a36.
Canonical [r2 snapshot](snapshot-r2.json), SHA-256 `bb2d8867d4c14de6131e91b262fd46483d0f40ac6b048797fa04459c6300f0c2`, binds the reviewed candidate and relevant closure. [Raw results](reviewer-r2-results.json) retain commands and outputs. Base remains eeeb932734ccd9034020592f0da7a87b619cbe3f.

**Terminal verdict for r2: Fail. No SnapshotApproval.** One newly traced spatial/query/publication ownership defect remains. This is distinct from the corrected explicit examples in [r1](review-r1.md). Other unfinished semantic review remains Incomplete; a census count does not certify every placement.

## R2-F01 — authoritative query metadata and query kernels assigned to presenter

The actual assignment map still places ScenePhysicsAssembly._queryGeometry, _queryOwners, _referenceBindings and _presentationPoses in GodotPresenter. assemblies.md calls _queryGeometry a derived browser cache. The production call chain contradicts that classification:

- LinearPusherPart.ObservePhysics (lines 134–145) reads CollisionGeometry, verifies shaft geometry/surface/source/opacity, creates WithChild replacement and calls ReplaceCollisionGeometry.
- ScenePhysicsAssembly.ReplaceCollider (183–194) applies the physical collider update, then registers geometry metadata keyed by CompoundGeometry. Its comment explicitly requires exact metadata recovery from retained snapshot geometry after rollback.
- CaptureQueryBodies (153–168) and CollisionGeometry (172–180) read the same registry and reject missing physical query metadata.
- CapturePublicationReads (216–230) requires that metadata to construct committed BodyQueryRead. _queryOwners selects physical/query ownership and activity. It invokes CapturePresentationReads, which uses _referenceBindings and mutates _presentationPoses before publication.
- WorldGeometry.Trace and CaptureSweep consume that geometry for light/sound/air/solid queries. SweepSnapshot._surfaces and PreparedColliderGroup are also assigned Presenter, but FindOverlap/Sweep instantiate PhysicsBody and invoke CompoundCollision, ConvexSeparation and ConvexSweep.
- MachineWorld.PhysicsAssembly remains a Presenter property returning the now Host-assigned backing field, without an exact getter replacement that resolves this live topology reference.

The permitted Presenter graph references Geometry and Protocol only. Current scene-key metadata needs a forward value/identity transformation, but authoritative query metadata, rollback registration and solver-backed sweep state cannot be relabelled browser preview resources. Expected correction: compiler candidate topology before atomic install; Core-owned authoritative query/geometry metadata and kernels; Host-owned committed publication scratch; separate browser construction capture, resources and received pose/preview history. Shared current buffers/reference bindings require explicit producer versus recipient splits. Immutable metadata does not authorize cross-context live reference access.

The implementation owner independently confirmed the complete source chain and is correcting this closure. No reviewer authored a deliverable fix.

## Prior findings and independent checks

| Scope / criterion | Independent result and remaining limit |
| --- | --- |
| R1-F01 / A02, A04 | r2-boundaries explicitly relocates CommittedEventId/PoseReadStamp/enum reads into Protocol, keeps BodyPublicationRead as a Host producer aggregate and moves topology validation/evaluator methods out of Protocol. Every currently Protocol-assigned type is represented in its declaration table. The named r1 contradictions are corrected; the newly traced spatial kernel/metadata contradiction still fails the overall assembly/authority gate. |
| R1-F02 / A03, A04 | Six parent fields now target Host with explicit producer/recipient splits. Stage/capture, occurrence/acoustic reservation and recipient application are distinguished. The nested spatial producer dependencies remain wrong under R2-F01. |
| R1-F03 / A01, A07 | Independent updated capture performs read-only actual web Compile evaluation and hashes the additional 12 inputs. Program.cs source/lifetime was independently inspected; r2 correctly says main/test have four semantic contexts and web has source reconciliation only. Browser async Engine ownership/disposal/loop calls are explicitly mapped. No generated web runtime or deployment proof is claimed. |
| R1-F04 / A06 | New semantic-operation classifier and authorization of all timer reference recipients close the reproduced wrapper bypass. Independent compiled fixtures test nested object casts, as-casts, conditional/coalescing arrays, nested local functions, tuple aliases, Length reads, returned aliases and collection mutation: 18 known/unknown recipient controls pass. Tuple alias classifies Read but unauthorized recipient still rejects, consistent with conservative bounds. This proves the scoped guard, not arbitrary alias precision. |
| A01 exact identity | All 55 candidate and 26 recorded relevant-input hashes match. Scoped diff independently equals af1c3e47f325207391b4d20bab48171ec8872da5b4359699696389fe51ff8b2f. r1 snapshot hash unchanged; all 22 shard reversals reconstruct exact prior hashes. |
| A03 current census | Reviewer full audit: 1,820 inputs, 4,386 members, 332,135 uses, 333,066 calls, 18,551 conservative callers, zero binding errors, exit 0. Incorrect semantic placement still passes structural currency as documented. |
| A05 lifecycle/reuse | All 916 original runtime source/build hashes match; existing timer/animation/spatial/transaction code remains untouched. R2-F01 affects planned ownership of collider replacement/rollback/query metadata, so complete lifecycle design review remains Fail/Incomplete. |
| A07 preservation/impact | Exact two-line TODO reversal reproduces ce513fdfc6f1cf34578ee323ee4783949ffb34bb7fee0aec3e39c91f0bfdd619. Read-only source tracing covered pusher topology replacement, rollback, physical query eligibility, optical/air/acoustic queries, producer publication and recipient presentation. |
| A08 required tooling proof | Reviewer clean Release build: zero warnings/errors. Bundled canonical positive, 22 negatives and 22 semantic flow controls pass. Independent 18-case probe and real updated production audit pass. Owner global Coverage results remain separately reported; no global-current/runtime qualification claim is made here. |
| A09 independent review | Frozen r2 reviewed; R2-F01 prevents SnapshotApproval. Remaining unexamined semantic groups are Incomplete. |
| A10 publication | Not authorized or attempted. |

## Reuse and stage limits

The r1 raw failure is preserved. New source classification/input closure was independently rebuilt and reproduced; unchanged runtime identity was compared directly rather than rerunning unrelated native/browser suites. This review does not reuse an earlier runtime pass as current qualification. Flow evidence is bound to r2 SourceInventory, OwnershipAudit, OwnershipPolicy and model through the canonical snapshot and the review-only probe at tools/p0-004-review-r2.

No browser input, server restart, production export or deployment was performed. Root retains exclusive Chrome/Playwright control. Native/browser performance failures, real worker isolation, physical devices and per-part/mode lifecycle gates stay open. r2 is Design/tooling only. The implementation owner may fix the bounded finding, preserve r2 and freeze a revised snapshot for independent review; no successor may proceed on this Fail.
