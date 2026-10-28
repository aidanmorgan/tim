# 9A.44B implementation notes

Scope: [frozen spec](spec-9a-44b-f32-game-values-and-integer-time.md), based on published `6125040efa3becf31c6a5957f35571735f04972c`. Implementation fixes are complete; independent review, the coordinator's final full local round, hooks and publication remain pending. Browser and detailed performance qualification retain their named later gates.

## Behavior and assertion mapping

| Contract | Current proof |
| --- | --- |
| f32 authored configuration, save/command fields and physical read validation | Existing F32LaneRoundTrip, F32SaveBoundary, ConstructionDecodeBoundary, electrical/contact, orientation and snapping tests use the float APIs. Field values, rejection, bounds and gameplay outcomes remain asserted. |
| Integral time conversion, normalization, local progression and overflow | PhysicalTimeTests checks rational/nanosecond conversion, checked refusal, one-hour and large-origin adjacent samples, real looping/follow/impulse/oscillation, independent presentation/world clocks and Reset ownership. Existing cadence/tick tests retain exact eligibility. |
| Motion/history/wire and declared cues | WorkshopRead, WorkshopWire, DeclaredPresentation, EventRingConsumer, ClockGuard, WorkshopAnimationTracks and WorkshopActivationAnimation tests use ordinal/phase timestamps. Physical and event identities remain integers. The browser harness checks the 364-byte diagnostic's u64 ordinal and f32 phase separately. |
| Derivation/ring/interop | WorkshopClockDerivationTests uses integer nanosecond fixtures, preserving startup/outlier, slew/rederive, epoch and lifecycle checks. New checks cover adjacent large stamps, envelope width/padding/outcome refusal and overflow without counter/pending-state changes. The shipped clock-ring harness checks signed corrections and atomic refusal. |
| Semantic architecture guard | GameFloatGuardTests checks actual Csc sources/references/options across engine, UI, element packages, production projects and BundleElements. Controls cover inferred double, implicit promotion, Math overloads, widen/calculate/narrow, valid float/integer code, neighboring adapter members and wider arithmetic inside adapter arguments. Half-precision and literal-ABI guards remain active. |
| Shipped glue and ordinary ownership | Four runtime sources and four harnesses moved to strict TypeScript, with explicit build/deploy outputs. NodeHarnessTests already discovers tools/runtime/*.test.ts automatically. Tests load emitted output and retain startup, late replies, disposal, receipts, event ownership and malformed-envelope controls. |

Old double absolute-time fixtures now construct PhysicalTime; there is no compatibility overload. The impossible double-MaxValue timestamp case now uses the real ulong maximum. Reversed-world/invalid-time controls still assert atomic refusal. Old event-seconds decimal equality is replaced by timestamp/cue identity and observable progression.

Motion interpolation, sine/phase-neighbor samples and the hidden bumper's cosmetic blend no longer pin the previous wider arithmetic's last bit. Their local f32 operation allowances preserve authored endpoints and independently expected progression. The oscillator's settled check uses a visually negligible 0.001 rad bound. No historical trajectory/tick golden, output hash, arbitrary scene precision or precision-period engine was added. External NaN/infinity rejection now lives at the native-clock conversion boundary rather than in an integral exchange constructor.

## Verification retained

- Incremental native and Animation worker Debug builds passed with zero warnings/errors; pinned strict tsc passed.
- Authoring/read selection: 521 passed, zero failed/skipped. Motion/history/wire follow-up: 119 passed, zero failed/skipped.
- Latest broad affected native run: 699 passed, zero failed/skipped, 44s.
- Five emitted-output clock/animation/client/observation harnesses: 77 passed, zero failed/skipped/cancelled/todo.
- Existing NodeHarnessTests runner: all 9 discovered harness registrations passed, zero failed/skipped.
- Final numeric guard, UI failure, half-precision and literal-ABI selection: 126 passed, zero failed/skipped, 22s; includes all 11 GameFloatGuardTests cases.

The matching five compiler logs are in `/private/tmp/tim-f32-gate.82ezne`: playtest, solution, web, check-staged-tests and verify-policy. Commands are the five compiler-proof commands in [local-test-authority](../../docs/delivery-workflow.md#local-test-authority), with that directory substituted. Current playtest/solution/web captures passed with zero warnings/errors in22.98s/23.12s/14.16s. File-tool captures passed. Environment: .NET10.0.403, wasm-tools10.0.112/10.0.100 (workload10.0.401), Node26.12.1.

Original failed attempts remain evidence, not claimed passes: earlier animation runs were412/33 then444/1; the timer's integer division and world/master fixture mismatch were corrected. A resumed source/animation check was360/53, exposing old exact scalar expectations, source paths and synthetic exports. The first migrated node run was73/2 (old error text and one number-initialized worker clock fixture); both were fixed. Intermediate guard/source run was413/2 (adapter signature classification and one source count), then51/0; subsequent controls passed52/0. The first repository semantic guard was10 passed/1 failed: residual wider literals/UI arithmetic and external conversion inventory gaps were corrected. One expected build-target pin mismatch was updated for the actual tsc target. One E2eRunner build exited with MSB4166 during restore; the sequential -m:1 retry passed. Original outputs are retained in the session transcript. Superseded captures remain in `/private/tmp/tim-f32-gate.JONoak` and `/private/tmp/tim-f32-gate.82ezne/before-boundary-audit`; unaffected file-tool captures were reused.

## Forward deletion and remaining gates

Deleted source implementations: engine/gpu/PhysicalPhase.cs (moved to Abstractions); Animation.Worker/wwwroot/worker.js; Simulation/wwwroot/clock-master.js; web/wwwroot/clock-ring.js and native-clock.js; tools/workshop-{clock-ring,animation-clock,animation-output,client-lifecycle}.test.mjs. Deployed JavaScript is generated by tsc, and existing .js URLs remain deployment names. Removed ambient module stand-ins, floating absolute game-time APIs and both f64 derivation ring lanes. Periodic evaluation is bounded integer/f32 and allocation-free, with no BigInteger machinery.

No full solution test round, independent review, commit, push, Release worker build, web publish, browser test or mutant was run by the implementer. The coordinator owns the full round, five Auto review layers, hooks and publication.

## Full round1 corrections and affected verification

The coordinator's five preserved `**/TestResults/f32-full-round1.trx` files report main4366 passed/12 failed/0 skipped (20m17s), tools491 passed/0 failed/0 skipped; total4857 passed/12 failed/0 skipped. All12 failures were investigated:
- BothGatePresentation(1) and PresentationBindingGolden(4): fixtures advanced master capture seconds against world-clock cues; now use world tick/physical time. Authored From/To declarations and actual native Reset values remain exact; sampled colour/scale/metre values use a visually negligible0.00001 bound around independently known easing amounts. PhysicalTime tests retain exact completed clip/impulse endpoints and settled follow endpoint.
- WorkshopHint maximum finite delta(1): FrameDelta now validates finite/nonnegative input and applies the existing0.1s camera clamp before f32 conversion. Invalid-input atomicity and worker-only hint samples remain covered.
- Detector off-grid(2) and Domino authored fixtures(1): assert independent authored metre positions within0.1mm instead of exact re-derived decimal remainders. Detector cell48 stays exact; the1cm inside/outside aperture controls remain unchanged.
- Handler boundary(1): balanced argument parsing accepts conversion inside the existing catch. Controls reject conversion outside it, unprotected awaits and narrow catches.
- MotionEndPhase finite control(1): end phase is structural; changing it alone must refuse, as for start/anchor. Old double absolute-time addition had absorbed the subnormal change. Nonfinite refusal and subsequent clean tick still execute.
- Bumper fixture(1): removed redundant re-derived cell/local comparisons; the existing AuthoredContentAssertions.Placement independently compares every source cell/local/rotation/lock exactly.

WorkshopWire rational-deadline theory now checks both0s and3600s directly at90/144Hz: Due immediately before and at the next deadline, plus Deadline of that ordinal. No sleeps or solver-hour stepping.

Incremental native Debug build passed0 warnings/errors(13.06s). Affected test run passed274/0/0 in12s, preserved in `CuriousContraptions.tests/TestResults/f32-round1-fixes.trx`. It selects BothGatePresentationTests, PresentationBindingGoldenTests, WorkshopHintTests, DeclaredIndicatorPassageTests, WorkshopHandlerBoundaryTests, DominoEffectTests, F32LaneRoundTripTests, WorkshopBumperTests, WorkshopWireTests, PhysicalTimeTests and WorkshopUiFailureTests. Numeric/handler/clock guards passed208/0/0 in24s, preserved in `f32-round1-guards.trx`; selection: GameFloatGuardTests, WorkshopHandlerBoundaryTests, WorkshopClockDerivationTests, ClockGuardTests, HalfPrecisionGuardTests, WorkshopAbiLiteralScanTests. Both commands use `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -noAutoResponse --no-build --no-restore -c Debug -f net10.0 -p:TreatNoTestsAsError=true --filter '<listed classes joined by |>' --logger 'console;verbosity=minimal'`, with their named TRX logger. The guard run sets `CSHARP_STATIC_GATE_BINLOGS=/private/tmp/tim-f32-round2.z0mV4s`.

Matching captures are now `/private/tmp/tim-f32-round2.z0mV4s`. Three affected compiler commands passed0 warnings/errors:
```sh
dotnet build CuriousContraptions.slnx -c Debug --nologo --no-restore --no-incremental -p:PlaytestDiagnostics=true -noAutoResponse -bl:/private/tmp/tim-f32-round2.z0mV4s/playtest.binlog -v:quiet
dotnet build CuriousContraptions.slnx -c Debug --nologo --no-restore --no-incremental -noAutoResponse -bl:/private/tmp/tim-f32-round2.z0mV4s/solution.binlog -v:quiet
dotnet build CuriousContraptions.web/CuriousContraptions.web.csproj -c Debug --nologo --no-restore --no-incremental -p:WasmBuildOnlyAfterPublish=true -p:TwoDogAutoImport=false -noAutoResponse -bl:/private/tmp/tim-f32-round2.z0mV4s/web.binlog -v:quiet
```
Durations22.09s/23.25s/14.85s. Unchanged check-staged-tests.binlog and verify-policy.binlog were copied from the previous capture directory; all original captures and round1 TRXs remain intact. `git diff --check` passed. No full solution test rerun, commit or publication was performed by the implementer; coordinator owns round2 and the remaining gates.

## Review fix: committed world-time bound

Service now calls the internal WorkshopDeclaredPresentation.Advance(schedule, master) seam. That single production path projects from the installed master/world anchor and bounds running evaluation by the evaluator's latest admitted committed tick. Paused/Completed/Building projections use their anchor; the existing backwards-time rejection remains active. The replaced private worker WorldTime calculation was deleted. Autonomous controls still use master time, and no public API or fallback was added.

DeclaredPresentationTests exercises that worker-used seam for all3 simulation ×4 animation cadences over real production simulation reads, nonzero/off-lattice master anchors and an off-lattice event. Assertions cover uncapped fractional progression, further progression at the committed bound, delayed-read freeze, missing-event control, foreign-schedule refusal, Pause, Step, Resume, Completed, Reset and autonomous UI progress during pause. PhysicalTimeTests now starts a real two-second AnimationBatch loop at zero and jumps the same running channel to one hour and independently known quarter phases, without long stepping.

The first incremental test build failed on a missing Simulation namespace import; corrected. The first affected run (DeclaredPresentation, PhysicalTime and the3 autonomous WorkshopAnimationTracks checks) was40 passed/12 failed/0 skipped: each new cadence case reached Pause, then my test incorrectly assumed the UI registry used slot0. The test now enumerates the actual TryRead slots and checks the single public sample. Final affected run:49 passed/0 failed/0 skipped,9s:
```sh
dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -noAutoResponse --no-build --no-restore -c Debug -f net10.0 -p:TreatNoTestsAsError=true --filter 'DeclaredPresentationTests|PhysicalTimeTests' --logger 'console;verbosity=minimal'
```
Matching incremental native test build and Animation.Worker Debug build passed0 warnings/errors(8.48s/5.98s), each with `dotnet build <project> -c Debug --no-restore --nologo -noAutoResponse -v:quiet`. `git diff --check` passed. No wider tests or compiler-capture refresh were run for this review fix. The previous playtest/solution/web captures require refresh for the changed worker/shared production source; file-tool captures remain unaffected. Coordinator owns independent re-review and final verification.
