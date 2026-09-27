# Visual design contract

This document records the implemented **Curious Contraptions** visual language as inspected on 2026-09-27. The current style is approved by the project owner and should be retained. It is a preservation guide, not a proposal to redesign the game or a claim that every interaction is finished.

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
- Each inventory row has a distinct part pictogram plus a neighbouring name and remaining count. Do not replace all parts with generic boxes or emoji.

The common button helper explicitly empties tooltips. A 1.2-second global delay and compact tooltip class exist, but ordinary controls currently do **not** show tooltip copy. Keep long explanations in the drawer or explicitly opened help, not invasive hover bubbles.

## Interaction and spatial clarity

The default camera is orthographic, looking at (0, 3.6, 0), distance 26, azimuth 0.6 rad, elevation 0.48 rad, size 13.8. Scroll zoom is bounded to 8–22. Right-drag orbits; WASD pans in camera-relative screen directions. Camera controls also live in the closed menu. Avoid adding forced perspective or automatic camera jumps during manipulation.

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

Keep a world-axis widget at the selected movable part, not three unrelated floating sliders. Rotation mode shows three coloured circular rings and spherical grab points; movement mode replaces them with three arrows. X/Y/Z colours stay consistent. The widget is unshaded, does not cast shadows, renders without depth testing at priority 10, and has a minimum screen radius of 65 px for small parts.

Sphere/arrow handles have an 18 px selection radius; rings a 9 px proximity allowance. Active handles enlarge to 1.4× and the active ring lightens by 35%. Shift constrains rotation to 15° or aligns the chosen movement coordinate to the 0.1-unit world grid. Escape cancels the whole gesture; one completed drag is one Undo action. Edge-on rings/axes have screen-space fallbacks. The gizmo disappears during runs and for fixed parts.

Advanced front view exists inside the menu: other depth layers fade to 75% transparency. Selecting a layer does not silently move a part; moving to it is explicit. Keep these precision tools secondary to direct manipulation.

## Motion and state feedback

Motion explains what the contraption does. Preserve fluid mechanical readability and couple state changes to the relevant object rather than adding distracting screen-wide effects.

Current implementation:

| Element | Existing behaviour |
| --- | --- |
| Fan | Active rotor turns around local X at 18 rad/s during simulation. |
| Conveyor | Ten top treads advance in a wrapping phase at authored belt speed; gold arrow shows direction. |
| Domino | Active visual topples around local Z at 4 rad/s to -1.4 rad; this is a scripted visual/activation rule, not a general rigid-body lever. |
| Switch | Contact moves the button from Y=0.06 to -0.02 and changes it to pale green `#bff5b0`; this is an immediate state change, not an eased tween. |
| Lamp | Active bulb changes from slate `#556573` to `#fff0a5`, with emission `#e9b24c`; no fade tween. |
| Basket | Pale green rim `#bdf4bd` indicates capture area, scales with authored capture margin, and turns white when active. |
| Activation links | Two gold cylindrical segments with a lowered midpoint (0.5 units sag); no flowing current or chain animation. |
| Spring | Static plates and three metal rings; launch impulse exists, compression/rebound animation does not yet. |

Difficulty assistance is **not a player-exposed “nudge” tool**. At Run, eligible placement correction uses quintic easing `6t⁵−15t⁴+10t³`, position interpolation and quaternion Slerp, moving artwork and collision transform together. Current authored assisted fan corrections take 0.15 s; other authored corrections commonly take 0.4 s, with a 0.1 s runtime minimum. Values belong to level/part definitions, not a global visual snap. Precise placement correction is zero. Preserve bounded continuous motion; do not teleport parts or expose solution-alignment controls.

Simulation is fixed at 120 Hz with four substeps. This does not establish a guaranteed rendered frame rate or render interpolation. Current success stops the simulation and can freeze mechanisms mid-animation; there is no celebration overlay or animated transition. Run changes to Build Again and a short positive bottom message appears. Reset restores the pre-run arrangement. Menu opening, selection and button states are immediate rather than animated.

## Known limitations, not approved permanent constraints

The visual language is approved; unfinished behaviour is not frozen by this document.

- Mobile/touch-first gestures, safe-area layouts and real-device testing remain TODO.
- Keyboard-only manipulation, screen-reader semantics, contrast and colour-vision accessibility are not comprehensively verified. Internal action metadata alone is not an accessibility guarantee. Preserve labels and affordances while improving this.
- Dense scenes can obscure deeper parts; scrolled palette rows can clip. Improve these without adding opaque walls or persistent control clutter.
- Springs need activation animation; success should not abruptly freeze an otherwise fluid chain reaction.
- Distinct electrical sources/wires, motors/chains/belts, and weights/pulleys/ropes are planned, not implemented visual systems. New systems should use this palette, simple geometry and icon language, while giving different connection types clearly different shapes as well as colours.
- There is no claimed reduced-motion mode, sound design or animation accessibility system in this snapshot.

See [TODO.md](TODO.md) for the live backlog. Do not document a planned feature as already present.

## Preservation check for future changes

Before accepting a visual change:

- Compare at 1440 × 900 in both build and run states, including a selected part, preview, all three gizmo axes, an open menu and a dense puzzle.
- Keep the sky/wood/cream/navy/gold hierarchy, simple toy silhouettes and a readable uncluttered puzzle.
- Check real exported-browser icons, not only editor/import previews.
- Verify fixture and placed-part projections, transparent walls, and solid-floor behaviour.
- Ensure move/rotate controls remain contextual, goal/hint panels collapsed by default, and hover does not summon long floating text.
- Exercise selection, drag, Shift, Escape, Undo, Run and Reset; visual changes must not silently change placement or physics.
- Review active mechanisms and assisted motion in motion, not only screenshots.
- Record intentional exceptions here; do not overwrite the approved style incidentally while implementing a new feature.

## Source of truth

Implementation references: [Workshop UI/stage](ui/Workshop.cs), [guidance and layout behaviours](ui/WorkshopGuidance.cs), [icons](ui/WorkshopIcons.cs), [gizmo](ui/RotationGizmo.cs), [projections](ui/PlacementShadows.cs), [part geometry/materials](engine/PartArt.cs), [selection](engine/MachinePart.cs), [workbench](engine/Workbench.cs), [assistance easing](engine/PartAssistance.cs), [part implementations](parts), [catalog colours](parts/catalog), and [project settings](project.godot).

Existing browser evidence was visually checked at `.playwright-mcp/L05-precise-near-positive-direct-reset-outcome.png`; it shows the bright toy-workbench composition and sparse solved-state UI. That local screenshot is a reference, not a complete accessibility or animation audit. Additional browser observations are recorded in [docs/browser-playtest.md](docs/browser-playtest.md).

