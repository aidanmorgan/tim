# Hollow-funnel collision foundation

Status: **native geometry foundation only; not connected to hinge flight and not a completed part proof**.

The lever still ignores declared frustum shells in `WorldHinges`; separately declared straight collars retain their existing tube collision. This increment supplies the static query and conservative clearance needed for a continuous rotating-beam query. It does not change player-visible physics, geometry, colours or controls.

## Geometry

`FrustumBoxIntersection` works in the frustum's local X-axis frame. Clip the box against the finite axial slab before testing its hollow shell. With slope `k = (outlet-inlet)/(2L)`, intercept `a = (inlet+outlet)/2` and radial distance `r`, a shell point satisfies:

- `-L <= x <= L`;
- `a <= r-k*x <= a+thickness`.

The clipped box is convex and connected, so the continuous function `f=r-k*x` has an interval image. Its maximum is at a clipped vertex. Its minimum occurs at a vertex, along an edge, or on the radial axis. At a smooth stationary point inside a planar face, the direction `(1,k*y/r,k*z/r)` is tangent to that face and preserves `f` until it reaches an edge or the axis. Thus a face-interior minimizer needs no separate face solver.

The implementation enumerates all clipped vertex pairs: this includes all clipped edges, while extra chords stay inside the convex box and cannot produce an outside witness. A segment's radial norm minus its linear axial term has an analytic minimum, including a nonsmooth axis crossing. A separate box/axis interval check covers axis minima inside faces or the volume.

For expansion by `delta`, the axial bounds expand by `delta` and both intercept bounds by `delta*sqrt(1+k*k)`. These are the respective Lipschitz constants, so the expansion contains Euclidean dilation. Bisection returns a known-disjoint expansion within 0.000003125 of first intersection; a positive value is a conservative clearance bound. Negative values describe erosion, not a collision normal or a minimum translation vector. The query uses finite floating-point arithmetic, not exact arithmetic.

Research: David Eberly's [Intersection of a Box and a Cone or Cone Frustum](https://www.geometrictools.com/Documentation/IntersectionBoxCone.pdf) describes axial clipping and an axis/edge search for solid-cone intersection. It supports the geometric approach, but does not prove this implementation's annular interval extension, clearance bound or future rotational sweep. The derivation above and tests below address this project's hollow shape.

## Verification

Nineteen native cases cover:

- bore, shell, inlet/outlet annular rims and open holes, beyond-end separation, surrounding box and coupled axial/radial miss;
- the same arrangements under a common arbitrary 3D rigid transform;
- independently calculated sloping-normal clearance and bore clearance;
- an edge-interior extremum that vertex-only tests miss, plus a nearby separated control;
- a broad box face pierced by the shell without any box vertex in the solid cone;
- 200 seeded equal-radius comparisons against the independently implemented cylinder query, including clearance;
- 200 seeded tapered configurations with independent `FrustumProxy.Surface` interior witnesses and expansion bracketing;
- invalid dimensions, non-finite inputs and non-rigid transforms.

Interior sampling can expose missed intersections; it is not exhaustive proof of all contacts. Cylinder agreement tests the zero-slope limit, not arbitrary taper correctness by itself.

Commands: `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`, production `dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet`, and `git diff --check`. The full native suite has 1,340 passing cases. No failed geometry test was observed in this increment.

No new UI evidence is claimed: this helper is not yet used by gameplay. Prior tube evidence remains historical proof for the unchanged tube implementation.

## Required next work

- Implement the continuous rotational query using slope-aware clearance; test high-speed intermediate hits, tangent approach/release, inner wall and both end rims.
- Integrate declared frustums into the typed hinge-obstacle dispatch and initial placement validation.
- Prove shell blocking and open-bore travel with native world tests, meaningful missed controls, passivity and exact Reset.
- Construct corresponding actual-UI Playwright cases, inspect actual placements, retain failures, review continuous motion and recheck production output before claiming funnel/lever interaction works.
- Keep bend, moving-fixture, beam-to-beam, fulcrum, rope, guided-body, broader load, campaign and mobile requirements open.
