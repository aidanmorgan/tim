# General collision and constraint architecture — execution plan

Status: **executing; replacement is not yet complete**. Requested after the shape-pair specialisation in the lever work. Baseline: `35acec8`, 1,359 native tests and recorded actual-UI proofs.

## Required outcome

Adding a puzzle part must declare geometry, material, mass/inertia, motion constraints and component-specific forces/events. It must not add a part-pair or shape-pair collision solver. One world owns broad-phase candidate selection, narrow-phase contacts, continuous translation/rotation, contact response and joint/rope constraints. Preserve the scene graph, C# browser deployment, current visual style, author-controlled assistance, exact construction Reset and deterministic same-build replay.

No optional legacy solver, fallback routing, compatibility aliases or hidden old/new runtime selection. Existing specialised production paths and their old solver implementations are removed at their forward cutover. Preserve independent physical requirements and analytic expectations in migrated tests, not copies of the removed algorithms. Do not weaken tolerances or silently change hollow passages to filled hulls to obtain a green test suite.

## Execution sequence and acceptance gates

1. **Qualify the existing backend before selecting it.** Inspect pinned Godot/2dog APIs; run executable native and browser probes for general convex queries, compound hollow geometry, contact output, high-speed translation, pure rotation, moving/moving contact and joint/constraint support. Distinguish API presence, runtime operation and behavioural coverage. Record limitations rather than infer complete CCD from a translation-only query.
2. **Select and document one backend.** Prefer the packaged Godot physics facilities if they meet the measured requirements. If not, justify a general C# support-mapped convex query/solver, not another family of handwritten pair equations. A missing API is not by itself proof that the backend's simulation cannot meet the requirement.
3. **Introduce typed declarations.** Collider/body identities, body motion type, collision layers, material, compound children, joint axes/limits and event roles must be compiler-checked. Separate geometry from response and puzzle behaviours. Give every shape builder explicit units, finite-input validation and deterministic ordering.
4. **Represent hollow shapes generically.** Build bounded-error compound convex geometry (or another qualified general concave representation) for tubes, bends and funnels. Quantify surface and clearance error, seam behaviour, bore openness and performance at all authored dimensions. Representation accuracy is independent of player difficulty. No per-pair dispatch for new part types.
5. **Implement one continuous contact pipeline.** Both bodies contribute linear/angular motion; preserve the earliest-contact clock, manifold identity, stable resting contacts, non-penetration and explicit failure on exhausted numerical budgets. Rotation-only thin-wall and clear-endpoint/intermediate-hit tests are mandatory.
6. **Unify response and constraints.** Use shared mass/inertia and Jacobian-based contacts, friction, hinge/slider limits, compliant elements and rope endpoints. No separate canned lever response or family-specific bypass. Existing powered effects remain explicit bounded force/impulse/energy contracts.
7. **Forward-cut over the current catalogue.** Update declarations, scene graph integration, world stepping, queries, authoring, save/Reset and current callers together. Remove superseded production collision code. Fail explicitly for unsupported declarations; no silent ignored proxies.
8. **Prove and publish the replacement.** Run native analytic/property/regression tests and real-UI Playwright positive/negative/integration cases for every affected part/mode, including rotated/hollow/compound geometry, moving obstacles, two moving mechanisms, ropes, load/friction, Reset and continuous desktop/mobile motion. Build production and verify deployment. Commit/push verified increments; do not call an entire phase complete from a narrower test.

## Evidence and regression contract

- Preserve current results as historical baselines, not a promise of bit-identical trajectories across an intentional solver replacement.
- Establish explicit physical error bounds before changing an assertion. Account for any trajectory change in puzzle authoring and difficulty assistance; do not simply regenerate expected success.
- Keep failures and source revisions/hashes. Existing simple analytic primitives can serve as independent test oracles, never a runtime fallback.
- Benchmark catalogue-sized scenes on the single-threaded browser host. Measure narrow-phase work, allocations, frame time and contact stability.
- Passing a backend probe is not a part's actual-UI behavioural proof.
- Completion requires one production pipeline, removal of obsolete paths and every affected part's fresh evidence. This refactor does not drop any component, campaign (75 levels), mobile or difficulty requirement from TODO.

## Initial inspection

The pinned host uses `2dog.engine 4.7.2.91`, `2dog.browser-wasm 4.7.2.6` and Godot 4.7.2. `MachineWorld.Step` currently runs custom substeps and specialised sphere/hinge paths; it does not delegate the game to Godot rigid-body simulation.

The packaged C# bindings expose convex shapes, compound bodies, space queries, body motion tests and joints. Godot's [direct-space API](https://docs.godotengine.org/en/stable/classes/class_physicsdirectspacestate3d.html) documents translation casts separately from static overlap queries; overlap queries ignore motion and casts ignore initial overlaps. These facts require explicit tests rather than assuming arbitrary rigid-motion coverage.

The [2dog browser host](https://2dog.dev/hosts/web) embeds Godot and C# in a single-threaded WebAssembly runtime and does not load native GDExtension side modules. Qualification therefore targets the installed build, not a presumed external native physics plugin. No package upgrade or backend substitution has been made.

## Progress

- [x] Inspect current runtime architecture and pinned API surface.
- [ ] Execute native/browser backend qualification and record the selection decision.
- [ ] Typed geometry/body/constraint declarations and bounded-error hollow geometry.
- [ ] General continuous contact pipeline and shared constraint response.
- [ ] Forward catalogue/world/query cutover and obsolete-code removal.
- [ ] Complete affected-part Playwright/native/build/mobile evidence and deployment.

## Executed increment: general query foundation

Native suite: **1,400 passed, zero failures/skips** (baseline 1,359 + 33 generic geometry/motion tests + 8 packaged-backend qualification cases). The three backend simulation cases deliberately record observations; passing those recording tests does **not** mean their physical behaviour meets the replacement requirements.

The installed backend's native and browser results agree:

| Probe | Observed result |
| --- | --- |
| Sphere, box, hull translation into wall | Contact, safe fraction 0.44140625 |
| Compound open passage / wall | Clear / contact at fraction 0.2890625 |
| 1,000-unit/s translation with CCD | Stopped at wall |
| Pure rotation, 120 rad/s, obstacle at 0.4 rad | Passed through: angles 1 then 2 rad, zero recorded contacts |
| Symmetric opposing bodies with CCD | Asymmetric trajectory; needs investigation before acceptance |

**Decision for this increment:** unmodified packaged simulation is not qualified as the replacement. Implement and qualify a shared C# support-mapped collision pipeline. This is not a claim that every Godot configuration or alternative library fails. The replacement backend is the general C# pipeline. Packaged joint behaviour remains unqualified and is not relied on; the C# response layer must satisfy the remaining gates before cutover.

Implemented in `engine/physics/`: immutable sphere/box/hull declarations, double-precision GJK distance bounds and witnesses, conservative rigid-motion sweeps including both bodies' angular motion, typed compound child identities, and conservative swept bounds. A test-only capsule needs only a support function, with no new pair algorithm. Numerical exhaustion throws rather than reporting clear. Initial overlap is explicitly not a penetration-depth result.

Browser probes of the new pipeline:
- Rotation collision at 0.002702584758762334 seconds (12 distance queries).
- Opposing spheres contact at 0.0037498749999999997 seconds.
- Compound passage remains clear; wall contact at 0.2916499999975121 seconds.
- No browser console errors.

[Recorded browser results and UI frames](general-collision-qualification-results.json) and [read-only browser probe recipe](general-collision-qualification.playwright.js) retain the observations. Build diagnostics with `dotnet publish CuriousContraptions.web -p:PlaytestDiagnostics=true --no-restore`, serve through the existing Preview host at port 8060, then execute the recipe with Playwright. Diagnostic probes use isolated physics spaces, not player-world mutation.

Actual-UI regression: replayed the existing lever/funnel inner-bore recipe from [its evidence manifest](impact-lever-frustum-recipes.json), using the same palette/3D-handle adapter. The new attempt is `general-collision-foundation-ui-regression-v1`. All **101 sampled frames match the prior baseline at matching ticks**; beam stop remains 0.35652214814725963 rad. Run/Reset construction snapshots match exactly, with no browser errors. Reviewed the holding screenshot. This establishes that diagnostic startup did not alter that existing behaviour; it is **not gameplay proof of the new solver**, which is not connected to the world yet.

Production Release publish with `PlaytestDiagnostics=false` succeeded; probe startup is conditional and probe classes are excluded. The new query core is not a selectable legacy/new runtime mode.

### Remaining before gameplay cutover

- Penetration/manifolds, persistent contacts, robust degeneracy/scale stress tests.
- Shared contact/friction/inertia and hinge/slider/rope response.
- Bounded-error tube, bend and funnel compound builders; seam/clearance/performance evidence.
- Catalogue declarations, world stepping and all callers forward-refactored together; remove old specialised runtime solvers.
- Fresh affected-part positive/negative/integration/Reset evidence, mobile motion/performance and deployment verification.

Do not mark the full architecture or any unfinished part complete from this increment.

## Full-replacement continuation: shared impulse response

The active objective is the **entire engine replacement**, not delivery of the query foundation. Completion requires removal of the old collision/response code and forward migration of every caller, without shims, runtime selection or compatibility support.

Implemented the shared response primitives in `ImpulseBody.cs` and `ImpulseConstraint.cs`:
- Typed body identities and static/kinematic/dynamic motion; finite, positive-definite full 3D inertia.
- One bounded Jacobian row for contacts, locks, limits, motors and compliant velocity constraints.
- Accumulated-impulse clamping, restitution captured once, coupled iteration with a measured residual, explicit failure for unsolvable/nonconvergent constraints.
- Both body velocity updates validated before committing an impulse.
- No part-type or shape-type response dispatch.

The formulation uses effective mass and accumulated impulses as described in [Catto's constraint formulation](https://box2d.org/files/ErinCatto_ModelingAndSolvingConstraints_GDC2009.pdf) and [Box2D's solver discussion](https://box2d.org/posts/2024/02/solver2d/). This implementation is 3D; these references do not prove its correctness.

Fourteen focused native cases pass, covering elastic/inelastic momentum, off-centre angular momentum and energy, anisotropic inertia, moving kinematic surfaces, separating contacts, a coupled hinge/contact system, simultaneous contacts, accumulated-impulse reduction, bounded motors, softness and explicit invalid/nonconvergent failures. [Browser qualification](general-impulse-qualification-results.json) passes the same elastic and coupled-hinge equations: elastic velocities -1.8 and 2.2; coupled solve converges in eight iterations with residual 3.52e-9 or less. The [Playwright recipe](general-impulse-qualification.playwright.js) asserts these independently computed values. The full suite passed 1,413 cases before the additional non-finite-residual guard test; production Release publish also passed. The final focused run passed all 14 response tests, including that guard. These are isolated solver probes, not part behavioural proof.

**Remaining at that increment (superseded by subsequent progress below):** friction blocks, joint assembly/position stabilization, penetration/manifolds, persistent world state/integration, complete compound builders, catalogue/world cutover and obsolete-code removal. No gameplay part is newly marked complete from these solver-unit tests.

## Full-replacement continuation: friction and joint assembly

Implemented:
- A shared constraint interface for scalar rows, coupled joint blocks and contact friction.
- A full 2x2 tangent-mass solve constrained to a circular Coulomb disk, including off-centre angular response and changing normal-impulse budgets.
- Double-precision rigid rotations and world-space joint frames.
- Ball-socket, hinge and slider assembly; bilateral blocks use diagonally scaled Cholesky factorization instead of iterating their internal equations independently.
- Unilateral coordinate limits and a two-endpoint tension-only rope equation. Slider equations include rotation of the carrier's axis at displaced anchors.
- Explicit rejection of singular blocks, invalid frames and undefined coincident-endpoint rope gradients. No diagonal regularization or alternate solver is used.

Focused verification: **45 response/constraint cases pass**, comprising the existing 14 impulse cases, 10 friction cases, 16 joint/rotation cases and 5 block-solver cases. Tests include arbitrary hinge orientation, half-turn alignment, rotating carriers, offsets up to 100 units, momentum, slip/stick, direction invariance, decreasing friction budgets and coupled hinge/friction contact.

A scalar-row offset-joint case initially exhausted 256 iterations. This failure is retained in [the evidence record](general-constraint-qualification-results.json); introducing a coupled matrix block resolved it without raising the iteration budget. The offset-anchor correction assertion was also corrected to include angular point velocity.

[Browser probes](general-constraint-qualification.playwright.js) pass:
- Diagonal sliding ends at velocity (1.8, 0, 2.4), friction impulse (-0.6, 0, -0.8).
- A rotating slider carrier gives its load velocity (3, 0, 0) and angular velocity (0, 1, 0).
- The offset joint retains only axial velocity (0, 0, 1).
- All three converge in one outer iteration, with maximum residual below 5e-15 and no browser errors.

The full native suite passes **1,445 tests** with no failures/skips, and the production Release publish with diagnostics disabled succeeds.

These are instantaneous constraint equations, not a completed dynamics engine. Still required: persistent body/joint state, inertia updates with pose, integration and split positional correction, complete rope routing/multiple endpoints, limit event timing, penetration/manifolds, compound hollow geometry, catalogue/world cutover, removal of every old solver path, and fresh per-part UI evidence. The current runtime still uses the old solver and must not be called migrated.

## Full-replacement continuation: authoritative rigid body state

Forward-refactored the new solver's temporary `ImpulseBody` into `PhysicsBody`, migrated every production/test/diagnostic caller, and deleted the old class and script identity. There is no alias, overload retaining the old constructor, or body synchronization adapter. Constructors now require an explicit `RigidPose`.

The body owns pose, linear velocity and world angular momentum. Body-local inertia is rotated with pose; force/torque kicks and off-centre impulses act on momentum directly. Torque-free orientation uses bounded Lie-midpoint integration, with explicit failure on exhausted budgets. Static, kinematic and dynamic motion remain enum-typed. Snapshot restoration is validated before mutation, and pose revisions invalidate stale constraint geometry.

**55 focused solver/body tests pass**, including ten new body tests: rotated inertia, force/torque integration, ten-second free rotation, time reversal, exact restore/replay, motion-type contracts, stale constraints and atomic failure. A constructor-edit compile failure was fixed by correcting its caller, not adding a compatibility signature; retained in [the evidence record](general-body-qualification-results.json).

The [browser recipe](general-body-qualification.playwright.js) verifies 4,800 steps over ten seconds, exact world angular momentum, exact restore and replay, and maximum relative kinetic-energy error **2.074595161929313e-7** for the tested anisotropic free body. No browser errors were recorded. This is one measured integration case, not a general energy-preservation guarantee or gameplay part proof.

The full native suite passes **1,455 tests**, and production Release publication with diagnostics disabled succeeds.

**Next integration requirement:** the collision trajectory and the body's advance trajectory must agree. Existing constant-spin sweeps must not be used to certify a different torque-free rotating trajectory. Implement shared trajectory evaluation/bounds before world cutover. Still outstanding are penetration/manifolds, split positional correction, persistent joint/rope ownership, hollow catalogue geometry, world/caller migration, deletion of the original gameplay solver and fresh per-part UI proof.

## Full-replacement continuation: shared collision and advancement trajectory

`BodyTrajectory` now captures the immutable free-flight path once. Dynamic rotation is integrated into bounded Lie-midpoint segments; each segment is evaluated as a constant angular-velocity exponential. The maximum segment spin is the rotation-speed bound used by conservative advancement. Linear motion is identical in query and commit. Multi-turn kinematic motion retains its full spin rather than interpolating between endpoint orientations.

`PhysicsBody.Advance` now requires the captured path and elapsed time; the duration-only API and separate internal drift implementation are removed. `ConvexMotion` and `CompoundMotion` consume that same path; their old pivot/velocity constructors and independent rotation evaluator are removed. All callers are forward-migrated. Source pose/velocity/momentum changes invalidate a path before commit.

Seven new native cases pass: exact query/commit pose identity; invalidated paths after an impulse; angular-speed bounds across segment boundaries; an anisotropic intermediate collision with both endpoints clear; compound child identity on that path; multi-turn rotation; and atomic rejection of invalid times. Existing sweep/compound/body tests also pass. The rotation test no longer depends on an old production collision algorithm: its independent expected angle comes from the fixture's side-projection equation.

Local collider declarations still use their existing validated Godot transforms at the declaration boundary. Time-varying body poses are evaluated in double precision and are not converted to float at each sweep trial.

Still incomplete: penetration/contact manifolds, stable resting/releasing contact handling, split positional correction, persistent joint and multi-endpoint rope ownership, catalogue hollow builders, full world migration, removal of the original gameplay solver and per-part UI re-verification. This increment does not claim any of those are complete.

[Browser verification](general-trajectory-qualification-results.json) finds the anisotropic contact at **0.1132580265338625 s** along a 55-segment path. Both endpoint separations exceed 1.12 units, the reported contact separation is 0.00010004424744187166, and the committed pose equals the queried pose exactly. The [Playwright recipe](general-trajectory-qualification.playwright.js) asserts those conditions and no browser errors. The full native suite passes **1,462 tests**, and the diagnostics-disabled production Release publish succeeds. This is engine-path proof, not per-part gameplay proof.

## Full-replacement continuation: bounded penetration queries

Added `ConvexPenetration`: a support-mapped expanding-polytope query with lower/upper depth bounds, separating normals and convex witness pairs. It follows the general approach described in [van den Bergen's proximity/penetration paper](https://graphics.stanford.edu/courses/cs468-01-fall/Papers/van-den-bergen.pdf), rather than dispatching by shape pair. Hull expansion, coplanar witness selection and exhausted budgets have explicit validation.

All support shapes now declare an `InteriorBall`. The Minkowski sum of these balls supplies a geometric lower bound; if a support plane meets that bound, the result is already certified. This handles concentric spheres efficiently without a sphere-pair branch. Boxes declare their inscribed radius, hulls a guaranteed interior convex-combination point, and transforms propagate a conservative radius accounting for float-basis roundoff.

Verification covers every ordered sphere/box/hull pair, 200 analytic sphere cases, 500 randomly rotated box cases against an independent separating-axis calculation, translating by the reported separating vector, touching surfaces, lower-dimensional hulls and invalid bounds. Flat/point geometry is not given artificial thickness; coincident points explicitly have no unique normal. The inscribed-ball bounds do not make arbitrary-hull queries approximate or switch algorithms.

This provides penetration depth and one witness pair, **not a stable multi-point contact manifold**. Still required: support-feature extraction and contact-patch clipping, manifold persistence, resting/releasing contacts, position correction, hollow catalogue geometry, world/caller cutover, old-solver deletion and affected-part UI proof.

[Browser verification](general-penetration-qualification-results.json) reports sphere depth **1.1417424236758462** and box/hull depth **1.5999999940395355**, with zero witness error in all three cases and no browser errors. The [Playwright recipe](general-penetration-qualification.playwright.js) checks analytic depths and bounded results through read-only diagnostics. The full native suite passes **1,481 tests**, and the diagnostics-disabled production Release publish succeeds. These checks qualify the query, not gameplay parts or the world cutover.

## Full-replacement continuation: generic contact patches

Added mandatory supporting-feature declarations to convex geometry, with typed vertex identities. Sphere, box, hull and the test capsule describe their own geometry; the query never dispatches on a shape pair. Instance and trajectory transforms propagate features through the same poses as distance/sweep queries.

`ContactPatch` projects supporting features into one tangent plane, computes their convex intersection, and reconstructs paired anchors using nonnegative convex weights. Point, edge, face, collinear and disjoint cases share this algorithm. It retains the complete patch (including eight corners for overlapping rotated squares), not an arbitrary single centre point. Anchors remain in the convex input geometry; no artificial thickness is added.

`ContactManifold.Query` connects distance/penetration bounds to that patch. Both certified query witnesses participate in the features on every query, alongside support vertices: this accounts for finite normal accuracy on curved shapes without a shape-specific path or empty-patch substitution. Undefined normals and inconsistent bounds fail explicitly. Query-local witness vertex IDs are not persistent contact identities.

The feature approach is informed by [Catto's contact-manifold presentation](https://box2d.org/files/ErinCatto_ContactManifolds_GDC2007.pdf); this implementation uses a shared 3D tangent-plane intersection, not Box2D's pair-specific collision code.

Focused verification covers 400 analytic rectangle intersections, 200 randomized rotated box contacts, 300 mixed sphere/box/hull configurations, all nine ordered shape pairs, contact skin, common rigid transforms, feature identities, degenerate features and invalid inputs. A four-corner box impact feeds the existing generic impulse solver and verifies no residual translation or spin above 1e-8.

Retained development failures: the first test compile required explicit element types for three single-element params calls. Random seeds 83 and 142 then exposed nearly collinear triangle winding and reconstruction errors. The projection algorithm now evaluates feasible triangle/segment/vertex combinations and certifies the reconstruction error before accepting a point; it does not enlarge the tolerance or substitute a different collision path.

Still incomplete: contact persistence/warm starting, resting/releasing time advancement, split position correction, persistent joint/rope ownership, hollow catalogue builders, full gameplay/world cutover, old-solver deletion and new per-part UI proof. This increment establishes instantaneous manifolds, not a completed resting-contact simulation.

[Browser evidence](general-manifold-qualification-results.json) and the [reproducible recipe](general-manifold-qualification.playwright.js) verify four-corner and eight-corner impact patches, the analytic clipped coordinates, residual linear/angular motion below 1e-8, a clear control with unchanged velocity, exact body snapshot restore and exact response replay. No browser errors were recorded. All **1,497 native tests** pass. These are engine diagnostics, not UI construction/Reset evidence for any gameplay part.

The diagnostics-disabled production Release publish also succeeds. Domain selectors remain enum-typed; supporting vertices use typed indices, and string conversion occurs only at the diagnostic JSON boundary.

## Full-replacement continuation: persistent contact state

`PersistentContactPair` now owns the contact state of one immutable convex-child pair. Each step queries fresh geometry, matches both body-local anchors and the normal one-to-one, retains typed contact IDs, and sorts by those IDs to keep solver ordering stable. Changed normals or unmatched anchors receive new IDs and no borrowed impulse. A clear manifold removes the episode's cached contacts.

The explicit enum lifecycle is prepare, warm start, global solve, complete. All pairs must be prepared before any warm start so restitution observes pre-solve velocities. Restitution is applied at the beginning of a contact episode, not to every newly clipped corner. Normal and body-local tangent impulses are cached; timestep changes scale the force estimate. The current friction disk bounds the warm-start estimate, and accumulated-impulse solving can retract it. Duplicate initialization, stale poses, invalid lifecycle calls and unsolved cache commits are rejected.

Immutable cache snapshots include duration and the next contact ID. Both bodies must be restored before their cache; an owner mismatch is rejected. Restoring discards any prepared frame. This is in-memory physics reset state, not a legacy save-format adapter.

The warm-start/accumulated-impulse approach is informed by [Catto's Solver2D discussion](https://box2d.org/posts/2024/02/solver2d/). It accelerates convergence; it does not replace fresh contact geometry or certify free motion.

Thirteen focused native tests cover 600 gravity-supported steps, a 240-step coupled two-body stack, friction-disk projection and impulse retraction, timestep scaling, stable one-to-one IDs under common motion, normal-change invalidation, release/re-contact, one restitution event, invalid/stale lifecycle calls, and exact cache/body restore/replay.

Retained failures and corrections:

- Fresh geometric ordering permuted the same four IDs. Prepared rows now use stable ID ordering.
- A common rigid transform exposed a redundant fifth point on a flat contact edge. Supporting features and the resulting contact boundary now remove a sample only when every original vertex on the replaced arc lies within the existing tolerance of its retained chord. Paired contact anchors are checked on both bodies, preventing cumulative simplification error. This does not enlarge collision tolerances.
- The first stack test compared absolute body speed to twice the default per-contact residual and observed 2.1502622028588688e-8. Those are different quantities. The stack qualification explicitly requests a tighter 1e-10 relative-contact residual (same iteration budget), rather than weakening its absolute-speed assertion. Single-body support retains the default 1e-8 residual.

Still incomplete: a world-owned contact graph and contact-aware continuous time advancement (including rotational release/re-contact), split positional correction, persistent joints and routed ropes, hollow catalogue builders, gameplay/caller migration, original-solver deletion and affected-part real-UI proof. The support loops here are controlled engine fixtures, not a collision-complete world integrator.

[Browser verification](general-persistence-qualification-results.json), using the [recorded Playwright recipe](general-persistence-qualification.playwright.js), reports:

- Single support: 600 steps, maximum drift 6.76941855415641e-11, maximum speed 3.5075879511508064e-9; cold solve 21 iterations and later solves at most one.
- Two-body stack: 240 steps, maximum drift 7.086187190884336e-12, maximum speed 2.0888887740104901e-10; cold solve 214 iterations and later solves at most nine.
- Stable IDs, exact body/cache restore, exact 20-step replay and release-cleared caches in both fixtures; no browser errors.
- The four/eight-corner manifold and clear-control browser regressions also pass after boundary simplification.

The cold stack's 214 iterations are close to the 256-iteration budget: larger stacks and coupled-scene performance remain unqualified and must be tested before full-engine completion. All **1,510 native tests** pass, and the diagnostics-disabled production Release publish succeeds. Lifecycle/status/probe values remain enums end-to-end; point identities are typed indices, and serialization strings are confined to the external diagnostic boundary.

## Full-replacement continuation: signed continuous clearance

Forward-refactored convex and compound sweeps to require an explicit `minimumSeparation`; the previous three-argument API is removed and every current caller is migrated. Results carry a signed separation interval. Distance and penetration queries remain distinct internally, so touching GJK output is never treated as a penetration-depth answer.

Each sweep interval is certified by a fixed supporting plane. Relative linear velocity is projected onto that plane; both bodies' bounded rotational travel supplies the remaining possible closing motion. A non-closing plane certifies the remaining horizon. Otherwise the interval ends before its lower bound can cross the requested separation. The approach follows the conservative-advancement principle in [Catto's continuous-collision presentation](https://box2d.org/files/ErinCatto_ContinuousCollision_GDC2013.pdf), extended here to signed targets and the shared body trajectory.

Geometry now declares its exact Minkowski rounding radius. The residual core radius bounds orientation-dependent support motion. A centred sphere therefore has zero rotational reach, while an offset sphere retains its orbital reach. Float declaration bases are not treated as perfectly orthogonal: the rounded term's bound includes their eigenvalue-range distortion. The broad phase still uses the full enclosing radius, not the reduced rotational radius. No shape-pair dispatch is introduced.

The new fixtures explicitly request a -1e-6 separation boundary with the existing 1e-7 event tolerance. That is a measured query boundary, not a difficulty setting or a hidden gameplay relaxation. First-contact callers still explicitly request their original positive contact skin. Negative-boundary sweeps do not prevent all overlap; a world integrator must pair them with position correction and enforce its stated penetration budget. Undefined planes and exhausted precision/budgets fail explicitly.

Seventeen focused tests cover analytic signed sphere gaps; fast tangential sliding; a centred sphere spinning at 1,000,000 rad/s; an initially touching orbit that releases and later collides at three angular speeds; a grazing full revolution with no penetration event; compound child identity; 600 sampled rotational-support bounds; float-basis distortion; broad faces at four scales; and invalid declarations/thresholds.

Retained numerical failures: the wide-floor sphere fixture exhausted GJK's 256-iteration budget, initially at its start and then at the translated endpoint. Re-orthogonalized QR replaced normal-equation simplex projection. Endpoint diagnosis then showed witness subtraction corrupting the tiny tangential components of the search direction (upper distance 3.868844076175008e-7, lower zero). The search now uses the affine feature's geometric normal, while actual convex witness separation still certifies the upper bound. The current support plane must certify the lower bound; a lower bound from a different normal is no longer reused for termination. No tolerance or iteration-budget increase was used. A temporary diagnostic interpolation compile error was corrected and the verbose simplex dump removed.

Still outstanding: world contact graph/time-loop ownership, split position correction, joint/rope ownership, compound catalogue migration, gameplay cutover, removal of all original solver code and current real-UI part proof. These are certified sweep primitives, not yet the completed world step.

[Browser evidence](general-signed-sweep-qualification-results.json) and the [recipe](general-signed-sweep-qualification.playwright.js) verify one-iteration full-horizon certificates for spinning-sphere and box sliding, and orbit re-contact at **0.04601811294129047 s** (27 iterations). The full grazing revolution remains clear of the requested -1e-6 boundary, but costs **2,387 iterations**: many-pair scene performance remains unqualified. Queried/committed poses and restore/replay match exactly, with no browser errors.

The original general-sweep probes and the penetration, manifold, persistence and shared-trajectory browser recipes all pass again after the distance-kernel change. All **1,527 native tests** pass, and diagnostics-disabled production Release publication succeeds. Signed statuses and diagnostic selectors are enums; separation/radius quantities remain numeric, and string conversions remain at external diagnostic boundaries.

## Full-replacement continuation: shared world event loop

The replacement now has a `PhysicsWorld`: immutable body/compound declarations, deterministic child-pair ordering, persistent contacts, a shared continuous event clock and transactional step snapshots. Each substep applies gravity, projects penetrations independently of velocity, prepares every contact before warm starting, solves their shared velocity state, and advances **all** bodies to the earliest certified event. It then rebuilds all remaining trajectories from the resulting state. No part-pair dispatch or alternate solver is introduced.

New contacts use the positive contact skin; active pairs are swept against the explicit negative penetration budget. Active contacts are not simply omitted from continuous detection. A new swept impact ends the preceding support estimate without reusing contact identities. Both temporal stagnation and exhausted numerical/event/substep budgets throw. A failed world step restores all bodies, contact caches, impacts, clock and step index.

Position correction uses fresh contact manifolds and mass/inertia-weighted configuration impulses, with a new geometric query on each nonlinear iteration. It preserves physical linear velocity and world angular momentum rather than adding separation velocity. Reorienting an anisotropic inertia tensor can change kinetic energy; this is not a claim that arbitrary initial-overlap projection preserves energy. Gravity uses a fixed maximum-step semi-implicit kick, not exact parabolic flight.

Retained failure: the first rotating-kinematic-beam world test failed after 256 velocity iterations across two rows, with residual **2.63109300249198e-5** versus **1e-8**. Including both an approximate smooth supporting point and its refined witness had manufactured a tiny edge from normal-query uncertainty. The generic manifold now preserves a single-vertex supporting feature's dimension: its certified witness refines that point instead of extending it. Multi-vertex features retain clipping. No shape-pair branch, relaxed tolerance, increased iteration budget or dropped residual was used. A regression checks one-point output under both body orders; the rotating-beam world case now passes.

Seventeen added native cases cover thin-wall impact at 1,000 units/s, opposing dynamic bodies, clear controls, three impacts in one step, exact replay, transactional failure, analytic mass-weighted overlap correction, 600 resting world steps, rotating intermediate impact, empty compound passage versus typed child-wall hit, box overlap without artificial velocity, impossible immovable projection, invalid/unrepresentable steps, duplicate identities, foreign snapshots and impact-episode lifecycle. The complete suite passes **1,544 tests**, zero failures/skips.

Remaining world work is explicit: persistent joint/rope graph integration, collision filtering/events and mutable topology for catalogue spawning, bounded-error hollow builders, coupled-scene/performance qualification, gameplay/caller cutover, deletion of the original solver and fresh actual-UI proof for every affected part. Current broad phase checks all child pairs; no spatial tree/island or catalogue-scale performance claim is made. Static/kinematic pairs with no dynamic body are prescribed and do not exchange contact impulses. The original gameplay solver is still active; these engine fixtures do not complete any gameplay-part proof.

[Browser observations](general-world-qualification-results.json), using the [recorded recipe](general-world-qualification.playwright.js), pass the integrated thin-wall, three-bounce, 600-step support and rotating-beam fixtures with exact restore/replay and no browser errors. Shared manifold, persistence and signed-sweep browser regressions also pass.

The browser's 1,000-unit/s sphere-wall impact exposes a remaining anchor-precision issue: residual spin is **0.013327748504073852 rad/s**, although outgoing linear velocity is -999.9999999911186 units/s. The approximate distance witness introduces a small off-axis torque. This result is retained, not asserted to be a physically exact zero-spin collision. Generic contact-anchor refinement and quantified angular error remain required before full-engine completion. The test's current acceptance covers impact timing/response, not zero-spin analytic fidelity.

The diagnostics-disabled production Release publish succeeds. New world/probe/lifecycle choices remain enums, body/child/contact identities remain typed, and string conversion is confined to the diagnostic JSON boundary. This is verified progress toward the active replacement goal, not its completion.

## Full-replacement continuation: rounded-core contact anchors

The high-speed sphere-wall regression now asserts angular speed <=1e-8 rad/s. It failed the preceding implementation's approximately 0.0133 rad/s result. A first attempt to keep actual singleton support points fixed that central impact but failed both existing mixed-shape manifold seeds (83 and 142): normal uncertainty still left curved/flat supporting features without an intersection. Those failures are retained here; the fix was not accepted on the narrow central-impact test alone.

The generic manifold now queries signed separation of convex **cores**, then subtracts their declared Minkowski rounding radii and offsets the core witnesses onto the rounded surfaces. Single-vertex features retain their actual supporting surface point. Extended features still use certified witnesses and clipping. No shape-type inspection, pair registration, empty-patch fallback or increased tolerance is used. Radius zero follows the same equations for unrounded geometry.

Core-plus-radius representations are also described in the official [Box2D collision documentation](https://box2d.org/documentation/md_collision.html). Our implementation uses the existing general support-mapped GJK/EPA algorithms and remains a separate C# implementation, not a Box2D integration.

Float declaration bases retain their actual distortion. For local rounding radius r and singular-value bounds sigmaMin/sigmaMax, r*sigmaMin²/sigmaMax is a conservative lower bound on the transformed ball's minimum curvature radius. Removing that radius leaves a convex residual support function. An exact identity basis removes the full radius; non-orthogonal float roundoff stays in the residual core. The declared interior ball must contain the rounding ball so its contracted core interior certificate remains valid.

Eight new native cases cover 600 analytic randomly oriented sphere pairs, tangent/overlapping/separated capsule line contacts, 500 distorted-basis core-support checks, unchanged zero-radius polyhedral queries, and dynamic/dynamic momentum, energy and zero-spin response. Together with the strengthened high-speed test and existing mixed-shape regressions, the full suite passes **1,552 tests**, zero failures/skips.

[Browser refinement evidence](general-rounded-anchor-qualification-results.json), produced by the [new stricter recipe](general-rounded-anchor-qualification.playwright.js), reports exactly zero spin for the thin-wall and three-bounce sphere fixtures and 2.648813952269598e-13 rad/s for the rotating-beam sphere impact. Outgoing thin-wall velocity is exactly [-1000,0,0]. All fixtures retain exact restore/replay, with no browser errors. Manifold, persistent-support and signed-sweep browser regressions pass unchanged. Historical world evidence is preserved rather than overwritten.

This closes the specifically recorded anchor-precision defect, not all possible geometry/numerics qualification. World joint/rope integration, catalogue-scale performance, gameplay cutover, original-solver deletion and affected-part UI evidence remain incomplete.

The diagnostics-disabled Release publish succeeds. The refinement adds numeric geometry contracts, not string-based domain selectors; existing status/probe choices remain enums throughout their callers.

## Full-replacement continuation: world-owned joints

The world constructor now requires an explicit joint collection; the previous signature is removed and all current callers are migrated. Immutable body-local ball-socket, hinge, slider and endpoint-rope declarations have typed joint IDs and an explicit connected-body collision enum. The world validates exact body ownership, unique IDs and declarations, orders joints deterministically, and includes their fresh equations in the same global solve as contacts. A disabled collision declaration excludes that connected pair; unrelated contacts remain active.

Geometric joint equations are shared between velocity response and nonlinear position correction. The existing scaled Cholesky algebra has been extracted into one mass-matrix implementation, not copied into another joint solver. Position projection applies configuration changes directly and preserves linear velocity and world angular momentum. It does not create temporary bodies or overwrite physical velocities to run a pseudo-velocity solve. Slack ropes have no positional equality and cannot push. The world also solves terminal substep configuration/velocity state; snapshots restore bodies and contact caches exactly while the joint declarations themselves are immutable.

Retained integration failure: a ball hitting a hinged beam exhausted the unchanged 4,096-iteration continuous sweep budget after impact. A stationary-plane curvature bound alone also failed. The worst-case angular closing bound was forcing very short intervals while the contact face rotated away from that stationary plane. The correction adds **interval certificates**, not a touching-pair exclusion or a larger iteration budget:

- For each material-point pair, a bound M on the second derivative of its plane projection gives a lower bound of min(endpoint gaps) - M*h²/8 throughout the interval.
- The separating plane can be world-fixed or rotate with either body's captured path. For a plane fixed to A, the A-local support contribution is constant; the bound is omegaA²*D + 2*omegaA*relativeLinearSpeed + (omegaA+omegaB)²*reachB. D bounds the centre separation across the interval. B-fixed planes are symmetric.
- Certificates stop at both paths' constant-spin segment boundaries. No curvature certificate crosses a spin discontinuity. Existing first-order support-plane certificates remain valid mathematical bounds in the same sweep; no alternate collision solver is selected.
- Endpoint support gaps are necessary inputs to an interval bound, not an endpoint-only noncollision test.

Thirteen added native cases verify frame correction without injected velocity, a 240-step hinge pendulum with exact replay, slider/free-rotation degrees of freedom, slack/taut/overextended ropes, explicit collision policy, invalid ownership/identities, a coupled ball-to-hinged-beam impact and every constant-spin segment boundary. The full native suite passes **1,565 tests**, zero failures/skips.

Remaining requirements are not hidden: joint limits/motors and routed multi-endpoint ropes are not integrated; coincident endpoint gradients are explicitly rejected; joint impulse warm-start caches and catalogue-scale convergence/performance are not qualified. Free-flight intervals are certified, but nonlinear joint position projection is not yet a continuously swept constrained trajectory—large correction paths and thin-obstacle safety require further work before production cutover. Body/joint topology remains immutable per world. Gameplay still uses the original solver, which must be removed at catalogue/world migration, followed by fresh affected-part actual-UI proof.

[Browser joint evidence](general-joint-qualification-results.json), using the [recorded recipe](general-joint-qualification.playwright.js), passes all five fixtures with exact restore/replay and no browser errors:

- Hinge pendulum: maximum geometric error 9.670417688845134e-9 and velocity residual 8.881784197001252e-16 over 240 steps.
- Slider: final axial position 5.5125000000000215, velocity 7.000000000000043, zero transverse velocity/spin.
- Slack rope: free motion from 0.5 to 0.6000000000000023 at speed 0.1. Taut rope: stays at length 1 with zero outward velocity.
- Coupled beam impact: 11 continuous contact events / 158 sweep iterations across 12 substeps, beam spin -9.17490211446844 and payload vertical velocity -9.08245375479509; pivot error zero.
- Grazing-orbit regression now takes 20 sweep iterations (previously 2,387); release/recontact takes nine. Existing analytic event/clearance assertions still pass. Shared world, anisotropic-trajectory and constraint browser regressions pass.

Terminal contact solving changes the 600-step resting-world fixture's final drift to roughly 2.2e-8 and speed to roughly 1.18e-8, still within its existing acceptance bounds. This is retained in the report, not claimed bit-identical to the previous solver ordering. Cold/warm convergence costs need further qualification.

The diagnostics-disabled production Release publish succeeds. Joint kinds, connected-collision policies and plane-reference choices are enums; joint and body ownership uses typed identities throughout. No constructor compatibility overload or old/new solver switch was added.
