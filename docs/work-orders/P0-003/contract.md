# P0-003 — current workload and measurement freeze

Design/tooling stage; implementation in progress, independent review and publication pending. This is the current executable measurement contract, extending the retained [S002 manifest](../../s002-benchmark-manifest.md). Historical evidence remains unchanged. No runtime, browser, device or campaign qualification is claimed.

Implementation owner: /root/p0_003, provider session 01a0f786-9175-7532-9b17-c30b3d21388c; parent 01a0f5f1-ab9a-79a3-9181-0dbb87285ec5. Starting HEAD 9d1d8f8e5567949830c70836a964450378fd3b7c. Prerequisite: [P0-002 terminal independent review](../../verification/P0-002/review.md), deliverable 61e35b3 and published review/status 76cd6218/9d1d8f8. Starting source hashes are in [identity.json](../../verification/P0-003/identity.json). The working runtime is not reproducible from HEAD alone.

## Scope and authority

[TODO P0-003](../../../TODO.md#work-p0-003), [measurement contract](../../../TODO.md#measurement-contract), [worker budgets](../../../TODO.md#worker-performance-budgets), [REQ-14](../../../TODO.md#paired-subagent-workflow) and [performance design](../../browser-physics-performance.md) are binding. This row freezes parameters, populations, devices, arithmetic and stage gates; it does not implement physics models, generic fixture UI, workers or final CHECK-AGGREGATE certification.

Allowed deliverables: this directory; docs/verification/P0-003 implementation evidence; the existing tools/Performance project (Program.cs, Performance.csproj, PerformanceAudit.cs, README.md) plus new MeasurementContract.cs; tools/Performance.Tests project/tests; inherited CuriousContraptions.tests/PerformanceAuditTests.cs; the13 affected PerformanceAudit symbol references in docs/coverage/engine/task-001.json, task-002.json and task-003.json; narrowly scoped current TODO handoff/status. No game source, current content, production assets, historical evidence, AGENTS or unrelated migration changes. The existing tool and reader tests were untracked at entry; publishing them does not imply the linked runtime contracts were published. Three exact runtime-source copies are retained only as immutable verification snapshots (.cs.txt), never compiled by the normal project or used as a fallback. Ordinary clean-checkout tool and integrated-game reproducibility remain Incomplete until the separately reviewed runtime prerequisites are published. A scratch build reconstructed from evidence is labeled reconstructed, not clean-checkout reproduction. Reviewer writes only independent review evidence. The local TODO update is reviewed but excluded from publication because the large external register rewrite remains uncommitted.

Current implementations retained: PerformanceAudit.Analyze and ReadBrowserBatches produce typed diagnostic coverage and inclusive elapsed distributions, not browser qualification. Program.cs is their sole production CLI caller; PerformanceAuditTests and DisplayClockIntegrationTests are test callers. MeasurementContract extends that same audit owner with arithmetic oracles and strict evidence DTOs. There was no existing actual-presentation acceptance function to replace. Existing CLI arguments/output keep their narrower meaning; historical formats remain unsupported. The new standalone test project runs the existing reader tests too. The main game excludes tools/**/*.cs, so this scope cannot change a game bundle.

Current review revision: [snapshot-r2.json](../../verification/P0-003/snapshot-r2.json), including reviewer /root/p0_003_review, provider session01a0f79a-73eb-7ed0-b090-5c9ff8dabf8c. The original snapshot.json and original logs remain the failed first review revision. Current dispositions come from the independent review, not these implementation notes.

At the revised frozen boundary, source inventory and catalogue membership/deletion audits remain current. The capability audit initially returned exit1, “Stale source symbol,” because this row changed the shared PerformanceAudit declaration. This directly affected drift is owned and resolved here: refresh13 existing symbol hashes across three shards and add the new partial source to those same consumers. Their capability, source, mode and owner semantics stay unchanged. Re-audit the full current capability index before review. This is a scoped current-view refresh, not global runtime qualification or a rewrite of historical P0-002 proof. CHECK-METRICS/AGGREGATE and P0-034 retain future enforcement; unrelated unpublished runtime input prerequisites remain P0-029/035.

## Frozen acceptance set

| ID | Required now / independent oracle | Evidence / publication phase |
| --- | --- | --- |
| P0-003/A01 | Every required workload has exact geometry/population/control, declared outcome/minima and named missing UI prerequisite. No missing size/rate/units. | workloads.md; pre-publication design review |
| P0-003/A02 | Every support class has a chosen physical configuration and explicit access/identity prerequisites; all budgets, conservation limits, startup and memory caps frozen. | devices-budgets.md; pre-publication design review |
| P0-003/A03 | Complete fresh Run, prior complete warm-up, early goal, timeout, thermal and noise oracles are unambiguous. | measurement.md; independently calculated timestamp examples |
| P0-003/A04 | Typed strict schema rejects missing/unknown/undefined/null inputs; no string domain selectors. Coverage, throughput and presentation remain distinct. | focused C# tests and enum/caller audit |
| P0-003/A05 | Rehearse 3600-tick timeout, early win, warm-up/fresh Run, dropped gap, slow simulation; reject stitched/insufficient observations. | reviewer independently derives results; implementation test log |
| P0-003/A06 | Existing diagnostic reader behavior preserved; all affected tooling compiles and its old/new tests pass. No runtime source changed. | Release tooling build; existing PerformanceAuditTests + new tests |
| P0-003/A07 | Current source requirements, links, 4558 orders, 799 specification anchors, 847 original sequence anchors and 150-level scope preserved; impact matrix has no unresolved scope regression. | preservation/link/caller checks |
| P0-003/A08 | Distinct actual reviewer identity, exact snapshot/diff/contract/artifact hashes and independent criterion/impact matrices; all pre-publication findings resolved. | independent review; SnapshotApproval only |
| P0-003/A09 | Approved implementation files match commit and remote branch; reviewer confirms receipt against approved hashes. | publication-dependent; terminal Design/tooling Pass only afterward |

A09 is the only publication-dependent criterion. No deployed behavior is needed for this non-runtime tooling/design change; P0-029/034/035 own production-origin behavior and cannot substitute a receipt. No successor until terminal independent Pass. All actual runtime qualification remains open.

## Second-order impact expectations

| Reachable scope | Intended invariant/change | Check |
| --- | --- | --- |
| Diagnostic CLI → PerformanceAudit → linked protocol/contracts | Original typed record coverage and elapsed-stage reports unchanged; no browser Pass from complete ticks | Compile CLI; all existing reader tests |
| Main native test project → linked PerformanceAudit; DisplayClockIntegrationTests | Partial declaration does not require new methods/source in the old caller; original signature remains | Compile affected main test callers; no runtime changes |
| Measurement DTO → JSON → arithmetic methods → tests | Reject unknown enums/fields, missing identities and malformed intervals; never promote synthetic values to observed browser proof | Positive and mutated boundary fixtures |
| Future CHECK-METRICS/AGGREGATE, P0-034 and device gates | Consume these frozen oracles; preserve separate coverage/behavior/performance/publication | Source/link consistency review; future enforcement remains open |
| Future ENGINE-FIXTURE-UI/law fixtures → P0-030/031 | Exact target recipes use ordinary UI, no setters/imports or hidden longer Run | Workload/prerequisite review; missing fixture capability is Incomplete |
| Worker clocks, lifecycle, renderer, content and saves | No runtime edits; new contract requires separate clocks, finite memory, exact restoration and full timeline | Source-diff bounds; retained source hash continuity |
| Historical S001/S002 and retained failures | Byte preservation; no rewritten old claims or fitted thresholds | Artifact hash comparison |
| TODO current handoff/register | Only P0-003 status/current handoff; preserve every requirement/anchor | Exact small patch + structural counts |

## Commands

Run serially; no browser timing during build/test:

```sh
dotnet build tools/Performance.Tests/Performance.Tests.csproj -c Release --nologo
dotnet tools/Performance.Tests/bin/Release/net10.0/Performance.Tests.dll -noColor
dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Release --no-restore --nologo
```

The main build verifies existing linked callers, not native physics correctness. No unchanged physics suite rerun is represented as a new browser result. Runtime proofs later require both production and diagnostic Release exports, current Chrome/Playwright ordinary actions, exact Run/Reset/save and identity-bound integrated costs. Freeze snapshot before measurements; never fit thresholds to results.
