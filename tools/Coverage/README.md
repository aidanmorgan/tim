# Coverage source inventory

A read-only C# preparation tool for TODO's canonical coverage ledger and PERF-23. It discovers source obligations and checks a saved inventory for missing, orphaned, duplicate or changed records. It does not modify game state.

```sh
dotnet build tools/Coverage.Tests -c Release
dotnet tools/Coverage.Tests/bin/Release/net10.0/Coverage.Tests.dll -noColor
dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll /path/to/repository
dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll /path/to/repository /path/to/inventory.json
```

One argument prints a newly seeded JSON inventory to stdout. Two arguments audit an existing inventory against freshly discovered sources. Exit 0 means successful discovery or current source inventory; exit 2 means missing, orphaned or changed sources; exit 1 means invalid input. No command overwrites an inventory or migrates formats.

Discovery covers numeric element, thermal, radiation and GAP anchors in TODO.md, every parts/catalog/*.tres definition, every locked authored fixture instance in content/puzzles.json, and every docs/*research.md contract. Counts are discovered, never fixed. Catalogue entries and fixture instances remain separate obligations; matching titles never imply equivalent elements or shared proof. A changed requirement block, definition, fixture configuration or research contract invalidates its saved content hash.

Records retain typed source identities, origins and independent mapping, implementation, correctness, UI, performance and publication states. Enum strings are allowed only at the strict JSON boundary; casing variants, integers, undefined values, missing constructor fields and unknown members reject. Identity/hash formats validate separately. Evidence state Recorded means a claim that evidence exists, not validated correctness.

**This is the source-inventory layer, not the completed element/process manifest.** Every seed record starts unreviewed and has no attached proof. Missing evidence means no evidence has been linked into this inventory; it does not assert that no historical evidence exists elsewhere. Research documents are whole-contract obligations pending individual candidate/mode reconciliation; prose requirements outside the selected anchors still need semantic review. Next layers must reconcile canonical elements and duplicates, split supported modes, map processes/contracts/prerequisites, and attach current independent evidence and published revisions. Do not count source rows as distinct implemented parts.

SourceInventoryCurrent only verifies the scoped inventory against input files. CompletionProven is deliberately always false: this tool cannot verify physics, actual UI behavior, performance, publication or full scope. Even marking every record Reviewed/Present/Recorded/Pushed cannot produce completion. No part/family or TODO checkbox closes from this report.

## Verification checkpoint

29 September 2026, .NET 10.0.12, xUnit 3.2.2. Debug build: zero warnings/errors; 8/8 tests, 0.105 s. Final Release build: zero warnings/errors; 8/8 tests, 0.104 s. Tests cover separate identities, ignored navigation anchors, duplicate/empty requirements, distinct fixtures, configuration invalidation, missing/orphaned/changed records, unsupported internal states, all canonical enum mappings, required/unknown JSON fields, invalid identities/hashes, and refusal to infer completion from claimed statuses.

Read-only execution on the then-current uncommitted migration discovers 669 sources: 216 element, 37 thermal, 22 radiation, 18 GAP, 72 catalogue, 300 fixture and four research contracts. Audit reports all 669 unreviewed, zero changed/missing/orphaned and CompletionProven false. These are dated source counts, not baseline-commit counts, implementation counts or current proof. The local inventory is separate from this independently buildable tool publication because the underlying migration inputs are not yet published.

Retained exploration failures: an initial console capture was truncated by a 1000-token output limit; it was rerun with adequate capture and parsed completely. Earlier shell exploration used a nonexistent levels directory and an unmatched engine/Ids* glob; no source was written by those commands. All test executions passed. This tooling change does not change a game part or simulation; native tests do not substitute for the still-required per-part browser evidence.
