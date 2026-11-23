# Planning audit: `spec-elements-visible-solid-remainder-b2.md` (visible-solid remainder, part B2: Tennis ball)

Audited 22 November 2026. Phase: planning. Nothing was built or run; every statement about a test's outcome below is a prediction from reading the code. Contract, policy and code were read in the `spec-drafts` checkout (`8d1d68aa`, contract is an uncommitted draft of 3,086 words). Published main `fa86969f` was read only through `git show` and `git diff 8d1d68aa fa86969f`.

## Verdict: Incomplete

The design is sound: redrawing the seam at 31/128 and 33/128 of the radius is admissible over the whole radius range, the two matcher changes cannot hide a defect at normal sizes, mass and inertia do not change, the Cannon figure is right, and the engine tier is right.

The contract is not Ready for Development because one of its stated facts is false and its "nothing else changes" analysis rests on it. A moving torus does meet the bench plane and another torus. From that follow one existing test that I expect to be refused at construction, one that I expect to be at risk, a second such test on main, and three places where the read-only intent contract is wrong or stale. One more consumer was missed (a ball's own seam can shadow its own airflow samples). O1 is still written as an open owner decision.

All fixes are text, plus small edits to four existing test scenes. None needs a redesign. With B1 to B5 applied the contract needs one short re-audit of the changed rows (T5, AC3, the Code Map's consumer list), because B2 changes what the slice edits.

## What was checked and holds

- **Admissibility over the whole radius range.** Bindings `Radius = 1 · radius` and `HalfExtentY = 0.03125 · radius` (contract `:83`) go through `BundleGeometry.Colliders` (`CuriousContraptions.Bundle/BundleGeometry.cs:28–46`), which evaluates `multiplier · parameter + offset`. Both multipliers are powers of two, so no radius in [1/16, 16] rounds. Admission (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:261–267`, `Range` inclusive at `:325–329`): tube r/32 ≥ 1/512 exactly when r ≥ 1/16; tube ≤ 0.5 ≤ 16; centreline r within [1/16, 16]; centreline > tube always. The scene values 0.2421875 and 0.2578125 are exact in f32, and the extractor's `(inner + outer) · 0.5` and `(outer − inner) · 0.5` (`tools/BundleElements/SceneGeometryExtractor.cs:210–216`) give exactly 0.25 and 0.0078125. Art at the bounds: scale 4 · r is 0.25 and 64, both exact; drawn centreline and tube equal the collider's to the bit.
- **Proud height at each end.** The seam stands 1/32 of the radius above the felt at every size (3.125 per cent; the Basketball's stripe is 0.01 / 0.34 = 2.9 per cent, `elements/Ball/ball.tscn:9`, `:17–18`).

  | Radius | Tube radius | Proud height | Band width |
  |---|---|---|---|
  | 1/16 m | 1/512 m (the floor) | 1.95 mm | 3.9 mm |
  | 0.25 m (default) | 7.8 mm | 7.8 mm | 15.6 mm |
  | 0.32 m | 10 mm | 10 mm | 20 mm |
  | 2 m | 62.5 mm | 62.5 mm | 125 mm |
  | 16 m | 0.5 m | 0.5 m | 1 m |

  The owner accepted a proud band "of about 1 cm". Above a 0.32 m radius the seam is more than 1 cm proud in absolute terms, up to 0.5 m at 16 m, but it is the same fraction of the ball at every size, so the ride over it looks the same. I judge this acceptable and within the coordinator's delegation; it is listed for the owner's information below.
- **S1 (scale-relative uniformity guard).** The present guard is absolute (`VisibleSolidColliderTests.cs:288`). At the 16 m ball the seam's basis is 64 times a 45° turn about X, whose Y and Z column lengths are 64 · √(c² + s²) and round by a few parts in 10⁷, that is several micrometres against a 2e-6 bound; the X column is exactly 64, and the drawn extents use only `s.X` (`:289–290`). So the failure the contract describes is real and the extents stay exact. The new bound `Tol · max(1, s.X)` is identical at a scale of 1 or less. Above it, it admits a relative non-uniformity of 2e-6: 32 µm on the radius of a 16 m torus. It cannot hide a squashed torus at any size.
- **S2 (mesh-error bound, larger of 5 mm and 0.2 per cent of the drawn outer radius).** Identical below a 2.5 m drawn radius. The redrawn seam with Godot's default 64 rings and 32 ring segments has error 0.2578125 · (1 − cos(π/64)) + 0.0078125 · (1 − cos(π/32)) = 3.48e-4 per unit of scale: 0.135 per cent of the outer radius, 22 mm at 16 m against a bound of 33 mm; it passes 5 mm up to a 3.6 m ball. A 32-ring torus (0.48 per cent) still fails wherever its error exceeds 5 mm, as today. No hollow part in the current table is drawn above 2.5 m. It cannot hide a defect at normal sizes.
- **Mass and inertia.** The body's mass and moments come from the declared source (`TennisBallElementDeclaration.cs:48–53`); the cited tests exist (`TennisBallTests.cs:29`, `CompoundMassAndRollingTests.cs:283`). A moving hollow collider needs an explicit mass frame (`WorkshopPhysicsLawHollowTests.cs:893`); the declared frame is one.
- **Bounce.** The restitution binding is per body; the seam takes the ball's bounce.
- **Cannon.** `ChamberRegions` gives widths 0.6 to 0.84 m (`elements/Cannon/CannonElementDeclaration.cs:31–32`). `SeatInChamber` takes the width over every enabled collider of the payload from support points (`WorkshopPhysicsEngine.Chambers.cs:112–137`). Across the seam the ball is 2.0625 r wide: 0.84 / 2.0625 = 0.4073 m. The smallest stays 0.3 m because both cross widths must reach 0.6 m and the narrower is the felt. The claim is right. See N6 for what it means in play.
- **Tests that assert one collider.** Exactly the two the contract names: `TennisBallTests.cs:45` and `TennisArtTests.cs:34`. Nothing asserts the seam's old radii (search for 0.245 and 0.253 in tests, tools, packages and element documents). `ElementFootprint` counts bodies and joints, not colliders (`CuriousContraptions.Abstractions/ElementFootprint.cs:5–9`), so the declared footprint does not change.
- **Authored levels.** `content/puzzles.json` has 23 records of kind `tennis` in 22 puzzles. Every one of those puzzles has `"availability": "Planned"`; none of the 15 `Playable` puzzles contains a Tennis ball, and no test names any of the 22 puzzle ids. No authored playable level is affected. The contract's sentence "whether any is compiled with this package was not established" (`:59`) can be replaced with this.
- **Capability probe citations.** Every cited symbol and test exists at the cited place, with the exceptions in B1, B5 and N3.
- **Review tier.** Engine tier is correct: the slice changes shared matcher code in a test class, which the allow-list excludes ("no new or changed shared harness, fixture or helper code"), and the probe has a narrower capability.
- **Shape and size.** One product repair slice over one package; 3,086 words; five matrix rows and five acceptance criteria.
- **Commit path.** No new source file; nothing is discovered by a glob; the bundle and the evidence directory are ignored (`.gitignore:6`, `:17`); the run log is intent-to-add before `git diff --check`.

## Blocking findings

### B1. A moving torus does meet the bench plane and another torus

- Where: contract `:99` ("A torus does not meet the bench plane or another torus, as for the Basketball"), and through it `:98`, `:162` and the intent row T5 (`:39`).
- Evidence: `CollideHollow` maps Plane, Tube, Frustum and Torus partners (`CuriousContraptions.Simulation/WorkshopHollowKernels.cs:27–40`). `HollowContactQuery.Measure` dispatches a plane to `CollidePlaneHollow` and a hollow partner to `CollideHollowPair` (`engine/gpu/HollowContactQuery.cs:40–55`, `:632`, `:855`). The candidate filter rejects only same-body, static-static and joint-disabled pairs (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:875–887`). Executed tests exist: `WorkshopPhysicsLawHollowTests.MovingHollowRestsOnThePlaneAtItsAnalyticHeight` (`:1338`), `MovingHollowPairsTransferImpulseAndTheSeparatedControlPasses` (`:1371`), `TwoMovingHollowPartnersExchangeMomentumWithoutCreatingEnergy` (`:1474`). The comment at `WorkshopCollisionKernels.cs:1996–1998` ("hollow colliders are static") is stale and is probably the source of the claim.
- Consequence: on the bench, the surface most play happens on, a rolling Tennis ball rides over its seam. The scene turns the seam about X, so the seam always passes through the ball's ±X poles; a ball rolling along X crosses it twice per turn. Two Tennis balls meet seam to seam. The placement check compares one ball's seam with the other's. The owner accepted the ride in principle, so this is not a design change, but every "unaffected" judgement in the contract that relied on `:99` has to be redone (B2).
- Fix, `:99`: "Airflow is sampled on the sphere, collider 0 (see the airflow note). A moving torus meets the bench plane and other hollow colliders like any other shape (`WorkshopPhysicsLawHollowTests.MovingHollowRestsOnThePlaneAtItsAnalyticHeight`, `MovingHollowPairsTransferImpulseAndTheSeparatedControlPasses`), as the Basketball's stripe does."
- Fix, `:98`: "Rolling: the ball rides over its seam on every surface, the bench included. The seam passes through the ball's ±X poles, so a ball rolling along X crosses it twice per turn; the seam first presses on the surface when the ball has turned 69.5° from its placed pose (the seam is within 14.4° of the lowest point). Dropped without spin it lands on felt: …" (keep the rest).
- Fix, capability probe: add two Present entries with the tests above.
- Classification: wrong specified behaviour (a false premise in the Code Map).

### B2. Existing tests that the slice breaks or puts at risk, against T5 and AC3

- Where: intent row T5 (`:39`), AC3 (`:130`), task 5 (`:123`), the owned scope (`:54`), the T5 table row (`:162`).
- Defect: T5 and AC3 say the named existing tests pass with only the two single-collider edits. By reading, that is not so.

1. **`TennisBallTests.ConfiguredMassChangesMomentumTransferredToAnEqualRadiusPayload`** (`TennisBallTests.cs:118–132`). Two default Tennis balls are placed 0.51 m apart on X (`:121–122`). Each seam reaches 33/128 m = 0.2578 m along X, so the two seams overlap by 5.6 mm at the start (0.5156 > 0.51). `ValidateHollowPlacement` runs in every compile (`engine/gpu/WorkshopPhysicsCompiler.cs:110`) and compares every enabled collider of a dynamic body with every hollow collider of another body (`:498–523`); `HollowContactQuery.Overlaps` reports a hollow pair deeper than 0.1 mm (`engine/gpu/HollowContactQuery.cs:14`, `:58–66`). I expect `WorkshopEditRejection.InitialOverlap` for both theory cases. Seam against felt is clear by 2.2 mm.
2. **On main: `BaseballTests.MatchedSizeImpactChangesPayloadRouteByMass`** through its `Impact` helper (main `CuriousContraptions.tests/BaseballTests.cs:63–100`): a Tennis striker at x = −0.51 against a Tennis target at 0, twice (`:72–73`). Same overlap, same expected refusal. The Baseball striker case is clear by 2.2 mm. The drafted Baseball repair deletes this helper; which state B2 meets depends on the order of the two slices.
3. **`TennisBallTests.ConfiguredRadiusChangesContactSpinAtFixedMass(.125f)`** (`:134–150`). The ball sticks and rolls on the bench at 5/7 m/s for about 28.8 ticks: 0.171 / r rad, that is 79° at r = 0.125 m. The seam presses on the bench from 69.5°, so in the last three or four ticks the bench pushes up on the seam ahead of the centre. Gravity is cancelled in this scene, and I estimate the kick changes the spin by several per cent against a ±5 per cent band. At risk, not certain. The 0.5 m case turns 20° and is not affected.
4. **Fixtures derived from the Tennis entry.** `GasRuntimeTests.cs:18–24` sets `ColliderBindings = []` and resizes collider 0 from a gas store, which would leave a fixed 0.25 m ring on a sphere of another size. `ParameterizedBodyIntegrationTests.cs:17–26` rebinds only collider 0 to another parameter, leaving the ring unbound. Neither is a Tennis ball; both borrow the entry as "a sphere". `FanIntegrationTests.cs:134`, `WindmillAdmissionTests.cs:179` and `FanAdmissionTests.cs:295` keep the bindings and are consistent.

- Consequence: the implementer either breaks AC3 or stops and returns to planning, and the intent contract cannot be edited then.
- Fix, intent row T5 (`:39`), Expected column: "Pass. The only edits to existing tests are those the Code Map lists: the two single-collider selections, the start spacing of head-on Tennis pairs, and the borrowed-sphere fixtures."
- Fix, owned scope (`:54`), add:
  - "`CuriousContraptions.tests/TennisBallTests.cs:121` -- the striker starts at x = −0.53 (seams 14 mm apart). The expected speeds do not change: they follow from the masses."
  - "`CuriousContraptions.tests/BaseballTests.cs` `Impact`, if the helper exists at the baseline -- the same start, x = −0.53; the one-second route thresholds are re-derived from the 2 cm later contact, not fitted."
  - "`CuriousContraptions.tests/GasRuntimeTests.cs:20` and `ParameterizedBodyIntegrationTests.cs:20` -- the derived entry takes `Colliders = [original.Colliders[0]]`: it is a sphere fixture, not a Tennis ball."
- Fix, a new line under "Footprint, capture and occlusion": "`ConfiguredRadiusChangesContactSpinAtFixedMass(.125f)` rolls 79° and the seam meets the bench from 69.5°. If the scoped run fails it, the small case becomes radius 0.1875 m (52° of roll, still distinct from the default 0.25 m lever arm) with the bounds unchanged; the bounds are not widened."
- Fix, AC3 (`:130`): "…then all pass with no edit other than those the owned scope lists."
- Fix, T5 table row (`:162`): name the four tests above. Fix, the filter at `:170`: add `FullyQualifiedName~Baseball` (`GasRuntimeTests` and `ParameterizedBodyIntegrationTests` are already in it).
- Classification: test.

### B3. Stale or wrong text inside the read-only intent contract

- `:23`, "(41 of 44 after parts A and B)". On main there are 45 packages: the Baseball was added outside the table, and its repair adds a row of its own. Fix: delete the parenthesis. In AC1 (`:128`) replace "(41)" with "(baseline + 1)".
- `:39`, T5: see B2.
- `:27`, "airflow sampling … unchanged". The sample points are unchanged, but the seam becomes an obstacle to the ball's own samples (B5). Fix: "…restitution binding, the airflow sample points, buoyancy and drag are unchanged. The seam is an obstacle to air like any other collider, as the Basketball's stripe is."
- Classification: wrong specified behaviour (text).

### B4. O1 is still an open owner decision

- Where: `:112`, and Design Note 8 (`:147`).
- Defect: planning is not ready while a required-now decision is open (role card, Informed auditors). The coordinator has decided it under the owner's standing delegation.
- Fix, `:112`: "**Coordinator decision (22 November 2026, under the owner's standing delegation): the seam is redrawn.** Radii 31/128 and 33/128 of the ball's radius, so that a radius-bound torus is admissible at the smallest ball (tube 1/512 m, centreline 1/16 m, `PhysicsDeclarationValues.cs:261–267`). Not chosen: lowering the engine's torus floor (an engine slice with solver consequences) or raising the radius minimum (cuts a declared range that tests use at 1/16 m). The seam is 15.6 mm wide and 7.8 mm proud at the default size, where it was 8 mm and 3 mm; it is 1/32 of the radius proud at every size, from 2 mm at 1/16 m to 0.5 m at 16 m."
- Fix, `:147`: remove "O1." from the uncertain list.
- Classification: wrong specified behaviour (record).

### B5. Missed consumer: the ball's own seam can shadow its own airflow samples

- Where: contract `:59` ("Not consumers, checked … the airflow receiver names collider 0") and `:99`.
- Evidence: `AirflowLineClear` skips only the receiver's own collider slot and the source's declared exclusions (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:142–160`, the skip at `:151`). The seam is another collider of the same body, so a ray to one of the four samples (`TennisBallElementDeclaration.cs:27–30`) is blocked when it crosses the seam. `AirflowReceiverDescriptor` has no exclusion list; the receiver-side exclusion parameters at `:142` are used only by the rotary capture (`WorkshopPhysicsEngine.RotaryAirflow.cs:60–61`).
- Size: in the placed pose, for a stream along X, Y or Z, each ray passes 0.354 r from the seam's plane against a tube of r/32, so nothing is lost and the existing Fan tests should be unaffected. Once the ball has turned, a sample is shadowed over roughly 3 per cent of orientations (my estimate), and each lost sample is a quarter of the push for that tick. The Basketball already behaves this way. All 22 planned Tennis levels are wind levels, so the fact should be on record.
- Fix, capability probe: add "**Narrower, accepted.** A receiver's own other colliders are not exempt from shadowing its samples (`WorkshopPhysicsEngine.Airflow.cs:151`). Clear in the placed pose for axis-aligned streams; a turned ball can lose one sample at a time, as the Basketball does. A receiver-side exemption would be an abstraction and ABI change, so it is an engine slice if wanted: recorded in `deferred-work.md`, not built here."
- Fix, task 6 (`:124`): add "the airflow note" to what `CAT-064-tennis.md` and `deferred-work.md` record.
- Classification: wrong specified behaviour (a missed consumer; the behaviour follows precedent).

## Non-blocking findings

- **N1. `:92` is wrong about the Weight.** "No existing row has a hollow part … scaled above 1 at a parameter bound checked today": the Weight's art scale is 0.136734 · mass + 0.453070, 1.547 at its 8 kg bound (`elements/Weight/WeightElementDeclaration.cs:20`, `:33`), and its bands and eye are tori. S1 loosens their guard from 2e-6 to 3.1e-6 there. Harmless, and a relaxation cannot turn a pass into a failure. Fix: "The Weight's tori are scaled 1.547 at its 8 kg bound, where S1 allows 3.1e-6 in place of 2e-6; no other existing hollow part is scaled above 1 or drawn above 2.5 m." Classification: test.
- **N2. S2 does not define "drawn outer radius" per kind.** Fix, `:90`: "for a torus `Extents.X + Extents.Y`; for an open cylinder the larger of `Extents.X` and `Extents.Z`". Classification: test.
- **N3. Probe item 2 overstates the validator.** `BundleValidator.cs:408–428` checks only that a bound evaluates finite and positive (`:423–426`); the torus floors are enforced at compile-time admission. Fix: "…checked finite and positive at both parameter bounds (`BundleValidator.cs:421–427`); admission at the bounds is T3's proof." Classification: test.
- **N4. Axis agreement at scale 64 is unproven.** `AxisTol` is 2.9e-7 (`VisibleSolidColliderTests.cs:37`) and the art's quaternion is recovered from a basis scaled by 64. I expect it to hold, but if the 16 m case fails on the axis it is a planning finding, not a tolerance to widen. Add that sentence beside S1. Classification: test.
- **N5. T1 and T2 placement.** `Meet` is private to `VisibleSolidColliderTests` (`:682`) and the 17 mm bound is that class's `Penetration` (`:412`). Putting the two cases in `TennisBallTests` means a second copy of both. Either place the theory in `VisibleSolidColliderTests` (already owned) or keep the restatement and say so. `TennisBallTests.Ball` has no turn argument (`:10–14`); the test builds the instance with `WorkshopInput.Generic(…, qx: sin 22.5°, 0, 0, qw: cos 22.5°)`. State the sign: a positive turn about X stands the seam upright; a negative one lays it flat and T1 fails 62.5 mm low, which is the wrong-pose control. The default Wall top is 0.25 m thick (`elements/Wall/WallElementDeclaration.cs:30`), wider than the 125 mm seam. Classification: test.
- **N6. Cannon seating depends on the ball's turn between 0.407 and 0.42 m.** In the placed pose the seam's wide axis is X; a ball that has rolled may present it across the bore. A ball in that radius band then seats or stays `Loading` by orientation. Record this in `CAT-064-tennis.md` with the window, as task 6 already plans. I did not analyse `ContainsMaterial` for the seam along the bore. Classification: product behaviour to record.
- **N7. T3's control.** The cited test proves the tube floor (`CompoundMassAndRollingTests.cs:198–214`: `BitDecrement(1/512)` is refused). It does not step below the 1/16 m centreline floor. The radius parameter already refuses below 1/16 m, so no further control is needed; say so in the row. Classification: test.
- **N8. Part A's planned tests use Tennis probes of radius 0.1 m and 0.0625 m** (`spec-elements-visible-solid-remainder.md:172`, `:176`, `:181`), one of them starting inside the Fan's ring. They are existing tests by the time B2 runs. With the old thin seam a 0.1 m ball would be inadmissible, which supports D1. Add them by name to the T5 row so the scoped run is known to cover them.

## Effect of main moving to `fa86969f` (Baseball added)

- The package count is 45, not 44 (B3).
- `VisibleSolidColliderTests.cs` was not changed on main, so the measured line numbers in that file still hold until part A lands. The Baseball has no row; its two stitch rings stand 2.6 mm proud without a collider. That belongs to the Baseball repair, not to B2.
- `BaseballTests` uses the Tennis ball as target and as striker at a 0.51 m spacing (B2, item 2), and compares rebound peaks with it (`IdenticalDropDistinguishesEveryRetainedPreset`, a spinless drop on felt: not affected).
- `TODO.md`, `deferred-work.md` and `docs/planning/invest/delivery-batches.md` changed on main; B2 edits all three. The contract already says it is replanned from the revision that publishes part B, which covers this.
- B2 and the Baseball repair both edit `VisibleSolidColliderTests.Specs` and both touch the Tennis-and-Baseball impact scene. They must be serialised, and AC1's case count must be written relative to the baseline (B3). Add the Baseball repair to "Running beside other work" (`:181`).
- Nothing in the Baseball commit changes the hooks, `tools/anvil`, the loop protocol or `docs/delivery-workflow.md`.

## Decisions for the owner

None blocks. For information:

1. The seam's look changes: twice as wide and 2.6 times as proud at the default size (B4).
2. Above a 0.32 m radius the seam is more than the "about 1 cm" the owner accepted, in the same proportion as at the default size; 0.5 m at the 16 m ball.
3. A Tennis ball rolling on the bench bumps over its seam twice per turn, as the Basketball does.
4. A turned Tennis ball can briefly lose a quarter of a fan's push when its seam shadows a sample, as the Basketball does. Removing that for all seamed balls would be an engine slice.

## Evidence limits

- No build and no test run. The refusal in B2 items 1 and 2 is predicted from the placement code and the geometry; the risk in item 3 is an estimate; item 4 is a judgement that the fixtures should not carry the ring, not a predicted failure.
- Godot's rounding of `Basis.Scale` at scale 64 was reasoned, not executed.
- Parts A and B are not published; line numbers and the 40-case baseline were not re-measured against them.
- Not opened: the pressure plate's load rule, `ContainsMaterial`, the solution file's membership of `tools/BundleElements.Tests` (where `BundleRoundTripTests` lives), and the data of `AnUnrepresentableHollowColliderIsRefusedAtAdmission`.
