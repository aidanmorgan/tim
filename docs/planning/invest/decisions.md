# Bounded model decisions inside existing owners

Each row settles one model question for a generic capability family before the slice that first needs it ([compilation model](../../gpu-f32-physics.md#compilation-model)). Reuse an applicable independently reviewed answer; otherwise take exactly one question, build the named construction in Chrome through real controls, and stop with the adopted/rejected model, units/ranges and the player-observable expected result at puzzle scale within the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope). Elements never get their own solver; the [engine contracts](../../engine-contracts.md#general-data-driven-engines) are the authority for that rule. A diagnostic does not pass runtime acceptance. Each decision is a supporting design criterion of its functional owner, with the same pair and required-stage review.

[Full requirement bodies](../requirements.md) · [Family boundaries](profiles.md).

<a id="s257"></a>
## S257

Exact original domain criterion and technical stage: S257. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| electrical-port: Which electrical source/load/control roles, directions and capacities are valid without conflating signal with power? | One source/load/control wired through real controls; a reversed or incompatible wire is refused at connection and the construction is unchanged; a full-capacity attempt refuses the extra wire. | S270 |
| mechanical-port: Which shaft/belt/chain attachment roles and direction/capacity constraints preserve distinct connection contracts? | One supplied shaft pair per connection kind drives its load in the declared direction; an invalid attachment is refused; chain and belt are not interchangeable in the palette. | S690 |
| rope-port: Which rope anchors/sheaves/control attachments own length and load, and which attachments reject? | One loaded routed rope lifts its load; a slack rope lifts nothing; an invalid endpoint or over-capacity attachment is refused and the construction is unchanged. | S688 |

<a id="s416"></a>
## S416

Exact original domain criterion and technical stage: S416. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| advection: Which finite-volume transport update preserves mass at empty, full and split/merge boundaries? | Two containers and one branching route: the receiving levels rise by what the source loses; an empty source sends nothing; Reset restores the levels. | S418 |
| pressure-work: How does the pressure/head model move a loaded hydraulic boundary? | One source, piston and return: the piston moves under head, stalls against a blocked load and stops when the supply is depleted. | S419 |
| buoyancy: Which displaced-volume/immersion rule applies? | One body crossing the surface: a lighter-than-water body floats at a visible waterline, a denser one sinks; the water level stays conserved. | S420 |
| capillary: Which bounded wick model and capacity limit are adopted? | One reservoir and porous path: the wick wets up to its limit, a dry reservoir wets nothing, reversed head does not lift. | S421 |

<a id="s470"></a>
## S470

Exact original domain criterion and technical stage: S470. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| gas-state: Which pressure-volume-temperature update represents the sealed-gas state? | Two connected volumes: compressing one visibly pushes the other; a closed valve isolates them; an exhausted source stops movement; Reset exact. | S471 |
| open-versus-sealed: Which declarations expose an open airflow field versus sealed pressure ports? | One fan/receiver and one sealed reservoir/valve: cross-domain wiring is refused; the fan pushes a ball, the sealed valve moves its actuator. | S697 |

<a id="s484"></a>
## S484

Exact original domain criterion and technical stage: S484. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| finite-colour: How does one optical source allocate finite per-channel output across simultaneous receivers? | One split source and two receivers: neither receiver reads more than it would alone; a wrong-channel or no-light receiver stays off. | S485 |
| optical-commit: Which snapshot boundary prevents moving occlusion or multiple receivers duplicating light? | One moving occluder across a shared beam: receivers switch together at the crossing; no receiver flickers on while occluded. | S486 |
| optical-law: Which interval transport model handles partial occlusion and transparent/hollow paths? | One beam through transparent, hollow and opaque parts: receiver lights behind transparent and hollow, off behind opaque. | S488 |
| absorption: How is absorbed light converted into material heat without duplicate power? | One illuminated absorber and a transparent control: the absorber warms visibly, the transparent part does not; unlit control stays cold. | S489 |

<a id="s528"></a>
## S528

Exact original domain criterion and technical stage: S528. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| arrival-combination: Strongest arrival or additive intensity for simultaneous paths? | One equal two-path arrival and one unequal delayed path on a Sound meter: choose one rule, observe the meter reading at the chosen threshold, record the rejected alternative. | S530 |
| event-routing: How are event identity, delay and loop rejection represented without suppressing a later pulse? | One horn/duct loop with repeated arrivals and a second strike: the meter triggers once per admissible path, the loop does not grow, the second strike triggers again. | S529 |

<a id="s543"></a>
## S543

Exact original domain criterion and technical stage: S543. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| conduction: Which contact/conductivity law transfers heat between finite stores? | Two touching bodies at unequal temperature equalise visibly; an insulated control does not. | S544 |
| convection: Which fluid-to-surface transfer model governs convection? | One flowing material over a surface warms/cools it; disabled exchange leaves it unchanged. | S545 |
| thermal-radiation: Which radiant heat exchange law applies, separate from ionizing transport? | Two facing surfaces exchange heat; an occluder between them stops it. | S546 |
| melting: How are latent heat and state represented through melting? | A solid heated below the point stays solid; at/above it loses support and melts; Reset restores the solid. | S547 |
| freezing: How does cooling cross freezing? | A liquid cooled to the point visibly solidifies; without a colder sink it stays liquid. | S548 |
| evaporation: Which surface evaporation limit debits liquid? | A wet surface dries over time; a dry or exhausted one shows no vapour. | S549 |
| boiling: Which pressure/temperature boundary permits boiling? | A heated vessel produces vapour only above its boundary; empty vessel produces none. | S550 |
| condensation: How does vapour release heat and become liquid? | A cooled vapour stream yields visible liquid; insufficient cooling yields none. | S551 |
| sublimation: Which solid-to-gas transition is supported? | A declared material sublimes directly; an unsupported material is refused. | S552 |
| deposition: Which gas-to-solid transition is supported? | A gas deposits solid on a cold surface; a warm surface grows nothing. | S553 |
| reaction: Which stoichiometry and rate limit are adopted? | Two finite reactants produce products until one is exhausted; a missing reactant produces nothing. | S554 |
| ignition: Which conditions initiate combustion? | Sufficient heat with oxidiser ignites; insufficient energy or no oxidiser does not. | S555 |
| extinction: Which boundary stops combustion? | Each declared suppression cause visibly stops the burn and leaves residue. | S556 |
| expansion: How does expansion change dimensions and exert work? | A heated free member lengthens; a constrained one pushes its load; unheated control is unchanged. | S557 |
| thermal-stress: Which gradient/constraint model reaches failure? | A constrained member fails under differential heating, not uniform heating. | S558 |
| heat-pump: Which work/heat bound applies to active cooling? | A powered pump cools its cold side and warms its hot side; unpowered does nothing. | S559 |
| thermoelectric: How does a temperature difference yield electrical work? | Hot/cold pair lights a lamp; equal temperatures or open load light nothing. | S560 |
| phase-storage: How does a phase store retain/release latent energy? | A charged store keeps a load warm after the source stops; a full store takes no more. | S561 |
| coating: Which deposited material changes surface properties? | A coated surface behaves differently (friction/heat) only where coated; unsupported pairing refused. | S562 |
| fracture: Which strength rule creates fragments? | Just above the break condition the object splits into bounded pieces; just below it holds; unsupported material refused. | S563 |
| electrical-heat: Where does dissipated electrical work enter heat? | A loaded resistor warms; disconnected or exhausted supply leaves it cold. | S564 |
| phase-topology: Which committed phase change adds/removes geometry atomically? | A melting barrier opens a path the ball can use; the old and new shapes never coexist; Reset restores. | S565 |
| temperature-strength: Which strength-versus-temperature curve applies? | A loaded hot member fails where the cold one holds; display colour never selects behavior. | S566 |

<a id="s605"></a>
## S605

Exact original domain criterion and technical stage: S605. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| photon: Which Photon attenuation/dose model maps source to receiver? | One source and two shield thicknesses: thicker shield reads less; missing source or gap reads as declared. | S606 |
| alpha: Which Alpha range/stopping law applies? | Thin shield passes, thick shield stops; no optical substitute lights the detector. | S606 |
| beta-minus: Which BetaMinus deflection model and sign apply? | Through a field the trail bends one way; reversed polarity bends the other; no field goes straight. | S606 |
| neutron: How do Fast/Slow groups moderate/absorb? | With moderator the slow detector reads, without it the fast detector reads; absent source reads nothing. | S606 |
| decay: Which deterministic activity envelope distinguishes a decaying capsule? | At successive declared intervals the detector reads less; Reset restores the initial reading; a steady source does not fade. | S607 |

<a id="s635"></a>
## S635

Exact original domain criterion and technical stage: S635. Each row is separately bounded; this domain parent is not one ready story.

| Label / concrete question | Chrome construction and expected observation | Named next implementation |
| --- | --- | --- |
| field-response: Which field/material force law supports substitutable responders? | A supported responder moves in the field, an unsupported one does not; zero source or reversed field reverses/halts motion. | LAW-FIELD-I |
| granular: Which grain representation preserves openings, jamming and finite material? | Finite feed passes a wide opening and jams a narrow one; the count delivered never exceeds the count fed. | LAW-GRANULAR-I |
| controller: Which typed finite-state controller expresses sensing/intent? | A sensed target produces the declared action; missing supply or unsupported command produces none; nothing teleports. | LAW-CONTROLLER-I |
| goal-occurrences: Which occurrence identity/window/order rules implement the four goal primitives? | One level per quantity/rate/order/protected-final variant: duplicate, out-of-window, wrong-order and unsafe-final routes do not solve; the intended route shows Solved once. | LAW-GOALS-I |
| environment: Which gravity/atmosphere/material ranges are immutable for a Run and persisted? | A nondefault environment changes the ball's fall visibly; out-of-range values are refused; Save/Load and Reset keep it. | LAW-ENVIRONMENT-I |

<a id="decision-gltf-element-assets"></a>
## Architectural Decision: Adoption of glTF 2.0 (.glb) for Element Visual and Collision Meshes

**Status:** Approved / Authoritative

### Context
Earlier prototypes explored constructing puzzle element 3D geometry and physical colliders imperatively in C# (`engine/PartArt.cs`, procedural ramp prisms, wall boxes, bumper domes) or relying on hardcoded C# coordinate and collider tables. This approach created tight coupling between engine source code and individual puzzle elements, violated the zero-element-assumptions invariant, bloated compiled C# assemblies, and prevented 3D artists from using standard digital content creation (DCC) workflows (such as Blender).

### Alternatives Considered
1. **Imperative Procedural Primitives in C# (`PartArt.cs`):**
   - *Rejected:* Violates the data-driven architecture by requiring C# compilation for visual or geometric adjustments. Severely restricts artistic expression and sculptural aesthetic (Monument Valley diorama styling). Fails the core requirement that new puzzle elements must be pure declaration data with zero engine code edits.
2. **Custom JSON Mesh / Shape Definition Formats:**
   - *Rejected:* Heavy custom parser maintenance burden. No DCC ecosystem support without building and maintaining custom Blender import/export plugins. High runtime deserialization overhead (parsing large JSON arrays of vertices and indices on the main thread).
3. **Proprietary Autodesk FBX:**
   - *Rejected:* Closed binary format with complex licensing restrictions. Monolithic, heavyweight parser dependencies inconsistent with web deployment. Lacks modern web graphics ecosystem alignment.
4. **Wavefront OBJ:**
   - *Rejected:* Legacy text-based format lacking standard PBR materials, transform hierarchies, node naming conventions, and modern physical extension standards. Requires managing loose multi-file `.obj` + `.mtl` + texture dependencies.

### Decision
Adopt open-source, industry-standard **glTF 2.0 binary (`.glb`)** as the universal format for defining BOTH visual meshes and physical collision meshes across all puzzle elements, fixtures, and tools:
- **Visual Meshes:** Authored in the `.glb` scene with standard PBR metallic-roughness materials conforming to the project color palette and sculptural aesthetic in [DESIGN.md](../../../DESIGN.md).
- **Co-Located Collision Geometry:** Authored directly within the same `.glb` container alongside visual meshes using standardized analytic shape proxy nodes (`col_box`, `col_sphere`, `col_capsule`, `col_cylinder`), convex hull proxies (`col_hull`, `-convcol`), or the official Khronos `KHR_physics_rigid_bodies` extension.
- **Socket Anchors:** Authored as empty locator nodes `Loc_Port_<Id>` inside the `.glb` transform hierarchy, matching declared `PortDefinition.Id` entries.
- **Articulated & Secondary Sub-Assemblies:** Secondary dynamic components (e.g., motor shafts, springboard moving plates, lever arms) group their visual, collision, and locator nodes under dedicated child branches in the `.glb`.
- **Zero Code-Defined Geometry:** Imperative C# procedural mesh generation (`engine/PartArt.cs`) is deprecated and scheduled for complete deletion at `LEGACY-0`.

### Rationale
- **Universal Industry Standard:** Developed by the Khronos Group and universally supported across Blender, modern DCC tools, Godot, WebGL 2, and WebGPU pipelines.
- **Self-Contained Single Binary File:** Binary `.glb` embeds geometry, PBR materials, textures, transform hierarchies, and node metadata into a single compact binary payload, eliminating missing-file sync issues.
- **Physics Extensibility:** Standard node naming conventions combined with `KHR_physics_rigid_bodies` enable seamless artist authoring of collision hulls and analytic primitives without writing engine code.
- **Strict Decoupling:** Allows puzzle elements to be added purely through declarative catalog resources (`.tres`) pointing to `.glb` assets, satisfying the zero-element-assumptions invariant and dynamic part discovery.

### Impact on Engine & Workflows
- **Compiler:** `WorkshopPhysicsCompiler` inspects `.glb` scene hierarchies at compile-to-state time, extracts `ColliderDeclaration` records from collision nodes without element-specific branching, and packs them into flat SIMD Structure-of-Arrays (SoA) tables.
- **Engine Codebase:** Procedural mesh code in `PartArt.cs` and hardcoded collider dimension tables are deprecated and scheduled for deletion at `LEGACY-0`.
- **Artist Workflow:** Artists author 1:1 metric assets in Blender, visually verifying PBR materials, collision proxies, and socket locators, and export directly to `.glb`.

<a id="decision-dynamic-part-discovery"></a>
## Architectural Decision: Dynamic Puzzle Part Discovery & Extensible PartId Architecture

**Status:** Approved / Authoritative

### Context
Earlier implementations relied on a closed `WorkshopPartKind` C# enum and hard-coded file path registrations in `PartRegistry.Discover()`. Introducing or altering any puzzle element required modifying engine enums, updating switch statements across the codebase, and creating brittle test dependencies. This directly contradicted the requirement that new puzzle elements must be pure declarative data without engine code modifications.

### Decision
1. **Dynamic Discovery:** parts are discovered without a hand list. Story 6.5b scanned the former `.tres` catalogue at runtime; Story 9A.14 retired that scan, and `tools/BundleElements` now discovers the `elements/*` packages at build time into the one bundle that `PartRegistry.Load()` reads.
2. **Extensible Strongly-Typed Identities:** Puzzle parts are identified by a strongly typed `PartId` record struct wrapping a canonical string identifier (e.g. `PartId("basketball")`, `PartId("springboard")`). Closed enums for puzzle parts are permanently deprecated and eliminated.
3. **Generic Part Instantiation:** `PartRegistry.Create(PartId)` instantiates parts directly from `definition.Scene` and configures them as generic `MachinePart` entities without switch statements.
4. **Dynamic Inventory Allocation:** In Free Workshop mode, `WorkshopInventoryPolicy.Free` populates the palette dynamically from all discovered `PartRegistry.Definitions` with unlimited allowances.

<a id="decision-existing-implementation-conversion"></a>
## Architectural Decision: Conversion of Existing Implementations to New Design (Zero Backwards Compatibility)

**Status:** Approved / Authoritative

### Context
Curious Contraptions currently possesses 14 existing puzzle element implementations created during early engine bringup (Basketball, Bowling Ball, Tennis Ball, Balloon, Receiver, Ramp, Wall, Pinball Bumper, Domino, Springboard, Impact Switch, Signal Lamp, Delay Box, Battery). These implementations rely on procedural mesh building in C# (`engine/PartArt.cs`), hardcoded C# collider dimensions (`WallDimensionsResource.cs`, `RampDimensionsResource.cs`, `ReceiverGeometry.cs`, `SpringboardResource.cs`, etc.), and bespoke per-part C# classes (`parts/*Part.cs`).

### Decision
1. **Sequential Forward Conversion:** All 14 existing implementations must be converted to the new declarative architecture:
   - Self-contained glTF 2.0 (`.glb`) assets in `parts/models/` defining visual meshes and co-located `col_*` collision proxy nodes.
   - Named locator nodes (`Loc_Port_<Id>`) for socket anchor resolution.
   - Declarative `.tres` catalog resources in `parts/catalog/`.
   - Generic `MachinePart` instances replacing bespoke `parts/*Part.cs` classes.
2. **Strict Zero Backwards Compatibility Invariant:**
   - No backwards compatibility shims, adapters, legacy wrappers, or fallback loaders may be retained or created.
   - Procedural mesh routines in `engine/PartArt.cs`, closed enums in `engine/gpu/WorkshopPartKind.cs`, hardcoded collider dimension resources, and bespoke `parts/*Part.cs` files must be completely and permanently deleted upon conversion.
   - Old scene or persistence formats that rely on deprecated enums or procedural code are rejected atomically.
   - Git history serves as the permanent archive. No dead code is retained in the active tree.
