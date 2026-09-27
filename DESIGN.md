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

## Optical logic gates

Five operations share a cyan cube and navy foot. Cream-rimmed left/top control lenses carry one/two raised marks; a gold-rimmed front carrier enters separately and leaves the gold-rimmed right outlet. Controls absorb light and never become output energy. The outlet lights only when a carrier actually exits, not merely when Boolean truth permits it. Control lamps ease slate-to-gold; physics changes only on fixed-tick boundaries.

A small cream relief shows the four two-input truth rows with raised gold output dots. Toolbox pictograms distinguish the operations without adding permanent panels. Preserve the palette. Current NOR browser captures demonstrate carrier/no-carrier output and downstream actuation, not legibility of every tiny relief mark or continuous animation quality; those remain visual-review work.

## Supplied electrical logic

The Both gate retains its cyan body, cream face, navy base and two numbered condition inputs. A separate lower-front gold socket accepts the energy supply; it is not a third condition. The output lamp indicates supplied output, so two lit input lamps with an unpowered supply leave the output slate. The real-UI positive/negative pair verifies that distinction. OR/XOR/NOR/NAND share this separation and body silhouette. Each displays its original navy operation pictogram on the cream face, matching its toolbox icon; small raised gold truth-row marks supplement it. Invalid feedback stays in build mode and uses the existing bottom status line, not a new popup. Keep that explanation short enough to fit the viewport.

## Sound speaker

Use a cream speaker cabinet on a navy foot, a gold-rimmed navy diaphragm and cyan centre. Two raised bars identify the default mid tone. Its diaphragm gives a small eased vibration after a real pulse; thin, shadow-free translucent wavefronts expand forward and fade with distance. Avoid solid gold hoops that dominate the diorama. The visible pulse and sound meter must remain useful when audio is muted. Current desktop wave contrast is subtle; mobile contrast and continuous motion remain review items.

## Sound meter

The sound meter is a cyan cabinet with a cream dial, navy tick marks, a gold eased needle and a slate-to-gold output lamp. The needle shows received intensity even without supply; the lamp indicates supplied output. Keep the top-right activation socket distinct from the lower electrical sockets. A wall may block reception while the speaker's cosmetic ring remains visible beyond it: the meter, not the decorative ring, is the authoritative feedback. Acoustic wave clipping and richer material behaviour are future work.

## Impact bell

A small gold tapered bell has a rounded crown, cream rim, navy hanging stem and cyan clapper. Keep its silhouette simple and toy-like; do not add an electrical socket. Impacts excite a damped rocking motion without snapping the pose on repeat hits or moving the collision proxy. Three very thin, translucent, shadow-free great-circle wavefronts expand in all directions and fade; they supplement the sound meter, not replace it. The bell toolbox pictogram uses the existing navy stroke style. Preserve the current palette.

## Wind chimes

Use a small cream circular cap with a navy hanging stud, four slender gold tubes at different lengths, dark suspension strings, a cream clapper and a cyan sail. The sail and clapper visibly swing together; do not replace their motion with a flashing indicator. Thin fading three-plane wavefronts communicate sound without obscuring the diorama. The original navy line icon shows hanging tubes and a diamond-shaped sail. Preserve the approved palette and restrained geometry.

## Windmill

Use four cyan pitched vanes with gold tips and hub inside a slender cream guard. A cream mast/gearbox on a navy foot supports the rotor; a cream side pulley with a gold spoke clearly exposes its mechanical output. The original navy line icon shows vanes and a small tower. Preserve existing palette values and uncluttered toy geometry.

Rotor and pulley angles follow the actual signed shaft speed: spin-up, reversal through zero and coast-down are continuous simulation motions, not decorative loops. A blocked fan path leaves the rotor at rest. The fixed guard/hub are collision geometry; blades are not individually simulated striking surfaces. Sampled desktop captures verify the connected, disconnected and blocked states; sustained fluidity and mobile review remain outstanding.

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

## Beam shutter addition — 27 September 2026

The standalone optical shutter uses a navy header/foot, two cream rails and a thin gold blade with cream witness stripes on both faces. A gold electrical socket and slate/gold indicator show supply; ochre indicates obstructed closing. It retains the established palette and an original outline icon. Unlike the tube gate it has no tube mouth or snap connection.

The blade accelerates along local Y and retracts into the header. Its rendered pose and physical/optical OBB are identical; partial clearance really admits the beam. Supply loss closes it, stopping before a visible ball rather than crushing it. Shared C# motion keeps tube and optical shutters consistent. Browser open/closed samples are checked; a continuous animation/performance audit remains pending.

## Colour optics addition — 27 September 2026

Channel filter frames are cream with navy feet and translucent panes. Red/green/blue reuse the established gizmo accents (#de7058/#62aa78/#5b9cdb); no global palette changes. One/two/three raised bars on the frame, matching receiver faces and original icons distinguish channels without relying only on hue. Receivers retain the slate-to-gold eased activation disc, so “correct channel” and “currently active” remain separate cues.

Traced single-channel beams use those accents; broadband keeps warm cream and two-channel artwork uses the established combination accents documented below. This is a palette-preserving signal convention, not spectral colourimetry. Opacity follows maximum RGB channel power instead of red alone, keeping blue/green beams visible without granting simulated energy. Filter previews remain faint, selected-only and non-activating. Browser snapshots verify all three colours; the lower-power blue beam is faint against the sky, so broader contrast/mobile review remains pending.

## Combiner and mixed-channel receivers — 27 September 2026

The combiner is a chunky cyan cube on a navy foot. Three cream-rimmed input discs carry one/two/three cream bars; these identify ports, not mandatory input colours. Its smaller gold-rimmed outlet distinguishes the outgoing direction. The output lens eases toward the actual exiting light colour, or slate when dark; light exhausted inside the body cannot falsely light the outlet.

Yellow/cyan/magenta/white-channel receivers retain the cream plate, navy foot and eased gold activation disc. Required RGB channels appear as grouped coloured bar rows using existing accents. Each required channel must meet threshold; the names denote game-channel combinations rather than precise colourimetry.

Runtime beams now share one world renderer. Co-directed paths with the same physical origin sum over their actual overlapping intervals; a shorter colour contribution ends where its range ends. Red+green uses existing gold (#f7cb52), green+blue cyan (#66b8c9), red+blue balloon pink (#ed6378), and three-channel light retains warm cream. Crossing or differently directed paths remain separate. No global palette replacement. Selected combiner previews are faint and non-activating; broader preview/mobile/continuous-motion review remains pending.

## Source of truth

Implementation references: [Workshop UI/stage](ui/Workshop.cs), [guidance and layout behaviours](ui/WorkshopGuidance.cs), [icons](ui/WorkshopIcons.cs), [gizmo](ui/RotationGizmo.cs), [projections](ui/PlacementShadows.cs), [part geometry/materials](engine/PartArt.cs), [selection](engine/MachinePart.cs), [workbench](engine/Workbench.cs), [assistance easing](engine/PartAssistance.cs), [part implementations](parts), [catalog colours](parts/catalog), and [project settings](project.godot).

Existing browser evidence was visually checked at `.playwright-mcp/L05-precise-near-positive-direct-reset-outcome.png`; it shows the bright toy-workbench composition and sparse solved-state UI. That local screenshot is a reference, not a complete accessibility or animation audit. Additional browser observations are recorded in [docs/browser-playtest.md](docs/browser-playtest.md).
