# Current coverage inventory

The [current engine contracts](../engine-contracts.md) and current requirement sources define intended behavior. This inventory tracks membership and proof state; it does not turn archived reports or retained trace records into executable prerequisites.

The [engine capability index](engine-capabilities.json) names 54 strict bounded shards: 71 capabilities, 1,471 source bindings/consumers and 104 current catalogue configuration/mode records. The sources are 799 task specifications, 216 EL, 37 TH, 22 RAD, 18 GAP, 72 catalogue entries, 302 authored fixtures and five linked research documents. Authored fixtures are discovered regardless of locked state.

[Source inventory](source-inventory.json) retains all 1,471 records unreviewed; current source-path/hash reconciliation and outstanding drift are described below. [Catalogue mapping](catalogue-elements.json) retains 72 catalogue entries, 302 fixtures and 1,097 unresolved non-catalogue obligations. Its 72 mode reviews and 72 process reviews remain pending. These separate manifests deliberately do not import a capability mapping as a behavioral pass.

```sh
dotnet build tools/Coverage.Tests -c Release --no-restore
dotnet tools/Coverage.Tests/bin/Release/net10.0/Coverage.Tests.dll -noColor
dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll audit-capabilities . docs/coverage/engine-capabilities.json
dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll audit-inventory . docs/coverage/source-inventory.json
dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll audit-elements . docs/coverage/catalogue-elements.json
```

Exit 0 for capability audit means structurally current inventory only. Missing/stale source membership returns 2; malformed schema, invalid semantic relationships, owners, dependencies or source artifacts return 1. RuntimeQualified and CompletionProven remain false. The tool does not parse every requirement from arbitrary prose; independent source-to-child/law review is mandatory.

[Pre-P0-002 README](history/pre-p0-002-README.md) and its manifests retain previous counts, stale SpringPart observations and historical publication claims unchanged. They are historical evidence, not supported current manifests. The finite spring replacement and shared runtime migration are not reimplemented by this inventory.

No current part/mode proof, worker integration, integrated 60/90 FPS tier, physical-device qualification or 150-level campaign gate closes from these records.

Historical P0-002 audit provenance is retained in its separate verification record. Current integrated qualification requires the actual candidate and relevant current source identities; tool tests or publication receipts alone do not establish it.


## Current documentation reconciliation

The 3 October documentation cleanup refreshes only changed current requirement/research source titles and fingerprints, matching source bindings, and navigation to relocated records. Current model contracts and acceptance live in the working documentation. Every source, owner, capability, part, fixture and mode record remains; no implementation, proof or publication state is upgraded. Retired Task 057/133/187 retain structural trace records but create no executable prerequisite; their current no-action disposition and removed dependency edges remain in current planning documents and the obligation map.

Current read-only audits retain failures: source inventory exits 2 (1,471 unreviewed records, 127 changed, zero missing/orphaned); catalogue audit exits 2 (72 elements, 302 fixtures, pending mode/process reviews). Capability audit exits 1 with complete current unresolved source/proof-role and unreviewed implementation-role diagnostics; it no longer reads the deleted register. Planned catalogue declarations preserve all 104 modes independently of resource availability, while admitted parts require actual resource/scene/script bindings. New current declaration and referenced acceptance hashes make prior catalogue evidence stale; no proof state or manifest was upgraded. These are current execution/fixture reconciliation obligations under the owning engine work, not documentation-source drift and not GlobalCurrent or runtime qualification. The cleanup does not refresh runtime symbol fingerprints into apparent approval. The current owner, blocker and next check remain in [TODO](../../TODO.md).

The 2 October workflow and GPU transition results remain historical verification records. This page is current tool guidance, not a continuation of their pass claims.
