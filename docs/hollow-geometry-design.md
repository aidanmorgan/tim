# Hollow geometry

Hollow passages (Clear pipe CAT-048, Funnel CAT-030, bends CAT-049/050, the Ball detector's and Powered gate's collars) and round
rings are declared hollow colliders evaluated in the shared shape-pair table of the single WASM SIMD f32 solver
([capability inventory](gpu-f32-physics.md#capability-inventory)). There is no annular-specific kernel and no AnnularFeature enum;
radii are authored data.

**Owner decision "true hollow shapes" (25 Oct 2026), implemented in the unreviewed candidate for
[`engine-hollow-shapes`](../_bmad-output/planning-artifacts/epics.md#story-engine-hollow-shapes).** The engine has three true hollow
collider kinds, and every hollow part is moved onto them. The box approximations (16-box shells, octagonal bend slices with slots
between them, tilted funnel rings, square collar frames) are deleted; no box-built hollow collider remains as a second path.

## Kinds

Each kind is a 2D profile revolved about its local +Y axis (`engine/gpu/HollowGeometry.cs`, `HollowSolid`, the single geometric
definition the narrowphase and placement both read):

- **Tube** (an annular cylinder): the rectangle ρ ∈ [r_i, r_o], y ∈ [−h, h], a full turn. A straight shell, a stub or a cylindrical
  collar. Its end faces are the annular rims.
- **Frustum** (a tapered tube wall): the quadrilateral (r_0 − t, −h), (r_0, −h), (r_1, h), (r_1 − t, h), a full turn, with a constant
  radial wall t. The funnel wall.
- **Torus** (a torus segment): the annulus r_i ≤ |(ρ − R, y)| ≤ r_o (a disc when r_i is zero: a solid tube) revolved over the sweep
  a ∈ [0, θ] from local +X toward +Z, with flat annular end faces; θ is in (0, π] or a full turn 2π. A bend wall (hollow tube,
  partial sweep) or a drawn ring or collar (solid tube, full turn).

## Declaration data

- **Lanes** (the physics ABI's existing radius and three half-extent lanes; no new lanes): Tube — radius r_o; (r_i, h, 0). Frustum —
  radius r_0 (outer at −h); (t, h, r_1). Torus — radius R; (r_i, r_o, θ). The same lanes are the bundle's `ColliderDescriptor`
  (`Radius` and `HalfExtents`); a solid kind keeps its radius in its half-extents and declares a zero radius lane (the bundle
  validator refuses any other).
- **Scene proxies** (`tools/BundleElements/SceneGeometryExtractor.cs`): a hidden `col_*` `MeshInstance3D` that shares the art's own
  mesh resource, drawn at uniform scale:
  - a `CylinderMesh` with both caps off and the node's `metadata/wall_thickness` t: equal radii give the Tube (outer r, inner r − t,
    half-length height / 2), unequal radii the Frustum (r_0 the bottom radius, r_1 the top);
  - a `TorusMesh`: the full solid Torus of centreline (inner + outer) / 2 and tube (outer − inner) / 2; `metadata/sweep_degrees` makes
    it a segment and `metadata/wall_thickness` hollows its tube.

  One open cap, an open cylinder without its wall and a hollow proxy at nonuniform scale are refused at build time.
- **Admission** (`ColliderDeclaration.Validate`) refuses, atomically at compile, before any state is stepped: a tube or frustum
  without a bore of at least 1/64 m, a wall thinner than 1/1024 m, a frustum with equal end radii (a tube), a torus whose tube reaches
  its axis (R ≤ r_o: self-intersecting), a sweep outside (0, π] that is not the full turn, and any non-finite or out-of-range lane.
  Hollow colliders are static only: a dynamic body still admits one homogeneous sphere, box or cylinder.
- The bore and both end openings are physically open; no filled convex hull is used.

The scene-independent `HollowContactQuery` owns sphere, box and cylinder feature measurements for simulation, placement and compile admission. Simulation alone applies impulses. Candidate scratch holds at most 152 geometric features: 8 box vertices, 12 edge minima, and 6 faces times 22 crest/rim/generator candidates; cylinder and sphere maxima are 75 and 17 respectively. The sphere maximum is one closest surface plus four points on each of at most four segment-end rims. An intersecting independent dynamic payload is refused as `InvalidConstruction` without replacing the prior construction/world. The shared placement slop is 0.1 mm; constrained mechanism children retain their declared own-parent contacts.

## The parts

| Part | Hollow colliders (all at the pipe family's documented restitution 0.15, except the gate frame's 0.1) |
| :--- | :--- |
| Clear pipe | Shell Tube, bore 0.65 to the drawn 0.7 wall, reaching both mouths (half-length length / 2 + 0.09, so the drawn shell is extended to the mouths too); two collar tori (centreline 0.715, solid tube 0.065) |
| 45° and 90° bends | Torus segment (R 2.4, bore 0.65, wall 0.7, sweep 45° or 90°) between the collars; a 9 cm Tube stub from each collar to its mouth; two collar tori. The shell is drawn as 8 or 16 chords plus the two stubs (Godot has no torus-segment mesh) |
| Funnel | Frustum wall (outer 1.35 at the inlet to 0.7 at the outlet, radial wall 0.05: bores 1.3 and 0.65); a 9 cm Tube stub at each end; the inlet (centreline 1.365) and outlet (0.715) collar tori. The drawn shell was turned end for end so its wide end is the inlet |
| Ball detector | Collar Tube, bore 0.65 to the drawn 0.82, half-length 0.28 (its mouths) |
| Powered gate | Sleeve Tube (0.65–0.7, half-length 0.4) and two collar Tubes (0.65–0.78, half-length 0.09, reaching the mouths at ±0.49) |
| Bumper | Its upper ring: the drawn full solid torus (centreline 0.49, tube 0.018) |

The torus parts on dynamic bodies (the Basketball's seam, the Weight's bands and lifting eye) and the pressure plate's ring (a sixth
collider would take its contact-load row's identity) stay uncollided: both wait for
[`engine-multi-collider-dynamic`](../_bmad-output/planning-artifacts/epics.md), which owns multi-collider and dynamic hollow bodies
and the identity layout.

## Pair rules

One rule per pair, through the solid's exact signed distance (`HollowSolid.Distance`: within the sweep the meridian profile's
distance, which is exact for a revolved profile; outside a segment's sweep the nearer end face's planar annulus, exact for θ ≤ π;
inside the wall the nearest of the profile and the end faces' half-spaces):

- **Sphere:** the closest point, with the solid's outward normal (bore, outer wall, end rim or the edge between them). For a full
  revolution it also adds, within the speculative margin only, the nearest surface point in the meridians a quarter, a half and three
  quarters of a turn from the sphere's own, each at its exact distance: a sphere resting on a whole circle (centred in a cone's bore, on
  a rim or on a ring narrower than itself) is then held by the circle rather than pushed off its axis by one arbitrary point.
  A partial torus also measures four stable points on each end rim, completing the contact set for a centred oversized sphere at a bare opening.
- **Box and cylinder:** candidates measured exactly — the partner's vertices, edge minima, rim minima and axis interior minimum
  (a side contact) against the solid's distance, and the solid's support, generator and rim points toward each partner face against
  the partner's distance — kept within the margin, merged within 1 mm and reduced to four: retain the deepest point and the point farthest from it,
  then repeatedly maximize the summed triangle areas formed with retained point pairs. Area ties maximize the summed squared distances
  to retained points, then prefer depth and emitted order; the distance tie preserves both collinear extremes. Emit original
  points in original order, preserving their normals, gaps and features; this spreads support without inventing contact geometry.
  Flat-end cap proof covers both orders and 60/120/240 Hz at 0° and 17° yaw: horizontal landing offset and settled excursion
  independently stay within 20 µm. Rotational surface displacement from the one-second orientation has its own 20 µm bound;
  linear and circumscribed rotational surface speeds each stay below 1 mm/s throughout 1–10 s. Reset/reused/fresh-engine
  replay compares decoded identities, ticks and numeric physical state.
  Every count is fixed. Tilted torus crest directions are projected into the sampled meridian before constructing a surface point. A fully represented cylinder cap omits its redundant moving rim minimum; partial rims and side generators retain their minimum.
- Shared normal-block translation couples rows by inverse mass times the dot product of their normals; nonparallel hollow rim normals obey the same impulse rule as parallel face contacts.
- Placement volume bounds use signed directional support distances, preserving the unoccupied side of a partial torus instead of reflecting its arc about the revolution origin.
- A plane or another hollow collider never pairs with a hollow collider (both static).

## Seams and joints

Snapped parts meet at their mouths, where one part's bore rim lies on the next part's bore. A rolling body sinks into its supporting
wall by its soft contact (up to the 0.5 mm resting penetration), so the next part's rim, or a collar torus's inner equator lying on
the bore, would meet it as a bump of that depth with a normal tilted against its motion. The engine removes such internal features
generically (`WorkshopCollisionKernels.RemoveInternalHollowEdges`, the enhanced internal-edge rule of modern engines): a contact of a
body with one hollow collider is dropped when its surface point lies on or inside another hollow collider the same body touches in that
substep (within 1 mm) and its gap exceeds that collider's smallest gap to the body. A free end, with no hollow collider on its rim,
keeps its lip: a ball still pivots over it. Tube–tube, stub–arc, arc–stub and collar crossings are continuous within the bound below.

## Surface-error bound

- **Analytic fixture target:** retain 10 µm distance error plus at most 0.5 mm resting penetration for the near-origin fixtures in
  `WorkshopPhysicsLawHollowTests`. The former “twenty roundings within 4 m” estimate is not a universal proof: admission permits
  dimensions up to 16 m and translated world coordinates, and dot products, square roots, trigonometry and projection must all be
  accounted for. Literal 16 m dimensions and a 32 m translated axis-permutation fixture are checked independently, but universal qualification of the 10 µm target remains incomplete; the admitted domain is unchanged.
- **Declared surface against drawn mesh: at most 5 mm.** A circular mesh contributes r(1 − cos(π/n)). A torus contributes the sum
  of its major-circle bound (outer radius) and tube-circle bound, not their maximum. Pipe-family collars now use 64 major segments
  and 16 tube segments: at most 2.98 mm at the 1.43 m funnel inlet, and 2.20 mm at a 0.78 m collar. Shell/stub circles use 64
  radial segments (0.844 mm at radius 0.7 m). A bend's chord adds R − sqrt(R² − a²), with R = 2.4 m and a = 0.13 m:
  3.524 mm plus 0.844 mm = 4.368 mm. Detector and gate circles retain 32 segments, below 4 mm.
  `VisibleSolidColliderTests` reads the actual mesh subdivisions and bounds both simultaneous contributions, plus the bend chord
  contribution; its existing span checks retain endpoint/stub coverage. Palette and physical dimensions are unchanged.
- **Joints:** the tighter existing straight-joint/analytic seam checks remain at 10 µm plus 0.5 mm resting penetration. Production
  snapped straight–bend and funnel–bend mouth crossings additionally check the unchanged 5 mm surface-to-art limit.
- **Dynamic loading:** impacts and centripetal loading retain the separate temporary penetration envelope (5% of smaller extent)
  and existing energy bounds. These do not enlarge either the resting or art bound. Fixed box/cylinder candidate searches still
  require completeness controls; an exact signed distance by itself does not prove a complete manifold.

## Behaviour

A payload contacts the outer shell, the bore wall and the end rims through the same speculative-contact pass as every other shape
(margin |v|·dt + slop, [envelope](gpu-f32-physics.md#game-grade-envelope)): a ball rolls or falls through a tube, follows a bend on its
outer wall, leaves a funnel's throat, is deflected by a rim or collar it strikes, and stops on a collar or ring at its drawn surface.
Resting penetration stays within contact slop; it never faults a tick.

## Chrome-observable acceptance (CAT-048)

- `clear_pipe` is solved through the UI, including fast and edge entries; the visible pipe pose matches the physical body; no
  teleportation or scripted routing.
- Controls: an oversize payload jams; a ball aimed beside the opening misses; an initially overlapping placement rejects at compile.
- Exact Reset and Save/Load after every Run; same construction and inputs give the same outcome.
- `rg -i annular engine/` finds nothing.

Before the post-catalogue E2E phase these cases are proved natively against the production simulation; the Chrome runs are queued for
that phase.
