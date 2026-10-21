---
title: '9A.45a Early Element Isolation Ratchet'
type: test
created: '2026-10-22'
status: review
route: dispatch
baseline_commit: e923349524ae38295054e08fcc2c23175719d388
context:
  - AGENTS.md
  - docs/architecture/element-package-isolation.md
---
## Intent
Make existing element-specific production code an owned, shrinking debt list. Reject new element knowledge outside packages without changing gameplay. Exact epics Story9A.45a and Story9A.45 scan scope govern this slice.

## Scope and design
Use existing native xUnit/Roslyn and generated TestBundle entries. Scan production C# and shipped JavaScript/TypeScript beneath engine, ui and CuriousContraptions.* projects excluding tests and build outputs. Include web/Simulation wwwroot and Animation worker. Explicit exclusions: elements owners; tests/tools/e2e machine-building consumers; content level data; tools/Coverage nonshipping planner; docs/_bmad-output/archives. Generated bundle IDs and package aliases supply recognition, not a hand-maintained catalogue.

Find element-id string literals, PartId named accesses, package record/type patterns (including is/OfType/switch), and package-derived identifiers. Retain file:line diagnostics and group current hits by file/symbol/rule/token with exact counts. Checked-in entries use typed owners mapped to actual sprint keys. Known entries must match, may only shrink, and fail if their owner is done. Baseline covers CaptureInstance arms, all remaining element panels, compiler/ActivationNetwork/instance validation arms; each deletion belongs to its existing named story.

Compare current entries to the prior committed list: dirty candidate against HEAD; committed candidate against first parent. First introduction accepts only the pinned initial census identity; later missing history fails with a corrective instruction. CI fetches two commits, no network repair in tests. Membership/owner/count cannot widen even if another entry shrinks. Existing two Story9.4b solver pins retain Story9A.19 and acquire the same prior-list rule.

Generic capability meanings use exact file/symbol/token/count contexts and never suppress literals or named PartId accesses. New reviewed generic contexts are allowed; current or prior Known entries cannot be laundered into Generic. Known violations alone obey historical shrink.

## Tasks
- [x] Add bounded source scanner and exact checked-in owned census; preserve all current production behavior.
- [x] Add strict prior-list, count/stale/duplicate/undefined-owner/missing-status/done-owner checks, including solver pins.
- [x] Prove positive current source and planted ID/type, addition/replacement/widening, removal/re-add, stale and done-owner negatives through the same scanner/validator.
- [x] Delete one-time census/owner-generation plumbing and obsolete parallel scanner paths; retain only the checked-in reviewed census.
- [x] Align latest package-isolation design: declared generic capabilities remain valid, actual element knowledge retains named deletion owners; shared f32 migration remains9A.44.
- [x] Build solution once per relevant input change and run native tests with --no-build --no-restore. C# UNIT tests are PRIMARY proof; integration is supplemental. Routine Chrome/E2E is deferred until ALL catalogue elements are implemented; no new harness.
- [x] Carry terminal9A.27 receipt and update short TODO/sprint.
- [ ] Independent SnapshotApproval then protected publication and exact remote/CI/deployment checks.

## Baseline and evidence
Read-only preliminary census:22 generated packages,197 production C#/JS/TS files,345 literal/member/type hits in39files (tool8dd4de). This is not the final syntax-aware baseline. Existing solver pins:4 gate numeric literals plus2 fSpring identifiers, owner9A.19.

Final census:1025 Known entries and533 exact Generic contexts. Source traversal prunes excluded roots before descent. Existing two solver pins remain unchanged and owned by9A.19; active family completion also rejects remaining pins.

Matching-input solution build: `dotnet build CuriousContraptions.slnx -v:q`,0warnings/errors,4.96s (toolff6cf8). Focused `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter FullyQualifiedName~ElementIsolationTests -v:q`:21/21,608ms (tool03991c). Required `dotnet test CuriousContraptions.slnx --no-build --no-restore -v:q`:2452/2452, main2035/2035 in35s (terminaltool1f22f8; project outputs f6ef07/97e9c7). Controls cover escaped JS/template literals, qualified/aliased PartId, arbitrary package-word identifiers, generic-context new names/literals/members, exact counts, owner completion, historical additions/re-additions and Known-to-Generic laundering.

Original development results retained: initial missing-list failures and alias/pattern findings led to causal fixes; preliminary census was not accepted. Focused context proof21/21 in618ms (toola3eaf0) preceded the final capability-classification correction; final matching21/21 supersedes it. No production code changed and no browser qualification is claimed.

## Risks and boundaries
No global isolation claim: existing owned hits remain until their named conversions, final9A.45 empties the list. Shared f32/ABI migration remains9A.44. Native-only structural slice; browser qualification remains post-catalogue. Preserve unrelated main dirt and existing review evidence.
