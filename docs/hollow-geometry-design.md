# Hollow geometry and shared-solver qualification

This increment supplies collision geometry and engine-level tests. It does **not** migrate production pipe/funnel/bend parts, replace the original gameplay solver, or qualify any part through gameplay UI.

## Declared geometry

Straight tubes and frusta use local X. Bends use centreline R*(sin(a),cos(a),0), for a in [0,sweep]. Each wall is a compound of convex cells; the bore is never filled by a convex hull. Error and child budgets are explicit, independent of difficulty. Unsupported dimensions, unresolved precision, exhausted child budgets and self-intersecting bends reject rather than downgrade.

For a cross-section with n angular cells, write d=pi/n. Inner vertices lie at the declared bore radius, and outer vertices at outerRadius/cos(d). The inner polygon lies inside the bore circle and the outer polygon circumscribes the outer circle. Each trapezoid therefore contains its ideal annular sector. Its excess is at most outerRadius*(sec(d)-1). Linear interpolation between endpoint cross-sections gives straight/frustum cells. Their end planes remain open.

For bends, let h be half a centreline angular cell and S=1-cos(h), evaluated without cancellation as 2*sin(h/2)^2. Let op=outerRadius/cos(d). Endpoint cross-sections form an eight-vertex hull. Every ideal shell point has a corresponding chord point within (R+outerRadius)*S, so rounding the hull by that radius covers the ideal curved shell. Conversely, every hull point differs radially from the interpolated trapezoid by at most (R+op)*S. The triangle inequality bounds excess after rounding by:

    ringError + (2*R + outerRadius + op)*S

The reported surface error adds a scale-dependent double-roundoff reserve. This is an analytic geometric bound with sampled floating-point qualification, not a formal interval-arithmetic proof of library trigonometry. Bend endpoint extension is the declared rounding radius plus reserve; callers must account for it. Minimum bore radius subtracts the complete reported error. Increasing requested accuracy increases subdivision, never changes response or hides the wall.

Child origins cross the scene's float-transform boundary explicitly. The exact converted origin is subtracted from double vertices, preserving their intended coordinates after translation. Swept bounds start from each child's support AABB, add signed translation and a conservative angular displacement bound; static children no longer inherit a large body-origin bounding sphere.

## Shared numerical corrections

All signed queries now remove exact Minkowski rounding, query core separation/penetration, and restore radii/witnesses analytically. The separate RoundedSeparation implementation is deleted. Contacts, free-flight sweeps and position constraints use the same API. A singleton Minkowski difference has non-unique supporting directions at coincidence; a deterministic unit axis is a valid choice, verified with concentric spheres and point contacts.

Conservative advancement reserves room inside the existing event tolerance instead of landing exactly on a forbidden separation boundary. This prevents arithmetic from turning a stopped correction into an initial-overlap rejection when it subsequently moves away. No production tolerance or iteration limit was increased.

Near-parallel contacts exposed slow scalar impulse convergence at a join. The shared iteration now performs bounded two-coordinate minimizations for coupled scalar rows before its ordinary row/block/friction sweep. For each pair, candidates include the free stationary point, stationary points on every finite bound edge, and coordinate stationary points covering rank-deficient solutions. The lowest feasible quadratic objective is selected. A mass-norm Schur complement avoids subtracting almost equal diagonal products. Both increments are combined by typed body identity and validated before any body is committed. This is regular block-coordinate descent, not a failure-triggered alternate solver. It applies equally to contact normals and other exposed bounded rows. Bilateral blocks continue to own and solve their complete equation sets.

## Verification and remaining limits

The [browser recipe](general-hollow-qualification.playwright.js) exercises axial clear/wall controls, both bend angles with curved passage and high-speed wall controls, and a joined-tube world with exact restore/replay. Query-only probes deliberately have no Reset assertion. Full evidence, retained failures and production-build results belong in [qualification results](general-hollow-qualification-results.json).

Native tests also sample material coverage/excess, bore clearance, deterministic child order, authored pipe/collar sizes, subdivision budgets, swept bounds, concentric rounded witnesses and bounded-pair complementarity. Near-parallel warm-start tests require a one-iteration coupled solution; a 400-case independent quadratic grid checks bounded solutions.

World pair topology is still eager, and coupled-row scheduling currently enumerates all pairs with nonzero mass coupling. Catalogue-scale broadphase, islands, memory and mobile performance remain unqualified. Probe timings include construction and replay and are not frame-rate measurements. Production geometry/query cutover, original solver deletion, actual-UI proof for every affected part, round-sheave ropes and the remaining campaign/component work are unfinished.
