# Research: faster Playwright/Chrome e2e suites without weakening proof

Date: 2026-10-09 (rewrite). Read-only; no suite, publish or Chrome was run. Evidence: per-test `duration_ms` in reviewer logs `scratchpad/murdoch9/f-all-e2e.log` (709 s), `murdoch10` (718 s), `murdoch11` (732 s), `murdoch12/cumulative.log` (731 s; 15 suites, 54 tests, all pass); `tools/e2e/*.test.ts`, `workshop-driver.ts`, `workshop-client.js`, `ui/WorkshopPuzzle.cs`, `tools/Preview/Program.cs`, `project.godot`, Playwright 1.64 `coreBundle.js`. Machine: 16 cores, 64 GB, Chrome 154, Node 26.11.

## 1. Where the 731 s goes now

The trimmed workers removed the load stall. Runs are now deterministic: suite totals vary < 2% across four runs, and CAT-014 takes 75.4 s standalone and inside the full run. Time is sleep- and physics-bound, not load-bound.

Calibration from the code and log: suite minus test sum is 4.6 s for every suite (launch, first load, close). 2b1 #1 and 2a1 #3–#5 give one `reload()` ≈ 1.25 s (≈ 0.85 s load + fixed 400 ms). 2a3 #1 gives `selectLevel` ≈ 1.15 s, of which 1.12 s is fixed sleep. 2a1 #2 measures exactly its two `toggleRun` sleeps (1.65 s).

Static inventory: 15 launches, 65 reloads, 47 `selectLevel`, 156 `toggleRun` sites (232 executed with the two 20-iteration CCD loops), ≈ 370 `clickAt`, 53 drags, 31 save/load menu sequences.

| Bucket | Seconds | Share | Nature |
|---|---|---|---|
| Real-time physics: solve waits, sampling windows, negative-control holds, 40 × 1.2 s CCD windows, CAT-014 rest windows | ≈ 300 | 41% | Floor, except duplicated and reach-a-state windows |
| Fixed UI settles: `waitForReady` +400 ms × 127; `clickAt` 60 ms hold + 200–800 ms × ≈ 370; drags 0.6 s × 53; Escape 200/300 ms | ≈ 250 | 34% | Replaceable by condition waits |
| `toggleRun` Reset/Run settles ≤ 900 ms, plus 70 ms key delay × 232 | ≈ 58 | 8% | Replaceable by pose-ring conditions |
| Browser launch, first load, close (15 × 4.6 s, measured) | 69 | 9% | Structural |
| Page reloads (65 × ≈ 0.85 s) | 55 | 8% | 15 are persistence criteria |

Findings that change the earlier plan:
- **Level selection does not recreate the worker.** `SelectModeFromPicker` submits a new construction to the existing worker (`ui/WorkshopPuzzle.cs:12–55`), and `create()` refuses a second worker without a reload (`workshop-client.js:80`). The driver comment at `workshop-driver.ts:257` is therefore wrong. After `selectLevel`, the `waitForReady` predicate is already true, so only the fixed 400 ms covers the level submission. That is a latent race.
- **Redundant calls.** 14 suites call `reload()` as the first action of test 1, right after `launch()` loaded a fresh page in a fresh profile. 13 `selectLevel('free_workshop')` calls follow a reload that already lands on the free workshop.
- **Identical recipes across suites.** The "Exact Reset and Save/Load" action sequence is byte-identical apart from assertions in 2a3 #5, 2b1 #3, 2b2 #3, 2b3 #3, 2b4 #3 and 2c #3 (≈ 19.6 s each). The two-ramp solve is identical in 2a3 #4 and 2b1/2b2/2b3 #2 (9.5 s each). The 20-iteration CCD test is identical in 2a4 #2 and 2b3 #1 (36.7 s each).
- **Server.** The server sends no `Cache-Control` and strips conditional headers. Subresources carry `Last-Modified`, so on a normal reload Chrome probably serves them from heuristic-fresh cache (with the wasm code cache). This is unverified. At ≈ 0.85 s per reload there is little left to gain.

## 2. Headless Chrome

- `channel: 'chrome', headless: true` on Chrome 154 is already the new headless mode (full Chrome; the old headless shell left the Chrome binary in M132).
- Playwright already passes `--disable-background-timer-throttling`, `--disable-backgrounding-occluded-windows`, `--disable-renderer-backgrounding` and `--enable-unsafe-swiftshader`. The driver's extra arguments repeat them and are harmless.
- **Do not add** `--disable-frame-rate-limit` or `--disable-gpu-vsync`. `admitDisplayRate` rejects a rAF cadence above 240 Hz (`workshop-client.js:55–76`), and an uncapped renderer would take CPU from the physics worker.
- **Unknown:** whether headless WebGL on this Mac runs on Metal/ANGLE or on SwiftShader. Measure it once with `WEBGL_debug_renderer_info` before raising concurrency; SwiftShader would make rendering the main CPU cost per suite.
- **Render resolution:** `project.godot` uses `stretch/mode="canvas_items"` on a 1440×900 base with `canvasResizePolicy: 2`. `deviceScaleFactor: 0.5` with an unchanged 1440×900 CSS viewport should keep every CSS anchor valid while quartering the pixel fill. It does not reduce serial time (rendering is not on the critical path); it only frees headroom for concurrency. Verify Godot's DPR and input mapping with one A/B probe.

## 3. Ranked changes

| # | Change | Est. serial saving | Proof risk | Type |
|---|---|---|---|---|
| 1 | Condition waits. `waitForReady`: drop the +400 ms and wait two rAF. After `selectLevel`, also wait for the pose ring to show the new construction. `clickAt`: one-frame hold, then two rAF. Placement: wait until the ring body count changes. Save: poll the `curious-contraptions-workshop` IndexedDB slot (a read-only observer). `toggleRun`: after Run, wait for an even sequence advance; after Reset, wait for the restored body set. Goal loops: resolve from the `CCGOAL_SOLVED` listener instead of 300 ms polling. | 150–200 s | Low. Each wait becomes the signal it approximated, and it also closes the `selectLevel` race. First confirm that Build mode publishes poses (2a1 #2 reads a zero-body slot, which suggests it does). | driver-only |
| 2 | Consolidate the identical recipes into one test each that carries every slice ID and the union of assertions. | ≈ 160 s | Low technically: the same bundle running the same recipe adds only flake sampling. Needs an owner decision on per-ID traceability (preserve IDs and criteria). | test-only |
| 3 | Delete the 14 first-test reloads and the 13 redundant free-workshop selections. | ≈ 30 s (≈ 20 s after #1) | None | test-only |
| 4 | Early exit from reach-a-state windows: CCD iterations stop once the rebound is seen (the trough is already sampled); 2a2 settle stops after 300 ms in band with \|vy\| < 0.05. Keep whole-window assertions: "no sample for N s", rest drift, "never lights", through-pass. | 15–25 s | None/low | test-only |
| 5 | `--test-concurrency=3`, with a driver guard that fails when committed-tick rate deviates from 120 Hz beyond REQ-03's 0.99–1.01 during any sampled window. Each suite already has its own browser profile, so IndexedDB is isolated. Spec 5.1 notes a host tick of ≈ 6 ms of 8.33 ms (≈ 0.7 core per physics worker); the earlier "browser closed" failure happened under contention. | Wall ÷ ≈ 2.7 (≈ 250 s today) | Medium. Mitigate with the guard, the renderer measurement, optionally #7, and no more than 3 suites at once. | harness config |
| 6 | One Chrome via `launchServer`, each suite connecting with its own `newContext()` (storage, cache and service worker stay per suite). | 15–30 s | Low | harness + driver |
| 7 | `deviceScaleFactor: 0.5` | ≈ 0 serial; headroom for #5 | Low–medium (screenshot resolution, DPR mapping) | driver-only |
| 8 | Preview `Cache-Control: no-cache` with conditional requests allowed | ≈ 0, possibly slower | None; improves bundle-identity exactness if a republish happens mid-run | server |
| — | Replace test-start reloads with `selectLevel` | ≈ 45 s | Medium. Body IDs grow across tests (`_nextId`), so asserted IDs (`id === 1`, `TILE_A`) shift, and the fresh-session start per test is lost. | not recommended |

## 4. Not allowed

- Fast-forwarding or accelerating physics: `requirements.md` §93 says "Wall time is not simulated time… do not add a hidden benchmark mode", and REQ-03 requires real-time pacing.
- Setters, imported solutions or numeric placement menus (REQ-11, AGENTS.md).
- Skipping UI construction or Save/Load through the real menu.
- Mocking or replacing the worker, or using fake pages as primary proof.
- Removing the 15 mid-test persistence reloads or the negative-control holds.

Reading `WorkshopPoseRing`, `WorkshopAnimation`, IndexedDB or the console is observation, not control, so condition waits are allowed.

## 5. Recommended first slice and target

**Slice 1 (driver + trivial test edits):** #1 plus #3, and fix the stale `selectLevel` comment. Acceptance:
- Two consecutive serial full runs, 54/54 pass each.
- A per-test before/after duration table.
- Existing reviewer mutants (`murdoch12/mut-zdrag.js`, `mut-minradius.js`) still fail their target suites.
- One recorded headless renderer string.

Expected: ≈ 731 → ≈ 520 s.

**Then:** #2 after the owner decides on traceability (→ ≈ 380 s), #4 (→ ≈ 365 s), then #5 with the cadence guard.

**Target:** under 6.5 min serial (≤ 8.5 min without consolidation), and under 2.5 min wall at `--test-concurrency=3`. The irreducible floor is the deduplicated real-time physics (≈ 190 s) plus loads (≈ 100 s).
