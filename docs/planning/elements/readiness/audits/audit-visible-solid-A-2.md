# Planning audit 2: ELEMENTS-VISIBLE-SOLID-REMAINDER part A

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md` as repaired (4,185 words). Checkout `spec-drafts` at `8d1d68aa`. Phase: planning. Nothing was built or run; the numbers below are hand estimates from the scenes and declarations.

## Verdict: Scoped Pass, with one text correction to apply first

The five blocking points of the first audit are repaired and the intent contract is now true of the slice. One contradiction remains in the Code Map (R1). It is a sign swap with an exact fix, the planned test already catches it, and it is outside `<intent-contract>`, so it needs no third audit once applied. Scope of the Pass: planning only, for part A; no test has run.

## Remaining finding

### R1. The arrow's two head strokes are turned the wrong way

- Where: contract line 101.
- Defect: `HeadA` is centred at local z −0.0283 and turned +45° about Y; `HeadB` at z +0.0283 and turned −45°. A turn of +θ about Y carries +X to (cos θ, 0, −sin θ). So `HeadA` runs from (0.2434, 0) to (0.30, −0.0566) in Fan-local x, z, and `HeadB` mirrors it. The strokes meet at x = 0.2434, on the shaft's end, and open toward +X. That is a tail, not a head, and the text's own claim "both end at the point x = 0.30, z = 0" is false.
- Consequence: the planned test at line 179 is right and would fail against the scene as specified: ink would not cover (0.29, 0) or (0.25, 0.04). The implementer would have to depart from the Code Map.
- Fix: in line 101 change "turned +45° and −45° about Y" to "turned −45° and +45° about Y". Centres, sizes and the test stay as they are. With that, `HeadB` runs from (0.2434, 0.0566) to (0.30, 0); at x = 0.25 its ink spans z 0.029 to 0.071 and at x = 0.29 the two strokes span z −0.031 to 0.031, so all three ink points of line 179 hold.
- Classification: wrong specified behaviour (Code Map only).

## The hub test

The author's objection to my first proposal is correct on both counts. A ball moving along the hinge axis meets the hub with its velocity in the plane that contains the axis, so friction acts in that plane and has no moment about the axis. And a 0.25 m ball offset 0.25 m reaches 0.50 m, past the ring's 0.49 m inner surface.

The re-authored construction should pass the 0.1 rad/s bound with a wide margin. Estimate:

- Start: ball radius 0.1 at Fan-local (0, 0.30, 0.1), at rest. It is 0.316 m from the hub's centre (contact is at 0.24) and its far side is at 0.416 m from the axis (ring at 0.49), so it starts clear of both by about 7.5 cm. The post's top is at y −0.05, below it.
- Contact: at y = √(0.24² − 0.1²) = 0.218, after a fall of 0.082 m, at 1.27 m/s. The contact normal is (0, 0.909, 0.417). The approach speed along it is 1.15 m/s and the sliding speed is 0.53 m/s, in the Y–Z plane, so its whole moment is about the hinge axis X.
- Impulse: the Tennis ball is 0.35 kg with inertia 0.4·m·r² (`TennisBallElementDeclaration.cs:16,47`); the hub is 0.15 kg with inertia 0.001176 on a hinge with zero bearing friction (`FanElementDeclaration.cs:38-40`). The impulse that stops sliding is 0.53 / (3.5/0.35 + 0.14²/0.001176) = 0.020 N·s. The friction limit is 0.3 × 0.35 × 1.15 = 0.12 N·s, with the pair coefficient √(0.3 × 0.3) (`WorkshopConstraintSolver.cs:119`), so the contact sticks.
- Result: hub speed 0.020 × 0.14 / 0.001176 ≈ 2.4 rad/s, about 24 times the bound. Even a 0.058 kg ball would give about 0.8 rad/s. The ball then rolls off toward +z, which adds to the turn.
- `hitsHub: false`: at (0, 1, 0.8) the ball's near side is 0.70 m from the axis, 7 cm outside the ring's 0.63, and outside the base's 0.5 m half-depth. Nothing touches the hub.

The contract's instruction to return to planning rather than lower the bound is right. No alternative construction is needed.

## Repairs confirmed

- **B1, B2:** A9 and AC3 now name the re-authored hub test and the count at `FanIntegrationTests.cs:465`; the Always clause and D6 state the hub consequence.
- **B3:** A7 is split; the arrow is three flat quads 0.2 mm above the base at x 0.10 to 0.31, inside the strip the post and ring leave free and inside the 0.325 edge. See R1 for the stroke angles.
- **B4:** D4 and D5 are recorded as coordinator decisions; no decision is open.
- **B5:** both placements hold. Ring case: the four samples sit 0.529 to 0.591 m from the axis, inside the tube's 0.49 to 0.63 shadow and inside the 0.714 cone; after 12 ticks of fall (5 cm) they are still inside both. Post case: samples at y −0.27 to −0.33, inside the post's shadow and the 0.425 cone, and still so after the fall.
- **N1 to N6:** the eye control, the lever stand-offs (0.2 and 0.4 mm), the wider filter, the bundle baseline, the level-proof serialisation and the citations are all in.
- **Count:** 5 + 4 + 3 + 3 = 15 matches the table.

## Notes, not blocking

- Line 176: the Wall control gives no pose. A default Wall (3 × 2 × 0.25) centred at Fan-local (2, 0, 0.56) spans z 0.435 to 0.685 and covers all four streamlines; say so.
- Line 172: the Tennis ball rebounds at 0.85 from the mount, so "stops ... within 5 mm" should be read as its lowest centre height, as the drop theory does.
- Line 179: `FaceMarkingTests.Covers` is private and projects onto X and Y; the Fan fact needs its own X, Z projection.

## Evidence limits

- The hub estimate is a rigid impulse calculation; the solver's soft contact may give less, against a margin of about 24.
- No build or test was run. Rope, weight and balloon tests were checked by placement only, as in the first audit.
