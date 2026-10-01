# Coverage preparation checkpoint

**Current status, later 29 September 2026:** Refreshed manifests audit current: 671 source records, all unreviewed; 72 catalogue entries; 302 locked fixtures; 297 unresolved source obligations; 72 pending mode and 72 pending process reviews. SourceInventoryCurrent and LinksCurrent are true; CompletionProven remains false. Previous manifests are retained unchanged in [history](history/). Counts and SpringPart inspection below are historical checkpoints. The finite spring replacement, 62-level authored stage and subsequent shared cache verification are recorded in [current verification](../prediction-samples.md). Generated manifests and engine changes remain unpublished; canonical mode/process mapping, per-part proof, performance and delivery remain open.

29 September 2026. Partial execution-preparation / PERF-23 work.

The independently buildable [C# source-inventory tool](../../tools/Coverage/README.md) and eight tests are committed and pushed as [f006bd2e9bb62558c45e8eb8067af28694a87a1d](https://github.com/aidanmorgan/tim/commit/f006bd2e9bb62558c45e8eb8067af28694a87a1d). The pre-commit policy reported zero findings; push succeeded and git ls-remote confirmed that exact main revision. This publication contains only the eight tool/test/README files, not the pending engine migration or generated input inventory.

[Source inventory](source-inventory.json) records 669 obligations from the working migration: 216 element anchors, 37 thermal anchors, 22 radiation anchors, 18 GAP records, 72 catalogue definitions, 300 locked authored fixtures and four complete research contracts. Every record is explicitly unreviewed. These are source records, not 669 distinct elements or implemented processes.

The local inventory is not yet published because the underlying TODO, research, catalogue and campaign migration inputs are not all committed. Their per-record content hashes allow changed or missing inputs to invalidate inventory currency. The tool's published revision does not identify the uncommitted game implementation.

Reproduce current discovery/audit with the commands in the tool README, using this repository as the root and docs/coverage/source-inventory.json as the audit input. Final Release build has zero warnings/errors; 8/8 tests pass, 0.104 seconds. The current audit result is:

```json
{
  "Sources": 669,
  "Unreviewed": 669,
  "Changed": 0,
  "Missing": 0,
  "Orphaned": 0,
  "SourceInventoryCurrent": true,
  "CompletionProven": false
}
```

Retained command failures: discovery output initially exceeded a 1000-token console limit; a complete capture succeeded. A later attempt to read the entire large TODO through a 60000-token console response also truncated and failed JSON parsing; the update used a bounded line/patch instead. No incomplete parse was written. Earlier nonexistent-directory/unmatched-glob explorations and all test results are recorded in the tool README.

## Current next action and unclosed gates

Reconcile these source obligations into canonical individual element/mode records, beginning with the existing catalogue and shared mechanical foundation. Map duplicates explicitly while retaining every source obligation. Add process/material/controller contracts, units and limits, prerequisites, implementation symbols, geometry and animation classification, independent positive/control/boundary/UI/Reset/save evidence, performance qualification and published revision for each record. Split supported modes and research candidates individually; whole-document research records are unresolved obligations, not completed candidate enumeration.

The first validator does not discover every semantic requirement from arbitrary prose. Unlocked preplaced instances and research candidate/mode splitting still need explicit scope reconciliation. Inventory currency does not prove implementation, physics, actual UI behavior or full coverage; even claimed Recorded/Pushed statuses never yield CompletionProven. Native tool tests do not replace mandatory game/browser proofs.

Shared foundation readiness remains unproven: bridge and separate-animation implementation, remaining generic processes, current per-part proofs, outstanding contact failures and engine publication remain open. PERF-01/02 qualification, integrated-GPU laptop and physical Pixel 8 Pro measurements remain open. No element, family, P0 requirement or campaign gate closes from this checkpoint.

## Catalogue/fixture mapping increment

The next validator increment is committed and pushed as [3cdb6f85754a9c02fd106e9e7151ad3b337b9012](https://github.com/aidanmorgan/tim/commit/3cdb6f85754a9c02fd106e9e7151ad3b337b9012); remote main was checked against that exact hash. Six tool/test/README files were published. Pre-commit policy: zero findings. Final Release build: zero warnings/errors; 16/16 tests, 0.096 seconds.

[Catalogue elements](catalogue-elements.json) retains 72 distinct catalogue identities, 300 individual fixture bindings and all 297 remaining source obligations. It does not merge similarly named/scripted variants. Fresh audit reports LinksCurrent true, 72 pending mode reviews, 72 pending process reviews and CompletionProven false. The original 669-source inventory also remains current and entirely unreviewed. Both generated artifacts and the migration inputs remain uncommitted.

Current commands use explicit enum-checked operations:

```sh
dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll audit-inventory . docs/coverage/source-inventory.json
dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll audit-elements . docs/coverage/catalogue-elements.json
```

The former positional-only invocation, an unknown operation and an audit invocation missing its manifest each exit 1 with an explicit error. No alias or format inference remains. Added tests reject wrong/duplicate fixture ownership, missing/changed records, hidden catalogue entries and unsupported mode/process review claims. Catalogue evidence cannot qualify fixture behavior; fixture proof remains its own source obligation.

### Next engine implementation slice

Source inspection confirms that [SpringPart.PhysicsImpact](../../parts/SpringPart.cs) still computes a target-relative launch impulse in a part callback. [EL-194](../../TODO.md#element-194) instead requires finite elastic storage, physical compression/release, and no free launch on uncharged or missed contact. This is a concrete PERF-24 law-ownership and element-contract mismatch, not a performance-tuning issue.

Review the existing generic compliant/elastic/load/store capabilities and the linked springboard contracts before changing its declarations. Implement any genuinely missing reusable process, remove the bespoke callback path when replaced, and prove energy limits, contact/miss/uncharged cases, integration, boundaries, exact Reset/save and actual UI behavior. Current historical springboard launch evidence cannot establish the new finite-store contract. Preserve artwork/palette while separating functional contact motion from cosmetic recoil.

Continue remaining canonical source/mode/process reconciliation alongside shared foundations. The ledger does not postpone bridge/animation, generic processes or component coverage until all metadata is polished. No part or foundation milestone is complete, and the engine migration still needs verified publication.
