# Planning audit 3: spec-family-ball-presets.md

Audited 21 November 2026 on checkout `spec-drafts` (`8d1d68aa`), read-only. Nothing was built or run; every number below is from reading code and scenes. The verdict is given as it would stand once S7 is published as drafted.

## Verdict: Scoped Pass (blocked only on prerequisite S7)

No blocking finding remains. R1 (roll rows) and R2 (art decision) from audit 2 are repaired: the ramp is tilted and the rest condition is gone, and the owner has decided the art and the Baseball proportions. The new numbers agree with the code. The three points the author could not verify are settled below, all in the contract's favour. Six small text fixes are listed; none changes intent.

## The three unverified points

### 1. A ball rolling over its band on the 10 degree ramp still clears 0.64 m: yes

- The band's axis is 35 degrees from vertical in the authored pose (`elements/Ball/ball.tscn`, quaternion (0, 0, -0.3007058, 0.95371695)). On a ramp tilted 10 degrees about Z the band first reaches the contact point after 45 degrees of roll at the earliest (65, 115 or 135 degrees for the other sign and direction).
- 45 degrees of roll on a 0.32 m ball is 0.25 m of slope. Under the delivered law (`WorkshopPhysicsLawTests.cs:1170`) the Soccer ball then moves at about 0.7 m/s, which is 0.35 J/kg of rolling energy against 0.10 J/kg to climb a 1 cm step. Gravity keeps paying 0.17 m of drop per half turn afterwards.
- Past the lower end the ball moves at about 1.7 m/s (Soccer) or 1.8 m/s (Cannonball). 0.64 m is less than one half turn (1.0 m), so it meets the band at most once there.
- The bench is an infinite plane (`engine/gpu/WorkshopPhysicsCompiler.cs:97`-`:102`, `ColliderShapeKind.Plane`), so the ball cannot leave it and speed up. "Slower after 480 ticks" holds: about 0.75 m/s against 1.7 m/s.

### 2. Two Soccer balls fit side by side on a default plate: yes, with one placement note

- The sensing top is 1.8 x 1.8 m (`elements/PressurePlate/pressure_plate.tscn:22`, `:43`, at y 0.03, so the face is at y 0.08). Two 0.66 m balls need 1.32 m.
- The top carries a centre ring: centreline radius 0.3 m, tube 0.018 m, at y 0.084 (`pressure_plate.tscn:34`-`:37`, `:88`-`:89`, collider `col_visual_ring`). Studs are at the corners (plus or minus 1, plus or minus 1), outside the top.
- One ball at the plate centre rests on the face inside the ring for every radius here (0.25 to 0.34 m): the ring is at least 0.09 m clear of the sphere.
- Two balls at plus or minus 0.4 m, as the delivered Basketball row places them (`PressurePlateTests.cs:410`), start 3 to 7 mm inside the ring and are pushed outward onto the face. That works today for Basketballs, but it adds an incidental nudge.
- Only direct contact with the face counts (`PressurePlateTests.cs:466`), and the threshold is inclusive on summed declared mass (`engine/gpu/ContactLoadSensorDeclaration.cs:27`): two Soccer balls 0.90 kg press; two Baseballs 0.30 kg do not; two Tennis balls 0.70 kg press; at 0.25 kg two Baseballs press and one (0.15 kg) does not.

### 3. A ball with spin never rests on its band: not provable, and no row needs it

- A rolling ball can stop balanced on its band only inside a very narrow window (a 0.014 m tube crown held by a rolling coefficient of 0.02 to 0.04); it would then sit 1 cm high.
- No acceptance row asserts a rest height after rolling or launching. SB5 and CB5 assert distance and slowing. CB2 and CB3 assert speed. The rest rows (SB2, CB1, BB1, the plate rows, the level-bench control) place or drop the ball without spin, where the lowest point of the band is 0.195 m below centre against the sphere's 0.32 m, so the sphere lands and rests.
- Contract line 154 claims only the no-spin case, which is correct. No change needed.

## New numbers checked against code

- **Airflow law (BB5).** Impulse = force x weight x seconds x (1 - speed / flow speed) (`CuriousContraptions.Abstractions/ConicalAirflow.cs:19`-`:26`). The Fan's flow speed is 12 m/s and its power is force x 12, so the source cap equals the force and does not bind (`elements/Fan/FanElementDeclaration.cs:46`-`:51`). Four samples of weight 0.25 sit at half the sphere radius, inside the cone at 3 m, so the full 9 N applies whatever the size. After 12 ticks: Baseball 4.7 m/s, Tennis ball 2.3 m/s, Basketball 0.87 m/s; ratios 2.04 and 5.4, as the contract says. Thresholds 1.5 and 3 are sound.
- **One geometric margin in BB5.** The occlusion check skips only the receiver's own sphere collider, not the band of the same body (`WorkshopPhysicsEngine.Airflow.cs:151`). In the authored pose with the fan blowing along +X, the ray to the +Y sample passes the upstream side of the band with 2.9 mm to spare on the Baseball (7.4 mm Soccer, 8.9 mm Basketball), against a 0.2 mm query tolerance. It clears. If it did not, the Baseball would take three quarters of the force: about 3.75 m/s, still 1.6 times the Tennis ball and 4.3 times the Basketball, so both thresholds hold either way.
- **Cannon window.** 0.6 to 0.84 m inclusive on both transverse widths (`elements/Cannon/CannonElementDeclaration.cs:31`-`:32`; `WorkshopPhysicsEngine.Chambers.cs:131`-`:139`, which takes the support of every collider of the payload). Soccer ball and Cannonball: 0.64 to 0.66 m depending on band orientation, seated. Baseball: 0.52 to 0.54 m, never seats. The Basketball already seats at 0.68 to 0.70 m with its stripe.
- **Band geometry.** Basketball stripe: inner 0.322, outer 0.35 on r 0.34, so r - 0.018 and r + 0.01; compiled torus radius 0.336, tube 0.014 (`BasketballResourceTests.cs:51`-`:53`). Contract values 0.302 / 0.33 and 0.242 / 0.27 match, as does "centreline r - 0.004, tube 0.014" on line 234.
- **Scene and `Specs`.** `col_visual_band` as a hidden `MeshInstance3D` with the torus mesh is baked as a collider by its `col_` name (`tools/BundleElements/SceneGeometryExtractor.cs:23`-`:25`), as `col_visual_stripe` is. The `Specs` row form matches the Basketball's (`VisibleSolidColliderTests.cs:109`).
- **Inertia.** 0.4 x 0.15 x 0.26 squared = 0.004056. Correct.
- **Capability 17.** Symbol and both cited tests exist and exercise a torus on a dynamic ball.

## Small fixes (not blocking; apply before the intent freezes)

1. Line 51 (SB3) and line 59 (BB3): state the placement so the ring is not involved. Add to the note on line 45: "Plate rows place one ball at the plate centre, and two balls 0.5 m either side of it, clear of the plate's centre ring (centreline 0.3 m)."
2. Line 60 (BB5): name the construction. Replace "each alone 3 m in front of a default Fan" with "each alone on the axis of a default Fan, 3 m in front of it and in its authored pose, as `FanGuidanceTests.cs:19`-`:21` builds it".
3. Line 251 (BB5 boundary cell): add "the +Y sample ray clears the Baseball's own band by about 3 mm in the authored pose; with that sample lost the ratios are still about 1.6 and 4.3".
4. Line 199 (task 7): "capabilities 1-15 as exists" becomes "capabilities 1-15 and 17".
5. Line 135: add "band colour" to what differs per identity.
6. Line 160: "two of any other ball do" becomes "two of any other ball preset at its default mass do".

## Unchanged from earlier audits and still correct

Family shape, capability rows 1 to 16, the eight hand-edited lists, review tier, commit path, task 8's S7 text, the carry-forward note on S7 exclusion 7(a).

## Evidence limits

- Planned tests were not run; the simulated values above are hand calculations from the declared laws, not measurements.
- The band-step energy argument ignores impact loss at the step (about 2 percent of speed for a 1 cm step on a 0.32 m ball).
- S7 was read as drafted, not as published.
