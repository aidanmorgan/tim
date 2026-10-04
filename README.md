# Curious Contraptions

Curious Contraptions is a 3D construction puzzle game: build a machine from readable toy-like parts, connect its mechanisms, Run it, observe the result and Reset to refine the construction. The intended game has **150 progressively taught levels**, a Free Workshop, every named puzzle element and variant, and Forgiving, Balanced and Precise difficulty profiles. The approved sky/wood/cream/navy/gold palette and sculptural art direction are defined in [DESIGN.md](DESIGN.md).

**Current playable implementation:** Basketball, Receiver and Ramp use the generic WGSL f16 physics engine and shared C# animation worker. First principles supports manual placement of two ramps, the authored Receiver assistance at precision 0/.45/1, named capture and once-only Solved feedback. Actual Chrome controls verify puzzle Save → edit → Load → Run → capture → exact Reset, canonical ramp dimensions, fixed parts and inventory limits. Saves retain typed instances, puzzle settings and stable identities through transactional storage and the same GPU admission path as editing. Physical ramp nudging remains unsupported; its authored data and future requirement are retained. Other named parts and puzzles remain to be admitted. First principles has independent deployed playable verification. The current local checkpoint adds Impact switch → Signal lamp through typed activation connections: accepted contact latches the switch and lamp, and shared Animation presents the button press and light. Its actual UI controls pass; independent publication checks remain pending. Electrical pass-through is unsupported and remains a separate obligation. [TODO.md](TODO.md) owns current review/publication status; playable acceptance is not full engine, device or performance qualification.

Use the [working documentation index](docs/README.md) to find current design, requirements, delivery and tooling.

## Intended architecture and delivery

The simulation worker hosts **WebGPU/WGSL f16 physics**. C# owns typed Half construction/content/read models, command admission, integer identities/ticks, discrete controllers and atomic transactions. There is no CPU physical fallback or second numerical authority. A separate C# animation worker owns cosmetics; the browser main thread owns Godot/WebGL rendering, input and final presentation.

Simulation runs at 120 Hz, initially four outer substeps, animation initially at 60 Hz, and presentation follows its own display clock. Sustained 60 FPS on baseline devices and 90 FPS on qualified devices are acceptance targets. See the [numeric/execution contract](docs/gpu-f16-physics.md), [simulation–presentation bridge](docs/simulation-presentation-bridge.md) and [performance plan](docs/browser-physics-performance.md).

Complete physics, rendering and independent workers through **P0-035** using small playable slices of existing parts before new components, general UX or campaign expansion. Then deliver each named element/variant with individual evidence and publication; component coverage precedes exhaustive campaign/difficulty sweeps. All original behavioral requirements remain in the [source requirements](docs/planning/requirements.md) and [work register](docs/planning/work-register.md).

## Play online

The published game is available on [GitHub Pages](https://aidanmorgan.github.io/tim/). [TODO.md](TODO.md) distinguishes the verified deployed checkpoint from the current local candidate.

Deployment uses [GitHub Actions](https://github.com/aidanmorgan/tim/actions/workflows/pages.yml) and [.github/workflows/pages.yml](.github/workflows/pages.yml). Publishing must follow [independent snapshot approval and deployed verification](docs/delivery-workflow.md#paired-subagent-workflow); a build or receipt alone does not close qualification. Do not commit AppBundle or enable PlaytestDiagnostics for the public build. A rollback is a source revert through the same reviewed build/deployment path.

## Toolchain

- .NET 10 SDK, selected by [global.json](global.json).
- Godot .NET API and 2dog desktop/test/browser hosts, pinned in [Directory.Build.props](Directory.Build.props).
- WebAssembly tools: `dotnet workload install wasm-tools`.
- Chrome with WebGPU and the required `shader-f16` feature for this physical target.

C# implements gameplay orchestration, authoring, UI, tooling and the worker hosts where viable; WGSL implements authoritative numerical physics. Godot scenes/resources and puzzle data are declarative. The browser host includes the JavaScript/HTML needed to start WebAssembly and bridge external APIs. The desktop host and a non-.NET Godot editor cannot run this browser-only physical target.

## Run and test

From the repository root:

```sh
dotnet build CuriousContraptions.csproj
dotnet test CuriousContraptions.tests
dotnet publish CuriousContraptions.web
dotnet run --project tools/Preview
```

Open http://127.0.0.1:8060. The preview serves CuriousContraptions.web/AppBundle on loopback; republish after changing code/content and bind browser evidence to that bundle. Commands describe the normal workflow, not a claim that every check currently passes. Consult [TODO](TODO.md) for active native-test failures and current Production-export evidence; do not treat a stale bundle as a successful new build.

The admitted game uses the independent shared C# Animation worker for its presentation bindings. Preserved excluded test sources are not executed or counted as passes. Use [tool applicability](docs/verification/CAT-001-I/tool-applicability.md) and each [tool README](docs/README.md#tools-and-current-records) to select valid checks.

## Workshop controls

Choose a part to show its translucent preview, then click to place it. The contextual drawer provides Move, Rotate and Remove, with Connect or Resize when applicable. The three-axis widget supports direct manipulation; Shift constrains/snaps, Escape cancels, and one gesture is one Undo. Run evaluates the construction; Reset restores its exact admitted values.

Right-drag orbits, the wheel zooms, WASD moves on the ground plane and Q/E turns the camera. Menu holds secondary camera/fine-rotation controls. Goal starts collapsed and Hint requires a separate action. Save replaces one local construction slot while Building; Load restores that construction through atomic admission. First principles offers manual ramp placement and its authored precision settings. In Free Workshop, select a switch, choose Connect ActivationOut, then select a lamp and confirm ActivationOut → ActivationIn. Disconnect signal removes that canonical edge. Further campaign levels, electrical routing and physical ramp nudging remain to be admitted.

The complete [interaction and spatial contract](DESIGN.md#interaction-and-spatial-clarity) preserves placement, camera, menu, keyboard, inventory, projections and contextual connection behavior. Author-controlled difficulty assistance is automatic; it is not a player “solve” or “nudge” tool. See [difficulty](docs/difficulty.md).

## Required delivery workflow

Follow [AGENTS.md](AGENTS.md) and [the delivery workflow](docs/delivery-workflow.md#paired-subagent-workflow). One small functional slice uses a dedicated implementation owner and a different independent adversarial reviewer through fixes. Keep exact source/artifact identities, positive/control/boundary proof, affected build/Chrome/lifecycle/performance checks and zero unresolved scoped regressions. Required publication needs snapshot approval first and independent deployed verification before terminal Pass.

## Extending the engine

Before adding a component, select its [bounded INVEST scope](docs/planning/invest-index.md) and read the exact source behavior, prerequisites and stage criteria. Map the part to generic physical capabilities, geometry, materials, typed ports and finite stores. A missing required law reopens the engine gate.

Parts declare construction and presentation; generic WGSL laws and typed C# discrete controllers own behavior. Closed sets stay enums end-to-end; extensible identities stay typed IDs. Validate genuine external boundaries and reject unsupported input atomically. Update current callers/content/tools/tests together, remove replaced paths and retain exact Run/Reset and supported save/load proof. No compatibility shims, automatic migrations, old/new solver selection or synchronous fallback.

## Current evidence and remaining work

[TODO](TODO.md) owns the current status. [Requirements](docs/planning/requirements.md) contain complete named behavior/control and the 150-level teaching target; the [register](docs/planning/work-register.md) keeps stable owners, criteria and technical prerequisites. Use the [working documentation index](docs/README.md) for current models and tools.

## UI icon credits

Toolbar icons are bundled [Lucide](https://lucide.dev) SVGs recoloured to the approved palette. Retain [ISC and inherited Feather MIT notices](assets/icons/LICENSE.txt) in the game. Parts use original vector pictograms, names and inventory counts. Icons load as Godot textures; action tooltips remain disabled and goal/advanced controls stay closed until requested.
