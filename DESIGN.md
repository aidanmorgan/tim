# Visual design contract

**Current architecture:** [Canonical IEEE-754 f32 game values and WASM SIMD physics](docs/gpu-f32-physics.md) define the numerical model. Current design/acceptance is self-contained; implementation and qualification status are in [TODO](TODO.md).

This is the intended **Curious Contraptions** visual and interaction contract, including the owner's **Monument Valley-inspired art direction**. Preserve the approved palette, readable mechanisms and contextual construction controls. The art direction takes precedence over older implementation form, while source-specific behavior and physical truth remain mandatory.

Each functional design/implementation slice uses one dedicated implementation subagent and a different independent adversarial review subagent, retained through its supporting documentation, verification and fixes. The reviewer verifies the actual change, all required-now acceptance criteria and a justified direct/transitive impact scope; unresolved regressions or unknown coverage block completion and publication. This includes documentation, visual design, cleanup and verification. See [REQ-14 and the mandatory workflow](docs/delivery-workflow.md#paired-subagent-workflow) and [project rules](AGENTS.md); coordinator-only review and self-review are insufficient. Where publication is required, the reviewer first approves the exact snapshot for publication only; the task remains Incomplete until the reviewer verifies all required publication/deployed proof and issues the final Pass.

Use the [focused engineering rules](docs/delivery-workflow.md#focused-agentic-engineering) for design changes: state the intended visible behavior and material assumptions, choose the simplest complete change within this contract, and verify the affected composition, interaction or motion. Preserve the approved palette and physical truth; broader visual redesign or speculative systems need a current requirement. Supporting design work belongs to its functional slice.

## Presentation and performance architecture — required target

This is the required design, not a claim that the current implementation or browser performance is qualified. Three systems execute concurrently: the physics worker, the animation worker and the main-thread renderer, as fixed by the [compilation model](docs/gpu-f32-physics.md#compilation-model) and delivered in the [ordered roadmap](docs/planning/invest/vertical-delivery.md#rolling-playable-roadmap). The [simulation–presentation bridge](docs/simulation-presentation-bridge.md) defines ownership and transport; the [performance checklist](docs/browser-physics-performance.md) defines the release-checklist budgets. The mechanism requirements below describe intended presentation. Each admitted part requires current qualification.

| Execution context | Exclusive responsibility | Clock |
| --- | --- | --- |
| Dedicated physics Web Worker: C# WebAssembly host + generic WASM SIMD128 f32 solver | Continuous physical state, spatial queries, and sensor predicates over the compiled initial state; typed f32 construction/read values; C# integer/discrete events and atomic transactions | Fixed 120 Hz tick, 480 Hz substeps |
| Separate C# animation Web Worker | Its own compiled animation model: cosmetic tracks, motor spin, recoil, indicator/UI easing and effect clocks, fed one way by committed physics; no retained UI/DOM objects | Independent clock, currently 60 Hz |
| Browser main thread with Universal WebGL 2 / WebGPU instanced renderer | Input, construction previews, scene/resource ownership; reads committed physics poses from the SharedArrayBuffer triple ring and latest animation samples, interpolates, and issues instanced draw batches | Independent display clock; 30–60 FPS (release checklist) |

The contexts must execute concurrently with rates ordered physics > animation ≥ renderer; rendering never influences either worker. Browser rendering remaining on the main thread is the selected design; a fourth render worker is not required. Separate methods, tasks or asynchronous APIs on one thread do not satisfy the worker requirement. Independent .NET worker runtimes exchange bounded typed messages and owned transferable buffers; zero-copy atomic pose sharing via `SharedArrayBuffer` provides direct presentation updates without thread stalls.

- Input and construction emit typed intent commands. Show previews and pending state until authoritative acknowledgement; validation errors use existing contextual feedback.
- Present one coherent generation/revision at a selected display time. Use bounded timestamp histories, explicit buffer acquisition/release and backpressure; do not assume two mutable buffers suffice for delayed independent consumers. Failed or intermediate simulation work never becomes a successful motion/event.
- Preserve reliable impacts, pulses, removals and results when replaceable pose samples coalesce. Interpolate from worker timestamps and explicit clock-origin mapping; no external engine interpolation fraction drives the worker timeline.
- Separate render geometry from contact and domain-interaction geometry. Shared dimensions/local frames keep them aligned; visibility, LOD and cosmetic motion never change physical behavior.
- Use dirty property updates, immutable asset reuse, appropriate instancing and rigid-detail merging. Qualify silhouettes, passages, sockets and touch targets at their actual projected size.
- Qualify transparency, shadows and pixel cost while preserving the approved palette and composition. Idle rendering may stop only when input, camera, UI, animation, resource, resize and visibility dependencies are settled.
- Require nonblocking Run/pause/step/success/Reset/Load/save/disposal barriers, generation rejection and truthful pending/error states. Cosmetic/UI animation continues with physics paused. Hidden-tab policy pauses/rebases at acknowledged boundaries rather than dropping unsolved physical time.
- Verify complete production workloads against the [release checklist](docs/browser-physics-performance.md) at its P0-034 gate and P0-035 engine closure, including sustained physical-device measurements, frame tails, freshness, input response, memory and transport. Screenshots, native timings and one small smooth scene cannot establish 30–60 FPS qualification.

## Independent animation ownership

Use the [general data-driven physics and animation engines](docs/engine-contracts.md#general-data-driven-engines). Each element authors typed tracks/curves, transitions, physical-observable/event bindings and visual assets for shared evaluators; never give it a bespoke animation evaluator, private update loop or physical solver. Distinct artwork and declarative motion remain encouraged within this visual contract.

Cosmetic motion does not need a physical body, joint, force or authoritative angular phase. The animation worker owns typed tracks and their lifecycle; the browser presenter alone applies their resulting scene properties. Motor spin may use active/direction/speed observations to communicate real behavior. Actual contact-bearing shafts, moving shields and other functional geometry follow committed physical poses, with decorative child motion composed at the same display time.

One owner writes each mutable property. Reset and generation changes cancel stale effects and restore the visual baseline; cosmetic phase never enters authoritative physical save/replay. Prove identical physical results with animation hidden or disabled, and coherent composition under delayed physical or cosmetic samples.

Forward-refactor current implementations, callers, content, tools and tests together. Remove superseded browser-thread simulation, part-local cosmetic schedulers, direct scene reads in laws, obsolete APIs and retired-schema readers. No backwards compatibility, shims, old-name aliases, automatic migrations or old/new runtime selection may remain. Keep historical evidence unchanged and labelled as historical; it is not supported current input. Physical direction, obstruction, contact and supply must remain truthful under this separation.

## Art direction — Monument Valley-inspired

The owner requests this visual direction for both the parts and the game environment. Use [Monument Valley's official visual reference](https://www.monumentvalleygame.com/mv1) as inspiration for an original, calm, sculptural puzzle diorama. The following are our application rules, not a claim to reproduce its assets or exact palette.

- Compose levels as spacious orthographic dioramas with simple architectural masses and clear silhouettes. Preserve free camera movement/orbit; do not force perspective tricks into a physics puzzle.
- **Preserve the current colour scheme.** Keep the exact source colours documented below: sky blue, warm wood and cream, navy icon ink, gold accents and established part colours. The owner's clarification overrides any suggestion of a new pastel palette or per-environment recolouring. Monument Valley informs form, composition, lighting and animation, not palette replacement.
- Make parts feel like small sculptural objects: rounded or chamfered primitive forms, matte ceramic/plaster-like housings, clean surfaces and minimal seams. Reduce exposed screws, wood detailing and industrial clutter as assets evolve; do not erase the features that explain how a mechanism works.
- Pipes use smooth arches, simple collars and readable transparent windows. Timers use a sculptural dial/countdown ring. Optics use simple stands, framed mirror/lens discs and quiet luminous beams. Motors, pulleys and springs retain visibly moving working elements inside this shared visual language.
- Use soft directional illumination, gentle grounding shadows and restrained depth shading. Avoid harsh specular metal, gritty textures, excessive bloom and outlines around every mesh. Keep transparent reference walls and unfilled three-plane placement projections distinct from real lighting.
- Animate with calm, fluid, purposeful motion: readable compression, rotation, release and settling. Decorative easing must not desynchronise contacts, timers, beam state or collision geometry. Finish activation feedback gracefully after success.
- Retain minimal icon-driven controls, generous spacing and contextual disclosure. No permanent inspectors or new floating toolbars. Signals must remain readable without relying on colour alone; retain shapes, icons and on-object state cues.
- Keep artwork, layouts and pictograms original. This is visual inspiration, not a request for Monument Valley characters, copied levels, branding or impossible-geometry gameplay.
- Apply the direction consistently to new families and incrementally reconcile existing shapes and motion while retaining the current palette. Validate build/run screenshots and motion at desktop and mobile sizes; lighting changes must preserve the approved colour appearance.

## Optical logic gates

Five operations share a cyan cube and navy foot. Cream-rimmed left/top control lenses carry one/two raised marks; a gold-rimmed front carrier enters separately and leaves the gold-rimmed right outlet. Controls absorb light and never become output energy. The outlet lights only when a carrier actually exits, not merely when Boolean truth permits it. Control lamps ease slate-to-gold; physics changes only on fixed-tick boundaries.

A small cream relief shows the four two-input truth rows with raised gold output dots. Toolbox pictograms distinguish the operations without adding permanent panels. Preserve the palette. Verify carrier/no-carrier output, downstream actuation, truth-row legibility and continuous animation at the actual projected size.

## Supplied electrical logic

The Both gate retains its cyan body, cream face, navy base and two numbered condition inputs. A separate lower-front gold socket accepts the energy supply; it is not a third condition. The output lamp indicates supplied output, so two lit input lamps with an unpowered supply leave the output slate. Verify that distinction through a real-UI positive/negative pair. OR/XOR/NOR/NAND share this separation and body silhouette. Each displays its original navy operation pictogram on the cream face, matching its toolbox icon; small raised gold truth-row marks supplement it. Invalid feedback stays in build mode and uses the existing bottom status line, not a new popup. Keep that explanation short enough to fit the viewport.

## Sound speaker

Use a cream speaker cabinet on a navy foot, a gold-rimmed navy diaphragm and cyan centre. Two raised bars identify the default mid tone. Its diaphragm gives a small eased vibration after a real pulse; thin, shadow-free translucent wavefronts expand forward and fade with distance. Avoid solid gold hoops that dominate the diorama. The visible pulse and sound meter must remain useful when audio is muted. Qualify wave contrast and continuous motion on desktop and mobile.

## Sound meter

The sound meter is a cyan cabinet with a cream dial, navy tick marks, a gold eased needle and a slate-to-gold output lamp. The needle shows received intensity even without supply; the lamp indicates supplied output. Keep the top-right activation socket distinct from the lower electrical sockets. A wall may block reception while the speaker's cosmetic ring remains visible beyond it: the meter, not the decorative ring, is the authoritative feedback. Acoustic wave clipping and material behavior follow their named source requirements and qualification stages.

## Impact bell

A small gold tapered bell has a rounded crown, cream rim, navy hanging stem and cyan clapper. Keep its silhouette simple and toy-like; do not add an electrical socket. Impacts excite a damped rocking motion without snapping the pose on repeat hits or moving the collision proxy. Three very thin, translucent, shadow-free great-circle wavefronts expand in all directions and fade; they supplement the sound meter, not replace it. The bell toolbox pictogram uses the existing navy stroke style. Preserve the current palette.

## Wind chimes

Use a small cream circular cap with a navy hanging stud, four slender gold tubes at different lengths, dark suspension strings, a cream clapper and a cyan sail. The sail and clapper visibly swing together; do not replace their motion with a flashing indicator. Thin fading three-plane wavefronts communicate sound without obscuring the diorama. The original navy line icon shows hanging tubes and a diamond-shaped sail. Preserve the approved palette and restrained geometry.

## Windmill

Use four cyan pitched vanes with gold tips and hub inside a slender cream guard. A cream mast/gearbox on a navy foot supports the rotor; a cream side pulley with a gold spoke clearly exposes its mechanical output. The original navy line icon shows vanes and a small tower. Preserve existing palette values and uncluttered toy geometry.

Rotor and pulley angles follow the actual signed shaft speed: spin-up, reversal through zero and coast-down are continuous simulation motions, not decorative loops. A blocked fan path leaves the rotor at rest. The fixed guard/hub are collision geometry; blades are not individually simulated striking surfaces. Verify connected, disconnected and blocked states, sustained fluidity and mobile readability.

## Bellows

Use a navy base, cyan accordion body with thin cream fold rims, a cream press plate with a gold impact pad, and a short gold side nozzle with navy opening. The original navy pictogram shows a folded pump and side spout. Keep geometry chunky and uncluttered; retain the existing palette.

The press plate and its collision proxy descend together while folds compress, then remain held by the load. Once clear, silent refill raises the same geometry; no decorative pulse loop continues after the air ends. Verify compression, downstream motion, sustained fluidity and mobile readability.

## Electrically controlled clutch

Use cream bearing blocks and pulleys on a navy foot, two gold sliding coupling plates and a cyan coil ring. Gold pulley spokes expose rotation on each side independently. A small slate/cyan/gold lamp supplements the visible plate gap; do not rely on colour alone to distinguish open, closing and engaged states. Its original navy toolbox pictogram shows two separated plates between shafts.

Plate travel follows the simulated closing fraction. Input and output spokes follow their respective signed shaft speeds: an open clutch can show a turning input and stationary output. Electricity closes the gap but cannot generate rotation without an upstream drive. Power loss immediately disconnects the speed/work route while plates separate smoothly; show only motion and work supported by the committed physical model; model coverage is determined by the named clutch requirements. Preserve the approved palette and add no permanent controls.

## Electric linear pusher

Cream barrel on a navy foot, slim gold rod, cream spherical head with cyan ring, gold travel marks and distinct lower command/supply versus upper end-switch sockets. The rod and head follow the actual collision extension; never animate through an obstruction or teleport cargo. Slate indicates held/unpowered, cyan free movement and gold loaded/contact-blocked or conflicting commands. Brake on power loss holds the current pose; no spring-return animation. Original pictogram and paired endpoint/input icons preserve the minimal contextual UI. Ideal self-locking electric servo, not a pneumatic piston or battery-energy model.

## Mechanical work feedback

Mechanical sources and belt branches have bounded torque/work allowances. Preserve the existing artwork and palette; do not add a torque inspector or floating status panel. Motor/windmill source speeds remain regulated by the mechanical network, and the electrical motor's existing lamp indicates active supply. The conveyor's finite-mass driven roller owns its physical motion: treads, pulleys and direction feedback follow its solved shaft motion. Power loss removes new work supply while physical stored inertia can coast; source rotor and connected mechanism artwork follow committed physical motion. Qualify actual finite work, motion and readability against the named source model; the historical target-speed implementation is not a restriction on required future shaft/belt capabilities.

## Latched wound-spring launcher

Use cream guide rails and a rounded physical plunger on a navy base, a transparent cyan guide, a continuous silver helix and a small gold latch. Preserve the existing palette and original navy toolbox pictogram; add no permanent panel. Five navy marks on the cream rail and a moving gold index show compression, not a linear energy percentage (spring energy is quadratic in compression).

The helix and index follow the physical plunger; never ease them independently through an obstruction. The cosmetic latch moves toward its released angle of −0.65 radians at a bounded 8 radians/second, then returns after rearming. Its render updates do not move collision geometry or alter stored energy. This bounded motion is not a claim of eased acceleration. Verify the marks, winding/compressed/extended helix, launched payload, intermediate latch angles and return in continuous motion. Preserve retained-charge, obstruction/resumption and loading-edge controls from [the source record](docs/planning/requirements.md#todo-085). Desktop, mobile and sustained frame qualification remain separate acceptance criteria.

## Passive trampoline

Use a cream rectangular frame, cyan membrane, restrained gold corner supports and a navy back plate. Keep the exposed gap deep enough to make compression readable. The original navy pictogram shows a ball above a sagging bed; do not reuse the springboard icon.

The cyan mesh follows the simulated contact indentation and returns as the load leaves. Its profile stays below the contacting sphere, including deep off-centre loads. The rigid frame/back do not perform a decorative bounce. This part returns impact energy through contact springs; both it and the [springboard](docs/springboard-elastic-contract.md) use physical compression/release rather than imposing a launch velocity. Preserve the palette and restrained toy geometry. Spread off-centre sag toward the available interior fabric using direction-aware support bounded by the fixed frame; avoid a tiny circular pocket that makes the ball appear to sink through a flat sheet. Verify compression/recovery across relevant camera angles, sustained motion and mobile readability. Do not imply a cloth-wave model unless the named physical requirements support it.

<a id="impact-lever--initial-implementation"></a>
## Impact lever

Use a cream beam with a cyan upper inset, four restrained gold lever-arm marks, a gold pivot and visible gold end stops above a navy foot. Keep the original navy outline seesaw pictogram and the existing move/rotate controls; no new inspector or toolbar. The child beam follows the simulated hinge angle, without a canned flip or decorative easing that changes its contact pose. Verify continuous tilt, payload release and the exported toolbox icon. Rope sockets, fixture-blocking/fulcrum collision coverage and mobile behavior retain their named source acceptance.

## Reloadable toy cannon

Use a fixed navy base, cream breech and open collars, transparent cyan annular jacket and a visible physical payload. The gold charge bar and small ready flag distinguish charging from loaded/ready; separate supply and trigger sockets use the existing contextual wiring UI. Keep the original navy cannon pictogram and current palette.

A slate (#556573) inner sleeve contrasts with the fixed cream collars and moves backward inside the fixed transparent jacket after an accepted shot. Quintic easing gives an 0.08-second compression and 0.45-second return; overlapping pulses combine with smooth saturation below 0.16 world units, without resetting the pose. The sleeve stays within the jacket's annular solid envelope, never narrows the bore, and applies no extra payload impulse. This is anchored recoil feedback, not a simulated freely recoiling barrel. Fixed chamber, breech and outer collision surfaces remain aligned with their artwork.

Rejected shots do not recoil. Presentation finishes after simulation stop and Reset clears it. Verify bounds, settling, unchanged authoritative physics, actual browser readability, continuous motion and mobile behavior.

## Character and hierarchy

A bright, friendly toy workbench: sky blue space, warm wood and cream surfaces, readable coloured mechanisms, restrained dark-blue line icons. The contraption is the main content. Keep the playful, simple geometry and ample empty space; avoid a CAD/editor aesthetic, permanent drafting grids, dense toolbars, opaque enclosing walls, photorealistic grime, or a dark industrial dashboard.

The normal screen has a small title at top left, level selector at top centre, goal and menu icons at top right, a compact parts drawer on the left, and Run/Undo at bottom centre. Short status text sits along the bottom. Secondary controls are disclosed when needed, not spread across floating windows. Labels beside inventory icons and text in the level selector remain useful; “icon-driven” does not mean removing all explanatory text.

## Colour system

These are source colours, before lighting, transparency and display conversion. Do not sample a lit screenshot to replace the material palette. Hex values with eight digits use RGBA.

| Role | Source colour |
| --- | --- |
| Sky/background | `#91cbed` |
| Workbench base / deck / corner studs | `#b77c42` / `#ead39b` / `#9c743f` |
| Primary button/icon ink | `#293954` |
| Default label ink | `#25344b` (the code calls this “Cream”; it is dark ink) |
| Secondary label ink | `#48556a` |
| Positive/build/hint text | `#326537` |
| Panel fill / border | `#fff8e9eb` / `#c5bca8` |
| Level selector fill | `#eee5cc` |
| Ordinary action normal / hover / pressed | transparent `#ffffff00` / `#fff4d8` / `#cbbd9b` |
| Primary Run action normal / hover / pressed | `#f7cb52` / `#ffe38a` / `#dbae38` |
| Shared style fallback border | `#867961` |
| Selection ring | `#efffbd` |
| Generic activation links | `#e8b764` |
| Gizmo X / Y / Z | coral `#de7058` / green `#62aa78` / blue `#5b9cdb` |
| Floor / back / side projection outlines | ochre `#a5823b` / blue `#527c98` / terracotta `#a96f5c` |

Part identity is defined in `parts/catalog/*.tres`. Preserve these palette relationships when adding variants. Hex below is the nearest 8-bit representation of the recorded source RGB values. These exact approved palette identities are an explicit rendering boundary; they do not authorize a wider floating game-value model or physical feedback.

| Part | RGB | Approximate hex |
| --- | --- | --- |
| Ball | 0.96, 0.49, 0.22 | `#f57d38` |
| Bowling ball | 0.27, 0.39, 0.61 | `#45639c` |
| Tennis ball | 0.72, 0.86, 0.35 | `#b8db59` |
| Balloon | 0.93, 0.39, 0.47 | `#ed6378` |
| Ramp | 0.76, 0.56, 0.32 | `#c28f52` |
| Basket | 0.29, 0.67, 0.58 | `#4aab94` |
| Spring | 0.96, 0.70, 0.33 | `#f5b354` |
| Fan | 0.40, 0.72, 0.79 | `#66b8c9` |
| Switch | 0.94, 0.43, 0.33 | `#f06e54` |
| Domino | 0.91, 0.83, 0.65 | `#e8d4a6` |
| Lamp | 0.98, 0.86, 0.51 | `#fadb82` |
| Conveyor | 0.84, 0.61, 0.28 | `#d69c47` |
| Pinball bumper | 0.87, 0.44, 0.35 | `#de7059` |
| Physical wall | 0.76, 0.56, 0.32 | `#c28f52` |
| Battery | 0.87, 0.44, 0.35 | `#de7059` |
| Electric motor | 0.40, 0.72, 0.79 | `#66b8c9` |
| Clear pipe | 0.40, 0.72, 0.79 (shell alpha 0.16) | `#66b8c9` |
| Delay box | 0.84, 0.61, 0.28 | `#d69c47` |
| Reverse transmission | 0.84, 0.61, 0.28 | `#d69c47` |
| Weight | 0.27, 0.39, 0.61 | `#45639c` |
| Flashlight | 0.96, 0.70, 0.33 | `#f5b354` |
| Solar cells | 0.27, 0.39, 0.61 | `#45639c` |
| Pulley / rope anchor | 0.84, 0.61, 0.28 | `#d69c47` |

Catalog colour is not necessarily every visible surface: the lamp currently uses its own off/on bulb colours, for example. Structural bases use dark slate, metal details use pale grey, and operational cues use warm gold or pale green. Retain readable silhouettes as well as colour identity.

## Geometry, materials and lighting

Parts are built from simple boxes, spheres, cylinders, rings and cylindrical lines beneath a `Visual` scene-graph node. Surfaces are clean and mostly untextured. Standard part materials use roughness **0.48** and metallic **0** by default, giving simple satin-like toy surfaces rather than polished chrome. Spheres use 24 radial segments/12 rings; cylinders 24 radial segments; torus rings use 32 rings/8 ring segments.

The bench is a solid finite tabletop, not a visual-only plane: deck dimensions are 16.8 × 0.06 × 9.8 world units, with top at Y=-0.46, above a wooden base. Balls can bounce on it. Do not replace it with a translucent construction grid.

Lighting is warm and directional: ambient `#fff5dd` at 0.65 energy; main light `#fff0d6` at 1.3 energy, rotation (-38°, -28°, 0°); cool fill `#b7ddea` at 0.45 energy, rotation (-10°, 145°, 0°). Tone mapping is linear. The main light currently casts real shadows. These grounding shadows are distinct from the non-shadow placement aids below; do not describe the existing renderer as shadow-free.

## Typography and interface surfaces

No custom font asset or font-family override is currently supplied; the UI uses the clean system sans-serif font stack (`system-ui, -apple-system, sans-serif`). Preserve its plain, readable sans-serif character rather than introducing decorative or technical drafting lettering.

At the 1440 × 900 reference layout:

- Title: 18 px, uppercase; section labels such as PARTS: 11 px.
- Level selector and panel heading: 16 px; inventory/descriptions: 14 px.
- Secondary help/timer: 13 px; bottom status: 12 px.
- Shared panel radius: 10 px; border: 1 px; content padding: 16 px horizontally and 12 px vertically; vertical separation: 10 px.
- Ordinary action buttons: minimum 40 × 40; top goal/menu buttons: 44 × 44; primary Run: minimum 56 × 48.
- Action buttons have no border in normal/hover/pressed overrides. Panel drop-shadow size is zero; the configured shadow colour/offset does not create a visible shadow at that size.

The canvas scales uniformly by min(viewport width/1440, viewport height/900), with edge/centre anchoring recalculated in layout coordinates. This is the current desktop adaptation, **not** a completed responsive mobile layout. Tiny-screen shrinkage is a limitation, not a rule to preserve.

## Icons

Use actual vector-derived textures, never Unicode symbols or an icon font as the visible control artwork. Toolbar SVGs live in `assets/icons/` and are resolved directly through the web presentation asset pipeline. Buttons carry action metadata, but their displayed text is empty.

The toolbar uses **Lucide**, with bundled ISC licensing and Feather-derived icons covered by MIT; retain [the complete icon licence](assets/icons/LICENSE.txt). These are the “equivalent” icon source, not Noun Project assets.

- Toolbar: 24 × 24 SVG viewBox, no fill, `#293954` stroke, 2-unit stroke, round caps and joins.
- Custom part/move/front/cube pictograms: the same 24-unit canvas and ink, 1.7-unit stroke, no fill, round caps/joins; rasterised from SVG at 2×.
- Button icon maximum width: 20 px, without icon expansion. Electrical connections with multiple source outputs show paired source/target pictograms in a 64 × 40 button (42 px combined icon width). The pair distinguishes both ends without adding visible button text or persistent tooltips.
- Run is a play triangle; running uses a stop square; solved/reset uses a counterclockwise arrow. Undo, trash, link, save, folder, lightbulb, sliders and camera/rotation actions retain their established pictograms.
- Each inventory row has a distinct part pictogram plus a neighbouring name and remaining count. The entire row is one click target, including the words and count; the labels do not intercept pointer events. The toolbox grows to fit its inventory, capped to the available screen height with scrolling only for overflow. Exhausted rows are disabled. Do not replace all parts with generic boxes or emoji.

The common button helper explicitly empties tooltips. A 1.2-second global delay and compact tooltip class exist, but ordinary controls currently do **not** show tooltip copy. Keep long explanations in the drawer or explicitly opened help, not invasive hover bubbles.

## Interaction and spatial clarity

The default camera is orthographic, looking at (0, 3.6, 0), distance 26, azimuth 0.6 rad, elevation 0.48 rad, size 13.8. Scroll zoom is bounded to 8–22. Right-drag orbits. Per the revised camera request, WASD uses FPS-style ground-plane movement: W/S forward/back relative to the current heading, A/D strafe; Q/E continuously turns left/right using the existing orbit model (1.2 rad/s). Movement preserves height, normalises diagonal speed, and stops on release or window focus loss. Q/E does not rotate parts; rings and Fine rotate retain that role. Camera controls also live in the closed menu. Retain the orthographic toy-workbench presentation rather than forcing first-person perspective or automatic camera jumps during manipulation.

Choosing a part creates a **50%-transparent preview** without consuming inventory or affecting physics. Placement is committed by clicking the workbench; default placement uses a horizontal plane at Y=3 and tenth-unit placement snapping. Selected parts can be adjusted directly; fixed fixtures cannot be moved or rotated. Cancel/Undo/Reset remain predictable escape routes.

The selected part has a pale ring. A single contextual control group in the parts drawer switches between move and rotate, with remove/connect shown only when applicable. No permanent inspector is needed. The goal/introduction starts collapsed; hints require a separate request. Goal and options panels close each other.

### Three-plane placement projections

All parts, including fixed fixtures and the current preview, receive three dashed, **unfilled** artwork-bounds projections in build mode. These are line geometry, not shadow maps, physical objects or filled silhouettes:

- Floor XZ plane: Y=-0.42.
- Back XY plane: Z=-4.65.
- Side YZ plane: X=-7.65.
- Dash spacing is approximately 0.22 world units, with 60% of each interval drawn.
- Selected/preview outlines darken by 20%.
- The reference “walls” have no geometry: they are completely transparent.
- Projections disappear during a run and are rebuilt as parts change or Reset restores the arrangement.

Do not reintroduce opaque walls, filled projected boxes, or a permanent grid. Actual light shadows can remain underneath these separate aids.

### Move/rotation widget

Physical puzzle walls are warm wooden panels with inset cream edge bands, distinct from the invisible reference walls. A third contextual resize icon appears only for a selected movable resizable part. Resize replaces arrowheads with square handles aligned to the wall's local axes; moving and rotation keep their established world-axis behaviour. Dragging stretches symmetrically about the unchanged centre, Shift snaps dimensions to 0.1, Escape cancels, and one gesture is one Undo action. Width/height/thickness are bounded to 0.4–8 / 0.4–6 / 0.12–2 world units. Geometry, solid box collision, selection ring and dashed projections follow the dimensions. No numeric inspector or permanent toolbar is added.

Keep a world-axis widget at the selected movable part, not three unrelated floating sliders. Rotation mode shows three coloured circular rings and spherical grab points; movement mode replaces them with three arrows. X/Y/Z colours stay consistent. The widget is unshaded, does not cast shadows, renders without depth testing at priority 10, and has a minimum screen radius of 65 px for small parts.

Sphere/arrow handles have an 18 px selection radius; rings a 9 px proximity allowance. Active handles enlarge to 1.4× and the active ring lightens by 35%. Shift constrains rotation to 15° or aligns the chosen movement coordinate to the 0.1-unit world grid. Escape cancels the whole gesture; one completed drag is one Undo action. Edge-on rings/axes have screen-space fallbacks. The gizmo disappears during runs and for fixed parts.

Advanced front view exists inside the menu: other depth layers fade to 75% transparency. Selecting Back/Middle/Front does not silently move a part; Move selected here is explicit. Front-view dragging edits sideways and vertically within the selected depth layer. Fine rotate offers Tip / Turn / Tilt in 5° steps for previews and placed parts. Page Up/Down changes depth, Escape cancels and Ctrl/Cmd+Z undoes. Camera buttons turn in 45° steps and Reset camera restores the initial angle. Direct workbench dragging preserves a part's height and starts without a centre jump; lifting the preview changes its placement height. Keep these precision tools secondary to direct manipulation.

Battery and motor reuse existing coral, teal, cream, navy and gold colours. The battery has a cream band, raised gold terminal and navy plus sign. The impact switch also has two navy supply terminals; its depressed/green button indicates a latched contact that passes electricity only when an upstream supply exists. The motor has a rounded cylindrical housing on a navy base, a cream shaft wheel with a gold index bar and an on-body supply indicator. Electrical cables are navy and thicker than gold activation links; endpoints follow named local sockets under the part transform. Contextual linking highlights compatible targets and never guesses between multiple compatible socket pairs.

## Motion and state feedback

Motion explains what the contraption does. Preserve fluid mechanical readability and couple state changes to the relevant object rather than adding distracting screen-wide effects.

Required mechanism presentation:

Physical algorithms and model coverage come from the named [source requirements](docs/planning/requirements.md) under [canonical IEEE-754 f32 and WASM SIMD authority](docs/gpu-f32-physics.md).

| Element | Required behaviour |
| --- | --- |
| Electric motor | Supply/state observations communicate acceleration, active supply and coast-down; Reset restores the zero-angle, stopped visual baseline. Functional shafts and connected mechanisms follow the committed physical motion and finite-work model; cosmetic spin cannot act as a second solver. |
| Fan | Active rotor turns around local X at 18 rad/s during simulation. |
| Conveyor | Mechanical input drives ten wrapping treads, visible pulleys and a direction-following gold arrow. Output relays the signed shaft speed 1:1. Unconnected belts remain stopped. The authored `surface_per_radian` models gearing from shaft rotation to surface travel. |
| Domino | Show the committed toppling pose and corresponding activation truthfully. The historical scripted local-Z tilt is not an alternative physical authority. |
| Switch | Contact moves the button from Y=0.06 to -0.02 and changes it to pale green `#bff5b0`; this is an immediate state change, not an eased tween. |
| Lamp | Active bulb changes from slate `#556573` to `#fff0a5`, with emission `#e9b24c`; no fade tween. |
| Basket | Pale green rim `#bdf4bd` indicates capture area, scales with authored capture margin, and turns white when active. |
| Reverse transmission | Cream housing, navy base and two ochre wheels with navy/gold index marks. The input/output wheels turn smoothly in opposite directions with a -1 ratio. Positive shaft speed is clockwise viewed from the marked local +Z face; rotating the whole part does not change transmission sign. |
| Mechanical belts | Taut, closed navy twin strands and moving gold witness marks distinguish them from sagging electrical/activation cables. Endpoints follow transformed sockets, including eased placement correction. Reset stops and restores all phases. |
| Activation links | Two gold cylindrical segments with a lowered midpoint (0.5 units sag); no flowing current or chain animation. |
| Delay box | Round ochre housing, cream clock face, twelve navy marks and a navy hand. The hand makes one clockwise revolution during the authored delay; a small indicator changes from slate to ochre to gold. Input/output sockets are on opposite sides. No extra toolbar or always-visible inspector. Reset restores the waiting state; it is a command module, not an electrical source. |
| Light and solar | Gold-bodied torch with cream lens collar, pressing top button and lit lens. Blue nine-cell panel in a cream frame, navy foot and four gold power-meter marks. Torch light is a translucent warm-cream cone: four nested shells soften its edge, widen to the physical 15° half-angle, fade at the finite range and clip sampled rays against the shared collision proxies. No axial rod remains. This is a render-time approximation, not volumetric scattering; fine silhouettes can reveal sampling steps. |
| Rope systems | Blue cylindrical loads with cream bands and gold eyes; heavier loads are visibly larger. Cream-rimmed ochre pulley wheels turn with rope travel. Single warm-wood ropes have gold endpoint knots and no pulley knots. Tangent legs meet curved arcs outside the cream wheel rims; threading favours the local upper side and retains that winding during motion. Free spans curve when slack and stay straight when taut; unfinished threading is dashed and carries no tension. Anchor eyes are gold on cream mounts. Weight motion follows committed physical poses; free-span slack is a length-matched visual approximation, not collision geometry. Finite-radius rim arcs and constraint length must be reconciled under the rope's named model; artwork cannot claim supported pulley behavior that has not been implemented. |
| Set/Reset latch | Cyan housing, navy foot and cream face. A navy rocker eases between two angles; a slate/gold lamp mirrors the remembered off/on state. Raised bar and hollow ring identify the two top control sockets without relying on colour. Matching icon-only choices appear in the existing part drawer when wiring an ambiguous target; no persistent toolbar. Left/right gold sockets carry separately supplied electricity. Reset wins commands delivered in the same simulation tick, settled on the following boundary; rocker easing is presentation-only. |
| Repeating clock | Tall ochre housing and navy foot with a cream face and slate pendulum recess. A cream rod and gold bob swing once per powered interval; a small slate/gold lamp fades after each trigger. The pendulum eases to rest when power is lost. Left electrical input and right trigger output are separate gold sockets. No immediate startup pulse; power restoration starts a whole interval. On-object motion replaces an extra control panel. |
| Both gate | Cyan rectangular housing, navy foot and cream face. Two slate/gold input lamps converge along navy paths to one output lamp; all indicators ease visually without delaying circuit state. One raised mark labels the upper input and two marks the lower; matching contextual one/two-mark icons select sockets in the existing drawer. The right output supplies electricity only while both left inputs receive real supply; no memory or implicit source. |
| Laser emitter | Ochre toy housing, navy foot, cream lens collar and slate/cream illuminated lens. Top gold trigger-enable socket is separate from rear electrical input. A thin unshaded warm-cream beam stops at the nearest opaque surface or receiver; it casts no shadow. This is an intentionally visible narrow-ray convention, distinct from the flashlight’s widening cone. No extra toolbar. |
| Beam splitter | Transparent cyan pane in a chunky cream frame with navy foot, ochre circular coating boundary and paired gold studs. The pane blocks balls but transmits light; its frame remains opaque. The same rotation gizmo aims it, and selected-part previews reveal both outgoing paths without activating receivers. Running branches each carry half the incoming power and appear fainter than the parent. No added permanent controls. |
| Flat mirror | Cream square backing, navy foot, cyan circular front face, ochre rim and two quiet cream glints. Only the finite front disc reflects; the back/frame absorb. The existing three-axis gizmo controls orientation. Selecting a placed mirror in build mode reveals only its faint outgoing aim guide, without powering receivers or adding a toolbar. Running beams render each reflected segment with reduced opacity as power is lost; reflection geometry is not eased away from the actual normal. |
| Laser receiver | Cream target plate on a navy foot; a front-facing slate/gold disc with a cream concentric ring and gold centre marks the sensitive area. The disc colour eases while the electrical contact responds at the fixed tick. Author-set sensitivity defaults to 0.25, accepting one half-power amber beam but not one quarter-power branch. Two lower gold sockets carry separate supply/input and switched output. The opaque rear and frame do not detect a laser. |
| Counter | Ochre rectangular housing on a navy foot, cream face and one visible dot per target event (1–9 in rows of three). Dots ease from slate to gold as events arrive, so lit/total dots show current/target without an inspector. This short presentation fade finishes even after success. Top/bottom gold sockets carry trigger input/output; left/right sockets carry the separately supplied electrical contact. At the target the count saturates, emits once and holds the contact until workshop Reset. Existing contextual trigger/power choice applies to ambiguous links. |
| Pressure plate | Broad cream top on a thin navy base, a small ochre centre ring and four corner studs. Studs are slate when empty, ochre for an insufficient direct-contact load, and ease to gold when pressed. The top stays physically and visually fixed; there is no decorative depression that disagrees with collision. Gold sockets on opposite edges carry electrical supply through the occupied contact. Existing move/rotate tools apply; no new permanent controls or threshold inspector. |
| Funnel | Open cream annular rims frame a transparent cyan taper with two thin navy rails. The inlet is twice the standard tube diameter; its narrow outlet matches the common 1.3-unit bore. Glass casts no visual shadow and transmits gameplay light; the cream collars are opaque. Continuous sloping contact surfaces guide visible balls under gravity, without a suction effect or extra animation. Standard move/rotate and compatible mouth snapping apply; no resize controls or new toolbar. |
| Ball detector | Thick cream open collar with a small cyan sensor housing, navy direction chevron and gold trigger socket. A slate indicator pulses gold and fades smoothly after a forward ball crossing. No beam crosses or blocks the aperture; contact uses the actual hollow collar. Uses the common tube bore and mouth snapping, move/rotate tools and original ring-and-arrow pictogram. It emits a command, not electricity or a continuous occupied signal. Reset clears its crossing memory and indicator. |
| Hold timer | Cyan rectangular housing, navy foot and inset cream face. A gold bar drains leftward across a navy track while the contact is held; a gold indicator returns to slate on expiry. Three gold sockets separate the top trigger from left/right electrical input/output. Default two seconds, author-configurable; no duration inspector is added. It closes a powered circuit rather than supplying energy. Ambiguous source/target wiring reveals only the relevant trigger/power icon choices in the existing part drawer, then hides them after connection or cancellation. |
| Powered gate | Cream open pipe collars and translucent cyan sleeve frame a gold shutter, cream rails and chunky navy actuator. Electrical supply retracts the blade along local +Y with bounded acceleration; the moving collision box follows its visible pose. Supply loss closes it, stopping at an obstructing ball. Prove this obstruction-stop behavior through the declared finite-work/contact model; feedback and the moving blade must follow the committed result. Indicator is slate when unpowered, gold when powered and ochre when blocked. Uses the standard move/rotate gizmo, compatible tube-mouth snapping and a single original icon; no extra toolbar. Reset closes the shutter. |
| Pipe bends | Fixed 45° and 90° hollow curves use the straight pipe's translucent cyan shell, cream open collars and thin navy rails. Original angle-specific icons distinguish them. The 1.3-unit bore and 2.4-unit centreline radius stay fixed; move/rotate and mouth snapping apply, with no resize controls. Contact redirects the visible ball under gravity, without scripted travel or added energy. |
| Clear pipe | Straight transparent cyan shell, open cream annular collars and two thin navy rails. The orange ball remains visible as gravity carries it through; no transport teleport, velocity boost or control overlay. Shell casts no visual shadow and transmits gameplay light; collars use the same hollow geometry for contact and optical occlusion. Standard move/rotate tools and dashed projections apply. Length resizes from 1–8 units through one local-axis square handle; the 1.3-unit bore and collar proportions remain fixed. The centre stays put, Escape cancels and one drag is one Undo. No extra handles or numeric inspector. Nearby free mouths align within 0.45 units and 20°; the translucent placement preview shows the proposed pose, while a move snaps on release. Collar faces meet without a new toolbar, cable or transport effect. Pull apart to disconnect; one gesture remains one Undo. |
| Spring | Gold finite-mass moving top plate, fixed navy base and a continuous silver three-turn helix. The plate and collider share the same physical pose under the [elastic springboard contract](docs/springboard-elastic-contract.md); the coil follows compression along the local axis. Contacts and overlapping loads act on the physical plate without resetting its pose or adding a decorative recoil. Presentation continues after success; Reset restores authored rest/precompression exactly. |
| Pinball bumper | Coral spherical head, cream rings, gold cap and slate pedestal. Impact rings expand up to 24% and warm toward gold over a 0.32 s smooth pulse. Overlapping hits combine without resetting the current ring pose. Render-time feedback finishes after simulation success; the fixed spherical collider never scales with it. |

Difficulty assistance is **not a player-exposed “nudge” tool**. At Run, eligible placement correction uses quintic easing `6t⁵−15t⁴+10t³`, position interpolation and quaternion Slerp, moving artwork and collision transform together. Current authored assisted fan corrections take 0.15 s; other authored corrections commonly take 0.4 s, with a 0.1 s runtime minimum. Values belong to level/part definitions, not a global visual snap. Precise placement correction is zero. Preserve bounded continuous motion; do not teleport parts or expose solution-alignment controls.

Simulation is fixed at 120 Hz with 480 Hz substeps; its cadence does not establish rendered frame performance. Success stops authoritative simulation at an acknowledged boundary while independent cosmetic feedback finishes gracefully. Run changes to Build Again and a short positive bottom message appears. Reset restores the pre-run arrangement and cancels stale effects. Keep menu, selection and button responses immediate; any easing belongs to the animation owner.

## Known limitations, not approved permanent constraints

The visual language is approved; unfinished behaviour is not frozen by this document.

- Mobile/touch-first gestures, safe-area layouts and real-device testing remain TODO.
- Keyboard-only manipulation, screen-reader semantics, contrast and colour-vision accessibility are not comprehensively verified. Internal action metadata alone is not an accessibility guarantee. Preserve labels and affordances while improving this.
- Dense scenes can obscure deeper parts; scrolled palette rows can clip. Improve these without adding opaque walls or persistent control clutter.
- All cosmetic feedback must follow the independent animation lifecycle and finish or cancel coherently at success, Reset and generation changes.
- Apply the approved palette, simple geometry and icon language to batteries, switched wires, motors, belt-driven conveyors, reverse transmissions, weights, pulleys, anchors, ropes, flashlights and solar panels. Chain mechanisms, rope cutting and moving-block pulleys retain their separate source obligations; distinguish connection types through shape as well as colour.
- Reduced-motion, sound and animation accessibility require their source-scoped decisions and proof; visual descriptions are not accessibility qualification.

See [TODO.md](TODO.md) for the live backlog. Do not document a planned feature as already present.

## Preservation check for future changes

Before accepting a visual change:

- Compare at 1440 × 900 in both build and run states, including a selected part, preview, all three gizmo axes, an open menu and a dense puzzle.
- Keep the existing sky/wood/cream/navy/gold hierarchy and established part colours unchanged. Apply the Monument Valley-inspired direction through sculptural forms, composition and motion, with a readable uncluttered puzzle.
- Check real exported-browser icons, not only editor/import previews.
- Verify fixture and placed-part projections, transparent walls, and solid-floor behaviour.
- Ensure move/rotate controls remain contextual, goal/hint panels collapsed by default, and hover does not summon long floating text.
- Exercise selection, drag, Shift, Escape, Undo, Run and Reset; visual changes must not silently change placement or physics.
- Review active mechanisms and assisted motion in motion, not only screenshots.
- Record intentional exceptions here; do not overwrite the approved style incidentally while implementing a new feature.

<a id="beam-shutter-addition--27-september-2026"></a>
## Beam shutter

The standalone optical shutter uses a navy header/foot, two cream rails and a thin gold blade with cream witness stripes on both faces. A gold electrical socket and slate/gold indicator show supply; ochre indicates obstructed closing. It retains the established palette and an original outline icon. Unlike the tube gate it has no tube mouth or snap connection.

The blade accelerates along local Y and retracts into the header. Its rendered pose and physical/optical OBB are identical; partial clearance really admits the beam. Supply loss closes it, stopping before a visible ball rather than crushing it. Prove this non-crushing obstruction stop through the declared finite-work/contact model; the visible blade cannot pass through an obstruction or claim a stopped physical pose that has not committed. Tube and optical shutters share generic WASM SIMD physical capabilities. Verify open/closed behavior, continuous animation and performance.

<a id="colour-optics-addition--27-september-2026"></a>
## Colour optics

Channel filter frames are cream with navy feet and translucent panes. Red/green/blue reuse the established gizmo accents (#de7058/#62aa78/#5b9cdb); no global palette changes. One/two/three raised bars on the frame, matching receiver faces and original icons distinguish channels without relying only on hue. Receivers retain the slate-to-gold eased activation disc, so “correct channel” and “currently active” remain separate cues.

Traced single-channel beams use those accents; broadband keeps warm cream and two-channel artwork uses the established combination accents documented below. This is a palette-preserving signal convention, not spectral colourimetry. Opacity follows maximum RGB channel power instead of red alone, keeping blue/green beams visible without granting simulated energy. Filter previews remain faint, selected-only and non-activating. Verify all three colours, including lower-power blue against the sky, on desktop and mobile.

<a id="combiner-and-mixed-channel-receivers--27-september-2026"></a>
## Combiner and mixed-channel receivers

The combiner is a chunky cyan cube on a navy foot. Three cream-rimmed input discs carry one/two/three cream bars; these identify ports, not mandatory input colours. Its smaller gold-rimmed outlet distinguishes the outgoing direction. The output lens eases toward the actual exiting light colour, or slate when dark; light exhausted inside the body cannot falsely light the outlet.

Yellow/cyan/magenta/white-channel receivers retain the cream plate, navy foot and eased gold activation disc. Required RGB channels appear as grouped coloured bar rows using existing accents. Each required channel must meet threshold; the names denote game-channel combinations rather than precise colourimetry.

Runtime beams share one world renderer. Co-directed paths with the same physical origin sum over their actual overlapping intervals; a shorter colour contribution ends where its range ends. Red+green uses existing gold (#f7cb52), green+blue cyan (#66b8c9), red+blue balloon pink (#ed6378), and three-channel light retains warm cream. Crossing or differently directed paths remain separate. No global palette replacement. Selected combiner previews remain faint and non-activating; verify preview clarity, mobile readability and continuous motion.

## Source of truth

The contracts above define intended behavior; implementation references are navigation aids and may be incomplete during migration: [Workshop UI/stage](ui/Workshop.cs), [guidance and layout behaviours](ui/WorkshopGuidance.cs), [icons](ui/WorkshopIcons.cs), [gizmo](ui/RotationGizmo.cs), [projections](ui/PlacementShadows.cs), [part geometry/materials](engine/PartArt.cs), [selection](engine/MachinePart.cs), [workbench](engine/Workbench.cs), [assistance easing](engine/PartAssistance.cs), [part implementations](parts), [catalog colours](parts/catalog), and [project settings](project.godot).

[TODO](TODO.md) owns implementation status; every affected mechanism must meet the current visual, physical, accessibility and animation criteria here.
