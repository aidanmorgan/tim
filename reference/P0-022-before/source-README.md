# Curious Contraptions

The current source is an **unqualified GPU cutover** for one Basketball in Free Workshop. Chrome with WebGPU `shader-f16` is required for physics; the retained CPU solver is unshipped reference. The deployed Pages game below is an older release, not this source candidate. Campaign, other catalogue parts, Save and Load are explicitly unavailable in the current admitted target. See [TODO](TODO.md), [current evidence](docs/verification/CAT-001-I/implementation.md), [test obligations](docs/verification/CAT-001-I/unsupported-scene-tests.md) and [tool applicability](docs/verification/CAT-001-I/tool-applicability.md).

## Play online

The production game is deployed to [GitHub Pages](https://aidanmorgan.github.io/tim/).
Pushes to `main` run the C# tests, publish the Release WebAssembly bundle (without playtest diagnostics), and deploy it through `.github/workflows/pages.yml`.
GitHub Pages serves the static SPA under `/tim/`; assets use relative paths and `404.html` returns deep links to the app root while preserving their route in the URL fragment. The game currently selects levels in its own UI rather than parsing URL routes.
The historical deployed 2dog/Godot browser host uses WebGL 2. Its in-process execution is not proof of the current independent GPU worker. Initial loading downloads the engine and .NET runtime; allow extra time on a cold cache.
Local browser screenshots, raw playtest records, generated bundles and agent state are intentionally not committed.

Historical deployed Save/Load supported its version-3 schema, with stable puzzle IDs and explicit typed connection sockets. Older/missing versions and unknown puzzle IDs are rejected; there are no compatibility adapters or automatic save migrations. Existing saved machines must be rebuilt. Development policy: always refactor forward, updating current code, content and tests together. Use enums for closed protocol sets, including connection domains, socket roles, goals and event kinds; preserve those enum types through callers, collections and tests. Use strongly typed IDs for extensible resources, instances and endpoints; convert strings only at validated external boundaries.

Deployment verification (2026-09-27): [initial Actions run](https://github.com/aidanmorgan/tim/actions/runs/36293929096) passed all 89 tests and deployed successfully. On the actual Pages origin, Chrome rendered the workshop, a UI-only level-2 switch-to-lamp solution won, and `/tim/levels/1` returned to `/tim/#/levels/1` and loaded the app. Browser console: no errors or warnings and no playtest diagnostic output. UI Save, a real page reload, then UI Load also restored a placed ramp and its inventory in the same Chrome profile. This does not establish persistence across different browsers, devices, private sessions, or cleared storage.


Deployment uses [GitHub Actions](https://github.com/aidanmorgan/tim/actions/workflows/pages.yml). To roll back a release, revert the offending source commit on `main` and push; the same tests/build must pass before Pages switches to the replacement artifact. For a transient deployment failure, rerun the failed workflow. Do not commit `AppBundle` or enable `PlaytestDiagnostics` for the public build.


A modern 3D contraption-puzzle prototype built with **Godot .NET and C#**.

The development target is **150 progressively taught levels**, with all required puzzle elements retained in [TODO.md](TODO.md). Complete the engine, rendering and independent-worker architecture through **P0-035** in its [delivery priority and technical dependencies](docs/delivery-workflow.md#pipeline-priority) before product, catalogue and campaign expansion.

Required architecture: a dedicated C# simulation Web Worker at fixed 120 Hz (initially four outer substeps), a separate C# animation Web Worker initially at 60 Hz, and Godot/WebGL rendering on the browser main thread's independent display clock. Typed bounded messages and owned buffers connect them. The target is sustained 60 FPS on baseline devices and 90 FPS on qualified displays/devices; these are acceptance requirements, not achieved performance. See the [bridge design](docs/simulation-presentation-bridge.md) and [performance plan](docs/browser-physics-performance.md).

Historical campaign/mechanics snapshot retained for evidence; its counts, ordering and ideal transmission model are not current architecture or completion claims:

The local build now contains 58 draft levels: bumpers at 10–11, adjustable walls at 12–13, battery/motor and switched supply at 14–15, then motor-driven conveyors, conveyor-to-conveyor transmission and reversing gears at 16–18, and counterweight/3D pulley-routing lessons at 19–20, followed by flashlight/solar power and wall-shadow lessons at 21–22 and one-shot delay/timed-solar lessons at 23–24, then a clear gravity-tube introduction at 25 and 45°/90° bend lessons at 26–27 and a joined-tube route at 28. Conveyors require a mechanical input and pass signed rotation to their output; the reverse transmission flips direction 1:1. Belts are distinct from electrical cables. The model transfers ideal shaft speed, not torque or load sharing.

Gameplay, simulation, procedural artwork, workshop UI, serialization, tests, and the preview server are C#. Godot scenes/resources and puzzle JSON remain declarative data. The browser host includes the JavaScript/HTML required to start WebAssembly; there is no GDScript gameplay code. Use C# for new executable logic wherever possible.

## Toolchain

- .NET 10 SDK (the repository's `global.json` selects a compatible installed SDK).
- Godot 4.7.2 .NET API, pinned in the project.
- [2dog](https://2dog.dev/getting-started.html) desktop/test/browser hosts, pinned in `Directory.Build.props`.
- `dotnet workload install wasm-tools` for web publishing.

Godot 4's official C# web export is not supported by the inspected [export documentation](https://docs.godotengine.org/en/latest/tutorials/export/exporting_for_web.html). This project uses the third-party 2dog WebAssembly host to meet the C# plus browser requirements. All 40 levels have Balanced reference playthroughs through the browser UI; the broader difficulty matrix remains incomplete. A standard, non-.NET Godot editor cannot run these C# scripts.

## Run and test

From the repository root:

```sh
dotnet build CuriousContraptions.csproj
dotnet test CuriousContraptions.tests
```

These commands compile the fixed admitted assembly and run scoped host/codec/resource tests. Other preserved test sources are not executed or counted as passes. The desktop host cannot run this browser-only physical target.

To publish and serve locally (the current environment is blocked by MSB4216 in WasmAppBuilder; do not reuse an old bundle as current proof):

```sh
dotnet publish CuriousContraptions.web
dotnet run --project tools/Preview
```

Open http://127.0.0.1:8060. The server serves `CuriousContraptions.web/AppBundle`, so republish after changing game code or content. It binds only to loopback.

## Workshop controls

The current target retains placement, camera, move/rotate, Remove, Undo, Run and Reset for one Basketball. Connection, campaign, difficulty and save/load descriptions below describe retained historical requirements; those paths are not admitted in this cutover. Actual current UI qualification remains pending.

Start in an angled **3D workbench**. Choose a part, move its translucent preview, and click to place it. Drag placed parts across the bench without changing their height. The compact tool strip in the parts drawer uses icon-only **Move**, **Rotate**, and **Remove** buttons, with **Connect** only for parts that can send power. Move swaps the rotation rings for three coloured arrows: green lifts, coral moves sideways, and blue adjusts depth. Drag an arrow tip in either direction; hold Shift to align the chosen axis to the tenth-unit workbench grid, including parts that started off-grid. Rotate restores the rings. The active mode stays highlighted. Names and quantities sit beside the part pictograms within the same clickable inventory row. Secondary actions—Save, Load, difficulty, camera buttons, and Fine rotate—live in **Menu**. The puzzle title stays compact; **Goal** opens the introduction, then **Hint** reveals extra help. Select a placed part to reveal three coloured 3D rotation rings with spherical grab handles: coral X, green Y, blue Z. Drag a handle or ring to rotate continuously, hold Shift to snap to 15°, and press Escape to cancel. Each drag is one Undo action. Edge-on rings use a screen-tangent fallback. Fine rotate exposes Tip / Turn / Tilt in 5° steps for both previews and placed parts. Every placed part—including fixed fixtures—and the placement preview gets dashed, unfilled bounding boxes projected onto the floor, back, and side planes. Gold, blue, and terracotta distinguish the three directions; the selected part has darker outlines. The reference walls are fully transparent (no wall surfaces), and there are no filled silhouettes or connecting lines. These visual-only guides disappear during simulation. New parts start at height 3; lifting a preview changes its placement height.

Camera buttons rotate the view in 45-degree steps; **Reset camera** restores the initial angle. **Menu → Fine rotate** exposes Front view and the Back/Middle/Front layer controls. Choosing a layer does not move an existing part: use **Move selected here** when intended. In Front view, dragging edits sideways and vertically within that depth layer. The bright sky, wooden bench, cream panels, and chunky yellow Run button are original, procedurally drawn artwork inspired by the playful feel of The Incredible Machine—not copied assets.

Drag movable parts without a centre jump. Cancel/deselect and Undo make experimentation reversible. Wiring highlights eligible powered targets. Run the machine, then Reset to recover your original arrangement. Author-controlled difficulty assistance remains automatic and is not exposed as an editing action.

The parts toolbox grows to show its inventory and only scrolls when the list would exceed the screen. Click anywhere on a part row—icon, name or quantity—to select its placement preview, then click the workbench to place it. Empty inventory rows remain disabled.

Keyboard shortcuts remain optional: W/S moves forward/backward along the workbench relative to the camera heading, A/D strafes left/right, and Q/E continuously turns the camera left/right. Movement stays level and diagonals have the same speed. Rotate parts with the 3D rings or Menu → Fine rotate; Q/E no longer tilts parts. Page Up/Down move through depth, Escape cancels, and Ctrl/Cmd+Z undoes. Right-drag still orbits and the wheel zooms; Reset camera restores the initial view.

## Required delivery workflow

Every work item—including code, refactors, fixes, tests, tooling, design, documentation, research, cleanup and verification—is delivered by a dedicated implementation subagent and verified by a different independent adversarial review subagent. The reviewer independently runs/reproduces applicable new or affected checks, verifies any permitted reuse of prior independently reviewed evidence under the [scoped verification rules](AGENTS.md#scoped-evidence-and-publication), and investigates direct/transitive impacts; current evidence must show complete scoped acceptance and zero unresolved unintended regressions. Missing/stale review or unknown impact coverage blocks completion, successors and publication. Fixes return to an implementation subagent and require separate re-review. Follow [AGENTS.md](AGENTS.md) and [REQ-14's paired-subagent workflow](docs/delivery-workflow.md#paired-subagent-workflow); self-review and coordinator-only approval do not qualify. For publication-required tasks, nonterminal reviewer approval authorizes publication of the exact reviewed snapshot only; required remote/deployed proof must then pass independent verification before final Pass, task closure or successors.

## Extending the engine

Finish the P0 engine gate before adding catalogue elements. Every required generic process must already have declared model/units, native analytic controls and real-UI fixture proof; current parts must be migrated and qualified through both workers and the renderer.

1. Map the new element/mode to qualified generic capabilities, geometry, materials, typed ports and finite stores. A missing law reopens the engine gate.
2. Author its C# construction/declaration adapter, scene/resource and original presentation assets. Physical state and controller laws belong to the simulation worker; cosmetic tracks belong to the animation worker; Godot property application belongs to the browser presenter.
3. Use enums for closed sets and typed extensible IDs through callers, collections and tests. Validate current serialization boundaries and reject unsupported input.
4. Prove the element through actual Chrome/Playwright construction, positive/control behavior, typed integration, exact Run/Reset/save, independent cadences and incremental performance; compile/build, review and commit/push it before starting the next element.

The registry discovers resources; display labels never select behavior. New parts using supported processes must not add solver code or part-specific physics callbacks. Update current code, content, tooling and tests together and remove replaced paths. No old/new solver switch, backwards-compatibility shim, alias, automatic migration or synchronous fallback is permitted. Historical artifacts remain evidence, not supported current input.

Author-controlled placement assistance and physics difficulty use the same authoritative worker and explicit typed profiles. They preserve containment, finite resources, causal goal success and exact construction restoration. See the [difficulty contract](docs/difficulty.md); older placement-only behavior is historical implementation evidence.

## Current evidence and remaining work

The following native/browser observations describe earlier 40/58-level revisions. Retain their evidence scope; current status, failures, source/artifact identities and the 150-level target are tracked in [TODO.md](TODO.md). They do not certify the worker migration or current campaign.

The native suite covers all 58 draft campaign solutions at three precision settings, legal inventory/placement bounds, no untouched puzzle auto-solving, catalog and workshop creation, restart repeatability, depth separation, required-ball goals, power cycles, and automatic assistance bounds, interpolation, reset, successful-placement sweeps, powered conveyor transport through multiple orientations, and beginner interaction flows (preview, cancel, optional layers, height-preserving dragging, lift gestures, three-plane projections, all-axis rotation and mixed rotations through 90°, undo, run/reset controls, icon-only actions, and switching the widget between axis-constrained movement and rotation).

Fifty-eight draft levels now exist, authored as tutorials followed by combinations of motion, signals, domino chains, powered fans, conveyors, and depth-oriented routes. Their difficulty progression and variety still need hands-on playtesting; this is not the finished game. Chrome rendering, icon visibility, movement/rotation mode switching, lifting/sliding/rotating placed parts, camera controls, and floor bouncing have been checked through Playwright. The original 40-level campaign has historical successful Balanced reference playthroughs using the game UI, documented in [the browser report](docs/browser-playtest.md). Historical captures and level numbers belong to their recorded campaign hashes, not the current reordered campaign. The full difficulty/placement-error matrix remains incomplete; partial records do not prove the whole campaign's tolerance behavior. The original game's integer physics have not been reproduced or quantitatively matched; cross-platform determinism is unproven. The realism toggle currently changes contact friction only. Ropes now support explicit lengths, slack, tension and fixed pulley routing; powered narrow-ray lasers, separately supplied receivers and adjustable flat mirrors and 50:50 beam splitters now support initial optical routing. Moving-block ratios, rope cutting, chain-drive variants, colour optics/lenses and character behaviors remain future work. After P0-035 closes, complete component coverage before final 150-level balancing and the exhaustive difficulty matrix. Every affected component still needs focused proof during its own delivery.

The [research dossier](docs/research.md) records historical references, puzzle families, physics findings, and the evidence required before claiming original-like behavior.

## UI icon credits

Toolbar icons are bundled SVGs from [Lucide](https://lucide.dev), recoloured to match the workshop. Their ISC and inherited Feather MIT notices are included in [assets/icons/LICENSE.txt](assets/icons/LICENSE.txt) and packaged with the game. Icons load as Godot textures instead of font glyphs. Action tooltips are disabled. Parts use original vector pictograms generated in C#, with names and inventory counts alongside. The goal and advanced controls stay closed until requested.
