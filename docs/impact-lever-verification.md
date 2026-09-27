# Impact lever — implementation and verification

Status: **in progress: catalogue scene, sphere-contact and fixed-box obstruction UI proof, not a fully verified component**. See the [fixed-box/workbench obstruction increment](impact-lever-obstruction-verification.md) for the latest tests, browser evidence and remaining collision coverage. The research TODO's “expand existing lever” wording did not correspond to an implemented lever in the current parts/catalogue. The work below is a prerequisite, not a substitute for the complete component.

## Fixed-axis groundwork

`engine/RevoluteJoint.cs` represents finite inertia, authored lower/upper angle limits, angular velocity and kinetic energy. World-space moment arms determine signed angular impulses and point velocities. Stop state and advance results use the `HingeLimit` enum. Free contact inverse mass is the squared normal moment arm divided by inertia.

The basis is [OpenStax's fixed-axis torque relationship](https://openstax.org/books/university-physics-volume-1/pages/10-7-newtons-second-law-for-rotation) and [rotational kinetic energy](https://openstax.org/books/university-physics-volume-1/pages/10-4-moment-of-inertia-and-rotational-kinetic-energy). This is a proposed physically constrained game model, not measured TIM fidelity.

`Advance` is force-free flight. It reports energy dissipated at an inelastic end stop and leaves the remaining time stationary. A future world caller must split at the earliest geometry contact or hinge limit; calling this method independently after body flight would not prove correct moving collisions. Outward angular impulse at a stop is absorbed; inward impulse releases it. Forces/contacts are not inferred from cargo proximity.

Fourteen native tests in `RevoluteJointTests.cs` verify:

- Signed moment arms; balanced mass × arm moments and an unbalanced shifted load.
- No rotation from axial, radial or pivot impulses.
- An analytically constructed single free contact transfers angular momentum and respects restitution without adding energy, across varied masses/arms.
- Rigid rotation of the entire setup preserves contact response and velocity.
- Both finite stops, dissipated energy, blocked outward impulse and inward release.
- Partition-independent force-free flight, exact authored-angle Reset, and invalid-input rejection without mutation.

The contact test calculates the unconstrained impulse in the test; it is **not** evidence of an implemented world contact solver. These checks do not establish static cargo balance, sliding, ball-to-ball transfer through a real beam, rope loading or UI behaviour.

## Verification run

On base revision `685fe05` plus this unused engine primitive/tests, all **1,207 native cases pass**, including the 14 focused hinge cases. The production Release browser publish passes. No existing simulation caller uses the hinge yet, so no browser behaviour or component proof is claimed.

Source SHA-256: `RevoluteJoint.cs` = `e910d0290b0eece58a2c544a8f783c8a4f5956363484e68b7e22cb86923c1352`; tests = `85808d5de58b858712413c090ac9838e5cf1a91d13a529941aa00e61dfa24c42`.

## Rotating geometry and finite-inertia contact groundwork

On base revision `ecbafb4`, `RotatingBoxSweep` adds continuous sphere/rotating-box time-of-impact queries. It follows a box around a fixed world-space pivot at constant angular speed while the sphere moves linearly. A separating plane's maximum vertex velocity and projected acceleration bound determine the next conservative sample; endpoint poses alone cannot establish clearance. Contact is classified with `SphereSweepStatus`, with world normal, surface point, penetration, time and iteration count. Invalid geometry, loss of representable progress and iteration exhaustion are explicit errors, not collision-free fallbacks.

Eighteen sweep tests cover analytic positive/negative rotation hits at ordinary and high speed, clear endpoints with an intermediate collision, agreement with stationary-box linear sweeps, separating contact, a later hit after initial separation, persistent axial tangency without tiny steps, depth/radius misses, initial overlap, arbitrary transformed setups and an off-centre orbiting box. Three seeded tests contain 240 moving/rotating setups and independently sample poses for missed or late deep contacts. This finite sample suite is not a mathematical proof for every possible input.

`RevoluteContact` applies a normal impact to a free sphere and finite-inertia hinge. A resting outward stop acts as an immovable surface. If an impact reverses an inward-moving hinge back into its stop, the stop absorbs angular energy; residual approach is removed plastically without applying restitution a second time. No friction or multiple-contact solver is claimed.

Eighteen contact tests cover energy/angular-momentum transfer, restitution, both resting stops, inward release, moving-at-stop reversal, pivot and separating contacts, invalid input, and a continuous rotation-driven hit that transfers actual hinge energy to a stationary sphere. Three seeded tests additionally exercise 3,000 varied mass/inertia/velocity/arm/restitution cases across interior/lower/upper states, checking passivity and no residual approach. Together with the original hinge cases, **50 focused tests pass**.

A new rejection assertion initially failed: speed `1e-200` over duration `1e200` was accepted even though squaring that speed underflowed the acceleration bound to zero. The sweep now explicitly rejects angular rates whose squared bound is zero or nonfinite. The failed test output is retained in `docs/playtest-results/rotating-box-underflow-v1-failure.txt`; this is an unsupported numeric-range error, not a stationary-motion fallback.

After the numeric-range correction, all **1,243 native cases pass** and the production Release browser publish succeeds. Reproduce with `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`; focused coverage selects `RevoluteJoint`, `RotatingBoxSweep` and `RevoluteContact`. Source SHA-256: rotating sweep `bc65634080caab63649d330e1605aea5cad0b8cf2b648ade273fbc388caf2691`; contact response `f175a81851afd82f7edf7bebca1466053bef4f15e414786ddd868c49ec4b90a9`.

At that groundwork revision, these operations remained unused by `MachineWorld`. The coupled sweep/contact test is a direct engine-level construction, not actual-UI or scene-graph proof. Existing gameplay is unchanged. World-clock integration, resting/sliding cargo, beam obstruction by fixtures, rope constraints, visuals and browser evidence remain required.

## Initial scene and shared-clock integration

On `9ca3638` plus the current integration, each part may own enum-identified `HingedBody` objects. The world advances their angles on the same event clock as spheres, including finite end stops and rotation-only impacts. Ordinary geometry/ray queries see the posed beam; flight excludes a frozen copy and uses its continuous sweep instead. Hinge angle and velocity now contribute to deterministic state signatures and typed read-only Playwright diagnostics.

The contact API was forward-refactored from mass to normal inverse-mass response, with all callers/tests updated. A constrained normal has zero mobility; it is not replaced by a very large fictitious mass. Initial overlap against a fully constrained body's forbidden positional direction still throws explicitly and remains integration work.

A first real-world transfer test failed at tick 74 after exhausting 4,096 contact iterations: a heavy load riding the turning beam generated tiny repeated contacts. The failure remains in `docs/playtest-results/impact-lever-world-v1-failure.txt`. The correction applies a plastic support projection using the predicted contact geometry after an actual impact. It removes inward velocity without a bias speed, extra restitution, iteration-limit increase or discarded simulation time. Focused energy bounds are unchanged.

The initial C# scene has a cream beam, cyan upper inset, four gold arm marks, gold pivot/stops and navy foot. It uses the normal toolbox/placement/rotation controls and an original outline pictogram; there are no new panels. `ImpactLeverParameter` types the beam mass and authored initial-angle parameters. Its visible child beam follows the actual joint angle.

Nine native lever cases verify aligned impact transfer versus depth-missed/pivot controls, a 1% energy bound through 480 ticks, exact Reset/deterministic replay, equal opposite loads remaining balanced through 1,200 ticks, four authored 3D orientations and the enum parameter boundary. A normal-constrained contact case verifies zero forbidden body motion. Two diagnostic cases verify enum/state round-trip and canonical/invalid wire values.

Initial integration verification: **all 1,255 native tests pass**, including 62 hinge/sweep/contact/lever/diagnostic cases. Diagnostic and production Release browser publishes pass. The broader component remains incomplete for the reasons below.

### Actual-UI evidence

[Exact recipes, adapters, camera actions and capture source](impact-lever-ui-recipes.json) record Free workshop row 62. Each actual construction has exactly three expected parts, no links, positions within 0.025 units of the recipe, zero browser errors and exact Run/Reset equality.

- `impact-lever-transfer-v1`: lever (0,3,0), bowling ball (−1.2,5.988797,0), orange ball (1.2,3.4520533,0). In 101 frames, the orange ball reaches Y=4.756195 and sampled upward speed 4.8468475 above Y=3.5. The beam reverses and reaches its upper stop. Outcome images show the same orange ball leaving the raised end.
- `impact-lever-missed-v1`: bowling ball moved to Z=2, actual Y=5.993779. The orange ball never rises above Y=3.4577332; the beam tips toward that load and stays at its lower stop. 101 frames, with inspected no-transfer images.
- `impact-lever-motion-v1`: repeated aligned construction in a fresh recording context, with ordinary wheel zoom and toolbox scrolling. All 101 shared body/hinge frames equal the first transfer run. The exported-browser icon and build projections were inspected. Consecutive 25-fps frames from seconds 27.55–28.55 show intermediate tilt/contact/release poses; this focused desktop readability review passes, not mobile or 60-fps certification.
- Recording: 36.28 seconds, VP8, 1440×900, 25 fps. SHA-256 `06099fd215189fd0ea092bd15297492ea291328c496f9f27cdeb563f0f0fdeb4`. Diagnostic samples span 7.964 wall-clock seconds for eight simulated seconds. No receiver/campaign goal is present in this workshop proof.

Full local logs remain under `docs/playtest-results/<caseId>.json`; screenshots/video remain under `.playwright-mcp/`. Current part SHA-256 `5db45b325a5a472090fd19cb6d939571d1ee6027b26929eea03560c7ced0ca04`, hinge adapter `abdec153836b0cd97f81c178bbc0ef2d2cc5a2d2a9d2941bec0d5d2690a87848`, flight `d6a41e3ba57a2bb7a99f7f391b1408f66ff3af06b9d9e88b00ff544f9030b7d0`.

The existing trampoline-to-pipe construction was repeated as `impact-lever-flight-regression-v1`. Its actual configuration matches the historical run, all 100 shared body-state frames match exactly, the ball visibly traverses the pipe, and Reset/errors pass. This is one focused no-hinge shared-clock regression, not the exhaustive campaign matrix.

### Rounded stop-event correction

Final review found a second numerical edge: advancing from the representable angle immediately below a limit by 75% of the remaining time can round the angle onto the limit while angular velocity is still nonzero. A new `RoundedArrivalAtAStopCanBeResolvedWithoutAdvancingTime` test failed: `Advance(0)` returned `None` instead of dissipating that outward motion. The original output is retained in `docs/playtest-results/impact-lever-rounded-stop-v1-failure.txt`.

The joint now resolves an already-reached stop even for a zero-duration advance. The shared clock explicitly invokes that operation for a hinge-stop event, including zero-time events, so rounding cannot produce an endless series of unchanged stop events. No timestep is discarded, and no restitution/energy limit is relaxed. The v1 source hashes above identify the earlier captures. [Refreshed recipes and source hashes](impact-lever-stop-correction.json) identify the correction: aligned v2 matches all 101 shared body/hinge frames and missed v2 all 100 shared frames, with identical configurations, zero errors and exact Reset. The artwork is unchanged. All **1,256 native tests** (63 focused hinge/lever/diagnostic cases), diagnostic publish and production Release publish pass after the correction.

**Still incomplete:** beam blocking against fixtures/other beams, full fulcrum collision coverage, end rope sockets and angular rope constraints, guided-overlap positional solving, broader contact/friction and interacting loads, campaign lessons, mobile and broader performance review. The part remains unchecked. A passing throw in empty space is not proof of these requirements.

## Required integration before the component can pass

1. Complete shared-clock collision coverage for beam/fixture and beam/beam obstruction, plus solid fulcrum coverage. Preserve the verified finite-inertia sphere response and end stops; a kinematic rotating box must not inject energy into cargo.
2. Support resting/sliding loads and distributed/off-centre contacts, rather than scripted opposite-end launches. Verify mass × arm balance through actual contact.
3. Add typed end rope sockets and couple their moment arms/velocities into rope constraints. Test slack, taut and shutter loads.
4. Complete and review the C# scene's remaining physical coverage and rope artwork. Preserve the current cream/cyan/navy forms, gold fulcrum/stops and original icon. The beam must display its simulated angle without decorative flipping.
5. Test impact transfer, insufficient energy, hinge/tip/off-centre hits, blocked sweep, stops, moving/sliding cargo, multiple contacts, arbitrary authored 3D orientation, deterministic replay and exact construction Reset.
6. Prove intended behaviour and negative controls with actual-UI Playwright construction and continuous motion review. Record actual placements/connections, retained failures and production build. Add progressively taught campaign lessons.
7. Only mark the part complete and publish it as verified after the component-level gates pass. The hinge primitive alone does not meet them.
