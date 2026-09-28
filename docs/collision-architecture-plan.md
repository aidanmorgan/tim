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

**Not yet implemented:** friction blocks, joint assembly/position stabilization, penetration/manifolds, persistent world state/integration, complete compound builders, catalogue/world cutover and obsolete-code removal. No gameplay part is newly marked complete from these solver-unit tests.
