# P0-004 r1 independent review — Fail

Reviewer /root/p0_004_review, actual thread 01a0f7d2-92c9-7e11-9fbf-3ea247ea179c, is distinct from implementation /root/p0_004, thread 01a0f7b9-0ecd-74b2-81ce-361031596a36.
Reviewed canonical [snapshot](snapshot.json): SHA-256 `07ca8e38a35d9fefafe99d324668138e1abbe9fbce5e9059d3f82ceeb9b2927a`, base `eeeb932734ccd9034020592f0da7a87b619cbe3f`.
The record binds the exact 43-file candidate and its input closure; this review does not duplicate that manifest. [Raw reviewer results](reviewer-r1-results.json) retain actual commands, outputs and exit codes.

**Terminal verdict for r1: Fail. No SnapshotApproval or publication authorization.** Known semantic inconsistencies remain. Unfinished review coverage is additionally Incomplete, not a claimed regression-free scope. The implementation owner acknowledged findings and will produce a revised immutable snapshot; r1 and this failure remain historical evidence.

## Findings

| ID | Independent evidence / expected invariant | Observed result / required disposition |
| --- | --- | --- |
| R1-F01 | Protocol may depend on Geometry only. CommittedEvent<T> is declared with PoseReadStamp and CommittedEventId in engine/bridge/CommittedEventStream.cs:25. | All CommittedEvent<T> properties are assigned Protocol, while CommittedEventId and PoseReadStamp properties are assigned SimulationHost/HostState. EnumRead/EnumReadKey/EnumObservationSlot likewise remain host-located, and BodyPublicationRead.Query remains a Protocol property containing core BodyQueryRead/live CompoundGeometry. These assignments contradict the target graph. Resolve transitive type/signature/generic/enum dependencies and explicit value replacements, not just selected owner guards. |
| R1-F02 | Browser-owned parents cannot retain live host producer builders. MachineWorld.cs:778–780 stages/captures its observation references inside physical commit. | _scalarObservations/_booleanObservations/_enumObservations are assigned GodotPresenter/SceneResource/P0-026, while the corresponding producer builders are SimulationHost/P0-014. No explicit recipient transformation resolves these parent fields. Reconcile complete producer/recipient aggregates, including occurrence/acoustic nesting, with exact authority and replacement. |
| R1-F03 | Every included current caller/lifecycle input must be mapped or explicitly bounded with source evidence. | Four contexts capture main/test only. CuriousContraptions.web/Program.cs and its separately compiled project are absent from input closure. Program.Main starts the real browser loop and retains Engine across DisposeAsync; TwoDogWebBoot being compiled into main does not include this caller/async lifetime. No ordinary authored field omission is alleged. Bind its source/configuration and justify its actual host ownership/caller boundary. |
| R1-F04 | A refreshed member fingerprint must not authorize an unreviewed timer mutation merely because equivalent syntax is wrapped. | Independent probe invokes actual SourceInventory.Classify and OwnershipAudit.Validate. Direct _states[0]=default rejects (ReferenceEscape); cast and parenthesized equivalents classify Read and pass with refreshed fingerprints. OwnershipPolicy excludes Read despite the documented conservative escape rule. Fix syntax/authority treatment and test equivalent wrappers independently. This is a classifier/guard probe, not a runtime source mutation or worker proof. |

## Criterion and impact matrix

| Criterion / impacted scope | Checks and result |
| --- | --- |
| A01 provenance and prerequisites | All 43 allowlist hashes and 24 recorded relevant-input hashes matched. Scoped diff and two-line TODO reversal independently reproduced exactly. Distinct actual identities match. Web-host closure R1-F03 fails the claimed completeness; full prerequisite applicability remains to be recorded on revised closure. |
| A02 assembly graph | Independently read eight-boundary graph, source record definitions and actual member assignments. Forbidden dependencies R1-F01: Fail. |
| A03 state/callers | Real capture reproduces 4,386 members, 332,228 uses, 333,066 calls and 18,551 conservative caller identities. Indexer and linked-test additions inspected. Producer-parent placement R1-F02 and web caller gap R1-F03: Fail. Conservative graph is not a points-to or worker-isolation proof. |
| A04 authority/bindings | Visibility eligibility and four-owner reuse distinctions are explicitly present in revised draft text. Protocol/producer placement still violates required ownership. Other semantic groups are not yet independently certified: Fail with remaining coverage Incomplete. |
| A05 reuse/lifecycle | Source contract preserves existing timers, AnimationBatch, BodyBoundsTree and transaction algorithms; no implementation rewrite is claimed. Complete applicability/semantic group review awaits revised dependencies: Incomplete. |
| A06 typed boundary and adversarial proof | Clean tool build and all 21 bundled attacks pass. Independent refreshed-wrapper attack R1-F04 succeeds improperly: Fail. Production runtime enums are unchanged; no repository-wide enum compliance is claimed. |
| A07 direct/transitive impacts | Main build excludes tools; scoped candidate changes tooling/docs only. Parent producer references, nested protocol types, web startup/disposal and conservative generated/alias effects examined. Complete transitive placement is not established: Fail/Incomplete as above. |
| A08 focused tooling proof | Reviewer Release build passes with zero warnings/errors. Bundled positive plus 21 negatives pass. Reviewer full production audit exits 0 with 1,808 source inputs, zero binding errors and RuntimeQualified=false. Three Coverage audits and runtime baseline identity were owner-reported; independent reuse/global-audit applicability is not yet certified here. No browser/export/performance result is substituted. |
| A09 frozen independent review | Exact r1 candidate reviewed; known failures prevent SnapshotApproval. |
| A10 publication | Not attempted or authorized. |

## Preliminary observation dispositions

PF01 linked inputs, PF02 binding errors and PF06 indexers have concrete implementation corrections and the independent production audit reproduces their reported expanded census/zero errors. PF03 now includes refreshed attacks, but R1-F04 shows remaining unauthorized-mutator rejection failure. PF04/PF07 gained explicit conservative alias/dispatch/capture treatment; no runtime isolation follows, and web async lifetime is missing. PF05 publication/history/application prose is corrected; transitive member placements still need R1-F01/R1-F02 correction. No preliminary finding is silently upgraded to full semantic Pass.

## Reuse, runtime and publication limits

No earlier runtime pass was reused to close a criterion in this review. Unchanged runtime behavior is claimed only as the candidate's declared scope, not as a new browser qualification. Chrome/Playwright input stayed exclusively with root. The reviewer performed no runtime edit, game UI action, export or deployment. Existing native/browser/device failures remain open with their named owners.

The independent probe lives under tools/p0-004-review, outside the implementation allowlist and the game Compile closure. Its source is review-only evidence, not a deliverable fix. All review-evidence writes passed Anvil after the earlier preliminary missing-operation request was corrected; no policy bypass occurred. The implementation owner may now fix the four findings, retain r1 and request independent re-review. No successor may begin on this verdict.
