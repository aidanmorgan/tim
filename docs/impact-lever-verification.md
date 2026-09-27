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

## Required integration before the component can pass

1. Add the beam's real swept rotating collision geometry to the shared flight clock. Solve finite-inertia contact and active end stops together; a kinematic rotating box must not inject energy into cargo.
2. Support resting/sliding loads and distributed/off-centre contacts, rather than scripted opposite-end launches. Verify mass × arm balance through actual contact.
3. Add typed end rope sockets and couple their moment arms/velocities into rope constraints. Test slack, taut and shutter loads.
4. Build the C# scene/catalogue entry and original pictogram. Keep cream/cyan/navy forms, a gold fulcrum, visible physical stops and the current palette. The beam must display its simulated angle without decorative flipping.
5. Test impact transfer, insufficient energy, hinge/tip/off-centre hits, blocked sweep, stops, moving/sliding cargo, multiple contacts, arbitrary authored 3D orientation, deterministic replay and exact construction Reset.
6. Prove intended behaviour and negative controls with actual-UI Playwright construction and continuous motion review. Record actual placements/connections, retained failures and production build. Add progressively taught campaign lessons.
7. Only mark the part complete and publish it as verified after the component-level gates pass. The hinge primitive alone does not meet them.
