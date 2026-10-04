# Hollow geometry

Use declared body-local compounds in the sole [WGSL f16 world](gpu-f16-physics.md). Straight tubes and frusta use local X. Bend centrelines follow R × (sin(a), cos(a), 0), for a in [0, sweep]. The bore and end openings remain physically open; a filled convex hull is not an admissible substitute.

Declare inner/outer radii, length or centreline radius/sweep, material and explicit surface-error and child budgets independent of difficulty. Reject unsupported dimensions, self-intersection, exhausted child budgets or unrepresentable geometry rather than silently downgrading.

For n angular cells with d = π/n, inner vertices lie at the declared bore radius and outer vertices at outerRadius/cos(d). Each annular trapezoid conservatively contains its ideal sector; ring excess is bounded by outerRadius × (sec(d) − 1). Linear interpolation of endpoint sections builds straight/frustum cells with open end planes.

For a bend cell with half angle h, S = 2 sin²(h/2), and op = outerRadius/cos(d), a rounded endpoint hull covers the curved shell. Its geometric excess is bounded by ringError + (2R + outerRadius + op) × S. Declare the endpoint extension and complete error, including the current f16 arithmetic/local-coordinate allowance. Minimum admitted bore subtracts the whole error. Increasing accuracy increases subdivision, not physical response or invisible wall removal. These analytical geometric bounds require independent numeric qualification; library trigonometry is not assumed formally interval-certified.

Child origins and vertices retain canonical coordinates through declared adapters. Swept bounds use child support bounds, signed translation and conservative full angular displacement, including interior extrema and multiple turns. All contact, query, free-flight and correction paths share one signed separation/witness contract.

Qualify spheres, boxes and compounds against outer shell, bore and end rims; axial/coaxial and tangential motion; rotating/translating geometry; tube/frustum/bend junctions; blocked/missed and initial-overlap controls; corrections that encounter new obstacles; exact rollback and Reset. Preserve deterministic child ordering, authored pipe/collar dimensions and concentric rounded-witness/point-contact controls. Verify actual ball passage, jam, gravity/momentum and visible pose through UI, with no teleportation or scripted routing.
