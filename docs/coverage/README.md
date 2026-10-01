# Current coverage inventory

P0-002 implementation snapshot: [contract](../work-orders/P0-002/contract.md), [derivation and limits](../verification/P0-002/derivation.md). Initial independent review failed; the forward-corrected snapshot awaits re-review and publication.

The [engine capability index](engine-capabilities.json) names 54 strict bounded shards: 71 capabilities, 1,471 source bindings/consumers and 104 current catalogue configuration/mode records. The sources are 799 task specifications, 216 EL, 37 TH, 22 RAD, 18 GAP, 72 catalogue entries, 302 authored fixtures and five linked research documents. Authored fixtures are discovered regardless of locked state.

[Source inventory](source-inventory.json) is current with all 1,471 records unreviewed. [Catalogue mapping](catalogue-elements.json) retains 72 catalogue entries, 302 fixtures and 1,097 unresolved non-catalogue obligations. Its 72 mode reviews and 72 process reviews remain pending. These separate manifests deliberately do not import a capability mapping as a behavioral pass.

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

The working integrated audit requires the exact unpublished input snapshot recorded in [input provenance](../verification/P0-002/input-provenance.json). A remote clean checkout remains Incomplete for that audit until those inputs are independently published. Tool build/tests and scoped publication receipts do not prove remote integrated currency.

