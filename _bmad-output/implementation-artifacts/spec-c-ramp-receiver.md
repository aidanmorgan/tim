---
title: "C-Ramp+Receiver — generic authoring and declared capture"
status: review
baseline: e036b7f9eb55afe55d818c18c10578f1b3b5a610
---

# Outcome

CAT-054 Ramp and CAT-004 Receiver now use generic typed f32 parameters. Package-owned capture/guide declarations replace receiver records, compiler branches and scalar wire fields. Roadmap position24 precedes NAND. Dependencies9A27/9A26/9.6 are verified. Exact acceptance: epics C-Ramp+Receiver,9A28/29,9R12/13 and CAT004/CAT054 current rows. Production C# UNIT tests PRIMARY; integration supplemental; browser qualification deferred until catalogue completion.

# Design and cleanup

Ramp uses declared collider bindings and generic X/Z resize (.125–4m); gestures clamp while active, Cancel restores and completed resize creates one Undo. Direct malformed vectors reject. Receiver declares fixed capture/guide regions and six typed parameters; shared compilation creates one sensor/guide per admitted ball with stable ordering and IDs. Capacity admission reads the capability, not a Receiver identity. The redundant warmup Ramp was removed: five warmup ticks never reached it and other fixtures already exercise static boxes; Initialize/Construct/Run5/Reset and borrowed ownership remain.

Generic defaults/save retain exact f32, including non-Half-representable inputs. Existing common collider/sensor/guide admission and authored AssistanceKnot values still use Half and remain9A44 work. The retained upper capture bound has its existing shared rounding point. No old authoring model, alias, compatibility path or extra editor fields were retained.

Immutable36lesson and9A16geometry fixtures are unchanged. Expected historical JSON is normalized into the current generic comparison shape; actual current parameters are not rounded to fit history. Actual compiled capture/guide bounds, speed/dwell/participation/support are separately compared with historical observations. New f32 default and exact-save assertions establish current authoring precision. All50 Planned content rows remain unchanged.

# Tasks

- [x] Capture existing behavior/compiled baseline before conversion:85/85 native2s,6fb3a2; immutable fixtures and independently verified prior CI2123/2123.
- [x] Shared typed capability/bundle admission, generic Ramp/Receiver parameters, authored content, current wire/save, UI and caller conversion.
- [x] Delete WorkshopRamp/RampDimensions/WorkshopInput.Ramp, WorkshopReceiver/ReceiverCaptureSettings/ReceiverForceRegion/WorkshopInput.Receiver, CaptureReceiver, Receiver accessor, CaptureInstance/compiler/whitelist/scalar paths and obsolete fixtures. Known ledger shrinks56 rows, adds none.
- [x] Align latest design: package identity/geometry/parameters, typed shared engines, exact f32 authoring, explicitly bounded remaining9A44 fields.
- [x] Primary real C# simulation proof: ramp length/runout vs side/depth misses; capture speed/fast-pass, participation/dwell/reentry, actual shifted receiver miss, rotated descending guide vs ascending/outside, bounded acceleration/fixed walls; named goal controls, authored solutions/assistance, save/Reset and capture presentation.
- [x] Native generic resize min/max/overshoot/Cancel/oneUndo, exact non-Half save vectors, parameter rejection and authored forgery controls.
- [x] Matching Debug/worker builds, bundle admission, schema/strictTS and isolation checks; no rebuild between unchanged filters.
- [x] Independent exact SnapshotApproval, normal protected publication, exact remote/CI/artifact/deployment scoped Pass: source8665e68b, main9b324bbd, CI39697329074 native2138/2138, artifact12392320102, deployment7240389509 ([review](review-c-ramp-receiver.md#terminal-scoped-pass--22-october-2026)).
- [ ] [Corrective forward fix](#corrective-forward-fix) (uncommitted): independent review, then normal publication.

# Evidence and applicability

Original tool receipts, not reconstructed logs:

| Scope | Result / original receipt |
|---|---|
| Actual engine, JoinedPipe, Springboard, compiled geometry |84/84,4s,690677|
| Save/wire/parameter parity and receiver lessons |290/291,6s,01f886; sole failure historical JSON representation|
| Corrected historical36 observations, exact non-Half save and resize |5/5,954ms,ea23ac|
| Bundle invalid capture/guide/null/missing/unknown parameter controls |35/35,68ms,fdf67e; buildfb298a|
| Real ramp roll/runout/misses |1/1,146ms,7ab5c9|
| Real receiver guide/rotation/eligibility/bounds/dwell/participation |7/7,111ms,4f10d1|
| Exact isolation shrink, capacity and capture animation/Reset |89/89,13s,585436; matching build93322e clean6.31s|
| Speed/fast-pass and partial residence leave/reentry |4/4,71ms,a63e8d; matching test-only builda0a917 clean4.19s|
| Current wire schema snapshot |24/24,280ms,5c2301|
| Strict TypeScript6.0.3 |pass fa8027|
| Release worker |clean4.34s,193245|

Native filters use `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj --no-build --no-restore --filter <named affected classes/tests> -v:minimal`. Builds use `dotnet build <affected csproj> --no-restore` in Debug or worker Release. Schema command: installed tsx `--test tools/e2e/unit/run-reset-snapshot.test.ts`; strictTS: installed TypeScript6.0.3 `tsc -p tools/e2e/tsconfig.json --typeRoots <installed @types>`. Source commands and original outputs remain in the tool receipts.

Prior failures remain explicit: CA1861 allocation38a62a and unused overloadf31ad4 corrected; test caller compile errors10326d/d6a5a8/549a44 corrected to current fields/types; historical representation failure01f886 corrected on expected side only. Isolation6fa5c4 found stale deleted debt and redundant warmup identity, now89/89. TS1cf462 found five retired-field accesses, now current generic vector access. Initial worker no-restore002059 lacked assets; restored then clean193245. Tool invocation failures (tsc PATH/typeRoots/Node enum loader) were corrected, not product failures.

Native goal proof `NamedGoalUsesCommittedBodySensorAndEpochWithoutAnimationFeedback` explicitly rejects wrong body, foreign sensor, empty capture and wrong epoch. Ordinary assistance/lesson tests retain existing Half inputs where they test the still-current AssistanceKnot boundary. Production capacity change only replaces equivalent declaration recognition; reviewer establishes unchanged prior proof applicability. No Chrome qualification claimed. Moving-frame/nudging and broad numeric migration remain named later scope.

# Corrective forward fix

Owner transport_implementer; retained reviewer transport_reviewer. Coordinator decision (22 Oct): this slice also repairs, forward, the pre-existing `dotnet test CuriousContraptions.slnx` regressions from closed slices (9A.26 2f5650b6, 9.6 4c5516ea); slnx 100% is a standing requirement. Uncommitted. No engine/worker source changed; the two element declarations only moved to named metadata arguments, and the generated `elements/elements.bundle.json` is byte-identical to a HEAD build (sha256 65964d95…2785 both).

- [x] Proof strengthening: longer Ramp gives longer runout (long > short + .1m, not abs); `capture_margin` widens the mouth while a beside decoy stays Outside; non-Half Ramp+Receiver run/capture, exact Reset and save/load replay; Ramp editor exact 1.234567 length, atomic 0.1/4.5/NaN and direct out-of-range refusal (construction, commands, undo, dimensions unchanged). Reviewer finding 2: the direct refusal now asserts the typed OutOfRange status "Invalid declared parameter value."; mutation with the editor's `parameters.Validate` removed fails (status became "Unexpected fault (ArgumentException)…"), then reverted (no ui diff) and rebuilt.
- [x] Generated TS region equals flatc output (misplaced eslint block and 4 blank lines removed).
- [x] Content readers: new shared `tools/E2eRunner/LessonFixtures.cs` (linked into Coverage) reads each lesson by typed availability: Playable `placements[].slot/instance.part/locked`, Planned `parts[].id/kind/locked` (the engine indexes Planned rows unconstructed, so this is the current schema, not a fallback). Unknown availability, mixed/missing shape, non-array, missing slot/part/locked reject. Coverage `ValidateFixtures` and `RequirementDiscovery.Fixtures` and E2eRunner `ReadLevelParts` use it; the stale `parts`-only readers are deleted.
- [x] Docs current-consumers fixture identities for the 12 Playable lessons now use content slots (`level/fixture_N`). Immutable `lessons-484ec38` shows domino_effect builds Basketball, end Domino and Signal lamp: FIX-014-002 (bowling) retired, FIX-001-064 ball and FIX-035-028 lamp added; CAT-001/014/035 level lists and CAT-014 doc corrected.
- [x] E2eRunner `ElementPackages` gains the BothGate and HoldTimer packages; their declarations use the named `Id:`/`ScenePath:` metadata convention every other package uses and the coverage tool reads (values unchanged).
- [x] Stale expectations, each justified by current content: fixtures 302→303 and sources 1471→1472 (one added lamp fixture); admitted packages 22→24; synthetic content gains `availability` plus Playable-shape positive/negative cases; D6 selection now uses the current domino_effect lamp, with a control that a Bowling change reaches no suite through domino_effect. BundleElements `(PortSocket)5` is FirstIn since 9.6; fixture uses undefined 99.
- [x] CI gap closed (coordinator): `.github/workflows/pages.yml` adds a "Test C# tools" step after the unchanged native step, running Coverage.Tests, BundleElements.Tests, WorkerAotGuard.Tests and E2eRunner.Tests with `--no-build --no-restore -c Debug -f net10.0 -noAutoResponse` against CI's existing Debug slnx build (test outputs verified at `bin/Debug/net10.0`). Reviewer finding 1 corrected: only the 15 E2eRunner tests named in the step's `toolchain` list (TypeCheckTests 2, SuiteProcessTests 8, IdentityTests 5; exact `FullyQualifiedName!=` filter) are excluded on the runner, each confirmed to fail with `HOME=$(mktemp -d)` (15/15 failed) and pass with the workstation toolchain (15/15); the other 9 tests of those classes run in CI. The excluded tests drive the workstation browser toolchain (npx-pinned tsx 4.23.15/TypeScript 6.0.3/@types/node, tsconfig's Homebrew Playwright path and the untracked checkout-parent `node_modules/flatbuffers`) and remain in local slnx runs and the post-catalogue browser tier.
- [x] Known consequence (coordinator accepted): `E2eRunner select` now refuses both_gate/hold_timer changes with `NoPrimarySuite` until their post-catalogue suites exist, instead of silently selecting Full.
- [x] Sprint status: 9a-c-ramp-receiver-conversion/9a-28/9a-29 done (0 Known rows owned by Ramp/Receiver). 9r-12/9r-13 stay review: the addendum leaves their Chrome suites queued.

| Scope | Result / command |
|---|---|
| Matching build after final edit | `dotnet build CuriousContraptions.slnx -c Debug` 0 warnings/0 errors |
| Full solution tests | `dotnet test CuriousContraptions.slnx --no-build --no-restore`: CuriousContraptions.tests 2142/2142, E2eRunner.Tests 189/189, Coverage.Tests 77/77, BundleElements.Tests 141/141, WorkerAotGuard.Tests 17/17 (2566/2566) |
| Release worker after element-declaration change | `dotnet build CuriousContraptions.Simulation -c Release --no-restore` 0/0 |
| Strict TS / schema (TS inputs unchanged since) | TypeScript6.0.3 `tsc --noEmit -p tools/e2e/tsconfig.json --typeRoots <@types 26.0.1>` exit0; tsx `--test tools/e2e/unit/run-reset-snapshot.test.ts` 24/24; E2eRunner TypeCheckTests pass |
| Generated wire parity | Pinned flatc25.2.10 run was permission-denied; Homebrew 25.12.19 reproduces e036b7f9's committed 2500-line TS region byte for byte, so TS output is version-invariant here; current region now equals it. C# differs only in the retained 25_2_10 ValidateVersion stamp and one header blank line also absent at e036b7f9. |
| Non-unit gates | Pre-commit hook and CI build the slnx; neither invokes Coverage or E2eRunner executables or an inventory audit. Historical `docs/coverage/engine/*.json` snapshots untouched. |
| CI step, simulated on Linux | Scratch git copy (two commits), Docker `mcr.microsoft.com/dotnet/sdk:10.0.401` + `dotnet workload install wasm-tools` + Node 20 (no npx cache, like the runner). CI restore + `dotnet build CuriousContraptions.slnx --no-restore --no-incremental -noAutoResponse`: all tool/test outputs built; only the Godot native import failed (linux-x64 libgodot on an arm64 host, CI is x64). Exact step commands under `bash -eo pipefail`: Coverage 77/77, BundleElements 141/141, WorkerAotGuard 17/17, E2eRunner filtered 174/174, exit 0 (step script extracted verbatim from the YAML). Unfiltered E2eRunner there: 15 failures without the npx cache, 6 with it (TS2307 playwright/flatbuffers, suite-process runs). Same extracted step on macOS 77/141/17/174, exit 0; `CiKeepsTheSameRestoreAndBuilds`/`OnlyMainDeploys` 12/12; YAML parses. |
| E2e selector impact | `E2eRunner select --changed` on either declaration (and `--diff HEAD`) now reports `NoPrimarySuite` for both_gate/hold_timer: registered packages without a declared e2e suite. Previously their paths were Unknown and silently selected Full. Explicit missing post-catalogue suite coverage (owners 9.6 Both Gate and Hold Timer browser suites); no current gate runs the selector, as Tier 2 browser runs are deferred. |
| Baseline of the repaired failures | At an e036b7f9 checkout: Coverage.Tests 4 KeyNotFound, E2eRunner `ElementPackagesMatchDirectoriesAndBundle` and `PartPreplacedByADeclaredLevelSelectsThatLevelsSuites` failed identically. Intermediate runs: Coverage 5 then 3 then 1 (BothGate/HoldTimer positional metadata, synthetic content shape, remaining stale totals), E2eRunner 1 (reason line includes connected reasons; assertion now checks the clock-master reason set). |

# Current state

Conversion published with terminal scoped Pass (main9b324bbd). The 9.6 terminal Pass is recorded in [review-9-6 Terminal publication verification](review-9-6-both-gate.md#terminal-publication-verification--22-october-2026) (main e036b7f9, CI39690098234 2123/2123, artifact12388827243, deployment7239438508) and ships with the next publication. Uncommitted corrective forward fix reaches slnx 2566/2566; no product defect found. Next: independent review, its normal publication, then NAND.
