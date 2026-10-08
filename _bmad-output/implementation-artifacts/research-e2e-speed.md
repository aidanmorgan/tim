# Research: making the Playwright/Chrome e2e suites faster without weakening the proof

Date: 2026-10-09. Read-only investigation; no repo file changed. Sources: `tools/e2e/*.test.ts`, `tools/e2e/workshop-driver.ts`, `tools/Preview/Program.cs`, `CuriousContraptions.web/wwwroot/workshop-client.js`, `CuriousContraptions.Simulation/wwwroot/worker.js`, the reviewer's logs `scratchpad/murdoch/e2e-all.log`, `murdoch2/e2e-all.log`, `murdoch/e2e-2c-rerun.log`, `murdoch2/e2e-anim-1b.log`, and `research-wasm-load-time.md`. Machine: Apple M4 Max, 16 cores, 64 GB. No suite was run for this report.

## 1. Where the ~2000 s goes

Two full runs: 2020 s (8 Oct, 2c failed "browser closed" under contention) and 2024 s (murdoch2). 11 suites, 38 tests. Code inventory: 47 `driver.reload()` + 11 launches = **58 cold page loads**; 29 `selectLevel()` (each disposes and recreates both workers); 11 save/load tests with two reloads each.

Per-test durations separate cleanly by reload count: tests without a reload take 3 ms–1.7 s (2a1 #1/#2); one reload ≈ 35–43 s; two reloads ≈ 67–80 s. Suite totals exceed test sums by ≈ 325 s, i.e. ≈ 30 s per `before()` launch+load. So **one cold load costs ≈ 30 s in these runs**, yet the same operation sometimes completes in ≈ 3 s: 2b3 #2 (reload + `selectLevel`) took 9.4 s and 11.1 s inside the same full runs, and anim-1b #1 took 4.7 s standalone versus 36–40 s cumulatively. The 30 s is a stall mode, not intrinsic cost. `research-wasm-load-time.md` §4 attributes it to the 368 per-assembly worker requests (175 assemblies per untrimmed worker) queuing on six HTTP/1.1 connections to Kestrel, with 3.5 s TTFB on 5 KB files; clock qualification itself is bounded to 5 s and measured at ≈ 0.3 s. The bundle served on :8060 right now is still untrimmed (`AppBundle/simulation/_framework/dotnet.boot.js`, 8 Oct 23:56, lists 175 assemblies); the trimmed worker csproj settings are in the working tree only.

| Bucket | Estimate | Share | Nature |
|---|---|---|---|
| 58 cold page loads at ≈ 28–30 s (Godot 47 MB wasm + main `_framework` + 2 × 26.5 MB worker runtimes, 416 requests, ≈ 128 MB, no caching) | ≈ 1650 s | 82% | Stall mode; best case 3.5 s untrimmed, ≈ 1.4 s trimmed |
| Real-time physics Runs (ball drops 1–4.2 s, 2 × 20 CCD iterations × 1.4 s, solve loops ≤ 8 s, settle 4.2 s × 3, negative controls 0.6–3 s) | ≈ 200 s | 10% | Irreducible while physics is wall-clock paced |
| Fixed UI settles (`clickAt` 60 ms + 200–800 ms × ≈ 570 clicks, `waitForReady` +400 ms × 87, Escape +200/300 ms, 29 `selectLevel` worker recreations) | ≈ 130 s | 6% | Replaceable by condition waits |
| Node/Playwright overhead, browser launch/close × 11 | ≈ 40 s | 2% | Negligible |

Per full run the Preview server ships ≈ 7.4 GB over loopback.

## 2. Ranked changes

| # | Change | Est. saving (serial) | Proof-validity risk | Where |
|---|---|---|---|---|
| 1 | **Remove the load stall.** (a) Publish the trimmed workers already configured in the working tree (`PublishTrimmed`/`TrimMode=full`, `InvariantGlobalization`, no symbols, `WasmDebugLevel 0`): ≈ 25 requests per worker instead of 184. (b) Fix `tools/Preview/Program.cs`: stop stripping `If-None-Match`/`If-Modified-Since` so reloads get 304s, add `Cache-Control: no-cache` (revalidate, never stale), serve the existing `.br` siblings with `Content-Encoding`. | 58 × (30 → ≈ 2–3 s) ≈ **1550 s** | None: production build unchanged; Preview headers are dev-loop only. Publication proof still runs against the deployed origin. Caveat: the stall cause was measured under contention; confirm with one uncontended timing after (a). | app-side build config (already in tree) + tools/Preview |
| 2 | **Parallel suites.** Run `node --test --test-concurrency=3 tools/e2e/*.test.ts`. Each suite already owns its own Chrome instance (own user-data-dir, so IndexedDB `constructionDatabase` and the isolation service worker are per-browser); one preview server serves all. Headroom: a suite is ≈ 3 busy threads (Godot main, physics worker, animation worker) and ≈ 1.5 GB; three suites fit the 12 performance cores with margin. Observed load average was 5.8 with one suite plus tooling; the earlier "browser closed" 2c failure coincided with the research measurement running a second headless Chrome, so do not exceed 3–4. Docs (`architecture.md`, `autonomous-development-guide.md`, `prd.md`) say "serially" and need the same edit. | Wall time ÷ ≈ 2.5 on the non-load part | Medium: physics is wall-clock paced (REQ-03 ratio 0.99–1.01); CPU starvation turns real-time asserts flaky. Add a driver guard that asserts pose-ring tick advance ≈ 120 Hz over each sampled window, so contention fails loudly rather than silently. | test-only (invocation + docs) |
| 3 | **Condition waits in the driver.** `waitForReady` +400 ms → wait two `requestAnimationFrame`s (Godot's first UI frame after overlay removal). `clickAt` 200–800 ms → one or two rAF (Godot consumes input per frame, ≈ 16 ms). `toggleRun(N)` after Run → wait until pose sequence advances and the sampled predicate holds; after Reset → wait until the ring stops advancing. Goal loops → resolve from the `CCGOAL_SOLVED` console listener instead of 300 ms polls. `readLatestPose` retry stays. | ≈ 100–130 s | Low: all waits are replaced by the signal they already approximate; the pose-ring parity (even = committed) remains the commit proof. Keep one extra frame of margin on level picker and connection panel (their rebuild spans frames). | driver-only |
| 4 | **Drop `reload()` from non-persistence tests.** 36 of 47 reloads only produce a blank workbench; `selectLevel` already disposes/recreates the client and worker world. Keep both reloads in the 11 save/load tests (persistence across page lifetime is the criterion) and one explicit reload lifecycle check per suite. | ≈ 36 × (2–3 s) ≈ 80 s after #1 (≈ 1000 s today) | Low–medium: cross-test leakage of `capturedCount`, selection or dock state must be reset by the driver; verify `selectLevel` yields `bodyCount 0`. | test + driver |
| 5 | **Early exit from fixed sampling windows** (`while (Date.now()-start < 2500)` loops, 2a2's `toggleRun(4200)` settle): exit once every predicate is satisfied; keep windows whose assertion is a min/max over the whole window (`minPy > 3.0`, "no sample for 600 ms"). | ≈ 20–30 s | None where the assertion is reach-a-state; keep whole-window assertions. | test-only |
| 6 | Move `godot.wasm`/`.pck` `<link rel=preload>` behind isolation so the pre-isolation navigation does not fetch 47 MB twice (`research-wasm-load-time.md` #4). | ≈ 0.1 s × 58 loopback | None | app-side `index.html` |
| 7 | Share one browser across suites via `--test-isolation=none` (Node 26 supports it). | ≈ 10 s | Conflicts with #2 (needs separate browsers) | not recommended |

## 3. Simulation pacing

Not available and not allowed. `worker.js` steps at a fixed `dt = 1/480` driven by the qualified native clock (`performance.now`), and the UI exposes Run/Reset, Pause/Resume only. `docs/planning/requirements.md` §93: "Wall time is not simulated time. Do not change the timeout, disable goals, inject state or add a hidden benchmark mode"; REQ-03 treats synthetic cadence as proof of independence only; REQ-11 forbids setters. A fast-forward would be a hidden benchmark mode and would stop the evidence being actual real-time Chrome behaviour. What is allowed: waiting on the real event instead of a fixed horizon (#3, #5), and reading telemetry globals (`WorkshopPoseRing`, `WorkshopAnimation`), which are observers, not setters. The ≈ 200 s of real-time Runs is therefore the floor.

## 4. Bundle and page load

Cold load currently ≈ 28–30 s × 58 = ≈ 82% of the run. Measured best case on this origin is 3.5 s untrimmed (1.97 s download, 0.75 s Godot/.NET start, 0.3 s qualification); the trimmed-worker working tree is reported at ≈ 1.4 s. The Preview server sends no `Cache-Control`, strips conditional headers and never serves `.br`, so every reload re-downloads ≈ 128 MB: with 304s that is ≈ 2 s × 58 ≈ 115 s saved at best case and, more importantly, the request-queue stall class disappears. GitHub Pages (`max-age=600`, gzip only) is unaffected.

## 5. Flakiness-only waits and their deterministic replacement

| Wait | Absorbs | Replace with |
|---|---|---|
| `waitForReady` +400 ms | Godot's first rendered frame after the overlay is removed | 2 × rAF, or first pose-ring slot readable |
| `clickAt` post-delay 200–800 ms, Escape +200/300 ms | Godot input → UI tree rebuild (dock, picker, connection rows) | rAF wait; for the picker, `waitForReady` already follows |
| `toggleRun(500)` after Reset | Worker retire + new world publish | Ring sequence stops advancing / `bodyCount` restored |
| `toggleRun(100)` "second run" | First committed tick after Run | Sequence > previous, even parity |
| `readLatestPose` 5 × 10 ms retries | Ring null right after worker recreation | Keep (already conditional) |
| anim `waitForTimeout(600/700/3000)` | "no sample appears" negative controls | Inherently time-bounded; bound to the declared curve length |

A small telemetry-only app addition (read-only `WorkshopUi` global: current level, run state, selected part, part mode) would make every UI wait deterministic without touching REQ-11, since it exposes state and sets nothing.

## 6. Recommended target

Serial after #1 and #3: ≈ 58 × 3 s + 29 × 1.5 s + 200 s physics + ≈ 30 s settles + 40 s overhead ≈ **8 min**; with #4 and #5 ≈ 6.5 min; with `--test-concurrency=3` **≈ 3–4 min wall**. Order: #1 first (it is already half done and removes 75% of the time with zero proof risk), then #3, then #2 with the cadence guard, then #4/#5. Re-time one suite uncontended after each step; the `murdoch2/e2e-anim-1b.log` standalone numbers (4.7 s for a reload + level + Run test) show the floor is already reachable on this machine.
