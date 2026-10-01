# S001 — untouched baseline

Scoped verdict: PASS for baseline capture, accepted by the coordinator. Publication remains pending. This records the current baseline; it does not accept the runtime migration, close PERF-01/02/39, or claim a speedup. No production, test or tooling source was changed for S001.

## Identity and preservation

Tested HEAD: `0c3cf53428d2b0eb7b4b9b5e8764bab512be3d7b`, plus the existing uncommitted migration. [Source baseline](s001-source-baseline.json) records 916 source entries, SHA-256 for each, tracked dirty-diff SHA-256 `ecb4dd2ba53b26ea45b5c8205b9cd90a6e5a161ed4ae79c8719769d1129b4a33`, and source-manifest digest `68a68a6589944ef576f667af22d9ba2a21606e2288ef6364342ba5b23d6ab693`. Independent post-test comparison found zero source-hash mismatches. The selected manifest includes runtime, tests, content and resources; it does not inventory every browser automation script or every dirty document. A hash manifest identifies this worktree; it is not a source archive or reproducible published runtime revision. The tracked diff also cannot substitute for the untracked source entries.

Current production bundle hashes match the previously served production evidence: godot.pck `c4750a03507b9aba726ebd8695d08b54c432440c299f42f3bca2aeec06bdfeb0`; managed WASM `4d126142a28fabcc675a5c63e56907cff25fc0c4d99e913ba77c2d197c1ad784`. Diagnostic pck `849da82d3a58cb413a4a76097aaacea44a86b94a720a01d60b56b23004857b31` is a different build mode. Production export success is retained in [export log](committed-compliant-browser-production-export.log); no needless re-export was performed for this source-unchanged audit.

Environment: MacBook Pro M4 Max, 64 GB, macOS 26.5.2, Chrome 154.0.8037.58, 1440×900 viewport at DPR 1. Timing is Chrome RAF callback pacing, not GPU service or distinct presented frames. Physical Pixel 8 Pro, integrated-GPU reference qualification, power/thermal characterization, worker execution and sustained 60/90 FPS qualification remain unavailable or unproven.

The referenced compliant diagnostic console is archived in [compressed raw log](s001-compliant-console.log.gz), with [original line mapping and SHA-256](s001-compliant-console.json). It preserves original lines13287–14781; subtract13286 to index this archive. Binary snapshot validation used a partial preview plus SHA-256, not a full-content secret scan.

## Native baseline

Commands, executed separately:

```sh
dotnet build CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Release --no-restore > docs/s001-native-build.log 2>&1
dotnet CuriousContraptions.tests/bin/Release/net10.0/CuriousContraptions.tests.dll -noColor -class '*BellowsTests' -class '*WoundSpringRuntimeTests' -class '*CommittedCompliantContactTests' -class '*WorkshopAnimationTests' -xml docs/s001-native-baseline.xml > docs/s001-native-baseline.log 2>&1
```

Build: exit 0, zero warnings/errors, 16.69 seconds. Tests: exit 1, 76 total, 66 passed, 10 failed, 167.619 seconds. Scope: Bellows 39, WoundSpringRuntime 15, committed-contact 13, workshop-animation 9. [XML](s001-native-baseline.xml), [raw log](s001-native-baseline.log), and [exact failure messages/stacks](s001-native-failures.json) are retained.

The five WoundSpring failures are two released-head energy mismatches (expected 51.200000725878958 J; observed 51.500738503661267/51.5007385030748 J), two power-loss charge drifts of approximately 7.75e-9 J, and one constraint right-hand-side dynamic-range rejection. Bellows has residual stroke 0.00041936039924622692 against maximum 0.0001; two bounded-rate sweep interval-budget errors; and two support-iteration failures in the chime/low-resistance transfer cases. These are live correctness failures, not merely stale render assertions, and no tolerances were relaxed.

## Existing physical positive/control integration

The [current compliant checkpoint](committed-compliant-contacts.md) identifies unchanged-source Chrome evidence: three UI-placed parts (tilted trampoline, ball, receiver), one ball-depth difference, no explicit wire connections, 3,600 ticks per accepted run. Positive reaches the receiver at approximately [5.0501,1.160114,0]; depth control stays approximately x=0 and misses. Both restore exact construction on Reset; positive UI Save/Load restores exactly before Run and after Reset. Recipes/actions and typed construction records are in committed-compliant-recipes.json, committed-compliant-receiver.json, committed-compliant-control.json and committed-compliant-save.json.

Positive diagnostic complete-tick p95 is 17.1 ms, physics 16.0 ms; fresh control 10.5/9.6 ms. Both fail the complete-tick 2.5 ms budget. Publication p95 is 0.2 ms, animation 0.1 ms, submission approximately 0.1 ms; these are individual distributions, not additive aggregate service or GPU timings. The positive's 30 simulated seconds took 57.448 wall seconds; control 36.524 seconds.

Use the first run in committed-compliant-browser-stages.json for positive attribution and committed-compliant-control-stages.json for the fresh complete control. The former artifact's interrupted 1,992-tick control and historical qualification text are superseded by the later evidence, not rewritten. The fresh control's 20 stale prior-run telemetry samples are retained separately and excluded by run identity. Production [pacing](committed-compliant-production-pacing.json) retains a 50-second window including Run lead-in: p95 150.1 ms, p99 159.0 ms, max 241.7 ms. It fails both 60 Hz 18/25 ms and 90 Hz 12/16.7 ms frame-interval targets. One window is not repeated/sustained qualification.

The 55-second positive observer timeout and [unexpected-input incident](committed-compliant-input-incident.md) remain failures. The user was not operating the tab; the unrequested Reset/depth edit/Run source is unresolved. The altered run is invalid evidence. No recurrence is not a fix.

## Smallest workloads and motion

The coordinator used the existing Chrome/Playwright page. This subagent's connector read returned about:blank with none of the page-owned capture state; it performed no input or second-browser experiment. The coordinator retained browser ownership and collected the following artifacts, independently rechecked here from raw timestamps using nearest-rank percentiles of adjacent RAF intervals.

| Workload | Actual UI actions / duration | Intervals; p95 / p99 / max |
| --- | --- | --- |
| Animation-only control | First principles, physics stopped, goal panel open; 5 seconds idle | 599; 9.2 / 9.4 / 10.2 ms |
| Animation-only active | Same state; click hint at (1186,258) 60 times with 500 ms waits; complete capture 35.3763 seconds | 3,639; 9.2 / 9.3 / 51.8 ms |
| Smallest physical | Default First principles ball + receiver, no placed ramps; Run at (688,836), approximately 30 wall seconds, then Reset | 796; 83.6 / 84.3 / 92.3 ms |
| Combined | Same fixture; Run, 30 hint clicks at (1186,258) with 1,000 ms waits, then Reset | 838; 83.7 / 84.1 / 90.9 ms |

Raw timing: [animation](s001-animation-pacing.json), [physical](s001-physical-pacing.json), [combined](s001-combined-pacing.json). The animation file contains 4,240 timestamps and all 60 action times; the split omits the single interval crossing the 5-second boundary. Physical timing elapsed 30.0425 seconds; the subsequent screenshot shows 26.27 simulated seconds. Combined elapsed 33.2669 seconds; subsequent screenshot shows 28.96 simulated seconds. These windows include Run lead-in, lack repeated warmed runs, and fall short of 30 simulated seconds. Neither is a qualification window. Both fail 60/90 Hz pacing budgets. Animation-only callback percentiles satisfy interval limits in this small window, but no distinct-frame, animation CPU, freshness, hidden-work or worker-independence qualification follows.

The [DOM input trace](s001-inputs.json) retains 216 pointer/key events across the recorded session. Coordinator review found a recurrence outside the selected windows: two unrequested click pairs at (675.43359375,822.109375), down/up times 680288.5/680363.3 and 682790.5/682873.2 ms. They occur after animation capture ended at 662570.4 ms and before physical capture started at 815254.1 ms. Neither coordinator nor this subagent requested those coordinates; this subagent made only the read-only about:blank query described above. Attribution remains unresolved and must stay explicit for S007. For the combined timing interval it shows the requested single Run plus 30 hint presses; Reset occurs after timing. This trace correlates actions but cannot identify the origin of historical unrequested inputs. No screenshots, native tests or builds overlapped these three timing captures. Physical and combined fixture motion is falling/settling away from the receiver, not a solved-level assertion. New fixture Reset is visually recorded; exact authoritative Reset/save evidence comes from the existing unchanged-source positive/control records above.

Motion assets are separate from timing: s001-hint-motion.webm (VP8, 800×500, 2.584 seconds) and s001-combined-motion.webm (VP8, 1440×900, 3.967 seconds). Their nominal 60 Hz encoding does not prove application frame rate. Both captures overlapped native tests and are excluded from performance evidence. Corresponding screenshots show hint visibility, falling ball and restored construction; screenshots alone do not prove exact authoritative restoration.

## Criterion-level disposition

| S001 criterion | Expected | Observed / verdict |
| --- | --- | --- |
| Untouched source identity | Exact HEAD, dirty diff, bundle and source hashes; no runtime edits | 916 entries, zero mismatches; production hashes match. Pass as identification; unpublished source reproducibility remains limited. |
| Exact current failures | Reproduce relevant retained failures without hiding or weakening them | 76/66/10 with full XML/messages/stacks; build passes. Pass for baseline preservation, runtime correctness fails. |
| Physical workload and control | Real UI physical motion plus a meaningful control | Existing unchanged-source 3,600-tick positive/depth-control integration and exact Reset/save retained; smallest fixture window below. Scope pass only. |
| Animation and combined workloads | Real hint actions with physics stopped and running; retain raw timing/actions and motion | Raw captures independently reproduce recorded percentiles; stopped-physics hint motion and combined fixture motion retained. Pass for scoped capture, not animation/worker/FPS qualification. |
| Applicable performance gates | Compare actual integrated evidence to declared units/targets | Existing physical production 150.1/159.0 ms and complete-tick 17.1/10.5 ms fail. No speedup or device qualification. |
| Current/stale distinction | Preserve failures and identify superseded/inapplicable observations | Interrupted control excluded, fresh control selected by run identity; motion timing contaminated; older checkpoint wording remains historical. Pass. |
| Typing/ownership scope | No new untyped domain contracts or ownership paths | No runtime/tooling edits. Existing repository-wide typing and worker-ownership audits remain open; this audit cannot close them. |
| Review/publication | Coordinator independently inspects artifacts, scopes publication | Coordinator accepted the scoped capture after independently recomputing raw timestamp distributions, verifying 76/66/10 XML results and all 916 source hashes, inspecting screenshots/motion, and confirming the unrequested inputs fall outside the timing windows. Publication pending. Documentation-only publication cannot publish or qualify the uncommitted runtime migration. |

Next numbered row after accepted S001: S002, fix the benchmark manifest and task-specific thresholds. Do not use this baseline to skip S002–S006 or claim PERF-39 complete. No S002 work was performed here.
