# Hollow geometry

Hollow passages (Clear pipe CAT-048, Funnel CAT-030, bends CAT-049/050 and collars) are declared hollow/compound colliders evaluated in the shared shape-pair table of the single WASM SIMD f32 solver ([capability inventory](gpu-f16-physics.md#capability-inventory)). There is no annular-specific kernel and no AnnularFeature enum; the roadmap slice [CAT-048](planning/invest/vertical-delivery.md#rolling-playable-roadmap) deletes that legacy and makes radii authored data.

## Declaration data

- Straight tube or frustum: inner radius, outer radius (end radii for a frustum), length along the owning body's local X, material, local pose.
- Bend: inner/outer radius, centreline radius R and sweep; the centreline is R × (sin a, cos a, 0) for a in [0, sweep].
- The bore and both end openings are physically open; a filled convex hull is not an admissible substitute. Hollow bodies may be static or dynamic and may rotate/translate like any body.
- Admission rejects unsupported dimensions, self-intersection or unrepresentable geometry atomically at compile, before any mutation; nothing silently downgrades.

## Behaviour

A payload contacts the outer shell, the bore wall and the end rims through the same speculative-contact pass as every other shape (margin |v|·dt + slop, [envelope](gpu-f16-physics.md#game-grade-envelope)): a ball rolls or falls through a tube, follows a bend, jams when its diameter exceeds the bore and is deflected by a rim it strikes. Tube/frustum/bend junctions are continuous. A wall at least 1 cell thick is never tunnelled at up to 64 m/s. Moving hollow bodies use swept bounds covering full angular displacement. Resting penetration stays within contact slop; it never faults a tick.

## Chrome-observable acceptance (CAT-048)

- `clear_pipe` is solved through the UI, including fast and edge entries; the visible pipe pose matches the physical body; no teleportation or scripted routing.
- Controls: an oversize payload jams; a ball aimed beside the opening misses; an initially overlapping placement rejects at compile.
- Exact Reset and Save/Load after every Run; same construction and inputs give the same outcome.
- `rg -i annular engine/` finds nothing.
