# Adversarial Review Report: Story 9R.0c — Undelivered Parts Leave the Palette and Bench Placement Never Throws

**Reviewer:** Murdoch-4. I am independent of the implementer (Amelia), and I edited no deliverable file.
- Mutants and probes ran only in an isolated `git archive` extract of `5779fe2b`, at `S/murdoch4/mut`, with the worktree's `.godot` import cache copied in.
- The probe bundle was served from that extract on :8061.

**Source acceptance:** `_bmad-output/planning-artifacts/epics.md` §Story 9R.0c. Tiers come from `autonomous-development-guide.md#verification-tiers`.
**Candidate:**
- branch `worktree-agent-a67f1ccf03b1c80b5`, base `35eba218`;
- commits `d87164dc` (candidate 1) and `5779fe2b` (candidate 2, the round-1 lens fixes);
- the diff reviewed is `git diff 35eba218..5779fe2b`, saved as `S/murdoch4/full.diff`. The candidate-2 delta is `S/murdoch4/c2.diff`.

**Terminal verdict: PASS (scoped), on candidate 4, `8de7f259`.** See §R2.3. Sections 1–7 below record round 1, which failed candidate 2 (`5779fe2b`) on F1.
**Date:** 19 Oct 2026.
**Raw evidence:** `S` = `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/49d80295-44c4-4940-bc16-8455cdf5d09d/scratchpad/`. Mine is under `S/murdoch4/`; the implementer's is under `S/9r0c/`.

---

## 1. Verdict

**Fail.** One blocking Medium, F1, is proven in Chrome. Everything else I attacked holds: all ACs other than AC 2's "never throws past the UI", round-1 Mediums 1, 3 and 4, the deletions, Tier 1 and Tier 2.

This is a one-line forward fix, returned to Amelia: delete `throw failure;` and set a visible status. A fix-only re-review then needs:
- the native suite;
- a mutant or test that pins "no escape";
- palette-placement plus one smoke suite on the rebuilt bundle.

## 2. Findings (ranked)

### F1 (Medium, blocking, proven): the round-1 fix rethrows out of `async void Click`, and in the browser that terminates the .NET runtime

**The code.** At `ui/Workshop.cs:541-548`, the post-admission presentation `catch` does `GD.PushError(...)` and then `throw failure;` from `private async void Click`. AC 2: "placing a part on the bench never throws past the UI." Round-1 finding #2 asked only that the failure not be *swallowed*, and `PushError` with its context already meets that. The `throw` adds back the exact mechanism behind the owner's crash.

**Browser proof.** Probe file: `S/murdoch4/probe-f1.test.ts`.
- **Setup:** the bundle is `5779fe2b` plus one injected throw in the presentation block, for the Ramp only (`probe-F1.diff`). `CuriousContraptions.wasm` is `aaefe28f…`, and two independent publishes reproduced it.
- **Rethrow variant** (the candidate's code), logs `probe-f1.log` and `probe-f1-rethrow2.log`:
  - `Unhandled Exception: System.InvalidOperationException: Bench placement of 'ramp' as body 1 was admitted, but presenting it failed.`, raised via `Task.<>c.<ThrowAsync>` → `ThreadPool.BackgroundJobHandler`;
  - `[MONO] Process terminated.`;
  - `MONO_WASM: Assert failed: .NET runtime already exited with 1`;
  - `CCGPU_TRANSPORT_FAILURE 3`;
  - the canvas `computePosition` reads a null `width`.
  - The follow-on Wall placement at (450,485): "no Workshop command acknowledgement … the UI did not issue the command". **The game is dead until reload.**
- **Control:** the same injection with only `throw failure;` removed (`probe-F1-control.diff`, wasm `dd24e025…`), log `probe-f1-control2.log`.
  - The error is logged once.
  - There is no termination.
  - The follow-on Wall places, and Save holds `["ramp","wall"]`. **The game survives.**
- **Failed control attempt, retained:** `probe-f1-control.log`.
  - My first control run placed the follow-on Wall at (560,600) without pressing Escape, and it failed: "the UI did not issue the command". The runtime was *not* terminated.
  - The passing run changed two variables at once (Escape added, point moved to (450,485)), so the cause of the first failure is **not isolated**.
  - A likely contributor is that after the caught failure, `_tool = null`, `ClearPreview()`, `RefreshPalette()`, `RefreshLayers()` and `_dragging = false` never ran, which leaves the Workshop half-updated.
- **The native test doesn't distinguish the two.** Mutant M6 deletes only `throw failure;`, and the native `AnAdmittedPlacementWhosePresentationFailsConsumesItsIdentityAndFailsLoudly` still passes, so the rethrow is untested. Natively, Godot's synchronization context logs the escaped exception. In the browser, the continuation runs on the thread pool with no context, so it is fatal.

**Severity.** The branch is reached only by a defect after admission, and the base code was equally unguarded there. It is still a direct AC 2 violation, and it was introduced deliberately by this story's fix round, so it blocks.

**Remedy:**
- Keep the `PushError` with its context.
- Set `_status.Text` to a visible failure message, and return.
- After a caught presentation failure, the Workshop must be left consistent (tool cleared, preview cleared, palette and layers refreshed, not dragging), and it must accept the next placement **without the player pressing Escape**.
- Pin it: for example, assert that the click leaves the Workshop usable (a second placement succeeds) and that exactly one error is logged. Then re-run M6, which must now be killed by the "no escape" assertion, or equivalent.

### Low findings (recorded, non-blocking)

- **L1: `Propose` maps every exception to `UnsupportedPart` and logs nothing** (`ui/WorkshopBenchPlacement.cs:26`).
  - Mutant M3 narrows the catch to `ArgumentException`, and it **survives**. So the breadth is untested; every real throw today is an `ArgumentException`.
  - A capture or compose defect (for example an NRE) reaches only the status label. The e2e console watcher can't see it; only palette-placement's saved-construction assertion would.
  - Suggest `GD.PushWarning` with the context, or narrowing to the typed failures. Owner: coordinator, trivial.
- **L2: the bench path is not literally "the same typed rejection path `CommitSelected` uses".** `CommitSelected` (`ui/WorkshopGpu.cs:70-85`) is an untyped catch that shows `error.Message`. `Propose` is typed and shows `Placement rejected: …`, matching `SubmitConstruction`. This meets the AC's intent and is stricter, so accept.
- **L3: at the 40-entry undo cap**, push-then-pop on a refused Submit drops the oldest entry. This predates the story and is shared with `DeleteSelected` and `CommitSelected`.
- **L4: palette-placement hard-codes its 22-row list.** A row appended after the State latch would not be placed in Chrome, although the native test derives its list from `Free`. The suite proves "appears" through the saved construction, not through a rendered check. A failed presentation is still caught, because it logs a console error.
- **L5: dispose on every throw path (the implementer declined round-1 Low 4). I accept her reason.**
  - Any `Admit` throw aborts `Discover`, which is fatal to boot, so releasing the wrapper first buys nothing.
  - Godot keeps one managed wrapper per native object, so disposing a cached resource that another holder may share is a real hazard.
  - Leave it to the GC.

## 3. Acceptance-criterion matrix

| AC | Evidence | Status |
|---|---|---|
| AC1: the Free palette offers only constructible parts, without Tennis or Balloon | `FreeRows` filters `UndeliveredParts.Ids`, and the `Free` getter goes through the same filter before Discover. M10 (filter removed) is killed by the palette test and `WorkbenchCapacityTests` row order. M9 (`Admit` admits undelivered parts) is killed by 6 tests. In Chrome, the adjacency probes Springboard→Pipe and Pipe←Springboard pass. | Pass |
| AC1a: an explicit, typed, checked-in set, not a text match | `enum UndeliveredPart { Tennis, Balloon }` plus a frozen `Ids` set (`engine/gpu/UndeliveredParts.cs`). The text check `content.Contains("Parameters = {")` is deleted. | Pass |
| AC1b: a test fails if a palette part lacks capture, BallMaterial, footprint or save decode | `EveryFreePalettePartCapturesSizesAndDecodesFromTheSaveAndTheWorkerWire` asserts palette == registry, and also covers the worker wire. The implementer's mutants 6a–6e were killed; her logs are reviewed in `S/9r0c/mutation-6*`. | Pass |
| AC2: bench placement never throws past the UI | Capture and compose are inside `Propose`, but the post-admission path rethrows and kills the runtime in Chrome (F1). | **Fail (F1)** |
| AC2a: capture inside the typed rejection path | `WorkshopBenchPlacement.Propose`. M11 (the original defect back in `Click`) is killed by the Click negative control. | Pass (L2) |
| AC2b: `PushUndo` only after a successful capture | M1 (undo before capture) is killed by 5 tests. M2 (undo between capture and `WithInstance`, which is round-1 Medium #1) is killed by `AProposalWhoseLessonRefuses…`. | Pass |
| AC2c: one typed rejection, a visible message and no undo | `BenchPlacementRejection.UnsupportedPart`; `_status.Text`; the Click test asserts the exact message, an empty `_undo`, no command and an unchanged `_nextId`. M4 (no undo pop on a refused Submit) is killed. | Pass |
| AC2d: native negative control through `Click`'s capture path | `ClickingTheBenchWithAnUncapturablePartIs…` calls the private `Click` on the actual `workshop.tscn` with Balloon. | Pass |
| AC3: Chrome palette-placement, actual UI, every row, no errors, Tennis and Balloon absent | `tools/e2e/palette-placement.test.ts`: mine 25/25, and the implementer's 25/25 on `5779fe2b` | Pass (L4) |
| AC4: Tier 2 plus smoke pair; Tier 1 at 100% | §4 and §5 | Pass |
| Deletes: the text check and 3 test assertions | `PartRegistryTests` (24→22), `BasketballResourceTests:34`, `WorkbenchCapacityTests:168` | Pass |

### Round-1 Medium fixes

| # | Result |
|---|---|
| 1: undo before `WithInstance` | Fixed. Mutant M2 is killed. |
| 2: catch-all after Apply, and `_nextId` | `_nextId` is advanced before presentation (M5, which moves it after, is killed). The "not swallowed" requirement is over-corrected into a fatal rethrow, which is **F1**. |
| 3: no test for the Parameters fail-fast | Fixed. M8 (Parameters admitted) and M7 (undelivered check moved ahead of the duplicate check) are killed by the `Admit` test. |
| 4: two lists, and `Free` unfiltered before Discover | Fixed. M10 is killed. |

## 4. Identity and reuse

**Served bundle.** The :8060 preview is this worktree's `tools/Preview` (PID 77999). I hashed served files and compared them with `S/9r0c/c2-bundle-manifest.sha256` (manifest sha256 `c05c9aa1…`); all match:

| File | sha256 |
|---|---|
| `_framework/CuriousContraptions.wasm` | `ccb9eecc…` |
| `godot.pck` | `18745f0a…` |
| `index.html` | `a9ce2851…` |
| `simulation/…/CuriousContraptions.Simulation.wasm` | `af013c25…` |

- The publish (16:39) ran after the `5779fe2b` commit (16:38:43), and the worktree is clean.
- The served wasm hash was unchanged before and after my Chrome run.

**Implementer Tier 2, reused.** `S/9r0c/c2-tier2/summary.txt` records `candidate 5779fe2be628…`, with per-file TAP:

| Suite | Result |
|---|---|
| palette-placement | 25/25 |
| engine-core-2b1 | 1/1 |
| engine-core-2a1 | 5/5 |
| cat-062 | 7/7 |
| cat-015b | 4/4 |
| cat-030 | 7/7 |

All exited 0, with `finished 08:47:13Z`. The selection is the story suite, the smoke pair, and the suites that exercise the palette and placement (Springboard, the cat-015b lesson, and cat-030). That fits the AC's Tier 2 scope.

**Implementer Tier 1, not reused.** Her `c2-tier1-*` logs (16:37) predate the commit, so they can't be bound to it. My own Tier 1 on the `5779fe2b` extract supersedes them.

**The coordinator's claim that candidate 2 "only changes rejection and exception paths" is incorrect.** Candidate 2 changes these success paths:
- `PartRegistry.Discover`, which every boot runs, now goes through `Admit` and its `seen` set.
- The canonical order now includes Tennis and Balloon, filtered by `FreeRows`, and produces every palette row.
- Click's accepted branch now sits inside `try/finally`, and `_nextId` is computed from `placedId`.
- `Propose` now composes the construction before it records undo.
- `UndeliveredParts` moved into `engine/gpu`, so it and the changed `WorkshopInventory.cs` now compile into the Simulation and Animation worker assemblies (`Compile Include="../engine/gpu/*.cs"`). No worker source references either type, so the worker behaviour is unchanged and this is not a Tier 3 trigger.

Reuse is still sound, for a different reason: candidate 2's own Tier 2 re-proves those success paths on `5779fe2b`.
- palette-placement places all 22 rows through the UI.
- Every suite boots through `Discover`.
- `WorkbenchCapacityTests` pins the exact row order.

The candidate-1 full run (34 files, 165/165, bundle manifest `d58f718b…`) is supporting evidence only. None of the changed files is in a Tier 3 category, so no full run is required.

**Restored screenshot.** Main's working tree has no modified tracked evidence; only untracked `preview.log` files and the coordinator's three planning docs are modified. The worktree is clean. Her restore holds.

## 5. Independent execution (on `5779fe2b`)

| Check | Result | Log |
|---|---|---|
| `dotnet build CuriousContraptions.slnx` (extract) | 0 warnings, 0 errors | `S/murdoch4/t1-build.log` |
| `dotnet test CuriousContraptions.slnx` (extract) | 1536/1536; Coverage 78/78 | `S/murdoch4/t1-test.log` |
| Node harnesses `tools/*.test.mjs` (extract) | 76/76 | `S/murdoch4/t1-node.log` |
| `anvil check --since 35eba218` (.ts) and `--extensions .cs` (10 files) | no warnings | `S/murdoch4/anvil*.log` |
| `dotnet run -c Release --project tools/LifecycleContract`, `tools/WireContract`, and `tools/Ownership -- --oracles` (extract) | Lifecycle: Count 510, Failures 0. Wire: Count 109, Failures 0. Ownership: Positive, 22 negatives all rejected. All exit 0. | `S/murdoch4/t1-contracts.log` |
| Chrome palette-placement (:8060, HEAD `5779fe2b`, wasm `ccb9eecc…`) | 25/25, exit 0, 61 s | `S/murdoch4/chrome-palette-placement.tap` |
| Chrome engine-core-2a1 (smoke) | 5/5, exit 0, 19 s | `S/murdoch4/chrome-engine-core-2a1.tap` |
| F1 browser probe, rethrow vs control (:8061) | rethrow terminates the runtime; control survives | `S/murdoch4/probe-f1*.log` |

## 6. Mutants

These ran in `S/murdoch4/mut`, with the focused 62-test set (PalettePlacement, PartRegistry, WorkbenchCapacity, BasketballResource). The runner is `S/murdoch4/run-mutant.sh`. Each mutant has `mutant-<name>.diff` and `.log`, and the pristine source was restored after each.

| Mutant | Result | Killing test(s) |
|---|---|---|
| M0 control (unmutated) | 62/62 pass | — |
| M1: `pushUndo` before capture | killed (5) | 3× `BenchPlacementOfAnUncapturablePart…`, `AProposalWhoseLesson…`, `ClickingTheBenchWithAnUncapturablePart…` |
| M2: undo between capture and `WithInstance` | killed | `AProposalWhoseLessonRefuses…` |
| M3: catch narrowed to `ArgumentException` | **survives** | — (L1) |
| M4: no undo pop on a refused Submit | killed | `ARefusedPlacementLeavesNoUndoAndKeepsItsIdentity` |
| M5: `_nextId` advanced after presentation | killed | `AnAdmittedPlacementWhosePresentationFails…` |
| M6: rethrow deleted (log only) | **survives** | — (F1: the rethrow is untested natively, and fatal in the browser) |
| M7: undelivered check before the duplicate check | killed | `Admit_RefusesUntypedParameters…_AndChecksIdentityFirst` |
| M8: Parameters fail-fast removed | killed | `Admit_…` |
| M9: `Admit` registers undelivered parts | killed (6) | `Discover_…`, `UndeliveredPartsShip…`, `EveryFreePalettePart…`, `Admit_…`, `BasketballResource…`, `WorkshopInventoryPolicy_Free_…` |
| M10: `FreeRows` ignores the undelivered set | killed (2) | `ThePaletteIsTheCanonicalOrder…`, `FreeInventoryListsEveryPlayableKind…` |
| M11: the original defect in `Click` (undo, then an unguarded capture) | killed | `ClickingTheBenchWithAnUncapturablePart…` |

## 7. Final verdict

**Fail**, on F1 (Medium, proven in Chrome). Return it to Amelia.

Required fix:
- remove `throw failure;`;
- keep the `PushError`;
- show a visible status;
- leave the Workshop consistent, so the next placement works without Escape;
- add a test or assertion that kills M6.

Re-review scope:
- native tests;
- the M6 re-kill;
- the F1 browser probe, re-run against the fixed bundle, with the Escape removed and at a known-good bench point;
- palette-placement plus engine-core-2a1 on the new bundle identity.

The other evidence on this candidate stays reusable for the parts the fix does not touch: Tier 2, mutants M1–M5 and M7–M11, and the identity checks.

Lows L1–L4 are recorded. L1 is trivial and should be fixed in the same round. L5 is accepted as declined.

---

# Round 2: candidate 3, `4b1a70c3` ("no exception escapes an async void UI handler")

**Diff reviewed:** `git diff 5779fe2b..4b1a70c3`, saved as `S/murdoch4/c3.diff`. It touches 10 files (+234/−22), and the worktree is clean at `4b1a70c3`.
**Status:** static review complete. Runtime checks (Tier 1, mutants, palette-placement, identity of her full run) are pending the coordinator's "CHROME CLEAR".

## R2.1 Static results

**F1 fix (presentation failure after admission), `ui/Workshop.cs:551-558`: correct.**
- `throw failure;` is gone, and `PushError` keeps the full context.
- The Workshop leaves placement mode: `_tool = null` and `_dragging = false`, with `ClearPreview`, `RefreshPalette` and `RefreshLayers` in their own logged try.
- A visible status is set. `_nextId` was already advanced, and the undo entry is kept.
- This answers the "next placement without Escape" requirement from my failed control attempt.

**L1 fix: correct.** `Propose` now `PushWarning`s the full exception with the part and body (`ui/WorkshopBenchPlacement.cs:30`). The native tests `Expect` the warning text.

**The handler audit.** All 19 `async void` methods under `ui/` became exact boundaries (`rg` finds 21 `async void` lines; the other 2 are comments), `try { await XAsync(..); } catch (Exception error) { HandlerFailed(nameof(X), error); }`:

| File | Handlers |
|---|---|
| `Workshop.cs` | `_Ready`, `Click`, `DeleteSelected`, `Undo`, `ToggleRun`, `ResetRun`, `Save`, `LoadSave` |
| `WorkshopBattery.cs` | 2 |
| `WorkshopConfiguration.cs` | 4 |
| `WorkshopConnections.cs` | 1 |
| `WorkshopGpu.cs` | `Playback`, `CommitSelected` |
| `WorkshopPuzzle.cs` | 2 |

- `HandlerFailed` (`ui/WorkshopGpu.cs:491-497`) logs with the handler name and the full exception, then sets a visible status unless the UI was removed. A failure while setting the status is itself logged.
- No rethrow and no silent swallow.

**Coverage of the defect class is complete.**
- `rg` finds no other `async void` in the game sources (`ui/`, `engine/`, the web, Simulation and Animation hosts), apart from `MachineWorld._ExitTree => await DisposeWorkshop()`. `DisposeWorkshop` catches its client-disposal failure (`engine/MachineWorld.Gpu.cs:387-388`), so I accept it.
- The only async lambda in game code is the `Func<Task>` at `engine/MachineWorld.Gpu.cs:58`, which is not void.

**No change on success paths.**
- Each wrapper runs its `XAsync` synchronously up to the first incomplete await, exactly as the old `async void` body did, and captures the same synchronization context.
- The handler bodies are unchanged apart from the F1 catch. The only other change is one parameter-spacing fix in the `ApplyDelayDuration` signature.

**The M6-killing test is sound in design.** `AnAdmittedPlacementWhosePresentationFailsIsLoggedOnceNeverEscapesAndLeavesTheWorkshopUsable`:
- runs `Click` under an `EscapeRecorder` synchronization context, which records anything an `async void` posts;
- asserts nothing escapes, exactly one error is logged, the status is visible, `_tool` is null and `_dragging` is false;
- re-chooses the tool and places again: no new error, both identities are in the construction, and `_nextId` is 3.

Reintroducing `throw failure;` would now be caught by the boundary and produce two errors, so `Single` fails. Removing the boundary as well would let the exception escape into the recorder. To be confirmed by mutant.

**The guard test.** `WorkshopHandlerBoundaryTests`:
- requires the exact boundary shape for every `async void` under `ui/`, with a floor of at least 19;
- includes two negative controls;
- pins `engine/` to the single `_ExitTree`.

Its limits:
- it is text and regex based, so an unusual layout such as `async void` with generic parameters or a line break before the name is missed;
- it does not see `async` lambdas assigned to `Action`. There are none today.

I record that as a Low.

## R2.2 New findings

### R2-M1 (Medium, provisional): `HandlerFailed`'s behaviour is untested; only its name is pinned

- No behaviour test reaches `HandlerFailed`. `rg` finds it only in the guard regex.
- The F1 catch inside `ClickAsync` handles the presentation failure locally, so the M6 test never calls the boundary either.
- So a mutant that makes `HandlerFailed` rethrow (recreating the browser crash for all 19 handlers), or log nothing (a silent swallow), would very likely survive.
- That is new decision logic on the story's central invariant, so mutation proof is required.
- **Remedy:** one native test that drives a real handler failure through its boundary. For example, run `Click` under `EscapeRecorder` with a fault that escapes `ClickAsync`, then assert:
  - nothing escaped;
  - exactly one error was logged, naming the handler;
  - the status shows "That action failed and was logged".
- This becomes a confirmed Medium if the mutant survives at runtime; otherwise it is withdrawn.

### Lows

- **R2-L1: a boundary can't restore handler-local state.**
  - Several bodies set `_gpuPending = true` and call `RefreshPalette()` *before* their own `try`: `ToggleRunAsync` (`Workshop.cs:669`), `ResetRunAsync` (707), `SaveAsync` (838) and `LoadSaveAsync` (870).
  - `DeleteSelectedAsync` pushes undo before composing.
  - A throw in those few synchronous statements now leaves the Workshop logged, visible but non-editable (`CanEdit` false), instead of a terminated runtime.
  - That is strictly better, and the structure predates the story. Record it against the owner of the handlers' refactor (Epic 9A).
- **R2-L2:** the guard is a text and regex scan, with the limits listed above.

## R2.3 Candidate 4, `8de7f259`: runtime results and terminal verdict

**Candidate 4** is `4b1a70c3` plus one native test, `AFailingHandlerIsCaughtAtItsBoundaryLoggedOnceByNameAndShown`, which is the R2-M1 remedy (`git diff 4b1a70c3..8de7f259` touches `PalettePlacementTests.cs` only). The test drives a real failure inside `UndoAsync` through `Undo`'s boundary, then asserts:
- nothing escaped (`EscapeRecorder`);
- exactly one error was logged, naming `Undo`;
- the status reads "That action failed and was logged: …";
- no command was sent.

**Tier 1, on a clean `git archive` of `8de7f259` (`S/murdoch4/mut4`):**

| Check | Result |
|---|---|
| `dotnet build CuriousContraptions.slnx` | 0 warnings, 0 errors |
| `dotnet test CuriousContraptions.slnx` | **1539/1539**; Coverage 78/78 |
| Node harnesses | 76/76 |
| LifecycleContract | 510/0 |
| WireContract | 109/0 |
| Ownership | Positive, 22 negatives rejected |
| `anvil check --since 5779fe2b --extensions .cs` | 9 files, no warnings |

Logs: `S/murdoch4/c4-t1-*.log` and `c4-anvil.log`.

**Mutants.** The focused set is PalettePlacementTests plus WorkshopHandlerBoundaryTests: control 15/15. The runner is `S/murdoch4/run-mutant4.sh`, with each mutant's diff and log in `S/murdoch4/r2-mutant-*`. The pristine sources were restored and verified with `cmp`.

| Mutant | Result | Killing test(s) |
|---|---|---|
| M6a: `throw;` put back in the presentation catch | killed | `AnAdmittedPlacementWhosePresentationFails…` (two errors logged) |
| M6b: `Click` boundary removed plus a rethrow | killed (2) | the same test (the `EscapeRecorder` caught the escaped exception) and the guard test |
| H1: `HandlerFailed` rethrows | killed | `AFailingHandlerIsCaughtAtItsBoundary…` |
| H2: `HandlerFailed` logs nothing | killed | the same test |
| H3: `HandlerFailed` shows no status | killed | the same test |
| P1: `Propose` warning removed | killed (5) | the uncapturable-part and lesson-refusal tests, via `Errors.Expect` |
| B01–B19: each of the 19 boundary catches narrowed to `InvalidOperationException`, one at a time (each diff is exactly one line) | **19/19 killed** | `EveryAsyncVoidUiHandlerIsACatchingBoundaryOverItsTaskMethod`. B04 (`Undo`) is also killed by the behaviour test. |

**R2-M1 is resolved.** `HandlerFailed`'s rethrow, no-log and no-status variants are all killed.

**Her candidate-3 full run, identity-verified and reused:**
- `S/9r0c/c3-full-run/summary.txt` records `candidate 4b1a70c36f59…`, started 09:12:05Z and finished 09:39:36Z.
- It covers 34 files, which equals `tools/e2e/*.test.ts` (34). That is 165/165, every file exits 0, and every TAP shows `# fail 0`.
- The publish (17:11:54 local) ran after the `4b1a70c3` commit (17:10:31) and before the run.
- The clock-master screenshot was redirected to the scratch folder, and main's tracked evidence is unmodified.
- The served bundle on :8060 matches manifest `c3-bundle-manifest.sha256` (`63da5de0…`):

| File | sha256 |
|---|---|
| `_framework/CuriousContraptions.wasm` | `657b35ff…` |
| `godot.pck` | `6d20d473…` |
| `index.html` | `a9ce2851…` |
| `simulation/…/CuriousContraptions.Simulation.wasm` | `a63ac802…` |

- Candidate 4 changes only a test file, so that bundle is candidate 4's bundle.

**My Chrome run.** palette-placement on :8060 (HEAD `8de7f259`, served wasm `657b35ff…` both before and after): **25/25, exit 0** (`S/murdoch4/c4-chrome-palette-placement.tap`).

**Open Lows (non-blocking, recorded):**
- **L2:** the bench rejection path differs from `CommitSelected`'s; stricter, so accepted.
- **L3:** at the 40-entry undo cap, a refused Submit drops the oldest entry (predates the story).
- **L4:** palette-placement hard-codes its 22-row list.
- **L5:** dispose on every throw path; declined, accepted.
- **R2-L1:** `_gpuPending` is set before the handler's own `try` in Run, Reset, Save and Load; a throw there leaves the Workshop stuck non-editable, not dead. Owner: Epic 9A.
- **R2-L2:** the guard test is a regex scan.
- L1 was fixed in candidate 3.

**Terminal verdict for Story 9R.0c, candidate 4 `8de7f259`: Pass (scoped).**
- Every AC is verified.
- F1 and R2-M1 are resolved and mutation-proven.
- Tier 1 passes 100% on a clean copy.
- Tier 2 and a Tier 3 full run (165/165) are identity-verified on the identical bundle.
- I independently re-ran palette-placement in Chrome.
- There are zero unresolved High or Medium findings.

This is a scoped Pass, not global or runtime qualification.

## R2.4 Runtime plan, as planned before CHROME CLEAR (now executed in R2.3)

- Tier 1 on a clean `git archive` of `4b1a70c3`: build, `dotnet test`, the Node harnesses, the three contracts, and anvil `--since 5779fe2b`.
- Mutants:
  - **M6′:** put `throw failure;` back;
  - **M6″:** remove `Click`'s boundary and rethrow;
  - **H1:** `HandlerFailed` rethrows;
  - **H2:** `HandlerFailed` logs nothing;
  - **B-x:** one per new boundary catch (catch removed or narrowed). Expected to be killed by the guard test;
  - **P1:** `PushWarning` removed from `Propose`.
- palette-placement once on the `4b1a70c3` bundle.
- Identity-check her full run: per-file TAP, the `4b1a70c3` HEAD, and the served-bundle hash.
