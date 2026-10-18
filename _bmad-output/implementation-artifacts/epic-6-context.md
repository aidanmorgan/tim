# Epic 6 Context: Core Interactive Catalogue Elements

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver the foundational mechanical and aperture elements as declarations in the shared engine, exposed through playable Workshop interactions and progressively taught challenge levels. Each outcome includes its necessary authoring, physics, presentation and lifecycle behavior. Component coverage precedes exhaustive difficulty and campaign sweeps; the product target remains 150 progressively taught levels and unlimited Free Workshop.

## Stories

- Story 6.1: Bowling Ball material declaration
- Story 6.2: Bumper radial contact impulse and finite work store
- Story 6.3: Bumper multi-angle contacts and advanced puzzles
- Story 6.4: Prismatic slider and unified TGS Soft spring constraint
- Story 6.5: Preload energy store and Springboard launch
- Story 6.5b: Dynamic Puzzle Part Discovery & Extensible PartId Architecture
- Story 6.5c: glTF Asset-Driven Geometry & Collider Extraction (ENGINE-CORE-2d)
- Story 6.5d: Existing Implementation Conversion — Core Ball & Obstacle Primitives (CAT-001, CAT-014, CAT-064, CAT-003, CAT-066, CAT-054, CAT-004)
- Story 6.5e: Existing Implementation Conversion — Interactive Mechanisms & Dynamic Fixtures (CAT-015, CAT-023, CAT-062, CAT-063, CAT-035, CAT-022, CAT-005)
- Story 6.5f: Zero-Remnant Procedural Geometry, Per-Part Classes, and WorkshopPartKind Purge
- Story 6.6: Straight Pipe compound cylindrical collider
- Story 6.7: Hollow signed-distance Torus Rim Cap Pipe collider
- Story 6.8: Generic aperture sensor with directional rearm
- Story 6.9: 45-degree Pipe Bend hollow torus segment
- Story 6.10: 90-degree Pipe Bend and joined route
- Story 6.11: Funnel hollow frustum

## Requirements & Constraints

Preserve every named element, fixture, supported mode and acceptance criterion. A family representative does not discharge its variants. Each admitted outcome must be playable through real placement, rotation, configuration and connection controls, with verified authored values, intended behavior and a meaningful unsuccessful/control case. Reset restores construction exactly; Save/Load retains supported configuration and typed connections.

Physical interaction uses actual geometry and contact points. Separated bodies do not receive contact impulses. Added kinetic work comes from a finite admitted store or supplier; ordinary collision response remains when work is unavailable. Presentation must reflect accepted events without becoming evidence that the physical effect occurred.

Keep slices small and independently reviewed by a separate retained reviewer. Required-now correctness, production builds, actual Chrome behavior and ordinary lifecycle/resource ownership accompany implementation. Detailed profiling, device/workload qualification, injected faults and exhaustive retention stress remain at their named later gates. Select checks from actual shared and transitive impact, retain failed attempts and reuse unchanged proof only after establishing applicability.

Retain existing difficulty data and its intended assistance behavior. Manual placement proof cannot silently discharge unimplemented physical nudging or exhaustive campaign acceptance. No archived approach creates an additional prerequisite. Original legacy facts retain the pinned baseline authority; archives provide evidence, not a second current implementation.

## Technical Decisions

Separate the logical authoring graph, compiled physics graph and rendering graph. Run compiles typed declarations into flat contiguous SIMD-aligned state. A dedicated WASM SIMD worker advances physics at 120 Hz with 480 Hz substeps. The independent animation worker samples at 60 Hz; rendering remains display-paced. Data flows from physics to animation/rendering, never back from cosmetic state into physics.

Use shared contact, constraint, sensor, signal and finite-work primitives. Elements supply declaration data, artwork and bindings, never private solvers, kernels or update loops. Elements define visual and physical collision meshes purely via declarative glTF 2.0 (`.glb`) binary assets and `.tres` catalog resources, with co-located `col_*` proxy nodes and `Loc_Port_*` socket locators. Prefer C# and preserve typed enums for closed sets across configuration, admission, worker transport and tests; extensible part identities use strongly typed `PartId` record structs; closed enums like `WorkshopPartKind` are forbidden. Convert external strings only at declared boundaries; reject unsupported or malformed input atomically.

Canonical new game values use IEEE-754 f32 and WASM SIMD. Existing geometry, pose and construction precision lanes remain under their explicit later migration ownership unless pulled into a slice. Do not introduce a second numeric model or claim global f32 qualification from a local change. Ordinary numerical residuals clamp or continue within the game-grade envelope; valid ticks do not fail because of proof-grade arithmetic expectations.

Refactor forward through one current path, updating callers, content, tools and active documentation together. All 14 existing implementations must be converted to the new declarative `.glb` + `PartId` architecture. Procedural mesh routines in `engine/PartArt.cs`, hardcoded collider dimension resources, bespoke per-part C# scripts (`parts/*Part.cs`), and closed part enums (`WorkshopPartKind`) must be deleted permanently. Strictly zero backwards compatibility shims, dual loaders, or legacy runtime modes. Git history preserves deleted code.

## UX & Interaction Patterns

Players inspect a three-dimensional scene, place and rotate parts, configure properties and connect typed sockets, then Run, observe and Reset to retry. Camera orbit and depth views support spatial reasoning. Construction remains available through actual Workshop controls; test-only setters, imported solutions and numeric placement menus are not substitutes.

Preserve the established palette, readable silhouettes and restrained feedback. Cosmetic movement must not change collider geometry. Bumper feedback uses the coral head, cream/gold rings and slate pedestal; overlapping accepted events compose smoothly and settle after physics stops. Difficulty profiles express Forgiving, Balanced and Precise intent without changing the basic player workflow.

## Cross-Story Dependencies

Engine-first delivery precedes new element admission. Bowling Ball and Bumper outcomes exercise existing generic contact and finite-work capabilities. Springboard depends on the shared compliant constraint and stored-energy model. Straight Pipe supplies the body needed by later hollow geometry and aperture sensing; bends then establish joined routing. Funnel air and light integration remains with its later owning systems.

The approved Pipe purge requires a fresh canonical declaration when that story begins. Existing Bumper/Battery acceptance does not qualify full electrical networks, motors or later elements. Each independently verified outcome must complete required publication and production-origin review before a dependent slice begins.
