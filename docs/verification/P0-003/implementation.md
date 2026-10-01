# P0-003 implementation evidence

Implementation session: 01a0f786-9175-7532-9b17-c30b3d21388c, /root/p0_003. Independent reviewer not yet assigned; all acceptance claims here are implementation observations, not terminal Pass.

- Release tooling build:0 warnings/errors, final2.03s. Full raw test output in tool-tests.log:30 tests,0 failures,0 skipped,0 not run; final0.179s.
- Affected main-test Release build:0 warnings/errors,23.90s; raw main-build.log. This compiled existing PerformanceAudit/DisplayClockIntegration callers; no physics suite or browser result inferred.
- Anvil anti-pattern check on all five C# source/test paths:0 warnings, local backend, daemonStatus not-wired. This is scoped static evidence, not repository-wide enum compliance.
- preservation.json retains4558 orders,799 specification anchors,847 original execution anchors,0 undefined lines,0 missing local contract links and byte-identical three source snapshots.
- Current game/browser remains unqualified. No UI input, new browser measurement, runtime code edit or production export occurred in this Design/tooling work.
- Retained failures and boundary attacks are specified in the contract/test cases; no new test/build failure occurred. Initial source-snapshot copies had one added trailing newline from patch application; byte equality check caught it and all three were corrected before review. No false exact-hash claim was published.

## Inherited versus new scope

Existing, previously untracked: tools/Performance/Program.cs, Performance.csproj, PerformanceAudit.cs, README.md, and CuriousContraptions.tests/PerformanceAuditTests.cs. The only inherited reader-code edit is making PerformanceAudit partial; README adds the current contract and reproducibility limits. New: MeasurementContract.cs and tools/Performance.Tests. Existing reader tests are compiled into the standalone test project; no duplicate supported reader implementation.

The three runtime-linked source files remain unpublished: engine/PerformanceContracts.cs, diagnostics/PerformanceProtocol.cs and diagnostics/ExactPlaytestEnumConverter.cs. source-snapshot preserves exact .cs.txt bytes for verification only. The normal project has no snapshot fallback. A normal clean-checkout tool build remains Incomplete pending separately reviewed runtime publication at P0-029/035. The in-workspace build uses the actual current sources.

A reviewer may reconstruct a scratch evidence rehearsal by copying the approved tools/Performance and tools/Performance.Tests source/project files plus the inherited PerformanceAuditTests.cs into their original relative paths, then copying each source-snapshot/*.cs.txt to the corresponding engine/ or diagnostics/ *.cs path. Build the unchanged Performance.Tests.csproj there. No project-path rewriting, source modification, runtime substitution or repository mutation is needed. Such a run is a **reconstructed source-evidence rehearsal**, not ordinary clean-checkout or game/production reproducibility. Capture exact commands, hashes and scratch path.

## Typing and meaning review

AttemptPurpose, AttemptEnd, CadenceTier, PresentationEvidence, MetricVerdict and TimelinePhase are enums through records, comparisons and APIs. AttemptId and PresentationId are distinct typed identities. Collections use those identity types; slot indices are local arithmetic indices. Raw field names and deliberately invalid enum strings occur only in the explicit JSON-boundary tests. ExactPlaytestEnumConverter is the existing canonical conversion boundary. No alias, case-folding fallback or retired schema acceptance was introduced.

The original Program calls only diagnostic Analyze/ReadBrowserBatches. Its Complete enum means complete tick-record coverage, as before; it never produces a browser performance Pass. The new helpers are invoked by schema validation and rehearsal tests and deliberately do not implement authenticated CHECK-AGGREGATE certification. The future checker must additionally verify raw provenance, per-attempt joins, device capabilities, all cost budgets and publication identity.

## Current-stage dispositions

A01–A03: finite design delivered for independent review; required fixture controls/devices/observability remain explicitly named future prerequisites.
A04–A06: implementation tooling checks pass; independent reproduction pending.
A07: structural/link/byte-preservation observations pass; independent impact review pending.
A08: independent SnapshotApproval pending.
A09: commit/push receipt pending, no publication authorized.
