# Planning audit 6: ELEMENTS-VISIBLE-SOLID-REMAINDER part D

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-d.md` (5,998 words by `wc -w`; limit 6,000), status `blocked` on the engine programme. Checkout `spec-drafts`, main `0959e883` (code unchanged since `25df3cd5` for every path cited). Binding decisions read: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` (22, 24h, 33, 37-42 applied). Fifth audit: `audit-part-d-5.md`. Nothing was built or run. Every position below was recomputed from the `.tscn` scenes and declarations under `elements/`.

## Verdict: Fail

The fifth audit's F1 (M8 for the Wound spring socket), F3 (five wrong statements) and R1-R7 are resolved in substance, and decision 42's five new floating solids are seated correctly (table below: every seating, port and rim move recomputes). The Fail is for four things: (F1) the Tennis-ball drops (M2 and the Wound spring socket) are specified to read a rebound that cannot occur, because the Clutch and the Wound spring have static restitution 0, so the fifth audit's F2 is not resolved in substance; (F2) two test files that assert moved positions are in no owned scope, and a Code Map sentence says there are none; (F3) one sentence inside `<intent-contract>` (the Windmill hub clearance) is wrong; (F4) the *present, untested* revolute-filter capability is "tested" only by a read of a declaration field, and probe item 1 and item 5 cite no exercising test. With F1-F4 and R1-R7 applied the contract is "Scoped Pass (ready, blocked on the programme)". The re-audit can be limited to the diff.

## Fifth-audit findings: status

| Fifth-audit finding | Status | Evidence |
|---|---|---|
| F1 Wound spring socket drop cannot discriminate | Resolved in substance (Basketball case removed, Tennis case added). The Tennis case itself has F1 below | Ring contact at 0.4978 m recomputed: (sqrt(0.62^2+h^2) - 0.49)^2 + 0.15^2 = 0.34^2 gives h = 0.49782; socket stop 0.505; a 0.0625 m ball cannot reach the ring 0.15 m behind |
| F2 M2 stop read from samples, bounds borrowed | NOT resolved: the new reading (arc meeting) needs a rebound; see F1 here | `ClutchElementDeclaration.cs:89`, `WoundSpringElementDeclaration.cs:28`, `WorkshopConstraintSolver.cs:117`, `:590` |
| F3 five wrong statements | Four resolved; the hub clearance (item 5) is restated wrongly: see F3 here | Items 1-4 recompute: rule text, lever 0.203/0.093, ActivationIn 0.198/0.035, blade x -0.205 to -0.035 |
| R1 revolute filter premise and tier line | Resolved in wording (`BodiesCollide` false at `WoundSpringElementDeclaration.cs:48`, filter `WorkshopPhysicsEngine.cs:884-888`); the planned test is a declaration pin: F4 here. Tier line says "probe item 7" but the probe has six items: R1 here | |
| R2 CAT-070 and batch row | Resolved (CAT-070:52, :55, :121 added; `delivery-batches.md:251` carries decisions 39 and 42). CAT-070:38 is still missing: R2 here | |
| R3 half-moved port protections | Resolved (`BundlePortsTests`, BUNDLE011-013 named, filter has `BundlePortsTests`) | |
| R4 scene mechanics | Resolved (`col_axle` order, `col_Axle` under `Visual`, lever band) | `clutch.tscn:107-109`, `wound_spring.tscn` `Shaft` node |
| R5 commit path for the disc | Resolved (`git add -N` of `.obj` and `.import`; generated import) | `elements/ElectricalNand/printed-disc.obj` and its `.import` are tracked; `tools/anvil/CheckStagedTests.cs` is on main |
| R6 retry condition names B and C | Resolved | |
| R7 test wording | Resolved | |
| O4 unowned floating solids | Resolved by coordinator decision 42; no further floating solid found in the eight packages (stud, lamp, base, tower, nacelle, hub, rod, indicator, breech all overlap or touch their support; the Wound spring's `Mark`, `ChargeIndex` and `Spoke` and the pusher's travel marks are part B's prints) | |

## Blocking findings

### F1. M2 and the Wound spring socket drop read a rebound the Clutch and the Wound spring do not produce
- Where: contract line 54 (M2), line 80 ("bounds as M2" and the Wound spring socket case), line 163 (tolerance paragraph), lines 167-168 and 174 (table).
- Defect: the intent says the contact height is read "where the free-flight arc through the last falling commit meets the arc through the first rising commit", and the tolerance paragraph derives the lower bound 5.4 mm from a pair restitution e = 0.85 (0.85/1.85 x 2.43/480 = 2.3 mm). The pair restitution is the product of the two bodies' (`WorkshopConstraintSolver.cs:117`: `c.Restitution = c.ColA.Restitution * c.ColB.Restitution`). The Tennis ball's is 0.85 (`TennisBallElementDeclaration.cs`, `SceneBodies` `new(".", new(.85f, ...))`, parameter `bounce`), but the Clutch's static body is `new(".", new(0f, 0.3f, 0f, 0.1f))` (`ClutchElementDeclaration.cs:89`; CAT-018:175 "restitution = 0") and the Wound spring's is `new(".", new(0f, .3f, 0f, .1f))` (`WoundSpringElementDeclaration.cs:28`). Product 0, so `Bounces` (`WorkshopConstraintSolver.cs:590`, `c.Restitution != 0f`) is false and the ball does not rise: there is no "first rising commit", and the helper as specified has no arc to meet (`AssertDropOnto`'s own `Assert.True(vOut is not null, "did not rebound")` would fail if copied). Part B's contract states the same fact for its parts (line 220: "static restitution ... 0 (the others) ... asserts the stop only") and reads the first landed read. The 5.4 mm lower bound is a 2.3 mm bias for a rebound that does not exist.
- Consequence: M2's main, control and boundary cases and the Wound spring socket case cannot be implemented as written; an implementer would either fit the bounds to output (forbidden) or return to planning.
- Fix, intent row M2 (line 54), Expected column: replace with: "Its first fall ends on the axle: the ball's height at its first committed read after landing (the first read whose downward speed has fallen below half the impact speed; the Clutch's static restitution is 0, so the product with the ball's bounce is 0 and the ball does not rebound) lies between 3.1 mm below and 5 mm above the axle's top plus the ball's radius (0.055 + 0.0625 = 0.1175 m above the part origin); it may then roll off either side".
- Fix, tolerance paragraph (line 163), replace from "The drops read the contact height" to "5.4 mm; the upper bound": "The drops read the first landed read, as part B's low-restitution theory does (landing detected as `VisibleSolidColliderTests.AssertDropOnto` detects it, `y - rest < .05 && vy > -.5 * vIn`), because both statics have restitution 0 and the ball cannot rise after the stop, so the read is the stop. The lower bound is the envelope penetration, 5 % of the ball's 0.0625 m radius (3.1 mm; `docs/gpu-f32-physics.md:368`); the upper bound". Keep the sentences about a ball passing through the axle and about the helper, and delete "plus the arcs' meeting bias ... 5.4 mm".
- Fix, line 80: "bounds as M2" stays; add "(restitution 0, first landed read)" after "release 0.3 m above the contact".
- Fix, line 168 and 174: no change except the words "contact height" in any cell become "first landed read".
- Classification: test (the intent row M2 changes with it; this is a wrong specified reading, not wrong behaviour).

### F2. Two test files assert positions this slice moves, and a Code Map sentence says none do
- Where: Code Map lines 83-84 (literals list) and line 90 ("The only port-position assertions are the Counter's and the Pressure plate's").
- Defect and consumers missed:
  - `CuriousContraptions.tests/ClutchTests.cs:178` and `:185`: `Assert.Equal(new Vector3(-0.65f, 0f, 0.55f), driveIn.LocalPosition)` and the same for +0.65. The Clutch ports move to z 0.51; both assertions fail. (The contract lists `ClutchTests.cs:967`, `:971-985`, `:1504-1505` but not these.)
  - `CuriousContraptions.tests/JointFrameTests.cs:47-51` (comment and `Assert.InRange(MathF.Abs(motorMovingZ - 0.54f), ...)`) and `:204-209` (comment and `expectedMotorPos = motorPos + Transform(new Vector3(0f, 0f, 0.54f), ownerRot)`): the Motor hinge's base offset moves 0.54 -> 0.495; both fail. The contract's literal list names `MotorTests.cs:34` only.
- Consequence: the full round fails on an unowned file, or the implementer edits a file outside the owned scope and breaks AC3.
- Fix: in the literal list (line 83) add "`ClutchTests.cs:178`, `:185` (declared `DriveIn` and `Drive` positions z .55 -> .51), `JointFrameTests.cs:47-51`, `:204-209` (Motor hinge base z .54 -> .495, comments included)"; add `CuriousContraptions.tests/JointFrameTests.cs` to the Owned scope list and to the Verification filter (`FullyQualifiedName~JointFrameTests`); replace line 90's last sentence with "The port-position assertions are the Counter's (`CounterTests.cs:121-124`, `:154-157`), the Pressure plate's (`PressurePlateTests.cs:88-91`, `:113-115`, unaffected) and the Clutch's (`ClutchTests.cs:178`, `:185`)". The `rg` re-run at line 83 already matches `.54f`; say it also matches `0.54f` and `0.55f` (the Clutch ports), and run it over `CuriousContraptions.tests tools content`.
- Classification: test.

### F3. A wrong statement inside the intent contract: the Windmill hub clearance
- Where: line 39, "its z range 0.275 to 0.395 clears the hub (radius 0.14 at x -0.12) by 0.31 m".
- Defect: 0.31 m is the distance from the hub's centre to the wheel's nearest point (wheel point (0.0183, -0.0374, 0.275); hub centre (-0.12, 0, 0): sqrt(0.1383^2 + 0.0374^2 + 0.275^2) = 0.3099). With the hub's radius 0.14 the clearance is 0.17 m. The fifth audit asked for 0.31 as the centre distance (its F3 item 5); the author's rewording turned it into a clearance next to the radius.
- Consequence: the intent contract becomes read-only with a false number.
- Fix: replace with "clears the hub (radius 0.14 at x -0.12) by 0.17 m, its nearest wheel point being 0.31 m from the hub's centre".
- Classification: wrong specified behaviour (descriptive text).

### F4. The *present, untested* capability is tested by a declaration pin, and two *present* probe items cite no exercising test
- Where: probe lines 101, 105 and 106; line 82 (M7 premise).
- Defect:
  - Item 6 (the revolute `BodiesCollide` filter, `WorkshopPhysicsEngine.cs:884-888`): the protocol says a *present, untested* capability's slice adds the test. The planned test reads `RevoluteHingeDescriptor.BodiesCollide == false` in `WoundSpringArtTests`. That pins the declaration, not the engine behaviour that matters (that no pair is formed between the static axle and the spinning wheel, so no contact, no friction). The prismatic twin has a behavioural test (`WorkshopPhysicsLawPrismaticTests.TheConnectedCollisionPolicyFiltersOnlyItsDeclaredPair`, `:298`); the revolute loop has none, and `LawScene.Revolute` already takes the policy (`WorkshopPhysicsLawTests.cs:288`; `RevoluteHingeFrictionTests.cs:42-45` builds such scenes). The winding lessons would not catch a wheel dragged by a zero-width contact.
  - Item 1 (a static cylinder from a hidden proxy) cites `SceneGeometryExtractor.cs:15`, `:104` and the Clutch coil scene node, no test. The test exists: `tools/BundleElements.Tests/SceneGeometryExtractorTests.cs:79` (`CylinderShapesAndUntaperedMeshesBakeAsTrueCylinders`).
  - Item 5 (hinge base offset, rim and port are declared values) cites a type, no test. Exercising tests exist: `JointFrameTests.cs:51`/`:209` (the Motor base frame is honoured by the compiler) and `BundlePortsTests.cs:323-375` (declared ports equal locators).
- Consequence: an untested engine behaviour carries the Wound spring axle's correctness; two probe items fail the "symbol and exercising test" rule.
- Fix: (a) add to the owned scope a revolute twin of the prismatic test in `CuriousContraptions.tests/RevoluteHingeFrictionTests.cs`, named `TheRevoluteConnectedCollisionPolicyFiltersOnlyItsDeclaredPair`: a static box and a dynamic body overlapped on a revolute with `ConnectedCollision.Disabled` is unmoved by contact, with `Enabled` it responds, and an unrelated ball collides under both (same control and boundary as the prismatic test; the only exact value is the filtered body's zero speed, exact by contract because a filtered pair exerts nothing). AC2's total becomes baseline + 8. Keep the declaration read in M7 as the declaration half. (b) Item 1: add the cited test. (c) Item 5: add "`JointFrameTests.cs:51`, `BundlePortsTests.cs:323-375`". (d) Item 3 (the axle never meets the shafts) is a clearance derivation, not a capability: delete it from the probe or move it to an M3 geometry note, and recount ("5 present, 1 present-untested" becomes "4 present, 1 present-untested").
- Classification: test.

## Required fixes (outside the intent)

### R1. Tier sentence points at a probe item that does not exist
- Line 67 says "probe item 7 is *present, untested*"; the probe (lines 99-106) has six items. Fix: "the last probe item (the revolute filter) is *present, untested*". Classification: test (record).

### R2. Documents that carry the moved positions: lines missing from the owned scope
- Line 86 lists CAT-018:53-56, :193, :201, :205 and the like but not the lines that state the positions and ports. Add, each updated to the seated value: `CAT-018-clutch.md:26` (shafts at (±0.65, 0, 0.43)), `:35`, `:36` (ports z 0.55); `CAT-042-motor.md:22` (rotor 0.54), `:45` (port 0.65 and rim 0.605), `:111` (rotor at 0.54); `CAT-057-reverse_transmission.md:29` (wheels z 0.32), `:38`, `:39` (ports 0.4); `CAT-070-windmill.md:38` (Drive port 0.52, rim 0.44); `CAT-039-linear_pusher.md:34` (ports), `:45` (foot at y -0.5), `:51`, `:54`; `CAT-071-wound_spring.md:27`, `:46` (rails 0.13 wide), `:50`, `:54`, `:57` and the `ActivationIn` port row; `CAT-016-cannon.md:32`, `:49`, `:53`, `:55`, `:93`. Replace "(re-read for their lines)" at line 86 with these lines. Classification: test (planning record).

### R3. `content/puzzles.json` count is wrong and hides the real consumer
- Line 93: "54 lines name `linear_pusher`". Measured on this checkout: `grep -c linear_pusher content/puzzles.json` = 0, `grep -c '"counter"'` = 0, `"clutch"` 0, `"cannon"` 0, `"windmill"` 0, `"motor"` 49, `reverse_transmission` 2, `wound_spring` 3 (the 54 is those three summed). Fix: "No lesson places a Linear pusher, Counter, Clutch, Cannon or Windmill; 49 lines name the Motor, 2 the Reverse gear and 3 the Wound spring (`grep -c` on `content/puzzles.json`). The Motor's rotor, port and rim move 0.045 m, so the lessons that mechanically link a Motor are the consumers: they connect by port id and `MechanicalLinkValidation.Rim` checks no distance, so the full round's lesson-solution tests are the check; a lesson whose solution rests cargo on a Motor's rotor returns to planning." Delete the sentence about the pusher's raised foot and cargo (no lesson uses it; test projects do, and the full round covers them). Classification: test (record).

### R4. Word budget has no headroom
- 5,998 of 6,000 words. F1-F4 and R1-R3 add roughly 250 words. Move the "Planning verdict" paragraph (line 69, audit history, about 190 words) and the Concurrency and Design Notes duplicates to the run log, citing it in one line, before applying the fixes. The intent contract, constraints and acceptance rows are not cut.

### R5. Tolerance wording of M7/M9 "equal"
- M7 says port positions "equal the seated locators" and the foot top "equals" the barrel's bottom. State the tolerance: ports within 1e-5 m (`BundlePortsTests.cs:350`), seats within the 0.5 mm touch band. Declared and authored constants are f32 literals, but the sentence reads as an exact-float pin.

### R6. Windmill drop position
- The Code Map line for `WindmillOutput` gives only the face height. Add "X 0.35, Z 0.1 unchanged from part C" (the position the 0.393 m blade clearance below is computed for).

## Seating, port and rim moves recomputed

Part-local metres; "Mine" is recomputed from the scene or declaration.

| Node (with proxy, locator, port) | Delivered | Contract | Mine | Verdict |
|---|---|---|---|---|
| Counter foot | (0, -0.72, 0); top -0.64 | (0, -0.705, 0) | housing bottom -0.625 = -0.705 + 0.08 | holds (`counter.tscn:64`, mesh 1.8 x 0.16 x 0.85) |
| Counter face | (0, 0, 0.35) | (0, 0, 0.345) | housing front 0.325 = 0.345 - 0.02; dot lamps front 0.5 vs face front 0.365 | holds |
| ActivationIn socket | (0, 0.72, 0.1) | (0, 0.625, 0.1) | r 0.075 on top face; top 0.7 | holds |
| ActivationOut | (0, -0.72, 0.1) | (0, -0.705, 0.1) | y -0.78..-0.63 in foot -0.785..-0.625; z 0.025..0.175 in +-0.425 | holds |
| PowerIn, Supply | (-+0.93, 0, 0) | (-+0.825, 0, 0) | on side faces; reach 0.9 = foot | holds |
| Counter reach (M5) | x 1.005 | rows 6.2/6.0, 3.6/3.4, 3.2/3.0 | limits x +-7, z +-4 (`PlacementPose.cs` `Inside`/`Within`); reach x 0.9, z +0.5 (dot lamp 0.4 + 0.1); 7.1/6.9, 4.1/3.9, 4.1/3.9; all on the 0.1 grid; the x 6.0 row fails if unseated (7.005) | holds |
| Pusher upper sockets | (-+0.45, 0.4, 0.45) | (-+0.45, 0.325, 0.325) | barrel 1.2 x 0.65 x 0.65: top 0.325, front 0.325; indicator (-0.4, 0.2, 0.34) distance 0.13546 vs radii 0.135 | holds; two-axis move stated |
| Pusher foot | (0, -0.5, 0); top -0.4 | (0, -0.425, 0) | top -0.325 = barrel bottom; footprint +-0.8 x +-0.5 holds +-0.6 x +-0.325; head bottom -0.25 and ring -0.255 clear by 0.075 / 0.07; lower sockets (y -0.4, z 0.5) on the foot's front face (y -0.525..-0.325) | holds |
| Wound spring ActivationSocket | (0.62, -0.55, 0.55) | (0.62, -0.55, 0.1) | rail front z 0.1; x 0.62 inside 0.49..0.665 (overhang 0.02) | holds |
| Wound spring Latch | (0.48, 0.25, 0.5) | (0.48, 0.25, 0) | wall band at x 0.48: abs z <= 0.0985 (contract 0.098); lever x 0.37..0.59, z +-0.05, outer end inside the widened rail; pitched -0.65 rad: corner x 0.362, 0.042 from the head's 0.32 | holds |
| Wound spring wheel and axle | wheel z 0.45..0.55 at (-0.62, -0.55) | stays; axle r 0.04, z 0.1..0.45, centre z 0.275 | left rail x -0.665..-0.49 holds the axle (-0.66..-0.58); wheel back 0.5 - 0.05 = 0.45; alternative at z 0.1: (-0.408, -0.4) radial 0.4197 in the wall band | holds |
| Wound spring rails | 0.13 wide, inner face 0.535 | 0.175 wide, centres +-0.5775 | outer faces 0.665, inner 0.49 = sleeve outer radius; sleeve touches along z = 0 over y -0.4..0.85 (rail top 0.85); marks/index/socket/axle stay in 0.49..0.665 | holds |
| Cannon sockets | (-0.45, -0.55, 0.55), (0.15, -0.55, 0.55) | y -0.65 | foot top -0.65 (foot 0.2 at y -0.75); collar radial 0.851 vs 0.65; ActivationIn sphere y -0.72..-0.58, z 0.48..0.62 is behind the track's front z 0.655 and below its top -0.54 (hidden from the front; visible from above/behind where z < 0.585) | holds; see owner item O3 |
| Cannon ChargeTrack, ChargeBar | y -0.55 | y -0.595 | track y -0.65..-0.54 (bottom on the foot), bar 0.07 high inside it | holds |
| Windmill Output | (0.25, -0.1, 0.44) | (0.25, -0.1, 0.335) | wheel 0.12 along z: back 0.275 = nacelle front (0.55/2) | holds |
| Windmill Drive port, rim | 0.52, 0.44 | 0.415, 0.335 | port 0.02 in front of face 0.395; rim at the wheel centre | holds |
| Windmill blade/hub clearance | | "0.045 m" blades, "0.31 m" hub | blade x -0.205..-0.035 (0.0375 cos30 + 0.105 sin30 = 0.085); wheel from x 0.01: 0.045; hub: 0.31 centre distance, 0.17 clearance | blades hold; hub wording F3 |
| Clutch shafts | z 0.43 (back 0.365) | 0.39 | blocks 0.35 x 0.7 x 0.65: front 0.325 = 0.39 - 0.065; overhang 0.105 each side in x, 0.03 above (shaft +-0.28 vs block top 0.25); plates x +-0.16..0.34 vs shaft x from 0.37 (0.03 clear); ports 0.55 -> 0.51, rims 0.495 -> 0.455 (port 0.055 off the rim, as delivered) | holds |
| Clutch axle | no collider | cylinder r 0.055 x 1.3 along x | `Visual_Axle` r 0.055, height 1.3; visible spans x 0.0325..0.16 and 0.34..0.475; shaft clearance 0.325 - 0.055 = 0.27 | holds |
| Motor rotor | z 0.54 (back 0.475) | 0.495 | housing front 0.43 (cylinder 0.86 along z); rim 0.605 -> 0.56; port 0.65 -> 0.605; lamp radial 0.4252 - 0.075 - 0.3 = 0.0502 | holds |
| Reverse gear wheels | z 0.32 (back 0.25) | 0.24 | housing front -0.08 + 0.25 = 0.17; wheels x +-0.36 +- 0.32 inside +-0.7, y +-0.32 inside +-0.325, 0.08 apart; ports 0.4 -> 0.32, rims 0.39 -> 0.31; disc mark radial 0.21 + 0.07 = 0.28 < 0.32 | holds |
| Prints | spokes z 0.08 (35 mm), index 0.09 / mark 0.1, motor 0.067 | 0.0652, 0.0702 / 0.0704, 0.0652 | faces at 0.065 (heights 0.13), 0.07 (0.14), 0.065 (0.13): 0.2, 0.2/0.4, 0.2 mm; inside the 0..0.5 mm band; mark over the bar by 0.2 mm | holds |

Hinge behaviour: every hinge that now seats a back face on its housing (Clutch x2, Motor, Reverse gear x2) and the Wound spring wheel declares `BodiesCollide` false (`ClutchElementDeclaration.cs` `ShaftHinge`, `MotorElementDeclaration.cs` hinge, `ReverseTransmissionElementDeclaration.cs` `Wheel`, `WoundSpringElementDeclaration.cs:48`), so the engine forms no body-housing pair (`WorkshopPhysicsEngine.cs:884-888`); the contract's premise for decision 42 holds. Declaration line citations (`ClutchElementDeclaration.cs:72-78`, `:114-115`; `MotorElementDeclaration.cs:52`, `:54`, `:92`; `ReverseTransmissionElementDeclaration.cs:27-32`, `:54`; `CounterElementDeclaration.cs:60-63`; `LinearPusherElementDeclaration.cs:47-48`; `WoundSpringElementDeclaration.cs:63-64`; `CannonElementDeclaration.cs:37-40`; `WindmillElementDeclaration.cs:46-50`) open at those lines.

## Drops recomputed
- Pusher (part B's `PusherSocket`, seated): ball over (-0.45, z 0.325); without the socket it stops on the barrel's top edge at 0.325 + 0.34 = 0.665 against 0.395 + 0.34 = 0.735: 0.07 lower, discriminates; the indicator is 0.535 away vertically.
- Windmill (part C's `WindmillOutput`, drop at X 0.35, Z 0.1 as part C has it): ball centre (0.35, -0.1, 0.735) in the Back pose; nearest blade corner (-0.035, -0.072, 0.66) is 0.393 away (> 0.34); guard plane 0.435; nacelle face 0.12 below: discriminates (R6 asks the contract to state X and Z).
- Clutch axle: control at x 0.4, z -0.2 clears the block face (0.475) by 12.5 mm and lands on the base top -0.4 (centre -0.3375); boundary at x 0.105 clears the coil face by 10 mm; both recompute. The reading method is F1.
- Wound spring socket (Tennis ball, Back pose, X 0.62, Z 0.55): stop 0.2275 against the rail face 0.1625 (65 mm); the ring (0.15 m behind) is out of a 0.0625 m ball's reach. Discriminates once the reading method is F1's.

## Checks 1-12
1. Ready for Development: actionable, logical and surface-anchored apart from F1, F2 and F4; complete; no TBD.
2. Intent vs source: CAT-020 repair 3 values equal the contract's Counter values; decision 39's list is covered once; decision 42's five solids are covered once with their ports, rims and hinge offsets; no acceptance dropped; "test total +7" is true of the contract as written (becomes +8 with F4).
3. Capability probe: see F4 and R1; no solver, ABI, wire or shared-presentation capability is built here.
4. Consumers: F2, R2 and R3; BUNDLE011-013, `BundlePortsTests`, `BundleRoundTripTests` named; no e2e TypeScript or `tools/Coverage` file reads a moved position.
5. Removed protections: the delivered floating positions and raised prints were guarded by nothing; M7 and M9 now guard them, and `PackageValidator` plus `BundlePortsTests` catch a half-moved port; `PlacementPoseTests` catches an unseated Counter reach. Nothing lost.
6. Commit path: the three untracked files (`HingeFacePrintTests.cs`, `printed-disc.obj`, its `.import`) are `git add -N` and staged; the test project compiles `*.cs` by glob (`CuriousContraptions.tests.csproj:77`); `.godot/` is rejected at `.githooks/pre-commit:26`; `CheckStagedTests.cs` is on main. `JointFrameTests.cs` and `RevoluteHingeFrictionTests.cs` are tracked: filter only, not the staging list.
7. Test discrimination: F1 and F4; M1, M4, M5, M7, M8 (pusher, Windmill), M9 discriminate with named controls; tolerances are stated (0.5 mm band, 10 micrometre planarity, 5 % penetration, 5 mm); no hashes, skips or mutants. Tolerance wording R5.
8. Engineering rules: forward-only (raised meshes and floating positions replaced, nothing kept beside them); no engine code; enums and f32 unaffected; no TypeScript.
9. Tier: engine is right (the candidate carries `TODO.md`; the revolute filter is *present, untested*). Delivery shape "one repair slice over eight packages" is a repair, not a Family slice or duplicate closure, which is acceptable; see O5.
10. Owner decisions: below.
11. Size: 5,998 words (R4); nine matrix rows, three acceptance criteria; one coherent repair (see O5 on splitting).
12. Concurrency: stated correctly (`VisibleSolidColliderTests.cs` with `ENGINE-ART-SOLIDITY` and parts B and C, `PlacementPoseTests.cs`, `CounterTests.cs`; add `JointFrameTests.cs` and `RevoluteHingeFrictionTests.cs`, with no queued slice). Blocked status: reason and retry condition are explicit (owner decision 22 and coordinator decisions 33, 39, 42; parts A, B, B2, C published; the thirteen programme keys `doc-engine-status` to `engine-soa-simd`, which exist at `sprint-status.yaml:586-598`). The contract cites decision 22 but not decision 3a's wording ("blocked on the engine programme (owner decision 3a)"); add "(owner decisions 3a and 22)" to the status line for consistency with the element specs.

## Decisions that need the owner
Each has a recommended default; none cuts scope or waives a gate, so the coordinator may decide under the standing delegation, but the owner should see them in the batch.
- **O1. The Wound spring wheel keeps its place and gains a static axle (a new solid and collider)** instead of being seated on the rail. Moving it puts its inner edge in the sleeve wall (0.4197 from the axis). Default: accept (coordinator decision 42).
- **O2. The latch lever sits at z 0 in the sleeve wall as a latch pin**: its outer 0.1 m is buried in the right rail and about 0.12 m shows through the glass. Default: accept.
- **O3. The Cannon's `ActivationIn` socket is hidden behind the charge track from the front** (visible from above or behind only). The connection chooser picks sockets by name. Default: accept (coordinator decision 42); alternative: seat it at x 0.5, beyond the track end.
- **O4. The Wound spring rails get 35 % wider (0.13 -> 0.175 m)** so the sleeve touches them; a visible silhouette change on a delivered part. Default: accept.
- **O5. One slice for eight packages.** The three hinge-body parts (Clutch, Reverse gear, Motor: prints, axle, seating, M1-M3, M9) and the five static-art parts (Counter, Pusher, Spring, Cannon, Windmill: M4, M5, M7, M8) share only the visible-solid theme; the contract is at its word limit. Default: keep one slice (coordinator decisions 33, 39 and 42 placed them together, and they publish once after the programme); the coordinator may split at the programme's end if review cost warrants.

## Evidence limits
Nothing was built or run. Stop heights, clearances and distances are rigid-body geometry from the scenes and harness constants, not simulated. The claim that the Clutch and Wound spring drops do not rebound rests on the declared restitution and the solver's product law and `Bounces` rule (read, not executed); part B's contract records the same behaviour for statics with restitution 0. The revolute connected-collision filter was read, not executed. Whether the Godot import step produces the disc's `.import` without a manual editor run was not tested. Parts A, B, B2 and C are not published on this main, so every reference to their proxies, rows and `Feature` cases was checked against their draft contracts only.
