# P0-004 independent pre-freeze observations

Reviewer: /root/p0_004_review; actual thread 01a0f7d2-92c9-7e11-9fbf-3ea247ea179c.
Implementation owner: /root/p0_004; actual thread 01a0f7b9-0ecd-74b2-81ce-361031596a36.
Parent session: 01a0f5f1-ab9a-79a3-9181-0dbb87285ec5.
Starting HEAD supplied to review: eeeb932734ccd9034020592f0da7a87b619cbe3f.

This records observations against an explicitly mutable, unapproved draft, not findings against a frozen snapshot. No SnapshotApproval or terminal verdict is issued. The owner acknowledged these observations and is correcting the candidate before freeze. Their dispositions require independent verification on the eventual snapshot.

| Observation | Evidence inspected independently | Required disposition before approval |
| --- | --- | --- |
| PF01: test input closure omits linked sources | SourceInventory enumerates only CuriousContraptions.tests/**/*.cs; its project also Compile-links tools/Playtest/Audit.cs and tools/Performance/PerformanceAudit.cs. | Resolve actual Compile/reference/configuration inputs and retain source/generated-input bounds. |
| PF02: semantic binding uncertainty is unmeasured | Draft combines production and test syntax against production references and does not inspect compiler diagnostics. | Record diagnostics; resolve missing references and reconcile unresolved/generated cases without treating erroneous binding as exact. |
| PF03: unauthorized writer oracle currently tests staleness | Oracle changes observed Uses but not MemberSha256. A refreshed self-consistent mutation is not covered. Same distinction applies to ReferenceContentsOmitted. | Distinguish drift from semantic invalidity and independently challenge valid-but-wrong assignments. |
| PF04: alias and dispatch references are not a complete caller graph | Classify treats assigning/passing a mutable member as Read; mutation through a local alias lacks the original member name. Virtual/interface/delegate callers are outside SimpleName enumeration. | Concrete ownership/dispatch/alias closure and bounded claims; no inference of worker isolation. |
| PF05: extraction/application ownership differs from publication/history | Initial assemblies.md table points some extraction to P0-029 and application to P0-025. TODO defines P0-029 publishing, P0-025 histories and P0-026 final application. | Final map must separate extraction, consumer, publication and proof owners. Owner had independently identified this draft issue. |
| PF06: indexer properties and callers omitted | SourceInventory handles PropertyDeclarationSyntax, not IndexerDeclarationSyntax. SceneBodyGeometry.this[ColliderChildId] and CompoundGeometry.this[ColliderChildId] expose reference-bearing geometry; element-access callers are not SimpleNameSyntax. | Include/reconcile both properties and their callers with source-derived ownership. |
| PF07: escaping captures are not always invocation scratch | BodySlot owns provider delegates; SceneRotaryShaft.Slot captures provider/value parameters; PositionProjector retains PositionContactQuery. | Explicit target treatment: browser capture providers become values, simulation callbacks remain within its owner; no delegated scene/runtime references on wire. |

Read-only commands included source reads of tools/Ownership/*.cs, the production/native test projects, AGENTS.md, TODO stage/P0/reuse contracts, SimulationTransaction, SimulationTimers, BodyBoundsTree, SceneOccurrenceRun, SceneCollisionGeometry and CompoundCollision. Anvil symbol lookup returned ready with a paginated best-effort graph; no complete caller claim relies on that page. Source preprocessor search found additive PLAYTEST, TWODOG_WEB_BOOT and DEBUG || PLAYTEST branches, with no #else/#elif in the inspected production tree; final configuration closure still needs a frozen identity.

No build, test, browser action, source mutation or publication was performed by this reviewer during this preliminary inspection. Runtime qualification remains outside this Design result and unresolved. Final review will independently execute affected checks after the implementation owner's stop-edit notice and frozen snapshot.

