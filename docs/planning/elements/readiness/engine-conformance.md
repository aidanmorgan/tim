# Engine conformance standard for element specs (owner instruction, 22 November 2026)

The owner requires that every element spec conforms to the current design: a generalised engine with no custom per-element logic, data-driven components, visuals separate from colliders separate from animations, and a proper high-performance physics engine. This standard binds every element-spec author, repairer and auditor. Where it and an older spec text differ, this wins; the older text is rewritten, not kept beside it (forward-only).

## Authorities

- `AGENTS.md`: "Element-free engine gate", "Tri-Graph compile-to-state architecture", "Refactor forward", typing and f32 rules.
- `_bmad/custom/element-free-engine-design.md` (contracts D1–D7) and `_bmad/custom/element-free-engine-review.md` (E1–E9).
- `docs/gpu-f32-physics.md` (compilation model, game-grade envelope, rates) and `docs/architecture/element-package-isolation.md`.
- [owner-decisions-2026-11-22.md](owner-decisions-2026-11-22.md), especially 3a (engine story names and order).
- Earlier owner decisions: no specific handler per interaction — fix the one generic law or data; scenarios are tests. Visible = solid.

Use the documents' own terms and numbers. Do not invent a budget, limit or rate that those documents and the code do not state; where a spec needs one that is missing, say so and name the engine story that must set it.

## What each spec shows

1. **Declaration data only.** The element is declaration data, art and bindings. The spec names no element-specific solver, kernel branch, update loop, per-tick callback, event handler, script, state machine in code, or engine branch on an element, kind or package id. Every behaviour is expressed as bodies, colliders, materials, joints, drives, stores, typed ports and domain nodes, sensors, declared parameters and declared presentation bindings — each an instance of a general engine capability any package could declare.
2. **Generic laws.** Each behaviour row names the general law that produces it and the declared values that parameterise it. A row that reads "when X touches this part, do Y" is rewritten as a consequence of a generic law and data, or the missing general capability is named with exactly one owning `ENGINE-*` story, stated element-free. A capability that would exist only for this element is a finding: generalise it or redesign the element from existing capabilities.
3. **Three separate layers, declared separately.**
   - **Colliders (physics graph):** collision shapes with dimensions, body, material and filter; hidden `col_` proxies; from the engine's admitted shape set. Nothing in physics reads art.
   - **Visuals (rendering graph):** art meshes, materials and instancing; never used as colliders; each static proud visible solid has its own collider or is flush or decorative for an allowed reason.
   - **Animation (animation graph):** cosmetic motion declared as presentation bindings (driver → target node property), evaluated off the physics thread; reads committed physics or domain state and never writes back to physics, sensors or goals. Art moved by a binding is decorative; anything that must push, block or be pushed is a physics body with a collider, and its art follows the body's pose.
   For each art node the spec states what moves it: a body pose, a presentation binding, or nothing.
4. **One-way data flow and typing.** Physics → animation → renderer and physics → renderer; nothing flows back. Closed sets are enums; ids are typed; game values are f32. Authoring strings appear only at UI and serialisation boundaries.
5. **Compiles to flat state.** Everything declared compiles at Run into the engine's flat tables: fixed counts of bodies, colliders, joints, nodes and stores known at compile time; no per-instance managed objects or allocation during a tick; no per-element ordering dependency beyond the engine's declared tick phases. The spec states the counts (bodies, colliders by shape, joints, drives, sensors, domain nodes, stores, bindings).
6. **Performance fitness.** Few primitive colliders; a reason for any collider count above the nearest delivered part's; no collider or joint whose only purpose is cosmetic; static parts are static bodies; sensors and field regions are bounded volumes queried through the engine's spatial query interface; nothing requires per-tick work proportional to the whole scene; fast small bodies rely on the engine's general tunnelling protection, not a per-element check. Where the engine's present implementation is not general or fast enough, the spec records the engine gap with its owning story and does not design around it with element logic.
7. **Tests are scenarios.** Acceptance rows exercise the real production simulation through defined interfaces and prove the generic law's consequence for this declaration; none depends on an element-specific code path.

## For auditors

Give a conformance verdict per spec — **Conforms / Conforms with fixes / Does not conform** — with each violation as `file:line`, which of points 1–7 it breaks, and the exact rewrite or the engine story (with element-free story text) needed. A missing capability with exactly one owning element-free engine story counts as Conforms. For delivered packages, search the engine for code that names or special-cases the element (package id, kind and class names under `engine/` and the `CuriousContraptions.*` engine projects); any hit is a finding recorded against the spec as a required forward-only refactor.
