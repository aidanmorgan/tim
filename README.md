# Curious Contraptions

## Play online

The production game is deployed to [GitHub Pages](https://aidanmorgan.github.io/tim/).
Pushes to `main` run the C# tests, publish the Release WebAssembly bundle (without playtest diagnostics), and deploy it through `.github/workflows/pages.yml`.
GitHub Pages serves the static SPA under `/tim/`; assets use relative paths and `404.html` returns deep links to the app root while preserving their route in the URL fragment. The game currently selects levels in its own UI rather than parsing URL routes.
This single-threaded 2dog host needs WebGL 2, but no custom cross-origin isolation headers or server backend. Initial loading downloads the engine and .NET runtime; allow extra time on a cold cache.
Local browser screenshots, raw playtest records, generated bundles and agent state are intentionally not committed.

A modern 3D contraption-puzzle prototype built with **Godot .NET and C#**.

Gameplay, simulation, procedural artwork, workshop UI, serialization, tests, and the preview server are C#. Godot scenes/resources and puzzle JSON remain declarative data. The browser host includes the JavaScript/HTML required to start WebAssembly; there is no GDScript gameplay code. Use C# for new executable logic wherever possible.

## Toolchain

- .NET 10 SDK (the repository's `global.json` selects a compatible installed SDK).
- Godot 4.7.2 .NET API, pinned in the project.
- [2dog](https://2dog.dev/getting-started.html) desktop/test/browser hosts, pinned in `Directory.Build.props`.
- `dotnet workload install wasm-tools` for web publishing.

Godot 4's official C# web export is not supported by the inspected [export documentation](https://docs.godotengine.org/en/latest/tutorials/export/exporting_for_web.html). This project uses the third-party 2dog WebAssembly host to meet the C# plus browser requirements. Successful publishing is not evidence of browser compatibility; full campaign browser playtesting remains outstanding. A standard, non-.NET Godot editor cannot run these C# scripts.

## Run and test

From the repository root:

```sh
dotnet build CuriousContraptions.csproj
dotnet test CuriousContraptions.tests
dotnet run --project CuriousContraptions.2dog
```

To publish and serve locally:

```sh
dotnet publish CuriousContraptions.web
dotnet run --project tools/Preview
```

Open http://127.0.0.1:8060. The server serves `CuriousContraptions.web/AppBundle`, so republish after changing game code or content. It binds only to loopback.

## Workshop controls

Start in an angled **3D workbench**. Choose a part, move its translucent preview, and click to place it. Drag placed parts across the bench without changing their height. The compact tool strip in the parts drawer uses icon-only **Move**, **Rotate**, and **Remove** buttons, with **Connect** only for parts that can send power. Move swaps the rotation rings for three coloured arrows: green lifts, coral moves sideways, and blue adjusts depth. Drag an arrow tip in either direction; hold Shift to align the chosen axis to the tenth-unit workbench grid, including parts that started off-grid. Rotate restores the rings. The active mode stays highlighted. Names and quantities sit beside the part pictograms, outside the buttons. Secondary actions—Save, Load, difficulty, camera buttons, and Fine rotate—live in **Menu**. The puzzle title stays compact; **Goal** opens the introduction, then **Hint** reveals extra help. Select a placed part to reveal three coloured 3D rotation rings with spherical grab handles: coral X, green Y, blue Z. Drag a handle or ring to rotate continuously, hold Shift to snap to 15°, and press Escape to cancel. Each drag is one Undo action. Edge-on rings use a screen-tangent fallback. Fine rotate exposes Tip / Turn / Tilt in 5° steps for both previews and placed parts. Every placed part—including fixed fixtures—and the placement preview gets dashed, unfilled bounding boxes projected onto the floor, back, and side planes. Gold, blue, and terracotta distinguish the three directions; the selected part has darker outlines. The reference walls are fully transparent (no wall surfaces), and there are no filled silhouettes or connecting lines. These visual-only guides disappear during simulation. New parts start at height 3; lifting a preview changes its placement height.

Camera buttons rotate the view in 45-degree steps; **Reset camera** restores the initial angle. **Menu → Fine rotate** exposes Front view and the Back/Middle/Front layer controls. Choosing a layer does not move an existing part: use **Move selected here** when intended. In Front view, dragging edits sideways and vertically within that depth layer. The bright sky, wooden bench, cream panels, and chunky yellow Run button are original, procedurally drawn artwork inspired by the playful feel of The Incredible Machine—not copied assets.

Drag movable parts without a centre jump. Cancel/deselect and Undo make experimentation reversible. Wiring highlights eligible powered targets. Run the machine, then Reset to recover your original arrangement. Author-controlled difficulty assistance remains automatic and is not exposed as an editing action.

Keyboard shortcuts remain optional: WASD pans the view (W/S up/down, A/D left/right), Q/E tilts, Page Up/Down move through depth, Escape cancels, and Ctrl/Cmd+Z undoes. Right-drag orbits and the wheel zooms.

## Extending the engine

1. Create a C# subclass of `MachinePart` with visual children and collision proxies.
2. Attach it to a `Node3D` scene in `parts/scenes`.
3. Add a `PartDefinition` resource in `parts/catalog` referencing that scene, with a unique ID and default parameters.
4. Add catalog-instantiation and behavior tests, then add the part to puzzle inventories.

The registry discovers resources; the palette does not need a new switch statement. The scene tree owns instances/visuals, while explicit stable-ID connections represent power links. More connection types require additional engine behavior.

The solver uses fixed 120 Hz ticks, four substeps, stable part ordering, and quantized floating-point state. Sphere/box and sphere/sphere collisions use XYZ coordinates. The finite workbench is solid: its two collision boxes share dimensions with the visible deck and base, support ball bounce and settling, and allow falls beyond its edges. Each puzzle part owns its difficulty curve. Automatic placement correction is bounded by authored position/angle windows and eased over time; it is not a player action. Meshes and colliders move together, and reset restores the player's unassisted build. Receiver guide force, capture requirements, and trigger thresholds are also authored per instance. See [difficulty authoring](docs/difficulty.md).

## Current evidence and remaining work

Sixty-one native test cases pass, including all 40 draft campaign solutions at three precision settings, legal inventory/placement bounds, no untouched puzzle auto-solving, catalog and workshop creation, restart repeatability, depth separation, required-ball goals, power cycles, and automatic assistance bounds, interpolation, reset, successful-placement sweeps, powered conveyor transport through multiple orientations, and beginner interaction flows (preview, cancel, optional layers, height-preserving dragging, lift gestures, three-plane projections, all-axis rotation and mixed rotations through 90°, undo, run/reset controls, icon-only actions, and switching the widget between axis-constrained movement and rotation).

Forty draft levels now exist, authored as tutorials followed by combinations of motion, signals, domino chains, powered fans, conveyors, and depth-oriented routes. Their difficulty progression and variety still need hands-on playtesting; this is not the finished game. Chrome rendering, icon visibility, movement/rotation mode switching, lifting/sliding/rotating placed parts, camera controls, and floor bouncing have been checked through Playwright. All 40 levels still require complete browser playthroughs. The original game's integer physics have not been reproduced or quantitatively matched; cross-platform determinism is unproven. The realism toggle currently changes contact friction only. Ropes, belts, lasers, and character behaviors remain future work.

The [research dossier](docs/research.md) records historical references, puzzle families, physics findings, and the evidence required before claiming original-like behavior.

## UI icon credits

Toolbar icons are bundled SVGs from [Lucide](https://lucide.dev), recoloured to match the workshop. Their ISC and inherited Feather MIT notices are included in [assets/icons/LICENSE.txt](assets/icons/LICENSE.txt) and packaged with the game. Icons load as Godot textures instead of font glyphs. Action tooltips are disabled. Parts use original vector pictograms generated in C#, with names and inventory counts alongside. The goal and advanced controls stay closed until requested.
