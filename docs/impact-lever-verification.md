# Impact lever — implementation and verification

Status: **in progress, not a playable or verified catalogue part**. The research TODO's “expand existing lever” wording did not correspond to an implemented lever in the current parts/catalogue. The work below is a prerequisite, not a substitute for the complete component.

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

These operations remain unused by `MachineWorld`. The coupled sweep/contact test is a direct engine-level construction, not actual-UI or scene-graph proof. Existing gameplay is unchanged. World-clock integration, resting/sliding cargo, beam obstruction by fixtures, rope constraints, visuals and browser evidence remain required.

## Required integration before the component can pass

1. Add the beam's real swept rotating collision geometry to the shared flight clock. Solve finite-inertia contact and active end stops together; a kinematic rotating box must not inject energy into cargo.
2. Support resting/sliding loads and distributed/off-centre contacts, rather than scripted opposite-end launches. Verify mass × arm balance through actual contact.
3. Add typed end rope sockets and couple their moment arms/velocities into rope constraints. Test slack, taut and shutter loads.
4. Build the C# scene/catalogue entry and original pictogram. Keep cream/cyan/navy forms, a gold fulcrum, visible physical stops and the current palette. The beam must display its simulated angle without decorative flipping.
5. Test impact transfer, insufficient energy, hinge/tip/off-centre hits, blocked sweep, stops, moving/sliding cargo, multiple contacts, arbitrary authored 3D orientation, deterministic replay and exact construction Reset.
6. Prove intended behaviour and negative controls with actual-UI Playwright construction and continuous motion review. Record actual placements/connections, retained failures and production build. Add progressively taught campaign lessons.
7. Only mark the part complete and publish it as verified after the component-level gates pass. The hinge primitive alone does not meet them.
