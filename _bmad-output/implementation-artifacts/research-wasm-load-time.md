# Research: WebAssembly load and readiness time (P0-034 / Epic 16)

Date: 2026-10-08. Read-only investigation; no source, config or bundle was changed. Raw waterfalls: `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/537235c4-e933-45de-99f1-70c27b375e0c/scratchpad/wasm-load/waterfall-s1.json`, `waterfall-s2.json` (script `measure.mjs`, Chrome 154 headless via the Playwright connector, `--enable-features=SharedArrayBuffer`, target `http://127.0.0.1:8060/`).

## 1. What is shipped (`CuriousContraptions.web/AppBundle`, 195 MB on disk, 1236 files, 804 of them `.br`/`.gz` siblings)

| Area | Uncompressed fetched | gz (deployed wire) | br sibling | Notes |
|---|---|---|---|---|
| `godot.wasm` (Godot 4.7.2 mono + .NET native, relinked by 2dog) | 47.4 MB | 11.4 MB | 10.0 MB | nothreads template; `godot.pck` is 42 KB (nothing to strip) |
| `_framework/` main .NET host (22 assemblies, trimmed partial) | 29.9 MB | 3.5 MB | 3.4 MB | includes **`dotnet.native.js.symbols` 19.4 MB (1.9 MB gz), fetched on every boot** |
| `simulation/_framework/` physics worker | 26.5 MB, 175 assemblies | 9.5 MB | 9.1 MB | untrimmed, `debugLevel -1`, 2 `.pdb`, `.map`, `.symbols`, 3 ICU shards |
| `animation/_framework/` animation worker | 26.5 MB, 175 assemblies | 9.5 MB | 9.1 MB | identical runtime copy of the above |
| shell (`index.html`, `godot.js`, `workshop-*.js`, `native-clock.js`) | 0.1 MB | <0.1 MB | partial | `native-clock.js`, isolation worker have no `.br` |
| **Cold total actually requested (measured)** | **128.5 MB, 416 requests** | **~32–35 MB** | | 368 of the 416 requests are worker assemblies |

Serving: `tools/Preview/Program.cs` has `UseResponseCompression()` commented out, never serves the `.br`/`.gz` siblings, sends no `Cache-Control`, no COOP/COEP, and strips `If-None-Match`/`If-Modified-Since`, so every local reload re-downloads the full 127.6 MB (measured warm = cold bytes). GitHub Pages (`.github/workflows/pages.yml` uploads the folder verbatim; verified with HEAD against `aidanmorgan.github.io/tim/`) serves on-the-fly **gzip only, `cache-control: max-age=600`, no brotli, no COOP/COEP** — the `.br` siblings are dead weight there and cross-origin isolation comes from the `workshop-isolation-worker.js` service worker plus one forced reload.

## 2. Build settings in effect

| Setting | Main host (`CuriousContraptions.web`) | Workers (`Simulation`, `Animation.Worker`) | Load-time effect |
|---|---|---|---|
| `RunAOTCompilation` | unset (interpreter + jiterpreter) | unset | AOT would grow downloads 2–3x; not a load win |
| `WasmEnableSIMD` | true (2dog default) | runtime-pack default (true); stock `dotnet.native.wasm`, no relink | required by `wasm-simd128` |
| `WasmEnableThreads` | false | false | n/a |
| `PublishTrimmed`/`TrimMode` | true / partial (2dog) | **none — bundle comes from `Build`, not `Publish`** (`GetWorkshopWorkerBundle` in both csproj) | 175 assemblies, 26.5 MB per worker |
| `InvariantGlobalization` | false (`sharded`, EFIGS shard ~0.55 MB per runtime) | false | 3 ICU loads |
| `WasmEmitSymbolMap` | false in 2dog targets, but **manifest still lists `wasmSymbols`** (stale relink) | manifest lists 0.48 MB symbols each | 19.4 MB extra on main boot |
| `WasmInitialHeapSize` | 256 MB (2dog) | runtime default | memory, not download |
| `WasmBuildNative` | true | false | – |
| Webcil | yes (`.wasm` assemblies) | yes | already minimal format |
| Lazy assemblies / single-file bundle | none / not used | none | – |
| Precompression | 2dog `TwoDogWebPrecompress=true`, level `Optimal` | copied siblings | unused by both servers |
| Godot export (`export_presets.cfg`) | `thread_support=false`, PWA off, `ensureCrossOriginIsolationHeaders=false`, filtered export (`.pck` 42 KB) | | fine |

## 3. Boot sequence (from `index.html`, `workshop-isolation.js`, `ui/Workshop.cs`, `engine/MachineWorld.Gpu.cs`, `workshop-client.js`)

Strictly sequential: (1) register SW, handshake, `location.reload()` to gain isolation; (2) after reload, `engine.init` + `.pck` preload, Godot/.NET boot, `callMain`; (3) Godot `Workshop._Ready` → `World.InitializeWorkshop()` → `JSHost.ImportAsync("workshop-client.js")` → `create()` spawns **both** workers (parallel with each other) and starts the 5 s qualification timer; (4) each worker `admitNativeClock()` → `dotnet.create()` → downloads its own 26.5 MB runtime → `ready`; (5) clock qualification (`ProbeCapacity=8`, `StartupSpacing=10 ms` ⇒ ≥80 ms theoretical) → `isAnimationQualified`; (6) overlay removed. The `<link rel=preload href=godot.wasm>` fires **before** the isolation reload: sample s2 cold fetched `godot.wasm` twice (95 MB in the "godot/shell" bucket).

## 4. Measurements (local Kestrel, loopback; 2 samples cold+warm; concurrent headless suite was running — load average 5–6, 68 Chrome processes)

| Milestone (ms from navigation) | s1 cold (best) | s1 warm | s2 cold | s2 warm |
|---|---|---|---|---|
| isolation reload committed | 46 | — (no reload) | 58 | — |
| godot.wasm + main `_framework` downloaded (`2dog:downloads-done`) | 1987 | 154 | 1970 | ~1000 |
| `callMain` → Godot running in browser | 2168 → 2917 | 175 → 411 | 2191 → 2929 | — |
| workers spawned / pose ring published | 3091 | 548 | 3108 / 3130 | 2681 |
| both worker bundles fetched (368 requests) | 3087 → 3271 | 537 → 30034 | 3109 → 35723 | 2672 → 35744 |
| `CCGPU_READY` | 3293 | 30057 | 35743 | — |
| animation clock qualified | 3387 | 30162 | 35836 | 35855 |
| **overlay removed (ready)** | **3479** | 30228 | 35931 | 35942 |

Best-case cold readiness is **3.5 s on loopback** (1.97 s downloading 77 MB, 0.75 s Godot/.NET start, 0.17 s `workshop-client.js` import on a busy main thread, 0.2 s worker fetch, 0.1 s runtime start, 0.3 s clock qualification). Three of four samples took 30–36 s: individual tiny worker assemblies showed 3.4–3.7 s TTFB (`System.IO.wasm` 5 KB, 3.58 s) — serialized stalls across 368 requests on six HTTP/1.1 connections to a contended Kestrel. Contention caveat: the stall cause cannot be separated from the concurrent suite, but the exposure (hundreds of per-assembly requests) is intrinsic to the untrimmed worker bundles. Scaled to the P0-034 **20 Mbps / 50 ms profile**, the ~32–35 MB gzip wire alone is **≈13–14 s**, consistent with the historical ≈12.6 s; the ≤5 s target is byte-bound, not CPU-bound. No clean warm total was obtained (both warm samples stalled); warm pose-ring publication was 548 ms.

## 5. Ranked recommendations (savings estimated against the 20 Mbps profile unless stated)

| # | Change | Saving | Kind | Constraints |
|---|---|---|---|---|
| 1 | Publish (not Build) the two workers: `PublishTrimmed` + `TrimMode=full/partial` with `TrimmerRootAssembly` for the `[JSExport]` entry assemblies, `InvariantGlobalization=true`, strip `.pdb`/`.map`/`.symbols`, `debugLevel 0`, drop unused ICU shards. Expected per worker ≈3 MB gz (CoreLib 1.55 + native 1.18 + app 0.3) and ~25 requests instead of 184 | **≈13 MB gz, ≈5 s; removes ~340 requests and the 3.5 s TTFB stall class** | build-config (web csproj `GetWorkshop*Bundle` targets + worker csproj) | keeps SIMD; a prior commit ("Restore worker dependencies before publishing") shows trimming must be verified against JSImport/JSExport, `WorkshopWire` and animation reflection |
| 2 | Remove `dotnet.native.js.symbols` from the main boot manifest (fresh relink with the existing `WasmEmitSymbolMap=false`; never delete the file alone — the 2dog targets warn a manifest 404 fails boot) | 19.4 MB raw / 1.9 MB gz, **≈0.8 s** (160 ms loopback) | build-config | none |
| 3 | Brotli on the wire: host that honours precompressed `.br` (Cloudflare Pages / Netlify / own origin) or a SW that fetches `.br` and decodes; GitHub Pages cannot | ≈8% of wire (3 MB gz→br), **≈1.2 s** | hosting / SW code | GitHub Pages lock-in; SW rule "no cache, no offline path" must stay |
| 4 | Serve COOP/COEP natively (same host move as #3) and drop the isolation reload; until then move the `godot.wasm`/`.pck` `preload` hints behind `prepareIsolation` so the pre-reload navigation does not start a 47 MB fetch | 50–100 ms loopback; up to one wasted RTT + partial download on slow links | hosting, or small `index.html` change | SAB/COOP+COEP remain required |
| 5 | Start both workers' `dotnet.create()` from `index.html` right after isolation (parallel with Godot boot) and let `BrowserWorkshopClient.Create` attach to the already-created workers; `modulepreload` `workshop-client.js` | ≈0.4–0.5 s cold (worker fetch + runtime create + `workshop-client.js` import overlap Godot's 0.75 s start) | code (JS + C# lifecycle; worker clock/GPU prep already overlaps) | touches the frozen P0-005/006 lifecycle contract — needs the paired review |
| 6 | Local Preview parity: enable precompressed sibling serving (`Content-Encoding`), `Cache-Control`, stop stripping conditional headers; add `Cache-Control: immutable` + fingerprinted assets on a real host | dev-loop only: 127 MB per reload → ~0; deployed `max-age=600` cannot change on Pages | tools / hosting | none |
| 7 | Clock qualification: `ProbeCapacity 8 × 10 ms` is already ≈100 ms; measured pose-ring → qualified 296 ms | ≤0.2 s | code (P0-006 contract) | low value; do not weaken the clock gate |
| 8 | `godot.wasm` (10 MB br, one-third of the wire): custom engine build with unused Godot modules disabled, `-Oz`/wasm-opt | unknown, plausibly 20–30% of 10 MB | needs the 2dog native package (vendor) | not measurable here |
| 9 | AOT for the physics worker | **negative for load** (+2–3x managed bytes); only justified by solver CPU at P0-034 with partial/profile-guided AOT | build-config | measure CPU vs bytes per `docs/browser-physics-performance.md` |

Already effective: streaming wasm instantiation (compile overlaps download; `2dog:dotnet-created` 200 ms after fetch), webcil assemblies, filtered `.pck`, parallel spawn of the two workers.

Projected: #1 + #2 bring the 20 Mbps wire to ≈17–19 MB gz (≈7–8 s); adding #3 ≈6.5–7 s. Reaching ≤5 s additionally needs #8 or a faster profile; on loopback #1, #2 and #5 should take the best cold case from 3.5 s to ≈2.7 s and eliminate the 30 s stall mode.

## Not measured

20 Mbps profile directly (estimated from bytes); deployed-origin timing; Godot module composition of `godot.wasm`; SIMD confirmed only by configuration, not opcode scan; a clean warm total under contention.
