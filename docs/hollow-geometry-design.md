# Hollow geometry

Hollow passages (Clear pipe CAT-048, Funnel CAT-030, bends CAT-049/050 and collars) are declared hollow colliders evaluated in the shared shape-pair table of the single WASM SIMD f32 solver ([capability inventory](gpu-f32-physics.md#capability-inventory)). There is no annular-specific kernel and no AnnularFeature enum; the roadmap slice [CAT-048](planning/invest/vertical-delivery.md#rolling-playable-roadmap) deletes that legacy and makes radii authored data.

**Owner decision "true hollow shapes" (25 Oct 2026).** The engine slice [`engine-hollow-shapes`](../_bmad-output/planning-artifacts/epics.md#story-engine-hollow-shapes) adds three true hollow collider kinds to the shared pair table and moves every hollow part onto them:

- **Ring:** a straight tube wall or collar between an inner and an outer radius, with annular end rims.
- **Torus segment:** a bend wall of tube radii about a centreline radius over a sweep, with annular end rims.
- **Frustum:** a tapered tube wall between two end radius pairs, with annular end rims.

## Current state (not the target)

A collider audit (25 Oct 2026) found that the hollow parts are box approximations today. The collars are square frames reaching 0.95–1.24 m against the drawn 0.78 m (the funnel inlet 1.6–2.16 m against 1.43 m); the 45° and 90° bends are octagons with 4–8 cm slots between slices; a snapped pipe joint dips about 1.2 cm. The legacy CPU segmentation's 5 mm surface-error limit was dropped with it, and nothing has replaced it yet. `engine-hollow-shapes` deletes these approximations; no box-built hollow collider stays as a second path.

## Declaration data

- Straight tube or frustum: inner radius, outer radius (end radii for a frustum), length along the owning body's local X, material, local pose.
- Bend: inner/outer radius, centreline radius R and sweep; the centreline is R × (sin a, cos a, 0) for a in [0, sweep].
- The bore and both end openings are physically open; a filled convex hull is not an admissible substitute. Hollow bodies may be static or dynamic and may rotate/translate like any body.
- Admission rejects unsupported dimensions, self-intersection or unrepresentable geometry atomically at compile, before any mutation; nothing silently downgrades.

## Surface-error bound

The collider surface follows the drawn surface within a stated bound: the largest distance between the two, for every kind. `engine-hollow-shapes` derives the bound, records its value and derivation here and in [gpu-f32-physics.md](gpu-f32-physics.md#game-grade-envelope), and tests it per kind. Until then no bound is stated; the dropped 5 mm limit is not carried forward by default.

## Behaviour

A payload contacts the outer shell, the bore wall and the end rims through the same speculative-contact pass as every other shape (margin |v|·dt + slop, [envelope](gpu-f32-physics.md#game-grade-envelope)): a ball rolls or falls through a tube, follows a bend, jams when its diameter exceeds the bore and is deflected by a rim it strikes. Tube/frustum/bend junctions are continuous: a ball crossing a snapped straight–straight, straight–bend or bend–funnel joint meets no dip or step beyond the surface-error bound. A wall at least 1 cell thick is never tunnelled at up to 64 m/s. Moving hollow bodies use swept bounds covering full angular displacement. Resting penetration stays within contact slop; it never faults a tick.

## Chrome-observable acceptance (CAT-048)

- `clear_pipe` is solved through the UI, including fast and edge entries; the visible pipe pose matches the physical body; no teleportation or scripted routing.
- Controls: an oversize payload jams; a ball aimed beside the opening misses; an initially overlapping placement rejects at compile.
- Exact Reset and Save/Load after every Run; same construction and inputs give the same outcome.
- `rg -i annular engine/` finds nothing.

Before the post-catalogue E2E phase these cases are proved natively against the production simulation; the Chrome runs are queued for that phase.
