# Visual design contract

This document records the implemented **Curious Contraptions** visual language as inspected on 2026-09-27, with the owner's subsequent **Monument Valley-inspired art direction** below. The implementation measurements remain a baseline, not a claim that the new direction is already implemented. Where a preservation rule conflicts with that explicit direction, the direction takes precedence; interaction and physics requirements remain intact.

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

Part identity is defined in `parts/catalog/*.tres`. Preserve these relationships when adding variants. Hex below is the nearest 8-bit representation; the RGB floats are authoritative.

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

No custom font asset or font-family override is currently supplied; the UI uses Godot's default/fallback font. Preserve its plain, readable sans-serif character rather than introducing decorative or technical drafting lettering.

At the 1440 × 900 reference layout:

- Title: 18 px, uppercase; section labels such as PARTS: 11 px.
- Level selector and panel heading: 16 px; inventory/descriptions: 14 px.
- Secondary help/timer: 13 px; bottom status: 12 px.
- Shared panel radius: 10 px; border: 1 px; content padding: 16 px horizontally and 12 px vertically; vertical separation: 10 px.
- Ordinary action buttons: minimum 40 × 40; top goal/menu buttons: 44 × 44; primary Run: minimum 56 × 48.
- Action buttons have no border in normal/hover/pressed overrides. Panel drop-shadow size is zero; the configured shadow colour/offset does not create a visible shadow at that size.

The canvas scales uniformly by min(viewport width/1440, viewport height/900), with edge/centre anchoring recalculated in layout coordinates. This is the current desktop adaptation, **not** a completed responsive mobile layout. Tiny-screen shrinkage is a limitation, not a rule to preserve.

## Icons

Use actual vector-derived textures, never Unicode symbols or an icon font as the visible control artwork. Toolbar SVGs live in `assets/icons/` and are loaded through Godot resource imports with `GD.Load<Texture2D>`, so exported packs resolve them correctly. Buttons carry action metadata, but their displayed text is empty.

The toolbar uses **Lucide**, with bundled ISC licensing and Feather-derived icons covered by MIT; retain [the complete icon licence](assets/icons/LICENSE.txt). These are the “equivalent” icon source, not Noun Project assets.

- Toolbar: 24 × 24 SVG viewBox, no fill, `#293954` stroke, 2-unit stroke, round caps and joins.
- Custom part/move/front/cube pictograms: the same 24-unit canvas and ink, 1.7-unit stroke, no fill, round caps/joins; rasterised from SVG at 2×.
- Button icon maximum width: 20 px, without icon expansion.
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

Advanced front view exists inside the menu: other depth layers fade to 75% transparency. Selecting a layer does not silently move a part; moving to it is explicit. Keep these precision tools secondary to direct manipulation.

Battery and motor reuse existing coral, teal, cream, navy and gold colours. The battery has a cream band, raised gold terminal and navy plus sign. The impact switch also has two navy supply terminals; its depressed/green button indicates a latched contact that passes electricity only when an upstream supply exists. The motor has a rounded cylindrical housing on a navy base, a cream shaft wheel with a gold index bar and an on-body supply indicator. Electrical cables are navy and thicker than gold activation links; endpoints follow named local sockets under the part transform. Contextual linking highlights compatible targets and never guesses between multiple compatible socket pairs.

## Motion and state feedback

Motion explains what the contraption does. Preserve fluid mechanical readability and couple state changes to the relevant object rather than adding distracting screen-wide effects.

Current implementation:

| Element | Existing behaviour |
| --- | --- |
| Electric motor | Electrical supply accelerates the shaft toward 6 rad/s by default; loss of supply clears its active state and decelerates it to rest at 18 rad/s². Reset rebuilds the zero-angle, stopped pose. Its signed shaft speed drives connected mechanisms; torque/load sharing is not simulated. |
| Fan | Active rotor turns around local X at 18 rad/s during simulation. |
| Conveyor | Mechanical input drives ten wrapping treads, visible pulleys and a direction-following gold arrow. Output relays the signed shaft speed 1:1. Unconnected belts remain stopped. The authored `surface_per_radian` models gearing from shaft rotation to surface travel. |
| Domino | Active visual topples around local Z at 4 rad/s to -1.4 rad; this is a scripted visual/activation rule, not a general rigid-body lever. |
| Switch | Contact moves the button from Y=0.06 to -0.02 and changes it to pale green `#bff5b0`; this is an immediate state change, not an eased tween. |
| Lamp | Active bulb changes from slate `#556573` to `#fff0a5`, with emission `#e9b24c`; no fade tween. |
| Basket | Pale green rim `#bdf4bd` indicates capture area, scales with authored capture margin, and turns white when active. |
| Reverse transmission | Cream housing, navy base and two ochre wheels with navy/gold index marks. The input/output wheels turn smoothly in opposite directions with a -1 ratio. Positive shaft speed is clockwise viewed from the marked local +Z face; rotating the whole part does not change transmission sign. |
| Mechanical belts | Taut, closed navy twin strands and moving gold witness marks distinguish them from sagging electrical/activation cables. Endpoints follow transformed sockets, including eased placement correction. Reset stops and restores all phases. |
| Activation links | Two gold cylindrical segments with a lowered midpoint (0.5 units sag); no flowing current or chain animation. |
| Delay box | Round ochre housing, cream clock face, twelve navy marks and a navy hand. The hand makes one clockwise revolution during the authored delay; a small indicator changes from slate to ochre to gold. Input/output sockets are on opposite sides. No extra toolbar or always-visible inspector. Reset restores the waiting state; it is a command module, not an electrical source. |
| Light and solar | Gold-bodied torch with cream lens collar, pressing top button and lit lens. Blue nine-cell panel in a cream frame, navy foot and four gold power-meter marks. Torch light is a translucent warm-cream cone: four nested shells soften its edge, widen to the physical 15° half-angle, fade at the finite range and clip sampled rays against the shared collision proxies. No axial rod remains. This is a render-time approximation, not volumetric scattering; fine silhouettes can reveal sampling steps. |
| Rope systems | Blue cylindrical loads with cream bands and gold eyes; heavier loads are visibly larger. Cream-rimmed ochre pulley wheels turn with rope travel. Single warm-wood ropes have gold endpoint knots and no pulley knots. Tangent legs meet curved arcs outside the cream wheel rims; threading favours the local upper side and retains that winding during motion. Free spans curve when slack and stay straight when taut; unfinished threading is dashed and carries no tension. Anchor eyes are gold on cream mounts. Weight motion follows the sphere-envelope physics; free-span slack is a length-matched visual approximation, not collision geometry. Rim arcs remain presentation over the fixed-point solver; their finite-radius length is not yet reconciled with the constraint. |
| Hold timer | Cyan rectangular housing, navy foot and inset cream face. A gold bar drains leftward across a navy track while the contact is held; a gold indicator returns to slate on expiry. Three gold sockets separate the top trigger from left/right electrical input/output. Default two seconds, author-configurable; no duration inspector is added. It closes a powered circuit rather than supplying energy. Ambiguous source/target wiring reveals only the relevant trigger/power icon choices in the existing part drawer, then hides them after connection or cancellation. |
| Powered gate | Cream open pipe collars and translucent cyan sleeve frame a gold shutter, cream rails and chunky navy actuator. Electrical supply retracts the blade along local +Y with bounded acceleration; the moving collision box follows its visible pose. Supply loss closes it, stopping at an obstructing ball. Indicator is slate when unpowered, gold when powered and ochre when blocked. Uses the standard move/rotate gizmo, compatible tube-mouth snapping and a single original icon; no extra toolbar. Reset closes the shutter. |
| Pipe bends | Fixed 45° and 90° hollow curves use the straight pipe's translucent cyan shell, cream open collars and thin navy rails. Original angle-specific icons distinguish them. The 1.3-unit bore and 2.4-unit centreline radius stay fixed; move/rotate and mouth snapping apply, with no resize controls. Contact redirects the visible ball under gravity, without scripted travel or added energy. |
| Clear pipe | Straight transparent cyan shell, open cream annular collars and two thin navy rails. The orange ball remains visible as gravity carries it through; no transport teleport, velocity boost or control overlay. Shell casts no visual shadow and transmits gameplay light; collars use the same hollow geometry for contact and optical occlusion. Standard move/rotate tools and dashed projections apply. Length resizes from 1–8 units through one local-axis square handle; the 1.3-unit bore and collar proportions remain fixed. The centre stays put, Escape cancels and one drag is one Undo. No extra handles or numeric inspector. Nearby free mouths align within 0.45 units and 20°; the translucent placement preview shows the proposed pose, while a move snaps on release. Collar faces meet without a new toolbar, cable or transport effect. Pull apart to disconnect; one gesture remains one Undo. |
| Spring | Gold moving top plate, fixed navy base and a continuous silver three-turn helix. Accepted contact starts a bounded compression/rebound lasting 0.84 seconds, with decreasing oscillation and smooth settling. Overlapping hits add without resetting the pose; the coil scales along the part's local axis. Presentation continues after success and Reset restores rest. Collision/launch remain unchanged; this is impact feedback, not deformable-spring physics. |
| Pinball bumper | Coral spherical head, cream rings, gold cap and slate pedestal. Impact rings expand up to 24% and warm toward gold over a 0.32 s smooth pulse. Overlapping hits combine without resetting the current ring pose. Render-time feedback finishes after simulation success; the fixed spherical collider never scales with it. |

Difficulty assistance is **not a player-exposed “nudge” tool**. At Run, eligible placement correction uses quintic easing `6t⁵−15t⁴+10t³`, position interpolation and quaternion Slerp, moving artwork and collision transform together. Current authored assisted fan corrections take 0.15 s; other authored corrections commonly take 0.4 s, with a 0.1 s runtime minimum. Values belong to level/part definitions, not a global visual snap. Precise placement correction is zero. Preserve bounded continuous motion; do not teleport parts or expose solution-alignment controls.

Simulation is fixed at 120 Hz with four substeps. This does not establish a guaranteed rendered frame rate or render interpolation. Current success stops the simulation and can freeze mechanisms mid-animation; there is no celebration overlay or animated transition. Run changes to Build Again and a short positive bottom message appears. Reset restores the pre-run arrangement. Menu opening, selection and button states are immediate rather than animated.

## Known limitations, not approved permanent constraints

The visual language is approved; unfinished behaviour is not frozen by this document.

- Mobile/touch-first gestures, safe-area layouts and real-device testing remain TODO.
- Keyboard-only manipulation, screen-reader semantics, contrast and colour-vision accessibility are not comprehensively verified. Internal action metadata alone is not an accessibility guarantee. Preserve labels and affordances while improving this.
- Dense scenes can obscure deeper parts; scrolled palette rows can clip. Improve these without adding opaque walls or persistent control clutter.
- Spring recoil and bumper pulses finish after success; other mechanisms can still freeze mid-animation and need a consistent completion policy.
- Batteries, switched wires, motors, belt-driven conveyors and reverse transmissions have initial visual systems. Weights, fixed point-guide pulleys, anchors and tension-only ropes now have initial visual systems. Flashlights and solar panels now have initial visual systems; the cone now uses softly layered, ray-clipped artwork. Chain-specific mechanisms, rope cutting and moving-block pulleys remain planned. New systems should use this palette, simple geometry and icon language, while giving different connection types clearly different shapes as well as colours.
- There is no claimed reduced-motion mode, sound design or animation accessibility system in this snapshot.

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

## Source of truth

Implementation references: [Workshop UI/stage](ui/Workshop.cs), [guidance and layout behaviours](ui/WorkshopGuidance.cs), [icons](ui/WorkshopIcons.cs), [gizmo](ui/RotationGizmo.cs), [projections](ui/PlacementShadows.cs), [part geometry/materials](engine/PartArt.cs), [selection](engine/MachinePart.cs), [workbench](engine/Workbench.cs), [assistance easing](engine/PartAssistance.cs), [part implementations](parts), [catalog colours](parts/catalog), and [project settings](project.godot).

Existing browser evidence was visually checked at `.playwright-mcp/L05-precise-near-positive-direct-reset-outcome.png`; it shows the bright toy-workbench composition and sparse solved-state UI. That local screenshot is a reference, not a complete accessibility or animation audit. Additional browser observations are recorded in [docs/browser-playtest.md](docs/browser-playtest.md).
