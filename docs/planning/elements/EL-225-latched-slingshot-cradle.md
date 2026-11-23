# EL-225 · Latched slingshot cradle — named-identity spec

**Current delivery (22 November 2026):** [EL-225 ledger entry](../invest/named-elements.md#element-225), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Reworked 22 November 2026 after the independent audit (`docs/planning/elements/readiness/audits/elements-new-identities.md`, F3): the drawing load, the seat and the launch energy now close on this spec's own numbers. Propagated 23 November 2026 to the [element-spec decisions of 22 November 2026](readiness/owner-decisions-2026-11-22.md). Repaired 23 November 2026 after the second audit (`docs/planning/elements/readiness/audits/reaudit-group-1.md`, EL-225): the prongs are 1.9 m tall so a launched ball crosses them, the undrawn release answers Released (S2, S6) and S1 checks the band spans.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-225 · Latched slingshot cradle · Mechanical |
| Requirement anchor | [todo-396](../requirements.md#todo-396) (`sequence-task-379`) |
| Ledger entry / owner | [element-225](../invest/named-elements.md#element-225); slice `named-el-225`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a) and `ENGINE-JOINT-KINDS` (the one-way hold of a coordinate no transmission winds, and its release by a physical request bound to `ActivationIn`; engine-stories-index C2).** Delivery batches lists it in 1b; the probe finds the latch narrower than needed. Checked against Duplicate closure with the Wound spring (CAT-071): **not** a duplicate, because it is drawn by a rope, not wound through a Mechanical link, and it seats a ball in a cradle. |
| Planned package | id `slingshot`, directory `elements/Slingshot/` |
| Related | [CAT-071 Wound spring](CAT-071-wound_spring.md) (shape), [EL-063 Cable winch](EL-063-cable-winch.md), [EL-226 Hinged elastic catapult](EL-226-hinged-elastic-catapult.md), [EL-228 Zipline carriage](EL-228-zipline-signal-carriage.md) (same latch gap) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP` (a latched cradle and its seated ball at rest); then `ENGINE-JOINT-KINDS` (the one-way hold of the rope-drawn slider and its release by a physical request bound to `ActivationIn`; engine-stories-index C2), and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g). |

## 2. Declaration

Shape: the Wound spring's sprung slider, retention and release request (`elements/WoundSpring/WoundSpringElementDeclaration.cs:30`, `:55`, `:66`), without its winding shaft and transmission.

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `IPrismaticRetentionElement` (`:85`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement` (`SceneGeometry.cs:10`). The retention is declared in the form `ENGINE-JOINT-KINDS` delivers for a coordinate no transmission winds.
- **Frame (body 0, static).** Base 1.6 × 0.1 × 1.1 m and two prongs 0.1 × 1.9 × 0.1 m at z = ±0.5 m (clear gap 0.9 m), standing on the base at the travel-0 line so the launched ball passes between them. Prong height, worked (second audit, 23 November 2026): at travel 0 the floor's open front edge is 0.5 + 1.0 × sin 30° = 1.0 m above the base top; a ball of radius r leaves it with its centre r·cos 30° higher and r·sin 30° behind the edge, and climbs a further r·sin 30° × tan 30° before the prong line, so its centre crosses the prong line at 1.0 + 1.155·r m: 1.29 m for the Tennis ball (r 0.25), 1.39 m for the Basketball (r 0.34), 1.58 m for a 1.0 m ball (r 0.5; the trajectory's drop over that 0.15–0.25 m is under 0.01 m). Every crossing lies inside the prongs' 1.9 m height; the earlier 0.9 m prongs ended below every ball's path, so nothing could strike them.
- **Cradle (body 1).** An open-fronted cup: floor 1.0 × 0.05 × 0.88 m, back plate 0.06 × 0.5 × 0.88 m, two side walls 1.0 × 0.15 × 0.04 m (inner width 0.80 m). Mass 0.5 kg. Restitution 0.1, friction 0.4.
- **Slider.** The launch axis is the slider's base frame, **elevated 30° above the base** in the authored pose. Limits −`draw` to 0; spring `stiffness` toward 0, damping fixed 1 N·s/m; no drive. The back plate's lowest edge is 0.5 m above the base at travel 0 and 0.2 m above it at a 0.6 m draw.
- **Why the axis is tilted.** The ball must follow the cradle back as it is drawn. On a level floor nothing pulls it back; on a 30° floor gravity rolls it against the back plate (3.5 m/s² for a solid ball). The floor is 1.0 m long so a ball that lags during a fast draw stays on it (worst case below).
- **Retention.** Two parts, both of `ENGINE-JOINT-KINDS` (holds on a coordinate and their release; engine-stories-index C2). The hold: one-way, travel may go more negative and may not return until released (`readiness/batch-i.md` section 5 G1; summary gap G2). The release: a physical release request bound to `ActivationIn` frees it. **Re-arm distance 0.1 m, re-arm rest speed v_r 0.05 m/s and settle time t_s 0.5 s** (declared values of `ENGINE-JOINT-KINDS`' re-arm at rest: the rule is coordinator decisions 33 and 34, the values coordinator decision 37, 23 November 2026): a released cradle re-arms when it comes to rest (its speed below v_r for t_s) within 0.1 m of its hold position, travel 0 (coordinator decision 31, 23 November 2026; `ENGINE-JOINT-KINDS`). 0.1 m exceeds the largest rest it must catch, the seated sag 0.061 m (item 1), by 0.039 m, and stays below the smallest authored `draw`, 0.2 m, so a drawn cradle is never mistaken for one at rest near its hold.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `stiffness` | Real, N/m | 60–250 | 120 | Reject, build-only | At the default an 8 kg Weight (the Weight's maximum, `elements/Weight/WeightElementDeclaration.cs:33`) holds the full default draw with 18 % to spare |
| `draw` | Real, m | 0.2–0.8 | 0.6 | Reject | The draw limit; the Wound spring's stroke range |

| Port | Domain | Direction, role | Position (m) |
| --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional, Tie, load attachment on the cradle | Back of the back plate, on the axis |
| `ActivationIn` | Signal | Input, ActivationIn, Pulse | On the base (0.6, 0.1, 0.55) |

The rope port is declared `PortDirection.Bidirectional` as on every delivered rope part (`CuriousContraptions.Abstractions/PortDescriptor.cs:9`; `elements/ImpactLever/ImpactLeverElementDeclaration.cs:44`).

- **Presentation.** Cradle body binding; two bands as spans from the prong tips to the cradle (they visibly stretch and return); latch from `CommittedRetentionReleased`; charge bar from `CommittedStoredFraction`.
- **Palette.** Motion, "Slingshot"; order 132 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Base and prongs | Boxes, cream `#fff8e9` (owner decision 4) | Solid, body 0 | nothing |
| Cradle floor, back plate, side walls | Boxes, coral `#d96b58` | Solid, body 1 | body pose |
| Two bands | Thin cords, 0.02 m, in the thin-wire class of owner decision 1 (non-solid; the slider's declared spring carries the load), routed from the prong tips (2.0 m above the base top) to the cradle's outer side walls; both ends lie at \|z\| ≥ 0.44 m, so the bands stay outside the path of any ball up to 0.85 m across (\|z\| ≤ 0.425 m); the slice confirms the routing in its scene | Decorative: moved by a binding | presentation binding (span from prong tip to cradle) |
| Latch and charge bar | Inside the base outline | Decorative: enclosed, printed flat | presentation binding (committed retention state, stored fraction) |

Icon: a Y fork with a band and a cup. It differs from the Wound spring (a vertical coil) by the fork and bands.

### Behaviour and numbers

Hand calculation at the defaults (k = 120 N/m, draw 0.6 m, Basketball 1 kg, cradle 0.5 kg, 30° axis, g = 9.81 m/s²):

1. **Weight along the axis.** (1 + 0.5) × 9.81 × sin 30° = 7.4 N. With a ball seated the static sag is 7.36 ÷ 120 = 0.061 m; empty, 0.5 × 9.81 × sin 30° ÷ 120 = 0.020 m. The hold keeps the deepest point of the cradle's first fall from travel 0 at Run start (in the delivered law the retained bound follows inward travel and is the coordinate's upper stop, `CuriousContraptions.Simulation/SimPrismaticRetention.cs:78`, `PrismaticConstraintSolver.cs:78`). With damping ratio 1 ÷ (2√(120 × 1.5)) = 0.037 the first swing overshoots by 0.89 of the sag, so the seated cradle is held near 0.061 × 1.89 = 0.116 m with ½ × 120 × 0.116² = 0.8 J; empty (ζ = 0.065, overshoot 0.81) near 0.037 m with 0.08 J. Either way the stored energy is at most 2(m·g·sin 30°)² ÷ k: 0.90 J seated, 0.10 J empty. The delivered retention answers a release request Empty only within `ActuatorStopSlopMetres` (5e-4 m, `CuriousContraptions.Abstractions/PhysicalTolerance.cs:14`) of travel 0 (`SimPrismaticRetention.cs:44`), so a pulse on the undrawn cradle answers **Released**. The released spring lifts cradle and ball back toward the 0.061 m equilibrium and, damped, to about 0.01 m short of travel 0: lifting 7.36 N through 0.105 m takes 0.77 J of the 0.8 J, so nothing is launched and the ball stays in the cup (row S2; section 6 item 5).
2. **Drawing.** A rope from `Tie` runs back along the axis over a Pulley to a hanging Weight. Holding a draw x needs k·x − 7.4 N. Full draw: 120 × 0.6 − 7.4 = **64.6 N**; an 8 kg Weight gives 78.5 N, so it reaches the stop with 13.9 N to spare. A default 4 kg Weight (39.2 N) holds (39.2 + 7.4) ÷ 120 = 0.39 m. A Weight let go from rest overshoots its static draw (up to twice, less damping) and the one-way latch keeps the furthest point, always bounded by `draw`. Settings an 8 kg Weight cannot hold (k·`draw` − 7.4 N above 78.5 N) need a drop-draw or the Cable winch (EL-063, later); the latch keeps whatever was reached.
3. **The drawing rope must let go before release.** While the 8 kg Weight hangs on `Tie`, the net force on the cradle is 120·x − 7.4 − 78.5 N, negative for every x up to 0.6 m: a pulse frees the latch and nothing launches. The rope must be cut, or paid out by a winch, first. A Weight resting on a floor does not help: the rope goes taut again as soon as the cradle moves.
4. **Stored energy.** ½·k·x² = ½ × 120 × 0.36 = **21.6 J**.
5. **Launch, rope cut.** Lifting 1.5 kg through 0.6 × sin 30° = 0.3 m takes 4.4 J. Damping takes about 1 N·s/m × 3.7 m/s (mean) × 0.6 m = 2.2 J. Kinetic energy at travel 0: 21.6 − 4.4 − 2.2 = 15.0 J in 1.5 kg, so **4.5 m/s** (4.8 m/s with no damping, from v² = (k·x² − 2·m·g·x·sin 30°) ÷ m, m = 1.5 kg). The cradle stops at 0 and the ball flies on between the prongs at 30° elevation: range on the level about 4.5² × sin 60° ÷ 9.81 = 1.8 m, apex 0.26 m above the release point.
6. **Seat during a fast draw.** Worst case, 8 kg let go from rest: the cradle reaches the 0.6 m stop in 0.39 s (motion about the 0.675 m balance point at √(120 ÷ 8.5) = 3.8 rad/s); the ball, rolling at about 3 m/s², has moved 0.23 m and lags 0.37 m. Its contact point moves from 0.35 m to 0.72 m along the 1.0 m floor, then it rolls back against the plate. A test waits for the ball to rest before the pulse.
7. **Balls.** Any ball up to 0.85 m across passes the 0.9 m prong gap; every preset (0.50–0.70 m) seats and launches. A wider ball strikes the prongs.
8. After release the store is empty. Another pulse does nothing until a rope draws it again: no free recharge. A cut rope leaves the cradle latched at its present draw. Direction is the placed orientation; there is no aiming during Run. Rotated so the axis is level or points down, the ball rolls out of the open front: physical, and a Build-time matter.
9. **Ropes.** Ropes are solid strands under owner decision 1 once `ENGINE-CONNECTION-MATERIALS` (solid strand) is delivered; the delivered rope draws without a collider, a recorded visible-solid defect owned by that story. Every rope route in these rows runs clear of other colliders, so the rows hold either way. Route condition: the drawing rope lies on the axis line behind the cradle. Taking the floor's open front at the prong line at travel 0 and the axis along the floor, the back plate sits (1.0 + 0.6) × cos 30° = 1.39 m behind the prongs at full draw and the axis line reaches the base's top 0.2 ÷ tan 30° = 0.35 m further back, about 1.74 m behind the prongs: beyond the 1.6 m base wherever it lies under the prongs, so the rope crosses no collider of this part. The Pulley stands beyond the base with its rim on that line, below the base's top, so the rows set the slingshot at the edge of a raised Wall deck and the Weight hangs clear of it. The slice's scene confirms this geometry.
10. Whether a latched cradle, or a ball seated in it, may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Sprung slider storing elastic energy, on a tilted base frame | present | `MechanicalContractValues.cs:109` | `WorkshopSpringboardTests` (class); `PrismaticRetentionTests.HeldRetainedSpringConvergesWithoutInventingEnergy`; tilt: `DeclaredPrismaticDriveTests.AnUnpoweredInvertedOrTiltedMountRestingInItsInteriorWithNothingInTheWayIsNotBlocked` |
| Retention and release by a pulse, with empty and already-released results | present | `PrismaticRetentionValues.cs:9` | `WoundSpringControlTests.PackageTriggerReportsEmptyThenReleasedThenAlreadyReleasedAndRearms` |
| A rope tied to a slider body | present | rope domain | `RopeTests.ASuppliedPrismaticDriveHeldByARopeHoldsShortOfItsTargetAndCommitsRest` |
| A rope cut during Run, restored on Reset | present | rope domain | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` |
| Passive charging observed at law level | present | same | `PrismaticRetentionTests.PassiveGravityChargeHasItsOwnBudgetAndNoMotorWork` |
| **Retention declared without a winding transmission (one-way hold of a rope-drawn coordinate), re-arming at rest within a declared distance** | **narrower** | `PrismaticRetentionDescriptor` requires `WindingTransmission` (`PrismaticRetentionValues.cs:13`–`:15`); a released retention re-arms only within `ActuatorStopSlopMetres` (5e-4 m) of travel 0 (`CuriousContraptions.Simulation/SimPrismaticRetention.cs:85`) | Owner: the hold and its release by a physical request bound to `ActivationIn`, `ENGINE-JOINT-KINDS` (holds on a coordinate and their release; engine-stories-index C2), with its re-arm at rest (declared rest speed and settle time) within a declared re-arm distance of the hold position (`readiness/batch-i.md` section 5 G1; summary gap G2; coordinator decisions 31, 33 and 34) |
| A winch to draw it | missing in the catalogue, not in this part | — | EL-063 Cable winch, whose winding is the winding clause of `ENGINE-TENSILE-EXTENSIONS`; a Weight over a Pulley draws it with delivered parts |

Counts: 5 present, 0 untested, 1 narrower, 0 missing in this part.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static fork, one sprung slider on a tilted frame, one retention, one release request and two ports. Drawing is rope tension against the spring law; holding is the retention law; the launch is the spring law, gravity and rigid contact ending at the slider limit; the ball follows the cradle by gravity and contact. The narrower capability is stated for any package: a one-way hold on a coordinate that no transmission winds, and its release by a physical request bound to a declared signal input, owned by `ENGINE-JOINT-KINDS` (engine-stories-index C2). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 7 boxes (base, 2 prongs; cradle floor, back plate, 2 side walls) as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: the two bands are presentation spans and the latch and charge bar are bindings from committed retention state; the bands carry no load (the slider's spring does). Per art node: base and prongs are moved by nothing; the cradle by body 1's pose; bands, latch and charge bar by presentation bindings.
- **Flat state.** 2 bodies; 7 box colliders (0 of any other shape); 1 prismatic joint; 1 retention; 0 drives; 0 sensors; 1 signal node; 0 stores; 1 body-pose binding, 2 spans, 2 driver bindings.
- **Performance.** Few primitives; the fork is static. Whether a latched cradle may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. The ball leaves at about 4.5 m/s (7 m/s at the stiffest setting), within the engine's speculative-contact range.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-396](../requirements.md#todo-396): "Rope/winch draws an elastic cradle; separate latch releases its seated ball. No reflex aiming during Run. Visible band extension and return; bounded authored charge." Lesson column: "Motor/winch charges; sound meter releases through a timed gate. Test zero charge, rope cut, draw limit, oversized load, empty release and no free recharge."
- **Campaign.** Chapter 5 (41–50), "slingshot".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: launch speed ±25 % of the no-damping value v(x) = √((k·x² − 2·m·g·x·sin 30°) ÷ m) for the latched draw x read in the test (the band covers damping, about 6 %, and contact losses; a ball outside it has not been launched by the spring alone); draw ±0.02 m; energy bound + 0.5 J; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| S1 | Basketball seated; an 8 kg Weight over a Pulley draws the cradle to the stop; the rope is cut; the ball is at rest; one pulse | The latched draw is 0.6 m; the ball leaves between the prongs at ±25 % of v(x) (3.6–6.0 m/s at x = 0.6 m) and the cradle returns to within 0.05 m of 0; throughout the draw and the return the two band spans' committed end points stay within 0.02 m of the prong tips and of the cradle's side walls (todo-396, "visible band extension and return") | Control 1: no pulse, the ball stays for 10 s. Control 2: pulse with the rope not cut, the cradle stays at the stop (the Weight holds it) | `SlingshotTests.ADrawnCradleLaunchesItsBallOnRelease` |
| S2 | Never drawn; Basketball seated (the cradle held near the bottom of its first fall, item 1); one pulse | The request reports Released with stored energy ≤ 2 × 7.36² ÷ 120 + 0.5 = 1.4 J (about 0.8 J; Empty is answered only within 5e-4 m of travel 0); the ball never leaves the cup, nothing is launched and the cradle does not pass travel 0 | Control: drawn to the stop as in S1, the same pulse launches the ball | `SlingshotTests.ZeroChargeLaunchesNothing` |
| S3 | Rope cut after drawing; 10 s wait | The cradle stays latched within 0.02 m; the pulse then launches | Control: default 4 kg Weight lowered slowly holds 0.39 m and launches slower than the 8 kg draw | `SlingshotTests.ACutRopeKeepsTheCharge` |
| S4 | `draw` 0.2 with an 8 kg Weight; `draw` 0.8 with `stiffness` 60 (needs 40.6 N) | The cradle stops at `draw` within 0.02 m | Boundary: `draw` 0.2 and 0.8 | `SlingshotTests.TheDrawLimitBoundsTheCharge` |
| S5 | A 1.0 m ball (Tennis ball, `radius` 0.5); no ball | The oversize ball strikes the prongs and does not pass, with no penetration; an empty release moves only the cradle | Boundary: a 0.8 m ball passes | `SlingshotTests.OversizeAndEmptyReleasesStayPhysical` |
| S6 | Two pulses after one draw: the second on the tick after the first; in a second run, the second 6 s after the first | One launch in each run. A second pulse on the next tick reports AlreadyReleased (the cradle is still returning: a quarter period of √(1.5 ÷ 120) × π/2 = 0.18 s). A second pulse after 6 s finds the empty cradle re-armed (at rest at its 0.020 m empty sag, inside the 0.1 m re-arm distance; empty, ζω = 1 ÷ (2 × 0.5) = 1 /s, so a 0.02 m swing has decayed below 0.001 m within ln 20 = 3 s; with ω = √(120 ÷ 0.5) = 15.5 rad/s its speed stays below v_r 0.05 m/s once the amplitude is under 0.0032 m, from about ln(0.02 ÷ 0.0032) = 1.8 s, so it re-arms by about 2.3 s) and held no deeper than its first fall (at most about 0.037 m, item 1): it reports Released with stored energy ≤ 2 × 2.45² ÷ 120 + 0.5 = 0.6 J (about ½ × 120 × 0.020² = 0.024 J), the cradle moves less than 0.05 m and does not pass travel 0. In both runs the ball's kinetic energy at the prongs ≤ ½·k·x² + 0.5 J | — | `SlingshotTests.ThereIsNoFreeRecharge` |
| S7 | Run, Reset; Save, Load | Cradle undrawn, rope, parameters and wire restored | — | `SlingshotTests.ResetAndReloadRestoreTheAuthoredSlingshot` |
| S8 | Electrical wire into `ActivationIn`; `stiffness` 59 | Refused with a typed reason | Boundary: 60 and 250 admitted | `SlingshotTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |
| S9 | Re-arm at rest (coordinator decision 31). Never drawn; Basketball seated; one pulse at 1 s (as S2), then a second pulse 20 s later | After the first pulse the released cradle swings about its seated equilibrium and comes to rest at the 0.061 m sag (±0.01 m), inside the 0.1 m re-arm distance, its speed staying below v_r 0.05 m/s for t_s 0.5 s (seated, ω = √(120 ÷ 1.5) = 8.9 rad/s, so the swing amplitude must fall below 0.05 ÷ 8.9 = 0.0056 m, reached about ln(0.055 ÷ 0.0056) ÷ 0.33 = 6.9 s after release, re-armed by about 7.4 s), and re-arms: the latch binding shows armed and the second pulse reports **Released**, not AlreadyReleased (stored about ½ × 120 × 0.061² = 0.22 J; nothing is launched and the ball stays in the cup). Seated, ζω = 1 ÷ (2 × 1.5) = 0.33 /s, so the 0.116 − 0.061 = 0.055 m swing has decayed below 0.001 m within ln 55 ÷ 0.33 = 12 s, inside the 20 s wait. The delivered law, which re-arms only within 5e-4 m of travel 0, fails this row | Control: a test declaration with the same cradle and a re-arm distance of 0.05 m, below the 0.061 m rest: the cradle rests at the same place, stays released, and the second pulse reports AlreadyReleased | `SlingshotTests.AReleasedCradleReArmsWhereItComesToRest` |

The requirement's lesson "sound meter releases through a timed gate" needs a sound meter after ENGINE-ACOUSTIC, and "Motor/winch charges" needs EL-063; both are later-partner rows in those slices.

**Integration.** Weight, Pulley, Rope anchor, Hold timer, Impact switch (delivered); EL-063 winch (later). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `slingshot`; Named identity EL-225; Acceptance todo-396 and sequence-task-379; owner slice `named-el-225`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** One element slice. Prerequisites: blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP`; then `ENGINE-JOINT-KINDS` (the one-way hold without a winding transmission, its release by physical request, and its re-arm at rest within a declared distance, row S9), and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/Slingshot/` package, `CuriousContraptions.tests/SlingshotTests.cs`, and the hand-edited lists until S3. Review tier: engine. **Disqualified if** the only way to declare the latch is an internal winding shaft (then the part is a Wound spring with a cradle and must be re-examined as a duplicate); or the ball does not stay on the floor through an 8 kg drop-draw (floor length and the 30° tilt are the tuning values; the rows do not change).

## 6. Open questions

1. *Rope-drawn or shaft-wound?* Resolved under the standing delegation: rope-drawn, as the requirement says; shaft winding is the Wound spring's contract.
2. *Aiming.* Resolved: the placed orientation only; the authored axis is 30° up so the ball seats.
3. *How does the drawing load let go?* Resolved under the standing delegation (audit F3): the rope is cut or paid out before release; a load left on `Tie` holds the cradle and that is asserted as a control.
4. *Default stiffness.* Resolved under the standing delegation: 120 N/m, so the heaviest Weight draws the default fully by plain statics; the earlier 250 N/m could not be held (150 N against 78.5 N).

5. *A pulse on a sagged, undrawn cradle (second audit X-2, 23 November 2026).* Under the delivered retention law it answers Released (item 1), not Empty; rows S2 and S6 assert that answer and no row depends on Empty. After such a release the delivered law would leave the hold released, because it re-arms only within 5e-4 m of travel 0 (`SimPrismaticRetention.cs:85`) and this damped cradle comes to rest at its sag. **Decided (coordinator decision 31, 23 November 2026):** a released coordinate re-arms when it comes to rest within a declared re-arm distance of its hold position (`ENGINE-JOINT-KINDS`); this cradle declares 0.1 m (section 2, Retention), and row S9 asserts the re-arm with a control outside the distance.

Needs the owner: nothing.
